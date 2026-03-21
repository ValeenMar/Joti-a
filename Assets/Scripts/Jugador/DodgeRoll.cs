using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealmBrawl
{
    /// <summary>
    /// Dodge roll estilo GoW: doble tap de WASD activa un roll en esa dirección.
    /// Consume stamina e invoca iframes a través de Eventos.AlIniciarRoll.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DodgeRoll : MonoBehaviour
    {
        [Header("Configuración del Roll")]
        [SerializeField] float duracionRoll = 0.35f;
        [SerializeField] float velocidadRoll = 10f;
        [SerializeField] float cooldownRoll = 0.4f;
        [SerializeField] float ventanaDoubleTap = 0.25f;

        Rigidbody rb;
        Animator animator;
        Estamina estamina;

        // Rastreo de doble tap por tecla
        readonly KeyCode[] teclasDireccion = {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D
        };
        readonly Dictionary<KeyCode, float> ultimoPulsado = new Dictionary<KeyCode, float>();

        bool enCooldown;

        static readonly int hashRoll = Animator.StringToHash("RollTrigger");

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            estamina = GetComponent<Estamina>();

            foreach (var tecla in teclasDireccion)
                ultimoPulsado[tecla] = -99f;
        }

        void Update()
        {
            if (enCooldown) return;
            if (EstadoJugadorController.Instancia == null || !EstadoJugadorController.Instancia.PuedeDodge()) return;

            foreach (var tecla in teclasDireccion)
            {
                if (Input.GetKeyDown(tecla))
                {
                    float tiempoAnterior = ultimoPulsado[tecla];
                    ultimoPulsado[tecla] = Time.time;

                    bool esDoubleTap = (Time.time - tiempoAnterior) <= ventanaDoubleTap;
                    if (esDoubleTap)
                    {
                        TentarRoll();
                        break;
                    }
                }
            }
        }

        void TentarRoll()
        {
            if (estamina != null && !estamina.PuedeDodge()) return;

            // Calcular dirección relativa a cámara (igual que MovimientoJugador)
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;

            Vector3 direccion;
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam != null)
            {
                Vector3 adelante = cam.forward;
                Vector3 derecha = cam.right;
                adelante.y = 0f;
                derecha.y = 0f;
                adelante.Normalize();
                derecha.Normalize();
                direccion = (adelante * v + derecha * h).normalized;
            }
            else
            {
                direccion = new Vector3(h, 0f, v).normalized;
            }

            // Si no hay dirección, roll hacia adelante del jugador
            if (direccion.sqrMagnitude < 0.01f)
                direccion = transform.forward;

            EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.Rodando);
            estamina?.ConsumirRoll();
            Eventos.AlIniciarRoll?.Invoke();

            if (animator != null)
                animator.SetTrigger(hashRoll);

            StartCoroutine(EjecutarRoll(direccion));
        }

        IEnumerator EjecutarRoll(Vector3 direccion)
        {
            float tiempoTranscurrido = 0f;

            // Rotar hacia la dirección del roll
            if (direccion.sqrMagnitude > 0.01f)
                rb.rotation = Quaternion.LookRotation(direccion);

            while (tiempoTranscurrido < duracionRoll)
            {
                rb.MovePosition(rb.position + direccion * velocidadRoll * Time.fixedDeltaTime);
                tiempoTranscurrido += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Eventos.AlTerminarRoll?.Invoke();
            EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.Normal);

            // Cooldown adicional antes de poder hacer otro roll
            enCooldown = true;
            yield return new WaitForSeconds(cooldownRoll);
            enCooldown = false;

            // Limpiar timestamps para evitar doble-tap accidental post-roll
            foreach (var tecla in teclasDireccion)
                ultimoPulsado[tecla] = -99f;
        }
    }
}
