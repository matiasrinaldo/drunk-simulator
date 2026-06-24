---
phase: 1
slug: econom-a
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-22
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Proyecto Unity sin tests/asmdef: la validación primaria es **manual en Play Mode** con criterios observables. EditMode tests (Nivel 3) son opcionales para la lógica pura de los stores.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (instalado, sin asmdef configurado) — usado solo si se opta por Nivel 3 |
| **Config file** | none — `Assets/Tests/EditMode/*.asmdef` lo crea Wave 0 si se hace Nivel 3 |
| **Quick run command** | Manual: entrar a Play Mode en la escena relevante y ejecutar el guion de verificación |
| **Full suite command** | EditMode (si existe): *Window → General → Test Runner → EditMode → Run All* |
| **Estimated runtime** | Manual ~2-3 min por criterio; EditMode <5 s |

---

## Sampling Rate

- **After every task commit:** Verificación manual en Play Mode del comportamiento tocado (o `Run All` EditMode si la task tocó un store con tests).
- **After every plan wave:** Recorrer el loop completo Home → Bar → vender → comprar en Play Mode.
- **Before `/gsd:verify-work`:** Los 5 Success Criteria del ROADMAP verificados manualmente (tabla abajo) en verde.
- **Max feedback latency:** ~3 min (manual) / <5 s (EditMode si existe).

---

## Per-Task Verification Map

| Req ID | Comportamiento a verificar | Test Type | Cómo verificarlo | Status |
|--------|---------------------------|-----------|------------------|--------|
| ECON-01 | `CarryableObject` tiene valor monetario configurable por Inspector | manual (Play/Inspector) | Seleccionar un CarryableObject en Home.unity; el campo `SellableDefinition` muestra el asset con su `sellValue` | ⬜ pending |
| ECON-02 | Vender objeto en el bar aumenta el dinero y persiste al volver a City | manual (Play) | Agarrar objeto en Home → ir al Bar → vender en mostrador → salir a City; `Debug.Log(PlayerMoneyStore.Money)` muestra el saldo aumentado tras recargar City | ⬜ pending |
| ECON-03 | Bebida con precio; no se puede tomar sin dinero suficiente | manual (Play) | Con $0 intentar tomar una bebida en el Bar; suena SFX de rechazo y NO se bebe | ⬜ pending |
| ECON-04 | Comprar descuenta precio; trueque ya no funciona | manual (Play) | Tras vender, comprar bebida; el dinero baja exactamente el precio; tomar ya no depende de tener objeto en mano | ⬜ pending |
| PAT-01 | Flyweight: múltiples instancias comparten el mismo SO | manual (Inspector) | Dos bebidas del mismo tipo muestran el MISMO asset en `definition`; ningún valor hardcodeado de alcohol/precio queda en `PickupItem`/prefab | ⬜ pending |
| ECON-02/03/04 (lógica pura) | `PlayerMoneyStore` Add/Spend/Clear correctos | unit (EditMode, opcional Nivel 3) | `Run All` EditMode → `PlayerMoneyStoreTests` verde | ⬜ pending |
| D-03 (transporte) | `HeldObjectStore` transporta `SellableDefinition` entre escenas | unit (EditMode, opcional Nivel 3) | `Run All` EditMode → `HeldObjectStoreTests` verde | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Debug Aids (Nivel 2 — recomendado durante implementación)

Patrón existente: `DrunkManager.debugAddAlcoholKey`. Agregar una tecla de debug para inyectar dinero sin tener que recorrer el loop completo cada vez:

```csharp
[Header("Debug")]
[SerializeField] private KeyCode debugAddMoneyKey = KeyCode.M;
[SerializeField] private int debugAddMoneyAmount = 50;
// en Update: if (Input.GetKeyDown(debugAddMoneyKey)) { PlayerMoneyStore.Add(debugAddMoneyAmount); Debug.Log($"[Debug] Saldo: ${PlayerMoneyStore.Money}"); }
```

---

## Wave 0 Requirements

*Solo si el planner opta por Nivel 3 (EditMode tests). Para el MVP académico, Nivel 1 (manual) es suficiente y Wave 0 puede omitirse.*

- [ ] `Assets/Tests/EditMode/*.asmdef` referenciando `Unity.TestFramework` (+ asmdef de runtime que exponga `PlayerMoneyStore`/`HeldObjectStore`)
- [ ] `PlayerMoneyStoreTests.cs` — cubre ECON-02, ECON-03, ECON-04 (Add/Spend/Clear)
- [ ] `HeldObjectStoreTests.cs` — cubre D-03 (transporte de `SellableDefinition`)
- [ ] Verificar *Window → General → Test Runner → EditMode*

*Si no se hace Nivel 3: "None — validación manual cubre todos los criterios observables de la fase."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Vender objeto con raycast + E en el mostrador del Bar | ECON-02 | Interacción de gameplay en escena 3D; no automatizable sin harness de input/escena | Mirar el mostrador, apretar E con un objeto en mano, observar que el dinero sube y el objeto se consume |
| SFX de rechazo al no tener dinero | ECON-03 | El audio se percibe, no se asierta fácilmente en EditMode | Con $0 intentar comprar; oír el SFX de rechazo y confirmar que no se bebe |
| Persistencia del dinero al cruzar escena (Single load) | ECON-02 | Requiere ciclo real de `LoadSceneAsync(Single)` en Play Mode | Cruzar Bar→City y confirmar `PlayerMoneyStore.Money` intacto |
| Flyweight compartido entre instancias | PAT-01 | Inspección visual de referencias de asset en el Editor | Comparar el campo `definition`/`SellableDefinition` de dos instancias del mismo tipo |

*Los stores puros (Add/Spend/Clear) SÍ son automatizables vía EditMode (Nivel 3); el resto es manual por naturaleza de Unity gameplay.*

---

## Validation Sign-Off

- [ ] Cada Success Criteria del ROADMAP tiene una verificación manual definida (tabla Per-Task)
- [ ] Sampling continuity: se verifica tras cada task y al cerrar cada wave
- [ ] Wave 0 cubierto si se eligió Nivel 3 (o explícitamente omitido)
- [ ] No watch-mode flags
- [ ] Feedback latency < 180 s (manual) / < 5 s (EditMode)
- [ ] `nyquist_compliant: true` set in frontmatter (al aprobar)

**Approval:** pending
