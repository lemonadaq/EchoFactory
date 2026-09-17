using System;
using System.Collections.Generic;

namespace EchoFactory.Core
{
    public enum StrategicPhase { Planning, Resolving, Summary }
    public enum ResourceKind { None, Steel, Energy, Bitumen, Scrap, Electronics }
    public enum FacilityKind { Empty, ProductionHall, LogisticsHall, EnergyHall, Workshop, ResearchHall }

    [Serializable]
    public sealed class ParcelState
    {
        public int Id, Price, Size, ResourceRichness, LogisticsBonus;
        public ResourceKind Resource;
        public bool Owned;
        public ParcelState Copy() { return (ParcelState)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class FacilityState
    {
        public int Id, Level = 1, ParcelId = -1, MachineSlots;
        public FacilityKind Kind;
        public FacilityState Copy() { return (FacilityState)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class StrategicState
    {
        public int Turn = 1, Credits = 10000, NextFacilityId = 1;
        public StrategicPhase Phase = StrategicPhase.Planning;
        public int LastTurnIncome, LastTurnCosts, LastTurnProduction;
        public List<ParcelState> Parcels = new List<ParcelState>();
        public List<FacilityState> Facilities = new List<FacilityState>();
        public Dictionary<ResourceKind, int> Stock = new Dictionary<ResourceKind, int>();
        public int GetStock(ResourceKind kind) { int v; return Stock.TryGetValue(kind, out v) ? v : 0; }
        public void AddStock(ResourceKind kind, int amount) { if (kind != ResourceKind.None && amount != 0) Stock[kind] = GetStock(kind) + amount; }
        public StrategicState Copy()
        {
            var c = new StrategicState { Turn=Turn, Credits=Credits, NextFacilityId=NextFacilityId, Phase=Phase, LastTurnIncome=LastTurnIncome, LastTurnCosts=LastTurnCosts, LastTurnProduction=LastTurnProduction };
            for (int i=0;i<Parcels.Count;i++) c.Parcels.Add(Parcels[i].Copy());
            for (int i=0;i<Facilities.Count;i++) c.Facilities.Add(Facilities[i].Copy());
            foreach (var p in Stock) c.Stock[p.Key]=p.Value;
            return c;
        }
    }

    public static class StrategicWorldGenerator
    {
        static readonly ResourceKind[] Resources = { ResourceKind.Steel, ResourceKind.Energy, ResourceKind.Bitumen, ResourceKind.Scrap, ResourceKind.Electronics };
        public static StrategicState NewGame(int seed)
        {
            var s = new StrategicState { Parcels = GenerateParcels(seed, 9) };
            s.Parcels[0].Owned = true;
            s.Facilities.Add(new FacilityState { Id=s.NextFacilityId++, Kind=FacilityKind.ProductionHall, ParcelId=0, MachineSlots=2 });
            s.AddStock(ResourceKind.Steel, 25);
            return s;
        }
        public static List<ParcelState> GenerateParcels(int seed, int count)
        {
            var rng = new Random(seed); var result = new List<ParcelState>();
            for (int i=0;i<count;i++) result.Add(new ParcelState { Id=i, Price=5000+rng.Next(0,8)*1500, Size=700+rng.Next(0,14)*100, Resource=Resources[rng.Next(Resources.Length)], ResourceRichness=35+rng.Next(66), LogisticsBonus=rng.Next(-5,16) });
            return result;
        }
    }
}
