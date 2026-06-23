using System;
using UnityEngine;

/// <summary>
/// Guarda el dinero del jugador entre cargas de escena (Single mode).
/// Se resetea al cerrar el juego o al llamar Clear() (Nueva partida).
/// Sigue el patron de CarStateStore y DeliveredObjectsStore.
/// El jugador arranca con $0 (D-07).
/// </summary>
public static class PlayerMoneyStore
{
    /// <summary>Se dispara cuando el saldo cambia. Argumento: nuevo saldo.</summary>
    public static event Action<int> OnMoneyChanged;

    /// <summary>Saldo actual del jugador en pesos.</summary>
    public static int Money { get; private set; } = 0;

    /// <summary>Suma la cantidad indicada al saldo del jugador.</summary>
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        Debug.Log($"[PlayerMoneyStore] +${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);
    }

    /// <summary>Descuenta el monto indicado. Retorna false si no hay saldo suficiente.</summary>
    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;
        Money -= amount;
        Debug.Log($"[PlayerMoneyStore] -${amount}. Saldo: ${Money}");
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    /// <summary>Indica si el jugador tiene saldo suficiente para el monto dado.</summary>
    public static bool CanAfford(int amount) => Money >= amount;

    /// <summary>Resetea el dinero a cero (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        Money = 0;
        OnMoneyChanged?.Invoke(Money);
    }
}
