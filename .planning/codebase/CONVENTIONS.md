# Coding Conventions

**Analysis Date:** 2026-06-22

## Namespace

**Rule: No namespace declarations.** All classes live in the global namespace. This is enforced across every file in `Assets/_Project/`. Do NOT introduce `namespace` blocks.

```csharp
// CORRECT — global namespace
public class CarController : MonoBehaviour { }

// WRONG
namespace DrunkSimulator.Vehicles
{
    public class CarController : MonoBehaviour { }
}
```

## Naming Patterns

**Files:**
- One class per file; filename matches the class name exactly (PascalCase): `DrunkManager.cs`, `PlayerCarController.cs`, `CarStateStore.cs`
- Interfaces prefixed with `I`: `ICommand.cs`
- Enums defined in the same file as their primary consumer when small (e.g., `PickupType` in `PickupItem.cs`)

**Classes:**
- `MonoBehaviour` subclasses: PascalCase noun or noun-phrase — `PlayerMovement`, `CarFollowCamera`, `BarDoorTrigger`
- Non-`MonoBehaviour` classes: same PascalCase rule — `CommandQueue`, `EnterCarCommand`, `CarStateStore`
- Static store classes: suffix `Store` — `CarStateStore`, `DeliveredObjectsStore`
- Editor-only tools: plain PascalCase static class — `CityBuilder`

**Methods:**
- PascalCase for all non-Unity-message methods: `AddAlcohol()`, `SetControlled()`, `ApplyCameraState()`
- Unity message callbacks use their exact signature casing: `Awake()`, `Update()`, `FixedUpdate()`, `LateUpdate()`, `OnTriggerEnter()`, `OnDrawGizmosSelected()`

**Fields and variables:**
- Inspector-exposed fields with `[SerializeField]`: `private` + camelCase: `private float thrust`, `private bool triggered`
- Public serialized fields (older scripts like `PlayerMovement`): plain `public` + camelCase with no attribute: `public float speed`
- Private backing fields: camelCase: `private bool isControlled`, `private Rigidbody rb`
- Local variables: camelCase: `float drunkAmount`, `ParkingSpot spot`

**Properties:**
- Expression-body auto-properties: PascalCase: `public bool IsInCar { get; private set; }`, `public float EffectIntensity => ...`

**Constants:**
- `const` fields at class scope: PascalCase — `const float RoadHalfLength = 110f`
- `static readonly` arrays: PascalCase — `private static readonly string[] BgmResourcePaths`

## Input API

**Use the legacy Input API exclusively.** The new Input System package (`com.unity.inputsystem`) is installed but unused in gameplay code. An `.inputactions` asset exists at `Assets/_Project/Core/Input/InputSystem_Actions.inputactions` but no script reads from it.

```csharp
// CORRECT — legacy API
float x = Input.GetAxis("Horizontal");
float z = Input.GetAxis("Vertical");
if (Input.GetKeyDown(KeyCode.E)) { }
if (Input.GetKey(KeyCode.LeftShift)) { }
if (Input.GetButtonDown("Jump")) { }
float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
```

Never use `InputSystem`, `InputAction`, or `InputActionAsset` APIs in gameplay scripts.

## Inspector Exposure Pattern

**Preferred (newer scripts): `[SerializeField] private`**
```csharp
[Header("Drunk Driving")]
[SerializeField] private float drunkSteerDriftAmount = 0.55f;
[Tooltip("Descripcion en español")]
[SerializeField] private float drunkSteerDriftFrequency = 0.7f;
```

**Also present (older scripts): bare `public`**
```csharp
// PlayerMovement.cs — public fields, no [SerializeField]
public float speed = 5f;
public float sprintMultiplier = 1.75f;
```

Use `[SerializeField] private` for new code. Use `[Header("...")]` to group related fields in the Inspector. Use `[Tooltip("...")]` for non-obvious parameters.

Use `[RequireComponent(typeof(Collider))]` on components that depend on a sibling component: `BarDoorTrigger`, `ParkingSpot`, etc.

## DrunkManager Caching Pattern

Every script that reads `EffectIntensity` caches a `DrunkManager` reference in `Awake` using a component lookup first, then `FindFirstObjectByType` as fallback:

```csharp
DrunkManager drunkManager;  // untyped access modifier = private by default

void Awake()
{
    // 1. Try the same GameObject first
    drunkManager = GetComponent<DrunkManager>();

    // 2. Fallback: scene-wide search
    if (drunkManager == null)
    {
        drunkManager = FindFirstObjectByType<DrunkManager>();
    }
}
```

Then read it defensively each frame:
```csharp
float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;
```

Files that implement this pattern: `Assets/_Project/Gameplay/Player/PlayerMovement.cs`, `Assets/_Project/Gameplay/Player/MouseLook.cs`, `Assets/_Project/Gameplay/Player/PlayerPickup.cs`, `Assets/_Project/Gameplay/Vehicles/CarController.cs`, `Assets/_Project/Gameplay/Vehicles/CarFollowCamera.cs`.

`CarController` (`Assets/_Project/Gameplay/Vehicles/CarController.cs`) uses only `FindFirstObjectByType` (no same-object fallback needed because `DrunkManager` never lives on the car).

## Resources.Load Fallback Pattern

Audio clips loaded at runtime use a fallback array of paths tried in order:

```csharp
// BackgroundMusicManager.cs — static array + loop
private static readonly string[] BgmResourcePaths =
{
    "Audio/Music/BackgroundMusic",
    "Audio/BackgroundMusic"
};

private static AudioClip LoadBackgroundMusicClip()
{
    for (int i = 0; i < BgmResourcePaths.Length; i++)
    {
        AudioClip clip = Resources.Load<AudioClip>(BgmResourcePaths[i]);
        if (clip != null) return clip;
    }
    return null;
}
```

For one-off SFX clips, inline double-fallback is used:
```csharp
// PlayerPickup.cs
drinkSipClip = Resources.Load<AudioClip>("Audio/SFX/DrinkSip");
if (drinkSipClip == null)
{
    drinkSipClip = Resources.Load<AudioClip>("Audio/DrinkSip");
}
```

All audio assets must live under `Assets/Resources/` for `Resources.Load` to find them.

## Singleton Pattern

`BackgroundMusicManager` (`Assets/_Project/Core/Audio/BackgroundMusicManager.cs`) is the only singleton. Its pattern:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Bootstrap()
{
    if (instance != null) return;
    GameObject managerObject = new GameObject(nameof(BackgroundMusicManager));
    instance = managerObject.AddComponent<BackgroundMusicManager>();
}

private void Awake()
{
    if (instance != null && instance != this) { Destroy(gameObject); return; }
    instance = this;
    DontDestroyOnLoad(gameObject);
    // ... init
}
```

Do NOT apply `DontDestroyOnLoad` to gameplay scripts. Use the static store pattern (see below) instead.

## Static Store Pattern (Cross-Scene State)

Cross-scene state lives in `static` classes, never in `DontDestroyOnLoad` MonoBehaviours (except `BackgroundMusicManager`):

```csharp
public static class CarStateStore
{
    public static bool HasSavedState { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Save(Transform car) { ... HasSavedState = true; }
    public static void Clear() { HasSavedState = false; }
}
```

Every store exposes a `Clear()` method for future "new game" support. Existing stores: `Assets/_Project/Core/SceneManagement/CarStateStore.cs`, `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`. `PlayerSpawner.NextSpawnId` (`Assets/_Project/Core/SceneManagement/PlayerSpawner.cs`) is a plain `public static string` field on the MonoBehaviour.

## Door Trigger Pattern

All scene-transition triggers share an identical structure:

```csharp
[RequireComponent(typeof(Collider))]
public class BarDoorTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string sceneToLoad = "Bar";
    [SerializeField] private string playerTag = "player";

    private bool triggered;  // guard against multiple trigger events

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;
        // optionally save state before unload
        SceneManager.LoadSceneAsync(sceneToLoad);
    }
}
```

Files: `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs`, `HomeDoorTrigger.cs`, `BarExitTrigger.cs`, `CityHomeDoorTrigger.cs`.

## Command Pattern

Deferred actions implement `ICommand` (`Assets/_Project/Patterns/Command/ICommand.cs`) and are enqueued into `CommandQueue` (`Assets/_Project/Patterns/Command/CommandQueue.cs`), which dequeues one command per `Update`:

```csharp
public interface ICommand
{
    void Execute();
}
```

Command classes hold only references needed to call their target's public API. They have no Update loop; all logic is in `Execute()`. See `EnterCarCommand`, `ExitCarCommand` in `Assets/_Project/Patterns/Command/Commands/`.

## Drunk Effect Pattern

Any new script that applies a drunk distortion effect follows this pattern:

1. Declare `DrunkManager drunkManager;` as an unqualified (implicitly private) field.
2. Cache in `Awake` with the GetComponent + FindFirstObjectByType fallback.
3. In the relevant Update/FixedUpdate/LateUpdate, compute `float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;`
4. Apply distortion: `Mathf.Sin(Time.time * frequency) * amplitude * drunkAmount`.

## Comments and Log Messages

All inline comments and user-facing strings are written in **Spanish**. `Debug.Log`/`Debug.LogWarning` messages include a `[ClassName]` prefix tag:

```csharp
Debug.LogWarning("[BackgroundMusicManager] No se encontro el clip...");
Debug.LogWarning($"[PlayerSpawner] No SpawnPoint found with id '{NextSpawnId}'");
Debug.Log("[CityBuilder] Ciudad generada. Guardá la escena con Ctrl+S.");
```

XML doc comments (`<summary>`) are also in Spanish: `/// <summary>Guarda la posicion/rotacion del auto entre cargas de escena.</summary>`

## Error Handling

**No exceptions thrown.** Scripts use early-return guard clauses instead:

```csharp
public void AddAlcohol(int amount)
{
    if (amount <= 0) return;
    // ...
}

public void ExitCar(ParkingSpot spot)
{
    if (!IsInCar || spot == null) return;
    // ...
}
```

Null checks on Unity object references use `!= null` (not null-coalescing or null-conditional operators on `UnityEngine.Object`). Defensive clamping with `Mathf.Clamp`, `Mathf.Clamp01`, and ternary guards (`maxLevel > 0 ? ... : 0f`) is preferred.

## Module Design

**No barrel files / re-export files.** Each `.cs` file declares exactly one public type (class, interface, or enum). Enum `PickupType` is the exception — it appears at the top of `PickupItem.cs` because it is only used there.

**Editor-only code** lives in `Assets/_Project/Editor/` and uses `UnityEditor` APIs. It must not be referenced from runtime scripts.

---

*Convention analysis: 2026-06-22*
