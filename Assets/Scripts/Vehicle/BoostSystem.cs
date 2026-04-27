using GTX.Data;
using UnityEngine;

namespace GTX.Vehicle
{
    [System.Serializable]
    public sealed class BoostSystem
    {
        public float Resource { get; private set; }
        public float Heat { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsOverheated { get; private set; }
        public float Resource01 => tuningCapacity <= 0f ? 0f : Resource / tuningCapacity;
        public float Heat01 => overheatThreshold <= 0f ? 0f : Heat / overheatThreshold;

        private float tuningCapacity = 100f;
        private float overheatThreshold = 100f;

        public void Reset(VehicleTuning tuning)
        {
            tuningCapacity = tuning != null ? tuning.boostCapacity : 100f;
            overheatThreshold = tuning != null ? tuning.overheatThreshold : 100f;
            Resource = tuningCapacity;
            Heat = 0f;
            IsActive = false;
            IsOverheated = false;
        }

        public void Tick(VehicleTuning tuning, bool requested, float throttle, float deltaTime)
        {
            if (tuning == null)
            {
                return;
            }

            tuningCapacity = Mathf.Max(0.01f, tuning.boostCapacity);
            overheatThreshold = Mathf.Max(0.01f, tuning.overheatThreshold);
            bool canBoost = requested && throttle > 0.15f && Resource > 0.01f && !IsOverheated;
            IsActive = canBoost;

            if (IsActive)
            {
                Resource = Mathf.Max(0f, Resource - tuning.boostBurnPerSecond * deltaTime);
                Heat = Mathf.Min(overheatThreshold, Heat + tuning.boostHeatPerSecond * deltaTime);
                if (Heat >= overheatThreshold)
                {
                    IsOverheated = true;
                    IsActive = false;
                }
            }
            else
            {
                Resource = Mathf.Min(tuningCapacity, Resource + tuning.boostRegenPerSecond * deltaTime);
                Heat = Mathf.Max(0f, Heat - tuning.boostCoolPerSecond * deltaTime);
                if (IsOverheated && Heat <= tuning.overheatedLockoutHeat)
                {
                    IsOverheated = false;
                }
            }
        }

        public float GetTorqueMultiplier(VehicleTuning tuning)
        {
            return IsActive && tuning != null ? tuning.boostTorqueMultiplier : 1f;
        }
    }
}
