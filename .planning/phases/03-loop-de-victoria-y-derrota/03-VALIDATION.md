---
phase: 03
slug: loop-de-victoria-y-derrota
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 03 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (`com.unity.test-framework`) — instalado, sin tests ni assembly definitions aún (ver CLAUDE.md) |
| **Config file** | none — no hay assembly definitions |
| **Quick run command** | Build en Editor (compilación) + `read_console` sin errores |
| **Full suite command** | Play mode manual del flow completo (Window → General → Test Runner no tiene tests) |
| **Estimated runtime** | ~30–60s por Play test manual |

*Esta fase NO introduce tests automatizados: CLAUDE.md confirma que no existen assembly definitions. La validación es manual en modo Play. Agregar un test framework es deuda documentada en CONCERNS.md.*

---

## Sampling Rate

- **After every task commit:** Build en Editor; `read_console` debe estar sin errores de compilación.
- **After every plan wave:** Play test manual del slice afectado (p. ej. choque → Result; o llegada a Home → Victory).
- **Before `/gsd:verify-work`:** Los 5 success criteria verificados en modo Play; flow completo (inicio → bar → conducir borracho → choque/victoria → Result → Reintentar → inicio limpio).
- **Max feedback latency:** compilación inmediata; Play test manual ~1–2 min.

---

## Per-Task Verification Map

| Behavior | Requirement | Wave | Test Type | Verification | Status |
|----------|-------------|------|-----------|--------------|--------|
| Chocar con edificio mientras `IsControlled=true` → Result/Defeat | GAME-01 | Play en City, conducir hacia edificio | Manual Play | Aparece pantalla Defeat | ⬜ pending |
| Chocar con árbol → Result/Defeat | GAME-01 | Play en City, conducir hacia árbol | Manual Play | Aparece pantalla Defeat | ⬜ pending |
| Chocar con niño/placeholder → Result/Defeat | GAME-01 | Play en City, conducir hacia personaje | Manual Play | Aparece pantalla Defeat | ⬜ pending |
| Chocar a pie (`IsControlled=false`) → NO Result | GAME-01 / Criterio 5 | Caminar contra edificio | Manual Play | No aparece Result | ⬜ pending |
| Sin vender todo + alcohol bajo → llegar a Home carga Home normal | GAME-02 / Criterio 5 | Ir a Home sin vender | Manual Play | Home carga, sin Result | ⬜ pending |
| Vendido todo + alcohol ≥ mínimo → llegar a Home carga Result/Victory | GAME-02 | Vender todo → conducir a Home borracho | Manual Play | Aparece pantalla Victory | ⬜ pending |
| Pantalla Victory distinguible + texto éxito + botones | GAME-03 | Trigger de victoria → inspeccionar UI | Manual Play | Verde, "LLEGASTE A CASA", Reintentar/Salir | ⬜ pending |
| Pantalla Defeat distinguible + texto derrota + botones | GAME-04 | Trigger de derrota → inspeccionar UI | Manual Play | Rojo, "CHOCASTE", Reintentar/Salir | ⬜ pending |
| Cursor visible + `timeScale=1` en Result | D-10 | Verificar cursor liberado en Result | Manual Play | Cursor libre, juego no pausado | ⬜ pending |
| Reintentar limpia todos los stores | D-09 / D-13 | Reintentar → inspeccionar Home | Manual Play | Home con todos los objetos, alcohol=0, dinero=0 | ⬜ pending |

*Status: ⬜ pending · ✅ verificado · ❌ falla · ⚠️ inestable*

---

## Wave 0 Requirements

- [ ] `Assets/Scenes/Result.unity` debe crearse y registrarse en Build Settings antes de que cualquier script la referencie (`SceneManager.LoadSceneAsync("Result")`).
- [ ] No se instala framework de tests automatizados en esta fase (decisión consciente — toda la validación es Play mode manual).

*No se recomiendan tests automáticos en Wave 0: CLAUDE.md indica que no hay assembly definitions. Deuda documentada en CONCERNS.md.*

---

## Manual-Only Verifications

Todas las verificaciones de esta fase son manuales (ver Per-Task Verification Map). Razón: el proyecto no tiene framework de tests configurado y los criterios de éxito son comportamientos en runtime de Unity (colisiones físicas, transiciones de escena, UI visible) que requieren Play mode.

---

## Validation Sign-Off

- [ ] Cada tarea de código compila sin errores en Editor (`read_console` limpio)
- [ ] Los 5 success criteria del roadmap verificados en Play mode
- [ ] Flow completo end-to-end probado (inicio → choque/victoria → Result → Reintentar → inicio limpio)
- [ ] `Result.unity` creada y en Build Settings antes de cualquier referencia
- [ ] `nyquist_compliant: true` set en frontmatter tras la verificación de fase

**Approval:** pending
