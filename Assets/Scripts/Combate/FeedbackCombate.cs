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

    // Esta lista guarda materiales para evitar buscarlos en cada golpe.
    private readonly List<Material> materiales = new List<Material>();

    // Esta lista guarda el color original de cada material.
    private readonly List<Color> coloresOriginales = new List<Color>();

    // Esta referencia guarda la corrutina de destello actual.
    private Coroutine corrutinaDestelloActiva;

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
        for (int indice = 0; indice < materiales.Count; indice++)
        {
            // Volvemos al color original para no dejar el material alterado.
            EscribirColorPrincipal(materiales[indice], coloresOriginales[indice]);

            // Si usamos emision, tambien restauramos emision.
            if (usarEmision && materiales[indice].HasProperty("_EmissionColor"))
            {
                materiales[indice].SetColor("_EmissionColor", Color.black);
            }
        }

        // Limpiamos referencia para saber que ya termino.
        corrutinaDestelloActiva = null;
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

