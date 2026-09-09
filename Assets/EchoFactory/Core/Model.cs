using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoFactory.Core
{
    public static class Rules
    {
        public const int Version = 1, TickRate = 20, ShiftTicks = 1200, EchoLimit = 5, GraceTicks = 120;
        public const int Width = 14, Height = 9, PlatePrice = 20;
        public static readonly Cell Spawn = new Cell(2, 6);
    }

    [Serializable]
    public struct Cell : IEquatable<Cell>
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode() { return X * 397 ^ Y; }
        public static bool operator ==(Cell a, Cell b) { return a.Equals(b); }
        public static bool operator !=(Cell a, Cell b) { return !a.Equals(b); }
        public int Distance(Cell b) { return Math.Abs(X - b.X) + Math.Abs(Y - b.Y); }
    }

    public enum Material { None, Ore, Plate }
    public enum StationKind { Feeder, Press, Dispatch, Buffer }
    public enum CommandKind { Move, Pickup, Deposit }
    public enum UpgradeKind { InputCapacity, OutputCapacity, ProductionSpeed, Cargo, MoveSpeed, HandlingSpeed }
    public enum GamePhase { Planning, Recording, Summary, Autoloop }

    [Serializable]
    public sealed class OperatorProfile
    {
        public int Version = 1, Cargo = 1, MoveTicks = 10, HandlingTicks = 20;
        public OperatorProfile Copy() { return (OperatorProfile)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class StationSpec
    {
        public int Id, InputCapacity = 4, OutputCapacity = 4, ProcessTicks = 80;
        public StationKind Kind;
        public Material BufferMaterial = Material.Plate;
        public Cell Position;
        public Cell Port { get { return new Cell(Position.X, Position.Y + 1); } }
        public StationSpec Copy() { return (StationSpec)MemberwiseClone(); }
        public string Name { get { return Kind == StationKind.Feeder ? "Podajnik" : Kind == StationKind.Press ? "Prasa" : Kind == StationKind.Dispatch ? "Wysyłka" : "Bufor " + (BufferMaterial == Material.Ore ? "rudy" : "płyt"); } }
    }

    [Serializable]
    public sealed class Layout
    {
        public List<StationSpec> Stations = new List<StationSpec>();
        public int NextStationId = 4;
        public OperatorProfile Operator = new OperatorProfile();
        public static Layout Default()
        {
            var l = new Layout();
            l.Stations.Add(new StationSpec { Id = 1, Kind = StationKind.Feeder, Position = new Cell(2, 3) });
            l.Stations.Add(new StationSpec { Id = 2, Kind = StationKind.Press, Position = new Cell(6, 3) });
            l.Stations.Add(new StationSpec { Id = 3, Kind = StationKind.Dispatch, Position = new Cell(10, 3) });
            return l;
        }
        public Layout Copy() { return new Layout { Stations = Stations.Select(s => s.Copy()).ToList(), NextStationId = NextStationId, Operator = Operator.Copy() }; }
        public bool Walkable(Cell c) { return c.X >= 0 && c.Y >= 0 && c.X < Rules.Width && c.Y < Rules.Height && !Stations.Any(s => s.Position == c); }
        public bool Valid(out string reason)
        {
            reason = "";
            if (Stations == null || Operator == null || Stations.Count < 3 || Stations.Count > 20) { reason = "Nieprawidłowa lista stacji."; return false; }
            if (Stations.Select(s => s.Id).Distinct().Count() != Stations.Count || Stations.Any(s => s.Id <= 0) || NextStationId <= Stations.Max(s => s.Id)) { reason = "Nieprawidłowe ID stacji."; return false; }
            if (!Stations.Any(s => s.Kind == StationKind.Feeder) || !Stations.Any(s => s.Kind == StationKind.Press) || !Stations.Any(s => s.Kind == StationKind.Dispatch)) { reason = "Brak podstawowej linii."; return false; }
            if (Operator.Cargo < 1 || Operator.Cargo > 4 || Operator.MoveTicks < 4 || Operator.MoveTicks > 20 || Operator.HandlingTicks < 4 || Operator.HandlingTicks > 20) { reason = "Nieprawidłowy profil."; return false; }
            foreach (var s in Stations)
            {
                if (!Enum.IsDefined(typeof(StationKind), s.Kind) || s.InputCapacity < 1 || s.InputCapacity > 64 || s.OutputCapacity < 1 || s.OutputCapacity > 64 || s.ProcessTicks < 20 || s.ProcessTicks > 80) { reason = "Nieprawidłowe parametry stacji."; return false; }
                if (s.Kind == StationKind.Buffer && s.BufferMaterial != Material.Ore && s.BufferMaterial != Material.Plate) { reason = "Nieprawidłowy materiał bufora."; return false; }
                if (s.Position.X < 0 || s.Position.X >= Rules.Width || s.Position.Y < 0 || s.Position.Y >= Rules.Height - 1 || s.Position == Rules.Spawn || !Walkable(s.Port)) { reason = "Zablokowane wejście lub pole poza halą."; return false; }
                if (Stations.Any(o => o.Id != s.Id && (o.Position == s.Position || o.Port == s.Port))) { reason = "Stacje nachodzą na siebie."; return false; }
            }
            if (!Walkable(Rules.Spawn) || Stations.Any(s => Pathfinder.Find(this, Rules.Spawn, s.Port) == null)) { reason = "Nie ma dojścia do wszystkich stanowisk."; return false; }
            return true;
        }
    }

    [Serializable]
    public sealed class Command
    {
        public int Tick, TargetId, Quantity = 1, TimeoutTicks = Rules.GraceTicks;
        public CommandKind Kind;
        public Material Material;
        public List<Cell> Path = new List<Cell>();
        public Command Copy() { var c = (Command)MemberwiseClone(); c.Path = new List<Cell>(Path); return c; }
    }

    [Serializable]
    public sealed class Recording
    {
        public int Id;
        public string Name = "Echo";
        public bool Enabled = true;
        public OperatorProfile Profile = new OperatorProfile();
        public List<Command> Commands = new List<Command>();
        public Recording Copy() { return new Recording { Id = Id, Name = Name, Enabled = Enabled, Profile = Profile.Copy(), Commands = Commands.Select(c => c.Copy()).ToList() }; }
    }

    [Serializable]
    public sealed class Campaign
    {
        public int SchemaVersion = 1, SimulationVersion = Rules.Version, Credits = 200, Shift = 1, NextEchoId = 1;
        public Layout Layout = Layout.Default();
        public List<Recording> Echoes = new List<Recording>();
        public bool Valid(out string reason)
        {
            reason = "Nieobsługiwana lub uszkodzona wersja zapisu.";
            if (SchemaVersion != 1 || SimulationVersion != Rules.Version || Layout == null || !Layout.Valid(out reason) || Credits < 0 || Shift < 1 || Echoes == null || Echoes.Count > 100) return false;
            if (Echoes.Count(e => e.Enabled) > Rules.EchoLimit || Echoes.Select(e => e.Id).Distinct().Count() != Echoes.Count || NextEchoId <= Echoes.Select(e => e.Id).DefaultIfEmpty(0).Max()) return false;
            foreach (var e in Echoes)
            {
                if (e.Id <= 0 || e.Id == int.MaxValue || e.Profile == null || e.Commands == null || e.Commands.Count > 2400 || e.Profile.Cargo < 1 || e.Profile.Cargo > 4 || e.Profile.MoveTicks < 4 || e.Profile.MoveTicks > 20 || e.Profile.HandlingTicks < 4 || e.Profile.HandlingTicks > 20) return false;
                int tick = -1;
                foreach (var c in e.Commands)
                {
                    if (c == null || c.Path == null || c.Tick < tick || c.Tick >= Rules.ShiftTicks || c.TimeoutTicks < 0 || c.TimeoutTicks > 1200 || !Enum.IsDefined(typeof(CommandKind), c.Kind) || !Enum.IsDefined(typeof(Material), c.Material) || c.Quantity < 1 || c.Quantity > 4 || c.Path.Count > Rules.Width * Rules.Height) return false;
                    tick = c.Tick;
                }
            }
            reason = ""; return true;
        }
    }

    public static class Pathfinder
    {
        private static readonly Cell[] Directions = { new Cell(0, -1), new Cell(-1, 0), new Cell(1, 0), new Cell(0, 1) };
        public static List<Cell> Find(Layout layout, Cell start, Cell goal)
        {
            if (!layout.Walkable(start) || !layout.Walkable(goal)) return null;
            var queue = new Queue<Cell>(); var parent = new Dictionary<Cell, Cell>();
            queue.Enqueue(start); parent[start] = start;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c == goal) { var path = new List<Cell>(); while (c != start) { path.Add(c); c = parent[c]; } path.Reverse(); return path; }
                foreach (var d in Directions) { var n = new Cell(c.X + d.X, c.Y + d.Y); if (layout.Walkable(n) && !parent.ContainsKey(n)) { parent[n] = c; queue.Enqueue(n); } }
            }
            return null;
        }
    }
}
