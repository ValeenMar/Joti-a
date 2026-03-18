using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor agrega el sistema de pociones al jugador
// y deja valores por defecto sin tener que configurar a mano.

// Esta clase agrega un menu que prepara el sistema de pociones del jugador.
public static class ConfiguradorPociones
{
    // Esta opcion de menu busca al jugador y le agrega el sistema de pociones.
    [MenuItem("Realm Brawl/Configurar/Pociones del Jugador")]
    public static void ConfigurarPocionesDelJugador()
    {
        // Buscamos un jugador existente en la escena.
        GameObject jugador = BuscarJugador();

        // Si no encontramos ninguno, avisamos y salimos.
        if (jugador == null)
        {
            EditorUtility.DisplayDialog("Realm Brawl", "No encontre un objeto Jugador con VidaJugador en la escena.", "Entendido");
            return;
        }

        // Buscamos o agregamos el componente VidaJugador.
        VidaJugador vidaJugador = jugador.GetComponent<VidaJugador>();

        // Si falta, lo agregamos para que el sistema tenga donde curar.
        if (vidaJugador == null)
        {
            vidaJugador = jugador.AddComponent<VidaJugador>();
        }

        // Buscamos o agregamos el componente de pociones.
        SistemaPociones sistemaPociones = jugador.GetComponent<SistemaPociones>();

        // Si no estaba, lo agregamos ahora.
        if (sistemaPociones == null)
        {
            sistemaPociones = jugador.AddComponent<SistemaPociones>();
        }

        // Asignamos referencias y valores por defecto usando SerializedObject.
        SerializedObject serializadoPociones = new SerializedObject(sistemaPociones);
        serializadoPociones.FindProperty("vidaJugador").objectReferenceValue = vidaJugador;
        serializadoPociones.FindProperty("teclaUsarPocion").intValue = (int)KeyCode.H;
        serializadoPociones.FindProperty("cantidadInicialPociones").intValue = 3;
        serializadoPociones.FindProperty("cantidadMaximaPociones").intValue = 5;
        serializadoPociones.FindProperty("cantidadActualPociones").intValue = 3;
        serializadoPociones.FindProperty("curacionPorPocion").floatValue = 35f;
        serializadoPociones.FindProperty("enfriamientoUso").floatValue = 0.35f;
        serializadoPociones.ApplyModifiedPropertiesWithoutUndo();

        // Marcamos la escena como modificada para poder guardarla.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Marcamos el componente como sucio para que guarde referencias.
        EditorUtility.SetDirty(sistemaPociones);

        // Seleccionamos al jugador para que el usuario vea el resultado.
        Selection.activeGameObject = jugador;
    }

    // Este metodo intenta encontrar el jugador principal de la escena.
    private static GameObject BuscarJugador()
    {
        // Primero intentamos encontrar un VidaJugador ya configurado.
        VidaJugador vidaJugador = Object.FindObjectOfType<VidaJugador>();

        // Si existe, devolvemos su GameObject.
        if (vidaJugador != null)
        {
            return vidaJugador.gameObject;
        }

        // Si no, probamos por tag Player.
        GameObject jugadorPorTag = GameObject.FindGameObjectWithTag("Player");

        // Si existe, lo devolvemos.
        if (jugadorPorTag != null)
        {
            return jugadorPorTag;
        }

        // Si no, probamos por nombre Jugador.
        GameObject jugadorPorNombre = GameObject.Find("Jugador");

        // Devolvemos el resultado aunque sea nulo.
        return jugadorPorNombre;
    }

    // COPILOT-EXPAND: Aqui podes agregar configuracion automatica de pickups de pociones y balance por dificultad.
}
