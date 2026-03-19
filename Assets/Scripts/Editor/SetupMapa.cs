using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script arma el mapa medieval completo con un solo menu:
// terreno, decoracion, luces, NavMesh y puntos de spawn.

// Esta clase agrega el menu de configuracion del mapa medieval.
public static class SetupMapa
{
    // Ruta base de los packs importados.
    private const string RutaPolytope = "Assets/Polytope Studio";

    // Menu principal del setup.
    [MenuItem("Realm Brawl/Setup/Mapa Medieval")]
    public static void ConfigurarMapaMedieval()
    {
        GameObject raizMapa = ObtenerOCrearRaizMapa();

        GeneradorMapa generadorMapa = raizMapa.GetComponent<GeneradorMapa>();
        if (generadorMapa == null)
        {
            generadorMapa = raizMapa.AddComponent<GeneradorMapa>();
        }

        Material materialTerreno = BuscarMaterial("PT_Grass_Mat", "PT_Terrain_mat", "PT_Grass_Mat 1");
        GameObject prefabArbol = BuscarPrefab("PT_Generic_Tree_01_green", "PT_Pine_Tree_03_green", "PT_Fruit_Tree_01_green");
        GameObject prefabRoca = BuscarPrefab("PT_Generic_Rock_01", "PT_River_Rock_Pile_02", "PT_Menhir_Rock_02");
        GameObject prefabArbusto = BuscarPrefab("PT_Generic_Shrub_01_green", "PT_Grass_02", "PT_Grass_01");
        GameObject prefabValla = BuscarPrefab("PT_Modular_Fence_Wood_01", "PT_Modular_Fence_Wood_02", "PT_Village_Fence_Small_01");
        GameObject prefabAntorcha = BuscarPrefab("PT_Torch_01", "PT_Torch", "PT_Fire_01");

        generadorMapa.AsignarRecursos(materialTerreno, prefabArbol, prefabRoca, prefabArbusto, prefabValla, prefabAntorcha);

        EliminarPlaneViejo();

        generadorMapa.GenerarMapa();

        ConfigurarIluminacion();
        ReconfigurarNavMesh(raizMapa);
        ReasignarPuntosSpawnASistemaOleadas(generadorMapa);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = raizMapa;

        Debug.Log("Mapa medieval configurado correctamente.");
    }

    // Busca o crea la raiz principal del mapa.
    private static GameObject ObtenerOCrearRaizMapa()
    {
        GameObject raizMapa = GameObject.Find("MapaMedieval");
        if (raizMapa != null)
        {
            return raizMapa;
        }

        return new GameObject("MapaMedieval");
    }

    // Borra el plane viejo si todavia existe.
    private static void EliminarPlaneViejo()
    {
        GameObject planeViejo = GameObject.Find("Plane");
        if (planeViejo != null)
        {
            Object.DestroyImmediate(planeViejo);
        }
    }

    // Configura luz, fog y skybox.
    private static void ConfigurarIluminacion()
    {
        Light luzDireccional = BuscarOLightDirectional();
        luzDireccional.color = new Color(1f, 0.78f, 0.6f, 1f);
        luzDireccional.intensity = 0.8f;
        luzDireccional.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.3f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.45f, 0.52f, 0.6f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;

        Material skybox = BuscarMaterial("PT_Skybox_mat");
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }
    }

    // Busca una Directional Light o la crea.
    private static Light BuscarOLightDirectional()
    {
        Light[] luces = Object.FindObjectsOfType<Light>(true);
        for (int indice = 0; indice < luces.Length; indice++)
        {
            if (luces[indice] != null && luces[indice].type == LightType.Directional)
            {
                return luces[indice];
            }
        }

        GameObject objetoLuz = GameObject.Find("Directional Light");
        if (objetoLuz == null)
        {
            objetoLuz = new GameObject("Directional Light");
        }

        Light luz = objetoLuz.GetComponent<Light>();
        if (luz == null)
        {
            luz = objetoLuz.AddComponent<Light>();
        }

        luz.type = LightType.Directional;
        return luz;
    }

    // Reconstruye y rebakea el NavMesh.
    private static void ReconfigurarNavMesh(GameObject raizMapa)
    {
        NavMeshSurface navMeshSurface = raizMapa.GetComponent<NavMeshSurface>();
        if (navMeshSurface == null)
        {
            navMeshSurface = raizMapa.AddComponent<NavMeshSurface>();
        }

        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.layerMask = ~0;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.defaultArea = 0;
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();
    }

    // Vuelve a cargar los puntos de spawn dentro del sistema de oleadas.
    private static void ReasignarPuntosSpawnASistemaOleadas(GeneradorMapa generadorMapa)
    {
        if (generadorMapa == null)
        {
            return;
        }

        SistemaOleadas sistemaOleadas = Object.FindObjectOfType<SistemaOleadas>(true);
        if (sistemaOleadas == null)
        {
            return;
        }

        Transform[] puntosSpawn = generadorMapa.PuntosSpawnGenerados;
        SerializedObject serializadoOleadas = new SerializedObject(sistemaOleadas);
        SerializedProperty propiedadPuntosSpawn = serializadoOleadas.FindProperty("puntosSpawn");

        if (propiedadPuntosSpawn == null)
        {
            return;
        }

        propiedadPuntosSpawn.arraySize = puntosSpawn.Length;
        for (int indice = 0; indice < puntosSpawn.Length; indice++)
        {
            propiedadPuntosSpawn.GetArrayElementAtIndex(indice).objectReferenceValue = puntosSpawn[indice];
        }

        serializadoOleadas.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sistemaOleadas);
    }

    // Busca un material por varios nombres posibles.
    private static Material BuscarMaterial(params string[] nombres)
    {
        string[] carpetasBusqueda =
        {
            RutaPolytope + "/Lowpoly_Characters",
            RutaPolytope + "/Lowpoly_Environments",
            RutaPolytope + "/Lowpoly_Props",
            RutaPolytope + "/Lowpoly_Village",
            RutaPolytope + "/Lowpoly_Weapons",
            RutaPolytope + "/Lowpoly_Demos"
        };

        for (int indiceNombre = 0; indiceNombre < nombres.Length; indiceNombre++)
        {
            string[] guids = AssetDatabase.FindAssets(nombres[indiceNombre] + " t:Material", carpetasBusqueda);
            if (guids.Length == 0)
            {
                continue;
            }

            string ruta = AssetDatabase.GUIDToAssetPath(guids[0]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material != null)
            {
                return material;
            }
        }

        return null;
    }

    // Busca un prefab por varios nombres posibles.
    private static GameObject BuscarPrefab(params string[] nombres)
    {
        string[] carpetasBusqueda =
        {
            RutaPolytope + "/Lowpoly_Characters",
            RutaPolytope + "/Lowpoly_Environments",
            RutaPolytope + "/Lowpoly_Props",
            RutaPolytope + "/Lowpoly_Village",
            RutaPolytope + "/Lowpoly_Weapons",
            RutaPolytope + "/Lowpoly_Demos"
        };

        for (int indiceNombre = 0; indiceNombre < nombres.Length; indiceNombre++)
        {
            string[] guids = AssetDatabase.FindAssets(nombres[indiceNombre] + " t:Prefab", carpetasBusqueda);
            if (guids.Length == 0)
            {
                continue;
            }

            string ruta = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    // COPILOT-EXPAND: aqui podes agregar presets por bioma, ruinas, caminos o variantes de clima.
}
