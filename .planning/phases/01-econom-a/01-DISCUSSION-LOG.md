# Phase 1: Economía - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-22
**Phase:** 1-Economía
**Areas discussed:** Mecánica de venta, Asociación Flyweight, Balance económico, Feedback sin dinero

---

## Mecánica de venta

### ¿Cómo se concreta la venta del objeto en el bar?

| Option | Description | Selected |
|--------|-------------|----------|
| Mostrador con tecla E | Zona/mostrador; mirás y apretás E para vender. Reusa raycast + E de PlayerPickup. | ✓ |
| Zona trigger automática | Entrás con el objeto y se vende solo al pisar la zona (OnTriggerEnter). | |
| Soltar en punto de venta | Apuntás a un punto y soltás el objeto ahí; al soltarlo se acredita. | |

**User's choice:** Mostrador con tecla E
**Notes:** Consistente con cómo se agarran cosas hoy. Nota técnica: al agarrar un CarryableObject hoy el objeto se desactiva sin recordar cuál era; para venderlo con su valor habrá que recordar su SellableDefinition.

### ¿Cuántos objetos llevás del home al bar por viaje?

| Option | Description | Selected |
|--------|-------------|----------|
| Uno por viaje | Como hoy: hasHeldObject es un solo bool. Más viajes, mínimo cambio. | ✓ |
| Varios a la vez | Inventario/contador simple; menos viajes, cambia el modelo actual. | |

**User's choice:** Uno por viaje
**Notes:** Mantiene el modelo de un objeto en mano; refuerza el loop de manejar borracho repetidamente.

---

## Asociación Flyweight

### ¿Cómo se vincula cada bebida/objeto a su ScriptableObject?

| Option | Description | Selected |
|--------|-------------|----------|
| Referencia explícita en prefab | Campo que apunta directo al SO; elimina ResolvedPickupType por nombre. | ✓ |
| Resolución por nombre → SO | Mantiene el matching por nombre pero devuelve el SO; arrastra el concern. | |
| Híbrido | Referencia explícita con fallback a nombre; migración gradual. | |

**User's choice:** Referencia explícita en prefab
**Notes:** Resuelve el concern conocido de fragilidad por nombre; uso "de libro" del Flyweight.

### ¿Cómo se organizan las definiciones de objetos vendibles?

| Option | Description | Selected |
|--------|-------------|----------|
| Catálogo por tipo | Un SellableDefinition por tipo (TV, lámpara...), compartido por instancias. | ✓ |
| Uno por objeto | Cada objeto su propia definición/valor único. | |

**User's choice:** Catálogo por tipo
**Notes:** Estado intrínseco compartido entre muchos objetos; refuerza la demostración de PAT-01.

---

## Balance económico

### ¿Con cuánto dinero arranca el jugador?

| Option | Description | Selected |
|--------|-------------|----------|
| Arranca en $0 | Tenés que vender sí o sí antes de tomar. Fuerza el loop completo. | ✓ |
| Algo de plata inicial | Podés comprar una bebida antes de vender; arranque más suave. | |

**User's choice:** Arranca en $0

### ¿Cómo se relacionan precio y efecto de las bebidas?

| Option | Description | Selected |
|--------|-------------|----------|
| Más fuerte = más cara | Whisky > trago > cerveza en precio; crea decisiones económicas. | ✓ |
| Todas el mismo precio | Solo difieren en cuánto emborrachan; siempre conviene la más fuerte. | |
| Vos decidís (Claude) | Valores sensatos en el plan, ajustables en Inspector. | |

**User's choice:** Más fuerte = más cara
**Notes:** Las cifras exactas quedan tuneables vía ScriptableObject.

---

## Feedback sin dinero

### ¿Qué feedback recibe el jugador al no poder comprar?

| Option | Description | Selected |
|--------|-------------|----------|
| Sonido de rechazo | SFX de 'no' y no se concreta la compra. Reusa AudioSource + Resources.Load. | ✓ |
| Mensaje en pantalla | Texto temporal vía OnGUI; anticipa algo de UI. | |
| Sonido + mensaje | Las dos cosas. | |
| Nada por ahora | Solo no se concreta la compra; feedback recién en Phase 2. | |

**User's choice:** Sonido de rechazo
**Notes:** Feedback claro sin depender del HUD (que es Phase 2).

---

## Claude's Discretion

- Valores/precios numéricos iniciales en los ScriptableObjects.
- Nombre y forma exacta del store de dinero (`PlayerMoneyStore` tentativo) y del transporte de la SellableDefinition del objeto sostenido entre escenas.
- Clip/asset concreto del SFX de rechazo.
- Forma exacta del mostrador de venta en la escena Bar.

## Deferred Ideas

- Inventario / llevar varios objetos a la vez — descartado para esta fase; reconsiderar si el loop se siente repetitivo.
- Mensaje/UI de "no te alcanza" — diferido al HUD (Phase 2).
