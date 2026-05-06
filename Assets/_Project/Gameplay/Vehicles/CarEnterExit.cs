using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerCarController playerCarController;
    [SerializeField] private Transform player;

    [Header("Config")]
    [SerializeField] private float interactRange = 5f;

    private CommandQueue commandQueue;

    void Awake()
    {
        commandQueue = FindFirstObjectByType<CommandQueue>();
        if (playerCarController == null) playerCarController = FindFirstObjectByType<PlayerCarController>();
    }

    void Update()
    {
        if (playerCarController == null || player == null) return;
        if (playerCarController.IsInCar) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        float distance = Vector3.Distance(player.position, transform.position);
        if (distance > interactRange) return;

        if (commandQueue != null) commandQueue.Enqueue(new EnterCarCommand(playerCarController));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
