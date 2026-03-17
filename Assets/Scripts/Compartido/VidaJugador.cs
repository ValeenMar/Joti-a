using UnityEngine;

// Esta clase representa la vida del jugador para poder recibir daño de enemigos.
public class VidaJugador : MonoBehaviour, IRecibidorDanio
{
    // Esta es la cantidad máxima de vida que puede tener el jugador.
    [SerializeField] private float vidaMaxima = 100f;

    // Esta variable guarda la vida actual del jugador en tiempo real.
    [SerializeField] private float vidaActual = 100f;

    // Esta propiedad permite leer la vida actual desde otros scripts.
    public float VidaActual => vidaActual;

    // Esta propiedad permite leer la vida máxima desde otros scripts.
    public float VidaMaxima => vidaMaxima;

    // Esta propiedad indica si el jugador sigue con vida.
    public bool EstaVivo => vidaActual > 0f;

    // Esta función se ejecuta al activar el objeto.
    private void Awake()
    {
        // Al iniciar, aseguramos que la vida actual arranque llena.
        vidaActual = vidaMaxima;
    }

    // Este método permite que otro sistema le aplique daño al jugador.
    public void RecibirDanio(DatosDanio datosDanio)
    {
        // Si por algun motivo llega un dato nulo, creamos uno basico para no romper el flujo.
        if (datosDanio == null)
        {
            // Creamos un contenedor basico para que el sistema siga siendo robusto.
            datosDanio = new DatosDanio();
        }

        // Si el jugador ya murió, no seguimos procesando daño.
        if (!EstaVivo)
        {
            // Salimos del método para evitar valores negativos o eventos repetidos.
            return;
        }

        // Si el golpe no trajo objetivo, usamos este propio jugador como receptor real.
        if (datosDanio.objetivo == null)
        {
            // Asignamos este gameObject para que UI y analytics tengan una referencia valida.
            datosDanio.objetivo = gameObject;
        }

        // Restamos el daño final calculado a la vida actual.
        vidaActual -= datosDanio.DanioFinalCalculado;

        // Evitamos que la vida baje de cero.
        vidaActual = Mathf.Max(0f, vidaActual);

        // Avisamos al resto del juego que el jugador recibió daño.
        EventosJuego.NotificarJugadorRecibioDanio(gameObject, datosDanio.DanioFinalCalculado);

        // Avisamos también que hubo un golpe aplicado para daño flotante y otros sistemas.
        EventosJuego.NotificarDanioAplicado(datosDanio);

        // Si la vida llegó a cero, notificamos la muerte.
        if (vidaActual <= 0f)
        {
            // Disparamos el evento de muerte del jugador.
            EventosJuego.NotificarJugadorMurio(gameObject);
        }
    }
}
