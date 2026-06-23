/// <summary>
/// Recuerda si el jugador lleva un objeto en mano y cual es su definicion,
/// de manera que la informacion persista al cambiar de escena.
/// Reemplaza el campo estatico hasHeldObject de PlayerPickup (D-03, D-02).
/// SellableDefinition es un asset ScriptableObject, por lo que sobrevive a
/// SceneManager.LoadSceneAsync(Single) sin necesidad de DontDestroyOnLoad.
/// </summary>
public static class HeldObjectStore
{
    /// <summary>True si el jugador tiene un objeto en mano.</summary>
    public static bool HasHeldObject { get; private set; }

    /// <summary>Definicion del objeto sostenido (tipo, valor de venta).</summary>
    public static SellableDefinition HeldDefinition { get; private set; }

    /// <summary>ID estable del objeto sostenido (para marcarlo en DeliveredObjectsStore al vender).</summary>
    public static string HeldObjectId { get; private set; }

    /// <summary>Registra que el jugador agarro un objeto.</summary>
    public static void SetHeld(SellableDefinition definition, string stableId)
    {
        if (definition == null) return;     // guard clause — analogo a CarStateStore.Save null check
        HeldDefinition = definition;
        HeldObjectId = stableId;
        HasHeldObject = true;
    }

    /// <summary>Libera el objeto sostenido (vendido o descartado).</summary>
    public static void Clear()
    {
        HeldDefinition = null;
        HeldObjectId = null;
        HasHeldObject = false;
    }
}
