using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTX.Progression
{
    public static class VectorSSGarageSaveSystem
    {
        public const string PlayerPrefsKey = "VectorSS.Garage.Save.Exists.v1";
        private const string Prefix = "VectorSS.Garage.v1.";

        public static bool HasSave()
        {
            return PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
        }

        public static VectorSSGarageSaveData LoadOrCreate()
        {
            VectorSSGarageSaveData save = VectorSSGarageCatalog.CreateDefaultSave();
            if (!HasSave())
            {
                Save(save);
                return save;
            }

            save.wallet = new VectorSSResourceWallet(
                PlayerPrefs.GetInt(Prefix + "Metal", save.wallet.metal),
                PlayerPrefs.GetInt(Prefix + "Plastic", save.wallet.plastic),
                PlayerPrefs.GetInt(Prefix + "Rubber", save.wallet.rubber));
            save.selectedVehicleId = (VectorSSVehicleId)PlayerPrefs.GetInt(Prefix + "SelectedVehicle", (int)save.selectedVehicleId);
            save.selectedMapId = (VectorSSMapId)PlayerPrefs.GetInt(Prefix + "SelectedMap", (int)save.selectedMapId);
            save.totalRaces = PlayerPrefs.GetInt(Prefix + "TotalRaces", save.totalRaces);
            save.totalVictories = PlayerPrefs.GetInt(Prefix + "TotalVictories", save.totalVictories);
            save.lastSavedUtcTicks = ReadLong(Prefix + "LastSavedUtcTicks", save.lastSavedUtcTicks);

            LoadVehicleStates(save);
            LoadMapStates(save);
            Normalize(save);
            return save;
        }

        public static void Save(VectorSSGarageSaveData save)
        {
            if (save == null)
            {
                return;
            }

            Normalize(save);
            save.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            PlayerPrefs.SetInt(Prefix + "Metal", save.wallet.metal);
            PlayerPrefs.SetInt(Prefix + "Plastic", save.wallet.plastic);
            PlayerPrefs.SetInt(Prefix + "Rubber", save.wallet.rubber);
            PlayerPrefs.SetInt(Prefix + "SelectedVehicle", (int)save.selectedVehicleId);
            PlayerPrefs.SetInt(Prefix + "SelectedMap", (int)save.selectedMapId);
            PlayerPrefs.SetInt(Prefix + "TotalRaces", save.totalRaces);
            PlayerPrefs.SetInt(Prefix + "TotalVictories", save.totalVictories);
            PlayerPrefs.SetString(Prefix + "LastSavedUtcTicks", save.lastSavedUtcTicks.ToString());
            SaveVehicleStates(save);
            SaveMapStates(save);
            PlayerPrefs.Save();
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.DeleteKey(Prefix + "Metal");
            PlayerPrefs.DeleteKey(Prefix + "Plastic");
            PlayerPrefs.DeleteKey(Prefix + "Rubber");
            PlayerPrefs.DeleteKey(Prefix + "SelectedVehicle");
            PlayerPrefs.DeleteKey(Prefix + "SelectedMap");
            PlayerPrefs.DeleteKey(Prefix + "TotalRaces");
            PlayerPrefs.DeleteKey(Prefix + "TotalVictories");
            PlayerPrefs.DeleteKey(Prefix + "LastSavedUtcTicks");

            VectorSSGarageSaveData defaults = VectorSSGarageCatalog.CreateDefaultSave();
            for (int i = 0; i < defaults.vehicles.Count; i++)
            {
                VectorSSGarageVehicleState vehicle = defaults.vehicles[i];
                PlayerPrefs.DeleteKey(VehicleKey(vehicle.vehicleId, "Unlocked"));
                PlayerPrefs.DeleteKey(VehicleKey(vehicle.vehicleId, "Xp"));
                for (int upgradeIndex = 0; upgradeIndex < vehicle.upgrades.Count; upgradeIndex++)
                {
                    PlayerPrefs.DeleteKey(UpgradeKey(vehicle.vehicleId, vehicle.upgrades[upgradeIndex].id));
                }
            }

            for (int i = 0; i < defaults.maps.Count; i++)
            {
                VectorSSGarageMapState map = defaults.maps[i];
                PlayerPrefs.DeleteKey(MapKey(map.mapId, "Unlocked"));
                PlayerPrefs.DeleteKey(MapKey(map.mapId, "FirstClear"));
                PlayerPrefs.DeleteKey(MapKey(map.mapId, "Completions"));
                PlayerPrefs.DeleteKey(MapKey(map.mapId, "BestPlacement"));
                PlayerPrefs.DeleteKey(MapKey(map.mapId, "BestScore"));
            }

            PlayerPrefs.Save();
        }

        public static void Normalize(VectorSSGarageSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.schemaVersion = Mathf.Max(1, save.schemaVersion);
            save.schemaName = VectorSSBrand.SaveSchema;
            save.productTitle = VectorSSBrand.Title;

            if (save.wallet == null)
            {
                save.wallet = VectorSSResourceWallet.Zero();
            }

            if (save.vehicles == null)
            {
                save.vehicles = new List<VectorSSGarageVehicleState>();
            }

            if (save.maps == null)
            {
                save.maps = new List<VectorSSGarageMapState>();
            }

            IList<VectorSSGarageVehicleDefinition> vehicles = VectorSSGarageCatalog.GetVehicles();
            for (int i = 0; i < vehicles.Count; i++)
            {
                VectorSSGarageVehicleDefinition definition = vehicles[i];
                VectorSSGarageVehicleState state = save.GetVehicleState(definition.id, true);
                if (definition.startingUnlocked)
                {
                    state.unlocked = true;
                }

                EnsureUpgradeStates(state, definition.id);
            }

            IList<VectorSSGarageMapDefinition> maps = VectorSSGarageCatalog.GetMaps();
            for (int i = 0; i < maps.Count; i++)
            {
                VectorSSGarageMapDefinition definition = maps[i];
                VectorSSGarageMapState state = save.GetMapState(definition.id, true);
                if (definition.startingUnlocked)
                {
                    state.unlocked = true;
                }
            }

            VectorSSGarageVehicleState selectedVehicle = save.GetVehicleState(save.selectedVehicleId, false);
            if (selectedVehicle == null || !selectedVehicle.unlocked)
            {
                save.selectedVehicleId = VectorSSVehicleId.Hammer;
            }

            VectorSSGarageMapState selectedMap = save.GetMapState(save.selectedMapId, false);
            if (selectedMap == null || !selectedMap.unlocked)
            {
                save.selectedMapId = VectorSSMapId.BlacklineCircuit;
            }
        }

        public static bool SelectVehicle(VectorSSGarageSaveData save, VectorSSVehicleId vehicleId, bool saveImmediately)
        {
            if (save == null)
            {
                return false;
            }

            Normalize(save);
            VectorSSGarageVehicleState state = save.GetVehicleState(vehicleId, false);
            if (state == null || !state.unlocked)
            {
                return false;
            }

            save.selectedVehicleId = vehicleId;
            if (saveImmediately)
            {
                Save(save);
            }

            return true;
        }

        public static bool SelectMap(VectorSSGarageSaveData save, VectorSSMapId mapId, bool saveImmediately)
        {
            if (save == null)
            {
                return false;
            }

            Normalize(save);
            VectorSSGarageMapState state = save.GetMapState(mapId, false);
            if (state == null || !state.unlocked)
            {
                return false;
            }

            save.selectedMapId = mapId;
            if (saveImmediately)
            {
                Save(save);
            }

            return true;
        }

        public static VectorSSPurchaseResult TryUnlockVehicle(VectorSSGarageSaveData save, VectorSSVehicleId vehicleId, bool saveImmediately)
        {
            if (save == null)
            {
                return VectorSSPurchaseResult.Failure("No Vector SS save data is loaded.", null, null);
            }

            Normalize(save);
            VectorSSGarageVehicleDefinition definition = VectorSSGarageCatalog.GetVehicle(vehicleId);
            VectorSSGarageVehicleState state = save.GetVehicleState(vehicleId, true);
            if (definition == null || state == null)
            {
                return VectorSSPurchaseResult.Failure("Vehicle is not in the Vector SS catalog.", null, save.wallet);
            }

            if (state.unlocked)
            {
                return VectorSSPurchaseResult.Success(definition.displayName + " is already unlocked.", VectorSSResourceWallet.Zero(), save.wallet);
            }

            VectorSSResourceWallet cost = definition.unlockCost != null ? definition.unlockCost.Clone() : VectorSSResourceWallet.Zero();
            if (!save.wallet.TrySpend(cost))
            {
                return VectorSSPurchaseResult.Failure("Not enough resources to unlock " + definition.displayName + ".", cost, save.wallet);
            }

            state.unlocked = true;
            if (saveImmediately)
            {
                Save(save);
            }

            return VectorSSPurchaseResult.Success("Unlocked " + definition.displayName + ".", cost, save.wallet);
        }

        public static VectorSSPurchaseResult TryUnlockMap(VectorSSGarageSaveData save, VectorSSMapId mapId, bool saveImmediately)
        {
            if (save == null)
            {
                return VectorSSPurchaseResult.Failure("No Vector SS save data is loaded.", null, null);
            }

            Normalize(save);
            VectorSSGarageMapDefinition definition = VectorSSGarageCatalog.GetMap(mapId);
            VectorSSGarageMapState state = save.GetMapState(mapId, true);
            if (definition == null || state == null)
            {
                return VectorSSPurchaseResult.Failure("Map is not in the Vector SS catalog.", null, save.wallet);
            }

            if (state.unlocked)
            {
                return VectorSSPurchaseResult.Success(definition.displayName + " is already unlocked.", VectorSSResourceWallet.Zero(), save.wallet);
            }

            VectorSSResourceWallet cost = definition.unlockCost != null ? definition.unlockCost.Clone() : VectorSSResourceWallet.Zero();
            if (!save.wallet.TrySpend(cost))
            {
                return VectorSSPurchaseResult.Failure("Not enough resources to unlock " + definition.displayName + ".", cost, save.wallet);
            }

            state.unlocked = true;
            if (saveImmediately)
            {
                Save(save);
            }

            return VectorSSPurchaseResult.Success("Unlocked " + definition.displayName + ".", cost, save.wallet);
        }

        public static VectorSSPurchaseResult TryPurchaseUpgrade(VectorSSGarageSaveData save, VectorSSVehicleId vehicleId, VectorSSUpgradeId upgradeId, bool saveImmediately)
        {
            if (save == null)
            {
                return VectorSSPurchaseResult.Failure("No Vector SS save data is loaded.", null, null);
            }

            Normalize(save);
            VectorSSGarageVehicleDefinition vehicle = VectorSSGarageCatalog.GetVehicle(vehicleId);
            VectorSSGarageUpgradeDefinition upgrade = VectorSSGarageCatalog.GetUpgrade(upgradeId);
            VectorSSGarageVehicleState vehicleState = save.GetVehicleState(vehicleId, true);

            if (vehicle == null || upgrade == null || vehicleState == null)
            {
                return VectorSSPurchaseResult.Failure("Upgrade or vehicle is not in the Vector SS catalog.", null, save.wallet);
            }

            if (!vehicleState.unlocked)
            {
                return VectorSSPurchaseResult.Failure(vehicle.displayName + " is locked.", null, save.wallet);
            }

            if (!VectorSSGarageCatalog.CanApplyUpgradeToVehicle(upgrade, vehicle))
            {
                return VectorSSPurchaseResult.Failure(upgrade.displayName + " cannot be installed on " + vehicle.displayName + ".", null, save.wallet);
            }

            VectorSSGarageUpgradeState upgradeState = vehicleState.GetUpgradeState(upgradeId, true);
            int currentLevel = Mathf.Clamp(upgradeState.level, 0, upgrade.maxLevel);
            if (currentLevel >= upgrade.maxLevel)
            {
                return VectorSSPurchaseResult.Success(upgrade.displayName + " is already maxed.", VectorSSResourceWallet.Zero(), save.wallet);
            }

            VectorSSResourceWallet cost = VectorSSGarageCatalog.GetUpgradeCost(upgradeId, currentLevel);
            if (!save.wallet.TrySpend(cost))
            {
                return VectorSSPurchaseResult.Failure("Not enough resources for " + upgrade.displayName + " level " + (currentLevel + 1) + ".", cost, save.wallet);
            }

            upgradeState.level = currentLevel + 1;
            if (saveImmediately)
            {
                Save(save);
            }

            return VectorSSPurchaseResult.Success("Installed " + upgrade.displayName + " level " + upgradeState.level + ".", cost, save.wallet);
        }

        public static VectorSSGarageRaceReward ApplyRaceResultAndSave(VectorSSGarageSaveData save, VectorSSGarageRaceResult result)
        {
            if (save == null)
            {
                save = LoadOrCreate();
            }

            Normalize(save);
            VectorSSGarageRaceReward reward = VectorSSGarageRewardCalculator.Calculate(result, save);
            ApplyReward(save, result, reward);
            Save(save);
            return reward;
        }

        public static void ApplyReward(VectorSSGarageSaveData save, VectorSSGarageRaceResult result, VectorSSGarageRaceReward reward)
        {
            if (save == null || result == null || reward == null)
            {
                return;
            }

            Normalize(save);
            save.wallet.Add(reward.resources);
            save.totalRaces++;

            VectorSSGarageVehicleState vehicleState = save.GetVehicleState(result.vehicleId, true);
            if (vehicleState != null)
            {
                vehicleState.xp = Mathf.Max(0, vehicleState.xp + reward.xp);
            }

            VectorSSGarageMapState mapState = save.GetMapState(result.mapId, true);
            if (mapState != null)
            {
                mapState.completions += result.finished ? 1 : 0;
                mapState.bestScore = Mathf.Max(mapState.bestScore, reward.score);
                if (result.finished && (mapState.bestPlacement <= 0 || result.placement < mapState.bestPlacement))
                {
                    mapState.bestPlacement = Mathf.Max(1, result.placement);
                }

                if (reward.firstClear)
                {
                    mapState.firstClearCompleted = true;
                }
            }

            if (result.finished && result.placement == 1)
            {
                save.totalVictories++;
                UnlockFollowupMap(save, result.mapId, reward);
            }
        }

        private static void EnsureUpgradeStates(VectorSSGarageVehicleState state, VectorSSVehicleId vehicleId)
        {
            if (state == null)
            {
                return;
            }

            if (state.upgrades == null)
            {
                state.upgrades = new List<VectorSSGarageUpgradeState>();
            }

            IList<VectorSSGarageUpgradeDefinition> upgrades = VectorSSGarageCatalog.GetUpgradesForVehicle(vehicleId);
            for (int i = 0; i < upgrades.Count; i++)
            {
                state.GetUpgradeState(upgrades[i].id, true);
            }
        }

        private static void UnlockFollowupMap(VectorSSGarageSaveData save, VectorSSMapId clearedMap, VectorSSGarageRaceReward reward)
        {
            VectorSSGarageMapDefinition definition = VectorSSGarageCatalog.GetMap(clearedMap);
            if (definition == null || !definition.hasFirstWinUnlock)
            {
                return;
            }

            VectorSSGarageMapState followup = save.GetMapState(definition.firstWinUnlocksMap, true);
            if (followup == null || followup.unlocked)
            {
                return;
            }

            followup.unlocked = true;
            reward.unlockedFollowupMap = true;
            reward.followupMapId = definition.firstWinUnlocksMap;
        }

        private static void LoadVehicleStates(VectorSSGarageSaveData save)
        {
            if (save == null || save.vehicles == null)
            {
                return;
            }

            for (int i = 0; i < save.vehicles.Count; i++)
            {
                VectorSSGarageVehicleState vehicle = save.vehicles[i];
                if (vehicle == null)
                {
                    continue;
                }

                vehicle.unlocked = PlayerPrefs.GetInt(VehicleKey(vehicle.vehicleId, "Unlocked"), vehicle.unlocked ? 1 : 0) == 1;
                vehicle.xp = PlayerPrefs.GetInt(VehicleKey(vehicle.vehicleId, "Xp"), vehicle.xp);
                if (vehicle.upgrades == null)
                {
                    continue;
                }

                for (int upgradeIndex = 0; upgradeIndex < vehicle.upgrades.Count; upgradeIndex++)
                {
                    VectorSSGarageUpgradeState upgrade = vehicle.upgrades[upgradeIndex];
                    if (upgrade != null)
                    {
                        upgrade.level = PlayerPrefs.GetInt(UpgradeKey(vehicle.vehicleId, upgrade.id), upgrade.level);
                    }
                }
            }
        }

        private static void LoadMapStates(VectorSSGarageSaveData save)
        {
            if (save == null || save.maps == null)
            {
                return;
            }

            for (int i = 0; i < save.maps.Count; i++)
            {
                VectorSSGarageMapState map = save.maps[i];
                if (map == null)
                {
                    continue;
                }

                map.unlocked = PlayerPrefs.GetInt(MapKey(map.mapId, "Unlocked"), map.unlocked ? 1 : 0) == 1;
                map.firstClearCompleted = PlayerPrefs.GetInt(MapKey(map.mapId, "FirstClear"), map.firstClearCompleted ? 1 : 0) == 1;
                map.completions = PlayerPrefs.GetInt(MapKey(map.mapId, "Completions"), map.completions);
                map.bestPlacement = PlayerPrefs.GetInt(MapKey(map.mapId, "BestPlacement"), map.bestPlacement);
                map.bestScore = PlayerPrefs.GetInt(MapKey(map.mapId, "BestScore"), map.bestScore);
            }
        }

        private static void SaveVehicleStates(VectorSSGarageSaveData save)
        {
            if (save == null || save.vehicles == null)
            {
                return;
            }

            for (int i = 0; i < save.vehicles.Count; i++)
            {
                VectorSSGarageVehicleState vehicle = save.vehicles[i];
                if (vehicle == null)
                {
                    continue;
                }

                PlayerPrefs.SetInt(VehicleKey(vehicle.vehicleId, "Unlocked"), vehicle.unlocked ? 1 : 0);
                PlayerPrefs.SetInt(VehicleKey(vehicle.vehicleId, "Xp"), vehicle.xp);
                if (vehicle.upgrades == null)
                {
                    continue;
                }

                for (int upgradeIndex = 0; upgradeIndex < vehicle.upgrades.Count; upgradeIndex++)
                {
                    VectorSSGarageUpgradeState upgrade = vehicle.upgrades[upgradeIndex];
                    if (upgrade != null)
                    {
                        PlayerPrefs.SetInt(UpgradeKey(vehicle.vehicleId, upgrade.id), Mathf.Max(0, upgrade.level));
                    }
                }
            }
        }

        private static void SaveMapStates(VectorSSGarageSaveData save)
        {
            if (save == null || save.maps == null)
            {
                return;
            }

            for (int i = 0; i < save.maps.Count; i++)
            {
                VectorSSGarageMapState map = save.maps[i];
                if (map == null)
                {
                    continue;
                }

                PlayerPrefs.SetInt(MapKey(map.mapId, "Unlocked"), map.unlocked ? 1 : 0);
                PlayerPrefs.SetInt(MapKey(map.mapId, "FirstClear"), map.firstClearCompleted ? 1 : 0);
                PlayerPrefs.SetInt(MapKey(map.mapId, "Completions"), Mathf.Max(0, map.completions));
                PlayerPrefs.SetInt(MapKey(map.mapId, "BestPlacement"), Mathf.Max(0, map.bestPlacement));
                PlayerPrefs.SetInt(MapKey(map.mapId, "BestScore"), Mathf.Max(0, map.bestScore));
            }
        }

        private static string VehicleKey(VectorSSVehicleId vehicleId, string field)
        {
            return Prefix + "Vehicle." + vehicleId + "." + field;
        }

        private static string UpgradeKey(VectorSSVehicleId vehicleId, VectorSSUpgradeId upgradeId)
        {
            return Prefix + "Vehicle." + vehicleId + ".Upgrade." + upgradeId;
        }

        private static string MapKey(VectorSSMapId mapId, string field)
        {
            return Prefix + "Map." + mapId + "." + field;
        }

        private static long ReadLong(string key, long fallback)
        {
            long value;
            return long.TryParse(PlayerPrefs.GetString(key, fallback.ToString()), out value) ? value : fallback;
        }
    }
}
