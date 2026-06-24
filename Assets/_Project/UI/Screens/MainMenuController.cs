using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Texture2D backgroundImage;
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private Rect playButtonNormalizedRect = new Rect(0.3f, 0.54f, 0.4f, 0.18f);

    private void Start()
    {
        HUDController.SetVisible(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnGUI()
    {
        if (backgroundImage != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backgroundImage, ScaleMode.ScaleAndCrop);
        }

        Rect playButtonRect = new Rect(
            Screen.width * playButtonNormalizedRect.x,
            Screen.height * playButtonNormalizedRect.y,
            Screen.width * playButtonNormalizedRect.width,
            Screen.height * playButtonNormalizedRect.height);

        if (GUI.Button(playButtonRect, GUIContent.none, GUIStyle.none))
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        HUDController.SetVisible(true);
        CarStateStore.Clear();
        DeliveredObjectsStore.Clear();
        DrunkLevelStore.Clear();
        PlayerMoneyStore.Clear();
        HeldObjectStore.Clear();
        HomeObjectsTotalStore.Clear();
        GameResultStore.Clear();
        WorldTimeStore.Clear();
        PlayerSpawner.NextSpawnId = null;
        SceneManager.LoadSceneAsync(homeSceneName);
    }
}
