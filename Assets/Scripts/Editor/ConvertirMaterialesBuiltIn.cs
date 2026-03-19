using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script convierte materiales de los packs importados desde shaders URP/SRP
// o shaders personalizados a un fallback Built-in/Standard compatible con el proyecto.

// Esta clase agrega un menu para convertir de una sola vez todos los materiales de Polytope Studio.
public static class ConvertirMaterialesBuiltIn
{
    // Esta opcion de menu procesa toda la carpeta de Polytope Studio.
    [MenuItem("Realm Brawl/Setup/Convertir Materiales")]
    public static void ConvertirMateriales()
    {
        // Definimos las carpetas raiz que queremos procesar.
        string[] carpetasObjetivo = new string[]
        {
            "Assets/Polytope Studio/Lowpoly_Characters",
            "Assets/Polytope Studio/Lowpoly_Environments",
            "Assets/Polytope Studio/Lowpoly_Weapons",
            "Assets/Polytope Studio/Lowpoly_Village",
            "Assets/Polytope Studio/Lowpoly_Props",
            "Assets/Polytope Studio/Lowpoly_Demos"
        };

        int materialesProcesados = 0;
        int materialesConvertidos = 0;
        int materialesOmitidos = 0;

        for (int indiceCarpeta = 0; indiceCarpeta < carpetasObjetivo.Length; indiceCarpeta++)
        {
            string carpetaActual = carpetasObjetivo[indiceCarpeta];
            if (!AssetDatabase.IsValidFolder(carpetaActual))
            {
                continue;
            }

            string[] guidsMateriales = AssetDatabase.FindAssets("t:Material", new[] { carpetaActual });
            for (int indiceMaterial = 0; indiceMaterial < guidsMateriales.Length; indiceMaterial++)
            {
                string rutaMaterial = AssetDatabase.GUIDToAssetPath(guidsMateriales[indiceMaterial]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
                if (material == null)
                {
                    continue;
                }

                materialesProcesados++;
                if (ConvertirMaterialSiHaceFalta(material, rutaMaterial))
                {
                    materialesConvertidos++;
                }
                else
                {
                    materialesOmitidos++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Realm Brawl -> Convertir Materiales terminado. Procesados: " + materialesProcesados + " | Convertidos: " + materialesConvertidos + " | Omitidos: " + materialesOmitidos);
    }

    // Este metodo decide si un material necesita conversion y la ejecuta.
    private static bool ConvertirMaterialSiHaceFalta(Material material, string rutaMaterial)
    {
        if (material == null)
        {
            return false;
        }

        if (!DebeConvertirse(material))
        {
            Debug.Log("Material omitido (ya compatible): " + rutaMaterial);
            return false;
        }

        Texture texturaBase = LeerTextura(material, "_BaseMap", "_MainTex");
        Color colorBase = LeerColor(material, "_BaseColor", "_Color", Color.white);
        Texture texturaNormal = LeerTextura(material, "_BumpMap");
        Texture texturaEmision = LeerTextura(material, "_EmissionMap");
        Color colorEmision = LeerColor(material, "_EmissionColor", Color.black);
        Texture texturaOcclusion = LeerTextura(material, "_OcclusionMap");
        float cutoff = LeerFloat(material, 0.5f, "_Cutoff", "_AlphaCutoff");
        float metallic = LeerFloat(material, 0f, "_Metallic");
        float smoothness = LeerFloat(material, 0.5f, "_Smoothness", "_Glossiness");

        Shader shaderDestino = ElegirShaderDestino(material);
        if (shaderDestino == null)
        {
            Debug.LogWarning("No encontre shader Built-in destino para: " + rutaMaterial);
            return false;
        }

        material.shader = shaderDestino;

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texturaBase);
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetTextureOffset("_MainTex", Vector2.zero);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texturaBase);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", colorBase);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", colorBase);
        }

        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", texturaNormal);
            material.DisableKeyword("_NORMALMAP");
            if (texturaNormal != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
        }

        if (material.HasProperty("_EmissionMap"))
        {
            material.SetTexture("_EmissionMap", texturaEmision);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", colorEmision);
        }

        if (material.HasProperty("_OcclusionMap"))
        {
            material.SetTexture("_OcclusionMap", texturaOcclusion);
        }

        if (material.HasProperty("_Cutoff"))
        {
            material.SetFloat("_Cutoff", cutoff);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        ConfigurarModoStandard(material, colorBase.a);

        Debug.Log("Material convertido a Built-in: " + rutaMaterial + " -> " + material.shader.name);
        EditorUtility.SetDirty(material);
        return true;
    }

    // Este metodo decide si el material viene de URP/SRP o de un shader custom.
    private static bool DebeConvertirse(Material material)
    {
        if (material.shader == null)
        {
            return true;
        }

        string nombreShader = material.shader.name ?? string.Empty;
        string rutaShader = AssetDatabase.GetAssetPath(material.shader) ?? string.Empty;

        if (nombreShader == "Standard" ||
            nombreShader.StartsWith("Legacy Shaders/") ||
            nombreShader.StartsWith("Skybox/") ||
            nombreShader.StartsWith("Nature/") ||
            nombreShader.StartsWith("UI/") ||
            nombreShader.StartsWith("Unlit/") ||
            nombreShader.StartsWith("Sprites/"))
        {
            return false;
        }

        if (rutaShader.Contains("Polytope Studio") ||
            nombreShader.Contains("Universal Render Pipeline") ||
            nombreShader.Contains("URP") ||
            nombreShader.Contains("SRP") ||
            nombreShader.Contains("Pipeline"))
        {
            return true;
        }

        return !string.IsNullOrEmpty(rutaShader);
    }

    // Este metodo elige el shader Built-in destino mas seguro.
    private static Shader ElegirShaderDestino(Material material)
    {
        if (material.name.IndexOf("skybox", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Shader shaderSkybox = Shader.Find("Skybox/Procedural");
            if (shaderSkybox != null)
            {
                return shaderSkybox;
            }
        }

        return Shader.Find("Standard");
    }

    private static Texture LeerTextura(Material material, params string[] nombresPropiedad)
    {
        for (int indice = 0; indice < nombresPropiedad.Length; indice++)
        {
            string nombre = nombresPropiedad[indice];
            if (material.HasProperty(nombre))
            {
                return material.GetTexture(nombre);
            }
        }

        return null;
    }

    private static Color LeerColor(Material material, string nombrePrimario, string nombreSecundario, Color colorPorDefecto)
    {
        if (material.HasProperty(nombrePrimario))
        {
            return material.GetColor(nombrePrimario);
        }

        if (material.HasProperty(nombreSecundario))
        {
            return material.GetColor(nombreSecundario);
        }

        return colorPorDefecto;
    }

    private static float LeerFloat(Material material, float valorPorDefecto, params string[] nombresPropiedad)
    {
        for (int indice = 0; indice < nombresPropiedad.Length; indice++)
        {
            string nombre = nombresPropiedad[indice];
            if (material.HasProperty(nombre))
            {
                return material.GetFloat(nombre);
            }
        }

        return valorPorDefecto;
    }

    private static void ConfigurarModoStandard(Material material, float alphaBase)
    {
        bool esTransparente = alphaBase < 0.99f ||
            material.name.IndexOf("transparent", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            material.name.IndexOf("fade", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (!esTransparente)
        {
            material.SetFloat("_Mode", 0f);
            material.renderQueue = -1;
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return;
        }

        material.SetFloat("_Mode", 2f);
        material.renderQueue = 3000;
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }
}
