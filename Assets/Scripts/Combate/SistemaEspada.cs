using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sistema de espada estilo God of War: auto-giro al objetivo, plantado y dano por Animation Event.
public class SistemaEspada : MonoBehaviour
{
    private static Material materialImpactoCompartido;

    private struct ObjetivoAtaque
    {
        public Collider colliderObjetivo;
        public IRecibidorDanio receptorDanio;
        public MonoBehaviour comportamientoReceptor;
        public ZonasDebiles zonaDebil;
        public FeedbackCombate feedbackCombate;
        public Vector3 puntoImpacto;
        public float distancia;
        public float puntaje;
    }

    [SerializeField] private HitboxEspada hitboxEspada;
    [SerializeField] private Transform pivoteVisualEspada;
    [SerializeField] private LayerMask mascaraObjetivosAtaque = ~0;
    [SerializeField] private float danioBaseRespaldo = 20f;
    [SerializeField] private float multiplicadorDanioGolpeFuerte = 2.5f;
    [SerializeField] private float rangoAtaqueNormal = 2.5f;
    [SerializeField] private float rangoAtaqueFuerte = 3.2f;
    [SerializeField] private float rangoAutoTarget = 4f;
    [SerializeField] private float anguloFrontalAtaque = 120f;
    [SerializeField] private float enfriamientoAtaqueNormal = 0.5f;
    [SerializeField] private float enfriamientoAtaqueFuerte = 1.2f;
    [SerializeField] private float duracionPlantadoNormal = 0.6f;
    [SerializeField] private float duracionPlantadoFuerte = 1.05f;
    [SerializeField] private float tiempoRespaldoImpactoNormal = 0.28f;
    [SerializeField] private float tiempoRespaldoImpactoFuerte = 0.52f;
    [SerializeField] private float duracionHitStopNormal = 0.05f;
    [SerializeField] private float duracionHitStopFuerte = 0.1f;
    [SerializeField] private float alturaOrigenAtaque = 1f;
    [SerializeField] private bool usarGiroVisual = true;
    [SerializeField] private float anguloMaximoGiroVisualNormal = 28f;
    [SerializeField] private float anguloMaximoGiroVisualFuerte = 42f;

    private readonly List<Collider> resultadosBusquedaObjetivos = new List<Collider>();
    private EstadisticasJugador estadisticasJugador;
    private Estamina estaminaJugador;
    private SistemaDefensaEspada sistemaDefensaEspada;
    private ControladorAnimacionJugador controladorAnimacionJugador;
    private Animator animatorJugadorVisual;
    private Rigidbody cuerpoRigido;
    private FeedbackCombate feedbackObjetivoResaltadoActual;
    private GameObject objetivoActualEnRango;
    private Coroutine corrutinaLiberarAtaque;
    private Coroutine corrutinaImpactoRespaldo;
    private Coroutine corrutinaHitStopActiva;
    private float timeScaleAnteriorHitStop = 1f;
    private float tiempoProximoAtaque;
    private int tokenAtaqueActual;
    private int tokenPendienteImpacto;
    private bool ataqueEnCurso;
    private bool ataquePendienteActivo;
    private bool ataquePendienteFueFuerte;
    private bool impactoYaAplicadoEnAtaqueActual;
    private Vector3 ultimaDireccionSwing = Vector3.forward;
    private bool ultimoAtaqueFueFuerte;
    private TipoZonaDanio tipoZonaObjetivoActual = TipoZonaDanio.Cuerpo;

    public Vector3 UltimaDireccionSwing => ultimaDireccionSwing;
    public bool UltimoAtaqueFueFuerte => ultimoAtaqueFueFuerte;
    public bool HayObjetivoEnRangoActual => objetivoActualEnRango != null;
    public TipoZonaDanio TipoZonaObjetivoActual => tipoZonaObjetivoActual;
    public bool EstaAtacando => ataqueEnCurso;

    private void Awake()
    {
        estadisticasJugador = GetComponent<EstadisticasJugador>();
        estaminaJugador = GetComponent<Estamina>();
        sistemaDefensaEspada = GetComponent<SistemaDefensaEspada>();
        cuerpoRigido = GetComponent<Rigidbody>();
        hitboxEspada = hitboxEspada != null ? hitboxEspada : GetComponentInChildren<HitboxEspada>(true);
        controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        animatorJugadorVisual = GetComponentInChildren<Animator>(true);
    }

    private void Update()
    {
        ActualizarObjetivoEnRango();

        if (ataqueEnCurso)
        {
            return;
        }

        if (sistemaDefensaEspada == null)
        {
            sistemaDefensaEspada = GetComponent<SistemaDefensaEspada>();
        }

        if (sistemaDefensaEspada != null && sistemaDefensaEspada.BloqueaMovimiento)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            IntentarAtaque(false);
        }

        if (Input.GetMouseButtonDown(1) && !EstaPresionadoModificadorParry())
        {
            IntentarAtaque(true);
        }
    }

    private void OnDisable()
    {
        LimpiarObjetivoResaltadoActual();
        ataqueEnCurso = false;
        ataquePendienteActivo = false;
        impactoYaAplicadoEnAtaqueActual = false;
        tokenAtaqueActual = 0;
        tokenPendienteImpacto = 0;
        EstablecerPlantado(false);

        if (corrutinaLiberarAtaque != null) StopCoroutine(corrutinaLiberarAtaque);
        if (corrutinaImpactoRespaldo != null) StopCoroutine(corrutinaImpactoRespaldo);
        if (corrutinaHitStopActiva != null)
        {
            StopCoroutine(corrutinaHitStopActiva);
            Time.timeScale = timeScaleAnteriorHitStop;
        }
    }

    private void IntentarAtaque(bool solicitarGolpeFuerte)
    {
        if (sistemaDefensaEspada == null)
        {
            sistemaDefensaEspada = GetComponent<SistemaDefensaEspada>();
        }

        if (sistemaDefensaEspada != null && sistemaDefensaEspada.BloqueaMovimiento)
        {
            return;
        }

        if (estaminaJugador != null && !estaminaJugador.PuedeAtacar) return;
        if (Time.time < tiempoProximoAtaque) return;

        bool esGolpeFuerte = false;
        if (solicitarGolpeFuerte)
        {
            esGolpeFuerte = estaminaJugador == null || estaminaJugador.IntentarConsumirGolpeFuerte();
            if (!esGolpeFuerte) return;
        }

        ObjetivoAtaque objetivoAutoTarget;
        if (IntentarBuscarObjetivoAutoTarget(rangoAutoTarget, out objetivoAutoTarget))
        {
            Vector3 dir = objetivoAutoTarget.puntoImpacto - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        ultimaDireccionSwing = ObtenerDireccionMiradaAtaque();
        ultimoAtaqueFueFuerte = esGolpeFuerte;
        ataqueEnCurso = true;
        ataquePendienteActivo = true;
        ataquePendienteFueFuerte = esGolpeFuerte;
        impactoYaAplicadoEnAtaqueActual = false;
        tokenAtaqueActual++;
        tokenPendienteImpacto = tokenAtaqueActual;
        tiempoProximoAtaque = Time.time + (esGolpeFuerte ? enfriamientoAtaqueFuerte : enfriamientoAtaqueNormal);

        if (cuerpoRigido != null) cuerpoRigido.velocity = new Vector3(0f, cuerpoRigido.velocity.y, 0f);

        EstablecerPlantado(true);
        EventosJuego.NotificarJugadorLanzoAtaque(gameObject, esGolpeFuerte);

        controladorAnimacionJugador = controladorAnimacionJugador != null ? controladorAnimacionJugador : GetComponentInChildren<ControladorAnimacionJugador>(true);
        if (controladorAnimacionJugador != null)
        {
            if (esGolpeFuerte) controladorAnimacionJugador.ReproducirAtaqueFuerte();
            else controladorAnimacionJugador.ReproducirAtaqueNormal();
        }
        else
        {
            DispararTriggerAnimatorRespaldo(esGolpeFuerte ? "AttackFuerte" : "AttackNormal");
            DispararTriggerAnimatorRespaldo("Attack");
        }

        if (corrutinaLiberarAtaque != null) StopCoroutine(corrutinaLiberarAtaque);
        corrutinaLiberarAtaque = StartCoroutine(LiberarAtaque(esGolpeFuerte ? duracionPlantadoFuerte : duracionPlantadoNormal));

        if (corrutinaImpactoRespaldo != null) StopCoroutine(corrutinaImpactoRespaldo);
        corrutinaImpactoRespaldo = StartCoroutine(CorrutinaImpactoRespaldo(tokenPendienteImpacto, esGolpeFuerte));

        if (usarGiroVisual && pivoteVisualEspada != null)
        {
            StartCoroutine(CorrutinaGiroVisual(ultimaDireccionSwing, esGolpeFuerte));
        }
    }

    private IEnumerator LiberarAtaque(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        ataqueEnCurso = false;
        EstablecerPlantado(false);
        corrutinaLiberarAtaque = null;
    }

    public void OnImpactoNormal()
    {
        ResolverImpactoPendiente(tokenPendienteImpacto, false, true);
    }

    public void OnImpactoFuerte()
    {
        ResolverImpactoPendiente(tokenPendienteImpacto, true, true);
    }

    public void AplicarDanioNormal() => OnImpactoNormal();
    public void AplicarDanioFuerte() => OnImpactoFuerte();

    public void OnSwordImpact()
    {
        if (ataquePendienteFueFuerte) OnImpactoFuerte();
        else OnImpactoNormal();
    }

    private IEnumerator CorrutinaImpactoRespaldo(int tokenAtaque, bool esGolpeFuerte)
    {
        float tiempoEspera = esGolpeFuerte ? tiempoRespaldoImpactoFuerte : tiempoRespaldoImpactoNormal;
        yield return new WaitForSeconds(tiempoEspera);

        if (tokenAtaque != tokenAtaqueActual || tokenAtaque != tokenPendienteImpacto)
        {
            corrutinaImpactoRespaldo = null;
            yield break;
        }

        if (!ataquePendienteActivo || impactoYaAplicadoEnAtaqueActual)
        {
            corrutinaImpactoRespaldo = null;
            yield break;
        }

        Debug.Log("[SistemaEspada] Impacto aplicado por respaldo temporal.");
        ResolverImpactoPendiente(tokenAtaque, esGolpeFuerte, false);
        corrutinaImpactoRespaldo = null;
    }

    private void ResolverImpactoPendiente(int tokenAtaque, bool esGolpeFuerte, bool vinoDesdeEvento)
    {
        if (tokenAtaque != tokenAtaqueActual || tokenAtaque != tokenPendienteImpacto)
        {
            return;
        }

        if (!ataquePendienteActivo)
        {
            return;
        }

        if (ataquePendienteFueFuerte != esGolpeFuerte)
        {
            return;
        }

        if (impactoYaAplicadoEnAtaqueActual)
        {
            return;
        }

        impactoYaAplicadoEnAtaqueActual = true;
        tokenPendienteImpacto = 0;
        AplicarDanioEnCono(esGolpeFuerte ? ObtenerDanioFuerteActual() : ObtenerDanioNormalActual(), esGolpeFuerte);
        ataquePendienteActivo = false;

        if (corrutinaImpactoRespaldo != null && vinoDesdeEvento)
        {
            StopCoroutine(corrutinaImpactoRespaldo);
            corrutinaImpactoRespaldo = null;
        }
    }

    private void AplicarDanioEnCono(float danioBaseGolpe, bool esGolpeFuerte)
    {
        ObjetivoAtaque objetivo;
        bool encontroObjetivo = IntentarBuscarMejorObjetivoEnRango(esGolpeFuerte ? rangoAtaqueFuerte : rangoAtaqueNormal, out objetivo);
        if (!encontroObjetivo)
        {
            Debug.Log("[SistemaEspada] Swing en el aire.");
            return;
        }

        TipoZonaDanio tipoZona = objetivo.zonaDebil != null ? objetivo.zonaDebil.TipoZona : TipoZonaDanio.Cuerpo;
        float multiplicadorZona = objetivo.zonaDebil != null ? objetivo.zonaDebil.ObtenerMultiplicador() : 1f;
        EnemigoDummy enemigoDummy = objetivo.comportamientoReceptor != null
            ? objetivo.comportamientoReceptor.GetComponentInParent<EnemigoDummy>() ?? objetivo.comportamientoReceptor.GetComponent<EnemigoDummy>()
            : null;

        if (enemigoDummy != null)
        {
            multiplicadorZona *= enemigoDummy.ConsumirMultiplicadorVulnerabilidadParry();
        }

        DatosDanio datosDanio = new DatosDanio
        {
            atacante = gameObject,
            objetivo = objetivo.comportamientoReceptor.gameObject,
            danioBase = danioBaseGolpe,
            multiplicadorZona = multiplicadorZona,
            tipoZona = tipoZona,
            puntoImpacto = objetivo.puntoImpacto,
            direccionImpacto = ultimaDireccionSwing,
            esGolpeFuerte = esGolpeFuerte
        };

        objetivo.receptorDanio.RecibirDanio(datosDanio);
        IniciarHitStop(esGolpeFuerte ? duracionHitStopFuerte : duracionHitStopNormal);
        TriggerScreenShake(esGolpeFuerte ? 0.3f : 0.1f, esGolpeFuerte ? 0.25f : 0.15f);
        CrearImpactoParticulas(objetivo.puntoImpacto, ultimaDireccionSwing, tipoZona, esGolpeFuerte);
        if (objetivo.feedbackCombate != null) objetivo.feedbackCombate.MostrarDestello(tipoZona);
    }

    private bool IntentarBuscarMejorObjetivoEnRango(float rangoAtaque, out ObjetivoAtaque mejorObjetivo)
    {
        mejorObjetivo = default;
        Vector3 origen = transform.position + Vector3.up * alturaOrigenAtaque;
        Vector3 direccionMirada = ObtenerDireccionMiradaAtaque();
        if (direccionMirada.sqrMagnitude < 0.001f) return false;

        Collider[] colliders = Physics.OverlapSphere(origen, rangoAtaque, mascaraObjetivosAtaque, QueryTriggerInteraction.Collide);
        resultadosBusquedaObjetivos.Clear();
        resultadosBusquedaObjetivos.AddRange(colliders);

        float mejorPuntaje = float.MinValue;
        for (int i = 0; i < resultadosBusquedaObjetivos.Count; i++)
        {
            Collider colliderActual = resultadosBusquedaObjetivos[i];
            if (colliderActual == null) continue;
            if (colliderActual.transform.root == transform.root) continue;
            if (DebeIgnorarColliderGenerico(colliderActual)) continue;

            IRecibidorDanio receptor = colliderActual.GetComponentInParent<IRecibidorDanio>();
            MonoBehaviour comportamiento = receptor as MonoBehaviour;
            if (receptor == null || comportamiento == null) continue;

            Vector3 punto = colliderActual.ClosestPoint(origen);
            if ((punto - origen).sqrMagnitude < 0.0001f) punto = comportamiento.transform.position;

            Vector3 dir = punto - origen;
            dir.y = 0f;
            float distancia = dir.magnitude;
            if (distancia <= 0.001f || distancia > rangoAtaque) continue;

            Vector3 dirNormalizada = dir / distancia;
            float angulo = Vector3.Angle(direccionMirada, dirNormalizada);
            if (angulo > anguloFrontalAtaque * 0.5f) continue;

            ZonasDebiles zona = colliderActual.GetComponent<ZonasDebiles>() ?? colliderActual.GetComponentInParent<ZonasDebiles>();
            FeedbackCombate feedback = colliderActual.GetComponentInParent<FeedbackCombate>();

            float puntaje = Vector3.Dot(direccionMirada, dirNormalizada) * 10f;
            puntaje += -distancia;
            puntaje += zona != null ? zona.ObtenerMultiplicador() * 0.02f : 0f;

            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejorObjetivo.colliderObjetivo = colliderActual;
                mejorObjetivo.receptorDanio = receptor;
                mejorObjetivo.comportamientoReceptor = comportamiento;
                mejorObjetivo.zonaDebil = zona;
                mejorObjetivo.feedbackCombate = feedback;
                mejorObjetivo.puntoImpacto = punto;
                mejorObjetivo.distancia = distancia;
                mejorObjetivo.puntaje = puntaje;
            }
        }

        return mejorObjetivo.comportamientoReceptor != null;
    }

    private bool IntentarBuscarObjetivoAutoTarget(float rangoBusqueda, out ObjetivoAtaque mejorObjetivo)
    {
        mejorObjetivo = default;
        Vector3 origen = transform.position + Vector3.up * alturaOrigenAtaque;
        Collider[] colliders = Physics.OverlapSphere(origen, rangoBusqueda, mascaraObjetivosAtaque, QueryTriggerInteraction.Collide);

        float mejorDistancia = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider colliderActual = colliders[i];
            if (colliderActual == null) continue;
            if (colliderActual.transform.root == transform.root) continue;
            if (DebeIgnorarColliderGenerico(colliderActual)) continue;

            IRecibidorDanio receptor = colliderActual.GetComponentInParent<IRecibidorDanio>();
            MonoBehaviour comportamiento = receptor as MonoBehaviour;
            if (receptor == null || comportamiento == null) continue;

            Vector3 punto = colliderActual.ClosestPoint(origen);
            if ((punto - origen).sqrMagnitude < 0.0001f) punto = comportamiento.transform.position;

            Vector3 dir = punto - origen;
            dir.y = 0f;
            float distancia = dir.magnitude;
            if (distancia <= 0.001f || distancia > rangoBusqueda) continue;

            ZonasDebiles zona = colliderActual.GetComponent<ZonasDebiles>() ?? colliderActual.GetComponentInParent<ZonasDebiles>();
            FeedbackCombate feedback = colliderActual.GetComponentInParent<FeedbackCombate>();

            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorObjetivo.colliderObjetivo = colliderActual;
                mejorObjetivo.receptorDanio = receptor;
                mejorObjetivo.comportamientoReceptor = comportamiento;
                mejorObjetivo.zonaDebil = zona;
                mejorObjetivo.feedbackCombate = feedback;
                mejorObjetivo.puntoImpacto = punto;
                mejorObjetivo.distancia = distancia;
                mejorObjetivo.puntaje = -distancia;
            }
        }

        return mejorObjetivo.comportamientoReceptor != null;
    }

    private bool DebeIgnorarColliderGenerico(Collider colliderActual)
    {
        if (colliderActual.GetComponent<ZonasDebiles>() != null) return false;
        return colliderActual.GetComponentInChildren<ZonasDebiles>() != null;
    }

    private void ActualizarObjetivoEnRango()
    {
        if (estaminaJugador != null && !estaminaJugador.PuedeAtacar)
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        float rangoDisponible = ObtenerRangoMayorDisponibleActual();
        if (rangoDisponible <= 0f)
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        ObjetivoAtaque objetivo;
        if (!IntentarBuscarMejorObjetivoEnRango(rangoDisponible, out objetivo))
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        objetivoActualEnRango = objetivo.comportamientoReceptor.gameObject;
        tipoZonaObjetivoActual = objetivo.zonaDebil != null ? objetivo.zonaDebil.TipoZona : TipoZonaDanio.Cuerpo;

        if (feedbackObjetivoResaltadoActual != null && feedbackObjetivoResaltadoActual != objetivo.feedbackCombate)
        {
            feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(false);
        }

        feedbackObjetivoResaltadoActual = objetivo.feedbackCombate;
        if (feedbackObjetivoResaltadoActual != null) feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(true);
    }

    private void LimpiarObjetivoResaltadoActual()
    {
        if (feedbackObjetivoResaltadoActual != null) feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(false);
        feedbackObjetivoResaltadoActual = null;
        objetivoActualEnRango = null;
        tipoZonaObjetivoActual = TipoZonaDanio.Cuerpo;
    }

    private float ObtenerRangoMayorDisponibleActual()
    {
        if (estaminaJugador == null) return Mathf.Max(rangoAtaqueNormal, rangoAtaqueFuerte);
        if (!estaminaJugador.PuedeAtacar) return 0f;
        if (estaminaJugador.PuedeUsarGolpeFuerte) return Mathf.Max(rangoAtaqueNormal, rangoAtaqueFuerte);
        return rangoAtaqueNormal;
    }

    private Vector3 ObtenerDireccionMiradaAtaque()
    {
        Vector3 direccionMirada = transform.forward;
        direccionMirada.y = 0f;
        if (direccionMirada.sqrMagnitude < 0.001f) direccionMirada = Vector3.forward;
        return direccionMirada.normalized;
    }

    private float ObtenerDanioNormalActual() => estadisticasJugador != null ? Mathf.Max(1f, estadisticasJugador.DanioActual) : danioBaseRespaldo;
    private float ObtenerDanioFuerteActual() => ObtenerDanioNormalActual() * multiplicadorDanioGolpeFuerte;

    private bool EstaPresionadoModificadorParry()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void EstablecerPlantado(bool plantado)
    {
        controladorAnimacionJugador = controladorAnimacionJugador != null ? controladorAnimacionJugador : GetComponentInChildren<ControladorAnimacionJugador>(true);
        if (controladorAnimacionJugador != null) controladorAnimacionJugador.EstablecerPlantado(plantado);

        animatorJugadorVisual = animatorJugadorVisual != null ? animatorJugadorVisual : GetComponentInChildren<Animator>(true);
        if (animatorJugadorVisual != null && AnimatorTieneParametro(animatorJugadorVisual, "Plantado", AnimatorControllerParameterType.Bool))
        {
            animatorJugadorVisual.SetBool("Plantado", plantado);
        }
    }

    private void DispararTriggerAnimatorRespaldo(string nombreTrigger)
    {
        animatorJugadorVisual = animatorJugadorVisual != null ? animatorJugadorVisual : GetComponentInChildren<Animator>(true);
        if (animatorJugadorVisual == null) return;
        if (!AnimatorTieneParametro(animatorJugadorVisual, nombreTrigger, AnimatorControllerParameterType.Trigger)) return;
        animatorJugadorVisual.ResetTrigger(nombreTrigger);
        animatorJugadorVisual.SetTrigger(nombreTrigger);
    }

    private bool AnimatorTieneParametro(Animator animator, string nombre, AnimatorControllerParameterType tipo)
    {
        if (animator == null) return false;
        AnimatorControllerParameter[] parametros = animator.parameters;
        for (int i = 0; i < parametros.Length; i++)
        {
            if (parametros[i].name == nombre) return parametros[i].type == tipo;
        }
        return false;
    }

    private IEnumerator CorrutinaGiroVisual(Vector3 direccionSwing, bool esGolpeFuerte)
    {
        if (pivoteVisualEspada == null) yield break;

        float signo = Vector3.Dot(transform.right, direccionSwing) >= 0f ? 1f : -1f;
        Quaternion rotacionOriginal = pivoteVisualEspada.localRotation;
        float angulo = esGolpeFuerte ? anguloMaximoGiroVisualFuerte : anguloMaximoGiroVisualNormal;
        Quaternion rotacionIda = rotacionOriginal * Quaternion.Euler(0f, 0f, -angulo * signo);

        float duracionIda = esGolpeFuerte ? 0.12f : 0.06f;
        float tiempo = 0f;
        while (tiempo < duracionIda)
        {
            tiempo += Time.deltaTime;
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionOriginal, rotacionIda, Mathf.Clamp01(tiempo / duracionIda));
            yield return null;
        }

        float duracionVuelta = esGolpeFuerte ? 0.18f : 0.1f;
        tiempo = 0f;
        while (tiempo < duracionVuelta)
        {
            tiempo += Time.deltaTime;
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionIda, rotacionOriginal, Mathf.Clamp01(tiempo / duracionVuelta));
            yield return null;
        }

        pivoteVisualEspada.localRotation = rotacionOriginal;
    }

    private void IniciarHitStop(float duracion)
    {
        if (duracion <= 0f) return;
        if (corrutinaHitStopActiva != null)
        {
            StopCoroutine(corrutinaHitStopActiva);
            Time.timeScale = timeScaleAnteriorHitStop;
        }
        corrutinaHitStopActiva = StartCoroutine(RutinaHitStop(duracion));
    }

    private IEnumerator RutinaHitStop(float duracion)
    {
        timeScaleAnteriorHitStop = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duracion);
        Time.timeScale = timeScaleAnteriorHitStop;
        corrutinaHitStopActiva = null;
    }

    private void TriggerScreenShake(float intensidad, float duracion)
    {
        CamaraTercerPersona camara = FindObjectOfType<CamaraTercerPersona>();
        if (camara != null) camara.TriggerShake(intensidad, duracion);
    }

    private void CrearImpactoParticulas(Vector3 puntoImpacto, Vector3 direccionImpacto, TipoZonaDanio tipoZona, bool esGolpeFuerte)
    {
        GameObject go = new GameObject("ImpactoEspada_Runtime");
        go.SetActive(false);
        go.transform.position = puntoImpacto;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = esGolpeFuerte ? 0.5f : 0.25f;
        main.startLifetime = esGolpeFuerte ? 0.45f : 0.25f;
        main.startSpeed = esGolpeFuerte ? 7f : 4f;
        main.startSize = esGolpeFuerte ? 0.18f : 0.1f;
        main.startColor = ObtenerColorImpacto(tipoZona, esGolpeFuerte);
        main.maxParticles = esGolpeFuerte ? 20 : 10;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(esGolpeFuerte ? 20 : 10)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.05f;

        go.transform.rotation = Quaternion.LookRotation(direccionImpacto.sqrMagnitude > 0.001f ? direccionImpacto : Vector3.forward);

        ParticleSystemRenderer rendererParticulas = ps.GetComponent<ParticleSystemRenderer>();
        if (rendererParticulas != null)
        {
            rendererParticulas.renderMode = ParticleSystemRenderMode.Billboard;
            rendererParticulas.sharedMaterial = ObtenerMaterialImpactoCompartido();
        }

        go.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);
        ps.Play(true);
        Object.Destroy(go, 1.5f);
    }

    private static Material ObtenerMaterialImpactoCompartido()
    {
        if (materialImpactoCompartido != null) return materialImpactoCompartido;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        materialImpactoCompartido = new Material(shader);
        materialImpactoCompartido.name = "MaterialImpactoEspada_Runtime";
        if (materialImpactoCompartido.HasProperty("_Color")) materialImpactoCompartido.SetColor("_Color", Color.white);
        if (materialImpactoCompartido.HasProperty("_BaseColor")) materialImpactoCompartido.SetColor("_BaseColor", Color.white);
        return materialImpactoCompartido;
    }

    private Color ObtenerColorImpacto(TipoZonaDanio tipoZona, bool esGolpeFuerte)
    {
        if (esGolpeFuerte) return new Color(1f, 0.4f, 0.1f, 1f);
        if (tipoZona == TipoZonaDanio.Cabeza) return new Color(1f, 0.95f, 0.55f, 1f);
        return new Color(1f, 0.9f, 0.3f, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origen = transform.position + Vector3.up * alturaOrigenAtaque;
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(origen, rangoAtaqueNormal);
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(origen, rangoAutoTarget);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Vector3 frente = ObtenerDireccionMiradaAtaque();
        Vector3 izq = Quaternion.Euler(0f, -anguloFrontalAtaque * 0.5f, 0f) * frente;
        Vector3 der = Quaternion.Euler(0f, anguloFrontalAtaque * 0.5f, 0f) * frente;
        Gizmos.DrawRay(origen, izq * rangoAtaqueNormal);
        Gizmos.DrawRay(origen, der * rangoAtaqueNormal);
        Gizmos.DrawRay(origen, frente * rangoAtaqueNormal);
    }
}
