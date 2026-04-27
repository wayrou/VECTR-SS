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
        public bool ClutchKickActive => clutchKickTimer > 0f;

        private float clutchKickTimer;
        private float entryKickTimer;
        private float entryKickDirection;
        private float previousClutch;
        private bool previousHandbrake;

        public void Reset()
        {
            IsDrifting = false;
            DriftAmount = 0f;
            SlipAngle = 0f;
            clutchKickTimer = 0f;
            entryKickTimer = 0f;
            entryKickDirection = 0f;
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
            if (clutchReleasedQuickly)
            {
                clutchKickTimer = tuning.clutchKickDuration;
                PrimeEntryKick(input, localVelocity);
            }

            if (handbrakeTapped)
            {
                PrimeEntryKick(input, localVelocity);
            }

            clutchKickTimer = Mathf.Max(0f, clutchKickTimer - deltaTime);
            entryKickTimer = Mathf.Max(0f, entryKickTimer - deltaTime);
            previousClutch = input.clutch;
            previousHandbrake = input.handbrake;

            float slip01 = Mathf.InverseLerp(tuning.driftSlipThreshold * 8f, tuning.driftSlipThreshold * 38f, Mathf.Abs(SlipAngle));
            float handbrake01 = input.handbrake ? 1f : 0f;
            float kick01 = ClutchKickActive ? Mathf.Max(0.72f, tuning.clutchKickSlipBoost) : 0f;
            float target = speed >= tuning.driftMinSpeed ? Mathf.Clamp01(Mathf.Max(slip01, handbrake01, kick01)) : 0f;

            DriftAmount = Mathf.MoveTowards(DriftAmount, target, deltaTime * (target > DriftAmount ? 8f : 1.75f));
            IsDrifting = DriftAmount > 0.2f;
        }

        private void PrimeEntryKick(VehicleInputState input, Vector3 localVelocity)
        {
            float direction = Mathf.Abs(input.steer) > 0.05f ? Mathf.Sign(input.steer) : Mathf.Sign(localVelocity.x);
            if (Mathf.Abs(direction) < 0.05f)
            {
                direction = 1f;
            }

            entryKickDirection = direction;
            entryKickTimer = 0.22f;
        }

        public void ApplyArcadeAssist(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float deltaTime)
        {
            if (tuning == null || body == null || DriftAmount <= 0.01f)
            {
                return;
            }

            float speed = body.velocity.magnitude;
            float steerDirection = Mathf.Abs(input.steer) > 0.04f ? Mathf.Sign(input.steer) : entryKickDirection;
            float entry01 = Mathf.Clamp01(entryKickTimer / 0.22f);
            float yawInput = input.steer * tuning.driftYawAssist * DriftAmount;
            yawInput += entryKickDirection * tuning.driftEntryYawKick * entry01 * Mathf.InverseLerp(tuning.driftMinSpeed, tuning.driftMinSpeed + 12f, speed);
            body.AddRelativeTorque(Vector3.up * yawInput, ForceMode.Acceleration);

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            float slipSign = Mathf.Abs(SlipAngle) > 1.2f ? Mathf.Sign(SlipAngle) : 0f;
            float counterSteer01 = Mathf.Clamp01(-slipSign * input.steer);
            float sideGrip = Mathf.Lerp(tuning.driftSideGrip, tuning.normalSideGrip, counterSteer01 * tuning.driftCounterSteerAssist);
            localVelocity.x *= Mathf.Lerp(1f, sideGrip, DriftAmount * deltaTime * 6.5f);
            localVelocity.x += steerDirection * tuning.driftSidewaysThrow * DriftAmount * Mathf.Clamp01(speed / 24f) * deltaTime;
            body.velocity = body.transform.TransformDirection(localVelocity);

            if (input.throttle > 0.1f)
            {
                float forwardAssist = tuning.driftForwardAssist * input.throttle * DriftAmount;
                body.AddForce(body.transform.forward * forwardAssist, ForceMode.Acceleration);
            }
        }
    }
}
