# Testing Patterns

**Analysis Date:** 2026-06-22

## Current State: No Tests Exist

There are **zero test files** in this project. No `.cs` files with `Test`, `Tests`, or `Spec` in their name exist anywhere under `Assets/`. No assembly definition (`.asmdef`) files exist in the project's own code (`Assets/_Project/`).

The `com.unity.test-framework` package **is installed** (version `1.6.0`, declared in `Packages/manifest.json`), but it has not been used yet.

## Test Framework

**Runner:**
- `com.unity.test-framework` 1.6.0
- Status: installed, **not configured**, no tests written
- Config: no `*.asmdef` files exist yet; the Test Runner window (`Window → General → Test Runner`) will show no suites

**Assertion Library:**
- NUnit (bundled with `com.unity.test-framework`)

**Run Commands:**
```
# There are no CLI build scripts or Makefile. Tests are run via the Unity Editor:
#   Window → General → Test Runner → Run All
```

## How to Add Tests (Setup Steps Required First)

Before writing any test, three prerequisites must be in place:

1. **Create a runtime assembly definition** for the code under test, e.g. `Assets/_Project/DrunkSimulator.Runtime.asmdef`. This allows the test assembly to reference it.

2. **Create a test assembly definition**, e.g. `Assets/Tests/EditMode/DrunkSimulator.EditMode.Tests.asmdef` (for Edit Mode tests) or `Assets/Tests/PlayMode/DrunkSimulator.PlayMode.Tests.asmdef` (for Play Mode tests). Set the `testPlatforms` field accordingly and add a reference to the runtime `.asmdef`.

3. **Place test files** under the corresponding `Assets/Tests/` folder, following the naming pattern `[ClassUnderTest]Tests.cs`.

Without assembly definitions, the Unity Test Runner cannot discover or compile test classes.

## What Is Testable (Pure Logic)

The following classes contain logic that does not depend on scene state or Unity lifecycle methods, making them candidates for Edit Mode unit tests:

**`CarStateStore`** (`Assets/_Project/Core/SceneManagement/CarStateStore.cs`):
- Pure static class: `Save()`, `Clear()`, `HasSavedState`, `Position`, `Rotation`
- No MonoBehaviour dependency; testable with plain NUnit

**`DeliveredObjectsStore`** (`Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs`):
- Pure static class: `MarkTaken()`, `IsTaken()`, `Clear()`
- No MonoBehaviour dependency; testable with plain NUnit

**`DrunkManager` — computed properties** (`Assets/_Project/Core/Managers/DrunkManager.cs`):
- `NormalizedLevel`, `EffectIntensity`, `AddAlcohol()` boundary logic
- Requires `[UnityTest]` or Play Mode test (is a MonoBehaviour), but the math is simple enough to extract

**`PickupItem.ResolvedPickupType` and `AlcoholPerSip`** (`Assets/_Project/Gameplay/Items/PickupItem.cs`):
- Name-based type resolution logic (`lowerName.Contains("cerveza")` etc.)
- Testable in Play Mode by instantiating a prefab or as an Edit Mode test if the logic is extracted to a static helper

## Test Patterns to Follow When Tests Are Added

**Edit Mode test skeleton** (NUnit, no scene required):
```csharp
using NUnit.Framework;

public class CarStateStoreTests
{
    [SetUp]
    public void SetUp()
    {
        CarStateStore.Clear();
    }

    [Test]
    public void HasSavedState_IsFalse_WhenNeverSaved()
    {
        Assert.IsFalse(CarStateStore.HasSavedState);
    }
}
```

**Play Mode test skeleton** (requires `[UnityTest]`):
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

public class DrunkManagerTests
{
    [UnityTest]
    public IEnumerator AddAlcohol_ClampsAtMaxLevel()
    {
        var go = new GameObject();
        var manager = go.AddComponent<DrunkManager>();
        yield return null; // let Awake run

        // DrunkManager.maxLevel is private — would require [SerializeField] exposure
        // or a test-only accessor to verify clamping
        Object.Destroy(go);
    }
}
```

## Mocking

**No mocking library is installed.** Unity Test Framework includes NUnit but not NSubstitute, Moq, or similar.

The current architecture makes mocking difficult:
- `DrunkManager` is a `MonoBehaviour` — cannot be `new`-instantiated; must use `AddComponent<T>()` on a `new GameObject()`.
- `FindFirstObjectByType<DrunkManager>()` is called in `Awake` by multiple scripts — tests must ensure a `DrunkManager` GameObject exists in the test scene or the field will be null.
- Static stores (`CarStateStore`, `DeliveredObjectsStore`) are singletons with global state; each test must call `.Clear()` in `[SetUp]` or `[TearDown]`.

## Coverage

**Requirements:** None enforced (no `.coverage` config, no CI pipeline).

**Coverage tool:** Unity's built-in Code Coverage package can be installed separately (`com.unity.testtools.codecoverage`); it is not currently in `manifest.json`.

## Test Types

**Unit Tests (Edit Mode):**
- Scope: pure static classes and logic extracted from MonoBehaviours
- Best candidates: `CarStateStore`, `DeliveredObjectsStore`, `PickupItem.ResolvedPickupType` logic

**Integration/Play Mode Tests:**
- Scope: MonoBehaviour lifecycles — `DrunkManager` alcohol accumulation, `CommandQueue` dequeue timing, scene-transition store writes
- Requires a test scene or `RuntimeInitializeOnLoadMethod` setup

**E2E Tests:**
- Not applicable; no automation framework for full scene walkthroughs is installed

## Missing Infrastructure (Action Items)

To get the first tests running:

1. Add `Assets/_Project/DrunkSimulator.Runtime.asmdef` (no special flags; include `Assets/_Project/**`)
2. Add `Assets/Tests/EditMode/DrunkSimulator.EditMode.Tests.asmdef` (check `Editor` platform only, reference `DrunkSimulator.Runtime`)
3. Write Edit Mode tests for `CarStateStore` and `DeliveredObjectsStore` as a starting point — they are pure static classes with no MonoBehaviour dependency
4. Call `CarStateStore.Clear()` and `DeliveredObjectsStore.Clear()` in `[TearDown]` to avoid state bleed between tests

---

*Testing analysis: 2026-06-22*
