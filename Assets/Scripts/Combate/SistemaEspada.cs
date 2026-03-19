using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase controla el ataque de espada basado en movimiento previo del mouse.
public class SistemaEspada : MonoBehaviour
{
    // Esta estructura guarda una muestra de movimiento de mouse con su tiempo.
    private struct MuestraMouse
    {
        // Esta variable guarda el delta de mouse del frame.
        public Vector2 deltaMouse;

        // Esta variable guarda el tiempo exacto de la muestra.
        public float tiempoMuestra;
    }

    // Esta estructura guarda toda la informacion del mejor objetivo encontrado para este ataque.
    private struct ObjetivoAtaque
    {
        // Esta referencia guarda el collider exacto que vamos a golpear.
        public Collider colliderObjetivo;

        // Esta referencia guarda el receptor real de dano del objetivo.
        public IRecibidorDanio receptorDanio;

        // Esta referencia guarda el comportamiento del receptor para acceder a su GameObject.
        public MonoBehaviour comportamientoReceptor;

        // Esta referencia guarda la zona debil encontrada, si existe.
        public ZonasDebiles zonaDebil;

        // Esta referencia guarda el feedback visual del objetivo para destellos y resaltado.
        public FeedbackCombate feedbackCombate;

        // Este vector guarda el punto concreto donde vamos a considerar que impacto el golpe.
        public Vector3 puntoImpacto;

        // Este valor guarda la distancia al objetivo en el plano.
        public float distancia;
    }

    // Esta referencia apunta al hitbox antiguo de la espada por compatibilidad futura.
    [SerializeField] private HitboxEspada hitboxEspada;

    // Esta referencia opcional al objeto visual de la espada para girarlo durante swing.
    [SerializeField] private Transform pivoteVisualEspada;

    // Esta mascara limita que colliders pueden entrar en el auto-target del ataque.
    [SerializeField] private LayerMask mascaraObjetivosAtaque = ~0;

    // Esta variable define cuanto tiempo guardamos del historial del mouse.
    [SerializeField] private float duracionBufferMouse = 0.2f;

    // Esta variable define el danio base si no hay EstadisticasJugador.
    [SerializeField] private float danioBaseRespaldo = 10f;

    // Esta variable multiplica el danio cuando el jugador logra hacer un golpe fuerte.
    [SerializeField] private float multiplicadorDanioGolpeFuerte = 2f;

    // Esta variable define el rango del golpe normal.
    [SerializeField] private float rangoAtaqueNormal = 2.5f;

    // Esta variable define el rango del golpe fuerte.
    [SerializeField] private float rangoAtaqueFuerte = 3.5f;

    // Esta variable define el angulo total del cono de ataque frente al jugador.
    [SerializeField] private float anguloFrontalAtaque = 90f;

    // Esta variable define el delay del golpe normal antes de aplicar dano.
    [SerializeField] private float delayAntesDelDanioNormal = 0.1f;

    // Esta variable define el delay del golpe fuerte antes de aplicar dano.
    [SerializeField] private float delayAntesDelDanioFuerte = 0.22f;

    // Esta variable define el tiempo minimo entre golpes normales.
    [SerializeField] private float enfriamientoAtaqueNormal = 0.35f;

    // Esta variable define el tiempo minimo entre golpes fuertes.
    [SerializeField] private float enfriamientoAtaqueFuerte = 0.7f;

    // Esta variable define una altura de origen para calcular mejor los objetivos del ataque.
    [SerializeField] private float alturaOrigenAtaque = 1f;

    // Esta variable define un minimo de movimiento de mouse para validar swing.
    [SerializeField] private float minimoMagnitudBuffer = 0.05f;

    // Esta variable controla si queremos un giro visual simple de espada.
    [SerializeField] private bool usarGiroVisual = true;

    // Esta variable define grados maximos del giro visual en golpe normal.
    [SerializeField] private float anguloMaximoGiroVisualNormal = 28f;

    // Esta variable define grados maximos del giro visual en golpe fuerte.
    [SerializeField] private float anguloMaximoGiroVisualFuerte = 42f;

    // Esta cola guarda muestras recientes de mouse.
    private readonly Queue<MuestraMouse> bufferMouse = new Queue<MuestraMouse>();

    // Esta lista reutilizable evita crear basura cada frame al buscar objetivos cercanos.
    private readonly List<Collider> resultadosBusquedaObjetivos = new List<Collider>();

    // Esta referencia guarda estadisticas de jugador para escalar danio.
    private EstadisticasJugador estadisticasJugador;

    // Esta referencia guarda la estamina del jugador para sprint y golpe fuerte.
    private Estamina estaminaJugador;

    // Esta referencia guarda la camara principal para orientar mejor el ataque y el cono frontal.
    private Camera camaraPrincipal;

    // Esta referencia guarda el controlador de animacion del jugador para disparar ataques visuales.
    private ControladorAnimacionJugador controladorAnimacionJugador;

    // Esta variable indica si estamos en medio de un ataque.
    private bool ataqueEnCurso;

    // Esta variable guarda el tiempo desde el que se permite volver a atacar.
    private float tiempoProximoAtaque;

    // Esta variable guarda la direccion del ultimo swing para debug y uso externo.
    private Vector3 ultimaDireccionSwing = Vector3.forward;

    // Esta variable indica si el ultimo ataque disparado fue fuerte o normal.
    private bool ultimoAtaqueFueFuerte;

    // Esta referencia guarda el feedback del objetivo resaltado en este momento.
    private FeedbackCombate feedbackObjetivoResaltadoActual;

    // Esta referencia guarda el GameObject actual en rango de ataque.
    private GameObject objetivoActualEnRango;

    // Esta variable guarda si hay un ataque animado esperando el evento de danio.
    private bool ataquePendienteActivo;

    // Esta variable guarda si el ataque pendiente es fuerte o normal.
    private bool ataquePendienteFueFuerte;

    // Esta variable guarda la direccion del swing del ataque pendiente.
    private Vector3 direccionSwingPendiente = Vector3.forward;

    // Esta variable guarda el multiplicador de danio del ataque pendiente.
    private float multiplicadorDanioPendiente = 1f;

    // Esta variable guarda el rango real del ataque pendiente.
    private float rangoAtaquePendiente = 2.5f;

    // Esta variable guarda una corrutina de seguridad por si el evento de animacion no se dispara.
    private Coroutine corrutinaSeguridadAtaque;

    // Esta funcion publica permite leer la direccion del ultimo swing.
    public Vector3 UltimaDireccionSwing => ultimaDireccionSwing;

    // Esta propiedad permite saber si el ultimo golpe fue fuerte.
    public bool UltimoAtaqueFueFuerte => ultimoAtaqueFueFuerte;

    // Esta propiedad permite saber si hoy hay un enemigo valido dentro del rango de ataque disponible.
    public bool HayObjetivoEnRangoActual => objetivoActualEnRango != null;

    // Esta funcion se ejecuta una vez al activar el objeto.
    private void Awake()
    {
        // Buscamos estadisticas en este objeto para usar danio por nivel.
        estadisticasJugador = GetComponent<EstadisticasJugador>();

        // Buscamos tambien la estamina en este mismo objeto para conectar golpe fuerte y agotamiento.
        estaminaJugador = GetComponent<Estamina>();

        // Si no asignaron hitbox en inspector, intentamos buscarla en hijos para no perder la referencia.
        if (hitboxEspada == null)
        {
            hitboxEspada = GetComponentInChildren<HitboxEspada>();
        }

        // Buscamos el controlador de animacion en el mismo objeto para sincronizar triggers de ataque.
        controladorAnimacionJugador = GetComponent<ControladorAnimacionJugador>();

        // Si no estaba en la raiz, lo buscamos en hijos porque el Animator visual vive en el modelo 3D.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        // Intentamos guardar la camara principal desde el inicio.
        camaraPrincipal = Camera.main;
    }

    // Esta funcion corre cada frame y se usa para input, buffer y resaltado de rango.
    private void Update()
    {
        // Guardamos muestra actual del mouse para usar historial reciente del swing.
        RegistrarMuestraMouse();

        // Actualizamos que enemigo esta dentro del rango real de ataque para el crosshair y el material.
        ActualizarObjetivoEnRango();

        // Si el jugador hizo click izquierdo, intentamos un golpe normal.
        if (Input.GetMouseButtonDown(0))
        {
            IntentarAtaque(false);
        }

        // Si el jugador hizo click derecho, intentamos un golpe fuerte.
        if (Input.GetMouseButtonDown(1))
        {
            IntentarAtaque(true);
        }
    }

    // Esta funcion se ejecuta al desactivar el componente y limpia el resaltado del enemigo actual.
    private void OnDisable()
    {
        // Si habia un enemigo resaltado, apagamos su indicador para no dejarlo rojo al salir.
        if (feedbackObjetivoResaltadoActual != null)
        {
            feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(false);
        }

        // Limpiamos las referencias internas de resaltado actual.
        feedbackObjetivoResaltadoActual = null;
        objetivoActualEnRango = null;

        // Limpamos cualquier ataque pendiente para no dejar estados colgados al desactivar el objeto.
        ataquePendienteActivo = false;
        ataqueEnCurso = false;

        // Si habia una corrutina de seguridad, la frenamos.
        if (corrutinaSeguridadAtaque != null)
        {
            StopCoroutine(corrutinaSeguridadAtaque);
            corrutinaSeguridadAtaque = null;
        }
    }

    // Esta funcion registra el movimiento de mouse del frame en la cola.
    private void RegistrarMuestraMouse()
    {
        // Leemos el movimiento horizontal del mouse en este frame.
        float deltaX = Input.GetAxisRaw("Mouse X");

        // Leemos el movimiento vertical del mouse en este frame.
        float deltaY = Input.GetAxisRaw("Mouse Y");

        // Creamos una muestra nueva con datos y tiempo.
        MuestraMouse muestra;
        muestra.deltaMouse = new Vector2(deltaX, deltaY);
        muestra.tiempoMuestra = Time.time;

        // Guardamos la muestra en el buffer.
        bufferMouse.Enqueue(muestra);

        // Quitamos del buffer muestras mas viejas que la ventana permitida.
        while (bufferMouse.Count > 0 && Time.time - bufferMouse.Peek().tiempoMuestra > duracionBufferMouse)
        {
            bufferMouse.Dequeue();
        }
    }

    // Esta funcion decide si el jugador puede iniciar un ataque y si sera normal o fuerte.
    private void IntentarAtaque(bool solicitarGolpeFuerte)
    {
        // Si hay estamina y esta agotado, no puede atacar de ninguna forma.
        if (estaminaJugador != null && !estaminaJugador.PuedeAtacar)
        {
            return;
        }

        // Si ya estamos atacando, no permitimos otro ataque encima.
        if (ataqueEnCurso)
        {
            return;
        }

        // Si aun no termina el enfriamiento, no permitimos atacar.
        if (Time.time < tiempoProximoAtaque)
        {
            return;
        }

        // Calculamos la direccion de swing desde el buffer de mouse.
        Vector3 direccionSwing = CalcularDireccionSwingDesdeBuffer();

        // Si la direccion es muy chica, ignoramos el ataque para evitar golpes accidentales.
        if (direccionSwing.sqrMagnitude < minimoMagnitudBuffer * minimoMagnitudBuffer)
        {
            return;
        }

        // Guardamos la ultima direccion valida para debug y otros sistemas.
        ultimaDireccionSwing = direccionSwing.normalized;

        // Por defecto configuramos el ataque como normal.
        bool esGolpeFuerte = false;
        float multiplicadorDanioAtaque = 1f;
        float rangoAtaque = rangoAtaqueNormal;
        float delayAtaque = delayAntesDelDanioNormal;
        float enfriamientoAtaque = enfriamientoAtaqueNormal;

        // Si el jugador pidio golpe fuerte, intentamos convertirlo realmente en un golpe fuerte.
        if (solicitarGolpeFuerte)
        {
            // Si no existe estamina, permitimos el golpe fuerte como respaldo para pruebas.
            if (estaminaJugador == null)
            {
                esGolpeFuerte = true;
            }
            else
            {
                // Si alcanza la estamina y no esta agotado, consumimos el costo del golpe fuerte.
                esGolpeFuerte = estaminaJugador.IntentarConsumirGolpeFuerte();
            }

            // Si efectivamente salio golpe fuerte, actualizamos dano, rango y timing.
            if (esGolpeFuerte)
            {
                multiplicadorDanioAtaque = multiplicadorDanioGolpeFuerte;
                rangoAtaque = rangoAtaqueFuerte;
                delayAtaque = delayAntesDelDanioFuerte;
                enfriamientoAtaque = enfriamientoAtaqueFuerte;
            }
        }

        // Guardamos si este ataque puntual fue fuerte o no.
        ultimoAtaqueFueFuerte = esGolpeFuerte;

        // Si la referencia al controlador se perdio por un reload, la recuperamos antes de disparar la animacion.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        // Marcamos que el jugador quedo comprometido con este ataque hasta que la animacion lo resuelva.
        ataqueEnCurso = true;

        // Marcamos el instante mas temprano en que se podra volver a atacar.
        tiempoProximoAtaque = Time.time + enfriamientoAtaque;

        // Avisamos al sistema visual que el jugador acaba de atacar para crosshair y otros efectos.
        EventosJuego.NotificarJugadorLanzoAtaque(gameObject, esGolpeFuerte);

        // Guardamos los datos del ataque para que el evento de animacion los consuma luego.
        PrepararAtaquePendiente(ultimaDireccionSwing, multiplicadorDanioAtaque, esGolpeFuerte, rangoAtaque);

        // Si tenemos controlador de animacion, usamos la ruta nueva con Animation Events.
        if (controladorAnimacionJugador != null)
        {
            // Disparamos la animacion correcta segun el tipo de golpe.
            if (esGolpeFuerte)
            {
                controladorAnimacionJugador.ReproducirAtaqueFuerte();
            }
            else
            {
                controladorAnimacionJugador.ReproducirAtaqueNormal();
            }

            // En Mirror, aqui el cliente local enviaria un [Command] al servidor:
            // [Command] CmdSolicitarAtaque(ultimaDireccionSwing, esGolpeFuerte, Time.time);
            // Dejamos una seguridad por si un clip no trae el Animation Event esperado.
            if (corrutinaSeguridadAtaque != null)
            {
                StopCoroutine(corrutinaSeguridadAtaque);
            }

            corrutinaSeguridadAtaque = StartCoroutine(CorrutinaSeguridadAtaque(esGolpeFuerte ? 1.0f : 0.8f));
        }
        else
        {
            // Si no hay controlador de animacion, mantenemos el flujo clasico de respaldo.
            StartCoroutine(CorrutinaAtaque(ultimaDireccionSwing, multiplicadorDanioAtaque, esGolpeFuerte, rangoAtaque, delayAtaque));
        }
    }

    // Este metodo guarda los datos del ataque pendiente hasta que llegue el evento de animacion.
    private void PrepararAtaquePendiente(Vector3 direccionSwing, float multiplicadorDanioAtaque, bool esGolpeFuerte, float rangoAtaque)
    {
        // Marcamos que hay un ataque esperando resolverse.
        ataquePendienteActivo = true;

        // Guardamos si el golpe pendiente es fuerte.
        ataquePendienteFueFuerte = esGolpeFuerte;

        // Guardamos la direccion para calcular el impacto.
        direccionSwingPendiente = direccionSwing.sqrMagnitude > 0.001f ? direccionSwing.normalized : Vector3.forward;

        // Guardamos el multiplicador de danio del golpe.
        multiplicadorDanioPendiente = multiplicadorDanioAtaque;

        // Guardamos el rango del impacto.
        rangoAtaquePendiente = rangoAtaque;
    }

    // Esta corrutina funciona como seguridad por si la animacion no dispara su evento de dano.
    private IEnumerator CorrutinaSeguridadAtaque(float tiempoMaximoEspera)
    {
        // Esperamos a que la animacion tenga tiempo razonable de ejecutar el evento.
        yield return new WaitForSecondsRealtime(tiempoMaximoEspera);

        // Si el ataque sigue pendiente, resolvemos por respaldo y evitamos que el jugador quede trabado.
        if (ataquePendienteActivo)
        {
            ProcesarGolpePendienteDesdeAnimacion(ataquePendienteFueFuerte);
        }

        // Limpiamos la referencia de seguridad.
        corrutinaSeguridadAtaque = null;
    }

    // Este metodo lo llama la animacion cuando el golpe normal llega al frame correcto.
    public void AplicarDanioNormal()
    {
        // Si el ataque pendiente no era normal, ignoramos este evento.
        if (!ataquePendienteActivo || ataquePendienteFueFuerte)
        {
            return;
        }

        // Procesamos el impacto usando los datos guardados.
        ProcesarGolpePendienteDesdeAnimacion(false);
    }

    // Este metodo lo llama la animacion cuando el golpe fuerte llega al frame correcto.
    public void AplicarDanioFuerte()
    {
        // Si el ataque pendiente no era fuerte, ignoramos este evento.
        if (!ataquePendienteActivo || !ataquePendienteFueFuerte)
        {
            return;
        }

        // Procesamos el impacto usando los datos guardados.
        ProcesarGolpePendienteDesdeAnimacion(true);
    }

    // Este metodo concentra el proceso real de impacto que llega desde Animation Events o desde la seguridad fallback.
    private void ProcesarGolpePendienteDesdeAnimacion(bool esGolpeFuerteEvento)
    {
        // Si no habia ataque pendiente, no hacemos nada.
        if (!ataquePendienteActivo)
        {
            return;
        }

        // Si el tipo de evento no coincide con el tipo pendiente, no resolvemos para no duplicar impactos.
        if (ataquePendienteFueFuerte != esGolpeFuerteEvento)
        {
            return;
        }

        // Buscamos el mejor objetivo disponible en el rango del ataque pendiente.
        ObjetivoAtaque objetivoAtaque;
        bool encontroObjetivo = IntentarBuscarMejorObjetivoEnRango(rangoAtaquePendiente, out objetivoAtaque);

        // Si encontramos un objetivo valido, aplicamos el dano.
        if (encontroObjetivo)
        {
            AplicarGolpeAlObjetivo(objetivoAtaque, direccionSwingPendiente, multiplicadorDanioPendiente, esGolpeFuerteEvento);
        }

        // Limpiamos el estado de ataque para permitir el siguiente swing.
        ataquePendienteActivo = false;
        ataqueEnCurso = false;

        // Si habia una corrutina de seguridad corriendo, la cancelamos porque el evento ya resolvio el impacto.
        if (corrutinaSeguridadAtaque != null)
        {
            StopCoroutine(corrutinaSeguridadAtaque);
            corrutinaSeguridadAtaque = null;
        }
    }

    // Esta funcion calcula direccion de swing usando movimientos recientes del mouse.
    private Vector3 CalcularDireccionSwingDesdeBuffer()
    {
        // Si no hay muestras, devolvemos cero para indicar que no hay swing.
        if (bufferMouse.Count == 0)
        {
            return Vector3.zero;
        }

        // Acumulamos deltas del buffer para obtener la direccion predominante.
        Vector2 acumulado = Vector2.zero;

        // Recorremos cada muestra guardada.
        foreach (MuestraMouse muestra in bufferMouse)
        {
            // Sumamos deltas crudos para reforzar la direccion dominante.
            acumulado += muestra.deltaMouse;
        }

        // Obtenemos la referencia de orientacion del ataque en base a la camara si existe.
        Transform referenciaAtaque = ObtenerReferenciaOrientacionAtaque();

        // Convertimos movimiento de pantalla a direccion del mundo usando esa referencia.
        Vector3 direccionMundo = referenciaAtaque.right * acumulado.x + referenciaAtaque.forward * acumulado.y;

        // Ignoramos la componente vertical para mantener el golpe sobre el plano de juego.
        direccionMundo.y = 0f;

        // Devolvemos la direccion final.
        return direccionMundo;
    }

    // Esta corrutina maneja el timing completo del ataque.
    private IEnumerator CorrutinaAtaque(Vector3 direccionSwing, float multiplicadorDanioAtaque, bool esGolpeFuerte, float rangoAtaque, float delayAtaque)
    {
        // Marcamos que estamos dentro de un ataque.
        ataqueEnCurso = true;

        // Si hay pivote visual y esta opcion activa, hacemos un giro visual simple para el arma.
        if (usarGiroVisual && pivoteVisualEspada != null)
        {
            StartCoroutine(CorrutinaGiroVisual(direccionSwing, esGolpeFuerte));
        }

        // Esperamos el delay del arma antes de que el golpe realmente conecte.
        yield return new WaitForSeconds(delayAtaque);

        // Si por algun motivo el jugador se deshabilito, dejamos de seguir.
        if (!isActiveAndEnabled)
        {
            ataqueEnCurso = false;
            yield break;
        }

        // Buscamos el mejor objetivo disponible en el rango de este ataque.
        ObjetivoAtaque objetivoAtaque;
        bool encontroObjetivo = IntentarBuscarMejorObjetivoEnRango(rangoAtaque, out objetivoAtaque);

        // Si encontramos un objetivo valido, aplicamos el dano.
        if (encontroObjetivo)
        {
            AplicarGolpeAlObjetivo(objetivoAtaque, direccionSwing, multiplicadorDanioAtaque, esGolpeFuerte);
        }

        // Marcamos fin del ataque.
        ataqueEnCurso = false;
    }

    // Este metodo aplica el dano final al objetivo elegido por rango y angulo.
    private void AplicarGolpeAlObjetivo(ObjetivoAtaque objetivoAtaque, Vector3 direccionSwing, float multiplicadorDanioAtaque, bool esGolpeFuerte)
    {
        // Elegimos el danio base segun estadisticas o valor de respaldo.
        float danioBase = estadisticasJugador != null ? estadisticasJugador.DanioActual : danioBaseRespaldo;

        // Multiplicamos el danio si este ataque puntual era fuerte.
        danioBase *= multiplicadorDanioAtaque;

        // Definimos zona y multiplicador por defecto.
        TipoZonaDanio tipoZona = TipoZonaDanio.Cuerpo;
        float multiplicadorZona = 1f;

        // Si el objetivo tenia una zona debil concreta, usamos sus datos.
        if (objetivoAtaque.zonaDebil != null)
        {
            tipoZona = objetivoAtaque.zonaDebil.TipoZona;
            multiplicadorZona = objetivoAtaque.zonaDebil.ObtenerMultiplicador();
        }

        // Armamos datos completos del impacto para vida, UI y feedback.
        DatosDanio datosDanio = new DatosDanio();
        datosDanio.atacante = gameObject;
        datosDanio.objetivo = objetivoAtaque.comportamientoReceptor.gameObject;
        datosDanio.danioBase = danioBase;
        datosDanio.multiplicadorZona = multiplicadorZona;
        datosDanio.tipoZona = tipoZona;
        datosDanio.puntoImpacto = objetivoAtaque.puntoImpacto;
        datosDanio.direccionImpacto = direccionSwing.normalized;
        datosDanio.esGolpeFuerte = esGolpeFuerte;

        // En single player aplicamos dano directo.
        // Con Mirror, esta parte deberia ejecutarse SOLO en servidor.
        // [Command] El cliente pediria el ataque.
        // [ClientRpc] El servidor luego replicaria feedback visual.
        objetivoAtaque.receptorDanio.RecibirDanio(datosDanio);

        // Si el objetivo tiene feedback visual, mostramos el destello correspondiente a la zona golpeada.
        if (objetivoAtaque.feedbackCombate != null)
        {
            objetivoAtaque.feedbackCombate.MostrarDestello(tipoZona);
        }
    }

    // Esta funcion actualiza el objetivo actual en rango para crosshair y color del enemigo.
    private void ActualizarObjetivoEnRango()
    {
        // Si no existe estamina o el jugador no esta agotado, seguimos normalmente.
        if (estaminaJugador != null && !estaminaJugador.PuedeAtacar)
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        // Calculamos cual es el rango mas grande realmente disponible para el jugador ahora mismo.
        float rangoDisponible = ObtenerRangoMayorDisponibleActual();

        // Si no hay rango disponible, apagamos resaltados y salimos.
        if (rangoDisponible <= 0f)
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        // Buscamos si hay al menos un objetivo valido dentro del rango disponible.
        ObjetivoAtaque objetivoAtaque;
        bool encontroObjetivo = IntentarBuscarMejorObjetivoEnRango(rangoDisponible, out objetivoAtaque);

        // Si no encontramos nada, limpiamos el resaltado actual y salimos.
        if (!encontroObjetivo)
        {
            LimpiarObjetivoResaltadoActual();
            return;
        }

        // Guardamos el nuevo objetivo en rango actual.
        objetivoActualEnRango = objetivoAtaque.comportamientoReceptor.gameObject;

        // Si el feedback del objetivo cambio, apagamos el resaltado del anterior.
        if (feedbackObjetivoResaltadoActual != null && feedbackObjetivoResaltadoActual != objetivoAtaque.feedbackCombate)
        {
            feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(false);
        }

        // Guardamos el nuevo feedback actual.
        feedbackObjetivoResaltadoActual = objetivoAtaque.feedbackCombate;

        // Si el enemigo tiene feedback visual, encendemos el indicador de rango.
        if (feedbackObjetivoResaltadoActual != null)
        {
            feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(true);
        }
    }

    // Este metodo busca el mejor objetivo posible segun rango y angulo frontal.
    private bool IntentarBuscarMejorObjetivoEnRango(float rangoAtaque, out ObjetivoAtaque mejorObjetivo)
    {
        // Creamos un objetivo vacio por defecto para tener salida segura.
        mejorObjetivo = default;

        // Calculamos el origen del ataque a la altura del torso del jugador.
        Vector3 origenAtaque = transform.position + Vector3.up * alturaOrigenAtaque;

        // Obtenemos la direccion de mirada real del jugador segun la camara.
        Vector3 direccionMirada = ObtenerDireccionMiradaAtaque();

        // Si la direccion es invalida, no seguimos.
        if (direccionMirada.sqrMagnitude < 0.001f)
        {
            return false;
        }

        // Buscamos todos los colliders en el radio del ataque.
        Collider[] collidersCercanos = Physics.OverlapSphere(origenAtaque, rangoAtaque, mascaraObjetivosAtaque, QueryTriggerInteraction.Collide);

        // Limpiamos la lista reutilizable para copiar los resultados actuales.
        resultadosBusquedaObjetivos.Clear();

        // Copiamos los colliders encontrados a la lista reutilizable.
        resultadosBusquedaObjetivos.AddRange(collidersCercanos);

        // Guardamos el mejor puntaje encontrado hasta ahora.
        float mejorPuntaje = float.MinValue;

        // Recorremos todos los colliders encontrados.
        for (int indiceCollider = 0; indiceCollider < resultadosBusquedaObjetivos.Count; indiceCollider++)
        {
            // Guardamos el collider actual.
            Collider colliderActual = resultadosBusquedaObjetivos[indiceCollider];

            // Si el collider no existe, pasamos al siguiente.
            if (colliderActual == null)
            {
                continue;
            }

            // Si el collider pertenece al propio jugador, lo ignoramos.
            if (colliderActual.transform.root == transform.root)
            {
                continue;
            }

            // Si es un collider generico de un enemigo que ya tiene zonas debiles hijas, lo ignoramos para no tapar las zonas.
            if (DebeIgnorarColliderGenerico(colliderActual))
            {
                continue;
            }

            // Buscamos un receptor de dano en el collider o en su jerarquia.
            IRecibidorDanio receptorDanio = colliderActual.GetComponentInParent<IRecibidorDanio>();

            // Si no encontramos receptor, este collider no sirve como objetivo.
            if (receptorDanio == null)
            {
                continue;
            }

            // Convertimos la interfaz a MonoBehaviour para obtener su GameObject real.
            MonoBehaviour comportamientoReceptor = receptorDanio as MonoBehaviour;

            // Si la conversion fallo, no podemos seguir con este collider.
            if (comportamientoReceptor == null)
            {
                continue;
            }

            // Calculamos el punto de impacto mas cercano sobre este collider.
            Vector3 puntoImpacto = colliderActual.ClosestPoint(origenAtaque);

            // Si por algun motivo el punto dio exactamente el origen, usamos la posicion del objetivo como respaldo.
            if ((puntoImpacto - origenAtaque).sqrMagnitude < 0.0001f)
            {
                puntoImpacto = comportamientoReceptor.transform.position;
            }

            // Calculamos la direccion desde el jugador hasta el punto a impactar.
            Vector3 direccionHastaObjetivo = puntoImpacto - origenAtaque;

            // Ignoramos la componente vertical para comparar solo en el plano del combate.
            direccionHastaObjetivo.y = 0f;

            // Calculamos la distancia real en el plano.
            float distanciaObjetivo = direccionHastaObjetivo.magnitude;

            // Si la distancia es invalida o supera el rango, este objetivo no sirve.
            if (distanciaObjetivo <= 0.001f || distanciaObjetivo > rangoAtaque)
            {
                continue;
            }

            // Normalizamos la direccion para comparar angulo y puntaje.
            Vector3 direccionNormalizada = direccionHastaObjetivo / distanciaObjetivo;

            // Calculamos el angulo entre la mirada del jugador y el objetivo.
            float anguloHastaObjetivo = Vector3.Angle(direccionMirada, direccionNormalizada);

            // Si el objetivo esta fuera del cono frontal permitido, lo ignoramos.
            if (anguloHastaObjetivo > anguloFrontalAtaque * 0.5f)
            {
                continue;
            }

            // Buscamos si este collider representa una zona debil concreta.
            ZonasDebiles zonaDebil = colliderActual.GetComponent<ZonasDebiles>();

            // Si no esta en el collider exacto, intentamos en la jerarquia.
            if (zonaDebil == null)
            {
                zonaDebil = colliderActual.GetComponentInParent<ZonasDebiles>();
            }

            // Buscamos feedback visual en el objetivo golpeable.
            FeedbackCombate feedbackCombate = colliderActual.GetComponentInParent<FeedbackCombate>();

            // Calculamos un puntaje combinando angulo, distancia y una minima preferencia por colliders de zona.
            float puntajeAngulo = Vector3.Dot(direccionMirada, direccionNormalizada) * 10f;
            float puntajeDistancia = -distanciaObjetivo;
            float puntajeZona = zonaDebil != null ? zonaDebil.ObtenerMultiplicador() * 0.02f : 0f;
            float puntajeTotal = puntajeAngulo + puntajeDistancia + puntajeZona;

            // Si este objetivo es mejor que el mejor anterior, lo guardamos.
            if (puntajeTotal > mejorPuntaje)
            {
                mejorPuntaje = puntajeTotal;
                mejorObjetivo.colliderObjetivo = colliderActual;
                mejorObjetivo.receptorDanio = receptorDanio;
                mejorObjetivo.comportamientoReceptor = comportamientoReceptor;
                mejorObjetivo.zonaDebil = zonaDebil;
                mejorObjetivo.feedbackCombate = feedbackCombate;
                mejorObjetivo.puntoImpacto = puntoImpacto;
                mejorObjetivo.distancia = distanciaObjetivo;
            }
        }

        // Devolvemos verdadero solo si realmente guardamos un objetivo valido.
        return mejorObjetivo.comportamientoReceptor != null;
    }

    // Este metodo devuelve si conviene ignorar un collider raiz porque el enemigo ya tiene zonas mas especificas debajo.
    private bool DebeIgnorarColliderGenerico(Collider colliderActual)
    {
        // Si el propio collider ya tiene una zona debil concreta, no lo ignoramos.
        if (colliderActual.GetComponent<ZonasDebiles>() != null)
        {
            return false;
        }

        // Si este mismo objeto tiene hijos con zonas debiles, preferimos esas zonas especificas al collider generico.
        return colliderActual.GetComponentInChildren<ZonasDebiles>() != null;
    }

    // Este metodo devuelve el rango maximo realmente disponible segun la estamina actual.
    private float ObtenerRangoMayorDisponibleActual()
    {
        // Si la estamina no existe, consideramos disponible el mayor rango del sistema.
        if (estaminaJugador == null)
        {
            return Mathf.Max(rangoAtaqueNormal, rangoAtaqueFuerte);
        }

        // Si el jugador esta agotado y no puede atacar, devolvemos cero.
        if (!estaminaJugador.PuedeAtacar)
        {
            return 0f;
        }

        // Si el jugador puede usar golpe fuerte, devolvemos el mayor rango.
        if (estaminaJugador.PuedeUsarGolpeFuerte)
        {
            return Mathf.Max(rangoAtaqueNormal, rangoAtaqueFuerte);
        }

        // Si no puede fuerte, devolvemos solo el rango normal.
        return rangoAtaqueNormal;
    }

    // Este metodo limpia el resaltado del objetivo actual cuando deja de estar disponible.
    private void LimpiarObjetivoResaltadoActual()
    {
        // Si habia un feedback resaltado, lo apagamos.
        if (feedbackObjetivoResaltadoActual != null)
        {
            feedbackObjetivoResaltadoActual.EstablecerIndicadorRango(false);
        }

        // Limpiamos referencias del objetivo actual.
        feedbackObjetivoResaltadoActual = null;
        objetivoActualEnRango = null;
    }

    // Este metodo devuelve el transform cuya orientacion usamos para definir hacia donde esta mirando el jugador.
    private Transform ObtenerReferenciaOrientacionAtaque()
    {
        // Si no tenemos camara guardada, la reintentamos obtener.
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        // Si no existe camara, usamos el propio transform del jugador como respaldo.
        if (camaraPrincipal == null)
        {
            return transform;
        }

        // Si la camara esta dentro de un rig, usamos el padre para una orientacion mas estable.
        if (camaraPrincipal.transform.parent != null)
        {
            return camaraPrincipal.transform.parent;
        }

        // Si no hay padre, usamos la propia camara.
        return camaraPrincipal.transform;
    }

    // Este metodo devuelve la direccion horizontal hacia la que esta mirando el jugador para atacar.
    private Vector3 ObtenerDireccionMiradaAtaque()
    {
        // Tomamos el transform de referencia elegido para orientar el ataque.
        Transform referenciaAtaque = ObtenerReferenciaOrientacionAtaque();

        // Leemos su vector forward.
        Vector3 direccionMirada = referenciaAtaque.forward;

        // Quitamos la componente vertical para comparar solo en el plano.
        direccionMirada.y = 0f;

        // Si la direccion quedo invalida, usamos el forward del jugador como respaldo.
        if (direccionMirada.sqrMagnitude < 0.001f)
        {
            direccionMirada = transform.forward;
            direccionMirada.y = 0f;
        }

        // Devolvemos la direccion normalizada.
        return direccionMirada.normalized;
    }

    // Esta corrutina hace una animacion visual simple de giro de espada.
    private IEnumerator CorrutinaGiroVisual(Vector3 direccionSwing, bool esGolpeFuerte)
    {
        // Calculamos el signo segun la direccion lateral del swing.
        float signo = Vector3.Dot(transform.right, direccionSwing) >= 0f ? 1f : -1f;

        // Guardamos la rotacion original para restaurarla al final.
        Quaternion rotacionOriginal = pivoteVisualEspada.localRotation;

        // Elegimos el angulo maximo segun si el ataque es normal o fuerte.
        float anguloMaximo = esGolpeFuerte ? anguloMaximoGiroVisualFuerte : anguloMaximoGiroVisualNormal;

        // Calculamos la rotacion objetivo de ida.
        Quaternion rotacionIda = rotacionOriginal * Quaternion.Euler(0f, 0f, -anguloMaximo * signo);

        // Definimos duracion de ida segun si el golpe es fuerte o rapido.
        float duracionIda = esGolpeFuerte ? 0.12f : 0.06f;

        // Iniciamos cronometro de ida.
        float tiempoIda = 0f;

        // Animamos ida.
        while (tiempoIda < duracionIda)
        {
            tiempoIda += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoIda / duracionIda);
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionOriginal, rotacionIda, progreso);
            yield return null;
        }

        // Definimos duracion de vuelta segun el tipo de ataque.
        float duracionVuelta = esGolpeFuerte ? 0.18f : 0.1f;

        // Iniciamos cronometro de vuelta.
        float tiempoVuelta = 0f;

        // Animamos vuelta a la posicion original.
        while (tiempoVuelta < duracionVuelta)
        {
            tiempoVuelta += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoVuelta / duracionVuelta);
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionIda, rotacionOriginal, progreso);
            yield return null;
        }

        // Aseguramos rotacion final exacta original.
        pivoteVisualEspada.localRotation = rotacionOriginal;
    }

    // Este metodo dibuja gizmos utiles para depurar rango y cono de ataque en el editor.
    private void OnDrawGizmosSelected()
    {
        // Calculamos un origen visual para los gizmos del ataque.
        Vector3 origenAtaque = transform.position + Vector3.up * alturaOrigenAtaque;

        // Dibujamos el rango normal en amarillo.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origenAtaque, rangoAtaqueNormal);

        // Dibujamos el rango fuerte en naranja.
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(origenAtaque, rangoAtaqueFuerte);
    }
}
