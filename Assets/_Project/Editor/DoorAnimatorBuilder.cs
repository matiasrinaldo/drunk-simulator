using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera los assets del Animator de puerta: Door.controller y DoorOpen.anim.
/// Menu: Drunk Simulator → Build Door Animator
///
/// Idempotente: si los assets ya existen en la ruta esperada, no los duplica.
/// CRITICO: este archivo usa UnityEditor.Animations y AnimationClip.SetCurve
/// (ambos Editor-only) => DEBE vivir bajo Assets/_Project/Editor/ y NUNCA
/// ser referenciado desde codigo runtime.
/// </summary>
public static class DoorAnimatorBuilder
{
    // ── Rutas de los assets generados ─────────────────────────────────────
    const string AnimFolder      = "Assets/_Project/Animations";
    const string ControllerPath  = "Assets/_Project/Animations/Door.controller";
    const string ClipPath        = "Assets/_Project/Animations/DoorOpen.anim";

    // ── Parametros de la animacion ─────────────────────────────────────────
    const string ParamOpen       = "Open";           // nombre del parametro Trigger
    const float  RotacionFinal   = 95f;             // grados Y de apertura de la bisagra
    const float  DuracionClip    = 0.5f;            // duracion del clip en segundos

    [MenuItem("Drunk Simulator/Build Door Animator")]
    static void Build()
    {
        // 1. Guardia de carpeta: crear Assets/_Project/Animations si no existe
        if (!AssetDatabase.IsValidFolder(AnimFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Animations");
            Debug.Log("[DoorAnimatorBuilder] Carpeta creada: " + AnimFolder);
        }

        // 2. Crear o reutilizar el clip DoorOpen.anim
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = CrearClipApertura();
            AssetDatabase.CreateAsset(clip, ClipPath);
            Debug.Log("[DoorAnimatorBuilder] Clip creado: " + ClipPath);
        }
        else
        {
            Debug.Log("[DoorAnimatorBuilder] Clip ya existia, reutilizando: " + ClipPath);
        }

        // 3. Crear o reutilizar el AnimatorController Door.controller
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = CrearController(clip);
            Debug.Log("[DoorAnimatorBuilder] Controller creado: " + ControllerPath);
        }
        else
        {
            Debug.Log("[DoorAnimatorBuilder] Controller ya existia, reutilizando: " + ControllerPath);
        }

        // 4. Guardar y refrescar el proyecto
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DoorAnimatorBuilder] Listo. Assets en: " + AnimFolder);
        EditorUtility.DisplayDialog(
            "Build Door Animator",
            "Assets generados correctamente:\n  " + ControllerPath + "\n  " + ClipPath,
            "OK");
    }

    /// <summary>
    /// Construye el AnimationClip que rota localEulerAngles.y de 0 a ~95 grados
    /// en ~0.5 segundos con ease-out (tangentes suavizadas).
    /// </summary>
    static AnimationClip CrearClipApertura()
    {
        var clip = new AnimationClip();
        clip.name = "DoorOpen";

        // Curva de rotacion sobre el eje Y de la bisagra: 0 grados -> 95 grados en 0.5s
        // con ease-out (primer keyframe tangente saliente suavizada, segundo plana)
        var curva = new AnimationCurve(
            new Keyframe(0f,        0f),
            new Keyframe(DuracionClip, RotacionFinal)
        );
        // SmoothTangents para ease-out natural en ambos keyframes
        curva.SmoothTangents(0, 0f);
        curva.SmoothTangents(1, 0f);

        // Asignar la curva a localEulerAngles.y del Transform raiz del GameObject de la puerta.
        // La ruta "" apunta al GameObject que tiene el Animator (la bisagra/raiz de la puerta).
        clip.SetCurve("", typeof(Transform), "localEulerAngles.y", curva);

        return clip;
    }

    /// <summary>
    /// Construye el AnimatorController con:
    ///   - Parametro Open (Trigger)
    ///   - Estado Closed (default, sin motion)
    ///   - Estado Open (motion = clip DoorOpen)
    ///   - Transicion Closed->Open con condicion If Open, hasExitTime=false
    /// </summary>
    static AnimatorController CrearController(AnimationClip clipApertura)
    {
        // Crear el controller en disco
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Agregar el parametro Trigger "Open"
        controller.AddParameter(ParamOpen, AnimatorControllerParameterType.Trigger);

        // Obtener la state machine de la capa base (layer 0)
        var sm = controller.layers[0].stateMachine;

        // Estado Closed: default, sin motion (la puerta esta estatica cerrada)
        var estadoClosed = sm.AddState("Closed");
        sm.defaultState = estadoClosed;

        // Estado Open: reproduce el clip de apertura
        var estadoOpen = sm.AddState("Open");
        estadoOpen.motion = clipApertura;

        // Transicion Closed -> Open disparada por el parametro Open (Trigger)
        var transicion = estadoClosed.AddTransition(estadoOpen);
        transicion.AddCondition(AnimatorConditionMode.If, 0f, ParamOpen);
        transicion.hasExitTime = false;

        return controller;
    }
}
