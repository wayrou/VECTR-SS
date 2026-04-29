using UnityEngine;
using UnityEngine.EventSystems;

namespace GTX.UI
{
    [DisallowMultipleComponent]
    public sealed class VectorSSDraggableResizablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] private float dragHeaderHeight = 34f;
        [SerializeField] private float resizeHandleSize = 24f;
        [SerializeField] private Vector2 minSize = new Vector2(180f, 92f);
        [SerializeField] private bool wholeBodyDraggable = true;

        private RectTransform rect;
        private Canvas canvas;
        private bool dragging;
        private bool resizing;
        private Vector2 pointerOffset;
        private Vector2 resizeStartPointer;
        private Vector2 resizeStartSize;

        public void Configure(Vector2 nextMinSize, bool dragWholeBody = true, float nextResizeHandleSize = 24f)
        {
            minSize = nextMinSize;
            wholeBodyDraggable = dragWholeBody;
            resizeHandleSize = Mathf.Max(8f, nextResizeHandleSize);
        }

        private void Awake()
        {
            rect = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return;
            }

            Rect localRect = rect.rect;
            bool inResizeHandle = localPoint.x >= localRect.xMax - resizeHandleSize && localPoint.y <= localRect.yMin + resizeHandleSize;
            bool inDragHeader = wholeBodyDraggable || localPoint.y >= localRect.yMax - dragHeaderHeight;
            resizing = inResizeHandle;
            dragging = !resizing && inDragHeader;

            float scale = CanvasScale();
            if (resizing)
            {
                resizeStartPointer = eventData.position;
                resizeStartSize = rect.sizeDelta;
            }
            else if (dragging)
            {
                RectTransform parentRect = rect.parent as RectTransform;
                Vector2 parentPoint;
                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out parentPoint))
                {
                    pointerOffset = rect.anchoredPosition - parentPoint;
                }
                else
                {
                    pointerOffset = rect.anchoredPosition - eventData.position / Mathf.Max(0.01f, scale);
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rect == null)
            {
                return;
            }

            float scale = CanvasScale();
            if (resizing)
            {
                Vector2 delta = (eventData.position - resizeStartPointer) / Mathf.Max(0.01f, scale);
                Vector2 next = new Vector2(
                    Mathf.Max(minSize.x, resizeStartSize.x + delta.x),
                    Mathf.Max(minSize.y, resizeStartSize.y - delta.y));
                rect.sizeDelta = next;
                return;
            }

            if (dragging)
            {
                RectTransform parentRect = rect.parent as RectTransform;
                Vector2 parentPoint;
                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out parentPoint))
                {
                    rect.anchoredPosition = parentPoint + pointerOffset;
                }
                else
                {
                    rect.anchoredPosition = eventData.position / Mathf.Max(0.01f, scale) + pointerOffset;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            resizing = false;
        }

        private float CanvasScale()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            return canvas != null ? canvas.scaleFactor : 1f;
        }
    }
}
