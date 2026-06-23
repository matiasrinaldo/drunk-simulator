using UnityEngine;

/// <summary>
/// Componente que identifica un item como bebida comprable.
/// Los parametros de precio y alcohol se delegan al DrinkDefinition Flyweight.
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("Definicion")]
    [SerializeField] private DrinkDefinition definition;

    public bool infiniteSupply = true;
    public GameObject heldVisualPrefab;
    public bool overrideHeldVisualScale;
    public Vector3 heldVisualScale = Vector3.one;
    public Color highlightColor = new Color(1f, 0.9f, 0.15f, 1f);

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    bool isHighlighted;

    /// <summary>Acceso publico a la definicion Flyweight de esta bebida.</summary>
    public DrinkDefinition Definition => definition;

    /// <summary>Precio de la bebida en pesos (delegado al SO).</summary>
    public int Price => definition != null ? definition.Price : 0;

    /// <summary>Unidades de alcohol por sorbo (delegado al SO).</summary>
    public int AlcoholPerSip => definition != null ? definition.AlcoholPerSip : 0;

    /// <summary>Cantidad de sorbos de la bebida (delegado al SO).</summary>
    public int MaxSips => definition != null ? definition.MaxSips : 1;

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
