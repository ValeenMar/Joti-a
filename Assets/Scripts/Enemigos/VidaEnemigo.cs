using System;
using UnityEngine;

// Esta clase administra la vida del enemigo y recibe dano desde el sistema de combate.
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

    // Esta variable evita procesar dano una vez que el enemigo ya murio.
    private bool estaMuerto;

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

    // Esta funcion se ejecuta cuando el objeto se activa en escena.
    private void Awake()
    {
        // Aseguramos que al iniciar la vida actual sea igual a la vida maxima.
        vidaActual = vidaMaxima;

        // Marcamos al enemigo como vivo al iniciar.
        estaMuerto = false;
    }

    // Este metodo recibe dano desde cualquier sistema que use IRecibidorDanio.
    public void RecibirDanio(DatosDanio datosDanio)
    {
        // Si ya esta muerto, ignoramos nuevos impactos.
        if (estaMuerto)
        {
            // Salimos para evitar eventos duplicados.
            return;
        }

        // Si llega un dato nulo, creamos uno basico para no romper el flujo.
        if (datosDanio == null)
        {
            // Creamos datos por defecto para que el sistema sea robusto.
            datosDanio = new DatosDanio();
        }

        // Si el objetivo no viene seteado, lo forzamos al enemigo actual.
        if (datosDanio.objetivo == null)
        {
            // El objetivo real de este dano es este gameObject.
            datosDanio.objetivo = gameObject;
        }

        // Restamos el dano final calculado por zona debil.
        vidaActual -= datosDanio.DanioFinalCalculado;

        // Evitamos que la vida baje de cero.
        vidaActual = Mathf.Max(0f, vidaActual);

        // Notificamos el golpe para dano flotante y feedback general.
        EventosJuego.NotificarDanioAplicado(datosDanio);

        // Avisamos a quien escuche que se recibio un impacto.
        AlRecibirDanio?.Invoke(datosDanio);

        // Avisamos a la barra de vida cuanto queda.
        AlVidaActualizada?.Invoke(vidaActual, vidaMaxima);

        // Si la vida llego a cero, procesamos muerte.
        if (vidaActual <= 0f)
        {
            // Ejecutamos el flujo central de muerte.
            ProcesarMuerte(datosDanio);
        }
    }

    // Este metodo centraliza todo lo que pasa cuando el enemigo muere.
    private void ProcesarMuerte(DatosDanio datosDanio)
    {
        // Marcamos al enemigo como muerto para bloquear logica futura.
        estaMuerto = true;

        // Si hay un prefab de orbe asignado, lo instanciamos justo en la posicion del enemigo.
        if (prefabOrbeExperiencia != null)
        {
            // Creamos la orbe en el mismo lugar donde murio el enemigo.
            Instantiate(prefabOrbeExperiencia, transform.position, Quaternion.identity);
        }

        // En Mirror, esta notificacion deberia salir desde el servidor con autoridad.
        // [Mirror futuro] Este bloque seria llamado desde un [Command] validado por servidor.
        EventosJuego.NotificarEnemigoEliminado(datosDanio.atacante, gameObject, experienciaOtorgada);

        // Avisamos a los componentes del enemigo que deben reaccionar a la muerte.
        AlMorir?.Invoke(datosDanio);
    }
}
