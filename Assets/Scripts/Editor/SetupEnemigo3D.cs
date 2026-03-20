using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// COPILOT-CONTEXT:
// Realm Brawl, Unity 2022.3.
// Herramienta de editor para convertir el enemigo dummy en esqueleto 3D,
// crear el Animator Controller y generar clips placeholder utiles.

public static class SetupEnemigo3D
{
    private const string NombreContenedorVisual = "VisualEnemigo";
    private const string RutaAnimacionesRaiz = "Assets/Animaciones";
    private const string RutaAnimacionesEnemigo = "Assets/Animaciones/Enemigo";
    private const string RutaClipsEnemigo = "Assets/Animaciones/Enemigo/Clips";
    private const string RutaController = "Assets/Animaciones/Enemigo/EnemigoController.controller";
    private const string RutaCarpetaMateriales = "Assets/Materials";
    private const string RutaMaterialHueso = "Assets/Materials/Material_Esqueleto_Hueso.mat";
    private static readonly Color ColorMaterialHueso = new Color(0.82f, 0.78f, 0.70f, 1f);
    private const float GlossinessMaterialHueso = 0.05f;
    private const float MetallicMaterialHueso = 0f;

    private const string RutaClipIdle = "Assets/Animaciones/Enemigo/Clips/Idle.anim";
    private const string RutaClipPatrullar = "Assets/Animaciones/Enemigo/Clips/Patrullar.anim";
    private const string RutaClipPerseguir = "Assets/Animaciones/Enemigo/Clips/Perseguir.anim";
    private const string RutaClipAtacar = "Assets/Animaciones/Enemigo/Clips/Atacar.anim";
    private const string RutaClipRecibirDanio = "Assets/Animaciones/Enemigo/Clips/RecibirDanio.anim";
    private const string RutaClipMorir = "Assets/Animaciones/Enemigo/Clips/Morir.anim";

    private const string RutaIdleImportado = "Assets/Animations/Breathing Idle.fbx";
    private const string RutaCaminarImportado = "Assets/Animations/Walking.fbx";
    private const string RutaCorrerImportado = "Assets/Animations/Running.fbx";
    private const string RutaAtacarImportado = "Assets/Animations/Sword And Shield Slash.fbx";
    private const string RutaRecibirDanioImportado = "Assets/Animations/Hit Reaction.fbx";
    private const string RutaMorirImportado = "Assets/Animations/Dying.fbx";

    [MenuItem("Realm Brawl/Setup/Enemigo Esqueleto 3D")]
    public static void ConfigurarEnemigoEsqueleto3D()
    {
        AsegurarCarpetas();

        EnemigoDummy enemigo = Object.FindObjectsOfType<EnemigoDummy>(true)
            .FirstOrDefault(e => e != null && e.gameObject.scene.IsValid());

        if (enemigo == null)
        {
            Debug.LogError("SetupEnemigo3D: no encontre un EnemigoDummy en la escena.");
            return;
        }

        GameObject prefabEsqueleto = BuscarPrefab("PT_Skeleton_Male_Modular");
        if (prefabEsqueleto == null)
        {
            Debug.LogError("SetupEnemigo3D: no encontre el prefab PT_Skeleton_Male_Modular.");
            return;
        }

        GameObject prefabEspada = BuscarPrefab(
            "PT_Longsword_01_a",
            "PT_Sword_01_a",
            "PT_Mace_01_a",
            "PT_ShortWaraxe_01_a");

        AnimatorController controller = CrearControllerYClips(prefabEsqueleto);

        EliminarVisualesPrevios(enemigo.transform);
        Transform contenedorModelo = ObtenerOCrearContenedor(enemigo.transform);
        LimpiarHijos(contenedorModelo);

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefabEsqueleto, contenedorModelo);
        modelo.name = "ModeloEnemigo";
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        modelo.transform.localScale = Vector3.one;

        Animator animador = BuscarAnimatorVisualModelo(modelo.transform);
        if (animador != null)
        {
            animador.runtimeAnimatorController = controller;
            animador.avatar = ObtenerAvatarDesdePrefab(prefabEsqueleto, animador);
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animador.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animador);
        }
        else
        {
            animador = modelo.AddComponent<Animator>();
            animador.runtimeAnimatorController = controller;
            animador.avatar = ObtenerAvatarDesdePrefab(prefabEsqueleto, animador);
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animador.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animador);
        }

        ControladorAnimacionEnemigo controladorEnContenedor = modelo.GetComponent<ControladorAnimacionEnemigo>();
        if (controladorEnContenedor != null && controladorEnContenedor.gameObject != animador.gameObject)
        {
            Object.DestroyImmediate(controladorEnContenedor);
        }

        ControladorAnimacionEnemigo controladorAnimacion = animador.GetComponent<ControladorAnimacionEnemigo>();
        if (controladorAnimacion == null)
        {
            controladorAnimacion = animador.gameObject.AddComponent<ControladorAnimacionEnemigo>();
        }

        SerializedObject soControlador = new SerializedObject(controladorAnimacion);
        soControlador.FindProperty("animador").objectReferenceValue = animador;
        soControlador.FindProperty("enemigoDummy").objectReferenceValue = enemigo;
        soControlador.FindProperty("feedbackCombate").objectReferenceValue = enemigo.GetComponentInChildren<FeedbackCombate>(true);
        soControlador.FindProperty("agenteNavMesh").objectReferenceValue = enemigo.GetComponent<NavMeshAgent>();
        soControlador.FindProperty("raizVisual").objectReferenceValue = modelo.transform;
        soControlador.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controladorAnimacion);

        DesactivarVisualVieja(enemigo.gameObject);
        AlinearEnemigoAlNavMesh(enemigo.transform);
        AlinearModeloSobreSuelo(modelo.transform, animador);
        AjustarCapsulaYAgente(enemigo.gameObject, modelo.transform);
        ReinicializarAnimatorSeguro(animador);
        ConfigurarZonasDebiles(enemigo.transform, modelo.transform);
        AdjuntarEspada(modelo.transform, prefabEspada);
        NormalizarMaterialesModelo(modelo.transform);
        AplicarMaterialHuesoEditor(modelo);

        EnlazarVidaYIA(enemigo, controladorAnimacion);
        ConfigurarFeedbackVisual(enemigo.gameObject);

        PrefabUtility.RecordPrefabInstancePropertyModifications(enemigo);
        PrefabUtility.RecordPrefabInstancePropertyModifications(modelo);
        if (animador != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(animador);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(controladorAnimacion);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(enemigo.gameObject.scene);
        Selection.activeGameObject = enemigo.gameObject;

        Debug.Log("SetupEnemigo3D: enemigo esqueleto configurado correctamente.");
    }

    private static AnimatorController CrearControllerYClips(GameObject prefabEsqueleto)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController) != null)
        {
            AssetDatabase.DeleteAsset(RutaController);
        }

        AnimationClip clipIdleFuente = CargarClipPrincipalDesdeFbx(RutaIdleImportado);
        AnimationClip clipPatrullarFuente = CargarClipPrincipalDesdeFbx(RutaCaminarImportado);
        AnimationClip clipPerseguirFuente = CargarClipPrincipalDesdeFbx(RutaCorrerImportado);
        AnimationClip clipAtacarFuente = CargarClipPrincipalDesdeFbx(RutaAtacarImportado);
        AnimationClip clipRecibirDanioFuente = CargarClipPrincipalDesdeFbx(RutaRecibirDanioImportado);
        AnimationClip clipMorirFuente = CargarClipPrincipalDesdeFbx(RutaMorirImportado);

        AnimationClip clipIdle = CrearClipDesdeFuenteOPlaceholder(
            RutaClipIdle,
            clipIdleFuente,
            true,
            null,
            ConstruirIdle,
            prefabEsqueleto);

        AnimationClip clipPatrullar = CrearClipDesdeFuenteOPlaceholder(
            RutaClipPatrullar,
            clipPatrullarFuente,
            true,
            null,
            ConstruirPatrullar,
            prefabEsqueleto);

        AnimationClip clipPerseguir = CrearClipDesdeFuenteOPlaceholder(
            RutaClipPerseguir,
            clipPerseguirFuente,
            true,
            null,
            ConstruirPerseguir,
            prefabEsqueleto);

        AnimationClip clipAtacar = CrearClipDesdeFuenteOPlaceholder(
            RutaClipAtacar,
            clipAtacarFuente,
            false,
            CrearEvento("AplicarDanioAtaque", clipAtacarFuente != null ? clipAtacarFuente.length * 0.4f : 0.42f),
            ConstruirAtaque,
            prefabEsqueleto);

        AnimationClip clipRecibirDanio = CrearClipDesdeFuenteOPlaceholder(
            RutaClipRecibirDanio,
            clipRecibirDanioFuente,
            false,
            null,
            ConstruirRecibirDanio,
            prefabEsqueleto);

        AnimationClip clipMorir = CrearClipDesdeFuenteOPlaceholder(
            RutaClipMorir,
            clipMorirFuente,
            false,
            CrearEvento("NotificarMuerteAnimacionTerminada", clipMorirFuente != null ? clipMorirFuente.length * 0.95f : 1.02f),
            ConstruirMorir,
            prefabEsqueleto);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(RutaController);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        controller.parameters = new[]
        {
            new AnimatorControllerParameter { name = "Velocidad", type = AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name = "Atacar", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "RecibirDanio", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Morir", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Persiguiendo", type = AnimatorControllerParameterType.Bool },
        };

        AnimatorState idle = sm.AddState("Idle");
        AnimatorState patrullar = sm.AddState("Patrullar");
        AnimatorState perseguir = sm.AddState("Perseguir");
        AnimatorState atacar = sm.AddState("Atacar");
        AnimatorState recibirDanio = sm.AddState("RecibirDanio");
        AnimatorState morir = sm.AddState("Morir");

        idle.motion = clipIdle;
        patrullar.motion = clipPatrullar;
        perseguir.motion = clipPerseguir;
        atacar.motion = clipAtacar;
        recibirDanio.motion = clipRecibirDanio;
        morir.motion = clipMorir;

        atacar.speed = 1.05f;
        recibirDanio.speed = 1.1f;
        morir.speed = 1f;

        sm.defaultState = idle;

        CrearTransicionDoble(idle, patrullar, "Velocidad", AnimatorConditionMode.Greater, 0.1f, "Persiguiendo", AnimatorConditionMode.IfNot, 0f, 0.1f, 0f);
        CrearTransicionDoble(idle, perseguir, "Velocidad", AnimatorConditionMode.Greater, 0.1f, "Persiguiendo", AnimatorConditionMode.If, 0f, 0.1f, 0f);
        CrearTransicion(patrullar, idle, "Velocidad", AnimatorConditionMode.Less, 0.1f, 0.2f, 0f);
        CrearTransicion(perseguir, idle, "Velocidad", AnimatorConditionMode.Less, 0.1f, 0.2f, 0f);
        CrearTransicion(patrullar, perseguir, "Persiguiendo", AnimatorConditionMode.If, 0f, 0.1f, 0f);
        CrearTransicion(perseguir, patrullar, "Persiguiendo", AnimatorConditionMode.IfNot, 0f, 0.1f, 0f);

        CrearAnyState(sm, atacar, "Atacar", 0.05f);
        CrearAnyState(sm, recibirDanio, "RecibirDanio", 0.05f);
        CrearAnyState(sm, morir, "Morir", 0.0f, true);

        CrearSalida(atacar, idle, 1f, 0.15f);
        CrearSalida(recibirDanio, idle, 1f, 0.05f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SetupEnemigo3D: controller y clips del esqueleto creados usando animaciones reales cuando estuvieron disponibles.");
        return controller;
    }

    private static AnimationClip CrearClipDesdeFuenteOPlaceholder(string ruta, AnimationClip clipFuente, bool loop, AnimationEvent[] eventos, System.Action<AnimationClip, Transform> construirFallback, GameObject prefabEsqueleto)
    {
        if (clipFuente != null)
        {
            return CrearClipDesdeFuente(ruta, clipFuente, loop, eventos);
        }

        return CrearClip(ruta, loop, eventos, construirFallback, prefabEsqueleto);
    }

    private static AnimationClip CrearClipDesdeFuente(string ruta, AnimationClip clipFuente, bool loop, AnimationEvent[] eventos)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimationClip clipNuevo = Object.Instantiate(clipFuente);
        clipNuevo.name = System.IO.Path.GetFileNameWithoutExtension(ruta);
        AssetDatabase.CreateAsset(clipNuevo, ruta);

        ConfigurarLoop(clipNuevo, loop);

        if (eventos != null && eventos.Length > 0)
        {
            AnimationUtility.SetAnimationEvents(clipNuevo, eventos);
        }

        EditorUtility.SetDirty(clipNuevo);
        return clipNuevo;
    }

    private static AnimationClip CrearClip(string ruta, bool loop, AnimationEvent[] eventos, System.Action<AnimationClip, Transform> construir, GameObject prefabEsqueleto)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimationClip clip = new AnimationClip { frameRate = 30f };
        AssetDatabase.CreateAsset(clip, ruta);

        GameObject instanciaTemporal = (GameObject)PrefabUtility.InstantiatePrefab(prefabEsqueleto);
        instanciaTemporal.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            construir?.Invoke(clip, instanciaTemporal.transform);
        }
        finally
        {
            Object.DestroyImmediate(instanciaTemporal);
        }

        ConfigurarLoop(clip, loop);
        if (eventos != null && eventos.Length > 0)
        {
            AnimationUtility.SetAnimationEvents(clip, eventos);
        }
        return clip;
    }

    private static AnimationClip CargarClipPrincipalDesdeFbx(string rutaFbx)
    {
        Object[] assetsRuta = AssetDatabase.LoadAllAssetsAtPath(rutaFbx);
        if (assetsRuta == null || assetsRuta.Length == 0)
        {
            Debug.LogWarning("[SetupEnemigo3D] No se encontraron assets en " + rutaFbx);
            return null;
        }

        for (int indiceAsset = 0; indiceAsset < assetsRuta.Length; indiceAsset++)
        {
            AnimationClip clipActual = assetsRuta[indiceAsset] as AnimationClip;
            if (clipActual == null)
            {
                continue;
            }

            if (clipActual.name.StartsWith("__"))
            {
                continue;
            }

            return clipActual;
        }

        Debug.LogWarning("[SetupEnemigo3D] No se encontro un AnimationClip util dentro de " + rutaFbx);
        return null;
    }

    private static void ConstruirIdle(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_Hips", "m_LocalPosition.y", 0f, 0.02f, 0f, 1.5f);
        CurvaPosicion(clip, raiz, "PT_Spine3", "m_LocalPosition.y", 0f, 0.01f, 0f, 1.5f);
        CurvaPosicion(clip, raiz, "PT_Head", "m_LocalPosition.x", 0f, 0.005f, 0f, 1.5f);
    }

    private static void ConstruirPatrullar(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_RightLeg", "m_LocalPosition.x", -0.03f, 0.03f, -0.03f, 1f);
        CurvaPosicion(clip, raiz, "PT_LeftLeg", "m_LocalPosition.x", 0.03f, -0.03f, 0.03f, 1f);
        CurvaPosicion(clip, raiz, "PT_RightArm", "m_LocalPosition.x", 0.02f, -0.02f, 0.02f, 1f);
        CurvaPosicion(clip, raiz, "PT_LeftArm", "m_LocalPosition.x", -0.02f, 0.02f, -0.02f, 1f);
        CurvaPosicion(clip, raiz, "PT_Hips", "m_LocalPosition.y", 0f, 0.02f, 0f, 1f);
    }

    private static void ConstruirPerseguir(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_RightLeg", "m_LocalPosition.x", -0.05f, 0.05f, -0.05f, 0.75f);
        CurvaPosicion(clip, raiz, "PT_LeftLeg", "m_LocalPosition.x", 0.05f, -0.05f, 0.05f, 0.75f);
        CurvaPosicion(clip, raiz, "PT_RightArm", "m_LocalPosition.x", 0.03f, -0.03f, 0.03f, 0.75f);
        CurvaPosicion(clip, raiz, "PT_LeftArm", "m_LocalPosition.x", -0.03f, 0.03f, -0.03f, 0.75f);
        CurvaPosicion(clip, raiz, "PT_Hips", "m_LocalPosition.y", 0f, 0.03f, 0f, 0.75f);
    }

    private static void ConstruirAtaque(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_RightArm", "m_LocalPosition.x", 0.01f, 0.05f, -0.04f, 0.7f);
        CurvaPosicion(clip, raiz, "PT_RightArm", "m_LocalPosition.z", 0f, -0.03f, 0.05f, 0.7f);
        CurvaPosicion(clip, raiz, "PT_RightForeArm", "m_LocalPosition.x", 0.01f, 0.04f, -0.02f, 0.7f);
        CurvaPosicion(clip, raiz, "PT_Weapon_slot", "m_LocalPosition.x", 0f, -0.03f, 0.04f, 0.7f);
        CurvaPosicion(clip, raiz, "PT_Spine3", "m_LocalPosition.y", 0f, 0.01f, -0.02f, 0.7f);
    }

    private static void ConstruirRecibirDanio(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_Spine3", "m_LocalPosition.z", 0f, -0.03f, 0f, 0.2f);
        CurvaPosicion(clip, raiz, "PT_Spine3", "m_LocalPosition.y", 0f, 0.02f, 0f, 0.2f);
        CurvaPosicion(clip, raiz, "PT_Head", "m_LocalPosition.x", 0f, -0.02f, 0f, 0.2f);
    }

    private static void ConstruirMorir(AnimationClip clip, Transform raiz)
    {
        CurvaPosicion(clip, raiz, "PT_Hips", "m_LocalPosition.y", 0f, -0.08f, -0.18f, 1.2f);
        CurvaPosicion(clip, raiz, "PT_Spine3", "m_LocalPosition.z", 0f, 0.02f, 0.12f, 1.2f);
        CurvaPosicion(clip, raiz, "PT_Head", "m_LocalPosition.x", 0f, 0.01f, 0.02f, 1.2f);
        CurvaPosicion(clip, raiz, "PT_LeftLeg", "m_LocalPosition.x", 0f, -0.01f, 0.02f, 1.2f);
        CurvaPosicion(clip, raiz, "PT_RightLeg", "m_LocalPosition.x", 0f, 0.01f, -0.02f, 1.2f);
    }

    private static void CurvaPosicion(AnimationClip clip, Transform raiz, string hueso, string propiedad, float valorInicio, float valorMedio, float valorFinal, float duracion)
    {
        Transform target = BuscarTransform(raiz, hueso);
        if (target == null)
        {
            return;
        }

        string ruta = AnimationUtility.CalculateTransformPath(target, raiz);
        AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, valorInicio),
            new Keyframe(duracion * 0.5f, valorMedio),
            new Keyframe(duracion, valorFinal));

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(ruta, typeof(Transform), propiedad), curva);
    }

    private static void ConfigurarLoop(AnimationClip clip, bool loop)
    {
        SerializedObject so = new SerializedObject(clip);
        SerializedProperty settings = so.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
            settings.FindPropertyRelative("m_LoopBlend").boolValue = loop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static AnimationEvent[] CrearEvento(string funcion, float tiempo)
    {
        return new[]
        {
            new AnimationEvent
            {
                functionName = funcion,
                time = tiempo,
                messageOptions = SendMessageOptions.RequireReceiver
            }
        };
    }

    private static void CrearTransicion(AnimatorState origen, AnimatorState destino, string parametro, AnimatorConditionMode modo, float umbral, float duracion, float offset)
    {
        AnimatorStateTransition t = origen.AddTransition(destino);
        t.hasExitTime = false;
        t.duration = duracion;
        t.offset = offset;
        t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
        t.AddCondition(modo, umbral, parametro);
    }

    private static void CrearTransicionDoble(AnimatorState origen, AnimatorState destino, string parametro1, AnimatorConditionMode modo1, float umbral1, string parametro2, AnimatorConditionMode modo2, float umbral2, float duracion, float offset)
    {
        AnimatorStateTransition t = origen.AddTransition(destino);
        t.hasExitTime = false;
        t.duration = duracion;
        t.offset = offset;
        t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
        t.AddCondition(modo1, umbral1, parametro1);
        t.AddCondition(modo2, umbral2, parametro2);
    }

    private static void CrearAnyState(AnimatorStateMachine sm, AnimatorState destino, string trigger, float duracion, bool esMuerte = false)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(destino);
        t.hasExitTime = false;
        t.duration = duracion;
        t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        if (esMuerte)
        {
            t.canTransitionToSelf = false;
            t.orderedInterruption = true;
        }
    }

    private static void CrearSalida(AnimatorState origen, AnimatorState destino, float exitTime, float duracion)
    {
        AnimatorStateTransition t = origen.AddTransition(destino);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.duration = duracion;
        t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
    }

    private static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder(RutaAnimacionesRaiz))
        {
            AssetDatabase.CreateFolder("Assets", "Animaciones");
        }

        if (!AssetDatabase.IsValidFolder(RutaAnimacionesEnemigo))
        {
            AssetDatabase.CreateFolder(RutaAnimacionesRaiz, "Enemigo");
        }

        if (!AssetDatabase.IsValidFolder(RutaClipsEnemigo))
        {
            AssetDatabase.CreateFolder(RutaAnimacionesEnemigo, "Clips");
        }

        if (!AssetDatabase.IsValidFolder(RutaCarpetaMateriales))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
    }

    private static GameObject BuscarPrefab(params string[] nombres)
    {
        foreach (string nombre in nombres)
        {
            string[] guids = AssetDatabase.FindAssets(nombre + " t:Prefab", new[] { "Assets/Polytope Studio" });
            foreach (string guid in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab != null && prefab.name == nombre)
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    private static Transform ObtenerOCrearContenedor(Transform raiz)
    {
        Transform existente = raiz.Find(NombreContenedorVisual);
        if (existente != null)
        {
            return existente;
        }

        GameObject contenedor = new GameObject(NombreContenedorVisual);
        contenedor.transform.SetParent(raiz, false);
        Undo.RegisterCreatedObjectUndo(contenedor, "Crear ModeloEnemigo");
        return contenedor.transform;
    }

    private static void EliminarVisualesPrevios(Transform raizEnemigo)
    {
        if (raizEnemigo == null)
        {
            return;
        }

        for (int indiceHijo = raizEnemigo.childCount - 1; indiceHijo >= 0; indiceHijo--)
        {
            Transform hijo = raizEnemigo.GetChild(indiceHijo);
            if (hijo == null)
            {
                continue;
            }

            bool esContenedorVisual = hijo.name == NombreContenedorVisual;
            bool esModeloDirectoViejo =
                hijo.name == "ModeloEnemigo" &&
                (hijo.GetComponentInChildren<SkinnedMeshRenderer>(true) != null ||
                 hijo.GetComponentInChildren<Animator>(true) != null);

            if (esContenedorVisual || esModeloDirectoViejo)
            {
                Object.DestroyImmediate(hijo.gameObject);
            }
        }
    }

    private static void LimpiarHijos(Transform raiz)
    {
        for (int i = raiz.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(raiz.GetChild(i).gameObject);
        }
    }

    private static void DesactivarVisualVieja(GameObject enemigo)
    {
        MeshRenderer renderer = enemigo.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        MeshFilter filter = enemigo.GetComponent<MeshFilter>();
        if (filter != null)
        {
            filter.sharedMesh = null;
        }
    }

    private static void AjustarCapsulaYAgente(GameObject enemigo, Transform modelo)
    {
        CapsuleCollider capsule = enemigo.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = Undo.AddComponent<CapsuleCollider>(enemigo);
        }

        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        capsule.direction = 1;
        capsule.height = Mathf.Max(1.2f, bounds.size.y * 0.95f);
        capsule.radius = Mathf.Max(0.25f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f);
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

        NavMeshAgent agente = enemigo.GetComponent<NavMeshAgent>();
        if (agente == null)
        {
            agente = Undo.AddComponent<NavMeshAgent>(enemigo);
        }

        if (agente != null)
        {
            agente.height = capsule.height;
            agente.radius = capsule.radius;
            agente.baseOffset = 0f;
            agente.stoppingDistance = Mathf.Max(1.4f, capsule.radius + 0.5f);
            agente.angularSpeed = 720f;
            agente.acceleration = 16f;
        }
    }

    private static void AdjuntarEspada(Transform modelo, GameObject prefabEspada)
    {
        Transform slot = BuscarTransform(modelo, "PT_Right_Hand_Weapon_slot") ?? BuscarTransform(modelo, "PT_Weapon_slot");
        if (slot == null)
        {
            return;
        }

        Transform armaExistente = slot.Find("EspadaEnemigo");
        if (armaExistente != null)
        {
            Object.DestroyImmediate(armaExistente.gameObject);
        }

        if (prefabEspada == null)
        {
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = "EspadaEnemigo";
            placeholder.transform.SetParent(slot, false);
            placeholder.transform.localPosition = Vector3.zero;
            placeholder.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            placeholder.transform.localScale = new Vector3(0.12f, 0.12f, 0.9f);
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            return;
        }

        GameObject espada = (GameObject)PrefabUtility.InstantiatePrefab(prefabEspada, slot);
        espada.name = "EspadaEnemigo";
        espada.transform.localPosition = Vector3.zero;
        espada.transform.localRotation = Quaternion.identity;
        espada.transform.localScale = Vector3.one;
    }

    private static void ConfigurarZonasDebiles(Transform raizEnemigo, Transform modelo)
    {
        CrearZona(raizEnemigo, BuscarTransform(modelo, "PT_Head") ?? modelo, "Cabeza", TipoZonaDanio.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.08f), new Vector3(0.45f, 0.45f, 0.45f));
        CrearZona(raizEnemigo, BuscarTransform(modelo, "PT_Spine3") ?? modelo, "Espalda", TipoZonaDanio.Espalda, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.12f), new Vector3(0.55f, 0.75f, 0.25f));
        CrearZona(raizEnemigo, BuscarTransform(modelo, "PT_Hips") ?? modelo, "Cuerpo", TipoZonaDanio.Cuerpo, PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.8f, 0.5f));
    }

    private static void CrearZona(Transform raizEnemigo, Transform padre, string nombre, TipoZonaDanio tipoZona, PrimitiveType primitiva, Vector3 posicionLocal, Vector3 escalaLocal)
    {
        Transform existente = BuscarTransform(raizEnemigo, nombre);
        if (existente != null)
        {
            Object.DestroyImmediate(existente.gameObject);
        }

        GameObject zona = GameObject.CreatePrimitive(primitiva);
        zona.name = nombre;
        zona.transform.SetParent(padre, false);
        zona.transform.localPosition = posicionLocal;
        zona.transform.localRotation = Quaternion.identity;
        zona.transform.localScale = escalaLocal;

        Collider collider = zona.GetComponent<Collider>();
        collider.isTrigger = true;

        MeshRenderer renderer = zona.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        ZonasDebiles zonasDebiles = zona.GetComponent<ZonasDebiles>();
        if (zonasDebiles == null)
        {
            zonasDebiles = Undo.AddComponent<ZonasDebiles>(zona);
        }

        SerializedObject so = new SerializedObject(zonasDebiles);
        so.FindProperty("tipoZona").enumValueIndex = (int)tipoZona;
        so.FindProperty("multiplicadorManual").floatValue = -1f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnlazarVidaYIA(EnemigoDummy enemigo, ControladorAnimacionEnemigo controladorAnimacion)
    {
        VidaEnemigo vida = enemigo.GetComponent<VidaEnemigo>();
        if (vida != null)
        {
            SerializedObject soVida = new SerializedObject(vida);
            soVida.FindProperty("controladorAnimacionEnemigo").objectReferenceValue = controladorAnimacion;
            soVida.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(vida);
        }

        SerializedObject soIA = new SerializedObject(enemigo);
        SerializedProperty propControlador = soIA.FindProperty("controladorAnimacionEnemigo");
        if (propControlador != null)
        {
            propControlador.objectReferenceValue = controladorAnimacion;
            soIA.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(enemigo);
    }

    private static void ConfigurarFeedbackVisual(GameObject enemigo)
    {
        FeedbackCombate feedback = enemigo.GetComponent<FeedbackCombate>();
        if (feedback == null)
        {
            return;
        }

        SerializedObject soFeedback = new SerializedObject(feedback);
        SerializedProperty propiedadColorRango = soFeedback.FindProperty("colorIndicadorRango");
        SerializedProperty propiedadIntensidad = soFeedback.FindProperty("intensidadIndicadorRango");
        SerializedProperty propiedadUsarEmision = soFeedback.FindProperty("usarEmision");

        if (propiedadColorRango != null)
        {
            propiedadColorRango.colorValue = new Color(1f, 0.3f, 0.3f, 1f);
        }

        if (propiedadIntensidad != null)
        {
            propiedadIntensidad.floatValue = 0.14f;
        }

        if (propiedadUsarEmision != null)
        {
            propiedadUsarEmision.boolValue = false;
        }

        soFeedback.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feedback);
    }

    private static void NormalizarMaterialesModelo(Transform modelo)
    {
        if (modelo == null)
        {
            return;
        }

        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>(true);
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            if (renderers[indiceRenderer] == null)
            {
                continue;
            }

            Material[] materiales = renderers[indiceRenderer].sharedMaterials;
            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null)
                {
                    continue;
                }

                if (Shader.Find("Standard") != null)
                {
                    material.shader = Shader.Find("Standard");
                }

                bool tieneTexturaBase =
                    material.mainTexture != null ||
                    (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null) ||
                    (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null);

                Color colorActual = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                bool colorAmarillo = colorActual.r > 0.7f && colorActual.g > 0.5f && colorActual.b < 0.3f;

                if (!tieneTexturaBase || colorAmarillo)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", new Color(0.82f, 0.78f, 0.70f, 1f));
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", new Color(0.82f, 0.78f, 0.70f, 1f));
                    }

                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", 0.05f);
                    }

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", 0f);
                    }

                    EditorUtility.SetDirty(material);
                    continue;
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", Color.white);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", Color.white);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", 0.05f);
                }

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0f);
                }

                EditorUtility.SetDirty(material);
            }
        }
    }

    private static void AplicarMaterialHuesoEditor(GameObject esqueleto)
    {
        if (esqueleto == null)
        {
            return;
        }

        Material materialHueso = ObtenerOCrearMaterialHuesoEditor();
        if (materialHueso == null)
        {
            return;
        }

        SkinnedMeshRenderer[] renderers = esqueleto.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            SkinnedMeshRenderer smr = renderers[indiceRenderer];
            if (smr == null)
            {
                continue;
            }

            Material[] materiales = smr.sharedMaterials;
            bool modifico = false;
            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null)
                {
                    continue;
                }

                if (!EsMaterialHuesoObjetivo(material))
                {
                    continue;
                }

                materiales[indiceMaterial] = materialHueso;
                modifico = true;
            }

            if (modifico)
            {
                smr.sharedMaterials = materiales;
                EditorUtility.SetDirty(smr);
                PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
            }
        }
    }

    private static bool EsMaterialHuesoObjetivo(Material material)
    {
        if (material == null)
        {
            return false;
        }

        bool sinTextura =
            material.mainTexture == null &&
            (!material.HasProperty("_MainTex") || material.GetTexture("_MainTex") == null) &&
            (!material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null);

        Color colorActual = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        bool colorAmarillo = colorActual.r > 0.7f && colorActual.g > 0.5f && colorActual.b < 0.3f;

        return sinTextura || colorAmarillo;
    }

    private static Material ObtenerOCrearMaterialHuesoEditor()
    {
        if (!AssetDatabase.IsValidFolder(RutaCarpetaMateriales))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            Debug.LogWarning("[SetupEnemigo3D] No se encontro un shader compatible para Material_Esqueleto_Hueso.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHueso);
        if (material == null)
        {
            material = new Material(shader);
            material.name = "Material_Esqueleto_Hueso";
            AssetDatabase.CreateAsset(material, RutaMaterialHueso);
        }
        else
        {
            material.shader = shader;
        }

        material.name = "Material_Esqueleto_Hueso";
        material.color = ColorMaterialHueso;
        material.SetFloat("_Glossiness", GlossinessMaterialHueso);
        material.SetFloat("_Metallic", MetallicMaterialHueso);
        EditorUtility.SetDirty(material);
        return material;
    }

    [MenuItem("Realm Brawl/Setup/Fix Esqueletos")]
    public static void FixEsqueletosEnEscena()
    {
        AsegurarCarpetas();

        EnemigoDummy[] enemigos = Object.FindObjectsOfType<EnemigoDummy>(true);
        int corregidos = 0;

        for (int indice = 0; indice < enemigos.Length; indice++)
        {
            EnemigoDummy enemigo = enemigos[indice];
            if (enemigo == null || !enemigo.gameObject.scene.IsValid() || !enemigo.gameObject.activeInHierarchy)
            {
                continue;
            }

            NormalizarMaterialesModelo(enemigo.transform);
            AplicarMaterialHuesoEditor(enemigo.gameObject);
            EditorUtility.SetDirty(enemigo.gameObject);
            EditorSceneManager.MarkSceneDirty(enemigo.gameObject.scene);
            corregidos++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupEnemigo3D] Fix Esqueletos termino. Corregidos: " + corregidos);
    }

    private static void AlinearModeloSobreSuelo(Transform raizModelo, Animator animador)
    {
        if (raizModelo == null)
        {
            return;
        }

        float alturaMinima = float.MaxValue;
        bool encontroPies = false;

        if (animador != null && animador.isHuman)
        {
            Transform pieIzquierdo = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform pieDerecho = animador.GetBoneTransform(HumanBodyBones.RightFoot);

            if (pieIzquierdo != null)
            {
                alturaMinima = Mathf.Min(alturaMinima, pieIzquierdo.position.y);
                encontroPies = true;
            }

            if (pieDerecho != null)
            {
                alturaMinima = Mathf.Min(alturaMinima, pieDerecho.position.y);
                encontroPies = true;
            }
        }

        if (!encontroPies)
        {
            Renderer[] renderizadores = raizModelo.GetComponentsInChildren<Renderer>(true);
            if (renderizadores == null || renderizadores.Length == 0)
            {
                return;
            }

            Bounds bounds = renderizadores[0].bounds;
            for (int indice = 1; indice < renderizadores.Length; indice++)
            {
                if (renderizadores[indice] != null)
                {
                    bounds.Encapsulate(renderizadores[indice].bounds);
                }
            }

            alturaMinima = bounds.min.y;
        }

        float alturaObjetivo = raizModelo.parent != null ? raizModelo.parent.position.y : 0f;
        float desplazamientoY = alturaObjetivo - alturaMinima;
        raizModelo.position += new Vector3(0f, desplazamientoY, 0f);
    }

    // Este metodo busca el Animator visual principal del modelo del enemigo.
    private static Animator BuscarAnimatorVisualModelo(Transform raizModelo)
    {
        if (raizModelo == null)
        {
            return null;
        }

        Animator[] animadores = raizModelo.GetComponentsInChildren<Animator>(true);
        for (int indiceAnimator = 0; indiceAnimator < animadores.Length; indiceAnimator++)
        {
            if (animadores[indiceAnimator] == null)
            {
                continue;
            }

            if (animadores[indiceAnimator].GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                continue;
            }

            return animadores[indiceAnimator];
        }

        return raizModelo.GetComponentInChildren<Animator>(true);
    }

    private static void AlinearEnemigoAlNavMesh(Transform raizEnemigo)
    {
        if (raizEnemigo == null)
        {
            return;
        }

        if (NavMesh.SamplePosition(raizEnemigo.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            raizEnemigo.position = hit.position;
        }
    }

    private static void ReinicializarAnimatorSeguro(Animator animador)
    {
        if (animador == null)
        {
            return;
        }

        animador.Rebind();

        if (!animador.gameObject.activeInHierarchy)
        {
            return;
        }

        animador.Update(0f);
    }

    // Este metodo intenta recuperar el Avatar del prefab fuente del esqueleto.
    private static Avatar ObtenerAvatarDesdePrefab(GameObject prefabEsqueleto, Animator animadorActual)
    {
        if (animadorActual != null && animadorActual.avatar != null)
        {
            return animadorActual.avatar;
        }

        if (prefabEsqueleto == null)
        {
            return null;
        }

        Animator animadorPrefab = prefabEsqueleto.GetComponentInChildren<Animator>(true);
        if (animadorPrefab != null && animadorPrefab.avatar != null)
        {
            return animadorPrefab.avatar;
        }

        return null;
    }

    private static Transform BuscarTransform(Transform raiz, string nombre)
    {
        return raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);
    }

    // COPILOT-EXPAND:
    // Se puede extender este setup para crear variantes de esqueleto, ragdoll,
    // o integrar multiples armas y animaciones importadas sin tocar la IA base.
}
