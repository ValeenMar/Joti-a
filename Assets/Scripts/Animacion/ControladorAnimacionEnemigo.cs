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
            velocidadActual = agenteNavMesh.velocity.magnitude;
        }

        animador.SetFloat(parametroVelocidad, velocidadActual);
        animador.SetBool(parametroPersiguiendo, enemigoDummy.EstadoActual == EstadosEnemigo.Perseguir);
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

        animador.ResetTrigger(triggerRecibirDanio);
        animador.ResetTrigger(triggerMorir);
        animador.SetTrigger(triggerAtacar);
    }

    public void ReproducirRecibirDanio(DatosDanio datosDanio)
    {
        if (animador == null || muerteEnCurso)
        {
            return;
        }

        animador.ResetTrigger(triggerAtacar);
        animador.ResetTrigger(triggerMorir);
        animador.SetTrigger(triggerRecibirDanio);

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
        if (animador == null)
        {
            NotificarMuerteAnimacionTerminada();
            return;
        }

        muerteEnCurso = true;
        muerteNotificada = false;
        animador.ResetTrigger(triggerAtacar);
        animador.ResetTrigger(triggerRecibirDanio);
        animador.SetTrigger(triggerMorir);

        if (rutinaRespaldoMuerte != null)
        {
            StopCoroutine(rutinaRespaldoMuerte);
        }

        rutinaRespaldoMuerte = StartCoroutine(RutinaRespaldoMuerte());
    }

    public void AplicarDanioAtaque()
    {
        if (enemigoDummy == null)
        {
            return;
        }

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

    // COPILOT-EXPAND:
    // Aqui se puede sumar Blend Trees, sincronizacion Mirror, hits reactions mas complejas
    // y variantes de muerte o bloqueo sin reescribir la IA.
}
