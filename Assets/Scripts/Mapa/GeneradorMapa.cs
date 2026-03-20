using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este componente centraliza la generacion visual del mapa medieval
// usando una base simple, solida y compatible con Built-in Render Pipeline.
// La prioridad de esta version es:
// 1. piso legible con colision,
// 2. decoracion en los bordes,
// 3. area central limpia para combate,
// 4. puntos de spawn estables.

// Esta clase construye el mapa medieval usando recursos asignados desde el editor.
public class GeneradorMapa : MonoBehaviour
{
    // Este valor define el grosor del piso para que tenga colision solida.
    private const float GrosorPiso = 1f;

    // Material principal del piso.
    [Header("Piso")]
    [SerializeField] private Material materialTerreno;

    // Esta textura permite forzar un suelo con detalle cuando el material importado no alcanza.
    [SerializeField] private Texture texturaPisoPreferida;

    // Tamano total del mapa.
    [SerializeField] private Vector2 tamanoMapa = new Vector2(120f, 120f);

    // Tamano del area de combate despejada.
    [SerializeField] private Vector2 tamanoAreaCombate = new Vector2(42f, 42f);

    // Desplazamiento vertical base de toda la escena jugable.
    [SerializeField] private float alturaBase = 0f;

    // Cuanto se repite la textura del piso.
    [SerializeField] private Vector2 mosaicoPiso = new Vector2(10f, 10f);

    // Cuanto se repite la textura del borde visual para que el terreno no se vea plano.
    [SerializeField] private Vector2 mosaicoBordePiso = new Vector2(6f, 6f);

    // Seed para decoracion procedural reproducible.
    [Header("Decoracion")]
    [SerializeField] private int semillaDecoracion = 42;

    // Cantidad de arboles del borde.
    [SerializeField] private int cantidadArboles = 36;

    // Cantidad de rocas grandes del borde.
    [SerializeField] private int cantidadRocas = 14;

    // Cantidad de arbustos del borde.
    [SerializeField] private int cantidadArbustos = 12;

    // Prefabs opcionales del pack.
    [SerializeField] private GameObject prefabArbol;
    [SerializeField] private GameObject prefabRoca;
    [SerializeField] private GameObject prefabArbusto;
    [SerializeField] private GameObject prefabValla;
    [SerializeField] private GameObject prefabAntorcha;

    // Contenedores internos para ordenar mejor la jerarquia.
    private Transform contenedorDecoracion;
    private Transform contenedorSpawn;

    // Referencia al piso principal creado.
    private GameObject pisoPrincipal;

    // Cache de puntos de spawn generados.
    private readonly List<Transform> puntosSpawn = new List<Transform>();

    // Esta propiedad expone los puntos de spawn al sistema de oleadas.
    public Transform[] PuntosSpawnGenerados => puntosSpawn.ToArray();

    // Esta funcion permite asignar recursos desde el setup del editor.
    public void AsignarRecursos(Material material, Texture texturaPiso, GameObject arbol, GameObject roca, GameObject arbusto, GameObject valla, GameObject antorcha)
    {
        materialTerreno = material;
        texturaPisoPreferida = texturaPiso;
        prefabArbol = arbol;
        prefabRoca = roca;
        prefabArbusto = arbusto;
        prefabValla = valla;
        prefabAntorcha = antorcha;
    }

    // Esta funcion elimina toda la generacion anterior.
    public void LimpiarMapaGenerado()
    {
        for (int indiceHijo = transform.childCount - 1; indiceHijo >= 0; indiceHijo--)
        {
            Transform hijoActual = transform.GetChild(indiceHijo);
            if (hijoActual == null)
            {
                continue;
            }

            DestruirObjeto(hijoActual.gameObject);
        }

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

        if (pisoPrincipal != null)
        {
            DestruirObjeto(pisoPrincipal);
            pisoPrincipal = null;
        }

        puntosSpawn.Clear();
    }

    // Esta funcion genera el mapa completo.
    public void GenerarMapa()
    {
        LimpiarMapaGenerado();
        Random.InitState(semillaDecoracion);
        CrearPisoBase();
        CrearContenedores();
        CrearBordeVisualSuave();
        GenerarArenaAutorada();
        GenerarDecoracionPerimetral();
        GenerarPuntosSpawn();
    }

    // Este metodo agrega una composicion authored para que el mapa se sienta como una arena real y no solo un plano vacio.
    private void GenerarArenaAutorada()
    {
        GenerarCarrilIzquierdoBoscoso();
        GenerarCarrilDerechoConVallasYAntorchas();
        GenerarChokePointsLaterales();
        GenerarLandmarkFondoRuina();
        GenerarAntorchasEsquinas();
    }

    // Este metodo arma un lateral izquierdo mas cerrado con arboles y rocas bajas.
    private void GenerarCarrilIzquierdoBoscoso()
    {
        float bordeInteriorX = -(tamanoAreaCombate.x * 0.5f + 7.5f);
        float[] alturasZ = { -18f, -8f, 4f, 15f };

        for (int indice = 0; indice < alturasZ.Length; indice++)
        {
            Vector3 posicionArbol = new Vector3(bordeInteriorX - Random.Range(1.5f, 4.5f), alturaBase, alturasZ[indice]);
            GameObject arbol = CrearInstanciaDecoracion(prefabArbol, posicionArbol, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), "CarrilIzquierdo_Arbol_" + indice, Vector3.one * Random.Range(0.92f, 1.15f), "Arbol");
            if (arbol == null)
            {
                CrearFallbackDecoracion("Arbol", posicionArbol, 1.15f);
            }

            Vector3 posicionRoca = posicionArbol + new Vector3(Random.Range(1.8f, 3.2f), 0f, Random.Range(-1.2f, 1.2f));
            GameObject roca = CrearInstanciaDecoracion(prefabRoca, posicionRoca, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), "CarrilIzquierdo_Roca_" + indice, Vector3.one * Random.Range(0.85f, 1.08f), "Roca");
            if (roca == null)
            {
                CrearFallbackDecoracion("Roca", posicionRoca, 0.95f);
            }
        }
    }

    // Este metodo arma un lateral derecho mas artificial con vallas rotas y antorchas.
    private void GenerarCarrilDerechoConVallasYAntorchas()
    {
        float bordeInteriorX = tamanoAreaCombate.x * 0.5f + 7.8f;
        float[] posicionesZ = { -16f, -6f, 6f, 16f };

        for (int indice = 0; indice < posicionesZ.Length; indice++)
        {
            Vector3 posicionValla = new Vector3(bordeInteriorX + Random.Range(1.2f, 3.5f), alturaBase, posicionesZ[indice]);
            GameObject valla = CrearInstanciaDecoracion(prefabValla, posicionValla, Quaternion.Euler(0f, 90f + Random.Range(-12f, 12f), 0f), "CarrilDerecho_Valla_" + indice, Vector3.one, "Valla");
            if (valla == null)
            {
                CrearFallbackDecoracion("Valla", posicionValla, 1f);
            }
        }

        for (int indiceAntorcha = 0; indiceAntorcha < 2; indiceAntorcha++)
        {
            Vector3 posicionAntorcha = new Vector3(bordeInteriorX + 1.5f, alturaBase, indiceAntorcha == 0 ? -10f : 10f);
            CrearAntorchaSimple("CarrilDerecho_Antorcha_" + indiceAntorcha, posicionAntorcha);
        }
    }

    // Este metodo crea dos choke points suaves para darle mas lectura tactica a la arena.
    private void GenerarChokePointsLaterales()
    {
        Vector3[] posicionesChoke =
        {
            new Vector3(-9f, alturaBase, 7f),
            new Vector3(9f, alturaBase, -7f)
        };

        for (int indice = 0; indice < posicionesChoke.Length; indice++)
        {
            Vector3 posicion = posicionesChoke[indice];
            GameObject roca = CrearInstanciaDecoracion(prefabRoca, posicion, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), "ChokePoint_Roca_" + indice, Vector3.one * 1.05f, "Roca");
            if (roca == null)
            {
                CrearFallbackDecoracion("Roca", posicion, 1.1f);
            }
        }
    }

    // Este metodo crea una pequena ruina al fondo para que la arena tenga un landmark reconocible.
    private void GenerarLandmarkFondoRuina()
    {
        if (contenedorDecoracion == null)
        {
            return;
        }

        GameObject ruina = new GameObject("Landmark_RuinaFondo");
        ruina.transform.SetParent(contenedorDecoracion, false);
        ruina.transform.localPosition = new Vector3(0f, alturaBase, tamanoAreaCombate.y * 0.5f + 16f);
        ruina.transform.localRotation = Quaternion.identity;

        CrearBloqueRuina(ruina.transform, "Base", new Vector3(0f, 0.65f, 0f), new Vector3(6.2f, 1.3f, 2.4f));
        CrearBloqueRuina(ruina.transform, "ColumnaIzquierda", new Vector3(-2.4f, 1.9f, 0f), new Vector3(1.1f, 2.4f, 1.1f));
        CrearBloqueRuina(ruina.transform, "ColumnaDerecha", new Vector3(2.4f, 1.9f, 0f), new Vector3(1.1f, 2.4f, 1.1f));
        CrearBloqueRuina(ruina.transform, "Lintel", new Vector3(0f, 3.15f, 0f), new Vector3(4.6f, 0.7f, 1.1f));
        CrearBloqueRuina(ruina.transform, "EscombroA", new Vector3(-3.1f, 0.55f, -1.8f), new Vector3(1.2f, 1.1f, 1.0f));
        CrearBloqueRuina(ruina.transform, "EscombroB", new Vector3(3.0f, 0.45f, 1.5f), new Vector3(1.4f, 0.9f, 0.9f));
    }

    // Este metodo crea una antorcha authored simple reutilizando la misma estetica del mapa.
    private void CrearAntorchaSimple(string nombre, Vector3 posicionLocal)
    {
        GameObject antorcha = new GameObject(nombre);
        antorcha.transform.SetParent(contenedorDecoracion, false);
        antorcha.transform.localPosition = posicionLocal;
        antorcha.transform.localRotation = Quaternion.identity;

        GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        palo.name = "Palo";
        palo.transform.SetParent(antorcha.transform, false);
        palo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        palo.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);

        MeshRenderer rendererPalo = palo.GetComponent<MeshRenderer>();
        if (rendererPalo != null)
        {
            Material materialPalo = new Material(Shader.Find("Standard"));
            materialPalo.color = new Color(0.34f, 0.22f, 0.10f, 1f);
            materialPalo.SetFloat("_Glossiness", 0.06f);
            rendererPalo.sharedMaterial = materialPalo;
        }

        Collider colliderPalo = palo.GetComponent<Collider>();
        if (colliderPalo != null)
        {
            colliderPalo.enabled = false;
        }

        GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Cube);
        llama.name = "Llama";
        llama.transform.SetParent(antorcha.transform, false);
        llama.transform.localPosition = new Vector3(0f, 1.42f, 0f);
        llama.transform.localScale = new Vector3(0.18f, 0.24f, 0.18f);

        MeshRenderer rendererLlama = llama.GetComponent<MeshRenderer>();
        if (rendererLlama != null)
        {
            Material materialLlama = new Material(Shader.Find("Standard"));
            materialLlama.color = new Color(1f, 0.62f, 0.18f, 1f);
            materialLlama.EnableKeyword("_EMISSION");
            materialLlama.SetColor("_EmissionColor", new Color(1f, 0.36f, 0.06f, 0.45f));
            rendererLlama.sharedMaterial = materialLlama;
        }

        Collider colliderLlama = llama.GetComponent<Collider>();
        if (colliderLlama != null)
        {
            colliderLlama.enabled = false;
        }

        CapsuleCollider colliderRaiz = antorcha.AddComponent<CapsuleCollider>();
        colliderRaiz.direction = 1;
        colliderRaiz.radius = 0.2f;
        colliderRaiz.height = 1.75f;
        colliderRaiz.center = new Vector3(0f, 0.88f, 0f);

        Light luz = antorcha.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.60f, 0.24f, 1f);
        luz.intensity = 1.15f;
        luz.range = 4.4f;
        luz.shadows = LightShadows.None;
    }

    // Este metodo crea un bloque simple de ruina con material de piedra y colision.
    private void CrearBloqueRuina(Transform padre, string nombre, Vector3 posicionLocal, Vector3 escalaLocal)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicionLocal;
        bloque.transform.localRotation = Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
        bloque.transform.localScale = escalaLocal;

        MeshRenderer rendererBloque = bloque.GetComponent<MeshRenderer>();
        if (rendererBloque != null)
        {
            Material materialPiedra = new Material(Shader.Find("Standard"));
            materialPiedra.color = new Color(0.42f, 0.40f, 0.36f, 1f);
            materialPiedra.SetFloat("_Glossiness", 0.04f);
            rendererBloque.sharedMaterial = materialPiedra;
        }
    }

    // Este metodo crea un piso plano con collider real y material compatible con Built-in.
    private void CrearPisoBase()
    {
        pisoPrincipal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pisoPrincipal.name = "TerrenoMedieval";
        pisoPrincipal.transform.SetParent(transform, false);
        pisoPrincipal.transform.localPosition = new Vector3(0f, alturaBase - (GrosorPiso * 0.5f), 0f);
        pisoPrincipal.transform.localRotation = Quaternion.identity;
        pisoPrincipal.transform.localScale = new Vector3(tamanoMapa.x, GrosorPiso, tamanoMapa.y);

        MeshRenderer rendererPiso = pisoPrincipal.GetComponent<MeshRenderer>();
        if (rendererPiso != null)
        {
            rendererPiso.sharedMaterial = CrearMaterialInstanciaPiso();
            rendererPiso.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendererPiso.receiveShadows = true;
        }

        BoxCollider colliderPiso = pisoPrincipal.GetComponent<BoxCollider>();
        if (colliderPiso != null)
        {
            colliderPiso.size = Vector3.one;
            colliderPiso.center = Vector3.zero;
        }

        GameObject baseVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseVisual.name = "BaseTerreno";
        baseVisual.transform.SetParent(transform, false);
        baseVisual.transform.localPosition = new Vector3(0f, alturaBase - 1.05f, 0f);
        baseVisual.transform.localRotation = Quaternion.identity;
        baseVisual.transform.localScale = new Vector3(tamanoMapa.x + 4f, 1.1f, tamanoMapa.y + 4f);

        MeshRenderer rendererBase = baseVisual.GetComponent<MeshRenderer>();
        if (rendererBase != null)
        {
            Material materialBase = new Material(Shader.Find("Standard"));
            materialBase.name = "Material_BaseTerreno_Runtime";
            materialBase.color = new Color(0.28f, 0.22f, 0.18f, 1f);
            materialBase.SetFloat("_Glossiness", 0.05f);
            rendererBase.sharedMaterial = materialBase;
        }

        Collider colliderBase = baseVisual.GetComponent<Collider>();
        if (colliderBase != null)
        {
            colliderBase.enabled = false;
        }
    }

    // Este metodo crea un material runtime para el piso sin pisar el asset importado.
    private Material CrearMaterialInstanciaPiso()
    {
        Shader shaderStandard = Shader.Find("Standard");
        Material materialInstancia = new Material(shaderStandard);
        materialInstancia.name = "Material_PisoMedieval_Runtime";
        materialInstancia.color = new Color(0.3f, 0.64f, 0.3f, 1f);
        materialInstancia.SetFloat("_Glossiness", 0.05f);

        // Si el setup nos asigno una textura concreta de suelo, la usamos primero.
        if (texturaPisoPreferida != null)
        {
            materialInstancia.mainTexture = texturaPisoPreferida;
            materialInstancia.mainTextureScale = mosaicoPiso;
        }

        if (materialTerreno != null)
        {
            bool materialTerrenoSeguro = materialTerreno.name.ToLowerInvariant().Contains("terrain");
            Texture texturaPrincipal = null;
            if (materialTerreno.HasProperty("_MainTex"))
            {
                texturaPrincipal = materialTerreno.GetTexture("_MainTex");
            }

            if (texturaPrincipal == null && materialTerreno.mainTexture != null)
            {
                texturaPrincipal = materialTerreno.mainTexture;
            }

            if (materialInstancia.mainTexture == null && texturaPrincipal != null)
            {
                materialInstancia.mainTexture = texturaPrincipal;
                materialInstancia.mainTextureScale = mosaicoPiso;
            }

            if (materialTerreno.HasProperty("_Color"))
            {
                Color colorBase = materialTerreno.GetColor("_Color");
                materialInstancia.color = materialTerrenoSeguro
                    ? Color.Lerp(new Color(0.36f, 0.62f, 0.34f, 1f), colorBase, 0.2f)
                    : Color.Lerp(new Color(0.36f, 0.62f, 0.34f, 1f), colorBase, 0.12f);
            }
        }

        return materialInstancia;
    }

    // Este metodo agrega una banda visual suave alrededor del area central para cortar la monotonia del piso.
    private void CrearBordeVisualSuave()
    {
        if (contenedorDecoracion == null)
        {
            return;
        }

        float alturaBorde = alturaBase + 0.02f;
        float mitadCombateX = tamanoAreaCombate.x * 0.5f;
        float mitadCombateZ = tamanoAreaCombate.y * 0.5f;
        float anchoBanda = 5f;

        CrearBandaVisual("BandaNorte", new Vector3(0f, alturaBorde, mitadCombateZ + (anchoBanda * 0.5f)), new Vector3(tamanoAreaCombate.x + 6f, 0.06f, anchoBanda), true);
        CrearBandaVisual("BandaSur", new Vector3(0f, alturaBorde, -(mitadCombateZ + (anchoBanda * 0.5f))), new Vector3(tamanoAreaCombate.x + 6f, 0.06f, anchoBanda), true);
        CrearBandaVisual("BandaEste", new Vector3(mitadCombateX + (anchoBanda * 0.5f), alturaBorde, 0f), new Vector3(anchoBanda, 0.06f, tamanoAreaCombate.y + 6f), false);
        CrearBandaVisual("BandaOeste", new Vector3(-(mitadCombateX + (anchoBanda * 0.5f)), alturaBorde, 0f), new Vector3(anchoBanda, 0.06f, tamanoAreaCombate.y + 6f), false);
    }

    // Este metodo crea una banda visual muy fina sin colision para dar variacion al terreno.
    private void CrearBandaVisual(string nombre, Vector3 posicionLocal, Vector3 escalaLocal, bool horizontal)
    {
        GameObject banda = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banda.name = nombre;
        banda.transform.SetParent(transform, false);
        banda.transform.localPosition = posicionLocal;
        banda.transform.localRotation = Quaternion.identity;
        banda.transform.localScale = escalaLocal;

        MeshRenderer rendererBanda = banda.GetComponent<MeshRenderer>();
        if (rendererBanda != null)
        {
            Material materialBanda = new Material(Shader.Find("Standard"));
            materialBanda.name = "Material_BandaTerreno_Runtime";
            materialBanda.color = horizontal
                ? new Color(0.28f, 0.46f, 0.24f, 1f)
                : new Color(0.24f, 0.42f, 0.2f, 1f);
            materialBanda.SetFloat("_Glossiness", 0.02f);

            if (texturaPisoPreferida != null)
            {
                materialBanda.mainTexture = texturaPisoPreferida;
                materialBanda.mainTextureScale = mosaicoBordePiso;
            }
            else if (materialTerreno != null && materialTerreno.mainTexture != null)
            {
                materialBanda.mainTexture = materialTerreno.mainTexture;
                materialBanda.mainTextureScale = mosaicoBordePiso;
            }

            rendererBanda.sharedMaterial = materialBanda;
            rendererBanda.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendererBanda.receiveShadows = true;
        }

        Collider colliderBanda = banda.GetComponent<Collider>();
        if (colliderBanda != null)
        {
            colliderBanda.enabled = false;
        }
    }

    // Este metodo crea los contenedores de decoracion y spawn.
    private void CrearContenedores()
    {
        GameObject decoracion = new GameObject("DecoracionMedieval");
        decoracion.transform.SetParent(transform, false);
        contenedorDecoracion = decoracion.transform;

        GameObject spawn = new GameObject("PuntosSpawnOleadas");
        spawn.transform.SetParent(transform, false);
        contenedorSpawn = spawn.transform;
    }

    // Este metodo genera arboles, rocas y arbustos en el borde del mapa.
    private void GenerarDecoracionPerimetral()
    {
        Random.InitState(semillaDecoracion);

        float mitadX = tamanoMapa.x * 0.5f;
        float mitadZ = tamanoMapa.y * 0.5f;
        float combateMitadX = tamanoAreaCombate.x * 0.5f;
        float combateMitadZ = tamanoAreaCombate.y * 0.5f;

        GenerarGrupoBorde(prefabArbol, cantidadArboles, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.9f, "Arbol");
        GenerarGrupoBorde(prefabRoca, cantidadRocas, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.9f, "Roca");
        GenerarGrupoBorde(prefabArbusto, cantidadArbustos, mitadX, mitadZ, combateMitadX, combateMitadZ, 0.85f, "Arbusto");

        if (prefabValla != null)
        {
            CrearCercadoPerimetral(mitadX, mitadZ);
        }
    }

    // Este metodo coloca un grupo de decoracion alrededor del borde.
    private void GenerarGrupoBorde(GameObject prefab, int cantidad, float mitadX, float mitadZ, float combateMitadX, float combateMitadZ, float escalaMinima, string prefijo)
    {
        for (int indice = 0; indice < cantidad; indice++)
        {
            Vector3 posicion = GenerarPosicionEnBorde(mitadX, mitadZ, combateMitadX, combateMitadZ);
            Quaternion rotacion = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Vector3 escala = Vector3.one * Random.Range(escalaMinima, 1.2f);

            GameObject instancia = CrearInstanciaDecoracion(prefab, posicion, rotacion, prefijo + "_" + (indice + 1), escala, prefijo);
            if (instancia == null)
            {
                CrearFallbackDecoracion(prefijo, posicion, Random.Range(0.8f, 1.2f));
            }
        }
    }

    // Este metodo calcula una posicion segura alrededor del borde.
    private Vector3 GenerarPosicionEnBorde(float mitadX, float mitadZ, float combateMitadX, float combateMitadZ)
    {
        float angulo = Random.Range(0f, Mathf.PI * 2f);
        float radioMinimoX = combateMitadX + 12f;
        float radioMinimoZ = combateMitadZ + 12f;
        float radioX = Random.Range(radioMinimoX, mitadX - 6f);
        float radioZ = Random.Range(radioMinimoZ, mitadZ - 6f);

        Vector3 posicion = new Vector3(Mathf.Cos(angulo) * radioX, alturaBase, Mathf.Sin(angulo) * radioZ);

        if (Mathf.Abs(posicion.x) < combateMitadX + 10f)
        {
            posicion.x = posicion.x >= 0f ? combateMitadX + 10f : -combateMitadX - 10f;
        }

        if (Mathf.Abs(posicion.z) < combateMitadZ + 10f)
        {
            posicion.z = posicion.z >= 0f ? combateMitadZ + 10f : -combateMitadZ - 10f;
        }

        return posicion;
    }

    // Este metodo crea un cercado visual alrededor del mapa.
    private void CrearCercadoPerimetral(float mitadX, float mitadZ)
    {
        float[] ladosX = { -mitadX + 4f, mitadX - 4f };
        float[] ladosZ = { -mitadZ + 4f, mitadZ - 4f };

        for (int indiceX = 0; indiceX < ladosX.Length; indiceX++)
        {
            for (int paso = -5; paso <= 5; paso++)
            {
                Vector3 posicionX = new Vector3(ladosX[indiceX], alturaBase, paso * 9f);
                CrearInstanciaDecoracion(prefabValla, posicionX, Quaternion.Euler(0f, indiceX == 0 ? 90f : -90f, 0f), "Valla_X", Vector3.one, "Valla");
            }
        }

        for (int indiceZ = 0; indiceZ < ladosZ.Length; indiceZ++)
        {
            for (int paso = -5; paso <= 5; paso++)
            {
                Vector3 posicionZ = new Vector3(paso * 9f, alturaBase, ladosZ[indiceZ]);
                CrearInstanciaDecoracion(prefabValla, posicionZ, Quaternion.Euler(0f, indiceZ == 0 ? 180f : 0f, 0f), "Valla_Z", Vector3.one, "Valla");
            }
        }
    }

    // Este metodo crea antorchas simples con colision base y luz mas controlada.
    private void GenerarAntorchasEsquinas()
    {
        float margenX = tamanoAreaCombate.x * 0.5f + 8f;
        float margenZ = tamanoAreaCombate.y * 0.5f + 8f;

        Vector3[] esquinas =
        {
            new Vector3(-margenX, alturaBase, -margenZ),
            new Vector3(-margenX, alturaBase, margenZ),
            new Vector3(margenX, alturaBase, -margenZ),
            new Vector3(margenX, alturaBase, margenZ)
        };

        for (int indice = 0; indice < esquinas.Length; indice++)
        {
            GameObject raizAntorcha = new GameObject("Antorcha_" + (indice + 1));
            raizAntorcha.transform.SetParent(contenedorDecoracion, false);
            raizAntorcha.transform.localPosition = esquinas[indice];
            raizAntorcha.transform.localRotation = Quaternion.identity;

            GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            palo.name = "Palo";
            palo.transform.SetParent(raizAntorcha.transform, false);
            palo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            palo.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);

            MeshRenderer rendererPalo = palo.GetComponent<MeshRenderer>();
            if (rendererPalo != null)
            {
                Material materialPalo = new Material(Shader.Find("Standard"));
                materialPalo.color = new Color(0.32f, 0.2f, 0.08f, 1f);
                materialPalo.SetFloat("_Glossiness", 0.08f);
                rendererPalo.sharedMaterial = materialPalo;
            }

            Collider colliderPalo = palo.GetComponent<Collider>();
            if (colliderPalo != null)
            {
                colliderPalo.enabled = false;
            }

            GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Cube);
            llama.name = "Llama";
            llama.transform.SetParent(raizAntorcha.transform, false);
            llama.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            llama.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);

            MeshRenderer rendererLlama = llama.GetComponent<MeshRenderer>();
            if (rendererLlama != null)
            {
                Material materialLlama = new Material(Shader.Find("Standard"));
                materialLlama.color = new Color(1f, 0.58f, 0.16f, 1f);
                materialLlama.EnableKeyword("_EMISSION");
                materialLlama.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.05f, 0.4f));
                rendererLlama.sharedMaterial = materialLlama;
            }

            Collider colliderLlama = llama.GetComponent<Collider>();
            if (colliderLlama != null)
            {
                colliderLlama.enabled = false;
            }

            CapsuleCollider colliderRaiz = raizAntorcha.AddComponent<CapsuleCollider>();
            colliderRaiz.direction = 1;
            colliderRaiz.radius = 0.18f;
            colliderRaiz.height = 1.6f;
            colliderRaiz.center = new Vector3(0f, 0.8f, 0f);

            Light luz = raizAntorcha.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.62f, 0.28f, 1f);
            luz.intensity = 1.1f;
            luz.range = 4.5f;
            luz.shadows = LightShadows.None;
        }
    }

    // Este metodo crea los cuatro puntos de spawn en las esquinas de la arena.
    private void GenerarPuntosSpawn()
    {
        puntosSpawn.Clear();

        Vector3[] esquinas =
        {
            new Vector3(-(tamanoAreaCombate.x * 0.5f + 11f), alturaBase, -9f),
            new Vector3(-(tamanoAreaCombate.x * 0.5f + 9f), alturaBase, 11f),
            new Vector3(tamanoAreaCombate.x * 0.5f + 11f, alturaBase, -11f),
            new Vector3(0f, alturaBase, tamanoAreaCombate.y * 0.5f + 20f)
        };

        for (int indice = 0; indice < esquinas.Length; indice++)
        {
            GameObject punto = new GameObject("PuntoSpawn_" + (indice + 1));
            punto.transform.SetParent(contenedorSpawn, false);
            punto.transform.localPosition = esquinas[indice];
            punto.transform.LookAt(Vector3.zero);
            puntosSpawn.Add(punto.transform);
        }
    }

    // Este metodo instancia un prefab de decoracion y le agrega colision si hace falta.
    private GameObject CrearInstanciaDecoracion(GameObject prefab, Vector3 posicion, Quaternion rotacion, string nombre, Vector3 escala, string tipoDecoracion)
    {
        if (prefab == null || contenedorDecoracion == null)
        {
            return null;
        }

        GameObject instancia = Instantiate(prefab, posicion, rotacion, contenedorDecoracion);
        instancia.name = nombre;
        instancia.transform.localScale = escala;

        ConfigurarColisionDecoracion(instancia, tipoDecoracion);
        return instancia;
    }

    // Este metodo agrega colision simple a la decoracion importante.
    private void ConfigurarColisionDecoracion(GameObject objetoDecoracion, string tipoDecoracion)
    {
        if (objetoDecoracion == null)
        {
            return;
        }

        Bounds bounds = CalcularBounds(objetoDecoracion);
        if (bounds.size == Vector3.zero)
        {
            return;
        }

        if (tipoDecoracion == "Arbol")
        {
            CapsuleCollider capsule = objetoDecoracion.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = objetoDecoracion.AddComponent<CapsuleCollider>();
            }

            float radio = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.12f, 0.35f, 0.9f);
            float altura = Mathf.Clamp(bounds.size.y * 0.55f, 2f, bounds.size.y);
            capsule.direction = 1;
            capsule.radius = radio;
            capsule.height = altura;
            capsule.center = objetoDecoracion.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + (altura * 0.5f), bounds.center.z));
        }
        else if (tipoDecoracion == "Roca" || tipoDecoracion == "Valla")
        {
            BoxCollider box = objetoDecoracion.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = objetoDecoracion.AddComponent<BoxCollider>();
            }

            Vector3 centroLocal = objetoDecoracion.transform.InverseTransformPoint(bounds.center);
            Vector3 tamanoLocal = bounds.size;
            tamanoLocal.x = Mathf.Max(0.4f, tamanoLocal.x);
            tamanoLocal.y = Mathf.Max(0.6f, tamanoLocal.y);
            tamanoLocal.z = Mathf.Max(0.2f, tamanoLocal.z);
            box.center = centroLocal;
            box.size = tamanoLocal;
        }

        if (tipoDecoracion == "Arbol" || tipoDecoracion == "Roca" || tipoDecoracion == "Valla")
        {
            NavMeshObstacle obstaculo = objetoDecoracion.GetComponent<NavMeshObstacle>();
            if (obstaculo == null)
            {
                obstaculo = objetoDecoracion.AddComponent<NavMeshObstacle>();
            }

            obstaculo.shape = NavMeshObstacleShape.Box;
            obstaculo.center = objetoDecoracion.transform.InverseTransformPoint(bounds.center);
            obstaculo.size = new Vector3(
                Mathf.Max(0.5f, bounds.size.x * 0.8f),
                Mathf.Max(1f, bounds.size.y * 0.8f),
                Mathf.Max(0.5f, bounds.size.z * 0.8f));
            obstaculo.carving = false;
        }
    }

    // Este metodo calcula bounds combinados de todos los renderers hijos.
    private Bounds CalcularBounds(GameObject objeto)
    {
        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(objeto.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int indice = 1; indice < renderers.Length; indice++)
        {
            if (renderers[indice] != null)
            {
                bounds.Encapsulate(renderers[indice].bounds);
            }
        }

        return bounds;
    }

    // Este metodo crea un fallback si no existe prefab decorativo.
    private void CrearFallbackDecoracion(string nombre, Vector3 posicion, float escala)
    {
        GameObject raiz = new GameObject(nombre + "_Fallback");
        raiz.transform.SetParent(contenedorDecoracion, false);
        raiz.transform.localPosition = posicion;

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(raiz.transform, false);
        tronco.transform.localScale = new Vector3(escala * 0.35f, escala, escala * 0.35f);

        MeshRenderer renderer = tronco.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = nombre == "Roca" ? new Color(0.45f, 0.45f, 0.45f, 1f) : new Color(0.3f, 0.2f, 0.1f, 1f);
            renderer.sharedMaterial = material;
        }
    }

    // Este metodo destruye objetos de forma segura en editor o play mode.
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

    // COPILOT-EXPAND: aqui podes sumar caminos, ruinas, terreno con relieve o biomas alternativos.
}
