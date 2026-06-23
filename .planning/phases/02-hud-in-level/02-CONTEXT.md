# Phase 2: HUD in-level - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Mostrar en pantalla, de forma persistente y en cualquier modo de juego (FPS en City/Bar/Home y modo auto en City con `CarFollowCamera`), dos lecturas en tiempo real:
1. Una **barra de borrachera** que refleja el estado del `DrunkManager`.
2. Un **indicador de dinero** (texto TMP) que refleja `PlayerMoneyStore.Money`.

Cumple HUD-01 (barra de borrachera) y HUD-02 (indicador de dinero). Es solo lectura/visualización: no agrega mecánicas nuevas, no toca el balance económico ni el modelo de embriaguez. Pantallas de menú/resultado y partículas/efectos quedan fuera (otras fases).

</domain>

<decisions>
## Implementation Decisions

### Arquitectura y persistencia del HUD
- **D-01:** El HUD es un **singleton que sobrevive las cargas de escena** vía `DontDestroyOnLoad`, auto-arrancado con `[RuntimeInitializeOnLoadMethod]` — mismo patrón que `BackgroundMusicManager` (`Assets/_Project/Core/Audio/BackgroundMusicManager.cs`). Se instancia una sola vez; no se agrega un Canvas a cada escena.
- **D-02:** Como `DrunkManager` es un objeto **por-escena** (no persiste), el HUD debe **re-vincularse al `DrunkManager` activo en cada carga de escena**, suscribiéndose a `SceneManager.sceneLoaded` y re-resolviendo con `FindFirstObjectByType<DrunkManager>()` (con fallback si todavía no existe en ese frame). El dinero no tiene este problema: `PlayerMoneyStore` es estático y siempre está disponible.

### Update del dinero
- **D-03:** Agregar un **evento estático `OnMoneyChanged`** a `PlayerMoneyStore` (`Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs`) que disparan `Add`, `Spend` y `Clear`. El HUD se **suscribe** a ese evento (no hace polling). Es consistente con el patrón de evento de `DrunkManager.OnAlcoholLevelChanged`. El HUD también debe leer el valor actual al suscribirse / al cargar la escena para reflejar el estado inicial correcto.

### Barra de borrachera
- **D-04:** La barra refleja **`DrunkManager.EffectIntensity`** (la curva no lineal `pow(NormalizedLevel, effectExponent)` — lo que el jugador realmente *siente*), tal como pide el criterio de éxito de la fase. NO usar `NormalizedLevel` lineal.
- **D-05:** El fill se **suaviza con lerp** hacia el valor objetivo cada frame (evita saltos bruscos al tomar sorbos). Implementación como **Image con `type = Filled`** (fill horizontal), no Slider. Empieza vacía (alcohol 0) y se llena al tomar.

### Layout y estilo
- **D-06:** Canvas en **Screen Space - Overlay** (cámara-agnóstico: se ve igual en FPS y en modo auto sin reconfigurar por cámara).
- **D-07:** Barra de borrachera + texto de dinero **agrupados en la esquina inferior izquierda**, estilo **minimalista**, sin tapar el centro de la acción ni el mostrador/objetos. Texto de dinero con **TextMeshPro** (TMP disponible vía `com.unity.ugui 2.0.0`).

### Claude's Discretion
- Colores exactos, tipografía, tamaños, y formato del texto de dinero (p. ej. `$50`) quedan a criterio de implementación dentro del estilo minimalista en esquina inferior izquierda.
- Umbrales visuales opcionales de la barra (p. ej. cambio de color al acercarse al máximo) son opcionales; no son requisito de la fase.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap / requisitos
- `.planning/ROADMAP.md` §"Phase 2: HUD in-level" — goal, success criteria, requisitos HUD-01/HUD-02.
- `.planning/REQUIREMENTS.md` — HUD-01 (barra de borrachera), HUD-02 (indicador de dinero).

### Código a leer/extender (fuentes de verdad)
- `Assets/_Project/Core/Managers/DrunkManager.cs` — `EffectIntensity`, `NormalizedLevel`, `AlcoholLevel`/`MaxLevel`, evento `OnAlcoholLevelChanged`. Objeto por-escena.
- `Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs` — store estático `Money`; acá se agrega `OnMoneyChanged` (D-03).
- `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` — patrón de singleton `RuntimeInitializeOnLoadMethod` + `DontDestroyOnLoad` a replicar para el HUD (D-01).

### Convenciones del proyecto
- `CLAUDE.md` y `.planning/codebase/CONVENTIONS.md` — namespace global (sin `namespace`), Input legacy, comentarios en español, stores estáticos para estado entre escenas.

No hay ADRs/specs externos adicionales — los requisitos quedan capturados en las decisiones de arriba.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Patrón singleton de `BackgroundMusicManager`**: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `DontDestroyOnLoad(gameObject)` — base directa para el bootstrap del HUD (D-01).
- **`DrunkManager.OnAlcoholLevelChanged` (event Action<int>)** y `EffectIntensity` — fuente de la barra; el evento sirve para saber cuándo refrescar, aunque el lerp se hace por frame leyendo `EffectIntensity`.
- **`PlayerMoneyStore.Money` (static)** — fuente del texto de dinero; se le agrega el evento `OnMoneyChanged`.
- Carpetas `Assets/_Project/UI/HUD`, `UI/Prefabs`, etc. ya existen (vacías) — destino natural del prefab/script del HUD.

### Established Patterns
- **Persistencia entre escenas** se logra con stores estáticos (`PlayerMoneyStore`, `HeldObjectStore`, `DeliveredObjectsStore`) o singletons `DontDestroyOnLoad`. El HUD usa el segundo.
- **Descubrimiento de managers por-escena**: el código existente usa `FindFirstObjectByType<DrunkManager>()` con fallback (ver `CarController`, `MouseLook`, `PlayerMovement`). El HUD replica este patrón en `sceneLoaded` (D-02).
- **Eventos sobre polling** para cambios de estado discretos (patrón de `DrunkManager`). D-03 lo extiende a `PlayerMoneyStore`.

### Integration Points
- `PlayerMoneyStore.Add/Spend/Clear` → disparan `OnMoneyChanged` → HUD actualiza el texto (venta en `SellCounter.TrySell`, compra en `PlayerPickup`).
- `SceneManager.sceneLoaded` → HUD re-resuelve `DrunkManager` de la nueva escena.
- TMP (`com.unity.ugui 2.0.0`) ya disponible — sin cambios de manifest.

</code_context>

<specifics>
## Specific Ideas

- HUD minimalista, no intrusivo: barra de borrachera + dinero juntos en la esquina inferior izquierda.
- Barra que "se siente": refleja la intensidad del efecto (no el alcohol crudo) y se suaviza con lerp.
- El dinero debe verse cambiar **al instante** al vender o comprar (criterio de éxito 2) — garantizado por el evento `OnMoneyChanged`.

</specifics>

<deferred>
## Deferred Ideas

None — la discusión se mantuvo dentro del scope de la fase. (Persistencia del nivel de alcohol entre escenas es un comportamiento existente del `DrunkManager`, no del HUD; no se aborda en esta fase de solo-visualización.)

</deferred>

---

*Phase: 2-HUD in-level*
*Context gathered: 2026-06-23*
