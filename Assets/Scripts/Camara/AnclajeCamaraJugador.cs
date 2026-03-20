using UnityEngine;

// Esta clase crea y mantiene un punto de anclaje para que la camara siga al jugador desde una altura estable.
public class AnclajeCamaraJugador : MonoBehaviour
{
    // Este texto define el nombre que tendra el objeto hijo usado como anclaje.
    [SerializeField] private string nombreAnclaje = "AnclajeCamara";

    // Este valor define la posicion local del anclaje respecto del jugador.
    [SerializeField] private Vector3 desplazamientoLocal = new Vector3(0f, 1.82f, 0f);

    // Esta opcion indica si el script puede crear el anclaje automaticamente si no existe.
    [SerializeField] private bool crearAnclajeAutomaticamente = true;

    // Esta variable guarda la referencia al transform del anclaje real.
    [SerializeField] private Transform transformAnclaje;

    // Esta propiedad permite que otros scripts lean el transform del anclaje.
    public Transform TransformAnclaje => transformAnclaje;

    // Esta funcion corre al iniciar el objeto y asegura que exista un anclaje valido.
    private void Awake()
    {
        // Si ya hay un anclaje asignado desde el Inspector, no necesitamos buscarlo.
        if (transformAnclaje != null)
        {
            // Antes de salir, forzamos su posicion para que arranque correcta.
            ActualizarAnclaje();
            return;
        }

        // Buscamos un hijo con el nombre configurado para reutilizarlo si ya existe.
        Transform hijoExistente = transform.Find(nombreAnclaje);

        // Si encontramos un hijo con ese nombre, lo usamos como anclaje.
        if (hijoExistente != null)
        {
            transformAnclaje = hijoExistente;
            ActualizarAnclaje();
            return;
        }

        // Si no existe y se permite crearlo automaticamente, creamos el objeto hijo.
        if (crearAnclajeAutomaticamente)
        {
            // Creamos un nuevo GameObject que actuara como punto de seguimiento de camara.
            GameObject objetoAnclaje = new GameObject(nombreAnclaje);

            // Hacemos que el anclaje sea hijo del jugador para que se mueva junto con el.
            objetoAnclaje.transform.SetParent(transform);

            // Guardamos la referencia para usarla desde otros scripts.
            transformAnclaje = objetoAnclaje.transform;

            // Ajustamos su posicion y rotacion local en este mismo frame.
            ActualizarAnclaje();
        }
    }

    // Esta funcion corre despues de Update y mantiene el anclaje en su offset correcto.
    private void LateUpdate()
    {
        // Si por algun motivo no hay anclaje, salimos sin intentar actualizar nada.
        if (transformAnclaje == null)
        {
            return;
        }

        // Aseguramos cada frame la posicion local definida para evitar desplazamientos accidentales.
        ActualizarAnclaje();
    }

    // Este metodo aplica el offset y rotacion local correctos al anclaje.
    private void ActualizarAnclaje()
    {
        // Ubicamos el anclaje a la altura y desplazamiento local configurados.
        transformAnclaje.localPosition = desplazamientoLocal;

        // Dejamos la rotacion local en identidad para que solo herede la rotacion del jugador.
        transformAnclaje.localRotation = Quaternion.identity;
    }

    // Este metodo devuelve el mejor objetivo para la camara, usando anclaje si existe.
    public Transform ObtenerObjetivoCamara()
    {
        // Si tenemos anclaje valido, devolvemos ese transform.
        if (transformAnclaje != null)
        {
            return transformAnclaje;
        }

        // Si no hay anclaje, devolvemos el transform del propio jugador como fallback seguro.
        return transform;
    }
}
