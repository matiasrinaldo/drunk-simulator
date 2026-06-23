# Phase 2: HUD in-level - Research

**Researched:** 2026-06-22
**Domain:** Unity uGUI (Canvas / Image / TextMeshProUGUI), singleton persistente entre escenas, binding de eventos en tiempo real
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** El HUD es un singleton que sobrevive las cargas de escena vía `DontDestroyOnLoad`, auto-arrancado con `[RuntimeInitializeOnLoadMethod]` — mismo patrón que `BackgroundMusicManager`. Se instancia una sola vez; no se agrega un Canvas a cada escena.
- **D-02:** Como `DrunkManager` es un objeto por-escena (no persiste), el HUD debe re-vincularse al `DrunkManager` activo en cada carga de escena, suscribiéndose a `SceneManager.sceneLoaded` y re-resolviendo con `FindFirstObjectByType<DrunkManager>()` (con fallback si todavía no existe en ese frame). El dinero no tiene este problema: `PlayerMoneyStore` es estático y siempre está disponible.
- **D-03:** Agregar un evento estático `OnMoneyChanged` a `PlayerMoneyStore` (`Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs`) que disparan `Add`, `Spend` y `Clear`. El HUD se suscribe a ese evento (no hace polling). El HUD también debe leer el valor actual al suscribirse / al cargar la escena.
- **D-04:** La barra refleja `DrunkManager.EffectIntensity` (la curva no lineal `pow(NormalizedLevel, effectExponent)`). NO usar `NormalizedLevel` lineal.
- **D-05:** El fill se suaviza con lerp hacia el valor objetivo cada frame. Implementación como `Image` con `type = Filled` (fill horizontal), no Slider. Empieza vacía (alcohol 0) y se llena al tomar.
- **D-06:** Canvas en Screen Space - Overlay (cámara-agnóstico: se ve igual en FPS y en modo auto sin reconfigurar por cámara).
- **D-07:** Barra de borrachera + texto de dinero agrupados en la esquina inferior izquierda, estilo minimalista. Texto de dinero con TextMeshPro (TMP disponible vía `com.unity.ugui 2.0.0`).

### Claude's Discretion

- Colores exactos, tipografía, tamaños, y formato del texto de dinero (p. ej. `$50`) quedan a criterio de implementación dentro del estilo minimalista en esquina inferior izquierda (acotados por la UI-SPEC).
- Umbrales visuales opcionales de la barra (p. ej. cambio de color al acercarse al máximo) son opcionales; no son requisito de la fase.

### Deferred Ideas (OUT OF SCOPE)

None — la discusión se mantuvo dentro del scope de la fase.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Descripción | Soporte de investigación |
|----|-------------|--------------------------|
| HUD-01 | Barra de borrachera visible durante el juego que refleja el nivel actual de alcohol | `Image type=Filled` lerpeando hacia `DrunkManager.EffectIntensity`; HUD singleton via `DontDestroyOnLoad` |
| HUD-02 | Indicador de dinero visible que se actualiza al vender objetos y comprar bebidas | `TextMeshProUGUI` (TMP ya importado); evento `OnMoneyChanged` en `PlayerMoneyStore` |

</phase_requirements>

---

## Summary

Esta fase agrega el HUD de lectura en tiempo real: una barra de borrachera y un indicador de dinero. No agrega mecánicas — es visualización pura. Todo el código existente ya tiene los hooks correctos (`DrunkManager.OnAlcoholLevelChanged`, `PlayerMoneyStore.Add/Spend/Clear`) y solo falta (a) agregar `OnMoneyChanged` a `PlayerMoneyStore`, (b) crear el singleton HUD con `[RuntimeInitializeOnLoadMethod]` que instancia un prefab de Canvas, y (c) implementar el binding con re-vínculo al `DrunkManager` en cada `sceneLoaded`.

El desafío no es técnico sino de plumbing: el HUD es DontDestroyOnLoad pero el `DrunkManager` no lo es. Cada vez que `SceneManager.LoadSceneAsync(Single)` reconstruye la escena, el `DrunkManager` antiguo es destruido y uno nuevo es creado. El HUD debe detectar este ciclo y re-suscribirse — ese es el patrón clave de D-02.

La dependencia con TMP es la única con riesgo de setup: el paquete `com.unity.ugui 2.0.0` incluye TMP en su source, la carpeta `Assets/TextMesh Pro/` tiene la estructura de directorios pero los assets esenciales (LiberationSans SDF, TMP Settings) aún NO han sido importados al proyecto. El plan necesita un Wave 0 que importe los TMP Essential Resources antes de crear el prefab del Canvas.

**Recomendación primaria:** Dos scripts, un prefab, una modificación a un store existente. Wave 0 = importar TMP Essentials + crear prefab en editor; Wave 1 = agregar `OnMoneyChanged` a `PlayerMoneyStore` + implementar `HUDController` singleton; Wave 2 = cablear el prefab con el script y validar en Play Mode en las 3 escenas.

---

## Architectural Responsibility Map

| Capacidad | Tier Principal | Tier Secundario | Justificación |
|-----------|----------------|-----------------|---------------|
| Canvas persistente del HUD | Singleton MonoBehaviour `HUDController` (DontDestroyOnLoad) | — | Debe sobrevivir recargas de escena; Screen Space Overlay no requiere cámara específica |
| Barra de borrachera (fill) | `HUDController.Update` (lerp por frame) | `DrunkManager.EffectIntensity` (fuente de verdad) | El lerp es visual; el valor objetivo viene del manager per-escena |
| Re-vínculo al DrunkManager | `HUDController` vía `SceneManager.sceneLoaded` | `FindFirstObjectByType<DrunkManager>()` | DrunkManager es per-escena; el HUD necesita redescubrirlo tras cada carga |
| Indicador de dinero (texto) | `HUDController` suscripto a `PlayerMoneyStore.OnMoneyChanged` | `PlayerMoneyStore` (store estático) | El store ya persiste entre escenas; solo hay que agregar el evento |
| Evento `OnMoneyChanged` | `PlayerMoneyStore` (modificación al store existente) | — | Consistente con el patrón de `DrunkManager.OnAlcoholLevelChanged`; el HUD no debe pollear |
| Prefab del Canvas | Asset en `Assets/_Project/UI/HUD/` | `HUDController.Bootstrap()` lo instancia | Un solo prefab; el Bootstrap lo carga con `Resources.Load` |

---

## Standard Stack

### Core (ya presente en el proyecto — no instalar nada)

| Componente | Versión | Propósito | Por qué es el estándar |
|-----------|---------|-----------|------------------------|
| Unity Engine | 6000.3.11f1 | Runtime principal | Fijo por el proyecto |
| `com.unity.ugui` | 2.0.0 | Canvas, Image, CanvasScaler, GraphicRaycaster | Ya declarado en manifest.json; incluye TMP en ugui 2.0.0 [VERIFIED: Packages/manifest.json] |
| `TextMeshProUGUI` | TMP bundled in ugui 2.0.0 | Texto de dinero con SDF rendering | Mismo package; sin instalar nada adicional [VERIFIED: codebase grep] |
| `Image type=Filled` | uGUI nativo | Barra de borrachera | API nativa; controla `fillAmount` (0→1) directamente [VERIFIED: codebase] |
| `CanvasScaler` | uGUI nativo | Escala 1920×1080 reference resolution | Necesario para que el HUD se vea igual en cualquier resolución [VERIFIED: UI-SPEC] |
| `SceneManager.sceneLoaded` | UnityEngine.SceneManagement | Re-vínculo al DrunkManager por escena | Evento de Unity; se dispara después de que la escena nueva está en memoria [ASSUMED] |
| `[RuntimeInitializeOnLoadMethod]` | UnityEngine | Bootstrap del HUD antes de la primera escena | Mismo atributo que `BackgroundMusicManager`; ya funciona en este proyecto [VERIFIED: codebase] |

### No hay packages nuevos para esta fase

Todo lo necesario está ya en el proyecto. No se instala ni actualiza ningún paquete.

## Package Legitimacy Audit

> No aplica para esta fase — no se instalan packages externos. Todos los componentes son de Unity nativo (`com.unity.ugui 2.0.0`) ya declarados en `manifest.json`.

---

## Architecture Patterns

### Diagrama de flujo del HUD

```
Bootstrap (RuntimeInitializeOnLoadMethod)
        |
        v
HUDController (DontDestroyOnLoad)
  |              |
  |              +-- sceneLoaded --> FindFirstObjectByType<DrunkManager>()
  |                                   |
  |                             re-subscribe a OnAlcoholLevelChanged
  |                             (opcional: limpiar suscripcion anterior)
  |
  +-- Update (cada frame)
  |     |
  |     +-- lerp(fillAmount, EffectIntensity, speed*dt)
  |
  +-- OnMoneyChanged(int) <-- PlayerMoneyStore.OnMoneyChanged
        |
        +-- moneyText.text = $"{newAmount}"
```

```
PlayerPickup / SellCounter
        |
        +-- PlayerMoneyStore.Add(valor)
        |       |
        |       +-- OnMoneyChanged?.Invoke(Money)  <-- [NUEVO D-03]
        |
        +-- PlayerMoneyStore.Spend(precio)
                |
                +-- OnMoneyChanged?.Invoke(Money)  <-- [NUEVO D-03]
```

### Recommended Project Structure

```
Assets/_Project/
├── UI/
│   └── HUD/
│       ├── HUDController.cs          # MonoBehaviour singleton: lerp fill, suscripción eventos
│       └── HUDCanvas.prefab          # Canvas (Screen Space Overlay) + HUDGroup + MoneyText + DrunkBar
├── Core/
│   └── SceneManagement/
│       └── PlayerMoneyStore.cs       # MODIFICAR: agregar event Action<int> OnMoneyChanged
│
Assets/Resources/
│   └── UI/
│       └── HUDCanvas.prefab          # O cargar desde Resources; ver patrón de carga abajo
```

> Nota sobre carga del prefab: El singleton `HUDController` puede (a) cargar el prefab desde `Resources.Load<GameObject>("UI/HUDCanvas")` al igual que `BackgroundMusicManager` carga el clip de música, o (b) llevar el Canvas como hijo del propio GameObject del HUDController (sin prefab separado). Opción (a) es más limpia para producción; opción (b) es más simple para MVP. Se recomienda (b) para esta fase: el script construye la jerarquía de UI por código en `Awake`, sin prefab externo, igual que `BackgroundMusicManager` crea el `AudioSource` por código.

### Pattern 1: Singleton HUD con RuntimeInitializeOnLoadMethod

**Qué hace:** Instancia el HUDController antes de que cargue la primera escena. Garantiza que haya exactamente un HUD durante toda la sesión de juego.

**Cuándo usarlo:** Cualquier objeto que deba existir en todas las escenas y no pertenecer a ninguna escena concreta.

**Ejemplo (basado en BackgroundMusicManager existente):**
```csharp
// Source: Assets/_Project/Core/Audio/BackgroundMusicManager.cs (patrón a replicar)
public class HUDController : MonoBehaviour
{
    private static HUDController instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject(nameof(HUDController));
        instance = go.AddComponent<HUDController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Construir jerarquía de UI por código (ver Pattern 2)
        BuildHUD();

        // Suscribirse al dinero (el store estático siempre existe)
        PlayerMoneyStore.OnMoneyChanged += HandleMoneyChanged;
        HandleMoneyChanged(PlayerMoneyStore.Money); // estado inicial

        // Suscribirse a carga de escenas para re-resolver DrunkManager
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        PlayerMoneyStore.OnMoneyChanged -= HandleMoneyChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (drunkManager != null)
            drunkManager.OnAlcoholLevelChanged -= HandleAlcoholChanged;
    }
}
```

### Pattern 2: Re-vínculo al DrunkManager por escena

**Qué hace:** Cada vez que una escena termina de cargarse, el HUD busca el `DrunkManager` nuevo y se re-suscribe. El manager per-escena anterior ya fue destruido por `LoadSceneAsync(Single)`.

**Cuándo usarlo:** Cualquier objeto DontDestroyOnLoad que dependa de un MonoBehaviour per-escena.

```csharp
// Source: patrón D-02 de CONTEXT.md + convención FindFirstObjectByType del proyecto
private DrunkManager drunkManager;
private float targetFillAmount;

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    // Desvincular manager anterior (ya destruido, pero limpiar suscripcion)
    if (drunkManager != null)
        drunkManager.OnAlcoholLevelChanged -= HandleAlcoholChanged;

    drunkManager = FindFirstObjectByType<DrunkManager>();

    if (drunkManager != null)
    {
        drunkManager.OnAlcoholLevelChanged += HandleAlcoholChanged;
        // Actualizar el target inmediatamente con el valor actual de la nueva escena
        targetFillAmount = drunkManager.EffectIntensity;
    }
    // Si DrunkManager no existe en esta escena (p.ej. escena sin alcohol),
    // la barra mantiene su fillAmount actual (no se resetea ni da error).
}

private void HandleAlcoholChanged(int newLevel)
{
    // El evento avisa que algo cambio; recalculamos el target desde EffectIntensity
    if (drunkManager != null)
        targetFillAmount = drunkManager.EffectIntensity;
}

private void Update()
{
    // Lerp suave hacia el target (D-05)
    if (fillImage != null)
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, 3f * Time.deltaTime);
}
```

### Pattern 3: Evento OnMoneyChanged en PlayerMoneyStore

**Qué hace:** Agregar un evento estático al store para que el HUD se entere de cambios sin pollear.

**Cuándo usarlo:** Cada vez que el estado de un store estático necesita notificar a observers.

```csharp
// Source: patrón a agregar en PlayerMoneyStore.cs (D-03)
public static class PlayerMoneyStore
{
    public static event Action<int> OnMoneyChanged;  // NUEVO

    public static int Money { get; private set; } = 0;

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        Debug.Log($"[PlayerMoneyStore] +${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);  // NUEVO
    }

    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;
        Money -= amount;
        Debug.Log($"[PlayerMoneyStore] -${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);  // NUEVO
        return true;
    }

    public static void Clear()
    {
        Money = 0;
        OnMoneyChanged?.Invoke(Money);  // NUEVO
    }
    // ... resto sin cambios
}
```

### Pattern 4: Construcción de la jerarquía de UI por código

**Qué hace:** El singleton construye el Canvas y sus hijos en `Awake`, sin depender de un prefab separado en Resources. Consistent con cómo `BackgroundMusicManager` crea su `AudioSource` por código.

```csharp
// Source: patrón del proyecto (BackgroundMusicManager.Awake crea AudioSource por código)
private Image fillImage;
private TMP_Text moneyText;

private void BuildHUD()
{
    // Canvas raíz
    Canvas canvas = gameObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 100;

    CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;

    gameObject.AddComponent<GraphicRaycaster>();

    // Grupo HUD (anchor bottom-left)
    GameObject group = new GameObject("HUDGroup");
    group.transform.SetParent(gameObject.transform, false);
    RectTransform groupRect = group.AddComponent<RectTransform>();
    groupRect.anchorMin = Vector2.zero;
    groupRect.anchorMax = Vector2.zero;
    groupRect.pivot = Vector2.zero;
    groupRect.anchoredPosition = new Vector2(24f, 24f);

    // Texto de dinero
    GameObject moneyGO = new GameObject("MoneyText");
    moneyGO.transform.SetParent(group.transform, false);
    moneyText = moneyGO.AddComponent<TextMeshProUGUI>();
    moneyText.text = "$0";
    moneyText.fontSize = 28f;
    moneyText.fontStyle = FontStyles.Bold;
    moneyText.color = Color.white;
    // RectTransform del texto (por encima de la barra; gap 8px bajo este)
    RectTransform moneyRect = moneyGO.GetComponent<RectTransform>();
    moneyRect.anchorMin = new Vector2(0f, 1f);
    moneyRect.anchorMax = new Vector2(0f, 1f);
    moneyRect.pivot = new Vector2(0f, 1f);
    moneyRect.anchoredPosition = Vector2.zero;
    moneyRect.sizeDelta = new Vector2(220f, 35f);

    // Track de la barra (fondo semitransparente)
    GameObject trackGO = new GameObject("Track");
    trackGO.transform.SetParent(group.transform, false);
    Image trackImage = trackGO.AddComponent<Image>();
    trackImage.color = new Color(0f, 0f, 0f, 0.45f);
    RectTransform trackRect = trackGO.GetComponent<RectTransform>();
    trackRect.anchorMin = new Vector2(0f, 0f);
    trackRect.anchorMax = new Vector2(0f, 0f);
    trackRect.pivot = new Vector2(0f, 0f);
    // Posicionar 8px bajo el texto + altura del texto
    trackRect.anchoredPosition = new Vector2(0f, 0f);
    trackRect.sizeDelta = new Vector2(220f, 20f);

    // Fill de la barra (image filled horizontal)
    GameObject fillGO = new GameObject("Fill");
    fillGO.transform.SetParent(trackGO.transform, false);
    fillImage = fillGO.AddComponent<Image>();
    fillImage.color = new Color(0.878f, 0.690f, 0.251f, 1f); // #E0B040 amber
    fillImage.type = Image.Type.Filled;
    fillImage.fillMethod = Image.FillMethod.Horizontal;
    fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    fillImage.fillAmount = 0f;
    RectTransform fillRect = fillGO.GetComponent<RectTransform>();
    fillRect.anchorMin = Vector2.zero;
    fillRect.anchorMax = Vector2.one;
    fillRect.offsetMin = Vector2.zero;
    fillRect.offsetMax = Vector2.zero;
}
```

> Nota: La construcción por código garantiza que el HUD no requiera un prefab en `Resources/` y no depende de un drag-and-drop manual. Es el mismo principio que BackgroundMusicManager. Un Wave 0 de la fase puede crear el prefab en el Editor vía Unity MCP si se prefiere tener el asset persistido en disco; pero para MVP el enfoque por código es autocontenido.

### Anti-Patterns to Avoid

- **Canvas per-escena:** Agregar un Canvas a cada escena (City.unity, Bar.unity, Home.unity) rompe la persistencia y duplica el HUD. El Canvas debe vivir en el singleton DontDestroyOnLoad, NO en las escenas.
- **Polling de dinero en Update:** Llamar a `PlayerMoneyStore.Money` en cada frame es ineficiente y puede introducir un frame de delay. Usar el evento `OnMoneyChanged` (D-03).
- **Usar Slider en vez de Image.type=Filled:** El componente `Slider` incluye lógica de interacción (pointer, drag) que el HUD no necesita. `Image.type=Filled` es más liviano y directo.
- **No limpiar suscripciones en OnSceneLoaded:** Si el HUD no des-suscribe el evento del DrunkManager anterior antes de re-suscribirse al nuevo, el callback cuelga de un objeto destruido. En Unity un objeto destruido no ejecuta callbacks C#, pero la referencia queda sucia; limpiar siempre en `OnSceneLoaded`.
- **Olvidar leer el estado inicial:** Al suscribirse a `OnMoneyChanged` o al re-vincular el DrunkManager, el HUD debe leer el valor actual en ese momento (no esperar al próximo cambio). Si no, el texto de dinero muestra `$0` hasta la próxima compra/venta aunque el jugador ya tenga dinero al recargar.
- **DontDestroyOnLoad sin guard de instancia:** Sin `if (instance != null && instance != this) { Destroy(gameObject); return; }` en `Awake`, cada vez que Unity recarga el singleton (en Edge Cases como recargar la escena desde el Editor en Play Mode) se crean instancias duplicadas.

---

## Don't Hand-Roll

| Problema | No construir | Usar en cambio | Por qué |
|----------|-------------|----------------|---------|
| Barra de progreso visual | Slider con handle custom, custom mesh | `Image.type=Filled` (uGUI nativo) | Tiene fill amount 0→1 nativo, sin lógica de interacción innecesaria |
| Texto con sombra/outline | Shader custom, segundo texto desplazado | TMP outline material (material SDF con `outlineWidth`) | Más performante (SDF distance field); sin artefactos a escala |
| Escalar HUD a resoluciones distintas | Cálculos manuales de Screen.width/height | `CanvasScaler` con `ScaleWithScreenSize` | El CanvasScaler ajusta el Canvas transform automáticamente |
| Detectar carga de escena | Coroutine que pollea escenas | `SceneManager.sceneLoaded` event | Es el API oficial de Unity para este caso; garantiza que la escena esté lista |

---

## State of the Art

| Enfoque antiguo | Enfoque actual (Unity 6 / ugui 2.0.0) | Cuándo cambió | Impacto |
|-----------------|---------------------------------------|---------------|---------|
| `TextMesh` component (legacy) | `TextMeshProUGUI` (TMP) en Canvas | Unity 2018+ | TMP es el estándar; `TextMesh` no tiene SDF ni CanvasScaler support |
| `Slider` para barras HUD | `Image.type=Filled` | Siempre disponible, pero ahora idiomático | Slider incluye interactividad innecesaria; Image.Filled es más liviano |
| TMP como package separado (`com.unity.textmeshpro`) | TMP bundled en `com.unity.ugui 2.0.0` | Unity 6 (2023+) | En Unity 6, TMP ya no es un package separado — viene en ugui 2.0.0 [CITED: Packages/manifest.json + packages-lock.json del proyecto] |
| Canvas en cada escena para HUD | Singleton `DontDestroyOnLoad` con `RuntimeInitializeOnLoadMethod` | Patrón establecido en este proyecto (BackgroundMusicManager) | Evita duplicados, garantiza persistencia entre escenas Single-mode |

**Deprecated/outdated:**
- `TextMesh` component: No usar en proyectos nuevos. Siempre usar `TextMeshProUGUI` en un Canvas o `TextMeshPro` en World Space.
- `com.unity.textmeshpro` como package independiente: En Unity 6 con `com.unity.ugui 2.0.0`, TMP está incorporado. No agregar la dependencia separada.
- `FindObjectOfType` (sin `First`): Reemplazado por `FindFirstObjectByType` en Unity 2023+/Unity 6. El proyecto ya usa la versión correcta.

---

## Common Pitfalls

### Pitfall 1: TMP Essentials no importados — error en runtime al crear TextMeshProUGUI por código

**Qué pasa:** Si se crea un `TextMeshProUGUI` por código sin haber importado los TMP Essential Resources (que incluyen el `LiberationSans SDF` font asset y el `TMP Settings` asset), Unity lanza advertencias y el texto no se renderiza.

**Por qué pasa:** En este proyecto, la carpeta `Assets/TextMesh Pro/` tiene la estructura de directorios creada pero vacía — los subdirectorios `Fonts/`, `Resources/`, `Sprites/` existen como carpetas sin contenido. Los TMP Essential Resources deben importarse explícitamente desde `Window > TextMeshPro > Import TMP Essential Resources`. El unitypackage existe en `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Package Resources/TMP Essential Resources.unitypackage` pero aún no fue extraído. [VERIFIED: codebase — find Assets/TextMesh Pro -type f retorna solo .meta files]

**Cómo evitar:** Wave 0 del plan debe incluir: `Window > TextMeshPro > Import TMP Essential Resources`. Esto pobla `Assets/TextMesh Pro/Resources/` con el `LiberationSans SDF` asset y `TMP Settings`. Solo es necesario una vez por proyecto. Después de importar, guardar con `Ctrl+S` y commitear los assets generados.

**Señales de alerta:** El texto TMP aparece como bloque negro o no se ve; Unity emite `TMP Essential Resources not found`.

### Pitfall 2: DrunkManager null en el primer frame tras sceneLoaded

**Qué pasa:** `SceneManager.sceneLoaded` se dispara cuando la escena terminó de cargarse, pero los `Awake()` de los GameObjects recién cargados se ejecutan antes del `sceneLoaded` callback. En principio, el `DrunkManager.Awake()` ya corrió cuando `HUDController.OnSceneLoaded` lo busca — pero si hay frames de diferencia por orden de inicialización, `FindFirstObjectByType<DrunkManager>()` puede retornar null en escenas que no tengan `DrunkManager` (como posibles escenas futuras de menú).

**Por qué pasa:** No todas las escenas garantizan un `DrunkManager`. Bar, City y Home tienen uno (revisado en el código existente: `PlayerPickup` y `CarFollowCamera` lo buscan via `FindFirstObjectByType`), pero el HUD no debe asumir que siempre habrá uno.

**Cómo evitar:** Null guard explícito tras `FindFirstObjectByType`. Si `drunkManager == null`, el HUD mantiene `targetFillAmount` en su último valor sin cambiar. No se muestra error ni se lanza excepción (patrón del proyecto: guard clauses con early return). [VERIFIED: codebase — CONVENTIONS.md "No exceptions thrown"]

**Señales de alerta:** `NullReferenceException` en `HUDController.OnSceneLoaded`.

### Pitfall 3: Doble suscripción al DrunkManager si OnSceneLoaded se llama dos veces

**Qué pasa:** Si `SceneManager.LoadSceneAsync` se llama dos veces rápido (edge case de transición), `OnSceneLoaded` puede dispararse dos veces y suscribir `HandleAlcoholChanged` al mismo `DrunkManager` dos veces, duplicando el callback.

**Por qué pasa:** C# `event` permite múltiples suscripciones del mismo delegado.

**Cómo evitar:** Siempre des-suscribir antes de re-suscribir: `drunkManager.OnAlcoholLevelChanged -= HandleAlcoholChanged` antes de `+= HandleAlcoholChanged`. El guard `if (drunkManager != null)` aplica antes de des-suscribir. [VERIFIED: patrón estándar C#]

**Señales de alerta:** La barra de borrachera sube el doble de rápido de lo esperado tras dos cargas de escena.

### Pitfall 4: OnGUI (aim dot) y Canvas coexistiendo — no es un conflicto

**Qué pasa (no):** `PlayerPickup.OnGUI()` dibuja el aim dot con `GUI.DrawTexture`. Podría parecer que este sistema IMGUI conflictúa con el Canvas uGUI.

**Realidad:** IMGUI (`OnGUI`) y uGUI (Canvas) son sistemas ortogonales en Unity; pueden coexistir sin problemas. El Canvas HUD se renderiza en Screen Space Overlay; el IMGUI se dibuja sobre todo lo demás. El aim dot seguirá visible y no interferirá con el HUD. [VERIFIED: codebase — PlayerPickup.cs líneas 151-167]

**Acción necesaria:** Ninguna. Documentado para que el implementador no "solucione" algo que no está roto.

### Pitfall 5: Memory leak si HUDController se destruye antes de des-suscribir

**Qué pasa:** Si por alguna razón el objeto del HUDController es destruido (solo puede ocurrir si se llama `Destroy()` explícitamente, ya que DontDestroyOnLoad previene la destrucción normal), los delegates `PlayerMoneyStore.OnMoneyChanged` y `SceneManager.sceneLoaded` quedan con referencias colgantes.

**Cómo evitar:** Implementar `OnDestroy()` que des-suscriba todos los eventos. [VERIFIED: patrón estándar; ya implementado en el ejemplo de Pattern 1]

---

## Code Examples

### Agregar OnMoneyChanged a PlayerMoneyStore (D-03)

```csharp
// Source: Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs (modificacion a agregar)
using System;   // necesario para Action<int>

public static class PlayerMoneyStore
{
    /// <summary>Se dispara cuando el saldo cambia. Argumento: nuevo saldo.</summary>
    public static event Action<int> OnMoneyChanged;

    public static int Money { get; private set; } = 0;

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        Debug.Log($"[PlayerMoneyStore] +${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);
    }

    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;
        Money -= amount;
        Debug.Log($"[PlayerMoneyStore] -${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public static void Clear()
    {
        Money = 0;
        OnMoneyChanged?.Invoke(Money);
    }

    public static bool CanAfford(int amount) => Money >= amount;
}
```

### Texto de dinero con TMP (handler de evento)

```csharp
// Source: HUDController.cs (a crear)
private TMP_Text moneyText;  // using TMPro;

private void HandleMoneyChanged(int newAmount)
{
    if (moneyText != null)
        moneyText.text = $"${newAmount}";
}
```

### Image.type = Filled — configuración clave

```csharp
// Source: Unity uGUI API nativo
Image fillImage = fillGO.AddComponent<Image>();
fillImage.type = Image.Type.Filled;
fillImage.fillMethod = Image.FillMethod.Horizontal;
fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
fillImage.fillAmount = 0f;  // empieza vacia (D-05)
```

### CanvasScaler — resolución de referencia

```csharp
// Source: UI-SPEC.md §Design System
CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920f, 1080f);
scaler.matchWidthOrHeight = 0.5f;
```

---

## Environment Availability

| Dependencia | Requerida por | Disponible | Versión | Fallback |
|-------------|---------------|------------|---------|---------|
| Unity Editor 6000.3.11f1 | Todo | ✓ | 6000.3.11f1 | — |
| `com.unity.ugui` (Canvas, Image, TMP) | HUD entero | ✓ | 2.0.0 | — |
| TMP Essential Resources (assets importados) | `TextMeshProUGUI` en runtime | ✗ (carpeta vacia) | — | Importar via `Window > TextMeshPro > Import TMP Essential Resources` — Wave 0 obligatorio |
| `LiberationSans SDF` (TMP font asset) | Texto de dinero | ✗ (no en Assets) | — | Se genera al importar TMP Essentials |
| Unity MCP (`com.coplaydev.unity-mcp`) | Automatización editor | ✓ (instalado) | main branch | Configurar manualmente en el Editor si MCP no conecta |

**Missing dependencies con fallback:**
- TMP Essential Resources: No importados todavía. Acción en Wave 0: `Window > TextMeshPro > Import TMP Essential Resources`. Sin esto, el texto TMP no renderiza en runtime.

**Missing dependencies sin fallback:**
- Ninguna que bloquee la fase.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (`com.unity.test-framework`) — instalado, sin tests existentes |
| Config file | Ninguno — no hay `.asmdef` en `Assets/_Project/`; el Test Runner no descubrirá tests sin assembly definitions |
| Quick run command | `Window > General > Test Runner > Run All` (solo desde el Editor) |
| Full suite command | Ídem — no hay CLI |

### Phase Requirements -> Test Map

| Req ID | Comportamiento | Tipo de test | Notas |
|--------|---------------|--------------|-------|
| HUD-01 | Barra de borrachera refleja `EffectIntensity` en tiempo real | Play Mode (manual) | No hay framework para automatizar; verificar visualmente en Play Mode |
| HUD-02 | Texto de dinero se actualiza al vender/comprar | Play Mode (manual) | Verificar en Bar (venta) y Bar (compra de bebida) |

### Wave 0 Gaps

No hay tests automáticos para esta fase. La arquitectura de UI no es candidata para Edit Mode tests. Los tests de validación son manuales:

1. Entrar a Play Mode en City — confirmar HUD visible con barra vacía y `$0`
2. Cargar Bar (pasar puerta) — confirmar HUD persiste, no se duplica
3. Comprar y beber cerveza — confirmar que la barra sube progresivamente (lerp)
4. Vender objeto en el mostrador — confirmar que el texto cambia instantáneamente

La lógica pura que sí es testeable: `PlayerMoneyStore.OnMoneyChanged` puede verificarse con un Edit Mode test. Queda fuera del scope de esta fase por no haber asmdef configurado (ver TESTING.md). No es un blocker.

---

## Project Constraints (from CLAUDE.md)

Directivas que el plan debe respetar explícitamente:

| Directiva | Impacto en Phase 2 |
|-----------|-------------------|
| Namespace global (sin `namespace`) | `HUDController` y cualquier script nuevo va sin `namespace` |
| Input legacy (`UnityEngine.Input`, `Input.GetKeyDown`) | HUD no tiene input; no aplica directamente, pero no introducir Input System |
| Comentarios en español | Todo `Debug.Log` con prefijo `[HUDController]` y `/// <summary>` en español |
| Namespace global | No agregar `namespace DrunkSimulator.UI {}` aunque sea tentador |
| Un archivo por clase | `HUDController.cs` solo contiene la clase `HUDController` |
| `private`+ `[SerializeField]` para campos expuestos al Inspector | N/A — el HUD construye por código; si hubiera campos serializados, seguir esta convención |
| Patrón de fallback en `FindFirstObjectByType` | Ya implementado en D-02; null guard obligatorio tras la llamada |
| `DontDestroyOnLoad` + `RuntimeInitializeOnLoadMethod` | Patrón directo de `BackgroundMusicManager` — replicar exactamente |
| Sin `namespace` | Ya dicho; el proyecto entero está en namespace global |
| Assets propios en `Assets/_Project/` organizados por responsabilidad | `HUDController.cs` → `Assets/_Project/UI/HUD/`; si se crea un prefab → misma carpeta |

---

## Assumptions Log

| # | Claim | Sección | Riesgo si es incorrecto |
|---|-------|---------|------------------------|
| A1 | `SceneManager.sceneLoaded` se dispara después de que los `Awake()` de la escena nueva ya corrieron, por lo que `FindFirstObjectByType<DrunkManager>()` en ese callback encontrará el manager ya inicializado | Architecture Patterns / Pattern 2 | Si el orden fuera al revés, el HUD obtendría null y necesitaría una Coroutine o `WaitForEndOfFrame` de fallback |
| A2 | En Unity 6 con ugui 2.0.0, `using TMPro;` y `TextMeshProUGUI` están disponibles sin instalar ningún package adicional | Standard Stack | Si el namespace estuviera separado, habría que agregar una referencia de assembly |
| A3 | Construir la jerarquía de UI enteramente por código (sin prefab) en `HUDController.Awake` es viable sin problemas de ordering | Architecture Patterns / Pattern 4 | Si Unity requiriera que el Canvas estuviera configurado antes de Awake, sería necesario un prefab en Resources |

**Si la tabla está vacía en A1:** `SceneManager.sceneLoaded` ocurre después de `Awake` según la documentación de Unity — pero esto está marcado [ASSUMED] porque no fue verificado explícitamente en un test en esta sesión. En la práctica, el fallback con null guard es suficiente para cualquier escenario de ordering.

---

## Open Questions (RESOLVED)

1. **Outline del texto TMP: material vs Shadow component**
   - Lo que sabemos: La UI-SPEC indica usar outline (`outlineWidth ≈ 0.2`) sobre el material SDF o un `Shadow` component como alternativa.
   - Lo que no está claro: Si el `LiberationSans SDF` importado viene con un material que expone el outline en el Inspector, o si hay que crear un material custom.
   - Recomendación: Usar el `Shadow` component de uGUI (disponible sin material custom) para MVP. Si el resultado visual no es satisfactorio, crear un material SDF custom es el paso siguiente.

2. **Reset del HUD al empezar nueva partida**
   - Lo que sabemos: `PlayerMoneyStore.Clear()` ya dispara `OnMoneyChanged` (D-03). Para el alcohol: el `DrunkManager` es per-escena y se resetea automáticamente al recargar la escena inicial.
   - Lo que no está claro: Si hay un flujo de "Nueva Partida" en alguna fase futura que necesite resetear el fill de la barra también.
   - Recomendación: Documentar en el código que al llamar `DrunkManager.ResetLevel()` en la escena, el `OnAlcoholLevelChanged` ya se dispara y el HUD recibe el 0. No hay acción adicional necesaria en este scope.

---

## Sources

### Primary (HIGH confidence)
- `Assets/_Project/Core/Audio/BackgroundMusicManager.cs` — patrón exacto a replicar para D-01
- `Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs` — store a modificar (D-03)
- `Assets/_Project/Core/Managers/DrunkManager.cs` — fuente de `EffectIntensity`, evento `OnAlcoholLevelChanged`
- `Assets/_Project/Core/Managers/PlayerCarController.cs` — confirma que el HUD Screen Space Overlay no interactúa con el toggle fpsCamera/carCamera
- `Packages/manifest.json` — confirma `com.unity.ugui 2.0.0` (TMP incluido)
- `.planning/phases/02-hud-in-level/02-CONTEXT.md` — decisiones D-01 a D-07 bloqueadas
- `.planning/phases/02-hud-in-level/02-UI-SPEC.md` — especificaciones visuales, dimensiones, colores
- `.planning/codebase/CONVENTIONS.md` — convenciones del proyecto
- `.planning/codebase/TESTING.md` — estado del testing (no hay asmdef)

### Secondary (MEDIUM confidence)
- `find Assets/TextMesh Pro -type f` — confirmó que los TMP Essential Resources NO están importados (solo .meta de carpetas vacías)
- `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Package Resources/TMP Essential Resources.unitypackage` — confirma que el package existe y puede importarse

### Tertiary (LOW confidence / ASSUMED)
- Orden de ejecución de `SceneManager.sceneLoaded` respecto a `Awake` — no verificado explícitamente, marcado A1

---

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — verificado directo en manifest.json y codebase
- Architecture: HIGH — basado en código existente (BackgroundMusicManager, DrunkManager, PlayerMoneyStore); patrón ya funciona en el proyecto
- TMP Essentials status: HIGH — confirmado via filesystem: carpeta vacía, unitypackage en Library cache sin extraer
- Pitfalls: MEDIUM-HIGH — derivados del código existente; A1 es el único claim no verificado en esta sesión

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (estable; Unity 6 / ugui 2.0.0 no cambia frecuentemente)
