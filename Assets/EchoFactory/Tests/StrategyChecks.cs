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
            Assert(result.Income==300,ref assertions);
            Assert(result.SteelConsumed==1 && result.EnergyConsumed==1,ref assertions);
            Assert(result.ScrapGenerated==1,ref assertions);
            Assert(a.GetStock(ResourceKind.FinishedGoods)==1,ref assertions);
            Assert(a.GetStock(ResourceKind.Scrap)==1,ref assertions);
            Assert(a.Credits==10100,ref assertions);
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

            // A03: bottleneck report gives a reason for every machine that did not work.
            var bn=StrategicWorldGenerator.NewGame(31);
            bn.Facilities[0].Machines[0].Kind=MachineKind.BasicPress;
            bn.Stock[ResourceKind.Steel]=0; bn.Stock[ResourceKind.Energy]=5;
            var rNoSteel=ProductionSystem.Resolve(bn);
            Assert(rNoSteel.Reports.Count==1 && rNoSteel.Reports[0].Reason==IdleReason.NoSteel && !rNoSteel.Reports[0].Worked,ref assertions);
            bn.Stock[ResourceKind.Steel]=3; bn.Stock[ResourceKind.Energy]=0;
            Assert(ProductionSystem.Resolve(bn).Reports[0].Reason==IdleReason.NoEnergy,ref assertions);
            bn.Stock[ResourceKind.Energy]=2;
            Assert(ProductionSystem.Resolve(bn).Reports[0].Worked,ref assertions);
            bn.Facilities[0].Machines[0].Enabled=false;
            Assert(ProductionSystem.Resolve(bn).Reports[0].Reason==IdleReason.Disabled,ref assertions);
            bn.Facilities[0].Machines[0].Enabled=true; bn.Facilities[0].Machines[0].Kind=MachineKind.Recycler;
            Assert(ProductionSystem.Resolve(bn).Reports[0].Reason==IdleReason.MissingTechnology,ref assertions);
            bn.Facilities[0].Machines[0].Kind=MachineKind.Generator; bn.Stock[ResourceKind.Bitumen]=0;
            Assert(ProductionSystem.Resolve(bn).Reports[0].Reason==IdleReason.NoBitumen,ref assertions);
            var bnTurn=StrategicWorldGenerator.NewGame(31);
            TurnSystem.EndTurn(bnTurn);
            Assert(bnTurn.LastMachineReports.Count==1 && bnTurn.LastMachineReports[0].Worked,ref assertions);

            // A01: extraction keeps the starter strategy alive and never produces from nothing.
            var mine=StrategicWorldGenerator.NewGame(12345);
            Assert(mine.Parcels[0].Resource==ResourceKind.Steel,ref assertions);
            var rich=new ParcelState{Owned=true,Resource=ResourceKind.Bitumen,ResourceRichness=100};
            var poor=new ParcelState{Owned=true,Resource=ResourceKind.Bitumen,ResourceRichness=35};
            Assert(ExtractionSystem.YieldPerTurn(rich)==5 && ExtractionSystem.YieldPerTurn(poor)==1,ref assertions);
            Assert(ExtractionSystem.YieldPerTurn(new ParcelState{Owned=false,Resource=ResourceKind.Steel,ResourceRichness=100})==0,ref assertions);
            int goods=0, steelMined=mine.Parcels[0].ResourceRichness/ExtractionSystem.RichnessPerUnit;
            for(int t=0;t<20;t++)
            {
                var r=TurnSystem.EndTurn(mine);
                Assert(mine.LastExtracted[ResourceKind.Steel]==steelMined,ref assertions);
                Assert(r.Production>=1,ref assertions);
                goods+=r.Production;
                TurnSystem.ContinueToPlanning(mine);
            }
            Assert(goods==20 && mine.Credits>10000,ref assertions);
            var bought=StrategicWorldGenerator.NewGame(5); var extra=bought.Parcels[1]; bought.Credits=extra.Price;
            int before1=bought.GetStock(extra.Resource);
            ParcelSystem.Buy(bought,extra.Id); var mined=ExtractionSystem.Extract(bought);
            Assert(mined[extra.Resource]>=ExtractionSystem.YieldPerTurn(extra) && bought.GetStock(extra.Resource)>before1,ref assertions);

            // A07: a Logistics Hall raises the parcel's yield; without it nothing changes.
            var lg=StrategicWorldGenerator.NewGame(5); var lp=lg.Parcels[1]; lp.Owned=true; lp.LogisticsBonus=12; lp.ResourceRichness=60; lg.Credits=20000;
            int lbase=ExtractionSystem.YieldPerTurn(lp);
            Assert(ExtractionSystem.LogisticsYieldBonus(lg,lp)==0,ref assertions);
            Assert(FacilitySystem.Build(lg,lp.Id,FacilityKind.LogisticsHall) && ExtractionSystem.LogisticsYieldBonus(lg,lp)==2,ref assertions);
            int lstock=lg.GetStock(lp.Resource); var lmined=ExtractionSystem.Extract(lg);
            Assert(lmined[lp.Resource]==lbase+2 && lg.GetStock(lp.Resource)==lstock+lbase+2,ref assertions);
            Assert(ExtractionSystem.LogisticsYieldBonus(lg,lg.Parcels[0])==0,ref assertions);

            // A02: market prices follow supply; goods are no longer auto-sold.
            var m=StrategicWorldGenerator.NewGame(1);
            Assert(m.Credits==10000 && MarketSystem.BuyPrice(m,ResourceKind.Steel)>MarketSystem.SellPrice(m,ResourceKind.Steel),ref assertions);
            Assert(!MarketSystem.CanBuy(m,ResourceKind.FinishedGoods,1) && !MarketSystem.CanSell(m,ResourceKind.Steel,1),ref assertions);
            int steel0=m.GetStock(ResourceKind.Steel), p0=MarketSystem.BuyPrice(m,ResourceKind.Steel), quote=MarketSystem.BuyQuote(m,ResourceKind.Steel,3);
            Assert(quote>=3*p0 && MarketSystem.Buy(m,ResourceKind.Steel,3),ref assertions);
            Assert(m.Credits==10000-quote && m.GetStock(ResourceKind.Steel)==steel0+3,ref assertions);
            Assert(MarketSystem.BuyPrice(m,ResourceKind.Steel)>p0,ref assertions);
            m.AddStock(ResourceKind.FinishedGoods,4);
            int s0=MarketSystem.SellPrice(m,ResourceKind.FinishedGoods), payout=MarketSystem.SellQuote(m,ResourceKind.FinishedGoods,4), cr=m.Credits;
            Assert(payout<=4*s0 && payout>0 && MarketSystem.Sell(m,ResourceKind.FinishedGoods,4),ref assertions);
            Assert(m.Credits==cr+payout && m.GetStock(ResourceKind.FinishedGoods)==0,ref assertions);
            Assert(MarketSystem.SellPrice(m,ResourceKind.FinishedGoods)<s0,ref assertions);
            Assert(!MarketSystem.Sell(m,ResourceKind.FinishedGoods,1),ref assertions);
            var poorM=StrategicWorldGenerator.NewGame(1); poorM.Credits=10;
            Assert(!MarketSystem.Buy(poorM,ResourceKind.Electronics,1) && poorM.Credits==10 && poorM.GetStock(ResourceKind.Electronics)==0,ref assertions);
            m.AddStock(ResourceKind.Scrap,100); MarketSystem.Sell(m,ResourceKind.Scrap,100);
            Assert(MarketSystem.SellPrice(m,ResourceKind.Scrap)>=1 && m.GetSupply(ResourceKind.Scrap)==MarketSystem.MaxSupply,ref assertions);
            var cp=m.Copy(); Assert(cp.GetSupply(ResourceKind.Scrap)==MarketSystem.MaxSupply,ref assertions);
            TurnSystem.EndTurn(m);
            Assert(m.GetSupply(ResourceKind.Scrap)==MarketSystem.MaxSupply-1 && m.GetSupply(ResourceKind.Steel)==-2,ref assertions);
            var loop=StrategicWorldGenerator.NewGame(12345);
            for(int t=0;t<20;t++){TurnSystem.EndTurn(loop); TurnSystem.ContinueToPlanning(loop); MarketSystem.Sell(loop,ResourceKind.FinishedGoods,loop.GetStock(ResourceKind.FinishedGoods)); Assert(loop.GetStock(ResourceKind.Steel)>0,ref assertions);}
            Assert(loop.Credits>10000,ref assertions);

            // A04: save snapshot round-trip.
            var sv=StrategicWorldGenerator.NewGame(77); sv.Credits=sv.Parcels[1].Price; ParcelSystem.Buy(sv,1);
            MarketSystem.Sell(sv,ResourceKind.Steel,3); sv.AddStock(ResourceKind.Scrap,4);
            TurnSystem.EndTurn(sv);
            var snap=StrategicSave.From(sv); string why;
            Assert(snap.Valid(out why) && snap.Stock.Count>0 && snap.LastMachineReports.Count==sv.LastMachineReports.Count,ref assertions);
            var back=snap.ToState();
            Assert(back.Turn==sv.Turn && back.Credits==sv.Credits && back.Phase==StrategicPhase.Summary && back.Parcels.Count==sv.Parcels.Count && back.Parcels[1].Owned,ref assertions);
            Assert(back.GetStock(ResourceKind.Steel)==sv.GetStock(ResourceKind.Steel) && back.GetStock(ResourceKind.Scrap)==sv.GetStock(ResourceKind.Scrap) && back.GetSupply(ResourceKind.Steel)==sv.GetSupply(ResourceKind.Steel),ref assertions);
            Assert(back.LastExtracted.Count==sv.LastExtracted.Count && back.LastMachineReports.Count==sv.LastMachineReports.Count && back.Facilities[0].Machines.Count==sv.Facilities[0].Machines.Count,ref assertions);
            TurnSystem.ContinueToPlanning(back); TurnSystem.EndTurn(back);
            Assert(back.Turn==sv.Turn+1,ref assertions);
            var bad=StrategicSave.From(sv); bad.Version=99; Assert(!bad.Valid(out why),ref assertions);
            var bad2=StrategicSave.From(sv); bad2.Facilities[0].ParcelId=50; Assert(!bad2.Valid(out why),ref assertions);

            // A05: goal and bankruptcy.
            var g=StrategicWorldGenerator.NewGame(3);
            TurnSystem.EndTurn(g);
            Assert(g.Outcome==GameOutcome.Playing && g.DebtTurns==0,ref assertions);
            var win=StrategicWorldGenerator.NewGame(3); win.Credits=GoalSystem.WinCredits;
            TurnSystem.EndTurn(win);
            Assert(win.Outcome==GameOutcome.Won,ref assertions);
            var wc=win.Copy(); Assert(wc.Outcome==GameOutcome.Won,ref assertions);
            bool threw=false; try{TurnSystem.ContinueToPlanning(win);}catch(InvalidOperationException){threw=true;}
            Assert(threw,ref assertions);
            var debt=StrategicWorldGenerator.NewGame(3); debt.Credits=-5000;
            TurnSystem.EndTurn(debt);
            Assert(debt.Credits<0 && debt.DebtTurns==1 && debt.Outcome==GameOutcome.Playing,ref assertions);
            TurnSystem.ContinueToPlanning(debt); debt.Credits=1000; TurnSystem.EndTurn(debt);
            Assert(debt.DebtTurns==0,ref assertions);
            TurnSystem.ContinueToPlanning(debt); debt.Credits=-5000; TurnSystem.EndTurn(debt);
            TurnSystem.ContinueToPlanning(debt); debt.Credits=-5000; TurnSystem.EndTurn(debt);
            Assert(debt.Outcome==GameOutcome.Lost && debt.DebtTurns==GoalSystem.BankruptcyTurns,ref assertions);
            threw=false; try{TurnSystem.ContinueToPlanning(debt);}catch(InvalidOperationException){threw=true;}
            Assert(threw,ref assertions);
            var lostBack=StrategicSave.From(debt).ToState();
            Assert(lostBack.Outcome==GameOutcome.Lost && lostBack.DebtTurns==debt.DebtTurns,ref assertions);
            threw=false; try{TurnSystem.EndTurn(lostBack);}catch(InvalidOperationException){threw=true;}
            Assert(threw,ref assertions);

            var lay=StrategicWorldGenerator.NewGame(3); FactoryLayoutSystem.EnsureLayout(lay,1);
            lay.Credits=10000; Assert(FactoryLayoutSystem.PlaceMachine(lay,1,MachineKind.BasicPress,0.9f,0.9f),ref assertions);
            var fac=lay.Facilities[0]; int firstId=fac.Machines[0].Id; int newId=fac.Machines[1].Id;
            Assert(!FactoryLayoutSystem.MoveMachine(lay,1,newId,fac.Machines[0].X,fac.Machines[0].Y),ref assertions);
            Assert(!FactoryLayoutSystem.MoveMachine(lay,1,newId,1.5f,0.5f),ref assertions);
            Assert(!FactoryLayoutSystem.MoveMachine(lay,1,9999,0.5f,0.9f),ref assertions);
            Assert(FactoryLayoutSystem.MoveMachine(lay,1,newId,0.1f,0.9f) && fac.Machines[1].X==0.1f && fac.Machines[1].Y==0.9f,ref assertions);
            Assert(FactoryLayoutSystem.MoveMachine(lay,1,newId,0.1f,0.9f),ref assertions);
            int creditsBefore=lay.Credits; int machinePrice=ProductionSystem.GetMachinePrice(MachineKind.BasicPress);
            Assert(FactoryLayoutSystem.DemolishMachine(lay,1,newId)==machinePrice/2 && lay.Credits==creditsBefore+machinePrice/2 && fac.Machines.Count==1,ref assertions);
            Assert(FactoryLayoutSystem.DemolishMachine(lay,1,newId)==-1 && lay.Credits==creditsBefore+machinePrice/2,ref assertions);
            Assert(FactoryLayoutSystem.DemolishMachine(lay,1,firstId)>=0 && fac.Machines.Count==0,ref assertions);
            lay.Phase=StrategicPhase.Summary;
            Assert(FactoryLayoutSystem.DemolishMachine(lay,1,firstId)==-1 && !FactoryLayoutSystem.CanMoveMachine(lay,1,firstId,0.5f,0.5f),ref assertions);

            return assertions;
        }

        static void Assert(bool condition,ref int count){if(!condition)throw new InvalidOperationException("Strategic check failed.");count++;}
    }
}
