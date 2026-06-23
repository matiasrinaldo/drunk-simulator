using UnityEngine;

/// <summary>
/// Definicion Flyweight de un tipo de objeto vendible.
/// Cada tipo de objeto (TV, Lampara, Cuadro) tiene un unico asset de este tipo;
/// todas las instancias del mismo tipo apuntan al mismo asset.
/// El valor de venta es configurable desde el Inspector (D-06, D-09).
/// </summary>
[CreateAssetMenu(fileName = "SellableDefinition", menuName = "Drunk Simulator/Sellable Definition")]
public class SellableDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField] private string itemName = "Objeto";

    [Header("Economia")]
    [Tooltip("Valor en pesos al vender este tipo de objeto")]
    [SerializeField] private int sellValue = 20;

    /// <summary>Nombre legible del tipo de objeto.</summary>
    public string ItemName => itemName;

    /// <summary>Cuanto dinero recibe el jugador al vender este objeto.</summary>
    public int SellValue => sellValue;
}
