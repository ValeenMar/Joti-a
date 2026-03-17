using System.Collections;
using UnityEngine;

// Esta clase dibuja una barra de vida simple sobre el enemigo usando OnGUI.
[RequireComponent(typeof(VidaEnemigo))]
public class BarraVidaFlotante : MonoBehaviour
{
    // Este offset define a que altura se dibuja la barra sobre el enemigo.
    [SerializeField] private Vector3 offsetBarra = new Vector3(0f, 2.2f, 0f);

    // Este tamano define el ancho y alto de la barra en pantalla.
    [SerializeField] private Vector2 tamanoBarra = new Vector2(70f, 10f);

    // Esta velocidad suaviza el valor mostrado para que no cambie de golpe.
    [SerializeField] private float velocidadSuavizado = 8f;

    // Este color se usa para el fondo oscuro de la barra.
    [SerializeField] private Color colorFondo = new Color(0f, 0f, 0f, 0.75f);

    // Este color se usa para la parte rellena de la barra.
    [SerializeField] private Color colorRelleno = new Color(0.25f, 0.9f, 0.25f, 1f);

    // Si esta activo, oculta la barra poco despues de morir.
    [SerializeField] private bool ocultarTrasMuerte = true;

    // Esta referencia guarda el sistema de vida del enemigo.
    private VidaEnemigo vidaEnemigo;

    // Esta referencia guarda la camara principal para convertir mundo a pantalla.
    private Camera camaraPrincipal;

    // Este valor representa la vida objetivo normalizada.
    private float valorObjetivo = 1f;

    // Este valor representa la vida que estamos mostrando suavizada.
    private float valorMostrado = 1f;

    // Esta bandera indica si todavia debemos dibujar la barra.
    private bool mostrarBarra = true;

    // Esta funcion se ejecuta al iniciar componentes.
    private void Awake()
    {
        // Obtenemos la referencia a la vida del enemigo.
        vidaEnemigo = GetComponent<VidaEnemigo>();
    }

    // Esta funcion se ejecuta al habilitar el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento de cambio de vida.
        vidaEnemigo.AlVidaActualizada += ManejarVidaActualizada;

        // Nos suscribimos al evento de muerte para ocultar la barra.
        vidaEnemigo.AlMorir += ManejarMuerteEnemigo;
    }

    // Esta funcion se ejecuta al deshabilitar el componente.
    private void OnDisable()
    {
        // Quitamos la suscripcion al evento de vida.
        vidaEnemigo.AlVidaActualizada -= ManejarVidaActualizada;

        // Quitamos la suscripcion al evento de muerte.
        vidaEnemigo.AlMorir -= ManejarMuerteEnemigo;
    }

    // Esta funcion se ejecuta una vez por frame para suavizar el relleno.
    private void LateUpdate()
    {
        // Si no tenemos camara principal, intentamos obtenerla.
        if (camaraPrincipal == null)
        {
            // Guardamos la referencia a la camara principal actual.
            camaraPrincipal = Camera.main;
        }

        // Movemos el valor mostrado hacia el objetivo suavemente.
        valorMostrado = Mathf.MoveTowards(valorMostrado, valorObjetivo, velocidadSuavizado * Time.deltaTime);
    }

    // Esta funcion dibuja la barra en pantalla.
    private void OnGUI()
    {
        // Si no debemos mostrar la barra, no dibujamos nada.
        if (!mostrarBarra)
        {
            return;
        }

        // Si no hay camara principal disponible, no podemos convertir coordenadas.
        if (camaraPrincipal == null)
        {
            return;
        }

        // Calculamos la posicion del enemigo en pantalla usando el offset configurado.
        Vector3 posicionPantalla = camaraPrincipal.WorldToScreenPoint(transform.position + offsetBarra);

        // Si el punto quedo detras de la camara, no dibujamos la barra.
        if (posicionPantalla.z <= 0f)
        {
            return;
        }

        // Convertimos la coordenada Y de mundo a espacio GUI.
        float posicionX = posicionPantalla.x - tamanoBarra.x * 0.5f;
        float posicionY = Screen.height - posicionPantalla.y - tamanoBarra.y * 0.5f;

        // Armamos el rectangulo base de la barra.
        Rect rectanguloFondo = new Rect(posicionX, posicionY, tamanoBarra.x, tamanoBarra.y);

        // Armamos el rectangulo del relleno segun la vida actual mostrada.
        Rect rectanguloRelleno = new Rect(posicionX + 1f, posicionY + 1f, (tamanoBarra.x - 2f) * valorMostrado, tamanoBarra.y - 2f);

        // Guardamos el color GUI anterior para restaurarlo al final.
        Color colorAnterior = GUI.color;

        // Dibujamos primero el fondo oscuro.
        GUI.color = colorFondo;
        GUI.DrawTexture(rectanguloFondo, Texture2D.whiteTexture);

        // Dibujamos luego el relleno de vida.
        GUI.color = colorRelleno;
        GUI.DrawTexture(rectanguloRelleno, Texture2D.whiteTexture);

        // Restauramos el color original de la GUI.
        GUI.color = colorAnterior;
    }

    // Este metodo responde cada vez que cambia la vida actual.
    private void ManejarVidaActualizada(float vidaActual, float vidaMaxima)
    {
        // Si la vida maxima no es valida, forzamos la barra a vacia.
        if (vidaMaxima <= 0f)
        {
            valorObjetivo = 0f;
            return;
        }

        // Convertimos la vida actual en un valor entre 0 y 1.
        valorObjetivo = Mathf.Clamp01(vidaActual / vidaMaxima);
    }

    // Este metodo responde cuando el enemigo muere.
    private void ManejarMuerteEnemigo(DatosDanio datosDanio)
    {
        // Forzamos la barra a vacia.
        valorObjetivo = 0f;

        // Si la configuracion dice que siga visible, terminamos aca.
        if (!ocultarTrasMuerte)
        {
            return;
        }

        // Iniciamos una pequena espera antes de ocultarla por completo.
        StartCoroutine(RutinaOcultarBarra());
    }

    // Esta corrutina apaga el dibujo de la barra tras una pausa corta.
    private IEnumerator RutinaOcultarBarra()
    {
        // Esperamos un instante para que se vea el vaciado final.
        yield return new WaitForSeconds(0.2f);

        // Dejamos de dibujar la barra desde este momento.
        mostrarBarra = false;
    }
}
