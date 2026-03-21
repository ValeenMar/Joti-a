using System.Collections;
using UnityEngine;

namespace RealmBrawl
{
    /// <summary>
    /// Sistema de combate estilo God of War para el caballero con espada.
    /// Reemplaza a SistemaCombate. Agregar al prefab del jugador y quitar SistemaCombate.
    /// </summary>
    public class CombateCaballero : MonoBehaviour
    {
        [Header("Daño")]
        [SerializeField] float danioBase = 20f;
        [SerializeField] float bonusDanioPorNivel = 2f;
        [SerializeField] float multiplicadorFuerte = 2.2f;

        [Header("Alcance")]
        [SerializeField] float alcanceAtaque = 2.5f;
        [SerializeField] float anguloAtaque = 90f;
        [SerializeField] LayerMask mascaraEnemigos = ~0;

        [Header("Hitbox")]
        [SerializeField] Transform puntoHitbox;

        [Header("Timing")]
        [SerializeField] float duracionGolpe = 0.35f;
        [SerializeField] float duracionGolpeFuerte = 0.6f;
        [SerializeField] float ventanaCombo = 0.5f;

        // Estado del combo
        int comboActual = -1;   // -1 = sin combo activo
        bool inputPendiente;
        bool duranteAtaque;
        float timerVentana;

        int nivelActual = 1;
        Animator animator;
        Estamina estamina;

        // Hashes de triggers del Animator
        static readonly int hash0 = Animator.StringToHash("AtaqueTrigger0");
        static readonly int hash1 = Animator.StringToHash("AtaqueTrigger1");
        static readonly int hash2 = Animator.StringToHash("AtaqueTrigger2");
        static readonly int hashFuerte = Animator.StringToHash("AtaqueFuerteTrigger");
        static readonly int hashRecibirDanio = Animator.StringToHash("RecibirDanio");
        static readonly int hashMorir = Animator.StringToHash("Morir");

        float DanioActual => danioBase + (nivelActual - 1) * bonusDanioPorNivel;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            estamina = GetComponent<Estamina>();
        }

        void OnEnable()
        {
            Eventos.AlSubirNivel += OnSubirNivel;
        }

        void OnDisable()
        {
            Eventos.AlSubirNivel -= OnSubirNivel;
        }

        void OnSubirNivel(int nivel) => nivelActual = nivel;

        void Update()
        {
            // Timer de ventana de combo
            if (timerVentana > 0f)
            {
                timerVentana -= Time.deltaTime;
                if (timerVentana <= 0f && !duranteAtaque)
                    ResetearCombo();
            }

            var estado = EstadoJugadorController.Instancia;

            // Click izquierdo = ataque de combo
            if (Input.GetMouseButtonDown(0))
            {
                if (estado.PuedeAtacar())
                {
                    if (!duranteAtaque)
                        IniciarGolpe();
                    else
                        inputPendiente = true;
                }
            }

            // Click derecho = ataque fuerte
            if (Input.GetMouseButtonDown(1))
            {
                if (estado.PuedeAtacar() && !duranteAtaque)
                {
                    if (estamina != null && estamina.PuedeGolpeFuerte())
                        IniciarGolpeFuerte();
                }
            }
        }

        void IniciarGolpe()
        {
            // Avanzar en la cadena de combo (0 → 1 → 2 → 0)
            comboActual = (comboActual + 1) % 3;
            inputPendiente = false;
            duranteAtaque = true;
            timerVentana = ventanaCombo;

            EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.Atacando);
            Eventos.AlCambiarComboIndex?.Invoke(comboActual);

            // Disparar trigger del Animator según el índice del combo
            if (animator != null)
            {
                int hashActual = comboActual == 0 ? hash0 : comboActual == 1 ? hash1 : hash2;
                animator.SetTrigger(hashActual);
            }

            StartCoroutine(FinGolpe(duracionGolpe));
        }

        IEnumerator FinGolpe(float duracion)
        {
            yield return new WaitForSeconds(duracion);
            duranteAtaque = false;

            bool ventanaViva = timerVentana > 0f;

            if (inputPendiente && ventanaViva)
            {
                // Encadenar el siguiente golpe del combo
                IniciarGolpe();
            }
            else
            {
                // Volver a Normal; el timer de ventana decide cuándo resetear el combo
                EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.Normal);
                if (!ventanaViva)
                    ResetearCombo();
            }
        }

        void IniciarGolpeFuerte()
        {
            ResetearCombo();
            duranteAtaque = true;

            EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.GolpeFuerte);
            estamina.ConsumirGolpeFuerte();

            if (animator != null)
                animator.SetTrigger(hashFuerte);

            StartCoroutine(FinGolpeFuerte());
        }

        IEnumerator FinGolpeFuerte()
        {
            yield return new WaitForSeconds(duracionGolpeFuerte);
            duranteAtaque = false;
            EstadoJugadorController.Instancia.CambiarEstado(EstadoJugador.Normal);
        }

        void ResetearCombo()
        {
            comboActual = -1;
            inputPendiente = false;
            timerVentana = 0f;
        }

        /// <summary>
        /// Llamado por ReceptorAnimacionJugador cuando el Animation Event de impacto se dispara.
        /// Aplica el daño en el frame exacto de la animación.
        /// </summary>
        public void EjecutarImpacto(bool esFuerte)
        {
            float danio = esFuerte ? DanioActual * multiplicadorFuerte : DanioActual;
            float alcance = esFuerte ? alcanceAtaque * 1.2f : alcanceAtaque;
            AplicarDanioEnArea(danio, alcance, esFuerte);
        }

        void AplicarDanioEnArea(float danio, float alcance, bool esCritico)
        {
            Vector3 origen = puntoHitbox != null
                ? puntoHitbox.position
                : transform.position + transform.forward + Vector3.up;

            Collider[] hits = Physics.OverlapSphere(origen, alcance, mascaraEnemigos);

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                Vector3 dirHaciaEnemigo = (col.transform.position - transform.position).normalized;
                float angulo = Vector3.Angle(transform.forward, dirHaciaEnemigo);
                if (angulo > anguloAtaque * 0.5f) continue;

                var receptor = col.GetComponent<IRecibidorDanio>();
                if (receptor == null) receptor = col.GetComponentInParent<IRecibidorDanio>();
                if (receptor == null || !receptor.EstaVivo) continue;

                var datos = new DatosDanio
                {
                    cantidad = danio,
                    puntoImpacto = col.ClosestPoint(origen),
                    atacante = gameObject,
                    objetivo = col.gameObject,
                    esCritico = esCritico
                };

                receptor.RecibirDanio(datos);
                Eventos.AlAplicarDanio?.Invoke(datos);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 origen = puntoHitbox != null
                ? puntoHitbox.position
                : transform.position + transform.forward + Vector3.up;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origen, alcanceAtaque);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origen, alcanceAtaque * 1.2f);
        }
    }
}
