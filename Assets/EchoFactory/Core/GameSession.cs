using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoFactory.Core
{
    public sealed class Verification
    {
        public bool Passed;
        public int Plates, Errors;
        public ulong Trace;
        public SimEvent FirstProblem;
        public string Message;
    }
    public sealed class GameSession
    {
        public Campaign Data { get; private set; }
        public Simulation Simulation { get; private set; }
        public GamePhase Phase { get; private set; }
        public Verification Certificate { get; private set; }
        public int ReplacingId { get; private set; }
        public string Message { get; private set; } = "Rozpocznij zmianę i naucz Echo swojej pracy.";
        public int LastEarnings { get; private set; }
        private readonly Stack<PlanningSnapshot> undo = new Stack<PlanningSnapshot>();
        private sealed class PlanningSnapshot { public Layout Layout; public int Credits; }
        public GameSession(Campaign campaign = null)
        {
            Data = campaign ?? new Campaign(); string reason;
            if (!Data.Valid(out reason)) throw new ArgumentException(reason);
            Simulation = new Simulation(Data.Layout, Data.Echoes, true);
        }
        private bool Reject(string message) { Message = message; return false; }
        public bool Start(bool autoloop = false)
        {
            if (Phase != GamePhase.Planning) return Reject("Najpierw zakończ bieżącą zmianę.");
            if (autoloop && (Certificate == null || !Certificate.Passed || ReplacingId != 0)) return Reject("Najpierw sprawdź autonomię.");
            Simulation = new Simulation(Data.Layout, Data.Echoes.Where(e => e.Id != ReplacingId), !autoloop);
            Phase = autoloop ? GamePhase.Autoloop : GamePhase.Recording; undo.Clear(); Message = autoloop ? "Fabryka pracuje samodzielnie." : "Nagrywanie rozpoczęte."; return true;
        }
        public void Tick()
        {
            if (Phase != GamePhase.Recording && Phase != GamePhase.Autoloop) return;
            Simulation.Step(); if (!Simulation.Finished) return;
            LastEarnings = Simulation.Shipped * Rules.PlatePrice;
            // Phase transition owns the payout. Repeated UI calls cannot settle it twice.
            checked { Data.Credits += LastEarnings; Data.Shift++; }
            if (Phase == GamePhase.Autoloop) Simulation = new Simulation(Data.Layout, Data.Echoes, false);
            else { Phase = GamePhase.Summary; Message = "Zmiana ukończona. Zapisz Echo albo odrzuć nagranie."; }
        }
        public void Abort()
        {
            if (Phase == GamePhase.Summary) { Discard(); return; }
            Phase = GamePhase.Planning; ReplacingId = 0;
            Simulation = new Simulation(Data.Layout, Data.Echoes, true); Message = "Zmiana przerwana bez wypłaty.";
        }
        public bool SaveEcho(string name)
        {
            if (Phase != GamePhase.Summary) return Reject("Nagranie można zapisać po ukończeniu zmiany.");
            var u = Simulation.Operator;
            if (u == null || u.Commands.Count == 0) return Reject("Brak nagranych czynności.");
            var old = Data.Echoes.Find(e => e.Id == ReplacingId);
            if (old == null && Data.Echoes.Count >= 100) return Reject("Archiwum osiągnęło limit 100 nagrań.");
            if (old == null && Data.Echoes.Count(e => e.Enabled) >= Rules.EchoLimit) return Reject("Wykorzystano 5 slotów. Odrzuć próbę, wyłącz Echo lub nagraj zastępstwo.");
            var recording = new Recording { Id = old == null ? Data.NextEchoId++ : old.Id, Enabled = old == null || old.Enabled, Name = string.IsNullOrWhiteSpace(name) ? "Echo" : name.Trim().Substring(0, Math.Min(28, name.Trim().Length)), Profile = u.Profile.Copy(), Commands = u.Commands.Select(c => c.Copy()).ToList() };
            if (old != null) Data.Echoes.Remove(old);
            Data.Echoes.Add(recording); Certificate = null; ReturnToPlan(); Message = "Echo zapisane. Rozpocznij kolejną zmianę."; return true;
        }
        public void Discard() { if (Phase != GamePhase.Summary) return; ReturnToPlan(); Message = "Nagranie odrzucone. Zarobek zachowany."; }
        private void ReturnToPlan() { Phase = GamePhase.Planning; ReplacingId = 0; Simulation = new Simulation(Data.Layout, Data.Echoes, true); }
        public bool Replace(int id)
        {
            if (Phase != GamePhase.Planning || !Data.Echoes.Any(e => e.Id == id)) return false;
            ReplacingId = ReplacingId == id ? 0 : id; Message = ReplacingId == 0 ? "Anulowano zastępowanie." : "Nagraj zastępstwo. Stare Echo nie weźmie udziału w tej próbie."; return true;
        }
        public bool ToggleEcho(int id)
        {
            if (Phase != GamePhase.Planning) return false;
            var e = Data.Echoes.Find(r => r.Id == id); if (e == null) return false;
            if (!e.Enabled && Data.Echoes.Count(r => r.Enabled) >= Rules.EchoLimit) return Reject("Limit 5 aktywnych Echo.");
            e.Enabled = !e.Enabled; Changed(); return true;
        }
        public bool UpdateEchoProfile(int id)
        {
            if (Phase != GamePhase.Planning) return false;
            var e = Data.Echoes.Find(r => r.Id == id); if (e == null) return false;
            e.Profile = Data.Layout.Operator.Copy(); e.Profile.Version++; Changed(); Message = "Profil zaktualizowany. Czasy komend pozostają bez zmian — sprawdź autonomię."; return true;
        }
        public Verification Verify()
        {
            if (Phase != GamePhase.Planning || ReplacingId != 0) return new Verification { Message = "Zakończ zmianę lub anuluj zastępowanie." };
            var a = new Simulation(Data.Layout, Data.Echoes, false); a.RunToEnd();
            var b = new Simulation(Data.Layout, Data.Echoes, false); b.RunToEnd();
            bool sameEvents = a.Events.Count == b.Events.Count && a.Events.Zip(b.Events, (x, y) => x.Tick == y.Tick && x.UnitId == y.UnitId && x.StationId == y.StationId && x.Error == y.Error && x.Message == y.Message && x.CommandIndex == y.CommandIndex && x.Position == y.Position).All(x => x);
            var result = new Verification { Passed = a.Shipped >= 1 && a.ErrorCount == 0 && !a.HasUnfinished && a.Trace == b.Trace && sameEvents, Plates = a.Shipped, Errors = a.ErrorCount, Trace = a.Trace, FirstProblem = a.FirstProblem() };
            result.Message = result.Passed ? "Autonomia potwierdzona w dwóch przebiegach." : "Test nieudany: " + a.Shipped + " płyt, " + a.ErrorCount + " błędów" + (a.HasUnfinished ? ", niedokończone akcje." : ".");
            if (result.FirstProblem != null) result.Message += " " + result.FirstProblem.Context + ": " + result.FirstProblem.Message;
            Certificate = result.Passed ? result : null; Simulation = a; Message = result.Message; return result;
        }
        private void Remember() { undo.Push(new PlanningSnapshot { Layout = Data.Layout.Copy(), Credits = Data.Credits }); }
        private void Changed() { Certificate = null; Simulation = new Simulation(Data.Layout, Data.Echoes, true); }
        public bool Undo()
        {
            if (Phase != GamePhase.Planning || undo.Count == 0) return Reject("Brak zmiany układu lub zakupu do cofnięcia.");
            var state = undo.Pop(); Data.Layout = state.Layout; Data.Credits = state.Credits; Changed(); return true;
        }
        public int Dependents(int id) { return Data.Echoes.Count(e => e.Commands.Any(c => c.TargetId == id)); }
        // Preview and commit share validation; preview never spends Credits or changes recordings.
        public bool PreviewPlacement(int id, Cell position, out string reason, out List<int> affected,
            StationKind kind = StationKind.Buffer, Material material = Material.Plate)
        {
            Layout proposed; int cost;
            bool valid = PreparePlacement(id, position, kind, material, out proposed, out cost, out reason);
            affected = new List<int>();
            if (!valid) return false;
            var old = Data.Layout.Stations.Find(s => s.Id == id);
            if (old != null && old.Position == position) { reason = "Budynek jest już na tym polu."; return true; }
            // Include disabled recordings, since enabling them later must not hide broken routes.
            affected = Data.Echoes.Where(e => e.Commands.Any(c =>
                (id != 0 && c.TargetId == id && c.Kind != CommandKind.Move) ||
                (c.Kind == CommandKind.Move && c.Path.Contains(position))))
                .OrderBy(e => e.Id).Select(e => e.Id).ToList();
            reason = affected.Count == 0 ? "Brak bezpośrednich kolizji z nagraniami. Po zmianie sprawdź autonomię."
                : "Sprawdź nagrania Echo: " + string.Join(", ", affected.Select(x => "#" + x)) +
                  ". Zmienia się cel obsługi lub budynek przecina zapisaną trasę.";
            return true;
        }
        private bool PreparePlacement(int id, Cell position, StationKind kind, Material material,
            out Layout layout, out int cost, out string reason)
        {
            layout = null; cost = 0; reason = "Budowanie jest dostępne podczas planowania.";
            if (Phase != GamePhase.Planning) return false;
            layout = Data.Layout.Copy(); var s = layout.Stations.Find(x => x.Id == id);
            if (id != 0 && s == null) { reason = "Nie znaleziono stacji."; return false; }
            if (s == null)
            {
                if (kind != StationKind.Buffer && kind != StationKind.Press) { reason = "Możesz dobudować prasę lub bufor."; return false; }
                cost = kind == StationKind.Press ? 120 : 40;
                if (Data.Credits < cost) { reason = "Brak Credits."; return false; }
                s = new StationSpec { Id = layout.NextStationId++, Kind = kind, BufferMaterial = material }; layout.Stations.Add(s);
            }
            s.Position = position;
            return layout.Valid(out reason);
        }
        public bool Place(int id, Cell position, StationKind kind = StationKind.Buffer, Material material = Material.Plate)
        {
            string why; List<int> affected;
            if (!PreviewPlacement(id, position, out why, out affected, kind, material)) return Reject(why);
            var old = Data.Layout.Stations.Find(s => s.Id == id);
            if (old != null && old.Position == position) { Message = why; return true; }
            Layout layout; int cost; string validation;
            if (!PreparePlacement(id, position, kind, material, out layout, out cost, out validation)) return Reject(validation);
            Remember(); Data.Layout = layout; Data.Credits -= cost; Changed();
            Message = "Układ zmieniony. " + why; return true;
        }
        public int UpgradeCost(int id, UpgradeKind kind)
        {
            if (kind == UpgradeKind.Cargo) return 60 * Data.Layout.Operator.Cargo;
            if (kind == UpgradeKind.MoveSpeed) return 60 + (10 - Data.Layout.Operator.MoveTicks) * 20;
            if (kind == UpgradeKind.HandlingSpeed) return 60 + (20 - Data.Layout.Operator.HandlingTicks) * 10;
            var s = Data.Layout.Stations.Find(x => x.Id == id); if (s == null) return int.MaxValue;
            if (kind == UpgradeKind.ProductionSpeed) return 60 + (80 - s.ProcessTicks) * 2;
            return 20 + (kind == UpgradeKind.InputCapacity ? s.InputCapacity : s.OutputCapacity) * 5;
        }
        public bool Upgrade(int id, UpgradeKind kind)
        {
            if (Phase != GamePhase.Planning) return false;
            var l = Data.Layout.Copy(); var s = l.Stations.Find(x => x.Id == id); var op = l.Operator;
            int cost = UpgradeCost(id, kind);
            if (Data.Credits < cost) return Reject("Brak Credits.");
            if (kind == UpgradeKind.Cargo) { if (op.Cargo >= 4) return Reject("Maksymalny udźwig: 4."); op.Cargo++; op.Version++; }
            else if (kind == UpgradeKind.MoveSpeed) { if (op.MoveTicks <= 4) return Reject("Osiągnięto limit szybkości."); op.MoveTicks -= 2; op.Version++; }
            else if (kind == UpgradeKind.HandlingSpeed) { if (op.HandlingTicks <= 4) return Reject("Osiągnięto limit obsługi."); op.HandlingTicks -= 4; op.Version++; }
            else if (s == null) return false;
            else if (kind == UpgradeKind.ProductionSpeed) { if (s.Kind != StationKind.Press || s.ProcessTicks <= 20) return Reject("Brak kolejnego poziomu."); s.ProcessTicks -= 20; }
            else if (kind == UpgradeKind.InputCapacity) { if (s.Kind != StationKind.Press || s.InputCapacity >= 16) return Reject("Brak kolejnego poziomu."); s.InputCapacity += 2; }
            else if (kind == UpgradeKind.OutputCapacity) { if (s.Kind == StationKind.Dispatch || s.OutputCapacity >= 16) return Reject("Brak kolejnego poziomu."); s.OutputCapacity += 2; }
            else return false;
            Remember(); Data.Layout = l; Data.Credits -= cost; Changed(); Message = "Ulepszenie kupione. Dawne Echo zachowują swoje profile."; return true;
        }
    }
}
