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

    // Esta variable guarda el controlador de animacion del jugador para sincronizar velocidad y sprint.
    private ControladorAnimacionJugador controladorAnimacionJugador;

    // Esta referencia guarda el Animator visual para consultar si el personaje esta plantado durante el swing.
    private Animator animatorVisualJugador;

    // Esta referencia guarda el sistema de defensa para reflejar el bloqueo visual del parry.
    private SistemaDefensaEspada sistemaDefensaEspada;

    // Esta variable guarda cuanto quiere moverse el jugador en horizontal.
    private float entradaHorizontal;

    // Esta variable guarda cuanto quiere moverse el jugador en vertical.
    private float entradaVertical;

    // Esta variable indica si el jugador esta intentando correr en este frame.
    private bool quiereCorrer;

    // Esta variable indica si finalmente este frame se esta aplicando el sprint.
    private bool estaCorriendo;

    // Esta variable indica si el jugador toca el piso en este frame.
    private bool estaEnSuelo;

    // Esta variable indica si la animacion actual esta bloqueando movimiento por ataque o muerte.
    private bool estaPlantadoPorAnimacion;

    // Esta distancia se usa para comprobar el suelo bajo el jugador.
    [SerializeField] private float distanciaChequeoSuelo = 1.1f;

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

        // Buscamos el controlador de animacion en el mismo objeto para notificar velocidad y sprint.
        controladorAnimacionJugador = GetComponent<ControladorAnimacionJugador>();

        // Si no estaba en la raiz, lo buscamos en hijos porque el Animator visual vive en el modelo 3D.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        // Buscamos tambien el Animator visual del jugador para leer el bool Plantado.
        animatorVisualJugador = BuscarAnimatorJugador();

        // Buscamos el sistema de defensa para reflejar el estado de bloqueo en el Animator.
        sistemaDefensaEspada = GetComponent<SistemaDefensaEspada>();

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

        // Si el Animator visual se perdio por un reload, lo buscamos otra vez.
        if (animatorVisualJugador == null)
        {
            animatorVisualJugador = BuscarAnimatorJugador();
        }

        // Calculamos si la animacion actual esta plantando al personaje.
        estaPlantadoPorAnimacion = ObtenerEstadoPlantadoDesdeAnimacion();

        // Si el jugador esta plantado por un swing o una muerte, bloqueamos el input de locomocion.
        if (estaPlantadoPorAnimacion)
        {
            // Limpiamos entrada para que no quede un arrastre del frame anterior.
            entradaHorizontal = 0f;
            entradaVertical = 0f;
            quiereCorrer = false;
            estaCorriendo = false;

            // Seguimos actualizando suelo para no romper la logica visual.
            ActualizarEstadoSuelo();

            // Si existe controlador de animacion, le avisamos que la velocidad visual debe quedar en cero.
            if (controladorAnimacionJugador == null)
            {
                controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
            }

            if (controladorAnimacionJugador != null)
            {
                controladorAnimacionJugador.SincronizarMovimiento(0f, false);
                controladorAnimacionJugador.SincronizarEstadoSuelo(estaEnSuelo);
            }

            ActualizarParametrosDirectosAnimator(0f, false, estaEnSuelo, EstaBloqueandoVisualmente());

            // Salimos para no procesar giro, sprint ni locomocion este frame.
            return;
        }

        // Actualizamos si este frame realmente puede correr segun input y estamina.
        ActualizarEstadoSprint();

        // Actualizamos si el jugador sigue pisando suelo para informar al Animator.
        ActualizarEstadoSuelo();

        // Si el jugador se esta moviendo, giramos el personaje hacia la direccion de avance.
        ActualizarRotacionVisualSegunMovimiento();

        // Si se perdio la referencia del controlador de animacion, la recuperamos.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        // Si existe un controlador de animacion, le informamos si el jugador esta en suelo.
        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.SincronizarEstadoSuelo(estaEnSuelo);
        }

        ActualizarParametrosDirectosAnimatorDesdeMovimiento();
        ActualizarAnimatorPaladinDesdeMovimiento();
    }

    // Esta funcion se ejecuta a ritmo de fisica y es ideal para mover un Rigidbody.
    private void FixedUpdate()
    {
        // Si la animacion actual esta plantando al personaje, bloqueamos locomocion fisica.
        if (estaPlantadoPorAnimacion)
        {
            // Mantenemos la posicion actual para no interpolar arrastres visuales.
            posicionFisicaAnterior = posicionFisicaActual;
            posicionFisicaActual = cuerpoRigido.position;

            // Cortamos la velocidad horizontal por seguridad, pero respetamos la vertical.
            cuerpoRigido.velocity = new Vector3(0f, cuerpoRigido.velocity.y, 0f);

            // Si existe controlador de animacion, informamos que no hay velocidad de locomocion.
            if (controladorAnimacionJugador == null)
            {
                controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
            }

            if (controladorAnimacionJugador != null)
            {
                controladorAnimacionJugador.SincronizarMovimiento(0f, false);
                controladorAnimacionJugador.SincronizarEstadoSuelo(estaEnSuelo);
            }

            // No procesamos movimiento mientras dura el swing.
            return;
        }

        // Calculamos la direccion de movimiento tomando como referencia la camara si existe.
        Vector3 direccionMovimiento = ObtenerDireccionMovimiento();

        // Si se aprietan dos teclas al mismo tiempo, normalizamos para que no corra mas rapido en diagonal.
        float intensidadMovimiento = Mathf.Clamp01(direccionMovimiento.magnitude);

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

        // Calculamos una velocidad maxima de referencia para normalizar la locomocion visual entre 0 y 1.
        float velocidadMaximaReferencia = velocidadMovimiento * multiplicadorSprint;

        // Si el jugador tiene estadisticas, usamos su velocidad real como nueva base de referencia.
        if (estadisticasJugador != null)
        {
            velocidadMaximaReferencia = Mathf.Max(velocidadMaximaReferencia, estadisticasJugador.VelocidadActual * multiplicadorSprint);
        }

        // Calculamos la velocidad visual normalizada para que el Animator responda de forma consistente.
        float velocidadVisualAnimacion = velocidadMaximaReferencia > 0.001f
            ? Mathf.Clamp01((intensidadMovimiento * velocidadFinal) / velocidadMaximaReferencia)
            : 0f;

        // Guardamos la posicion fisica anterior para poder interpolar luego en render.
        posicionFisicaAnterior = posicionFisicaActual;

        // Guardamos la posicion fisica actual esperada de este paso.
        posicionFisicaActual = posicionDestino;

        // Movemos el Rigidbody usando fisica en vez de mover el Transform a mano.
        cuerpoRigido.MovePosition(posicionDestino);

        // Si existe controlador de animacion, le pasamos la velocidad y el estado de sprint actual.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }

        // Si existe controlador de animacion, le pasamos la velocidad y el estado de sprint actual.
        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.SincronizarMovimiento(velocidadVisualAnimacion, estaCorriendo);
            controladorAnimacionJugador.SincronizarEstadoSuelo(estaEnSuelo);
        }
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

    // Esta funcion comprueba con un raycast si el jugador toca suelo.
    private void ActualizarEstadoSuelo()
    {
        // Elegimos un origen apenas por encima de la base del jugador para evitar falsos negativos.
        Vector3 origenChequeoSuelo = transform.position + Vector3.up * 0.1f;

        // Lanzamos un raycast corto hacia abajo para saber si hay piso debajo.
        estaEnSuelo = Physics.Raycast(origenChequeoSuelo, Vector3.down, distanciaChequeoSuelo, ~0, QueryTriggerInteraction.Ignore);
    }

    // Esta funcion gira suavemente al jugador hacia la direccion real en la que se mueve.
    private void ActualizarRotacionVisualSegunMovimiento()
    {
        // Calculamos la direccion de movimiento usando el mismo criterio que usa la locomocion.
        Vector3 direccionMovimiento = ObtenerDireccionMovimiento();

        // Quitamos cualquier componente vertical para no inclinar el personaje.
        direccionMovimiento.y = 0f;

        // Si casi no hay movimiento, no cambiamos la orientacion.
        if (direccionMovimiento.magnitude <= 0.1f)
        {
            return;
        }

        // Calculamos una rotacion que mire hacia la direccion de avance actual.
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento.normalized);

        // Giramos con suavidad para que no se vea brusco.
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 12f);
    }

    // Este metodo empuja parametros directos al Animator nuevo del paladin sin depender solo del puente visual.
    private void ActualizarParametrosDirectosAnimatorDesdeMovimiento()
    {
        if (animatorVisualJugador == null)
        {
            return;
        }

        float velocidadMaximaReferencia = velocidadMovimiento * multiplicadorSprint;
        if (estadisticasJugador != null)
        {
            velocidadMaximaReferencia = Mathf.Max(velocidadMaximaReferencia, estadisticasJugador.VelocidadActual * multiplicadorSprint);
        }

        float velocidadHorizontal = cuerpoRigido != null
            ? new Vector3(cuerpoRigido.velocity.x, 0f, cuerpoRigido.velocity.z).magnitude
            : 0f;

        float speedNorm = velocidadMaximaReferencia > 0.001f
            ? Mathf.Clamp01(velocidadHorizontal / velocidadMaximaReferencia)
            : 0f;

        ActualizarParametrosDirectosAnimator(speedNorm, estaCorriendo, estaEnSuelo, EstaBloqueandoVisualmente());
    }

    // Este metodo empuja al PaladinAnimator los parametros Speed e IsSprinting usando las variables reales del movimiento.
    private void ActualizarAnimatorPaladinDesdeMovimiento()
    {
        if (animatorVisualJugador == null)
        {
            animatorVisualJugador = BuscarAnimatorJugador();
        }

        if (animatorVisualJugador == null || cuerpoRigido == null)
        {
            return;
        }

        float velocidadMaxima = velocidadMovimiento * multiplicadorSprint;
        if (estadisticasJugador != null)
        {
            velocidadMaxima = Mathf.Max(velocidadMaxima, estadisticasJugador.VelocidadActual * multiplicadorSprint);
        }

        if (velocidadMaxima <= 0.001f)
        {
            velocidadMaxima = 1f;
        }

        float vel = new Vector3(cuerpoRigido.velocity.x, 0f, cuerpoRigido.velocity.z).magnitude;
        if (AnimatorTieneParametro("Speed", AnimatorControllerParameterType.Float))
        {
            animatorVisualJugador.SetFloat("Speed", Mathf.Clamp01(vel / velocidadMaxima), 0.1f, Time.deltaTime);
        }

        if (AnimatorTieneParametro("IsSprinting", AnimatorControllerParameterType.Bool))
        {
            animatorVisualJugador.SetBool("IsSprinting", estaCorriendo);
        }

        Vector3 dir = new Vector3(cuerpoRigido.velocity.x, 0f, cuerpoRigido.velocity.z);
        if (dir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                Time.deltaTime * 10f);
        }
    }

    // Este metodo setea Speed, sprint, suelo y bloqueo solo si el Animator expone esos parametros.
    private void ActualizarParametrosDirectosAnimator(float speedNorm, bool sprint, bool grounded, bool blocking)
    {
        if (animatorVisualJugador == null)
        {
            return;
        }

        EstablecerFloatAnimatorSiExiste("Speed", speedNorm);
        EstablecerBoolAnimatorSiExiste("IsSprinting", sprint);
        EstablecerBoolAnimatorSiExiste("IsGrounded", grounded);
        EstablecerBoolAnimatorSiExiste("IsBlocking", blocking);
    }

    // Este metodo deriva un bloqueo visual corto desde el sistema real de parry.
    private bool EstaBloqueandoVisualmente()
    {
        if (sistemaDefensaEspada == null)
        {
            sistemaDefensaEspada = GetComponent<SistemaDefensaEspada>();
        }

        if (sistemaDefensaEspada == null)
        {
            return false;
        }

        return sistemaDefensaEspada.ParryEnCurso || sistemaDefensaEspada.VentanaActiva;
    }

    // Este metodo setea un float del Animator solo si el parametro existe y coincide en tipo.
    private void EstablecerFloatAnimatorSiExiste(string nombreParametro, float valor)
    {
        if (!AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Float))
        {
            return;
        }

        animatorVisualJugador.SetFloat(nombreParametro, valor, 0.1f, Time.deltaTime);
    }

    // Este metodo setea un bool del Animator solo si el parametro existe y coincide en tipo.
    private void EstablecerBoolAnimatorSiExiste(string nombreParametro, bool valor)
    {
        if (!AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animatorVisualJugador.SetBool(nombreParametro, valor);
    }

    // Este metodo comprueba si el Animator visual actual tiene un parametro dado.
    private bool AnimatorTieneParametro(string nombreParametro, AnimatorControllerParameterType tipoEsperado)
    {
        if (animatorVisualJugador == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parametros = animatorVisualJugador.parameters;
        for (int i = 0; i < parametros.Length; i++)
        {
            if (parametros[i].name == nombreParametro)
            {
                return parametros[i].type == tipoEsperado;
            }
        }

        return false;
    }

    // Este metodo prioriza el Animator del hijo ModeloJugador para evitar agarrar un Animator viejo o incorrecto.
    private Animator BuscarAnimatorJugador()
    {
        Transform modeloJugador = transform.Find("ModeloJugador");
        if (modeloJugador != null)
        {
            Animator animatorModelo = modeloJugador.GetComponent<Animator>();
            if (animatorModelo != null)
            {
                return animatorModelo;
            }

            animatorModelo = modeloJugador.GetComponentInChildren<Animator>(true);
            if (animatorModelo != null)
            {
                return animatorModelo;
            }
        }

        return GetComponentInChildren<Animator>(true);
    }

    // Este metodo consulta si el Animator visual actual tiene activo el bool Plantado.
    private bool ObtenerEstadoPlantadoDesdeAnimacion()
    {
        // Si falta Animator, asumimos que no esta plantado.
        if (animatorVisualJugador == null)
        {
            return false;
        }

        // Recorremos los parametros reales del Animator para verificar que exista el bool.
        AnimatorControllerParameter[] parametros = animatorVisualJugador.parameters;
        for (int indiceParametro = 0; indiceParametro < parametros.Length; indiceParametro++)
        {
            if (parametros[indiceParametro].name != "Plantado")
            {
                continue;
            }

            if (parametros[indiceParametro].type != AnimatorControllerParameterType.Bool)
            {
                return false;
            }

            return animatorVisualJugador.GetBool("Plantado");
        }

        // Si el parametro no existe, consideramos que no esta plantado.
        return false;
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
