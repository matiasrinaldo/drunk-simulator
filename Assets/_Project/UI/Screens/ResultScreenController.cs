using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye por codigo la pantalla de resultado (Victory / Defeat) en la escena Result.
/// Lee <see cref="GameResultStore.Result"/> en Start(), aplica colores y textos segun el
/// estado, conecta los botones Reintentar y Salir.
/// No usa DontDestroyOnLoad: vive unicamente en la escena Result.
/// </summary>
public class ResultScreenController : MonoBehaviour
{
    // Referencias a botones para conectar listeners (se asignan en BuildResultUI)
    private Button retryButton;
    private Button quitButton;

    void Start()
    {
        // D-10: liberar cursor y restablecer escala de tiempo al entrar a la escena Result.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

        // Pitfall 4: ocultar el HUD persistente para que no tape la pantalla de resultado.
        HUDController.SetVisible(false);

        // Construir Canvas y conectar botones.
        BuildResultUI();

        retryButton.onClick.AddListener(OnRetry);
        quitButton.onClick.AddListener(OnQuit);
    }

    /// <summary>
    /// Construye la jerarquia Canvas → Overlay → ResultPanel → TitleText / MessageText /
    /// ButtonGroup (RetryButton + QuitButton) por codigo, siguiendo la UI-SPEC aprobada.
    /// </summary>
    private void BuildResultUI()
    {
        GameResult resultado = GameResultStore.Result;
        bool esVictoria = resultado == GameResult.Victory;

        // ── Colores segun estado (UI-SPEC §Color) ──
        Color colorPanel        = esVictoria
            ? new Color(0.102f, 0.180f, 0.102f, 0.92f)   // #1A2E1A
            : new Color(0.180f, 0.102f, 0.102f, 0.92f);  // #2E1A1A

        Color colorTitulo       = esVictoria
            ? new Color(0.298f, 0.686f, 0.314f, 1.0f)    // #4CAF50
            : new Color(0.898f, 0.224f, 0.208f, 1.0f);   // #E53935

        Color colorBotonReintentar = esVictoria
            ? new Color(0.220f, 0.557f, 0.235f, 1.0f)    // #388E3C
            : new Color(0.773f, 0.157f, 0.157f, 1.0f);   // #C62828

        Color colorBotonSalir   = new Color(0.380f, 0.380f, 0.380f, 1.0f); // #616161

        // ── Textos segun estado (UI-SPEC §Copywriting Contract) ──
        string textoTitulo   = esVictoria ? "LLEGASTE A CASA" : "CHOCASTE";
        string textoMensaje  = esVictoria
            ? "Vendiste todo y llegaste sano. Por ahora."
            : "El alcohol gano esta vez. Siempre hay otra.";
        string textoReintentar = esVictoria ? "Jugar de nuevo" : "Reintentar";

        // ── Font con fallback (patron HUDController) ──
        TMP_FontAsset fuente = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
            ?? Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Canvas "ResultCanvas"
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject canvasGO = new GameObject("ResultCanvas", typeof(RectTransform));
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Overlay negro semitransparente (UI-SPEC: #000000 alpha 0.55)
        // Cubre toda la pantalla detras del panel.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject overlayGO = new GameObject("Overlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.sprite = CreateSolidSprite();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ResultPanel — centrado, 640x360 px (UI-SPEC §ResultPanel)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject panelGO = new GameObject("ResultPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 360f);
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = colorPanel;
        panelImg.sprite = CreateSolidSprite();

        // Layout interior (pivot en centro del panel):
        // Panel alto = 360px, padding top/bottom = 48px → area interior vertical = 264px
        // TitleText alto = 80px → anchoredPosition.y = 360/2 - 48 - 80/2 = 132
        // gap md (16px) entre TitleText y MessageText
        // MessageText alto = 60px → anchoredPosition.y = 132 - 80/2 - 16 - 60/2 = 36
        // gap xl (32px) entre MessageText y ButtonGroup
        // ButtonGroup alto = 112px → anchoredPosition.y = 36 - 60/2 - 32 - 112/2 = -88

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // TitleText — 576x80 px, 52px Bold (UI-SPEC §TitleText)
        // Shadow solo en TitleText (UI-SPEC §Typography)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform));
        titleGO.transform.SetParent(panelGO.transform, false);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot     = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 132f);
        titleRect.sizeDelta = new Vector2(576f, 80f);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (fuente != null) titleTMP.font = fuente;
        titleTMP.text      = textoTitulo;
        titleTMP.fontSize  = 52f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = colorTitulo;

        // Sombra solo en TitleText (UI-SPEC §Typography)
        Shadow titleSombra = titleGO.AddComponent<Shadow>();
        titleSombra.effectDistance = new Vector2(1f, -1f);
        titleSombra.effectColor    = Color.black;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // MessageText — 576x60 px, 24px Regular (UI-SPEC §MessageText)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject msgGO = new GameObject("MessageText", typeof(RectTransform));
        msgGO.transform.SetParent(panelGO.transform, false);
        RectTransform msgRect = msgGO.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.5f, 0.5f);
        msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.pivot     = new Vector2(0.5f, 0.5f);
        msgRect.anchoredPosition = new Vector2(0f, 36f);
        msgRect.sizeDelta = new Vector2(576f, 60f);

        TextMeshProUGUI msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
        if (fuente != null) msgTMP.font = fuente;
        msgTMP.text      = textoMensaje;
        msgTMP.fontSize  = 24f;
        msgTMP.fontStyle = FontStyles.Normal;
        msgTMP.alignment = TextAlignmentOptions.Center;
        msgTMP.color     = Color.white;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ButtonGroup — 576x112 px, VerticalLayoutGroup spacing 8px
        // (UI-SPEC §ButtonGroup)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        GameObject groupGO = new GameObject("ButtonGroup", typeof(RectTransform));
        groupGO.transform.SetParent(panelGO.transform, false);
        RectTransform groupRect = groupGO.GetComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0.5f, 0.5f);
        groupRect.anchorMax = new Vector2(0.5f, 0.5f);
        groupRect.pivot     = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = new Vector2(0f, -88f);
        groupRect.sizeDelta = new Vector2(576f, 112f);

        VerticalLayoutGroup vlg = groupGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8f;
        vlg.childForceExpandWidth  = true;
        vlg.childControlWidth      = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = false;

        // ── Boton Reintentar ──
        retryButton = CrearBoton(groupGO.transform, "RetryButton",
            textoReintentar, colorBotonReintentar, fuente);

        // ── Boton Salir ──
        quitButton = CrearBoton(groupGO.transform, "QuitButton",
            "Salir", colorBotonSalir, fuente);
    }

    /// <summary>
    /// Crea un Button hijo del padre dado con imagen de fondo solida, ColorBlock configurado
    /// y texto TMP centrado en blanco. Patron comun a RetryButton y QuitButton.
    /// </summary>
    private Button CrearBoton(Transform padre, string nombre, string texto,
                               Color colorBase, TMP_FontAsset fuente)
    {
        // Contenedor del boton
        GameObject btnGO = new GameObject(nombre, typeof(RectTransform));
        btnGO.transform.SetParent(padre, false);
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(576f, 48f);

        // Imagen de fondo
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color  = colorBase;
        btnImg.sprite = CreateSolidSprite();

        // Componente Button con ColorBlock (UI-SPEC §Estados de botones)
        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = colorBase;
        cb.highlightedColor = Color.Lerp(colorBase, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(colorBase, Color.black, 0.15f);
        cb.fadeDuration     = 0.1f;
        btn.colors          = cb;
        btn.targetGraphic   = btnImg;

        // Texto del boton (TextMeshProUGUI)
        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(btnGO.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        if (fuente != null) labelTMP.font = fuente;
        labelTMP.text      = texto;
        labelTMP.fontSize  = 20f;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color     = Color.white;

        return btn;
    }

    /// <summary>
    /// Crea un sprite blanco 1x1 solido por codigo.
    /// Necesario para que Image.color se renderice correctamente sin un sprite asignado
    /// desde el proyecto (patron identico al de HUDController.CreateSolidSprite).
    /// </summary>
    private static Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Listener del boton Reintentar: resetea todos los stores y carga Home (D-09).</summary>
    private void OnRetry()
    {
        Debug.Log("[ResultScreen] Reintentar presionado — iniciando nueva partida.");
        GameManager.NewGame();
    }

    /// <summary>Listener del boton Salir: cierra la aplicacion (D-08). Log en Editor.</summary>
    private void OnQuit()
    {
        Debug.Log("[ResultScreen] Salir presionado.");
        Application.Quit();
    }
}
