# Phase 1: Economía - Research

**Researched:** 2026-06-22
**Domain:** Unity ScriptableObject / Flyweight, economía in-game, persistencia de escenas
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Decisiones cerradas (Locked Decisions)
- **D-01:** La venta se concreta en un mostrador dentro del bar, raycast + tecla E (reusar `PlayerPickup.UpdateSelectionByLook` + `Input.GetKeyDown(pickupKey)`).
- **D-02:** Un objeto por viaje. `PlayerPickup.hasHeldObject` sigue siendo bool único. Sin inventario múltiple.
- **D-03:** El estado del objeto sostenido debe recordar su `SellableDefinition` entre escenas (no solo el bool).
- **D-04:** Referencia explícita al ScriptableObject en cada `PickupItem` (→ `DrinkDefinition`) y `CarryableObject` (→ `SellableDefinition`). Elimina `ResolvedPickupType` por nombre.
- **D-05:** Catálogo de `SellableDefinition` por tipo (TV, lámpara, cuadro…), compartido por instancias. Flyweight de libro.
- **D-06:** `DrinkDefinition` = fuente de verdad de precio + alcohol/sorbo + maxSips. `SellableDefinition` = valor de venta.
- **D-07:** El jugador arranca con $0.
- **D-08:** Bebidas más fuertes cuestan más (whisky > trago > cerveza).
- **D-09:** Cifras tuneables desde Inspector; Claude propone valores iniciales.
- **D-10:** SFX de rechazo si no alcanza el dinero (patrón `AudioSource` + `Resources.Load` con fallback). Sin UI en pantalla (Phase 2).
- **D-11:** Dinero persiste vía nuevo store estático `PlayerMoneyStore` con `Clear()`.

### Claude's Discretion
- Valores/precios numéricos iniciales en los ScriptableObjects.
- Nombre y forma exacta del store de dinero y del transporte de la `SellableDefinition` entre escenas.
- Clip/asset del SFX de rechazo.
- Forma exacta del mostrador de venta en la escena Bar.

### Ideas Diferidas (OUT OF SCOPE)
- Inventario / llevar varios objetos a la vez.
- Mensaje/UI de "no te alcanza" (Phase 2 — HUD).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ECON-01 | Cada objeto agarrable de la casa tiene un valor monetario configurable | `SellableDefinition` ScriptableObject con campo `sellValue`; asignado en Inspector sobre cada `CarryableObject` prefab/instancia |
| ECON-02 | Al vender un objeto en el bar, el jugador recibe su valor en dinero | Nuevo componente `SellCounter` en Bar; lee `HeldObjectStore.HeldDefinition`, suma a `PlayerMoneyStore`, consume el objeto |
| ECON-03 | Cada bebida tiene un precio y solo se puede comprar si el jugador tiene dinero suficiente | `DrinkDefinition` con campo `price`; `PlayerPickup.Pickup()` verifica `PlayerMoneyStore.Money >= price` antes de dejar tomar |
| ECON-04 | Comprar una bebida descuenta su precio del dinero del jugador (reemplaza el trueque actual) | La rama `hasHeldObject` en `PlayerPickup.Update` se elimina; la compra descuenta del `PlayerMoneyStore` |
| PAT-01 | Implementar el patrón Flyweight para datos compartidos | `DrinkDefinition` y `SellableDefinition` como ScriptableObjects compartidos; múltiples instancias de prefab apuntan al mismo asset |
</phase_requirements>

---

## Summary

Esta fase implementa una economía con dinero sobre un codebase ya funcional de pickup/drunk/escenas. El trabajo se divide en tres capas ortogonales: (1) crear los ScriptableObjects Flyweight (`DrinkDefinition`, `SellableDefinition`) y cablear referencias en los prefabs existentes; (2) agregar la capa de persistencia de estado (`PlayerMoneyStore`, `HeldObjectStore`) siguiendo el patrón de stores estáticos ya establecido; (3) modificar `PlayerPickup` para reemplazar el trueque por compra-con-dinero y agregar el nuevo componente `SellCounter` en el bar.

El riesgo técnico más alto es la migración de `PickupItem.ResolvedPickupType` (D-04): hay 3 prefabs de bebida en `Assets/_Project/Prefabs/Items/` y 3 instancias `PickupItem` en `Bar.unity` que hay que re-cablear manualmente en el Inspector luego de agregar el campo `[SerializeField] private DrinkDefinition definition`. El orden importa: agregar el campo al script primero, dejar que Unity recompile, luego asignar el asset en cada prefab/instancia, y sólo entonces eliminar la resolución por nombre. Si se borra la propiedad `ResolvedPickupType` antes de re-cablear, los prefabs pierden el dato; Unity llenará el nuevo campo con `null` y habrá que asignarlo a mano de todas formas — hacer el rename en dos commits separados es la estrategia segura.

El transporte de la `SellableDefinition` entre escenas (D-03) requiere un segundo store estático (`HeldObjectStore`) que recuerde qué definición tiene el jugador en mano. No es posible guardar una referencia de MonoBehaviour entre escenas, pero sí se puede guardar una referencia a un asset ScriptableObject: los assets viven en el proyecto, no en la escena, por lo que sobreviven a `LoadSceneAsync(Single)` sin problemas.

**Recomendación primaria:** Crear los assets ScriptableObject primero (Wave 0 de setup), luego migrar los prefabs y scripts en ese orden: SO → scripts con nuevos campos → Inspector re-cabling → eliminar código viejo → stores → lógica de compra/venta.

---

## Architectural Responsibility Map

| Capacidad | Tier Principal | Tier Secundario | Justificación |
|-----------|----------------|-----------------|---------------|
| Definición de precios y alcohol | ScriptableObject asset (proyecto) | — | Los SOs son assets del proyecto, no de escena; compartidos por todas las instancias |
| Estado del dinero del jugador | Store estático en memoria (`PlayerMoneyStore`) | — | Debe sobrevivir a `LoadSceneAsync(Single)` sin DontDestroyOnLoad |
| Estado del objeto sostenido | Store estático en memoria (`HeldObjectStore`) | — | La `SellableDefinition` es un asset (referencia válida entre escenas); el bool `hasHeldObject` migra acá |
| Compra de bebida | `PlayerPickup` (Gameplay/Player) | `DrinkDefinition` (SO) | Contiene la lógica de interacción con el jugador; lee precio del SO y saldo del store |
| Venta de objeto | Nuevo `SellCounter` (Gameplay/Items o Gameplay/Systems) | `HeldObjectStore`, `PlayerMoneyStore` | Es un objeto de escena del Bar con raycast; encaja en el flujo de pickup existente |
| SFX de rechazo | `PlayerPickup` (o `SellCounter`) | `AudioSource` + `Resources.Load` | Sigue el patrón de drinkSipClip/payDrinkClip ya establecido en `PlayerPickup` |
| Persistencia entre escenas | Stores estáticos (`PlayerMoneyStore`, `HeldObjectStore`) | — | Patrón establecido: `CarStateStore`, `DeliveredObjectsStore` |

---

## Standard Stack

### Core (ya presente en el proyecto — no instalar nada)

| Componente | Versión | Propósito | Por qué es el estándar |
|-----------|---------|-----------|------------------------|
| Unity Engine | 6000.3.11f1 | Runtime principal | Fijo por consigna del proyecto |
| URP | 17.3 | Render pipeline | Ya configurado; MaterialPropertyBlock en uso |
| `ScriptableObject` (UnityEngine) | — | Flyweight de datos | API nativa de Unity; no requiere package adicional |
| `[CreateAssetMenu]` attribute | — | Crear assets SO desde menú | API nativa, idiomática para catálogos de datos |
| `[SerializeField] private` | — | Exponer campos al Inspector | Convención establecida del proyecto (CONVENTIONS.md) |

### No hay packages nuevos para esta fase

Esta fase no introduce ninguna dependencia nueva. Todo lo necesario (`ScriptableObject`, `AudioSource`, `Resources.Load`, `Physics.Raycast`, `LoadSceneAsync`) ya está en uso en el proyecto. No se instala ningún paquete adicional.

## Package Legitimacy Audit

> No aplica para esta fase — no se instalan packages externos.

---

## Architecture Patterns

### Diagrama de flujo de la economía

```
[Home] CarryableObject (SellableDefinition asignada en Inspector)
          |
          | Jugador presiona E → PickupCarryable()
          |
          v
    HeldObjectStore.SetHeld(definition, stableId)  ← store estático
    PlayerPickup.hasHeldObject = true
          |
          | Escena carga → LoadSceneAsync("Bar")  ← HeldObjectStore sobrevive
          |
[Bar]     v
    SellCounter.OnInteract()  ← raycast + E detecta "mostrador"
          |
          +-- Lee HeldObjectStore.HeldDefinition.sellValue
          +-- PlayerMoneyStore.Add(sellValue)  ← store estático
          +-- HeldObjectStore.Clear()
          |
          v
    Jugador apunta a PickupItem (bebida) en Bar
          |
          | Presiona E → PlayerPickup.Pickup()
          |
          +-- Verifica PlayerMoneyStore.Money >= definition.price
          |       No alcanza → sfxSource.PlayOneShot(rejectClip) + return
          |
          +-- PlayerMoneyStore.Spend(definition.price)
          +-- drunkManager.AddAlcohol() ahora usa definition.alcoholPerSip
          |
          v
    Jugador vuelve a City → LoadSceneAsync("City")  ← PlayerMoneyStore sobrevive
```

### Estructura de archivos nuevos propuesta

```
Assets/_Project/
├── ScriptableObjects/
│   └── Items/
│       ├── DrinkDefinition.cs          ← nuevo ScriptableObject
│       ├── SellableDefinition.cs       ← nuevo ScriptableObject
│       └── Instances/                  ← assets concretos (creados desde menú)
│           ├── Cerveza.asset
│           ├── Trago.asset
│           ├── Whisky.asset
│           ├── TV.asset
│           ├── Lampara.asset
│           └── Cuadro.asset (etc.)
├── Core/SceneManagement/
│   ├── PlayerMoneyStore.cs             ← nuevo store estático
│   └── HeldObjectStore.cs             ← nuevo store estático (reemplaza hasHeldObject)
└── Gameplay/Items/
    └── SellCounter.cs                  ← nuevo componente del mostrador
```

### Patrón 1: ScriptableObject Flyweight (D-04, D-05, D-06)

**Qué es:** Un asset SO por tipo (no por instancia). Muchos `PickupItem`/`CarryableObject` apuntan al mismo asset.

**Cuándo usarlo:** Datos intrínsecos que no cambian entre instancias (precio, alcohol/sorbo, valor de venta).

**Ejemplo — DrinkDefinition:**
```csharp
// Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs
// Source: Unity Learn "Flyweight Pattern" (Unity 6) + docs.unity3d.com/Manual/class-ScriptableObject.html

[CreateAssetMenu(fileName = "DrinkDefinition", menuName = "Drunk Simulator/Drink Definition")]
public class DrinkDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField] private string drinkName = "Bebida";

    [Header("Economia")]
    [Tooltip("Precio en pesos para comprar esta bebida")]
    [SerializeField] private int price = 10;

    [Header("Alcohol")]
    [Tooltip("Unidades de alcohol agregadas por sorbo")]
    [SerializeField] private int alcoholPerSip = 1;
    [Tooltip("Cuantos sorbos tiene la bebida")]
    [SerializeField] private int maxSips = 4;

    public string DrinkName => drinkName;
    public int Price => price;
    public int AlcoholPerSip => alcoholPerSip;
    public int MaxSips => maxSips;
}
```

**Ejemplo — SellableDefinition:**
```csharp
// Assets/_Project/ScriptableObjects/Items/SellableDefinition.cs

[CreateAssetMenu(fileName = "SellableDefinition", menuName = "Drunk Simulator/Sellable Definition")]
public class SellableDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField] private string itemName = "Objeto";

    [Header("Economia")]
    [Tooltip("Valor en pesos al vender este tipo de objeto")]
    [SerializeField] private int sellValue = 20;

    public string ItemName => itemName;
    public int SellValue => sellValue;
}
```

**Cómo se cablea en los prefabs:**
```csharp
// PickupItem.cs — campo nuevo, reemplaza resolución por nombre
[Header("Definicion")]
[SerializeField] private DrinkDefinition definition;

// Propiedades migradas desde hardcoded
public int AlcoholPerSip => definition != null ? definition.AlcoholPerSip : 0;
public int MaxSips => definition != null ? definition.MaxSips : 1;
public int Price => definition != null ? definition.Price : 0;

// CarryableObject.cs — campo nuevo
[Header("Definicion")]
[SerializeField] private SellableDefinition definition;

public SellableDefinition Definition => definition;
public int SellValue => definition != null ? definition.SellValue : 0;
```

### Patrón 2: Store estático para el dinero (D-11)

**Qué es:** Clase estática C# pura, idéntica en estructura a `CarStateStore` y `DeliveredObjectsStore`. No hereda de MonoBehaviour.

**Cuándo usarlo:** Estado que debe sobrevivir a `LoadSceneAsync(Single)` sin DontDestroyOnLoad.

**Ejemplo — PlayerMoneyStore:**
```csharp
// Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs
// Sigue el patron de CarStateStore.cs

/// <summary>
/// Guarda el dinero del jugador entre cargas de escena.
/// Se resetea al cerrar el juego o al llamar Clear() (Nueva partida).
/// </summary>
public static class PlayerMoneyStore
{
    public static int Money { get; private set; } = 0;

    /// <summary>Suma la cantidad indicada al saldo del jugador.</summary>
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
    }

    /// <summary>Descuenta la cantidad indicada. Retorna false si no hay saldo suficiente.</summary>
    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;
        Money -= amount;
        return true;
    }

    /// <summary>Indica si hay saldo suficiente para el monto dado.</summary>
    public static bool CanAfford(int amount) => Money >= amount;

    /// <summary>Resetea el dinero a cero (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        Money = 0;
    }
}
```

### Patrón 3: Store del objeto sostenido (D-03)

**Por qué un store aparte:** La referencia a un `SellableDefinition` (asset ScriptableObject) sobrevive a `LoadSceneAsync` porque los assets no pertenecen a ninguna escena — viven en el proyecto. Sólo hay que guardar la referencia en un campo estático antes de cargar la escena. No se necesita serialización a disco ni ningún mecanismo especial.

**Nota crítica:** No guardar una referencia a `CarryableObject` (MonoBehaviour de escena): se destruye con la escena. Sólo guardar la `SellableDefinition` (asset) y el `stableId` (string) para poder marcar el objeto como entregado después.

```csharp
// Assets/_Project/Core/SceneManagement/HeldObjectStore.cs

/// <summary>
/// Recuerda si el jugador lleva un objeto en mano y cual es su definicion,
/// de manera que la informacion persista al cambiar de escena.
/// Reemplaza el campo estatico PlayerPickup.hasHeldObject.
/// </summary>
public static class HeldObjectStore
{
    public static bool HasHeldObject { get; private set; }
    public static SellableDefinition HeldDefinition { get; private set; }
    public static string HeldObjectId { get; private set; }

    /// <summary>Registra que el jugador agarró un objeto.</summary>
    public static void SetHeld(SellableDefinition definition, string stableId)
    {
        HeldDefinition = definition;
        HeldObjectId = stableId;
        HasHeldObject = true;
    }

    /// <summary>Libera el objeto (vendido o descartado).</summary>
    public static void Clear()
    {
        HeldDefinition = null;
        HeldObjectId = null;
        HasHeldObject = false;
    }
}
```

**Integración con `CarryableObject.OnPickedUp()`:**
```csharp
// Antes: DeliveredObjectsStore.MarkTaken(StableId); hasHeldObject = true;
// Después:
public void OnPickedUp()
{
    // NO marcar como entregado todavía — se marca al vender, no al agarrar
    SetHighlighted(false);
    gameObject.SetActive(false);
    HeldObjectStore.SetHeld(definition, StableId);
}
```

**Nota:** El `DeliveredObjectsStore.MarkTaken(stableId)` se debe mover al momento de la venta (en `SellCounter`), no al agarrar. Si el jugador agarra pero NO llega al bar (por ejemplo cierra el juego o reinicia), el objeto no debe quedar marcado como entregado. Sin embargo para esta fase MVP (sin "nueva partida") se puede marcar al agarrar sin problema funcional.

### Patrón 4: SellCounter — mostrador de venta (D-01)

**Qué es:** Un MonoBehaviour nuevo en el Bar que detecta si el jugador (con `HeldObjectStore.HasHeldObject == true`) presiona E mirando el mostrador.

**Cómo se monta:** Un GameObject con `Collider` (trigger o sólido), la cámara hace raycast sobre él y `PlayerPickup.UpdateSelectionByLook` lo detecta. Pero la lógica de venta puede vivir directamente en el `SellCounter`, no en `PlayerPickup`. El `SellCounter` tiene su propio `Update` que lee la tecla E, o bien `PlayerPickup` puede delegarle.

**Opción recomendada — SellCounter autónomo:**

```csharp
// Assets/_Project/Gameplay/Items/SellCounter.cs
// Montado en el mostrador del Bar; funciona junto con el highlight de PlayerPickup

[RequireComponent(typeof(Collider))]
public class SellCounter : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip sellClip;
    [SerializeField, Range(0f, 1f)] private float sellVolume = 1f;

    [Header("Config")]
    public KeyCode interactKey = KeyCode.E;
    public Color highlightColor = new Color(0.3f, 1f, 0.3f, 1f);

    AudioSource sfxSource;
    PlayerPickup playerPickup;

    void Awake()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (sellClip == null)
        {
            sellClip = Resources.Load<AudioClip>("Audio/SFX/Sell");
            if (sellClip == null)
                sellClip = Resources.Load<AudioClip>("Audio/SFX/PayDrink"); // fallback al existente
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        // Solo actua si el jugador está mirando este mostrador (PlayerPickup se encarga del raycast)
        // y tiene un objeto en mano
        if (!HeldObjectStore.HasHeldObject) return;

        // Buscar si el jugador está apuntando a este objeto (verificar por proximidad + look)
        // Simplificación MVP: si el jugador tiene objeto, cualquier E frente al mostrador vende
        TrySell();
    }

    void TrySell()
    {
        SellableDefinition def = HeldObjectStore.HeldDefinition;
        if (def == null) return;

        int valor = def.SellValue;
        PlayerMoneyStore.Add(valor);

        // Marcar el objeto como entregado permanentemente
        if (!string.IsNullOrEmpty(HeldObjectStore.HeldObjectId))
            DeliveredObjectsStore.MarkTaken(HeldObjectStore.HeldObjectId);

        HeldObjectStore.Clear();

        if (sellClip != null && sfxSource != null)
            sfxSource.PlayOneShot(sellClip, Mathf.Clamp01(sellVolume));

        Debug.Log($"[SellCounter] Objeto vendido por ${valor}. Saldo: ${PlayerMoneyStore.Money}");
    }
}
```

**Alternativa más limpia (recomendada para la integración con highlight):** Hacer que `PlayerPickup` detecte `SellCounter` igual que detecta `PickupItem`/`CarryableObject` — agregando `currentSellCounter` a su flujo de `UpdateSelectionByLook`. La ventaja es que el highlight del mostrador reusar exactamente el mismo pipeline de emisión ya existente. El planeador puede elegir entre ambos enfoques según granularidad de tareas.

### Patrón 5: Compra con dinero en PlayerPickup (D-04, ECON-03, ECON-04)

**Qué cambia:** En `PlayerPickup.Update`, la rama que hoy dice `currentPickupItem != null && hasHeldObject` (trueque) se reemplaza por verificación de saldo.

**Antes (trueque — eliminar):**
```csharp
else if (currentPickupItem != null && hasHeldObject)
{
    hasHeldObject = false;
    Pickup(currentPickupItem);
}
```

**Después (compra con dinero):**
```csharp
else if (currentPickupItem != null)
{
    int precio = currentPickupItem.Price;
    if (!PlayerMoneyStore.CanAfford(precio))
    {
        // SFX de rechazo (D-10)
        if (rejectClip != null && sfxSource != null)
            sfxSource.PlayOneShot(rejectClip, Mathf.Clamp01(rejectVolume));
        return;
    }
    PlayerMoneyStore.Spend(precio);
    Pickup(currentPickupItem);
}
```

**Nota:** La condición `hasHeldObject` desaparece del flujo de bebida. El jugador puede comprar una bebida independientemente de si lleva un objeto (el objeto lo lleva el `HeldObjectStore`, no bloquea la compra). Esto es correcto por D-02: un solo objeto físico por viaje, pero en el bar ya lo vendió antes de poder comprar.

### Anti-Patrones a Evitar

- **SerializeReference para los SOs:** No usar `[SerializeReference]` en los campos de `DrinkDefinition`/`SellableDefinition`. `SerializeReference` es para polimorfismo de clases C# puras. Los `ScriptableObject` son `UnityEngine.Object`; se serializan por referencia automáticamente con `[SerializeField]` normal — Unity almacena el GUID del asset. [VERIFIED: docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html]
- **Guardar referencia a MonoBehaviour entre escenas:** No intentar guardar `CarryableObject` en `HeldObjectStore`. El objeto MonoBehaviour se destruye con la escena. Sólo guardar la `SellableDefinition` (asset) y el `stableId` (string).
- **Eliminar ResolvedPickupType antes de re-cablear prefabs:** El campo `ResolvedPickupType` debe eliminarse sólo DESPUÉS de asignar `DrinkDefinition` en todos los prefabs e instancias de escena. Ver sección "Migración de ResolvedPickupType".
- **Modificar PlayerMoneyStore después de LoadSceneAsync:** Siempre modificar los stores ANTES de llamar a `SceneManager.LoadSceneAsync`. Leerlos sólo en `Start` (no en `Awake`) del destino. [VERIFIED: patrón establecido en ARCHITECTURE.md]

---

## Migración de ResolvedPickupType (D-04) — Orden seguro

**Contexto del codebase:**
- 3 prefabs: `Assets/_Project/Prefabs/Items/Whisky.prefab`, `Cerveza.prefab`, `Trago.prefab`
- 3 instancias `PickupItem` en `Bar.unity` (verificado por grep en escena)
- `PlayerPickup.CreateHeldVisual()` usa `item.ResolvedPickupType.ToString()` para nombrar el clon (línea 266). Este uso también debe actualizarse.

**Orden de cambios (sin romper el juego entre pasos):**

1. **Crear los assets SO** (`DrinkDefinition.cs` + assets `.asset`) sin tocar scripts existentes
2. **Agregar campo `[SerializeField] private DrinkDefinition definition;`** a `PickupItem.cs`. Dejar `ResolvedPickupType` intacto por ahora — Unity recompila, los prefabs siguen funcionando (el campo nuevo está `null`)
3. **Cablear el campo `definition`** en el Inspector de cada prefab (Whisky, Cerveza, Trago) y en las 3 instancias de Bar.unity
4. **Agregar propiedades que lean del SO** (`Price`, `AlcoholPerSip`, `MaxSips`) manteniendo los campos viejos como fallback temporal
5. **Verificar en Play Mode** que las bebidas funcionen con el SO
6. **Eliminar `ResolvedPickupType`**, `itemName`, y los campos hardcodeados (`beerAlcoholPerSip`, etc.)
7. **Limpiar** el `CreateHeldVisual` para no depender del enum `PickupType` eliminado

**Nota sobre el enum `PickupType`:** Si se elimina junto con `ResolvedPickupType`, `CreateHeldVisual` necesita un reemplazo para el nombre del clon. Usar `definition.DrinkName` o simplemente `item.gameObject.name` sirve. [ASSUMED — el único uso del enum es en esa línea y en el campo `pickupType` del Inspector]

---

## Don't Hand-Roll

| Problema | No construir | Usar en cambio | Por qué |
|----------|-------------|----------------|---------|
| Datos compartidos entre instancias de prefab | Sistema de registro propio / dictionary estático de tipos | `ScriptableObject` con `[CreateAssetMenu]` | Unity serializa la referencia por GUID; el asset vive una sola vez en memoria |
| Persistencia entre `LoadSceneAsync(Single)` | `DontDestroyOnLoad` MonoBehaviour adicional | Store estático C# (`PlayerMoneyStore`, `HeldObjectStore`) | Ya es el patrón del proyecto; más simple, sin singleton management |
| SFX de rechazo | Audio custom system | `AudioSource.PlayOneShot()` + `Resources.Load` con fallback | Ya es el patrón de `PlayerPickup`; dos líneas de código |
| Highlight del mostrador | Shader personalizado | `MaterialPropertyBlock` + `_EmissionColor` | Ya funciona en `PickupItem` y `CarryableObject` |

**Insight clave:** Esta fase no requiere ninguna librería externa ni patrón nuevo. Toda la infraestructura (ScriptableObject, stores estáticos, MaterialPropertyBlock, AudioSource, Resources.Load) ya está en el proyecto.

---

## Valores Económicos Iniciales Propuestos (D-09)

Propuestos por Claude para los assets SO. Todos son tuneables desde el Inspector.

### DrinkDefinition — Precios y alcohol

| Asset | Precio ($) | AlcoholPerSip | MaxSips | Alcohol total |
|-------|-----------|---------------|---------|---------------|
| Cerveza.asset | 10 | 1 | 4 | 4 |
| Trago.asset | 20 | 2 | 3 | 6 |
| Whisky.asset | 35 | 3 | 2 | 6 |

**Lógica de balance:** Cerveza es la más barata y suave. Whisky da alcohol igual que Trago pero en menos sorbos (se emborracha más rápido por sorbo) y cuesta más. El jugador que quiera borrachera máxima con mínimo dinero combinará ambas. `DrunkManager.maxLevel = 24`, así que 4 whiskies (28 unidades) alcanza el tope.

### SellableDefinition — Valores de venta (Home)

El jugador tiene 6 CarryableObjects en Home.unity (verificado por grep). Propuesta de valores que dan al menos una compra de Whisky por objeto vendido:

| Tipo | Valor de venta ($) | Cuántas Cervezas | Cuántos Whiskies |
|------|--------------------|-----------------|-----------------|
| TV (valor alto) | 50 | 5 | 1 + cambio |
| Silla / Lámpara | 30 | 3 | 0 (necesita 2 ventas) |
| Cuadro / Adorno | 20 | 2 | — |

**Nota:** Los valores exactos por objeto dependen de cuáles son los 6 objetos en la escena. El plan debe incluir una tarea de "revisar escena Home y asignar SellableDefinition a cada CarryableObject".

---

## Common Pitfalls

### Pitfall 1: Guardar MonoBehaviour en lugar de ScriptableObject entre escenas
**Qué falla:** Si `HeldObjectStore` guarda `CarryableObject` en vez de `SellableDefinition`, la referencia queda inválida tras `LoadSceneAsync` (el objeto se destruye con la escena).
**Por qué ocurre:** Confusión entre asset (proyecto) y componente (escena).
**Cómo evitar:** Solo guardar en stores estáticos tipos que no sean MonoBehaviour ni GameObject: primitivos, structs, strings, y assets (`ScriptableObject`, `Texture2D`, etc.).
**Señales de alerta:** `MissingReferenceException` al intentar leer `HeldObjectStore.HeldDefinition` en la escena Bar.

### Pitfall 2: Eliminar ResolvedPickupType antes de re-cablear prefabs
**Qué falla:** Las 3 instancias de `PickupItem` en Bar.unity y los 3 prefabs quedan con el campo `definition` en `null`. Las bebidas no tienen precio ni alcohol.
**Por qué ocurre:** El compilador acepta el cambio de script, pero la serialización de los prefabs no tiene ningún `DrinkDefinition` asignado hasta que el usuario lo hace en el Inspector.
**Cómo evitar:** Secuencia de dos pasos: primero agregar campo + assets + cablear; recién entonces eliminar el código viejo.

### Pitfall 3: `hasHeldObject` estático sin `Clear()` — campo antiguo que persiste
**Qué falla:** `PlayerPickup.hasHeldObject` es un `static bool` sin `Clear()`. Si se migra a `HeldObjectStore` pero se olvida eliminar el campo estático, puede haber estado inconsistente.
**Por qué ocurre:** El campo existe como preocupación conocida en CONCERNS.md. La migración a `HeldObjectStore` lo reemplaza; hay que eliminar el campo estático en `PlayerPickup` y actualizar `HasHeldObject` y `ConsumeHeldObject()` para delegar al nuevo store.
**Cómo evitar:** En el mismo commit que se introduce `HeldObjectStore`, eliminar `static bool hasHeldObject` de `PlayerPickup` y actualizar `HasHeldObject` getter para leer `HeldObjectStore.HasHeldObject`.

### Pitfall 4: Detectar el mostrador desde PlayerPickup requiere un tipo reconocible
**Qué falla:** `PlayerPickup.UpdateSelectionByLook` sólo busca `PickupItem` y `CarryableObject` por `GetComponentInParent`. Si el mostrador es un GameObject genérico, el raycast no lo "selecciona".
**Por qué ocurre:** El flujo de selección es tipo-específico.
**Cómo evitar:** Dos opciones: (A) `PlayerPickup` agrega detección de `SellCounter` (igual que detecta `PickupItem`), con su propio `currentSellCounter`; (B) `SellCounter` tiene su propio raycast independiente. La opción A es más limpia para el highlight.

### Pitfall 5: `PickupItem.Awake` muta materiales compartidos
**Qué falla:** CONCERNS.md documenta que `renderers[i].materials` puede mutar el material del asset en el Editor.
**Impacto en esta fase:** Al agregar el campo `DrinkDefinition`, si se testea en Edit Mode con `ExecuteInEditMode`, puede corromper materiales.
**Cómo evitar:** No agregar `ExecuteInEditMode`; dejar que `Awake` corra sólo en Play Mode (comportamiento por defecto). No tocar ese código de `Awake` en esta fase.

### Pitfall 6: SFX de rechazo necesita un clip de audio real
**Qué falla:** No hay un archivo `NoMoney.mp3` (ni equivalente) en `Assets/Resources/Audio/SFX/`. El clip de rechazo no existe todavía.
**Opciones (D-10):** (A) Reusar `PayDrink.mp3` con pitch negativo como rechazo implícito; (B) agregar un clip nuevo `NoMoney.mp3`; (C) reusar `DrinkSip.mp3` — mínima diferencia perceptual pero funciona para MVP.
**Recomendación para el plan:** Incluir una tarea de "agregar o reusar clip SFX de rechazo". Si no hay asset de audio disponible, `Resources.Load` retornará `null` y el fallback es silencio (no es un error de compilación). Documentar en el comentario del script.

---

## Validation Architecture

> `workflow.nyquist_validation: true` en `.planning/config.json` — sección incluida.

### Contexto de testing del proyecto

No hay tests ni `.asmdef` en el proyecto. `com.unity.test-framework` está instalado pero sin configurar. Introducir tests completos en esta fase requeriría crear asmdef (lo que cambia el flujo de compilación del proyecto). Para este MVP académico, la validación es **manual en Play Mode** con criterios observables, complementada opcionalmente con EditMode tests para la lógica pura de los stores.

### Estrategia de validación recomendada

**Nivel 1 — Verificación manual en Play Mode (obligatoria, sin setup previo):**
Cada Success Criteria del ROADMAP se verifica manualmente al completar la fase.

| Req ID | Comportamiento a verificar | Cómo verificarlo en Play Mode |
|--------|---------------------------|-------------------------------|
| ECON-01 | `CarryableObject` tiene valor monetario configurable por Inspector | Seleccionar cualquier CarryableObject en Home.unity; el campo `SellableDefinition` debe mostrar el asset asignado con su `sellValue` visible |
| ECON-02 | Vender objeto en el bar aumenta el dinero y persiste al volver a City | (1) Agarrar objeto en Home, (2) ir al Bar, (3) vender en mostrador, (4) salir a City; en el Inspector de un script con acceso a `PlayerMoneyStore.Money` (o log `Debug.Log`) verificar que el valor aumentó |
| ECON-03 | Bebida con precio; no se puede tomar sin dinero suficiente | Con $0 en saldo intentar agarrar una bebida en el Bar; debe sonar SFX de rechazo y no tomarse |
| ECON-04 | Comprar descuenta precio; trueque ya no funciona | Después de vender objeto, comprar bebida; verificar que el dinero disminuyó el precio correcto; verificar que ya no es posible tomar sin dinero |
| PAT-01 | Flyweight: múltiples PickupItem apuntan al mismo DrinkDefinition asset | En el Inspector, dos bebidas del mismo tipo deben mostrar el mismo asset SO en el campo `definition` (mismo objeto, no copia) |

**Nivel 2 — Debug de desarrollo (recomendado durante implementación):**
Agregar teclas de debug (patrón existente en `DrunkManager.debugAddAlcoholKey`):

```csharp
// En PlayerPickup o en un script Debug separado
[Header("Debug")]
[SerializeField] private KeyCode debugAddMoneyKey = KeyCode.M;
[SerializeField] private int debugAddMoneyAmount = 50;

void Update()
{
    // ... código existente ...
    if (Input.GetKeyDown(debugAddMoneyKey))
    {
        PlayerMoneyStore.Add(debugAddMoneyAmount);
        Debug.Log($"[Debug] Dinero agregado. Saldo: ${PlayerMoneyStore.Money}");
    }
}
```

**Nivel 3 — EditMode tests opcionales (solo si se quiere coverage automatizado):**

Si el planner decide introducir asmdef, los stores estáticos (`PlayerMoneyStore`, `HeldObjectStore`) son candidatos ideales para unit tests porque son clases C# puras sin dependencia de MonoBehaviour:

```csharp
// Assets/Tests/EditMode/PlayerMoneyStoreTests.cs
// (Requiere crear asmdef + Test Runner en Window → General → Test Runner)
using NUnit.Framework;

public class PlayerMoneyStoreTests
{
    [SetUp] public void Setup() => PlayerMoneyStore.Clear();

    [Test] public void Add_IncreasesBalance()
    {
        PlayerMoneyStore.Add(50);
        Assert.AreEqual(50, PlayerMoneyStore.Money);
    }

    [Test] public void Spend_DecreasesBalance_WhenAffordable()
    {
        PlayerMoneyStore.Add(100);
        bool result = PlayerMoneyStore.Spend(30);
        Assert.IsTrue(result);
        Assert.AreEqual(70, PlayerMoneyStore.Money);
    }

    [Test] public void Spend_ReturnsFalse_WhenInsufficientFunds()
    {
        PlayerMoneyStore.Add(10);
        bool result = PlayerMoneyStore.Spend(50);
        Assert.IsFalse(result);
        Assert.AreEqual(10, PlayerMoneyStore.Money);
    }

    [Test] public void Clear_ResetsToZero()
    {
        PlayerMoneyStore.Add(200);
        PlayerMoneyStore.Clear();
        Assert.AreEqual(0, PlayerMoneyStore.Money);
    }
}
```

**Decisión para el planner:** Para MVP académico sin setup adicional, Level 1 (Play Mode manual) es suficiente. Level 3 es valioso pero requiere un Wave 0 de asmdef setup que podría diferirse.

### Wave 0 Gaps (si se opta por Level 3)
- [ ] Crear `Assets/Tests/EditMode/` con `.asmdef` referenciando `Unity.TestFramework`
- [ ] `PlayerMoneyStoreTests.cs` — cubre ECON-02, ECON-03, ECON-04
- [ ] `HeldObjectStoreTests.cs` — cubre D-03 (transporte de definición)
- [ ] Verificar Test Runner en Window → General → Test Runner → EditMode

*(Si no se hace Level 3: "None — validación manual cubre todos los criterios observables de la fase")*

---

## State of the Art

| Enfoque Antiguo | Enfoque Actual | Impacto |
|-----------------|----------------|---------|
| Resolver tipo de bebida por nombre de objeto (`string.Contains("cerveza")`) | Referencia explícita a `DrinkDefinition` SO | Elimina una categoría entera de bugs silenciosos (D-04) |
| Trueque directo (objeto ↔ bebida) | Economía con dinero (`PlayerMoneyStore`) | Desacopla el valor del objeto del tipo de bebida; habilita balance más rico |
| Campos hardcodeados en `PickupItem` (`beerAlcoholPerSip`, etc.) | Datos en `DrinkDefinition` SO | Un solo punto de verdad; tuneable sin recompilar |
| `static bool hasHeldObject` sin `Clear()` | `HeldObjectStore` con `Clear()` + definición | Fix del concern conocido en CONCERNS.md |

**Deprecado en esta fase:**
- `PickupItem.ResolvedPickupType` y el enum `PickupType` — reemplazados por referencia directa al SO
- `PickupItem.itemName` (para resolución de tipo) — puede quedarse como label opcional de display
- `PickupItem.beerAlcoholPerSip`, `cocktailAlcoholPerSip`, `whiskyAlcoholPerSip` — migran a `DrinkDefinition`
- `PlayerPickup.static bool hasHeldObject` — migra a `HeldObjectStore`

---

## Environment Availability

> Verificación de dependencias externas para esta fase.

| Dependencia | Requerida por | Disponible | Versión | Fallback |
|-------------|---------------|-----------|---------|----------|
| Unity Editor | Todo | ✓ | 6000.3.11f1 | — |
| URP | MaterialPropertyBlock highlight | ✓ | 17.3 | — |
| Assets de audio SFX de rechazo | D-10 | ✗ (archivo no existe) | — | Reusar `PayDrink.mp3` o silencio si null |

**Dependencias faltantes con fallback:**
- **SFX de rechazo:** No existe `NoMoney.mp3` ni equivalente en `Assets/Resources/Audio/SFX/`. El código usa `Resources.Load` con fallback a `null`; si es null, `PlayOneShot` no se llama (no es error). El plan debe incluir tarea de crear/agregar este archivo o reusar `PayDrink.mp3`.

**Dependencias faltantes sin fallback:** ninguna.

---

## Open Questions (RESOLVED)

> Las tres preguntas quedaron resueltas durante la planificación (ver 01-01-PLAN.md / 01-02-PLAN.md). Eran opciones de diseño, no incógnitas bloqueantes.

1. **¿El mostrador de venta detecta al jugador por raycast de `PlayerPickup` o por trigger propio?** — **(RESOLVED)** Se extiende `PlayerPickup` para detectar `SellCounter` (opción A), highlight consistente — Plan 01-01 Task 2 Paso C.
   - Lo que sabemos: `PlayerPickup.UpdateSelectionByLook` sólo busca `PickupItem` y `CarryableObject`.
   - Lo que no está claro: si el planner quiere highlight del mostrador (igual que los items), necesita extender `PlayerPickup`. Si no necesita highlight, `SellCounter` puede ser autónomo.
   - Recomendación: Extender `PlayerPickup` para detectar `SellCounter` (opción A) — da highlight consistente y evita duplicar lógica de raycast. El planner decide si esto entra en el scope de Wave 1 o Wave 2.

2. **¿Los 6 `CarryableObject` en Home.unity tienen `objectId` asignados o usan el StableId por jerarquía?** — **(RESOLVED)** El plan asigna strings de `objectId` únicos en el Inspector — Plan 01-01 Task 2 Paso D.
   - Lo que sabemos: CONCERNS.md documenta que el StableId por jerarquía puede colisionar si se reordenan objetos. Hay 6 instancias en la escena.
   - Recomendación: El plan debe incluir una tarea de "asignar `objectId` único a cada CarryableObject en Home.unity" como parte del setup. Previene el bug de colisión documentado.

3. **¿Eliminar `PickupType` enum completamente o mantenerlo como label?** — **(RESOLVED)** Se elimina el enum por completo — Plan 01-02 Task 1 Paso 7.
   - Lo que sabemos: El único uso de `ResolvedPickupType` post-migración es en `CreateHeldVisual` para nombrar el clon. Puede reemplazarse con `definition.DrinkName`.
   - Recomendación: Eliminar el enum por completo (D-04 lo establece explícitamente). Simplifica el código.

---

## Assumptions Log

| # | Claim | Sección | Riesgo si está mal |
|---|-------|---------|-------------------|
| A1 | Los 6 `CarryableObject` en Home.unity son objetos de mobiliario distintos (TV, lámpara, cuadro, etc.) | Valores Económicos Iniciales | Si son objetos idénticos, el balance propuesto puede no aplicar |
| A2 | El enum `PickupType` sólo se usa en `PickupItem.cs` y `PlayerPickup.CreateHeldVisual` | Migración de ResolvedPickupType | Si hay otro script que usa `PickupType`, eliminarlo causará error de compilación |
| A3 | El único uso de `item.ResolvedPickupType.ToString()` en `CreateHeldVisual` es para naming cosmético del clon | Migración de ResolvedPickupType | Bajo — el nombre del GameObject hijo no afecta gameplay |
| A4 | El mostrador de venta no existe aún en Bar.unity (sería un GameObject nuevo) | SellCounter | Si ya existe un objeto de barra/mostrador en la escena, puede reutilizarse |

---

## Project Constraints (from CLAUDE.md)

Directivas del CLAUDE.md que el plan debe respetar:

| Directiva | Impacto en Phase 1 |
|-----------|-------------------|
| Unity 6000.3.11f1 exactamente | No instalar paquetes que requieran versión distinta |
| Input API legacy (`UnityEngine.Input`, `KeyCode`) | `SellCounter` y modificaciones de `PlayerPickup` usan `Input.GetKeyDown(KeyCode.E)` |
| Namespace global (sin `namespace`) | `DrinkDefinition`, `SellableDefinition`, `PlayerMoneyStore`, `HeldObjectStore`, `SellCounter` — todos sin `namespace` |
| Comentarios e internos en español | Todos los `<summary>`, `[Tooltip]`, `Debug.Log`, `[Header]` en español |
| No editar `Library/`, `Temp/`, archivos generados | Los stores `.cs` van en `Assets/_Project/`, no en carpetas generadas |
| ScriptableObjects en `Assets/_Project/ScriptableObjects/Items/` | Destino natural ya identificado en el codebase (carpeta existe y está vacía) |
| Patrón `[SerializeField] private` para nuevos scripts | `DrinkDefinition`, `SellableDefinition`, `SellCounter` siguen esta convención |
| `[Header("...")]` + `[Tooltip("...")]` para agrupar campos | Aplicar en todos los SO y componentes nuevos |
| Patrón `Resources.Load` con fallback doble | SFX de rechazo en `SellCounter`/`PlayerPickup` sigue el patrón de DrinkSip/PayDrink |
| Guard clauses + no exceptions | `PlayerMoneyStore.Spend`, `HeldObjectStore.SetHeld` usan early return |
| Debug.Log con `[ClassName]` prefix | `[PlayerMoneyStore]`, `[HeldObjectStore]`, `[SellCounter]` |

---

## Sources

### Primarias (HIGH confidence)
- [Unity 6 Manual: ScriptableObject](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html) — `CreateAssetMenu`, referencia desde MonoBehaviour, datos compartidos
- [Unity Learn: Flyweight Pattern (Unity 6)](https://learn.unity.com/course/design-patterns-unity-6/tutorial/flyweight-pattern?version=6.0) — patrón SO como Flyweight, distinción intrínseco/extrínseco
- [Unity 6 Manual: Script Serialization Rules](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html) — `SerializeField` vs `SerializeReference` para `UnityEngine.Object`
- Codebase analizado directamente: `PlayerPickup.cs`, `PickupItem.cs`, `CarryableObject.cs`, `DeliveredObjectsStore.cs`, `CarStateStore.cs`, `BarDoorTrigger.cs`, `DrunkManager.cs`, `Bar.unity`, `Home.unity`

### Secundarias (MEDIUM confidence)
- [Unity Discussions: ScriptableObject serialization in prefabs](https://discussions.unity.com/t/scriptableobject-serialization-in-prefabs/554760) — comportamiento de serialización de referencias SO en prefabs
- [Game Dev Beginner: ScriptableObjects in Unity](https://gamedevbeginner.com/scriptable-objects-in-unity/) — patrón de shared data entre prefabs

### Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — todo el stack es ya del proyecto; sin dependencias externas
- Architecture Patterns: HIGH — code examples derivados directamente del codebase existente y docs oficiales Unity 6
- Migración de ResolvedPickupType: HIGH — todos los archivos relevantes leídos directamente; 3 prefabs + 3 instancias de escena confirmados
- Valores económicos: ASSUMED (por diseño — D-09 establece que son tuneables, Claude propone)
- Pitfalls: HIGH — tres de los seis pitfalls son concerns documentados en CONCERNS.md del propio proyecto

**Research date:** 2026-06-22
**Válido hasta:** 2026-07-22 (estable — Unity 6000.3.11f1, sin dependencias externas cambiantes)
