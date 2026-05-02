using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float sprintMultiplier = 1.75f;
    public float gravity = -9.8f;
    public float yVelocity = 0f;
    public float jumpForce = 1f;

    public CharacterController controller;

    [Header("Drunk Movement")]
    public float lateralSwayDistance = 0.9f;
    public float lateralSwayFrequency = 1.4f;
    public float inputDriftAmount = 0.35f;
    public float inputDriftFrequency = 0.8f;

    DrunkManager drunkManager;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        drunkManager = GetComponent<DrunkManager>();

        if (drunkManager == null)
        {
            drunkManager = FindFirstObjectByType<DrunkManager>();
        }
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float drunkAmount = drunkManager != null ? drunkManager.EffectIntensity : 0f;

        float lateralSway = Mathf.Sin(Time.time * lateralSwayFrequency) * lateralSwayDistance * drunkAmount;
        float inputDrift = Mathf.Cos(Time.time * inputDriftFrequency) * inputDriftAmount * drunkAmount;

        Vector3 move = transform.right * (x + inputDrift) + transform.forward * z;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            currentSpeed *= sprintMultiplier;
        }

        move += transform.right * lateralSway;

        // gravedad
        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            yVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = new Vector3(0, yVelocity, 0);

        controller.Move((move * currentSpeed + velocity) * Time.deltaTime);
    }
}
