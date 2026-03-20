using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmBrawl
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [Header("Estado")]
        EstadoJuego estado = EstadoJuego.Jugando;

        // Estadisticas de la partida
        int totalKills;
        int rachaActual;
        int mejorRacha;
        int oleadaActual;
        int nivelJugador = 1;
        float tiempoPartida;

        public EstadoJuego Estado => estado;
        public int TotalKills => totalKills;
        public int MejorRacha => mejorRacha;
        public int OleadaActual => oleadaActual;
        public int NivelJugador => nivelJugador;
        public float TiempoPartida => tiempoPartida;

        SistemaOleadas oleadas;
        SistemaXP sistemaXP;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            oleadas = GetComponent<SistemaOleadas>();
            sistemaXP = GetComponent<SistemaXP>();
        }

        void OnEnable()
        {
            Eventos.AlMatarEnemigo += OnEnemigoMuerto;
            Eventos.AlMorirJugador += OnJugadorMuerto;
            Eventos.AlSubirNivel += OnSubirNivel;
            Eventos.AlIniciarOleada += OnIniciarOleada;
            Eventos.AlCambiarRacha += OnCambiarRacha;
        }

        void OnDisable()
        {
            Eventos.AlMatarEnemigo -= OnEnemigoMuerto;
            Eventos.AlMorirJugador -= OnJugadorMuerto;
            Eventos.AlSubirNivel -= OnSubirNivel;
            Eventos.AlIniciarOleada -= OnIniciarOleada;
            Eventos.AlCambiarRacha -= OnCambiarRacha;
        }

        void Start()
        {
            estado = EstadoJuego.Jugando;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;

            // Iniciar oleadas
            if (oleadas != null)
                oleadas.IniciarOleadas();
        }

        void Update()
        {
            if (estado == EstadoJuego.Jugando)
                tiempoPartida += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (estado == EstadoJuego.GameOver)
                    ReiniciarPartida();
            }
        }

        void OnEnemigoMuerto(GameObject enemigo)
        {
            totalKills++;

            // Dar XP
            var enemigoBase = enemigo.GetComponent<EnemigoBase>();
            if (enemigoBase != null && sistemaXP != null)
                sistemaXP.AgregarXP(enemigoBase.XPAlMorir);
        }

        void OnJugadorMuerto()
        {
            estado = EstadoJuego.GameOver;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnSubirNivel(int nivel)
        {
            nivelJugador = nivel;
        }

        void OnIniciarOleada(int oleada)
        {
            oleadaActual = oleada;
        }

        void OnCambiarRacha(int racha)
        {
            rachaActual = racha;
            if (racha > mejorRacha)
                mejorRacha = racha;
        }

        public void ReiniciarPartida()
        {
            Time.timeScale = 1f;
            Eventos.LimpiarTodo();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy()
        {
            if (Instancia == this)
                Instancia = null;
        }
    }
}
