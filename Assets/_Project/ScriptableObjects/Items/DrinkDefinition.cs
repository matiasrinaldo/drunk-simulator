using UnityEngine;

/// <summary>
/// Definicion Flyweight de un tipo de bebida.
/// Cada tipo de bebida (Cerveza, Trago, Whisky) tiene un unico asset de este tipo;
/// todas las instancias del mismo tipo apuntan al mismo asset.
/// El precio y los parametros de alcohol son configurables desde el Inspector (D-06, D-09).
/// </summary>
[CreateAssetMenu(fileName = "DrinkDefinition", menuName = "Drunk Simulator/Drink Definition")]
public class DrinkDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField] private string drinkName = "Bebida";

    [Header("Economia")]
    [Tooltip("Precio en pesos para comprar esta bebida")]
    [SerializeField] private int price = 10;

    [Header("Alcohol")]
    [Tooltip("Unidades de alcohol agregadas por sorbo")]
    [SerializeField] private int alcoholPerSip = 1;
    [Tooltip("Cuantos sorbos tiene la bebida")]
    [SerializeField] private int maxSips = 4;

    /// <summary>Nombre legible de la bebida.</summary>
    public string DrinkName => drinkName;

    /// <summary>Cuanto dinero cuesta comprar esta bebida.</summary>
    public int Price => price;

    /// <summary>Unidades de alcohol que se agregan por cada sorbo.</summary>
    public int AlcoholPerSip => alcoholPerSip;

    /// <summary>Cantidad de sorbos que tiene la bebida.</summary>
    public int MaxSips => maxSips;
}
