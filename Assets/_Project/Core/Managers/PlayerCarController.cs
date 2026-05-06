using UnityEngine;

public class PlayerCarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject player;
    [SerializeField] private Camera fpsCamera;
    [SerializeField] private CarController car;
    [SerializeField] private Camera carCamera;

    public bool IsInCar { get; private set; }

    private CommandQueue commandQueue;

    void Awake()
    {
        commandQueue = FindFirstObjectByType<CommandQueue>();
        ApplyCameraState();
    }

    void Update()
    {
        if (!IsInCar) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        ParkingSpot spot = FindActiveSpotForExit();
        if (spot == null) return;

        if (commandQueue != null) commandQueue.Enqueue(new ExitCarCommand(this, spot));
    }

    public void EnterCar()
    {
        if (IsInCar || car == null || player == null) return;

        player.SetActive(false);
        car.SetControlled(true);
        IsInCar = true;
        ApplyCameraState();
    }

    public void ExitCar(ParkingSpot spot)
    {
        if (!IsInCar || spot == null) return;

        car.SetControlled(false);
        IsInCar = false;

        Transform exit = spot.ExitPoint;
        player.transform.SetPositionAndRotation(exit.position, exit.rotation);
        player.SetActive(true);

        ApplyCameraState();
    }

    private void ApplyCameraState()
    {
        if (fpsCamera != null) fpsCamera.enabled = !IsInCar;
        if (carCamera != null) carCamera.enabled = IsInCar;
    }

    private ParkingSpot FindActiveSpotForExit()
    {
        ParkingSpot[] spots = FindObjectsByType<ParkingSpot>(FindObjectsSortMode.None);
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i].CanExit()) return spots[i];
        }
        return null;
    }
}
