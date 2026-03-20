using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script ayuda a diagnosticar y limpiar componentes faltantes en la escena activa,
// algo util cuando Unity muestra "The referenced script (Unknown) on this Behaviour is missing!".

// Esta clase expone utilidades de editor para encontrar y borrar Missing Scripts.
public static class LimpiarScriptsFaltantes
{
    // Este menu recorre la escena activa y solo informa donde hay scripts faltantes.
    [MenuItem("Realm Brawl/Setup/Diagnosticar Scripts Faltantes")]
    public static void DiagnosticarScriptsFaltantes()
    {
        Scene escenaActiva = SceneManager.GetActiveScene();
        if (!escenaActiva.IsValid())
        {
            Debug.LogWarning("[LimpiarScriptsFaltantes] No hay una escena activa valida para diagnosticar.");
            return;
        }

        int totalFaltantes = 0;
        GameObject[] raices = escenaActiva.GetRootGameObjects();
        for (int indiceRaiz = 0; indiceRaiz < raices.Length; indiceRaiz++)
        {
            totalFaltantes += DiagnosticarEnJerarquia(raices[indiceRaiz].transform);
        }

        Debug.Log("[LimpiarScriptsFaltantes] Diagnostico completo. Missing Scripts encontrados: " + totalFaltantes + ".");
    }

    // Este menu elimina todos los Missing Scripts de la escena activa.
    [MenuItem("Realm Brawl/Setup/Limpiar Scripts Faltantes")]
    public static void LimpiarScriptsFaltantesEnEscena()
    {
        Scene escenaActiva = SceneManager.GetActiveScene();
        if (!escenaActiva.IsValid())
        {
            Debug.LogWarning("[LimpiarScriptsFaltantes] No hay una escena activa valida para limpiar.");
            return;
        }

        int totalEliminados = 0;
        GameObject[] raices = escenaActiva.GetRootGameObjects();
        for (int indiceRaiz = 0; indiceRaiz < raices.Length; indiceRaiz++)
        {
            totalEliminados += LimpiarEnJerarquia(raices[indiceRaiz].transform);
        }

        if (totalEliminados > 0)
        {
            EditorSceneManager.MarkSceneDirty(escenaActiva);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LimpiarScriptsFaltantes] Limpieza completa. Missing Scripts eliminados: " + totalEliminados + ".");
    }

    // Este menu recorre todos los prefabs del proyecto y avisa donde hay scripts faltantes.
    [MenuItem("Realm Brawl/Setup/Diagnosticar Scripts Faltantes En Prefabs")]
    public static void DiagnosticarScriptsFaltantesEnPrefabs()
    {
        string[] guidsPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int totalFaltantes = 0;

        for (int indicePrefab = 0; indicePrefab < guidsPrefabs.Length; indicePrefab++)
        {
            string rutaPrefab = AssetDatabase.GUIDToAssetPath(guidsPrefabs[indicePrefab]);
            if (string.IsNullOrWhiteSpace(rutaPrefab))
            {
                continue;
            }

            GameObject raizPrefab = PrefabUtility.LoadPrefabContents(rutaPrefab);
            if (raizPrefab == null)
            {
                continue;
            }

            int faltantesEnPrefab = DiagnosticarEnJerarquia(raizPrefab.transform);
            if (faltantesEnPrefab > 0)
            {
                totalFaltantes += faltantesEnPrefab;
                Debug.LogWarning("[LimpiarScriptsFaltantes] Missing Scripts en prefab: " + rutaPrefab + " | cantidad: " + faltantesEnPrefab, raizPrefab);
            }

            PrefabUtility.UnloadPrefabContents(raizPrefab);
        }

        Debug.Log("[LimpiarScriptsFaltantes] Diagnostico de prefabs completo. Missing Scripts encontrados: " + totalFaltantes + ".");
    }

    // Este menu limpia scripts faltantes tanto en la escena activa como en todos los prefabs del proyecto.
    [MenuItem("Realm Brawl/Setup/Limpiar Scripts Faltantes En Prefabs")]
    public static void LimpiarScriptsFaltantesEnPrefabs()
    {
        string[] guidsPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int totalEliminados = 0;

        for (int indicePrefab = 0; indicePrefab < guidsPrefabs.Length; indicePrefab++)
        {
            string rutaPrefab = AssetDatabase.GUIDToAssetPath(guidsPrefabs[indicePrefab]);
            if (string.IsNullOrWhiteSpace(rutaPrefab))
            {
                continue;
            }

            GameObject raizPrefab = PrefabUtility.LoadPrefabContents(rutaPrefab);
            if (raizPrefab == null)
            {
                continue;
            }

            int eliminadosEnPrefab = LimpiarEnJerarquia(raizPrefab.transform);
            if (eliminadosEnPrefab > 0)
            {
                totalEliminados += eliminadosEnPrefab;
                PrefabUtility.SaveAsPrefabAsset(raizPrefab, rutaPrefab);
                Debug.Log("[LimpiarScriptsFaltantes] Missing Scripts eliminados en prefab: " + rutaPrefab + " | cantidad: " + eliminadosEnPrefab, raizPrefab);
            }

            PrefabUtility.UnloadPrefabContents(raizPrefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LimpiarScriptsFaltantes] Limpieza de prefabs completa. Missing Scripts eliminados: " + totalEliminados + ".");
    }

    // Este metodo recorre una jerarquia y reporta cuantos Missing Scripts hay.
    private static int DiagnosticarEnJerarquia(Transform raiz)
    {
        if (raiz == null)
        {
            return 0;
        }

        int encontrados = 0;
        Stack<Transform> pendientes = new Stack<Transform>();
        pendientes.Push(raiz);

        while (pendientes.Count > 0)
        {
            Transform actual = pendientes.Pop();
            if (actual == null)
            {
                continue;
            }

            GameObject objetoActual = actual.gameObject;
            int faltantesEnObjeto = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objetoActual);
            if (faltantesEnObjeto > 0)
            {
                encontrados += faltantesEnObjeto;
                Debug.LogWarning("[LimpiarScriptsFaltantes] Missing Script en: " + ObtenerRutaJerarquia(actual) + " | cantidad: " + faltantesEnObjeto, objetoActual);
            }

            for (int indiceHijo = 0; indiceHijo < actual.childCount; indiceHijo++)
            {
                pendientes.Push(actual.GetChild(indiceHijo));
            }
        }

        return encontrados;
    }

    // Este metodo recorre una jerarquia y elimina Missing Scripts.
    private static int LimpiarEnJerarquia(Transform raiz)
    {
        if (raiz == null)
        {
            return 0;
        }

        int eliminados = 0;
        Stack<Transform> pendientes = new Stack<Transform>();
        pendientes.Push(raiz);

        while (pendientes.Count > 0)
        {
            Transform actual = pendientes.Pop();
            if (actual == null)
            {
                continue;
            }

            GameObject objetoActual = actual.gameObject;
            int faltantesEnObjeto = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objetoActual);
            if (faltantesEnObjeto > 0)
            {
                Undo.RegisterCompleteObjectUndo(objetoActual, "Limpiar Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(objetoActual);
                eliminados += faltantesEnObjeto;
                Debug.Log("[LimpiarScriptsFaltantes] Missing Scripts eliminados en: " + ObtenerRutaJerarquia(actual) + " | cantidad: " + faltantesEnObjeto, objetoActual);
            }

            for (int indiceHijo = 0; indiceHijo < actual.childCount; indiceHijo++)
            {
                pendientes.Push(actual.GetChild(indiceHijo));
            }
        }

        return eliminados;
    }

    // Este metodo arma la ruta de jerarquia para ubicar rapido el objeto en escena.
    private static string ObtenerRutaJerarquia(Transform actual)
    {
        if (actual == null)
        {
            return "(nulo)";
        }

        string ruta = actual.name;
        Transform cursor = actual.parent;
        while (cursor != null)
        {
            ruta = cursor.name + "/" + ruta;
            cursor = cursor.parent;
        }

        return ruta;
    }
}
