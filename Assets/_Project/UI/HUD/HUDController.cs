using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD persistente del juego (D-01). Singleton que sobrevive cargas de escena,
/// auto-arrancado antes de la primera escena (patron BackgroundMusicManager).
/// Construye un unico Canvas Screen Space Overlay por codigo: barra de borrachera
/// (lerp hacia DrunkManager.EffectIntensity) e indicador de dinero TMP suscripto
/// al evento de cambio de saldo de PlayerMoneyStore. Se re-vincula al DrunkManager
/// activo en cada carga de escena.
/// </summary>
public class HUDController : MonoBehaviour
{
    private static HUDController instance;

    private DrunkManager drunkManager;   // DrunkManager de la escena activa
    private float targetFillAmount;      // valor objetivo del lerp de la barra (0->1)
    private Image fillImage;             // fill de la barra (Image.type = Filled)
    private TMP_Text moneyText;          // texto de dinero (TextMeshProUGUI)

    /// <summary>Crea el HUD una sola vez antes de cargar la primera escena (D-01).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject hudObject = new GameObject(nameof(HUDController));
        instance = hudObject.AddComponent<HUDController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildHUD();

        // Dinero: suscribir y leer estado inicial (D-03).
        PlayerMoneyStore.OnMoneyChanged += HandleMoneyChanged;
        HandleMoneyChanged(PlayerMoneyStore.Money);

        // Re-vinculo al DrunkManager por cada carga de escena (D-02).
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Construye la jerarquia del Canvas por codigo (D-06, D-07, UI-SPEC).</summary>
    private void BuildHUD()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Grupo en esquina inferior izquierda.
        GameObject group = new GameObject("HUDGroup", typeof(RectTransform));
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.SetParent(transform, false);
        groupRect.anchorMin = Vector2.zero;
        groupRect.anchorMax = Vector2.zero;
        groupRect.pivot = Vector2.zero;
        groupRect.anchoredPosition = new Vector2(24f, 24f);

        // Texto de dinero (arriba del grupo).
        GameObject moneyObject = new GameObject("MoneyText", typeof(RectTransform));
        RectTransform moneyRect = moneyObject.GetComponent<RectTransform>();
        moneyRect.SetParent(groupRect, false);
        moneyRect.anchorMin = new Vector2(0f, 1f);
        moneyRect.anchorMax = new Vector2(0f, 1f);
        moneyRect.pivot = new Vector2(0f, 1f);
        moneyRect.anchoredPosition = Vector2.zero;
        moneyRect.sizeDelta = new Vector2(220f, 35f);

        moneyText = moneyObject.AddComponent<TextMeshProUGUI>();
        moneyText.text = "$0";
        moneyText.fontSize = 28f;
        moneyText.fontStyle = FontStyles.Bold;
        moneyText.color = Color.white;
        moneyText.alignment = TextAlignmentOptions.Left;

        Shadow moneyShadow = moneyObject.AddComponent<Shadow>();
        moneyShadow.effectDistance = new Vector2(1f, -1f);
        moneyShadow.effectColor = Color.black;

        // Barra de borrachera (debajo del texto, gap sm = 8px).
        GameObject barObject = new GameObject("DrunkBar", typeof(RectTransform));
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.SetParent(groupRect, false);
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = Vector2.zero;
        barRect.pivot = Vector2.zero;
        barRect.anchoredPosition = new Vector2(0f, -43f);
        barRect.sizeDelta = new Vector2(220f, 20f);

        // Track (fondo de la barra).
        GameObject trackObject = new GameObject("Track", typeof(RectTransform));
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.SetParent(barRect, false);
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        Image trackImage = trackObject.AddComponent<Image>();
        trackImage.color = new Color(0f, 0f, 0f, 0.45f);

        // Fill (relleno ambar #E0B040, empieza vacio).
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(trackRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObject.AddComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        fillImage.color = new Color(0.878f, 0.690f, 0.251f, 1f);
    }

    /// <summary>Re-vincula la barra al DrunkManager de la escena recien cargada (D-02).</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (drunkManager != null)
            drunkManager.OnAlcoholLevelChanged -= HandleAlcoholChanged;

        drunkManager = FindFirstObjectByType<DrunkManager>();

        if (drunkManager != null)
        {
            drunkManager.OnAlcoholLevelChanged += HandleAlcoholChanged;
            targetFillAmount = drunkManager.EffectIntensity;
        }
        // Si no hay DrunkManager, se mantiene targetFillAmount actual sin lanzar excepcion.

        Debug.Log($"[HUDController] Escena cargada: {scene.name}. DrunkManager {(drunkManager != null ? "encontrado" : "no encontrado")}.");
    }

    /// <summary>Actualiza el objetivo de la barra cuando cambia el nivel de alcohol (D-04).</summary>
    private void HandleAlcoholChanged(int newLevel)
    {
        if (drunkManager == null) return;
        targetFillAmount = drunkManager.EffectIntensity;
    }

    /// <summary>Actualiza el texto de dinero al instante cuando cambia el saldo (D-03).</summary>
    private void HandleMoneyChanged(int newAmount)
    {
        if (moneyText == null) return;
        moneyText.text = $"${newAmount}";
    }

    private void Update()
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, 3f * Time.deltaTime);
    }

    private void OnDestroy()
    {
        PlayerMoneyStore.OnMoneyChanged -= HandleMoneyChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (drunkManager != null)
            drunkManager.OnAlcoholLevelChanged -= HandleAlcoholChanged;
    }
}
