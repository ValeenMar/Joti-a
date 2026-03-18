using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor prepara puntos de patrulla alrededor del enemigo
// y conecta automaticamente el array de patrulla del EnemigoDummy.

// Esta clase agrega un menu que crea puntos de patrulla y los conecta con la IA.
public static class ConfiguradorPatrullaEnemigo
{
    // Esta opcion de menu crea tres puntos de patrulla alrededor del enemigo seleccionado.
    [MenuItem("Realm Brawl/Configurar/Patrulla e IA del Enemigo")]
    public static void ConfigurarPatrullaEnemigo()
    {
        // Buscamos un enemigo valido a partir de la seleccion o de la escena.
        EnemigoDummy enemigo = BuscarEnemigoObjetivo();

        // Si no encontramos ninguno, avisamos y salimos.
        if (enemigo == null)
        {
            EditorUtility.DisplayDialog("Realm Brawl", "No encontre un EnemigoDummy en la escena ni seleccionado.", "Entendido");
            return;
        }

        // Buscamos o creamos el contenedor de patrulla como hijo del enemigo.
        Transform contenedorPatrulla = enemigo.transform.Find("PuntosPatrulla");

        // Si no existe, lo creamos ahora.
        if (contenedorPatrulla == null)
        {
            GameObject objetoContenedor = new GameObject("PuntosPatrulla");
            objetoContenedor.transform.SetParent(enemigo.transform);
            objetoContenedor.transform.localPosition = Vector3.zero;
            contenedorPatrulla = objetoContenedor.transform;
        }

        // Si ya tenia hijos, los reutilizamos. Si no, los creamos.
        Transform[] puntos = contenedorPatrulla.childCount > 0 ? ObtenerHijos(contenedorPatrulla) : CrearPuntosPatrullaBasicos(contenedorPatrulla, enemigo.transform.position);

        // Nos aseguramos de que cada punto tenga su componente PuntoPatrulla.
        for (int indicePunto = 0; indicePunto < puntos.Length; indicePunto++)
        {
            if (puntos[indicePunto] != null && puntos[indicePunto].GetComponent<PuntoPatrulla>() == null)
            {
                puntos[indicePunto].gameObject.AddComponent<PuntoPatrulla>();
            }
        }

        // Le pasamos los puntos al enemigo usando su metodo publico.
        enemigo.AsignarPuntosPatrulla(puntos);

        // Marcamos escena y enemigo como modificados.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.SetDirty(enemigo);

        // Seleccionamos el enemigo para mostrar el resultado.
        Selection.activeGameObject = enemigo.gameObject;
    }

    // Este metodo intenta encontrar un EnemigoDummy partiendo de la seleccion actual.
    private static EnemigoDummy BuscarEnemigoObjetivo()
    {
        // Si hay un objeto seleccionado, intentamos sacar de ahi el enemigo.
        if (Selection.activeGameObject != null)
        {
            EnemigoDummy enemigoSeleccionado = Selection.activeGameObject.GetComponent<EnemigoDummy>();

            // Si no estaba en el mismo objeto, buscamos en padres.
            if (enemigoSeleccionado == null)
            {
                enemigoSeleccionado = Selection.activeGameObject.GetComponentInParent<EnemigoDummy>();
            }

            // Si encontramos uno, lo devolvemos.
            if (enemigoSeleccionado != null)
            {
                return enemigoSeleccionado;
            }
        }

        // Si no habia nada util seleccionado, devolvemos el primero de la escena.
        return Object.FindObjectOfType<EnemigoDummy>();
    }

    // Este metodo devuelve todos los hijos directos de un transform.
    private static Transform[] ObtenerHijos(Transform padre)
    {
        // Creamos el array del tamaño correcto.
        Transform[] hijos = new Transform[padre.childCount];

        // Copiamos cada hijo al array.
        for (int indiceHijo = 0; indiceHijo < padre.childCount; indiceHijo++)
        {
            hijos[indiceHijo] = padre.GetChild(indiceHijo);
        }

        // Devolvemos el array completo.
        return hijos;
    }

    // Este metodo crea tres puntos en triangulo alrededor del enemigo.
    private static Transform[] CrearPuntosPatrullaBasicos(Transform contenedor, Vector3 centro)
    {
        // Definimos tres offsets simples para una patrulla inicial.
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(3f, 0f, 0f),
            new Vector3(-2f, 0f, 2.5f),
            new Vector3(-2f, 0f, -2.5f)
        };

        // Creamos el array final de puntos.
        Transform[] puntos = new Transform[offsets.Length];

        // Recorremos cada offset configurado.
        for (int indiceOffset = 0; indiceOffset < offsets.Length; indiceOffset++)
        {
            // Creamos el objeto del punto.
            GameObject punto = new GameObject("PuntoPatrulla_" + (indiceOffset + 1));

            // Lo parentamos al contenedor.
            punto.transform.SetParent(contenedor);

            // Lo movemos al lugar deseado.
            punto.transform.position = centro + offsets[indiceOffset];

            // Guardamos su transform en el array.
            puntos[indiceOffset] = punto.transform;
        }

        // Devolvemos los tres puntos creados.
        return puntos;
    }

    // COPILOT-EXPAND: Aqui podes agregar menus para patrullas circulares, defensivas y rutas de guardias complejas.
}
