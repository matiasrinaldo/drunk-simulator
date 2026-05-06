using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public static string NextSpawnId;

    void Start()
    {
        if (string.IsNullOrEmpty(NextSpawnId)) return;

        SpawnPoint target = null;
        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.Id == NextSpawnId)
            {
                target = sp;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"[PlayerSpawner] No SpawnPoint found with id '{NextSpawnId}'");
            NextSpawnId = null;
            return;
        }

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);

        if (controller != null) controller.enabled = true;

        NextSpawnId = null;
    }
}
