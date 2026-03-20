using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupPaladinAnimator
{
    private const string RutaController = "Assets/Animations/PaladinAnimator.controller";
    private const string RutaModeloPaladin = "Assets/Personaje_Paladin/Modelo/Paladin WProp J Nordstrom.fbx";

    [MenuItem("Realm Brawl/Setup/Paladin Animator")]
    public static void ConfigurarPaladinAnimator()
    {
        var jugador = GameObject.Find("Jugador");
        if (jugador == null)
        {
            Debug.LogError("[PaladinAnimator] No encontró el objeto Jugador en la escena.");
            EditorUtility.DisplayDialog("Paladin Animator", "No se encontró el objeto Jugador en la escena.", "OK");
            return;
        }

        var animator = jugador.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            SkinnedMeshRenderer smr = jugador.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null)
            {
                Debug.LogError("[PaladinAnimator] No encontró un SkinnedMeshRenderer para agregar Animator.");
                EditorUtility.DisplayDialog("Paladin Animator", "No se encontró un SkinnedMeshRenderer dentro de Jugador.", "OK");
                return;
            }

            animator = smr.gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = smr.gameObject.AddComponent<Animator>();
                Debug.Log("[PaladinAnimator] Se agregó Animator a " + smr.gameObject.name);
            }
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController) != null)
        {
            AssetDatabase.DeleteAsset(RutaController);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(RutaController);
        if (controller == null)
        {
            Debug.LogError("[PaladinAnimator] No pudo crear el controller en " + RutaController);
            EditorUtility.DisplayDialog("Paladin Animator", "No se pudo crear PaladinAnimator.controller.", "OK");
            return;
        }

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackNormal", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackFuerte", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);

        var rootSM = controller.layers[0].stateMachine;

        AnimatorState CrearEstado(string nombre, string archivoFBX, bool loop)
        {
            var state = rootSM.AddState(nombre);
            state.motion = GetClip(archivoFBX);
            if (state.motion != null)
            {
                var clip = state.motion as AnimationClip;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
            return state;
        }

        var stIdle = CrearEstado("Idle", "sword and shield idle.fbx", true);
        var stWalk = CrearEstado("Walk", "sword and shield walk.fbx", true);
        var stRun = CrearEstado("Run", "sword and shield run.fbx", true);
        var stAtkN = CrearEstado("AttackNormal", "sword and shield slash.fbx", false);
        var stAtkF = CrearEstado("AttackFuerte", "sword and shield slash (2).fbx", false);
        var stHit = CrearEstado("Hit", "sword and shield impact.fbx", false);
        var stDeath = CrearEstado("Death", "sword and shield death.fbx", false);
        var stBlock = CrearEstado("Block", "sword and shield block idle.fbx", true);

        stAtkN.speed = 1.3f;
        stAtkF.speed = 0.8f;

        rootSM.defaultState = stIdle;

        var t = stIdle.AddTransition(stWalk);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.duration = 0.15f;
        t.hasExitTime = false;

        t = stWalk.AddTransition(stIdle);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.duration = 0.15f;
        t.hasExitTime = false;

        t = stWalk.AddTransition(stRun);
        t.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsSprinting");
        t.duration = 0.15f;
        t.hasExitTime = false;

        t = stRun.AddTransition(stWalk);
        t.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");
        t.duration = 0.15f;
        t.hasExitTime = false;

        var atN = rootSM.AddAnyStateTransition(stAtkN);
        atN.AddCondition(AnimatorConditionMode.If, 0f, "AttackNormal");
        atN.duration = 0.08f;
        atN.hasExitTime = false;

        var atF = rootSM.AddAnyStateTransition(stAtkF);
        atF.AddCondition(AnimatorConditionMode.If, 0f, "AttackFuerte");
        atF.duration = 0.12f;
        atF.hasExitTime = false;

        var th = rootSM.AddAnyStateTransition(stHit);
        th.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
        th.duration = 0.05f;
        th.hasExitTime = false;

        var td = rootSM.AddAnyStateTransition(stDeath);
        td.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        td.duration = 0.1f;
        td.hasExitTime = false;

        t = stIdle.AddTransition(stBlock);
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsBlocking");
        t.duration = 0.1f;
        t.hasExitTime = false;

        t = stBlock.AddTransition(stIdle);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBlocking");
        t.duration = 0.1f;
        t.hasExitTime = false;

        t = stAtkN.AddTransition(stIdle);
        t.hasExitTime = true;
        t.exitTime = 0.85f;
        t.duration = 0.15f;

        t = stAtkF.AddTransition(stIdle);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.15f;

        t = stHit.AddTransition(stIdle);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.1f;

        Avatar avatarPaladin = ObtenerAvatarPaladin();
        if (animator.avatar == null && avatarPaladin != null)
        {
            animator.avatar = avatarPaladin;
            Debug.Log("[PaladinAnimator] Se asignó Avatar del paladín a " + animator.gameObject.name);
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PaladinAnimator] PaladinAnimator creado y asignado al Jugador.");
        EditorUtility.DisplayDialog("Listo", "PaladinAnimator creado y asignado al Jugador.", "OK");
    }

    private static Avatar ObtenerAvatarPaladin()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RutaModeloPaladin);
        for (int i = 0; i < assets.Length; i++)
        {
            Avatar avatar = assets[i] as Avatar;
            if (avatar != null)
            {
                return avatar;
            }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloPaladin);
        if (prefab != null)
        {
            Animator animatorPrefab = prefab.GetComponentInChildren<Animator>(true);
            if (animatorPrefab != null && animatorPrefab.avatar != null)
            {
                return animatorPrefab.avatar;
            }
        }

        Debug.LogWarning("[PaladinAnimator] No encontró Avatar en " + RutaModeloPaladin);
        return null;
    }

    private static AnimationClip GetClip(string nombre)
    {
        string ruta = "Assets/Personaje_Paladin/Animaciones/" + nombre;
        var assets = AssetDatabase.LoadAllAssetsAtPath(ruta);
        foreach (var a in assets)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__"))
            {
                return c;
            }
        }
        Debug.LogWarning("[PaladinAnimator] No encontró clip: " + ruta);
        return null;
    }
}
