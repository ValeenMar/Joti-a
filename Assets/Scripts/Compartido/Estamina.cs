using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Esta clase administra toda la estamina del jugador: gasto, regeneracion, agotamiento y barra visual.
public class Estamina : MonoBehaviour
{
    // Este arreglo define nombres comunes para buscar barra de estamina.
    private static readonly string[] nombresBarraEstaminaCandidata = { "BarraEstamina", "SliderEstamina", "EstaminaBarra", "StaminaBar" };

    // Este evento avisa cuando cambia la estamina para que otros sistemas puedan reaccionar.
    public event Action<float, float> AlEstaminaActualizada;

    // Este valor define cuanta estamina maxima tiene el jugador.
    [SerializeField] private float estaminaMaxima = 100f;

    // Este valor guarda cuanta estamina tiene ahora mismo el jugador.
    [SerializeField] private float estaminaActual = 100f;

    // Este valor define cuanta estamina se recupera por segundo cuando empieza la recarga.
    [SerializeField] private float regeneracionPorSegundo = 30f;

    // Este valor define cuanto tiempo hay que esperar despues de gastar antes de regenerar.
    [SerializeField] private float delayAntesDeRegenerar = 1.1f;

    // Este valor define desde cuanta estamina se desbloquea otra vez sprint y golpe fuerte tras agotarse.
    [SerializeField] private float umbralRecuperacionAgotado = 35f;

    // Este valor define cuanto gasta por segundo el sprint.
    [SerializeField] private float costoSprintPorSegundo = 22f;

    // Este valor define cuanto cuesta hacer un golpe fuerte.
    [SerializeField] private float costoGolpeFuerte = 35f;

    // Este valor define desde cuanta estamina restante la barra pasa a color rojo.
    [SerializeField] private float umbralBarraBaja = 25f;

    // Esta referencia apunta a la barra de estamina en la UI si decidis usar una.
    [SerializeField] private Slider barraEstamina;

    // Esta referencia apunta a la imagen de relleno de la barra para poder cambiarle el color.
    [SerializeField] private Image imagenRellenoBarra;

    // Este es el color normal de la barra cuando la estamina esta sana.
    [SerializeField] private Color colorBarraNormal = new Color(1f, 0.82f, 0.18f, 1f);

    // Este es el color de la barra cuando queda poca estamina.
    [SerializeField] private Color colorBarraBaja = new Color(0.9f, 0.2f, 0.2f, 1f);

    // Este es el color que se mezcla en el flash cuando la estamina se agota por completo.
    [SerializeField] private Color colorFlashAgotado = new Color(1f, 0.05f, 0.05f, 1f);

    // Este valor define cuanto dura el flash de agotamiento.
    [SerializeField] private float duracionFlashAgotado = 0.2f;

    // Este valor define cuan rapido palpita el flash mientras esta activo.
    [SerializeField] private float velocidadFlashAgotado = 14f;

    // Esta variable guarda hasta que momento debemos seguir bloqueados por agotamiento.
    private bool estaAgotado;

    // Esta variable guarda el ultimo instante en el que se gasto estamina.
    private float tiempoUltimoGasto = -999f;

    // Esta variable guarda cuanto tiempo de flash queda activo.
    private float tiempoFlashRestante;

    // Esta variable limita cada cuanto reintentamos vincular UI.
    private float tiempoProximoReintentoUI;

    // Esta variable evita duplicar corutinas de reintento.
    private bool reintentoUIEnCurso;

    // Esta propiedad permite leer cuanta estamina actual tiene el jugador.
    public float EstaminaActual => estaminaActual;

    // Esta propiedad permite leer la estamina maxima del jugador.
    public float EstaminaMaxima => estaminaMaxima;

    // Esta propiedad dice si el jugador esta agotado y todavia no recupero lo minimo para volver a acciones costosas.
    public bool EstaAgotado => estaAgotado;

    // Esta propiedad expone el costo del golpe fuerte para otros sistemas.
    public float CostoGolpeFuerte => costoGolpeFuerte;

    // Esta propiedad dice si ya puede volver a usar sprint.
    public bool PuedeCorrer => !estaAgotado && estaminaActual > 0.01f;

    // Esta propiedad dice si ya puede volver a usar golpe fuerte.
    public bool PuedeUsarGolpeFuerte => !estaAgotado && estaminaActual >= costoGolpeFuerte;

    // Esta propiedad dice si el jugador puede atacar de cualquier forma.
    public bool PuedeAtacar => !estaAgotado;

    // Esta funcion se ejecuta al iniciar el componente.
    private void Awake()
    {
        // Nos aseguramos de que la estamina arranque dentro de valores validos.
        estaminaActual = Mathf.Clamp(estaminaActual, 0f, estaminaMaxima);

        // Intentamos vincular la UI por si ya existe en escena.
        IntentarVincularUI(true);

        // Actualizamos la barra una vez al inicio para que arranque sincronizada.
        ActualizarVisualBarra();

        // Avisamos el valor inicial de estamina al resto del juego.
        NotificarCambioEstamina();
    }

    // Esta funcion se ejecuta cuando el componente se habilita.
    private void OnEnable()
    {
        // Nos suscribimos al evento de escena para re-vincular tras LoadScene.
        SceneManager.sceneLoaded += ManejarEscenaCargada;
    }

    // Esta funcion se ejecuta cuando el componente se deshabilita.
    private void OnDisable()
    {
        // Quitamos suscripcion para evitar callbacks invalidos.
        SceneManager.sceneLoaded -= ManejarEscenaCargada;
    }

    // Esta funcion corre al iniciar y refuerza la vinculacion si el HUD llega tarde.
    private void Start()
    {
        // Intentamos una segunda vinculacion por seguridad.
        IntentarVincularUI(true);

        // Si faltan referencias, dejamos reintentos cortos.
        if (!TieneReferenciasUIValidas())
        {
            StartCoroutine(RutinaReintentarVinculacionUI(12, 0.2f));
        }
    }

    // Esta funcion corre cada frame para regenerar y animar el flash de la barra.
    private void Update()
    {
        // Si la UI se perdio o aun no existe, reintentamos por intervalos.
        ReintentarVinculacionUIEnUpdate();

        // Intentamos regenerar estamina si ya paso el delay desde el ultimo gasto.
        IntentarRegenerarEstamina();

        // Si hay flash activo, descontamos tiempo usando tiempo real del frame.
        if (tiempoFlashRestante > 0f)
        {
            tiempoFlashRestante -= Time.unscaledDeltaTime;
        }

        // Refrescamos la visual de la barra para color y flash.
        ActualizarVisualBarra();
    }

    // Este metodo gasta estamina continua para el sprint de un frame.
    public bool ConsumirEstaminaSprint(float deltaTiempo)
    {
        // Si ya esta agotado o no tiene nada para gastar, no permitimos correr.
        if (!PuedeCorrer)
        {
            return false;
        }

        // Calculamos cuanto cuesta este frame de sprint.
        float costoFrame = costoSprintPorSegundo * Mathf.Max(0f, deltaTiempo);

        // Si el costo resulto cero o negativo, consideramos que si se puede correr.
        if (costoFrame <= 0f)
        {
            return true;
        }

        // Guardamos cuanta estamina habia antes de gastar para saber si este frame podia correr.
        float estaminaAntesDeGastar = estaminaActual;

        // Restamos la estamina de este frame y evitamos valores negativos.
        estaminaActual = Mathf.Max(0f, estaminaActual - costoFrame);

        // Guardamos el instante de gasto para reiniciar el delay de regeneracion.
        tiempoUltimoGasto = Time.time;

        // Si tocamos cero, entramos en estado agotado.
        if (estaminaActual <= 0.001f)
        {
            ActivarAgotamiento();
        }

        // Avisamos al resto del juego que la estamina cambio.
        NotificarCambioEstamina();

        // Permitimos correr este frame si antes del gasto habia estamina disponible.
        return estaminaAntesDeGastar > 0.001f;
    }

    // Este metodo intenta gastar una cantidad exacta de estamina para una accion puntual.
    public bool ConsumirEstamina(float cantidad)
    {
        // Si la cantidad es cero o negativa, no hace falta gastar nada y devolvemos exito.
        if (cantidad <= 0f)
        {
            return true;
        }

        // Si la estamina actual no alcanza, no consumimos nada y devolvemos falso.
        if (estaminaActual < cantidad)
        {
            return false;
        }

        // Si estamos agotados por haber tocado cero antes, tampoco permitimos acciones costosas todavia.
        if (estaAgotado)
        {
            return false;
        }

        // Restamos la cantidad exacta pedida y evitamos negativos por seguridad.
        estaminaActual = Mathf.Max(0f, estaminaActual - cantidad);

        // Guardamos el instante de gasto para pausar la regeneracion.
        tiempoUltimoGasto = Time.time;

        // Si tocamos cero, activamos agotamiento y el flash visual.
        if (estaminaActual <= 0.001f)
        {
            ActivarAgotamiento();
        }

        // Avisamos al resto del juego que la estamina cambio.
        NotificarCambioEstamina();

        // Devolvemos exito porque si se pudo pagar la accion.
        return true;
    }

    // Este metodo intenta consumir especificamente el costo del golpe fuerte.
    public bool IntentarConsumirGolpeFuerte()
    {
        // Si no podemos usar golpe fuerte todavia, devolvemos falso para que el ataque vuelva al golpe normal.
        if (!PuedeUsarGolpeFuerte)
        {
            return false;
        }

        // Reutilizamos el metodo general de gasto con el costo configurado para golpe fuerte.
        return ConsumirEstamina(costoGolpeFuerte);
    }

    // Este metodo intenta regenerar estamina automaticamente si ya paso el delay.
    private void IntentarRegenerarEstamina()
    {
        // Si ya estamos llenos, no hace falta regenerar nada.
        if (estaminaActual >= estaminaMaxima)
        {
            return;
        }

        // Si todavia no paso el tiempo de espera tras el ultimo gasto, no regeneramos aun.
        if (Time.time < tiempoUltimoGasto + delayAntesDeRegenerar)
        {
            return;
        }

        // Sumamos estamina segun la velocidad de regeneracion por segundo.
        estaminaActual += regeneracionPorSegundo * Time.deltaTime;

        // Limitamos la estamina al maximo configurado.
        estaminaActual = Mathf.Min(estaminaActual, estaminaMaxima);

        // Si estabamos agotados y ya recuperamos el minimo requerido, levantamos el bloqueo.
        if (estaAgotado && estaminaActual >= umbralRecuperacionAgotado)
        {
            estaAgotado = false;
        }

        // Avisamos al resto del juego que la estamina cambio.
        NotificarCambioEstamina();
    }

    // Este metodo centraliza lo que pasa cuando la estamina toca cero.
    private void ActivarAgotamiento()
    {
        // Marcamos al jugador como agotado para bloquear sprint y golpe fuerte.
        estaAgotado = true;

        // Encendemos el temporizador del flash de barra.
        tiempoFlashRestante = duracionFlashAgotado;

        // En Mirror, este agotamiento real deberia validarlo el servidor.
        // [Mirror futuro] El cliente pediria la accion con [Command].
        // [Mirror futuro] El servidor gastaria estamina y luego avisaria el resultado visual con [ClientRpc] o [TargetRpc].
    }

    // Este metodo actualiza el Slider y el color de la barra si las referencias estan configuradas.
    private void ActualizarVisualBarra()
    {
        // Si faltan referencias, intentamos recuperarlas antes de usar UI.
        if (!TieneReferenciasUIValidas())
        {
            IntentarVincularUI(false);
        }

        // Si existe una barra asignada, sincronizamos sus limites y valor.
        if (barraEstamina != null)
        {
            barraEstamina.minValue = 0f;
            barraEstamina.maxValue = estaminaMaxima;
            barraEstamina.value = estaminaActual;
        }

        // Si no existe imagen de relleno, no podemos cambiar colores y salimos.
        if (imagenRellenoBarra == null)
        {
            return;
        }

        // Elegimos el color base segun si queda poca o suficiente estamina.
        Color colorBase = estaminaActual <= umbralBarraBaja ? colorBarraBaja : colorBarraNormal;

        // Si estamos agotados, hacemos parpadear la barra en rojo mientras dure el agotamiento.
        if (estaAgotado)
        {
            float intensidadPulsoAgotado = Mathf.PingPong(Time.unscaledTime * velocidadFlashAgotado, 1f);
            imagenRellenoBarra.color = Color.Lerp(colorBarraBaja, colorFlashAgotado, intensidadPulsoAgotado);
            return;
        }

        // Si el flash esta activo, mezclamos el color base con el color de flash.
        if (tiempoFlashRestante > 0f)
        {
            float intensidadFlash = Mathf.PingPong(Time.unscaledTime * velocidadFlashAgotado, 1f);
            imagenRellenoBarra.color = Color.Lerp(colorBase, colorFlashAgotado, intensidadFlash);
            return;
        }

        // Si no hay flash, dejamos el color base normal de la barra.
        imagenRellenoBarra.color = colorBase;
    }

    // Este metodo avisa al resto del juego que el valor de estamina se actualizo.
    private void NotificarCambioEstamina()
    {
        // Lanzamos el evento con estamina actual y maxima.
        AlEstaminaActualizada?.Invoke(estaminaActual, estaminaMaxima);
    }

    // Este metodo valida si la UI minima esta disponible.
    private bool TieneReferenciasUIValidas()
    {
        // Si no hay slider no podemos dibujar barra.
        if (barraEstamina == null)
        {
            return false;
        }

        // Si falta fill, intentamos recuperarlo desde fillRect.
        if (imagenRellenoBarra == null && barraEstamina.fillRect != null)
        {
            imagenRellenoBarra = barraEstamina.fillRect.GetComponent<Image>();
        }

        // Pedimos slider y fill para considerar UI completamente vinculada.
        return barraEstamina != null && imagenRellenoBarra != null;
    }

    // Este metodo intenta vincular barra e imagen con varios fallbacks.
    private void IntentarVincularUI(bool permitirBusquedaAmplia)
    {
        // Si falta slider, buscamos por nombres comunes.
        if (barraEstamina == null)
        {
            for (int indiceNombre = 0; indiceNombre < nombresBarraEstaminaCandidata.Length; indiceNombre++)
            {
                GameObject objetoCandidato = GameObject.Find(nombresBarraEstaminaCandidata[indiceNombre]);
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
                    barraEstamina = sliderCandidato;
                    break;
                }
            }
        }

        // Si sigue faltando, buscamos sliders por tipo y nombre.
        if (barraEstamina == null && permitirBusquedaAmplia)
        {
            Slider[] slidersEscena = FindObjectsOfType<Slider>(true);
            for (int indiceSlider = 0; indiceSlider < slidersEscena.Length; indiceSlider++)
            {
                if (slidersEscena[indiceSlider] == null)
                {
                    continue;
                }

                string nombre = slidersEscena[indiceSlider].name.ToLowerInvariant();
                if (nombre.Contains("estamina") || nombre.Contains("stamina"))
                {
                    barraEstamina = slidersEscena[indiceSlider];
                    break;
                }
            }
        }

        // Fallback final pedido: cualquier slider disponible.
        if (barraEstamina == null && permitirBusquedaAmplia)
        {
            barraEstamina = FindObjectOfType<Slider>(true);
        }

        // Si falta imagen, intentamos usar fillRect del slider.
        if (imagenRellenoBarra == null && barraEstamina != null && barraEstamina.fillRect != null)
        {
            imagenRellenoBarra = barraEstamina.fillRect.GetComponent<Image>();
        }

        // Si sigue faltando, intentamos ubicar un hijo Fill.
        if (imagenRellenoBarra == null && barraEstamina != null)
        {
            Transform fillDirecto = barraEstamina.transform.Find("Fill Area/Fill");
            if (fillDirecto == null)
            {
                fillDirecto = barraEstamina.transform.Find("Fill");
            }

            if (fillDirecto != null)
            {
                imagenRellenoBarra = fillDirecto.GetComponent<Image>();
            }
        }

        // Si aun falta fill y se permite busqueda amplia, buscamos por nombre en toda la escena.
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
                if (nombre.Contains("fill") && (nombre.Contains("estamina") || nombre.Contains("stamina")))
                {
                    imagenRellenoBarra = imagenesEscena[indiceImagen];
                    break;
                }
            }
        }
    }

    // Este metodo reintenta vinculacion desde Update en intervalos.
    private void ReintentarVinculacionUIEnUpdate()
    {
        // Si ya estan slider y fill validos no hace falta buscar.
        if (TieneReferenciasUIValidas())
        {
            return;
        }

        // Si no toca reintento todavia, salimos.
        if (Time.unscaledTime < tiempoProximoReintentoUI)
        {
            return;
        }

        // Programamos siguiente intento.
        tiempoProximoReintentoUI = Time.unscaledTime + 0.5f;

        // Reintentamos vinculacion completa.
        IntentarVincularUI(true);
    }

    // Esta corutina reintenta vincular UI varias veces por si aparece tarde.
    private IEnumerator RutinaReintentarVinculacionUI(int cantidadIntentos, float esperaEntreIntentos)
    {
        // Evitamos corutinas duplicadas.
        if (reintentoUIEnCurso)
        {
            yield break;
        }

        // Marcamos reintento en curso.
        reintentoUIEnCurso = true;

        // Ejecutamos intentos limitados.
        for (int intento = 0; intento < cantidadIntentos; intento++)
        {
            // Si ya esta vinculada la barra, terminamos.
            if (barraEstamina != null)
            {
                break;
            }

            // Reintentamos con busqueda completa.
            IntentarVincularUI(true);

            // Esperamos entre intentos.
            yield return new WaitForSecondsRealtime(esperaEntreIntentos);
        }

        // Marcamos fin de la rutina.
        reintentoUIEnCurso = false;
    }

    // Este callback se ejecuta al cargar una escena nueva.
    private void ManejarEscenaCargada(Scene escenaCargada, LoadSceneMode modoCarga)
    {
        // Limpiamos referencias para re-vincular con UI de la nueva escena.
        barraEstamina = null;
        imagenRellenoBarra = null;
        tiempoProximoReintentoUI = 0f;

        // Intentamos vincular enseguida.
        IntentarVincularUI(true);

        // Si sigue faltando, lanzamos reintentos.
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RutinaReintentarVinculacionUI(15, 0.2f));
        }
    }
}
