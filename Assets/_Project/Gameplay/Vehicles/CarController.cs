using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField] private float thrust = 12000f;
    [SerializeField] private float reverseMultiplier = 0.5f;
    [SerializeField] private float steerSpeed = 2.5f;
    [SerializeField] private float maxSpeed = 18f;

    [Tooltip("Por debajo de esta velocidad, el torque de giro se atenua linealmente hasta 0 (no gira parado).")]
    [SerializeField] private float steerSpeedThreshold = 1.5f;

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Drunk Driving")]
    [Tooltip("Drift agregado al steer (en unidades de input, -1..1) por la borrachera.")]
    [SerializeField] private float drunkSteerDriftAmount = 0.55f;
    [SerializeField] private float drunkSteerDriftFrequency = 0.7f;
    [Tooltip("Drift agregado al acelerador por la borrachera.")]
    [SerializeField] private float drunkThrottleDriftAmount = 0.2f;
    [SerializeField] private float drunkThrottleDriftFrequency = 0.5f;
    [Tooltip("Torque random para que el auto trompee cuando esta muy borracho.")]
    [SerializeField] private float drunkYawJitterTorque = 800f;
    [SerializeField] private float drunkYawJitterFrequency = 1.3f;

    private Rigidbody rb;
    private bool isControlled;
    private float throttleInput;
    private float steerInput;
    private DrunkManager drunkManager;

    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public bool IsControlled => isControlled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
        drunkManager = FindFirstObjectByType<DrunkManager>();
    }

    void Start()
    {
        // Si veniamos de otra escena, restauramos donde dejamos el auto
        // (p. ej. estacionado frente al bar) en vez de la posicion por defecto.
        if (!CarStateStore.HasSavedState) return;

        transform.SetPositionAndRotation(CarStateStore.Position, CarStateStore.Rotation);
        rb.position = CarStateStore.Position;
        rb.rotation = CarStateStore.Rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void SetControlled(bool value)
    {
        isControlled = value;
        if (!value)
        {
            throttleInput = 0f;
            steerInput = 0f;
        }
    }

    void Update()
    {
        if (!isControlled)
        {
            throttleInput = 0f;
            steerInput = 0f;
            return;
        }

        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (!isControlled) return;

        float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;

        float steerDrift = Mathf.Sin(Time.time * drunkSteerDriftFrequency) * drunkSteerDriftAmount * drunkAmount;
        float throttleDrift = Mathf.Cos(Time.time * drunkThrottleDriftFrequency) * drunkThrottleDriftAmount * drunkAmount;

        float effectiveSteer = Mathf.Clamp(steerInput + steerDrift, -1f, 1f);
        float effectiveThrottle = Mathf.Clamp(throttleInput + throttleDrift, -1f, 1f);

        // Acelerar / frenar (reversa atenuada).
        if (Mathf.Abs(effectiveThrottle) > 0.01f && rb.linearVelocity.magnitude < maxSpeed)
        {
            float force = effectiveThrottle >= 0f ? thrust : thrust * reverseMultiplier;
            rb.AddForce(transform.forward * (effectiveThrottle * force), ForceMode.Force);
        }

        // Giro: seteamos angularVelocity directo cuando hay input. Sin input, el damping del Rigidbody atenua naturalmente.
        if (Mathf.Abs(effectiveSteer) > 0.01f)
        {
            float speed = rb.linearVelocity.magnitude;
            float steerScale = Mathf.Clamp01(speed / steerSpeedThreshold);
            float forwardDot = Vector3.Dot(rb.linearVelocity, transform.forward);
            float steerSign = forwardDot >= 0f ? 1f : -1f;

            Vector3 angVel = rb.angularVelocity;
            angVel.y = effectiveSteer * steerSpeed * steerScale * steerSign;
            rb.angularVelocity = angVel;
        }

        if (drunkAmount > 0f && drunkYawJitterTorque > 0f)
        {
            float jitter = Mathf.Sin(Time.time * drunkYawJitterFrequency * Mathf.PI * 2f) * drunkYawJitterTorque * drunkAmount;
            rb.AddTorque(transform.up * jitter, ForceMode.Force);
        }
    }
}
