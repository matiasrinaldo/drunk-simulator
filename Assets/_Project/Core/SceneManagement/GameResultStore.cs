/// <summary>
/// Posibles resultados de una partida. Se comunica entre escenas via
/// <see cref="GameResultStore"/> (D-12).
/// </summary>
public enum GameResult { None, Victory, Defeat }

/// <summary>
/// Guarda el resultado de la partida (victoria o derrota) entre cargas de
/// escena (Single mode). GameManager lo escribe antes de cargar Result;
/// ResultScreenController lo lee en Start() para mostrar el texto correcto.
/// Sigue el patron de DrunkLevelStore y CarStateStore.
/// </summary>
public static class GameResultStore
{
    /// <summary>Resultado de la partida actual.</summary>
    public static GameResult Result { get; private set; } = GameResult.None;

    /// <summary>Guarda el resultado para que sobreviva la carga de la escena Result.</summary>
    public static void Set(GameResult result)
    {
        Result = result;
    }

    /// <summary>Resetea el resultado a None (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        Result = GameResult.None;
    }
}
