using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParkingSpot : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Empty child que marca donde aparece el player al bajar del auto.")]
    [SerializeField] private Transform exitPoint;

    [Header("Config")]
    [SerializeField] private float maxExitSpeed = 1f;

    private CarController carInside;

    public Transform ExitPoint => exitPoint != null ? exitPoint : transform;
    public bool HasCar => carInside != null;

    public bool CanExit()
    {
        return carInside != null && carInside.CurrentSpeed <= maxExitSpeed;
    }

    void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<CarController>();
        if (car != null) carInside = car;
    }

    void OnTriggerExit(Collider other)
    {
        var car = other.GetComponentInParent<CarController>();
        if (car != null && car == carInside) carInside = null;
    }
}
