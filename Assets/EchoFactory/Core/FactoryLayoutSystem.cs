using System;
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
        private const float MinDistanceSq = 0.035f;

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
                if (dx * dx + dy * dy < MinDistanceSq) return false;
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

        public const int DemolishRefundPercent = 50;

        public static bool CanMoveMachine(StrategicState state, int facilityId, int machineId, float x, float y)
        {
            if (state == null || state.Phase != StrategicPhase.Planning || state.Outcome != GameOutcome.Playing) return false;
            if (x < 0f || x > 1f || y < 0f || y > 1f) return false;
            FacilityState facility = FindFacility(state, facilityId);
            if (facility == null || !facility.Unlocked) return false;
            bool found = false;
            for (int i = 0; i < facility.Machines.Count; i++)
            {
                MachineState m = facility.Machines[i];
                if (m.Id == machineId) { found = true; continue; }
                if (m.X < 0f || m.Y < 0f) continue;
                float dx = m.X - x;
                float dy = m.Y - y;
                if (dx * dx + dy * dy < MinDistanceSq) return false;
            }
            return found;
        }

        public static bool MoveMachine(StrategicState state, int facilityId, int machineId, float x, float y)
        {
            if (!CanMoveMachine(state, facilityId, machineId, x, y)) return false;
            FacilityState facility = FindFacility(state, facilityId);
            for (int i = 0; i < facility.Machines.Count; i++)
            {
                if (facility.Machines[i].Id != machineId) continue;
                facility.Machines[i].X = x;
                facility.Machines[i].Y = y;
                return true;
            }
            return false;
        }

        public static int GetRefund(MachineKind kind)
        {
            return ProductionSystem.GetMachinePrice(kind) * DemolishRefundPercent / 100;
        }

        // Removes the machine and returns half of its price; returns -1 when nothing was demolished.
        public static int DemolishMachine(StrategicState state, int facilityId, int machineId)
        {
            if (state == null || state.Phase != StrategicPhase.Planning || state.Outcome != GameOutcome.Playing) return -1;
            FacilityState facility = FindFacility(state, facilityId);
            if (facility == null || !facility.Unlocked) return -1;
            for (int i = 0; i < facility.Machines.Count; i++)
            {
                if (facility.Machines[i].Id != machineId) continue;
                int refund = GetRefund(facility.Machines[i].Kind);
                facility.Machines.RemoveAt(i);
                state.Credits += refund;
                return refund;
            }
            return -1;
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
