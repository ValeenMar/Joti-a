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
    Parry,
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

    [Header("Fuentes")]
    [SerializeField] private AudioSource fuenteSfx;
    [SerializeField] private AudioSource fuenteMusica;

    [Header("Clips SFX")]
    [SerializeField] private AudioClip clipGolpeNormal;
    [SerializeField] private AudioClip clipGolpeCritico;
    [SerializeField] private AudioClip clipParry;
    [SerializeField] private AudioClip clipMuerteEnemigo;
    [SerializeField] private AudioClip clipSubirNivel;
    [SerializeField] private AudioClip clipPocion;
    [SerializeField] private AudioClip clipGameOver;

    [Header("Musica")]
    [SerializeField] private AudioClip clipMusicaFondo;

    // Esta funcion se ejecuta al despertar el objeto.
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        AsegurarFuentesAudio();
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
        AsegurarFuentesAudio();

        switch (tipo)
        {
            case TipoGolpe.Critico:
                ReproducirClipSfx(clipGolpeCritico);
                break;
            case TipoGolpe.Parry:
                ReproducirClipSfx(clipParry);
                break;
            default:
                ReproducirClipSfx(clipGolpeNormal);
                break;
        }
    }

    // Esta funcion queda lista para reproducir la muerte de un enemigo.
    public void ReproducirMuerteEnemigo()
    {
        AsegurarFuentesAudio();
        ReproducirClipSfx(clipMuerteEnemigo);
    }

    // Esta funcion queda lista para reproducir una subida de nivel.
    public void ReproducirSubirNivel()
    {
        AsegurarFuentesAudio();
        ReproducirClipSfx(clipSubirNivel);
    }

    // Esta funcion queda lista para reproducir el uso de una pocion.
    public void ReproducirPocion()
    {
        AsegurarFuentesAudio();
        ReproducirClipSfx(clipPocion);
    }

    // Esta funcion queda lista para reproducir game over.
    public void ReproducirGameOver()
    {
        AsegurarFuentesAudio();
        ReproducirClipSfx(clipGameOver);
    }

    // Esta funcion queda lista para arrancar la musica de fondo.
    public void ReproducirMusicaFondo()
    {
        AsegurarFuentesAudio();

        if (fuenteMusica == null || clipMusicaFondo == null)
        {
            return;
        }

        if (fuenteMusica.isPlaying && fuenteMusica.clip == clipMusicaFondo)
        {
            return;
        }

        fuenteMusica.clip = clipMusicaFondo;
        fuenteMusica.loop = true;
        fuenteMusica.Play();
    }

    // Este metodo asegura que existan dos fuentes separadas para musica y efectos.
    private void AsegurarFuentesAudio()
    {
        if (fuenteSfx == null)
        {
            fuenteSfx = ObtenerOCrearFuente("AudioSfx", false, 1f);
        }

        if (fuenteMusica == null)
        {
            fuenteMusica = ObtenerOCrearFuente("AudioMusica", true, 0.6f);
        }
    }

    // Este metodo obtiene o crea una fuente hija con configuracion base.
    private AudioSource ObtenerOCrearFuente(string nombre, bool loop, float volumen)
    {
        Transform hija = transform.Find(nombre);
        AudioSource fuente = hija != null ? hija.GetComponent<AudioSource>() : null;
        if (fuente == null)
        {
            GameObject objetoFuente = hija != null ? hija.gameObject : new GameObject(nombre);
            if (objetoFuente.transform.parent != transform)
            {
                objetoFuente.transform.SetParent(transform, false);
            }

            fuente = objetoFuente.GetComponent<AudioSource>();
            if (fuente == null)
            {
                fuente = objetoFuente.AddComponent<AudioSource>();
            }
        }

        fuente.playOnAwake = false;
        fuente.loop = loop;
        fuente.spatialBlend = 0f;
        fuente.volume = volumen;
        return fuente;
    }

    // Este metodo reproduce un clip one-shot si ya hay audio asignado.
    private void ReproducirClipSfx(AudioClip clip)
    {
        if (fuenteSfx == null || clip == null)
        {
            return;
        }

        fuenteSfx.PlayOneShot(clip);
    }
}
