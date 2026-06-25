# 🍻 Drunk Simulator

> *"Tomá. Tambaleá. Volvé a casa. Si podés."*

Un simulador absurdo en primera persona donde encarnás a un cliente de un bar.
Frente a vos hay cervezas, tragos y whisky. Cuanto más tomás, más se desfigura el
mundo: la cámara se balancea, tus pasos se desvían sin permiso, y agarrar un vaso
se vuelve una odisea. **El bar es divertido. Volver a casa es el verdadero desafío.**

No es un juego de beber. Es un juego *sobre* beber.

---

## Contexto

Proyecto desarrollado para la electiva **Introducción al Desarrollo de Videojuegos**
del [ITBA](https://www.itba.edu.ar/). Corresponde a la **Entrega 2** de la materia.

**Equipo:**
- Matías Sánchez Novelli
- María Otegui
- Matías Rinaldo

El proyecto está enteramente en español: comentarios, mensajes de commit y documentación interna.

## El juego

Un loop de gameplay corto y caótico, inspirado en simuladores absurdos como
*Surgeon Simulator*, *Goat Simulator* o *Drunkn Bar Fight*. La gracia está en el
caos físico: las mecánicas se rebelan contra el control a medida que subís de nivel
de alcohol.

1. **Agarrar un objeto de tu casa** — Empezás en casa. Buscá algo de valor para
   llevarte: ese objeto es tu boleto al bar.
2. **Manejar al bar** — Subite al auto y conducí hasta el bar.
3. **Vender el objeto** — En el bar, entregá el objeto para conseguir plata y tragos.
4. **Emborracharse** — Tomá cerveza, tragos y whisky. Subí el nivel de alcohol y
   dejá que el mundo se empiece a desfigurar.
5. **Volver a casa (vivo)** — El verdadero desafío: manejar de vuelta con la cámara
   que se balancea y los controles que te traicionan.

**Condición de victoria:** llegar a casa manejando, habiendo vendido los objetos y
con suficiente alcohol encima. Chocar el auto contra un obstáculo letal es derrota.

## Tecnología

- **Motor:** [Unity](https://unity.com/) `6000.3.11f1` (usar exactamente esa versión).
- **Render pipeline:** Universal Render Pipeline (URP) `17.3`.
- **Lenguaje:** C# (clases en namespace global, estilo de la materia).
- **Input:** API legacy de Unity (`UnityEngine.Input`).
- **Assets de terceros:**
  - [Kenney CityKit](https://kenney.nl/) — modelos low-poly de la ciudad.
  - ADG Textures — texturas.

## Cómo correrlo

No hay scripts de build por CLI. Se trabaja desde el Editor:

1. Abrir el proyecto con **Unity Editor `6000.3.11f1`**.
2. Cargar una escena de `Assets/Scenes/` y darle a **Play**, o buildear desde
   *File → Build Settings*.

**Escenas:** `MainMenu` → `Home` → `City` → `Bar` → `Result`. Cada puerta carga la
siguiente escena en modo *Single*, y el estado de partida (auto, alcohol, plata,
objetos vendidos, hora del mundo) persiste en clases estáticas en memoria entre cargas.

## Arquitectura

Todo el código propio vive en `Assets/_Project/`, organizado por responsabilidad:

- `Core/Managers/` — estado central de juego (`DrunkManager`, `GameManager`, `PlayerCarController`).
- `Core/SceneManagement/` — triggers de puertas, spawn y *stores* estáticos de persistencia entre escenas.
- `Core/Audio/` — música de fondo.
- `Gameplay/` — comportamiento del jugador, vehículos, ítems y sistemas.
- `Patterns/` — implementaciones de patrones de diseño (Command, Strategy, Event Queue).
- `Editor/` — herramientas de editor (`CityBuilder` genera el layout de la ciudad).

**El sistema de embriaguez** es el corazón del juego: `DrunkManager` mantiene el
nivel de alcohol y expone un escalar `EffectIntensity` (0→1, con curva no lineal).
Cada consumidor —movimiento del jugador, cámara FPS, control del auto, cámara del
auto— lee ese valor cada frame y aplica su propia distorsión senoidal. Así, beber
más significa más sway, más drift y más wobble en todo.

---

🍻 *¡Salud!*
