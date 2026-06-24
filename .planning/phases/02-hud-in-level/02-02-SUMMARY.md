---
phase: 02-hud-in-level
plan: 02
subsystem: ui
tags: [hud, canvas, textmeshpro, singleton, drunkmanager, persistence]

# Dependency graph
requires:
  - phase: 02
    plan: 01
    provides: TMP Essential Resources + PlayerMoneyStore.OnMoneyChanged
provides:
  - HUDController (singleton DontDestroyOnLoad, Canvas Screen Space Overlay por codigo)
  - Barra de borrachera con lerp + indicador de dinero TMP
  - DrunkLevelStore (persistencia de alcohol entre escenas)
affects: [DrunkManager]

# Tech tracking
tech-stack:
  added: []
  patterns: [HUD singleton auto-arrancado patron BackgroundMusicManager, store estatico para nivel de alcohol]

key-files:
  created:
    - Assets/_Project/UI/HUD/HUDController.cs
    - Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs
  modified:
    - Assets/_Project/Core/Managers/DrunkManager.cs

key-decisions:
  - "D-04 revisada: la barra usa NormalizedLevel (lineal) en vez de EffectIntensity (no lineal). La curva ^1.6 subia <1% por trago y daba sensacion de barra rota"
  - "Fill necesita un sprite: Image.type=Filled sin sprite ignora fillAmount y se ve siempre lleno. Se genera sprite blanco 1x1 por codigo"
  - "Font asignado explicitamente (Resources.Load con fallback) porque el HUD se construye BeforeSceneLoad antes de que TMP resuelva su default"
  - "Alcohol persistido via DrunkLevelStore (clase estatica) en vez de hacer DrunkManager DontDestroyOnLoad: respeta el patron del proyecto y evita DrunkManagers duplicados por escena"

patterns-established:
  - "Store estatico para estado de gameplay entre escenas (DrunkLevelStore, analogo a CarStateStore/PlayerMoneyStore)"

requirements-completed: [HUD-01, HUD-02]

# Metrics
duration: 1 sesion (con verificacion humana iterativa)
completed: 2026-06-23
---

# Phase 02 Plan 02: HUDController — Summary

**HUDController singleton con Canvas Screen Space Overlay por codigo: barra de borrachera (lineal, lerp) e indicador de dinero TMP. Alcohol persistente entre escenas via DrunkLevelStore.**

## Accomplishments
- `HUDController.cs` — singleton `DontDestroyOnLoad` auto-arrancado con `RuntimeInitializeOnLoadMethod` (patron `BackgroundMusicManager`). Construye por codigo un Canvas Screen Space Overlay con barra de borrachera y dinero en la esquina inferior izquierda.
- Barra se re-vincula al `DrunkManager` activo en cada `sceneLoaded` (D-02) y refleja el nivel con lerp suave.
- Dinero TMP suscripto a `PlayerMoneyStore.OnMoneyChanged` (D-03), actualizacion instantanea.
- HUD persiste entre escenas sin duplicarse (un solo Canvas, guard de singleton).
- `DrunkLevelStore` — la borrachera ahora es continua entre City/Bar/Home.

## Task Commits

1. **Task 1: Crear HUDController.cs** - `24831c5` (feat) + `0848075` (chore .meta)
2. **Task 2: Verificacion humana en Play Mode** — bugs detectados y corregidos:
   - `264726d` (fix) — layout off-screen + font BeforeSceneLoad + fill sprite + barra lineal
   - `14cfdc9` (feat) — persistencia de alcohol (DrunkLevelStore + wiring DrunkManager)

## Files Created/Modified
- `Assets/_Project/UI/HUD/HUDController.cs` — singleton HUD (Canvas por codigo, lerp, suscripciones, sprite del fill, font explicito)
- `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` — store estatico del nivel de alcohol
- `Assets/_Project/Core/Managers/DrunkManager.cs` — restaura nivel en Awake, guarda en AddAlcohol/ResetLevel

## Deviations from Plan
La verificacion humana (Task 2, gate bloqueante) destapo 4 problemas que el plan no anticipo, corregidos antes de cerrar:
1. **DrunkBar fuera de pantalla** — el spec posicionaba la barra en `anchoredPosition (0,-43)` desde el bottom del grupo → caia debajo del borde. Corregido apilando barra+dinero hacia arriba desde el margen de 24px.
2. **Font sin asignar** — TMP no resolvia su font por defecto al construir el HUD BeforeSceneLoad ("No Font Asset has been assigned"). Asignado `LiberationSans SDF` explicitamente con fallback.
3. **Fill siempre lleno** — `Image.type=Filled` sin sprite ignora `fillAmount`. Agregado sprite blanco 1x1 generado por codigo.
4. **D-04 revisada** — barra cambiada de `EffectIntensity` (no lineal ^1.6) a `NormalizedLevel` (lineal) por decision del usuario: la curva daba feedback imperceptible en los primeros tragos.

Ademas, fuera del alcance original del plan (HUD), el usuario pidio corregir el reseteo del alcohol entre escenas → se agrego `DrunkLevelStore` y se modifico `DrunkManager` (commit separado `14cfdc9`).

## Issues Encountered
Todos los bugs anteriores se detectaron en Play Mode durante el checkpoint humano y se resolvieron iterando con verificacion del usuario. Compilacion final del Editor limpia, sin NullReferenceException ni errores de TMP.

## User Setup Required
None.

## Next Phase Readiness
- HUD-01 y HUD-02 satisfechos: barra de borrachera y dinero visibles en todos los modos, persistentes entre escenas.
- `DrunkLevelStore.Clear()` disponible para sumar a un futuro "Nueva partida" junto al resto de los stores.

---
*Phase: 02-hud-in-level*
*Completed: 2026-06-23*
