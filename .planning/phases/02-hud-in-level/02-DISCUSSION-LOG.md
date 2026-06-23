# Phase 2: HUD in-level - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 2-HUD in-level
**Areas discussed:** Persistencia del HUD, Update del dinero, Barra de borrachera, Layout y estilo visual

---

## Persistencia del HUD

| Option | Description | Selected |
|--------|-------------|----------|
| Singleton DontDestroyOnLoad | HUD bootstrappeado una vez (RuntimeInitializeOnLoadMethod + DontDestroyOnLoad, como BackgroundMusicManager); re-vincula DrunkManager via SceneManager.sceneLoaded. Menos setup, sin divergencia. | ✓ |
| Canvas por escena | Canvas con HUD agregado a City/Bar/Home por separado. Binding local trivial, pero 3× setup y riesgo de divergencia. | |

**User's choice:** Singleton DontDestroyOnLoad
**Notes:** El estado de alcohol no persiste (DrunkManager por-escena) → el HUD debe re-resolverlo en cada carga; el dinero (store estático) siempre disponible.

---

## Update del dinero

| Option | Description | Selected |
|--------|-------------|----------|
| Evento OnMoneyChanged | Agregar evento static OnMoneyChanged a PlayerMoneyStore disparado por Add/Spend/Clear; HUD se suscribe. Instantáneo, consistente con el patrón de DrunkManager. | ✓ |
| Polling en Update | HUD lee PlayerMoneyStore.Money cada frame y actualiza si cambió. No toca el store, menos elegante. | |

**User's choice:** Evento OnMoneyChanged
**Notes:** Garantiza el criterio de "actualización instantánea" al vender/comprar.

---

## Barra de borrachera

| Option | Description | Selected |
|--------|-------------|----------|
| EffectIntensity + lerp | Llena según EffectIntensity (curva no lineal, lo que el jugador siente) con suavizado por lerp. Image fill horizontal. | ✓ |
| EffectIntensity instantáneo | Mismo valor sin suavizado; la barra salta cada frame. | |
| NormalizedLevel lineal | Llena según alcohol bebido/máximo (lineal). Se desvía del criterio escrito. | |

**User's choice:** EffectIntensity + lerp
**Notes:** El criterio de éxito de la fase pide explícitamente EffectIntensity.

---

## Layout y estilo visual

| Option | Description | Selected |
|--------|-------------|----------|
| Esquina inf-izq, juntos | Barra + dinero agrupados en la esquina inferior izquierda, minimalista. No tapa el centro ni el mostrador/objetos. | ✓ |
| Barra abajo-centro, dinero arr-der | Barra centrada abajo (prominente) y dinero esquina superior derecha. Ocupa más pantalla. | |
| Esquina sup-izq, juntos | Ambos en la esquina superior izquierda. | |

**User's choice:** Esquina inf-izq, juntos
**Notes:** Canvas en Screen Space - Overlay (cámara-agnóstico, sirve FPS y modo auto sin reconfigurar).

## Claude's Discretion

- Colores, tipografía, tamaños y formato del texto de dinero (dentro del estilo minimalista en esquina inferior izquierda).
- Umbrales de color opcionales de la barra (no requisito de la fase).

## Deferred Ideas

None — la discusión se mantuvo dentro del scope de la fase.
