using UnityEngine;

public class CarFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.TransformPoint(localOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, positionSmoothTime);
        transform.LookAt(target.position + lookAtOffset);
    }
}
