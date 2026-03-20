using System.Collections;
using UnityEngine;

// Este script agrega una defensa pura de parry con la misma espada del jugador.
public class SistemaDefensaEspada : MonoBehaviour
{
    // Esta tecla secundaria permite hacer parry usando la misma logica que el sprint del proyecto.
    [Header("Input")]
    [SerializeField] private KeyCode teclaModificadorParryPrincipal = KeyCode.LeftShift;
    [SerializeField] private KeyCode teclaModificadorParrySecundaria = KeyCode.RightShift;

    // Estos tiempos definen la ventana completa del parry puro.
    [Header("Ventanas")]
    [SerializeField] private float duracionAnticipacion = 0.06f;
    [SerializeField] private float duracionVentanaActiva = 0.14f;
    [SerializeField] private float duracionRecuperacionFallida = 0.32f;
    [SerializeField] private float duracionRecuperacionExitosa = 0.18f;

    // Estos valores controlan lo que le pasa al enemigo cuando el parry entra.
    [Header("Resultado")]
    [SerializeField] private float duracionAturdimientoEnemigo = 0.7f;
    [SerializeField] private float duracionVulnerabilidadEnemigo = 1.2f;
    [SerializeField] private float multiplicadorContraataque = 1.5f;
    [SerializeField] private float duracionHitStopParry = 0.08f;
    [SerializeField] private float intensidadShakeParry = 0.14f;
    [SerializeField] private float duracionShakeParry = 0.16f;

    // Esta referencia apunta al puente de animacion del jugador.
    private ControladorAnimacionJugador controladorAnimacionJugador;

    // Esta referencia apunta al sistema ofensivo para evitar mezclar estados.
    private SistemaEspada sistemaEspada;

    // Esta referencia apunta a la vida del jugador para no permitir parry muerto.
    private VidaJugador vidaJugador;

    // Esta referencia apunta al rigidbody para frenar el movimiento durante la defensa.
    private Rigidbody cuerpoRigido;

    // Esta referencia apunta a la camara principal para disparar shake.
    private CamaraTercerPersona camaraTercerPersona;

    // Esta corrutina controla toda la ventana del parry.
    private Coroutine corrutinaParryActiva;

    // Esta corrutina controla un hit stop corto al hacer parry perfecto.
    private Coroutine corrutinaHitStopActiva;

    // Este valor guarda el timeScale previo al hit stop.
    private float timeScaleAnterior = 1f;

    // Estas banderas exponen el estado de defensa.
    private bool parryEnCurso;
    private bool ventanaParryActiva;
    private bool parryResueltoConExito;
    private bool bloqueaMovimiento;

    // Esta propiedad permite a otros sistemas saber si el jugador esta bloqueado por la defensa.
    public bool BloqueaMovimiento => bloqueaMovimiento;

    // Esta propiedad permite saber si el jugador esta en la ventana que realmente para golpes.
    public bool VentanaActiva => ventanaParryActiva;

    // Esta propiedad expone si el jugador sigue dentro de toda la secuencia del parry.
    public bool ParryEnCurso => parryEnCurso;

    // Esta funcion se ejecuta al crear el componente.
    private void Awake()
    {
        controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        sistemaEspada = GetComponent<SistemaEspada>();
        vidaJugador = GetComponent<VidaJugador>();
        cuerpoRigido = GetComponent<Rigidbody>();
        camaraTercerPersona = FindObjectOfType<CamaraTercerPersona>(true);
    }

    // Esta funcion se ejecuta cada frame para leer el input del parry.
    private void Update()
    {
        // Si no se puede procesar input de defensa, no hacemos nada.
        if (!PuedeProcesarInputParry())
        {
            return;
        }

        // El parry puro usa Shift mas click derecho.
        if (Input.GetMouseButtonDown(1) && EstaPresionadoModificadorParry())
        {
            IniciarParry();
        }
    }

    // Esta funcion limpia corrutinas y estados si el componente se apaga.
    private void OnDisable()
    {
        if (corrutinaParryActiva != null)
        {
            StopCoroutine(corrutinaParryActiva);
            corrutinaParryActiva = null;
        }

        if (corrutinaHitStopActiva != null)
        {
            StopCoroutine(corrutinaHitStopActiva);
            corrutinaHitStopActiva = null;
            Time.timeScale = timeScaleAnterior;
        }

        RestaurarEstadoReposo();
    }

    // Este metodo intenta consumir un golpe enemigo dentro de la ventana activa.
    public bool IntentarResolverParry(DatosDanio datosDanio)
    {
        // Si no estamos en ventana activa o los datos son invalidos, no hay parry.
        if (!ventanaParryActiva || datosDanio == null || datosDanio.atacante == null)
        {
            return false;
        }

        // Intentamos obtener el enemigo real que nos golpeo.
        EnemigoDummy enemigo = datosDanio.atacante.GetComponentInParent<EnemigoDummy>() ?? datosDanio.atacante.GetComponent<EnemigoDummy>();
        if (enemigo == null)
        {
            return false;
        }

        // Marcamos exito para no ejecutar la recuperacion fallida.
        parryResueltoConExito = true;
        ventanaParryActiva = false;

        // Si habia una rutina de ventana, la cortamos y pasamos directo a la recuperacion buena.
        if (corrutinaParryActiva != null)
        {
            StopCoroutine(corrutinaParryActiva);
        }

        corrutinaParryActiva = StartCoroutine(RutinaRecuperacionParry(true));

        // Aplicamos el castigo al enemigo.
        enemigo.AplicarAturdimientoParry(duracionAturdimientoEnemigo);
        enemigo.MarcarVulnerablePorParry(duracionVulnerabilidadEnemigo, multiplicadorContraataque);

        // Disparamos feedback global para UI, audio y efectos.
        EventosJuego.NotificarParryExitoso(gameObject, enemigo.gameObject);
        SistemaAudio.Instancia?.ReproducirGolpe(TipoGolpe.Parry);

        // Hacemos un pequeno hit stop y shake para vender el impacto.
        SolicitarHitStop(duracionHitStopParry);
        if (camaraTercerPersona == null)
        {
            camaraTercerPersona = FindObjectOfType<CamaraTercerPersona>(true);
        }

        if (camaraTercerPersona != null)
        {
            camaraTercerPersona.TriggerShake(intensidadShakeParry, duracionShakeParry);
        }

        Debug.Log("[SistemaDefensaEspada] Parry exitoso.");
        return true;
    }

    // Este metodo define si el sistema puede leer una nueva orden de defensa.
    private bool PuedeProcesarInputParry()
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (vidaJugador != null && !vidaJugador.EstaVivo)
        {
            return false;
        }

        if (parryEnCurso || bloqueaMovimiento || ventanaParryActiva)
        {
            return false;
        }

        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponent<SistemaEspada>();
        }

        if (sistemaEspada != null && sistemaEspada.EstaAtacando)
        {
            return false;
        }

        return true;
    }

    // Este metodo detecta si Shift esta presionado en cualquiera de sus dos teclas.
    private bool EstaPresionadoModificadorParry()
    {
        return Input.GetKey(teclaModificadorParryPrincipal) || Input.GetKey(teclaModificadorParrySecundaria);
    }

    // Este metodo inicia toda la secuencia del parry.
    private void IniciarParry()
    {
        parryEnCurso = true;
        parryResueltoConExito = false;
        bloqueaMovimiento = true;

        if (cuerpoRigido != null)
        {
            cuerpoRigido.velocity = new Vector3(0f, cuerpoRigido.velocity.y, 0f);
        }

        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.EstablecerPlantado(true);
            controladorAnimacionJugador.ReproducirParry();
        }

        if (corrutinaParryActiva != null)
        {
            StopCoroutine(corrutinaParryActiva);
        }

        corrutinaParryActiva = StartCoroutine(RutinaParry());
        Debug.Log("[SistemaDefensaEspada] Parry iniciado.");
    }

    // Esta corrutina cubre anticipacion, ventana activa y recuperacion fallida.
    private IEnumerator RutinaParry()
    {
        ventanaParryActiva = false;

        yield return new WaitForSeconds(Mathf.Max(0.01f, duracionAnticipacion));
        ventanaParryActiva = true;

        yield return new WaitForSeconds(Mathf.Max(0.01f, duracionVentanaActiva));
        ventanaParryActiva = false;

        yield return new WaitForSeconds(Mathf.Max(0.01f, duracionRecuperacionFallida));

        EventosJuego.NotificarParryFallido(gameObject);
        RestaurarEstadoReposo();
        corrutinaParryActiva = null;
        Debug.Log("[SistemaDefensaEspada] Parry fallido.");
    }

    // Esta corrutina cubre la salida mas corta cuando el parry salio bien.
    private IEnumerator RutinaRecuperacionParry(bool fueExitoso)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, fueExitoso ? duracionRecuperacionExitosa : duracionRecuperacionFallida));
        RestaurarEstadoReposo();
        corrutinaParryActiva = null;
    }

    // Este metodo devuelve el sistema a reposo despues de defender.
    private void RestaurarEstadoReposo()
    {
        parryEnCurso = false;
        ventanaParryActiva = false;
        bloqueaMovimiento = false;
        parryResueltoConExito = false;

        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.FinalizarParry();
            controladorAnimacionJugador.EstablecerPlantado(false);
        }
    }

    // Este metodo dispara un hit stop corto usando tiempo real.
    private void SolicitarHitStop(float duracion)
    {
        if (corrutinaHitStopActiva != null)
        {
            StopCoroutine(corrutinaHitStopActiva);
            Time.timeScale = timeScaleAnterior;
        }

        corrutinaHitStopActiva = StartCoroutine(RutinaHitStop(duracion));
    }

    // Esta corrutina congela un instante el tiempo para vender el parry.
    private IEnumerator RutinaHitStop(float duracion)
    {
        timeScaleAnterior = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duracion));
        Time.timeScale = timeScaleAnterior;
        corrutinaHitStopActiva = null;
    }
}
