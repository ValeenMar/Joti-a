using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor corrige materiales importados de los packs de Polytope Studio
// para que se vean bien en Built-in Render Pipeline con Standard shader.

// Esta clase centraliza varios arreglos visuales para que se ejecuten desde un menu.
public static class PulirMateriales
{
    // Estas palabras ayudan a detectar materiales de hojas, arboles y follaje.
    private static readonly string[] PalabrasClaveHojas =
    {
        "leaf",
        "leave",
        "leaves",
        "hoja",
        "foliage",
        "tree",
        "arbol",
        "bush",
        "bark",
        "copa"
    };

    // Estas palabras indican materiales que deben quedarse opacos aunque pertenezcan a arboles.
    private static readonly string[] PalabrasClaveExcluirCutout =
    {
        "trunk",
        "stump",
        "log",
        "mushroom",
        "root",
        "roots"
    };

    // Estas palabras ayudan a detectar esqueletos o enemigos esqueletos.
    private static readonly string[] PalabrasClaveEsqueleto =
    {
        "skeleton",
        "esqueleto",
        "enemy",
        "enemigo"
    };

    // Estas palabras ayudan a detectar el personaje jugador y sus materiales.
    private static readonly string[] PalabrasClaveJugador =
    {
        "player",
        "jugador",
        "hero",
        "knight",
        "character"
    };

    // Estas palabras ayudan a detectar props de madera y elementos de soporte visual.
    private static readonly string[] PalabrasClaveMadera =
    {
        "post",
        "fence",
        "torch",
        "antorcha",
        "poste"
    };

    // Esta opcion aplica el arreglo de hojas y follaje en toda la escena.
    [MenuItem("Realm Brawl/Setup/Pulir Materiales")]
    public static void PulirMaterialesHojas()
    {
        int materialesCorregidos = 0;
        HashSet<Material> materialesProcesados = new HashSet<Material>();

        Renderer[] renderers = ObtenerRenderersEscena();
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            Renderer renderer = renderers[indiceRenderer];
            if (!EsRendererValido(renderer))
            {
                continue;
            }

            Material[] materiales = renderer.sharedMaterials;
            if (materiales == null || materiales.Length == 0)
            {
                continue;
            }

            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null || materialesProcesados.Contains(material))
                {
                    continue;
                }

                if (!DebePulirseComoHojas(material, renderer))
                {
                    continue;
                }

                if (AplicarAlphaCutoutHojas(material, renderer))
                {
                    materialesCorregidos++;
                    materialesProcesados.Add(material);
                }
            }
        }

        GuardarYRefrescar();
        Debug.Log("[PulirMateriales] Hojas corregidas: " + materialesCorregidos);
    }

    // Esta opcion corrige el color y el shader de los esqueletos enemigos.
    [MenuItem("Realm Brawl/Setup/Arreglar Esqueletos")]
    public static void ArreglarEsqueletos()
    {
        int materialesCorregidos = 0;
        HashSet<Material> materialesProcesados = new HashSet<Material>();

        SkinnedMeshRenderer[] renderers = ObtenerSkinnedMeshRenderersEscena();
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            SkinnedMeshRenderer renderer = renderers[indiceRenderer];
            if (!EsRendererValido(renderer))
            {
                continue;
            }

            string nombreRaiz = ObtenerNombreRaiz(renderer.transform);
            bool coincideNombre = ContienePalabraClave(nombreRaiz, PalabrasClaveEsqueleto) || ContienePalabraClave(renderer.gameObject.name, PalabrasClaveEsqueleto);

            Material[] materiales = renderer.sharedMaterials;
            if (materiales == null || materiales.Length == 0)
            {
                continue;
            }

            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null || materialesProcesados.Contains(material))
                {
                    continue;
                }

                bool colorAmarillo = EsColorAmarillo(material);
                if (!coincideNombre && !colorAmarillo)
                {
                    continue;
                }

                if (ArreglarMaterialEsqueleto(material, renderer))
                {
                    materialesCorregidos++;
                    materialesProcesados.Add(material);
                }
            }
        }

        GuardarYRefrescar();
        Debug.Log("[PulirMateriales] Esqueletos corregidos: " + materialesCorregidos);
    }

    // Esta opcion corrige el color y el shader del jugador.
    [MenuItem("Realm Brawl/Setup/Arreglar Jugador")]
    public static void ArreglarJugador()
    {
        int materialesCorregidos = 0;
        HashSet<Material> materialesProcesados = new HashSet<Material>();

        SkinnedMeshRenderer[] renderers = ObtenerSkinnedMeshRenderersEscena();
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            SkinnedMeshRenderer renderer = renderers[indiceRenderer];
            if (!EsRendererValido(renderer))
            {
                continue;
            }

            string nombreRaiz = ObtenerNombreRaiz(renderer.transform);
            bool coincideNombre = ContienePalabraClave(nombreRaiz, PalabrasClaveJugador) || ContienePalabraClave(renderer.gameObject.name, PalabrasClaveJugador);
            if (!coincideNombre)
            {
                continue;
            }

            Material[] materiales = renderer.sharedMaterials;
            if (materiales == null || materiales.Length == 0)
            {
                continue;
            }

            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null || materialesProcesados.Contains(material))
                {
                    continue;
                }

                if (ArreglarMaterialJugador(material, renderer))
                {
                    materialesCorregidos++;
                    materialesProcesados.Add(material);
                }
            }
        }

        GuardarYRefrescar();
        Debug.Log("[PulirMateriales] Jugador corregido: " + materialesCorregidos);
    }

    // Esta opcion corrige props de madera como postes, vallas y antorchas.
    [MenuItem("Realm Brawl/Setup/Arreglar Props Madera")]
    public static void ArreglarPropsMadera()
    {
        int materialesCorregidos = 0;
        HashSet<Material> materialesProcesados = new HashSet<Material>();

        Renderer[] renderers = ObtenerRenderersEscena();
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            Renderer renderer = renderers[indiceRenderer];
            if (!EsRendererValido(renderer))
            {
                continue;
            }

            string nombreRaiz = ObtenerNombreRaiz(renderer.transform);
            bool coincideNombre = ContienePalabraClave(nombreRaiz, PalabrasClaveMadera) ||
                                  ContienePalabraClave(renderer.gameObject.name, PalabrasClaveMadera);

            Material[] materiales = renderer.sharedMaterials;
            if (materiales == null || materiales.Length == 0)
            {
                continue;
            }

            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null || materialesProcesados.Contains(material))
                {
                    continue;
                }

                bool nombreSospechoso = coincideNombre || ContienePalabraClave(material.name, PalabrasClaveMadera);
                if (!nombreSospechoso)
                {
                    continue;
                }

                if (ArreglarMaterialMadera(material, renderer))
                {
                    materialesCorregidos++;
                    materialesProcesados.Add(material);
                }
            }
        }

        GuardarYRefrescar();
        Debug.Log("[PulirMateriales] Props de madera corregidos: " + materialesCorregidos);
    }

    // Esta opcion ejecuta los tres arreglos seguidos para dejar todo listo de una vez.
    [MenuItem("Realm Brawl/Setup/⚡ Arreglar TODO")]
    [MenuItem("Realm Brawl/Setup/Arreglar TODO")]
    public static void ArreglarTodo()
    {
        PulirMaterialesHojas();
        ArreglarEsqueletos();
        ArreglarJugador();
        ArreglarPropsMadera();
        Debug.Log("[PulirMateriales] Arreglar TODO completado.");
    }

    // Este metodo aplica Standard en modo cutout y busca textura si el material no la tiene.
    private static bool AplicarAlphaCutoutHojas(Material material, Renderer renderer)
    {
        if (material == null)
        {
            return false;
        }

        Shader shaderStandard = Shader.Find("Standard");
        if (shaderStandard == null)
        {
            Debug.LogWarning("[PulirMateriales] No se encontro el shader Standard para " + material.name);
            return false;
        }

        material.shader = shaderStandard;

        if (material.mainTexture == null)
        {
            Texture2D texturaEncontrada = BuscarTexturaPorNombre(material, renderer);
            if (texturaEncontrada != null)
            {
                material.mainTexture = texturaEncontrada;
            }
        }

        material.SetFloat("_Mode", 1f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 2450;
        material.SetFloat("_Cutoff", 0.35f);
        material.SetOverrideTag("RenderType", "TransparentCutout");

        EditorUtility.SetDirty(material);
        Debug.Log("[PulirMateriales] Material de hoja corregido: " + material.name + " | Renderer: " + renderer.name);
        return true;
    }

    // Este metodo aplica un look de madera creible a props como postes, vallas o antorchas.
    private static bool ArreglarMaterialMadera(Material material, Renderer renderer)
    {
        if (material == null)
        {
            return false;
        }

        Shader shaderStandard = Shader.Find("Standard");
        if (shaderStandard == null)
        {
            Debug.LogWarning("[PulirMateriales] No se encontro el shader Standard para " + material.name);
            return false;
        }

        material.shader = shaderStandard;
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetFloat("_Glossiness", 0.08f);
        material.SetFloat("_Metallic", 0.0f);

        Texture2D texturaEncontrada = BuscarTexturaPorNombre(material, renderer);
        if (texturaEncontrada != null)
        {
            material.mainTexture = texturaEncontrada;
            material.color = Color.white;
        }
        else
        {
            material.color = new Color(0.45f, 0.32f, 0.18f, 1f);
        }

        EditorUtility.SetDirty(material);
        Debug.Log("[PulirMateriales] Prop de madera corregido: " + material.name + " | Renderer: " + renderer.name);
        return true;
    }

    // Este metodo decide si un material realmente deberia tratarse como follaje con alpha cutout.
    private static bool DebePulirseComoHojas(Material material, Renderer renderer)
    {
        if (material == null || renderer == null)
        {
            return false;
        }

        string nombreMaterial = material.name;
        string nombreRenderer = renderer.gameObject != null ? renderer.gameObject.name : string.Empty;
        string nombreRaiz = ObtenerNombreRaiz(renderer.transform);

        bool coincideConHojas =
            ContienePalabraClave(nombreMaterial, PalabrasClaveHojas) ||
            ContienePalabraClave(nombreRenderer, PalabrasClaveHojas) ||
            ContienePalabraClave(nombreRaiz, PalabrasClaveHojas);

        if (!coincideConHojas)
        {
            return false;
        }

        bool coincideConExclusion =
            ContienePalabraClave(nombreMaterial, PalabrasClaveExcluirCutout) ||
            ContienePalabraClave(nombreRenderer, PalabrasClaveExcluirCutout) ||
            ContienePalabraClave(nombreRaiz, PalabrasClaveExcluirCutout);

        return !coincideConExclusion;
    }

    // Este metodo deja al esqueleto con un color creible y una configuracion Standard limpia.
    private static bool ArreglarMaterialEsqueleto(Material material, Renderer renderer)
    {
        if (material == null)
        {
            return false;
        }

        Shader shaderStandard = Shader.Find("Standard");
        if (shaderStandard == null)
        {
            Debug.LogWarning("[PulirMateriales] No se encontro el shader Standard para " + material.name);
            return false;
        }

        material.shader = shaderStandard;
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetFloat("_Glossiness", 0.1f);
        material.SetFloat("_Metallic", 0.0f);

        Texture2D texturaEncontrada = BuscarTexturaPorNombre(material, renderer);
        if (texturaEncontrada != null)
        {
            material.mainTexture = texturaEncontrada;
            material.color = Color.white;
        }
        else
        {
            material.color = new Color(0.85f, 0.80f, 0.70f, 1f);
        }

        EditorUtility.SetDirty(material);
        Debug.Log("[PulirMateriales] Esqueleto corregido: " + material.name + " | Renderer: " + renderer.name);
        return true;
    }

    // Este metodo deja al jugador con un color coherente segun el tipo de material.
    private static bool ArreglarMaterialJugador(Material material, Renderer renderer)
    {
        if (material == null)
        {
            return false;
        }

        Shader shaderStandard = Shader.Find("Standard");
        if (shaderStandard == null)
        {
            Debug.LogWarning("[PulirMateriales] No se encontro el shader Standard para " + material.name);
            return false;
        }

        material.shader = shaderStandard;
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetFloat("_Glossiness", 0.35f);
        material.SetFloat("_Metallic", 0.1f);

        Texture2D texturaEncontrada = BuscarTexturaPorNombre(material, renderer);
        if (texturaEncontrada != null)
        {
            material.mainTexture = texturaEncontrada;
            material.color = Color.white;
        }
        else
        {
            string nombreMaterial = material.name.ToLowerInvariant();
            if (nombreMaterial.Contains("metal") || nombreMaterial.Contains("armor") || nombreMaterial.Contains("armadura"))
            {
                material.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            }
            else if (nombreMaterial.Contains("skin") || nombreMaterial.Contains("piel") || nombreMaterial.Contains("face"))
            {
                material.color = new Color(0.9f, 0.75f, 0.65f, 1f);
            }
            else
            {
                material.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            }
        }

        EditorUtility.SetDirty(material);
        Debug.Log("[PulirMateriales] Jugador corregido: " + material.name + " | Renderer: " + renderer.name);
        return true;
    }

    // Este metodo busca todas las texturas del proyecto una sola vez.
    private static Texture2D[] CargarTodasLasTexturas()
    {
        List<Texture2D> texturas = new List<Texture2D>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");

        for (int indice = 0; indice < guids.Length; indice++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[indice]);
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (textura != null)
            {
                texturas.Add(textura);
            }
        }

        return texturas.ToArray();
    }

    // Este metodo intenta encontrar una textura que coincida con el material, el renderer o la raiz.
    private static Texture2D BuscarTexturaCoincidente(Texture2D[] texturas, params string[] nombresBusqueda)
    {
        if (texturas == null || texturas.Length == 0)
        {
            return null;
        }

        string[] terminosBusqueda = ConstruirTerminosBusqueda(nombresBusqueda);
        Texture2D mejorTextura = null;
        int mejorPuntuacion = 0;

        for (int indice = 0; indice < texturas.Length; indice++)
        {
            Texture2D textura = texturas[indice];
            if (textura == null)
            {
                continue;
            }

            string nombreNormalizado = NormalizarNombre(textura.name);
            string ruta = AssetDatabase.GetAssetPath(textura);
            string nombreRuta = NormalizarNombre(Path.GetFileNameWithoutExtension(ruta));
            int puntuacion = 0;

            for (int i = 0; i < terminosBusqueda.Length; i++)
            {
                string termino = terminosBusqueda[i];
                if (string.IsNullOrEmpty(termino))
                {
                    continue;
                }

                if (nombreNormalizado == termino || nombreRuta == termino)
                {
                    puntuacion = Mathf.Max(puntuacion, 100);
                }
                else if (nombreNormalizado.Contains(termino) || nombreRuta.Contains(termino) || termino.Contains(nombreNormalizado) || termino.Contains(nombreRuta))
                {
                    puntuacion = Mathf.Max(puntuacion, 70);
                }
                else if (CoincidePorPalabras(nombreNormalizado, termino) || CoincidePorPalabras(nombreRuta, termino))
                {
                    puntuacion = Mathf.Max(puntuacion, 40);
                }
            }

            if (puntuacion > mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejorTextura = textura;
            }
        }

        return mejorPuntuacion > 0 ? mejorTextura : null;
    }

    // Este metodo intenta encontrar una textura directamente en el proyecto usando AssetDatabase y palabras clave.
    private static Texture2D BuscarTexturaCoincidenteEnProyecto(params string[] nombresBusqueda)
    {
        string[] terminosBusqueda = ConstruirTerminosBusqueda(nombresBusqueda);
        if (terminosBusqueda.Length == 0)
        {
            return null;
        }

        HashSet<string> guidsProcesados = new HashSet<string>();
        Texture2D mejorTextura = null;
        int mejorPuntuacion = 0;

        for (int indiceTermino = 0; indiceTermino < terminosBusqueda.Length; indiceTermino++)
        {
            string termino = terminosBusqueda[indiceTermino];
            if (string.IsNullOrEmpty(termino))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets(termino + " t:Texture2D");
            for (int indiceGuid = 0; indiceGuid < guids.Length; indiceGuid++)
            {
                string guid = guids[indiceGuid];
                if (!guidsProcesados.Add(guid))
                {
                    continue;
                }

                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                if (textura == null)
                {
                    continue;
                }

                string nombreTextura = NormalizarNombre(textura.name);
                string nombreRuta = NormalizarNombre(Path.GetFileNameWithoutExtension(ruta));
                int puntuacion = 0;

                if (nombreTextura == termino || nombreRuta == termino)
                {
                    puntuacion = 100;
                }
                else if (nombreTextura.Contains(termino) || nombreRuta.Contains(termino) || termino.Contains(nombreTextura) || termino.Contains(nombreRuta))
                {
                    puntuacion = 70;
                }
                else if (CoincidePorPalabras(nombreTextura, termino) || CoincidePorPalabras(nombreRuta, termino))
                {
                    puntuacion = 40;
                }

                if (puntuacion > mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    mejorTextura = textura;
                }
            }
        }

        return mejorPuntuacion > 0 ? mejorTextura : null;
    }

    // Este metodo arma una lista de terminos limpios para comparar nombres.
    private static string[] ConstruirTerminosBusqueda(IEnumerable<string> nombres)
    {
        HashSet<string> terminos = new HashSet<string>();
        if (nombres == null)
        {
            return Array.Empty<string>();
        }

        foreach (string nombre in nombres)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                continue;
            }

            string normalizado = NormalizarNombre(nombre);
            if (string.IsNullOrEmpty(normalizado))
            {
                continue;
            }

            terminos.Add(normalizado);
        }

        return new List<string>(terminos).ToArray();
    }

    // Este metodo deja el texto listo para comparaciones simples.
    private static string NormalizarNombre(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(texto.Length);
        for (int indice = 0; indice < texto.Length; indice++)
        {
            char caracter = char.ToLowerInvariant(texto[indice]);
            if (char.IsLetterOrDigit(caracter))
            {
                builder.Append(caracter);
            }
        }

        return builder.ToString();
    }

    // Este metodo compara si dos nombres comparten alguna palabra util.
    private static bool CoincidePorPalabras(string textoBase, string termino)
    {
        if (string.IsNullOrEmpty(textoBase) || string.IsNullOrEmpty(termino))
        {
            return false;
        }

        return textoBase.Contains(termino) || termino.Contains(textoBase);
    }

    // Este metodo revisa si un texto contiene cualquiera de las palabras clave.
    private static bool ContienePalabraClave(string texto, string[] palabrasClave)
    {
        if (string.IsNullOrEmpty(texto) || palabrasClave == null)
        {
            return false;
        }

        string textoNormalizado = NormalizarNombre(texto);
        for (int indice = 0; indice < palabrasClave.Length; indice++)
        {
            string palabraNormalizada = NormalizarNombre(palabrasClave[indice]);
            if (!string.IsNullOrEmpty(palabraNormalizada) && textoNormalizado.Contains(palabraNormalizada))
            {
                return true;
            }
        }

        return false;
    }

    // Este metodo detecta colores amarillo intenso para forzar la correccion del esqueleto.
    private static bool EsColorAmarillo(Material material)
    {
        if (material == null)
        {
            return false;
        }

        Color color = material.HasProperty("_Color") ? material.color : Color.white;
        return color.r > 0.7f && color.g > 0.5f && color.b < 0.3f;
    }

    // Este metodo reusa la deteccion de texturas pero aplicando una busqueda puntual sobre el proyecto.
    private static Texture2D BuscarTexturaPorNombre(Material material, Renderer renderer)
    {
        if (material == null || renderer == null)
        {
            return null;
        }

        Texture2D texturaEncontrada = BuscarTexturaCoincidenteEnProyecto(
            material.name,
            renderer.gameObject.name,
            ObtenerNombreRaiz(renderer.transform));

        if (texturaEncontrada != null)
        {
            return texturaEncontrada;
        }

        return BuscarTexturaCoincidente(CargarTodasLasTexturas(), material.name, renderer.gameObject.name, ObtenerNombreRaiz(renderer.transform));
    }

    // Este metodo obtiene el nombre de la raiz del renderer.
    private static string ObtenerNombreRaiz(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        Transform raiz = transform.root != null ? transform.root : transform;
        return raiz != null ? raiz.name : string.Empty;
    }

    // Este metodo obtiene todos los renderers de la escena abierta.
    private static Renderer[] ObtenerRenderersEscena()
    {
        return UnityEngine.Object.FindObjectsOfType<Renderer>(true);
    }

    // Este metodo obtiene todos los SkinnedMeshRenderer de la escena abierta.
    private static SkinnedMeshRenderer[] ObtenerSkinnedMeshRenderersEscena()
    {
        return UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>(true);
    }

    // Este metodo evita tocar objetos que no pertenecen a la escena activa.
    private static bool EsRendererValido(Renderer renderer)
    {
        if (renderer == null || renderer.gameObject == null)
        {
            return false;
        }

        Scene scene = renderer.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    // Este metodo guarda todos los cambios y actualiza la vista del proyecto.
    private static void GuardarYRefrescar()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // COPILOT-EXPAND:
    // Aqui se puede ampliar el detector para otros packs o nuevos tipos de materiales.
}
