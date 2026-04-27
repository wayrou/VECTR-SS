using System.Collections.Generic;
using GTX.Combat;
using GTX.Data;
using GTX.Flow;
using GTX.Progression;
using GTX.UI;
using GTX.Vehicle;
using GTX.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GTX.Core
{
    public sealed class GTXBootstrapper : MonoBehaviour
    {
        private Material roadMaterial;
        private Material barrierMaterial;
        private Material stripeMaterial;
        private Material playerMaterial;
        private Material playerAccentMaterial;
        private Material playerSecondaryMaterial;
        private Material glassMaterial;
        private Material rivalMaterial;
        private Material wheelMaterial;
        private Material outlineMaterial;
        private Material inkMaterial;
        private Material boostTrailMaterial;
        private Material trackMarkerMaterial;
        private Material trackMarkerBlueMaterial;
        private Material desertMaterial;
        private Material pitFloorMaterial;
        private Material pitPropMaterial;
        private bool hasInvertedHullOutline;
        private VectorSSPlayerProfile playerProfile;
        private VectorSSScreen screen = VectorSSScreen.MainMenu;
        private VectorSSMapDefinition selectedMap;
        private VectorSSVehicleDefinition selectedVehicle;
        private VectorSSRaceResult lastResult;
        private Transform sessionRoot;
        private RuntimeTrackRoute activeRoute;
        private PlayerRig activePlayer;
        private SimpleRouteRivalAI activeRivalAi;
        private VectorSSVehicleModuleController activeModuleController;
        private VectorSSModuleHUD activeModuleHud;
        private bool hasActivePlayer;
        private float raceStartTime;
        private float bestRouteDistance;
        private float currentRouteDistance;
        private int currentLap;
        private int targetLaps = 1;
        private int nextCheckpointIndex;
        private float nextRazorNearMissTime;
        private int combatScore;
        private Vector2 garageScroll;
        private Vector2 menuScroll;
        private string garageMessage = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle scrollStyle;
        private Texture2D panelTexture;
        private bool startRaceQueued;
        private static readonly float[] CheckpointFractions = { 0.25f, 0.5f, 0.75f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<GTXBootstrapper>() != null)
            {
                return;
            }

            new GameObject("GTX Bootstrapper").AddComponent<GTXBootstrapper>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            BuildMaterials();
            BuildLighting();
            EnsureEventSystem();
            EnsureProgressionState();
            screen = VectorSSScreen.MainMenu;
            SetRaceHudVisible(false);
        }

        private void Update()
        {
            ProcessQueuedGuiActions();

            if (screen == VectorSSScreen.Racing && hasActivePlayer)
            {
                UpdateRaceCompletion();
                UpdateRazorNearMissFlow();
                UpdateModuleHud();
            }
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            EnsureProgressionState();
            GUI.backgroundColor = Color.white;
            if (screen == VectorSSScreen.Racing)
            {
                DrawRaceOverlay();
                return;
            }

            Rect panel = new Rect(36f, 36f, 520f, Screen.height - 72f);
            GUILayout.BeginArea(panel, panelStyle);
            menuScroll = GUILayout.BeginScrollView(menuScroll, scrollStyle);
            GUILayout.Label("VECTOR SS", titleStyle);
            GUILayout.Label("0.1.0 Blackline Prototype", bodyStyle);
            GUILayout.Space(12f);

            switch (screen)
            {
                case VectorSSScreen.MapSelect:
                    DrawMapSelect();
                    break;
                case VectorSSScreen.VehicleSelect:
                    DrawVehicleSelect();
                    break;
                case VectorSSScreen.Garage:
                    DrawGarage();
                    break;
                case VectorSSScreen.Results:
                    DrawResults();
                    break;
                default:
                    DrawMainMenu();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void BuildMaterials()
        {
            roadMaterial = CreateMaterial("GTX Warm Ink Asphalt", new Color(0.18f, 0.20f, 0.22f, 1f), true);
            barrierMaterial = CreateMaterial("GTX Sunbleached Barrier", new Color(0.86f, 0.82f, 0.70f, 1f), true);
            stripeMaterial = CreateMaterial("GTX Print Blue Stripe", new Color(0.04f, 0.42f, 0.83f, 1f), true);
            playerMaterial = CreateMaterial("GTX Rally Shell White", new Color(0.94f, 0.90f, 0.78f, 1f), true);
            playerAccentMaterial = CreateMaterial("GTX Safety Orange", new Color(1f, 0.34f, 0.05f, 1f), true);
            playerSecondaryMaterial = CreateMaterial("GTX Package Blue", new Color(0.03f, 0.24f, 0.72f, 1f), true);
            glassMaterial = CreateMaterial("GTX Painted Glass Blue", new Color(0.45f, 0.74f, 0.92f, 1f), true);
            rivalMaterial = CreateMaterial("GTX Rival Lime Rally", new Color(0.62f, 0.82f, 0.12f, 1f), true);
            wheelMaterial = CreateMaterial("GTX Wheel Charcoal", new Color(0.045f, 0.05f, 0.055f, 1f), true);
            outlineMaterial = CreateOutlineMaterial("GTX Inverted Hull Ink", new Color(0.006f, 0.009f, 0.02f, 1f));
            inkMaterial = CreateMaterial("GTX Solid Ink Navy", new Color(0.006f, 0.009f, 0.02f, 1f), false);
            boostTrailMaterial = CreateMaterial("GTX Cyan Boost Print", new Color(0.08f, 0.78f, 1f, 0.92f), false);
            trackMarkerMaterial = CreateMaterial("GTX Track Marker Orange", new Color(1f, 0.44f, 0.08f, 1f), true);
            trackMarkerBlueMaterial = CreateMaterial("GTX Track Marker Blue", new Color(0.05f, 0.42f, 0.96f, 1f), true);
            desertMaterial = CreateMaterial("GTX Desert Board Sand", new Color(0.80f, 0.65f, 0.42f, 1f), true);
            pitFloorMaterial = CreateMaterial("GTX Pit Mat Bluegray", new Color(0.24f, 0.31f, 0.36f, 1f), true);
            pitPropMaterial = CreateMaterial("GTX Pit Prop Cream", new Color(0.92f, 0.84f, 0.62f, 1f), true);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.82f, 0.88f, 0.92f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.58f, 0.49f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.29f, 0.21f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.78f, 0.83f, 0.82f, 1f);
            RenderSettings.fogDensity = 0.0045f;

            GameObject lightObject = new GameObject("GTX Hard Toon Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.48f;
            sun.color = new Color(1f, 0.92f, 0.74f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void EnsureProgressionState()
        {
            if (playerProfile == null)
            {
                playerProfile = VectorSSSaveSystem.Load();
            }

            if (selectedMap == null)
            {
                selectedMap = VectorSSCatalog.GetMap(playerProfile.selectedMap);
            }

            if (selectedVehicle == null)
            {
                selectedVehicle = VectorSSCatalog.GetVehicle(playerProfile.selectedVehicle);
            }
        }

        private void DrawMainMenu()
        {
            GUILayout.Label("Complete loop: map select -> vehicle select -> race -> resources -> garage -> race again.", bodyStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Selected Map", headerStyle);
            GUILayout.Label(selectedMap.displayName + " - " + selectedMap.purpose, bodyStyle);
            GUILayout.Label("Selected Vehicle", headerStyle);
            GUILayout.Label(selectedVehicle.fullName + " - " + selectedVehicle.role, bodyStyle);
            GUILayout.Space(12f);
            if (GUILayout.Button("Start Game", buttonStyle, GUILayout.Height(42f)))
            {
                screen = VectorSSScreen.MapSelect;
            }

            if (GUILayout.Button("Garage", buttonStyle, GUILayout.Height(38f)))
            {
                screen = VectorSSScreen.Garage;
            }

            if (GUILayout.Button("Quick Race With Current Setup", buttonStyle, GUILayout.Height(38f)))
            {
                startRaceQueued = true;
            }

            GUILayout.Space(12f);
            GUILayout.Label(playerProfile.resources.ToString(), headerStyle);
        }

        private void DrawMapSelect()
        {
            GUILayout.Label("Choose Map", headerStyle);
            for (int i = 0; i < VectorSSCatalog.Maps.Length; i++)
            {
                VectorSSMapDefinition map = VectorSSCatalog.Maps[i];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(map.displayName, headerStyle);
                GUILayout.Label(map.theme, bodyStyle);
                GUILayout.Label("Laps: " + map.lapCount + "   Reward bias: +" + map.mapBonus, bodyStyle);
                if (GUILayout.Button(playerProfile.selectedMap == map.id ? "Selected" : "Select " + map.displayName, buttonStyle, GUILayout.Height(34f)))
                {
                    playerProfile.selectedMap = map.id;
                    selectedMap = map;
                    VectorSSSaveSystem.Save(playerProfile);
                }

                GUILayout.EndVertical();
                GUILayout.Space(8f);
            }

            if (GUILayout.Button("Continue To Vehicle Select", buttonStyle, GUILayout.Height(40f)))
            {
                screen = VectorSSScreen.VehicleSelect;
            }

            if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(32f)))
            {
                screen = VectorSSScreen.MainMenu;
            }
        }

        private void DrawVehicleSelect()
        {
            GUILayout.Label("Choose Vehicle", headerStyle);
            for (int i = 0; i < VectorSSCatalog.Vehicles.Length; i++)
            {
                VectorSSVehicleDefinition vehicle = VectorSSCatalog.Vehicles[i];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(vehicle.fullName, headerStyle);
                GUILayout.Label(vehicle.vehicleClass + " - " + vehicle.role, bodyStyle);
                GUILayout.Label(vehicle.StatsLine, bodyStyle);
                GUILayout.Label("Primary resource: " + vehicle.primaryResource, bodyStyle);
                if (GUILayout.Button(playerProfile.selectedVehicle == vehicle.id ? "Selected" : "Select " + vehicle.displayName, buttonStyle, GUILayout.Height(34f)))
                {
                    playerProfile.selectedVehicle = vehicle.id;
                    selectedVehicle = vehicle;
                    VectorSSSaveSystem.Save(playerProfile);
                }

                GUILayout.EndVertical();
                GUILayout.Space(8f);
            }

            if (GUILayout.Button("Race " + selectedMap.displayName, buttonStyle, GUILayout.Height(42f)))
            {
                startRaceQueued = true;
            }

            if (GUILayout.Button("Back To Map Select", buttonStyle, GUILayout.Height(32f)))
            {
                screen = VectorSSScreen.MapSelect;
            }
        }

        private void DrawGarage()
        {
            GUILayout.Label("Garage", headerStyle);
            GUILayout.Label(playerProfile.resources.ToString(), headerStyle);
            GUILayout.Label("Current vehicle: " + selectedVehicle.fullName, bodyStyle);
            GUILayout.Space(8f);

            garageScroll = GUILayout.BeginScrollView(garageScroll, scrollStyle, GUILayout.Height(Mathf.Min(520f, Screen.height - 250f)));
            DrawTuningSlider("Steering Sensitivity", ref playerProfile.tuning.steering, 0.45f, 1.85f);
            DrawTuningSlider("Brake Bias / Power", ref playerProfile.tuning.brakeBias, 0.45f, 1.85f);
            DrawTuningSlider("Drift Grip", ref playerProfile.tuning.driftGrip, 0.45f, 1.85f);
            DrawTuningSlider("Final Drive", ref playerProfile.tuning.finalDrive, 0.65f, 1.45f);
            DrawTuningSlider("Boost Valve", ref playerProfile.tuning.boostValve, 0.45f, 1.85f);
            DrawTuningSlider("Suspension", ref playerProfile.tuning.suspension, 0.55f, 1.65f);
            DrawTuningSlider("Tire Grip", ref playerProfile.tuning.tireGrip, 0.55f, 1.85f);
            DrawTuningSlider("Clutch Bite", ref playerProfile.tuning.clutchBite, 0.55f, 1.8f);
            DrawTuningSlider("Outline Thickness", ref playerProfile.tuning.outlineThickness, 0.65f, 1.6f);
            DrawTuningSlider("Camera Shake", ref playerProfile.tuning.cameraShake, 0f, 1f);
            if (selectedVehicle.isBike)
            {
                DrawTuningSlider("Lean Response", ref playerProfile.tuning.leanResponse, 0.45f, 1.8f);
                DrawTuningSlider("Rear Brake Slide", ref playerProfile.tuning.rearBrakeSlide, 0.45f, 1.85f);
            }

            GUILayout.Space(12f);
            GUILayout.Label("Upgrades", headerStyle);
            for (int i = 0; i < VectorSSCatalog.Upgrades.Length; i++)
            {
                DrawUpgrade(VectorSSCatalog.Upgrades[i]);
            }

            GUILayout.Space(12f);
            GUILayout.Label("Modules", headerStyle);
            GUILayout.Label(SlotUsageText(selectedVehicle), bodyStyle);
            if (!string.IsNullOrEmpty(garageMessage))
            {
                GUILayout.Label(garageMessage, bodyStyle);
            }

            for (int i = 0; i < VectorSSCatalog.Modules.Length; i++)
            {
                DrawModule(VectorSSCatalog.Modules[i]);
            }

            DrawModuleLayoutEditor();

            GUILayout.EndScrollView();

            if (GUILayout.Button("Save Garage", buttonStyle, GUILayout.Height(36f)))
            {
                VectorSSSaveSystem.Save(playerProfile);
            }

            if (GUILayout.Button("Race Again", buttonStyle, GUILayout.Height(42f)))
            {
                VectorSSSaveSystem.Save(playerProfile);
                screen = VectorSSScreen.MapSelect;
            }

            if (GUILayout.Button("Back To Main Menu", buttonStyle, GUILayout.Height(32f)))
            {
                VectorSSSaveSystem.Save(playerProfile);
                screen = VectorSSScreen.MainMenu;
            }
        }

        private void DrawResults()
        {
            GUILayout.Label("Race Complete", headerStyle);
            if (lastResult != null)
            {
                GUILayout.Label(lastResult.vehicle.displayName + " on " + lastResult.map.displayName, bodyStyle);
                GUILayout.Label("Time: " + lastResult.raceTime.ToString("0.0") + "s   Flow Style: " + Mathf.RoundToInt(lastResult.flow01 * 100f) + "%", bodyStyle);
                GUILayout.Label("Completion: " + lastResult.completionReward, bodyStyle);
                GUILayout.Label("Style/Combat: " + lastResult.styleReward, bodyStyle);
                GUILayout.Label("Map Bonus: " + lastResult.mapReward, bodyStyle);
                GUILayout.Label("Earned: " + lastResult.Total, headerStyle);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Totals: " + playerProfile.resources, headerStyle);
            if (GUILayout.Button("Continue To Garage", buttonStyle, GUILayout.Height(42f)))
            {
                screen = VectorSSScreen.Garage;
            }

            if (GUILayout.Button("Map Select", buttonStyle, GUILayout.Height(38f)))
            {
                screen = VectorSSScreen.MapSelect;
            }
        }

        private void DrawRaceOverlay()
        {
            Rect top = new Rect(Screen.width - 340f, 24f, 300f, 148f);
            GUI.Box(top, string.Empty);
            GUILayout.BeginArea(new Rect(top.x + 14f, top.y + 10f, top.width - 28f, top.height - 18f));
            GUILayout.Label(selectedMap.displayName, headerStyle);
            GUILayout.Label(selectedVehicle.displayName + "   Lap " + Mathf.Clamp(currentLap + 1, 1, targetLaps) + "/" + targetLaps, bodyStyle);
            GUILayout.Label("Progress " + Mathf.RoundToInt(RaceProgress01() * 100f) + "%   " + CheckpointStatusText(), bodyStyle);
            if (activeRivalAi != null)
            {
                GUILayout.Label("Rival " + Mathf.RoundToInt(activeRivalAi.Progress01 * 100f) + "%   Lap " + Mathf.Clamp(activeRivalAi.LapCount + 1, 1, targetLaps) + "/" + targetLaps, bodyStyle);
            }
            GUILayout.Label("Return through START after CP3.", bodyStyle);
            GUILayout.EndArea();
        }

        private string CheckpointStatusText()
        {
            if (nextCheckpointIndex < CheckpointFractions.Length)
            {
                return "CP" + (nextCheckpointIndex + 1) + "/" + CheckpointFractions.Length;
            }

            return "FINISH";
        }

        private void ProcessQueuedGuiActions()
        {
            if (startRaceQueued)
            {
                startRaceQueued = false;
                StartRace();
            }
        }

        private void DrawTuningSlider(string label, ref float value, float min, float max)
        {
            GUILayout.Label(label + "  " + value.ToString("0.00"), bodyStyle);
            float newValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(430f));
            if (!Mathf.Approximately(newValue, value))
            {
                value = newValue;
                VectorSSSaveSystem.Save(playerProfile);
            }
        }

        private void DrawUpgrade(VectorSSUpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            bool bought = playerProfile.HasUpgrade(upgrade.id);
            bool classMatch = upgrade.preferredClass == null || upgrade.preferredClass.Value == selectedVehicle.vehicleClass;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(upgrade.displayName + (bought ? "  PURCHASED" : string.Empty), headerStyle);
            GUILayout.Label(upgrade.description, bodyStyle);
            GUILayout.Label("Cost: " + upgrade.cost + (classMatch ? string.Empty : "   off-class but usable"), bodyStyle);
            GUI.enabled = !bought && playerProfile.resources.CanAfford(upgrade.cost);
            if (GUILayout.Button(bought ? "Installed" : "Purchase", buttonStyle, GUILayout.Height(30f)))
            {
                if (playerProfile.TryPurchase(upgrade))
                {
                    VectorSSSaveSystem.Save(playerProfile);
                }
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private void DrawModule(VectorSSModuleDefinition module)
        {
            if (module == null)
            {
                return;
            }

            bool supported = module.Supports(selectedVehicle);
            bool purchased = playerProfile.HasModule(module.id);
            bool installed = playerProfile.IsModuleInstalled(selectedVehicle.id, module.id);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(module.displayName + "  [" + module.category + " / " + module.slot + "]" + (installed ? "  INSTALLED" : purchased ? "  OWNED" : string.Empty), headerStyle);
            GUILayout.Label(module.description, bodyStyle);
            GUILayout.Label("Cost: " + module.cost + "   Widget: " + module.widget + (string.IsNullOrEmpty(module.controlHint) ? string.Empty : "   Control: " + module.controlHint), bodyStyle);
            if (!supported)
            {
                GUILayout.Label("Unsupported by " + selectedVehicle.displayName + ".", bodyStyle);
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = supported && !purchased && playerProfile.resources.CanAfford(module.cost);
            if (GUILayout.Button(purchased ? "Purchased" : "Buy", buttonStyle, GUILayout.Height(30f), GUILayout.Width(112f)))
            {
                if (playerProfile.TryPurchaseModule(module))
                {
                    garageMessage = "Purchased " + module.displayName + ".";
                    VectorSSSaveSystem.Save(playerProfile);
                }
            }

            GUI.enabled = supported && purchased && !installed;
            if (GUILayout.Button("Install", buttonStyle, GUILayout.Height(30f), GUILayout.Width(112f)))
            {
                string message;
                if (playerProfile.TryInstallModule(selectedVehicle, module, out message))
                {
                    garageMessage = message;
                    VectorSSSaveSystem.Save(playerProfile);
                }
                else
                {
                    garageMessage = message;
                }
            }

            GUI.enabled = installed;
            if (GUILayout.Button("Uninstall", buttonStyle, GUILayout.Height(30f), GUILayout.Width(112f)))
            {
                playerProfile.UninstallModule(selectedVehicle.id, module.id);
                garageMessage = "Uninstalled " + module.displayName + ".";
                VectorSSSaveSystem.Save(playerProfile);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawModuleLayoutEditor()
        {
            GUILayout.Space(12f);
            GUILayout.Label("HUD Layout", headerStyle);
            GUILayout.Label("Adjust installed module widgets. X/Y use the 1920x1080 HUD canvas; Save Garage persists positions.", bodyStyle);

            HashSet<string> installed = playerProfile.InstalledModulesFor(selectedVehicle.id, false);
            if (installed == null || installed.Count == 0)
            {
                GUILayout.Label("No installed modules yet.", bodyStyle);
                return;
            }

            foreach (string moduleId in installed)
            {
                VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                if (module == null || module.widget == VectorSSModuleWidget.None)
                {
                    continue;
                }

                VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, module, true);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(module.displayName, headerStyle);
                float layoutX = layout.position.x;
                float layoutY = layout.position.y;
                float layoutScale = layout.scale;
                DrawLayoutSlider("X", ref layoutX, 24f, 1700f);
                DrawLayoutSlider("Y", ref layoutY, -900f, -72f);
                DrawLayoutSlider("Scale", ref layoutScale, 0.65f, 1.25f);
                if (!Mathf.Approximately(layoutX, layout.position.x) || !Mathf.Approximately(layoutY, layout.position.y) || !Mathf.Approximately(layoutScale, layout.scale))
                {
                    layout.position = new Vector2(layoutX, layoutY);
                    layout.scale = layoutScale;
                    VectorSSSaveSystem.Save(playerProfile);
                }

                bool visible = GUILayout.Toggle(layout.visible, "Visible");
                if (visible != layout.visible)
                {
                    layout.visible = visible;
                    VectorSSSaveSystem.Save(playerProfile);
                }

                if (GUILayout.Button("Reset " + module.displayName + " Layout", buttonStyle, GUILayout.Height(28f)))
                {
                    layout.ResetToDefinition(selectedVehicle.id, module);
                    VectorSSSaveSystem.Save(playerProfile);
                }

                GUILayout.EndVertical();
            }
        }

        private void DrawLayoutSlider(string label, ref float value, float min, float max)
        {
            GUILayout.Label(label + "  " + value.ToString("0.00"), bodyStyle);
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(430f));
            if (!Mathf.Approximately(next, value))
            {
                value = next;
            }
        }

        private string SlotUsageText(VectorSSVehicleDefinition vehicle)
        {
            return "Slots  Sensor " + playerProfile.InstalledSlotCount(vehicle.id, VectorSSModuleSlot.Sensor) + "/" + vehicle.sensorSlots +
                "   Control " + playerProfile.InstalledSlotCount(vehicle.id, VectorSSModuleSlot.Control) + "/" + vehicle.controlSlots +
                "   Combat " + playerProfile.InstalledSlotCount(vehicle.id, VectorSSModuleSlot.Combat) + "/" + vehicle.combatSlots +
                "   Utility " + playerProfile.InstalledSlotCount(vehicle.id, VectorSSModuleSlot.Utility) + "/" + vehicle.utilitySlots;
        }

        private void StartRace()
        {
            ClearSession();
            selectedMap = VectorSSCatalog.GetMap(playerProfile.selectedMap);
            selectedVehicle = VectorSSCatalog.GetVehicle(playerProfile.selectedVehicle);
            ApplyMapPalette(selectedMap);
            ApplyVehiclePalette(selectedVehicle);

            sessionRoot = new GameObject("Vector SS Runtime Session").transform;
            Transform trackRoot = new GameObject("Vector SS Runtime Test Track").transform;
            trackRoot.SetParent(sessionRoot, false);
            activeRoute = BuildTrack(trackRoot, selectedMap);
            TrackPose resetPose = activeRoute.PoseAtDistance(3f);
            Transform resetPoint = CreateResetPoint(resetPose.position + Vector3.up * 0.28f, resetPose.rotation);
            resetPoint.SetParent(sessionRoot, true);

            activePlayer = CreatePlayerMachine(resetPoint, selectedVehicle);
            activeModuleController = activePlayer.moduleController;
            activePlayer.root.transform.SetParent(sessionRoot, true);
            float rivalStartDistance = Mathf.Min(activeRoute.TotalLength * 0.16f, 76f);
            TrackPose rivalPose = activeRoute.PoseAtDistance(rivalStartDistance);
            GameObject rival = CreateDummyRival(rivalPose.position + rivalPose.right * 1.8f + Vector3.up * 0.02f, rivalPose.rotation);
            rival.transform.SetParent(sessionRoot, true);
            activeRivalAi = rival.GetComponent<SimpleRouteRivalAI>();
            if (activeRivalAi != null)
            {
                float cruiseSpeed = selectedMap.id == VectorSSMapId.ScraplineYard ? 31f : selectedMap.id == VectorSSMapId.RubberRidge ? 27f : 33f;
                activeRivalAi.Configure(activeRoute.samples, activeRoute.TotalLength, rivalStartDistance, cruiseSpeed, selectedMap.id == VectorSSMapId.ScraplineYard ? 0.72f : 0.56f);
            }

            Camera camera = ConfigureCamera(activePlayer);
            ConfigureHud(activePlayer);
            SetRaceHudVisible(true);
            activePlayer.visuals.Configure(activePlayer.vehicle, activePlayer.flowState, activePlayer.effects, activePlayer.boostTrail);
            camera.GetComponent<GTXCameraRig>().Configure(activePlayer.root.transform, activePlayer.body, activePlayer.flowState);
            activePlayer.sideSlam.FeedbackRaised += delegate { combatScore += selectedVehicle.isBike ? 1 : 4; };
            activePlayer.boostRam.FeedbackRaised += delegate { combatScore += selectedVehicle.isBike ? 2 : 8; };

            hasActivePlayer = true;
            combatScore = 0;
            bestRouteDistance = 0f;
            currentRouteDistance = 0f;
            currentLap = 0;
            targetLaps = Mathf.Max(1, selectedMap.lapCount);
            nextCheckpointIndex = 0;
            raceStartTime = Time.time;
            nextRazorNearMissTime = 0f;
            screen = VectorSSScreen.Racing;
        }

        private void CompleteRace()
        {
            if (!hasActivePlayer)
            {
                return;
            }

            float flow01 = activePlayer.flowState != null ? activePlayer.flowState.Normalized : 0f;
            lastResult = VectorSSProgressionUtility.BuildRaceResult(selectedMap, selectedVehicle, Time.time - raceStartTime, flow01, combatScore);
            playerProfile.resources.Add(lastResult.Total);
            VectorSSSaveSystem.Save(playerProfile);
            ClearSession();
            SetRaceHudVisible(false);
            screen = VectorSSScreen.Results;
        }

        private void ClearSession()
        {
            hasActivePlayer = false;
            activeRivalAi = null;
            activeModuleController = null;
            if (activeModuleHud != null)
            {
                activeModuleHud.Clear();
            }

            if (sessionRoot != null)
            {
                Destroy(sessionRoot.gameObject);
                sessionRoot = null;
            }
        }

        private void SetRaceHudVisible(bool visible)
        {
            GTXRuntimeHUD hud = FindObjectOfType<GTXRuntimeHUD>();
            if (hud != null)
            {
                hud.SetRaceHudVisible(visible);
            }
        }

        private void UpdateRaceCompletion()
        {
            if (activePlayer.root == null || activeRoute.samples == null)
            {
                return;
            }

            float distance = activeRoute.DistanceAlongRoute(activePlayer.root.transform.position);
            currentRouteDistance = distance;
            bestRouteDistance = Mathf.Max(bestRouteDistance, distance);
            UpdateCheckpointProgress();
            if (CanCompleteLap(distance))
            {
                currentLap++;
                if (currentLap >= targetLaps)
                {
                    CompleteRace();
                    return;
                }

                bestRouteDistance = 0f;
                nextCheckpointIndex = 0;
            }
        }

        private void UpdateCheckpointProgress()
        {
            if (activeRoute.TotalLength <= 0.01f)
            {
                return;
            }

            while (nextCheckpointIndex < CheckpointFractions.Length && bestRouteDistance >= activeRoute.TotalLength * CheckpointFractions[nextCheckpointIndex])
            {
                nextCheckpointIndex++;
                activePlayer.flowState?.AddFlow(4f + nextCheckpointIndex * 1.5f);
                activePlayer.effects?.PlaySpeedLines(activePlayer.root.transform, 0.55f + nextCheckpointIndex * 0.1f);
            }
        }

        private bool CanCompleteLap(float distance)
        {
            if (activeRoute.TotalLength <= 0.01f || Time.time - raceStartTime < 10f)
            {
                return false;
            }

            bool allCheckpointsCleared = nextCheckpointIndex >= CheckpointFractions.Length;
            bool hasRunMostOfLap = bestRouteDistance > activeRoute.TotalLength * 0.86f;
            bool backAtStart = distance < 38f || distance > activeRoute.TotalLength - 18f;
            return allCheckpointsCleared && hasRunMostOfLap && backAtStart;
        }

        private void UpdateRazorNearMissFlow()
        {
            if (selectedVehicle == null || !selectedVehicle.isBike || Time.time < nextRazorNearMissTime || activePlayer.body == null)
            {
                return;
            }

            if (activePlayer.body.velocity.magnitude < 15f)
            {
                return;
            }

            Vector3 origin = activePlayer.root.transform.position + Vector3.up * 0.65f;
            bool leftNear = Physics.Raycast(origin, -activePlayer.root.transform.right, out RaycastHit leftHit, 1.55f, ~0, QueryTriggerInteraction.Ignore) && leftHit.rigidbody != activePlayer.body;
            bool rightNear = Physics.Raycast(origin, activePlayer.root.transform.right, out RaycastHit rightHit, 1.55f, ~0, QueryTriggerInteraction.Ignore) && rightHit.rigidbody != activePlayer.body;
            if (!leftNear && !rightNear)
            {
                return;
            }

            nextRazorNearMissTime = Time.time + 0.45f;
            activePlayer.flowState?.AddFlow(3.5f * selectedVehicle.nearMissFlowMultiplier);
            activePlayer.effects?.PlaySpeedLines(activePlayer.root.transform, 0.75f);
            combatScore += 1;
        }

        private float RaceProgress01()
        {
            if (activeRoute.TotalLength <= 0f)
            {
                return 0f;
            }

            float checkpointProgress = nextCheckpointIndex / (float)(CheckpointFractions.Length + 1);
            float distanceProgress = Mathf.Clamp01(bestRouteDistance / activeRoute.TotalLength);
            return Mathf.Max(checkpointProgress, distanceProgress);
        }

        private void ApplyMapPalette(VectorSSMapDefinition map)
        {
            SetMaterialColor(roadMaterial, map.roadColor);
            SetMaterialColor(desertMaterial, map.groundColor);
            SetMaterialColor(barrierMaterial, map.barrierColor);
        }

        private void ApplyVehiclePalette(VectorSSVehicleDefinition vehicle)
        {
            SetMaterialColor(playerMaterial, vehicle.bodyColor);
            SetMaterialColor(playerAccentMaterial, vehicle.accentColor);
            SetMaterialColor(playerSecondaryMaterial, vehicle.secondaryColor);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_ShadowColor"))
            {
                material.SetColor("_ShadowColor", Color.Lerp(color, new Color(0.02f, 0.025f, 0.04f, color.a), 0.46f));
            }
        }

        private void EnsureGuiStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.84f, 0.32f, 1f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box);
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.015f, 0.02f, 0.045f, 0.94f));
            panelTexture.Apply();
            panelStyle.normal.background = panelTexture;
            panelStyle.normal.textColor = Color.white;
            panelStyle.padding = new RectOffset(18, 18, 18, 18);
            scrollStyle = new GUIStyle(GUI.skin.scrollView);
            scrollStyle.normal.background = panelTexture;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        }

        private Transform CreateResetPoint(Vector3 position, Quaternion rotation)
        {
            GameObject reset = new GameObject("Player Reset Point");
            reset.transform.SetPositionAndRotation(position, rotation);
            return reset.transform;
        }

        private RuntimeTrackRoute BuildTrack(Transform root, VectorSSMapDefinition map)
        {
            RuntimeTrackRoute route = CreateRuntimeLoopRoute(map.id);
            CreateChamferedBox("Vector SS Ground Board", root, MapBoardCenter(map.id), Quaternion.identity, MapBoardScale(map.id), desertMaterial, false, 0.035f);
            LowPolyMeshFactory.CreateTrackRibbon("Vector SS " + map.displayName + " Surface", root, route.samples, route.widths, roadMaterial, true);
            CreateRouteStripe(root, route);
            CreateRouteBarriers(root, route);
            CreateStartGate(root, route.PoseAtDistance(22f));
            CreateCheckpointGates(root, route);
            CreateTrackMarkers(root, route);
            CreatePitDiorama(root, route);
            CreateMapDressing(root, map, route);
            return route;
        }

        private RuntimeTrackRoute CreateRuntimeLoopRoute(VectorSSMapId mapId)
        {
            Vector3[] nodes;
            float[] nodeWidths;
            switch (mapId)
            {
                case VectorSSMapId.ScraplineYard:
                    nodes = new[]
                    {
                        new Vector3(0f, 0.08f, 8f),
                        new Vector3(0f, 0.08f, 96f),
                        new Vector3(24f, 0.08f, 164f),
                        new Vector3(92f, 0.08f, 210f),
                        new Vector3(166f, 0.08f, 192f),
                        new Vector3(196f, 0.08f, 116f),
                        new Vector3(150f, 0.08f, 42f),
                        new Vector3(72f, 0.08f, 20f),
                        new Vector3(18f, 0.08f, -38f),
                        new Vector3(-64f, 0.08f, -20f),
                        new Vector3(-92f, 0.08f, 64f),
                        new Vector3(-42f, 0.08f, 130f)
                    };
                    nodeWidths = new[] { 22f, 28f, 42f, 54f, 48f, 36f, 30f, 42f, 24f, 28f, 30f, 24f };
                    break;
                case VectorSSMapId.RubberRidge:
                    nodes = new[]
                    {
                        new Vector3(0f, 0.08f, 8f),
                        new Vector3(0f, 0.08f, 78f),
                        new Vector3(-44f, 1.85f, 132f),
                        new Vector3(-112f, 0.08f, 118f),
                        new Vector3(-140f, 0.08f, 34f),
                        new Vector3(-88f, 0.08f, -40f),
                        new Vector3(-6f, 0.08f, -72f),
                        new Vector3(78f, 0.08f, -40f),
                        new Vector3(136f, 0.08f, 32f),
                        new Vector3(112f, 1.35f, 114f),
                        new Vector3(42f, 0.08f, 158f),
                        new Vector3(124f, 0.08f, 162f),
                        new Vector3(186f, 0.08f, 92f),
                        new Vector3(168f, 0.08f, -10f),
                        new Vector3(88f, 0.08f, -96f),
                        new Vector3(-18f, 0.08f, -54f)
                    };
                    nodeWidths = new[] { 21f, 20f, 18f, 22f, 20f, 21f, 24f, 21f, 20f, 18f, 21f, 22f, 23f, 22f, 24f, 22f };
                    break;
                default:
                    nodes = new[]
                    {
                        new Vector3(0f, 0.08f, 8f),
                        new Vector3(0f, 0.08f, 92f),
                        new Vector3(2f, 1.65f, 130f),
                        new Vector3(10f, 0.25f, 172f),
                        new Vector3(46f, 0.08f, 232f),
                        new Vector3(112f, 0.08f, 258f),
                        new Vector3(166f, 0.08f, 214f),
                        new Vector3(182f, 0.08f, 126f),
                        new Vector3(142f, 0.08f, 34f),
                        new Vector3(72f, 0.08f, -34f),
                        new Vector3(-4f, 0.08f, -58f),
                        new Vector3(-76f, 0.08f, -20f),
                        new Vector3(-96f, 0.08f, 58f),
                        new Vector3(-52f, 0.08f, 120f),
                        new Vector3(-8f, 0.08f, 78f)
                    };
                    nodeWidths = new[] { 18f, 18f, 16f, 24f, 40f, 48f, 28f, 22f, 21f, 23f, 27f, 26f, 23f, 21f, 18f };
                    break;
            }

            return CreateRouteFromNodes(nodes, nodeWidths, 10);
        }

        private RuntimeTrackRoute CreateRouteFromNodes(Vector3[] nodes, float[] nodeWidths, int samplesPerSegment)
        {
            List<Vector3> samples = new List<Vector3>(nodes.Length * samplesPerSegment + 1);
            List<float> widths = new List<float>(nodes.Length * samplesPerSegment + 1);
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 p0 = nodes[(i - 1 + nodes.Length) % nodes.Length];
                Vector3 p1 = nodes[i];
                Vector3 p2 = nodes[(i + 1) % nodes.Length];
                Vector3 p3 = nodes[(i + 2) % nodes.Length];
                float w1 = nodeWidths[i];
                float w2 = nodeWidths[(i + 1) % nodeWidths.Length];

                for (int step = 0; step < samplesPerSegment; step++)
                {
                    float t = step / (float)samplesPerSegment;
                    samples.Add(CatmullRom(p0, p1, p2, p3, t));
                    widths.Add(Mathf.Lerp(w1, w2, Mathf.SmoothStep(0f, 1f, t)));
                }
            }

            samples.Add(samples[0]);
            widths.Add(widths[0]);
            return RuntimeTrackRoute.FromSamples(samples.ToArray(), widths.ToArray());
        }

        private static Vector3 MapBoardCenter(VectorSSMapId mapId)
        {
            switch (mapId)
            {
                case VectorSSMapId.ScraplineYard:
                    return new Vector3(44f, -0.28f, 82f);
                case VectorSSMapId.RubberRidge:
                    return new Vector3(18f, -0.28f, 34f);
                default:
                    return new Vector3(36f, -0.28f, 98f);
            }
        }

        private static Vector3 MapBoardScale(VectorSSMapId mapId)
        {
            switch (mapId)
            {
                case VectorSSMapId.ScraplineYard:
                    return new Vector3(340f, 0.18f, 300f);
                case VectorSSMapId.RubberRidge:
                    return new Vector3(386f, 0.18f, 304f);
                default:
                    return new Vector3(320f, 0.18f, 365f);
            }
        }

        private void CreateRouteStripe(Transform root, RuntimeTrackRoute route)
        {
            Vector3[] stripeSamples = new Vector3[route.samples.Length];
            float[] stripeWidths = new float[route.samples.Length];
            for (int i = 0; i < route.samples.Length; i++)
            {
                stripeSamples[i] = route.samples[i] + Vector3.up * 0.055f;
                stripeWidths[i] = 0.34f;
            }

            LowPolyMeshFactory.CreateTrackRibbon("GTX Continuous Blue Center Ink", root, stripeSamples, stripeWidths, stripeMaterial, false);
        }

        private void CreateRouteBarriers(Transform root, RuntimeTrackRoute route)
        {
            const int step = 3;
            for (int i = 0; i < route.samples.Length - 1; i += step)
            {
                int end = Mathf.Min(route.samples.Length - 1, i + step);
                Vector3 a = route.samples[i];
                Vector3 b = route.samples[end];
                Vector3 segment = b - a;
                Vector3 flat = new Vector3(segment.x, 0f, segment.z);
                if (flat.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                Vector3 forward = flat.normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                float startWidth = route.widths[i];
                float endWidth = route.widths[end];
                float length = flat.magnitude + 0.35f;
                Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
                Vector3 centerLift = Vector3.up * 0.64f;
                Vector3 leftStart = a + centerLift - right * (startWidth * 0.5f + 0.78f);
                Vector3 leftEnd = b + centerLift - right * (endWidth * 0.5f + 0.78f);
                Vector3 rightStart = a + centerLift + right * (startWidth * 0.5f + 0.78f);
                Vector3 rightEnd = b + centerLift + right * (endWidth * 0.5f + 0.78f);

                if (!RailWouldBlockRoute(route, i, end, leftStart, leftEnd))
                {
                    CreateRoundedBarrierRail(root, "Loop Left Rail " + i, (leftStart + leftEnd) * 0.5f, rotation, length);
                }

                if (!RailWouldBlockRoute(route, i, end, rightStart, rightEnd))
                {
                    CreateRoundedBarrierRail(root, "Loop Right Rail " + i, (rightStart + rightEnd) * 0.5f, rotation, length);
                }
            }
        }

        private static bool RailWouldBlockRoute(RuntimeTrackRoute route, int startIndex, int endIndex, Vector3 railStart, Vector3 railEnd)
        {
            if (route.samples == null || route.widths == null || route.samples.Length < 4)
            {
                return false;
            }

            int segmentCount = route.samples.Length - 1;
            int start = NormalizeRouteIndex(startIndex, segmentCount);
            int end = NormalizeRouteIndex(endIndex, segmentCount);
            for (int sample = 0; sample <= 6; sample++)
            {
                Vector3 point = Vector3.Lerp(railStart, railEnd, sample / 6f);
                for (int i = 0; i < segmentCount; i++)
                {
                    if (RouteIndexNear(i, start, segmentCount) || RouteIndexNear(i, end, segmentCount))
                    {
                        continue;
                    }

                    float distance = DistancePointToSegmentXZ(point, route.samples[i], route.samples[i + 1]);
                    float localHalfWidth = (route.widths[i] + route.widths[i + 1]) * 0.25f;
                    if (distance < localHalfWidth + 1.55f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RouteIndexNear(int candidate, int anchor, int segmentCount)
        {
            int distance = Mathf.Abs(NormalizeRouteIndex(candidate, segmentCount) - anchor);
            distance = Mathf.Min(distance, segmentCount - distance);
            return distance <= 8;
        }

        private static int NormalizeRouteIndex(int index, int segmentCount)
        {
            if (segmentCount <= 0)
            {
                return 0;
            }

            int normalized = index % segmentCount;
            return normalized < 0 ? normalized + segmentCount : normalized;
        }

        private static float DistancePointToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 start = new Vector2(a.x, a.z);
            Vector2 end = new Vector2(b.x, b.z);
            Vector2 segment = end - start;
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq < 0.0001f)
            {
                return Vector2.Distance(p, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - start, segment) / lengthSq);
            return Vector2.Distance(p, start + segment * t);
        }

        private void CreateRoundedBarrierRail(Transform root, string name, Vector3 position, Quaternion rotation, float length)
        {
            LowPolyMeshFactory.CreatePrism(name, root, 10, position, rotation, new Vector3(0.92f, 0.92f, length), barrierMaterial, false);
            GameObject collider = CreatePrimitive(name + " Collider", PrimitiveType.Cube, root, position, rotation, new Vector3(0.78f, 1.08f, length), barrierMaterial, true);
            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private void CreateStartGate(Transform root, TrackPose pose)
        {
            Transform gate = new GameObject("GTX Start Gate").transform;
            gate.SetParent(root, false);
            gate.localPosition = pose.position + Vector3.up * 0.18f;
            gate.localRotation = pose.rotation;

            float halfWidth = pose.width * 0.5f + 2.2f;
            CreateChamferedBox("Start Gate Left", gate, new Vector3(-halfWidth, 3.1f, 0f), Quaternion.identity, new Vector3(0.9f, 6.2f, 0.9f), inkMaterial, true, 0.08f);
            CreateChamferedBox("Start Gate Right", gate, new Vector3(halfWidth, 3.1f, 0f), Quaternion.identity, new Vector3(0.9f, 6.2f, 0.9f), inkMaterial, true, 0.08f);
            CreateChamferedBox("Start Gate Header", gate, new Vector3(0f, 6.15f, 0f), Quaternion.identity, new Vector3(halfWidth * 2f + 1.2f, 0.9f, 0.9f), inkMaterial, true, 0.06f);
            CreatePrimitive("Start Gate Orange Tag", PrimitiveType.Cube, gate, new Vector3(-3.4f, 6.72f, -0.04f), Quaternion.identity, new Vector3(2.4f, 0.18f, 0.08f), trackMarkerMaterial, false);
            CreatePrimitive("Start Gate Blue Tag", PrimitiveType.Cube, gate, new Vector3(3.4f, 6.72f, -0.04f), Quaternion.identity, new Vector3(2.4f, 0.18f, 0.08f), trackMarkerBlueMaterial, false);
        }

        private void CreateCheckpointGates(Transform root, RuntimeTrackRoute route)
        {
            if (route.TotalLength <= 0.01f)
            {
                return;
            }

            for (int i = 0; i < CheckpointFractions.Length; i++)
            {
                TrackPose pose = route.PoseAtDistance(route.TotalLength * CheckpointFractions[i]);
                CreateCheckpointGate(root, "Checkpoint Gate " + (i + 1), pose, i);
            }
        }

        private void CreateCheckpointGate(Transform root, string name, TrackPose pose, int index)
        {
            Transform gate = new GameObject(name).transform;
            gate.SetParent(root, false);
            gate.localPosition = pose.position + Vector3.up * 0.2f;
            gate.localRotation = pose.rotation;

            float halfWidth = Mathf.Max(6f, pose.width * 0.5f - 1.1f);
            Material primary = index % 2 == 0 ? trackMarkerBlueMaterial : trackMarkerMaterial;
            Material secondary = index % 2 == 0 ? trackMarkerMaterial : trackMarkerBlueMaterial;
            CreatePrimitive(name + " Cross Ink", PrimitiveType.Cube, gate, new Vector3(0f, 0.18f, 0f), Quaternion.identity, new Vector3(halfWidth * 2f, 0.09f, 0.22f), primary, false);
            CreatePrimitive(name + " Left Flag", PrimitiveType.Cube, gate, new Vector3(-halfWidth, 1.55f, 0f), Quaternion.Euler(0f, 0f, -8f), new Vector3(0.28f, 2.4f, 0.18f), secondary, false);
            CreatePrimitive(name + " Right Flag", PrimitiveType.Cube, gate, new Vector3(halfWidth, 1.55f, 0f), Quaternion.Euler(0f, 0f, 8f), new Vector3(0.28f, 2.4f, 0.18f), secondary, false);
            CreatePrimitive(name + " Number Plate", PrimitiveType.Cube, gate, new Vector3(0f, 2.78f, 0f), Quaternion.identity, new Vector3(2.2f, 0.42f, 0.16f), primary, false);
        }

        private void CreateTrackMarkers(Transform root, RuntimeTrackRoute route)
        {
            CreateChevronAtDistance(root, route, "Ramp Read Chevron", 90f, 7.2f, trackMarkerMaterial);
            CreateChevronAtDistance(root, route, "Landing Strike Chevron", 165f, 10f, trackMarkerBlueMaterial);
            CreateChevronAtDistance(root, route, "Combat Gallery Chevron", 245f, 20f, trackMarkerMaterial);
            CreateChevronAtDistance(root, route, "Outer Sweeper Chevron", 350f, 12f, trackMarkerBlueMaterial);
            CreateChevronAtDistance(root, route, "Return Hairpin Chevron", 555f, 10f, trackMarkerMaterial);
            CreateChevronAtDistance(root, route, "Final Curve Chevron", 665f, 8.5f, trackMarkerBlueMaterial);

            CreatePylonAtDistance(root, route, "Combat Pylon Left", 245f, -0.5f, trackMarkerMaterial);
            CreatePylonAtDistance(root, route, "Combat Pylon Right", 245f, 0.5f, trackMarkerBlueMaterial);
            CreatePylonAtDistance(root, route, "Ramp Warning Pylon Left", 115f, -0.5f, trackMarkerMaterial);
            CreatePylonAtDistance(root, route, "Ramp Warning Pylon Right", 115f, 0.5f, trackMarkerBlueMaterial);
            CreatePylonAtDistance(root, route, "Hairpin Pylon Left", 555f, -0.5f, trackMarkerBlueMaterial);
            CreatePylonAtDistance(root, route, "Hairpin Pylon Right", 555f, 0.5f, trackMarkerMaterial);
        }

        private void CreateChevronAtDistance(Transform root, RuntimeTrackRoute route, string name, float distance, float halfWidth, Material material)
        {
            TrackPose pose = route.PoseAtDistance(distance);
            CreateChevronPair(root, name, pose.position + Vector3.up * 0.28f, pose.rotation, halfWidth, material);
        }

        private void CreatePylonAtDistance(Transform root, RuntimeTrackRoute route, string name, float distance, float side, Material material)
        {
            TrackPose pose = route.PoseAtDistance(distance);
            Vector3 position = pose.position + pose.right * (pose.width * side + side * 2.4f) + Vector3.up * 1.02f;
            CreatePylon(root, name, position, material);
        }

        private void CreateChevronPair(Transform root, string name, Vector3 position, Quaternion rotation, float halfWidth, Material material)
        {
            Transform marker = new GameObject(name).transform;
            marker.SetParent(root, false);
            marker.localPosition = position;
            marker.localRotation = rotation;

            CreatePrimitive(name + " Left A", PrimitiveType.Cube, marker, new Vector3(-halfWidth, 0f, -1.3f), Quaternion.Euler(0f, 32f, 0f), new Vector3(0.34f, 0.08f, 2.8f), material, false);
            CreatePrimitive(name + " Left B", PrimitiveType.Cube, marker, new Vector3(-halfWidth, 0f, 1.3f), Quaternion.Euler(0f, -32f, 0f), new Vector3(0.34f, 0.08f, 2.8f), material, false);
            CreatePrimitive(name + " Right A", PrimitiveType.Cube, marker, new Vector3(halfWidth, 0f, -1.3f), Quaternion.Euler(0f, -32f, 0f), new Vector3(0.34f, 0.08f, 2.8f), material, false);
            CreatePrimitive(name + " Right B", PrimitiveType.Cube, marker, new Vector3(halfWidth, 0f, 1.3f), Quaternion.Euler(0f, 32f, 0f), new Vector3(0.34f, 0.08f, 2.8f), material, false);
        }

        private void CreatePylon(Transform root, string name, Vector3 position, Material material)
        {
            LowPolyMeshFactory.CreatePrism(name, root, 5, position, Quaternion.identity, new Vector3(0.8f, 1.7f, 0.8f), material, false);
            LowPolyMeshFactory.CreatePrism(name + " Ink Cap", root, 5, position + Vector3.up * 0.92f, Quaternion.identity, new Vector3(0.9f, 0.18f, 0.9f), inkMaterial, false);
        }

        private void CreatePitDiorama(Transform root, RuntimeTrackRoute route)
        {
            Transform pit = new GameObject("GTX Anime Rally Pit Diorama").transform;
            pit.SetParent(root, false);
            TrackPose pitPose = route.PoseAtDistance(44f);
            pit.localPosition = pitPose.position - pitPose.right * (pitPose.width * 0.5f + 9f);
            pit.localRotation = pitPose.rotation;

            CreateChamferedBox("Pit Printed Mat", pit, new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(13f, 0.12f, 16f), pitFloorMaterial, false, 0.03f);
            CreatePrimitive("Pit Orange Edge A", PrimitiveType.Cube, pit, new Vector3(-6.7f, 0.18f, 0f), Quaternion.identity, new Vector3(0.28f, 0.08f, 16f), trackMarkerMaterial, false);
            CreatePrimitive("Pit Blue Edge B", PrimitiveType.Cube, pit, new Vector3(6.7f, 0.18f, 0f), Quaternion.identity, new Vector3(0.28f, 0.08f, 16f), trackMarkerBlueMaterial, false);

            CreateChamferedBox("Pit Tool Chest", pit, new Vector3(-4.8f, 0.72f, 4.5f), Quaternion.identity, new Vector3(1.8f, 1.2f, 0.9f), trackMarkerMaterial, true, 0.08f);
            CreatePrimitive("Pit Tool Chest Ink Stripe", PrimitiveType.Cube, pit, new Vector3(-4.8f, 1.06f, 4f), Quaternion.identity, new Vector3(1.9f, 0.16f, 0.12f), inkMaterial, false);
            CreateChamferedBox("Pit Parts Crate", pit, new Vector3(4.1f, 0.52f, 4.2f), Quaternion.Euler(0f, -10f, 0f), new Vector3(2.2f, 0.9f, 1.1f), pitPropMaterial, true, 0.07f);
            CreateChamferedBox("Pit Gantry Left", pit, new Vector3(-5.8f, 2.4f, -5.3f), Quaternion.identity, new Vector3(0.28f, 4.4f, 0.28f), inkMaterial, true, 0.05f);
            CreateChamferedBox("Pit Gantry Right", pit, new Vector3(5.8f, 2.4f, -5.3f), Quaternion.identity, new Vector3(0.28f, 4.4f, 0.28f), inkMaterial, true, 0.05f);
            CreateChamferedBox("Pit Gantry Header", pit, new Vector3(0f, 4.35f, -5.3f), Quaternion.identity, new Vector3(12f, 0.44f, 0.32f), playerSecondaryMaterial, true, 0.05f);

            CreateTireStack(pit, "Pit Tire Stack A", new Vector3(5.5f, 0.45f, -1.5f));
            CreateTireStack(pit, "Pit Tire Stack B", new Vector3(4.4f, 0.45f, -2.8f));
            CreateTireStack(pit, "Pit Tire Stack C", new Vector3(-5.2f, 0.45f, -1.2f));
        }

        private void CreateMapDressing(Transform root, VectorSSMapDefinition map, RuntimeTrackRoute route)
        {
            switch (map.id)
            {
                case VectorSSMapId.ScraplineYard:
                    CreateScraplineDressing(root, route);
                    break;
                case VectorSSMapId.RubberRidge:
                    CreateRubberRidgeDressing(root, route);
                    break;
                default:
                    CreateBlacklineDressing(root, route);
                    break;
            }
        }

        private void CreateBlacklineDressing(Transform root, RuntimeTrackRoute route)
        {
            for (int i = 0; i < 5; i++)
            {
                TrackPose pose = route.PoseAtDistance(70f + i * 48f);
                CreateChamferedBox("Blackline Elevated Pylon L " + i, root, pose.position - pose.right * (pose.width * 0.5f + 5f) + Vector3.up * 5f, pose.rotation, new Vector3(1.2f, 10f, 1.2f), inkMaterial, true, 0.07f);
                CreateChamferedBox("Blackline Elevated Pylon R " + i, root, pose.position + pose.right * (pose.width * 0.5f + 5f) + Vector3.up * 5f, pose.rotation, new Vector3(1.2f, 10f, 1.2f), inkMaterial, true, 0.07f);
            }
        }

        private void CreateScraplineDressing(Transform root, RuntimeTrackRoute route)
        {
            for (int i = 0; i < 8; i++)
            {
                TrackPose pose = route.PoseAtDistance(160f + i * 18f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = pose.position + pose.right * side * (pose.width * 0.5f - 5f) + Vector3.up * 1f;
                GameObject container = CreateChamferedBox("Scrapline Movable Crate " + i, root, position, pose.rotation * Quaternion.Euler(0f, 90f, 0f), new Vector3(2.6f, 1.8f, 5.6f), i % 2 == 0 ? trackMarkerMaterial : trackMarkerBlueMaterial, true, 0.06f);
                Rigidbody body = container.AddComponent<Rigidbody>();
                body.mass = 90f;
                body.drag = 0.25f;
                body.angularDrag = 1.8f;
            }

            TrackPose cranePose = route.PoseAtDistance(220f);
            CreateChamferedBox("Scrapline Crane Tower", root, cranePose.position - cranePose.right * 34f + Vector3.up * 6f, cranePose.rotation, new Vector3(2f, 12f, 2f), inkMaterial, true, 0.06f);
            CreateChamferedBox("Scrapline Crane Arm", root, cranePose.position - cranePose.right * 18f + Vector3.up * 12f, cranePose.rotation * Quaternion.Euler(0f, 0f, 4f), new Vector3(34f, 0.8f, 1f), playerSecondaryMaterial, true, 0.04f);
        }

        private void CreateRubberRidgeDressing(Transform root, RuntimeTrackRoute route)
        {
            for (int i = 0; i < 14; i++)
            {
                TrackPose pose = route.PoseAtDistance(120f + i * 34f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = pose.position + pose.right * side * (pose.width * 0.5f + 2.2f) + Vector3.up * 0.5f;
                CreateTireStack(root, "Rubber Ridge Tire Wall " + i, position);
            }

            Vector3[] shortcut =
            {
                route.PoseAtDistance(390f).position + Vector3.up * 0.18f,
                route.PoseAtDistance(430f).position + Vector3.up * 1.25f + Vector3.right * 8f,
                route.PoseAtDistance(500f).position + Vector3.up * 0.18f
            };
            float[] widths = { 5.2f, 4.5f, 5.2f };
            LowPolyMeshFactory.CreateTrackRibbon("Rubber Ridge Razor Shortcut", root, shortcut, widths, roadMaterial, true);
            LowPolyMeshFactory.CreateTrackRibbon("Rubber Ridge Shortcut Ink", root, new[] { shortcut[0] + Vector3.up * 0.04f, shortcut[1] + Vector3.up * 0.04f, shortcut[2] + Vector3.up * 0.04f }, new[] { 0.18f, 0.18f, 0.18f }, trackMarkerBlueMaterial, false);
        }

        private void CreateTireStack(Transform parent, string name, Vector3 position)
        {
            for (int i = 0; i < 3; i++)
            {
                LowPolyMeshFactory.CreatePrism(name + " Tire " + i, parent, 8, position + Vector3.up * (i * 0.28f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.78f, 0.78f, 0.2f), wheelMaterial, false);
            }
        }

        private void CreateRoadSegment(Transform root, string name, Vector3 position, Quaternion rotation, float width, float length)
        {
            Transform segment = new GameObject(name).transform;
            segment.SetParent(root, false);
            segment.localPosition = position;
            segment.localRotation = rotation;

            CreateChamferedBox(name + " Surface", segment, Vector3.zero, Quaternion.identity, new Vector3(width, 0.24f, length), roadMaterial, true, 0.035f);
            CreatePrimitive(name + " Center Ink", PrimitiveType.Cube, segment, new Vector3(0f, 0.14f, 0f), Quaternion.identity, new Vector3(0.28f, 0.04f, length * 0.82f), stripeMaterial, false);
            CreateChamferedBox(name + " Left Barrier", segment, new Vector3(-width * 0.5f - 0.48f, 0.72f, 0f), Quaternion.identity, new Vector3(0.72f, 1.35f, length), barrierMaterial, true, 0.08f);
            CreateChamferedBox(name + " Right Barrier", segment, new Vector3(width * 0.5f + 0.48f, 0.72f, 0f), Quaternion.identity, new Vector3(0.72f, 1.35f, length), barrierMaterial, true, 0.08f);
        }

        private void CreateRamp(Transform root, string name, Vector3 position, Quaternion rotation, float width, float length)
        {
            Transform ramp = new GameObject(name).transform;
            ramp.SetParent(root, false);
            ramp.localPosition = position;
            ramp.localRotation = rotation;
            CreateChamferedBox(name + " Surface", ramp, Vector3.zero, Quaternion.identity, new Vector3(width, 0.56f, length), roadMaterial, true, 0.04f);
            CreateChamferedBox(name + " Left Rail", ramp, new Vector3(-width * 0.5f - 0.42f, 0.66f, 0f), Quaternion.identity, new Vector3(0.62f, 1.08f, length), barrierMaterial, true, 0.08f);
            CreateChamferedBox(name + " Right Rail", ramp, new Vector3(width * 0.5f + 0.42f, 0.66f, 0f), Quaternion.identity, new Vector3(0.62f, 1.08f, length), barrierMaterial, true, 0.08f);
        }

        private PlayerRig CreatePlayerMachine(Transform resetPoint, VectorSSVehicleDefinition vehicleDefinition)
        {
            GameObject root = new GameObject("Vector SS Player " + vehicleDefinition.displayName);
            root.transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 1350f;
            body.drag = 0.025f;
            body.angularDrag = 1.2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = vehicleDefinition.colliderCenter;
            bodyCollider.size = vehicleDefinition.colliderSize;

            Transform boostTrail;
            if (vehicleDefinition.isBike)
            {
                boostTrail = CreateRazorBikeVisuals(root.transform, vehicleDefinition);
            }
            else
            {
                CreateCarVisuals(root.transform, vehicleDefinition);
                boostTrail = LowPolyMeshFactory.CreatePrism("Boost Trail Low Poly Hex", root.transform, 6, new Vector3(0f, 0.46f, -3.1f), Quaternion.identity, new Vector3(0.88f, 0.88f, 2.8f), boostTrailMaterial, false).transform;
            }

            boostTrail.gameObject.SetActive(false);

            Vector3[] wheelPositions = WheelColliderPositions(vehicleDefinition);
            float wheelRadius = vehicleDefinition.isBike ? 0.34f : 0.38f;
            float suspension = vehicleDefinition.isBike ? 0.22f : 0.28f;
            WheelCollider frontLeft = CreateWheelCollider(root.transform, "Front Left WheelCollider", wheelPositions[0], wheelRadius, suspension);
            WheelCollider frontRight = CreateWheelCollider(root.transform, "Front Right WheelCollider", wheelPositions[1], wheelRadius, suspension);
            WheelCollider rearLeft = CreateWheelCollider(root.transform, "Rear Left WheelCollider", wheelPositions[2], wheelRadius, suspension);
            WheelCollider rearRight = CreateWheelCollider(root.transform, "Rear Right WheelCollider", wheelPositions[3], wheelRadius, suspension);

            VehicleTuning tuning = ScriptableObject.CreateInstance<VehicleTuning>();
            tuning.name = "Vector SS Runtime Vehicle Tuning " + vehicleDefinition.displayName;
            VectorSSProgressionUtility.ApplyToVehicleTuning(tuning, vehicleDefinition, playerProfile);

            VehicleController vehicle = root.AddComponent<VehicleController>();
            vehicle.Configure(tuning, frontLeft, frontRight, rearLeft, rearRight);
            if (vehicleDefinition.isBike)
            {
                VectorSSBikeLeanVisual lean = root.GetComponent<VectorSSBikeLeanVisual>();
                if (lean != null)
                {
                    Transform leanRoot = root.transform.Find("Razor Lean Visual");
                    lean.Configure(leanRoot, vehicle, body, playerProfile.tuning.leanResponse * 8f);
                }
            }

            FlowState flowState = root.AddComponent<FlowState>();
            RuntimeImpactEffects effects = root.AddComponent<RuntimeImpactEffects>();
            VisualIntensityMapper intensityMapper = root.AddComponent<VisualIntensityMapper>();
            SideSlamController sideSlam = root.AddComponent<SideSlamController>();
            BoostRamDetector boostRam = root.AddComponent<BoostRamDetector>();
            SpinGuardController spinGuard = root.AddComponent<SpinGuardController>();
            FlowVisualController visuals = root.AddComponent<FlowVisualController>();
            GTXDrivingFlowBridge flowBridge = root.AddComponent<GTXDrivingFlowBridge>();
            VectorSSVehicleModuleController moduleController = root.AddComponent<VectorSSVehicleModuleController>();

            sideSlam.Configure(body, flowState, effects);
            boostRam.Configure(body, flowState, effects);
            spinGuard.Configure(body, flowState, effects);
            flowBridge.Configure(vehicle, flowState, effects, boostRam, resetPoint);
            moduleController.Configure(vehicle, body, flowState, effects, playerProfile, vehicleDefinition);
            float ramMultiplier = VectorSSProgressionUtility.RamMultiplier(vehicleDefinition, playerProfile);
            sideSlam.SetPowerMultiplier(ramMultiplier);
            boostRam.SetPowerMultiplier(ramMultiplier * (vehicleDefinition.isBike ? 1.18f : 1f));
            intensityMapper.enabled = true;

            return new PlayerRig(root, body, tuning, vehicle, flowState, effects, sideSlam, boostRam, spinGuard, visuals, boostTrail, moduleController);
        }

        private void CreateCarVisuals(Transform parent, VectorSSVehicleDefinition vehicleDefinition)
        {
            Transform visualRoot = parent;
            Vector3 scale = vehicleDefinition.visualScale;
            CreateOutlinedChamferedBox(visualRoot, "Player Body", new Vector3(0f, 0.68f, 0f), Quaternion.identity, Vector3.Scale(new Vector3(2.26f, 0.64f, 4.34f), scale), playerMaterial, 1.09f * playerProfile.tuning.outlineThickness, 0.1f);
            CreateOutlinedChamferedBox(visualRoot, "Player Cabin Block", new Vector3(0f, 1.16f, -0.35f * scale.z), Quaternion.identity, Vector3.Scale(new Vector3(1.38f, 0.56f, 1.36f), scale), glassMaterial, 1.08f * playerProfile.tuning.outlineThickness, 0.09f);
            CreateOutlinedWedge(visualRoot, "Player Front Bite", new Vector3(0f, 0.94f, 1.78f * scale.z), Quaternion.identity, Vector3.Scale(new Vector3(2.12f, 0.38f, 1.16f), scale), playerMaterial, 1.08f * playerProfile.tuning.outlineThickness);
            CreatePrimitive("Player Hood Blue Slash", PrimitiveType.Cube, visualRoot, new Vector3(-0.34f * scale.x, 1.05f, 1.28f * scale.z), Quaternion.Euler(0f, -10f, 0f), new Vector3(0.28f, 0.08f, 1.75f), playerSecondaryMaterial, false);
            CreatePrimitive("Player Hood Orange Slash", PrimitiveType.Cube, visualRoot, new Vector3(0.22f * scale.x, 1.06f, 1.2f * scale.z), Quaternion.Euler(0f, -10f, 0f), new Vector3(0.36f, 0.09f, 1.9f), playerAccentMaterial, false);
            CreatePrimitive("Player Roof Print Plate", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.48f, -0.38f * scale.z), Quaternion.identity, new Vector3(1.18f, 0.08f, 0.86f), playerAccentMaterial, false);
            CreatePrimitive("Player Roof Blue Label", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.535f, -0.38f * scale.z), Quaternion.identity, new Vector3(0.62f, 0.06f, 0.38f), playerSecondaryMaterial, false);
            CreateRallyLivery(visualRoot);
            CreateRoundedMachineCaps(visualRoot);
            CreateLowPolyFin(visualRoot, "Left Armor Fin", new Vector3(-1.28f * scale.x, 0.88f, -0.5f * scale.z), Quaternion.Euler(0f, 0f, -8f));
            CreateLowPolyFin(visualRoot, "Right Armor Fin", new Vector3(1.28f * scale.x, 0.88f, -0.5f * scale.z), Quaternion.Euler(0f, 180f, 8f));
            CreateWheelVisuals(visualRoot, vehicleDefinition);
        }

        private Transform CreateRazorBikeVisuals(Transform parent, VectorSSVehicleDefinition vehicleDefinition)
        {
            Transform leanRoot = new GameObject("Razor Lean Visual").transform;
            leanRoot.SetParent(parent, false);
            parent.gameObject.AddComponent<VectorSSBikeLeanVisual>();

            CreateOutlinedPrism(leanRoot, "Razor Front Wheel", 10, new Vector3(0f, 0.43f, 1.08f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.72f, 0.18f), wheelMaterial, 1.06f * playerProfile.tuning.outlineThickness);
            CreateOutlinedPrism(leanRoot, "Razor Rear Wheel", 10, new Vector3(0f, 0.43f, -1.08f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.72f, 0.18f), wheelMaterial, 1.06f * playerProfile.tuning.outlineThickness);
            CreateOutlinedChamferedBox(leanRoot, "Razor Spine Frame", new Vector3(0f, 0.78f, 0f), Quaternion.Euler(-4f, 0f, 0f), new Vector3(0.36f, 0.32f, 2.2f), playerMaterial, 1.12f * playerProfile.tuning.outlineThickness, 0.08f);
            CreateOutlinedWedge(leanRoot, "Razor Front Fairing", new Vector3(0f, 0.94f, 0.86f), Quaternion.identity, new Vector3(0.72f, 0.44f, 0.9f), playerAccentMaterial, 1.1f * playerProfile.tuning.outlineThickness);
            CreateOutlinedChamferedBox(leanRoot, "Razor Battery Block", new Vector3(0f, 0.92f, -0.28f), Quaternion.identity, new Vector3(0.64f, 0.5f, 0.72f), playerSecondaryMaterial, 1.08f * playerProfile.tuning.outlineThickness, 0.08f);
            CreateOutlinedPrism(leanRoot, "Razor Fork", 6, new Vector3(0f, 0.78f, 1.18f), Quaternion.Euler(16f, 0f, 0f), new Vector3(0.16f, 0.16f, 1f), inkMaterial, 1.04f);
            CreateOutlinedPrism(leanRoot, "Razor Handlebar", 6, new Vector3(0f, 1.28f, 0.92f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.12f, 0.12f, 1.25f), inkMaterial, 1.04f);
            CreateOutlinedChamferedBox(leanRoot, "Razor Rider Torso", new Vector3(0f, 1.36f, -0.24f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.48f, 0.72f, 0.36f), inkMaterial, 1.06f, 0.08f);
            CreateOutlinedPrism(leanRoot, "Razor Rider Helmet", 10, new Vector3(0f, 1.84f, 0.02f), Quaternion.identity, new Vector3(0.42f, 0.42f, 0.42f), glassMaterial, 1.08f);
            CreatePrimitive("Razor Side Check Left", PrimitiveType.Cube, leanRoot, new Vector3(-0.46f, 0.86f, -0.05f), Quaternion.Euler(0f, 0f, -10f), new Vector3(0.08f, 0.42f, 1.34f), playerAccentMaterial, false);
            CreatePrimitive("Razor Side Check Right", PrimitiveType.Cube, leanRoot, new Vector3(0.46f, 0.86f, -0.05f), Quaternion.Euler(0f, 0f, 10f), new Vector3(0.08f, 0.42f, 1.34f), playerAccentMaterial, false);

            return LowPolyMeshFactory.CreatePrism("Razor Narrow Boost Trail", leanRoot, 6, new Vector3(0f, 0.46f, -2.35f), Quaternion.identity, new Vector3(0.34f, 0.34f, 2.2f), boostTrailMaterial, false).transform;
        }

        private Vector3[] WheelColliderPositions(VectorSSVehicleDefinition vehicleDefinition)
        {
            if (vehicleDefinition.isBike)
            {
                return new[]
                {
                    new Vector3(-0.28f, 0.43f, 1.05f),
                    new Vector3(0.28f, 0.43f, 1.05f),
                    new Vector3(-0.24f, 0.43f, -1.05f),
                    new Vector3(0.24f, 0.43f, -1.05f)
                };
            }

            float x = 1.05f * vehicleDefinition.visualScale.x;
            float z = 1.38f * vehicleDefinition.visualScale.z;
            return new[]
            {
                new Vector3(-x, 0.43f, z),
                new Vector3(x, 0.43f, z),
                new Vector3(-x, 0.43f, -z),
                new Vector3(x, 0.43f, -z)
            };
        }

        private void CreateWheelVisuals(Transform parent, VectorSSVehicleDefinition vehicleDefinition)
        {
            float x = 1.12f * vehicleDefinition.visualScale.x;
            float z = 1.38f * vehicleDefinition.visualScale.z;
            Vector3[] positions =
            {
                new Vector3(-x, 0.43f, z),
                new Vector3(x, 0.43f, z),
                new Vector3(-x, 0.43f, -z),
                new Vector3(x, 0.43f, -z)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateOutlinedPrism(parent, "Player Wheel " + i, 8, positions[i], Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.72f, 0.24f), wheelMaterial, 1.06f * playerProfile.tuning.outlineThickness);
            }
        }

        private void CreateRoundedMachineCaps(Transform parent)
        {
            CreateOutlinedPrism(parent, "Player Front Orange Bumper Tube", 10, new Vector3(0f, 0.62f, 2.27f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.26f, 0.26f, 2.22f), playerAccentMaterial, 1.08f);
            CreateOutlinedPrism(parent, "Player Rear Blue Bumper Tube", 10, new Vector3(0f, 0.62f, -2.27f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.26f, 0.26f, 2.22f), playerSecondaryMaterial, 1.08f);
            CreateOutlinedPrism(parent, "Player Left Rounded Rocker", 10, new Vector3(-1.21f, 0.62f, -0.1f), Quaternion.identity, new Vector3(0.2f, 0.2f, 3.4f), playerSecondaryMaterial, 1.08f);
            CreateOutlinedPrism(parent, "Player Right Rounded Rocker", 10, new Vector3(1.21f, 0.62f, -0.1f), Quaternion.identity, new Vector3(0.2f, 0.2f, 3.4f), playerSecondaryMaterial, 1.08f);
        }

        private void CreateRallyLivery(Transform parent)
        {
            CreatePrimitive("Left Door Blue Print Block", PrimitiveType.Cube, parent, new Vector3(-1.16f, 0.8f, -0.22f), Quaternion.identity, new Vector3(0.07f, 0.46f, 1.42f), playerSecondaryMaterial, false);
            CreatePrimitive("Right Door Blue Print Block", PrimitiveType.Cube, parent, new Vector3(1.16f, 0.8f, -0.22f), Quaternion.identity, new Vector3(0.07f, 0.46f, 1.42f), playerSecondaryMaterial, false);
            CreatePrimitive("Left Door Orange Plate", PrimitiveType.Cube, parent, new Vector3(-1.2f, 0.82f, 0.6f), Quaternion.identity, new Vector3(0.08f, 0.36f, 0.52f), playerAccentMaterial, false);
            CreatePrimitive("Right Door Orange Plate", PrimitiveType.Cube, parent, new Vector3(1.2f, 0.82f, 0.6f), Quaternion.identity, new Vector3(0.08f, 0.36f, 0.52f), playerAccentMaterial, false);
            CreatePrimitive("Rear Quarter Blue A", PrimitiveType.Cube, parent, new Vector3(-1.18f, 0.82f, -1.42f), Quaternion.Euler(0f, 0f, 10f), new Vector3(0.07f, 0.34f, 0.9f), playerSecondaryMaterial, false);
            CreatePrimitive("Rear Quarter Blue B", PrimitiveType.Cube, parent, new Vector3(1.18f, 0.82f, -1.42f), Quaternion.Euler(0f, 0f, -10f), new Vector3(0.07f, 0.34f, 0.9f), playerSecondaryMaterial, false);
            CreatePrimitive("Front Bumper Orange Lip", PrimitiveType.Cube, parent, new Vector3(0f, 0.55f, 2.24f), Quaternion.identity, new Vector3(2.12f, 0.18f, 0.12f), playerAccentMaterial, false);
            CreatePrimitive("Rear Bumper Blue Lip", PrimitiveType.Cube, parent, new Vector3(0f, 0.55f, -2.24f), Quaternion.identity, new Vector3(2.12f, 0.18f, 0.12f), playerSecondaryMaterial, false);
            CreatePrimitive("Rear Hatch Orange Panel", PrimitiveType.Cube, parent, new Vector3(0f, 0.84f, -2.24f), Quaternion.identity, new Vector3(1.38f, 0.36f, 0.09f), playerAccentMaterial, false);
            CreatePrimitive("Rear Hatch Blue Tag", PrimitiveType.Cube, parent, new Vector3(0f, 0.86f, -2.295f), Quaternion.identity, new Vector3(0.46f, 0.12f, 0.06f), playerSecondaryMaterial, false);
            CreatePrimitive("Rear Deck Orange Print Block", PrimitiveType.Cube, parent, new Vector3(0.28f, 1.05f, -1.54f), Quaternion.Euler(0f, 8f, 0f), new Vector3(1.02f, 0.1f, 1.22f), playerAccentMaterial, false);
            CreatePrimitive("Rear Deck Blue Print Block", PrimitiveType.Cube, parent, new Vector3(-0.56f, 1.06f, -1.44f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.42f, 0.11f, 1.22f), playerSecondaryMaterial, false);
            CreatePrimitive("Cabin Orange Sun Strip", PrimitiveType.Cube, parent, new Vector3(0f, 1.48f, 0.04f), Quaternion.identity, new Vector3(1.38f, 0.1f, 0.26f), playerAccentMaterial, false);
            CreatePrimitive("Left Rear Orange Corner", PrimitiveType.Cube, parent, new Vector3(-1.18f, 0.91f, -1.62f), Quaternion.identity, new Vector3(0.08f, 0.42f, 0.74f), playerAccentMaterial, false);
            CreatePrimitive("Right Rear Orange Corner", PrimitiveType.Cube, parent, new Vector3(1.18f, 0.91f, -1.62f), Quaternion.identity, new Vector3(0.08f, 0.42f, 0.74f), playerAccentMaterial, false);
            CreatePrimitive("Rear Model Kit Stripe", PrimitiveType.Cube, parent, new Vector3(0f, 0.99f, -2.01f), Quaternion.identity, new Vector3(1.72f, 0.08f, 0.12f), playerAccentMaterial, false);
            CreatePrimitive("Left Hood Vent Ink", PrimitiveType.Cube, parent, new Vector3(-0.62f, 1.1f, 0.58f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.36f, 0.06f, 0.08f), inkMaterial, false);
            CreatePrimitive("Right Hood Vent Ink", PrimitiveType.Cube, parent, new Vector3(0.62f, 1.1f, 0.58f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.36f, 0.06f, 0.08f), inkMaterial, false);
            CreatePrimitive("Cabin Rear Ink Shelf", PrimitiveType.Cube, parent, new Vector3(0f, 1.29f, -1.22f), Quaternion.identity, new Vector3(1.55f, 0.12f, 0.18f), inkMaterial, false);

            LowPolyMeshFactory.CreatePrism("Left Door Number Disc", parent, 12, new Vector3(-1.22f, 0.86f, -0.18f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.72f, 0.055f), playerMaterial, false);
            LowPolyMeshFactory.CreatePrism("Right Door Number Disc", parent, 12, new Vector3(1.22f, 0.86f, -0.18f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.72f, 0.055f), playerMaterial, false);
            CreatePrimitive("Left Door Number Bar", PrimitiveType.Cube, parent, new Vector3(-1.25f, 0.86f, -0.18f), Quaternion.identity, new Vector3(0.08f, 0.1f, 0.52f), inkMaterial, false);
            CreatePrimitive("Right Door Number Bar", PrimitiveType.Cube, parent, new Vector3(1.25f, 0.86f, -0.18f), Quaternion.identity, new Vector3(0.08f, 0.1f, 0.52f), inkMaterial, false);
        }

        private WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 localPosition, float radius = 0.38f, float suspensionDistance = 0.28f)
        {
            GameObject wheelObject = new GameObject(name);
            wheelObject.transform.SetParent(parent, false);
            wheelObject.transform.localPosition = localPosition;
            WheelCollider wheel = wheelObject.AddComponent<WheelCollider>();
            wheel.radius = radius;
            wheel.suspensionDistance = suspensionDistance;
            wheel.forceAppPointDistance = 0.18f;
            wheel.mass = 34f;
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = 36000f;
            spring.damper = 4600f;
            spring.targetPosition = 0.52f;
            wheel.suspensionSpring = spring;
            wheel.forwardFriction = TireFriction(1f);
            wheel.sidewaysFriction = TireFriction(1f);
            return wheel;
        }

        private GameObject CreateDummyRival(Vector3 position, Quaternion rotation)
        {
            GameObject rival = new GameObject("GTX Rival Dummy Target");
            rival.transform.SetPositionAndRotation(position, rotation);

            Rigidbody body = rival.AddComponent<Rigidbody>();
            body.mass = 1250f;
            body.drag = 0.04f;
            body.angularDrag = 1.35f;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            BoxCollider collider = rival.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.68f, 0f);
            collider.size = new Vector3(2.15f, 0.92f, 4.2f);

            CreateOutlinedChamferedBox(rival.transform, "Rival Body", new Vector3(0f, 0.68f, 0f), Quaternion.identity, new Vector3(2.15f, 0.68f, 4.2f), rivalMaterial, 1.08f, 0.1f);
            CreateOutlinedWedge(rival.transform, "Rival Cockpit", new Vector3(0f, 1.14f, -0.42f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.2f, 0.56f, 1.32f), rivalMaterial, 1.08f);
            CreatePrimitive("Rival Target Stripe", PrimitiveType.Cube, rival.transform, new Vector3(0f, 1.01f, 1.15f), Quaternion.identity, new Vector3(1.6f, 0.08f, 0.22f), playerAccentMaterial, false);
            rival.AddComponent<DummyRivalTarget>();
            rival.AddComponent<SimpleRouteRivalAI>();
            return rival;
        }

        private Camera ConfigureCamera(PlayerRig player)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.86f, 0.91f, 1f);
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 650f;
            GTXCameraRig rig = camera.GetComponent<GTXCameraRig>();
            if (rig == null)
            {
                rig = camera.gameObject.AddComponent<GTXCameraRig>();
            }

            rig.Configure(player.root.transform, player.body, player.flowState);
            camera.transform.SetPositionAndRotation(player.root.transform.position + new Vector3(0f, 4.4f, -10f), Quaternion.Euler(18f, 0f, 0f));
            return camera;
        }

        private void ConfigureHud(PlayerRig player)
        {
            GTXRuntimeHUD hud = FindObjectOfType<GTXRuntimeHUD>();
            if (hud == null)
            {
                hud = new GameObject("GTX Runtime HUD Controller").AddComponent<GTXRuntimeHUD>();
            }

            hud.Bind(player.vehicle, player.body, player.flowState);
            player.sideSlam.FeedbackRaised += hud.ShowCallout;
            player.boostRam.FeedbackRaised += hud.ShowCallout;
            player.spinGuard.FeedbackRaised += hud.ShowCallout;
            if (player.moduleController != null)
            {
                player.moduleController.FeedbackRaised += hud.ShowCallout;
                hud.SetModuleControlHints(player.moduleController.BuildControlHints());
            }
            else
            {
                hud.SetModuleControlHints(null);
            }

            hud.SetCoreHeatVisible(false);
            ConfigureModuleHud(hud, player.moduleController);
        }

        private void ConfigureModuleHud(GTXRuntimeHUD hud, VectorSSVehicleModuleController moduleController)
        {
            if (hud == null || moduleController == null)
            {
                return;
            }

            if (activeModuleHud == null)
            {
                activeModuleHud = hud.GetComponent<VectorSSModuleHUD>();
                if (activeModuleHud == null)
                {
                    activeModuleHud = hud.gameObject.AddComponent<VectorSSModuleHUD>();
                }
            }

            activeModuleHud.Configure(hud.HudCanvas, BuildModuleWidgetStates(moduleController));
            UpdateModuleHud();
        }

        private List<VectorSSModuleHUD.ModuleWidgetState> BuildModuleWidgetStates(VectorSSVehicleModuleController moduleController)
        {
            List<VectorSSModuleHUD.ModuleWidgetState> widgets = new List<VectorSSModuleHUD.ModuleWidgetState>();
            if (moduleController == null)
            {
                return widgets;
            }

            IList<VectorSSModuleDefinition> modules = moduleController.InstalledModules;
            for (int i = 0; i < modules.Count; i++)
            {
                VectorSSModuleDefinition module = modules[i];
                if (module == null || module.widget == VectorSSModuleWidget.None)
                {
                    continue;
                }

                VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, module, true);
                widgets.Add(new VectorSSModuleHUD.ModuleWidgetState
                {
                    moduleId = module.id,
                    title = module.displayName,
                    value = moduleController.GetWidgetValue(module.id),
                    position = layout != null ? layout.position : module.defaultHudPosition,
                    scale = layout != null ? layout.scale : module.defaultHudScale,
                    visible = layout == null || layout.visible
                });
            }

            return widgets;
        }

        private void UpdateModuleHud()
        {
            if (activeModuleHud == null || activeModuleController == null)
            {
                return;
            }

            IList<VectorSSModuleDefinition> modules = activeModuleController.InstalledModules;
            for (int i = 0; i < modules.Count; i++)
            {
                VectorSSModuleDefinition module = modules[i];
                if (module != null && module.widget != VectorSSModuleWidget.None)
                {
                    activeModuleHud.UpdateWidget(module.id, activeModuleController.GetWidgetValue(module.id), activeModuleController.GetWidgetNormalized(module.id));
                }
            }
        }

        private GameObject CreateOutlinedPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, float outlineMultiplier)
        {
            if (hasInvertedHullOutline)
            {
                CreatePrimitive(name + " Outline", type, parent, localPosition, localRotation, localScale * outlineMultiplier, outlineMaterial, false);
            }

            return CreatePrimitive(name, type, parent, localPosition, localRotation, localScale, material, false);
        }

        private GameObject CreateOutlinedChamferedBox(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, float outlineMultiplier, float bevel)
        {
            if (hasInvertedHullOutline)
            {
                CreateChamferedBox(name + " Outline", parent, localPosition, localRotation, localScale * outlineMultiplier, outlineMaterial, false, bevel);
            }

            return CreateChamferedBox(name, parent, localPosition, localRotation, localScale, material, false, bevel);
        }

        private GameObject CreateOutlinedPrism(Transform parent, string name, int sides, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, float outlineMultiplier)
        {
            if (hasInvertedHullOutline)
            {
                LowPolyMeshFactory.CreatePrism(name + " Outline", parent, sides, localPosition, localRotation, localScale * outlineMultiplier, outlineMaterial, false);
            }

            return LowPolyMeshFactory.CreatePrism(name, parent, sides, localPosition, localRotation, localScale, material, false);
        }

        private GameObject CreateOutlinedWedge(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, float outlineMultiplier)
        {
            if (hasInvertedHullOutline)
            {
                LowPolyMeshFactory.CreateWedge(name + " Outline", parent, localPosition, localRotation, localScale * outlineMultiplier, outlineMaterial, false);
            }

            return LowPolyMeshFactory.CreateWedge(name, parent, localPosition, localRotation, localScale, material, false);
        }

        private void CreateLowPolyFin(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            CreateOutlinedWedge(parent, name, localPosition, localRotation, new Vector3(0.34f, 0.54f, 2.4f), playerAccentMaterial, 1.08f);
        }

        private GameObject CreateChamferedBox(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider, float bevel)
        {
            return LowPolyMeshFactory.CreateChamferedBox(name, parent, localPosition, localRotation, localScale, material, keepCollider, bevel);
        }

        private GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }

            if (!keepCollider)
            {
                Collider collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            return primitive;
        }

        private Material CreateMaterial(string name, Color color, bool toon)
        {
            Shader shader = toon ? Shader.Find("GTX/ToonCel") : Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            if (material.HasProperty("_ShadowColor"))
            {
                Color shadow = Color.Lerp(color, new Color(0.02f, 0.025f, 0.04f, color.a), 0.46f);
                material.SetColor("_ShadowColor", shadow);
            }

            if (material.HasProperty("_Steps"))
            {
                material.SetFloat("_Steps", 2f);
            }

            return material;
        }

        private Material CreateOutlineMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("GTX/InvertedHullOutline");
            hasInvertedHullOutline = shader != null;
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.renderQueue = 2001;
            return material;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static WheelFrictionCurve TireFriction(float stiffness)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = 0.34f,
                extremumValue = 1f,
                asymptoteSlip = 0.72f,
                asymptoteValue = 0.72f,
                stiffness = stiffness
            };
        }

        private readonly struct TrackPose
        {
            public readonly Vector3 position;
            public readonly Vector3 forward;
            public readonly Vector3 right;
            public readonly float width;

            public TrackPose(Vector3 position, Vector3 forward, Vector3 right, float width)
            {
                this.position = position;
                this.forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                this.right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
                this.width = width;
            }

            public Quaternion rotation
            {
                get { return Quaternion.LookRotation(forward, Vector3.up); }
            }
        }

        private readonly struct RuntimeTrackRoute
        {
            public readonly Vector3[] samples;
            public readonly float[] widths;
            private readonly float[] cumulativeDistances;
            private readonly float totalLength;
            public float TotalLength
            {
                get { return totalLength; }
            }

            private RuntimeTrackRoute(Vector3[] samples, float[] widths, float[] cumulativeDistances, float totalLength)
            {
                this.samples = samples;
                this.widths = widths;
                this.cumulativeDistances = cumulativeDistances;
                this.totalLength = totalLength;
            }

            public static RuntimeTrackRoute FromSamples(Vector3[] samples, float[] widths)
            {
                float[] cumulativeDistances = new float[samples.Length];
                for (int i = 1; i < samples.Length; i++)
                {
                    cumulativeDistances[i] = cumulativeDistances[i - 1] + Vector3.Distance(samples[i - 1], samples[i]);
                }

                return new RuntimeTrackRoute(samples, widths, cumulativeDistances, cumulativeDistances[cumulativeDistances.Length - 1]);
            }

            public TrackPose PoseAtDistance(float distance)
            {
                if (samples == null || samples.Length < 2 || cumulativeDistances == null || cumulativeDistances.Length < 2)
                {
                    return new TrackPose(Vector3.zero, Vector3.forward, Vector3.right, 18f);
                }

                float wrappedDistance = totalLength > 0.01f ? Mathf.Repeat(distance, totalLength) : 0f;
                int segmentIndex = samples.Length - 2;
                for (int i = 0; i < cumulativeDistances.Length - 1; i++)
                {
                    if (cumulativeDistances[i + 1] >= wrappedDistance)
                    {
                        segmentIndex = i;
                        break;
                    }
                }

                int nextIndex = Mathf.Min(samples.Length - 1, segmentIndex + 1);
                float segmentLength = Mathf.Max(0.001f, cumulativeDistances[nextIndex] - cumulativeDistances[segmentIndex]);
                float t = Mathf.Clamp01((wrappedDistance - cumulativeDistances[segmentIndex]) / segmentLength);
                Vector3 position = Vector3.Lerp(samples[segmentIndex], samples[nextIndex], t);
                Vector3 direction = samples[nextIndex] - samples[segmentIndex];
                Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
                if (flatDirection.sqrMagnitude < 0.001f)
                {
                    flatDirection = Vector3.forward;
                }

                Vector3 forward = flatDirection.normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                float width = 18f;
                if (widths != null && widths.Length > 0)
                {
                    width = Mathf.Lerp(widths[Mathf.Clamp(segmentIndex, 0, widths.Length - 1)], widths[Mathf.Clamp(nextIndex, 0, widths.Length - 1)], t);
                }

                return new TrackPose(position, forward, right, width);
            }

            public float DistanceAlongRoute(Vector3 worldPosition)
            {
                if (samples == null || samples.Length < 2 || cumulativeDistances == null || cumulativeDistances.Length < 2)
                {
                    return 0f;
                }

                int bestIndex = 0;
                float bestDistance = float.MaxValue;
                Vector2 target = new Vector2(worldPosition.x, worldPosition.z);
                for (int i = 0; i < samples.Length; i++)
                {
                    Vector2 sample = new Vector2(samples[i].x, samples[i].z);
                    float distance = Vector2.SqrMagnitude(target - sample);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                return cumulativeDistances[Mathf.Clamp(bestIndex, 0, cumulativeDistances.Length - 1)];
            }
        }

        private readonly struct PlayerRig
        {
            public readonly GameObject root;
            public readonly Rigidbody body;
            public readonly VehicleTuning tuning;
            public readonly VehicleController vehicle;
            public readonly FlowState flowState;
            public readonly RuntimeImpactEffects effects;
            public readonly SideSlamController sideSlam;
            public readonly BoostRamDetector boostRam;
            public readonly SpinGuardController spinGuard;
            public readonly FlowVisualController visuals;
            public readonly Transform boostTrail;
            public readonly VectorSSVehicleModuleController moduleController;

            public PlayerRig(GameObject root, Rigidbody body, VehicleTuning tuning, VehicleController vehicle, FlowState flowState, RuntimeImpactEffects effects, SideSlamController sideSlam, BoostRamDetector boostRam, SpinGuardController spinGuard, FlowVisualController visuals, Transform boostTrail, VectorSSVehicleModuleController moduleController)
            {
                this.root = root;
                this.body = body;
                this.tuning = tuning;
                this.vehicle = vehicle;
                this.flowState = flowState;
                this.effects = effects;
                this.sideSlam = sideSlam;
                this.boostRam = boostRam;
                this.spinGuard = spinGuard;
                this.visuals = visuals;
                this.boostTrail = boostTrail;
                this.moduleController = moduleController;
            }
        }
    }
}
