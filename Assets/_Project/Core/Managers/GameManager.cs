using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager central de estado de juego. Vive per-escena en City (no DontDestroyOnLoad — D-11).
/// Recibe la senal de choque desde <see cref="CarController"/> y evalua la condicion
/// de victoria al recibir la senal de llegada a Home desde CityHomeDoorTrigger.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Condicion de victoria")]
    [Tooltip("Nivel minimo de alcohol requerido para ganar (D-05).")]
    [SerializeField] private int minAlcoholRequired = 6;

    /// <summary>
    /// Llamado por <see cref="CarController.OnCollisionEnter"/> cuando el auto
    /// (IsControlled=true) choca contra un obstaculo letal (D-01/D-02/D-03).
    /// Setea el resultado a Derrota y carga la escena Result.
    /// La guard de IsControlled ya esta en CarController — aqui no se vuelve a verificar.
    /// </summary>
    public void OnCarCrash()
    {
        Debug.Log("[GameManager] Choque detectado — cargando Result con Defeat.");
        GameResultStore.Set(GameResult.Defeat);
        SceneManager.LoadSceneAsync("Result");
    }

    /// <summary>
    /// Llamado por CityHomeDoorTrigger cuando el jugador llega a Home manejando (D-04).
    /// Evalua ambas condiciones de victoria:
    /// 1. Todos los objetos entregados (DeliveredObjectsStore.TakenCount >= HomeObjectsTotalStore.Total).
    /// 2. Nivel de alcohol suficiente (DrunkLevelStore.AlcoholLevel >= minAlcoholRequired).
    /// Si ambas se cumplen → Victory + carga Result. Caso contrario → carga Home.
    /// Guard: si HomeObjectsTotalStore.Total == 0, la partida no esta inicializada → carga Home (Pitfall 5).
    /// </summary>
    public void OnPlayerArrivedHome()
    {
        // Guard: partida no inicializada (el jugador fue directo a City sin pasar por Home).
        if (HomeObjectsTotalStore.Total == 0)
        {
            Debug.Log("[GameManager] HomeObjectsTotalStore.Total == 0 — partida no inicializada, cargando Home.");
            SceneManager.LoadSceneAsync("Home");
            return;
        }

        bool allDelivered = DeliveredObjectsStore.SoldCount >= HomeObjectsTotalStore.Total;
        bool drunkEnough  = DrunkLevelStore.AlcoholLevel >= minAlcoholRequired;

        Debug.Log($"[GameManager] Evaluacion de victoria — " +
                  $"vendidos: {DeliveredObjectsStore.SoldCount}/{HomeObjectsTotalStore.Total}, " +
                  $"alcohol: {DrunkLevelStore.AlcoholLevel}/{minAlcoholRequired}");

        if (allDelivered && drunkEnough)
        {
            Debug.Log("[GameManager] Victoria — cargando Result con Victory.");
            GameResultStore.Set(GameResult.Victory);
            SceneManager.LoadSceneAsync("Result");
        }
        else
        {
            Debug.Log("[GameManager] Condicion de victoria no cumplida — volviendo a Home.");
            SceneManager.LoadSceneAsync("Home");
        }
    }

    /// <summary>
    /// Reset completo de partida (D-13). Llama Clear() de todos los stores y carga Home desde cero.
    /// Debe llamarse desde ResultScreenController al presionar el boton Reintentar (D-09).
    /// </summary>
    public static void NewGame()
    {
        // Restaurar el HUD oculto en Result (CR-01)
        HUDController.SetVisible(true);
        CarStateStore.Clear();
        DeliveredObjectsStore.Clear();
        DrunkLevelStore.Clear();
        PlayerMoneyStore.Clear();
        HeldObjectStore.Clear();
        HomeObjectsTotalStore.Clear();
        GameResultStore.Clear();
        PlayerSpawner.NextSpawnId = null;

        Debug.Log("[GameManager] NewGame — todos los stores reseteados, cargando Home.");
        SceneManager.LoadSceneAsync("Home");
    }
}
