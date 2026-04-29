using GTX.Progression;
using UnityEngine;

namespace GTX.Visuals
{
    public static class VectrStyleTokens
    {
        public static readonly Color InkBlack = new Color(0.018f, 0.018f, 0.016f, 1f);
        public static readonly Color AsphaltNavy = new Color(0.072f, 0.082f, 0.086f, 1f);
        public static readonly Color WarmConcreteGray = new Color(0.54f, 0.53f, 0.47f, 1f);
        public static readonly Color BoneWhite = new Color(0.90f, 0.86f, 0.72f, 1f);
        public static readonly Color OilGray = new Color(0.16f, 0.16f, 0.145f, 1f);
        public static readonly Color RubberBlack = new Color(0.034f, 0.034f, 0.03f, 1f);
        public static readonly Color SignalRed = new Color(0.66f, 0.08f, 0.055f, 1f);
        public static readonly Color SafetyOrange = new Color(0.78f, 0.32f, 0.075f, 1f);
        public static readonly Color ElectricCyan = new Color(0.13f, 0.36f, 0.48f, 1f);
        public static readonly Color HotMagenta = new Color(0.54f, 0.16f, 0.27f, 1f);
        public static readonly Color AcidYellowGreen = new Color(0.42f, 0.50f, 0.24f, 1f);
        public static readonly Color DeepViolet = new Color(0.18f, 0.15f, 0.22f, 1f);
        public static readonly Color DustTan = new Color(0.62f, 0.49f, 0.33f, 1f);
        public static readonly Color RustOrange = new Color(0.55f, 0.25f, 0.10f, 1f);

        public const float OuterOutlineMultiplier = 1.13f;
        public const float PanelOutlineMultiplier = 1.075f;
        public const float DetailOutlineMultiplier = 1.035f;

        public static Color ShadowFor(Color color)
        {
            return Color.Lerp(color, InkBlack, 0.56f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color VehicleBody(VectorSSVehicleId vehicle)
        {
            switch (vehicle)
            {
                case VectorSSVehicleId.Hammer:
                    return SignalRed;
                case VectorSSVehicleId.Needle:
                    return BoneWhite;
                case VectorSSVehicleId.Surge:
                    return ElectricCyan;
                case VectorSSVehicleId.Razor:
                    return OilGray;
                case VectorSSVehicleId.Hauler:
                    return SafetyOrange;
                default:
                    return BoneWhite;
            }
        }

        public static Color VehicleAccent(VectorSSVehicleId vehicle)
        {
            switch (vehicle)
            {
                case VectorSSVehicleId.Hammer:
                    return SafetyOrange;
                case VectorSSVehicleId.Needle:
                    return HotMagenta;
                case VectorSSVehicleId.Surge:
                    return DeepViolet;
                case VectorSSVehicleId.Razor:
                    return AcidYellowGreen;
                case VectorSSVehicleId.Hauler:
                    return ElectricCyan;
                default:
                    return ElectricCyan;
            }
        }

        public static Color VehicleSecondary(VectorSSVehicleId vehicle)
        {
            switch (vehicle)
            {
                case VectorSSVehicleId.Hammer:
                    return OilGray;
                case VectorSSVehicleId.Needle:
                    return ElectricCyan;
                case VectorSSVehicleId.Surge:
                    return BoneWhite;
                case VectorSSVehicleId.Razor:
                    return ElectricCyan;
                case VectorSSVehicleId.Hauler:
                    return BoneWhite;
                default:
                    return WarmConcreteGray;
            }
        }

        public static Color BoostTrail(VectorSSVehicleId vehicle)
        {
            switch (vehicle)
            {
                case VectorSSVehicleId.Surge:
                    return ElectricCyan;
                case VectorSSVehicleId.Razor:
                    return AcidYellowGreen;
                case VectorSSVehicleId.Hauler:
                    return ElectricCyan;
                case VectorSSVehicleId.Needle:
                    return HotMagenta;
                default:
                    return SafetyOrange;
            }
        }

        public static Color MapRoad(VectorSSMapId map)
        {
            if (map == VectorSSMapId.SpecialStage)
            {
                return new Color(0.05f, 0.052f, 0.048f, 1f);
            }

            return map == VectorSSMapId.RubberRidge ? RubberBlack : AsphaltNavy;
        }

        public static Color MapGround(VectorSSMapId map)
        {
            switch (map)
            {
                case VectorSSMapId.ScraplineYard:
                    return RustOrange;
                case VectorSSMapId.RubberRidge:
                    return DustTan;
                case VectorSSMapId.SpecialStage:
                    return new Color(0.33f, 0.32f, 0.28f, 1f);
                default:
                    return WarmConcreteGray;
            }
        }

        public static Color MapBarrier(VectorSSMapId map)
        {
            switch (map)
            {
                case VectorSSMapId.ScraplineYard:
                    return new Color(0.63f, 0.61f, 0.54f, 1f);
                case VectorSSMapId.RubberRidge:
                    return RubberBlack;
                case VectorSSMapId.SpecialStage:
                    return new Color(0.66f, 0.65f, 0.58f, 1f);
                default:
                    return BoneWhite;
            }
        }

        public static Color MapAccent(VectorSSMapId map)
        {
            switch (map)
            {
                case VectorSSMapId.ScraplineYard:
                    return SafetyOrange;
                case VectorSSMapId.RubberRidge:
                    return AcidYellowGreen;
                case VectorSSMapId.SpecialStage:
                    return ElectricCyan;
                default:
                    return ElectricCyan;
            }
        }

        public static Color MapWarning(VectorSSMapId map)
        {
            return map == VectorSSMapId.BlacklineCircuit ? HotMagenta : SafetyOrange;
        }
    }
}
