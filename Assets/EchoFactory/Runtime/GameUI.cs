using System;
using System.Collections.Generic;
using EchoFactory.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoFactory.Runtime
{
    /// <summary>
    /// Strategic game screen built with UI Toolkit. Layout and look live in Resources/UI/*.uxml and Game.uss,
    /// so they can be edited visually in UI Builder. This script only fills data and reacts to clicks;
    /// game rules stay in EchoFactory.Core.
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        private enum Screen { Menu, Map, Facility, Hall, Research, Market, Summary }

        [Header("Opcjonalne — puste pola są ładowane z Resources/UI")]
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private VisualTreeAsset parcelCard;
        [SerializeField] private VisualTreeAsset listCard;
        [SerializeField] private VisualTreeAsset resourceBox;
        [SerializeField] private VisualTreeAsset machineToken;
        [SerializeField] private VisualTreeAsset shopItem;
        [SerializeField] private VisualTreeAsset marketRow;
        [SerializeField] private PanelSettings panelSettings;

        private UIDocument document;
        private VisualElement root;
        private StrategicState state;
        private Screen screen = Screen.Menu;
        private int selectedParcel;
        private int selectedFacility = -1;
        private int selectedMachine = -1;
        private MachineKind? placing;
        private FactoryGame echoHall;

        private static readonly MachineKind[] AllMachines = { MachineKind.BasicPress, MachineKind.ImprovedPress, MachineKind.HighSpeedPress, MachineKind.Recycler, MachineKind.Generator, MachineKind.ElectronicsAssembler };
        private static readonly FacilityKind[] BuildableFacilities = { FacilityKind.ProductionHall, FacilityKind.LogisticsHall, FacilityKind.EnergyHall, FacilityKind.Workshop };
        private static readonly TechnologyKind[] Technologies = { TechnologyKind.BasicAutomation, TechnologyKind.ImprovedPress, TechnologyKind.AdvancedPress, TechnologyKind.SmartLogistics, TechnologyKind.EnergyEfficiency, TechnologyKind.AdvancedMaterials };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCamera();
            LoadMissingAssets();

            // Configure the document while it is disabled, so it builds the tree once with everything assigned.
            document = GetComponent<UIDocument>();
            if (document == null) document = gameObject.AddComponent<UIDocument>();
            document.enabled = false;
            if (document.panelSettings == null) document.panelSettings = panelSettings;
            if (document.visualTreeAsset == null) document.visualTreeAsset = layout;
            document.enabled = true;
        }

        private void Update()
        {
            // UIDocument rebuilds its tree when re-enabled or when the UXML is saved in UI Builder during Play.
            if (document != null && document.rootVisualElement != null && document.rootVisualElement != root)
            {
                Bind();
                Refresh();
                if (echoHall != null && echoHall.enabled) SetVisible(root, false);
            }
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.92f, .93f, .91f);
            go.transform.position = new Vector3(0, 0, -10);
        }

        private void LoadMissingAssets()
        {
            if (layout == null) layout = Resources.Load<VisualTreeAsset>("UI/Game");
            if (parcelCard == null) parcelCard = Resources.Load<VisualTreeAsset>("UI/ParcelCard");
            if (listCard == null) listCard = Resources.Load<VisualTreeAsset>("UI/ListCard");
            if (resourceBox == null) resourceBox = Resources.Load<VisualTreeAsset>("UI/ResourceBox");
            if (machineToken == null) machineToken = Resources.Load<VisualTreeAsset>("UI/Machine");
            if (shopItem == null) shopItem = Resources.Load<VisualTreeAsset>("UI/ShopItem");
            if (marketRow == null) marketRow = Resources.Load<VisualTreeAsset>("UI/MarketRow");
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Echo Factory Panel (runtime)";
                panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/EchoTheme");
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1440, 900);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
            }
        }

        // ------------------------------------------------------------------ binding

        private void Bind()
        {
            root = document.rootVisualElement;
            root.style.flexGrow = 1;

            Click("btn-new-game", NewGame);
            Click("btn-echo-hall", OpenEchoHall);
            Click("nav-map", () => Show(Screen.Map));
            Click("nav-facility", () => Show(Screen.Facility));
            Click("nav-research", () => Show(Screen.Research));
            Click("nav-market", () => Show(Screen.Market));
            Click("btn-end-turn", EndTurn);
            Click("btn-buy-parcel", BuyParcel);
            Click("btn-manage-parcel", () => Show(Screen.Facility));
            Click("btn-back-map", () => Show(Screen.Map));
            Click("btn-back-facility", () => { placing = null; Show(Screen.Facility); });
            Click("btn-cancel-place", () => { placing = null; Refresh(); });
            Click("btn-machine-toggle", ToggleMachine);
            Click("btn-build-lab", BuildResearchHall);
            Click("btn-next-turn", NextTurn);

            var floor = root.Q("hall-floor");
            if (floor != null) floor.RegisterCallback<ClickEvent>(OnFloorClicked);
        }

        private void Click(string name, Action action)
        {
            var button = root.Q<Button>(name);
            if (button == null) { Debug.LogWarning("Echo Factory UI: brak przycisku '" + name + "' w Game.uxml."); return; }
            button.clicked += () => { action(); };
        }

        private T Find<T>(string name) where T : VisualElement { return root == null ? null : root.Q<T>(name); }
        private void SetText(string name, string text) { var l = Find<Label>(name); if (l != null) l.text = text; }
        private static void SetVisible(VisualElement e, bool visible) { if (e != null) e.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }
        private void SetVisible(string name, bool visible) { SetVisible(Find<VisualElement>(name), visible); }
        private void SetEnabled(string name, bool enabled) { var e = Find<VisualElement>(name); if (e != null) e.SetEnabled(enabled); }
        private static void SetLabel(VisualElement parent, string name, string text) { var l = parent.Q<Label>(name); if (l != null) l.text = text; }

        private static VisualElement Spawn(VisualTreeAsset asset, VisualElement parent)
        {
            if (asset == null || parent == null) return null;
            var instance = asset.Instantiate();
            parent.Add(instance);
            return instance;
        }

        private void Message(string text, bool warning = false)
        {
            var label = Find<Label>("lbl-message");
            if (label == null) return;
            label.text = text;
            label.EnableInClassList("toast--warning", warning);
        }

        // ------------------------------------------------------------------ actions

        private void NewGame()
        {
            state = StrategicWorldGenerator.NewGame(Environment.TickCount);
            selectedParcel = 0;
            selectedFacility = state.Facilities[0].Id;
            selectedMachine = -1;
            placing = null;
            Message("Nowa gra. Masz jedną działkę i halę produkcyjną z prasą podstawową.");
            Show(Screen.Map);
        }

        private void OpenEchoHall()
        {
            if (echoHall == null)
            {
                echoHall = gameObject.AddComponent<FactoryGame>();
                echoHall.Exit = CloseEchoHall;
            }
            echoHall.enabled = true;
            SetVisible(root, false);
        }

        private void CloseEchoHall()
        {
            if (echoHall != null) echoHall.enabled = false;
            SetVisible(root, true);
            Refresh();
        }

        private void Show(Screen next)
        {
            if (state == null && next != Screen.Menu) next = Screen.Menu;
            screen = next;
            if (screen != Screen.Hall) placing = null;
            Refresh();
        }

        private void BuyParcel()
        {
            var p = ParcelSystem.Find(state, selectedParcel);
            if (ParcelSystem.Buy(state, selectedParcel)) { Message("Kupiono działkę " + (p.Id + 1) + "."); Show(Screen.Facility); }
            else Message("Nie można kupić tej działki.", true);
        }

        private void BuildFacility(FacilityKind kind)
        {
            if (FacilitySystem.Build(state, selectedParcel, kind))
            {
                selectedFacility = state.Facilities[state.Facilities.Count - 1].Id;
                Message("Zbudowano: " + Names.Facility(kind) + ". Otwórz halę, aby ustawić maszyny.");
            }
            else Message("Nie można zbudować: " + Names.Facility(kind) + ".", true);
            Refresh();
        }

        private void OpenHall(int facilityId)
        {
            selectedFacility = facilityId;
            selectedMachine = -1;
            FactoryLayoutSystem.EnsureLayout(state, facilityId);
            Show(Screen.Hall);
        }

        private void StartPlacing(MachineKind kind)
        {
            placing = kind;
            Message("Kliknij wolne miejsce na hali, aby postawić: " + Names.Machine(kind) + ".");
            Refresh();
        }

        private void OnFloorClicked(ClickEvent evt)
        {
            var floor = evt.currentTarget as VisualElement;
            if (floor == null || evt.target != floor || placing == null || state == null) return;
            Vector2 local = floor.WorldToLocal(evt.position);
            float w = floor.resolvedStyle.width, h = floor.resolvedStyle.height;
            if (w <= 0 || h <= 0) return;
            float x = Mathf.Clamp01(local.x / w), y = Mathf.Clamp01(local.y / h);
            var kind = placing.Value;
            if (FactoryLayoutSystem.PlaceMachine(state, selectedFacility, kind, x, y))
            {
                var f = FindFacility(selectedFacility);
                selectedMachine = f.Machines[f.Machines.Count - 1].Id;
                placing = null;
                Message("Postawiono: " + Names.Machine(kind) + ".");
            }
            else Message("Tu nie można postawić maszyny — za blisko innej albo brak środków.", true);
            Refresh();
        }

        private void ToggleMachine()
        {
            var m = FindMachine(selectedMachine);
            if (m == null || state.Phase != StrategicPhase.Planning) return;
            m.Enabled = !m.Enabled;
            Message(Names.Machine(m.Kind) + (m.Enabled ? " włączona." : " wyłączona."));
            Refresh();
        }

        private void BuildResearchHall()
        {
            var parcel = ParcelSystem.Find(state, selectedParcel);
            int parcelId = parcel != null && parcel.Owned ? parcel.Id : 0;
            if (ResearchSystem.BuildResearchHall(state, parcelId)) Message("Zbudowano Halę Badawczą na działce " + (parcelId + 1) + ".");
            else Message("Nie można zbudować Hali Badawczej (7500 C, jedna na sieć).", true);
            Refresh();
        }

        private void Research(TechnologyKind kind)
        {
            if (ResearchSystem.UnlockTechnology(state, kind)) Message("Odblokowano: " + Names.Technology(kind) + ".");
            else Message("Nie można jeszcze zbadać: " + Names.Technology(kind) + ".", true);
            Refresh();
        }

        private void EndTurn()
        {
            if (state == null || state.Phase != StrategicPhase.Planning) return;
            TurnSystem.EndTurn(state);
            Message("");
            Show(Screen.Summary);
        }

        private void NextTurn()
        {
            if (state == null || state.Phase != StrategicPhase.Summary) return;
            TurnSystem.ContinueToPlanning(state);
            Show(Screen.Map);
        }

        // ------------------------------------------------------------------ drawing

        private void Refresh()
        {
            if (root == null) return;
            SetVisible("screen-menu", screen == Screen.Menu);
            SetVisible("game", screen != Screen.Menu);
            SetVisible("screen-map", screen == Screen.Map);
            SetVisible("screen-facility", screen == Screen.Facility);
            SetVisible("screen-hall", screen == Screen.Hall);
            SetVisible("screen-research", screen == Screen.Research);
            SetVisible("screen-market", screen == Screen.Market);
            SetVisible("screen-summary", screen == Screen.Summary);
            if (state == null) return;

            RefreshTopBar();
            if (screen == Screen.Map) RefreshMap();
            else if (screen == Screen.Facility) RefreshFacility();
            else if (screen == Screen.Hall) RefreshHall();
            else if (screen == Screen.Research) RefreshResearch();
            else if (screen == Screen.Market) RefreshMarket();
            else if (screen == Screen.Summary) RefreshSummary();
        }

        private void RefreshTopBar()
        {
            bool planning = state.Phase == StrategicPhase.Planning;
            SetText("lbl-turn", "TURA " + state.Turn + "  ·  " + (planning ? "PLANOWANIE" : "PODSUMOWANIE"));
            SetText("lbl-credits", state.Credits + " C");
            SetText("lbl-stock", "STAL " + state.GetStock(ResourceKind.Steel) + "  |  EN " + state.GetStock(ResourceKind.Energy) + "  |  ZŁOM " + state.GetStock(ResourceKind.Scrap) + "  |  GOTOWE " + state.GetStock(ResourceKind.FinishedGoods));
            SetEnabled("btn-end-turn", planning);
            SetEnabled("nav-map", planning);
            SetEnabled("nav-facility", planning);
            SetEnabled("nav-research", planning);
            SetEnabled("nav-market", planning);
            Find<VisualElement>("nav-market")?.EnableInClassList("btn--selected", screen == Screen.Market);
            Find<VisualElement>("nav-map")?.EnableInClassList("btn--selected", screen == Screen.Map);
            Find<VisualElement>("nav-facility")?.EnableInClassList("btn--selected", screen == Screen.Facility || screen == Screen.Hall);
            Find<VisualElement>("nav-research")?.EnableInClassList("btn--selected", screen == Screen.Research);
        }

        private void RefreshMap()
        {
            var grid = Find<VisualElement>("parcel-grid");
            if (grid != null)
            {
                grid.Clear();
                foreach (var p in state.Parcels)
                {
                    var card = Spawn(parcelCard, grid);
                    if (card == null) break;
                    var c = card.Q("card") ?? card;
                    c.EnableInClassList("parcel-card--owned", p.Owned);
                    c.EnableInClassList("parcel-card--selected", p.Id == selectedParcel);
                    SetLabel(card, "name", "DZIAŁKA " + (p.Id + 1) + (p.Owned ? " · TWOJA" : ""));
                    SetLabel(card, "resource", Names.Resource(p.Resource));
                    SetLabel(card, "stats", "Zasób: " + p.ResourceRichness + " · Log: " + Names.Signed(p.LogisticsBonus));
                    SetLabel(card, "price", p.Size + " m² · " + p.Price + " C");
                    int id = p.Id;
                    var open = card.Q<Button>("open");
                    if (open != null) { open.text = id == selectedParcel ? "WYBRANA" : "WYBIERZ"; open.clicked += () => { selectedParcel = id; Refresh(); }; }
                }
            }

            var parcel = ParcelSystem.Find(state, selectedParcel);
            if (parcel == null) return;
            SetText("lbl-parcel-title", "DZIAŁKA " + (parcel.Id + 1));
            SetText("lbl-parcel-info", "Surowiec: " + Names.Resource(parcel.Resource) + "\nBogactwo: " + parcel.ResourceRichness + "/100\nPremia logistyczna: " + Names.Signed(parcel.LogisticsBonus) + "%\nPowierzchnia: " + parcel.Size + " m²");
            var buy = Find<Button>("btn-buy-parcel");
            if (buy != null) { buy.text = "KUP DZIAŁKĘ · " + parcel.Price + " C"; buy.SetEnabled(ParcelSystem.CanBuy(state, parcel.Id)); }
            SetVisible("btn-buy-parcel", !parcel.Owned);
            SetText("lbl-parcel-hint", parcel.Owned ? "" : state.Credits < parcel.Price ? "Brakuje kapitału. Rozbuduj produkcję albo poczekaj na przychód." : "Zakup działki odblokuje budowę hal.");
            SetVisible("lbl-parcel-objects", parcel.Owned);
            SetVisible("btn-manage-parcel", parcel.Owned);

            var list = Find<VisualElement>("parcel-facilities");
            if (list != null)
            {
                list.Clear();
                if (parcel.Owned)
                    foreach (var f in state.Facilities)
                        if (f.ParcelId == parcel.Id)
                            list.Add(new Label("#" + f.Id + "  " + Names.Facility(f.Kind) + " · " + f.Machines.Count + "/" + f.MachineSlots + " maszyn") { name = "facility-row" });
            }
        }

        private void RefreshFacility()
        {
            var parcel = ParcelSystem.Find(state, selectedParcel);
            SetText("lbl-facility-title", "ZARZĄDZANIE · DZIAŁKA " + (selectedParcel + 1));
            bool owned = parcel != null && parcel.Owned;
            SetText("lbl-facility-hint", owned ? "" : "Ta działka nie jest jeszcze twoja. Wróć na mapę i kup ją.");

            var list = Find<VisualElement>("facility-list");
            if (list != null)
            {
                list.Clear();
                if (owned)
                    foreach (var f in state.Facilities)
                    {
                        if (f.ParcelId != selectedParcel) continue;
                        var card = Spawn(listCard, list);
                        if (card == null) break;
                        SetLabel(card, "title", "#" + f.Id + "  " + Names.Facility(f.Kind));
                        SetLabel(card, "detail", f.Kind == FacilityKind.ResearchHall ? "Odblokowuje drzewo R&D." : "Maszyny: " + f.Machines.Count + "/" + f.MachineSlots + " · dochód: " + f.PassiveIncomePerTurn + "/turę");
                        var action = card.Q<Button>("action");
                        int id = f.Id;
                        if (action != null)
                        {
                            bool research = f.Kind == FacilityKind.ResearchHall;
                            action.text = research ? "R&D" : "OTWÓRZ HALĘ";
                            action.EnableInClassList("btn--primary", id == selectedFacility);
                            action.clicked += () => { if (research) Show(Screen.Research); else OpenHall(id); };
                        }
                    }
            }

            var build = Find<VisualElement>("build-list");
            if (build != null)
            {
                build.Clear();
                foreach (var kind in BuildableFacilities)
                {
                    var card = Spawn(listCard, build);
                    if (card == null) break;
                    SetLabel(card, "title", Names.Facility(kind));
                    SetLabel(card, "detail", "Cena " + FacilitySystem.GetPrice(kind) + " C · " + FacilitySystem.GetSlots(kind) + " sloty · " + Names.FacilityPurpose(kind));
                    var action = card.Q<Button>("action");
                    var k = kind;
                    if (action != null)
                    {
                        action.text = "BUDUJ";
                        action.AddToClassList("btn--primary");
                        action.SetEnabled(owned && FacilitySystem.CanBuild(state, selectedParcel, kind));
                        action.clicked += () => BuildFacility(k);
                    }
                }
            }
        }

        private void RefreshHall()
        {
            var f = FindFacility(selectedFacility);
            if (f == null) { Show(Screen.Facility); return; }
            SetText("lbl-hall-title", "HALA #" + f.Id + "  ·  " + Names.Facility(f.Kind));
            SetText("lbl-hall-slots", "Sloty: " + f.Machines.Count + " / " + f.MachineSlots);
            SetText("lbl-floor-caption", placing != null ? "USTAWIANIE: " + Names.Machine(placing.Value) + " · kliknij wolne miejsce" : "Kliknij maszynę, aby ją wybrać. Maszyny pracują podczas rozwiązania tury.");

            var inputs = Find<VisualElement>("hall-inputs");
            if (inputs != null)
            {
                inputs.Clear();
                foreach (var r in new[] { ResourceKind.Steel, ResourceKind.Energy, ResourceKind.Bitumen, ResourceKind.Electronics }) AddResource(inputs, r);
            }
            var outputs = Find<VisualElement>("hall-outputs");
            if (outputs != null)
            {
                outputs.Clear();
                AddResource(outputs, ResourceKind.FinishedGoods);
                AddResource(outputs, ResourceKind.Scrap);
            }

            var floor = Find<VisualElement>("hall-floor");
            if (floor != null)
            {
                floor.EnableInClassList("hall-floor--placing", placing != null);
                foreach (var old in floor.Query<VisualElement>(className: "machine-slot").ToList()) old.RemoveFromHierarchy();
                foreach (var m in f.Machines)
                {
                    var token = Spawn(machineToken, floor);
                    if (token == null) break;
                    // The template container is the positioned element so the art keeps its own size.
                    token.AddToClassList("machine-slot");
                    token.pickingMode = PickingMode.Ignore;
                    token.style.position = Position.Absolute;
                    token.style.left = Length.Percent(Mathf.Clamp01(m.X) * 100f);
                    token.style.top = Length.Percent(Mathf.Clamp01(m.Y) * 100f);
                    var body = token.Q("machine");
                    if (body == null) continue;
                    body.RemoveFromClassList("machine--BasicPress");
                    body.AddToClassList("machine--" + m.Kind);
                    body.EnableInClassList("machine--selected", m.Id == selectedMachine);
                    body.EnableInClassList("machine--disabled", !m.Enabled);
                    SetLabel(token, "label", Names.MachineShort(m.Kind));
                    int id = m.Id;
                    body.RegisterCallback<ClickEvent>(e => { selectedMachine = id; placing = null; e.StopPropagation(); Refresh(); });
                }
            }

            var selected = FindMachine(selectedMachine);
            SetText("lbl-machine-name", selected == null ? "—" : Names.Machine(selected.Kind));
            SetText("lbl-machine-info", selected == null ? "Kliknij maszynę na hali." : Names.Recipe(selected.Kind) + "\n" + (selected.Enabled ? "Status: pracuje" : "Status: wyłączona"));
            var toggle = Find<Button>("btn-machine-toggle");
            if (toggle != null) { toggle.text = selected != null && !selected.Enabled ? "WŁĄCZ" : "WYŁĄCZ"; toggle.SetEnabled(selected != null && state.Phase == StrategicPhase.Planning); }

            var shop = Find<VisualElement>("machine-shop");
            if (shop != null)
            {
                shop.Clear();
                foreach (var kind in AllMachines)
                {
                    if (!FactoryLayoutSystem.IsMachineAllowed(f.Kind, kind)) continue;
                    var item = Spawn(shopItem, shop);
                    if (item == null) break;
                    var icon = item.Q("icon");
                    if (icon != null) { icon.RemoveFromClassList("machine--BasicPress"); icon.AddToClassList("machine--" + kind); }
                    var buy = item.Q<Button>("buy");
                    var k = kind;
                    if (buy != null)
                    {
                        buy.text = Names.Machine(kind) + " · " + ProductionSystem.GetMachinePrice(kind) + " C";
                        buy.SetEnabled(ProductionSystem.CanBuildMachine(state, f.Id, kind));
                        buy.EnableInClassList("btn--selected", placing == kind);
                        buy.clicked += () => StartPlacing(k);
                    }
                }
                if (shop.childCount == 0) shop.Add(new Label("Ten typ hali nie przyjmuje jeszcze maszyn.") { name = "shop-empty" });
            }
            SetText("lbl-shop-hint", f.Machines.Count >= f.MachineSlots ? "Brak wolnych slotów w tej hali." : "Wybierz maszynę, potem kliknij wolne miejsce na hali. Szare = brak technologii lub środków.");
            SetVisible("btn-cancel-place", placing != null);
        }

        private void AddResource(VisualElement parent, ResourceKind kind)
        {
            var box = Spawn(resourceBox, parent);
            if (box == null) return;
            SetLabel(box, "name", Names.Resource(kind));
            SetLabel(box, "amount", "STAN  " + state.GetStock(kind));
            var b = box.Q("box");
            if (b != null) b.EnableInClassList("resource-box--waste", kind == ResourceKind.Scrap);
        }

        private void RefreshResearch()
        {
            bool hall = ResearchSystem.HasResearchHall(state);
            SetText("lbl-research-hint", hall ? "Laboratorium aktywne. Technologie zmieniają reguły produkcji." : "Najpierw zbuduj Halę Badawczą na własnej działce (" + ResearchSystem.ResearchHallPrice + " C).");
            var build = Find<Button>("btn-build-lab");
            if (build != null) { build.SetEnabled(!hall && state.Phase == StrategicPhase.Planning && state.Credits >= ResearchSystem.ResearchHallPrice); build.text = hall ? "HALA BADAWCZA ISTNIEJE" : "ZBUDUJ HALĘ BADAWCZĄ · " + ResearchSystem.ResearchHallPrice + " C"; }

            var list = Find<VisualElement>("tech-list");
            if (list == null) return;
            list.Clear();
            foreach (var kind in Technologies)
            {
                var tech = FindTech(kind);
                if (tech == null) continue;
                var card = Spawn(listCard, list);
                if (card == null) break;
                SetLabel(card, "title", Names.Technology(kind));
                SetLabel(card, "detail", "Koszt: " + tech.ResearchCost + " C · " + Names.TechRequirement(kind));
                var action = card.Q<Button>("action");
                var k = kind;
                if (action != null)
                {
                    action.text = tech.Unlocked ? "ODBLOKOWANA" : "BADAJ";
                    action.EnableInClassList("btn--primary", !tech.Unlocked);
                    action.EnableInClassList("btn--selected", tech.Unlocked);
                    action.SetEnabled(!tech.Unlocked && ResearchSystem.CanUnlock(state, kind));
                    action.clicked += () => Research(k);
                }
            }
        }

        private static readonly ResourceKind[] MarketGoods = { ResourceKind.Steel, ResourceKind.Energy, ResourceKind.Bitumen, ResourceKind.Electronics, ResourceKind.Scrap, ResourceKind.FinishedGoods };

        private void RefreshMarket()
        {
            SetText("lbl-market-hint", "Ceny rosną, gdy kupujesz, i spadają, gdy sprzedajesz. Co turę wracają do normy. Gotowe produkty nie sprzedają się same.");
            var list = Find<VisualElement>("market-list");
            if (list == null) return;
            list.Clear();
            foreach (var kind in MarketGoods)
            {
                var row = Spawn(marketRow, list);
                if (row == null) break;
                bool canBuy = MarketSystem.CanBuyKind(kind);
                SetLabel(row, "title", ResourceName(kind).ToUpperInvariant() + " · masz " + state.GetStock(kind));
                SetLabel(row, "detail", canBuy ? "Kupno " + MarketSystem.BuyPrice(state, kind) + " C / szt." : "Sprzedaż " + MarketSystem.SellPrice(state, kind) + " C / szt.");
                BindTrade(row, "buy-1", canBuy, "KUP 1", () => Trade(kind, 1, true), MarketSystem.CanBuy(state, kind, 1));
                BindTrade(row, "buy-5", canBuy, "KUP 5", () => Trade(kind, 5, true), MarketSystem.CanBuy(state, kind, 5));
                BindTrade(row, "sell-1", !canBuy, "SPRZEDAJ 1", () => Trade(kind, 1, false), MarketSystem.CanSell(state, kind, 1));
                BindTrade(row, "sell-all", !canBuy, "SPRZEDAJ WSZYSTKO", () => Trade(kind, state.GetStock(kind), false), MarketSystem.CanSell(state, kind, state.GetStock(kind)));
            }
        }

        private static void BindTrade(VisualElement row, string name, bool visible, string text, Action action, bool enabled)
        {
            var button = row.Q<Button>(name);
            if (button == null) return;
            SetVisible(button, visible);
            button.text = text;
            button.SetEnabled(enabled);
            button.clicked += action;
        }

        private void Trade(ResourceKind kind, int amount, bool buy)
        {
            if (state == null) return;
            bool ok = buy ? MarketSystem.Buy(state, kind, amount) : MarketSystem.Sell(state, kind, amount);
            Message(ok ? "" : "Transakcja niemożliwa.", !ok);
            Refresh();
        }

        private string ExtractionText()
        {
            string text = "";
            foreach (var p in state.LastExtracted) text += (text.Length > 0 ? ", " : "") + ResourceName(p.Key) + " +" + p.Value;
            return text.Length > 0 ? text : "brak";
        }

        private static string ResourceName(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Steel: return "stal";
                case ResourceKind.Energy: return "energia";
                case ResourceKind.Bitumen: return "bitum";
                case ResourceKind.Scrap: return "złom";
                case ResourceKind.Electronics: return "elektronika";
                default: return kind.ToString();
            }
        }

        private void RefreshSummary()
        {
            SetText("lbl-summary-title", "PODSUMOWANIE TURY " + state.Turn);
            SetText("lbl-summary-body",
                "Produkcja: " + state.LastTurnProduction + " szt.\n" +
                "Przychód: +" + state.LastTurnIncome + " C (w tym pasywny +" + state.LastPassiveIncome + " C)\n" +
                "Koszt operacyjny: -" + state.LastTurnCosts + " C\n" +
                "Wydobycie: " + ExtractionText() + "\n\n" +
                "Stal: " + state.GetStock(ResourceKind.Steel) + " · Energia: " + state.GetStock(ResourceKind.Energy) + " · Złom: " + state.GetStock(ResourceKind.Scrap) + "\n\n" +
                "Saldo: " + state.Credits + " C");
            var verdict = Find<Label>("lbl-summary-verdict");
            if (verdict != null)
            {
                bool idle = state.LastTurnProduction == 0;
                verdict.text = idle ? "SYSTEM NIE PRODUKOWAŁ.\n\nSprawdź surowce, energię i dostępne maszyny." : "SYSTEM DZIAŁA.\n\nCo stało się wąskim gardłem?";
                verdict.EnableInClassList("amber", idle);
                verdict.EnableInClassList("accent", !idle);
            }
        }

        // ------------------------------------------------------------------ lookups

        private FacilityState FindFacility(int id)
        {
            if (state == null) return null;
            foreach (var f in state.Facilities) if (f.Id == id) return f;
            return null;
        }

        private MachineState FindMachine(int id)
        {
            var f = FindFacility(selectedFacility);
            if (f == null) return null;
            foreach (var m in f.Machines) if (m.Id == id) return m;
            return null;
        }

        private TechnologyState FindTech(TechnologyKind kind)
        {
            foreach (var t in state.Technologies) if (t.Kind == kind) return t;
            return null;
        }
    }
}
