using UnityEngine;

namespace GTX.Progression
{
    public static class VectorSSGarageRewardCalculator
    {
        public static VectorSSGarageRaceReward Calculate(VectorSSGarageRaceResult result, VectorSSGarageSaveData save)
        {
            if (result == null)
            {
                result = new VectorSSGarageRaceResult();
            }

            VectorSSGarageMapDefinition map = VectorSSGarageCatalog.GetMap(result.mapId) ?? VectorSSGarageCatalog.GetMap(VectorSSMapId.BlacklineCircuit);
            VectorSSGarageVehicleDefinition vehicle = VectorSSGarageCatalog.GetVehicle(result.vehicleId) ?? VectorSSGarageCatalog.GetVehicle(VectorSSVehicleId.Hammer);
            VectorSSGarageMapState mapState = save != null ? save.GetMapState(result.mapId, true) : null;

            int totalLaps = result.totalLaps > 0 ? result.totalLaps : (map != null ? map.lapCount : 3);
            float completion = result.finished ? 1f : Mathf.Clamp01(totalLaps > 0 ? (float)result.completedLaps / totalLaps : 0f);
            float placementMultiplier = GetPlacementMultiplier(result.placement, result.fieldSize);
            float flowMultiplier = 1f + Mathf.Clamp01(result.flowPercent) * 0.25f;
            float cleanMultiplier = result.cleanRun ? 1.08f : 1f;
            float difficultyMultiplier = map != null ? Mathf.Max(0.25f, map.difficulty) : 1f;
            float vehicleMultiplier = vehicle != null ? Mathf.Max(0.1f, vehicle.rewardMultiplier) : 1f;
            float multiplier = Mathf.Max(0.1f, completion * placementMultiplier * flowMultiplier * cleanMultiplier * difficultyMultiplier * vehicleMultiplier);

            VectorSSResourceWallet baseReward = map != null && map.baseReward != null ? map.baseReward : new VectorSSResourceWallet(30, 25, 25);
            VectorSSGarageRaceReward reward = new VectorSSGarageRaceReward();
            reward.multiplier = multiplier;
            reward.resources = new VectorSSResourceWallet(
                Mathf.RoundToInt(baseReward.metal * multiplier) + Mathf.Max(0, result.ramHits) * 2,
                Mathf.RoundToInt(baseReward.plastic * multiplier) + (result.cleanRun ? 6 : 0),
                Mathf.RoundToInt(baseReward.rubber * multiplier) + Mathf.RoundToInt(Mathf.Max(0, result.driftScore) * 0.015f) + Mathf.RoundToInt(Mathf.Clamp01(result.flowPercent) * 8f));

            if (vehicle != null && vehicle.kind == VectorSSVehicleKind.Bike)
            {
                reward.resources.rubber += Mathf.RoundToInt(6f * completion);
            }

            reward.firstClear = result.finished && (mapState == null || !mapState.firstClearCompleted);
            if (reward.firstClear && map != null && map.firstClearBonus != null)
            {
                reward.firstClearResources = map.firstClearBonus.Clone();
                reward.resources.Add(reward.firstClearResources);
            }

            int baseXp = map != null ? map.baseXp : 100;
            reward.xp = Mathf.RoundToInt(baseXp * multiplier) + Mathf.RoundToInt(Mathf.Clamp01(result.flowPercent) * 20f);
            reward.score = CalculateScore(result, multiplier);
            reward.summary = BuildSummary(reward);
            return reward;
        }

        private static float GetPlacementMultiplier(int placement, int fieldSize)
        {
            int safePlacement = Mathf.Max(1, placement);
            int safeFieldSize = Mathf.Max(1, fieldSize);
            if (safePlacement == 1)
            {
                return 1.35f;
            }

            if (safePlacement == 2)
            {
                return 1.12f;
            }

            if (safePlacement == 3)
            {
                return 1f;
            }

            float tail = Mathf.InverseLerp(safeFieldSize, 4, safePlacement);
            return Mathf.Lerp(0.68f, 0.92f, tail);
        }

        private static int CalculateScore(VectorSSGarageRaceResult result, float multiplier)
        {
            int placementScore = Mathf.Max(0, result.fieldSize - Mathf.Max(1, result.placement) + 1) * 250;
            int flowScore = Mathf.RoundToInt(Mathf.Clamp01(result.flowPercent) * 1000f);
            int styleScore = Mathf.Max(0, result.driftScore) + Mathf.Max(0, result.ramHits) * 120;
            int cleanScore = result.cleanRun ? 250 : 0;
            int finishScore = result.finished ? 500 : 0;
            return Mathf.RoundToInt((placementScore + flowScore + styleScore + cleanScore + finishScore) * Mathf.Max(0.1f, multiplier));
        }

        private static string BuildSummary(VectorSSGarageRaceReward reward)
        {
            if (reward == null || reward.resources == null)
            {
                return "No reward.";
            }

            string firstClear = reward.firstClear ? " First clear bonus applied." : string.Empty;
            return string.Format("+{0} XP, +{1} Metal, +{2} Plastic, +{3} Rubber.{4}",
                reward.xp,
                reward.resources.metal,
                reward.resources.plastic,
                reward.resources.rubber,
                firstClear);
        }
    }
}
