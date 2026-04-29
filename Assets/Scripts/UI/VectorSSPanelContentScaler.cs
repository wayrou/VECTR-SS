using UnityEngine;

namespace GTX.UI
{
    [DisallowMultipleComponent]
    public sealed class VectorSSPanelContentScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 designSize = new Vector2(332f, 136f);

        private RectTransform rect;
        private RectTransform parentRect;
        private Vector2 lastParentSize;

        public void Configure(Vector2 nextDesignSize)
        {
            designSize = new Vector2(Mathf.Max(1f, nextDesignSize.x), Mathf.Max(1f, nextDesignSize.y));
            ApplyScale(true);
        }

        private void Awake()
        {
            CacheRects();
            ApplyScale(true);
        }

        private void OnEnable()
        {
            ApplyScale(true);
        }

        private void LateUpdate()
        {
            ApplyScale(false);
        }

        private void CacheRects()
        {
            if (rect == null)
            {
                rect = transform as RectTransform;
            }

            if (parentRect == null)
            {
                parentRect = transform.parent as RectTransform;
            }
        }

        private void ApplyScale(bool force)
        {
            CacheRects();
            if (rect == null || parentRect == null)
            {
                return;
            }

            Vector2 parentSize = parentRect.rect.size;
            if (!force && (parentSize - lastParentSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            lastParentSize = parentSize;
            float scaleX = Mathf.Max(0.01f, parentSize.x) / Mathf.Max(1f, designSize.x);
            float scaleY = Mathf.Max(0.01f, parentSize.y) / Mathf.Max(1f, designSize.y);
            rect.localScale = new Vector3(scaleX, scaleY, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = designSize;
        }
    }
}
