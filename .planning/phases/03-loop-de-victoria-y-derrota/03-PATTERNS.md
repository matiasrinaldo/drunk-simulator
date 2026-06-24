# Phase 3: Loop de victoria y derrota - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 12 (7 nuevos + 5 modificaciones)
**Analogs found:** 12 / 12

---

## File Classification

| Archivo nuevo / modificado | Rol | Flujo de datos | Análogo más cercano | Calidad |
|---|---|---|---|---|
| `Assets/_Project/Gameplay/Vehicles/LethalObstacle.cs` | componente-marcador | ninguno (datos puros) | `Assets/_Project/Core/SceneManagement/SpawnPoint.cs` | role-match |
| `Assets/_Project/Core/SceneManagement/GameResultStore.cs` | store estático | estado entre escenas | `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` | exacto |
| `Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs` | store estático | estado entre escenas | `Assets/_Project/Core/SceneManagement/CarStateStore.cs` | exacto |
| `Assets/_Project/Core/Managers/GameManager.cs` | manager (MonoBehaviour) | request-response | `Assets/_Project/Core/Managers/DrunkManager.cs` | exacto |
| `Assets/_Project/UI/Screens/ResultScreenController.cs` | controller de UI (MonoBehaviour) | request-response | `Assets/_Project/UI/HUD/HUDController.cs` | role-match |
| `Assets/Scenes/Result.unity` | escena Unity | — | `Assets/Scenes/Bar.unity` (escena de resultado diferida) | estructural |
| `Assets/_Project/Gameplay/Vehicles/CarController.cs` (mod) | vehículo / detección de colisión | event-driven | propio archivo — agregar `OnCollisionEnter` | self-analog |
| `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` (mod) | trigger de escena | request-response | `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` | exacto |
| `Assets/_Project/UI/HUD/HUDController.cs` (mod) | singleton persistente | event-driven | propio archivo — agregar `SetVisible` | self-analog |
| `Assets/_Project/Core/SceneManagement/DeliveredObjectsStore.cs` (mod) | store estático | estado entre escenas | propio archivo — agregar `TakenCount` | self-analog |
| `Assets/_Project/Editor/CityBuilder.cs` (mod) | editor tool | batch / generación procedural | propio archivo — agregar `AddComponent<LethalObstacle>` en `PlaceBuilding`/`PlaceTree` | self-analog |
| `HomeInitializer.cs` (nuevo, en escena Home) | inicializador por escena | event-driven (Awake) | `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` | role-match |

---

## Pattern Assignments

### `Assets/_Project/Gameplay/Vehicles/LethalObstacle.cs` (componente-marcador)

**Análogo:** `Assets/_Project/Core/SceneManagement/SpawnPoint.cs` — componente marcador sin lógica, solo datos serializados.

**Patrón de imports** (líneas 1-2 de SpawnPoint.cs — sin using, solo UnityEngine):
```csharp
using UnityEngine;
```

**Patrón de componente marcador** — clase mínima, campo serializado, propiedad pública:
```csharp
// SpawnPoint.cs — patrón a replicar
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id;
    public string Id => id;
}
```

**Patrón a aplicar en LethalObstacle** (con enum en mismo archivo, namespace global):
- Declarar el enum `ObstacleCategory` en el mismo archivo, antes de la clase.
- Un solo campo `[SerializeField] private ObstacleCategory category`.
- Una propiedad pública `public ObstacleCategory Category => category;`.
- Sin Awake ni Update: este componente es solo datos.

---

### `Assets/_Project/Core/SceneManagement/GameResultStore.cs` (store estático)

**Análogo:** `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` — store estático con valor tipado, getter privado, `Save`/`Clear`.

**Patrón completo** (DrunkLevelStore.cs, líneas 1-27):
```csharp
using UnityEngine;

public static class DrunkLevelStore
{
    /// <summary>Nivel de alcohol persistido entre escenas.</summary>
    public static int AlcoholLevel { get; private set; } = 0;

    /// <summary>Guarda el nivel actual para que sobreviva la proxima carga de escena.</summary>
    public static void Save(int level)
    {
        AlcoholLevel = Mathf.Max(0, level);
    }

    /// <summary>Resetea el nivel a cero (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        AlcoholLevel = 0;
    }
}
```

**Adaptación para GameResultStore:**
- Reemplazar `int AlcoholLevel` por `GameResult Result` (tipo enum `GameResult { None, Victory, Defeat }`).
- Renombrar `Save` por `Set`.
- No necesita `using UnityEngine` (no usa `Mathf`).
- El enum se declara en el mismo archivo, antes de la clase.
- `Clear()` setea `Result = GameResult.None`.
- No disparar eventos: los consumidores leen el store en `Start()` de la escena Result.

---

### `Assets/_Project/Core/SceneManagement/HomeObjectsTotalStore.cs` (store estático)

**Análogo:** `Assets/_Project/Core/SceneManagement/CarStateStore.cs` — store estático con datos primitivos, `HasSavedState` como guard, `Clear()`.

**Patrón completo** (CarStateStore.cs, líneas 1-28):
```csharp
using UnityEngine;

public static class CarStateStore
{
    public static bool HasSavedState { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Save(Transform car)
    {
        if (car == null) return;
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

**Adaptación para HomeObjectsTotalStore:**
- Un solo campo `int Total` (get; private set) con valor inicial `0`.
- Método `Set(int total)` con guard `if (total >= 0)`.
- `Clear()` setea `Total = 0`.
- No necesita `using UnityEngine`.

---

### `Assets/_Project/Core/Managers/GameManager.cs` (manager MonoBehaviour)

**Análogo:** `Assets/_Project/Core/Managers/DrunkManager.cs` — MonoBehaviour manager central con `[Header]`, `[SerializeField]`, descubrimiento por `FindFirstObjectByType`, sin namespace.

**Patrón de imports y declaración de clase** (DrunkManager.cs, líneas 1-5):
```csharp
using System;
using UnityEngine;

public class DrunkManager : MonoBehaviour
{
```

**Patrón de campos serializados con Header** (DrunkManager.cs, líneas 7-15):
```csharp
    [Header("Config")]
    [SerializeField] private int maxLevel = 24;
    [SerializeField] private float effectExponent = 1.6f;

    [Header("Debug")]
    [SerializeField] private KeyCode debugAddAlcoholKey = KeyCode.G;

    [Header("Runtime")]
    [SerializeField] private int alcoholLevel = 0;
```

**Patrón de Awake con restauración de estado persistido** (DrunkManager.cs, líneas 25-29):
```csharp
    void Awake()
    {
        // Restaurar el nivel persistido para que la borrachera sea continua entre
        // escenas (la escena se reconstruye en modo Single y resetearia el campo).
        alcoholLevel = Mathf.Min(DrunkLevelStore.AlcoholLevel, maxLevel);
    }
```

**Patrón de método público con guard clause** (DrunkManager.cs, líneas 40-47):
```csharp
    public void AddAlcohol(int amount)
    {
        if (amount <= 0) return;

        alcoholLevel = Mathf.Min(alcoholLevel + amount, maxLevel);
        DrunkLevelStore.Save(alcoholLevel);
        OnAlcoholLevelChanged?.Invoke(alcoholLevel);
    }
```

**Adaptación para GameManager:**
- `[Header("Condicion de victoria")]` con `[SerializeField] private int minAlcoholRequired = 6;`.
- Métodos públicos `OnPlayerArrivedHome()` y `OnCarCrash()`.
- `OnPlayerArrivedHome()` lee `HomeObjectsTotalStore.Total`, `DeliveredObjectsStore.TakenCount`, `DrunkLevelStore.AlcoholLevel`, llama `GameResultStore.Set(...)` y `SceneManager.LoadSceneAsync(...)`.
- `OnCarCrash()` llama `GameResultStore.Set(GameResult.Defeat)` + `SceneManager.LoadSceneAsync("Result")`.
- `NewGame()` puede ser `public static void` en el mismo archivo o clase separada — centraliza los `Clear()` de todos los stores y carga Home.
- Requiere `using UnityEngine.SceneManagement;`.

---

### `Assets/_Project/UI/Screens/ResultScreenController.cs` (controller de UI)

**Análogo:** `Assets/_Project/UI/HUD/HUDController.cs` — MonoBehaviour con TMP, suscripción a eventos, construcción de UI, `FindFirstObjectByType`.

**Patrón de imports** (HUDController.cs, líneas 1-6):
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
```

**Patrón de búsqueda de manager con fallback null-safe** (HUDController.cs, línea 158):
```csharp
    drunkManager = FindFirstObjectByType<DrunkManager>();
    if (drunkManager != null)
    {
        drunkManager.OnAlcoholLevelChanged += HandleAlcoholChanged;
        targetFillAmount = drunkManager.NormalizedLevel;
    }
```

**Patrón de gestión de cursor** — no existe en HUDController; aplicar en `Start()`:
```csharp
    // Mostrar cursor (D-10)
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
    Time.timeScale = 1f;
```

**Patrón de botón con listener por código** — consistente con estilo del proyecto (no usar onClick en Inspector):
```csharp
    retryButton.onClick.AddListener(OnRetry);
    quitButton.onClick.AddListener(OnQuit);
```

**Adaptación para ResultScreenController:**
- NO usa `DontDestroyOnLoad` ni `[RuntimeInitializeOnLoadMethod]` — vive solo en la escena Result.
- En `Start()`: leer `GameResultStore.Result`, setear textos TMP, registrar listeners de botones, ocultar HUD (`FindFirstObjectByType<HUDController>()?.gameObject.SetActive(false)`).
- Botón Reintentar llama a `GameManager.NewGame()` (o `NewGame.Start()` si es clase separada).
- Botón Salir llama a `Application.Quit()`.
- Usar `[SerializeField]` para las refs de `TMP_Text` y `Button`, asignadas desde el Inspector de la escena.

---

### `HomeInitializer.cs` (nuevo, colocado en escena Home)

**Análogo:** `Assets/_Project/Core/SceneManagement/PlayerSpawner.cs` — MonoBehaviour de inicialización por escena en `Start()`/`Awake()`, usa `FindObjectsByType`, guarda en store estático.

**Patrón completo** (PlayerSpawner.cs, líneas 1-37):
```csharp
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public static string NextSpawnId;

    void Start()
    {
        if (string.IsNullOrEmpty(NextSpawnId)) return;

        SpawnPoint target = null;
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.Id == NextSpawnId)
            {
                target = sp;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"[PlayerSpawner] No SpawnPoint found with id '{NextSpawnId}'");
            NextSpawnId = null;
            return;
        }
        // ...
        NextSpawnId = null;
    }
}
```

**Adaptación para HomeInitializer:**
- En `Awake()` (no `Start()`, para que el total esté disponible antes de cualquier pickup).
- Guard: `if (HomeObjectsTotalStore.Total > 0) return;` — captura el total solo la primera vez.
- Llamar `FindObjectsByType<CarryableObject>(FindObjectsSortMode.None).Length`.
- Llamar `HomeObjectsTotalStore.Set(count)`.
- `Debug.Log` con prefijo `[HomeInitializer]`.

---

### Modificación: `CarController.cs` — agregar `OnCollisionEnter`

**Self-analog:** El archivo ya tiene el patrón de guard clause con `isControlled` en `Update` y `FixedUpdate` (líneas 70-75 y 83):
```csharp
    void Update()
    {
        if (!isControlled)
        {
            throttleInput = 0f;
            steerInput = 0f;
            return;
        }
        // ...
    }
```

**Patrón de freno de emergencia** ya usado en `Start()` (líneas 54-55):
```csharp
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
```

**Patrón de FindFirstObjectByType con fallback** (línea 42):
```csharp
    drunkManager = FindFirstObjectByType<DrunkManager>();
```

**Patrón a aplicar para OnCollisionEnter:**
```csharp
    private void OnCollisionEnter(Collision collision)
    {
        if (!isControlled) return;
        LethalObstacle obstacle = collision.gameObject.GetComponent<LethalObstacle>();
        if (obstacle == null) return;

        // Frenar antes de disparar la transicion (D-03)
        rb.linearVelocity    = Vector3.zero;
        rb.angularVelocity   = Vector3.zero;
        SetControlled(false);

        // Notificar al GameManager (patron FindFirstObjectByType con fallback)
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.OnCarCrash();
    }
```

---

### Modificación: `CityHomeDoorTrigger.cs` — delegar a GameManager

**Análogo:** `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` — trigger idéntico en estructura; `CityHomeDoorTrigger` es su clon con SaveCar extra.

**Patrón actual completo** (CityHomeDoorTrigger.cs, líneas 19-32):
```csharp
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        // Recordamos donde quedo estacionado el auto antes de descargar la City.
        var car = FindFirstObjectByType<CarController>();
        if (car != null) CarStateStore.Save(car.transform);

        SceneManager.LoadSceneAsync(sceneToLoad);
    }
```

**Patrón a aplicar — mínima invasión (D-04, Pitfall 3):**
Mover `triggered = true` y el `LoadSceneAsync` al final del handler; setear `triggered` solo cuando se confirma la transición:
```csharp
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        // Recordamos donde quedo estacionado el auto antes de descargar la City.
        var car = FindFirstObjectByType<CarController>();
        if (car != null) CarStateStore.Save(car.transform);

        // D-04: delegar al GameManager la decision de escena
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            triggered = true;
            gm.OnPlayerArrivedHome();
            return;
        }

        // Fallback: comportamiento anterior si no hay GameManager
        triggered = true;
        SceneManager.LoadSceneAsync(sceneToLoad);
    }
```

---

### Modificación: `HUDController.cs` — agregar `SetVisible`

**Self-analog:** Patrón de instancia estática con null-check (HUDController.cs, líneas 17, 34-37):
```csharp
    private static HUDController instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        // ...
    }
```

**Método a agregar:**
```csharp
    /// <summary>Muestra u oculta el HUD (usado por ResultScreenController).</summary>
    public static void SetVisible(bool visible)
    {
        if (instance != null) instance.gameObject.SetActive(visible);
    }
```

---

### Modificación: `DeliveredObjectsStore.cs` — agregar `TakenCount`

**Self-analog:** Estructura existente (líneas 14, 17-34):
```csharp
    static readonly HashSet<string> takenIds = new HashSet<string>();

    public static void MarkTaken(string id) { ... }
    public static bool IsTaken(string id) { ... }
    public static void Clear() { takenIds.Clear(); }
```

**Propiedad a agregar — una línea, después de `IsTaken`:**
```csharp
    /// <summary>Cantidad de objetos tomados en esta partida.</summary>
    public static int TakenCount => takenIds.Count;
```

---

### Modificación: `CityBuilder.cs` — agregar `LethalObstacle` en PlaceBuilding y PlaceTree

**Self-analog:** El patrón de instanciación ya hace `go.name = ...`, `go.transform.position = ...` al final de `PlaceBuilding` (líneas 195-198) y `PlaceTree` (líneas 238-239). El componente se agrega después de setear transform.

**Patrón a replicar (PlaceBuilding, líneas 183-199):**
```csharp
    static void PlaceBuilding(Transform parent, string goName, string fbxName,
                              Vector3 pos, float yRot)
    {
        // ...instanciacion existente...
        go.name                   = goName;
        go.transform.position     = pos;
        go.transform.rotation     = Quaternion.Euler(0f, yRot, 0f);
        go.transform.localScale   = Vector3.one * BuildingScale;
        // AGREGAR AL FINAL:
        go.AddComponent<LethalObstacle>(); // categoria default = Casa (primer enum)
    }
```

**Para PlaceTree (líneas 238-239):**
```csharp
    go.name               = $"Tree_{(side < 0 ? "W" : "E")}_{Mathf.RoundToInt(z)}";
    go.transform.position = new Vector3(xPos, 0f, z);
    // AGREGAR AL FINAL:
    var lo = go.AddComponent<LethalObstacle>();
    // la.category no es public; si LethalObstacle expone SetCategory o el campo es interno,
    // usarlo; sino dejar default (Casa) y renombrar: la categoria Arbol requiere que el campo
    // sea accesible desde CityBuilder o que LethalObstacle exponga un constructor con categoria.
    // Solucion preferida: field con default = Arbol en la sobrecarga de PlaceTree,
    // o exponer: public void SetCategory(ObstacleCategory c) { category = c; }
```

---

## Patrones Compartidos (cross-cutting)

### Store estático — estructura canónica del proyecto

**Fuente:** `Assets/_Project/Core/SceneManagement/DrunkLevelStore.cs` (más simple) y `Assets/_Project/Core/SceneManagement/CarStateStore.cs` (con bool de estado).

**Aplicar a:** `GameResultStore.cs`, `HomeObjectsTotalStore.cs`.

Estructura obligatoria:
```csharp
// Sin 'namespace'
// Comentario XML en español sobre el propósito
// using mínimos (solo los necesarios)
public static class XxxStore
{
    // Propiedades con { get; private set; }
    // Método Set/Save con guard clause
    // Método Clear() con comentario "util al empezar una partida nueva"
}
```

---

### Guard clause con `FindFirstObjectByType` + null-check

**Fuente:** `Assets/_Project/Core/Managers/DrunkManager.cs` línea 42 y `Assets/_Project/Core/SceneManagement/CityHomeDoorTrigger.cs` línea 28.

**Aplicar a:** `CarController.OnCollisionEnter` (GameManager), `ResultScreenController.Start` (HUDController), `HomeInitializer.Awake` (ninguno extra).

```csharp
var target = FindFirstObjectByType<SomeManager>();
if (target != null) target.DoSomething();
```

---

### Trigger de escena — estructura canónica

**Fuente:** `Assets/_Project/Core/SceneManagement/BarDoorTrigger.cs` (líneas 1-32, el más limpio).

**Aplicar a:** cualquier trigger nuevo en City para Home o Result.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class XxxTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string sceneToLoad = "NombreEscena";
    [SerializeField] private string playerTag = "player";

    private bool triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;
        // lógica específica...
        SceneManager.LoadSceneAsync(sceneToLoad);
    }
}
```

---

### Convenciones de código (aplicar a todos los archivos nuevos)

- Sin `namespace`.
- Campos privados `[SerializeField] private`.
- `[Header("...")]` para agrupar campos en el Inspector (en español).
- `[Tooltip("...")]` en campos configurables clave.
- Comentarios en español, XML `/// <summary>` en métodos públicos.
- Input legacy (`UnityEngine.Input`, `KeyCode`) — ResultScreenController no usa input, pero si algún debug-key se agrega, usar este API.
- `Debug.Log($"[NombreClase] ...")` para trazas, con prefijo de clase entre corchetes.
- Guard clauses al inicio de métodos (`if (x == null) return;`).

---

## Archivos sin análogo externo

Ninguno. Todos los archivos tienen análogo directo en el codebase:

| Archivo | Razón de no necesitar búsqueda externa |
|---|---|
| `Assets/Scenes/Result.unity` | Escena Unity — se crea desde el Editor, no por código. Patrón estructural: igual a `Bar.unity` / `Home.unity`. |

---

## Metadata

**Alcance de búsqueda de análogos:** `Assets/_Project/Core/`, `Assets/_Project/Gameplay/`, `Assets/_Project/UI/`, `Assets/_Project/Editor/`
**Archivos leídos:** 13 (10 scripts + 2 docs + 1 RESEARCH verificado)
**Fecha de extracción:** 2026-06-24
