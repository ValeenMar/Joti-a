using UnityEngine;

namespace RealmBrawl
{
    public class SistemaXP : MonoBehaviour
    {
        [SerializeField] float xpBaseNivel = 100f;
        [SerializeField] float factorEscalado = 1.25f;

        float xpActual;
        int nivelActual = 1;

        public int Nivel => nivelActual;
        public float XPActual => xpActual;
        public float XPRequerida => xpBaseNivel * Mathf.Pow(factorEscalado, nivelActual - 1);

        void Start()
        {
            Eventos.AlCambiarXP?.Invoke(xpActual, XPRequerida, nivelActual);
        }

        public void AgregarXP(float cantidad)
        {
            xpActual += cantidad;

            while (xpActual >= XPRequerida)
            {
                xpActual -= XPRequerida;
                nivelActual++;
                Eventos.AlSubirNivel?.Invoke(nivelActual);
            }

            Eventos.AlCambiarXP?.Invoke(xpActual, XPRequerida, nivelActual);
        }
    }
}
