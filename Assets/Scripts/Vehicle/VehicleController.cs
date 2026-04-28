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
        public bool AutomaticTransmission { get; set; }

        private Rigidbody body;
        private WheelCollider[] driveWheels;
        private WheelCollider[] allWheels;
        private Vector3 baseCenterOfMass;
        private bool hasBaseCenterOfMass;
        private Vector3 collisionRecoveryNormal;
        private float collisionRecoveryTimer;
        private VehicleInputState externalInput;
        private float shapedSteer;

        public void SetInput(VehicleInputState input)
        {
            readUnityInput = false;
            externalInput = input;
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
                externalInput = VehicleInputState.FromUnityInput();
            }

            float dt = Time.fixedDeltaTime;
            CurrentInput = ShapeInput(externalInput, dt);
            if (AutomaticTransmission)
            {
                CurrentInput = ApplyAutomaticTransmissionInput(CurrentInput, dt);
            }

            if (AutomaticTransmission)
            {
                VehicleInputState gearboxInput = CurrentInput;
                gearboxInput.shiftUp = false;
                gearboxInput.shiftDown = false;
                gearboxInput.shiftDownHeld = false;
                gearbox.Tick(tuning, gearboxInput, engine.Rpm, dt);
                ApplyAutomaticShiftRequests(CurrentInput);
            }
            else
            {
                gearbox.Tick(tuning, CurrentInput, engine.Rpm, dt);
            }

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
            shapedSteer = 0f;
        }

        private VehicleInputState ShapeInput(VehicleInputState input, float deltaTime)
        {
            float targetSteer = Mathf.Clamp(input.steer, -1f, 1f);
            bool returningToCenter = Mathf.Abs(targetSteer) < Mathf.Abs(shapedSteer) || Mathf.Sign(targetSteer) != Mathf.Sign(shapedSteer);
            float rate = returningToCenter ? tuning.steeringInputFallRate : tuning.steeringInputRiseRate;
            shapedSteer = Mathf.MoveTowards(shapedSteer, targetSteer, Mathf.Max(0.1f, rate) * deltaTime);
            input.steer = Mathf.Clamp(shapedSteer, -1f, 1f);
            return input;
        }

        private void ApplyAutomaticShiftRequests(VehicleInputState input)
        {
            if (input.shiftDownHeld && gearbox.CurrentGear != GearboxController.ReverseGear)
            {
                gearbox.RequestShift(tuning, GearboxController.ReverseGear, engine.Rpm, false);
            }
            else if (input.shiftUp)
            {
                gearbox.RequestShift(tuning, gearbox.CurrentGear + 1, engine.Rpm, false);
            }
            else if (input.shiftDown)
            {
                gearbox.RequestShift(tuning, gearbox.CurrentGear - 1, engine.Rpm, false);
            }
        }

        private VehicleInputState ApplyAutomaticTransmissionInput(VehicleInputState input, float deltaTime)
        {
            input.shiftUp = false;
            input.shiftDown = false;
            input.shiftDownHeld = false;
            input.clutch = 0f;

            float forwardSpeed = body != null ? Vector3.Dot(body.velocity, transform.forward) : 0f;
            bool reverseHeld = input.brake > 0.08f && input.throttle < 0.08f;
            bool forwardHeld = input.throttle > 0.08f;

            if (reverseHeld && forwardSpeed <= 0.45f)
            {
                float reversePower = Mathf.Clamp01(input.brake);
                if (gearbox.CurrentGear != GearboxController.ReverseGear)
                {
                    gearbox.ForceShiftImmediate(tuning, GearboxController.ReverseGear);
                }

                input.shiftDownHeld = false;
                input.shiftDown = false;
                input.throttle = reversePower;
                input.brake = 0f;
                return input;
            }

            if (gearbox.CurrentGear == GearboxController.ReverseGear && forwardHeld)
            {
                gearbox.ForceShiftImmediate(tuning, GearboxController.MinForwardGear);
            }

            if (gearbox.CurrentGear <= GearboxController.NeutralGear && forwardHeld)
            {
                input.shiftUp = true;
                return input;
            }

            if (gearbox.CurrentGear < GearboxController.MinForwardGear || gearbox.IsShifting)
            {
                return input;
            }

            int maxForwardGear = tuning.forwardRatios != null && tuning.forwardRatios.Length > 0 ? Mathf.Min(GearboxController.MaxForwardGear, tuning.forwardRatios.Length) : GearboxController.MaxForwardGear;
            int speedTargetGear = CalculateAutomaticTargetGear(maxForwardGear);
            if (speedTargetGear > gearbox.CurrentGear)
            {
                input.shiftUp = true;
                return input;
            }

            if (speedTargetGear < gearbox.CurrentGear)
            {
                input.shiftDown = true;
                return input;
            }

            float throttle01 = Mathf.Clamp01(input.throttle);
            float upshiftRpm = Mathf.Lerp(tuning.perfectShiftRpmMin * 0.9f, tuning.perfectShiftRpmMax * 1.01f, throttle01);
            float downshiftRpm = Mathf.Lerp(tuning.idleRpm + 950f, tuning.perfectShiftRpmMin * 0.64f, throttle01);

            if (engine.Rpm >= upshiftRpm && gearbox.CurrentGear < maxForwardGear)
            {
                input.shiftUp = true;
            }
            else if (engine.Rpm <= downshiftRpm && gearbox.CurrentGear > GearboxController.MinForwardGear && SpeedMetersPerSecond > 5.5f)
            {
                input.shiftDown = true;
            }

            return input;
        }

        private int CalculateAutomaticTargetGear(int maxForwardGear)
        {
            float speed = SpeedMetersPerSecond;
            if (speed < 11f)
            {
                return GearboxController.MinForwardGear;
            }

            if (speed < 19f)
            {
                return Mathf.Min(2, maxForwardGear);
            }

            if (speed < 28f)
            {
                return Mathf.Min(3, maxForwardGear);
            }

            if (speed < 38f)
            {
                return Mathf.Min(4, maxForwardGear);
            }

            if (speed < 50f)
            {
                return Mathf.Min(5, maxForwardGear);
            }

            return maxForwardGear;
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
            float lowSpeedAssist = Mathf.Lerp(1f + tuning.lowSpeedSteeringAssist, 1f, speed01);
            float highSpeedStability = Mathf.Lerp(1f, 1f - Mathf.Clamp01(tuning.highSpeedSteeringStability), speed01);
            float steerAngle = Mathf.Lerp(tuning.steeringAngle, tuning.steeringAngleAtSpeed, speed01) * lowSpeedAssist * highSpeedStability * CurrentInput.steer;
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
            float entryGrip = Mathf.Min(tuning.handbrakeRearGrip, tuning.driftSideGrip);
            float driftGrip = Mathf.Lerp(tuning.normalSideGrip, tuning.driftSideGrip, drift.DriftAmount);
            float rearGrip = CurrentInput.handbrake ? Mathf.Lerp(driftGrip, entryGrip, drift.EntryAmount) : driftGrip;
            rearGrip = Mathf.Lerp(rearGrip, tuning.normalSideGrip * 0.9f, drift.CounterSteerAmount * drift.DriftAmount * 0.45f);
            float frontGrip = Mathf.Lerp(tuning.normalSideGrip, tuning.normalSideGrip * 1.18f, drift.CounterSteerAmount * drift.DriftAmount);
            SetSideGrip(frontLeft, frontGrip);
            SetSideGrip(frontRight, frontGrip);
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
            float lowSpeedYawAssist = Mathf.InverseLerp(18f, 3f, speed) * 0.35f;
            float handbrakeYawAssist = CurrentInput.handbrake ? Mathf.InverseLerp(tuning.driftMinSpeed, tuning.driftMinSpeed + 10f, speed) : 0f;
            float yawAssist = Mathf.Max(lowSpeedYawAssist, handbrakeYawAssist);
            if (yawAssist > 0.001f)
            {
                float speedStability = Mathf.Lerp(1f, 1f - Mathf.Clamp01(tuning.highSpeedSteeringStability), Mathf.InverseLerp(18f, 58f, speed));
                body.AddRelativeTorque(Vector3.up * CurrentInput.steer * tuning.arcadeYawAssist * speedStability * yawAssist, ForceMode.Acceleration);
            }

            ApplyLaunchAndCollisionRecovery(deltaTime);
            drift.ApplyArcadeAssist(tuning, body, CurrentInput, deltaTime);
            ApplyDriftExitBoost();
        }

        private void ApplyDriftExitBoost()
        {
            if (tuning == null || body == null || !drift.SuccessfulExitThisFrame)
            {
                return;
            }

            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f)
            {
                return;
            }

            float speed01 = Mathf.Clamp01(SpeedMetersPerSecond / 34f);
            float boostForce = tuning.driftExitBoostForce * Mathf.Lerp(0.72f, 1.12f, speed01);
            body.AddForce(flatForward.normalized * boostForce, ForceMode.VelocityChange);
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

            if (AutomaticTransmission && gearbox.LastShiftAge < 0.35f && gearbox.LastShiftQuality == ShiftQuality.None)
            {
                return "AUTO SHIFT";
            }

            if (gearbox.ReverseHoldProgress > 0.15f && gearbox.CurrentGear != GearboxController.ReverseGear)
            {
                return AutomaticTransmission ? "HOLD BRAKE FOR REVERSE" : "HOLD Q FOR REVERSE";
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
                if (drift.TurnInAmount > 0.55f)
                {
                    return "TIGHT DRIFT";
                }

                return drift.ClutchKickActive ? "CLUTCH KICK" : "DRIFT";
            }

            if (drift.ExitBoostTimer > 0f)
            {
                return "DRIFT EXIT BOOST";
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
