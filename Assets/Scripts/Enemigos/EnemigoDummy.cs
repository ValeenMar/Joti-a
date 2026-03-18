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

    // Este retraso crea sensacion de peso antes de que entre el dano.
    [SerializeField] private float delayImpactoAtaque = 0.35f;

    // Este tiempo define cuanto tarda en caer antes de desaparecer.
    [Header("Muerte")]
    [SerializeField] private float duracionCaida = 0.35f;

    // Este tiempo define cuando se destruye el enemigo muerto.
    [SerializeField] private float tiempoDesaparecer = 3f;

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

    // Esta variable guarda la ultima posicion donde vio al jugador.
    private Vector3 ultimaPosicionVistaJugador;

    // Esta variable guarda el instante en el que vio por ultima vez al jugador.
    private float tiempoUltimaVisionJugador = -999f;

    // Esta variable marca si el jugador esta siendo visto en este frame.
    private bool jugadorVisibleEnEsteFrame;

    // Esta variable guarda el proximo instante en el que se recalculara la vision.
    private float tiempoProximoChequeoVision;

    // Esta funcion se ejecuta al iniciar el objeto.
    private void Awake()
    {
        // Obtenemos referencia al NavMeshAgent del mismo objeto.
        agenteNavMesh = GetComponent<NavMeshAgent>();

        // Obtenemos referencia al sistema de vida del mismo objeto.
        vidaEnemigo = GetComponent<VidaEnemigo>();
    }

    // Esta funcion se ejecuta cuando el objeto se habilita.
    private void OnEnable()
    {
        // Escuchamos el evento de muerte para iniciar la secuencia final.
        vidaEnemigo.AlMorir += ManejarMuerteEnemigo;
    }

    // Esta funcion se ejecuta cuando el objeto se deshabilita.
    private void OnDisable()
    {
        // Dejamos de escuchar el evento para evitar referencias colgadas.
        vidaEnemigo.AlMorir -= ManejarMuerteEnemigo;
    }

    // Esta funcion se ejecuta una sola vez al empezar la escena.
    private void Start()
    {
        // Inicializamos la velocidad de patrulla.
        agenteNavMesh.speed = velocidadPatrulla;

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
    }

    // Esta funcion se ejecuta cada frame para actualizar IA.
    private void Update()
    {
        // Si el enemigo ya murio, detenemos toda logica de IA.
        if (!vidaEnemigo.EstaVivo)
        {
            // Salimos para no ejecutar comportamiento post mortem.
            return;
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

    // Este metodo intenta localizar un jugador con VidaJugador en la escena.
    private void BuscarObjetivoJugador()
    {
        // Buscamos cualquier componente VidaJugador activo en escena.
        VidaJugador candidato = FindObjectOfType<VidaJugador>();

        // Si encontramos uno, lo guardamos como objetivo.
        if (candidato != null)
        {
            // Guardamos referencia al componente de vida del jugador.
            vidaJugadorObjetivo = candidato;

            // Guardamos referencia al transform del jugador.
            objetivoJugador = candidato.transform;

            // Guardamos la ultima posicion conocida para poder perseguir por memoria si se esconde.
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

        // Iniciamos la rutina de ataque con delay de impacto.
        StartCoroutine(RutinaAtacar());
    }

    // Esta corutina ejecuta el ataque con retraso para sensacion de peso.
    private IEnumerator RutinaAtacar()
    {
        // Marcamos que hay un ataque en progreso.
        ataqueEnCurso = true;

        // En Mirror, desde aqui deberia lanzarse un [Command] al servidor.
        // [Mirror futuro] Validar distancia y cooldown en servidor antes del dano real.

        // Esperamos el delay antes de aplicar el golpe.
        yield return new WaitForSeconds(delayImpactoAtaque);

        // Si el enemigo murio durante la espera, cancelamos.
        if (!vidaEnemigo.EstaVivo)
        {
            ataqueEnCurso = false;
            yield break;
        }

        // Si el objetivo desaparecio durante la espera, cancelamos.
        if (objetivoJugador == null)
        {
            ataqueEnCurso = false;
            yield break;
        }

        // Revalidamos distancia para no pegar a distancia irreal.
        float distanciaActual = Vector3.Distance(transform.position, objetivoJugador.position);

        // Si sigue en rango de ataque, aplicamos dano.
        if (distanciaActual <= distanciaAtaque + 0.25f)
        {
            AplicarDanioAlJugador();
        }

        // Guardamos cuando podra atacar nuevamente.
        tiempoProximoAtaque = Time.time + tiempoEntreAtaques;

        // Marcamos que termino el ataque actual.
        ataqueEnCurso = false;
    }

    // Este metodo aplica dano al objetivo jugador usando directamente VidaJugador.
    private void AplicarDanioAlJugador()
    {
        // Si no tenemos referencia valida a la vida del jugador, cortamos.
        if (vidaJugadorObjetivo == null || !vidaJugadorObjetivo.EstaVivo)
        {
            return;
        }

        // Enviamos el golpe simple directamente al sistema de vida del jugador.
        vidaJugadorObjetivo.RecibirDanio(danioAtaque);
    }

    // Este metodo responde al evento de muerte de VidaEnemigo.
    private void ManejarMuerteEnemigo(DatosDanio datosDanio)
    {
        // Cambiamos estado interno a muerto.
        estadoActual = EstadosEnemigo.Muerto;

        // Frenamos cualquier intento de navegacion.
        if (agenteNavMesh != null && agenteNavMesh.enabled)
        {
            agenteNavMesh.isStopped = true;
        }

        // Si ya habia una rutina de muerte, no iniciamos otra.
        if (rutinaMuerteActiva != null)
        {
            return;
        }

        // Iniciamos secuencia visual de caida y desaparicion.
        rutinaMuerteActiva = StartCoroutine(RutinaMuerte());
    }

    // Esta corutina hace caer al enemigo y destruye el objeto.
    private IEnumerator RutinaMuerte()
    {
        // Desactivamos colliders para evitar choques raros tras morir.
        Collider[] colliders = GetComponentsInChildren<Collider>();

        // Recorremos todos los colliders encontrados.
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        // Guardamos rotacion inicial para animar caida suave.
        Quaternion rotacionInicial = transform.rotation;

        // Calculamos rotacion final inclinada hacia adelante.
        Quaternion rotacionFinal = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

        // Inicializamos temporizador interno de caida.
        float tiempoCaida = 0f;

        // Mientras no termine el tiempo de caida, interpolamos.
        while (tiempoCaida < duracionCaida)
        {
            tiempoCaida += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoCaida / duracionCaida);
            transform.rotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, progreso);
            yield return null;
        }

        // Esperamos el tiempo configurado antes de destruir.
        yield return new WaitForSeconds(tiempoDesaparecer);

        // En Mirror, aqui la destruccion deberia replicarse desde servidor con [ClientRpc].
        // [Mirror futuro] La destruccion de red debe pasar por autoridad de servidor.

        // Destruimos el objeto enemigo de la escena.
        Destroy(gameObject);
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

    // COPILOT-EXPAND: Aqui podes agregar ataques especiales, llamadas de ayuda, evasiones y comportamientos de grupo.
}
