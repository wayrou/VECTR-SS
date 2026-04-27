using System;
using System.Collections.Generic;
using GTX.Data;
using UnityEngine;

namespace GTX.Progression
{
    public enum VectorSSScreen
    {
        MainMenu,
        MapSelect,
        VehicleSelect,
        Garage,
        Racing,
        Results
    }

    public enum VectorSSMapId
    {
        BlacklineCircuit,
        ScraplineYard,
        RubberRidge
    }

    public enum VectorSSVehicleId
    {
        Hammer,
        Needle,
        Surge,
        Razor
    }

    public enum VectorSSVehicleClass
    {
        Strike,
        Drift,
        Volt,
        Bike
    }

    [Serializable]
    public struct VectorSSResources
    {
        public int metal;
        public int plastic;
        public int rubber;

        public VectorSSResources(int metal, int plastic, int rubber)
        {
            this.metal = metal;
            this.plastic = plastic;
            this.rubber = rubber;
        }

        public bool CanAfford(VectorSSResources cost)
        {
            return metal >= cost.metal && plastic >= cost.plastic && rubber >= cost.rubber;
        }

        public void Add(VectorSSResources reward)
        {
            metal += reward.metal;
            plastic += reward.plastic;
            rubber += reward.rubber;
        }

        public bool TrySpend(VectorSSResources cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            metal -= cost.metal;
            plastic -= cost.plastic;
            rubber -= cost.rubber;
            return true;
        }

        public override string ToString()
        {
            return "Metal " + metal + "   Plastic " + plastic + "   Rubber " + rubber;
        }
    }

    [Serializable]
    public sealed class VectorSSTuningState
    {
        [Range(0.45f, 1.85f)] public float steering = 1f;
        [Range(0.45f, 1.85f)] public float brakeBias = 1f;
        [Range(0.45f, 1.85f)] public float driftGrip = 1f;
        [Range(0.65f, 1.45f)] public float finalDrive = 1f;
        [Range(0.45f, 1.85f)] public float boostValve = 1f;
        [Range(0.55f, 1.65f)] public float suspension = 1f;
        [Range(0.55f, 1.85f)] public float tireGrip = 1f;
        [Range(0.55f, 1.8f)] public float clutchBite = 1f;
        [Range(0.65f, 1.6f)] public float outlineThickness = 1f;
        [Range(0f, 1f)] public float cameraShake = 0.5f;
        [Range(0.45f, 1.8f)] public float leanResponse = 1f;
        [Range(0.45f, 1.85f)] public float rearBrakeSlide = 1f;

        public void CopyFrom(VectorSSTuningState other)
        {
            if (other == null)
            {
                return;
            }

            steering = other.steering;
            brakeBias = other.brakeBias;
            driftGrip = other.driftGrip;
            finalDrive = other.finalDrive;
            boostValve = other.boostValve;
            suspension = other.suspension;
            tireGrip = other.tireGrip;
            clutchBite = other.clutchBite;
            outlineThickness = other.outlineThickness;
            cameraShake = other.cameraShake;
            leanResponse = other.leanResponse;
            rearBrakeSlide = other.rearBrakeSlide;
        }
    }

    public sealed class VectorSSVehicleDefinition
    {
        public VectorSSVehicleId id;
        public string displayName;
        public string fullName;
        public VectorSSVehicleClass vehicleClass;
        public string role;
        public string primaryResource;
        public Color bodyColor;
        public Color accentColor;
        public Color secondaryColor;
        public float massMultiplier = 1f;
        public float engineMultiplier = 1f;
        public float steeringMultiplier = 1f;
        public float gripMultiplier = 1f;
        public float boostMultiplier = 1f;
        public float ramMultiplier = 1f;
        public float impactResistance = 1f;
        public float nearMissFlowMultiplier = 1f;
        public float airControlMultiplier = 1f;
        public Vector3 visualScale = Vector3.one;
        public Vector3 colliderSize = new Vector3(2.25f, 0.95f, 4.45f);
        public Vector3 colliderCenter = new Vector3(0f, 0.72f, 0f);
        public bool isBike;

        public string StatsLine
        {
            get
            {
                return "Mass " + massMultiplier.ToString("0.00") +
                    "  Turn " + steeringMultiplier.ToString("0.00") +
                    "  Boost " + boostMultiplier.ToString("0.00") +
                    "  Ram " + ramMultiplier.ToString("0.00");
            }
        }
    }

    public sealed class VectorSSMapDefinition
    {
        public VectorSSMapId id;
        public string displayName;
        public string theme;
        public string purpose;
        public int lapCount = 1;
        public VectorSSResources baseReward;
        public VectorSSResources mapBonus;
        public Color roadColor;
        public Color groundColor;
        public Color barrierColor;
    }

    public sealed class VectorSSUpgradeDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public VectorSSResources cost;
        public VectorSSVehicleClass? preferredClass;
    }

    public sealed class VectorSSRaceResult
    {
        public VectorSSMapDefinition map;
        public VectorSSVehicleDefinition vehicle;
        public float raceTime;
        public float flow01;
        public int combatScore;
        public VectorSSResources completionReward;
        public VectorSSResources styleReward;
        public VectorSSResources mapReward;

        public VectorSSResources Total
        {
            get
            {
                return new VectorSSResources(
                    completionReward.metal + styleReward.metal + mapReward.metal,
                    completionReward.plastic + styleReward.plastic + mapReward.plastic,
                    completionReward.rubber + styleReward.rubber + mapReward.rubber);
            }
        }
    }

    public sealed class VectorSSPlayerProfile
    {
        public VectorSSResources resources = new VectorSSResources(100, 90, 100);
        public VectorSSMapId selectedMap = VectorSSMapId.BlacklineCircuit;
        public VectorSSVehicleId selectedVehicle = VectorSSVehicleId.Hammer;
        public VectorSSTuningState tuning = new VectorSSTuningState();
        public readonly HashSet<string> purchasedUpgrades = new HashSet<string>();

        public bool HasUpgrade(string id)
        {
            return purchasedUpgrades.Contains(id);
        }

        public bool TryPurchase(VectorSSUpgradeDefinition upgrade)
        {
            if (upgrade == null || HasUpgrade(upgrade.id) || !resources.TrySpend(upgrade.cost))
            {
                return false;
            }

            purchasedUpgrades.Add(upgrade.id);
            return true;
        }
    }

    public static class VectorSSCatalog
    {
        public static readonly VectorSSVehicleDefinition[] Vehicles =
        {
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Hammer,
                displayName = "Hammer",
                fullName = "Vector SS-C \"Hammer\"",
                vehicleClass = VectorSSVehicleClass.Strike,
                role = "Heavy combat / ramming / stability",
                primaryResource = "Metal",
                bodyColor = new Color(0.92f, 0.83f, 0.58f, 1f),
                accentColor = new Color(0.96f, 0.25f, 0.06f, 1f),
                secondaryColor = new Color(0.08f, 0.19f, 0.44f, 1f),
                massMultiplier = 1.24f,
                engineMultiplier = 1.05f,
                steeringMultiplier = 0.82f,
                gripMultiplier = 1.08f,
                boostMultiplier = 0.96f,
                ramMultiplier = 1.48f,
                impactResistance = 1.38f,
                nearMissFlowMultiplier = 0.8f,
                visualScale = new Vector3(1.12f, 1f, 1.12f),
                colliderSize = new Vector3(2.45f, 1.02f, 4.82f)
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Needle,
                displayName = "Needle",
                fullName = "Vector SS-D \"Needle\"",
                vehicleClass = VectorSSVehicleClass.Drift,
                role = "Technical cornering / clutch kicks / Flow",
                primaryResource = "Rubber",
                bodyColor = new Color(0.88f, 0.90f, 0.78f, 1f),
                accentColor = new Color(0.06f, 0.55f, 0.95f, 1f),
                secondaryColor = new Color(0.98f, 0.38f, 0.08f, 1f),
                massMultiplier = 0.88f,
                engineMultiplier = 0.98f,
                steeringMultiplier = 1.38f,
                gripMultiplier = 0.9f,
                boostMultiplier = 1.03f,
                ramMultiplier = 0.76f,
                impactResistance = 0.82f,
                nearMissFlowMultiplier = 1.14f,
                visualScale = new Vector3(0.9f, 0.92f, 1.08f),
                colliderSize = new Vector3(2.0f, 0.86f, 4.48f)
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Surge,
                displayName = "Surge",
                fullName = "Vector SS-V \"Surge\"",
                vehicleClass = VectorSSVehicleClass.Volt,
                role = "Boost / speed / heat risk",
                primaryResource = "Plastic + Metal",
                bodyColor = new Color(0.15f, 0.48f, 0.95f, 1f),
                accentColor = new Color(1f, 0.86f, 0.08f, 1f),
                secondaryColor = new Color(0.03f, 0.05f, 0.08f, 1f),
                massMultiplier = 0.94f,
                engineMultiplier = 1.12f,
                steeringMultiplier = 1.04f,
                gripMultiplier = 0.94f,
                boostMultiplier = 1.52f,
                ramMultiplier = 0.88f,
                impactResistance = 0.9f,
                nearMissFlowMultiplier = 1.02f,
                visualScale = new Vector3(0.98f, 0.96f, 1.04f),
                colliderSize = new Vector3(2.15f, 0.9f, 4.42f)
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Razor,
                displayName = "Razor",
                fullName = "Vector SS-B \"Razor\"",
                vehicleClass = VectorSSVehicleClass.Bike,
                role = "Agility / near-misses / shortcuts / high-risk precision",
                primaryResource = "Rubber + Plastic",
                bodyColor = new Color(0.98f, 0.92f, 0.72f, 1f),
                accentColor = new Color(0.98f, 0.18f, 0.12f, 1f),
                secondaryColor = new Color(0.04f, 0.68f, 0.86f, 1f),
                massMultiplier = 0.54f,
                engineMultiplier = 1.18f,
                steeringMultiplier = 1.62f,
                gripMultiplier = 1.02f,
                boostMultiplier = 1.16f,
                ramMultiplier = 0.34f,
                impactResistance = 0.52f,
                nearMissFlowMultiplier = 1.75f,
                airControlMultiplier = 1.4f,
                visualScale = new Vector3(0.58f, 1f, 0.92f),
                colliderSize = new Vector3(0.92f, 1.25f, 2.8f),
                colliderCenter = new Vector3(0f, 0.82f, 0f),
                isBike = true
            }
        };

        public static readonly VectorSSMapDefinition[] Maps =
        {
            new VectorSSMapDefinition
            {
                id = VectorSSMapId.BlacklineCircuit,
                displayName = "Blackline Circuit",
                purpose = "Intro city/highway map",
                theme = "Elevated cel-shaded city/highway with balanced driving.",
                lapCount = 1,
                baseReward = new VectorSSResources(32, 30, 30),
                mapBonus = new VectorSSResources(14, 12, 12),
                roadColor = new Color(0.17f, 0.19f, 0.21f, 1f),
                groundColor = new Color(0.52f, 0.58f, 0.62f, 1f),
                barrierColor = new Color(0.86f, 0.82f, 0.70f, 1f)
            },
            new VectorSSMapDefinition
            {
                id = VectorSSMapId.ScraplineYard,
                displayName = "Scrapline Yard",
                purpose = "Combat-focused industrial map",
                theme = "Containers, cranes, scrap props, and wide lanes.",
                lapCount = 1,
                baseReward = new VectorSSResources(36, 22, 22),
                mapBonus = new VectorSSResources(36, 8, 8),
                roadColor = new Color(0.20f, 0.20f, 0.18f, 1f),
                groundColor = new Color(0.44f, 0.37f, 0.30f, 1f),
                barrierColor = new Color(0.64f, 0.60f, 0.52f, 1f)
            },
            new VectorSSMapDefinition
            {
                id = VectorSSMapId.RubberRidge,
                displayName = "Rubber Ridge",
                purpose = "Drift/jump mountain map",
                theme = "Canyon hairpins, tire walls, jumps, and a narrow bike line.",
                lapCount = 1,
                baseReward = new VectorSSResources(24, 24, 38),
                mapBonus = new VectorSSResources(8, 10, 38),
                roadColor = new Color(0.18f, 0.19f, 0.20f, 1f),
                groundColor = new Color(0.58f, 0.45f, 0.30f, 1f),
                barrierColor = new Color(0.26f, 0.26f, 0.24f, 1f)
            }
        };

        public static readonly VectorSSUpgradeDefinition[] Upgrades =
        {
            new VectorSSUpgradeDefinition { id = "engine_torque_1", displayName = "Engine Torque I", description = "More torque and stronger acceleration.", cost = new VectorSSResources(40, 10, 0) },
            new VectorSSUpgradeDefinition { id = "grip_tires_1", displayName = "Grip Tires I", description = "Higher tire grip and cleaner drift exits.", cost = new VectorSSResources(0, 0, 60) },
            new VectorSSUpgradeDefinition { id = "combat_plating_1", displayName = "Combat Plating I", description = "More mass, ram strength, and impact recovery.", cost = new VectorSSResources(70, 0, 0), preferredClass = VectorSSVehicleClass.Strike },
            new VectorSSUpgradeDefinition { id = "clutch_response_1", displayName = "Clutch Response I", description = "Sharper clutch bite and better clutch kicks.", cost = new VectorSSResources(20, 0, 40), preferredClass = VectorSSVehicleClass.Drift },
            new VectorSSUpgradeDefinition { id = "boost_valve_1", displayName = "Boost Valve I", description = "More boost punch and stronger boost rams.", cost = new VectorSSResources(20, 30, 0), preferredClass = VectorSSVehicleClass.Volt },
            new VectorSSUpgradeDefinition { id = "lightweight_aero_1", displayName = "Lightweight Aero I", description = "Lower mass, more downforce, better top speed.", cost = new VectorSSResources(10, 50, 0) },
            new VectorSSUpgradeDefinition { id = "lightweight_frame_1", displayName = "Lightweight Frame I", description = "Razor acceleration up, impact resistance down.", cost = new VectorSSResources(20, 40, 0), preferredClass = VectorSSVehicleClass.Bike },
            new VectorSSUpgradeDefinition { id = "razor_tires_1", displayName = "Razor Tires I", description = "Better bike cornering and rear-brake slide control.", cost = new VectorSSResources(0, 0, 70), preferredClass = VectorSSVehicleClass.Bike },
            new VectorSSUpgradeDefinition { id = "lean_stabilizer_1", displayName = "Lean Stabilizer I", description = "Razor recovers faster after hard turns.", cost = new VectorSSResources(0, 35, 35), preferredClass = VectorSSVehicleClass.Bike },
            new VectorSSUpgradeDefinition { id = "boost_tuck_1", displayName = "Boost Tuck I", description = "Razor boost stability and top speed.", cost = new VectorSSResources(0, 50, 20), preferredClass = VectorSSVehicleClass.Bike }
        };

        public static VectorSSVehicleDefinition GetVehicle(VectorSSVehicleId id)
        {
            for (int i = 0; i < Vehicles.Length; i++)
            {
                if (Vehicles[i].id == id)
                {
                    return Vehicles[i];
                }
            }

            return Vehicles[0];
        }

        public static VectorSSMapDefinition GetMap(VectorSSMapId id)
        {
            for (int i = 0; i < Maps.Length; i++)
            {
                if (Maps[i].id == id)
                {
                    return Maps[i];
                }
            }

            return Maps[0];
        }

        public static VectorSSUpgradeDefinition GetUpgrade(string id)
        {
            for (int i = 0; i < Upgrades.Length; i++)
            {
                if (Upgrades[i].id == id)
                {
                    return Upgrades[i];
                }
            }

            return null;
        }
    }

    public static class VectorSSSaveSystem
    {
        private const string Prefix = "VectorSS.0.1.0.";

        public static VectorSSPlayerProfile Load()
        {
            VectorSSPlayerProfile profile = new VectorSSPlayerProfile();
            profile.resources = new VectorSSResources(
                PlayerPrefs.GetInt(Prefix + "Metal", profile.resources.metal),
                PlayerPrefs.GetInt(Prefix + "Plastic", profile.resources.plastic),
                PlayerPrefs.GetInt(Prefix + "Rubber", profile.resources.rubber));

            profile.selectedMap = (VectorSSMapId)PlayerPrefs.GetInt(Prefix + "Map", (int)profile.selectedMap);
            profile.selectedVehicle = (VectorSSVehicleId)PlayerPrefs.GetInt(Prefix + "Vehicle", (int)profile.selectedVehicle);
            LoadTuning(profile.tuning);

            string rawUpgrades = PlayerPrefs.GetString(Prefix + "Upgrades", string.Empty);
            if (!string.IsNullOrEmpty(rawUpgrades))
            {
                string[] ids = rawUpgrades.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < ids.Length; i++)
                {
                    profile.purchasedUpgrades.Add(ids[i]);
                }
            }

            return profile;
        }

        public static void Save(VectorSSPlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            PlayerPrefs.SetInt(Prefix + "Metal", profile.resources.metal);
            PlayerPrefs.SetInt(Prefix + "Plastic", profile.resources.plastic);
            PlayerPrefs.SetInt(Prefix + "Rubber", profile.resources.rubber);
            PlayerPrefs.SetInt(Prefix + "Map", (int)profile.selectedMap);
            PlayerPrefs.SetInt(Prefix + "Vehicle", (int)profile.selectedVehicle);
            SaveTuning(profile.tuning);
            PlayerPrefs.SetString(Prefix + "Upgrades", string.Join("|", new List<string>(profile.purchasedUpgrades).ToArray()));
            PlayerPrefs.Save();
        }

        private static void LoadTuning(VectorSSTuningState tuning)
        {
            tuning.steering = PlayerPrefs.GetFloat(Prefix + "Tune.Steering", tuning.steering);
            tuning.brakeBias = PlayerPrefs.GetFloat(Prefix + "Tune.BrakeBias", tuning.brakeBias);
            tuning.driftGrip = PlayerPrefs.GetFloat(Prefix + "Tune.DriftGrip", tuning.driftGrip);
            tuning.finalDrive = PlayerPrefs.GetFloat(Prefix + "Tune.FinalDrive", tuning.finalDrive);
            tuning.boostValve = PlayerPrefs.GetFloat(Prefix + "Tune.BoostValve", tuning.boostValve);
            tuning.suspension = PlayerPrefs.GetFloat(Prefix + "Tune.Suspension", tuning.suspension);
            tuning.tireGrip = PlayerPrefs.GetFloat(Prefix + "Tune.TireGrip", tuning.tireGrip);
            tuning.clutchBite = PlayerPrefs.GetFloat(Prefix + "Tune.ClutchBite", tuning.clutchBite);
            tuning.outlineThickness = PlayerPrefs.GetFloat(Prefix + "Tune.OutlineThickness", tuning.outlineThickness);
            tuning.cameraShake = PlayerPrefs.GetFloat(Prefix + "Tune.CameraShake", tuning.cameraShake);
            tuning.leanResponse = PlayerPrefs.GetFloat(Prefix + "Tune.LeanResponse", tuning.leanResponse);
            tuning.rearBrakeSlide = PlayerPrefs.GetFloat(Prefix + "Tune.RearBrakeSlide", tuning.rearBrakeSlide);
        }

        private static void SaveTuning(VectorSSTuningState tuning)
        {
            PlayerPrefs.SetFloat(Prefix + "Tune.Steering", tuning.steering);
            PlayerPrefs.SetFloat(Prefix + "Tune.BrakeBias", tuning.brakeBias);
            PlayerPrefs.SetFloat(Prefix + "Tune.DriftGrip", tuning.driftGrip);
            PlayerPrefs.SetFloat(Prefix + "Tune.FinalDrive", tuning.finalDrive);
            PlayerPrefs.SetFloat(Prefix + "Tune.BoostValve", tuning.boostValve);
            PlayerPrefs.SetFloat(Prefix + "Tune.Suspension", tuning.suspension);
            PlayerPrefs.SetFloat(Prefix + "Tune.TireGrip", tuning.tireGrip);
            PlayerPrefs.SetFloat(Prefix + "Tune.ClutchBite", tuning.clutchBite);
            PlayerPrefs.SetFloat(Prefix + "Tune.OutlineThickness", tuning.outlineThickness);
            PlayerPrefs.SetFloat(Prefix + "Tune.CameraShake", tuning.cameraShake);
            PlayerPrefs.SetFloat(Prefix + "Tune.LeanResponse", tuning.leanResponse);
            PlayerPrefs.SetFloat(Prefix + "Tune.RearBrakeSlide", tuning.rearBrakeSlide);
        }
    }

    public static class VectorSSProgressionUtility
    {
        public static void ApplyToVehicleTuning(VehicleTuning tuning, VectorSSVehicleDefinition vehicle, VectorSSPlayerProfile profile)
        {
            if (tuning == null || vehicle == null || profile == null)
            {
                return;
            }

            VectorSSTuningState t = profile.tuning;
            tuning.mass *= vehicle.massMultiplier;
            tuning.peakTorque *= vehicle.engineMultiplier * (profile.HasUpgrade("engine_torque_1") ? 1.13f : 1f);
            tuning.maxDriveTorque *= vehicle.engineMultiplier * (profile.HasUpgrade("engine_torque_1") ? 1.12f : 1f);
            tuning.rigidbodyFallbackDriveForce *= vehicle.engineMultiplier;
            tuning.steeringAngle *= vehicle.steeringMultiplier * t.steering;
            tuning.steeringAngleAtSpeed *= Mathf.Lerp(0.82f, 1.22f, Mathf.InverseLerp(0.45f, 1.85f, t.steering));
            tuning.arcadeYawAssist *= vehicle.steeringMultiplier * t.suspension;
            tuning.finalDrive *= t.finalDrive;
            tuning.brakeTorque *= t.brakeBias;
            tuning.normalSideGrip *= vehicle.gripMultiplier * t.tireGrip * (profile.HasUpgrade("grip_tires_1") ? 1.12f : 1f);
            tuning.driftSideGrip *= t.driftGrip;
            tuning.handbrakeRearGrip *= vehicle.isBike ? Mathf.Lerp(0.72f, 1.08f, t.rearBrakeSlide) : Mathf.Lerp(0.88f, 1.06f, t.driftGrip);
            tuning.clutchTransferSharpness *= t.clutchBite * (profile.HasUpgrade("clutch_response_1") ? 1.18f : 1f);
            tuning.clutchKickSlipBoost *= Mathf.Lerp(0.82f, 1.28f, Mathf.InverseLerp(0.55f, 1.8f, t.clutchBite));
            tuning.boostTorqueMultiplier = 1f + (tuning.boostTorqueMultiplier - 1f) * vehicle.boostMultiplier * t.boostValve * (profile.HasUpgrade("boost_valve_1") ? 1.14f : 1f);
            tuning.boostBurnPerSecond *= Mathf.Lerp(1.12f, 0.86f, Mathf.InverseLerp(0.45f, 1.85f, t.boostValve));
            tuning.downforce *= t.suspension;

            if (profile.HasUpgrade("combat_plating_1"))
            {
                tuning.mass *= 1.06f;
            }

            if (profile.HasUpgrade("lightweight_aero_1"))
            {
                tuning.mass *= 0.96f;
                tuning.downforce *= 1.12f;
                tuning.finalDrive *= 0.96f;
            }

            if (vehicle.isBike)
            {
                tuning.mass *= profile.HasUpgrade("lightweight_frame_1") ? 0.94f : 1f;
                tuning.steeringAngle *= profile.HasUpgrade("lean_stabilizer_1") ? 1.06f : 1f;
                tuning.normalSideGrip *= profile.HasUpgrade("razor_tires_1") ? 1.12f : 1f;
                tuning.driftSideGrip *= profile.HasUpgrade("razor_tires_1") ? 1.12f : 1f;
                tuning.boostTorqueMultiplier *= profile.HasUpgrade("boost_tuck_1") ? 1.08f : 1f;
                tuning.centerOfMassOffset = new Vector3(0f, -0.66f, 0.08f);
                tuning.handbrakeTorque *= 0.76f;
            }
        }

        public static float RamMultiplier(VectorSSVehicleDefinition vehicle, VectorSSPlayerProfile profile)
        {
            float multiplier = vehicle != null ? vehicle.ramMultiplier : 1f;
            if (profile != null && profile.HasUpgrade("combat_plating_1"))
            {
                multiplier *= 1.18f;
            }

            if (profile != null && profile.HasUpgrade("boost_valve_1"))
            {
                multiplier *= 1.05f;
            }

            return multiplier;
        }

        public static VectorSSRaceResult BuildRaceResult(VectorSSMapDefinition map, VectorSSVehicleDefinition vehicle, float raceTime, float flow01, int combatScore)
        {
            int style = Mathf.RoundToInt(Mathf.Clamp01(flow01) * 34f);
            int combat = Mathf.Clamp(combatScore, 0, 24);
            return new VectorSSRaceResult
            {
                map = map,
                vehicle = vehicle,
                raceTime = raceTime,
                flow01 = flow01,
                combatScore = combatScore,
                completionReward = map.baseReward,
                styleReward = new VectorSSResources(6 + combat, 8 + style / 2, 8 + style),
                mapReward = map.mapBonus
            };
        }
    }
}
