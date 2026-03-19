using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script controla oleadas de enemigos usando un prefab o una plantilla de escena,
// crea enemigos en puntos de spawn y se integra con GameManager.

// Esta clase administra el ciclo completo de oleadas de enemigos.
public class SistemaOleadas : MonoBehaviour
{
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

    // Este valor guarda la oleada actual.
    [SerializeField] private int oleadaActual = 0;

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

    // Esta propiedad permite leer la oleada actual desde otros scripts.
    public int OleadaActual => oleadaActual;

    // Esta propiedad permite leer cuántos enemigos quedan vivos de la oleada actual.
    public int EnemigosRestantes => enemigosActivos.Count;

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
        yield return new WaitForSeconds(demoraInicialPrimeraOleada);

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

            // Esperamos antes de lanzar la siguiente.
            yield return new WaitForSeconds(tiempoEntreOleadas);
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

        // Marcamos que hay una oleada en progreso.
        oleadaActiva = true;

        // Marcamos que el spawn de esta oleada ya comenzo.
        spawnEnProgresoOleada = true;

        // Avisamos al GameManager cuál es la nueva oleada.
        if (gameManager != null)
        {
            gameManager.RegistrarOleadaNueva(oleadaActual);
        }

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

            // Hacemos que el enemigo arranque mirando al jugador para que la deteccion no dependa de una rotacion azarosa.
            OrientarEnemigoHaciaJugadorSiExiste(enemigoCreado);

            // Lo renombramos para depurar mejor en la jerarquia.
            enemigoCreado.name = nombreBaseEnemigo + "_Oleada_" + oleadaActual + "_" + indiceEnemigo;

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

    // Este metodo limpia referencias nulas que puedan quedar en el conjunto de enemigos.
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
            }
        }

        // Eliminamos todos los nulos encontrados.
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

    // Esta funcion dibuja un HUD simple con oleada actual y enemigos restantes.
    private void OnGUI()
    {
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

    // COPILOT-EXPAND: Aqui podes agregar jefes, pausas entre oleadas, elite mobs y recompensas entre rondas.
}
