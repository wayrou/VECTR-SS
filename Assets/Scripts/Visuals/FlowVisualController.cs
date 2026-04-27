using GTX.Flow;
using GTX.Vehicle;
using UnityEngine;

namespace GTX.Visuals
{
    public sealed class FlowVisualController : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;
        [SerializeField] private Transform boostTrail;
        [SerializeField] private float speedLineInterval = 0.16f;

        private Transform[] outlineTransforms = new Transform[0];
        private Vector3[] outlineBaseScales = new Vector3[0];
        private Renderer boostTrailRenderer;
        private float nextSpeedLineTime;

        public void Configure(VehicleController newVehicle, FlowState newFlowState, RuntimeImpactEffects newEffects, Transform newBoostTrail)
        {
            vehicle = newVehicle;
            flowState = newFlowState;
            effects = newEffects;
            boostTrail = newBoostTrail;
            if (boostTrail != null)
            {
                boostTrailRenderer = boostTrail.GetComponent<Renderer>();
            }

            CaptureOutlines();
        }

        private void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }

            if (flowState == null)
            {
                flowState = GetComponent<FlowState>();
            }

            if (effects == null)
            {
                effects = GetComponent<RuntimeImpactEffects>();
            }
        }

        private void Start()
        {
            CaptureOutlines();
        }

        private void Update()
        {
            float flow01 = flowState != null ? flowState.Normalized : 0f;
            bool boosting = vehicle != null && vehicle.IsBoosting;
            float boost01 = boosting ? 1f : 0f;
            float speed01 = vehicle != null ? Mathf.InverseLerp(17f, 44f, vehicle.SpeedMetersPerSecond) : 0f;
            float pulse = 1f + Mathf.Sin(Time.time * Mathf.Lerp(5f, 13f, flow01)) * 0.012f * flow01;
            float outlineScale = 1f + (0.018f + flow01 * 0.034f + boost01 * 0.012f) * pulse;

            for (int i = 0; i < outlineTransforms.Length; i++)
            {
                if (outlineTransforms[i] != null)
                {
                    outlineTransforms[i].localScale = outlineBaseScales[i] * outlineScale;
                }
            }

            UpdateBoostTrail(flow01, boosting);
            if ((boosting || flow01 > 0.58f || speed01 > 0.28f) && effects != null && Time.time >= nextSpeedLineTime)
            {
                float intensity = Mathf.Clamp01(Mathf.Max(flow01, speed01) + boost01 * 0.25f);
                nextSpeedLineTime = Time.time + Mathf.Lerp(speedLineInterval, 0.055f, intensity);
                effects.PlaySpeedLines(transform, intensity);
            }
        }

        private void CaptureOutlines()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].name.Contains("Outline"))
                {
                    count++;
                }
            }

            outlineTransforms = new Transform[count];
            outlineBaseScales = new Vector3[count];
            int index = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].name.Contains("Outline"))
                {
                    continue;
                }

                outlineTransforms[index] = renderers[i].transform;
                outlineBaseScales[index] = renderers[i].transform.localScale;
                index++;
            }
        }

        private void UpdateBoostTrail(float flow01, bool boosting)
        {
            if (boostTrail == null)
            {
                return;
            }

            float target = boosting ? 1f : Mathf.Clamp01(flow01 - 0.62f);
            boostTrail.gameObject.SetActive(target > 0.02f);
            float length = Mathf.Lerp(1.1f, 3.8f, Mathf.Clamp01(target + flow01 * 0.35f));
            float width = Mathf.Lerp(0.38f, 0.82f, flow01);
            boostTrail.localPosition = new Vector3(0f, 0.46f, -2.18f - length * 0.5f);
            boostTrail.localScale = new Vector3(width, length, width);

            if (boostTrailRenderer != null)
            {
                Color color = Color.Lerp(new Color(0.1f, 0.72f, 1f, 0.85f), new Color(1f, 0.95f, 0.2f, 0.95f), flow01);
                boostTrailRenderer.material.color = color;
            }
        }
    }
}
