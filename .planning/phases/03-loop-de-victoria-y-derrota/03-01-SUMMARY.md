---
phase: 03-loop-de-victoria-y-derrota
plan: "01"
subsystem: gameplay
tags: [unity, csharp, stores, scene-management, hud, result-screen]

# Dependency graph
requires:
  - phase: 02-econom-a
    provides: DeliveredObjectsStore (base con takenIds HashSet y MarkTaken/IsTaken/Clear)
  - phase: 02-econom-a
    provides: HUDController (singleton DontDestroyOnLoad con barra de borrachera y dinero TMP)

provides:
  - GameResultStore — store estático con enum GameResult { None, Victory, Defeat } y Set/Clear
  - HomeObjectsTotalStore — store estático con int Total y Set(guard >= 0)/Clear
  - LethalObstacle — componente marcador con enum ObstacleCategory { Casa, Arbol, Nino, Mascota }
  - DeliveredObjectsStore.TakenCount — propiedad int que retorna takenIds.Count
  - HUDController.SetVisible(bool) — método estático para ocultar/mostrar el HUD Canvas
  - Assets/Scenes/Result.unity — escena dedicada de resultado registrada en Build Settings (buildIndex 3)

affects:
  - 03-02 (GameManager: lee HomeObjectsTotalStore.Total, GameResultStore.Set, LethalObstacle en OnTriggerEnter)
  - 03-03 (ResultScreenController: lee GameResultStore.Result, llama HUDController.SetVisible(false))
  - 03-04 (HUD de resultado: SetVisible es el mecanismo de ocultado)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Store estático sin MonoBehaviour: propiedad con setter privado, Set(), Clear() — ver DrunkLevelStore/CarStateStore como análogos"
    - "Componente marcador mínimo: MonoBehaviour con un campo SerializeField y propiedad pública, sin Awake/Update"
    - "Checkpoint:human-action para creación de escena Unity (no scripteable por CLI)"

key-files:
  created:
    - Assets/_Project/Gameplay/Systems/LethalObstacle.cs
    - Assets/_Project/Core/SceneManagement/GameResultStore.cs
    - Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs
    - Assets/Scenes/Result.unity
    - Assets/Scenes/Result.unity.meta
    - Assets/_Project/Gameplay/Systems/LethalObstacle.cs.meta
    - Assets/_Project/Core/SceneManagement/GameResultStore.cs.meta
    - Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs.meta
  modified:
    - Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs (+ TakenCount)
    - Assets/_Project/UI/HUD/HUDController.cs (+ SetVisible)
    - ProjectSettings/EditorBuildSettings.asset (Result en buildIndex 3)

key-decisions:
  - "Result.unity creada via Editor (checkpoint:human-action) — imposible por CLI/código; consistente con RESEARCH.md §Open Items #2"
  - "GameResult enum declarado en el mismo archivo que GameResultStore (D-12), sin archivo separado"
  - "ObstacleCategory enum declarado en el mismo archivo que LethalObstacle para cohesión"
  - "HUDController.SetVisible solo verifica instance != null; no restaura visibilidad al salir de Result (DontDestroyOnLoad mantiene gameObject activo)"

patterns-established:
  - "Store estático: clase estática pública, propiedad con get/private set, métodos Set() y Clear() sin MonoBehaviour — patrón canónico para persistencia entre escenas"
  - "Componente marcador: MonoBehaviour mínimo con [SerializeField] y propiedad pública — sin lógica de runtime"
  - "Meta files de scripts se generan en domain reload; deben commitearse junto con el primer commit que introduce el .cs correspondiente o en el commit de checkpoint que sigue"

requirements-completed: [GAME-01, GAME-02, GAME-03, GAME-04]

# Metrics
duration: 45min
completed: 2026-06-24
---

# Phase 03 Plan 01: Contratos base del loop de victoria y derrota

**Tres stores estáticos nuevos (GameResultStore, HomeObjectsTotalStore, LethalObstacle), dos extensiones de contratos existentes (TakenCount, SetVisible), y escena Result.unity registrada en Build Settings — fundamentos que desbloquean los planes 03-02 a 03-04.**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-06-24T13:42:05Z
- **Completed:** 2026-06-24
- **Tasks:** 3 (2 auto + 1 checkpoint:human-action)
- **Files modificados/creados:** 11

## Accomplishments

- Creados LethalObstacle.cs, GameResultStore.cs y HomeObjectsTotalStore.cs — los tres compilan sin errores en Unity 6000.3.11f1
- Extendido DeliveredObjectsStore con TakenCount y HUDController con SetVisible(bool) sin romper funcionalidad existente
- Escena Result.unity creada desde el Editor y registrada en Build Settings en buildIndex 3 (enabled)

## Task Commits

Cada tarea commiteada atómicamente:

1. **Task 1: Crear stores nuevos y LethalObstacle** - `68c7e64` (feat)
2. **Task 2: Extender DeliveredObjectsStore y HUDController** - `3db0f2b` (feat)
3. **Task 3: Crear escena Result y registrar en Build Settings** - `cbe00e6` (feat)

## Files Created/Modified

- `Assets/_Project/Gameplay/Systems/LethalObstacle.cs` — componente marcador con enum ObstacleCategory { Casa, Arbol, Nino, Mascota }
- `Assets/_Project/Core/SceneManagement/GameResultStore.cs` — store estático con enum GameResult { None, Victory, Defeat } y Set/Clear
- `Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs` — store estático con int Total, Set(int) con guard, Clear
- `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` — agregada propiedad `TakenCount => takenIds.Count`
- `Assets/_Project/UI/HUD/HUDController.cs` — agregado método estático `SetVisible(bool visible)`
- `Assets/Scenes/Result.unity` — escena dedicada con Camera y Directional Light
- `ProjectSettings/EditorBuildSettings.asset` — Result.unity en buildIndex 3, enabled
- `.meta` files: LethalObstacle.cs.meta, GameResultStore.cs.meta, HomeObjectsTotalStore.cs.meta, Result.unity.meta

## Decisions Made

- Result.unity se creó vía checkpoint:human-action porque la API de Unity no permite crear escenas desde CLI/scripts externos de forma fiable sin GUID explícito. El orchestrador usó UnityMCP.
- GameResult y ObstacleCategory declarados en el mismo archivo .cs que su clase principal (no archivos separados) — consistente con el estilo existente del proyecto (ver DrunkLevelStore).
- HUDController.SetVisible no restaura visibilidad al volver de Result: DontDestroyOnLoad mantiene el gameObject activo por defecto, y NewGame cargará Home desde cero.

## Deviations from Plan

Ninguna — plan ejecutado exactamente como estaba especificado. Los .meta files de los tres scripts nuevos se generaron en el domain reload de Unity y se incluyeron en el commit del checkpoint (Task 3), lo cual es comportamiento esperado de Unity.

## Issues Encountered

Ninguno. La compilación fue limpia en todos los pasos. Los .meta files aparecieron como untracked porque el orchestrador que ejecutó Tasks 1 y 2 no tuvo acceso a Unity para hacer el domain reload — Unity los generó al abrir la escena Result.

## User Setup Required

Ninguno — no se requiere configuración externa. La escena Result.unity ya está registrada en Build Settings.

## Next Phase Readiness

- Plan 03-02 (GameManager) puede compilar: GameResultStore, HomeObjectsTotalStore, LethalObstacle y DeliveredObjectsStore.TakenCount están disponibles
- Plan 03-03 (ResultScreenController) puede compilar: GameResultStore.Result y HUDController.SetVisible están disponibles
- Plan 03-04 (HUD resultado) puede compilar: la escena Result.unity existe como destino de LoadSceneAsync("Result")
- Sin bloqueantes conocidos para los planes siguientes

## Self-Check

**Archivos creados:**
- `Assets/_Project/Gameplay/Systems/LethalObstacle.cs` — verificado en commit 68c7e64
- `Assets/_Project/Core/SceneManagement/GameResultStore.cs` — verificado en commit 68c7e64
- `Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs` — verificado en commit 68c7e64
- `Assets/Scenes/Result.unity` — verificado en commit cbe00e6
- `Assets/Scenes/Result.unity.meta` — verificado en commit cbe00e6

**Commits verificados:**
- `68c7e64` — stores y LethalObstacle
- `3db0f2b` — TakenCount y SetVisible
- `cbe00e6` — Result.unity y EditorBuildSettings

## Self-Check: PASSED

---
*Phase: 03-loop-de-victoria-y-derrota*
*Completed: 2026-06-24*
