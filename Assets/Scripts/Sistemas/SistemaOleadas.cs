using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla oleadas de enemigos usando un prefab o una plantilla de escena,
// crea enemigos en puntos de spawn y se integra con GameManager.

// Esta clase administra el ciclo completo de oleadas de enemigos.
public class SistemaOleadas : MonoBehaviour
{
    private static Material materialHuesoRuntimeCache;
    private static readonly Color colorMaterialHuesoRuntime = new Color(0.82f, 0.78f, 0.70f, 1f);
    private const float glossinessMaterialHuesoRuntime = 0.05f;
    private const float metallicMaterialHuesoRuntime = 0f;

    // Esta referencia apunta al GameManager de la partida.
    [SerializeField] private GameManager gameManager;

    // Esta referencia apunta al prefab o plantilla base del enemigo.
    [SerializeField] private GameObject prefabEnemigo;

    // Esta referencia guarda una plantilla runtime limpia para no clonar enemigos ya modificados en escena.
    private GameObject plantillaEnemigoRuntime;

    // Este texto guarda un nombre seguro para las copias aunque la fuente original se destruya despues.
    private string nombreBaseEnemigo = "EnemigoDummy";

    // Esta lista contiene los puntos donde pueden aparecer enemigos.
    [SerializeField] private Transform[] puntosSpawn;

    // Este valor define cuántos enemigos trae la primera oleada.
    [SerializeField] private int cantidadBaseEnemigos = 2;

    // Este valor suma enemigos extra por cada oleada nueva.
    [SerializeField] private int incrementoEnemigosPorOleada = 1;

    // Este valor define cuántos segundos pasan antes de la primera oleada.
    [SerializeField] private float demoraInicialPrimeraOleada = 1.5f;

    // Este valor define cuántos segundos pasan entre una oleada y la siguiente.
    [SerializeField] private float tiempoEntreOleadas = 4f;

    // Esta bandera decide si arranca sola al comenzar la escena.
    [SerializeField] private bool iniciarAutomaticamente = true;

    // Esta posicion controla dónde se dibuja el HUD de oleadas.
    [SerializeField] private Vector2 posicionHud = new Vector2(20f, 150f);

    // Esta bandera permite apagar el HUD OnGUI cuando usamos una UI mas prolija en Canvas.
    [SerializeField] private bool mostrarHudOnGui = true;

    // Esta variable define cuanto dura el fade in y fade out del anuncio de oleada.
    [SerializeField] private float duracionFadeAnuncioOleada = 0.3f;

    // Esta variable define cuanto tiempo queda el anuncio fijo en pantalla.
    [SerializeField] private float duracionMantenerAnuncioOleada = 1.5f;

    // Este color se usa para el anuncio grande de oleada.
    [SerializeField] private Color colorAnuncioOleada = new Color(1f, 0.84f, 0.26f, 1f);

    // Este color se usa para el texto del countdown entre oleadas.
    [SerializeField] private Color colorCountdownOleada = new Color(1f, 1f, 1f, 0.92f);

    // Este color define el fondo de la placa medieval del anuncio.
    [SerializeField] private Color colorFondoOverlayOleada = new Color(0.07f, 0.05f, 0.03f, 0.82f);

    // Este color define el borde principal de la placa del anuncio.
    [SerializeField] private Color colorBordeOverlayOleada = new Color(0.84f, 0.67f, 0.30f, 0.95f);

    // Este color define un borde interior mas oscuro para darle profundidad.
    [SerializeField] private Color colorBordeInteriorOverlayOleada = new Color(0.22f, 0.15f, 0.06f, 0.95f);

    // Este color se usa para la sombra fuerte del texto.
    [SerializeField] private Color colorSombraOverlayOleada = new Color(0f, 0f, 0f, 0.92f);

    // Esta fraccion de vida se restaura entre oleadas.
    [SerializeField] private float restauracionVidaEntreOleadas = 0.2f;

    // Esta fraccion de estamina se restaura entre oleadas.
    [SerializeField] private float restauracionEstaminaEntreOleadas = 0.2f;

    // Este multiplicador escala la vida de los enemigos segun la oleada.
    [SerializeField] private float multiplicadorVidaPorOleada = 0.15f;

    // Este multiplicador escala el dano de los enemigos segun la oleada.
    [SerializeField] private float multiplicadorDanioPorOleada = 0.10f;

    // Esta oleada define desde cuando aparece al menos un enemigo elite.
    [SerializeField] private int oleadaEliteDesde = 3;

    // Este multiplicador de vida adicional se aplica al enemigo elite.
    [SerializeField] private float multiplicadorVidaElite = 2f;

    // Esta escala visual se aplica al enemigo elite.
    [SerializeField] private float escalaElite = 1.3f;

    // Este valor guarda la oleada actual.
    [SerializeField] private int oleadaActual = 0;

    // Esta referencia guarda la corrutina del anuncio de oleada.
    private Coroutine rutinaAnuncioOleada;

    // Esta referencia guarda la corrutina del countdown entre oleadas.
    private Coroutine rutinaCountdownEntreOleadas;

    // Esta variable guarda la oleada que se esta anunciando visualmente.
    private int oleadaAnuncioActual;

    // Esta variable guarda el alpha del anuncio de oleada.
    private float alphaAnuncioOleada;

    // Esta variable guarda el tiempo restante del countdown visible.
    private float tiempoRestanteCountdownEntreOleadas;

    // Esta variable indica si estamos en una pausa entre oleadas.
    private bool pausaEntreOleadasActiva;

    // Esta variable indica si una oleada esta en curso.
    private bool oleadaActiva;

    // Esta variable indica si todavia faltan enemigos por aparecer de la oleada actual.
    private bool spawnEnProgresoOleada;

    // Este indice reparte los spawns de forma rotativa entre todos los puntos disponibles.
    private int indiceSpawnRotativo;

    // Esta lista guarda solo los enemigos creados por este sistema.
    private readonly HashSet<GameObject> enemigosActivos = new HashSet<GameObject>();

    // Esta referencia guarda la corrutina principal de control.
    private Coroutine corrutinaControlOleadas;

    // Esta referencia guarda el estilo del HUD.
    private GUIStyle estiloHud;

    // Esta referencia guarda el estilo grande para el anuncio de oleada.
    private GUIStyle estiloAnuncioOleada;

    // Esta referencia guarda el estilo para el countdown entre oleadas.
    private GUIStyle estiloCountdownOleada;

    // Esta referencia guarda el sistema de pociones del jugador para otorgar recompensas entre oleadas.
    private SistemaPociones sistemaPociones;

    // Esta referencia guarda la vida del jugador para curarlo parcialmente entre oleadas.
    private VidaJugador vidaJugadorObjetivo;

    // Esta referencia guarda la estamina del jugador para recargarla parcialmente entre oleadas.
    private Estamina estaminaJugadorObjetivo;

    // Esta propiedad permite leer la oleada actual desde otros scripts.
    public int OleadaActual => oleadaActual;

    // Esta propiedad permite leer cuántos enemigos quedan vivos de la oleada actual.
    public int EnemigosRestantes => enemigosActivos.Count;

    // Esta propiedad expone si el HUD legacy de OnGUI esta visible.
    public bool MostrarHudOnGui => mostrarHudOnGui;

    // Esta propiedad permite a la UI saber si estamos esperando la proxima oleada.
    public bool PausaEntreOleadasActiva => pausaEntreOleadasActiva;

    // Esta propiedad expone el tiempo restante del countdown visible entre oleadas.
    public float TiempoRestanteCountdownEntreOleadas => Mathf.Max(0f, tiempoRestanteCountdownEntreOleadas);

    // Esta funcion se ejecuta al iniciar el componente.
    private void Awake()
    {
        // Si no se asigno GameManager, intentamos obtenerlo automaticamente.
        if (gameManager == null)
        {
            gameManager = GameManager.Instancia != null ? GameManager.Instancia : FindObjectOfType<GameManager>();
        }
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Limpiamos estado runtime para soportar restart con recarga parcial de escena.
        ReiniciarEstadoRuntime();

        // Reintentamos resolver referencias del jugador y recompensas por si la escena se recargo.
        BuscarReferenciasJugadorYRecompensa();

        // Escuchamos el evento global de enemigo eliminado para descontar enemigos vivos.
        EventosJuego.AlEnemigoEliminado += ManejarEnemigoEliminado;
    }

    // Esta funcion se ejecuta al desactivar el componente.
    private void OnDisable()
    {
        // Dejamos de escuchar el evento global para evitar referencias colgadas.
        EventosJuego.AlEnemigoEliminado -= ManejarEnemigoEliminado;

        // Si habia una corrutina corriendo, la frenamos.
        if (corrutinaControlOleadas != null)
        {
            StopCoroutine(corrutinaControlOleadas);
            corrutinaControlOleadas = null;
        }

        // Si habia un anuncio de oleada activo, lo detenemos para no dejar UI colgada.
        if (rutinaAnuncioOleada != null)
        {
            StopCoroutine(rutinaAnuncioOleada);
            rutinaAnuncioOleada = null;
        }

        // Si habia un countdown de entre oleadas activo, lo detenemos.
        if (rutinaCountdownEntreOleadas != null)
        {
            StopCoroutine(rutinaCountdownEntreOleadas);
            rutinaCountdownEntreOleadas = null;
        }

        // Si habiamos creado una plantilla runtime temporal, la destruimos para no dejar basura al salir.
        if (plantillaEnemigoRuntime != null && plantillaEnemigoRuntime != prefabEnemigo)
        {
            Destroy(plantillaEnemigoRuntime);
            plantillaEnemigoRuntime = null;
        }
    }

    // Esta funcion se ejecuta una vez al empezar la escena.
    private void Start()
    {
        // Si no se asigno GameManager, lo buscamos de nuevo por si fue creado antes en esta escena.
        if (gameManager == null)
        {
            gameManager = GameManager.Instancia != null ? GameManager.Instancia : FindObjectOfType<GameManager>();
        }

        // Reintentamos resolver referencias del jugador y recompensas.
        BuscarReferenciasJugadorYRecompensa();

        // Si no hay puntos de spawn, intentamos encontrarlos en hijos.
        if (puntosSpawn == null || puntosSpawn.Length == 0)
        {
            puntosSpawn = ObtenerPuntosSpawnEnHijos();
        }

        // Si sigue sin haber puntos, creamos una configuracion minima alrededor del jugador.
        if (puntosSpawn == null || puntosSpawn.Length == 0)
        {
            puntosSpawn = CrearPuntosSpawnPorDefecto();
        }

        // Si no se asigno prefab, intentamos encontrar una plantilla usable.
        if (prefabEnemigo == null)
        {
            prefabEnemigo = BuscarPlantillaEnemigoAutomaticamente();
        }

        // Si ya conocemos una fuente base, guardamos su nombre antes de que pueda destruirse en runtime.
        if (prefabEnemigo != null)
        {
            nombreBaseEnemigo = prefabEnemigo.name;
        }

        // Preparamos una fuente de spawn limpia para que las oleadas futuras no hereden estado de un enemigo muerto.
        PrepararPlantillaEnemigoRuntimeSiHaceFalta();

        // Si debe iniciar solo, arrancamos el ciclo de oleadas.
        if (iniciarAutomaticamente && prefabEnemigo != null && puntosSpawn != null && puntosSpawn.Length > 0)
        {
            IniciarSistemaOleadas();
        }
    }

    // Este metodo arranca el loop principal de oleadas.
    public void IniciarSistemaOleadas()
    {
        // Si ya hay una corrutina corriendo, no iniciamos otra.
        if (corrutinaControlOleadas != null)
        {
            return;
        }

        // Lanzamos la corrutina principal de control.
        corrutinaControlOleadas = StartCoroutine(CorrutinaControlOleadas());
    }

    // Esta corrutina controla el flujo completo entre oleadas.
    private IEnumerator CorrutinaControlOleadas()
    {
        // Esperamos un poco antes de la primera oleada.
        yield return new WaitForSecondsRealtime(demoraInicialPrimeraOleada);

        // Mientras este objeto siga activo, controlamos oleadas.
        while (enabled)
        {
            // Si la partida termino, salimos del sistema.
            if (gameManager != null && gameManager.EstadoActual == EstadoPartida.GameOver)
            {
                corrutinaControlOleadas = null;
                yield break;
            }

            // Si no hay una oleada activa, iniciamos la siguiente.
            if (!oleadaActiva)
            {
                yield return StartCoroutine(IniciarSiguienteOleada());
            }

            // Mientras falten enemigos por aparecer o queden vivos en escena, esperamos.
            while (spawnEnProgresoOleada || enemigosActivos.Count > 0)
            {
                // Limpiamos referencias nulas por si algun enemigo desaparecio por otra via.
                LimpiarEnemigosNulos();

                // Si el juego termino en medio de la espera, abortamos.
                if (gameManager != null && gameManager.EstadoActual == EstadoPartida.GameOver)
                {
                    corrutinaControlOleadas = null;
                    yield break;
                }

                // Esperamos al siguiente frame.
                yield return null;
            }

            // Marcamos que la oleada actual ya termino.
            oleadaActiva = false;

            // Dejamos una traza clara en consola para depurar transiciones entre oleadas.
            Debug.Log("[SistemaOleadas] Oleada " + oleadaActual + " completada.");

            // Esperamos antes de lanzar la siguiente.
            yield return StartCoroutine(RutinaEntreOleadas());
        }

        // Limpiamos la referencia si salimos del while.
        corrutinaControlOleadas = null;
    }

    // Este metodo inicia una nueva oleada con cantidad escalada.
    private IEnumerator IniciarSiguienteOleada()
    {
        // Buscamos la mejor fuente de spawn disponible para esta oleada.
        GameObject fuenteSpawnEnemigo = ObtenerFuenteSpawnEnemigo();

        // Si no hay fuente de spawn o puntos, no podemos hacer nada.
        if (fuenteSpawnEnemigo == null || puntosSpawn == null || puntosSpawn.Length == 0)
        {
            yield break;
        }

        // Aumentamos el contador local de oleada.
        oleadaActual++;

        // Dejamos una traza clara para confirmar el avance de oleadas en consola.
        Debug.Log("[SistemaOleadas] Iniciando oleada " + oleadaActual + ".");

        // Marcamos que hay una oleada en progreso.
        oleadaActiva = true;

        // Marcamos que el spawn de esta oleada ya comenzo.
        spawnEnProgresoOleada = true;

        // Avisamos al GameManager cuál es la nueva oleada.
        if (gameManager != null)
        {
            gameManager.RegistrarOleadaNueva(oleadaActual);
        }

        // Disparamos el anuncio visual de la oleada actual.
        ProgramarAnuncioOleada(oleadaActual);

        // Calculamos cuántos enemigos debe traer esta oleada.
        int cantidadEnemigosEstaOleada = Mathf.Max(1, cantidadBaseEnemigos + incrementoEnemigosPorOleada * Mathf.Max(0, oleadaActual - 1));

        // Definimos si hace falta espaciar temporalmente los spawns porque hay mas enemigos que puntos.
        bool necesitaSeparacionTemporal = cantidadEnemigosEstaOleada > puntosSpawn.Length;

        // Creamos todos los enemigos de esta oleada.
        for (int indiceEnemigo = 0; indiceEnemigo < cantidadEnemigosEstaOleada; indiceEnemigo++)
        {
            // Elegimos un punto de spawn de forma rotativa para repartir mejor la oleada.
            Transform puntoElegido = null;

            // Intentamos encontrar el siguiente punto valido sin usar random.
            for (int intentoPunto = 0; intentoPunto < puntosSpawn.Length; intentoPunto++)
            {
                int indicePuntoActual = (indiceSpawnRotativo + intentoPunto) % puntosSpawn.Length;

                if (puntosSpawn[indicePuntoActual] != null)
                {
                    puntoElegido = puntosSpawn[indicePuntoActual];
                    indiceSpawnRotativo = (indicePuntoActual + 1) % puntosSpawn.Length;
                    break;
                }
            }

            // Si el punto elegido no existe, pasamos al siguiente.
            if (puntoElegido == null)
            {
                continue;
            }

            // Instanciamos un nuevo enemigo en ese punto.
            GameObject enemigoCreado = Instantiate(fuenteSpawnEnemigo, puntoElegido.position, puntoElegido.rotation);

            // Si la fuente era una plantilla inactiva, activamos la copia creada para que entre a jugar normalmente.
            if (!enemigoCreado.activeSelf)
            {
                enemigoCreado.SetActive(true);
            }

            // Reaplicamos el material hueso runtime por seguridad apenas se instancia la copia.
            AplicarMaterialHuesoRuntime(enemigoCreado);

            // Hacemos que el enemigo arranque mirando al jugador para que la deteccion no dependa de una rotacion azarosa.
            OrientarEnemigoHaciaJugadorSiExiste(enemigoCreado);

            // Escalamos vida y dano segun la oleada, y marcamos elite si corresponde.
            bool esElite = oleadaActual >= oleadaEliteDesde && indiceEnemigo == 0;
            ConfigurarEscaladoEnemigo(enemigoCreado, oleadaActual, esElite);

            // Lo renombramos para depurar mejor en la jerarquia.
            enemigoCreado.name = nombreBaseEnemigo + "_Oleada_" + oleadaActual + "_" + indiceEnemigo + (esElite ? "_Elite" : "");

            // Registramos este enemigo dentro de la oleada activa.
            enemigosActivos.Add(enemigoCreado);

            // Inicializamos la IA del enemigo despues del spawn para evitar fallos de NavMesh post-restart.
            StartCoroutine(InicializarEnemigoPostSpawn(enemigoCreado));

            // Si hay mas enemigos que puntos, separamos cada spawn para evitar apilarlos.
            if (necesitaSeparacionTemporal && indiceEnemigo < cantidadEnemigosEstaOleada - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Marcamos que ya termino de generarse toda la oleada.
        spawnEnProgresoOleada = false;
    }

    // Esta corrutina inicializa la IA del enemigo al final del frame de instanciacion.
    private IEnumerator InicializarEnemigoPostSpawn(GameObject enemigoCreado)
    {
        // Si por algun motivo no existe enemigo, salimos.
        if (enemigoCreado == null)
        {
            yield break;
        }

        // Esperamos al menos al final del frame para que NavMeshAgent quede listo tras spawn/reload.
        yield return new WaitForEndOfFrame();

        // Si el enemigo ya no existe despues de esperar, abortamos.
        if (enemigoCreado == null)
        {
            yield break;
        }

        // Buscamos el script de IA principal en el enemigo recien creado.
        EnemigoDummy enemigoDummy = enemigoCreado.GetComponent<EnemigoDummy>();

        // Si existe IA, pedimos reinicio diferido post-spawn.
        if (enemigoDummy != null)
        {
            enemigoDummy.ReiniciarIATrasSpawn();
        }
    }

    // Este metodo responde cuando muere un enemigo.
    private void ManejarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        // Si el enemigo reportado es nulo, no hay nada que quitar.
        if (enemigo == null)
        {
            return;
        }

        // Si ese enemigo pertenecia a esta oleada, lo removemos del conjunto.
        if (enemigosActivos.Contains(enemigo))
        {
            enemigosActivos.Remove(enemigo);
        }
    }

    // Esta corrutina maneja la pausa entre oleadas, la recompensa y el countdown visible.
    private IEnumerator RutinaEntreOleadas()
    {
        // Marcamos que estamos en pausa entre rondas.
        pausaEntreOleadasActiva = true;

        // Reiniciamos el countdown visible.
        tiempoRestanteCountdownEntreOleadas = Mathf.Max(0f, tiempoEntreOleadas);

        // Otorgamos la recompensa una sola vez por intermedio de oleadas.
        OtorgarRecompensasEntreOleadas();

        // Si ya habia una rutina previa, la detenemos para no superponer countdowns.
        if (rutinaCountdownEntreOleadas != null)
        {
            StopCoroutine(rutinaCountdownEntreOleadas);
        }

        // Usamos esta misma corrutina como referencia para poder frenarla al salir.
        rutinaCountdownEntreOleadas = StartCoroutine(CorrutinaCountdownEntreOleadas());

        // Esperamos hasta que termine la pausa entre oleadas.
        while (pausaEntreOleadasActiva)
        {
            // Si la partida termino durante la pausa, abortamos.
            if (gameManager != null && gameManager.EstadoActual == EstadoPartida.GameOver)
            {
                pausaEntreOleadasActiva = false;
                break;
            }

            yield return null;
        }

        // Limpiamos la referencia del countdown si sigue apuntando a esta rutina.
        if (rutinaCountdownEntreOleadas != null)
        {
            StopCoroutine(rutinaCountdownEntreOleadas);
            rutinaCountdownEntreOleadas = null;
        }
    }

    // Esta corrutina baja el countdown entre oleadas usando tiempo real.
    private IEnumerator CorrutinaCountdownEntreOleadas()
    {
        // Bajamos el contador hasta cero.
        while (tiempoRestanteCountdownEntreOleadas > 0f && pausaEntreOleadasActiva)
        {
            tiempoRestanteCountdownEntreOleadas -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Limpiamos estado visible.
        tiempoRestanteCountdownEntreOleadas = 0f;
        pausaEntreOleadasActiva = false;
        rutinaCountdownEntreOleadas = null;
    }

    // Esta funcion programa el anuncio visual de la oleada actual.
    private void ProgramarAnuncioOleada(int numeroOleada)
    {
        // Si ya habia un anuncio activo, lo detenemos para evitar superposiciones.
        if (rutinaAnuncioOleada != null)
        {
            StopCoroutine(rutinaAnuncioOleada);
            rutinaAnuncioOleada = null;
        }

        // Guardamos el numero para dibujarlo en pantalla.
        oleadaAnuncioActual = numeroOleada;

        // Arrancamos el anuncio con fade completo.
        rutinaAnuncioOleada = StartCoroutine(RutinaAnuncioOleada(numeroOleada));
    }

    // Esta corrutina dibuja un anuncio que entra, queda y sale.
    private IEnumerator RutinaAnuncioOleada(int numeroOleada)
    {
        // Limpiamos alpha inicial.
        alphaAnuncioOleada = 0f;

        // Entrada suave.
        float tiempoEntrada = 0f;
        while (tiempoEntrada < Mathf.Max(0.01f, duracionFadeAnuncioOleada))
        {
            tiempoEntrada += Time.unscaledDeltaTime;
            alphaAnuncioOleada = Mathf.Clamp01(tiempoEntrada / Mathf.Max(0.01f, duracionFadeAnuncioOleada));
            yield return null;
        }

        // Mantenemos el anuncio firme.
        alphaAnuncioOleada = 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duracionMantenerAnuncioOleada));

        // Salida suave.
        float tiempoSalida = 0f;
        while (tiempoSalida < Mathf.Max(0.01f, duracionFadeAnuncioOleada))
        {
            tiempoSalida += Time.unscaledDeltaTime;
            alphaAnuncioOleada = 1f - Mathf.Clamp01(tiempoSalida / Mathf.Max(0.01f, duracionFadeAnuncioOleada));
            yield return null;
        }

        // Limpiamos estado final.
        alphaAnuncioOleada = 0f;
        if (oleadaAnuncioActual == numeroOleada)
        {
            rutinaAnuncioOleada = null;
        }
    }

    // Esta funcion busca el jugador y los sistemas de recompensa para la pausa entre oleadas.
    private void BuscarReferenciasJugadorYRecompensa()
    {
        // Intentamos encontrar la vida del jugador si aun no esta cacheada.
        if (vidaJugadorObjetivo == null)
        {
            vidaJugadorObjetivo = FindObjectOfType<VidaJugador>(true);
        }

        // Intentamos encontrar la estamina del jugador si aun no esta cacheada.
        if (estaminaJugadorObjetivo == null)
        {
            estaminaJugadorObjetivo = FindObjectOfType<Estamina>(true);
        }

        // Intentamos encontrar el sistema de pociones si aun no esta cacheado.
        if (sistemaPociones == null)
        {
            sistemaPociones = FindObjectOfType<SistemaPociones>(true);
        }
    }

    // Esta funcion otorga la recompensa entre oleadas al jugador y genera una pocion recolectable.
    private void OtorgarRecompensasEntreOleadas()
    {
        // Reintentamos referencias por si la escena se recompuso.
        BuscarReferenciasJugadorYRecompensa();

        // Curamos una fraccion de la vida maxima del jugador.
        if (vidaJugadorObjetivo != null && vidaJugadorObjetivo.EstaVivo)
        {
            float curacion = vidaJugadorObjetivo.VidaMaxima * Mathf.Clamp01(restauracionVidaEntreOleadas);
            vidaJugadorObjetivo.Curar(curacion);
            Debug.Log("[SistemaOleadas] Recompensa entre oleadas: curacion parcial aplicada.");
        }

        // Reponemos una fraccion de estamina usando reflexion porque el sistema no expone un setter publico.
        if (estaminaJugadorObjetivo != null)
        {
            RestaurarEstaminaParcial(estaminaJugadorObjetivo, restauracionEstaminaEntreOleadas);
        }

        // Generamos una pocion visual recolectable en el centro del mapa.
        // La recompensa real se entrega cuando el jugador la recoge para no duplicar la cantidad.
        CrearPocionRecompensaVisual();
    }

    // Esta funcion restaura estamina directamente para una pausa entre oleadas.
    private void RestaurarEstaminaParcial(Estamina estamina, float fraccionMaxima)
    {
        if (estamina == null)
        {
            return;
        }

        float cantidadRestaurar = estamina.EstaminaMaxima * Mathf.Clamp01(fraccionMaxima);
        System.Type tipoEstamina = typeof(Estamina);

        FieldInfo campoActual = tipoEstamina.GetField("estaminaActual", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo campoAgotado = tipoEstamina.GetField("estaAgotado", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo campoTiempoGasto = tipoEstamina.GetField("tiempoUltimoGasto", BindingFlags.Instance | BindingFlags.NonPublic);

        if (campoActual == null)
        {
            return;
        }

        float valorActual = System.Convert.ToSingle(campoActual.GetValue(estamina));
        float nuevoValor = Mathf.Clamp(valorActual + cantidadRestaurar, 0f, estamina.EstaminaMaxima);
        campoActual.SetValue(estamina, nuevoValor);

        if (campoAgotado != null && nuevoValor >= 35f)
        {
            campoAgotado.SetValue(estamina, false);
        }

        if (campoTiempoGasto != null)
        {
            campoTiempoGasto.SetValue(estamina, Time.time - 999f);
        }

        Debug.Log("[SistemaOleadas] Recompensa entre oleadas: estamina parcial aplicada.");
    }

    // Esta funcion crea una pocion visual recolectable en el centro del mapa.
    private void CrearPocionRecompensaVisual()
    {
        // Buscamos el centro del mapa si existe, o el origen como respaldo.
        Vector3 centroMapa = Vector3.zero;
        GameObject mapaMedieval = GameObject.Find("MapaMedieval");
        if (mapaMedieval != null)
        {
            centroMapa = mapaMedieval.transform.position;
        }

        // Creamos la esfera visual.
        GameObject pocion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pocion.name = "PocionRecompensaOleada";
        pocion.transform.position = centroMapa + Vector3.up * 1.2f;
        pocion.transform.localScale = Vector3.one * 0.65f;

        // Le damos un aspecto verde simple.
        Renderer rendererPocion = pocion.GetComponent<Renderer>();
        if (rendererPocion != null)
        {
            Material materialPocion = new Material(Shader.Find("Standard"));
            materialPocion.color = new Color(0.18f, 0.85f, 0.42f, 1f);
            materialPocion.SetFloat("_Glossiness", 0.25f);
            rendererPocion.sharedMaterial = materialPocion;
        }

        // La convertimos en trigger recolectable.
        Collider collider = pocion.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Le agregamos un rigidbody para que los triggers funcionen de forma confiable.
        Rigidbody cuerpo = pocion.AddComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;

        // Le agregamos el recolector local.
        PocionRecompensaRecolectable recolectable = pocion.AddComponent<PocionRecompensaRecolectable>();
        recolectable.Inicializar(this);

        // La limpiamos sola si nadie la recoge.
        Destroy(pocion, 20f);
        Debug.Log("[SistemaOleadas] Pocion de recompensa generada en el centro del mapa.");
    }

    // Esta funcion escala al enemigo segun la oleada y define si es elite.
    private void ConfigurarEscaladoEnemigo(GameObject enemigoCreado, int numeroOleada, bool esElite)
    {
        if (enemigoCreado == null)
        {
            return;
        }

        float multiplicadorVida = 1f + (multiplicadorVidaPorOleada * Mathf.Max(1, numeroOleada));
        float multiplicadorDanio = 1f + (multiplicadorDanioPorOleada * Mathf.Max(1, numeroOleada));

        if (esElite)
        {
            multiplicadorVida *= multiplicadorVidaElite;
        }

        VidaEnemigo vidaEnemigo = enemigoCreado.GetComponent<VidaEnemigo>();
        if (vidaEnemigo != null)
        {
            vidaEnemigo.AplicarEscaladoOleada(multiplicadorVida);
        }

        EnemigoDummy enemigoDummy = enemigoCreado.GetComponent<EnemigoDummy>();
        if (enemigoDummy != null)
        {
            enemigoDummy.AplicarEscaladoOleada(multiplicadorDanio);
        }

        if (esElite)
        {
            enemigoCreado.transform.localScale *= escalaElite;
            OscurecerVisualElite(enemigoCreado);
        }
    }

    // Esta funcion oscurece visualmente al enemigo elite para que se distinga del resto.
    private void OscurecerVisualElite(GameObject enemigoCreado)
    {
        if (enemigoCreado == null)
        {
            return;
        }

        Renderer[] renderers = enemigoCreado.GetComponentsInChildren<Renderer>(true);
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            Renderer rendererActual = renderers[indiceRenderer];
            if (rendererActual == null)
            {
                continue;
            }

            Material[] materiales = rendererActual.materials;
            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material materialActual = materiales[indiceMaterial];
                if (materialActual == null)
                {
                    continue;
                }

                if (materialActual.HasProperty("_Color"))
                {
                    Color colorOriginal = materialActual.color;
                    materialActual.color = new Color(colorOriginal.r * 0.78f, colorOriginal.g * 0.78f, colorOriginal.b * 0.78f, colorOriginal.a);
                }

                if (materialActual.HasProperty("_Glossiness"))
                {
                    materialActual.SetFloat("_Glossiness", Mathf.Min(0.08f, materialActual.GetFloat("_Glossiness")));
                }
            }
        }
    }

    // Este metodo busca una plantilla de enemigo automaticamente en la escena.
    private GameObject BuscarPlantillaEnemigoAutomaticamente()
    {
        // Buscamos todos los EnemigoDummy, incluso si estan desactivados, para poder reutilizar un template de escena oculto.
        EnemigoDummy[] enemigosDisponibles = FindObjectsOfType<EnemigoDummy>(true);

        // Recorremos candidatos hasta encontrar uno valido como plantilla base.
        for (int indiceEnemigo = 0; indiceEnemigo < enemigosDisponibles.Length; indiceEnemigo++)
        {
            EnemigoDummy enemigoDummy = enemigosDisponibles[indiceEnemigo];

            // Ignoramos referencias nulas por seguridad.
            if (enemigoDummy == null)
            {
                continue;
            }

            // Ignoramos la propia plantilla runtime si ya existiera para no caer en recursividad rara.
            if (plantillaEnemigoRuntime != null && enemigoDummy.gameObject == plantillaEnemigoRuntime)
            {
                continue;
            }

            // Tomamos el primer candidato usable y guardamos su nombre base.
            nombreBaseEnemigo = enemigoDummy.gameObject.name;
            return enemigoDummy.gameObject;
        }

        // Si no hay ninguno, devolvemos nulo.
        return null;
    }

    // Este metodo deja lista una plantilla runtime limpia cuando la fuente de spawn viene de un enemigo de escena.
    private void PrepararPlantillaEnemigoRuntimeSiHaceFalta()
    {
        // Si no hay fuente base configurada, no podemos preparar nada.
        if (prefabEnemigo == null)
        {
            plantillaEnemigoRuntime = null;
            return;
        }

        // Si ya existe una plantilla runtime valida, la reutilizamos sin crear otra.
        if (plantillaEnemigoRuntime != null)
        {
            return;
        }

        // Si la fuente es un prefab real o un objeto fuera de escena, lo usamos directo.
        if (!prefabEnemigo.scene.IsValid())
        {
            nombreBaseEnemigo = prefabEnemigo.name;
            plantillaEnemigoRuntime = prefabEnemigo;
            AplicarMaterialHuesoRuntime(plantillaEnemigoRuntime);
            return;
        }

        // Si la fuente esta en escena, creamos una copia limpia ahora mismo para no heredar muerte, colliders apagados o estados raros.
        plantillaEnemigoRuntime = Instantiate(prefabEnemigo, transform);

        // Le damos un nombre claro para depuracion.
        plantillaEnemigoRuntime.name = prefabEnemigo.name + "_PlantillaRuntime";

        // Guardamos el nombre base seguro antes de que la fuente de escena pueda destruirse.
        nombreBaseEnemigo = prefabEnemigo.name;

        // Dejamos la plantilla apagada para que no participe de la partida.
        plantillaEnemigoRuntime.SetActive(false);

        // Corregimos los materiales del template runtime para que las copias posteriores hereden el arreglo visual.
        AplicarMaterialHuesoRuntime(plantillaEnemigoRuntime);
    }

    // Este metodo devuelve la fuente correcta desde donde se deben instanciar enemigos nuevos.
    private GameObject ObtenerFuenteSpawnEnemigo()
    {
        // Si por algun motivo no existe plantilla runtime pero si existe fuente base, la preparamos ahora.
        if (plantillaEnemigoRuntime == null && prefabEnemigo != null)
        {
            PrepararPlantillaEnemigoRuntimeSiHaceFalta();
        }

        // Si tenemos plantilla runtime, esa es la mas segura.
        if (plantillaEnemigoRuntime != null)
        {
            return plantillaEnemigoRuntime;
        }

        // Como fallback final usamos el prefab/base original.
        return prefabEnemigo;
    }

    // Este metodo crea o reutiliza el material hueso runtime para esqueletos spawneados.
    private static Material ObtenerMaterialHuesoRuntime()
    {
        if (materialHuesoRuntimeCache != null)
        {
            return materialHuesoRuntimeCache;
        }

        Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            Debug.LogWarning("[SistemaOleadas] No se encontro un shader compatible para el material hueso runtime.");
            return null;
        }

        materialHuesoRuntimeCache = new Material(shader)
        {
            name = "Material_Esqueleto_Hueso_Runtime",
            hideFlags = HideFlags.HideAndDontSave
        };
        materialHuesoRuntimeCache.color = colorMaterialHuesoRuntime;
        materialHuesoRuntimeCache.SetFloat("_Glossiness", glossinessMaterialHuesoRuntime);
        materialHuesoRuntimeCache.SetFloat("_Metallic", metallicMaterialHuesoRuntime);
        return materialHuesoRuntimeCache;
    }

    // Este metodo aplica el material hueso runtime solo a esqueletos que aun estan amarillos o sin textura.
    private static void AplicarMaterialHuesoRuntime(GameObject enemigo)
    {
        if (enemigo == null)
        {
            return;
        }

        Material materialHueso = ObtenerMaterialHuesoRuntime();
        if (materialHueso == null)
        {
            return;
        }

        SkinnedMeshRenderer[] renderers = enemigo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            SkinnedMeshRenderer smr = renderers[indiceRenderer];
            if (smr == null)
            {
                continue;
            }

            Material[] materiales = smr.sharedMaterials;
            bool modifico = false;
            for (int indiceMaterial = 0; indiceMaterial < materiales.Length; indiceMaterial++)
            {
                Material material = materiales[indiceMaterial];
                if (material == null)
                {
                    continue;
                }

                if (!EsMaterialHuesoObjetivo(material))
                {
                    continue;
                }

                materiales[indiceMaterial] = materialHueso;
                modifico = true;
            }

            if (modifico)
            {
                smr.sharedMaterials = materiales;
            }
        }
    }

    // Este metodo detecta si un material todavia esta en estado amarillo/default o sin textura base.
    private static bool EsMaterialHuesoObjetivo(Material material)
    {
        if (material == null)
        {
            return false;
        }

        bool sinTextura =
            material.mainTexture == null &&
            (!material.HasProperty("_MainTex") || material.GetTexture("_MainTex") == null) &&
            (!material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null);

        Color colorActual = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        bool colorAmarillo = colorActual.r > 0.7f && colorActual.g > 0.5f && colorActual.b < 0.3f;

        return sinTextura || colorAmarillo;
    }

    // Este metodo intenta reunir todos los puntos de spawn hijos de este objeto.
    private Transform[] ObtenerPuntosSpawnEnHijos()
    {
        // Creamos una lista temporal para juntar los puntos validos.
        List<Transform> puntosEncontrados = new List<Transform>();

        // Recorremos todos los hijos inmediatos de este objeto.
        for (int indiceHijo = 0; indiceHijo < transform.childCount; indiceHijo++)
        {
            // Guardamos referencia al hijo actual.
            Transform hijoActual = transform.GetChild(indiceHijo);

            // Ignoramos hijos nulos.
            if (hijoActual == null)
            {
                continue;
            }

            // Ignoramos la plantilla runtime y cualquier hijo que claramente sea un enemigo en vez de un punto de spawn.
            if (hijoActual.GetComponent<EnemigoDummy>() != null || hijoActual.name.Contains("PlantillaRuntime"))
            {
                continue;
            }

            // Si paso todos los filtros, lo agregamos como punto valido.
            puntosEncontrados.Add(hijoActual);
        }

        // Convertimos la lista a array y la devolvemos.
        return puntosEncontrados.ToArray();
    }

    // Este metodo crea un conjunto simple de puntos de spawn si la escena no los trae armados.
    private Transform[] CrearPuntosSpawnPorDefecto()
    {
        // Buscamos o creamos un contenedor para los puntos de spawn.
        GameObject contenedorSpawn = GameObject.Find("PuntosSpawnOleadas");

        if (contenedorSpawn == null)
        {
            contenedorSpawn = new GameObject("PuntosSpawnOleadas");
        }

        // Si el contenedor no es hijo de este sistema, lo parentamos para mantener orden.
        if (contenedorSpawn.transform.parent != transform)
        {
            contenedorSpawn.transform.SetParent(transform);
        }

        // Si ya tenia hijos, reutilizamos esos puntos existentes.
        if (contenedorSpawn.transform.childCount > 0)
        {
            return ObtenerPuntosDesdeContenedor(contenedorSpawn.transform);
        }

        // Buscamos al jugador para usarlo como centro de referencia.
        GameObject jugador = GameObject.Find("Jugador");

        // Tomamos el centro del jugador o el origen si no existe.
        Vector3 centro = jugador != null ? jugador.transform.position : Vector3.zero;

        // Definimos cuatro offsets simples para que la arena vuelva a funcionar.
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(6f, 0f, 6f),
            new Vector3(-6f, 0f, 6f),
            new Vector3(6f, 0f, -6f),
            new Vector3(-6f, 0f, -6f)
        };

        // Creamos los puntos con esos offsets.
        for (int indiceOffset = 0; indiceOffset < offsets.Length; indiceOffset++)
        {
            GameObject puntoSpawn = new GameObject("PuntoSpawn_" + (indiceOffset + 1));
            puntoSpawn.transform.SetParent(contenedorSpawn.transform);
            puntoSpawn.transform.position = centro + offsets[indiceOffset];

            // Hacemos que el punto quede orientado hacia el centro para que los enemigos nazcan mirando la arena.
            Vector3 direccionHaciaCentro = centro - puntoSpawn.transform.position;
            direccionHaciaCentro.y = 0f;

            if (direccionHaciaCentro.sqrMagnitude > 0.001f)
            {
                puntoSpawn.transform.rotation = Quaternion.LookRotation(direccionHaciaCentro.normalized, Vector3.up);
            }
        }

        // Devolvemos el array de puntos recien creado.
        return ObtenerPuntosDesdeContenedor(contenedorSpawn.transform);
    }

    // Este metodo convierte todos los hijos de un contenedor en un array de transforms utilizable.
    private Transform[] ObtenerPuntosDesdeContenedor(Transform contenedor)
    {
        // Creamos una lista temporal para reunir puntos validos.
        List<Transform> puntos = new List<Transform>();

        // Recorremos todos los hijos del contenedor.
        for (int indiceHijo = 0; indiceHijo < contenedor.childCount; indiceHijo++)
        {
            Transform hijoActual = contenedor.GetChild(indiceHijo);

            if (hijoActual == null)
            {
                continue;
            }

            // Ignoramos cualquier hijo que no sea realmente un punto de spawn util.
            if (hijoActual.GetComponent<EnemigoDummy>() != null || hijoActual.name.Contains("PlantillaRuntime"))
            {
                continue;
            }

            puntos.Add(hijoActual);
        }

        // Devolvemos todos los puntos encontrados.
        return puntos.ToArray();
    }

    // Este metodo orienta al enemigo recien creado hacia el jugador para que la IA pueda detectarlo mejor al arrancar.
    private void OrientarEnemigoHaciaJugadorSiExiste(GameObject enemigoCreado)
    {
        // Si el enemigo no existe, no hacemos nada.
        if (enemigoCreado == null)
        {
            return;
        }

        // Intentamos encontrar un jugador por tag.
        GameObject jugador = null;

        try
        {
            jugador = GameObject.FindWithTag("Player");
        }
        catch (UnityException)
        {
            jugador = GameObject.Find("Jugador");
        }

        // Si no encontramos jugador, no tocamos la rotacion.
        if (jugador == null)
        {
            return;
        }

        // Calculamos una direccion horizontal hacia el jugador.
        Vector3 direccion = jugador.transform.position - enemigoCreado.transform.position;
        direccion.y = 0f;

        // Si la direccion es valida, aplicamos una rotacion inicial mirando hacia el jugador.
        if (direccion.sqrMagnitude > 0.001f)
        {
            enemigoCreado.transform.rotation = Quaternion.LookRotation(direccion.normalized, Vector3.up);
        }
    }

    // Este metodo limpia referencias invalidas, muertas o inactivas que puedan quedar en el conjunto de enemigos.
    private void LimpiarEnemigosNulos()
    {
        // Si no hay ningun enemigo, no hace falta limpiar nada.
        if (enemigosActivos.Count == 0)
        {
            return;
        }

        // Creamos una lista temporal con los que deben salir.
        List<GameObject> enemigosAEliminar = new List<GameObject>();

        // Recorremos todos los enemigos guardados.
        foreach (GameObject enemigoActual in enemigosActivos)
        {
            // Si alguno ya no existe, lo marcamos para limpieza.
            if (enemigoActual == null)
            {
                enemigosAEliminar.Add(enemigoActual);
                continue;
            }

            // Si el objeto ya no esta activo en jerarquia, no debe seguir contando para la oleada.
            if (!enemigoActual.activeInHierarchy)
            {
                enemigosAEliminar.Add(enemigoActual);
                continue;
            }

            // Si tiene vida enemiga y ya no esta vivo, tampoco debe bloquear la siguiente oleada.
            VidaEnemigo vidaEnemigo = enemigoActual.GetComponent<VidaEnemigo>();
            if (vidaEnemigo != null && !vidaEnemigo.EstaVivo)
            {
                enemigosAEliminar.Add(enemigoActual);
            }
        }

        // Eliminamos todos los enemigos invalidos encontrados.
        for (int indiceNulo = 0; indiceNulo < enemigosAEliminar.Count; indiceNulo++)
        {
            enemigosActivos.Remove(enemigosAEliminar[indiceNulo]);
        }
    }

    // Este metodo deja el sistema limpio para reinicios de escena o reactivaciones.
    private void ReiniciarEstadoRuntime()
    {
        // Reiniciamos banderas internas de oleada.
        oleadaActiva = false;
        spawnEnProgresoOleada = false;

        // Reiniciamos contador rotativo de spawn.
        indiceSpawnRotativo = 0;

        // Limpiamos enemigos activos por si quedaban referencias de una corrida previa.
        enemigosActivos.Clear();
    }

    // Este metodo prepara el estilo del HUD de oleadas.
    private void PrepararEstiloSiHaceFalta()
    {
        // Si ya existe el estilo, no hacemos nada.
        if (estiloHud != null)
        {
            return;
        }

        // Creamos un estilo basico partiendo de la skin actual.
        estiloHud = new GUIStyle(GUI.skin.label);

        // Subimos un poco el tamaño del texto.
        estiloHud.fontSize = 18;

        // Lo ponemos en negrita para destacarlo.
        estiloHud.fontStyle = FontStyle.Bold;

        // Lo pintamos de blanco.
        estiloHud.normal.textColor = Color.white;
    }

    // Este metodo prepara el estilo grande del anuncio de oleada.
    private void PrepararEstiloAnuncioSiHaceFalta()
    {
        if (estiloAnuncioOleada != null)
        {
            return;
        }

        estiloAnuncioOleada = new GUIStyle(GUI.skin.label);
        estiloAnuncioOleada.fontSize = 34;
        estiloAnuncioOleada.fontStyle = FontStyle.Bold;
        estiloAnuncioOleada.alignment = TextAnchor.MiddleCenter;
        estiloAnuncioOleada.normal.textColor = Color.white;
        estiloAnuncioOleada.richText = true;
    }

    // Este metodo prepara el estilo del countdown entre oleadas.
    private void PrepararEstiloCountdownSiHaceFalta()
    {
        if (estiloCountdownOleada != null)
        {
            return;
        }

        estiloCountdownOleada = new GUIStyle(GUI.skin.label);
        estiloCountdownOleada.fontSize = 20;
        estiloCountdownOleada.fontStyle = FontStyle.Bold;
        estiloCountdownOleada.alignment = TextAnchor.MiddleCenter;
        estiloCountdownOleada.normal.textColor = Color.white;
        estiloCountdownOleada.richText = true;
    }

    // Este metodo dibuja el anuncio grande y el countdown entre oleadas.
    private void DibujarOverlayOleadas()
    {
        // Si no estamos ni anunciando ni en countdown, no hace falta dibujar nada.
        if (!pausaEntreOleadasActiva && alphaAnuncioOleada <= 0.001f)
        {
            return;
        }

        PrepararEstiloAnuncioSiHaceFalta();
        PrepararEstiloCountdownSiHaceFalta();

        Color colorAnterior = GUI.color;

        if (alphaAnuncioOleada > 0.001f)
        {
            Rect rectPlacaAnuncio = new Rect(Screen.width * 0.5f - 280f, 44f, 560f, 64f);
            DibujarPlacaTematica(rectPlacaAnuncio, alphaAnuncioOleada);

            Rect rectAnuncio = new Rect(rectPlacaAnuncio.x, rectPlacaAnuncio.y + 6f, rectPlacaAnuncio.width, rectPlacaAnuncio.height - 8f);
            DibujarTextoConContorno(rectAnuncio, "OLEADA " + oleadaAnuncioActual, estiloAnuncioOleada, colorAnuncioOleada, alphaAnuncioOleada, 2f);
        }

        if (pausaEntreOleadasActiva)
        {
            float countdownMostrado = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestanteCountdownEntreOleadas));
            Rect rectPlacaCountdown = new Rect(Screen.width * 0.5f - 220f, 114f, 440f, 40f);
            DibujarPlacaTematica(rectPlacaCountdown, 0.96f);

            Rect rectCountdown = new Rect(rectPlacaCountdown.x, rectPlacaCountdown.y + 3f, rectPlacaCountdown.width, rectPlacaCountdown.height - 4f);
            DibujarTextoConContorno(rectCountdown, "PROXIMA RONDA EN " + countdownMostrado, estiloCountdownOleada, colorCountdownOleada, 1f, 1.6f);
        }

        GUI.color = colorAnterior;
    }

    // Este metodo dibuja una placa simple medieval con doble borde para anuncios importantes.
    private void DibujarPlacaTematica(Rect rect, float alpha)
    {
        Color colorAnterior = GUI.color;

        GUI.color = new Color(colorFondoOverlayOleada.r, colorFondoOverlayOleada.g, colorFondoOverlayOleada.b, colorFondoOverlayOleada.a * alpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = new Color(colorBordeOverlayOleada.r, colorBordeOverlayOleada.g, colorBordeOverlayOleada.b, colorBordeOverlayOleada.a * alpha);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);

        Rect rectInterior = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
        GUI.color = new Color(colorBordeInteriorOverlayOleada.r, colorBordeInteriorOverlayOleada.g, colorBordeInteriorOverlayOleada.b, colorBordeInteriorOverlayOleada.a * alpha);
        GUI.DrawTexture(new Rect(rectInterior.x, rectInterior.y, rectInterior.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rectInterior.x, rectInterior.yMax - 1f, rectInterior.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rectInterior.x, rectInterior.y, 1f, rectInterior.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rectInterior.xMax - 1f, rectInterior.y, 1f, rectInterior.height), Texture2D.whiteTexture);

        GUI.color = colorAnterior;
    }

    // Este metodo dibuja texto con sombra y contorno negro para que se lea sobre cualquier fondo.
    private void DibujarTextoConContorno(Rect rect, string texto, GUIStyle estilo, Color colorTexto, float alpha, float distanciaContorno)
    {
        if (string.IsNullOrEmpty(texto) || estilo == null)
        {
            return;
        }

        Color colorAnterior = GUI.color;

        GUI.color = new Color(colorSombraOverlayOleada.r, colorSombraOverlayOleada.g, colorSombraOverlayOleada.b, colorSombraOverlayOleada.a * alpha);
        GUI.Label(new Rect(rect.x - distanciaContorno, rect.y, rect.width, rect.height), texto, estilo);
        GUI.Label(new Rect(rect.x + distanciaContorno, rect.y, rect.width, rect.height), texto, estilo);
        GUI.Label(new Rect(rect.x, rect.y - distanciaContorno, rect.width, rect.height), texto, estilo);
        GUI.Label(new Rect(rect.x, rect.y + distanciaContorno, rect.width, rect.height), texto, estilo);
        GUI.Label(new Rect(rect.x + distanciaContorno, rect.y + distanciaContorno, rect.width, rect.height), texto, estilo);

        GUI.color = new Color(colorTexto.r, colorTexto.g, colorTexto.b, alpha);
        GUI.Label(rect, texto, estilo);

        GUI.color = colorAnterior;
    }

    // Esta funcion dibuja un HUD simple con oleada actual y enemigos restantes.
    private void OnGUI()
    {
        // Si ya existe la UI moderna del jugador, no dibujamos el HUD legacy.
        if (UIJugador.HUDPrincipalActivo)
        {
            return;
        }

        // Si no existe la UI moderna, si mantenemos el overlay grande de oleadas.
        DibujarOverlayOleadas();

        // Si el HUD legacy esta desactivado, no dibujamos nada.
        if (!mostrarHudOnGui)
        {
            return;
        }

        // Si la partida termino, no hace falta mostrar este HUD.
        if (gameManager != null && gameManager.EstadoActual == EstadoPartida.GameOver)
        {
            return;
        }

        // Preparamos el estilo si todavia no esta listo.
        PrepararEstiloSiHaceFalta();

        // Dibujamos el numero de oleada actual.
        GUI.Label(new Rect(posicionHud.x, posicionHud.y, 260f, 24f), "Oleada: " + oleadaActual, estiloHud);

        // Dibujamos cuantos enemigos siguen vivos.
        GUI.Label(new Rect(posicionHud.x, posicionHud.y + 22f, 260f, 24f), "Restantes: " + enemigosActivos.Count, estiloHud);
    }

    // Este metodo permite a un setup externo apagar o prender el HUD viejo sin romper gameplay.
    public void EstablecerHudOnGuiVisible(bool visible)
    {
        // Guardamos el nuevo estado de visibilidad del HUD legacy.
        mostrarHudOnGui = visible;
    }

    // Esta clase pequena representa la pocion visual recolectable que aparece entre oleadas.
    private class PocionRecompensaRecolectable : MonoBehaviour
    {
        // Esta referencia guarda el sistema que creo la pocion.
        private SistemaOleadas sistemaOrigen;

        // Esta funcion inicializa la referencia de origen.
        public void Inicializar(SistemaOleadas sistema)
        {
            sistemaOrigen = sistema;
        }

        // Este metodo se ejecuta cuando un jugador toca la pocion.
        private void OnTriggerEnter(Collider other)
        {
            // Si no tenemos sistema de origen, no hacemos nada.
            if (sistemaOrigen == null || other == null)
            {
                return;
            }

            // Buscamos la vida del jugador en la jerarquia del collider que entro.
            VidaJugador vidaJugador = other.GetComponentInParent<VidaJugador>();
            if (vidaJugador == null || !vidaJugador.EstaVivo)
            {
                return;
            }

            // Buscamos el sistema de pociones en ese mismo jugador.
            SistemaPociones sistemaPociones = other.GetComponentInParent<SistemaPociones>();
            if (sistemaPociones != null)
            {
                sistemaPociones.AgregarPociones(1);
            }

            // Informamos que se recogio la recompensa.
            Debug.Log("[SistemaOleadas] Pocion de recompensa recogida.");

            // Destruimos la pocion visual al recogerla.
            Destroy(gameObject);
        }
    }

    // COPILOT-EXPAND: Aqui podes agregar jefes, pausas entre oleadas, elite mobs y recompensas entre rondas.
}
