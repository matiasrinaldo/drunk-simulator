# Phase 4: Animaciones - Research

**Researched:** 2026-06-24
**Domain:** Unity 6 (6000.3.11f1) animation — Animator state machine + code-driven animation, URP 17.3
**Confidence:** HIGH (codebase facts verified directly; Unity API facts CITED from official docs)

## Summary

Phase 4 demanda dos cosas concretas y distinguibles en Play Mode: (1) **ANIM-01** un Animator con ≥2 estados y una transición condicional sobre un elemento principal visible, y (2) **ANIM-02** un elemento animado **enteramente por código** (sin AnimationClip), modificando transform/material/propiedades en `Update` o coroutine. El proyecto es brownfield: ya existe un patrón de animación por código maduro (`PlayerPickup.AnimateDrinkSip`, lerp de la barra en `HUDController.Update`) y un patrón establecido de consumir `DrunkManager.EffectIntensity` por frame. Eso hace que **ANIM-02 sea casi gratis** reusando esos patrones; el trabajo real está en **ANIM-01**, porque el juego NO tiene ningún Animator hoy (verificado: cero `.controller`/`.anim` fuera de ThirdParty) y el jugador es una **cápsula primitiva** (mesh built-in 10208, `CharacterController`, sin SkinnedMeshRenderer ni rig) — así que un walk/idle humanoide queda descartado sin autoría de mallas.

La recomendación primaria para **ANIM-01** es una **puerta animada por Animator** (la puerta de entrada al Bar o a Home en la escena City) con dos estados `Closed`/`Open` y una transición disparada por un parámetro `Open` (Trigger o Bool) que setea el código del trigger existente cuando el jugador se acerca. Es un "elemento principal visible", el AnimationClip de rotación de bisagra se autorea fácil en el editor (o por script con `AnimationClip.SetCurve`), y el disparo se engancha en los `*DoorTrigger.OnTriggerEnter` ya existentes — con la salvedad importante de que **hoy esos triggers cargan la escena inmediatamente**, así que hay que introducir un pequeño retardo para que la animación sea visible (ver Pitfall 3).

La recomendación primaria para **ANIM-02** es el **bob/sway del objeto sostenido** (`PlayerPickup`, el `currentHeldVisual` en el `holdPoint`): un movimiento senoidal de `localPosition` en `Update`, idealmente **modulado por `DrunkManager.EffectIntensity`** para reforzar el tema del juego (más borracho → más tambaleo del trago en la mano). Reusa el patrón exacto del proyecto y es trivialmente distinguible. Alternativa fuerte: pulso de escala/alpha de la barra de borrachera en el HUD cuando el nivel es alto.

**Primary recommendation:** ANIM-01 = puerta con Animator (estados Closed/Open + transición por parámetro `Open`, disparado desde el DoorTrigger con retardo de cierre de escena). ANIM-02 = bob senoidal del objeto sostenido en `PlayerPickup.Update`, escalado por `EffectIntensity`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Animator state machine (ANIM-01) | Asset (AnimatorController + AnimationClip) | Gameplay script que setea el parámetro | El estado/transición vive en el asset `.controller`; el código solo dispara el parámetro |
| Disparo de transición ANIM-01 | Gameplay trigger (`*DoorTrigger`) | — | El evento ya existe (`OnTriggerEnter`); solo agrega `animator.SetTrigger("Open")` |
| Animación por código (ANIM-02) | Gameplay script (`PlayerPickup`) | `DrunkManager.EffectIntensity` (lectura por frame) | Sigue el patrón establecido del proyecto: consumidor lee EffectIntensity y aplica distorsión |
| Material/emisión animada (alternativa ANIM-02) | Gameplay script via `MaterialPropertyBlock` | URP Lit shader (`_EmissionColor`) | El proyecto ya usa MPB para highlight; URP usa `_BaseColor`/`_EmissionColor` |

## User Constraints

> No existe CONTEXT.md para esta fase (research standalone / integrado sin discuss previo). Restricciones derivadas de CLAUDE.md y REQUIREMENTS.md:

### Locked Decisions (de CLAUDE.md / convenciones del proyecto)
- **Unity 6000.3.11f1 exacto**, URP 17.3. No cambiar versión.
- **Input legacy API** (`UnityEngine.Input`, `Input.GetKeyDown`, `KeyCode`). No usar el nuevo Input System aunque esté instalado.
- **Namespace global** (sin `namespace`). Mantener.
- **Comentarios en español.**
- Código propio vive en `Assets/_Project/`, organizado por responsabilidad (no por tipo).
- Patrón de efectos de borrachera: cachear `DrunkManager` en `Awake` (con fallback `FindFirstObjectByType`), leer `EffectIntensity` por frame, multiplicar.
- Patrón de carga de recursos: rutas con **fallback** (`Resources.Load`).
- Estado entre escenas: clases estáticas en memoria con `.Clear()`.
- Verificación en **Play Mode** (no hay build por CLI ni Makefile).

### Claude's Discretion
- Qué elemento concreto recibe el Animator (puerta vs. otro), siempre que sea "elemento principal visible" con ≥2 estados + transición condicional.
- Qué elemento recibe la animación por código.
- Si el AnimationClip de ANIM-01 se autorea en el editor o por script.

### Deferred Ideas (OUT OF SCOPE)
- Cinemachine (TP1.5), menú principal elaborado, Strategy, UI scrolling, ≥3 eventos — todos diferidos a milestone de pendientes TP1.
- Partículas y luces (FX-01/FX-02) y pantalla de carga (SCENE-01) → **Phase 5**, no acá.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ANIM-01 | Animación por máquina de estados (Animator) en un elemento principal | Candidato primario: puerta (Bar/Home) con Animator estados Closed/Open + transición por parámetro `Open`. Hook: `*DoorTrigger.OnTriggerEnter`. Ver "ANIM-01 Candidate Survey" y "Code Examples". |
| ANIM-02 | Animación por código en al menos un elemento | Candidato primario: bob senoidal de `currentHeldVisual` en `PlayerPickup.Update`, escalado por `EffectIntensity`. Reusa patrón `AnimateDrinkSip` / `HUDController` lerp. Ver "ANIM-02 Candidate Survey". |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `UnityEngine.Animator` | built-in (Unity 6000.3) | Reproduce el AnimatorController con estados/transiciones | API estándar para ANIM-01 [CITED: docs.unity3d.com/6000.3/Documentation/Manual/class-AnimatorController.html] |
| `UnityEditor.Animations.AnimatorController` | built-in (Editor) | Crear el `.controller`, `AddMotion`, `AddTransition`, `AddCondition` por script si se quiere | Permite generar el asset sin clicks; es **Editor-only** [CITED: docs.unity3d.com/ScriptReference/Animations.AnimatorController.html] |
| `AnimationClip` + `AnimationClip.SetCurve` | built-in (Editor) | Crear el clip de rotación de la puerta por código | `SetCurve` es **Editor-only** para clips no-legacy [CITED: docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationClip.SetCurve.html] |
| `MaterialPropertyBlock` | built-in | Animar emisión/color por código sin instanciar materiales | El proyecto YA lo usa en `PickupItem`/`CarryableObject` para highlight |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Mathf.Sin` / `Mathf.PingPong` | built-in | Movimiento senoidal/oscilante para ANIM-02 | Bob del objeto sostenido, pulso de barra |
| `AnimationCurve` | built-in | Curva de ease para el clip o para suavizar código | Easing del clip de puerta |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| AnimatorController para la puerta | Solo código (Quaternion.Slerp en coroutine) | Cumpliría una animación pero **NO satisface ANIM-01** (ANIM-01 exige Animator/state machine explícitamente) |
| Puerta como elemento ANIM-01 | Indicador del HUD con Animator (RectTransform) | Posible pero el HUD se construye 100% por código BeforeSceneLoad; meter un Animator+clip ahí es más friccionante que una puerta en escena |
| `manage_animation` (UnityMCP) | Autoría manual en el editor | El servidor UnityMCP está conectado; si expone `manage_animation` puede crear controller/clip sin editor manual. **No verificado** que ese tool exista en esta versión — confirmar al planear (ver Open Questions) |

**Installation:** Ninguna. Todo es API built-in de Unity 6 / URP 17.3. No se instalan paquetes externos → **no aplica Package Legitimacy Audit** (no hay paquetes de terceros).

## ANIM-01 Candidate Survey (Animator)

| Candidato | GameObject/Prefab | Parámetro Animator | Disparo desde código existente | Clip: editor o script | Veredicto |
|-----------|-------------------|--------------------|--------------------------------|------------------------|-----------|
| **Puerta Bar/Home** (recomendado) | Puerta visible en escena City junto al `BarDoorTrigger` / `CityHomeDoorTrigger` (o una puerta nueva con su `MeshRenderer`) | `Open` (Trigger o Bool) | `BarDoorTrigger.OnTriggerEnter` / `CityHomeDoorTrigger.OnTriggerEnter` → `animator.SetTrigger("Open")` antes de cargar escena | Rotación Y de bisagra (0°→90°): autorear en editor (Animation window) o por script `SetCurve("localEulerAnglesRaw.y")` | **PRIMARIO** |
| Auto (faros/capó/puerta del auto) | `Car_Sedan.prefab` | `Honk`/`Open` | `CarEnterExit`/`PlayerCarController` (Command pattern) | Editor | Viable pero el auto comparte transform con física → riesgo de fighting (Pitfall 1) |
| Indicador HUD (Animator en RectTransform) | `HUDController` Canvas (construido por código) | `Pulse` (Bool) | `HUDController.HandleAlcoholChanged` | Editor sobre prefab de UI | Friccionante: el HUD es 100% código; agregar un prefab con Animator rompe el patrón |
| Personaje walk/idle | `player.prefab` | `IsMoving` (Bool) desde `PlayerMovement` | `PlayerMovement.Update` (velocidad) | **Requiere rig + clip humanoide** | **DESCARTADO**: el player es una cápsula primitiva (mesh 10208), sin SkinnedMeshRenderer ni rig — no hay esqueleto para animar |

**Por qué la puerta:** es el camino de menor resistencia que cumple ANIM-01 literalmente (state machine real, ≥2 estados, transición condicional), engancha en un evento que **ya existe** (`OnTriggerEnter` del DoorTrigger), es visiblemente inequívoco (una puerta que se abre) y no compite con física ni con scripts que escriban su transform (la puerta no la mueve nadie más). Único cuidado: el trigger hoy carga la escena de inmediato (ver Pitfall 3).

### Detalle de implementación ANIM-01 (puerta)
1. Asset: `Assets/_Project/Animations/Door.controller` con dos estados:
   - `Closed` (default) — clip `DoorClosed` (1 frame, rotación 0°) o estado vacío.
   - `Open` — clip `DoorOpen` (anima `localRotation.y` de 0° a ~95° en ~0.5s con ease-out).
   - Transición `Closed → Open` con condición sobre el parámetro `Open` (Trigger), `Has Exit Time = false`.
   - (Opcional) `Open → Closed` para reset entre partidas.
2. GameObject: la malla de la puerta debe estar **parentada a un pivote en la bisagra** (la rotación es sobre el borde, no el centro). El `Animator` va en el GameObject que rota.
3. Código (engancha en trigger existente):
   ```csharp
   // En BarDoorTrigger / CityHomeDoorTrigger, ANTES de cargar escena:
   [SerializeField] private Animator doorAnimator; // arrastrar la puerta en el Inspector
   ...
   if (doorAnimator != null) doorAnimator.SetTrigger("Open");
   // luego retardar la carga de escena ~0.6s para que la animación se vea (Pitfall 3)
   ```

## ANIM-02 Candidate Survey (código puro, sin AnimationClip)

| Candidato | Script/método existente a enganchar | Qué se modifica | Distinguible | Veredicto |
|-----------|-------------------------------------|-----------------|--------------|-----------|
| **Bob del objeto sostenido** (recomendado) | `PlayerPickup.Update` (sobre `currentHeldVisual.transform.localPosition`) | `localPosition.y` += `Mathf.Sin(Time.time * f) * amp`, con `amp` escalado por `drunkManager.EffectIntensity` | Sí — el trago/objeto tambalea más cuanto más borracho | **PRIMARIO** |
| Pulso de la barra de borrachera | `HUDController.Update` (sobre `fillImage` o su RectTransform/color) | escala o alpha pulsante cuando `targetFillAmount` alto | Sí — barra "late" cuando estás muy borracho | Alternativa fuerte (refuerza el tema) |
| Wobble UI al quedar sin dinero | `PlayerPickup.Update` (rama `!CanAfford` ya existe) → notificar a `HUDController` | shake de `moneyText` RectTransform por unos frames | Sí pero requiere coroutine + canal de evento nuevo | Más trabajo (cruza dos clases) |
| Rotación/idle del objeto vendible resaltado | `CarryableObject` cuando `isHighlighted` | rotación lenta `transform.Rotate` en Update | Sí | Viable pero menos central que el objeto en mano |

**Por qué el bob del objeto sostenido:** (a) reusa el patrón EXACTO ya presente en `PlayerPickup.AnimateDrinkSip` (modificar `localPosition`/`localRotation` por frame), (b) tiene un hook natural en `PlayerPickup.Update` que ya corre cada frame, (c) `PlayerPickup` ya cachea `drunkManager` en `Awake`, así que escalar por `EffectIntensity` es inmediato y temáticamente perfecto, (d) **no usa ningún AnimationClip** — cumple ANIM-02 literalmente, (e) coexiste con `AnimateDrinkSip` si se respeta `isDrinkingSip` (no aplicar bob mientras la coroutine de sorbo controla el transform — ver Pitfall 4).

### Detalle de implementación ANIM-02 (bob)
```csharp
// En PlayerPickup.Update, después de UpdateSelectionByLook():
if (currentHeldVisual != null && !isDrinkingSip)
{
    // Bob senoidal escalado por borrachera (patrón EffectIntensity del proyecto).
    float intensidad = drunkManager != null ? drunkManager.EffectIntensity : 0f;
    float amplitud = Mathf.Lerp(0.005f, 0.04f, intensidad);
    float frecuencia = Mathf.Lerp(2f, 6f, intensidad);
    Vector3 baseLocal = Vector3.zero; // el holdPoint hijo arranca en localPosition cero
    float offsetY = Mathf.Sin(Time.time * frecuencia) * amplitud;
    float offsetX = Mathf.Cos(Time.time * frecuencia * 0.7f) * amplitud * 0.5f;
    currentHeldVisual.transform.localPosition = baseLocal + new Vector3(offsetX, offsetY, 0f);
}
```
> Nota: `AnimateDrinkSip` escribe `localPosition` partiendo del valor actual; aplicar el bob solo cuando `!isDrinkingSip` evita pelearse con la coroutine de sorbo. Cuando termina el sorbo, devuelve al objeto a `startPosition` (cero), por lo que el bob continúa limpio.

## Unity 6 / URP Specifics

### AnimatorController + AnimationClip workflow
- **Crear el controller por script (Editor-only):** `UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path)`; luego `controller.AddParameter("Open", AnimatorControllerParameterType.Trigger)`, `rootStateMachine.AddState("Open")`, `closed.AddTransition(open)`, `transition.AddCondition(AnimatorConditionMode.If, 0, "Open")`. [CITED: docs.unity3d.com/ScriptReference/Animations.AnimatorController.html]
- **Crear el clip por script:** `AnimationClip clip = new(); clip.SetCurve("", typeof(Transform), "localEulerAngles.y", curve);` — **`SetCurve` es Editor-only** para clips no-legacy. [CITED: docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationClip.SetCurve.html]
- **Autoría manual (alternativa simple):** Animation window → Create → grabar la rotación de la puerta en dos keyframes. Para un MVP de 2 keyframes esto es más rápido que scriptearlo.
- **Asignar clip al estado:** `state.motion = clip;` o `AnimatorController.AddMotion(clip)`.
- **Cualquier script que genere assets va en `Assets/_Project/Editor/`** (como `CityBuilder`), porque `UnityEditor.*` no compila en builds.

### URP material property names (para animación de material por código)
- URP Lit usa **`_BaseColor`** (no `_Color` del Standard legacy) y **`_EmissionColor`** para emisión; requiere `material.EnableKeyword("_EMISSION")`. [VERIFIED: docs.unity3d.com/6000.1/Documentation/ScriptReference/Material.SetColor.html] [CITED: gamedevcheatsheet.com/shader-properties]
- El proyecto **ya hace esto bien** en `PickupItem.Awake`/`CarryableObject.Awake` (`EnableKeyword("_EMISSION")`, `RealtimeEmissive`) y anima `_EmissionColor` vía `MaterialPropertyBlock` en `SetHighlighted`. Si una animación por código toca material, **usar `MaterialPropertyBlock`** (no `renderer.material`, que instancia y filtra memoria).
- Para hot paths, cachear `Shader.PropertyToID("_BaseColor")` en lugar de pasar el string cada frame. [CITED: gamedevcheatsheet.com/shader-properties]

### Animator vs. código que escribe transforms (evitar pelea)
- El Animator, cuando un estado anima una propiedad, **sobrescribe** lo que un script escriba en esa misma propiedad ese frame. Por eso ANIM-01 (puerta) y ANIM-02 (objeto en mano) están **deliberadamente en objetos distintos** para no competir. [CITED: docs.unity3d.com/6000.4/Documentation/ScriptReference/Animator.html]
- Si el Animator se pusiera sobre el player o el auto (que mueven su transform por código/física), habría fighting: el Animator ganaría la propiedad animada cada frame. **No poner el Animator de la puerta sobre un objeto que también mueve un script de gameplay.**
- **Root Motion:** desactivar `Apply Root Motion` en el Animator de la puerta — no queremos que la animación desplace el GameObject; solo rota localmente.
- **Write Defaults:** mantener consistente entre estados (todos on o todos off) para evitar que propiedades no animadas queden en valores indeterminados. Para un controller de 2 estados con un solo clip, dejar el default de Unity está bien.

## Architecture Patterns

### Pattern 1: Consumidor de EffectIntensity (ya establecido)
**What:** Cada efecto de borrachera cachea `DrunkManager` en `Awake`, lee `EffectIntensity` por frame, multiplica su distorsión.
**When to use:** ANIM-02 debería seguirlo para que la animación por código sea coherente con el resto del juego.
**Example:**
```csharp
// Patrón del proyecto (DrunkManager + consumidores):
DrunkManager drunkManager;
void Awake() { drunkManager = GetComponent<DrunkManager>() ?? FindFirstObjectByType<DrunkManager>(); }
void Update() { float k = drunkManager != null ? drunkManager.EffectIntensity : 0f; /* aplicar k */ }
```

### Pattern 2: Animación por código vía coroutine de pose (ya presente)
**What:** `PlayerPickup.AnimateHeldDrinkPose` lerpea `localPosition`/`localRotation` con smoothstep entre dos poses.
**When to use:** Plantilla directa si la animación por código necesita un evento puntual (no continuo).
**Example:** ver `PlayerPickup.cs:330-355` (no recopiar; ya existe en el repo).

### Recommended Project Structure
```
Assets/_Project/
├── Animations/                 # NUEVO: Door.controller, DoorOpen.anim (assets ANIM-01)
├── Gameplay/Player/PlayerPickup.cs   # MODIFICAR: bob ANIM-02 en Update
├── Core/SceneManagement/BarDoorTrigger.cs        # MODIFICAR: SetTrigger("Open") + retardo
├── Core/SceneManagement/CityHomeDoorTrigger.cs   # MODIFICAR igual (si la puerta es ahí)
└── Editor/                     # (opcional) script que genera Door.controller
```

### Anti-Patterns to Avoid
- **Poner el Animator sobre el player/auto:** pelea con scripts de movimiento/física.
- **Usar `renderer.material` para animar color:** instancia el material y filtra memoria; usar `MaterialPropertyBlock` (patrón del proyecto).
- **Hacer ANIM-02 con un AnimationClip:** viola ANIM-02 (debe ser "sin ningún AnimationClip").
- **Cargar la escena en el mismo frame que disparás la animación de puerta:** la animación nunca se ve (Pitfall 3).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Máquina de estados de animación | Un enum + switch que lerpea poses | `Animator` + `AnimatorController` | ANIM-01 EXIGE Animator; además el editor visualiza estados/transiciones |
| Curva de ease del clip | Tablas de interpolación a mano | `AnimationCurve` / smoothstep (`t*t*(3-2t)`, ya usado) | Built-in, legible |
| Animar emisión de material | `new Material(...)` por objeto | `MaterialPropertyBlock` (ya en el proyecto) | Sin fugas de memoria, sin romper SRP batcher |
| Generar el `.controller` a mano si querés reproducibilidad | YAML del controller editado a mano | `AnimatorController` API en `Editor/` | El YAML del controller es frágil; la API es la fuente soportada |

**Key insight:** El proyecto ya resolvió la mitad de Phase 4 sin saberlo. ANIM-02 es reusar `EffectIntensity` + el patrón de pose por coroutine. El único componente realmente nuevo es el `Animator`/`.controller` de la puerta para ANIM-01.

## Runtime State Inventory

> Esta es una fase mayormente aditiva (nuevos assets + edits de código), no un rename. Aun así:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Ninguno — verificado: ANIM-01/02 no persisten estado entre escenas | None |
| Live service config | Ninguno — proyecto local Unity, sin servicios externos | None |
| OS-registered state | Ninguno | None |
| Secrets/env vars | Ninguno | None |
| Build artifacts | Si se crea un script en `Editor/` que usa `UnityEditor.Animations`, **NO debe referenciarse desde código de runtime** (no compila en build). El `Door.controller` y `.anim` son assets normales, sí se incluyen en build. | Verificar que el código que dispara `SetTrigger` es runtime puro |

## Common Pitfalls

### Pitfall 1: Animator peleando con scripts/física de transform
**What goes wrong:** Si el Animator anima la rotación/posición de un objeto que también mueve un script o un Rigidbody, el Animator gana cada frame y el otro movimiento "no funciona".
**Why it happens:** El Animator escribe las propiedades animadas en LateUpdate, pisando lo que escribió el script en Update.
**How to avoid:** Poner el Animator sobre un objeto dedicado (la puerta) que **nadie más mueve**. No sobre player/auto.
**Warning signs:** El objeto "salta" a una pose fija o ignora movimiento de gameplay.

### Pitfall 2: `SetCurve`/`AnimatorController` API son Editor-only
**What goes wrong:** Generar el clip/controller por código en un script de runtime → no compila en build (errores `UnityEditor` no encontrado).
**Why it happens:** `UnityEditor.Animations` y `AnimationClip.SetCurve` viven en el ensamblado del Editor.
**How to avoid:** Cualquier generación de assets va en `Assets/_Project/Editor/`. En runtime solo se referencian los assets ya creados (el `.controller`) y se setea el parámetro (`Animator.SetTrigger`).
**Warning signs:** `read_console` muestra errores de compilación con `UnityEditor`. (CLAUDE.md / UnityMCP: chequear consola tras crear scripts.)

### Pitfall 3: La puerta se anima pero la escena ya cargó (no se ve)
**What goes wrong:** `BarDoorTrigger.OnTriggerEnter` hace `SceneManager.LoadSceneAsync` **en el mismo frame**; la animación de la puerta nunca es visible porque la City se descarga al instante.
**Why it happens:** El trigger actual está diseñado para transición inmediata.
**How to avoid:** Retardar la carga: disparar `SetTrigger("Open")`, esperar ~0.5–0.7s (coroutine `WaitForSeconds`) y recién entonces cargar. Mantener el flag `triggered=true` para no re-disparar (ya existe). **Alternativa más segura para ANIM-01:** usar una puerta **decorativa** que se abre al acercarse SIN cargar escena (p.ej. una puerta interior del Bar o un portón en City que solo es visual), desacoplando ANIM-01 del flujo de carga. Recomendado para evitar tocar el camino crítico de transición de escenas.
**Warning signs:** En Play Mode la escena cambia sin ver la puerta abrirse.

### Pitfall 4: Bob de ANIM-02 peleando con la coroutine de sorbo
**What goes wrong:** `AnimateDrinkSip` ya escribe `localPosition`/`localRotation` del objeto sostenido; si el bob también escribe en los mismos frames, el sorbo tiembla o se ve mal.
**Why it happens:** Dos escritores del mismo transform en el mismo frame.
**How to avoid:** Aplicar el bob solo cuando `!isDrinkingSip` (campo ya existente). La coroutine devuelve el objeto a `localPosition = cero` al terminar, así el bob retoma limpio.
**Warning signs:** El trago "vibra" durante el sorbo.

### Pitfall 5: Animar RectTransform de UI vs. transform de mundo
**What goes wrong:** Si ANIM-02 fuera el pulso del HUD, animar `transform.localScale` de un elemento UI funciona, pero animar `position` mundial de un elemento de Canvas Overlay no tiene efecto esperado (UI usa `anchoredPosition`).
**Why it happens:** UI vive en espacio de Canvas, no de mundo.
**How to avoid:** Para UI usar `RectTransform.localScale`, `anchoredPosition` o color/alpha. (El bob del objeto sostenido es transform de mundo → no aplica, otra razón para preferirlo.)

## Code Examples

### ANIM-01: generar el controller por script (Editor-only, opcional)
```csharp
// Source: docs.unity3d.com/ScriptReference/Animations.AnimatorController.html
// Colocar en Assets/_Project/Editor/  (NO runtime)
using UnityEditor.Animations;
using UnityEngine;

var controller = AnimatorController.CreateAnimatorControllerAtPath(
    "Assets/_Project/Animations/Door.controller");
controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);
var sm = controller.layers[0].stateMachine;
var closed = sm.AddState("Closed");   // estado default
var open   = sm.AddState("Open");
open.motion = doorOpenClip;           // AnimationClip autoreado o por SetCurve
var t = closed.AddTransition(open);
t.AddCondition(AnimatorConditionMode.If, 0f, "Open");
t.hasExitTime = false;
sm.defaultState = closed;
```

### ANIM-01: disparo runtime desde el trigger existente (con retardo de carga)
```csharp
// En BarDoorTrigger / puerta decorativa. Runtime puro (sin UnityEditor).
[SerializeField] private Animator doorAnimator;

void OnTriggerEnter(Collider other)
{
    if (triggered || !other.CompareTag(playerTag)) return;
    triggered = true;
    if (doorAnimator != null) doorAnimator.SetTrigger("Open");
    StartCoroutine(LoadAfterDoor());   // ver Pitfall 3
}

System.Collections.IEnumerator LoadAfterDoor()
{
    yield return new WaitForSeconds(0.6f);
    // ... lógica de carga de escena existente ...
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `material.SetColor("_Color", ...)` (Standard) | `_BaseColor` / `_EmissionColor` (URP Lit) | URP | El proyecto ya usa `_EmissionColor` correctamente |
| `FindObjectOfType` | `FindFirstObjectByType` | Unity 2023+/6 | El proyecto ya usa `FindFirstObjectByType` |

**Deprecated/outdated:**
- Legacy `Animation` component (clips legacy con `SetCurve` en runtime): no usar; usar `Animator` + `AnimatorController`.

## Validation Architecture

> `nyquist_validation: true` en config. No hay framework de tests (CLAUDE.md: test-framework instalado pero sin tests ni asmdefs). La validación de esta fase es **manual en Play Mode** — apropiado para animaciones visuales.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Ninguno automatizado (manual Play Mode). UnityMCP `manage_editor` puede entrar/salir de Play Mode y `read_console` puede chequear errores. |
| Config file | none |
| Quick run command | Entrar a Play Mode en la escena relevante (Bar/City/Home) vía Editor o UnityMCP `manage_editor` |
| Full suite command | Recorrido manual: tomar trago → ver bob; acercarse a puerta → ver Animator abrir |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Verificación | File Exists? |
|--------|----------|-----------|--------------|-------------|
| ANIM-01 | Animator con ≥2 estados + transición condicional sobre puerta visible | manual Play Mode | Acercarse a la puerta → la puerta rota de Closed a Open; inspeccionar Animator window muestra 2 estados + transición con condición | ❌ assets a crear (Wave 0/1) |
| ANIM-02 | Objeto sostenido se anima por código (bob), sin AnimationClip | manual Play Mode | Comprar/sostener trago → oscila en la mano; más borracho → más tambaleo; confirmar que NO hay AnimationClip involucrado | ❌ código a agregar |
| (ambos) | Ambas animaciones visiblemente distinguibles sin inspeccionar | manual Play Mode | En una sesión: el bob del trago y la apertura de puerta son obvios a simple vista | — |

### Sampling Rate
- **Per task commit:** Entrar a Play Mode, `read_console` sin errores de compilación.
- **Per wave merge:** Verificación visual del comportamiento de la wave (puerta o bob).
- **Phase gate:** Recorrido completo: trago en mano oscilando + puerta abriéndose por Animator, ambos distinguibles.

### Wave 0 Gaps
- [ ] `Assets/_Project/Animations/Door.controller` — AnimatorController con estados Closed/Open + transición por parámetro `Open` (ANIM-01).
- [ ] `Assets/_Project/Animations/DoorOpen.anim` — clip de rotación de bisagra (editor o script).
- [ ] Puerta con pivote en bisagra + `Animator` en escena (Bar interior, o puerta decorativa en City) — referencia serializada en el trigger.
- [ ] (Si se genera por script) `Assets/_Project/Editor/DoorAnimatorBuilder.cs`.
- [ ] Sin framework de tests a instalar — validación manual es la política del proyecto.

## Security Domain

No aplica de forma sustantiva: fase puramente visual/cliente, sin entrada de usuario no confiable, sin red, sin persistencia sensible. `security_enforcement` no configurado en config.json (sin `security_enforcement: false` explícito, pero el dominio es un juego single-player local sin superficie de ataque). Sin categorías ASVS relevantes (no auth, no input externo, no cripto, no datos).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor 6000.3.11f1 | Todo | ✓ (asumido, es el entorno del proyecto) | 6000.3.11f1 | — |
| URP 17.3 | Materiales | ✓ | 17.3 | — |
| `UnityEditor.Animations` API | Generar controller por script (opcional) | ✓ (Editor) | built-in | Autoría manual en Animation window |
| UnityMCP `manage_animation` | Crear assets sin editor manual | ✗ no verificado | — | Autoría manual / `AnimatorController` API en Editor/ |

**Missing dependencies with fallback:**
- `manage_animation` (UnityMCP) no confirmado en esta versión → fallback: crear el controller/clip por Animation window o por script `Editor/`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | El player es una cápsula primitiva sin rig (mesh built-in 10208) | ANIM-01 survey | Bajo — verificado por grep del prefab; si hubiera un rig importado aparte, un walk/idle sería viable pero igual la puerta es más simple |
| A2 | UnityMCP no expone `manage_animation` en esta versión | Standard Stack / Env | Bajo — solo afecta el método de creación del asset, no el diseño; fallback manual existe |
| A3 | El AnimatorController generado por `AnimatorController` API funciona idéntico en 6000.3 que en docs 6000.x | Code Examples | Bajo — API estable desde hace años |
| A4 | Retardar la carga de escena ~0.6s no rompe el flujo de `triggered`/CarStateStore | Pitfall 3 | Medio — verificar que `CarStateStore.Save` y el flag se mantienen; preferible puerta decorativa desacoplada |

## Open Questions

1. **¿Qué puerta concreta recibe el Animator?**
   - What we know: hay triggers de puerta a Bar y a Home en City; ambos cargan escena al instante.
   - What's unclear: si conviene animar una de esas (tocando el camino crítico de carga) o una puerta **decorativa** desacoplada.
   - Recommendation: **puerta decorativa que se abre al acercarse sin cargar escena** — cumple ANIM-01, no toca el flujo de transición, cero riesgo de regresión. Decidir en discuss/plan.

2. **¿Crear el `.controller` por script (Editor/) o a mano en Animation window?**
   - What we know: ambas funcionan; el MVP necesita 2 estados y 1 transición.
   - What's unclear: preferencia de reproducibilidad vs. velocidad.
   - Recommendation: para un controller tan chico, autoría manual es más rápida; scriptearlo solo si se quiere reproducibilidad/versionado limpio.

3. **¿UnityMCP `manage_animation` está disponible?**
   - Recommendation: al planear, listar las tools del servidor UnityMCP conectado; si existe, úsalo para crear controller+clip; si no, fallback manual.

## Sources

### Primary (HIGH confidence)
- Codebase (lectura directa): `PlayerPickup.cs`, `HUDController.cs`, `DrunkManager.cs`, `PickupItem.cs`, `CarryableObject.cs`, `BarDoorTrigger.cs`, `CityHomeDoorTrigger.cs`, `player.prefab` (mesh 10208 = cápsula primitiva, sin Animator/rig). Verificado: cero `.controller`/`.anim` fuera de ThirdParty.
- [docs.unity3d.com/ScriptReference/Animations.AnimatorController.html] — AnimatorController API (AddState/AddTransition/AddCondition)
- [docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationClip.SetCurve.html] — SetCurve es Editor-only
- [docs.unity3d.com/6000.1/Documentation/ScriptReference/Material.SetColor.html] — `_BaseColor`/`_EmissionColor`
- [docs.unity3d.com/6000.3/Documentation/Manual/class-AnimatorController.html] — AnimatorController manual (6000.3)
- [docs.unity3d.com/6000.4/Documentation/ScriptReference/Animator.html] — Animator (override de propiedades, root motion)

### Secondary (MEDIUM confidence)
- [gamedevcheatsheet.com/shader-properties] — tabla de property names URP/Standard (cross-verificada con docs de Material.SetColor)
- [discussions.unity.com/t/...652348] — Animator y reset de transform (contexto de fighting)

### Tertiary (LOW confidence)
- Disponibilidad de UnityMCP `manage_animation` — no encontrada documentación específica; marcar para verificar al planear.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — todo built-in, APIs verificadas en docs oficiales.
- Architecture (candidatos ANIM-01/02): HIGH — derivado de lectura directa del código; los hooks existen literalmente.
- Pitfalls: HIGH — fundados en el comportamiento real del código (triggers cargan escena al instante; coroutine de sorbo escribe el mismo transform).

**Research date:** 2026-06-24
**Valid until:** ~2026-07-24 (estable; APIs de animación de Unity 6 son maduras)
