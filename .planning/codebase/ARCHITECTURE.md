<!-- refreshed: 2026-06-22 -->
# Architecture

**Analysis Date:** 2026-06-22

## System Overview

```text
┌─────────────────────────────────────────────────────────────────────┐
│                     Unity Scenes (Single-load)                       │
│     City.unity          Bar.unity           Home.unity               │
└────────┬────────────────────────┬───────────────────┬───────────────┘
         │ door triggers          │ door triggers      │ door triggers
         ▼                        ▼                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                  Core / Managers (per-scene GameObjects)             │
│   DrunkManager          PlayerCarController      CommandQueue        │
│  `Core/Managers/`       `Core/Managers/`         `Patterns/Command/`│
└────────┬────────────────────────┬───────────────────────────────────┘
         │ EffectIntensity (read  │ EnterCar/ExitCar
         │ every frame by all     │ commands
         ▼ consumers)             ▼
┌────────────────────┐  ┌────────────────────────────────────────────┐
│  Drunk Effect      │  │          Gameplay Consumers                 │
│  Consumers         │  │  PlayerMovement  MouseLook  CarController  │
│ (read DrunkManager │  │  CarFollowCamera  PlayerPickup              │
│  .EffectIntensity) │  │  `Gameplay/Player/` `Gameplay/Vehicles/`   │
└────────────────────┘  └────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Static In-Memory Persistence (cross-scene)              │
│  PlayerSpawner.NextSpawnId   CarStateStore   DeliveredObjectsStore   │
│  PlayerPickup.hasHeldObject (static field)                           │
│  `Core/SceneManagement/`                                             │
└─────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Cross-scene Singleton (DontDestroyOnLoad)               │
│                    BackgroundMusicManager                             │
│                    `Core/Audio/`                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| `DrunkManager` | Source of truth for alcohol level; exposes `EffectIntensity` scalar | `Assets/_Project/Core/Managers/DrunkManager.cs` |
| `PlayerCarController` | Toggles player↔car mode; manages active cameras and AudioListeners | `Assets/_Project/Core/Managers/PlayerCarController.cs` |
| `CommandQueue` | Dequeues one `ICommand` per `Update` tick; used for car enter/exit | `Assets/_Project/Patterns/Command/CommandQueue.cs` |
| `BackgroundMusicManager` | DontDestroyOnLoad singleton; loads BGM from Resources with fallback paths | `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` |
| `PlayerMovement` | FPS character movement; applies lateral sway + input drift + speed penalty scaled by `EffectIntensity` | `Assets/_Project/Gameplay/Player/PlayerMovement.cs` |
| `MouseLook` | FPS camera rotation; applies pitch/yaw/roll sinusoidal wobble scaled by `EffectIntensity` | `Assets/_Project/Gameplay/Player/MouseLook.cs` |
| `PlayerPickup` | Raycast + overlap-sphere item detection; picks up drinks and carryable objects | `Assets/_Project/Gameplay/Player/PlayerPickup.cs` |
| `CarController` | Rigidbody car driving; applies steer/throttle drift + yaw jitter scaled by `EffectIntensity` | `Assets/_Project/Gameplay/Vehicles/CarController.cs` |
| `CarFollowCamera` | Smooth-follow camera for car; applies roll/pitch/yaw wobble scaled by `EffectIntensity` | `Assets/_Project/Gameplay/Vehicles/CarFollowCamera.cs` |
| `CarEnterExit` | Proximity trigger on car; enqueues `EnterCarCommand` when player presses E | `Assets/_Project/Gameplay/Vehicles/CarEnterExit.cs` |
| `ParkingSpot` | Trigger zone; tracks car inside and exposes `CanExit()` speed gate | `Assets/_Project/Gameplay/Systems/ParkingSpot.cs` |
| `PickupItem` | Drinkable item; resolves type by name, exposes `AlcoholPerSip`; handles highlight via `MaterialPropertyBlock` | `Assets/_Project/Gameplay/Items/PickupItem.cs` |
| `CarryableObject` | Physical object the player carries; auto-hides on Awake if already taken via `DeliveredObjectsStore` | `Assets/_Project/Gameplay/Items/CarryableObject.cs` |
| `PlayerSpawner` | On `Start`, reads `NextSpawnId` static field and teleports player to matching `SpawnPoint` | `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` |
| `SpawnPoint` | Named marker in a scene; matched by `PlayerSpawner.NextSpawnId` | `Assets/_Project/Core/SceneManagement/SpawnPoint.cs` |
| `CarStateStore` | Static class; stores car position/rotation so it survives scene reload | `Assets/_Project/Core/SceneManagement/CarStateStore.cs` |
| `DeliveredObjectsStore` | Static class; `HashSet<string>` of taken object IDs so they don't respawn | `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` |
| `BarDoorTrigger` | City → Bar transition; saves car state, loads Bar scene | `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` |
| `BarExitTrigger` | Bar → City transition; sets `PlayerSpawner.NextSpawnId = "BarFront"`, loads City | `Assets/_Project/Core/SceneManagement/BarExitTrigger.cs` |
| `CityHomeDoorTrigger` | City → Home transition; saves car state, loads Home scene | `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` |
| `HomeDoorTrigger` | Home → City transition; loads City scene | `Assets/_Project/Core/SceneManagement/HomeDoorTrigger.cs` |
| `CityBuilder` | Editor-only tool; procedurally generates City.unity layout from Kenney FBX assets | `Assets/_Project/Editor/CityBuilder.cs` |

## Pattern Overview

**Overall:** Event-driven observer for alcohol state + push-pull consumer pattern + Command pattern for deferred actions + static stores for cross-scene persistence.

**Key Characteristics:**
- `DrunkManager` owns one integer (`alcoholLevel`) and a derived float (`EffectIntensity`). It does not apply effects itself.
- Every drunk-effect consumer caches `DrunkManager` in `Awake` (with `FindFirstObjectByType` fallback) and reads `EffectIntensity` per frame.
- All drunk effects use sinusoidal modulation: `Mathf.Sin(Time.time * frequency) * amount * drunkAmount`.
- Scene transitions use `LoadSceneAsync` in Single mode (full destroy/recreate); continuity is provided entirely by static in-memory fields.
- Car enter/exit is deferred through `CommandQueue` (one command per Update) to avoid mid-frame state changes.

## Layers

**Core — Managers:**
- Purpose: Global runtime state (alcohol level, player-car mode)
- Location: `Assets/_Project/Core/Managers/`
- Contains: `DrunkManager`, `PlayerCarController`
- Depends on: `Patterns/Command`, `Gameplay/Vehicles`, `Gameplay/Systems`
- Used by: All gameplay consumers

**Core — Audio:**
- Purpose: Persistent background music across scene loads
- Location: `Assets/_Project/Core/Audio/`
- Contains: `BackgroundMusicManager`
- Depends on: `Assets/Resources/Audio/` (loaded at runtime via `Resources.Load`)
- Used by: Bootstrapped automatically via `[RuntimeInitializeOnLoadMethod]`

**Core — SceneManagement:**
- Purpose: Scene transitions and cross-scene persistence stores
- Location: `Assets/_Project/Core/SceneManagement/`
- Contains: Door triggers (`BarDoorTrigger`, `BarExitTrigger`, `CityHomeDoorTrigger`, `HomeDoorTrigger`), `PlayerSpawner`, `SpawnPoint`, `CarStateStore`, `DeliveredObjectsStore`
- Depends on: `Gameplay/Vehicles` (to read car transform before unloading)
- Used by: Scene GameObjects; static stores consumed by `CarController`, `CarryableObject`

**Gameplay — Player:**
- Purpose: FPS locomotion, camera, and item interaction
- Location: `Assets/_Project/Gameplay/Player/`
- Contains: `PlayerMovement`, `MouseLook`, `PlayerPickup`
- Depends on: `Core/Managers/DrunkManager`, `Gameplay/Items`
- Used by: Player prefab (`Assets/_Project/Prefabs/Player/player.prefab`)

**Gameplay — Vehicles:**
- Purpose: Car driving physics and enter/exit flow
- Location: `Assets/_Project/Gameplay/Vehicles/`
- Contains: `CarController`, `CarEnterExit`, `CarFollowCamera`
- Depends on: `Core/Managers/DrunkManager`, `Core/Managers/PlayerCarController`, `Patterns/Command`, `Gameplay/Systems`
- Used by: Car prefab (`Assets/_Project/Prefabs/Vehicles/Car_Sedan.prefab`)

**Gameplay — Items:**
- Purpose: Interactable objects (drinks and carryables)
- Location: `Assets/_Project/Gameplay/Items/`
- Contains: `PickupItem`, `CarryableObject`
- Depends on: `Core/SceneManagement/DeliveredObjectsStore`
- Used by: `PlayerPickup`, scene item GameObjects

**Gameplay — Systems:**
- Purpose: World-level gameplay rules (parking spots)
- Location: `Assets/_Project/Gameplay/Systems/`
- Contains: `ParkingSpot`
- Depends on: `Gameplay/Vehicles/CarController`
- Used by: `PlayerCarController`, `CarEnterExit`

**Patterns — Command:**
- Purpose: Deferred execution of gameplay actions (one per Update tick)
- Location: `Assets/_Project/Patterns/Command/`
- Contains: `ICommand`, `CommandQueue`, `EnterCarCommand`, `ExitCarCommand`
- Depends on: `Core/Managers/PlayerCarController`, `Gameplay/Systems/ParkingSpot`
- Used by: `CarEnterExit`, `PlayerCarController`

**Editor:**
- Purpose: Procedural city layout generation (edit-time only)
- Location: `Assets/_Project/Editor/`
- Contains: `CityBuilder`
- Depends on: `Assets/ThirdParty/Kenney/CityKit/`
- Used by: Unity Editor menu "Drunk Simulator → Build City Layout"

## Data Flow

### Player Drinks (Bar scene)

1. Player walks near a `PickupItem` — `PlayerPickup.UpdateSelectionByLook()` detects via raycast or `OverlapSphere` (`Assets/_Project/Gameplay/Player/PlayerPickup.cs:130-178`)
2. Player presses E — `PlayerPickup.Pickup()` creates a held visual clone parented to `holdPoint` (`PlayerPickup.cs:213`)
3. Player presses E again — `PlayerPickup.DrinkHeldItem()` calls `drunkManager.AddAlcohol(heldAlcoholPerSip)` (`PlayerPickup.cs:235-256`)
4. `DrunkManager.AddAlcohol()` clamps level, fires `OnAlcoholLevelChanged` event (`DrunkManager.cs:33-39`)
5. Next frame: all consumers read `DrunkManager.EffectIntensity` and scale their sinusoidal effects accordingly

### Scene Transition (City → Bar)

1. Player walks into `BarDoorTrigger` collider — `OnTriggerEnter` fires (`BarDoorTrigger.cs:19`)
2. Trigger finds `CarController` via `FindFirstObjectByType` and calls `CarStateStore.Save(car.transform)` (`BarDoorTrigger.cs:27-28`)
3. `SceneManager.LoadSceneAsync("Bar")` — City scene is fully destroyed
4. Bar scene loads; `PlayerSpawner.Start()` checks `NextSpawnId` (null here, so player stays at default position)
5. On return: `BarExitTrigger.OnTriggerEnter` sets `PlayerSpawner.NextSpawnId = "BarFront"`, loads City
6. City reloads; `CarController.Start()` reads `CarStateStore.HasSavedState` and restores position (`CarController.cs:48-56`)
7. `PlayerSpawner.Start()` teleports player to the `SpawnPoint` with id `"BarFront"` (`PlayerSpawner.cs:8-36`)

### Car Enter/Exit

1. Player presses E near car — `CarEnterExit.Update()` checks range, enqueues `EnterCarCommand` (`CarEnterExit.cs:28-29`)
2. Next `Update` tick — `CommandQueue.Update()` dequeues and calls `command.Execute()` (`CommandQueue.cs:16-18`)
3. `EnterCarCommand.Execute()` → `PlayerCarController.EnterCar()`: deactivates player GameObject, enables `CarController`, swaps cameras, fixes `AudioListener` (`PlayerCarController.cs:33-42`)
4. Exit: `PlayerCarController.Update()` finds a valid `ParkingSpot` (car stopped), enqueues `ExitCarCommand`
5. `ExitCarCommand.Execute()` → `PlayerCarController.ExitCar(spot)`: re-activates player at `spot.ExitPoint`, swaps back cameras/listeners

**State Management:**
- Alcohol level: `DrunkManager.alcoholLevel` (int, per-scene)
- Player-in-car: `PlayerCarController.IsInCar` (bool, per-scene)
- Held object: `PlayerPickup.hasHeldObject` (static bool, cross-scene)
- Car position: `CarStateStore.Position/Rotation` (static, cross-scene)
- Taken objects: `DeliveredObjectsStore.takenIds` (static HashSet, cross-scene)
- Next spawn: `PlayerSpawner.NextSpawnId` (static string, consumed once on scene load)

## Key Abstractions

**DrunkEffect Consumer Pattern:**
- Purpose: Apply alcohol-scaled distortion without coupling to DrunkManager's internals
- Examples: `Assets/_Project/Gameplay/Player/PlayerMovement.cs`, `Assets/_Project/Gameplay/Player/MouseLook.cs`, `Assets/_Project/Gameplay/Vehicles/CarController.cs`, `Assets/_Project/Gameplay/Vehicles/CarFollowCamera.cs`
- Pattern: Cache `DrunkManager` in `Awake` → read `EffectIntensity` in `Update`/`FixedUpdate`/`LateUpdate` → multiply by `Mathf.Sin(Time.time * freq) * amount`

**Static Store Pattern:**
- Purpose: Persist discrete state across `LoadSceneAsync(Single)` reloads without DontDestroyOnLoad objects
- Examples: `Assets/_Project/Core/SceneManagement/CarStateStore.cs`, `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`
- Pattern: `public static class` with properties/methods and a `Clear()` method for new-game reset

**ICommand / CommandQueue:**
- Purpose: Defer state-changing actions by one frame to avoid mid-Update conflicts
- Examples: `Assets/_Project/Patterns/Command/Commands/EnterCarCommand.cs`, `Assets/_Project/Patterns/Command/Commands/ExitCarCommand.cs`
- Pattern: Implement `ICommand.Execute()` → `CommandQueue.Enqueue(command)` → executed next `Update`

## Entry Points

**Runtime Bootstrap:**
- Location: `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`)
- Triggers: Automatically before any scene loads
- Responsibilities: Creates `BackgroundMusicManager` singleton, loads and plays BGM from `Resources/`

**Scene Start — Player:**
- Location: `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` (`Start()`)
- Triggers: On scene load when player prefab is active
- Responsibilities: Reads `NextSpawnId`, teleports player to matching `SpawnPoint`, clears the static field

**Scene Start — Car:**
- Location: `Assets/_Project/Gameplay/Vehicles/CarController.cs` (`Start()`)
- Triggers: On scene load when car prefab is active
- Responsibilities: Reads `CarStateStore.HasSavedState`, restores car position/rotation/velocity

**Scene Start — CarryableObject:**
- Location: `Assets/_Project/Gameplay/Items/CarryableObject.cs` (`Awake()`)
- Triggers: On scene load for each carryable in the scene
- Responsibilities: Checks `DeliveredObjectsStore.IsTaken(StableId)` and self-deactivates if already collected

## Architectural Constraints

- **Threading:** Single-threaded Unity main thread. No jobs or worker threads. `FixedUpdate` used only by `CarController` for physics forces.
- **Global state:** `DrunkManager` is a per-scene MonoBehaviour (not DontDestroyOnLoad). Alcohol level resets between scenes. Static stores (`CarStateStore`, `DeliveredObjectsStore`, `PlayerSpawner.NextSpawnId`, `PlayerPickup.hasHeldObject`) survive scene loads.
- **Input API:** Legacy `UnityEngine.Input` everywhere (`Input.GetKeyDown`, `Input.GetAxis`). An `InputSystem_Actions.inputactions` asset exists at `Assets/_Project/Core/Input/` but is unused in code.
- **Namespace:** All classes are in the global namespace (no `namespace` declarations). Do not add namespace wrappers.
- **Circular imports:** None detected. Dependencies flow: Gameplay → Core/Managers → (no back-reference). Patterns are standalone. SceneManagement triggers reference Gameplay/Vehicles only to save car state before unloading.
- **No assembly definitions:** All scripts compile into a single assembly. No `.asmdef` files present.

## Anti-Patterns

### FindFirstObjectByType in Awake

**What happens:** Components like `PlayerMovement`, `MouseLook`, `CarController`, `CarFollowCamera`, `CarEnterExit`, and `CommandQueue` all call `FindFirstObjectByType<DrunkManager>()` or `FindFirstObjectByType<CommandQueue>()` in `Awake` as a fallback when the serialized reference is null.
**Why it's wrong:** It couples components to scene-search at startup cost and silently succeeds even when the scene is wired incorrectly.
**Do this instead:** Always wire the reference in the Inspector via `[SerializeField]`. Keep the `FindFirstObjectByType` only as a logged warning fallback, not silent acceptance.

### Static field for held-object state

**What happens:** `PlayerPickup.hasHeldObject` is a `static bool` field (not a property on a dedicated store class).
**Why it's wrong:** Unlike `CarStateStore` and `DeliveredObjectsStore`, this static field has no `Clear()` method. If a new-game reset is added, it would be missed.
**Do this instead:** Move it into a dedicated static store class following the same pattern as `CarStateStore` (`Assets/_Project/Core/SceneManagement/`).

## Error Handling

**Strategy:** Null-guard with early return. No exceptions thrown.

**Patterns:**
- All `MonoBehaviour` methods guard against null references with `if (x == null) return;` before using serialized fields.
- `DrunkManager.AddAlcohol` guards `amount <= 0`.
- `CarryableObject.StableId` falls back to a scene-path-derived ID if `objectId` is empty.
- `BackgroundMusicManager` logs a `Debug.LogWarning` and returns if the BGM clip is not found; never throws.
- Resource loading uses ordered fallback path arrays (try primary path, then secondary).

## Cross-Cutting Concerns

**Logging:** `Debug.Log` / `Debug.LogWarning` with bracketed prefix indicating source (e.g., `[BackgroundMusicManager]`, `[PlayerSpawner]`, `[CityBuilder]`). No structured logging framework.
**Validation:** No runtime validation framework. Input clamping done manually (`Mathf.Clamp`, `Mathf.Clamp01`).
**Authentication:** Not applicable (single-player local game, no network).

---

*Architecture analysis: 2026-06-22*
