using GTX.Combat;
using GTX.Data;
using GTX.UI;
using GTX.Vehicle;
using UnityEngine;

namespace GTX.Core
{
    public sealed class GTXRuntimeTuningApplier : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private VehicleTuning tuning;
        [SerializeField] private GTXRuntimeHUD hud;
        [SerializeField] private SideSlamController sideSlam;
        [SerializeField] private BoostRamDetector boostRam;

        private Baseline baseline;
        private bool hasBaseline;

        public void Configure(VehicleController newVehicle, VehicleTuning newTuning, GTXRuntimeHUD newHud, SideSlamController newSideSlam, BoostRamDetector newBoostRam)
        {
            vehicle = newVehicle;
            tuning = newTuning;
            hud = newHud;
            sideSlam = newSideSlam;
            boostRam = newBoostRam;
            CaptureBaseline();
            if (hud != null)
            {
                hud.TuningChanged += ApplyProfile;
                ApplyProfile(hud.Tuning);
            }
        }

        private void OnDestroy()
        {
            if (hud != null)
            {
                hud.TuningChanged -= ApplyProfile;
            }
        }

        private void CaptureBaseline()
        {
            if (tuning == null)
            {
                return;
            }

            baseline = new Baseline(tuning);
            hasBaseline = true;
        }

        private void ApplyProfile(GTXTuningProfile profile)
        {
            if (profile == null || tuning == null || !hasBaseline)
            {
                return;
            }

            GTXCarClassPreset carClass = PresetForHud();
            tuning.mass = baseline.mass * carClass.massMultiplier;
            tuning.peakTorque = baseline.peakTorque * profile.acceleration * carClass.engineMultiplier;
            tuning.maxDriveTorque = baseline.maxDriveTorque * profile.acceleration;
            tuning.rigidbodyFallbackDriveForce = baseline.fallbackDriveForce * profile.acceleration;
            tuning.finalDrive = baseline.finalDrive / Mathf.Max(0.35f, profile.topSpeed);
            tuning.redlineRpm = baseline.redlineRpm * Mathf.Lerp(0.96f, 1.08f, Mathf.InverseLerp(0.6f, 1.6f, profile.topSpeed));
            tuning.downforce = baseline.downforce * Mathf.Lerp(0.88f, 1.18f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));

            tuning.normalSideGrip = baseline.normalSideGrip * profile.grip * carClass.gripMultiplier;
            tuning.driftSideGrip = Mathf.Clamp(baseline.driftSideGrip * Mathf.Lerp(1.16f, 0.82f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip)), 0.32f, 1.1f);
            tuning.handbrakeRearGrip = Mathf.Clamp(baseline.handbrakeRearGrip / Mathf.Max(0.65f, profile.grip), 0.24f, 0.72f);
            tuning.steeringAngle = baseline.steeringAngle * profile.steeringResponse * carClass.steeringMultiplier;
            tuning.steeringAngleAtSpeed = baseline.steeringAngleAtSpeed * Mathf.Lerp(0.9f, 1.18f, profile.steeringResponse);
            tuning.arcadeYawAssist = baseline.arcadeYawAssist * Mathf.Lerp(0.9f, 1.1f, Mathf.InverseLerp(0.4f, 1.8f, profile.steeringResponse));
            tuning.steeringInputRiseRate = baseline.steeringInputRiseRate * Mathf.Lerp(0.86f, 1.34f, Mathf.InverseLerp(0.4f, 1.8f, profile.steeringResponse));
            tuning.steeringInputFallRate = baseline.steeringInputFallRate * Mathf.Lerp(0.94f, 1.24f, Mathf.InverseLerp(0.4f, 1.8f, profile.steeringResponse));
            tuning.lowSpeedSteeringAssist = baseline.lowSpeedSteeringAssist * Mathf.Lerp(0.86f, 1.18f, Mathf.InverseLerp(0.4f, 1.8f, profile.steeringResponse));
            tuning.highSpeedSteeringStability = baseline.highSpeedSteeringStability * Mathf.Lerp(1.16f, 0.9f, Mathf.InverseLerp(0.4f, 1.8f, profile.steeringResponse)) * Mathf.Lerp(0.94f, 1.14f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.brakeTorque = baseline.brakeTorque * profile.brakePower;
            tuning.driftSustain = baseline.driftSustain * Mathf.Lerp(1.16f, 0.9f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.driftExitRecovery = baseline.driftExitRecovery * Mathf.Lerp(0.86f, 1.18f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.driftExitYawDamping = baseline.driftExitYawDamping * Mathf.Lerp(0.9f, 1.24f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.driftExitHoldSeconds = baseline.driftExitHoldSeconds * Mathf.Lerp(1.08f, 0.88f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.driftLateralDamping = baseline.driftLateralDamping * Mathf.Lerp(0.9f, 1.18f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));
            tuning.driftHandbrakeEntryKick = baseline.driftHandbrakeEntryKick * Mathf.Lerp(1.12f, 0.92f, Mathf.InverseLerp(0.6f, 1.8f, profile.grip));

            tuning.boostCapacity = baseline.boostCapacity * Mathf.Lerp(0.82f, 1.28f, Mathf.InverseLerp(0.4f, 1.8f, profile.boostPower));
            tuning.boostTorqueMultiplier = 1f + (baseline.boostTorqueMultiplier - 1f) * profile.boostPower * carClass.boostMultiplier;
            tuning.boostBurnPerSecond = baseline.boostBurnPerSecond * Mathf.Lerp(0.85f, 1.22f, Mathf.InverseLerp(0.4f, 1.8f, profile.boostPower));
            tuning.boostCoolPerSecond = baseline.boostCoolPerSecond * profile.cooling;
            tuning.boostHeatPerSecond = baseline.boostHeatPerSecond * Mathf.Lerp(1.28f, 0.72f, Mathf.InverseLerp(0.5f, 1.8f, profile.cooling)) * (hud != null && hud.ActivePreset == GTXTuningPreset.Volt ? 1.18f : 1f);

            sideSlam?.SetPowerMultiplier(carClass.ramMultiplier);
            boostRam?.SetPowerMultiplier(carClass.ramMultiplier * Mathf.Lerp(0.9f, 1.15f, Mathf.InverseLerp(0.4f, 1.8f, profile.boostPower)));
            vehicle?.ApplyTuning(tuning, false);
        }

        private GTXCarClassPreset PresetForHud()
        {
            if (hud == null)
            {
                return GTXCarClassPreset.Create(GTXCarClass.Strike);
            }

            switch (hud.ActivePreset)
            {
                case GTXTuningPreset.Drift:
                    return GTXCarClassPreset.Create(GTXCarClass.Drift);
                case GTXTuningPreset.Volt:
                    return GTXCarClassPreset.Create(GTXCarClass.Volt);
                default:
                    return GTXCarClassPreset.Create(GTXCarClass.Strike);
            }
        }

        private readonly struct Baseline
        {
            public readonly float mass;
            public readonly float peakTorque;
            public readonly float maxDriveTorque;
            public readonly float fallbackDriveForce;
            public readonly float finalDrive;
            public readonly float redlineRpm;
            public readonly float downforce;
            public readonly float normalSideGrip;
            public readonly float driftSideGrip;
            public readonly float handbrakeRearGrip;
            public readonly float steeringAngle;
            public readonly float steeringAngleAtSpeed;
            public readonly float steeringInputRiseRate;
            public readonly float steeringInputFallRate;
            public readonly float lowSpeedSteeringAssist;
            public readonly float highSpeedSteeringStability;
            public readonly float arcadeYawAssist;
            public readonly float brakeTorque;
            public readonly float driftSustain;
            public readonly float driftExitRecovery;
            public readonly float driftExitYawDamping;
            public readonly float driftExitHoldSeconds;
            public readonly float driftLateralDamping;
            public readonly float driftHandbrakeEntryKick;
            public readonly float boostCapacity;
            public readonly float boostTorqueMultiplier;
            public readonly float boostBurnPerSecond;
            public readonly float boostCoolPerSecond;
            public readonly float boostHeatPerSecond;

            public Baseline(VehicleTuning tuning)
            {
                mass = tuning.mass;
                peakTorque = tuning.peakTorque;
                maxDriveTorque = tuning.maxDriveTorque;
                fallbackDriveForce = tuning.rigidbodyFallbackDriveForce;
                finalDrive = tuning.finalDrive;
                redlineRpm = tuning.redlineRpm;
                downforce = tuning.downforce;
                normalSideGrip = tuning.normalSideGrip;
                driftSideGrip = tuning.driftSideGrip;
                handbrakeRearGrip = tuning.handbrakeRearGrip;
                steeringAngle = tuning.steeringAngle;
                steeringAngleAtSpeed = tuning.steeringAngleAtSpeed;
                steeringInputRiseRate = tuning.steeringInputRiseRate;
                steeringInputFallRate = tuning.steeringInputFallRate;
                lowSpeedSteeringAssist = tuning.lowSpeedSteeringAssist;
                highSpeedSteeringStability = tuning.highSpeedSteeringStability;
                arcadeYawAssist = tuning.arcadeYawAssist;
                brakeTorque = tuning.brakeTorque;
                driftSustain = tuning.driftSustain;
                driftExitRecovery = tuning.driftExitRecovery;
                driftExitYawDamping = tuning.driftExitYawDamping;
                driftExitHoldSeconds = tuning.driftExitHoldSeconds;
                driftLateralDamping = tuning.driftLateralDamping;
                driftHandbrakeEntryKick = tuning.driftHandbrakeEntryKick;
                boostCapacity = tuning.boostCapacity;
                boostTorqueMultiplier = tuning.boostTorqueMultiplier;
                boostBurnPerSecond = tuning.boostBurnPerSecond;
                boostCoolPerSecond = tuning.boostCoolPerSecond;
                boostHeatPerSecond = tuning.boostHeatPerSecond;
            }
        }
    }
}
