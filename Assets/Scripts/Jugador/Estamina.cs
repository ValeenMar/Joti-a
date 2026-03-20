using UnityEngine;

namespace RealmBrawl
{
    public class Estamina : MonoBehaviour
    {
        [Header("Valores")]
        [SerializeField] float estaminaMaxima = 100f;
        [SerializeField] float regeneracionPorSegundo = 25f;
        [SerializeField] float delayRegeneracion = 1.5f;

        [Header("Costos")]
        [SerializeField] float costoSprintPorSegundo = 20f;
        [SerializeField] float costoGolpeFuerte = 30f;

        [Header("Agotamiento")]
        [SerializeField] float umbralRecuperacion = 30f;

        float estaminaActual;
        float timerDelay;
        bool agotado;

        public bool EstaAgotado => agotado;
        public bool TieneEstamina => estaminaActual > 0f;
        public float Porcentaje => estaminaActual / estaminaMaxima;

        void Awake()
        {
            estaminaActual = estaminaMaxima;
        }

        void Update()
        {
            if (timerDelay > 0f)
            {
                timerDelay -= Time.deltaTime;
                return;
            }

            if (estaminaActual < estaminaMaxima)
            {
                estaminaActual = Mathf.Min(estaminaActual + regeneracionPorSegundo * Time.deltaTime, estaminaMaxima);

                if (agotado && estaminaActual >= umbralRecuperacion)
                    agotado = false;

                Eventos.AlCambiarEstamina?.Invoke(estaminaActual, estaminaMaxima);
            }
        }

        public void ConsumirSprint(float deltaTime)
        {
            Consumir(costoSprintPorSegundo * deltaTime);
        }

        public bool PuedeGolpeFuerte()
        {
            return !agotado && estaminaActual >= costoGolpeFuerte;
        }

        public void ConsumirGolpeFuerte()
        {
            Consumir(costoGolpeFuerte);
        }

        void Consumir(float cantidad)
        {
            estaminaActual = Mathf.Max(0f, estaminaActual - cantidad);
            timerDelay = delayRegeneracion;

            if (estaminaActual <= 0f)
                agotado = true;

            Eventos.AlCambiarEstamina?.Invoke(estaminaActual, estaminaMaxima);
        }

        public void Restaurar()
        {
            estaminaActual = estaminaMaxima;
            agotado = false;
            Eventos.AlCambiarEstamina?.Invoke(estaminaActual, estaminaMaxima);
        }
    }
}
