using System.Text;
using UnityEngine;

public class CarryableObject : MonoBehaviour
{
    public Color highlightColor = new Color(1f, 0.9f, 0.15f, 1f);

    [Tooltip("Opcional. Si se deja vacio se calcula un ID estable por su ubicacion en la escena.")]
    [SerializeField] string objectId;

    [Header("Definicion")]
    [SerializeField] private SellableDefinition definition;

    /// <summary>Definicion Flyweight de este tipo de objeto (valor de venta, nombre).</summary>
    public SellableDefinition Definition => definition;

    /// <summary>Valor de venta en pesos. Retorna 0 si no hay definicion asignada.</summary>
    public int SellValue => definition != null ? definition.SellValue : 0;

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    bool isHighlighted;

    /// <summary>
    /// Identidad estable entre recargas de escena. Se usa para recordar si este
    /// objeto ya fue entregado (ver <see cref="DeliveredObjectsStore"/>). Si no se
    /// asigna un objectId a mano, se deriva del nombre de la escena + la ruta de
    /// indices de hermanos en la jerarquia (estable mientras no se reordene).
    /// </summary>
    public string StableId
    {
        get
        {
            if (!string.IsNullOrEmpty(objectId)) return objectId;

            StringBuilder sb = new StringBuilder(gameObject.scene.name);
            Transform t = transform;
            string suffix = "";
            while (t != null)
            {
                suffix = "/" + t.GetSiblingIndex() + suffix;
                t = t.parent;
            }
            return sb.Append(suffix).ToString();
        }
    }

    void Awake()
    {
        // Si ya fue tomado en esta partida, no debe reaparecer al recargar la escena.
        if (DeliveredObjectsStore.IsTaken(StableId))
        {
            gameObject.SetActive(false);
            return;
        }

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
        if (renderers == null) return;
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
        // Registrar en el store el objeto sostenido (definicion + id estable).
        HeldObjectStore.SetHeld(definition, StableId);
        // Marcar como entregado YA: el objeto salio del mundo al agarrarlo. Evita que
        // reaparezca al recargar la escena mientras sigue en mano (duplicacion / venta
        // extra). No hay mecanismo de "soltar", asi que marcar al agarrar es seguro:
        // este es el unico punto de verdad del marcado (CR-01).
        DeliveredObjectsStore.MarkTaken(StableId);
        SetHighlighted(false);
        gameObject.SetActive(false);
    }
}
