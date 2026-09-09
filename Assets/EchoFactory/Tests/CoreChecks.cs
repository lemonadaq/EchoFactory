using System;
using System.Collections.Generic;
using System.Linq;
using EchoFactory.Core;

namespace EchoFactory.Tests
{
    public static class CoreChecks
    {
        private static int checks;
        private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception("FAIL: " + message); }
        private static void Action(Simulation sim, int id, CommandKind kind, Material material, int quantity = 1)
        {
            string reason; Check(sim.EnqueueAction(id, kind, material, quantity, out reason), "enqueue: " + reason);
            int before = sim.Operator.Errors;
            while (!sim.Operator.Idle && !sim.Finished) sim.Step();
            Check(sim.Operator.Errors == before, "operator action failed");
        }
        private static Recording Record(Simulation sim, int id = 1)
        {
            return new Recording { Id = id, Profile = sim.Operator.Profile.Copy(), Commands = sim.Operator.Commands.Select(c => c.Copy()).ToList() };
        }
        private static void Produce(Simulation sim)
        {
            Action(sim, 1, CommandKind.Pickup, Material.Ore);
            Action(sim, 2, CommandKind.Deposit, Material.Ore);
            Action(sim, 2, CommandKind.Pickup, Material.Plate);
            Action(sim, 3, CommandKind.Deposit, Material.Plate);
        }
        public static string Run()
        {
            checks = 0; string why; var layout = Layout.Default(); Check(layout.Valid(out why), "default layout");
            var manual = new Simulation(layout, new Recording[0], true); Produce(manual); Produce(manual); Produce(manual);
            Check(manual.Shipped == 3 && manual.ErrorCount == 0, "manual chain");
            var echo = Record(manual); var replay = new Simulation(layout, new[] { echo }, false); replay.RunToEnd();
            Check(replay.Shipped == 3 && replay.ErrorCount == 0 && !replay.HasUnfinished, "semantic replay");
            for (int i = 0; i < 1000; i++)
            {
                var same = new Simulation(layout, new[] { echo }, false);
                // Deliberately group simulation ticks differently, independent of rendering.
                int group = i % 7 + 1;
                while (!same.Finished) for (int j = 0; j < group; j++) same.Step();
                Check(same.Trace == replay.Trace && same.Shipped == replay.Shipped, "determinism " + i);
                Check(same.Events.Count == replay.Events.Count && same.Events.Zip(replay.Events, (a,b) => a.Tick == b.Tick && a.Message == b.Message && a.UnitId == b.UnitId && a.StationId == b.StationId && a.Error == b.Error).All(x => x), "event determinism " + i);
            }
            var pressTest = new Simulation(layout, new Recording[0], false); var press = pressTest.Station(2); press.Input = 4;
            for (int i = 0; i < 401; i++) pressTest.Step();
            Check(press.Output == 4 && press.Input == 0 && press.ProcessEnd == 0, "buffered machine processes backlog");
            press.Input = 2; for (int i = 0; i < 100; i++) pressTest.Step();
            Check(press.Output == 4 && press.Input == 2, "full output pauses without consuming ore");
            press.Output--; pressTest.Step(); Check(press.Input == 1 && press.ReservedOutput, "output slot reserved before production");
            for (int i = 0; i < 80; i++) pressTest.Step();
            Check(press.Output == 4 && press.Input == 1, "reserved product committed exactly once");
            var path = Pathfinder.Find(layout, Rules.Spawn, layout.Stations[1].Port);
            var commands = new List<Command> { new Command { Tick = 0, Kind = CommandKind.Move, Path = path }, new Command { Tick = 80, Kind = CommandKind.Pickup, TargetId = 2, Material = Material.Plate, TimeoutTicks = 40 } };
            var conflict = new Simulation(layout, new[] { new Recording { Id = 2, Commands = commands }, new Recording { Id = 1, Commands = commands } }, false); conflict.Station(2).Output = 1; conflict.RunToEnd();
            Check(conflict.Units.Find(u => u.Id == 1).CargoCount == 1 && conflict.Units.Find(u => u.Id == 2).CargoCount == 0, "one plate assigned to older Echo regardless input order");
            Check(conflict.ErrorCount == 1 && conflict.Events.Any(e => e.Error && e.StationId == 2), "conflict diagnosed");
            var disabled = echo.Copy(); disabled.Enabled = false; var none = new Simulation(layout, new[] { disabled }, false); none.RunToEnd(); Check(none.Shipped == 0, "disabled Echo");
            var campaign = new Campaign(); campaign.Echoes.Add(echo); campaign.NextEchoId = 2;
            var game = new GameSession(campaign); int credits = campaign.Credits;
            Check(game.Verify().Passed && game.Data.Credits == credits, "verification without operator or payout");
            Check(game.Start(true), "certified autoloop starts");
            for (int i = 0; i < Rules.ShiftTicks; i++) game.Tick();
            Check(game.Data.Credits == credits + 60 && game.Data.Shift == 2, "one autoloop payout"); game.Abort();
            Check(game.Data.Credits == credits + 60, "partial autoloop abort no payout");
            Check(game.Upgrade(2, UpgradeKind.InputCapacity) && game.Certificate == null, "upgrade invalidates certificate");
            Check(game.Undo() && game.Data.Layout.Stations.Find(s => s.Id == 2).InputCapacity == 4, "planning undo");
            var oldMove = game.Data.Echoes[0].Profile.MoveTicks;
            Check(game.Upgrade(0, UpgradeKind.MoveSpeed) && game.Data.Echoes[0].Profile.MoveTicks == oldMove, "old profile preserved");
            game.Undo();
            Check(!game.Place(2, Rules.Spawn), "cannot cover spawn");
            Check(game.Place(2, new Cell(6, 5)), "move press");
            Check(!game.Verify().Passed, "moved station does not silently fix old route");
            game.Undo(); Check(game.Verify().Passed, "original layout restores replay");
            Check(game.Replace(1) && game.Start(), "replace trial"); game.Simulation.RunToEnd(); game.Tick(); game.Discard();
            Check(game.Data.Echoes[0].Commands.Count == echo.Commands.Count && game.Data.Echoes[0].Enabled, "discard replacement preserves original");
            var economy = new GameSession(); economy.Start(); Produce(economy.Simulation);
            while (economy.Phase == GamePhase.Recording) economy.Tick(); int paid = economy.Data.Credits;
            for (int i = 0; i < 20; i++) economy.Tick();
            Check(paid == 220 && economy.Data.Credits == paid, "summary settlement idempotent");
            economy.Discard(); Check(economy.Data.Credits == paid && economy.Data.Echoes.Count == 0, "discard keeps money only");
            economy.Start(); Produce(economy.Simulation); economy.Abort(); Check(economy.Data.Credits == paid, "abort forfeits partial result");
            var bufferLayout = Layout.Default(); bufferLayout.Stations.Add(new StationSpec { Id = 4, Kind = StationKind.Buffer, BufferMaterial = Material.Ore, Position = new Cell(4, 6) }); bufferLayout.NextStationId = 5;
            var bufferSim = new Simulation(bufferLayout, new Recording[0], true);
            Action(bufferSim, 1, CommandKind.Pickup, Material.Ore); Action(bufferSim, 4, CommandKind.Deposit, Material.Ore);
            Check(bufferSim.Station(4).Output == 1 && bufferSim.Operator.CargoCount == 0, "buffer deposit");
            Action(bufferSim, 4, CommandKind.Pickup, Material.Ore); Check(bufferSim.Station(4).Output == 0 && bufferSim.Operator.CargoCount == 1, "buffer withdrawal");
            var boundary = new Simulation(layout, new[] { new Recording { Id = 1, Commands = new List<Command> { new Command { Tick = 1199, Kind = CommandKind.Move, Path = new List<Cell> { new Cell(2,5) } } } } }, false); boundary.RunToEnd(); Check(boundary.HasUnfinished, "end boundary flags unfinished");
            var emptyGame = new GameSession(); Check(!emptyGame.Verify().Passed, "empty factory cannot certify");
            var full = new Simulation(layout, new Recording[0], true); Action(full, 1, CommandKind.Pickup, Material.Ore);
            while(full.Tick < 1190) full.Step(); int history = full.Operator.Commands.Count;
            Check(!full.EnqueueAction(3, CommandKind.Deposit, Material.Plate, 1, out why) && full.Operator.Commands.Count == history, "late request does not destroy recording");
            return "PASS: " + checks + " assertions; 1000 deterministic replays; buffers, atomic service, conflict, upgrades, placement, autonomy, recording and payouts.";
        }
    }
}
