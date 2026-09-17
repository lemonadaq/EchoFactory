using System;

namespace EchoFactory.Core
{
    public static class ResearchSystem
    {
        public const int ResearchHallPrice = 7500;

        public static bool BuildResearchHall(StrategicState state, int parcelId)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.Phase != StrategicPhase.Planning) return false;
            if (state.Credits < ResearchHallPrice) return false;
            for (int i=0;i<state.Facilities.Count;i++) if (state.Facilities[i].Kind == FacilityKind.ResearchHall && state.Facilities[i].Unlocked) return false;
            if (!IsOwnedParcel(state, parcelId)) return false;
            state.Credits -= ResearchHallPrice;
            state.Facilities.Add(new FacilityState { Id=state.NextFacilityId++, Kind=FacilityKind.ResearchHall, ParcelId=parcelId, MachineSlots=0, PassiveIncomePerTurn=0 });
            return true;
        }

        public static bool UnlockTechnology(StrategicState state, TechnologyKind technology)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (state.Phase != StrategicPhase.Planning || !HasResearchHall(state)) return false;
            for (int i=0;i<state.Technologies.Count;i++)
            {
                var tech = state.Technologies[i];
                if (tech.Kind != technology || tech.Unlocked) return false;
                if (!PrerequisiteMet(state, technology)) return false;
                if (state.Credits < tech.ResearchCost) return false;
                state.Credits -= tech.ResearchCost;
                tech.Unlocked = true;
                state.LastResearchSpent = tech.ResearchCost;
                return true;
            }
            return false;
        }

        static bool HasResearchHall(StrategicState state)
        {
            for (int i=0;i<state.Facilities.Count;i++) if (state.Facilities[i].Kind == FacilityKind.ResearchHall && state.Facilities[i].Unlocked) return true;
            return false;
        }

        static bool IsOwnedParcel(StrategicState state, int id)
        {
            for (int i=0;i<state.Parcels.Count;i++) if (state.Parcels[i].Id == id) return state.Parcels[i].Owned;
            return false;
        }

        static bool PrerequisiteMet(StrategicState state, TechnologyKind tech)
        {
            if (tech == TechnologyKind.BasicAutomation) return true;
            if (tech == TechnologyKind.ImprovedPress) return state.HasTechnology(TechnologyKind.BasicAutomation);
            if (tech == TechnologyKind.AdvancedPress) return state.HasTechnology(TechnologyKind.ImprovedPress);
            if (tech == TechnologyKind.SmartLogistics) return state.HasTechnology(TechnologyKind.BasicAutomation);
            if (tech == TechnologyKind.EnergyEfficiency) return state.HasTechnology(TechnologyKind.BasicAutomation);
            if (tech == TechnologyKind.AdvancedMaterials) return state.HasTechnology(TechnologyKind.ImprovedPress) && state.HasTechnology(TechnologyKind.EnergyEfficiency);
            return false;
        }
    }
}
