using UnityEngine;

public class CarryableObject : MonoBehaviour
{
    public Color highlightColor = new Color(1f, 0.9f, 0.15f, 1f);

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    bool isHighlighted;

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
        gameObject.SetActive(false);
    }
}
