using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla la pantalla de Game Over, muestra estadisticas
// finales y reinicia la escena actual con un boton.

// Esta clase maneja la UI final cuando termina la partida.
public class PantallaGameOver : MonoBehaviour
{
    // Esta referencia apunta al panel raiz que contiene toda la UI de game over.
    [SerializeField] private GameObject panelGameOver;

    // Esta referencia apunta al texto del titulo principal.
    [SerializeField] private Text textoTitulo;

    // Esta referencia apunta al texto que muestra la oleada alcanzada.
    [SerializeField] private Text textoOleada;

    // Esta referencia apunta al texto que muestra las kills totales.
    [SerializeField] private Text textoKills;

    // Esta referencia apunta al texto que muestra el tiempo final de partida.
    [SerializeField] private Text textoTiempo;

    // Esta referencia apunta al texto que muestra el nivel alcanzado.
    [SerializeField] private Text textoNivel;

    // Esta referencia apunta al texto que muestra la mejor racha.
    [SerializeField] private Text textoRacha;

    // Esta referencia apunta al boton de reinicio.
    [SerializeField] private Button botonReiniciar;

    // Esta referencia guarda el GameManager actual de la escena.
    private GameManager gameManager;

    // Esta funcion corre al iniciar el componente y deja todo listo.
    private void Awake()
    {
        // Si no se asigno el panel, usamos el primer hijo como respaldo.
        if (panelGameOver == null && transform.childCount > 0)
        {
            panelGameOver = transform.GetChild(0).gameObject;
        }

        // Si existe un boton de reinicio, conectamos su evento una sola vez.
        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.RemoveListener(ReiniciarPartida);
            botonReiniciar.onClick.AddListener(ReiniciarPartida);
        }

        // Dejamos el panel oculto al comenzar.
        OcultarPanelVisual();
    }

    // Esta funcion se ejecuta al habilitar el objeto y conecta eventos globales.
    private void OnEnable()
    {
        // Intentamos encontrar GameManager si todavia no estaba cacheado.
        IntentarEncontrarGameManager();

        // Escuchamos recarga de escena para restaurar estado visual limpio.
        SceneManager.sceneLoaded += ManejarEscenaCargada;
    }

    // Esta funcion se ejecuta al deshabilitar el objeto y limpia eventos.
    private void OnDisable()
    {
        // Si existe GameManager, dejamos de escuchar su evento.
        if (gameManager != null)
        {
            gameManager.AlEstadoPartidaCambiado -= ManejarCambioEstadoPartida;
        }

        // Restauramos el tiempo por seguridad al salir.
        Time.timeScale = 1f;

        // Dejamos de escuchar carga de escenas para evitar suscripciones duplicadas.
        SceneManager.sceneLoaded -= ManejarEscenaCargada;
    }

    // Esta funcion corre cada frame para reintentar encontrar GameManager si aparece mas tarde.
    private void Update()
    {
        // Si ya tenemos GameManager, no hace falta seguir buscando.
        if (gameManager != null)
        {
            return;
        }

        // Reintentamos encontrarlo.
        IntentarEncontrarGameManager();
    }

    // Este metodo intenta localizar y conectar el GameManager actual.
    private void IntentarEncontrarGameManager()
    {
        // Si ya estaba encontrado, no hacemos nada.
        if (gameManager != null)
        {
            return;
        }

        // Usamos la instancia global si ya existe.
        gameManager = GameManager.Instancia;

        // Si aun no existe, buscamos uno en la escena.
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        // Si encontramos uno, nos suscribimos y sincronizamos el estado visual actual.
        if (gameManager != null)
        {
            gameManager.AlEstadoPartidaCambiado -= ManejarCambioEstadoPartida;
            gameManager.AlEstadoPartidaCambiado += ManejarCambioEstadoPartida;
            ManejarCambioEstadoPartida(gameManager.EstadoActual);
        }
    }

    // Este metodo responde cuando cambia el estado general de la partida.
    private void ManejarCambioEstadoPartida(EstadoPartida nuevoEstado)
    {
        // Si la partida llego a GameOver, mostramos la pantalla.
        if (nuevoEstado == EstadoPartida.GameOver)
        {
            MostrarPantallaGameOver();
            return;
        }

        // En cualquier otro estado, la ocultamos.
        OcultarPanelVisual();
    }

    // Este metodo muestra la pantalla de game over y actualiza sus textos.
    private void MostrarPantallaGameOver()
    {
        // Activamos visualmente el panel si existe.
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        // Actualizamos los textos finales con los datos del manager.
        ActualizarTextos();

        // Frenamos el tiempo para que el juego quede congelado en el final.
        Time.timeScale = 0f;

        // Liberamos cursor para que se pueda interactuar con los botones de UI.
        ConfigurarCursorModoUI();
    }

    // Este metodo oculta solo la parte visual del panel.
    private void OcultarPanelVisual()
    {
        // Si el panel existe, lo apagamos.
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }
    }

    // Este metodo escribe todos los textos finales.
    private void ActualizarTextos()
    {
        // Si falta GameManager, no podemos mostrar datos reales.
        if (gameManager == null)
        {
            return;
        }

        // Si existe el titulo, escribimos el mensaje principal.
        if (textoTitulo != null)
        {
            textoTitulo.text = "GAME OVER";
        }

        // Si existe el texto de oleada, mostramos la oleada alcanzada.
        if (textoOleada != null)
        {
            textoOleada.text = "Oleada: " + gameManager.OleadaActual;
        }

        // Si existe el texto de kills, mostramos el total.
        if (textoKills != null)
        {
            textoKills.text = "Kills: " + gameManager.KillsTotales;
        }

        // Si existe el texto de tiempo, mostramos el tiempo formateado.
        if (textoTiempo != null)
        {
            textoTiempo.text = "Tiempo: " + gameManager.ObtenerTiempoFormateado();
        }

        // Si existe el texto de nivel, mostramos el nivel actual del jugador.
        if (textoNivel != null)
        {
            textoNivel.text = "Nivel: " + gameManager.NivelJugadorPrincipal;
        }

        // Si existe el texto de racha, mostramos la mejor racha.
        if (textoRacha != null)
        {
            textoRacha.text = "Mejor racha: " + gameManager.MejorRachaJugadorPrincipal;
        }
    }

    // Este metodo recarga la escena actual para reiniciar la partida.
    public void ReiniciarPartida()
    {
        // Restauramos el tiempo normal antes de recargar.
        Time.timeScale = 1f;

        // Rebloqueamos cursor para volver al control de juego.
        ConfigurarCursorModoJuego();

        // Si tenemos manager, usamos su metodo centralizado de reinicio limpio.
        if (gameManager != null)
        {
            gameManager.ReiniciarPartida();
            return;
        }

        // Si no habia manager, hacemos fallback directo de recarga.
        Scene escenaActual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActual.buildIndex);
    }

    // COPILOT-EXPAND: Aqui podes agregar boton de volver al menu, compartir puntaje y tabla de resultados cooperativa.

    // Este metodo se ejecuta cuando Unity termina de cargar una escena.
    private void ManejarEscenaCargada(Scene escenaCargada, LoadSceneMode modoCarga)
    {
        // Dejamos tiempo normal por seguridad para evitar congelamiento post-restart.
        Time.timeScale = 1f;

        // Ocultamos panel en una escena nueva para iniciar limpio.
        OcultarPanelVisual();

        // Rebloqueamos cursor para modo juego.
        ConfigurarCursorModoJuego();

        // Reintentamos enlazar GameManager de la nueva escena.
        gameManager = null;
        IntentarEncontrarGameManager();
    }

    // Este metodo configura cursor para interaccion de menus.
    private void ConfigurarCursorModoUI()
    {
        // Soltamos el cursor para poder usar botones.
        Cursor.lockState = CursorLockMode.None;

        // Mostramos el cursor en pantalla.
        Cursor.visible = true;
    }

    // Este metodo configura cursor para gameplay.
    private void ConfigurarCursorModoJuego()
    {
        // Bloqueamos el cursor al centro.
        Cursor.lockState = CursorLockMode.Locked;

        // Ocultamos el cursor durante juego.
        Cursor.visible = false;
    }
}
