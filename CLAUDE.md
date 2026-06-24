# VRFireTraining — Unity Reference Guide

## Project Summary
VR Fire Extinguisher training game for Meta Quest (standalone). 4 scenes: Tutorial, Level 1 (escape), Level 2 (find & use extinguisher), Level 3 (rescue NPCs). C# MonoBehaviours, Unity Particle Systems for fire, Meta XR SDK for grab/input.

## Important Notes for AI Assistant
- Unity 2022.3 LTS, Meta Quest 2/3, Android, Vulkan, Forward Rendering
- Grab system: Meta XR SDK — `OVRGrabbable` on objects, `OVRGrabber` on hands
- Input: `OVRInput.Get()` / `OVRInput.GetDown()` — no Unity New Input System mapping needed
- Use `InvokeRepeating` for recurring fire/NPC logic — never `Update` for game logic loops
- NavMesh must be baked in editor (Window > AI > Navigation > Bake), not at runtime
- Always guard division: `Mathf.Max(denominator, 1)` before dividing in score math
- All C# scripts are in `Assets/Scripts/` — no Blueprints
- Particle Systems are CPU-based (never VFX Graph — Android doesn't support it at Quest perf level)

---

## Quick Start — First Time Opening in Unity

1. Install Unity 2022.3 LTS via Unity Hub
2. Add Android Build Support module (Unity Hub > Installs > gear icon)
3. Open `VRFireTraining/` folder as a Unity project
4. Unity Package Manager will resolve packages from `Packages/manifest.json` automatically
5. If Meta XR SDK fails to resolve: Install via Unity Asset Store ("Meta XR All-in-One SDK") as fallback
6. **Edit > Project Settings > Player > Android tab:**
   - Package Name: `com.student.vrfiretraining`
   - Minimum API: 29, Target API: 32
   - Scripting Backend: IL2CPP, ARM64 only
   - Graphics: Vulkan (remove OpenGLES)
7. **Edit > Project Settings > XR Plugin Management > Android tab:**
   - Enable Oculus
8. **Edit > Project Settings > Quality:**
   - Rendering Path: Forward
   - Anti Aliasing: 4x MSAA
   - Disable shadows or set to hard shadows only
9. Create 4 scenes in `Assets/Scenes/`: Tutorial, Level1, Level2, Level3
10. Add all scenes to **File > Build Settings** in order: Tutorial first

---

## Folder Map

```
Assets/
  Scenes/          → Tutorial.unity, Level1.unity, Level2.unity, Level3.unity
  Scripts/
    Fire/          → FireActor.cs
    Extinguisher/  → FireExtinguisher.cs, ExtinguisherSpawner.cs
    NPC/           → NPCCharacter.cs
    Gameplay/      → ScoringManager.cs, SafeZone.cs, GameManager.cs
    Audio/         → AlarmAudio.cs
    Player/        → VRPlayer.cs
    UI/            → TutorialPanel.cs, HUDController.cs, ScoreScreen.cs
  Prefabs/
    Fire/          → FireActor.prefab (drag FireActor.cs onto GameObject)
    Extinguisher/  → FireExtinguisher.prefab
    NPC/           → NPCCharacter.prefab
  Audio/           → imported WAV files
  Materials/       → fire/wall/floor materials
  Textures/        → UI images, tutorial diagrams
  VFX/             → FirePS.prefab, SmokePS.prefab, HeatHazePS.prefab, SprayPS.prefab
  UI/              → Canvas prefabs
  Meshes/          → imported meshes from Fab.com / Unity Asset Store
Packages/
  manifest.json    → package dependencies (auto-resolved)
ProjectSettings/
  TagManager.asset → tags: Flammable, Grabbable, VRPlayer, NPC
```

---

## Scene Setup (all scenes)

Every scene needs:
- **OVRCameraRig** (from Meta XR SDK prefab) — replaces VR Pawn
  - Attach `VRPlayer.cs` to the OVRCameraRig root
  - Drag `LeftHandAnchor/OVRGrabber` → `leftGrabber` field
  - Drag `RightHandAnchor/OVRGrabber` → `rightGrabber` field
- **ScoringManager** GameObject (only in Level1–3) — auto-persists via DontDestroyOnLoad
- **GameManager** GameObject — set `levelIndex` and `npcCount` in Inspector
- **HUD Canvas** (Screen Space - Camera, or World Space anchored to camera)
- **EventSystem** (auto-created with Canvas)

---

## FireActor — Script Reference

**File:** `Assets/Scripts/Fire/FireActor.cs`
**Attach to:** Empty GameObject with Particle Systems and Colliders as children

### Required Components to Add in Inspector:
| Component | Settings |
|---|---|
| SphereCollider "BaseZone" | Radius 0.3m, Center Y=0, Is Trigger ON |
| SphereCollider "TopZone" | Radius 0.3m, Center Y=1.1m, Is Trigger ON |
| SphereCollider "SpreadDetection" | Radius 2.0m, Is Trigger ON |
| ParticleSystem "FirePS" | See Particle Setup below |
| ParticleSystem "SmokePS" | Auto-start ON |
| ParticleSystem "HeatHazePS" | Auto-start ON |
| AudioSource | Fire crackle WAV, Loop ON, Play On Awake ON |

### Inspector Fields:
| Field | Default | Notes |
|---|---|---|
| fireIntensity | 0.1 | 0=out, 1=full blaze |
| maxIntensity | 1.0 | cap |
| growthRate | 0.05 | added per second |
| spreadTime | 15.0 | seconds before spreading |
| fireActorPrefab | — | drag FireActor.prefab here |
| fireVFX / smokeVFX / heatHazeVFX | — | drag child ParticleSystems |
| fireCrackle | — | drag AudioSource |

### Public API:
```csharp
fire.ExtinguishHit(FireHitZone zone);   // called by FireExtinguisher
fire.IsExtinguished                      // bool property
fire.OnFireExtinguished                  // event Action, fires when put out
```

### Logic Flow:
- `Start()` → `InvokeRepeating("GrowFire", 1, 1)` + `InvokeRepeating("CheckSpread", 5, 5)`
- `GrowFire`: intensity += growthRate, updates ParticleSystem emission rate and scale
- `CheckSpread`: `Physics.OverlapSphere` radius 2m → spawns FireActor prefab on "Flammable" tagged objects
- `ExtinguishHit(Base)`: intensity −0.15; `ExtinguishHit(Top/Middle)`: intensity −0.03
- When intensity ≤ 0: stops all ParticleSystems + audio, fires `OnFireExtinguished`

---

## FireExtinguisher — Script Reference

**File:** `Assets/Scripts/Extinguisher/FireExtinguisher.cs`
**Attach to:** Extinguisher mesh GameObject

### Required Components:
| Component | Settings |
|---|---|
| OVRGrabbable | (Meta XR SDK) — handles grab physics |
| MeshCollider or BoxCollider | For grab detection |
| ParticleSystem (child) | Spray VFX — Auto-start OFF |
| AudioSource | Spray WAV, Loop ON, Play On Awake OFF |
| Transform "NozzleTip" | Empty child at the nozzle end, pointing forward (+Z) |

Add tag `Grabbable` to the GameObject.

### Inspector Fields:
| Field | Default |
|---|---|
| fuelLevel | 1.0 |
| drainRate | 0.05 |
| sprayRange | 3.0 (300cm) |
| nozzleTip | drag nozzle child Transform |
| sprayVFX | drag spray ParticleSystem |
| sprayAudio | drag AudioSource |

### Public API:
```csharp
ext.StartSpray();       // called by VRPlayer on trigger press
ext.StopSpray();        // called by VRPlayer on trigger release
ext.NotifyGrabbed(bool);// called by VRPlayer when grab state changes
ext.isNearestExtinguisher; // set by ExtinguisherSpawner
ext.fuelLevel;           // 0.0–1.0, read by HUDController
```

### Logic Flow:
- `VRPlayer` detects grab via `OVRGrabber.grabbedObject` and calls `NotifyGrabbed(true/false)`
- `StartSpray` → plays VFX + audio → starts `SprayTick` coroutine every 0.1s
- `SprayTick`: drains fuel, `Physics.Raycast` from nozzle forward
  - Hits `FireActor` → determines zone by Y position → calls `ExtinguishHit`
  - Updates `HUDController.Instance.SetFuelLevel`
- `StopSpray` → stops VFX + audio + coroutine

### Hit Zone Detection (in SprayTick):
```
hitY < fireBaseY + 0.30m  →  Base  (−0.15 intensity, +1 BaseHit)
hitY > fireBaseY + 0.80m  →  Top   (−0.03 intensity)
else                       →  Middle (−0.03 intensity)
```

---

## ExtinguisherSpawner — Script Reference

**File:** `Assets/Scripts/Extinguisher/ExtinguisherSpawner.cs`
**Place:** One per Level2 and Level3 scene. Any GameObject.

Finds all `FireActor` and `FireExtinguisher` objects at start (after 0.5s delay), calculates which extinguisher is nearest to the first fire, sets `isNearestExtinguisher = true` on it.

No Inspector fields needed.

---

## ScoringManager — Script Reference

**File:** `Assets/Scripts/Gameplay/ScoringManager.cs`
**Attach to:** Persistent singleton GameObject (DontDestroyOnLoad)

### Public API:
```csharp
ScoringManager.Instance.levelIndex = 2;           // set by GameManager
ScoringManager.Instance.InitLevel(npcCount);       // call at scene start
ScoringManager.Instance.RecordExtinguisherPickup(bool wasNearest);
ScoringManager.Instance.RecordSprayHit(FireHitZone zone);
ScoringManager.Instance.RecordNPCRescued();
ScoringManager.Instance.RecordExitUsed(bool correct);
int score = ScoringManager.Instance.CalculateFinalScore();
ScoreData data = ScoringManager.Instance.BuildScoreData();  // for ScoreScreen
```

### Score Formula:
| Component | Max | Condition |
|---|---|---|
| Time | 40 pts | `((120 - seconds) / 120) * 40` |
| Technique | 30 pts | `(baseHits / totalHits) * 30` (Level 2 & 3 only) |
| Correct Exit | 20 pts | bIsCorrectExit == true |
| Nearest Ext. | 10 pts | picked up nearest (Level 2 & 3 only) |
| NPC Rescue | 20 pts | `(rescued / total) * 20` (Level 3 only) |

---

## SafeZone — Script Reference

**File:** `Assets/Scripts/Gameplay/SafeZone.cs`
**Attach to:** GameObject with BoxCollider (IsTrigger ON) covering exit doorway.

| Inspector Field | Default | Notes |
|---|---|---|
| bIsCorrectExit | false | Set true only on the real emergency exit |

- Player (`VRPlayer` tag) enters → calls `ScoringManager.RecordExitUsed` → if correct, calls `GameManager.LevelComplete()`
- NPC (`NPCCharacter`) enters → calls `npc.OnRescued()`

---

## GameManager — Script Reference

**File:** `Assets/Scripts/Gameplay/GameManager.cs`
**Attach to:** Any GameObject in each Level scene.

| Inspector Field | Level1 | Level2 | Level3 |
|---|---|---|---|
| levelIndex | 1 | 2 | 3 |
| npcCount | 0 | 0 | 2 |

- `Start()` → calls `ScoringManager.InitLevel` and `HUDController.ShowFuelBar`
- `LevelComplete()` → calls `ScoringManager.BuildScoreData` → shows `ScoreScreen`
- `ReloadScene()`, `LoadNextLevel(int)`, `LoadMainMenu()` — static, called by ScoreScreen buttons

---

## AlarmAudio — Script Reference

**File:** `Assets/Scripts/Audio/AlarmAudio.cs`
**Attach to:** Any GameObject. Requires `AudioSource` (alarm WAV, Loop ON, Play On Awake ON).

- Every 1 second: pitch lerps 1.0→1.8, volume lerps 0.4→1.0 over `escalationDuration` (120s)

---

## VRPlayer — Script Reference

**File:** `Assets/Scripts/Player/VRPlayer.cs`
**Attach to:** OVRCameraRig root

| Inspector Field | Assign |
|---|---|
| rightGrabber | RightHandAnchor > OVRGrabber component |
| leftGrabber | LeftHandAnchor > OVRGrabber component |

- Tags self as "VRPlayer" in Awake (NPCCharacter and SafeZone detect by this tag)
- Each `Update()`: checks both `OVRGrabber.grabbedObject` for a `FireExtinguisher`
- On grab change: calls `NotifyGrabbed(true/false)` to track state
- Right trigger (`OVRInput.Axis1D.SecondaryIndexTrigger`) > 0.1 → `StartSpray`
- Right trigger < 0.05 → `StopSpray`

---

## NPCCharacter — Script Reference

**File:** `Assets/Scripts/NPC/NPCCharacter.cs`
**Attach to:** Character GameObject

### Required Components:
| Component | Settings |
|---|---|
| NavMeshAgent | Speed 2.0, Stopping Distance 1.0 |
| SphereCollider | Radius 2.0m, IsTrigger ON — player detection |
| AudioSource | For help/thank-you clips |

Add tag `NPC` to the GameObject.

| Inspector Field | Assign |
|---|---|
| helpClip | "Help! Over here!" WAV |
| thankYouClip | "Thank you!" WAV |
| audioSource | drag AudioSource |

- When player enters 2m sphere trigger → starts `InvokeRepeating("FollowPlayer", 0, 0.5)`
- `FollowPlayer`: `Physics.Linecast` to player; if `FireActor` blocks → `agent.isStopped = true`
- `OnRescued()` (called by SafeZone) → stops movement, calls `ScoringManager.RecordNPCRescued`

---

## TutorialPanel — Script Reference

**File:** `Assets/Scripts/UI/TutorialPanel.cs`
**Attach to:** Canvas root in Tutorial scene

### Canvas Hierarchy (build manually in Editor):
```
Canvas (World Space, 1.9m wide × 1.07m tall, facing player)
  └─ Panel (dark semi-transparent background)
  └─ VerticalLayout
       └─ RawImage "SlideImage" (500px tall)
       └─ TMP_Text "TitleText" (48pt, Bold, White, Center)
       └─ TMP_Text "BodyText" (24pt, Wrap, Light Gray, Center)
  └─ HorizontalLayout "Dots"
       └─ 6× Image (dot indicators)
  └─ Button "NextButton"  → wire to TutorialPanel
  └─ Button "StartButton" → wire to TutorialPanel (hidden by default)
```

Slides are pre-populated in code (6 slides). You can also add slides via Inspector (`slides` list).

---

## HUDController — Script Reference

**File:** `Assets/Scripts/UI/HUDController.cs`
**Attach to:** HUD Canvas

### Canvas Hierarchy:
```
Canvas (Screen Space - Camera)
  └─ TMP_Text "TimerText" (top-center, 64pt, Bold, White)
  └─ GameObject "FuelContainer" (bottom-right, hidden by default)
       └─ TMP_Text "CO2" label
       └─ Slider "FuelBar" (value binding: 0–1)
  └─ TMP_Text "HintText" (bottom-left, fades after 5s)
```

- `SetFuelLevel(float)` — update Slider value
- `ShowFuelBar(bool)` — show/hide FuelContainer (Level 2 & 3 only)
- Timer updates every 1 second via `InvokeRepeating`

---

## ScoreScreen — Script Reference

**File:** `Assets/Scripts/UI/ScoreScreen.cs`
**Attach to:** Score Screen Canvas (disabled by default)

Shown by `ScoreScreen.Show(ScoreData data)` — static method, no scene reference needed.

### Canvas Hierarchy:
```
Canvas (World Space, or Screen Space - Camera)
  └─ TMP_Text "LevelCompleteText"
  └─ TMP_Text "TimeRow"
  └─ GameObject "ExtinguisherContainer"
       └─ TMP_Text "ExtinguisherRow"
  └─ GameObject "TechniqueContainer"
       └─ TMP_Text "TechniqueRow"
  └─ TMP_Text "ExitRow"
  └─ GameObject "NPCContainer"
       └─ TMP_Text "NPCRow"
  └─ TMP_Text "TotalScoreText" (large, gold/silver/bronze by score)
  └─ TMP_Text "FeedbackText"
  └─ HorizontalLayout "Buttons"
       └─ Button "RetryButton"
       └─ Button "NextLevelButton"
       └─ Button "MainMenuButton"
```

Score colors: ≥80 gold `#FFA500`, ≥50 silver `#C0C0C0`, else bronze `#CD7F32`.

---

## Unity Particle System Setup

### FirePS (fire particle system)
1. Add > Effects > Particle System to FireActor child
2. **Main module:** Start Lifetime 0.5–1.5, Start Speed 1–2, Start Size 0.1–0.4, Start Color orange→gray
3. **Emission:** Rate over Time = 500 (will be overridden by script)
4. **Shape:** Cone, Angle 15°, Radius 0.1
5. **Color over Lifetime:** Orange (birth) → Dark Gray (death)
6. **Size over Lifetime:** Curve: 0→0.5→0 (grow then shrink)
7. **Velocity over Lifetime:** Y = 1–2 (upward drift), random XZ ±0.3
8. **Renderer:** Render Mode = Billboard, Material = fire sprite or additive particle

### SmokePS
- Gray color, larger particles (0.5–1m), slow upward drift, low emission rate (30/s)

### SprayPS (extinguisher spray)
- Color: White with slight blue tint
- Velocity: forward +Z 6m/s, tight cone ±10°
- Lifetime: 0.3–0.5s, Size: 0.05m
- Emission: 300/s
- **Enable Local Space** in Main module (moves with the nozzle)
- Auto-start OFF

---

## Level Layout Notes

### Tutorial Scene
- OVRCameraRig in center of room
- BP_TutorialPanel equivalent: 3D Canvas with TutorialPanel.cs on wall
- No fire, no extinguisher, no scoring

### Level1 Scene (~10m × 8m × 3m)
- 4 doors:
  - North: SafeZone (bIsCorrectExit=true, "Emergency Exit" label)
  - East: SafeZone (bIsCorrectExit=false, "Storage")
  - South: SafeZone (bIsCorrectExit=false, "Utility")
  - West: blocked (no SafeZone)
- 1 FireActor (NW corner), 2 furniture GameObjects tagged "Flammable"
- 1 AlarmAudio anywhere
- GameManager: levelIndex=1, npcCount=0

### Level2 Scene (~15m × 12m)
- 1 FireActor (center-right)
- 3 FireExtinguishers:
  - EXT_A: 3m from fire
  - EXT_B: 7m from fire
  - EXT_C: 12m from fire
- ExtinguisherSpawner (marks EXT_A as nearest)
- 1 correct exit SafeZone, 2 wrong exits
- GameManager: levelIndex=2, npcCount=0

### Level3 Scene (2-room building)
- Room A: FireActor in doorway, NPCCharacter_1
- Room B: FireActor in corner, NPCCharacter_2, 1 extinguisher
- Lobby: player start, 1 extinguisher
- SafeZone on lobby exit (bIsCorrectExit=true)
- NavMesh: Window > AI > Navigation > Bake (covers all 3 rooms)
- GameManager: levelIndex=3, npcCount=2

---

## Build & Deploy to Meta Quest

### First Build Setup
1. **File > Build Settings** → Android → Switch Platform
2. Add all 4 scenes in order (Tutorial first = index 0)
3. **Edit > Project Settings > Player > Android:**
   - Company Name, Product Name
   - Bundle ID: `com.student.vrfiretraining`
   - Minimum API: 29, Target: 32
   - IL2CPP, ARM64
   - Internet Access: Not Required
4. **Edit > Project Settings > XR Plug-in Management > Android:** enable Oculus
5. **Edit > Project Settings > Oculus:** set Target Devices = Quest 2, Quest 3

### Build & Install
```bash
# Build APK: File > Build Settings > Build (save as VRFireTraining.apk)

# Check Quest is connected via USB
adb devices

# Install APK
adb install -r VRFireTraining.apk

# Launch from Quest: Library > Unknown Sources > VRFireTraining
```

### Useful ADB Commands
```bash
# View Unity logcat
adb logcat -s Unity:* ActivityManager:*

# Capture screenshot from Quest
adb shell screencap -p /sdcard/screenshot.png && adb pull /sdcard/screenshot.png

# Check GPU performance
adb shell setprop debug.oculus.gpuLevel 4
```

---

## Common Errors & Fixes

| Error | Fix |
|---|---|
| `OVRGrabber` not found | Meta XR SDK not imported — install via Asset Store or manifest.json Meta registry |
| `NavMeshAgent` errors | `com.unity.ai.navigation` package missing in manifest.json |
| NPC doesn't move | NavMesh not baked — Window > AI > Navigation > Bake |
| Fire doesn't spread | Furniture GameObjects missing "Flammable" tag |
| Spray raycast misses | NozzleTip Transform not assigned in Inspector or facing wrong direction |
| Score always 0 | ScoringManager singleton destroyed — check DontDestroyOnLoad, only one instance per session |
| Grab not working | OVRGrabbable not on extinguisher; or OVRGrabber not on hand anchor |
| Alarm doesn't play | AudioSource: Play On Awake ON, Loop ON, clip assigned |
| Particles not visible | Check Renderer material is not null; check layer visibility in Scene view |
| Build fails on Android | Ensure IL2CPP + ARM64 selected, Vulkan added, OpenGLES removed |
| VR not activating | XR Plugin Management > Android: Oculus must be checked |
| NPCCharacter tag issue | Make sure NPC GameObject has tag "NPC" and VRPlayer has tag "VRPlayer" |
