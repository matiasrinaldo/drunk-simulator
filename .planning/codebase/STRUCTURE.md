# Codebase Structure

**Analysis Date:** 2026-06-22

## Directory Layout

```
drunk-simulator/
├── Assets/
│   ├── _Project/                   # All own code and assets (organized by responsibility)
│   │   ├── Audio/                  # Audio mixer and music assets (non-code)
│   │   │   ├── Mixers/             # Unity AudioMixer assets
│   │   │   ├── Music/              # BGM audio clips
│   │   │   └── SFX/                # Sound effect clips
│   │   ├── Core/                   # Runtime infrastructure
│   │   │   ├── Audio/              # BackgroundMusicManager.cs
│   │   │   ├── Input/              # InputSystem_Actions.inputactions (unused in code)
│   │   │   ├── Managers/           # DrunkManager.cs, PlayerCarController.cs
│   │   │   ├── SceneManagement/    # Door triggers, stores, PlayerSpawner, SpawnPoint
│   │   │   └── Utilities/          # (empty — reserved for shared helpers)
│   │   ├── Editor/                 # CityBuilder.cs (Editor-only, excluded from builds)
│   │   ├── Gameplay/               # All gameplay-specific behavior
│   │   │   ├── Items/              # PickupItem.cs, CarryableObject.cs
│   │   │   ├── Obstacles/          # (empty — reserved for obstacle logic)
│   │   │   ├── Player/             # PlayerMovement.cs, MouseLook.cs, PlayerPickup.cs
│   │   │   ├── Systems/            # ParkingSpot.cs
│   │   │   └── Vehicles/           # CarController.cs, CarEnterExit.cs, CarFollowCamera.cs
│   │   ├── Patterns/               # Reusable design pattern implementations
│   │   │   ├── Command/            # ICommand.cs, CommandQueue.cs
│   │   │   │   └── Commands/       # EnterCarCommand.cs, ExitCarCommand.cs
│   │   │   ├── EventQueue/         # (empty — reserved)
│   │   │   └── Strategy/           # (empty — reserved)
│   │   ├── Prefabs/                # Assembled prefabs (no code)
│   │   │   ├── Environment/        # Scene environment pieces
│   │   │   ├── Items/              # Item prefabs + materials
│   │   │   ├── Obstacles/          # Obstacle prefabs
│   │   │   ├── Player/             # player.prefab
│   │   │   └── Vehicles/           # Car_Sedan.prefab
│   │   ├── ScriptableObjects/
│   │   │   └── Items/              # (empty — reserved for item SO definitions)
│   │   └── UI/
│   │       ├── HUD/                # HUD elements
│   │       ├── Layouts/            # UI layout assets
│   │       ├── Prefabs/            # UI prefabs
│   │       └── Screens/            # Screen-level UI
│   ├── Art/
│   │   ├── Materials/City/         # Generated city materials (by CityBuilder)
│   │   ├── Models/                 # Custom 3D models
│   │   ├── Shaders/                # Custom URP shaders
│   │   └── Textures/               # Project textures (BarComposition etc.)
│   ├── Resources/                  # Runtime-loadable assets (Resources.Load)
│   │   └── Audio/
│   │       └── SFX/                # DrinkSip.clip, PayDrink.clip (loaded with fallback)
│   ├── Scenes/
│   │   ├── City.unity              # Open-world driving scene
│   │   ├── Bar.unity               # Bar / drinking scene
│   │   └── Home.unity              # Home / carryable-object scene
│   ├── Settings/                   # URP pipeline and quality settings
│   ├── TextMesh Pro/               # TMPro package assets
│   └── ThirdParty/
│       ├── Kenney/
│       │   ├── CarKit/             # Car models (FBX/GLB/OBJ)
│       │   ├── CharacterKit/       # Character models
│       │   ├── CityKit/            # Building models used by CityBuilder
│       │   ├── FurnitureKit/       # Furniture models for Home/Bar
│       │   ├── NatureKit/          # Tree/vegetation models
│       │   └── RoadKit/            # Road tile models
│       ├── ADG_Textures/           # Plank texture packs
│       └── TutorialInfo/           # Unity template tutorial scripts (not used in gameplay)
├── Packages/                       # Unity Package Manager manifest
├── ProjectSettings/                # Unity project settings (version-controlled)
└── .planning/codebase/             # GSD architecture documents
```

## Directory Purposes

**`Assets/_Project/Core/Managers/`:**
- Purpose: Central runtime state managers that other systems depend on
- Contains: `DrunkManager.cs` (alcohol state), `PlayerCarController.cs` (player-vs-car mode)
- Key files: `DrunkManager.cs`, `PlayerCarController.cs`

**`Assets/_Project/Core/Audio/`:**
- Purpose: Persistent audio systems that must survive scene transitions
- Contains: `BackgroundMusicManager.cs` — bootstrapped via `[RuntimeInitializeOnLoadMethod]`, uses `DontDestroyOnLoad`

**`Assets/_Project/Core/SceneManagement/`:**
- Purpose: Everything that bridges scene load boundaries
- Contains: Four door trigger scripts, `PlayerSpawner.cs`, `SpawnPoint.cs`, plus two static store classes
- Key files: `CarStateStore.cs`, `DeliveredObjectsStore.cs`, `PlayerSpawner.cs`, `BarExitTrigger.cs`

**`Assets/_Project/Gameplay/Player/`:**
- Purpose: FPS player behavior — movement, camera, item interaction
- Contains: `PlayerMovement.cs`, `MouseLook.cs`, `PlayerPickup.cs`

**`Assets/_Project/Gameplay/Vehicles/`:**
- Purpose: Car physics, camera, and enter/exit interaction
- Contains: `CarController.cs`, `CarEnterExit.cs`, `CarFollowCamera.cs`

**`Assets/_Project/Gameplay/Items/`:**
- Purpose: Interactable world objects — drinks and carryables
- Contains: `PickupItem.cs` (drinks), `CarryableObject.cs` (objects player carries between scenes)

**`Assets/_Project/Gameplay/Systems/`:**
- Purpose: World-level gameplay rules not specific to a single entity
- Contains: `ParkingSpot.cs` — trigger zone that gates car exit by speed

**`Assets/_Project/Patterns/Command/`:**
- Purpose: Command pattern infrastructure for deferred execution
- Contains: `ICommand.cs`, `CommandQueue.cs` (MonoBehaviour), `Commands/EnterCarCommand.cs`, `Commands/ExitCarCommand.cs`

**`Assets/_Project/Editor/`:**
- Purpose: Unity Editor-only tools; excluded from builds automatically by Unity
- Contains: `CityBuilder.cs` — procedurally places Kenney CityKit assets in `City.unity`

**`Assets/Resources/`:**
- Purpose: Assets loadable at runtime via `Resources.Load<T>(path)` — required by `BackgroundMusicManager` and `PlayerPickup`
- Key paths: `Audio/Music/BackgroundMusic` (BGM), `Audio/SFX/DrinkSip`, `Audio/SFX/PayDrink`
- Note: Always provide a fallback path when loading (see `BackgroundMusicManager.BgmResourcePaths`)

## Key File Locations

**Entry Points:**
- `Assets/_Project/Core/Audio/BackgroundMusicManager.cs`: Auto-bootstrap before first scene load
- `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs`: Player position on scene load
- `Assets/_Project/Gameplay/Vehicles/CarController.cs`: Car state restore on scene load

**Configuration:**
- `ProjectSettings/ProjectVersion.txt`: Unity version (`6000.3.11f1`)
- `Packages/manifest.json`: Package dependencies including URP 17.3

**Core Logic:**
- `Assets/_Project/Core/Managers/DrunkManager.cs`: Alcohol state + EffectIntensity
- `Assets/_Project/Core/Managers/PlayerCarController.cs`: Player↔car mode switching
- `Assets/_Project/Patterns/Command/CommandQueue.cs`: One-command-per-frame queue

**Persistence Stores:**
- `Assets/_Project/Core/SceneManagement/CarStateStore.cs`: Static car position store
- `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`: Static taken-object set

**Scenes:**
- `Assets/Scenes/City.unity`: Open-world driving; has car, parking spots, door triggers to Bar and Home
- `Assets/Scenes/Bar.unity`: Drinking; has PickupItem instances and BarExitTrigger
- `Assets/Scenes/Home.unity`: Home; has CarryableObject instances and HomeDoorTrigger

**Prefabs:**
- `Assets/_Project/Prefabs/Player/player.prefab`: Player GameObject with `PlayerMovement`, `MouseLook`, `PlayerPickup`, `PlayerSpawner`, `DrunkManager`
- `Assets/_Project/Prefabs/Vehicles/Car_Sedan.prefab`: Car with `CarController`, `CarEnterExit`, `CarFollowCamera`

## Naming Conventions

**Files:**
- MonoBehaviour scripts: `PascalCase` matching the class name (`DrunkManager.cs`, `PlayerPickup.cs`)
- Static store classes: `PascalCase` ending in `Store` (`CarStateStore.cs`, `DeliveredObjectsStore.cs`)
- Door triggers: `[From][To]DoorTrigger` or `[Location]ExitTrigger` (`BarDoorTrigger.cs`, `BarExitTrigger.cs`, `CityHomeDoorTrigger.cs`, `HomeDoorTrigger.cs`)
- Command classes: `[Action]Command` (`EnterCarCommand.cs`, `ExitCarCommand.cs`)
- Prefabs: lowercase or PascalCase depending on type (`player.prefab`, `Car_Sedan.prefab`)
- Materials: `M_` prefix (`M_Ground`, `M_Asphalt`)

**Classes:**
- All classes in global namespace (no `namespace` block). Do not add namespaces.
- `PascalCase` for all class and method names
- `camelCase` for private fields; `[SerializeField] private` for Inspector-exposed fields
- Properties use `PascalCase` (`EffectIntensity`, `AlcoholLevel`, `IsInCar`)
- `static` fields in stores: `PascalCase` properties with private setters (`CarStateStore.HasSavedState`)

**Directories:**
- `PascalCase` throughout `Assets/_Project/`
- Sub-grouping by responsibility, not by type (no `Scripts/`, `Components/`, etc.)

## Where to Add New Code

**New drunk effect (on any component):**
1. Cache `DrunkManager` in `Awake`: `drunkManager = GetComponent<DrunkManager>() ?? FindFirstObjectByType<DrunkManager>();`
2. In the appropriate Unity callback (`Update`, `FixedUpdate`, `LateUpdate`): `float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;`
3. Apply: `Mathf.Sin(Time.time * frequency) * amount * drunkAmount`
4. Place the script in the relevant `Gameplay/` subdirectory

**New gameplay system (world rule, not tied to player or car):**
- Implementation: `Assets/_Project/Gameplay/Systems/`
- Tests (if added later): follow Unity Test Framework pattern alongside implementation

**New item type (drinkable):**
- Script (if needed): `Assets/_Project/Gameplay/Items/`
- Prefab: `Assets/_Project/Prefabs/Items/`
- Add to `PickupType` enum in `Assets/_Project/Gameplay/Items/PickupItem.cs`
- Add name-matching in `PickupItem.ResolvedPickupType`

**New scene transition:**
- Trigger script: `Assets/_Project/Core/SceneManagement/`
- Set `PlayerSpawner.NextSpawnId` before calling `LoadSceneAsync` if the player must appear at a specific point
- If car state must persist: call `CarStateStore.Save(car.transform)` before loading

**New cross-scene persisted state:**
- Create a `public static class` in `Assets/_Project/Core/SceneManagement/` following the `CarStateStore` template
- Include a `Clear()` method for future new-game reset support
- Document it in ARCHITECTURE.md under "Key Abstractions — Static Store Pattern"

**New deferred action:**
- Implement `ICommand` (`Assets/_Project/Patterns/Command/`)
- Place the concrete command class in `Assets/_Project/Patterns/Command/Commands/`
- Enqueue via `commandQueue.Enqueue(new MyCommand(...))` — never call `Execute()` directly

**New editor tool:**
- Place in `Assets/_Project/Editor/`
- Use `[MenuItem("Drunk Simulator/...")]` for the menu entry to stay consistent with `CityBuilder`

**New UI element:**
- Prefabs: `Assets/_Project/UI/Prefabs/`
- Screen-level canvases: `Assets/_Project/UI/Screens/`
- HUD overlays: `Assets/_Project/UI/HUD/`

**New shared helper / utility:**
- Pure C# utility: `Assets/_Project/Core/Utilities/`

## Special Directories

**`Assets/Resources/`:**
- Purpose: Assets loaded at runtime via `Resources.Load<T>()` — required for `BackgroundMusicManager` and `PlayerPickup` SFX
- Generated: No
- Committed: Yes
- Note: Only place assets here if they must be loaded dynamically by path. Always add a fallback path in the loading code.

**`Assets/ThirdParty/`:**
- Purpose: Third-party asset packs (Kenney kits, ADG textures)
- Generated: No
- Committed: Yes
- Note: Do not modify third-party files. Reference them from `Assets/_Project/` or `CityBuilder.cs` by path.

**`Assets/_Project/Editor/`:**
- Purpose: Unity Editor tooling; automatically excluded from runtime builds by Unity
- Generated: No
- Committed: Yes

**`Library/`, `Temp/`, `Logs/`, `UserSettings/`:**
- Purpose: Unity-generated caches and per-user settings
- Generated: Yes
- Committed: No (in `.gitignore`)

**`.planning/codebase/`:**
- Purpose: GSD architecture documents consumed by plan-phase and execute-phase agents
- Generated: Yes (by map-codebase)
- Committed: Per project convention

---

*Structure analysis: 2026-06-22*
