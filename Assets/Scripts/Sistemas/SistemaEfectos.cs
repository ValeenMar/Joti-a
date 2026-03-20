using System.Collections;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script centraliza efectos visuales de combate, muerte, pociones y subida de nivel.

// Esta clase spawnea efectos visuales reutilizables y reacciona a eventos globales del juego.
public class SistemaEfectos : MonoBehaviour
{
    // Esta referencia publica permite acceso simple a la instancia actual.
    public static SistemaEfectos Instancia { get; private set; }

    // Este prefab se usa para impactos normales.
    [Header("Prefabs")]
    [SerializeField] private GameObject prefabGolpeNormal;

    // Este prefab se usa para golpes criticos o zonas debiles fuertes.
    [SerializeField] private GameObject prefabGolpeCritico;

    // Este prefab se usa para la muerte del enemigo.
    [SerializeField] private GameObject prefabMuerteEsqueleto;

    // Este prefab se usa para pociones o curacion.
    [SerializeField] private GameObject prefabPocion;

    // Este prefab se usa para subida de nivel.
    [SerializeField] private GameObject prefabSubirNivel;

    // Este contenedor opcional organiza los efectos en la jerarquia.
    [SerializeField] private Transform contenedorEfectos;

    // Este valor define cuanto tardamos en limpiar un efecto estandar.
    [SerializeField] private float tiempoAutoDestruccionGeneral = 1.5f;

    // Esta referencia guarda la rutina de limpieza para no duplicar esperas.
    private Coroutine rutinaLimpiezaActiva;

    // Esta funcion se ejecuta al despertar el objeto.
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        AsegurarReferenciasDesdeEditor();

        if (contenedorEfectos == null)
        {
            contenedorEfectos = transform;
        }
    }

    // Esta funcion se ejecuta al destruir el objeto.
    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    // Esta funcion se ejecuta al habilitar el objeto y conecta eventos globales.
    private void OnEnable()
    {
        EventosJuego.AlAplicarDanio += ManejarDanioAplicado;
        EventosJuego.AlEnemigoEliminado += ManejarEnemigoEliminado;
        EventosJuego.AlJugadorSubioNivel += ManejarJugadorSubioNivel;
        SistemaPociones.AlPocionUsada += ManejarPocionUsada;
    }

    // Esta funcion se ejecuta al deshabilitar el objeto y limpia eventos.
    private void OnDisable()
    {
        EventosJuego.AlAplicarDanio -= ManejarDanioAplicado;
        EventosJuego.AlEnemigoEliminado -= ManejarEnemigoEliminado;
        EventosJuego.AlJugadorSubioNivel -= ManejarJugadorSubioNivel;
        SistemaPociones.AlPocionUsada -= ManejarPocionUsada;
    }

    // Esta funcion intenta completar referencias desde assets conocidos mientras trabajamos en el editor.
    private void AsegurarReferenciasDesdeEditor()
    {
#if UNITY_EDITOR
        if (prefabGolpeNormal == null)
        {
            prefabGolpeNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Efectos/EfectoGolpeNormal.prefab");
        }

        if (prefabGolpeCritico == null)
        {
            prefabGolpeCritico = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Efectos/EfectoGolpeCritico.prefab");
        }

        if (prefabMuerteEsqueleto == null)
        {
            prefabMuerteEsqueleto = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Efectos/EfectoMuerteEsqueleto.prefab");
        }

        if (prefabPocion == null)
        {
            prefabPocion = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Efectos/EfectoPocion.prefab");
        }

        if (prefabSubirNivel == null)
        {
            prefabSubirNivel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Efectos/EfectoSubirNivel.prefab");
        }
#endif
    }

    // Este metodo responde a cualquier dano aplicado para disparar el efecto visual correcto.
    private void ManejarDanioAplicado(DatosDanio datosDanio)
    {
        if (datosDanio == null)
        {
            return;
        }

        Vector3 posicionImpacto = datosDanio.puntoImpacto;
        Quaternion rotacion = Quaternion.LookRotation(datosDanio.direccionImpacto.sqrMagnitude > 0.001f ? datosDanio.direccionImpacto.normalized : Vector3.forward, Vector3.up);

        if (datosDanio.FueCritico)
        {
            DispararEfectoGolpeCritico(posicionImpacto, rotacion);
            return;
        }

        DispararEfectoGolpeNormal(posicionImpacto, rotacion);
    }

    // Este metodo responde cuando muere un enemigo para disparar el efecto de muerte.
    private void ManejarEnemigoEliminado(GameObject atacante, GameObject enemigo, int experienciaOtorgada)
    {
        if (enemigo == null)
        {
            return;
        }

        DispararEfectoMuerteEsqueleto(enemigo.transform.position, enemigo.transform.rotation);
        SistemaAudio.Instancia?.ReproducirMuerteEnemigo();
    }

    // Este metodo responde cuando el jugador sube de nivel.
    private void ManejarJugadorSubioNivel(GameObject jugador, int nivelAnterior, int nivelNuevo)
    {
        if (jugador == null)
        {
            return;
        }

        DispararEfectoSubirNivel(jugador.transform.position + Vector3.up * 1f, Quaternion.identity);
        SistemaAudio.Instancia?.ReproducirSubirNivel();
    }

    // Este metodo responde a pociones usadas.
    private void ManejarPocionUsada(GameObject jugador, Vector3 posicion)
    {
        if (jugador == null)
        {
            return;
        }

        DispararEfectoPocion(posicion + Vector3.up * 1f, Quaternion.identity);
        SistemaAudio.Instancia?.ReproducirPocion();
    }

    // Este metodo dispara el efecto de golpe normal.
    public void DispararEfectoGolpeNormal(Vector3 posicion, Quaternion rotacion)
    {
        InstanciarEfecto(prefabGolpeNormal, posicion, rotacion, tiempoAutoDestruccionGeneral);
        SistemaAudio.Instancia?.ReproducirGolpe(TipoGolpe.Normal);
    }

    // Este metodo dispara el efecto de golpe critico.
    public void DispararEfectoGolpeCritico(Vector3 posicion, Quaternion rotacion)
    {
        InstanciarEfecto(prefabGolpeCritico, posicion, rotacion, tiempoAutoDestruccionGeneral);
        SistemaAudio.Instancia?.ReproducirGolpe(TipoGolpe.Critico);
    }

    // Este metodo dispara el efecto de muerte de enemigo.
    public void DispararEfectoMuerteEsqueleto(Vector3 posicion, Quaternion rotacion)
    {
        InstanciarEfecto(prefabMuerteEsqueleto, posicion, rotacion, tiempoAutoDestruccionGeneral);
    }

    // Este metodo dispara el efecto de pocion.
    public void DispararEfectoPocion(Vector3 posicion, Quaternion rotacion)
    {
        InstanciarEfecto(prefabPocion, posicion, rotacion, tiempoAutoDestruccionGeneral);
    }

    // Este metodo dispara el efecto de subir nivel.
    public void DispararEfectoSubirNivel(Vector3 posicion, Quaternion rotacion)
    {
        InstanciarEfecto(prefabSubirNivel, posicion, rotacion, tiempoAutoDestruccionGeneral);
    }

    // Este metodo instancia un prefab de efecto y programa su limpieza.
    private void InstanciarEfecto(GameObject prefab, Vector3 posicion, Quaternion rotacion, float tiempoVida)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject objetoEfecto = Instantiate(prefab, posicion, rotacion, contenedorEfectos);

        // Dejamos el objeto temporalmente inactivo para que ningun ParticleSystem empiece a correr antes de reiniciarlo.
        objetoEfecto.SetActive(false);

        // Reiniciamos los sistemas de particulas recien instanciados para que arranquen en un estado limpio.
        ParticleSystem[] sistemasParticulas = objetoEfecto.GetComponentsInChildren<ParticleSystem>(true);
        for (int indiceSistema = 0; indiceSistema < sistemasParticulas.Length; indiceSistema++)
        {
            ParticleSystem sistemaActual = sistemasParticulas[indiceSistema];
            if (sistemaActual == null)
            {
                continue;
            }

            sistemaActual.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sistemaActual.Clear(true);
        }

        // Activamos el objeto una vez que todos los sistemas quedaron limpios y listos.
        objetoEfecto.SetActive(true);

        // Recién ahora reproducimos cada sistema para evitar warnings de Unity por configuraciones tardías.
        for (int indiceSistema = 0; indiceSistema < sistemasParticulas.Length; indiceSistema++)
        {
            ParticleSystem sistemaActual = sistemasParticulas[indiceSistema];
            if (sistemaActual == null)
            {
                continue;
            }

            sistemaActual.Play(true);
        }

        float duracionCalculada = Mathf.Max(tiempoVida, CalcularDuracionEfecto(objetoEfecto));

        if (rutinaLimpiezaActiva != null)
        {
            StopCoroutine(rutinaLimpiezaActiva);
        }

        rutinaLimpiezaActiva = StartCoroutine(RutinaDestruirLuego(objetoEfecto, duracionCalculada));
    }

    // Esta corrutina destruye el efecto luego de su tiempo de vida.
    private IEnumerator RutinaDestruirLuego(GameObject objetoEfecto, float tiempoVida)
    {
        yield return new WaitForSecondsRealtime(tiempoVida);

        if (objetoEfecto != null)
        {
            Destroy(objetoEfecto);
        }

        rutinaLimpiezaActiva = null;
    }

    // Este metodo estima cuanto tarda en terminar un efecto visual.
    private float CalcularDuracionEfecto(GameObject objetoEfecto)
    {
        if (objetoEfecto == null)
        {
            return 0.5f;
        }

        ParticleSystem[] sistemasParticulas = objetoEfecto.GetComponentsInChildren<ParticleSystem>(true);
        if (sistemasParticulas == null || sistemasParticulas.Length == 0)
        {
            return 0.5f;
        }

        float duracionMaxima = 0.5f;
        for (int indice = 0; indice < sistemasParticulas.Length; indice++)
        {
            ParticleSystem sistema = sistemasParticulas[indice];
            if (sistema == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = sistema.main;
            float startLifetime = ObtenerLifetimeMaximo(main.startLifetime);
            duracionMaxima = Mathf.Max(duracionMaxima, main.duration + startLifetime);
        }

        return duracionMaxima + 0.25f;
    }

    // Este metodo extrae el lifetime maximo de una curva.
    private float ObtenerLifetimeMaximo(ParticleSystem.MinMaxCurve curva)
    {
        if (curva.mode == ParticleSystemCurveMode.Constant)
        {
            return curva.constant;
        }

        if (curva.mode == ParticleSystemCurveMode.TwoConstants)
        {
            return Mathf.Max(curva.constantMin, curva.constantMax);
        }

        return Mathf.Max(curva.Evaluate(1f), curva.Evaluate(0f));
    }
}
