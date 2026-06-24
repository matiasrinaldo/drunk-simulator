using UnityEngine;

/// <summary>
/// Trigger de proximidad de single-fire para una puerta decorativa.
/// Al acercarse el jugador, dispara la transicion Closed->Open en el Animator de la puerta.
/// CRITICO: NO carga ninguna escena (SceneManager.LoadSceneAsync nunca se llama aqui).
/// Esta puerta es puramente decorativa y desacoplada del flujo de carga de escenas.
/// Evita el pitfall de que la escena se descargue antes de ver la animacion.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorProximityTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string playerTag = "player";

    [Header("Referencia al Animator de la puerta")]
    [SerializeField] private Animator doorAnimator;

    // Bandera para que el trigger se dispare solo una vez
    private bool triggered;

    /// <summary>
    /// En Reset() forzamos isTrigger = true para que el collider sea trigger por defecto
    /// al agregar este componente, igual que BarDoorTrigger.
    /// </summary>
    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Single-fire: si ya se disparo, ignoramos
        if (triggered) return;

        // Solo reacciona al jugador (tag "player" con minuscula, igual que en el proyecto)
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        // Disparar la transicion Closed->Open en el Animator de la puerta decorativa.
        // Solo se anima: esta puerta NO carga ninguna escena.
        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");
    }
}
