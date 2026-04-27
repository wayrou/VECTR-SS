using GTX.Flow;
using GTX.UI;
using GTX.Vehicle;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GTX.Terminal
{
    public sealed class GTXQuacTerminalOverlay : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
        [SerializeField] private KeyCode alternateToggleKey = KeyCode.Backslash;

        private readonly List<string> history = new List<string>();
        private GTXQuacCommandRegistry registry;
        private GTXRuntimeHUD hud;
        private Canvas canvas;
        private Font font;
        private GameObject panel;
        private Text historyText;
        private InputField inputField;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapTerminal()
        {
            if (FindObjectOfType<GTXQuacTerminalOverlay>() != null)
            {
                return;
            }

            new GameObject("GTX QUAC Terminal Overlay").AddComponent<GTXQuacTerminalOverlay>();
        }

        private void Awake()
        {
            font = LoadBuiltInFont();
            registry = new GTXQuacCommandRegistry();
            RegisterContextCommands();
        }

        private void Start()
        {
            EnsureHud();
            BuildTerminal();
            AppendHistory("Q.U.A.C. local terminal ready. Try /help.");
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey) || UnityEngine.Input.GetKeyDown(alternateToggleKey))
            {
                ToggleTerminal();
            }

            if (panel != null && panel.activeSelf && (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                SubmitInput();
            }
        }

        private void RegisterContextCommands()
        {
            registry.Register("/help", _ => GTXQuacCommandResult.Info("Q.U.A.C.", "Commands: /status, /garage, /preset strike|drift|volt, /tune field value, /controls, /flow, /lobby, /loom, /clear."));
            registry.Register("/garage", HandleGarageCommand);
            registry.Register("/controls", HandleControlsCommand);
            registry.Register("/flow", HandleFlowCommand);
            registry.Register("/preset", HandlePresetCommand);
            registry.Register("/tune", HandleTuneCommand);
            registry.Register("/status", HandleStatusCommand);
            registry.Register("/clear", _ =>
            {
                history.Clear();
                RefreshHistory();
                return GTXQuacCommandResult.Info("CLEAR", "Terminal history cleared.");
            });
        }

        private GTXQuacCommandResult HandleGarageCommand(string[] args)
        {
            if (!EnsureHud())
            {
                return GTXQuacCommandResult.Error("GARAGE", "HUD not ready.");
            }

            bool? visible = ParseVisibility(args);
            if (visible.HasValue)
            {
                hud.SetGarageVisible(visible.Value);
                return GTXQuacCommandResult.Info("GARAGE", visible.Value ? "Garage opened." : "Garage closed.");
            }

            hud.ToggleGarage();
            return GTXQuacCommandResult.Info("GARAGE", "Garage toggled.");
        }

        private GTXQuacCommandResult HandleControlsCommand(string[] args)
        {
            if (!EnsureHud())
            {
                return GTXQuacCommandResult.Error("CONTROLS", "HUD not ready.");
            }

            bool? visible = ParseVisibility(args);
            if (visible.HasValue)
            {
                hud.SetControlsVisible(visible.Value);
                return GTXQuacCommandResult.Info("CONTROLS", visible.Value ? "Controls guide shown." : "Controls guide hidden.");
            }

            hud.ToggleControls();
            return GTXQuacCommandResult.Info("CONTROLS", "Controls guide toggled.");
        }

        private GTXQuacCommandResult HandleFlowCommand(string[] args)
        {
            if (!EnsureHud())
            {
                return GTXQuacCommandResult.Error("FLOW", "HUD not ready.");
            }

            bool? visible = ParseVisibility(args);
            if (visible.HasValue)
            {
                hud.SetFlowDebugVisible(visible.Value);
                return GTXQuacCommandResult.Info("FLOW", visible.Value ? "Debug Flow requested." : "Debug Flow hidden.");
            }

            hud.ToggleFlowDebug();
            return GTXQuacCommandResult.Info("FLOW", "Debug Flow toggled.");
        }

        private GTXQuacCommandResult HandlePresetCommand(string[] args)
        {
            if (!EnsureHud())
            {
                return GTXQuacCommandResult.Error("PRESET", "HUD not ready.");
            }

            if (args.Length == 0)
            {
                return GTXQuacCommandResult.Info("PRESET", hud.GetTuningSummary());
            }

            return hud.TryApplyPreset(args[0], out string message)
                ? GTXQuacCommandResult.Info("PRESET", message)
                : GTXQuacCommandResult.Error("PRESET", message);
        }

        private GTXQuacCommandResult HandleTuneCommand(string[] args)
        {
            if (!EnsureHud())
            {
                return GTXQuacCommandResult.Error("TUNE", "HUD not ready.");
            }

            if (args.Length < 2)
            {
                return GTXQuacCommandResult.Info("TUNE", hud.GetTuningSummary());
            }

            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return GTXQuacCommandResult.Error("TUNE", "Value must be a number, for example /tune boost 1.25.");
            }

            return hud.TrySetTuningValue(args[0], value, out string message)
                ? GTXQuacCommandResult.Info("TUNE", message)
                : GTXQuacCommandResult.Error("TUNE", message);
        }

        private GTXQuacCommandResult HandleStatusCommand(string[] args)
        {
            VehicleController vehicle = FindObjectOfType<VehicleController>();
            FlowState flow = FindObjectOfType<FlowState>();
            if (vehicle == null)
            {
                return GTXQuacCommandResult.Error("STATUS", "Vehicle not found.");
            }

            string message = "Speed " + Mathf.RoundToInt(vehicle.SpeedKph) +
                " kph | Gear " + vehicle.CurrentGear +
                " | RPM " + Mathf.RoundToInt(vehicle.RPM) +
                " | Boost " + Mathf.RoundToInt(vehicle.Boost01 * 100f) + "%" +
                " | Heat " + Mathf.RoundToInt(vehicle.Heat01 * 100f) + "%";
            if (flow != null)
            {
                message += " | Flow tier " + flow.Tier;
            }

            return GTXQuacCommandResult.Info("STATUS", message);
        }

        private void ToggleTerminal()
        {
            if (panel == null)
            {
                BuildTerminal();
            }

            bool active = !panel.activeSelf;
            panel.SetActive(active);
            if (active && inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }

        private void SubmitInput()
        {
            if (inputField == null)
            {
                return;
            }

            string command = inputField.text;
            inputField.text = string.Empty;
            if (string.IsNullOrWhiteSpace(command))
            {
                inputField.ActivateInputField();
                return;
            }

            AppendHistory("> " + command.Trim());
            GTXQuacCommandResult result = registry.Run(command);
            AppendHistory(result.Title + ": " + result.Message);
            inputField.ActivateInputField();
        }

        private bool EnsureHud()
        {
            if (hud == null)
            {
                hud = FindObjectOfType<GTXRuntimeHUD>();
            }

            return hud != null;
        }

        private void BuildTerminal()
        {
            if (panel != null)
            {
                return;
            }

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
            panel = Panel("QUAC Terminal", root, new Vector2(-32f, 32f), new Vector2(560f, 178f), new Color(0.015f, 0.018f, 0.02f, 0.88f));
            Label("QUAC Title", panel.transform, "Q.U.A.C.", 16, TextAnchor.MiddleLeft, new Vector2(16f, 142f), new Vector2(100f, 22f));
            Label("QUAC Hint", panel.transform, "` toggles terminal", 11, TextAnchor.MiddleRight, new Vector2(398f, 145f), new Vector2(144f, 18f));
            historyText = Label("QUAC History", panel.transform, string.Empty, 12, TextAnchor.UpperLeft, new Vector2(16f, 50f), new Vector2(528f, 90f));
            inputField = InputBox("QUAC Input", panel.transform, new Vector2(16f, 14f), new Vector2(528f, 28f));
            panel.SetActive(false);
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

        private GameObject Panel(string name, Transform parent, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return gameObject;
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
            label.color = Color.white;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return label;
        }

        private InputField InputBox(string name, Transform parent, Vector2 position, Vector2 dimensions)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Image background = root.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Text text = Label("Text", root.transform, string.Empty, 13, TextAnchor.MiddleLeft, new Vector2(8f, 4f), new Vector2(dimensions.x - 16f, dimensions.y - 8f));
            Text placeholder = Label("Placeholder", root.transform, "/help", 13, TextAnchor.MiddleLeft, new Vector2(8f, 4f), new Vector2(dimensions.x - 16f, dimensions.y - 8f));
            placeholder.color = new Color(1f, 1f, 1f, 0.38f);

            InputField field = root.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.caretColor = Color.white;
            field.selectionColor = new Color(0.1f, 0.75f, 1f, 0.42f);
            return field;
        }

        private void AppendHistory(string line)
        {
            history.Add(line);
            while (history.Count > 7)
            {
                history.RemoveAt(0);
            }

            RefreshHistory();
        }

        private void RefreshHistory()
        {
            if (historyText != null)
            {
                historyText.text = string.Join("\n", history.ToArray());
            }
        }

        private static bool? ParseVisibility(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            string value = args[0].Trim().ToLowerInvariant();
            if (value == "on" || value == "open" || value == "show" || value == "true")
            {
                return true;
            }

            if (value == "off" || value == "close" || value == "hide" || value == "false")
            {
                return false;
            }

            return null;
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
    }
}
