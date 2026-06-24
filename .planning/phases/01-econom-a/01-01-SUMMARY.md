---
phase: 01-econom-a
plan: 01
subsystem: economy
tags: [flyweight, scriptableobject, static-store, sell-mechanic, persistence]
dependency_graph:
  requires: []
  provides:
    - SellableDefinition (Flyweight SO)
    - PlayerMoneyStore (static store)
    - HeldObjectStore (static store)
    - SellCounter (componente de venta)
  affects:
    - CarryableObject.cs (campo definition + OnPickedUp migrado)
    - PlayerPickup.cs (hasHeldObject eliminado, SellCounter detectado)
    - Bar.unity (MostradorVenta agregado)
    - Home.unity (6 CarryableObjects cableados con SellableDefinition)
tech_stack:
  added:
    - ScriptableObject Flyweight (SellableDefinition)
    - Static stores de persistencia entre escenas (PlayerMoneyStore, HeldObjectStore)
  patterns:
    - Flyweight: SellableDefinition compartido por instancias del mismo tipo
    - Store estatico: PlayerMoneyStore, HeldObjectStore (analogo a CarStateStore)
    - Guard clause: en todos los metodos publicos de stores y SellCounter.TrySell
key_files:
  created:
    - Assets/_Project/ScriptableObjects/Items/SellableDefinition.cs
    - Assets/_Project/Core/SceneManagement/PlayerMoneyStore.cs
    - Assets/_Project/Core/SceneManagement/HeldObjectStore.cs
    - Assets/_Project/Gameplay/Items/SellCounter.cs
    - Assets/_Project/ScriptableObjects/Items/Instances/TV.asset
    - Assets/_Project/ScriptableObjects/Items/Instances/Lampara.asset
    - Assets/_Project/ScriptableObjects/Items/Instances/Cuadro.asset
  modified:
    - Assets/_Project/Gameplay/Items/CarryableObject.cs
    - Assets/_Project/Gameplay/Player/PlayerPickup.cs
    - Assets/Scenes/Bar.unity
    - Assets/Scenes/Home.unity
decisions:
  - "D-01: venta via raycast en SellCounter — se integra a PlayerPickup.UpdateSelectionByLook con currentSellCounter (Opcion A de RESEARCH.md)"
  - "D-02: un objeto por viaje — HeldObjectStore.HasHeldObject reemplaza static bool hasHeldObject"
  - "D-03: HeldObjectStore.HeldDefinition persiste la referencia al SO entre escenas"
  - "D-04: referencia explicita a SellableDefinition en CarryableObject.definition"
  - "D-05: SellableDefinition Flyweight — 3 assets: TV (50), Lampara (30), Cuadro (20)"
  - "D-06: SellableDefinition.SellValue es la fuente de verdad del valor de venta"
  - "D-07: PlayerMoneyStore.Money = 0 al inicio"
  - "D-09: sellValue configurable por Inspector en cada SellableDefinition asset"
  - "D-11: dinero persiste entre escenas via PlayerMoneyStore estatico"
metrics:
  duration_minutes: 8
  completed_date: 2026-06-23
  tasks_completed: 2
  tasks_total: 2
  files_created: 11
  files_modified: 4
---

# Phase 1 Plan 01: Slice A — Vender objetos por dinero (SUMMARY)

**One-liner:** Flyweight SellableDefinition + stores estaticos PlayerMoneyStore/HeldObjectStore + SellCounter en Bar con raycast; reemplaza trueque por economia real.

## Tasks Completadas

| Task | Nombre | Commit | Archivos clave |
|------|--------|--------|----------------|
| 1 | SellableDefinition + Stores | f54b15f | SellableDefinition.cs, PlayerMoneyStore.cs, HeldObjectStore.cs, TV/Lampara/Cuadro.asset |
| 2 | CarryableObject + SellCounter + Escenas | e4fdaf2 | SellCounter.cs, CarryableObject.cs, PlayerPickup.cs, Bar.unity, Home.unity |

## Que se construyo

### Task 1 — Flyweight SO y stores de persistencia

**SellableDefinition.cs:** ScriptableObject con `[CreateAssetMenu]` que actua como patron Flyweight. Cada tipo de objeto vendible (TV, Lampara, Cuadro) tiene un unico asset `.asset`; todas las instancias del mismo tipo apuntan al mismo asset. Campos `itemName` y `sellValue` serializados con `[Header]`/`[SerializeField] private`, expuestos como properties de solo lectura.

**PlayerMoneyStore.cs:** Store estatico con `Money` (int), `Add`/`Spend`/`CanAfford`/`Clear`. Guard clauses en todos los metodos. `Debug.Log` con prefijo `[PlayerMoneyStore]` en mutaciones. Arranca en $0 (D-07). Sobrevive a `LoadSceneAsync(Single)` por ser un valor en memoria de proceso.

**HeldObjectStore.cs:** Reemplaza el campo `static bool hasHeldObject` de PlayerPickup. Mantiene `HasHeldObject`, `HeldDefinition` (SellableDefinition) y `HeldObjectId` (string). `SetHeld` tiene guard clause `if definition == null return`. `Clear()` libera los tres campos. El asset ScriptableObject sobrevive entre escenas sin `DontDestroyOnLoad` porque es un asset del proyecto, no un objeto de escena.

**Assets:** TV.asset ($50), Lampara.asset ($30), Cuadro.asset ($20) en `Assets/_Project/ScriptableObjects/Items/Instances/` con GUIDs propios y referencia al script de SellableDefinition.

### Task 2 — Logica de venta e integracion de escenas

**CarryableObject.cs:** Campo `[SerializeField] private SellableDefinition definition` con properties `Definition` y `SellValue`. `OnPickedUp()` ahora llama `HeldObjectStore.SetHeld(definition, StableId)` en vez de `DeliveredObjectsStore.MarkTaken` (que se movio a SellCounter, ya que el objeto se "entrega" al vender, no al agarrar).

**SellCounter.cs:** Nuevo componente con `[RequireComponent(typeof(Collider))]`. `TrySell()` implementa el flujo completo: guard clause `if !HasHeldObject`, lee `HeldDefinition`, llama `PlayerMoneyStore.Add(valor)`, luego `DeliveredObjectsStore.MarkTaken(HeldObjectId)`, luego `HeldObjectStore.Clear()`, SFX + `Debug.Log "[SellCounter] Objeto vendido por $X. Saldo: $Y"`. AudioSource con patron de fallback doble (`Resources.Load`).

**PlayerPickup.cs:** `static bool hasHeldObject` eliminado; `HasHeldObject` delega a `HeldObjectStore.HasHeldObject`; `ConsumeHeldObject()` llama `HeldObjectStore.Clear()`; campo `currentSellCounter` detectado en el raycast de `UpdateSelectionByLook`; en `Update`, prioridad: beber > vender (SellCounter) > agarrar bebida > agarrar objeto. Trueque eliminado.

**Bar.unity:** GameObject `MostradorVenta` en posicion (0.3, 0.2, -4.5), BoxCollider trigger, MonoBehaviour SellCounter referenciado por GUID del script.

**Home.unity:** 6 CarryableObjects con `objectId` unico (`home-obj-1` a `home-obj-6`) y `definition` asignada (2 TV, 2 Lampara, 2 Cuadro).

## Decisiones tomadas

| Decision | Descripcion | Razon |
|----------|-------------|-------|
| D-01 Opcion A | SellCounter detectado desde PlayerPickup.UpdateSelectionByLook | Reutiliza el raycast existente; no duplica deteccion |
| MarkTaken en SellCounter | Movido de CarryableObject.OnPickedUp a SellCounter.TrySell | El objeto se "entrega" logicamente al venderlo, no al agarrarlo |
| interactKey en SellCounter | `KeyCode.E` en Inspector, serializado | Consistente con pickupKey de PlayerPickup |
| BoxCollider isTrigger=true | Configurado en Reset() y en Bar.unity YAML | Permite que el raycast de PlayerPickup lo detecte con QueryTriggerInteraction.Collide |

## Deviaciones del Plan

### Auto-fixed Issues

Ninguna desviacion automatica fue necesaria. El plan se ejecuto exactamente como estaba especificado.

**Nota:** La modificacion del flujo de compra de bebidas en PlayerPickup (trueque → economía) se realizo como parte del plan (Paso B), eliminando la rama `currentPickupItem != null && hasHeldObject` que era el trueque. La nueva rama permite agarrar bebidas directamente (sin checar dinero — eso es scope del Plan 01-02 donde se implementa `DrinkDefinition` con precio).

## Stubs conocidos

| Stub | Archivo | Descripcion |
|------|---------|-------------|
| Compra de bebida sin verificar precio | PlayerPickup.cs linea ~103 | La rama `currentPickupItem != null` llama `Pickup()` sin `PlayerMoneyStore.CanAfford` — eso es scope del Plan 01-02 (DrinkDefinition con precio) |

## Threat Flags

No se introdujo superficie de ataque nueva. El plan analizo T-01-03 (DoS en TrySell) y lo mitigo con guard clauses segun el threat model.

## Self-Check: PASSED

| Item | Resultado |
|------|-----------|
| SellableDefinition.cs | FOUND |
| PlayerMoneyStore.cs | FOUND |
| HeldObjectStore.cs | FOUND |
| SellCounter.cs | FOUND |
| TV.asset | FOUND |
| Lampara.asset | FOUND |
| Cuadro.asset | FOUND |
| Commit f54b15f (Task 1) | FOUND |
| Commit e4fdaf2 (Task 2) | FOUND |
