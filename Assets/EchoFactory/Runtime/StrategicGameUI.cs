using System;
using System.Collections.Generic;
using EchoFactory.Core;
using UnityEngine;

namespace EchoFactory.Runtime
{
    /// <summary>First playable strategic layer. Intentionally uses Unity IMGUI so no scene wiring or prefabs are required.</summary>
    public sealed class StrategicGameUI : MonoBehaviour
    {
        private enum Screen { Menu, Map, Facility, Research, Summary }
        private StrategicState state;
        private Screen screen = Screen.Menu;
        private int selectedParcel = 0;
        private int selectedFacility = -1;
        private Vector2 scroll;
        private GUIStyle title, h1, h2, body, small, button, card;
        private readonly Color bg = new Color(.92f, .93f, .91f);
        private readonly Color panel = new Color(.98f, .98f, .96f);
        private readonly Color ink = new Color(.12f, .15f, .16f);
        private readonly Color accent = new Color(.08f, .42f, .43f);
        private readonly Color amber = new Color(.68f, .43f, .12f);
        private const float W = 1440f, H = 900f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            if (FindObjectOfType<StrategicGameUI>() != null) return;
            var go = new GameObject("EchoFactory Strategic UI");
            DontDestroyOnLoad(go);
            go.AddComponent<StrategicGameUI>();
        }

        private void Awake() { Application.targetFrameRate = 60; }

        private void InitStyles()
        {
            if (body != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold };
            h1 = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            h2 = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 13 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true, padding = new RectOffset(10, 10, 7, 7) };
            card = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
            title.normal.textColor = h1.normal.textColor = h2.normal.textColor = body.normal.textColor = small.normal.textColor = ink;
        }

        private void OnGUI()
        {
            InitStyles();
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.height / H, 1));
            GUI.backgroundColor = bg;
            GUI.color = bg;
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (screen == Screen.Menu) DrawMenu();
            else { DrawTopBar(); if (screen == Screen.Map) DrawMap(); else if (screen == Screen.Facility) DrawFacility(); else if (screen == Screen.Research) DrawResearch(); else DrawSummary(); }
        }

        private void Label(float x, float y, float w, float h, string text, GUIStyle style = null, Color? color = null)
        { var old = GUI.color; GUI.color = color ?? ink; GUI.Label(new Rect(x, y, w, h), text, style ?? body); GUI.color = old; }

        private bool Btn(float x, float y, float w, float h, string text, bool enabled = true, bool primary = false)
        {
            bool old = GUI.enabled; Color oldBg = GUI.backgroundColor; GUI.enabled = enabled;
            GUI.backgroundColor = primary ? accent : new Color(.80f, .83f, .81f);
            bool result = GUI.Button(new Rect(x, y, w, h), text, button);
            GUI.enabled = old; GUI.backgroundColor = oldBg; return result;
        }

        private void Box(float x, float y, float w, float h)
        { var old = GUI.color; GUI.color = panel; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = old; }

        private void DrawMenu()
        {
            Label(95, 100, 900, 70, "ECHO FACTORY", title, accent);
            Label(98, 172, 760, 55, "Strategia produkcji. Nie budujesz bonusów — budujesz system.", h1);
            Box(95, 260, 760, 330);
            Label(130, 300, 690, 42, "PROTOTYP — WARSTWA STRATEGICZNA", h2, accent);
            Label(130, 355, 650, 150, "Kupuj działki, rozwijaj hale, badaj technologie, ustawiaj maszyny i kończ tury. Każda decyzja zmienia przepływ surowców, energii, złomu i gotowych produktów.", body);
            if (Btn(130, 515, 300, 52, "NOWA GRA", true, true)) NewGame();
            Label(95, 690, 1000, 50, "Na tym etapie grafika jest celowo prosta. Najpierw sprawdzamy, czy ekonomia i decyzje dają frajdę.", small, new Color(.35f, .38f, .37f));
        }

        private void NewGame()
        { state = StrategicWorldGenerator.NewGame(Environment.TickCount); selectedParcel = 0; selectedFacility = state.Facilities[0].Id; screen = Screen.Map; }

        private void DrawTopBar()
        {
            Label(30, 20, 380, 40, "ECHO FACTORY", h1, accent);
            Label(430, 22, 230, 34, "TURA " + state.Turn + "  ·  PLANOWANIE", h2);
            Label(930, 20, 210, 38, state.Credits + " C", h1, amber);
            Label(1145, 23, 250, 32, "STAL " + state.GetStock(ResourceKind.Steel) + "  |  EN " + state.GetStock(ResourceKind.Energy) + "  |  ZŁOM " + state.GetStock(ResourceKind.Scrap), small);
            if (Btn(30, 65, 125, 34, "MAPA")) screen = Screen.Map;
            if (Btn(165, 65, 125, 34, "R&D", state != null)) screen = Screen.Research;
            if (Btn(300, 65, 155, 34, "KONSTRUKCJE", state != null)) screen = Screen.Facility;
            if (Btn(1210, 65, 185, 42, "ZAKOŃCZ TURĘ", state.Phase == StrategicPhase.Planning, true)) ResolveTurn();
        }

        private void DrawMap()
        {
            Label(30, 125, 700, 42, "MAPA PRZEMYSŁOWA", h1);
            Label(30, 168, 700, 30, "Każda działka ma inny profil zasobów, logistykę i koszt wejścia.", small);
            for (int i = 0; i < state.Parcels.Count; i++)
            {
                var p = state.Parcels[i]; int col = i % 3, row = i / 3; float x = 30 + col * 235, y = 215 + row * 150;
                bool sel = p.Id == selectedParcel; Box(x, y, 215, 135);
                Label(x + 12, y + 10, 190, 25, "DZIAŁKA " + (p.Id + 1) + (p.Owned ? " · TWOJA" : ""), h2, p.Owned ? accent : ink);
                Label(x + 12, y + 40, 190, 25, ResourceName(p.Resource), body);
                Label(x + 12, y + 66, 190, 20, "Zasób: " + p.ResourceRichness + " · Log: " + Signed(p.LogisticsBonus), small);
                Label(x + 12, y + 88, 190, 20, p.Size + " m² · " + p.Price + " C", small);
                if (Btn(x + 12, y + 108, 90, 22, sel ? "WYBRANA" : "OTWÓRZ")) { selectedParcel = p.Id; screen = Screen.Facility; }
            }
            DrawParcelPanel(745, 125, 650, 705);
        }

        private void DrawParcelPanel(float x, float y, float w, float h)
        {
            var p = state.Parcels[selectedParcel]; Box(x, y, w, h);
            Label(x + 25, y + 22, w - 50, 40, "DZIAŁKA " + (p.Id + 1), h1, accent);
            Label(x + 25, y + 70, w - 50, 95, "Surowiec: " + ResourceName(p.Resource) + "\nBogactwo: " + p.ResourceRichness + "/100\nPremia logistyczna: " + Signed(p.LogisticsBonus) + "%\nPowierzchnia: " + p.Size + " m²", body);
            if (!p.Owned)
            {
                if (Btn(x + 25, y + 185, 280, 48, "KUP DZIAŁKĘ · " + p.Price + " C", state.Credits >= p.Price, true)) { state.Credits -= p.Price; p.Owned = true; screen = Screen.Facility; }
                Label(x + 25, y + 240, w - 50, 55, state.Credits < p.Price ? "Brakuje kapitału. Rozbuduj istniejącą produkcję albo poczekaj na przychód." : "Zakup działki odblokuje budowę hali.", small, amber);
            }
            else
            {
                Label(x + 25, y + 185, w - 50, 30, "OBIEKTY NA DZIAŁCE", h2);
                int yy = 225;
                for (int i = 0; i < state.Facilities.Count; i++) if (state.Facilities[i].ParcelId == p.Id)
                {
                    var f = state.Facilities[i]; Label(x + 25, y + yy, w - 70, 28, "#" + f.Id + "  " + FacilityName(f.Kind) + " · " + f.Machines.Count + "/" + f.MachineSlots + " maszyn", body); yy += 32;
                }
                if (Btn(x + 25, y + 550, 260, 42, "ZARZĄDZAJ DZIAŁKĄ")) screen = Screen.Facility;
            }
        }

        private void DrawFacility()
        {
            var p = state.Parcels[selectedParcel];
            Label(30, 125, 700, 40, "ZARZĄDZANIE · DZIAŁKA " + (p.Id + 1), h1);
            if (!p.Owned) { Label(30, 185, 650, 50, "Ta działka nie jest jeszcze twoja. Wróć na mapę i kup ją.", body, amber); return; }
            int y = 190;
            for (int i = 0; i < state.Facilities.Count; i++) if (state.Facilities[i].ParcelId == p.Id)
            {
                var f = state.Facilities[i]; Box(30, y, 630, 92);
                Label(50, y + 12, 380, 28, "#" + f.Id + "  " + FacilityName(f.Kind), h2, accent);
                Label(50, y + 45, 390, 25, "Maszyny: " + f.Machines.Count + "/" + f.MachineSlots + " · dochód: " + f.PassiveIncomePerTurn + "/turę", small);
                if (Btn(480, y + 20, 145, 42, "OTWÓRZ")) { selectedFacility = f.Id; }
                y += 105;
            }
            Label(705, 125, 650, 38, "BUDOWA HALI", h1);
            int by = 190;
            foreach (FacilityKind kind in new[] { FacilityKind.ProductionHall, FacilityKind.LogisticsHall, FacilityKind.EnergyHall, FacilityKind.Workshop })
            {
                int price = FacilitySystem.GetPrice(kind); bool can = FacilitySystem.CanBuild(state, p.Id, kind);
                Box(705, by, 650, 82); Label(725, by + 10, 260, 25, FacilityName(kind), h2); Label(725, by + 40, 390, 24, "Cena " + price + " C · " + FacilitySystem.GetSlots(kind) + " sloty", small);
                if (Btn(1165, by + 20, 165, 40, "BUDUJ", can, true)) { FacilitySystem.Build(state, p.Id, kind); }
                by += 95;
            }
            if (Btn(30, 785, 250, 40, "← WRÓĆ NA MAPĘ")) screen = Screen.Map;
            DrawSelectedFacility(705, 580, 650, 210);
        }

        private void DrawSelectedFacility(float x, float y, float w, float h)
        {
            if (selectedFacility < 0) return; FacilityState f = FindFacility(selectedFacility); if (f == null) return;
            Box(x, y, w, h); Label(x + 20, y + 12, w - 40, 30, "HALA #" + f.Id + " · " + FacilityName(f.Kind), h2, accent);
            Label(x + 20, y + 48, w - 40, 30, "Sloty: " + f.Machines.Count + "/" + f.MachineSlots, body);
            int xx = (int)x + 20;
            for (int i = 0; i < f.Machines.Count; i++) { Label(xx, y + 82, 185, 25, MachineName(f.Machines[i].Kind), small); xx += 195; }
            if (f.Machines.Count < f.MachineSlots)
            {
                int yy = (int)y + 118;
                foreach (MachineKind k in new[] { MachineKind.BasicPress, MachineKind.ImprovedPress, MachineKind.HighSpeedPress, MachineKind.Recycler, MachineKind.Generator, MachineKind.ElectronicsAssembler })
                {
                    if (ProductionSystem.CanBuildMachine(state, f.Id, k))
                    { if (Btn(xx, yy, 155, 38, "+ " + MachineName(k) + "  " + ProductionSystem.GetMachinePrice(k), true)) ProductionSystem.BuildMachine(state, f.Id, k); xx += 165; if (xx > x + w - 160) { xx = (int)x + 20; yy += 45; } }
                }
            }
        }

        private void DrawResearch()
        {
            Label(30, 125, 700, 40, "R&D · DRZEWO TECHNOLOGII", h1);
            bool hall = HasResearchHall();
            Label(30, 170, 900, 32, hall ? "Laboratorium aktywne. Technologie zmieniają reguły produkcji." : "Najpierw zbuduj Halę Badawczą na dowolnej własnej działce (7500 C).", body, hall ? accent : amber);
            int y = 225;
            foreach (TechnologyKind k in new[] { TechnologyKind.BasicAutomation, TechnologyKind.ImprovedPress, TechnologyKind.AdvancedPress, TechnologyKind.SmartLogistics, TechnologyKind.EnergyEfficiency, TechnologyKind.AdvancedMaterials })
            {
                var t = FindTech(k); Box(30, y, 900, 72); Label(50, y + 9, 250, 26, TechName(k), h2, t.Unlocked ? accent : ink); Label(300, y + 12, 360, 42, "Koszt: " + t.ResearchCost + " C\n" + TechRequirement(k), small);
                if (Btn(750, y + 15, 150, 38, t.Unlocked ? "ODBLOCKOWANA" : "BADAJ", hall && !t.Unlocked, t.Unlocked)) ResearchSystem.UnlockTechnology(state, k);
                y += 82;
            }
            if (!hall)
            {
                for (int i = 0; i < state.Parcels.Count; i++) if (state.Parcels[i].Owned && ResearchSystem.BuildResearchHall(state, state.Parcels[i].Id)) { screen = Screen.Research; break; }
            }
            Label(965, 225, 410, 200, "HALA BADAWCZA\n\n7500 C\n\nJedna na całą sieć. Odblokowuje drzewo R&D.\n\nTo celowy wybór strategiczny: inwestujesz kapitał teraz, żeby zmienić sposób działania fabryki później.", body);
        }

        private void ResolveTurn()
        {
            if (state.Phase != StrategicPhase.Planning) return;
            TurnResult result = TurnSystem.EndTurn(state); screen = Screen.Summary;
        }

        private void DrawSummary()
        {
            Label(30, 125, 800, 42, "PODSUMOWANIE TURY " + state.Turn, h1);
            Box(30, 190, 760, 360);
            Label(60, 220, 680, 40, "WYNIK SYSTEMU PRODUKCJI", h2, accent);
            Label(60, 275, 650, 220, "Produkcja: " + state.LastTurnProduction + " szt.\nDochód pasywny: +" + state.LastPassiveIncome + " C\nKoszt operacyjny: -" + state.LastTurnCosts + " C\nZużycie stali: " + state.GetStock(ResourceKind.Steel) + " pozostało\nEnergia: " + state.GetStock(ResourceKind.Energy) + " pozostało\nZłom: " + state.GetStock(ResourceKind.Scrap) + " pozostało\n\nSaldo: " + state.Credits + " C", body);
            Label(840, 210, 520, 190, state.LastTurnProduction == 0 ? "SYSTEM NIE PRODUKOWAŁ.\n\nSprawdź surowce, energię i dostępne maszyny." : "SYSTEM DZIAŁA.\n\nTeraz ważniejsze od samego wyniku jest pytanie: co stało się wąskim gardłem?", h1, state.LastTurnProduction == 0 ? amber : accent);
            if (Btn(840, 450, 330, 55, "NASTĘPNA TURA", true, true)) { TurnSystem.ContinueToPlanning(state); screen = Screen.Map; }
        }

        private FacilityState FindFacility(int id) { for (int i = 0; i < state.Facilities.Count; i++) if (state.Facilities[i].Id == id) return state.Facilities[i]; return null; }
        private TechnologyState FindTech(TechnologyKind k) { for (int i = 0; i < state.Technologies.Count; i++) if (state.Technologies[i].Kind == k) return state.Technologies[i]; return null; }
        private bool HasResearchHall() { for (int i = 0; i < state.Facilities.Count; i++) if (state.Facilities[i].Kind == FacilityKind.ResearchHall) return true; return false; }
        private static string Signed(int v) { return v >= 0 ? "+" + v : v.ToString(); }
        private static string ResourceName(ResourceKind k) { switch (k) { case ResourceKind.Steel: return "STAL"; case ResourceKind.Energy: return "ENERGIA"; case ResourceKind.Bitumen: return "BITUMEN"; case ResourceKind.Scrap: return "ZŁOM"; case ResourceKind.Electronics: return "ELEKTRONIKA"; default: return "—"; } }
        private static string FacilityName(FacilityKind k) { switch (k) { case FacilityKind.ProductionHall: return "HALA PRODUKCYJNA"; case FacilityKind.LogisticsHall: return "HALA LOGISTYCZNA"; case FacilityKind.EnergyHall: return "HALA ENERGETYCZNA"; case FacilityKind.Workshop: return "WARSZTAT"; case FacilityKind.ResearchHall: return "HALA BADAWCZA"; default: return "—"; } }
        private static string MachineName(MachineKind k) { switch (k) { case MachineKind.BasicPress: return "Prasa podstawowa"; case MachineKind.ImprovedPress: return "Prasa ulepszona"; case MachineKind.HighSpeedPress: return "Prasa High-Speed"; case MachineKind.Recycler: return "Recykler"; case MachineKind.Generator: return "Generator"; case MachineKind.ElectronicsAssembler: return "Montaż elektroniki"; default: return k.ToString(); } }
        private static string TechName(TechnologyKind k) { switch (k) { case TechnologyKind.BasicAutomation: return "Podstawowa automatyzacja"; case TechnologyKind.ImprovedPress: return "Ulepszona prasa"; case TechnologyKind.AdvancedPress: return "Prasa zaawansowana"; case TechnologyKind.SmartLogistics: return "Smart Logistics"; case TechnologyKind.EnergyEfficiency: return "Efektywność energetyczna"; case TechnologyKind.AdvancedMaterials: return "Materiały zaawansowane"; default: return k.ToString(); } }
        private static string TechRequirement(TechnologyKind k) { switch (k) { case TechnologyKind.BasicAutomation: return "Brak wymagań"; case TechnologyKind.ImprovedPress: return "Wymaga: Podstawowa automatyzacja"; case TechnologyKind.AdvancedPress: return "Wymaga: Ulepszona prasa"; case TechnologyKind.SmartLogistics: return "Wymaga: Podstawowa automatyzacja"; case TechnologyKind.EnergyEfficiency: return "Wymaga: Podstawowa automatyzacja"; case TechnologyKind.AdvancedMaterials: return "Wymaga: Ulepszona prasa + Efektywność"; default: return ""; } }
    }
}
