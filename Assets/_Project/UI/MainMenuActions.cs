using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuActions : MonoBehaviour
{
    [SerializeField] string homeSceneName = "Home";

    public void StartGame()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    public void Play()
    {
        StartGame();
    }

    public void ExitGame()
    {
        Quit();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
