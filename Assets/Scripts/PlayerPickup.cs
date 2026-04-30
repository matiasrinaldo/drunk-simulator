using System;
using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float selectionDistance = 5f;
    public LayerMask pickupLayerMask = Physics.DefaultRaycastLayers;
    public Transform holdPoint;
    public PickupItem currentPickupItem;

    [Header("Aim Dot")]
    public bool showAimDot = true;
    public float aimDotSize = 4f;
    public Color aimDotColor = Color.white;

    [Header("Drinking")]
    public DrunkManager drunkManager;
    public float beerAlcoholAmount = 8f;
    public float cocktailAlcoholAmount = 15f;
    public float whiskyAlcoholAmount = 30f;
    public int maxSipsPerDrink = 4;

    [Header("Throwing")]
    public int throwMouseButton = 1;
    public float minThrowForce = 4f;
    public float maxThrowForce = 16f;
    public float maxThrowChargeTime = 1.5f;
    public float upwardThrowForce = 0f;

    public event Action<PickupItem> OnItemPickedUp;

    Camera mainCamera;
    GameObject currentHeldVisual;
    PickupItem lastHighlightedItem;
    bool hasHeldDrink;
    PickupType heldDrinkType;
    int heldDrinkSips;
    bool isChargingThrow;
    float throwChargeStartTime;

    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerPickup: no main camera found. Using player transform for hold point.");
        }

        if (holdPoint == null)
        {
            var parent = mainCamera != null ? mainCamera.transform : transform;
            var holdPointGameObject = new GameObject("HoldPoint");
            holdPointGameObject.transform.SetParent(parent);
            holdPointGameObject.transform.localPosition = new Vector3(0.5f, -0.15f, 0.5f);
            holdPointGameObject.transform.localRotation = Quaternion.Euler(10f, -20f, 0f);
            holdPointGameObject.transform.localScale = Vector3.one;
            holdPoint = holdPointGameObject.transform;
        }
        else
        {
            holdPoint.localScale = Vector3.one;
        }

        if (drunkManager == null)
        {
            drunkManager = GetComponent<DrunkManager>();
        }
    }

    void Update()
    {
        UpdateSelectionByLook();
        UpdateThrowInput();

        if (Input.GetKeyDown(pickupKey))
        {
            if (hasHeldDrink)
            {
                DrinkHeldItem();
            }
            else if (currentPickupItem != null)
            {
                Pickup(currentPickupItem);
            }
        }
    }

    void UpdateThrowInput()
    {
        if (hasHeldDrink && Input.GetMouseButtonDown(throwMouseButton))
        {
            isChargingThrow = true;
            throwChargeStartTime = Time.time;
        }

        if (isChargingThrow && Input.GetMouseButtonUp(throwMouseButton))
        {
            ThrowHeldDrink();
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

        if (mainCamera == null)
        {
            return;
        }

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, selectionDistance, pickupLayerMask))
        {
            var pickupItem = hit.collider.GetComponentInParent<PickupItem>();
            if (pickupItem != null)
            {
                currentPickupItem = pickupItem;
            }
        }

        if (lastHighlightedItem != null && lastHighlightedItem != currentPickupItem)
        {
            lastHighlightedItem.SetHighlighted(false);
            lastHighlightedItem = null;
        }

        if (currentPickupItem != null && lastHighlightedItem != currentPickupItem)
        {
            lastHighlightedItem = currentPickupItem;
            currentPickupItem.SetHighlighted(true);
        }
    }

    void Pickup(PickupItem item)
    {
        if (item == null) return;

        item.SetHighlighted(false);
        Destroy(currentHeldVisual);
        currentHeldVisual = CreateHeldVisual(item);
        heldDrinkType = item.pickupType;
        heldDrinkSips = 0;
        hasHeldDrink = true;

        item.OnPickedUp();
        OnItemPickedUp?.Invoke(item);

        currentPickupItem = null;
        lastHighlightedItem = null;
    }

    GameObject CreateHeldVisual(PickupItem item)
    {
        if (item == null || holdPoint == null) return null;

        GameObject source = item.heldVisualPrefab != null ? item.heldVisualPrefab : item.gameObject;
        Vector3 originalWorldScale = item.transform.lossyScale;
        GameObject clone = Instantiate(source);
        clone.transform.SetParent(holdPoint, false);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = item.overrideHeldVisualScale
            ? item.heldVisualScale
            : GetLocalScaleForWorldScale(originalWorldScale, holdPoint.lossyScale);

        foreach (var col in clone.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        foreach (var pickupComponent in clone.GetComponentsInChildren<PickupItem>())
        {
            Destroy(pickupComponent);
        }

        foreach (var audioSource in clone.GetComponentsInChildren<AudioSource>())
        {
            Destroy(audioSource);
        }

        return clone;
    }

    void DrinkHeldItem()
    {
        if (drunkManager == null) return;

        drunkManager.AddAlcohol(GetAlcoholAmount(heldDrinkType));
        heldDrinkSips++;

        if (heldDrinkSips >= maxSipsPerDrink)
        {
            ClearHeldDrink(true);
        }
    }

    void ThrowHeldDrink()
    {
        if (!hasHeldDrink || currentHeldVisual == null)
        {
            ClearHeldDrink(false);
            return;
        }

        float chargeDuration = Time.time - throwChargeStartTime;
        float charge = maxThrowChargeTime <= 0f ? 1f : Mathf.Clamp01(chargeDuration / maxThrowChargeTime);
        float throwForce = Mathf.Clamp(Mathf.Lerp(minThrowForce, maxThrowForce, charge), minThrowForce, maxThrowForce);
        Vector3 throwDirection = GetThrowDirection();

        GameObject thrownObject = currentHeldVisual;
        thrownObject.transform.SetParent(null, true);
        AddThrowablePhysics(thrownObject);

        var rigidbody = thrownObject.GetComponent<Rigidbody>();
        rigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        ClearHeldDrink(false);
    }

    Vector3 GetThrowDirection()
    {
        Vector3 forward = mainCamera != null ? mainCamera.transform.forward : transform.forward;
        return (forward + Vector3.up * upwardThrowForce).normalized;
    }

    void AddThrowablePhysics(GameObject thrownObject)
    {
        if (thrownObject.GetComponentInChildren<Collider>() == null)
        {
            AddColliderFromRenderers(thrownObject);
        }

        var rigidbody = thrownObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = thrownObject.AddComponent<Rigidbody>();
        }

        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void AddColliderFromRenderers(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            target.AddComponent<BoxCollider>();
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var boxCollider = target.AddComponent<BoxCollider>();
        boxCollider.center = target.transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            bounds.size.x / Mathf.Max(Mathf.Abs(target.transform.lossyScale.x), 0.0001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(target.transform.lossyScale.y), 0.0001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(target.transform.lossyScale.z), 0.0001f)
        );
    }

    void ClearHeldDrink(bool destroyVisual)
    {
        if (destroyVisual)
        {
            Destroy(currentHeldVisual);
        }

        currentHeldVisual = null;
        hasHeldDrink = false;
        heldDrinkSips = 0;
        isChargingThrow = false;
    }

    float GetAlcoholAmount(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.Cerveza:
                return beerAlcoholAmount;
            case PickupType.Whisky:
                return whiskyAlcoholAmount;
            case PickupType.Trago:
            default:
                return cocktailAlcoholAmount;
        }
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

