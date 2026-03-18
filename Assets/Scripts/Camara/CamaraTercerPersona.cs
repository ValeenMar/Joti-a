using UnityEngine;
using UnityEngine.SceneManagement;

// Esta clase controla una camara de tercera persona con seguimiento suave y prevencion de colisiones.
[DisallowMultipleComponent]
public class CamaraTercerPersona : MonoBehaviour
{
    // Este transform es el objetivo que la camara debe seguir (idealmente el anclaje del jugador).
    [SerializeField] private Transform objetivoSeguimiento;

    // Esta opcion resetea la camara hija al centro del rig para evitar offsets raros al parentarla.
    [SerializeField] private bool alinearCamaraHijaAutomaticamente = true;

    // Este valor mueve fisicamente la camara hacia un costado para lograr sensacion de hombro.
    [SerializeField] private float desplazamientoLateralCamara = 0.9f;

    // Este valor mueve levemente el punto al que mira la camara para que el personaje no quede centrado del todo.
    [SerializeField] private float desplazamientoLateralFoco = 0.35f;

    // Esta tecla permite cambiar manualmente entre hombro derecho e izquierdo.
    [SerializeField] private KeyCode teclaCambiarHombro = KeyCode.Q;

    // Esta opcion define si el juego arranca mostrando el hombro derecho.
    [SerializeField] private bool empezarEnHombroDerecho = true;

    // Esta velocidad suaviza el cambio entre hombros para que no sea un salto brusco.
    [SerializeField] private float velocidadCambioHombro = 6f;

    // Esta altura adicional sube la camara por encima del anclaje del jugador.
    [SerializeField] private float alturaCamaraAdicional = 0.45f;

    // Esta altura adicional sube el punto de foco para mirar un poco por encima del torso.
    [SerializeField] private float alturaFocoAdicional = 0.18f;

    // Esta capa define contra que objetos se evaluara la colision de camara.
    [SerializeField] private LayerMask mascaraColision = ~0;

    // Esta distancia es la separacion deseada entre camara y objetivo cuando no hay obstaculos.
    [SerializeField] private float distanciaDeseada = 4.5f;

    // Esta distancia extra fija empuja un poco mas la camara hacia atras para darle mas aire al jugador.
    [SerializeField] private float distanciaExtraBase = 1.25f;

    // Esta distancia extra se suma cuando hay enemigos cerca y la camara abre el encuadre.
    [SerializeField] private float distanciaExtraPorAmenaza = 1.25f;

    // Esta distancia define a partir de que cercania de enemigos empieza a abrirse el zoom dinamico.
    [SerializeField] private float rangoAmenazaZoom = 6f;

    // Esta velocidad suaviza el cambio del zoom dinamico para que no se note brusco.
    [SerializeField] private float suavizadoZoom = 8f;

    // Esta distancia minima evita que la camara se pegue demasiado al jugador.
    [SerializeField] private float distanciaMinima = 1.1f;

    // Este radio se usa en SphereCast para evitar atravesar paredes y piso.
    [SerializeField] private float radioColision = 0.25f;

    // Este margen separa la camara un poquito del obstaculo para evitar clipping visual.
    [SerializeField] private float margenSeparacion = 0.1f;

    // Esta velocidad suaviza la posicion para que la camara no se mueva a saltos.
    [SerializeField] private float suavizadoPosicion = 0.08f;

    // Esta opcion define si queremos mantener suavizado extra aun siguiendo a un objetivo controlado por fisica.
    [SerializeField] private bool usarSuavizadoPosicionConObjetivoFisico = false;

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

    // Este valor asegura que la camara mantenga siempre un leve angulo hacia abajo.
    [SerializeField] private float anguloVerticalMinimoPersistente = 12f;

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

    // Esta referencia guarda el script de movimiento del jugador seguido para usar su posicion renderizada.
    private MovimientoJugador movimientoJugadorObjetivo;

    // Esta referencia guarda la raiz real del objetivo de seguimiento.
    private Transform raizObjetivoSeguimiento;

    // Esta variable evita que el primer frame de camara arranque desde una posicion vieja o incorrecta.
    private bool posicionInicialAplicada;

    // Esta variable guarda a que hombro queremos llegar: 1 para derecho y -1 para izquierdo.
    private float direccionHombroObjetivo;

    // Esta variable guarda el valor suavizado actual del hombro.
    private float direccionHombroSuavizada;

    // Esta variable guarda la distancia suavizada que usa la camara en tiempo real.
    private float distanciaDeseadaSuavizada;

    // Esta funcion se ejecuta al iniciar para preparar referencias.
    private void Awake()
    {
        // Guardamos la referencia de la camara de Unity para uso opcional futuro.
        camaraUnity = GetComponent<Camera>();

        // Si este script esta en el rig y no en la camara, buscamos una camara hija.
        if (camaraUnity == null)
        {
            // Tomamos la primera camara hija encontrada dentro del rig.
            camaraUnity = GetComponentInChildren<Camera>();
        }

        // Si no se asigno objetivo en Inspector, intentamos resolver uno automaticamente.
        if (objetivoSeguimiento == null)
        {
            IntentarAsignarObjetivoAutomatico();
        }

        // Si hay una camara hija y queremos alinearla, reseteamos su transform local para que no meta offsets raros.
        if (alinearCamaraHijaAutomaticamente && camaraUnity != null && camaraUnity.transform != transform)
        {
            // Colocamos la camara hija exactamente en el centro del rig.
            camaraUnity.transform.localPosition = Vector3.zero;

            // Dejamos la rotacion local en cero para que el rig controle completamente la orientacion.
            camaraUnity.transform.localRotation = Quaternion.identity;
        }

        // Inicializamos angulos segun la rotacion actual para evitar saltos al entrar en Play.
        Vector3 angulosActuales = transform.eulerAngles;
        anguloHorizontal = angulosActuales.y;
        anguloVertical = angulosActuales.x;

        // Guardamos posicion actual como base de suavizado inicial.
        posicionSuavizada = transform.position;

        // Guardamos la distancia actual como punto de partida del zoom dinamico.
        distanciaDeseadaSuavizada = distanciaDeseada + distanciaExtraBase;

        // Elegimos el hombro inicial segun la configuracion del Inspector.
        direccionHombroObjetivo = empezarEnHombroDerecho ? 1f : -1f;

        // Arrancamos ya en ese mismo hombro para evitar un deslizamiento inicial innecesario.
        direccionHombroSuavizada = direccionHombroObjetivo;

        // Actualizamos referencias del objetivo seguido para saber si estamos siguiendo un Rigidbody del jugador.
        ActualizarReferenciasObjetivo();

        // Intentamos aplicar la pose instantanea desde el arranque para evitar cualquier barrido inicial.
        ForzarReposicionInstantanea();
    }

    // Esta funcion se ejecuta al habilitar el componente y se usa para detectar recargas de escena.
    private void OnEnable()
    {
        // Escuchamos la carga de escena para reposicionar instantaneamente la camara tras un reinicio.
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // Esta funcion se ejecuta al deshabilitar el componente y limpia la suscripcion de escena.
    private void OnDisable()
    {
        // Dejamos de escuchar la carga de escena para evitar referencias colgadas.
        SceneManager.sceneLoaded -= AlCargarEscena;
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

        // Si el objetivo cambio o sus referencias aun no estaban preparadas, las resolvemos ahora.
        if (raizObjetivoSeguimiento == null || objetivoSeguimiento.root != raizObjetivoSeguimiento)
        {
            ActualizarReferenciasObjetivo();
        }

        // Mantenemos un angulo minimo de inclinacion para que la camara no quede nunca demasiado plana.
        anguloVertical = Mathf.Clamp(anguloVertical, Mathf.Max(limiteMinimoVertical, anguloVerticalMinimoPersistente), limiteMaximoVertical);

        // NOTA MIRROR: este bloque de input debe correr solo en el cliente local que controla la camara.
        // En el futuro con Mirror, normalmente esto iria ligado al LocalPlayer (hasAuthority/isLocalPlayer).
        float entradaMouseX = Input.GetAxis("Mouse X");
        float entradaMouseY = Input.GetAxis("Mouse Y");

        // Si se apreta la tecla configurada, alternamos entre hombro derecho e izquierdo.
        if (Input.GetKeyDown(teclaCambiarHombro))
        {
            // Invertimos el valor objetivo para cambiar de lado.
            direccionHombroObjetivo *= -1f;
        }

        // Acumulamos rotacion horizontal segun movimiento del mouse y sensibilidad.
        anguloHorizontal += entradaMouseX * sensibilidadMouseX * Time.deltaTime;

        // Acumulamos rotacion vertical invertida para comportamiento clasico de camara tercera persona.
        anguloVertical -= entradaMouseY * sensibilidadMouseY * Time.deltaTime;

        // Limitamos rotacion vertical para no atravesar angulos incomodos.
        anguloVertical = Mathf.Clamp(anguloVertical, Mathf.Max(limiteMinimoVertical, anguloVerticalMinimoPersistente), limiteMaximoVertical);

        // Construimos la rotacion deseada en base a los angulos calculados.
        Quaternion rotacionDeseada = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);

        // Calculamos el zoom dinamico segun la distancia del enemigo mas cercano.
        float distanciaObjetivoDinamica = CalcularDistanciaDeseadaDinamica();

        // Suavizamos el cambio del zoom para que no salte de golpe.
        distanciaDeseadaSuavizada = Mathf.Lerp(distanciaDeseadaSuavizada, distanciaObjetivoDinamica, Time.deltaTime * suavizadoZoom);

        // Suavizamos el cambio de hombro para que el pasaje de un lado al otro sea agradable.
        direccionHombroSuavizada = Mathf.MoveTowards(direccionHombroSuavizada, direccionHombroObjetivo, velocidadCambioHombro * Time.deltaTime);

        // Calculamos un offset lateral usando el eje derecho de la rotacion actual.
        Vector3 offsetLateral = rotacionDeseada * Vector3.right * desplazamientoLateralCamara * direccionHombroSuavizada;

        // Guardamos el punto foco base usando una posicion de render estable del objetivo seguido.
        Vector3 puntoFocoBase = ObtenerPuntoSeguimientoBase();

        // Desplazamos un poco el foco hacia el mismo hombro para que el personaje no quede pegado al centro.
        Vector3 puntoFoco = puntoFocoBase + Vector3.up * alturaFocoAdicional + rotacionDeseada * Vector3.right * desplazamientoLateralFoco * direccionHombroSuavizada;

        // Calculamos la posicion ideal sin obstaculos, sumando el offset lateral de hombro.
        Vector3 posicionIdeal = puntoFocoBase + Vector3.up * alturaCamaraAdicional - rotacionDeseada * Vector3.forward * distanciaDeseadaSuavizada + offsetLateral;

        // Ajustamos la posicion ideal en caso de colision con piso o paredes.
        Vector3 posicionAjustada = AjustarPosicionPorColision(puntoFoco, posicionIdeal);

        // En el primer frame valido ubicamos la camara directamente donde corresponde para evitar arranques raros.
        if (!posicionInicialAplicada)
        {
            // Guardamos la posicion correcta como base interna de suavizado.
            posicionSuavizada = posicionAjustada;

            // Aplicamos la posicion correcta inmediatamente.
            transform.position = posicionAjustada;

            // Calculamos la rotacion mirando al foco desde la posicion correcta.
            Quaternion rotacionInicial = Quaternion.LookRotation((puntoFoco - transform.position).normalized, Vector3.up);

            // Aplicamos la rotacion correcta inmediatamente.
            transform.rotation = rotacionInicial;

            // Marcamos que la camara ya quedo sincronizada con el objetivo.
            posicionInicialAplicada = true;

            // Terminamos este frame porque ya colocamos la camara de forma definitiva.
            return;
        }

        // Si seguimos a un jugador movido por fisica, por defecto evitamos una segunda capa de suavizado.
        if (movimientoJugadorObjetivo != null && !usarSuavizadoPosicionConObjetivoFisico)
        {
            // Igualamos la posicion suavizada a la ajustada para mantener al jugador estable en pantalla.
            posicionSuavizada = posicionAjustada;

            // Limpiamos la velocidad interna para que no arrastre inercia vieja.
            velocidadSuavizado = Vector3.zero;
        }
        else
        {
            // Si no es un objetivo fisico o queremos suavizado extra, usamos SmoothDamp como antes.
            posicionSuavizada = Vector3.SmoothDamp(posicionSuavizada, posicionAjustada, ref velocidadSuavizado, suavizadoPosicion);
        }

        // Aplicamos posicion final suavizada.
        transform.position = posicionSuavizada;

        // Calculamos la rotacion que mira al foco desde la posicion actual.
        Quaternion rotacionMirandoAlFoco = Quaternion.LookRotation((puntoFoco - transform.position).normalized, Vector3.up);

        // Suavizamos la rotacion para evitar micro-jitter.
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionMirandoAlFoco, suavizadoRotacion * Time.deltaTime);
    }

    // Esta funcion calcula la distancia dinamica segun la amenaza mas cercana.
    private float CalcularDistanciaDeseadaDinamica()
    {
        // Empezamos con la distancia base que ya tenia la camara mas un empuje extra fijo.
        float distanciaObjetivo = distanciaDeseada + distanciaExtraBase;

        // Si no hay enemigos activos, devolvemos la distancia base ampliada.
        VidaEnemigo[] enemigos = FindObjectsOfType<VidaEnemigo>();

        // Guardamos la distancia mas cercana encontrada.
        float distanciaMasCercana = float.MaxValue;

        // Recorremos la lista de enemigos para saber cual esta mas cerca de la camara/jugador.
        for (int indiceEnemigo = 0; indiceEnemigo < enemigos.Length; indiceEnemigo++)
        {
            // Si el enemigo no existe o ya murio, lo ignoramos.
            if (enemigos[indiceEnemigo] == null || !enemigos[indiceEnemigo].EstaVivo)
            {
                continue;
            }

            // Calculamos la distancia al enemigo mas cercano.
            float distanciaActual = Vector3.Distance(transform.position, enemigos[indiceEnemigo].transform.position);

            // Si esta distancia es la menor encontrada, la guardamos.
            if (distanciaActual < distanciaMasCercana)
            {
                distanciaMasCercana = distanciaActual;
            }
        }

        // Si no encontramos enemigos vivos, devolvemos la distancia base ampliada.
        if (distanciaMasCercana == float.MaxValue)
        {
            return distanciaObjetivo;
        }

        // Calculamos una amenaza de 0 a 1 segun la cercania del enemigo.
        float amenazaCercana = 1f - Mathf.Clamp01(distanciaMasCercana / Mathf.Max(0.01f, rangoAmenazaZoom));

        // Sumamos la apertura extra segun la amenaza.
        distanciaObjetivo += distanciaExtraPorAmenaza * amenazaCercana;

        // Devolvemos la distancia final dinamica.
        return distanciaObjetivo;
    }

    // Este metodo calcula una posicion segura usando SphereCast para no atravesar objetos.
    private Vector3 AjustarPosicionPorColision(Vector3 puntoOrigen, Vector3 posicionObjetivoSinColision)
    {
        // Calculamos direccion desde el foco hacia la posicion deseada de camara.
        Vector3 direccion = (posicionObjetivoSinColision - puntoOrigen).normalized;

        // Calculamos la distancia maxima que intentamos recorrer con el cast.
        float distanciaMaxima = Vector3.Distance(puntoOrigen, posicionObjetivoSinColision);

        // Guardamos la distancia del obstaculo valido mas cercano.
        float distanciaGolpeMasCercano = float.MaxValue;

        // Esta bandera indica si realmente encontramos un obstaculo valido.
        bool encontroObstaculoValido = false;

        // Ejecutamos SphereCastAll para poder ignorar los colliders del propio jugador seguido.
        RaycastHit[] golpes = Physics.SphereCastAll(puntoOrigen, radioColision, direccion, distanciaMaxima, mascaraColision, QueryTriggerInteraction.Ignore);

        // Recorremos todos los impactos encontrados.
        for (int indiceGolpe = 0; indiceGolpe < golpes.Length; indiceGolpe++)
        {
            // Si el golpe pertenece al jugador que seguimos o a alguno de sus hijos, lo ignoramos.
            if (objetivoSeguimiento != null)
            {
                // Guardamos la raiz del objetivo para comparar sin importar si seguimos un anclaje hijo.
                Transform raizObjetivo = objetivoSeguimiento.root;

                // Si el collider golpeado pertenece al mismo jugador, seguimos buscando otro obstaculo.
                if (golpes[indiceGolpe].transform == raizObjetivo || golpes[indiceGolpe].transform.IsChildOf(raizObjetivo))
                {
                    continue;
                }
            }

            // Si este golpe esta mas cerca que los anteriores, lo guardamos como el mas importante.
            if (golpes[indiceGolpe].distance < distanciaGolpeMasCercano)
            {
                // Guardamos la nueva distancia util mas corta.
                distanciaGolpeMasCercano = golpes[indiceGolpe].distance;

                // Marcamos que ya encontramos un obstaculo que si hay que respetar.
                encontroObstaculoValido = true;
            }
        }

        // Si encontramos un obstaculo real, reducimos la distancia para quedar delante de el.
        if (encontroObstaculoValido)
        {
            // Calculamos una distancia segura sin pegarnos demasiado ni atravesar el obstaculo.
            float distanciaSegura = Mathf.Max(distanciaMinima, distanciaGolpeMasCercano - margenSeparacion);

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
            ActualizarReferenciasObjetivo();
            ForzarReposicionInstantanea();
            return;
        }

        // Si no tiene anclaje, seguimos directamente el transform del jugador.
        objetivoSeguimiento = posibleJugador.transform;
        ActualizarReferenciasObjetivo();
        ForzarReposicionInstantanea();
    }

    // Esta funcion se ejecuta cuando una escena termina de cargarse.
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Si no estamos en Play, no hacemos nada.
        if (!Application.isPlaying)
        {
            return;
        }

        // Reiniciamos el estado interno para evitar que la camara arrastre la pose anterior.
        ReiniciarEstadoInstantaneo();

        // Intentamos reasignar objetivo y aplicar la pose inmediata lo antes posible.
        if (objetivoSeguimiento == null)
        {
            IntentarAsignarObjetivoAutomatico();
        }

        // Si ya tenemos objetivo, forzamos la pose inicial sin esperar al primer sweep.
        ForzarReposicionInstantanea();
    }

    // Este metodo permite asignar el objetivo manualmente desde otros scripts.
    public void AsignarObjetivo(Transform nuevoObjetivo)
    {
        // Guardamos el nuevo objetivo de seguimiento.
        objetivoSeguimiento = nuevoObjetivo;

        // Actualizamos referencias internas para que el seguimiento no quede apuntando a datos viejos.
        ActualizarReferenciasObjetivo();

        // Reposicionamos instantaneamente para evitar barridos al cambiar de objetivo.
        ForzarReposicionInstantanea();
    }

    // Este metodo publico permite cambiar de hombro desde otros scripts si algun dia lo necesitamos.
    public void CambiarHombro()
    {
        // Invertimos el hombro objetivo actual.
        direccionHombroObjetivo *= -1f;
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

    // Este metodo actualiza referencias internas del objetivo para saber si seguimos un jugador fisico.
    private void ActualizarReferenciasObjetivo()
    {
        // Si no hay objetivo valido, limpiamos referencias y salimos.
        if (objetivoSeguimiento == null)
        {
            raizObjetivoSeguimiento = null;
            movimientoJugadorObjetivo = null;
            return;
        }

        // Guardamos la raiz real del objetivo para comparar y buscar componentes del jugador.
        raizObjetivoSeguimiento = objetivoSeguimiento.root;

        // Si no hay raiz valida, limpiamos referencia de movimiento y salimos.
        if (raizObjetivoSeguimiento == null)
        {
            movimientoJugadorObjetivo = null;
            return;
        }

        // Intentamos obtener el script de movimiento del jugador seguido.
        movimientoJugadorObjetivo = raizObjetivoSeguimiento.GetComponent<MovimientoJugador>();
    }

    // Esta funcion resetea el estado visual de la camara para un inicio limpio.
    private void ReiniciarEstadoInstantaneo()
    {
        // Marcamos que todavia no aplicamos la pose inicial.
        posicionInicialAplicada = false;

        // Limpiamos la velocidad interna del suavizado para que no arrastre inercia vieja.
        velocidadSuavizado = Vector3.zero;

        // Reiniciamos la posicion suavizada a la posicion actual del rig.
        posicionSuavizada = transform.position;

        // Recentramos el hombro para evitar cambios bruscos al volver a cargar.
        direccionHombroSuavizada = direccionHombroObjetivo;
    }

    // Esta funcion intenta poner la camara exactamente en la posicion correcta en el mismo frame.
    private void ForzarReposicionInstantanea()
    {
        // Si no hay objetivo valido, no podemos reposicionar todavia.
        if (objetivoSeguimiento == null)
        {
            return;
        }

        // Si el objetivo cambio o sus referencias aun no estaban preparadas, las resolvemos ahora.
        if (raizObjetivoSeguimiento == null || objetivoSeguimiento.root != raizObjetivoSeguimiento)
        {
            ActualizarReferenciasObjetivo();
        }

        // Si aun no hay objetivo raiz, salimos por seguridad.
        if (raizObjetivoSeguimiento == null)
        {
            return;
        }

        // Tomamos los angulos actuales como base para el reposicionamiento inmediato.
        Vector3 angulosActuales = transform.eulerAngles;
        anguloHorizontal = angulosActuales.y;
        anguloVertical = Mathf.Clamp(Mathf.Max(angulosActuales.x, anguloVerticalMinimoPersistente), Mathf.Max(limiteMinimoVertical, anguloVerticalMinimoPersistente), limiteMaximoVertical);

        // Calculamos la rotacion deseada con los angulos actuales.
        Quaternion rotacionDeseada = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);

        // Calculamos la distancia dinamica de este frame.
        distanciaDeseadaSuavizada = CalcularDistanciaDeseadaDinamica();

        // Recentramos el hombro suavizado para que no arrastre una curva vieja.
        direccionHombroSuavizada = direccionHombroObjetivo;

        // Calculamos el offset lateral usando el eje derecho de la rotacion actual.
        Vector3 offsetLateral = rotacionDeseada * Vector3.right * desplazamientoLateralCamara * direccionHombroSuavizada;

        // Obtenemos la base de seguimiento estable del jugador.
        Vector3 puntoFocoBase = ObtenerPuntoSeguimientoBase();

        // Calculamos el punto que la camara va a mirar.
        Vector3 puntoFoco = puntoFocoBase + Vector3.up * alturaFocoAdicional + rotacionDeseada * Vector3.right * desplazamientoLateralFoco * direccionHombroSuavizada;

        // Calculamos la posicion ideal ya con altura y distancia nuevas.
        Vector3 posicionIdeal = puntoFocoBase + Vector3.up * alturaCamaraAdicional - rotacionDeseada * Vector3.forward * distanciaDeseadaSuavizada + offsetLateral;

        // Ajustamos la posicion por colision para no atravesar paredes ni piso.
        Vector3 posicionAjustada = AjustarPosicionPorColision(puntoFoco, posicionIdeal);

        // Aplicamos la posicion directamente para evitar cualquier barrido visual.
        transform.position = posicionAjustada;

        // Aplicamos la rotacion directa mirando al foco.
        transform.rotation = Quaternion.LookRotation((puntoFoco - transform.position).normalized, Vector3.up);

        // Sincronizamos los valores internos para que el siguiente frame arranque limpio.
        posicionSuavizada = posicionAjustada;
        velocidadSuavizado = Vector3.zero;
        posicionInicialAplicada = true;
    }

    // Este metodo devuelve un punto de seguimiento estable para render.
    private Vector3 ObtenerPuntoSeguimientoBase()
    {
        // Si no hay objetivo valido, usamos la posicion actual del rig como respaldo.
        if (objetivoSeguimiento == null)
        {
            return transform.position;
        }

        // Si no estamos siguiendo a un MovimientoJugador, usamos la posicion del objetivo como siempre.
        if (movimientoJugadorObjetivo == null || raizObjetivoSeguimiento == null)
        {
            return objetivoSeguimiento.position;
        }

        // Si seguimos exactamente la raiz del jugador, pedimos su posicion interpolada para render.
        if (objetivoSeguimiento == raizObjetivoSeguimiento)
        {
            return movimientoJugadorObjetivo.ObtenerPosicionInterpoladaParaRender();
        }

        // Si seguimos un hijo del jugador, convertimos su offset local a mundo usando la posicion interpolada del jugador.
        if (objetivoSeguimiento.IsChildOf(raizObjetivoSeguimiento))
        {
            Vector3 offsetLocalHastaObjetivo = raizObjetivoSeguimiento.InverseTransformPoint(objetivoSeguimiento.position);
            return movimientoJugadorObjetivo.ObtenerPosicionInterpoladaConOffsetLocal(offsetLocalHastaObjetivo);
        }

        // Si no encaja en ningun caso anterior, devolvemos la posicion del objetivo directamente.
        return objetivoSeguimiento.position;
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
