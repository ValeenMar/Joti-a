using UnityEngine;

namespace RealmBrawl
{
    /// <summary>
    /// Controla el estado del jugador (Normal, Atacando, Rodando, etc.).
    /// Singleton. Agregarlo al mismo GameObject que GameManager.
    /// </summary>
    public class EstadoJugadorController : MonoBehaviour
    {
        public static EstadoJugadorController Instancia { get; private set; }

        public EstadoJugador Estado { get; private set; } = EstadoJugador.Normal;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        void OnEnable()
        {
            Eventos.AlMorirJugador += OnMorirJugador;
        }

        void OnDisable()
        {
            Eventos.AlMorirJugador -= OnMorirJugador;
        }

        void OnDestroy()
        {
            if (Instancia == this)
                Instancia = null;
        }

        public void CambiarEstado(EstadoJugador nuevo)
        {
            if (Estado == nuevo) return;
            Estado = nuevo;
            Eventos.AlCambiarEstadoJugador?.Invoke(nuevo);
        }

        /// <summary>True si el jugador puede iniciar o encadenar un ataque.</summary>
        public bool PuedeAtacar() => Estado == EstadoJugador.Normal || Estado == EstadoJugador.Atacando;

        /// <summary>True si el jugador puede activar el dodge roll.</summary>
        public bool PuedeDodge() => Estado == EstadoJugador.Normal || Estado == EstadoJugador.Atacando;

        /// <summary>True si el jugador puede recibir input de movimiento.</summary>
        public bool PuedeMoverse() => Estado != EstadoJugador.Rodando && Estado != EstadoJugador.Muerto;

        void OnMorirJugador()
        {
            CambiarEstado(EstadoJugador.Muerto);
        }
    }
}
