---
phase: 03-loop-de-victoria-y-derrota
plan: "02"
subsystem: gameplay
tags: [unity, csharp, gamemanager, collision, lethalobstacle, citybuilder, scenemanagement]

# Dependency graph
requires:
  - phase: 03-01
    provides: LethalObstacle, GameResultStore, HomeObjectsTotalStore, DeliveredObjectsStore.TakenCount

provides:
  - GameManager con OnCarCrash, OnPlayerArrivedHome (lógica completa) y NewGame() estático
  - CarController.OnCollisionEnter con guard IsControlled y freno de emergencia
  - CityBuilder genera LethalObstacle en cada edificio y árbol al correr Build City Layout
  - Peatones Nino y Mascota en City.unity como obstáculos letales para prueba inmediata

affects:
  - 03-03-PLAN (CityHomeDoorTrigger delega a GameManager.OnPlayerArrivedHome)
  - 03-04-PLAN (ResultScreenController lee GameResultStore.Result seteado aquí)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - FindFirstObjectByType<GameManager>() con null-check como patrón de señalización entre componentes
    - Guard IsControlled en OnCollisionEnter para filtrar colisiones en modo peatón
    - Stores estáticos .Clear() en NewGame() para reset completo de partida

key-files:
  created:
    - Assets/_Project/Core/Managers/GameManager.cs
  modified:
    - Assets/_Project/Gameplay/Vehicles/CarController.cs
    - Assets/_Project/Editor/CityBuilder.cs
    - Assets/Scenes/City.unity

key-decisions:
  - "GameManager no usa DontDestroyOnLoad — vive per-escena en City (D-11)"
  - "OnPlayerArrivedHome implementado con lógica de victoria completa (no stub) en este plan"
  - "minAlcoholRequired = 6 como [SerializeField] para configuración desde el Inspector sin recompilar"
  - "Peatones Nino y Mascota colocados manualmente en City.unity para permitir prueba del slice inmediatamente sin esperar al próximo re-run de CityBuilder"

patterns-established:
  - "Señalización de gameplay: componentes de vehículo usan FindFirstObjectByType<GameManager>() + null-check para notificar eventos"
  - "Reset de partida: NewGame() estático llama .Clear() en todos los stores antes de cargar Home"

requirements-completed:
  - GAME-01
  - GAME-04

# Metrics
duration: —
completed: 2026-06-24
---

# Phase 03 Plan 02: Slice Derrota End-to-End Summary

**GameManager con OnCarCrash/NewGame, CarController.OnCollisionEnter con guard IsControlled, y peatones Nino/Mascota en City.unity cierran el loop de derrota: chocar el auto contra un obstáculo letal carga la escena Result con GameResult.Defeat**

## Performance

- **Duration:** —
- **Started:** —
- **Completed:** 2026-06-24
- **Tasks:** 2
- **Files modificados:** 4

## Accomplishments

- GameManager.cs creado como MonoBehaviour per-escena con lógica completa de derrota (OnCarCrash), victoria (OnPlayerArrivedHome con evaluación de stores) y reset de partida (NewGame estático con Clear() en 7 stores)
- CarController modificado con OnCollisionEnter: guard por IsControlled, freno de emergencia (linearVelocity y angularVelocity a cero), SetControlled(false) y notificación al GameManager
- CityBuilder modificado para agregar LethalObstacle en cada edificio (categoria Casa por defecto) y árbol (categoria Arbol) al ejecutar Build City Layout
- Peatón "Nino" (character-b.fbx + CapsuleCollider no-trigger + LethalObstacle.Nino) y "Mascota" (cubo primitivo + BoxCollider no-trigger + LethalObstacle.Mascota) colocados en City.unity para prueba inmediata de GAME-01

## Task Commits

Cada tarea fue commiteada atómicamente:

1. **Task 1: GameManager con OnCarCrash + OnPlayerArrivedHome + NewGame** - `4312af7` (feat)
2. **Task 2: CarController.OnCollisionEnter + CityBuilder LethalObstacle + peatones City** - `f4a3b91` (feat), `50f7b54` (feat)

## Files Created/Modified

- `Assets/_Project/Core/Managers/GameManager.cs` — MonoBehaviour per-escena: OnCarCrash() setea Defeat y carga Result; OnPlayerArrivedHome() evalúa allDelivered y drunkEnough antes de setear Victory o volver a Home; NewGame() estático limpia 7 stores y carga Home
- `Assets/_Project/Gameplay/Vehicles/CarController.cs` — OnCollisionEnter agregado con guard !isControlled, GetComponent<LethalObstacle> con null-check, freno de emergencia y llamada a GameManager.OnCarCrash()
- `Assets/_Project/Editor/CityBuilder.cs` — PlaceBuilding y PlaceTree llaman AddComponent<LethalObstacle>() con la categoria correcta (Casa / Arbol)
- `Assets/Scenes/City.unity` — GameObject "Nino" en (-6, 0, -35) y "Mascota" en (-6, 0.2, 15) con colliders no-trigger y LethalObstacle configurado

## Decisions Made

- GameManager vive per-escena en City sin DontDestroyOnLoad (D-11): lo buscan CarController y CityHomeDoorTrigger por FindFirstObjectByType en el mismo frame
- OnPlayerArrivedHome implementado con lógica completa de evaluación (no como stub): lee HomeObjectsTotalStore.Total, DeliveredObjectsStore.TakenCount y DrunkLevelStore.AlcoholLevel
- Guard en OnPlayerArrivedHome: si HomeObjectsTotalStore.Total == 0, cargar Home directamente (partida no inicializada, evita falso positivo de victoria)
- minAlcoholRequired = 6 como [SerializeField] con Tooltip en español para configuración desde el Inspector

## Deviations from Plan

None — el plan se ejecutó exactamente como estaba especificado.

## Issues Encountered

None — compilación limpia (0 errores) verificada por el orquestador después de cada commit.

## Notas de seguimiento: reconstruccion de City

Los edificios y árboles ya presentes en City.unity (geometria bakeada antes de este plan) NO tienen el componente LethalObstacle. El cambio en CityBuilder.cs aplica solamente a los GameObjects que se generen la próxima vez que el usuario corra **Drunk Simulator → Build City Layout** desde el menú del Editor.

**Acción requerida por el usuario:** Correr "Drunk Simulator → Build City Layout" para regenerar los obstáculos de la ciudad con LethalObstacle. Esta es una acción destructiva del Editor (regenera la ciudad proceduralmente) que se dejó intencional al usuario para no sobrescribir ajustes manuales existentes en la escena.

Los peatones Nino y Mascota agregados en City.unity en este plan permiten verificar el funcionamiento de GAME-01 de forma inmediata sin necesidad de reconstruir la ciudad.

## User Setup Required

None — no se requiere configuración de servicios externos.

## Next Phase Readiness

- GameManager.OnPlayerArrivedHome() está listo para ser llamado por CityHomeDoorTrigger (plan 03-03)
- GameResultStore.Result == Defeat se setea correctamente antes de cargar Result (plan 03-04 puede leerlo)
- NewGame() estático disponible para el boton "Reintentar" de ResultScreenController (plan 03-04)
- Pendiente: ResultScreenController (03-04) aún no existe; la escena Result carga sin UI — esto es esperado

---

## Self-Check

**Archivos verificados:**

- `Assets/_Project/Core/Managers/GameManager.cs` — creado en commit 4312af7
- `Assets/_Project/Gameplay/Vehicles/CarController.cs` — modificado en commit f4a3b91
- `Assets/_Project/Editor/CityBuilder.cs` — modificado en commit f4a3b91
- `Assets/Scenes/City.unity` — modificado en commit 50f7b54

**Commits verificados:**

- `4312af7` — GameManager.cs creado
- `f4a3b91` — CarController + CityBuilder modificados
- `50f7b54` — City.unity con Nino y Mascota

## Self-Check: PASSED

*Phase: 03-loop-de-victoria-y-derrota*
*Completado: 2026-06-24*
