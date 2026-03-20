using UnityEngine;

namespace RealmBrawl
{
    public class SistemaRachas : MonoBehaviour
    {
        int rachaActual;

        void OnEnable()
        {
            Eventos.AlMatarEnemigo += OnKill;
            Eventos.AlRomperRacha += OnRomper;
            Eventos.AlMorirJugador += OnRomper;
        }

        void OnDisable()
        {
            Eventos.AlMatarEnemigo -= OnKill;
            Eventos.AlRomperRacha -= OnRomper;
            Eventos.AlMorirJugador -= OnRomper;
        }

        void OnKill(GameObject _)
        {
            rachaActual++;
            Eventos.AlCambiarRacha?.Invoke(rachaActual);
        }

        void OnRomper()
        {
            if (rachaActual > 0)
            {
                rachaActual = 0;
                Eventos.AlCambiarRacha?.Invoke(0);
            }
        }
    }
}
