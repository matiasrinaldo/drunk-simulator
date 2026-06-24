# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

Juego de Unity ("drunk simulator") para la electiva Introducción al Desarrollo de Videojuegos (ITBA). El jugador toma tragos en un bar, se emborracha y conduce/camina por una ciudad mientras los efectos del alcohol distorsionan el control y la cámara. Los comentarios y mensajes del proyecto están en español.

## Entorno y comandos

- **Unity Editor `6000.3.11f1`** (ver `ProjectSettings/ProjectVersion.txt`). Usar exactamente esa versión.
- **Render pipeline:** URP 17.3 (`com.unity.render-pipelines.universal`).
- No hay scripts de build por CLI ni Makefile: se abre el proyecto en el Editor y se corre con Play, o se buildea desde *File → Build Settings*.
- **Input:** el código usa la **API legacy** (`UnityEngine.Input`, `Input.GetKeyDown`, `KeyCode`), no el Input System nuevo aunque el paquete esté instalado. Mantené ese estilo al agregar input.
- **Tests:** `com.unity.test-framework` está instalado pero **no hay tests ni assembly definitions** todavía. Si se agregan, correrlos vía *Window → General → Test Runner*.

### Archivos generados (no editar a mano, ignorados por git)
`Library/`, `Temp/`, `Logs/`, `UserSettings/`, los `*.csproj`/`*.slnx`/`*.sln`. **Ojo:** `.gitignore` también ignora `*.md` y `CLAUDE.md`, así que este archivo no se versiona.

## Estructura

Todo el código propio vive en `Assets/_Project/`, organizado por responsabilidad (no por tipo):

- `Core/Managers/` — estado central de juego (`DrunkManager`, `PlayerCarController`).
- `Core/Audio/` — `BackgroundMusicManager`.
- `Core/SceneManagement/` — triggers de puertas, spawn, y stores de persistencia entre escenas.
- `Gameplay/Player/`, `Gameplay/Vehicles/`, `Gameplay/Items/`, `Gameplay/Systems/` — comportamiento de gameplay.
- `Patterns/Command/` — patrón Command (ver abajo).
- `Editor/` — herramientas de editor (`CityBuilder`).
- `Prefabs/`, `ScriptableObjects/`, `UI/`, `Audio/`.

Las clases están en el **namespace global** (sin `namespace`). Mantené esa convención.

Escenas en `Assets/Scenes/`: **City**, **Bar**, **Home**. Assets de terceros (Kenney CityKit, ADG textures) en `Assets/ThirdParty/`.

## Arquitectura (lo que cruza varios archivos)

### Estado de embriaguez — `DrunkManager`
Es la fuente de verdad del nivel de alcohol. Mantiene `alcoholLevel` (0..`maxLevel`) y expone `EffectIntensity = pow(NormalizedLevel, effectExponent)` — un escalar 0→1 con curva no lineal. **Los efectos no viven en el manager**: cada consumidor lee `EffectIntensity` cada frame y aplica su propia distorsión senoidal:
- `PlayerMovement` — sway lateral, drift de input y penalización de velocidad.
- `MouseLook` — wobble de pitch/yaw/roll de cámara FPS.
- `CarController` — drift de dirección/acelerador y jitter de torque.
- `CarFollowCamera` — roll/pitch/yaw y offset de mirada de la cámara del auto.

Al agregar un efecto nuevo, seguí este patrón: cachear `DrunkManager` en `Awake` (con fallback `FindFirstObjectByType`), leer `EffectIntensity`, multiplicar. El alcohol se agrega vía `AddAlcohol`/`AddBeer`; `DrunkManager` dispara `OnAlcoholLevelChanged`.

### Tomar y conducir
- `PlayerPickup` (raycast desde la cámara + overlap de proximidad) resalta y agarra ítems. Tragos (`PickupItem`) se beben en sorbos → `DrunkManager.AddAlcohol(AlcoholPerSip)`. El tipo de trago se resuelve por nombre del objeto (`ResolvedPickupType`).
- `PlayerCarController` alterna jugador↔auto: desactiva el GameObject del jugador, conmuta cámaras y **gestiona los `AudioListener`** para que nunca haya dos activos ni se corte la música al entrar al auto.

### Patrón Command
Entrar/salir del auto no se ejecuta directo: `CarEnterExit`/`PlayerCarController` encolan `EnterCarCommand`/`ExitCarCommand` en `CommandQueue` (un `MonoBehaviour` que desencola **un comando por `Update`**). Acciones de gameplay diferibles deberían pasar por acá implementando `ICommand`.

### Persistencia entre escenas (clave)
Cada puerta hace `SceneManager.LoadSceneAsync` en modo **Single**, lo que reconstruye la escena de cero. Para que el mundo se sienta continuo, el estado vive en **clases estáticas en memoria** (sobreviven cargas de escena, se resetean al cerrar el juego):
- `PlayerSpawner.NextSpawnId` — id del `SpawnPoint` donde aparecer en la próxima escena.
- `CarStateStore` — posición/rotación del auto (guardada por los triggers antes de descargar la City).
- `DeliveredObjectsStore` — ids de `CarryableObject` ya tomados, para que no reaparezcan al recargar Home.
- `PlayerPickup.hasHeldObject` (campo `static`) — si el jugador lleva algo en mano.

Si en el futuro se agrega "Nueva partida", llamar a `.Clear()` de cada store. Al crear estado nuevo que deba cruzar escenas, seguí este patrón de store estático.

### Audio y carga de recursos
`BackgroundMusicManager` es un singleton que se autoarranca con `[RuntimeInitializeOnLoadMethod]` + `DontDestroyOnLoad`. Los clips se cargan con `Resources.Load` desde `Assets/Resources/` (música y `Audio/SFX/`), siempre con **rutas de fallback** (p. ej. `"Audio/Music/BackgroundMusic"` → `"Audio/BackgroundMusic"`). Mantené ese patrón de fallback al cargar recursos.

### Herramienta de editor
`CityBuilder` (menú **Drunk Simulator → Build City Layout**) genera el layout de City.unity proceduralmente con modelos de Kenney CityKit. Editar acá las posiciones de edificios/calle/estacionamientos, no a mano en la escena.
