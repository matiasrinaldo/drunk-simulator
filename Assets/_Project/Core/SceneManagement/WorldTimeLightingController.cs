using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldTimeLightingController
{
    private static readonly Color DayAmbientSky = new Color(0.212f, 0.227f, 0.259f, 1f);
    private static readonly Color DayAmbientEquator = new Color(0.114f, 0.125f, 0.133f, 1f);
    private static readonly Color DayAmbientGround = new Color(0.047f, 0.043f, 0.035f, 1f);
    private static readonly Color DaySunColor = new Color(1f, 0.956f, 0.839f, 1f);

    private static readonly Color NightAmbientSky = new Color(0.015f, 0.02f, 0.055f, 1f);
    private static readonly Color NightAmbientEquator = new Color(0.01f, 0.012f, 0.025f, 1f);
    private static readonly Color NightAmbientGround = new Color(0.004f, 0.004f, 0.01f, 1f);
    private static readonly Color NightSunColor = new Color(0.25f, 0.35f, 0.75f, 1f);

    private static Material daySkybox;
    private static Material nightSkybox;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private static void ApplyToScene(Scene scene)
    {
        if (scene.name != "City") return;

        bool isNight = WorldTimeStore.CurrentTimeOfDay == WorldTimeOfDay.Night;
        if (daySkybox == null)
        {
            daySkybox = RenderSettings.skybox;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = isNight ? NightAmbientSky : DayAmbientSky;
        RenderSettings.ambientEquatorColor = isNight ? NightAmbientEquator : DayAmbientEquator;
        RenderSettings.ambientGroundColor = isNight ? NightAmbientGround : DayAmbientGround;
        RenderSettings.ambientIntensity = isNight ? 0.25f : 1f;
        RenderSettings.skybox = isNight ? GetNightSkybox() : daySkybox;
        DynamicGI.UpdateEnvironment();

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Light sceneLight in lights)
        {
            if (sceneLight.type != LightType.Directional) continue;

            sceneLight.color = isNight ? NightSunColor : DaySunColor;
            sceneLight.intensity = isNight ? 0.08f : 1f;
            sceneLight.transform.rotation = Quaternion.Euler(isNight ? 8f : 50f, -30f, 0f);
        }

        Debug.Log($"[WorldTimeLightingController] City cargada en {(isNight ? "noche" : "dia")}.");
    }

    private static Material GetNightSkybox()
    {
        if (daySkybox == null)
        {
            daySkybox = RenderSettings.skybox;
        }

        if (nightSkybox != null) return nightSkybox;

        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        nightSkybox = new Material(shader)
        {
            name = "Runtime Night Skybox"
        };

        SetColorIfPresent(nightSkybox, "_SkyTint", new Color(0.01f, 0.018f, 0.055f, 1f));
        SetColorIfPresent(nightSkybox, "_GroundColor", new Color(0.003f, 0.004f, 0.01f, 1f));
        SetColorIfPresent(nightSkybox, "_BaseColor", new Color(0.01f, 0.018f, 0.055f, 1f));
        SetColorIfPresent(nightSkybox, "_Color", new Color(0.01f, 0.018f, 0.055f, 1f));
        SetFloatIfPresent(nightSkybox, "_Exposure", 0.12f);
        SetFloatIfPresent(nightSkybox, "_AtmosphereThickness", 0.25f);
        SetFloatIfPresent(nightSkybox, "_SunSize", 0.01f);

        return nightSkybox;
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
