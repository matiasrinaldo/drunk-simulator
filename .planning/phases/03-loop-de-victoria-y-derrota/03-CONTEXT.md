# Phase 3: Loop de victoria y derrota - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Cerrar el loop core del juego con sus dos finales:

1. **Derrota (GAME-01, GAME-04):** chocar contra un obstáculo letal (casa / árbol / niño / mascota) mientras se maneja el auto en City detiene el auto y lleva a una pantalla de derrota.
2. **Victoria (GAME-02, GAME-03):** haber vendido todos los objetos de Home **y** tener el nivel de alcohol mínimo requerido, evaluado al volver a Home manejando, lleva a una pantalla de victoria.

Ambos resultados se muestran en una **escena dedicada de resultado** distinguible, con acciones para reintentar o salir. En una partida normal (sin chocar ni completar la condición de victoria) no aparece ninguna pantalla de resultado (criterio 5).

**Fuera de scope:** partículas/efectos de choque (Phase 5), HUD in-level (Phase 2, ya hecho), animaciones (Phase 4), escena de loading asíncrona dedicada (SCENE-01, Phase 5). El balance económico y el modelo de embriaguez no se tocan; esta fase solo los **lee** para decidir el resultado.

</domain>

<decisions>
## Implementation Decisions

### Detección de derrota (choque)
- **D-01:** Los obstáculos letales se marcan con un **componente marcador `LethalObstacle`** (MonoBehaviour, idealmente con un enum de categoría casa/árbol/niño/mascota para futuro). La detección es por `OnCollisionEnter` en el auto → `GetComponent<LethalObstacle>()`. Desacopla de tags/layers y deja lugar a data futura. El `CityBuilder` debe agregar el componente a los obstáculos que genera.
- **D-02:** **Cualquier contacto** con un obstáculo letal mientras el auto está siendo controlado (`CarController.IsControlled == true`) cuenta como derrota. **Sin umbral de velocidad**. Esto respeta el criterio 5: la pantalla solo aparece al chocar, nunca en una partida normal.
- **D-03:** Al confirmarse la derrota, el auto **se frena** (Rigidbody `linearVelocity`/`angularVelocity` → 0) y se **bloquea el control** (`CarController.SetControlled(false)`) antes de disparar la transición a la escena de resultado.

### Condición y momento de victoria
- **D-04:** La victoria se **evalúa al llegar a Home manejando** (en el trigger de entrada a Home desde City), cuando se cumplen ambas condiciones. Refuerza el core value: "volvés a casa manejando borracho".
- **D-05:** **Nivel mínimo de alcohol bajo (~6 de `MaxLevel`=24)**, expuesto como **campo serializado configurable** desde el Inspector (en el GameManager). Borrachera leve suficiente; balanceable.
- **D-06:** "Vender todos los objetos" se determina **registrando el total de `CarryableObject` de Home y comparando contra los entregados** (`DeliveredObjectsStore` count vs total). El total se captura una sola vez (primera carga de Home / inicio de partida). NO depende de "no quedan objetos activos".

### Pantallas de resultado y acciones
- **D-07:** Las pantallas son una **escena dedicada `Result`** cargada con `LoadSceneAsync` (Single). Una sola escena que lee un **flag estático** (`GameResultStore`: Victory | Defeat) y muestra texto/imagen distinta según el caso — claramente distinguible entre victoria y derrota (criterios 2 y 4).
- **D-08:** Cada pantalla ofrece **Reintentar + Salir**. Salir = `Application.Quit()` (en el editor queda como log/no-op).
- **D-09:** **Reintentar = reinicio completo de partida**: limpiar todos los stores y cargar Home desde cero (vía `NewGame()`, ver D-13).
- **D-10:** La escena de resultado **muestra el cursor** (`Cursor.visible = true`, `lockState = None`) y asegura `Time.timeScale = 1f`. Como es escena dedicada (la gameplay se descarga al cargarla), **NO** se usa `Time.timeScale = 0` — el "stop" lo da el unload de la escena anterior.

### Arquitectura del estado de juego
- **D-11:** Se introduce un **GameManager central** (patrón Manager, estilo `DrunkManager`) como punto de verdad del estado de partida: chequea la victoria al llegar a Home, recibe la señal de choque desde el auto, y dispara la transición a la escena `Result`.
- **D-12:** **`GameResultStore` estático** (Victory | Defeat) comunica el resultado a la escena `Result` (mismo patrón de stores estáticos del proyecto).
- **D-13:** Reset central **`NewGame()`** (en el GameManager o un helper estático) llama `Clear()` de cada store —`CarStateStore`, `DeliveredObjectsStore`, `DrunkLevelStore`, `PlayerMoneyStore`, `HeldObjectStore`— resetea `PlayerSpawner.NextSpawnId` (campo sin Clear), limpia `GameResultStore`, y carga Home. El botón Reintentar lo invoca. Es la "Nueva partida" que la doc del proyecto anticipaba.

### Claude's Discretion
- Diseño visual exacto de las pantallas (colores, tipografía, mensajes), siempre que Victory y Defeat sean **claramente distinguibles** entre sí y de cualquier otra pantalla.
- Si el GameManager es per-escena (como `DrunkManager`) o singleton `DontDestroyOnLoad` — según dónde necesite vivir (probablemente instancia en City para el choque + hook en el trigger de Home).
- Mecanismo exacto de captura del total de objetos (en el store vs. GameManager al primer load de Home) y de comunicación auto→GameManager del choque (evento estático, `FindFirstObjectByType`, o suscripción).
- Si el botón usa UI legacy (Button + onClick) o handlers por código; mantener TMP donde haya texto, consistente con el HUD.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap / requisitos
- `.planning/ROADMAP.md` §"Phase 3: Loop de victoria y derrota" — goal, success criteria (5), requisitos GAME-01..04, `Mode: mvp`, `UI hint: yes`.
- `.planning/REQUIREMENTS.md` — GAME-01 (derrota por choque), GAME-02 (victoria = vender todo + alcohol mínimo), GAME-03 (pantalla de victoria), GAME-04 (pantalla de derrota).

### Derrota / colisión (fuentes de verdad)
- `Assets/_Project/Gameplay/Vehicles/CarController.cs` — `IsControlled`, `SetControlled(bool)`, `CurrentSpeed`, `Rigidbody`. Acá se agrega `OnCollisionEnter` + el frenado (D-01, D-02, D-03).
- `Assets/_Project/Editor/CityBuilder.cs` — genera el layout de City con Kenney CityKit; acá se agrega `LethalObstacle` a los obstáculos generados, y si faltan, niño/mascota (ver Open Items).

### Victoria / estado de juego
- `Assets/_Project/Core/Managers/DrunkManager.cs` — `AlcoholLevel`, `MaxLevel` para el umbral mínimo (D-05).
- `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` — nivel de alcohol persistido entre escenas (ya resuelto en Phase 2).
- `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` — `IsTaken`/`MarkTaken`/`Clear`. Necesita exponer un **count** y conocer el **total** para D-06.
- `Assets/_Project/Gameplay/Items/CarryableObject.cs` — `StableId`; los `CarryableObject` de Home son el universo total de objetos vendibles.
- `Assets/_Project/Gameplay/Items/SellCounter.cs` — venta (los objetos se marcan como entregados al agarrarlos, no al vender).

### Triggers de llegada a Home (dónde hookear la victoria — D-04)
- `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs`
- `Assets/_Project/Core/SceneManagement/HomeDoorTrigger.cs`
- `Assets/_Project/Core/SceneManagement/BarExitTrigger.cs` / `BarDoorTrigger.cs` — patrón de trigger + `LoadSceneAsync` a replicar para la escena Result.

### Reset de partida (D-13)
- `Assets/_Project/Core/SceneManagement/CarStateStore.cs`, `PlayerMoneyStore.cs`, `HeldObjectStore.cs` — todos ya tienen `Clear()`.
- `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` — `NextSpawnId` es campo `static` sin `Clear()`: resetear a `null` en `NewGame()`.

### Patrones a replicar
- `Assets/_Project/UI/HUD/HUDController.cs` — patrón de UI construida por código + suscripción a eventos (referencia de estilo, aunque la pantalla de resultado va en escena dedicada).
- `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` — patrón singleton `[RuntimeInitializeOnLoadMethod]` + `DontDestroyOnLoad` (si el GameManager necesita persistir).

### Convenciones del proyecto
- `CLAUDE.md` y `.planning/codebase/CONVENTIONS.md` — namespace global (sin `namespace`), Input legacy (`UnityEngine.Input`/`KeyCode`), comentarios en español, stores estáticos para estado entre escenas, rutas de fallback en `Resources.Load`.
- `.planning/codebase/CONCERNS.md` — fragilidad de stores estáticos y resolución por nombre; tenerlo presente al agregar `GameResultStore` y `NewGame()`.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Stores estáticos** (`CarStateStore`, `DeliveredObjectsStore`, `DrunkLevelStore`, `PlayerMoneyStore`, `HeldObjectStore`): patrón directo para `GameResultStore`; todos (salvo `PlayerSpawner.NextSpawnId`) ya exponen `Clear()`, base de `NewGame()`.
- **Patrón Manager** (`DrunkManager`, `PlayerCarController`): molde para el `GameManager` central (D-11).
- **Patrón trigger + `LoadSceneAsync`** (`BarDoorTrigger`, `CityHomeDoorTrigger`, etc.): base para la transición a la escena `Result` y para hookear la victoria al llegar a Home.
- **`CarController.IsControlled` / `SetControlled`**: ya distinguen "manejando" de "no manejando", justo lo que necesita la condición de derrota (D-02).

### Established Patterns
- **Estado entre escenas = stores estáticos** o singletons `DontDestroyOnLoad`. `GameResultStore` usa el primero; el `GameManager` podría usar el segundo si necesita persistir.
- **Descubrimiento de managers por-escena**: `FindFirstObjectByType<T>()` con fallback (HUDController, CarController). El GameManager/auto pueden usarlo para comunicarse.
- **Eventos sobre polling** para cambios discretos (`DrunkManager.OnAlcoholLevelChanged`, `PlayerMoneyStore.OnMoneyChanged`): el choque puede comunicarse vía un evento/callback similar.

### Integration Points
- `CarController.OnCollisionEnter` (nuevo) → notifica al `GameManager` → `GameResultStore = Defeat` → carga `Result`.
- Trigger de llegada a Home → `GameManager` evalúa victoria (total objetos vendidos + alcohol ≥ mínimo) → `GameResultStore = Victory` → carga `Result`.
- Botón Reintentar (escena `Result`) → `GameManager.NewGame()` → `Clear()` de todos los stores + carga Home.
- **Build Settings**: hoy contiene Home/City/Bar (`ProjectSettings/EditorBuildSettings.asset`); hay que **agregar la escena `Result`**.

</code_context>

<specifics>
## Specific Ideas

- El final del loop es **llegar a casa**: la victoria se siente como cerrar el viaje, no como un popup al vender.
- Una sola escena `Result` parametrizada por un flag estático, con presentación visual claramente distinta para ganar vs. perder.
- Reintentar = **partida nueva real** (limpia todo), no un "continuar".
- Detección de obstáculos letales por **componente marcador**, no por tags sueltos.

</specifics>

<deferred>
## Deferred Ideas / Open Items to verify in research

- **Entidades "niño" y "mascota" en City:** el criterio 1 las nombra como obstáculos letales, pero el `CityBuilder` hoy genera casas/árboles del Kenney CityKit. Research/planning debe **verificar si existen** en la escena City; si faltan, el plan debe agregarlas (modelo + collider no-trigger + `LethalObstacle`). Las cuatro categorías deben poder matar.
- **Escena `Result` en Build Settings:** crear la escena y registrarla en `EditorBuildSettings.asset` (hoy: Home, City, Bar).
- **Partículas de choque / feedback visual extra:** Phase 5 (FX-01). No en esta fase.
- (Sin todos cruzados ni scope creep durante la discusión.)

</deferred>

---

*Phase: 3-Loop de victoria y derrota*
*Context gathered: 2026-06-23*
