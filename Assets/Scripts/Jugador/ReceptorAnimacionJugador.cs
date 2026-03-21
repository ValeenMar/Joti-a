using UnityEngine;

namespace RealmBrawl
{
    /// <summary>
    /// Recibe Animation Events del modelo del jugador y los reenvía al sistema de combate.
    /// Este script DEBE estar en el mismo GameObject que el Animator (el hijo "Modelo").
    /// </summary>
    public class ReceptorAnimacionJugador : MonoBehaviour
    {
        CombateCaballero combate;

        void Awake()
        {
            // El CombateCaballero está en el padre (Jugador)
            combate = GetComponentInParent<CombateCaballero>();
            if (combate == null)
                Debug.LogWarning("ReceptorAnimacionJugador: No se encontró CombateCaballero en el padre.");
        }

        // Llamado por Animation Event en las animaciones de ataque normal
        void OnImpactoNormal()
        {
            combate?.EjecutarImpacto(false);
        }

        // Llamado por Animation Event en las animaciones de ataque fuerte
        void OnImpactoFuerte()
        {
            combate?.EjecutarImpacto(true);
        }
    }
}
