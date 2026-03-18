/*
COPILOT-CONTEXT
Proyecto: Realm Brawl.
Motor: Unity 2022.3 LTS.
Este script centraliza estado de partida, tiempo, kills, nivel, racha y oleadas.
*/
using System;
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

// Esta clase centraliza estado global, tiempo, kills, nivel, racha y oleadas.
public class GameManager : MonoBehaviour
{
    // Esta referencia permite acceso global simple al manager.
    public static GameManager Instancia { get; private set; }

    // Este evento avisa cuando cambia el estado general de la partida.
    public event Action<EstadoPartida> AlEstadoPartidaCambiado;

    // Este evento avisa cuando cambia la oleada actual.
    public event Action<int> AlOleadaActualizada;

    // Este valor guarda el estado actual de la partida.
    [SerializeField] private EstadoPartida estadoActual = EstadoPartida.Iniciando;

    // Este valor indica si el estado debe pasar a Jugando automaticamente al iniciar.
    [SerializeField] private bool iniciarPartidaAutomaticamente = true;

    // Esta referencia apunta al jugador principal local para UI y game over.
    [SerializeField] private GameObject jugadorPrincipal;

    // Esta referencia apunta al sistema de oleadas activo en la escena.
    [SerializeField] private SistemaOleadas sistemaOleadas;

    // Este valor guarda el tiempo acumulado de partida en segundos.
    [SerializeField] private float tiempoPartida = 0f;

    // Este valor guarda las kills totales de la partida.
    [SerializeField] private int killsTotales = 0;

    // Este valor guarda el nivel actual del jugador principal.
    [SerializeField] private int nivelJugadorPrincipal = 1;

    // Este valor guarda la mejor racha conseguida por el jugador principal.
    [SerializeField] private int mejorRachaJugadorPrincipal = 0;

    // Este valor guarda la oleada actual del combate.
    [SerializeField] private int oleadaActual = 0;

    // Este valor guarda cuantos enemigos hay vivos en la oleada actual.
    [SerializeField] private int enemigosVivosOleada = 0;

    // Este valor guarda cuantos enemigos faltan por aparecer en la oleada actual.
    [SerializeField] private int enemigosPendientesOleada = 0;

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

    // Esta propiedad permite leer la mejor racha del jugador principal.
    public int MejorRachaJugadorPrincipal => mejorRachaJugadorPrincipal;

    // Esta propiedad permite leer la oleada actual.
    public int OleadaActual => oleadaActual;

    // Esta propiedad permite leer enemigos vivos de la oleada actual.
    public int EnemigosVivosOleada => enemigosVivosOleada;

    // Esta propiedad permite leer enemigos pendientes de spawn en la oleada actual.
    public int EnemigosPendientesOleada => enemigosPendientesOleada;

    // Esta propiedad expone el jugador principal actual.
    public GameObject JugadorPrincipal => jugadorPrincipal;

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
        if (iniciarPartidaAutomaticamente && estadoActual != EstadoPartida.GameOver)
        {
            // Pasamos al estado principal del juego.
            CambiarEstado(EstadoPartida.Jugando);
        }

        // Si no hay sistema de oleadas asignado, intentamos detectarlo automaticamente.
        if (sistemaOleadas == null)
        {
            // Buscamos el primer sistema de oleadas encontrado en la escena.
            sistemaOleadas = FindObjectOfType<SistemaOleadas>();
        }

        // Si encontramos sistema de oleadas, lo registramos para integracion simple.
        if (sistemaOleadas != null)
        {
            RegistrarSistemaOleadas(sistemaOleadas);
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

        // Nos suscribimos al evento de racha para guardar la mejor racha del jugador principal.
        EventosJuego.AlRachaActualizada += ManejarRachaActualizada;

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

        // Removemos suscripcion de racha.
        EventosJuego.AlRachaActualizada -= ManejarRachaActualizada;

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

        // Intentamos leer su mejor racha actual si existe el sistema correspondiente.
        SistemaRacha sistemaRacha = jugadorPrincipal != null ? jugadorPrincipal.GetComponent<SistemaRacha>() : null;

        // Si existe, sincronizamos el dato global.
        if (sistemaRacha != null)
        {
            mejorRachaJugadorPrincipal = sistemaRacha.MejorRacha;
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

        // Avisamos a cualquier UI o sistema que el estado cambio.
        AlEstadoPartidaCambiado?.Invoke(estadoActual);
    }

    // Este metodo registra una nueva oleada y la expone a la UI.
    public void RegistrarOleadaNueva(int nuevaOleada)
    {
        // Guardamos la oleada actual asegurando que no baje de uno.
        oleadaActual = Mathf.Max(1, nuevaOleada);

        // Avisamos a la UI que el numero de oleada cambio.
        AlOleadaActualizada?.Invoke(oleadaActual);

        // Si no estamos en game over, aseguramos que la partida siga en estado jugando.
        if (estadoActual != EstadoPartida.GameOver)
        {
            CambiarEstado(EstadoPartida.Jugando);
        }
    }

    // Este metodo permite registrar el sistema de oleadas que usa esta escena.
    public void RegistrarSistemaOleadas(SistemaOleadas sistema)
    {
        // Guardamos referencia del sistema recibido para consultas futuras.
        sistemaOleadas = sistema;
    }

    // Este metodo recibe el progreso de oleadas y actualiza datos globales.
    public void NotificarProgresoOleada(int nuevaOleada, int enemigosVivos, int enemigosPendientes)
    {
        // Si la oleada cambio, usamos el flujo existente para notificar eventos.
        if (nuevaOleada > 0 && nuevaOleada != oleadaActual)
        {
            RegistrarOleadaNueva(nuevaOleada);
        }

        // Guardamos cantidad de enemigos vivos reportados por el sistema.
        enemigosVivosOleada = Mathf.Max(0, enemigosVivos);

        // Guardamos cantidad de enemigos pendientes por aparecer.
        enemigosPendientesOleada = Mathf.Max(0, enemigosPendientes);
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

    // Este metodo procesa cuando cambia la racha de un jugador.
    private void ManejarRachaActualizada(GameObject jugador, int rachaActual)
    {
        // Si no tenemos jugador principal asignado, no seguimos.
        if (jugadorPrincipal == null)
        {
            return;
        }

        // Si el evento no pertenece al jugador principal, ignoramos.
        if (jugador != jugadorPrincipal)
        {
            return;
        }

        // Guardamos el valor mas alto alcanzado como mejor racha.
        mejorRachaJugadorPrincipal = Mathf.Max(mejorRachaJugadorPrincipal, rachaActual);
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

    // Este metodo devuelve el tiempo de partida en formato amigable para UI.
    public string ObtenerTiempoFormateado()
    {
        // Convertimos el tiempo total a minutos enteros.
        int minutos = Mathf.FloorToInt(tiempoPartida / 60f);

        // Convertimos el resto del tiempo a segundos enteros.
        int segundos = Mathf.FloorToInt(tiempoPartida % 60f);

        // Devolvemos el texto formateado con dos digitos.
        return minutos.ToString("00") + ":" + segundos.ToString("00");
    }

    // COPILOT-EXPAND: Aqui podes agregar puntaje global, monedas, dificultad y soporte multijugador con Mirror.
}
