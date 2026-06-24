# Requirements: Drunk Simulator

**Defined:** 2026-06-22
**Core Value:** El loop de dificultad creciente — tomar distorsiona el manejo, y volver a casa sin chocar se vuelve cada vez más difícil.
**Milestone:** TP2 + loop core mínimo

> Objetivos TP2 ya satisfechos por el código existente (no requieren trabajo): **TP2.7 Raycasting** (`PlayerPickup` usa `Physics.Raycast`) y la parte de **gravedad** de TP2.6 (Rigidbody del auto). Este milestone cubre el resto.

## v1 Requirements

Requisitos del milestone actual. Cada uno mapea a una fase del roadmap.

### Economía

- [x] **ECON-01**: Cada objeto agarrable de la casa tiene un valor monetario configurable
- [x] **ECON-02**: Al vender un objeto en el bar, el jugador recibe su valor en dinero
- [x] **ECON-03**: Cada bebida tiene un precio y solo se puede comprar si el jugador tiene dinero suficiente
- [x] **ECON-04**: Comprar una bebida descuenta su precio del dinero (reemplaza el trueque objeto↔bebida actual)

### Estado de Juego

- [x] **GAME-01**: El jugador pierde al chocar contra casa, árbol, niño o mascota mientras maneja
- [x] **GAME-02**: El jugador gana cuando vendió todos los objetos de su casa habiendo alcanzado un nivel mínimo de borrachera
- [x] **GAME-03**: Se muestra un estado/pantalla de victoria al cumplir la condición de ganar
- [x] **GAME-04**: Se muestra un estado/pantalla de derrota al chocar

### HUD in-level (TP2.3)

- [ ] **HUD-01**: Barra de borrachera visible durante el juego que refleja el nivel actual de alcohol
- [ ] **HUD-02**: Indicador de dinero visible que se actualiza al vender objetos y comprar bebidas

### Patrón Flyweight (TP2.1)

- [x] **PAT-01**: Implementar el patrón Flyweight para datos compartidos (p.ej. definiciones de objetos vendibles y/o bebidas como datos intrínsecos compartidos)

### Animaciones (TP2.2)

- [ ] **ANIM-01**: Animación por máquina de estados (Animator) en un elemento principal del juego
- [x] **ANIM-02**: Animación por código en al menos un elemento del juego

### Efectos visuales (TP2.5, TP2.6)

- [ ] **FX-01**: Sistema de partículas en al menos un evento del juego (p.ej. choque, sorbo, venta)
- [ ] **FX-02**: Efecto de luces en el juego (p.ej. luces/faros del auto, ambiente del bar, halo de objetos)

### Carga de nivel (TP2.4)

- [ ] **SCENE-01**: Escena dedicada de carga asincrónica de nivel con feedback visual (pantalla de loading)

## v2 Requirements

Diferido a un milestone posterior (pendientes de TP1). Reconocido pero fuera del roadmap actual.

### Pendientes TP1

- **TP1-CINE**: Usar Cinemachine para el manejo de cámara (TP1.5)
- **TP1-MENU**: Escena de menú principal (TP1.12)
- **TP1-STRAT**: Implementar patrón Strategy (TP1.9)
- **TP1-UISCROLL**: Elemento de UI con scrolling / layout group (TP1.10)
- **TP1-EVENTS**: Llegar a ≥3 eventos (actions/delegates) con sentido (TP1.11) — hoy hay 1

## Out of Scope

Excluido explícitamente para evitar scope creep.

| Feature | Reason |
|---------|--------|
| Bajar a Unity 2022.3.35 LTS | La consigna lo pide pero el proyecto está en 6000.3.11f1; downgrade riesgoso, requiere confirmación de la cátedra |
| Migración completa de cámaras a Cinemachine | Decisión de mantener cámaras custom este milestone; se asume el riesgo en TP1.5 |
| Menú principal elaborado | Diferido a milestone de pendientes TP1 |
| Multiplayer / red | Fuera del alcance del juego |

## Traceability

Qué fases cubren qué requisitos. Se completa al crear el roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| ECON-01 | Phase 1 | Complete |
| ECON-02 | Phase 1 | Complete |
| ECON-03 | Phase 1 | Complete |
| ECON-04 | Phase 1 | Complete |
| PAT-01 | Phase 1 | Complete |
| HUD-01 | Phase 2 | Pending |
| HUD-02 | Phase 2 | Pending |
| GAME-01 | Phase 3 | Complete |
| GAME-02 | Phase 3 | Complete |
| GAME-03 | Phase 3 | Complete |
| GAME-04 | Phase 3 | Complete |
| ANIM-01 | Phase 4 | Pending |
| ANIM-02 | Phase 4 | Complete |
| FX-01 | Phase 5 | Pending |
| FX-02 | Phase 5 | Pending |
| SCENE-01 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-22*
*Last updated: 2026-06-22 — traceability completa tras creación del roadmap*
