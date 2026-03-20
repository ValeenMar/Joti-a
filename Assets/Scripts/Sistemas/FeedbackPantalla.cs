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

    // Esta referencia apunta a la vida del jugador para mostrar vignette y feedback de dano.
    [SerializeField] private VidaJugador vidaJugadorObjetivo;

    // Estas referencias apuntan a las cuatro lineas del crosshair.
    [SerializeField] private RectTransform lineaSuperior;
    [SerializeField] private RectTransform lineaInferior;
    [SerializeField] private RectTransform lineaIzquierda;
    [SerializeField] private RectTransform lineaDerecha;

    // Esta lista contiene las imagenes del crosshair para cambiarles el color juntas.
    [SerializeField] private Graphic[] graficosCrosshair;

    // Esta imagen cubre toda la pantalla y se usa para el flash critico.
    [SerializeField] private Image imagenFlashPantalla;

    // Esta intensidad define el flash rojo por dano normal.
    [SerializeField] private float duracionFlashDanioNormal = 0.2f;

    // Este color se usa para el flash rojo por dano normal.
    [SerializeField] private Color colorFlashDanioNormal = new Color(1f, 0.2f, 0.2f, 0.28f);

    // Este umbral define cuando empieza la vignette de vida baja.
    [SerializeField] private float umbralVidaVignette = 0.3f;

    // Esta intensidad define cuanto se nota la vignette cuando la vida esta baja.
    [SerializeField] private float intensidadVignette = 0.45f;

    // Este valor define la fuerza del pulso de vignette en vida baja.
    [SerializeField] private float velocidadPulsoVignette = 2.2f;

    // Este color es el color normal del crosshair cuando no hay objetivo.
    [SerializeField] private Color colorCrosshairNormal = Color.white;

    // Este color se usa cuando hay un enemigo realmente en rango de ataque.
    [SerializeField] private Color colorCrosshairObjetivo = new Color(1f, 0.25f, 0.25f, 1f);

    // Este color se usa cuando la mejor zona actual es la espalda.
    [SerializeField] private Color colorCrosshairEspalda = new Color(1f, 0.62f, 0.22f, 1f);

    // Este color se usa cuando la mejor zona actual es la cabeza.
    [SerializeField] private Color colorCrosshairCabeza = new Color(1f, 0.88f, 0.25f, 1f);

    // Este color se usa para el flash rapido de golpe critico.
    [SerializeField] private Color colorFlashCritico = new Color(1f, 1f, 1f, 0.75f);

    // Este color se usa para remarcar un parry exitoso.
    [SerializeField] private Color colorFlashParryExitoso = new Color(0.52f, 0.88f, 1f, 0.36f);

    // Este color se usa para remarcar un parry fallido.
    [SerializeField] private Color colorFlashParryFallido = new Color(1f, 0.54f, 0.16f, 0.20f);

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

    // Este valor define cuanto dura el flash de parry exitoso.
    [SerializeField] private float duracionFlashParryExitoso = 0.12f;

    // Este valor define cuanto dura el flash de parry fallido.
    [SerializeField] private float duracionFlashParryFallido = 0.10f;

    // Esta opcion muestra una ayudita textual bajo el crosshair para entender la zona del golpe.
    [SerializeField] private bool mostrarEtiquetaZonaObjetivo = true;

    // Esta variable guarda la separacion actual del crosshair en pantalla.
    private float separacionActualCrosshair;

    // Esta variable guarda el alpha actual del flash critico.
    private float alphaFlashActual;

    // Esta variable guarda el color actual del flash principal de pantalla.
    private Color colorFlashPantallaActual;

    // Esta variable guarda el alpha actual del flash rojo por dano normal.
    private float alphaFlashDanioNormal;

    // Esta referencia guarda la corrutina activa del freeze frame.
    private Coroutine corrutinaFreezeActiva;

    // Esta referencia guarda la corrutina activa del flash critico.
    private Coroutine corrutinaFlashActiva;

    // Esta referencia guarda la corrutina activa del flash por dano normal.
    private Coroutine corrutinaFlashDanioNormalActiva;

    // Esta variable guarda el timeScale previo para restaurarlo tras el freeze.
    private float timeScaleAnterior = 1f;

    // Esta textura simple se usa para dibujar overlays y una vignette manual de bordes.
    private Texture2D texturaVignette;

    // Este estilo dibuja la etiqueta simple de zona bajo el crosshair.
    private GUIStyle estiloEtiquetaZona;

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

        colorFlashPantallaActual = colorFlashCritico;

        // Si ya conocemos el jugador, intentamos vincular su vida para feedback de dano y vida baja.
        if (vidaJugadorObjetivo == null && jugadorObjetivo != null)
        {
            vidaJugadorObjetivo = jugadorObjetivo.GetComponent<VidaJugador>();
        }

        // Preparamos una textura simple para overlays de GUI.
        texturaVignette = Texture2D.whiteTexture;
    }

    // Esta funcion se ejecuta al habilitar el componente y se suscribe a eventos globales.
    private void OnEnable()
    {
        // Nos suscribimos al evento global de dano aplicado.
        EventosJuego.AlAplicarDanio += ManejarDanioAplicado;

        // Nos suscribimos al evento global de ataque lanzado.
        EventosJuego.AlJugadorLanzoAtaque += ManejarJugadorLanzoAtaque;

        // Nos suscribimos a los eventos del parry para mejorar la lectura del timing.
        EventosJuego.AlParryExitoso += ManejarParryExitoso;
        EventosJuego.AlParryFallido += ManejarParryFallido;
    }

    // Esta funcion se ejecuta al deshabilitar el componente y limpia suscripciones.
    private void OnDisable()
    {
        // Dejamos de escuchar el evento de dano aplicado.
        EventosJuego.AlAplicarDanio -= ManejarDanioAplicado;

        // Dejamos de escuchar el evento de ataque lanzado.
        EventosJuego.AlJugadorLanzoAtaque -= ManejarJugadorLanzoAtaque;

        // Dejamos de escuchar los eventos del parry.
        EventosJuego.AlParryExitoso -= ManejarParryExitoso;
        EventosJuego.AlParryFallido -= ManejarParryFallido;

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

        // Si aun no encontramos la vida del jugador, la reintentamos desde el mismo objeto.
        if (vidaJugadorObjetivo == null && jugadorObjetivo != null)
        {
            vidaJugadorObjetivo = jugadorObjetivo.GetComponent<VidaJugador>();
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
            imagenFlashPantalla.color = new Color(colorFlashPantallaActual.r, colorFlashPantallaActual.g, colorFlashPantallaActual.b, alphaFlashActual);
        }
    }

    // Este metodo dibuja una etiqueta corta para explicar donde pegaria mas fuerte el ataque actual.
    private void OnGUI()
    {
        // Si no tenemos la textura base de GUI, usamos la blanca de Unity.
        if (texturaVignette == null)
        {
            texturaVignette = Texture2D.whiteTexture;
        }

        // Si el estilo todavia no existe, lo construimos recien dentro de OnGUI para evitar errores de GUIUtility.
        if (estiloEtiquetaZona == null)
        {
            estiloEtiquetaZona = new GUIStyle(GUI.skin.label);
            estiloEtiquetaZona.alignment = TextAnchor.MiddleCenter;
            estiloEtiquetaZona.fontStyle = FontStyle.Bold;
            estiloEtiquetaZona.fontSize = 15;
            estiloEtiquetaZona.normal.textColor = Color.white;
        }

        // Dibujamos primero el feedback rojo por dano normal y por vida baja.
        DibujarOverlayDanioYVidaBaja();

        // Si no queremos la ayuda o falta la espada, no dibujamos la etiqueta.
        if (!mostrarEtiquetaZonaObjetivo || sistemaEspada == null)
        {
            return;
        }

        // Solo mostramos la etiqueta si realmente hay un enemigo valido en rango.
        if (!sistemaEspada.HayObjetivoEnRangoActual)
        {
            return;
        }

        // Elegimos el texto segun la mejor zona actual.
        string textoZona = ObtenerTextoZonaActual(sistemaEspada.TipoZonaObjetivoActual);

        // Ajustamos el color para que se entienda rapido la importancia del golpe.
        estiloEtiquetaZona.normal.textColor = ObtenerColorCrosshairPorZona(sistemaEspada.TipoZonaObjetivoActual);

        // Dibujamos la etiqueta apenas debajo del crosshair.
        Rect rectangulo = new Rect((Screen.width * 0.5f) - 100f, (Screen.height * 0.5f) + 20f, 200f, 24f);
        GUI.Label(rectangulo, textoZona, estiloEtiquetaZona);
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

        // Si el golpe fue contra nuestro jugador, reproducimos un flash rojo corto.
        if (datosDanio.objetivo == jugadorObjetivo)
        {
            ReproducirFlashDanioNormal();
            return;
        }

        // Si el atacante no fue nuestro jugador, ignoramos este impacto ofensivo.
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
        ReproducirFlashPantalla(colorFlashCritico, duracionFlashCritico);
    }

    // Este metodo reproduce un flash rojo corto cuando el jugador recibe daño normal.
    private void ReproducirFlashDanioNormal()
    {
        // Si ya habia un flash rojo activo, lo reiniciamos limpio.
        if (corrutinaFlashDanioNormalActiva != null)
        {
            StopCoroutine(corrutinaFlashDanioNormalActiva);
        }

        // Lanzamos la rutina del flash rojo corto.
        corrutinaFlashDanioNormalActiva = StartCoroutine(CorrutinaFlashDanioNormal());
    }

    // Esta corrutina enciende y apaga rapidamente el flash de pantalla.
    private void ManejarParryExitoso(GameObject jugador, GameObject enemigo)
    {
        if (jugador != jugadorObjetivo)
        {
            return;
        }

        ReproducirFlashPantalla(colorFlashParryExitoso, duracionFlashParryExitoso);
    }

    // Este metodo responde visualmente cuando el jugador falla el timing del parry.
    private void ManejarParryFallido(GameObject jugador)
    {
        if (jugador != jugadorObjetivo)
        {
            return;
        }

        ReproducirFlashPantalla(colorFlashParryFallido, duracionFlashParryFallido);
    }

    // Este metodo centraliza cualquier flash de pantalla del feedback fuerte.
    private void ReproducirFlashPantalla(Color colorFlash, float duracion)
    {
        // Si no existe una imagen de flash, no podemos mostrar nada visual.
        if (imagenFlashPantalla == null)
        {
            return;
        }

        colorFlashPantallaActual = colorFlash;

        // Si ya habia un flash activo, lo detenemos para reiniciarlo limpio.
        if (corrutinaFlashActiva != null)
        {
            StopCoroutine(corrutinaFlashActiva);
        }

        // Lanzamos la corrutina nueva del flash pedido.
        corrutinaFlashActiva = StartCoroutine(CorrutinaFlashPrincipal(Mathf.Max(0.01f, duracion), colorFlash.a));
    }

    // Esta corrutina enciende y apaga rapidamente el flash principal de pantalla.
    private IEnumerator CorrutinaFlashPrincipal(float duracion, float alphaInicial)
    {
        // Arrancamos con el alpha maximo del flash configurado.
        alphaFlashActual = alphaInicial;

        // Guardamos el tiempo transcurrido de la animacion.
        float tiempo = 0f;

        // Mientras dure la animacion, vamos bajando el alpha.
        while (tiempo < duracion)
        {
            // Sumamos tiempo no escalado para que siga funcionando durante freeze frames.
            tiempo += Time.unscaledDeltaTime;

            // Calculamos progreso normalizado entre 0 y 1.
            float progreso = Mathf.Clamp01(tiempo / duracion);

            // Bajamos el alpha desde el valor inicial hasta cero.
            alphaFlashActual = Mathf.Lerp(alphaInicial, 0f, progreso);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Aseguramos que el flash quede completamente apagado.
        alphaFlashActual = 0f;

        // Limpiamos referencia de corrutina activa.
        corrutinaFlashActiva = null;
    }

    // Esta corrutina anima el alpha del flash rojo de dano recibido.
    private IEnumerator CorrutinaFlashDanioNormal()
    {
        // Empezamos con el alpha maximo del color configurado.
        alphaFlashDanioNormal = colorFlashDanioNormal.a;

        // Guardamos tiempo local.
        float tiempo = 0f;

        // Mientras dure, reducimos alpha poco a poco.
        while (tiempo < duracionFlashDanioNormal)
        {
            tiempo += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(tiempo / Mathf.Max(0.01f, duracionFlashDanioNormal));
            alphaFlashDanioNormal = Mathf.Lerp(colorFlashDanioNormal.a, 0f, progreso);
            yield return null;
        }

        // Aseguramos que termine apagado.
        alphaFlashDanioNormal = 0f;
        corrutinaFlashDanioNormalActiva = null;
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
        Color colorObjetivo = colorCrosshairNormal;

        // Si hay objetivo en rango, usamos un color distinto segun donde pegaria mejor.
        if (sistemaEspada != null && sistemaEspada.HayObjetivoEnRangoActual)
        {
            colorObjetivo = ObtenerColorCrosshairPorZona(sistemaEspada.TipoZonaObjetivoActual);
        }

        // Aplicamos ese color a cada grafico del crosshair.
        for (int indiceGrafico = 0; indiceGrafico < graficosCrosshair.Length; indiceGrafico++)
        {
            if (graficosCrosshair[indiceGrafico] != null)
            {
                graficosCrosshair[indiceGrafico].color = colorObjetivo;
            }
        }
    }

    // Este metodo devuelve un color mas informativo segun la zona seleccionada por el auto-target.
    private Color ObtenerColorCrosshairPorZona(TipoZonaDanio tipoZona)
    {
        // Elegimos el color segun la zona del posible impacto.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                return colorCrosshairCabeza;

            case TipoZonaDanio.Espalda:
                return colorCrosshairEspalda;

            default:
                return colorCrosshairObjetivo;
        }
    }

    // Este metodo construye una etiqueta simple para que el jugador entienda el multiplicador actual.
    private string ObtenerTextoZonaActual(TipoZonaDanio tipoZona)
    {
        // Devolvemos una etiqueta corta con el multiplicador.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                return "CABEZA x3";

            case TipoZonaDanio.Espalda:
                return "ESPALDA x2";

            default:
                return "CUERPO x1";
        }
    }

    // Este metodo dibuja una vignette simple por vida baja y un flash rojo cuando recibimos dano.
    private void DibujarOverlayDanioYVidaBaja()
    {
        // Si no hay textura de GUI o no conocemos la vida del jugador, no dibujamos nada.
        if (texturaVignette == null || vidaJugadorObjetivo == null)
        {
            return;
        }

        // Calculamos cuanto deberia verse la vignette por vida baja.
        float alphaVidaBaja = 0f;
        if (vidaJugadorObjetivo.VidaMaxima > 0f)
        {
            float porcentajeVida = vidaJugadorObjetivo.VidaActual / vidaJugadorObjetivo.VidaMaxima;
            if (porcentajeVida <= umbralVidaVignette)
            {
                float faltaVida = 1f - Mathf.Clamp01(porcentajeVida / Mathf.Max(0.01f, umbralVidaVignette));
                float pulso = 0.7f + Mathf.Sin(Time.unscaledTime * velocidadPulsoVignette) * 0.3f;
                alphaVidaBaja = intensidadVignette * faltaVida * pulso;
            }
        }

        // Si no hay ni vignette ni flash de dano, salimos.
        float alphaFinal = Mathf.Max(alphaVidaBaja, alphaFlashDanioNormal);
        if (alphaFinal <= 0.001f)
        {
            return;
        }

        // Elegimos un color rojo base para la advertencia visual.
        Color colorOverlay = new Color(colorFlashDanioNormal.r, colorFlashDanioNormal.g, colorFlashDanioNormal.b, alphaFinal);

        // Guardamos color anterior de GUI para restaurarlo al final.
        Color colorAnterior = GUI.color;
        GUI.color = colorOverlay;

        // Dibujamos cuatro franjas en los bordes para una vignette simple y barata.
        float grosorBorde = Mathf.Lerp(24f, 110f, Mathf.Clamp01(alphaFinal / Mathf.Max(0.01f, intensidadVignette)));
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, grosorBorde), texturaVignette);
        GUI.DrawTexture(new Rect(0f, Screen.height - grosorBorde, Screen.width, grosorBorde), texturaVignette);
        GUI.DrawTexture(new Rect(0f, 0f, grosorBorde, Screen.height), texturaVignette);
        GUI.DrawTexture(new Rect(Screen.width - grosorBorde, 0f, grosorBorde, Screen.height), texturaVignette);

        // Restauramos el color original de GUI.
        GUI.color = colorAnterior;
    }
}
