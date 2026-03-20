using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
    // Estas propiedades cubren las texturas base mas comunes que usan los packs Polytope.
    private static readonly string[] NombresPropiedadTexturaBase =
    {
        "_BaseMap",
        "_MainTex",
        "_BaseTexture",
        "_Texture0",
        "_TextureSample0",
        "_TextureSample2",
        "_TextureSample9",
        "_Exteriorwallstexture",
        "_Interiorwallstexture"
    };

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

        if (EsMaterialVariant(rutaMaterial))
        {
            Debug.LogWarning("Material omitido (variant de material, requiere conversion manual o del material padre): " + rutaMaterial);
            return false;
        }

        bool necesitaReparacionStandard = NecesitaReparacionMaterialStandard(material, rutaMaterial);
        if (!DebeConvertirse(material) && !necesitaReparacionStandard)
        {
            Debug.Log("Material omitido (ya compatible): " + rutaMaterial);
            return false;
        }

        string nombrePropiedadTexturaBase = string.Empty;
        Vector2 escalaTexturaBase = Vector2.one;
        Vector2 offsetTexturaBase = Vector2.zero;
        Texture texturaBase = LeerTexturaBaseCompatible(material, rutaMaterial, out nombrePropiedadTexturaBase, out escalaTexturaBase, out offsetTexturaBase);
        Color colorBase = LeerColor(material, "_BaseColor", "_Color", Color.white);
        Texture texturaNormal = LeerTextura(material, "_BumpMap");
        Texture texturaEmision = LeerTextura(material, "_EmissionMap");
        Color colorEmision = LeerColor(material, "_EmissionColor", "_Color", Color.black);
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
            material.SetTextureScale("_MainTex", escalaTexturaBase);
            material.SetTextureOffset("_MainTex", offsetTexturaBase);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texturaBase);
            material.SetTextureScale("_BaseMap", escalaTexturaBase);
            material.SetTextureOffset("_BaseMap", offsetTexturaBase);
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

        if (material.shader != null && material.shader.name == "Standard")
        {
            ConfigurarModoStandard(material, colorBase.a);
        }

        if (necesitaReparacionStandard && !DebeConvertirse(material))
        {
            Debug.Log("Material reparado para Built-in: " + rutaMaterial + " -> " + material.shader.name);
        }
        else
        {
            Debug.Log("Material convertido a Built-in: " + rutaMaterial + " -> " + material.shader.name);
        }

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

    private static Texture LeerTextura(Material material, out string nombrePropiedadEncontrado, params string[] nombresPropiedad)
    {
        nombrePropiedadEncontrado = string.Empty;

        for (int indice = 0; indice < nombresPropiedad.Length; indice++)
        {
            string nombre = nombresPropiedad[indice];
            if (material.HasProperty(nombre))
            {
                nombrePropiedadEncontrado = nombre;
                return material.GetTexture(nombre);
            }
        }

        return null;
    }

    private static Texture LeerTexturaBaseCompatible(Material material, string rutaMaterial, out string nombrePropiedadEncontrado, out Vector2 escalaTextura, out Vector2 offsetTextura)
    {
        nombrePropiedadEncontrado = string.Empty;
        escalaTextura = Vector2.one;
        offsetTextura = Vector2.zero;

        Texture texturaDesdeShader = LeerTextura(material, out nombrePropiedadEncontrado, NombresPropiedadTexturaBase);
        if (texturaDesdeShader != null)
        {
            if (material.HasProperty(nombrePropiedadEncontrado))
            {
                escalaTextura = material.GetTextureScale(nombrePropiedadEncontrado);
                offsetTextura = material.GetTextureOffset(nombrePropiedadEncontrado);
            }

            return texturaDesdeShader;
        }

        return LeerTexturaDesdeArchivoMaterial(rutaMaterial, out nombrePropiedadEncontrado, out escalaTextura, out offsetTextura, NombresPropiedadTexturaBase);
    }

    private static Texture LeerTextura(Material material, params string[] nombresPropiedad)
    {
        string nombreIgnorado;
        return LeerTextura(material, out nombreIgnorado, nombresPropiedad);
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

    private static void CopiarEscalaYDesplazamientoTextura(Material material, string propiedadOrigen, string propiedadDestino)
    {
        if (material == null || string.IsNullOrEmpty(propiedadOrigen) || string.IsNullOrEmpty(propiedadDestino))
        {
            return;
        }

        if (!material.HasProperty(propiedadOrigen) || !material.HasProperty(propiedadDestino))
        {
            return;
        }

        material.SetTextureScale(propiedadDestino, material.GetTextureScale(propiedadOrigen));
        material.SetTextureOffset(propiedadDestino, material.GetTextureOffset(propiedadOrigen));
    }

    private static bool NecesitaReparacionMaterialStandard(Material material, string rutaMaterial)
    {
        if (material == null || material.shader == null)
        {
            return false;
        }

        if (material.shader.name != "Standard")
        {
            return false;
        }

        if (material.mainTexture != null)
        {
            return false;
        }

        string nombrePropiedad;
        Vector2 escala;
        Vector2 offset;
        Texture texturaRecuperada = LeerTexturaDesdeArchivoMaterial(rutaMaterial, out nombrePropiedad, out escala, out offset, NombresPropiedadTexturaBase);
        return texturaRecuperada != null;
    }

    private static Texture LeerTexturaDesdeArchivoMaterial(string rutaMaterial, out string nombrePropiedadEncontrado, out Vector2 escalaTextura, out Vector2 offsetTextura, params string[] nombresPropiedad)
    {
        nombrePropiedadEncontrado = string.Empty;
        escalaTextura = Vector2.one;
        offsetTextura = Vector2.zero;

        if (string.IsNullOrEmpty(rutaMaterial) || !File.Exists(rutaMaterial))
        {
            return null;
        }

        string[] lineas = File.ReadAllLines(rutaMaterial);
        for (int indiceNombre = 0; indiceNombre < nombresPropiedad.Length; indiceNombre++)
        {
            string nombrePropiedad = nombresPropiedad[indiceNombre];
            Texture texturaEncontrada = BuscarTexturaEnLineas(lineas, nombrePropiedad, out escalaTextura, out offsetTextura);
            if (texturaEncontrada != null)
            {
                nombrePropiedadEncontrado = nombrePropiedad;
                return texturaEncontrada;
            }
        }

        return null;
    }

    private static Texture BuscarTexturaEnLineas(string[] lineas, string nombrePropiedad, out Vector2 escalaTextura, out Vector2 offsetTextura)
    {
        escalaTextura = Vector2.one;
        offsetTextura = Vector2.zero;

        if (lineas == null || lineas.Length == 0 || string.IsNullOrEmpty(nombrePropiedad))
        {
            return null;
        }

        for (int indiceLinea = 0; indiceLinea < lineas.Length; indiceLinea++)
        {
            string lineaActual = lineas[indiceLinea]?.Trim();
            if (lineaActual != "- " + nombrePropiedad + ":")
            {
                continue;
            }

            string guidTextura = string.Empty;

            for (int indiceBloque = indiceLinea + 1; indiceBloque < lineas.Length; indiceBloque++)
            {
                string lineaBloque = lineas[indiceBloque]?.Trim();
                if (string.IsNullOrEmpty(lineaBloque))
                {
                    continue;
                }

                if (lineaBloque.StartsWith("- "))
                {
                    break;
                }

                if (lineaBloque.StartsWith("m_Texture:"))
                {
                    guidTextura = ExtraerGuid(lineaBloque);
                }
                else if (lineaBloque.StartsWith("m_Scale:"))
                {
                    escalaTextura = ExtraerVector2(lineaBloque, Vector2.one);
                }
                else if (lineaBloque.StartsWith("m_Offset:"))
                {
                    offsetTextura = ExtraerVector2(lineaBloque, Vector2.zero);
                }
            }

            if (string.IsNullOrEmpty(guidTextura))
            {
                return null;
            }

            string rutaTextura = AssetDatabase.GUIDToAssetPath(guidTextura);
            if (string.IsNullOrEmpty(rutaTextura))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture>(rutaTextura);
        }

        return null;
    }

    private static string ExtraerGuid(string linea)
    {
        Match match = Regex.Match(linea, @"guid:\s*([a-fA-F0-9]+)");
        if (!match.Success)
        {
            return string.Empty;
        }

        return match.Groups[1].Value;
    }

    private static Vector2 ExtraerVector2(string linea, Vector2 valorPorDefecto)
    {
        Match match = Regex.Match(linea, @"x:\s*([-+]?[0-9]*\.?[0-9]+),\s*y:\s*([-+]?[0-9]*\.?[0-9]+)");
        if (!match.Success)
        {
            return valorPorDefecto;
        }

        float x;
        float y;
        if (!float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x))
        {
            x = valorPorDefecto.x;
        }

        if (!float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y))
        {
            y = valorPorDefecto.y;
        }

        return new Vector2(x, y);
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

    private static bool EsMaterialVariant(string rutaMaterial)
    {
        if (string.IsNullOrEmpty(rutaMaterial) || !File.Exists(rutaMaterial))
        {
            return false;
        }

        using (StreamReader lector = new StreamReader(rutaMaterial))
        {
            while (!lector.EndOfStream)
            {
                string linea = lector.ReadLine();
                if (string.IsNullOrEmpty(linea))
                {
                    continue;
                }

                string lineaSinEspacios = linea.Trim();
                if (!lineaSinEspacios.StartsWith("m_Parent:"))
                {
                    continue;
                }

                return !lineaSinEspacios.Contains("{fileID: 0}");
            }
        }

        return false;
    }
}
