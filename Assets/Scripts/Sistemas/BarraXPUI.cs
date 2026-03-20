using UnityEngine;

// Esta clase dibuja una barra de experiencia simple en pantalla usando OnGUI.
public class BarraXPUI : MonoBehaviour
{
    // Esta referencia apunta al sistema de experiencia que vamos a mostrar.
    [SerializeField] private SistemaXP sistemaXP;

    // Esta posicion define donde arranca la barra en pantalla.
    [SerializeField] private Vector2 posicionPantalla = new Vector2(20f, 20f);

    // Este tamano define el ancho y alto de la barra de experiencia.
    [SerializeField] private Vector2 tamanoBarra = new Vector2(260f, 18f);

    // Este color se usa para el fondo de la barra.
    [SerializeField] private Color colorFondo = new Color(0f, 0f, 0f, 0.7f);

    // Este color se usa para el relleno de experiencia.
    [SerializeField] private Color colorRelleno = new Color(0.85f, 0.7f, 0.2f, 1f);

    // Este estilo se usa para el texto principal.
    private GUIStyle estiloTexto;

    // Esta funcion se ejecuta cuando se activa el componente.
    private void OnEnable()
    {
        // Si no hay sistema asignado, intentamos encontrar uno en la escena.
        if (sistemaXP == null)
        {
            sistemaXP = FindObjectOfType<SistemaXP>();
        }
    }

    // Esta funcion prepara estilos GUI la primera vez que se necesitan.
    private void AsegurarEstilos()
    {
        // Si el estilo ya existe, no volvemos a crearlo.
        if (estiloTexto != null)
        {
            return;
        }

        // Creamos un estilo basado en la skin por defecto.
        estiloTexto = new GUIStyle(GUI.skin.label);

        // Hacemos el texto un poco mas grande para leerlo mejor.
        estiloTexto.fontSize = 15;

        // Elegimos un color claro para que contraste con el fondo.
        estiloTexto.normal.textColor = Color.white;
    }

    // Esta funcion dibuja la barra y los textos en pantalla.
    private void OnGUI()
    {
        // Si ya existe la UI moderna del jugador, no dibujamos esta barra legacy.
        if (UIJugador.HUDPrincipalActivo)
        {
            return;
        }

        // Si no hay sistema XP disponible, no dibujamos nada.
        if (sistemaXP == null)
        {
            return;
        }

        // Nos aseguramos de tener el estilo listo para los textos.
        AsegurarEstilos();

        // Leemos la experiencia actual del nivel.
        float experienciaActual = sistemaXP.ExperienciaActualNivel;

        // Leemos la experiencia requerida para subir.
        float experienciaRequerida = sistemaXP.ExperienciaRequeridaNivelActual;

        // Calculamos el porcentaje de progreso.
        float progreso = experienciaRequerida > 0f ? experienciaActual / experienciaRequerida : 0f;

        // Armamos el rectangulo del fondo de la barra.
        Rect rectanguloFondo = new Rect(posicionPantalla.x, posicionPantalla.y + 22f, tamanoBarra.x, tamanoBarra.y);

        // Armamos el rectangulo del relleno segun el progreso.
        Rect rectanguloRelleno = new Rect(posicionPantalla.x + 1f, posicionPantalla.y + 23f, (tamanoBarra.x - 2f) * Mathf.Clamp01(progreso), tamanoBarra.y - 2f);

        // Guardamos el color GUI anterior para restaurarlo despues.
        Color colorAnterior = GUI.color;

        // Dibujamos el texto del nivel.
        GUI.Label(new Rect(posicionPantalla.x, posicionPantalla.y, 220f, 20f), "Nivel " + sistemaXP.NivelActual, estiloTexto);

        // Dibujamos el fondo de la barra.
        GUI.color = colorFondo;
        GUI.DrawTexture(rectanguloFondo, Texture2D.whiteTexture);

        // Dibujamos el relleno de experiencia.
        GUI.color = colorRelleno;
        GUI.DrawTexture(rectanguloRelleno, Texture2D.whiteTexture);

        // Restauramos el color GUI.
        GUI.color = colorAnterior;

        // Dibujamos el texto de experiencia actual.
        GUI.Label(new Rect(posicionPantalla.x, posicionPantalla.y + 42f, 260f, 20f), Mathf.RoundToInt(experienciaActual) + " / " + Mathf.RoundToInt(experienciaRequerida) + " XP", estiloTexto);
    }

    // Este metodo queda disponible por compatibilidad con el flujo original.
    public void RefrescarUI()
    {
        // OnGUI dibuja cada frame, asi que no necesitamos logica extra aca.
    }
}
