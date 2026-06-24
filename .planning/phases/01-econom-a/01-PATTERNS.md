# Phase 1: Economía - Pattern Map

**Mapeado:** 2026-06-22
**Archivos analizados:** 8 (3 nuevos, 2 nuevos stores, 1 nuevo componente, 2 modificaciones)
**Análogos encontrados:** 8 / 8

---

## File Classification

| Archivo nuevo/modificado | Rol | Flujo de datos | Análogo más cercano | Calidad |
|--------------------------|-----|---------------|---------------------|---------|
| `Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs` | model (SO Flyweight) | transform | `Assets/_Project/Core/Managers/DrunkManager.cs` (patrón `[Header]` + `[SerializeField] private`) | role-match |
| `Assets/_Project/ScriptableObjects/Items/SellableDefinition.cs` | model (SO Flyweight) | transform | Igual que DrinkDefinition | role-match |
| `Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs` | store (util estático) | CRUD | `Assets/_Project/Core/SceneManagement/CarStateStore.cs` | exact |
| `Assets/_Project/Core/SceneManagement/HeldObjectStore.cs` | store (util estático) | CRUD | `Assets/_Project/Core/SceneManagement/CarStateStore.cs` + campo `static bool hasHeldObject` de `PlayerPickup.cs` | exact |
| `Assets/_Project/Gameplay/Items/SellCounter.cs` | component (interacción) | request-response | `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` (trigger con colisión) + `PlayerPickup.cs` (raycast + E + AudioSource) | role-match |
| `Assets/_Project/Gameplay/Items/PickupItem.cs` (modificar) | component (item) | transform | sí mismo (ya leído) — migrar campos a SO | exact |
| `Assets/_Project/Gameplay/Items/CarryableObject.cs` (modificar) | component (item) | CRUD | sí mismo (ya leído) — agregar referencia a SO | exact |
| `Assets/_Project/Gameplay/Player/PlayerPickup.cs` (modificar) | component (player input) | request-response | sí mismo (ya leído) — reemplazar trueque por compra | exact |

---

## Pattern Assignments

### `DrinkDefinition.cs` (ScriptableObject, transform)

**Análogo:** `Assets/_Project/Core/Managers/DrunkManager.cs` (patrón de campos `[Header]` + `[SerializeField] private` con properties de solo lectura)

No existe ningún ScriptableObject en el proyecto todavía — la carpeta `Assets/_Project/ScriptableObjects/Items/` está vacía. El análogo de estructura más cercano es `DrunkManager.cs`, que muestra la convención de `[Header]` + `[SerializeField] private` + properties `=>`.

**Patrón de campos serializados** (DrunkManager.cs líneas 6-17):
```csharp
[Header("Config")]
[SerializeField] private int maxLevel = 24;
[SerializeField] private float effectExponent = 1.6f;

[Header("Debug")]
[SerializeField] private KeyCode debugAddAlcoholKey = KeyCode.G;
[SerializeField] private int debugAddAlcoholAmount = 1;

[Header("Runtime")]
[SerializeField] private int alcoholLevel = 0;

public int AlcoholLevel => alcoholLevel;
public int MaxLevel => maxLevel;
```

**Patrón early-return (guard clause)** (DrunkManager.cs líneas 34-35):
```csharp
public void AddAlcohol(int amount)
{
    if (amount <= 0) return;
    // ...
}
```

**Estructura a copiar para DrinkDefinition:**
```csharp
// Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs
// Sin namespace — convención global del proyecto (CONVENTIONS.md)

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

---

### `SellableDefinition.cs` (ScriptableObject, transform)

**Análogo:** mismo que `DrinkDefinition.cs` — `DrunkManager.cs` patrón `[Header]`/`[SerializeField] private`/property `=>`

**Estructura a copiar para SellableDefinition:**
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

---

### `PlayerMoneyStore.cs` (store estático, CRUD)

**Análogo:** `Assets/_Project/Core/SceneManagement/CarStateStore.cs` — coincidencia exacta de rol y estructura.

**Patrón de store estático completo** (CarStateStore.cs líneas 1-28):
```csharp
using UnityEngine;

/// <summary>
/// Guarda la posicion/rotacion del auto entre cargas de escena (Single mode).
/// ...
/// </summary>
public static class CarStateStore
{
    public static bool HasSavedState { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Save(Transform car)
    {
        if (car == null) return;     // guard clause
        Position = car.position;
        Rotation = car.rotation;
        HasSavedState = true;
    }

    /// <summary>Olvida el estado guardado (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        HasSavedState = false;
    }
}
```

**Patrón de IDs con HashSet** (DeliveredObjectsStore.cs líneas 1-35) — para variante con colección:
```csharp
using System.Collections.Generic;

public static class DeliveredObjectsStore
{
    static readonly HashSet<string> takenIds = new HashSet<string>();

    public static void MarkTaken(string id)
    {
        if (string.IsNullOrEmpty(id)) return;   // guard clause en string
        takenIds.Add(id);
    }

    public static bool IsTaken(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return takenIds.Contains(id);
    }

    public static void Clear() { takenIds.Clear(); }
}
```

**Estructura a copiar para PlayerMoneyStore** — combinar ambos: properties `{ get; private set; }` de `CarStateStore` con guard clauses de `DeliveredObjectsStore`:
```csharp
// Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs
// Sin using adicionales — no requiere UnityEngine ni colecciones

/// <summary>
/// Guarda el dinero del jugador entre cargas de escena.
/// Se resetea al cerrar el juego o al llamar Clear() (Nueva partida).
/// Sigue el patron de CarStateStore y DeliveredObjectsStore.
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

    /// <summary>Descuenta el monto. Retorna false si no hay saldo suficiente.</summary>
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
    public static void Clear() { Money = 0; }
}
```

---

### `HeldObjectStore.cs` (store estático, CRUD)

**Análogo primario:** `Assets/_Project/Core/SceneManagement/CarStateStore.cs` — estructura de properties + `Clear()`.
**Análogo secundario:** `PlayerPickup.cs` líneas 33-40 — el campo `static bool hasHeldObject` que este store reemplaza.

**Campo estático que se migra** (PlayerPickup.cs líneas 33-40):
```csharp
static bool hasHeldObject;

public bool HasHeldObject => hasHeldObject;

public void ConsumeHeldObject()
{
    hasHeldObject = false;
}
```

**Estructura a copiar para HeldObjectStore** — extiende `CarStateStore` con la referencia al asset SO (válida entre escenas porque los SOs son assets del proyecto, no objetos de escena):
```csharp
// Assets/_Project/Core/SceneManagement/HeldObjectStore.cs

/// <summary>
/// Recuerda si el jugador lleva un objeto en mano y cual es su definicion,
/// de manera que la informacion persista al cambiar de escena.
/// Reemplaza el campo estatico hasHeldObject de PlayerPickup.
/// </summary>
public static class HeldObjectStore
{
    public static bool HasHeldObject { get; private set; }
    public static SellableDefinition HeldDefinition { get; private set; }
    public static string HeldObjectId { get; private set; }

    /// <summary>Registra que el jugador agarro un objeto.</summary>
    public static void SetHeld(SellableDefinition definition, string stableId)
    {
        if (definition == null) return;     // guard clause — analog: CarStateStore.Save null check
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

**Nota crítica de integración:** `SellableDefinition` es un asset ScriptableObject (no MonoBehaviour), por lo que sobrevive a `SceneManager.LoadSceneAsync(Single)` sin DontDestroyOnLoad — mismo motivo por el que `CarStateStore` guarda `Vector3`/`Quaternion` (tipos valor) en vez de referencias de escena.

---

### `SellCounter.cs` (component, request-response)

**Análogo primario de estructura:** `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` — `[RequireComponent(typeof(Collider))]`, `[Header("Config")]`, `[SerializeField] private`, guard clauses en el método de interacción.

**Análogo del flujo de audio:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs` — `AudioSource` + `Resources.Load` con ruta de fallback (líneas 64-87).

**Patrón de componente con collider requerido** (BarDoorTrigger.cs líneas 1-32):
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class BarDoorTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string sceneToLoad = "Bar";
    [SerializeField] private string playerTag = "player";

    private bool triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }
    // ...
}
```

**Patrón AudioSource + Resources.Load con fallback doble** (PlayerPickup.cs líneas 64-87):
```csharp
sfxSource = gameObject.GetComponent<AudioSource>();
if (sfxSource == null)
{
    sfxSource = gameObject.AddComponent<AudioSource>();
}
sfxSource.playOnAwake = false;

if (drinkSipClip == null)
{
    drinkSipClip = Resources.Load<AudioClip>("Audio/SFX/DrinkSip");
    if (drinkSipClip == null)
    {
        drinkSipClip = Resources.Load<AudioClip>("Audio/DrinkSip");
    }
}

if (payDrinkClip == null)
{
    payDrinkClip = Resources.Load<AudioClip>("Audio/SFX/PayDrink");
    if (payDrinkClip == null)
    {
        payDrinkClip = Resources.Load<AudioClip>("Audio/PayDrink");
    }
}
```

**Patrón PlayOneShot** (PlayerPickup.cs líneas 225-228):
```csharp
if (payDrinkClip != null && sfxSource != null)
{
    sfxSource.PlayOneShot(payDrinkClip, Mathf.Clamp01(payDrinkVolume));
}
```

**Patrón de detección del SellCounter desde PlayerPickup** — copiar el patrón de `UpdateSelectionByLook` (PlayerPickup.cs líneas 130-178). Actualmente busca `PickupItem` y `CarryableObject`. Agregar `SellCounter` al mismo pipeline:
```csharp
// En PlayerPickup.UpdateSelectionByLook — sección de raycast (líneas 141-148)
Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
if (Physics.Raycast(ray, out RaycastHit hit, selectionDistance, pickupLayerMask, QueryTriggerInteraction.Collide))
{
    currentPickupItem = hit.collider.GetComponentInParent<PickupItem>();
    if (currentPickupItem == null)
    {
        currentCarryable = hit.collider.GetComponentInParent<CarryableObject>();
    }
    // AGREGAR: detección de SellCounter con el mismo patrón GetComponentInParent
}
```

**Debug.Log con prefijo de clase** — patrón del proyecto (DrunkManager implícito, BarDoorTrigger no tiene, PlayerPickup no tiene, pero RESEARCH.md exige prefijo):
```csharp
Debug.Log($"[SellCounter] Objeto vendido por ${valor}. Saldo: ${PlayerMoneyStore.Money}");
```

---

### `PickupItem.cs` — modificaciones (component, transform)

**Analog:** sí mismo — los campos a migrar están en líneas 12-18; las properties derivadas en líneas 43-58.

**Campos actuales a reemplazar** (PickupItem.cs líneas 3-18):
```csharp
public enum PickupType { Trago, Cerveza, Whisky }

public class PickupItem : MonoBehaviour
{
    public PickupType pickupType = PickupType.Trago;
    public string itemName = "";
    public bool infiniteSupply = true;
    public int maxSips = 4;
    public int beerAlcoholPerSip = 1;
    public int cocktailAlcoholPerSip = 2;
    public int whiskyAlcoholPerSip = 3;
    // ...
}
```

**Propiedad a migrar** (PickupItem.cs líneas 28-41 — `ResolvedPickupType` por nombre):
```csharp
public PickupType ResolvedPickupType
{
    get
    {
        string lookupName = string.IsNullOrWhiteSpace(itemName) ? gameObject.name : itemName;
        string lowerName = lookupName.ToLowerInvariant();
        if (lowerName.Contains("cerveza")) return PickupType.Cerveza;
        if (lowerName.Contains("whisky")) return PickupType.Whisky;
        if (lowerName.Contains("trago")) return PickupType.Trago;
        return pickupType;
    }
}
```

**Uso de ResolvedPickupType a reemplazar** (PlayerPickup.cs línea 266 — `CreateHeldVisual`):
```csharp
clone.name = item.ResolvedPickupType.ToString();
// -> reemplazar con: clone.name = item.definition != null ? item.definition.DrinkName : item.gameObject.name;
```

**Properties actuales de `AlcoholPerSip`** (PickupItem.cs líneas 43-58) — reemplazar switch por delegación al SO:
```csharp
public int AlcoholPerSip
{
    get
    {
        switch (ResolvedPickupType) { ... }    // eliminar esto
    }
}
// -> reemplazar con: public int AlcoholPerSip => definition != null ? definition.AlcoholPerSip : 0;
```

**Patrón de property delegada al SO** — copiar el mismo estilo que `CarStateStore`:
```csharp
// Campos nuevos a agregar (con [Header] y [SerializeField] — patrón DrunkManager)
[Header("Definicion")]
[SerializeField] private DrinkDefinition definition;

// Properties delegadas al SO (con fallback a 0/1 si null — guard clause)
public int Price => definition != null ? definition.Price : 0;
public int AlcoholPerSip => definition != null ? definition.AlcoholPerSip : 0;
public int MaxSips => definition != null ? definition.MaxSips : 1;
```

---

### `CarryableObject.cs` — modificaciones (component, CRUD)

**Analog:** sí mismo — agregar campo + property, sin cambiar la lógica existente de highlight ni StableId.

**Campo y método a modificar** (CarryableObject.cs líneas 82-88):
```csharp
public void OnPickedUp()
{
    // Lo agarraste: su lugar en la casa queda vacio para siempre (esta partida).
    DeliveredObjectsStore.MarkTaken(StableId);   // MOVER esta línea a SellCounter.TrySell()
    SetHighlighted(false);
    gameObject.SetActive(false);
}
```

**Campo nuevo a agregar** (patrón `[SerializeField] private` de DrunkManager):
```csharp
[Header("Definicion")]
[SerializeField] private SellableDefinition definition;

public SellableDefinition Definition => definition;
public int SellValue => definition != null ? definition.SellValue : 0;
```

**OnPickedUp migrado** — reemplazar `DeliveredObjectsStore.MarkTaken` por `HeldObjectStore.SetHeld`:
```csharp
public void OnPickedUp()
{
    // Registrar en el store el objeto sostenido (definición + id estable).
    // NO marcar como entregado aquí — se marca al vender en SellCounter.
    HeldObjectStore.SetHeld(definition, StableId);
    SetHighlighted(false);
    gameObject.SetActive(false);
}
```

---

### `PlayerPickup.cs` — modificaciones (component, request-response)

**Analog:** sí mismo — reemplazar la rama del trueque en `Update` y `PickupCarryable`.

**Rama del trueque actual a eliminar** (PlayerPickup.cs líneas 100-103):
```csharp
else if (currentPickupItem != null && hasHeldObject)
{
    hasHeldObject = false;
    Pickup(currentPickupItem);
}
```

**Nueva rama de compra con dinero** — reemplazar la anterior usando `PlayerMoneyStore.CanAfford` (patrón guard clause):
```csharp
else if (currentPickupItem != null)
{
    int precio = currentPickupItem.Price;
    if (!PlayerMoneyStore.CanAfford(precio))
    {
        // SFX de rechazo (D-10) — mismo patrón que payDrinkClip/drinkSipClip
        if (rejectClip != null && sfxSource != null)
            sfxSource.PlayOneShot(rejectClip, Mathf.Clamp01(rejectVolume));
        return;
    }
    PlayerMoneyStore.Spend(precio);
    Pickup(currentPickupItem);
}
```

**Rama de CarryableObject a modificar** (PlayerPickup.cs líneas 105-108):
```csharp
else if (currentCarryable != null && !hasHeldObject)
{
    PickupCarryable(currentCarryable);
}
// -> cambiar la condición a: !HeldObjectStore.HasHeldObject
```

**Método PickupCarryable a modificar** (PlayerPickup.cs líneas 203-211):
```csharp
void PickupCarryable(CarryableObject carryable)
{
    if (carryable == null) return;
    carryable.OnPickedUp();
    hasHeldObject = true;               // -> eliminar; ahora lo hace HeldObjectStore.SetHeld()
    currentCarryable = null;
    lastHighlightedCarryable = null;
}
```

**Campo estático a eliminar** (PlayerPickup.cs líneas 33-40):
```csharp
static bool hasHeldObject;             // -> eliminar; migra a HeldObjectStore
public bool HasHeldObject => hasHeldObject;             // -> delegar a HeldObjectStore.HasHeldObject
public void ConsumeHeldObject() { hasHeldObject = false; }    // -> delegar a HeldObjectStore.Clear()
```

**Campos de audio nuevos a agregar** — copiar patrón de `drinkSipClip`/`payDrinkClip` (PlayerPickup.cs líneas 17-20):
```csharp
[Header("Audio")]
[SerializeField] private AudioClip drinkSipClip;
[SerializeField, Range(0f, 1f)] private float drinkSipVolume = 1f;
[SerializeField] private AudioClip payDrinkClip;
[SerializeField, Range(0f, 1f)] private float payDrinkVolume = 1f;
// AGREGAR:
[SerializeField] private AudioClip rejectClip;
[SerializeField, Range(0f, 1f)] private float rejectVolume = 1f;
```

**Carga de rejectClip en Awake** — copiar exactamente el patrón de fallback doble (PlayerPickup.cs líneas 71-87):
```csharp
if (rejectClip == null)
{
    rejectClip = Resources.Load<AudioClip>("Audio/SFX/NoMoney");
    if (rejectClip == null)
    {
        rejectClip = Resources.Load<AudioClip>("Audio/SFX/PayDrink"); // fallback al existente
    }
}
```

---

## Shared Patterns

### Patrón: Store estático en memoria (cross-scene state)
**Fuente:** `Assets/_Project/Core/SceneManagement/CarStateStore.cs` + `DeliveredObjectsStore.cs`
**Aplicar a:** `PlayerMoneyStore.cs`, `HeldObjectStore.cs`

Reglas extraídas directamente del código:
1. `public static class` sin herencia de MonoBehaviour
2. Properties con `{ get; private set; }` — estado solo modificable desde dentro
3. Métodos de mutación con guard clause como primera línea (`if (x == null) return;`)
4. `Clear()` público para "Nueva partida" — presente en ambos stores existentes
5. Sin `using UnityEngine` si no se usan tipos de Unity (DeliveredObjectsStore no lo importa)

### Patrón: [Header] + [SerializeField] private + property `=>`
**Fuente:** `Assets/_Project/Core/Managers/DrunkManager.cs` (líneas 6-21)
**Aplicar a:** `DrinkDefinition.cs`, `SellableDefinition.cs`, `SellCounter.cs`, campos nuevos en `PickupItem.cs` y `CarryableObject.cs`

```csharp
[Header("Config")]
[SerializeField] private int campo = valorDefault;

public int Campo => campo;
```

### Patrón: AudioSource + Resources.Load con fallback doble
**Fuente:** `Assets/_Project/Gameplay/Player/PlayerPickup.cs` (líneas 64-87)
**Aplicar a:** `SellCounter.cs` (clip de venta), `PlayerPickup.cs` (rejectClip)

```csharp
// Awake:
sfxSource = gameObject.GetComponent<AudioSource>();
if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
sfxSource.playOnAwake = false;

if (clip == null)
{
    clip = Resources.Load<AudioClip>("Audio/SFX/NombreClip");
    if (clip == null)
        clip = Resources.Load<AudioClip>("Audio/NombreClip");  // ruta de fallback
}

// Uso:
if (clip != null && sfxSource != null)
    sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumen));
```

### Patrón: Guard clause en métodos de interacción
**Fuente:** `CarStateStore.Save` (línea 16), `DeliveredObjectsStore.MarkTaken` (línea 18), `DrunkManager.AddAlcohol` (línea 33)
**Aplicar a:** todos los métodos públicos de los nuevos stores y del `SellCounter.TrySell`

```csharp
public static void Metodo(Tipo param)
{
    if (param == null) return;   // guard clause — no throw, no exception
    // lógica...
}
```

### Patrón: [RequireComponent] en componentes que necesitan Collider
**Fuente:** `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` (línea 4) + `HomeDoorTrigger.cs` (línea 4)
**Aplicar a:** `SellCounter.cs`

```csharp
[RequireComponent(typeof(Collider))]
public class SellCounter : MonoBehaviour { ... }
```

### Patrón: MaterialPropertyBlock highlight
**Fuente:** `Assets/_Project/Gameplay/Items/PickupItem.cs` (líneas 61-93) + `CarryableObject.cs` (líneas 39-80)
**Aplicar a:** `SellCounter.cs` si se quiere highlight del mostrador (opcional — misma implementación que en PickupItem/CarryableObject)

```csharp
// Awake:
renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
propertyBlock = new MaterialPropertyBlock();
foreach (var r in renderers)
    foreach (var mat in r.materials)
    {
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

// SetHighlighted:
Color emission = highlighted ? highlightColor : Color.black;
foreach (var r in renderers)
{
    r.GetPropertyBlock(propertyBlock);
    propertyBlock.SetColor("_EmissionColor", emission);
    r.SetPropertyBlock(propertyBlock);
}
```

---

## Advertencias de migración

### Orden de cambios para PickupItem (evitar pérdida de datos en prefabs)

El RESEARCH.md documenta el riesgo en detalle. El orden seguro extraído es:

1. Crear `DrinkDefinition.cs` + assets `.asset` (sin tocar scripts existentes)
2. Agregar campo `[SerializeField] private DrinkDefinition definition;` a `PickupItem.cs` — Unity recompila, prefabs siguen funcionando con `definition == null`
3. Cablear en el Inspector de cada prefab (Whisky, Cerveza, Trago) y 3 instancias en Bar.unity
4. Agregar properties delegadas al SO (`Price`, `AlcoholPerSip`, `MaxSips`)
5. Verificar en Play Mode
6. Recién ahora eliminar `ResolvedPickupType`, enum `PickupType`, campos hardcodeados

### Migración de hasHeldObject

Eliminar `static bool hasHeldObject` de `PlayerPickup.cs` en el mismo commit que se introduce `HeldObjectStore`. No dejar los dos en paralelo — generaría estado inconsistente (ver Pitfall 3 en RESEARCH.md).

---

## No Analog Found

Ningún archivo de esta fase queda sin análogo — todos los patrones necesarios están representados en el codebase existente.

| Archivo | Razón |
|---------|-------|
| — | Todos los archivos tienen análogo directo |

---

## Metadata

**Scope de búsqueda de análogos:** `Assets/_Project/` (25 archivos .cs)
**Archivos escaneados:** 8 leídos completos (`DeliveredObjectsStore.cs`, `CarStateStore.cs`, `PickupItem.cs`, `CarryableObject.cs`, `PlayerPickup.cs`, `BarDoorTrigger.cs`, `HomeDoorTrigger.cs`, `DrunkManager.cs`)
**Fecha de extracción de patrones:** 2026-06-22
