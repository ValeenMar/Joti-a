using System;
using UnityEngine;

// Esta clase controla la experiencia, el nivel y la subida de estadisticas del jugador.
public class SistemaXP : MonoBehaviour
{
    // Este evento avisa a la UI cuando cambian los valores de experiencia o nivel.
    public event Action AlExperienciaActualizada;

    // Este valor guarda el nivel actual del jugador.
    [SerializeField] private int nivelActual = 1;

    // Este valor guarda la experiencia acumulada dentro del nivel actual.
    [SerializeField] private int experienciaActualNivel = 0;

    // Este valor define cuanta experiencia pide el nivel 1 para subir al nivel 2.
    [SerializeField] private int experienciaBaseParaSubir = 100;

    // Este valor define cuanto escala la experiencia requerida por cada nivel nuevo.
    [SerializeField] private float multiplicadorExperienciaPorNivel = 1.25f;

    // Esta referencia apunta al objeto jugador dueno de este sistema.
    [SerializeField] private GameObject jugadorPropietario;

    // Esta referencia conecta con las estadisticas para escalar danio y velocidad al subir de nivel.
    private EstadisticasJugador estadisticasJugador;

    // Esta propiedad expone el nivel actual para otros scripts.
    public int NivelActual => nivelActual;

    // Esta propiedad expone la experiencia actual del nivel para otros scripts.
    public int ExperienciaActualNivel => experienciaActualNivel;

    // Esta propiedad calcula la experiencia requerida para el nivel actual.
    public int ExperienciaRequeridaNivelActual => CalcularExperienciaRequerida(nivelActual);

    // Esta funcion se ejecuta cuando Unity crea el objeto.
    private void Awake()
    {
        // Si no se asigno un jugador propietario, usamos este mismo objeto por defecto.
        if (jugadorPropietario == null)
        {
            // Guardamos la referencia al mismo GameObject para evitar nulls.
            jugadorPropietario = gameObject;
        }

        // Buscamos el componente de estadisticas para aplicar escalado al subir nivel.
        estadisticasJugador = jugadorPropietario.GetComponent<EstadisticasJugador>();

        // Si encontramos estadisticas, sincronizamos su nivel con el nivel actual del sistema.
        if (estadisticasJugador != null)
        {
            // Aplicamos el nivel para que velocidad y danio arranquen coherentes.
            estadisticasJugador.EstablecerNivel(nivelActual);
        }
    }

    // Esta funcion se ejecuta al activar el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento de enemigo eliminado para sumar experiencia por kill.
        EventosJuego.AlEnemigoEliminado += ManejarEnemigoEliminado;
    }

    // Esta funcion se ejecuta al desactivar el componente.
    private void OnDisable()
    {
        // Nos desuscribimos para evitar errores por referencias colgadas.
        EventosJuego.AlEnemigoEliminado -= ManejarEnemigoEliminado;
    }

    // Este metodo recibe el evento cuando se elimina un enemigo.
    private void ManejarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        // En una version con Mirror, esta validacion la haria el servidor antes de sumar XP.
        if (atacante != jugadorPropietario)
        {
            // Si el atacante no es este jugador, no sumamos experiencia.
            return;
        }

        // Sumamos la experiencia otorgada por el enemigo eliminado.
        AgregarExperiencia(experienciaOtorgada);
    }

    // Este metodo agrega experiencia y procesa subidas de nivel si corresponde.
    public void AgregarExperiencia(int cantidadExperiencia)
    {
        // Si llega una cantidad invalida, no hacemos nada.
        if (cantidadExperiencia <= 0)
        {
            // Salimos para mantener los datos limpios.
            return;
        }

        // Sumamos la experiencia recibida al acumulado del nivel actual.
        experienciaActualNivel += cantidadExperiencia;

        // Mientras tengamos experiencia suficiente, subimos nivel en cadena.
        while (experienciaActualNivel >= ExperienciaRequeridaNivelActual)
        {
            // Restamos el costo del nivel actual para dejar solo el sobrante.
            experienciaActualNivel -= ExperienciaRequeridaNivelActual;

            // Ejecutamos la subida de nivel.
            SubirNivel();
        }

        // Avisamos a la UI que hubo cambios.
        AlExperienciaActualizada?.Invoke();
    }

    // Este metodo permite sumar experiencia desde una orbe sin duplicar logica.
    public void RecogerExperienciaDesdeOrbe(int cantidadExperiencia)
    {
        // Reutilizamos el mismo flujo central de experiencia.
        AgregarExperiencia(cantidadExperiencia);
    }

    // Este metodo realiza una subida de nivel y notifica al resto del juego.
    private void SubirNivel()
    {
        // Guardamos el nivel anterior para enviarlo en el evento.
        int nivelAnterior = nivelActual;

        // Aumentamos el nivel en uno.
        nivelActual++;

        // Si existe estadisticas, actualizamos el nivel para escalar velocidad y danio.
        if (estadisticasJugador != null)
        {
            // Aplicamos el nivel nuevo al componente de estadisticas.
            estadisticasJugador.EstablecerNivel(nivelActual);
        }

        // Notificamos al bus de eventos la subida de nivel de este jugador.
        EventosJuego.NotificarJugadorSubioNivel(jugadorPropietario, nivelAnterior, nivelActual);

        // Avisamos a la UI que hubo cambios.
        AlExperienciaActualizada?.Invoke();
    }

    // Este metodo calcula cuanta experiencia pide un nivel dado.
    private int CalcularExperienciaRequerida(int nivel)
    {
        // Nos aseguramos de no calcular con niveles invalidos.
        int nivelSeguro = Mathf.Max(1, nivel);

        // Calculamos el valor escalado por potencia.
        float valorEscalado = experienciaBaseParaSubir * Mathf.Pow(multiplicadorExperienciaPorNivel, nivelSeguro - 1);

        // Redondeamos y forzamos un minimo de 1 para evitar divisiones raras en UI.
        return Mathf.Max(1, Mathf.RoundToInt(valorEscalado));
    }
}
