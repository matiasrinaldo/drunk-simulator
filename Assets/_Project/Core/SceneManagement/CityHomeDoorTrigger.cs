using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class CityHomeDoorTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string sceneToLoad = "Home";
    [SerializeField] private string playerTag = "player";

    private bool triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        // Recordamos donde quedo estacionado el auto antes de descargar la City.
        var car = FindFirstObjectByType<CarController>();
        if (car != null) CarStateStore.Save(car.transform);

        // D-04: delegar al GameManager la decision de cargar Home o Result/Victory.
        // triggered=true se setea para evitar disparos multiples si el jugador
        // tiene multiples colliders o el async load tarda en destruir la escena (WR-01).
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            triggered = true;
            gm.OnPlayerArrivedHome();
            return;
        }

        // Fallback: comportamiento anterior si no hay GameManager en la escena.
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            triggered = true;
            SceneManager.LoadSceneAsync(sceneToLoad);
        }
        else
        {
            Debug.LogError($"[CityHomeDoorTrigger] Fallback no puede cargar escena '{sceneToLoad}'.");
        }
    }
}
