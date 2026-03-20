using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor crea los prefabs de efectos visuales,
// asegura los sistemas globales y deja todo listo con un solo menu.

// Esta clase agrega un menu para preparar el sistema visual completo.
public static class SetupEfectos
{
    // Esta opcion de menu arma los prefabs de efectos y los sistemas de escena.
    [MenuItem("Realm Brawl/Setup/Efectos Visuales")]
    public static void ConfigurarEfectosVisuales()
    {
        string rutaCarpetaEfectos = AsegurarCarpetaEfectos();

        GameObject prefabGolpeNormal = CrearPrefabEfecto(rutaCarpetaEfectos, "EfectoGolpeNormal", new Color(0.95f, 0.95f, 0.95f, 1f), false, 12, 0.25f, 1.1f, 0.08f, ParticleSystemShapeType.Sphere);
        GameObject prefabGolpeCritico = CrearPrefabEfecto(rutaCarpetaEfectos, "EfectoGolpeCritico", new Color(1f, 0.82f, 0.25f, 1f), true, 18, 0.35f, 1.6f, 0.11f, ParticleSystemShapeType.Sphere);
        GameObject prefabMuerteEsqueleto = CrearPrefabEfecto(rutaCarpetaEfectos, "EfectoMuerteEsqueleto", new Color(0.55f, 0.55f, 0.55f, 1f), false, 20, 0.6f, 2.4f, 0.09f, ParticleSystemShapeType.Sphere);
        GameObject prefabPocion = CrearPrefabEfecto(rutaCarpetaEfectos, "EfectoPocion", new Color(0.35f, 1f, 0.5f, 1f), true, 16, 0.75f, 0.55f, 0.06f, ParticleSystemShapeType.Cone);
        GameObject prefabSubirNivel = CrearPrefabEfecto(rutaCarpetaEfectos, "EfectoSubirNivel", new Color(1f, 0.88f, 0.35f, 1f), true, 24, 0.9f, 1.35f, 0.08f, ParticleSystemShapeType.Circle);

        SistemaEfectos sistemaEfectos = BuscarOCrearSistemaEfectos();
        SistemaAudio sistemaAudio = BuscarOCrearSistemaAudio();

        SerializedObject serializadoEfectos = new SerializedObject(sistemaEfectos);
        serializadoEfectos.FindProperty("prefabGolpeNormal").objectReferenceValue = prefabGolpeNormal;
        serializadoEfectos.FindProperty("prefabGolpeCritico").objectReferenceValue = prefabGolpeCritico;
        serializadoEfectos.FindProperty("prefabMuerteEsqueleto").objectReferenceValue = prefabMuerteEsqueleto;
        serializadoEfectos.FindProperty("prefabPocion").objectReferenceValue = prefabPocion;
        serializadoEfectos.FindProperty("prefabSubirNivel").objectReferenceValue = prefabSubirNivel;
        serializadoEfectos.FindProperty("tiempoAutoDestruccionGeneral").floatValue = 1.5f;
        serializadoEfectos.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(sistemaEfectos);
        EditorUtility.SetDirty(sistemaAudio);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = sistemaEfectos.gameObject;
        Debug.Log("Realm Brawl -> Efectos Visuales configurados. Prefabs creados en " + rutaCarpetaEfectos);
    }

    // Este metodo asegura la carpeta donde guardamos los prefabs.
    private static string AsegurarCarpetaEfectos()
    {
        string rutaPrefabs = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(rutaPrefabs))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string rutaEfectos = "Assets/Prefabs/Efectos";
        if (!AssetDatabase.IsValidFolder(rutaEfectos))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Efectos");
        }

        return rutaEfectos;
    }

    // Este metodo crea un prefab de particulas con parametros simples y claros.
    private static GameObject CrearPrefabEfecto(string rutaCarpeta, string nombrePrefab, Color colorBase, bool agregarLuz, int cantidadParticulas, float duracion, float velocidad, float tamano, ParticleSystemShapeType tipoForma)
    {
        string rutaCompleta = rutaCarpeta + "/" + nombrePrefab + ".prefab";

        GameObject prefabExistente = AssetDatabase.LoadAssetAtPath<GameObject>(rutaCompleta);
        bool actualizandoPrefabExistente = prefabExistente != null;

        GameObject objetoTemporal = actualizandoPrefabExistente
            ? PrefabUtility.LoadPrefabContents(rutaCompleta)
            : new GameObject(nombrePrefab);

        objetoTemporal.name = nombrePrefab;
        objetoTemporal.SetActive(false);

        ParticleSystem sistemaParticulas = objetoTemporal.GetComponent<ParticleSystem>();
        if (sistemaParticulas == null)
        {
            sistemaParticulas = objetoTemporal.AddComponent<ParticleSystem>();
        }

        // Frenamos cualquier reproduccion previa para evitar warnings al reconfigurar prefabs existentes.
        sistemaParticulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        sistemaParticulas.Clear(true);

        ParticleSystemRenderer rendererParticulas = objetoTemporal.GetComponent<ParticleSystemRenderer>();
        if (rendererParticulas == null)
        {
            rendererParticulas = objetoTemporal.AddComponent<ParticleSystemRenderer>();
        }

        ParticleSystem.MainModule main = sistemaParticulas.main;
        main.loop = nombrePrefab.Contains("Pocion");
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(duracion);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidad);
        main.startSize = new ParticleSystem.MinMaxCurve(tamano);
        main.startColor = new ParticleSystem.MinMaxGradient(colorBase);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = sistemaParticulas.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, (short)cantidadParticulas);
        emission.SetBursts(new[] { burst });

        ParticleSystem.ShapeModule shape = sistemaParticulas.shape;
        shape.shapeType = tipoForma;
        shape.radius = 0.15f;
        shape.angle = 25f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sistemaParticulas.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[] { new GradientColorKey(colorBase, 0f), new GradientColorKey(colorBase, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradiente;

        main.gravityModifier = nombrePrefab.Contains("Muerte") ? 1.3f : 0f;
        rendererParticulas.renderMode = ParticleSystemRenderMode.Billboard;
        rendererParticulas.sharedMaterial = ObtenerOCrearMaterialParticulas(rutaCarpeta, nombrePrefab, colorBase, agregarLuz);

        Light luz = objetoTemporal.GetComponent<Light>();

        if (agregarLuz)
        {
            if (luz == null)
            {
                luz = objetoTemporal.AddComponent<Light>();
            }

            luz.type = LightType.Point;
            luz.range = 3f;
            luz.intensity = 1.5f;
            luz.color = colorBase;
        }
        else if (luz != null)
        {
            Object.DestroyImmediate(luz);
        }

        objetoTemporal.SetActive(true);

        GameObject prefabCreado = PrefabUtility.SaveAsPrefabAsset(objetoTemporal, rutaCompleta);

        if (actualizandoPrefabExistente)
        {
            PrefabUtility.UnloadPrefabContents(objetoTemporal);
        }
        else
        {
            Object.DestroyImmediate(objetoTemporal);
        }

        return prefabCreado;
    }

    // Este metodo obtiene o crea un material de particulas compatible con Built-in Render Pipeline.
    private static Material ObtenerOCrearMaterialParticulas(string rutaCarpeta, string nombrePrefab, Color colorBase, bool aditivo)
    {
        string rutaMaterial = rutaCarpeta + "/" + nombrePrefab + "_Mat.mat";
        Material materialExistente = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (materialExistente != null)
        {
            return materialExistente;
        }

        Shader shaderParticulas = Shader.Find(aditivo ? "Legacy Shaders/Particles/Additive" : "Particles/Standard Unlit");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        }

        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Sprites/Default");
        }

        Material nuevoMaterial = new Material(shaderParticulas);
        nuevoMaterial.name = nombrePrefab + "_Mat";

        if (nuevoMaterial.HasProperty("_Color"))
        {
            nuevoMaterial.SetColor("_Color", colorBase);
        }

        if (nuevoMaterial.HasProperty("_BaseColor"))
        {
            nuevoMaterial.SetColor("_BaseColor", colorBase);
        }

        AssetDatabase.CreateAsset(nuevoMaterial, rutaMaterial);
        return nuevoMaterial;
    }

    // Este metodo busca o crea el sistema de efectos en escena.
    private static SistemaEfectos BuscarOCrearSistemaEfectos()
    {
        SistemaEfectos sistema = Object.FindObjectOfType<SistemaEfectos>(true);
        if (sistema != null)
        {
            return sistema;
        }

        GameObject objetoSistema = new GameObject("SistemaEfectos");
        return objetoSistema.AddComponent<SistemaEfectos>();
    }

    // Este metodo busca o crea el sistema de audio placeholder.
    private static SistemaAudio BuscarOCrearSistemaAudio()
    {
        SistemaAudio sistema = Object.FindObjectOfType<SistemaAudio>(true);
        if (sistema != null)
        {
            return sistema;
        }

        GameObject objetoSistema = new GameObject("SistemaAudio");
        return objetoSistema.AddComponent<SistemaAudio>();
    }
}
