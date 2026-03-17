using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase controla el collider de golpe de la espada.
[RequireComponent(typeof(Collider))]
public class HitboxEspada : MonoBehaviour
{
    // Esta referencia guarda el collider usado como hitbox.
    [SerializeField] private Collider colliderGolpe;

    // Esta capa opcional limita a que objetos se les puede aplicar danio.
    [SerializeField] private LayerMask mascaraObjetivos = ~0;

    // Esta variable indica si la ventana de golpe esta activa.
    private bool ventanaGolpeActiva;

    // Esta variable guarda quien esta atacando en este swing.
    private GameObject atacanteActual;

    // Esta variable guarda el danio base del swing actual.
    private float danioBaseActual;

    // Esta variable guarda la direccion del swing actual.
    private Vector3 direccionSwingActual;

    // Esta lista guarda IDs para no golpear dos veces al mismo objetivo en un swing.
    private readonly HashSet<int> objetivosYaGolpeados = new HashSet<int>();

    // Esta referencia guarda la corrutina de ventana activa.
    private Coroutine corrutinaVentanaActiva;

    // Esta funcion se ejecuta una vez al activar el objeto.
    private void Awake()
    {
        // Si no se asigno collider manualmente, usamos el de este objeto.
        if (colliderGolpe == null)
        {
            colliderGolpe = GetComponent<Collider>();
        }

        // Forzamos a trigger para que el hitbox no empuje fisicamente.
        colliderGolpe.isTrigger = true;

        // Al iniciar, la hitbox queda apagada para no daniar siempre.
        colliderGolpe.enabled = false;
    }

    // Esta funcion prepara datos del swing antes de abrir la ventana de golpe.
    public void PrepararGolpe(GameObject atacante, float danioBase, Vector3 direccionSwing)
    {
        // Guardamos quien ataca para armar datos del impacto.
        atacanteActual = atacante;

        // Guardamos el danio base que llego desde SistemaEspada.
        danioBaseActual = danioBase;

        // Guardamos direccion normalizada para efectos y direccion de impacto.
        direccionSwingActual = direccionSwing.sqrMagnitude > 0.001f ? direccionSwing.normalized : Vector3.forward;
    }

    // Esta funcion abre la ventana de golpe por una duracion especifica.
    public void ActivarVentanaGolpe(float duracionVentana)
    {
        // Si ya habia una ventana activa, la cancelamos para empezar limpia.
        if (corrutinaVentanaActiva != null)
        {
            StopCoroutine(corrutinaVentanaActiva);
        }

        // Iniciamos la corrutina nueva de ventana de golpe.
        corrutinaVentanaActiva = StartCoroutine(CorrutinaVentanaGolpe(duracionVentana));
    }

    // Esta corrutina activa el collider por un tiempo corto.
    private IEnumerator CorrutinaVentanaGolpe(float duracionVentana)
    {
        // Limpiamos objetivos golpeados para este swing.
        objetivosYaGolpeados.Clear();

        // Marcamos que el hitbox esta activo.
        ventanaGolpeActiva = true;

        // Encendemos el collider para detectar impactos.
        colliderGolpe.enabled = true;

        // Esperamos el tiempo de ventana de golpe.
        yield return new WaitForSeconds(duracionVentana);

        // Apagamos el collider cuando termina la ventana.
        colliderGolpe.enabled = false;

        // Marcamos que ya no hay ventana activa.
        ventanaGolpeActiva = false;

        // Limpiamos referencia de corrutina.
        corrutinaVentanaActiva = null;
    }

    // Esta funcion se dispara cuando la hitbox entra en contacto con otro collider.
    private void OnTriggerEnter(Collider otroCollider)
    {
        // Si la ventana no esta activa, ignoramos colisiones.
        if (!ventanaGolpeActiva)
        {
            return;
        }

        // Si el atacante no esta definido, no podemos procesar impacto.
        if (atacanteActual == null)
        {
            return;
        }

        // Si la capa del objeto no esta en la mascara permitida, lo ignoramos.
        if ((mascaraObjetivos.value & (1 << otroCollider.gameObject.layer)) == 0)
        {
            return;
        }

        // Evitamos golpear al mismo personaje que porta esta espada.
        if (otroCollider.transform.root == transform.root)
        {
            return;
        }

        // Buscamos un receptor de danio en el collider o en su jerarquia.
        IRecibidorDanio receptorDanio = otroCollider.GetComponentInParent<IRecibidorDanio>();

        // Si no hay receptor, no aplicamos nada.
        if (receptorDanio == null)
        {
            return;
        }

        // Convertimos la interfaz a MonoBehaviour para obtener GameObject.
        MonoBehaviour comportamientoReceptor = receptorDanio as MonoBehaviour;

        // Si no se pudo convertir, evitamos errores.
        if (comportamientoReceptor == null)
        {
            return;
        }

        // Creamos un ID por objetivo para impedir doble golpe en el mismo swing.
        int idObjetivo = comportamientoReceptor.gameObject.GetInstanceID();

        // Si ya golpeamos este objetivo en esta ventana, salimos.
        if (objetivosYaGolpeados.Contains(idObjetivo))
        {
            return;
        }

        // Agregamos el objetivo como ya golpeado.
        objetivosYaGolpeados.Add(idObjetivo);

        // Buscamos configuracion de zona en el collider exacto.
        ZonasDebiles zonaDebil = otroCollider.GetComponent<ZonasDebiles>();

        // Si no esta en el collider exacto, intentamos en la jerarquia.
        if (zonaDebil == null)
        {
            zonaDebil = otroCollider.GetComponentInParent<ZonasDebiles>();
        }

        // Definimos zona y multiplicador por defecto.
        TipoZonaDanio tipoZona = TipoZonaDanio.Cuerpo;
        float multiplicadorZona = 1f;

        // Si encontramos zona configurada, tomamos sus valores.
        if (zonaDebil != null)
        {
            tipoZona = zonaDebil.TipoZona;
            multiplicadorZona = zonaDebil.ObtenerMultiplicador();
        }

        // Armamos datos completos del impacto para vida, UI y feedback.
        DatosDanio datosDanio = new DatosDanio();
        datosDanio.atacante = atacanteActual;
        datosDanio.objetivo = comportamientoReceptor.gameObject;
        datosDanio.danioBase = danioBaseActual;
        datosDanio.multiplicadorZona = multiplicadorZona;
        datosDanio.tipoZona = tipoZona;
        datosDanio.puntoImpacto = otroCollider.ClosestPoint(transform.position);
        datosDanio.direccionImpacto = direccionSwingActual;

        // En single player aplicamos danio directo.
        // Con Mirror, esta parte deberia ejecutarse SOLO en servidor.
        // [Command] El cliente pediria ataque.
        // [ClientRpc] El servidor luego replicaria feedback visual.
        receptorDanio.RecibirDanio(datosDanio);

        // El objeto que recibe el golpe es quien notifica el dano aplicado al resto de sistemas.
        // Esto evita duplicar numeros flotantes cuando el receptor ya emite el evento.

        // Buscamos feedback visual en el objetivo golpeado.
        FeedbackCombate feedbackCombate = otroCollider.GetComponentInParent<FeedbackCombate>();

        // Si existe feedback, mostramos destello segun zona.
        if (feedbackCombate != null)
        {
            feedbackCombate.MostrarDestello(tipoZona);
        }
    }
}
