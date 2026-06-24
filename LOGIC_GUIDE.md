# VR Fire Training — Full Logic Guide for Beginners

This document explains every script in the game in plain English, step by step.
No prior coding experience is assumed. Read it top to bottom once — by the end
you will understand exactly how the whole game works and how all the pieces fit together.

---

## Part 1 — How Unity Works (the basics you need)

Before reading the scripts, understand these four Unity ideas:

### 1. GameObjects and Components
Everything in a Unity scene is a **GameObject** — a fire, a wall, the player, a button.
A GameObject by itself does nothing. You attach **Components** to it to give it behaviour.
A script is just a component. So when we say "attach `FireActor.cs` to a GameObject",
we mean drag the script onto the object in the Unity editor — Unity then runs that script
for that object.

### 2. MonoBehaviour — the base class every script inherits from
Every script in this project starts with:
```csharp
public class SomeName : MonoBehaviour
```
The `: MonoBehaviour` part gives the script special Unity powers: it can be attached to
GameObjects, and Unity will automatically call certain **magic methods** on it:

| Method | When Unity calls it |
|---|---|
| `Awake()` | Once, the moment the object is created (before anything else) |
| `Start()` | Once, just before the first frame (after all Awakes have run) |
| `Update()` | Every single frame (~72 times per second on Quest) |
| `OnTriggerEnter(Collider other)` | When another collider overlaps a trigger collider on this object |
| `OnDestroy()` | When the object is deleted from the scene |

### 3. Singletons — one global manager everyone can talk to
Some objects in the game (GameManager, ScoringManager, HUDController) need to be
reachable from any other script without searching for them.
The pattern used is called a **singleton**:
```csharp
public static GameManager Instance { get; private set; }
void Awake() { Instance = this; }
```
`static` means "belongs to the class, not any specific object."
After Awake runs, any other script can just write `GameManager.Instance.LevelComplete()`
without needing a reference to the specific GameObject.

### 4. InvokeRepeating vs Update
`Update()` runs 72+ times per second. Most game logic doesn't need to run that often
and doing so wastes battery on the Quest.
`InvokeRepeating("MethodName", startDelay, interval)` calls a method on a timer.
Example: `InvokeRepeating("GrowFire", 1f, 1f)` calls `GrowFire` after 1 second,
then again every 1 second. Fire growth, NPC following, and the alarm all use this.

---

## Part 2 — The Big Picture: How the Game Flows

```
Player puts on Quest
        │
        ▼
  Tutorial.unity loads
  ──────────────────────────────────────────────────
  TutorialPanel shows 6 slides (RACE, PASS, etc.)
  Player presses "Start Training" button
        │
        ▼
  Level1.unity loads   ← "Escape" scene
  ──────────────────────────────────────────────────
  Fire grows in the room
  Alarm sound escalates
  Player finds correct exit (North door)
  SafeZone trigger → ScoringManager records exit
  GameManager.LevelComplete() → ScoreScreen appears
  Player presses "Next Level"
        │
        ▼
  Level2.unity loads   ← "Extinguisher" scene
  ──────────────────────────────────────────────────
  ExtinguisherSpawner marks nearest extinguisher
  Player grabs extinguisher → VRPlayer detects grab
  Player aims at fire base and squeezes trigger
  FireExtinguisher raycast hits FireActor
  FireActor.ExtinguishHit() lowers intensity
  Fire goes out → player exits → ScoreScreen
        │
        ▼
  Level3.unity loads   ← "NPC Rescue" scene
  ──────────────────────────────────────────────────
  Same as Level2 + 2 NPCs following player
  NPCCharacter follows player through rooms
  Stops if fire blocks the path
  SafeZone.OnTriggerEnter → npc.OnRescued()
  ScoringManager counts rescued NPCs
  Player exits → ScoreScreen → Main Menu
```

---

## Part 3 — The Scripts, One by One

The 12 scripts are split into 6 groups. Each section explains what the script does,
then walks through the actual code.

---

### GROUP 1: GAMEPLAY MANAGERS

---

#### `GameManager.cs`
**What it is:** The traffic controller of each level. It knows what level we're on,
how many NPCs need to be rescued, and calls other systems when the level ends.

**What it does:**
- Sets up the ScoringManager when the level starts
- Tells the HUD to show/hide the fuel bar
- Triggers the ScoreScreen when the player reaches the correct exit
- Provides static helpers to load the next scene

**Code walkthrough:**

```csharp
public static GameManager Instance { get; private set; }
```
Singleton pattern. Only one GameManager exists per scene; this line makes it
reachable from anywhere via `GameManager.Instance`.

```csharp
public int levelIndex = 1;
public int npcCount = 0;
```
These are set in the Unity Inspector. Level1 gets levelIndex=1, npcCount=0.
Level3 gets levelIndex=3, npcCount=2.

```csharp
void Start()
{
    ScoringManager.Instance.InitLevel(npcCount);
    HUDController.Instance?.ShowFuelBar(levelIndex >= 2);
}
```
`Start()` runs once when the level loads.
- Resets the scoring for this level (time, hits, etc.)
- Shows the fuel bar only in Level 2 and Level 3 (where you use an extinguisher)
- The `?.` is a safety check — "only call this if Instance isn't null"

```csharp
public void LevelComplete()
{
    ScoreData data = ScoringManager.Instance?.BuildScoreData() ?? default;
    ScoreScreen.Show(data);
}
```
Called by SafeZone when the player walks through the correct exit.
Collects all the score data and tells ScoreScreen to appear.
`?? default` means "if BuildScoreData returns null, use the default empty ScoreData".

```csharp
public static void LoadNextLevel(int currentIndex)
{
    string next = currentIndex switch
    {
        1 => "Level2",
        2 => "Level3",
        _ => "Tutorial",
    };
    SceneManager.LoadScene(next);
}
```
A `switch` expression matches the current level index to the next scene name.
`_` is the default case — if level is anything other than 1 or 2, go back to Tutorial.

---

#### `ScoringManager.cs`
**What it is:** The scorebook. It remembers everything the player did during a level
(how long they took, whether they aimed at the base, whether they used the nearest
extinguisher, which exit they used, how many NPCs they saved) and calculates a final score.

**What it does:**
- Persists across scene loads (DontDestroyOnLoad)
- Records events when other scripts call its methods
- Calculates a score out of 100 when the level ends

**Code walkthrough:**

```csharp
void Awake()
{
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```
The singleton guard: if a ScoringManager already exists (from a previous scene load),
destroy this new one. Otherwise, register as the Instance and survive scene changes.
This means the very first ScoringManager created (in Level1) lives for the whole game.

```csharp
public void InitLevel(int npcCount)
{
    levelStartTime = Time.time;
    npcsTotal = npcCount;
    totalSprayHits = 0;
    baseSprayHits = 0;
    pickedUpNearestExtinguisher = false;
    correctExitUsed = false;
    npcsRescued = 0;
}
```
Called by GameManager.Start(). `Time.time` is the number of seconds since the app launched.
Recording the start time lets us calculate elapsed time later.
Everything else is reset to zero/false to start fresh.

```csharp
public int CalculateFinalScore()
{
    float timeTaken = Time.time - levelStartTime;
    float timeScore = Mathf.Clamp((120f - timeTaken) / 120f * 40f, 0f, 40f);
    ...
    float total = levelIndex switch
    {
        1 => timeScore + exitScore,
        2 => timeScore + techScore + exitScore + extScore,
        _ => timeScore + techScore + npcScore + extScore,
    };
    return Mathf.FloorToInt(total);
}
```
The score formula:

| What | Max | Only in |
|---|---|---|
| Time (under 120 s) | 40 pts | All levels |
| Technique (% base hits) | 30 pts | Level 2, 3 |
| Correct exit | 20 pts | All levels |
| Nearest extinguisher | 10 pts | Level 2, 3 |
| NPC rescue | 20 pts | Level 3 |

`Mathf.Clamp(value, 0, 40)` prevents the score going negative if the player takes longer
than 120 seconds. `Mathf.FloorToInt` rounds down (87.9 → 87).

---

### GROUP 2: FIRE

---

#### `FireActor.cs`
**What it is:** The fire itself. Each fire GameObject in the scene has this script.
It grows over time, can spread to nearby flammable objects, and can be put out
by a fire extinguisher hitting it.

**What it does:**
- Grows intensity every second (makes particles bigger and louder)
- After 15 seconds, tries to spread to objects tagged "Flammable" nearby
- Responds to hits from the extinguisher via `ExtinguishHit()`
- When fully extinguished, stops all particles and audio

**Code walkthrough:**

```csharp
public enum FireHitZone { Base, Top, Middle }
```
An enum is a named list of options. A fire can be hit in three zones.
The base is the most effective (cuts off the fuel source — like real fire fighting).

```csharp
void Start()
{
    InvokeRepeating(nameof(GrowFire), 1f, 1f);
    InvokeRepeating(nameof(CheckSpread), 5f, 5f);
}
```
`nameof(GrowFire)` is just the string `"GrowFire"` but safer to write because if you rename
the method, the compiler will catch it. This sets up two repeating timers:
- GrowFire runs every 1 second
- CheckSpread runs every 5 seconds

```csharp
void GrowFire()
{
    if (IsExtinguished) return;
    spreadTimer += 1f;
    fireIntensity = Mathf.Clamp(fireIntensity + growthRate, 0f, maxIntensity);
    ApplyVFX();
}
```
Every second, intensity increases by 0.05 (growthRate). `Mathf.Clamp` keeps it between 0
and 1. Then ApplyVFX() updates the particle system to match the new intensity.

```csharp
void ApplyVFX()
{
    var emission = fireVFX.emission;
    emission.rateOverTime = fireIntensity * 1000f;
    fireVFX.transform.localScale = Vector3.one * fireIntensity;
}
```
`emission.rateOverTime` is how many particles spawn per second.
At full intensity (1.0): 1000 particles/sec. At start (0.1): 100 particles/sec.
The fire also physically scales up as it grows.

```csharp
void CheckSpread()
{
    if (!canSpread || IsExtinguished || spreadTimer < spreadTime) return;
    canSpread = false;
    Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
    foreach (var hit in hits)
    {
        if (hit.CompareTag("Flammable"))
            Instantiate(fireActorPrefab, hit.transform.position, Quaternion.identity);
    }
}
```
`Physics.OverlapSphere` is like casting an invisible sphere outward and collecting everything
it touches. If any of those objects are tagged "Flammable", a new fire prefab is spawned on
top of them. `canSpread = false` ensures fire only spreads once.
`Quaternion.identity` means "no rotation" (upright).

```csharp
public void ExtinguishHit(FireHitZone zone)
{
    float reduction = (zone == FireHitZone.Base) ? 0.15f : 0.03f;
    fireIntensity = Mathf.Clamp(fireIntensity - reduction, 0f, maxIntensity);

    if (fireIntensity <= 0f)
    {
        IsExtinguished = true;
        fireVFX?.Stop();
        smokeVFX?.Stop();
        heatHazeVFX?.Stop();
        fireCrackle?.Stop();
        OnFireExtinguished?.Invoke();
    }
}
```
Called by FireExtinguisher every 0.1 seconds while spraying.
- Base hit: −0.15 intensity (PASS technique — aim at the base)
- Top/Middle hit: −0.03 intensity (5× less effective, teaching bad technique)

When intensity reaches 0, everything stops and the `OnFireExtinguished` event fires.
Events are like public announcements — any script that "subscribed" to this event gets notified.
The `?.` before Stop() means "only call Stop() if the reference isn't null"
(in case audio or particles weren't assigned in the Inspector).

---

### GROUP 3: EXTINGUISHER

---

#### `FireExtinguisher.cs`
**What it is:** The fire extinguisher the player picks up and aims.
It tracks its own fuel level, fires a raycast from the nozzle, and tells FireActor
which zone was hit.

**What it does:**
- Knows when it is grabbed/dropped (told by VRPlayer)
- Drains fuel while spraying
- Fires an invisible ray from the nozzle tip
- If the ray hits a fire collider, calculates which zone and calls ExtinguishHit
- Updates the HUD fuel bar every 0.1 seconds

**Code walkthrough:**

```csharp
[RequireComponent(typeof(OVRGrabbable))]
public class FireExtinguisher : MonoBehaviour
```
`[RequireComponent]` is an instruction to Unity: "this script needs OVRGrabbable on the same
GameObject — add it automatically." OVRGrabbable is from Meta's SDK and handles the physics
of picking things up in VR (snapping to hand, applying grab physics).

```csharp
public void NotifyGrabbed(bool grabbed)
{
    if (grabbed == IsGrabbed) return;
    IsGrabbed = grabbed;
    if (!grabbed) StopSpray();
    else ScoringManager.Instance?.RecordExtinguisherPickup(isNearestExtinguisher);
}
```
Called by VRPlayer when the grab state changes.
If you drop the extinguisher: spray stops.
If you pick it up: ScoringManager records whether you grabbed the nearest one
(set by ExtinguisherSpawner at scene start).

```csharp
public void StartSpray()
{
    if (fuelLevel <= 0f || !IsGrabbed || IsSpraying) return;
    IsSpraying = true;
    sprayVFX?.Play();
    sprayAudio?.Play();
    sprayCoroutine = StartCoroutine(SprayTick());
}
```
Guard checks: no fuel → can't spray. Not held → can't spray. Already spraying → don't start twice.
Then plays visual and audio effects and starts the SprayTick coroutine.

**What is a Coroutine?**
A coroutine is a method that can pause mid-execution and resume later.
`StartCoroutine(SprayTick())` starts running SprayTick, but when it hits
`yield return new WaitForSeconds(0.1f)`, it pauses for 0.1 seconds and lets Unity
run everything else, then resumes. This is how you run something every 0.1 seconds
without blocking the game.

```csharp
IEnumerator SprayTick()
{
    while (IsSpraying && fuelLevel > 0f)
    {
        fuelLevel = Mathf.Clamp(fuelLevel - drainRate * 0.1f, 0f, 1f);
        HUDController.Instance?.SetFuelLevel(fuelLevel);

        if (nozzleTip != null)
        {
            Ray ray = new Ray(nozzleTip.position, nozzleTip.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, sprayRange))
            {
                FireActor fire = hit.collider.GetComponentInParent<FireActor>();
                if (fire != null)
                {
                    float baseY = fire.transform.position.y;
                    FireHitZone zone;
                    if (hit.point.y < baseY + 0.30f)      zone = FireHitZone.Base;
                    else if (hit.point.y > baseY + 0.80f) zone = FireHitZone.Top;
                    else                                    zone = FireHitZone.Middle;

                    fire.ExtinguishHit(zone);
                    ScoringManager.Instance?.RecordSprayHit(zone);
                }
            }
        }

        if (fuelLevel <= 0f) StopSpray();
        yield return new WaitForSeconds(0.1f);
    }
}
```
This is the heart of the extinguisher logic. Every 0.1 seconds:

1. Drain fuel slightly
2. Update the fuel bar on the HUD
3. Cast a Ray from the nozzle tip in the direction the nozzle is pointing
4. `Physics.Raycast` shoots that invisible ray and returns what it hits (up to sprayRange = 3 m)
5. If it hit something, check if the hit object (or its parent) has a `FireActor` script
6. Compare the Y position of the hit point to the fire's base Y to determine the zone:
   - Below 0.30 m above base → Base hit (most effective)
   - Above 0.80 m above base → Top hit (least effective)
   - In between → Middle hit
7. Tell the fire it was hit, and tell ScoringManager to record the hit type

---

#### `ExtinguisherSpawner.cs`
**What it is:** A tiny helper script that runs once when Level2 or Level3 loads.
Its only job is to figure out which extinguisher is closest to the fire and mark it.

**Code walkthrough:**

```csharp
void Start()
{
    Invoke(nameof(FindNearest), 0.5f);
}
```
Wait 0.5 seconds before running FindNearest. This gives all other scripts time to
finish their own `Start()` methods first. Without this delay, FireActor or
FireExtinguisher objects might not be fully initialized yet.

```csharp
void FindNearest()
{
    FireActor[] fires = FindObjectsOfType<FireActor>();
    FireExtinguisher[] extinguishers = FindObjectsOfType<FireExtinguisher>();

    Vector3 firePos = fires[0].transform.position;
    FireExtinguisher nearest = null;
    float nearestDist = float.MaxValue;

    foreach (var ext in extinguishers)
    {
        float dist = Vector3.Distance(ext.transform.position, firePos);
        if (dist < nearestDist)
        {
            nearestDist = dist;
            nearest = ext;
        }
    }

    if (nearest != null)
        nearest.isNearestExtinguisher = true;
}
```
`FindObjectsOfType<T>()` searches the whole scene for objects with that script attached.
Then it loops through every extinguisher, calculates distance to the first fire,
and keeps track of which one is closest (`float.MaxValue` is the largest possible float,
so any real distance will be smaller on the first check).
The closest one gets `isNearestExtinguisher = true` — FireExtinguisher.NotifyGrabbed()
then reports this to ScoringManager when the player picks it up.

---

### GROUP 4: PLAYER

---

#### `VRPlayer.cs`
**What it is:** The script that bridges the VR controllers to the game logic.
It watches what both hands are holding and routes trigger button presses to
whichever FireExtinguisher the player is holding.

**Code walkthrough:**

```csharp
public OVRGrabber rightGrabber;
public OVRGrabber leftGrabber;
```
These are assigned in the Unity Inspector by dragging the OVRGrabber components
from each hand anchor of the OVRCameraRig. OVRGrabber is Meta's script that tracks
what each hand is physically holding.

```csharp
void Awake()
{
    gameObject.tag = "VRPlayer";
}
```
Tags this object so other scripts (SafeZone, NPCCharacter) can find the player
using `CompareTag("VRPlayer")` without needing a direct reference.

```csharp
void Update()
{
    TrackGrab(rightGrabber, OVRInput.Axis1D.SecondaryIndexTrigger);
    TrackGrab(leftGrabber,  OVRInput.Axis1D.PrimaryIndexTrigger);
}
```
Every frame, check both hands. "SecondaryIndexTrigger" is the right index trigger;
"PrimaryIndexTrigger" is the left.

```csharp
void TrackGrab(OVRGrabber grabber, OVRInput.Axis1D triggerAxis)
{
    if (grabber == null) return;

    OVRGrabbable grabbed = grabber.grabbedObject;
    FireExtinguisher ext = grabbed != null ? grabbed.GetComponent<FireExtinguisher>() : null;

    if (ext != heldExtinguisher)
    {
        heldExtinguisher?.NotifyGrabbed(false);   // tell old extinguisher it was dropped
        heldExtinguisher = ext;
        heldExtinguisher?.NotifyGrabbed(true);    // tell new extinguisher it was picked up
    }

    if (heldExtinguisher != null)
    {
        float trigger = OVRInput.Get(triggerAxis);
        if (trigger > 0.1f)       heldExtinguisher.StartSpray();
        else if (trigger < 0.05f) heldExtinguisher.StopSpray();
    }
}
```
Step by step:
1. Get whatever this hand is currently grabbing (`grabber.grabbedObject`)
2. Check if that grabbed object has a FireExtinguisher script on it
3. If the held extinguisher changed (picked up or dropped), notify the old and new ones
4. Read how far the trigger is pressed (0.0 = not pressed, 1.0 = fully pressed)
5. If pressed more than 10%: start spraying. If released below 5%: stop.
   (The 5%/10% gap prevents flickering on/off at the threshold)

---

### GROUP 5: NPC

---

#### `NPCCharacter.cs`
**What it is:** An NPC (non-player character) stuck in the building.
When the player gets close, the NPC starts following them to safety.
If fire blocks the path, the NPC waits for it to be cleared.

**Code walkthrough:**

```csharp
[RequireComponent(typeof(NavMeshAgent))]
public class NPCCharacter : MonoBehaviour
```
`NavMeshAgent` is Unity's pathfinding component. It moves a character along a
pre-baked navigation mesh (the walkable floor area). That's why Level3's NavMesh
must be baked in the editor before building — without it, the NPC can't navigate.

```csharp
void OnTriggerEnter(Collider other)
{
    if (isFollowing || IsRescued) return;
    if (!other.CompareTag("VRPlayer")) return;

    isFollowing = true;
    followTarget = other.transform;
    InvokeRepeating(nameof(FollowPlayer), 0f, 0.5f);

    if (audioSource != null && helpClip != null)
        audioSource.PlayOneShot(helpClip);
}
```
`OnTriggerEnter` fires when something enters the NPC's SphereCollider (radius 2 m).
It ignores everything except the player (`CompareTag("VRPlayer")`).
When the player enters the 2 m bubble:
- Set followTarget to the player's Transform (position and rotation data)
- Start `FollowPlayer` repeating every 0.5 seconds (not every frame — saves CPU)
- Play the "Help! Over here!" voice clip once

```csharp
void FollowPlayer()
{
    if (!isFollowing || IsRescued || followTarget == null)
    {
        CancelInvoke(nameof(FollowPlayer));
        return;
    }

    if (Physics.Linecast(transform.position, followTarget.position, out RaycastHit hit))
    {
        if (hit.collider.GetComponentInParent<FireActor>() != null)
        {
            agent.isStopped = true;
            return;
        }
    }

    agent.isStopped = false;
    agent.SetDestination(followTarget.position);
}
```
`Physics.Linecast` draws a straight invisible line between the NPC and the player.
If the line hits something that has a FireActor (fire), the NPC stops moving
(`agent.isStopped = true`) — this simulates the NPC being blocked by flames.
If the path is clear, it tells the NavMeshAgent to walk toward the player's position.
`SetDestination` is called every 0.5 seconds so the NPC continuously updates where
the player has moved to.

```csharp
public void OnRescued()
{
    if (IsRescued) return;
    IsRescued = true;
    isFollowing = false;
    CancelInvoke(nameof(FollowPlayer));
    agent.isStopped = true;
    ScoringManager.Instance?.RecordNPCRescued();
    if (audioSource != null && thankYouClip != null)
        audioSource.PlayOneShot(thankYouClip);
}
```
Called by SafeZone when the NPC walks into the correct exit.
Stops all movement, records the rescue in ScoringManager, plays the thank-you clip.

---

### GROUP 6: UI

---

#### `TutorialPanel.cs`
**What it is:** The tutorial slide show displayed on a 3D canvas in the Tutorial scene.
6 slides teach the player RACE, PASS technique, and emergency exits.

**Code walkthrough:**

```csharp
public List<TutorialSlide> slides = new();

void Start()
{
    if (slides.Count == 0) PopulateDefaultSlides();
    nextButton.onClick.AddListener(OnNext);
    startButton.onClick.AddListener(OnStart);
    UpdateDisplay();
}
```
If no slides were manually added in the Inspector, it fills in 6 default slides.
`AddListener` wires up the buttons: clicking "Next" calls `OnNext()`,
clicking "Start Training" calls `OnStart()`.
`UpdateDisplay()` shows the first slide immediately.

```csharp
void OnNext()
{
    slideIndex = Mathf.Min(slideIndex + 1, slides.Count - 1);
    UpdateDisplay();
}
```
Advance the slide index but never go past the last slide.
`Mathf.Min` returns the smaller of two values — it keeps the index capped.

```csharp
void UpdateDisplay()
{
    titleText.text = slide.title;
    bodyText.text  = slide.body;

    for (int i = 0; i < dotImages.Length; i++)
        dotImages[i].color = new Color(1, 1, 1, i <= slideIndex ? 1f : 0.3f);

    bool isLast = slideIndex >= slides.Count - 1;
    nextButton.gameObject.SetActive(!isLast);
    startButton.gameObject.SetActive(isLast);
}
```
Sets the title and body text for the current slide.
Loops through the 6 dot indicators: dots at or before the current slide are fully
white (alpha 1.0), dots after it are faded (alpha 0.3).
On the last slide, hides "Next" and shows "Start Training" instead.

```csharp
[System.Serializable]
public class TutorialSlide
{
    public string title;
    [TextArea(3, 6)] public string body;
    public Texture2D image;
}
```
`[System.Serializable]` makes this class show up in the Unity Inspector so you can
edit slides without touching code. `[TextArea]` makes the body text field multiline
in the Inspector (min 3 rows, max 6).

---

#### `HUDController.cs`
**What it is:** The heads-up display. Shows a timer at the top, a fuel bar bottom-right
(Level 2 & 3 only), and a hint message that fades after 5 seconds.

**Code walkthrough:**

```csharp
void Start()
{
    levelStartTime = Time.time;
    InvokeRepeating(nameof(UpdateTimer), 1f, 1f);
    if (hintText != null) Invoke(nameof(FadeHint), 5f);
}
```
Records when the level started. Updates the timer every 1 second.
Schedules the hint text to disappear after 5 seconds.

```csharp
void UpdateTimer()
{
    float elapsed = Time.time - levelStartTime;
    int minutes = Mathf.FloorToInt(elapsed / 60f);
    int seconds = Mathf.FloorToInt(elapsed % 60f);
    timerText.text = $"{minutes}:{seconds:D2}";
}
```
`elapsed % 60` gives the remainder after dividing by 60 (e.g. 75 seconds → 15 seconds).
`D2` means "always show at least 2 digits" so 5 seconds shows as `:05` not `:5`.

```csharp
public void SetFuelLevel(float level)  { if (fuelBar != null) fuelBar.value = level; }
public void ShowFuelBar(bool show)     { if (fuelContainer != null) fuelContainer.SetActive(show); }
```
Simple setters. `fuelBar.value` moves the slider from 0 (empty) to 1 (full).
`SetActive(false)` hides the fuel bar in Level 1 where there's no extinguisher.

---

#### `ScoreScreen.cs`
**What it is:** The results screen that appears when a level ends.
Shows a breakdown of points, a coloured total (gold/silver/bronze),
feedback text, and three buttons (Retry, Next Level, Main Menu).

**Code walkthrough:**

```csharp
void Awake()
{
    Instance = this;
    gameObject.SetActive(false);
}

public static void Show(ScoreData data)
{
    if (Instance == null) return;
    Instance.gameObject.SetActive(true);
    Instance.Populate(data);
}
```
Starts hidden. The static `Show()` method makes the canvas visible and fills it with data.
Because it's static, GameManager can call `ScoreScreen.Show(data)` without a direct reference.

```csharp
void Populate(ScoreData d)
{
    levelCompleteText.text = $"Level {d.levelIndex} Complete!";
    timeRow.text = $"Time: {d.timeTaken:F0}s  —  {d.timePoints} pts";

    bool showExt = d.levelIndex >= 2;
    extinguisherContainer?.SetActive(showExt);
    techniqueContainer?.SetActive(showExt);
    ...

    totalScoreText.color = d.grandTotal >= 80 ? new Color(1f, 0.647f, 0f) :   // gold
                           d.grandTotal >= 50 ? new Color(0.753f, 0.753f, 0.753f) :  // silver
                                                new Color(0.804f, 0.498f, 0.196f);   // bronze
}
```
`{d.timeTaken:F0}` formats the float to 0 decimal places (whole seconds).
`extinguisherContainer.SetActive(showExt)` hides the extinguisher row in Level 1
where those points don't apply. The colour uses if-else-if logic written as a ternary chain.

```csharp
retryButton.onClick.RemoveAllListeners();
retryButton.onClick.AddListener(GameManager.ReloadScene);
```
`RemoveAllListeners()` clears any previous wiring first (important if ScoreScreen
is reused across level retries). Then hooks up the button to reload the current scene.

---

### GROUP 7: AUDIO

---

#### `AlarmAudio.cs`
**What it is:** An ambient alarm sound that escalates over 120 seconds.
Pitch and volume increase the longer the player takes, creating urgency.

**Code walkthrough:**

```csharp
void Start()
{
    audioSource = GetComponent<AudioSource>();
    InvokeRepeating(nameof(EscalateAlarm), 1f, 1f);
}
```
Grabs the AudioSource on the same GameObject (alarm WAV must be assigned in Inspector,
Loop ON, Play On Awake ON). Starts escalating every second.

```csharp
void EscalateAlarm()
{
    currentTime += 1f;
    float alpha = Mathf.Clamp01(currentTime / escalationDuration);
    audioSource.pitch  = Mathf.Lerp(1.0f, 1.8f, alpha);
    audioSource.volume = Mathf.Lerp(0.4f, 1.0f, alpha);
}
```
`alpha` goes from 0.0 (start) to 1.0 (after 120 seconds). `Mathf.Clamp01` keeps it
in that range so it never exceeds 1.0 even if the player takes longer than 120 seconds.
`Mathf.Lerp(a, b, t)` linearly interpolates: at t=0 returns a, at t=1 returns b,
at t=0.5 returns exactly halfway between. So:
- At start: pitch = 1.0, volume = 0.4 (calm)
- After 60 s: pitch = 1.4, volume = 0.7 (urgent)
- After 120 s: pitch = 1.8, volume = 1.0 (maximum alarm)

---

### GROUP 8: EXITS

---

#### `SafeZone.cs`
**What it is:** An invisible trigger volume placed over each doorway.
When the player or an NPC walks through, it decides what happens.

**Code walkthrough:**

```csharp
public bool bIsCorrectExit;
```
Set in the Inspector. Only the real emergency exit gets `true`.
All other doors get `false`.

```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("VRPlayer"))
    {
        ScoringManager.Instance?.RecordExitUsed(bIsCorrectExit);
        if (bIsCorrectExit)
            GameManager.Instance?.LevelComplete();
    }
    else
    {
        NPCCharacter npc = other.GetComponent<NPCCharacter>();
        if (npc != null) npc.OnRescued();
    }
}
```
Unity calls `OnTriggerEnter` automatically when any Collider enters this BoxCollider
(which has `IsTrigger ON`).

Two cases:
1. **It's the player** → Record whether the exit was correct. If correct, end the level.
   Wrong exit: the game records the penalty but doesn't end — the player must find the right one.
2. **It's not the player** → Check if it has an NPCCharacter script. If yes, call OnRescued().
   This is how NPCs get credit for escaping: they physically walk through the exit door.

---

## Part 4 — How All the Scripts Talk to Each Other

Here is a diagram of which script calls which:

```
VRPlayer ──────────────────────────────────► FireExtinguisher
   │                                              │
   │  (detects grab via OVRGrabber)               │  NotifyGrabbed()
   │  (reads trigger via OVRInput)                │
   │                                              ▼
   │                                        ScoringManager
   │                                         RecordExtinguisherPickup()
   │                                         RecordSprayHit()
   │                                              ▲
   │                                              │
   └──────────────────────────────────► FireActor.ExtinguishHit()
                                              │
                                              │  Physics.Raycast hits fire
                                              │  → determines zone
                                              ▼
                                        ScoringManager (again)

SafeZone ──► player enters ──────────────► ScoringManager.RecordExitUsed()
           │                              GameManager.LevelComplete()
           │                                    │
           │                                    ▼
           │                              ScoringManager.BuildScoreData()
           │                              ScoreScreen.Show(data)
           │
           └─► NPC enters ──────────────► NPCCharacter.OnRescued()
                                              │
                                              ▼
                                        ScoringManager.RecordNPCRescued()

NPCCharacter ◄── player enters 2m sphere ── OnTriggerEnter
           │
           └── FollowPlayer (every 0.5s) ──► NavMeshAgent.SetDestination()
                                              Physics.Linecast (fire check)

GameManager ──► Start() ──────────────────► ScoringManager.InitLevel()
                                            HUDController.ShowFuelBar()

ExtinguisherSpawner ──► FindNearest() ───► FireExtinguisher.isNearestExtinguisher = true

FireExtinguisher ──► SprayTick() ────────► HUDController.SetFuelLevel()

AlarmAudio ──► EscalateAlarm() ──────────► AudioSource.pitch / .volume
```

---

## Part 5 — The Score, From Start to Finish

Here is the exact journey of one data point — the **time score** — to show how data flows:

1. `ScoringManager.InitLevel()` runs → records `levelStartTime = Time.time` (e.g. 120.0 s)
2. Player plays the level for 45 seconds
3. Player walks through correct exit → `SafeZone.OnTriggerEnter` fires
4. SafeZone calls `GameManager.Instance.LevelComplete()`
5. GameManager calls `ScoringManager.Instance.BuildScoreData()`
6. Inside BuildScoreData: `timeTaken = Time.time - levelStartTime` = 165.0 − 120.0 = **45 seconds**
7. `timePoints = Mathf.FloorToInt((120 − 45) / 120 × 40)` = floor(25) = **25 points**
8. `ScoreData` struct is created with all values filled in
9. `ScoreScreen.Show(data)` is called
10. `ScoreScreen.Populate(data)` runs → `timeRow.text = "Time: 45s — 25 pts"`
11. Total is coloured gold/silver/bronze based on grandTotal value

---

## Part 6 — Common Beginner Questions

**Q: Why does the fire not spread even though there are Flammable objects?**
A: The GameObjects need the tag "Flammable" assigned in their Inspector (the Tag dropdown
at the top of the Inspector). The SceneBuilder editor script creates them with the right tag,
but check it in the Inspector to confirm.

**Q: Why does the alarm not play?**
A: The AudioSource on the AlarmAudio GameObject needs: (1) an audio clip assigned,
(2) Loop = ON, (3) Play On Awake = ON.

**Q: Why does the NPC not move?**
A: Either the NavMesh hasn't been baked (Window → AI → Navigation → Bake), or the NPC
is not on the NavMesh surface. Make sure the NPC's starting position is on the floor,
not floating.

**Q: Why is the score always 0?**
A: ScoringManager uses DontDestroyOnLoad and is a singleton. If you load straight into
Level2 in the editor without going through Tutorial→Level1 first, ScoringManager's
`InitLevel()` may not have been called. Always play from Tutorial (scene index 0)
or manually call InitLevel() in testing.

**Q: Why does the spray not hit the fire?**
A: The NozzleTip Transform must point forward (the +Z axis must face away from the
nozzle, toward the fire). Select the NozzleTip child in the scene and check the blue
arrow (Z-axis) points in the spray direction. Also make sure sprayRange (3 m default)
is long enough to reach the fire.

**Q: Why does grabbing not work?**
A: OVRGrabbable must be on the extinguisher, and OVRGrabber must be on each hand anchor
of the OVRCameraRig. These are Meta XR SDK components — they won't appear until the
SDK is imported and the real OVRCameraRig prefab is in the scene (replacing the
placeholder the SceneBuilder created).

---

*End of Logic Guide — VR Fire Training Project*
