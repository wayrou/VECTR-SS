using System;
using UnityEngine;

namespace GTX.Data
{
    public enum GTXCarClass
    {
        Strike,
        Drift,
        Air,
        Volt,
        Phantom
    }

    [Serializable]
    public sealed class GTXCarClassPreset
    {
        public GTXCarClass carClass;
        public string displayName;
        [TextArea] public string fantasy;
        public float massMultiplier = 1f;
        public float engineMultiplier = 1f;
        public float steeringMultiplier = 1f;
        public float gripMultiplier = 1f;
        public float boostMultiplier = 1f;
        public float ramMultiplier = 1f;

        public static GTXCarClassPreset Create(GTXCarClass carClass)
        {
            switch (carClass)
            {
                case GTXCarClass.Drift:
                    return new GTXCarClassPreset
                    {
                        carClass = carClass,
                        displayName = "Drift Class",
                        fantasy = "Technical cornering, clutch kicks, and Flow from controlled slides.",
                        massMultiplier = 0.94f,
                        engineMultiplier = 0.96f,
                        steeringMultiplier = 1.28f,
                        gripMultiplier = 0.82f,
                        boostMultiplier = 1.02f,
                        ramMultiplier = 0.9f
                    };
                case GTXCarClass.Air:
                    return new GTXCarClassPreset
                    {
                        carClass = carClass,
                        displayName = "Air Class",
                        fantasy = "Jump control and landing shockwaves. Placeholder for a later air-drop move.",
                        massMultiplier = 0.9f,
                        engineMultiplier = 1f,
                        steeringMultiplier = 1.08f,
                        gripMultiplier = 0.96f,
                        boostMultiplier = 1.08f,
                        ramMultiplier = 0.88f
                    };
                case GTXCarClass.Volt:
                    return new GTXCarClassPreset
                    {
                        carClass = carClass,
                        displayName = "Volt Class",
                        fantasy = "Aggressive boost output with more heat risk.",
                        massMultiplier = 0.92f,
                        engineMultiplier = 1.12f,
                        steeringMultiplier = 1.06f,
                        gripMultiplier = 0.98f,
                        boostMultiplier = 1.35f,
                        ramMultiplier = 0.94f
                    };
                case GTXCarClass.Phantom:
                    return new GTXCarClassPreset
                    {
                        carClass = carClass,
                        displayName = "Phantom Class",
                        fantasy = "Evasive trickster platform. Placeholder for afterimages and jamming.",
                        massMultiplier = 0.88f,
                        engineMultiplier = 0.98f,
                        steeringMultiplier = 1.22f,
                        gripMultiplier = 1.02f,
                        boostMultiplier = 1.12f,
                        ramMultiplier = 0.78f
                    };
                default:
                    return new GTXCarClassPreset
                    {
                        carClass = GTXCarClass.Strike,
                        displayName = "Strike Class",
                        fantasy = "Heavy frame, stable contact, strong side slams and boost rams.",
                        massMultiplier = 1.16f,
                        engineMultiplier = 1.08f,
                        steeringMultiplier = 0.94f,
                        gripMultiplier = 1.08f,
                        boostMultiplier = 1f,
                        ramMultiplier = 1.32f
                    };
            }
        }
    }
}
