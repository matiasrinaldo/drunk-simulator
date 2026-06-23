# Drunk Simulator

## What This Is

Juego 3D en Unity (URP) para la electiva Introducción al Desarrollo de Videojuegos (ITBA). Sos un borracho que quiere tomar alcohol: agarrás objetos de tu casa, los vendés en el bar para conseguir dinero, comprás bebidas con ese dinero y volvés a tu casa **manejando borracho sin chocar**. Cada bebida aumenta el efecto de borrachera y distorsiona más el manejo. Ganás cuando vendiste todos los objetos de tu casa (alcanzando un nivel mínimo de borrachera); perdés si chocás contra una casa, árbol, niño o mascota.

## Core Value

El loop de dificultad creciente: tomar → más distorsión de control/cámara → manejar de vuelta a casa sin chocar se vuelve cada vez más difícil. Esa tensión es el corazón del juego; todo lo demás la sirve.

## Requirements

### Validated

<!-- Inferidos del código existente (ver .planning/codebase/). Funcionan y se confían. -->

- ✓ Sistema de embriaguez: `DrunkManager` como fuente de verdad, `EffectIntensity` con curva no lineal, distorsión senoidal por consumidor (`PlayerMovement`, `MouseLook`, `CarController`, `CarFollowCamera`) — existing
- ✓ Mecánica de tomar: `PickupItem` con sorbos y alcohol por tipo (cerveza=1, trago=2, whisky=3) — existing
- ✓ Sistema de pickup: raycast desde cámara + overlap de proximidad + highlight por emisión — existing (cumple TP2.7 Raycasting)
- ✓ Conducir auto + entrar/salir vía patrón Command + `CommandQueue` (event queue) — existing (cumple TP1.13)
- ✓ Persistencia entre escenas con stores estáticos (`CarStateStore`, `DeliveredObjectsStore`, `PlayerSpawner.NextSpawnId`, `PlayerPickup.hasHeldObject`) — existing
- ✓ Carga asincrónica de escenas entre City/Bar/Home (`LoadSceneAsync`) — existing (cumple TP1.8)
- ✓ Música de fondo + 2 SFX (`BackgroundMusic`, `DrinkSip`, `PayDrink`) — existing (cumple TP1.4)
- ✓ Objetos agarrables de la casa (`CarryableObject`) con persistencia de entregados — existing
- ✓ Patrón Manager (`DrunkManager`, `BackgroundMusicManager`, `PlayerCarController`) — existing (cumple parte de TP1.9)
- ✓ Gravedad sobre el auto (Rigidbody) — existing (cumple parte de TP2.6)
- ✓ Economía: objetos vendibles con valor (`SellableDefinition` Flyweight), venta en mostrador, dinero persistente (`PlayerMoneyStore`) — validated Phase 1
- ✓ Bebidas con precio real (`DrinkDefinition` Flyweight); compra descuenta dinero, rechazo con SFX si no alcanza; reemplaza el trueque — validated Phase 1
- ✓ Patrón Flyweight (TP2.1) como modelo de datos compartido para vendibles y bebidas — validated Phase 1

### Active

<!-- Milestone actual: TP2 + loop core mínimo. Hipótesis hasta shipear. -->

**Loop core (gameplay)**
- [x] Economía: cada objeto de la casa tiene un valor; venderlo da dinero (✓ Phase 1)
- [x] Las bebidas tienen precio; comprás con dinero (reemplaza el trueque actual) (✓ Phase 1)
- [ ] Condición de derrota: chocar contra casa/árbol/niño/mascota
- [ ] Condición de victoria: vender todos los objetos alcanzando un nivel mínimo de borrachera
- [ ] HUD in-level: dinero + barra de borrachera (cumple TP2.3)

**Objetivos técnicos TP2**
- [x] Patrón Flyweight (TP2.1) (✓ Phase 1)
- [ ] Animaciones por código + por estado en elementos principales (TP2.2)
- [ ] Sistema de partículas (TP2.6) — p.ej. choque, sorbo, venta
- [ ] Efecto de luces (TP2.5)
- [ ] Escena asincrónica de carga de nivel dedicada (TP2.4)

### Out of Scope

<!-- Diferido a un milestone posterior (pendientes de TP1), con razón. -->

- Migración a Cinemachine (TP1.5) — se mantienen cámaras custom por decisión; riesgo asumido en el objetivo
- Escena de menú principal (TP1.12) — diferido a milestone de pendientes TP1
- Patrón Strategy (TP1.9) — diferido a milestone de pendientes TP1
- UI con scrolling / layout group (TP1.10) — diferido a milestone de pendientes TP1
- 2 eventos adicionales para llegar a ≥3 (TP1.11) — se sumarán naturalmente con la economía/victoria, pero no es objetivo formal de este milestone
- Bajar a Unity 2022.3.35 LTS — la consigna lo pide pero el proyecto está en 6000.3.11f1; no se toca sin confirmación de la cátedra

## Context

- Proyecto brownfield ya mapeado en `.planning/codebase/` (STACK, ARCHITECTURE, STRUCTURE, CONVENTIONS, TESTING, INTEGRATIONS, CONCERNS).
- La consigna completa (TP1 y TP2) está en `consignas.md`. La auditoría detallada vive en este milestone.
- Mecánica actual a reemplazar: trueque puro — dejás un objeto y agarrás cualquier bebida (`PlayerPickup.cs`). Se reemplaza por economía con dinero.
- Las bebidas YA emborrachan distinto; lo que falta es el dinero/valor de objetos y precios.
- Concerns conocidos: stores estáticos frágiles, resolución de tipo de bebida por nombre, gestión de `AudioListener` al entrar/salir del auto, sin tests.

## Constraints

- **Tech stack**: Unity 6000.3.11f1 + URP 17.3 — versión fija del Editor
- **Input**: API legacy (`UnityEngine.Input`, `KeyCode`) — mantener el estilo aunque el paquete nuevo esté instalado
- **Convención**: clases en namespace global (sin `namespace`); comentarios y mensajes en español
- **Cámaras**: custom (`CarFollowCamera`, `MouseLook`), sin Cinemachine en este milestone
- **Académico**: entregable de la materia; la corrección incluye preguntas de comprensión por integrante

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Economía con dinero real + precios | Da profundidad: objetos de distinto valor habilitan distintas bebidas | — Pending |
| Mantener cámaras custom (sin Cinemachine) | Menos refactor; se asume el riesgo en TP1.5 (fuera de scope este milestone) | ⚠️ Revisit |
| Victoria = vender todo + nivel mínimo de borrachera | Refuerza el core value (hay que emborracharse, no solo vender) | — Pending |
| Choque (casa/árbol/niño/mascota) = derrota | Tensión del manejo borracho | — Pending |
| Scope = TP2 + loop core mínimo | Prioriza la entrega TP2 sin arrastrar todo TP1 | — Pending |
| Pendientes TP1 a milestone posterior | Cinemachine, menú, strategy, scrolling UI, eventos | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-23 — Phase 1 (Economía) complete*
