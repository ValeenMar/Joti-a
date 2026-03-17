using UnityEngine;

// Esta clase aplica una sacudida suave de camara para reforzar impactos y sensacion de golpe.
[DisallowMultipleComponent]
public class SacudidaCamara : MonoBehaviour
{
    // Esta intensidad base se usa cuando no se pasa una intensidad personalizada.
    [SerializeField] private float intensidadBase = 0.08f;

    // Esta duracion base se usa cuando no se pasa una duracion personalizada.
    [SerializeField] private float duracionBase = 0.15f;

    // Esta frecuencia controla cuan "nerviosa" se siente la vibracion.
    [SerializeField] private float frecuenciaRuido = 28f;

    // Este valor controla que tan rapido se apaga la sacudida.
    [SerializeField] private float velocidadDesvanecimiento = 8f;

    // Este multiplicador ajusta que tanto escala la sacudida segun dano recibido.
    [SerializeField] private float multiplicadorDanioAIntensidad = 0.01f;

    // Esta referencia permite validar si la camara sigue al jugador que recibio dano.
    [SerializeField] private CamaraTercerPersona camaraTercerPersona;

    // Esta variable guarda intensidad actual en tiempo real.
    private float intensidadActual;

    // Esta variable guarda el tiempo restante de sacudida.
    private float tiempoRestante;

    // Estas semillas aleatorias evitan patrones repetitivos en el ruido.
    private float semillaRuidoX;
    private float semillaRuidoY;

    // Esta variable guarda la posicion local inicial de la camara.
    private Vector3 posicionLocalInicial;

    // Esta funcion corre al iniciar y prepara valores base.
    private void Awake()
    {
        // Guardamos la posicion local inicial para poder volver exactamente al origen visual.
        posicionLocalInicial = transform.localPosition;

        // Si no se asigno referencia de camara, intentamos tomarla del mismo objeto.
        if (camaraTercerPersona == null)
        {
            camaraTercerPersona = GetComponent<CamaraTercerPersona>();
        }

        // Si sigue faltando, intentamos encontrarla en el padre para el caso rig padre + camera hija.
        if (camaraTercerPersona == null)
        {
            camaraTercerPersona = GetComponentInParent<CamaraTercerPersona>();
        }

        // Generamos semillas distintas para ruido perlin en ambos ejes.
        semillaRuidoX = Random.Range(0f, 1000f);
        semillaRuidoY = Random.Range(0f, 1000f);
    }

    // Esta funcion se activa al habilitar el componente y se suscribe a eventos globales de juego.
    private void OnEnable()
    {
        // Nos suscribimos al evento de dano al jugador para activar shake automaticamente.
        EventosJuego.AlJugadorRecibioDanio += AlJugadorRecibioDanio;
    }

    // Esta funcion se activa al deshabilitar el componente y limpia suscripciones.
    private void OnDisable()
    {
        // Nos desuscribimos para evitar referencias colgantes y dobles eventos.
        EventosJuego.AlJugadorRecibioDanio -= AlJugadorRecibioDanio;
    }

    // Esta funcion corre al final del frame para aplicar la sacudida sobre la camara ya posicionada.
    private void LateUpdate()
    {
        // Si no queda tiempo ni intensidad, volvemos suave a la posicion inicial y salimos.
        if (tiempoRestante <= 0f && intensidadActual <= 0.0001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posicionLocalInicial, Time.deltaTime * velocidadDesvanecimiento);
            return;
        }

        // Reducimos el tiempo restante de la sacudida usando deltaTime.
        tiempoRestante -= Time.deltaTime;

        // Reducimos gradualmente la intensidad para un final natural.
        intensidadActual = Mathf.Lerp(intensidadActual, 0f, Time.deltaTime * velocidadDesvanecimiento);

        // Calculamos tiempo para ruido en eje X.
        float tiempoRuidoX = Time.time * frecuenciaRuido + semillaRuidoX;

        // Calculamos tiempo para ruido en eje Y.
        float tiempoRuidoY = Time.time * frecuenciaRuido + semillaRuidoY;

        // Generamos desplazamiento X entre -1 y 1 usando PerlinNoise.
        float ruidoX = (Mathf.PerlinNoise(tiempoRuidoX, 0f) - 0.5f) * 2f;

        // Generamos desplazamiento Y entre -1 y 1 usando PerlinNoise.
        float ruidoY = (Mathf.PerlinNoise(0f, tiempoRuidoY) - 0.5f) * 2f;

        // Armamos un offset local de sacudida leve en X/Y sin tocar Z para no marear.
        Vector3 offsetSacudida = new Vector3(ruidoX, ruidoY, 0f) * intensidadActual;

        // Aplicamos la posicion final combinando base + offset.
        transform.localPosition = posicionLocalInicial + offsetSacudida;
    }

    // Este metodo publico permite disparar sacudida desde cualquier otro script.
    public void AplicarSacudida(float intensidadPersonalizada, float duracionPersonalizada)
    {
        // Si la intensidad recibida es menor o igual a cero, usamos la intensidad base.
        float intensidadFinal = intensidadPersonalizada > 0f ? intensidadPersonalizada : intensidadBase;

        // Si la duracion recibida es menor o igual a cero, usamos la duracion base.
        float duracionFinal = duracionPersonalizada > 0f ? duracionPersonalizada : duracionBase;

        // Conservamos la intensidad mayor para que impactos fuertes no sean pisados por debiles.
        intensidadActual = Mathf.Max(intensidadActual, intensidadFinal);

        // Conservamos la mayor duracion restante para extender sacudidas cuando corresponde.
        tiempoRestante = Mathf.Max(tiempoRestante, duracionFinal);
    }

    // Este metodo sobrecargado permite disparar sacudida usando valores base.
    public void AplicarSacudida()
    {
        // Llamamos al metodo principal usando configuraciones base.
        AplicarSacudida(intensidadBase, duracionBase);
    }

    // Este metodo escucha el evento cuando un jugador recibe dano.
    private void AlJugadorRecibioDanio(GameObject jugadorDanado, float cantidadDanio)
    {
        // Si no hay jugador reportado, salimos para evitar errores.
        if (jugadorDanado == null)
        {
            return;
        }

        // Si tenemos camara de tercera persona y no seguimos a ese jugador, ignoramos el evento.
        if (camaraTercerPersona != null && !camaraTercerPersona.EstaSiguiendoJugador(jugadorDanado))
        {
            return;
        }

        // Convertimos dano en intensidad con limites para mantener efecto agradable.
        float intensidadDesdeDanio = Mathf.Clamp(cantidadDanio * multiplicadorDanioAIntensidad, 0.03f, 0.22f);

        // Disparamos una sacudida corta y controlada para feedback de golpe recibido.
        AplicarSacudida(intensidadDesdeDanio, duracionBase);

        // NOTA MIRROR: en multijugador final, este efecto visual deberia activarse solo en cliente local.
        // Flujo recomendado futuro:
        // 1) Servidor valida dano real [Command + logica servidor].
        // 2) Servidor notifica al cliente afectado [TargetRpc/ClientRpc].
        // 3) Cliente afectado llama a AplicarSacudida() localmente.
    }
}
