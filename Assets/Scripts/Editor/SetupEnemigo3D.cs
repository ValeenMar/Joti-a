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
    private const string RutaAnimacionesRaiz = "Assets/Animaciones";
    private const string RutaAnimacionesEnemigo = "Assets/Animaciones/Enemigo";
    private const string RutaClipsEnemigo = "Assets/Animaciones/Enemigo/Clips";
    private const string RutaController = "Assets/Animaciones/Enemigo/EnemigoController.controller";

    private const string RutaClipIdle = "Assets/Animaciones/Enemigo/Clips/Idle.anim";
    private const string RutaClipPatrullar = "Assets/Animaciones/Enemigo/Clips/Patrullar.anim";
    private const string RutaClipPerseguir = "Assets/Animaciones/Enemigo/Clips/Perseguir.anim";
    private const string RutaClipAtacar = "Assets/Animaciones/Enemigo/Clips/Atacar.anim";
    private const string RutaClipRecibirDanio = "Assets/Animaciones/Enemigo/Clips/RecibirDanio.anim";
    private const string RutaClipMorir = "Assets/Animaciones/Enemigo/Clips/Morir.anim";

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

        Transform contenedorModelo = ObtenerOCrearContenedor(enemigo.transform);
        LimpiarHijos(contenedorModelo);

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefabEsqueleto, contenedorModelo);
        modelo.name = "ModeloEnemigo";
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        modelo.transform.localScale = Vector3.one;

        Animator animador = modelo.GetComponent<Animator>();
        if (animador != null)
        {
            animador.runtimeAnimatorController = controller;
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animador);
        }

        ControladorAnimacionEnemigo controladorAnimacion = modelo.GetComponent<ControladorAnimacionEnemigo>();
        if (controladorAnimacion == null)
        {
            controladorAnimacion = modelo.AddComponent<ControladorAnimacionEnemigo>();
        }

        SerializedObject soControlador = new SerializedObject(controladorAnimacion);
        soControlador.FindProperty("animador").objectReferenceValue = animador;
        soControlador.FindProperty("enemigoDummy").objectReferenceValue = enemigo;
        soControlador.FindProperty("agenteNavMesh").objectReferenceValue = enemigo.GetComponent<NavMeshAgent>();
        soControlador.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controladorAnimacion);

        DesactivarVisualVieja(enemigo.gameObject);
        AjustarCapsulaYAgente(enemigo.gameObject, modelo.transform);
        ConfigurarZonasDebiles(enemigo.transform, modelo.transform);
        AdjuntarEspada(modelo.transform, prefabEspada);

        EnlazarVidaYIA(enemigo, controladorAnimacion);

        PrefabUtility.RecordPrefabInstancePropertyModifications(enemigo);
        PrefabUtility.RecordPrefabInstancePropertyModifications(modelo);
        if (animador != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(animador);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(controladorAnimacion);

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

        AnimationClip clipIdle = CrearClip(RutaClipIdle, true, null, ConstruirIdle, prefabEsqueleto);
        AnimationClip clipPatrullar = CrearClip(RutaClipPatrullar, true, null, ConstruirPatrullar, prefabEsqueleto);
        AnimationClip clipPerseguir = CrearClip(RutaClipPerseguir, true, null, ConstruirPerseguir, prefabEsqueleto);
        AnimationClip clipAtacar = CrearClip(RutaClipAtacar, false, CrearEvento("AplicarDanioAtaque", 0.42f), ConstruirAtaque, prefabEsqueleto);
        AnimationClip clipRecibirDanio = CrearClip(RutaClipRecibirDanio, false, null, ConstruirRecibirDanio, prefabEsqueleto);
        AnimationClip clipMorir = CrearClip(RutaClipMorir, false, CrearEvento("NotificarMuerteAnimacionTerminada", 1.02f), ConstruirMorir, prefabEsqueleto);

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

        Debug.Log("SetupEnemigo3D: controller y clips creados en Assets/Animaciones/Enemigo.");
        return controller;
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
        Transform existente = raiz.Find("ModeloEnemigo");
        if (existente != null)
        {
            return existente;
        }

        GameObject contenedor = new GameObject("ModeloEnemigo");
        contenedor.transform.SetParent(raiz, false);
        Undo.RegisterCreatedObjectUndo(contenedor, "Crear ModeloEnemigo");
        return contenedor.transform;
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

    private static Transform BuscarTransform(Transform raiz, string nombre)
    {
        return raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);
    }

    // COPILOT-EXPAND:
    // Se puede extender este setup para crear variantes de esqueleto, ragdoll,
    // o integrar multiples armas y animaciones importadas sin tocar la IA base.
}
