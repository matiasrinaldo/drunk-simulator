---
phase: 04-animaciones
plan: "01"
subsystem: HUD / Animacion
tags: [anim-02, hud, animation-by-code, drunkbar, moneytext]
dependency_graph:
  requires: []
  provides: [ANIM-02-hud-pulse, ANIM-02-hud-wobble]
  affects: [HUDController, PlayerPickup]
tech_stack:
  added: []
  patterns:
    - "Pulso senoidal por frame en Update (patron MouseLook/CarController)"
    - "Coroutine one-shot con handle unico (patron StopDrinkAnimation/drinkAnimationRoutine)"
    - "Accessor estatico publico -> metodo de instancia privado (patron SetVisible)"
key_files:
  created: []
  modified:
    - Assets/_Project/UI/HUD/HUDController.cs
    - Assets/_Project/Gameplay/Player/PlayerPickup.cs
decisions:
  - "Pivot de DrunkBar cambiado a (0.5,0.5) con anchoredPosition compensada a (110,10) para que el pulso senoidal sea simetrico visualmente sin wrapper adicional"
  - "Color flash del wobble implementado (blanco -> #D64545 -> blanco) en la misma duracion 0.30s; el snap final garantiza Color.white exacto"
metrics:
  duration: "~25 min"
  completed: "2026-06-24"
  tasks_completed: 2
  files_modified: 2
---

# Phase 04 Plan 01: Animaciones HUD por Codigo (ANIM-02) Summary

Pulso senoidal de DrunkBar escalado por EffectIntensity + wobble one-shot de MoneyText al rechazar compra, ambos 100% codigo (sin AnimationClip). Satisface ANIM-02.

## Tareas Completadas

| Tarea | Nombre | Commit | Archivos |
|-------|--------|--------|---------|
| 1 | Pulso de DrunkBar por EffectIntensity (Token A) | 6776443 | HUDController.cs |
| 2 | Wobble de MoneyText one-shot + disparo desde PlayerPickup (Token B) | 6776443 | HUDController.cs, PlayerPickup.cs |

## Que se implemento

### Task 1 — Pulso de DrunkBar (Token A)

**HUDController.cs:**
- Nuevo campo privado `drunkBarRect` (RectTransform) capturado en `BuildHUD()`.
- Pivot de DrunkBar cambiado de `(0,0)` a `(0.5, 0.5)` con `anchoredPosition` compensada a `(110f, 10f)` (mitad de sizeDelta 220x20) para que el pulso escale desde el centro y se vea simetrico, no como estiramiento de esquina.
- `Update()`: despues del lerp de fill existente (sin tocarlo), aplica pulso senoidal sobre `drunkBarRect.localScale`:
  - Si `EffectIntensity <= 0.05f`: fuerza `localScale = Vector3.one` (barra exactamente quieta en reposo).
  - Sino: `amp = Mathf.Lerp(0, 0.08, k)`, `f = Mathf.Lerp(0, 1.8, k)`, `s = 1 + sin(Time.time * f * 2π) * amp`, aplica `new Vector3(s, s, 1f)`.

### Task 2 — Wobble de MoneyText (Token B)

**HUDController.cs:**
- Nuevo campo `private Coroutine moneyWobbleRoutine`.
- `public static void FlashMoneyRejected()`: accessor estatico (molde de `SetVisible`) que delega a `instance.StartMoneyWobble()`.
- `StartMoneyWobble()`: handle unico — si hay coroutine corriendo, la frena y re-snapea antes de reiniciar (sin apilar offsets).
- `MoneyWobbleRoutine()`: 6px amplitud, 12 Hz, 0.30s, envolvente lineal `(1 - t)`. Flash de color blanco → `#D64545` → blanco. Snap final garantizado: `anchoredPosition = (0, 28)` y `color = Color.white`.

**PlayerPickup.cs:**
- Una linea agregada en la rama `!CanAfford`: `HUDController.FlashMoneyRejected();`, antes del `return`, junto al SFX de rechazo existente.

## Criterios de Aceptacion

- [x] `HUDController.cs` contiene campo `drunkBarRect` capturado en `BuildHUD`.
- [x] `Update` aplica `drunkBarRect.localScale` con `Mathf.Sin` escalado por `EffectIntensity` (amp 0→0.08, f 0→1.8 Hz).
- [x] Con `EffectIntensity <= 0.05` el codigo fuerza `localScale = Vector3.one`.
- [x] `Update` NO modifica la formula existente de `fillImage.fillAmount`.
- [x] `HUDController.cs` contiene `FlashMoneyRejected()` y coroutine con handle unico (`moneyWobbleRoutine`).
- [x] Coroutine escribe `anchoredPosition.x` con `Mathf.Sin` a 12 Hz, amplitud 6px, envolvente `(1 - elapsed/duration)`, duracion 0.30s.
- [x] Al terminar resetea `anchoredPosition` a `(0, 28)` y `color` a `Color.white`.
- [x] Re-disparar mientras corre reinicia sin apilar offsets.
- [x] `PlayerPickup.cs` contiene `HUDController.FlashMoneyRejected()` dentro de la rama `!CanAfford`, antes del `return`.
- [x] ANIM-02 satisfecho: ambas animaciones son codigo puro, sin AnimationClip.

## Desvios del Plan

### Ajuste de pivot (Decision de implementacion dentro del margen del plan)

**Encontrado en:** Task 1

**Descripcion:** El plan indicaba "executor's call" para la simetria del pulso. El barRect original tenia pivot `(0,0)` en esquina. Cambiar pivot a `(0.5, 0.5)` y compensar `anchoredPosition` a `(110f, 10f)` (mitad de sizeDelta) fue la solucion mas simple y limpia — sin wrapper adicional, sin reestructurar jerarquia.

**Efecto:** La posicion visual de la barra queda identica, el pulso escala desde el centro.

**Archivos:** `HUDController.cs`

**Confirmado en:** Criterio de aceptacion "pulso simetrico visible".

### Implementacion de flash de color (Token B, opcional)

**Encontrado en:** Task 2

**Descripcion:** El UI-SPEC marcaba el flash de color blanco->#D64545->blanco como "OPCIONAL". Se implemento dentro del mismo coroutine usando `Color.Lerp(colorRojo, Color.white, t)` con snap final garantizado a `Color.white`. No agrega dependencias ni riesgo; refuerza el feedback visual de rechazo.

## Known Stubs

Ninguno. Ambas animaciones estan completamente cableadas: el pulso lee `DrunkManager.EffectIntensity` cada frame (re-bindeado en `OnSceneLoaded`), y el wobble se dispara desde el path de rechazo real de `PlayerPickup`.

## Threat Flags

Ninguno. Juego single-player offline; las modificaciones son UI-only sobre datos locales sin nuevas superficies de red, auth, ni PII.

## Self-Check: PASSED

- [x] `Assets/_Project/UI/HUD/HUDController.cs` — existe y modificado
- [x] `Assets/_Project/Gameplay/Player/PlayerPickup.cs` — existe y modificado
- [x] Commit `6776443` — existe en git log
- [x] `FlashMoneyRejected` en HUDController.cs — presente
- [x] `HUDController.FlashMoneyRejected()` en PlayerPickup.cs — presente
- [x] `drunkBarRect` field en HUDController.cs — presente
- [x] `moneyWobbleRoutine` field en HUDController.cs — presente
