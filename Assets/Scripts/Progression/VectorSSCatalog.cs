using System.Collections.Generic;
using UnityEngine;

namespace GTX.Progression
{
    public static class VectorSSGarageCatalog
    {
        private static readonly VectorSSGarageVehicleDefinition[] VehicleDefinitions =
        {
            new VectorSSGarageVehicleDefinition
            {
                id = VectorSSVehicleId.Hammer,
                displayName = "Hammer",
                kind = VectorSSVehicleKind.Car,
                description = "Heavy contact car with stable exits and strong ram scaling.",
                startingUnlocked = true,
                unlockCost = VectorSSResourceWallet.Zero(),
                rewardMultiplier = 1f,
                baseStats = new VectorSSTuningStats
                {
                    mass = 1.16f,
                    acceleration = 1.05f,
                    topSpeed = 0.98f,
                    grip = 1.06f,
                    steering = 0.92f,
                    brake = 1.04f,
                    boostCapacity = 0.96f,
                    boostPower = 0.98f,
                    cooling = 1.08f,
                    heat = 0.96f,
                    downforce = 1.1f,
                    ram = 1.35f,
                    drift = 0.92f,
                    stability = 1.16f,
                    centerOfMassOffset = new Vector3(0f, -0.04f, 0.03f)
                }
            },
            new VectorSSGarageVehicleDefinition
            {
                id = VectorSSVehicleId.Needle,
                displayName = "Needle",
                kind = VectorSSVehicleKind.Car,
                description = "Light precision car built for late braking and narrow racing lines.",
                startingUnlocked = false,
                unlockCost = new VectorSSResourceWallet(110, 85, 70),
                rewardMultiplier = 1.05f,
                baseStats = new VectorSSTuningStats
                {
                    mass = 0.88f,
                    acceleration = 1.02f,
                    topSpeed = 1.08f,
                    grip = 1.12f,
                    steering = 1.2f,
                    brake = 1.12f,
                    boostCapacity = 0.92f,
                    boostPower = 1f,
                    cooling = 1f,
                    heat = 1f,
                    downforce = 1.05f,
                    ram = 0.78f,
                    drift = 1.04f,
                    stability = 0.96f,
                    centerOfMassOffset = new Vector3(0f, -0.02f, -0.02f)
                }
            },
            new VectorSSGarageVehicleDefinition
            {
                id = VectorSSVehicleId.Surge,
                displayName = "Surge",
                kind = VectorSSVehicleKind.Car,
                description = "Boost-forward car with higher heat risk and faster recovery windows.",
                startingUnlocked = false,
                unlockCost = new VectorSSResourceWallet(95, 120, 80),
                rewardMultiplier = 1.08f,
                baseStats = new VectorSSTuningStats
                {
                    mass = 0.96f,
                    acceleration = 1.12f,
                    topSpeed = 1.04f,
                    grip = 0.98f,
                    steering = 1.05f,
                    brake = 0.96f,
                    boostCapacity = 1.22f,
                    boostPower = 1.28f,
                    cooling = 0.88f,
                    heat = 1.18f,
                    downforce = 0.98f,
                    ram = 0.94f,
                    drift = 1.02f,
                    stability = 0.98f,
                    centerOfMassOffset = new Vector3(0f, -0.01f, 0.01f)
                }
            },
            new VectorSSGarageVehicleDefinition
            {
                id = VectorSSVehicleId.Razor,
                displayName = "Razor",
                kind = VectorSSVehicleKind.Bike,
                description = "Razor bike platform: low mass, fast lean response, fragile contact game.",
                startingUnlocked = false,
                unlockCost = new VectorSSResourceWallet(80, 95, 135),
                rewardMultiplier = 1.12f,
                baseStats = new VectorSSTuningStats
                {
                    mass = 0.54f,
                    acceleration = 1.1f,
                    topSpeed = 1.06f,
                    grip = 1.08f,
                    steering = 1.34f,
                    brake = 1.02f,
                    boostCapacity = 0.84f,
                    boostPower = 1.1f,
                    cooling = 1.02f,
                    heat = 1.04f,
                    downforce = 0.72f,
                    ram = 0.48f,
                    drift = 1.14f,
                    stability = 0.82f,
                    centerOfMassOffset = new Vector3(0f, -0.16f, -0.08f)
                }
            },
            new VectorSSGarageVehicleDefinition
            {
                id = VectorSSVehicleId.Hauler,
                displayName = "Hauler",
                kind = VectorSSVehicleKind.Car,
                description = "Low-poly pickup truck: utility contact platform with stable exits and heavy bed armor.",
                startingUnlocked = true,
                unlockCost = new VectorSSResourceWallet(125, 60, 95),
                rewardMultiplier = 1.06f,
                baseStats = new VectorSSTuningStats
                {
                    mass = 1.18f,
                    acceleration = 0.98f,
                    topSpeed = 0.95f,
                    grip = 1.12f,
                    steering = 0.9f,
                    brake = 1.1f,
                    boostCapacity = 0.94f,
                    boostPower = 0.92f,
                    cooling = 1.08f,
                    heat = 0.94f,
                    downforce = 1.04f,
                    ram = 1.22f,
                    drift = 0.9f,
                    stability = 1.18f,
                    centerOfMassOffset = new Vector3(0f, -0.08f, -0.1f)
                }
            }
        };

        private static readonly VectorSSGarageMapDefinition[] MapDefinitions =
        {
            new VectorSSGarageMapDefinition
            {
                id = VectorSSMapId.BlacklineCircuit,
                displayName = "Blackline Circuit",
                description = "Balanced starter loop with clean asphalt, readable turns, and even material payouts.",
                lapCount = 3,
                difficulty = 1f,
                startingUnlocked = true,
                unlockCost = VectorSSResourceWallet.Zero(),
                baseReward = new VectorSSResourceWallet(34, 28, 28),
                firstClearBonus = new VectorSSResourceWallet(35, 35, 35),
                baseXp = 100,
                hasFirstWinUnlock = true,
                firstWinUnlocksMap = VectorSSMapId.ScraplineYard
            },
            new VectorSSGarageMapDefinition
            {
                id = VectorSSMapId.ScraplineYard,
                displayName = "Scrapline Yard",
                description = "Industrial course with tight gates and a Metal/Plastic reward bias.",
                lapCount = 3,
                difficulty = 1.22f,
                startingUnlocked = false,
                unlockCost = new VectorSSResourceWallet(80, 55, 25),
                baseReward = new VectorSSResourceWallet(54, 42, 24),
                firstClearBonus = new VectorSSResourceWallet(80, 50, 30),
                baseXp = 125,
                hasFirstWinUnlock = true,
                firstWinUnlocksMap = VectorSSMapId.RubberRidge
            },
            new VectorSSGarageMapDefinition
            {
                id = VectorSSMapId.RubberRidge,
                displayName = "Rubber Ridge",
                description = "Ridge road with long sweepers, tire wear fantasy, and Rubber-heavy payouts.",
                lapCount = 4,
                difficulty = 1.35f,
                startingUnlocked = false,
                unlockCost = new VectorSSResourceWallet(70, 50, 120),
                baseReward = new VectorSSResourceWallet(36, 30, 66),
                firstClearBonus = new VectorSSResourceWallet(45, 45, 115),
                baseXp = 145,
                hasFirstWinUnlock = false
            },
            new VectorSSGarageMapDefinition
            {
                id = VectorSSMapId.SpecialStage,
                displayName = "Special Stage",
                description = "Wide-open city test course with broad streets, plazas, and buildings to drift around.",
                lapCount = 3,
                difficulty = 0.9f,
                startingUnlocked = true,
                unlockCost = VectorSSResourceWallet.Zero(),
                baseReward = new VectorSSResourceWallet(34, 42, 42),
                firstClearBonus = new VectorSSResourceWallet(40, 50, 50),
                baseXp = 90,
                hasFirstWinUnlock = false
            }
        };

        private static readonly VectorSSGarageUpgradeDefinition[] UpgradeDefinitions =
        {
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.EngineBlock,
                displayName = "Engine Block",
                slot = VectorSSUpgradeSlot.Powertrain,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Raises launch torque and stretches the useful top end.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(65, 20, 0),
                costGrowth = 1.48f,
                perLevelStats = new VectorSSTuningStats
                {
                    acceleration = 1.07f,
                    topSpeed = 1.02f,
                    heat = 1.03f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.CompositePanels,
                displayName = "Composite Panels",
                slot = VectorSSUpgradeSlot.Chassis,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Swaps body weight for lighter Plastic composite panels.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(25, 70, 0),
                costGrowth = 1.42f,
                perLevelStats = new VectorSSTuningStats
                {
                    mass = 0.96f,
                    topSpeed = 1.025f,
                    ram = 0.98f,
                    stability = 0.99f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.GripRubber,
                displayName = "Grip Rubber",
                slot = VectorSSUpgradeSlot.Tires,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Improves tire bite, steering confidence, and controlled slide recovery.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(0, 25, 72),
                costGrowth = 1.45f,
                perLevelStats = new VectorSSTuningStats
                {
                    grip = 1.06f,
                    steering = 1.025f,
                    drift = 1.03f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.BrakeKit,
                displayName = "Brake Kit",
                slot = VectorSSUpgradeSlot.Brakes,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Adds braking force and stability during contact-heavy corner entries.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(42, 18, 26),
                costGrowth = 1.4f,
                perLevelStats = new VectorSSTuningStats
                {
                    brake = 1.08f,
                    stability = 1.03f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.BoostCanister,
                displayName = "Boost Canister",
                slot = VectorSSUpgradeSlot.Boost,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Expands boost storage and peak boost torque at the cost of extra heat.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(50, 50, 12),
                costGrowth = 1.5f,
                perLevelStats = new VectorSSTuningStats
                {
                    boostCapacity = 1.08f,
                    boostPower = 1.06f,
                    heat = 1.06f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.CoolingDucts,
                displayName = "Cooling Ducts",
                slot = VectorSSUpgradeSlot.Cooling,
                applicability = VectorSSUpgradeApplicability.AllVehicles,
                description = "Improves boost cooling and reduces heat buildup.",
                maxLevel = 3,
                coreUpgrade = true,
                baseCost = new VectorSSResourceWallet(30, 68, 0),
                costGrowth = 1.44f,
                perLevelStats = new VectorSSTuningStats
                {
                    cooling = 1.1f,
                    heat = 0.92f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.ChainDrive,
                displayName = "Chain Drive",
                slot = VectorSSUpgradeSlot.BikeDrive,
                applicability = VectorSSUpgradeApplicability.BikesOnly,
                description = "Razor-only drive upgrade for sharper launches and higher exit speed.",
                maxLevel = 3,
                coreUpgrade = false,
                baseCost = new VectorSSResourceWallet(46, 20, 22),
                costGrowth = 1.38f,
                perLevelStats = new VectorSSTuningStats
                {
                    acceleration = 1.08f,
                    topSpeed = 1.03f,
                    stability = 0.99f
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.ForkGeometry,
                displayName = "Fork Geometry",
                slot = VectorSSUpgradeSlot.BikeHandling,
                applicability = VectorSSUpgradeApplicability.BikesOnly,
                description = "Razor-only steering geometry for faster lean-in and cleaner recovery.",
                maxLevel = 3,
                coreUpgrade = false,
                baseCost = new VectorSSResourceWallet(22, 46, 40),
                costGrowth = 1.36f,
                perLevelStats = new VectorSSTuningStats
                {
                    steering = 1.07f,
                    grip = 1.035f,
                    drift = 1.04f,
                    centerOfMassOffset = new Vector3(0f, -0.015f, 0f)
                }
            },
            new VectorSSGarageUpgradeDefinition
            {
                id = VectorSSUpgradeId.GyroCore,
                displayName = "Gyro Core",
                slot = VectorSSUpgradeSlot.BikeStability,
                applicability = VectorSSUpgradeApplicability.BikesOnly,
                description = "Razor-only stabilization that makes contact and boost exits less punishing.",
                maxLevel = 2,
                coreUpgrade = false,
                baseCost = new VectorSSResourceWallet(62, 62, 18),
                costGrowth = 1.55f,
                perLevelStats = new VectorSSTuningStats
                {
                    stability = 1.12f,
                    ram = 1.08f,
                    cooling = 1.04f
                }
            }
        };

        public static IList<VectorSSGarageVehicleDefinition> GetVehicles()
        {
            return new List<VectorSSGarageVehicleDefinition>(VehicleDefinitions);
        }

        public static IList<VectorSSGarageMapDefinition> GetMaps()
        {
            return new List<VectorSSGarageMapDefinition>(MapDefinitions);
        }

        public static IList<VectorSSGarageUpgradeDefinition> GetUpgrades()
        {
            return new List<VectorSSGarageUpgradeDefinition>(UpgradeDefinitions);
        }

        public static VectorSSGarageVehicleDefinition GetVehicle(VectorSSVehicleId id)
        {
            for (int i = 0; i < VehicleDefinitions.Length; i++)
            {
                if (VehicleDefinitions[i].id == id)
                {
                    return VehicleDefinitions[i];
                }
            }

            return null;
        }

        public static VectorSSGarageMapDefinition GetMap(VectorSSMapId id)
        {
            for (int i = 0; i < MapDefinitions.Length; i++)
            {
                if (MapDefinitions[i].id == id)
                {
                    return MapDefinitions[i];
                }
            }

            return null;
        }

        public static VectorSSGarageUpgradeDefinition GetUpgrade(VectorSSUpgradeId id)
        {
            for (int i = 0; i < UpgradeDefinitions.Length; i++)
            {
                if (UpgradeDefinitions[i].id == id)
                {
                    return UpgradeDefinitions[i];
                }
            }

            return null;
        }

        public static IList<VectorSSGarageUpgradeDefinition> GetUpgradesForVehicle(VectorSSVehicleId vehicleId)
        {
            List<VectorSSGarageUpgradeDefinition> upgrades = new List<VectorSSGarageUpgradeDefinition>();
            VectorSSGarageVehicleDefinition vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                return upgrades;
            }

            for (int i = 0; i < UpgradeDefinitions.Length; i++)
            {
                if (CanApplyUpgradeToVehicle(UpgradeDefinitions[i], vehicle))
                {
                    upgrades.Add(UpgradeDefinitions[i]);
                }
            }

            return upgrades;
        }

        public static bool CanApplyUpgradeToVehicle(VectorSSGarageUpgradeDefinition upgrade, VectorSSGarageVehicleDefinition vehicle)
        {
            if (upgrade == null || vehicle == null)
            {
                return false;
            }

            if (upgrade.applicability == VectorSSUpgradeApplicability.CarsOnly)
            {
                return vehicle.kind == VectorSSVehicleKind.Car;
            }

            if (upgrade.applicability == VectorSSUpgradeApplicability.BikesOnly)
            {
                return vehicle.kind == VectorSSVehicleKind.Bike;
            }

            return true;
        }

        public static VectorSSResourceWallet GetUpgradeCost(VectorSSUpgradeId upgradeId, int currentLevel)
        {
            VectorSSGarageUpgradeDefinition upgrade = GetUpgrade(upgradeId);
            if (upgrade == null)
            {
                return VectorSSResourceWallet.Zero();
            }

            int safeLevel = Mathf.Max(0, currentLevel);
            float scale = Mathf.Pow(Mathf.Max(1f, upgrade.costGrowth), safeLevel);
            return ScaleCost(upgrade.baseCost, scale);
        }

        public static VectorSSGarageSaveData CreateDefaultSave()
        {
            VectorSSGarageSaveData save = new VectorSSGarageSaveData
            {
                productTitle = VectorSSBrand.Title,
                schemaName = VectorSSBrand.SaveSchema,
                wallet = new VectorSSResourceWallet(85, 75, 70),
                selectedVehicleId = VectorSSVehicleId.Hammer,
                selectedMapId = VectorSSMapId.BlacklineCircuit,
                lastSavedUtcTicks = System.DateTime.UtcNow.Ticks
            };

            for (int i = 0; i < VehicleDefinitions.Length; i++)
            {
                VectorSSGarageVehicleDefinition definition = VehicleDefinitions[i];
                VectorSSGarageVehicleState state = new VectorSSGarageVehicleState(definition.id, definition.startingUnlocked);
                IList<VectorSSGarageUpgradeDefinition> upgrades = GetUpgradesForVehicle(definition.id);
                for (int upgradeIndex = 0; upgradeIndex < upgrades.Count; upgradeIndex++)
                {
                    state.upgrades.Add(new VectorSSGarageUpgradeState(upgrades[upgradeIndex].id, 0));
                }

                save.vehicles.Add(state);
            }

            for (int i = 0; i < MapDefinitions.Length; i++)
            {
                VectorSSGarageMapDefinition definition = MapDefinitions[i];
                save.maps.Add(new VectorSSGarageMapState(definition.id, definition.startingUnlocked));
            }

            return save;
        }

        private static VectorSSResourceWallet ScaleCost(VectorSSResourceWallet baseCost, float scale)
        {
            if (baseCost == null)
            {
                return VectorSSResourceWallet.Zero();
            }

            return new VectorSSResourceWallet(
                Mathf.RoundToInt(baseCost.metal * scale),
                Mathf.RoundToInt(baseCost.plastic * scale),
                Mathf.RoundToInt(baseCost.rubber * scale));
        }
    }
}
