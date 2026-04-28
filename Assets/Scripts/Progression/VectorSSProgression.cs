using System;
using System.Collections.Generic;
using System.Globalization;
using GTX.Data;
using GTX.Visuals;
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
        Paused,
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
        Razor,
        Hauler
    }

    public enum VectorSSVehicleClass
    {
        Strike,
        Drift,
        Volt,
        Bike,
        Pickup
    }

    public enum VectorSSModuleCategory
    {
        Sensor,
        ActiveControl,
        Combat,
        Utility
    }

    public enum VectorSSModuleSlot
    {
        Sensor,
        Control,
        Combat,
        Utility
    }

    public enum VectorSSModuleWidget
    {
        None,
        HeatGauge,
        ClutchKick,
        BoostValve,
        BrakeBias,
        DifferentialLock,
        ArmorPlate,
        SnapLean,
        RearBrakeSlide
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
        public bool automaticTransmission;

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
            automaticTransmission = other.automaticTransmission;
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
        public int sensorSlots = 1;
        public int controlSlots = 2;
        public int combatSlots = 1;
        public int utilitySlots = 1;

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

        public int SlotCapacity(VectorSSModuleSlot slot)
        {
            switch (slot)
            {
                case VectorSSModuleSlot.Sensor:
                    return sensorSlots;
                case VectorSSModuleSlot.Control:
                    return controlSlots;
                case VectorSSModuleSlot.Combat:
                    return combatSlots;
                case VectorSSModuleSlot.Utility:
                    return utilitySlots;
                default:
                    return 0;
            }
        }
    }

    public sealed class VectorSSMapDefinition
    {
        public VectorSSMapId id;
        public string displayName;
        public string theme;
        public string purpose;
        public int lapCount = 3;
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

    public sealed class VectorSSModuleDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public VectorSSModuleCategory category;
        public VectorSSModuleSlot slot;
        public VectorSSModuleWidget widget;
        public VectorSSResources cost;
        public VectorSSVehicleId? allowedVehicle;
        public VectorSSVehicleClass? allowedClass;
        public string controlHint;
        public Vector2 defaultHudPosition = new Vector2(1488f, -210f);
        public float defaultHudScale = 0.9f;

        public bool Supports(VectorSSVehicleDefinition vehicle)
        {
            if (vehicle == null)
            {
                return false;
            }

            if (allowedVehicle != null && allowedVehicle.Value != vehicle.id)
            {
                return false;
            }

            if (allowedClass != null && allowedClass.Value != vehicle.vehicleClass)
            {
                return false;
            }

            return true;
        }
    }

    public sealed class VectorSSModuleHudLayout
    {
        public VectorSSVehicleId vehicleId;
        public string moduleId;
        public Vector2 position;
        public float scale = 0.9f;
        public bool visible = true;

        public void ResetToDefinition(VectorSSVehicleId vehicle, VectorSSModuleDefinition module)
        {
            vehicleId = vehicle;
            moduleId = module != null ? module.id : moduleId;
            position = module != null ? module.defaultHudPosition : new Vector2(1488f, -210f);
            scale = module != null ? module.defaultHudScale : 0.9f;
            visible = true;
        }
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
        public readonly HashSet<string> purchasedModules = new HashSet<string>();
        public readonly Dictionary<VectorSSVehicleId, HashSet<string>> installedModules = new Dictionary<VectorSSVehicleId, HashSet<string>>();
        public readonly Dictionary<string, VectorSSModuleHudLayout> moduleHudLayouts = new Dictionary<string, VectorSSModuleHudLayout>();

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

        public bool HasModule(string id)
        {
            return purchasedModules.Contains(id);
        }

        public bool IsModuleInstalled(VectorSSVehicleId vehicle, string moduleId)
        {
            HashSet<string> modules;
            return installedModules.TryGetValue(vehicle, out modules) && modules.Contains(moduleId);
        }

        public HashSet<string> InstalledModulesFor(VectorSSVehicleId vehicle, bool createIfMissing)
        {
            HashSet<string> modules;
            if (installedModules.TryGetValue(vehicle, out modules))
            {
                return modules;
            }

            if (!createIfMissing)
            {
                return null;
            }

            modules = new HashSet<string>();
            installedModules.Add(vehicle, modules);
            return modules;
        }

        public int InstalledSlotCount(VectorSSVehicleId vehicle, VectorSSModuleSlot slot)
        {
            HashSet<string> modules = InstalledModulesFor(vehicle, false);
            if (modules == null)
            {
                return 0;
            }

            int count = 0;
            foreach (string moduleId in modules)
            {
                VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                if (module != null && module.slot == slot)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryPurchaseModule(VectorSSModuleDefinition module)
        {
            if (module == null || HasModule(module.id) || !resources.TrySpend(module.cost))
            {
                return false;
            }

            purchasedModules.Add(module.id);
            return true;
        }

        public bool TryInstallModule(VectorSSVehicleDefinition vehicle, VectorSSModuleDefinition module, out string message)
        {
            message = string.Empty;
            if (vehicle == null || module == null)
            {
                message = "Missing vehicle or module.";
                return false;
            }

            if (!HasModule(module.id))
            {
                message = "Purchase this module first.";
                return false;
            }

            if (!module.Supports(vehicle))
            {
                message = module.displayName + " is not supported by " + vehicle.displayName + ".";
                return false;
            }

            HashSet<string> modules = InstalledModulesFor(vehicle.id, true);
            if (modules.Contains(module.id))
            {
                message = module.displayName + " is already installed.";
                return false;
            }

            int capacity = vehicle.SlotCapacity(module.slot);
            if (InstalledSlotCount(vehicle.id, module.slot) >= capacity)
            {
                message = vehicle.displayName + " has no open " + module.slot + " slot.";
                return false;
            }

            modules.Add(module.id);
            GetModuleLayout(vehicle.id, module, true);
            message = "Installed " + module.displayName + ".";
            return true;
        }

        public void UninstallModule(VectorSSVehicleId vehicle, string moduleId)
        {
            HashSet<string> modules = InstalledModulesFor(vehicle, false);
            if (modules != null)
            {
                modules.Remove(moduleId);
            }
        }

        public VectorSSModuleHudLayout GetModuleLayout(VectorSSVehicleId vehicle, VectorSSModuleDefinition module, bool createIfMissing)
        {
            if (module == null)
            {
                return null;
            }

            string key = ModuleLayoutKey(vehicle, module.id);
            VectorSSModuleHudLayout layout;
            if (moduleHudLayouts.TryGetValue(key, out layout))
            {
                return layout;
            }

            if (!createIfMissing)
            {
                return null;
            }

            layout = new VectorSSModuleHudLayout();
            layout.ResetToDefinition(vehicle, module);
            moduleHudLayouts.Add(key, layout);
            return layout;
        }

        public static string ModuleLayoutKey(VectorSSVehicleId vehicle, string moduleId)
        {
            return vehicle + ":" + moduleId;
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
                bodyColor = VectrStyleTokens.VehicleBody(VectorSSVehicleId.Hammer),
                accentColor = VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Hammer),
                secondaryColor = VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Hammer),
                massMultiplier = 1.24f,
                engineMultiplier = 1.05f,
                steeringMultiplier = 0.82f,
                gripMultiplier = 1.08f,
                boostMultiplier = 0.96f,
                ramMultiplier = 1.48f,
                impactResistance = 1.38f,
                nearMissFlowMultiplier = 0.8f,
                visualScale = new Vector3(1.12f, 1f, 1.12f),
                colliderSize = new Vector3(2.45f, 1.02f, 4.82f),
                sensorSlots = 1,
                controlSlots = 2,
                combatSlots = 2,
                utilitySlots = 1
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Needle,
                displayName = "Needle",
                fullName = "Vector SS-D \"Needle\"",
                vehicleClass = VectorSSVehicleClass.Drift,
                role = "Technical cornering / clutch kicks / Flow",
                primaryResource = "Rubber",
                bodyColor = VectrStyleTokens.VehicleBody(VectorSSVehicleId.Needle),
                accentColor = VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Needle),
                secondaryColor = VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Needle),
                massMultiplier = 0.88f,
                engineMultiplier = 0.98f,
                steeringMultiplier = 1.38f,
                gripMultiplier = 0.9f,
                boostMultiplier = 1.03f,
                ramMultiplier = 0.76f,
                impactResistance = 0.82f,
                nearMissFlowMultiplier = 1.14f,
                visualScale = new Vector3(0.9f, 0.92f, 1.08f),
                colliderSize = new Vector3(2.0f, 0.86f, 4.48f),
                sensorSlots = 2,
                controlSlots = 3,
                combatSlots = 1,
                utilitySlots = 1
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Surge,
                displayName = "Surge",
                fullName = "Vector SS-V \"Surge\"",
                vehicleClass = VectorSSVehicleClass.Volt,
                role = "Boost / speed / heat risk",
                primaryResource = "Plastic + Metal",
                bodyColor = VectrStyleTokens.VehicleBody(VectorSSVehicleId.Surge),
                accentColor = VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Surge),
                secondaryColor = VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Surge),
                massMultiplier = 0.94f,
                engineMultiplier = 1.12f,
                steeringMultiplier = 1.04f,
                gripMultiplier = 0.94f,
                boostMultiplier = 1.52f,
                ramMultiplier = 0.88f,
                impactResistance = 0.9f,
                nearMissFlowMultiplier = 1.02f,
                visualScale = new Vector3(0.98f, 0.96f, 1.04f),
                colliderSize = new Vector3(2.15f, 0.9f, 4.42f),
                sensorSlots = 3,
                controlSlots = 2,
                combatSlots = 1,
                utilitySlots = 1
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Razor,
                displayName = "Razor",
                fullName = "Vector SS-B \"Razor\"",
                vehicleClass = VectorSSVehicleClass.Bike,
                role = "Agility / near-misses / shortcuts / high-risk precision",
                primaryResource = "Rubber + Plastic",
                bodyColor = VectrStyleTokens.VehicleBody(VectorSSVehicleId.Razor),
                accentColor = VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Razor),
                secondaryColor = VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Razor),
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
                isBike = true,
                sensorSlots = 2,
                controlSlots = 3,
                combatSlots = 1,
                utilitySlots = 2
            },
            new VectorSSVehicleDefinition
            {
                id = VectorSSVehicleId.Hauler,
                displayName = "Hauler",
                fullName = "Vector SS-P \"Hauler\"",
                vehicleClass = VectorSSVehicleClass.Pickup,
                role = "Pickup truck / utility combat / stable exits",
                primaryResource = "Metal + Rubber",
                bodyColor = VectrStyleTokens.VehicleBody(VectorSSVehicleId.Hauler),
                accentColor = VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Hauler),
                secondaryColor = VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Hauler),
                massMultiplier = 1.18f,
                engineMultiplier = 1.03f,
                steeringMultiplier = 0.9f,
                gripMultiplier = 1.12f,
                boostMultiplier = 0.9f,
                ramMultiplier = 1.22f,
                impactResistance = 1.26f,
                nearMissFlowMultiplier = 0.92f,
                airControlMultiplier = 0.9f,
                visualScale = new Vector3(1.1f, 1.08f, 1.18f),
                colliderSize = new Vector3(2.38f, 1.08f, 5.12f),
                colliderCenter = new Vector3(0f, 0.76f, -0.08f),
                sensorSlots = 2,
                controlSlots = 2,
                combatSlots = 2,
                utilitySlots = 1
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
                lapCount = 3,
                baseReward = new VectorSSResources(32, 30, 30),
                mapBonus = new VectorSSResources(14, 12, 12),
                roadColor = VectrStyleTokens.MapRoad(VectorSSMapId.BlacklineCircuit),
                groundColor = VectrStyleTokens.MapGround(VectorSSMapId.BlacklineCircuit),
                barrierColor = VectrStyleTokens.MapBarrier(VectorSSMapId.BlacklineCircuit)
            },
            new VectorSSMapDefinition
            {
                id = VectorSSMapId.ScraplineYard,
                displayName = "Scrapline Yard",
                purpose = "Combat-focused industrial map",
                theme = "Containers, cranes, scrap props, and wide lanes.",
                lapCount = 3,
                baseReward = new VectorSSResources(36, 22, 22),
                mapBonus = new VectorSSResources(36, 8, 8),
                roadColor = VectrStyleTokens.MapRoad(VectorSSMapId.ScraplineYard),
                groundColor = VectrStyleTokens.MapGround(VectorSSMapId.ScraplineYard),
                barrierColor = VectrStyleTokens.MapBarrier(VectorSSMapId.ScraplineYard)
            },
            new VectorSSMapDefinition
            {
                id = VectorSSMapId.RubberRidge,
                displayName = "Rubber Ridge",
                purpose = "Drift/jump mountain map",
                theme = "Canyon hairpins, tire walls, jumps, and a narrow bike line.",
                lapCount = 3,
                baseReward = new VectorSSResources(24, 24, 38),
                mapBonus = new VectorSSResources(8, 10, 38),
                roadColor = VectrStyleTokens.MapRoad(VectorSSMapId.RubberRidge),
                groundColor = VectrStyleTokens.MapGround(VectorSSMapId.RubberRidge),
                barrierColor = VectrStyleTokens.MapBarrier(VectorSSMapId.RubberRidge)
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

        public static readonly VectorSSModuleDefinition[] Modules =
        {
            new VectorSSModuleDefinition
            {
                id = "heat_gauge",
                displayName = "Heat Gauge",
                description = "Adds a precise boost heat readout to the cockpit.",
                category = VectorSSModuleCategory.Sensor,
                slot = VectorSSModuleSlot.Sensor,
                widget = VectorSSModuleWidget.HeatGauge,
                cost = new VectorSSResources(10, 25, 0),
                controlHint = string.Empty,
                defaultHudPosition = new Vector2(1488f, -210f)
            },
            new VectorSSModuleDefinition
            {
                id = "clutch_kick_assist",
                displayName = "Clutch Kick Assist",
                description = "Adds an X input that snaps the drivetrain into a drift entry without replacing manual clutch skill.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Control,
                widget = VectorSSModuleWidget.ClutchKick,
                cost = new VectorSSResources(25, 0, 45),
                controlHint = "X / L3 clutch kick",
                defaultHudPosition = new Vector2(1488f, -268f)
            },
            new VectorSSModuleDefinition
            {
                id = "boost_valve_lever",
                displayName = "Boost Valve Lever",
                description = "Adds a V toggle for Low / Medium / High boost pressure.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Control,
                widget = VectorSSModuleWidget.BoostValve,
                cost = new VectorSSResources(25, 50, 0),
                controlHint = "V / D-pad up boost valve",
                defaultHudPosition = new Vector2(1488f, -326f)
            },
            new VectorSSModuleDefinition
            {
                id = "brake_bias_dial",
                displayName = "Brake Bias Dial",
                description = "Adds [ and ] controls for front/rear brake balance.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Control,
                widget = VectorSSModuleWidget.BrakeBias,
                cost = new VectorSSResources(0, 25, 30),
                controlHint = "[/] / D-pad left-right brake bias",
                defaultHudPosition = new Vector2(1488f, -384f)
            },
            new VectorSSModuleDefinition
            {
                id = "differential_lock_switch",
                displayName = "Differential Lock Switch",
                description = "Adds a G toggle for traction and ram stability at the cost of rotation.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Control,
                widget = VectorSSModuleWidget.DifferentialLock,
                cost = new VectorSSResources(60, 0, 20),
                controlHint = "G / Start diff lock",
                defaultHudPosition = new Vector2(1488f, -442f)
            },
            new VectorSSModuleDefinition
            {
                id = "armor_plate_deploy",
                displayName = "Armor Plate Deploy",
                description = "Adds a B control that briefly hardens the frame for impacts.",
                category = VectorSSModuleCategory.Combat,
                slot = VectorSSModuleSlot.Combat,
                widget = VectorSSModuleWidget.ArmorPlate,
                cost = new VectorSSResources(80, 20, 0),
                controlHint = "B / B-button armor plates",
                defaultHudPosition = new Vector2(1488f, -500f)
            },
            new VectorSSModuleDefinition
            {
                id = "snap_lean_module",
                displayName = "Snap Lean Module",
                description = "Razor-only evasive lean burst for near-miss lines.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Utility,
                widget = VectorSSModuleWidget.SnapLean,
                cost = new VectorSSResources(0, 35, 45),
                allowedVehicle = VectorSSVehicleId.Razor,
                controlHint = "Left Alt / R3 snap lean",
                defaultHudPosition = new Vector2(1488f, -558f)
            },
            new VectorSSModuleDefinition
            {
                id = "rear_brake_slide_controller",
                displayName = "Rear Brake Slide Controller",
                description = "Razor-only rear slide tuning and readout for tight shortcut entries.",
                category = VectorSSModuleCategory.ActiveControl,
                slot = VectorSSModuleSlot.Utility,
                widget = VectorSSModuleWidget.RearBrakeSlide,
                cost = new VectorSSResources(20, 0, 50),
                allowedVehicle = VectorSSVehicleId.Razor,
                controlHint = "Space / A-button rear slide",
                defaultHudPosition = new Vector2(1488f, -616f)
            }
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

        public static VectorSSModuleDefinition GetModule(string id)
        {
            for (int i = 0; i < Modules.Length; i++)
            {
                if (Modules[i].id == id)
                {
                    return Modules[i];
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

            LoadModules(profile);
            LoadModuleLayouts(profile);
            NormalizeModules(profile);
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
            SaveModules(profile);
            SaveModuleLayouts(profile);
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
            tuning.automaticTransmission = PlayerPrefs.GetInt(Prefix + "Tune.AutomaticTransmission", tuning.automaticTransmission ? 1 : 0) == 1;
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
            PlayerPrefs.SetInt(Prefix + "Tune.AutomaticTransmission", tuning.automaticTransmission ? 1 : 0);
        }

        private static void LoadModules(VectorSSPlayerProfile profile)
        {
            string rawPurchased = PlayerPrefs.GetString(Prefix + "Modules.Purchased", string.Empty);
            if (!string.IsNullOrEmpty(rawPurchased))
            {
                string[] ids = rawPurchased.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < ids.Length; i++)
                {
                    if (VectorSSCatalog.GetModule(ids[i]) != null)
                    {
                        profile.purchasedModules.Add(ids[i]);
                    }
                }
            }

            string rawInstalled = PlayerPrefs.GetString(Prefix + "Modules.Installed", string.Empty);
            if (string.IsNullOrEmpty(rawInstalled))
            {
                return;
            }

            string[] vehicles = rawInstalled.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < vehicles.Length; i++)
            {
                string[] pair = vehicles[i].Split(new[] { '=' }, 2);
                if (pair.Length != 2)
                {
                    continue;
                }

                VectorSSVehicleId vehicleId;
                if (!Enum.TryParse(pair[0], out vehicleId))
                {
                    continue;
                }

                HashSet<string> modules = profile.InstalledModulesFor(vehicleId, true);
                string[] ids = pair[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int moduleIndex = 0; moduleIndex < ids.Length; moduleIndex++)
                {
                    if (VectorSSCatalog.GetModule(ids[moduleIndex]) != null)
                    {
                        modules.Add(ids[moduleIndex]);
                    }
                }
            }
        }

        private static void SaveModules(VectorSSPlayerProfile profile)
        {
            PlayerPrefs.SetString(Prefix + "Modules.Purchased", string.Join("|", new List<string>(profile.purchasedModules).ToArray()));

            List<string> vehicleEntries = new List<string>();
            VectorSSVehicleId[] vehicles = (VectorSSVehicleId[])Enum.GetValues(typeof(VectorSSVehicleId));
            for (int i = 0; i < vehicles.Length; i++)
            {
                HashSet<string> modules = profile.InstalledModulesFor(vehicles[i], false);
                if (modules == null || modules.Count == 0)
                {
                    continue;
                }

                vehicleEntries.Add(vehicles[i] + "=" + string.Join(",", new List<string>(modules).ToArray()));
            }

            PlayerPrefs.SetString(Prefix + "Modules.Installed", string.Join(";", vehicleEntries.ToArray()));
        }

        private static void LoadModuleLayouts(VectorSSPlayerProfile profile)
        {
            string rawLayouts = PlayerPrefs.GetString(Prefix + "Modules.Layouts", string.Empty);
            if (string.IsNullOrEmpty(rawLayouts))
            {
                return;
            }

            string[] entries = rawLayouts.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split(':');
                if (parts.Length != 6)
                {
                    continue;
                }

                VectorSSVehicleId vehicleId;
                if (!Enum.TryParse(parts[0], out vehicleId))
                {
                    continue;
                }

                string moduleId = parts[1];
                if (VectorSSCatalog.GetModule(moduleId) == null)
                {
                    continue;
                }

                float x;
                float y;
                float scale;
                bool visible;
                if (!TryParseFloat(parts[2], out x) || !TryParseFloat(parts[3], out y) || !TryParseFloat(parts[4], out scale) || !bool.TryParse(parts[5], out visible))
                {
                    continue;
                }

                profile.moduleHudLayouts[VectorSSPlayerProfile.ModuleLayoutKey(vehicleId, moduleId)] = new VectorSSModuleHudLayout
                {
                    vehicleId = vehicleId,
                    moduleId = moduleId,
                    position = new Vector2(x, y),
                    scale = Mathf.Clamp(scale, 0.55f, 1.35f),
                    visible = visible
                };
            }
        }

        private static void SaveModuleLayouts(VectorSSPlayerProfile profile)
        {
            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, VectorSSModuleHudLayout> pair in profile.moduleHudLayouts)
            {
                VectorSSModuleHudLayout layout = pair.Value;
                if (layout == null || string.IsNullOrEmpty(layout.moduleId))
                {
                    continue;
                }

                entries.Add(layout.vehicleId + ":" + layout.moduleId + ":" +
                    FormatFloat(layout.position.x) + ":" +
                    FormatFloat(layout.position.y) + ":" +
                    FormatFloat(layout.scale) + ":" +
                    layout.visible);
            }

            PlayerPrefs.SetString(Prefix + "Modules.Layouts", string.Join("|", entries.ToArray()));
        }

        private static void NormalizeModules(VectorSSPlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            VectorSSVehicleId[] vehicles = (VectorSSVehicleId[])Enum.GetValues(typeof(VectorSSVehicleId));
            for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
            {
                VectorSSVehicleDefinition vehicle = VectorSSCatalog.GetVehicle(vehicles[vehicleIndex]);
                HashSet<string> modules = profile.InstalledModulesFor(vehicles[vehicleIndex], false);
                if (modules == null)
                {
                    continue;
                }

                List<string> invalid = new List<string>();
                Dictionary<VectorSSModuleSlot, int> slotUse = new Dictionary<VectorSSModuleSlot, int>();
                foreach (string moduleId in modules)
                {
                    VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                    if (module == null || !profile.HasModule(moduleId) || !module.Supports(vehicle))
                    {
                        invalid.Add(moduleId);
                        continue;
                    }

                    int used = slotUse.ContainsKey(module.slot) ? slotUse[module.slot] : 0;
                    if (used >= vehicle.SlotCapacity(module.slot))
                    {
                        invalid.Add(moduleId);
                        continue;
                    }

                    slotUse[module.slot] = used + 1;
                }

                for (int i = 0; i < invalid.Count; i++)
                {
                    modules.Remove(invalid[i]);
                }
            }
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
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
            float steering01 = Mathf.InverseLerp(0.45f, 1.85f, t.steering);
            tuning.arcadeYawAssist *= Mathf.Lerp(0.85f, 1.12f, steering01) * Mathf.Lerp(0.95f, 1.05f, Mathf.InverseLerp(0.55f, 1.65f, t.suspension));
            tuning.steeringInputRiseRate *= Mathf.Lerp(0.86f, 1.36f, steering01) * Mathf.Lerp(0.92f, 1.12f, vehicle.steeringMultiplier - 0.82f);
            tuning.steeringInputFallRate *= Mathf.Lerp(0.94f, 1.28f, steering01);
            tuning.lowSpeedSteeringAssist *= Mathf.Lerp(0.84f, 1.22f, steering01);
            tuning.highSpeedSteeringStability *= Mathf.Lerp(1.08f, 0.88f, steering01) * Mathf.Lerp(1.18f, 0.92f, vehicle.steeringMultiplier - 0.82f);
            tuning.finalDrive *= t.finalDrive;
            tuning.brakeTorque *= t.brakeBias;
            tuning.normalSideGrip *= vehicle.gripMultiplier * t.tireGrip * (profile.HasUpgrade("grip_tires_1") ? 1.12f : 1f);
            tuning.driftSideGrip *= t.driftGrip;
            tuning.handbrakeRearGrip *= vehicle.isBike ? Mathf.Lerp(0.72f, 1.08f, t.rearBrakeSlide) : Mathf.Lerp(0.88f, 1.06f, t.driftGrip);
            float driftGrip01 = Mathf.InverseLerp(0.45f, 1.85f, t.driftGrip);
            tuning.driftSustain *= Mathf.Lerp(1.24f, 0.86f, driftGrip01);
            tuning.driftExitRecovery *= Mathf.Lerp(0.82f, 1.26f, driftGrip01) * Mathf.Lerp(0.94f, 1.16f, Mathf.InverseLerp(0.55f, 1.65f, t.suspension));
            tuning.driftExitYawDamping *= Mathf.Lerp(0.92f, 1.24f, driftGrip01) * Mathf.Lerp(0.96f, 1.16f, Mathf.InverseLerp(0.55f, 1.65f, t.suspension));
            tuning.driftExitHoldSeconds *= Mathf.Lerp(1.08f, 0.88f, driftGrip01);
            tuning.driftLateralDamping *= Mathf.Lerp(0.86f, 1.2f, driftGrip01);
            tuning.driftHandbrakeEntryKick *= Mathf.Lerp(1.16f, 0.92f, driftGrip01);
            tuning.driftHandbrakeEntryDuration *= Mathf.Lerp(1.12f, 0.94f, driftGrip01);
            tuning.driftExitBoostForce *= Mathf.Lerp(0.94f, 1.18f, driftGrip01) * (profile.HasUpgrade("grip_tires_1") ? 1.1f : 1f);
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

            ApplyVehicleFeelIdentity(tuning, vehicle);
        }

        private static void ApplyVehicleFeelIdentity(VehicleTuning tuning, VectorSSVehicleDefinition vehicle)
        {
            switch (vehicle.id)
            {
                case VectorSSVehicleId.Hammer:
                    tuning.highSpeedSteeringStability *= 1.28f;
                    tuning.driftSustain *= 0.62f;
                    tuning.driftExitRecovery *= 1.35f;
                    tuning.driftExitYawDamping *= 1.28f;
                    tuning.driftLateralDamping *= 1.16f;
                    tuning.driftHandbrakeEntryKick *= 0.72f;
                    break;
                case VectorSSVehicleId.Needle:
                    tuning.highSpeedSteeringStability *= 0.84f;
                    tuning.driftSustain *= 1.08f;
                    tuning.driftExitRecovery *= 0.92f;
                    tuning.driftExitYawDamping *= 0.92f;
                    tuning.driftHandbrakeEntryKick *= 1.08f;
                    tuning.driftHandbrakeEntryDuration *= 1.05f;
                    tuning.driftThrottleInfluence *= 1.08f;
                    tuning.driftExitBoostForce *= 1.16f;
                    break;
                case VectorSSVehicleId.Surge:
                    tuning.highSpeedSteeringStability *= 1.36f;
                    tuning.driftSustain *= 0.72f;
                    tuning.driftExitYawDamping *= 1.18f;
                    tuning.driftLateralDamping *= 1.18f;
                    tuning.driftHandbrakeEntryKick *= 0.82f;
                    break;
                case VectorSSVehicleId.Razor:
                    tuning.highSpeedSteeringStability *= 1.05f;
                    tuning.driftSustain *= 0.84f;
                    tuning.driftExitYawDamping *= 1.1f;
                    tuning.driftLateralDamping *= 1.14f;
                    tuning.driftHandbrakeEntryKick *= 1.08f;
                    tuning.driftHandbrakeEntryDuration *= 0.9f;
                    tuning.driftThrottleInfluence *= 0.82f;
                    break;
                case VectorSSVehicleId.Hauler:
                    tuning.highSpeedSteeringStability *= 1.22f;
                    tuning.driftSustain *= 0.78f;
                    tuning.driftExitRecovery *= 1.18f;
                    tuning.driftExitYawDamping *= 1.16f;
                    tuning.driftLateralDamping *= 1.12f;
                    tuning.driftHandbrakeEntryKick *= 0.9f;
                    tuning.driftThrottleInfluence *= 0.92f;
                    break;
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
