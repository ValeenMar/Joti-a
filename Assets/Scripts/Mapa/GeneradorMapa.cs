using System.Collections.Generic;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este componente centraliza la generacion visual del mapa medieval:
// terreno, decoracion, antorchas y puntos de spawn.

// Esta clase construye el mapa medieval usando recursos asignados desde el editor.
public class GeneradorMapa : MonoBehaviour
{
    // Material principal del terreno.
    [Header("Terreno")]
    [SerializeField] private Material materialTerreno;

    // Tamano total del mapa.
    [SerializeField] private Vector2 tamanoMapa = new Vector2(120f, 120f);

    // Tamano del area de combate despejada.
    [SerializeField] private Vector2 tamanoAreaCombate = new Vector2(36f, 36f);

    // Desplazamiento vertical base para toda la decoracion.
    [SerializeField] private float alturaBase = 0f;

    // Seed para la distribucion procedural.
    [Header("Decoracion")]
    [SerializeField] private int semillaDecoracion = 42;

    // Cantidad de arboles del borde.
    [SerializeField] private int cantidadArboles = 44;

    // Cantidad de rocas del borde.
    [SerializeField] private int cantidadRocas = 18;

    // Cantidad de arbustos del borde.
    [SerializeField] private int cantidadArbustos = 16;

    // Cantidad de antorchas en las esquinas.
    [SerializeField] private int cantidadAntorchas = 4;

    // Prefabs opcionais para decorar.
    [SerializeField] private GameObject prefabArbol;
    [SerializeField] private GameObject prefabRoca;
    [SerializeField] private GameObject prefabArbusto;
    [SerializeField] private GameObject prefabValla;
    [SerializeField] private GameObject prefabAntorcha;

    // Contenedores internos para ordenar la escena.
    private Transform contenedorDecoracion;
    private Transform contenedorSpawn;

    // Referencia al terreno creado.
    private Terrain terrenoPrincipal;

    // Cache de puntos de spawn.
    private readonly List<Transform> puntosSpawn = new List<Transform>();

    // Expone los puntos generados.
    public Transform[] PuntosSpawnGenerados => puntosSpawn.ToArray();

    // Esta funcion permite asignar recursos desde el setup de editor.
    public void AsignarRecursos(Material material, GameObject arbol, GameObject roca, GameObject arbusto, GameObject valla, GameObject antorcha)
    {
        materialTerreno = material;
        prefabArbol = arbol;
        prefabRoca = roca;
        prefabArbusto = arbusto;
        prefabValla = valla;
        prefabAntorcha = antorcha;
    }

    // Esta funcion elimina toda la generacion anterior.
    public void LimpiarMapaGenerado()
    {
        if (contenedorDecoracion != null)
        {
            DestruirObjeto(contenedorDecoracion.gameObject);
            contenedorDecoracion = null;
        }

        if (contenedorSpawn != null)
        {
            DestruirObjeto(contenedorSpawn.gameObject);
            contenedorSpawn = null;
        }

        if (terrenoPrincipal != null)
        {
            DestruirObjeto(terrenoPrincipal.gameObject);
            terrenoPrincipal = null;
        }

        puntosSpawn.Clear();
    }

    // Esta funcion genera el mapa completo.
    public void GenerarMapa()
    {
        LimpiarMapaGenerado();
        CrearTerrenoBase();
        CrearContenedores();
        GenerarDecoracionPerimetral();
        GenerarAntorchasEsquinas();
        GenerarPuntosSpawn();
    }

    // Crea un terreno simple y plano.
    private void CrearTerrenoBase()
    {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 33;
        terrainData.size = new Vector3(tamanoMapa.x, 12f, tamanoMapa.y);
        terrainData.SetHeights(0, 0, CrearAlturasPlanas(terrainData.heightmapResolution));

        GameObject objetoTerreno = Terrain.CreateTerrainGameObject(terrainData);
        objetoTerreno.name = "TerrenoMedieval";
        objetoTerreno.transform.SetParent(transform, false);
        objetoTerreno.transform.localPosition = new Vector3(-tamanoMapa.x * 0.5f, alturaBase, -tamanoMapa.y * 0.5f);

        terrenoPrincipal = objetoTerreno.GetComponent<Terrain>();
        if (terrenoPrincipal != null && materialTerreno != null)
        {
            terrenoPrincipal.materialTemplate = materialTerreno;
        }
    }

    // Devuelve una matriz de alturas completamente plana.
    private float[,] CrearAlturasPlanas(int resolucion)
    {
        float[,] alturas = new float[resolucion, resolucion];
        for (int x = 0; x < resolucion; x++)
        {
            for (int z = 0; z < resolucion; z++)
            {
                alturas[x, z] = 0f;
            }
        }

        return alturas;
    }

    // Crea los contenedores de decoracion y spawn.
    private void CrearContenedores()
    {
        GameObject decoracion = new GameObject("DecoracionMedieval");
        decoracion.transform.SetParent(transform, false);
        contenedorDecoracion = decoracion.transform;

        GameObject spawn = new GameObject("PuntosSpawnOleadas");
        spawn.transform.SetParent(transform, false);
        contenedorSpawn = spawn.transform;
    }

    // Genera arboles, rocas y arbustos en el borde del mapa.
    private void GenerarDecoracionPerimetral()
    {
        Random.InitState(semillaDecoracion);

        float mitadX = tamanoMapa.x * 0.5f;
        float mitadZ = tamanoMapa.y * 0.5f;
        float combateMitadX = tamanoAreaCombate.x * 0.5f;
        float combateMitadZ = tamanoAreaCombate.y * 0.5f;

        GenerarGrupoBorde(prefabArbol, cantidadArboles, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.92f, "Arbol");
        GenerarGrupoBorde(prefabRoca, cantidadRocas, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.86f, "Roca");
        GenerarGrupoBorde(prefabArbusto, cantidadArbustos, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.95f, "Arbusto");

        if (prefabValla != null)
        {
            CrearCercadoPerimetral(mitadX, mitadZ);
        }
    }

    // Coloca objetos alrededor del mapa sin invadir la arena central.
    private void GenerarGrupoBorde(GameObject prefab, int cantidad, float mitadX, float mitadZ, float combateMitadX, float combateMitadZ, float escalaMinima, string prefijo)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 posicion = GenerarPosicionEnBorde(mitadX, mitadZ, combateMitadX, combateMitadZ);
            GameObject instancia = CrearInstanciaDecoracion(prefab, posicion, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), prefijo + "_" + (i + 1), Vector3.one * Random.Range(escalaMinima, 1.2f));

            if (instancia == null)
            {
                CrearFallbackDecoracion(prefijo, posicion, Random.Range(0.8f, 1.3f));
            }
        }
    }

    // Calcula una posicion alrededor del borde del mapa.
    private Vector3 GenerarPosicionEnBorde(float mitadX, float mitadZ, float combateMitadX, float combateMitadZ)
    {
        float angulo = Random.Range(0f, Mathf.PI * 2f);
        float radioX = Random.Range(combateMitadX + 6f, mitadX - 4f);
        float radioZ = Random.Range(combateMitadZ + 6f, mitadZ - 4f);
        Vector3 posicion = new Vector3(Mathf.Cos(angulo) * radioX, alturaBase, Mathf.Sin(angulo) * radioZ);

        if (Mathf.Abs(posicion.x) < combateMitadX)
        {
            posicion.x = posicion.x >= 0f ? combateMitadX + 6f : -combateMitadX - 6f;
        }

        if (Mathf.Abs(posicion.z) < combateMitadZ)
        {
            posicion.z = posicion.z >= 0f ? combateMitadZ + 6f : -combateMitadZ - 6f;
        }

        return posicion;
    }

    // Crea un cercado visual alrededor del mapa.
    private void CrearCercadoPerimetral(float mitadX, float mitadZ)
    {
        float[] lados = { -mitadX, mitadX };

        for (int i = 0; i < lados.Length; i++)
        {
            for (int j = -3; j <= 3; j++)
            {
                Vector3 posicionX = new Vector3(lados[i], alturaBase, j * 8f);
                CrearInstanciaDecoracion(prefabValla, posicionX, Quaternion.Euler(0f, i == 0 ? 90f : -90f, 0f), "Valla", Vector3.one);

                Vector3 posicionZ = new Vector3(j * 8f, alturaBase, lados[i]);
                CrearInstanciaDecoracion(prefabValla, posicionZ, Quaternion.Euler(0f, i == 0 ? 180f : 0f, 0f), "Valla", Vector3.one);
            }
        }
    }

    // Crea antorchas simples con Point Light.
    private void GenerarAntorchasEsquinas()
    {
        float margenX = tamanoAreaCombate.x * 0.5f + 4f;
        float margenZ = tamanoAreaCombate.y * 0.5f + 4f;

        Vector3[] esquinas =
        {
            new Vector3(-margenX, alturaBase, -margenZ),
            new Vector3(-margenX, alturaBase, margenZ),
            new Vector3(margenX, alturaBase, -margenZ),
            new Vector3(margenX, alturaBase, margenZ)
        };

        for (int i = 0; i < esquinas.Length; i++)
        {
            GameObject raiz = new GameObject("Antorcha_" + (i + 1));
            raiz.transform.SetParent(contenedorDecoracion, false);
            raiz.transform.localPosition = esquinas[i];

            if (prefabAntorcha != null)
            {
                GameObject prefab = Instantiate(prefabAntorcha, raiz.transform);
                prefab.name = "Visual";
                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;
            }
            else
            {
                GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                palo.name = "Palo";
                palo.transform.SetParent(raiz.transform, false);
                palo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                palo.transform.localScale = new Vector3(0.15f, 0.65f, 0.15f);
                DestruirObjeto(palo.GetComponent<Collider>());

                GameObject fuego = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fuego.name = "Fuego";
                fuego.transform.SetParent(raiz.transform, false);
                fuego.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                fuego.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                DestruirObjeto(fuego.GetComponent<Collider>());
            }

            Light luz = raiz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.6f, 0.25f, 1f);
            luz.intensity = 1.5f;
            luz.range = 8f;
        }
    }

    // Crea los cuatro puntos de spawn en las esquinas de la arena.
    private void GenerarPuntosSpawn()
    {
        puntosSpawn.Clear();

        float margenX = tamanoAreaCombate.x * 0.5f + 6f;
        float margenZ = tamanoAreaCombate.y * 0.5f + 6f;

        Vector3[] esquinas =
        {
            new Vector3(-margenX, alturaBase, -margenZ),
            new Vector3(-margenX, alturaBase, margenZ),
            new Vector3(margenX, alturaBase, -margenZ),
            new Vector3(margenX, alturaBase, margenZ)
        };

        for (int i = 0; i < esquinas.Length; i++)
        {
            GameObject punto = new GameObject("PuntoSpawn_" + (i + 1));
            punto.transform.SetParent(contenedorSpawn, false);
            punto.transform.localPosition = esquinas[i];
            punto.transform.LookAt(Vector3.zero);
            puntosSpawn.Add(punto.transform);
        }
    }

    // Crea una instancia de decoracion o devuelve null si no hay prefab.
    private GameObject CrearInstanciaDecoracion(GameObject prefab, Vector3 posicion, Quaternion rotacion, string nombre, Vector3 escala)
    {
        if (prefab == null || contenedorDecoracion == null)
        {
            return null;
        }

        GameObject instancia = Instantiate(prefab, posicion, rotacion, contenedorDecoracion);
        instancia.name = nombre;
        instancia.transform.localScale = escala;
        return instancia;
    }

    // Crea un fallback con primitivas si el prefab no esta disponible.
    private void CrearFallbackDecoracion(string nombre, Vector3 posicion, float escala)
    {
        GameObject raiz = new GameObject(nombre + "_Fallback");
        raiz.transform.SetParent(contenedorDecoracion, false);
        raiz.transform.localPosition = posicion;

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(raiz.transform, false);
        tronco.transform.localScale = new Vector3(escala * 0.4f, escala, escala * 0.4f);
        DestruirObjeto(tronco.GetComponent<Collider>());
    }

    // Destruye un objeto o componente de forma segura en editor/runtime.
    private void DestruirObjeto(Object objeto)
    {
        if (objeto == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(objeto);
        }
        else
        {
            DestroyImmediate(objeto);
        }
    }

    // COPILOT-EXPAND: aqui podes sumar ruinas, senderos, agua, niebla por zonas o variacion por bioma.
}
