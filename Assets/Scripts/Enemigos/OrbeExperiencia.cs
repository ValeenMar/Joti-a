using UnityEngine;

// Esta clase controla una orbe visual de experiencia con flotacion y rotacion.
[RequireComponent(typeof(Collider))]
public class OrbeExperiencia : MonoBehaviour
{
    // Esta cantidad es la experiencia que entregara la orbe al jugador.
    [SerializeField] private int experienciaOtorgada = 25;

    // Esta velocidad controla la rotacion continua de la orbe.
    [SerializeField] private float velocidadRotacion = 120f;

    // Esta amplitud controla cuanto sube y baja la orbe.
    [SerializeField] private float amplitudFlotacion = 0.15f;

    // Esta frecuencia controla que tan rapido oscila la flotacion.
    [SerializeField] private float frecuenciaFlotacion = 2f;

    // Esta duracion limita cuanto tiempo vive la orbe en el suelo.
    [SerializeField] private float duracionMaxima = 12f;

    // Esta variable guarda la posicion base para animar flotacion.
    private Vector3 posicionBase;

    // Esta bandera evita recolectar dos veces la misma orbe.
    private bool yaRecolectada;

    // Esta funcion se ejecuta al crear o activar el objeto.
    private void Awake()
    {
        // Guardamos la posicion inicial como base para la oscilacion.
        posicionBase = transform.position;

        // Obtenemos el collider de la orbe.
        Collider colliderOrbe = GetComponent<Collider>();

        // Forzamos que sea trigger para detectar recoleccion sin empujar.
        colliderOrbe.isTrigger = true;
    }

    // Esta funcion se ejecuta una sola vez al inicio.
    private void Start()
    {
        // Programamos autodestruccion para limpiar orbes olvidadas.
        Destroy(gameObject, duracionMaxima);
    }

    // Esta funcion se ejecuta en cada frame para animacion visual.
    private void Update()
    {
        // Rotamos la orbe sobre su eje Y para llamar la atencion.
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.World);

        // Calculamos desplazamiento vertical senoidal.
        float desplazamientoY = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;

        // Actualizamos posicion oscilante alrededor de la base.
        transform.position = posicionBase + new Vector3(0f, desplazamientoY, 0f);
    }

    // Este metodo se dispara cuando algo entra al trigger.
    private void OnTriggerEnter(Collider otroCollider)
    {
        // Si ya fue recolectada, ignoramos nuevas entradas.
        if (yaRecolectada)
        {
            // Salimos para evitar doble premio.
            return;
        }

        // Verificamos si el objeto que entra parece ser el jugador.
        bool esJugador = otroCollider.GetComponent<VidaJugador>() != null || otroCollider.GetComponentInParent<VidaJugador>() != null;

        // Si no es jugador, ignoramos el trigger.
        if (!esJugador)
        {
            // Salimos porque solo el jugador puede recolectar.
            return;
        }

        // Marcamos la orbe como ya recolectada.
        yaRecolectada = true;

        // Primero intentamos encontrar un SistemaXP en el jugador o en sus padres.
        SistemaXP sistemaXP = otroCollider.GetComponentInParent<SistemaXP>();

        // Si existe sistema XP, le damos la experiencia de forma directa y segura.
        if (sistemaXP != null)
        {
            // Sumamos experiencia al jugador que recogio la orbe.
            sistemaXP.RecogerExperienciaDesdeOrbe(experienciaOtorgada);
        }
        else
        {
            // Si no encontramos el sistema, probamos con un mensaje hacia arriba como respaldo.
            otroCollider.SendMessageUpwards("RecogerExperienciaDesdeOrbe", experienciaOtorgada, SendMessageOptions.DontRequireReceiver);
        }

        // Destruimos la orbe al ser recogida.
        Destroy(gameObject);
    }
}
