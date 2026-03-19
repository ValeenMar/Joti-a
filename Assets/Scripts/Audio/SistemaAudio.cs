using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script deja preparado un punto unico para sonido,
// aunque por ahora los metodos quedan vacios como placeholder.

// Este enum define categorias simples de sonido para futuro.
public enum TipoGolpe
{
    Normal,
    Critico,
    Muerte,
    Pocion,
    Nivel,
    GameOver
}

// Esta clase centraliza el audio futuro del proyecto.
public class SistemaAudio : MonoBehaviour
{
    // Esta referencia deja acceso global simple si hace falta.
    public static SistemaAudio Instancia { get; private set; }

    // Esta funcion se ejecuta al despertar el objeto.
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
    }

    // Esta funcion se ejecuta al destruir el objeto.
    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    // Esta funcion queda lista para reproducir un golpe segun el tipo.
    public void ReproducirGolpe(TipoGolpe tipo)
    {
        // COPILOT-EXPAND: aqui se conectarian los AudioClip de golpe normal, critico y variantes.
    }

    // Esta funcion queda lista para reproducir la muerte de un enemigo.
    public void ReproducirMuerteEnemigo()
    {
        // COPILOT-EXPAND: aqui se conectaria un clip de muerte o impacto final.
    }

    // Esta funcion queda lista para reproducir una subida de nivel.
    public void ReproducirSubirNivel()
    {
        // COPILOT-EXPAND: aqui se conectaria un jingle corto de recompensa.
    }

    // Esta funcion queda lista para reproducir el uso de una pocion.
    public void ReproducirPocion()
    {
        // COPILOT-EXPAND: aqui se conectaria un sonido de beber o energia.
    }

    // Esta funcion queda lista para reproducir game over.
    public void ReproducirGameOver()
    {
        // COPILOT-EXPAND: aqui se conectaria un sonido grave o de derrota.
    }

    // Esta funcion queda lista para arrancar la musica de fondo.
    public void ReproducirMusicaFondo()
    {
        // COPILOT-EXPAND: aqui se conectaria el loop de musica ambiental medieval.
    }
}
