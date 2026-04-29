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
        [SerializeField] private Color boostTrailBaseColor = new Color(0.13f, 0.36f, 0.48f, 0.78f);
        [SerializeField] private Color boostTrailHotColor = new Color(0.54f, 0.16f, 0.27f, 0.86f);
        [SerializeField] private bool narrowBoostTrail;
        [SerializeField] private float driftBodyRollDegrees = 4.2f;
        [SerializeField] private float driftInsideLift = 0.032f;
        [SerializeField] private float driftOutsideDrop = 0.01f;
        [SerializeField] private float boostNoseLiftDegrees = 8.4f;
        [SerializeField] private float boostFrontLift = 0.13f;
        [SerializeField] private float boostRearSquat = 0.044f;

        private Transform[] outlineTransforms = new Transform[0];
        private Vector3[] outlineBaseScales = new Vector3[0];
        private Transform[] bodyRollTransforms = new Transform[0];
        private Vector3[] bodyRollBasePositions = new Vector3[0];
        private Quaternion[] bodyRollBaseRotations = new Quaternion[0];
        private Renderer boostTrailRenderer;
        private float nextSpeedLineTime;
        private float visualDriftRoll;
        private float visualBoostLift;
        private float driftRollSign = 1f;
        private float previousSpeed;
        private bool hasPreviousSpeed;
        private bool driftLiftLatched;

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
            hasPreviousSpeed = false;
            driftLiftLatched = false;
        }

        public void SetBoostTrailStyle(Color baseColor, Color hotColor, bool narrow)
        {
            boostTrailBaseColor = baseColor;
            boostTrailHotColor = hotColor;
            narrowBoostTrail = narrow;
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
            float speed = vehicle != null ? vehicle.SpeedMetersPerSecond : 0f;
            float speed01 = Mathf.InverseLerp(17f, 44f, speed);
            float pulse = 1f + Mathf.Sin(Time.time * Mathf.Lerp(5f, 13f, flow01)) * 0.012f * flow01;
            float outlineScale = 1f + (0.018f + flow01 * 0.034f + boost01 * 0.012f) * pulse;

            for (int i = 0; i < outlineTransforms.Length; i++)
            {
                if (outlineTransforms[i] != null)
                {
                    outlineTransforms[i].localScale = outlineBaseScales[i] * outlineScale;
                }
            }

            UpdateBodyMotion(speed01, speed);
            UpdateBoostTrail(flow01);
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

            CaptureBodyRollVisuals(renderers);
        }

        private void CaptureBodyRollVisuals(Renderer[] renderers)
        {
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsBodyRollRenderer(renderers[i]))
                {
                    count++;
                }
            }

            bodyRollTransforms = new Transform[count];
            bodyRollBasePositions = new Vector3[count];
            bodyRollBaseRotations = new Quaternion[count];
            int index = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!IsBodyRollRenderer(renderers[i]))
                {
                    continue;
                }

                Transform visual = renderers[i].transform;
                bodyRollTransforms[index] = visual;
                bodyRollBasePositions[index] = visual.localPosition;
                bodyRollBaseRotations[index] = visual.localRotation;
                index++;
            }
        }

        private static bool IsBodyRollRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            string lowerName = renderer.name.ToLowerInvariant();
            return !lowerName.Contains("wheel") &&
                !lowerName.Contains("trail") &&
                !lowerName.Contains("smoke") &&
                !lowerName.Contains("spark") &&
                !lowerName.Contains("shadow");
        }

        private void UpdateBodyMotion(float speed01, float speed)
        {
            float drift01 = vehicle != null && vehicle.Drift != null ? vehicle.Drift.DriftAmount : 0f;
            float steer = vehicle != null ? vehicle.CurrentInput.steer : 0f;
            float slip01 = vehicle != null && vehicle.Drift != null ? Mathf.InverseLerp(10f, 26f, Mathf.Abs(vehicle.Drift.SlipAngle)) : 0f;
            float counterSteerLiftReduction = vehicle != null && vehicle.Drift != null ? vehicle.Drift.CounterSteerAmount : 0f;
            bool tightDrift = drift01 > 0.34f && slip01 > 0.2f && counterSteerLiftReduction < 0.32f;
            if (tightDrift)
            {
                if (!driftLiftLatched)
                {
                    if (Mathf.Abs(steer) > 0.08f)
                    {
                        driftRollSign = Mathf.Sign(steer);
                    }
                    else if (vehicle != null && vehicle.Drift != null && Mathf.Abs(vehicle.Drift.SlipAngle) > 1.2f)
                    {
                        driftRollSign = Mathf.Sign(vehicle.Drift.SlipAngle);
                    }

                    driftLiftLatched = true;
                }
            }
            else
            {
                driftLiftLatched = false;
                if (drift01 < 0.12f && Mathf.Abs(steer) > 0.08f)
                {
                    driftRollSign = Mathf.Sign(steer);
                }
            }

            float turnInLift = vehicle != null && vehicle.Drift != null ? vehicle.Drift.TurnInAmount : 0f;
            float rollStrength = tightDrift
                ? Mathf.Clamp01(Mathf.Pow(drift01, 1.35f) * slip01 * Mathf.Lerp(0.35f, 0.82f, speed01) * Mathf.Lerp(0.4f, 1f, turnInLift))
                : 0f;
            float targetRoll = driftRollSign * driftBodyRollDegrees * rollStrength;
            float rollResponse = targetRoll == 0f ? 18f : 9f;
            visualDriftRoll = Mathf.Lerp(visualDriftRoll, targetRoll, 1f - Mathf.Exp(-rollResponse * Time.deltaTime));
            float acceleration = 0f;
            if (hasPreviousSpeed && Time.deltaTime > 0.0001f)
            {
                acceleration = (speed - previousSpeed) / Time.deltaTime;
            }

            previousSpeed = speed;
            hasPreviousSpeed = true;
            float targetBoostLift = Mathf.InverseLerp(2.8f, 9.5f, acceleration);
            visualBoostLift = Mathf.Lerp(visualBoostLift, targetBoostLift, 1f - Mathf.Exp(-8f * Time.deltaTime));
            float nosePitch = -boostNoseLiftDegrees * visualBoostLift;

            for (int i = 0; i < bodyRollTransforms.Length; i++)
            {
                Transform visual = bodyRollTransforms[i];
                if (visual == null)
                {
                    continue;
                }

                Vector3 basePosition = bodyRollBasePositions[i];
                float localSide = Mathf.Clamp(basePosition.x * driftRollSign, -1f, 1f);
                float localForward = Mathf.Clamp(basePosition.z, -1f, 1f);
                float lift = Mathf.Max(0f, localSide) * driftInsideLift * rollStrength;
                float drop = Mathf.Max(0f, -localSide) * driftOutsideDrop * rollStrength;
                float boostLift = Mathf.Max(0f, localForward) * boostFrontLift * visualBoostLift;
                float boostSquat = Mathf.Max(0f, -localForward) * boostRearSquat * visualBoostLift;
                visual.localPosition = basePosition + Vector3.up * (lift - drop + boostLift - boostSquat);
                visual.localRotation = bodyRollBaseRotations[i] * Quaternion.Euler(nosePitch, 0f, visualDriftRoll);
            }
        }

        private void UpdateBoostTrail(float flow01)
        {
            if (boostTrail == null)
            {
                return;
            }

            float target = Mathf.Clamp01(flow01 - 0.62f);
            boostTrail.gameObject.SetActive(target > 0.02f);
            float length = Mathf.Lerp(1.1f, narrowBoostTrail ? 4.35f : 3.8f, Mathf.Clamp01(target + flow01 * 0.35f));
            float width = narrowBoostTrail ? Mathf.Lerp(0.18f, 0.42f, flow01) : Mathf.Lerp(0.38f, 0.82f, flow01);
            boostTrail.localPosition = new Vector3(0f, 0.46f, -2.18f - length * 0.5f);
            boostTrail.localScale = new Vector3(width, length, width);

            if (boostTrailRenderer != null)
            {
                Color color = Color.Lerp(boostTrailBaseColor, boostTrailHotColor, flow01);
                boostTrailRenderer.material.color = color;
            }
        }
    }
}
