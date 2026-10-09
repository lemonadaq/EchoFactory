using System;
using System.Collections.Generic;

namespace EchoFactory.Core
{
    public static class ExtractionSystem
    {
        public const int RichnessPerUnit = 20;
        public const int GridEnergyPerFacilityParcel = 1;
        public const int LogisticsHallBaseBonus = 1;
        public const int LogisticsBonusPerUnit = 10;

        // Units of the parcel's primary resource mined per turn (richness 35..100 -> 1..5).
        public static int YieldPerTurn(ParcelState parcel)
        {
            if (parcel == null || !parcel.Owned || parcel.Resource == ResourceKind.None) return 0;
            return Math.Max(1, parcel.ResourceRichness / RichnessPerUnit);
        }

        // A Logistics Hall on a parcel adds transport capacity: +1 unit per turn, +1 more for every 10 points of parcel logistics bonus.
        public static int LogisticsYieldBonus(StrategicState state, ParcelState parcel)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (YieldPerTurn(parcel) <= 0 || !HasFacility(state, parcel.Id, FacilityKind.LogisticsHall)) return 0;
            return LogisticsHallBaseBonus + Math.Max(0, parcel.LogisticsBonus) / LogisticsBonusPerUnit;
        }

        // Owned parcels mine their primary resource; each owned parcel with an unlocked facility also draws grid energy.
        public static Dictionary<ResourceKind, int> Extract(StrategicState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            var mined = new Dictionary<ResourceKind, int>();
            for (int i = 0; i < state.Parcels.Count; i++)
            {
                var parcel = state.Parcels[i];
                int amount = YieldPerTurn(parcel) + LogisticsYieldBonus(state, parcel);
                if (amount > 0) Add(mined, parcel.Resource, amount);
                if (parcel.Owned && HasFacility(state, parcel.Id, FacilityKind.Empty)) Add(mined, ResourceKind.Energy, GridEnergyPerFacilityParcel);
            }
            foreach (var p in mined) state.AddStock(p.Key, p.Value);
            return mined;
        }

        static void Add(Dictionary<ResourceKind, int> d, ResourceKind k, int n) { int v; d.TryGetValue(k, out v); d[k] = v + n; }

        // kind == Empty matches any facility kind.
        static bool HasFacility(StrategicState state, int parcelId, FacilityKind kind)
        {
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                var f = state.Facilities[i];
                if (f.Unlocked && f.ParcelId == parcelId && (kind == FacilityKind.Empty || f.Kind == kind)) return true;
            }
            return false;
        }
    }
}
