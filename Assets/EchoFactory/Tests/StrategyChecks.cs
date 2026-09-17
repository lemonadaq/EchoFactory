using System;
using EchoFactory.Core;

namespace EchoFactory.Tests
{
    public static class StrategyChecks
    {
        public static int Run()
        {
            int assertions = 0;
            var a = StrategicWorldGenerator.NewGame(12345);
            var b = StrategicWorldGenerator.NewGame(12345);
            Assert(a.Parcels.Count == 9, ref assertions);
            Assert(a.Parcels[0].Owned, ref assertions);
            Assert(a.Facilities.Count == 1, ref assertions);
            Assert(a.Parcels[3].Resource == b.Parcels[3].Resource && a.Parcels[3].Price == b.Parcels[3].Price, ref assertions);
            Assert(a.Credits == 10000, ref assertions);

            var result = TurnSystem.EndTurn(a);
            Assert(a.Phase == StrategicPhase.Summary, ref assertions);
            Assert(result.Production == 2, ref assertions);
            Assert(result.Income == 240, ref assertions);
            Assert(a.Credits == 9940, ref assertions);
            TurnSystem.ContinueToPlanning(a);
            Assert(a.Turn == 2 && a.Phase == StrategicPhase.Planning, ref assertions);
            return assertions;
        }

        static void Assert(bool condition, ref int count)
        {
            if (!condition) throw new InvalidOperationException("Strategic check failed.");
            count++;
        }
    }
}
