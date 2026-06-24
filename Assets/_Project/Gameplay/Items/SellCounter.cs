using UnityEngine;

/// <summary>
/// Mostrador de venta en el Bar. Funciona como zona automatica: cuando el jugador
/// entra al trigger con un objeto en mano, se vende solo (no hace falta apuntar ni
/// apretar nada). Requiere un Collider isTrigger lo bastante grande para que el
/// jugador lo atraviese al acercarse (mismo patron de deteccion que BarDoorTrigger:
/// OnTriggerEnter + CompareTag("player")).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SellCounter : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip sellClip;
    [SerializeField, Range(0f, 1f)] private float sellVolume = 1f;

    [Header("Config")]
    [SerializeField] private string playerTag = "player";

    AudioSource sfxSource;

    void Awake()
    {
        // Resolver AudioSource con patron de fallback (igual que PlayerPickup)
        sfxSource = gameObject.GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;

        // Cargar clip de venta con ruta de fallback (patron BackgroundMusicManager)
        if (sellClip == null)
        {
            sellClip = Resources.Load<AudioClip>("Audio/SFX/Sell");
            if (sellClip == null)
            {
                sellClip = Resources.Load<AudioClip>("Audio/SFX/PayDrink");
            }
        }
    }

    void Reset()
    {
        // Asegurar que el collider sea trigger al agregar el componente
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>
    /// Vende automaticamente al entrar a la zona del mostrador con un objeto en mano.
    /// TrySell ya limpia HeldObjectStore, asi que no se revende en el mismo ingreso.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        TrySell();
    }

    /// <summary>
    /// Intenta vender el objeto que tiene el jugador en mano. Si no hay objeto, no hace nada.
    /// </summary>
    public void TrySell()
    {
        if (!HeldObjectStore.HasHeldObject) return;

        SellableDefinition def = HeldObjectStore.HeldDefinition;
        if (def == null) return;

        int valor = def.SellValue;

        // Acreditar dinero al jugador
        PlayerMoneyStore.Add(valor);

        // El objeto ya quedo marcado como entregado al agarrarlo (CarryableObject.OnPickedUp).
        // La venta solo acredita dinero y cuenta para la condicion de victoria (CR-02).
        DeliveredObjectsStore.IncrementSoldCount();
        WorldTimeStore.MarkSoldInBar();

        // Limpiar el objeto sostenido
        HeldObjectStore.Clear();

        // Feedback de audio
        if (sellClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(sellClip, Mathf.Clamp01(sellVolume));
        }

        Debug.Log($"[SellCounter] Objeto vendido por ${valor}. Saldo: ${PlayerMoneyStore.Money}");
    }
}
