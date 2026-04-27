using System;
using UnityEngine;

namespace GTX.Data
{
    public enum GTXTuningPreset
    {
        Strike,
        Drift,
        Volt
    }

    [Serializable]
    public class GTXTuningProfile
    {
        [Range(0.6f, 1.6f)] public float acceleration = 1.0f;
        [Range(0.6f, 1.6f)] public float topSpeed = 1.0f;
        [Range(0.6f, 1.8f)] public float grip = 1.0f;
        [Range(0.4f, 1.8f)] public float steeringResponse = 1.0f;
        [Range(0.4f, 1.8f)] public float brakePower = 1.0f;
        [Range(0.4f, 1.8f)] public float boostPower = 1.0f;
        [Range(0.5f, 1.8f)] public float cooling = 1.0f;

        public static GTXTuningProfile FromPreset(GTXTuningPreset preset)
        {
            switch (preset)
            {
                case GTXTuningPreset.Drift:
                    return new GTXTuningProfile
                    {
                        acceleration = 0.95f,
                        topSpeed = 0.92f,
                        grip = 0.72f,
                        steeringResponse = 1.42f,
                        brakePower = 1.08f,
                        boostPower = 1.05f,
                        cooling = 0.94f
                    };
                case GTXTuningPreset.Volt:
                    return new GTXTuningProfile
                    {
                        acceleration = 1.18f,
                        topSpeed = 1.03f,
                        grip = 0.96f,
                        steeringResponse = 1.08f,
                        brakePower = 0.95f,
                        boostPower = 1.45f,
                        cooling = 0.76f
                    };
                default:
                    return new GTXTuningProfile
                    {
                        acceleration = 1.12f,
                        topSpeed = 1.16f,
                        grip = 1.08f,
                        steeringResponse = 0.98f,
                        brakePower = 1.12f,
                        boostPower = 1.0f,
                        cooling = 1.08f
                    };
            }
        }

        public void CopyFrom(GTXTuningProfile other)
        {
            if (other == null)
            {
                return;
            }

            acceleration = other.acceleration;
            topSpeed = other.topSpeed;
            grip = other.grip;
            steeringResponse = other.steeringResponse;
            brakePower = other.brakePower;
            boostPower = other.boostPower;
            cooling = other.cooling;
        }
    }
}
