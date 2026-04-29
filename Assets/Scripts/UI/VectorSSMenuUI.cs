using System;
using System.Collections.Generic;
using System.Reflection;
using GTX.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GTX.UI
{
    public enum VectorSSMenuScreen
    {
        MainMenu,
        MapSelect,
        VehicleSelect,
        Garage,
        Results
    }

    [DisallowMultipleComponent]
    public sealed class VectorSSMenuUI : MonoBehaviour
    {
        private const string TitleText = "VECTOR SS";
        private const string VersionText = "0.1.0";
        private const string NoSelectionText = "NONE";
        private const float RowHeight = 104f;
        private const float SmallRowHeight = 82f;

        private static readonly Color Paper = new Color(0.94f, 0.92f, 0.86f, 0.98f);
        private static readonly Color PaperDim = new Color(0.82f, 0.80f, 0.74f, 0.98f);
        private static readonly Color Ink = new Color(0.02f, 0.02f, 0.018f, 1f);
        private static readonly Color InkSoft = new Color(0.12f, 0.12f, 0.105f, 1f);
        private static readonly Color Blue = new Color(0.13f, 0.36f, 0.48f, 1f);
        private static readonly Color Orange = new Color(0.78f, 0.32f, 0.075f, 1f);
        private static readonly Color Green = new Color(0.42f, 0.50f, 0.24f, 1f);

        [Header("Startup")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool showOnAwake = true;
        [SerializeField] private bool autoAdvanceScreens = true;
        [SerializeField] private VectorSSMenuScreen initialScreen = VectorSSMenuScreen.MainMenu;
        [SerializeField] private int sortingOrder = 180;

        [Header("Optional Canvas")]
        [SerializeField] private Canvas targetCanvas;

        [Header("Unity Events")]
        [SerializeField] private UnityEvent startRequestedUnityEvent = new UnityEvent();
        [SerializeField] private StringEvent mapSelectedUnityEvent = new StringEvent();
        [SerializeField] private StringEvent vehicleSelectedUnityEvent = new StringEvent();
        [SerializeField] private UnityEvent garageBackUnityEvent = new UnityEvent();
        [SerializeField] private RaceStartUnityEvent raceStartUnityEvent = new RaceStartUnityEvent();
        [SerializeField] private StringEvent purchaseUpgradeUnityEvent = new StringEvent();
        [SerializeField] private StringFloatEvent tuningChangedUnityEvent = new StringFloatEvent();
        [SerializeField] private UnityEvent resultsContinueUnityEvent = new UnityEvent();

        private readonly Dictionary<VectorSSMenuScreen, GameObject> screens = new Dictionary<VectorSSMenuScreen, GameObject>();
        private readonly List<MapOption> maps = new List<MapOption>();
        private readonly List<VehicleOption> vehicles = new List<VehicleOption>();
        private readonly List<UpgradeOption> upgrades = new List<UpgradeOption>();

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform root;
        private Font font;
        private MenuCallbacks callbacks;
        private ResultsState results;
        private GTXTuningProfile tuning;
        private string pilotName = "ROOKIE";
        private string selectedMapId;
        private string selectedVehicleId;
        private int credits = 350;
        private bool built;
        private bool suppressTuningEvents;

        private Text mainStatusText;
        private Text mainCreditsText;
        private Text mapSelectedText;
        private Text vehicleSelectedText;
        private Text vehicleMapText;
        private Text garageCreditsText;
        private Text garageVehicleText;
        private Text resultsTitleText;
        private Text resultsBodyText;
        private RectTransform mapListRoot;
        private RectTransform vehicleListRoot;
        private RectTransform upgradeListRoot;
        private RectTransform tuningListRoot;
        private Button mapNextButton;
        private Button vehicleGarageButton;
        private Button raceStartButton;

        public event Action StartRequested;
        public event Action<string> MapSelected;
        public event Action<string> VehicleSelected;
        public event Action GarageBackRequested;
        public event Action<RaceStartRequest> RaceStartRequested;
        public event Action<string> PurchaseUpgradeRequested;
        public event Action<string, float> TuningChanged;
        public event Action ResultsContinueRequested;

        public string SelectedMapId { get { return selectedMapId; } }
        public string SelectedVehicleId { get { return selectedVehicleId; } }
        public int Credits { get { return credits; } }
        public GTXTuningProfile CurrentTuning { get { EnsureDefaultData(); return tuning; } }
        public bool AutoAdvanceScreens { get { return autoAdvanceScreens; } set { autoAdvanceScreens = value; } }

        public void Bind(VectorSSMenuBinding binding)
        {
            if (binding == null)
            {
                EnsureDefaultData();
                RefreshAllScreens();
                return;
            }

            pilotName = string.IsNullOrWhiteSpace(binding.pilotName) ? "ROOKIE" : binding.pilotName.Trim();
            credits = Mathf.Max(0, binding.credits);
            CopyOptions(binding.maps, maps);
            CopyOptions(binding.vehicles, vehicles);
            CopyOptions(binding.upgrades, upgrades);
            selectedMapId = binding.selectedMapId;
            selectedVehicleId = binding.selectedVehicleId;
            tuning = CloneTuning(binding.tuning);
            results = binding.results != null ? binding.results.Copy() : ResultsState.Default();

            EnsureDefaultData();
            RefreshAllScreens();
        }

        public void Bind(VectorSSMenuBinding binding, MenuCallbacks newCallbacks)
        {
            BindCallbacks(newCallbacks);
            Bind(binding);
        }

        public void BindCallbacks(MenuCallbacks newCallbacks)
        {
            callbacks = newCallbacks;
        }

        public void SetResults(ResultsState nextResults)
        {
            results = nextResults != null ? nextResults.Copy() : ResultsState.Default();
            RefreshResults();
        }

        public void SetCredits(int nextCredits)
        {
            credits = Mathf.Max(0, nextCredits);
            RefreshAllScreens();
        }

        public void SetSelections(string mapId, string vehicleId)
        {
            selectedMapId = mapId;
            selectedVehicleId = vehicleId;
            EnsureDefaultData();
            RefreshAllScreens();
        }

        public void Show(VectorSSMenuScreen screen)
        {
            EnsureBuilt();
            EnsureDefaultData();
            SetVisible(true);

            foreach (KeyValuePair<VectorSSMenuScreen, GameObject> pair in screens)
            {
                pair.Value.SetActive(pair.Key == screen);
            }

            RefreshAllScreens();
        }

        public void ShowMainMenu()
        {
            Show(VectorSSMenuScreen.MainMenu);
        }

        public void ShowMapSelect()
        {
            Show(VectorSSMenuScreen.MapSelect);
        }

        public void ShowVehicleSelect()
        {
            Show(VectorSSMenuScreen.VehicleSelect);
        }

        public void ShowGarage()
        {
            Show(VectorSSMenuScreen.Garage);
        }

        public void ShowResults()
        {
            Show(VectorSSMenuScreen.Results);
        }

        public void Hide()
        {
            EnsureBuilt();
            SetVisible(false);
        }

        private void Awake()
        {
            font = LoadBuiltInFont();
            EnsureDefaultData();

            if (buildOnAwake)
            {
                EnsureBuilt();
            }

            if (showOnAwake)
            {
                Show(initialScreen);
            }
            else if (built)
            {
                SetVisible(false);
            }
        }

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildScreens();
            built = true;
            RefreshAllScreens();
        }

        private void BuildCanvas()
        {
            canvas = targetCanvas;
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Vector SS Menu Canvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = sortingOrder;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            canvas.sortingOrder = sortingOrder;
            ConfigureCanvasScaler(canvas);

            GameObject rootObject = new GameObject("Vector SS Menu Root");
            rootObject.transform.SetParent(canvas.transform, false);
            root = rootObject.AddComponent<RectTransform>();
            Stretch(root);
            Image background = rootObject.AddComponent<Image>();
            background.color = Paper;
            canvasGroup = rootObject.AddComponent<CanvasGroup>();
        }

        private void BuildScreens()
        {
            BuildMainMenu();
            BuildMapSelect();
            BuildVehicleSelect();
            BuildGarageScreen();
            BuildResultsScreen();
        }

        private void BuildMainMenu()
        {
            GameObject screen = CreateScreen(VectorSSMenuScreen.MainMenu, "Main Menu");
            CreateAccentBars(screen.transform);
            Label("Title", screen.transform, TitleText, 78, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(86f, -92f), new Vector2(600f, 86f), Ink);
            Label("Version", screen.transform, VersionText + " MENU SHELL", 18, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(92f, -166f), new Vector2(260f, 28f), InkSoft);

            GameObject statusPanel = Panel("Status Panel", screen.transform, Anchor.TopRight, new Vector2(-86f, -92f), new Vector2(520f, 230f), Paper, true);
            Label("Pilot Label", statusPanel.transform, "PILOT", 13, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(22f, -20f), new Vector2(90f, 22f), InkSoft);
            mainStatusText = Label("Status Text", statusPanel.transform, string.Empty, 24, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(22f, -54f), new Vector2(472f, 116f), Ink);
            mainCreditsText = Label("Credits Text", statusPanel.transform, string.Empty, 20, TextAnchor.MiddleLeft, Anchor.BottomLeft, new Vector2(22f, 22f), new Vector2(260f, 28f), Ink);

            RectTransform menuStack = Stack("Main Menu Stack", screen.transform, Anchor.BottomLeft, new Vector2(86f, 110f), new Vector2(420f, 330f), 16f);
            Button("Start Button", menuStack, "START", delegate { RequestStart(); }, 76f, 28);
            Button("Map Select Button", menuStack, "MAP SELECT", delegate { ShowMapSelect(); }, 62f, 20);
            Button("Vehicle Select Button", menuStack, "VEHICLE SELECT", delegate { ShowVehicleSelect(); }, 62f, 20);
            Button("Garage Button", menuStack, "GARAGE", delegate { ShowGarage(); }, 62f, 20);
            Button("Results Button", menuStack, "RESULTS", delegate { ShowResults(); }, 62f, 20);

            GameObject note = Panel("Menu Note", screen.transform, Anchor.BottomRight, new Vector2(-86f, 110f), new Vector2(520f, 142f), Paper, true);
            Label("Note Title", note.transform, "RUNTIME CANVAS", 16, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(20f, -18f), new Vector2(220f, 24f), Ink);
            Label("Note Text", note.transform, "Blackline race session armed. Choose a route, tune the machine, and stage clean.", 20, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(20f, -50f), new Vector2(470f, 74f), InkSoft);
        }

        private void BuildMapSelect()
        {
            GameObject screen = CreateScreen(VectorSSMenuScreen.MapSelect, "Map Select");
            CreateScreenHeader(screen.transform, "MAP SELECT", "Choose the course contract.", delegate { ShowMainMenu(); });

            mapListRoot = Stack("Map List", screen.transform, Anchor.TopLeft, new Vector2(86f, -220f), new Vector2(910f, 600f), 14f);

            GameObject sidePanel = Panel("Map Side Panel", screen.transform, Anchor.TopRight, new Vector2(-86f, -220f), new Vector2(640f, 380f), Paper, true);
            Label("Selected Map Label", sidePanel.transform, "SELECTED MAP", 14, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(24f, -24f), new Vector2(180f, 24f), InkSoft);
            mapSelectedText = Label("Selected Map Text", sidePanel.transform, string.Empty, 34, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(24f, -68f), new Vector2(590f, 112f), Ink);
            Label("Map Hint", sidePanel.transform, "Surface notes, best-time marks, and unlock states sit here when the season board is ready.", 19, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(24f, -202f), new Vector2(590f, 100f), InkSoft);
            mapNextButton = Button("Map Next", sidePanel.transform, "NEXT", delegate { ShowVehicleSelect(); }, Anchor.BottomRight, new Vector2(-24f, 24f), new Vector2(160f, 56f), 20);
        }

        private void BuildVehicleSelect()
        {
            GameObject screen = CreateScreen(VectorSSMenuScreen.VehicleSelect, "Vehicle Select");
            CreateScreenHeader(screen.transform, "VEHICLE SELECT", "Pick a chassis before staging.", delegate { ShowMapSelect(); });

            vehicleListRoot = Stack("Vehicle List", screen.transform, Anchor.TopLeft, new Vector2(86f, -220f), new Vector2(980f, 600f), 14f);

            GameObject sidePanel = Panel("Vehicle Side Panel", screen.transform, Anchor.TopRight, new Vector2(-86f, -220f), new Vector2(570f, 450f), Paper, true);
            Label("Vehicle Map Label", sidePanel.transform, "MAP", 14, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(24f, -24f), new Vector2(90f, 24f), InkSoft);
            vehicleMapText = Label("Vehicle Map Text", sidePanel.transform, string.Empty, 23, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(24f, -58f), new Vector2(520f, 70f), Ink);
            Label("Vehicle Label", sidePanel.transform, "VEHICLE", 14, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(24f, -148f), new Vector2(120f, 24f), InkSoft);
            vehicleSelectedText = Label("Vehicle Selected Text", sidePanel.transform, string.Empty, 31, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(24f, -184f), new Vector2(520f, 92f), Ink);
            vehicleGarageButton = Button("Vehicle Garage Button", sidePanel.transform, "GARAGE", delegate { ShowGarage(); }, Anchor.BottomLeft, new Vector2(24f, 28f), new Vector2(190f, 56f), 20);
            raceStartButton = Button("Race Start Button", sidePanel.transform, "RACE", delegate { RequestRaceStart(); }, Anchor.BottomRight, new Vector2(-24f, 28f), new Vector2(190f, 56f), 20);
        }

        private void BuildGarageScreen()
        {
            GameObject screen = CreateScreen(VectorSSMenuScreen.Garage, "Garage");
            CreateScreenHeader(screen.transform, "GARAGE", "Upgrade bay and tuning deck.", delegate { RequestGarageBack(); });

            GameObject leftPanel = Panel("Upgrade Panel", screen.transform, Anchor.TopLeft, new Vector2(86f, -212f), new Vector2(820f, 660f), Paper, true);
            Label("Upgrade Title", leftPanel.transform, "UPGRADES", 20, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(24f, -22f), new Vector2(210f, 30f), Ink);
            upgradeListRoot = Stack("Upgrade List", leftPanel.transform, Anchor.TopLeft, new Vector2(24f, -76f), new Vector2(772f, 520f), 12f);
            garageCreditsText = Label("Garage Credits", leftPanel.transform, string.Empty, 22, TextAnchor.MiddleLeft, Anchor.BottomLeft, new Vector2(24f, 26f), new Vector2(260f, 28f), Ink);

            GameObject rightPanel = Panel("Tuning Panel", screen.transform, Anchor.TopRight, new Vector2(-86f, -212f), new Vector2(820f, 660f), Paper, true);
            Label("Tuning Title", rightPanel.transform, "TUNING", 20, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(24f, -22f), new Vector2(180f, 30f), Ink);
            garageVehicleText = Label("Garage Vehicle", rightPanel.transform, string.Empty, 17, TextAnchor.MiddleRight, Anchor.TopRight, new Vector2(-24f, -22f), new Vector2(420f, 30f), InkSoft);
            tuningListRoot = Stack("Tuning List", rightPanel.transform, Anchor.TopLeft, new Vector2(24f, -86f), new Vector2(772f, 490f), 16f);
            Button("Garage Back Button", rightPanel.transform, "BACK", delegate { RequestGarageBack(); }, Anchor.BottomRight, new Vector2(-24f, 26f), new Vector2(160f, 54f), 20);
        }

        private void BuildResultsScreen()
        {
            GameObject screen = CreateScreen(VectorSSMenuScreen.Results, "Results");
            CreateAccentBars(screen.transform);
            Label("Results Header", screen.transform, "RESULTS", 62, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(86f, -92f), new Vector2(520f, 78f), Ink);

            GameObject resultPanel = Panel("Results Panel", screen.transform, Anchor.MiddleCenter, Vector2.zero, new Vector2(920f, 520f), Paper, true);
            resultsTitleText = Label("Results Title", resultPanel.transform, string.Empty, 38, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(38f, -34f), new Vector2(820f, 56f), Ink);
            resultsBodyText = Label("Results Body", resultPanel.transform, string.Empty, 28, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(38f, -120f), new Vector2(820f, 290f), Ink);
            Button("Results Continue Button", resultPanel.transform, "CONTINUE", delegate { RequestResultsContinue(); }, Anchor.BottomRight, new Vector2(-38f, 38f), new Vector2(210f, 62f), 22);
        }

        private void RefreshAllScreens()
        {
            if (!built)
            {
                return;
            }

            EnsureDefaultData();
            RefreshMain();
            RefreshMapList();
            RefreshVehicleList();
            RefreshGarage();
            RefreshResults();
        }

        private void RefreshMain()
        {
            if (mainStatusText == null)
            {
                return;
            }

            mainStatusText.text = pilotName.ToUpperInvariant() + "\nMAP  " + DisplayNameForMap(selectedMapId) + "\nCAR  " + DisplayNameForVehicle(selectedVehicleId);
            mainCreditsText.text = "CREDITS " + credits.ToString("0000");
        }

        private void RefreshMapList()
        {
            if (mapListRoot == null)
            {
                return;
            }

            ClearChildren(mapListRoot);
            for (int i = 0; i < maps.Count; i++)
            {
                CreateMapRow(maps[i], i);
            }

            if (mapSelectedText != null)
            {
                MapOption selected = FindMap(selectedMapId);
                mapSelectedText.text = selected != null ? selected.DisplayNameOrId().ToUpperInvariant() + "\n" + selected.StatusLine() : NoSelectionText;
            }

            if (mapNextButton != null)
            {
                mapNextButton.interactable = !string.IsNullOrEmpty(selectedMapId);
            }
        }

        private void RefreshVehicleList()
        {
            if (vehicleListRoot == null)
            {
                return;
            }

            ClearChildren(vehicleListRoot);
            for (int i = 0; i < vehicles.Count; i++)
            {
                CreateVehicleRow(vehicles[i], i);
            }

            if (vehicleMapText != null)
            {
                vehicleMapText.text = DisplayNameForMap(selectedMapId).ToUpperInvariant();
            }

            if (vehicleSelectedText != null)
            {
                VehicleOption selected = FindVehicle(selectedVehicleId);
                vehicleSelectedText.text = selected != null ? selected.DisplayNameOrId().ToUpperInvariant() + "\n" + selected.classLabel.ToUpperInvariant() : NoSelectionText;
            }

            bool canRace = !string.IsNullOrEmpty(selectedMapId) && !string.IsNullOrEmpty(selectedVehicleId);
            if (vehicleGarageButton != null)
            {
                vehicleGarageButton.interactable = !string.IsNullOrEmpty(selectedVehicleId);
            }

            if (raceStartButton != null)
            {
                raceStartButton.interactable = canRace;
            }
        }

        private void RefreshGarage()
        {
            if (upgradeListRoot == null || tuningListRoot == null)
            {
                return;
            }

            ClearChildren(upgradeListRoot);
            for (int i = 0; i < upgrades.Count; i++)
            {
                CreateUpgradeRow(upgrades[i]);
            }

            ClearChildren(tuningListRoot);
            CreateTuningSlider("ACCELERATION", "acceleration");
            CreateTuningSlider("TOP SPEED", "topSpeed");
            CreateTuningSlider("GRIP", "grip");
            CreateTuningSlider("STEERING", "steeringResponse");
            CreateTuningSlider("BRAKES", "brakePower");
            CreateTuningSlider("BOOST", "boostPower");
            CreateTuningSlider("COOLING", "cooling");

            if (garageCreditsText != null)
            {
                garageCreditsText.text = "CREDITS " + credits.ToString("0000");
            }

            if (garageVehicleText != null)
            {
                garageVehicleText.text = DisplayNameForVehicle(selectedVehicleId).ToUpperInvariant();
            }
        }

        private void RefreshResults()
        {
            if (resultsTitleText == null || resultsBodyText == null)
            {
                return;
            }

            ResultsState state = results != null ? results : ResultsState.Default();
            resultsTitleText.text = state.title.ToUpperInvariant();
            resultsBodyText.text =
                "MAP          " + SafeText(state.mapName, DisplayNameForMap(selectedMapId)) + "\n" +
                "VEHICLE      " + SafeText(state.vehicleName, DisplayNameForVehicle(selectedVehicleId)) + "\n" +
                "POSITION     " + SafeText(state.positionLabel, "--") + "\n" +
                "TIME         " + SafeText(state.finishTimeLabel, "--:--.---") + "\n" +
                "BEST LAP     " + SafeText(state.bestLapLabel, "--:--.---") + "\n" +
                "FLOW SCORE   " + state.flowScore.ToString("000") + "\n" +
                "CREDITS +    " + state.creditsEarned.ToString("0000") + "\n\n" +
                SafeText(state.message, "Run complete.");
        }

        private void CreateMapRow(MapOption option, int index)
        {
            GameObject row = Row("Map Row " + index, mapListRoot, RowHeight, option.id == selectedMapId, option.locked);
            Label("Name", row.transform, option.DisplayNameOrId().ToUpperInvariant(), 28, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(22f, -16f), new Vector2(450f, 34f), Ink);
            Label("Description", row.transform, SafeText(option.description, "Route contract pending."), 17, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(22f, -54f), new Vector2(570f, 42f), InkSoft);
            Label("Meta", row.transform, option.StatusLine(), 18, TextAnchor.MiddleRight, Anchor.TopRight, new Vector2(-22f, -36f), new Vector2(250f, 30f), option.locked ? Orange : Blue);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.interactable = !option.locked;
            ColorButton(button);
            MapOption captured = option;
            button.onClick.AddListener(delegate { SelectMap(captured); });
        }

        private void CreateVehicleRow(VehicleOption option, int index)
        {
            GameObject row = Row("Vehicle Row " + index, vehicleListRoot, RowHeight, option.id == selectedVehicleId, option.locked);
            Label("Name", row.transform, option.DisplayNameOrId().ToUpperInvariant(), 28, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(22f, -16f), new Vector2(390f, 34f), Ink);
            Label("Class", row.transform, SafeText(option.classLabel, "Prototype").ToUpperInvariant(), 15, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(22f, -54f), new Vector2(220f, 22f), InkSoft);
            Label("Description", row.transform, SafeText(option.description, "Vehicle contract pending."), 15, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(250f, -20f), new Vector2(360f, 70f), InkSoft);
            Label("State", row.transform, option.locked ? "LOCKED" : option.id == selectedVehicleId ? "SELECTED" : "READY", 18, TextAnchor.MiddleRight, Anchor.TopRight, new Vector2(-22f, -18f), new Vector2(150f, 28f), option.locked ? Orange : Blue);

            ReadOnlyMeter("Speed Meter", row.transform, Anchor.BottomRight, new Vector2(-22f, 50f), new Vector2(220f, 8f), Mathf.Clamp01(option.speed01), Blue);
            ReadOnlyMeter("Grip Meter", row.transform, Anchor.BottomRight, new Vector2(-22f, 34f), new Vector2(220f, 8f), Mathf.Clamp01(option.handling01), Green);
            ReadOnlyMeter("Boost Meter", row.transform, Anchor.BottomRight, new Vector2(-22f, 18f), new Vector2(220f, 8f), Mathf.Clamp01(option.boost01), Orange);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.interactable = !option.locked;
            ColorButton(button);
            VehicleOption captured = option;
            button.onClick.AddListener(delegate { SelectVehicle(captured); });
        }

        private void CreateUpgradeRow(UpgradeOption option)
        {
            GameObject row = Row("Upgrade " + option.id, upgradeListRoot, SmallRowHeight, option.owned, option.locked);
            Label("Upgrade Name", row.transform, option.DisplayNameOrId().ToUpperInvariant(), 21, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(18f, -12f), new Vector2(360f, 28f), Ink);
            Label("Upgrade Desc", row.transform, SafeText(option.description, "Upgrade contract pending."), 15, TextAnchor.UpperLeft, Anchor.TopLeft, new Vector2(18f, -42f), new Vector2(420f, 34f), InkSoft);
            Label("Upgrade Stat", row.transform, SafeText(option.statLabel, string.Empty).ToUpperInvariant(), 14, TextAnchor.MiddleRight, Anchor.TopRight, new Vector2(-170f, -16f), new Vector2(130f, 24f), Blue);

            string actionLabel = option.owned ? "OWNED" : option.locked ? "LOCKED" : option.cost > 0 ? option.cost.ToString("0000") : "BUY";
            Button purchaseButton = Button("Buy " + option.id, row.transform, actionLabel, delegate { RequestPurchaseUpgrade(option); }, Anchor.MiddleRight, new Vector2(-18f, 0f), new Vector2(128f, 42f), 16);
            purchaseButton.interactable = option.canPurchase && !option.owned && !option.locked && (option.cost <= 0 || credits >= option.cost);
        }

        private void CreateTuningSlider(string label, string fieldName)
        {
            GameObject row = Row("Tuning " + fieldName, tuningListRoot, 56f, false, false);
            Label("Tuning Label", row.transform, label, 18, TextAnchor.MiddleLeft, Anchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(190f, 28f), Ink);
            Text valueText = Label("Tuning Value", row.transform, "1.00", 17, TextAnchor.MiddleRight, Anchor.MiddleRight, new Vector2(-18f, 0f), new Vector2(70f, 28f), Ink);
            Slider slider = Slider("Tuning Slider", row.transform, Anchor.MiddleLeft, new Vector2(230f, 0f), new Vector2(410f, 18f), Blue);

            FieldInfo field = typeof(GTXTuningProfile).GetField(fieldName);
            if (field == null)
            {
                slider.interactable = false;
                valueText.text = "--";
                return;
            }

            RangeAttribute range = Attribute.GetCustomAttribute(field, typeof(RangeAttribute)) as RangeAttribute;
            slider.minValue = range != null ? range.min : 0f;
            slider.maxValue = range != null ? range.max : 2f;
            float value = tuning != null ? (float)field.GetValue(tuning) : 1f;
            suppressTuningEvents = true;
            slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
            valueText.text = slider.value.ToString("0.00");
            suppressTuningEvents = false;

            slider.onValueChanged.AddListener(delegate(float nextValue)
            {
                valueText.text = nextValue.ToString("0.00");
                if (suppressTuningEvents || tuning == null)
                {
                    return;
                }

                field.SetValue(tuning, nextValue);
                NotifyTuningChanged(fieldName, nextValue);
            });
        }

        private void SelectMap(MapOption option)
        {
            if (option == null || option.locked)
            {
                return;
            }

            selectedMapId = option.id;
            RefreshAllScreens();
            callbacks?.onMapSelected?.Invoke(selectedMapId);
            MapSelected?.Invoke(selectedMapId);
            mapSelectedUnityEvent.Invoke(selectedMapId);
        }

        private void SelectVehicle(VehicleOption option)
        {
            if (option == null || option.locked)
            {
                return;
            }

            selectedVehicleId = option.id;
            RefreshAllScreens();
            callbacks?.onVehicleSelected?.Invoke(selectedVehicleId);
            VehicleSelected?.Invoke(selectedVehicleId);
            vehicleSelectedUnityEvent.Invoke(selectedVehicleId);
        }

        private void RequestStart()
        {
            callbacks?.onStart?.Invoke();
            StartRequested?.Invoke();
            startRequestedUnityEvent.Invoke();

            if (autoAdvanceScreens)
            {
                ShowMapSelect();
            }
        }

        private void RequestGarageBack()
        {
            callbacks?.onGarageBack?.Invoke();
            GarageBackRequested?.Invoke();
            garageBackUnityEvent.Invoke();

            if (autoAdvanceScreens)
            {
                ShowVehicleSelect();
            }
        }

        private void RequestRaceStart()
        {
            RaceStartRequest request = new RaceStartRequest(selectedMapId, selectedVehicleId, CloneTuning(tuning), credits);
            callbacks?.onRaceStart?.Invoke(request);
            RaceStartRequested?.Invoke(request);
            raceStartUnityEvent.Invoke(selectedMapId, selectedVehicleId);
        }

        private void RequestPurchaseUpgrade(UpgradeOption option)
        {
            if (option == null)
            {
                return;
            }

            callbacks?.onPurchaseUpgrade?.Invoke(option.id);
            PurchaseUpgradeRequested?.Invoke(option.id);
            purchaseUpgradeUnityEvent.Invoke(option.id);
        }

        private void RequestResultsContinue()
        {
            callbacks?.onResultsContinue?.Invoke();
            ResultsContinueRequested?.Invoke();
            resultsContinueUnityEvent.Invoke();

            if (autoAdvanceScreens)
            {
                ShowMainMenu();
            }
        }

        private void NotifyTuningChanged(string fieldName, float value)
        {
            callbacks?.onTuningChange?.Invoke(fieldName, value);
            TuningChanged?.Invoke(fieldName, value);
            tuningChangedUnityEvent.Invoke(fieldName, value);
        }

        private void EnsureDefaultData()
        {
            if (tuning == null)
            {
                tuning = GTXTuningProfile.FromPreset(GTXTuningPreset.Strike);
            }

            if (results == null)
            {
                results = ResultsState.Default();
            }

            if (maps.Count == 0)
            {
                maps.Add(new MapOption("mesa-loop", "Mesa Loop", "Short desert loop with wide contact zones.", "BEST 02:12.300", false));
                maps.Add(new MapOption("dock-yard", "Dock Yard", "Tight concrete course with boost-line exits.", "NEW", false));
                maps.Add(new MapOption("night-pass", "Night Pass", "Mountain route reserved for progression unlock.", "LOCKED", true));
            }

            if (vehicles.Count == 0)
            {
                vehicles.Add(new VehicleOption("strike", "Strike", "STRIKE CLASS", "Heavy contact platform.", 0.72f, 0.56f, 0.62f, false));
                vehicles.Add(new VehicleOption("drift", "Drift", "DRIFT CLASS", "Fast yaw response and flow gain.", 0.62f, 0.82f, 0.58f, false));
                vehicles.Add(new VehicleOption("volt", "Volt", "VOLT CLASS", "Boost-heavy build with heat risk.", 0.76f, 0.62f, 0.92f, false));
                vehicles.Add(new VehicleOption("hauler", "Hauler", "PICKUP CLASS", "Stable utility truck with bed armor.", 0.64f, 0.68f, 0.6f, false));
            }

            if (upgrades.Count == 0)
            {
                upgrades.Add(new UpgradeOption("engine-kit-a", "Engine Kit A", "+ torque", "Entry torque package.", 220, false, false));
                upgrades.Add(new UpgradeOption("brace-plate", "Brace Plate", "+ ram", "Blackline frame brace.", 180, false, false));
                upgrades.Add(new UpgradeOption("cooling-loop", "Cooling Loop", "+ cooling", "Boost heat recovery loop.", 260, false, false));
            }

            NormalizeIds();
            if (!HasUnlockedMap(selectedMapId))
            {
                selectedMapId = FirstUnlockedMapId();
            }

            if (!HasUnlockedVehicle(selectedVehicleId))
            {
                selectedVehicleId = FirstUnlockedVehicleId();
            }
        }

        private void NormalizeIds()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(maps[i].id))
                {
                    maps[i].id = "map-" + i;
                }
            }

            for (int i = 0; i < vehicles.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(vehicles[i].id))
                {
                    vehicles[i].id = "vehicle-" + i;
                }
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(upgrades[i].id))
                {
                    upgrades[i].id = "upgrade-" + i;
                }
            }
        }

        private bool HasUnlockedMap(string id)
        {
            MapOption option = FindMap(id);
            return option != null && !option.locked;
        }

        private bool HasUnlockedVehicle(string id)
        {
            VehicleOption option = FindVehicle(id);
            return option != null && !option.locked;
        }

        private string FirstUnlockedMapId()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].locked)
                {
                    return maps[i].id;
                }
            }

            return maps.Count > 0 ? maps[0].id : string.Empty;
        }

        private string FirstUnlockedVehicleId()
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                if (!vehicles[i].locked)
                {
                    return vehicles[i].id;
                }
            }

            return vehicles.Count > 0 ? vehicles[0].id : string.Empty;
        }

        private MapOption FindMap(string id)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].id == id)
                {
                    return maps[i];
                }
            }

            return null;
        }

        private VehicleOption FindVehicle(string id)
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                if (vehicles[i].id == id)
                {
                    return vehicles[i];
                }
            }

            return null;
        }

        private string DisplayNameForMap(string id)
        {
            MapOption option = FindMap(id);
            return option != null ? option.DisplayNameOrId() : NoSelectionText;
        }

        private string DisplayNameForVehicle(string id)
        {
            VehicleOption option = FindVehicle(id);
            return option != null ? option.DisplayNameOrId() : NoSelectionText;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private GameObject CreateScreen(VectorSSMenuScreen screen, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(root, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            Stretch(rect);
            screens[screen] = gameObject;
            gameObject.SetActive(false);
            return gameObject;
        }

        private void CreateScreenHeader(Transform parent, string title, string subtitle, UnityAction backAction)
        {
            CreateAccentBars(parent);
            Label(title + " Title", parent, title, 56, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(86f, -78f), new Vector2(620f, 70f), Ink);
            Label(title + " Subtitle", parent, subtitle, 18, TextAnchor.MiddleLeft, Anchor.TopLeft, new Vector2(92f, -142f), new Vector2(650f, 28f), InkSoft);
            Button(title + " Back", parent, "BACK", backAction, Anchor.TopRight, new Vector2(-86f, -86f), new Vector2(150f, 50f), 18);
        }

        private void CreateAccentBars(Transform parent)
        {
            Panel("Top Ink Rule", parent, Anchor.TopLeft, new Vector2(86f, -42f), new Vector2(430f, 8f), Ink, false);
            Panel("Top Orange Rule", parent, Anchor.TopLeft, new Vector2(530f, -42f), new Vector2(76f, 8f), Orange, false);
            Panel("Bottom Blue Rule", parent, Anchor.BottomRight, new Vector2(-86f, 64f), new Vector2(360f, 8f), Blue, false);
        }

        private RectTransform Stack(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 dimensions, float spacing)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            SetAnchor(rect, anchor, position, dimensions);
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private GameObject Row(string name, Transform parent, float height, bool selected, bool disabled)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = disabled ? PaperDim : selected ? new Color(0.88f, 0.91f, 0.98f, 1f) : Paper;
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? Blue : Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            LayoutElement layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            return gameObject;
        }

        private GameObject Panel(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 dimensions, Color color, bool outlined)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, position, dimensions);

            if (outlined)
            {
                Outline outline = gameObject.AddComponent<Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = new Vector2(2f, -2f);

                if (dimensions.x >= 180f && dimensions.y >= 90f)
                {
                    gameObject.AddComponent<VectorSSDraggableResizablePanel>();
                }
            }

            return gameObject;
        }

        private Text Label(string name, Transform parent, string text, int size, TextAnchor alignment, Anchor anchor, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Text label = gameObject.AddComponent<Text>();
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = label.rectTransform;
            SetAnchor(rect, anchor, position, dimensions);
            return label;
        }

        private Button Button(string name, Transform parent, string label, UnityAction onClick, float height, int fontSize)
        {
            GameObject gameObject = Panel(name, parent, Anchor.BottomLeft, Vector2.zero, new Vector2(0f, height), Paper, true);
            LayoutElement layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ColorButton(button);
            button.onClick.AddListener(onClick);
            Label(label + " Text", gameObject.transform, label, fontSize, TextAnchor.MiddleCenter, Anchor.Stretch, Vector2.zero, Vector2.zero, Ink);
            return button;
        }

        private Button Button(string name, Transform parent, string label, UnityAction onClick, Anchor anchor, Vector2 position, Vector2 dimensions, int fontSize)
        {
            GameObject gameObject = Panel(name, parent, anchor, position, dimensions, Paper, true);
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ColorButton(button);
            button.onClick.AddListener(onClick);
            Label(label + " Text", gameObject.transform, label, fontSize, TextAnchor.MiddleCenter, Anchor.Stretch, Vector2.zero, Vector2.zero, Ink);
            return button;
        }

        private Slider Slider(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 dimensions, Color fillColor)
        {
            GameObject rootObject = Panel(name, parent, anchor, position, dimensions, new Color(0.06f, 0.06f, 0.055f, 0.18f), true);

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(rootObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            Stretch(fillRect);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = fillColor;

            GameObject handleObject = Panel("Handle", rootObject.transform, Anchor.MiddleLeft, Vector2.zero, new Vector2(16f, 30f), Ink, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();

            Slider slider = rootObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.ColorTint;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObject.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            ColorBlock colors = slider.colors;
            colors.normalColor = Ink;
            colors.highlightedColor = Blue;
            colors.pressedColor = Orange;
            colors.disabledColor = PaperDim;
            slider.colors = colors;
            return slider;
        }

        private void ReadOnlyMeter(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 dimensions, float value, Color color)
        {
            GameObject rootObject = Panel(name, parent, anchor, position, dimensions, new Color(0.06f, 0.06f, 0.055f, 0.18f), false);
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(rootObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = color;
        }

        private static void ColorButton(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Paper;
            colors.highlightedColor = new Color(0.86f, 0.89f, 0.93f, 1f);
            colors.pressedColor = new Color(0.74f, 0.79f, 0.86f, 1f);
            colors.selectedColor = new Color(0.86f, 0.89f, 0.93f, 1f);
            colors.disabledColor = new Color(0.62f, 0.60f, 0.55f, 0.82f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static void ConfigureCanvasScaler(Canvas target)
        {
            CanvasScaler scaler = target.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = target.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (target.GetComponent<GraphicRaycaster>() == null)
            {
                target.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rect, Anchor anchor, Vector2 position, Vector2 dimensions)
        {
            switch (anchor)
            {
                case Anchor.Stretch:
                    Stretch(rect);
                    return;
                case Anchor.TopLeft:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    break;
                case Anchor.TopCenter:
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    break;
                case Anchor.MiddleLeft:
                    rect.anchorMin = new Vector2(0f, 0.5f);
                    rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    break;
                case Anchor.MiddleRight:
                    rect.anchorMin = new Vector2(1f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    break;
                case Anchor.MiddleCenter:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case Anchor.BottomRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case Anchor.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    break;
                default:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    break;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static Font LoadBuiltInFont()
        {
            Font runtimeFont = TryLoadBuiltInFont("LegacyRuntime.ttf");
            return runtimeFont != null ? runtimeFont : TryLoadBuiltInFont("Arial.ttf");
        }

        private static Font TryLoadBuiltInFont(string path)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string SafeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static GTXTuningProfile CloneTuning(GTXTuningProfile source)
        {
            GTXTuningProfile clone = GTXTuningProfile.FromPreset(GTXTuningPreset.Strike);
            if (source != null)
            {
                clone.CopyFrom(source);
            }

            return clone;
        }

        private static void CopyOptions<T>(IEnumerable<T> source, List<T> destination) where T : class
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                if (item != null)
                {
                    destination.Add(item);
                }
            }
        }

        [Serializable]
        public sealed class VectorSSMenuBinding
        {
            public string pilotName = "ROOKIE";
            public int credits = 350;
            public string selectedMapId;
            public string selectedVehicleId;
            public GTXTuningProfile tuning = GTXTuningProfile.FromPreset(GTXTuningPreset.Strike);
            public ResultsState results = ResultsState.Default();
            public List<MapOption> maps = new List<MapOption>();
            public List<VehicleOption> vehicles = new List<VehicleOption>();
            public List<UpgradeOption> upgrades = new List<UpgradeOption>();
        }

        public sealed class MenuCallbacks
        {
            public Action onStart;
            public Action<string> onMapSelected;
            public Action<string> onVehicleSelected;
            public Action onGarageBack;
            public Action<RaceStartRequest> onRaceStart;
            public Action<string> onPurchaseUpgrade;
            public Action<string, float> onTuningChange;
            public Action onResultsContinue;
        }

        [Serializable]
        public sealed class RaceStartRequest
        {
            public string mapId;
            public string vehicleId;
            public GTXTuningProfile tuning;
            public int creditsAtStart;

            public RaceStartRequest()
            {
            }

            public RaceStartRequest(string mapId, string vehicleId, GTXTuningProfile tuning, int creditsAtStart)
            {
                this.mapId = mapId;
                this.vehicleId = vehicleId;
                this.tuning = tuning;
                this.creditsAtStart = creditsAtStart;
            }
        }

        [Serializable]
        public sealed class MapOption
        {
            public string id;
            public string displayName;
            [TextArea] public string description;
            public string statusLabel;
            public bool locked;

            public MapOption()
            {
            }

            public MapOption(string id, string displayName, string description, string statusLabel, bool locked)
            {
                this.id = id;
                this.displayName = displayName;
                this.description = description;
                this.statusLabel = statusLabel;
                this.locked = locked;
            }

            public string DisplayNameOrId()
            {
                return SafeText(displayName, SafeText(id, "Map"));
            }

            public string StatusLine()
            {
                if (locked)
                {
                    return SafeText(statusLabel, "LOCKED");
                }

                return SafeText(statusLabel, "READY");
            }
        }

        [Serializable]
        public sealed class VehicleOption
        {
            public string id;
            public string displayName;
            public string classLabel;
            [TextArea] public string description;
            [Range(0f, 1f)] public float speed01 = 0.65f;
            [Range(0f, 1f)] public float handling01 = 0.65f;
            [Range(0f, 1f)] public float boost01 = 0.65f;
            public bool locked;

            public VehicleOption()
            {
            }

            public VehicleOption(string id, string displayName, string classLabel, string description, float speed01, float handling01, float boost01, bool locked)
            {
                this.id = id;
                this.displayName = displayName;
                this.classLabel = classLabel;
                this.description = description;
                this.speed01 = speed01;
                this.handling01 = handling01;
                this.boost01 = boost01;
                this.locked = locked;
            }

            public string DisplayNameOrId()
            {
                return SafeText(displayName, SafeText(id, "Vehicle"));
            }
        }

        [Serializable]
        public sealed class UpgradeOption
        {
            public string id;
            public string displayName;
            public string statLabel;
            [TextArea] public string description;
            public int cost;
            public bool owned;
            public bool locked;
            public bool canPurchase = true;

            public UpgradeOption()
            {
            }

            public UpgradeOption(string id, string displayName, string statLabel, string description, int cost, bool owned, bool locked)
            {
                this.id = id;
                this.displayName = displayName;
                this.statLabel = statLabel;
                this.description = description;
                this.cost = cost;
                this.owned = owned;
                this.locked = locked;
                canPurchase = true;
            }

            public string DisplayNameOrId()
            {
                return SafeText(displayName, SafeText(id, "Upgrade"));
            }
        }

        [Serializable]
        public sealed class ResultsState
        {
            public string title = "Race Complete";
            public string mapName;
            public string vehicleName;
            public string positionLabel = "P1";
            public string finishTimeLabel = "02:12.300";
            public string bestLapLabel = "00:42.800";
            public int flowScore = 86;
            public int creditsEarned = 240;
            [TextArea] public string message = "Rewards tallied. Awaiting next run.";

            public static ResultsState Default()
            {
                return new ResultsState();
            }

            public ResultsState Copy()
            {
                return new ResultsState
                {
                    title = title,
                    mapName = mapName,
                    vehicleName = vehicleName,
                    positionLabel = positionLabel,
                    finishTimeLabel = finishTimeLabel,
                    bestLapLabel = bestLapLabel,
                    flowScore = flowScore,
                    creditsEarned = creditsEarned,
                    message = message
                };
            }
        }

        [Serializable]
        public sealed class StringEvent : UnityEvent<string>
        {
        }

        [Serializable]
        public sealed class StringFloatEvent : UnityEvent<string, float>
        {
        }

        [Serializable]
        public sealed class RaceStartUnityEvent : UnityEvent<string, string>
        {
        }

        private enum Anchor
        {
            BottomLeft,
            BottomRight,
            BottomCenter,
            TopLeft,
            TopRight,
            TopCenter,
            MiddleLeft,
            MiddleRight,
            MiddleCenter,
            Stretch
        }
    }
}
