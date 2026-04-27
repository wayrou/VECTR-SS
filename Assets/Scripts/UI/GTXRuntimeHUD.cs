using System;
using System.Reflection;
using GTX.Data;
using GTX.Flow;
using GTX.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace GTX.UI
{
    public class GTXRuntimeHUD : MonoBehaviour
    {
        [Header("Vehicle Source")]
        [SerializeField] private Component telemetrySource;
        [SerializeField] private Rigidbody vehicleRigidbody;
        [SerializeField] private FlowState flowState;

        [Header("Tuning")]
        [SerializeField] private GTXTuningPreset activePreset = GTXTuningPreset.Strike;
        [SerializeField] private GTXTuningProfile tuning = GTXTuningProfile.FromPreset(GTXTuningPreset.Strike);

        [Header("Input")]
        [SerializeField] private KeyCode garageToggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode controlsToggleKey = KeyCode.F1;
        [SerializeField] private KeyCode flowToggleKey = KeyCode.F3;

        private Canvas canvas;
        private Font font;
        private Text speedText;
        private Text gearText;
        private Text rpmText;
        private Text boostText;
        private Text heatText;
        private Text feedbackText;
        private Text controlsText;
        private Text flowText;
        private GameObject hudPanel;
        private GameObject controlsPanel;
        private GameObject flowPanel;
        private GameObject garagePanel;
        private Slider rpmSlider;
        private Slider boostSlider;
        private Slider heatSlider;
        private Slider flowSlider;
        private GTXTelemetrySnapshot demoTelemetry;
        private float calloutTimer;
        private string currentCallout = "Ready";
        private const string TuningPrefsPrefix = "GTX.RuntimeTuning.";
        private static readonly string[] TuningFields = { "acceleration", "topSpeed", "grip", "steeringResponse", "brakePower", "boostPower", "cooling" };

        public event Action<GTXTuningProfile> TuningChanged;
        public GTXTuningProfile Tuning { get { return tuning; } }
        public GTXTuningPreset ActivePreset { get { return activePreset; } }

        public void ShowCallout(string callout)
        {
            if (string.IsNullOrEmpty(callout))
            {
                return;
            }

            currentCallout = callout;
            calloutTimer = 1.8f;
        }

        public void Bind(Component newTelemetrySource, Rigidbody newVehicleRigidbody, FlowState newFlowState)
        {
            telemetrySource = newTelemetrySource;
            vehicleRigidbody = newVehicleRigidbody;
            flowState = newFlowState;
        }

        public void ToggleGarage()
        {
            SetGarageVisible(garagePanel == null || !garagePanel.activeSelf);
        }

        public void SetGarageVisible(bool visible)
        {
            if (garagePanel != null)
            {
                garagePanel.SetActive(visible);
            }
        }

        public void SetRaceHudVisible(bool visible)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(visible);
            }

            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetGarageVisible(false);
                SetControlsVisible(false);
                SetFlowDebugVisible(false);
            }
        }

        public void ToggleControls()
        {
            SetControlsVisible(controlsPanel == null || !controlsPanel.activeSelf);
        }

        public void SetControlsVisible(bool visible)
        {
            if (controlsPanel != null)
            {
                controlsPanel.SetActive(visible);
            }
        }

        public void ToggleFlowDebug()
        {
            SetFlowDebugVisible(flowPanel == null || !flowPanel.activeSelf);
        }

        public void SetFlowDebugVisible(bool visible)
        {
            if (flowPanel != null)
            {
                flowPanel.SetActive(visible && Debug.isDebugBuild);
            }
        }

        public bool TryApplyPreset(string presetName, out string message)
        {
            GTXTuningPreset preset;
            if (!Enum.TryParse(presetName, true, out preset))
            {
                message = "Unknown preset. Use Strike, Drift, or Volt.";
                return false;
            }

            ApplyPreset(preset);
            message = "Preset applied: " + preset;
            return true;
        }

        public bool TrySetTuningValue(string fieldName, float value, out string message)
        {
            FieldInfo field = ResolveTuningField(fieldName);
            if (field == null)
            {
                message = "Unknown tuning field. Use acceleration, topSpeed, grip, steering, brakes, boost, or cooling.";
                return false;
            }

            RangeAttribute range = Attribute.GetCustomAttribute(field, typeof(RangeAttribute)) as RangeAttribute;
            float clamped = range != null ? Mathf.Clamp(value, range.min, range.max) : value;
            field.SetValue(tuning, clamped);
            RefreshGarageSliders();
            SaveTuning();
            TuningChanged?.Invoke(tuning);
            currentCallout = "Tune " + field.Name;
            calloutTimer = 1.8f;
            message = field.Name + " = " + clamped.ToString("0.00");
            return true;
        }

        public string GetTuningSummary()
        {
            return activePreset + " accel " + tuning.acceleration.ToString("0.00") +
                " top " + tuning.topSpeed.ToString("0.00") +
                " grip " + tuning.grip.ToString("0.00") +
                " steer " + tuning.steeringResponse.ToString("0.00") +
                " boost " + tuning.boostPower.ToString("0.00");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapHud()
        {
            if (FindObjectOfType<GTXRuntimeHUD>() != null)
            {
                return;
            }

            GameObject hud = new GameObject("GTX Runtime HUD Controller");
            hud.AddComponent<GTXRuntimeHUD>();
        }

        private void Awake()
        {
            font = LoadBuiltInFont();
            EnsureSources();
            BuildHud();
            LoadSavedTuningOrApplyDefault();
        }

        private void Update()
        {
            if (Input.GetKeyDown(garageToggleKey))
            {
                ToggleGarage();
            }

            if (Input.GetKeyDown(controlsToggleKey))
            {
                ToggleControls();
            }

            if (Input.GetKeyDown(flowToggleKey) && Debug.isDebugBuild)
            {
                ToggleFlowDebug();
            }

            UpdateHud(ReadTelemetry());
        }

        private void EnsureSources()
        {
            if (vehicleRigidbody == null)
            {
                VehicleController vehicle = FindObjectOfType<VehicleController>();
                if (vehicle != null)
                {
                    telemetrySource = vehicle;
                    vehicleRigidbody = vehicle.GetComponent<Rigidbody>();
                }
            }

            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = FindObjectOfType<Rigidbody>();
            }

            if (telemetrySource == null && vehicleRigidbody != null)
            {
                telemetrySource = vehicleRigidbody.GetComponent<VehicleController>();
            }

            if (flowState == null)
            {
                flowState = FindObjectOfType<FlowState>();
            }
        }

        private GTXTelemetrySnapshot ReadTelemetry()
        {
            GTXTelemetrySnapshot snapshot = GTXTelemetrySnapshot.Idle;

            if (vehicleRigidbody != null)
            {
                snapshot.speedKph = vehicleRigidbody.velocity.magnitude * 3.6f;
            }
            else
            {
                demoTelemetry.speedKph = Mathf.PingPong(Time.time * 38f, 240f);
                snapshot.speedKph = demoTelemetry.speedKph;
            }

            object source = telemetrySource;
            snapshot.gear = Mathf.RoundToInt(ReadNumber(source, new[] { "CurrentGear", "currentGear", "Gear", "gear" }, Mathf.Clamp(Mathf.FloorToInt(snapshot.speedKph / 48f) + 1, 1, 6)));
            snapshot.rpm = ReadNumber(source, new[] { "RPM", "rpm", "EngineRPM", "engineRPM", "CurrentRPM", "currentRPM" }, Mathf.Lerp(900f, 7600f, Mathf.PingPong(Time.time * 0.18f + snapshot.speedKph / 280f, 1f)));
            snapshot.rpm01 = Mathf.Clamp01(ReadNumber(source, new[] { "RPM01", "rpm01", "Rev01", "rev01" }, Mathf.InverseLerp(800f, 8200f, snapshot.rpm)));
            snapshot.boost01 = Mathf.Clamp01(ReadNumber(source, new[] { "Boost01", "boost01", "Nitro01", "nitro01", "Boost", "boost", "Nitro", "nitro" }, Mathf.PingPong(Time.time * 0.16f, 1f)));
            snapshot.heat01 = Mathf.Clamp01(ReadNumber(source, new[] { "Heat01", "heat01", "Temperature01", "temperature01", "Heat", "heat" }, Mathf.Clamp01(snapshot.boost01 * 0.75f + snapshot.rpm01 * 0.25f)));
            snapshot.flow01 = flowState != null ? flowState.Normalized : Mathf.Clamp01(ReadNumber(source, new[] { "Flow", "flow", "Flow01", "flow01" }, Mathf.Clamp01(1f - Mathf.Abs(0.62f - snapshot.rpm01))));
            snapshot.isBoosting = ReadBool(source, new[] { "IsBoosting", "isBoosting", "Boosting", "boosting" }, snapshot.boost01 > 0.72f);
            snapshot.isDrifting = ReadBool(source, new[] { "IsDrifting", "isDrifting", "Drifting", "drifting" }, false);
            snapshot.feedback = ReadText(source, new[] { "Feedback", "feedback", "Callout", "callout", "DrivingState", "drivingState" }, BuildFeedback(snapshot));

            return snapshot;
        }

        private void UpdateHud(GTXTelemetrySnapshot snapshot)
        {
            speedText.text = Mathf.RoundToInt(snapshot.speedKph).ToString("000");
            gearText.text = FormatGear(snapshot.gear);
            rpmText.text = Mathf.RoundToInt(snapshot.rpm).ToString("0") + " RPM";
            boostText.text = "BOOST " + Mathf.RoundToInt(snapshot.boost01 * 100f) + "%";
            heatText.text = "HEAT " + Mathf.RoundToInt(snapshot.heat01 * 100f) + "%";
            rpmSlider.value = snapshot.rpm01;
            boostSlider.value = snapshot.boost01;
            heatSlider.value = snapshot.heat01;
            flowSlider.value = snapshot.flow01;
            flowText.text = "FLOW " + Mathf.RoundToInt(snapshot.flow01 * 100f) + "%";

            if (calloutTimer <= 0f && !string.IsNullOrEmpty(snapshot.feedback) && snapshot.feedback != currentCallout)
            {
                currentCallout = snapshot.feedback;
                calloutTimer = ShouldPinFeedback(snapshot.feedback) ? 1.8f : 0f;
            }

            calloutTimer = Mathf.Max(0f, calloutTimer - Time.deltaTime);
            feedbackText.text = calloutTimer > 0f ? currentCallout : BuildFeedback(snapshot);
        }

        private string BuildFeedback(GTXTelemetrySnapshot snapshot)
        {
            if (snapshot.heat01 > 0.86f)
            {
                return "Cool it";
            }

            if (snapshot.isDrifting)
            {
                return "Hold angle";
            }

            if (snapshot.isBoosting || snapshot.boost01 > 0.78f)
            {
                return "Boost window";
            }

            if (snapshot.rpm01 > 0.82f)
            {
                return "Shift";
            }

            if (snapshot.flow01 > 0.78f)
            {
                return "Flow";
            }

            return "Clean line";
        }

        private static string FormatGear(int gear)
        {
            if (gear < 0)
            {
                return "R";
            }

            if (gear == 0)
            {
                return "N";
            }

            return "G" + gear;
        }

        private static bool ShouldPinFeedback(string feedback)
        {
            return feedback != "READY" && feedback != "Ready" && feedback != "Clean line";
        }

        private void BuildHud()
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("GTX Runtime HUD");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            ConfigureCanvasScaler(canvas);

            RectTransform root = canvas.GetComponent<RectTransform>();
            hudPanel = Panel("HUD", root, Anchor.BottomLeft, new Vector2(36f, 36f), new Vector2(356f, 188f), new Color(0.015f, 0.02f, 0.045f, 0.82f));
            speedText = Label("Speed", hudPanel.transform, "000", 66, TextAnchor.MiddleLeft, new Vector2(20f, 98f), new Vector2(170f, 72f));
            Label("SpeedUnit", hudPanel.transform, "KM/H", 15, TextAnchor.MiddleLeft, new Vector2(194f, 132f), new Vector2(72f, 22f));
            gearText = Label("Gear", hudPanel.transform, "G1", 38, TextAnchor.MiddleCenter, new Vector2(270f, 102f), new Vector2(62f, 56f));
            rpmText = Label("RPM", hudPanel.transform, "900 RPM", 16, TextAnchor.MiddleLeft, new Vector2(20f, 72f), new Vector2(132f, 22f));
            rpmSlider = Meter("RPM Meter", hudPanel.transform, new Vector2(154f, 78f), new Vector2(178f, 10f), new Color(1f, 0.48f, 0.08f, 1f));
            boostText = Label("Boost", hudPanel.transform, "BOOST 0%", 14, TextAnchor.MiddleLeft, new Vector2(20f, 44f), new Vector2(94f, 20f));
            boostSlider = Meter("Boost Meter", hudPanel.transform, new Vector2(116f, 50f), new Vector2(216f, 8f), new Color(0.36f, 0.67f, 1f, 1f));
            heatText = Label("Heat", hudPanel.transform, "HEAT 0%", 14, TextAnchor.MiddleLeft, new Vector2(20f, 20f), new Vector2(94f, 20f));
            heatSlider = Meter("Heat Meter", hudPanel.transform, new Vector2(116f, 26f), new Vector2(216f, 8f), new Color(1f, 0.48f, 0.08f, 1f));

            feedbackText = Label("Feedback", root, "Ready", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(280f, 46f));
            SetAnchor(feedbackText.rectTransform, Anchor.TopCenter, new Vector2(0f, -76f), new Vector2(280f, 46f));

            controlsPanel = Panel("Controls Overlay", root, Anchor.TopLeft, new Vector2(24f, -24f), new Vector2(330f, 104f), new Color(0.015f, 0.02f, 0.045f, 0.68f));
            Label("Controls Title", controlsPanel.transform, "CONTROLS", 12, TextAnchor.MiddleLeft, new Vector2(12f, 78f), new Vector2(86f, 18f));
            controlsText = Label("Controls Text", controlsPanel.transform,
                "W/S throttle/brake   A/D steer\n" +
                "Q/E gears   Shift clutch   Space drift\n" +
                "F boost   Z/C slam   X guard   R reset\n" +
                "Tab garage   ` QUAC   F3 flow   F1 hide",
                11, TextAnchor.MiddleLeft, new Vector2(12f, 10f), new Vector2(306f, 66f));

            flowPanel = Panel("Debug Flow", root, Anchor.TopRight, new Vector2(-32f, -32f), new Vector2(224f, 72f), new Color(0.015f, 0.02f, 0.045f, 0.78f));
            flowText = Label("Flow Text", flowPanel.transform, "FLOW 0%", 15, TextAnchor.MiddleLeft, new Vector2(16f, 38f), new Vector2(92f, 20f));
            flowSlider = Meter("Flow Meter", flowPanel.transform, new Vector2(16f, 24f), new Vector2(192f, 8f), new Color(1f, 0.48f, 0.08f, 1f));
            flowPanel.SetActive(false);

            BuildGarage(root);
        }

        private static void ConfigureCanvasScaler(Canvas targetCanvas)
        {
            CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildGarage(RectTransform root)
        {
            garagePanel = Panel("Garage Tuning", root, Anchor.MiddleRight, new Vector2(-34f, 0f), new Vector2(324f, 438f), new Color(0.015f, 0.02f, 0.045f, 0.94f));
            Label("Garage Title", garagePanel.transform, "GARAGE", 24, TextAnchor.MiddleLeft, new Vector2(18f, 392f), new Vector2(130f, 30f));

            AddPresetButton("Strike", GTXTuningPreset.Strike, new Vector2(18f, 348f));
            AddPresetButton("Drift", GTXTuningPreset.Drift, new Vector2(118f, 348f));
            AddPresetButton("Volt", GTXTuningPreset.Volt, new Vector2(218f, 348f));

            AddTuningSlider("Acceleration", "acceleration", 300f);
            AddTuningSlider("Top Speed", "topSpeed", 254f);
            AddTuningSlider("Grip", "grip", 208f);
            AddTuningSlider("Steering", "steeringResponse", 162f);
            AddTuningSlider("Brakes", "brakePower", 116f);
            AddTuningSlider("Boost", "boostPower", 70f);
            AddTuningSlider("Cooling", "cooling", 24f);
            garagePanel.SetActive(false);
        }

        private void AddPresetButton(string label, GTXTuningPreset preset, Vector2 position)
        {
            Button button = Button(label, garagePanel.transform, position, new Vector2(84f, 30f));
            button.onClick.AddListener(delegate { ApplyPreset(preset); });
        }

        private void AddTuningSlider(string label, string fieldName, float y)
        {
            Label(label + " Label", garagePanel.transform, label, 14, TextAnchor.MiddleLeft, new Vector2(18f, y), new Vector2(110f, 20f));
            Slider slider = Meter(label + " Slider", garagePanel.transform, new Vector2(132f, y + 6f), new Vector2(138f, 8f), new Color(0.08f, 0.78f, 1f, 1f));
            slider.minValue = 0.4f;
            slider.maxValue = 1.8f;
            FieldInfo field = typeof(GTXTuningProfile).GetField(fieldName);
            if (field != null)
            {
                slider.value = (float)field.GetValue(tuning);
                slider.onValueChanged.AddListener(delegate(float value)
                {
                    field.SetValue(tuning, value);
                    SaveTuning();
                    TuningChanged?.Invoke(tuning);
                });
            }

            Text valueText = Label(label + " Value", garagePanel.transform, "1.00", 13, TextAnchor.MiddleRight, new Vector2(274f, y), new Vector2(34f, 20f));
            slider.onValueChanged.AddListener(delegate(float value) { valueText.text = value.ToString("0.00"); });
            valueText.text = slider.value.ToString("0.00");
        }

        private void ApplyPreset(GTXTuningPreset preset)
        {
            activePreset = preset;
            tuning.CopyFrom(GTXTuningProfile.FromPreset(preset));
            RefreshGarageSliders();
            currentCallout = preset.ToString() + " tune";
            calloutTimer = 1.8f;
            SaveTuning();
            TuningChanged?.Invoke(tuning);
        }

        private void RefreshGarageSliders()
        {
            if (garagePanel == null)
            {
                return;
            }

            Slider[] sliders = garagePanel.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length && i < TuningFields.Length; i++)
            {
                FieldInfo field = typeof(GTXTuningProfile).GetField(TuningFields[i]);
                if (field != null)
                {
                    sliders[i].value = (float)field.GetValue(tuning);
                }
            }
        }

        private void LoadSavedTuningOrApplyDefault()
        {
            string presetName = PlayerPrefs.GetString(TuningPrefsPrefix + "Preset", activePreset.ToString());
            GTXTuningPreset savedPreset;
            if (Enum.TryParse(presetName, true, out savedPreset))
            {
                activePreset = savedPreset;
            }

            tuning.CopyFrom(GTXTuningProfile.FromPreset(activePreset));
            for (int i = 0; i < TuningFields.Length; i++)
            {
                FieldInfo field = typeof(GTXTuningProfile).GetField(TuningFields[i]);
                if (field != null)
                {
                    float value = (float)field.GetValue(tuning);
                    field.SetValue(tuning, PlayerPrefs.GetFloat(TuningPrefsPrefix + TuningFields[i], value));
                }
            }

            RefreshGarageSliders();
            currentCallout = activePreset + " tune";
            calloutTimer = 1.8f;
            TuningChanged?.Invoke(tuning);
        }

        private void SaveTuning()
        {
            PlayerPrefs.SetString(TuningPrefsPrefix + "Preset", activePreset.ToString());
            for (int i = 0; i < TuningFields.Length; i++)
            {
                FieldInfo field = typeof(GTXTuningProfile).GetField(TuningFields[i]);
                if (field != null)
                {
                    PlayerPrefs.SetFloat(TuningPrefsPrefix + TuningFields[i], (float)field.GetValue(tuning));
                }
            }

            PlayerPrefs.Save();
        }

        private static FieldInfo ResolveTuningField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            string normalized = fieldName.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "accel":
                case "acceleration":
                case "engine":
                    return typeof(GTXTuningProfile).GetField("acceleration");
                case "top":
                case "speed":
                case "topspeed":
                case "finaldrive":
                    return typeof(GTXTuningProfile).GetField("topSpeed");
                case "grip":
                case "tire":
                case "tires":
                    return typeof(GTXTuningProfile).GetField("grip");
                case "steer":
                case "steering":
                case "steeringresponse":
                    return typeof(GTXTuningProfile).GetField("steeringResponse");
                case "brake":
                case "brakes":
                case "brakepower":
                    return typeof(GTXTuningProfile).GetField("brakePower");
                case "boost":
                case "boostpower":
                    return typeof(GTXTuningProfile).GetField("boostPower");
                case "cool":
                case "cooling":
                    return typeof(GTXTuningProfile).GetField("cooling");
                default:
                    return typeof(GTXTuningProfile).GetField(fieldName);
            }
        }

        private static float ReadNumber(object source, string[] names, float fallback)
        {
            object value = ReadMember(source, names);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch (InvalidCastException)
            {
                return fallback;
            }
            catch (FormatException)
            {
                return fallback;
            }
        }

        private static bool ReadBool(object source, string[] names, bool fallback)
        {
            object value = ReadMember(source, names);
            return value is bool ? (bool)value : fallback;
        }

        private static string ReadText(object source, string[] names, string fallback)
        {
            object value = ReadMember(source, names);
            return value == null ? fallback : value.ToString();
        }

        private static object ReadMember(object source, string[] names)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], flags);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                PropertyInfo property = type.GetProperty(names[i], flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(source, null);
                }
            }

            return null;
        }

        private Text Label(string name, Transform parent, string text, int size, TextAnchor anchor, Vector2 position, Vector2 dimensions)
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
            label.alignment = anchor;
            label.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return label;
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

        private Button Button(string label, Transform parent, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = Panel(label + " Button", parent, Anchor.BottomLeft, position, dimensions, new Color(0.08f, 0.13f, 0.23f, 0.96f));
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.32f, 0.34f, 1f);
            colors.pressedColor = new Color(0.08f, 0.22f, 0.24f, 1f);
            button.colors = colors;
            Label(label + " Text", gameObject.transform, label, 14, TextAnchor.MiddleCenter, Vector2.zero, dimensions);
            return button;
        }

        private Slider Meter(string name, Transform parent, Vector2 position, Vector2 dimensions, Color fillColor)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Image background = root.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.16f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(root.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            Slider slider = root.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            return slider;
        }

        private GameObject Panel(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, position, dimensions);
            return gameObject;
        }

        private static void SetAnchor(RectTransform rect, Anchor anchor, Vector2 position, Vector2 dimensions)
        {
            switch (anchor)
            {
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
                case Anchor.MiddleRight:
                    rect.anchorMin = new Vector2(1f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
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

        private enum Anchor
        {
            BottomLeft,
            TopLeft,
            TopRight,
            TopCenter,
            MiddleRight
        }
    }
}
