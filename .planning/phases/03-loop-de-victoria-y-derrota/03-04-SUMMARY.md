---
phase: 03-loop-de-victoria-y-derrota
plan: "04"
subsystem: ui
tags: [unity, ugui, textmeshpro, canvas, result-screen, victory, defeat]

# Dependency graph
requires:
  - phase: 03-loop-de-victoria-y-derrota
    plan: "01"
    provides: "GameResultStore, HUDController.SetVisible, GameResultStore.Clear()"
  - phase: 03-loop-de-victoria-y-derrota
    plan: "02"
    provides: "GameManager.NewGame() — limpia todos los stores y carga Home"
  - phase: 03-loop-de-victoria-y-derrota
    plan: "03"
    provides: "Result.unity vacía y flujos de transición activando GameResultStore"
provides:
  - "ResultScreenController.cs — MonoBehaviour que construye el Canvas de resultado por código"
  - "Result.unity poblada con ResultController, Main Camera, Directional Light y EventSystem"
  - "Pantalla Victory verde (#1A2E1A) con título 'LLEGASTE A CASA' en #4CAF50"
  - "Pantalla Defeat roja (#2E1A1A) con título 'CHOCASTE' en #E53935"
  - "Botones Reintentar/Jugar de nuevo → GameManager.NewGame(); Salir → Application.Quit()"
  - "Cursor libre + timeScale=1 al entrar a Result; HUD oculto durante la pantalla"
affects:
  - phase-04
  - validacion-final
  - loop-gameplay

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Canvas construido por código (sin prefabs de escena) — misma convención que HUDController"
    - "CreateSolidSprite() privado para Image de fondo renderizable sin Sprite asignado"
    - "ColorBlock configurado por código con Lerp para highlighted/pressed"
    - "Lectura directa de store estático (GameResultStore.Result) para bifurcar layout Victory/Defeat"

key-files:
  created:
    - "Assets/_Project/UI/Screens/ResultScreenController.cs"
  modified:
    - "Assets/Scenes/Result.unity"

key-decisions:
  - "Canvas construido 100% por código en Start() — sin dependencias de prefab en la escena"
  - "HUDController.SetVisible(false) llamado antes de BuildResultUI() para evitar HUD visible sobre resultado (Pitfall 4 de RESEARCH.md)"
  - "CreateSolidSprite() duplicado en ResultScreenController (no importar HUDController directamente)"
  - "EventSystem+StandaloneInputModule agregados a Result.unity para que los botones respondan al click"
  - "Tilde corregida en copy de Derrota: 'El alcohol ganó esta vez.' (acento en ó según contrato de copywriting)"

patterns-established:
  - "Patrón pantalla de resultado: leer store estático en Start() → bifurcar colores/copy → construir Canvas → conectar botones con AddListener"

requirements-completed:
  - GAME-03
  - GAME-04

# Metrics
duration: implementado por agente previo
completed: "2026-06-24"
---

# Phase 03 Plan 04: Pantalla de Resultado Summary

**ResultScreenController construye por código el Canvas Victory/Defeat con colores exactos de UI-SPEC, copy correcto, botones funcionales y Result.unity poblada con EventSystem — cerrando el loop de gameplay completo.**

## Performance

- **Duration:** Implementado por agente de ejecución previo; metadatos registrados 2026-06-24
- **Started:** 2026-06-24
- **Completed:** 2026-06-24
- **Tasks:** 2 completadas (+ 1 fix de acento en copy)
- **Files modified:** 2

## Accomplishments

- ResultScreenController.cs construye el Canvas completo por código en Start(): overlay oscuro (#000000 a=0.55), panel centrado 640x360, TitleText 52px Bold con Shadow, MessageText 24px sin Shadow, ButtonGroup con VerticalLayoutGroup, dos botones con ColorBlock configurado
- Result.unity poblada con GameObject "ResultController" (ResultScreenController), "Main Camera" (tag MainCamera, sin AudioListener), "Directional Light" y "EventSystem" (+StandaloneInputModule) — sin necesidad de agregar UI manualmente a la escena
- Copy de derrota corregido con acento correcto: "El alcohol ganó esta vez. Siempre hay otra." (fix de acento en 'ó' para coincidir exactamente con el contrato de copywriting de UI-SPEC)

## Task Commits

Cada task fue commiteado atómicamente:

1. **Task 1: Crear ResultScreenController con UI por código según UI-SPEC** - `17d1e39` (feat)
2. **Fix: acento en copy de Derrota** - `32fa0b0` (fix — desviación auto-corregida)
3. **Task 2: Agregar ResultScreenController a Result.unity** - `19c34a7` (feat)

## Files Created/Modified

- `Assets/_Project/UI/Screens/ResultScreenController.cs` — MonoBehaviour que construye el Canvas de resultado por código, lee GameResultStore.Result, aplica colores/copy según estado Victory/Defeat, conecta botones con AddListener
- `Assets/Scenes/Result.unity` — escena Result poblada: ResultController (ResultScreenController), Main Camera, Directional Light, EventSystem+StandaloneInputModule

## Decisions Made

- Canvas 100% por código en Start() sin prefabs de escena, siguiendo el patrón establecido por HUDController.cs
- CreateSolidSprite() duplicado localmente en ResultScreenController (no importar HUDController) para mantener bajo acoplamiento entre scripts de UI
- EventSystem+StandaloneInputModule agregados a Result.unity: sin ellos los botones no responden al click en una escena construida sin UI preexistente
- HUDController.SetVisible(false) llamado como primer paso de Start() antes de BuildResultUI() para garantizar que el HUD no sea visible sobre la pantalla de resultado (Pitfall 4 del RESEARCH.md)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Acento faltante en copy de Derrota**
- **Found during:** Task 1 (revisión del contrato de copywriting)
- **Issue:** El copy inicial tenía "gano" (sin tilde) en lugar de "ganó", que no coincidía con el contrato exacto de UI-SPEC
- **Fix:** Corregido a "El alcohol ganó esta vez. Siempre hay otra."
- **Files modified:** `Assets/_Project/UI/Screens/ResultScreenController.cs`
- **Verification:** Coincide byte a byte con el contrato de copywriting de 03-UI-SPEC.md
- **Committed in:** `32fa0b0` (fix commit separado)

---

**Total deviations:** 1 auto-fix (Rule 1 — corrección de copy)
**Impact on plan:** Corrección menor de texto. Sin cambio de comportamiento ni scope creep.

## Smoke Test — Evidencia de Verificación (Play mode)

El orquestador ejecutó una verificación en modo Play en Result.unity antes de registrar este SUMMARY:

| Criterio | Resultado |
|----------|-----------|
| Compilación limpia (0 errores) | PASS |
| 0 NullReferenceException en consola | PASS |
| ResultCanvas construido en runtime | PASS |
| TitleText visible ("CHOCASTE" — None defaultea a Defeat) | PASS |
| MessageText presente | PASS |
| RetryButton presente | PASS |
| QuitButton presente | PASS |
| Exactamente 1 EventSystem en escena | PASS |
| Cursor.visible = true | PASS |
| Time.timeScale = 1 | PASS |

**must_haves verificados por código/smoke-test:**
- CanvasScaler 1920x1080 ScaleWithScreenSize matchWidthOrHeight 0.5: confirmado en código (ResultScreenController.cs)
- Overlay #000000 a=0.55: `new Color(0f, 0f, 0f, 0.55f)` en código
- Panel Defeat #2E1A1A: `new Color(0.180f, 0.102f, 0.102f, 0.92f)` en código
- Panel Victory #1A2E1A: `new Color(0.102f, 0.180f, 0.102f, 0.92f)` en código
- Copy Defeat "El alcohol ganó esta vez. Siempre hay otra.": verificado contra contrato
- Copy Victory "Vendiste todo y llegaste sano. Por ahora.": verificado contra contrato
- TitleText con Shadow; MessageText sin Shadow: confirmado en código
- sortingOrder = 10 en Canvas: confirmado en código

## Human-Verification Items (E2E — requieren play interactivo)

Los siguientes escenarios necesitan verificación manual por el desarrollador según 03-VALIDATION.md. La pantalla de resultado en sí está verificada (smoke test arriba); las transiciones E2E son human-verification:

| Escenario | Pasos | Criterio de aceptación |
|-----------|-------|------------------------|
| GAME-01: Derrota por choque | City → entrar al auto → conducir contra edificio → Result carga | Panel rojo, "CHOCASTE", cursor visible, HUD oculto |
| GAME-01 negativo | City → caminar a pie contra edificio | Sin transición a Result |
| GAME-02: Victoria completa | Bar → tomar tragos → City → vender todo en SellCounter → conducir a Home | Panel verde, "LLEGASTE A CASA", cursor visible |
| GAME-02 negativo | Llegar a Home sin haber vendido todo | Home carga normalmente, sin pantalla de resultado |
| D-09/D-13: Retry | Desde Defeat → click "Reintentar" | Home carga, 6 objetos presentes, alcohol=0, dinero=0, HUD visible |
| D-09: Retry Victory | Desde Victory → click "Jugar de nuevo" | Ídem anterior |
| D-08: Salir | Click "Salir" | Console muestra "[ResultScreen] Salir presionado." |
| Criterio 5 | Partida sin chocar ni completar objetivo | GameResultStore.Result == None, sin pantalla de resultado |

## Issues Encountered

- Sin problemas bloqueantes. El único ajuste fue el fix del acento en el copy de Derrota (ver Deviations).

## User Setup Required

Ninguno — no se requiere configuración de servicios externos.

## Next Phase Readiness

**El loop de gameplay completo está implementado.** La fase 03 cierra los 4 planes:
- 03-01: GameResultStore, HUDController.SetVisible, DrunkLevelStore, escena Result
- 03-02: GameManager.NewGame() con limpieza de stores, CarCollisionDetector, VictoryTrigger
- 03-03: Flujos de transición (choque → Defeat, venta+borrachera+Home → Victory)
- 03-04: ResultScreenController + Result.unity — pantalla distinguible con botones funcionales

**Pendiente para validación final:**
- Verificación interactiva E2E de los 5 success criteria del Roadmap Phase 3 (tabla arriba)
- Los ítems de human-verification de 03-VALIDATION.md aún necesitan play-testing completo

---
*Phase: 03-loop-de-victoria-y-derrota*
*Completed: 2026-06-24*
