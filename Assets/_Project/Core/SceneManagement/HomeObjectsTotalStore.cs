/// <summary>
/// Guarda el total de objetos cargables presentes en la escena Home al inicio
/// de la partida. HomeInitializer lo setea en Awake (primera carga de Home);
/// GameManager lo lee al evaluar la condicion de victoria (D-06).
/// Sigue el patron de CarStateStore.
/// </summary>
public static class HomeObjectsTotalStore
{
    /// <summary>Total de objetos cargables en Home capturado al inicio de partida.</summary>
    public static int Total { get; private set; } = 0;

    /// <summary>Guarda el total de objetos (debe ser >= 0). Ignorado si el valor es negativo.</summary>
    public static void Set(int total)
    {
        if (total >= 0) Total = total;
    }

    /// <summary>Resetea el total a cero (util al empezar una partida nueva).</summary>
    public static void Clear()
    {
        Total = 0;
    }
}
