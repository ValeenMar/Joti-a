using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

    // Esta velocidad se usa mientras persigue.
    [Header("Persecucion")]
    [SerializeField] private float velocidadPersecucion = 3.5f;

    // Esta distancia define si puede ver al jugador.
    [SerializeField] private float rangoVision = 10f;

    // Esta mascara sirve para comprobar obstaculos entre enemigo y jugador.
    [SerializeField] private LayerMask mascaraObstaculosVision = ~0;

    // Esta altura ayuda a lanzar el raycast desde un punto cercano a los ojos.
    [SerializeField] private float alturaOjos = 1.5f;

    // Esta distancia define cuando empieza a atacar.
    [Header("Ataque")]
    [SerializeField] private float distanciaAtaque = 2.1f;

    // Este dano base se aplica al jugador cuando conecta el ataque.
    [SerializeField] private float danioAtaque = 12f;

    // Este tiempo representa el cooldown entre ataques consecutivos.
    [SerializeField] private float tiempoEntreAtaques = 1.5f;

    // Este retraso crea sensacion de peso antes de que entre el dano.
    [SerializeField] private float delayImpactoAtaque = 0.35f;

    // Este tiempo define cuanto tarda en caer antes de desaparecer.
    [Header("Muerte")]
    [SerializeField] private float duracionCaida = 0.35f;

    // Este tiempo define cuando se destruye el enemigo muerto.
    [SerializeField] private float tiempoDesaparecer = 3f;

    // Este prefab visual se instancia al morir.
    [SerializeField] private GameObject prefabOrbeExperiencia;

    // Este desplazamiento permite soltar la orbe apenas arriba del suelo.
    [SerializeField] private Vector3 offsetOrbe = new Vector3(0f, 0.25f, 0f);

    // Referencia al componente de navegacion.
    private NavMeshAgent agenteNavMesh;

    // Referencia al componente de vida.
    private VidaEnemigo vidaEnemigo;

    // Referencia al objetivo jugador actual.
    private Transform objetivoJugador;

    // Referencia al componente de vida del jugador.
    private VidaJugador vidaJugadorObjetivo;

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
            // Cambiamos al estado de ataque.
            estadoActual = EstadosEnemigo.Atacar;

            // Terminamos esta decision.
            return;
        }

        // Si esta en rango de vision y hay linea de vista, perseguimos.
        if (distanciaJugador <= rangoVision && TieneLineaDeVisionConJugador())
        {
            // Cambiamos al estado de persecucion.
            estadoActual = EstadosEnemigo.Perseguir;

            // Terminamos esta decision.
            return;
        }

        // Si no cumple condiciones de vision o ataque, seguimos patrullando.
        estadoActual = EstadosEnemigo.Patrullar;
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

        // Si no hay puntos, no hacemos nada.
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            // Salimos para evitar errores por indice invalido.
            return;
        }

        // Si todavia esta calculando ruta, esperamos al siguiente frame.
        if (agenteNavMesh.pathPending)
        {
            // Salimos para no tomar decisiones prematuras.
            return;
        }

        // Si llego al punto actual, empezamos a contar espera.
        if (agenteNavMesh.remainingDistance <= agenteNavMesh.stoppingDistance + 0.05f)
        {
            // Sumamos tiempo esperando en este punto.
            temporizadorEsperaPatrulla += Time.deltaTime;

            // Si ya espero suficiente, pasamos al siguiente punto.
            if (temporizadorEsperaPatrulla >= tiempoEsperaEnPunto)
            {
                // Reiniciamos el temporizador de espera.
                temporizadorEsperaPatrulla = 0f;

                // Avanzamos al siguiente indice en bucle.
                indicePuntoPatrulla = (indicePuntoPatrulla + 1) % puntosPatrulla.Length;

                // Ordenamos ir al siguiente punto de patrulla.
                IntentarDefinirDestino(puntosPatrulla[indicePuntoPatrulla].position);
            }
        }
    }

    // Este metodo implementa el comportamiento de persecucion.
    private void EjecutarPersecucion()
    {
        // Si no hay objetivo, no hay a quien perseguir.
        if (objetivoJugador == null)
        {
            // Salimos para evitar referencias nulas.
            return;
        }

        // Ajustamos velocidad de persecucion.
        agenteNavMesh.speed = velocidadPersecucion;

        // Permitimos movimiento del agente.
        agenteNavMesh.isStopped = false;

        // Actualizamos destino continuamente con posicion del jugador.
        IntentarDefinirDestino(objetivoJugador.position);
    }

    // Este metodo implementa el comportamiento de ataque.
    private void EjecutarAtaque()
    {
        // Si no hay objetivo, salimos.
        if (objetivoJugador == null)
        {
            // Sin objetivo no podemos atacar.
            return;
        }

        // Frenamos el agente para atacar en sitio.
        agenteNavMesh.isStopped = true;

        // Rotamos suavemente hacia el jugador para que el golpe tenga direccion.
        RotarHaciaObjetivo(objetivoJugador.position);

        // Si ya hay un ataque corriendo, no iniciamos otro.
        if (ataqueEnCurso)
        {
            // Esperamos a que termine la corutina actual.
            return;
        }

        // Si aun no paso el cooldown, no atacamos todavia.
        if (Time.time < tiempoProximoAtaque)
        {
            // Esperamos hasta que se cumpla el tiempo.
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
            // Limpiamos bandera y salimos.
            ataqueEnCurso = false;
            yield break;
        }

        // Si el objetivo desaparecio durante la espera, cancelamos.
        if (objetivoJugador == null)
        {
            // Limpiamos bandera y salimos.
            ataqueEnCurso = false;
            yield break;
        }

        // Revalidamos distancia para no pegar a distancia irreal.
        float distanciaActual = Vector3.Distance(transform.position, objetivoJugador.position);

        // Si sigue en rango de ataque, aplicamos dano.
        if (distanciaActual <= distanciaAtaque + 0.25f)
        {
            // Ejecutamos aplicacion real de dano al jugador.
            AplicarDanioAlJugador();
        }

        // Guardamos cuando podra atacar nuevamente.
        tiempoProximoAtaque = Time.time + tiempoEntreAtaques;

        // Marcamos que termino el ataque actual.
        ataqueEnCurso = false;
    }

    // Este metodo aplica dano al objetivo jugador usando IRecibidorDanio.
    private void AplicarDanioAlJugador()
    {
        // Si no hay objetivo valido, cortamos.
        if (objetivoJugador == null)
        {
            // Salimos sin hacer nada.
            return;
        }

        // Intentamos obtener IRecibidorDanio en el objetivo.
        IRecibidorDanio recibidor = objetivoJugador.GetComponent<IRecibidorDanio>();

        // Si no esta en el objeto raiz, buscamos en sus hijos.
        if (recibidor == null)
        {
            // Buscamos en children por robustez.
            recibidor = objetivoJugador.GetComponentInChildren<IRecibidorDanio>();
        }

        // Si no encontramos recibidor, no podemos aplicar dano.
        if (recibidor == null)
        {
            // Salimos para evitar null reference.
            return;
        }

        // Construimos los datos de dano para el sistema compartido.
        DatosDanio datosDanio = new DatosDanio
        {
            // El atacante es este enemigo.
            atacante = gameObject,

            // El objetivo es el jugador detectado.
            objetivo = objetivoJugador.gameObject,

            // Dano base del ataque enemigo.
            danioBase = danioAtaque,

            // El enemigo no usa critico por zona en este ataque simple.
            multiplicadorZona = 1f,

            // Marcamos dano como cuerpo para feedback normal.
            tipoZona = TipoZonaDanio.Cuerpo,

            // Punto aproximado del impacto.
            puntoImpacto = objetivoJugador.position + Vector3.up,

            // Direccion en la que viaja el golpe.
            direccionImpacto = (objetivoJugador.position - transform.position).normalized
        };

        // Enviamos el golpe al sistema de vida del jugador.
        recibidor.RecibirDanio(datosDanio);
    }

    // Este metodo responde al evento de muerte de VidaEnemigo.
    private void ManejarMuerteEnemigo(DatosDanio datosDanio)
    {
        // Cambiamos estado interno a muerto.
        estadoActual = EstadosEnemigo.Muerto;

        // Frenamos cualquier intento de navegacion.
        if (agenteNavMesh != null && agenteNavMesh.enabled)
        {
            // Detenemos el agente.
            agenteNavMesh.isStopped = true;
        }

        // Si ya habia una rutina de muerte, no iniciamos otra.
        if (rutinaMuerteActiva != null)
        {
            // Salimos para evitar dobles destrucciones.
            return;
        }

        // Iniciamos secuencia visual de caida y desaparicion.
        rutinaMuerteActiva = StartCoroutine(RutinaMuerte());
    }

    // Esta corutina hace caer al enemigo, suelta orbe y destruye el objeto.
    private IEnumerator RutinaMuerte()
    {
        // Desactivamos colliders para evitar choques raros tras morir.
        Collider[] colliders = GetComponentsInChildren<Collider>();

        // Recorremos todos los colliders encontrados.
        for (int i = 0; i < colliders.Length; i++)
        {
            // Apagamos cada collider para limpiar interacciones.
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
            // Sumamos deltaTime en cada frame.
            tiempoCaida += Time.deltaTime;

            // Calculamos progreso normalizado de 0 a 1.
            float progreso = Mathf.Clamp01(tiempoCaida / duracionCaida);

            // Aplicamos rotacion suave entre inicio y fin.
            transform.rotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, progreso);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Si hay prefab de orbe configurado, lo instanciamos.
        if (prefabOrbeExperiencia != null)
        {
            // Creamos la orbe en posicion del enemigo con un pequeno offset.
            Instantiate(prefabOrbeExperiencia, transform.position + offsetOrbe, Quaternion.identity);
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
            // Salimos para evitar rotaciones invalidas.
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
            // Devolvemos falso por seguridad.
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
                // Continuamos para evaluar otros impactos.
                continue;
            }

            // Si este impacto esta mas cerca que el guardado, lo tomamos.
            if (impactos[i].distance < distanciaImpactoMasCercano)
            {
                // Guardamos la nueva distancia minima.
                distanciaImpactoMasCercano = impactos[i].distance;

                // Guardamos el transform correspondiente.
                impactoMasCercano = impactos[i].transform;
            }
        }

        // Si no hubo impacto util, consideramos que hay vision libre.
        if (impactoMasCercano == null)
        {
            // Devolvemos verdadero por no detectar bloqueo.
            return true;
        }

        // Solo hay linea de vision si el primer impacto util es el jugador.
        return impactoMasCercano == objetivoJugador || impactoMasCercano.IsChildOf(objetivoJugador);
    }

    // Este metodo define destino del agente solo si esta sobre NavMesh.
    private void IntentarDefinirDestino(Vector3 destino)
    {
        // Si el agente no existe, no hacemos nada.
        if (agenteNavMesh == null)
        {
            // Salimos por seguridad.
            return;
        }

        // Si el componente esta deshabilitado, no se puede navegar.
        if (!agenteNavMesh.enabled)
        {
            // Salimos por seguridad.
            return;
        }

        // Si el agente no esta sobre una zona NavMesh valida, evitamos error.
        if (!agenteNavMesh.isOnNavMesh)
        {
            // Salimos para evitar warnings de Unity.
            return;
        }

        // Asignamos el destino de navegacion.
        agenteNavMesh.SetDestination(destino);
    }
}
