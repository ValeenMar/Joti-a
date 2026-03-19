using System;
using UnityEngine;

// Esta clase administra la vida del enemigo y coordina su muerte con la animacion.
public class VidaEnemigo : MonoBehaviour, IRecibidorDanio
{
    // Esta variable define la vida maxima del enemigo.
    [SerializeField] private float vidaMaxima = 120f;

    // Esta variable guarda la vida actual del enemigo.
    [SerializeField] private float vidaActual = 120f;

    // Esta variable define cuanta experiencia entrega al morir.
    [SerializeField] private int experienciaOtorgada = 25;

    // Esta variable publica te deja arrastrar desde el Inspector el prefab de la orbe de experiencia.
    public GameObject prefabOrbeExperiencia;

    // Esta referencia guarda el controlador visual del esqueleto.
    [SerializeField] private ControladorAnimacionEnemigo controladorAnimacionEnemigo;

    // Esta variable evita procesar dano una vez que el enemigo ya murio.
    private bool estaMuerto;

    // Esta variable evita completar la muerte mas de una vez.
    private bool muerteProcesada;

    // Esta referencia guarda una posible finalizacion de respaldo si falta el evento de animacion.
    private Coroutine rutinaMuerteFallback;

    // Este evento avisa cambios de vida para actualizar UI de barra flotante.
    public event Action<float, float> AlVidaActualizada;

    // Este evento avisa que el enemigo recibio un golpe.
    public event Action<DatosDanio> AlRecibirDanio;

    // Este evento avisa que el enemigo murio para que otros sistemas reaccionen.
    public event Action<DatosDanio> AlMorir;

    // Esta propiedad permite leer la vida actual desde otros scripts.
    public float VidaActual => vidaActual;

    // Esta propiedad permite leer la vida maxima desde otros scripts.
    public float VidaMaxima => vidaMaxima;

    // Esta propiedad permite saber si el enemigo sigue vivo.
    public bool EstaVivo => !estaMuerto;

    // Esta propiedad expone la experiencia que suelta al morir.
    public int ExperienciaOtorgada => experienciaOtorgada;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        estaMuerto = false;
        muerteProcesada = false;
        BuscarYConectarControladorAnimacion();
    }

    private void OnEnable()
    {
        BuscarYConectarControladorAnimacion();
    }

    private void Start()
    {
        BuscarYConectarControladorAnimacion();
    }

    private void OnDisable()
    {
        DesconectarControladorAnimacion();

        if (rutinaMuerteFallback != null)
        {
            StopCoroutine(rutinaMuerteFallback);
            rutinaMuerteFallback = null;
        }
    }

    // Este metodo intenta encontrar el controlador visual y conectarlo al evento de muerte.
    private void BuscarYConectarControladorAnimacion()
    {
        // Buscamos el controlador en hijos, incluyendo objetos inactivos.
        ControladorAnimacionEnemigo controladorEncontrado = GetComponentInChildren<ControladorAnimacionEnemigo>(true);

        // Si no lo encontramos en hijos, probamos en el mismo objeto.
        if (controladorEncontrado == null)
        {
            controladorEncontrado = GetComponent<ControladorAnimacionEnemigo>();
        }

        // Si no hay cambios, no hacemos trabajo extra.
        if (controladorAnimacionEnemigo == controladorEncontrado)
        {
            return;
        }

        // Desconectamos el controlador viejo si existia.
        DesconectarControladorAnimacion();

        // Guardamos la nueva referencia.
        controladorAnimacionEnemigo = controladorEncontrado;

        // Si ahora si hay controlador, escuchamos el final de la animacion de muerte.
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.OnMuerteAnimacionTerminada += ManejarMuerteAnimacionTerminada;
        }
    }

    // Este metodo quita la suscripcion al controlador visual si estaba conectada.
    private void DesconectarControladorAnimacion()
    {
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.OnMuerteAnimacionTerminada -= ManejarMuerteAnimacionTerminada;
            controladorAnimacionEnemigo = null;
        }
    }

    // Este metodo recibe dano desde cualquier sistema que use IRecibidorDanio.
    public void RecibirDanio(DatosDanio datosDanio)
    {
        // Si ya esta muerto, ignoramos nuevos impactos.
        if (estaMuerto)
        {
            return;
        }

        // Si llega un dato nulo, creamos uno basico para no romper el flujo.
        if (datosDanio == null)
        {
            datosDanio = new DatosDanio();
        }

        // Si el objetivo no viene seteado, lo forzamos al enemigo actual.
        if (datosDanio.objetivo == null)
        {
            datosDanio.objetivo = gameObject;
        }

        // Restamos el dano final calculado por zona debil.
        float danioFinal = Mathf.Max(0f, datosDanio.DanioFinalCalculado);
        vidaActual = Mathf.Max(0f, vidaActual - danioFinal);

        // Notificamos el golpe para dano flotante y feedback general.
        EventosJuego.NotificarDanioAplicado(datosDanio);

        // Avisamos a quien escuche que se recibio un impacto.
        AlRecibirDanio?.Invoke(datosDanio);

        // Avisamos a la barra de vida cuanto queda.
        AlVidaActualizada?.Invoke(vidaActual, vidaMaxima);

        // Si la vida llego a cero, iniciamos la muerte animada.
        if (vidaActual <= 0f)
        {
            IniciarMuerteAnimada(datosDanio);
            return;
        }

        // Si sigue vivo, reproducimos el golpe visual de retroceso.
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.ReproducirRecibirDanio(datosDanio);
        }
    }

    // Este metodo centraliza el inicio de la muerte.
    private void IniciarMuerteAnimada(DatosDanio datosDanio)
    {
        // Marcamos al enemigo como muerto para bloquear nuevos golpes.
        estaMuerto = true;
        muerteProcesada = false;

        // Guardamos el golpe fatal para usarlo cuando termine la animacion.
        ultimoDanioFatal = datosDanio;

        // Si hay controlador visual, reproducimos la animacion de muerte.
        if (controladorAnimacionEnemigo != null)
        {
            controladorAnimacionEnemigo.ReproducirMorir();
        }
        else
        {
            // Si falta el controlador, usamos un respaldo para no dejar el enemigo trabado.
            if (rutinaMuerteFallback != null)
            {
                StopCoroutine(rutinaMuerteFallback);
            }

            rutinaMuerteFallback = StartCoroutine(RutinaMuerteFallback());
        }
    }

    // Esta corrutina finaliza la muerte si no llego el evento de animacion.
    private System.Collections.IEnumerator RutinaMuerteFallback()
    {
        yield return new WaitForSeconds(0.2f);
        ManejarMuerteAnimacionTerminada();
    }

    // Este metodo se llama cuando el Animation Event de muerte indica que ya termino.
    private void ManejarMuerteAnimacionTerminada()
    {
        // Evitamos procesar la muerte dos veces.
        if (muerteProcesada)
        {
            return;
        }

        muerteProcesada = true;

        // Si habia un respaldo corriendo, lo detenemos.
        if (rutinaMuerteFallback != null)
        {
            StopCoroutine(rutinaMuerteFallback);
            rutinaMuerteFallback = null;
        }

        // Si hay un prefab de orbe asignado, lo instanciamos justo en la posicion del enemigo.
        if (prefabOrbeExperiencia != null)
        {
            Instantiate(prefabOrbeExperiencia, transform.position, Quaternion.identity);
        }

        // En Mirror, esta notificacion deberia salir desde el servidor con autoridad.
        // [Mirror futuro] Este bloque seria llamado desde un [Command] validado por servidor.
        EventosJuego.NotificarEnemigoEliminado(ultimoDanioFatal != null ? ultimoDanioFatal.atacante : null, gameObject, experienciaOtorgada);

        // Avisamos a los componentes del enemigo que deben reaccionar a la muerte.
        AlMorir?.Invoke(ultimoDanioFatal);
    }

    // Esta variable guarda el ultimo golpe fatal recibido.
    private DatosDanio ultimoDanioFatal;
}
