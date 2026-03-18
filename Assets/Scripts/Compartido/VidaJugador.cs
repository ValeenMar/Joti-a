using System;
using UnityEngine;
using UnityEngine.UI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla la vida del jugador, su barra visual,
// la reaccion a dano y la curacion desde sistemas como pociones.

// Esta clase representa la vida del jugador, actualiza su barra visual y procesa el daño recibido.
public class VidaJugador : MonoBehaviour, IRecibidorDanio
{
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

    // Esta variable guarda hasta cuando el jugador sigue protegido contra dano extra.
    private float tiempoFinInvencibilidadDanio;

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
        // Si no asignaste la imagen de relleno pero sí la barra, intentamos encontrarla automáticamente.
        if (imagenRellenoBarra == null && barraVida != null && barraVida.fillRect != null)
        {
            // Tomamos la imagen del Fill del Slider para poder cambiar su color desde código.
            imagenRellenoBarra = barraVida.fillRect.GetComponent<Image>();
        }

        // Al iniciar, aseguramos que la vida actual arranque llena y dentro de límites válidos.
        vidaActual = Mathf.Clamp(vidaMaxima, 0f, vidaMaxima);

        // Buscamos las cámaras con sacudida para poder llamarlas si existen.
        sacudidasCamaraDisponibles = FindObjectsOfType<SacudidaCamara>(true);

        // Actualizamos la barra una vez al inicio para que arranque correcta.
        ActualizarVisualBarraVida();

        // Avisamos el valor inicial de vida por si otra UI quiere escucharlo.
        NotificarCambioVida();
    }

    // Esta función se ejecuta cada frame para animar el pulso de peligro de la barra.
    private void Update()
    {
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

    // Este método concentra lo que pasa cuando la vida llega a cero.
    private void ProcesarMuerte()
    {
        // Mostramos un mensaje simple en consola para esta primera versión.
        Debug.Log("Jugador muerto");

        // Avisamos al resto del juego que el jugador murió.
        EventosJuego.NotificarJugadorMurio(gameObject);

        // Desactivamos este objeto para que deje de participar en la escena.
        gameObject.SetActive(false);
    }

    // COPILOT-EXPAND: Aqui podes agregar invulnerabilidad temporal, armadura, curacion por tiempo y reanimacion cooperativa.
}
