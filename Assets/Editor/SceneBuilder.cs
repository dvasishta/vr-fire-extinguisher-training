// SceneBuilder.cs  —  VRFire/Build All Scenes
// Opens Unity, then run: menu bar → VRFire → Build All Scenes
// Prerequisites: Meta XR SDK imported (OVRGrabbable must resolve), TextMeshPro imported.
//
// What it builds:
//   Assets/Scenes/Tutorial.unity  — slide panel, OVR rig placeholder
//   Assets/Scenes/Level1.unity    — escape route, 1 fire, 3 exits
//   Assets/Scenes/Level2.unity    — pick nearest extinguisher, 3 fire extinguishers
//   Assets/Scenes/Level3.unity    — rescue 2 NPCs, 2-room building
//
// What you still do manually after running:
//   1. Replace each OVRCameraRig_Placeholder with real OVRCameraRig prefab
//   2. Drag hand anchors' OVRGrabber components into VRPlayer fields in Inspector
//   3. Assign audio clips to AlarmAudio + NPCCharacter in Inspector
//   4. Configure Particle System visuals on each FirePS / SmokePS / HeatHazePS / SprayPS child
//   5. Level3: Window → AI → Navigation → Bake NavMesh

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class SceneBuilder
{
    const string ScenesPath = "Assets/Scenes";

    // ─────────────────────────────────────────────────────────────────────────
    // ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("VRFire/Build All Scenes")]
    public static void BuildAllScenes()
    {
        if (!AssetDatabase.IsValidFolder(ScenesPath))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EnsureTags();

        BuildTutorial();
        BuildLevel1();
        BuildLevel2();
        BuildLevel3();

        AddToBuildSettings();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "VRFire Scenes Built",
            "All 4 scenes created in Assets/Scenes/ and added to Build Settings.\n\n" +
            "Manual steps:\n" +
            "  1. Replace each 'OVRCameraRig_Placeholder' with real OVRCameraRig prefab\n" +
            "  2. Assign OVRGrabber hand refs in VRPlayer Inspector\n" +
            "  3. Assign audio clips for AlarmAudio & NPCCharacter\n" +
            "  4. Configure Particle System visuals on Fire/Spray child GameObjects\n" +
            "  5. Level3 only: Window > AI > Navigation > Bake NavMesh",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TUTORIAL
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildTutorial()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateEventSystem();
        CreateRoom("Room", Vector3.zero, 8f, 6f, 3f);

        CreateOVRPlaceholder(new Vector3(0f, 0f, -2f));
        CreateTutorialCanvas(new Vector3(0f, 1.5f, 2.8f));

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.levelIndex = 0;
        gm.npcCount = 0;

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Tutorial.unity");
        Debug.Log("[SceneBuilder] Tutorial.unity saved.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEVEL 1  —  ESCAPE
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildLevel1()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateEventSystem();
        CreateRoom("Room", Vector3.zero, 10f, 8f, 3f);

        CreateOVRPlaceholder(new Vector3(0f, 0f, 0f));

        // One fire in the NW corner
        CreateFireActor(new Vector3(-4f, 0f, 3f), "FireActor_1");

        // Two flammable props nearby
        CreateFlammable(new Vector3(-3.5f, 0.5f, 2.5f), new Vector3(1f, 1f, 0.5f), "Desk_Flammable");
        CreateFlammable(new Vector3(-3f,   0.5f, 3.2f), new Vector3(0.8f, 1f, 0.6f), "Cabinet_Flammable");

        // Three exits  —  North = correct emergency exit
        CreateSafeZone(new Vector3(0f,   1f,  4.05f), new Vector3(2f, 2.5f, 0.2f), true,  "Exit_North_CORRECT");
        CreateSafeZone(new Vector3(5.05f, 1f,  0f),   new Vector3(0.2f, 2.5f, 2f), false, "Exit_East_Storage");
        CreateSafeZone(new Vector3(0f,   1f, -4.05f), new Vector3(2f, 2.5f, 0.2f), false, "Exit_South_Utility");

        CreateAlarm();
        CreateManagers(1, 0);
        CreateHUDCanvas();
        CreateScoreScreen();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level1.unity");
        Debug.Log("[SceneBuilder] Level1.unity saved.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEVEL 2  —  EXTINGUISHER
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildLevel2()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateEventSystem();
        CreateRoom("Room", Vector3.zero, 15f, 12f, 3f);

        CreateOVRPlaceholder(new Vector3(0f, 0f, -5f));

        // Fire at center-right; extinguishers at 3 m, 7 m, 12 m
        CreateFireActor(new Vector3(4f, 0f, 2f), "FireActor_1");
        CreateExtinguisher(new Vector3(1f,  0.5f,  2f), "EXT_A_Nearest_3m");
        CreateExtinguisher(new Vector3(-3f, 0.5f,  2f), "EXT_B_Mid_7m");
        CreateExtinguisher(new Vector3(-8f, 0.5f,  2f), "EXT_C_Far_12m");

        new GameObject("ExtinguisherSpawner").AddComponent<ExtinguisherSpawner>();

        CreateSafeZone(new Vector3(0f,    1f,  6.05f), new Vector3(2f, 2.5f, 0.2f),  true,  "Exit_North_CORRECT");
        CreateSafeZone(new Vector3(7.6f,  1f,  0f),   new Vector3(0.2f, 2.5f, 2f), false, "Exit_East_Wrong");
        CreateSafeZone(new Vector3(-7.6f, 1f,  0f),   new Vector3(0.2f, 2.5f, 2f), false, "Exit_West_Wrong");

        CreateAlarm();
        CreateManagers(2, 0);
        CreateHUDCanvas();
        CreateScoreScreen();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level2.unity");
        Debug.Log("[SceneBuilder] Level2.unity saved.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEVEL 3  —  NPC RESCUE
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildLevel3()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateDirectionalLight();
        CreateEventSystem();

        // Three rooms: Lobby (player start), Room A (right), Room B (ahead)
        CreateRoom("Lobby",  new Vector3(0f,   0f, 0f),  8f, 6f, 3f);
        CreateRoom("RoomA",  new Vector3(10f,  0f, 0f),  8f, 6f, 3f);
        CreateRoom("RoomB",  new Vector3(0f,   0f, 8f),  8f, 6f, 3f);

        CreateOVRPlaceholder(new Vector3(0f, 0f, -2f));

        // Fires — one blocking RoomA doorway, one in RoomB corner
        CreateFireActor(new Vector3(5.5f, 0f, 0f),  "FireActor_DoorwayA");
        CreateFireActor(new Vector3(10f,  0f, 5f),  "FireActor_RoomB");

        // Extinguishers
        CreateExtinguisher(new Vector3(-2f, 0.5f,  0f),  "EXT_Lobby");
        CreateExtinguisher(new Vector3(10f, 0.5f, -2f), "EXT_RoomA");

        new GameObject("ExtinguisherSpawner").AddComponent<ExtinguisherSpawner>();

        // NPCs
        CreateNPC(new Vector3(9f,  0f, 1f), "NPCCharacter_1");
        CreateNPC(new Vector3(10f, 0f, 6f), "NPCCharacter_2");

        // Correct exit — front of lobby
        CreateSafeZone(new Vector3(0f, 1f, -3.05f), new Vector3(2f, 2.5f, 0.2f), true, "Exit_Lobby_CORRECT");

        CreateAlarm();
        CreateManagers(3, 2);
        CreateHUDCanvas();
        CreateScoreScreen();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level3.unity");
        Debug.Log("[SceneBuilder] Level3.unity saved.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMMON HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    static void CreateManagers(int levelIndex, int npcCount)
    {
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.levelIndex = levelIndex;
        gm.npcCount   = npcCount;

        new GameObject("ScoringManager").AddComponent<ScoringManager>();
    }

    static void CreateAlarm()
    {
        var go   = new GameObject("AlarmAudio");
        var comp = go.AddComponent<AlarmAudio>(); // [RequireComponent] auto-adds AudioSource
        var src  = go.GetComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = true;
        // Assign alarm WAV clip in Inspector
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCENE GEOMETRY
    // ─────────────────────────────────────────────────────────────────────────

    static void CreateRoom(string label, Vector3 center, float width, float depth, float height)
    {
        var root = new GameObject($"{label}_Geometry");
        root.transform.position = center;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);

        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(root.transform, false);
        ceiling.transform.localPosition  = new Vector3(0f, height, 0f);
        ceiling.transform.localRotation  = Quaternion.Euler(180f, 0f, 0f);
        ceiling.transform.localScale     = new Vector3(width / 10f, 1f, depth / 10f);

        float hw = width * 0.5f, hd = depth * 0.5f, hh = height * 0.5f;
        MakeWall(root, "Wall_North", new Vector3(0f,  hh,  hd), new Vector3(width, height, 0.2f));
        MakeWall(root, "Wall_South", new Vector3(0f,  hh, -hd), new Vector3(width, height, 0.2f));
        MakeWall(root, "Wall_East",  new Vector3( hw, hh,  0f), new Vector3(0.2f,  height, depth));
        MakeWall(root, "Wall_West",  new Vector3(-hw, hh,  0f), new Vector3(0.2f,  height, depth));
    }

    static void MakeWall(GameObject parent, string name, Vector3 localPos, Vector3 size)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.SetParent(parent.transform, false);
        w.transform.localPosition = localPos;
        w.transform.localScale    = size;
    }

    static void CreateFlammable(Vector3 pos, Vector3 scale, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name             = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.tag = "Flammable";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OVR CAMERA RIG PLACEHOLDER
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateOVRPlaceholder(Vector3 pos)
    {
        var rig = new GameObject("OVRCameraRig_Placeholder");
        rig.transform.position = pos;

        // VRPlayer.cs tags itself VRPlayer in Awake, but we set the tag here too
        // so SafeZone trigger detection works even before Play mode
        try { rig.tag = "VRPlayer"; } catch { /* tag not yet in project */ }

        rig.AddComponent<VRPlayer>();
        // NOTE: after replacing with real OVRCameraRig prefab, drag the two
        //       OVRGrabber components from LeftHandAnchor / RightHandAnchor
        //       into the rightGrabber / leftGrabber fields on VRPlayer.

        var cam = new GameObject("CenterEyeAnchor");
        cam.transform.SetParent(rig.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.AddComponent<Camera>();

        var leftHand = new GameObject("LeftHandAnchor");
        leftHand.transform.SetParent(rig.transform, false);
        leftHand.transform.localPosition = new Vector3(-0.2f, 1.2f, 0.3f);

        var rightHand = new GameObject("RightHandAnchor");
        rightHand.transform.SetParent(rig.transform, false);
        rightHand.transform.localPosition = new Vector3(0.2f, 1.2f, 0.3f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIRE ACTOR
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateFireActor(Vector3 pos, string name)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var fire = go.AddComponent<FireActor>();

        // Zone colliders
        fire.baseZone       = AddSphereZone(go, "BaseZone",       Vector3.zero,              0.3f);
        fire.topZone        = AddSphereZone(go, "TopZone",        new Vector3(0f, 1.1f, 0f), 0.3f);
        fire.spreadDetection = AddSphereZone(go, "SpreadDetection", Vector3.zero,             2.0f);

        // Particle Systems (configure visuals in Inspector — see CLAUDE.md)
        fire.fireVFX      = AddParticleChild(go, "FirePS",     true);
        fire.smokeVFX     = AddParticleChild(go, "SmokePS",    true);
        fire.heatHazeVFX  = AddParticleChild(go, "HeatHazePS", true);

        // Audio
        var src = go.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = true;
        fire.fireCrackle = src;
        // Assign fire crackle WAV in Inspector
    }

    static SphereCollider AddSphereZone(GameObject parent, string childName, Vector3 localPos, float radius)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPos;
        var col = child.AddComponent<SphereCollider>();
        col.radius    = radius;
        col.isTrigger = true;
        return col;
    }

    static ParticleSystem AddParticleChild(GameObject parent, string childName, bool playOnAwake)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        var ps   = child.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = playOnAwake;
        return ps;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SAFE ZONE  (exit doorway trigger)
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateSafeZone(Vector3 pos, Vector3 colliderSize, bool isCorrect, string name)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size      = colliderSize;
        col.isTrigger = true;

        var zone = go.AddComponent<SafeZone>();
        zone.bIsCorrectExit = isCorrect;

        // Green strip on the floor shows correct exit; none on wrong exits
        if (isCorrect)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "ExitMarker";
            strip.transform.SetParent(go.transform, false);
            strip.transform.localPosition = new Vector3(0f, -0.49f, 0f);
            strip.transform.localScale    = new Vector3(colliderSize.x, 0.02f, colliderSize.z > 0.3f ? colliderSize.z : 0.5f);
            var mat = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.9f, 0.1f, 0.8f) };
            strip.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(strip.GetComponent<BoxCollider>());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIRE EXTINGUISHER
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateExtinguisher(Vector3 pos, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name             = name;
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);

        var mat = new Material(Shader.Find("Standard")) { color = new Color(0.8f, 0.1f, 0.1f) };
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // [RequireComponent(typeof(OVRGrabbable))] on FireExtinguisher auto-adds OVRGrabbable
        var ext = go.AddComponent<FireExtinguisher>();

        var nozzle = new GameObject("NozzleTip");
        nozzle.transform.SetParent(go.transform, false);
        nozzle.transform.localPosition = new Vector3(0f, 0.65f, 0.15f);
        ext.nozzleTip = nozzle.transform;

        var sprayPS = AddParticleChild(nozzle, "SprayPS", false);
        ext.sprayVFX = sprayPS;

        var src = go.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = false;
        ext.sprayAudio = src;
        // Assign spray WAV in Inspector
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NPC CHARACTER
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateNPC(Vector3 pos, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name             = name;
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

        var mat = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.55f, 1f) };
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        try { go.tag = "NPC"; } catch { }

        // Detection sphere  (triggers NPC follow when player enters)
        var detection = go.AddComponent<SphereCollider>();
        detection.radius    = 2.0f;
        detection.isTrigger = true;

        // [RequireComponent(typeof(NavMeshAgent))] auto-adds NavMeshAgent
        var npc   = go.AddComponent<NPCCharacter>();
        var agent = go.GetComponent<NavMeshAgent>();
        agent.speed             = 2.0f;
        agent.stoppingDistance  = 1.0f;

        var src = go.AddComponent<AudioSource>();
        src.loop        = false;
        src.playOnAwake = false;
        npc.audioSource = src;
        // Assign helpClip and thankYouClip WAVs in Inspector
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HUD CANVAS
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateHUDCanvas()
    {
        var root = new GameObject("HUD_Canvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var hud = root.AddComponent<HUDController>();

        // Timer  (top-centre)
        var timerGO  = MakeTMPText(root, "TimerText", "0:00", 56, Color.white,
                                   new Vector2(0.35f, 0.88f), Vector2.one);
        timerGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        hud.timerText = timerGO.GetComponent<TextMeshProUGUI>();

        // Fuel container  (bottom-right, hidden — GameManager.Start shows it for Level2+)
        var fuelCtr = new GameObject("FuelContainer");
        fuelCtr.transform.SetParent(root.transform, false);
        SetAnchors(fuelCtr, new Vector2(0.72f, 0f), new Vector2(1f, 0.15f));
        fuelCtr.SetActive(false);
        hud.fuelContainer = fuelCtr;

        MakeTMPText(fuelCtr, "CO2_Label", "CO₂", 22, Color.white,
                    new Vector2(0f, 0.55f), Vector2.one);

        var sliderGO = new GameObject("FuelBar");
        sliderGO.transform.SetParent(fuelCtr.transform, false);
        SetAnchors(sliderGO, Vector2.zero, new Vector2(1f, 0.5f));
        sliderGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;
        hud.fuelBar = slider;

        // Hint text  (bottom-left, fades after 5 s)
        var hintGO = MakeTMPText(root, "HintText", "Find the emergency exit!", 28, Color.yellow,
                                 Vector2.zero, new Vector2(0.45f, 0.1f));
        hintGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
        hud.hintText = hintGO.GetComponent<TextMeshProUGUI>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE SCREEN CANVAS
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateScoreScreen()
    {
        var root = new GameObject("ScoreScreen_Canvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        // Background
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var ss = root.AddComponent<ScoreScreen>();
        // Awake() calls SetActive(false) at runtime; leave active for editor wiring.

        ss.levelCompleteText = MakeTMPText(root, "LevelCompleteText", "Level Complete!", 72,
            Color.white, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 1f))
            .GetComponent<TextMeshProUGUI>();

        ss.totalScoreText = MakeTMPText(root, "TotalScoreText", "0", 80,
            new Color(1f, 0.65f, 0f), new Vector2(0.3f, 0.68f), new Vector2(0.7f, 0.83f))
            .GetComponent<TextMeshProUGUI>();
        ss.totalScoreText.fontStyle = FontStyles.Bold;

        ss.feedbackText = MakeTMPText(root, "FeedbackText", "", 26,
            new Color(0.85f, 0.85f, 0.85f), new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.69f))
            .GetComponent<TextMeshProUGUI>();

        ss.timeRow = MakeTMPText(root, "TimeRow", "Time: 0:00  —  0 pts", 32,
            Color.white, new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.61f))
            .GetComponent<TextMeshProUGUI>();
        ss.timeRow.alignment = TextAlignmentOptions.Left;

        ss.exitRow = MakeTMPText(root, "ExitRow", "Exit: —  0 pts", 32,
            Color.white, new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.52f))
            .GetComponent<TextMeshProUGUI>();
        ss.exitRow.alignment = TextAlignmentOptions.Left;

        // Extinguisher container (Level 2 & 3 only)
        ss.extinguisherContainer = MakeRowContainer(root, "ExtinguisherContainer",
            new Vector2(0.15f, 0.34f), new Vector2(0.85f, 0.43f));
        ss.extinguisherRow = MakeTMPText(ss.extinguisherContainer, "ExtinguisherRow",
            "Nearest extinguisher: 0 pts", 30, Color.white, Vector2.zero, Vector2.one)
            .GetComponent<TextMeshProUGUI>();
        ss.extinguisherRow.alignment = TextAlignmentOptions.Left;

        // Technique container (Level 2 & 3 only)
        ss.techniqueContainer = MakeRowContainer(root, "TechniqueContainer",
            new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.34f));
        ss.techniqueRow = MakeTMPText(ss.techniqueContainer, "TechniqueRow",
            "Technique: 0% base hits  —  0 pts", 30, Color.white, Vector2.zero, Vector2.one)
            .GetComponent<TextMeshProUGUI>();
        ss.techniqueRow.alignment = TextAlignmentOptions.Left;

        // NPC container (Level 3 only)
        ss.npcContainer = MakeRowContainer(root, "NPCContainer",
            new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.25f));
        ss.npcRow = MakeTMPText(ss.npcContainer, "NPCRow",
            "NPCs Rescued: 0/0  —  0 pts", 30, Color.white, Vector2.zero, Vector2.one)
            .GetComponent<TextMeshProUGUI>();
        ss.npcRow.alignment = TextAlignmentOptions.Left;

        // Buttons row
        var btnRow = new GameObject("Buttons");
        btnRow.transform.SetParent(root.transform, false);
        SetAnchors(btnRow, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.14f));
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing          = 24f;
        hlg.childAlignment   = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        ss.retryButton     = MakeButton(btnRow, "RetryButton",     "Retry");
        ss.nextLevelButton = MakeButton(btnRow, "NextLevelButton", "Next Level");
        ss.mainMenuButton  = MakeButton(btnRow, "MainMenuButton",  "Main Menu");
        // Button onClick listeners are wired at runtime by ScoreScreen.Populate()
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TUTORIAL CANVAS  (World Space panel on north wall)
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateTutorialCanvas(Vector3 worldPos)
    {
        var root = new GameObject("TutorialPanel_Canvas");
        root.transform.position   = worldPos;
        root.transform.localScale = Vector3.one * 0.001f; // 1px = 1mm

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1900f, 1070f);

        // Semi-transparent background panel
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        SetAnchors(bg, Vector2.zero, Vector2.one);

        var panel = root.AddComponent<TutorialPanel>();

        panel.titleText = MakeTMPText(root, "TitleText", "Welcome", 72,
            Color.white, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f))
            .GetComponent<TextMeshProUGUI>();
        panel.titleText.fontStyle = FontStyles.Bold;

        panel.bodyText = MakeTMPText(root, "BodyText", "", 32,
            new Color(0.9f, 0.9f, 0.9f), new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.75f))
            .GetComponent<TextMeshProUGUI>();
        panel.bodyText.alignment = TextAlignmentOptions.TopLeft;

        // Slide image placeholder
        var imgGO = new GameObject("SlideImage");
        imgGO.transform.SetParent(root.transform, false);
        var rawImg = imgGO.AddComponent<RawImage>();
        rawImg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        SetAnchors(imgGO, new Vector2(0.25f, 0.55f), new Vector2(0.75f, 0.76f));
        panel.slideImage = rawImg;

        // Dot indicators  (6 slides)
        var dotsGO = new GameObject("Dots");
        dotsGO.transform.SetParent(root.transform, false);
        SetAnchors(dotsGO, new Vector2(0.3f, 0.27f), new Vector2(0.7f, 0.34f));
        var hlg = dotsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing          = 12f;
        hlg.childAlignment   = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        var dots = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            var d = new GameObject($"Dot_{i}");
            d.transform.SetParent(dotsGO.transform, false);
            dots[i] = d.AddComponent<Image>();
            dots[i].color = i == 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
        panel.dotImages = dots;

        // Next button
        var nextBtn = MakeButton(root, "NextButton", "Next →");
        var nextRT  = nextBtn.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(0.55f, 0.05f);
        nextRT.anchorMax = new Vector2(0.75f, 0.2f);
        nextRT.offsetMin = nextRT.offsetMax = Vector2.zero;
        panel.nextButton = nextBtn.GetComponent<Button>();

        // Start button (hidden until last slide)
        var startBtn = MakeButton(root, "StartButton", "Start Training!");
        var startRT  = startBtn.GetComponent<RectTransform>();
        startRT.anchorMin = new Vector2(0.55f, 0.05f);
        startRT.anchorMax = new Vector2(0.75f, 0.2f);
        startRT.offsetMin = startRT.offsetMax = Vector2.zero;
        startBtn.gameObject.SetActive(false);
        panel.startButton = startBtn;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI PRIMITIVES
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject MakeTMPText(GameObject parent, string name, string text,
                                  float size, Color color,
                                  Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    static Button MakeButton(GameObject parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.92f);
        var btn = go.AddComponent<Button>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 30f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(textGO, Vector2.zero, Vector2.one);

        return btn;
    }

    static GameObject MakeRowContainer(GameObject parent, string name,
                                       Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    static void CreateDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = go.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        light.color     = new Color(1f, 0.95f, 0.84f);
        light.shadows   = LightShadows.Hard;
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static void EnsureTags()
    {
        string[] required = { "VRPlayer", "Flammable", "Grabbable", "NPC" };
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");

        foreach (var tag in required)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                { found = true; break; }
            }
            if (!found)
            {
                tagsProp.arraySize++;
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    static void AddToBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/Tutorial.unity", true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level1.unity",  true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level2.unity",  true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level3.unity",  true),
        };
    }
}
