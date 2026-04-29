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
    public sealed partial class GTXBootstrapper : MonoBehaviour
    {
        private enum GarageTab
        {
            Build,
            Modules,
            Tuning,
            Hud
        }

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
        private Material launchSmokeMaterial;
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
        private readonly List<SimpleRouteRivalAI> activeRivals = new List<SimpleRouteRivalAI>();
        private readonly List<Collider> startGhostPlayerColliders = new List<Collider>();
        private readonly List<Collider> startGhostRivalColliders = new List<Collider>();
        private VectorSSVehicleModuleController activeModuleController;
        private VectorSSModuleHUD activeModuleHud;
        private bool hasActivePlayer;
        private float raceStartTime;
        private float bestRouteDistance;
        private float currentRouteDistance;
        private float previousRouteDistance;
        private int currentLap;
        private int targetLaps = 1;
        private int nextCheckpointIndex;
        private bool hasRouteProgress;
        private float nextRazorNearMissTime;
        private int combatScore;
        private Vector2 garageScroll;
        private string garageMessage = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle focusStyle;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle cardStyle;
        private GUIStyle scrollStyle;
        private Texture2D panelTexture;
        private Texture2D cardTexture;
        private Texture2D focusTexture;
        private bool startRaceQueued;
        private bool restartRaceQueued;
        private bool racePaused;
        private float timeScaleBeforePause = 1f;
        private float raceCountdownStartTime;
        private float raceCountdownGoTime;
        private float raceStartGhostUntil;
        private bool raceLaunched;
        private bool raceStartGhostActive;
        private int menuFocusIndex;
        private GarageTab activeGarageTab = GarageTab.Build;
        private int lastPreviewMapIndex = -1;
        private int lastPreviewVehicleIndex = -1;
        private Transform previewRoot;
        private Camera mapPreviewCamera;
        private Camera vehiclePreviewCamera;
        private RenderTexture mapPreviewTexture;
        private RenderTexture vehiclePreviewTexture;
        private Transform vehiclePreviewSpinRoot;
        private readonly Dictionary<string, Rect> movableGuiRects = new Dictionary<string, Rect>();
        private readonly List<Transform> mapPreviewSpinRoots = new List<Transform>();
        private string activeGuiDragId;
        private string activeGuiResizeId;
        private Vector2 activeGuiPointerOffset;
        private Vector2 activeGuiResizeStartMouse;
        private Vector2 activeGuiResizeStartSize;
        private bool pausePanelDragging;
        private Vector2 pausePanelDragOffset;
        private static readonly float[] CheckpointFractions = { 0.25f, 0.5f, 0.75f };
        private static readonly Color[] RivalBlipColors =
        {
            VectrStyleTokens.SignalRed,
            VectrStyleTokens.SafetyOrange,
            VectrStyleTokens.ElectricCyan
        };
        private const float RaceGridDistance = 16f;
        private const float RaceStartLineDistance = 22f;
        private const float RaceGridLaneOffset = 4.2f;
        private const float RaceGridRowSpacing = 7.5f;
        private const float RaceGridSpawnLift = 0.28f;
        private const float RaceCountdownSeconds = 3f;
        private const float RaceGoBannerSeconds = 0.85f;
        private const float RaceStartGhostSeconds = 2.75f;
        private const float DefaultGuiWindowScale = 2f;
        private const int MaxRivalCount = 5;

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
            EnsureMenuPreviews();
            screen = VectorSSScreen.MainMenu;
            SetRaceHudVisible(false);
        }

        private void OnDestroy()
        {
            if (racePaused || Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = Mathf.Approximately(timeScaleBeforePause, 0f) ? 1f : timeScaleBeforePause;
            }
        }

        private void Update()
        {
            ProcessQueuedGuiActions();
            HandleControllerMenuInput();
            UpdateMenuPreviews();

            if (screen == VectorSSScreen.Racing && hasActivePlayer)
            {
                UpdateRaceCountdownGate();
                if (activePlayer.vehicle != null && playerProfile != null)
                {
                    activePlayer.vehicle.AutomaticTransmission = playerProfile.tuning.automaticTransmission;
                }

                if (raceLaunched)
                {
                    UpdateRaceCompletion();
                    UpdateRazorNearMissFlow();
                }

                UpdateRaceStartGhosting();

                UpdateModuleHud();
            }
            else if (screen == VectorSSScreen.Pitblock)
            {
                UpdatePitblock();
            }
            else if (screen == VectorSSScreen.Paused && hasActivePlayer)
            {
                UpdateModuleHud();
                SyncPausedModuleHudLayout();
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

            switch (screen)
            {
                case VectorSSScreen.Pitblock:
                    DrawPitblockOverlay();
                    break;
                case VectorSSScreen.Paused:
                    DrawPauseMenu();
                    break;
                case VectorSSScreen.MapSelect:
                    DrawMapSelect();
                    break;
                case VectorSSScreen.VehicleSelect:
                    DrawVehicleSelect();
                    break;
                case VectorSSScreen.RaceSettings:
                    DrawRaceSettings();
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
        }

        private void BuildMaterials()
        {
            roadMaterial = CreateMaterial("VECTR Asphalt Navy Cel", VectrStyleTokens.AsphaltNavy, true);
            barrierMaterial = CreateMaterial("VECTR Bone Concrete Barrier", VectrStyleTokens.BoneWhite, true);
            stripeMaterial = CreateMaterial("VECTR Rally Route Stripe", VectrStyleTokens.ElectricCyan, true);
            playerMaterial = CreateMaterial("VECTR Vehicle Body Paint", VectrStyleTokens.VehicleBody(VectorSSVehicleId.Hammer), true);
            playerAccentMaterial = CreateMaterial("VECTR Vehicle Accent Paint", VectrStyleTokens.VehicleAccent(VectorSSVehicleId.Hammer), true);
            playerSecondaryMaterial = CreateMaterial("VECTR Vehicle Secondary Paint", VectrStyleTokens.VehicleSecondary(VectorSSVehicleId.Hammer), true);
            glassMaterial = CreateMaterial("VECTR Smoked Rally Glass", new Color(0.34f, 0.43f, 0.44f, 1f), true);
            rivalMaterial = CreateMaterial("VECTR Rival Olive Dummy", VectrStyleTokens.AcidYellowGreen, true);
            wheelMaterial = CreateMaterial("VECTR Rubber Tire Paint", VectrStyleTokens.RubberBlack, true);
            outlineMaterial = CreateOutlineMaterial("VECTR Heavy Blackline Ink", VectrStyleTokens.InkBlack);
            inkMaterial = CreateMaterial("VECTR Ink Black Solid", VectrStyleTokens.InkBlack, false);
            boostTrailMaterial = CreateMaterial("VECTR Boost Exhaust Ribbon", VectrStyleTokens.WithAlpha(VectrStyleTokens.ElectricCyan, 0.82f), false);
            launchSmokeMaterial = CreateParticleMaterial("VECTR Dust And Tire Smoke", new Color(0.45f, 0.43f, 0.36f, 0.48f));
            trackMarkerMaterial = CreateMaterial("VECTR Warning Orange Marker", VectrStyleTokens.SafetyOrange, true);
            trackMarkerBlueMaterial = CreateMaterial("VECTR Blue Rally Marker", VectrStyleTokens.ElectricCyan, true);
            desertMaterial = CreateMaterial("VECTR Track Ground Base", VectrStyleTokens.WarmConcreteGray, true);
            pitFloorMaterial = CreateMaterial("VECTR Garage Oil Concrete", VectrStyleTokens.OilGray, true);
            pitPropMaterial = CreateMaterial("VECTR Garage Bone Panels", VectrStyleTokens.BoneWhite, true);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.64f, 1f);
            RenderSettings.ambientEquatorColor = VectrStyleTokens.WarmConcreteGray;
            RenderSettings.ambientGroundColor = VectrStyleTokens.OilGray;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.50f, 0.52f, 0.48f, 1f);
            RenderSettings.fogDensity = 0.0056f;

            GameObject lightObject = new GameObject("VECTR Hard Toon Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.58f;
            sun.color = new Color(0.96f, 0.86f, 0.68f, 1f);
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
            DrawMenuBackdrop();
            Rect hero = MovableResizableRect("main-hero", new Rect(64f, 54f, Screen.width - 128f, Screen.height - 108f), new Vector2(720f, 520f));
            GUI.Box(hero, string.Empty, panelStyle);
            DrawResizeGrip(hero);
            GUI.Label(new Rect(hero.x + 34f, hero.y + 28f, 520f, 72f), "VECTOR SS", titleStyle);
            GUI.Label(new Rect(hero.x + 38f, hero.y + 102f, 560f, 28f), "BLACKLINE PROTOTYPE / GARAGE LOOP", bodyStyle);

            Rect mapPreview = new Rect(hero.x + hero.width - 468f, hero.y + 56f, 398f, 228f);
            DrawRenderPreview(mapPreview, mapPreviewTexture);
            GUI.Label(new Rect(mapPreview.x, mapPreview.yMax + 10f, mapPreview.width, 28f), selectedMap.displayName.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(mapPreview.x, mapPreview.yMax + 42f, mapPreview.width, 52f), selectedMap.theme, bodyStyle);

            Rect vehiclePreview = new Rect(hero.x + hero.width - 468f, hero.y + 372f, 398f, 228f);
            DrawRenderPreview(vehiclePreview, vehiclePreviewTexture);
            GUI.Label(new Rect(vehiclePreview.x, vehiclePreview.yMax + 10f, vehiclePreview.width, 28f), selectedVehicle.fullName.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(vehiclePreview.x, vehiclePreview.yMax + 42f, vehiclePreview.width, 44f), selectedVehicle.role, bodyStyle);

            Rect buttonColumn = new Rect(hero.x + 38f, hero.y + 198f, 430f, 292f);
            if (DrawFocusedButton(new Rect(buttonColumn.x, buttonColumn.y, buttonColumn.width, 62f), "START GAME", 0))
            {
                EnterPitblock();
                menuFocusIndex = 0;
            }

            if (DrawFocusedButton(new Rect(buttonColumn.x, buttonColumn.y + 78f, buttonColumn.width, 56f), "GARAGE", 1))
            {
                screen = VectorSSScreen.Garage;
                menuFocusIndex = 0;
            }

            if (DrawFocusedButton(new Rect(buttonColumn.x, buttonColumn.y + 148f, buttonColumn.width, 56f), "QUICK RACE", 2))
            {
                startRaceQueued = true;
            }

            GUI.Label(new Rect(buttonColumn.x, buttonColumn.y + 232f, buttonColumn.width, 54f), playerProfile.resources.ToString(), headerStyle);
            GUI.Label(new Rect(hero.x + 38f, hero.yMax - 46f, hero.width - 76f, 26f), "D-PAD / LEFT STICK NAVIGATE    CIRCLE CONFIRM    X BACK", smallStyle);
        }

        private void DrawMapSelect()
        {
            DrawMenuBackdrop();
            DrawCarouselHeader("STAGE SELECT", "LEFT / RIGHT TO SCROLL STAGES");
            int selectedIndex = IndexOfMap(selectedMap);
            DrawStageCarousel(selectedIndex);

            Rect preview = MovableResizableRect("stage-preview", new Rect(Screen.width * 0.5f - 300f, 164f, 600f, 338f), new Vector2(360f, 220f));
            DrawRenderPreview(preview, mapPreviewTexture);
            DrawResizeGrip(preview);
            VectorSSMapDefinition map = selectedMap;
            GUI.Label(new Rect(82f, 534f, Screen.width - 164f, 42f), map.displayName.ToUpperInvariant(), titleStyle);
            GUI.Label(new Rect(84f, 584f, Screen.width - 168f, 30f), map.purpose + " / " + map.lapCount + " LAP", headerStyle);
            GUI.Label(new Rect(84f, 622f, Screen.width - 168f, 48f), map.theme, bodyStyle);
            GUI.Label(new Rect(84f, 674f, Screen.width - 168f, 30f), "BASE " + map.baseReward + "     BONUS " + map.mapBonus, smallStyle);

            if (DrawFocusedButton(new Rect(Screen.width - 300f, Screen.height - 94f, 220f, 52f), "NEXT", 0))
            {
                screen = VectorSSScreen.VehicleSelect;
                menuFocusIndex = 1;
            }
            if (DrawFocusedButton(new Rect(80f, Screen.height - 94f, 180f, 52f), "BACK", 1))
            {
                if (pitblockRoot != null)
                {
                    EnterPitblock();
                }
                else
                {
                    screen = VectorSSScreen.MainMenu;
                }
                menuFocusIndex = 0;
            }
        }

        private void DrawVehicleSelect()
        {
            DrawMenuBackdrop();
            DrawCarouselHeader("VEHICLE SELECT", "LEFT / RIGHT TO SCROLL MACHINES");
            int selectedIndex = IndexOfVehicle(selectedVehicle);
            DrawVehicleCarousel(selectedIndex);

            Rect preview = MovableResizableRect("vehicle-preview", new Rect(Screen.width * 0.5f - 300f, 152f, 600f, 350f), new Vector2(360f, 220f));
            DrawRenderPreview(preview, vehiclePreviewTexture);
            DrawResizeGrip(preview);
            VectorSSVehicleDefinition vehicle = selectedVehicle;
            GUI.Label(new Rect(82f, 530f, Screen.width - 164f, 42f), vehicle.fullName.ToUpperInvariant(), titleStyle);
            GUI.Label(new Rect(84f, 580f, Screen.width - 168f, 30f), vehicle.vehicleClass.ToString(), headerStyle);
            GUI.Label(new Rect(84f, 616f, Screen.width - 168f, 48f), vehicle.role, bodyStyle);
            GUI.Label(new Rect(84f, 668f, Screen.width - 168f, 30f), vehicle.StatsLine, smallStyle);

            if (DrawFocusedButton(new Rect(Screen.width - 300f, Screen.height - 94f, 220f, 52f), "NEXT", 0))
            {
                screen = VectorSSScreen.RaceSettings;
                menuFocusIndex = 0;
            }
            if (DrawFocusedButton(new Rect(80f, Screen.height - 94f, 180f, 52f), "BACK", 1))
            {
                screen = VectorSSScreen.MapSelect;
                menuFocusIndex = 0;
            }
        }

        private void DrawRaceSettings()
        {
            DrawMenuBackdrop();
            DrawCarouselHeader("RACE SETTINGS", "CHOOSE LAPS AND FIELD SIZE");
            Rect panel = MovableResizableRect("race-settings-panel", new Rect(Screen.width * 0.5f - 430f, 134f, 860f, 512f), new Vector2(760f, 456f));
            GUI.Box(panel, string.Empty, panelStyle);
            DrawResizeGrip(panel);

            float margin = 34f;
            float contentX = panel.x + margin;
            float contentW = panel.width - margin * 2f;
            GUI.Label(new Rect(contentX, panel.y + 26f, contentW * 0.52f, 34f), selectedMap.displayName.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(contentX, panel.y + 62f, contentW * 0.66f, 28f), selectedVehicle.fullName.ToUpperInvariant(), bodyStyle);
            GUI.Label(new Rect(contentX, panel.y + 96f, contentW, 24f), "Configure this race before staging.", smallStyle);

            Rect lapRow = new Rect(contentX, panel.y + 142f, contentW, 112f);
            Rect rivalRow = new Rect(contentX, panel.y + 274f, contentW, 112f);
            GUI.Box(lapRow, string.Empty, cardStyle);
            GUI.Box(rivalRow, string.Empty, cardStyle);

            GUI.Label(new Rect(lapRow.x + 24f, lapRow.y + 16f, 180f, 30f), "LAPS", headerStyle);
            GUI.Label(new Rect(lapRow.x + 24f, lapRow.y + 52f, lapRow.width * 0.48f, 42f), playerProfile.tuning.endlessRace ? "ENDLESS" : playerProfile.tuning.raceLapCount.ToString("00"), titleStyle);
            GUI.Label(new Rect(lapRow.x + lapRow.width * 0.46f, lapRow.y + 24f, lapRow.width * 0.18f, 58f), playerProfile.tuning.endlessRace ? "Race only ends from pause menu." : "1 to 99 laps.", bodyStyle);

            float buttonW = Mathf.Min(136f, (lapRow.width - 500f) * 0.32f);
            buttonW = Mathf.Max(104f, buttonW);
            float rightX = lapRow.xMax - 24f - buttonW;
            float midX = rightX - 16f - buttonW;
            float leftX = midX - 16f - buttonW;
            if (DrawFocusedButton(new Rect(leftX, lapRow.y + 34f, buttonW, 46f), "- LAP", 0))
            {
                AdjustRaceLaps(-1);
            }
            if (DrawFocusedButton(new Rect(midX, lapRow.y + 34f, buttonW, 46f), "+ LAP", 1))
            {
                AdjustRaceLaps(1);
            }
            if (DrawFocusedButton(new Rect(rightX, lapRow.y + 34f, buttonW, 46f), playerProfile.tuning.endlessRace ? "ENDLESS" : "FINITE", 2))
            {
                ToggleEndlessRace();
            }

            GUI.Label(new Rect(rivalRow.x + 24f, rivalRow.y + 16f, 180f, 30f), "RIVALS", headerStyle);
            GUI.Label(new Rect(rivalRow.x + 24f, rivalRow.y + 52f, rivalRow.width * 0.48f, 42f), playerProfile.tuning.rivalCount.ToString("0"), titleStyle);
            GUI.Label(new Rect(rivalRow.x + rivalRow.width * 0.46f, rivalRow.y + 24f, rivalRow.width * 0.24f, 58f), playerProfile.tuning.rivalCount == 0 ? "Solo test drive." : "AI racers on the grid.", bodyStyle);

            float rivalMinusX = rivalRow.xMax - 24f - buttonW * 2f - 16f;
            float rivalPlusX = rivalRow.xMax - 24f - buttonW;
            if (DrawFocusedButton(new Rect(rivalMinusX, rivalRow.y + 34f, buttonW, 46f), "- RIVAL", 3))
            {
                CycleRivalCount(-1);
            }
            if (DrawFocusedButton(new Rect(rivalPlusX, rivalRow.y + 34f, buttonW, 46f), "+ RIVAL", 4))
            {
                CycleRivalCount(1);
            }

            GUI.Label(new Rect(contentX, panel.yMax - 48f, contentW * 0.56f, 24f), "D-pad / left stick moves focus. Left / right adjusts selected setting.", smallStyle);
            if (DrawFocusedButton(new Rect(panel.xMax - 244f, panel.yMax - 70f, 190f, 52f), "START RACE", 5))
            {
                startRaceQueued = true;
            }
            if (DrawFocusedButton(new Rect(panel.x + 54f, panel.yMax - 70f, 170f, 52f), "BACK", 6))
            {
                screen = VectorSSScreen.VehicleSelect;
                menuFocusIndex = 0;
            }
        }

        private void DrawMenuBackdrop()
        {
            GUI.color = new Color(0.025f, 0.032f, 0.045f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            for (int i = 0; i < 10; i++)
            {
                float y = 28f + i * 74f + Mathf.Sin(Time.time * 0.7f + i) * 8f;
                GUI.color = i % 2 == 0 ? new Color(0.05f, 0.22f, 0.34f, 0.28f) : new Color(0.95f, 0.26f, 0.05f, 0.2f);
                GUI.DrawTexture(new Rect(-40f, y, Screen.width + 80f, 3f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawCarouselHeader(string title, string hint)
        {
            GUI.Box(new Rect(52f, 38f, Screen.width - 104f, 92f), string.Empty, panelStyle);
            GUI.Label(new Rect(82f, 50f, 540f, 54f), title, titleStyle);
            GUI.Label(new Rect(86f, 100f, 520f, 22f), hint + "    CIRCLE CONFIRM    X BACK", smallStyle);
        }

        private void DrawStageCarousel(int selectedIndex)
        {
            float cardWidth = 228f;
            float centerX = Screen.width * 0.5f;
            float y = Screen.height - 156f;
            for (int i = 0; i < VectorSSCatalog.Maps.Length; i++)
            {
                int offset = i - selectedIndex;
                Rect rect = new Rect(centerX - cardWidth * 0.5f + offset * 252f, y, cardWidth, 92f);
                DrawCarouselCard(rect, VectorSSCatalog.Maps[i].displayName, VectorSSCatalog.Maps[i].purpose, i == selectedIndex);
            }
        }

        private void DrawVehicleCarousel(int selectedIndex)
        {
            float cardWidth = 218f;
            float centerX = Screen.width * 0.5f;
            float y = Screen.height - 156f;
            for (int i = 0; i < VectorSSCatalog.Vehicles.Length; i++)
            {
                int offset = i - selectedIndex;
                Rect rect = new Rect(centerX - cardWidth * 0.5f + offset * 238f, y, cardWidth, 92f);
                VectorSSVehicleDefinition vehicle = VectorSSCatalog.Vehicles[i];
                DrawCarouselCard(rect, vehicle.displayName, vehicle.vehicleClass.ToString(), i == selectedIndex);
            }
        }

        private void DrawCarouselCard(Rect rect, string title, string subtitle, bool selected)
        {
            GUI.Box(rect, string.Empty, selected ? focusStyle : cardStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 28f), title.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 48f, rect.width - 28f, 34f), subtitle, smallStyle);
        }

        private bool DrawFocusedButton(Rect rect, string label, int focusIndex)
        {
            bool focused = menuFocusIndex == focusIndex;
            GUIStyle style = focused ? focusStyle : buttonStyle;
            bool pressed = GUI.Button(rect, label, style);
            if (pressed)
            {
                menuFocusIndex = focusIndex;
            }

            return pressed;
        }

        private void DrawRenderPreview(Rect rect, RenderTexture texture)
        {
            GUI.Box(rect, string.Empty, cardStyle);
            if (texture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f), texture, ScaleMode.ScaleToFit, false);
            }
        }

        private void DrawGarage()
        {
            DrawMenuBackdrop();
            Rect frame = MovableResizableRect("garage-panel", new Rect(36f, 28f, Screen.width - 72f, Screen.height - 56f), new Vector2(980f, 620f));
            GUI.Box(frame, string.Empty, panelStyle);
            DrawResizeGrip(frame);

            Rect header = new Rect(frame.x + 26f, frame.y + 20f, frame.width - 52f, 92f);
            GUI.Label(new Rect(header.x, header.y, 440f, 48f), "VECTR BUILD BAY", titleStyle);
            GUI.Label(new Rect(header.x, header.y + 52f, 560f, 26f), selectedVehicle.fullName.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(header.x + header.width - 520f, header.y + 8f, 500f, 26f), "METAL " + playerProfile.resources.metal + "    PLASTIC " + playerProfile.resources.plastic + "    RUBBER " + playerProfile.resources.rubber, headerStyle);
            GUI.Label(new Rect(header.x + header.width - 520f, header.y + 48f, 500f, 26f), "D-PAD MOVE    LEFT/RIGHT ADJUST    CIRCLE CONFIRM    X BACK", smallStyle);

            Rect tabStrip = new Rect(frame.x + 26f, frame.y + 118f, frame.width - 52f, 54f);
            DrawGarageTabs(tabStrip);

            Rect list = new Rect(frame.x + 26f, frame.y + 188f, frame.width * 0.58f, frame.height - 292f);
            Rect details = new Rect(list.xMax + 22f, list.y, frame.xMax - list.xMax - 48f, list.height);
            GUI.Box(list, string.Empty, cardStyle);
            GUI.Box(details, string.Empty, cardStyle);
            DrawGarageActiveList(list);
            DrawGarageDetailPanel(details);

            Rect message = new Rect(frame.x + 26f, frame.yMax - 86f, frame.width - 52f, 30f);
            GUI.Label(message, string.IsNullOrEmpty(garageMessage) ? GarageTabHint() : garageMessage, smallStyle);

            int buttonStart = GarageButtonFocusStart();
            float buttonY = frame.yMax - 52f;
            float buttonW = 196f;
            if (DrawFocusedButton(new Rect(frame.x + 26f, buttonY, buttonW, 38f), "LOCK GARAGE", buttonStart))
            {
                VectorSSSaveSystem.Save(playerProfile);
                garageMessage = "Garage locked.";
            }

            if (DrawFocusedButton(new Rect(frame.xMax - 26f - buttonW * 2f - 16f, buttonY, buttonW, 38f), "ROLL OUT", buttonStart + 1))
            {
                VectorSSSaveSystem.Save(playerProfile);
                screen = VectorSSScreen.MapSelect;
                menuFocusIndex = 0;
            }

            if (DrawFocusedButton(new Rect(frame.xMax - 26f - buttonW, buttonY, buttonW, 38f), "MAIN MENU", buttonStart + 2))
            {
                VectorSSSaveSystem.Save(playerProfile);
                screen = VectorSSScreen.MainMenu;
                menuFocusIndex = 0;
            }
        }

        private void DrawGarageTabs(Rect strip)
        {
            string[] labels = { "BUILD", "MODULES", "TUNING", "HUD" };
            float gap = 10f;
            float width = (strip.width - gap * 3f) / 4f;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(strip.x + i * (width + gap), strip.y, width, strip.height);
                bool active = (int)activeGarageTab == i;
                bool focused = menuFocusIndex == i;
                GUIStyle style = focused || active ? focusStyle : buttonStyle;
                if (GUI.Button(rect, labels[i], style))
                {
                    activeGarageTab = (GarageTab)i;
                    menuFocusIndex = i;
                    garageScroll.y = 0f;
                }
            }
        }

        private void DrawGarageActiveList(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 28f), GarageTabTitle(), headerStyle);
            int itemCount = GarageActiveItemCount();
            if (itemCount <= 0)
            {
                GUI.Label(new Rect(rect.x + 18f, rect.y + 58f, rect.width - 36f, 60f), GarageEmptyText(), bodyStyle);
                return;
            }

            const float rowHeight = 72f;
            Rect clip = new Rect(rect.x + 12f, rect.y + 52f, rect.width - 24f, rect.height - 64f);
            int visibleRows = Mathf.Max(1, Mathf.FloorToInt(clip.height / rowHeight));
            int first = Mathf.Clamp(Mathf.FloorToInt(garageScroll.y / rowHeight), 0, Mathf.Max(0, itemCount - visibleRows));
            int last = Mathf.Min(itemCount, first + visibleRows);
            for (int i = first; i < last; i++)
            {
                int focusIndex = GarageItemFocusStart() + i;
                Rect row = new Rect(clip.x, clip.y + (i - first) * rowHeight, clip.width, rowHeight - 8f);
                DrawGarageRow(row, i, focusIndex);
            }

            if (itemCount > visibleRows)
            {
                GUI.Label(new Rect(rect.xMax - 92f, rect.y + 18f, 74f, 22f), (first + 1) + "-" + last + "/" + itemCount, smallStyle);
            }
        }

        private void DrawGarageRow(Rect row, int itemIndex, int focusIndex)
        {
            bool focused = menuFocusIndex == focusIndex;
            GUI.Box(row, string.Empty, focused ? focusStyle : cardStyle);
            string title;
            string meta;
            string value;
            GarageRowText(itemIndex, out title, out meta, out value);
            GUI.Label(new Rect(row.x + 16f, row.y + 8f, row.width * 0.48f, 24f), title, headerStyle);
            GUI.Label(new Rect(row.x + 16f, row.y + 34f, row.width * 0.62f, 22f), meta, smallStyle);
            GUI.Label(new Rect(row.xMax - 190f, row.y + 18f, 172f, 28f), value, bodyStyle);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                menuFocusIndex = focusIndex;
            }
        }

        private void GarageRowText(int itemIndex, out string title, out string meta, out string value)
        {
            title = string.Empty;
            meta = string.Empty;
            value = string.Empty;
            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    VectorSSUpgradeDefinition upgrade = VectorSSCatalog.Upgrades[itemIndex];
                    bool bought = playerProfile.HasUpgrade(upgrade.id);
                    bool classMatch = upgrade.preferredClass == null || upgrade.preferredClass.Value == selectedVehicle.vehicleClass;
                    title = upgrade.displayName;
                    meta = bought ? "Purchased" : classMatch ? "Available upgrade" : "Off-class but usable";
                    value = bought ? "OWNED" : upgrade.cost.ToString();
                    break;
                case GarageTab.Modules:
                    VectorSSModuleDefinition module = VectorSSCatalog.Modules[itemIndex];
                    bool supported = module.Supports(selectedVehicle);
                    bool purchased = playerProfile.HasModule(module.id);
                    bool installed = playerProfile.IsModuleInstalled(selectedVehicle.id, module.id);
                    title = module.displayName;
                    meta = module.slot + " / " + module.category;
                    value = !supported ? "LOCKED" : installed ? "INSTALLED" : purchased ? "OWNED" : module.cost.ToString();
                    break;
                case GarageTab.Tuning:
                    GarageTuningRowText(itemIndex, out title, out meta, out value);
                    break;
                case GarageTab.Hud:
                    VectorSSModuleDefinition hudModule = GarageHudModuleAt(itemIndex);
                    VectorSSModuleHudLayout layout = hudModule != null ? playerProfile.GetModuleLayout(selectedVehicle.id, hudModule, true) : null;
                    title = hudModule != null ? hudModule.displayName : "HUD Module";
                    meta = layout != null && layout.visible ? "Visible on race HUD" : "Hidden on race HUD";
                    value = layout != null ? Mathf.RoundToInt(layout.size.x) + "x" + Mathf.RoundToInt(layout.size.y) : "-";
                    break;
            }
        }

        private void DrawGarageDetailPanel(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 28f), GarageFocusLabel(menuFocusIndex), headerStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 54f, rect.width - 40f, 74f), GarageFocusDescription(), bodyStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 138f, rect.width - 40f, 26f), GaragePrimaryActionText(), smallStyle);
            DrawGarageVehicleSummary(new Rect(rect.x + 20f, rect.y + 188f, rect.width - 40f, 124f));
            GUI.Label(new Rect(rect.x + 20f, rect.yMax - 82f, rect.width - 40f, 56f), SlotUsageText(selectedVehicle), bodyStyle);
        }

        private void DrawGarageVehicleSummary(Rect rect)
        {
            GUI.Box(rect, string.Empty, panelStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 24f), selectedVehicle.displayName.ToUpperInvariant(), headerStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 38f, rect.width - 28f, 40f), selectedVehicle.role, bodyStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 84f, rect.width - 28f, 22f), selectedVehicle.StatsLine, smallStyle);
        }

        private void DrawResults()
        {
            DrawMenuBackdrop();
            Rect panel = MovableResizableRect("results-panel", new Rect(Screen.width * 0.5f - 310f, 92f, 620f, 500f), new Vector2(480f, 380f));
            GUI.Box(panel, string.Empty, panelStyle);
            DrawResizeGrip(panel);
            GUI.Label(new Rect(panel.x + 34f, panel.y + 30f, panel.width - 68f, 58f), "RACE COMPLETE", titleStyle);
            if (lastResult != null)
            {
                GUI.Label(new Rect(panel.x + 38f, panel.y + 106f, panel.width - 76f, 28f), lastResult.vehicle.displayName + " / " + lastResult.map.displayName, headerStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 150f, panel.width - 76f, 28f), "PLACE " + Ordinal(lastResult.placement) + " / " + lastResult.fieldSize + "     TIME " + lastResult.raceTime.ToString("0.0") + "s", bodyStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 176f, panel.width - 76f, 28f), "FLOW " + Mathf.RoundToInt(lastResult.flow01 * 100f) + "%     RIVALS " + Mathf.Max(0, lastResult.fieldSize - 1), bodyStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 210f, panel.width - 76f, 28f), "COMPLETION  " + lastResult.completionReward, bodyStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 246f, panel.width - 76f, 28f), "STYLE / COMBAT  " + lastResult.styleReward, bodyStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 282f, panel.width - 76f, 28f), "MAP BONUS  " + lastResult.mapReward, bodyStyle);
                GUI.Label(new Rect(panel.x + 38f, panel.y + 320f, panel.width - 76f, 34f), "EARNED  " + lastResult.Total + "   Scrap Cubes +" + lastResult.scrapCubes, headerStyle);
            }

            GUI.Label(new Rect(panel.x + 38f, panel.y + 374f, panel.width - 76f, 34f), "TOTALS  Scrap Cubes " + playerProfile.scrapCubes + "   " + playerProfile.resources, smallStyle);
            if (DrawFocusedButton(new Rect(panel.x + 38f, panel.y + 428f, 250f, 52f), "GARAGE", 0))
            {
                EnterPitblock();
                menuFocusIndex = 0;
            }
            if (DrawFocusedButton(new Rect(panel.x + panel.width - 288f, panel.y + 428f, 250f, 52f), "MAP SELECT", 1))
            {
                screen = VectorSSScreen.MapSelect;
                menuFocusIndex = 0;
            }
        }

        private void DrawRaceOverlay()
        {
            Rect positionPanel = DrawRacePositionModule();

            float minimapSize = Mathf.Clamp(Screen.height * 0.22f, 174f, 230f);
            Rect minimapRect = MovableResizableRect("race-minimap", new Rect(Screen.width - minimapSize - 40f, positionPanel.yMax + 18f, minimapSize, minimapSize), new Vector2(140f, 140f), true);
            DrawRaceMinimap(minimapRect);
            DrawRaceCountdownOverlay();
        }

        private Rect DrawRacePositionModule()
        {
            Rect rect = MovableResizableRect("race-position-panel", new Rect(Screen.width - 245f, 8f, 150f, 70f), new Vector2(220f, 96f), true);

            int placement = CalculatePlayerPlacement();
            int fieldSize = Mathf.Max(1, activeRivals.Count + 1);
            float unit = Mathf.Min(rect.width / 300f, rect.height / 132f);
            float pad = Mathf.Max(12f, 18f * unit);
            Rect titleRect = new Rect(rect.x + pad, rect.y + pad * 0.45f, rect.width - pad * 2f, Mathf.Max(24f, 30f * unit));
            Rect numberRect = new Rect(rect.x + pad, rect.y + rect.height * 0.34f, rect.width * 0.48f, rect.height * 0.58f);
            Rect slashRect = new Rect(rect.x + rect.width * 0.49f, rect.y + rect.height * 0.46f, rect.width * 0.13f, rect.height * 0.35f);
            Rect fieldRect = new Rect(rect.x + rect.width * 0.60f, rect.y + rect.height * 0.48f, rect.width * 0.30f, rect.height * 0.30f);

            GUIStyle positionStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(22f * unit), 18, 46),
                fontStyle = FontStyle.Bold
            };
            GUIStyle numberStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(58f * unit), 42, 104),
                fontStyle = FontStyle.Bold
            };
            GUIStyle slashStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(42f * unit), 28, 74),
                fontStyle = FontStyle.Bold
            };
            GUIStyle fieldStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(30f * unit), 22, 58),
                fontStyle = FontStyle.Bold
            };

            DrawOutlinedLabel(titleRect, "POSITION", positionStyle, new Color(0.36f, 0.62f, 0.30f, 1f), VectrStyleTokens.InkBlack, Mathf.Max(2f, 3f * unit));
            DrawOutlinedLabel(numberRect, placement.ToString("0"), numberStyle, VectrStyleTokens.BoneWhite, VectrStyleTokens.InkBlack, Mathf.Max(3f, 4f * unit));
            DrawOutlinedLabel(slashRect, "/", slashStyle, VectrStyleTokens.BoneWhite, VectrStyleTokens.InkBlack, Mathf.Max(2f, 3f * unit));
            DrawOutlinedLabel(fieldRect, fieldSize.ToString("0"), fieldStyle, VectrStyleTokens.BoneWhite, VectrStyleTokens.InkBlack, Mathf.Max(2f, 3f * unit));
            return rect;
        }

        private void DrawRaceCountdownOverlay()
        {
            if (!hasActivePlayer)
            {
                return;
            }

            float now = Time.time;
            if (raceLaunched && now > raceCountdownGoTime + RaceGoBannerSeconds)
            {
                return;
            }

            string label = RaceCountdownLabel(now);
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            Rect rect = new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.28f - 78f, 380f, 156f);
            GUI.Box(rect, string.Empty, panelStyle);
            GUIStyle countdownStyle = new GUIStyle(titleStyle)
            {
                fontSize = label == "GO!" ? 76 : 92,
                alignment = TextAnchor.MiddleCenter
            };
            countdownStyle.normal.textColor = label == "GO!" ? VectrStyleTokens.SafetyOrange : VectrStyleTokens.BoneWhite;
            GUI.Label(rect, label, countdownStyle);
        }

        private string RaceCountdownLabel(float now)
        {
            if (now < raceCountdownGoTime)
            {
                float remaining = raceCountdownGoTime - now;
                return Mathf.CeilToInt(remaining).ToString();
            }

            if (now <= raceCountdownGoTime + RaceGoBannerSeconds)
            {
                return "GO!";
            }

            return string.Empty;
        }

        private void DrawRaceMinimap(Rect rect)
        {
            if (activeRoute.samples == null || activeRoute.samples.Length < 2 || !hasActivePlayer || activePlayer.root == null)
            {
                return;
            }

            Transform playerTransform = activePlayer.root.transform;
            Rect mapRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);

            Vector2 min;
            Vector2 max;
            GetRouteBoundsXZ(activeRoute.samples, out min, out max);
            float scale = MinimapScale(mapRect, min, max) * 0.96f;
            Vector2 playerPosition = new Vector2(playerTransform.position.x, playerTransform.position.z);
            Vector3 forward3 = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up);
            Vector2 playerForward = forward3.sqrMagnitude > 0.001f
                ? new Vector2(forward3.x, forward3.z).normalized
                : Vector2.up;
            Vector2 playerRight = new Vector2(playerForward.y, -playerForward.x);

            for (int i = 0; i < activeRoute.samples.Length - 1; i++)
            {
                Vector2 a = WorldToPlayerCenteredMinimap(activeRoute.samples[i], mapRect, playerPosition, playerForward, playerRight, scale);
                Vector2 b = WorldToPlayerCenteredMinimap(activeRoute.samples[i + 1], mapRect, playerPosition, playerForward, playerRight, scale);
                DrawGuiLine(a, b, VectrStyleTokens.WithAlpha(VectrStyleTokens.InkBlack, 0.96f), 8f);
                DrawGuiLine(a, b, VectrStyleTokens.WithAlpha(VectrStyleTokens.MapAccent(selectedMap.id), 0.94f), 4f);
            }

            Vector2 start = WorldToPlayerCenteredMinimap(activeRoute.PoseAtDistance(RaceStartLineDistance).position, mapRect, playerPosition, playerForward, playerRight, scale);
            DrawMinimapBlip(start, VectrStyleTokens.HotMagenta, 7f);

            float markerDistance = nextCheckpointIndex < CheckpointFractions.Length
                ? activeRoute.TotalLength * CheckpointFractions[nextCheckpointIndex]
                : RaceStartLineDistance;
            Vector2 checkpoint = WorldToPlayerCenteredMinimap(activeRoute.PoseAtDistance(markerDistance).position, mapRect, playerPosition, playerForward, playerRight, scale);
            DrawMinimapBlip(checkpoint, VectrStyleTokens.SafetyOrange, 8f);

            for (int i = 0; i < activeRivals.Count; i++)
            {
                SimpleRouteRivalAI rivalAi = activeRivals[i];
                if (rivalAi == null)
                {
                    continue;
                }

                Vector2 rival = WorldToPlayerCenteredMinimap(rivalAi.transform.position, mapRect, playerPosition, playerForward, playerRight, scale);
                DrawMinimapBlip(rival, RivalBlipColors[i % RivalBlipColors.Length], 6f);
            }

            Vector2 player = mapRect.center;
            DrawMinimapBlip(player, VectrStyleTokens.BoneWhite, 10f);
            DrawGuiLine(player, player + Vector2.up * Mathf.Clamp(mapRect.height * 0.12f, 12f, 28f), VectrStyleTokens.BoneWhite, 3f);
        }

        private static void GetRouteBoundsXZ(Vector3[] samples, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < samples.Length; i++)
            {
                Vector2 point = new Vector2(samples[i].x, samples[i].z);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        private static float MinimapScale(Rect rect, Vector2 min, Vector2 max)
        {
            Vector2 size = max - min;
            float width = Mathf.Max(1f, size.x);
            float height = Mathf.Max(1f, size.y);
            return Mathf.Min(rect.width / width, rect.height / height) * 0.86f;
        }

        private static Vector2 WorldToMinimap(Vector3 world, Rect rect, Vector2 min, Vector2 max, float scale)
        {
            Vector2 center = (min + max) * 0.5f;
            Vector2 offset = new Vector2(world.x - center.x, world.z - center.y) * scale;
            return rect.center + new Vector2(offset.x, -offset.y);
        }

        private static Vector2 WorldToPlayerCenteredMinimap(Vector3 world, Rect rect, Vector2 playerPosition, Vector2 playerForward, Vector2 playerRight, float scale)
        {
            Vector2 delta = new Vector2(world.x, world.z) - playerPosition;
            float localX = Vector2.Dot(delta, playerRight);
            float localY = Vector2.Dot(delta, playerForward);
            return rect.center + new Vector2(localX * scale, -localY * scale);
        }

        private static void DrawMinimapBlip(Vector2 position, Color color, float size)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(position.x - size * 0.5f, position.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawGuiLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color fill, Color outline, float thickness)
        {
            Color previousColor = GUI.color;
            Color previousText = style.normal.textColor;
            style.normal.textColor = outline;
            int steps = Mathf.Max(1, Mathf.CeilToInt(thickness));
            for (int x = -steps; x <= steps; x++)
            {
                for (int y = -steps; y <= steps; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (x * x + y * y > steps * steps)
                    {
                        continue;
                    }

                    GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), text, style);
                }
            }

            style.normal.textColor = fill;
            GUI.Label(rect, text, style);
            style.normal.textColor = previousText;
            GUI.color = previousColor;
        }

        private Rect MovableResizableRect(string id, Rect defaultRect, Vector2 minSize, bool dragWholeRect = false)
        {
            Rect rect;
            if (!movableGuiRects.TryGetValue(id, out rect))
            {
                rect = ScaledDefaultGuiWindow(defaultRect);
            }

            rect.width = Mathf.Max(minSize.x, rect.width);
            rect.height = Mathf.Max(minSize.y, rect.height);
            rect = ClampGuiRectToScreen(rect);
            movableGuiRects[id] = rect;

            Event current = Event.current;
            if (current == null)
            {
                return rect;
            }

            Rect resizeHandle = new Rect(rect.xMax - 22f, rect.yMax - 22f, 22f, 22f);
            Rect dragHandle = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect leftHandle = new Rect(rect.x, rect.y, 12f, rect.height);
            Rect rightHandle = new Rect(rect.xMax - 12f, rect.y, 12f, rect.height);
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                if (resizeHandle.Contains(current.mousePosition))
                {
                    activeGuiResizeId = id;
                    activeGuiDragId = null;
                    activeGuiResizeStartMouse = current.mousePosition;
                    activeGuiResizeStartSize = rect.size;
                    current.Use();
                }
                else if (dragWholeRect ? rect.Contains(current.mousePosition) : dragHandle.Contains(current.mousePosition) || leftHandle.Contains(current.mousePosition) || rightHandle.Contains(current.mousePosition))
                {
                    activeGuiDragId = id;
                    activeGuiResizeId = null;
                    activeGuiPointerOffset = current.mousePosition - rect.position;
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (activeGuiDragId == id)
                {
                    rect.position = current.mousePosition - activeGuiPointerOffset;
                    rect = ClampGuiRectToScreen(rect);
                    movableGuiRects[id] = rect;
                    current.Use();
                }
                else if (activeGuiResizeId == id)
                {
                    Vector2 delta = current.mousePosition - activeGuiResizeStartMouse;
                    rect.width = Mathf.Max(minSize.x, activeGuiResizeStartSize.x + delta.x);
                    rect.height = Mathf.Max(minSize.y, activeGuiResizeStartSize.y + delta.y);
                    rect = ClampGuiRectToScreen(rect);
                    movableGuiRects[id] = rect;
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseUp)
            {
                if (activeGuiDragId == id || activeGuiResizeId == id)
                {
                    activeGuiDragId = null;
                    activeGuiResizeId = null;
                    current.Use();
                }
            }

            return rect;
        }

        private static Rect ScaledDefaultGuiWindow(Rect rect)
        {
            if (DefaultGuiWindowScale <= 1.001f)
            {
                return rect;
            }

            Vector2 center = rect.center;
            rect.width = Mathf.Min(rect.width * DefaultGuiWindowScale, Mathf.Max(1f, Screen.width - 8f));
            rect.height = Mathf.Min(rect.height * DefaultGuiWindowScale, Mathf.Max(1f, Screen.height - 8f));
            rect.center = center;
            return rect;
        }

        private static Rect ClampGuiRectToScreen(Rect rect)
        {
            float maxX = Mathf.Max(0f, Screen.width - rect.width);
            float maxY = Mathf.Max(0f, Screen.height - rect.height);
            rect.x = Mathf.Clamp(rect.x, 0f, maxX);
            rect.y = Mathf.Clamp(rect.y, 0f, maxY);
            return rect;
        }

        private void DrawResizeGrip(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = VectrStyleTokens.WithAlpha(VectrStyleTokens.InkBlack, 0.52f);
            GUI.DrawTexture(new Rect(rect.xMax - 18f, rect.yMax - 5f, 14f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 11f, rect.yMax - 11f, 7f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 5f, rect.yMax - 18f, 3f, 14f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static string Ordinal(int value)
        {
            int safeValue = Mathf.Max(1, value);
            int lastTwo = safeValue % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return safeValue + "th";
            }

            switch (safeValue % 10)
            {
                case 1:
                    return safeValue + "st";
                case 2:
                    return safeValue + "nd";
                case 3:
                    return safeValue + "rd";
                default:
                    return safeValue + "th";
            }
        }

        private void DrawPauseMenu()
        {
            DrawRaceOverlay();

            Rect panel = PausePanelRect();
            GUI.Box(panel, string.Empty, panelStyle);
            DrawResizeGrip(panel);
            GUI.Label(new Rect(panel.x + 22f, panel.y + 18f, panel.width - 44f, 44f), "PAUSED", titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 68f, panel.width - 48f, 26f), selectedMap.displayName.ToUpperInvariant() + " / " + selectedVehicle.displayName.ToUpperInvariant(), smallStyle);

            if (DrawFocusedButton(new Rect(panel.x + 24f, panel.y + 112f, panel.width - 48f, 46f), "RESUME", 0))
            {
                ResumeRace();
            }

            if (DrawFocusedButton(new Rect(panel.x + 24f, panel.y + 166f, panel.width - 48f, 46f), "RESTART RACE", 1))
            {
                RestartRace();
            }

            if (DrawFocusedButton(new Rect(panel.x + 24f, panel.y + 220f, panel.width - 48f, 46f), "LEAVE RACE", 2))
            {
                LeaveRace();
            }
        }

        private Rect PausePanelRect()
        {
            const string id = "pause-panel";
            Rect rect;
            if (!movableGuiRects.TryGetValue(id, out rect))
            {
                rect = ScaledDefaultGuiWindow(new Rect(24f, 24f, 360f, 300f));
            }

            rect.width = Mathf.Max(300f, rect.width);
            rect.height = Mathf.Max(248f, rect.height);
            rect = ClampGuiRectToScreen(rect);
            movableGuiRects[id] = rect;

            Event current = Event.current;
            if (current == null)
            {
                return rect;
            }

            Rect resizeHandle = new Rect(rect.xMax - 22f, rect.yMax - 22f, 22f, 22f);
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                if (resizeHandle.Contains(current.mousePosition))
                {
                    activeGuiResizeId = id;
                    pausePanelDragging = false;
                    activeGuiResizeStartMouse = current.mousePosition;
                    activeGuiResizeStartSize = rect.size;
                }
                else
                {
                    pausePanelDragging = true;
                    activeGuiResizeId = null;
                    pausePanelDragOffset = current.mousePosition - rect.position;
                }

                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (activeGuiResizeId == id)
                {
                    Vector2 delta = current.mousePosition - activeGuiResizeStartMouse;
                    rect.width = Mathf.Max(300f, activeGuiResizeStartSize.x + delta.x);
                    rect.height = Mathf.Max(248f, activeGuiResizeStartSize.y + delta.y);
                    rect = ClampGuiRectToScreen(rect);
                    movableGuiRects[id] = rect;
                    current.Use();
                }
                else if (pausePanelDragging)
                {
                    rect.position = current.mousePosition - pausePanelDragOffset;
                    rect = ClampGuiRectToScreen(rect);
                    movableGuiRects[id] = rect;
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseUp)
            {
                if (pausePanelDragging || activeGuiResizeId == id)
                {
                    pausePanelDragging = false;
                    activeGuiResizeId = null;
                    current.Use();
                }
            }

            return rect;
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
            if (restartRaceQueued)
            {
                restartRaceQueued = false;
                ResumeRaceTime();
                StartRace();
                return;
            }

            if (startRaceQueued)
            {
                startRaceQueued = false;
                ResumeRaceTime();
                StartRace();
            }
        }

        private void HandleControllerMenuInput()
        {
            if (screen == VectorSSScreen.Racing)
            {
                if (PausePressed())
                {
                    PauseRace();
                }

                return;
            }

            EnsureProgressionState();
            if (screen == VectorSSScreen.Paused)
            {
                HandlePauseMenuInput();
                return;
            }

            if (screen == VectorSSScreen.Pitblock)
            {
                return;
            }

            if (GTXInput.ButtonDown(0))
            {
                ControllerBack();
                return;
            }

            if (ControllerMenuLeft())
            {
                ControllerHorizontal(-1);
            }
            else if (ControllerMenuRight())
            {
                ControllerHorizontal(1);
            }

            if (ControllerMenuDown())
            {
                ControllerVertical(1);
            }
            else if (ControllerMenuUp())
            {
                ControllerVertical(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                ControllerVertical(1);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                ControllerVertical(-1);
            }

            if (GTXInput.ButtonDown(1))
            {
                ControllerConfirm();
            }
            else if (GTXInput.ButtonDown(2))
            {
                startRaceQueued = screen == VectorSSScreen.MainMenu || screen == VectorSSScreen.RaceSettings;
            }
            else if (GTXInput.ButtonDown(3))
            {
                if (screen == VectorSSScreen.MainMenu || screen == VectorSSScreen.Results)
                {
                    if (screen == VectorSSScreen.Results)
                    {
                        EnterPitblock();
                    }
                    else
                    {
                        screen = VectorSSScreen.Garage;
                    }
                }
                else if (screen == VectorSSScreen.Garage)
                {
                    activeGarageTab = (GarageTab)WrapIndex((int)activeGarageTab + 1, 4);
                    menuFocusIndex = (int)activeGarageTab;
                    garageScroll.y = 0f;
                }
            }
        }

        private void ControllerVertical(int delta)
        {
            menuFocusIndex = WrapIndex(menuFocusIndex + delta, MenuFocusCount());
            if (screen == VectorSSScreen.Garage)
            {
                NudgeGarageScrollToFocus();
            }
        }

        private void NudgeGarageScrollToFocus()
        {
            int itemIndex = menuFocusIndex - GarageItemFocusStart();
            if (itemIndex < 0)
            {
                garageScroll.y = 0f;
                return;
            }

            const float rowHeight = 72f;
            float targetY = itemIndex * rowHeight;
            float viewHeight = Mathf.Max(72f, Screen.height - 360f);
            if (targetY < garageScroll.y + 36f)
            {
                garageScroll.y = Mathf.Max(0f, targetY - 36f);
            }
            else if (targetY > garageScroll.y + viewHeight - 96f)
            {
                garageScroll.y = Mathf.Max(0f, targetY - viewHeight + 96f);
            }
        }

        private void ControllerHorizontal(int delta)
        {
            if (screen == VectorSSScreen.MapSelect)
            {
                int index = IndexOfMap(selectedMap);
                selectedMap = VectorSSCatalog.Maps[WrapIndex(index + delta, VectorSSCatalog.Maps.Length)];
                playerProfile.selectedMap = selectedMap.id;
                VectorSSSaveSystem.Save(playerProfile);
            }
            else if (screen == VectorSSScreen.VehicleSelect)
            {
                int index = IndexOfVehicle(selectedVehicle);
                selectedVehicle = VectorSSCatalog.Vehicles[WrapIndex(index + delta, VectorSSCatalog.Vehicles.Length)];
                playerProfile.selectedVehicle = selectedVehicle.id;
                VectorSSSaveSystem.Save(playerProfile);
            }
            else if (screen == VectorSSScreen.RaceSettings)
            {
                if (menuFocusIndex <= 1)
                {
                    AdjustRaceLaps(delta);
                }
                else if (menuFocusIndex == 2)
                {
                    ToggleEndlessRace();
                }
                else if (menuFocusIndex <= 4)
                {
                    CycleRivalCount(delta);
                }
            }
            else if (screen == VectorSSScreen.Garage)
            {
                if (menuFocusIndex < GarageItemFocusStart())
                {
                    activeGarageTab = (GarageTab)WrapIndex((int)activeGarageTab + delta, 4);
                    menuFocusIndex = (int)activeGarageTab;
                    garageScroll.y = 0f;
                }
                else
                {
                    AdjustGarageFocus(delta);
                }
            }
        }

        private bool ControllerMenuUp()
        {
            return GTXInput.AxisPressedDown("GTX_DPadY", 0.5f, 10) || GTXInput.AxisPressedDown("Vertical", 0.72f, 11);
        }

        private bool ControllerMenuDown()
        {
            return GTXInput.AxisNegativePressedDown("GTX_DPadY", 0.5f, 12) || GTXInput.AxisNegativePressedDown("Vertical", 0.72f, 13);
        }

        private bool ControllerMenuLeft()
        {
            return GTXInput.AxisNegativePressedDown("GTX_DPadX", 0.5f, 14) || GTXInput.AxisNegativePressedDown("Horizontal", 0.72f, 15);
        }

        private bool ControllerMenuRight()
        {
            return GTXInput.AxisPressedDown("GTX_DPadX", 0.5f, 16) || GTXInput.AxisPressedDown("Horizontal", 0.72f, 17);
        }

        private void ControllerConfirm()
        {
            switch (screen)
            {
                case VectorSSScreen.MainMenu:
                    if (menuFocusIndex == 1)
                    {
                        screen = VectorSSScreen.Garage;
                    }
                    else if (menuFocusIndex == 2)
                    {
                        startRaceQueued = true;
                    }
                    else
                    {
                        EnterPitblock();
                    }

                    menuFocusIndex = 0;
                    break;
                case VectorSSScreen.MapSelect:
                    if (menuFocusIndex == 1)
                    {
                        if (pitblockRoot != null)
                        {
                            EnterPitblock();
                        }
                        else
                        {
                            screen = VectorSSScreen.MainMenu;
                        }
                    }
                    else
                    {
                        screen = VectorSSScreen.VehicleSelect;
                    }
                    menuFocusIndex = 0;
                    break;
                case VectorSSScreen.VehicleSelect:
                    if (menuFocusIndex == 0)
                    {
                        screen = VectorSSScreen.RaceSettings;
                        menuFocusIndex = 1;
                    }
                    else
                    {
                        screen = VectorSSScreen.MapSelect;
                        menuFocusIndex = 0;
                    }
                    break;
                case VectorSSScreen.RaceSettings:
                    if (menuFocusIndex == 0)
                    {
                        AdjustRaceLaps(-1);
                    }
                    else if (menuFocusIndex == 1)
                    {
                        AdjustRaceLaps(1);
                    }
                    else if (menuFocusIndex == 2)
                    {
                        ToggleEndlessRace();
                    }
                    else if (menuFocusIndex == 3)
                    {
                        CycleRivalCount(-1);
                    }
                    else if (menuFocusIndex == 4)
                    {
                        CycleRivalCount(1);
                    }
                    else if (menuFocusIndex == 6)
                    {
                        screen = VectorSSScreen.VehicleSelect;
                        menuFocusIndex = 0;
                    }
                    else
                    {
                        startRaceQueued = true;
                    }

                    break;
                case VectorSSScreen.Garage:
                    ActivateGarageFocus();
                    break;
                case VectorSSScreen.Results:
                    if (menuFocusIndex == 0)
                    {
                        EnterPitblock();
                    }
                    else
                    {
                        screen = VectorSSScreen.MapSelect;
                    }

                    menuFocusIndex = 0;
                    break;
            }
        }

        private void ControllerBack()
        {
            switch (screen)
            {
                case VectorSSScreen.MapSelect:
                case VectorSSScreen.Garage:
                case VectorSSScreen.Results:
                    VectorSSSaveSystem.Save(playerProfile);
                    if (pitblockRoot != null || screen == VectorSSScreen.Results)
                    {
                        EnterPitblock();
                    }
                    else
                    {
                        screen = VectorSSScreen.MainMenu;
                    }
                    menuFocusIndex = 0;
                    break;
                case VectorSSScreen.VehicleSelect:
                    screen = VectorSSScreen.MapSelect;
                    menuFocusIndex = 0;
                    break;
                case VectorSSScreen.RaceSettings:
                    screen = VectorSSScreen.VehicleSelect;
                    menuFocusIndex = 0;
                    break;
            }
        }

        private int MenuFocusCount()
        {
            switch (screen)
            {
                case VectorSSScreen.MainMenu:
                    return 3;
                case VectorSSScreen.MapSelect:
                case VectorSSScreen.Results:
                    return 2;
                case VectorSSScreen.VehicleSelect:
                    return 2;
                case VectorSSScreen.RaceSettings:
                    return 7;
                case VectorSSScreen.Paused:
                    return 3;
                case VectorSSScreen.Garage:
                    return GarageFocusCount();
                default:
                    return 1;
            }
        }

        private int GarageFocusCount()
        {
            return GarageItemFocusStart() + GarageActiveItemCount() + 3;
        }

        private int GarageItemFocusStart()
        {
            return 4;
        }

        private int GarageTuningFocusCount()
        {
            return selectedVehicle != null && selectedVehicle.isBike ? 14 : 12;
        }

        private int GarageActiveItemCount()
        {
            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    return VectorSSCatalog.Upgrades.Length;
                case GarageTab.Modules:
                    return VectorSSCatalog.Modules.Length;
                case GarageTab.Tuning:
                    return GarageTuningFocusCount();
                case GarageTab.Hud:
                    return GarageHudModuleCount();
                default:
                    return 0;
            }
        }

        private int GarageButtonFocusStart()
        {
            return GarageItemFocusStart() + GarageActiveItemCount();
        }

        private int GarageUpgradeFocusStart()
        {
            return GarageItemFocusStart();
        }

        private int GarageModuleFocusStart()
        {
            return GarageItemFocusStart();
        }

        private void AdjustGarageFocus(int delta)
        {
            int itemIndex = menuFocusIndex - GarageItemFocusStart();
            if (itemIndex < 0)
            {
                return;
            }

            if (activeGarageTab == GarageTab.Hud)
            {
                AdjustGarageHudFocus(itemIndex, delta);
                return;
            }

            if (activeGarageTab != GarageTab.Tuning)
            {
                return;
            }

            const float smallStep = 0.05f;
            switch (itemIndex)
            {
                case 0:
                    playerProfile.tuning.steering = Mathf.Clamp(playerProfile.tuning.steering + delta * smallStep, 0.45f, 1.85f);
                    break;
                case 1:
                    playerProfile.tuning.brakeBias = Mathf.Clamp(playerProfile.tuning.brakeBias + delta * smallStep, 0.45f, 1.85f);
                    break;
                case 2:
                    playerProfile.tuning.driftGrip = Mathf.Clamp(playerProfile.tuning.driftGrip + delta * smallStep, 0.45f, 1.85f);
                    break;
                case 3:
                    playerProfile.tuning.finalDrive = Mathf.Clamp(playerProfile.tuning.finalDrive + delta * smallStep, 0.65f, 1.45f);
                    break;
                case 4:
                    playerProfile.tuning.boostValve = Mathf.Clamp(playerProfile.tuning.boostValve + delta * smallStep, 0.45f, 1.85f);
                    break;
                case 5:
                    playerProfile.tuning.suspension = Mathf.Clamp(playerProfile.tuning.suspension + delta * smallStep, 0.55f, 1.65f);
                    break;
                case 6:
                    playerProfile.tuning.tireGrip = Mathf.Clamp(playerProfile.tuning.tireGrip + delta * smallStep, 0.55f, 1.85f);
                    break;
                case 7:
                    playerProfile.tuning.clutchBite = Mathf.Clamp(playerProfile.tuning.clutchBite + delta * smallStep, 0.55f, 1.8f);
                    break;
                case 8:
                    playerProfile.tuning.automaticTransmission = delta > 0;
                    break;
                case 9:
                    playerProfile.tuning.rivalCount = Mathf.Clamp(playerProfile.tuning.rivalCount + delta, 0, MaxRivalCount);
                    break;
                case 10:
                    playerProfile.tuning.outlineThickness = Mathf.Clamp(playerProfile.tuning.outlineThickness + delta * smallStep, 0.65f, 1.6f);
                    break;
                case 11:
                    playerProfile.tuning.cameraShake = Mathf.Clamp01(playerProfile.tuning.cameraShake + delta * smallStep);
                    break;
                case 12:
                    if (selectedVehicle != null && selectedVehicle.isBike)
                    {
                        playerProfile.tuning.leanResponse = Mathf.Clamp(playerProfile.tuning.leanResponse + delta * smallStep, 0.45f, 1.8f);
                    }

                    break;
                case 13:
                    if (selectedVehicle != null && selectedVehicle.isBike)
                    {
                        playerProfile.tuning.rearBrakeSlide = Mathf.Clamp(playerProfile.tuning.rearBrakeSlide + delta * smallStep, 0.45f, 1.85f);
                    }

                    break;
            }

            VectorSSSaveSystem.Save(playerProfile);
        }

        private void ActivateGarageFocus()
        {
            if (menuFocusIndex < GarageItemFocusStart())
            {
                activeGarageTab = (GarageTab)Mathf.Clamp(menuFocusIndex, 0, 3);
                garageScroll.y = 0f;
                return;
            }

            int buttonStart = GarageButtonFocusStart();
            int itemIndex = menuFocusIndex - GarageItemFocusStart();
            if (menuFocusIndex < buttonStart)
            {
                ActivateGarageItem(itemIndex);
                return;
            }

            if (menuFocusIndex == buttonStart)
            {
                VectorSSSaveSystem.Save(playerProfile);
                garageMessage = "Garage locked.";
            }
            else if (menuFocusIndex == buttonStart + 1)
            {
                VectorSSSaveSystem.Save(playerProfile);
                screen = VectorSSScreen.MapSelect;
                menuFocusIndex = 0;
            }
            else if (menuFocusIndex == buttonStart + 2)
            {
                VectorSSSaveSystem.Save(playerProfile);
                if (pitblockRoot != null)
                {
                    EnterPitblock();
                }
                else
                {
                    screen = VectorSSScreen.MainMenu;
                }
                menuFocusIndex = 0;
            }
        }

        private string GarageFocusLabel(int index)
        {
            if (index < GarageItemFocusStart())
            {
                return GarageTabName((GarageTab)Mathf.Clamp(index, 0, 3));
            }

            int buttonStart = GarageButtonFocusStart();
            if (index < buttonStart)
            {
                string title;
                string meta;
                string value;
                GarageRowText(index - GarageItemFocusStart(), out title, out meta, out value);
                return title;
            }

            string[] buttons = { "Lock Garage", "Roll Out", "Back" };
            return buttons[Mathf.Clamp(index - buttonStart, 0, buttons.Length - 1)];
        }

        private static string GarageTabName(GarageTab tab)
        {
            switch (tab)
            {
                case GarageTab.Build:
                    return "Build Upgrades";
                case GarageTab.Modules:
                    return "Cockpit Modules";
                case GarageTab.Tuning:
                    return "Tuning Bench";
                case GarageTab.Hud:
                    return "HUD Layout";
                default:
                    return "Garage";
            }
        }

        private string GarageTabTitle()
        {
            return GarageTabName(activeGarageTab).ToUpperInvariant();
        }

        private string GarageTabHint()
        {
            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    return "Build upgrades are permanent purchases. Highlight an upgrade and press confirm to buy it.";
                case GarageTab.Modules:
                    return "Modules can be bought, installed, and uninstalled. The selected row shows what confirm will do.";
                case GarageTab.Tuning:
                    return "Tuning rows change with left and right. Transmission and rivals can also be toggled with confirm.";
                case GarageTab.Hud:
                    return "HUD rows resize with left and right. Confirm toggles visibility for installed HUD modules.";
                default:
                    return string.Empty;
            }
        }

        private string GarageEmptyText()
        {
            return activeGarageTab == GarageTab.Hud ? "No installed HUD modules yet. Buy and install cockpit modules first." : "Nothing available here.";
        }

        private void ActivateGarageItem(int itemIndex)
        {
            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    TryPurchaseUpgrade(VectorSSCatalog.Upgrades[itemIndex]);
                    break;
                case GarageTab.Modules:
                    ActivateGarageModule(VectorSSCatalog.Modules[itemIndex]);
                    break;
                case GarageTab.Tuning:
                    if (itemIndex == 8)
                    {
                        playerProfile.tuning.automaticTransmission = !playerProfile.tuning.automaticTransmission;
                        garageMessage = playerProfile.tuning.automaticTransmission ? "Automatic transmission enabled." : "Manual transmission enabled.";
                        VectorSSSaveSystem.Save(playerProfile);
                    }
                    else if (itemIndex == 9)
                    {
                        CycleRivalCount(1);
                    }

                    break;
                case GarageTab.Hud:
                    VectorSSModuleDefinition module = GarageHudModuleAt(itemIndex);
                    if (module != null)
                    {
                        VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, module, true);
                        layout.visible = !layout.visible;
                        garageMessage = module.displayName + (layout.visible ? " shown on HUD." : " hidden from HUD.");
                        VectorSSSaveSystem.Save(playerProfile);
                    }

                    break;
            }
        }

        private void AdjustGarageHudFocus(int itemIndex, int delta)
        {
            VectorSSModuleDefinition module = GarageHudModuleAt(itemIndex);
            if (module == null)
            {
                return;
            }

            VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, module, true);
            float step = delta * 16f;
            layout.size = new Vector2(Mathf.Clamp(layout.size.x + step, 96f, 720f), Mathf.Clamp(layout.size.y + step * 0.45f, 38f, 360f));
            garageMessage = module.displayName + " HUD size " + Mathf.RoundToInt(layout.size.x) + "x" + Mathf.RoundToInt(layout.size.y) + ".";
            VectorSSSaveSystem.Save(playerProfile);
        }

        private int GarageHudModuleCount()
        {
            int count = 0;
            HashSet<string> installed = playerProfile.InstalledModulesFor(selectedVehicle.id, false);
            if (installed == null)
            {
                return 0;
            }

            foreach (string moduleId in installed)
            {
                VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                if (module != null && module.widget != VectorSSModuleWidget.None)
                {
                    count++;
                }
            }

            return count;
        }

        private VectorSSModuleDefinition GarageHudModuleAt(int itemIndex)
        {
            int count = 0;
            HashSet<string> installed = playerProfile.InstalledModulesFor(selectedVehicle.id, false);
            if (installed == null)
            {
                return null;
            }

            foreach (string moduleId in installed)
            {
                VectorSSModuleDefinition module = VectorSSCatalog.GetModule(moduleId);
                if (module == null || module.widget == VectorSSModuleWidget.None)
                {
                    continue;
                }

                if (count == itemIndex)
                {
                    return module;
                }

                count++;
            }

            return null;
        }

        private string GaragePrimaryActionText()
        {
            if (menuFocusIndex < GarageItemFocusStart())
            {
                return "CONFIRM: OPEN TAB    LEFT/RIGHT: CHANGE TAB";
            }

            int itemIndex = menuFocusIndex - GarageItemFocusStart();
            if (menuFocusIndex >= GarageButtonFocusStart())
            {
                return "CONFIRM: " + GarageFocusLabel(menuFocusIndex).ToUpperInvariant();
            }

            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    return playerProfile.HasUpgrade(VectorSSCatalog.Upgrades[itemIndex].id) ? "ALREADY PURCHASED" : "CONFIRM: PURCHASE";
                case GarageTab.Modules:
                    VectorSSModuleDefinition module = VectorSSCatalog.Modules[itemIndex];
                    if (!module.Supports(selectedVehicle))
                    {
                        return "UNSUPPORTED BY THIS VEHICLE";
                    }

                    if (!playerProfile.HasModule(module.id))
                    {
                        return "CONFIRM: BUY";
                    }

                    return playerProfile.IsModuleInstalled(selectedVehicle.id, module.id) ? "CONFIRM: UNINSTALL" : "CONFIRM: INSTALL";
                case GarageTab.Tuning:
                    return itemIndex == 8 || itemIndex == 9 ? "LEFT/RIGHT: CHANGE    CONFIRM: TOGGLE" : "LEFT/RIGHT: ADJUST";
                case GarageTab.Hud:
                    return "LEFT/RIGHT: RESIZE    CONFIRM: SHOW/HIDE";
                default:
                    return string.Empty;
            }
        }

        private string GarageFocusDescription()
        {
            if (menuFocusIndex < GarageItemFocusStart())
            {
                return GarageTabHint();
            }

            int itemIndex = menuFocusIndex - GarageItemFocusStart();
            if (menuFocusIndex >= GarageButtonFocusStart())
            {
                return "Garage changes save automatically, but Lock Garage gives you an explicit save point before leaving.";
            }

            switch (activeGarageTab)
            {
                case GarageTab.Build:
                    VectorSSUpgradeDefinition upgrade = VectorSSCatalog.Upgrades[itemIndex];
                    return upgrade.description + "\nCost: " + upgrade.cost;
                case GarageTab.Modules:
                    VectorSSModuleDefinition module = VectorSSCatalog.Modules[itemIndex];
                    return module.description + "\nCost: " + module.cost + (string.IsNullOrEmpty(module.controlHint) ? string.Empty : "\nControl: " + module.controlHint);
                case GarageTab.Tuning:
                    string title;
                    string meta;
                    string value;
                    GarageTuningRowText(itemIndex, out title, out meta, out value);
                    return meta + "\nCurrent value: " + value;
                case GarageTab.Hud:
                    VectorSSModuleDefinition hudModule = GarageHudModuleAt(itemIndex);
                    if (hudModule == null)
                    {
                        return string.Empty;
                    }

                    VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, hudModule, true);
                    return hudModule.description + "\nPosition " + Mathf.RoundToInt(layout.position.x) + ", " + Mathf.RoundToInt(layout.position.y) + "    Scale " + layout.scale.ToString("0.00");
                default:
                    return string.Empty;
            }
        }

        private void GarageTuningRowText(int itemIndex, out string title, out string meta, out string value)
        {
            title = "Tuning";
            meta = "Left/right adjusts this setting.";
            value = string.Empty;
            switch (itemIndex)
            {
                case 0:
                    title = "Steering Sensitivity";
                    meta = "Steering response, smoothing, and yaw assist.";
                    value = playerProfile.tuning.steering.ToString("0.00");
                    break;
                case 1:
                    title = "Brake Bias / Power";
                    meta = "How hard the service brakes bite.";
                    value = playerProfile.tuning.brakeBias.ToString("0.00");
                    break;
                case 2:
                    title = "Drift Grip";
                    meta = "Slide looseness, sustain, and recovery.";
                    value = playerProfile.tuning.driftGrip.ToString("0.00");
                    break;
                case 3:
                    title = "Final Drive";
                    meta = "Acceleration versus top-end gearing.";
                    value = playerProfile.tuning.finalDrive.ToString("0.00");
                    break;
                case 4:
                    title = "Boost Valve";
                    meta = "Boost strength and burn rate.";
                    value = playerProfile.tuning.boostValve.ToString("0.00");
                    break;
                case 5:
                    title = "Suspension";
                    meta = "Stability, recovery, and downforce feel.";
                    value = playerProfile.tuning.suspension.ToString("0.00");
                    break;
                case 6:
                    title = "Tire Grip";
                    meta = "Normal driving traction.";
                    value = playerProfile.tuning.tireGrip.ToString("0.00");
                    break;
                case 7:
                    title = "Clutch Bite";
                    meta = "Manual launch and clutch-kick bite.";
                    value = playerProfile.tuning.clutchBite.ToString("0.00");
                    break;
                case 8:
                    title = "Transmission";
                    meta = "Automatic supports hold-brake reverse.";
                    value = playerProfile.tuning.automaticTransmission ? "AUTOMATIC" : "MANUAL";
                    break;
                case 9:
                    title = "Rivals";
                    meta = "Default AI field size.";
                    value = playerProfile.tuning.rivalCount.ToString("0");
                    break;
                case 10:
                    title = "Outline Thickness";
                    meta = "Visual ink weight.";
                    value = playerProfile.tuning.outlineThickness.ToString("0.00");
                    break;
                case 11:
                    title = "Camera Shake";
                    meta = "Impact and speed shake amount.";
                    value = playerProfile.tuning.cameraShake.ToString("0.00");
                    break;
                case 12:
                    title = "Lean Response";
                    meta = "Razor lean visual response.";
                    value = playerProfile.tuning.leanResponse.ToString("0.00");
                    break;
                case 13:
                    title = "Rear Brake Slide";
                    meta = "Razor rear-slide behavior.";
                    value = playerProfile.tuning.rearBrakeSlide.ToString("0.00");
                    break;
            }
        }

        private static int IndexOfMap(VectorSSMapDefinition map)
        {
            for (int i = 0; i < VectorSSCatalog.Maps.Length; i++)
            {
                if (VectorSSCatalog.Maps[i] == map || (map != null && VectorSSCatalog.Maps[i].id == map.id))
                {
                    return i;
                }
            }

            return 0;
        }

        private static int IndexOfVehicle(VectorSSVehicleDefinition vehicle)
        {
            for (int i = 0; i < VectorSSCatalog.Vehicles.Length; i++)
            {
                if (VectorSSCatalog.Vehicles[i] == vehicle || (vehicle != null && VectorSSCatalog.Vehicles[i].id == vehicle.id))
                {
                    return i;
                }
            }

            return 0;
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
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

        private void DrawTransmissionToggle()
        {
            bool automatic = playerProfile.tuning.automaticTransmission;
            string label = automatic ? "Transmission  Automatic" : "Transmission  Manual";
            bool newValue = GUILayout.Toggle(automatic, label, bodyStyle);
            if (newValue != automatic)
            {
                playerProfile.tuning.automaticTransmission = newValue;
                VectorSSSaveSystem.Save(playerProfile);
            }
        }

        private void DrawRivalCountSelector()
        {
            GUILayout.Label("Rivals  " + playerProfile.tuning.rivalCount, bodyStyle);
            float next = GUILayout.HorizontalSlider(playerProfile.tuning.rivalCount, 0f, MaxRivalCount, GUILayout.Width(430f));
            int nextCount = Mathf.Clamp(Mathf.RoundToInt(next), 0, MaxRivalCount);
            if (nextCount != playerProfile.tuning.rivalCount)
            {
                playerProfile.tuning.rivalCount = nextCount;
                VectorSSSaveSystem.Save(playerProfile);
            }
        }

        private void CycleRivalCount(int delta)
        {
            playerProfile.tuning.rivalCount = WrapIndex(playerProfile.tuning.rivalCount + delta, MaxRivalCount + 1);
            VectorSSSaveSystem.Save(playerProfile);
        }

        private void AdjustRaceLaps(int delta)
        {
            playerProfile.tuning.endlessRace = false;
            playerProfile.tuning.raceLapCount = Mathf.Clamp(playerProfile.tuning.raceLapCount + delta, 1, 99);
            VectorSSSaveSystem.Save(playerProfile);
        }

        private void ToggleEndlessRace()
        {
            playerProfile.tuning.endlessRace = !playerProfile.tuning.endlessRace;
            VectorSSSaveSystem.Save(playerProfile);
        }

        private void DrawUpgrade(VectorSSUpgradeDefinition upgrade, int focusIndex)
        {
            if (upgrade == null)
            {
                return;
            }

            bool bought = playerProfile.HasUpgrade(upgrade.id);
            bool classMatch = upgrade.preferredClass == null || upgrade.preferredClass.Value == selectedVehicle.vehicleClass;
            GUILayout.BeginVertical(GUI.skin.box);
            if (menuFocusIndex == focusIndex)
            {
                GUILayout.Label(">> CONTROLLER FOCUS", smallStyle);
            }

            GUILayout.Label(upgrade.displayName + (bought ? "  PURCHASED" : string.Empty), headerStyle);
            GUILayout.Label(upgrade.description, bodyStyle);
            GUILayout.Label("Cost: " + upgrade.cost + (classMatch ? string.Empty : "   off-class but usable"), bodyStyle);
            GUI.enabled = !bought && playerProfile.resources.CanAfford(upgrade.cost);
            if (GUILayout.Button(bought ? "Installed" : "Purchase", buttonStyle, GUILayout.Height(30f)))
            {
                TryPurchaseUpgrade(upgrade);
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private void DrawModule(VectorSSModuleDefinition module, int focusIndex)
        {
            if (module == null)
            {
                return;
            }

            bool supported = module.Supports(selectedVehicle);
            bool purchased = playerProfile.HasModule(module.id);
            bool installed = playerProfile.IsModuleInstalled(selectedVehicle.id, module.id);
            GUILayout.BeginVertical(GUI.skin.box);
            if (menuFocusIndex == focusIndex)
            {
                GUILayout.Label(">> CONTROLLER FOCUS: " + (!purchased ? "BUY" : installed ? "UNINSTALL" : "INSTALL"), smallStyle);
            }

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
                TryBuyModule(module);
            }

            GUI.enabled = supported && purchased && !installed;
            if (GUILayout.Button("Install", buttonStyle, GUILayout.Height(30f), GUILayout.Width(112f)))
            {
                TryInstallGarageModule(module);
            }

            GUI.enabled = installed;
            if (GUILayout.Button("Uninstall", buttonStyle, GUILayout.Height(30f), GUILayout.Width(112f)))
            {
                UninstallGarageModule(module);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void TryPurchaseUpgrade(VectorSSUpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            if (playerProfile.HasUpgrade(upgrade.id))
            {
                garageMessage = upgrade.displayName + " is already purchased.";
                return;
            }

            if (!playerProfile.resources.CanAfford(upgrade.cost))
            {
                garageMessage = "Not enough resources for " + upgrade.displayName + ".";
                return;
            }

            if (playerProfile.TryPurchase(upgrade))
            {
                garageMessage = "Purchased " + upgrade.displayName + ".";
                VectorSSSaveSystem.Save(playerProfile);
            }
        }

        private void ActivateGarageModule(VectorSSModuleDefinition module)
        {
            if (module == null)
            {
                return;
            }

            if (!playerProfile.HasModule(module.id))
            {
                TryBuyModule(module);
                return;
            }

            if (playerProfile.IsModuleInstalled(selectedVehicle.id, module.id))
            {
                UninstallGarageModule(module);
                return;
            }

            TryInstallGarageModule(module);
        }

        private void TryBuyModule(VectorSSModuleDefinition module)
        {
            if (module == null)
            {
                return;
            }

            if (!module.Supports(selectedVehicle))
            {
                garageMessage = module.displayName + " is not supported by " + selectedVehicle.displayName + ".";
                return;
            }

            if (playerProfile.HasModule(module.id))
            {
                garageMessage = module.displayName + " is already purchased.";
                return;
            }

            if (!playerProfile.resources.CanAfford(module.cost))
            {
                garageMessage = "Not enough resources for " + module.displayName + ".";
                return;
            }

            if (playerProfile.TryPurchaseModule(module))
            {
                garageMessage = "Purchased " + module.displayName + ".";
                VectorSSSaveSystem.Save(playerProfile);
            }
        }

        private void TryInstallGarageModule(VectorSSModuleDefinition module)
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

        private void UninstallGarageModule(VectorSSModuleDefinition module)
        {
            if (module == null)
            {
                return;
            }

            playerProfile.UninstallModule(selectedVehicle.id, module.id);
            garageMessage = "Uninstalled " + module.displayName + ".";
            VectorSSSaveSystem.Save(playerProfile);
        }

        private void DrawModuleLayoutEditor()
        {
            GUILayout.Space(12f);
            GUILayout.Label("DASHGRID HUD LAYOUT", headerStyle);
            GUILayout.Label("Arrange installed cockpit hardware. X/Y use the 1920x1080 dash canvas; LOCK GARAGE persists positions.", bodyStyle);

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
                DrawLayoutSlider("Scale", ref layoutScale, 0.65f, 2.7f);
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

                if (GUILayout.Button("REWIRE " + module.displayName, buttonStyle, GUILayout.Height(28f)))
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
            ResumeRaceTime();
            ClearPitblock();
            ClearSession();
            selectedMap = VectorSSCatalog.GetMap(playerProfile.selectedMap);
            selectedVehicle = VectorSSCatalog.GetVehicle(playerProfile.selectedVehicle);
            ApplyMapPalette(selectedMap);
            ApplyVehiclePalette(selectedVehicle);

            sessionRoot = new GameObject("Vector SS Runtime Session").transform;
            Transform trackRoot = new GameObject("Vector SS Runtime Test Track").transform;
            trackRoot.SetParent(sessionRoot, false);
            activeRoute = BuildTrack(trackRoot, selectedMap);
            GridSlot playerSlot = GridSlotForIndex(0);
            Transform resetPoint = CreateResetPoint(playerSlot.position, playerSlot.rotation);
            resetPoint.SetParent(sessionRoot, true);

            activePlayer = CreatePlayerMachine(resetPoint, selectedVehicle);
            activeModuleController = activePlayer.moduleController;
            activePlayer.root.transform.SetParent(sessionRoot, true);
            SpawnRivalField();

            Camera camera = ConfigureCamera(activePlayer);
            ConfigureHud(activePlayer);
            SetRaceHudVisible(true);
            activePlayer.visuals.Configure(activePlayer.vehicle, activePlayer.flowState, activePlayer.effects, activePlayer.boostTrail);
            activePlayer.visuals.SetBoostTrailStyle(VectrStyleTokens.WithAlpha(VectrStyleTokens.BoostTrail(selectedVehicle.id), 0.86f), VectrStyleTokens.WithAlpha(selectedVehicle.isBike ? VectrStyleTokens.AcidYellowGreen : VectrStyleTokens.HotMagenta, 0.96f), selectedVehicle.isBike);
            camera.GetComponent<GTXCameraRig>().Configure(activePlayer.root.transform, activePlayer.body, activePlayer.flowState);
            activePlayer.sideSlam.FeedbackRaised += delegate { combatScore += selectedVehicle.isBike ? 1 : 4; };
            activePlayer.boostRam.FeedbackRaised += delegate { combatScore += selectedVehicle.isBike ? 2 : 8; };

            hasActivePlayer = true;
            combatScore = 0;
            bestRouteDistance = 0f;
            currentRouteDistance = activeRoute.DistanceAlongRoute(playerSlot.routePosition);
            previousRouteDistance = currentRouteDistance;
            currentLap = 0;
            targetLaps = playerProfile.tuning.endlessRace ? 0 : Mathf.Clamp(playerProfile.tuning.raceLapCount, 1, 99);
            nextCheckpointIndex = 0;
            hasRouteProgress = true;
            raceCountdownStartTime = Time.time;
            raceCountdownGoTime = raceCountdownStartTime + RaceCountdownSeconds;
            raceStartTime = raceCountdownGoTime;
            raceLaunched = false;
            SetRaceControlEnabled(false);
            nextRazorNearMissTime = 0f;
            racePaused = false;
            screen = VectorSSScreen.Racing;
        }

        private void UpdateRaceCountdownGate()
        {
            if (raceLaunched)
            {
                return;
            }

            if (Time.time >= raceCountdownGoTime)
            {
                raceLaunched = true;
                raceStartTime = Time.time;
                SetRaceControlEnabled(true);
                BeginRaceStartGhosting();
                return;
            }

            SetRaceControlEnabled(false);
            HoldPlayerOnGrid();
        }

        private void SetRaceControlEnabled(bool enabled)
        {
            if (activePlayer.vehicle != null)
            {
                activePlayer.vehicle.InputLocked = !enabled;
            }

            for (int i = 0; i < activeRivals.Count; i++)
            {
                if (activeRivals[i] != null)
                {
                    activeRivals[i].DrivingEnabled = enabled;
                }
            }
        }

        private void HoldPlayerOnGrid()
        {
            if (activePlayer.body == null)
            {
                return;
            }

            activePlayer.body.velocity = Vector3.zero;
            activePlayer.body.angularVelocity = Vector3.zero;
        }

        private void BeginRaceStartGhosting()
        {
            EndRaceStartGhosting();
            if (activePlayer.root == null)
            {
                return;
            }

            Collider[] playerColliders = activePlayer.root.GetComponentsInChildren<Collider>();
            for (int rivalIndex = 0; rivalIndex < activeRivals.Count; rivalIndex++)
            {
                SimpleRouteRivalAI rival = activeRivals[rivalIndex];
                if (rival == null)
                {
                    continue;
                }

                Collider[] rivalColliders = rival.GetComponentsInChildren<Collider>();
                for (int p = 0; p < playerColliders.Length; p++)
                {
                    Collider playerCollider = playerColliders[p];
                    if (playerCollider == null)
                    {
                        continue;
                    }

                    for (int r = 0; r < rivalColliders.Length; r++)
                    {
                        Collider rivalCollider = rivalColliders[r];
                        if (rivalCollider == null)
                        {
                            continue;
                        }

                        Physics.IgnoreCollision(playerCollider, rivalCollider, true);
                        startGhostPlayerColliders.Add(playerCollider);
                        startGhostRivalColliders.Add(rivalCollider);
                    }
                }
            }

            raceStartGhostActive = startGhostPlayerColliders.Count > 0;
            raceStartGhostUntil = Time.time + RaceStartGhostSeconds;
        }

        private void UpdateRaceStartGhosting()
        {
            if (raceStartGhostActive && Time.time >= raceStartGhostUntil)
            {
                EndRaceStartGhosting();
            }
        }

        private void EndRaceStartGhosting()
        {
            for (int i = 0; i < startGhostPlayerColliders.Count && i < startGhostRivalColliders.Count; i++)
            {
                Collider playerCollider = startGhostPlayerColliders[i];
                Collider rivalCollider = startGhostRivalColliders[i];
                if (playerCollider != null && rivalCollider != null)
                {
                    Physics.IgnoreCollision(playerCollider, rivalCollider, false);
                }
            }

            startGhostPlayerColliders.Clear();
            startGhostRivalColliders.Clear();
            raceStartGhostActive = false;
            raceStartGhostUntil = 0f;
        }

        private void SpawnRivalField()
        {
            activeRivals.Clear();
            activeRivalAi = null;
            if (activeRoute.samples == null || activeRoute.samples.Length < 2 || sessionRoot == null)
            {
                return;
            }

            float baseCruiseSpeed = selectedMap.id == VectorSSMapId.ScraplineYard ? 31f : selectedMap.id == VectorSSMapId.RubberRidge ? 28f : 33f;
            float baseAggression = selectedMap.id == VectorSSMapId.ScraplineYard ? 0.74f : selectedMap.id == VectorSSMapId.RubberRidge ? 0.64f : 0.58f;
            int rivalCount = Mathf.Clamp(playerProfile != null ? playerProfile.tuning.rivalCount : 3, 0, MaxRivalCount);
            for (int i = 0; i < rivalCount; i++)
            {
                GridSlot slot = GridSlotForIndex(i + 1);
                GameObject rival = CreateRacingRival(i + 1, selectedVehicle, slot.position, slot.rotation);
                rival.transform.SetParent(sessionRoot, true);
                SimpleRouteRivalAI rivalAi = rival.GetComponent<SimpleRouteRivalAI>();
                if (rivalAi == null)
                {
                    continue;
                }

                float speedJitter = 1f + (i - 1) * 0.035f;
                float aggression = Mathf.Clamp01(baseAggression + i * 0.08f);
                rivalAi.Configure(activeRoute.samples, activeRoute.TotalLength, slot.distance, baseCruiseSpeed * speedJitter, aggression, slot.laneOffset);
                activeRivals.Add(rivalAi);
                if (activeRivalAi == null)
                {
                    activeRivalAi = rivalAi;
                }
            }
        }

        private GridSlot GridSlotForIndex(int slotIndex)
        {
            int row = Mathf.Max(0, slotIndex / 2);
            bool rightColumn = slotIndex % 2 == 0;
            float distance = Mathf.Clamp(RaceGridDistance - row * RaceGridRowSpacing, 1.5f, RaceStartLineDistance - 2f);
            TrackPose pose = activeRoute.PoseAtDistance(distance);
            float laneOffset = rightColumn ? RaceGridLaneOffset : -RaceGridLaneOffset;
            float safeLaneOffset = Mathf.Clamp(laneOffset, -Mathf.Max(2.8f, pose.width * 0.36f), Mathf.Max(2.8f, pose.width * 0.36f));
            Quaternion gridRotation = GridRotationAtDistance(distance);
            Vector3 position = pose.position + pose.right * safeLaneOffset + Vector3.up * RaceGridSpawnLift;
            return new GridSlot(distance, pose.position, position, gridRotation, safeLaneOffset);
        }

        private Quaternion GridRotationAtDistance(float distance)
        {
            if (activeRoute.TotalLength <= 0.01f)
            {
                return Quaternion.identity;
            }

            TrackPose pose = activeRoute.PoseAtDistance(distance);
            TrackPose ahead = activeRoute.PoseAtDistance(distance + 8f);
            Vector3 forward = ahead.position - pose.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = pose.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void CompleteRace()
        {
            if (!hasActivePlayer)
            {
                return;
            }

            float flow01 = activePlayer.flowState != null ? activePlayer.flowState.Normalized : 0f;
            int placement = CalculatePlayerPlacement();
            lastResult = VectorSSProgressionUtility.BuildRaceResult(selectedMap, selectedVehicle, Time.time - raceStartTime, flow01, combatScore, placement, activeRivals.Count + 1);
            playerProfile.resources.Add(lastResult.Total);
            playerProfile.scrapCubes = Mathf.Max(0, playerProfile.scrapCubes + lastResult.scrapCubes);
            VectorSSSaveSystem.Save(playerProfile);
            ResumeRaceTime();
            ClearSession();
            SetRaceHudVisible(false);
            screen = VectorSSScreen.Results;
        }

        private void ClearSession()
        {
            EndRaceStartGhosting();
            racePaused = false;
            hasActivePlayer = false;
            activeRivalAi = null;
            activeRivals.Clear();
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

        private void PauseRace()
        {
            if (screen != VectorSSScreen.Racing || !hasActivePlayer)
            {
                return;
            }

            timeScaleBeforePause = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            racePaused = true;
            screen = VectorSSScreen.Paused;
            menuFocusIndex = 0;
        }

        private void ResumeRace()
        {
            if (!racePaused)
            {
                return;
            }

            ResumeRaceTime();
            screen = VectorSSScreen.Racing;
            menuFocusIndex = 0;
        }

        private void ResumeRaceTime()
        {
            if (racePaused || Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = Mathf.Approximately(timeScaleBeforePause, 0f) ? 1f : timeScaleBeforePause;
            }

            racePaused = false;
        }

        private void RestartRace()
        {
            restartRaceQueued = true;
        }

        private void LeaveRace()
        {
            ResumeRaceTime();
            ClearSession();
            SetRaceHudVisible(false);
            screen = VectorSSScreen.MainMenu;
            menuFocusIndex = 0;
        }

        private void HandlePauseMenuInput()
        {
            if (PausePressed() || GTXInput.ButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeRace();
                return;
            }

            if (ControllerMenuDown())
            {
                ControllerVertical(1);
            }
            else if (ControllerMenuUp())
            {
                ControllerVertical(-1);
            }

            if (GTXInput.ButtonDown(1) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ActivatePauseFocus();
            }
        }

        private void ActivatePauseFocus()
        {
            switch (menuFocusIndex)
            {
                case 1:
                    RestartRace();
                    break;
                case 2:
                    LeaveRace();
                    break;
                default:
                    ResumeRace();
                    break;
            }
        }

        private bool PausePressed()
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || GTXInput.ButtonDown(9);
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

            if (activeRoute.TotalLength <= 0.01f)
            {
                return;
            }

            float distance = activeRoute.DistanceAlongRoute(activePlayer.root.transform.position);
            if (!hasRouteProgress)
            {
                currentRouteDistance = distance;
                previousRouteDistance = distance;
                hasRouteProgress = true;
                return;
            }

            float forwardDelta = ForwardRouteDelta(previousRouteDistance, distance, activeRoute.TotalLength);
            bool crossedStartLine = CrossedRouteDistance(previousRouteDistance, distance, RaceStartLineDistance, activeRoute.TotalLength, forwardDelta);
            currentRouteDistance = distance;
            previousRouteDistance = distance;
            if (forwardDelta > 0f)
            {
                bestRouteDistance = Mathf.Min(activeRoute.TotalLength + 60f, bestRouteDistance + forwardDelta);
            }

            UpdateCheckpointProgress();
            if (CanCompleteLap(crossedStartLine))
            {
                currentLap++;
                if (targetLaps > 0 && currentLap >= targetLaps)
                {
                    CompleteRace();
                    return;
                }

                bestRouteDistance = Mathf.Min(DistancePastStartLine(distance, activeRoute.TotalLength), activeRoute.TotalLength * 0.12f);
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

        private bool CanCompleteLap(bool crossedStartLine)
        {
            if (activeRoute.TotalLength <= 0.01f || Time.time - raceStartTime < 10f)
            {
                return false;
            }

            bool allCheckpointsCleared = nextCheckpointIndex >= CheckpointFractions.Length;
            bool hasRunFullLoop = bestRouteDistance >= activeRoute.TotalLength * 0.96f;
            return allCheckpointsCleared && hasRunFullLoop && crossedStartLine;
        }

        private static float ForwardRouteDelta(float previousDistance, float currentDistance, float routeLength)
        {
            if (routeLength <= 0.01f)
            {
                return 0f;
            }

            float delta = currentDistance - previousDistance;
            if (delta < -routeLength * 0.5f)
            {
                delta += routeLength;
            }
            else if (delta > routeLength * 0.5f)
            {
                delta -= routeLength;
            }

            return delta;
        }

        private static bool CrossedRouteDistance(float previousDistance, float currentDistance, float markerDistance, float routeLength, float forwardDelta)
        {
            if (routeLength <= 0.01f || forwardDelta <= 0.01f)
            {
                return false;
            }

            float marker = Mathf.Repeat(markerDistance, routeLength);
            float previous = Mathf.Repeat(previousDistance, routeLength);
            float current = Mathf.Repeat(currentDistance, routeLength);
            if (previous <= current)
            {
                return previous < marker && current >= marker;
            }

            return marker > previous || marker <= current;
        }

        private static float DistancePastStartLine(float distance, float routeLength)
        {
            return routeLength <= 0.01f ? 0f : Mathf.Repeat(distance - RaceStartLineDistance, routeLength);
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

        private string RacePositionText()
        {
            int fieldSize = activeRivals.Count + 1;
            return CalculatePlayerPlacement() + "/" + Mathf.Max(1, fieldSize);
        }

        private int CalculatePlayerPlacement()
        {
            float playerDistance = PlayerRaceDistance();
            int placement = 1;
            for (int i = 0; i < activeRivals.Count; i++)
            {
                SimpleRouteRivalAI rival = activeRivals[i];
                if (rival != null && rival.DistanceTravelled > playerDistance + 1.5f)
                {
                    placement++;
                }
            }

            return Mathf.Clamp(placement, 1, activeRivals.Count + 1);
        }

        private float PlayerRaceDistance()
        {
            if (activeRoute.TotalLength <= 0.01f)
            {
                return 0f;
            }

            return currentLap * activeRoute.TotalLength + currentRouteDistance;
        }

        private SimpleRouteRivalAI LeadRival()
        {
            SimpleRouteRivalAI lead = null;
            float bestDistance = float.MinValue;
            for (int i = 0; i < activeRivals.Count; i++)
            {
                SimpleRouteRivalAI rival = activeRivals[i];
                if (rival != null && rival.DistanceTravelled > bestDistance)
                {
                    bestDistance = rival.DistanceTravelled;
                    lead = rival;
                }
            }

            return lead;
        }

        private void ApplyMapPalette(VectorSSMapDefinition map)
        {
            SetMaterialColor(roadMaterial, map.roadColor);
            SetMaterialColor(desertMaterial, map.groundColor);
            SetMaterialColor(barrierMaterial, map.barrierColor);
            SetMaterialColor(stripeMaterial, VectrStyleTokens.MapAccent(map.id));
            SetMaterialColor(trackMarkerMaterial, VectrStyleTokens.MapWarning(map.id));
            SetMaterialColor(trackMarkerBlueMaterial, VectrStyleTokens.MapAccent(map.id));
        }

        private void ApplyVehiclePalette(VectorSSVehicleDefinition vehicle)
        {
            SetMaterialColor(playerMaterial, vehicle.bodyColor);
            SetMaterialColor(playerAccentMaterial, vehicle.accentColor);
            SetMaterialColor(playerSecondaryMaterial, vehicle.secondaryColor);
            SetMaterialColor(boostTrailMaterial, VectrStyleTokens.WithAlpha(VectrStyleTokens.BoostTrail(vehicle.id), 0.93f));
            Color glassColor = vehicle.id == VectorSSVehicleId.Razor
                ? Color.Lerp(VectrStyleTokens.ElectricCyan, VectrStyleTokens.InkBlack, 0.35f)
                : Color.Lerp(VectrStyleTokens.VehicleSecondary(vehicle.id), VectrStyleTokens.BoneWhite, 0.28f);
            SetMaterialColor(glassMaterial, glassColor);
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
                material.SetColor("_ShadowColor", VectrStyleTokens.ShadowFor(color));
            }
        }

        private void EnsureGuiStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, normal = { textColor = VectrStyleTokens.BoneWhite } };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = VectrStyleTokens.ElectricCyan } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = VectrStyleTokens.BoneWhite } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, normal = { textColor = new Color(0.76f, 0.78f, 0.70f, 1f) } };
            panelStyle = new GUIStyle(GUI.skin.box);
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, VectrStyleTokens.WithAlpha(VectrStyleTokens.AsphaltNavy, 0.95f));
            panelTexture.Apply();
            panelStyle.normal.background = panelTexture;
            panelStyle.normal.textColor = Color.white;
            panelStyle.padding = new RectOffset(18, 18, 18, 18);
            scrollStyle = new GUIStyle(GUI.skin.scrollView);
            scrollStyle.normal.background = panelTexture;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
            cardTexture = new Texture2D(1, 1);
            cardTexture.SetPixel(0, 0, VectrStyleTokens.WithAlpha(VectrStyleTokens.OilGray, 0.94f));
            cardTexture.Apply();
            focusTexture = new Texture2D(1, 1);
            focusTexture.SetPixel(0, 0, VectrStyleTokens.WithAlpha(VectrStyleTokens.SafetyOrange, 0.98f));
            focusTexture.Apply();
            cardStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            cardStyle.normal.background = cardTexture;
            cardStyle.normal.textColor = Color.white;
            focusStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            focusStyle.normal.background = focusTexture;
            focusStyle.hover.background = focusTexture;
            focusStyle.active.background = focusTexture;
            focusStyle.normal.textColor = Color.white;
        }

        private void EnsureMenuPreviews()
        {
            if (previewRoot != null)
            {
                return;
            }

            previewRoot = new GameObject("Vector SS Menu Preview Rig").transform;
            previewRoot.position = new Vector3(6200f, 0f, 6200f);
            mapPreviewTexture = new RenderTexture(720, 420, 16) { name = "Vector SS Stage Preview" };
            vehiclePreviewTexture = new RenderTexture(720, 420, 16) { name = "Vector SS Vehicle Preview" };

            mapPreviewCamera = CreatePreviewCamera("Stage Preview Camera", previewRoot, new Vector3(-140f, 120f, 0f), Quaternion.Euler(90f, 0f, 0f), mapPreviewTexture, 42f);
            vehiclePreviewCamera = CreatePreviewCamera("Vehicle Preview Camera", previewRoot, new Vector3(140f, 3.4f, -7.6f), Quaternion.Euler(18f, 0f, 0f), vehiclePreviewTexture, 3.1f);
            RefreshMenuPreviews(true);
        }

        private Camera CreatePreviewCamera(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, RenderTexture texture, float orthographicSize)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = localPosition;
            cameraObject.transform.localRotation = localRotation;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.028f, 0.035f, 0.052f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 260f;
            camera.targetTexture = texture;
            return camera;
        }

        private void UpdateMenuPreviews()
        {
            if (screen == VectorSSScreen.Racing)
            {
                return;
            }

            EnsureMenuPreviews();
            RefreshMenuPreviews(false);
            float rotation = Time.deltaTime * 28f;
            for (int i = 0; i < mapPreviewSpinRoots.Count; i++)
            {
                if (mapPreviewSpinRoots[i] != null)
                {
                    mapPreviewSpinRoots[i].Rotate(Vector3.up, rotation * 0.35f, Space.World);
                }
            }

            if (vehiclePreviewSpinRoot != null)
            {
                vehiclePreviewSpinRoot.Rotate(Vector3.up, rotation, Space.World);
            }
        }

        private void RefreshMenuPreviews(bool force)
        {
            int mapIndex = IndexOfMap(selectedMap);
            if (force || mapIndex != lastPreviewMapIndex)
            {
                BuildMapPreview(selectedMap);
                lastPreviewMapIndex = mapIndex;
            }

            int vehicleIndex = IndexOfVehicle(selectedVehicle);
            if (force || vehicleIndex != lastPreviewVehicleIndex)
            {
                BuildVehiclePreview(selectedVehicle);
                lastPreviewVehicleIndex = vehicleIndex;
            }
        }

        private void BuildMapPreview(VectorSSMapDefinition map)
        {
            if (previewRoot == null || map == null)
            {
                return;
            }

            Transform old = previewRoot.Find("Stage Preview Root");
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            ApplyMapPalette(map);
            Transform root = new GameObject("Stage Preview Root").transform;
            root.SetParent(previewRoot, false);
            root.localPosition = new Vector3(-140f, 0f, 0f) - MapBoardCenter(map.id) * 0.18f;
            root.localScale = Vector3.one * 0.18f;
            mapPreviewSpinRoots.Clear();
            mapPreviewSpinRoots.Add(root);
            BuildTrack(root, map);
        }

        private void BuildVehiclePreview(VectorSSVehicleDefinition vehicle)
        {
            if (previewRoot == null || vehicle == null)
            {
                return;
            }

            Transform old = previewRoot.Find("Vehicle Preview Root");
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            ApplyVehiclePalette(vehicle);
            Transform root = new GameObject("Vehicle Preview Root").transform;
            root.SetParent(previewRoot, false);
            root.localPosition = new Vector3(140f, -0.42f, 0f);
            root.localRotation = Quaternion.Euler(0f, 35f, 0f);
            vehiclePreviewSpinRoot = root;
            if (vehicle.isBike)
            {
                CreateRazorBikeVisuals(root, vehicle);
            }
            else
            {
                CreateCarVisuals(root, vehicle);
                CreateWheelVisuals(root, vehicle);
            }
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
            if (map.id == VectorSSMapId.SpecialStage)
            {
                CreateSpecialStageCityBase(root);
            }
            else
            {
                CreateChamferedBox("Vector SS Ground Board", root, MapBoardCenter(map.id), Quaternion.identity, MapBoardScale(map.id), desertMaterial, false, 0.035f);
            }

            LowPolyMeshFactory.CreateTrackRibbon("Vector SS " + map.displayName + " Surface", root, route.samples, route.widths, roadMaterial, true);
            CreateRouteStripe(root, route);
            if (map.id != VectorSSMapId.SpecialStage)
            {
                CreateRouteBarriers(root, route);
            }

            CreateStartGate(root, route.PoseAtDistance(RaceStartLineDistance));
            CreateCheckpointGates(root, route);
            CreateTrackMarkers(root, route);
            CreatePitDiorama(root, route);
            CreateMapDressing(root, map, route);
            return route;
        }

        private void CreateSpecialStageCityBase(Transform root)
        {
            const float radius = 300f;
            CreateSpecialStageCircularFloor(root, radius, new Vector3(0f, 0.045f, 12f));

            CreatePrimitive(
                "Special Stage Outer Edge Ink Ring",
                PrimitiveType.Cylinder,
                root,
                new Vector3(0f, 0.085f, 12f),
                Quaternion.identity,
                new Vector3(radius * 2.01f, 0.012f, radius * 2.01f),
                inkMaterial,
                false);

            CreatePrimitive(
                "Special Stage Inner City Floor Tint",
                PrimitiveType.Cylinder,
                root,
                new Vector3(0f, 0.075f, 12f),
                Quaternion.identity,
                new Vector3(radius * 1.82f, 0.01f, radius * 1.82f),
                pitFloorMaterial,
                false);
        }

        private void CreateSpecialStageCircularFloor(Transform root, float radius, Vector3 center)
        {
            const int segments = 128;
            GameObject floor = new GameObject("Special Stage Continuous Circular City Floor");
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = center;
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = Vector3.one;

            Vector3[] vertices = new Vector3[segments + 1];
            Vector3[] normals = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = new Vector2(0.5f + x / (radius * 2f), 0.5f + z / (radius * 2f));

                int next = i == segments - 1 ? 1 : i + 2;
                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = next;
                triangles[tri + 2] = i + 1;
            }

            Mesh mesh = new Mesh { name = "Special Stage Circular Floor Mesh" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            MeshFilter filter = floor.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = floor.AddComponent<MeshRenderer>();
            renderer.material = desertMaterial;
            MeshCollider collider = floor.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private RuntimeTrackRoute CreateRuntimeLoopRoute(VectorSSMapId mapId)
        {
            Vector3[] nodes;
            float[] nodeWidths;
            switch (mapId)
            {
                case VectorSSMapId.SpecialStage:
                    nodes = new[]
                    {
                        new Vector3(-168f, 0.08f, -154f),
                        new Vector3(-44f, 0.08f, -192f),
                        new Vector3(118f, 0.08f, -178f),
                        new Vector3(218f, 0.08f, -92f),
                        new Vector3(238f, 0.08f, 42f),
                        new Vector3(178f, 0.08f, 178f),
                        new Vector3(42f, 0.08f, 232f),
                        new Vector3(-112f, 0.08f, 210f),
                        new Vector3(-236f, 0.08f, 118f),
                        new Vector3(-252f, 0.08f, -42f)
                    };
                    nodeWidths = new[] { 78f, 86f, 92f, 84f, 96f, 88f, 98f, 92f, 84f, 80f };
                    break;
                case VectorSSMapId.ScraplineYard:
                    nodes = new[]
                    {
                        new Vector3(-12f, 0.08f, -78f),
                        new Vector3(44f, 0.08f, -74f),
                        new Vector3(116f, 0.08f, -18f),
                        new Vector3(184f, 0.08f, 76f),
                        new Vector3(196f, 0.08f, 148f),
                        new Vector3(150f, 0.08f, 204f),
                        new Vector3(72f, 0.08f, 214f),
                        new Vector3(4f, 0.08f, 174f),
                        new Vector3(-58f, 0.08f, 118f),
                        new Vector3(-94f, 0.08f, 42f),
                        new Vector3(-70f, 0.08f, -34f)
                    };
                    nodeWidths = new[] { 28f, 30f, 36f, 52f, 54f, 44f, 36f, 42f, 30f, 30f, 28f };
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
                        new Vector3(-18f, 0.08f, -76f),
                        new Vector3(54f, 0.08f, -58f),
                        new Vector3(128f, 0.08f, 8f),
                        new Vector3(178f, 0.08f, 94f),
                        new Vector3(176f, 0.08f, 174f),
                        new Vector3(126f, 0.08f, 248f),
                        new Vector3(54f, 0.08f, 236f),
                        new Vector3(12f, 1.65f, 176f),
                        new Vector3(-46f, 0.25f, 132f),
                        new Vector3(-98f, 0.08f, 68f),
                        new Vector3(-82f, 0.08f, -18f)
                    };
                    nodeWidths = new[] { 22f, 24f, 24f, 22f, 28f, 46f, 42f, 22f, 24f, 24f, 22f };
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
                case VectorSSMapId.SpecialStage:
                    return new Vector3(0f, -0.28f, 12f);
                case VectorSSMapId.ScraplineYard:
                    return new Vector3(44f, -0.28f, 68f);
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
                case VectorSSMapId.SpecialStage:
                    return new Vector3(620f, 0.18f, 560f);
                case VectorSSMapId.ScraplineYard:
                    return new Vector3(340f, 0.18f, 330f);
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
            const int step = 1;
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
                float length = flat.magnitude + 0.08f;
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
                    float distance = DistancePointToSegmentXZ(point, route.samples[i], route.samples[i + 1]);
                    float localHalfWidth = (route.widths[i] + route.widths[i + 1]) * 0.25f;
                    if (RouteIndexNear(i, start, segmentCount) || RouteIndexNear(i, end, segmentCount))
                    {
                        if (distance < localHalfWidth + 0.12f)
                        {
                            return true;
                        }

                        continue;
                    }

                    if (distance < localHalfWidth + 3.75f)
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
            Transform pit = new GameObject("VECTR Tuner Lab Pit Diorama").transform;
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
            CreateChamferedBox("Pit Metal Resource Bin", pit, new Vector3(-2.6f, 0.55f, 5.4f), Quaternion.identity, new Vector3(1.4f, 0.9f, 1f), inkMaterial, true, 0.06f);
            CreatePrimitive("Pit Metal Bolt Icon", PrimitiveType.Cube, pit, new Vector3(-2.6f, 1.08f, 4.86f), Quaternion.Euler(0f, 0f, 45f), new Vector3(0.42f, 0.08f, 0.08f), trackMarkerBlueMaterial, false);
            CreateChamferedBox("Pit Plastic Resource Bin", pit, new Vector3(-0.8f, 0.55f, 5.4f), Quaternion.identity, new Vector3(1.4f, 0.9f, 1f), playerSecondaryMaterial, true, 0.06f);
            CreatePrimitive("Pit Plastic Aero Icon", PrimitiveType.Cube, pit, new Vector3(-0.8f, 1.08f, 4.86f), Quaternion.Euler(0f, 0f, -14f), new Vector3(0.62f, 0.08f, 0.08f), pitPropMaterial, false);
            CreateChamferedBox("Pit Rubber Resource Bin", pit, new Vector3(1f, 0.55f, 5.4f), Quaternion.identity, new Vector3(1.4f, 0.9f, 1f), wheelMaterial, true, 0.06f);
            CreatePrimitive("Pit Rubber Tread Icon", PrimitiveType.Cube, pit, new Vector3(1f, 1.08f, 4.86f), Quaternion.Euler(0f, 0f, 18f), new Vector3(0.58f, 0.08f, 0.08f), trackMarkerMaterial, false);
            CreateChamferedBox("Pit Half Working Monitor", pit, new Vector3(3.7f, 1.6f, -5.2f), Quaternion.identity, new Vector3(2.3f, 1.25f, 0.18f), inkMaterial, false, 0.05f);
            CreatePrimitive("Pit Monitor Cyan Scanline", PrimitiveType.Cube, pit, new Vector3(3.7f, 1.75f, -5.32f), Quaternion.identity, new Vector3(1.7f, 0.08f, 0.06f), trackMarkerBlueMaterial, false);
            CreatePrimitive("Pit Monitor Warning Scanline", PrimitiveType.Cube, pit, new Vector3(3.55f, 1.43f, -5.32f), Quaternion.identity, new Vector3(1.1f, 0.08f, 0.06f), trackMarkerMaterial, false);
            CreatePrimitive("Pit Cable Coil A", PrimitiveType.Cube, pit, new Vector3(-3.4f, 0.22f, -4f), Quaternion.Euler(0f, 24f, 0f), new Vector3(2.3f, 0.08f, 0.18f), inkMaterial, false);
            CreatePrimitive("Pit Cable Coil B", PrimitiveType.Cube, pit, new Vector3(-3.9f, 0.25f, -3.5f), Quaternion.Euler(0f, -18f, 0f), new Vector3(1.5f, 0.08f, 0.18f), inkMaterial, false);
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
                case VectorSSMapId.SpecialStage:
                    CreateSpecialStageDressing(root, route);
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
                Vector3 signPosition = pose.position + pose.right * (i % 2 == 0 ? -(pose.width * 0.5f + 8.5f) : pose.width * 0.5f + 8.5f) + Vector3.up * 4.2f;
                CreateTrackBillboard(root, "Blackline Neon Ad Board " + i, signPosition, pose.rotation, i % 2 == 0 ? trackMarkerBlueMaterial : trackMarkerMaterial, "VECTR BLK");
            }

            for (int i = 0; i < 4; i++)
            {
                TrackPose pose = route.PoseAtDistance(118f + i * 122f);
                CreateOverheadRaceLight(root, "Blackline Overhead Dash Module " + i, pose, i % 2 == 0 ? trackMarkerBlueMaterial : trackMarkerMaterial);
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
            for (int i = 0; i < 6; i++)
            {
                TrackPose pose = route.PoseAtDistance(72f + i * 64f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 position = pose.position + pose.right * side * (pose.width * 0.5f + 4.6f) + Vector3.up * 0.16f;
                CreateHazardPanel(root, "Scrapline Hazard Teeth " + i, position, pose.rotation, side);
                CreateOilSmear(root, "Scrapline Oil Smear " + i, pose.position - pose.forward * 3f + pose.right * side * (pose.width * 0.22f), pose.rotation);
            }
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

            for (int i = 0; i < 7; i++)
            {
                TrackPose pose = route.PoseAtDistance(88f + i * 74f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 marker = pose.position + pose.right * side * (pose.width * 0.5f - 1.6f) + Vector3.up * 0.22f;
                CreatePrimitive("Rubber Ridge Apex Ink Tooth " + i, PrimitiveType.Cube, root, marker, pose.rotation * Quaternion.Euler(0f, side * 34f, 0f), new Vector3(0.36f, 0.08f, 2.6f), trackMarkerBlueMaterial, false);
                CreateChamferedBox("Rubber Ridge Cliff Shadow Slab " + i, root, pose.position - pose.right * side * (pose.width * 0.5f + 8f) + Vector3.up * 2.2f, pose.rotation, new Vector3(8f, 4f, 1.1f), desertMaterial, false, 0.12f);
            }
        }

        private void CreateSpecialStageDressing(Transform root, RuntimeTrackRoute route)
        {
            CreateSpecialStageStreet(root, "Special Stage Cross Street East West A", new Vector3(0f, 0.13f, -96f), Quaternion.identity, new Vector3(440f, 0.08f, 34f));
            CreateSpecialStageStreet(root, "Special Stage Cross Street East West B", new Vector3(4f, 0.13f, 76f), Quaternion.identity, new Vector3(500f, 0.08f, 38f));
            CreateSpecialStageStreet(root, "Special Stage Cross Street East West C", new Vector3(-18f, 0.13f, 184f), Quaternion.identity, new Vector3(330f, 0.08f, 30f));
            CreateSpecialStageStreet(root, "Special Stage North South Avenue A", new Vector3(-132f, 0.14f, 14f), Quaternion.Euler(0f, 90f, 0f), new Vector3(360f, 0.08f, 34f));
            CreateSpecialStageStreet(root, "Special Stage North South Avenue B", new Vector3(82f, 0.14f, 14f), Quaternion.Euler(0f, 90f, 0f), new Vector3(440f, 0.08f, 38f));

            CreateChamferedBox("Special Stage Central Drift Plaza", root, new Vector3(0f, 0.16f, 26f), Quaternion.identity, new Vector3(122f, 0.09f, 96f), roadMaterial, true, 0.02f);
            CreatePrimitive("Special Stage Plaza Cyan Lane A", PrimitiveType.Cube, root, new Vector3(0f, 0.24f, 26f), Quaternion.identity, new Vector3(112f, 0.035f, 0.34f), trackMarkerBlueMaterial, false);
            CreatePrimitive("Special Stage Plaza Cyan Lane B", PrimitiveType.Cube, root, new Vector3(0f, 0.245f, 26f), Quaternion.Euler(0f, 90f, 0f), new Vector3(82f, 0.035f, 0.34f), trackMarkerBlueMaterial, false);

            Vector3[] blockCenters =
            {
                new Vector3(-214f, 0f, -148f),
                new Vector3(-112f, 0f, -36f),
                new Vector3(32f, 0f, -154f),
                new Vector3(158f, 0f, -34f),
                new Vector3(202f, 0f, 116f),
                new Vector3(76f, 0f, 170f),
                new Vector3(-78f, 0f, 142f),
                new Vector3(-206f, 0f, 66f)
            };

            for (int i = 0; i < blockCenters.Length; i++)
            {
                CreateSpecialStageBlock(root, blockCenters[i], i);
            }

            for (int i = 0; i < 7; i++)
            {
                TrackPose pose = route.PoseAtDistance(90f + i * 118f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 signPosition = pose.position + pose.right * side * (pose.width * 0.5f + 6f) + Vector3.up * 4.2f;
                CreateTrackBillboard(root, "Special Stage Wide City Sign " + i, signPosition, pose.rotation, i % 2 == 0 ? trackMarkerBlueMaterial : trackMarkerMaterial, "SPECIAL");
            }
        }

        private void CreateSpecialStageStreet(Transform root, string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            CreateChamferedBox(name, root, position, rotation, scale, roadMaterial, true, 0.018f);
            CreatePrimitive(name + " Cyan Center", PrimitiveType.Cube, root, position + Vector3.up * 0.075f, rotation, new Vector3(scale.x * 0.92f, 0.03f, 0.22f), trackMarkerBlueMaterial, false);
        }

        private void CreateSpecialStageBlock(Transform root, Vector3 center, int index)
        {
            CreateChamferedBox("Special Stage Block Sidewalk " + index, root, center + Vector3.up * 0.08f, Quaternion.identity, new Vector3(56f, 0.12f, 48f), pitFloorMaterial, false, 0.03f);
            int towers = 2 + index % 3;
            for (int i = 0; i < towers; i++)
            {
                float x = -16f + i * 16f + (index % 2 == 0 ? 0f : 5f);
                float z = i % 2 == 0 ? -10f : 12f;
                float height = 10f + ((index + i) % 4) * 4f;
                Vector3 size = new Vector3(9f + (i % 2) * 3f, height, 10f + ((index + i) % 2) * 4f);
                Vector3 position = center + new Vector3(x, height * 0.5f + 0.12f, z);
                Material material = (index + i) % 3 == 0 ? pitPropMaterial : (index + i) % 3 == 1 ? inkMaterial : playerSecondaryMaterial;
                CreateChamferedBox("Special Stage Building " + index + "-" + i, root, position, Quaternion.Euler(0f, (index * 11f + i * 7f) % 28f - 14f, 0f), size, material, true, 0.08f);
                CreatePrimitive("Special Stage Window Strip " + index + "-" + i, PrimitiveType.Cube, root, position + new Vector3(0f, height * 0.1f, -size.z * 0.51f), Quaternion.identity, new Vector3(size.x * 0.72f, 0.18f, 0.08f), trackMarkerBlueMaterial, false);
            }
        }

        private void CreateTrackBillboard(Transform root, string name, Vector3 position, Quaternion rotation, Material material, string label)
        {
            CreateChamferedBox(name + " Ink Backer", root, position, rotation, new Vector3(5.8f, 2f, 0.26f), inkMaterial, false, 0.08f);
            CreateChamferedBox(name + " Face", root, position + Vector3.up * 0.04f, rotation, new Vector3(5.25f, 1.45f, 0.18f), material, false, 0.05f);
            CreatePrimitive(name + " Label Bar A " + label, PrimitiveType.Cube, root, position + Vector3.up * 0.34f, rotation, new Vector3(3.6f, 0.18f, 0.2f), pitPropMaterial, false);
            CreatePrimitive(name + " Label Bar B", PrimitiveType.Cube, root, position - Vector3.up * 0.26f, rotation, new Vector3(2.15f, 0.14f, 0.2f), inkMaterial, false);
        }

        private void CreateOverheadRaceLight(Transform root, string name, TrackPose pose, Material material)
        {
            Transform lightRig = new GameObject(name).transform;
            lightRig.SetParent(root, false);
            lightRig.localPosition = pose.position + Vector3.up * 5.4f;
            lightRig.localRotation = pose.rotation;
            float width = pose.width * 0.5f + 1.2f;
            CreateChamferedBox(name + " Crossbar", lightRig, Vector3.zero, Quaternion.identity, new Vector3(width * 2f, 0.24f, 0.32f), inkMaterial, false, 0.04f);
            for (int i = 0; i < 5; i++)
            {
                CreateOutlinedPrism(lightRig, name + " LED " + i, 10, new Vector3(-1.6f + i * 0.8f, -0.34f, -0.04f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.26f, 0.26f, 0.07f), material, VectrStyleTokens.DetailOutlineMultiplier);
            }
        }

        private void CreateHazardPanel(Transform root, string name, Vector3 position, Quaternion rotation, float side)
        {
            CreateChamferedBox(name + " Ink Plate", root, position, rotation, new Vector3(4.2f, 0.18f, 1.4f), inkMaterial, false, 0.05f);
            for (int i = 0; i < 4; i++)
            {
                CreatePrimitive(name + " Orange Stripe " + i, PrimitiveType.Cube, root, position + Vector3.up * 0.1f, rotation * Quaternion.Euler(0f, 28f * side, 0f), new Vector3(0.24f, 0.08f, 1.52f), trackMarkerMaterial, false);
                position += (rotation * Vector3.right) * 0.82f;
            }
        }

        private void CreateOilSmear(Transform root, string name, Vector3 position, Quaternion rotation)
        {
            CreatePrimitive(name + " Ink Pool", PrimitiveType.Cube, root, position + Vector3.up * 0.06f, rotation * Quaternion.Euler(0f, 18f, 0f), new Vector3(3.4f, 0.035f, 1.25f), inkMaterial, false);
            CreatePrimitive(name + " Sheen", PrimitiveType.Cube, root, position + Vector3.up * 0.075f, rotation * Quaternion.Euler(0f, -12f, 0f), new Vector3(1.75f, 0.028f, 0.28f), trackMarkerBlueMaterial, false);
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
            if (!vehicleDefinition.isBike)
            {
                Transform[] wheelVisuals = CreateWheelVisuals(root.transform, vehicleDefinition);
                VectorSSWheelVisualSync wheelSync = root.AddComponent<VectorSSWheelVisualSync>();
                wheelSync.Configure(new[] { frontLeft, frontRight, rearLeft, rearRight }, wheelVisuals, Quaternion.Euler(0f, 90f, 0f));
            }

            VehicleTuning tuning = ScriptableObject.CreateInstance<VehicleTuning>();
            tuning.name = "Vector SS Runtime Vehicle Tuning " + vehicleDefinition.displayName;
            VectorSSProgressionUtility.ApplyToVehicleTuning(tuning, vehicleDefinition, playerProfile);

            VehicleController vehicle = root.AddComponent<VehicleController>();
            vehicle.AutomaticTransmission = playerProfile.tuning.automaticTransmission;
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
            VectorSSLaunchSmoke launchSmoke = root.AddComponent<VectorSSLaunchSmoke>();
            GTXDrivingFlowBridge flowBridge = root.AddComponent<GTXDrivingFlowBridge>();
            VectorSSVehicleModuleController moduleController = root.AddComponent<VectorSSVehicleModuleController>();

            sideSlam.Configure(body, flowState, effects);
            boostRam.Configure(body, flowState, effects);
            spinGuard.Configure(body, flowState, effects);
            flowBridge.Configure(vehicle, flowState, effects, boostRam, resetPoint);
            launchSmoke.Configure(vehicle, body, new[] { wheelPositions[2], wheelPositions[3] }, launchSmokeMaterial);
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
            RetroCarVisualProfile profile = RetroCarVisualProfile.ForVehicle(vehicleDefinition.id, vehicleDefinition.visualScale);
            float outline = playerProfile.tuning.outlineThickness;

            CreateOutlinedRetroCarBody(visualRoot, "VECTR Continuous Retro Body", profile, playerMaterial, VectrStyleTokens.OuterOutlineMultiplier * outline);
            CreateRetroGlassPanels(visualRoot, profile);
            CreateRetroWheelArch(visualRoot, "Front", profile.frontWheelZ, profile);
            CreateRetroWheelArch(visualRoot, "Rear", profile.rearWheelZ, profile);
            CreateRetroFaceDetails(visualRoot, profile);
            CreateRallyLivery(visualRoot, profile);
            CreateRoundedMachineCaps(visualRoot, profile);
            CreateRetroSideMirrors(visualRoot, profile);
            CreateVehicleClassVisualKit(visualRoot, vehicleDefinition, profile.scale);
        }

        private GameObject CreateOutlinedRetroCarBody(Transform parent, string name, RetroCarVisualProfile profile, Material material, float outlineMultiplier)
        {
            if (hasInvertedHullOutline)
            {
                GameObject outline = LowPolyVehicleMeshFactory.CreateRetroCarBody(name + " Outline", parent, profile, outlineMaterial, false);
                outline.transform.localScale = Vector3.one * outlineMultiplier;
            }

            return LowPolyVehicleMeshFactory.CreateRetroCarBody(name, parent, profile, material, false);
        }

        private void CreateRetroGlassPanels(Transform parent, RetroCarVisualProfile profile)
        {
            float front = profile.cabinFrontZ;
            float roofFront = profile.cabinFrontZ - profile.hoodLength * 0.2f;
            float rearRoof = profile.cabinRearZ + profile.trunkLength * 0.08f;
            float rearGlass = profile.cabinRearZ - profile.trunkLength * 0.34f;
            float glassInset = 0.08f * profile.scale.x;
            float sideX = profile.halfWidth + 0.012f;
            float sideTopX = profile.cabinHalfWidth + 0.05f * profile.scale.x;

            CreatePanelWithBacker(
                parent,
                "Retro Windshield Panel",
                new Vector3(-profile.cabinHalfWidth - glassInset, profile.beltY + 0.08f * profile.scale.y, front + 0.04f * profile.scale.z),
                new Vector3(profile.cabinHalfWidth + glassInset, profile.beltY + 0.08f * profile.scale.y, front + 0.04f * profile.scale.z),
                new Vector3(sideTopX, profile.roofY - 0.11f * profile.scale.y, roofFront),
                new Vector3(-sideTopX, profile.roofY - 0.11f * profile.scale.y, roofFront),
                glassMaterial,
                1.045f);

            CreatePanelWithBacker(
                parent,
                "Retro Rear Glass Panel",
                new Vector3(-profile.cabinHalfWidth * 0.96f, profile.roofY - 0.13f * profile.scale.y, rearRoof),
                new Vector3(profile.cabinHalfWidth * 0.96f, profile.roofY - 0.13f * profile.scale.y, rearRoof),
                new Vector3(profile.cabinHalfWidth + glassInset, profile.deckY + 0.18f * profile.scale.y, rearGlass),
                new Vector3(-profile.cabinHalfWidth - glassInset, profile.deckY + 0.18f * profile.scale.y, rearGlass),
                glassMaterial,
                1.045f);

            CreatePanelWithBacker(
                parent,
                "Left Retro Side Window",
                new Vector3(-sideX, profile.beltY + 0.1f * profile.scale.y, front - 0.05f * profile.scale.z),
                new Vector3(-sideX, profile.beltY + 0.1f * profile.scale.y, rearGlass + 0.06f * profile.scale.z),
                new Vector3(-sideTopX, profile.roofY - 0.14f * profile.scale.y, rearRoof - 0.08f * profile.scale.z),
                new Vector3(-sideTopX, profile.roofY - 0.12f * profile.scale.y, roofFront + 0.08f * profile.scale.z),
                glassMaterial,
                1.04f);

            CreatePanelWithBacker(
                parent,
                "Right Retro Side Window",
                new Vector3(sideX, profile.beltY + 0.1f * profile.scale.y, rearGlass + 0.06f * profile.scale.z),
                new Vector3(sideX, profile.beltY + 0.1f * profile.scale.y, front - 0.05f * profile.scale.z),
                new Vector3(sideTopX, profile.roofY - 0.12f * profile.scale.y, roofFront + 0.08f * profile.scale.z),
                new Vector3(sideTopX, profile.roofY - 0.14f * profile.scale.y, rearRoof - 0.08f * profile.scale.z),
                glassMaterial,
                1.04f);

            float pillarZ = Mathf.Lerp(front, rearGlass, 0.42f);
            CreatePrimitive("Left Retro Black B Pillar", PrimitiveType.Cube, parent, new Vector3(-sideX - 0.018f, profile.beltY + 0.28f * profile.scale.y, pillarZ), Quaternion.identity, new Vector3(0.055f * profile.scale.x, 0.62f * profile.scale.y, 0.09f * profile.scale.z), inkMaterial, false);
            CreatePrimitive("Right Retro Black B Pillar", PrimitiveType.Cube, parent, new Vector3(sideX + 0.018f, profile.beltY + 0.28f * profile.scale.y, pillarZ), Quaternion.identity, new Vector3(0.055f * profile.scale.x, 0.62f * profile.scale.y, 0.09f * profile.scale.z), inkMaterial, false);
            CreatePrimitive("Retro Roof Rally Strip", PrimitiveType.Cube, parent, new Vector3(0f, profile.roofY + 0.035f * profile.scale.y, Mathf.Lerp(roofFront, rearRoof, 0.48f)), Quaternion.identity, new Vector3(profile.cabinHalfWidth * 1.72f, 0.055f * profile.scale.y, Mathf.Abs(roofFront - rearRoof) * 0.74f), playerAccentMaterial, false);
        }

        private void CreatePanelWithBacker(Transform parent, string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material, float backerScale)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 panelOffset = normal * 0.006f;
            if (hasInvertedHullOutline)
            {
                Vector3 center = (a + b + c + d) * 0.25f;
                LowPolyVehicleMeshFactory.CreateQuadPanel(name + " Ink Gasket", parent, ScalePoint(a, center, backerScale) - panelOffset, ScalePoint(b, center, backerScale) - panelOffset, ScalePoint(c, center, backerScale) - panelOffset, ScalePoint(d, center, backerScale) - panelOffset, inkMaterial);
            }

            LowPolyVehicleMeshFactory.CreateQuadPanel(name, parent, a + panelOffset, b + panelOffset, c + panelOffset, d + panelOffset, material);
        }

        private static Vector3 ScalePoint(Vector3 point, Vector3 center, float scale)
        {
            return center + (point - center) * scale;
        }

        private static Vector3 CarPos(float x, float y, float z, Vector3 scale)
        {
            return new Vector3(x * scale.x, y, z * scale.z);
        }

        private static Vector3 CarScale(float x, float y, float z, Vector3 scale)
        {
            return new Vector3(x * scale.x, y * scale.y, z * scale.z);
        }

        private Transform CreateRazorBikeVisuals(Transform parent, VectorSSVehicleDefinition vehicleDefinition)
        {
            Vector3 scale = vehicleDefinition.visualScale;
            float outline = playerProfile.tuning.outlineThickness;
            Transform leanRoot = new GameObject("Razor Lean Visual").transform;
            leanRoot.SetParent(parent, false);
            parent.gameObject.AddComponent<VectorSSBikeLeanVisual>();

            CreateRazorWheelsAndHardware(leanRoot, scale, outline);
            CreateOutlinedRazorBikeBody(leanRoot, "Razor Continuous Fairing Shell", scale, playerMaterial, VectrStyleTokens.OuterOutlineMultiplier * outline);
            CreateRazorFairingPanels(leanRoot, scale);
            CreateRazorCockpitAndRider(leanRoot, scale);
            CreateRazorDecalsAndNeon(leanRoot, scale);

            Transform trail = LowPolyMeshFactory.CreatePrism("Razor Narrow Boost Trail", leanRoot, 3, new Vector3(0f, 0.54f * scale.y, -2.38f * scale.z), Quaternion.Euler(0f, 0f, 30f), new Vector3(0.22f * scale.x, 0.18f * scale.y, 2.45f * scale.z), boostTrailMaterial, false).transform;
            CreatePrimitive("Razor Left Boost Slice", PrimitiveType.Cube, leanRoot, new Vector3(-0.18f * scale.x, 0.58f * scale.y, -1.88f * scale.z), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.035f * scale.x, 0.08f * scale.y, 1.2f * scale.z), playerAccentMaterial, false);
            CreatePrimitive("Razor Right Boost Slice", PrimitiveType.Cube, leanRoot, new Vector3(0.18f * scale.x, 0.58f * scale.y, -1.88f * scale.z), Quaternion.Euler(0f, 0f, 18f), new Vector3(0.035f * scale.x, 0.08f * scale.y, 1.2f * scale.z), playerSecondaryMaterial, false);
            return trail;
        }

        private GameObject CreateOutlinedRazorBikeBody(Transform parent, string name, Vector3 scale, Material material, float outlineMultiplier)
        {
            if (hasInvertedHullOutline)
            {
                GameObject outline = LowPolyVehicleMeshFactory.CreateRazorBikeBody(name + " Outline", parent, scale, outlineMaterial, false);
                outline.transform.localScale = Vector3.one * outlineMultiplier;
            }

            return LowPolyVehicleMeshFactory.CreateRazorBikeBody(name, parent, scale, material, false);
        }

        private void CreateRazorWheelsAndHardware(Transform parent, Vector3 scale, float outline)
        {
            float wheelDiameter = 0.82f * scale.y;
            float tireWidth = 0.18f * scale.x;
            Vector3 front = new Vector3(0f, 0.43f * scale.y, 1.08f * scale.z);
            Vector3 rear = new Vector3(0f, 0.43f * scale.y, -1.08f * scale.z);

            CreateOutlinedPrism(parent, "Razor Front Wheel", 12, front, Quaternion.Euler(0f, 90f, 0f), new Vector3(wheelDiameter, wheelDiameter, tireWidth), wheelMaterial, 1.06f * outline);
            CreateOutlinedPrism(parent, "Razor Rear Wheel", 12, rear, Quaternion.Euler(0f, 90f, 0f), new Vector3(wheelDiameter, wheelDiameter, tireWidth), wheelMaterial, 1.06f * outline);
            CreateOutlinedPrism(parent, "Razor Front Cyan Rim", 10, front + new Vector3(0.01f * scale.x, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(wheelDiameter * 0.48f, wheelDiameter * 0.48f, tireWidth * 1.12f), playerSecondaryMaterial, VectrStyleTokens.DetailOutlineMultiplier);
            CreateOutlinedPrism(parent, "Razor Rear Cyan Rim", 10, rear + new Vector3(0.01f * scale.x, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(wheelDiameter * 0.48f, wheelDiameter * 0.48f, tireWidth * 1.12f), playerSecondaryMaterial, VectrStyleTokens.DetailOutlineMultiplier);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                CreateOutlinedChamferedBox(parent, "Razor Front Fork " + i, new Vector3(side * 0.12f * scale.x, 0.78f * scale.y, 1.12f * scale.z), Quaternion.Euler(15f, 0f, 0f), new Vector3(0.055f * scale.x, 0.07f * scale.y, 0.96f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.018f);
                CreateOutlinedChamferedBox(parent, "Razor Rear Swingarm " + i, new Vector3(side * 0.15f * scale.x, 0.62f * scale.y, -0.75f * scale.z), Quaternion.Euler(-10f, 0f, 0f), new Vector3(0.055f * scale.x, 0.07f * scale.y, 0.98f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.018f);
            }

            CreateOutlinedPrism(parent, "Razor Handlebar", 6, new Vector3(0f, 1.23f * scale.y, 0.82f * scale.z), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.08f * scale.y, 0.08f * scale.y, 0.9f * scale.x), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier);
        }

        private void CreateRazorFairingPanels(Transform parent, Vector3 scale)
        {
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                CreatePanelWithBacker(
                    parent,
                    side < 0f ? "Razor Left Dark Fairing Panel" : "Razor Right Dark Fairing Panel",
                    new Vector3(side * 0.31f * scale.x, 0.76f * scale.y, 1.08f * scale.z),
                    new Vector3(side * 0.42f * scale.x, 1.02f * scale.y, 0.56f * scale.z),
                    new Vector3(side * 0.36f * scale.x, 0.98f * scale.y, -0.58f * scale.z),
                    new Vector3(side * 0.24f * scale.x, 0.72f * scale.y, -0.2f * scale.z),
                    playerSecondaryMaterial,
                    1.035f);
                CreatePanelWithBacker(
                    parent,
                    side < 0f ? "Razor Left Acid Blade Graphic" : "Razor Right Acid Blade Graphic",
                    new Vector3(side * 0.43f * scale.x, 0.96f * scale.y, 0.72f * scale.z),
                    new Vector3(side * 0.45f * scale.x, 1.08f * scale.y, 0.46f * scale.z),
                    new Vector3(side * 0.36f * scale.x, 0.92f * scale.y, -0.4f * scale.z),
                    new Vector3(side * 0.31f * scale.x, 0.82f * scale.y, -0.1f * scale.z),
                    playerAccentMaterial,
                    1.02f);
                CreateOutlinedPrism(parent, side < 0f ? "Razor Left Number Disc" : "Razor Right Number Disc", 12, new Vector3(side * 0.43f * scale.x, 0.9f * scale.y, 0.16f * scale.z), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.26f * scale.y, 0.26f * scale.y, 0.035f * scale.x), glassMaterial, VectrStyleTokens.DetailOutlineMultiplier);
                CreatePrimitive(side < 0f ? "Razor Left Side Check Rail" : "Razor Right Side Check Rail", PrimitiveType.Cube, parent, new Vector3(side * 0.46f * scale.x, 0.78f * scale.y, -0.02f * scale.z), Quaternion.Euler(0f, 0f, side * 10f), new Vector3(0.045f * scale.x, 0.26f * scale.y, 1.14f * scale.z), playerAccentMaterial, false);
            }
        }

        private void CreateRazorCockpitAndRider(Transform parent, Vector3 scale)
        {
            CreatePanelWithBacker(
                parent,
                "Razor Smoked Windscreen",
                new Vector3(-0.24f * scale.x, 1.05f * scale.y, 0.86f * scale.z),
                new Vector3(0.24f * scale.x, 1.05f * scale.y, 0.86f * scale.z),
                new Vector3(0.15f * scale.x, 1.42f * scale.y, 0.42f * scale.z),
                new Vector3(-0.15f * scale.x, 1.42f * scale.y, 0.42f * scale.z),
                glassMaterial,
                1.08f);
            CreateOutlinedWedge(parent, "Razor Tucked Rider Silhouette", new Vector3(0f, 1.29f * scale.y, -0.14f * scale.z), Quaternion.Euler(-14f, 180f, 0f), new Vector3(0.36f * scale.x, 0.38f * scale.y, 0.72f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier);
            CreateOutlinedPrism(parent, "Razor Low Helmet", 10, new Vector3(0f, 1.54f * scale.y, 0.26f * scale.z), Quaternion.identity, new Vector3(0.28f * scale.y, 0.28f * scale.y, 0.28f * scale.y), glassMaterial, VectrStyleTokens.DetailOutlineMultiplier);
        }

        private void CreateRazorDecalsAndNeon(Transform parent, Vector3 scale)
        {
            CreatePrimitive("Razor Cyan Module Light", PrimitiveType.Cube, parent, new Vector3(0f, 1.12f * scale.y, 0.5f * scale.z), Quaternion.identity, new Vector3(0.38f * scale.x, 0.055f * scale.y, 0.08f * scale.z), playerSecondaryMaterial, false);
            CreatePrimitive("Razor Acid Tank Stripe", PrimitiveType.Cube, parent, new Vector3(0f, 1.22f * scale.y, 0.02f * scale.z), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.08f * scale.x, 0.055f * scale.y, 0.86f * scale.z), playerAccentMaterial, false);
            CreatePrimitive("Razor Tail Warning Slice", PrimitiveType.Cube, parent, new Vector3(0f, 1.06f * scale.y, -1.14f * scale.z), Quaternion.identity, new Vector3(0.4f * scale.x, 0.06f * scale.y, 0.08f * scale.z), playerAccentMaterial, false);
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

        private Transform[] CreateWheelVisuals(Transform parent, VectorSSVehicleDefinition vehicleDefinition)
        {
            float x = 1.2f * vehicleDefinition.visualScale.x;
            float z = 1.38f * vehicleDefinition.visualScale.z;
            Vector3[] positions =
            {
                new Vector3(-x, 0.43f, z),
                new Vector3(x, 0.43f, z),
                new Vector3(-x, 0.43f, -z),
                new Vector3(x, 0.43f, -z)
            };

            Transform[] visuals = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                visuals[i] = CreateWheelVisualAssembly(parent, "Player Wheel " + i, positions[i], vehicleDefinition.visualScale).transform;
            }

            return visuals;
        }

        private GameObject CreateWheelVisualAssembly(Transform parent, string name, Vector3 localPosition, Vector3 vehicleScale)
        {
            GameObject assembly = new GameObject(name + " Visual");
            assembly.transform.SetParent(parent, false);
            assembly.transform.localPosition = localPosition;
            assembly.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            float diameter = Mathf.Lerp(1.02f, 1.16f, Mathf.Clamp01((vehicleScale.x + vehicleScale.z - 1.8f) * 0.5f));
            CreateOutlinedPrism(assembly.transform, name + " Tire", 10, Vector3.zero, Quaternion.identity, new Vector3(diameter, diameter, 0.34f), wheelMaterial, 1.08f * playerProfile.tuning.outlineThickness);
            CreateOutlinedPrism(assembly.transform, name + " Deep Dish Rim", 8, new Vector3(0f, 0f, 0.19f), Quaternion.identity, new Vector3(diameter * 0.58f, diameter * 0.58f, 0.12f), playerSecondaryMaterial, VectrStyleTokens.DetailOutlineMultiplier);
            CreatePrimitive(name + " Accent Spoke A", PrimitiveType.Cube, assembly.transform, new Vector3(0f, 0f, 0.28f), Quaternion.identity, new Vector3(0.1f, diameter * 0.68f, 0.045f), playerAccentMaterial, false);
            CreatePrimitive(name + " Accent Spoke B", PrimitiveType.Cube, assembly.transform, new Vector3(0f, 0f, 0.315f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.09f, diameter * 0.54f, 0.04f), playerAccentMaterial, false);
            CreatePrimitive(name + " Tire Letter Block", PrimitiveType.Cube, assembly.transform, new Vector3(0f, diameter * 0.44f, 0.32f), Quaternion.identity, new Vector3(diameter * 0.34f, 0.05f, 0.035f), playerSecondaryMaterial, false);
            CreatePrimitive(name + " Hub", PrimitiveType.Cube, assembly.transform, new Vector3(0f, 0f, 0.36f), Quaternion.identity, new Vector3(0.2f, 0.2f, 0.055f), inkMaterial, false);
            return assembly;
        }

        private void CreateRetroWheelArch(Transform parent, string label, float z, RetroCarVisualProfile profile)
        {
            float outline = playerProfile.tuning.outlineThickness;
            float sideX = profile.halfWidth + profile.fenderWidth * 0.4f;
            float fenderHeight = profile.wheelRadius * 0.95f;
            float fenderLength = profile.wheelRadius * 2.06f;
            float archDiameter = profile.wheelRadius * 2.16f;
            float archY = 0.43f * profile.scale.y;
            CreateOutlinedChamferedBox(parent, label + " Left Integrated Fender", new Vector3(-sideX, archY + fenderHeight * 0.24f, z), Quaternion.identity, new Vector3(profile.fenderWidth * 1.42f, fenderHeight, fenderLength), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier * outline, 0.13f);
            CreateOutlinedChamferedBox(parent, label + " Right Integrated Fender", new Vector3(sideX, archY + fenderHeight * 0.24f, z), Quaternion.identity, new Vector3(profile.fenderWidth * 1.42f, fenderHeight, fenderLength), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier * outline, 0.13f);
            CreateOutlinedPrism(parent, label + " Left Ink Wheel Well", 14, new Vector3(-profile.halfWidth - 0.03f * profile.scale.x, archY, z), Quaternion.Euler(0f, 90f, 0f), new Vector3(archDiameter, archDiameter, 0.07f * profile.scale.x), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier);
            CreateOutlinedPrism(parent, label + " Right Ink Wheel Well", 14, new Vector3(profile.halfWidth + 0.03f * profile.scale.x, archY, z), Quaternion.Euler(0f, 90f, 0f), new Vector3(archDiameter, archDiameter, 0.07f * profile.scale.x), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier);
        }

        private void CreateRetroFaceDetails(Transform parent, RetroCarVisualProfile profile)
        {
            float front = profile.FrontZ + 0.018f * profile.scale.z;
            float rear = profile.RearZ - 0.018f * profile.scale.z;
            CreatePrimitive("Retro Front Black Grille", PrimitiveType.Cube, parent, new Vector3(0f, profile.rockerY + 0.21f * profile.scale.y, front), Quaternion.identity, new Vector3(profile.halfWidth * 1.26f, 0.2f * profile.scale.y, 0.07f * profile.scale.z), inkMaterial, false);
            CreatePrimitive("Retro Front Grille Slat A", PrimitiveType.Cube, parent, new Vector3(0f, profile.rockerY + 0.27f * profile.scale.y, front + 0.04f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 1.08f, 0.032f * profile.scale.y, 0.04f * profile.scale.z), playerSecondaryMaterial, false);
            CreatePrimitive("Retro Front Grille Slat B", PrimitiveType.Cube, parent, new Vector3(0f, profile.rockerY + 0.16f * profile.scale.y, front + 0.045f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 1.08f, 0.032f * profile.scale.y, 0.04f * profile.scale.z), playerSecondaryMaterial, false);
            CreatePrimitive("Retro Left Headlight", PrimitiveType.Cube, parent, new Vector3(-profile.halfWidth * 0.64f, profile.beltY - 0.08f * profile.scale.y, front + 0.05f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 0.34f, 0.17f * profile.scale.y, 0.07f * profile.scale.z), glassMaterial, false);
            CreatePrimitive("Retro Right Headlight", PrimitiveType.Cube, parent, new Vector3(profile.halfWidth * 0.64f, profile.beltY - 0.08f * profile.scale.y, front + 0.05f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 0.34f, 0.17f * profile.scale.y, 0.07f * profile.scale.z), glassMaterial, false);
            CreatePrimitive("Retro Left Turn Light Stack", PrimitiveType.Cube, parent, new Vector3(-profile.halfWidth * 0.9f, profile.rockerY + 0.11f * profile.scale.y, front + 0.055f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 0.18f, 0.1f * profile.scale.y, 0.055f * profile.scale.z), playerAccentMaterial, false);
            CreatePrimitive("Retro Right Turn Light Stack", PrimitiveType.Cube, parent, new Vector3(profile.halfWidth * 0.9f, profile.rockerY + 0.11f * profile.scale.y, front + 0.055f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 0.18f, 0.1f * profile.scale.y, 0.055f * profile.scale.z), playerAccentMaterial, false);
            CreatePrimitive("Retro Left Tail Light", PrimitiveType.Cube, parent, new Vector3(-profile.halfWidth * 0.58f, profile.beltY - 0.13f * profile.scale.y, rear), Quaternion.identity, new Vector3(profile.halfWidth * 0.34f, 0.16f * profile.scale.y, 0.07f * profile.scale.z), playerAccentMaterial, false);
            CreatePrimitive("Retro Right Tail Light", PrimitiveType.Cube, parent, new Vector3(profile.halfWidth * 0.58f, profile.beltY - 0.13f * profile.scale.y, rear), Quaternion.identity, new Vector3(profile.halfWidth * 0.34f, 0.16f * profile.scale.y, 0.07f * profile.scale.z), playerAccentMaterial, false);
            CreatePrimitive("Retro Black Rear Plate", PrimitiveType.Cube, parent, new Vector3(0f, profile.beltY - 0.07f * profile.scale.y, rear - 0.02f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 0.58f, 0.16f * profile.scale.y, 0.055f * profile.scale.z), inkMaterial, false);
        }

        private void CreateRoundedMachineCaps(Transform parent, RetroCarVisualProfile profile)
        {
            CreateOutlinedChamferedBox(parent, "Retro Front Bumper Bar", new Vector3(0f, profile.rockerY - 0.02f * profile.scale.y, profile.FrontZ + 0.04f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 1.86f, 0.22f * profile.scale.y, 0.2f * profile.scale.z), playerAccentMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.08f);
            CreateOutlinedChamferedBox(parent, "Retro Rear Bumper Bar", new Vector3(0f, profile.rockerY - 0.02f * profile.scale.y, profile.RearZ - 0.04f * profile.scale.z), Quaternion.identity, new Vector3(profile.halfWidth * 1.82f, 0.22f * profile.scale.y, 0.2f * profile.scale.z), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.08f);
            CreateOutlinedChamferedBox(parent, "Retro Left Rocker Skirt", new Vector3(-profile.halfWidth - 0.025f * profile.scale.x, profile.rockerY - 0.05f * profile.scale.y, -0.08f * profile.scale.z), Quaternion.identity, new Vector3(0.18f * profile.scale.x, 0.2f * profile.scale.y, profile.length * 0.68f), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.07f);
            CreateOutlinedChamferedBox(parent, "Retro Right Rocker Skirt", new Vector3(profile.halfWidth + 0.025f * profile.scale.x, profile.rockerY - 0.05f * profile.scale.y, -0.08f * profile.scale.z), Quaternion.identity, new Vector3(0.18f * profile.scale.x, 0.2f * profile.scale.y, profile.length * 0.68f), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.07f);
        }

        private void CreateRetroSideMirrors(Transform parent, RetroCarVisualProfile profile)
        {
            CreateOutlinedChamferedBox(parent, "Left Retro Mirror", new Vector3(-profile.halfWidth - 0.13f * profile.scale.x, profile.beltY + 0.22f * profile.scale.y, profile.cabinFrontZ + 0.15f * profile.scale.z), Quaternion.Euler(0f, 0f, -8f), new Vector3(0.16f * profile.scale.x, 0.13f * profile.scale.y, 0.3f * profile.scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.04f);
            CreateOutlinedChamferedBox(parent, "Right Retro Mirror", new Vector3(profile.halfWidth + 0.13f * profile.scale.x, profile.beltY + 0.22f * profile.scale.y, profile.cabinFrontZ + 0.15f * profile.scale.z), Quaternion.Euler(0f, 0f, 8f), new Vector3(0.16f * profile.scale.x, 0.13f * profile.scale.y, 0.3f * profile.scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.04f);
        }

        private void CreateRallyLivery(Transform parent, RetroCarVisualProfile profile)
        {
            float hoodY = profile.hoodY + 0.035f * profile.scale.y;
            float hoodFront = profile.FrontZ - 0.55f * profile.scale.z;
            float hoodRear = profile.cabinFrontZ + 0.08f * profile.scale.z;
            CreatePanelWithBacker(parent, "Retro Hood Wide Graphic", new Vector3(-profile.halfWidth * 0.68f, hoodY, hoodFront), new Vector3(-profile.halfWidth * 0.18f, hoodY + 0.01f * profile.scale.y, hoodFront + 0.08f * profile.scale.z), new Vector3(-profile.halfWidth * 0.02f, hoodY + 0.02f * profile.scale.y, hoodRear), new Vector3(-profile.halfWidth * 0.54f, hoodY + 0.01f * profile.scale.y, hoodRear - 0.06f * profile.scale.z), playerSecondaryMaterial, 1.035f);
            CreatePanelWithBacker(parent, "Retro Hood Accent Graphic", new Vector3(profile.halfWidth * 0.02f, hoodY + 0.02f * profile.scale.y, hoodFront + 0.04f * profile.scale.z), new Vector3(profile.halfWidth * 0.5f, hoodY, hoodFront + 0.1f * profile.scale.z), new Vector3(profile.halfWidth * 0.34f, hoodY + 0.02f * profile.scale.y, hoodRear), new Vector3(profile.halfWidth * -0.08f, hoodY + 0.025f * profile.scale.y, hoodRear - 0.08f * profile.scale.z), playerAccentMaterial, 1.035f);
            CreatePrimitive("Retro Roof Number Slash", PrimitiveType.Cube, parent, new Vector3(0.04f * profile.scale.x, profile.roofY + 0.085f * profile.scale.y, Mathf.Lerp(profile.cabinFrontZ, profile.cabinRearZ, 0.55f)), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.18f * profile.scale.x, 0.055f * profile.scale.y, 0.62f * profile.scale.z), playerSecondaryMaterial, false);

            CreateSideLivery(parent, profile, -1f);
            CreateSideLivery(parent, profile, 1f);

            CreatePrimitive("Left Hood Vent Ink", PrimitiveType.Cube, parent, new Vector3(-profile.halfWidth * 0.46f, hoodY + 0.02f * profile.scale.y, profile.cabinFrontZ + 0.34f * profile.scale.z), Quaternion.identity, new Vector3(0.32f * profile.scale.x, 0.045f * profile.scale.y, 0.08f * profile.scale.z), inkMaterial, false);
            CreatePrimitive("Right Hood Vent Ink", PrimitiveType.Cube, parent, new Vector3(profile.halfWidth * 0.46f, hoodY + 0.02f * profile.scale.y, profile.cabinFrontZ + 0.34f * profile.scale.z), Quaternion.identity, new Vector3(0.32f * profile.scale.x, 0.045f * profile.scale.y, 0.08f * profile.scale.z), inkMaterial, false);
            CreatePrimitive("Cabin Rear Ink Shelf", PrimitiveType.Cube, parent, new Vector3(0f, profile.deckY + 0.28f * profile.scale.y, profile.cabinRearZ - 0.32f * profile.scale.z), Quaternion.identity, new Vector3(profile.cabinHalfWidth * 2f, 0.09f * profile.scale.y, 0.15f * profile.scale.z), inkMaterial, false);
        }

        private void CreateSideLivery(Transform parent, RetroCarVisualProfile profile, float side)
        {
            float x = side * (profile.halfWidth + 0.038f * profile.scale.x);
            float sign = Mathf.Sign(side);
            string prefix = side < 0f ? "Left" : "Right";
            CreatePanelWithBacker(
                parent,
                prefix + " Door Bold Graphic",
                new Vector3(x, profile.rockerY + 0.08f * profile.scale.y, profile.cabinFrontZ - 0.04f * profile.scale.z),
                new Vector3(x, profile.rockerY + 0.08f * profile.scale.y, profile.cabinRearZ - 0.22f * profile.scale.z),
                new Vector3(x, profile.beltY - 0.08f * profile.scale.y, profile.cabinRearZ - 0.04f * profile.scale.z),
                new Vector3(x, profile.beltY - 0.02f * profile.scale.y, profile.cabinFrontZ + 0.22f * profile.scale.z),
                playerSecondaryMaterial,
                1.035f);

            CreatePanelWithBacker(
                parent,
                prefix + " Rear Quarter Slash",
                new Vector3(x + sign * 0.006f, profile.rockerY + 0.1f * profile.scale.y, profile.rearWheelZ - 0.18f * profile.scale.z),
                new Vector3(x + sign * 0.006f, profile.rockerY + 0.16f * profile.scale.y, profile.RearZ + 0.42f * profile.scale.z),
                new Vector3(x + sign * 0.006f, profile.beltY + 0.02f * profile.scale.y, profile.RearZ + 0.76f * profile.scale.z),
                new Vector3(x + sign * 0.006f, profile.beltY - 0.05f * profile.scale.y, profile.rearWheelZ + 0.42f * profile.scale.z),
                playerAccentMaterial,
                1.03f);

            LowPolyMeshFactory.CreatePrism(prefix + " Door Number Disc", parent, 12, new Vector3(x + sign * 0.014f, profile.rockerY + 0.34f * profile.scale.y, Mathf.Lerp(profile.cabinFrontZ, profile.cabinRearZ, 0.52f)), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.58f * profile.scale.x, 0.58f * profile.scale.y, 0.055f * profile.scale.x), playerMaterial, false);
            CreatePrimitive(prefix + " Door Number Bar", PrimitiveType.Cube, parent, new Vector3(x + sign * 0.026f, profile.rockerY + 0.34f * profile.scale.y, Mathf.Lerp(profile.cabinFrontZ, profile.cabinRearZ, 0.52f)), Quaternion.identity, new Vector3(0.06f * profile.scale.x, 0.1f * profile.scale.y, 0.44f * profile.scale.z), inkMaterial, false);
            CreatePrimitive(prefix + " Tiny Fake Sponsor A", PrimitiveType.Cube, parent, new Vector3(x + sign * 0.02f, profile.rockerY + 0.06f * profile.scale.y, profile.cabinFrontZ - 0.48f * profile.scale.z), Quaternion.identity, new Vector3(0.055f * profile.scale.x, 0.07f * profile.scale.y, 0.42f * profile.scale.z), playerAccentMaterial, false);
            CreatePrimitive(prefix + " Tiny Fake Sponsor B", PrimitiveType.Cube, parent, new Vector3(x + sign * 0.02f, profile.rockerY + 0.06f * profile.scale.y, profile.cabinFrontZ - 0.88f * profile.scale.z), Quaternion.identity, new Vector3(0.055f * profile.scale.x, 0.07f * profile.scale.y, 0.28f * profile.scale.z), playerSecondaryMaterial, false);
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

        private GameObject CreateRacingRival(int index, VectorSSVehicleDefinition vehicleDefinition, Vector3 position, Quaternion rotation)
        {
            GameObject rival = new GameObject("VECTR Rival Racer " + index);
            rival.transform.SetPositionAndRotation(position, rotation);

            Rigidbody body = rival.AddComponent<Rigidbody>();
            body.mass = 1250f * Mathf.Max(0.6f, vehicleDefinition != null ? vehicleDefinition.massMultiplier : 1f);
            body.drag = 0.025f;
            body.angularDrag = 1.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider collider = rival.AddComponent<BoxCollider>();
            collider.center = vehicleDefinition != null ? vehicleDefinition.colliderCenter : new Vector3(0f, 0.68f, 0f);
            collider.size = vehicleDefinition != null ? vehicleDefinition.colliderSize : new Vector3(2.15f, 0.92f, 4.2f);

            Material previousPlayer = playerMaterial;
            Material previousAccent = playerAccentMaterial;
            Material previousSecondary = playerSecondaryMaterial;
            ApplyRivalPalette(index, vehicleDefinition);
            if (vehicleDefinition != null && vehicleDefinition.isBike)
            {
                CreateRazorBikeVisuals(rival.transform, vehicleDefinition);
            }
            else if (vehicleDefinition != null)
            {
                CreateCarVisuals(rival.transform, vehicleDefinition);
                CreateWheelVisuals(rival.transform, vehicleDefinition);
            }
            else
            {
                CreateOutlinedChamferedBox(rival.transform, "Rival Body", new Vector3(0f, 0.68f, 0f), Quaternion.identity, new Vector3(2.15f, 0.68f, 4.2f), rivalMaterial, 1.08f, 0.1f);
                CreateOutlinedWedge(rival.transform, "Rival Cockpit", new Vector3(0f, 1.14f, -0.42f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.2f, 0.56f, 1.32f), rivalMaterial, 1.08f);
            }

            playerMaterial = previousPlayer;
            playerAccentMaterial = previousAccent;
            playerSecondaryMaterial = previousSecondary;

            Material stripeMaterialForRival = index % 2 == 0 ? trackMarkerBlueMaterial : trackMarkerMaterial;
            CreatePrimitive("Rival Number Plate " + index, PrimitiveType.Cube, rival.transform, new Vector3(0f, 1.22f, 1.82f), Quaternion.identity, new Vector3(0.58f, 0.28f, 0.08f), stripeMaterialForRival, false);
            rival.AddComponent<SimpleRouteRivalAI>();
            rival.AddComponent<DummyRivalTarget>();
            return rival;
        }

        private void ApplyRivalPalette(int index, VectorSSVehicleDefinition vehicleDefinition)
        {
            Color baseBody = vehicleDefinition != null ? vehicleDefinition.bodyColor : VectrStyleTokens.AcidYellowGreen;
            Color baseAccent = vehicleDefinition != null ? vehicleDefinition.accentColor : VectrStyleTokens.ElectricCyan;
            Color baseSecondary = vehicleDefinition != null ? vehicleDefinition.secondaryColor : VectrStyleTokens.SafetyOrange;
            float hueShift = Mathf.Repeat(0.18f + index * 0.17f, 1f);
            playerMaterial = CreateMaterial("VECTR Rival " + index + " Body Palette", PaletteShift(baseBody, hueShift, 1.12f), true);
            playerAccentMaterial = CreateMaterial("VECTR Rival " + index + " Accent Palette", PaletteShift(baseAccent, hueShift + 0.08f, 1.18f), true);
            playerSecondaryMaterial = CreateMaterial("VECTR Rival " + index + " Secondary Palette", PaletteShift(baseSecondary, hueShift + 0.14f, 1.08f), true);
        }

        private static Color PaletteShift(Color color, float hueOffset, float valueMultiplier)
        {
            float h;
            float s;
            float v;
            Color.RGBToHSV(color, out h, out s, out v);
            return Color.HSVToRGB(Mathf.Repeat(h + hueOffset, 1f), Mathf.Clamp01(s * 1.16f + 0.08f), Mathf.Clamp01(v * valueMultiplier));
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

            GTXPixelFilter pixelFilter = camera.GetComponent<GTXPixelFilter>();
            if (pixelFilter == null)
            {
                pixelFilter = camera.gameObject.AddComponent<GTXPixelFilter>();
            }

            pixelFilter.PixelFilterEnabled = false;
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
                    size = layout != null ? layout.size : new Vector2(202f, 64f),
                    scale = layout != null ? layout.scale : module.defaultHudScale,
                    visible = layout == null || layout.visible
                });
            }

            return widgets;
        }

        private void SyncPausedModuleHudLayout()
        {
            if (activeModuleHud == null || activeModuleController == null || playerProfile == null)
            {
                return;
            }

            bool changed = false;
            IList<VectorSSModuleDefinition> modules = activeModuleController.InstalledModules;
            for (int i = 0; i < modules.Count; i++)
            {
                VectorSSModuleDefinition module = modules[i];
                if (module == null || module.widget == VectorSSModuleWidget.None)
                {
                    continue;
                }

                Vector2 position;
                Vector2 size;
                float scale;
                if (!activeModuleHud.TryReadWidgetLayout(module.id, out position, out size, out scale))
                {
                    continue;
                }

                VectorSSModuleHudLayout layout = playerProfile.GetModuleLayout(selectedVehicle.id, module, true);
                if (layout == null)
                {
                    continue;
                }

                Vector2 clampedSize = new Vector2(Mathf.Clamp(size.x, 96f, 720f), Mathf.Clamp(size.y, 38f, 360f));
                if ((layout.position - position).sqrMagnitude > 0.25f || (layout.size - clampedSize).sqrMagnitude > 0.25f || Mathf.Abs(layout.scale - scale) > 0.001f)
                {
                    layout.position = position;
                    layout.size = clampedSize;
                    layout.scale = Mathf.Clamp(scale, 0.55f, 2.7f);
                    changed = true;
                }
            }

            if (changed)
            {
                VectorSSSaveSystem.Save(playerProfile);
            }
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

        private void CreateVehicleClassVisualKit(Transform parent, VectorSSVehicleDefinition vehicle, Vector3 scale)
        {
            switch (vehicle.id)
            {
                case VectorSSVehicleId.Hammer:
                    CreateHammerVisualKit(parent, scale);
                    break;
                case VectorSSVehicleId.Needle:
                    CreateNeedleVisualKit(parent, scale);
                    break;
                case VectorSSVehicleId.Surge:
                    CreateSurgeVisualKit(parent, scale);
                    break;
                case VectorSSVehicleId.Hauler:
                    CreatePickupVisualKit(parent, scale);
                    break;
            }
        }

        private void CreateHammerVisualKit(Transform parent, Vector3 scale)
        {
            CreateOutlinedChamferedBox(parent, "Hammer Bolt-On Ram Plate", new Vector3(0f, 0.72f, 2.58f * scale.z), Quaternion.identity, new Vector3(2.48f * scale.x, 0.42f, 0.26f), playerSecondaryMaterial, VectrStyleTokens.OuterOutlineMultiplier, 0.08f);
            CreateOutlinedChamferedBox(parent, "Hammer Hood Scoop", new Vector3(0f, 1.28f, 0.8f * scale.z), Quaternion.identity, new Vector3(0.84f, 0.28f, 0.92f), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.07f);
            CreateOutlinedChamferedBox(parent, "Hammer Wide Front Fender L", new Vector3(-1.38f * scale.x, 0.78f, 1.28f * scale.z), Quaternion.identity, new Vector3(0.38f, 0.46f, 1.28f), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.11f);
            CreateOutlinedChamferedBox(parent, "Hammer Wide Front Fender R", new Vector3(1.38f * scale.x, 0.78f, 1.28f * scale.z), Quaternion.identity, new Vector3(0.38f, 0.46f, 1.28f), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.11f);
            CreateOutlinedChamferedBox(parent, "Hammer Wide Rear Fender L", new Vector3(-1.38f * scale.x, 0.78f, -1.26f * scale.z), Quaternion.identity, new Vector3(0.42f, 0.5f, 1.36f), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.11f);
            CreateOutlinedChamferedBox(parent, "Hammer Wide Rear Fender R", new Vector3(1.38f * scale.x, 0.78f, -1.26f * scale.z), Quaternion.identity, new Vector3(0.42f, 0.5f, 1.36f), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.11f);
            CreatePrimitive("Hammer Mudflap FL", PrimitiveType.Cube, parent, new Vector3(-1.28f * scale.x, 0.34f, 1.92f * scale.z), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.42f, 0.52f, 0.06f), inkMaterial, false);
            CreatePrimitive("Hammer Mudflap FR", PrimitiveType.Cube, parent, new Vector3(1.28f * scale.x, 0.34f, 1.92f * scale.z), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.42f, 0.52f, 0.06f), inkMaterial, false);
            CreatePrimitive("Hammer Mudflap RL", PrimitiveType.Cube, parent, new Vector3(-1.28f * scale.x, 0.34f, -1.96f * scale.z), Quaternion.Euler(8f, 0f, 0f), new Vector3(0.42f, 0.52f, 0.06f), inkMaterial, false);
            CreatePrimitive("Hammer Mudflap RR", PrimitiveType.Cube, parent, new Vector3(1.28f * scale.x, 0.34f, -1.96f * scale.z), Quaternion.Euler(8f, 0f, 0f), new Vector3(0.42f, 0.52f, 0.06f), inkMaterial, false);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.54f + i * 0.36f;
                CreateOutlinedPrism(parent, "Hammer Rally Lamp " + i, 12, new Vector3(x, 0.92f, 2.42f * scale.z), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.24f, 0.24f, 0.08f), glassMaterial, VectrStyleTokens.DetailOutlineMultiplier);
            }
        }

        private void CreateNeedleVisualKit(Transform parent, Vector3 scale)
        {
            CreateOutlinedChamferedBox(parent, "Needle Low Drift Splitter", new Vector3(0f, 0.46f, 2.48f * scale.z), Quaternion.identity, new Vector3(2.28f * scale.x, 0.12f, 0.72f), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.05f);
            CreateOutlinedChamferedBox(parent, "Needle Tall Drift Wing", new Vector3(0f, 1.52f, -2.28f * scale.z), Quaternion.identity, new Vector3(2.3f * scale.x, 0.16f, 0.32f), playerAccentMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.05f);
            CreatePrimitive("Needle Wing Left Upright", PrimitiveType.Cube, parent, new Vector3(-0.72f * scale.x, 1.16f, -2.16f * scale.z), Quaternion.Euler(0f, 0f, -8f), new Vector3(0.08f, 0.72f, 0.12f), inkMaterial, false);
            CreatePrimitive("Needle Wing Right Upright", PrimitiveType.Cube, parent, new Vector3(0.72f * scale.x, 1.16f, -2.16f * scale.z), Quaternion.Euler(0f, 0f, 8f), new Vector3(0.08f, 0.72f, 0.12f), inkMaterial, false);
            CreatePrimitive("Needle Magenta Slash L", PrimitiveType.Cube, parent, new Vector3(-1.16f * scale.x, 0.95f, -0.1f), Quaternion.Euler(0f, 0f, 18f), new Vector3(0.07f, 0.22f, 2.3f), playerAccentMaterial, false);
            CreatePrimitive("Needle Magenta Slash R", PrimitiveType.Cube, parent, new Vector3(1.16f * scale.x, 0.95f, -0.1f), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.07f, 0.22f, 2.3f), playerAccentMaterial, false);
            CreatePrimitive("Needle Cyan Tire Letter FL", PrimitiveType.Cube, parent, new Vector3(-1.23f * scale.x, 0.83f, 1.38f * scale.z), Quaternion.identity, new Vector3(0.055f, 0.08f, 0.58f), playerSecondaryMaterial, false);
            CreatePrimitive("Needle Cyan Tire Letter FR", PrimitiveType.Cube, parent, new Vector3(1.23f * scale.x, 0.83f, 1.38f * scale.z), Quaternion.identity, new Vector3(0.055f, 0.08f, 0.58f), playerSecondaryMaterial, false);
        }

        private void CreateSurgeVisualKit(Transform parent, Vector3 scale)
        {
            CreateOutlinedWedge(parent, "Surge Active Nose Duct", new Vector3(0f, 0.94f, 1.92f * scale.z), Quaternion.identity, new Vector3(1.52f, 0.32f, 0.86f), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier);
            CreatePrimitive("Surge Cyan Intake L", PrimitiveType.Cube, parent, new Vector3(-0.72f * scale.x, 0.86f, 2.12f * scale.z), Quaternion.identity, new Vector3(0.42f, 0.08f, 0.12f), playerMaterial, false);
            CreatePrimitive("Surge Cyan Intake R", PrimitiveType.Cube, parent, new Vector3(0.72f * scale.x, 0.86f, 2.12f * scale.z), Quaternion.identity, new Vector3(0.42f, 0.08f, 0.12f), playerMaterial, false);
            CreateOutlinedWedge(parent, "Surge Aero Fin L", new Vector3(-1.26f * scale.x, 1.04f, -1.48f * scale.z), Quaternion.Euler(0f, 0f, -14f), new Vector3(0.22f, 0.56f, 1.5f), playerAccentMaterial, VectrStyleTokens.PanelOutlineMultiplier);
            CreateOutlinedWedge(parent, "Surge Aero Fin R", new Vector3(1.26f * scale.x, 1.04f, -1.48f * scale.z), Quaternion.Euler(0f, 180f, 14f), new Vector3(0.22f, 0.56f, 1.5f), playerAccentMaterial, VectrStyleTokens.PanelOutlineMultiplier);
            CreatePrimitive("Surge Violet Battery Spine", PrimitiveType.Cube, parent, new Vector3(0f, 1.15f, -0.2f), Quaternion.identity, new Vector3(0.34f, 0.12f, 2.8f), playerAccentMaterial, false);
            CreatePrimitive("Surge White Charge Tick A", PrimitiveType.Cube, parent, new Vector3(-0.22f, 1.24f, -0.82f), Quaternion.Euler(0f, 0f, 24f), new Vector3(0.08f, 0.08f, 0.54f), playerSecondaryMaterial, false);
            CreatePrimitive("Surge White Charge Tick B", PrimitiveType.Cube, parent, new Vector3(0.22f, 1.24f, 0.18f), Quaternion.Euler(0f, 0f, -24f), new Vector3(0.08f, 0.08f, 0.54f), playerSecondaryMaterial, false);
        }

        private void CreatePickupVisualKit(Transform parent, Vector3 scale)
        {
            CreateOutlinedChamferedBox(parent, "Hauler Open Bed Ink Floor", new Vector3(0f, 0.98f * scale.y, -1.28f * scale.z), Quaternion.identity, new Vector3(1.74f * scale.x, 0.08f * scale.y, 1.64f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.04f);
            CreateOutlinedChamferedBox(parent, "Hauler Left Bed Rail", new Vector3(-1.02f * scale.x, 1.22f * scale.y, -1.28f * scale.z), Quaternion.identity, new Vector3(0.16f * scale.x, 0.34f * scale.y, 1.72f * scale.z), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.05f);
            CreateOutlinedChamferedBox(parent, "Hauler Right Bed Rail", new Vector3(1.02f * scale.x, 1.22f * scale.y, -1.28f * scale.z), Quaternion.identity, new Vector3(0.16f * scale.x, 0.34f * scale.y, 1.72f * scale.z), playerSecondaryMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.05f);
            CreateOutlinedChamferedBox(parent, "Hauler Tailgate Armor", new Vector3(0f, 0.94f * scale.y, -2.4f * scale.z), Quaternion.identity, new Vector3(2.04f * scale.x, 0.5f * scale.y, 0.18f * scale.z), playerAccentMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.08f);
            CreateOutlinedChamferedBox(parent, "Hauler Heavy Front Push Bar", new Vector3(0f, 0.72f * scale.y, 2.52f * scale.z), Quaternion.identity, new Vector3(2.18f * scale.x, 0.38f * scale.y, 0.2f * scale.z), playerSecondaryMaterial, VectrStyleTokens.OuterOutlineMultiplier, 0.07f);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                CreateOutlinedChamferedBox(parent, "Hauler Roll Bar Upright " + i, new Vector3(side * 0.72f * scale.x, 1.48f * scale.y, -0.52f * scale.z), Quaternion.Euler(0f, 0f, side * 6f), new Vector3(0.12f * scale.x, 0.82f * scale.y, 0.14f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.025f);
                CreateOutlinedChamferedBox(parent, "Hauler Wide Fender " + i, new Vector3(side * 1.34f * scale.x, 0.82f * scale.y, -1.38f * scale.z), Quaternion.identity, new Vector3(0.34f * scale.x, 0.42f * scale.y, 1.18f * scale.z), playerMaterial, VectrStyleTokens.PanelOutlineMultiplier, 0.1f);
                CreatePrimitive("Hauler Bed Slash " + i, PrimitiveType.Cube, parent, new Vector3(side * 1.11f * scale.x, 1.12f * scale.y, -1.4f * scale.z), Quaternion.Euler(0f, 0f, side * 20f), new Vector3(0.07f * scale.x, 0.18f * scale.y, 1.18f * scale.z), playerAccentMaterial, false);
            }

            CreateOutlinedChamferedBox(parent, "Hauler Roll Bar Top", new Vector3(0f, 1.9f * scale.y, -0.52f * scale.z), Quaternion.identity, new Vector3(1.64f * scale.x, 0.1f * scale.y, 0.16f * scale.z), inkMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.025f);
            CreateOutlinedChamferedBox(parent, "Hauler Roof Scoop", new Vector3(0f, 1.72f * scale.y, 0.12f * scale.z), Quaternion.identity, new Vector3(0.72f * scale.x, 0.18f * scale.y, 0.7f * scale.z), playerSecondaryMaterial, VectrStyleTokens.DetailOutlineMultiplier, 0.06f);
            CreatePrimitive("Hauler Bed Cyan Cargo Light", PrimitiveType.Cube, parent, new Vector3(0f, 1.58f * scale.y, -0.56f * scale.z), Quaternion.identity, new Vector3(0.62f * scale.x, 0.07f * scale.y, 0.08f * scale.z), playerAccentMaterial, false);
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
                material.SetColor("_ShadowColor", VectrStyleTokens.ShadowFor(color));
            }

            if (material.HasProperty("_HighlightColor"))
            {
                material.SetColor("_HighlightColor", Color.Lerp(color, VectrStyleTokens.BoneWhite, 0.38f));
            }

            if (material.HasProperty("_Steps"))
            {
                material.SetFloat("_Steps", 2f);
            }

            if (material.HasProperty("_RimThreshold"))
            {
                material.SetFloat("_RimThreshold", 0.78f);
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

        private Material CreateParticleMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Mobile/Particles/Alpha Blended");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
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

                float bestSqrDistance = float.MaxValue;
                float bestRouteDistance = 0f;
                Vector2 target = new Vector2(worldPosition.x, worldPosition.z);
                for (int i = 0; i < samples.Length - 1; i++)
                {
                    Vector2 start = new Vector2(samples[i].x, samples[i].z);
                    Vector2 end = new Vector2(samples[i + 1].x, samples[i + 1].z);
                    Vector2 segment = end - start;
                    float segmentSqrLength = segment.sqrMagnitude;
                    if (segmentSqrLength < 0.001f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(Vector2.Dot(target - start, segment) / segmentSqrLength);
                    Vector2 closest = start + segment * t;
                    float sqrDistance = Vector2.SqrMagnitude(target - closest);
                    if (sqrDistance < bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        float segmentLength = cumulativeDistances[i + 1] - cumulativeDistances[i];
                        bestRouteDistance = cumulativeDistances[i] + segmentLength * t;
                    }
                }

                return totalLength > 0.01f ? Mathf.Repeat(bestRouteDistance, totalLength) : bestRouteDistance;
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

        private readonly struct GridSlot
        {
            public readonly float distance;
            public readonly Vector3 routePosition;
            public readonly Vector3 position;
            public readonly Quaternion rotation;
            public readonly float laneOffset;

            public GridSlot(float distance, Vector3 routePosition, Vector3 position, Quaternion rotation, float laneOffset)
            {
                this.distance = distance;
                this.routePosition = routePosition;
                this.position = position;
                this.rotation = rotation;
                this.laneOffset = laneOffset;
            }
        }
    }
}
