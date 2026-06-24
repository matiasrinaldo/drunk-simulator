using System.Collections.Generic;

/// <summary>
/// Recuerda que objetos cargables (<see cref="CarryableObject"/>) ya fueron
/// tomados de la casa, para que NO reaparezcan al recargar la escena Home
/// (cada puerta hace LoadSceneAsync, lo que reconstruye la escena de cero).
///
/// Es estado en memoria, igual que <see cref="CarStateStore"/>: se mantiene
/// entre cargas de escena dentro de la misma partida y se resetea al cerrar el
/// juego. Si en el futuro se agrega un menu "Nueva partida", llamar a Clear().
/// </summary>
public static class DeliveredObjectsStore
{
    static readonly HashSet<string> takenIds = new HashSet<string>();

    /// <summary>Marca un objeto como ya tomado (no debe volver a aparecer).</summary>
    public static void MarkTaken(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        takenIds.Add(id);
    }

    /// <summary>Indica si el objeto ya fue tomado en esta partida.</summary>
    public static bool IsTaken(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return takenIds.Contains(id);
    }

    /// <summary>Cantidad de objetos tomados en esta partida.</summary>
    public static int TakenCount => takenIds.Count;

    /// <summary>Olvida todo lo tomado (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        takenIds.Clear();
    }
}
