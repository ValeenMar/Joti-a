using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace RealmBrawl
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemigoBase : MonoBehaviour, IRecibidorDanio
    {
        [Header("Stats")]
        [SerializeField] float vidaMaxima = 50f;
        [SerializeField] float danioAtaque = 10f;
        [SerializeField] float cooldownAtaque = 2f;
        [SerializeField] float velocidadMovimiento = 3.5f;

        [Header("Deteccion")]
        [SerializeField] float rangoDeteccion = 8f;
        [SerializeField] float rangoAtaque = 2.5f;
        [SerializeField] float anguloVision = 120f;

        [Header("Tipo")]
        [SerializeField] TipoEnemigo tipo = TipoEnemigo.EsqueletoBasico;

        [Header("XP")]
        [SerializeField] float xpAlMorir = 25f;

        // Estado interno
        float vidaActual;
        NavMeshAgent agente;
        Animator animator;
        Transform jugador;
        EstadoEnemigo estado = EstadoEnemigo.Patrullar;
        float timerAtaque;
        bool muertoProcessado;

        // Delay de activacion al spawnear
        float tiempoHastaActivar = 1.5f;

        // Movimiento directo (fallback cuando no hay NavMesh)
        bool usarMovimientoDirecto = false;

        // Patrulla
        Vector3 puntoPatrullaA;
        Vector3 puntoPatrullaB;
        bool yendoAB = true;
        float timerEspera;

        // Animator hashes
        static readonly int hashVelocidad = Animator.StringToHash("Velocidad");
        static readonly int hashAtacar = Animator.StringToHash("Atacar");
        static readonly int hashDanio = Animator.StringToHash("RecibirDanio");
        static readonly int hashMorir = Animator.StringToHash("Morir");

        public bool EstaVivo => vidaActual > 0f;
        public TipoEnemigo Tipo => tipo;
        public float PorcentajeVida => vidaActual / vidaMaxima;

        // Evento para barra de vida
        public System.Action<float> AlCambiarVida;
        public System.Action AlMorir;

        enum EstadoEnemigo { Patrullar, Perseguir, Atacar, Muerto }

        void Awake()
        {
            agente = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            vidaActual = vidaMaxima;
        }

        void Start()
        {
            agente.speed = velocidadMovimiento;

            // Validar que el agente este en el NavMesh
            if (!agente.isOnNavMesh)
            {
                Debug.LogWarning($"Enemigo {gameObject.name} no está en el NavMesh - usando movimiento directo");
                usarMovimientoDirecto = true;
            }

            // Buscar jugador
            var jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null) jugador = jugadorObj.transform;

            // Definir puntos de patrulla alrededor del spawn
            Vector3 pos = transform.position;
            puntoPatrullaA = pos + Random.insideUnitSphere.normalized * 5f;
            puntoPatrullaA.y = pos.y;
            puntoPatrullaB = pos - Random.insideUnitSphere.normalized * 5f;
            puntoPatrullaB.y = pos.y;

            // Validar que los puntos estan en NavMesh
            if (NavMesh.SamplePosition(puntoPatrullaA, out NavMeshHit hitA, 5f, NavMesh.AllAreas))
                puntoPatrullaA = hitA.position;
            if (NavMesh.SamplePosition(puntoPatrullaB, out NavMeshHit hitB, 5f, NavMesh.AllAreas))
                puntoPatrullaB = hitB.position;
        }

        void Update()
        {
            if (estado == EstadoEnemigo.Muerto) return;

            // Delay de activacion post-spawn: no hacer nada hasta que expire
            if (tiempoHastaActivar > 0f)
            {
                tiempoHastaActivar -= Time.deltaTime;
                return;
            }

            timerAtaque -= Time.deltaTime;

            switch (estado)
            {
                case EstadoEnemigo.Patrullar:
                    ActualizarPatrulla();
                    break;
                case EstadoEnemigo.Perseguir:
                    ActualizarPersecucion();
                    break;
                case EstadoEnemigo.Atacar:
                    ActualizarAtaque();
                    break;
            }

            // Animar velocidad
            if (animator != null && agente.isOnNavMesh)
                animator.SetFloat(hashVelocidad, agente.velocity.magnitude / Mathf.Max(velocidadMovimiento, 0.1f));

            // Chequear si ve al jugador (desde patrulla)
            if (estado == EstadoEnemigo.Patrullar && jugador != null)
            {
                if (PuedeVerJugador())
                    CambiarEstado(EstadoEnemigo.Perseguir);
            }
        }

        void ActualizarPatrulla()
        {
            if (usarMovimientoDirecto) return; // sin NavMesh, no patrullamos
            if (!agente.isOnNavMesh) return;

            if (timerEspera > 0f)
            {
                timerEspera -= Time.deltaTime;
                return;
            }

            Vector3 destino = yendoAB ? puntoPatrullaB : puntoPatrullaA;
            agente.SetDestination(destino);

            if (!agente.pathPending && agente.remainingDistance < 1f)
            {
                yendoAB = !yendoAB;
                timerEspera = Random.Range(1f, 3f);
            }
        }

        void ActualizarPersecucion()
        {
            if (jugador == null) return;

            float dist = Vector3.Distance(transform.position, jugador.position);

            if (dist <= rangoAtaque)
            {
                CambiarEstado(EstadoEnemigo.Atacar);
                return;
            }

            if (dist > rangoDeteccion * 1.5f)
            {
                CambiarEstado(EstadoEnemigo.Patrullar);
                return;
            }

            if (usarMovimientoDirecto)
            {
                Vector3 dir = (jugador.position - transform.position).normalized;
                transform.position += dir * velocidadMovimiento * 0.5f * Time.deltaTime;
                transform.LookAt(jugador);
            }
            else if (agente.isOnNavMesh)
            {
                agente.SetDestination(jugador.position);
            }
        }

        void ActualizarAtaque()
        {
            if (jugador == null) return;

            float dist = Vector3.Distance(transform.position, jugador.position);

            // Mirar al jugador
            Vector3 dir = (jugador.position - transform.position).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

            if (dist > rangoAtaque * 1.3f)
            {
                CambiarEstado(EstadoEnemigo.Perseguir);
                return;
            }

            // Parar al atacar
            if (!usarMovimientoDirecto && agente.isOnNavMesh)
                agente.SetDestination(transform.position);

            if (timerAtaque <= 0f)
            {
                EjecutarAtaque();
                timerAtaque = cooldownAtaque;
            }
        }

        void EjecutarAtaque()
        {
            if (animator != null) animator.SetTrigger(hashAtacar);
            // El daño se aplica con delay para coincidir con la animacion
            StartCoroutine(AplicarDanioConDelay(0.5f));
        }

        IEnumerator AplicarDanioConDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!EstaVivo || jugador == null) yield break;

            float dist = Vector3.Distance(transform.position, jugador.position);
            if (dist > rangoAtaque * 1.2f) yield break;

            var vidaJugador = jugador.GetComponent<VidaJugador>();
            if (vidaJugador != null && vidaJugador.EstaVivo)
            {
                var datos = new DatosDanio
                {
                    cantidad = danioAtaque,
                    puntoImpacto = jugador.position + Vector3.up,
                    atacante = gameObject,
                    objetivo = jugador.gameObject,
                    esCritico = false
                };
                vidaJugador.RecibirDanio(datos);
            }
        }

        bool PuedeVerJugador()
        {
            if (jugador == null) return false;

            float dist = Vector3.Distance(transform.position, jugador.position);
            if (dist > rangoDeteccion) return false;

            Vector3 dir = (jugador.position - transform.position).normalized;
            float angulo = Vector3.Angle(transform.forward, dir);
            if (angulo > anguloVision * 0.5f) return false;

            return true;
        }

        void CambiarEstado(EstadoEnemigo nuevoEstado)
        {
            estado = nuevoEstado;
            if (nuevoEstado == EstadoEnemigo.Perseguir && !usarMovimientoDirecto && agente.isOnNavMesh)
                agente.isStopped = false;
        }

        // --- IRecibidorDanio ---

        public void RecibirDanio(DatosDanio datos)
        {
            if (!EstaVivo) return;

            vidaActual = Mathf.Max(0f, vidaActual - datos.cantidad);
            AlCambiarVida?.Invoke(PorcentajeVida);

            if (animator != null && EstaVivo)
                animator.SetTrigger(hashDanio);

            // Agro inmediato al recibir danio
            if (estado == EstadoEnemigo.Patrullar && EstaVivo)
            {
                if (datos.atacante != null)
                {
                    jugador = datos.atacante.transform;
                    CambiarEstado(EstadoEnemigo.Perseguir);
                }
            }

            if (!EstaVivo)
                Morir();
        }

        void Morir()
        {
            if (muertoProcessado) return;
            muertoProcessado = true;

            estado = EstadoEnemigo.Muerto;

            // Detener y desactivar NavMeshAgent
            if (agente.isOnNavMesh)
                agente.isStopped = true;
            agente.enabled = false;

            // Desactivar Rigidbody si existe para evitar que flote
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            if (animator != null)
                animator.SetTrigger(hashMorir);

            // Desactivar colliders para que no bloquee
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            AlMorir?.Invoke();
            Eventos.AlMatarEnemigo?.Invoke(gameObject);

            // Destruir despues de la animacion de muerte
            StartCoroutine(DestruirDespuesDeMuerte());
        }

        IEnumerator DestruirDespuesDeMuerte()
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(gameObject);
        }

        // --- Configuracion desde oleadas ---

        public void ConfigurarComoBasico()
        {
            tipo = TipoEnemigo.EsqueletoBasico;
            vidaMaxima = 50f;
            danioAtaque = 10f;
            velocidadMovimiento = 3.5f;
            xpAlMorir = 25f;
            cooldownAtaque = 2f;
        }

        public void ConfigurarComoBoss(int oleada)
        {
            tipo = TipoEnemigo.Boss;
            float multiplicador = 1f + (oleada / 4) * 0.5f;
            vidaMaxima = 200f * multiplicador;
            danioAtaque = 25f * multiplicador;
            velocidadMovimiento = 2.5f;
            xpAlMorir = 100f * multiplicador;
            cooldownAtaque = 2.5f;
            transform.localScale = Vector3.one * 1.5f;
        }

        public void ConfigurarStats(float vida, float danio, float velocidad, float xp)
        {
            vidaMaxima = vida;
            vidaActual = vida;
            danioAtaque = danio;
            velocidadMovimiento = velocidad;
            xpAlMorir = xp;
            if (agente != null) agente.speed = velocidadMovimiento;
        }

        public float XPAlMorir => xpAlMorir;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        }
    }
}
