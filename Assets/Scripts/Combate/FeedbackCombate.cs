using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Esta clase muestra un destello visual en el objetivo cuando recibe un golpe.
public class FeedbackCombate : MonoBehaviour
{
    // Esta lista permite definir renderizadores manualmente.
    [SerializeField] private Renderer[] renderizadoresObjetivo;

    // Este color se usa para golpes normales.
    [SerializeField] private Color colorGolpeNormal = Color.white;

    // Este color se usa para golpes en espalda.
    [SerializeField] private Color colorGolpeEspalda = new Color(1f, 0.4f, 0.4f, 1f);

    // Este color se usa para golpes en cabeza.
    [SerializeField] private Color colorGolpeCritico = Color.red;

    // Esta variable controla cuanto se mezcla el color de destello.
    [Range(0f, 1f)]
    [SerializeField] private float intensidadDestello = 0.8f;

    // Esta variable define cuanto dura el destello.
    [SerializeField] private float duracionDestello = 0.12f;

    // Esta variable indica si tambien se toca el color de emision.
    [SerializeField] private bool usarEmision = true;

    // Este color se usa como indicador suave cuando el enemigo esta dentro del rango de ataque.
    [SerializeField] private Color colorIndicadorRango = new Color(1f, 0.35f, 0.35f, 1f);

    // Esta variable controla cuanto se mezcla el indicador de rango con el color original.
    [Range(0f, 1f)]
    [SerializeField] private float intensidadIndicadorRango = 0.35f;

    // Esta lista guarda materiales para evitar buscarlos en cada golpe.
    private readonly List<Material> materiales = new List<Material>();

    // Esta lista guarda el color original de cada material.
    private readonly List<Color> coloresOriginales = new List<Color>();

    // Esta referencia guarda la corrutina de destello actual.
    private Coroutine corrutinaDestelloActiva;

    // Esta variable indica si el enemigo esta resaltado por estar en rango de ataque.
    private bool indicadorRangoActivo;

    // Esta bandera indica si el enemigo esta telegrapheando un ataque.
    private bool telegraphAtaqueActivo;

    // Esta bandera indica si el enemigo esta vulnerable tras un parry.
    private bool vulnerableParryActiva;

    // Este color se usa para avisar que el enemigo esta cargando un golpe.
    [SerializeField] private Color colorTelegraphAtaque = new Color(1f, 0.50f, 0.16f, 1f);

    // Esta intensidad mezcla el telegraph con el color base.
    [Range(0f, 1f)]
    [SerializeField] private float intensidadTelegraphAtaque = 0.48f;

    // Este color avisa que el enemigo esta vulnerable despues de un parry.
    [SerializeField] private Color colorVulnerableParry = new Color(0.36f, 0.90f, 1f, 1f);

    // Esta intensidad mezcla la vulnerabilidad con el color base.
    [Range(0f, 1f)]
    [SerializeField] private float intensidadVulnerableParry = 0.42f;

    // Esta opcion crea un disco sencillo en el suelo para leer mejor telegraph y vulnerabilidad.
    [SerializeField] private bool usarIndicadorSuelo = true;

    // Esta altura separa el disco del suelo para evitar z-fighting.
    [SerializeField] private float alturaIndicadorSuelo = 0.04f;

    // Esta velocidad da un pulso corto al telegraph de ataque.
    [SerializeField] private float velocidadPulsoTelegraph = 7f;

    // Esta velocidad da un pulso mas estable al estado vulnerable.
    [SerializeField] private float velocidadPulsoVulnerable = 4f;

    // Esta funcion se ejecuta una vez al activar el objeto.
    private void Awake()
    {
        // Si no se cargaron renderizadores manualmente, tomamos todos los hijos.
        if (renderizadoresObjetivo == null || renderizadoresObjetivo.Length == 0)
        {
            renderizadoresObjetivo = GetComponentsInChildren<Renderer>();
        }

        // Capturamos materiales y sus colores base.
        CapturarMateriales();

        // Preparamos un indicador en el suelo para leer mejor estados de combate.
        AsegurarIndicadorSuelo();
        AplicarColoresBaseSegunEstado();
    }

    // Esta funcion actualiza el indicador de suelo y mantiene su escala ajustada al modelo.
    private void LateUpdate()
    {
        ActualizarIndicadorSuelo();
    }

    // Esta funcion publica la usa el sistema de combate para disparar el destello.
    public void MostrarDestello(TipoZonaDanio tipoZona)
    {
        // Elegimos el color segun la zona del impacto.
        Color colorDestino = ObtenerColorPorZona(tipoZona);

        // Si habia un destello corriendo, lo frenamos para no mezclar animaciones.
        if (corrutinaDestelloActiva != null)
        {
            StopCoroutine(corrutinaDestelloActiva);
        }

        // Lanzamos una nueva animacion de destello.
        corrutinaDestelloActiva = StartCoroutine(CorrutinaDestello(colorDestino));
    }

    // Esta funcion permite encender o apagar el indicador suave de rango.
    public void EstablecerIndicadorRango(bool activo)
    {
        // Si no hubo cambio real, no hace falta recalcular materiales.
        if (indicadorRangoActivo == activo)
        {
            return;
        }

        // Guardamos el nuevo estado del indicador.
        indicadorRangoActivo = activo;

        // Si ahora mismo no hay un destello golpeando el material, actualizamos el color base visible.
        if (corrutinaDestelloActiva == null)
        {
            AplicarColoresBaseSegunEstado();
        }
    }

    // Esta funcion prende o apaga un telegraph visual de ataque.
    public void EstablecerTelegraphAtaque(bool activo)
    {
        if (telegraphAtaqueActivo == activo)
        {
            return;
        }

        telegraphAtaqueActivo = activo;
        if (corrutinaDestelloActiva == null)
        {
            AplicarColoresBaseSegunEstado();
        }
    }

    // Esta funcion prende o apaga el estado visual de vulnerabilidad tras un parry.
    public void EstablecerVulnerableParry(bool activo)
    {
        if (vulnerableParryActiva == activo)
        {
            return;
        }

        vulnerableParryActiva = activo;
        if (corrutinaDestelloActiva == null)
        {
            AplicarColoresBaseSegunEstado();
        }
    }

    // Esta funcion recorre renderizadores y guarda materiales con sus colores base.
    private void CapturarMateriales()
    {
        // Limpiamos listas por seguridad si este metodo se vuelve a llamar.
        materiales.Clear();
        coloresOriginales.Clear();

        // Recorremos cada renderizador.
        for (int indiceRender = 0; indiceRender < renderizadoresObjetivo.Length; indiceRender++)
        {
            // Guardamos referencia al renderizador actual.
            Renderer renderizadorActual = renderizadoresObjetivo[indiceRender];

            // Si el renderizador no existe, pasamos al siguiente.
            if (renderizadorActual == null)
            {
                continue;
            }

            // Tomamos todos los materiales visibles de ese renderizador.
            Material[] materialesRender = renderizadorActual.materials;

            // Recorremos cada material del renderizador.
            for (int indiceMaterial = 0; indiceMaterial < materialesRender.Length; indiceMaterial++)
            {
                // Guardamos referencia al material actual.
                Material materialActual = materialesRender[indiceMaterial];

                // Si el material no existe, lo salteamos.
                if (materialActual == null)
                {
                    continue;
                }

                // Agregamos el material a la lista principal.
                materiales.Add(materialActual);

                // Guardamos su color base para restaurarlo luego.
                coloresOriginales.Add(LeerColorPrincipal(materialActual));
            }
        }
    }

    // Esta funcion crea un disco simple en el suelo para telegraph y estados especiales.
    private void AsegurarIndicadorSuelo()
    {
        if (!usarIndicadorSuelo)
        {
            return;
        }

        Transform existente = transform.Find("IndicadorSueloCombate");
        if (existente != null)
        {
            return;
        }

        GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disco.name = "IndicadorSueloCombate";
        disco.transform.SetParent(transform, false);
        disco.transform.localRotation = Quaternion.identity;
        disco.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);

        Collider colliderDisco = disco.GetComponent<Collider>();
        if (colliderDisco != null)
        {
            if (Application.isPlaying)
            {
                Destroy(colliderDisco);
            }
            else
            {
                DestroyImmediate(colliderDisco);
            }
        }

        MeshRenderer rendererDisco = disco.GetComponent<MeshRenderer>();
        if (rendererDisco != null)
        {
            Material materialDisco = new Material(Shader.Find("Standard"));
            materialDisco.name = "IndicadorSueloCombate_Mat";
            materialDisco.SetFloat("_Mode", 3f);
            materialDisco.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materialDisco.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materialDisco.SetInt("_ZWrite", 0);
            materialDisco.DisableKeyword("_ALPHATEST_ON");
            materialDisco.EnableKeyword("_ALPHABLEND_ON");
            materialDisco.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            materialDisco.renderQueue = 3000;
            materialDisco.SetFloat("_Glossiness", 0f);
            materialDisco.SetFloat("_Metallic", 0f);
            rendererDisco.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendererDisco.receiveShadows = false;
            rendererDisco.sharedMaterial = materialDisco;
            rendererDisco.enabled = false;
        }
    }

    // Esta funcion devuelve un color segun la zona golpeada.
    private Color ObtenerColorPorZona(TipoZonaDanio tipoZona)
    {
        // Elegimos el color segun el tipo de zona.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                return colorGolpeCritico;

            case TipoZonaDanio.Espalda:
                return colorGolpeEspalda;

            default:
                return colorGolpeNormal;
        }
    }

    // Esta corrutina aplica el destello y luego restaura colores originales.
    private IEnumerator CorrutinaDestello(Color colorDestello)
    {
        // Aplicamos color de impacto en todos los materiales.
        for (int indice = 0; indice < materiales.Count; indice++)
        {
            // Mezclamos color original con color de impacto.
            Color colorMezclado = Color.Lerp(coloresOriginales[indice], colorDestello, intensidadDestello);

            // Aplicamos el color mezclado al material.
            EscribirColorPrincipal(materiales[indice], colorMezclado);

            // Si la opcion esta activa y el shader tiene emision, la encendemos.
            if (usarEmision && materiales[indice].HasProperty("_EmissionColor"))
            {
                materiales[indice].EnableKeyword("_EMISSION");
                materiales[indice].SetColor("_EmissionColor", colorMezclado);
            }
        }

        // Esperamos el tiempo de destello.
        yield return new WaitForSeconds(duracionDestello);

        // Restauramos los colores originales.
        AplicarColoresBaseSegunEstado();

        // Limpiamos referencia para saber que ya termino.
        corrutinaDestelloActiva = null;
    }

    // Esta funcion aplica el color base correcto segun si el enemigo esta o no dentro del rango de ataque.
    private void AplicarColoresBaseSegunEstado()
    {
        // Recorremos todos los materiales conocidos.
        for (int indice = 0; indice < materiales.Count; indice++)
        {
            // Partimos siempre del color original capturado al iniciar.
            Color colorBase = coloresOriginales[indice];

            // Si el indicador de rango esta activo, lo mezclamos suavemente con rojo tenue.
            if (indicadorRangoActivo)
            {
                colorBase = Color.Lerp(colorBase, colorIndicadorRango, intensidadIndicadorRango);
            }

            // Si el enemigo esta cargando un ataque, el telegraph tiene prioridad visual.
            if (telegraphAtaqueActivo)
            {
                colorBase = Color.Lerp(colorBase, colorTelegraphAtaque, intensidadTelegraphAtaque);
            }

            // Si esta vulnerable tras parry, lo teñimos de un cian claro para leerlo rapido.
            if (vulnerableParryActiva)
            {
                colorBase = Color.Lerp(colorBase, colorVulnerableParry, intensidadVulnerableParry);
            }

            // Escribimos el color base final en el material.
            EscribirColorPrincipal(materiales[indice], colorBase);

            // Si usamos emision y el shader la soporta, la dejamos apagada mientras no haya un golpe.
            if (usarEmision && materiales[indice].HasProperty("_EmissionColor"))
            {
                materiales[indice].SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // Esta funcion recoloca el disco bajo el enemigo y lo enciende solo cuando hay estados legibles.
    private void ActualizarIndicadorSuelo()
    {
        if (!usarIndicadorSuelo)
        {
            return;
        }

        Transform indicador = transform.Find("IndicadorSueloCombate");
        if (indicador == null)
        {
            return;
        }

        MeshRenderer rendererIndicador = indicador.GetComponent<MeshRenderer>();
        if (rendererIndicador == null || rendererIndicador.sharedMaterial == null)
        {
            return;
        }

        Bounds bounds;
        bool tieneBounds = IntentarCalcularBounds(out bounds);
        if (!tieneBounds)
        {
            rendererIndicador.enabled = false;
            return;
        }

        bool mostrar = telegraphAtaqueActivo || vulnerableParryActiva;
        rendererIndicador.enabled = mostrar;
        if (!mostrar)
        {
            return;
        }

        float tamanoBase = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 1.1f, 1.2f, 3.4f);
        indicador.position = new Vector3(bounds.center.x, bounds.min.y + alturaIndicadorSuelo, bounds.center.z);
        indicador.rotation = Quaternion.identity;
        indicador.localScale = new Vector3(tamanoBase, 0.01f, tamanoBase);

        Color colorObjetivo = colorTelegraphAtaque;
        float alphaBase = 0.28f;

        if (vulnerableParryActiva)
        {
            colorObjetivo = colorVulnerableParry;
            alphaBase = 0.26f;
        }

        if (telegraphAtaqueActivo)
        {
            colorObjetivo = Color.Lerp(colorTelegraphAtaque, Color.white, (Mathf.Sin(Time.time * velocidadPulsoTelegraph) + 1f) * 0.12f);
            alphaBase = 0.34f + (Mathf.Sin(Time.time * velocidadPulsoTelegraph) + 1f) * 0.08f;
        }
        else if (vulnerableParryActiva)
        {
            colorObjetivo = Color.Lerp(colorVulnerableParry, Color.white, (Mathf.Sin(Time.time * velocidadPulsoVulnerable) + 1f) * 0.08f);
            alphaBase = 0.24f + (Mathf.Sin(Time.time * velocidadPulsoVulnerable) + 1f) * 0.05f;
        }

        Color colorFinal = new Color(colorObjetivo.r, colorObjetivo.g, colorObjetivo.b, alphaBase);
        rendererIndicador.sharedMaterial.color = colorFinal;
        if (rendererIndicador.sharedMaterial.HasProperty("_EmissionColor"))
        {
            rendererIndicador.sharedMaterial.EnableKeyword("_EMISSION");
            rendererIndicador.sharedMaterial.SetColor("_EmissionColor", colorFinal * 0.22f);
        }
    }

    // Esta funcion calcula bounds visuales del enemigo para posicionar correctamente el disco.
    private bool IntentarCalcularBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool encontro = false;

        for (int indice = 0; indice < renderizadoresObjetivo.Length; indice++)
        {
            Renderer renderizador = renderizadoresObjetivo[indice];
            if (renderizador == null || !renderizador.enabled)
            {
                continue;
            }

            if (!encontro)
            {
                bounds = renderizador.bounds;
                encontro = true;
            }
            else
            {
                bounds.Encapsulate(renderizador.bounds);
            }
        }

        return encontro;
    }

    // Esta funcion intenta leer el color principal de un material.
    private Color LeerColorPrincipal(Material material)
    {
        // Si el shader usa BaseColor, leemos ese valor.
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        // Si el shader usa Color clasico, leemos ese valor.
        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        // Si no hay propiedad de color compatible, devolvemos blanco.
        return Color.white;
    }

    // Esta funcion intenta escribir el color principal de un material.
    private void EscribirColorPrincipal(Material material, Color color)
    {
        // Si el shader usa BaseColor, escribimos ahi.
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        // Si el shader usa Color clasico, escribimos tambien ahi.
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
