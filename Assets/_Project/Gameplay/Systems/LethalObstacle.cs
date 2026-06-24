using UnityEngine;

/// <summary>
/// Categorias de obstaculos letales. El jugador pierde si choca contra
/// cualquier GameObject con este componente mientras conduce (D-01).
/// </summary>
public enum ObstacleCategory { Casa, Arbol, Nino, Mascota }

/// <summary>
/// Componente marcador que identifica obstaculos letales en la escena.
/// Al agregarlo a un GameObject, el <see cref="CarController"/> detectara
/// la colision y disparara la derrota (GAME-01). Sin logica propia; solo
/// datos serializados. Analogo a <see cref="SpawnPoint"/>.
/// </summary>
public class LethalObstacle : MonoBehaviour
{
    [SerializeField] private ObstacleCategory category = ObstacleCategory.Casa;

    /// <summary>Categoria del obstaculo letal (Casa, Arbol, Nino o Mascota).</summary>
    public ObstacleCategory Category => category;

    /// <summary>Asigna la categoria por codigo (usado por CityBuilder).</summary>
    public void SetCategory(ObstacleCategory c)
    {
        category = c;
    }
}
