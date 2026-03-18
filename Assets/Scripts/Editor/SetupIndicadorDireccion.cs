using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor crea indicadores direccionales simples con primitivas
// para que jugador y enemigos muestren claramente hacia donde miran.

// Esta clase agrega un menu para crear indicadores de direccion en capsulas sin modelo.
public static class SetupIndicadorDireccion
{
    // Esta opcion de menu crea indicadores sobre el jugador y todos los enemigos de la escena.
    [MenuItem("Realm Brawl/Configurar/Indicadores de Direccion")]
    public static void ConfigurarIndicadoresDireccion()
    {
        // Intentamos encontrar el jugador por nombre o por componente.
        GameObject jugador = BuscarJugador();

        // Si existe jugador, le creamos o actualizamos su indicador.
        if (jugador != null)
        {
            CrearOActualizarIndicador(jugador.transform, "IndicadorDireccionJugador", new Color(0.15f, 0.85f, 1f, 1f));
        }

        // Buscamos todos los enemigos dummy de la escena.
        EnemigoDummy[] enemigos = Object.FindObjectsOfType<EnemigoDummy>(true);

        // Recorremos cada enemigo encontrado.
        for (int indiceEnemigo = 0; indiceEnemigo < enemigos.Length; indiceEnemigo++)
        {
            if (enemigos[indiceEnemigo] != null)
            {
                CrearOActualizarIndicador(enemigos[indiceEnemigo].transform, "IndicadorDireccionEnemigo", new Color(1f, 0.25f, 0.2f, 1f));
            }
        }

        // Marcamos la escena como modificada para que pueda guardarse.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // Este metodo intenta encontrar el jugador principal de la escena.
    private static GameObject BuscarJugador()
    {
        // Primero probamos con VidaJugador porque es el dato mas confiable.
        VidaJugador vidaJugador = Object.FindObjectOfType<VidaJugador>(true);

        // Si existe, devolvemos su objeto.
        if (vidaJugador != null)
        {
            return vidaJugador.gameObject;
        }

        // Si no, intentamos por nombre.
        return GameObject.Find("Jugador");
    }

    // Este metodo crea o actualiza el indicador direccional para un personaje.
    private static void CrearOActualizarIndicador(Transform objetivo, string nombreIndicador, Color colorIndicador)
    {
        // Si falta el objetivo, no hacemos nada.
        if (objetivo == null)
        {
            return;
        }

        // Buscamos si ya existia un indicador anterior.
        Transform indicadorExistente = objetivo.Find(nombreIndicador);

        // Si no existe, creamos la raiz del indicador.
        if (indicadorExistente == null)
        {
            GameObject raizIndicador = new GameObject(nombreIndicador);
            raizIndicador.transform.SetParent(objetivo, false);
            indicadorExistente = raizIndicador.transform;
        }

        // Colocamos la raiz un poco por encima y delante de la capsula.
        indicadorExistente.localPosition = new Vector3(0f, 1.05f, 0.55f);
        indicadorExistente.localRotation = Quaternion.identity;
        indicadorExistente.localScale = Vector3.one;

        // Creamos o actualizamos la base alargada de la flecha.
        GameObject cuerpoFlecha = BuscarOCrearPrimitiva(indicadorExistente, "Cuerpo", PrimitiveType.Cube);
        cuerpoFlecha.transform.localPosition = new Vector3(0f, 0f, 0.16f);
        cuerpoFlecha.transform.localRotation = Quaternion.identity;
        cuerpoFlecha.transform.localScale = new Vector3(0.12f, 0.12f, 0.36f);

        // Creamos o actualizamos la punta frontal de la flecha.
        GameObject puntaFlecha = BuscarOCrearPrimitiva(indicadorExistente, "Punta", PrimitiveType.Cube);
        puntaFlecha.transform.localPosition = new Vector3(0f, 0f, 0.42f);
        puntaFlecha.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        puntaFlecha.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);

        // Aplicamos el color pedido a ambas piezas.
        AplicarColor(cuerpoFlecha, colorIndicador);
        AplicarColor(puntaFlecha, colorIndicador);
    }

    // Este metodo busca una primitiva hija o la crea si no existe.
    private static GameObject BuscarOCrearPrimitiva(Transform padre, string nombre, PrimitiveType tipo)
    {
        // Buscamos si ya existe una hija con ese nombre.
        Transform hijaExistente = padre.Find(nombre);

        // Si existe, devolvemos su GameObject.
        if (hijaExistente != null)
        {
            return hijaExistente.gameObject;
        }

        // Si no existe, creamos una primitiva nueva.
        GameObject objetoNuevo = GameObject.CreatePrimitive(tipo);

        // Le ponemos el nombre correcto.
        objetoNuevo.name = nombre;

        // La hacemos hija del indicador.
        objetoNuevo.transform.SetParent(padre, false);

        // Si tenia collider, lo quitamos para que no interfiera con gameplay.
        Collider collider = objetoNuevo.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        // Devolvemos el objeto listo.
        return objetoNuevo;
    }

    // Este metodo aplica un color simple a una primitiva.
    private static void AplicarColor(GameObject objeto, Color color)
    {
        // Si falta el objeto, no seguimos.
        if (objeto == null)
        {
            return;
        }

        // Obtenemos el renderer de la primitiva.
        Renderer renderer = objeto.GetComponent<Renderer>();

        // Si no tiene renderer, no podemos colorearlo.
        if (renderer == null)
        {
            return;
        }

        // Buscamos un shader basico compatible.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        // Si no existe ese shader, caemos al Standard clasico.
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        // Si no encontramos ningun shader, dejamos el material como esta.
        if (shader == null)
        {
            return;
        }

        // Creamos un material nuevo solo para este indicador.
        Material material = new Material(shader);

        // Le asignamos el color deseado.
        material.color = color;

        // Lo colocamos como material compartido para que quede guardado en escena.
        renderer.sharedMaterial = material;
    }

    // COPILOT-EXPAND: Aqui podes cambiar la flecha por un mesh custom, iconos flotantes o indicadores por equipo.
}
