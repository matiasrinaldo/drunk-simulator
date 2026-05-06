using UnityEngine;

/// <summary>
/// Reproduce musica de fondo global y persiste entre escenas.
/// Carga el clip desde Resources para no depender de una escena puntual.
/// </summary>
public class BackgroundMusicManager : MonoBehaviour
{
    private static readonly string[] BgmResourcePaths =
    {
        "Audio/Music/BackgroundMusic",
        "Audio/BackgroundMusic"
    };

    private static BackgroundMusicManager instance;

    [Header("Playback")]
    [SerializeField] private float volume = 0.35f;
    [SerializeField] private bool loop = true;

    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject managerObject = new GameObject(nameof(BackgroundMusicManager));
        instance = managerObject.AddComponent<BackgroundMusicManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = Mathf.Clamp01(volume);

        AudioClip bgm = LoadBackgroundMusicClip();
        if (bgm == null)
        {
            Debug.LogWarning("[BackgroundMusicManager] No se encontro el clip en Resources/Audio/Music/BackgroundMusic ni Resources/Audio/BackgroundMusic.");
            return;
        }

        audioSource.clip = bgm;
        audioSource.Play();
    }

    private static AudioClip LoadBackgroundMusicClip()
    {
        for (int i = 0; i < BgmResourcePaths.Length; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>(BgmResourcePaths[i]);
            if (clip != null) return clip;
        }

        return null;
    }
}
