using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase controla el ataque de espada basado en movimiento previo del mouse.
public class SistemaEspada : MonoBehaviour
{
    // Esta estructura guarda una muestra de movimiento de mouse con su tiempo.
    private struct MuestraMouse
    {
        // Esta variable guarda el delta de mouse del frame.
        public Vector2 deltaMouse;

        // Esta variable guarda el tiempo exacto de la muestra.
        public float tiempoMuestra;
    }

    // Esta referencia apunta al hitbox de la espada.
    [SerializeField] private HitboxEspada hitboxEspada;

    // Esta referencia opcional al objeto visual de la espada para girarlo durante swing.
    [SerializeField] private Transform pivoteVisualEspada;

    // Esta variable define cuanto tiempo guardamos del historial del mouse.
    [SerializeField] private float duracionBufferMouse = 0.2f;

    // Esta variable define el danio base si no hay EstadisticasJugador.
    [SerializeField] private float danioBaseRespaldo = 10f;

    // Esta variable define el delay desde click hasta activar la hitbox.
    [SerializeField] private float delayAntesDelDanio = 0.12f;

    // Esta variable define cuanto tiempo queda activa la hitbox.
    [SerializeField] private float duracionVentanaGolpe = 0.2f;

    // Esta variable define el tiempo minimo entre ataques.
    [SerializeField] private float enfriamientoAtaque = 0.45f;

    // Esta variable define un minimo de movimiento de mouse para validar swing.
    [SerializeField] private float minimoMagnitudBuffer = 0.05f;

    // Esta variable controla si queremos un giro visual simple de espada.
    [SerializeField] private bool usarGiroVisual = true;

    // Esta variable define grados maximos del giro visual.
    [SerializeField] private float anguloMaximoGiroVisual = 35f;

    // Esta cola guarda muestras recientes de mouse.
    private readonly Queue<MuestraMouse> bufferMouse = new Queue<MuestraMouse>();

    // Esta referencia guarda estadisticas de jugador para escalar danio.
    private EstadisticasJugador estadisticasJugador;

    // Esta variable indica si estamos en medio de un ataque.
    private bool ataqueEnCurso;

    // Esta variable guarda el tiempo desde el que se permite volver a atacar.
    private float tiempoProximoAtaque;

    // Esta variable guarda la direccion del ultimo swing para debug y uso externo.
    private Vector3 ultimaDireccionSwing = Vector3.forward;

    // Esta funcion publica permite leer la direccion del ultimo swing.
    public Vector3 UltimaDireccionSwing => ultimaDireccionSwing;

    // Esta funcion se ejecuta una vez al activar el objeto.
    private void Awake()
    {
        // Buscamos estadisticas en este objeto para usar danio por nivel.
        estadisticasJugador = GetComponent<EstadisticasJugador>();

        // Si no asignaron hitbox en inspector, intentamos buscarla en hijos.
        if (hitboxEspada == null)
        {
            hitboxEspada = GetComponentInChildren<HitboxEspada>();
        }
    }

    // Esta funcion corre cada frame y se usa para input y buffer.
    private void Update()
    {
        // Guardamos muestra actual del mouse para usar historial de 0.2 segundos.
        RegistrarMuestraMouse();

        // Si el jugador hizo click izquierdo, intentamos atacar.
        if (Input.GetMouseButtonDown(0))
        {
            IntentarAtaque();
        }
    }

    // Esta funcion registra el movimiento de mouse del frame en la cola.
    private void RegistrarMuestraMouse()
    {
        // Leemos el movimiento horizontal del mouse en este frame.
        float deltaX = Input.GetAxisRaw("Mouse X");

        // Leemos el movimiento vertical del mouse en este frame.
        float deltaY = Input.GetAxisRaw("Mouse Y");

        // Creamos una muestra nueva con datos y tiempo.
        MuestraMouse muestra;
        muestra.deltaMouse = new Vector2(deltaX, deltaY);
        muestra.tiempoMuestra = Time.time;

        // Guardamos la muestra en el buffer.
        bufferMouse.Enqueue(muestra);

        // Quitamos del buffer muestras mas viejas que la ventana permitida.
        while (bufferMouse.Count > 0 && Time.time - bufferMouse.Peek().tiempoMuestra > duracionBufferMouse)
        {
            bufferMouse.Dequeue();
        }
    }

    // Esta funcion intenta iniciar un ataque si las condiciones son validas.
    private void IntentarAtaque()
    {
        // Si no hay hitbox configurada, no seguimos para evitar null reference.
        if (hitboxEspada == null)
        {
            return;
        }

        // Si ya estamos atacando, no permitimos otro ataque encima.
        if (ataqueEnCurso)
        {
            return;
        }

        // Si aun no termina enfriamiento, no permitimos atacar.
        if (Time.time < tiempoProximoAtaque)
        {
            return;
        }

        // Calculamos la direccion de swing desde el buffer de mouse.
        Vector3 direccionSwing = CalcularDireccionSwingDesdeBuffer();

        // Si la direccion es muy chica, ignoramos el ataque para evitar golpes accidentales.
        if (direccionSwing.sqrMagnitude < minimoMagnitudBuffer * minimoMagnitudBuffer)
        {
            return;
        }

        // Guardamos la ultima direccion valida para debug y otros sistemas.
        ultimaDireccionSwing = direccionSwing.normalized;

        // En single player arrancamos el ataque directo.
        // Con Mirror, aqui el cliente local enviaria un [Command] al servidor:
        // [Command] CmdSolicitarAtaque(ultimaDireccionSwing, Time.time);
        StartCoroutine(CorrutinaAtaque(ultimaDireccionSwing));
    }

    // Esta funcion calcula direccion de swing usando movimientos recientes del mouse.
    private Vector3 CalcularDireccionSwingDesdeBuffer()
    {
        // Si no hay muestras, devolvemos cero para indicar no hay swing.
        if (bufferMouse.Count == 0)
        {
            return Vector3.zero;
        }

        // Acumulamos deltas del buffer para obtener direccion predominante.
        Vector2 acumulado = Vector2.zero;

        // Recorremos cada muestra guardada.
        foreach (MuestraMouse muestra in bufferMouse)
        {
            // Sumamos deltas crudos para reforzar direccion dominante.
            acumulado += muestra.deltaMouse;
        }

        // Convertimos movimiento de pantalla a direccion local del jugador.
        Vector3 direccionMundo = transform.right * acumulado.x + transform.forward * acumulado.y;

        // Ignoramos componente vertical para mantener golpe sobre plano de juego.
        direccionMundo.y = 0f;

        // Devolvemos la direccion final.
        return direccionMundo;
    }

    // Esta corrutina maneja el timing completo del ataque.
    private IEnumerator CorrutinaAtaque(Vector3 direccionSwing)
    {
        // Marcamos que estamos dentro de un ataque.
        ataqueEnCurso = true;

        // Programamos el proximo tiempo valido de ataque.
        tiempoProximoAtaque = Time.time + enfriamientoAtaque;

        // Si hay pivote visual y esta opcion activa, hacemos un giro rapido para feedback.
        if (usarGiroVisual && pivoteVisualEspada != null)
        {
            // Lanzamos giro visual sin bloquear el resto de la logica.
            StartCoroutine(CorrutinaGiroVisual(direccionSwing));
        }

        // Esperamos el delay para dar sensacion de peso del arma.
        yield return new WaitForSeconds(delayAntesDelDanio);

        // Elegimos el danio base segun estadisticas o valor respaldo.
        float danioBase = estadisticasJugador != null ? estadisticasJugador.DanioActual : danioBaseRespaldo;

        // Preparamos la hitbox con datos del golpe actual.
        hitboxEspada.PrepararGolpe(gameObject, danioBase, direccionSwing);

        // Abrimos la ventana de danio por un lapso corto.
        hitboxEspada.ActivarVentanaGolpe(duracionVentanaGolpe);

        // Con Mirror, esta parte deberia resolverla el servidor y replicar visual:
        // [ClientRpc] RpcReproducirSwingVisual(direccionSwing);

        // Esperamos fin de la ventana para cerrar el estado de ataque.
        yield return new WaitForSeconds(duracionVentanaGolpe);

        // Marcamos fin del ataque.
        ataqueEnCurso = false;
    }

    // Esta corrutina hace una animacion visual simple de giro de espada.
    private IEnumerator CorrutinaGiroVisual(Vector3 direccionSwing)
    {
        // Calculamos signo segun direccion lateral del swing.
        float signo = Vector3.Dot(transform.right, direccionSwing) >= 0f ? 1f : -1f;

        // Guardamos rotacion original para restaurar al final.
        Quaternion rotacionOriginal = pivoteVisualEspada.localRotation;

        // Calculamos la rotacion objetivo de ida.
        Quaternion rotacionIda = rotacionOriginal * Quaternion.Euler(0f, 0f, -anguloMaximoGiroVisual * signo);

        // Definimos duracion de ida.
        float duracionIda = 0.07f;

        // Iniciamos cronometro de ida.
        float tiempoIda = 0f;

        // Animamos ida.
        while (tiempoIda < duracionIda)
        {
            // Sumamos tiempo de frame.
            tiempoIda += Time.deltaTime;

            // Calculamos progreso normalizado.
            float progreso = Mathf.Clamp01(tiempoIda / duracionIda);

            // Interpolamos rotacion hacia la ida.
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionOriginal, rotacionIda, progreso);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Definimos duracion de vuelta.
        float duracionVuelta = 0.12f;

        // Iniciamos cronometro de vuelta.
        float tiempoVuelta = 0f;

        // Animamos vuelta a la posicion original.
        while (tiempoVuelta < duracionVuelta)
        {
            // Sumamos tiempo de frame.
            tiempoVuelta += Time.deltaTime;

            // Calculamos progreso normalizado.
            float progreso = Mathf.Clamp01(tiempoVuelta / duracionVuelta);

            // Interpolamos rotacion hacia el estado original.
            pivoteVisualEspada.localRotation = Quaternion.Slerp(rotacionIda, rotacionOriginal, progreso);

            // Esperamos al siguiente frame.
            yield return null;
        }

        // Aseguramos rotacion final exacta original.
        pivoteVisualEspada.localRotation = rotacionOriginal;
    }
}

