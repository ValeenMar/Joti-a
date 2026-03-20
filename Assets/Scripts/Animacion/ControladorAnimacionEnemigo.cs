using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// COPILOT-CONTEXT:
// Realm Brawl, Unity 2022.3.
// Controla la animacion del esqueleto enemigo y expone hooks para ataque,
// recibir dano y terminar la muerte por Animation Events.

[RequireComponent(typeof(Animator))]
public class ControladorAnimacionEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animador;
    [SerializeField] private EnemigoDummy enemigoDummy;
    [SerializeField] private FeedbackCombate feedbackCombate;
    [SerializeField] private NavMeshAgent agenteNavMesh;
    [SerializeField] private Transform raizVisual;

    [Header("Parametros Animator")]
    [SerializeField] private string parametroVelocidad = "Velocidad";
    [SerializeField] private string parametroPersiguiendo = "Persiguiendo";
    [SerializeField] private string triggerAtacar = "Atacar";
    [SerializeField] private string triggerRecibirDanio = "RecibirDanio";
    [SerializeField] private string triggerMorir = "Morir";

    [Header("Sacudida")]
    [SerializeField] private float duracionSacudidaNormal = 0.12f;
    [SerializeField] private float duracionSacudidaCritica = 0.16f;
    [SerializeField] private float intensidadSacudidaNormal = 0.03f;
    [SerializeField] private float intensidadSacudidaCritica = 0.06f;

    [Header("Respaldo")]
    [SerializeField] private float tiempoRespaldoMuerte = 1.15f;

    public event Action OnMuerteAnimacionTerminada;

    private bool muerteEnCurso;
    private bool muerteNotificada;
    private bool impactoAtaqueConsumidoEnEsteSwing;
    private Coroutine rutinaSacudidaActiva;
    private Coroutine rutinaRespaldoMuerte;
    private Vector3 posicionBaseRaizVisual;
    private Quaternion rotacionBaseRaizVisual;

    private void Awake()
    {
        BuscarReferencias();
        GuardarPoseBase();
    }

    private void OnEnable()
    {
        BuscarReferencias();
        GuardarPoseBase();
    }

    private void OnDisable()
    {
        if (rutinaSacudidaActiva != null)
        {
            StopCoroutine(rutinaSacudidaActiva);
            rutinaSacudidaActiva = null;
        }

        if (rutinaRespaldoMuerte != null)
        {
            StopCoroutine(rutinaRespaldoMuerte);
            rutinaRespaldoMuerte = null;
        }

        muerteEnCurso = false;
        muerteNotificada = false;
        impactoAtaqueConsumidoEnEsteSwing = false;
    }

    private void LateUpdate()
    {
        if (animador == null || enemigoDummy == null || muerteEnCurso)
        {
            return;
        }

        float velocidadActual = 0f;
        if (agenteNavMesh != null && agenteNavMesh.enabled)
        {
            float velocidadAgente = Mathf.Max(0.01f, agenteNavMesh.speed);
            velocidadActual = Mathf.Clamp01(agenteNavMesh.velocity.magnitude / velocidadAgente);
        }

        EstablecerFloatSiExiste(parametroVelocidad, velocidadActual);
        EstablecerBoolSiExiste(parametroPersiguiendo, enemigoDummy.EstadoActual == EstadosEnemigo.Perseguir || enemigoDummy.EstadoActual == EstadosEnemigo.Atacar);
    }

    private void BuscarReferencias()
    {
        if (animador == null)
        {
            animador = GetComponent<Animator>();
        }

        if (enemigoDummy == null)
        {
            enemigoDummy = GetComponentInParent<EnemigoDummy>();
        }

        if (feedbackCombate == null)
        {
            feedbackCombate = GetComponentInParent<FeedbackCombate>();
        }

        if (agenteNavMesh == null)
        {
            agenteNavMesh = GetComponentInParent<NavMeshAgent>();
        }

        if (raizVisual == null)
        {
            raizVisual = transform;
        }

        if (animador != null)
        {
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    private void GuardarPoseBase()
    {
        if (raizVisual == null)
        {
            return;
        }

        posicionBaseRaizVisual = raizVisual.localPosition;
        rotacionBaseRaizVisual = raizVisual.localRotation;
    }

    public void ReproducirAtacar()
    {
        if (animador == null || muerteEnCurso)
        {
            return;
        }

        impactoAtaqueConsumidoEnEsteSwing = false;
        ResetearTriggerSiExiste(triggerRecibirDanio);
        ResetearTriggerSiExiste(triggerMorir);
        DispararTriggerSiExiste(triggerAtacar);
    }

    public void ReproducirRecibirDanio(DatosDanio datosDanio)
    {
        if (animador == null || muerteEnCurso)
        {
            return;
        }

        ResetearTriggerSiExiste(triggerAtacar);
        ResetearTriggerSiExiste(triggerMorir);
        DispararTriggerSiExiste(triggerRecibirDanio);

        if (feedbackCombate != null)
        {
            feedbackCombate.MostrarDestello(datosDanio != null ? datosDanio.tipoZona : TipoZonaDanio.Cuerpo);
        }

        bool esCritico = datosDanio != null && datosDanio.FueCritico;
        float intensidad = esCritico ? intensidadSacudidaCritica : intensidadSacudidaNormal;
        float duracion = esCritico ? duracionSacudidaCritica : duracionSacudidaNormal;

        if (rutinaSacudidaActiva != null)
        {
            StopCoroutine(rutinaSacudidaActiva);
        }

        rutinaSacudidaActiva = StartCoroutine(RutinaSacudida(intensidad, duracion));
    }

    public void ReproducirMorir()
    {
        if (animador == null || muerteEnCurso)
        {
            NotificarMuerteAnimacionTerminada();
            return;
        }

        muerteEnCurso = true;
        muerteNotificada = false;
        ResetearTriggerSiExiste(triggerAtacar);
        ResetearTriggerSiExiste(triggerRecibirDanio);
        DispararTriggerSiExiste(triggerMorir);

        if (rutinaRespaldoMuerte != null)
        {
            StopCoroutine(rutinaRespaldoMuerte);
        }

        rutinaRespaldoMuerte = StartCoroutine(RutinaRespaldoMuerte());
    }

    public bool EstaMuerteEnCurso => muerteEnCurso;

    public void AplicarDanioAtaque()
    {
        IntentarAplicarDanioAtaque();
    }

    public void OnImpactoNormal()
    {
        IntentarAplicarDanioAtaque();
    }

    public void OnImpactoFuerte()
    {
        IntentarAplicarDanioAtaque();
    }

    private void IntentarAplicarDanioAtaque()
    {
        if (enemigoDummy == null || impactoAtaqueConsumidoEnEsteSwing)
        {
            return;
        }

        impactoAtaqueConsumidoEnEsteSwing = true;
        enemigoDummy.AplicarDanioAtaqueDesdeAnimacion();
    }

    public void NotificarMuerteAnimacionTerminada()
    {
        if (muerteNotificada)
        {
            return;
        }

        muerteNotificada = true;

        if (rutinaRespaldoMuerte != null)
        {
            StopCoroutine(rutinaRespaldoMuerte);
            rutinaRespaldoMuerte = null;
        }

        OnMuerteAnimacionTerminada?.Invoke();
    }

    private IEnumerator RutinaSacudida(float intensidad, float duracion)
    {
        if (raizVisual == null)
        {
            yield break;
        }

        Vector3 posicionBase = posicionBaseRaizVisual;
        Quaternion rotacionBase = rotacionBaseRaizVisual;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float factor = 1f - Mathf.Clamp01(tiempoTranscurrido / duracion);

            raizVisual.localPosition = posicionBase + UnityEngine.Random.insideUnitSphere * intensidad * factor;
            raizVisual.localRotation = rotacionBase * Quaternion.Euler(
                UnityEngine.Random.Range(-4f, 4f) * factor,
                UnityEngine.Random.Range(-4f, 4f) * factor,
                UnityEngine.Random.Range(-4f, 4f) * factor);

            yield return null;
        }

        raizVisual.localPosition = posicionBase;
        raizVisual.localRotation = rotacionBase;
        rutinaSacudidaActiva = null;
    }

    private IEnumerator RutinaRespaldoMuerte()
    {
        yield return new WaitForSeconds(tiempoRespaldoMuerte);
        NotificarMuerteAnimacionTerminada();
    }

    private void EstablecerFloatSiExiste(string nombreParametro, float valor)
    {
        if (!AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Float))
        {
            return;
        }

        animador.SetFloat(nombreParametro, valor, 0.12f, Time.deltaTime);
    }

    private void EstablecerBoolSiExiste(string nombreParametro, bool valor)
    {
        if (!AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animador.SetBool(nombreParametro, valor);
    }

    private void DispararTriggerSiExiste(string nombreTrigger)
    {
        if (!AnimatorTieneParametro(nombreTrigger, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        animador.ResetTrigger(nombreTrigger);
        animador.SetTrigger(nombreTrigger);
    }

    private void ResetearTriggerSiExiste(string nombreTrigger)
    {
        if (!AnimatorTieneParametro(nombreTrigger, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        animador.ResetTrigger(nombreTrigger);
    }

    private bool AnimatorTieneParametro(string nombreParametro, AnimatorControllerParameterType tipoParametro)
    {
        if (animador == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parametros = animador.parameters;
        for (int indiceParametro = 0; indiceParametro < parametros.Length; indiceParametro++)
        {
            if (parametros[indiceParametro].name == nombreParametro && parametros[indiceParametro].type == tipoParametro)
            {
                return true;
            }
        }

        return false;
    }

    // COPILOT-EXPAND:
    // Aqui se puede sumar Blend Trees, sincronizacion Mirror, hits reactions mas complejas
    // y variantes de muerte o bloqueo sin reescribir la IA.
}
