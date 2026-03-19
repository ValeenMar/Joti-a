using System;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla las pociones del jugador, su curacion,
// una HUD simple en pantalla y el uso por tecla o boton.

// Esta clase administra la cantidad de pociones y cura al jugador cuando las usa.
public class SistemaPociones : MonoBehaviour
{
    // Este evento avisa cuando una pocion se uso correctamente.
    public static event Action<GameObject, Vector3> AlPocionUsada;

    // Esta referencia apunta al sistema de vida del mismo jugador.
    [SerializeField] private VidaJugador vidaJugador;

    // Esta tecla permite usar una pocion sin tocar UI.
    [SerializeField] private KeyCode teclaUsarPocion = KeyCode.H;

    // Esta es la cantidad inicial de pociones al empezar la partida.
    [SerializeField] private int cantidadInicialPociones = 3;

    // Esta es la cantidad maxima que el jugador puede almacenar.
    [SerializeField] private int cantidadMaximaPociones = 5;

    // Esta variable guarda cuantas pociones quedan ahora mismo.
    [SerializeField] private int cantidadActualPociones = 3;

    // Este valor define cuanta vida cura una pocion.
    [SerializeField] private float curacionPorPocion = 35f;

    // Este valor define cuanto tarda en poder volver a usar otra pocion.
    [SerializeField] private float enfriamientoUso = 0.35f;

    // Esta posicion define donde se dibuja el HUD de pociones.
    [SerializeField] private Vector2 posicionHud = new Vector2(20f, 100f);

    // Este color se usa cuando aun quedan pociones.
    [SerializeField] private Color colorHudNormal = Color.white;

    // Este color se usa cuando ya no quedan pociones.
    [SerializeField] private Color colorHudSinPociones = new Color(1f, 0.35f, 0.35f, 1f);

    // Esta variable guarda hasta cuando sigue activo el enfriamiento.
    private float tiempoFinEnfriamiento;

    // Esta referencia guarda el estilo de texto del HUD.
    private GUIStyle estiloHud;

    // Esta referencia guarda el rectangulo del boton dibujado por OnGUI.
    private Rect rectBotonUsarPocion;

    // Este valor define cada cuanto reintentamos recuperar VidaJugador si se pierde tras reload.
    [SerializeField] private float intervaloRebindVida = 0.5f;

    // Esta variable guarda el momento minimo para volver a intentar el rebind.
    private float tiempoProximoRebindVida;

    // Esta propiedad expone cuantas pociones quedan.
    public int CantidadActualPociones => cantidadActualPociones;

    // Esta propiedad expone cuantas pociones como maximo se pueden guardar.
    public int CantidadMaximaPociones => cantidadMaximaPociones;

    // Esta funcion se ejecuta al iniciar el componente.
    private void Awake()
    {
        // Si no se asigno VidaJugador, intentamos tomarla del mismo objeto.
        if (vidaJugador == null)
        {
            vidaJugador = GetComponent<VidaJugador>();
        }

        // Aseguramos que la cantidad inicial quede dentro de limites validos.
        cantidadActualPociones = Mathf.Clamp(cantidadInicialPociones, 0, cantidadMaximaPociones);

        // Definimos el rectangulo del boton visual.
        rectBotonUsarPocion = new Rect(posicionHud.x, posicionHud.y + 26f, 150f, 28f);
    }

    // Esta funcion corre despues de Awake y ayuda a recuperar referencias tras reinicio de escena.
    private void Start()
    {
        // Reintentamos vincular VidaJugador por si se perdio despues de un reload.
        RebindVidaJugadorSiHaceFalta(true);
    }

    // Esta funcion corre cada frame para leer la tecla de uso.
    private void Update()
    {
        // Reintentamos vincular VidaJugador de forma dinamica por robustez post-restart.
        RebindVidaJugadorSiHaceFalta(false);

        // Si se apreto H (o la tecla configurada), registramos el intento de usar pocion.
        bool inputPocionDetectado = Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(teclaUsarPocion);

        // Si no tenemos vida jugador, no podemos usar pociones.
        if (vidaJugador == null)
        {
            return;
        }

        // Si la partida termino en game over, no permitimos usar pociones.
        if (GameManager.Instancia != null && GameManager.Instancia.EstadoActual == EstadoPartida.GameOver)
        {
            return;
        }

        // Si se detecto input de pocion, intentamos consumirla.
        if (inputPocionDetectado)
        {
            IntentarUsarPocion();
        }
    }

    // Este metodo recupera la referencia de VidaJugador si se pierde tras reinicios o recargas.
    private void RebindVidaJugadorSiHaceFalta(bool forzarIntento)
    {
        // Si ya existe la referencia, no hace falta buscar.
        if (vidaJugador != null)
        {
            return;
        }

        // Si no forzamos y aun no toca reintentar, salimos para evitar costo cada frame.
        if (!forzarIntento && Time.unscaledTime < tiempoProximoRebindVida)
        {
            return;
        }

        // Definimos cuando sera el siguiente reintento automatico.
        tiempoProximoRebindVida = Time.unscaledTime + Mathf.Max(0.1f, intervaloRebindVida);

        // Primer intento: buscar VidaJugador en el mismo objeto.
        vidaJugador = GetComponent<VidaJugador>();

        // Segundo intento: buscar VidaJugador en padre (por si este script esta en un hijo).
        if (vidaJugador == null)
        {
            vidaJugador = GetComponentInParent<VidaJugador>();
        }

        // Tercer intento: usar el jugador con tag Player como ruta rapida tras reinicio.
        if (vidaJugador == null)
        {
            GameObject jugadorConTag = null;

            try
            {
                jugadorConTag = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
                jugadorConTag = null;
            }

            if (jugadorConTag != null)
            {
                vidaJugador = jugadorConTag.GetComponent<VidaJugador>();
            }
        }

        // Cuarto intento: buscar cualquier VidaJugador activa o inactiva en la escena.
        if (vidaJugador == null)
        {
            vidaJugador = FindObjectOfType<VidaJugador>(true);
        }
    }

    // Este metodo intenta gastar una pocion y curar al jugador.
    public bool IntentarUsarPocion()
    {
        // Si no hay vida configurada, no podemos curar.
        if (vidaJugador == null)
        {
            return false;
        }

        // Si el jugador ya esta muerto, no permitimos usarla.
        if (!vidaJugador.EstaVivo)
        {
            return false;
        }

        // Si estamos en enfriamiento, no dejamos usar otra todavia.
        if (Time.time < tiempoFinEnfriamiento)
        {
            return false;
        }

        // Si no queda ninguna pocion, no hacemos nada.
        if (cantidadActualPociones <= 0)
        {
            return false;
        }

        // Si la vida ya esta llena, no desperdiciamos una pocion.
        if (vidaJugador.VidaActual >= vidaJugador.VidaMaxima)
        {
            return false;
        }

        // Restamos una pocion del inventario.
        cantidadActualPociones--;

        // Curamos la vida configurada al jugador.
        vidaJugador.Curar(curacionPorPocion);

        // Iniciamos el enfriamiento para el siguiente uso.
        tiempoFinEnfriamiento = Time.time + enfriamientoUso;

        // Mostramos en consola el resultado real para evitar confundir input con curacion exitosa.
        Debug.Log("Poción usada -> vida actual: " + vidaJugador.VidaActual + " / " + vidaJugador.VidaMaxima + " | pociones restantes: " + cantidadActualPociones + " / " + cantidadMaximaPociones);

        // Avisamos al sistema visual que se uso una pocion para disparar particulas.
        AlPocionUsada?.Invoke(vidaJugador.gameObject, vidaJugador.transform.position);

        // Devolvemos verdadero para avisar que si se uso.
        return true;
    }

    // Este metodo suma pociones sin superar el maximo permitido.
    public void AgregarPociones(int cantidad)
    {
        // Si la cantidad es invalida, no hacemos nada.
        if (cantidad <= 0)
        {
            return;
        }

        // Sumamos y limitamos al maximo configurado.
        cantidadActualPociones = Mathf.Clamp(cantidadActualPociones + cantidad, 0, cantidadMaximaPociones);
    }

    // Este metodo recarga las pociones al maximo.
    public void RellenarPociones()
    {
        // Llevamos la cantidad actual al maximo configurado.
        cantidadActualPociones = cantidadMaximaPociones;
    }

    // Este metodo prepara el estilo del HUD si todavia no existe.
    private void PrepararEstiloSiHaceFalta()
    {
        // Si ya existe, no hace falta crearlo otra vez.
        if (estiloHud != null)
        {
            return;
        }

        // Creamos un estilo basico a partir de la skin actual.
        estiloHud = new GUIStyle(GUI.skin.label);

        // Subimos un poco el tamaño para que se lea mejor.
        estiloHud.fontSize = 16;

        // Ponemos el texto en negrita para destacarlo.
        estiloHud.fontStyle = FontStyle.Bold;
    }

    // Esta funcion dibuja un HUD simple de pociones sin necesitar armar UI manual.
    private void OnGUI()
    {
        // Si no existe vida jugador, no dibujamos el HUD.
        if (vidaJugador == null)
        {
            return;
        }

        // Preparamos el estilo si todavia no esta listo.
        PrepararEstiloSiHaceFalta();

        // Guardamos el color anterior para restaurarlo despues.
        Color colorAnterior = GUI.color;

        // Elegimos color rojo si no quedan pociones y blanco si aun quedan.
        GUI.color = cantidadActualPociones > 0 ? colorHudNormal : colorHudSinPociones;

        // Dibujamos la etiqueta con la cantidad actual.
        GUI.Label(new Rect(posicionHud.x, posicionHud.y, 260f, 24f), "Pociones: " + cantidadActualPociones + " / " + cantidadMaximaPociones + "  [" + teclaUsarPocion + "]", estiloHud);

        // Deshabilitamos el boton si no se puede usar en este momento.
        bool estadoGuiAnterior = GUI.enabled;
        GUI.enabled = cantidadActualPociones > 0 && vidaJugador.VidaActual < vidaJugador.VidaMaxima && Time.time >= tiempoFinEnfriamiento && vidaJugador.EstaVivo;

        // Dibujamos un boton manual por si el usuario quiere clickearlo.
        if (GUI.Button(rectBotonUsarPocion, "Usar pocion"))
        {
            // Intentamos consumir una pocion desde boton.
            bool usoExitoso = IntentarUsarPocion();

            // Si realmente se uso, dejamos traza de depuracion.
            if (usoExitoso)
            {
                Debug.Log("Poción usada");
            }
        }

        // Restauramos el estado anterior del GUI.
        GUI.enabled = estadoGuiAnterior;

        // Restauramos el color anterior del GUI.
        GUI.color = colorAnterior;
    }

    // COPILOT-EXPAND: Aqui podes agregar pickups de pociones, distintas calidades y curacion gradual en el tiempo.
}
