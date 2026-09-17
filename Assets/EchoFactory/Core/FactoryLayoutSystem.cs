using System;
using UnityEngine;

namespace EchoFactory.Core
{
    public sealed class FactoryNode
    {
        public int MachineId;
        public float X;
        public float Y;
        public bool Connected;
    }

    public static class FactoryLayoutSystem
    {
        public static bool CanPlaceMachine(StrategicState state, int facilityId, MachineKind kind, float x, float y)
        {
            if (!ProductionSystem.CanBuildMachine(state, facilityId, kind)) return false;
            if (x < 0f || x > 1f || y < 0f || y > 1f) return false;
            FacilityState facility = FindFacility(state, facilityId);
            if (facility == null) return false;
            // Prototype spatial rule: keep a minimum distance between machines.
            for (int i = 0; i < facility.Machines.Count; i++)
            {
                MachineState m = facility.Machines[i];
                if (m.X < 0f || m.Y < 0f) continue;
                float dx = m.X - x;
                float dy = m.Y - y;
                if (dx * dx + dy * dy < 0.035f) return false;
            }
            return true;
        }

        public static bool PlaceMachine(StrategicState state, int facilityId, MachineKind kind, float x, float y)
        {
            if (!CanPlaceMachine(state, facilityId, kind, x, y)) return false;
            if (!ProductionSystem.BuildMachine(state, facilityId, kind)) return false;
            FacilityState facility = FindFacility(state, facilityId);
            MachineState machine = facility.Machines[facility.Machines.Count - 1];
            machine.X = x;
            machine.Y = y;
            return true;
        }

        public static void EnsureLayout(StrategicState state, int facilityId)
        {
            FacilityState facility = FindFacility(state, facilityId);
            if (facility == null) return;
            for (int i = 0; i < facility.Machines.Count; i++)
            {
                MachineState m = facility.Machines[i];
                if (m.X >= 0f && m.Y >= 0f) continue;
                int col = i % 3;
                int row = i / 3;
                m.X = 0.25f + col * 0.25f;
                m.Y = 0.30f + row * 0.30f;
            }
        }

        public static bool IsMachineAllowed(FacilityKind facilityKind, MachineKind machineKind)
        {
            switch (facilityKind)
            {
                case FacilityKind.ProductionHall:
                    return machineKind == MachineKind.BasicPress || machineKind == MachineKind.ImprovedPress || machineKind == MachineKind.HighSpeedPress || machineKind == MachineKind.ElectronicsAssembler;
                case FacilityKind.Workshop:
                    return machineKind == MachineKind.Recycler;
                case FacilityKind.EnergyHall:
                    return machineKind == MachineKind.Generator;
                default:
                    return false;
            }
        }

        private static FacilityState FindFacility(StrategicState state, int facilityId)
        {
            if (state == null) return null;
            for (int i = 0; i < state.Facilities.Count; i++)
                if (state.Facilities[i].Id == facilityId) return state.Facilities[i];
            return null;
        }
    }
}
