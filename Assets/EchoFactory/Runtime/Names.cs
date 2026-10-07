using EchoFactory.Core;

namespace EchoFactory.Runtime
{
    /// <summary>Polish display names shared by all screens.</summary>
    public static class Names
    {
        public static string Signed(int v) { return v >= 0 ? "+" + v : v.ToString(); }

        public static string Resource(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Steel: return "STAL";
                case ResourceKind.Energy: return "ENERGIA";
                case ResourceKind.Bitumen: return "BITUMEN";
                case ResourceKind.Scrap: return "ZŁOM";
                case ResourceKind.Electronics: return "ELEKTRONIKA";
                case ResourceKind.FinishedGoods: return "GOTOWE PRODUKTY";
                default: return "—";
            }
        }

        public static string Facility(FacilityKind k)
        {
            switch (k)
            {
                case FacilityKind.ProductionHall: return "HALA PRODUKCYJNA";
                case FacilityKind.LogisticsHall: return "HALA LOGISTYCZNA";
                case FacilityKind.EnergyHall: return "HALA ENERGETYCZNA";
                case FacilityKind.Workshop: return "WARSZTAT";
                case FacilityKind.ResearchHall: return "HALA BADAWCZA";
                default: return "OBIEKT";
            }
        }

        public static string FacilityPurpose(FacilityKind k)
        {
            switch (k)
            {
                case FacilityKind.ProductionHall: return "prasy i montaż";
                case FacilityKind.EnergyHall: return "generatory";
                case FacilityKind.Workshop: return "recykling złomu";
                case FacilityKind.LogisticsHall: return "brak maszyn w prototypie";
                default: return "";
            }
        }

        public static string Machine(MachineKind k)
        {
            switch (k)
            {
                case MachineKind.BasicPress: return "Prasa podstawowa";
                case MachineKind.ImprovedPress: return "Prasa ulepszona";
                case MachineKind.HighSpeedPress: return "Prasa High-Speed";
                case MachineKind.Recycler: return "Recykler";
                case MachineKind.Generator: return "Generator";
                case MachineKind.ElectronicsAssembler: return "Montaż elektroniki";
                default: return k.ToString();
            }
        }

        public static string MachineShort(MachineKind k)
        {
            switch (k)
            {
                case MachineKind.BasicPress: return "PRASA";
                case MachineKind.ImprovedPress: return "PRASA+";
                case MachineKind.HighSpeedPress: return "PRASA HS";
                case MachineKind.Recycler: return "RECYKLER";
                case MachineKind.Generator: return "GENERATOR";
                case MachineKind.ElectronicsAssembler: return "MONTAŻ";
                default: return "MASZYNA";
            }
        }

        public static string Recipe(MachineKind k)
        {
            switch (k)
            {
                case MachineKind.BasicPress: return "1 stal + 1 energia → 1 produkt + 1 złom";
                case MachineKind.ImprovedPress: return "1 stal + 1–2 energia → 2 produkty + 1 złom";
                case MachineKind.HighSpeedPress: return "2 stal + 3 energia → 3+ produkty + 2 złom";
                case MachineKind.Recycler: return "2 złom + 1 energia → 1–2 stal";
                case MachineKind.Generator: return "2 bitumen → 3+ energia";
                case MachineKind.ElectronicsAssembler: return "elektronika + stal + 2 energia → 3 produkty";
                default: return "";
            }
        }

        public static string Technology(TechnologyKind k)
        {
            switch (k)
            {
                case TechnologyKind.BasicAutomation: return "Podstawowa automatyzacja";
                case TechnologyKind.ImprovedPress: return "Ulepszona prasa";
                case TechnologyKind.AdvancedPress: return "Prasa zaawansowana";
                case TechnologyKind.SmartLogistics: return "Smart Logistics";
                case TechnologyKind.EnergyEfficiency: return "Efektywność energetyczna";
                case TechnologyKind.AdvancedMaterials: return "Materiały zaawansowane";
                default: return k.ToString();
            }
        }

        public static string TechRequirement(TechnologyKind k)
        {
            switch (k)
            {
                case TechnologyKind.BasicAutomation: return "Brak wymagań";
                case TechnologyKind.ImprovedPress: return "Wymaga: Podstawowa automatyzacja";
                case TechnologyKind.AdvancedPress: return "Wymaga: Ulepszona prasa";
                case TechnologyKind.SmartLogistics: return "Wymaga: Podstawowa automatyzacja";
                case TechnologyKind.EnergyEfficiency: return "Wymaga: Podstawowa automatyzacja";
                case TechnologyKind.AdvancedMaterials: return "Wymaga: Ulepszona prasa + Efektywność";
                default: return "";
            }
        }
    }
}
