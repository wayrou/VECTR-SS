using System;
using System.Collections.Generic;
using GTX.Data;
using GTX.Flow;
using GTX.Progression;
using GTX.Visuals;
using UnityEngine;

namespace GTX.Vehicle
{
    public sealed class VectorSSVehicleModuleController : MonoBehaviour
    {
        private const string HeatGaugeId = "heat_gauge";
        private const string ClutchKickId = "clutch_kick_assist";
        private const string BoostValveId = "boost_valve_lever";
        private const string BrakeBiasId = "brake_bias_dial";
        private const string DifferentialLockId = "differential_lock_switch";
        private const string ArmorPlateId = "armor_plate_deploy";
        private const string SnapLeanId = "snap_lean_module";
        private const string RearBrakeSlideId = "rear_brake_slide_controller";

        [SerializeField] private VehicleController vehicle;
        [SerializeField] private Rigidbody body;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;

        private VehicleTuning tuning;
        private VectorSSPlayerProfile profile;
        private VectorSSVehicleDefinition vehicleDefinition;
        private readonly List<VectorSSModuleDefinition> installedModules = new List<VectorSSModuleDefinition>();
        private readonly HashSet<string> installedIds = new HashSet<string>();
        private Baseline baseline;
        private int boostValveMode = 1;
        private float brakeBias = 0.5f;
        private bool differentialLocked;
        private float nextClutchKickTime;
        private float armorActiveUntil;
        private float nextArmorTime;
        private float nextSnapLeanTime;
        private bool baselineCaptured;

        public event Action<string> FeedbackRaised;

        public IList<VectorSSModuleDefinition> InstalledModules
        {
            get { return installedModules.AsReadOnly(); }
        }

        public void Configure(VehicleController newVehicle, Rigidbody newBody, FlowState newFlowState, RuntimeImpactEffects newEffects, VectorSSPlayerProfile newProfile, VectorSSVehicleDefinition newVehicleDefinition)
        {
            vehicle = newVehicle;
            body = newBody != null ? newBody : GetComponent<Rigidbody>();
            flowState = newFlowState;
            effects = newEffects;
            profile = newProfile;
            vehicleDefinition = newVehicleDefinition;
            tuning = vehicle != null ? vehicle.Tuning : null;
            CaptureBaseline();
            RefreshInstalledModules();
            ApplyPassiveModuleEffects();
            ApplyRuntimeTuning();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }

            if (flowState == null)
            {
                flowState = GetComponent<FlowState>();
            }

            if (effects == null)
            {
                effects = GetComponent<RuntimeImpactEffects>();
            }
        }

        private void Update()
        {
            if (vehicle == null || tuning == null || profile == null || vehicleDefinition == null)
            {
                return;
            }

            HandleInputs();
            ApplyRuntimeTuning();
        }

        public bool HasModule(string moduleId)
        {
            return installedIds.Contains(moduleId);
        }

        public string GetWidgetValue(string moduleId)
        {
            if (moduleId == HeatGaugeId)
            {
                return Mathf.RoundToInt(vehicle.Heat01 * 100f) + "%";
            }

            if (moduleId == ClutchKickId)
            {
                return ClutchKickReady01() >= 1f ? "READY" : Mathf.CeilToInt((nextClutchKickTime - Time.time) * 10f) / 10f + "s";
            }

            if (moduleId == BoostValveId)
            {
                return BoostValveLabel();
            }

            if (moduleId == BrakeBiasId)
            {
                return Mathf.RoundToInt(brakeBias * 100f) + "% F";
            }

            if (moduleId == DifferentialLockId)
            {
                return differentialLocked ? "LOCKED" : "OPEN";
            }

            if (moduleId == ArmorPlateId)
            {
                if (ArmorActive01() > 0f)
                {
                    return "ACTIVE";
                }

                return ArmorReady01() >= 1f ? "READY" : Mathf.CeilToInt((nextArmorTime - Time.time) * 10f) / 10f + "s";
            }

            if (moduleId == SnapLeanId)
            {
                return SnapLeanReady01() >= 1f ? "READY" : Mathf.CeilToInt((nextSnapLeanTime - Time.time) * 10f) / 10f + "s";
            }

            if (moduleId == RearBrakeSlideId)
            {
                return vehicle.CurrentInput.handbrake ? "SLIDE" : "STANDBY";
            }

            return "--";
        }

        public float GetWidgetNormalized(string moduleId)
        {
            if (moduleId == HeatGaugeId)
            {
                return vehicle.Heat01;
            }

            if (moduleId == ClutchKickId)
            {
                return ClutchKickReady01();
            }

            if (moduleId == BoostValveId)
            {
                return boostValveMode / 2f;
            }

            if (moduleId == BrakeBiasId)
            {
                return brakeBias;
            }

            if (moduleId == DifferentialLockId)
            {
                return differentialLocked ? 1f : 0f;
            }

            if (moduleId == ArmorPlateId)
            {
                float active = ArmorActive01();
                return active > 0f ? active : ArmorReady01();
            }

            if (moduleId == SnapLeanId)
            {
                return SnapLeanReady01();
            }

            if (moduleId == RearBrakeSlideId)
            {
                return vehicle.CurrentInput.handbrake ? 1f : 0.35f;
            }

            return 0f;
        }

        public List<string> BuildControlHints()
        {
            List<string> hints = new List<string>();
            for (int i = 0; i < installedModules.Count; i++)
            {
                VectorSSModuleDefinition module = installedModules[i];
                if (module.category == VectorSSModuleCategory.ActiveControl || module.category == VectorSSModuleCategory.Combat)
                {
                    if (!string.IsNullOrEmpty(module.controlHint))
                    {
                        hints.Add(module.controlHint);
                    }
                }
            }

            return hints;
        }

        private void RefreshInstalledModules()
        {
            installedModules.Clear();
            installedIds.Clear();
            if (profile == null || vehicleDefinition == null)
            {
                return;
            }

            HashSet<string> installed = profile.InstalledModulesFor(vehicleDefinition.id, false);
            if (installed == null)
            {
                return;
            }

            foreach (string moduleId in installed)
            {
                VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                if (module != null && module.Supports(vehicleDefinition))
                {
                    installedModules.Add(module);
                    installedIds.Add(module.id);
                }
            }
        }

        private void CaptureBaseline()
        {
            if (baselineCaptured || tuning == null)
            {
                return;
            }

            baseline = new Baseline(tuning, body);
            baselineCaptured = true;
        }

        private void ApplyPassiveModuleEffects()
        {
            if (!baselineCaptured || tuning == null)
            {
                return;
            }

            if (HasModule(RearBrakeSlideId))
            {
                tuning.handbrakeRearGrip *= 1.18f;
                tuning.driftYawAssist *= 1.08f;
            }
        }

        private void HandleInputs()
        {
            if (HasModule(ClutchKickId) && Input.GetKeyDown(KeyCode.X))
            {
                TryClutchKickAssist();
            }

            if (HasModule(BoostValveId) && Input.GetKeyDown(KeyCode.V))
            {
                boostValveMode = (boostValveMode + 1) % 3;
                RaiseFeedback("BOOST VALVE " + BoostValveLabel());
            }

            if (HasModule(BrakeBiasId))
            {
                if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
                {
                    brakeBias = Mathf.Clamp01(brakeBias - 0.1f);
                    RaiseFeedback("BRAKE BIAS " + Mathf.RoundToInt(brakeBias * 100f) + "% F");
                }

                if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
                {
                    brakeBias = Mathf.Clamp01(brakeBias + 0.1f);
                    RaiseFeedback("BRAKE BIAS " + Mathf.RoundToInt(brakeBias * 100f) + "% F");
                }
            }

            if (HasModule(DifferentialLockId) && Input.GetKeyDown(KeyCode.G))
            {
                differentialLocked = !differentialLocked;
                RaiseFeedback(differentialLocked ? "DIFF LOCK" : "DIFF OPEN");
            }

            if (HasModule(ArmorPlateId) && Input.GetKeyDown(KeyCode.B))
            {
                TryDeployArmor();
            }

            if (HasModule(SnapLeanId) && vehicleDefinition.isBike && Input.GetKeyDown(KeyCode.LeftAlt))
            {
                TrySnapLean();
            }
        }

        private void TryClutchKickAssist()
        {
            if (Time.time < nextClutchKickTime || body == null || vehicle == null)
            {
                return;
            }

            nextClutchKickTime = Time.time + 3.4f;
            VehicleInputState input = vehicle.CurrentInput;
            float side = Mathf.Abs(input.steer) > 0.08f ? Mathf.Sign(input.steer) : 1f;
            vehicle.Drift.TriggerAssistedClutchKick(tuning, body, input, 1.18f);
            body.AddRelativeTorque(Vector3.up * side * 4.4f, ForceMode.VelocityChange);
            body.AddForce((transform.right * side * 1.65f) + (transform.forward * 2.4f), ForceMode.Impulse);
            flowState?.AddFlow(vehicleDefinition.isBike ? 6f : 4f);
            effects?.PlaySkid(transform.position - transform.forward * 0.55f, transform.forward, 0.82f);
            RaiseFeedback("CLUTCH KICK");
        }

        private void TryDeployArmor()
        {
            if (Time.time < nextArmorTime)
            {
                return;
            }

            armorActiveUntil = Time.time + 2.35f;
            nextArmorTime = Time.time + 8.5f;
            flowState?.AddFlow(2f);
            effects?.PlayImpactBurst(transform.position + transform.forward * 0.8f + Vector3.up * 0.6f, -transform.forward, 0.62f);
            RaiseFeedback("ARMOR PLATES");
        }

        private void TrySnapLean()
        {
            if (Time.time < nextSnapLeanTime || body == null)
            {
                return;
            }

            nextSnapLeanTime = Time.time + 2.6f;
            float side = Mathf.Abs(vehicle.CurrentInput.steer) > 0.08f ? Mathf.Sign(vehicle.CurrentInput.steer) : 1f;
            body.AddForce((transform.right * side * 4.8f) + (transform.forward * 1.6f), ForceMode.Impulse);
            body.AddTorque(transform.forward * -side * 4.2f, ForceMode.VelocityChange);
            flowState?.AddFlow(8f);
            effects?.PlaySpeedLines(transform, 0.82f);
            RaiseFeedback("SNAP LEAN");
        }

        private void ApplyRuntimeTuning()
        {
            if (!baselineCaptured || tuning == null)
            {
                return;
            }

            float valvePower = 1f;
            float valveBurn = 1f;
            float valveHeat = 1f;
            if (HasModule(BoostValveId))
            {
                if (boostValveMode == 0)
                {
                    valvePower = 0.82f;
                    valveBurn = 0.72f;
                    valveHeat = 0.68f;
                }
                else if (boostValveMode == 2)
                {
                    valvePower = 1.26f;
                    valveBurn = 1.42f;
                    valveHeat = 1.58f;
                }
            }

            float diffGrip = differentialLocked && HasModule(DifferentialLockId) ? 1.14f : 1f;
            float diffYaw = differentialLocked && HasModule(DifferentialLockId) ? 0.72f : 1f;
            float armorMass = ArmorActive01() > 0f && HasModule(ArmorPlateId) ? 1.14f : 1f;

            tuning.boostTorqueMultiplier = baseline.boostTorqueMultiplier * valvePower;
            tuning.boostBurnPerSecond = baseline.boostBurnPerSecond * valveBurn;
            tuning.boostHeatPerSecond = baseline.boostHeatPerSecond * valveHeat;
            tuning.normalSideGrip = baseline.normalSideGrip * diffGrip;
            tuning.driftSideGrip = baseline.driftSideGrip * (differentialLocked && HasModule(DifferentialLockId) ? 1.08f : 1f);
            tuning.arcadeYawAssist = baseline.arcadeYawAssist * diffYaw;
            tuning.tractionControl = differentialLocked && HasModule(DifferentialLockId) ? Mathf.Max(baseline.tractionControl, 0.58f) : baseline.tractionControl;
            tuning.handbrakeRearGrip = baseline.handbrakeRearGrip * (HasModule(RearBrakeSlideId) ? 1.18f : 1f);
            tuning.driftYawAssist = baseline.driftYawAssist * (HasModule(RearBrakeSlideId) ? 1.08f : 1f);

            if (vehicle != null)
            {
                vehicle.RuntimeBrakeBias = HasModule(BrakeBiasId) ? brakeBias : 0.5f;
            }

            if (body != null)
            {
                body.mass = baseline.bodyMass * armorMass;
                body.angularDrag = baseline.angularDrag * (differentialLocked && HasModule(DifferentialLockId) ? 1.22f : 1f);
            }
        }

        private string BoostValveLabel()
        {
            if (boostValveMode == 0)
            {
                return "LOW";
            }

            if (boostValveMode == 2)
            {
                return "HIGH";
            }

            return "MED";
        }

        private float ClutchKickReady01()
        {
            return Mathf.Clamp01(1f - Mathf.Max(0f, nextClutchKickTime - Time.time) / 3.4f);
        }

        private float ArmorReady01()
        {
            return Mathf.Clamp01(1f - Mathf.Max(0f, nextArmorTime - Time.time) / 8.5f);
        }

        private float ArmorActive01()
        {
            return Mathf.Clamp01(Mathf.Max(0f, armorActiveUntil - Time.time) / 2.35f);
        }

        private float SnapLeanReady01()
        {
            return Mathf.Clamp01(1f - Mathf.Max(0f, nextSnapLeanTime - Time.time) / 2.6f);
        }

        private void RaiseFeedback(string message)
        {
            FeedbackRaised?.Invoke(message);
        }

        private readonly struct Baseline
        {
            public readonly float boostTorqueMultiplier;
            public readonly float boostBurnPerSecond;
            public readonly float boostHeatPerSecond;
            public readonly float normalSideGrip;
            public readonly float driftSideGrip;
            public readonly float arcadeYawAssist;
            public readonly float tractionControl;
            public readonly float handbrakeRearGrip;
            public readonly float driftYawAssist;
            public readonly float bodyMass;
            public readonly float angularDrag;

            public Baseline(VehicleTuning tuning, Rigidbody body)
            {
                boostTorqueMultiplier = tuning != null ? tuning.boostTorqueMultiplier : 1f;
                boostBurnPerSecond = tuning != null ? tuning.boostBurnPerSecond : 28f;
                boostHeatPerSecond = tuning != null ? tuning.boostHeatPerSecond : 34f;
                normalSideGrip = tuning != null ? tuning.normalSideGrip : 1f;
                driftSideGrip = tuning != null ? tuning.driftSideGrip : 0.44f;
                arcadeYawAssist = tuning != null ? tuning.arcadeYawAssist : 0.9f;
                tractionControl = tuning != null ? tuning.tractionControl : 0.28f;
                handbrakeRearGrip = tuning != null ? tuning.handbrakeRearGrip : 0.24f;
                driftYawAssist = tuning != null ? tuning.driftYawAssist : 2.65f;
                bodyMass = body != null ? body.mass : tuning != null ? tuning.mass : 1350f;
                angularDrag = body != null ? body.angularDrag : 1.2f;
            }
        }
    }
}
