using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Esta clase controla el freeze frame, el flash critico y el crosshair del centro de pantalla.
public class FeedbackPantalla : MonoBehaviour
{
    // Esta referencia apunta al jugador del que queremos escuchar ataques e impactos.
    [SerializeField] private GameObject jugadorObjetivo;

    // Esta referencia apunta al sistema de espada para saber si hay un enemigo en rango.
    [SerializeField] private SistemaEspada sistemaEspada;

    // Estas referencias apuntan a las cuatro lineas del crosshair.
    [SerializeField] private RectTransform lineaSuperior;
    [SerializeField] private RectTransform lineaInferior;
    [SerializeField] private RectTransform lineaIzquierda;
    [SerializeField] private RectTransform lineaDerecha;

    // Esta lista contiene las imagenes del crosshair para cambiarles el color juntas.
    [SerializeField] private Graphic[] graficosCrosshair;

    // Esta imagen cubre toda la pantalla y se usa para el flash critico.
    [SerializeField] private Image imagenFlashPantalla;

    // Este color es el color normal del crosshair cuando no hay objetivo.
    [SerializeField] private Color colorCrosshairNormal = Color.white;

    // Este color se usa cuando hay un enemigo realmente en rango de ataque.
    [SerializeField] private Color colorCrosshairObjetivo = new Color(1f, 0.25f, 0.25f, 1f);

    // Este color se usa para el flash rapido de golpe critico.
    [SerializeField] private Color colorFlashCritico = new Color(1f, 1f, 1f, 0.75f);

    // Este valor define la separacion base entre el centro y las lineas del crosshair.
    [SerializeField] private float separacionBaseCrosshair = 8f;

    // Este valor define cuanto se abre el crosshair cuando atacamos.
    [SerializeField] private float separacionExpandidaCrosshair = 16f;

    // Este valor define cuan rapido vuelve el crosshair a su separacion base.
    [SerializeField] private float velocidadRetornoCrosshair = 60f;

    // Este valor define cuanto dura el freeze frame de un golpe fuerte.
    [SerializeField] private float duracionFreezeGolpeFuerte = 0.08f;

    // Este valor define cuanto dura el freeze frame de un golpe critico.
    [SerializeField] private float duracionFreezeGolpeCritico = 0.15f;

    // Este valor define cuanto tarda en apagarse el flash critico.
    [SerializeField] private float duracionFlashCritico = 0.12f;

    // Esta variable guarda la separacion actual del crosshair en pantalla.
    private float separacionActualCrosshair;

    // Esta variable guarda el alpha actual del flash critico.
    private float alphaFlashActual;

    // Esta referencia guarda la corrutina activa del freeze frame.
    private Coroutine corrutinaFreezeActiva;

    // Esta referencia guarda la corrutina activa del flash critico.
    private Coroutine corrutinaFlashActiva;

    // Esta variable guarda el timeScale previo para restaurarlo tras el freeze.
    private float timeScaleAnterior = 1f;

    // Esta funcion se ejecuta cuando el objeto se crea.
    private void Awake()
    {
        // Si no se asigno jugador objetivo pero si existe un sistema de espada, usamos ese jugador.
        if (jugadorObjetivo == null && sistemaEspada != null)
        {
            jugadorObjetivo = sistemaEspada.gameObject;
        }

        // Si no se asigno sistema de espada pero si hay jugador, intentamos tomar el del mismo jugador.
        if (sistemaEspada == null && jugadorObjetivo != null)
        {
            sistemaEspada = jugadorObjetivo.GetComponent<SistemaEspada>();
        }

        // Arrancamos con el crosshair en su separacion base.
        separacionActualCrosshair = separacionBaseCrosshair;

        // Actualizamos una vez la posicion del crosshair para que arranque bien.
        ActualizarPosicionCrosshair();

        // Si hay una imagen de flash, la dejamos invisible al iniciar.
        if (imagenFlashPantalla != null)
        {
            imagenFlashPantalla.color = new Color(colorFlashCritico.r, colorFlashCritico.g, colorFlashCritico.b, 0f);
        }
    }

    // Esta funcion se ejecuta al habilitar el componente y se suscribe a eventos globales.
    private void OnEnable()
    {
        // Nos suscribimos al evento global de dano aplicado.
        EventosJuego.AlAplicarDanio += ManejarDanioAplicado;

        // Nos suscribimos al evento global de ataque lanzado.
        EventosJuego.AlJugadorLanzoAtaque += ManejarJugadorLanzoAtaque;
    }

    // Esta funcion se ejecuta al deshabilitar el componente y limpia suscripciones.
    private void OnDisable()
    {
        // Dejamos de escuchar el evento de dano aplicado.
        EventosJuego.AlAplicarDanio -= ManejarDanioAplicado;

        // Dejamos de escuchar el evento de ataque lanzado.
        EventosJuego.AlJugadorLanzoAtaque -= ManejarJugadorLanzoAtaque;

        // Si habia un freeze activo, restauramos el timeScale al salir por seguridad.
        if (corrutinaFreezeActiva != null)
        {
            Time.timeScale = timeScaleAnterior;
        }
    }

    // Esta funcion corre cada frame para animar crosshair y flash con tiempo no escalado.
    private void Update()
    {
        // Si no se asigno jugador pero si existe sistema de espada, lo recuperamos.
        if (jugadorObjetivo == null && sistemaEspada != null)
        {
            jugadorObjetivo = sistemaEspada.gameObject;
        }

        // Si no se asigno sistema de espada pero si existe el jugador, lo recuperamos.
        if (sistemaEspada == null && jugadorObjetivo != null)
        {
            sistemaEspada = jugadorObjetivo.GetComponent<SistemaEspada>();
        }

        // Hacemos que el crosshair vuelva suavemente a su separacion base.
        separacionActualCrosshair = Mathf.MoveTowards(separacionActualCrosshair, separacionBaseCrosshair, velocidadRetornoCrosshair * Time.unscaledDeltaTime);

        // Aplicamos la separacion actual a las cuatro lineas.
        ActualizarPosicionCrosshair();

        // Actualizamos el color del crosshair segun si hay objetivo en rango.
        ActualizarColorCrosshair();

        // Si existe imagen de flash, actualizamos su alpha visible.
        if (imagenFlashPantalla != null)
        {
            imagenFlashPantalla.color = new Color(colorFlashCritico.r, colorFlashCritico.g, colorFlashCritico.b, alphaFlashActual);
        }
    }

    // Este metodo responde cuando el jugador lanza cualquier ataque.
    private void ManejarJugadorLanzoAtaque(GameObject jugador, bool esGolpeFuerte)
    {
        // Si el ataque no lo hizo nuestro jugador, no tocamos este crosshair.
        if (jugador != jugadorObjetivo)
        {
            return;
        }

        // Abrimos un poco el crosshair al atacar para dar respuesta visual inmediata.
        separacionActualCrosshair = separacionExpandidaCrosshair;
    }

    // Este metodo responde cuando se aplico realmente un dano en el juego.
    private void ManejarDanioAplicado(DatosDanio datosDanio)
    {
        // Si no llegaron datos validos, no hacemos nada.
        if (datosDanio == null)
        {
            return;
        }

        // Si el atacante no fue nuestro jugador, ignoramos este impacto.
        if (datosDanio.atacante != jugadorObjetivo)
        {
            return;
        }

        // Si el golpe fue critico, priorizamos el freeze largo y el flash blanco.
        if (datosDanio.FueCritico)
        {
            SolicitarFreezeFrame(duracionFreezeGolpeCritico);
            ReproducirFlashCritico();
            return;
        }

        // Si no fue critico pero si fue un golpe fuerte, hacemos un freeze corto.
        if (datosDanio.esGolpeFuerte)
        {
            SolicitarFreezeFrame(duracionFreezeGolpeFuerte);
        }

        // En Mirror, este feedback visual deberia dispararse solo en el cliente local.
        // [Mirror futuro] El servidor validaria el dano y luego un [TargetRpc] activaria este efecto local.
    }

    // Este metodo solicita un freeze frame y reemplaza uno anterior si llega uno mas nuevo.
    private void SolicitarFreezeFrame(float duracion)
    {
        // Si ya habia un freeze activo, lo detenemos y restauramos el timeScale antes de iniciar otro.
        if (corrutinaFreezeActiva != null)
        {
            StopCoroutine(corrutinaFreezeActiva);
            Time.timeScale = timeScaleAnterior;
            corrutinaFreezeActiva = null;
        }

        // Lanzamos una nueva corrutina de freeze con la duracion pedida.
        corrutinaFreezeActiva = StartCoroutine(CorrutinaFreezeFrame(duracion));
    }

    // Esta corrutina congela el tiempo del juego por una duracion real muy corta.
    private IEnumerator CorrutinaFreezeFrame(float duracion)
    {
        // Guardamos el timeScale actual para restaurarlo exactamente despues.
        timeScaleAnterior = Time.timeScale;

        // Congelamos el juego.
        Time.timeScale = 0f;

        // Esperamos tiempo real para que el freeze funcione aunque el juego este detenido.
        yield return new WaitForSecondsRealtime(duracion);

        // Restauramos el timeScale anterior.
        Time.timeScale = timeScaleAnterior;

        // Limpiamos la referencia de corrutina activa.
        corrutinaFreezeActiva = null;
    }

    // Este metodo reproduce un flash blanco rapido para golpes criticos.
    private void ReproducirFlashCritico()
    {
        // Si no existe una imagen de flash, no podemos mostrar nada visual.
        if (imagenFlashPantalla == null)
        {
            return;
        }

        // Si ya habia un flash activo, lo detenemos para reiniciarlo limpio.
        if (corrutinaFlashActiva != null)
        {
            StopCoroutine(corrutinaFlashActiva);
        }

        // Lanzamos la corrutina nueva del flash critico.
        corrutinaFlashActiva = StartCoroutine(CorrutinaFlashCritico());
    }

    // Esta corrutina enciende y apaga rapidamente el flash de pantalla.
    private IEnumerator CorrutinaFlashCritico()
    {
        // Arrancamos con el alpha maximo del flash configurado.
        alphaFlashActual = colorFlashCritico.a;

        // Guardamos el tiempo transcurrido de la animacion.
        float tiempo = 0f;

        // Mientras dure la animacion, vamos bajando el alpha.
        while (tiempo < duracionFlashCritico)
        {
            // Sumamos tiempo no escalado para que siga funcionando durante freeze frames.
            tiempo += Time.unscaledDeltaTime;

            // Calculamos progreso normalizado entre 0 y 1.
            float progreso = Mathf.Clamp01(tiempo / duracionFlashCritico);

            // Bajamos el alpha desde el valor inicial hasta cero.
            alphaFlashActual = Mathf.Lerp(colorFlashCritico.a, 0f, progreso);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Aseguramos que el flash quede completamente apagado.
        alphaFlashActual = 0f;

        // Limpiamos referencia de corrutina activa.
        corrutinaFlashActiva = null;
    }

    // Este metodo recoloca las cuatro lineas del crosshair segun la separacion actual.
    private void ActualizarPosicionCrosshair()
    {
        // Si existe la linea superior, la colocamos por encima del centro.
        if (lineaSuperior != null)
        {
            lineaSuperior.anchoredPosition = new Vector2(0f, separacionActualCrosshair);
        }

        // Si existe la linea inferior, la colocamos por debajo del centro.
        if (lineaInferior != null)
        {
            lineaInferior.anchoredPosition = new Vector2(0f, -separacionActualCrosshair);
        }

        // Si existe la linea izquierda, la colocamos a la izquierda del centro.
        if (lineaIzquierda != null)
        {
            lineaIzquierda.anchoredPosition = new Vector2(-separacionActualCrosshair, 0f);
        }

        // Si existe la linea derecha, la colocamos a la derecha del centro.
        if (lineaDerecha != null)
        {
            lineaDerecha.anchoredPosition = new Vector2(separacionActualCrosshair, 0f);
        }
    }

    // Este metodo cambia el color del crosshair segun si hay un enemigo realmente en rango.
    private void ActualizarColorCrosshair()
    {
        // Si no hay graficos asignados, no podemos cambiar ningun color.
        if (graficosCrosshair == null || graficosCrosshair.Length == 0)
        {
            return;
        }

        // Elegimos el color segun si el sistema de espada detecta un objetivo valido.
        Color colorObjetivo = sistemaEspada != null && sistemaEspada.HayObjetivoEnRangoActual ? colorCrosshairObjetivo : colorCrosshairNormal;

        // Aplicamos ese color a cada grafico del crosshair.
        for (int indiceGrafico = 0; indiceGrafico < graficosCrosshair.Length; indiceGrafico++)
        {
            if (graficosCrosshair[indiceGrafico] != null)
            {
                graficosCrosshair[indiceGrafico].color = colorObjetivo;
            }
        }
    }
}
