# Entrega 2 — Drunk Simulator

Documento que detalla cómo cumplimos cada consigna del TP, con referencias al código y a los assets relevantes.

> **Leyenda**
> *   ✅ Implementado
> *   ⚠️ Parcialmente implementado
> *   ❌ No implementado todavía

## 1. Patrón Flyweight ✅

Cada tipo de objeto comparte un único asset de definición (estado intrínseco) y las instancias de la escena sólo guardan una referencia a él, en vez de duplicar los datos. Las definiciones son `ScriptableObject` bajo `Assets/_Project/ScriptableObjects/Items/`:

- `DrinkDefinition.cs` — flyweight de bebidas: `DrinkName`, `Price`, `AlcoholPerSip`, `MaxSips`. Configurable desde el Inspector vía `[CreateAssetMenu]`.
- `SellableDefinition.cs` — flyweight de objetos vendibles: `ItemName`, `SellValue`.

Instancias compartidas (un asset por tipo) en `ScriptableObjects/Items/Instances/`:

| Flyweight | Instancias |
| --- | --- |
| `DrinkDefinition` | `Cerveza.asset`, `Trago.asset`, `Whisky.asset` |
| `SellableDefinition` | `TV.asset`, `Lampara.asset`, `Cuadro.asset` |

Consumidores que apuntan al flyweight en vez de replicar sus datos: `PickupItem.cs`, `CarryableObject.cs`, `SellCounter.cs` y `HeldObjectStore.cs`. Todas las cervezas de la escena comparten el mismo `Cerveza.asset`; cambiar el precio o el alcohol por sorbo en un solo lugar afecta a todas las instancias.

## 2. Animaciones (por código + sistema por estado) ⚠️

**Por código ✅** — La animación de beber un sorbo se resuelve enteramente por código en `PlayerPickup.cs`:

- `AnimateDrinkSip()` (línea 301) es una coroutine que sube el vaso a la boca, lo sostiene y lo baja.
- `AnimateHeldDrinkPose()` (línea 330) interpola pose con `Vector3.Lerp` (línea 346) y `Quaternion.Slerp` (línea 347), con suavizado *smoothstep* (`t * t * (3f - 2f * t)`).

Otras animaciones procedurales (no por Animator) que ya existían: el *wobble* de cámara (`MouseLook.cs`), el *sway* de movimiento (`PlayerMovement.cs`) y el seguimiento del auto con `SmoothDamp` (`CarFollowCamera.cs`).

**Sistema de animaciones por estado ❌** — No hay ningún `Animator`, `AnimatorController` (`.controller`) ni `AnimationClip` (`.anim`) propios en el proyecto. Ningún script referencia la clase `Animator`.

> **Pendiente:** crear un `AnimatorController` con una state machine para un elemento principal (p. ej. el jugador: `Idle → Walk → Drink`, o una puerta del bar con estados `Closed → Open`) y manejar las transiciones con parámetros (`SetBool`/`SetTrigger`) desde el código de gameplay.

## 3. Interfaz gráfica dentro del nivel (barra de vida / puntos) ✅

`HUDController.cs` construye por código un HUD persistente *in-game* (Canvas Screen Space Overlay) que sobrevive a las cargas de escena con el patrón singleton + `DontDestroyOnLoad` (auto-arranque con `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`).

| Elemento | Comportamiento | Referencia |
| --- | --- | --- |
| Barra de borrachera | `Image` tipo `Filled`, hace *lerp* del `fillAmount` hacia `DrunkManager.NormalizedLevel` | `HUDController.cs:190` |
| Indicador de dinero | `TextMeshProUGUI` suscripto a `PlayerMoneyStore.OnMoneyChanged` | `HUDController.cs:181` |

Se re-vincula al `DrunkManager` de cada escena en `OnSceneLoaded` (línea 152) y se oculta con `SetVisible(false)` al entrar al menú principal y a la pantalla de resultado para no taparlas.

## 4. Escena asincrónica de carga de nivel ⚠️

Todas las transiciones del juego usan `SceneManager.LoadSceneAsync(...)`, de modo que la carga de niveles es **asincrónica**:

- Triggers de puerta (`BarDoorTrigger`, `HomeDoorTrigger`, `BarExitTrigger`, `CityHomeDoorTrigger`).
- `MainMenuController.StartGame()` (`MainMenuController.cs:47`) y `GameManager` al evaluar victoria/derrota (`GameManager.cs:25, 42, 57, 62`).

Lo que falta es una **escena de carga dedicada** (pantalla intermedia con barra de progreso) que se muestre entre niveles mientras corre el `AsyncOperation`.

> **Pendiente:** crear una escena `Loading.unity` que cargue la escena destino con `LoadSceneAsync`, lea `operation.progress` y muestre una barra de progreso, usando `allowSceneActivation = false` hasta llegar a `0.9`. Hoy la carga es async pero sin pantalla de transición.

## 5. Efecto de luces ⚠️

Las escenas de interior tienen iluminación con luces puntuales que generan atmósfera de bar/casa, además de la direccional principal:

| Escena | Direccionales (`Type 1`) | Puntuales (`Type 2`) |
| --- | --- | --- |
| Bar | 1 | 6 |
| Home | 1 | 3 |
| City | 1 | 1 |

Es iluminación **estática** colocada en la escena: no hay ningún script que anime intensidad, color o posición de las luces (parpadeo, pulso al subir el alcohol, faros del auto, etc.).

> **Pendiente:** agregar un efecto de luz dinámico por código para que cuente como "efecto" claramente atribuible — por ejemplo, faros encendidos en el auto, un cartel de neón con *flicker*, o modular la luz/ambiente del mundo según `DrunkManager.EffectIntensity`.

## 6. Gravedad + sistema de partículas ✅

**Gravedad ✅** — Aplicada en dos sistemas independientes:

- Jugador a pie: `PlayerMovement.cs` integra gravedad manual sobre el `CharacterController` (`gravity = -9.8f`, acumulada en `yVelocity` en la línea 73 y aplicada en `controller.Move` en la línea 77; salto con `Mathf.Sqrt` en la línea 70).
- Auto: `Car_Sedan.prefab` usa un `Rigidbody` con `useGravity` activado y centro de masa bajado (`CarController.cs:58`) para estabilidad.

**Sistema de partículas ✅** — `CarController.cs` crea y maneja partículas por código mientras se conduce:

- `CreateSmokeParticles()` (línea 159) — humo del escape (cono, `simulationSpace = World`, `gravityModifier = -0.05f` para que el humo suba).
- `CreateSparkParticles()` (línea 192) — chispas en las cuatro ruedas al derrapar (`gravityModifier = 1.2f` para que caigan).
- `UpdateDrivingEffects()` (línea 233) regula `emission.rateOverTime` según la velocidad y el giro: humo al avanzar, chispas en las ruedas del lado hacia el que se dobla.

## 7. Raycasting (al menos 1 caso) ✅

`PlayerPickup.cs` usa raycasting para la selección de objetos a agarrar: dispara un `Physics.Raycast` desde la cámara hacia adelante y resuelve el `PickupItem` o `CarryableObject` impactado.

```
Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
if (Physics.Raycast(ray, out RaycastHit hit, selectionDistance, pickupLayerMask, QueryTriggerInteraction.Collide))
```

`PlayerPickup.cs:182-183` — usa `pickupLayerMask` para filtrar capas y `QueryTriggerInteraction.Collide` para detectar también colliders marcados como trigger. Como respaldo de proximidad, `FindClosestPickupInRange()` (línea 222) complementa con `Physics.OverlapSphere`.

## Resumen del estado

| # | Consigna | Estado |
| --- | --- | --- |
| 1 | Patrón Flyweight | ✅ |
| 2 | Animaciones (por código + por estado) | ⚠️ (falta state machine / Animator) |
| 3 | UI dentro del nivel (barra / puntos) | ✅ |
| 4 | Escena asincrónica de carga de nivel | ⚠️ (carga async sin escena de loading) |
| 5 | Efecto de luces | ⚠️ (luces estáticas, falta efecto dinámico) |
| 6 | Gravedad + sistema de partículas | ✅ |
| 7 | Raycasting | ✅ |
