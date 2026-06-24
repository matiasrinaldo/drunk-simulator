# Phase 4: Animaciones - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 7 (3 modify, 3 new assets, 1 new editor script optional)
**Analogs found:** 6 / 6 (every code change has a strong in-repo analog)

> Unity 6000.3.11f1 / URP 17.3. Global namespace (no `namespace`), Spanish comments, legacy Input API, code lives in `Assets/_Project/` organized by responsibility. This phase is **additive**: new Animator assets + per-frame/coroutine edits on existing scripts. No source file is renamed; no persistent store is added.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Assets/_Project/UI/HUD/HUDController.cs` (MODIFY) | UI controller (singleton) | event-driven + per-frame transform | itself (`Update` fill lerp) + `MouseLook.cs` (EffectIntensity consumer) | exact (in-file pattern) |
| `Assets/_Project/Gameplay/Player/PlayerPickup.cs` (MODIFY) | gameplay controller | event-driven (one-shot signal emit) | itself (`!CanAfford` reject branch L136-142) | exact (in-file pattern) |
| `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` *(or a new decorative `DoorProximityTrigger`)* (MODIFY/NEW) | gameplay trigger | event-driven (`OnTriggerEnter` → `Animator.SetTrigger`) | `BarDoorTrigger.cs` / `CityHomeDoorTrigger.cs` `OnTriggerEnter` | role + flow match |
| `Assets/_Project/Animations/Door.controller` (NEW asset) | animator-controller asset | state-machine | none (zero `.controller` outside ThirdParty) | **NO ANALOG** |
| `Assets/_Project/Animations/DoorOpen.anim` (NEW asset) | animation-clip asset | transform curve | none | **NO ANALOG** |
| `Assets/_Project/Editor/DoorAnimatorBuilder.cs` *(optional)* | editor tool | asset generation (`AssetDatabase`/`AnimatorController` API) | `Assets/_Project/Editor/CityBuilder.cs` | role match (editor asset-gen) |

> ANIM-01 candidate is locked by RESEARCH + UI-SPEC to a **world-space decorative door** (opens on approach, does NOT load a scene), to avoid the scene-load timing pitfall (RESEARCH Pitfall 3). The planner decides whether the trigger reuses `BarDoorTrigger`/`CityHomeDoorTrigger` or is a new decoupled `DoorProximityTrigger`. The recommended low-risk path is a NEW decorative trigger that does not touch the scene-load critical path.

---

## Pattern Assignments

### `HUDController.cs` — Motion Token A: Drunk-Bar Pulse (UI controller, per-frame transform)

**Analog 1 (per-frame EffectIntensity consumer):** `MouseLook.cs` — the canonical drunk-distortion pattern to copy verbatim for the pulse math.

`MouseLook.cs:16,20-33` — cache `DrunkManager` in `Awake` with `FindFirstObjectByType` fallback:
```csharp
DrunkManager drunkManager;
void Awake()
{
    if (playerBody != null) drunkManager = playerBody.GetComponent<DrunkManager>();
    if (drunkManager == null) drunkManager = FindFirstObjectByType<DrunkManager>();
}
```
> **HUDController already owns this binding** — do NOT re-implement caching. `drunkManager` is re-bound every scene load in `OnSceneLoaded` (`HUDController.cs:152-167`, `drunkManager = FindFirstObjectByType<DrunkManager>()`). The pulse reads that existing field.

`MouseLook.cs:39,44-48` — read `EffectIntensity` each frame, build a sine term scaled by it, multiply:
```csharp
float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;
float pitchWobble = Mathf.Sin(Time.time * pitchWobbleFrequency) * pitchWobbleAmount * drunkAmount;
...
transform.localRotation = Quaternion.Euler(xRotation + pitchWobble, yawWobble, rollWobble);
```
**Copy this exact shape** for the pulse: `float k = drunkManager != null ? drunkManager.EffectIntensity : 0f;` then `amp = Mathf.Lerp(0f, 0.08f, k)`, `f = Mathf.Lerp(0f, 1.8f, k)`, `s = 1f + Mathf.Sin(Time.time * f * 2f * Mathf.PI) * amp` (UI-SPEC Token A values). Below `k <= 0.05f` set `DrunkBar.localScale = Vector3.one` exactly (UI-SPEC activation threshold).

**Analog 2 (where to put it + the landmine):** the existing fill lerp in `HUDController.Update` is the integration point AND the thing the pulse must NOT fight.

`HUDController.cs:187-197` — existing `Update` (the pulse goes here, on a DIFFERENT object/property):
```csharp
private void Update()
{
    if (fillImage == null) return;
    fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, 3f * Time.deltaTime);
    if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) < 0.001f)
        fillImage.fillAmount = targetFillAmount;
}
```
**Hard rule (UI-SPEC Landmine, lines 112-128):** the fill lerp owns `Fill` Image → `fillAmount`. The pulse owns `DrunkBar` (the container `RectTransform`) → `localScale`. Different object, different property → cannot fight. **Never** drive the pulse through `fillAmount` and **never** scale the `Fill` child alone.

**Plumbing the executor must add (no new field exists today):** `HUDController` only caches `fillImage` and `moneyText` (`HUDController.cs:18-21`). To pulse the bar container, capture a reference to the `DrunkBar` `RectTransform` in `BuildHUD` (the `barRect` local at `HUDController.cs:101-107`) into a new private field, then scale it in `Update`. Element hierarchy is verified: `HUDGroup → DrunkBar (sizeDelta 220×20) → Track → Fill` (`BuildHUD`, `HUDController.cs:63-136`). Pivot of `DrunkBar` is `(0,0)` (`barRect.pivot = Vector2.zero`, L105) — scaling around a corner; UI-SPEC requires the throb read centered, so the executor must either temporarily scale around center or insert a centered child wrapper (executor's call, UI-SPEC Token A "Pivot for scale").

---

### `HUDController.cs` — Motion Token B: Money-Text Wobble (UI controller, one-shot coroutine)

**Analog 1 (the one-shot damped-pose coroutine template):** `PlayerPickup.AnimateHeldDrinkPose` — the project's existing per-frame `elapsed/duration` lerp with smoothstep easing and a guaranteed final snap.

`PlayerPickup.cs:330-355`:
```csharp
IEnumerator AnimateHeldDrinkPose(Transform t, Vector3 fromPos, Quaternion fromRot,
    Vector3 toPos, Quaternion toRot, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        if (t == null) yield break;
        elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(elapsed / duration);
        k = k * k * (3f - 2f * k);                 // smoothstep ease (project convention)
        t.localPosition = Vector3.Lerp(fromPos, toPos, k);
        t.localRotation = Quaternion.Slerp(fromRot, toRot, k);
        yield return null;
    }
    if (t == null) yield break;
    t.localPosition = toPos;                       // guaranteed final snap → no drift
    t.localRotation = toRot;
}
```
**Adapt to the wobble:** one coroutine over `duration = 0.30s` writing `moneyText.rectTransform.anchoredPosition.x = baseX + Mathf.Sin(elapsed*12f*2π) * 6f * (1 - elapsed/duration)` (UI-SPEC Token B: 6px start, 12Hz, linear-decay envelope). **Mirror the final-snap discipline** (L353-354): on completion reset `anchoredPosition` to the built value `(0, 28)` (`moneyRect.anchoredPosition = new Vector2(0f, 28f)`, `HUDController.cs:79`) and color to `Color.white` (L92) — UI-SPEC "Reset guarantee" / "No drift".

**Re-trigger discipline:** mirror `PlayerPickup`'s single-routine handle pattern (`drinkAnimationRoutine` field + `StopDrinkAnimation()`, `PlayerPickup.cs:48,385-394`): keep ONE `Coroutine moneyWobbleRoutine` field; on a fresh rejection, `StopCoroutine` the old one and restart, then re-snap base before starting (UI-SPEC "Re-trigger: restart, do not stack offsets").

> `moneyText` is `TMP_Text`; UI elements use `anchoredPosition`, never world `position` (RESEARCH Pitfall 5). Use `moneyText.rectTransform`.

---

### `PlayerPickup.cs` — emit the rejection signal (gameplay controller, one-shot event)

**Analog (the exact branch to hook):** the insufficient-funds reject path already exists and already side-effects (plays SFX). The wobble signal goes on the SAME branch, same moment as the SFX.

`PlayerPickup.cs:135-142` — `Update` → `pickupKey` → buy attempt:
```csharp
int precio = currentPickupItem.Price;
if (!PlayerMoneyStore.CanAfford(precio))
{
    // SFX de rechazo si no hay plata suficiente (D-10)
    if (rejectClip != null && sfxSource != null)
        sfxSource.PlayOneShot(rejectClip, Mathf.Clamp01(rejectVolume));
    return;                       // ← insert the HUD wobble signal right here, before return
}
```
**Add one line in this block:** `HUDController.FlashMoneyRejected();` (UI-SPEC Token B "Trigger", lines 99-100). The executor must NOT invent a new persistent store for this — it is a transient one-shot, unlike the cross-scene stores.

**Analog for the signal API on `HUDController` (static accessor over a singleton):** `HUDController.SetVisible` already proves the pattern — a `public static` method that forwards to the live `instance`.

`HUDController.cs:211-214`:
```csharp
public static void SetVisible(bool visible)
{
    if (instance != null) instance.gameObject.SetActive(visible);
}
```
**Mirror it** for the wobble: `public static void FlashMoneyRejected() { if (instance != null) instance.StartMoneyWobble(); }` (UI-SPEC explicitly suggests `HUDController.FlashMoneyRejected()` as the wiring, line 100). This keeps `PlayerPickup` decoupled from the HUD internals — same shape as the existing `SetVisible` call site.

> Alternative wiring (planner's call): a `public static event Action` on a store, mirroring `PlayerMoneyStore.OnMoneyChanged` (`PlayerMoneyStore.cs:13,24`) which `HUDController` already subscribes to in `Awake`/`OnDestroy` (`HUDController.cs:41,201`). The static-method-over-singleton route (above) is lighter and matches `SetVisible`; the event route matches the money subscription. Either is in-pattern.

---

### Door trigger — fire the Animator (gameplay trigger, event-driven)

**Analog:** `BarDoorTrigger.OnTriggerEnter` / `CityHomeDoorTrigger.OnTriggerEnter` — tag-gated, single-fire `OnTriggerEnter` with a `triggered` guard and `[RequireComponent(typeof(Collider))]` + `Reset()` that forces `isTrigger`.

`BarDoorTrigger.cs:4-31`:
```csharp
[RequireComponent(typeof(Collider))]
public class BarDoorTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "player";
    private bool triggered;
    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;
        // ... action ...
    }
}
```
**For the decorative door (recommended, RESEARCH Pitfall 3 / Open Question 1):** copy this skeleton into a new `DoorProximityTrigger` that, instead of loading a scene, holds `[SerializeField] private Animator doorAnimator;` and calls `doorAnimator.SetTrigger("Open")` inside the guarded block. **Crucially it does NOT call `SceneManager.LoadSceneAsync`** — so the door is visibly opened without the scene unloading. The `triggered` guard prevents re-fire. This is **runtime-pure** (no `UnityEditor` reference). RESEARCH Code Example lines 267-285 shows the exact runtime shape (the `LoadAfterDoor` delay is only needed if you reuse a scene-loading trigger; the decorative door avoids it entirely).

> **Anti-pattern to avoid (RESEARCH Pitfall 1):** the `Animator` must sit on a door GameObject that NO gameplay script or Rigidbody also moves, or the Animator wins the transform every frame. Keep it off the player/car. Disable `Apply Root Motion`. Door mesh must be parented to a hinge pivot (RESEARCH ANIM-01 detail, lines 99, 322-324).

---

### `Door.controller` + `DoorOpen.anim` (NEW assets) — optional generator `DoorAnimatorBuilder.cs`

**No runtime analog** (zero `.controller`/`.anim` outside ThirdParty — verified). Two creation routes; planner picks one (RESEARCH Open Question 2):

1. **Manual authoring** (faster for 2 states / 2 keyframes): Animation window → record hinge rotation 0°→~95° over ~0.5s ease-out; AnimatorController with `Closed` (default) + `Open`, parameter `Open` (Trigger), transition `Closed→Open` condition `If Open`, `Has Exit Time = false`. RESEARCH lines 94-98, 142.

2. **Scripted in `Editor/`** — analog for editor asset-generation is `CityBuilder.cs`.

**Analog for editor asset-gen:** `CityBuilder.cs` — `[MenuItem]`-driven editor-only tool that creates assets via `AssetDatabase`, guarded folder creation, idempotent load-or-create.

`CityBuilder.cs:1-9,37-38` — editor-only header + menu entry (mirror namespace/structure):
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CityBuilder            // global namespace, static editor tool
{
    [MenuItem("Drunk Simulator/Build City Layout")]
    static void Build() { ... }
}
```
`CityBuilder.cs:295-312` — guarded folder + load-or-create-asset idempotency (mirror for `Door.controller`):
```csharp
static void EnsureMaterialsFolder()
{
    if (!AssetDatabase.IsValidFolder("Assets/Art/Materials/City"))
        AssetDatabase.CreateFolder("Assets/Art/Materials", "City");
}
static Material GetOrCreateMat(string matName, Color color)
{
    string path = $"{MaterialsDir}/{matName}.mat";
    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (mat == null)
    {
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(mat, path);
    }
    return mat;
}
```
**For a `DoorAnimatorBuilder`:** mirror this — a `public static class DoorAnimatorBuilder` in `Assets/_Project/Editor/` with `[MenuItem("Drunk Simulator/Build Door Animator")]`, ensure the `Assets/_Project/Animations` folder via `AssetDatabase.IsValidFolder`/`CreateFolder`, then `AnimatorController.CreateAnimatorControllerAtPath(...)` + `AddParameter`/`AddState`/`AddTransition`/`AddCondition` and `clip.SetCurve(...)` (RESEARCH Code Example lines 247-265). **`UnityEditor.Animations` and `AnimationClip.SetCurve` are Editor-only** — this file MUST live under `Editor/` and MUST NOT be referenced from runtime (RESEARCH Pitfall 2). `CityBuilder` already establishes that all editor asset-gen lives in `Assets/_Project/Editor/`.

---

## Shared Patterns

### EffectIntensity consumer (cache in Awake + FindFirstObjectByType fallback, read per-frame, multiply)
**Source:** `MouseLook.cs:16,20-48` (also `PlayerMovement`, `CarController` per CLAUDE.md).
**Apply to:** Motion Token A pulse in `HUDController.Update`.
```csharp
float k = drunkManager != null ? drunkManager.EffectIntensity : 0f;   // 0..1 non-linear
// distorsion = waveform(Time.time * freq) * amplitude * k;
```
> `HUDController` already binds `drunkManager` (`OnSceneLoaded`, L157) and re-binds per scene — reuse that field, do not add a second cache.

### Single-coroutine handle + guaranteed final snap (one-shot pose animation)
**Source:** `PlayerPickup.cs:48,330-355,385-394` (`drinkAnimationRoutine`, `AnimateHeldDrinkPose`, `StopDrinkAnimation`).
**Apply to:** Motion Token B money wobble (one `moneyWobbleRoutine` field; smoothstep/envelope; reset to base `(0,28)` + white on end; restart-don't-stack on re-trigger).

### Static accessor over a DontDestroyOnLoad singleton (decoupled UI signal)
**Source:** `HUDController.cs:16,211-214` (`SetVisible`).
**Apply to:** `HUDController.FlashMoneyRejected()` called from `PlayerPickup.cs:141`.

### Tag-gated single-fire `OnTriggerEnter` (proximity trigger)
**Source:** `BarDoorTrigger.cs:4-31`, `CityHomeDoorTrigger.cs:4-50` (`[RequireComponent(typeof(Collider))]`, `Reset()` sets `isTrigger`, `triggered` guard, `CompareTag(playerTag)`).
**Apply to:** the decorative door trigger that calls `doorAnimator.SetTrigger("Open")`.

### Resources.Load with fallback path
**Source:** `HUDController.cs:86-87` (font), `PlayerPickup.cs:88-112` (SFX clips).
**Apply to:** only relevant if any animation loads a resource (it does not, this phase) — listed for completeness / project convention.

### Editor-only asset generation (`[MenuItem]` + `AssetDatabase` idempotent load-or-create)
**Source:** `CityBuilder.cs:1-9,37,295-312`.
**Apply to:** optional `DoorAnimatorBuilder.cs` (must stay under `Editor/`, never referenced from runtime — Pitfall 2).

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `Assets/_Project/Animations/Door.controller` | animator-controller asset | state-machine | Project has zero `.controller` outside `ThirdParty/`; no in-repo Animator to copy. Use RESEARCH Code Example (lines 247-265) + Unity docs. |
| `Assets/_Project/Animations/DoorOpen.anim` | animation-clip asset | transform curve | No `.anim` outside ThirdParty. Author manually (2 keyframes) or via `SetCurve` (Editor-only). RESEARCH lines 94-96, 141. |

> For both, the planner should source the pattern from `04-RESEARCH.md` ("Unity 6 / URP Specifics" + "Code Examples"), not from the codebase. The `DoorProximityTrigger` runtime hook and the optional generator DO have in-repo analogs (above).

---

## Metadata

**Analog search scope:** `Assets/_Project/UI/HUD/`, `Assets/_Project/Gameplay/Player/`, `Assets/_Project/Gameplay/Items/`, `Assets/_Project/Core/Managers/`, `Assets/_Project/Core/SceneManagement/`, `Assets/_Project/Editor/`; `find Assets -name '*.controller' -not -path '*ThirdParty*'` (zero hits).
**Files scanned:** 10 source files read/grepped (HUDController, PlayerPickup, DrunkManager, MouseLook, CarryableObject, BarDoorTrigger, CityHomeDoorTrigger, PlayerMoneyStore, CityBuilder, store list).
**Pattern extraction date:** 2026-06-24
