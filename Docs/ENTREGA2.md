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

Además se agregó una escena de menú principal:

- `Assets/Scenes/MainMenu.unity` — primera escena del build, con la imagen `Assets/Art/UI/menu_drunk_simulator.png` como pantalla de inicio.
- `MainMenuController.cs` — dibuja la imagen a pantalla completa, detecta el click sobre el botón `JUGAR`, resetea stores de partida (`CarStateStore`, `DeliveredObjectsStore`, `DrunkLevelStore`, `WorldTimeStore`, etc.) y carga `Home` con `SceneManager.LoadSceneAsync`.

## 4. Escena asincrónica de carga de nivel ⚠️

Todas las transiciones del juego usan `SceneManager.LoadSceneAsync(...)`, de modo que la carga de niveles es **asincrónica**:

- Triggers de puerta (`BarDoorTrigger`, `HomeDoorTrigger`, `BarExitTrigger`, `CityHomeDoorTrigger`).
- `MainMenuController.StartGame()` (`MainMenuController.cs:48`) y `GameManager` al evaluar victoria/derrota (`GameManager.cs:25, 42, 57, 62`).

Lo que falta es una **escena de carga dedicada** (pantalla intermedia con barra de progreso) que se muestre entre niveles mientras corre el `AsyncOperation`.

> **Pendiente:** crear una escena `Loading.unity` que cargue la escena destino con `LoadSceneAsync`, lea `operation.progress` y muestre una barra de progreso, usando `allowSceneActivation = false` hasta llegar a `0.9`. Hoy la carga es async pero sin pantalla de transición.

## 5. Efecto de luces ✅

Las escenas tienen iluminación estática de base y ahora también efectos dinámicos por código:

| Escena | Direccionales (`Type 1`) | Puntuales (`Type 2`) |
| --- | --- | --- |
| Bar | 1 | 6 |
| Home | 1 | 3 |
| City | 1 | 1 |

**Ciclo día/noche ✅** — `WorldTimeStore.cs` guarda si el mundo está de día o de noche y si se vendió algo durante la visita actual al bar.

- `HomeDoorTrigger.cs:25` fuerza `WorldTimeStore.SetDay()` al salir de la casa hacia `City`.
- `BarDoorTrigger.cs:30` inicia una visita al bar con `WorldTimeStore.BeginBarVisit()`.
- `SellCounter.cs:78` marca `WorldTimeStore.MarkSoldInBar()` al vender un objeto.
- `BarExitTrigger.cs:27-29` cambia a noche con `WorldTimeStore.SetNight()` si el jugador vendió algo antes de salir del bar.

**Aplicación visual del ciclo ✅** — `WorldTimeLightingController.cs` se suscribe a `SceneManager.sceneLoaded` y aplica la iluminación al cargar `City`:

- `ApplyToScene()` (`WorldTimeLightingController.cs:32`) cambia `RenderSettings.ambientSkyColor`, `ambientEquatorColor`, `ambientGroundColor`, `ambientIntensity` y la luz direccional.
- `GetNightSkybox()` (`WorldTimeLightingController.cs:63`) crea un skybox procedural oscuro para la noche, evitando que el cielo siga celeste.
- `DynamicGI.UpdateEnvironment()` (`WorldTimeLightingController.cs:48`) refresca la iluminación ambiental luego de cambiar el skybox.

**Faros del auto ✅** — `CarController.cs` crea tres luces delanteras por código:

- `CreateHeadlights()` (`CarController.cs:318`) agrega dos faros laterales y una luz central larga para visibilidad en tercera persona.
- `UpdateHeadlights()` (`CarController.cs:348`) prende los faros sólo cuando `WorldTimeStore.CurrentTimeOfDay == WorldTimeOfDay.Night`.
- Los valores son deliberadamente altos (`centerHeadlightRange = 220`, `centerHeadlightIntensity = 180`) para que la calle se vea desde la cámara del auto.

## 6. Gravedad + sistema de partículas ✅

**Gravedad ✅** — Aplicada en dos sistemas independientes:

- Jugador a pie: `PlayerMovement.cs` integra gravedad manual sobre el `CharacterController` (`gravity = -9.8f`, acumulada en `yVelocity` en la línea 73 y aplicada en `controller.Move` en la línea 77; salto con `Mathf.Sqrt` en la línea 70).
- Auto: `Car_Sedan.prefab` usa un `Rigidbody` con `useGravity` activado y centro de masa bajado (`CarController.cs:73`) para estabilidad.

**Sistema de partículas ✅** — `CarController.cs` crea y maneja partículas por código mientras se conduce:

- `CreateSmokeParticles()` (`CarController.cs:177`) — humo del escape (cono, `simulationSpace = World`, `gravityModifier = -0.05f` para que el humo suba).
- `CreateSparkParticles()` (`CarController.cs:210`) — chispas amarillas en las ruedas al derrapar (`gravityModifier = 1.2f` para que caigan).
- `UpdateDrivingEffects()` (`CarController.cs:251`) regula `emission.rateOverTime` según la velocidad y el giro: humo al avanzar, chispas en las ruedas del lado hacia el que se dobla.

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
| 5 | Efecto de luces | ✅ |
| 6 | Gravedad + sistema de partículas | ✅ |
| 7 | Raycasting | ✅ |
