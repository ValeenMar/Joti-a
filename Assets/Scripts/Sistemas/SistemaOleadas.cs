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
    }

    // Esta funcion se ejecuta una vez al empezar la escena.
    private void Start()
    {
        // Si no se asigno prefab, intentamos encontrar una plantilla usable.
        if (prefabEnemigo == null)
        {
            prefabEnemigo = BuscarPlantillaEnemigoAutomaticamente();
        }

        // Si no hay puntos de spawn, intentamos encontrarlos en hijos.
        if (puntosSpawn == null || puntosSpawn.Length == 0)
        {
            puntosSpawn = ObtenerPuntosSpawnEnHijos();
        }

        // Si debe iniciar solo, arrancamos el ciclo de oleadas.
        if (iniciarAutomaticamente)
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
        // Si no hay prefab o puntos de spawn, no podemos hacer nada.
        if (prefabEnemigo == null || puntosSpawn == null || puntosSpawn.Length == 0)
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
            GameObject enemigoCreado = Instantiate(prefabEnemigo, puntoElegido.position, puntoElegido.rotation);

            // Lo renombramos para depurar mejor en la jerarquia.
            enemigoCreado.name = prefabEnemigo.name + "_Oleada_" + oleadaActual + "_" + indiceEnemigo;

            // Registramos este enemigo dentro de la oleada activa.
            enemigosActivos.Add(enemigoCreado);

            // Si hay mas enemigos que puntos, separamos cada spawn para evitar apilarlos.
            if (necesitaSeparacionTemporal && indiceEnemigo < cantidadEnemigosEstaOleada - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Marcamos que ya termino de generarse toda la oleada.
        spawnEnProgresoOleada = false;
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
        // Buscamos el primer EnemigoDummy disponible.
        EnemigoDummy enemigoDummy = FindObjectOfType<EnemigoDummy>();

        // Si encontramos uno, usamos su GameObject como plantilla.
        if (enemigoDummy != null)
        {
            return enemigoDummy.gameObject;
        }

        // Si no hay ninguno, devolvemos nulo.
        return null;
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

            // Si el hijo existe, lo agregamos a la lista.
            if (hijoActual != null)
            {
                puntosEncontrados.Add(hijoActual);
            }
        }

        // Convertimos la lista a array y la devolvemos.
        return puntosEncontrados.ToArray();
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
