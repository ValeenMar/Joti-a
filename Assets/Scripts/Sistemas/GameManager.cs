using System.Collections.Generic;
using UnityEngine;

// Este enum define los estados basicos de la partida.
public enum EstadoPartida
{
    // Estado de preparacion inicial.
    Iniciando,

    // Estado principal donde se juega.
    Jugando,

    // Estado final cuando se termina la partida.
    GameOver
}

// Esta clase centraliza estado global, tiempo y conteo de kills.
public class GameManager : MonoBehaviour
{
    // Esta referencia permite acceso global simple al manager.
    public static GameManager Instancia { get; private set; }

    // Este valor guarda el estado actual de la partida.
    [SerializeField] private EstadoPartida estadoActual = EstadoPartida.Iniciando;

    // Este valor indica si el estado debe pasar a Jugando automaticamente al iniciar.
    [SerializeField] private bool iniciarPartidaAutomaticamente = true;

    // Esta referencia apunta al jugador principal local para UI y game over.
    [SerializeField] private GameObject jugadorPrincipal;

    // Este valor guarda el tiempo acumulado de partida en segundos.
    [SerializeField] private float tiempoPartida = 0f;

    // Este valor guarda las kills totales de la partida.
    [SerializeField] private int killsTotales = 0;

    // Este valor guarda el nivel actual del jugador principal.
    [SerializeField] private int nivelJugadorPrincipal = 1;

    // Este diccionario guarda kills por jugador para facilitar futuro multijugador.
    private readonly Dictionary<GameObject, int> killsPorJugador = new Dictionary<GameObject, int>();

    // Esta propiedad permite leer el estado actual.
    public EstadoPartida EstadoActual => estadoActual;

    // Esta propiedad permite leer el tiempo de partida.
    public float TiempoPartida => tiempoPartida;

    // Esta propiedad permite leer kills totales.
    public int KillsTotales => killsTotales;

    // Esta propiedad permite leer el nivel del jugador principal.
    public int NivelJugadorPrincipal => nivelJugadorPrincipal;

    // Esta funcion se ejecuta cuando Unity crea el objeto.
    private void Awake()
    {
        // Si ya existia una instancia y no somos nosotros, destruimos este duplicado.
        if (Instancia != null && Instancia != this)
        {
            // Eliminamos duplicado para mantener singleton estable.
            Destroy(gameObject);
            return;
        }

        // Guardamos esta instancia como unica.
        Instancia = this;
    }

    // Esta funcion se ejecuta al iniciar la escena.
    private void Start()
    {
        // Si aun no hay jugador principal, intentamos detectar uno automaticamente.
        if (jugadorPrincipal == null)
        {
            // Buscamos el primer VidaJugador disponible para tomarlo como jugador principal.
            VidaJugador vidaJugador = FindObjectOfType<VidaJugador>();

            // Si encontramos uno, lo registramos.
            if (vidaJugador != null)
            {
                RegistrarJugadorPrincipal(vidaJugador.gameObject);
            }
        }

        // Si hay auto inicio, cambiamos el estado a Jugando.
        if (iniciarPartidaAutomaticamente)
        {
            // Pasamos al estado principal del juego.
            CambiarEstado(EstadoPartida.Jugando);
        }
    }

    // Esta funcion se ejecuta cada frame.
    private void Update()
    {
        // Solo contamos tiempo si la partida esta en estado Jugando.
        if (estadoActual != EstadoPartida.Jugando)
        {
            // En otros estados no aumentamos tiempo.
            return;
        }

        // Sumamos tiempo de juego usando deltaTime.
        tiempoPartida += Time.deltaTime;
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento de enemigo eliminado para sumar kills.
        EventosJuego.AlEnemigoEliminado += ManejarEnemigoEliminado;

        // Nos suscribimos al evento de subida de nivel para actualizar dato global.
        EventosJuego.AlJugadorSubioNivel += ManejarJugadorSubioNivel;

        // Nos suscribimos al evento de muerte para pasar a game over si muere jugador principal.
        EventosJuego.AlJugadorMurio += ManejarJugadorMurio;
    }

    // Esta funcion se ejecuta al desactivar el componente.
    private void OnDisable()
    {
        // Removemos suscripcion de kills.
        EventosJuego.AlEnemigoEliminado -= ManejarEnemigoEliminado;

        // Removemos suscripcion de nivel.
        EventosJuego.AlJugadorSubioNivel -= ManejarJugadorSubioNivel;

        // Removemos suscripcion de muerte.
        EventosJuego.AlJugadorMurio -= ManejarJugadorMurio;
    }

    // Este metodo permite asignar o cambiar el jugador principal durante runtime.
    public void RegistrarJugadorPrincipal(GameObject jugador)
    {
        // Guardamos la referencia del jugador principal.
        jugadorPrincipal = jugador;

        // Intentamos leer su nivel inicial desde SistemaXP.
        SistemaXP sistemaXP = jugadorPrincipal != null ? jugadorPrincipal.GetComponent<SistemaXP>() : null;

        // Si existe sistema XP, tomamos su nivel actual.
        if (sistemaXP != null)
        {
            // Actualizamos nivel global para UI o debug.
            nivelJugadorPrincipal = sistemaXP.NivelActual;
        }
    }

    // Este metodo cambia el estado de la partida.
    public void CambiarEstado(EstadoPartida nuevoEstado)
    {
        // Si no cambia nada, evitamos trabajo innecesario.
        if (estadoActual == nuevoEstado)
        {
            // Salimos sin tocar nada.
            return;
        }

        // Actualizamos al nuevo estado.
        estadoActual = nuevoEstado;
    }

    // Este metodo procesa cuando se elimina un enemigo.
    private void ManejarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        // En Mirror, este conteo deberia consolidarse en servidor y replicarse a clientes.

        // Sumamos una kill al contador global.
        killsTotales++;

        // Si no hubo atacante valido, dejamos el contador global y terminamos.
        if (atacante == null)
        {
            // Salimos para no usar una clave nula en el diccionario.
            return;
        }

        // Si no habia entrada para este atacante, la creamos en cero.
        if (!killsPorJugador.ContainsKey(atacante))
        {
            // Inicializamos conteo por jugador.
            killsPorJugador[atacante] = 0;
        }

        // Sumamos una kill al atacante.
        killsPorJugador[atacante]++;
    }

    // Este metodo procesa cuando un jugador sube de nivel.
    private void ManejarJugadorSubioNivel(GameObject jugador, int nivelAnterior, int nivelNuevo)
    {
        // Si no tenemos jugador principal asignado, ignoramos.
        if (jugadorPrincipal == null)
        {
            // Salimos hasta que se registre jugador principal.
            return;
        }

        // Solo actualizamos dato global si el evento es del jugador principal.
        if (jugador != jugadorPrincipal)
        {
            // Ignoramos niveles de otros jugadores.
            return;
        }

        // Guardamos nivel nuevo para paneles globales.
        nivelJugadorPrincipal = nivelNuevo;
    }

    // Este metodo procesa la muerte de un jugador.
    private void ManejarJugadorMurio(GameObject jugador)
    {
        // Si no hay jugador principal asignado, no podemos evaluar game over local.
        if (jugadorPrincipal == null)
        {
            // Salimos de forma segura.
            return;
        }

        // Si murio el jugador principal, pasamos a GameOver.
        if (jugador == jugadorPrincipal)
        {
            // Cambiamos estado de la partida.
            CambiarEstado(EstadoPartida.GameOver);
        }
    }

    // Este metodo devuelve kills de un jugador especifico.
    public int ObtenerKillsDeJugador(GameObject jugador)
    {
        // Si el jugador no existe en diccionario, devolvemos cero.
        if (!killsPorJugador.ContainsKey(jugador))
        {
            // Retornamos cero por defecto.
            return 0;
        }

        // Devolvemos el conteo actual de kills para ese jugador.
        return killsPorJugador[jugador];
    }
}
