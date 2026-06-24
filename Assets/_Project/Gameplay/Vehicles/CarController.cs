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

    [Header("Driving Effects")]
    [SerializeField] private bool enableDrivingEffects = true;
    [SerializeField] private float smokeSpeedThreshold = 0.6f;
    [SerializeField] private float smokeRateWhenDriving = 18f;
    [SerializeField] private float sparkSpeedThreshold = 0.8f;
    [SerializeField] private float sparkSteerThreshold = 0.15f;
    [SerializeField] private float sparkRateWhenTurning = 140f;
    [SerializeField] private Vector3 smokeLocalPosition = new Vector3(0f, 0.25f, -1.35f);
    [SerializeField] private Vector3 frontLeftWheelLocalPosition = new Vector3(-0.9f, 0.12f, 0.85f);
    [SerializeField] private Vector3 frontRightWheelLocalPosition = new Vector3(0.9f, 0.12f, 0.85f);
    [SerializeField] private Vector3 rearLeftWheelLocalPosition = new Vector3(-0.9f, 0.12f, -0.85f);
    [SerializeField] private Vector3 rearRightWheelLocalPosition = new Vector3(0.9f, 0.12f, -0.85f);

    private Rigidbody rb;
    private bool isControlled;
    private float throttleInput;
    private float steerInput;
    private DrunkManager drunkManager;
    private ParticleSystem smokeParticles;
    private ParticleSystem[] sparkParticles;
    private Material smokeMaterial;
    private Material sparkMaterial;

    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public bool IsControlled => isControlled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
        drunkManager = FindFirstObjectByType<DrunkManager>();
        CreateDrivingEffects();
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
            UpdateDrivingEffects(0f, 0f);
            return;
        }

        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (!isControlled)
        {
            UpdateDrivingEffects(0f, 0f);
            return;
        }

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

        UpdateDrivingEffects(rb.linearVelocity.magnitude, effectiveSteer);
    }

    private void CreateDrivingEffects()
    {
        if (!enableDrivingEffects) return;

        smokeParticles = CreateSmokeParticles();
        sparkParticles = new[]
        {
            CreateSparkParticles("Chispas Rueda Delantera Izquierda", frontLeftWheelLocalPosition),
            CreateSparkParticles("Chispas Rueda Delantera Derecha", frontRightWheelLocalPosition),
            CreateSparkParticles("Chispas Rueda Trasera Izquierda", rearLeftWheelLocalPosition),
            CreateSparkParticles("Chispas Rueda Trasera Derecha", rearRightWheelLocalPosition)
        };
    }

    private ParticleSystem CreateSmokeParticles()
    {
        ParticleSystem particles = CreateParticleSystemChild("Humo Auto", smokeLocalPosition);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.startLifetime = 1.1f;
        main.startSpeed = 0.55f;
        main.startSize = 0.35f;
        main.startColor = new Color(0.45f, 0.45f, 0.45f, 0.45f);
        main.gravityModifier = -0.05f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.12f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetOrCreateSmokeMaterial();

        particles.Play();
        return particles;
    }

    private ParticleSystem CreateSparkParticles(string effectName, Vector3 localPosition)
    {
        ParticleSystem particles = CreateParticleSystemChild(effectName, localPosition);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;
        main.startLifetime = 0.3f;
        main.startSpeed = 4.8f;
        main.startSize = 0.09f;
        main.startColor = Color.yellow;
        main.gravityModifier = 1.2f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetOrCreateSparkMaterial();

        particles.Play();
        return particles;
    }

    private ParticleSystem CreateParticleSystemChild(string effectName, Vector3 localPosition)
    {
        GameObject effect = new GameObject(effectName);
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = localPosition;
        return effect.AddComponent<ParticleSystem>();
    }

    private void UpdateDrivingEffects(float speed, float effectiveSteer)
    {
        if (!enableDrivingEffects) return;

        bool shouldSmoke = isControlled && speed > smokeSpeedThreshold;
        SetEmissionRate(smokeParticles, shouldSmoke ? smokeRateWhenDriving : 0f);

        if (sparkParticles == null) return;

        bool shouldSpark = isControlled && speed > sparkSpeedThreshold && Mathf.Abs(effectiveSteer) > sparkSteerThreshold;
        bool turningLeft = effectiveSteer < -sparkSteerThreshold;
        bool turningRight = effectiveSteer > sparkSteerThreshold;

        for (int i = 0; i < sparkParticles.Length; i++)
        {
            bool isLeftWheel = i == 0 || i == 2;
            bool isRightWheel = i == 1 || i == 3;
            bool isTurningSide = (turningLeft && isLeftWheel) || (turningRight && isRightWheel);
            SetEmissionRate(sparkParticles[i], shouldSpark && isTurningSide ? sparkRateWhenTurning : 0f);
        }
    }

    private static void SetEmissionRate(ParticleSystem particles, float rate)
    {
        if (particles == null) return;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
    }

    private Material GetOrCreateSmokeMaterial()
    {
        if (smokeMaterial != null) return smokeMaterial;

        smokeMaterial = CreateParticleMaterial("Humo Auto Material", new Color(0.45f, 0.45f, 0.45f, 0.45f));
        return smokeMaterial;
    }

    private Material GetOrCreateSparkMaterial()
    {
        if (sparkMaterial != null) return sparkMaterial;

        sparkMaterial = CreateParticleMaterial("Chispas Auto Material", Color.yellow);
        return sparkMaterial;
    }

    private static Material CreateParticleMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);

        return material;
    }

    /// <summary>
    /// Detecta colision con un obstaculo letal mientras el auto esta controlado (D-01/D-02/D-03).
    /// Guard: si !isControlled, el jugador va a pie — no cuenta como derrota (D-02).
    /// Al confirmar la colision: frena el auto, bloquea el control, notifica al GameManager.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // D-02: Solo cuenta si el jugador esta actualmente conduciendo.
        if (!isControlled) return;

        // D-01: Solo obstaculos marcados con LethalObstacle disparan la derrota.
        LethalObstacle obstacle = collision.gameObject.GetComponent<LethalObstacle>();
        if (obstacle == null) return;

        // D-03: Frenar el auto antes de disparar la transicion de escena.
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        SetControlled(false);

        // Notificar al GameManager (patron FindFirstObjectByType con null-check).
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.OnCarCrash();
    }
}
