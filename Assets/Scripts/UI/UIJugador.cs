using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Esta HUD es la interfaz principal canónica de la demo:
// - vida / estamina / XP / pociones abajo al centro
// - tarjeta de oleada arriba a la izquierda
// - soporte para feedback visual de parry y fade en game over

// Esta clase actualiza y anima la interfaz principal del jugador.
public class UIJugador : MonoBehaviour
{
    public static UIJugador Instancia { get; private set; }
    public static bool HUDPrincipalActivo => Instancia != null && Instancia.isActiveAndEnabled;

    [Header("Referencias Gameplay")]
    [SerializeField] private VidaJugador vidaJugador;
    [SerializeField] private Estamina estaminaJugador;
    [SerializeField] private SistemaXP sistemaXP;
    [SerializeField] private SistemaOleadas sistemaOleadas;
    [SerializeField] private SistemaPociones sistemaPociones;
    [SerializeField] private SistemaRacha sistemaRacha;
    [SerializeField] private SistemaDefensaEspada sistemaDefensaEspada;
    [SerializeField] private GameManager gameManager;

    [Header("Referencias UI")]
    public Image fillVida;
    public Image fillEstamina;
    public Image fillExperiencia;
    public Graphic iconoCorazon;
    public Graphic iconoEstamina;
    public Graphic iconoExperiencia;
    public TMP_Text textoVida;
    public TMP_Text textoEstamina;
    public TMP_Text textoExperiencia;
    public TMP_Text textoParryInferior;
    public TMP_Text textoOleadaSuperior;
    public TMP_Text textoProximaRondaSuperior;
    public TMP_Text textoNivelSuperior;
    public TMP_Text textoExperienciaSuperior;
    public TMP_Text textoPocionesSuperior;
    public TMP_Text textoRachaSuperior;
    public TMP_Text textoPocionesInferior;
    public TMP_Text textoStats;
    public CanvasGroup panelGroup;
    public CanvasGroup[] panelesSecundarios;
    public Image brilloVida;
    public Image[] ranurasPociones;

    [Header("Ajustes Visuales")]
    [SerializeField] private float velocidadSuavizadoBarras = 8f;
    [SerializeField] private float velocidadFadePanel = 6f;
    [SerializeField] private float duracionFlashVida = 0.12f;
    [SerializeField] private float duracionPulsoCorazon = 0.18f;
    [SerializeField] private float frecuenciaPulsoAgotado = 1f;
    [SerializeField] private Color colorVidaNormal = new Color(1f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color colorEstaminaNormal = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private Color colorEstaminaAgotada = new Color(1f, 0.24f, 0.24f, 1f);
    [SerializeField] private Color colorExperienciaNormal = new Color(0.67f, 0.35f, 1f, 1f);
    [SerializeField] private Color colorExperienciaOscura = new Color(0.25f, 0.10f, 0.44f, 1f);
    [SerializeField] private Color colorPocionActiva = new Color(0.35f, 0.95f, 0.62f, 1f);
    [SerializeField] private Color colorPocionInactiva = new Color(0.12f, 0.18f, 0.16f, 0.95f);
    [SerializeField] private Color colorParryActivo = new Color(0.68f, 0.96f, 1f, 1f);
    [SerializeField] private Color colorParryReposo = new Color(0.68f, 0.96f, 1f, 0f);

    private float valorVidaVisual = 1f;
    private float valorEstaminaVisual = 1f;
    private float valorExperienciaVisual;

    private Vector3 escalaBaseCorazon = Vector3.one;
    private float tiempoPulsoCorazonRestante;
    private float tiempoFlashVidaRestante;
    private float tiempoPulsoAgotado;
    private float vidaAnterior = -1f;
    private bool eventoVidaSuscripto;
    private Coroutine corrutinaRebindReferencias;

    private void Awake()
    {
        Instancia = this;

        if (panelGroup == null)
        {
            panelGroup = GetComponent<CanvasGroup>();
        }

        if (iconoCorazon != null)
        {
            escalaBaseCorazon = iconoCorazon.rectTransform.localScale;
        }

        BuscarReferenciasGameplay(false);
        NormalizarEtiquetasHud();
        InicializarValoresVisuales();
        SilenciarHudLegacy();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += ManejarEscenaCargada;
        RegistrarEventoVida();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= ManejarEscenaCargada;
        DesregistrarEventoVida();

        if (corrutinaRebindReferencias != null)
        {
            StopCoroutine(corrutinaRebindReferencias);
            corrutinaRebindReferencias = null;
        }

        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    private void Start()
    {
        BuscarReferenciasGameplay(true);
        SilenciarHudLegacy();
        NormalizarEtiquetasHud();
        InicializarValoresVisuales();
        ActualizarUI(true);

        if (HayReferenciasGameplayFaltantes())
        {
            LanzarRebindCorto();
        }
    }

    private void Update()
    {
        ActualizarUI(false);
    }

    private void ManejarEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        BuscarReferenciasGameplay(true);
        RegistrarEventoVida();
        SilenciarHudLegacy();
        InicializarValoresVisuales();

        if (HayReferenciasGameplayFaltantes())
        {
            LanzarRebindCorto();
        }
    }

    private void ActualizarUI(bool forzar)
    {
        NormalizarEtiquetasHud();

        float vidaActual = vidaJugador != null ? vidaJugador.VidaActual : 0f;
        float vidaMaxima = vidaJugador != null ? Mathf.Max(1f, vidaJugador.VidaMaxima) : 100f;
        float estaminaActual = estaminaJugador != null ? estaminaJugador.EstaminaActual : 0f;
        float estaminaMaxima = estaminaJugador != null ? Mathf.Max(1f, estaminaJugador.EstaminaMaxima) : 100f;
        int nivelActual = sistemaXP != null ? sistemaXP.NivelActual : 1;
        int experienciaActual = sistemaXP != null ? sistemaXP.ExperienciaActualNivel : 0;
        int experienciaRequerida = sistemaXP != null ? Mathf.Max(1, sistemaXP.ExperienciaRequeridaNivelActual) : 100;
        int oleadaActual = sistemaOleadas != null ? sistemaOleadas.OleadaActual : (gameManager != null ? gameManager.OleadaActual : 0);
        int pocionesActuales = sistemaPociones != null ? sistemaPociones.CantidadActualPociones : 0;
        int pocionesMaximas = sistemaPociones != null ? sistemaPociones.CantidadMaximaPociones : 5;
        int rachaActual = sistemaRacha != null ? sistemaRacha.RachaActual : 0;
        float tiempoProximaRonda = sistemaOleadas != null ? sistemaOleadas.TiempoRestanteCountdownEntreOleadas : 0f;
        bool esperandoProximaRonda = sistemaOleadas != null && sistemaOleadas.PausaEntreOleadasActiva;
        bool parryActivo = sistemaDefensaEspada != null && sistemaDefensaEspada.VentanaActiva;
        bool parryEnCurso = sistemaDefensaEspada != null && sistemaDefensaEspada.ParryEnCurso;

        float porcentajeVida = Mathf.Clamp01(vidaActual / vidaMaxima);
        float porcentajeEstamina = Mathf.Clamp01(estaminaActual / estaminaMaxima);
        float porcentajeExperiencia = Mathf.Clamp01((float)experienciaActual / experienciaRequerida);

        float delta = Time.unscaledDeltaTime;
        valorVidaVisual = forzar ? porcentajeVida : Mathf.Lerp(valorVidaVisual, porcentajeVida, delta * velocidadSuavizadoBarras);
        valorEstaminaVisual = forzar ? porcentajeEstamina : Mathf.Lerp(valorEstaminaVisual, porcentajeEstamina, delta * velocidadSuavizadoBarras);

        if (forzar || porcentajeExperiencia < valorExperienciaVisual - 0.45f)
        {
            valorExperienciaVisual = porcentajeExperiencia;
        }
        else
        {
            valorExperienciaVisual = Mathf.Lerp(valorExperienciaVisual, porcentajeExperiencia, delta * velocidadSuavizadoBarras);
        }

        if (fillVida != null)
        {
            fillVida.fillAmount = valorVidaVisual;
            fillVida.color = colorVidaNormal;
        }

        if (fillEstamina != null)
        {
            fillEstamina.fillAmount = valorEstaminaVisual;
            if (estaminaJugador != null && estaminaJugador.EstaAgotado)
            {
                tiempoPulsoAgotado += delta * Mathf.PI * 2f * frecuenciaPulsoAgotado;
                float tPulso = (Mathf.Sin(tiempoPulsoAgotado) + 1f) * 0.5f;
                fillEstamina.color = Color.Lerp(colorEstaminaNormal, colorEstaminaAgotada, tPulso);
            }
            else
            {
                tiempoPulsoAgotado = 0f;
                fillEstamina.color = Color.Lerp(fillEstamina.color, colorEstaminaNormal, delta * velocidadSuavizadoBarras);
            }
        }

        if (fillExperiencia != null)
        {
            fillExperiencia.fillAmount = valorExperienciaVisual;
            fillExperiencia.color = Color.Lerp(fillExperiencia.color, colorExperienciaNormal, delta * velocidadSuavizadoBarras);
        }

        if (textoVida != null)
        {
            textoVida.text = Mathf.RoundToInt(vidaActual) + " / " + Mathf.RoundToInt(vidaMaxima);
        }

        if (textoEstamina != null)
        {
            textoEstamina.text = Mathf.RoundToInt(estaminaActual) + " / " + Mathf.RoundToInt(estaminaMaxima);
            textoEstamina.color = estaminaJugador != null && estaminaJugador.EstaAgotado ? new Color(1f, 0.72f, 0.72f, 1f) : Color.white;
        }

        if (textoExperiencia != null)
        {
            textoExperiencia.text = experienciaActual + " / " + experienciaRequerida;
            textoExperiencia.color = Color.Lerp(colorExperienciaOscura, Color.white, 0.82f);
        }

        if (textoParryInferior != null)
        {
            textoParryInferior.text = parryEnCurso ? "PARRY" : string.Empty;
            Color colorParry = parryActivo
                ? Color.Lerp(colorParryActivo, Color.white, (Mathf.Sin(Time.unscaledTime * 18f) + 1f) * 0.15f)
                : (parryEnCurso ? Color.Lerp(colorParryActivo, new Color(1f, 0.82f, 0.62f, 1f), 0.35f) : colorParryReposo);
            textoParryInferior.color = colorParry;
        }

        if (textoOleadaSuperior != null)
        {
            textoOleadaSuperior.text = "Oleada: " + oleadaActual;
        }

        if (textoNivelSuperior != null)
        {
            textoNivelSuperior.text = "Nivel: " + nivelActual;
        }

        if (textoProximaRondaSuperior != null)
        {
            textoProximaRondaSuperior.text = esperandoProximaRonda
                ? "Próx. ronda: " + Mathf.CeilToInt(tiempoProximaRonda) + "s"
                : "Próx. ronda: en combate";
        }

        if (textoExperienciaSuperior != null)
        {
            textoExperienciaSuperior.text = "XP: " + experienciaActual + " / " + experienciaRequerida;
        }

        if (textoPocionesSuperior != null)
        {
            textoPocionesSuperior.text = "Pociones: " + pocionesActuales + " / " + pocionesMaximas;
        }

        if (textoRachaSuperior != null)
        {
            textoRachaSuperior.text = "Racha: x" + Mathf.Max(0, rachaActual);
        }

        if (textoPocionesInferior != null)
        {
            textoPocionesInferior.text = "Pociones " + pocionesActuales + " / " + pocionesMaximas;
        }

        if (textoStats != null)
        {
            textoStats.text = string.Empty;
        }

        if (ranurasPociones != null)
        {
            for (int indice = 0; indice < ranurasPociones.Length; indice++)
            {
                Image ranuraActual = ranurasPociones[indice];
                if (ranuraActual == null)
                {
                    continue;
                }

                bool activa = indice < pocionesActuales;
                ranuraActual.color = activa ? colorPocionActiva : colorPocionInactiva;
            }
        }

        AnimarPulsoCorazon(delta);
        AnimarFlashVida(delta);
        AnimarFadePanel(delta);
        vidaAnterior = vidaActual;
    }

    private void NormalizarEtiquetasHud()
    {
        NormalizarTextoGrafico(iconoCorazon, "HP", 18f);
        NormalizarTextoGrafico(iconoEstamina, "STA", 14f);
        NormalizarTextoGrafico(iconoExperiencia, "XP", 18f);
    }

    private void NormalizarTextoGrafico(Graphic grafico, string textoEsperado, float tamanoFuente)
    {
        if (grafico == null)
        {
            return;
        }

        TMP_Text textoTMP = grafico as TMP_Text;
        if (textoTMP == null)
        {
            textoTMP = grafico.GetComponent<TMP_Text>();
        }

        if (textoTMP == null)
        {
            return;
        }

        textoTMP.text = textoEsperado;
        textoTMP.fontSize = tamanoFuente;
        textoTMP.enableWordWrapping = false;
    }

    private void RegistrarEventoVida()
    {
        DesregistrarEventoVida();

        if (vidaJugador == null)
        {
            return;
        }

        vidaJugador.AlVidaActualizada += ManejarVidaActualizada;
        eventoVidaSuscripto = true;
    }

    private void DesregistrarEventoVida()
    {
        if (!eventoVidaSuscripto || vidaJugador == null)
        {
            eventoVidaSuscripto = false;
            return;
        }

        vidaJugador.AlVidaActualizada -= ManejarVidaActualizada;
        eventoVidaSuscripto = false;
    }

    private void ManejarVidaActualizada(float vidaActual, float vidaMaxima)
    {
        if (vidaAnterior >= 0f && vidaActual < vidaAnterior)
        {
            tiempoPulsoCorazonRestante = duracionPulsoCorazon;
            tiempoFlashVidaRestante = duracionFlashVida;
        }

        vidaAnterior = vidaActual;
    }

    private void AnimarPulsoCorazon(float delta)
    {
        if (iconoCorazon == null)
        {
            return;
        }

        if (escalaBaseCorazon == Vector3.zero)
        {
            escalaBaseCorazon = Vector3.one;
        }

        if (tiempoPulsoCorazonRestante > 0f)
        {
            tiempoPulsoCorazonRestante -= delta;
            float t = 1f - Mathf.Clamp01(tiempoPulsoCorazonRestante / duracionPulsoCorazon);
            iconoCorazon.rectTransform.localScale = Vector3.Lerp(escalaBaseCorazon * 1.18f, escalaBaseCorazon, t);
        }
        else
        {
            iconoCorazon.rectTransform.localScale = Vector3.Lerp(iconoCorazon.rectTransform.localScale, escalaBaseCorazon, delta * 12f);
        }
    }

    private void AnimarFlashVida(float delta)
    {
        if (brilloVida == null)
        {
            return;
        }

        if (tiempoFlashVidaRestante > 0f)
        {
            tiempoFlashVidaRestante -= delta;
            float t = Mathf.Clamp01(tiempoFlashVidaRestante / duracionFlashVida);
            brilloVida.color = new Color(1f, 1f, 1f, t * 0.72f);
        }
        else
        {
            brilloVida.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void AnimarFadePanel(float delta)
    {
        float alphaObjetivo = 1f;
        if (gameManager != null && gameManager.EstadoActual == EstadoPartida.GameOver)
        {
            alphaObjetivo = 0f;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, alphaObjetivo, delta * velocidadFadePanel);
        }

        if (panelesSecundarios != null)
        {
            for (int indice = 0; indice < panelesSecundarios.Length; indice++)
            {
                CanvasGroup panelSecundario = panelesSecundarios[indice];
                if (panelSecundario == null)
                {
                    continue;
                }

                if (panelSecundario.gameObject != null && panelSecundario.gameObject.name == "MapaGrandeHUD")
                {
                    continue;
                }

                panelSecundario.alpha = Mathf.Lerp(panelSecundario.alpha, alphaObjetivo, delta * velocidadFadePanel);
            }
        }
    }

    private void BuscarReferenciasGameplay(bool logWarnings)
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instancia != null ? GameManager.Instancia : FindObjectOfType<GameManager>(true);
        }

        GameObject jugadorPrincipal = null;
        if (gameManager != null && gameManager.JugadorPrincipal != null)
        {
            jugadorPrincipal = gameManager.JugadorPrincipal;
        }

        if (jugadorPrincipal == null)
        {
            try
            {
                jugadorPrincipal = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
                jugadorPrincipal = null;
            }
        }

        if (jugadorPrincipal == null)
        {
            VidaJugador vidaEncontrada = FindObjectOfType<VidaJugador>(true);
            if (vidaEncontrada != null)
            {
                jugadorPrincipal = vidaEncontrada.gameObject;
            }
        }

        if (jugadorPrincipal != null)
        {
            if (vidaJugador == null)
            {
                vidaJugador = jugadorPrincipal.GetComponent<VidaJugador>();
            }

            if (estaminaJugador == null)
            {
                estaminaJugador = jugadorPrincipal.GetComponent<Estamina>();
            }

            if (sistemaXP == null)
            {
                sistemaXP = jugadorPrincipal.GetComponent<SistemaXP>();
            }

            if (sistemaPociones == null)
            {
                sistemaPociones = jugadorPrincipal.GetComponent<SistemaPociones>();
            }

            if (sistemaRacha == null)
            {
                sistemaRacha = jugadorPrincipal.GetComponent<SistemaRacha>();
            }

            if (sistemaDefensaEspada == null)
            {
                sistemaDefensaEspada = jugadorPrincipal.GetComponent<SistemaDefensaEspada>();
            }
        }

        if (sistemaOleadas == null)
        {
            sistemaOleadas = FindObjectOfType<SistemaOleadas>(true);
        }

        if (logWarnings)
        {
            if (vidaJugador == null) Debug.LogWarning("[UIJugador] No se encontro VidaJugador en la escena.");
            if (estaminaJugador == null) Debug.LogWarning("[UIJugador] No se encontro Estamina en la escena.");
            if (sistemaXP == null) Debug.LogWarning("[UIJugador] No se encontro SistemaXP en la escena.");
            if (sistemaPociones == null) Debug.LogWarning("[UIJugador] No se encontro SistemaPociones en la escena.");
            if (sistemaRacha == null) Debug.LogWarning("[UIJugador] No se encontro SistemaRacha en la escena.");
            if (sistemaOleadas == null) Debug.LogWarning("[UIJugador] No se encontro SistemaOleadas en la escena.");
        }
    }

    private void InicializarValoresVisuales()
    {
        float vidaActual = vidaJugador != null ? vidaJugador.VidaActual : 100f;
        float vidaMaxima = vidaJugador != null ? Mathf.Max(1f, vidaJugador.VidaMaxima) : 100f;
        float estaminaActual = estaminaJugador != null ? estaminaJugador.EstaminaActual : 100f;
        float estaminaMaxima = estaminaJugador != null ? Mathf.Max(1f, estaminaJugador.EstaminaMaxima) : 100f;
        float experienciaActual = sistemaXP != null ? sistemaXP.ExperienciaActualNivel : 0f;
        float experienciaRequerida = sistemaXP != null ? Mathf.Max(1f, sistemaXP.ExperienciaRequeridaNivelActual) : 100f;

        valorVidaVisual = Mathf.Clamp01(vidaActual / vidaMaxima);
        valorEstaminaVisual = Mathf.Clamp01(estaminaActual / estaminaMaxima);
        valorExperienciaVisual = Mathf.Clamp01(experienciaActual / experienciaRequerida);
        vidaAnterior = vidaActual;

        if (fillVida != null) fillVida.fillAmount = valorVidaVisual;
        if (fillEstamina != null) fillEstamina.fillAmount = valorEstaminaVisual;
        if (fillExperiencia != null) fillExperiencia.fillAmount = valorExperienciaVisual;
    }

    private void SilenciarHudLegacy()
    {
        if (sistemaOleadas != null)
        {
            sistemaOleadas.EstablecerHudOnGuiVisible(false);
        }

        if (sistemaPociones != null)
        {
            sistemaPociones.EstablecerHudOnGuiVisible(false);
        }

        if (sistemaRacha != null)
        {
            sistemaRacha.EstablecerHudOnGuiVisible(false);
        }

        BarraXPUI barraXPUI = FindObjectOfType<BarraXPUI>(true);
        if (barraXPUI != null)
        {
            barraXPUI.enabled = false;
        }

        DesactivarObjetoUI("BarraVida");
        DesactivarObjetoUI("BarraStamina");
        DesactivarObjetoUI("BarraEstamina");
    }

    private void DesactivarObjetoUI(string nombreObjeto)
    {
        GameObject objeto = GameObject.Find(nombreObjeto);
        if (objeto != null && objeto != gameObject)
        {
            objeto.SetActive(false);
        }
    }

    private bool HayReferenciasGameplayFaltantes()
    {
        return vidaJugador == null || estaminaJugador == null || sistemaXP == null || sistemaPociones == null || sistemaRacha == null || sistemaOleadas == null;
    }

    private void LanzarRebindCorto()
    {
        if (corrutinaRebindReferencias != null)
        {
            StopCoroutine(corrutinaRebindReferencias);
        }

        corrutinaRebindReferencias = StartCoroutine(CorrutinaRebindReferencias());
    }

    private IEnumerator CorrutinaRebindReferencias()
    {
        for (int intento = 0; intento < 8; intento++)
        {
            if (!HayReferenciasGameplayFaltantes())
            {
                corrutinaRebindReferencias = null;
                yield break;
            }

            BuscarReferenciasGameplay(false);
            SilenciarHudLegacy();
            yield return new WaitForSecondsRealtime(0.2f);
        }

        corrutinaRebindReferencias = null;
    }
}
