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
        public bool ClutchKickActive => clutchKickTimer > 0f;

        private float clutchKickTimer;
        private float entryKickTimer;
        private float entryKickDuration;
        private float entryKickDirection;
        private float exitAssistTimer;
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
            clutchKickTimer = 0f;
            entryKickTimer = 0f;
            entryKickDuration = 0f;
            entryKickDirection = 0f;
            exitAssistTimer = 0f;
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
            float throttleSustain = input.handbrake ? input.throttle * tuning.driftThrottleInfluence * Mathf.Clamp01(DriftAmount) : 0f;
            float sustain01 = input.handbrake ? Mathf.Clamp01(Mathf.Max(slip01, throttleSustain) * tuning.driftSustain) : 0f;
            float intentionalSlip01 = input.handbrake || EntryAmount > 0f ? slip01 : 0f;
            float target = speed >= tuning.driftMinSpeed ? Mathf.Clamp01(Mathf.Max(intentionalSlip01, handbrake01, kick01, sustain01)) : 0f;

            previousDriftAmount = DriftAmount;
            DriftAmount = Mathf.MoveTowards(DriftAmount, target, deltaTime * (target > DriftAmount ? 8f : Mathf.Max(0.25f, tuning.driftExitRecovery)));
            bool exitingDrift = previousDriftAmount > 0.2f && DriftAmount < previousDriftAmount - 0.001f;
            if (exitingDrift || (previousDriftAmount > 0.2f && DriftAmount <= 0.2f))
            {
                exitAssistTimer = Mathf.Max(exitAssistTimer, tuning.driftExitHoldSeconds);
            }

            exitAssistTimer = Mathf.Max(0f, exitAssistTimer - deltaTime);
            IsDrifting = DriftAmount > 0.2f;
            if (!IsDrifting && exitAssistTimer <= 0f)
            {
                CounterSteerAmount = 0f;
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
            entryKickDuration = Mathf.Max(0.05f, duration);
            entryKickTimer = entryKickDuration;
            exitAssistTimer = 0f;
        }

        public void ApplyArcadeAssist(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float deltaTime)
        {
            if (tuning == null || body == null || (DriftAmount <= 0.01f && exitAssistTimer <= 0f))
            {
                return;
            }

            float speed = body.velocity.magnitude;
            float steerDirection = Mathf.Abs(input.steer) > 0.04f ? Mathf.Sign(input.steer) : entryKickDirection;
            float entry01 = EntryAmount;
            float yawInput = input.steer * tuning.driftYawAssist * DriftAmount;
            yawInput += entryKickDirection * tuning.driftEntryYawKick * tuning.driftHandbrakeEntryKick * entry01 * Mathf.InverseLerp(tuning.driftMinSpeed, tuning.driftMinSpeed + 12f, speed);
            body.AddRelativeTorque(Vector3.up * yawInput, ForceMode.Acceleration);

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            float slipSign = Mathf.Abs(SlipAngle) > 1.2f ? Mathf.Sign(SlipAngle) : 0f;
            CounterSteerAmount = Mathf.Clamp01(-slipSign * input.steer);
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
                float forwardAssist = tuning.driftForwardAssist * input.throttle * DriftAmount * Mathf.Lerp(1f, 0.62f, exitControl01);
                body.AddForce(body.transform.forward * forwardAssist, ForceMode.Acceleration);
            }
        }
    }
}
