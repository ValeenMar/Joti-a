using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupPersonajeJugador
{
    private const string RutaModeloPaladin = "Assets/Personaje_Paladin/Modelo/Paladin WProp J Nordstrom.fbx";
    private const string RutaControllerJugador = "Assets/Animations/JugadorAnimator.controller";
    private const string NombreJugador = "Jugador";
    private const string NombreModeloJugador = "ModeloJugador";
    private const string NombreContenedorVisualViejo = "VisualJugador";
    private const float AlturaObjetivoModelo = 1.8f;

    // Estos offsets quedan documentados para tunearlos facil si el modelo cambia.
    private static readonly Vector3 OffsetModelo = Vector3.zero;
    private static readonly Vector3 RotacionModelo = Vector3.zero;

    [MenuItem("Realm Brawl/Setup/Personaje Jugador 3D")]
    public static void ConfigurarPersonajeJugador3D()
    {
        GameObject jugador = GameObject.Find(NombreJugador);
        if (jugador == null)
        {
            Debug.LogError("[SetupPersonajeJugador] No se encontró el objeto 'Jugador' en la escena.");
            EditorUtility.DisplayDialog("Personaje Jugador", "No se encontró el objeto 'Jugador' en la escena.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloPaladin);
        if (prefab == null)
        {
            Debug.LogError("[SetupPersonajeJugador] No se encontró el modelo del paladín en " + RutaModeloPaladin);
            EditorUtility.DisplayDialog("Personaje Jugador", "No se encontró el modelo del paladín.", "OK");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControllerJugador);
        if (controller == null)
        {
            Debug.LogWarning("[SetupPersonajeJugador] No se encontró JugadorAnimator.controller. Ejecutá antes 'Realm Brawl/Setup/Animator Jugador'.");
        }

        DestruirModelosPrevios(jugador.transform);

        GameObject modelo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (modelo == null)
        {
            Debug.LogError("[SetupPersonajeJugador] No se pudo instanciar el prefab del paladín.");
            EditorUtility.DisplayDialog("Personaje Jugador", "No se pudo instanciar el paladín.", "OK");
            return;
        }

        modelo.name = NombreModeloJugador;
        modelo.transform.SetParent(jugador.transform, false);
        modelo.transform.localPosition = OffsetModelo;
        modelo.transform.localRotation = Quaternion.Euler(RotacionModelo);
        modelo.transform.localScale = Vector3.one;

        AjustarEscalaModelo(modelo.transform, AlturaObjetivoModelo);
        AlinearModeloAlSuelo(modelo.transform);

        Animator animator = PrepararAnimatorPrincipal(modelo);

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        if (animator.avatar == null)
        {
            Avatar avatarModelo = ObtenerAvatarModelo();
            if (avatarModelo != null)
            {
                animator.avatar = avatarModelo;
            }
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        ConfigurarControladorAnimacion(jugador, animator);
        ConfigurarReferenciasSistemaEspada(jugador, modelo.transform);
        AplicarMaterialesYTexturasModelo(modelo.transform);
        AjustarColisionJugador(jugador, modelo);
        DesactivarVisualRaiz(jugador);

        EditorUtility.SetDirty(jugador);
        EditorUtility.SetDirty(modelo);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(jugador.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = jugador;

        Debug.Log("[SetupPersonajeJugador] Paladín configurado como ModeloJugador.");
        EditorUtility.DisplayDialog("Personaje Jugador", "Se reemplazó el modelo del jugador por el paladín y se conectó al controller actual.", "OK");
    }

    private static void DestruirModelosPrevios(Transform raizJugador)
    {
        if (raizJugador == null)
        {
            return;
        }

        List<GameObject> aDestruir = new List<GameObject>();
        Transform[] transforms = raizJugador.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform actual = transforms[i];
            if (actual == null || actual == raizJugador)
            {
                continue;
            }

            if (actual.name == NombreModeloJugador ||
                actual.name == NombreContenedorVisualViejo ||
                actual.name == "SocketEspadaVisual" ||
                actual.name == "EspadaVisual" ||
                actual.name == "EspadaVisualPlaceholder")
            {
                aDestruir.Add(actual.gameObject);
            }
        }

        for (int i = 0; i < aDestruir.Count; i++)
        {
            if (aDestruir[i] != null)
            {
                Object.DestroyImmediate(aDestruir[i]);
            }
        }
    }

    private static void AjustarEscalaModelo(Transform modelo, float alturaObjetivo)
    {
        if (modelo == null)
        {
            return;
        }

        Bounds? boundsModelo = CalcularBoundsRenderers(modelo);
        if (!boundsModelo.HasValue)
        {
            Debug.LogWarning("[SetupPersonajeJugador] No se pudieron calcular bounds del modelo para escalarlo.");
            return;
        }

        float alturaActual = boundsModelo.Value.size.y;
        if (alturaActual <= 0.001f)
        {
            return;
        }

        float factorEscala = alturaObjetivo / alturaActual;
        modelo.localScale = Vector3.one * factorEscala;
    }

    private static void AlinearModeloAlSuelo(Transform modelo)
    {
        if (modelo == null)
        {
            return;
        }

        Bounds? boundsModelo = CalcularBoundsRenderers(modelo);
        if (!boundsModelo.HasValue)
        {
            return;
        }

        float offset = modelo.position.y - boundsModelo.Value.min.y;
        modelo.localPosition += Vector3.up * offset;
    }

    private static Bounds? CalcularBoundsRenderers(Transform raiz)
    {
        Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
        Bounds? bounds = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererActual = renderers[i];
            if (rendererActual == null)
            {
                continue;
            }

            if (!bounds.HasValue)
            {
                bounds = rendererActual.bounds;
            }
            else
            {
                Bounds acumulado = bounds.Value;
                acumulado.Encapsulate(rendererActual.bounds);
                bounds = acumulado;
            }
        }

        return bounds;
    }

    private static Animator BuscarAnimatorVisual(Transform raizModelo)
    {
        if (raizModelo == null)
        {
            return null;
        }

        Animator[] animators = raizModelo.GetComponentsInChildren<Animator>(true);
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

        return raizModelo.GetComponentInChildren<Animator>(true);
    }

    private static Animator PrepararAnimatorPrincipal(GameObject modelo)
    {
        if (modelo == null)
        {
            return null;
        }

        Animator animatorRaiz = modelo.GetComponent<Animator>();
        Animator animatorDetectado = BuscarAnimatorVisual(modelo.transform);
        Avatar avatar = ObtenerAvatarModelo();

        if (animatorRaiz == null)
        {
            animatorRaiz = modelo.AddComponent<Animator>();
            Debug.Log("[SetupPersonajeJugador] Se agregó Animator principal en la raíz de ModeloJugador.");
        }

        if (avatar == null && animatorDetectado != null && animatorDetectado.avatar != null)
        {
            avatar = animatorDetectado.avatar;
        }

        if (avatar != null)
        {
            animatorRaiz.avatar = avatar;
        }

        Animator[] animators = modelo.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animatorActual = animators[i];
            if (animatorActual == null || animatorActual == animatorRaiz)
            {
                continue;
            }

            animatorActual.runtimeAnimatorController = null;
            animatorActual.enabled = false;
        }

        animatorRaiz.enabled = true;
        animatorRaiz.applyRootMotion = false;
        animatorRaiz.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animatorRaiz.updateMode = AnimatorUpdateMode.Normal;
        return animatorRaiz;
    }

    private static Avatar ObtenerAvatarModelo()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RutaModeloPaladin);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                Avatar avatar = assets[i] as Avatar;
                if (avatar != null)
                {
                    return avatar;
                }
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

        return null;
    }

    private static void ConfigurarControladorAnimacion(GameObject jugador, Animator animator)
    {
        if (jugador == null || animator == null)
        {
            return;
        }

        ControladorAnimacionJugador controlador = animator.GetComponent<ControladorAnimacionJugador>();
        if (controlador == null)
        {
            controlador = animator.gameObject.AddComponent<ControladorAnimacionJugador>();
            Debug.Log("[SetupPersonajeJugador] Se agregó ControladorAnimacionJugador al modelo del paladín.");
        }

        SerializedObject serializado = new SerializedObject(controlador);
        serializado.FindProperty("animatorJugador").objectReferenceValue = animator;
        serializado.FindProperty("cuerpoRigido").objectReferenceValue = jugador.GetComponent<Rigidbody>();
        serializado.FindProperty("sistemaEspada").objectReferenceValue = jugador.GetComponent<SistemaEspada>();

        SerializedProperty corregirBrazo = serializado.FindProperty("corregirBrazoDerechoEnIdle");
        if (corregirBrazo != null)
        {
            corregirBrazo.boolValue = false;
        }

        serializado.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controlador);
    }

    private static void ConfigurarReferenciasSistemaEspada(GameObject jugador, Transform raizModelo)
    {
        if (jugador == null)
        {
            return;
        }

        SistemaEspada sistemaEspada = jugador.GetComponent<SistemaEspada>();
        if (sistemaEspada == null)
        {
            Debug.LogWarning("[SetupPersonajeJugador] No se encontró SistemaEspada en el jugador.");
            return;
        }

        Transform transformEspada = BuscarTransformProp(raizModelo, new[] { "sword", "weapon", "blade", "prop", "wprop" });
        Transform transformEscudo = BuscarTransformProp(raizModelo, new[] { "shield" });

        SerializedObject serializado = new SerializedObject(sistemaEspada);
        SerializedProperty pivoteVisualEspada = serializado.FindProperty("pivoteVisualEspada");
        if (pivoteVisualEspada != null)
        {
            pivoteVisualEspada.objectReferenceValue = transformEspada;
        }

        serializado.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sistemaEspada);

        if (transformEspada != null)
        {
            Debug.Log("[SetupPersonajeJugador] Se asignó pivoteVisualEspada a " + transformEspada.name);
        }
        else
        {
            Debug.LogWarning("[SetupPersonajeJugador] No se encontró una espada separada en la jerarquía del paladín. Se deja el modelo tal cual.");
        }

        if (transformEscudo != null)
        {
            Debug.Log("[SetupPersonajeJugador] Escudo detectado en " + transformEscudo.name);
        }
    }

    private static void AplicarMaterialesYTexturasModelo(Transform raizModelo)
    {
        if (raizModelo == null)
        {
            return;
        }

        Material[] materialesImportados = ObtenerMaterialesImportadosDelModelo();
        Renderer[] renderers = raizModelo.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererActual = renderers[i];
            if (rendererActual == null)
            {
                continue;
            }

            Material[] materialesActuales = rendererActual.sharedMaterials;
            for (int j = 0; j < materialesActuales.Length; j++)
            {
                Material materialActual = materialesActuales[j];
                Material materialImportado = BuscarMaterialImportado(materialActual != null ? materialActual.name : string.Empty, materialesImportados);
                if (materialImportado != null)
                {
                    materialesActuales[j] = materialImportado;
                }

                Material materialFinal = materialesActuales[j];
                if (materialFinal == null)
                {
                    continue;
                }

                Shader shaderStandard = Shader.Find("Standard");
                if (shaderStandard != null)
                {
                    materialFinal.shader = shaderStandard;
                }

                if (materialFinal.mainTexture == null)
                {
                    Texture2D textura = BuscarTexturaApropiada(materialFinal.name);
                    if (textura != null)
                    {
                        materialFinal.mainTexture = textura;
                        Debug.Log("[SetupPersonajeJugador] Se asignó textura " + textura.name + " a " + materialFinal.name);
                    }
                }

                if (materialFinal.HasProperty("_Color"))
                {
                    materialFinal.color = Color.white;
                }
            }

            rendererActual.sharedMaterials = materialesActuales;
            EditorUtility.SetDirty(rendererActual);
        }
    }

    private static Material[] ObtenerMaterialesImportadosDelModelo()
    {
        List<Material> materiales = new List<Material>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RutaModeloPaladin);
        if (assets == null)
        {
            return materiales.ToArray();
        }

        for (int i = 0; i < assets.Length; i++)
        {
            Material material = assets[i] as Material;
            if (material != null)
            {
                materiales.Add(material);
            }
        }

        return materiales.ToArray();
    }

    private static Material BuscarMaterialImportado(string nombreMaterial, Material[] materiales)
    {
        if (string.IsNullOrEmpty(nombreMaterial) || materiales == null || materiales.Length == 0)
        {
            return null;
        }

        string nombreNormalizado = nombreMaterial.Replace(" (Instance)", string.Empty).Trim().ToLowerInvariant();
        for (int i = 0; i < materiales.Length; i++)
        {
            Material material = materiales[i];
            if (material == null)
            {
                continue;
            }

            if (material.name.Trim().ToLowerInvariant() == nombreNormalizado)
            {
                return material;
            }
        }

        return null;
    }

    private static Texture2D BuscarTexturaApropiada(string nombreMaterial)
    {
        string nombreNormalizado = (nombreMaterial ?? string.Empty)
            .Replace(" (Instance)", string.Empty)
            .Replace("_", " ")
            .Trim();

        if (string.IsNullOrEmpty(nombreNormalizado))
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets(nombreNormalizado + " t:Texture2D");
        if (guids == null || guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("Paladin t:Texture2D");
        }

        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning("[SetupPersonajeJugador] No se encontraron texturas externas para " + nombreNormalizado + ". El modelo podría depender de materiales embebidos o no traer texturas al proyecto.");
            return null;
        }

        string ruta = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    private static Transform BuscarTransformProp(Transform raiz, string[] tokens)
    {
        if (raiz == null || tokens == null || tokens.Length == 0)
        {
            return null;
        }

        Transform[] transforms = raiz.GetComponentsInChildren<Transform>(true);

        for (int pasada = 0; pasada < 2; pasada++)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform actual = transforms[i];
                if (actual == null || actual == raiz)
                {
                    continue;
                }

                string nombre = actual.name.ToLowerInvariant();
                bool coincide = false;
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (nombre.Contains(tokens[j]))
                    {
                        coincide = true;
                        break;
                    }
                }

                if (!coincide)
                {
                    continue;
                }

                if (pasada == 0)
                {
                    if (actual.GetComponent<Renderer>() != null || actual.GetComponentInChildren<Renderer>(true) != null)
                    {
                        return actual;
                    }
                }
                else
                {
                    return actual;
                }
            }
        }

        return null;
    }

    private static void AjustarColisionJugador(GameObject jugador, GameObject modelo)
    {
        if (jugador == null || modelo == null)
        {
            return;
        }

        Bounds? boundsModelo = CalcularBoundsRenderers(modelo.transform);
        if (!boundsModelo.HasValue)
        {
            return;
        }

        float altura = Mathf.Max(1.6f, boundsModelo.Value.size.y);
        float radio = Mathf.Clamp(boundsModelo.Value.extents.x * 0.35f, 0.25f, 0.6f);

        CapsuleCollider capsula = jugador.GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            capsula.height = altura;
            capsula.radius = radio;
            capsula.center = new Vector3(0f, altura * 0.5f, 0f);
        }

        CharacterController characterController = jugador.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.height = altura;
            characterController.radius = radio;
            characterController.center = new Vector3(0f, altura * 0.5f, 0f);
        }
    }

    private static void DesactivarVisualRaiz(GameObject jugador)
    {
        if (jugador == null)
        {
            return;
        }

        MeshRenderer rendererRaiz = jugador.GetComponent<MeshRenderer>();
        if (rendererRaiz != null)
        {
            rendererRaiz.enabled = false;
        }
    }
}
