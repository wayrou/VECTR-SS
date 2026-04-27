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
        private float previousClutch;

        public void Reset()
        {
            IsDrifting = false;
            DriftAmount = 0f;
            SlipAngle = 0f;
            clutchKickTimer = 0f;
            previousClutch = 0f;
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
            if (clutchReleasedQuickly)
            {
                clutchKickTimer = tuning.clutchKickDuration;
            }

            clutchKickTimer = Mathf.Max(0f, clutchKickTimer - deltaTime);
            previousClutch = input.clutch;

            float slip01 = Mathf.InverseLerp(tuning.driftSlipThreshold * 10f, tuning.driftSlipThreshold * 42f, Mathf.Abs(SlipAngle));
            float handbrake01 = input.handbrake ? 1f : 0f;
            float kick01 = ClutchKickActive ? tuning.clutchKickSlipBoost : 0f;
            float target = speed >= tuning.driftMinSpeed ? Mathf.Clamp01(Mathf.Max(slip01, handbrake01, kick01)) : 0f;

            DriftAmount = Mathf.MoveTowards(DriftAmount, target, deltaTime * (target > DriftAmount ? 5f : 2.5f));
            IsDrifting = DriftAmount > 0.2f;
        }

        public void ApplyArcadeAssist(VehicleTuning tuning, Rigidbody body, VehicleInputState input, float deltaTime)
        {
            if (tuning == null || body == null || DriftAmount <= 0.01f)
            {
                return;
            }

            float yawInput = input.steer * tuning.driftYawAssist * DriftAmount;
            body.AddRelativeTorque(Vector3.up * yawInput, ForceMode.Acceleration);

            Vector3 localVelocity = body.transform.InverseTransformDirection(body.velocity);
            localVelocity.x *= Mathf.Lerp(1f, tuning.driftSideGrip, DriftAmount * deltaTime * 8f);
            body.velocity = body.transform.TransformDirection(localVelocity);
        }
    }
}
