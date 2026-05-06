using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField] private float thrust = 12000f;
    [SerializeField] private float reverseMultiplier = 0.5f;
    [Tooltip("Velocidad angular maxima del giro en rad/s. Se aplica directo sobre Rigidbody.angularVelocity.")]
    [SerializeField] private float steerSpeed = 2.5f;
    [SerializeField] private float maxSpeed = 18f;

    [Tooltip("Por debajo de esta velocidad, el torque de giro se atenua linealmente hasta 0 (no gira parado).")]
    [SerializeField] private float steerSpeedThreshold = 1.5f;

    [Header("Stability")]
    [Tooltip("Offset local del centro de masa. Bajarlo evita que el auto se de vuelta facil.")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    private Rigidbody rb;
    private bool isControlled;
    private float throttleInput;
    private float steerInput;

    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public bool IsControlled => isControlled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
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

        // Acelerar / frenar (reversa atenuada).
        if (Mathf.Abs(throttleInput) > 0.01f && rb.linearVelocity.magnitude < maxSpeed)
        {
            float force = throttleInput >= 0f ? thrust : thrust * reverseMultiplier;
            rb.AddForce(transform.forward * (throttleInput * force), ForceMode.Force);
        }

        // Giro: seteamos angularVelocity directo cuando hay input. Sin input, el damping del Rigidbody atenua naturalmente.
        if (Mathf.Abs(steerInput) > 0.01f)
        {
            float speed = rb.linearVelocity.magnitude;
            float steerScale = Mathf.Clamp01(speed / steerSpeedThreshold);
            float forwardDot = Vector3.Dot(rb.linearVelocity, transform.forward);
            float steerSign = forwardDot >= 0f ? 1f : -1f;

            Vector3 angVel = rb.angularVelocity;
            angVel.y = steerInput * steerSpeed * steerScale * steerSign;
            rb.angularVelocity = angVel;
        }
    }
}
