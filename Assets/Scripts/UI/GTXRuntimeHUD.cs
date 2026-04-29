using System;
using System.Collections.Generic;
using System.Reflection;
using GTX.Core;
using GTX.Data;
using GTX.Flow;
using GTX.Vehicle;
using GTX.Visuals;
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
        [SerializeField] private KeyCode controlsToggleKey = KeyCode.F1;
        [SerializeField] private KeyCode flowToggleKey = KeyCode.F3;

        private Canvas canvas;
        private Font font;
        private Text speedText;
        private Text gearText;
        private Text rpmText;
        private Text boostText;
        private Text heatText;
        private TachometerGraphic tachometer;
        private RectTransform tachNeedle;
        private RectTransform hudContentRoot;
        private Text feedbackText;
        private Text controlsText;
        private Text flowText;
        private GameObject hudPanel;
        private GameObject controlsPanel;
        private GameObject flowPanel;
        private GameObject garagePanel;
        private readonly List<string> moduleControlHints = new List<string>();
        private Slider boostSlider;
        private Slider flowSlider;
        private GTXTelemetrySnapshot demoTelemetry;
        private float calloutTimer;
        private string currentCallout = "Ready";
        private const string TuningPrefsPrefix = "GTX.RuntimeTuning.";
        private const float DefaultHudWindowScale = 2f;
        private static readonly string[] TuningFields = { "acceleration", "topSpeed", "grip", "steeringResponse", "brakePower", "boostPower", "cooling" };

        public event Action<GTXTuningProfile> TuningChanged;
        public GTXTuningProfile Tuning { get { return tuning; } }
        public GTXTuningPreset ActivePreset { get { return activePreset; } }
        public Canvas HudCanvas { get { return canvas; } }

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

        public void SetCoreHeatVisible(bool visible)
        {
            if (heatText != null)
            {
                heatText.gameObject.SetActive(visible);
            }

        }

        public void SetModuleControlHints(IEnumerable<string> hints)
        {
            moduleControlHints.Clear();
            if (hints != null)
            {
                foreach (string hint in hints)
                {
                    if (!string.IsNullOrEmpty(hint))
                    {
                        moduleControlHints.Add(hint);
                    }
                }
            }

            RefreshControlsText();
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
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleGarage();
            }

            if (Input.GetKeyDown(controlsToggleKey) || GTXInput.ButtonDown(10))
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
            speedText.text = Mathf.RoundToInt(snapshot.speedKph * 0.621371f).ToString("000") + " MPH";
            gearText.text = FormatGearNumber(snapshot.gear);
            if (rpmText != null)
            {
                rpmText.text = Mathf.RoundToInt(snapshot.rpm).ToString("0") + " RPM";
            }

            if (boostText != null)
            {
                boostText.text = "BOOST " + Mathf.RoundToInt(snapshot.boost01 * 100f) + "%";
            }

            if (heatText != null)
            {
                heatText.text = "HEAT " + Mathf.RoundToInt(snapshot.heat01 * 100f) + "%";
            }

            if (tachometer != null)
            {
                tachometer.Rpm01 = snapshot.rpm01;
            }

            if (tachNeedle != null)
            {
                tachNeedle.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(224f, -44f, Mathf.Clamp01(snapshot.rpm01)));
            }

            if (boostSlider != null)
            {
                boostSlider.value = snapshot.boost01;
            }

            if (flowSlider != null)
            {
                flowSlider.value = snapshot.flow01;
            }

            if (flowText != null)
            {
                flowText.text = "FLOW " + Mathf.RoundToInt(snapshot.flow01 * 100f) + "%";
            }

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

        private static string FormatGearNumber(int gear)
        {
            if (gear < 0)
            {
                return "R";
            }

            if (gear == 0)
            {
                return "N";
            }

            return gear.ToString("0");
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
            hudPanel = Panel("HUD", root, Anchor.BottomLeft, new Vector2(24f, 124f), new Vector2(332f, 136f), VectrStyleTokens.WithAlpha(VectrStyleTokens.AsphaltNavy, 0.88f));
            ApplyDefaultHudWindowScale(hudPanel);
            hudContentRoot = ScaledContentRoot("HUD Dial Content", hudPanel.transform, new Vector2(332f, 136f));
            tachometer = CreateTachometer(hudContentRoot, new Vector2(10f, 10f), new Vector2(124f, 116f));
            tachNeedle = CreateTachNeedle(hudContentRoot, new Vector2(72f, 58f), new Vector2(5f, 54f));
            LabelPlain("Tach 0", hudContentRoot, "0", 13, TextAnchor.MiddleCenter, new Vector2(55f, 12f), new Vector2(20f, 18f));
            LabelPlain("Tach 4", hudContentRoot, "4", 13, TextAnchor.MiddleCenter, new Vector2(19f, 42f), new Vector2(20f, 18f));
            LabelPlain("Tach 5", hudContentRoot, "5", 13, TextAnchor.MiddleCenter, new Vector2(23f, 73f), new Vector2(20f, 18f));
            LabelPlain("Tach 6", hudContentRoot, "6", 13, TextAnchor.MiddleCenter, new Vector2(47f, 96f), new Vector2(20f, 18f));
            LabelPlain("Tach 7", hudContentRoot, "7", 13, TextAnchor.MiddleCenter, new Vector2(78f, 96f), new Vector2(20f, 18f));
            LabelPlain("Tach 8", hudContentRoot, "8", 13, TextAnchor.MiddleCenter, new Vector2(103f, 73f), new Vector2(20f, 18f));
            LabelPlain("Tach 9", hudContentRoot, "9", 13, TextAnchor.MiddleCenter, new Vector2(107f, 42f), new Vector2(20f, 18f));
            rpmText = LabelPlain("RPM", hudContentRoot, "900 RPM", 10, TextAnchor.MiddleCenter, new Vector2(42f, 31f), new Vector2(62f, 18f));
            gearText = LabelPlain("Gear", hudContentRoot, "1", 52, TextAnchor.MiddleCenter, new Vector2(142f, 50f), new Vector2(64f, 62f));
            LabelPlain("Shift Label", hudContentRoot, "SHIFT", 12, TextAnchor.MiddleCenter, new Vector2(146f, 35f), new Vector2(56f, 16f));
            speedText = LabelPlain("Speed", hudContentRoot, "000 MPH", 31, TextAnchor.MiddleLeft, new Vector2(213f, 50f), new Vector2(106f, 48f));
            boostText = LabelPlain("Boost", hudContentRoot, "BOOST 0%", 10, TextAnchor.MiddleLeft, new Vector2(214f, 24f), new Vector2(78f, 16f));
            boostSlider = Meter("Boost Meter", hudContentRoot, new Vector2(214f, 18f), new Vector2(92f, 5f), VectrStyleTokens.SafetyOrange);
            heatText = LabelPlain("Heat", hudContentRoot, "HEAT 0%", 10, TextAnchor.MiddleLeft, new Vector2(214f, 8f), new Vector2(78f, 16f));

            feedbackText = Label("Feedback", root, "Ready", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(280f, 46f));
            SetAnchor(feedbackText.rectTransform, Anchor.TopCenter, new Vector2(0f, -76f), new Vector2(280f, 46f));

            controlsPanel = Panel("Controls Overlay", root, Anchor.TopLeft, new Vector2(24f, -24f), new Vector2(330f, 104f), VectrStyleTokens.WithAlpha(VectrStyleTokens.AsphaltNavy, 0.72f));
            ApplyDefaultHudWindowScale(controlsPanel);
            Label("Controls Title", controlsPanel.transform, "CONTROLS", 12, TextAnchor.MiddleLeft, new Vector2(12f, 78f), new Vector2(86f, 18f));
            controlsText = Label("Controls Text", controlsPanel.transform, string.Empty, 9, TextAnchor.MiddleLeft, new Vector2(12f, 3f), new Vector2(306f, 76f));
            RefreshControlsText();

            flowPanel = Panel("Debug Flow", root, Anchor.TopRight, new Vector2(-32f, -32f), new Vector2(224f, 72f), VectrStyleTokens.WithAlpha(VectrStyleTokens.DeepViolet, 0.78f));
            ApplyDefaultHudWindowScale(flowPanel);
            flowText = Label("Flow Text", flowPanel.transform, "FLOW 0%", 15, TextAnchor.MiddleLeft, new Vector2(16f, 38f), new Vector2(92f, 20f));
            flowSlider = Meter("Flow Meter", flowPanel.transform, new Vector2(16f, 24f), new Vector2(192f, 8f), VectrStyleTokens.HotMagenta, true);
            flowPanel.SetActive(false);

            BuildGarage(root);
        }

        private void RefreshControlsText()
        {
            if (controlsText == null)
            {
                return;
            }

            string text =
                "W/S or R2/L2 throttle/brake   A/D steer\n" +
                "Arrows camera   Q/E gears\n" +
                "Hold Q reverse   Shift clutch   Space drift\n" +
                "F / Circle boost   X drift   Triangle jump   Z/C slam   N guard   R reset\n" +
                "Tab garage   F4 pixels   F5 camera   ` QUAC   F3 flow   F1 hide";

            if (moduleControlHints.Count > 0)
            {
                text += "\nModules: " + string.Join("   ", moduleControlHints.ToArray());
            }

            controlsText.text = text;
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
            garagePanel = Panel("Garage Tuning", root, Anchor.MiddleRight, new Vector2(-34f, 0f), new Vector2(324f, 438f), VectrStyleTokens.WithAlpha(VectrStyleTokens.OilGray, 0.96f));
            ApplyDefaultHudWindowScale(garagePanel);
            Label("Garage Title", garagePanel.transform, "DASH BAY", 24, TextAnchor.MiddleLeft, new Vector2(18f, 392f), new Vector2(150f, 30f));

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
            Slider slider = Meter(label + " Slider", garagePanel.transform, new Vector2(132f, y + 6f), new Vector2(138f, 8f), VectrStyleTokens.ElectricCyan);
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

        private static RectTransform ScaledContentRoot(string name, Transform parent, Vector2 designSize)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = designSize;
            gameObject.AddComponent<VectorSSPanelContentScaler>().Configure(designSize);
            return rect;
        }

        private TachometerGraphic CreateTachometer(Transform parent, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = new GameObject("RPM Dial");
            gameObject.transform.SetParent(parent, false);
            TachometerGraphic graphic = gameObject.AddComponent<TachometerGraphic>();
            graphic.raycastTarget = false;
            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return graphic;
        }

        private RectTransform CreateTachNeedle(Transform parent, Vector2 pivotPosition, Vector2 dimensions)
        {
            GameObject gameObject = new GameObject("RPM Needle");
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = VectrStyleTokens.SignalRed;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = pivotPosition;
            rect.sizeDelta = dimensions;
            return rect;
        }

        private Text LabelPlain(string name, Transform parent, string text, int size, TextAnchor anchor, Vector2 position, Vector2 dimensions)
        {
            Text label = Label(name, parent, text, size, anchor, position, dimensions);
            VectorSSDraggableResizablePanel drag = label.GetComponent<VectorSSDraggableResizablePanel>();
            if (drag != null)
            {
                Destroy(drag);
            }

            label.raycastTarget = false;
            return label;
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
            label.color = VectrStyleTokens.BoneWhite;
            label.raycastTarget = true;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            gameObject.AddComponent<VectorSSDraggableResizablePanel>().Configure(new Vector2(Mathf.Min(44f, dimensions.x), Mathf.Min(18f, dimensions.y)), true, 14f);
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
            GameObject gameObject = Panel(label + " Button", parent, Anchor.BottomLeft, position, dimensions, VectrStyleTokens.WithAlpha(VectrStyleTokens.AsphaltNavy, 0.96f));
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = VectrStyleTokens.WithAlpha(VectrStyleTokens.ElectricCyan, 0.88f);
            colors.pressedColor = VectrStyleTokens.WithAlpha(VectrStyleTokens.DeepViolet, 0.9f);
            button.colors = colors;
            Label(label + " Text", gameObject.transform, label, 14, TextAnchor.MiddleCenter, Vector2.zero, dimensions);
            return button;
        }

        private Slider Meter(string name, Transform parent, Vector2 position, Vector2 dimensions, Color fillColor, bool draggable = false)
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
            background.color = VectrStyleTokens.WithAlpha(VectrStyleTokens.InkBlack, 0.72f);

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
            if (draggable)
            {
                root.AddComponent<VectorSSDraggableResizablePanel>().Configure(new Vector2(Mathf.Min(48f, dimensions.x), Mathf.Min(8f, dimensions.y)), true, 12f);
            }
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
            AddHardwareFrame(rect);
            gameObject.AddComponent<VectorSSDraggableResizablePanel>().Configure(new Vector2(Mathf.Min(160f, dimensions.x), Mathf.Min(72f, dimensions.y)), true, 24f);
            return gameObject;
        }

        private static void ApplyDefaultHudWindowScale(GameObject panel)
        {
            if (panel != null)
            {
                panel.transform.localScale = Vector3.one * DefaultHudWindowScale;
            }
        }

        private static void AddHardwareFrame(RectTransform panel)
        {
            CreatePanelStripe(panel, "Top Ink Frame", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), Vector2.zero, VectrStyleTokens.InkBlack);
            CreatePanelStripe(panel, "Bottom Ink Frame", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero, new Vector2(0f, 4f), VectrStyleTokens.InkBlack);
            CreatePanelStripe(panel, "Left Ink Frame", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Vector2(4f, 0f), VectrStyleTokens.InkBlack);
            CreatePanelStripe(panel, "Right Ink Frame", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0f), new Vector2(-4f, 0f), Vector2.zero, VectrStyleTokens.InkBlack);
            CreatePanelScrew(panel, "Screw NW", new Vector2(8f, -8f));
            CreatePanelScrew(panel, "Screw NE", new Vector2(-16f, -8f), true);
            CreatePanelScrew(panel, "Screw SW", new Vector2(8f, 16f), false, true);
            CreatePanelScrew(panel, "Screw SE", new Vector2(-16f, 16f), true, true);
        }

        private static void CreatePanelStripe(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject stripeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            stripeObject.transform.SetParent(parent, false);
            RectTransform stripe = stripeObject.GetComponent<RectTransform>();
            stripe.anchorMin = anchorMin;
            stripe.anchorMax = anchorMax;
            stripe.pivot = pivot;
            stripe.offsetMin = offsetMin;
            stripe.offsetMax = offsetMax;
            Image image = stripeObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void CreatePanelScrew(RectTransform parent, string name, Vector2 anchoredPosition, bool right = false, bool bottom = false)
        {
            GameObject screwObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            screwObject.transform.SetParent(parent, false);
            RectTransform screw = screwObject.GetComponent<RectTransform>();
            screw.anchorMin = new Vector2(right ? 1f : 0f, bottom ? 0f : 1f);
            screw.anchorMax = screw.anchorMin;
            screw.pivot = new Vector2(0f, 1f);
            screw.anchoredPosition = anchoredPosition;
            screw.sizeDelta = new Vector2(8f, 8f);
            Image image = screwObject.GetComponent<Image>();
            image.color = VectrStyleTokens.WarmConcreteGray;
            image.raycastTarget = false;
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

        private sealed class TachometerGraphic : Graphic
        {
            private float rpm01;

            public float Rpm01
            {
                get { return rpm01; }
                set
                {
                    float clamped = Mathf.Clamp01(value);
                    if (!Mathf.Approximately(rpm01, clamped))
                    {
                        rpm01 = clamped;
                        SetVerticesDirty();
                    }
                }
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Rect r = rectTransform.rect;
                Vector2 center = new Vector2(r.xMin + r.width * 0.5f, r.yMin + r.height * 0.42f);
                float radius = Mathf.Min(r.width, r.height) * 0.45f;
                const float start = 224f;
                const float end = -44f;

                AddArc(vh, center, radius, 5f, start, end, 36, VectrStyleTokens.BoneWhite);
                AddArc(vh, center, radius + 1f, 7f, 18f, -42f, 10, VectrStyleTokens.SafetyOrange);
                AddArc(vh, center, radius - 10f, 2f, start, end, 28, VectrStyleTokens.WithAlpha(VectrStyleTokens.WarmConcreteGray, 0.72f));

                for (int i = 0; i <= 9; i++)
                {
                    float t = i / 9f;
                    float angle = Mathf.Lerp(start, end, t);
                    float length = i >= 7 ? 13f : 9f;
                    float width = i >= 7 ? 3.2f : 2.2f;
                    Vector2 outer = center + Direction(angle) * (radius + 2f);
                    Vector2 inner = center + Direction(angle) * (radius - length);
                    AddLine(vh, inner, outer, width, VectrStyleTokens.BoneWhite);
                }

                AddDisc(vh, center, 7f, VectrStyleTokens.InkBlack, 14);
                AddDisc(vh, center, 4f, VectrStyleTokens.BoneWhite, 12);
            }

            private static void AddArc(VertexHelper vh, Vector2 center, float radius, float thickness, float startDeg, float endDeg, int segments, Color color)
            {
                Vector2 previousOuter = center + Direction(startDeg) * radius;
                Vector2 previousInner = center + Direction(startDeg) * (radius - thickness);
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float angle = Mathf.Lerp(startDeg, endDeg, t);
                    Vector2 outer = center + Direction(angle) * radius;
                    Vector2 inner = center + Direction(angle) * (radius - thickness);
                    AddQuad(vh, previousInner, previousOuter, outer, inner, color);
                    previousOuter = outer;
                    previousInner = inner;
                }
            }

            private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a0 = 360f * i / segments;
                    float a1 = 360f * (i + 1) / segments;
                    AddTriangle(vh, center, center + Direction(a0) * radius, center + Direction(a1) * radius, color);
                }
            }

            private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
            {
                Vector2 delta = b - a;
                if (delta.sqrMagnitude < 0.001f)
                {
                    return;
                }

                Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
                AddQuad(vh, a - normal, a + normal, b + normal, b - normal, color);
            }

            private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
            {
                int startIndex = vh.currentVertCount;
                AddVertex(vh, a, color);
                AddVertex(vh, b, color);
                AddVertex(vh, c, color);
                AddVertex(vh, d, color);
                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
            }

            private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                int startIndex = vh.currentVertCount;
                AddVertex(vh, a, color);
                AddVertex(vh, b, color);
                AddVertex(vh, c, color);
                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            }

            private static void AddVertex(VertexHelper vh, Vector2 position, Color color)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = position;
                vertex.color = color;
                vh.AddVert(vertex);
            }

            private static Vector2 Direction(float degrees)
            {
                float radians = degrees * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
        }
    }
}
