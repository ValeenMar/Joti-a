using UnityEngine;

namespace RealmBrawl
{
    public class VidaJugador : MonoBehaviour, IRecibidorDanio
    {
        [SerializeField] float vidaMaxima = 100f;
        [SerializeField] float tiempoInvulnerable = 0.5f;

        float vidaActual;
        float timerInvulnerable;
        Animator animator;

        static readonly int hashMorir = Animator.StringToHash("Morir");
        static readonly int hashDanio = Animator.StringToHash("RecibirDanio");

        public bool EstaVivo => vidaActual > 0f;
        public float Porcentaje => vidaActual / vidaMaxima;
        public float VidaActual => vidaActual;
        public float VidaMaxima => vidaMaxima;

        void Awake()
        {
            vidaActual = vidaMaxima;
            animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            Eventos.AlCambiarVidaJugador?.Invoke(vidaActual, vidaMaxima);
        }

        void Update()
        {
            if (timerInvulnerable > 0f)
                timerInvulnerable -= Time.deltaTime;
        }

        public void RecibirDanio(DatosDanio datos)
        {
            if (!EstaVivo) return;
            if (timerInvulnerable > 0f) return;

            vidaActual = Mathf.Max(0f, vidaActual - datos.cantidad);
            timerInvulnerable = tiempoInvulnerable;

            Eventos.AlCambiarVidaJugador?.Invoke(vidaActual, vidaMaxima);
            Eventos.AlRomperRacha?.Invoke();

            if (animator != null)
                animator.SetTrigger(hashDanio);

            if (vidaActual <= 0f)
                Morir();
        }

        void Morir()
        {
            if (animator != null)
                animator.SetTrigger(hashMorir);

            Eventos.AlMorirJugador?.Invoke();
        }

        public void Curar(float cantidad)
        {
            if (!EstaVivo) return;
            vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
            Eventos.AlCambiarVidaJugador?.Invoke(vidaActual, vidaMaxima);
        }

        public bool VidaLlena()
        {
            return vidaActual >= vidaMaxima;
        }

        public void Reiniciar()
        {
            vidaActual = vidaMaxima;
            timerInvulnerable = 0f;
            Eventos.AlCambiarVidaJugador?.Invoke(vidaActual, vidaMaxima);
        }
    }
}
