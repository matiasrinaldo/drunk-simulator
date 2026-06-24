using UnityEngine;

/// <summary>
/// Inicializador de escena para Home. Captura el total de CarryableObject presentes
/// en la escena y lo persiste en HomeObjectsTotalStore para que GameManager pueda
/// evaluar la condicion de victoria al llegar desde City (D-06).
///
/// Usa Awake() (no Start()) para que el total este disponible antes de que cualquier
/// otro MonoBehaviour lea el store en su propio Awake o Start.
///
/// Guard: si HomeObjectsTotalStore.Total > 0 ya fue capturado en una carga anterior
/// (el store es estatico y sobrevive recargas de escena), no sobreescribe el valor.
/// </summary>
public class HomeInitializer : MonoBehaviour
{
    void Awake()
    {
        // Calculamos el total buscando tambien los inactivos (por si un CarryableObject.Awake()
        // ya se desactivo a si mismo al cargar la escena tras un NewGame o re-entrada) (WR-04).
        var objetos = FindObjectsByType<CarryableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int total = objetos.Length;

        HomeObjectsTotalStore.Set(total);

        Debug.Log($"[HomeInitializer] Total de objetos en Home: {total}.");

        if (total == 0)
        {
            Debug.LogWarning("[HomeInitializer] No se encontraron CarryableObject en Home — la condicion de victoria es imposible.");
        }
    }
}
