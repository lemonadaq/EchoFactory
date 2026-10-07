using System;
using System.Collections.Generic;

namespace EchoFactory.Core
{
    public static class ExtractionSystem
    {
        public const int RichnessPerUnit = 20;
        public const int GridEnergyPerFacilityParcel = 1;

        // Units of the parcel's primary resource mined per turn (richness 35..100 -> 1..5).
        public static int YieldPerTurn(ParcelState parcel)
        {
            if (parcel == null || !parcel.Owned || parcel.Resource == ResourceKind.None) return 0;
            return Math.Max(1, parcel.ResourceRichness / RichnessPerUnit);
        }

        // Owned parcels mine their primary resource; each owned parcel with an unlocked facility also draws grid energy.
        public static Dictionary<ResourceKind, int> Extract(StrategicState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            var mined = new Dictionary<ResourceKind, int>();
            for (int i = 0; i < state.Parcels.Count; i++)
            {
                var parcel = state.Parcels[i];
                int amount = YieldPerTurn(parcel);
                if (amount > 0) Add(mined, parcel.Resource, amount);
                if (parcel.Owned && HasFacility(state, parcel.Id)) Add(mined, ResourceKind.Energy, GridEnergyPerFacilityParcel);
            }
            foreach (var p in mined) state.AddStock(p.Key, p.Value);
            return mined;
        }

        static void Add(Dictionary<ResourceKind, int> d, ResourceKind k, int n) { int v; d.TryGetValue(k, out v); d[k] = v + n; }

        static bool HasFacility(StrategicState state, int parcelId)
        {
            for (int i = 0; i < state.Facilities.Count; i++) if (state.Facilities[i].Unlocked && state.Facilities[i].ParcelId == parcelId) return true;
            return false;
        }
    }
}
