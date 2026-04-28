using GTX.Progression;
using UnityEngine;

namespace GTX.Visuals
{
    public static class VectrStyleTokens
    {
        public static readonly Color InkBlack = new Color(0.006f, 0.008f, 0.012f, 1f);
        public static readonly Color AsphaltNavy = new Color(0.045f, 0.064f, 0.095f, 1f);
        public static readonly Color WarmConcreteGray = new Color(0.55f, 0.55f, 0.50f, 1f);
        public static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.76f, 1f);
        public static readonly Color OilGray = new Color(0.14f, 0.15f, 0.16f, 1f);
        public static readonly Color RubberBlack = new Color(0.025f, 0.027f, 0.028f, 1f);
        public static readonly Color SignalRed = new Color(0.94f, 0.08f, 0.055f, 1f);
        public static readonly Color SafetyOrange = new Color(1f, 0.42f, 0.045f, 1f);
        public static readonly Color ElectricCyan = new Color(0.03f, 0.86f, 1f, 1f);
        public static readonly Color HotMagenta = new Color(1f, 0.08f, 0.66f, 1f);
        public static readonly Color AcidYellowGreen = new Color(0.74f, 1f, 0.08f, 1f);
        public static readonly Color DeepViolet = new Color(0.13f, 0.07f, 0.32f, 1f);
        public static readonly Color DustTan = new Color(0.67f, 0.52f, 0.34f, 1f);
        public static readonly Color RustOrange = new Color(0.64f, 0.28f, 0.08f, 1f);

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
