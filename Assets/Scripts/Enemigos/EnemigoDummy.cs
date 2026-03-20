using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla la IA basica del enemigo, incluyendo patrulla,
// memoria de vision, persecucion, ataque y secuencia de muerte.

// Esta clase controla la IA basica del enemigo con una maquina de estados simple.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(VidaEnemigo))]
public class EnemigoDummy : MonoBehaviour
{
    // Este valor guarda el rango de vision recomendado para esta IA.
    private const float RangoVisionRecomendado = 15f;

    // Este valor guarda el angulo de vision recomendado para esta IA.
    private const float AnguloVisionRecomendado = 120f;

    // Este valor guarda el intervalo recomendado de chequeo de vision.
    private const float IntervaloVisionRecomendado = 0.1f;

    // Este valor define cada cuanto se reintenta anclar el agente al NavMesh si se desengancha.
    private const float IntervaloReintentoNavMesh = 0.5f;

    // Esta variable define en que estado arranca el enemigo.
    [Header("Estado Actual")]
    [SerializeField] private EstadosEnemigo estadoActual = EstadosEnemigo.Patrullar;

    // Estos son los puntos fijos para patrullar.
    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;

    // Este valor define cuanto espera en cada punto antes de seguir.
    [SerializeField] private float tiempoEsperaEnPunto = 1.2f;

    // Esta velocidad se usa mientras patrulla.
    [SerializeField] private float velocidadPatrulla = 2f;

    // Esta opcion intenta poblar automaticamente la patrulla con puntos hijos si la lista esta vacia.
    [SerializeField] private bool autoBuscarPuntosPatrullaEnHijos = true;

    // Esta velocidad se usa mientras persigue.
    [Header("Persecucion")]
    [SerializeField] private float velocidadPersecucion = 3.5f;

    // Esta distancia define si puede ver al jugador.
    [SerializeField] private float rangoVision = 15f;

    // Este angulo define el cono de vision frontal.
    [SerializeField] private float anguloVision = 120f;

    // Este valor define cada cuanto se recalcula la vision real del jugador.
    [SerializeField] private float intervaloChequeoVision = 0.1f;

    // Este tiempo define cuanto recuerda la ultima posicion vista del jugador.
    [SerializeField] private float tiempoMemoriaVision = 1.5f;

    // Esta mascara sirve para comprobar obstaculos entre enemigo y jugador.
    [SerializeField] private LayerMask mascaraObstaculosVision = ~0;

    // Esta altura ayuda a lanzar el raycast desde un punto cercano a los ojos.
    [SerializeField] private float alturaOjos = 1.5f;

    // Esta distancia define cuando empieza a atacar.
    [Header("Ataque")]
    [SerializeField] private float distanciaAtaque = 2.1f;

    // Este dano base se aplica al jugador cuando conecta el ataque.
    [SerializeField] private float danioAtaque = 15f;

    // Este tiempo representa el cooldown entre ataques consecutivos.
    [SerializeField] private float tiempoEntreAtaques = 1.5f;

    // Este retraso se usa solo como respaldo si el Animation Event no dispara el dano.
    [SerializeField] private float delayImpactoAtaque = 0.35f;

    // Este tiempo define cuanto dura el telegraph visual antes de lanzar el golpe real.
    [SerializeField] private float duracionTelegraphAtaque = 0.24f;

    // Este tiempo define cuanto tarda en caer antes de desaparecer.
    [Header("Muerte")]
    [SerializeField] private float duracionCaida = 0.35f;

    // Este tiempo define cuando se destruye el enemigo muerto.
    [SerializeField] private float tiempoDesaparecer = 3f;

    // Esta bandera evita iniciar dos secuencias de muerte al mismo tiempo.
    private bool muerteEnCurso;

    // Esta bandera marca si la animacion de muerte ya termino por evento o respaldo.
    private bool muerteAnimacionTerminada;

    // Este tiempo guarda hasta cuando el enemigo queda aturdido por un parry.
    private float tiempoFinAturdimientoParry;

    // Este tiempo guarda hasta cuando sigue activa la vulnerabilidad extra del parry.
    private float tiempoFinVulnerabilidadParry;

    // Este valor guarda el multiplicador adicional del proximo golpe tras un parry exitoso.
    private float multiplicadorVulnerabilidadParry = 1f;

    // Referencia al componente de navegacion.
    private NavMeshAgent agenteNavMesh;

    // Referencia al componente de vida.
    private VidaEnemigo vidaEnemigo;

    // Referencia al objetivo jugador actual.
    private Transform objetivoJugador;

    // Referencia al componente de vida del jugador.
    private VidaJugador vidaJugadorObjetivo;

    // Referencia opcional a los componentes PuntoPatrulla.
    private PuntoPatrulla[] puntosPatrullaComponentes;

    // Indice del punto de patrulla actual.
    private int indicePuntoPatrulla;

    // Temporizador interno para espera en punto de patrulla.
    private float temporizadorEsperaPatrulla;

    // Tiempo en que podra atacar de nuevo.
    private float tiempoProximoAtaque;

    // Controla que no se lancen dos corutinas de ataque juntas.
    private bool ataqueEnCurso;

    // Guarda referencia de la rutina de muerte para evitar duplicados.
    private Coroutine rutinaMuerteActiva;

    // Guarda referencia de la rutina de respaldo del ataque.
    private Coroutine rutinaAtaqueRespaldoActiva;

    // Guarda la referencia al controlador visual del modelo esqueleto.
    [SerializeField] private ControladorAnimacionEnemigo controladorAnimacionEnemigo;

    // Esta referencia apunta al feedback visual para telegraphs y estados especiales.
    [SerializeField] private FeedbackCombate feedbackCombate;

    // Esta bandera marca si el dano del ataque ya se aplico por evento.
    private bool danioAtaqueAplicado;

    // Esta variable guarda la ultima posicion donde vio al jugador.
    private Vector3 ultimaPosicionVistaJugador;

    // Esta variable guarda el instante en el que vio por ultima vez al jugador.
    private float tiempoUltimaVisionJugador = -999f;

    // Esta variable marca si el jugador esta siendo visto en este frame.
    private bool jugadorVisibleEnEsteFrame;

    // Esta variable guarda el proximo instante en el que se recalculara la vision.
    private float tiempoProximoChequeoVision;

    // Esta variable guarda el estado inicial elegido en Inspector para restaurarlo tras respawn.
    private EstadosEnemigo estadoInicialConfigurado;

    // Esta variable marca si la IA termino su inicializacion diferida.
    private bool iaInicializada;

    // Esta referencia guarda la corrutina de inicializacion diferida.
    private Coroutine rutinaInicializacionIA;

    // Esta variable guarda cuando volver a intentar engancharse al NavMesh.
    private float tiempoProximoReintentoNavMesh;

    // Esta propiedad expone el estado actual para el controlador de animacion.
    public EstadosEnemigo EstadoActual => estadoActual;

    // Esta propiedad deja consultar si el enemigo esta temporalmente aturdido por un parry.
    public bool EstaAturdidoPorParry => Time.time < tiempoFinAturdimientoParry;

    // Esta funcion se ejecuta al iniciar el objeto.
    private void Awake()
    {
        // Obtenemos referencia al NavMeshAgent del mismo objeto.
        agenteNavMesh = GetComponent<NavMeshAgent>();

        // Obtenemos referencia al sistema de vida del mismo objeto.
        vidaEnemigo = GetComponent<VidaEnemigo>();

        // Guardamos el estado inicial para poder restaurarlo en reinicios.
        estadoInicialConfigurado = estadoActual;

        // Validamos parametros de vision para que arranquen en valores robustos.
        ValidarParametrosVision();

        // Buscamos el controlador visual una vez al iniciar.
        BuscarControladorAnimacion();
    }

    // Esta funcion se ejecuta cuando el objeto se habilita.
    private void OnEnable()
    {
        // Escuchamos el evento de muerte para iniciar la secuencia final.
        vidaEnemigo.AlMorir += ManejarMuerteEnemigo;

        // Reanudamos la busqueda del controlador visual por seguridad.
        BuscarControladorAnimacion();

        // Reintentamos inicializacion diferida para soportar respawn o reload.
        IniciarInicializacionDiferida();
    }

    // Esta funcion se ejecuta cuando el objeto se deshabilita.
    private void OnDisable()
    {
        // Dejamos de escuchar el evento para evitar referencias colgadas.
        if (vidaEnemigo != null)
        {
            vidaEnemigo.AlMorir -= ManejarMuerteEnemigo;
        }

        DesvincularEventoMuerteAnimacion();

        // Si hay una inicializacion en curso, la detenemos para no dejar corrutinas colgadas.
        if (rutinaInicializacionIA != null)
        {
            StopCoroutine(rutinaInicializacionIA);
            rutinaInicializacionIA = null;
        }

        // Si habia un ataque pendiente, lo detenemos.
        if (rutinaAtaqueRespaldoActiva != null)
        {
            StopCoroutine(rutinaAtaqueRespaldoActiva);
            rutinaAtaqueRespaldoActiva = null;
        }

        // Si habia una muerte en curso, la detenemos para no dejar corrutinas colgadas.
        if (rutinaMuerteActiva != null)
        {
            StopCoroutine(rutinaMuerteActiva);
            rutinaMuerteActiva = null;
        }

        // Marcamos la IA como no inicializada para el proximo enable.
        iaInicializada = false;
    }

    // Esta funcion se ejecuta una sola vez al empezar la escena.
    private void Start()
    {
        // Pedimos inicializacion diferida para soportar carga post-restart.
        IniciarInicializacionDiferida();

        // Volvemos a buscar el controlador por si Unity rearmo la escena.
        BuscarControladorAnimacion();
    }

    // Esta funcion se ejecuta cada frame para actualizar IA.
    private void Update()
    {
        // Si aun no termino la inicializacion diferida, no ejecutamos logica de IA.
        if (!iaInicializada)
        {
            return;
        }

        // Si el enemigo ya murio, detenemos toda logica de IA.
        if (!vidaEnemigo.EstaVivo)
        {
            // Salimos para no ejecutar comportamiento post mortem.
            return;
        }

        // Si esta aturdido por un parry, frenamos movimiento y esperamos a que termine.
        if (EstaAturdidoPorParry)
        {
            if (agenteNavMesh != null && agenteNavMesh.enabled)
            {
                agenteNavMesh.isStopped = true;
            }

            if (feedbackCombate != null)
            {
                feedbackCombate.EstablecerTelegraphAtaque(false);
            }

            return;
        }

        if (feedbackCombate != null && Time.time > tiempoFinVulnerabilidadParry)
        {
            feedbackCombate.EstablecerVulnerableParry(false);
        }

        // Si el agente se desengancho del NavMesh tras un reload, intentamos recuperarlo.
        if (agenteNavMesh != null && agenteNavMesh.enabled && !agenteNavMesh.isOnNavMesh)
        {
            // Solo reintentamos en una frecuencia fija para no gastar de mas.
            if (Time.time >= tiempoProximoReintentoNavMesh)
            {
                tiempoProximoReintentoNavMesh = Time.time + IntervaloReintentoNavMesh;
                AsegurarAgenteSobreNavMesh(6f);
            }

            // Si sigue fuera del NavMesh, esperamos al siguiente frame.
            if (!agenteNavMesh.isOnNavMesh)
            {
                return;
            }
        }

        // Si no tenemos jugador valido, intentamos encontrar uno.
        if (objetivoJugador == null || vidaJugadorObjetivo == null || !vidaJugadorObjetivo.EstaVivo)
        {
            // Reintentamos adquirir objetivo.
            BuscarObjetivoJugador();
        }

        // Actualizamos la vision en una frecuencia fija para reaccion consistente.
        ActualizarVisionSiHaceFalta();

        // Elegimos el estado correcto segun distancias y visibilidad.
        ActualizarEstado();

        // Ejecutamos la logica especifica del estado elegido.
        EjecutarEstadoActual();
    }

    // Este metodo busca el controlador de animacion del modelo esqueleto.
    private void BuscarControladorAnimacion()
    {
        // Si ya tenemos referencia, no repetimos la busqueda.
        if (controladorAnimacionEnemigo != null)
        {
            if (feedbackCombate == null)
            {
                feedbackCombate = controladorAnimacionEnemigo.GetComponentInParent<FeedbackCombate>();
            }

            return;
        }

        // Buscamos el controlador en hijos, incluyendo objetos inactivos.
        controladorAnimacionEnemigo = GetComponentInChildren<ControladorAnimacionEnemigo>(true);

        if (feedbackCombate == null)
        {
            feedbackCombate = GetComponentInChildren<FeedbackCombate>(true);
        }
    }

    // Este metodo intenta localizar un jugador con VidaJugador en la escena.
    private void BuscarObjetivoJugador()
    {
        // Limpiamos referencias previas para evitar usar objetivos invalidados.
        objetivoJugador = null;
        vidaJugadorObjetivo = null;

        // Esta referencia intenta ubicar un objeto con tag Player.
        GameObject jugadorConTag = null;

        // En algunas escenas el tag puede no existir todavia, por eso usamos try/catch.
        try
        {
            jugadorConTag = GameObject.FindWithTag("Player");
        }
        catch (UnityException)
        {
            jugadorConTag = null;
        }

        // Si encontramos objeto por tag, intentamos obtener VidaJugador desde ahi.
        if (jugadorConTag != null)
        {
            // Buscamos VidaJugador en el mismo objeto o en hijos.
            VidaJugador vidaPorTag = jugadorConTag.GetComponent<VidaJugador>();

            // Si no esta en raiz, probamos en hijos por seguridad.
            if (vidaPorTag == null)
            {
                vidaPorTag = jugadorConTag.GetComponentInChildren<VidaJugador>();
            }

            // Si encontramos uno vivo, lo tomamos como objetivo inmediato.
            if (vidaPorTag != null && vidaPorTag.EstaVivo)
            {
                vidaJugadorObjetivo = vidaPorTag;
                objetivoJugador = vidaPorTag.transform;
                ultimaPosicionVistaJugador = objetivoJugador.position;
                return;
            }
        }

        // Si no hubo target por tag, buscamos cualquier VidaJugador viva en escena.
        VidaJugador[] candidatos = FindObjectsOfType<VidaJugador>();

        // Guardamos la mejor opcion encontrada segun cercania.
        VidaJugador mejorCandidato = null;

        // Guardamos distancia minima en formato cuadrado para optimizar.
        float mejorDistanciaCuadrada = float.MaxValue;

        // Recorremos candidatos para elegir el vivo mas cercano.
        for (int indiceCandidato = 0; indiceCandidato < candidatos.Length; indiceCandidato++)
        {
            // Guardamos referencia local del candidato.
            VidaJugador candidatoActual = candidatos[indiceCandidato];

            // Ignoramos referencias nulas o jugadores muertos.
            if (candidatoActual == null || !candidatoActual.EstaVivo)
            {
                continue;
            }

            // Calculamos distancia al cuadrado desde este enemigo al candidato.
            float distanciaCuadradaActual = (candidatoActual.transform.position - transform.position).sqrMagnitude;

            // Si es mejor que la guardada, actualizamos candidato preferido.
            if (distanciaCuadradaActual < mejorDistanciaCuadrada)
            {
                mejorDistanciaCuadrada = distanciaCuadradaActual;
                mejorCandidato = candidatoActual;
            }
        }

        // Si encontramos candidato valido, lo guardamos como objetivo.
        if (mejorCandidato != null)
        {
            vidaJugadorObjetivo = mejorCandidato;
            objetivoJugador = mejorCandidato.transform;
            ultimaPosicionVistaJugador = objetivoJugador.position;
        }
    }

    // Este metodo decide el estado que corresponde en este frame.
    private void ActualizarEstado()
    {
        // Si no hay jugador objetivo, volvemos a patrullar.
        if (objetivoJugador == null)
        {
            // Cambiamos al estado de patrulla.
            estadoActual = EstadosEnemigo.Patrullar;

            // Terminamos esta decision.
            return;
        }

        // Calculamos la distancia actual hacia el jugador.
        float distanciaJugador = Vector3.Distance(transform.position, objetivoJugador.position);

        // Si esta lo bastante cerca para atacar, priorizamos ataque.
        if (distanciaJugador <= distanciaAtaque)
        {
            // Si efectivamente lo vemos, actualizamos memoria de vision.
            if (jugadorVisibleEnEsteFrame)
            {
                tiempoUltimaVisionJugador = Time.time;
                ultimaPosicionVistaJugador = objetivoJugador.position;
            }

            // Cambiamos al estado de ataque.
            estadoActual = EstadosEnemigo.Atacar;

            // Terminamos esta decision.
            return;
        }

        // Si esta en rango de vision y realmente esta dentro del cono y sin obstaculos, perseguimos.
        if (distanciaJugador <= rangoVision && jugadorVisibleEnEsteFrame)
        {
            // Marcamos que si lo vemos en este frame.
            jugadorVisibleEnEsteFrame = true;

            // Guardamos su ultima posicion visible.
            ultimaPosicionVistaJugador = objetivoJugador.position;

            // Guardamos el instante de vision.
            tiempoUltimaVisionJugador = Time.time;

            // Cambiamos al estado de persecucion.
            estadoActual = EstadosEnemigo.Perseguir;

            // Terminamos esta decision.
            return;
        }

        // Si hace poco lo vimos, seguimos persiguiendo su ultima posicion conocida.
        if (Time.time <= tiempoUltimaVisionJugador + tiempoMemoriaVision)
        {
            // Mantenemos el estado de persecucion gracias a la memoria.
            estadoActual = EstadosEnemigo.Perseguir;
            return;
        }

        // Si no cumple condiciones de vision o ataque, seguimos patrullando.
        estadoActual = EstadosEnemigo.Patrullar;
    }

    // Este metodo recalcula la vision del enemigo a intervalos fijos.
    private void ActualizarVisionSiHaceFalta()
    {
        // Si no hay objetivo, marcamos vision falsa y salimos.
        if (objetivoJugador == null)
        {
            jugadorVisibleEnEsteFrame = false;
            return;
        }

        // Si todavia no toca recalcular, usamos el valor cacheado actual.
        if (Time.time < tiempoProximoChequeoVision)
        {
            return;
        }

        // Guardamos cuando sera el proximo chequeo de vision.
        tiempoProximoChequeoVision = Time.time + intervaloChequeoVision;

        // Recalculamos si el jugador esta visible ahora mismo.
        jugadorVisibleEnEsteFrame = TieneLineaDeVisionConJugador();
    }

    // Este metodo corre la logica segun el estado actual.
    private void EjecutarEstadoActual()
    {
        // Elegimos comportamiento segun enum de estados.
        switch (estadoActual)
        {
            // Comportamiento para moverse entre puntos.
            case EstadosEnemigo.Patrullar:
                EjecutarPatrulla();
                break;

            // Comportamiento para seguir al jugador.
            case EstadosEnemigo.Perseguir:
                EjecutarPersecucion();
                break;

            // Comportamiento para atacar cuando esta cerca.
            case EstadosEnemigo.Atacar:
                EjecutarAtaque();
                break;

            // Estado muerto no hace nada.
            case EstadosEnemigo.Muerto:
                break;
        }
    }

    // Este metodo implementa el comportamiento de patrulla.
    private void EjecutarPatrulla()
    {
        // Ajustamos velocidad de patrulla.
        agenteNavMesh.speed = velocidadPatrulla;

        // Permitimos que el agente se mueva.
        agenteNavMesh.isStopped = false;

        // Si no hay puntos preparados, intentamos volver a armarlos.
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            PrepararPuntosPatrulla();
        }

        // Si sigue sin haber puntos, no hacemos nada.
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            return;
        }

        // Si todavia esta calculando ruta, esperamos al siguiente frame.
        if (agenteNavMesh.pathPending)
        {
            return;
        }

        // Si llego al punto actual, empezamos a contar espera.
        if (agenteNavMesh.remainingDistance <= agenteNavMesh.stoppingDistance + 0.05f)
        {
            // Sumamos tiempo esperando en este punto.
            temporizadorEsperaPatrulla += Time.deltaTime;

            // Calculamos cuanto deberia esperar en este punto puntual.
            float tiempoEsperaReal = ObtenerTiempoEsperaPuntoActual();

            // Si ya espero suficiente, pasamos al siguiente punto.
            if (temporizadorEsperaPatrulla >= tiempoEsperaReal)
            {
                // Reiniciamos el temporizador de espera.
                temporizadorEsperaPatrulla = 0f;

                // Avanzamos al siguiente indice en bucle.
                indicePuntoPatrulla = (indicePuntoPatrulla + 1) % puntosPatrulla.Length;

                // Ordenamos ir al siguiente punto de patrulla.
                IntentarDefinirDestino(puntosPatrulla[indicePuntoPatrulla].position);
            }
        }
        else
        {
            // Si aun no llego al punto, reseteamos la espera para evitar acumulacion rara.
            temporizadorEsperaPatrulla = 0f;
        }
    }

    // Este metodo implementa el comportamiento de persecucion.
    private void EjecutarPersecucion()
    {
        // Si no hay objetivo, no hay a quien perseguir.
        if (objetivoJugador == null)
        {
            return;
        }

        // Ajustamos velocidad de persecucion.
        agenteNavMesh.speed = velocidadPersecucion;

        // Permitimos movimiento del agente.
        agenteNavMesh.isStopped = false;

        // Si lo vemos ahora, perseguimos su posicion real.
        if (jugadorVisibleEnEsteFrame)
        {
            IntentarDefinirDestino(objetivoJugador.position);
            return;
        }

        // Si no lo vemos, perseguimos la ultima posicion donde si lo vimos.
        IntentarDefinirDestino(ultimaPosicionVistaJugador);

        // Si ya llegamos a la ultima posicion conocida y paso la memoria, volvemos a patrullar.
        if (!agenteNavMesh.pathPending && agenteNavMesh.remainingDistance <= agenteNavMesh.stoppingDistance + 0.2f && Time.time > tiempoUltimaVisionJugador + tiempoMemoriaVision)
        {
            estadoActual = EstadosEnemigo.Patrullar;
        }
    }

    // Este metodo implementa el comportamiento de ataque.
    private void EjecutarAtaque()
    {
        // Si no hay objetivo, salimos.
        if (objetivoJugador == null)
        {
            return;
        }

        // Frenamos el agente para atacar en sitio.
        agenteNavMesh.isStopped = true;

        // Rotamos suavemente hacia el jugador para que el golpe tenga direccion.
        RotarHaciaObjetivo(objetivoJugador.position);

        // Si ya hay un ataque corriendo, no iniciamos otro.
        if (ataqueEnCurso)
        {
            return;
        }

        // Si aun no paso el cooldown, no atacamos todavia.
        if (Time.time < tiempoProximoAtaque)
        {
            return;
        }

        // Iniciamos la rutina de ataque y dejamos que el Animation Event marque el impacto.
        rutinaAtaqueRespaldoActiva = StartCoroutine(RutinaAtacar());
    }

    // Esta corutina deja un respaldo por si el Animation Event del swing no aparece.
    private IEnumerator RutinaAtacar()
    {
        // Marcamos que hay un ataque en progreso.
        ataqueEnCurso = true;
        danioAtaqueAplicado = false;

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerTelegraphAtaque(true);
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, duracionTelegraphAtaque));

        // Reproducimos la animacion de ataque desde el controlador visual.
        BuscarControladorAnimacion();
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.ReproducirAtacar();
        }

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerTelegraphAtaque(false);
        }

        // En Mirror, desde aqui deberia lanzarse un [Command] al servidor.
        // [Mirror futuro] Validar distancia y cooldown en servidor antes del dano real.

        // Esperamos un tiempo corto solo como respaldo.
        yield return new WaitForSeconds(Mathf.Max(0.05f, delayImpactoAtaque));

        // Si el enemigo murio durante la espera, cancelamos.
        if (!vidaEnemigo.EstaVivo)
        {
            FinalizarAtaque();
            yield break;
        }

        // Si el objetivo desaparecio durante la espera, cancelamos.
        if (objetivoJugador == null)
        {
            FinalizarAtaque();
            yield break;
        }

        // Si el evento no aplico el dano, lo resolvemos ahora.
        if (!danioAtaqueAplicado)
        {
            AplicarDanioAlJugador();
        }

        // Cerramos el ciclo de ataque.
        FinalizarAtaque();
    }

    // Este metodo lo llama el Animation Event del swing para aplicar el dano exacto.
    public void AplicarDanioAtaqueDesdeAnimacion()
    {
        // Si el ataque no esta activo o ya se consumo, no hacemos nada.
        if (!ataqueEnCurso || danioAtaqueAplicado || !vidaEnemigo.EstaVivo)
        {
            return;
        }

        // Volvemos a validar distancia para evitar golpes fantasmas.
        if (objetivoJugador != null)
        {
            float distanciaActual = Vector3.Distance(transform.position, objetivoJugador.position);
            if (distanciaActual <= distanciaAtaque + 0.25f)
            {
                AplicarDanioAlJugador();
            }
        }

        // Marcamos el dano como aplicado y cerramos el ataque.
        danioAtaqueAplicado = true;
        FinalizarAtaque();
    }

    // Este metodo centraliza la limpieza del ataque y su cooldown.
    private void FinalizarAtaque()
    {
        // Guardamos cuando podra atacar nuevamente.
        tiempoProximoAtaque = Time.time + tiempoEntreAtaques;

        // Apagamos la bandera de ataque activo.
        ataqueEnCurso = false;

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerTelegraphAtaque(false);
        }

        // Si habia una corrutina de respaldo, la detenemos.
        if (rutinaAtaqueRespaldoActiva != null)
        {
            StopCoroutine(rutinaAtaqueRespaldoActiva);
            rutinaAtaqueRespaldoActiva = null;
        }
    }

    // Este metodo aplica dano al objetivo jugador usando directamente VidaJugador.
    private void AplicarDanioAlJugador()
    {
        // Si no tenemos referencia valida a la vida del jugador, cortamos.
        if (vidaJugadorObjetivo == null || !vidaJugadorObjetivo.EstaVivo)
        {
            return;
        }

        // Armamos datos completos de dano para mantener compatibilidad con feedback global.
        DatosDanio datosDanio = new DatosDanio
        {
            atacante = gameObject,
            objetivo = vidaJugadorObjetivo.gameObject,
            danioBase = danioAtaque,
            multiplicadorZona = 1f,
            tipoZona = TipoZonaDanio.Cuerpo,
            puntoImpacto = vidaJugadorObjetivo.transform.position + Vector3.up,
            direccionImpacto = (vidaJugadorObjetivo.transform.position - transform.position).normalized,
            esGolpeFuerte = false
        };

        // Enviamos el golpe al sistema de vida del jugador.
        vidaJugadorObjetivo.RecibirDanio(datosDanio);
    }

    // Este metodo permite escalar el dano del enemigo segun la oleada sin romper su logica interna.
    public void AplicarEscaladoOleada(float multiplicadorDanio)
    {
        // Aseguramos un multiplicador valido para no generar valores raros.
        float multiplicadorSeguro = Mathf.Max(0.01f, multiplicadorDanio);

        // Escalamos el dano base del ataque en el lugar donde la IA ya lo usa.
        danioAtaque = Mathf.Max(1f, danioAtaque * multiplicadorSeguro);
    }

    // Este metodo aturde al enemigo despues de recibir un parry exitoso del jugador.
    public void AplicarAturdimientoParry(float duracion)
    {
        // Si el enemigo ya murio, no tiene sentido aturdirlo.
        if (!vidaEnemigo.EstaVivo)
        {
            return;
        }

        // Extendemos la ventana de aturdimiento al tiempo mas largo recibido.
        tiempoFinAturdimientoParry = Mathf.Max(tiempoFinAturdimientoParry, Time.time + Mathf.Max(0.05f, duracion));

        // Cancelamos cualquier ataque actual y frenamos el agente.
        FinalizarAtaque();
        danioAtaqueAplicado = false;
        if (agenteNavMesh != null && agenteNavMesh.enabled)
        {
            agenteNavMesh.isStopped = true;
        }

        // Cambiamos el estado para que la IA no intente continuar una transicion vieja.
        estadoActual = EstadosEnemigo.Perseguir;

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerTelegraphAtaque(false);
        }

        // Si hay controlador visual, usamos la reaccion de golpe como telegraph del parry recibido.
        BuscarControladorAnimacion();
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.ReproducirRecibirDanio(new DatosDanio
            {
                atacante = null,
                objetivo = gameObject,
                danioBase = 0f,
                multiplicadorZona = 1f,
                tipoZona = TipoZonaDanio.Cuerpo,
                puntoImpacto = transform.position + Vector3.up,
                direccionImpacto = -transform.forward,
                esGolpeFuerte = false
            });
        }
    }

    // Este metodo marca al enemigo como vulnerable para el siguiente contraataque del jugador.
    public void MarcarVulnerablePorParry(float duracion, float multiplicador)
    {
        // Guardamos una ventana de vulnerabilidad que dura un tiempo corto despues del parry.
        tiempoFinVulnerabilidadParry = Mathf.Max(tiempoFinVulnerabilidadParry, Time.time + Mathf.Max(0.05f, duracion));
        multiplicadorVulnerabilidadParry = Mathf.Max(1f, multiplicador);

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerVulnerableParry(true);
        }
    }

    // Este metodo devuelve y consume el multiplicador adicional del proximo golpe tras un parry.
    public float ConsumirMultiplicadorVulnerabilidadParry()
    {
        // Si ya expiro la ventana, devolvemos multiplicador normal.
        if (Time.time > tiempoFinVulnerabilidadParry)
        {
            multiplicadorVulnerabilidadParry = 1f;
            if (feedbackCombate != null)
            {
                feedbackCombate.EstablecerVulnerableParry(false);
            }
            return 1f;
        }

        // Consumimos el bonus una sola vez para que el contraataque sea claro y no permanente.
        float multiplicador = Mathf.Max(1f, multiplicadorVulnerabilidadParry);
        tiempoFinVulnerabilidadParry = -1f;
        multiplicadorVulnerabilidadParry = 1f;
        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerVulnerableParry(false);
        }
        return multiplicador;
    }

    // Este metodo responde al evento de muerte de VidaEnemigo.
    private void ManejarMuerteEnemigo(DatosDanio datosDanio)
    {
        if (muerteEnCurso)
        {
            return;
        }

        // Cambiamos estado interno a muerto.
        estadoActual = EstadosEnemigo.Muerto;
        muerteEnCurso = true;
        muerteAnimacionTerminada = false;

        if (feedbackCombate != null)
        {
            feedbackCombate.EstablecerTelegraphAtaque(false);
            feedbackCombate.EstablecerVulnerableParry(false);
        }

        // Cortamos cualquier ataque que estuviera a medias.
        FinalizarAtaque();
        danioAtaqueAplicado = false;

        // Frenamos cualquier intento de navegacion.
        if (agenteNavMesh != null && agenteNavMesh.enabled)
        {
            agenteNavMesh.isStopped = true;
            agenteNavMesh.ResetPath();
            agenteNavMesh.enabled = false;
        }

        Rigidbody cuerpoRigido = GetComponent<Rigidbody>();
        if (cuerpoRigido != null)
        {
            cuerpoRigido.velocity = Vector3.zero;
            cuerpoRigido.angularVelocity = Vector3.zero;
            cuerpoRigido.isKinematic = true;
        }

        DesactivarCollidersGameplay();

        BuscarControladorAnimacion();
        VincularEventoMuerteAnimacion();

        if (rutinaMuerteActiva != null)
        {
            StopCoroutine(rutinaMuerteActiva);
            rutinaMuerteActiva = null;
        }

        // Iniciamos secuencia visual de caida y desaparicion.
        rutinaMuerteActiva = StartCoroutine(RutinaMuerte());
    }

    // Esta corutina limpia el cuerpo muerto y luego destruye el objeto.
    private IEnumerator RutinaMuerte()
    {
        // Esperamos un instante breve para que el cuerpo quede estabilizado antes de alinearlo.
        yield return new WaitForEndOfFrame();

        // Bajamos el cuerpo al suelo antes de disparar la animacion de muerte.
        yield return StartCoroutine(AlinearAlSuelo(Mathf.Max(0.05f, duracionCaida)));

        // Reproducimos la animacion de muerte desde el controlador visual.
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.ReproducirMorir();
        }
        else
        {
            Debug.LogWarning("[EnemigoDummy] No se encontro ControladorAnimacionEnemigo para reproducir la muerte.");
        }

        // Esperamos a que la animacion notifique su fin o a que venza el respaldo.
        float tiempoLimite = Mathf.Max(0.5f, tiempoDesaparecer);
        float tiempoInicio = Time.time;

        while (!muerteAnimacionTerminada && Time.time - tiempoInicio < tiempoLimite)
        {
            yield return null;
        }

        if (!muerteAnimacionTerminada)
        {
            Debug.LogWarning("[EnemigoDummy] La animacion de muerte no notifico a tiempo; aplicando respaldo de destruccion.");
        }

        DesvincularEventoMuerteAnimacion();

        // En Mirror, aqui la destruccion deberia replicarse desde servidor con [ClientRpc].
        // [Mirror futuro] La destruccion de red debe pasar por autoridad de servidor.

        // Destruimos el objeto enemigo de la escena.
        Destroy(gameObject);
    }

    // Esta corrutina alinea el enemigo al suelo usando un raycast simple.
    private IEnumerator AlinearAlSuelo(float duracion)
    {
        Vector3 posicionInicial = transform.position;
        float alturaObjetivo = posicionInicial.y;

        if (Physics.Raycast(posicionInicial + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f))
        {
            alturaObjetivo = hit.point.y;
        }

        Vector3 posicionFinal = new Vector3(posicionInicial.x, alturaObjetivo, posicionInicial.z);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = duracion <= 0.0001f ? 1f : Mathf.Clamp01(tiempo / duracion);
            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, progreso);
            yield return null;
        }

        transform.position = posicionFinal;
    }

    // Esta funcion desactiva los colliders de gameplay sin tocar otros componentes innecesarios.
    private void DesactivarCollidersGameplay()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int indiceCollider = 0; indiceCollider < colliders.Length; indiceCollider++)
        {
            if (colliders[indiceCollider] != null)
            {
                colliders[indiceCollider].enabled = false;
            }
        }
    }

    // Esta funcion suscribe el evento del controlador visual para saber cuando termino la muerte.
    private void VincularEventoMuerteAnimacion()
    {
        if (controladorAnimacionEnemigo == null)
        {
            return;
        }

        controladorAnimacionEnemigo.OnMuerteAnimacionTerminada -= ManejarFinMuerteAnimacion;
        controladorAnimacionEnemigo.OnMuerteAnimacionTerminada += ManejarFinMuerteAnimacion;
    }

    // Esta funcion desuscribe el evento del controlador visual de forma segura.
    private void DesvincularEventoMuerteAnimacion()
    {
        if (controladorAnimacionEnemigo == null)
        {
            return;
        }

        controladorAnimacionEnemigo.OnMuerteAnimacionTerminada -= ManejarFinMuerteAnimacion;
    }

    // Esta funcion marca que la animacion de muerte ya termino.
    private void ManejarFinMuerteAnimacion()
    {
        muerteAnimacionTerminada = true;
    }

    // Este metodo rota al enemigo para mirar al objetivo en plano horizontal.
    private void RotarHaciaObjetivo(Vector3 posicionObjetivo)
    {
        // Calculamos direccion horizontal hacia el objetivo.
        Vector3 direccion = posicionObjetivo - transform.position;

        // Quitamos componente vertical para no inclinarse.
        direccion.y = 0f;

        // Si la direccion es casi cero, no rotamos.
        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        // Calculamos la rotacion deseada mirando la direccion.
        Quaternion rotacionDeseada = Quaternion.LookRotation(direccion.normalized, Vector3.up);

        // Interpolamos suavemente la rotacion.
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * 10f);
    }

    // Este metodo revisa si hay linea de vista real al jugador.
    private bool TieneLineaDeVisionConJugador()
    {
        // Si no hay objetivo, no hay vision posible.
        if (objetivoJugador == null)
        {
            return false;
        }

        // Calculamos direccion horizontal hacia el jugador.
        Vector3 direccionHorizontal = objetivoJugador.position - transform.position;

        // Quitamos altura para evaluar solo el cono en el plano.
        direccionHorizontal.y = 0f;

        // Si la direccion es demasiado pequeña, no seguimos.
        if (direccionHorizontal.sqrMagnitude < 0.001f)
        {
            return false;
        }

        // Calculamos el angulo entre el frente del enemigo y el jugador.
        float anguloHaciaJugador = Vector3.Angle(transform.forward, direccionHorizontal.normalized);

        // Si queda fuera del cono de vision, devolvemos falso.
        if (anguloHaciaJugador > anguloVision * 0.5f)
        {
            return false;
        }

        // Definimos origen del raycast a la altura de ojos del enemigo.
        Vector3 origen = transform.position + Vector3.up * alturaOjos;

        // Definimos destino aproximado al torso del jugador.
        Vector3 destino = objetivoJugador.position + Vector3.up * 1f;

        // Calculamos direccion hacia el objetivo.
        Vector3 direccion = (destino - origen).normalized;

        // Calculamos distancia total a verificar.
        float distancia = Vector3.Distance(origen, destino);

        // Lanzamos raycast multiple para elegir el primer impacto util.
        RaycastHit[] impactos = Physics.RaycastAll(origen, direccion, distancia, mascaraObstaculosVision, QueryTriggerInteraction.Ignore);

        // Guardamos la distancia mas corta que no sea nuestro propio collider.
        float distanciaImpactoMasCercano = float.MaxValue;

        // Guardamos el transform del impacto mas cercano util.
        Transform impactoMasCercano = null;

        // Recorremos todos los impactos detectados.
        for (int i = 0; i < impactos.Length; i++)
        {
            // Ignoramos impactos contra el mismo enemigo o sus hijos.
            if (impactos[i].transform == transform || impactos[i].transform.IsChildOf(transform))
            {
                continue;
            }

            // Si este impacto esta mas cerca que el guardado, lo tomamos.
            if (impactos[i].distance < distanciaImpactoMasCercano)
            {
                distanciaImpactoMasCercano = impactos[i].distance;
                impactoMasCercano = impactos[i].transform;
            }
        }

        // Si no hubo impacto util, consideramos que hay vision libre.
        if (impactoMasCercano == null)
        {
            return true;
        }

        // Solo hay linea de vision si el primer impacto util es el jugador.
        return impactoMasCercano == objetivoJugador || impactoMasCercano.IsChildOf(objetivoJugador);
    }

    // Este metodo prepara la lista de puntos de patrulla.
    private void PrepararPuntosPatrulla()
    {
        // Si ya hay puntos manuales, sincronizamos sus componentes y terminamos.
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            SincronizarComponentesPuntosPatrulla();
            return;
        }

        // Si no queremos buscar automaticamente en hijos, terminamos.
        if (!autoBuscarPuntosPatrullaEnHijos)
        {
            return;
        }

        // Buscamos todos los componentes PuntoPatrulla en hijos.
        puntosPatrullaComponentes = GetComponentsInChildren<PuntoPatrulla>();

        // Si no encontramos ninguno, terminamos.
        if (puntosPatrullaComponentes == null || puntosPatrullaComponentes.Length == 0)
        {
            return;
        }

        // Creamos un array de transforms del mismo tamaño.
        puntosPatrulla = new Transform[puntosPatrullaComponentes.Length];

        // Copiamos cada transform de cada punto encontrado.
        for (int indicePunto = 0; indicePunto < puntosPatrullaComponentes.Length; indicePunto++)
        {
            puntosPatrulla[indicePunto] = puntosPatrullaComponentes[indicePunto].transform;
        }
    }

    // Este metodo sincroniza el array de componentes PuntoPatrulla con el array de transforms.
    private void SincronizarComponentesPuntosPatrulla()
    {
        // Si no hay array de puntos, limpiamos el cache y salimos.
        if (puntosPatrulla == null)
        {
            puntosPatrullaComponentes = null;
            return;
        }

        // Creamos el array con el mismo largo que los transforms.
        puntosPatrullaComponentes = new PuntoPatrulla[puntosPatrulla.Length];

        // Recorremos todos los transforms de patrulla.
        for (int indicePunto = 0; indicePunto < puntosPatrulla.Length; indicePunto++)
        {
            // Si el transform existe, intentamos tomar su componente PuntoPatrulla.
            if (puntosPatrulla[indicePunto] != null)
            {
                puntosPatrullaComponentes[indicePunto] = puntosPatrulla[indicePunto].GetComponent<PuntoPatrulla>();
            }
        }
    }

    // Este metodo selecciona el punto de patrulla mas cercano para arrancar de forma natural.
    private void SeleccionarPuntoPatrullaMasCercano()
    {
        // Si no hay puntos validos, no hacemos nada.
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            return;
        }

        // Arrancamos asumiendo que el primero es el mas cercano.
        float mejorDistancia = float.MaxValue;

        // Recorremos todos los puntos posibles.
        for (int indicePunto = 0; indicePunto < puntosPatrulla.Length; indicePunto++)
        {
            // Si el punto es nulo, lo ignoramos.
            if (puntosPatrulla[indicePunto] == null)
            {
                continue;
            }

            // Calculamos la distancia desde el enemigo a ese punto.
            float distanciaActual = Vector3.Distance(transform.position, puntosPatrulla[indicePunto].position);

            // Si esta distancia es mejor, actualizamos el indice actual.
            if (distanciaActual < mejorDistancia)
            {
                mejorDistancia = distanciaActual;
                indicePuntoPatrulla = indicePunto;
            }
        }
    }

    // Este metodo devuelve el tiempo de espera real del punto actual.
    private float ObtenerTiempoEsperaPuntoActual()
    {
        // Si no hay componentes de puntos, usamos el valor por defecto del enemigo.
        if (puntosPatrullaComponentes == null || indicePuntoPatrulla < 0 || indicePuntoPatrulla >= puntosPatrullaComponentes.Length)
        {
            return tiempoEsperaEnPunto;
        }

        // Si el componente actual es nulo, usamos el valor por defecto.
        if (puntosPatrullaComponentes[indicePuntoPatrulla] == null)
        {
            return tiempoEsperaEnPunto;
        }

        // Devolvemos el tiempo personalizado o el por defecto segun el punto.
        return puntosPatrullaComponentes[indicePuntoPatrulla].ObtenerTiempoEspera(tiempoEsperaEnPunto);
    }

    // Este metodo permite asignar puntos desde un editor script o desde otra herramienta.
    public void AsignarPuntosPatrulla(Transform[] nuevosPuntos)
    {
        // Guardamos el nuevo array de puntos.
        puntosPatrulla = nuevosPuntos;

        // Sincronizamos el cache de componentes.
        SincronizarComponentesPuntosPatrulla();

        // Elegimos de nuevo el punto mas cercano para arrancar mejor.
        SeleccionarPuntoPatrullaMasCercano();
    }

    // Este metodo define destino del agente solo si esta sobre NavMesh.
    private void IntentarDefinirDestino(Vector3 destino)
    {
        // Si el agente no existe, no hacemos nada.
        if (agenteNavMesh == null)
        {
            return;
        }

        // Si el componente esta deshabilitado, no se puede navegar.
        if (!agenteNavMesh.enabled)
        {
            return;
        }

        // Si el agente no esta sobre una zona NavMesh valida, evitamos error.
        if (!agenteNavMesh.isOnNavMesh)
        {
            return;
        }

        // Asignamos el destino de navegacion.
        agenteNavMesh.SetDestination(destino);
    }

    // Este metodo inicia la corrutina de inicializacion diferida de IA.
    private void IniciarInicializacionDiferida()
    {
        // Si este objeto no esta activo, no iniciamos corrutina.
        if (!isActiveAndEnabled)
        {
            return;
        }

        // Si ya habia una corrutina previa, la reiniciamos para tomar estado fresco.
        if (rutinaInicializacionIA != null)
        {
            StopCoroutine(rutinaInicializacionIA);
        }

        // Lanzamos la inicializacion post-frame para evitar race conditions tras spawn/reload.
        rutinaInicializacionIA = StartCoroutine(RutinaInicializacionDiferida());
    }

    // Esta corrutina inicializa IA y NavMeshAgent luego de que termine el frame de spawn.
    private IEnumerator RutinaInicializacionDiferida()
    {
        // Marcamos temporalmente como no inicializada mientras preparamos estado.
        iaInicializada = false;

        // Esperamos al final del frame para dar tiempo a que Unity registre NavMesh y colliders.
        yield return new WaitForEndOfFrame();

        // Si el objeto se desactivo durante la espera, abortamos.
        if (!isActiveAndEnabled)
        {
            rutinaInicializacionIA = null;
            yield break;
        }

        // Restauramos valores temporales de IA para un arranque limpio.
        ReiniciarEstadoTemporalIA();

        // Validamos parametros de vision por seguridad post-restart.
        ValidarParametrosVision();

        // Intentamos enganchar el agente sobre NavMesh en varios frames.
        for (int intentoNavMesh = 0; intentoNavMesh < 6; intentoNavMesh++)
        {
            // Si ya esta correctamente en NavMesh, no hace falta seguir intentando.
            if (AsegurarAgenteSobreNavMesh(6f))
            {
                break;
            }

            // Esperamos al siguiente frame para reintentar.
            yield return new WaitForEndOfFrame();
        }

        // Si el agente se perdio durante la espera, abortamos la inicializacion.
        if (agenteNavMesh == null)
        {
            rutinaInicializacionIA = null;
            yield break;
        }

        // Aplicamos velocidad inicial de patrulla.
        agenteNavMesh.speed = velocidadPatrulla;

        // Habilitamos movimiento del agente.
        agenteNavMesh.isStopped = false;

        // Buscamos un jugador inicial para tener objetivo temprano.
        BuscarObjetivoJugador();

        // Preparamos la lista de puntos de patrulla si hace falta.
        PrepararPuntosPatrulla();

        // Elegimos el punto mas cercano para empezar la patrulla de forma natural.
        SeleccionarPuntoPatrullaMasCercano();

        // Si hay puntos de patrulla configurados, arrancamos yendo al primero.
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            // Indicamos el primer destino de patrulla.
            IntentarDefinirDestino(puntosPatrulla[indicePuntoPatrulla].position);
        }

        // Marcamos la IA como lista para ejecutar logica normal.
        iaInicializada = true;

        // Limpiamos referencia de corrutina activa.
        rutinaInicializacionIA = null;
    }

    // Este metodo intenta asegurar que el agente este anclado a una zona NavMesh valida.
    private bool AsegurarAgenteSobreNavMesh(float radioBusqueda)
    {
        // Si el agente no existe o esta deshabilitado, no podemos usar navmesh.
        if (agenteNavMesh == null || !agenteNavMesh.enabled)
        {
            return false;
        }

        // Si ya esta sobre NavMesh, devolvemos exito.
        if (agenteNavMesh.isOnNavMesh)
        {
            return true;
        }

        // Intentamos encontrar un punto cercano valido en NavMesh.
        NavMeshHit hitNavMesh;
        bool encontroPunto = NavMesh.SamplePosition(transform.position, out hitNavMesh, Mathf.Max(1f, radioBusqueda), NavMesh.AllAreas);

        // Si no encontramos punto, devolvemos falso para reintentar despues.
        if (!encontroPunto)
        {
            return false;
        }

        // Intentamos mover el agente exactamente al punto valido.
        bool warpExitoso = agenteNavMesh.Warp(hitNavMesh.position);

        // Si Warp fallo, ajustamos transform manualmente como fallback.
        if (!warpExitoso)
        {
            transform.position = hitNavMesh.position;
        }

        // Devolvemos si finalmente quedo sobre NavMesh.
        return agenteNavMesh.isOnNavMesh;
    }

    // Este metodo reinicia estado temporal para evitar basura de sesiones previas.
    private void ReiniciarEstadoTemporalIA()
    {
        // Restauramos el estado inicial configurado en Inspector.
        estadoActual = estadoInicialConfigurado;

        // Reiniciamos indice de patrulla y timers basicos.
        indicePuntoPatrulla = 0;
        temporizadorEsperaPatrulla = 0f;
        tiempoProximoAtaque = 0f;
        tiempoUltimaVisionJugador = -999f;
        tiempoProximoChequeoVision = 0f;
        jugadorVisibleEnEsteFrame = false;
        ataqueEnCurso = false;
        danioAtaqueAplicado = false;
        tiempoProximoReintentoNavMesh = Time.time;
    }

    // Este metodo ajusta parametros de vision para mantener valores robustos.
    private void ValidarParametrosVision()
    {
        // Si el intervalo viene invalido, volvemos al recomendado de 0.1 segundos.
        if (intervaloChequeoVision <= 0f)
        {
            intervaloChequeoVision = IntervaloVisionRecomendado;
        }

        // Si el rango viene invalido, volvemos al recomendado de 15.
        if (rangoVision <= 0f)
        {
            rangoVision = RangoVisionRecomendado;
        }

        // Si el angulo viene invalido, volvemos al recomendado de 120.
        if (anguloVision <= 0f)
        {
            anguloVision = AnguloVisionRecomendado;
        }

        // Forzamos minimos saludables para que la IA no quede ciega por una config vieja de escena.
        intervaloChequeoVision = Mathf.Clamp(intervaloChequeoVision, 0.05f, IntervaloVisionRecomendado);
        rangoVision = Mathf.Clamp(rangoVision, RangoVisionRecomendado, 100f);
        anguloVision = Mathf.Clamp(anguloVision, AnguloVisionRecomendado, 179f);
    }

    // Este metodo se ejecuta en editor cuando cambia un valor serializado.
    private void OnValidate()
    {
        // Mantenemos la configuracion de vision en rangos seguros tambien desde Inspector.
        ValidarParametrosVision();
    }

    // Este metodo publico permite a spawners pedir reinicio robusto de IA tras instanciar.
    public void ReiniciarIATrasSpawn()
    {
        // Relanzamos la inicializacion diferida para asegurar navmesh y objetivo.
        IniciarInicializacionDiferida();
    }

    // COPILOT-EXPAND: Aqui podes agregar ataques especiales, llamadas de ayuda, evasiones y comportamientos de grupo.
}
