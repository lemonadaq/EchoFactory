using System;
using EchoFactory.Core;
using UnityEngine;

namespace EchoFactory.Runtime
{
    /// <summary>Asset-free top-down factory floor prototype. Turns a selected facility into a readable production system.</summary>
    public sealed class FactoryFloorView : MonoBehaviour
    {
        private const float W = 1440f, H = 900f;
        private StrategicState state;
        private int facilityId = -1;
        private GUIStyle title, h1, h2, body, small;
        private readonly Color paper = new Color(.91f, .92f, .90f);
        private readonly Color panel = new Color(.98f, .98f, .96f);
        private readonly Color ink = new Color(.12f, .15f, .16f);
        private readonly Color accent = new Color(.08f, .42f, .43f);
        private readonly Color amber = new Color(.68f, .43f, .12f);
        private Texture2D pixel;
        private bool visible;

        public void Show(StrategicState gameState, int selectedFacilityId)
        {
            state = gameState;
            facilityId = selectedFacilityId;
            visible = state != null && FindFacility() != null;
        }

        public void Hide() { visible = false; }

        private void Awake()
        {
            pixel = Texture2D.whiteTexture;
        }

        private void InitStyles()
        {
            if (body != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            h1 = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            h2 = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 12 };
            title.normal.textColor = h1.normal.textColor = h2.normal.textColor = body.normal.textColor = small.normal.textColor = ink;
        }

        private void OnGUI()
        {
            if (!visible) return;
            InitStyles();
            GUI.depth = -20;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.height / H, 1));
            var f = FindFacility();
            if (f == null) { visible = false; return; }

            GUI.color = paper;
            GUI.DrawTexture(new Rect(0, 0, W, H), pixel);
            GUI.color = Color.white;

            Label(40, 25, 700, 45, "HALA #" + f.Id + "  ·  " + FacilityName(f.Kind), title, accent);
            Label(40, 70, 850, 28, "WIDOK PRODUKCJI · układ funkcjonalny prototypu", small);

            DrawFlowLegend();
            DrawHall(f);
            DrawInputs(f);
            DrawOutputs(f);
            DrawMachines(f);
            DrawCapacity(f);
        }

        private void DrawHall(FacilityState f)
        {
            float x = 315, y = 150, w = 800, h = 610;
            GUI.color = panel;
            GUI.DrawTexture(new Rect(x, y, w, h), pixel);
            GUI.color = new Color(.66f, .69f, .66f);
            DrawRect(x, y, w, h, 3);
            GUI.color = Color.white;
            Label(x + 25, y + 18, 500, 32, "PRODUCTION FLOOR", h1);
            Label(x + 25, y + 52, 600, 25, "Przepływ: wejście → maszyny → produkt / złom", small);
        }

        private void DrawInputs(FacilityState f)
        {
            float x = 55, y = 205;
            Label(x, 150, 230, 30, "WEJŚCIA", h2, accent);
            int row = 0;
            ResourceKind[] resources = { ResourceKind.Steel, ResourceKind.Energy, ResourceKind.Bitumen, ResourceKind.Scrap };
            foreach (var r in resources)
            {
                int amount = state.GetStock(r);
                DrawResourceBox(x, y + row * 115, ResourceName(r), amount, r == ResourceKind.Scrap ? amber : accent);
                DrawArrow(x + 210, y + 42 + row * 115, 315, y + 42 + row * 115);
                row++;
            }
        }

        private void DrawOutputs(FacilityState f)
        {
            float x = 1135, y = 245;
            Label(x, 150, 250, 30, "WYJŚCIA", h2, accent);
            DrawResourceBox(x, y, "GOTOWE PRODUKTY", state.GetStock(ResourceKind.FinishedGoods), accent);
            DrawArrow(1115, y + 42, x, y + 42);
            DrawResourceBox(x, y + 145, "ZŁOM", state.GetStock(ResourceKind.Scrap), amber);
            DrawArrow(1115, y + 187, x, y + 187);
        }

        private void DrawMachines(FacilityState f)
        {
            int count = Math.Max(1, f.Machines.Count);
            float startX = 375, startY = 250;
            for (int i = 0; i < count; i++)
            {
                float x = startX + (i % 3) * 235;
                float y = startY + (i / 3) * 155;
                bool enabled = f.Machines.Count > i && f.Machines[i].Enabled;
                if (f.Machines.Count <= i)
                {
                    GUI.color = new Color(.90f, .91f, .89f);
                    GUI.DrawTexture(new Rect(x, y, 205, 105), pixel);
                    GUI.color = Color.white;
                    Label(x + 15, y + 32, 175, 30, "PUSTY SLOT", h2, new Color(.48f, .51f, .49f));
                    continue;
                }
                var m = f.Machines[i];
                GUI.color = enabled ? new Color(.84f, .91f, .90f) : new Color(.90f, .87f, .84f);
                GUI.DrawTexture(new Rect(x, y, 205, 105), pixel);
                GUI.color = Color.white;
                Label(x + 15, y + 12, 175, 27, MachineName(m.Kind), h2, enabled ? accent : amber);
                Label(x + 15, y + 45, 175, 45, RecipeText(m.Kind), small);
                if (enabled) DrawArrow(x - 60, y + 52, x, y + 52);
            }
        }

        private void DrawCapacity(FacilityState f)
        {
            Label(340, 685, 700, 30, "Wykorzystanie slotów: " + f.Machines.Count + " / " + f.MachineSlots, h2);
            Label(340, 720, 700, 30, "Maszyny pracują podczas ROZWIĄZANIA TURY. Ten widok pokazuje architekturę przepływu.", small);
        }

        private void DrawFlowLegend()
        {
            Label(1000, 35, 390, 25, "▸ SUROWIEC     ◆ MASZYNA     → PRODUKT", small);
        }

        private void DrawResourceBox(float x, float y, string name, int amount, Color edge)
        {
            GUI.color = panel;
            GUI.DrawTexture(new Rect(x, y, 210, 82), pixel);
            GUI.color = edge;
            DrawRect(x, y, 210, 82, 3);
            GUI.color = Color.white;
            Label(x + 12, y + 10, 185, 24, name, h2);
            Label(x + 12, y + 40, 185, 28, "STAN  " + amount, body, edge);
        }

        private void DrawArrow(float x1, float y1, float x2, float y2)
        {
            GUI.color = new Color(.66f, .69f, .66f);
            DrawLine(x1, y1, x2, y2, 3);
            DrawNode(x2, y2, 8);
            GUI.color = Color.white;
        }

        private void DrawLine(float x1, float y1, float x2, float y2, float width)
        {
            Matrix4x4 old = GUI.matrix;
            float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;
            float length = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
            GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
            GUI.DrawTexture(new Rect(x1, y1 - width * .5f, length, width), pixel);
            GUI.matrix = old;
        }

        private void DrawNode(float x, float y, float size)
        {
            GUI.DrawTexture(new Rect(x - size * .5f, y - size * .5f, size, size), pixel);
        }

        private void DrawRect(float x, float y, float w, float h, float width)
        {
            GUI.DrawTexture(new Rect(x, y, w, width), pixel);
            GUI.DrawTexture(new Rect(x, y + h - width, w, width), pixel);
            GUI.DrawTexture(new Rect(x, y, width, h), pixel);
            GUI.DrawTexture(new Rect(x + w - width, y, width, h), pixel);
        }

        private FacilityState FindFacility()
        {
            if (state == null) return null;
            for (int i = 0; i < state.Facilities.Count; i++)
                if (state.Facilities[i].Id == facilityId) return state.Facilities[i];
            return null;
        }

        private static string ResourceName(ResourceKind r)
        {
            switch (r)
            {
                case ResourceKind.Steel: return "STAL";
                case ResourceKind.Energy: return "ENERGIA";
                case ResourceKind.Bitumen: return "BITUMEN";
                case ResourceKind.Scrap: return "ZŁOM";
                case ResourceKind.Electronics: return "ELEKTRONIKA";
                case ResourceKind.FinishedGoods: return "GOTOWE";
                default: return "—";
            }
        }

        private static string FacilityName(FacilityKind k)
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

        private static string MachineName(MachineKind k)
        {
            switch (k)
            {
                case MachineKind.BasicPress: return "PRASA BASIC";
                case MachineKind.ImprovedPress: return "PRASA IMPROVED";
                case MachineKind.HighSpeedPress: return "PRASA HIGH-SPEED";
                case MachineKind.Recycler: return "RECYKLER";
                case MachineKind.Generator: return "GENERATOR";
                case MachineKind.ElectronicsAssembler: return "MONTAŻ ELEKTRONIKI";
                default: return "MASZYNA";
            }
        }

        private static string RecipeText(MachineKind k)
        {
            switch (k)
            {
                case MachineKind.BasicPress: return "1 stal + 1 energia\n→ 1 produkt + 1 złom";
                case MachineKind.ImprovedPress: return "1 stal + 1–2 energia\n→ 2 produkty + 1 złom";
                case MachineKind.HighSpeedPress: return "2 stal + 3 energia\n→ 3+ produkty + 2 złom";
                case MachineKind.Recycler: return "2 złom + 1 energia\n→ stal";
                case MachineKind.Generator: return "2 bitumen\n→ energia";
                case MachineKind.ElectronicsAssembler: return "elektronika + stal + 2 energia\n→ 3 produkty";
                default: return "schemat produkcji";
            }
        }
    }
}
