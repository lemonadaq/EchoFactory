using System;
using System.Collections.Generic;

namespace EchoFactory.Core
{
    public enum StrategicPhase { Planning, Resolving, Summary }
    public enum ResourceKind { None, Steel, Energy, Bitumen, Scrap, Electronics, FinishedGoods }
    public enum FacilityKind { Empty, ProductionHall, LogisticsHall, EnergyHall, Workshop, ResearchHall }
    public enum TechnologyKind { None, BasicAutomation, ImprovedPress, AdvancedPress, SmartLogistics, EnergyEfficiency, AdvancedMaterials }
    public enum MachineKind { BasicPress, ImprovedPress, HighSpeedPress, Recycler, Generator, ElectronicsAssembler }

    [Serializable] public sealed class TechnologyState { public TechnologyKind Kind; public bool Unlocked; public int ResearchCost; public TechnologyState Copy() { return (TechnologyState)MemberwiseClone(); } }
    [Serializable] public sealed class ParcelState { public int Id, Price, Size, ResourceRichness, LogisticsBonus; public ResourceKind Resource; public bool Owned; public ParcelState Copy() { return (ParcelState)MemberwiseClone(); } }

    [Serializable]
    public sealed class MachineState
    {
        public int Id, FacilityId, Level = 1;
        public MachineKind Kind;
        public bool Enabled = true;
        // Normalized floor coordinates (0..1). Negative means not placed yet.
        public float X = -1f, Y = -1f;
        public MachineState Copy() { return (MachineState)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class FacilityState
    {
        public int Id, Level = 1, ParcelId = -1, MachineSlots;
        public FacilityKind Kind; public bool Unlocked = true; public int PassiveIncomePerTurn;
        public List<MachineState> Machines = new List<MachineState>();
        public FacilityState Copy() { var c=(FacilityState)MemberwiseClone(); c.Machines=new List<MachineState>(); for(int i=0;i<Machines.Count;i++) c.Machines.Add(Machines[i].Copy()); return c; }
    }

    [Serializable]
    public sealed class StrategicState
    {
        public int Turn=1, Credits=10000, NextFacilityId=1, NextMachineId=1;
        public StrategicPhase Phase=StrategicPhase.Planning;
        public int LastTurnIncome, LastTurnCosts, LastTurnProduction, LastPassiveIncome, LastResearchSpent;
        public List<ParcelState> Parcels=new List<ParcelState>(); public List<FacilityState> Facilities=new List<FacilityState>(); public List<TechnologyState> Technologies=new List<TechnologyState>(); public Dictionary<ResourceKind,int> Stock=new Dictionary<ResourceKind,int>();
        public int GetStock(ResourceKind kind){int v;return Stock.TryGetValue(kind,out v)?v:0;}
        public void AddStock(ResourceKind kind,int amount){if(kind!=ResourceKind.None&&amount!=0)Stock[kind]=GetStock(kind)+amount;}
        public bool HasTechnology(TechnologyKind kind){for(int i=0;i<Technologies.Count;i++)if(Technologies[i].Kind==kind)return Technologies[i].Unlocked;return false;}
        public StrategicState Copy(){var c=new StrategicState{Turn=Turn,Credits=Credits,NextFacilityId=NextFacilityId,NextMachineId=NextMachineId,Phase=Phase,LastTurnIncome=LastTurnIncome,LastTurnCosts=LastTurnCosts,LastTurnProduction=LastTurnProduction,LastPassiveIncome=LastPassiveIncome,LastResearchSpent=LastResearchSpent};for(int i=0;i<Parcels.Count;i++)c.Parcels.Add(Parcels[i].Copy());for(int i=0;i<Facilities.Count;i++)c.Facilities.Add(Facilities[i].Copy());for(int i=0;i<Technologies.Count;i++)c.Technologies.Add(Technologies[i].Copy());foreach(var p in Stock)c.Stock[p.Key]=p.Value;return c;}
    }

    public static class StrategicWorldGenerator
    {
        static readonly ResourceKind[] Resources={ResourceKind.Steel,ResourceKind.Energy,ResourceKind.Bitumen,ResourceKind.Scrap,ResourceKind.Electronics};
        static readonly TechnologyKind[] StartingTechnologies={TechnologyKind.BasicAutomation,TechnologyKind.ImprovedPress,TechnologyKind.AdvancedPress,TechnologyKind.SmartLogistics,TechnologyKind.EnergyEfficiency,TechnologyKind.AdvancedMaterials};
        public static StrategicState NewGame(int seed)
        {
            var s=new StrategicState{Parcels=GenerateParcels(seed,9)}; s.Parcels[0].Owned=true;
            var starter=new FacilityState{Id=s.NextFacilityId++,Kind=FacilityKind.ProductionHall,ParcelId=0,MachineSlots=2,PassiveIncomePerTurn=300};
            starter.Machines.Add(new MachineState{Id=s.NextMachineId++,FacilityId=starter.Id,Kind=MachineKind.BasicPress}); s.Facilities.Add(starter);
            for(int i=0;i<StartingTechnologies.Length;i++)s.Technologies.Add(new TechnologyState{Kind=StartingTechnologies[i],ResearchCost=2500+i*1000});
            s.AddStock(ResourceKind.Steel,25); s.AddStock(ResourceKind.Energy,5); return s;
        }
        public static List<ParcelState> GenerateParcels(int seed,int count){var rng=new Random(seed);var result=new List<ParcelState>();for(int i=0;i<count;i++)result.Add(new ParcelState{Id=i,Price=5000+rng.Next(0,8)*1500,Size=700+rng.Next(0,14)*100,Resource=Resources[rng.Next(Resources.Length)],ResourceRichness=35+rng.Next(66),LogisticsBonus=rng.Next(-5,16)});return result;}
    }
}
