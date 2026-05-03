using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Genera el layout inicial de la ciudad en City.unity.
/// Menu: Drunk Simulator → Build City Layout
/// </summary>
public static class CityBuilder
{
    // ── Asset paths ────────────────────────────────────────────────────────
    const string CityKitFBX  = "Assets/ThirdParty/Kenney/CityKit/Models/FBX format/";
    const string MaterialsDir = "Assets/Art/Materials/City";

    // ── Layout ─────────────────────────────────────────────────────────────
    // La calle corre a lo largo del eje Z. Y=0 es el suelo.
    //
    //  Z = +100  ┌──[BAR]──[MERCADO]──┐
    //            │                    │
    //            │    calle (Z axis)  │
    //            │                    │
    //  Z = -100  └────────[CASA]──────┘
    //
    const float RoadHalfLength   = 110f;  // la calle va de Z=-110 a Z=+110
    const float RoadWidth        = 10f;   // 10 unidades → 2 carriles cómodos
    const float BuildingXOffset  = 16f;   // distancia del centro al frente del edificio
    const float BuildingScale    = 1f;    // ajustá si los modelos de Kenney quedan muy grandes

    // Posiciones Z clave
    const float Z_Casa            = -88f;
    const float Z_ParkCasa        = -70f;  // punto de estacionamiento frente a la casa
    const float Z_Mercado         = +68f;
    const float Z_Bar             = +86f;
    const float Z_ParkComercial   = +54f;  // punto de estacionamiento frente a mercado/bar

    // ── Entry point ────────────────────────────────────────────────────────
    [MenuItem("Drunk Simulator/Build City Layout")]
    static void Build()
    {
        var existing = GameObject.Find("City");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "City Builder",
                "Ya existe un objeto 'City' en la escena. ¿Reemplazarlo?",
                "Sí, reemplazar", "Cancelar");
            if (!replace) return;
            Object.DestroyImmediate(existing);
        }

        EnsureMaterialsFolder();

        var city = new GameObject("City");
        BuildGround(city.transform);
        BuildRoad(city.transform);
        BuildBuildings(city.transform);
        BuildVegetation(city.transform);
        BuildWaypoints(city.transform);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = city;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("[CityBuilder] Ciudad generada. Guardá la escena con Ctrl+S. " +
                  "Si los edificios quedan muy grandes/chicos, ajustá BuildingScale en CityBuilder.cs y volvé a correr.");
    }

    // ── Ground ─────────────────────────────────────────────────────────────
    static void BuildGround(Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Ground";
        go.transform.SetParent(parent);
        go.transform.localScale = new Vector3(30f, 1f, 30f); // 300x300
        go.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMat("M_Ground", new Color32(72, 110, 42, 255));
    }

    // ── Road ───────────────────────────────────────────────────────────────
    static void BuildRoad(Transform parent)
    {
        var root = new GameObject("Road");
        root.transform.SetParent(parent);

        float planeUnit = 10f; // Unity Plane = 10x10 unidades

        // Asfalto
        var asphalt = GameObject.CreatePrimitive(PrimitiveType.Plane);
        asphalt.name = "Asphalt";
        asphalt.transform.SetParent(root.transform);
        asphalt.transform.position = new Vector3(0f, 0.01f, 0f);
        asphalt.transform.localScale = new Vector3(
            RoadWidth / planeUnit,
            1f,
            RoadHalfLength * 2f / planeUnit);
        asphalt.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMat("M_Asphalt", new Color32(40, 40, 40, 255));

        // Línea central (sin collider, solo visual)
        var line = GameObject.CreatePrimitive(PrimitiveType.Plane);
        line.name = "CenterLine";
        line.transform.SetParent(root.transform);
        line.transform.position = new Vector3(0f, 0.02f, 0f);
        line.transform.localScale = new Vector3(
            0.04f,
            1f,
            RoadHalfLength * 2f / planeUnit);
        Object.DestroyImmediate(line.GetComponent<Collider>());
        line.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMat("M_RoadLine", new Color32(255, 215, 0, 255));

        // Aceras (sidewalks) — planos grises entre la calle y los edificios
        BuildSidewalk(root.transform, -1, planeUnit);
        BuildSidewalk(root.transform, +1, planeUnit);
    }

    static void BuildSidewalk(Transform parent, int side, float planeUnit)
    {
        float xCenter = side * (RoadWidth / 2f + (BuildingXOffset - RoadWidth / 2f) / 2f);
        float width   = BuildingXOffset - RoadWidth / 2f; // espacio entre calle y edificios

        var sw = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sw.name = side < 0 ? "Sidewalk_West" : "Sidewalk_East";
        sw.transform.SetParent(parent);
        sw.transform.position = new Vector3(xCenter, 0.005f, 0f);
        sw.transform.localScale = new Vector3(
            width / planeUnit,
            1f,
            RoadHalfLength * 2f / planeUnit);
        Object.DestroyImmediate(sw.GetComponent<Collider>());
        sw.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMat("M_Sidewalk", new Color32(160, 160, 160, 255));
    }

    // ── Buildings ──────────────────────────────────────────────────────────
    static void BuildBuildings(Transform parent)
    {
        var root = new GameObject("Buildings");
        root.transform.SetParent(parent);

        // ── Zona sur: Casa del jugador (lado oeste) ──────────────────────
        PlaceBuilding(root.transform, "Casa_Jugador",
            "building-type-a",
            new Vector3(-BuildingXOffset, 0f, Z_Casa),
            90f);

        // ── Zona norte: Mercado + Bar (lado este) ────────────────────────
        PlaceBuilding(root.transform, "Mercado",
            "building-type-e",
            new Vector3(BuildingXOffset, 0f, Z_Mercado),
            270f);

        PlaceBuilding(root.transform, "Bar",
            "building-type-c",
            new Vector3(BuildingXOffset, 0f, Z_Bar),
            270f);

        // ── Relleno lado oeste ───────────────────────────────────────────
        PlaceBuilding(root.transform, "Edificio_W1", "building-type-d",
            new Vector3(-BuildingXOffset, 0f, -40f), 90f);
        PlaceBuilding(root.transform, "Edificio_W2", "building-type-g",
            new Vector3(-BuildingXOffset, 0f, +10f), 90f);
        PlaceBuilding(root.transform, "Edificio_W3", "building-type-h",
            new Vector3(-BuildingXOffset, 0f, +50f), 90f);

        // ── Relleno lado este ────────────────────────────────────────────
        PlaceBuilding(root.transform, "Edificio_E1", "building-type-f",
            new Vector3(BuildingXOffset, 0f, -60f), 270f);
        PlaceBuilding(root.transform, "Edificio_E2", "building-type-b",
            new Vector3(BuildingXOffset, 0f, -20f), 270f);
        PlaceBuilding(root.transform, "Edificio_E3", "building-type-i",
            new Vector3(BuildingXOffset, 0f, +30f), 270f);
    }

    static void PlaceBuilding(Transform parent, string goName, string fbxName,
                              Vector3 pos, float yRot)
    {
        string path   = $"{CityKitFBX}{fbxName}.fbx";
        var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(8f, 12f, 8f);
            Debug.LogWarning($"[CityBuilder] FBX no encontrado: '{path}'. Usando cubo placeholder.");
        }

        go.name                   = goName;
        go.transform.position     = pos;
        go.transform.rotation     = Quaternion.Euler(0f, yRot, 0f);
        go.transform.localScale   = Vector3.one * BuildingScale;
    }

    // ── Vegetation ─────────────────────────────────────────────────────────
    static void BuildVegetation(Transform parent)
    {
        var root = new GameObject("Vegetation");
        root.transform.SetParent(parent);

        // Posiciones Z donde queremos árboles en ambos lados de la calle
        float[] treeZs = { -95f, -75f, -55f, -35f, -10f, +15f, +38f, +60f, +80f, +100f };
        bool useLarge = true;

        foreach (float z in treeZs)
        {
            string fbx = useLarge ? "tree-large" : "tree-small";
            PlaceTree(root.transform, fbx, -1, z); // lado oeste
            PlaceTree(root.transform, fbx, +1, z); // lado este
            useLarge = !useLarge;
        }
    }

    static void PlaceTree(Transform parent, string fbxName, int side, float z)
    {
        string path   = $"{CityKitFBX}{fbxName}.fbx";
        var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        float  xPos   = side * (RoadWidth / 2f + 2.5f); // justo al borde de la acera

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(1f, 3f, 1f);
        }

        go.name               = $"Tree_{(side < 0 ? "W" : "E")}_{Mathf.RoundToInt(z)}";
        go.transform.position = new Vector3(xPos, 0f, z);
    }

    // ── Waypoints ──────────────────────────────────────────────────────────
    // GameObjects vacíos que marcan posiciones clave. Se usan en Fases 2–4.
    static void BuildWaypoints(Transform parent)
    {
        var root = new GameObject("Waypoints");
        root.transform.SetParent(parent);

        // Puntos de estacionamiento (Fase 2)
        MakeWaypoint(root.transform, "ParkingSpot_Casa",
            new Vector3(-(RoadWidth / 2f + 1f), 0f, Z_ParkCasa));
        MakeWaypoint(root.transform, "ParkingSpot_Comercial",
            new Vector3(-(RoadWidth / 2f + 1f), 0f, Z_ParkComercial));

        // Triggers de interacción (Fases 3–4)
        MakeWaypoint(root.transform, "Trigger_HouseDoor",
            new Vector3(-RoadWidth / 2f, 0f, Z_Casa));
        MakeWaypoint(root.transform, "Trigger_Mercado",
            new Vector3(+RoadWidth / 2f, 0f, Z_Mercado));
        MakeWaypoint(root.transform, "Trigger_Bar",
            new Vector3(+RoadWidth / 2f, 0f, Z_Bar));
    }

    static void MakeWaypoint(Transform parent, string goName, Vector3 pos)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials/City"))
            AssetDatabase.CreateFolder("Assets/Art/Materials", "City");
    }

    static Material GetOrCreateMat(string matName, Color color)
    {
        string path = $"{MaterialsDir}/{matName}.mat";
        var    mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat       = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
