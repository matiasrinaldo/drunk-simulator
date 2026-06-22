# Phase 1: Economía - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Esta fase entrega la **economía con dinero** que reemplaza el trueque actual. Los objetos de la casa pasan a tener un valor monetario; venderlos en el bar acredita dinero al jugador; las bebidas tienen precio y solo se compran si hay dinero suficiente. El **patrón Flyweight** (ScriptableObject compartido) es la fuente de verdad de precios y alcohol, sin valores duplicados hardcodeados en prefabs.

**Cubre:** ECON-01, ECON-02, ECON-03, ECON-04, PAT-01.

**No cubre (otras fases):** la visualización del dinero/borrachera en pantalla (HUD — Phase 2), condiciones de victoria/derrota (Phase 3), animaciones (Phase 4), partículas/luces/loading (Phase 5).

</domain>

<decisions>
## Implementation Decisions

### Mecánica de venta
- **D-01:** La venta se concreta en un **mostrador/zona de venta dentro del bar** mediante **raycast + tecla E** — reusa el patrón de `PlayerPickup.UpdateSelectionByLook` + `Input.GetKeyDown(pickupKey)` ya existente. No es venta automática por trigger ni "soltar en un punto".
- **D-02:** **Un objeto por viaje.** Se mantiene el modelo actual de `PlayerPickup.hasHeldObject` como `bool` único (un solo objeto en mano a la vez). No se introduce inventario ni contador múltiple. El loop sigue siendo: agarrar 1 objeto en Home → manejar al bar → vender → comprar bebida → volver por el siguiente.
- **D-03:** Al agarrar un `CarryableObject` hoy el objeto se desactiva sin recordar *cuál* era ni su valor. Para venderlo con su valor correcto, el estado del objeto sostenido debe **recordar su `SellableDefinition`** (no solo el bool `hasHeldObject`). El cómo exacto (campo en el store estático / referencia transportada entre escenas) lo define el plan.

### Flyweight / definiciones (PAT-01)
- **D-04:** **Referencia explícita al ScriptableObject** en cada `PickupItem` (→ `DrinkDefinition`) y cada `CarryableObject` (→ `SellableDefinition`). Esto **elimina `ResolvedPickupType` por nombre** (concern de fragilidad conocido en CONCERNS.md). No se usa resolución por nombre ni híbrido.
- **D-05:** Los `SellableDefinition` se organizan como **catálogo por tipo de objeto** (TV, lámpara, cuadro, etc.): una definición por tipo, **compartida por todas las instancias** de ese tipo. Es el uso "de libro" del Flyweight (estado intrínseco compartido entre muchos objetos).
- **D-06:** `DrinkDefinition` es la fuente de verdad de **precio + alcohol por sorbo (+ maxSips)** de cada bebida; `SellableDefinition` es la fuente de verdad del **valor de venta** de cada tipo de objeto. Los valores que hoy viven hardcodeados en `PickupItem` (`beerAlcoholPerSip`, `cocktailAlcoholPerSip`, `whiskyAlcoholPerSip`, `maxSips`) migran al SO.

### Balance económico
- **D-07:** El jugador **arranca con $0**. Debe vender un objeto sí o sí antes de poder comprar la primera bebida — fuerza el loop completo desde el arranque y refuerza el core value.
- **D-08:** **Bebidas más fuertes cuestan más**: whisky (emborracha 3) > trago (2) > cerveza (1) en precio. Crea una decisión económica real (muchas baratas vs. pocas fuertes).
- **D-09:** Las **cifras exactas** (valores de objetos, precios de bebidas) son **tuneables desde el Inspector vía los ScriptableObjects**. Claude propone valores sensatos iniciales en el plan; no hay número fijo locked por el usuario.

### Feedback al no poder comprar
- **D-10:** Si el jugador intenta comprar una bebida y **no le alcanza el dinero**, suena un **SFX de rechazo** y la compra **no se concreta**. Reusa el patrón `AudioSource` + `Resources.Load("Audio/SFX/...")` con rutas de fallback que ya usa `PlayerPickup` (DrinkSip/PayDrink). Sin mensaje en pantalla en esta fase (eso es territorio del HUD, Phase 2).

### Persistencia
- **D-11:** El dinero **persiste entre escenas** (Success Criteria #2). Sigue el patrón de stores estáticos en memoria existente (`CarStateStore`, `DeliveredObjectsStore`, `PlayerSpawner.NextSpawnId`) — un nuevo store estático tipo `PlayerMoneyStore` (nombre tentativo). Debe exponer `Clear()` para futura "Nueva partida", igual que los otros stores.

### Claude's Discretion
- Valores/precios numéricos iniciales en los ScriptableObjects (D-09).
- Nombre y forma exacta del store de dinero y del transporte de la `SellableDefinition` del objeto sostenido entre escenas (D-03, D-11).
- Clip/asset concreto del SFX de rechazo (puede reusar uno existente o agregar uno nuevo bajo `Audio/SFX/`) (D-10).
- Forma exacta del mostrador de venta en la escena Bar (GameObject con collider + componente de venta), siguiendo el patrón de triggers/pickups existente (D-01).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requisitos y roadmap
- `.planning/REQUIREMENTS.md` — ECON-01..04 y PAT-01 (definición de los requisitos de esta fase).
- `.planning/ROADMAP.md` §"Phase 1: Economía" — Goal y los 5 Success Criteria que el plan debe satisfacer.
- `.planning/PROJECT.md` — Core Value (loop de dificultad creciente), decisiones clave y la mecánica actual a reemplazar (trueque).

### Mapa del codebase (relevante a esta fase)
- `.planning/codebase/ARCHITECTURE.md` — flujo de pickup/drunk/escenas y los stores estáticos de persistencia.
- `.planning/codebase/CONCERNS.md` — concerns conocidos: resolución de tipo por nombre (que D-04 elimina), stores estáticos frágiles (`PlayerPickup.hasHeldObject` sin `Clear()`), bug de alcohol que no persiste (relevante a fases posteriores, no a esta).
- `.planning/codebase/CONVENTIONS.md` — namespace global, input legacy, comentarios en español.

### Código a modificar (fuentes directas)
- `Assets/_Project/Gameplay/Player/PlayerPickup.cs` — flujo de trueque actual (`Update`, `Pickup`, `PickupCarryable`); acá se inserta comprar-con-dinero y vender.
- `Assets/_Project/Gameplay/Items/PickupItem.cs` — bebidas; migrar valores a `DrinkDefinition` y agregar referencia al SO; agregar precio.
- `Assets/_Project/Gameplay/Items/CarryableObject.cs` — objetos de la casa; agregar referencia a `SellableDefinition`.
- `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` — patrón de store estático a imitar para el dinero; ya marca objetos entregados.
- `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` — ejemplo de trigger/escena en el bar (patrón de referencia para el mostrador de venta).

No hay ADRs/specs externos adicionales — los requisitos están capturados en las decisiones de arriba más REQUIREMENTS.md/ROADMAP.md.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`PlayerPickup` raycast + E:** `UpdateSelectionByLook()` (raycast desde cámara + `OverlapSphere`) y `Input.GetKeyDown(pickupKey)` ya resuelven "mirar algo y apretar E". El mostrador de venta y la compra de bebida se montan sobre este mismo flujo.
- **Patrón `AudioSource` + `Resources.Load` con fallback:** `PlayerPickup` ya carga `Audio/SFX/DrinkSip` y `Audio/SFX/PayDrink` con rutas de fallback. El SFX de rechazo (D-10) sigue ese patrón.
- **Stores estáticos en memoria:** `CarStateStore`, `DeliveredObjectsStore`, `PlayerSpawner.NextSpawnId` — molde directo para `PlayerMoneyStore` (D-11), incluyendo `Clear()`.
- **Highlight por `MaterialPropertyBlock` + emisión:** tanto `PickupItem` como `CarryableObject` ya resaltan al apuntar; el mostrador puede reutilizar el mismo mecanismo si necesita feedback visual.

### Established Patterns
- **ScriptableObjects ya previstos:** existe `Assets/_Project/ScriptableObjects/Items/` (vacío) — destino natural de `DrinkDefinition` y `SellableDefinition`.
- **Namespace global, input legacy, comentarios en español** (CONVENTIONS.md) — mantener.
- **DrunkManager** sigue siendo la fuente de verdad del alcohol; el alcohol por sorbo ahora viene del `DrinkDefinition` pero se sigue aplicando con `drunkManager.AddAlcohol(...)`.

### Integration Points
- **Compra de bebida:** en `PlayerPickup.Update` la rama actual `currentPickupItem != null && hasHeldObject` (trueque) se reemplaza por "tenés dinero suficiente → descontar precio → tomar"; se elimina la dependencia de `hasHeldObject` para tomar.
- **Venta de objeto:** nueva interacción en el bar (mostrador) que lee la `SellableDefinition` del objeto sostenido, suma al `PlayerMoneyStore` y consume el objeto en mano (`hasHeldObject = false`).
- **Migración de datos:** `PickupItem.ResolvedPickupType` (resolución por nombre) se retira en favor de la referencia explícita al `DrinkDefinition` (D-04).

</code_context>

<specifics>
## Specific Ideas

- El loop concreto que el usuario tiene en mente: agarrás **1 objeto** en Home → manejás borracho al bar → lo **vendés en un mostrador con E** → con esa plata **comprás** una bebida (más fuerte = más cara) → volvés por el siguiente. Arrancás sin un peso, así que la primera acción obligada es vender.
- El Flyweight no es decorativo: se demuestra con un **catálogo de tipos** donde muchas instancias comparten una misma definición intrínseca.

</specifics>

<deferred>
## Deferred Ideas

- **Inventario / llevar varios objetos a la vez** — se evaluó y se descartó para esta fase (D-02: un objeto por viaje). Si en el futuro el loop se siente demasiado repetitivo, reconsiderar.
- **Mensaje/UI de "no te alcanza"** — diferido al HUD (Phase 2); en esta fase el feedback de compra fallida es solo sonoro (D-10).

</deferred>

---

*Phase: 1-Economía*
*Context gathered: 2026-06-22*
