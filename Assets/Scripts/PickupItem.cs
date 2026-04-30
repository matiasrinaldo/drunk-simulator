using UnityEngine;

public enum PickupType
{
    Trago,
    Cerveza,
    Whisky
}

public class PickupItem : MonoBehaviour
{
    public PickupType pickupType = PickupType.Trago;
    public float alcoholValue = 10f;
    public string itemName = "Trago";

    [Header("Optional")]
    public AudioClip pickupSfx;
    public GameObject heldVisualPrefab;
    public bool overrideHeldVisualScale;
    public Vector3 heldVisualScale = Vector3.zero;
    public Color highlightColor = new Color(1f, 0.94f, 0.5f, 1f);

    Renderer[] renderers;
    Color[] originalColors;
    bool isHighlighted;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = renderers[i].material.color;
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (!renderers[i].material.HasProperty("_Color")) continue;
            renderers[i].material.color = highlighted ? highlightColor : originalColors[i];
        }
    }

    public void OnPickedUp()
    {
        if (pickupSfx != null)
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = pickupSfx;
            audioSource.spatialBlend = 0f;
            audioSource.Play();
            Destroy(audioSource, pickupSfx.length);
        }

        Destroy(gameObject);
    }
}
