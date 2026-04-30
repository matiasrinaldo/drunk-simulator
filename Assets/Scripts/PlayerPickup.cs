using System;
using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float selectionDistance = 5f;
    public LayerMask pickupLayerMask = Physics.DefaultRaycastLayers;
    public Transform holdPoint;
    public PickupItem currentPickupItem;

    public event Action<PickupItem> OnItemPickedUp;

    Camera mainCamera;
    GameObject currentHeldVisual;
    PickupItem lastHighlightedItem;

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
    }

    void Update()
    {
        UpdateSelectionByLook();

        if (currentPickupItem != null && Input.GetKeyDown(pickupKey))
        {
            Pickup(currentPickupItem);
        }
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

    Vector3 GetLocalScaleForWorldScale(Vector3 worldScale, Vector3 parentWorldScale)
    {
        return new Vector3(
            parentWorldScale.x != 0f ? worldScale.x / parentWorldScale.x : worldScale.x,
            parentWorldScale.y != 0f ? worldScale.y / parentWorldScale.y : worldScale.y,
            parentWorldScale.z != 0f ? worldScale.z / parentWorldScale.z : worldScale.z
        );
    }
}

