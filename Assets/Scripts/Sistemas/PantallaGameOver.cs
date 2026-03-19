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

        // Si todavia no existe panel real, lo construimos en runtime para no depender de la escena guardada.
        if (panelGameOver == null)
        {
            CrearInterfazRuntimeSiHaceFalta();
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
        // Si el panel fue borrado o nunca existio, lo regeneramos en runtime.
        if (panelGameOver == null)
        {
            CrearInterfazRuntimeSiHaceFalta();
        }

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

        // Si la escena nueva no trae panel guardado, lo regeneramos para no perder el Game Over.
        if (panelGameOver == null)
        {
            CrearInterfazRuntimeSiHaceFalta();
        }
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

    // Este metodo crea una UI minima de Game Over si la escena no la trae armada.
    private void CrearInterfazRuntimeSiHaceFalta()
    {
        // Si ya existe panel, no armamos otro duplicado.
        if (panelGameOver != null)
        {
            return;
        }

        // Buscamos un Canvas existente en la escena.
        Canvas canvasExistente = GetComponentInParent<Canvas>();

        if (canvasExistente == null)
        {
            canvasExistente = FindObjectOfType<Canvas>(true);
        }

        // Si no hay Canvas, no podemos construir UI.
        if (canvasExistente == null)
        {
            return;
        }

        // Si este objeto no esta dentro del Canvas, lo parentamos ahi para que la UI viva en pantalla.
        if (transform.parent != canvasExistente.transform)
        {
            transform.SetParent(canvasExistente.transform, false);
        }

        // Estiramos este controlador para ocupar toda la pantalla y evitar que el panel quede en un cuadrado chico.
        RectTransform rectControlador = GetComponent<RectTransform>();
        rectControlador.anchorMin = Vector2.zero;
        rectControlador.anchorMax = Vector2.one;
        rectControlador.offsetMin = Vector2.zero;
        rectControlador.offsetMax = Vector2.zero;
        rectControlador.localScale = Vector3.one;
        rectControlador.SetAsLastSibling();

        // Creamos el panel principal que tapa la pantalla.
        panelGameOver = CrearObjetoUI("PanelGameOver", transform);
        RectTransform rectPanel = panelGameOver.GetComponent<RectTransform>();
        rectPanel.anchorMin = Vector2.zero;
        rectPanel.anchorMax = Vector2.one;
        rectPanel.offsetMin = Vector2.zero;
        rectPanel.offsetMax = Vector2.zero;

        // Pintamos un fondo oscuro semitransparente.
        Image imagenPanel = panelGameOver.AddComponent<Image>();
        imagenPanel.color = new Color(0f, 0f, 0f, 0.82f);

        // Agregamos un grupo vertical para ordenar los textos automaticamente.
        VerticalLayoutGroup grupoVertical = panelGameOver.AddComponent<VerticalLayoutGroup>();
        grupoVertical.childAlignment = TextAnchor.MiddleCenter;
        grupoVertical.childControlHeight = false;
        grupoVertical.childControlWidth = true;
        grupoVertical.childForceExpandHeight = false;
        grupoVertical.childForceExpandWidth = true;
        grupoVertical.spacing = 12f;
        grupoVertical.padding = new RectOffset(60, 60, 120, 80);

        // Permitimos que el layout respire bien segun contenido.
        ContentSizeFitter ajusteContenido = panelGameOver.AddComponent<ContentSizeFitter>();
        ajusteContenido.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ajusteContenido.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Cargamos la fuente builtin vigente de Unity.
        Font fuenteRuntime = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Creamos y guardamos todos los textos principales.
        textoTitulo = CrearTextoRuntime("TextoTitulo", panelGameOver.transform, fuenteRuntime, 38, FontStyle.Bold, "GAME OVER");
        textoOleada = CrearTextoRuntime("TextoOleada", panelGameOver.transform, fuenteRuntime, 24, FontStyle.Normal, "Oleada: 0");
        textoKills = CrearTextoRuntime("TextoKills", panelGameOver.transform, fuenteRuntime, 24, FontStyle.Normal, "Kills: 0");
        textoTiempo = CrearTextoRuntime("TextoTiempo", panelGameOver.transform, fuenteRuntime, 24, FontStyle.Normal, "Tiempo: 00:00");
        textoNivel = CrearTextoRuntime("TextoNivel", panelGameOver.transform, fuenteRuntime, 24, FontStyle.Normal, "Nivel: 1");
        textoRacha = CrearTextoRuntime("TextoRacha", panelGameOver.transform, fuenteRuntime, 24, FontStyle.Normal, "Mejor racha: 0");

        // Creamos un boton simple para reiniciar.
        GameObject objetoBoton = CrearObjetoUI("BotonReiniciar", panelGameOver.transform);
        Image imagenBoton = objetoBoton.AddComponent<Image>();
        imagenBoton.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        botonReiniciar = objetoBoton.AddComponent<Button>();
        ColorBlock coloresBoton = botonReiniciar.colors;
        coloresBoton.normalColor = imagenBoton.color;
        coloresBoton.highlightedColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        coloresBoton.pressedColor = new Color(0.6f, 0.15f, 0.15f, 1f);
        coloresBoton.selectedColor = coloresBoton.highlightedColor;
        botonReiniciar.colors = coloresBoton;

        RectTransform rectBoton = objetoBoton.GetComponent<RectTransform>();
        rectBoton.sizeDelta = new Vector2(260f, 56f);

        // Creamos el texto interno del boton.
        Text textoBoton = CrearTextoRuntime("TextoBotonReiniciar", objetoBoton.transform, fuenteRuntime, 24, FontStyle.Bold, "Reiniciar");
        textoBoton.alignment = TextAnchor.MiddleCenter;
        RectTransform rectTextoBoton = textoBoton.GetComponent<RectTransform>();
        rectTextoBoton.anchorMin = Vector2.zero;
        rectTextoBoton.anchorMax = Vector2.one;
        rectTextoBoton.offsetMin = Vector2.zero;
        rectTextoBoton.offsetMax = Vector2.zero;

        // Conectamos el boton al metodo de reinicio.
        botonReiniciar.onClick.RemoveListener(ReiniciarPartida);
        botonReiniciar.onClick.AddListener(ReiniciarPartida);

        // Lo dejamos oculto hasta que realmente se pierda la partida.
        panelGameOver.SetActive(false);
    }

    // Este metodo crea un GameObject UI basico con RectTransform.
    private GameObject CrearObjetoUI(string nombreObjeto, Transform padre)
    {
        GameObject objetoUI = new GameObject(nombreObjeto, typeof(RectTransform));
        objetoUI.layer = LayerMask.NameToLayer("UI");
        objetoUI.transform.SetParent(padre, false);
        return objetoUI;
    }

    // Este metodo crea un texto de UI simple listo para usar.
    private Text CrearTextoRuntime(string nombreObjeto, Transform padre, Font fuente, int tamanoFuente, FontStyle estiloFuente, string contenidoInicial)
    {
        GameObject objetoTexto = CrearObjetoUI(nombreObjeto, padre);
        Text textoCreado = objetoTexto.AddComponent<Text>();
        textoCreado.font = fuente;
        textoCreado.fontSize = tamanoFuente;
        textoCreado.fontStyle = estiloFuente;
        textoCreado.alignment = TextAnchor.MiddleCenter;
        textoCreado.color = Color.white;
        textoCreado.text = contenidoInicial;

        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
        rectTexto.sizeDelta = new Vector2(700f, 42f);

        return textoCreado;
    }
}
