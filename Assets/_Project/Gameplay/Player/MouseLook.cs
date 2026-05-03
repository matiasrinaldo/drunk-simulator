using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [Header("Drunk Camera")]
    public float pitchWobbleAmount = 2.5f;
    public float pitchWobbleFrequency = 1.1f;
    public float yawWobbleAmount = 1.25f;
    public float yawWobbleFrequency = 0.9f;
    public float rollWobbleAmount = 4f;
    public float rollWobbleFrequency = 1.6f;

    DrunkManager drunkManager;

    float xRotation = 0f;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody != null)
        {
            drunkManager = playerBody.GetComponent<DrunkManager>();
        }

        if (drunkManager == null)
        {
            drunkManager = FindFirstObjectByType<DrunkManager>();
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float drunkAmount = drunkManager != null ? drunkManager.NormalizedLevel : 0f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        float pitchWobble = Mathf.Sin(Time.time * pitchWobbleFrequency) * pitchWobbleAmount * drunkAmount;
        float yawWobble = Mathf.Cos(Time.time * yawWobbleFrequency) * yawWobbleAmount * drunkAmount;
        float rollWobble = Mathf.Sin(Time.time * rollWobbleFrequency) * rollWobbleAmount * drunkAmount;

        transform.localRotation = Quaternion.Euler(xRotation + pitchWobble, yawWobble, rollWobble);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
