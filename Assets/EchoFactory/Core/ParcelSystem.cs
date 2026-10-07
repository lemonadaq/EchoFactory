using System;

namespace EchoFactory.Core
{
    public static class ParcelSystem
    {
        public static ParcelState Find(StrategicState state, int parcelId)
        {
            if (state == null) return null;
            for (int i = 0; i < state.Parcels.Count; i++) if (state.Parcels[i].Id == parcelId) return state.Parcels[i];
            return null;
        }

        public static bool CanBuy(StrategicState state, int parcelId)
        {
            if (state == null) throw new ArgumentNullException("state");
            var parcel = Find(state, parcelId);
            return state.Phase == StrategicPhase.Planning && parcel != null && !parcel.Owned && state.Credits >= parcel.Price;
        }

        public static bool Buy(StrategicState state, int parcelId)
        {
            if (!CanBuy(state, parcelId)) return false;
            var parcel = Find(state, parcelId);
            state.Credits -= parcel.Price;
            parcel.Owned = true;
            return true;
        }
    }
}
