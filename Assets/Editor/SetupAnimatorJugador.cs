using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupAnimatorJugador
{
    private const string RutaControllerJugador = "Assets/Animations/JugadorAnimator.controller";
    private const string RutaCarpetaAnimacionesPaladin = "Assets/Personaje_Paladin/Animaciones/";

    private const string RutaIdle = RutaCarpetaAnimacionesPaladin + "sword and shield idle.fbx";
    private const string RutaIdleBlock = RutaCarpetaAnimacionesPaladin + "sword and shield block idle.fbx";
    private const string RutaWalk = RutaCarpetaAnimacionesPaladin + "sword and shield walk.fbx";
    private const string RutaRun = RutaCarpetaAnimacionesPaladin + "sword and shield run.fbx";
    private const string RutaAttackNormal = RutaCarpetaAnimacionesPaladin + "sword and shield slash.fbx";
    private const string RutaAttackFuerte = RutaCarpetaAnimacionesPaladin + "sword and shield slash (2).fbx";
    private const string RutaHit = RutaCarpetaAnimacionesPaladin + "sword and shield impact.fbx";
    private const string RutaDeath = RutaCarpetaAnimacionesPaladin + "sword and shield death.fbx";
    private const string RutaBlock = RutaCarpetaAnimacionesPaladin + "sword and shield block.fbx";
    private const string RutaJump = RutaCarpetaAnimacionesPaladin + "sword and shield jump.fbx";

    [MenuItem("Realm Brawl/Setup/Animator Jugador")]
    public static void CrearAnimatorJugador()
    {
        AsegurarCarpetaController();

        Avatar avatarModelo = ObtenerAvatarModeloPaladin();
        ConfigurarImportadorModeloPrincipal();

        ConfigurarImportadorClip(RutaIdle, true, avatarModelo);
        ConfigurarImportadorClip(RutaIdleBlock, true, avatarModelo);
        ConfigurarImportadorClip(RutaWalk, true, avatarModelo);
        ConfigurarImportadorClip(RutaRun, true, avatarModelo);
        ConfigurarImportadorClip(RutaAttackNormal, false, avatarModelo);
        ConfigurarImportadorClip(RutaAttackFuerte, false, avatarModelo);
        ConfigurarImportadorClip(RutaHit, false, avatarModelo);
        ConfigurarImportadorClip(RutaDeath, false, avatarModelo);
        ConfigurarImportadorClip(RutaBlock, false, avatarModelo);
        ConfigurarImportadorClip(RutaJump, false, avatarModelo);

        AnimationClip clipIdle = ObtenerClip(RutaIdle);
        AnimationClip clipIdleBlock = ObtenerClip(RutaIdleBlock);
        AnimationClip clipWalk = ObtenerClip(RutaWalk);
        AnimationClip clipRun = ObtenerClip(RutaRun);
        AnimationClip clipAttackNormal = ObtenerClip(RutaAttackNormal);
        AnimationClip clipAttackFuerte = ObtenerClip(RutaAttackFuerte);
        AnimationClip clipHit = ObtenerClip(RutaHit);
        AnimationClip clipDeath = ObtenerClip(RutaDeath);
        AnimationClip clipBlock = ObtenerClip(RutaBlock);
        AnimationClip clipJump = ObtenerClip(RutaJump);

        ConfigurarEventoImpacto(clipAttackNormal, 0.4f, "OnImpactoNormal");
        ConfigurarEventoImpacto(clipAttackFuerte, 0.5f, "OnImpactoFuerte");

        List<string> faltantes = new List<string>();
        RegistrarFaltante(clipIdle, RutaIdle, faltantes);
        RegistrarFaltante(clipIdleBlock, RutaIdleBlock, faltantes);
        RegistrarFaltante(clipWalk, RutaWalk, faltantes);
        RegistrarFaltante(clipRun, RutaRun, faltantes);
        RegistrarFaltante(clipAttackNormal, RutaAttackNormal, faltantes);
        RegistrarFaltante(clipAttackFuerte, RutaAttackFuerte, faltantes);
        RegistrarFaltante(clipHit, RutaHit, faltantes);
        RegistrarFaltante(clipDeath, RutaDeath, faltantes);
        RegistrarFaltante(clipBlock, RutaBlock, faltantes);
        RegistrarFaltante(clipJump, RutaJump, faltantes);

        AnimatorController controllerExistente = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControllerJugador);
        if (controllerExistente != null)
        {
            AssetDatabase.DeleteAsset(RutaControllerJugador);
            AssetDatabase.Refresh();
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(RutaControllerJugador);
        if (controller == null)
        {
            Debug.LogError("[SetupAnimatorJugador] No se pudo crear JugadorAnimator.controller.");
            EditorUtility.DisplayDialog("Animator Jugador", "No se pudo crear el Animator Controller del jugador.", "OK");
            return;
        }

        AgregarParametros(controller);

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState estadoIdle = CrearEstado(maquina, "Idle", clipIdle, new Vector3(240f, 140f, 0f));
        AnimatorState estadoIdleBlock = CrearEstado(maquina, "IdleBlock", clipIdleBlock, new Vector3(240f, 260f, 0f));
        AnimatorState estadoWalk = CrearEstado(maquina, "Walk", clipWalk, new Vector3(460f, 140f, 0f));
        AnimatorState estadoRun = CrearEstado(maquina, "Run", clipRun, new Vector3(680f, 140f, 0f));
        AnimatorState estadoAttackNormal = CrearEstado(maquina, "AttackNormal", clipAttackNormal, new Vector3(470f, 360f, 0f));
        AnimatorState estadoAttackFuerte = CrearEstado(maquina, "AttackFuerte", clipAttackFuerte, new Vector3(690f, 360f, 0f));
        AnimatorState estadoHit = CrearEstado(maquina, "Hit", clipHit, new Vector3(120f, 360f, 0f));
        AnimatorState estadoDeath = CrearEstado(maquina, "Death", clipDeath, new Vector3(-80f, 360f, 0f));
        AnimatorState estadoBlock = CrearEstado(maquina, "Block", clipBlock, new Vector3(900f, 360f, 0f));
        AnimatorState estadoJump = CrearEstado(maquina, "Jump", clipJump, new Vector3(1040f, 140f, 0f));

        estadoAttackNormal.speed = 1.3f;
        estadoAttackFuerte.speed = 0.8f;
        estadoDeath.speed = 1f;

        maquina.defaultState = estadoIdle;

        CrearTransicion(estadoIdle, estadoWalk, false, 0f, 0.15f, AnimatorConditionMode.Greater, 0.1f, "Speed");
        CrearTransicion(estadoWalk, estadoIdle, false, 0f, 0.15f, AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition walkARun = CrearTransicion(estadoWalk, estadoRun, false, 0f, 0.15f, AnimatorConditionMode.Greater, 0.6f, "Speed");
        walkARun.AddCondition(AnimatorConditionMode.If, 0f, "IsSprinting");

        CrearTransicion(estadoRun, estadoWalk, false, 0f, 0.15f, AnimatorConditionMode.Less, 0.6f, "Speed");
        CrearTransicion(estadoRun, estadoWalk, false, 0f, 0.15f, AnimatorConditionMode.IfNot, 0f, "IsSprinting");

        CrearTransicion(estadoIdle, estadoIdleBlock, false, 0f, 0.08f, AnimatorConditionMode.If, 0f, "IsBlocking");
        CrearTransicion(estadoIdleBlock, estadoIdle, false, 0f, 0.08f, AnimatorConditionMode.IfNot, 0f, "IsBlocking");

        CrearTransicionDesdeAnyState(maquina, estadoAttackNormal, 0.08f, "AttackNormal");
        CrearTransicionDesdeAnyState(maquina, estadoAttackFuerte, 0.12f, "AttackFuerte");
        CrearTransicionDesdeAnyState(maquina, estadoHit, 0.05f, "Hit");
        CrearTransicionDesdeAnyState(maquina, estadoJump, 0.10f, "Jump");
        CrearTransicionDesdeAnyState(maquina, estadoDeath, 0.10f, "Die");

        // Compatibilidad con los sistemas actuales de combate y parry del proyecto.
        CrearTransicionDesdeAnyState(maquina, estadoAttackNormal, 0.08f, "Attack");
        CrearTransicionDesdeAnyState(maquina, estadoBlock, 0.05f, "Parry");

        CrearTransicion(estadoAttackNormal, estadoIdle, true, 0.85f, 0.15f);
        CrearTransicion(estadoAttackFuerte, estadoIdle, true, 0.90f, 0.15f);
        CrearTransicion(estadoHit, estadoIdle, true, 0.90f, 0.10f);
        CrearTransicion(estadoJump, estadoIdle, true, 0.95f, 0.10f);

        AnimatorStateTransition blockAIdleBlock = CrearTransicion(estadoBlock, estadoIdleBlock, true, 0.92f, 0.10f);
        blockAIdleBlock.AddCondition(AnimatorConditionMode.If, 0f, "IsBlocking");
        AnimatorStateTransition blockAIdle = CrearTransicion(estadoBlock, estadoIdle, true, 0.92f, 0.10f);
        blockAIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBlocking");

        AsignarControllerEnEscena(controller);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string resumen = faltantes.Count == 0
            ? "Todos los clips del paladín se asignaron correctamente."
            : "Faltan clips: " + string.Join(", ", faltantes);

        Debug.Log("[SetupAnimatorJugador] JugadorAnimator.controller creado. " + resumen);
        EditorUtility.DisplayDialog("Animator Jugador", "Se recreó Assets/Animations/JugadorAnimator.controller.\n" + resumen, "OK");
    }

    private static void AsegurarCarpetaController()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
    }

    private static void ConfigurarImportadorModeloPrincipal()
    {
        const string rutaModelo = "Assets/Personaje_Paladin/Modelo/Paladin WProp J Nordstrom.fbx";
        ModelImporter importador = AssetImporter.GetAtPath(rutaModelo) as ModelImporter;
        if (importador == null)
        {
            Debug.LogWarning("[SetupAnimatorJugador] No se encontró ModelImporter para el modelo principal del paladín.");
            return;
        }

        bool necesitaReimport = false;

        if (importador.animationType != ModelImporterAnimationType.Human)
        {
            importador.animationType = ModelImporterAnimationType.Human;
            necesitaReimport = true;
        }

        if (importador.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            necesitaReimport = true;
        }

        if (!importador.preserveHierarchy)
        {
            importador.preserveHierarchy = true;
            necesitaReimport = true;
        }

        if (importador.optimizeBones)
        {
            importador.optimizeBones = false;
            necesitaReimport = true;
        }

        if (necesitaReimport)
        {
            importador.SaveAndReimport();
            Debug.Log("[SetupAnimatorJugador] Modelo principal del paladín reimportado como Humanoid.");
        }
    }

    private static void AgregarParametros(AnimatorController controller)
    {
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Plantado", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackNormal", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackFuerte", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Parry", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
    }

    private static AnimationClip ObtenerClip(string rutaFbx)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rutaFbx);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[SetupAnimatorJugador] No se encontraron assets en " + rutaFbx);
            return null;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__"))
            {
                return clip;
            }
        }

        Debug.LogWarning("[SetupAnimatorJugador] No se encontró clip en " + rutaFbx);
        return null;
    }

    private static void ConfigurarImportadorClip(string rutaFbx, bool loop, Avatar avatarModelo)
    {
        ModelImporter importador = AssetImporter.GetAtPath(rutaFbx) as ModelImporter;
        if (importador == null)
        {
            Debug.LogWarning("[SetupAnimatorJugador] No se encontró importer para " + rutaFbx);
            return;
        }

        bool necesitaReimport = false;

        if (importador.animationType != ModelImporterAnimationType.Human)
        {
            importador.animationType = ModelImporterAnimationType.Human;
            necesitaReimport = true;
        }

        if (avatarModelo != null && importador.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
        {
            importador.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            necesitaReimport = true;
        }

        if (avatarModelo != null && importador.sourceAvatar != avatarModelo)
        {
            importador.sourceAvatar = avatarModelo;
            necesitaReimport = true;
        }

        ModelImporterClipAnimation[] clips = importador.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importador.defaultClipAnimations;
        }

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        ModelImporterClipAnimation[] clipsConfigurados = new ModelImporterClipAnimation[clips.Length];
        for (int i = 0; i < clips.Length; i++)
        {
            ModelImporterClipAnimation clipConfigurado = clips[i];
            clipConfigurado.loopTime = loop;
            clipConfigurado.loopPose = loop;
            clipConfigurado.keepOriginalPositionY = true;
            clipsConfigurados[i] = clipConfigurado;
        }

        importador.clipAnimations = clipsConfigurados;
        necesitaReimport = true;

        if (necesitaReimport)
        {
            importador.SaveAndReimport();
        }
    }

    private static void ConfigurarEventoImpacto(AnimationClip clip, float factorTiempo, string nombreFuncion)
    {
        if (clip == null)
        {
            return;
        }

        AnimationEvent evento = new AnimationEvent();
        evento.time = clip.length * factorTiempo;
        evento.functionName = nombreFuncion;
        AnimationUtility.SetAnimationEvents(clip, new[] { evento });
        EditorUtility.SetDirty(clip);
    }

    private static void RegistrarFaltante(AnimationClip clip, string ruta, List<string> faltantes)
    {
        if (clip == null)
        {
            faltantes.Add(ruta);
        }
    }

    private static Avatar ObtenerAvatarModeloPaladin()
    {
        const string rutaModelo = "Assets/Personaje_Paladin/Modelo/Paladin WProp J Nordstrom.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rutaModelo);
        if (assets == null || assets.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            Avatar avatar = assets[i] as Avatar;
            if (avatar != null)
            {
                return avatar;
            }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        if (prefab != null)
        {
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.avatar != null)
            {
                return animator.avatar;
            }
        }

        Debug.LogWarning("[SetupAnimatorJugador] No se encontró Avatar util dentro del FBX del paladín.");
        return null;
    }

    private static AnimatorState CrearEstado(AnimatorStateMachine maquina, string nombre, Motion motion, Vector3 posicion)
    {
        AnimatorState estado = maquina.AddState(nombre, posicion);
        estado.motion = motion;
        estado.writeDefaultValues = true;
        return estado;
    }

    private static AnimatorStateTransition CrearTransicion(
        AnimatorState origen,
        AnimatorState destino,
        bool hasExitTime,
        float exitTime,
        float duration,
        AnimatorConditionMode modo = AnimatorConditionMode.If,
        float threshold = 0f,
        string parametro = null)
    {
        AnimatorStateTransition transicion = origen.AddTransition(destino);
        transicion.hasExitTime = hasExitTime;
        transicion.exitTime = exitTime;
        transicion.hasFixedDuration = true;
        transicion.duration = duration;
        transicion.canTransitionToSelf = false;

        if (!string.IsNullOrEmpty(parametro))
        {
            transicion.AddCondition(modo, threshold, parametro);
        }

        return transicion;
    }

    private static void CrearTransicionDesdeAnyState(AnimatorStateMachine maquina, AnimatorState destino, float duration, string trigger)
    {
        AnimatorStateTransition transicion = maquina.AddAnyStateTransition(destino);
        transicion.hasExitTime = false;
        transicion.hasFixedDuration = true;
        transicion.duration = duration;
        transicion.canTransitionToSelf = false;
        transicion.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AsignarControllerEnEscena(RuntimeAnimatorController controller)
    {
        GameObject jugador = GameObject.Find("Jugador");
        if (jugador == null)
        {
            Debug.LogWarning("[SetupAnimatorJugador] No se encontró 'Jugador' en la escena. El controller se creó pero no se pudo asignar.");
            return;
        }

        Transform modelo = BuscarModeloJugador(jugador.transform);
        Animator animatorVisual = null;
        if (modelo != null)
        {
            animatorVisual = modelo.GetComponent<Animator>();
        }

        if (animatorVisual == null)
        {
            animatorVisual = BuscarAnimatorVisualJugador(modelo != null ? modelo : jugador.transform);
        }

        if (animatorVisual == null)
        {
            Debug.LogWarning("[SetupAnimatorJugador] No se encontró Animator visual en el jugador.");
            return;
        }

        animatorVisual.runtimeAnimatorController = controller;
        if (animatorVisual.avatar == null)
        {
            Animator animatorPrefab = PrefabUtility.GetCorrespondingObjectFromSource(animatorVisual) as Animator;
            if (animatorPrefab != null && animatorPrefab.avatar != null)
            {
                animatorVisual.avatar = animatorPrefab.avatar;
            }
        }

        animatorVisual.applyRootMotion = false;
        animatorVisual.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animatorVisual.updateMode = AnimatorUpdateMode.Normal;

        ControladorAnimacionJugador controlador = animatorVisual.GetComponent<ControladorAnimacionJugador>();
        if (controlador == null)
        {
            controlador = animatorVisual.gameObject.AddComponent<ControladorAnimacionJugador>();
            Debug.Log("[SetupAnimatorJugador] Se agregó ControladorAnimacionJugador al Animator visual.");
        }

        SistemaDefensaEspada sistemaDefensa = jugador.GetComponent<SistemaDefensaEspada>();
        if (sistemaDefensa == null)
        {
            sistemaDefensa = jugador.AddComponent<SistemaDefensaEspada>();
            Debug.Log("[SetupAnimatorJugador] Se agregó SistemaDefensaEspada al jugador.");
        }

        EditorUtility.SetDirty(animatorVisual);
        EditorUtility.SetDirty(controlador);
        EditorUtility.SetDirty(sistemaDefensa);
        EditorSceneManager.MarkSceneDirty(jugador.scene);
    }

    private static Transform BuscarModeloJugador(Transform raizJugador)
    {
        if (raizJugador == null)
        {
            return null;
        }

        Transform directo = raizJugador.Find("ModeloJugador");
        if (directo != null)
        {
            return directo;
        }

        Transform[] hijos = raizJugador.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i] != null && hijos[i].name == "ModeloJugador")
            {
                return hijos[i];
            }
        }

        return null;
    }

    private static Animator BuscarAnimatorVisualJugador(Transform raizBusqueda)
    {
        if (raizBusqueda == null)
        {
            return null;
        }

        Animator[] animators = raizBusqueda.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
            {
                continue;
            }

            if (animator.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                return animator;
            }
        }

        return raizBusqueda.GetComponentInChildren<Animator>(true);
    }
}
