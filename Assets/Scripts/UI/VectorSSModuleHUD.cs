using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GTX.UI
{
    [DisallowMultipleComponent]
    public sealed class VectorSSModuleHUD : MonoBehaviour
    {
        private const float PanelWidth = 188f;
        private const float PanelHeight = 52f;
        private const float BarHeight = 5f;
        private const float Padding = 8f;
        private const string FontResourceName = "LegacyRuntime.ttf";

        private static readonly Color PanelColor = new Color(0.02f, 0.025f, 0.03f, 0.82f);
        private static readonly Color PanelEdgeColor = new Color(0.16f, 0.72f, 0.92f, 0.95f);
        private static readonly Color TitleColor = new Color(0.72f, 0.86f, 0.9f, 1f);
        private static readonly Color ValueColor = new Color(1f, 1f, 0.94f, 1f);
        private static readonly Color BarBackColor = new Color(0.12f, 0.14f, 0.15f, 0.95f);
        private static readonly Color BarFillColor = new Color(0.1f, 0.86f, 0.72f, 1f);

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
            panelImage.raycastTarget = false;

            RectTransform edge = CreateImage("Edge", panelRect, PanelEdgeColor);
            edge.anchorMin = new Vector2(0f, 0f);
            edge.anchorMax = new Vector2(0f, 1f);
            edge.pivot = new Vector2(0f, 0.5f);
            edge.sizeDelta = new Vector2(3f, 0f);
            edge.anchoredPosition = Vector2.zero;

            Text titleText = CreateText("Title", panelRect, 11, FontStyle.Bold, TextAnchor.UpperLeft, TitleColor);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(Padding, -24f);
            titleRect.offsetMax = new Vector2(-Padding, -5f);

            Text valueText = CreateText("Value", panelRect, 17, FontStyle.Bold, TextAnchor.MiddleLeft, ValueColor);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 0f);
            valueRect.pivot = new Vector2(0f, 0f);
            valueRect.offsetMin = new Vector2(Padding, 11f);
            valueRect.offsetMax = new Vector2(-Padding, 33f);

            RectTransform barBack = CreateImage("Bar Back", panelRect, BarBackColor);
            barBack.anchorMin = new Vector2(0f, 0f);
            barBack.anchorMax = new Vector2(1f, 0f);
            barBack.pivot = new Vector2(0f, 0f);
            barBack.offsetMin = new Vector2(Padding, Padding);
            barBack.offsetMax = new Vector2(-Padding, Padding + BarHeight);

            RectTransform barFill = CreateImage("Bar Fill", barBack, BarFillColor);
            barFill.anchorMin = new Vector2(0f, 0f);
            barFill.anchorMax = new Vector2(0f, 1f);
            barFill.pivot = new Vector2(0f, 0.5f);
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;

            WidgetView view = new WidgetView();
            view.root = panelRect;
            view.titleText = titleText;
            view.valueText = valueText;
            view.barFill = barFill;
            view.barBack = barBack;
            return view;
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
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

        [System.Serializable]
        public sealed class ModuleWidgetState
        {
            public string moduleId;
            public string title;
            public string value;
            public Vector2 position;
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
                    float availableWidth = barBack.rect.width > 0.01f ? barBack.rect.width : PanelWidth - (Padding * 2f);
                    float width = Mathf.Max(0f, availableWidth * Mathf.Clamp01(normalized));
                    barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                }
            }
        }
    }
}
