---
phase: 4
slug: animaciones
status: approved
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-24
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | none — Unity project sin tests ni asmdefs (ver CLAUDE.md). Verificación por Play Mode + chequeos de compilación. |
| **Config file** | none |
| **Quick run command** | Compilación limpia: revisar consola de Unity sin errores tras editar scripts (UnityMCP `read_console` o Editor). |
| **Full suite command** | Play Mode manual en escena City/Home con los pasos de la tabla "Manual-Only Verifications". |
| **Estimated runtime** | ~30–60 s de Play Mode por verificación |

---

## Sampling Rate

- **After every task commit:** Confirmar compilación sin errores (consola de Unity limpia).
- **After every plan wave:** Entrar a Play Mode y ejecutar el chequeo manual de la wave.
- **Before `/gsd:verify-work`:** Ambas animaciones (ANIM-01 Animator, ANIM-02 código) visibles en Play Mode.
- **Max feedback latency:** ~60 segundos (entrar a Play Mode y observar).

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 4-01-xx | 01 | 1 | ANIM-02 | — / — | N/A (juego offline, sin superficie de ataque) | manual | Play Mode — pulso de DrunkBar por EffectIntensity + wobble de MoneyText al rechazar compra | ✅ | ⬜ pending |
| 4-02-xx | 02 | 1 | ANIM-01 | — / — | N/A | manual | Editor — DoorAnimatorBuilder genera Door.controller (Closed/Open + transición) + DoorOpen.anim | ❌ W0 | ⬜ pending |
| 4-03-xx | 03 | 2 | ANIM-01 | — / — | N/A | manual | Play Mode — acercarse a la puerta dispara la transición del Animator (SetTrigger Open) | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Assets de Animator (`Door.controller` + clips de estados) creados antes de enganchar ANIM-01.

*Toda la verificación es Play Mode manual; no hay framework de tests a instalar.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| El Animator transiciona entre ≥2 estados por una condición (parámetro) | ANIM-01 | No hay framework de tests; es comportamiento visual en runtime de Unity | Entrar a Play Mode, provocar la condición de transición (p.ej. acercarse/activar la puerta), observar que el elemento pasa de un estado a otro de forma visible |
| Un elemento del HUD se anima solo por código (sin AnimationClip), modificando RectTransform en Update/coroutine | ANIM-02 | Comportamiento visual en runtime; sin assertion automatizable sin framework | Entrar a Play Mode: (a) tomar bebidas y observar que la barra de borrachera pulsa con intensidad creciente según EffectIntensity (localScale de DrunkBar); (b) intentar comprar sin dinero suficiente y observar el wobble del texto de dinero (anchoredPosition de MoneyText) |
| Ambas animaciones son distinguibles a simple vista | ANIM-01, ANIM-02 | Criterio perceptual | En una misma sesión de Play Mode confirmar que las dos animaciones son visibles sin inspeccionar el Editor |

---

## Validation Sign-Off

- [ ] Cada tarea tiene verificación manual de Play Mode documentada o dependencia de Wave 0
- [ ] Continuidad de muestreo: ninguna animación queda sin un chequeo de Play Mode asociado
- [ ] Wave 0 cubre los assets de Animator faltantes
- [ ] Sin flags de watch-mode (N/A — Unity)
- [ ] Feedback latency < 60s
- [x] `nyquist_compliant: true` set en frontmatter al cerrar planificación

**Approval:** approved 2026-06-24
