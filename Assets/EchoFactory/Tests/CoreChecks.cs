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
            Check(sim.Operator.Errors == before, "operator action failed at station " + id + " (" + kind + ", quantity " + quantity + "): " + sim.Operator.Status);
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
        private static void PlacementChecks(Recording echo)
        {
            string why; List<int> affected;
            var campaign = new Campaign(); campaign.Echoes.Add(echo.Copy()); campaign.NextEchoId = 2;
            var game = new GameSession(campaign);
            Check(game.Verify().Passed, "preview setup certified"); var certificate = game.Certificate;
            int credits = game.Data.Credits, nextId = game.Data.Layout.NextStationId;
            var obstacle = new Cell(3, 4); // Recorded feeder -> press path, not a station or port.
            Check(echo.Commands.Any(c => c.Kind == CommandKind.Move && c.Path.Contains(obstacle)), "test obstacle crosses recorded path");
            Check(game.PreviewPlacement(0, obstacle, out why, out affected) && affected.SequenceEqual(new[] { 1 }), "new building detects route-only dependency");
            Check(game.Data.Credits == credits && game.Data.Layout.NextStationId == nextId && game.Data.Layout.Stations.Count == 3 && game.Certificate == certificate, "preview is read-only");
            Check(!game.Undo(), "preview does not add undo entry");
            Check(!game.PreviewPlacement(0, Rules.Spawn, out why, out affected) && affected.Count == 0, "invalid placement preview rejected");
            Check(!game.Place(0, Rules.Spawn) && game.Data.Credits == credits && game.Certificate == certificate, "invalid placement leaves state unchanged");
            Check(game.Place(2, game.Data.Layout.Stations[1].Position) && game.Certificate == certificate && !game.Undo(), "unchanged position is a no-op");
            Check(game.PreviewPlacement(2, new Cell(8, 5), out why, out affected) && affected.SequenceEqual(new[] { 1 }), "moved service target detected once");
            Check(game.PreviewPlacement(0, new Cell(12, 6), out why, out affected) && affected.Count == 0, "unrelated building does not warn about route");
            Check(game.Place(0, obstacle) && game.Data.Credits == credits - 40 && game.Certificate == null, "placement commits cost and invalidates certificate");
            Check(!game.Verify().Passed, "predicted obstacle breaks replay");
            Check(game.Undo() && game.Data.Credits == credits && game.Verify().Passed, "undo restores route and credits");
            Check(game.ToggleEcho(1), "disable recording for preview");
            Check(game.PreviewPlacement(0, obstacle, out why, out affected) && affected.SequenceEqual(new[] { 1 }), "disabled recordings included in warning");
            Check(game.Start() && !game.PreviewPlacement(0, obstacle, out why, out affected), "preview refuses changes during shift");
        }
        private static Recording PickupRequest(int id, int tick, int quantity, int timeout = 120)
        {
            return new Recording { Id = id, Profile = new OperatorProfile { Cargo = 4 }, Commands = new List<Command> {
                new Command { Tick = tick, Kind = CommandKind.Pickup, TargetId = 4, Material = Material.Ore, Quantity = quantity, TimeoutTicks = timeout }
            } };
        }
        private static void BatchAndQueueChecks()
        {
            var layout = Layout.Default(); layout.Operator.Cargo = 4;
            layout.Stations.Add(new StationSpec { Id = 4, Kind = StationKind.Buffer, BufferMaterial = Material.Ore, Position = new Cell(4, 6) }); layout.NextStationId = 5;
            var batch = new Simulation(layout, new Recording[0], true);
            Action(batch, 1, CommandKind.Pickup, Material.Ore, 3);
            Action(batch, 4, CommandKind.Deposit, Material.Ore, 3);
            Check(batch.Station(4).Output == 3 && batch.Operator.CargoCount == 0, "batch deposit conserves material");
            Action(batch, 4, CommandKind.Pickup, Material.Ore, 2);
            Check(batch.Station(4).Output == 1 && batch.Operator.CargoCount == 2, "partial batch withdrawal leaves remainder");
            Action(batch, 2, CommandKind.Deposit, Material.Ore, 2);
            while (batch.Station(2).Output < 2 && !batch.Finished) batch.Step();
            Action(batch, 2, CommandKind.Pickup, Material.Plate, 2);
            Action(batch, 3, CommandKind.Deposit, Material.Plate, 2);
            Check(batch.Shipped == 2 && batch.Operator.CargoCount == 0 && batch.Station(4).Output == 1, "batch production and dispatch conserve material");
            var replay = new Simulation(layout, new[] { Record(batch) }, false); replay.RunToEnd();
            Check(replay.Shipped == 2 && replay.ErrorCount == 0 && !replay.HasUnfinished && replay.Station(4).Output == 1, "batch semantic replay");

            var fifo = new Simulation(layout, new[] { PickupRequest(1, 4, 1), PickupRequest(3, 0, 1), PickupRequest(2, 3, 1) }, false);
            foreach (var unit in fifo.Units) unit.Position = fifo.Station(4).Spec.Port;
            fifo.Station(4).Output = 3; fifo.RunToEnd();
            Check(fifo.Events.Where(e => e.StationId == 4).Select(e => e.UnitId).SequenceEqual(new[] { 3, 2, 1 }), "queue uses arrival before echo age");
            Check(fifo.Units.All(u => u.CargoCount == 1) && fifo.ErrorCount == 0 && fifo.Station(4).Output == 0, "queue serves each item once");

            var waiting = new Simulation(layout, new[] { PickupRequest(1, 0, 2, 30), PickupRequest(2, 2, 1) }, false);
            foreach (var unit in waiting.Units) unit.Position = waiting.Station(4).Spec.Port;
            waiting.Station(4).Output = 1;
            waiting.Step();
            Check(waiting.Station(4).ServiceUnit == 0 && waiting.Station(4).Output == 1 && waiting.Units[0].CargoCount == 0, "unavailable batch never takes a partial amount or service lock");
            waiting.Step();
            Check(waiting.Station(4).ServiceUnit == 2, "feasible request bypasses unavailable batch");
            waiting.RunToEnd();
            Check(waiting.Units[0].Errors == 1 && waiting.Units[0].CargoCount == 0 && waiting.Units[1].CargoCount == 1 && waiting.Station(4).ServiceUnit == 0, "expired batch releases queue without duplicating stock");

            // A supplier recording feeds the press while a second recording delivers its output.
            var supplier = new Simulation(layout, new Recording[0], true);
            Action(supplier, 1, CommandKind.Pickup, Material.Ore, 3); Action(supplier, 2, CommandKind.Deposit, Material.Ore, 3);
            var supplyEcho = Record(supplier);
            var receiver = new Simulation(layout, new[] { supplyEcho }, true);
            // Time the receiver after the supplier; two plates need two production cycles.
            while (receiver.Tick < 200) receiver.Step();
            Action(receiver, 2, CommandKind.Pickup, Material.Plate, 2); Action(receiver, 3, CommandKind.Deposit, Material.Plate, 2);
            var receivingEcho = Record(receiver, 2);
            var game = new GameSession(new Campaign { Layout = layout, Echoes = new List<Recording> { supplyEcho, receivingEcho }, NextEchoId = 3 });
            Check(game.Verify().Passed && game.Certificate.Plates == 2, "supplier and receiver cooperate autonomously with batches");
        }
        private static void DiagnosticChecks()
        {
            var layout = Layout.Default();
            var blocked = new Recording { Id = 1, Commands = new List<Command> {
                new Command { Kind = CommandKind.Move, Path = new List<Cell> { new Cell(2, 5), new Cell(2, 4), new Cell(2, 3) } },
                new Command { Tick = 100, Kind = CommandKind.Move, Path = new List<Cell> { new Cell(3, 4) } }
            } };
            var sim = new Simulation(layout, new[] { blocked }, false);
            Check(sim.FirstProblem() == null, "no diagnosis before an actual error"); sim.RunToEnd();
            var first = sim.FirstProblem();
            Check(first != null && first.UnitId == 1 && first.CommandIndex == 0 && first.Tick == 31, "diagnosis identifies first failed command and tick");
            Check(first.Position == new Cell(2, 3) && sim.Units[0].Position == new Cell(3, 4), "diagnosis preserves blocked cell after unit moves away");
            Check(first.StationId == 0 && first.Context.Contains("komenda 1") && first.Context.Contains("2,3"), "movement diagnosis has cell context and human command numbering");
            int count = sim.Events.Count; ulong trace = sim.Trace;
            Check(sim.FirstProblem() == first && sim.Events.Count == count && sim.Trace == trace, "reading diagnostics does not change events or simulation");

            var missing = new Recording { Id = 2, Commands = new List<Command> {
                new Command { Kind = CommandKind.Move, Path = Pathfinder.Find(layout, Rules.Spawn, layout.Stations[1].Port) },
                new Command { Tick = 80, Kind = CommandKind.Pickup, TargetId = 2, Material = Material.Plate, TimeoutTicks = 5 }
            } };
            var game = new GameSession(new Campaign { Echoes = new List<Recording> { missing }, NextEchoId = 3 });
            var result = game.Verify(); first = result.FirstProblem;
            Check(!result.Passed && first != null && first.Tick == 86 && first.StationId == 2 && first.CommandIndex == 1, "verification returns material shortage context");
            Check(first.Message.Contains("magazynie wyjściowym") && result.Message.Contains(first.Context), "verification explains observed shortage");

            var late = new Recording { Id = 3, Commands = new List<Command> {
                new Command { Tick = Rules.ShiftTicks - 1, Kind = CommandKind.Move, Path = new List<Cell> { new Cell(2, 5) } }
            } };
            var boundary = new Simulation(layout, new[] { late }, false);
            while (boundary.Tick < Rules.ShiftTicks - 1) boundary.Step();
            Check(boundary.FirstProblem() == null, "ongoing movement is not prematurely a failure");
            boundary.RunToEnd(); first = boundary.FirstProblem(); count = boundary.Events.Count;
            Check(boundary.ErrorCount == 0 && first != null && first.UnitId == 3 && first.Position == new Cell(2, 5), "unfinished shift diagnosed even without error event");
            Check(first.Message.Contains("Koniec zmiany") && first.Tick == Rules.ShiftTicks, "unfinished diagnosis explains boundary");
            boundary.FirstProblem(); Check(boundary.Events.Count == count && boundary.ErrorCount == 0, "unfinished diagnosis does not fabricate logged errors");
            var multiple = new Simulation(layout, new[] { late, blocked }, false); multiple.RunToEnd();
            Check(multiple.FirstProblem().UnitId == 1, "earlier error takes precedence over unfinished command");
            var empty = new Simulation(layout, new Recording[0], false); empty.RunToEnd();
            Check(empty.FirstProblem() == null, "idle factory has no fabricated diagnosis");
            var emptyGame = new GameSession();
            Check(!emptyGame.Verify().Passed && emptyGame.Simulation.FirstProblem() == null, "no shipment can fail autonomy without unit error");
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
                Check(same.Events.Count == replay.Events.Count && same.Events.Zip(replay.Events, (a,b) => a.Tick == b.Tick && a.Message == b.Message && a.UnitId == b.UnitId && a.StationId == b.StationId && a.Error == b.Error && a.CommandIndex == b.CommandIndex && a.Position == b.Position).All(x => x), "event determinism " + i);
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
            PlacementChecks(echo); BatchAndQueueChecks(); DiagnosticChecks();
            return "PASS: " + checks + " assertions; 1000 deterministic replays; buffers, atomic service, conflict, upgrades, placement, autonomy, recording and payouts.";
        }
    }
}
