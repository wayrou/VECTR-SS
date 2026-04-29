using UnityEngine;

namespace GTX.Data
{
    [CreateAssetMenu(menuName = "GTX/Vehicle/Vehicle Tuning", fileName = "VehicleTuning")]
    public sealed class VehicleTuning : ScriptableObject
    {
        [Header("Mass And Balance")]
        public float mass = 1350f;
        public Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0.1f);
        public float downforce = 48f;

        [Header("Engine")]
        public float idleRpm = 900f;
        public float redlineRpm = 7600f;
        public float stallRpm = 450f;
        public float peakTorque = 1760f;
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
        public float reverseShiftHoldSeconds = 1.25f;
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
        public float steeringAngle = 36f;
        public float steeringAngleAtSpeed = 10f;
        public float steeringSpeedReference = 50f;
        public float steeringInputRiseRate = 5.4f;
        public float steeringInputFallRate = 11.5f;
        public float lowSpeedSteeringAssist = 0.08f;
        public float highSpeedSteeringStability = 0.32f;
        public float brakeTorque = 4200f;
        public float handbrakeTorque = 5200f;
        public float maxDriveTorque = 6500f;
        public float rigidbodyFallbackDriveForce = 33600f;
        public float rigidbodyFallbackBrakeDrag = 6f;
        public float arcadeYawAssist = 0.22f;
        public float tractionControl = 0.48f;
        public float stuckLaunchAssistSpeed = 3.2f;
        public float stuckLaunchAssistForce = 7.5f;
        public float collisionRecoveryDuration = 0.85f;
        public float collisionRecoveryDriveForce = 10.5f;
        public float collisionRecoveryNudgeForce = 4.8f;
        public bool bikeHandling;

        [Header("Drift")]
        public float driftSlipThreshold = 0.32f;
        public float driftMinSpeed = 7.5f;
        public float driftYawAssist = 1.55f;
        public float driftEntryYawKick = 2.25f;
        public float driftCounterSteerAssist = 0.92f;
        public float driftCounterSteerExitRate = 2.35f;
        public float driftTightenYawAssist = 1.3f;
        public float driftForwardAssist = 3.2f;
        public float driftSidewaysThrow = 0.58f;
        public float driftSustain = 0.86f;
        public float driftExitRecovery = 5.4f;
        public float driftExitYawDamping = 7.2f;
        public float driftExitHoldSeconds = 0.42f;
        public float driftExitBoostForce = 4.8f;
        public float driftExitBoostDuration = 0.36f;
        public float driftExitBoostMinDriftSeconds = 0.42f;
        public float driftLateralDamping = 0.88f;
        public float driftHandbrakeEntryKick = 0.62f;
        public float driftHandbrakeEntryDuration = 0.2f;
        public float driftThrottleInfluence = 0.62f;
        public float driftSideGrip = 0.72f;
        public float normalSideGrip = 1.24f;
        public float handbrakeRearGrip = 0.42f;
        public float clutchKickDuration = 0.64f;

        [Header("Boost")]
        public float boostCapacity = 100f;
        public float boostBurnPerSecond = 24f;
        public float boostRegenPerSecond = 11f;
        public float boostTorqueMultiplier = 2.05f;
        public float boostHeatPerSecond = 34f;
        public float boostCoolPerSecond = 18f;
        public float overheatThreshold = 100f;
        public float overheatedLockoutHeat = 42f;
    }
}
