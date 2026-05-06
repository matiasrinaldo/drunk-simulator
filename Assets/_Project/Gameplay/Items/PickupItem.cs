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
    public string itemName = "";
    public bool infiniteSupply = true;
    public int maxSips = 4;
    public int beerAlcoholPerSip = 1;
    public int cocktailAlcoholPerSip = 2;
    public int whiskyAlcoholPerSip = 3;
    public GameObject heldVisualPrefab;
    public bool overrideHeldVisualScale;
    public Vector3 heldVisualScale = Vector3.one;
    public Color highlightColor = new Color(1f, 0.9f, 0.15f, 1f);

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    bool isHighlighted;

    public PickupType ResolvedPickupType
    {
        get
        {
            string lookupName = string.IsNullOrWhiteSpace(itemName) ? gameObject.name : itemName;
            string lowerName = lookupName.ToLowerInvariant();

            if (lowerName.Contains("cerveza")) return PickupType.Cerveza;
            if (lowerName.Contains("whisky")) return PickupType.Whisky;
            if (lowerName.Contains("trago")) return PickupType.Trago;

            return pickupType;
        }
    }

    public int AlcoholPerSip
    {
        get
        {
            switch (ResolvedPickupType)
            {
                case PickupType.Cerveza:
                    return beerAlcoholPerSip;
                case PickupType.Whisky:
                    return whiskyAlcoholPerSip;
                case PickupType.Trago:
                default:
                    return cocktailAlcoholPerSip;
            }
        }
    }

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            foreach (Material material in renderers[i].materials)
            {
                if (material == null) continue;
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;

        Color emission = highlighted ? highlightColor : Color.black;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", emission);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    public void OnPickedUp()
    {
        SetHighlighted(false);

        if (!infiniteSupply)
        {
            Destroy(gameObject);
        }
    }
}
