using UnityEngine;

/// <summary>
/// Guarda el nivel de alcohol del jugador entre cargas de escena (Single mode).
/// El DrunkManager se reconstruye en cada escena, asi que su nivel de instancia
/// se perderia; este store mantiene la borrachera continua entre City/Bar/Home.
/// Se resetea al cerrar el juego o al llamar Clear() (Nueva partida).
/// Sigue el patron de CarStateStore y PlayerMoneyStore.
/// </summary>
public static class DrunkLevelStore
{
    /// <summary>Nivel de alcohol persistido entre escenas.</summary>
    public static int AlcoholLevel { get; private set; } = 0;

    /// <summary>Guarda el nivel actual para que sobreviva la proxima carga de escena.</summary>
    public static void Save(int level)
    {
        AlcoholLevel = Mathf.Max(0, level);
    }

    /// <summary>Resetea el nivel a cero (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        AlcoholLevel = 0;
    }
}
