using GTX.Data;
using GTX.Vehicle;
using UnityEngine;

namespace GTX.Progression
{
    public sealed class VectorSSTuningBaseline
    {
        public float mass;
        public Vector3 centerOfMassOffset;
        public float downforce;
        public float redlineRpm;
        public float peakTorque;
        public float finalDrive;
        public float steeringAngle;
        public float steeringAngleAtSpeed;
        public float brakeTorque;
        public float maxDriveTorque;
        public float rigidbodyFallbackDriveForce;
        public float arcadeYawAssist;
        public float tractionControl;
        public float driftSideGrip;
        public float normalSideGrip;
        public float handbrakeRearGrip;
        public float boostCapacity;
        public float boostBurnPerSecond;
        public float boostRegenPerSecond;
        public float boostTorqueMultiplier;
        public float boostHeatPerSecond;
        public float boostCoolPerSecond;

        public VectorSSTuningBaseline()
        {
        }

        public VectorSSTuningBaseline(VehicleTuning tuning)
        {
            Capture(tuning);
        }

        public void Capture(VehicleTuning tuning)
        {
            if (tuning == null)
            {
                return;
            }

            mass = tuning.mass;
            centerOfMassOffset = tuning.centerOfMassOffset;
            downforce = tuning.downforce;
            redlineRpm = tuning.redlineRpm;
            peakTorque = tuning.peakTorque;
            finalDrive = tuning.finalDrive;
            steeringAngle = tuning.steeringAngle;
            steeringAngleAtSpeed = tuning.steeringAngleAtSpeed;
            brakeTorque = tuning.brakeTorque;
            maxDriveTorque = tuning.maxDriveTorque;
            rigidbodyFallbackDriveForce = tuning.rigidbodyFallbackDriveForce;
            arcadeYawAssist = tuning.arcadeYawAssist;
            tractionControl = tuning.tractionControl;
            driftSideGrip = tuning.driftSideGrip;
            normalSideGrip = tuning.normalSideGrip;
            handbrakeRearGrip = tuning.handbrakeRearGrip;
            boostCapacity = tuning.boostCapacity;
            boostBurnPerSecond = tuning.boostBurnPerSecond;
            boostRegenPerSecond = tuning.boostRegenPerSecond;
            boostTorqueMultiplier = tuning.boostTorqueMultiplier;
            boostHeatPerSecond = tuning.boostHeatPerSecond;
            boostCoolPerSecond = tuning.boostCoolPerSecond;
        }
    }

    public static class VectorSSGarageTuningApplier
    {
        public static VectorSSTuningBaseline CaptureBaseline(VehicleTuning tuning)
        {
            return new VectorSSTuningBaseline(tuning);
        }

        public static VectorSSTuningStats BuildAppliedStats(VectorSSGarageSaveData save)
        {
            VectorSSVehicleId vehicleId = save != null ? save.selectedVehicleId : VectorSSVehicleId.Hammer;
            return BuildAppliedStats(save, vehicleId);
        }

        public static VectorSSTuningStats BuildAppliedStats(VectorSSGarageSaveData save, VectorSSVehicleId vehicleId)
        {
            VectorSSGarageVehicleDefinition vehicle = VectorSSGarageCatalog.GetVehicle(vehicleId) ?? VectorSSGarageCatalog.GetVehicle(VectorSSVehicleId.Hammer);
            VectorSSTuningStats stats = vehicle != null && vehicle.baseStats != null ? vehicle.baseStats.Clone() : VectorSSTuningStats.One();

            if (save == null || vehicle == null)
            {
                return stats;
            }

            VectorSSGarageSaveSystem.Normalize(save);
            VectorSSGarageVehicleState vehicleState = save.GetVehicleState(vehicle.id, true);
            if (vehicleState == null || vehicleState.upgrades == null)
            {
                return stats;
            }

            for (int i = 0; i < vehicleState.upgrades.Count; i++)
            {
                VectorSSGarageUpgradeState upgradeState = vehicleState.upgrades[i];
                if (upgradeState == null || upgradeState.level <= 0)
                {
                    continue;
                }

                VectorSSGarageUpgradeDefinition upgrade = VectorSSGarageCatalog.GetUpgrade(upgradeState.id);
                if (!VectorSSGarageCatalog.CanApplyUpgradeToVehicle(upgrade, vehicle))
                {
                    continue;
                }

                int safeLevel = Mathf.Min(upgradeState.level, upgrade != null ? upgrade.maxLevel : upgradeState.level);
                stats.MultiplyPerLevel(upgrade.perLevelStats, safeLevel);
            }

            return stats;
        }

        public static GTXTuningProfile BuildTuningProfile(VectorSSGarageSaveData save)
        {
            VectorSSTuningStats stats = BuildAppliedStats(save);
            return new GTXTuningProfile
            {
                acceleration = Mathf.Clamp(stats.acceleration, 0.6f, 1.6f),
                topSpeed = Mathf.Clamp(stats.topSpeed, 0.6f, 1.6f),
                grip = Mathf.Clamp(stats.grip, 0.6f, 1.8f),
                steeringResponse = Mathf.Clamp(stats.steering, 0.4f, 1.8f),
                brakePower = Mathf.Clamp(stats.brake, 0.4f, 1.8f),
                boostPower = Mathf.Clamp(stats.boostPower, 0.4f, 1.8f),
                cooling = Mathf.Clamp(stats.cooling, 0.5f, 1.8f)
            };
        }

        public static void ApplyToRuntimeVehicle(VehicleController vehicle, VehicleTuning tuning, VectorSSGarageSaveData save, VectorSSTuningBaseline baseline)
        {
            ApplyToVehicleTuning(tuning, save, baseline);
            if (vehicle != null && tuning != null)
            {
                vehicle.ApplyTuning(tuning, false);
            }
        }

        public static void ApplyToVehicleTuning(VehicleTuning tuning, VectorSSGarageSaveData save, VectorSSTuningBaseline baseline)
        {
            if (tuning == null)
            {
                return;
            }

            VectorSSTuningBaseline source = baseline ?? new VectorSSTuningBaseline(tuning);
            VectorSSTuningStats stats = BuildAppliedStats(save);

            tuning.mass = Mathf.Max(120f, source.mass * stats.mass);
            tuning.centerOfMassOffset = source.centerOfMassOffset + stats.centerOfMassOffset;
            tuning.downforce = Mathf.Max(0f, source.downforce * stats.downforce * Mathf.Lerp(0.86f, 1.16f, Mathf.InverseLerp(0.7f, 1.4f, stats.stability)));

            tuning.peakTorque = Mathf.Max(80f, source.peakTorque * stats.acceleration);
            tuning.maxDriveTorque = Mathf.Max(100f, source.maxDriveTorque * stats.acceleration);
            tuning.rigidbodyFallbackDriveForce = Mathf.Max(1000f, source.rigidbodyFallbackDriveForce * stats.acceleration);
            tuning.finalDrive = Mathf.Max(0.5f, source.finalDrive / Mathf.Max(0.45f, stats.topSpeed));
            tuning.redlineRpm = Mathf.Max(source.redlineRpm * 0.72f, source.redlineRpm * Mathf.Lerp(0.96f, 1.1f, Mathf.InverseLerp(0.75f, 1.35f, stats.topSpeed)));

            tuning.steeringAngle = Mathf.Clamp(source.steeringAngle * stats.steering, 12f, 58f);
            tuning.steeringAngleAtSpeed = Mathf.Clamp(source.steeringAngleAtSpeed * Mathf.Lerp(0.94f, 1.16f, Mathf.InverseLerp(0.75f, 1.35f, stats.steering)), 4f, 22f);
            tuning.arcadeYawAssist = Mathf.Max(0f, source.arcadeYawAssist * Mathf.Lerp(0.85f, 1.28f, Mathf.InverseLerp(0.7f, 1.45f, stats.steering)) * Mathf.Lerp(0.92f, 1.1f, Mathf.InverseLerp(0.75f, 1.35f, stats.stability)));
            tuning.tractionControl = Mathf.Clamp01(source.tractionControl * Mathf.Lerp(0.92f, 1.24f, Mathf.InverseLerp(0.75f, 1.35f, stats.stability)));

            tuning.normalSideGrip = Mathf.Max(0.2f, source.normalSideGrip * stats.grip);
            tuning.driftSideGrip = Mathf.Clamp(source.driftSideGrip * stats.drift * Mathf.Lerp(1.08f, 0.92f, Mathf.InverseLerp(0.85f, 1.35f, stats.grip)), 0.24f, 1.35f);
            tuning.handbrakeRearGrip = Mathf.Clamp(source.handbrakeRearGrip / Mathf.Max(0.72f, stats.grip), 0.18f, 0.9f);
            tuning.brakeTorque = Mathf.Max(500f, source.brakeTorque * stats.brake);

            tuning.boostCapacity = Mathf.Max(15f, source.boostCapacity * stats.boostCapacity);
            tuning.boostTorqueMultiplier = Mathf.Max(1f, 1f + (source.boostTorqueMultiplier - 1f) * stats.boostPower);
            tuning.boostBurnPerSecond = Mathf.Max(1f, source.boostBurnPerSecond * Mathf.Lerp(0.92f, 1.18f, Mathf.InverseLerp(0.75f, 1.35f, stats.boostPower)));
            tuning.boostRegenPerSecond = Mathf.Max(1f, source.boostRegenPerSecond * Mathf.Lerp(0.92f, 1.12f, Mathf.InverseLerp(0.75f, 1.35f, stats.cooling)));
            tuning.boostCoolPerSecond = Mathf.Max(1f, source.boostCoolPerSecond * stats.cooling);
            tuning.boostHeatPerSecond = Mathf.Max(1f, source.boostHeatPerSecond * stats.heat / Mathf.Max(0.55f, Mathf.Sqrt(stats.cooling)));
        }

        public static float GetRamPowerMultiplier(VectorSSGarageSaveData save)
        {
            VectorSSTuningStats stats = BuildAppliedStats(save);
            return Mathf.Max(0.1f, stats.ram);
        }
    }
}
