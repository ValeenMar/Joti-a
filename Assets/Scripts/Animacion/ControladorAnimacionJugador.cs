using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script conecta la logica real del jugador con el Animator visual
// del modelo 3D para locomocion, ataques, dano y muerte.

// Esta clase controla parametros y triggers del Animator del jugador.
[RequireComponent(typeof(Animator))]
public class ControladorAnimacionJugador : MonoBehaviour
{
    // Estos nombres son los parametros nuevos pedidos para el Animator Controller.
    private const string ParametroVelocidadIngles = "Speed";
    private const string ParametroSprintIngles = "IsSprinting";
    private const string ParametroSueloIngles = "IsGrounded";
    private const string ParametroPlantadoIngles = "Plantado";
    private const string TriggerAtaqueIngles = "Attack";
    private const string TriggerAtaqueNormalIngles = "AttackNormal";
    private const string TriggerAtaqueFuerteIngles = "AttackFuerte";
    private const string TriggerParryIngles = "Parry";
    private const string TriggerGolpeIngles = "Hit";
    private const string TriggerMuerteIngles = "Die";

    // Estos nombres son compatibilidad con controladores anteriores del proyecto.
    private const string ParametroVelocidadEspanol = "Velocidad";
    private const string ParametroSprintEspanol = "Corriendo";
    private const string ParametroSueloEspanol = "EnSuelo";
    private const string TriggerAtaqueNormalEspanol = "AtaqueNormal";
    private const string TriggerAtaqueFuerteEspanol = "AtaqueFuerte";
    private const string TriggerGolpeEspanol = "RecibirDanio";
    private const string TriggerMuerteEspanol = "Morir";

    // Esta referencia apunta al Animator visual principal del jugador.
    [SerializeField] private Animator animatorJugador;

    // Esta referencia guarda el Rigidbody por si en el futuro queremos fallback visual.
    [SerializeField] private Rigidbody cuerpoRigido;

    // Esta referencia apunta a la espada para reenviar Animation Events del controller.
    [SerializeField] private SistemaEspada sistemaEspada;

    // Esta variable guarda la velocidad visual actual normalizada.
    [SerializeField] private float velocidadMovimientoActual;

    // Esta variable indica si el jugador esta corriendo.
    [SerializeField] private bool corriendoActual;

    // Esta variable indica si el jugador toca suelo.
    [SerializeField] private bool enSueloActual = true;

    // Esta opcion permite corregir el brazo derecho durante locomocion y parry para que la pose de espada se vea mas natural.
    [SerializeField] private bool corregirBrazoDerechoEnIdle = true;

    // Estas poses son fijas para evitar temblores por ajustes continuos durante locomocion.
    // El socket visual se deja en la orientacion definida por el setup para que no se pelee con la mano.
    private static readonly Vector3 PoseHombroDerechoLocomocion = new Vector3(0f, -18f, -28f);
    private static readonly Vector3 PoseBrazoDerechoLocomocion = new Vector3(-16f, -52f, -104f);
    private static readonly Vector3 PoseAntebrazoDerechoLocomocion = new Vector3(26f, -10f, 54f);
    private static readonly Vector3 PoseManoDerechaLocomocion = new Vector3(8f, -18f, -20f);
    private static readonly Vector3 PoseSocketEspadaLocomocion = new Vector3(156f, -90f, -94f);

    // Esta pose corta hace que el parry se vea defensivo aunque todavia no tengamos un clip dedicado.
    private static readonly Vector3 PoseHombroDerechoParry = new Vector3(-8f, -22f, -36f);
    private static readonly Vector3 PoseBrazoDerechoParry = new Vector3(4f, -34f, -76f);
    private static readonly Vector3 PoseAntebrazoDerechoParry = new Vector3(18f, -8f, 92f);
    private static readonly Vector3 PoseManoDerechaParry = new Vector3(18f, -34f, -42f);
    private static readonly Vector3 PoseSocketEspadaParry = new Vector3(18f, -90f, -98f);

    // Esta variable evita repetir busquedas cuando ya tenemos referencias listas.
    private bool referenciasListas;

    // Esta referencia guarda el hueso de brazo derecho para retocar su pose en Idle.
    private Transform huesoBrazoDerecho;

    // Esta referencia guarda el hueso del antebrazo derecho para bajar mejor la espada en locomocion.
    private Transform huesoAntebrazoDerecho;

    // Esta referencia guarda el hueso del hombro derecho para ayudar a bajar la pose general del brazo.
    private Transform huesoHombroDerecho;

    // Esta referencia guarda el hueso de la mano derecha para orientar mejor la espada en reposo.
    private Transform huesoManoDerecha;
    private Transform socketEspadaVisual;

    // Esta referencia guarda la rotacion base del brazo para restaurarla fuera de Idle.
    private Quaternion rotacionBaseBrazoDerecho = Quaternion.identity;

    // Esta referencia guarda la rotacion base del antebrazo para restaurarla fuera de locomocion.
    private Quaternion rotacionBaseAntebrazoDerecho = Quaternion.identity;

    // Esta referencia guarda la rotacion base del hombro derecho.
    private Quaternion rotacionBaseHombroDerecho = Quaternion.identity;

    // Esta referencia guarda la rotacion base de la mano derecha.
    private Quaternion rotacionBaseManoDerecha = Quaternion.identity;
    private Quaternion rotacionBaseSocketEspada = Quaternion.identity;

    // Esta variable recuerda si la pose idle ya tomo la rotacion base original.
    private bool poseBrazoInicializada;

    // Esta variable recuerda si el antebrazo ya guardo su rotacion base original.
    private bool poseAntebrazoInicializada;

    // Esta variable recuerda si el hombro derecho ya guardo su rotacion base original.
    private bool poseHombroInicializada;

    // Esta variable recuerda si la mano derecha ya guardo su rotacion base original.
    private bool poseManoInicializada;
    private bool poseSocketEspadaInicializada;

    // Esta bandera indica si estamos forzando una pose defensiva corta para el parry.
    private bool poseParryActiva;

    // Esta funcion se ejecuta al iniciar el objeto.
    private void Awake()
    {
        // Reintentamos enlazar todo desde el inicio.
        ReasignarReferencias();
    }

    // Esta funcion se ejecuta al reactivar el componente.
    private void OnEnable()
    {
        // Al reactivarse reintentamos por si hubo reinicio de escena.
        ReasignarReferencias();
    }

    // Este metodo restaura la pose base al desactivar el componente para no dejar residuos visuales.
    private void OnDisable()
    {
        RestaurarPoseBaseBrazoDerecho();
        poseParryActiva = false;
    }

    // Esta funcion corre al final del frame y empuja los valores al Animator.
    private void LateUpdate()
    {
        // Si todavia no tenemos referencias, reintentamos una vez.
        if (!referenciasListas)
        {
            ReasignarReferencias();
        }

        // Si no hay Animator valido, no hacemos nada mas.
        if (animatorJugador == null)
        {
            return;
        }

        // Aplicamos todos los parametros continuos de locomocion.
        AplicarParametrosContinuos();

        // Si la pose de locomocion necesita correccion, la aplicamos despues de que el Animator evaluo el frame.
        AplicarCorreccionBrazoDerechoEnIdle();
    }

    // Este metodo recibe velocidad y sprint desde el sistema de movimiento.
    public void SincronizarMovimiento(float velocidadVisual, bool estaCorriendo)
    {
        // Guardamos velocidad limpia entre 0 y 1.
        velocidadMovimientoActual = Mathf.Clamp01(velocidadVisual);

        // Guardamos el estado actual de sprint.
        corriendoActual = estaCorriendo;

        // Si el Animator ya esta listo, lo actualizamos al instante.
        if (animatorJugador != null)
        {
            AplicarParametrosContinuos();
        }
    }

    // Este metodo recibe el estado de suelo desde el sistema de movimiento.
    public void SincronizarEstadoSuelo(bool estaEnSuelo)
    {
        // Guardamos el dato para el siguiente LateUpdate o para aplicarlo ya.
        enSueloActual = estaEnSuelo;

        // Si el Animator ya esta listo, aplicamos el bool ahora mismo.
        if (animatorJugador != null)
        {
            AplicarParametrosContinuos();
        }
    }

    // Este metodo dispara la animacion de ataque normal.
    public void ReproducirAtaqueNormal()
    {
        // Reintentamos referencias antes de usar el Animator.
        ReasignarReferencias();

        // Si estabamos en pose de parry, la soltamos antes de atacar.
        poseParryActiva = false;

        // Marcamos al personaje como plantado durante el swing.
        EstablecerPlantado(true);

        // Disparamos el trigger nuevo y el legado para compatibilidad.
        DispararTriggerSiExiste(TriggerAtaqueIngles);
        DispararTriggerSiExiste(TriggerAtaqueNormalIngles);
        DispararTriggerSiExiste(TriggerAtaqueNormalEspanol);
    }

    // Este metodo dispara la animacion de ataque fuerte.
    public void ReproducirAtaqueFuerte()
    {
        // Reintentamos referencias antes de usar el Animator.
        ReasignarReferencias();

        // Si estabamos en pose de parry, la soltamos antes de atacar.
        poseParryActiva = false;

        // Marcamos al personaje como plantado durante el swing.
        EstablecerPlantado(true);

        // Para el controller nuevo usamos Attack, y para el viejo mantenemos AtaqueFuerte.
        DispararTriggerSiExiste(TriggerAtaqueIngles);
        DispararTriggerSiExiste(TriggerAtaqueFuerteIngles);
        DispararTriggerSiExiste(TriggerAtaqueFuerteEspanol);
    }

    // Este metodo dispara la reaccion corta de parry cuando el jugador levanta la espada para defender.
    public void ReproducirParry()
    {
        // Reintentamos referencias antes de tocar el Animator.
        ReasignarReferencias();

        // Durante el parry tambien dejamos al personaje plantado para que no se deslice.
        EstablecerPlantado(true);

        // Marcamos la pose defensiva para que el brazo se vea claramente en guardia.
        poseParryActiva = true;

        // Si el controller nuevo ya tiene trigger de parry, lo usamos.
        if (AnimatorTieneParametro(TriggerParryIngles, AnimatorControllerParameterType.Trigger))
        {
            DispararTriggerSiExiste(TriggerParryIngles);
            return;
        }
    }

    // Este metodo dispara la animacion de dano recibido.
    public void ReproducirRecibirDanio()
    {
        // Reintentamos referencias antes de usar el Animator.
        ReasignarReferencias();

        // El dano cancela cualquier pose de parry que hubiese quedado activa.
        poseParryActiva = false;

        // Disparamos el trigger nuevo y el de compatibilidad.
        DispararTriggerSiExiste(TriggerGolpeIngles);
        DispararTriggerSiExiste(TriggerGolpeEspanol);
    }

    // Este metodo dispara la animacion de muerte.
    public void ReproducirMorir()
    {
        // Reintentamos referencias antes de usar el Animator.
        ReasignarReferencias();

        // Al morir no mantenemos la pose de parry.
        poseParryActiva = false;

        // Al morir dejamos plantado al personaje para bloquear cualquier locomocion restante.
        EstablecerPlantado(true);

        // Disparamos el trigger nuevo y el de compatibilidad.
        DispararTriggerSiExiste(TriggerMuerteIngles);
        DispararTriggerSiExiste(TriggerMuerteEspanol);
    }

    // Este metodo deja un punto de cierre explicito para la ventana de parry.
    public void FinalizarParry()
    {
        // Soltamos la pose defensiva y garantizamos que locomocion pueda volver.
        poseParryActiva = false;
        EstablecerPlantado(false);
    }

    // Este metodo permite al gameplay marcar si el personaje esta plantado durante el swing.
    public void EstablecerPlantado(bool estaPlantado)
    {
        // Reintentamos referencias si el Animator aun no estaba enlazado.
        ReasignarReferencias();

        // Si no existe Animator, no hay nada que setear.
        if (animatorJugador == null)
        {
            return;
        }

        // Solo seteamos el bool si el controller actual realmente lo expone.
        EstablecerBoolSiExiste(ParametroPlantadoIngles, estaPlantado);
    }

    // Esta propiedad permite a otros sistemas consultar el estado real de plantado.
    public bool EstaPlantado
    {
        get
        {
            // Si no hay Animator o el parametro no existe, devolvemos falso seguro.
            if (animatorJugador == null || !AnimatorTieneParametro(ParametroPlantadoIngles, AnimatorControllerParameterType.Bool))
            {
                return false;
            }

            // Devolvemos el valor actual del parametro Plantado.
            return animatorJugador.GetBool(ParametroPlantadoIngles);
        }
    }

    // Este metodo busca de nuevo Animator, Rigidbody y espada.
    private void ReasignarReferencias()
    {
        // Si el Animator actual no sirve o no se asigno por inspector, intentamos encontrar el mas adecuado.
        if (animatorJugador == null || AnimatorPareceSerSoloTecnico(animatorJugador))
        {
            animatorJugador = BuscarAnimatorVisualMasAdecuado();
        }

        // Si falta Rigidbody, lo buscamos en el padre raiz del jugador.
        if (cuerpoRigido == null)
        {
            cuerpoRigido = GetComponentInParent<Rigidbody>();
        }

        // Si falta espada, la buscamos en la raiz del jugador.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si encontramos Animator, lo dejamos listo para usarse siempre.
        if (animatorJugador != null)
        {
            animatorJugador.applyRootMotion = false;
            animatorJugador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorJugador.updateMode = AnimatorUpdateMode.Normal;

            if (huesoBrazoDerecho == null && animatorJugador.isHuman)
            {
                huesoBrazoDerecho = animatorJugador.GetBoneTransform(HumanBodyBones.RightUpperArm);
            }

            if (huesoAntebrazoDerecho == null && animatorJugador.isHuman)
            {
                huesoAntebrazoDerecho = animatorJugador.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }

            if (huesoHombroDerecho == null && animatorJugador.isHuman)
            {
                huesoHombroDerecho = animatorJugador.GetBoneTransform(HumanBodyBones.RightShoulder);
            }

            if (huesoManoDerecha == null && animatorJugador.isHuman)
            {
                huesoManoDerecha = animatorJugador.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (socketEspadaVisual == null)
            {
                socketEspadaVisual = BuscarSocketEspadaVisual(animatorJugador.transform);
            }

            if (huesoBrazoDerecho != null && !poseBrazoInicializada)
            {
                rotacionBaseBrazoDerecho = huesoBrazoDerecho.localRotation;
                poseBrazoInicializada = true;
            }

            if (huesoAntebrazoDerecho != null && !poseAntebrazoInicializada)
            {
                rotacionBaseAntebrazoDerecho = huesoAntebrazoDerecho.localRotation;
                poseAntebrazoInicializada = true;
            }

            if (huesoHombroDerecho != null && !poseHombroInicializada)
            {
                rotacionBaseHombroDerecho = huesoHombroDerecho.localRotation;
                poseHombroInicializada = true;
            }

            if (huesoManoDerecha != null && !poseManoInicializada)
            {
                rotacionBaseManoDerecha = huesoManoDerecha.localRotation;
                poseManoInicializada = true;
            }

            if (socketEspadaVisual != null && !poseSocketEspadaInicializada)
            {
                rotacionBaseSocketEspada = socketEspadaVisual.localRotation;
                poseSocketEspadaInicializada = true;
            }
        }

        // Consideramos que lo minimo necesario es tener Animator.
        referenciasListas = animatorJugador != null;
    }

    // Este metodo busca el Animator visual mas util entre este objeto y sus hijos.
    private Animator BuscarAnimatorVisualMasAdecuado()
    {
        // Primero probamos el Animator local.
        Animator animatorLocal = GetComponent<Animator>();
        if (animatorLocal != null && !AnimatorPareceSerSoloTecnico(animatorLocal))
        {
            return animatorLocal;
        }

        // Si el local no sirve, buscamos uno mejor entre todos los hijos.
        Animator[] animatorsEncontrados = GetComponentsInChildren<Animator>(true);
        for (int indiceAnimator = 0; indiceAnimator < animatorsEncontrados.Length; indiceAnimator++)
        {
            if (animatorsEncontrados[indiceAnimator] == null)
            {
                continue;
            }

            if (AnimatorPareceSerSoloTecnico(animatorsEncontrados[indiceAnimator]))
            {
                continue;
            }

            return animatorsEncontrados[indiceAnimator];
        }

        // Si no encontramos uno ideal, devolvemos el local aunque sea tecnico como ultimo respaldo.
        return animatorLocal;
    }

    // Este metodo detecta si un Animator parece solo tecnico y no el visual del modelo.
    private bool AnimatorPareceSerSoloTecnico(Animator animatorCandidato)
    {
        // Si no existe, claramente no sirve.
        if (animatorCandidato == null)
        {
            return true;
        }

        // Si vive junto a un renderer de piel o tiene uno en hijos, lo consideramos visual y valido.
        if (animatorCandidato.GetComponent<SkinnedMeshRenderer>() != null || animatorCandidato.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
        {
            return false;
        }

        // Si ya tiene un controller asignado, tambien lo consideramos candidato valido.
        if (animatorCandidato.runtimeAnimatorController != null)
        {
            return false;
        }

        // En cualquier otro caso lo tratamos como un Animator tecnico vacio.
        return true;
    }

    // Este metodo aplica los bools y floats continuos del Animator.
    private void AplicarParametrosContinuos()
    {
        // Si no hay Animator, salimos.
        if (animatorJugador == null)
        {
            return;
        }

        // Actualizamos velocidad tanto en el parametro nuevo como en el legado si existen.
        EstablecerFloatSiExiste(ParametroVelocidadIngles, velocidadMovimientoActual);
        EstablecerFloatSiExiste(ParametroVelocidadEspanol, velocidadMovimientoActual);

        // Actualizamos sprint tanto en el parametro nuevo como en el legado si existen.
        EstablecerBoolSiExiste(ParametroSprintIngles, corriendoActual);
        EstablecerBoolSiExiste(ParametroSprintEspanol, corriendoActual);

        // Actualizamos el estado de suelo.
        EstablecerBoolSiExiste(ParametroSueloIngles, enSueloActual);
        EstablecerBoolSiExiste(ParametroSueloEspanol, enSueloActual);
    }

    // Este metodo lo llama un Animation Event del ataque normal.
    public void AplicarDanioNormal()
    {
        // Si la espada se perdio por un reload, la buscamos otra vez.
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

    // Este metodo lo llama un Animation Event del ataque fuerte.
    public void AplicarDanioFuerte()
    {
        // Si la espada se perdio por un reload, la buscamos otra vez.
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

    // Este metodo lo llama el Animation Event del clip de slash para aplicar el impacto real.
    public void OnSwordImpact()
    {
        // Si la espada se perdio por un reload, la buscamos otra vez.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si existe, delegamos el impacto al sistema real de combate.
        if (sistemaEspada != null)
        {
            sistemaEspada.OnSwordImpact();
        }
    }

    // Este metodo lo llama el evento del clip compartido cuando el slash normal llega al frame de impacto.
    public void OnImpactoNormal()
    {
        // Si la espada se perdio por un reload, la buscamos otra vez.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si existe, delegamos el impacto normal al sistema real de combate.
        if (sistemaEspada != null)
        {
            sistemaEspada.OnImpactoNormal();
        }
    }

    // Este metodo lo llama el evento del clip compartido cuando el slash fuerte llega al frame de impacto.
    public void OnImpactoFuerte()
    {
        // Si la espada se perdio por un reload, la buscamos otra vez.
        if (sistemaEspada == null)
        {
            sistemaEspada = GetComponentInParent<SistemaEspada>();
        }

        // Si existe, delegamos el impacto fuerte al sistema real de combate.
        if (sistemaEspada != null)
        {
            sistemaEspada.OnImpactoFuerte();
        }
    }

    // Este metodo dispara un trigger solo si el Animator lo tiene.
    private void DispararTriggerSiExiste(string nombreTrigger)
    {
        // Si falta Animator o el parametro no existe, no hacemos nada.
        if (animatorJugador == null || !AnimatorTieneParametro(nombreTrigger, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        // Limpiamos y luego disparamos el trigger para asegurar consistencia.
        animatorJugador.ResetTrigger(nombreTrigger);
        animatorJugador.SetTrigger(nombreTrigger);
    }

    // Este metodo setea un float solo si el parametro existe.
    private void EstablecerFloatSiExiste(string nombreParametro, float valor)
    {
        if (animatorJugador == null || !AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Float))
        {
            return;
        }

        animatorJugador.SetFloat(nombreParametro, valor, 0.1f, Time.unscaledDeltaTime);
    }

    // Este metodo setea un bool solo si el parametro existe.
    private void EstablecerBoolSiExiste(string nombreParametro, bool valor)
    {
        if (animatorJugador == null || !AnimatorTieneParametro(nombreParametro, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animatorJugador.SetBool(nombreParametro, valor);
    }

    // Este metodo revisa si el Animator tiene un parametro con nombre y tipo dados.
    private bool AnimatorTieneParametro(string nombreParametro, AnimatorControllerParameterType tipoEsperado)
    {
        if (animatorJugador == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parametros = animatorJugador.parameters;
        for (int indiceParametro = 0; indiceParametro < parametros.Length; indiceParametro++)
        {
            if (parametros[indiceParametro].name != nombreParametro)
            {
                continue;
            }

            return parametros[indiceParametro].type == tipoEsperado;
        }

        return false;
    }

    // Este metodo corrige la pose del brazo derecho durante locomocion para que la espada no quede levantada.
    private void AplicarCorreccionBrazoDerechoEnIdle()
    {
        if (!corregirBrazoDerechoEnIdle || animatorJugador == null || huesoBrazoDerecho == null)
        {
            return;
        }

        AnimatorStateInfo estadoActual = animatorJugador.GetCurrentAnimatorStateInfo(0);
        bool estaEnIdle = estadoActual.IsName("Idle") || estadoActual.IsName("Base Layer.Idle");
        bool estaEnWalk = estadoActual.IsName("Walk") || estadoActual.IsName("Base Layer.Walk");
        bool estaEnRun = estadoActual.IsName("Run") || estadoActual.IsName("Base Layer.Run");
        bool estaEnLocomocion = estaEnIdle || estaEnWalk || estaEnRun;
        bool estaEnParry = poseParryActiva || estadoActual.IsName("Parry") || estadoActual.IsName("Base Layer.Parry");
        bool estaEnAtaque =
            estadoActual.IsName("Attack") || estadoActual.IsName("Base Layer.Attack") ||
            estadoActual.IsName("AttackNormal") || estadoActual.IsName("Base Layer.AttackNormal") ||
            estadoActual.IsName("AttackFuerte") || estadoActual.IsName("Base Layer.AttackFuerte");
        bool estaEnGolpe =
            estadoActual.IsName("Hit") || estadoActual.IsName("Base Layer.Hit") ||
            estadoActual.IsName("RecibirDanio") || estadoActual.IsName("Base Layer.RecibirDanio");
        bool estaEnMuerte =
            estadoActual.IsName("Death") || estadoActual.IsName("Base Layer.Death") ||
            estadoActual.IsName("Die") || estadoActual.IsName("Base Layer.Die") ||
            estadoActual.IsName("Morir") || estadoActual.IsName("Base Layer.Morir");
        bool estaEnTransicionCritica = animatorJugador.IsInTransition(0) && EsEstadoCritico(animatorJugador.GetNextAnimatorStateInfo(0));

        if (estaEnAtaque || estaEnGolpe || estaEnMuerte || estaEnTransicionCritica)
        {
            return;
        }

        if (estaEnParry)
        {
            AplicarPoseBrazoDerecho(
                PoseHombroDerechoParry,
                PoseBrazoDerechoParry,
                PoseAntebrazoDerechoParry,
                PoseManoDerechaParry,
                PoseSocketEspadaParry);
            return;
        }

        if (estaEnLocomocion)
        {
            AplicarPoseBrazoDerecho(
                PoseHombroDerechoLocomocion,
                PoseBrazoDerechoLocomocion,
                PoseAntebrazoDerechoLocomocion,
                PoseManoDerechaLocomocion,
                PoseSocketEspadaLocomocion);
            return;
        }
    }

    // Este metodo aplica una pose fija sobre hombro, brazo, antebrazo y mano para evitar vibracion visual.
    private void AplicarPoseBrazoDerecho(Vector3 hombro, Vector3 brazo, Vector3 antebrazo, Vector3 mano, Vector3 socket)
    {
        if (huesoHombroDerecho != null)
        {
            huesoHombroDerecho.localRotation = Quaternion.Euler(hombro);
        }

        if (huesoBrazoDerecho != null)
        {
            huesoBrazoDerecho.localRotation = Quaternion.Euler(brazo);
        }

        if (huesoAntebrazoDerecho != null)
        {
            huesoAntebrazoDerecho.localRotation = Quaternion.Euler(antebrazo);
        }

        if (huesoManoDerecha != null)
        {
            huesoManoDerecha.localRotation = Quaternion.Euler(mano);
        }

        // El socket se mantiene con la rotacion base del setup para que la espada siga la mano sin desalinearse.
        if (socketEspadaVisual != null && poseSocketEspadaInicializada)
        {
            socketEspadaVisual.localRotation = rotacionBaseSocketEspada;
        }
    }

    // Esta funcion identifica si un estado visual pertenece a ataque, golpe o muerte.
    private bool EsEstadoCritico(AnimatorStateInfo estado)
    {
        return estado.IsName("Attack") || estado.IsName("Base Layer.Attack") ||
               estado.IsName("AttackNormal") || estado.IsName("Base Layer.AttackNormal") ||
               estado.IsName("AttackFuerte") || estado.IsName("Base Layer.AttackFuerte") ||
               estado.IsName("Hit") || estado.IsName("Base Layer.Hit") ||
               estado.IsName("RecibirDanio") || estado.IsName("Base Layer.RecibirDanio") ||
               estado.IsName("Death") || estado.IsName("Base Layer.Death") ||
               estado.IsName("Die") || estado.IsName("Base Layer.Die") ||
               estado.IsName("Morir") || estado.IsName("Base Layer.Morir");
    }

    // Este metodo devuelve huesos y socket a la rotacion original cuando el componente se apaga.
    private void RestaurarPoseBaseBrazoDerecho()
    {
        if (huesoHombroDerecho != null && poseHombroInicializada)
        {
            huesoHombroDerecho.localRotation = rotacionBaseHombroDerecho;
        }

        if (huesoBrazoDerecho != null && poseBrazoInicializada)
        {
            huesoBrazoDerecho.localRotation = rotacionBaseBrazoDerecho;
        }

        if (huesoAntebrazoDerecho != null && poseAntebrazoInicializada)
        {
            huesoAntebrazoDerecho.localRotation = rotacionBaseAntebrazoDerecho;
        }

        if (huesoManoDerecha != null && poseManoInicializada)
        {
            huesoManoDerecha.localRotation = rotacionBaseManoDerecha;
        }

        if (socketEspadaVisual != null && poseSocketEspadaInicializada)
        {
            socketEspadaVisual.localRotation = rotacionBaseSocketEspada;
        }
    }

    private Transform BuscarSocketEspadaVisual(Transform raizBusqueda)
    {
        if (raizBusqueda == null)
        {
            return null;
        }

        Transform encontrado = raizBusqueda.Find("SocketEspadaVisual");
        if (encontrado != null)
        {
            return encontrado;
        }

        foreach (Transform hijo in raizBusqueda.GetComponentsInChildren<Transform>(true))
        {
            if (hijo != null && hijo.name == "SocketEspadaVisual")
            {
                return hijo;
            }
        }

        return null;
    }

    // COPILOT-EXPAND: Aqui podes sumar sincronizacion Mirror, capas de upper body y blend trees mas avanzados.
}
