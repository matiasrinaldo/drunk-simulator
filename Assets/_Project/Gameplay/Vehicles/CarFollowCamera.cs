using UnityEngine;

public class CarFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Drunk Camera")]
    [SerializeField] private float drunkRollAmount = 6f;
    [SerializeField] private float drunkRollFrequency = 1.2f;
    [SerializeField] private float drunkPitchAmount = 2.5f;
    [SerializeField] private float drunkPitchFrequency = 0.9f;
    [SerializeField] private float drunkYawAmount = 3f;
    [SerializeField] private float drunkYawFrequency = 0.7f;
    [SerializeField] private float drunkLookOffsetAmount = 0.6f;
    [SerializeField] private float drunkLookOffsetFrequency = 0.8f;

    private Vector3 currentVelocity;
    private DrunkManager drunkManager;

    void Awake()
    {
        drunkManager = FindFirstObjectByType<DrunkManager>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.TransformPoint(localOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, positionSmoothTime);

        float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;

        Vector3 lookOffset = lookAtOffset;
        if (drunkAmount > 0f)
        {
            lookOffset.x += Mathf.Sin(Time.time * drunkLookOffsetFrequency) * drunkLookOffsetAmount * drunkAmount;
            lookOffset.y += Mathf.Cos(Time.time * drunkLookOffsetFrequency * 1.3f) * drunkLookOffsetAmount * 0.5f * drunkAmount;
        }

        transform.LookAt(target.position + lookOffset);

        if (drunkAmount > 0f)
        {
            float roll = Mathf.Sin(Time.time * drunkRollFrequency * Mathf.PI * 2f) * drunkRollAmount * drunkAmount;
            float pitch = Mathf.Sin(Time.time * drunkPitchFrequency * Mathf.PI * 2f) * drunkPitchAmount * drunkAmount;
            float yaw = Mathf.Cos(Time.time * drunkYawFrequency * Mathf.PI * 2f) * drunkYawAmount * drunkAmount;
            transform.rotation *= Quaternion.Euler(pitch, yaw, roll);
        }
    }
}
