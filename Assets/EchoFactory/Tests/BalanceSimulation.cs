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

        public static readonly string[] Names = { "Bierny", "Prasy z rynku", "Ekspansja", "Lekkomyslny", "Badania" };

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
                case 4: Research(s); break;
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

        // A12: research hall + technologies, then improved presses in every free slot of production halls.
        static void Research(StrategicState s)
        {
            if (!ResearchSystem.HasResearchHall(s))
            {
                int parcel = -1;
                for (int i = 0; i < s.Parcels.Count; i++) if (s.Parcels[i].Owned) { parcel = s.Parcels[i].Id; break; }
                if (parcel >= 0 && Affordable(s, ResearchSystem.ResearchHallPrice, 1500)) ResearchSystem.BuildResearchHall(s, parcel);
            }
            else
            {
                TechnologyKind[] order = { TechnologyKind.BasicAutomation };
                for (int i = 0; i < order.Length; i++)
                    if (!s.HasTechnology(order[i])) { if (s.Credits - ResearchCostOf(s, order[i]) >= 1500) ResearchSystem.UnlockTechnology(s, order[i]); break; }
            }
            for (int i = 0; i < s.Facilities.Count; i++)
            {
                var f = s.Facilities[i];
                if (f.Kind != FacilityKind.ProductionHall) continue;
                var kind = s.HasTechnology(TechnologyKind.ImprovedPress) ? MachineKind.ImprovedPress : MachineKind.BasicPress;
                while (f.Machines.Count < ProductionSystem.SlotCount(s, f) && Affordable(s, ProductionSystem.MachineCost(kind), 1500) && ProductionSystem.BuildMachine(s, f.Id, kind)) { }
            }
            FeedAll(s, 30);
        }

        static int ResearchCostOf(StrategicState s, TechnologyKind k)
        {
            for (int i = 0; i < s.Technologies.Count; i++) if (s.Technologies[i].Kind == k) return s.Technologies[i].ResearchCost;
            return int.MaxValue;
        }

        // Buys exactly the steel and energy every press (basic or improved) needs next turn.
        static void FeedAll(StrategicState s, int maxUnits)
        {
            int steel = 0, energy = 0;
            int improvedEnergy = s.HasTechnology(TechnologyKind.EnergyEfficiency) ? 1 : 2;
            for (int i = 0; i < s.Facilities.Count; i++)
                for (int j = 0; j < s.Facilities[i].Machines.Count; j++)
                {
                    var k = s.Facilities[i].Machines[j].Kind;
                    if (k == MachineKind.BasicPress) { steel++; energy++; }
                    else if (k == MachineKind.ImprovedPress) { steel++; energy += improvedEnergy; }
                }
            if (steel > s.GetStock(ResourceKind.Steel)) MarketSystem.Buy(s, ResourceKind.Steel, Math.Min(steel - s.GetStock(ResourceKind.Steel), maxUnits));
            if (energy > s.GetStock(ResourceKind.Energy)) MarketSystem.Buy(s, ResourceKind.Energy, Math.Min(energy - s.GetStock(ResourceKind.Energy), maxUnits));
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
