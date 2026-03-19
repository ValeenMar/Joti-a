using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script centraliza el puente entre el movimiento, el combate y el Animator
// del jugador para que la capa visual se mantenga separada de la logica de gameplay.

// Esta clase controla los parametros y triggers del Animator del jugador.
[RequireComponent(typeof(Animator))]
public class ControladorAnimacionJugador : MonoBehaviour
{
    // Nombre del parametro float que controla si el jugador esta quieto o moviendose.
    private const string ParametroVelocidad = "Velocidad";

    // Nombre del parametro bool que marca si el jugador esta corriendo.
    private const string ParametroCorriendo = "Corriendo";

    // Nombre del trigger que reproduce la animacion de ataque normal.
    private const string TriggerAtaqueNormal = "AtaqueNormal";

    // Nombre del trigger que reproduce la animacion de ataque fuerte.
    private const string TriggerAtaqueFuerte = "AtaqueFuerte";

    // Nombre del trigger que reproduce la animacion de recibir dano.
    private const string TriggerRecibirDanio = "RecibirDanio";

    // Nombre del trigger que reproduce la animacion de morir.
    private const string TriggerMorir = "Morir";

    // Esta referencia apunta al Animator principal del jugador.
    [SerializeField] private Animator animatorJugador;

    // Esta referencia guarda el Rigidbody por si luego queremos leer velocidad real como respaldo.
    [SerializeField] private Rigidbody cuerpoRigido;

    // Esta referencia apunta al sistema de espada del jugador para reenviar Animation Events.
    [SerializeField] private SistemaEspada sistemaEspada;

    // Esta variable guarda la velocidad visual mas reciente que recibio el animator.
    [SerializeField] private float velocidadMovimientoActual;

    // Esta variable guarda si el jugador corre en este momento.
    [SerializeField] private bool corriendoActual;

    // Esta variable evita repetir busquedas caras cada frame cuando ya tenemos referencias validas.
    private bool referenciasListas;

    // Esta funcion se ejecuta una vez al iniciar el objeto.
    private void Awake()
    {
        // Intentamos enlazar todo lo que necesitemos de entrada.
        ReasignarReferencias();
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Al volver a activarse, reintentamos enlazar por si hubo reload de escena.
        ReasignarReferencias();
    }

    // Esta funcion se ejecuta cada frame para mantener los parametros sincronizados.
    private void LateUpdate()
    {
        // Si por algun motivo las referencias se perdieron, reintentamos una vez.
        if (!referenciasListas)
        {
            ReasignarReferencias();
        }

        // Si no hay Animator, no podemos hacer nada mas.
        if (animatorJugador == null)
        {
            return;
        }

        // Mantenemos el float de velocidad siempre actualizado.
        animatorJugador.SetFloat(ParametroVelocidad, Mathf.Max(0f, velocidadMovimientoActual));

        // Mantenemos el bool de carrera siempre actualizado.
        animatorJugador.SetBool(ParametroCorriendo, corriendoActual);
    }

    // Este metodo guarda la velocidad y el estado de sprint que llegan desde el movimiento.
    public void SincronizarMovimiento(float velocidadVisual, bool estaCorriendo)
    {
        // Guardamos la velocidad para el siguiente LateUpdate.
        velocidadMovimientoActual = Mathf.Max(0f, velocidadVisual);

        // Guardamos si este frame el jugador corre.
        corriendoActual = estaCorriendo;

        // Si el Animator esta listo, actualizamos de inmediato para que no llegue con retraso.
        if (animatorJugador != null)
        {
            animatorJugador.SetFloat(ParametroVelocidad, velocidadMovimientoActual);
            animatorJugador.SetBool(ParametroCorriendo, corriendoActual);
        }
    }

    // Este metodo dispara la animacion de ataque normal.
    public void ReproducirAtaqueNormal()
    {
        // Reintentamos referencias si hiciera falta antes de disparar el trigger.
        ReasignarReferencias();

        // Si el Animator existe, reiniciamos y dispararmos el trigger correcto.
        DispararTrigger(TriggerAtaqueNormal);
    }

    // Este metodo dispara la animacion de ataque fuerte.
    public void ReproducirAtaqueFuerte()
    {
        // Reintentamos referencias si hiciera falta antes de disparar el trigger.
        ReasignarReferencias();

        // Si el Animator existe, reiniciamos y dispararmos el trigger correcto.
        DispararTrigger(TriggerAtaqueFuerte);
    }

    // Este metodo dispara la animacion de recibir dano.
    public void ReproducirRecibirDanio()
    {
        // Reintentamos referencias si hiciera falta antes de disparar el trigger.
        ReasignarReferencias();

        // Si el Animator existe, reiniciamos y dispararmos el trigger correcto.
        DispararTrigger(TriggerRecibirDanio);
    }

    // Este metodo dispara la animacion de muerte.
    public void ReproducirMorir()
    {
        // Reintentamos referencias si hiciera falta antes de disparar el trigger.
        ReasignarReferencias();

        // Si el Animator existe, reiniciamos y dispararmos el trigger correcto.
        DispararTrigger(TriggerMorir);
    }

    // Este metodo busca nuevamente Animator y Rigidbody por si la escena se recargo.
    private void ReasignarReferencias()
    {
        // Intentamos obtener el Animator del mismo objeto.
        if (animatorJugador == null)
        {
            animatorJugador = GetComponent<Animator>();
        }

        // Si no estaba en este objeto, lo buscamos en hijos como respaldo.
        if (animatorJugador == null)
        {
            animatorJugador = GetComponentInChildren<Animator>(true);
        }

        // Buscamos el Rigidbody del mismo objeto si faltaba.
        if (cuerpoRigido == null)
        {
            cuerpoRigido = GetComponent<Rigidbody>();
        }

        // Si falta la espada en este objeto, la buscamos en el padre porque la logica vive en la raiz del jugador.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si encontramos Animator, lo dejamos listo para usar en gameplay y muerte sin depender del time scale.
        if (animatorJugador != null)
        {
            animatorJugador.applyRootMotion = false;
            animatorJugador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorJugador.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // Marcamos si ya tenemos lo minimo listo.
        referenciasListas = animatorJugador != null;
    }

    // Este metodo lo llama un Animation Event del ataque normal para aplicar el dano justo en el frame correcto.
    public void AplicarDanioNormal()
    {
        // Reintentamos obtener la espada por si la referencia se perdio en un reload.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si existe la espada, delegamos el impacto real.
        if (sistemaEspada != null)
        {
            sistemaEspada.AplicarDanioNormal();
        }
    }

    // Este metodo lo llama un Animation Event del ataque fuerte para aplicar el dano en el frame correcto.
    public void AplicarDanioFuerte()
    {
        // Reintentamos obtener la espada por si la referencia se perdio en un reload.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si existe la espada, delegamos el impacto real.
        if (sistemaEspada != null)
        {
            sistemaEspada.AplicarDanioFuerte();
        }
    }

    // Este metodo dispara un trigger del Animator de forma segura.
    private void DispararTrigger(string nombreTrigger)
    {
        // Si no hay Animator, no hacemos nada.
        if (animatorJugador == null)
        {
            return;
        }

        // Limpiamos el trigger por si habia quedado pendiente de una accion previa.
        animatorJugador.ResetTrigger(nombreTrigger);

        // Lanzamos el trigger para que el Animator cambie de estado.
        animatorJugador.SetTrigger(nombreTrigger);
    }

    // COPILOT-EXPAND: Aqui podes agregar sincronizacion de red, capas de animacion y blend trees mas avanzados.
}
