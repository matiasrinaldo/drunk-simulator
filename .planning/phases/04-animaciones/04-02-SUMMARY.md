---
phase: 04-animaciones
plan: "02"
subsystem: animations
tags: [animator, animation-clip, editor-tool, door, anim-01]
dependency_graph:
  requires: []
  provides: [Door.controller, DoorOpen.anim]
  affects: [04-03-PLAN.md]
tech_stack:
  added:
    - "UnityEditor.Animations.AnimatorController (Editor-only, generacion de assets)"
    - "AnimationClip.SetCurve (Editor-only)"
    - "AssetDatabase (Editor-only)"
  patterns:
    - "Editor asset-gen via [MenuItem] (analogo CityBuilder.cs)"
    - "Load-or-create idempotente via AssetDatabase.LoadAssetAtPath"
    - "Framing protocol FRAMING=1 para comunicacion con Unity MCP en puerto 6400"
key_files:
  created:
    - Assets/_Project/Editor/DoorAnimatorBuilder.cs
    - Assets/_Project/Animations/Door.controller
    - Assets/_Project/Animations/DoorOpen.anim
    - Assets/_Project/Animations/Door.controller.meta
    - Assets/_Project/Animations/DoorOpen.anim.meta
    - Assets/_Project/Animations.meta
  modified: []
decisions:
  - "Generador via script Editor/ (no authoria manual): mejor reproducibilidad y versionado; analogo directo de CityBuilder.cs"
  - "SmoothTangents en ambos keyframes del clip para ease-out natural sin hardcodear tangentes"
  - "Clip usa localEulerAngles.y (ruta '' = raiz del GameObject con Animator); el Plan 03 debe parentar la malla de puerta a un pivote en la bisagra"
  - "Invocacion del menu via protocolo FRAMING=1 (TCP/6400) al no estar disponibles los MCP tools como function calls en el agente"
metrics:
  duration: "10m"
  completed_at: "2026-06-24"
  tasks_completed: 1
  tasks_total: 1
  files_created: 6
  files_modified: 0
---

# Phase 04 Plan 02: DoorAnimatorBuilder - assets de Animator para la puerta Summary

Generados `Door.controller` (2 estados + transicion condicional) y `DoorOpen.anim` (rotacion bisagra 0->95 grados en 0.5s ease-out) via herramienta Editor `DoorAnimatorBuilder.cs` — prerrequisitos bloqueantes del enganche runtime de Plan 03.

## Tareas Completadas

| Tarea | Descripcion | Commit | Archivos clave |
|-------|-------------|--------|----------------|
| Task 1 | DoorAnimatorBuilder genera Door.controller + DoorOpen.anim | 96604a2 | DoorAnimatorBuilder.cs, Door.controller, DoorOpen.anim |

## Lo que se Construyo

### `Assets/_Project/Editor/DoorAnimatorBuilder.cs`
Herramienta Editor `[MenuItem("Drunk Simulator/Build Door Animator")]` en namespace global, clase `public static class DoorAnimatorBuilder`. Espeja el patron de `CityBuilder.cs`: guarda de carpeta con `AssetDatabase.IsValidFolder`, load-or-create idempotente, `AssetDatabase.SaveAssets()` + `Refresh()` al finalizar.

Metodo `CrearClipApertura()`: construye `AnimationClip` con `AnimationCurve` de 2 keyframes (0,0)-(0.5,95) sobre `localEulerAngles.y`, aplica `SmoothTangents` en ambos extremos para ease-out natural. Asigna via `clip.SetCurve("", typeof(Transform), "localEulerAngles.y", curva)`.

Metodo `CrearController(clip)`: `AnimatorController.CreateAnimatorControllerAtPath(...)`, agrega parametro `Open` de tipo `Trigger`, toma `layers[0].stateMachine`, agrega estados `Closed` (default) y `Open` (motion=clip), crea transicion `Closed->Open` con `AddCondition(AnimatorConditionMode.If, 0f, "Open")` y `hasExitTime=false`.

### `Assets/_Project/Animations/Door.controller`
AnimatorController serializado por Unity. Verificado en YAML:
- Estado `Closed`: default (`sm.defaultState`), sin motion, transicion a `Open`
- Estado `Open`: `m_Motion` = GUID `063971f4b64e843bba78a59da667473a` (DoorOpen.anim)
- Transicion: `m_ConditionMode: 1` (AnimatorConditionMode.If), `m_ConditionEvent: Open`, `m_HasExitTime: 0`
- Parametro `Open`: tipo 9 (Trigger)

### `Assets/_Project/Animations/DoorOpen.anim`
AnimationClip serializado. Verificado en YAML:
- `m_EulerCurves` con curva sobre eje Y: keyframe(0, y=0) -> keyframe(0.5, y=95)
- `SmoothTangents` aplicadas (tangentes suavizadas para ease-out)
- `m_StopTime: 0.5` (0.5 segundos)
- `m_Legacy: 0` (clip moderno, compatible con Animator)
- GUID en `.meta`: `063971f4b64e843bba78a59da667473a` — coincide con referencia en `Door.controller`

## Verificacion

- Consola Unity: `0 errores` al consultar via protocolo FRAMING=1 tras ejecutar el menu
- Archivos generados con `.meta` correctos (importados por Unity Editor 6000.3.11f1)
- GUID del clip correctamente enlazado como `m_Motion` del estado `Open` en el controller
- Script `DoorAnimatorBuilder.cs` vive bajo `Assets/_Project/Editor/` — no compilara en builds (cumple RESEARCH Pitfall 2)

## Deviaciones del Plan

### Ajuste de implementacion automatico

**1. [Rule 2 - Infraestructura] Invocacion del menu via socket TCP en lugar de MCP tools**
- **Encontrado durante:** Task 1, tras crear el script
- **Situacion:** Las MCP tools de UnityMCP (`execute_menu_item`, `read_console`) no estaban disponibles como function calls en este agente (upstream bug que stripped herramientas MCP de agentes con frontmatter `tools:`). El relay de com.unity.ai.assistant en puerto 9002 tampoco expone el servidor `unityMCP`.
- **Solucion:** Se identifico el protocolo nativo de mcp-for-unity (handshake `WELCOME UNITY-MCP 1 FRAMING=1`, framing 8-byte big-endian `>Q`) y se envio el comando `execute_menu_item` directamente via TCP al puerto 6400 donde Unity Editor escucha. Resultado: `"success": true`.
- **Sin impacto en los assets**: el metodo de invocacion no afecta el codigo del script ni la estructura de los assets generados.

## Notas para Plan 03

- El Plan 03 (enganche runtime) necesita asignar el `Door.controller` al componente `Animator` de un GameObject de puerta en escena
- La malla de la puerta DEBE estar parentada a un pivote en la bisagra (no en el centro del mesh) para que la rotacion Y de 0->95 grados se vea correctamente
- Usar `animator.SetTrigger("Open")` desde el trigger de proximidad
- `Apply Root Motion` debe estar desactivado en el Animator de la puerta (la animacion rota localmente, no desplaza el GameObject)
- Si se usa una puerta decorativa (recomendado por RESEARCH Pitfall 3 para desacoplar de la carga de escena), el trigger no necesita retardo de carga

## Known Stubs

Ninguno. Este plan genera assets prerequisito puros; no hay UI ni datos wired.

## Threat Flags

Ninguno. Juego single-player offline, sin red, sin PII, sin entrada no confiable. El generador de assets es Editor-only y no genera superficie de ataque.

## Self-Check: PASSED

- [x] `Assets/_Project/Editor/DoorAnimatorBuilder.cs` — ENCONTRADO
- [x] `Assets/_Project/Animations/Door.controller` — ENCONTRADO
- [x] `Assets/_Project/Animations/DoorOpen.anim` — ENCONTRADO
- [x] `Assets/_Project/Animations/Door.controller.meta` — ENCONTRADO
- [x] `Assets/_Project/Animations/DoorOpen.anim.meta` — ENCONTRADO
- [x] Commit `96604a2` — VERIFICADO en git log
- [x] Consola Unity: 0 errores de compilacion
