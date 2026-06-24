using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class HomeDoorTrigger : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string sceneToLoad = "City";
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

        triggered = true;
        WorldTimeStore.SetDay();
        SceneManager.LoadSceneAsync(sceneToLoad);
    }
}
