using System;
using UnityEngine;

namespace RealmBrawl
{
    /// Bus central de eventos del juego.
    public static class Eventos
    {
        // Combate
        public static Action<DatosDanio> AlAplicarDanio;
        public static Action<GameObject> AlMatarEnemigo;

        // Jugador
        public static Action<float, float> AlCambiarVidaJugador; // (actual, max)
        public static Action<float, float> AlCambiarEstamina;    // (actual, max)
        public static Action AlMorirJugador;
        public static Action<int> AlSubirNivel; // nivel nuevo

        // Racha
        public static Action<int> AlCambiarRacha; // racha actual
        public static Action AlRomperRacha;

        // Oleadas
        public static Action<int> AlIniciarOleada; // numero de oleada
        public static Action AlCompletarOleada;

        // Inventario
        public static Action<int, ItemData> AlCambiarSlot; // (slot, item o null)
        public static Action<int> AlUsarItem; // slot

        // XP
        public static Action<float, float, int> AlCambiarXP; // (actual, requerida, nivel)

        // Combate GoW-style
        public static Action<int> AlCambiarComboIndex;          // índice actual del combo (0,1,2)
        public static Action AlIniciarRoll;                      // cuando empieza el dodge roll
        public static Action AlTerminarRoll;                     // cuando termina el dodge roll
        public static Action<EstadoJugador> AlCambiarEstadoJugador; // cambio de estado del jugador

        public static void LimpiarTodo()
        {
            AlAplicarDanio = null;
            AlMatarEnemigo = null;
            AlCambiarVidaJugador = null;
            AlCambiarEstamina = null;
            AlMorirJugador = null;
            AlSubirNivel = null;
            AlCambiarRacha = null;
            AlRomperRacha = null;
            AlIniciarOleada = null;
            AlCompletarOleada = null;
            AlCambiarSlot = null;
            AlUsarItem = null;
            AlCambiarXP = null;
            AlCambiarComboIndex = null;
            AlIniciarRoll = null;
            AlTerminarRoll = null;
            AlCambiarEstadoJugador = null;
        }
    }
}
