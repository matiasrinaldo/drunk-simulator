---
status: partial
phase: 01-econom-a
source: [01-01-SUMMARY.md, 01-02-SUMMARY.md, 01-REVIEW.md]
started: 2026-06-23
updated: 2026-06-23
---

## Current Test

[awaiting human testing en Play mode]

## Tests

### 1. Agarrar objeto vendible en Home
expected: En Home, mirar un objeto (TV / Lámpara / Cuadro) resaltado y agarrarlo con E. El objeto desaparece del mundo y queda "en mano".
result: [pending]

### 2. Vender en el mostrador del Bar
expected: Llevar el objeto al Bar, pararse frente al `MostradorVenta` y presionar E. El dinero del jugador aumenta exactamente por el `sellValue` del objeto (TV $50, Lámpara $30, Cuadro $20).
result: [pending]

### 3. Persistencia de dinero entre escenas
expected: Tras vender, cambiar de escena (Bar → City → Home). El dinero acumulado NO se reinicia.
result: [pending]

### 4. Objeto vendido no reaparece (fix CR-01)
expected: Agarrar un objeto en Home y, ANTES de venderlo, recargar Home (Home → City → Home). El objeto NO reaparece en el mundo (ya está "en mano"). Tras venderlo, tampoco reaparece al volver a Home.
result: [pending]

### 5. Comprar bebida con dinero suficiente
expected: Con saldo suficiente, agarrar una bebida en el Bar. Se descuenta el `price` de la bebida y el jugador se emborracha (sube el nivel de alcohol al tomar sorbos).
result: [pending]

### 6. Rechazo de compra sin dinero
expected: Con $0 (o saldo menor al precio), intentar agarrar una bebida. Suena un SFX de rechazo y la bebida NO se toma ni descuenta dinero.
result: [pending]

### 7. Precios diferenciados por bebida
expected: Cerveza cuesta $10, Trago $20, Whisky $35 — cada una descuenta su precio real desde su `DrinkDefinition`.
result: [pending]

## Summary

total: 7
passed: 0
issues: 0
pending: 7
skipped: 0
blocked: 0

## Gaps
