using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este minimapa deja de ser una camara cenital literal y pasa a ser un mapa tactico abstracto.
// Usa ArenaDemoMapa + MiniMapaRegionTactica como fuente visual del layout y mantiene memoria
// de enemigos, experiencia y aliados vistos.

// Esta clase dibuja y actualiza el minimapa táctico pequeño y su versión ampliada.
public class MiniMapaJugadorUI : MonoBehaviour
{
    private const string NombreObjetoMiniMapa = "MiniMapaHUD";
    private const string NombreObjetoMapaGrande = "MapaGrandeHUD";

    private enum TipoMarcador
    {
        Jugador,
        Enemigo,
        Experiencia
    }

    private class EntradaMarcador
    {
        public int idUnico;
        public TipoMarcador tipo;
        public Transform objetivo;
        public bool esJugadorLocal;
        public bool fueVisto;
        public float tiempoUltimaVision;
        public Vector3 ultimaPosicionConocida;
        public Image iconoMini;
        public Image iconoGrande;
        public TMP_Text textoMemoriaMini;
        public TMP_Text textoMemoriaGrande;
        public RectTransform rectMini;
        public RectTransform rectGrande;
    }

    [Header("Referencias UI")]
    public TMP_FontAsset fuenteTMP;
    public RectTransform panelMiniMapa;
    public RectTransform panelMapaGrande;
    public RectTransform contenedorMiniMapa;
    public RectTransform contenedorMapaGrande;
    public CanvasGroup panelGroup;
    public CanvasGroup panelMapaGrandeGroup;

    [Header("Ajustes")]
    [SerializeField] private KeyCode teclaMapaGrande = KeyCode.M;
    [SerializeField] private float intervaloActualizacionObjetivos = 0.25f;
    [SerializeField] private float duracionMemoriaObjetivos = 5.5f;
    [SerializeField] private float velocidadFadeMapaGrande = 10f;
    [SerializeField] private Color colorJugadorLocal = new Color(0.95f, 0.95f, 1f, 1f);
    [SerializeField] private Color colorJugadorAliado = new Color(0.35f, 0.95f, 1f, 1f);
    [SerializeField] private Color colorEnemigo = new Color(1f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color colorExperiencia = new Color(0.82f, 0.45f, 1f, 1f);
    [SerializeField] private Color colorRecuerdo = new Color(1f, 1f, 1f, 0.78f);

    private ArenaDemoMapa arenaDemoMapa;
    private Camera camaraPrincipal;
    private VidaJugador jugadorLocal;
    private float tiempoProximaActualizacionObjetivos;
    private bool mapaGrandeActivo;
    private RectTransform regionesMiniRaiz;
    private RectTransform regionesGrandeRaiz;
    private RectTransform marcadoresMiniRaiz;
    private RectTransform marcadoresGrandeRaiz;
    private readonly Dictionary<int, EntradaMarcador> entradasMarcadores = new Dictionary<int, EntradaMarcador>();
    private readonly List<Graphic> regionesMini = new List<Graphic>();
    private readonly List<Graphic> regionesGrandes = new List<Graphic>();

    public bool MapaGrandeActivo => mapaGrandeActivo;

    private void Awake()
    {
        mapaGrandeActivo = false;
        BuscarReferenciasBasicas();
        AsegurarJerarquiaMapa();
        ForzarEstadoInicialMapaGrande();
        ReconstruirMapaTactico();
    }

    private void OnEnable()
    {
        mapaGrandeActivo = false;
        BuscarReferenciasBasicas();
        AsegurarJerarquiaMapa();
        ForzarEstadoInicialMapaGrande();
        ReconstruirMapaTactico();
    }

    private void Update()
    {
        BuscarReferenciasBasicas();
        AsegurarJerarquiaMapa();

        if ((regionesMini.Count == 0 && regionesGrandes.Count == 0) &&
            arenaDemoMapa != null &&
            (contenedorMiniMapa != null || contenedorMapaGrande != null))
        {
            ReconstruirMapaTactico();
        }

        if (Input.GetKeyDown(teclaMapaGrande))
        {
            ToggleMapaGrande();
        }

        ActualizarFadeMapaGrande();

        if (Time.unscaledTime >= tiempoProximaActualizacionObjetivos)
        {
            tiempoProximaActualizacionObjetivos = Time.unscaledTime + Mathf.Max(0.1f, intervaloActualizacionObjetivos);
            RefrescarObjetivos();
        }

        ActualizarMarcadores();
    }

    public void ToggleMapaGrande()
    {
        mapaGrandeActivo = !mapaGrandeActivo;
    }

    private void ForzarEstadoInicialMapaGrande()
    {
        if (panelMapaGrandeGroup == null)
        {
            return;
        }

        // El mapa grande siempre arranca oculto para evitar que Unity
        // preserve su estado entre sesiones de Play o tras recompilar.
        mapaGrandeActivo = false;
        panelMapaGrandeGroup.alpha = 0f;
        panelMapaGrandeGroup.interactable = false;
        panelMapaGrandeGroup.blocksRaycasts = false;
    }

    public void ReconstruirMapaTactico()
    {
        BuscarReferenciasBasicas();
        AsegurarJerarquiaMapa();
        LimpiarRegiones();
        ConstruirRegiones();
        RefrescarObjetivos();
    }

    private void BuscarReferenciasBasicas()
    {
        if (panelMiniMapa == null)
        {
            GameObject miniMapa = GameObject.Find(NombreObjetoMiniMapa);
            if (miniMapa != null)
            {
                panelMiniMapa = miniMapa.GetComponent<RectTransform>();
            }
        }

        if (panelMapaGrande == null)
        {
            GameObject mapaGrande = GameObject.Find(NombreObjetoMapaGrande);
            if (mapaGrande != null)
            {
                panelMapaGrande = mapaGrande.GetComponent<RectTransform>();
            }
        }

        if (panelGroup == null && panelMiniMapa != null)
        {
            panelGroup = panelMiniMapa.GetComponent<CanvasGroup>();
        }

        if (panelMapaGrandeGroup == null && panelMapaGrande != null)
        {
            panelMapaGrandeGroup = panelMapaGrande.GetComponent<CanvasGroup>();
        }

        if (contenedorMiniMapa == null && panelMiniMapa != null)
        {
            Transform contenidoMini = panelMiniMapa.Find("MarcoMiniMapa/ContenidoMiniMapa");
            if (contenidoMini != null)
            {
                contenedorMiniMapa = contenidoMini as RectTransform;
            }
        }

        if (contenedorMapaGrande == null && panelMapaGrande != null)
        {
            Transform contenidoGrande = panelMapaGrande.Find("MarcoMapaGrande/ContenidoMapaGrande");
            if (contenidoGrande != null)
            {
                contenedorMapaGrande = contenidoGrande as RectTransform;
            }
        }

        if (fuenteTMP == null)
        {
            fuenteTMP = BuscarFuenteTMPFallback();
        }

        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (arenaDemoMapa == null)
        {
            arenaDemoMapa = FindObjectOfType<ArenaDemoMapa>(true);
        }

        if (jugadorLocal == null)
        {
            GameManager gameManager = GameManager.Instancia != null ? GameManager.Instancia : FindObjectOfType<GameManager>(true);
            if (gameManager != null && gameManager.JugadorPrincipal != null)
            {
                jugadorLocal = gameManager.JugadorPrincipal.GetComponent<VidaJugador>();
            }

            if (jugadorLocal == null)
            {
                jugadorLocal = FindObjectOfType<VidaJugador>(true);
            }
        }
    }

    private TMP_FontAsset BuscarFuenteTMPFallback()
    {
        TMP_FontAsset fuenteRecursos = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fuenteRecursos != null)
        {
            return fuenteRecursos;
        }

        TMP_Text[] textosActivos = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int indiceTexto = 0; indiceTexto < textosActivos.Length; indiceTexto++)
        {
            if (textosActivos[indiceTexto] != null && textosActivos[indiceTexto].font != null)
            {
                return textosActivos[indiceTexto].font;
            }
        }

        return null;
    }

    private void AsegurarJerarquiaMapa()
    {
        if (contenedorMiniMapa != null)
        {
            regionesMiniRaiz = ObtenerONCrearContenedor(contenedorMiniMapa, "Regiones");
            marcadoresMiniRaiz = ObtenerONCrearContenedor(contenedorMiniMapa, "Marcadores");
        }

        if (contenedorMapaGrande != null)
        {
            regionesGrandeRaiz = ObtenerONCrearContenedor(contenedorMapaGrande, "Regiones");
            marcadoresGrandeRaiz = ObtenerONCrearContenedor(contenedorMapaGrande, "Marcadores");
        }

    }

    private RectTransform ObtenerONCrearContenedor(RectTransform padre, string nombre)
    {
        if (padre == null)
        {
            return null;
        }

        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente as RectTransform;
        }

        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private void LimpiarRegiones()
    {
        for (int indice = regionesMini.Count - 1; indice >= 0; indice--)
        {
            if (regionesMini[indice] != null)
            {
                Destroy(regionesMini[indice].gameObject);
            }
        }

        regionesMini.Clear();

        for (int indice = regionesGrandes.Count - 1; indice >= 0; indice--)
        {
            if (regionesGrandes[indice] != null)
            {
                Destroy(regionesGrandes[indice].gameObject);
            }
        }

        regionesGrandes.Clear();
    }

    private void ConstruirRegiones()
    {
        if (arenaDemoMapa == null)
        {
            Debug.LogWarning("[MiniMapaJugadorUI] No se encontro ArenaDemoMapa para dibujar el mapa tactico.");
            return;
        }

        arenaDemoMapa.RecolectarReferenciasDesdeHijos();
        MiniMapaRegionTactica[] regiones = arenaDemoMapa.RegionesMiniMapa;
        if (regiones == null)
        {
            return;
        }

        for (int indice = 0; indice < regiones.Length; indice++)
        {
            MiniMapaRegionTactica region = regiones[indice];
            if (region == null)
            {
                continue;
            }

            if (regionesMiniRaiz != null)
            {
                regionesMini.Add(CrearRegionVisual(regionesMiniRaiz, region, "MiniRegion_" + indice));
            }

            if (regionesGrandeRaiz != null)
            {
                regionesGrandes.Add(CrearRegionVisual(regionesGrandeRaiz, region, "GrandeRegion_" + indice));
            }
        }
    }

    private Graphic CrearRegionVisual(RectTransform padre, MiniMapaRegionTactica region, string nombre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        Vector2 centroNormalizado = arenaDemoMapa.MundoANormalizado(region.transform.position);
        Vector2 tamanoNormalizado = new Vector2(
            region.TamanoMundo.x / Mathf.Max(1f, arenaDemoMapa.TamanoMundo.x),
            region.TamanoMundo.y / Mathf.Max(1f, arenaDemoMapa.TamanoMundo.y));

        rect.anchoredPosition = new Vector2(
            (centroNormalizado.x - 0.5f) * padre.rect.width,
            (centroNormalizado.y - 0.5f) * padre.rect.height);

        rect.sizeDelta = new Vector2(
            padre.rect.width * tamanoNormalizado.x,
            padre.rect.height * tamanoNormalizado.y);

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = region.ColorBase;
        imagen.raycastTarget = false;

        switch (region.Forma)
        {
            case MiniMapaRegionTactica.FormaRegionTactica.Diamante:
                rect.localRotation = Quaternion.Euler(0f, 0f, 45f + region.RotacionPlano);
                break;

            case MiniMapaRegionTactica.FormaRegionTactica.Capsula:
                rect.localRotation = Quaternion.Euler(0f, 0f, region.RotacionPlano);
                break;

            default:
                rect.localRotation = Quaternion.Euler(0f, 0f, region.RotacionPlano);
                break;
        }

        return imagen;
    }

    private void RefrescarObjetivos()
    {
        HashSet<int> idsPresentes = new HashSet<int>();

        VidaJugador[] jugadores = FindObjectsOfType<VidaJugador>(true);
        for (int indiceJugador = 0; indiceJugador < jugadores.Length; indiceJugador++)
        {
            VidaJugador jugadorActual = jugadores[indiceJugador];
            if (jugadorActual == null || !jugadorActual.EstaVivo)
            {
                continue;
            }

            RegistrarOActualizarEntrada(jugadorActual.transform, TipoMarcador.Jugador, jugadorActual == jugadorLocal);
            idsPresentes.Add(jugadorActual.transform.GetInstanceID());
        }

        EnemigoDummy[] enemigos = FindObjectsOfType<EnemigoDummy>(true);
        for (int indiceEnemigo = 0; indiceEnemigo < enemigos.Length; indiceEnemigo++)
        {
            EnemigoDummy enemigoActual = enemigos[indiceEnemigo];
            if (enemigoActual == null)
            {
                continue;
            }

            VidaEnemigo vidaEnemigo = enemigoActual.GetComponent<VidaEnemigo>();
            if (vidaEnemigo == null || !vidaEnemigo.EstaVivo || !enemigoActual.gameObject.activeInHierarchy)
            {
                continue;
            }

            RegistrarOActualizarEntrada(enemigoActual.transform, TipoMarcador.Enemigo, false);
            idsPresentes.Add(enemigoActual.transform.GetInstanceID());
        }

        OrbeExperiencia[] orbes = FindObjectsOfType<OrbeExperiencia>(true);
        for (int indiceOrbe = 0; indiceOrbe < orbes.Length; indiceOrbe++)
        {
            OrbeExperiencia orbeActual = orbes[indiceOrbe];
            if (orbeActual == null || !orbeActual.gameObject.activeInHierarchy)
            {
                continue;
            }

            RegistrarOActualizarEntrada(orbeActual.transform, TipoMarcador.Experiencia, false);
            idsPresentes.Add(orbeActual.transform.GetInstanceID());
        }

        List<int> idsAEliminar = new List<int>();
        foreach (KeyValuePair<int, EntradaMarcador> par in entradasMarcadores)
        {
            EntradaMarcador entrada = par.Value;
            if (entrada == null)
            {
                idsAEliminar.Add(par.Key);
                continue;
            }

            bool sigueValido = entrada.objetivo != null && idsPresentes.Contains(par.Key);
            if (!sigueValido)
            {
                DestruirVisualEntrada(entrada);
                idsAEliminar.Add(par.Key);
            }
        }

        for (int indiceEliminar = 0; indiceEliminar < idsAEliminar.Count; indiceEliminar++)
        {
            entradasMarcadores.Remove(idsAEliminar[indiceEliminar]);
        }
    }

    private void RegistrarOActualizarEntrada(Transform objetivo, TipoMarcador tipo, bool esJugadorLocal)
    {
        if (objetivo == null)
        {
            return;
        }

        int id = objetivo.GetInstanceID();
        if (!entradasMarcadores.TryGetValue(id, out EntradaMarcador entrada) || entrada == null)
        {
            entrada = new EntradaMarcador
            {
                idUnico = id,
                tipo = tipo,
                objetivo = objetivo,
                esJugadorLocal = esJugadorLocal,
                fueVisto = false,
                ultimaPosicionConocida = objetivo.position,
                tiempoUltimaVision = -999f
            };

            entradasMarcadores[id] = entrada;
        }
        else
        {
            entrada.objetivo = objetivo;
            entrada.tipo = tipo;
            entrada.esJugadorLocal = esJugadorLocal;
        }
    }

    private void ActualizarMarcadores()
    {
        if (arenaDemoMapa == null)
        {
            return;
        }

        List<int> idsAEliminar = new List<int>();
        foreach (KeyValuePair<int, EntradaMarcador> par in entradasMarcadores)
        {
            EntradaMarcador entrada = par.Value;
            if (entrada == null || entrada.objetivo == null)
            {
                idsAEliminar.Add(par.Key);
                continue;
            }

            AsegurarVisualEntrada(entrada);

            bool visible = entrada.esJugadorLocal || EstaVisibleParaJugador(entrada.objetivo);
            if (visible)
            {
                entrada.fueVisto = true;
                entrada.tiempoUltimaVision = Time.unscaledTime;
                entrada.ultimaPosicionConocida = entrada.objetivo.position;
            }

            float tiempoSinVision = Time.unscaledTime - entrada.tiempoUltimaVision;
            bool mostrarRecuerdo = !visible && entrada.fueVisto && tiempoSinVision <= duracionMemoriaObjetivos;
            if (!visible && !mostrarRecuerdo)
            {
                EstablecerActivoEntrada(entrada, false);
                continue;
            }

            Vector3 posicionMundo = visible ? entrada.objetivo.position : entrada.ultimaPosicionConocida;
            Vector2 normalizado = arenaDemoMapa.MundoANormalizado(posicionMundo);
            PosicionarVisualEntrada(entrada.rectMini, marcadoresMiniRaiz, normalizado);
            PosicionarVisualEntrada(entrada.rectGrande, marcadoresGrandeRaiz, normalizado);

            if (visible)
            {
                ConfigurarMarcadorVisible(entrada);
            }
            else
            {
                ConfigurarMarcadorRecordado(entrada, tiempoSinVision);
            }
        }

        for (int indice = 0; indice < idsAEliminar.Count; indice++)
        {
            entradasMarcadores.Remove(idsAEliminar[indice]);
        }
    }

    private bool EstaVisibleParaJugador(Transform objetivo)
    {
        if (objetivo == null)
        {
            return false;
        }

        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (camaraPrincipal == null)
        {
            return false;
        }

        Vector3 punto = ObtenerPuntoObjetivo(objetivo);
        Vector3 viewport = camaraPrincipal.WorldToViewportPoint(punto);
        if (viewport.z <= 0f)
        {
            return false;
        }

        bool dentroPantalla = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (!dentroPantalla)
        {
            return false;
        }

        Vector3 origen = camaraPrincipal.transform.position;
        Vector3 direccion = punto - origen;
        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, direccion.magnitude + 0.25f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != null && hit.transform.root != objetivo.root)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 ObtenerPuntoObjetivo(Transform objetivo)
    {
        if (objetivo == null)
        {
            return Vector3.zero;
        }

        Renderer rendererObjetivo = objetivo.GetComponentInChildren<Renderer>();
        if (rendererObjetivo != null)
        {
            return rendererObjetivo.bounds.center;
        }

        return objetivo.position + Vector3.up * 0.8f;
    }

    private void AsegurarVisualEntrada(EntradaMarcador entrada)
    {
        if (marcadoresMiniRaiz != null && (entrada.rectMini == null || entrada.iconoMini == null || entrada.textoMemoriaMini == null))
        {
            CrearVisualMarcador(marcadoresMiniRaiz, "MarcadorMini_" + entrada.idUnico, 14f, out entrada.rectMini, out entrada.iconoMini, out entrada.textoMemoriaMini);
        }

        if (marcadoresGrandeRaiz != null && (entrada.rectGrande == null || entrada.iconoGrande == null || entrada.textoMemoriaGrande == null))
        {
            CrearVisualMarcador(marcadoresGrandeRaiz, "MarcadorGrande_" + entrada.idUnico, 22f, out entrada.rectGrande, out entrada.iconoGrande, out entrada.textoMemoriaGrande);
        }
    }

    private void CrearVisualMarcador(RectTransform padre, string nombre, float tamano, out RectTransform rect, out Image icono, out TMP_Text textoMemoria)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Outline));
        objeto.transform.SetParent(padre, false);

        rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(tamano, tamano);
        rect.localScale = Vector3.one;

        icono = objeto.GetComponent<Image>();
        icono.raycastTarget = false;

        Outline outline = objeto.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject objetoTexto = new GameObject("Memoria", typeof(RectTransform), typeof(TextMeshProUGUI));
        objetoTexto.transform.SetParent(objeto.transform, false);
        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;
        rectTexto.localScale = Vector3.one;

        textoMemoria = objetoTexto.GetComponent<TextMeshProUGUI>();
        if (fuenteTMP != null)
        {
            textoMemoria.font = fuenteTMP;
        }
        textoMemoria.fontSize = tamano * 0.92f;
        textoMemoria.alignment = TextAlignmentOptions.Center;
        textoMemoria.enableWordWrapping = false;
        textoMemoria.text = string.Empty;
        textoMemoria.raycastTarget = false;
    }

    private void PosicionarVisualEntrada(RectTransform rect, RectTransform padre, Vector2 normalizado)
    {
        if (rect == null || padre == null)
        {
            return;
        }

        float ancho = padre.rect.width;
        float alto = padre.rect.height;
        rect.anchoredPosition = new Vector2((normalizado.x - 0.5f) * ancho, (normalizado.y - 0.5f) * alto);
    }

    private void ConfigurarMarcadorVisible(EntradaMarcador entrada)
    {
        EstablecerActivoEntrada(entrada, true);

        Color color = ObtenerColorVisible(entrada);
        float rotacion = entrada.esJugadorLocal ? 45f : 0f;
        Vector2 tamanoMini = entrada.tipo == TipoMarcador.Enemigo ? new Vector2(11f, 11f) : new Vector2(10f, 10f);
        Vector2 tamanoGrande = entrada.tipo == TipoMarcador.Enemigo ? new Vector2(18f, 18f) : new Vector2(16f, 16f);

        ConfigurarIconoVisible(entrada.iconoMini, entrada.textoMemoriaMini, color, tamanoMini, rotacion);
        ConfigurarIconoVisible(entrada.iconoGrande, entrada.textoMemoriaGrande, color, tamanoGrande, rotacion);
    }

    private void ConfigurarIconoVisible(Image imagen, TMP_Text textoMemoria, Color color, Vector2 tamano, float rotacion)
    {
        if (imagen != null)
        {
            imagen.enabled = true;
            imagen.color = color;
            imagen.rectTransform.sizeDelta = tamano;
            imagen.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotacion);
        }

        if (textoMemoria != null)
        {
            textoMemoria.text = string.Empty;
        }
    }

    private void ConfigurarMarcadorRecordado(EntradaMarcador entrada, float tiempoSinVision)
    {
        EstablecerActivoEntrada(entrada, true);

        float alpha = 1f - Mathf.Clamp01(tiempoSinVision / duracionMemoriaObjetivos);
        ConfigurarIconoRecordado(entrada.iconoMini, entrada.textoMemoriaMini, alpha);
        ConfigurarIconoRecordado(entrada.iconoGrande, entrada.textoMemoriaGrande, alpha);
    }

    private void ConfigurarIconoRecordado(Image imagen, TMP_Text textoMemoria, float alpha)
    {
        if (imagen != null)
        {
            imagen.enabled = false;
        }

        if (textoMemoria != null)
        {
            textoMemoria.text = "?";
            textoMemoria.color = new Color(colorRecuerdo.r, colorRecuerdo.g, colorRecuerdo.b, alpha);
        }
    }

    private Color ObtenerColorVisible(EntradaMarcador entrada)
    {
        switch (entrada.tipo)
        {
            case TipoMarcador.Enemigo:
                return colorEnemigo;
            case TipoMarcador.Experiencia:
                return colorExperiencia;
            default:
                return entrada.esJugadorLocal ? colorJugadorLocal : colorJugadorAliado;
        }
    }

    private void EstablecerActivoEntrada(EntradaMarcador entrada, bool activo)
    {
        if (entrada.rectMini != null)
        {
            entrada.rectMini.gameObject.SetActive(activo);
        }

        if (entrada.rectGrande != null)
        {
            entrada.rectGrande.gameObject.SetActive(activo);
        }
    }

    private void DestruirVisualEntrada(EntradaMarcador entrada)
    {
        if (entrada == null)
        {
            return;
        }

        if (entrada.rectMini != null)
        {
            Destroy(entrada.rectMini.gameObject);
        }

        if (entrada.rectGrande != null)
        {
            Destroy(entrada.rectGrande.gameObject);
        }
    }

    private void ActualizarFadeMapaGrande()
    {
        if (panelMapaGrandeGroup == null)
        {
            return;
        }

        float alphaObjetivo = mapaGrandeActivo ? 1f : 0f;
        panelMapaGrandeGroup.alpha = Mathf.Lerp(panelMapaGrandeGroup.alpha, alphaObjetivo, Time.unscaledDeltaTime * velocidadFadeMapaGrande);
        panelMapaGrandeGroup.interactable = panelMapaGrandeGroup.alpha > 0.9f;
        panelMapaGrandeGroup.blocksRaycasts = panelMapaGrandeGroup.alpha > 0.9f;
    }
}
