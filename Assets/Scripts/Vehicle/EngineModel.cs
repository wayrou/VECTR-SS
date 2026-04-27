using GTX.Data;
using UnityEngine;

namespace GTX.Vehicle
{
    [System.Serializable]
    public sealed class EngineModel
    {
        public float Rpm { get; private set; }
        public float NormalizedRpm { get; private set; }
        public bool IsStalled { get; private set; }

        public void Reset(VehicleTuning tuning)
        {
            Rpm = tuning != null ? tuning.idleRpm : 900f;
            NormalizedRpm = 0f;
            IsStalled = false;
        }

        public void Tick(VehicleTuning tuning, float throttle, float clutchTransfer, float drivetrainRpm, float deltaTime)
        {
            if (tuning == null)
            {
                return;
            }

            float freeRevTarget = Mathf.Lerp(tuning.idleRpm, tuning.redlineRpm, Mathf.Clamp01(throttle));
            float coupledTarget = Mathf.Max(tuning.stallRpm, drivetrainRpm);
            float targetRpm = Mathf.Lerp(freeRevTarget, coupledTarget, Mathf.Clamp01(clutchTransfer));

            Rpm = Mathf.Lerp(Rpm, targetRpm, 1f - Mathf.Exp(-tuning.rpmResponse * deltaTime));
            Rpm = Mathf.Clamp(Rpm, 0f, tuning.redlineRpm * 1.05f);
            IsStalled = Rpm < tuning.stallRpm && throttle < 0.05f;
            if (IsStalled)
            {
                Rpm = Mathf.MoveTowards(Rpm, 0f, tuning.idleRpm * deltaTime);
            }
            else if (Rpm < tuning.idleRpm && throttle > 0.02f)
            {
                Rpm = Mathf.MoveTowards(Rpm, tuning.idleRpm, tuning.idleRpm * deltaTime);
            }

            NormalizedRpm = Mathf.InverseLerp(tuning.idleRpm, tuning.redlineRpm, Rpm);
        }

        public float GetCrankTorque(VehicleTuning tuning, float throttle)
        {
            if (tuning == null || IsStalled)
            {
                return 0f;
            }

            float torqueFactor = tuning.torqueCurve != null ? tuning.torqueCurve.Evaluate(Mathf.Clamp01(NormalizedRpm)) : 1f;
            float positiveTorque = tuning.peakTorque * torqueFactor * Mathf.Clamp01(throttle);
            float brakingTorque = throttle < 0.05f ? -tuning.engineBrakeTorque * Mathf.InverseLerp(tuning.idleRpm, tuning.redlineRpm, Rpm) : 0f;
            return positiveTorque + brakingTorque;
        }
    }
}
