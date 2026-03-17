using UnityEngine;

// Esta clase guarda estadísticas base del jugador y cómo escalan con el nivel.
public class EstadisticasJugador : MonoBehaviour
{
    // Esta es la velocidad inicial del jugador en nivel 1.
    [SerializeField] private float velocidadBase = 5f;

    // Esta es la cantidad de daño base del jugador en nivel 1.
    [SerializeField] private float danioBase = 10f;

    // Esta es la bonificación de velocidad que se suma por cada nivel extra.
    [SerializeField] private float bonificacionVelocidadPorNivel = 0.35f;

    // Esta es la bonificación de daño que se suma por cada nivel extra.
    [SerializeField] private float bonificacionDanioPorNivel = 1.5f;

    // Esta variable guarda el nivel actual del jugador.
    [SerializeField] private int nivelActual = 1;

    // Esta propiedad devuelve la velocidad real del jugador según su nivel.
    public float VelocidadActual
    {
        get
        {
            // Calculamos la velocidad final sumando la base más la bonificación por nivel.
            return velocidadBase + bonificacionVelocidadPorNivel * Mathf.Max(0, nivelActual - 1);
        }
    }

    // Esta propiedad devuelve el daño real del jugador según su nivel.
    public float DanioActual
    {
        get
        {
            // Calculamos el daño final sumando la base más la bonificación por nivel.
            return danioBase + bonificacionDanioPorNivel * Mathf.Max(0, nivelActual - 1);
        }
    }

    // Esta propiedad deja leer el nivel actual desde otros scripts.
    public int NivelActual => nivelActual;

    // Este método actualiza el nivel del jugador y evita valores inválidos.
    public void EstablecerNivel(int nuevoNivel)
    {
        // Nos aseguramos de que el nivel mínimo sea 1.
        nivelActual = Mathf.Max(1, nuevoNivel);
    }
}

