using System;

namespace EchoFactory.Core
{
    public sealed class TurnResult
    {
        public int Turn;
        public int Income;
        public int Costs;
        public int Production;
        public int EndingCredits;
    }

    public static class TurnSystem
    {
        public const int BaseOperatingCost = 150;
        public const int ProductValue = 120;

        public static TurnResult EndTurn(StrategicState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.Phase != StrategicPhase.Planning) throw new InvalidOperationException("Turn can only end from Planning phase.");

            state.Phase = StrategicPhase.Resolving;
            int production = 0;
            int inputUsed = 0;

            for (int i = 0; i < state.Facilities.Count; i++)
            {
                var facility = state.Facilities[i];
                if (facility.Kind != FacilityKind.ProductionHall) continue;
                var cycles = Math.Max(0, facility.Level + facility.MachineSlots - 1);
                var available = state.GetStock(ResourceKind.Steel);
                var made = Math.Min(cycles, available);
                state.AddStock(ResourceKind.Steel, -made);
                inputUsed += made;
                production += made;
            }

            int income = production * ProductValue;
            int costs = BaseOperatingCost + state.Facilities.Count * 50;
            state.Credits += income - costs;
            state.LastTurnIncome = income;
            state.LastTurnCosts = costs;
            state.LastTurnProduction = production;
            state.Phase = StrategicPhase.Summary;

            return new TurnResult { Turn=state.Turn, Income=income, Costs=costs, Production=production, EndingCredits=state.Credits };
        }

        public static void ContinueToPlanning(StrategicState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.Phase != StrategicPhase.Summary) throw new InvalidOperationException("A turn must be resolved before continuing.");
            state.Turn++;
            state.Phase = StrategicPhase.Planning;
        }
    }
}
