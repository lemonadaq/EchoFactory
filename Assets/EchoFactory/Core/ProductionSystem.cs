using System;

namespace EchoFactory.Core
{
    public enum IdleReason { None, Disabled, MissingTechnology, NoSteel, NoEnergy, NoBitumen, NoScrap, NoElectronics }

    public sealed class MachineReport
    {
        public int MachineId, FacilityId;
        public MachineKind Kind;
        public IdleReason Reason; // None = machine worked this turn
        public bool Worked { get { return Reason == IdleReason.None; } }
    }

    public sealed class ProductionResult
    {
        public System.Collections.Generic.List<MachineReport> Reports = new System.Collections.Generic.List<MachineReport>();
        public int FinishedGoods;
        public int SteelConsumed;
        public int EnergyConsumed;
        public int BitumenConsumed;
        public int ScrapGenerated;
        public int ScrapRecycled;
        public int EnergyGenerated;
        public int MachinesWorked;
    }

    public static class ProductionSystem
    {
        public static int MachineCost(MachineKind kind)
        {
            switch (kind)
            {
                case MachineKind.BasicPress: return 1200;
                case MachineKind.ImprovedPress: return 3500;
                case MachineKind.HighSpeedPress: return 5000;
                case MachineKind.Recycler: return 2800;
                case MachineKind.Generator: return 3200;
                case MachineKind.ElectronicsAssembler: return 6000;
                default: return 0;
            }
        }

        // A13: Basic Automation adds one machine slot to every production hall.
        public static int SlotCount(StrategicState state, FacilityState facility)
        {
            int bonus = facility.Kind == FacilityKind.ProductionHall && state.HasTechnology(TechnologyKind.BasicAutomation) ? 1 : 0;
            return facility.MachineSlots + bonus;
        }

        public static int GetMachinePrice(MachineKind kind) { return MachineCost(kind); }

        public static bool CanBuildMachine(StrategicState state, int facilityId, MachineKind kind)
        {
            if (state == null || state.Phase != StrategicPhase.Planning) return false;
            if (!IsMachineUnlocked(state, kind)) return false;
            for (int i=0;i<state.Facilities.Count;i++)
            {
                var f = state.Facilities[i];
                if (f.Id != facilityId || !f.Unlocked || f.Machines.Count >= SlotCount(state, f)) continue;
                if (!FactoryLayoutSystem.IsMachineAllowed(f.Kind, kind)) return false;
                return state.Credits >= MachineCost(kind);
            }
            return false;
        }

        public static bool BuildMachine(StrategicState state, int facilityId, MachineKind kind)
        {
            if (!CanBuildMachine(state, facilityId, kind)) return false;
            var cost = MachineCost(kind);
            for (int i=0;i<state.Facilities.Count;i++)
            {
                var f = state.Facilities[i];
                if (f.Id != facilityId) continue;
                state.Credits -= cost;
                f.Machines.Add(new MachineState { Id=state.NextMachineId++, FacilityId=facilityId, Kind=kind });
                return true;
            }
            return false;
        }

        public static ProductionResult Resolve(StrategicState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            var result = new ProductionResult();
            for (int i=0;i<state.Facilities.Count;i++)
            {
                var facility = state.Facilities[i];
                if (!facility.Unlocked) continue;
                for (int j=0;j<facility.Machines.Count;j++)
                {
                    var machine = facility.Machines[j];
                    var report = new MachineReport { MachineId = machine.Id, FacilityId = facility.Id, Kind = machine.Kind };
                    if (!machine.Enabled) report.Reason = IdleReason.Disabled;
                    else if (!IsMachineUnlocked(state, machine.Kind)) report.Reason = IdleReason.MissingTechnology;
                    else report.Reason = ResolveMachine(state, facility, machine, result);
                    result.Reports.Add(report);
                }
            }
            return result;
        }

        static IdleReason ResolveMachine(StrategicState state, FacilityState facility, MachineState machine, ProductionResult result)
        {
            int richness = ParcelRichness(state, facility.ParcelId);
            int logistics = ParcelLogistics(state, facility.ParcelId);
            int logisticsBonus = state.HasTechnology(TechnologyKind.SmartLogistics) ? Math.Max(0, logistics) : Math.Min(0, logistics);
            switch (machine.Kind)
            {
                case MachineKind.BasicPress:
                    { var miss = Missing(state, 1, 1, 0, 0, 0); if (miss != IdleReason.None) return miss; }
                    state.AddStock(ResourceKind.Steel, -1); state.AddStock(ResourceKind.Energy, -1); state.AddStock(ResourceKind.FinishedGoods, 1); state.AddStock(ResourceKind.Scrap, 1);
                    result.SteelConsumed++; result.EnergyConsumed++; result.FinishedGoods++; result.ScrapGenerated++; result.MachinesWorked++; return IdleReason.None;
                case MachineKind.ImprovedPress:
                    if (!state.HasTechnology(TechnologyKind.ImprovedPress)) return IdleReason.MissingTechnology;
                    int improvedEnergy = state.HasTechnology(TechnologyKind.EnergyEfficiency) ? 1 : 2;
                    { var miss = Missing(state, 1, improvedEnergy, 0, 0, 0); if (miss != IdleReason.None) return miss; }
                    state.AddStock(ResourceKind.Steel, -1); state.AddStock(ResourceKind.Energy, -improvedEnergy); state.AddStock(ResourceKind.FinishedGoods, 2); state.AddStock(ResourceKind.Scrap, 1);
                    result.SteelConsumed++; result.EnergyConsumed += improvedEnergy; result.FinishedGoods += 2; result.ScrapGenerated++; result.MachinesWorked++; return IdleReason.None;
                case MachineKind.HighSpeedPress:
                    if (!state.HasTechnology(TechnologyKind.AdvancedPress)) return IdleReason.MissingTechnology;
                    { var miss = Missing(state, 2, 3, 0, 0, 0); if (miss != IdleReason.None) return miss; }
                    state.AddStock(ResourceKind.Steel, -2); state.AddStock(ResourceKind.Energy, -3); int output = 3 + Math.Max(0, logisticsBonus) / 10;
                    state.AddStock(ResourceKind.FinishedGoods, output); state.AddStock(ResourceKind.Scrap, 2); result.SteelConsumed += 2; result.EnergyConsumed += 3; result.FinishedGoods += output; result.ScrapGenerated += 2; result.MachinesWorked++; return IdleReason.None;
                case MachineKind.Recycler:
                    if (!state.HasTechnology(TechnologyKind.BasicAutomation)) return IdleReason.MissingTechnology;
                    { var miss = Missing(state, 0, 1, 0, 2, 0); if (miss != IdleReason.None) return miss; }
                    int recovered = 1 + (state.HasTechnology(TechnologyKind.AdvancedMaterials) ? 1 : 0);
                    state.AddStock(ResourceKind.Scrap, -2); state.AddStock(ResourceKind.Energy, -1); state.AddStock(ResourceKind.Steel, recovered); result.ScrapRecycled += 2; result.EnergyConsumed++; result.MachinesWorked++; return IdleReason.None;
                case MachineKind.Generator:
                    { var miss = Missing(state, 0, 0, 2, 0, 0); if (miss != IdleReason.None) return miss; }
                    int generated = 3 + richness / 40;
                    state.AddStock(ResourceKind.Bitumen, -2); state.AddStock(ResourceKind.Energy, generated); result.BitumenConsumed += 2; result.EnergyGenerated += generated; result.MachinesWorked++; return IdleReason.None;
                case MachineKind.ElectronicsAssembler:
                    if (!state.HasTechnology(TechnologyKind.AdvancedMaterials)) return IdleReason.MissingTechnology;
                    { var miss = Missing(state, 1, 2, 0, 0, 1); if (miss != IdleReason.None) return miss; }
                    state.AddStock(ResourceKind.Electronics, -1); state.AddStock(ResourceKind.Steel, -1); state.AddStock(ResourceKind.Energy, -2); state.AddStock(ResourceKind.FinishedGoods, 3); result.SteelConsumed++; result.EnergyConsumed += 2; result.FinishedGoods += 3; result.MachinesWorked++; return IdleReason.None;
            }
            return IdleReason.Disabled;
        }

        static IdleReason Missing(StrategicState state, int steel, int energy, int bitumen, int scrap, int electronics)
        {
            if (state.GetStock(ResourceKind.Steel) < steel) return IdleReason.NoSteel;
            if (state.GetStock(ResourceKind.Energy) < energy) return IdleReason.NoEnergy;
            if (state.GetStock(ResourceKind.Bitumen) < bitumen) return IdleReason.NoBitumen;
            if (state.GetStock(ResourceKind.Scrap) < scrap) return IdleReason.NoScrap;
            if (state.GetStock(ResourceKind.Electronics) < electronics) return IdleReason.NoElectronics;
            return IdleReason.None;
        }

        static bool IsMachineUnlocked(StrategicState state, MachineKind kind)
        {
            if (kind == MachineKind.BasicPress || kind == MachineKind.Generator) return true;
            if (kind == MachineKind.ImprovedPress || kind == MachineKind.Recycler) return state.HasTechnology(TechnologyKind.ImprovedPress) || state.HasTechnology(TechnologyKind.BasicAutomation);
            if (kind == MachineKind.HighSpeedPress) return state.HasTechnology(TechnologyKind.AdvancedPress);
            if (kind == MachineKind.ElectronicsAssembler) return state.HasTechnology(TechnologyKind.AdvancedMaterials);
            return false;
        }
        static int ParcelRichness(StrategicState state, int parcelId) { for(int i=0;i<state.Parcels.Count;i++) if(state.Parcels[i].Id==parcelId) return state.Parcels[i].ResourceRichness; return 50; }
        static int ParcelLogistics(StrategicState state, int parcelId) { for(int i=0;i<state.Parcels.Count;i++) if(state.Parcels[i].Id==parcelId) return state.Parcels[i].LogisticsBonus; return 0; }
    }
}
