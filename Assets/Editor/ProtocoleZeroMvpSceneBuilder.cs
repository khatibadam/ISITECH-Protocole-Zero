using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProtocoleZero;

namespace ProtocoleZero.EditorTools
{
    public static class ProtocoleZeroMvpSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/01_ProtocoleZero_MVP.unity";

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "01_ProtocoleZero_MVP";

            Material floorMat = MakeMaterial("M_Floor_ISITECH_Beige", new Color(0.68f, 0.62f, 0.52f));
            Material wallMat = MakeMaterial("M_Wall_White", new Color(0.86f, 0.86f, 0.82f));
            Material ceilingMat = MakeMaterial("M_Ceiling_Tiles", new Color(0.78f, 0.78f, 0.74f));
            Material glassMat = MakeMaterial("M_Glass_Corridor", new Color(0.45f, 0.85f, 1f, 0.32f), true);
            Material pillarMat = MakeMaterial("M_Pillar_White", new Color(0.96f, 0.96f, 0.92f));
            Material tableMat = MakeMaterial("M_Table_Grey", new Color(0.36f, 0.37f, 0.38f));
            Material chairMat = MakeMaterial("M_Chair_Blue", new Color(0.05f, 0.23f, 0.75f));
            Material blackMat = MakeMaterial("M_Server_Black", new Color(0.02f, 0.025f, 0.03f));
            Material entityMat = MakeMaterial("M_Entity_Shadow", new Color(0f, 0f, 0f));
            Material redMat = MakeMaterial("M_Feedback_Red", new Color(1f, 0.05f, 0.02f), false, true);
            Material yellowMat = MakeMaterial("M_Feedback_Yellow", new Color(1f, 0.72f, 0.08f), false, true);
            Material cyanMat = MakeMaterial("M_Feedback_Cyan", new Color(0.05f, 0.85f, 1f), false, true);
            Material orangeMat = MakeMaterial("M_Cable_Orange", new Color(1f, 0.38f, 0.05f));
            Material blueMat = MakeMaterial("M_Cable_Blue", new Color(0.05f, 0.28f, 1f));
            Material signMat = MakeMaterial("M_Sign_Green", new Color(0f, 0.5f, 0.22f), false, true);
            Material doorMat = MakeMaterial("M_Door_Dark", new Color(0.18f, 0.16f, 0.14f));
            Material invisibleTriggerMat = MakeMaterial("M_Trigger_Debug_Transparent", new Color(1f, 0f, 0f, 0.08f), true);

            GameObject systemsRoot = Root("00_SYSTEMS");
            GameObject playerRoot = Root("01_PLAYER");
            GameObject levelRoot = Root("02_LEVEL_GREYBOX_ISITECH_N3");
            GameObject gameplayRoot = Root("03_GAMEPLAY");
            GameObject uiRoot = Root("04_DIEGETIC_UI");
            GameObject lightingRoot = Root("05_LIGHTING");

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.1f);
            RenderSettings.fog = false;

            StressDirector stress = systemsRoot.AddComponent<StressDirector>();
            SubtitleManager subtitles = systemsRoot.AddComponent<SubtitleManager>();
            HapticFeedbackRouter haptics = systemsRoot.AddComponent<HapticFeedbackRouter>();
            ComfortSettingsManager comfort = systemsRoot.AddComponent<ComfortSettingsManager>();
            BatteryTimer battery = systemsRoot.AddComponent<BatteryTimer>();
            MusicAnchorController music = systemsRoot.AddComponent<MusicAnchorController>();
            EntityDirector entityDirector = systemsRoot.AddComponent<EntityDirector>();
            ProtocoleZeroGameFlow flow = systemsRoot.AddComponent<ProtocoleZeroGameFlow>();
            MissionTimer mission = systemsRoot.AddComponent<MissionTimer>();

            SetObj(comfort, "stressDirector", stress);
            SetObj(battery, "stressDirector", stress);
            SetObj(battery, "subtitles", subtitles);
            SetObj(music, "stressDirector", stress);
            SetObj(music, "subtitles", subtitles);
            SetObj(mission, "stressDirector", stress);
            SetObj(mission, "subtitles", subtitles);

            GameObject xrRig = InstantiatePrefab("Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab", playerRoot.transform);
            if (xrRig == null)
            {
                xrRig = Root("XR Origin Fallback");
                xrRig.transform.SetParent(playerRoot.transform, false);
            }
            xrRig.name = "XR Origin (VR) + Desktop Fallback";
            TrySetTag(xrRig, "Player");
            xrRig.transform.position = new Vector3(0f, 0f, -1.7f);

            CharacterController controller = xrRig.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = xrRig.AddComponent<CharacterController>();
            }
            controller.height = 1.7f;
            controller.radius = 0.23f;
            controller.center = new Vector3(0f, 0.85f, 0f);

            Camera mainCamera = xrRig.GetComponentInChildren<Camera>(true);
            if (mainCamera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                cameraGo.transform.SetParent(xrRig.transform, false);
                cameraGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                mainCamera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }
            TrySetTag(mainCamera.gameObject, "MainCamera");
            mainCamera.nearClipPlane = 0.03f;
            mainCamera.farClipPlane = 75f;

            SimplePlayerController desktopFallback = xrRig.GetComponent<SimplePlayerController>();
            if (desktopFallback == null)
            {
                desktopFallback = xrRig.AddComponent<SimplePlayerController>();
            }
            SetObj(desktopFallback, "playerCamera", mainCamera);

            GameObject simulator = InstantiatePrefab("Assets/Samples/XR Interaction Toolkit/3.2.1/XR Interaction Simulator/XR Interaction Simulator.prefab", playerRoot.transform);
            if (simulator != null)
            {
                simulator.name = "XR Interaction Simulator";
            }

            BuildArchitecture(levelRoot.transform, floorMat, wallMat, ceilingMat, glassMat, pillarMat, tableMat, chairMat, blackMat, signMat, doorMat);
            BuildLighting(lightingRoot.transform);

            TextMesh subtitleText = Text("Subtitle_WorldText", new Vector3(0f, 2.1f, 1.7f), Quaternion.Euler(18f, 180f, 0f), "", 0.11f, Color.white, uiRoot.transform);
            SetObj(subtitles, "textTarget", subtitleText);

            TextMesh pcScreen = Text("PC_Mars_Screen_Text", new Vector3(-2.4f, 1.22f, -1.78f), Quaternion.Euler(0f, 18f, 0f), "ISITECH PROTOCOLE ZERO", 0.055f, Color.cyan, uiRoot.transform);
            TextMesh wristText = Text("WristAnchor_Text", Vector3.zero, Quaternion.identity, "ANCRE", 0.035f, Color.green, uiRoot.transform);
            Transform wristParent = FindChildByName(xrRig.transform, "Left") ?? mainCamera.transform;
            wristText.transform.SetParent(wristParent, false);
            wristText.transform.localPosition = new Vector3(-0.12f, -0.1f, 0.18f);
            wristText.transform.localRotation = Quaternion.Euler(62f, 180f, 0f);

            WristAnchorUI wristUi = wristText.gameObject.AddComponent<WristAnchorUI>();
            SetObj(wristUi, "wristText", wristText);
            SetObj(wristUi, "stressDirector", stress);
            SetObj(wristUi, "musicAnchor", music);
            SetObj(wristUi, "batteryTimer", battery);
            SetObj(wristUi, "gameFlow", flow);
            SetObj(wristUi, "missionTimer", mission);
            SetObj(flow, "missionTimer", mission);

            GameObject pcAudio = new GameObject("Mars_PC_Playlist_AudioSource");
            pcAudio.transform.SetParent(gameplayRoot.transform, false);
            pcAudio.transform.position = new Vector3(-2.3f, 1.05f, -1.9f);
            AudioSource playlist = pcAudio.AddComponent<AudioSource>();
            playlist.loop = true;
            playlist.spatialBlend = 0f;
            playlist.playOnAwake = false;
            SetObj(music, "playlistSource", playlist);

            GameObject keyboard = Cube("PC_Keyboard_WakeMusic", new Vector3(-2.25f, 0.92f, -1.35f), new Vector3(0.55f, 0.06f, 0.22f), blackMat, gameplayRoot.transform);
            InteractableButton keyboardButton = keyboard.AddComponent<InteractableButton>();
            SetEnum(keyboardButton, "action", (int)InteractableButton.ActionKind.WakeMusic);
            SetObj(keyboardButton, "musicAnchor", music);
            Text("Label_PC_Keyboard", keyboard.transform.position + new Vector3(0f, 0.08f, 0f), Quaternion.Euler(72f, 0f, 0f), "PLAY", 0.04f, Color.cyan, uiRoot.transform);

            ElectricalPanelPuzzle marsPuzzle = CreatePanel("BT_Mars_Tutorial", "Mars", new Vector3(3.86f, 1.35f, -1.1f), Quaternion.Euler(0f, -90f, 0f), gameplayRoot.transform, yellowMat, cyanMat, blueMat, orangeMat, blackMat, stress, entityDirector, subtitles, haptics);
            ElectricalPanelPuzzle serverPuzzle = CreatePanel("BT_INFO_Server", "INFO", new Vector3(7.38f, 1.35f, 12f), Quaternion.Euler(0f, -90f, 0f), gameplayRoot.transform, yellowMat, cyanMat, blueMat, orangeMat, blackMat, stress, entityDirector, subtitles, haptics);

            GameObject finalDoorRoot = new GameObject("TwoHandDoor_FinalExit");
            finalDoorRoot.transform.SetParent(gameplayRoot.transform, false);
            finalDoorRoot.transform.position = new Vector3(0f, 1.25f, 27.95f);
            TwoHandDoor door = finalDoorRoot.AddComponent<TwoHandDoor>();
            CubeLocal("Door_Leaf", Vector3.zero, Vector3.zero, new Vector3(1.65f, 2.5f, 0.12f), doorMat, finalDoorRoot.transform);
            GameObject leftHandle = CubeLocal("Door_Handle_Left", new Vector3(-0.42f, 0.05f, -0.12f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.2f), yellowMat, finalDoorRoot.transform);
            GameObject rightHandle = CubeLocal("Door_Handle_Right", new Vector3(0.42f, 0.05f, -0.12f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.2f), yellowMat, finalDoorRoot.transform);
            TwoHandDoorHandle leftDoorHandle = leftHandle.AddComponent<TwoHandDoorHandle>();
            TwoHandDoorHandle rightDoorHandle = rightHandle.AddComponent<TwoHandDoorHandle>();
            SetObj(door, "doorPivot", finalDoorRoot.transform);
            SetObj(door, "subtitles", subtitles);
            SetObj(door, "gameFlow", flow);
            SetObj(leftDoorHandle, "door", door);
            SetBool(leftDoorHandle, "leftHandle", true);
            SetObj(rightDoorHandle, "door", door);
            SetBool(rightDoorHandle, "leftHandle", false);

            GameObject revealGroup = new GameObject("FinalReveal_SafeReality");
            revealGroup.transform.SetParent(gameplayRoot.transform, false);
            Light revealA = PointLight("Reveal_Light_Hall", new Vector3(-1.8f, 2.35f, 26f), 0f, 8f, new Color(1f, 0.95f, 0.82f), revealGroup.transform);
            Light revealB = PointLight("Reveal_Light_Corridor", new Vector3(1.4f, 2.35f, 20f), 0f, 9f, new Color(1f, 0.95f, 0.82f), revealGroup.transform);
            Text("Reveal_Text", new Vector3(0f, 1.55f, 27.2f), Quaternion.Euler(0f, 180f, 0f), "Le batiment etait encore ouvert.\nIl est temps de rentrer.", 0.09f, Color.white, revealGroup.transform);
            revealGroup.SetActive(false);

            BuildTeleportNodes(gameplayRoot.transform, signMat);
            BuildStressZones(gameplayRoot.transform, invisibleTriggerMat, stress);

            GameObject entityVisual = BuildEntityVisual(gameplayRoot.transform, entityMat, redMat);
            EntityAnchor e0 = CreateAnchor("E0_Backstage", "E0", StressStage.Anchored, new Vector3(6.6f, 1.2f, 14.1f), gameplayRoot.transform);
            EntityAnchor e1 = CreateAnchor("E1_GlassReflection", "E1", StressStage.Altered, new Vector3(1.85f, 1.2f, 8.5f), gameplayRoot.transform);
            EntityAnchor e2 = CreateAnchor("E2_ServerThreshold", "E2", StressStage.Panic, new Vector3(1.1f, 1.2f, 13.7f), gameplayRoot.transform);
            EntityAnchor e3 = CreateAnchor("E3_ServerCorner", "E3", StressStage.Panic, new Vector3(6.7f, 1.2f, 9.8f), gameplayRoot.transform);
            EntityAnchor e4 = CreateAnchor("E4_BehindExit", "E4", StressStage.Crisis, new Vector3(0f, 1.2f, 27.1f), gameplayRoot.transform);
            SetObj(entityDirector, "stressDirector", stress);
            SetObj(entityDirector, "playerHead", mainCamera.transform);
            SetObj(entityDirector, "entityVisual", entityVisual);
            SetObjArray(entityDirector, "anchors", new UnityEngine.Object[] { e0, e1, e2, e3, e4 });

            SetObj(flow, "finalDoor", door);
            SetObj(flow, "musicAnchor", music);
            SetObj(flow, "batteryTimer", battery);
            SetObj(flow, "stressDirector", stress);
            SetObj(flow, "entityDirector", entityDirector);
            SetObj(flow, "subtitles", subtitles);
            SetObj(flow, "pcScreenText", pcScreen);
            SetObj(flow, "revealGroup", revealGroup);
            SetObjArray(flow, "requiredPuzzles", new UnityEngine.Object[] { marsPuzzle, serverPuzzle });
            SetObjArray(flow, "revealLights", new UnityEngine.Object[] { revealA, revealB });

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Protocole Zero MVP scene at " + ScenePath);
        }

        private static void BuildArchitecture(Transform parent, Material floorMat, Material wallMat, Material ceilingMat, Material glassMat, Material pillarMat, Material tableMat, Material chairMat, Material blackMat, Material signMat, Material doorMat)
        {
            Cube("Floor_Mars", new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 6f), floorMat, parent);
            Cube("Ceiling_Mars", new Vector3(0f, 2.72f, 0f), new Vector3(8f, 0.08f, 6f), ceilingMat, parent);
            Cube("Wall_Mars_West", new Vector3(-4f, 1.35f, 0f), new Vector3(0.15f, 2.7f, 6f), wallMat, parent);
            Cube("Wall_Mars_East", new Vector3(4f, 1.35f, 0f), new Vector3(0.15f, 2.7f, 6f), wallMat, parent);
            Cube("Wall_Mars_South", new Vector3(0f, 1.35f, -3f), new Vector3(8f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Mars_North_Left", new Vector3(-2.75f, 1.35f, 3f), new Vector3(2.5f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Mars_North_Right", new Vector3(2.75f, 1.35f, 3f), new Vector3(2.5f, 2.7f, 0.15f), wallMat, parent);
            Text("Sign_Mars", new Vector3(0f, 2.15f, 2.92f), Quaternion.Euler(0f, 180f, 0f), "MARS", 0.16f, Color.white, parent);

            Cube("Floor_Corridor_Glass", new Vector3(0f, -0.05f, 12.7f), new Vector3(2.6f, 0.1f, 19.4f), floorMat, parent);
            Cube("Ceiling_Corridor", new Vector3(0f, 2.72f, 12.7f), new Vector3(2.6f, 0.08f, 19.4f), ceilingMat, parent);
            Cube("Wall_Corridor_West", new Vector3(-1.32f, 1.35f, 12.7f), new Vector3(0.12f, 2.7f, 19.4f), wallMat, parent);
            for (int i = 0; i < 5; i++)
            {
                float z = 5.2f + i * 3.5f;
                Cube("GlassPanel_Corridor_" + i, new Vector3(1.32f, 1.35f, z), new Vector3(0.06f, 2.25f, 2.6f), glassMat, parent);
                Cylinder("RoundWhitePillar_" + i, new Vector3(1.05f, 1.35f, z + 1.55f), new Vector3(0.22f, 1.35f, 0.22f), pillarMat, parent);
            }
            Text("Sign_Corridor_Info", new Vector3(-1.22f, 2.15f, 11.2f), Quaternion.Euler(0f, 90f, 0f), "INFO / BT", 0.1f, Color.cyan, parent);

            Cube("Floor_Server_INFO", new Vector3(5f, -0.05f, 12f), new Vector3(5f, 0.1f, 5f), floorMat, parent);
            Cube("Ceiling_Server_INFO", new Vector3(5f, 2.72f, 12f), new Vector3(5f, 0.08f, 5f), ceilingMat, parent);
            Cube("Wall_Server_East", new Vector3(7.5f, 1.35f, 12f), new Vector3(0.15f, 2.7f, 5f), wallMat, parent);
            Cube("Wall_Server_North", new Vector3(5f, 1.35f, 14.5f), new Vector3(5f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Server_South", new Vector3(5f, 1.35f, 9.5f), new Vector3(5f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Server_West_A", new Vector3(2.5f, 1.35f, 10.2f), new Vector3(0.15f, 2.7f, 1.4f), wallMat, parent);
            Cube("Wall_Server_West_B", new Vector3(2.5f, 1.35f, 13.8f), new Vector3(0.15f, 2.7f, 1.4f), wallMat, parent);
            Text("Sign_INFO", new Vector3(2.62f, 2.15f, 12f), Quaternion.Euler(0f, -90f, 0f), "INFO", 0.12f, Color.cyan, parent);
            for (int i = 0; i < 4; i++)
            {
                Cube("ServerRack_" + i, new Vector3(4.1f + (i % 2) * 1.2f, 1.05f, 10.5f + (i / 2) * 2.6f), new Vector3(0.75f, 2.1f, 0.65f), blackMat, parent);
                Cube("ServerRack_LED_" + i, new Vector3(3.72f + (i % 2) * 1.2f, 1.55f, 10.2f + (i / 2) * 2.6f), new Vector3(0.04f, 0.08f, 0.18f), signMat, parent);
            }

            Cube("Floor_Branch_Leisure_Kitchen", new Vector3(-4.6f, -0.05f, 15.7f), new Vector3(6.6f, 0.1f, 4f), floorMat, parent);
            Cube("Wall_Branch_Back", new Vector3(-4.6f, 1.35f, 17.75f), new Vector3(6.6f, 2.7f, 0.15f), wallMat, parent);
            Text("Sign_Pluto_Saturne_Mercure", new Vector3(-2.3f, 1.9f, 17.62f), Quaternion.Euler(0f, 180f, 0f), "PLUTON  SATURNE  MERCURE", 0.09f, Color.white, parent);
            Cube("Foosball_Placeholder", new Vector3(-5.2f, 0.55f, 15.5f), new Vector3(1.6f, 0.55f, 0.8f), tableMat, parent);
            Cube("Vending_Picard_Placeholder", new Vector3(-7.3f, 1.1f, 16.7f), new Vector3(0.9f, 2.2f, 0.6f), blackMat, parent);

            Cube("Floor_Hall", new Vector3(0f, -0.05f, 25f), new Vector3(8f, 0.1f, 6f), floorMat, parent);
            Cube("Ceiling_Hall", new Vector3(0f, 2.72f, 25f), new Vector3(8f, 0.08f, 6f), ceilingMat, parent);
            Cube("Wall_Hall_West", new Vector3(-4f, 1.35f, 25f), new Vector3(0.15f, 2.7f, 6f), wallMat, parent);
            Cube("Wall_Hall_East", new Vector3(4f, 1.35f, 25f), new Vector3(0.15f, 2.7f, 6f), wallMat, parent);
            Cube("Wall_Hall_North_Left", new Vector3(-2.9f, 1.35f, 28f), new Vector3(2.2f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Hall_North_Right", new Vector3(2.9f, 1.35f, 28f), new Vector3(2.2f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Hall_South_Left", new Vector3(-2.7f, 1.35f, 22f), new Vector3(2.6f, 2.7f, 0.15f), wallMat, parent);
            Cube("Wall_Hall_South_Right", new Vector3(2.7f, 1.35f, 22f), new Vector3(2.6f, 2.7f, 0.15f), wallMat, parent);
            Cube("ASC_Static_Left", new Vector3(-3.2f, 1.15f, 25f), new Vector3(0.12f, 2.3f, 1.25f), doorMat, parent);
            Cube("ASC_Static_Right", new Vector3(3.2f, 1.15f, 25f), new Vector3(0.12f, 2.3f, 1.25f), doorMat, parent);
            Text("Sign_ASC", new Vector3(-3.28f, 2.35f, 25f), Quaternion.Euler(0f, 90f, 0f), "ASC", 0.11f, Color.white, parent);
            Text("Sign_EXIT", new Vector3(0f, 2.45f, 27.82f), Quaternion.Euler(0f, 180f, 0f), "SORTIE", 0.16f, Color.green, parent);

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = -2.6f + col * 2.2f;
                    float z = -1.5f + row * 1.6f;
                    Cube("Mars_Table_" + row + "_" + col, new Vector3(x, 0.72f, z), new Vector3(1.35f, 0.08f, 0.7f), tableMat, parent);
                    Cube("Mars_Chair_Blue_" + row + "_" + col, new Vector3(x, 0.45f, z + 0.58f), new Vector3(0.45f, 0.7f, 0.45f), chairMat, parent);
                }
            }
            Cube("Mars_Laptop_Base", new Vector3(-2.25f, 0.88f, -1.65f), new Vector3(0.72f, 0.05f, 0.48f), blackMat, parent);
            Cube("Mars_Laptop_Screen", new Vector3(-2.25f, 1.15f, -1.92f), new Vector3(0.72f, 0.44f, 0.04f), blackMat, parent);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject sun = new GameObject("Directional_NightFill");
            sun.transform.SetParent(parent, false);
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 0.18f;
            sunLight.color = new Color(0.54f, 0.62f, 0.8f);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            for (int i = 0; i < 7; i++)
            {
                float z = 4.5f + i * 3.1f;
                PointLight("CeilingSpot_Corridor_" + i, new Vector3(0f, 2.45f, z), 1.15f, 5.5f, new Color(0.9f, 0.92f, 1f), parent);
            }
            PointLight("CeilingSpot_Mars_A", new Vector3(-2f, 2.45f, 0f), 1.6f, 6f, new Color(1f, 0.94f, 0.78f), parent);
            PointLight("CeilingSpot_Mars_B", new Vector3(2f, 2.45f, 0f), 1.2f, 6f, new Color(1f, 0.94f, 0.78f), parent);
            PointLight("Server_ColdLight", new Vector3(5f, 2.2f, 12f), 1.35f, 5.5f, new Color(0.55f, 0.85f, 1f), parent);
            PointLight("Hall_ExitLight", new Vector3(0f, 2.35f, 26.5f), 1.25f, 6f, new Color(0.75f, 1f, 0.85f), parent);
        }

        private static ElectricalPanelPuzzle CreatePanel(string name, string puzzleId, Vector3 position, Quaternion rotation, Transform parent, Material statusMat, Material cyanMat, Material blueMat, Material orangeMat, Material panelMat, StressDirector stress, EntityDirector entity, SubtitleManager subtitles, HapticFeedbackRouter haptics)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            ElectricalPanelPuzzle puzzle = root.AddComponent<ElectricalPanelPuzzle>();

            CubeLocal("Panel_Box", Vector3.zero, Vector3.zero, new Vector3(0.12f, 1.35f, 1.15f), panelMat, root.transform);
            GameObject statusCube = CubeLocal("Panel_Status_LED", new Vector3(-0.08f, 0.48f, 0.42f), Vector3.zero, new Vector3(0.05f, 0.12f, 0.16f), statusMat, root.transform);
            Light statusLight = PointLightLocal("Panel_Status_Light", new Vector3(-0.3f, 0.48f, 0.42f), 1.8f, 1.4f, Color.yellow, root.transform);
            TextMesh statusText = TextLocal("Panel_Status_Text", new Vector3(-0.09f, 0.62f, -0.2f), Quaternion.Euler(0f, 90f, 0f), "BT", 0.055f, Color.yellow, root.transform);

            ElectricalSocket[] sockets = new ElectricalSocket[2];
            CablePlug[] plugs = new CablePlug[2];
            string[] ids = { "A", "B" };
            Material[] plugMats = { blueMat, orangeMat };
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -0.28f : 0.28f;
                GameObject snap = new GameObject("Snap_" + ids[i]);
                snap.transform.SetParent(root.transform, false);
                snap.transform.localPosition = new Vector3(-0.22f, 0.1f, z);
                snap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                GameObject socketGo = SphereLocal("Socket_" + ids[i], new Vector3(-0.17f, 0.1f, z), Vector3.one * 0.22f, cyanMat, root.transform);
                ElectricalSocket socket = socketGo.AddComponent<ElectricalSocket>();
                sockets[i] = socket;
                Light socketLight = PointLightLocal("Socket_Light_" + ids[i], new Vector3(-0.34f, 0.1f, z), 1.2f, 0.9f, Color.yellow, root.transform);
                SetString(socket, "targetPlugId", ids[i]);
                SetObj(socket, "snapPoint", snap.transform);
                SetObj(socket, "puzzle", puzzle);
                SetObj(socket, "feedbackRenderer", socketGo.GetComponent<Renderer>());
                SetObj(socket, "feedbackLight", socketLight);

                GameObject plugGo = CylinderLocal("CablePlug_" + ids[i], new Vector3(-0.58f, -0.42f, z), new Vector3(0.1f, 0.16f, 0.1f), plugMats[i], root.transform);
                Rigidbody rb = plugGo.AddComponent<Rigidbody>();
                rb.mass = 0.08f;
                rb.useGravity = false;
                CablePlug plug = plugGo.AddComponent<CablePlug>();
                plugs[i] = plug;
                SetString(plug, "plugId", ids[i]);
                SetObj(plug, "feedbackRenderer", plugGo.GetComponent<Renderer>());
                AddComponentIfFound(plugGo, "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit", "UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                TextLocal("Cable_Label_" + ids[i], new Vector3(-0.78f, -0.42f, z), Quaternion.Euler(0f, 90f, 0f), ids[i], 0.06f, Color.white, root.transform);
            }

            SetString(puzzle, "puzzleId", puzzleId);
            SetObjArray(puzzle, "sockets", sockets);
            SetObjArray(puzzle, "plugs", plugs);
            SetObj(puzzle, "statusRenderer", statusCube.GetComponent<Renderer>());
            SetObj(puzzle, "statusLight", statusLight);
            SetObj(puzzle, "statusText", statusText);
            SetObj(puzzle, "stressDirector", stress);
            SetObj(puzzle, "entityDirector", entity);
            SetObj(puzzle, "subtitles", subtitles);
            SetObj(puzzle, "haptics", haptics);
            TextLocal("Panel_Title", new Vector3(-0.1f, -0.62f, 0f), Quaternion.Euler(0f, 90f, 0f), "BT " + puzzleId, 0.07f, Color.white, root.transform);
            return puzzle;
        }

        private static void BuildTeleportNodes(Transform parent, Material mat)
        {
            Vector3[] points =
            {
                new Vector3(0f, 0.02f, -1.5f),
                new Vector3(0f, 0.02f, 3.8f),
                new Vector3(0f, 0.02f, 8f),
                new Vector3(0f, 0.02f, 12f),
                new Vector3(4.2f, 0.02f, 12f),
                new Vector3(0f, 0.02f, 20f),
                new Vector3(0f, 0.02f, 25.5f)
            };

            for (int i = 0; i < points.Length; i++)
            {
                GameObject anchor = InstantiatePrefab("Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/DemoSceneAssets/Prefabs/Teleport/Teleport Anchor.prefab", parent);
                if (anchor != null)
                {
                    anchor.name = "TeleportAnchor_MVP_" + i;
                    anchor.transform.position = points[i];
                    anchor.transform.rotation = Quaternion.identity;
                }
                else
                {
                    Cylinder("TeleportDisc_MVP_" + i, points[i], new Vector3(0.45f, 0.01f, 0.45f), mat, parent);
                }
            }
        }

        private static void BuildStressZones(Transform parent, Material mat, StressDirector stress)
        {
            CreateStressZone("StressZone_CorridorDark", new Vector3(0f, 1.2f, 15.5f), new Vector3(2.5f, 2.4f, 7f), 3f, mat, parent, stress);
            CreateStressZone("StressZone_ServerNoise", new Vector3(5f, 1.2f, 12f), new Vector3(5f, 2.4f, 5f), 4.5f, mat, parent, stress);
        }

        private static void CreateStressZone(string name, Vector3 pos, Vector3 scale, float stressPerSecond, Material mat, Transform parent, StressDirector stress)
        {
            GameObject zone = Cube(name, pos, scale, mat, parent);
            zone.GetComponent<BoxCollider>().isTrigger = true;
            Renderer renderer = zone.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            StressZone stressZone = zone.AddComponent<StressZone>();
            SetFloat(stressZone, "stressPerSecond", stressPerSecond);
            SetObj(stressZone, "stressDirector", stress);
        }

        private static GameObject BuildEntityVisual(Transform parent, Material bodyMat, Material eyeMat)
        {
            GameObject root = new GameObject("Entity_Visual_ShadowPlaceholder");
            root.transform.SetParent(parent, false);
            CylinderLocal("Entity_Body", new Vector3(0f, 0.85f, 0f), new Vector3(0.26f, 0.85f, 0.26f), bodyMat, root.transform);
            SphereLocal("Entity_Head", new Vector3(0f, 1.82f, 0f), new Vector3(0.45f, 0.55f, 0.45f), bodyMat, root.transform);
            CubeLocal("Entity_Eye_Left", new Vector3(-0.09f, 1.86f, -0.23f), Vector3.zero, new Vector3(0.06f, 0.035f, 0.02f), eyeMat, root.transform);
            CubeLocal("Entity_Eye_Right", new Vector3(0.09f, 1.86f, -0.23f), Vector3.zero, new Vector3(0.06f, 0.035f, 0.02f), eyeMat, root.transform);
            root.SetActive(false);
            return root;
        }

        private static EntityAnchor CreateAnchor(string name, string id, StressStage stage, Vector3 position, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            EntityAnchor anchor = go.AddComponent<EntityAnchor>();
            SetString(anchor, "anchorId", id);
            SetEnum(anchor, "minimumStage", (int)stage);
            return anchor;
        }

        private static GameObject Root(string name)
        {
            return new GameObject(name);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static GameObject CubeLocal(string name, Vector3 localPosition, Vector3 localEuler, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static GameObject CylinderLocal(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static GameObject SphereLocal(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static TextMesh Text(string name, Vector3 position, Quaternion rotation, string content, float characterSize, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = content;
            text.characterSize = characterSize;
            text.fontSize = 96;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            return text;
        }

        private static TextMesh TextLocal(string name, Vector3 localPosition, Quaternion localRotation, string content, float characterSize, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = content;
            text.characterSize = characterSize;
            text.fontSize = 96;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            return text;
        }

        private static Light PointLight(string name, Vector3 position, float intensity, float range, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            return light;
        }

        private static Light PointLightLocal(string name, Vector3 localPosition, float intensity, float range, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            return light;
        }

        private static Material MakeMaterial(string name, Color color, bool transparent = false, bool emission = false)
        {
            string path = "Assets/Generated/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 1.7f);
                }
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject InstantiatePrefab(string assetPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab " + assetPath);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return null;
            }
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static Transform FindChildByName(Transform root, string token)
        {
            if (root.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildByName(root.GetChild(i), token);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Component AddComponentIfFound(GameObject go, params string[] typeNames)
        {
            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = Type.GetType(typeNames[i]);
                if (type != null)
                {
                    return go.AddComponent(type);
                }
            }
            return null;
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try
            {
                go.tag = tag;
            }
            catch (UnityException)
            {
                Debug.LogWarning("Tag unavailable: " + tag + " on " + go.name);
            }
        }

        private static void SetObj(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("Missing serialized property " + propertyName + " on " + target.name);
            }
        }

        private static void SetObjArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.isArray)
            {
                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("Missing array property " + propertyName + " on " + target.name);
            }
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetEnum(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
