---
phase: 01-econom-a
plan: 02
subsystem: economy
tags: [flyweight, scriptableobject, purchase, sfx, drink-economy]
dependency_graph:
  requires:
    - 01-01 (PlayerMoneyStore, HeldObjectStore, SellCounter)
  provides:
    - DrinkDefinition (Flyweight SO para bebidas)
    - Compra de bebidas con dinero real
    - SFX de rechazo por saldo insuficiente
  affects:
    - PickupItem.cs (migrado a DrinkDefinition; enum PickupType y campos hardcodeados eliminados)
    - PlayerPickup.cs (rama de compra con PlayerMoneyStore.CanAfford/Spend; rejectClip; debug M)
    - Bar.unity (3 instancias PickupItem cableadas con DrinkDefinition)
tech_stack:
  added:
    - ScriptableObject Flyweight (DrinkDefinition)
    - SFX de rechazo con fallback doble (Resources.Load)
    - Tecla de debug M para agregar dinero en Play Mode
  patterns:
    - Flyweight: DrinkDefinition compartido por instancias del mismo tipo
    - Guard clause: PlayerMoneyStore.CanAfford antes de Pickup()
    - Resources.Load con fallback doble para rejectClip
key_files:
  created:
    - Assets/_Project/ScriptableObjects/Items/DrinkDefinition.cs
    - Assets/_Project/ScriptableObjects/Items/Instances/Cerveza.asset
    - Assets/_Project/ScriptableObjects/Items/Instances/Trago.asset
    - Assets/_Project/ScriptableObjects/Items/Instances/Whisky.asset
  modified:
    - Assets/_Project/Gameplay/Items/PickupItem.cs
    - Assets/_Project/Gameplay/Player/PlayerPickup.cs
    - Assets/Scenes/Bar.unity
decisions:
  - "D-04: referencia explícita DrinkDefinition en PickupItem.definition (campo [SerializeField] private)"
  - "D-06: DrinkDefinition es la única fuente de verdad de price/alcoholPerSip/maxSips; campos hardcodeados eliminados"
  - "D-08: Whisky $35 > Trago $20 > Cerveza $10 (bebidas más fuertes cuestan más)"
  - "D-09: precios configurables desde Inspector vía DrinkDefinition assets"
  - "D-10: rejectClip cargado con fallback Audio/SFX/NoMoney → Audio/SFX/PayDrink"
metrics:
  duration_minutes: 6
  completed_date: 2026-06-22
  tasks_completed: 2
  tasks_total: 2
  files_created: 7
  files_modified: 3
---

# Phase 1 Plan 02: Slice B — Comprar bebidas con dinero real (SUMMARY)

**One-liner:** DrinkDefinition Flyweight SO (price + alcoholPerSip + maxSips) + compra con PlayerMoneyStore.CanAfford/Spend + SFX de rechazo con fallback doble; elimina enum PickupType y todos los campos hardcodeados de PickupItem.

## Tasks Completadas

| Task | Nombre | Commit | Archivos clave |
|------|--------|--------|----------------|
| 1 | DrinkDefinition + migración PickupItem + cablear Bar | 37adefd | DrinkDefinition.cs, Cerveza/Trago/Whisky.asset, PickupItem.cs, PlayerPickup.cs, Bar.unity |
| 2 | Compra con dinero en PlayerPickup + SFX de rechazo | 1071d0b | PlayerPickup.cs |

## Que se construyo

### Task 1 — Flyweight DrinkDefinition + migración de PickupItem

**DrinkDefinition.cs:** ScriptableObject con `[CreateAssetMenu]`. Campos `[SerializeField] private`: `drinkName`, `price`, `alcoholPerSip`, `maxSips`. Properties de solo lectura delegadas. Sin namespace (convención global del proyecto). Análogo exacto de SellableDefinition.cs del Plan 01.

**Assets Flyweight:**
- `Cerveza.asset`: drinkName="Cerveza", price=$10, alcoholPerSip=1, maxSips=4
- `Trago.asset`: drinkName="Trago", price=$20, alcoholPerSip=2, maxSips=3
- `Whisky.asset`: drinkName="Whisky", price=$35, alcoholPerSip=3, maxSips=2

**PickupItem.cs — migración completa:**
- Eliminado: `enum PickupType`, campo `public PickupType pickupType`, `public string itemName`, `public int maxSips`, `public int beerAlcoholPerSip`, `public int cocktailAlcoholPerSip`, `public int whiskyAlcoholPerSip`, property `ResolvedPickupType`
- Agregado: `[SerializeField] private DrinkDefinition definition`; properties `Definition`, `Price`, `AlcoholPerSip`, `MaxSips` que delegan al SO con fallback seguro (`definition != null ? ... : default`)
- `infiniteSupply`, `heldVisualPrefab`, `overrideHeldVisualScale`, `heldVisualScale`, `highlightColor` conservados

**Bar.unity:** 3 instancias de PickupItem en escena cableadas con su DrinkDefinition correspondiente (Trago.asset → instancia Trago, Whisky.asset → instancia Whisky, Cerveza.asset → instancia Cerveza). Las referencias usan el fileID 11400000 del ScriptableObject.

**PlayerPickup.cs (Task 1):** `item.ResolvedPickupType.ToString()` reemplazado por `item.Definition?.DrinkName ?? item.gameObject.name` en `CreateHeldVisual`; `item.maxSips` (campo público) reemplazado por `item.MaxSips` (property PascalCase) en `Pickup()`.

### Task 2 — Compra con dinero + SFX de rechazo

**PlayerPickup.cs — rama de compra:**
```
else if (currentPickupItem != null)
{
    int precio = currentPickupItem.Price;
    if (!PlayerMoneyStore.CanAfford(precio))
    {
        if (rejectClip != null && sfxSource != null)
            sfxSource.PlayOneShot(rejectClip, Mathf.Clamp01(rejectVolume));
        return;
    }
    PlayerMoneyStore.Spend(precio);
    Pickup(currentPickupItem);
}
```

**SFX de rechazo:** `rejectClip` cargado en `Awake()` con patrón fallback doble: `Audio/SFX/NoMoney` → `Audio/SFX/PayDrink`. Si ninguno existe, `rejectClip` queda null y el bloque no suena (sin error).

**Tecla debug M:** `Input.GetKeyDown(debugAddMoneyKey)` agrega `$50` al saldo vía `PlayerMoneyStore.Add(50)` con `Debug.Log("[PlayerPickup] Debug: dinero añadido. Saldo: $X")`. Configurable desde Inspector (`KeyCode.M`, `50`).

**Trueque eliminado:** La condición `currentPickupItem != null && hasHeldObject` ya no existe en el código. La compra de bebidas es independiente de si el jugador lleva un objeto en mano.

## Decisiones tomadas

| Decision | Descripcion | Razon |
|----------|-------------|-------|
| Prefabs sin PickupItem | Las bebidas no tienen PickupItem en el prefab — se agrega directamente en Bar.unity | Los prefabs existentes son mesh-only; PickupItem se añade como componente de escena |
| Fallback rejectClip a PayDrink | Si NoMoney.mp3 no existe, usa PayDrink como fallback | NoMoney.mp3 no está en el proyecto; PayDrink es el único SFX disponible |
| definition cableado en Bar.unity | Las 3 instancias PickupItem de Bar.unity tienen definition asignada en YAML | Es donde vive el componente PickupItem; los prefabs no tienen este componente |

## Deviaciones del Plan

### Auto-fixed Issues

Ninguna desviacion automatica fue necesaria. El plan se ejecuto exactamente como estaba especificado.

**Nota de arquitectura:** Los prefabs de bebida (Whisky.prefab, Cerveza.prefab, Trago.prefab) no contienen el componente PickupItem — son solo mallas. El componente PickupItem se agrega como MonoBehaviour override en Bar.unity sobre los GameObjects stripped. Por lo tanto, el cableado de `definition` se hizo en las 3 instancias de escena (Bar.unity) en lugar de en los prefabs. Esto es correcto para el patrón Flyweight: las 3 instancias en escena apuntan a sus respectivos assets (no a copias), cumpliendo PAT-01.

## Known Stubs

Ninguno — el plan completa el loop de compra-venta sin stubs. DrinkDefinition es la única fuente de verdad de precio/alcohol/sips. PlayerMoneyStore.CanAfford/Spend implementados y funcionales desde Plan 01.

## Threat Flags

No se introdujo superficie de ataque nueva. El análisis de T-02-03 (DoS en PlayerPickup.Update) se mitigó con la combinación `PlayOneShot` (no bloquea hilo) + `return` early (evita `Pickup()`) tal como especifica el threat model.

## Self-Check: PASSED

| Item | Resultado |
|------|-----------|
| DrinkDefinition.cs | FOUND |
| Cerveza.asset | FOUND |
| Trago.asset | FOUND |
| Whisky.asset | FOUND |
| PickupItem.cs sin enum PickupType | VERIFIED (grep=0) |
| PickupItem.cs sin beerAlcoholPerSip | VERIFIED (grep=0) |
| PickupItem.cs sin ResolvedPickupType | VERIFIED (grep=0) |
| PickupItem.cs con DrinkDefinition definition | VERIFIED (grep=1) |
| PickupItem.cs con definition.Price | VERIFIED (grep=1) |
| PlayerPickup.cs con PlayerMoneyStore.CanAfford | VERIFIED (grep=1) |
| PlayerPickup.cs con PlayerMoneyStore.Spend | VERIFIED (grep=1) |
| PlayerPickup.cs con rejectClip | VERIFIED (grep=7) |
| PlayerPickup.cs sin hasHeldObject | VERIFIED (grep=0) |
| Commit 37adefd (Task 1) | FOUND |
| Commit 1071d0b (Task 2) | FOUND |
