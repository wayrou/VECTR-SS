using GTX.Data;
using UnityEngine;

namespace GTX.Vehicle
{
    [System.Serializable]
    public sealed class DriftSystem
    {
        public bool IsDrifting { get; private set; }
        public float DriftAmount { get; private set; }
        public float SlipAngle { get; private set; }
        public float EntryAmount { get; private set; }
        public float CounterSteerAmount { get; private set; }
        public float TurnInAmount { get; private set; }
        public bool SuccessfulExitThisFrame { get; private set; }
        public float ExitBoostTimer { get; private set; }
        public bool ClutchKickActive => clutchKickTimer > 0f;

        private float clutchKickTimer;
        private float entryKickTimer;
        private float entryKickDuration;
        private float entryKickDirection;
        private float exitAssistTimer;
        private float driftDirection;
        private float driftTime;
        private float exitBoostCooldown;
        private float previousDriftAmount;
        private float previousClutch;
        private bool previousHandbrake;

        public void Reset()
        {
            IsDrifting = false;
            DriftAmount = 0f;
            SlipAngle = 0f;
            EntryAmount = 0f;
            CounterSteerAmount = 0f;
            TurnInAmount = 0f;
            SuccessfulExitThisFrame = false;
            ExitBoostTimer = 0f;
            clutchKickTimer = 0f;
            entryKickTimer = 0f;
            entryKickDuration = 0f;
            entryKickDirection = 0f;
            exitAssistTimer = 0f;
            driftDirection = 0f;
            driftTime = 0f;
            exitBoostCooldown = 0f;
            previousDriftAmount = 0f;
            previousClutch = 0f;
            previousHandbrake = false;
        }

        public void Tick(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float deltaTime)
        {
            if (tuning == null || body == null)
            {
                return;
            }

            SuccessfulExitThisFrame = false;
            ExitBoostTimer = Mathf.Max(0f, ExitBoostTimer - deltaTime);
            exitBoostCooldown = Mathf.Max(0f, exitBoostCooldown - deltaTime);

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            float speed = body.velocity.magnitude;
            SlipAngle = speed > 0.2f ? Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z)) * Mathf.Rad2Deg : 0f;

            bool clutchReleasedQuickly = previousClutch > 0.75f && input.clutch < 0.2f && input.throttle > 0.55f;
            bool handbrakeTapped = input.handbrake && !previousHandbrake;
            if (clutchReleasedQuickly && input.handbrake)
            {
                clutchKickTimer = tuning.clutchKickDuration;
                PrimeEntryKick(input, localVelocity, tuning.clutchKickDuration);
            }

            if (handbrakeTapped)
            {
                PrimeEntryKick(input, localVelocity, tuning.driftHandbrakeEntryDuration);
            }

            clutchKickTimer = Mathf.Max(0f, clutchKickTimer - deltaTime);
            entryKickTimer = Mathf.Max(0f, entryKickTimer - deltaTime);
            previousClutch = input.clutch;
            previousHandbrake = input.handbrake;
            EntryAmount = entryKickDuration > 0.001f ? Mathf.Clamp01(entryKickTimer / entryKickDuration) : 0f;

            float absSlip = Mathf.Abs(SlipAngle);
            float slip01 = Mathf.InverseLerp(tuning.driftSlipThreshold * 12f, tuning.driftSlipThreshold * 44f, absSlip);
            float steeringIntent = Mathf.Clamp01(Mathf.Abs(input.steer));
            float handbrake01 = input.handbrake ? Mathf.Lerp(0.45f, 1f, steeringIntent) : 0f;
            float kick01 = input.handbrake && ClutchKickActive ? Mathf.Max(0.72f, tuning.clutchKickSlipBoost) : 0f;
            float slipSign = ResolveDriftDirection();
            CounterSteerAmount = Mathf.Clamp01(-slipSign * input.steer);
            TurnInAmount = Mathf.Clamp01(slipSign * input.steer);
            bool wantsSustain = previousDriftAmount > 0.12f && input.throttle > 0.18f && (TurnInAmount > 0.04f || steeringIntent < 0.18f || input.handbrake);
            float throttleSustain = wantsSustain ? input.throttle * tuning.driftThrottleInfluence * Mathf.Clamp01(Mathf.Max(DriftAmount, 0.35f)) : 0f;
            float steeringSustain = wantsSustain ? Mathf.Lerp(0.34f, 0.9f, Mathf.Max(TurnInAmount, 1f - CounterSteerAmount)) : 0f;
            float sustain01 = wantsSustain ? Mathf.Clamp01(Mathf.Max(slip01, throttleSustain, steeringSustain) * tuning.driftSustain) : 0f;
            float intentionalSlip01 = input.handbrake || EntryAmount > 0f ? slip01 : 0f;
            bool deliberateExit = previousDriftAmount > 0.12f && !input.handbrake && CounterSteerAmount > 0.18f;
            float target = speed >= tuning.driftMinSpeed ? Mathf.Clamp01(Mathf.Max(intentionalSlip01, handbrake01, kick01, sustain01)) : 0f;
            if (deliberateExit)
            {
                target = Mathf.Min(target, Mathf.Lerp(previousDriftAmount, 0f, CounterSteerAmount));
            }

            previousDriftAmount = DriftAmount;
            float exitRate = deliberateExit ? tuning.driftExitRecovery * Mathf.Lerp(1.35f, tuning.driftCounterSteerExitRate, CounterSteerAmount) : tuning.driftExitRecovery;
            DriftAmount = Mathf.MoveTowards(DriftAmount, target, deltaTime * (target > DriftAmount ? 8f : Mathf.Max(0.25f, exitRate)));
            bool exitingDrift = previousDriftAmount > 0.2f && DriftAmount < previousDriftAmount - 0.001f;
            if (exitingDrift || (previousDriftAmount > 0.2f && DriftAmount <= 0.2f))
            {
                exitAssistTimer = Mathf.Max(exitAssistTimer, tuning.driftExitHoldSeconds);
            }

            if (DriftAmount > 0.2f)
            {
                driftDirection = slipSign;
                driftTime += deltaTime;
            }
            else if (exitAssistTimer <= 0f)
            {
                driftTime = 0f;
            }

            if (deliberateExit &&
                previousDriftAmount > 0.56f &&
                DriftAmount <= 0.36f &&
                driftTime >= tuning.driftExitBoostMinDriftSeconds &&
                speed >= tuning.driftMinSpeed + 1.5f &&
                exitBoostCooldown <= 0f)
            {
                SuccessfulExitThisFrame = true;
                ExitBoostTimer = tuning.driftExitBoostDuration;
                exitBoostCooldown = 0.82f;
                driftTime = 0f;
            }

            exitAssistTimer = Mathf.Max(0f, exitAssistTimer - deltaTime);
            IsDrifting = DriftAmount > 0.2f;
            if (!IsDrifting && exitAssistTimer <= 0f)
            {
                CounterSteerAmount = 0f;
                TurnInAmount = 0f;
            }
        }

        public void TriggerAssistedClutchKick(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float strength)
        {
            if (tuning == null || body == null)
            {
                return;
            }

            if (!input.handbrake && !IsDrifting)
            {
                return;
            }

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            clutchKickTimer = Mathf.Max(clutchKickTimer, tuning.clutchKickDuration * Mathf.Clamp(strength, 0.75f, 1.45f));
            PrimeEntryKick(input, localVelocity, tuning.clutchKickDuration);
            DriftAmount = Mathf.Max(DriftAmount, 0.62f);
            previousDriftAmount = DriftAmount;
            exitAssistTimer = 0f;
            driftDirection = entryKickDirection;
            driftTime = 0f;
            IsDrifting = true;
        }

        private void PrimeEntryKick(VehicleInputState input, Vector3 localVelocity, float duration)
        {
            float direction = Mathf.Abs(input.steer) > 0.05f ? Mathf.Sign(input.steer) : Mathf.Sign(localVelocity.x);
            if (Mathf.Abs(direction) < 0.05f)
            {
                direction = 1f;
            }

            entryKickDirection = direction;
            driftDirection = direction;
            entryKickDuration = Mathf.Max(0.05f, duration);
            entryKickTimer = entryKickDuration;
            exitAssistTimer = 0f;
        }

        private float ResolveDriftDirection()
        {
            if (Mathf.Abs(SlipAngle) > 1.2f)
            {
                return Mathf.Sign(SlipAngle);
            }

            if (Mathf.Abs(driftDirection) > 0.05f)
            {
                return Mathf.Sign(driftDirection);
            }

            if (Mathf.Abs(entryKickDirection) > 0.05f)
            {
                return Mathf.Sign(entryKickDirection);
            }

            return 1f;
        }

        public void ApplyArcadeAssist(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float deltaTime)
        {
            if (tuning == null || body == null || (DriftAmount <= 0.01f && exitAssistTimer <= 0f))
            {
                return;
            }

            float speed = body.velocity.magnitude;
            float steerDirection = Mathf.Abs(driftDirection) > 0.05f ? Mathf.Sign(driftDirection) : entryKickDirection;
            float entry01 = EntryAmount;
            float driftSign = ResolveDriftDirection();
            float turnIn = Mathf.Clamp01(driftSign * input.steer);
            float counterSteer = Mathf.Clamp01(-driftSign * input.steer);
            TurnInAmount = turnIn;
            CounterSteerAmount = counterSteer;
            float yawInput = input.steer * tuning.driftYawAssist * DriftAmount * (1f + turnIn * tuning.driftTightenYawAssist);
            yawInput += entryKickDirection * tuning.driftEntryYawKick * tuning.driftHandbrakeEntryKick * entry01 * Mathf.InverseLerp(tuning.driftMinSpeed, tuning.driftMinSpeed + 12f, speed);
            body.AddRelativeTorque(Vector3.up * yawInput, ForceMode.Acceleration);

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            float exitAssist01 = Mathf.Clamp01(Mathf.InverseLerp(0f, Mathf.Max(0.01f, tuning.driftExitHoldSeconds), exitAssistTimer));
            float lowDriftExit01 = Mathf.InverseLerp(0.72f, 0.06f, DriftAmount);
            float exitControl01 = Mathf.Clamp01(Mathf.Max(CounterSteerAmount * lowDriftExit01, exitAssist01 * lowDriftExit01));
            float dampingGrip = Mathf.Lerp(tuning.driftLateralDamping, 1.08f, CounterSteerAmount * tuning.driftCounterSteerAssist);
            float lateralDampingRate = Mathf.Lerp(6.5f, 18f, exitControl01);
            localVelocity.x *= Mathf.Lerp(1f, dampingGrip, Mathf.Max(DriftAmount, exitAssist01 * 0.55f) * deltaTime * lateralDampingRate);
            float slideThrow = tuning.driftSidewaysThrow * DriftAmount * Mathf.Lerp(1f, 0.08f, exitControl01);
            localVelocity.x += steerDirection * slideThrow * Mathf.Clamp01(speed / 24f) * Mathf.Lerp(0.72f, 1.2f, input.throttle * tuning.driftThrottleInfluence) * Mathf.Lerp(1f, 0.25f, CounterSteerAmount) * deltaTime;
            body.velocity = body.transform.TransformDirection(localVelocity);

            if (CounterSteerAmount > 0.05f || exitControl01 > 0.05f)
            {
                float yawDamping = body.angularVelocity.y * Mathf.Lerp(1.2f, tuning.driftExitYawDamping, exitControl01) * Mathf.Max(CounterSteerAmount, exitControl01);
                body.AddTorque(Vector3.up * -yawDamping, ForceMode.Acceleration);
            }

            if (input.throttle > 0.1f)
            {
                float forwardAssist = tuning.driftForwardAssist * input.throttle * Mathf.Max(DriftAmount, ExitBoostTimer > 0f ? 0.42f : 0f) * Mathf.Lerp(1f, 0.62f, exitControl01);
                body.AddForce(body.transform.forward * forwardAssist, ForceMode.Acceleration);
            }
        }
    }
}
