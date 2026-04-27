using GTX.Data;
using UnityEngine;

namespace GTX.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [Header("Tuning")]
        [SerializeField] private VehicleTuning tuning;
        [SerializeField] private bool readUnityInput = true;

        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeft;
        [SerializeField] private WheelCollider frontRight;
        [SerializeField] private WheelCollider rearLeft;
        [SerializeField] private WheelCollider rearRight;

        [Header("Runtime Models")]
        [SerializeField] private EngineModel engine = new EngineModel();
        [SerializeField] private GearboxController gearbox = new GearboxController();
        [SerializeField] private BoostSystem boost = new BoostSystem();
        [SerializeField] private DriftSystem drift = new DriftSystem();

        public VehicleTuning Tuning => tuning;
        public EngineModel Engine => engine;
        public GearboxController Gearbox => gearbox;
        public BoostSystem Boost => boost;
        public DriftSystem Drift => drift;
        public VehicleInputState CurrentInput { get; private set; }
        public float SpeedMetersPerSecond => body != null ? body.velocity.magnitude : 0f;
        public float SpeedKph => SpeedMetersPerSecond * 3.6f;
        public int CurrentGear => gearbox.CurrentGear;
        public float RPM => engine.Rpm;
        public float RPM01 => engine.NormalizedRpm;
        public float Boost01 => boost.Resource01;
        public float Heat01 => boost.Heat01;
        public bool IsBoosting => boost.IsActive;
        public bool IsDrifting => drift.IsDrifting;
        public string Feedback => BuildFeedback();
        public float ClutchTransfer { get; private set; }
        public float RuntimeBrakeBias { get; set; } = 0.5f;

        private Rigidbody body;
        private WheelCollider[] driveWheels;
        private WheelCollider[] allWheels;
        private Vector3 baseCenterOfMass;
        private bool hasBaseCenterOfMass;
        private Vector3 collisionRecoveryNormal;
        private float collisionRecoveryTimer;

        public void SetInput(VehicleInputState input)
        {
            readUnityInput = false;
            CurrentInput = input;
        }

        public void Configure(VehicleTuning newTuning, WheelCollider newFrontLeft, WheelCollider newFrontRight, WheelCollider newRearLeft, WheelCollider newRearRight)
        {
            tuning = newTuning;
            frontLeft = newFrontLeft;
            frontRight = newFrontRight;
            rearLeft = newRearLeft;
            rearRight = newRearRight;
            body = GetComponent<Rigidbody>();
            CacheWheels();
            ApplyTuningToBody();
            ResetRuntimeModels();
        }

        public void ApplyTuning(VehicleTuning newTuning, bool resetRuntimeModels)
        {
            tuning = newTuning;
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ApplyTuningToBody();
            if (resetRuntimeModels)
            {
                ResetRuntimeModels();
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            CacheWheels();
            ApplyTuningToBody();
            ResetRuntimeModels();
        }

        private void OnValidate()
        {
            CacheWheels();
        }

        private void FixedUpdate()
        {
            if (tuning == null)
            {
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (readUnityInput)
            {
                CurrentInput = VehicleInputState.FromUnityInput();
            }

            float dt = Time.fixedDeltaTime;
            gearbox.Tick(tuning, CurrentInput, engine.Rpm, dt);
            boost.Tick(tuning, CurrentInput.boost, CurrentInput.throttle, dt);
            drift.Tick(tuning, body, CurrentInput, dt);

            ClutchTransfer = CalculateClutchTransfer(CurrentInput.clutch);
            float drivetrainRpm = CalculateDrivetrainRpm();
            engine.Tick(tuning, CurrentInput.throttle, ClutchTransfer, drivetrainRpm, dt);

            ApplyWheelControls();
            ApplyArcadeForces(dt);
        }

        private void CacheWheels()
        {
            driveWheels = new[] { rearLeft, rearRight };
            allWheels = new[] { frontLeft, frontRight, rearLeft, rearRight };
        }

        private void ApplyTuningToBody()
        {
            if (body == null || tuning == null)
            {
                return;
            }

            body.mass = tuning.mass;
            if (!hasBaseCenterOfMass)
            {
                baseCenterOfMass = body.centerOfMass;
                hasBaseCenterOfMass = true;
            }

            body.centerOfMass = baseCenterOfMass + tuning.centerOfMassOffset;
        }

        private void ResetRuntimeModels()
        {
            engine.Reset(tuning);
            gearbox.Reset(tuning);
            boost.Reset(tuning);
            drift.Reset();
        }

        private float CalculateClutchTransfer(float clutchInput)
        {
            if (gearbox.IsShifting || gearbox.CurrentGear == GearboxController.NeutralGear)
            {
                return 0f;
            }

            float pedalReleased = 1f - Mathf.Clamp01(clutchInput);
            float bite = Mathf.InverseLerp(tuning.clutchBitePoint, 1f, pedalReleased);
            return Mathf.Pow(Mathf.Clamp01(bite), tuning.clutchTransferSharpness);
        }

        private float CalculateDrivetrainRpm()
        {
            float ratio = Mathf.Abs(gearbox.CurrentRatio * tuning.finalDrive);
            if (ratio <= 0.001f || driveWheels == null || driveWheels.Length == 0)
            {
                return tuning.idleRpm;
            }

            float rpm = 0f;
            int count = 0;
            for (int i = 0; i < driveWheels.Length; i++)
            {
                if (driveWheels[i] == null)
                {
                    continue;
                }

                rpm += Mathf.Abs(driveWheels[i].rpm) * ratio;
                count++;
            }

            return count > 0 ? rpm / count : tuning.idleRpm;
        }

        private void ApplyWheelControls()
        {
            float speed01 = Mathf.InverseLerp(0f, tuning.steeringSpeedReference, SpeedMetersPerSecond);
            float steerAngle = Mathf.Lerp(tuning.steeringAngle, tuning.steeringAngleAtSpeed, speed01) * CurrentInput.steer;
            SetSteer(frontLeft, steerAngle);
            SetSteer(frontRight, steerAngle);

            float crankTorque = engine.GetCrankTorque(tuning, CurrentInput.throttle);
            float ratio = gearbox.CurrentRatio * tuning.finalDrive;
            float shiftMultiplier = gearbox.GetShiftTorqueMultiplier(tuning);
            float boostMultiplier = boost.GetTorqueMultiplier(tuning);
            float tractionMultiplier = CalculateTractionMultiplier();
            float wheelTorque = crankTorque * ratio * ClutchTransfer * shiftMultiplier * boostMultiplier * tractionMultiplier;
            wheelTorque = Mathf.Clamp(wheelTorque, -tuning.maxDriveTorque, tuning.maxDriveTorque);

            ApplyDriveTorque(wheelTorque);
            ApplyBrakes();
            ApplyWheelGrip();
        }

        private float CalculateTractionMultiplier()
        {
            if (tuning.tractionControl <= 0f || CurrentInput.handbrake || drift.IsDrifting)
            {
                return 1f;
            }

            float worstSlip = 0f;
            for (int i = 0; i < driveWheels.Length; i++)
            {
                if (driveWheels[i] != null && driveWheels[i].GetGroundHit(out WheelHit hit))
                {
                    worstSlip = Mathf.Max(worstSlip, Mathf.Abs(hit.forwardSlip));
                }
            }

            return Mathf.Lerp(1f, Mathf.Clamp01(1f - worstSlip), tuning.tractionControl);
        }

        private void ApplyDriveTorque(float wheelTorque)
        {
            int liveWheels = 0;
            for (int i = 0; i < driveWheels.Length; i++)
            {
                if (driveWheels[i] != null)
                {
                    liveWheels++;
                }
            }

            float perWheelTorque = liveWheels > 0 ? wheelTorque / liveWheels : 0f;
            if (liveWheels == 0 && body != null)
            {
                float drive01 = tuning.maxDriveTorque <= 0f ? 0f : Mathf.Clamp(wheelTorque / tuning.maxDriveTorque, -1f, 1f);
                body.AddForce(transform.forward * drive01 * tuning.rigidbodyFallbackDriveForce, ForceMode.Force);
            }

            for (int i = 0; i < driveWheels.Length; i++)
            {
                if (driveWheels[i] != null)
                {
                    driveWheels[i].motorTorque = perWheelTorque;
                }
            }
        }

        private void ApplyBrakes()
        {
            float serviceBrake = CurrentInput.brake * tuning.brakeTorque;
            float frontBrake = serviceBrake * Mathf.Lerp(0.82f, 1.24f, Mathf.Clamp01(RuntimeBrakeBias));
            float rearBrake = serviceBrake * Mathf.Lerp(1.24f, 0.82f, Mathf.Clamp01(RuntimeBrakeBias));
            bool hasWheelBrakes = false;
            if (frontLeft != null)
            {
                hasWheelBrakes = true;
                frontLeft.brakeTorque = frontBrake;
            }

            if (frontRight != null)
            {
                hasWheelBrakes = true;
                frontRight.brakeTorque = frontBrake;
            }

            if (rearLeft != null)
            {
                hasWheelBrakes = true;
                rearLeft.brakeTorque = rearBrake;
            }

            if (rearRight != null)
            {
                hasWheelBrakes = true;
                rearRight.brakeTorque = rearBrake;
            }

            float handbrake = CurrentInput.handbrake ? tuning.handbrakeTorque : 0f;
            SetBrake(rearLeft, Mathf.Max(rearLeft != null ? rearLeft.brakeTorque : 0f, handbrake));
            SetBrake(rearRight, Mathf.Max(rearRight != null ? rearRight.brakeTorque : 0f, handbrake));

            if (!hasWheelBrakes && body != null)
            {
                float brake01 = Mathf.Clamp01(CurrentInput.brake + (CurrentInput.handbrake ? 1f : 0f));
                body.AddForce(-body.velocity * tuning.rigidbodyFallbackBrakeDrag * brake01, ForceMode.Acceleration);
            }
        }

        private void ApplyWheelGrip()
        {
            float rearGrip = CurrentInput.handbrake ? tuning.handbrakeRearGrip : Mathf.Lerp(tuning.normalSideGrip, tuning.driftSideGrip, drift.DriftAmount);
            SetSideGrip(frontLeft, tuning.normalSideGrip);
            SetSideGrip(frontRight, tuning.normalSideGrip);
            SetSideGrip(rearLeft, rearGrip);
            SetSideGrip(rearRight, rearGrip);
        }

        private void ApplyArcadeForces(float deltaTime)
        {
            if (body == null)
            {
                return;
            }

            float speed = SpeedMetersPerSecond;
            body.AddForce(-transform.up * tuning.downforce * speed, ForceMode.Force);
            body.AddRelativeTorque(Vector3.up * CurrentInput.steer * tuning.arcadeYawAssist * Mathf.Clamp01(speed / 25f), ForceMode.Acceleration);
            ApplyLaunchAndCollisionRecovery(deltaTime);
            drift.ApplyArcadeAssist(tuning, body, CurrentInput, deltaTime);
        }

        private void ApplyLaunchAndCollisionRecovery(float deltaTime)
        {
            float forwardSpeed = Vector3.Dot(body.velocity, transform.forward);
            bool drivingForward = CurrentInput.throttle > 0.08f && gearbox.CurrentGear > GearboxController.NeutralGear;
            bool slowForward = forwardSpeed < tuning.stuckLaunchAssistSpeed;

            if (drivingForward && slowForward)
            {
                body.AddForce(transform.forward * CurrentInput.throttle * tuning.stuckLaunchAssistForce, ForceMode.Acceleration);
            }

            if (collisionRecoveryTimer <= 0f)
            {
                return;
            }

            collisionRecoveryTimer = Mathf.Max(0f, collisionRecoveryTimer - deltaTime);
            Vector3 normal = collisionRecoveryNormal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.001f)
            {
                return;
            }

            normal.Normalize();
            float inwardSpeed = Vector3.Dot(body.velocity, -normal);
            if (inwardSpeed > 0f)
            {
                body.velocity += normal * inwardSpeed * 0.58f;
            }

            if (drivingForward)
            {
                Vector3 wallSlideForward = Vector3.ProjectOnPlane(transform.forward, normal);
                if (wallSlideForward.sqrMagnitude > 0.001f)
                {
                    body.AddForce(wallSlideForward.normalized * CurrentInput.throttle * tuning.collisionRecoveryDriveForce, ForceMode.Acceleration);
                }

                body.AddForce(normal * tuning.collisionRecoveryNudgeForce, ForceMode.Acceleration);
            }
        }

        private static void SetSteer(WheelCollider wheel, float angle)
        {
            if (wheel != null)
            {
                wheel.steerAngle = angle;
            }
        }

        private static void SetBrake(WheelCollider wheel, float torque)
        {
            if (wheel != null)
            {
                wheel.brakeTorque = torque;
            }
        }

        private static void SetSideGrip(WheelCollider wheel, float stiffness)
        {
            if (wheel == null)
            {
                return;
            }

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = Mathf.Max(0.01f, stiffness);
            wheel.sidewaysFriction = sideways;
        }

        private string BuildFeedback()
        {
            if (tuning == null)
            {
                return "Ready";
            }

            if (boost.IsOverheated)
            {
                return "BOOST OVERHEAT";
            }

            if (gearbox.ReverseHoldProgress > 0.15f && gearbox.CurrentGear != GearboxController.ReverseGear)
            {
                return "HOLD Q FOR REVERSE";
            }

            if (gearbox.LastShiftAge < tuning.shiftJudgementWindow)
            {
                if (gearbox.LastShiftQuality == ShiftQuality.Perfect)
                {
                    return "PERFECT SHIFT";
                }

                if (gearbox.LastShiftQuality == ShiftQuality.Bad)
                {
                    return "GEAR GRIND";
                }
            }

            if (drift.IsDrifting)
            {
                return drift.ClutchKickActive ? "CLUTCH KICK" : "DRIFT";
            }

            if (boost.IsActive)
            {
                return "BOOST";
            }

            if (gearbox.CurrentGear == GearboxController.ReverseGear)
            {
                return "REVERSE";
            }

            return gearbox.CurrentGear == GearboxController.NeutralGear ? "NEUTRAL" : "READY";
        }

        private void OnCollisionEnter(Collision collision)
        {
            RegisterWallRecoveryContact(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            RegisterWallRecoveryContact(collision);
        }

        private void RegisterWallRecoveryContact(Collision collision)
        {
            if (tuning == null || collision.contactCount <= 0)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            if (Mathf.Abs(contact.normal.y) > 0.62f)
            {
                return;
            }

            collisionRecoveryNormal = contact.normal;
            collisionRecoveryTimer = Mathf.Max(collisionRecoveryTimer, tuning.collisionRecoveryDuration);
        }
    }
}
