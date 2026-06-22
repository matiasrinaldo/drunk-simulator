# Codebase Concerns

**Analysis Date:** 2026-06-22

---

## Tech Debt

**No "New Game" reset — static stores never cleared:**
- Issue: Four pieces of cross-scene state live in static C# storage that survives the entire Editor/build session but has no caller for `.Clear()`. If the player ever returns to the main menu and starts a fresh run, all previous state leaks into the new session.
- Files: `Assets/_Project/Core/SceneManagement/CarStateStore.cs`, `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`, `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` (field `NextSpawnId`), `Assets/_Project/Gameplay/Player/PlayerPickup.cs` (field `static bool hasHeldObject`)
- Impact: Car spawns at wrong position, previously-collected carryable objects stay invisible, the player appears to already be holding an object — all from a prior play session.
- Fix approach: Create a `GameSession.Reset()` method that calls `CarStateStore.Clear()`, `DeliveredObjectsStore.Clear()`, `PlayerSpawner.NextSpawnId = null`, and `PlayerPickup.hasHeldObject = false` (needs a public static setter or reset method). Wire it to any "New Game" / "Play Again" button.

**`DrunkManager.alcoholLevel` is not persisted or reset across scene transitions:**
- Issue: `DrunkManager` is a regular `MonoBehaviour` on the player prefab. Every `LoadSceneAsync` in Single mode destroys it and re-instantiates it with `alcoholLevel = 0`. Alcohol consumed in the Bar is silently lost when returning to City.
- Files: `Assets/_Project/Core/Managers/DrunkManager.cs`
- Impact: The core gameplay loop (drink in Bar → drive drunk in City) does not work because the effect resets on scene load.
- Fix approach: Either add an `AlcoholStore` static class following the same pattern as `CarStateStore`, or make `DrunkManager` a `DontDestroyOnLoad` singleton (similar to `BackgroundMusicManager`) and guard against duplicates on scene re-entry.

**String-based pickup-type resolution:**
- Issue: `PickupItem.ResolvedPickupType` resolves the drink type by calling `string.Contains("cerveza")` / `Contains("whisky")` / `Contains("trago")` on the object name or the `itemName` field. The serialised `pickupType` field is only a fallback that triggers if none of the strings match.
- Files: `Assets/_Project/Gameplay/Items/PickupItem.cs` lines 28-41
- Impact: Renaming a prefab in the Inspector or in the scene hierarchy silently changes its alcohol value. A drink named "CervezaNegra" or "vaso_cerveza" will still match, but "beer" or "lager" will not — the type falls back to `Trago` without any warning.
- Fix approach: Remove the string-matching branch. Rely solely on the serialised `pickupType` enum field, which is already Inspector-visible and type-safe. Delete the `itemName` field or demote it to a display label only.

**`PlayerPickup.hasHeldObject` is a `static` field on an instance MonoBehaviour:**
- Issue: The field `static bool hasHeldObject` in `PlayerPickup` is readable through an instance property `HasHeldObject` but is mutated both from instance methods (`PickupCarryable`, `Update`, `ConsumeHeldObject`) and shared across all instances of the class. If a second `PlayerPickup` were ever added, both would share the same boolean invisibly.
- Files: `Assets/_Project/Gameplay/Player/PlayerPickup.cs` line 33
- Impact: Conceptual mismatch — the state is per-player but stored globally. Also prevents the `.Clear()` reset noted above from being called cleanly without a public static setter.
- Fix approach: Convert to a regular instance field. Add a `public static void ResetStatic()` method for the new-game reset path.

**Hardcoded scene name and spawn-point ID strings scattered across triggers:**
- Issue: Every door trigger serialises target scene names (`"Bar"`, `"City"`, `"Home"`) and spawn IDs (`"BarFront"`) as plain `string` Inspector fields with no validation. `PlayerSpawner` does a silent name-match loop at runtime.
- Files: `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs:8`, `Assets/_Project/Core/SceneManagement/BarExitTrigger.cs:8-10`, `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs:8`, `Assets/_Project/Core/SceneManagement/HomeDoorTrigger.cs:8`
- Impact: Renaming a scene in Build Settings, or renaming a `SpawnPoint` GameObject, silently breaks navigation with no compile-time error. `PlayerSpawner` logs a warning but the player appears at the default scene origin.
- Fix approach: Replace scene name strings with `SceneReference` ScriptableObjects (or the lightweight `[SceneAsset]` attribute trick). Replace spawn ID strings with a `SpawnPoint` direct reference serialised on the trigger, bypassing the runtime name search entirely.

**Cursor is locked only in `MouseLook.Awake` — no unlock path:**
- Issue: `Cursor.lockState = CursorLockMode.Locked` is set once in `MouseLook.Awake` with no corresponding unlock on pause, game-over, or returning to a menu.
- Files: `Assets/_Project/Gameplay/Player/MouseLook.cs` line 22
- Impact: The mouse remains trapped if the player somehow returns to a menu screen or the game is paused. This also makes Editor testing awkward since there is no toggle to release focus.
- Fix approach: Add an `Escape` key handler (or a pause-event callback) that sets `Cursor.lockState = CursorLockMode.None` and re-locks on resume.

---

## Known Bugs

**`BackgroundMusicManager` creates an `AudioSource` without an `AudioListener` on its own `DontDestroyOnLoad` object — silent gap possible:**
- Symptoms: The manager `GameObject` persists across scenes, but the `AudioListener` it depends on lives on the camera in the scene. When `PlayerCarController.UpdateAudioListeners` toggles the FPS listener off and the car listener on (or vice versa), there is a one-frame window where both are disabled before the other is enabled. During that frame, Unity emits a "no AudioListener" warning and audio may glitch.
- Files: `Assets/_Project/Core/Managers/PlayerCarController.cs` lines 68-82
- Trigger: Enter or exit the car.
- Workaround: The current code enables the incoming listener before (or simultaneously with) disabling the outgoing one — but the order is: `carListener.enabled = IsInCar` then `fpsListener.enabled = !IsInCar`. If the fpsCamera reference is null (missing Inspector assignment), the FPS listener is never re-enabled on exit.

**`ParkingSpot.carInside` is not cleared when the car's scene is unloaded:**
- Symptoms: If the player drives into a ParkingSpot then triggers a scene load (e.g. via `CityHomeDoorTrigger`) without explicitly leaving the spot, `carInside` holds a destroyed reference. On the next City load, a fresh `ParkingSpot` instance starts with `carInside == null` (safe), but any lingering `ExitCarCommand` in the queue would dereference the destroyed controller.
- Files: `Assets/_Project/Gameplay/Systems/ParkingSpot.cs`, `Assets/_Project/Patterns/Command/CommandQueue.cs`
- Trigger: Drive into a parking spot, immediately walk through a scene-change door without pressing E to exit.

**`CarryableObject.StableId` silently collides when objects share a hierarchy position:**
- Symptoms: If two `CarryableObject` prefabs are placed as siblings at the same sibling index in different parent chains but produce the same scene-name + index path, `DeliveredObjectsStore` will treat them as the same object. One taken object hides the other.
- Files: `Assets/_Project/Gameplay/Items/CarryableObject.cs` lines 21-36
- Trigger: Reordering or duplicating objects in the Home scene hierarchy without assigning explicit `objectId` values.
- Workaround: Assign a unique `objectId` string in the Inspector for every `CarryableObject` in the scene (the field exists but is not required).

---

## Fragile Areas

**Cross-scene persistence via static in-memory stores:**
- Files: `Assets/_Project/Core/SceneManagement/CarStateStore.cs`, `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`, `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs`, `Assets/_Project/Gameplay/Player/PlayerPickup.cs`
- Why fragile: All four stores reset to their zero/null/empty values when the Unity Editor exits Play Mode or when the built executable is closed. They do not survive crashes. There is no save-to-disk path. If the order of `Awake` calls across scene objects changes (e.g. after a Unity upgrade or prefab edit), state may be consumed before it is written or vice versa.
- Safe modification: Always set store state **before** calling `SceneManager.LoadSceneAsync`, never after. Read state only in `Start` (after all `Awake` calls). Never read store state in `Awake` of the destination scene's objects.
- Test coverage: None — there are no unit or integration tests for any store.

**`PlayerCarController.FindActiveSpotForExit` scans all `ParkingSpot` objects every `Update` frame while driving:**
- Files: `Assets/_Project/Core/Managers/PlayerCarController.cs` line 86
- Why fragile: `FindObjectsByType<ParkingSpot>()` allocates an array every call. Called every frame while `IsInCar` is true. With the current small scene this is negligible, but it is a GC pressure point that will grow with scene complexity.
- Safe modification: Cache the parking spots array on scene load and invalidate the cache only when spots change.

**`BackgroundMusicManager` bootstraps via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` but its `Awake` also has a singleton guard — double-init risk:**
- Files: `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` lines 23-57
- Why fragile: `Bootstrap()` runs before the first scene loads and creates the instance. If any scene also has a `BackgroundMusicManager` component placed in the hierarchy (e.g. by accident), `Awake` fires again, finds `instance != null`, and destroys it — silencing music for the rest of the session.
- Safe modification: Do not place `BackgroundMusicManager` as a component in any scene. It must only be created by `Bootstrap()`.

**`PickupItem` sets `material.EnableKeyword("_EMISSION")` and mutates `globalIlluminationFlags` in `Awake` on the shared material asset:**
- Files: `Assets/_Project/Gameplay/Items/PickupItem.cs` lines 63-75
- Why fragile: `renderers[i].materials` returns new material instances (per-renderer), but calling `EnableKeyword` on a shared material in some Unity versions (depending on material instantiation order) can leak into the asset on disk in the Editor. In play mode this is safe, but it is easy to accidentally permanently modify materials if this code runs in edit mode (e.g. via `ExecuteInEditMode` or a future editor script).
- Safe modification: Use `renderers[i].material` (singular, which is guaranteed to be instanced) or explicitly instantiate with `new Material(renderers[i].sharedMaterial)`.

**`PlayerPickup.FindClosestPickupInRange` uses `Physics.OverlapSphere` (allocating) every frame:**
- Files: `Assets/_Project/Gameplay/Player/PlayerPickup.cs` line 183
- Why fragile: Allocates a `Collider[]` every `Update` call. Replace with `Physics.OverlapSphereNonAlloc` and a pre-allocated buffer.

---

## Test Coverage Gaps

**All gameplay logic — no tests exist:**
- What's not tested: `DrunkManager` alcohol accumulation and `EffectIntensity` curve, `DeliveredObjectsStore` mark/check/clear cycle, `CarStateStore` save/clear cycle, `PickupItem.ResolvedPickupType` string matching, `CarryableObject.StableId` generation and collision, `CommandQueue` ordering and null-command handling.
- Files: All files under `Assets/_Project/`
- Risk: Any refactor to the persistence stores, the drunk effect pipeline, or the pickup type resolution can break the game silently with no automated signal.
- Priority: High — at minimum, data-layer classes (`DrunkManager`, `*Store`, `PickupItem.ResolvedPickupType`) should have EditMode unit tests since they have no MonoBehaviour dependencies.

---

## Missing Critical Features

**No "New Game" / session reset entry point:**
- Problem: There is no code path that resets all four static stores simultaneously. The main menu exists (`342d2b4 Add main menu actions`) but there is no evidence of a reset call in any scene management code.
- Blocks: Replaying without closing and reopening the game; QA testing multiple sessions back-to-back; any future game-over / retry flow.

**No alcohol persistence across scene transitions (Bar → City):**
- Problem: `DrunkManager` is destroyed on every `LoadSceneAsync`. Alcohol consumed at the bar is lost before the player reaches the car.
- Blocks: The entire core loop — the game's premise requires arriving at the car drunk.

**No audio clip assigned via Inspector for `PlayerPickup` — silent fallback only:**
- Problem: `drinkSipClip` and `payDrinkClip` are loaded at runtime via `Resources.Load` with fallback paths. If the `Resources/Audio/SFX/` path or filenames change, the clips silently fail to load (no error, just silence), because the fallback path `"Audio/DrinkSip"` also does not exist in the current `Resources/Audio/` folder (only `BackgroundMusic.mp3`, `SFX/DrinkSip.mp3`, `SFX/PayDrink.mp3` are present at the primary path; the fallback paths `"Audio/DrinkSip"` and `"Audio/PayDrink"` have no corresponding files).
- Files: `Assets/_Project/Gameplay/Player/PlayerPickup.cs` lines 73-87
- Blocks: Sound effects silently disappear if the `Resources` folder structure is ever reorganised.

---

*Concerns audit: 2026-06-22*
