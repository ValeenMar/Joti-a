using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este setup construye una arena authored para la demo jugable.
// La idea es dejar una base linda, legible y editable por el usuario,
// sin depender del mapa procedural anterior como layout final.

public static class SetupArenaDemoBase
{
    private const string RutaPolytope = "Assets/Polytope Studio";
    private const int SemillaArena = 42;
    private static readonly Vector2 TamanoArena = new Vector2(68f, 68f);
    private static readonly Vector2 AreaCombate = new Vector2(24f, 24f);

    [MenuItem("Realm Brawl/Setup/Arena Demo Base")]
    public static void ConstruirArenaDemoBase()
    {
        Random.InitState(SemillaArena);

        GameObject raiz = ObtenerOCrearRaiz();
        LimpiarHijos(raiz.transform);

        ArenaDemoMapa arenaDemoMapa = raiz.GetComponent<ArenaDemoMapa>();
        if (arenaDemoMapa == null)
        {
            arenaDemoMapa = raiz.AddComponent<ArenaDemoMapa>();
        }

        GameObject sueloPrincipal = CrearSueloPrincipal(raiz.transform);
        Transform propsRaiz = CrearNodo(raiz.transform, "Props");
        Transform regionesRaiz = CrearNodo(raiz.transform, "RegionesMiniMapa");
        Transform spawnsRaiz = CrearNodo(raiz.transform, "Spawns");

        GameObject arbolGrande = BuscarPrefab("PT_Generic_Tree_01_green", "PT_Fruit_Tree_01_green", "PT_Pine_Tree_03_green");
        GameObject roca = BuscarPrefab("PT_Generic_Rock_01", "PT_River_Rock_Pile_02", "PT_Menhir_Rock_02");
        GameObject arbusto = BuscarPrefab("PT_Generic_Shrub_01_green");
        GameObject valla = BuscarPrefab("PT_Modular_Fence_Wood_01", "PT_Modular_Fence_Wood_02", "PT_Village_Fence_Small_01");
        GameObject gate = BuscarPrefab("PT_Modular_Gate_Wood_01");
        GameObject puente = BuscarPrefab("PT_Wooden_Bridge_02");
        GameObject cofre = BuscarPrefab("PT_Chest_01");
        GameObject cruz = BuscarPrefab("PT_Wooden_Cross_01", "PT_Wooden_Cross_02");

        ConstruirBosqueIzquierdo(propsRaiz, regionesRaiz, arbolGrande, roca, arbusto);
        ConstruirCarrilDerecho(propsRaiz, regionesRaiz, arbolGrande, valla, gate, arbusto);
        ConstruirLandmark(propsRaiz, regionesRaiz, puente, gate, cofre, cruz, roca);
        ConstruirChokePoints(propsRaiz, regionesRaiz, roca);
        ConstruirAntorchas(propsRaiz);
        CrearSpawns(spawnsRaiz, regionesRaiz);

        ConfigurarArenaDemo(arenaDemoMapa, sueloPrincipal, spawnsRaiz, regionesRaiz);
        ReconfigurarSistemaOleadas(arenaDemoMapa);
        RehornearNavMesh(raiz);
        ReubicarActores(arenaDemoMapa);
        ConfigurarAmbienteVisual();
        DesactivarMapaLegacy();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = raiz;

        Debug.Log("[SetupArenaDemoBase] Arena demo authored creada correctamente.");
        EditorUtility.DisplayDialog("Arena Demo Base", "La arena authored quedó creada y lista para retocar a mano.", "OK");
    }

    private static GameObject ObtenerOCrearRaiz()
    {
        GameObject legacy = GameObject.Find("MapaMedieval");
        if (legacy != null)
        {
            Object.DestroyImmediate(legacy);
        }

        GameObject plane = GameObject.Find("Plane");
        if (plane != null)
        {
            Object.DestroyImmediate(plane);
        }

        GameObject arena = GameObject.Find("ArenaDemoBase");
        if (arena == null)
        {
            arena = new GameObject("ArenaDemoBase");
        }

        arena.transform.position = Vector3.zero;
        arena.transform.rotation = Quaternion.identity;
        return arena;
    }

    private static void LimpiarHijos(Transform raiz)
    {
        for (int indice = raiz.childCount - 1; indice >= 0; indice--)
        {
            Object.DestroyImmediate(raiz.GetChild(indice).gameObject);
        }
    }

    private static Transform CrearNodo(Transform padre, string nombre)
    {
        GameObject nodo = new GameObject(nombre);
        nodo.transform.SetParent(padre, false);
        return nodo.transform;
    }

    private static GameObject CrearSueloPrincipal(Transform padre)
    {
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "SueloPrincipal";
        suelo.transform.SetParent(padre, false);
        suelo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        suelo.transform.localScale = new Vector3(TamanoArena.x, 1f, TamanoArena.y);

        Material materialSuelo = CrearMaterialSuelo();
        MeshRenderer renderer = suelo.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = materialSuelo;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return suelo;
    }

    private static Material CrearMaterialSuelo()
    {
        Material material = CrearMaterialStandardSeguro("SueloPrincipal");
        if (material == null)
        {
            return null;
        }

        material.name = "ArenaDemo_Suelo_Runtime";
        material.color = new Color(0.33f, 0.58f, 0.31f, 1f);
        material.SetFloat("_Glossiness", 0.02f);

        Texture textura = BuscarTextura("PT_Ground_Grass_Green_01", "PT_Grass", "PT_Ground_Grass");
        if (textura != null)
        {
            material.mainTexture = textura;
            material.mainTextureScale = new Vector2(8f, 8f);
        }

        return material;
    }

    private static void ConstruirBosqueIzquierdo(Transform propsRaiz, Transform regionesRaiz, GameObject arbol, GameObject roca, GameObject arbusto)
    {
        Transform grupo = CrearNodo(propsRaiz, "BosqueIzquierdo");
        Vector3[] posiciones =
        {
            new Vector3(-23f, 0f, -20f),
            new Vector3(-26f, 0f, -8f),
            new Vector3(-24f, 0f, 5f),
            new Vector3(-22f, 0f, 18f),
            new Vector3(-16f, 0f, -4f),
            new Vector3(-14f, 0f, 12f)
        };

        float[] escalas =
        {
            1.18f,
            1.06f,
            1.14f,
            1.10f,
            0.94f,
            0.98f
        };

        for (int indice = 0; indice < posiciones.Length; indice++)
        {
            InstanciarProp(arbol, grupo, "Arbol_" + indice, posiciones[indice], Vector3.one * escalas[indice], Quaternion.Euler(0f, indice * 33f, 0f));
        }

        InstanciarProp(roca, grupo, "Roca_A", new Vector3(-11f, 0f, 10f), Vector3.one * 1.25f, Quaternion.Euler(0f, 18f, 0f));
        InstanciarProp(roca, grupo, "Roca_B", new Vector3(-15f, 0f, -12f), Vector3.one * 1.1f, Quaternion.Euler(0f, -24f, 0f));
        InstanciarProp(roca, grupo, "Roca_C", new Vector3(-18f, 0f, 14f), Vector3.one * 0.9f, Quaternion.Euler(0f, 42f, 0f));
        InstanciarProp(arbusto, grupo, "Arbusto_A", new Vector3(-10f, 0f, 4f), Vector3.one * 1.3f, Quaternion.Euler(0f, 15f, 0f));
        InstanciarProp(arbusto, grupo, "Arbusto_B", new Vector3(-18f, 0f, -2f), Vector3.one * 1.1f, Quaternion.Euler(0f, -18f, 0f));

        CrearRegion(regionesRaiz, "RegionBosqueIzquierdo", MiniMapaRegionTactica.TipoRegionTactica.Bosque, MiniMapaRegionTactica.FormaRegionTactica.Rectangulo, new Vector3(-20f, 0f, 0f), new Vector2(20f, 42f), new Color(0.18f, 0.30f, 0.18f, 0.94f));
    }

    private static void ConstruirCarrilDerecho(Transform propsRaiz, Transform regionesRaiz, GameObject arbol, GameObject valla, GameObject gate, GameObject arbusto)
    {
        Transform grupo = CrearNodo(propsRaiz, "CarrilDerecho");
        Vector3[] arboles =
        {
            new Vector3(18f, 0f, -21f),
            new Vector3(21f, 0f, -9f),
            new Vector3(19f, 0f, 7f),
            new Vector3(17f, 0f, 20f)
        };

        for (int indice = 0; indice < arboles.Length; indice++)
        {
            InstanciarProp(arbol, grupo, "ArbolCarril_" + indice, arboles[indice], Vector3.one * (0.96f + indice * 0.04f), Quaternion.Euler(0f, indice * 27f, 0f));
        }

        InstanciarProp(valla, grupo, "Valla_A", new Vector3(25f, 0f, -18f), Vector3.one, Quaternion.Euler(0f, 84f, 0f));
        InstanciarProp(valla, grupo, "Valla_B", new Vector3(24f, 0f, -6f), Vector3.one * 0.95f, Quaternion.Euler(0f, 94f, 0f));
        InstanciarProp(valla, grupo, "Valla_C", new Vector3(25.5f, 0f, 7f), Vector3.one, Quaternion.Euler(0f, 90f, 0f));
        InstanciarProp(valla, grupo, "Valla_D", new Vector3(23.8f, 0f, 18f), Vector3.one * 1.05f, Quaternion.Euler(0f, 102f, 0f));
        InstanciarProp(arbusto, grupo, "ArbustoCarril", new Vector3(13.5f, 0f, -1.5f), Vector3.one * 1.2f, Quaternion.Euler(0f, -10f, 0f));

        if (gate != null)
        {
            InstanciarProp(gate, grupo, "PortonRotoLejano", new Vector3(27f, 0f, 24f), Vector3.one * 0.9f, Quaternion.Euler(0f, 90f, 0f));
        }

        CrearRegion(regionesRaiz, "RegionCarrilDerecho", MiniMapaRegionTactica.TipoRegionTactica.Carril, MiniMapaRegionTactica.FormaRegionTactica.Rectangulo, new Vector3(21f, 0f, 0f), new Vector2(12f, 42f), new Color(0.34f, 0.25f, 0.14f, 0.88f));
        CrearRegion(regionesRaiz, "RegionBosqueDerecho", MiniMapaRegionTactica.TipoRegionTactica.Bosque, MiniMapaRegionTactica.FormaRegionTactica.Rectangulo, new Vector3(18f, 0f, 0f), new Vector2(10f, 34f), new Color(0.18f, 0.30f, 0.18f, 0.88f));
    }

    private static void ConstruirLandmark(Transform propsRaiz, Transform regionesRaiz, GameObject puente, GameObject gate, GameObject cofre, GameObject cruz, GameObject roca)
    {
        Transform grupo = CrearNodo(propsRaiz, "LandmarkFondo");
        CrearRuinaPiedra(grupo, "SantuarioRoto", new Vector3(0f, 0f, 24f));
        InstanciarProp(roca, grupo, "RocaLandmarkA", new Vector3(-4.4f, 0f, 26f), Vector3.one * 1.25f, Quaternion.Euler(0f, 18f, 0f));
        InstanciarProp(roca, grupo, "RocaLandmarkB", new Vector3(4.8f, 0f, 21f), Vector3.one * 1.08f, Quaternion.Euler(0f, -26f, 0f));
        InstanciarProp(roca, grupo, "RocaLandmarkC", new Vector3(0f, 0f, 28.5f), Vector3.one * 0.92f, Quaternion.Euler(0f, 34f, 0f));
        InstanciarProp(cruz, grupo, "CruzIzq", new Vector3(-6f, 0f, 22f), Vector3.one * 1.2f, Quaternion.Euler(0f, 18f, 0f));
        InstanciarProp(cruz, grupo, "CruzDer", new Vector3(6f, 0f, 25f), Vector3.one * 0.92f, Quaternion.Euler(0f, -22f, 0f));
        InstanciarProp(cofre, grupo, "CofreLandmark", new Vector3(0f, 0.18f, 23.4f), Vector3.one * 1.05f, Quaternion.Euler(0f, 180f, 0f));

        CrearRegion(regionesRaiz, "RegionLandmark", MiniMapaRegionTactica.TipoRegionTactica.Landmark, MiniMapaRegionTactica.FormaRegionTactica.Rectangulo, new Vector3(0f, 0f, 24f), new Vector2(16f, 10f), new Color(0.44f, 0.46f, 0.52f, 0.96f));
        CrearRegion(regionesRaiz, "RegionCaminoCentro", MiniMapaRegionTactica.TipoRegionTactica.Camino, MiniMapaRegionTactica.FormaRegionTactica.Capsula, new Vector3(0f, 0f, 10f), new Vector2(10f, 30f), new Color(0.30f, 0.36f, 0.20f, 0.72f));
    }

    private static void ConstruirChokePoints(Transform propsRaiz, Transform regionesRaiz, GameObject roca)
    {
        Transform grupo = CrearNodo(propsRaiz, "ChokePoints");
        InstanciarProp(roca, grupo, "ChokeIzquierdoA", new Vector3(-9.2f, 0f, 6.2f), Vector3.one * 1.25f, Quaternion.Euler(0f, 18f, 0f));
        InstanciarProp(roca, grupo, "ChokeIzquierdoB", new Vector3(-6.4f, 0f, 8.8f), Vector3.one * 0.88f, Quaternion.Euler(0f, -14f, 0f));
        InstanciarProp(roca, grupo, "ChokeDerechoA", new Vector3(8.6f, 0f, -7.4f), Vector3.one * 1.22f, Quaternion.Euler(0f, 21f, 0f));
        InstanciarProp(roca, grupo, "ChokeDerechoB", new Vector3(6.0f, 0f, -9.2f), Vector3.one * 0.92f, Quaternion.Euler(0f, -17f, 0f));

        CrearRegion(regionesRaiz, "RegionChokeIzq", MiniMapaRegionTactica.TipoRegionTactica.Choke, MiniMapaRegionTactica.FormaRegionTactica.Diamante, new Vector3(-8f, 0f, 7.5f), new Vector2(4.6f, 4.6f), new Color(0.58f, 0.36f, 0.18f, 0.92f));
        CrearRegion(regionesRaiz, "RegionChokeDer", MiniMapaRegionTactica.TipoRegionTactica.Choke, MiniMapaRegionTactica.FormaRegionTactica.Diamante, new Vector3(7.6f, 0f, -8f), new Vector2(4.6f, 4.6f), new Color(0.58f, 0.36f, 0.18f, 0.92f));
    }

    private static void ConstruirAntorchas(Transform propsRaiz)
    {
        Transform grupo = CrearNodo(propsRaiz, "Antorchas");
        CrearAntorcha(grupo, "AntorchaNoroeste", new Vector3(-13f, 0f, 13f));
        CrearAntorcha(grupo, "AntorchaNoreste", new Vector3(13f, 0f, 13f));
        CrearAntorcha(grupo, "AntorchaSuroeste", new Vector3(-13f, 0f, -13f));
        CrearAntorcha(grupo, "AntorchaSureste", new Vector3(13f, 0f, -13f));
    }

    private static void CrearAntorcha(Transform padre, string nombre, Vector3 posicion)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = posicion;

        GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        palo.name = "Palo";
        palo.transform.SetParent(raiz.transform, false);
        palo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        palo.transform.localScale = new Vector3(0.08f, 0.8f, 0.08f);
        if (palo.TryGetComponent(out Collider colliderPalo))
        {
            colliderPalo.enabled = false;
        }
        if (palo.TryGetComponent(out MeshRenderer rendererPalo))
        {
            Material materialMadera = CrearMaterialStandardSeguro("Antorcha/Palo");
            if (materialMadera != null)
            {
                materialMadera.color = new Color(0.33f, 0.21f, 0.10f, 1f);
                materialMadera.SetFloat("_Glossiness", 0.04f);
                rendererPalo.sharedMaterial = materialMadera;
            }
        }

        GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        llama.name = "Llama";
        llama.transform.SetParent(raiz.transform, false);
        llama.transform.localPosition = new Vector3(0f, 1.58f, 0f);
        llama.transform.localScale = new Vector3(0.22f, 0.32f, 0.22f);
        if (llama.TryGetComponent(out Collider colliderLlama))
        {
            colliderLlama.enabled = false;
        }
        if (llama.TryGetComponent(out MeshRenderer rendererLlama))
        {
            Material materialLlama = CrearMaterialStandardSeguro("Antorcha/Llama");
            if (materialLlama != null)
            {
                materialLlama.color = new Color(1f, 0.56f, 0.18f, 1f);
                materialLlama.EnableKeyword("_EMISSION");
                materialLlama.SetColor("_EmissionColor", new Color(0.95f, 0.42f, 0.06f, 0.5f));
                rendererLlama.sharedMaterial = materialLlama;
            }
        }

        Light luz = raiz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 5f;
        luz.intensity = 1.3f;
        luz.color = new Color(1f, 0.62f, 0.28f, 1f);
        luz.shadows = LightShadows.None;

        CapsuleCollider colliderRaiz = raiz.AddComponent<CapsuleCollider>();
        colliderRaiz.direction = 1;
        colliderRaiz.radius = 0.22f;
        colliderRaiz.height = 1.9f;
        colliderRaiz.center = new Vector3(0f, 0.95f, 0f);
    }

    private static void CrearSpawns(Transform spawnsRaiz, Transform regionesRaiz)
    {
        CrearSpawn(spawnsRaiz, regionesRaiz, "SpawnJugador", new Vector3(0f, 0f, -18f), false);
        CrearSpawn(spawnsRaiz, regionesRaiz, "SpawnEnemigo_1", new Vector3(-27f, 0f, -14f), true);
        CrearSpawn(spawnsRaiz, regionesRaiz, "SpawnEnemigo_2", new Vector3(-25f, 0f, 18f), true);
        CrearSpawn(spawnsRaiz, regionesRaiz, "SpawnEnemigo_3", new Vector3(27f, 0f, -14f), true);
        CrearSpawn(spawnsRaiz, regionesRaiz, "SpawnEnemigo_4", new Vector3(0f, 0f, 31f), true);
    }

    private static void CrearSpawn(Transform spawnsRaiz, Transform regionesRaiz, string nombre, Vector3 posicion, bool crearRegion)
    {
        GameObject spawn = new GameObject(nombre);
        spawn.transform.SetParent(spawnsRaiz, false);
        spawn.transform.localPosition = posicion;
        spawn.transform.LookAt(Vector3.zero);

        if (crearRegion)
        {
            CrearRegion(regionesRaiz, "Region_" + nombre, MiniMapaRegionTactica.TipoRegionTactica.Spawn, MiniMapaRegionTactica.FormaRegionTactica.Diamante, posicion, new Vector2(2.5f, 2.5f), new Color(0.76f, 0.28f, 0.22f, 0.92f));
        }
    }

    private static MiniMapaRegionTactica CrearRegion(Transform padre, string nombre, MiniMapaRegionTactica.TipoRegionTactica tipo, MiniMapaRegionTactica.FormaRegionTactica forma, Vector3 posicion, Vector2 tamano, Color color)
    {
        GameObject region = new GameObject(nombre);
        region.transform.SetParent(padre, false);
        region.transform.localPosition = posicion;

        MiniMapaRegionTactica componente = region.AddComponent<MiniMapaRegionTactica>();
        SerializedObject serializado = new SerializedObject(componente);
        serializado.FindProperty("tipo").enumValueIndex = (int)tipo;
        serializado.FindProperty("forma").enumValueIndex = (int)forma;
        serializado.FindProperty("tamanoMundo").vector2Value = tamano;
        serializado.FindProperty("colorBase").colorValue = color;
        serializado.ApplyModifiedPropertiesWithoutUndo();
        return componente;
    }

    private static GameObject InstanciarProp(GameObject prefab, Transform padre, string nombre, Vector3 posicion, Vector3 escala)
    {
        return InstanciarProp(prefab, padre, nombre, posicion, escala, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
    }

    private static GameObject InstanciarProp(GameObject prefab, Transform padre, string nombre, Vector3 posicion, Vector3 escala, Quaternion rotacion)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        instancia.name = nombre;
        instancia.transform.localPosition = posicion;
        instancia.transform.localRotation = rotacion;
        instancia.transform.localScale = escala;
        ConfigurarColisionYObstaculo(instancia);
        return instancia;
    }

    private static void ConfigurarColisionYObstaculo(GameObject objeto)
    {
        if (objeto == null)
        {
            return;
        }

        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int indice = 1; indice < renderers.Length; indice++)
        {
            if (renderers[indice] != null)
            {
                bounds.Encapsulate(renderers[indice].bounds);
            }
        }

        string nombre = objeto.name.ToLowerInvariant();
        bool esArbol = nombre.Contains("arbol") || nombre.Contains("tree") || nombre.Contains("pine") || nombre.Contains("fruit");
        bool esArbusto = nombre.Contains("arbusto") || nombre.Contains("shrub") || nombre.Contains("bush");
        bool esValla = nombre.Contains("valla") || nombre.Contains("fence") || nombre.Contains("gate") || nombre.Contains("porton");
        bool esRoca = nombre.Contains("roca") || nombre.Contains("rock");

        EliminarCollidersRaizPrevios(objeto);

        if (esArbol)
        {
            ConfigurarColisionArbol(objeto, bounds);
            return;
        }

        if (esArbusto)
        {
            ConfigurarColisionArbusto(objeto, bounds);
            return;
        }

        if (esValla)
        {
            ConfigurarColisionValla(objeto, bounds);
            return;
        }

        if (esRoca)
        {
            ConfigurarColisionRoca(objeto, bounds);
            return;
        }

        ConfigurarColisionCajaReducida(objeto, bounds, 0.72f, 0.75f);
    }

    private static void EliminarCollidersRaizPrevios(GameObject objeto)
    {
        Collider[] collidersRaiz = objeto.GetComponents<Collider>();
        for (int indiceCollider = collidersRaiz.Length - 1; indiceCollider >= 0; indiceCollider--)
        {
            if (collidersRaiz[indiceCollider] != null)
            {
                Object.DestroyImmediate(collidersRaiz[indiceCollider]);
            }
        }
    }

    private static void ConfigurarColisionArbol(GameObject objeto, Bounds bounds)
    {
        CapsuleCollider collider = objeto.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = objeto.AddComponent<CapsuleCollider>();
        }

        float radio = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.12f, 0.45f, 1.1f);
        float altura = Mathf.Clamp(bounds.size.y * 0.42f, 2.2f, 4.2f);
        Vector3 centroMundo = new Vector3(bounds.center.x, bounds.min.y + altura * 0.5f, bounds.center.z);

        collider.direction = 1;
        collider.radius = radio;
        collider.height = altura;
        collider.center = objeto.transform.InverseTransformPoint(centroMundo);

        NavMeshObstacle obstaculo = ObtenerOAgregarNavMeshObstacle(objeto);
        obstaculo.shape = NavMeshObstacleShape.Capsule;
        obstaculo.center = collider.center;
        obstaculo.radius = collider.radius;
        obstaculo.height = collider.height;
        obstaculo.carving = false;
    }

    private static void ConfigurarColisionArbusto(GameObject objeto, Bounds bounds)
    {
        CapsuleCollider collider = objeto.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = objeto.AddComponent<CapsuleCollider>();
        }

        float radio = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.22f, 0.35f, 0.7f);
        float altura = Mathf.Clamp(bounds.size.y * 0.55f, 0.6f, 1.3f);
        Vector3 centroMundo = new Vector3(bounds.center.x, bounds.min.y + altura * 0.5f, bounds.center.z);

        collider.direction = 1;
        collider.radius = radio;
        collider.height = altura;
        collider.center = objeto.transform.InverseTransformPoint(centroMundo);

        NavMeshObstacle obstaculo = ObtenerOAgregarNavMeshObstacle(objeto);
        obstaculo.shape = NavMeshObstacleShape.Capsule;
        obstaculo.center = collider.center;
        obstaculo.radius = collider.radius;
        obstaculo.height = collider.height;
        obstaculo.carving = false;
    }

    private static void ConfigurarColisionValla(GameObject objeto, Bounds bounds)
    {
        BoxCollider collider = objeto.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = objeto.AddComponent<BoxCollider>();
        }

        Vector3 sizeReducido = new Vector3(
            Mathf.Max(0.45f, bounds.size.x * 0.82f),
            Mathf.Clamp(bounds.size.y * 0.68f, 0.8f, 1.8f),
            Mathf.Max(0.18f, bounds.size.z * 0.32f));

        Vector3 centroMundo = new Vector3(bounds.center.x, bounds.min.y + sizeReducido.y * 0.5f, bounds.center.z);
        collider.center = objeto.transform.InverseTransformPoint(centroMundo);
        collider.size = ConvertirTamanoMundoALocal(objeto.transform, sizeReducido);

        NavMeshObstacle obstaculo = ObtenerOAgregarNavMeshObstacle(objeto);
        obstaculo.shape = NavMeshObstacleShape.Box;
        obstaculo.center = collider.center;
        obstaculo.size = collider.size;
        obstaculo.carving = false;
    }

    private static void ConfigurarColisionRoca(GameObject objeto, Bounds bounds)
    {
        ConfigurarColisionCajaReducida(objeto, bounds, 0.62f, 0.68f);
    }

    private static void ConfigurarColisionCajaReducida(GameObject objeto, Bounds bounds, float factorHorizontal, float factorVertical)
    {
        BoxCollider collider = objeto.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = objeto.AddComponent<BoxCollider>();
        }

        Vector3 sizeReducido = new Vector3(
            Mathf.Max(0.45f, bounds.size.x * factorHorizontal),
            Mathf.Max(0.6f, bounds.size.y * factorVertical),
            Mathf.Max(0.45f, bounds.size.z * factorHorizontal));

        Vector3 centroMundo = new Vector3(bounds.center.x, bounds.min.y + sizeReducido.y * 0.5f, bounds.center.z);
        collider.center = objeto.transform.InverseTransformPoint(centroMundo);
        collider.size = ConvertirTamanoMundoALocal(objeto.transform, sizeReducido);

        NavMeshObstacle obstaculo = ObtenerOAgregarNavMeshObstacle(objeto);
        obstaculo.shape = NavMeshObstacleShape.Box;
        obstaculo.center = collider.center;
        obstaculo.size = collider.size;
        obstaculo.carving = false;
    }

    private static NavMeshObstacle ObtenerOAgregarNavMeshObstacle(GameObject objeto)
    {
        NavMeshObstacle obstaculo = objeto.GetComponent<NavMeshObstacle>();
        if (obstaculo == null)
        {
            obstaculo = objeto.AddComponent<NavMeshObstacle>();
        }

        return obstaculo;
    }

    private static Vector3 ConvertirTamanoMundoALocal(Transform referencia, Vector3 sizeMundo)
    {
        Vector3 escala = referencia.lossyScale;
        float escalaX = Mathf.Max(0.001f, Mathf.Abs(escala.x));
        float escalaY = Mathf.Max(0.001f, Mathf.Abs(escala.y));
        float escalaZ = Mathf.Max(0.001f, Mathf.Abs(escala.z));
        return new Vector3(sizeMundo.x / escalaX, sizeMundo.y / escalaY, sizeMundo.z / escalaZ);
    }

    private static void ConfigurarArenaDemo(ArenaDemoMapa arenaDemoMapa, GameObject sueloPrincipal, Transform spawnsRaiz, Transform regionesRaiz)
    {
        SerializedObject serializado = new SerializedObject(arenaDemoMapa);
        serializado.FindProperty("tamanoMundo").vector2Value = TamanoArena;
        serializado.FindProperty("sueloPrincipal").objectReferenceValue = sueloPrincipal.GetComponent<Collider>();
        serializado.FindProperty("puntoSpawnJugador").objectReferenceValue = spawnsRaiz.Find("SpawnJugador");

        List<Transform> spawnsEnemigos = new List<Transform>();
        for (int indice = 0; indice < spawnsRaiz.childCount; indice++)
        {
            Transform hijo = spawnsRaiz.GetChild(indice);
            if (hijo != null && hijo.name != "SpawnJugador")
            {
                spawnsEnemigos.Add(hijo);
            }
        }

        SerializedProperty propiedadSpawns = serializado.FindProperty("puntosSpawnEnemigos");
        propiedadSpawns.arraySize = spawnsEnemigos.Count;
        for (int indice = 0; indice < spawnsEnemigos.Count; indice++)
        {
            propiedadSpawns.GetArrayElementAtIndex(indice).objectReferenceValue = spawnsEnemigos[indice];
        }

        MiniMapaRegionTactica[] regiones = regionesRaiz.GetComponentsInChildren<MiniMapaRegionTactica>(true);
        SerializedProperty propiedadRegiones = serializado.FindProperty("regionesMiniMapa");
        propiedadRegiones.arraySize = regiones.Length;
        for (int indice = 0; indice < regiones.Length; indice++)
        {
            propiedadRegiones.GetArrayElementAtIndex(indice).objectReferenceValue = regiones[indice];
        }

        serializado.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(arenaDemoMapa);
    }

    private static void ReconfigurarSistemaOleadas(ArenaDemoMapa arenaDemoMapa)
    {
        if (arenaDemoMapa == null)
        {
            return;
        }

        SistemaOleadas sistemaOleadas = Object.FindObjectOfType<SistemaOleadas>(true);
        if (sistemaOleadas == null)
        {
            return;
        }

        SerializedObject serializadoOleadas = new SerializedObject(sistemaOleadas);
        SerializedProperty propiedadSpawns = serializadoOleadas.FindProperty("puntosSpawn");
        if (propiedadSpawns != null)
        {
            Transform[] spawns = arenaDemoMapa.PuntosSpawnEnemigos;
            propiedadSpawns.arraySize = spawns.Length;
            for (int indice = 0; indice < spawns.Length; indice++)
            {
                propiedadSpawns.GetArrayElementAtIndex(indice).objectReferenceValue = spawns[indice];
            }
        }

        serializadoOleadas.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sistemaOleadas);
    }

    private static void RehornearNavMesh(GameObject raiz)
    {
        NavMeshSurface surface = raiz.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = raiz.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0;
        surface.RemoveData();
        surface.BuildNavMesh();
    }

    private static void ReubicarActores(ArenaDemoMapa arenaDemoMapa)
    {
        if (arenaDemoMapa == null)
        {
            return;
        }

        arenaDemoMapa.RecolectarReferenciasDesdeHijos();

        GameObject jugador = GameObject.Find("Jugador");
        if (jugador != null)
        {
            Vector3 destinoJugador = arenaDemoMapa.PuntoSpawnJugador != null ? arenaDemoMapa.PuntoSpawnJugador.position : jugador.transform.position;
            jugador.transform.position = arenaDemoMapa.ObtenerPosicionEnSuelo(destinoJugador, 10f);
            jugador.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            AlinearActorVisualAlSuelo(jugador.transform);
            EditorUtility.SetDirty(jugador);
        }

        EnemigoDummy[] enemigos = Object.FindObjectsOfType<EnemigoDummy>(true);
        Transform[] puntosSpawn = arenaDemoMapa.PuntosSpawnEnemigos;
        for (int indice = 0; indice < enemigos.Length; indice++)
        {
            if (enemigos[indice] == null)
            {
                continue;
            }

            Transform punto = puntosSpawn.Length > 0 ? puntosSpawn[Mathf.Clamp(indice, 0, puntosSpawn.Length - 1)] : null;
            Vector3 destino = punto != null ? punto.position : enemigos[indice].transform.position;
            enemigos[indice].transform.position = arenaDemoMapa.ObtenerPosicionEnSuelo(destino, 10f);
            if (punto != null)
            {
                enemigos[indice].transform.rotation = punto.rotation;
            }
            AlinearActorVisualAlSuelo(enemigos[indice].transform);
            EditorUtility.SetDirty(enemigos[indice]);
        }
    }

    private static void ConfigurarAmbienteVisual()
    {
        Light directional = Object.FindObjectOfType<Light>(true);
        if (directional == null || directional.type != LightType.Directional)
        {
            GameObject luz = GameObject.Find("Directional Light");
            if (luz == null)
            {
                luz = new GameObject("Directional Light");
            }

            directional = luz.GetComponent<Light>();
            if (directional == null)
            {
                directional = luz.AddComponent<Light>();
            }
            directional.type = LightType.Directional;
        }

        directional.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        directional.intensity = 1.1f;
        directional.color = new Color(1f, 0.95f, 0.85f, 1f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.05f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.77f, 0.82f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0045f;
    }

    private static void DesactivarMapaLegacy()
    {
        GameObject minimapaLegacy = GameObject.Find("CamaraMiniMapaHUD");
        if (minimapaLegacy != null)
        {
            Object.DestroyImmediate(minimapaLegacy);
        }
    }

    private static GameObject BuscarPrefab(params string[] nombres)
    {
        string[] carpetas =
        {
            RutaPolytope + "/Lowpoly_Environments",
            RutaPolytope + "/Lowpoly_Village",
            RutaPolytope + "/Lowpoly_Props",
            RutaPolytope + "/Lowpoly_Demos"
        };

        for (int indiceNombre = 0; indiceNombre < nombres.Length; indiceNombre++)
        {
            string[] guids = AssetDatabase.FindAssets(nombres[indiceNombre] + " t:Prefab", carpetas);
            for (int indiceGuid = 0; indiceGuid < guids.Length; indiceGuid++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[indiceGuid]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab != null)
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    private static Texture BuscarTextura(params string[] nombres)
    {
        string[] carpetas =
        {
            RutaPolytope + "/Lowpoly_Environments",
            RutaPolytope + "/Lowpoly_Demos"
        };

        for (int indiceNombre = 0; indiceNombre < nombres.Length; indiceNombre++)
        {
            string[] guids = AssetDatabase.FindAssets(nombres[indiceNombre] + " t:Texture2D", carpetas);
            if (guids.Length <= 0)
            {
                continue;
            }

            Texture textura = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (textura != null)
            {
                return textura;
            }
        }

        return null;
    }

    private static float RandomScale(float minimo, float maximo)
    {
        return Random.Range(minimo, maximo);
    }

    private static void AlinearActorVisualAlSuelo(Transform actor)
    {
        if (actor == null)
        {
            return;
        }

        Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = new Bounds(actor.position, Vector3.zero);
        bool encontro = false;
        for (int indice = 0; indice < renderers.Length; indice++)
        {
            Renderer renderer = renderers[indice];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!encontro)
            {
                bounds = renderer.bounds;
                encontro = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!encontro)
        {
            return;
        }

        float diferencia = actor.position.y - bounds.min.y;
        if (Mathf.Abs(diferencia) > 0.005f)
        {
            actor.position += Vector3.up * diferencia;
        }

        NavMeshAgent agente = actor.GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            agente.baseOffset = 0f;
        }
    }

    private static void CrearRuinaPiedra(Transform padre, string nombre, Vector3 posicion)
    {
        GameObject ruina = new GameObject(nombre);
        ruina.transform.SetParent(padre, false);
        ruina.transform.localPosition = posicion;

        Material materialPiedra = CrearMaterialPiedra();

        CrearBloquePiedra(ruina.transform, "Base", new Vector3(0f, 0.25f, 0f), new Vector3(6.2f, 0.5f, 4.2f), materialPiedra);
        CrearBloquePiedra(ruina.transform, "EscalonFrente", new Vector3(0f, 0.12f, -2.2f), new Vector3(4.8f, 0.24f, 0.8f), materialPiedra);
        CrearBloquePiedra(ruina.transform, "PilarIzq", new Vector3(-1.9f, 1.3f, 0.5f), new Vector3(0.7f, 2.1f, 0.7f), materialPiedra);
        CrearBloquePiedra(ruina.transform, "PilarDer", new Vector3(1.9f, 1.0f, -0.3f), new Vector3(0.7f, 1.5f, 0.7f), materialPiedra);
        CrearBloquePiedra(ruina.transform, "LintelRoto", new Vector3(-0.2f, 2.15f, 0.15f), new Vector3(3.4f, 0.4f, 0.75f), materialPiedra, Quaternion.Euler(0f, 0f, -4f));
        CrearBloquePiedra(ruina.transform, "BloqueCaidoA", new Vector3(-2.9f, 0.28f, 1.4f), new Vector3(0.9f, 0.56f, 0.9f), materialPiedra, Quaternion.Euler(12f, 16f, 8f));
        CrearBloquePiedra(ruina.transform, "BloqueCaidoB", new Vector3(2.7f, 0.22f, -1.2f), new Vector3(1.0f, 0.44f, 0.8f), materialPiedra, Quaternion.Euler(-8f, -18f, 4f));
    }

    private static void CrearBloquePiedra(Transform padre, string nombre, Vector3 posicionLocal, Vector3 escalaLocal, Material material, Quaternion? rotacionLocal = null)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicionLocal;
        bloque.transform.localRotation = rotacionLocal ?? Quaternion.identity;
        bloque.transform.localScale = escalaLocal;

        MeshRenderer renderer = bloque.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.receiveShadows = true;
        }

        ConfigurarColisionYObstaculo(bloque);
    }

    private static Material CrearMaterialPiedra()
    {
        Material material = CrearMaterialStandardSeguro("Landmark/Piedra");
        if (material == null)
        {
            return null;
        }

        material.name = "ArenaDemo_Piedra_Runtime";
        material.color = new Color(0.46f, 0.48f, 0.52f, 1f);
        material.SetFloat("_Glossiness", 0.08f);
        material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static Material CrearMaterialStandardSeguro(string contexto)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Diffuse");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("[SetupArenaDemoBase] No se encontro un shader compatible para " + contexto + ".");
            return null;
        }

        return new Material(shader);
    }
}
