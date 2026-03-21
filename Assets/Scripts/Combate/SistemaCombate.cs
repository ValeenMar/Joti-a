using UnityEngine;
using System.Collections;

namespace RealmBrawl
{
    public class SistemaCombate : MonoBehaviour
    {
        [Header("Danio")]
        [SerializeField] float danioBase = 20f;
        [SerializeField] float multiplicadorFuerte = 2f;
        [SerializeField] float bonusDanioPorNivel = 2f;

        [Header("Cooldown")]
        [SerializeField] float cooldownNormal = 0.4f;
        [SerializeField] float cooldownFuerte = 0.8f;

        [Header("Alcance")]
        [SerializeField] float alcanceAtaque = 2.5f;
        [SerializeField] float anguloAtaque = 90f;
        [SerializeField] LayerMask mascaraEnemigos = ~0;

        [Header("Hitbox")]
        [SerializeField] Transform puntoHitbox;

        Animator animator;
        Estamina estamina;
        float timerCooldown;
        int nivelActual = 1;
        bool atacando;
        bool ataqueEsFuerte;

        static readonly int hashAtaqueNormal = Animator.StringToHash("AtaqueNormal");
        static readonly int hashAtaqueFuerte = Animator.StringToHash("AtaqueFuerte");

        public bool EstaAtacando => atacando;

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

        void OnSubirNivel(int nivel)
        {
            nivelActual = nivel;
        }

        void Update()
        {
            if (timerCooldown > 0f)
                timerCooldown -= Time.deltaTime;

            if (timerCooldown > 0f || atacando) return;

            // Click izquierdo = ataque normal
            if (Input.GetMouseButtonDown(0))
                Atacar(false);
            // Click derecho = ataque fuerte (requiere stamina)
            else if (Input.GetMouseButtonDown(1))
                Atacar(true);
        }

        void Atacar(bool fuerte)
        {
            if (fuerte)
            {
                if (estamina != null && !estamina.PuedeGolpeFuerte()) return;
                estamina?.ConsumirGolpeFuerte();
                timerCooldown = cooldownFuerte;
                if (animator != null) animator.SetTrigger(hashAtaqueFuerte);
            }
            else
            {
                timerCooldown = cooldownNormal;
                if (animator != null) animator.SetTrigger(hashAtaqueNormal);
            }

            // Guardar el tipo de ataque; el daño se aplica cuando llega el Animation Event
            ataqueEsFuerte = fuerte;
            atacando = true;

            StartCoroutine(FinAtaque(fuerte ? 0.6f : 0.35f));
        }

        /// <summary>
        /// Llamado por ReceptorAnimacionJugador cuando el Animation Event de impacto se dispara.
        /// </summary>
        public void EjecutarImpacto(bool esFuerte)
        {
            float danio = ataqueEsFuerte ? DanioActual * multiplicadorFuerte : DanioActual;
            AplicarDanioEnArea(danio, ataqueEsFuerte);
        }

        IEnumerator FinAtaque(float duracion)
        {
            yield return new WaitForSeconds(duracion);
            atacando = false;
        }

        void AplicarDanioEnArea(float danio, bool esFuerte)
        {
            Vector3 origen = puntoHitbox != null ? puntoHitbox.position : transform.position + transform.forward + Vector3.up;

            Collider[] hits = Physics.OverlapSphere(origen, alcanceAtaque, mascaraEnemigos);

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                // Verificar angulo
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
                    esCritico = esFuerte
                };

                receptor.RecibirDanio(datos);
                Eventos.AlAplicarDanio?.Invoke(datos);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 origen = puntoHitbox != null ? puntoHitbox.position : transform.position + transform.forward + Vector3.up;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origen, alcanceAtaque);
        }
    }
}
