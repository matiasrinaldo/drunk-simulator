---
phase: 03-loop-de-victoria-y-derrota
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 11
files_reviewed_list:
  - Assets/_Project/Core/Managers/GameManager.cs
  - Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs
  - Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs
  - Assets/_Project/Core/SceneManagement/GameResultStore.cs
  - Assets/_Project/Core/SceneManagement/HomeInitializer.cs
  - Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs
  - Assets/_Project/Editor/CityBuilder.cs
  - Assets/_Project/Gameplay/Systems/LethalObstacle.cs
  - Assets/_Project/Gameplay/Vehicles/CarController.cs
  - Assets/_Project/UI/HUD/HUDController.cs
  - Assets/_Project/UI/Screens/ResultScreenController.cs
findings:
  critical: 2
  warning: 5
  info: 4
  total: 11
status: issues_found
---

# Phase 3: Code Review Report

**Reviewed:** 2026-06-24
**Depth:** standard
**Files Reviewed:** 11
**Status:** issues_found

## Summary

This phase adds the victory/defeat game loop: `GameManager` (per-scene, no `DontDestroyOnLoad`), lethal-obstacle crash detection in `CarController`, a result screen, and three new static stores (`GameResultStore`, `HomeObjectsTotalStore`, plus extended `DeliveredObjectsStore`). The cross-scene static-store pattern is applied consistently and most null-checks on `FindFirstObjectByType` are present.

Two BLOCKERs were found. First, the persistent HUD is permanently hidden after the player visits the Result screen — `HUDController.SetVisible(false)` is called on entering Result but no code ever re-enables it (the responsible TODO is left unimplemented in `NewGame()`), so every retry runs with no drunk bar / money UI. Second, the victory condition is built on `DeliveredObjectsStore.TakenCount`, which is incremented at **pickup** time (`CarryableObject.OnPickedUp`), not at sale/delivery time — so a player who picks up every object and drives home **without ever selling them** still satisfies `allDelivered`, which contradicts the "vendiste todo" victory copy and the intended sell-then-deliver loop.

Several WARNINGs concern static-store lifetime across scene reloads, the `triggered` latch never resetting, lethal obstacles on buildings the player must approach, and float-based normalized-level comparisons.

## Narrative Findings (AI reviewer)

## Critical Issues

### CR-01: HUD is permanently hidden after visiting the Result screen

**File:** `Assets/_Project/UI/Screens/ResultScreenController.cs:26`, `Assets/_Project/Core/Managers/GameManager.cs:70-84`, `Assets/_Project/UI/HUD/HUDController.cs:206-209`
**Issue:** Entering the Result scene calls `HUDController.SetVisible(false)`, which does `instance.gameObject.SetActive(false)` on the `DontDestroyOnLoad` HUD. Nothing ever calls `SetVisible(true)` again. After the player presses Reintentar, `GameManager.NewGame()` loads Home but leaves the HUD GameObject disabled — the comment at `GameManager.cs:72` (`// TODO: agregar HUDController.SetVisible(true) si fuera necesario al reiniciar.`) explicitly acknowledges this and leaves it unimplemented. Result: every game after the first defeat/victory runs with no drunk bar and no money indicator for the rest of the session. Because the HUD GameObject is inactive, its `Update` lerp and visual also stop, so even if the player navigates back into gameplay the bar is gone.
**Fix:** Re-enable the HUD when starting a new game (and/or when leaving Result):
```csharp
public static void NewGame()
{
    HUDController.SetVisible(true);   // restaurar el HUD oculto en Result (CR-01)
    CarStateStore.Clear();
    DeliveredObjectsStore.Clear();
    // ... resto de los Clear()
    SceneManager.LoadSceneAsync("Home");
}
```

### CR-02: Victory counts picked-up objects, not delivered/sold objects

**File:** `Assets/_Project/Core/Managers/GameManager.cs:46`, `Assets/_Project/Gameplay/Items/CarryableObject.cs:91-102`
**Issue:** The victory check is `bool allDelivered = DeliveredObjectsStore.TakenCount >= HomeObjectsTotalStore.Total;`. But `DeliveredObjectsStore.MarkTaken` is invoked from `CarryableObject.OnPickedUp` — i.e. the instant the object is **picked up in Home**, before it is ever sold at the Mercado. The variable name `allDelivered` and the victory message ("Vendiste todo y llegaste sano") imply the player must have *sold/delivered* the objects, but the condition is satisfied purely by removing them from the house. A player can grab every object, skip the Mercado entirely, get drunk, drive home, and win — money is never consulted. This is a correctness gap between the stated win condition and the implemented one.
**Fix:** Track delivery (sale) separately from pickup. Either add a distinct counter incremented at sale time (e.g. in the sell-counter logic) and check that in `GameManager.OnPlayerArrivedHome`, or, if "se llevó todo de la casa" is genuinely the intended condition, rename `allDelivered`/`TakenCount`/`MarkTaken` and update the victory copy so the semantics are unambiguous. Do not leave the win condition keyed on pickup while the UI claims sale.

## Warnings

### WR-01: `CityHomeDoorTrigger.triggered` latch never resets, breaking re-entry after a failed victory check

**File:** `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs:11,31-41`
**Issue:** `OnPlayerArrivedHome()` may decide the victory condition is *not* met and call `SceneManager.LoadSceneAsync("Home")` (GameManager.cs:62) — i.e. it bounces the player back to Home without a Result screen. The trigger sets `triggered = true` before delegating and is a per-scene component, so this single City visit is fine. But the design intent of the comment ("para evitar bloqueo del trigger si el GameManager no dispara ninguna transicion") is misleading: every branch of `OnPlayerArrivedHome` *does* call `LoadSceneAsync`, so the no-transition case the latch guards against cannot occur, while the real risk — the async load not completing before another `OnTriggerEnter` for a second collider on the player — is what the latch actually protects. The latch is correct but the rationale in the comment is wrong; more importantly, if `LoadSceneAsync` ever fails to start (invalid scene name in `sceneToLoad` fallback path), the trigger is permanently dead with no recovery.
**Fix:** Keep the latch, but correct the comment and consider resetting `triggered = false` if the load does not begin, or assert the scene is in Build Settings. Low effort, prevents a silent dead-trigger.

### WR-02: Buildings the player must reach are marked as lethal obstacles

**File:** `Assets/_Project/Editor/CityBuilder.cs:200-202,143-173`
**Issue:** `PlaceBuilding` unconditionally adds `LethalObstacle` to **every** building, including `Mercado` and `Bar` — the two destinations the player is supposed to drive to and park beside. Combined with CR-02 (sale is not actually required), the more immediate risk is that approaching the Mercado/Bar to sell or drink while still `IsControlled` triggers an instant defeat on any glancing contact with the building collider. The parking waypoints are offset, but drunk-driving drift (the whole point of the game) makes a clip into these buildings likely.
**Fix:** Exclude destination buildings (Bar, Mercado, Casa) from lethal marking, or give them a non-lethal approach zone:
```csharp
// Solo edificios de relleno son letales; los destinos no.
if (goName != "Bar" && goName != "Mercado" && goName != "Casa_Jugador")
    go.AddComponent<LethalObstacle>();
```

### WR-03: Lethal-obstacle defeat depends on FBX prefabs having colliders — never validated

**File:** `Assets/_Project/Editor/CityBuilder.cs:176-203,224-248`, `Assets/_Project/Gameplay/Vehicles/CarController.cs:125-142`
**Issue:** Defeat fires from `OnCollisionEnter`, which only runs if the obstacle GameObject has a non-trigger `Collider`. `PlaceBuilding`/`PlaceTree` add `LethalObstacle` to instantiated Kenney FBX prefabs but never ensure a `Collider` exists. Kenney CityKit FBX models frequently import with no collider unless mesh colliders are generated. If a building/tree has no collider, the car drives through it and the entire defeat mechanic silently no-ops for that obstacle — there is no runtime warning. The placeholder-cube fallback path *does* get a collider (from `CreatePrimitive`), so the bug only manifests when the real art loads, making it easy to miss in testing.
**Fix:** After instantiating, guarantee a collider:
```csharp
if (go.GetComponentInChildren<Collider>() == null)
    go.AddComponent<BoxCollider>(); // o MeshCollider segun el modelo
```
Consider also logging a warning when a `LethalObstacle` has no collider in `Awake`.

### WR-04: `HomeInitializer` total is captured once and never refreshed; `Clear()` followed by no re-entry leaves stale victory math

**File:** `Assets/_Project/Core/SceneManagement/HomeInitializer.cs:16-33`, `Assets/_Project/Core/Managers/GameManager.cs:39-46`
**Issue:** `HomeInitializer.Awake` early-returns if `HomeObjectsTotalStore.Total > 0`, so the total is captured on the *first* Home load and frozen for the rest of the session. After `NewGame()` calls `HomeObjectsTotalStore.Clear()` (resetting to 0), the next Home load recaptures correctly — that path is fine. However, the guard assumes the very first Home visit contains all objects. If the player can ever reach Home for the first time after some objects were already removed (e.g. a future flow, or an object disabled by `CarryableObject.Awake` because `IsTaken` returned true from a prior partial state that survived), `Total` undercounts and `allDelivered` becomes trivially true. The coupling between "first Home load defines the universe" and the `DeliveredObjectsStore` lifetime is fragile and undocumented as a hard invariant.
**Fix:** Document and enforce that Home must be the first scene loaded in a fresh session, or compute `Total` as `active CarryableObjects + DeliveredObjectsStore.TakenCount` so a partially-emptied first load still yields the true universe size.

### WR-05: Float `NormalizedLevel` compared/assigned with no epsilon; HUD bar can settle slightly off full

**File:** `Assets/_Project/UI/HUD/HUDController.cs:178,191`, `Assets/_Project/Core/Managers/GameManager.cs:47`
**Issue:** Minor robustness: the victory alcohol check (`DrunkLevelStore.AlcoholLevel >= minAlcoholRequired`) is integer-based and safe. But the HUD `targetFillAmount = drunkManager.NormalizedLevel` is a float driven by an asymptotic `Mathf.Lerp(fillAmount, target, 3f * Time.deltaTime)` that never exactly reaches `target`. At max drunkenness the bar visually stalls a hair below full and never reads 100%. Not a crash, but the bar never communicates "maxed out", which matters for a game whose core feedback is the drunk meter.
**Fix:** Snap when close: `if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) < 0.001f) fillImage.fillAmount = targetFillAmount;` after the lerp.

## Info

### IN-01: Unused `using System;` in ResultScreenController

**File:** `Assets/_Project/UI/HUD/HUDController.cs:1`
**Issue:** `using System;` is imported in `HUDController.cs` but `System` types are not referenced (the file uses TMPro, UnityEngine, SceneManagement, UI). Dead import.
**Fix:** Remove `using System;`.

### IN-02: TODO left in shipped code path

**File:** `Assets/_Project/Core/Managers/GameManager.cs:72`
**Issue:** `// TODO: agregar HUDController.SetVisible(true) si fuera necesario al reiniciar.` — this TODO marks the exact omission that causes CR-01. Resolve it rather than ship it.
**Fix:** Implement per CR-01 and delete the TODO.

### IN-03: `CreateSolidSprite` duplicated verbatim across two UI files

**File:** `Assets/_Project/UI/HUD/HUDController.cs:144-150`, `Assets/_Project/UI/Screens/ResultScreenController.cs:251-257`
**Issue:** Identical 1x1 solid-sprite factory copied into both controllers. Each call also allocates a new `Texture2D`+`Sprite` per element with no caching (every button/panel gets its own 1x1 texture). Maintainability duplication; correctness is fine.
**Fix:** Extract a shared `UISpriteUtil.SolidSprite()` (static, cached single instance) and reuse from both controllers.

### IN-04: Verbose per-event `Debug.Log` left enabled in gameplay paths

**File:** `Assets/_Project/Core/Managers/GameManager.cs:23,41,49-51,55,61,82`, `Assets/_Project/UI/HUD/HUDController.cs:167`, `Assets/_Project/Core/SceneManagement/HomeInitializer.cs:27`
**Issue:** Multiple unconditional `Debug.Log` calls fire on every crash, scene load, and victory evaluation. Fine for development, but these run in builds too (no `#if UNITY_EDITOR` / conditional). The scene-load log in `HUDController.OnSceneLoaded` runs on every scene transition for the lifetime of the session.
**Fix:** Gate behind a debug flag or wrap in `[Conditional("DEVELOPMENT_BUILD")]`/`#if UNITY_EDITOR` where these are not needed in production.

---

_Reviewed: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
