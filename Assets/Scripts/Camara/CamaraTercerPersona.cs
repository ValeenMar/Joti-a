using UnityEngine;

// Esta clase controla una camara de tercera persona con seguimiento suave y prevencion de colisiones.
[DisallowMultipleComponent]
public class CamaraTercerPersona : MonoBehaviour
{
    // Este transform es el objetivo que la camara debe seguir (idealmente el anclaje del jugador).
    [SerializeField] private Transform objetivoSeguimiento;

    // Esta capa define contra que objetos se evaluara la colision de camara.
    [SerializeField] private LayerMask mascaraColision = ~0;

    // Esta distancia es la separacion deseada entre camara y objetivo cuando no hay obstaculos.
    [SerializeField] private float distanciaDeseada = 4.5f;

    // Esta distancia minima evita que la camara se pegue demasiado al jugador.
    [SerializeField] private float distanciaMinima = 1.1f;

    // Este radio se usa en SphereCast para evitar atravesar paredes y piso.
    [SerializeField] private float radioColision = 0.25f;

    // Este margen separa la camara un poquito del obstaculo para evitar clipping visual.
    [SerializeField] private float margenSeparacion = 0.1f;

    // Esta velocidad suaviza la posicion para que la camara no se mueva a saltos.
    [SerializeField] private float suavizadoPosicion = 0.08f;

    // Esta velocidad suaviza la rotacion final de la camara.
    [SerializeField] private float suavizadoRotacion = 12f;

    // Esta sensibilidad controla cuanto rota con el mouse en eje horizontal.
    [SerializeField] private float sensibilidadMouseX = 120f;

    // Esta sensibilidad controla cuanto rota con el mouse en eje vertical.
    [SerializeField] private float sensibilidadMouseY = 90f;

    // Este limite evita que la camara mire demasiado hacia abajo.
    [SerializeField] private float limiteMinimoVertical = -25f;

    // Este limite evita que la camara mire demasiado hacia arriba.
    [SerializeField] private float limiteMaximoVertical = 65f;

    // Esta variable guarda el yaw acumulado para rotacion horizontal.
    private float anguloHorizontal;

    // Esta variable guarda el pitch acumulado para rotacion vertical.
    private float anguloVertical = 15f;

    // Esta variable guarda la velocidad interna del SmoothDamp.
    private Vector3 velocidadSuavizado;

    // Esta variable guarda la posicion suavizada final.
    private Vector3 posicionSuavizada;

    // Esta referencia se usa para leer datos de la camara real.
    private Camera camaraUnity;

    // Esta funcion se ejecuta al iniciar para preparar referencias.
    private void Awake()
    {
        // Guardamos la referencia de la camara de Unity para uso opcional futuro.
        camaraUnity = GetComponent<Camera>();

        // Si no se asigno objetivo en Inspector, intentamos resolver uno automaticamente.
        if (objetivoSeguimiento == null)
        {
            IntentarAsignarObjetivoAutomatico();
        }

        // Inicializamos angulos segun la rotacion actual para evitar saltos al entrar en Play.
        Vector3 angulosActuales = transform.eulerAngles;
        anguloHorizontal = angulosActuales.y;
        anguloVertical = angulosActuales.x;

        // Guardamos posicion actual como base de suavizado inicial.
        posicionSuavizada = transform.position;
    }

    // Esta funcion corre despues del movimiento del jugador y actualiza la camara.
    private void LateUpdate()
    {
        // Si todavia no tenemos objetivo, intentamos asignarlo automaticamente otra vez.
        if (objetivoSeguimiento == null)
        {
            IntentarAsignarObjetivoAutomatico();
        }

        // Si aun no hay objetivo, no podemos mover la camara y salimos.
        if (objetivoSeguimiento == null)
        {
            return;
        }

        // NOTA MIRROR: este bloque de input debe correr solo en el cliente local que controla la camara.
        // En el futuro con Mirror, normalmente esto iria ligado al LocalPlayer (hasAuthority/isLocalPlayer).
        float entradaMouseX = Input.GetAxis("Mouse X");
        float entradaMouseY = Input.GetAxis("Mouse Y");

        // Acumulamos rotacion horizontal segun movimiento del mouse y sensibilidad.
        anguloHorizontal += entradaMouseX * sensibilidadMouseX * Time.deltaTime;

        // Acumulamos rotacion vertical invertida para comportamiento clasico de camara tercera persona.
        anguloVertical -= entradaMouseY * sensibilidadMouseY * Time.deltaTime;

        // Limitamos rotacion vertical para no atravesar angulos incomodos.
        anguloVertical = Mathf.Clamp(anguloVertical, limiteMinimoVertical, limiteMaximoVertical);

        // Construimos la rotacion deseada en base a los angulos calculados.
        Quaternion rotacionDeseada = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);

        // Guardamos el punto foco que la camara debe mirar.
        Vector3 puntoFoco = objetivoSeguimiento.position;

        // Calculamos la posicion ideal sin obstaculos.
        Vector3 posicionIdeal = puntoFoco - rotacionDeseada * Vector3.forward * distanciaDeseada;

        // Ajustamos la posicion ideal en caso de colision con piso o paredes.
        Vector3 posicionAjustada = AjustarPosicionPorColision(puntoFoco, posicionIdeal);

        // Suavizamos la posicion para que siga al jugador sin tirones.
        posicionSuavizada = Vector3.SmoothDamp(posicionSuavizada, posicionAjustada, ref velocidadSuavizado, suavizadoPosicion);

        // Aplicamos posicion final suavizada.
        transform.position = posicionSuavizada;

        // Calculamos la rotacion que mira al foco desde la posicion actual.
        Quaternion rotacionMirandoAlFoco = Quaternion.LookRotation((puntoFoco - transform.position).normalized, Vector3.up);

        // Suavizamos la rotacion para evitar micro-jitter.
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionMirandoAlFoco, suavizadoRotacion * Time.deltaTime);
    }

    // Este metodo calcula una posicion segura usando SphereCast para no atravesar objetos.
    private Vector3 AjustarPosicionPorColision(Vector3 puntoOrigen, Vector3 posicionObjetivoSinColision)
    {
        // Calculamos direccion desde el foco hacia la posicion deseada de camara.
        Vector3 direccion = (posicionObjetivoSinColision - puntoOrigen).normalized;

        // Calculamos la distancia maxima que intentamos recorrer con el cast.
        float distanciaMaxima = Vector3.Distance(puntoOrigen, posicionObjetivoSinColision);

        // Ejecutamos SphereCast para detectar si hay un obstaculo en el camino de la camara.
        if (Physics.SphereCast(puntoOrigen, radioColision, direccion, out RaycastHit golpe, distanciaMaxima, mascaraColision, QueryTriggerInteraction.Ignore))
        {
            // Si hubo golpe, reducimos la distancia para ubicar la camara delante del obstaculo.
            float distanciaSegura = Mathf.Max(distanciaMinima, golpe.distance - margenSeparacion);

            // Devolvemos la posicion segura evitando clipping.
            return puntoOrigen + direccion * distanciaSegura;
        }

        // Si no hubo colision, devolvemos la posicion ideal original.
        return posicionObjetivoSinColision;
    }

    // Este metodo intenta encontrar automaticamente al jugador y su anclaje.
    private void IntentarAsignarObjetivoAutomatico()
    {
        // Primero intentamos buscar un objeto con tag Player para flujos estandar.
        GameObject posibleJugador = GameObject.FindGameObjectWithTag("Player");

        // Si no hay tag Player, usamos el nombre Jugador del objeto de la escena actual.
        if (posibleJugador == null)
        {
            posibleJugador = GameObject.Find("Jugador");
        }

        // Si todavia no encontramos jugador, salimos y probaremos en otro frame.
        if (posibleJugador == null)
        {
            return;
        }

        // Revisamos si ese jugador tiene un componente de anclaje de camara.
        AnclajeCamaraJugador anclaje = posibleJugador.GetComponent<AnclajeCamaraJugador>();

        // Si tiene anclaje, usamos su objetivo recomendado para mejor estabilidad visual.
        if (anclaje != null)
        {
            objetivoSeguimiento = anclaje.ObtenerObjetivoCamara();
            return;
        }

        // Si no tiene anclaje, seguimos directamente el transform del jugador.
        objetivoSeguimiento = posibleJugador.transform;
    }

    // Este metodo permite asignar el objetivo manualmente desde otros scripts.
    public void AsignarObjetivo(Transform nuevoObjetivo)
    {
        // Guardamos el nuevo objetivo de seguimiento.
        objetivoSeguimiento = nuevoObjetivo;
    }

    // Este metodo permite saber si esta camara esta siguiendo al jugador indicado.
    public bool EstaSiguiendoJugador(GameObject jugador)
    {
        // Si falta objetivo o jugador, devolvemos falso para evitar errores nulos.
        if (objetivoSeguimiento == null || jugador == null)
        {
            return false;
        }

        // Comparamos por raiz de jerarquia para cubrir casos con anclaje hijo.
        return objetivoSeguimiento.root == jugador.transform.root;
    }

    // Este metodo dibuja gizmos utiles para depurar distancia y colision en el editor.
    private void OnDrawGizmosSelected()
    {
        // Si no hay objetivo no podemos dibujar datos utiles, asi que salimos.
        if (objetivoSeguimiento == null)
        {
            return;
        }

        // Definimos color cian para visualizar el punto foco de seguimiento.
        Gizmos.color = Color.cyan;

        // Dibujamos una esfera pequena en el punto foco.
        Gizmos.DrawSphere(objetivoSeguimiento.position, 0.08f);

        // Definimos color amarillo para visualizar la posicion de la camara.
        Gizmos.color = Color.yellow;

        // Dibujamos una esfera en la posicion actual de la camara.
        Gizmos.DrawWireSphere(transform.position, radioColision);
    }
}

