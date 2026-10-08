using System;
using System.Collections.Generic;

namespace EchoFactory.Core
{
    [Serializable] public sealed class ResourceAmount { public ResourceKind Kind; public int Amount; }
    [Serializable] public sealed class MachineReportData { public int MachineId, FacilityId; public MachineKind Kind; public IdleReason Reason; }

    // JsonUtility-friendly snapshot of StrategicState (dictionaries become lists of pairs).
    [Serializable]
    public sealed class StrategicSave
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public int Turn, Credits, NextFacilityId, NextMachineId;
        public StrategicPhase Phase;
        public int LastTurnIncome, LastTurnCosts, LastTurnProduction, LastPassiveIncome, LastResearchSpent;
        public GameOutcome Outcome;
        public int DebtTurns;
        public List<ParcelState> Parcels = new List<ParcelState>();
        public List<FacilityState> Facilities = new List<FacilityState>();
        public List<TechnologyState> Technologies = new List<TechnologyState>();
        public List<ResourceAmount> Stock = new List<ResourceAmount>();
        public List<ResourceAmount> LastExtracted = new List<ResourceAmount>();
        public List<ResourceAmount> MarketSupply = new List<ResourceAmount>();
        public List<MachineReportData> LastMachineReports = new List<MachineReportData>();

        public static StrategicSave From(StrategicState s)
        {
            if (s == null) throw new ArgumentNullException("s");
            var c = s.Copy();
            var d = new StrategicSave
            {
                Turn = c.Turn, Credits = c.Credits, NextFacilityId = c.NextFacilityId, NextMachineId = c.NextMachineId, Phase = c.Phase,
                LastTurnIncome = c.LastTurnIncome, LastTurnCosts = c.LastTurnCosts, LastTurnProduction = c.LastTurnProduction,
                LastPassiveIncome = c.LastPassiveIncome, LastResearchSpent = c.LastResearchSpent, Outcome = c.Outcome, DebtTurns = c.DebtTurns,
                Parcels = c.Parcels, Facilities = c.Facilities, Technologies = c.Technologies
            };
            Pack(c.Stock, d.Stock); Pack(c.LastExtracted, d.LastExtracted); Pack(c.MarketSupply, d.MarketSupply);
            for (int i = 0; i < c.LastMachineReports.Count; i++)
            {
                var r = c.LastMachineReports[i];
                d.LastMachineReports.Add(new MachineReportData { MachineId = r.MachineId, FacilityId = r.FacilityId, Kind = r.Kind, Reason = r.Reason });
            }
            return d;
        }

        public bool Valid(out string reason)
        {
            reason = "";
            if (Version != CurrentVersion) { reason = "Nieznana wersja zapisu: " + Version + "."; return false; }
            if (Turn < 1 || NextFacilityId < 1 || NextMachineId < 1) { reason = "Nieprawidłowy licznik tury lub identyfikatorów."; return false; }
            if (Parcels == null || Parcels.Count == 0 || Facilities == null || Technologies == null) { reason = "Brakuje działek, hal lub technologii."; return false; }
            for (int i = 0; i < Facilities.Count; i++)
            {
                var f = Facilities[i];
                if (f == null || f.Machines == null) { reason = "Uszkodzona hala."; return false; }
                if (f.ParcelId < 0 || f.ParcelId >= Parcels.Count) { reason = "Hala wskazuje nieistniejącą działkę."; return false; }
            }
            return true;
        }

        public StrategicState ToState()
        {
            string reason;
            if (!Valid(out reason)) throw new InvalidOperationException(reason);
            var s = new StrategicState
            {
                Turn = Turn, Credits = Credits, NextFacilityId = NextFacilityId, NextMachineId = NextMachineId, Phase = Phase,
                LastTurnIncome = LastTurnIncome, LastTurnCosts = LastTurnCosts, LastTurnProduction = LastTurnProduction,
                LastPassiveIncome = LastPassiveIncome, LastResearchSpent = LastResearchSpent, Outcome = Outcome, DebtTurns = DebtTurns
            };
            for (int i = 0; i < Parcels.Count; i++) s.Parcels.Add(Parcels[i].Copy());
            for (int i = 0; i < Facilities.Count; i++) s.Facilities.Add(Facilities[i].Copy());
            for (int i = 0; i < Technologies.Count; i++) s.Technologies.Add(Technologies[i].Copy());
            Unpack(Stock, s.Stock); Unpack(LastExtracted, s.LastExtracted);
            if (MarketSupply != null) for (int i = 0; i < MarketSupply.Count; i++) s.SetSupply(MarketSupply[i].Kind, MarketSupply[i].Amount);
            if (LastMachineReports != null)
                for (int i = 0; i < LastMachineReports.Count; i++)
                {
                    var r = LastMachineReports[i];
                    s.LastMachineReports.Add(new MachineReport { MachineId = r.MachineId, FacilityId = r.FacilityId, Kind = r.Kind, Reason = r.Reason });
                }
            return s;
        }

        static void Pack(Dictionary<ResourceKind, int> from, List<ResourceAmount> to)
        {
            foreach (var p in from) to.Add(new ResourceAmount { Kind = p.Key, Amount = p.Value });
        }

        static void Unpack(List<ResourceAmount> from, Dictionary<ResourceKind, int> to)
        {
            if (from == null) return;
            for (int i = 0; i < from.Count; i++) to[from[i].Kind] = from[i].Amount;
        }
    }
}
