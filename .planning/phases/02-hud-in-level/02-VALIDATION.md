---
phase: 02
slug: hud-in-level
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-22
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.6.0 (`com.unity.test-framework`) — instalado, sin tests ni `.asmdef` |
| **Config file** | none — no hay assembly definitions en `Assets/_Project/`; el Test Runner no descubre tests sin asmdef |
| **Quick run command** | `Window > General > Test Runner > Run All` (solo desde el Editor) |
| **Full suite command** | Ídem — no hay CLI de tests |
| **Estimated runtime** | N/A — validación manual en Play Mode |

---

## Sampling Rate

- **After every task commit:** Compilar en el Editor (Console sin errores) vía MCP `read_console`
- **After every plan wave:** Entrar a Play Mode y verificar el comportamiento de la wave
- **Before `/gsd:verify-work`:** Las 4 verificaciones manuales (abajo) deben pasar en Play Mode
- **Max feedback latency:** ~30 segundos (compilación + Play Mode)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-*-* | TBD | 0 | — | — | N/A | compile | Console sin errores (MCP `read_console`) | ✅ | ⬜ pending |
| 02-*-* | TBD | 1 | HUD-01 | — | N/A | manual (Play Mode) | Barra refleja `EffectIntensity` en tiempo real | ✅ | ⬜ pending |
| 02-*-* | TBD | 1 | HUD-02 | — | N/A | manual (Play Mode) | Texto TMP cambia al vender/comprar | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Task IDs concretos se asignan al generar los PLAN.md.*

---

## Wave 0 Requirements

- [ ] `Window > TextMeshPro > Import TMP Essential Resources` — los TMP Essentials NO están importados (carpeta `Assets/TextMesh Pro/` vacía). BLOQUEANTE antes de crear cualquier `TextMeshProUGUI`.

*No hay infraestructura de tests automáticos a instalar — la validación de esta fase es manual en Play Mode.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HUD visible en City con barra vacía y `$0` | HUD-01, HUD-02 | UI no testeable sin asmdef; render visual | Entrar a Play Mode en City — confirmar HUD presente, barra vacía, dinero inicial |
| HUD persiste y no se duplica al cambiar de escena | HUD-01, HUD-02 | Requiere carga de escena Single en runtime | Pasar puerta City→Bar — confirmar HUD persiste, un solo Canvas |
| Barra sube progresivamente al beber | HUD-01 | Refleja `EffectIntensity` en tiempo real (lerp) | Comprar y beber cerveza en Bar — la barra se llena gradualmente |
| Texto de dinero se actualiza instantáneamente | HUD-02 | Reacciona a `OnMoneyChanged` | Vender objeto en el mostrador y comprar bebida — el texto cambia al instante |
| HUD visible en modo auto (CarFollowCamera) | HUD-01, HUD-02 | Screen Space Overlay independiente de cámara | Entrar al auto en City — confirmar HUD sigue visible |

---

## Validation Sign-Off

- [ ] Wave 0 importa TMP Essential Resources antes de cualquier `TextMeshProUGUI`
- [ ] Cada tarea no-Wave0 mapea a una verificación manual de Play Mode o a compilación limpia
- [ ] Las 5 verificaciones manuales cubren los 3 Success Criteria de la fase
- [ ] Sin watch-mode flags (no aplica — Editor)
- [ ] `nyquist_compliant: true` set en frontmatter al cerrar el plan

**Approval:** pending
