using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GTX.UI
{
    [DisallowMultipleComponent]
    public sealed class VectorSSModuleHUD : MonoBehaviour
    {
        private const float PanelWidth = 202f;
        private const float PanelHeight = 64f;
        private const float BarHeight = 8f;
        private const float BorderThickness = 4f;
        private const int BarSegmentCount = 12;
        private const string FontResourceName = "LegacyRuntime.ttf";

        private static readonly Color PanelColor = new Color(0.012f, 0.014f, 0.016f, 0.96f);
        private static readonly Color BezelColor = new Color(0.045f, 0.048f, 0.052f, 1f);
        private static readonly Color InnerPlateColor = new Color(0.018f, 0.022f, 0.024f, 0.96f);
        private static readonly Color BorderColor = new Color(0.005f, 0.006f, 0.007f, 1f);
        private static readonly Color BracketColor = new Color(0.16f, 0.17f, 0.17f, 1f);
        private static readonly Color ScrewColor = new Color(0.33f, 0.35f, 0.34f, 1f);
        private static readonly Color ScrewSlotColor = new Color(0.05f, 0.055f, 0.055f, 1f);
        private static readonly Color TitleColor = new Color(0.58f, 0.61f, 0.56f, 1f);
        private static readonly Color ValueColor = new Color(0.90f, 0.86f, 0.72f, 1f);
        private static readonly Color BarBackColor = new Color(0.035f, 0.039f, 0.04f, 1f);
        private static readonly Color BarDeadColor = new Color(0.09f, 0.1f, 0.098f, 0.96f);
        private static readonly Color ReadyColor = new Color(0.42f, 0.50f, 0.24f, 1f);
        private static readonly Color WarningColor = new Color(0.78f, 0.32f, 0.075f, 1f);
        private static readonly Color CautionColor = new Color(0.82f, 0.56f, 0.16f, 1f);
        private static readonly Color LabelPlateColor = new Color(0.06f, 0.065f, 0.063f, 1f);

        [SerializeField] private Canvas targetCanvas;

        private readonly Dictionary<string, WidgetView> widgetViews = new Dictionary<string, WidgetView>();
        private RectTransform root;
        private Font font;

        public Canvas TargetCanvas { get { return targetCanvas; } }

        public void Configure(Canvas canvas, IEnumerable<ModuleWidgetState> widgets)
        {
            targetCanvas = canvas;
            EnsureRoot();
            SetWidgets(widgets);
        }

        public void SetWidgets(IEnumerable<ModuleWidgetState> widgets)
        {
            EnsureRoot();
            if (root == null)
            {
                return;
            }

            HashSet<string> liveIds = new HashSet<string>();
            if (widgets != null)
            {
                foreach (ModuleWidgetState widget in widgets)
                {
                    if (widget == null || !widget.visible || string.IsNullOrEmpty(widget.moduleId))
                    {
                        continue;
                    }

                    WidgetView view = GetOrCreateWidget(widget.moduleId);
                    view.Apply(widget, font);
                    liveIds.Add(widget.moduleId);
                }
            }

            List<string> staleIds = new List<string>();
            foreach (KeyValuePair<string, WidgetView> pair in widgetViews)
            {
                if (!liveIds.Contains(pair.Key))
                {
                    staleIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                RemoveWidget(staleIds[i]);
            }
        }

        public void UpdateWidget(string moduleId, string value, float normalized)
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                return;
            }

            WidgetView view;
            if (!widgetViews.TryGetValue(moduleId, out view))
            {
                return;
            }

            view.SetValue(value, normalized);
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, WidgetView> pair in widgetViews)
            {
                if (pair.Value.root != null)
                {
                    Destroy(pair.Value.root.gameObject);
                }
            }

            widgetViews.Clear();
        }

        private void OnDestroy()
        {
            Clear();
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (targetCanvas == null)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>(FontResourceName);

            GameObject rootObject = new GameObject("VectorSS Module HUD", typeof(RectTransform));
            rootObject.transform.SetParent(targetCanvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
        }

        private WidgetView GetOrCreateWidget(string moduleId)
        {
            WidgetView view;
            if (widgetViews.TryGetValue(moduleId, out view))
            {
                return view;
            }

            view = CreateWidget(moduleId);
            widgetViews.Add(moduleId, view);
            return view;
        }

        private WidgetView CreateWidget(string moduleId)
        {
            GameObject panelObject = new GameObject("Module Widget - " + moduleId, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(root, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;
            panelObject.AddComponent<VectorSSDraggableResizablePanel>().Configure(new Vector2(96f, 38f), true, 28f);

            RectTransform bezel = CreateImage("Black Bezel", panelRect, BezelColor);
            Stretch(bezel, 3f, 3f, -3f, -3f);

            RectTransform innerPlate = CreateImage("Recessed Plate", panelRect, InnerPlateColor);
            Stretch(innerPlate, 10f, 8f, -10f, -8f);

            CreateBorder("Top Border", panelRect, 0f, PanelHeight - BorderThickness, PanelWidth, BorderThickness);
            CreateBorder("Bottom Border", panelRect, 0f, 0f, PanelWidth, BorderThickness);
            CreateBorder("Left Border", panelRect, 0f, 0f, BorderThickness, PanelHeight);
            CreateBorder("Right Border", panelRect, PanelWidth - BorderThickness, 0f, BorderThickness, PanelHeight);

            CreateBracket(panelRect, 12f, PanelHeight - 12f, true, true);
            CreateBracket(panelRect, PanelWidth - 24f, PanelHeight - 12f, false, true);
            CreateBracket(panelRect, 12f, 10f, true, false);
            CreateBracket(panelRect, PanelWidth - 24f, 10f, false, false);

            CreateScrew(panelRect, 8f, -8f);
            CreateScrew(panelRect, PanelWidth - 16f, -8f);
            CreateScrew(panelRect, 8f, -PanelHeight + 16f);
            CreateScrew(panelRect, PanelWidth - 16f, -PanelHeight + 16f);

            RectTransform titlePlate = CreateImage("Stamped Label Plate", panelRect, LabelPlateColor);
            titlePlate.anchorMin = new Vector2(0f, 1f);
            titlePlate.anchorMax = new Vector2(1f, 1f);
            titlePlate.pivot = new Vector2(0f, 1f);
            titlePlate.offsetMin = new Vector2(24f, -22f);
            titlePlate.offsetMax = new Vector2(-38f, -8f);

            RectTransform ledBezel = CreateImage("LED Bezel", panelRect, BorderColor);
            ledBezel.anchorMin = new Vector2(1f, 1f);
            ledBezel.anchorMax = new Vector2(1f, 1f);
            ledBezel.pivot = new Vector2(1f, 1f);
            ledBezel.sizeDelta = new Vector2(18f, 12f);
            ledBezel.anchoredPosition = new Vector2(-14f, -9f);

            RectTransform led = CreateImage("Status LED", ledBezel, ReadyColor);
            Stretch(led, 3f, 3f, -3f, -3f);

            Text titleText = CreateText("Title", panelRect, 10, FontStyle.Bold, TextAnchor.MiddleLeft, TitleColor);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(28f, -22f);
            titleRect.offsetMax = new Vector2(-42f, -8f);

            Text valueText = CreateText("Value", panelRect, 16, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 0f);
            valueRect.pivot = new Vector2(0f, 0f);
            valueRect.offsetMin = new Vector2(24f, 21f);
            valueRect.offsetMax = new Vector2(-12f, 43f);

            RectTransform barBack = CreateImage("Bar Back", panelRect, BarBackColor);
            barBack.anchorMin = new Vector2(0f, 0f);
            barBack.anchorMax = new Vector2(1f, 0f);
            barBack.pivot = new Vector2(0f, 0f);
            barBack.offsetMin = new Vector2(24f, 10f);
            barBack.offsetMax = new Vector2(-14f, 10f + BarHeight);

            RectTransform barFill = CreateImage("Bar Fill", barBack, new Color(ReadyColor.r, ReadyColor.g, ReadyColor.b, 0.22f));
            barFill.anchorMin = new Vector2(0f, 0f);
            barFill.anchorMax = new Vector2(0f, 1f);
            barFill.pivot = new Vector2(0f, 0.5f);
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;

            List<Image> barSegments = new List<Image>(BarSegmentCount);
            for (int i = 0; i < BarSegmentCount; i++)
            {
                RectTransform segment = CreateImage("Mechanical Segment " + (i + 1).ToString("00"), barBack, BarDeadColor);
                segment.anchorMin = new Vector2((float)i / BarSegmentCount, 0f);
                segment.anchorMax = new Vector2((float)(i + 1) / BarSegmentCount, 1f);
                segment.pivot = new Vector2(0f, 0.5f);
                segment.offsetMin = new Vector2(i == 0 ? 1f : 2f, 1f);
                segment.offsetMax = new Vector2(i == BarSegmentCount - 1 ? -1f : -2f, -1f);
                barSegments.Add(segment.GetComponent<Image>());
            }

            RectTransform statusRail = CreateImage("Lower Warning Rail", panelRect, ReadyColor);
            statusRail.anchorMin = new Vector2(0f, 0f);
            statusRail.anchorMax = new Vector2(1f, 0f);
            statusRail.pivot = new Vector2(0f, 0f);
            statusRail.offsetMin = new Vector2(24f, 5f);
            statusRail.offsetMax = new Vector2(-14f, 7f);

            WidgetView view = new WidgetView();
            view.root = panelRect;
            view.titleText = titleText;
            view.valueText = valueText;
            view.barFill = barFill;
            view.barBack = barBack;
            view.ledImage = led.GetComponent<Image>();
            view.statusRailImage = statusRail.GetComponent<Image>();
            view.barFillImage = barFill.GetComponent<Image>();
            view.barSegments = barSegments;
            return view;
        }

        private RectTransform CreateBorder(string name, RectTransform parent, float x, float y, float width, float height)
        {
            RectTransform border = CreateImage(name, parent, BorderColor);
            border.anchorMin = new Vector2(0f, 0f);
            border.anchorMax = new Vector2(0f, 0f);
            border.pivot = new Vector2(0f, 0f);
            border.anchoredPosition = new Vector2(x, y);
            border.sizeDelta = new Vector2(width, height);
            return border;
        }

        private void CreateBracket(RectTransform parent, float x, float y, bool left, bool top)
        {
            RectTransform horizontal = CreateImage("Steel Bracket H", parent, BracketColor);
            horizontal.anchorMin = new Vector2(0f, 0f);
            horizontal.anchorMax = new Vector2(0f, 0f);
            horizontal.pivot = new Vector2(0f, 0f);
            horizontal.anchoredPosition = new Vector2(x, y);
            horizontal.sizeDelta = new Vector2(12f, 3f);

            RectTransform vertical = CreateImage("Steel Bracket V", parent, BracketColor);
            vertical.anchorMin = new Vector2(0f, 0f);
            vertical.anchorMax = new Vector2(0f, 0f);
            vertical.pivot = new Vector2(0f, 0f);
            vertical.anchoredPosition = new Vector2(left ? x : x + 9f, top ? y - 9f : y);
            vertical.sizeDelta = new Vector2(3f, 12f);
        }

        private void CreateScrew(RectTransform parent, float x, float y)
        {
            RectTransform screw = CreateImage("Screw Head", parent, ScrewColor);
            screw.anchorMin = new Vector2(0f, 1f);
            screw.anchorMax = new Vector2(0f, 1f);
            screw.pivot = new Vector2(0f, 1f);
            screw.anchoredPosition = new Vector2(x, y);
            screw.sizeDelta = new Vector2(8f, 8f);

            RectTransform slot = CreateImage("Screw Slot", screw, ScrewSlotColor);
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = Vector2.zero;
            slot.sizeDelta = new Vector2(6f, 1.5f);
        }

        private void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private RectTransform CreateImage(string name, RectTransform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return imageObject.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, RectTransform parent, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void RemoveWidget(string moduleId)
        {
            WidgetView view;
            if (!widgetViews.TryGetValue(moduleId, out view))
            {
                return;
            }

            if (view.root != null)
            {
                Destroy(view.root.gameObject);
            }

            widgetViews.Remove(moduleId);
        }

        public bool TryReadWidgetLayout(string moduleId, out Vector2 position, out Vector2 size, out float scale)
        {
            position = Vector2.zero;
            size = new Vector2(PanelWidth, PanelHeight);
            scale = 1f;
            WidgetView view;
            if (string.IsNullOrEmpty(moduleId) || !widgetViews.TryGetValue(moduleId, out view) || view.root == null)
            {
                return false;
            }

            position = view.root.anchoredPosition;
            size = view.root.sizeDelta;
            scale = view.root.localScale.x;
            return true;
        }

        [System.Serializable]
        public sealed class ModuleWidgetState
        {
            public string moduleId;
            public string title;
            public string value;
            public Vector2 position;
            public Vector2 size = new Vector2(PanelWidth, PanelHeight);
            public float scale = 1f;
            public bool visible = true;
        }

        private sealed class WidgetView
        {
            public RectTransform root;
            public Text titleText;
            public Text valueText;
            public RectTransform barBack;
            public RectTransform barFill;
            public Image barFillImage;
            public Image ledImage;
            public Image statusRailImage;
            public List<Image> barSegments;

            public void Apply(ModuleWidgetState state, Font activeFont)
            {
                if (titleText != null)
                {
                    titleText.font = activeFont;
                    titleText.text = string.IsNullOrEmpty(state.title) ? state.moduleId : state.title;
                }

                if (valueText != null)
                {
                    valueText.font = activeFont;
                }

                if (root != null)
                {
                    root.anchoredPosition = state.position;
                    root.sizeDelta = new Vector2(
                        Mathf.Max(96f, state.size.x <= 0.01f ? PanelWidth : state.size.x),
                        Mathf.Max(38f, state.size.y <= 0.01f ? PanelHeight : state.size.y));
                    root.localScale = Vector3.one * Mathf.Max(0.01f, state.scale);
                    root.gameObject.SetActive(state.visible);
                }

                SetValue(state.value, 0f);
            }

            public void SetValue(string value, float normalized)
            {
                if (valueText != null)
                {
                    valueText.text = string.IsNullOrEmpty(value) ? "--" : value;
                }

                if (barFill != null && barBack != null)
                {
                    float availableWidth = barBack.rect.width > 0.01f ? barBack.rect.width : PanelWidth - 38f;
                    float clamped = Mathf.Clamp01(normalized);
                    float width = Mathf.Max(0f, availableWidth * clamped);
                    barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    Color statusColor = GetStatusColor(clamped);
                    bool isWarning = clamped < 0.2f;

                    if (barFillImage != null)
                    {
                        barFillImage.color = new Color(statusColor.r, statusColor.g, statusColor.b, 0.22f);
                    }

                    if (ledImage != null)
                    {
                        ledImage.color = statusColor;
                    }

                    if (statusRailImage != null)
                    {
                        statusRailImage.color = statusColor;
                    }

                    if (valueText != null)
                    {
                        valueText.color = isWarning ? new Color(0.90f, 0.66f, 0.48f, 1f) : ValueColor;
                    }

                    if (barSegments != null)
                    {
                        int activeSegments = Mathf.CeilToInt(clamped * barSegments.Count);
                        for (int i = 0; i < barSegments.Count; i++)
                        {
                            if (barSegments[i] != null)
                            {
                                barSegments[i].color = i < activeSegments ? statusColor : BarDeadColor;
                            }
                        }
                    }
                }
            }

            private static Color GetStatusColor(float normalized)
            {
                if (normalized < 0.2f)
                {
                    return WarningColor;
                }

                if (normalized < 0.55f)
                {
                    return CautionColor;
                }

                return ReadyColor;
            }
        }
    }
}
