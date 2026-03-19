using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla la vida del jugador, su barra visual,
// la reaccion a dano y la curacion desde sistemas como pociones.

// Esta clase representa la vida del jugador, actualiza su barra visual y procesa el daño recibido.
public class VidaJugador : MonoBehaviour, IRecibidorDanio
{
    // Este arreglo define nombres comunes para buscar la barra de vida en UI.
    private static readonly string[] nombresBarraVidaCandidata = { "BarraVida", "SliderVida", "VidaBarra", "HealthBar" };

    // Este evento avisa a otros sistemas cuando cambia la vida actual del jugador.
    public event Action<float, float> AlVidaActualizada;

    // Esta es la cantidad máxima de vida que puede tener el jugador.
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;

    // Esta variable guarda la vida actual del jugador en tiempo real y se ve en el Inspector.
    [SerializeField] private float vidaActual = 100f;

    // Esta referencia apunta al Slider de la barra de vida en la UI.
    [Header("UI")]
    [SerializeField] private Slider barraVida;

    // Esta referencia apunta a la imagen de relleno roja de la barra.
    [SerializeField] private Image imagenRellenoBarra;

    // Este porcentaje define desde cuándo la barra empieza a avisar peligro.
    [SerializeField] private float porcentajeVidaPeligro = 0.30f;

    // Este es el color normal de la barra cuando el jugador no está en peligro.
    [SerializeField] private Color colorBarraNormal = new Color(0.82f, 0.16f, 0.16f, 1f);

    // Este es el color base de peligro cuando la vida está baja.
    [SerializeField] private Color colorBarraPeligro = new Color(1f, 0.18f, 0.18f, 1f);

    // Este es el color de pulso que se mezcla para que la barra parpadee.
    [SerializeField] private Color colorPulsoPeligro = new Color(1f, 0.65f, 0.65f, 1f);

    // Este valor controla qué tan rápido palpita la barra cuando queda poca vida.
    [SerializeField] private float velocidadPulsoPeligro = 6f;

    // Este valor define cuantos segundos de invencibilidad basica hay despues de recibir dano.
    [SerializeField] private float duracionInvencibilidadTrasDanio = 0.3f;

    // Esta referencia guarda las cámaras con sacudida disponibles para reaccionar al daño.
    private SacudidaCamara[] sacudidasCamaraDisponibles;

    // Esta referencia guarda el controlador de animacion del jugador para disparar dano y muerte.
    private ControladorAnimacionJugador controladorAnimacionJugador;

    // Esta variable guarda hasta cuando el jugador sigue protegido contra dano extra.
    private float tiempoFinInvencibilidadDanio;

    // Esta variable limita cada cuanto reintentamos vincular UI para no gastar de mas.
    private float tiempoProximoReintentoUI;

    // Esta variable evita lanzar varias corutinas de reintento al mismo tiempo.
    private bool reintentoUIEnCurso;

    // Esta referencia guarda la corrutina de muerte para no repetir el apagado.
    private Coroutine rutinaMuerteJugador;

    // Esta variable define cuanto esperamos antes de desactivar el jugador al morir.
    [SerializeField] private float tiempoEsperaAntesDeDesactivarMuerte = 1f;

    // Esta propiedad permite leer la vida actual desde otros scripts.
    public float VidaActual => vidaActual;

    // Esta propiedad permite leer la vida máxima desde otros scripts.
    public float VidaMaxima => vidaMaxima;

    // Esta propiedad permite consultar el porcentaje de vida actual.
    public float PorcentajeVidaActual => vidaMaxima > 0f ? vidaActual / vidaMaxima : 0f;

    // Esta propiedad indica si el jugador sigue con vida.
    public bool EstaVivo => vidaActual > 0f;

    // Esta función se ejecuta al activar el objeto.
    private void Awake()
    {
        // Al iniciar, aseguramos que la vida actual arranque llena y dentro de límites válidos.
        vidaActual = Mathf.Clamp(vidaMaxima, 0f, vidaMaxima);

        // Buscamos las cámaras con sacudida para poder llamarlas si existen.
        sacudidasCamaraDisponibles = FindObjectsOfType<SacudidaCamara>(true);

        // Buscamos el controlador de animacion del jugador para enlazar dano y muerte.
        controladorAnimacionJugador = GetComponent<ControladorAnimacionJugador>();

        // Intentamos vincular referencias de UI por si ya existe el HUD en escena.
        IntentarVincularUI(true);

        // Actualizamos la barra una vez al inicio para que arranque correcta.
        ActualizarVisualBarraVida();

        // Avisamos el valor inicial de vida por si otra UI quiere escucharlo.
        NotificarCambioVida();
    }

    // Esta funcion se ejecuta cuando el componente queda activo en escena.
    private void OnEnable()
    {
        // Nos suscribimos al cambio de escena para re-vincular UI tras LoadScene.
        SceneManager.sceneLoaded += ManejarEscenaCargada;

        // Reintentamos vincular el controlador de animacion por si hubo reload de escena.
        IntentarVincularControladorAnimacion();
    }

    // Esta funcion se ejecuta cuando el componente se desactiva.
    private void OnDisable()
    {
        // Quitamos suscripcion para evitar callbacks sobre objetos desactivados.
        SceneManager.sceneLoaded -= ManejarEscenaCargada;
    }

    // Esta funcion corre despues de Awake y ayuda a re-vincular cuando el HUD aparece tarde.
    private void Start()
    {
        // Intentamos una segunda vinculacion por si la UI se creo entre Awake y Start.
        IntentarVincularUI(true);

        // Reintentamos vincular la animacion por si aun no estaba lista en Awake.
        IntentarVincularControladorAnimacion();

        // Si faltan referencias, empezamos un ciclo corto de reintentos.
        if (!TieneReferenciasUIValidas())
        {
            StartCoroutine(RutinaReintentarVinculacionUI(12, 0.2f));
        }
    }

    // Esta función se ejecuta cada frame para animar el pulso de peligro de la barra.
    private void Update()
    {
        // Si la UI se perdio por cambio de escena o destruccion, reintentamos vincularla.
        ReintentarVinculacionUIEnUpdate();

        // Refrescamos la parte visual de la vida para que el pulso se vea en tiempo real.
        ActualizarVisualBarraVida();
    }

    // Este método público simple permite que otro script dañe al jugador pasando solo un número.
    public void RecibirDanio(float cantidad)
    {
        // Si la cantidad recibida no tiene sentido, no hacemos nada.
        if (cantidad <= 0f)
        {
            return;
        }

        // Creamos un contenedor de daño básico para reutilizar la lógica principal del sistema.
        DatosDanio datosDanio = new DatosDanio
        {
            // Como este método recibe solo un número, no conocemos un atacante concreto aquí.
            atacante = null,

            // El objetivo real de este daño es este propio jugador.
            objetivo = gameObject,

            // Guardamos la cantidad como daño base.
            danioBase = cantidad,

            // Usamos multiplicador normal porque no hay zona débil en este camino simple.
            multiplicadorZona = 1f,

            // Marcamos el tipo como cuerpo para feedback normal.
            tipoZona = TipoZonaDanio.Cuerpo,

            // Guardamos un punto aproximado de impacto a la altura del torso.
            puntoImpacto = transform.position + Vector3.up,

            // Guardamos una dirección simple hacia atrás del jugador como referencia visual.
            direccionImpacto = -transform.forward,

            // Este daño no viene de un golpe fuerte del jugador.
            esGolpeFuerte = false
        };

        // Reutilizamos el método principal para aplicar el daño de verdad.
        RecibirDanio(datosDanio);
    }

    // Este método permite que cualquier sistema compatible le aplique daño al jugador.
    public void RecibirDanio(DatosDanio datosDanio)
    {
        // Si por algún motivo llega un dato nulo, creamos uno básico para no romper el flujo.
        if (datosDanio == null)
        {
            // Creamos un contenedor base para que el sistema siga funcionando.
            datosDanio = new DatosDanio();
        }

        // Si el jugador ya murió, no seguimos procesando daño.
        if (!EstaVivo)
        {
            // Salimos del método para evitar valores negativos o eventos repetidos.
            return;
        }

        // Si todavia estamos dentro de la ventana de invencibilidad, ignoramos este golpe.
        if (Time.time < tiempoFinInvencibilidadDanio)
        {
            // Salimos para evitar multiples impactos apilados en un instante muy corto.
            return;
        }

        // Si el golpe no trajo objetivo, usamos este propio jugador como receptor real.
        if (datosDanio.objetivo == null)
        {
            // Asignamos este gameObject para que otros sistemas sepan quién recibió el golpe.
            datosDanio.objetivo = gameObject;
        }

        // Calculamos el daño final real y evitamos valores negativos o raros.
        float danioFinal = Mathf.Max(0f, datosDanio.DanioFinalCalculado);

        // Si el daño final quedó en cero, no hace falta seguir.
        if (danioFinal <= 0f)
        {
            return;
        }

        // Restamos el daño final calculado a la vida actual.
        vidaActual -= danioFinal;

        // Evitamos que la vida baje de cero.
        vidaActual = Mathf.Max(0f, vidaActual);

        // Activamos una pequeña ventana de invencibilidad para bloquear daño encadenado inmediato.
        tiempoFinInvencibilidadDanio = Time.time + duracionInvencibilidadTrasDanio;

        // Mostramos en consola el daño recibido y la vida restante para poder depurar bien.
        Debug.Log("VidaJugador -> dano recibido: " + danioFinal + " | vida actual: " + vidaActual + " / " + vidaMaxima);

        // Actualizamos la parte visual enseguida para que la UI responda al instante.
        ActualizarVisualBarraVida();

        // Avisamos a cualquier UI que la vida cambió.
        NotificarCambioVida();

        // Si existe una cámara con sacudida asociada a este jugador, le pedimos feedback de golpe.
        AplicarSacudidaPorDanio(danioFinal);

        // Si existe un controlador de animacion, reproducimos el golpe recibido ahora mismo.
        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.ReproducirRecibirDanio();
        }

        // Avisamos al resto del juego que el jugador recibió daño.
        EventosJuego.NotificarJugadorRecibioDanio(gameObject, danioFinal);

        // Avisamos también que hubo un golpe aplicado para daño flotante y otros sistemas.
        EventosJuego.NotificarDanioAplicado(datosDanio);

        // Si la vida llegó a cero, procesamos la muerte del jugador.
        if (vidaActual <= 0f)
        {
            // Ejecutamos la lógica de muerte.
            ProcesarMuerte();
        }

        // NOTA MIRROR:
        // En multijugador final, el cliente no debería descontarse vida solo.
        // [Command futuro] El cliente pediría al servidor aplicar el daño.
        // [ClientRpc o TargetRpc futuro] El servidor devolvería el resultado visual a los clientes.
    }

    // Este metodo cura al jugador sin superar su vida maxima.
    public void Curar(float cantidadCuracion)
    {
        // Si la curacion es invalida, no seguimos.
        if (cantidadCuracion <= 0f)
        {
            return;
        }

        // Si el jugador ya esta muerto, no curamos en esta version.
        if (!EstaVivo)
        {
            return;
        }

        // Sumamos la curacion a la vida actual.
        vidaActual += cantidadCuracion;

        // Limitamos la vida al maximo configurado.
        vidaActual = Mathf.Min(vidaActual, vidaMaxima);

        // Actualizamos la barra enseguida para reflejar la curacion.
        ActualizarVisualBarraVida();

        // Avisamos el nuevo valor a cualquier sistema que escuche cambios de vida.
        NotificarCambioVida();
    }

    // Este metodo rellena la vida al maximo y actualiza UI.
    public void RestaurarVidaCompleta()
    {
        // Llevamos la vida actual directamente al maximo.
        vidaActual = vidaMaxima;

        // Refrescamos el valor visual de la barra.
        ActualizarVisualBarraVida();

        // Avisamos el cambio de valor al resto de sistemas.
        NotificarCambioVida();
    }

    // Este método intenta disparar la sacudida de cámara local sin duplicarla.
    private void AplicarSacudidaPorDanio(float cantidadDanio)
    {
        // Si no tenemos una lista de sacudidas todavía, intentamos buscarla ahora.
        if (sacudidasCamaraDisponibles == null || sacudidasCamaraDisponibles.Length == 0)
        {
            // Buscamos todas las cámaras con sacudida activas o desactivadas.
            sacudidasCamaraDisponibles = FindObjectsOfType<SacudidaCamara>(true);
        }

        // Si sigue sin haber ninguna, simplemente no hacemos nada.
        if (sacudidasCamaraDisponibles == null || sacudidasCamaraDisponibles.Length == 0)
        {
            return;
        }

        // Recorremos cada cámara con sacudida encontrada.
        for (int indiceSacudida = 0; indiceSacudida < sacudidasCamaraDisponibles.Length; indiceSacudida++)
        {
            // Guardamos una referencia cómoda a la cámara actual.
            SacudidaCamara sacudidaActual = sacudidasCamaraDisponibles[indiceSacudida];

            // Si esta referencia quedó nula por algún motivo, la ignoramos.
            if (sacudidaActual == null)
            {
                continue;
            }

            // Si la cámara no está asociada a este jugador, no la usamos.
            if (!sacudidaActual.EstaAsociadaA(gameObject))
            {
                continue;
            }

            // Le pedimos a esa cámara que aplique la sacudida según el daño recibido.
            sacudidaActual.AplicarSacudidaDesdeDanio(cantidadDanio);
        }
    }

    // Este método actualiza el Slider y el color visual de la barra de vida.
    private void ActualizarVisualBarraVida()
    {
        // Si faltan referencias, intentamos recuperarlas antes de tocar UI.
        if (!TieneReferenciasUIValidas())
        {
            IntentarVincularUI(false);
        }

        // Si existe una barra asignada, mantenemos sus límites y su valor actualizados.
        if (barraVida != null)
        {
            // El mínimo de vida de la barra es cero.
            barraVida.minValue = 0f;

            // El máximo de vida de la barra es la vida máxima configurada.
            barraVida.maxValue = vidaMaxima;

            // El valor visible actual es la vida que le queda al jugador.
            barraVida.value = vidaActual;
        }

        // Forzamos la actualización inmediata del Canvas para que la barra cambie en este mismo frame.
        Canvas.ForceUpdateCanvases();

        // Si no existe imagen de relleno, no podemos cambiar colores y salimos.
        if (imagenRellenoBarra == null)
        {
            return;
        }

        // Calculamos el porcentaje de vida actual para decidir si está en peligro.
        float porcentajeVidaActual = vidaMaxima > 0f ? vidaActual / vidaMaxima : 0f;

        // Si la vida está en el rango de peligro, hacemos parpadear la barra.
        if (porcentajeVidaActual <= porcentajeVidaPeligro)
        {
            // Generamos una intensidad de pulso que sube y baja con el tiempo.
            float intensidadPulso = Mathf.PingPong(Time.unscaledTime * velocidadPulsoPeligro, 1f);

            // Mezclamos entre el rojo de peligro y un rojo más claro para llamar la atención.
            imagenRellenoBarra.color = Color.Lerp(colorBarraPeligro, colorPulsoPeligro, intensidadPulso);

            // Terminamos aquí porque ya dejamos el color correcto de peligro.
            return;
        }

        // Si no está en peligro, dejamos el color rojo normal de la barra.
        imagenRellenoBarra.color = colorBarraNormal;
    }

    // Este metodo avisa a la UI y a otros sistemas que la vida cambio.
    private void NotificarCambioVida()
    {
        // Lanzamos el evento con vida actual y maxima.
        AlVidaActualizada?.Invoke(vidaActual, vidaMaxima);
    }

    // Este metodo decide si ya tenemos la UI minima para actualizar barra y color.
    private bool TieneReferenciasUIValidas()
    {
        // Si no hay slider, no hay UI de vida funcional.
        if (barraVida == null)
        {
            return false;
        }

        // Si hay slider, intentamos obtener fill cuando falte para color dinamico.
        if (imagenRellenoBarra == null && barraVida.fillRect != null)
        {
            imagenRellenoBarra = barraVida.fillRect.GetComponent<Image>();
        }

        // Pedimos slider y fill para considerar la UI completamente vinculada.
        return barraVida != null && imagenRellenoBarra != null;
    }

    // Este metodo intenta vincular slider e imagen con varias estrategias de fallback.
    private void IntentarVincularUI(bool permitirBusquedaAmplia)
    {
        // Si falta slider, intentamos por nombres conocidos con GameObject.Find.
        if (barraVida == null)
        {
            for (int indiceNombre = 0; indiceNombre < nombresBarraVidaCandidata.Length; indiceNombre++)
            {
                GameObject objetoCandidato = GameObject.Find(nombresBarraVidaCandidata[indiceNombre]);
                if (objetoCandidato == null)
                {
                    continue;
                }

                Slider sliderCandidato = objetoCandidato.GetComponent<Slider>();
                if (sliderCandidato == null)
                {
                    sliderCandidato = objetoCandidato.GetComponentInChildren<Slider>(true);
                }

                if (sliderCandidato != null)
                {
                    barraVida = sliderCandidato;
                    break;
                }
            }
        }

        // Si todavia falta slider, intentamos encontrarlo por tipo y nombre.
        if (barraVida == null && permitirBusquedaAmplia)
        {
            Slider[] slidersEscena = FindObjectsOfType<Slider>(true);
            for (int indiceSlider = 0; indiceSlider < slidersEscena.Length; indiceSlider++)
            {
                if (slidersEscena[indiceSlider] == null)
                {
                    continue;
                }

                string nombre = slidersEscena[indiceSlider].name.ToLowerInvariant();
                if (nombre.Contains("vida") || nombre.Contains("health"))
                {
                    barraVida = slidersEscena[indiceSlider];
                    break;
                }
            }
        }

        // Si no existe una barra de vida real en escena, intentamos crear una a partir de la de estamina.
        if (barraVida == null && permitirBusquedaAmplia)
        {
            CrearBarraVidaRuntimeSiHaceFalta();
        }

        // Si falta imagen de relleno, intentamos tomarla del fillRect del slider.
        if (imagenRellenoBarra == null && barraVida != null && barraVida.fillRect != null)
        {
            imagenRellenoBarra = barraVida.fillRect.GetComponent<Image>();
        }

        // Si sigue faltando fill, intentamos ubicar un hijo llamado Fill.
        if (imagenRellenoBarra == null && barraVida != null)
        {
            Transform fillDirecto = barraVida.transform.Find("Fill Area/Fill");
            if (fillDirecto == null)
            {
                fillDirecto = barraVida.transform.Find("Fill");
            }

            if (fillDirecto != null)
            {
                imagenRellenoBarra = fillDirecto.GetComponent<Image>();
            }
        }

        // Si aun falta fill y se permite busqueda amplia, probamos por nombre en toda la escena.
        if (imagenRellenoBarra == null && permitirBusquedaAmplia)
        {
            Image[] imagenesEscena = FindObjectsOfType<Image>(true);
            for (int indiceImagen = 0; indiceImagen < imagenesEscena.Length; indiceImagen++)
            {
                if (imagenesEscena[indiceImagen] == null)
                {
                    continue;
                }

                string nombre = imagenesEscena[indiceImagen].name.ToLowerInvariant();
                if (nombre.Contains("fill") && (nombre.Contains("vida") || nombre.Contains("health")))
                {
                    imagenRellenoBarra = imagenesEscena[indiceImagen];
                    break;
                }
            }
        }
    }

    // Este metodo crea una barra de vida runtime clonando la barra de estamina si la escena no trae una.
    private void CrearBarraVidaRuntimeSiHaceFalta()
    {
        // Si ya existe una barra vinculada, no hace falta crear otra.
        if (barraVida != null)
        {
            return;
        }

        // Buscamos la barra de estamina como plantilla visual.
        GameObject objetoBarraStamina = GameObject.Find("BarraStamina");

        // Si no existe con ese nombre, probamos la variante con E.
        if (objetoBarraStamina == null)
        {
            objetoBarraStamina = GameObject.Find("BarraEstamina");
        }

        // Si no tenemos plantilla, no podemos clonar nada.
        if (objetoBarraStamina == null)
        {
            return;
        }

        // Clonamos la barra de estamina bajo el mismo padre para reutilizar su look.
        GameObject objetoBarraVida = Instantiate(objetoBarraStamina, objetoBarraStamina.transform.parent);

        // Renombramos el clon para que futuros rebindeos lo encuentren de forma directa.
        objetoBarraVida.name = "BarraVida";

        // Tomamos su RectTransform para recolocarlo por encima de la estamina.
        RectTransform rectTransformBarraVida = objetoBarraVida.GetComponent<RectTransform>();

        // Si existe el RectTransform, lo ubicamos en la posicion de vida pedida para el HUD.
        if (rectTransformBarraVida != null)
        {
            rectTransformBarraVida.anchoredPosition = new Vector2(40f, -45f);
            rectTransformBarraVida.sizeDelta = new Vector2(260f, 22f);
        }

        // Guardamos el slider del clon como nuestra barra de vida.
        barraVida = objetoBarraVida.GetComponent<Slider>();

        // Si el slider existe, ajustamos su rango a la vida actual.
        if (barraVida != null)
        {
            barraVida.minValue = 0f;
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaActual;
        }

        // Si el clon tiene fillRect, recuperamos la imagen de relleno para pintarla de rojo.
        if (barraVida != null && barraVida.fillRect != null)
        {
            imagenRellenoBarra = barraVida.fillRect.GetComponent<Image>();
        }

        // Si encontramos el relleno, lo dejamos con el color rojo de vida.
        if (imagenRellenoBarra != null)
        {
            imagenRellenoBarra.color = colorBarraNormal;
        }
    }

    // Este metodo reintenta vinculacion desde Update con un intervalo razonable.
    private void ReintentarVinculacionUIEnUpdate()
    {
        // Si ya estan slider y fill validos, no hace falta reintentar.
        if (TieneReferenciasUIValidas())
        {
            return;
        }

        // Si aun no toca reintentar, salimos para evitar busquedas cada frame.
        if (Time.unscaledTime < tiempoProximoReintentoUI)
        {
            return;
        }

        // Definimos proximo intento.
        tiempoProximoReintentoUI = Time.unscaledTime + 0.5f;

        // Reintentamos con busqueda amplia.
        IntentarVincularUI(true);
    }

    // Esta corutina reintenta varias veces para cubrir UI que aparece con retraso.
    private IEnumerator RutinaReintentarVinculacionUI(int cantidadIntentos, float esperaEntreIntentos)
    {
        // Evitamos duplicar corutinas si ya hay una corriendo.
        if (reintentoUIEnCurso)
        {
            yield break;
        }

        // Marcamos que hay reintento activo.
        reintentoUIEnCurso = true;

        // Reintentamos una cantidad limitada de veces.
        for (int intento = 0; intento < cantidadIntentos; intento++)
        {
            // Si ya tenemos slider, cortamos.
            if (barraVida != null)
            {
                break;
            }

            // Probamos vincular con busqueda amplia.
            IntentarVincularUI(true);

            // Esperamos antes del siguiente intento.
            yield return new WaitForSecondsRealtime(esperaEntreIntentos);
        }

        // Marcamos fin del reintento.
        reintentoUIEnCurso = false;
    }

    // Este callback se ejecuta al terminar de cargar una escena nueva.
    private void ManejarEscenaCargada(Scene escenaCargada, LoadSceneMode modoCarga)
    {
        // Limpiamos referencias para forzar re-vinculacion en la escena nueva.
        barraVida = null;
        imagenRellenoBarra = null;
        tiempoProximoReintentoUI = 0f;
        controladorAnimacionJugador = null;

        // Intentamos vincular de inmediato.
        IntentarVincularUI(true);

        // Reintentamos vincular la animacion en la escena nueva.
        IntentarVincularControladorAnimacion();

        // Si no alcanza, dejamos una rutina de reintento.
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RutinaReintentarVinculacionUI(15, 0.2f));
        }
    }

    // Este método concentra lo que pasa cuando la vida llega a cero.
    private void ProcesarMuerte()
    {
        // Si ya habia una rutina de muerte corriendo, no iniciamos otra.
        if (rutinaMuerteJugador != null)
        {
            return;
        }

        // Iniciamos la secuencia de muerte para permitir que se vea la animacion.
        rutinaMuerteJugador = StartCoroutine(RutinaMuerteJugador());
    }

    // Esta corrutina reproduce la animacion de muerte, avisa al juego y luego desactiva el jugador.
    private IEnumerator RutinaMuerteJugador()
    {
        // Mostramos un mensaje simple en consola para esta primera version.
        Debug.Log("Jugador muerto");

        // Disparamos la animacion de muerte si existe un controlador.
        IntentarVincularControladorAnimacion();

        if (controladorAnimacionJugador != null)
        {
            controladorAnimacionJugador.ReproducirMorir();
        }

        // Avisamos al resto del juego que el jugador murió.
        EventosJuego.NotificarJugadorMurio(gameObject);

        // Esperamos en tiempo real para que la animacion se vea aunque el juego entre en GameOver.
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, tiempoEsperaAntesDeDesactivarMuerte));

        // Liberamos la referencia de la corrutina.
        rutinaMuerteJugador = null;

        // Si seguimos existiendo, apagamos el objeto para cerrar la escena de juego.
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }

    // Este metodo intenta vincular el controlador de animacion si aun no lo tenemos.
    private void IntentarVincularControladorAnimacion()
    {
        // Si ya hay una referencia valida, no hacemos nada.
        if (controladorAnimacionJugador != null)
        {
            return;
        }

        // Buscamos en el mismo objeto primero.
        controladorAnimacionJugador = GetComponent<ControladorAnimacionJugador>();

        // Si no esta en la raiz, buscamos en hijos como respaldo.
        if (controladorAnimacionJugador == null)
        {
            controladorAnimacionJugador = GetComponentInChildren<ControladorAnimacionJugador>(true);
        }
    }

    // COPILOT-EXPAND: Aqui podes agregar invulnerabilidad temporal, armadura, curacion por tiempo y reanimacion cooperativa.
}
