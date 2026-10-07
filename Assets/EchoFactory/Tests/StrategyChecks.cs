using System;
using EchoFactory.Core;

namespace EchoFactory.Tests
{
    public static class StrategyChecks
    {
        public static int Run()
        {
            int assertions=0;
            var a=StrategicWorldGenerator.NewGame(12345);
            var b=StrategicWorldGenerator.NewGame(12345);
            Assert(a.Parcels.Count==9,ref assertions);
            Assert(a.Parcels[0].Owned,ref assertions);
            Assert(a.Facilities.Count==1 && a.Facilities[0].Machines.Count==1,ref assertions);
            Assert(a.Parcels[3].Resource==b.Parcels[3].Resource && a.Parcels[3].Price==b.Parcels[3].Price,ref assertions);
            Assert(a.Credits==10000,ref assertions);
            Assert(a.GetStock(ResourceKind.Energy)==5,ref assertions);

            var result=TurnSystem.EndTurn(a);
            Assert(a.Phase==StrategicPhase.Summary,ref assertions);
            Assert(result.Production==1,ref assertions);
            Assert(result.PassiveIncome==300,ref assertions);
            Assert(result.Income==420,ref assertions);
            Assert(result.SteelConsumed==1 && result.EnergyConsumed==1,ref assertions);
            Assert(result.ScrapGenerated==1,ref assertions);
            Assert(a.GetStock(ResourceKind.FinishedGoods)==1,ref assertions);
            Assert(a.GetStock(ResourceKind.Scrap)==1,ref assertions);
            Assert(a.Credits==10220,ref assertions);
            TurnSystem.ContinueToPlanning(a);
            Assert(a.Turn==2 && a.Phase==StrategicPhase.Planning,ref assertions);

            var strategy=StrategicWorldGenerator.NewGame(77);
            strategy.Credits=19300;
            Assert(ResearchSystem.BuildResearchHall(strategy,0),ref assertions);
            Assert(strategy.Facilities.Count==2,ref assertions);
            Assert(ResearchSystem.UnlockTechnology(strategy,TechnologyKind.BasicAutomation),ref assertions);
            Assert(!ProductionSystem.BuildMachine(strategy,strategy.Facilities[0].Id,MachineKind.Recycler),ref assertions);
            Assert(FacilitySystem.Build(strategy,0,FacilityKind.Workshop),ref assertions);
            var workshop=strategy.Facilities[strategy.Facilities.Count-1];
            Assert(ProductionSystem.BuildMachine(strategy,workshop.Id,MachineKind.Recycler),ref assertions);
            Assert(strategy.Credits==0,ref assertions);

            var parcels=StrategicWorldGenerator.NewGame(5);
            var target=parcels.Parcels[1]; parcels.Credits=target.Price;
            Assert(ParcelSystem.CanBuy(parcels,target.Id),ref assertions);
            Assert(ParcelSystem.Buy(parcels,target.Id) && target.Owned && parcels.Credits==0,ref assertions);
            Assert(!ParcelSystem.Buy(parcels,target.Id),ref assertions);
            Assert(!ParcelSystem.Buy(parcels,2),ref assertions);

            var synergy=StrategicWorldGenerator.NewGame(88);
            synergy.Credits=30000;
            Assert(ResearchSystem.BuildResearchHall(synergy,0),ref assertions);
            Assert(ResearchSystem.UnlockTechnology(synergy,TechnologyKind.BasicAutomation),ref assertions);
            Assert(!ResearchSystem.UnlockTechnology(synergy,TechnologyKind.AdvancedPress),ref assertions);
            Assert(ResearchSystem.CanUnlock(synergy,TechnologyKind.ImprovedPress),ref assertions);
            Assert(ResearchSystem.UnlockTechnology(synergy,TechnologyKind.ImprovedPress),ref assertions);
            Assert(!ResearchSystem.UnlockTechnology(synergy,TechnologyKind.ImprovedPress),ref assertions);
            Assert(ResearchSystem.UnlockTechnology(synergy,TechnologyKind.EnergyEfficiency),ref assertions);
            Assert(ProductionSystem.BuildMachine(synergy,synergy.Facilities[0].Id,MachineKind.ImprovedPress),ref assertions);
            synergy.Facilities[0].Machines[0].Enabled=false; // isolate the improved press from the starter press
            synergy.AddStock(ResourceKind.Steel,1); synergy.AddStock(ResourceKind.Energy,2);
            var before=synergy.GetStock(ResourceKind.Energy);
            var chain=ProductionSystem.Resolve(synergy);
            Assert(chain.FinishedGoods==2,ref assertions);
            Assert(before-synergy.GetStock(ResourceKind.Energy)==1,ref assertions);

            return assertions;
        }

        static void Assert(bool condition,ref int count){if(!condition)throw new InvalidOperationException("Strategic check failed.");count++;}
    }
}
