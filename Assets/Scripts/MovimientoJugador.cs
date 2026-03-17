using UnityEngine;

// Esta línea le dice a Unity que este objeto necesita un Rigidbody para funcionar.
[RequireComponent(typeof(Rigidbody))]
public class MovimientoJugador : MonoBehaviour
{
    // Esta variable aparece en el Inspector para que puedas cambiar la velocidad sin tocar código.
    public float velocidadMovimiento = 5f;

    // Esta variable guarda la referencia al Rigidbody del jugador.
    private Rigidbody cuerpoRigido;

    // Esta variable guarda las estadísticas del jugador si existen en el mismo objeto.
    private EstadisticasJugador estadisticasJugador;

    // Esta variable guarda cuánto quiere moverse el jugador en horizontal.
    private float entradaHorizontal;

    // Esta variable guarda cuánto quiere moverse el jugador en vertical.
    private float entradaVertical;

    // Esta función se ejecuta una vez cuando el objeto se activa.
    private void Awake()
    {
        // Acá buscamos y guardamos el Rigidbody que está en este mismo objeto.
        cuerpoRigido = GetComponent<Rigidbody>();

        // Acá intentamos obtener las estadísticas del jugador para usar una velocidad escalable.
        estadisticasJugador = GetComponent<EstadisticasJugador>();

        // Esto hace que el movimiento se vea más suave en pantalla.
        cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;

        // Esto ayuda a detectar mejor colisiones cuando el objeto se mueve.
        cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Esta función se ejecuta una vez por frame y es ideal para leer el teclado.
    private void Update()
    {
        // Leemos la entrada horizontal del teclado con A, D o flechas izquierda y derecha.
        entradaHorizontal = Input.GetAxisRaw("Horizontal");

        // Leemos la entrada vertical del teclado con W, S o flechas arriba y abajo.
        entradaVertical = Input.GetAxisRaw("Vertical");
    }

    // Esta función se ejecuta a ritmo de física y es ideal para mover un Rigidbody.
    private void FixedUpdate()
    {
        // Armamos un vector con la dirección en la que queremos movernos.
        Vector3 direccionMovimiento = new Vector3(entradaHorizontal, 0f, entradaVertical);

        // Si se aprietan dos teclas al mismo tiempo, normalizamos para que no corra más rápido en diagonal.
        if (direccionMovimiento.magnitude > 1f)
        {
            // Convertimos la dirección en una dirección de largo 1 para mantener una velocidad pareja.
            direccionMovimiento = direccionMovimiento.normalized;
        }

        // Elegimos la velocidad final usando estadísticas si existen, o la velocidad manual si no existen.
        float velocidadFinal = estadisticasJugador != null ? estadisticasJugador.VelocidadActual : velocidadMovimiento;

        // Calculamos cuánto se tiene que mover el jugador en este paso de física.
        Vector3 desplazamiento = direccionMovimiento * velocidadFinal * Time.fixedDeltaTime;

        // Movemos el Rigidbody usando física en vez de mover el Transform a mano.
        cuerpoRigido.MovePosition(cuerpoRigido.position + desplazamiento);
    }
}
