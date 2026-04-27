using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTX.Progression
{
    public static class VectorSSBrand
    {
        public const string Title = "Vector SS";
        public const string SaveSchema = "VectorSS.Progression.0.1.0";
    }

    public enum VectorSSResourceType
    {
        Metal,
        Plastic,
        Rubber
    }

    public enum VectorSSVehicleKind
    {
        Car,
        Bike
    }

    public enum VectorSSUpgradeId
    {
        EngineBlock,
        CompositePanels,
        GripRubber,
        BrakeKit,
        BoostCanister,
        CoolingDucts,
        ChainDrive,
        ForkGeometry,
        GyroCore
    }

    public enum VectorSSUpgradeSlot
    {
        Powertrain,
        Chassis,
        Tires,
        Brakes,
        Boost,
        Cooling,
        BikeDrive,
        BikeHandling,
        BikeStability
    }

    public enum VectorSSUpgradeApplicability
    {
        AllVehicles,
        CarsOnly,
        BikesOnly
    }

    [Serializable]
    public sealed class VectorSSResourceWallet
    {
        public int metal;
        public int plastic;
        public int rubber;

        public VectorSSResourceWallet()
        {
        }

        public VectorSSResourceWallet(int metal, int plastic, int rubber)
        {
            this.metal = Mathf.Max(0, metal);
            this.plastic = Mathf.Max(0, plastic);
            this.rubber = Mathf.Max(0, rubber);
        }

        public static VectorSSResourceWallet Zero()
        {
            return new VectorSSResourceWallet(0, 0, 0);
        }

        public VectorSSResourceWallet Clone()
        {
            return new VectorSSResourceWallet(metal, plastic, rubber);
        }

        public int Get(VectorSSResourceType type)
        {
            switch (type)
            {
                case VectorSSResourceType.Plastic:
                    return plastic;
                case VectorSSResourceType.Rubber:
                    return rubber;
                default:
                    return metal;
            }
        }

        public void Set(VectorSSResourceType type, int value)
        {
            int safeValue = Mathf.Max(0, value);
            switch (type)
            {
                case VectorSSResourceType.Plastic:
                    plastic = safeValue;
                    break;
                case VectorSSResourceType.Rubber:
                    rubber = safeValue;
                    break;
                default:
                    metal = safeValue;
                    break;
            }
        }

        public void Add(VectorSSResourceWallet other)
        {
            if (other == null)
            {
                return;
            }

            metal = Mathf.Max(0, metal + other.metal);
            plastic = Mathf.Max(0, plastic + other.plastic);
            rubber = Mathf.Max(0, rubber + other.rubber);
        }

        public void Add(int metalDelta, int plasticDelta, int rubberDelta)
        {
            metal = Mathf.Max(0, metal + metalDelta);
            plastic = Mathf.Max(0, plastic + plasticDelta);
            rubber = Mathf.Max(0, rubber + rubberDelta);
        }

        public bool CanAfford(VectorSSResourceWallet cost)
        {
            return cost == null || (metal >= cost.metal && plastic >= cost.plastic && rubber >= cost.rubber);
        }

        public bool TrySpend(VectorSSResourceWallet cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            if (cost != null)
            {
                metal -= cost.metal;
                plastic -= cost.plastic;
                rubber -= cost.rubber;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Metal {0} / Plastic {1} / Rubber {2}", metal, plastic, rubber);
        }
    }

    [Serializable]
    public sealed class VectorSSTuningStats
    {
        public float mass = 1f;
        public float acceleration = 1f;
        public float topSpeed = 1f;
        public float grip = 1f;
        public float steering = 1f;
        public float brake = 1f;
        public float boostCapacity = 1f;
        public float boostPower = 1f;
        public float cooling = 1f;
        public float heat = 1f;
        public float downforce = 1f;
        public float ram = 1f;
        public float drift = 1f;
        public float stability = 1f;
        public Vector3 centerOfMassOffset = Vector3.zero;

        public static VectorSSTuningStats One()
        {
            return new VectorSSTuningStats();
        }

        public VectorSSTuningStats Clone()
        {
            return new VectorSSTuningStats
            {
                mass = mass,
                acceleration = acceleration,
                topSpeed = topSpeed,
                grip = grip,
                steering = steering,
                brake = brake,
                boostCapacity = boostCapacity,
                boostPower = boostPower,
                cooling = cooling,
                heat = heat,
                downforce = downforce,
                ram = ram,
                drift = drift,
                stability = stability,
                centerOfMassOffset = centerOfMassOffset
            };
        }

        public void Multiply(VectorSSTuningStats other)
        {
            if (other == null)
            {
                return;
            }

            mass *= other.mass;
            acceleration *= other.acceleration;
            topSpeed *= other.topSpeed;
            grip *= other.grip;
            steering *= other.steering;
            brake *= other.brake;
            boostCapacity *= other.boostCapacity;
            boostPower *= other.boostPower;
            cooling *= other.cooling;
            heat *= other.heat;
            downforce *= other.downforce;
            ram *= other.ram;
            drift *= other.drift;
            stability *= other.stability;
            centerOfMassOffset += other.centerOfMassOffset;
        }

        public void MultiplyPerLevel(VectorSSTuningStats perLevelStats, int level)
        {
            int safeLevel = Mathf.Max(0, level);
            for (int i = 0; i < safeLevel; i++)
            {
                Multiply(perLevelStats);
            }
        }
    }

    [Serializable]
    public sealed class VectorSSGarageVehicleDefinition
    {
        public VectorSSVehicleId id;
        public string displayName;
        public VectorSSVehicleKind kind = VectorSSVehicleKind.Car;
        [TextArea] public string description;
        public bool startingUnlocked;
        public VectorSSResourceWallet unlockCost = VectorSSResourceWallet.Zero();
        public VectorSSTuningStats baseStats = VectorSSTuningStats.One();
        public float rewardMultiplier = 1f;
    }

    [Serializable]
    public sealed class VectorSSGarageMapDefinition
    {
        public VectorSSMapId id;
        public string displayName;
        [TextArea] public string description;
        public int lapCount = 3;
        public float difficulty = 1f;
        public bool startingUnlocked;
        public VectorSSResourceWallet unlockCost = VectorSSResourceWallet.Zero();
        public VectorSSResourceWallet baseReward = new VectorSSResourceWallet(30, 25, 25);
        public VectorSSResourceWallet firstClearBonus = new VectorSSResourceWallet(30, 30, 30);
        public int baseXp = 100;
        public bool hasFirstWinUnlock;
        public VectorSSMapId firstWinUnlocksMap;
    }

    [Serializable]
    public sealed class VectorSSGarageUpgradeDefinition
    {
        public VectorSSUpgradeId id;
        public string displayName;
        public VectorSSUpgradeSlot slot;
        public VectorSSUpgradeApplicability applicability = VectorSSUpgradeApplicability.AllVehicles;
        [TextArea] public string description;
        public int maxLevel = 3;
        public bool coreUpgrade;
        public VectorSSResourceWallet baseCost = new VectorSSResourceWallet(50, 50, 50);
        public float costGrowth = 1.45f;
        public VectorSSTuningStats perLevelStats = VectorSSTuningStats.One();
    }

    [Serializable]
    public sealed class VectorSSGarageUpgradeState
    {
        public VectorSSUpgradeId id;
        public int level;

        public VectorSSGarageUpgradeState()
        {
        }

        public VectorSSGarageUpgradeState(VectorSSUpgradeId id, int level)
        {
            this.id = id;
            this.level = Mathf.Max(0, level);
        }
    }

    [Serializable]
    public sealed class VectorSSGarageVehicleState
    {
        public VectorSSVehicleId vehicleId;
        public bool unlocked;
        public int xp;
        public List<VectorSSGarageUpgradeState> upgrades = new List<VectorSSGarageUpgradeState>();

        public VectorSSGarageVehicleState()
        {
        }

        public VectorSSGarageVehicleState(VectorSSVehicleId vehicleId, bool unlocked)
        {
            this.vehicleId = vehicleId;
            this.unlocked = unlocked;
        }

        public VectorSSGarageUpgradeState GetUpgradeState(VectorSSUpgradeId upgradeId, bool createIfMissing)
        {
            if (upgrades == null)
            {
                upgrades = new List<VectorSSGarageUpgradeState>();
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i].id == upgradeId)
                {
                    return upgrades[i];
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            VectorSSGarageUpgradeState state = new VectorSSGarageUpgradeState(upgradeId, 0);
            upgrades.Add(state);
            return state;
        }
    }

    [Serializable]
    public sealed class VectorSSGarageMapState
    {
        public VectorSSMapId mapId;
        public bool unlocked;
        public bool firstClearCompleted;
        public int completions;
        public int bestPlacement;
        public int bestScore;

        public VectorSSGarageMapState()
        {
        }

        public VectorSSGarageMapState(VectorSSMapId mapId, bool unlocked)
        {
            this.mapId = mapId;
            this.unlocked = unlocked;
        }
    }

    [Serializable]
    public sealed class VectorSSGarageSaveData
    {
        public int schemaVersion = 1;
        public string schemaName = VectorSSBrand.SaveSchema;
        public string productTitle = VectorSSBrand.Title;
        public VectorSSResourceWallet wallet = VectorSSResourceWallet.Zero();
        public VectorSSVehicleId selectedVehicleId = VectorSSVehicleId.Hammer;
        public VectorSSMapId selectedMapId = VectorSSMapId.BlacklineCircuit;
        public List<VectorSSGarageVehicleState> vehicles = new List<VectorSSGarageVehicleState>();
        public List<VectorSSGarageMapState> maps = new List<VectorSSGarageMapState>();
        public int totalRaces;
        public int totalVictories;
        public long lastSavedUtcTicks;

        public VectorSSGarageVehicleState GetVehicleState(VectorSSVehicleId vehicleId, bool createIfMissing)
        {
            if (vehicles == null)
            {
                vehicles = new List<VectorSSGarageVehicleState>();
            }

            for (int i = 0; i < vehicles.Count; i++)
            {
                if (vehicles[i] != null && vehicles[i].vehicleId == vehicleId)
                {
                    return vehicles[i];
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            VectorSSGarageVehicleDefinition definition = VectorSSGarageCatalog.GetVehicle(vehicleId);
            VectorSSGarageVehicleState state = new VectorSSGarageVehicleState(vehicleId, definition != null && definition.startingUnlocked);
            vehicles.Add(state);
            return state;
        }

        public VectorSSGarageMapState GetMapState(VectorSSMapId mapId, bool createIfMissing)
        {
            if (maps == null)
            {
                maps = new List<VectorSSGarageMapState>();
            }

            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].mapId == mapId)
                {
                    return maps[i];
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            VectorSSGarageMapDefinition definition = VectorSSGarageCatalog.GetMap(mapId);
            VectorSSGarageMapState state = new VectorSSGarageMapState(mapId, definition != null && definition.startingUnlocked);
            maps.Add(state);
            return state;
        }
    }

    [Serializable]
    public sealed class VectorSSGarageRaceResult
    {
        public VectorSSVehicleId vehicleId = VectorSSVehicleId.Hammer;
        public VectorSSMapId mapId = VectorSSMapId.BlacklineCircuit;
        public bool finished = true;
        public int placement = 1;
        public int fieldSize = 4;
        public int completedLaps = 3;
        public int totalLaps = 3;
        public float finishSeconds;
        [Range(0f, 1f)] public float flowPercent;
        public int driftScore;
        public int ramHits;
        public bool cleanRun;
    }

    [Serializable]
    public sealed class VectorSSGarageRaceReward
    {
        public VectorSSResourceWallet resources = VectorSSResourceWallet.Zero();
        public VectorSSResourceWallet firstClearResources = VectorSSResourceWallet.Zero();
        public int xp;
        public int score;
        public float multiplier = 1f;
        public bool firstClear;
        public bool unlockedFollowupMap;
        public VectorSSMapId followupMapId;
        public string summary;
    }

    [Serializable]
    public sealed class VectorSSPurchaseResult
    {
        public bool success;
        public string message;
        public VectorSSResourceWallet cost = VectorSSResourceWallet.Zero();
        public VectorSSResourceWallet remaining = VectorSSResourceWallet.Zero();

        public static VectorSSPurchaseResult Success(string message, VectorSSResourceWallet cost, VectorSSResourceWallet remaining)
        {
            return new VectorSSPurchaseResult
            {
                success = true,
                message = message,
                cost = cost != null ? cost.Clone() : VectorSSResourceWallet.Zero(),
                remaining = remaining != null ? remaining.Clone() : VectorSSResourceWallet.Zero()
            };
        }

        public static VectorSSPurchaseResult Failure(string message, VectorSSResourceWallet cost, VectorSSResourceWallet remaining)
        {
            return new VectorSSPurchaseResult
            {
                success = false,
                message = message,
                cost = cost != null ? cost.Clone() : VectorSSResourceWallet.Zero(),
                remaining = remaining != null ? remaining.Clone() : VectorSSResourceWallet.Zero()
            };
        }
    }
}
