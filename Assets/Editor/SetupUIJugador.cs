using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este setup crea la HUD principal canónica de la demo:
// - combate abajo al centro
// - tarjeta de partida arriba a la izquierda
// - minimapa táctico arriba a la derecha
// - mapa grande centrado que se abre con la tecla M

// Esta clase crea una HUD de mejor calidad visual usando TextMeshPro.
public static class SetupUIJugador
{
    private const string RutaCarpetaFuentes = "Assets/Fonts";
    private const string RutaCarpetaFuenteBase = "Assets/Fonts/Source";
    private const string RutaCarpetaFuenteTMP = "Assets/Fonts/TMP";
    private const string RutaFuenteBaseProyecto = "Assets/Fonts/Source/RealmBrawlHUD.ttf";
    private const string RutaFuenteTMP = "Assets/Fonts/TMP/RealmBrawlHUD SDF.asset";

    private static readonly Vector2 TamanoHudInferior = new Vector2(920f, 138f);
    private static readonly Vector2 PosicionHudInferior = new Vector2(0f, 18f);

    private static readonly Vector2 TamanoTarjetaPartida = new Vector2(344f, 178f);
    private static readonly Vector2 PosicionTarjetaPartida = new Vector2(18f, -18f);

    private static readonly Vector2 TamanoMiniMapa = new Vector2(228f, 228f);
    private static readonly Vector2 PosicionMiniMapa = new Vector2(-18f, -18f);

    private static readonly Vector2 TamanoMapaGrande = new Vector2(1240f, 760f);
    private const string RutaPaqueteTmpEssentials = "Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage";

    [MenuItem("Realm Brawl/Setup/UI Jugador")]
    [MenuItem("Realm Brawl/Setup/UI Jugador HUD")]
    public static void CrearHUD()
    {
        AsegurarTMPEssentialsImportados();

        Canvas canvas = ObtenerOCrearCanvasPrincipal();
        if (canvas == null)
        {
            Debug.LogError("[SetupUIJugador] No se pudo crear el Canvas principal.");
            return;
        }

        TMP_FontAsset fuentePrincipal = ObtenerOCrearFuenteTMP();
        if (fuentePrincipal == null)
        {
            Debug.LogError("[SetupUIJugador] No se pudo crear una fuente TMP para la HUD.");
            return;
        }

        DestruirSiExiste(canvas.transform, "HUDJugador");
        DestruirSiExiste(canvas.transform, "TarjetaPartidaHUD");
        DestruirSiExiste(canvas.transform, "MiniMapaHUD");
        DestruirSiExiste(canvas.transform, "MapaGrandeHUD");

        GameObject camaraMiniMapaVieja = GameObject.Find("CamaraMiniMapaHUD");
        if (camaraMiniMapaVieja != null)
        {
            Object.DestroyImmediate(camaraMiniMapaVieja);
        }

        GameObject hudInferior = CrearHudInferior(canvas.transform, fuentePrincipal);
        GameObject tarjetaPartida = CrearTarjetaPartida(canvas.transform, fuentePrincipal);
        GameObject miniMapaHud = CrearMiniMapa(canvas.transform);
        GameObject mapaGrandeHud = CrearMapaGrande(canvas.transform);

        UIJugador uiJugador = hudInferior.GetComponent<UIJugador>();
        MiniMapaJugadorUI miniMapaJugador = miniMapaHud.GetComponent<MiniMapaJugadorUI>();

        if (uiJugador != null)
        {
            uiJugador.panelesSecundarios = new[]
            {
                tarjetaPartida.GetComponent<CanvasGroup>(),
                miniMapaHud.GetComponent<CanvasGroup>()
            };
        }

        if (miniMapaJugador != null)
        {
            Canvas.ForceUpdateCanvases();
            miniMapaJugador.fuenteTMP = fuentePrincipal;
            miniMapaJugador.panelMiniMapa = miniMapaHud.GetComponent<RectTransform>();
            miniMapaJugador.panelMapaGrande = mapaGrandeHud.GetComponent<RectTransform>();
            miniMapaJugador.panelGroup = miniMapaHud.GetComponent<CanvasGroup>();
            miniMapaJugador.panelMapaGrandeGroup = mapaGrandeHud.GetComponent<CanvasGroup>();
            miniMapaJugador.contenedorMiniMapa = miniMapaHud.transform.Find("MarcoMiniMapa/ContenidoMiniMapa") as RectTransform;
            miniMapaJugador.contenedorMapaGrande = mapaGrandeHud.transform.Find("MarcoMapaGrande/ContenidoMapaGrande") as RectTransform;
            miniMapaJugador.ReconstruirMapaTactico();
            Canvas.ForceUpdateCanvases();
        }

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(hudInferior);
        EditorUtility.SetDirty(tarjetaPartida);
        EditorUtility.SetDirty(miniMapaHud);
        EditorUtility.SetDirty(mapaGrandeHud);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupUIJugador] HUD TMP creada con panel inferior, tarjeta de partida y minimapa tactico.");
        EditorUtility.DisplayDialog("HUD recreada", "Se creó la HUD principal con TextMeshPro, tarjeta de partida y minimapa táctico.", "OK");
    }

    private static Canvas ObtenerOCrearCanvasPrincipal()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            ConfigurarCanvas(canvas);
            return canvas;
        }

        GameObject objetoCanvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = objetoCanvas.GetComponent<Canvas>();
        ConfigurarCanvas(canvas);
        return canvas;
    }

    private static void ConfigurarCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static TMP_FontAsset ObtenerOCrearFuenteTMP()
    {
        AsegurarCarpetasFuentes();

        // Ahora que el proyecto ya tiene TMP Essentials, preferimos la
        // fuente oficial de TMP para evitar atlas SDF raros o glifos faltantes.
        TMP_FontAsset fuenteTMPIntegrada = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fuenteTMPIntegrada != null)
        {
            return fuenteTMPIntegrada;
        }

        TMP_FontAsset fuenteExistente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuenteTMP);
        if (fuenteExistente != null)
        {
            return fuenteExistente;
        }

        Font fuenteBase = AssetDatabase.LoadAssetAtPath<Font>(RutaFuenteBaseProyecto);
        if (fuenteBase == null)
        {
            string[] rutasCandidatas =
            {
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\trebuc.ttf",
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\calibri.ttf"
            };

            for (int indiceRuta = 0; indiceRuta < rutasCandidatas.Length; indiceRuta++)
            {
                if (!File.Exists(rutasCandidatas[indiceRuta]))
                {
                    continue;
                }

                FileUtil.CopyFileOrDirectory(rutasCandidatas[indiceRuta], RutaFuenteBaseProyecto);
                AssetDatabase.ImportAsset(RutaFuenteBaseProyecto, ImportAssetOptions.ForceUpdate);
                fuenteBase = AssetDatabase.LoadAssetAtPath<Font>(RutaFuenteBaseProyecto);
                break;
            }
        }

        if (fuenteBase == null)
        {
            fuenteBase = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (fuenteBase == null)
        {
            TMP_FontAsset fuenteFallback = BuscarFuenteTMPExistente();
            if (fuenteFallback != null)
            {
                return fuenteFallback;
            }

            return null;
        }

        TMP_FontAsset nuevaFuente = TMP_FontAsset.CreateFontAsset(fuenteBase);
        nuevaFuente.name = "RealmBrawlHUD SDF";
        AssetDatabase.CreateAsset(nuevaFuente, RutaFuenteTMP);
        EditorUtility.SetDirty(nuevaFuente);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuenteTMP);
    }

    private static void AsegurarTMPEssentialsImportados()
    {
        if (BuscarFuenteTMPExistente() != null)
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SetupUIJugador] TMP Essentials aun no estan importados. Sali de Play y vuelve a correr el setup.");
            return;
        }

        if (!File.Exists(RutaPaqueteTmpEssentials))
        {
            Debug.LogWarning("[SetupUIJugador] No se encontro el paquete TMP Essential Resources en " + RutaPaqueteTmpEssentials);
            return;
        }

        AssetDatabase.ImportPackage(RutaPaqueteTmpEssentials, false);
        AssetDatabase.Refresh();
    }

    private static TMP_FontAsset BuscarFuenteTMPExistente()
    {
        TMP_FontAsset fuenteRecurso = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fuenteRecurso != null)
        {
            return fuenteRecurso;
        }

        string[] guidsFuentes = AssetDatabase.FindAssets("t:TMP_FontAsset");
        for (int indiceFuente = 0; indiceFuente < guidsFuentes.Length; indiceFuente++)
        {
            string rutaAsset = AssetDatabase.GUIDToAssetPath(guidsFuentes[indiceFuente]);
            TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaAsset);
            if (fuente != null)
            {
                return fuente;
            }
        }

        return null;
    }

    private static void AsegurarCarpetasFuentes()
    {
        if (!AssetDatabase.IsValidFolder(RutaCarpetaFuentes))
        {
            AssetDatabase.CreateFolder("Assets", "Fonts");
        }

        if (!AssetDatabase.IsValidFolder(RutaCarpetaFuenteBase))
        {
            AssetDatabase.CreateFolder(RutaCarpetaFuentes, "Source");
        }

        if (!AssetDatabase.IsValidFolder(RutaCarpetaFuenteTMP))
        {
            AssetDatabase.CreateFolder(RutaCarpetaFuentes, "TMP");
        }
    }

    private static GameObject CrearHudInferior(Transform padre, TMP_FontAsset fuente)
    {
        GameObject panel = CrearPanelBase(
            padre,
            "HUDJugador",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            PosicionHudInferior,
            TamanoHudInferior,
            new Color(0.02f, 0.04f, 0.06f, 0.70f));

        CrearMarcoDecorativo(panel.transform, "MarcoExterior", new Vector2(0f, 0f), TamanoHudInferior, new Color(0.62f, 0.78f, 0.85f, 0.10f), 2f);
        CrearLinea(panel.transform, "LineaSuperiorHUD", new Vector2(18f, -14f), new Vector2(884f, 1f), new Color(1f, 1f, 1f, 0.08f));

        TMP_Text iconoVida = CrearTextoTMP(panel.transform, "IconoVida", "HP", fuente, 18f, new Color(1f, 0.34f, 0.34f, 1f), new Vector2(14f, -18f), new Vector2(40f, 24f), TextAlignmentOptions.Center);
        Image barraVidaFondo = CrearBarra(panel.transform, "BarraVidaFondo", new Vector2(62f, -18f), new Vector2(520f, 22f), new Color(0.08f, 0.08f, 0.08f, 0.96f));
        Image barraVidaOscura = CrearFill(barraVidaFondo.transform, "FillVidaOscura", new Color(0.45f, 0.02f, 0.02f, 1f));
        Image barraVida = CrearFill(barraVidaFondo.transform, "FillVida", new Color(1f, 0.15f, 0.15f, 1f));
        Image brilloVida = CrearFill(barraVidaFondo.transform, "BrilloVida", new Color(1f, 1f, 1f, 0f));
        TMP_Text textoVida = CrearTextoTMP(panel.transform, "TextoVida", "100 / 100", fuente, 18f, Color.white, new Vector2(596f, -18f), new Vector2(144f, 24f), TextAlignmentOptions.Left);

        TMP_Text iconoEstamina = CrearTextoTMP(panel.transform, "IconoEstamina", "STA", fuente, 14f, new Color(1f, 0.85f, 0.04f, 1f), new Vector2(10f, -52f), new Vector2(46f, 24f), TextAlignmentOptions.Center);
        Image barraEstaminaFondo = CrearBarra(panel.transform, "BarraEstaminaFondo", new Vector2(62f, -52f), new Vector2(520f, 16f), new Color(0.08f, 0.08f, 0.08f, 0.96f));
        Image barraEstaminaOscura = CrearFill(barraEstaminaFondo.transform, "FillEstaminaOscura", new Color(0.70f, 0.42f, 0.03f, 1f));
        Image barraEstamina = CrearFill(barraEstaminaFondo.transform, "FillEstamina", new Color(1f, 0.84f, 0f, 1f));
        TMP_Text textoEstamina = CrearTextoTMP(panel.transform, "TextoEstamina", "100 / 100", fuente, 17f, Color.white, new Vector2(596f, -50f), new Vector2(144f, 20f), TextAlignmentOptions.Left);

        TMP_Text iconoExperiencia = CrearTextoTMP(panel.transform, "IconoExperiencia", "XP", fuente, 18f, new Color(0.83f, 0.56f, 1f, 1f), new Vector2(18f, -81f), new Vector2(34f, 20f), TextAlignmentOptions.Center);
        Image barraExperienciaFondo = CrearBarra(panel.transform, "BarraExperienciaFondo", new Vector2(62f, -81f), new Vector2(520f, 14f), new Color(0.08f, 0.08f, 0.08f, 0.96f));
        Image barraExperienciaOscura = CrearFill(barraExperienciaFondo.transform, "FillExperienciaOscura", new Color(0.23f, 0.10f, 0.42f, 1f));
        Image barraExperiencia = CrearFill(barraExperienciaFondo.transform, "FillExperiencia", new Color(0.68f, 0.36f, 1f, 1f));
        TMP_Text textoExperiencia = CrearTextoTMP(panel.transform, "TextoExperiencia", "0 / 100", fuente, 16f, Color.white, new Vector2(596f, -80f), new Vector2(144f, 18f), TextAlignmentOptions.Left);

        TMP_Text textoParry = CrearTextoTMP(panel.transform, "TextoParry", string.Empty, fuente, 16f, new Color(0.62f, 0.92f, 1f, 0f), new Vector2(760f, -18f), new Vector2(120f, 24f), TextAlignmentOptions.Center);

        TMP_Text tituloPociones = CrearTextoTMP(panel.transform, "TituloPociones", "POCIONES", fuente, 16f, new Color(0.94f, 0.97f, 1f, 0.92f), new Vector2(748f, -16f), new Vector2(140f, 20f), TextAlignmentOptions.Center);
        Image[] ranurasPociones = new Image[5];
        for (int indice = 0; indice < ranurasPociones.Length; indice++)
        {
            float posicionX = 742f + (indice * 28f);
            Image ranura = CrearBarra(panel.transform, "RanuraPocion_" + indice, new Vector2(posicionX, -48f), new Vector2(20f, 26f), new Color(0.10f, 0.16f, 0.14f, 0.96f));
            CrearMarcoDecorativo(ranura.transform, "Marco", Vector2.zero, new Vector2(20f, 26f), new Color(1f, 1f, 1f, 0.08f), 1f);
            ranurasPociones[indice] = ranura;
        }

        TMP_Text textoPocionesInferior = CrearTextoTMP(panel.transform, "TextoPocionesInferior", "Pociones 0 / 5", fuente, 16f, Color.white, new Vector2(740f, -82f), new Vector2(156f, 20f), TextAlignmentOptions.Center);

        UIJugador uiJugador = panel.AddComponent<UIJugador>();
        uiJugador.fillVida = barraVida;
        uiJugador.fillEstamina = barraEstamina;
        uiJugador.fillExperiencia = barraExperiencia;
        uiJugador.iconoCorazon = iconoVida;
        uiJugador.iconoEstamina = iconoEstamina;
        uiJugador.iconoExperiencia = iconoExperiencia;
        uiJugador.textoVida = textoVida;
        uiJugador.textoEstamina = textoEstamina;
        uiJugador.textoExperiencia = textoExperiencia;
        uiJugador.textoParryInferior = textoParry;
        uiJugador.textoPocionesInferior = textoPocionesInferior;
        uiJugador.ranurasPociones = ranurasPociones;
        uiJugador.panelGroup = AsegurarCanvasGroup(panel);
        uiJugador.brilloVida = brilloVida;

        return panel;
    }

    private static GameObject CrearTarjetaPartida(Transform padre, TMP_FontAsset fuente)
    {
        GameObject panel = CrearPanelBase(
            padre,
            "TarjetaPartidaHUD",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            PosicionTarjetaPartida,
            TamanoTarjetaPartida,
            new Color(0.03f, 0.05f, 0.07f, 0.74f));

        CrearMarcoDecorativo(panel.transform, "MarcoTarjeta", Vector2.zero, TamanoTarjetaPartida, new Color(0.78f, 0.86f, 0.92f, 0.12f), 2f);
        TMP_Text titulo = CrearTextoTMP(panel.transform, "TituloTarjeta", "REALM BRAWL", fuente, 28f, new Color(1f, 0.92f, 0.56f, 1f), new Vector2(18f, -16f), new Vector2(308f, 28f), TextAlignmentOptions.Left);
        CrearLinea(panel.transform, "LineaTarjeta", new Vector2(18f, -46f), new Vector2(308f, 1f), new Color(1f, 1f, 1f, 0.10f));

        TMP_Text textoOleada = CrearTextoTMP(panel.transform, "TextoOleadaSuperior", "Oleada: 1", fuente, 18f, Color.white, new Vector2(18f, -56f), new Vector2(308f, 22f), TextAlignmentOptions.Left);
        TMP_Text textoProxima = CrearTextoTMP(panel.transform, "TextoProximaRondaSuperior", "Próx. ronda: en combate", fuente, 17f, new Color(1f, 0.94f, 0.78f, 1f), new Vector2(18f, -82f), new Vector2(308f, 22f), TextAlignmentOptions.Left);
        TMP_Text textoNivel = CrearTextoTMP(panel.transform, "TextoNivelSuperior", "Nivel: 1", fuente, 17f, Color.white, new Vector2(18f, -108f), new Vector2(150f, 22f), TextAlignmentOptions.Left);
        TMP_Text textoXp = CrearTextoTMP(panel.transform, "TextoXpSuperior", "XP: 0 / 100", fuente, 17f, new Color(0.88f, 0.78f, 1f, 1f), new Vector2(18f, -132f), new Vector2(150f, 22f), TextAlignmentOptions.Left);
        TMP_Text textoPociones = CrearTextoTMP(panel.transform, "TextoPocionesSuperior", "Pociones: 0 / 5", fuente, 17f, new Color(0.76f, 1f, 0.88f, 1f), new Vector2(18f, -156f), new Vector2(150f, 22f), TextAlignmentOptions.Left);
        TMP_Text textoRacha = CrearTextoTMP(panel.transform, "TextoRachaSuperior", "Racha: x0", fuente, 17f, new Color(1f, 0.84f, 0.58f, 1f), new Vector2(180f, -156f), new Vector2(146f, 22f), TextAlignmentOptions.Left);

        UIJugador hudInferior = Object.FindObjectOfType<UIJugador>(true);
        if (hudInferior != null)
        {
            hudInferior.textoOleadaSuperior = textoOleada;
            hudInferior.textoProximaRondaSuperior = textoProxima;
            hudInferior.textoNivelSuperior = textoNivel;
            hudInferior.textoExperienciaSuperior = textoXp;
            hudInferior.textoPocionesSuperior = textoPociones;
            hudInferior.textoRachaSuperior = textoRacha;
        }

        AsegurarCanvasGroup(panel);
        return panel;
    }

    private static GameObject CrearMiniMapa(Transform padre)
    {
        GameObject panel = CrearPanelBase(
            padre,
            "MiniMapaHUD",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            PosicionMiniMapa,
            TamanoMiniMapa,
            new Color(0.02f, 0.04f, 0.06f, 0.78f));

        CrearMarcoDecorativo(panel.transform, "MarcoMiniMapa", Vector2.zero, TamanoMiniMapa, new Color(0.82f, 0.88f, 0.94f, 0.10f), 2f);
        GameObject contenido = new GameObject("ContenidoMiniMapa", typeof(RectTransform));
        contenido.transform.SetParent(panel.transform.Find("MarcoMiniMapa"), false);
        RectTransform rectContenido = contenido.GetComponent<RectTransform>();
        rectContenido.anchorMin = new Vector2(0f, 0f);
        rectContenido.anchorMax = new Vector2(1f, 1f);
        rectContenido.offsetMin = new Vector2(8f, 8f);
        rectContenido.offsetMax = new Vector2(-8f, -8f);

        panel.AddComponent<MiniMapaJugadorUI>();
        AsegurarCanvasGroup(panel);
        return panel;
    }

    private static GameObject CrearMapaGrande(Transform padre)
    {
        GameObject panel = CrearPanelBase(
            padre,
            "MapaGrandeHUD",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            TamanoMapaGrande,
            new Color(0.02f, 0.04f, 0.06f, 0.62f));

        CrearMarcoDecorativo(panel.transform, "MarcoMapaGrande", Vector2.zero, TamanoMapaGrande, new Color(0.84f, 0.90f, 0.96f, 0.10f), 2f);

        GameObject contenido = new GameObject("ContenidoMapaGrande", typeof(RectTransform));
        contenido.transform.SetParent(panel.transform.Find("MarcoMapaGrande"), false);
        RectTransform rectContenido = contenido.GetComponent<RectTransform>();
        rectContenido.anchorMin = new Vector2(0f, 0f);
        rectContenido.anchorMax = new Vector2(1f, 1f);
        rectContenido.offsetMin = new Vector2(18f, 18f);
        rectContenido.offsetMax = new Vector2(-18f, -18f);

        CanvasGroup group = AsegurarCanvasGroup(panel);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        panel.SetActive(true);
        return panel;
    }

    private static GameObject CrearPanelBase(Transform padre, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject panel = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(padre, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;

        Image imagen = panel.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return panel;
    }

    private static void CrearMarcoDecorativo(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color, float grosor)
    {
        GameObject marco = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        marco.transform.SetParent(padre, false);

        RectTransform rect = marco.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(grosor, grosor);
        rect.offsetMax = new Vector2(-grosor, -grosor);
        rect.localScale = Vector3.one;

        Image imagen = marco.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
    }

    private static Image CrearBarra(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject barra = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        barra.transform.SetParent(padre, false);

        RectTransform rect = barra.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;

        Image imagen = barra.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static Image CrearFill(Transform padre, string nombre, Color color)
    {
        GameObject fill = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(padre, false);

        RectTransform rect = fill.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image imagen = fill.GetComponent<Image>();
        imagen.color = color;
        imagen.type = Image.Type.Filled;
        imagen.fillMethod = Image.FillMethod.Horizontal;
        imagen.fillAmount = 1f;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static TMP_Text CrearTextoTMP(Transform padre, string nombre, string contenido, TMP_FontAsset fuente, float tamanoFuente, Color color, Vector2 posicion, Vector2 tamano, TextAlignmentOptions alineacion)
    {
        GameObject texto = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Shadow), typeof(Outline));
        texto.transform.SetParent(padre, false);

        RectTransform rect = texto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;

        TextMeshProUGUI tmp = texto.GetComponent<TextMeshProUGUI>();
        tmp.font = fuente;
        tmp.text = contenido;
        tmp.fontSize = tamanoFuente;
        tmp.color = color;
        tmp.alignment = alineacion;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.extraPadding = true;

        Shadow sombra = texto.GetComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.75f);
        sombra.effectDistance = new Vector2(2f, -2f);

        Outline contorno = texto.GetComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.55f);
        contorno.effectDistance = new Vector2(1f, -1f);

        return tmp;
    }

    private static void CrearLinea(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject linea = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        linea.transform.SetParent(padre, false);

        RectTransform rect = linea.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;

        Image imagen = linea.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
    }

    private static CanvasGroup AsegurarCanvasGroup(GameObject objeto)
    {
        CanvasGroup group = objeto.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = objeto.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private static void DestruirSiExiste(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            Object.DestroyImmediate(existente.gameObject);
        }
    }
}
