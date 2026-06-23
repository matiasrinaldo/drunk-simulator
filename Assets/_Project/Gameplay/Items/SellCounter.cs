using UnityEngine;

/// <summary>
/// Mostrador de venta en el Bar. El jugador mira el mostrador y aprieta E
/// para vender el objeto que tiene en mano. Requiere un Collider para que
/// PlayerPickup pueda detectarlo via raycast (mismo patron que BarDoorTrigger).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SellCounter : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip sellClip;
    [SerializeField, Range(0f, 1f)] private float sellVolume = 1f;

    [Header("Config")]
    [SerializeField] public KeyCode interactKey = KeyCode.E;

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
    /// Intenta vender el objeto que tiene el jugador en mano.
    /// Llamado desde PlayerPickup.Update cuando el jugador aprieta E mirando el mostrador.
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
        // La venta solo acredita dinero — unico punto de verdad del marcado en el pickup (CR-01).

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
