using System.Collections;
using UnityEngine;

// Esta clase muestra un destello y un micro freeze frame al subir de nivel.
public class EfectoSubidaNivel : MonoBehaviour
{
    // Esta referencia apunta al jugador al que debemos reaccionar.
    [SerializeField] private GameObject jugadorObjetivo;

    // Este color se usa para el flash de subida de nivel.
    [SerializeField] private Color colorDestello = new Color(0.95f, 0.9f, 0.45f, 1f);

    // Este efecto de particulas es opcional para reforzar la subida de nivel.
    [SerializeField] private ParticleSystem efectoParticulas;

    // Este valor define cuanto dura el freeze frame en segundos reales.
    [SerializeField] private float duracionFreezeFrame = 0.1f;

    // Este valor define la duracion del flash visual.
    [SerializeField] private float duracionDestello = 0.35f;

    // Esta variable guarda la corrutina actual para no duplicar efectos.
    private Coroutine corrutinaActiva;

    // Esta variable guarda el alpha actual del destello fullscreen.
    private float alphaDestello;

    // Esta funcion se ejecuta al crear el objeto.
    private void Awake()
    {
        // Si no se asigno jugador objetivo, usamos este mismo objeto.
        if (jugadorObjetivo == null)
        {
            jugadorObjetivo = gameObject;
        }
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento global de subida de nivel.
        EventosJuego.AlJugadorSubioNivel += ManejarJugadorSubioNivel;
    }

    // Esta funcion se ejecuta al desactivar el componente.
    private void OnDisable()
    {
        // Nos desuscribimos para evitar errores al destruir objetos.
        EventosJuego.AlJugadorSubioNivel -= ManejarJugadorSubioNivel;
    }

    // Este metodo se llama cuando cualquier jugador sube de nivel.
    private void ManejarJugadorSubioNivel(GameObject jugador, int nivelAnterior, int nivelNuevo)
    {
        // Si el evento no es para nuestro jugador objetivo, ignoramos.
        if (jugador != jugadorObjetivo)
        {
            return;
        }

        // Si ya hay una corrutina corriendo, la detenemos para reiniciar limpio.
        if (corrutinaActiva != null)
        {
            StopCoroutine(corrutinaActiva);
        }

        // Iniciamos la secuencia completa de freeze y destello.
        corrutinaActiva = StartCoroutine(CorrutinaSubidaNivel());
    }

    // Esta corrutina hace el freeze frame y el destello visual.
    private IEnumerator CorrutinaSubidaNivel()
    {
        // En Mirror, el servidor validaria la subida y cada cliente reproduciria el efecto por RPC.

        // Disparamos particulas opcionales si existen.
        if (efectoParticulas != null)
        {
            efectoParticulas.Play();
        }

        // Guardamos el timeScale actual para restaurarlo despues.
        float timeScaleAnterior = Time.timeScale;

        // Aplicamos freeze total del juego.
        Time.timeScale = 0f;

        // Esperamos tiempo real para que no dependa del timeScale.
        yield return new WaitForSecondsRealtime(duracionFreezeFrame);

        // Restauramos el timeScale previo.
        Time.timeScale = timeScaleAnterior;

        // Reiniciamos el contador de la animacion visual.
        float tiempo = 0f;

        // Mientras no termine la animacion, actualizamos alpha.
        while (tiempo < duracionDestello)
        {
            // Sumamos tiempo no escalado para que el efecto sea consistente.
            tiempo += Time.unscaledDeltaTime;

            // Calculamos el progreso entre 0 y 1.
            float progreso = Mathf.Clamp01(tiempo / duracionDestello);

            // Usamos una curva triangular para subir y bajar el brillo.
            alphaDestello = 1f - Mathf.Abs(2f * progreso - 1f);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Dejamos el destello apagado al final.
        alphaDestello = 0f;

        // Limpiamos referencia de corrutina activa.
        corrutinaActiva = null;
    }

    // Esta funcion dibuja el destello fullscreen en pantalla.
    private void OnGUI()
    {
        // Si el alpha es practicamente cero, no dibujamos nada.
        if (alphaDestello <= 0.001f)
        {
            return;
        }

        // Guardamos el color GUI anterior.
        Color colorAnterior = GUI.color;

        // Aplicamos el color de destello con alpha actual.
        GUI.color = new Color(colorDestello.r, colorDestello.g, colorDestello.b, alphaDestello);

        // Dibujamos una textura blanca a pantalla completa para simular el flash.
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        // Restauramos el color GUI anterior.
        GUI.color = colorAnterior;
    }
}
