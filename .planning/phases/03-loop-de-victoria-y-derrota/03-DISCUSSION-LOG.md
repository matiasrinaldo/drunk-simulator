# Phase 3: Loop de victoria y derrota - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 3-Loop de victoria y derrota
**Areas discussed:** Detección de derrota (choque), Condición y momento de victoria, Pantallas de resultado y acciones, Arquitectura del estado de juego

---

## Detección de derrota (choque)

### Marcar obstáculos
| Option | Description | Selected |
|--------|-------------|----------|
| Componente marcador (LethalObstacle) | MonoBehaviour marcador; CityBuilder lo agrega; OnCollisionEnter chequea GetComponent | ✓ |
| Por Tag | Tag 'Obstacle'; patrón ya usado (CompareTag) pero fácil de olvidar | |
| Por Layer | Layer 'Lethal' + layer mask; eficiente, menos granular | |

### Umbral choque
| Option | Description | Selected |
|--------|-------------|----------|
| Cualquier contacto manejando cuenta | IsControlled + toca obstáculo = derrota; simple y predecible | ✓ |
| Umbral de velocidad mínima | Solo si CurrentSpeed supera un mínimo configurable | |

### Reacción del auto
| Option | Description | Selected |
|--------|-------------|----------|
| Frena el auto y bloquea todo el control | SetControlled(false) + frenar Rigidbody + mostrar pantalla | ✓ |
| Solo muestra la pantalla (sin tocar el auto) | La pantalla pausa el juego | |

**User's choice:** Componente marcador `LethalObstacle`; cualquier contacto manejando cuenta; frena el auto y bloquea el control.
**Notes:** Claude registró un open item: verificar si "niño/mascota" existen en City o hay que crearlos (CityBuilder genera casas/árboles).

---

## Condición y momento de victoria

### Momento de victoria
| Option | Description | Selected |
|--------|-------------|----------|
| Al llegar a Home manejando | Chequeo al entrar a Home; refuerza "volvés a casa borracho" | ✓ |
| Apenas se cumplan ambas condiciones | Victoria instantánea al vender el último estando borracho | |
| Al vender el último objeto | Chequeo en la venta (acoplado a SellCounter) | |

### Alcohol mínimo
| Option | Description | Selected |
|--------|-------------|----------|
| Umbral configurable, default medio (~12/24) | Mitad del máximo | |
| Umbral bajo (~6/24) | Borrachera leve suficiente; menor riesgo de choque | ✓ |
| Umbral alto (~18/24) | Muy borracho; máxima tensión pero difícil | |

### Conteo de objetos
| Option | Description | Selected |
|--------|-------------|----------|
| Registrar el total y comparar contra entregados | Store/GameManager conoce total y compara count | ✓ |
| No quedan CarryableObjects activos en Home | Depende de re-entrar a Home para chequear | |

**User's choice:** Victoria al llegar a Home manejando; alcohol mínimo bajo (~6/24, configurable); conteo por total registrado vs. entregados.

---

## Pantallas de resultado y acciones

### Construcción UI
| Option | Description | Selected |
|--------|-------------|----------|
| Overlay por código (estilo HUDController) | Canvas overlay oculto que se muestra al ganar/perder | |
| Prefab de Canvas en escena | Prefab con paneles referenciado por un manager | |
| Escena dedicada de resultado | Cargar escena 'Result' con LoadSceneAsync | ✓ |

### Acciones
| Option | Description | Selected |
|--------|-------------|----------|
| Reintentar + Salir | Dos botones; Salir = Application.Quit | ✓ |
| Solo Reintentar | Un botón; cumple el mínimo | |

### Reintentar
| Option | Description | Selected |
|--------|-------------|----------|
| Reiniciar partida completa desde Home | Clear() de todos los stores + cargar Home | ✓ |
| Recargar la escena actual sin limpiar | Mantiene dinero/alcohol; puede dejar estados inconsistentes | |

### Pausa/cursor
| Option | Description | Selected |
|--------|-------------|----------|
| Sí: Time.timeScale=0 + cursor visible | Congela simulación y muestra cursor | ✓ |
| No pausar (control ya bloqueado) | El mundo sigue corriendo | |

**User's choice:** Escena dedicada 'Result'; Reintentar + Salir; Reintentar = reinicio completo desde Home; cursor visible.
**Notes:** Claude reconcilió la tensión entre "escena dedicada" y "timeScale=0": al ser escena dedicada la gameplay se descarga sola, así que NO se usa timeScale=0; basta asegurar timeScale=1 + cursor visible en la escena Result (registrado en D-10).

---

## Arquitectura del estado de juego

### Organización
| Option | Description | Selected |
|--------|-------------|----------|
| GameManager central | Centraliza victoria, choque y transición a Result | ✓ |
| Distribuido (sin manager nuevo) | Lógica dispersa en auto + trigger | |

### Escenas de resultado
| Option | Description | Selected |
|--------|-------------|----------|
| Una escena 'Result' + flag estático | GameResultStore (Victory\|Defeat); distinción por código/UI | ✓ |
| Dos escenas separadas (Victory, Defeat) | Una por resultado; máxima distinción, duplica estructura | |

### Reset de partida
| Option | Description | Selected |
|--------|-------------|----------|
| Método central NewGame() que limpia todo | Un lugar para resetear; agrega Clear() faltantes | ✓ |
| El botón Reintentar limpia store por store | Sin abstracción; hay que recordar todos los stores | |

**User's choice:** GameManager central; una escena 'Result' + flag estático (GameResultStore); método central NewGame() que limpia todo.

---

## Claude's Discretion

- Diseño visual exacto de las pantallas (colores, tipografía, mensajes), con Victory y Defeat claramente distinguibles.
- GameManager per-escena vs. singleton DontDestroyOnLoad.
- Mecanismo de captura del total de objetos y de comunicación auto→GameManager del choque.
- UI legacy Button vs. handlers por código (TMP donde haya texto).

## Deferred Ideas

- Verificar/crear entidades "niño" y "mascota" en City (open item de research).
- Registrar la escena Result en Build Settings (hoy: Home, City, Bar).
- Partículas/efectos de choque → Phase 5 (FX-01).
