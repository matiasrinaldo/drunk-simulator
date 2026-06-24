# Phase 3: Loop de victoria y derrota - Research

**Researched:** 2026-06-24
**Domain:** Unity 6000.3.11f1 — game-loop closure: collision detection, win condition evaluation, result screen UI, scene management, store reset
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Detección de derrota (choque)**
- D-01: Obstáculos letales marcados con componente marcador `LethalObstacle` (MonoBehaviour, enum de categoría). Detección por `OnCollisionEnter` en el auto → `GetComponent<LethalObstacle>()`. El `CityBuilder` debe agregar el componente a los obstáculos generados.
- D-02: Cualquier contacto con un obstáculo letal mientras `CarController.IsControlled == true` cuenta como derrota. Sin umbral de velocidad.
- D-03: Al confirmarse la derrota, el auto se frena (`rb.linearVelocity / angularVelocity → 0`) y se bloquea el control (`SetControlled(false)`) ANTES de disparar la transición a la escena Result.

**Condición y momento de victoria**
- D-04: La victoria se evalúa al llegar a Home manejando (en el trigger `CityHomeDoorTrigger` desde City), cuando se cumplen ambas condiciones.
- D-05: Nivel mínimo de alcohol bajo (~6 de `MaxLevel`=24), campo serializado configurable en el GameManager.
- D-06: "Vender todos los objetos" = total de `CarryableObject` de Home vs. `DeliveredObjectsStore` count. El total se captura una sola vez (primera carga de Home / inicio de partida).

**Pantallas de resultado y acciones**
- D-07: Escena dedicada `Result` cargada con `LoadSceneAsync` (Single). Lee un flag estático `GameResultStore` (Victory | Defeat) para mostrar texto/imagen distinto.
- D-08: Cada pantalla ofrece Reintentar + Salir. Salir = `Application.Quit()`.
- D-09: Reintentar = reinicio completo: limpiar todos los stores y cargar Home desde cero vía `NewGame()` (ver D-13).
- D-10: La escena Result muestra el cursor (`Cursor.visible = true`, `lockState = None`) y asegura `Time.timeScale = 1f`. NO se usa `Time.timeScale = 0`.

**Arquitectura del estado de juego**
- D-11: `GameManager` central (per-escena, en City) como punto de verdad: chequea la victoria al llegar a Home, recibe señal de choque desde el auto, dispara transición a `Result`.
- D-12: `GameResultStore` estático (Victory | Defeat) comunica el resultado a la escena `Result`.
- D-13: `NewGame()` (en GameManager o helper estático) llama `Clear()` de cada store (`CarStateStore`, `DeliveredObjectsStore`, `DrunkLevelStore`, `PlayerMoneyStore`, `HeldObjectStore`), resetea `PlayerSpawner.NextSpawnId = null`, limpia `GameResultStore`, y carga Home.

### Claude's Discretion
- Diseño visual exacto de las pantallas (colores, tipografía, mensajes), con Victory y Defeat claramente distinguibles.
- Si el GameManager es per-escena (en City) o singleton DontDestroyOnLoad — la decisión preliminar es per-escena en City.
- Mecanismo exacto de captura del total de objetos (en el store vs. en el GameManager al primer load de Home) y comunicación auto→GameManager del choque (evento estático, FindFirstObjectByType, o suscripción).
- Si los botones usan UI legacy (Button + onClick) o handlers por código; mantener TMP donde haya texto.

### Deferred Ideas (OUT OF SCOPE)
- Partículas de choque / feedback visual extra: Phase 5 (FX-01).
- Animaciones: Phase 4.
- Escena de loading asíncrona dedicada: Phase 5 (SCENE-01).
- Balance económico y modelo de embriaguez: solo se leen, no se modifican.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GAME-01 | El jugador pierde al chocar contra casa, árbol, niño o mascota mientras maneja | D-01/D-02/D-03: `OnCollisionEnter` + `LethalObstacle` + freno + transición a Result |
| GAME-02 | El jugador gana cuando vendió todos los objetos habiendo alcanzado nivel mínimo de borrachera | D-04/D-05/D-06: evaluación en `CityHomeDoorTrigger`, `DeliveredObjectsStore.TakenCount` vs total capturado, `DrunkLevelStore.AlcoholLevel >= minAlcohol` |
| GAME-03 | Se muestra pantalla de victoria al cumplir la condición de ganar | D-07/D-08/D-10: escena `Result` con `GameResultStore == Victory` |
| GAME-04 | Se muestra pantalla de derrota al chocar | D-07/D-08/D-10: escena `Result` con `GameResultStore == Defeat` |
</phase_requirements>

---

## Summary

Esta fase cierra el loop core del juego en dos caminos: derrota por colisión y victoria por entrega completa de objetos + borrachera mínima. El stack técnico ya existe casi en su totalidad: `CarController` tiene Rigidbody dinámico (no kinematic) con `CollisionDetection: Continuous Dynamic` y un `BoxCollider` no-trigger, lo que hace que `OnCollisionEnter` funcione de forma nativa. Los edificios y árboles de la City tienen `MeshColliders` físicos (no-trigger, confirmado en `City.unity`). Niño y mascota NO existen aún: el `CityBuilder` solo genera edificios (`building-type-X`) y árboles (`tree-large` / `tree-small`); los modelos humanos sí están disponibles en `CharacterKit` (18 FBX `character-a` a `character-r`), pero no hay modelos de animales en ninguno de los kits disponibles.

La condición de victoria se puede implementar limpiamente: `DeliveredObjectsStore` ya tiene todos los ids de objetos tomados; solo hay que agregar `TakenCount` (propiedad `takenIds.Count`) y un store adicional para el total capturado en Home (`HomeObjectsTotalStore`). El alcohol entre escenas ya está resuelto desde Phase 2 via `DrunkLevelStore`. Los 6 objetos `CarryableObject` en Home tienen `objectId` explícitos (`home-obj-1` a `home-obj-6`), lo que elimina el riesgo de colisión de `StableId`.

El patrón de escena dedicada `Result` replica exactamente el patrón de los triggers existentes (`LoadSceneAsync` en Single mode). La escena debe agregarse al `EditorBuildSettings.asset` manualmente (o desde File → Build Settings). El HUD (`HUDController` con `DontDestroyOnLoad`) persistirá en la escena `Result` y debe ocultarse; requiere agregar un método público `SetVisible(bool)` al `HUDController`.

**Recomendación primaria:** Implementar en tres ondas: (1) los stores nuevos + componentes base (`LethalObstacle`, `GameResultStore`, `TotalObjectsStore`), (2) `GameManager` en City + `OnCollisionEnter` en `CarController` + modificación de `CityHomeDoorTrigger`, (3) escena `Result` + `ResultScreenController` + `NewGame()`.

---

## Architectural Responsibility Map

| Capability | Tier Primario | Tier Secundario | Rationale |
|------------|---------------|-----------------|-----------|
| Detección de colisión letal | Vehículo (CarController) | — | El Rigidbody dinámico del auto es donde se produce la colisión física |
| Autoridad del resultado (win/lose) | GameManager (escena City) | — | Centraliza la lógica para evitar código distribuido en múltiples triggers |
| Comunicación de resultado entre escenas | GameResultStore (estático) | — | Mismo patrón que todos los stores existentes del proyecto |
| Evaluación de victoria | GameManager (hook en trigger) | CityHomeDoorTrigger | GameManager interpone su lógica antes de que el trigger cargue Home |
| Conteo de objetos entregados | DeliveredObjectsStore (estático) | HomeObjectsTotalStore (nuevo) | El store ya rastrea qué se tomó; solo falta count vs total |
| Pantalla de resultado | ResultScreenController (escena Result) | — | Escena dedicada; aislada del gameplay |
| Reset de partida | NewGame() (helper estático) | ResultScreenController (invoca) | Reset centralizado igual al que CONCERNS.md anticipaba |
| Ocultar HUD en pantalla de resultado | HUDController (DontDestroyOnLoad) | ResultScreenController (señaliza) | HUDController persiste; necesita método SetVisible |

---

## Open Items verificados (según CONTEXT.md)

### 1. Entidades "niño" y "mascota" en City — VERIFICADO

**Estado actual:**
- `CityBuilder.cs` genera: edificios (`building-type-a/b/c/d/e/f/g/h/i` en BuildBuildings) y árboles (`tree-large` / `tree-small` en BuildVegetation). **No genera personajes ni animales.**
- La escena `City.unity` NO tiene instancias de personajes ni animales.

**Assets disponibles:**
- **CharacterKit** (`Assets/ThirdParty/Kenney/CharacterKit/Models/FBX format/`): 18 modelos FBX genéricos (`character-a` a `character-r`). Sirven para representar "niño" (cualquier variante baja) o peatón genérico.
- **NatureKit** (`Assets/ThirdParty/Kenney/NatureKit/`): Solo vegetación, piedras, senderos. **No hay animales.**
- **CityKit**, **CarKit**, **FurnitureKit**, **RoadKit**: Sin modelos de animales.

**Conclusión:** Para el requerimiento de los 4 tipos (casa, árbol, niño, mascota):
- Casa y árbol: ya tienen physics colliders en City (confirmado en City.unity) — solo agregar `LethalObstacle`.
- Niño: usar `character-X.fbx` del CharacterKit como peatón; colocar 1-2 instancias en la calle + agregar BoxCollider + `LethalObstacle`.
- Mascota: **no existe modelo Kenney adecuado**. Opciones: (a) usar un cubo/cápsula placeholder con `LethalObstacle` y escala pequeña, (b) reutilizar un `character-X.fbx` a escala reducida, (c) definir "mascota" como out-of-scope de esta fase y dejarlo como placeholder. El plan debe tomar esta decisión. [ASSUMED: la opción más práctica para MVP es placeholder geométrico; el diseño visual queda a discreción de Claude]

### 2. Escena Result en Build Settings — VERIFICADO

**Formato actual de `EditorBuildSettings.asset`:**
```yaml
m_Scenes:
- enabled: 1
  path: Assets/Scenes/Home.unity
  guid: 5ed03f75adcdccc489274cd68c9b06a2
- enabled: 1
  path: Assets/Scenes/City.unity
  guid: 643a2c5c0508be4489ee5659d8be532c
- enabled: 1
  path: Assets/Scenes/Bar.unity
  guid: 99c9720ab356a0642a771bea13969a05
```

**Pasos para agregar Result:**
1. Crear `Assets/Scenes/Result.unity` desde el Editor (File → New Scene → Save As).
2. Unity genera automáticamente `Assets/Scenes/Result.unity.meta` con un GUID nuevo.
3. Agregar la escena desde **File → Build Settings → Add Open Scenes** (con Result abierta), O editar `EditorBuildSettings.asset` a mano copiando el bloque con el GUID del `.meta`.
4. El índice de escena no importa para `LoadSceneAsync(string)` — se carga por nombre.

**NOTA IMPORTANTE:** `EditorBuildSettings.asset` **no debe editarse a mano** en este proyecto — hacerlo con el Editor abierto puede causar conflictos. El plan debe indicar abrir `File → Build Settings` desde el Editor.

### 3. DeliveredObjectsStore count vs total (D-06) — VERIFICADO

**Estado actual de `DeliveredObjectsStore`:**
```csharp
static readonly HashSet<string> takenIds = new HashSet<string>();
public static void MarkTaken(string id)   // ya existe
public static bool IsTaken(string id)     // ya existe
public static void Clear()                // ya existe
// FALTA: public static int TakenCount => takenIds.Count;
```

**CarryableObjects en Home.unity:** 6 instancias con IDs explícitos: `home-obj-1` a `home-obj-6`.

**Solución para D-06:** Dos cambios mínimos:
1. Agregar `public static int TakenCount => takenIds.Count;` en `DeliveredObjectsStore`.
2. Crear `HomeObjectsTotalStore` (estático, igual patrón a los demás stores) con `int Total` y `void Set(int total)` y `void Clear()`. El GameManager (o un inicializador en Home) captura el total al primer load de Home via `FindObjectsByType<CarryableObject>().Length` y llama `HomeObjectsTotalStore.Set(count)`.

**Punto de captura del total:** El GameManager puede capturarlo en un `SceneManager.sceneLoaded` handler cuando la escena es "Home" y `HomeObjectsTotalStore.Total == 0` (primera carga). Alternativa: un `HomeInitializer` MonoBehaviour en la escena Home que lo setea en `Awake`. La segunda opción es más simple y sigue el patrón de la escena, pero el GameManager debe vivir en City para el choque (D-11), así que necesita información de Home. **Recomendación:** `HomeInitializer` en Home scene setea el total en `Awake`; el GameManager lo lee desde el store al evaluar en `CityHomeDoorTrigger`.

**Condición de victoria completa:**
```csharp
bool allDelivered = DeliveredObjectsStore.TakenCount >= HomeObjectsTotalStore.Total 
                    && HomeObjectsTotalStore.Total > 0;
bool drunkEnough  = DrunkLevelStore.AlcoholLevel >= minAlcoholRequired;
if (allDelivered && drunkEnough) // → Victory
```

### 4. CarController — colisión — VERIFICADO

**Confirmado leyendo `CarController.cs` y `Car_Sedan.prefab`:**
- `[RequireComponent(typeof(Rigidbody))]` — Rigidbody confirmado.
- `public bool IsControlled => isControlled;` — propiedad pública confirmada.
- `public void SetControlled(bool value)` — método público confirmado.
- Rigidbody: `m_IsKinematic: 0` (dinámico), `m_CollisionDetection: 2` (Continuous Dynamic).
- Colliders en Car_Sedan: 6 `MeshColliders` (partes de la carrocería/ruedas, `m_IsTrigger: 0`) + 1 `BoxCollider` (`1.2 × 1.3 × 2.5`, `m_IsTrigger: 0`) en el mismo GameObject que el Rigidbody.
- **`OnCollisionEnter` puede agregarse directamente en `CarController.cs`** — funciona con el setup existente.
- **Los edificios y árboles en City.unity tienen MeshColliders físicos (no-trigger)** — el auto ya colisiona físicamente con ellos hoy.

**ADVERTENCIA:** Los MeshColliders del auto tienen `m_Convex: 0` (no-convex). En Unity, los Rigidbody dinámicos con MeshColliders no-convex pueden generar errores `"Non-convex MeshCollider with non-kinematic Rigidbody"`. Sin embargo, el BoxCollider es el que participa en la física de colisión principal. Verificar en consola al probar que no haya errores. Si los hay, los MeshColliders de ruedas/decoración deben marcarse `isTrigger = true` o convertirse a convexos.

**Código patrón para OnCollisionEnter en CarController:**
```csharp
// [CITED: CONVENTIONS.md — patrón de guard clause + FindFirstObjectByType]
private void OnCollisionEnter(Collision collision)
{
    if (!isControlled) return;
    LethalObstacle obstacle = collision.gameObject.GetComponent<LethalObstacle>();
    if (obstacle == null) return;
    
    // Frenar (D-03)
    rb.linearVelocity    = Vector3.zero;
    rb.angularVelocity   = Vector3.zero;
    SetControlled(false);
    
    // Notificar al GameManager
    var gm = FindFirstObjectByType<GameManager>();
    if (gm != null) gm.OnCarCrash();
}
```

### 5. Triggers de llegada a Home (D-04) — VERIFICADO

**`CityHomeDoorTrigger.cs` actual:**
```csharp
void OnTriggerEnter(Collider other)
{
    if (triggered) return;
    if (!other.CompareTag(playerTag)) return;
    triggered = true;
    var car = FindFirstObjectByType<CarController>();
    if (car != null) CarStateStore.Save(car.transform);
    SceneManager.LoadSceneAsync(sceneToLoad);   // ← interceptar aquí
}
```

**Para D-04:** El trigger debe preguntar al GameManager si la condición de victoria se cumple ANTES de cargar Home. Si se cumple, cargar `Result` en lugar de `Home`. Alternativa: el GameManager intercepta via evento o modificando el trigger para que primero consulte.

**Patrón recomendado (mínima invasión):** Modificar `CityHomeDoorTrigger.OnTriggerEnter` para buscar el GameManager y delegar:
```csharp
void OnTriggerEnter(Collider other)
{
    if (triggered) return;
    if (!other.CompareTag(playerTag)) return;
    triggered = true;
    
    var car = FindFirstObjectByType<CarController>();
    if (car != null) CarStateStore.Save(car.transform);
    
    // D-04: delegar al GameManager la decisión de escena
    var gm = FindFirstObjectByType<GameManager>();
    if (gm != null) { gm.OnPlayerArrivedHome(); return; }
    
    // Fallback: comportamiento anterior si no hay GameManager
    SceneManager.LoadSceneAsync(sceneToLoad);
}
```

---

## Architecture Patterns

### Diagrama de flujo de datos

```
[CarController: OnCollisionEnter]
        │ IsControlled == true && LethalObstacle found
        ▼
  Frenar Rigidbody
  SetControlled(false)
        │
        ▼
[GameManager.OnCarCrash()]
        │
        ▼
  GameResultStore.Set(Defeat)
        │
        ▼
  SceneManager.LoadSceneAsync("Result")
        │
        ▼
[ResultScreenController.Start()]
        │ GameResultStore == Defeat
        ▼
  Mostrar pantalla Derrota
  Cursor.visible = true
  HUDController.SetVisible(false)
  Botón Reintentar → NewGame()
  Botón Salir → Application.Quit()


[CityHomeDoorTrigger: OnTriggerEnter]
        │ player tag
        ▼
  CarStateStore.Save(car)
  GameManager.OnPlayerArrivedHome()
        │
        ▼
  allDelivered = TakenCount >= Total && Total > 0
  drunkEnough  = DrunkLevelStore >= minAlcohol
        │
   ┌────┴────┐
   │ Sí gana │ No gana
   ▼         ▼
GameResultStore   SceneManager.Load
.Set(Victory)     AsyncAsync("Home")
LoadAsync("Result")


[HomeInitializer (Home scene)]
        │ Awake
        ▼
  if HomeObjectsTotalStore.Total == 0:
    HomeObjectsTotalStore.Set(
      FindObjectsByType<CarryableObject>().Length)


[ResultScreenController: botón Reintentar]
        │
        ▼
  NewGame()   ← estático
  ├── CarStateStore.Clear()
  ├── DeliveredObjectsStore.Clear()
  ├── DrunkLevelStore.Clear()
  ├── PlayerMoneyStore.Clear()
  ├── HeldObjectStore.Clear()
  ├── HomeObjectsTotalStore.Clear()
  ├── GameResultStore.Clear()
  └── PlayerSpawner.NextSpawnId = null
  SceneManager.LoadSceneAsync("Home")
```

### Estructura de archivos nuevos

```
Assets/_Project/
├── Core/
│   ├── Managers/
│   │   └── GameManager.cs              — evaluación victoria/derrota, NewGame()
│   └── SceneManagement/
│       ├── GameResultStore.cs          — enum Victory/Defeat, flag estático
│       └── HomeObjectsTotalStore.cs    — int Total, Set(int), Clear()
├── Gameplay/
│   └── Vehicles/
│       └── LethalObstacle.cs           — componente marcador con enum ObstacleCategory
├── Scenes/
│   └── Result/
│       └── ResultScreenController.cs   — MonoBehaviour en la escena Result
Assets/Scenes/
└── Result.unity                        — nueva escena (crear desde Editor)
```

Modificaciones a archivos existentes:
- `CarController.cs` — agregar `OnCollisionEnter`
- `CityHomeDoorTrigger.cs` — delegar a GameManager
- `HUDController.cs` — agregar `public static void SetVisible(bool)`
- `DeliveredObjectsStore.cs` — agregar `public static int TakenCount => takenIds.Count`
- `CityBuilder.cs` — agregar `LethalObstacle` a edificios y árboles generados

---

## Don't Hand-Roll

| Problema | No construir | Usar en cambio | Por qué |
|----------|-------------|----------------|---------|
| Detección de obstáculos letales | Tags/layers hardcodeados o `gameObject.name.Contains()` | Componente marcador `LethalObstacle` | Desacopla totalmente; permite datos futuros (audio de impacto, puntuación) |
| Reset de partida | Lógica distribuida en cada escena | `NewGame()` centralized helper | CONCERNS.md ya advierte el riesgo de stores no reseteados |
| UI de resultado | uGUI custom con Canvas manual como HUDController | MonoBehaviour simple en escena dedicada + Canvas en escena | La escena Result se descarga al volver; no necesita persistir |
| Comunicación resultado entre escenas | `DontDestroyOnLoad` MonoBehaviour | `GameResultStore` estático | Patrón establecido del proyecto; más simple y predecible |
| Freno de emergencia | `rb.drag = 999f` | `rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;` | Patrón ya usado en `CarController.Start()` para restaurar estado guardado |

---

## Common Pitfalls

### Pitfall 1: MeshColliders no-convex en Rigidbody dinámico
**Qué falla:** Unity lanza `"Non-convex MeshCollider with non-kinematic Rigidbody is no longer supported"` si un Rigidbody dinámico tiene MeshColliders no-convex activos. El auto ya tiene 6 MeshColliders con `m_Convex: 0`.
**Por qué pasa:** Unity 2022+ deshabilitó silenciosamente los MeshColliders no-convex en Rigidbodys dinámicos. El BoxCollider es el que maneja la física real; los MeshColliders son de decoración/ruedas.
**Cómo evitar:** Si aparece el error, marcar los MeshColliders de ruedas como `isTrigger = true` o habilitar `m_Convex: 1` en el Inspector. El BoxCollider (1.2×1.3×2.5) es el que disparará `OnCollisionEnter`.
**Señal de alerta:** Error en consola al entrar en modo Play o al construir.

### Pitfall 2: `OnCollisionEnter` no dispara porque el obstáculo no tiene Collider
**Qué falla:** Si el niño/mascota se colocan sin un Collider propio, `OnCollisionEnter` nunca se llama — el auto los atraviesa.
**Por qué pasa:** Los modelos FBX de Kenney CharacterKit tienen `addColliders: 0` en sus `.meta` (igual que CityKit). Al colocar un `character-X.fbx` en la escena no se genera ningún Collider.
**Cómo evitar:** Agregar manualmente un `BoxCollider` (o `CapsuleCollider`) a cada instancia de niño/mascota en la escena. El `LethalObstacle` debe agregarse al mismo GameObject raíz o al GameObject que tiene el Collider.

### Pitfall 3: `triggered = true` en `CityHomeDoorTrigger` bloquea re-evaluación
**Qué falla:** `CityHomeDoorTrigger` tiene `private bool triggered` como guard. Si la evaluación de victoria falla (GameManager.OnPlayerArrivedHome devuelve false y no carga escena), el trigger queda bloqueado y el jugador no puede volver a intentar cruzarlo.
**Por qué pasa:** El guard se setea en la primera línea del OnTriggerEnter.
**Cómo evitar:** Setear `triggered = true` solo cuando se confirma que se va a cargar una escena, no al inicio del handler. O resetear `triggered = false` si el GameManager no activa ninguna transición.

### Pitfall 4: HUD visible encima de la pantalla de resultado
**Qué falla:** `HUDController` es `DontDestroyOnLoad` y persiste en la escena `Result`. Su Canvas (sortingOrder 100) se dibuja encima de la UI de resultado.
**Por qué pasa:** El patrón de singleton persistente funciona para toda la vida del juego; no tiene lógica de ocultarse.
**Cómo evitar:** `ResultScreenController.Start()` debe llamar `HUDController.SetVisible(false)`. Al salir/reiniciar, volver a `SetVisible(true)` (o simplemente cargar una escena nueva donde HUD se re-muestra por `sceneLoaded`). Agregar `public static void SetVisible(bool v)` a `HUDController`.

### Pitfall 5: `HomeObjectsTotalStore.Total` queda en 0 si Home no se visita antes de la victoria
**Qué falla:** Si el total de objetos nunca se capturó (primer viaje en la partida directamente a City sin pasar por Home), `Total == 0` y `allDelivered` sería `true && false` = false, pero también puede evaluarse mal.
**Por qué pasa:** El total se captura en Home; si el jugador carga City primero (p. ej. debug), el store está vacío.
**Cómo evitar:** En la condición de victoria usar `HomeObjectsTotalStore.Total > 0` como guard explícito. Si `Total == 0`, no se puede ganar (partida no inicializada).

### Pitfall 6: `NewGame()` llamado desde la escena Result mientras HUDController y BackgroundMusicManager siguen vivos
**Qué falla:** Al llamar `NewGame()` y cargar Home, los singletons persisten y se intenta re-vincular al DrunkManager de la nueva escena (vía `sceneLoaded` en HUDController). Esto es correcto y ya está implementado en `HUDController.OnSceneLoaded`. No requiere acción extra.
**Señal de alerta:** Si `HUDController.OnSceneLoaded` deja de estar suscripto (p. ej. si se destruye el objeto), la barra de borrachera dejará de actualizarse en la nueva partida.

---

## Code Examples

### LethalObstacle (componente marcador)
```csharp
// [CITED: CONVENTIONS.md — patrón enum en mismo archivo, namespace global]
public enum ObstacleCategory { Casa, Arbol, Nino, Mascota }

public class LethalObstacle : MonoBehaviour
{
    [SerializeField] private ObstacleCategory category = ObstacleCategory.Casa;
    public ObstacleCategory Category => category;
}
```

### GameResultStore (store estático, patrón proyecto)
```csharp
// [CITED: CONVENTIONS.md — patrón Static Store]
public enum GameResult { None, Victory, Defeat }

public static class GameResultStore
{
    public static GameResult Result { get; private set; } = GameResult.None;
    public static void Set(GameResult result) { Result = result; }
    public static void Clear() { Result = GameResult.None; }
}
```

### HomeObjectsTotalStore (store estático nuevo)
```csharp
// [CITED: CONVENTIONS.md — patrón Static Store]
public static class HomeObjectsTotalStore
{
    public static int Total { get; private set; } = 0;
    public static void Set(int total) { if (total >= 0) Total = total; }
    public static void Clear() { Total = 0; }
}
```

### DeliveredObjectsStore — propiedad TakenCount (adición mínima)
```csharp
// Agregar a DeliveredObjectsStore.cs existente:
public static int TakenCount => takenIds.Count;
```

### GameManager — evaluación de victoria
```csharp
// [CITED: CONVENTIONS.md — patrón Manager con FindFirstObjectByType]
public class GameManager : MonoBehaviour
{
    [Header("Condición de victoria")]
    [Tooltip("Nivel mínimo de alcohol requerido para ganar (D-05)")]
    [SerializeField] private int minAlcoholRequired = 6;

    // Llamado desde CityHomeDoorTrigger cuando el jugador llega a Home manejando (D-04)
    public void OnPlayerArrivedHome()
    {
        bool allDelivered = HomeObjectsTotalStore.Total > 0 
                         && DeliveredObjectsStore.TakenCount >= HomeObjectsTotalStore.Total;
        bool drunkEnough  = DrunkLevelStore.AlcoholLevel >= minAlcoholRequired;

        if (allDelivered && drunkEnough)
        {
            GameResultStore.Set(GameResult.Victory);
            SceneManager.LoadSceneAsync("Result");
        }
        else
        {
            SceneManager.LoadSceneAsync("Home");
        }
    }

    // Llamado desde CarController.OnCollisionEnter (D-01/D-02/D-03)
    public void OnCarCrash()
    {
        GameResultStore.Set(GameResult.Defeat);
        SceneManager.LoadSceneAsync("Result");
    }
}
```

### NewGame() — reset centralizado (D-13)
```csharp
// [CITED: CONCERNS.md — fix de "No New Game reset"]
public static void NewGame()
{
    CarStateStore.Clear();
    DeliveredObjectsStore.Clear();
    DrunkLevelStore.Clear();
    PlayerMoneyStore.Clear();
    HeldObjectStore.Clear();
    HomeObjectsTotalStore.Clear();
    GameResultStore.Clear();
    PlayerSpawner.NextSpawnId = null;
    SceneManager.LoadSceneAsync("Home");
}
```

### ResultScreenController
```csharp
// [CITED: CONVENTIONS.md — UI con TMP, namespace global, comentarios en español]
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultScreenController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Mostrar cursor (D-10)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

        // Ocultar HUD persistente
        var hud = FindFirstObjectByType<HUDController>();
        if (hud != null) hud.gameObject.SetActive(false);

        bool isVictory = GameResultStore.Result == GameResult.Victory;
        titleText.text    = isVictory ? "¡Llegaste a casa!" : "Chocaste";
        subtitleText.text = isVictory ? "Bien hecho... creo." : "La calle ganó esta vez.";

        retryButton.onClick.AddListener(OnRetry);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnRetry() => NewGame.Start();   // o GameManager.NewGame() estático
    private void OnQuit()  => Application.Quit();
}
```

### CityBuilder — agregar LethalObstacle a edificios y árboles
```csharp
// En PlaceBuilding, al final del método, agregar:
go.AddComponent<LethalObstacle>().SetCategory(ObstacleCategory.Casa);

// En PlaceTree, agregar:
go.AddComponent<LethalObstacle>().SetCategory(ObstacleCategory.Arbol);
// Nota: requiere agregar SetCategory(ObstacleCategory) o usar [SerializeField] con default

// Alternativa sin SetCategory (el enum default es el primero):
// LethalObstacle la = go.AddComponent<LethalObstacle>();
// la.category = ObstacleCategory.Casa;  // si el campo es public/internal
```

### HUDController — método SetVisible (adición mínima)
```csharp
// Agregar a HUDController.cs:
public static void SetVisible(bool visible)
{
    if (instance != null) instance.gameObject.SetActive(visible);
}
```

---

## Standard Stack

Esta fase no instala paquetes externos. Usa exclusivamente APIs internas de Unity ya disponibles:

| API | Versión Unity | Propósito | Disponible |
|-----|--------------|-----------|------------|
| `MonoBehaviour.OnCollisionEnter(Collision)` | Unity 6000.3.11f1 | Detección de colisión física en el auto | Confirmado — Rigidbody dinámico + BoxCollider no-trigger |
| `SceneManager.LoadSceneAsync(string)` | Unity 6000.3.11f1 | Transición a escena Result en modo Single | Patrón ya usado en 5 triggers existentes |
| `Cursor.visible`, `Cursor.lockState` | Unity 6000.3.11f1 | Gestión del cursor en pantalla de resultado | Cursor ya se bloquea en MouseLook.Awake |
| `TMPro.TextMeshProUGUI` | com.unity.ugui 2.0.0 | Texto en pantalla de resultado | Confirmado en HUDController |
| `UnityEngine.UI.Button` | com.unity.ugui 2.0.0 | Botones Reintentar / Salir | Patrón UI nativa |
| `Application.Quit()` | Unity 6000.3.11f1 | Botón Salir | No-op en Editor, funciona en build |
| `Time.timeScale` | Unity 6000.3.11f1 | Garantizar timeScale = 1 en Result | No se usa timeScale = 0 (D-10) |

**No se agregan paquetes. No hay Package Legitimacy Audit que ejecutar.**

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor | Todo | Si | 6000.3.11f1 | — |
| Kenney CharacterKit FBXs | Personaje "niño" en City | Si | 18 modelos `character-a` a `character-r` | Primitiva Capsule/Cube como placeholder |
| Modelo de mascota/animal | Obstáculo "mascota" en City | No | — | Primitiva Cube pequeña como placeholder (ver Pitfall 2) |
| `Assets/Scenes/Result.unity` | ResultScreenController | No (debe crearse) | — | Crear desde File → New Scene en el Editor |

**Missing dependencies con fallback:**
- Modelo de mascota: usar primitiva geométrica como placeholder de MVP.
- Escena Result: crear desde el Editor como primera tarea del plan.

---

## Runtime State Inventory

Esta fase agrega stores nuevos y un reset centralizado. No es una fase de renombre, pero sí introduce estado nuevo entre escenas.

| Categoría | Items | Acción requerida |
|-----------|-------|-----------------|
| Stores estáticos nuevos | `GameResultStore`, `HomeObjectsTotalStore` | Deben limpiarse en `NewGame()` — incluir en la lista de `Clear()` |
| Stores existentes sin `Clear()` caller | Todos los stores existentes (`CarStateStore`, `DeliveredObjectsStore`, `DrunkLevelStore`, `PlayerMoneyStore`, `HeldObjectStore`) | `NewGame()` es el primer caller de todos sus `Clear()` — ver CONCERNS.md |
| `PlayerSpawner.NextSpawnId` | Campo `public static string` sin `Clear()` | `NewGame()` setea `= null` directamente |
| Singletons DontDestroyOnLoad | `HUDController`, `BackgroundMusicManager` | NO se destruyen en NewGame (correcto — persisten por diseño). HUDController debe ocultarse en Result y re-mostrarse al volver |
| Build Settings | `EditorBuildSettings.asset` sin escena Result | Agregar `Result` desde File → Build Settings antes del primer Play test |

---

## Validation Architecture

`nyquist_validation: true` en `.planning/config.json`.

El proyecto no tiene tests ni assembly definitions todavía (confirmado en CLAUDE.md). No se introducen tests automáticos en esta fase — la capa de validación es manual en modo Play.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework (`com.unity.test-framework`) — instalado, sin tests aún |
| Config file | Ninguno — no hay assembly definitions |
| Quick run command | Window → General → Test Runner (sin tests automatizados hoy) |
| Full suite command | N/A |

### Phase Requirements → Validation Map

| Req ID | Comportamiento | Tipo | Comando | Archivo |
|--------|---------------|------|---------|---------|
| GAME-01 | Chocar con edificio mientras `IsControlled=true` → Result/Defeat en <2s | Manual Play | Play en City, conducir hacia edificio | ❌ Manual |
| GAME-01 | Chocar con árbol → Result/Defeat | Manual Play | Play en City, conducir hacia árbol | ❌ Manual |
| GAME-01 | Chocar con niño (placeholder) → Result/Defeat | Manual Play | Play en City, conducir hacia personaje | ❌ Manual |
| GAME-01 | Chocar a pie (IsControlled=false) → NO aparece Result | Manual Play | Caminar contra edificio | ❌ Manual |
| GAME-02 | Sin haber vendido todo + alcohol bajo → llegar a Home carga Home normalmente | Manual Play | Ir a Home sin vender | ❌ Manual |
| GAME-02 | Vendido todo + alcohol >= 6 → llegar a Home carga Result/Victory | Manual Play | Vender todo en bar → conducir a Home con alcohol | ❌ Manual |
| GAME-03 | Pantalla Victory distinguible con texto de éxito + botones | Manual Play | Trigger de victoria → inspeccionar UI | ❌ Manual |
| GAME-04 | Pantalla Defeat distinguible con texto de derrota + botones | Manual Play | Trigger de derrota → inspeccionar UI | ❌ Manual |
| D-10 | Cursor visible en Result | Manual Play | Verificar cursor liberado en Result | ❌ Manual |
| D-09 | Reintentar limpiar todos los stores | Manual Play | Reintentar → verificar que Home tiene todos los objetos y alcohol = 0 | ❌ Manual |
| Criterio 5 | En partida normal sin chocar ni vender todo, ninguna pantalla aparece | Manual Play | Jugar normalmente | ❌ Manual |

### Sampling Rate

- **Por tarea committeada:** Build en Editor + Play mode, verificar consola sin errores.
- **Por wave completa:** Play test manual del flow completo (inicio → bar → conducir borracho → choque → Result → Reintentar → inicio limpio).
- **Gate de fase:** Todos los 5 criterios de success verificados en modo Play antes de `/gsd:verify-work`.

### Wave 0 Gaps

- [ ] No hay framework de tests automatizados — all validation es manual Play mode.
- [ ] `Assets/Scenes/Result.unity` debe crearse antes de que cualquier script la referencie.

*(Nota: no se recomiendan tests automáticos en Wave 0 dado que CLAUDE.md indica que no existen assembly definitions aún. Agregar tests es deuda documentada en CONCERNS.md.)*

---

## State of the Art

| Antes | Ahora (Phase 3) | Impacto |
|-------|----------------|---------|
| No existía condición de fin de partida | GameManager centraliza victoria y derrota | Cierra el loop core del juego |
| `NewGame()` no existía — tech debt de CONCERNS.md | `NewGame()` como función centralizada | Resuelve el bug de re-play sin reiniciar el ejecutable |
| `DeliveredObjectsStore` solo expone `IsTaken` | Agrega `TakenCount` | Permite evaluar la victoria sin contar objetos activos en la escena |
| `CityHomeDoorTrigger` siempre carga Home | Ahora puede cargar Result si se cumple la condición | Redirige el flujo sin cambiar la experiencia de "volver a casa" |
| HUD no tiene Show/Hide | `HUDController.SetVisible(bool)` | Permite ocultar el HUD en pantallas de resultado |

---

## Assumptions Log

| # | Claim | Sección | Riesgo si está mal |
|---|-------|---------|-------------------|
| A1 | La mascota puede representarse con una primitiva geométrica (placeholder de MVP) dado que ningún kit de Kenney incluye animales | Open Items #1 | Bajo — es una decisión de diseño explícita de MVP; el componente `LethalObstacle` funciona igual |
| A2 | El `OnCollisionEnter` en CarController detectará los MeshColliders de los edificios existentes en City.unity sin necesidad de agregar colliders extra a los edificios | Open Items #4 | Bajo — confirmado: los edificios tienen MeshColliders no-trigger en City.unity |
| A3 | Un breve delay de ~0.5s entre el freno del auto y `LoadSceneAsync` mejoraría la UX pero no es requerido por las decisions — la transición puede ser inmediata | Architecture Patterns | Bajo — es una elección de diseño visual; funciona igual sin delay |
| A4 | `HomeInitializer` (MonoBehaviour en Home scene) es el mejor punto para capturar el total de CarryableObjects | Open Items #3 | Bajo — alternativa válida: capturarlo en GameManager via `sceneLoaded`; ambas funcionan |

---

## Open Questions (RESOLVED)

1. **¿El GameManager debe estar en City o debe ser DontDestroyOnLoad?**
   - Lo que sabemos: D-11 dice "per-escena (como DrunkManager)"; el choque ocurre en City y la evaluación de victoria también es desde City. Sin embargo, si GameManager vive en City, no puede recibir el total de objetos de Home directamente (debe venir via `HomeObjectsTotalStore`).
   - La brecha: si el jugador va directo a City sin pasar por Home, `HomeObjectsTotalStore.Total` será 0 y la victoria es imposible. Esto está bien para MVP (el loop correcto es Home → City → Bar → City → Home).
   - Recomendación: GameManager per-escena en City con el guard `Total > 0`.

2. **¿Delay antes de cargar Result después del choque?**
   - Lo que sabemos: D-03 especifica freno + SetControlled(false) antes de la transición; no menciona delay.
   - La brecha: una transición inmediata puede sentirse abrupta.
   - Recomendación: sin delay para MVP (los criterios de éxito no lo requieren). Si se quiere feedback, Phase 5 (FX-01) es el lugar correcto.

3. **¿Cómo se distinguen visualmente Victory y Defeat?**
   - Lo que sabemos: debe ser "claramente distinguible" (criterios 2 y 4). El diseño visual es "Claude's Discretion".
   - Recomendación: colores de fondo distintos (verde claro para Victory, rojo oscuro para Defeat) + iconos simples de texto ASCII o unicode. TMP soporta colores inline. Sin assets externos.

---

## Sources

### Primary (HIGH confidence)

- Código fuente leído directamente:
  - `Assets/_Project/Gameplay/Vehicles/CarController.cs` — `IsControlled`, `SetControlled`, Rigidbody, `linearVelocity/angularVelocity`
  - `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` — API completa (`MarkTaken`, `IsTaken`, `Clear`; falta `TakenCount`)
  - `Assets/_Project/Gameplay/Items/CarryableObject.cs` — `StableId`, `OnPickedUp`, `objectId`
  - `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` — patrón trigger
  - `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` — patrón trigger + `LoadSceneAsync`
  - `Assets/_Project/UI/HUD/HUDController.cs` — patrón singleton persistente, UI por código, TMP
  - `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` — patrón `RuntimeInitializeOnLoadMethod`
  - `Assets/_Project/Core/Managers/DrunkManager.cs` — `AlcoholLevel`, `MaxLevel` = 24
  - `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` — `AlcoholLevel`, `Save`, `Clear`
  - `Assets/_Project/Editor/CityBuilder.cs` — qué genera hoy (edificios + árboles, sin personajes/animales)
  - `ProjectSettings/EditorBuildSettings.asset` — formato exacto, 3 escenas actuales (Home, City, Bar)
  - `ProjectSettings/TagManager.asset` — tags existentes: solo `"player"`
- `Assets/_Project/Prefabs/Vehicles/Car_Sedan.prefab` — Rigidbody dinámico confirmado, BoxCollider no-trigger, MeshColliders no-convex
- `Assets/Scenes/City.unity` — MeshColliders de edificios confirmados (no-trigger, mesh refs a building-type-a GUID)
- `Assets/Scenes/Home.unity` — 6 CarryableObjects con objectIds explícitos `home-obj-1` a `home-obj-6`
- `.planning/codebase/CONVENTIONS.md` — namespace global, [SerializeField] private, guard clauses, comentarios en español
- `.planning/codebase/CONCERNS.md` — tech debt de NewGame reset, cursor lock en MouseLook

### Secondary (MEDIUM confidence)

- Kenney CharacterKit — 18 FBX de personajes genéricos confirmados en filesystem; ninguno es específicamente "niño" (son todos proporciones adultas en el kit estándar de Kenney) [ASSUMED: se puede usar cualquiera como placeholder de niño sin diferencia de gameplay]

### Tertiary (LOW confidence)

- Comportamiento de MeshColliders no-convex en Rigidbody dinámico en Unity 6000.3.11f1: marcado como pitfall basado en conocimiento de entrenamiento. No verificado contra Unity 6 release notes. [ASSUMED: puede que Unity 6 los soporte con advertencia en lugar de error]

---

## Metadata

**Confidence breakdown:**
- Stores y API del proyecto: HIGH — leídos directamente del código fuente
- Colliders del auto y edificios en City: HIGH — inspeccionado en YAML de prefabs y scene
- Escena Result (creación e integración): HIGH — el patrón de `LoadSceneAsync` está replicado 5 veces en el proyecto
- Modelos de mascota: HIGH (no existen en ningún kit disponible)
- Comportamiento de MeshColliders no-convex: MEDIUM (documentado en Unity, no verificado en esta versión específica)

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (estable — no hay dependencias en evolución rápida)
