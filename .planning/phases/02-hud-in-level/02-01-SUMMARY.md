---
phase: 02-hud-in-level
plan: 01
subsystem: ui
tags: [textmeshpro, tmp, unity, events, money]

# Dependency graph
requires:
  - phase: 01
    provides: PlayerMoneyStore (saldo persistente entre escenas)
provides:
  - TMP Essential Resources importados (LiberationSans SDF + TMP Settings)
  - PlayerMoneyStore.OnMoneyChanged (evento estatico Action<int>)
affects: [02-02, HUDController]

# Tech tracking
tech-stack:
  added: [TextMeshPro Essential Resources]
  patterns: [evento estatico OnMoneyChanged analogo a DrunkManager.OnAlcoholLevelChanged]

key-files:
  created:
    - Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset
    - Assets/TextMesh Pro/Resources/TMP Settings.asset
  modified:
    - Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs

key-decisions:
  - "TMP importado programaticamente via AssetDatabase.ImportPackage(path, false) porque el menu item abre un dialogo modal que el MCP no puede confirmar"
  - "OnMoneyChanged es estatico porque PlayerMoneyStore es una clase estatica"

patterns-established:
  - "Notificacion push (evento) en stores estaticos: el HUD se suscribe, no hace polling"

requirements-completed: [HUD-02]

# Metrics
duration: 5min
completed: 2026-06-23
---

# Phase 02 Plan 01: Pre-requisitos del HUD — Summary

**TMP Essential Resources importados y PlayerMoneyStore.OnMoneyChanged (Action<int>) disparando en Add/Spend/Clear**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-23T05:19:00Z
- **Completed:** 2026-06-23T05:24:43Z
- **Tasks:** 2
- **Files modified:** 1 script (+ assets TMP importados)

## Accomplishments
- TMP Essential Resources importados en disco (`Assets/TextMesh Pro/Resources/`), destrabando cualquier `TextMeshProUGUI` creado por codigo
- `PlayerMoneyStore.OnMoneyChanged` agregado y disparando en `Add`, `Spend` (solo gasto exitoso) y `Clear` con el nuevo saldo (D-03)
- Compilacion del Editor limpia, consumidores existentes (SellCounter, PlayerPickup) sin romper

## Task Commits

1. **Task 1: Importar TMP Essential Resources** - `1b1fbf4` (feat)
2. **Task 2: Agregar OnMoneyChanged a PlayerMoneyStore (D-03)** - `e05775c` (feat)

## Files Created/Modified
- `Assets/TextMesh Pro/Resources/...` - Font asset LiberationSans SDF, TMP Settings y materiales/sprites asociados
- `Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs` - Evento estatico OnMoneyChanged + `using System;` + 3 invocaciones

## Decisions Made
- TMP importado via `AssetDatabase.ImportPackage(path, false)` (no interactivo). El menu item `Window/TextMeshPro/Import TMP Essential Resources` abre una ventana modal "Import Unity Package" que el MCP no puede confirmar; la importacion programatica evita el bloqueo.
- El package se ubico en `Library/PackageCache/com.unity.ugui@.../Package Resources/` (TMP esta integrado en com.unity.ugui en Unity 6).

## Deviations from Plan
None - plan ejecutado segun lo escrito. La unica nota es el metodo de importacion de TMP (programatico en vez de via menu interactivo), que el propio plan contemplaba ("Si el MCP no esta conectado, hacerlo manualmente"); aca se automatizo sin diálogo.

## Issues Encountered
- El menu item de TMP abre un dialogo modal no confirmable via MCP. Resuelto importando el `.unitypackage` directamente con `ImportPackage(path, interactive:false)`.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Ambos pre-requisitos del HUD listos: font asset TMP en disco + canal de eventos de dinero.
- Plan 02-02 (HUDController) puede crear el Canvas con `TextMeshProUGUI` y suscribirse a `PlayerMoneyStore.OnMoneyChanged`.

---
*Phase: 02-hud-in-level*
*Completed: 2026-06-23*
