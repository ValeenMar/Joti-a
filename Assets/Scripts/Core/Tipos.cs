using UnityEngine;

namespace RealmBrawl
{
    public struct DatosDanio
    {
        public float cantidad;
        public Vector3 puntoImpacto;
        public GameObject atacante;
        public GameObject objetivo;
        public bool esCritico;
    }

    public interface IRecibidorDanio
    {
        void RecibirDanio(DatosDanio datos);
        bool EstaVivo { get; }
    }

    public enum EstadoJuego
    {
        Jugando,
        GameOver,
        Pausado
    }

    public enum TipoEnemigo
    {
        EsqueletoBasico,
        EsqueletoArquero,
        Boss
    }

    public enum EstadoJugador
    {
        Normal,
        Atacando,      // en animación de golpe, acepta buffer del próximo hit
        GolpeFuerte,   // locked, no acepta input
        Rodando,       // iframes activos, no acepta input
        Muerto
    }
}
