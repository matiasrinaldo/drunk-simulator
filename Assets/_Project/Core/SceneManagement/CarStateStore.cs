using UnityEngine;

/// <summary>
/// Guarda la posicion/rotacion del auto entre cargas de escena (Single mode).
/// Asi, al volver a la City (por ejemplo saliendo del bar) el auto sigue donde
/// lo dejaste estacionado en vez de resetearse a su posicion por defecto.
/// Analogo a <see cref="PlayerSpawner.NextSpawnId"/>, pero para el vehiculo.
/// </summary>
public static class CarStateStore
{
    public static bool HasSavedState { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Save(Transform car)
    {
        if (car == null) return;
        Position = car.position;
        Rotation = car.rotation;
        HasSavedState = true;
    }

    /// <summary>Olvida el estado guardado (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        HasSavedState = false;
    }
}
