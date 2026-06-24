---
phase: 01-econom-a
reviewed: 2026-06-22T00:00:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - Assets/_Project/Core/SceneManagement/HeldObjectStore.cs
  - Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs
  - Assets/_Project/Gameplay/Items/CarryableObject.cs
  - Assets/_Project/Gameplay/Items/PickupItem.cs
  - Assets/_Project/Gameplay/Items/SellCounter.cs
  - Assets/_Project/Gameplay/Player/PlayerPickup.cs
  - Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs
  - Assets/_Project/ScriptableObjects/Items/SellableDefinition.cs
findings:
  critical: 1
  warning: 6
  info: 4
  total: 11
status: issues_found
---

# Fase 1: Reporte de Code Review — Economía (Flyweight)

**Reviewed:** 2026-06-22
**Depth:** standard
**Files Reviewed:** 8
**Status:** issues_found

## Summary

Se revisó la slice de economía (compra de bebidas con dinero real, agarrar
objetos vendibles, vender en mostrador, persistencia de dinero y objeto en mano
entre escenas). El núcleo es sólido: `PlayerMoneyStore` evita saldos negativos,
`Spend` re-valida antes de descontar (sin doble gasto), y los Flyweight
(`DrinkDefinition`/`SellableDefinition`) están bien encapsulados.

El defecto crítico es de **persistencia/duplicación**: un objeto vendible
agarrado pero NO marcado como entregado hasta venderlo permite duplicarlo
recargando la escena Home, lo que rompe la integridad de la economía
(item-clonado → venta extra). Además hay varios edge cases de estado en mano
(bebida vs objeto vendible) que pueden dejar al jugador trabado o perder
dinero/items.

## Critical Issues

### CR-01: Objeto vendible se duplica al recargar Home antes de venderlo

**File:** `Assets/_Project/Gameplay/Items/CarryableObject.cs:91-98` (y `Assets/_Project/Gameplay/Items/SellCounter.cs:64-68`)

**Issue:**
`OnPickedUp()` registra el objeto en `HeldObjectStore` y lo desactiva, pero **NO**
lo marca en `DeliveredObjectsStore`. El comentario lo dice explícitamente: "NO
marcar como entregado aqui — se marca al vender en SellCounter.TrySell()".

El problema: la persistencia entre escenas funciona vía recarga total
(`LoadSceneAsync` Single). Si el jugador agarra un objeto en Home y luego cambia
de escena (vuelve a entrar/salir por una puerta que recargue Home, o sale a City
y vuelve) **sin venderlo todavía**, el `Awake` del nuevo `CarryableObject`
consulta `DeliveredObjectsStore.IsTaken(StableId)` → `false` → el objeto
**reaparece en el mundo**, mientras `HeldObjectStore` sigue conservando una copia
"en mano".

Resultado: existen dos instancias lógicas del mismo objeto. El jugador vende la
copia en mano (acredita dinero y recién ahí marca el id como entregado), pero la
copia reaparecida ya no se vuelve a ocultar en esa sesión de escena. Peor: como
`StableId` se deriva de la jerarquía, ambas comparten id, pero la marca de venta
llega tarde. Esto habilita item-cloning y venta inflada → corrupción de la
economía (dinero infinito potencial reagarrando/recargando).

**Fix:**
Marcar el objeto como entregado en el momento de agarrarlo (igual que hace el
patrón existente para items recogidos), y limpiarlo si el jugador lo descarta:

```csharp
public void OnPickedUp()
{
    HeldObjectStore.SetHeld(definition, StableId);
    // Marcar entregado YA: el objeto salió del mundo. Evita que reaparezca
    // al recargar la escena mientras está en mano (duplicación / venta extra).
    DeliveredObjectsStore.MarkTaken(StableId);
    SetHighlighted(false);
    gameObject.SetActive(false);
}
```

(La marca en `SellCounter.TrySell` queda redundante pero inofensiva; conviene
quitarla o dejarla como idempotente.) Si se necesita poder "descartar" un objeto
sin venderlo, agregar un camino explícito que llame `DeliveredObjectsStore`
+ `HeldObjectStore.Clear()` de forma coherente.

## Warnings

### WR-01: Sólo se puede llevar un objeto vendible a la vez, sin feedback al rechazar

**File:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs:144-147`

**Issue:**
`PickupCarryable` sólo se ejecuta si `!HeldObjectStore.HasHeldObject`. Si el
jugador ya lleva un objeto y mira otro vendible, al apretar E no pasa
absolutamente nada (sin SFX de rechazo, sin mensaje). Es una mecánica silenciosa
que confunde: el highlight aparece (línea 219-223 resalta el carryable apuntado)
pero la acción no responde.

**Fix:**
Dar feedback (reproducir `rejectClip`) cuando se intenta agarrar un segundo
objeto, o documentar/clarificar la limitación. Mínimo:
```csharp
else if (currentCarryable != null)
{
    if (!HeldObjectStore.HasHeldObject)
        PickupCarryable(currentCarryable);
    else if (rejectClip != null && sfxSource != null)
        sfxSource.PlayOneShot(rejectClip, Mathf.Clamp01(rejectVolume));
}
```

### WR-02: Bebida en mano se pierde silenciosamente al cambiar de escena

**File:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs:35-38, 304-330`

**Issue:**
El estado de la bebida en mano (`hasHeldDrink`, `heldSips`, `currentHeldVisual`,
`heldAlcoholPerSip`, `heldMaxSips`) es estado de instancia del `MonoBehaviour`, no
persiste en un store estático. A diferencia del objeto vendible (que sí usa
`HeldObjectStore`), si el jugador compra una bebida con sorbos restantes y cruza
una puerta, la bebida y el dinero gastado se pierden. Inconsistente con el patrón
de persistencia documentado en CLAUDE.md y con el manejo del objeto vendible en
esta misma slice.

**Fix:**
Si se espera que las bebidas crucen escenas, persistir su estado en un store
estático análogo a `HeldObjectStore` (definición + sorbos restantes). Si por
diseño no cruzan, documentarlo. Como mínimo evaluar si el jugador puede comprar
en el Bar y beber en otra escena.

### WR-03: Doble estado "en mano" puede dejar al jugador trabado

**File:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs:119-148`

**Issue:**
Existen dos "manos" independientes: `hasHeldDrink` (bebida) y
`HeldObjectStore.HasHeldObject` (objeto vendible). Nada impide tener ambos a la
vez (comprar bebida y luego agarrar un vendible, o viceversa). El `Update`
prioriza beber (`if (hasHeldDrink)`) sobre vender, así que si el jugador tiene
bebida + objeto vendible y mira el mostrador, al apretar E **bebe** en lugar de
vender, sin forma de vender hasta terminar la bebida. Comportamiento sorpresivo
y difícil de descubrir.

**Fix:**
Definir explícitamente la regla (p.ej. no permitir agarrar vendible si hay
bebida en mano, o exponer otra tecla para vender), y priorizar la interacción
según lo que el jugador está apuntando en vez de un orden fijo.

### WR-04: `interactKey` de SellCounter es ignorado; se usa siempre `pickupKey`

**File:** `Assets/_Project/Gameplay/Items/SellCounter.cs:16` y `Assets/_Project/Gameplay/Player/PlayerPickup.cs:119-128`

**Issue:**
`SellCounter.interactKey` está expuesto en el Inspector (default `E`), pero la
venta se dispara desde `PlayerPickup.Update` cuando se aprieta `pickupKey`
(también `E`). El campo del mostrador nunca se lee. Si alguien cambia
`interactKey` en el Inspector esperando rebindear la venta, no tendrá efecto
(magic coupling silencioso).

**Fix:**
O bien eliminar `interactKey` de `SellCounter` (es código muerto que aparenta
configuración), o que `TrySell`/`PlayerPickup` respeten ese key. Recomendado
quitarlo para una sola fuente de verdad.

### WR-05: `Camera.main` puede quedar nulo y romper la selección por raycast

**File:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs:53, 175-179`

**Issue:**
`mainCamera = Camera.main` en `Awake`. Si la cámara aún no tiene el tag
`MainCamera` o se desactiva (p.ej. al entrar al auto, donde `PlayerCarController`
conmuta cámaras), `Camera.main` devuelve null. Hay un re-fetch en
`UpdateSelectionByLook` (175-178), pero si nunca hay cámara con tag MainCamera el
raycast no corre y el highlight/selección quedan muertos sin aviso. El
`holdPoint` creado en `Awake` (61-70) también se parenta a `transform` en vez de
a la cámara si ésta es null, descolocando el visual de la bebida.

**Fix:**
Cachear la cámara del jugador por referencia serializada en vez de depender de
`Camera.main`/tag, o validar y loguear cuando no haya cámara disponible.

### WR-06: `OnPickedUp` del vendible asume `Awake` ya corrió (renderers null si estaba inactivo)

**File:** `Assets/_Project/Gameplay/Items/CarryableObject.cs:48-55, 73-89`

**Issue:**
Si el objeto fue marcado entregado, `Awake` retorna temprano (53-54) dejando
`renderers`/`propertyBlock` en null. `SetHighlighted` se protege con
`if (renderers == null) return;` (75), pero el orden de inicialización en Unity
para objetos que arrancan inactivos no garantiza que `Awake` haya corrido antes
de que `PlayerPickup` intente resaltarlo si el flujo cambia. Hoy es seguro porque
los entregados quedan inactivos (no raycasteables), pero el acoplamiento es
frágil: cualquier cambio que reactive el objeto sin re-ejecutar `Awake` provoca
NRE en `OnPickedUp` (que no chequea null antes de usar el estado de render via
`SetHighlighted`, aunque sí está cubierto). Riesgo latente de NullReference.

**Fix:**
Inicializar `renderers`/`propertyBlock` de forma perezosa (lazy) en
`SetHighlighted`/`OnPickedUp` en vez de depender de que `Awake` haya corrido, o
documentar la invariante de que los objetos entregados nunca se reactivan.

## Info

### IN-01: `Debug.Log` en caliente queda en build de producción

**File:** `Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs:19, 28`; `Assets/_Project/Gameplay/Items/SellCounter.cs:79`; `Assets/_Project/Gameplay/Player/PlayerPickup.cs:116`

**Issue:**
Varios `Debug.Log` de cada transacción de dinero quedan activos. En gameplay
real generan spam de consola y costo menor. Aceptable para un proyecto de
cátedra, pero conviene envolverlos.

**Fix:**
Envolver en `#if UNITY_EDITOR` o un flag `verboseLogging`, o reducir a logs de
debug condicionales.

### IN-02: Tecla de debug para agregar dinero activa en runtime

**File:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs:24-26, 113-117`

**Issue:**
`debugAddMoneyKey` (M) suma `debugAddMoneyAmount` en cualquier build, no sólo en
Editor. Es un cheat embebido. Para una entrega de cátedra puede ser intencional,
pero conviene aislarlo.

**Fix:**
Condicionar con `#if UNITY_EDITOR` o `Debug.isDebugBuild`.

### IN-03: SellValue/Price/alcohol sin validación de no-negativos en los SO

**File:** `Assets/_Project/ScriptableObjects/Items/SellableDefinition.cs:17`; `Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs:17, 21, 23`

**Issue:**
`sellValue`, `price`, `alcoholPerSip`, `maxSips` se editan libremente en el
Inspector sin `[Min(0)]`/`[Min(1)]`. Un valor negativo en `price` haría que
`CanAfford` siempre pase y `Spend(<=0)` retorne true sin descontar (línea 25 de
PlayerMoneyStore), o un `sellValue` negativo intentaría acreditar 0 (Add ignora
<=0). No es explotable hoy, pero un misconfig pasa silencioso.

**Fix:**
Anotar los campos: `[Min(0)] price`, `[Min(0)] sellValue`, `[Min(1)] maxSips`,
`[Min(0)] alcoholPerSip`.

### IN-04: Comentario referencia decisiones (D-xx) sin enlace; ruido para mantenimiento

**File:** Varios (p.ej. `HeldObjectStore.cs:4`, `CarryableObject.cs:94`, `PlayerPickup.cs:102, 112`)

**Issue:**
Referencias a `D-03`, `D-10`, `RESEARCH.md`, `VALIDATION.md` en comentarios de
código. Útil ahora, pero sin trazabilidad estable se vuelve ruido cuando esos
docs cambien o se archiven.

**Fix:**
Mantener si el equipo usa esas referencias activamente; de lo contrario, mover el
porqué a un comentario autocontenido.

---

_Reviewed: 2026-06-22_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
