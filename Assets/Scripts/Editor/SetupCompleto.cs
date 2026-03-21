#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using RealmBrawl;

public class SetupCompleto : EditorWindow
{
    [MenuItem("Realm Brawl/>>> SETUP COMPLETO <<<", priority = 0)]
    static void SetupTodo()
    {
        if (!EditorUtility.DisplayDialog("Setup Completo",
            "Esto va a configurar toda la escena desde cero:\n\n" +
            "1. Limpiar escena\n" +
            "2. Crear arena\n" +
            "3. Configurar jugador con Paladin\n" +
            "4. Configurar enemigos\n" +
            "5. Configurar sistemas (oleadas, XP, etc)\n" +
            "6. Configurar UI\n" +
            "7. Bake NavMesh\n\n" +
            "Continuar?", "SI, DALE!", "Cancelar"))
            return;

        // Paso 0: Asegurar que existan los tags necesarios
        CrearTagSiNoExiste("Ground");
        CrearTagSiNoExiste("Jugador");
        CrearTagSiNoExiste("Enemigo");

        // Paso 1: Limpiar objetos viejos
        EditorUtility.DisplayProgressBar("Setup", "Limpiando escena...", 0.05f);
        LimpiarEscena();

        // Paso 2: Luz
        EditorUtility.DisplayProgressBar("Setup", "Configurando iluminacion...", 0.1f);
        ConfigurarLuz();

        // Paso 2.5: Arreglar materiales de arboles (shader correcto)
        EditorUtility.DisplayProgressBar("Setup", "Arreglando materiales de vegetacion...", 0.15f);
        ArreglarMaterialesVegetacion();

        // Paso 3: Arena
        EditorUtility.DisplayProgressBar("Setup", "Generando arena...", 0.2f);
        var arena = CrearArena();

        // Paso 4: Jugador
        EditorUtility.DisplayProgressBar("Setup", "Configurando jugador...", 0.4f);
        var jugador = CrearJugador();

        // Paso 5: Camara
        EditorUtility.DisplayProgressBar("Setup", "Configurando camara...", 0.5f);
        CrearCamara(jugador);

        // Paso 6: Prefab enemigo
        EditorUtility.DisplayProgressBar("Setup", "Creando prefab enemigo...", 0.6f);
        var prefabEnemigo = CrearPrefabEnemigo();

        // Paso 7: GameManager con sistemas
        EditorUtility.DisplayProgressBar("Setup", "Configurando sistemas...", 0.7f);
        CrearSistemas(prefabEnemigo, arena);

        // Paso 8: UI
        EditorUtility.DisplayProgressBar("Setup", "Configurando UI...", 0.8f);
        CrearUI();

        // Paso 9: NavMesh
        EditorUtility.DisplayProgressBar("Setup", "Baking NavMesh...", 0.9f);
        BakeNavMesh();

        // Paso 10: Pocion ScriptableObject
        EditorUtility.DisplayProgressBar("Setup", "Creando items...", 0.95f);
        CrearItemPocion(jugador);

        EditorUtility.ClearProgressBar();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Completo!",
            "Todo listo! Dale Play para probar.\n\n" +
            "Controles:\n" +
            "- WASD: Mover\n" +
            "- Shift: Correr\n" +
            "- Click izq: Ataque normal\n" +
            "- Click der: Ataque fuerte\n" +
            "- Q: Cambiar hombro de camara\n" +
            "- 1: Usar pocion\n" +
            "- Esc: Reiniciar (en Game Over)", "Genial!");
    }

    static void LimpiarEscena()
    {
        // Borrar todo excepto la luz
        var todos = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in todos)
        {
            if (obj == null) continue;
            if (obj.GetComponent<Light>() != null && obj.GetComponent<Light>().type == LightType.Directional) continue;
            if (obj.transform.parent != null) continue; // solo raiz
            if (obj.name == "EventSystem") continue;
            Object.DestroyImmediate(obj);
        }
    }

    static void ConfigurarLuz()
    {
        var luz = Object.FindObjectOfType<Light>();
        if (luz == null)
        {
            var luzObj = new GameObject("Directional Light");
            luz = luzObj.AddComponent<Light>();
            luz.type = LightType.Directional;
        }

        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        luz.color = new Color(1f, 0.85f, 0.7f);
        luz.intensity = 1.2f;

        RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.35f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.015f;
        RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.8f);
    }

    static GameObject CrearArena()
    {
        var arenaObj = new GameObject("Arena");
        var gen = arenaObj.AddComponent<GeneradorArena>();

        // Buscar assets de Polytope
        var arboles = BuscarPrefabs("Lowpoly_Environments", "Tree");
        var rocas = BuscarPrefabs("Lowpoly_Environments", "Rock");
        var arbustos = BuscarPrefabs("Lowpoly_Environments", "Shrub");

        Material matSuelo = BuscarMaterialSuelo();
        gen.AsignarRecursos(matSuelo, arboles, rocas, arbustos);
        gen.Generar();

        return arenaObj;
    }

    static GameObject CrearJugador()
    {
        var jugador = new GameObject("Jugador");
        jugador.tag = "Player";
        jugador.layer = LayerMask.NameToLayer("Default");
        jugador.transform.position = new Vector3(0, 0.1f, 0);

        // Rigidbody y collider (los agrega RequireComponent)
        var rb = jugador.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var col = jugador.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0, 0.9f, 0);
        col.radius = 0.35f;

        // Buscar el modelo del paladin
        string[] paladinGUIDs = AssetDatabase.FindAssets("Paladin WProp J Nordstrom t:Model",
            new[] { "Assets/Personaje_Paladin/Modelo" });

        if (paladinGUIDs.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(paladinGUIDs[0]);
            var paladinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (paladinPrefab != null)
            {
                var modelo = (GameObject)PrefabUtility.InstantiatePrefab(paladinPrefab);
                modelo.name = "Modelo";
                modelo.transform.SetParent(jugador.transform);
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;

                // Configurar Animator
                var animator = modelo.GetComponent<Animator>();
                if (animator == null) animator = modelo.AddComponent<Animator>();

                var controller = CrearAnimatorJugador();
                if (controller != null)
                    animator.runtimeAnimatorController = controller;

                // Receptor de Animation Events (OnImpactoNormal / OnImpactoFuerte)
                // Debe estar en el mismo GameObject que el Animator
                modelo.AddComponent<ReceptorAnimacionJugador>();
            }
        }
        else
        {
            // Fallback: cubo placeholder
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "Modelo";
            placeholder.transform.SetParent(jugador.transform);
            placeholder.transform.localPosition = new Vector3(0, 1f, 0);
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            Debug.LogWarning("No se encontro el modelo del Paladin en Assets/Personaje_Paladin/Modelo/");
        }

        // Punto de hitbox para la espada
        var hitbox = new GameObject("PuntoHitbox");
        hitbox.transform.SetParent(jugador.transform);
        hitbox.transform.localPosition = new Vector3(0, 1f, 1.2f);

        // Agregar componentes del jugador
        jugador.AddComponent<MovimientoJugador>();
        jugador.AddComponent<VidaJugador>();
        jugador.AddComponent<Estamina>();
        var combate = jugador.AddComponent<CombateCaballero>();
        jugador.AddComponent<DodgeRoll>();
        jugador.AddComponent<Inventario>();

        // Asignar punto hitbox al sistema de combate
        var so = new SerializedObject(combate);
        var propHitbox = so.FindProperty("puntoHitbox");
        if (propHitbox != null)
        {
            propHitbox.objectReferenceValue = hitbox.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Configurar layer mask para enemigos
        var propMascara = so.FindProperty("mascaraEnemigos");
        if (propMascara != null)
        {
            propMascara.intValue = ~0; // todas las capas
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return jugador;
    }

    static AnimatorController CrearAnimatorJugador()
    {
        string dir = "Assets/Animaciones/JugadorController";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string controllerPath = dir + "/JugadorAnimator.controller";

        // Siempre regenerar para garantizar que los parametros esten actualizados
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh(); // limpiar cache antes de crear el nuevo
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parametros
        controller.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        controller.AddParameter("Sprint", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AtaqueTrigger0", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AtaqueTrigger1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AtaqueTrigger2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AtaqueFuerteTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RollTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RecibirDanio", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Morir", AnimatorControllerParameterType.Trigger);

        var rootSM = controller.layers[0].stateMachine;

        // Buscar clips de animacion del Sword and Shield Pack
        var clips = BuscarClipsAnimacion();

        // Crear estados
        var idleState = rootSM.AddState("Idle", new Vector3(0, 0, 0));
        var walkState = rootSM.AddState("Walk", new Vector3(250, -50, 0));
        var runState = rootSM.AddState("Run", new Vector3(250, 50, 0));
        var combo0State = rootSM.AddState("Combo0", new Vector3(500, -150, 0));
        var combo1State = rootSM.AddState("Combo1", new Vector3(500, -50, 0));
        var combo2State = rootSM.AddState("Combo2", new Vector3(500, 50, 0));
        var fuertState = rootSM.AddState("AtaqueFuerte", new Vector3(500, 150, 0));
        var rollState = rootSM.AddState("Roll", new Vector3(500, 250, 0));
        var hitState = rootSM.AddState("RecibirDanio", new Vector3(500, 350, 0));
        var deathState = rootSM.AddState("Morir", new Vector3(250, 450, 0));

        rootSM.defaultState = idleState;

        // Asignar clips
        if (clips.ContainsKey("idle")) idleState.motion = clips["idle"];
        if (clips.ContainsKey("walk")) walkState.motion = clips["walk"];
        if (clips.ContainsKey("run")) runState.motion = clips["run"];
        // Combo0: primer clip de attack o slash
        if (clips.ContainsKey("attack")) combo0State.motion = clips["attack"];
        else if (clips.ContainsKey("slash")) combo0State.motion = clips["slash"];
        // Combo1: attack(2) o slash(2)
        if (clips.ContainsKey("attack(2)")) combo1State.motion = clips["attack(2)"];
        else if (clips.ContainsKey("slash(2)")) combo1State.motion = clips["slash(2)"];
        // Combo2: attack(3) o slash(3)
        if (clips.ContainsKey("attack(3)")) combo2State.motion = clips["attack(3)"];
        else if (clips.ContainsKey("slash(3)")) combo2State.motion = clips["slash(3)"];
        // Fuerte: power up o attack(4)
        if (clips.ContainsKey("power up")) fuertState.motion = clips["power up"];
        else if (clips.ContainsKey("attack(4)")) fuertState.motion = clips["attack(4)"];
        // Roll: roll si existe, sino jump
        if (clips.ContainsKey("roll")) rollState.motion = clips["roll"];
        else if (clips.ContainsKey("jump")) rollState.motion = clips["jump"];
        if (clips.ContainsKey("impact")) hitState.motion = clips["impact"];
        if (clips.ContainsKey("death")) deathState.motion = clips["death"];

        // Transiciones: Idle <-> Walk
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.15f;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.15f;

        // Walk <-> Run
        var walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(AnimatorConditionMode.If, 0, "Sprint");
        walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.15f;

        var runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "Sprint");
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.15f;

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;

        // AnyState -> Ataques combo y especiales (triggers)
        // canTransitionToSelf=false evita que el mismo estado se interrumpa a si mismo
        var anyToCombo0 = rootSM.AddAnyStateTransition(combo0State);
        anyToCombo0.AddCondition(AnimatorConditionMode.If, 0, "AtaqueTrigger0");
        anyToCombo0.hasExitTime = false;
        anyToCombo0.duration = 0.05f;
        anyToCombo0.canTransitionToSelf = false;

        var anyToCombo1 = rootSM.AddAnyStateTransition(combo1State);
        anyToCombo1.AddCondition(AnimatorConditionMode.If, 0, "AtaqueTrigger1");
        anyToCombo1.hasExitTime = false;
        anyToCombo1.duration = 0.05f;
        anyToCombo1.canTransitionToSelf = false;

        var anyToCombo2 = rootSM.AddAnyStateTransition(combo2State);
        anyToCombo2.AddCondition(AnimatorConditionMode.If, 0, "AtaqueTrigger2");
        anyToCombo2.hasExitTime = false;
        anyToCombo2.duration = 0.05f;
        anyToCombo2.canTransitionToSelf = false;

        var anyToFuert = rootSM.AddAnyStateTransition(fuertState);
        anyToFuert.AddCondition(AnimatorConditionMode.If, 0, "AtaqueFuerteTrigger");
        anyToFuert.hasExitTime = false;
        anyToFuert.duration = 0.05f;
        anyToFuert.canTransitionToSelf = false;

        var anyToRoll = rootSM.AddAnyStateTransition(rollState);
        anyToRoll.AddCondition(AnimatorConditionMode.If, 0, "RollTrigger");
        anyToRoll.hasExitTime = false;
        anyToRoll.duration = 0.05f;
        anyToRoll.canTransitionToSelf = false;

        // Ataques -> Idle (por exit time)
        var combo0ToIdle = combo0State.AddTransition(idleState);
        combo0ToIdle.hasExitTime = true;
        combo0ToIdle.exitTime = 0.85f;
        combo0ToIdle.duration = 0.15f;

        var combo1ToIdle = combo1State.AddTransition(idleState);
        combo1ToIdle.hasExitTime = true;
        combo1ToIdle.exitTime = 0.85f;
        combo1ToIdle.duration = 0.15f;

        var combo2ToIdle = combo2State.AddTransition(idleState);
        combo2ToIdle.hasExitTime = true;
        combo2ToIdle.exitTime = 0.85f;
        combo2ToIdle.duration = 0.15f;

        var fuertToIdle = fuertState.AddTransition(idleState);
        fuertToIdle.hasExitTime = true;
        fuertToIdle.exitTime = 0.85f;
        fuertToIdle.duration = 0.15f;

        var rollToIdle = rollState.AddTransition(idleState);
        rollToIdle.hasExitTime = true;
        rollToIdle.exitTime = 0.85f;
        rollToIdle.duration = 0.15f;

        // AnyState -> Danio
        var anyToHit = rootSM.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "RecibirDanio");
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0.1f;

        var hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.8f;
        hitToIdle.duration = 0.2f;

        // AnyState -> Muerte
        var anyToDeath = rootSM.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Morir");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.15f;

        AssetDatabase.SaveAssets();
        return controller;
    }

    static Dictionary<string, AnimationClip> BuscarClipsAnimacion()
    {
        var clips = new Dictionary<string, AnimationClip>();
        string animDir = "Assets/Personaje_Paladin/Animaciones";

        // Mapeo de nombres de archivo a nombres internos
        var mapeo = new Dictionary<string, string>
        {
            { "sword and shield idle", "idle" },
            { "sword and shield walk", "walk" },
            { "sword and shield run", "run" },
            { "sword and shield slash", "slash" },
            { "sword and shield attack", "attack" },
            { "sword and shield impact", "impact" },
            { "sword and shield death", "death" },
        };

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { animDir });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();

            foreach (var kv in mapeo)
            {
                // Usar el primer match sin numero (ej: "sword and shield idle" no "idle (2)")
                if (fileName == kv.Key && !clips.ContainsKey(kv.Value))
                {
                    // Extraer clip del FBX
                    var allClips = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in allClips)
                    {
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                        {
                            clips[kv.Value] = clip;
                            break;
                        }
                    }
                }
            }
        }

        if (clips.Count == 0)
            Debug.LogWarning("No se encontraron clips de animacion en " + animDir);
        else
            Debug.Log($"Se encontraron {clips.Count} clips de animacion: {string.Join(", ", clips.Keys)}");

        return clips;
    }

    static void CrearCamara(GameObject jugador)
    {
        // Reutilizar Main Camera si existe, sino crear
        var camObj = Camera.main != null ? Camera.main.gameObject : null;
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        // Limpiar componentes viejos
        var oldCam = camObj.GetComponent<CamaraTerceraPersona>();
        if (oldCam != null) Object.DestroyImmediate(oldCam);

        var cam = camObj.AddComponent<CamaraTerceraPersona>();
        cam.AsignarObjetivo(jugador.transform);

        camObj.transform.position = jugador.transform.position + new Vector3(0, 3, -5);
    }

    static GameObject CrearPrefabEnemigo()
    {
        string prefabDir = "Assets/Prefabs";
        if (!Directory.Exists(prefabDir))
            Directory.CreateDirectory(prefabDir);

        string prefabPath = prefabDir + "/Enemigo_Esqueleto.prefab";

        // Crear enemigo temporal en escena
        var enemigo = new GameObject("Enemigo_Template");

        // Buscar modelo esqueleto
        string[] skelGuids = AssetDatabase.FindAssets("PT_Male_Armors_Skeleton_Modular t:Prefab",
            new[] { "Assets/Polytope Studio" });

        if (skelGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(skelGuids[0]);
            var skelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (skelPrefab != null)
            {
                var modelo = (GameObject)PrefabUtility.InstantiatePrefab(skelPrefab);
                modelo.name = "Modelo";
                modelo.transform.SetParent(enemigo.transform);
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;

                // Configurar Animator con el controller existente si hay
                var animator = modelo.GetComponent<Animator>();
                if (animator == null) animator = modelo.AddComponent<Animator>();

                var enemigoController = BuscarOCrearControllerEnemigo();
                if (enemigoController != null)
                    animator.runtimeAnimatorController = enemigoController;
            }
        }
        else
        {
            // Fallback: cubo rojo
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "Modelo";
            placeholder.transform.SetParent(enemigo.transform);
            placeholder.transform.localPosition = new Vector3(0, 1f, 0);
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            placeholder.GetComponent<Renderer>().sharedMaterial = mat;
            Debug.LogWarning("No se encontro el esqueleto de Polytope Studio");
        }

        // Componentes
        var agent = enemigo.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1.8f;
        agent.radius = 0.4f;
        agent.height = 1.8f;

        var col = enemigo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0, 0.9f, 0);
        col.radius = 0.4f;

        enemigo.AddComponent<EnemigoBase>();
        enemigo.AddComponent<BarraVidaEnemigo>();

        // Guardar como prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(enemigo, prefabPath);
        Object.DestroyImmediate(enemigo);

        Debug.Log("Prefab enemigo creado en: " + prefabPath);
        return prefab;
    }

    static RuntimeAnimatorController BuscarOCrearControllerEnemigo()
    {
        // Buscar controller existente
        string[] guids = AssetDatabase.FindAssets("EnemigoController t:AnimatorController");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // Crear uno basico
        string dir = "Assets/Animaciones/EnemigoController";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/EnemigoAnimator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        controller.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RecibirDanio", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Morir", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        var idle = sm.AddState("Idle", new Vector3(0, 0, 0));
        var walk = sm.AddState("Caminar", new Vector3(250, 0, 0));
        var atk = sm.AddState("Atacar", new Vector3(250, 100, 0));
        var hit = sm.AddState("Danio", new Vector3(250, 200, 0));
        var die = sm.AddState("Morir", new Vector3(250, 300, 0));

        sm.defaultState = idle;

        var idleToWalk = idle.AddTransition(walk);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.2f;

        var walkToIdle = walk.AddTransition(idle);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.2f;

        var anyToAtk = sm.AddAnyStateTransition(atk);
        anyToAtk.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        anyToAtk.hasExitTime = false;
        anyToAtk.duration = 0.1f;

        var atkToIdle = atk.AddTransition(idle);
        atkToIdle.hasExitTime = true;
        atkToIdle.exitTime = 0.9f;
        atkToIdle.duration = 0.1f;

        var anyToHit = sm.AddAnyStateTransition(hit);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "RecibirDanio");
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0.1f;

        var hitToIdle = hit.AddTransition(idle);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.8f;
        hitToIdle.duration = 0.2f;

        var anyToDie = sm.AddAnyStateTransition(die);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Morir");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.15f;

        AssetDatabase.SaveAssets();
        return controller;
    }

    static void CrearSistemas(GameObject prefabEnemigo, GameObject arena)
    {
        var gmObj = new GameObject("GameManager");

        var gm = gmObj.AddComponent<GameManager>();
        var oleadas = gmObj.AddComponent<SistemaOleadas>();
        gmObj.AddComponent<SistemaXP>();
        gmObj.AddComponent<SistemaRachas>();
        gmObj.AddComponent<EstadoJugadorController>();

        // Configurar oleadas con prefab y spawn points
        var so = new SerializedObject(oleadas);
        var propPrefab = so.FindProperty("prefabEnemigo");
        if (propPrefab != null)
        {
            propPrefab.objectReferenceValue = prefabEnemigo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Buscar spawn points de la arena
        var genArena = arena.GetComponent<GeneradorArena>();
        if (genArena != null && genArena.PuntosSpawn.Count > 0)
        {
            var propSpawns = so.FindProperty("puntosSpawn");
            if (propSpawns != null)
            {
                propSpawns.arraySize = genArena.PuntosSpawn.Count;
                for (int i = 0; i < genArena.PuntosSpawn.Count; i++)
                    propSpawns.GetArrayElementAtIndex(i).objectReferenceValue = genArena.PuntosSpawn[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    static void CrearUI()
    {
        var uiObj = new GameObject("UI_Sistemas");
        uiObj.AddComponent<HUDJugador>();
        uiObj.AddComponent<PantallaGameOver>();
        uiObj.AddComponent<DanioFlotante>();
        uiObj.AddComponent<FeedbackPantalla>();
    }

    static void BakeNavMesh()
    {
        try
        {
            // Buscar suelo por tag o nombre
            GameObject suelo = null;
            try { suelo = GameObject.FindGameObjectWithTag("Ground"); } catch { }
            if (suelo == null) suelo = GameObject.Find("Suelo");

            if (suelo != null)
            {
                if (suelo.GetComponent<Collider>() == null)
                    suelo.AddComponent<MeshCollider>();
                suelo.isStatic = true;
            }

            // CRITICO: guardar la escena antes de bakear.
            // Unity necesita un archivo .unity en disco para persistir el NavMesh.
            var escena = SceneManager.GetActiveScene();
            string rutaEscena = escena.path;
            if (string.IsNullOrEmpty(rutaEscena))
            {
                // Escena sin guardar (Untitled) → guardar como ArenaGame
                rutaEscena = "Assets/Scenes/ArenaGame.unity";
                if (!Directory.Exists("Assets/Scenes"))
                    Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(escena, rutaEscena);
                Debug.Log("Escena guardada como: " + rutaEscena);
            }
            else
            {
                EditorSceneManager.SaveScene(escena);
            }

            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("NavMesh baked OK!");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("NavMesh bake fallo (no es critico): " + e.Message);
        }
    }

    static void CrearItemPocion(GameObject jugador)
    {
        string itemDir = "Assets/Items";
        if (!Directory.Exists(itemDir))
            Directory.CreateDirectory(itemDir);

        string pocionPath = itemDir + "/Pocion_Vida.asset";
        var existente = AssetDatabase.LoadAssetAtPath<ItemData>(pocionPath);
        if (existente == null)
        {
            var pocion = ScriptableObject.CreateInstance<ItemData>();
            pocion.nombre = "Pocion de Vida";
            pocion.tipo = ItemData.TipoItem.Pocion;
            pocion.valor = 35f;
            pocion.cantidadMaxima = 5;
            AssetDatabase.CreateAsset(pocion, pocionPath);
            AssetDatabase.SaveAssets();
            existente = pocion;
        }

        // Dar 3 pociones al jugador en Start via un helper
        var inv = jugador.GetComponent<Inventario>();
        if (inv != null)
        {
            // Crear un componente helper que da pociones al inicio
            var helper = jugador.AddComponent<DarItemsIniciales>();
            var so = new SerializedObject(helper);
            var propItem = so.FindProperty("pocion");
            if (propItem != null)
            {
                propItem.objectReferenceValue = existente;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    // --- Utilidades ---

    static void ArreglarMaterialesVegetacion()
    {
        // Buscar el shader correcto de Polytope para hojas
        var shaderFoliage = Shader.Find("PT_Vegetation_Foliage_Shader");
        if (shaderFoliage == null)
        {
            // Buscar por archivo
            string[] shaderGuids = AssetDatabase.FindAssets("PT_Vegetation_Foliage_Shader t:Shader");
            if (shaderGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderGuids[0]);
                var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shaderAsset != null) shaderFoliage = shaderAsset;
            }
        }

        if (shaderFoliage == null)
        {
            Debug.LogWarning("No se encontro PT_Vegetation_Foliage_Shader - los arboles pueden verse oscuros");
            return;
        }

        // Materiales de hojas que deben usar el shader de foliage
        string[] nombresHojas = {
            "PT_Pine_Tree_Leaves_Mat",
            "PT_Generic_Leaf_mat",
            "PT_Generic_Tree_Leaves_mat",
            "PT_Fruit_Tree_Foliage_Mat"
        };

        foreach (string nombre in nombresHojas)
        {
            string[] guids = AssetDatabase.FindAssets(nombre + " t:Material",
                new[] { "Assets/Polytope Studio" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != shaderFoliage)
                {
                    mat.shader = shaderFoliage;
                    EditorUtility.SetDirty(mat);
                    Debug.Log($"Material '{mat.name}' arreglado -> shader Foliage (double-sided)");
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Realm Brawl/Configurar/Arreglar Materiales Arboles")]
    static void ArreglarMaterialesMenu()
    {
        ArreglarMaterialesVegetacion();
        Debug.Log("Materiales de vegetacion arreglados!");
    }

    static void CrearTagSiNoExiste(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tagsProp = tagManager.FindProperty("tags");

        // Verificar si ya existe
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return; // Ya existe
        }

        // Agregar tag nuevo
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"Tag '{tag}' creado exitosamente");
    }

    static GameObject[] BuscarPrefabs(string carpeta, string filtro)
    {
        var resultado = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets(filtro + " t:Prefab",
            new[] { "Assets/Polytope Studio/" + carpeta });

        // Si no hay prefabs, buscar modelos FBX
        if (guids.Length == 0)
            guids = AssetDatabase.FindAssets(filtro + " t:Model",
                new[] { "Assets/Polytope Studio/" + carpeta });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj != null)
                resultado.Add(obj);
        }

        return resultado.ToArray();
    }

    static Material BuscarMaterialSuelo()
    {
        // Buscar materiales con nombres de piedra/tierra en la carpeta de Polytope
        string[] filtrosPiedra = { "Stone", "Rock", "Ground", "Dirt", "Cobble", "Gray" };
        foreach (string filtro in filtrosPiedra)
        {
            string[] guids = AssetDatabase.FindAssets(filtro + " t:Material",
                new[] { "Assets/Polytope Studio" });
            if (guids.Length > 0)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (mat != null)
                {
                    Debug.Log($"Material de suelo encontrado: {mat.name}");
                    return mat;
                }
            }
        }

        // Fallback: crear material gris oscuro estilo piedra
        var matPiedra = new Material(Shader.Find("Standard"));
        matPiedra.color = new Color(0.45f, 0.42f, 0.40f);
        matPiedra.name = "ArenaStone";
        Debug.LogWarning("No se encontro material de piedra en Polytope Studio - usando ArenaStone gris generado");
        return matPiedra;
    }

    static Material BuscarMaterial(string filtro)
    {
        string[] guids = AssetDatabase.FindAssets(filtro + " t:Material",
            new[] { "Assets/Polytope Studio" });

        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // Fallback: crear material verde basico
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.5f, 0.2f);
        return mat;
    }
}
#endif
