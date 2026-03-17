using UnityEngine;

// Esta clase lleva la cuenta de kills seguidos sin recibir danio.
public class SistemaRacha : MonoBehaviour
{
    // Esta referencia apunta al jugador dueno de la racha.
    [SerializeField] private GameObject jugadorPropietario;

    // Esta posicion define donde aparece el mensaje en pantalla.
    [SerializeField] private Vector2 posicionMensaje = new Vector2(20f, 80f);

    // Este valor guarda la racha actual.
    [SerializeField] private int rachaActual = 0;

    // Este valor guarda la mejor racha lograda en la partida.
    [SerializeField] private int mejorRacha = 0;

    // Este texto guarda el ultimo mensaje visual de racha.
    private string mensajeActual = string.Empty;

    // Este color guarda el color del mensaje actual.
    private Color colorMensajeActual = Color.white;

    // Este tiempo guarda hasta cuando debe verse el mensaje.
    private float tiempoFinMensaje;

    // Este estilo se usa para dibujar el mensaje de racha.
    private GUIStyle estiloMensaje;

    // Esta propiedad expone la racha actual.
    public int RachaActual => rachaActual;

    // Esta propiedad expone la mejor racha registrada.
    public int MejorRacha => mejorRacha;

    // Esta funcion se ejecuta cuando Unity crea el objeto.
    private void Awake()
    {
        // Si no hay jugador asignado, usamos este GameObject como dueno.
        if (jugadorPropietario == null)
        {
            jugadorPropietario = gameObject;
        }
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento de enemigo eliminado para aumentar racha.
        EventosJuego.AlEnemigoEliminado += ManejarEnemigoEliminado;

        // Nos suscribimos al evento de danio recibido para cortar racha.
        EventosJuego.AlJugadorRecibioDanio += ManejarJugadorRecibioDanio;

        // Nos suscribimos al evento de muerte para cortar racha.
        EventosJuego.AlJugadorMurio += ManejarJugadorMurio;
    }

    // Esta funcion se ejecuta al desactivar el componente.
    private void OnDisable()
    {
        // Removemos suscripcion del evento de kills.
        EventosJuego.AlEnemigoEliminado -= ManejarEnemigoEliminado;

        // Removemos suscripcion del evento de danio recibido.
        EventosJuego.AlJugadorRecibioDanio -= ManejarJugadorRecibioDanio;

        // Removemos suscripcion del evento de muerte.
        EventosJuego.AlJugadorMurio -= ManejarJugadorMurio;
    }

    // Este metodo se ejecuta cuando un enemigo fue eliminado.
    private void ManejarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        // En Mirror, esta validacion y conteo deberia vivir en el servidor.
        if (atacante != jugadorPropietario)
        {
            return;
        }

        // Aumentamos la racha actual en uno.
        rachaActual++;

        // Actualizamos mejor racha si superamos el record.
        mejorRacha = Mathf.Max(mejorRacha, rachaActual);

        // Avisamos al resto de sistemas que la racha cambio.
        EventosJuego.NotificarRachaActualizada(jugadorPropietario, rachaActual);

        // Mostramos un feedback escalado segun umbral.
        MostrarFeedbackPorUmbral();
    }

    // Este metodo se ejecuta cuando un jugador recibe danio.
    private void ManejarJugadorRecibioDanio(GameObject jugador, float cantidadDanio)
    {
        // Si el jugador daniado no es el dueno de esta racha, ignoramos.
        if (jugador != jugadorPropietario)
        {
            return;
        }

        // Cortamos la racha porque recibio danio.
        ReiniciarRacha();
    }

    // Este metodo se ejecuta cuando un jugador muere.
    private void ManejarJugadorMurio(GameObject jugador)
    {
        // Si no es nuestro jugador, no hacemos nada.
        if (jugador != jugadorPropietario)
        {
            return;
        }

        // Cortamos la racha porque murio.
        ReiniciarRacha();
    }

    // Este metodo corta la racha actual y notifica el cambio.
    public void ReiniciarRacha()
    {
        // Si ya estaba en cero, evitamos eventos repetidos.
        if (rachaActual <= 0)
        {
            return;
        }

        // Volvemos la racha al estado base.
        rachaActual = 0;

        // Avisamos al sistema que la racha se perdio.
        EventosJuego.NotificarRachaPerdida(jugadorPropietario);

        // Avisamos tambien que el valor actual quedo en cero.
        EventosJuego.NotificarRachaActualizada(jugadorPropietario, rachaActual);

        // Mostramos feedback visual de corte.
        MostrarFeedbackTexto("RACHA CORTADA", new Color(0.9f, 0.25f, 0.25f), 0.7f);
    }

    // Este metodo decide que feedback mostrar segun la racha.
    private void MostrarFeedbackPorUmbral()
    {
        // Si la racha llego a 10 o mas, mostramos el feedback mas fuerte.
        if (rachaActual >= 10)
        {
            MostrarFeedbackTexto("RACHA x" + rachaActual + " - LEGENDARIA", new Color(1f, 0.8f, 0.2f), 1f);
            return;
        }

        // Si la racha llego a 5 o mas, mostramos feedback intermedio.
        if (rachaActual >= 5)
        {
            MostrarFeedbackTexto("RACHA x" + rachaActual + " - IMPLACABLE", new Color(1f, 0.5f, 0.2f), 0.85f);
            return;
        }

        // Si la racha llego a 3 o mas, mostramos feedback inicial.
        if (rachaActual >= 3)
        {
            MostrarFeedbackTexto("RACHA x" + rachaActual, new Color(0.35f, 0.85f, 1f), 0.7f);
        }
    }

    // Este metodo guarda el mensaje temporal que OnGUI va a dibujar.
    private void MostrarFeedbackTexto(string mensaje, Color color, float intensidadPanel)
    {
        // Guardamos el mensaje actual para que OnGUI pueda dibujarlo.
        mensajeActual = mensaje;

        // Guardamos el color del mensaje actual.
        colorMensajeActual = color;

        // Calculamos cuanto tiempo queda visible el mensaje.
        tiempoFinMensaje = Time.unscaledTime + Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(intensidadPanel));
    }

    // Esta funcion prepara el estilo del mensaje si todavia no existe.
    private void AsegurarEstilo()
    {
        // Si ya existe el estilo, no hacemos nada.
        if (estiloMensaje != null)
        {
            return;
        }

        // Creamos un estilo basado en la skin actual.
        estiloMensaje = new GUIStyle(GUI.skin.label);

        // Aumentamos el tamano del texto para que se note mejor.
        estiloMensaje.fontSize = 18;

        // Lo ponemos en negrita para hacerlo mas impactante.
        estiloMensaje.fontStyle = FontStyle.Bold;
    }

    // Esta funcion dibuja el mensaje temporal de racha en pantalla.
    private void OnGUI()
    {
        // Si ya paso el tiempo del mensaje, no dibujamos nada.
        if (Time.unscaledTime >= tiempoFinMensaje)
        {
            return;
        }

        // Nos aseguramos de que el estilo exista.
        AsegurarEstilo();

        // Calculamos cuanto falta para que desaparezca.
        float tiempoRestante = Mathf.Clamp01(tiempoFinMensaje - Time.unscaledTime);

        // Guardamos el color anterior de la GUI.
        Color colorAnterior = GUI.color;

        // Aplicamos el color del mensaje con un alpha que va bajando.
        GUI.color = new Color(colorMensajeActual.r, colorMensajeActual.g, colorMensajeActual.b, tiempoRestante);

        // Dibujamos el mensaje de racha.
        GUI.Label(new Rect(posicionMensaje.x, posicionMensaje.y, 420f, 28f), mensajeActual, estiloMensaje);

        // Restauramos el color anterior de la GUI.
        GUI.color = colorAnterior;
    }
}
