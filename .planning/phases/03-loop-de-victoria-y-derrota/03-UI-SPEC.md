---
phase: 3
slug: loop-de-victoria-y-derrota
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-24
---

# Phase 3 — UI Design Contract: Loop de victoria y derrota

> Contrato visual e interactivo para la escena dedicada `Result`.
> Generado por gsd-ui-researcher. Consumido por gsd-planner y gsd-executor.

---

## Scope de esta fase

**Una sola superficie de UI nueva:** la escena `Result`, parametrizada por `GameResultStore` (Victory | Defeat).

Fuera de scope: HUD in-level (Phase 2, ya implementado), partículas de choque (Phase 5), animaciones (Phase 4).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Unity UI por codigo, mismo patron que HUDController) |
| Preset | not applicable |
| Component library | UnityEngine.UI (Canvas, Image, Button legacy) + TextMeshPro |
| Icon library | none |
| Font | LiberationSans SDF — `Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")` con fallback `"LiberationSans SDF"` (mismo patron que HUDController.cs) |

**Nota:** No hay design system web. Todo se construye por codigo en `ResultScreenController.cs`, dentro de `Assets/_Project/UI/Screens/`, replicando el patron de `HUDController.cs`: Canvas por codigo, componentes Unity UI, TextMeshProUGUI, sin prefabs de UI.

---

## Canvas y CanvasScaler

| Propiedad | Valor |
|-----------|-------|
| RenderMode | ScreenSpaceOverlay |
| sortingOrder | 10 (debajo del HUD, que usa 100; el HUD no debe mostrarse en la escena Result — ver seccion Integracion) |
| uiScaleMode | ScaleWithScreenSize |
| referenceResolution | 1920 x 1080 |
| matchWidthOrHeight | 0.5 |

El HUD (HUDController, DontDestroyOnLoad) persiste al cargar Result. Para ocultarlo: `HUDController.SetVisible(false)` o destruir su GameObject al entrar a Result. El executor decide el mecanismo; lo que es fijo: **el HUD no debe ser visible durante la pantalla de resultado**.

---

## Spacing Scale

Valores en pixeles logicos sobre referencia 1920x1080. Todos multiplos de 4.

| Token | Valor | Uso en esta pantalla |
|-------|-------|----------------------|
| xs | 4px | Gap entre lineas de subtitulo |
| sm | 8px | Separacion entre label y valor; padding interno de botones (vertical) |
| md | 16px | Espacio entre elementos del panel |
| lg | 24px | Margen del panel respecto al borde de pantalla |
| xl | 32px | Separacion entre el bloque de titulo y el bloque de botones |
| 2xl | 48px | Padding vertical interno del panel (top/bottom) |
| 3xl | 64px | No usado en esta fase |

Excepcion: touch targets de botones minimo 48px de alto (criterio de accesibilidad basico).

---

## Jerarquia de Canvas (estructura por codigo)

```
Canvas [ResultCanvas]
  └── Panel [ResultPanel]          -- fondo semi-transparente, centrado
        ├── TitleText              -- encabezado grande (Victoria / Derrota)
        ├── MessageText            -- mensaje secundario (1-2 lineas)
        └── ButtonGroup            -- contenedor vertical de botones
              ├── RetryButton      -- "Reintentar"
              └── QuitButton       -- "Salir"
```

### ResultPanel
- AnchorMin/Max: (0.5, 0.5) — centrado en pantalla
- Pivot: (0.5, 0.5)
- SizeDelta: 640 x 360 px (sobre referencia 1920x1080)
- Padding interno: 48px top/bottom, 32px left/right
- Fondo: Image solida, color segun estado (ver seccion Color)

### TitleText (TextMeshProUGUI)
- AnchoredPosition: parte superior del panel, debajo del padding top
- SizeDelta: 576 x 80 px (panel width - 2*padding)
- Fuente: LiberationSans SDF, 52px, Bold (700)
- Alineacion: Center/Middle
- Color: ver seccion Color segun estado

### MessageText (TextMeshProUGUI)
- Debajo de TitleText con gap md (16px)
- SizeDelta: 576 x 60 px
- Fuente: LiberationSans SDF, 24px, Regular (400)
- Alineacion: Center/Middle
- Line Height: 1.3
- Color: blanco puro (#FFFFFF) en ambos estados

### ButtonGroup
- Debajo de MessageText con gap xl (32px)
- VerticalLayoutGroup: spacing sm (8px), childForceExpandWidth true
- SizeDelta: 576 x 112 px (2 botones x 48px + 1 gap x 8px + 8px de margen)

### Cada boton (Button legacy + onClick)
- SizeDelta: 576 x 48 px
- Image de fondo: color segun estado y tipo (ver seccion Color — botones)
- Texto hijo (TextMeshProUGUI): 20px, Bold (700), color blanco
- Alineacion del texto: Center/Middle

---

## Typography

**Focal point: TitleText es el ancla visual primaria de la pantalla.** Es el elemento de mayor tamaño y el unico con sombra; el executor no debe agregar otros elementos que compitan con su jerarquia.

| Rol | Tamano | Peso | Line Height | Componente Unity |
|-----|--------|------|-------------|------------------|
| Titulo (TitleText) | 52px | Bold 700 | 1.2 | TextMeshProUGUI |
| Cuerpo / mensaje (MessageText) | 24px | Regular 400 | 1.3 | TextMeshProUGUI |
| Label boton | 20px | Bold 700 | 1.0 | TextMeshProUGUI |
| HUD existente (referencia, no tocar) | 28px | Bold 700 | — | TextMeshProUGUI |

Exactamente 3 tamanos declarados en esta pantalla: 52, 24, 20. Exactamente 2 pesos: Regular 400 (cuerpo) y Bold 700 (titulo y botones). En implementacion Unity: `FontStyles.Bold` para TitleText y labels de boton; `FontStyles.Normal` para MessageText.

Sombra de texto: aplicar `Shadow` component (effectDistance 1,-1, color negro) en TitleText unicamente, para resaltar el encabezado. MessageText y labels de boton sin sombra.

---

## Color

### Paleta base (60 / 30 / 10)

| Rol | Hex | RGBA Unity | Uso |
|-----|-----|------------|-----|
| Dominante (60%) | #1A1A2E | (0.102, 0.102, 0.180, 1.0) | Fondo de pantalla detras del panel (overlay oscuro sobre la escena descargada) |
| Secundario (30%) | #16213E | (0.086, 0.129, 0.243, 1.0) | Fondo del ResultPanel (ligeramente diferente del dominante para dar profundidad) |
| Acento Victoria (10%) | #4CAF50 | (0.298, 0.686, 0.314, 1.0) | TitleText en victoria, borde/highlight del panel en victoria, boton Reintentar en victoria |
| Acento Derrota (10%) | #E53935 | (0.898, 0.224, 0.208, 1.0) | TitleText en derrota, borde/highlight del panel en derrota, boton Reintentar en derrota |
| Destructivo | #757575 | (0.459, 0.459, 0.459, 1.0) | Boton "Salir" en ambos estados (accion de salir del juego) |

**Regla de distincion obligatoria (GAME-03 / GAME-04):**
- Victoria: panel con tinte verde (`#4CAF50` en TitleText + Image del panel con color `#1A2E1A` en lugar del secundario).
- Derrota: panel con tinte rojo (`#E53935` en TitleText + Image del panel con color `#2E1A1A` en lugar del secundario).
- La diferencia debe ser perceptible de un vistazo sin leer el texto.

### Colores del panel por estado

| Estado | Color fondo panel | Color TitleText | Color boton Reintentar |
|--------|-------------------|-----------------|------------------------|
| Victoria | #1A2E1A (0.102, 0.180, 0.102, 0.92) | #4CAF50 | #388E3C (verde oscuro) |
| Derrota | #2E1A1A (0.180, 0.102, 0.102, 0.92) | #E53935 | #C62828 (rojo oscuro) |
| Boton Salir (ambos) | #616161 (0.380, 0.380, 0.380, 1.0) | — | — |

Opacidad del panel: alpha 0.92 (casi opaco). Detras del panel: overlay negro `#000000` alpha 0.55 sobre toda la pantalla (un Image que ocupa el Canvas completo, anchorMin 0,0 anchorMax 1,1).

### Acento reservado para
- TitleText: unico uso del color de acento por estado.
- Boton Reintentar: unico boton que adopta el color de acento.
- Borde del panel: opcional si el executor agrega un Image de borde (1px de grosor simulado con un panel ligeramente mayor detras).

---

## Copywriting Contract

### Victoria

| Elemento | Copy exacto |
|----------|-------------|
| TitleText | `LLEGASTE A CASA` |
| MessageText | `Vendiste todo y llegaste sano. Por ahora.` |
| Boton primario (Reintentar) | `Jugar de nuevo` |
| Boton secundario (Salir) | `Salir` |

### Derrota

| Elemento | Copy exacto |
|----------|-------------|
| TitleText | `CHOCASTE` |
| MessageText | `El alcohol ganó esta vez. Siempre hay otra.` |
| Boton primario (Reintentar) | `Reintentar` |
| Boton secundario (Salir) | `Salir` |

### Estado de confirmacion para Salir
No se muestra dialogo de confirmacion adicional. `Application.Quit()` se llama directamente. En el Editor Unity, `Application.Quit()` es no-op; agregar un `Debug.Log("[ResultScreen] Salir presionado.")` para verificar en Play Mode.

### Convencion de mayusculas
- TitleText: MAYUSCULAS completas (FontStyles o string.ToUpper() en asignacion).
- Botones y MessageText: sentence case (solo primera letra mayuscula).

---

## Estados de botones

| Estado | Color fondo | Color texto | Transicion |
|--------|-------------|-------------|-----------|
| Normal | Color base del boton (ver tabla Color) | #FFFFFF | — |
| Highlighted (hover) | Color base + 15% mas claro | #FFFFFF | ColorBlock transition: 0.1s |
| Pressed | Color base - 15% mas oscuro | #FFFFFF | ColorBlock transition: 0.05s |
| Disabled | #424242 | #9E9E9E | — |

Configurar via `Button.colors` (ColorBlock) por codigo:
```csharp
ColorBlock cb = ColorBlock.defaultColorBlock;
cb.normalColor   = colorBase;
cb.highlightedColor = Color.Lerp(colorBase, Color.white, 0.15f);
cb.pressedColor  = Color.Lerp(colorBase, Color.black, 0.15f);
cb.fadeDuration  = 0.1f;
button.colors    = cb;
```

---

## Interaccion y navegacion

### Cursor
- Al inicializar la escena `Result`: `Cursor.visible = true; Cursor.lockState = CursorLockMode.None;`
- Esto es obligatorio (D-10). La escena anterior (City) tenia el cursor bloqueado para el FPS.

### Time.timeScale
- Al inicializar la escena `Result`: `Time.timeScale = 1f;` (D-10).
- No se usa freeze/pause; el unload de la escena anterior provee el "stop".

### Flujo de botones

| Boton | Accion | Detalle |
|-------|--------|---------|
| Reintentar | `GameManager.NewGame()` o metodo estatico equivalente | Llama `Clear()` de todos los stores (CarStateStore, DeliveredObjectsStore, DrunkLevelStore, PlayerMoneyStore, HeldObjectStore), resetea `PlayerSpawner.NextSpawnId = null`, limpia `GameResultStore`, luego `SceneManager.LoadSceneAsync("Home", LoadSceneMode.Single)` |
| Salir | `Application.Quit()` | En Editor: no-op + Debug.Log |

### Primer foco
No se establece primer foco de boton automatico (no es una UI de gamepad; el juego usa Input legacy con mouse). El cursor visible permite hacer click directamente.

---

## Integracion con HUD existente

El HUDController es un singleton `DontDestroyOnLoad` que persiste entre escenas. Al cargar `Result`, el HUD seguira visible a menos que se lo oculte.

**Contrato:** el ResultScreenController debe ocultar el HUD al inicializarse y restaurarlo al salir (aunque en la practica Reintentar recarga Home y el HUD se reinicializa).

Mecanismo recomendado: exponer `HUDController.SetVisible(bool)` que haga `canvas.enabled = value`. El executor puede elegir una alternativa equivalente.

---

## Estructura de archivos a crear

| Archivo | Path |
|---------|------|
| ResultScreenController.cs | `Assets/_Project/UI/Screens/ResultScreenController.cs` |
| GameResultStore.cs | `Assets/_Project/Core/SceneManagement/GameResultStore.cs` |
| GameManager.cs | `Assets/_Project/Core/Managers/GameManager.cs` |
| LethalObstacle.cs | `Assets/_Project/Gameplay/Systems/LethalObstacle.cs` |
| Escena Result | `Assets/Scenes/Result.unity` (agregar a Build Settings) |

`ResultScreenController` es un `MonoBehaviour` en la escena `Result` que construye el Canvas por codigo en `Awake`, lee `GameResultStore.Result`, aplica colores/textos segun el estado, y conecta los onClick de los botones.

---

## Registry Safety

| Registry | Bloques usados | Safety Gate |
|----------|---------------|-------------|
| shadcn | ninguno | not applicable (proyecto Unity, no web) |
| Terceros | ninguno | not applicable |

No se usan registros de terceros. Todos los componentes son de Unity (UnityEngine.UI, TextMeshPro, parte del proyecto).

---

## Pre-populated from

| Fuente | Decisiones utilizadas |
|--------|----------------------|
| 03-CONTEXT.md (D-07) | Escena dedicada Result, parametrizada por GameResultStore |
| 03-CONTEXT.md (D-08) | Dos botones: Reintentar + Salir |
| 03-CONTEXT.md (D-09) | Reintentar = reset completo de partida via NewGame() |
| 03-CONTEXT.md (D-10) | Cursor visible, lockState None, timeScale 1 |
| 03-CONTEXT.md (Claude's Discretion) | Colores, tipografia, mensajes exactos, mecanismo de boton |
| HUDController.cs | Canvas settings (1920x1080, ScaleWithScreenSize 0.5), fuente LiberationSans SDF con fallback, patron de construccion por codigo, patron Shadow, color ambar de referencia |
| REQUIREMENTS.md GAME-02/03/04 | Criterios de distincion visual entre estados |
| CLAUDE.md | Namespace global, sin namespace, comentarios en espanol, Input legacy, TMP |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
