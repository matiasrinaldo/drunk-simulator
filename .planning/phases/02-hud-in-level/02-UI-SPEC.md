---
phase: 2
slug: hud-in-level
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-23
---

# Phase 2 — UI Design Contract (HUD in-level)

> Contrato visual y de interacción para el HUD in-level. Generado por gsd-ui-researcher, verificado por gsd-ui-checker.
>
> **Contexto:** Esto NO es UI web. Es un HUD de Unity (uGUI + TextMeshPro) renderizado sobre el juego en un Canvas **Screen Space - Overlay**. Los conceptos del template (spacing, tipografía, color, estados) se adaptan a px de Canvas, RectTransform, anclajes y `TMP_FontAsset`. No hay CSS, shadcn ni registries: el "design system" del proyecto es uGUI/TMP nativo de Unity.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Unity uGUI nativo — shadcn no aplica) |
| Preset | not applicable |
| Component library | Unity UI (`com.unity.ugui 2.0.0`) — `Canvas`, `Image (type=Filled)`, `TextMeshProUGUI` |
| Icon library | none (HUD minimalista solo-texto + barra; sin íconos en esta fase) |
| Font | `LiberationSans SDF` (TMP_FontAsset por defecto de TextMesh Pro Essentials) |

**Notas de plataforma:**
- Resolución de referencia del `CanvasScaler`: **1920×1080**, modo **Scale With Screen Size**, `matchWidthOrHeight = 0.5` (escala uniforme FPS y modo auto).
- Render mode: **Screen Space - Overlay** (D-06) — cámara-agnóstico; se ve idéntico en FPS (City/Bar/Home) y en modo auto (`CarFollowCamera`).
- Sort order del Canvas: alto (p. ej. `100`) para quedar por encima de cualquier otro Canvas de escena.

---

## Spacing Scale

Valores declarados en px de Canvas @1920×1080 (múltiplos de 4):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Gap interno fino |
| sm | 8px | Gap entre barra de borrachera y texto de dinero (apilados verticalmente) |
| md | 16px | Padding interno de elementos / borde del marco de la barra |
| lg | 24px | **Margen del grupo HUD respecto a los bordes inferior e izquierdo de la pantalla** (anclaje bottom-left) |
| xl | 32px | (reservado — no usado en esta fase) |

**Layout del grupo HUD (D-07 — esquina inferior izquierda, minimalista):**
- Contenedor raíz `RectTransform` anclado a **bottom-left** (anchor min/max = (0,0)), pivote (0,0), posición `(24, 24)`.
- Orden vertical (de arriba hacia abajo dentro del grupo): **texto de dinero** arriba, **barra de borrachera** abajo. Gap entre ambos = `sm` (8px).
- Barra de borrachera: ancho `220px`, alto `20px`.
- El grupo NO debe invadir el centro de la pantalla ni el área del mostrador/objetos interactuables.

Exceptions: ninguna.

---

## Typography

Solo hay UN rol de texto en esta fase (el indicador de dinero). Se declara con sus variantes de tamaño para mantener el contrato dentro del rango 3–4 tamaños del checker, pero **únicamente "Money label" se usa en Phase 2**; los demás quedan reservados para fases con más HUD (resultado/victoria/derrota).

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Money label (en uso) | 28px | Bold (TMP `fontStyle = Bold`) | 1.0 (single line) |
| Caption (reservado) | 20px | Regular | 1.0 |
| Heading (reservado) | 36px | Bold | 1.2 |

**Pesos:** exactamente 2 — **Regular** y **Bold** (TMP no usa font-weight numérico; se aplica vía `fontStyle`/material por defecto del SDF).

**Money label — detalle TMP:**
- `TextMeshProUGUI`, font `LiberationSans SDF`, `fontSize = 28`, `fontStyle = Bold`.
- Alignment: **Left**, anclado a bottom-left del grupo.
- **Outline / borde** para legibilidad sobre cualquier fondo del juego: usar el material SDF con outline `width ≈ 0.2`, color outline = Outline token (ver Color). Alternativa equivalente: `Shadow` component con offset (1,-1). Esto NO es decorativo: es contraste obligatorio porque el HUD flota sobre escenas claras y oscuras.
- Las animaciones de la barra/HUD son por código (lerp); no hay tipografía animada en esta fase.

---

## Color

Paleta del HUD. La regla 60/30/10 se interpreta para un overlay: el "dominante" es transparente (el juego se ve a través), por lo que el 60/30/10 aplica al **área visible del HUD** (marco + fill + texto).

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#000000` @ 45% alpha (`rgba(0,0,0,0.45)`) | Fondo/track del marco de la barra (caja semitransparente que aloja el fill y da contraste) |
| Secondary (30%) | `#FFFFFF` | Texto de dinero (relleno del glifo) + posible borde del marco a 70% alpha |
| Accent (10%) | `#E0B040` (ámbar / "cerveza") | **Fill de la barra de borrachera** — único uso del accent |
| Destructive | not applicable | No hay acciones destructivas en esta fase (HUD de solo-lectura) |

**Accent reservado para:** exclusivamente el **fill de la barra de borrachera** (`Image type=Filled`). Ningún otro elemento usa ámbar.

**Outline token (auxiliar de contraste, no cuenta en el 60/30/10):** `#000000` @ 100% — outline del texto de dinero y borde del fill para garantizar legibilidad sobre fondos claros y oscuros.

**Umbral de color opcional (Claude's Discretion en CONTEXT.md — NO requisito):**
Si se implementa el feedback opcional al acercarse al máximo, el fill puede interpolar de `#E0B040` (ámbar, bajo) hacia `#C0392B` (rojo, ~`EffectIntensity ≥ 0.85`). Esto es opcional; si no se implementa, el fill permanece ámbar fijo. No agrega un color destructivo al contrato — es una variante del accent.

**Contraste:** texto blanco (#FFFFFF) sobre track negro 45% + outline negro → contraste efectivo ≥ 4.5:1 sobre cualquier fondo de juego.

---

## Copywriting Contract

El HUD es **solo-lectura y pasivo**: no tiene CTAs, ni formularios, ni acciones destructivas. El único texto es el indicador de dinero. Los estados "vacío/error" no aplican en el sentido web; se documentan los estados equivalentes del HUD.

| Element | Copy |
|---------|------|
| Primary CTA | not applicable (HUD pasivo, sin acciones) |
| Money label format | `${valor}` — sin separador de miles, sin decimales. Ej.: `$0`, `$50`, `$1250`. Consistente con los logs de `PlayerMoneyStore` (`$50`). |
| Estado inicial (dinero) | `$0` — el jugador arranca con saldo 0 (ver `PlayerMoneyStore.Money = 0`). NUNCA mostrar campo vacío ni guion. |
| Estado inicial (barra) | Fill = 0 (barra vacía) cuando `EffectIntensity == 0`. La barra vacía ES un estado válido y visible (el marco/track se sigue viendo). |
| Estado de error | not applicable — el HUD no falla a nivel de UI. Si `DrunkManager` aún no existe en el frame de carga, la barra mantiene su último valor (no muestra error ni texto). |
| Destructive confirmation | not applicable (sin acciones destructivas en esta fase) |

**Localización:** todo el texto en español, consistente con el proyecto. (El símbolo `$` no requiere traducción.)

---

## Interaction & State Contract (Unity HUD)

Sección específica de Unity que reemplaza los "estados de componente" web. El executor implementa exactamente estos comportamientos.

| Estado | Trigger | Comportamiento visual |
|--------|---------|-----------------------|
| Borrachera — relleno | Cada frame | `fillAmount` hace **lerp** hacia `DrunkManager.EffectIntensity` (D-04/D-05). `fillAmount = Mathf.MoveTowards/Lerp(current, target, speed*dt)`. Velocidad sugerida: ~`3` u/s o `lerp t≈0.1`/frame, ajustable, para que un sorbo suba la barra de forma suave (no salto). |
| Borrachera — vacía | `EffectIntensity == 0` | Fill = 0; el track/marco sigue visible. |
| Borrachera — llena | `EffectIntensity == 1` | Fill = 1 (lleno). Opcional: tinte rojo si se implementa el umbral. |
| Dinero — cambio | Evento `PlayerMoneyStore.OnMoneyChanged` (D-03) | El texto se actualiza **al instante** (criterio de éxito 2). Sin animación de conteo requerida. |
| Dinero — estado inicial | Suscripción / `sceneLoaded` | El HUD lee `PlayerMoneyStore.Money` al suscribirse y al cargar cada escena para reflejar el valor correcto desde el primer frame. |
| Re-vínculo por escena | `SceneManager.sceneLoaded` | El HUD re-resuelve `FindFirstObjectByType<DrunkManager>()` (D-02) con fallback si aún no existe; el dinero no necesita re-vínculo (store estático). |
| Persistencia del HUD | Bootstrap | Singleton `[RuntimeInitializeOnLoadMethod]` + `DontDestroyOnLoad` (D-01), patrón de `BackgroundMusicManager`. El Canvas se instancia una vez; no se agrega a cada escena. |
| Visibilidad multi-modo | Siempre | Visible en FPS (City/Bar/Home) y en modo auto (City) sin reconfigurar — garantizado por Screen Space - Overlay (D-06). |

**Image Filled config (barra):** `Image.type = Filled`, `fillMethod = Horizontal`, `fillOrigin = Left`. NO usar `Slider` (D-05).

---

## Component Inventory (prefab del HUD)

Jerarquía objetivo del prefab a crear en `Assets/_Project/UI/HUD/` (o `UI/Prefabs/`):

```
HUDCanvas (Canvas: Screen Space-Overlay, sort order 100; CanvasScaler 1920x1080; GraphicRaycaster)
└── HUDGroup (RectTransform: anchor bottom-left, pos (24,24), pivot (0,0))
    ├── MoneyText (TextMeshProUGUI: "$0", 28px Bold, blanco, outline negro)
    └── DrunkBar (RectTransform: 220x20, gap 8px bajo MoneyText)
        ├── Track  (Image: negro @45% alpha — fondo/marco)
        └── Fill   (Image type=Filled, Horizontal, Left; color ámbar #E0B040)
```

Script controlador (singleton): lee `EffectIntensity` por frame (lerp del fill), se suscribe a `OnMoneyChanged` y a `sceneLoaded`, namespace global (sin `namespace`), Input legacy si aplicara, comentarios en español. Sigue convenciones de `CLAUDE.md`.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none (Unity nativo) | uGUI `Canvas`/`Image`/`TextMeshProUGUI` (paquetes oficiales de Unity) | not applicable — sin registries de terceros |

No se declararon registries de terceros. Vetting gate: no aplica.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
