using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor configura automaticamente el sistema de oleadas,
// crea puntos de spawn y genera un prefab base del enemigo si hace falta.

// Esta clase agrega un menu que arma el sistema de oleadas con un solo click.
public static class ConfiguradorOleadas
{
    // Esta opcion de menu crea y conecta el sistema completo de oleadas.
    [MenuItem("Realm Brawl/Configurar/Sistema de Oleadas")]
    public static void ConfigurarSistemaDeOleadas()
    {
        // Buscamos o creamos GameManager porque el sistema de oleadas se integra con el.
        GameManager gameManager = BuscarOCrearGameManager();

        // Buscamos o creamos el objeto que alojara el sistema.
        GameObject objetoSistemaOleadas = GameObject.Find("SistemaOleadas");

        // Si no existe, lo creamos.
        if (objetoSistemaOleadas == null)
        {
            objetoSistemaOleadas = new GameObject("SistemaOleadas");
        }

        // Buscamos o agregamos el componente principal.
        SistemaOleadas sistemaOleadas = objetoSistemaOleadas.GetComponent<SistemaOleadas>();

        // Si falta, lo agregamos ahora.
        if (sistemaOleadas == null)
        {
            sistemaOleadas = objetoSistemaOleadas.AddComponent<SistemaOleadas>();
        }

        // Buscamos o creamos la carpeta contenedora de puntos de spawn.
        GameObject contenedorSpawn = GameObject.Find("PuntosSpawnOleadas");

        // Si no existe, la creamos ahora.
        if (contenedorSpawn == null)
        {
            contenedorSpawn = new GameObject("PuntosSpawnOleadas");
        }

        // Preparamos los puntos de spawn alrededor del jugador o del centro.
        Transform[] puntosSpawn = BuscarOCrearPuntosSpawn(contenedorSpawn.transform);

        // Buscamos un prefab de enemigo o construimos uno desde la escena.
        GameObject prefabEnemigo = BuscarOCrearPrefabEnemigo();

        // Asignamos todo via SerializedObject para tocar campos privados.
        SerializedObject serializadoOleadas = new SerializedObject(sistemaOleadas);
        serializadoOleadas.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializadoOleadas.FindProperty("prefabEnemigo").objectReferenceValue = prefabEnemigo;
        serializadoOleadas.FindProperty("cantidadBaseEnemigos").intValue = 2;
        serializadoOleadas.FindProperty("incrementoEnemigosPorOleada").intValue = 1;
        serializadoOleadas.FindProperty("demoraInicialPrimeraOleada").floatValue = 1.5f;
        serializadoOleadas.FindProperty("tiempoEntreOleadas").floatValue = 4f;
        serializadoOleadas.FindProperty("iniciarAutomaticamente").boolValue = true;
        serializadoOleadas.FindProperty("oleadaActual").intValue = 0;

        // Cargamos el array de puntos de spawn.
        SerializedProperty propiedadPuntosSpawn = serializadoOleadas.FindProperty("puntosSpawn");
        propiedadPuntosSpawn.arraySize = puntosSpawn.Length;

        // Recorremos cada punto creado o encontrado.
        for (int indicePunto = 0; indicePunto < puntosSpawn.Length; indicePunto++)
        {
            propiedadPuntosSpawn.GetArrayElementAtIndex(indicePunto).objectReferenceValue = puntosSpawn[indicePunto];
        }

        // Aplicamos cambios al componente.
        serializadoOleadas.ApplyModifiedPropertiesWithoutUndo();

        // Marcamos escena y componente como modificados.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.SetDirty(sistemaOleadas);

        // Seleccionamos el objeto del sistema para que el usuario lo vea.
        Selection.activeGameObject = objetoSistemaOleadas;
    }

    // Este metodo busca o crea un GameManager.
    private static GameManager BuscarOCrearGameManager()
    {
        // Buscamos uno ya existente en la escena.
        GameManager gameManager = Object.FindObjectOfType<GameManager>();

        // Si existe, lo devolvemos.
        if (gameManager != null)
        {
            return gameManager;
        }

        // Si no existe, creamos un objeto nuevo para alojarlo.
        GameObject objetoGameManager = new GameObject("GameManager");

        // Agregamos el componente y lo devolvemos.
        return objetoGameManager.AddComponent<GameManager>();
    }

    // Este metodo encuentra o crea cuatro puntos de spawn alrededor del jugador.
    private static Transform[] BuscarOCrearPuntosSpawn(Transform contenedor)
    {
        // Creamos una lista temporal de puntos.
        List<Transform> puntos = new List<Transform>();

        // Si el contenedor ya tiene hijos, los reutilizamos.
        for (int indiceHijo = 0; indiceHijo < contenedor.childCount; indiceHijo++)
        {
            puntos.Add(contenedor.GetChild(indiceHijo));
        }

        // Si ya habia puntos, devolvemos esos mismos.
        if (puntos.Count > 0)
        {
            return puntos.ToArray();
        }

        // Buscamos al jugador para tomarlo como centro de referencia.
        GameObject jugador = GameObject.Find("Jugador");

        // Definimos el centro de spawn alrededor del jugador o del origen.
        Vector3 centro = jugador != null ? jugador.transform.position : Vector3.zero;

        // Definimos cuatro offsets simples para la arena.
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(6f, 0f, 6f),
            new Vector3(-6f, 0f, 6f),
            new Vector3(6f, 0f, -6f),
            new Vector3(-6f, 0f, -6f)
        };

        // Creamos los cuatro puntos usando esos offsets.
        for (int indiceOffset = 0; indiceOffset < offsets.Length; indiceOffset++)
        {
            // Creamos el objeto del punto.
            GameObject punto = new GameObject("PuntoSpawn_" + (indiceOffset + 1));

            // Lo parentamos al contenedor.
            punto.transform.SetParent(contenedor);

            // Lo colocamos en el offset calculado.
            punto.transform.position = centro + offsets[indiceOffset];

            // Lo agregamos a la lista.
            puntos.Add(punto.transform);
        }

        // Devolvemos todos los puntos creados.
        return puntos.ToArray();
    }

    // Este metodo busca un prefab de enemigo o lo genera desde un enemigo de la escena.
    private static GameObject BuscarOCrearPrefabEnemigo()
    {
        // Definimos la ruta donde queremos guardar el prefab.
        string rutaPrefab = "Assets/Prefabs/EnemigoDummy.prefab";

        // Si el prefab ya existe, lo cargamos y devolvemos.
        GameObject prefabExistente = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);

        // Si ya existe, lo devolvemos enseguida.
        if (prefabExistente != null)
        {
            return prefabExistente;
        }

        // Buscamos un enemigo existente en la escena.
        EnemigoDummy enemigoEscena = Object.FindObjectOfType<EnemigoDummy>();

        // Si no encontramos uno, no podemos generar el prefab automaticamente.
        if (enemigoEscena == null)
        {
            return null;
        }

        // Si la carpeta Prefabs no existe, la creamos.
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Guardamos el objeto enemigo como prefab nuevo.
        GameObject prefabCreado = PrefabUtility.SaveAsPrefabAsset(enemigoEscena.gameObject, rutaPrefab);

        // Refrescamos la base de assets para que Unity lo vea enseguida.
        AssetDatabase.Refresh();

        // Devolvemos el prefab nuevo.
        return prefabCreado;
    }

    // COPILOT-EXPAND: Aqui podes agregar configuracion automatica de escalado por oleada y puntos de boss.
}
