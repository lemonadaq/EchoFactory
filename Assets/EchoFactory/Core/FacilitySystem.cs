using System;

namespace EchoFactory.Core
{
    public static class FacilitySystem
    {
        public static int GetPrice(FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.ProductionHall: return 6000;
                case FacilityKind.LogisticsHall: return 8000;
                case FacilityKind.EnergyHall: return 7000;
                case FacilityKind.Workshop: return 6500;
                default: return 0;
            }
        }

        public static int GetSlots(FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.ProductionHall: return 3;
                case FacilityKind.LogisticsHall: return 2;
                case FacilityKind.EnergyHall: return 2;
                case FacilityKind.Workshop: return 2;
                default: return 0;
            }
        }

        public static bool CanBuild(StrategicState state, int parcelId, FacilityKind kind)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.Phase != StrategicPhase.Planning || kind == FacilityKind.Empty || kind == FacilityKind.ResearchHall) return false;
            if (GetPrice(kind) <= 0 || state.Credits < GetPrice(kind)) return false;
            ParcelState parcel = null;
            for (int i = 0; i < state.Parcels.Count; i++) if (state.Parcels[i].Id == parcelId) parcel = state.Parcels[i];
            if (parcel == null || !parcel.Owned) return false;
            for (int i = 0; i < state.Facilities.Count; i++)
                if (state.Facilities[i].ParcelId == parcelId && state.Facilities[i].Kind == kind && state.Facilities[i].Unlocked) return false;
            return true;
        }

        public static bool Build(StrategicState state, int parcelId, FacilityKind kind)
        {
            if (!CanBuild(state, parcelId, kind)) return false;
            state.Credits -= GetPrice(kind);
            state.Facilities.Add(new FacilityState
            {
                Id = state.NextFacilityId++, Kind = kind, ParcelId = parcelId,
                MachineSlots = GetSlots(kind), PassiveIncomePerTurn = 0, Unlocked = true
            });
            return true;
        }
    }
}
