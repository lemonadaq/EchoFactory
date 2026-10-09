using System;
using System.Collections.Generic;
using EchoFactory.Core;

namespace EchoFactory.Tests
{
    public sealed class BalanceResult
    {
        public string Name;
        public int FinalCredits, MinCredits, Turns;
        public GameOutcome Outcome;
    }

    // A09: scripted players run the real turn loop for a fixed number of turns; used to check that the economy is balanced.
    public static class BalanceSimulation
    {
        public const int Turns = 30;

        public static readonly string[] Names = { "Bierny", "Prasy z rynku", "Ekspansja", "Lekkomyslny" };

        public static BalanceResult Run(int strategy, int seed, int turns = Turns)
        {
            var s = StrategicWorldGenerator.NewGame(seed);
            var result = new BalanceResult { Name = Names[strategy], MinCredits = s.Credits };
            for (int t = 0; t < turns && s.Outcome == GameOutcome.Playing; t++)
            {
                Plan(strategy, s);
                TurnSystem.EndTurn(s);
                result.Turns = t + 1;
                if (s.Credits < result.MinCredits) result.MinCredits = s.Credits;
                if (s.Outcome == GameOutcome.Playing) TurnSystem.ContinueToPlanning(s);
            }
            result.FinalCredits = s.Credits; result.Outcome = s.Outcome;
            return result;
        }

        static void Plan(int strategy, StrategicState s)
        {
            if (strategy != 3) SellAll(s);
            switch (strategy)
            {
                case 0: break; // does nothing but end turns
                case 1: BuildPress(s, 2); Feed(s, 20); break;
                case 2: Expand(s); Feed(s, 20); break;
                case 3: Reckless(s); break;
            }
        }

        static void SellAll(StrategicState s)
        {
            MarketSystem.Sell(s, ResourceKind.FinishedGoods, s.GetStock(ResourceKind.FinishedGoods));
            MarketSystem.Sell(s, ResourceKind.Scrap, s.GetStock(ResourceKind.Scrap));
        }

        // Keeps a Credits reserve so that building never starves the market purchases.
        static bool Affordable(StrategicState s, int cost, int reserve) { return s.Credits - cost >= reserve; }

        static void BuildPress(StrategicState s, int total)
        {
            int count = 0;
            for (int i = 0; i < s.Facilities.Count; i++) for (int j = 0; j < s.Facilities[i].Machines.Count; j++) if (s.Facilities[i].Machines[j].Kind == MachineKind.BasicPress) count++;
            for (int i = 0; i < s.Facilities.Count && count < total; i++)
                while (count < total && Affordable(s, ProductionSystem.MachineCost(MachineKind.BasicPress), 1500) && ProductionSystem.BuildMachine(s, s.Facilities[i].Id, MachineKind.BasicPress)) count++;
        }

        // Buys steel and energy so that every press can run next turn (stock target = machines of the kind).
        static void Feed(StrategicState s, int maxUnits)
        {
            int presses = 0;
            for (int i = 0; i < s.Facilities.Count; i++) for (int j = 0; j < s.Facilities[i].Machines.Count; j++) if (s.Facilities[i].Machines[j].Kind == MachineKind.BasicPress) presses++;
            int steelNeed = presses - s.GetStock(ResourceKind.Steel), energyNeed = presses - s.GetStock(ResourceKind.Energy);
            if (steelNeed > 0) MarketSystem.Buy(s, ResourceKind.Steel, Math.Min(steelNeed, maxUnits));
            if (energyNeed > 0) MarketSystem.Buy(s, ResourceKind.Energy, Math.Min(energyNeed, maxUnits));
        }

        static void Expand(StrategicState s)
        {
            // First a second parcel, then a production hall on it, then presses.
            ParcelState target = null;
            for (int i = 0; i < s.Parcels.Count; i++)
            {
                var p = s.Parcels[i];
                if (p.Owned || (p.Resource != ResourceKind.Steel && p.Resource != ResourceKind.Energy)) continue;
                if (target == null || p.Price - p.ResourceRichness * 40 < target.Price - target.ResourceRichness * 40) target = p;
            }
            int owned = 0;
            for (int i = 0; i < s.Parcels.Count; i++) if (s.Parcels[i].Owned) owned++;
            BuildPress(s, 2);
            if (owned < 2 && target != null && Affordable(s, target.Price, 1500)) ParcelSystem.Buy(s, target.Id);
            for (int i = 0; i < s.Parcels.Count; i++)
                if (s.Parcels[i].Owned && Affordable(s, FacilitySystem.GetPrice(FacilityKind.ProductionHall), 1500)) FacilitySystem.Build(s, s.Parcels[i].Id, FacilityKind.ProductionHall);
            BuildPress(s, 6);
        }

        // Spends everything on halls and machines, buys no input: should be able to go bankrupt.
        static void Reckless(StrategicState s)
        {
            for (int i = 0; i < s.Parcels.Count; i++) if (s.Parcels[i].Owned) { FacilitySystem.Build(s, s.Parcels[i].Id, FacilityKind.Workshop); FacilitySystem.Build(s, s.Parcels[i].Id, FacilityKind.ProductionHall); FacilitySystem.Build(s, s.Parcels[i].Id, FacilityKind.EnergyHall); }
            for (int i = 0; i < s.Parcels.Count; i++) if (!s.Parcels[i].Owned) ParcelSystem.Buy(s, s.Parcels[i].Id);
            for (int i = 0; i < s.Facilities.Count; i++) while (ProductionSystem.BuildMachine(s, s.Facilities[i].Id, MachineKind.BasicPress)) { }
        }
    }
}
