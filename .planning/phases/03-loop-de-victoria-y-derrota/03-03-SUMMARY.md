---
phase: 03-loop-de-victoria-y-derrota
plan: "03"
subsystem: gameplay
tags: [unity, csharp, scene-management, game-loop, victory, static-store]

# Dependency graph
requires:
  - phase: 03-loop-de-victoria-y-derrota/03-01
    provides: HomeObjectsTotalStore y DeliveredObjectsStore con TakenCount
  - phase: 03-loop-de-victoria-y-derrota/03-02
    provides: GameManager.OnPlayerArrivedHome() evaluando victoria/derrota

provides:
  - HomeInitializer.cs — captura el total de CarryableObject en Home.Awake() y lo persiste en HomeObjectsTotalStore
  - CityHomeDoorTrigger.cs modificado — delega la decision de escena a GameManager (con fallback sin GameManager)
  - Slice de victoria end-to-end conectado: llegar manejando a Home con condicion cumplida activa Result/Victory

affects:
  - 03-04 (pantalla de resultado — depende de GameResultStore.Result ya seteado)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Captura diferida de total en static store: guard (Total > 0) en Awake() para evitar sobreescritura en recargas"
    - "Delegacion al manager con fallback: FindFirstObjectByType<GameManager>() + comportamiento anterior si null"
    - "triggered=true post-guards, pre-accion: evita re-entrada sin bloquear el primer disparo"

key-files:
  created:
    - Assets/_Project/Core/SceneManagement/HomeInitializer.cs
  modified:
    - Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs
    - Assets/Scenes/Home.unity

key-decisions:
  - "HomeInitializer en Awake() (no Start()) con guard Total > 0 — captura solo la primera carga de Home, sin DontDestroyOnLoad"
  - "triggered=true en CityHomeDoorTrigger se mueve despues de las guards de tag (Pitfall 3) — previene bloqueo permanente del trigger"
  - "Fallback sin GameManager en CityHomeDoorTrigger mantiene backward compatibility al cargar Home directamente"
  - "GameObject 'GameInitializer' creado en Home.unity para alojar HomeInitializer (Home no tenia PlayerSpawner disponible)"

patterns-established:
  - "Static store con guard de idempotencia: if (Store.Total > 0) return; en Awake() — patron re-usable para cualquier store que capture estado de escena una sola vez"
  - "Delegacion segura a manager singleton: FindFirstObjectByType + null check + fallback — aplicar en futuros triggers que dependan de managers"

requirements-completed:
  - GAME-02
  - GAME-03

# Metrics
duration: —
completed: 2026-06-24
---

# Phase 03 Plan 03: Slice de victoria end-to-end — HomeInitializer y delegacion de CityHomeDoorTrigger al GameManager

**HomeInitializer captura el total de CarryableObject en Home al primer Awake() (guard idempotente), y CityHomeDoorTrigger delega al GameManager la decision de cargar Result/Victory o Home segun condicion de victoria**

## Performance

- **Duration:** —
- **Started:** —
- **Completed:** 2026-06-24
- **Tasks:** 2
- **Files modificados:** 3

## Accomplishments

- HomeInitializer.cs creado: captura `FindObjectsByType<CarryableObject>().Length` en `Awake()` con guard `HomeObjectsTotalStore.Total > 0` para evitar sobreescritura en recargas de escena. Incluye `Debug.LogWarning` si el total es 0 (condicion de victoria imposible). Confirmado 6 objetos en Home.unity.
- CityHomeDoorTrigger.cs modificado: `OnTriggerEnter` ahora busca `GameManager` y llama `gm.OnPlayerArrivedHome()` antes de cargar escena. El campo `triggered` se setea despues de las guards de tag (correccion del Pitfall 3) y antes de la accion. Fallback al comportamiento anterior si no hay `GameManager` en la escena.
- Home.unity actualizado: GameObject vacio `GameInitializer` con el componente `HomeInitializer` agregado. Compilacion verificada sin errores.

## Task Commits

1. **Task 1: Crear HomeInitializer y agregar a la escena Home** — `41f596f` (feat)
2. **Task 2: Modificar CityHomeDoorTrigger para delegar al GameManager** — `eb4b6bf` (feat)
3. **Home.unity: agregar GameInitializer con HomeInitializer** — `c385e5f` (feat)

## Files Created/Modified

- `Assets/_Project/Core/SceneManagement/HomeInitializer.cs` — MonoBehaviour que captura el total de `CarryableObject` en `HomeObjectsTotalStore` al primer `Awake()` de la escena Home
- `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` — Trigger modificado para delegar a `GameManager.OnPlayerArrivedHome()` con fallback sin GameManager
- `Assets/Scenes/Home.unity` — GameObject `GameInitializer` con `HomeInitializer` agregado

## Decisions Made

- **HomeInitializer en Awake() con guard idempotente:** Se eligio `Awake()` (no `Start()`) para asegurar que el total quede disponible antes de cualquier otro `Awake()` de `CarryableObject`. El guard `if (HomeObjectsTotalStore.Total > 0) return;` garantiza que recargas sucesivas no sobreescriban el valor inicial.
- **triggered=true post-guards en CityHomeDoorTrigger:** Siguiendo Pitfall 3 del RESEARCH, `triggered` se setea despues de las guards de tag y controlado, no en la primera linea del handler. Esto previene bloqueo permanente si el trigger se activa por un collider incorrecto.
- **Fallback en CityHomeDoorTrigger:** Si `FindFirstObjectByType<GameManager>()` retorna null, el trigger carga `sceneToLoad` directamente — igual que el comportamiento original. Garantiza backward compatibility para escenas sin GameManager.
- **GameObject GameInitializer en Home.unity:** Home no tenia un `PlayerSpawner` disponible en el que agregar el componente, por lo que se creo un GameObject raiz dedicado `GameInitializer`.

## Deviations from Plan

Ninguna — el plan se ejecuto exactamente como estaba escrito. Los tres artefactos se crearon/modificaron con la logica especificada en el PLAN.md.

## Issues Encountered

Ninguno. La compilacion resulto limpia (0 errores) segun la verificacion del orquestador.

## Verificacion en Play mode (pendiente — human-verification)

Las siguientes verificaciones requieren ejecucion interactiva en el Editor de Unity y NO fueron ejecutadas en Play mode. Corresponden a los criterios de aceptacion de GAME-02 y GAME-03:

| Escenario | Condicion | Resultado esperado | Estado |
|-----------|-----------|-------------------|--------|
| A — Victoria | `HomeObjectsTotalStore.Total=6`, 6 ids marcados en `DeliveredObjectsStore`, `DrunkLevelStore.Save(6)` | Trigger carga `Result` con `GameResultStore.Result == Victory` | PENDIENTE — verificar en Play |
| B — Sin condicion de victoria | Stores vacios (sin vender, sin alcohol) | Trigger delega a `GameManager` y carga `Home` normalmente | PENDIENTE — verificar en Play |
| C — Sin GameManager en escena | Quitar GameObject de `GameManager` de City | Fallback carga `Home` sin `NullReferenceException` | PENDIENTE — verificar en Play |
| D — Guard HomeInitializer | Recargar `Home.unity` sin llamar `Clear()` | `Debug.Log` de total NO aparece segunda vez; `Total` mantiene valor | PENDIENTE — verificar en Play |

Estas verificaciones corresponden a los criterios de aceptacion de GAME-02 y GAME-03 del ROADMAP y deben ejecutarse manualmente antes de marcar dichos requerimientos como 100% validados.

## User Setup Required

Ninguno. No se requiere configuracion de servicios externos.

## Next Phase Readiness

- El slice de victoria esta conectado end-to-end: `HomeInitializer` → `HomeObjectsTotalStore` → `GameManager.OnPlayerArrivedHome()` → `GameResultStore` → `SceneManager.LoadSceneAsync("Result")`
- Plan 03-04 puede implementar la pantalla de resultado leyendo `GameResultStore.Result` (ya seteado por `GameManager` en 03-02)
- Sin regresiones conocidas en el flow de derrota (03-02)

---

## Self-Check

### Archivos creados/modificados

- [FOUND] `Assets/_Project/Core/SceneManagement/HomeInitializer.cs` — commit `41f596f`
- [FOUND] `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` — commit `eb4b6bf`
- [FOUND] `Assets/Scenes/Home.unity` — commit `c385e5f`

### Commits verificados

- [FOUND] `41f596f` — HomeInitializer.cs
- [FOUND] `eb4b6bf` — CityHomeDoorTrigger.cs
- [FOUND] `c385e5f` — Home.unity

## Self-Check: PASSED

---
*Phase: 03-loop-de-victoria-y-derrota*
*Completed: 2026-06-24*
