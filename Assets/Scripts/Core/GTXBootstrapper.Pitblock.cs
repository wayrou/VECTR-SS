using System.Collections.Generic;
using GTX.Progression;
using GTX.UI;
using GTX.Vehicle;
using GTX.Visuals;
using UnityEngine;

namespace GTX.Core
{
    public sealed partial class GTXBootstrapper
    {
        private enum PitblockNodeKind
        {
            GarageTerminal,
            RaceBoard,
            ModuleBench,
            PaintBooth,
            ScrapCounter,
            VehicleBay
        }

        private sealed class PitblockNode
        {
            public PitblockNodeKind kind;
            public string name;
            public string prompt;
            public string bark;
            public Vector3 position;
            public float radius = 2.4f;
        }

        private Transform pitblockRoot;
        private Transform pitblockFootRoot;
        private CharacterController pitblockWalker;
        private Camera pitblockCamera;
        private readonly List<PitblockNode> pitblockNodes = new List<PitblockNode>();
        private Vector3 pitblockFootSpawn;
        private Vector3 pitblockFootVelocity;
        private Vector3 pitblockFootPlanarVelocity;
        private bool pitblockInVehicle;
        private bool pitblockScrapUiOpen;
        private bool pitblockPaintUiOpen;
        private string pitblockFeedback = "Welcome to The Pitblock.";
        private float pitPopHoldSeconds;
        private float pitblockPromptPulse;

        private const float PitPopRequiredSeconds = 1.5f;
        private const float PitblockCharacterScale = 0.58f;
        private const float PitblockWalkSpeed = 5.6f;
        private const float PitblockRunSpeed = 8.8f;
        private const float PitblockSprintSpeed = 11.8f;
        private const float PitblockJumpVelocity = 5.8f;
        private const float PitblockGravity = -22f;
        private const float PitblockAirControl = 0.5f;
        private const float PitblockGroundProbeDistance = 0.28f;

        private void EnterPitblock()
        {
            ResumeRaceTime();
            ClearSession();
            EnsureProgressionState();
            selectedMap = VectorSSCatalog.GetMap(playerProfile.selectedMap);
            selectedVehicle = VectorSSCatalog.GetVehicle(playerProfile.selectedVehicle);
            ApplyMapPalette(selectedMap);
            ApplyVehiclePalette(selectedVehicle);

            if (pitblockRoot == null)
            {
                BuildPitblock();
            }
            else if (activePlayer.root != null)
            {
                activeModuleController = activePlayer.moduleController;
                ConfigureHud(activePlayer);
                hasActivePlayer = true;
            }

            screen = VectorSSScreen.Pitblock;
            menuFocusIndex = 0;
            pitblockScrapUiOpen = false;
            pitblockPaintUiOpen = false;
            ExitPitblockVehicle(false);
            SetRaceHudVisible(false);
        }

        private void ClearPitblock()
        {
            pitblockNodes.Clear();
            pitblockFootRoot = null;
            pitblockWalker = null;
            pitblockCamera = null;
            pitblockInVehicle = false;
            pitblockScrapUiOpen = false;
            pitblockPaintUiOpen = false;
            pitPopHoldSeconds = 0f;

            if (pitblockRoot != null)
            {
                Destroy(pitblockRoot.gameObject);
                pitblockRoot = null;
            }
        }

        private void BuildPitblock()
        {
            pitblockRoot = new GameObject("Pitblock_Hub Runtime Scene").transform;
            pitblockRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            BuildPitblockGarage();
            BuildPitblockOutsideTestArea();
            BuildPitblockWalker();
            BuildPitblockVehicleBay();
            BuildPitblockCamera();
        }

        private void BuildPitblockGarage()
        {
            CreateChamferedBox("Pitblock Garage Floor", pitblockRoot, new Vector3(0f, -0.08f, 0f), Quaternion.identity, new Vector3(36f, 0.16f, 28f), pitFloorMaterial, true, 0.03f);
            CreateChamferedBox("Pitblock Back Wall", pitblockRoot, new Vector3(0f, 3.2f, 14.4f), Quaternion.identity, new Vector3(36f, 6.4f, 0.45f), pitPropMaterial, true, 0.04f);
            CreateChamferedBox("Pitblock Left Wall", pitblockRoot, new Vector3(-18.2f, 3.2f, 0f), Quaternion.identity, new Vector3(0.45f, 6.4f, 28f), pitPropMaterial, true, 0.04f);
            CreateChamferedBox("Pitblock Right Half Wall", pitblockRoot, new Vector3(18.2f, 2.2f, 5.4f), Quaternion.identity, new Vector3(0.45f, 4.4f, 17f), pitPropMaterial, true, 0.04f);
            CreateChamferedBox("Pitblock Overpass Beam", pitblockRoot, new Vector3(0f, 7.4f, -10.8f), Quaternion.identity, new Vector3(42f, 1.2f, 3.2f), inkMaterial, true, 0.08f);
            CreateChamferedBox("Pitblock Painted Floor Arrow", pitblockRoot, new Vector3(7.6f, 0.02f, -8.2f), Quaternion.Euler(0f, 18f, 0f), new Vector3(7f, 0.06f, 1.2f), trackMarkerBlueMaterial, false, 0.02f);
            CreateChamferedBox("Pitblock Vehicle Bay Platform", pitblockRoot, new Vector3(8f, 0.12f, 4f), Quaternion.identity, new Vector3(9.2f, 0.28f, 7.4f), trackMarkerMaterial, true, 0.04f);
            AddPitblockSign("THE PITBLOCK", new Vector3(0f, 5.4f, 14.04f), Quaternion.Euler(0f, 180f, 0f), 1.15f, VectrStyleTokens.ElectricCyan);

            AddPitblockNode(PitblockNodeKind.GarageTerminal, "Garage Terminal", "Open Garage", "Rook: Keep the build tight and the bolts tighter.", new Vector3(-13.2f, 0f, 8.7f), VectrStyleTokens.ElectricCyan);
            AddPitblockNode(PitblockNodeKind.RaceBoard, "Race Board", "Open Race Setup", "Calder: Pick a track, pick a machine, then make noise.", new Vector3(-5.6f, 0f, 9.5f), VectrStyleTokens.HotMagenta);
            AddPitblockNode(PitblockNodeKind.ModuleBench, "Module Bench", "Open Modules / HUD", "Mina: DashGrid wires are behaving. Mostly.", new Vector3(-14f, 0f, -2.5f), VectrStyleTokens.AcidYellowGreen);
            AddPitblockNode(PitblockNodeKind.PaintBooth, "Paint Booth", "Open Paint", "Jax: Two colors, one attitude. That is enough for now.", new Vector3(-4.8f, 0f, -7.2f), VectrStyleTokens.SafetyOrange);
            AddPitblockNode(PitblockNodeKind.ScrapCounter, "Scrap Counter", "Convert Scrap Cubes", "Vee: Scrap cubes in, real materials out.", new Vector3(13.2f, 0f, 10f), VectrStyleTokens.SignalRed);

            CreateResourceBin("Metal Bin", new Vector3(10.8f, 0.55f, 12.4f), inkMaterial);
            CreateResourceBin("Plastic Bin", new Vector3(13.2f, 0.55f, 12.4f), trackMarkerBlueMaterial);
            CreateResourceBin("Rubber Bin", new Vector3(15.6f, 0.55f, 12.4f), wheelMaterial);
            CreateScrapPile(new Vector3(15.2f, 0.25f, 8.4f));

            for (int i = 0; i < 6; i++)
            {
                float x = -16f + i * 1.2f;
                CreateChamferedBox("Pitblock Tire Stack " + i, pitblockRoot, new Vector3(x, 0.45f, -12f), Quaternion.identity, new Vector3(0.9f, 0.9f + (i % 3) * 0.34f, 0.9f), wheelMaterial, true, 0.12f);
            }

            for (int i = 0; i < 5; i++)
            {
                CreateChamferedBox("Pitblock Tool Rack " + i, pitblockRoot, new Vector3(-17.7f, 1.2f + i * 0.45f, 4f - i * 1.8f), Quaternion.identity, new Vector3(0.22f, 0.12f, 1.35f), i % 2 == 0 ? trackMarkerMaterial : inkMaterial, false, 0.03f);
            }
        }

        private void BuildPitblockOutsideTestArea()
        {
            CreateChamferedBox("Pitblock Exit Ramp", pitblockRoot, new Vector3(8f, 0.04f, -18f), Quaternion.identity, new Vector3(10f, 0.16f, 12f), roadMaterial, true, 0.02f);
            CreateChamferedBox("Pitblock Door Header", pitblockRoot, new Vector3(8f, 4.4f, -13.8f), Quaternion.identity, new Vector3(11f, 1f, 0.45f), inkMaterial, true, 0.06f);
            AddPitblockSign("TEST DRIVE", new Vector3(8f, 3.6f, -13.54f), Quaternion.identity, 0.62f, VectrStyleTokens.SafetyOrange);

            CreateChamferedBox("Pitblock Special Stage Connector", pitblockRoot, new Vector3(8f, -0.04f, -42f), Quaternion.identity, new Vector3(13f, 0.12f, 48f), roadMaterial, true, 0.02f);
            CreateChamferedBox("Pitblock Connector Left Rail", pitblockRoot, new Vector3(0.9f, 0.7f, -42f), Quaternion.identity, new Vector3(0.62f, 1.25f, 48f), barrierMaterial, true, 0.06f);
            CreateChamferedBox("Pitblock Connector Right Rail", pitblockRoot, new Vector3(15.1f, 0.7f, -42f), Quaternion.identity, new Vector3(0.62f, 1.25f, 48f), barrierMaterial, true, 0.06f);

            VectorSSMapDefinition previousMap = selectedMap;
            VectorSSMapDefinition specialStage = VectorSSCatalog.GetMap(VectorSSMapId.SpecialStage);
            if (specialStage != null)
            {
                ApplyMapPalette(specialStage);
                Transform stageRoot = new GameObject("Pitblock Outside Special Stage Test Area").transform;
                stageRoot.SetParent(pitblockRoot, false);
                stageRoot.localPosition = new Vector3(8f, 0f, -105f);
                stageRoot.localRotation = Quaternion.identity;
                BuildTrack(stageRoot, specialStage);
                if (previousMap != null)
                {
                    ApplyMapPalette(previousMap);
                }
            }
        }

        private void BuildPitblockWalker()
        {
            pitblockFootRoot = new GameObject("Pitblock Chibi Driver").transform;
            pitblockFootRoot.SetParent(pitblockRoot, false);
            pitblockFootSpawn = new Vector3(-8f, 0.12f, 2f);
            pitblockFootRoot.position = pitblockFootSpawn;
            pitblockWalker = pitblockFootRoot.gameObject.AddComponent<CharacterController>();
            pitblockWalker.height = 1.36f;
            pitblockWalker.radius = 0.27f;
            pitblockWalker.center = new Vector3(0f, 0.68f, 0f);
            pitblockWalker.stepOffset = 0.32f;
            pitblockWalker.slopeLimit = 52f;
            pitblockWalker.skinWidth = 0.08f;
            BuildChibiFigure(pitblockFootRoot, "Driver", playerMaterial, playerSecondaryMaterial, true);
        }

        private void BuildPitblockVehicleBay()
        {
            Transform resetPoint = new GameObject("Pitblock Vehicle Bay Spawn").transform;
            resetPoint.SetParent(pitblockRoot, false);
            resetPoint.position = new Vector3(8f, 0.42f, 4f);
            resetPoint.rotation = Quaternion.Euler(0f, 180f, 0f);

            activePlayer = CreatePlayerMachine(resetPoint, selectedVehicle);
            activePlayer.root.transform.SetParent(pitblockRoot, true);
            activeModuleController = activePlayer.moduleController;
            activePlayer.vehicle.InputLocked = true;
            activePlayer.vehicle.AutomaticTransmission = playerProfile.tuning.automaticTransmission;
            activePlayer.visuals.Configure(activePlayer.vehicle, activePlayer.flowState, activePlayer.effects, activePlayer.boostTrail);
            activePlayer.visuals.SetBoostTrailStyle(VectrStyleTokens.WithAlpha(VectrStyleTokens.BoostTrail(selectedVehicle.id), 0.86f), VectrStyleTokens.WithAlpha(selectedVehicle.isBike ? VectrStyleTokens.AcidYellowGreen : VectrStyleTokens.HotMagenta, 0.96f), selectedVehicle.isBike);
            ConfigureHud(activePlayer);
            hasActivePlayer = true;
            SetRaceHudVisible(false);

            AddPitblockNode(PitblockNodeKind.VehicleBay, "Vehicle Bay", "Enter Vehicle", "Current machine staged. Press interact to hop in.", activePlayer.root.transform.position, VectrStyleTokens.ElectricCyan);
            AddPitblockSign("VEHICLE BAY", new Vector3(8f, 2.7f, 9.1f), Quaternion.Euler(0f, 180f, 0f), 0.68f, VectrStyleTokens.ElectricCyan);
        }

        private void BuildPitblockCamera()
        {
            pitblockCamera = ConfigureCamera(activePlayer);
            GTXCameraRig rig = pitblockCamera.GetComponent<GTXCameraRig>();
            if (rig != null)
            {
                rig.Configure(pitblockFootRoot, null, activePlayer.flowState);
            }
        }

        private void UpdatePitblock()
        {
            if (pitblockRoot == null)
            {
                EnterPitblock();
                return;
            }

            if (pitblockInVehicle)
            {
                UpdatePitblockVehicleMode();
                return;
            }

            SetRaceHudVisible(false);
            UpdatePitblockWalker();
            UpdatePitblockInteraction();
        }

        private void UpdatePitblockWalker()
        {
            if (pitblockWalker == null || pitblockScrapUiOpen || pitblockPaintUiOpen)
            {
                return;
            }

            if (pitblockFootRoot.position.y < -10f)
            {
                ResetPitblockWalker(pitblockFootSpawn);
                return;
            }

            Vector2 moveInput = ReadPitblockMoveInput();
            bool sprint = Input.GetKey(KeyCode.LeftShift) || GTXInput.Button(4);
            bool walk = Input.GetKey(KeyCode.LeftControl) || GTXInput.Button(5);
            float speed = sprint ? PitblockSprintSpeed : walk ? PitblockWalkSpeed : PitblockRunSpeed;
            bool grounded = PitblockWalkerGrounded();
            Vector3 desiredMove = PitblockCameraRelativeMove(moveInput) * speed;
            float response = grounded ? 30f : 9f * PitblockAirControl;
            pitblockFootPlanarVelocity = Vector3.Lerp(pitblockFootPlanarVelocity, desiredMove, 1f - Mathf.Exp(-response * Time.deltaTime));

            if (desiredMove.sqrMagnitude > 0.04f)
            {
                Vector3 facing = desiredMove;
                facing.y = 0f;
                pitblockFootRoot.rotation = Quaternion.Slerp(pitblockFootRoot.rotation, Quaternion.LookRotation(facing.normalized, Vector3.up), 1f - Mathf.Exp(-24f * Time.deltaTime));
            }

            if (grounded && pitblockFootVelocity.y < 0f)
            {
                pitblockFootVelocity.y = -2.2f;
            }

            if ((Input.GetKeyDown(KeyCode.Space) || GTXInput.ButtonDown(3)) && grounded)
            {
                pitblockFootVelocity.y = PitblockJumpVelocity;
            }

            pitblockFootVelocity.y += PitblockGravity * Time.deltaTime;
            Vector3 motion = pitblockFootPlanarVelocity + Vector3.up * pitblockFootVelocity.y;
            CollisionFlags flags = pitblockWalker.Move(motion * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && pitblockFootVelocity.y > 0f)
            {
                pitblockFootVelocity.y = 0f;
            }
        }

        private Vector2 ReadPitblockMoveInput()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                y += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                y -= 1f;
            }

            if (Mathf.Approximately(x, 0f))
            {
                x = GTXInput.Axis("Horizontal");
            }

            if (Mathf.Approximately(y, 0f))
            {
                y = GTXInput.Axis("Vertical");
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private Vector3 PitblockCameraRelativeMove(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Transform reference = pitblockCamera != null ? pitblockCamera.transform : null;
            Vector3 forward = reference != null ? reference.forward : Vector3.forward;
            Vector3 right = reference != null ? reference.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.right;
            }

            return Vector3.ClampMagnitude(forward.normalized * input.y + right.normalized * input.x, 1f);
        }

        private bool PitblockWalkerGrounded()
        {
            if (pitblockWalker == null)
            {
                return false;
            }

            if (pitblockWalker.isGrounded)
            {
                return true;
            }

            Vector3 center = pitblockFootRoot.TransformPoint(pitblockWalker.center);
            float radius = Mathf.Max(0.08f, pitblockWalker.radius * 0.86f);
            float probeLength = pitblockWalker.height * 0.5f - pitblockWalker.radius + PitblockGroundProbeDistance;
            return Physics.SphereCast(center, radius, Vector3.down, out _, probeLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void ResetPitblockWalker(Vector3 position)
        {
            if (pitblockWalker == null)
            {
                return;
            }

            pitblockWalker.enabled = false;
            pitblockFootRoot.position = position;
            pitblockWalker.enabled = true;
            pitblockFootVelocity = Vector3.zero;
            pitblockFootPlanarVelocity = Vector3.zero;
        }

        private void UpdatePitblockInteraction()
        {
            PitblockNode node = NearestPitblockNode();
            if (node == null)
            {
                return;
            }

            pitblockPromptPulse += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.E) || GTXInput.ButtonDown(1))
            {
                ActivatePitblockNode(node);
            }
        }

        private void UpdatePitblockVehicleMode()
        {
            if (activePlayer.vehicle != null)
            {
                activePlayer.vehicle.AutomaticTransmission = playerProfile.tuning.automaticTransmission;
                activePlayer.vehicle.InputLocked = false;
            }

            UpdateModuleHud();
            bool pitPopHeld = PitPopInputHeld();
            if (pitPopHeld)
            {
                pitPopHoldSeconds = Mathf.Min(PitPopRequiredSeconds, pitPopHoldSeconds + Time.deltaTime);
                if (pitPopHoldSeconds >= PitPopRequiredSeconds)
                {
                    ExitPitblockVehicle(true);
                }
            }
            else
            {
                pitPopHoldSeconds = 0f;
            }
        }

        private bool PitPopInputHeld()
        {
            bool keyboard = Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.S);
            VehicleInputState input = activePlayer.vehicle != null ? activePlayer.vehicle.CurrentInput : default(VehicleInputState);
            bool vehicleInput = input.handbrake && input.brake > 0.45f;
            float stickBack = -GTXInput.Axis("Vertical");
            bool gamepad = (GTXInput.Button(0) || input.handbrake) && stickBack > 0.45f;
            return keyboard || vehicleInput || gamepad;
        }

        private void ActivatePitblockNode(PitblockNode node)
        {
            pitblockFeedback = node.bark;
            switch (node.kind)
            {
                case PitblockNodeKind.GarageTerminal:
                    activeGarageTab = GarageTab.Build;
                    garageMessage = "Opened from the Garage Terminal.";
                    screen = VectorSSScreen.Garage;
                    menuFocusIndex = 0;
                    break;
                case PitblockNodeKind.RaceBoard:
                    VectorSSSaveSystem.Save(playerProfile);
                    screen = VectorSSScreen.MapSelect;
                    menuFocusIndex = 0;
                    break;
                case PitblockNodeKind.ModuleBench:
                    activeGarageTab = GarageTab.Modules;
                    garageMessage = "Module Bench linked to cockpit modules and HUD layout.";
                    screen = VectorSSScreen.Garage;
                    menuFocusIndex = 1;
                    break;
                case PitblockNodeKind.PaintBooth:
                    pitblockPaintUiOpen = true;
                    break;
                case PitblockNodeKind.ScrapCounter:
                    pitblockScrapUiOpen = true;
                    break;
                case PitblockNodeKind.VehicleBay:
                    EnterPitblockVehicle();
                    break;
            }
        }

        private void EnterPitblockVehicle()
        {
            if (activePlayer.vehicle == null)
            {
                return;
            }

            pitblockInVehicle = true;
            pitPopHoldSeconds = 0f;
            pitblockFootRoot.gameObject.SetActive(false);
            activePlayer.vehicle.InputLocked = false;
            GTXCameraRig rig = pitblockCamera != null ? pitblockCamera.GetComponent<GTXCameraRig>() : null;
            if (rig != null)
            {
                rig.Configure(activePlayer.root.transform, activePlayer.body, activePlayer.flowState);
            }

            SetRaceHudVisible(true);
            pitblockFeedback = "Vehicle controls active. Hold Space + S for 1.5 seconds to Pit Pop.";
        }

        private void ExitPitblockVehicle(bool stylizedPop)
        {
            if (activePlayer.vehicle != null)
            {
                activePlayer.vehicle.InputLocked = true;
            }

            if (activePlayer.body != null)
            {
                activePlayer.body.velocity *= stylizedPop ? 0.18f : 0f;
                activePlayer.body.angularVelocity *= stylizedPop ? 0.18f : 0f;
            }

            Vector3 spawn = activePlayer.root != null ? activePlayer.root.transform.position - activePlayer.root.transform.right * 2.4f + Vector3.up * (stylizedPop ? 1.05f : 0.18f) : new Vector3(-8f, 0.1f, 2f);
            pitblockFootRoot.gameObject.SetActive(true);
            pitblockWalker.enabled = false;
            pitblockFootRoot.position = spawn;
            pitblockFootRoot.rotation = activePlayer.root != null ? Quaternion.LookRotation(activePlayer.root.transform.forward, Vector3.up) : Quaternion.identity;
            pitblockWalker.enabled = true;
            pitblockFootVelocity = stylizedPop ? new Vector3(0f, 4.2f, 0f) : Vector3.zero;
            pitblockFootPlanarVelocity = activePlayer.body != null ? Vector3.ProjectOnPlane(activePlayer.body.velocity, Vector3.up) * 0.12f : Vector3.zero;
            pitblockInVehicle = false;
            pitPopHoldSeconds = 0f;

            GTXCameraRig rig = pitblockCamera != null ? pitblockCamera.GetComponent<GTXCameraRig>() : null;
            if (rig != null)
            {
                rig.Configure(pitblockFootRoot, null, activePlayer.flowState);
            }

            SetRaceHudVisible(false);
            if (activeModuleHud != null)
            {
                activeModuleHud.Clear();
            }

            pitblockFeedback = stylizedPop ? "Pit Pop! Back on foot." : pitblockFeedback;
        }

        private void DrawPitblockOverlay()
        {
            if (pitblockRoot == null)
            {
                return;
            }

            PitblockNode node = !pitblockInVehicle ? NearestPitblockNode() : null;
            if (node != null && !pitblockScrapUiOpen && !pitblockPaintUiOpen)
            {
                Rect prompt = new Rect(Screen.width * 0.5f - 230f, Screen.height - 112f, 460f, 72f);
                GUI.Box(prompt, string.Empty, panelStyle);
                GUI.Label(new Rect(prompt.x + 18f, prompt.y + 10f, prompt.width - 36f, 26f), node.name.ToUpperInvariant(), headerStyle);
                GUI.Label(new Rect(prompt.x + 18f, prompt.y + 40f, prompt.width - 36f, 24f), "E / confirm: " + node.prompt, bodyStyle);
            }

            Rect status = new Rect(24f, 22f, 430f, 86f);
            GUI.Box(status, string.Empty, panelStyle);
            GUI.Label(new Rect(status.x + 18f, status.y + 10f, status.width - 36f, 28f), "THE PITBLOCK", headerStyle);
            GUI.Label(new Rect(status.x + 18f, status.y + 42f, status.width - 36f, 26f), pitblockInVehicle ? "Vehicle mode / Space + S: Pit Pop" : "WASD move / Ctrl walk / Shift sprint / Space jump / E interact", smallStyle);

            if (!string.IsNullOrEmpty(pitblockFeedback))
            {
                GUI.Label(new Rect(28f, 114f, 720f, 28f), pitblockFeedback, bodyStyle);
            }

            if (pitblockInVehicle && pitPopHoldSeconds > 0f)
            {
                DrawPitPopProgress();
            }

            if (pitblockScrapUiOpen)
            {
                DrawPitblockScrapCounter();
            }

            if (pitblockPaintUiOpen)
            {
                DrawPitblockPaintBooth();
            }
        }

        private void DrawPitPopProgress()
        {
            Rect rect = new Rect(Screen.width * 0.5f - 190f, Screen.height - 168f, 380f, 34f);
            GUI.Box(rect, string.Empty, panelStyle);
            float fill = Mathf.Clamp01(pitPopHoldSeconds / PitPopRequiredSeconds);
            Color old = GUI.color;
            GUI.color = VectrStyleTokens.SafetyOrange;
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 6f, (rect.width - 12f) * fill, rect.height - 12f), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(rect, "PIT POP " + Mathf.CeilToInt(PitPopRequiredSeconds - pitPopHoldSeconds).ToString("0"), headerStyle);
        }

        private void DrawPitblockScrapCounter()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 310f, Screen.height * 0.5f - 190f, 620f, 380f);
            GUI.Box(panel, string.Empty, panelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 42f), "SCRAP COUNTER", titleStyle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 82f, panel.width - 64f, 28f), "Scrap Cubes " + playerProfile.scrapCubes + "     " + playerProfile.resources, bodyStyle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 118f, panel.width - 64f, 24f), "1 Scrap Cube converts into 10 of the selected resource.", smallStyle);

            if (GUI.Button(new Rect(panel.x + 36f, panel.y + 168f, 170f, 48f), "+10 Metal", buttonStyle))
            {
                ConvertScrap(VectorSSResourceType.Metal);
            }

            if (GUI.Button(new Rect(panel.x + 224f, panel.y + 168f, 170f, 48f), "+10 Plastic", buttonStyle))
            {
                ConvertScrap(VectorSSResourceType.Plastic);
            }

            if (GUI.Button(new Rect(panel.x + 412f, panel.y + 168f, 170f, 48f), "+10 Rubber", buttonStyle))
            {
                ConvertScrap(VectorSSResourceType.Rubber);
            }

            GUI.Label(new Rect(panel.x + 34f, panel.y + 246f, panel.width - 68f, 32f), pitblockFeedback, bodyStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 190f, panel.y + panel.height - 66f, 150f, 42f), "CLOSE", buttonStyle) || Input.GetKeyDown(KeyCode.Escape))
            {
                pitblockScrapUiOpen = false;
            }
        }

        private void DrawPitblockPaintBooth()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 310f, Screen.height * 0.5f - 190f, 620f, 380f);
            GUI.Box(panel, string.Empty, panelStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 42f), "PAINT BOOTH", titleStyle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 82f, panel.width - 64f, 28f), selectedVehicle.fullName, headerStyle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 120f, panel.width - 64f, 24f), "0.1.0 visual options: palette preview and saved outline thickness.", smallStyle);

            DrawColorSwatch(new Rect(panel.x + 36f, panel.y + 164f, 128f, 54f), "Primary", selectedVehicle.bodyColor);
            DrawColorSwatch(new Rect(panel.x + 184f, panel.y + 164f, 128f, 54f), "Accent", selectedVehicle.accentColor);
            DrawColorSwatch(new Rect(panel.x + 332f, panel.y + 164f, 128f, 54f), "Boost", VectrStyleTokens.BoostTrail(selectedVehicle.id));

            GUI.Label(new Rect(panel.x + 36f, panel.y + 246f, 260f, 26f), "Outline Thickness " + playerProfile.tuning.outlineThickness.ToString("0.00"), bodyStyle);
            float outline = GUI.HorizontalSlider(new Rect(panel.x + 300f, panel.y + 254f, 240f, 20f), playerProfile.tuning.outlineThickness, 0.65f, 1.6f);
            if (!Mathf.Approximately(outline, playerProfile.tuning.outlineThickness))
            {
                playerProfile.tuning.outlineThickness = outline;
                VectorSSSaveSystem.Save(playerProfile);
                pitblockFeedback = "Paint Booth saved outline thickness.";
            }

            if (GUI.Button(new Rect(panel.x + panel.width - 190f, panel.y + panel.height - 66f, 150f, 42f), "CLOSE", buttonStyle) || Input.GetKeyDown(KeyCode.Escape))
            {
                pitblockPaintUiOpen = false;
            }
        }

        private void ConvertScrap(VectorSSResourceType type)
        {
            if (playerProfile.scrapCubes <= 0)
            {
                pitblockFeedback = "No Scrap Cubes available.";
                return;
            }

            playerProfile.scrapCubes--;
            switch (type)
            {
                case VectorSSResourceType.Plastic:
                    playerProfile.resources.plastic += 10;
                    pitblockFeedback = "Converted 1 Scrap Cube into 10 Plastic.";
                    break;
                case VectorSSResourceType.Rubber:
                    playerProfile.resources.rubber += 10;
                    pitblockFeedback = "Converted 1 Scrap Cube into 10 Rubber.";
                    break;
                default:
                    playerProfile.resources.metal += 10;
                    pitblockFeedback = "Converted 1 Scrap Cube into 10 Metal.";
                    break;
            }

            VectorSSSaveSystem.Save(playerProfile);
        }

        private PitblockNode NearestPitblockNode()
        {
            if (pitblockFootRoot == null || pitblockNodes.Count == 0)
            {
                return null;
            }

            PitblockNode best = null;
            float bestDistance = float.MaxValue;
            Vector3 point = pitblockFootRoot.position;
            for (int i = 0; i < pitblockNodes.Count; i++)
            {
                PitblockNode node = pitblockNodes[i];
                Vector3 nodePosition = node.kind == PitblockNodeKind.VehicleBay && activePlayer.root != null ? activePlayer.root.transform.position : node.position;
                float distance = Vector3.Distance(point, nodePosition);
                if (distance <= node.radius && distance < bestDistance)
                {
                    best = node;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void AddPitblockNode(PitblockNodeKind kind, string name, string prompt, string bark, Vector3 position, Color color)
        {
            PitblockNode node = new PitblockNode
            {
                kind = kind,
                name = name,
                prompt = prompt,
                bark = bark,
                position = position
            };
            pitblockNodes.Add(node);

            Transform root = new GameObject("Pitblock Node " + name).transform;
            root.SetParent(pitblockRoot, false);
            root.localPosition = position;
            CreateChamferedBox(name + " Base", root, new Vector3(0f, 0.32f, 0f), Quaternion.identity, new Vector3(2.2f, 0.64f, 1.5f), inkMaterial, true, 0.06f);
            CreateChamferedBox(name + " Face", root, new Vector3(0f, 1.28f, 0f), Quaternion.identity, new Vector3(2.6f, 1.35f, 0.32f), CreateMaterial(name + " Node Material", color, true), true, 0.05f);
            AddPitblockSign(name.ToUpperInvariant(), position + new Vector3(0f, 2.3f, -0.22f), Quaternion.identity, 0.34f, color);
            BuildChibiFigure(root, name + " NPC", CreateMaterial(name + " NPC Body", color, true), pitPropMaterial, false).localPosition = new Vector3(1.8f, 0f, 0.2f);
        }

        private Transform BuildChibiFigure(Transform parent, string name, Material body, Material accent, bool playerScale)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent, false);
            float scale = playerScale ? PitblockCharacterScale : PitblockCharacterScale * 0.82f;
            Material skin = CreateMaterial(name + " Warm Face Material", playerScale ? new Color(0.95f, 0.64f, 0.48f, 1f) : new Color(0.86f, 0.78f, 0.66f, 1f), true);
            Material hair = CreateMaterial(name + " Chunk Hair Material", playerScale ? new Color(0.95f, 0.62f, 0.16f, 1f) : VectrStyleTokens.ShadowFor(body.color), true);
            Material suit = CreateMaterial(name + " Suit Material", playerScale ? new Color(0.12f, 0.15f, 0.25f, 1f) : body.color, true);
            Material scarf = CreateMaterial(name + " Scarf Material", playerScale ? new Color(0.035f, 0.035f, 0.04f, 1f) : VectrStyleTokens.InkBlack, true);
            Material trim = CreateMaterial(name + " Cyan Trim Material", playerScale ? VectrStyleTokens.ElectricCyan : accent.color, true);

            CreateOutlinedPrimitive(name + " Big Faceted Head", root, PrimitiveType.Sphere, new Vector3(0f, 1.58f * scale, 0f), new Vector3(1.04f, 0.94f, 0.88f) * scale, skin);
            CreateOutlinedPrimitive(name + " Face Plate", root, PrimitiveType.Cube, new Vector3(0f, 1.55f * scale, 0.43f * scale), new Vector3(0.62f, 0.32f, 0.08f) * scale, skin);
            CreateOutlinedPrimitive(name + " Left Eye", root, PrimitiveType.Cube, new Vector3(-0.19f * scale, 1.6f * scale, 0.49f * scale), new Vector3(0.12f, 0.19f, 0.045f) * scale, trim);
            CreateOutlinedPrimitive(name + " Right Eye", root, PrimitiveType.Cube, new Vector3(0.19f * scale, 1.6f * scale, 0.49f * scale), new Vector3(0.12f, 0.19f, 0.045f) * scale, trim);

            CreateOutlinedPrimitive(name + " Hair Cap", root, PrimitiveType.Cube, new Vector3(0f, 2.0f * scale, -0.04f), new Vector3(0.95f, 0.22f, 0.7f) * scale, hair);
            CreateHairSpike(name + " Front Bang A", root, new Vector3(-0.26f, 1.9f, 0.38f), new Vector3(0.24f, 0.58f, 0.16f), new Vector3(18f, 0f, -18f), hair, scale);
            CreateHairSpike(name + " Front Bang B", root, new Vector3(0.02f, 1.92f, 0.4f), new Vector3(0.24f, 0.64f, 0.16f), new Vector3(12f, 0f, 5f), hair, scale);
            CreateHairSpike(name + " Front Bang C", root, new Vector3(0.31f, 1.88f, 0.36f), new Vector3(0.22f, 0.5f, 0.15f), new Vector3(18f, 0f, 20f), hair, scale);
            CreateHairSpike(name + " Side Lock L", root, new Vector3(-0.59f, 1.48f, 0.08f), new Vector3(0.2f, 0.62f, 0.18f), new Vector3(0f, 0f, 18f), hair, scale);
            CreateHairSpike(name + " Side Lock R", root, new Vector3(0.59f, 1.48f, 0.08f), new Vector3(0.2f, 0.62f, 0.18f), new Vector3(0f, 0f, -18f), hair, scale);
            CreateHairSpike(name + " Back Cowlick", root, new Vector3(0.18f, 2.22f, -0.22f), new Vector3(0.15f, 0.54f, 0.12f), new Vector3(-34f, 0f, -24f), hair, scale);

            CreateOutlinedPrimitive(name + " Scarf Collar", root, PrimitiveType.Cube, new Vector3(0f, 1.02f * scale, 0.03f), new Vector3(0.86f, 0.24f, 0.54f) * scale, scarf);
            CreateOutlinedPrimitive(name + " Small Torso", root, PrimitiveType.Cube, new Vector3(0f, 0.76f * scale, 0f), new Vector3(0.58f, 0.54f, 0.38f) * scale, suit);
            CreateOutlinedPrimitive(name + " Chest Plate", root, PrimitiveType.Cube, new Vector3(0f, 0.8f * scale, 0.23f * scale), new Vector3(0.36f, 0.4f, 0.07f) * scale, accent);
            CreateOutlinedPrimitive(name + " Coat Skirt", root, PrimitiveType.Cube, new Vector3(0f, 0.48f * scale, -0.02f), new Vector3(0.78f, 0.28f, 0.46f) * scale, suit);
            CreateOutlinedPrimitive(name + " Shorts", root, PrimitiveType.Cube, new Vector3(0f, 0.34f * scale, 0.06f), new Vector3(0.68f, 0.22f, 0.5f) * scale, suit);

            CreateOutlinedPrimitive(name + " Left Arm", root, PrimitiveType.Cube, new Vector3(-0.55f * scale, 0.82f * scale, 0.02f), new Vector3(0.28f, 0.55f, 0.28f) * scale, suit);
            CreateOutlinedPrimitive(name + " Right Arm", root, PrimitiveType.Cube, new Vector3(0.55f * scale, 0.82f * scale, 0.02f), new Vector3(0.28f, 0.55f, 0.28f) * scale, suit);
            CreateOutlinedPrimitive(name + " Left Cuff", root, PrimitiveType.Cube, new Vector3(-0.58f * scale, 0.6f * scale, 0.03f), new Vector3(0.38f, 0.22f, 0.34f) * scale, accent);
            CreateOutlinedPrimitive(name + " Right Cuff", root, PrimitiveType.Cube, new Vector3(0.58f * scale, 0.6f * scale, 0.03f), new Vector3(0.38f, 0.22f, 0.34f) * scale, accent);
            CreateOutlinedPrimitive(name + " Glove L", root, PrimitiveType.Sphere, new Vector3(-0.72f * scale, 0.45f * scale, 0.04f), new Vector3(0.32f, 0.25f, 0.25f) * scale, scarf);
            CreateOutlinedPrimitive(name + " Glove R", root, PrimitiveType.Sphere, new Vector3(0.72f * scale, 0.45f * scale, 0.04f), new Vector3(0.32f, 0.25f, 0.25f) * scale, scarf);

            CreateOutlinedPrimitive(name + " Leg L", root, PrimitiveType.Cube, new Vector3(-0.21f * scale, 0.17f * scale, 0.02f), new Vector3(0.24f, 0.34f, 0.24f) * scale, suit);
            CreateOutlinedPrimitive(name + " Leg R", root, PrimitiveType.Cube, new Vector3(0.21f * scale, 0.17f * scale, 0.02f), new Vector3(0.24f, 0.34f, 0.24f) * scale, suit);
            CreateOutlinedPrimitive(name + " Boot L", root, PrimitiveType.Cube, new Vector3(-0.24f * scale, 0.02f * scale, 0.13f), new Vector3(0.42f, 0.34f, 0.62f) * scale, wheelMaterial);
            CreateOutlinedPrimitive(name + " Boot R", root, PrimitiveType.Cube, new Vector3(0.24f * scale, 0.02f * scale, 0.13f), new Vector3(0.42f, 0.34f, 0.62f) * scale, wheelMaterial);
            CreateOutlinedPrimitive(name + " Boot Trim L", root, PrimitiveType.Cube, new Vector3(-0.24f * scale, 0.21f * scale, 0.16f), new Vector3(0.44f, 0.06f, 0.58f) * scale, trim);
            CreateOutlinedPrimitive(name + " Boot Trim R", root, PrimitiveType.Cube, new Vector3(0.24f * scale, 0.21f * scale, 0.16f), new Vector3(0.44f, 0.06f, 0.58f) * scale, trim);
            return root;
        }

        private void CreateHairSpike(string name, Transform root, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material, float scale)
        {
            CreateOutlinedPrimitive(name, root, PrimitiveType.Cube, localPosition * scale, localScale * scale, material).transform.localRotation = Quaternion.Euler(localEuler);
            Transform outline = root.Find(name + " Outline");
            if (outline != null)
            {
                outline.localRotation = Quaternion.Euler(localEuler);
            }
        }

        private GameObject CreateOutlinedPrimitive(string name, Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject outline = GameObject.CreatePrimitive(type);
            outline.name = name + " Outline";
            outline.transform.SetParent(parent, false);
            outline.transform.localPosition = localPosition;
            outline.transform.localScale = localScale * 1.12f;
            outline.GetComponent<Renderer>().material = outlineMaterial != null ? outlineMaterial : inkMaterial;
            Collider outlineCollider = outline.GetComponent<Collider>();
            if (outlineCollider != null)
            {
                Destroy(outlineCollider);
            }

            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().material = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return part;
        }

        private void CreateResourceBin(string name, Vector3 position, Material material)
        {
            CreateChamferedBox("Pitblock " + name, pitblockRoot, position, Quaternion.identity, new Vector3(1.7f, 1.1f, 1.4f), material, true, 0.06f);
            AddPitblockSign(name.ToUpperInvariant(), position + Vector3.up * 1.12f + Vector3.back * 0.45f, Quaternion.identity, 0.24f, VectrStyleTokens.BoneWhite);
        }

        private void CreateScrapPile(Vector3 position)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3((i % 4) * 0.32f - 0.46f, i / 4 * 0.28f, (i % 2) * 0.34f - 0.18f);
                CreateChamferedBox("Pitblock Scrap Cube " + i, pitblockRoot, position + offset, Quaternion.Euler(0f, i * 19f, i * 7f), new Vector3(0.32f, 0.32f, 0.32f), trackMarkerMaterial, true, 0.04f);
            }
        }

        private void AddPitblockSign(string text, Vector3 position, Quaternion rotation, float size, Color color)
        {
            GameObject sign = new GameObject("Pitblock Sign " + text);
            sign.transform.SetParent(pitblockRoot, false);
            sign.transform.position = position;
            sign.transform.rotation = rotation;
            TextMesh mesh = sign.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.color = color;
            MeshRenderer renderer = sign.GetComponent<MeshRenderer>();
            renderer.material = CreateMaterial("Pitblock Sign Mat " + text, color, false);
        }

        private void DrawColorSwatch(Rect rect, string label, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y - 24f, rect.width, 22f), label, smallStyle);
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Box(rect, string.Empty, cardStyle);
        }
    }
}
