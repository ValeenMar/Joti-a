using System;
using UnityEngine;

// Esta clase centraliza eventos simples para conectar sistemas sin acoplarlos demasiado.
public static class EventosJuego
{
    // Este evento avisa que se aplicó un golpe con datos completos.
    public static event Action<DatosDanio> AlAplicarDanio;

    // Este evento avisa que el jugador recibió daño.
    public static event Action<GameObject, float> AlJugadorRecibioDanio;

    // Este evento avisa que el jugador murió.
    public static event Action<GameObject> AlJugadorMurio;

    // Este evento avisa que un enemigo fue eliminado por un atacante.
    public static event Action<GameObject, GameObject, int> AlEnemigoEliminado;

    // Este evento avisa que el jugador subió de nivel.
    public static event Action<GameObject, int, int> AlJugadorSubioNivel;

    // Este evento avisa que cambió la racha actual del jugador.
    public static event Action<GameObject, int> AlRachaActualizada;

    // Este evento avisa que una racha se perdió.
    public static event Action<GameObject> AlRachaPerdida;

    // Este evento avisa que un jugador acaba de lanzar un ataque.
    public static event Action<GameObject, bool> AlJugadorLanzoAtaque;

    // Este evento avisa que el jugador resolvio un parry exitoso sobre un enemigo.
    public static event Action<GameObject, GameObject> AlParryExitoso;

    // Este evento avisa que el jugador intento un parry pero no conecto a tiempo.
    public static event Action<GameObject> AlParryFallido;

    // Este método dispara el evento de daño aplicado.
    public static void NotificarDanioAplicado(DatosDanio datosDanio)
    {
        // Si hay oyentes suscriptos, les enviamos los datos del golpe.
        AlAplicarDanio?.Invoke(datosDanio);
    }

    // Este método dispara el evento de daño recibido por el jugador.
    public static void NotificarJugadorRecibioDanio(GameObject jugador, float cantidadDanio)
    {
        // Si hay oyentes suscriptos, les enviamos el jugador y la cantidad de daño.
        AlJugadorRecibioDanio?.Invoke(jugador, cantidadDanio);
    }

    // Este método dispara el evento de muerte del jugador.
    public static void NotificarJugadorMurio(GameObject jugador)
    {
        // Si hay oyentes suscriptos, les avisamos que el jugador murió.
        AlJugadorMurio?.Invoke(jugador);
    }

    // Este método dispara el evento de enemigo eliminado.
    public static void NotificarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        // Si hay oyentes suscriptos, les enviamos atacante, enemigo y experiencia.
        AlEnemigoEliminado?.Invoke(atacante, enemigo, experienciaOtorgada);
    }

    // Este método dispara el evento de subida de nivel.
    public static void NotificarJugadorSubioNivel(GameObject jugador, int nivelAnterior, int nivelNuevo)
    {
        // Si hay oyentes suscriptos, les enviamos los datos del nuevo nivel.
        AlJugadorSubioNivel?.Invoke(jugador, nivelAnterior, nivelNuevo);
    }

    // Este método dispara el evento de cambio de racha.
    public static void NotificarRachaActualizada(GameObject jugador, int rachaActual)
    {
        // Si hay oyentes suscriptos, les enviamos el nuevo valor de racha.
        AlRachaActualizada?.Invoke(jugador, rachaActual);
    }

    // Este método dispara el evento de racha perdida.
    public static void NotificarRachaPerdida(GameObject jugador)
    {
        // Si hay oyentes suscriptos, les avisamos que la racha terminó.
        AlRachaPerdida?.Invoke(jugador);
    }

    // Este metodo dispara el evento de ataque lanzado por el jugador.
    public static void NotificarJugadorLanzoAtaque(GameObject jugador, bool esGolpeFuerte)
    {
        // Si hay oyentes suscriptos, les enviamos el jugador y si el golpe era fuerte.
        AlJugadorLanzoAtaque?.Invoke(jugador, esGolpeFuerte);
    }

    // Este metodo dispara el evento de parry exitoso.
    public static void NotificarParryExitoso(GameObject jugador, GameObject enemigo)
    {
        // Si hay oyentes suscriptos, les enviamos quien hizo parry y sobre que enemigo.
        AlParryExitoso?.Invoke(jugador, enemigo);
    }

    // Este metodo dispara el evento de parry fallido.
    public static void NotificarParryFallido(GameObject jugador)
    {
        // Si hay oyentes suscriptos, les avisamos que el jugador fallo el timing.
        AlParryFallido?.Invoke(jugador);
    }
}
