using UnityEngine;

namespace GTX.Data
{
    [CreateAssetMenu(menuName = "GTX/Vehicle/Vehicle Tuning", fileName = "VehicleTuning")]
    public sealed class VehicleTuning : ScriptableObject
    {
        [Header("Mass And Balance")]
        public float mass = 1350f;
        public Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0.1f);
        public float downforce = 38f;

        [Header("Engine")]
        public float idleRpm = 900f;
        public float redlineRpm = 7600f;
        public float stallRpm = 450f;
        public float peakTorque = 540f;
        public AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.35f, 0.82f),
            new Keyframe(0.62f, 1f),
            new Keyframe(0.86f, 0.92f),
            new Keyframe(1f, 0.62f));
        public float engineInertia = 0.22f;
        public float engineBrakeTorque = 90f;
        public float rpmResponse = 12f;

        [Header("Gearbox")]
        public float finalDrive = 3.42f;
        public float reverseRatio = -3.1f;
        public float[] forwardRatios = { 3.1f, 2.2f, 1.62f, 1.22f, 0.96f, 0.78f };
        public float shiftDuration = 0.18f;
        public float shiftCooldown = 0.08f;
        public float perfectShiftRpmMin = 6800f;
        public float perfectShiftRpmMax = 7350f;
        public float perfectShiftTorqueMultiplier = 1.16f;
        public float badShiftTorqueMultiplier = 0.62f;
        public float shiftJudgementWindow = 0.45f;

        [Header("Clutch")]
        [Range(0f, 1f)] public float clutchBitePoint = 0.35f;
        public float clutchTransferSharpness = 2.35f;
        public float clutchKickSlipBoost = 0.32f;

        [Header("Handling")]
        public float steeringAngle = 34f;
        public float steeringAngleAtSpeed = 9f;
        public float steeringSpeedReference = 42f;
        public float brakeTorque = 3300f;
        public float handbrakeTorque = 5200f;
        public float maxDriveTorque = 1650f;
        public float rigidbodyFallbackDriveForce = 9500f;
        public float rigidbodyFallbackBrakeDrag = 6f;
        public float arcadeYawAssist = 0.9f;
        public float tractionControl = 0.28f;

        [Header("Drift")]
        public float driftSlipThreshold = 0.36f;
        public float driftMinSpeed = 8f;
        public float driftYawAssist = 1.35f;
        public float driftSideGrip = 0.62f;
        public float normalSideGrip = 1f;
        public float handbrakeRearGrip = 0.42f;
        public float clutchKickDuration = 0.45f;

        [Header("Boost")]
        public float boostCapacity = 100f;
        public float boostBurnPerSecond = 28f;
        public float boostRegenPerSecond = 11f;
        public float boostTorqueMultiplier = 1.35f;
        public float boostHeatPerSecond = 34f;
        public float boostCoolPerSecond = 18f;
        public float overheatThreshold = 100f;
        public float overheatedLockoutHeat = 42f;
    }
}
