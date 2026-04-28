using UnityEngine;

namespace GTX.Visuals
{
    [RequireComponent(typeof(Camera))]
    public sealed class GTXPixelFilter : MonoBehaviour
    {
        [SerializeField] private int resolutionIndex = 1;
        [SerializeField] private KeyCode toggleKey = KeyCode.F4;
        [SerializeField] private int[] verticalResolutionOptions = { 0, 360 };
        [SerializeField] private bool showOptionLabel = true;

        public bool PixelFilterEnabled
        {
            get { return CurrentResolution > 0; }
            set
            {
                if (value && CurrentResolution <= 0)
                {
                    resolutionIndex = 1;
                }
                else if (!value)
                {
                    resolutionIndex = 0;
                }
            }
        }

        public int VerticalResolution
        {
            get { return CurrentResolution; }
            set
            {
                if (value <= 0)
                {
                    resolutionIndex = 0;
                    return;
                }

                EnsureOptions();
                int bestIndex = 1;
                int bestDistance = int.MaxValue;
                for (int i = 1; i < verticalResolutionOptions.Length; i++)
                {
                    int distance = Mathf.Abs(verticalResolutionOptions[i] - value);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                resolutionIndex = bestIndex;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                EnsureOptions();
                resolutionIndex = (resolutionIndex + 1) % verticalResolutionOptions.Length;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            int targetResolution = CurrentResolution;
            if (targetResolution <= 0 || source == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int height = Mathf.Clamp(targetResolution, 144, Mathf.Max(144, source.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (source.width / (float)source.height)));
            RenderTexture lowResolution = RenderTexture.GetTemporary(width, height, 0, source.format);
            lowResolution.filterMode = FilterMode.Point;
            source.filterMode = FilterMode.Point;

            Graphics.Blit(source, lowResolution);
            Graphics.Blit(lowResolution, destination);
            RenderTexture.ReleaseTemporary(lowResolution);
        }

        private void OnGUI()
        {
            if (!showOptionLabel)
            {
                return;
            }

            EnsureOptions();
            int clampedIndex = Mathf.Clamp(resolutionIndex, 0, verticalResolutionOptions.Length - 1);
            int targetResolution = verticalResolutionOptions[clampedIndex];
            string label = targetResolution > 0 ? "PIXEL " + clampedIndex + ": " + targetResolution + "p" : "PIXEL 0: OFF";
            Rect rect = new Rect(Screen.width - 176f, 18f, 154f, 28f);
            GUI.Box(rect, label);
        }

        private int CurrentResolution
        {
            get
            {
                EnsureOptions();
                return verticalResolutionOptions[Mathf.Clamp(resolutionIndex, 0, verticalResolutionOptions.Length - 1)];
            }
        }

        private void EnsureOptions()
        {
            if (verticalResolutionOptions == null || verticalResolutionOptions.Length == 0)
            {
                verticalResolutionOptions = new[] { 0, 360 };
                resolutionIndex = 1;
            }
        }
    }
}
