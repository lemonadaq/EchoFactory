using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoFactory.Core
{
    public sealed class StationState
    {
        public StationSpec Spec;
        public int Input, Output, ProcessEnd, NextSupply = 80, ServiceUnit;
        public bool ReservedOutput;
        public StationState(StationSpec spec) { Spec = spec.Copy(); Output = spec.Kind == StationKind.Feeder ? Math.Min(3, spec.OutputCapacity) : 0; }
    }
    public sealed class UnitState
    {
        public int Id, Index, CargoCount, EndTick, PathIndex, WaitSince = -1, Errors, Completed;
        public bool IsOperator, Moving, Servicing;
        public Material Cargo;
        public Cell Position = Rules.Spawn, PreviousPosition = Rules.Spawn;
        public OperatorProfile Profile;
        public List<Command> Commands;
        public string Status = "Gotowość";
        public Command Current { get { return Index < Commands.Count ? Commands[Index] : null; } }
        public bool Idle { get { return Current == null && !Moving && !Servicing; } }
    }
    public sealed class SimEvent
    {
        public int Tick, UnitId, StationId;
        public bool Error;
        public string Message;
    }
    public sealed class Simulation
    {
        public readonly Layout Layout;
        public readonly List<UnitState> Units = new List<UnitState>();
        public readonly List<StationState> Stations;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public int Tick { get; private set; }
        public int Shipped { get; private set; }
        public ulong Trace { get; private set; } = 14695981039346656037UL;
        public bool Finished { get { return Tick >= Rules.ShiftTicks; } }
        public UnitState Operator { get { return Units.Find(u => u.IsOperator); } }
        public int ErrorCount { get { return Units.Sum(u => u.Errors); } }
        public bool HasUnfinished { get { return Units.Any(u => !u.Idle); } }
        public Simulation(Layout layout, IEnumerable<Recording> recordings, bool withOperator)
        {
            Layout = layout.Copy(); Stations = Layout.Stations.OrderBy(s => s.Id).Select(s => new StationState(s)).ToList();
            foreach (var e in recordings.Where(e => e.Enabled).OrderBy(e => e.Id)) Units.Add(new UnitState { Id = e.Id, Profile = e.Profile.Copy(), Commands = e.Commands.Select(c => c.Copy()).ToList() });
            if (withOperator) Units.Add(new UnitState { Id = int.MaxValue, IsOperator = true, Profile = layout.Operator.Copy(), Commands = new List<Command>() });
        }
        public StationState Station(int id) { return Stations.Find(s => s.Spec.Id == id); }
        public bool EnqueueMove(Cell goal, out string reason)
        {
            reason = "Dokończ bieżącą czynność.";
            if (Finished || Operator == null || !Operator.Idle) return false;
            var path = Pathfinder.Find(Layout, Operator.Position, goal);
            if (path == null) { reason = "Brak drogi."; return false; }
            if (path.Count > 0) Operator.Commands.Add(new Command { Tick = Tick, Kind = CommandKind.Move, Path = path });
            reason = ""; return true;
        }
        public bool EnqueueAction(int stationId, CommandKind kind, Material material, int quantity, out string reason)
        {
            reason = "Dokończ bieżącą czynność.";
            if (Finished || Operator == null || !Operator.Idle) return false;
            var s = Station(stationId); if (s == null || kind == CommandKind.Move || quantity < 1 || quantity > Operator.Profile.Cargo) { reason = "Nieprawidłowa akcja."; return false; }
            var path = Pathfinder.Find(Layout, Operator.Position, s.Spec.Port);
            if (path == null) { reason = "Brak drogi do stacji."; return false; }
            int at = Tick;
            if (Tick + path.Count * Operator.Profile.MoveTicks >= Rules.ShiftTicks) { reason = "Za mało czasu na dojście."; return false; }
            if (path.Count > 0) { Operator.Commands.Add(new Command { Tick = at, Kind = CommandKind.Move, Path = path }); at += path.Count * Operator.Profile.MoveTicks; }
            Operator.Commands.Add(new Command { Tick = at, TargetId = stationId, Kind = kind, Material = material, Quantity = quantity });
            reason = ""; return true;
        }
        public void Step()
        {
            if (Finished) return;
            Tick++;
            // 1. Complete existing processes. Their output slot was reserved on start.
            foreach (var s in Stations)
            {
                if (s.ProcessEnd > 0 && s.ProcessEnd <= Tick) { s.Output++; s.ProcessEnd = 0; s.ReservedOutput = false; Log(null, s.Spec.Id, "Wyprodukowano płytę"); }
                if (s.Spec.Kind == StationKind.Feeder && Tick >= s.NextSupply) { s.Output = Math.Min(s.Spec.OutputCapacity, s.Output + 1); s.NextSupply += 80; }
            }
            // 2. Complete movement and services; older Echo resolve before the Operator.
            foreach (var u in Units)
            {
                var c = u.Current; if (c == null) continue;
                if (u.Moving && Tick >= u.EndTick)
                {
                    var target = c.Path[u.PathIndex];
                    if (!Layout.Walkable(target) || u.Position.Distance(target) != 1) { Fail(u, "Trasa jest zablokowana lub nieciągła"); continue; }
                    u.PreviousPosition = u.Position; u.Position = target; u.PathIndex++;
                    if (u.PathIndex == c.Path.Count) { u.Moving = false; Complete(u); }
                    else { u.PreviousPosition = u.Position; u.EndTick += u.Profile.MoveTicks; }
                }
                else if (u.Servicing && Tick >= u.EndTick)
                {
                    var s = Station(c.TargetId); string reason;
                    if (s == null || !CanTransfer(u, s, c, out reason)) { if (s != null) s.ServiceUnit = 0; Fail(u, "Warunki transferu zmieniły się"); continue; }
                    Transfer(u, s, c); s.ServiceUnit = 0; u.Servicing = false; Complete(u);
                }
            }
            // 3. Ready commands join logical queues; bodies never block each other.
            foreach (var u in Units)
            {
                var c = u.Current; if (c == null || c.Tick > Tick || u.Moving || u.Servicing) continue;
                if (Tick > c.Tick + c.TimeoutTicks) { Fail(u, u.Status == "Gotowość" ? "Przekroczono czas akcji" : u.Status); continue; }
                if (c.Kind == CommandKind.Move)
                {
                    if (c.Path.Count == 0) { Complete(u); continue; }
                    u.Moving = true; u.PathIndex = 0; u.PreviousPosition = u.Position; u.EndTick = Tick + u.Profile.MoveTicks; u.Status = "Ruch";
                }
                else
                {
                    if (u.WaitSince < 0) u.WaitSince = Tick;
                    var s = Station(c.TargetId); string why = "Brak stacji";
                    if (s != null) CanTransfer(u, s, c, out why);
                    u.Status = string.IsNullOrEmpty(why) ? "Kolejka do obsługi" : why;
                }
            }
            // 4. First feasible request in arrival order gets the single service position.
            foreach (var s in Stations)
            {
                if (s.ServiceUnit != 0) continue;
                foreach (var u in Units.Where(u => u.Current != null && u.Current.Kind != CommandKind.Move && u.Current.TargetId == s.Spec.Id && u.WaitSince >= 0 && !u.Servicing && !u.Moving).OrderBy(u => u.WaitSince).ThenBy(u => u.Id))
                {
                    string why; if (!CanTransfer(u, s, u.Current, out why)) continue;
                    s.ServiceUnit = u.Id; u.Servicing = true; u.EndTick = Tick + u.Profile.HandlingTicks; u.Status = "Obsługa"; break;
                }
            }
            // 5. New machine cycles consume an input and reserve an output slot now.
            foreach (var s in Stations)
                if (s.Spec.Kind == StationKind.Press && s.ProcessEnd == 0 && s.Input > 0 && s.Output < s.Spec.OutputCapacity)
                { s.Input--; s.ReservedOutput = true; s.ProcessEnd = Tick + s.Spec.ProcessTicks; }
            HashState();
        }
        private bool CanTransfer(UnitState u, StationState s, Command c, out string why)
        {
            why = "";
            if (u.Position != s.Spec.Port) { why = "Poza polem obsługi — nagraj trasę ponownie"; return false; }
            if (c.Kind == CommandKind.Pickup)
            {
                Material output = s.Spec.Kind == StationKind.Feeder ? Material.Ore : s.Spec.Kind == StationKind.Press ? Material.Plate : s.Spec.Kind == StationKind.Buffer ? s.Spec.BufferMaterial : Material.None;
                if (output == Material.None || c.Material != output) why = "Nieprawidłowy materiał wyjściowy";
                else if ((u.CargoCount > 0 && u.Cargo != c.Material) || u.CargoCount + c.Quantity > u.Profile.Cargo) why = "Brak miejsca w rękach";
                else if (s.Output < c.Quantity) why = "Brak " + c.Quantity + " szt. w magazynie wyjściowym";
            }
            else
            {
                Material input = s.Spec.Kind == StationKind.Press ? Material.Ore : s.Spec.Kind == StationKind.Dispatch ? Material.Plate : s.Spec.Kind == StationKind.Buffer ? s.Spec.BufferMaterial : Material.None;
                if (input == Material.None || input != c.Material) why = "Stacja nie przyjmuje tego materiału";
                else if (u.Cargo != c.Material || u.CargoCount < c.Quantity) why = "Brak materiału w rękach";
                else if (s.Spec.Kind == StationKind.Press && s.Input + c.Quantity > s.Spec.InputCapacity) why = "Pełny magazyn wejściowy";
                else if (s.Spec.Kind == StationKind.Buffer && s.Output + c.Quantity > s.Spec.OutputCapacity) why = "Pełny bufor";
            }
            return why.Length == 0;
        }
        private void Transfer(UnitState u, StationState s, Command c)
        {
            if (c.Kind == CommandKind.Pickup) { s.Output -= c.Quantity; u.Cargo = c.Material; u.CargoCount += c.Quantity; }
            else
            {
                u.CargoCount -= c.Quantity; if (u.CargoCount == 0) u.Cargo = Material.None;
                if (s.Spec.Kind == StationKind.Dispatch) Shipped += c.Quantity;
                else if (s.Spec.Kind == StationKind.Buffer) s.Output += c.Quantity;
                else s.Input += c.Quantity;
            }
            Log(u, s.Spec.Id, (c.Kind == CommandKind.Pickup ? "Pobrano " : "Odłożono ") + c.Quantity + " × " + c.Material);
        }
        private void Complete(UnitState u) { u.Completed++; u.Index++; u.WaitSince = -1; u.Status = "Gotowość"; }
        private void Fail(UnitState u, string reason)
        {
            u.Errors++; Log(u, u.Current == null ? 0 : u.Current.TargetId, reason, true);
            u.Moving = u.Servicing = false; u.Index++; u.WaitSince = -1; u.Status = "Błąd: " + reason;
        }
        private void Log(UnitState u, int station, string message, bool error = false) { Events.Add(new SimEvent { Tick = Tick, UnitId = u == null ? 0 : u.Id, StationId = station, Message = message, Error = error }); }
        private void Mix(int value) { unchecked { Trace ^= (uint)value; Trace *= 1099511628211UL; } }
        private void HashState()
        {
            Mix(Tick); Mix(Shipped);
            foreach (var s in Stations) { Mix(s.Spec.Id); Mix(s.Input); Mix(s.Output); Mix(s.ProcessEnd); Mix(s.ReservedOutput ? 1 : 0); Mix(s.NextSupply); Mix(s.ServiceUnit); }
            foreach (var u in Units) { Mix(u.Id); Mix(u.Position.X); Mix(u.Position.Y); Mix((int)u.Cargo); Mix(u.CargoCount); Mix(u.Index); Mix(u.EndTick); Mix(u.PathIndex); Mix(u.WaitSince); Mix(u.Errors); Mix(u.Moving ? 1 : 0); Mix(u.Servicing ? 1 : 0); }
        }
        public void RunToEnd() { while (!Finished) Step(); }
    }
}
