using UnityEngine;

// Esta linea le dice a Unity que este objeto necesita un Rigidbody para funcionar.
[RequireComponent(typeof(Rigidbody))]
public class MovimientoJugador : MonoBehaviour
{
    // Esta variable aparece en el Inspector para que puedas cambiar la velocidad base sin tocar codigo.
    [SerializeField] private float velocidadMovimiento = 5f;

    // Esta variable multiplica la velocidad cuando el jugador esta corriendo.
    [SerializeField] private float multiplicadorSprint = 1.65f;

    // Esta tecla permite correr manteniendola apretada.
    [SerializeField] private KeyCode teclaSprintPrincipal = KeyCode.LeftShift;

    // Esta segunda tecla permite correr tambien con Shift derecho.
    [SerializeField] private KeyCode teclaSprintSecundaria = KeyCode.RightShift;

    // Este valor reduce la velocidad cuando el jugador esta agotado.
    [SerializeField] private float multiplicadorVelocidadAgotado = 0.6f;

    // Esta variable guarda la referencia al Rigidbody del jugador.
    private Rigidbody cuerpoRigido;

    // Esta variable guarda las estadisticas del jugador si existen en el mismo objeto.
    private EstadisticasJugador estadisticasJugador;

    // Esta variable guarda el sistema de estamina si existe en el mismo objeto.
    private Estamina estaminaJugador;

    // Esta variable guarda la referencia a la camara principal para mover al jugador segun lo que ve el jugador.
    private Camera camaraPrincipal;

    // Esta variable guarda cuanto quiere moverse el jugador en horizontal.
    private float entradaHorizontal;

    // Esta variable guarda cuanto quiere moverse el jugador en vertical.
    private float entradaVertical;

    // Esta variable indica si el jugador esta intentando correr en este frame.
    private bool quiereCorrer;

    // Esta variable indica si finalmente este frame se esta aplicando el sprint.
    private bool estaCorriendo;

    // Esta variable guarda la posicion fisica anterior del jugador para interpolar mejor en render.
    private Vector3 posicionFisicaAnterior;

    // Esta variable guarda la posicion fisica actual del jugador para interpolar mejor en render.
    private Vector3 posicionFisicaActual;

    // Esta propiedad permite a otros scripts saber si el jugador esta corriendo.
    public bool EstaCorriendo => estaCorriendo;

    // Esta funcion se ejecuta una vez cuando el objeto se activa.
    private void Awake()
    {
        // Aca buscamos y guardamos el Rigidbody que esta en este mismo objeto.
        cuerpoRigido = GetComponent<Rigidbody>();

        // Aca intentamos obtener las estadisticas del jugador para usar una velocidad escalable.
        estadisticasJugador = GetComponent<EstadisticasJugador>();

        // Aca intentamos obtener la estamina para conectar sprint y agotamiento.
        estaminaJugador = GetComponent<Estamina>();

        // Intentamos encontrar la camara principal para que el movimiento sea relativo a su direccion.
        camaraPrincipal = Camera.main;

        // Esto hace que el movimiento se vea mas suave en pantalla entre pasos de fisica.
        cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;

        // Esto ayuda a detectar mejor colisiones cuando el objeto se mueve.
        cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Guardamos la posicion inicial dos veces para arrancar con una interpolacion limpia.
        posicionFisicaAnterior = cuerpoRigido.position;
        posicionFisicaActual = cuerpoRigido.position;
    }

    // Esta funcion se ejecuta una vez por frame y es ideal para leer input.
    private void Update()
    {
        // Leemos la entrada horizontal del teclado con A, D o flechas izquierda y derecha.
        entradaHorizontal = Input.GetAxisRaw("Horizontal");

        // Leemos la entrada vertical del teclado con W, S o flechas arriba y abajo.
        entradaVertical = Input.GetAxisRaw("Vertical");

        // Leemos si el jugador esta intentando correr con alguna de las dos teclas de sprint.
        quiereCorrer = Input.GetKey(teclaSprintPrincipal) || Input.GetKey(teclaSprintSecundaria);

        // Actualizamos si este frame realmente puede correr segun input y estamina.
        ActualizarEstadoSprint();
    }

    // Esta funcion se ejecuta a ritmo de fisica y es ideal para mover un Rigidbody.
    private void FixedUpdate()
    {
        // Calculamos la direccion de movimiento tomando como referencia la camara si existe.
        Vector3 direccionMovimiento = ObtenerDireccionMovimiento();

        // Si se aprietan dos teclas al mismo tiempo, normalizamos para que no corra mas rapido en diagonal.
        if (direccionMovimiento.magnitude > 1f)
        {
            // Convertimos la direccion en una direccion de largo 1 para mantener una velocidad pareja.
            direccionMovimiento = direccionMovimiento.normalized;
        }

        // Elegimos la velocidad final usando estadisticas si existen, o la velocidad manual si no existen.
        float velocidadFinal = estadisticasJugador != null ? estadisticasJugador.VelocidadActual : velocidadMovimiento;

        // Si este frame se esta corriendo, aplicamos el multiplicador de sprint.
        if (estaCorriendo)
        {
            // Aumentamos la velocidad final para reflejar la carrera.
            velocidadFinal *= multiplicadorSprint;
        }

        // Si existe estamina y el jugador esta agotado, aplicamos la penalizacion fuerte de movimiento.
        if (estaminaJugador != null && estaminaJugador.EstaAgotado)
        {
            // Reducimos la velocidad final para que el agotamiento se sienta mas castigador.
            velocidadFinal *= multiplicadorVelocidadAgotado;
        }

        // Calculamos cuanto se tiene que mover el jugador en este paso de fisica.
        Vector3 desplazamiento = direccionMovimiento * velocidadFinal * Time.fixedDeltaTime;

        // Calculamos la posicion final a la que queremos llegar en este paso.
        Vector3 posicionDestino = cuerpoRigido.position + desplazamiento;

        // Guardamos la posicion fisica anterior para poder interpolar luego en render.
        posicionFisicaAnterior = posicionFisicaActual;

        // Guardamos la posicion fisica actual esperada de este paso.
        posicionFisicaActual = posicionDestino;

        // Movemos el Rigidbody usando fisica en vez de mover el Transform a mano.
        cuerpoRigido.MovePosition(posicionDestino);
    }

    // Esta funcion decide si el jugador puede correr en este frame y consume estamina si corresponde.
    private void ActualizarEstadoSprint()
    {
        // Por defecto, asumimos que este frame no esta corriendo.
        estaCorriendo = false;

        // Si no se esta intentando correr, salimos dejando el sprint apagado.
        if (!quiereCorrer)
        {
            return;
        }

        // Si no hay input de movimiento, no gastamos estamina corriendo quietos.
        if (!HayInputDeMovimiento())
        {
            return;
        }

        // Si no existe sistema de estamina, permitimos correr igual como respaldo.
        if (estaminaJugador == null)
        {
            estaCorriendo = true;
            return;
        }

        // Le pedimos al sistema de estamina consumir el costo continuo del sprint de este frame.
        estaCorriendo = estaminaJugador.ConsumirEstaminaSprint(Time.deltaTime);
    }

    // Esta funcion arma una direccion en el plano horizontal usando la orientacion de la camara.
    private Vector3 ObtenerDireccionMovimiento()
    {
        // Si la referencia a la camara se perdio o aun no existia, la intentamos recuperar.
        if (camaraPrincipal == null)
        {
            // Buscamos nuevamente la camara principal de la escena.
            camaraPrincipal = Camera.main;
        }

        // Si no hay camara disponible, usamos movimiento clasico en ejes del mundo como respaldo.
        if (camaraPrincipal == null)
        {
            // Devolvemos la direccion usando solo el teclado y los ejes globales.
            return new Vector3(entradaHorizontal, 0f, entradaVertical);
        }

        // Elegimos una referencia de direccion estable.
        Transform referenciaDireccion = camaraPrincipal.transform;

        // Si la camara cuelga de un rig padre, usamos ese padre para evitar ruido visual o offsets locales.
        if (camaraPrincipal.transform.parent != null)
        {
            // Guardamos el padre como referencia de orientacion principal.
            referenciaDireccion = camaraPrincipal.transform.parent;
        }

        // Tomamos la direccion hacia adelante de la referencia elegida.
        Vector3 adelanteCamara = referenciaDireccion.forward;

        // Tomamos la direccion hacia la derecha de la misma referencia.
        Vector3 derechaCamara = referenciaDireccion.right;

        // Quitamos la inclinacion vertical para que el jugador no intente subir o bajar por mirar arriba o abajo.
        adelanteCamara.y = 0f;

        // Quitamos la componente vertical tambien de la derecha por prolijidad.
        derechaCamara.y = 0f;

        // Si la referencia quedo casi vertical y perdimos direccion util, usamos ejes globales como respaldo.
        if (adelanteCamara.sqrMagnitude < 0.001f || derechaCamara.sqrMagnitude < 0.001f)
        {
            // Devolvemos movimiento clasico para no dejar al jugador inmovil.
            return new Vector3(entradaHorizontal, 0f, entradaVertical);
        }

        // Normalizamos ambas direcciones para que tengan largo 1 y no alteren la velocidad.
        adelanteCamara.Normalize();
        derechaCamara.Normalize();

        // Combinamos teclado vertical y horizontal usando la orientacion actual de la camara.
        return adelanteCamara * entradaVertical + derechaCamara * entradaHorizontal;
    }

    // Esta funcion devuelve si el jugador esta intentando moverse en este frame.
    private bool HayInputDeMovimiento()
    {
        // Armamos un vector 2D solo con input para comprobar si realmente hay direccion.
        Vector2 entradaMovimiento = new Vector2(entradaHorizontal, entradaVertical);

        // Si la magnitud al cuadrado es suficientemente grande, consideramos que si hay movimiento.
        return entradaMovimiento.sqrMagnitude > 0.0001f;
    }

    // Esta funcion devuelve una posicion interpolada entre el ultimo y el proximo paso de fisica.
    public Vector3 ObtenerPosicionInterpoladaParaRender()
    {
        // Si no estamos en Play, devolvemos la posicion actual del transform por seguridad.
        if (!Application.isPlaying)
        {
            return transform.position;
        }

        // Si el paso fijo no es valido, devolvemos la posicion actual conocida.
        if (Time.fixedDeltaTime <= 0f)
        {
            return posicionFisicaActual;
        }

        // Calculamos cuanto avanzamos dentro del intervalo entre dos pasos de fisica.
        float alfaInterpolacion = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        // Interpolamos suavemente entre posicion fisica anterior y actual.
        return Vector3.Lerp(posicionFisicaAnterior, posicionFisicaActual, alfaInterpolacion);
    }

    // Esta funcion devuelve una posicion interpolada aplicando un offset local del jugador.
    public Vector3 ObtenerPosicionInterpoladaConOffsetLocal(Vector3 offsetLocal)
    {
        // Tomamos la posicion renderizada del jugador como base.
        Vector3 posicionBase = ObtenerPosicionInterpoladaParaRender();

        // Sumamos el offset local convertido a mundo segun la rotacion actual del jugador.
        return posicionBase + transform.rotation * offsetLocal;
    }
}
