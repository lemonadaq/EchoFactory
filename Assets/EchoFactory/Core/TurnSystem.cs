using System;

namespace EchoFactory.Core
{
    public sealed class TurnResult
    {
        public int Turn, Income, Costs, Production, PassiveIncome, EndingCredits;
        public int SteelConsumed, EnergyConsumed, ScrapGenerated, ScrapRecycled, EnergyGenerated;
    }

    public static class TurnSystem
    {
        public const int BaseOperatingCost=100;
        public const int OperatingCostPerFacility=100;

        public static TurnResult EndTurn(StrategicState state)
        {
            if(state==null)throw new ArgumentNullException("state");
            if(state.Phase!=StrategicPhase.Planning)throw new InvalidOperationException("Turn can only end from Planning phase.");
            if(state.Outcome!=GameOutcome.Playing)throw new InvalidOperationException("The game is over.");
            state.Phase=StrategicPhase.Resolving;
            state.LastExtracted=ExtractionSystem.Extract(state);
            var production=ProductionSystem.Resolve(state);
            int passiveIncome=0;
            for(int i=0;i<state.Facilities.Count;i++)if(state.Facilities[i].Unlocked)passiveIncome+=state.Facilities[i].PassiveIncomePerTurn;
            int income=passiveIncome; // finished goods are no longer sold automatically: see MarketSystem
            MarketSystem.Recover(state);
            int costs=BaseOperatingCost+state.Facilities.Count*OperatingCostPerFacility;
            state.Credits+=income-costs;
            state.LastTurnIncome=income; state.LastPassiveIncome=passiveIncome; state.LastTurnCosts=costs; state.LastTurnProduction=production.FinishedGoods; state.LastMachineReports=production.Reports;
            GoalSystem.Evaluate(state);
            state.Phase=StrategicPhase.Summary;
            return new TurnResult{Turn=state.Turn,Income=income,PassiveIncome=passiveIncome,Costs=costs,Production=production.FinishedGoods,EndingCredits=state.Credits,SteelConsumed=production.SteelConsumed,EnergyConsumed=production.EnergyConsumed,ScrapGenerated=production.ScrapGenerated,ScrapRecycled=production.ScrapRecycled,EnergyGenerated=production.EnergyGenerated};
        }

        public static void ContinueToPlanning(StrategicState state)
        {
            if(state==null)throw new ArgumentNullException("state");
            if(state.Phase!=StrategicPhase.Summary)throw new InvalidOperationException("A turn must be resolved before continuing.");
            if(state.Outcome!=GameOutcome.Playing)throw new InvalidOperationException("The game is over.");
            state.Turn++; state.Phase=StrategicPhase.Planning;
        }
    }
}
