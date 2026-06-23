using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float selectionDistance = 3f;
    public float proximityDistance = 1.5f;
    public LayerMask pickupLayerMask = Physics.DefaultRaycastLayers;
    public Transform holdPoint;

    [Header("Aim Dot")]
    public bool showAimDot = true;
    public float aimDotSize = 4f;
    public Color aimDotColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip drinkSipClip;
    [SerializeField, Range(0f, 1f)] private float drinkSipVolume = 1f;
    [SerializeField] private AudioClip payDrinkClip;
    [SerializeField, Range(0f, 1f)] private float payDrinkVolume = 1f;

    Camera mainCamera;
    DrunkManager drunkManager;
    PickupItem currentPickupItem;
    PickupItem lastHighlightedItem;
    CarryableObject currentCarryable;
    CarryableObject lastHighlightedCarryable;
    GameObject currentHeldVisual;
    int heldAlcoholPerSip;
    int heldMaxSips;
    int heldSips;
    bool hasHeldDrink;
    SellCounter currentSellCounter;

    /// <summary>Delega a HeldObjectStore — persiste entre escenas.</summary>
    public bool HasHeldObject => HeldObjectStore.HasHeldObject;

    /// <summary>Libera el objeto sostenido (delegado a HeldObjectStore).</summary>
    public void ConsumeHeldObject()
    {
        HeldObjectStore.Clear();
    }
    AudioSource sfxSource;

    void Awake()
    {
        mainCamera = Camera.main;
        drunkManager = GetComponent<DrunkManager>();

        if (drunkManager == null)
        {
            drunkManager = FindFirstObjectByType<DrunkManager>();
        }

        if (holdPoint == null)
        {
            Transform parent = mainCamera != null ? mainCamera.transform : transform;
            GameObject holdPointObject = new GameObject("HoldPoint");
            holdPointObject.transform.SetParent(parent);
            holdPointObject.transform.localPosition = new Vector3(0.45f, -0.2f, 0.65f);
            holdPointObject.transform.localRotation = Quaternion.Euler(8f, -18f, 0f);
            holdPointObject.transform.localScale = Vector3.one;
            holdPoint = holdPointObject.transform;
        }

        sfxSource = gameObject.GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;

        if (drinkSipClip == null)
        {
            drinkSipClip = Resources.Load<AudioClip>("Audio/SFX/DrinkSip");
            if (drinkSipClip == null)
            {
                drinkSipClip = Resources.Load<AudioClip>("Audio/DrinkSip");
            }
        }

        if (payDrinkClip == null)
        {
            payDrinkClip = Resources.Load<AudioClip>("Audio/SFX/PayDrink");
            if (payDrinkClip == null)
            {
                payDrinkClip = Resources.Load<AudioClip>("Audio/PayDrink");
            }
        }
    }

    void Update()
    {
        UpdateSelectionByLook();

        if (Input.GetKeyDown(pickupKey))
        {
            if (hasHeldDrink)
            {
                DrinkHeldItem();
            }
            else if (currentSellCounter != null && HeldObjectStore.HasHeldObject)
            {
                // Vender el objeto al mostrador del Bar (D-01)
                currentSellCounter.TrySell();
            }
            else if (currentPickupItem != null)
            {
                // Compra de bebida (el trueque fue reemplazado por economía — Plan 01-01)
                Pickup(currentPickupItem);
            }
            else if (currentCarryable != null && !HeldObjectStore.HasHeldObject)
            {
                PickupCarryable(currentCarryable);
            }
        }
    }

    void OnGUI()
    {
        if (!showAimDot) return;

        float size = Mathf.Max(1f, aimDotSize);
        Rect dotRect = new Rect(
            (Screen.width - size) * 0.5f,
            (Screen.height - size) * 0.5f,
            size,
            size
        );

        Color previousColor = GUI.color;
        GUI.color = aimDotColor;
        GUI.DrawTexture(dotRect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    void UpdateSelectionByLook()
    {
        currentPickupItem = null;
        currentCarryable = null;
        currentSellCounter = null;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, selectionDistance, pickupLayerMask, QueryTriggerInteraction.Collide))
        {
            currentPickupItem = hit.collider.GetComponentInParent<PickupItem>();
            if (currentPickupItem == null)
            {
                currentCarryable = hit.collider.GetComponentInParent<CarryableObject>();
            }
            if (currentPickupItem == null && currentCarryable == null)
            {
                // Detectar mostrador de venta — mismo patron GetComponentInParent (Opcion A RESEARCH.md)
                currentSellCounter = hit.collider.GetComponentInParent<SellCounter>();
            }
        }

        if (currentPickupItem == null && currentCarryable == null)
        {
            currentPickupItem = FindClosestPickupInRange();
        }

        if (lastHighlightedItem != null && lastHighlightedItem != currentPickupItem)
        {
            lastHighlightedItem.SetHighlighted(false);
            lastHighlightedItem = null;
        }

        if (currentPickupItem != null && lastHighlightedItem != currentPickupItem)
        {
            currentPickupItem.SetHighlighted(true);
            lastHighlightedItem = currentPickupItem;
        }

        if (lastHighlightedCarryable != null && lastHighlightedCarryable != currentCarryable)
        {
            lastHighlightedCarryable.SetHighlighted(false);
            lastHighlightedCarryable = null;
        }

        if (currentCarryable != null && lastHighlightedCarryable != currentCarryable)
        {
            currentCarryable.SetHighlighted(true);
            lastHighlightedCarryable = currentCarryable;
        }
    }

    PickupItem FindClosestPickupInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, proximityDistance, pickupLayerMask, QueryTriggerInteraction.Collide);
        PickupItem closestItem = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Collider hit in hits)
        {
            PickupItem item = hit.GetComponentInParent<PickupItem>();
            if (item == null) continue;

            float distance = Vector3.Distance(transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        return closestItem;
    }

    void PickupCarryable(CarryableObject carryable)
    {
        if (carryable == null) return;

        // OnPickedUp llama HeldObjectStore.SetHeld — no hay que asignar hasHeldObject aqui.
        carryable.OnPickedUp();
        currentCarryable = null;
        lastHighlightedCarryable = null;
    }

    void Pickup(PickupItem item)
    {
        if (item == null) return;

        item.SetHighlighted(false);
        Destroy(currentHeldVisual);
        currentHeldVisual = CreateHeldVisual(item);
        heldAlcoholPerSip = item.AlcoholPerSip;
        heldMaxSips = Mathf.Max(1, item.MaxSips);
        heldSips = 0;
        hasHeldDrink = currentHeldVisual != null;

        if (payDrinkClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(payDrinkClip, Mathf.Clamp01(payDrinkVolume));
        }

        item.OnPickedUp();
        currentPickupItem = null;
        lastHighlightedItem = null;
    }

    void DrinkHeldItem()
    {
        if (!hasHeldDrink) return;

        if (drunkManager != null)
        {
            drunkManager.AddAlcohol(heldAlcoholPerSip);
        }

        if (drinkSipClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(drinkSipClip, Mathf.Clamp01(drinkSipVolume));
        }

        heldSips++;
        if (heldSips >= heldMaxSips)
        {
            Destroy(currentHeldVisual);
            currentHeldVisual = null;
            heldSips = 0;
            hasHeldDrink = false;
        }
    }

    GameObject CreateHeldVisual(PickupItem item)
    {
        if (item == null || holdPoint == null) return null;

        GameObject source = item.heldVisualPrefab != null ? item.heldVisualPrefab : item.gameObject;
        Vector3 originalWorldScale = item.transform.lossyScale;
        GameObject clone = Instantiate(source);
        clone.name = item.Definition != null ? item.Definition.DrinkName : item.gameObject.name;
        clone.transform.SetParent(holdPoint, false);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = item.overrideHeldVisualScale
            ? item.heldVisualScale
            : GetLocalScaleForWorldScale(originalWorldScale, holdPoint.lossyScale);

        foreach (Collider itemCollider in clone.GetComponentsInChildren<Collider>())
        {
            Destroy(itemCollider);
        }

        foreach (PickupItem pickupItem in clone.GetComponentsInChildren<PickupItem>())
        {
            Destroy(pickupItem);
        }

        return clone;
    }

    Vector3 GetLocalScaleForWorldScale(Vector3 worldScale, Vector3 parentWorldScale)
    {
        return new Vector3(
            parentWorldScale.x != 0f ? worldScale.x / parentWorldScale.x : worldScale.x,
            parentWorldScale.y != 0f ? worldScale.y / parentWorldScale.y : worldScale.y,
            parentWorldScale.z != 0f ? worldScale.z / parentWorldScale.z : worldScale.z
        );
    }
}
