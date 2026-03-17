using UnityEngine;

// Este enum define los posibles estados del enemigo dummy.
public enum EstadosEnemigo
{
    // En este estado el enemigo camina entre puntos predefinidos.
    Patrullar,

    // En este estado el enemigo sigue al jugador si lo detecta.
    Perseguir,

    // En este estado el enemigo realiza su ataque cercano.
    Atacar,

    // En este estado el enemigo ya no actua porque murio.
    Muerto
}

