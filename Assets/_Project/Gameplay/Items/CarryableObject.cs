using UnityEngine;

public class CarryableObject : MonoBehaviour
{
    public Color highlightColor = new Color(1f, 0.9f, 0.15f, 1f);

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    Color[] originalColors;
    bool isHighlighted;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        propertyBlock = new MaterialPropertyBlock();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = GetRendererColor(renderers[i]);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Color color = highlighted ? highlightColor : originalColors[i];
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    public void OnPickedUp()
    {
        SetHighlighted(false);
        gameObject.SetActive(false);
    }

    Color GetRendererColor(Renderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
        {
            return Color.white;
        }

        Material material = targetRenderer.sharedMaterial;
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");

        return Color.white;
    }
}
