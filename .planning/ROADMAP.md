# Roadmap: Drunk Simulator — TP2 + loop core mínimo

## Overview

Partimos de un juego brownfield que ya tiene borrachera, pickup, auto con Command pattern y persistencia entre escenas. Este milestone completa el loop de gameplay: economía real (objetos con valor, bebidas con precio), condiciones de victoria y derrota, HUD in-level, animaciones, partículas, luces y una pantalla de carga dedicada. Cada fase entrega una capacidad jugable verificable en Play Mode antes de avanzar a la siguiente.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Economía** - Objetos vendibles con valor, bebidas con precio, y el patrón Flyweight como modelo de datos compartido
- [ ] **Phase 2: HUD in-level** - Barra de borrachera y contador de dinero siempre visibles durante el juego
- [ ] **Phase 3: Loop de victoria y derrota** - Detección de choque, condición de ganar vendiendo todo, y pantallas de resultado
- [ ] **Phase 4: Animaciones** - Animator state machine en un elemento principal y animación por código en otro
- [ ] **Phase 5: Efectos visuales y carga** - Partículas, luces y escena dedicada de carga asincrónica

## Phase Details

### Phase 1: Economía
**Goal**: El jugador puede vender objetos de su casa para obtener dinero y comprar bebidas con ese dinero en el bar
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: ECON-01, ECON-02, ECON-03, ECON-04, PAT-01
**Success Criteria** (what must be TRUE):
  1. Cada CarryableObject muestra un valor monetario configurable en el Inspector; objetos distintos pueden tener precios distintos
  2. Al entregar un objeto en el bar (trigger existente), el dinero del jugador aumenta en el valor del objeto y el total persiste cuando el jugador vuelve a City
  3. Cada PickupItem (bebida) muestra un precio; el jugador no puede tomar una bebida si no tiene dinero suficiente
  4. Comprar una bebida descuenta su precio del dinero del jugador (el trueque anterior ya no funciona)
  5. El Flyweight de definiciones (DrinkDefinition / SellableDefinition como ScriptableObject compartido) es la fuente de verdad de precio y alcohol; no hay valores duplicados hardcodeados en prefabs
**Plans**: 2 planes
Plans:
- [ ] 01-01-PLAN.md — Slice A: SellableDefinition + stores (PlayerMoneyStore, HeldObjectStore) + CarryableObject + SellCounter en Bar (vender objeto → dinero sube)
- [ ] 01-02-PLAN.md — Slice B: DrinkDefinition + migración PickupItem + compra-con-dinero en PlayerPickup + SFX de rechazo (comprar bebida con el dinero de A)

### Phase 2: HUD in-level
**Goal**: El jugador puede leer su nivel de borrachera y su dinero disponible en cualquier momento del juego
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: HUD-01, HUD-02
**Success Criteria** (what must be TRUE):
  1. Una barra de borrachera visible (Canvas en World Space o Screen Space Overlay) refleja en tiempo real el DrunkManager.EffectIntensity; empieza vacía y se llena al tomar
  2. Un indicador de dinero (texto TMP) se actualiza instantáneamente al vender un objeto o comprar una bebida
  3. El HUD es visible tanto en modo FPS (City/Bar/Home) como en modo auto (City con CarFollowCamera)
**Plans**: TBD
**UI hint**: yes

### Phase 3: Loop de victoria y derrota
**Goal**: El jugador puede ganar o perder la partida y recibir feedback visual claro de cada resultado
**Mode:** mvp
**Depends on**: Phase 2
**Requirements**: GAME-01, GAME-02, GAME-03, GAME-04
**Success Criteria** (what must be TRUE):
  1. Chocar con casa, árbol, niño o mascota mientras se maneja detiene el auto y activa la pantalla de derrota
  2. La pantalla de derrota es distinguible (texto/imagen distinta) de cualquier otra pantalla y ofrece al menos una acción (reintentar o salir)
  3. Haber vendido todos los objetos de Home Y tener el nivel de alcohol mínimo requerido activa la condición de victoria
  4. La pantalla de victoria es distinguible y muestra un mensaje de éxito; ofrece al menos una acción (reintentar o salir)
  5. En una partida normal sin chocar ni vender todo, ninguna pantalla de resultado aparece
**Plans**: TBD
**UI hint**: yes

### Phase 4: Animaciones
**Goal**: Al menos un elemento principal del juego tiene una animación controlada por Animator y otro elemento tiene una animación generada enteramente por código
**Mode:** mvp
**Depends on**: Phase 3
**Requirements**: ANIM-01, ANIM-02
**Success Criteria** (what must be TRUE):
  1. Un Animator con al menos dos estados y una transición con condición está activo en un elemento principal visible (p.ej. personaje al caminar/idle, puerta al abrirse, indicador del HUD)
  2. Al menos un elemento del juego se anima sin ningún AnimationClip — solo mediante código que modifica transform, material o propiedades de componente en Update/coroutine (p.ej. bob del objeto sostenido, pulso de la barra de borrachera, wobble de UI al quedar sin dinero)
  3. Ambas animaciones son visiblemente distinguibles en Play Mode sin necesidad de inspeccionarlas
**Plans**: TBD

### Phase 5: Efectos visuales y carga
**Goal**: El juego tiene partículas en al menos un evento de gameplay, luces que enriquecen la escena y una pantalla de carga dedicada entre transiciones de escena
**Mode:** mvp
**Depends on**: Phase 4
**Requirements**: FX-01, FX-02, SCENE-01
**Success Criteria** (what must be TRUE):
  1. Al menos un evento de gameplay (choque, sorbo de bebida o venta de objeto) dispara un sistema de partículas visible en Play Mode
  2. Al menos un efecto de luz está activo en el juego (p.ej. faros del auto, ambiente del bar, halo en objetos interactuables) — distinto de la iluminación ambiental por defecto
  3. Al transitar entre cualquier par de escenas (City↔Bar, City↔Home) se muestra una escena intermedia de carga con feedback visual (barra de progreso o spinner) mientras la escena de destino carga asincrónicamente; el jugador nunca ve un frame negro vacío
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Economía | 0/2 | Not started | - |
| 2. HUD in-level | 0/? | Not started | - |
| 3. Loop de victoria y derrota | 0/? | Not started | - |
| 4. Animaciones | 0/? | Not started | - |
| 5. Efectos visuales y carga | 0/? | Not started | - |
