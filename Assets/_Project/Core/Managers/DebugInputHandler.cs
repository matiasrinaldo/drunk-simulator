using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Atajo de teclado para pruebas en editor. Solo activo en builds de desarrollo.
/// P → dispara Victoria  (GameResult.Victory  + carga escena Result).
/// O → dispara Derrota   (GameResult.Defeat   + carga escena Result).
/// Adjuntar este componente al mismo GameObject que GameManager en la escena City.
/// </summary>
public class DebugInputHandler : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[DebugInputHandler] Tecla P — forzando VICTORIA.");
            GameResultStore.Set(GameResult.Victory);
            SceneManager.LoadSceneAsync("Result");
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("[DebugInputHandler] Tecla O — forzando DERROTA.");
            GameResultStore.Set(GameResult.Defeat);
            SceneManager.LoadSceneAsync("Result");
        }
    }
#endif
}
