using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este componente runtime mantiene viva la configuracion de la capa del jugador
// usando Cloth y colliders simples sobre cadera y piernas.

// Esta clase sostiene referencias de la capa y revalida su configuracion tras reinicios o recargas.
public class ConstraintCapa : MonoBehaviour
{
    // Esta referencia apunta al Animator humanoide del jugador dueño de la capa.
    [SerializeField] private Animator animatorJugador;

    // Esta referencia apunta al renderer de piel de la capa.
    [SerializeField] private SkinnedMeshRenderer renderizadorCapa;

    // Esta referencia apunta al componente Cloth que simula la tela.
    [SerializeField] private Cloth clothCapa;

    // Estas referencias guardan los huesos principales usados por la tela.
    [SerializeField] private Transform huesoCadera;
    [SerializeField] private Transform huesoPiernaIzquierda;
    [SerializeField] private Transform huesoPiernaDerecha;

    // Estas referencias guardan los colliders que bloquean la tela para evitar clipping.
    [SerializeField] private CapsuleCollider colliderCadera;
    [SerializeField] private CapsuleCollider colliderPiernaIzquierda;
    [SerializeField] private CapsuleCollider colliderPiernaDerecha;

    // Esta variable evita reconfigurar la tela en cada frame sin necesidad.
    private bool configuracionAplicada;

    // Esta funcion se ejecuta al cargar el componente por primera vez.
    private void Awake()
    {
        // Intentamos buscar todas las referencias necesarias desde el inicio.
        ReasignarReferenciasSiHaceFalta();

        // Aplicamos la configuracion inicial de la capa.
        AplicarConfiguracionSiHaceFalta();
    }

    // Esta funcion se ejecuta al reactivar el componente tras un reload de escena.
    private void OnEnable()
    {
        // Reintentamos referencias por si Unity reconstruyo jerarquias.
        ReasignarReferenciasSiHaceFalta();

        // Reaplicamos la configuracion porque Cloth a veces pierde referencias tras reinicios.
        configuracionAplicada = false;
        AplicarConfiguracionSiHaceFalta();
    }

    // Esta funcion corre tarde para asegurar que el rig ya evaluo su pose del frame.
    private void LateUpdate()
    {
        // Si falta algun dato critico, lo reintentamos de forma segura.
        ReasignarReferenciasSiHaceFalta();

        // Si la configuracion se perdio o aun no se aplico, la restauramos.
        AplicarConfiguracionSiHaceFalta();
    }

    // Este metodo permite al setup de editor dejar todas las referencias conectadas de una vez.
    public void Configurar(
        Animator animatorConfig,
        SkinnedMeshRenderer rendererConfig,
        Cloth clothConfig,
        Transform caderaConfig,
        Transform piernaIzquierdaConfig,
        Transform piernaDerechaConfig,
        CapsuleCollider colliderCaderaConfig,
        CapsuleCollider colliderPiernaIzquierdaConfig,
        CapsuleCollider colliderPiernaDerechaConfig)
    {
        // Guardamos todas las referencias recibidas desde el setup.
        animatorJugador = animatorConfig;
        renderizadorCapa = rendererConfig;
        clothCapa = clothConfig;
        huesoCadera = caderaConfig;
        huesoPiernaIzquierda = piernaIzquierdaConfig;
        huesoPiernaDerecha = piernaDerechaConfig;
        colliderCadera = colliderCaderaConfig;
        colliderPiernaIzquierda = colliderPiernaIzquierdaConfig;
        colliderPiernaDerecha = colliderPiernaDerechaConfig;

        // Marcamos que debe revalidarse la configuracion en el siguiente ciclo.
        configuracionAplicada = false;
        AplicarConfiguracionSiHaceFalta();
    }

    // Este metodo busca de nuevo referencias si se perdieron por reload o duplicado de escena.
    private void ReasignarReferenciasSiHaceFalta()
    {
        // Si falta el renderer de capa, lo buscamos en este objeto o en hijos.
        if (renderizadorCapa == null)
        {
            renderizadorCapa = GetComponent<SkinnedMeshRenderer>();

            if (renderizadorCapa == null)
            {
                renderizadorCapa = GetComponentInChildren<SkinnedMeshRenderer>(true);
            }
        }

        // Si falta el Cloth, lo buscamos junto al renderer.
        if (clothCapa == null)
        {
            clothCapa = GetComponent<Cloth>();

            if (clothCapa == null && renderizadorCapa != null)
            {
                clothCapa = renderizadorCapa.GetComponent<Cloth>();
            }
        }

        // Si falta el Animator, lo buscamos hacia arriba porque la capa cuelga del modelo del jugador.
        if (animatorJugador == null)
        {
            animatorJugador = GetComponentInParent<Animator>();
        }

        // Si el Animator es humanoide, reacquirimos huesos por API segura.
        if (animatorJugador != null && animatorJugador.isHuman)
        {
            if (huesoCadera == null)
            {
                huesoCadera = animatorJugador.GetBoneTransform(HumanBodyBones.Hips);
            }

            if (huesoPiernaIzquierda == null)
            {
                huesoPiernaIzquierda = animatorJugador.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            }

            if (huesoPiernaDerecha == null)
            {
                huesoPiernaDerecha = animatorJugador.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            }
        }

        // Si algun collider se perdio, intentamos recuperarlo por nombre en los huesos esperados.
        if (colliderCadera == null && huesoCadera != null)
        {
            colliderCadera = huesoCadera.GetComponentInChildren<CapsuleCollider>(true);
        }

        if (colliderPiernaIzquierda == null && huesoPiernaIzquierda != null)
        {
            colliderPiernaIzquierda = huesoPiernaIzquierda.GetComponentInChildren<CapsuleCollider>(true);
        }

        if (colliderPiernaDerecha == null && huesoPiernaDerecha != null)
        {
            colliderPiernaDerecha = huesoPiernaDerecha.GetComponentInChildren<CapsuleCollider>(true);
        }
    }

    // Este metodo reconfigura el Cloth si alguna referencia critica existe y la tela aun no esta lista.
    private void AplicarConfiguracionSiHaceFalta()
    {
        // Si ya esta configurado y el Cloth sigue vivo, no hacemos trabajo extra.
        if (configuracionAplicada && clothCapa != null)
        {
            return;
        }

        // Si falta Cloth o renderer, no podemos seguir.
        if (clothCapa == null || renderizadorCapa == null)
        {
            return;
        }

        // Reforzamos propiedades estables y livianas para la capa.
        clothCapa.stretchingStiffness = 0.8f;
        clothCapa.bendingStiffness = 0.5f;
        clothCapa.damping = 0.1f;
        clothCapa.useGravity = true;
        // Unity 2022 ya no permite configurar self collision desde esta API.
        // Dejamos la tela sin autos colision adicional y resolvemos clipping con capsulas en piernas/cadera.
        clothCapa.friction = 0.5f;
        clothCapa.useTethers = true;
        clothCapa.enabled = true;

        // Construimos la lista real de colliders de piernas y cadera ignorando nulos.
        CapsuleCollider[] collidersConfigurados = ConstruirListaCapsulas();

        // Si encontramos colliders validos, los aplicamos a la tela.
        if (collidersConfigurados.Length > 0)
        {
            clothCapa.capsuleColliders = collidersConfigurados;
        }

        // Si el mesh existe, damos libertad razonable a la tela para que la parte baja pueda separarse de las piernas.
        ConfigurarCoeficientesTela();

        // Marcamos la configuracion como aplicada.
        configuracionAplicada = true;
    }

    // Este metodo arma un arreglo compacto con las capsulas vigentes para el Cloth.
    private CapsuleCollider[] ConstruirListaCapsulas()
    {
        // Contamos manualmente cuantas capsulas validas tenemos.
        int cantidad = 0;

        if (colliderCadera != null)
        {
            cantidad++;
        }

        if (colliderPiernaIzquierda != null)
        {
            cantidad++;
        }

        if (colliderPiernaDerecha != null)
        {
            cantidad++;
        }

        // Si no hay ninguna, devolvemos un arreglo vacio seguro.
        if (cantidad == 0)
        {
            return new CapsuleCollider[0];
        }

        // Construimos el arreglo final del tamaño exacto.
        CapsuleCollider[] colliders = new CapsuleCollider[cantidad];
        int indiceActual = 0;

        if (colliderCadera != null)
        {
            colliders[indiceActual] = colliderCadera;
            indiceActual++;
        }

        if (colliderPiernaIzquierda != null)
        {
            colliders[indiceActual] = colliderPiernaIzquierda;
            indiceActual++;
        }

        if (colliderPiernaDerecha != null)
        {
            colliders[indiceActual] = colliderPiernaDerecha;
        }

        // Devolvemos la lista compacta.
        return colliders;
    }

    // Este metodo ajusta coeficientes del cloth para que la parte alta quede anclada y la baja pueda moverse.
    private void ConfigurarCoeficientesTela()
    {
        // Si no hay mesh compartido, no podemos calcular coeficientes.
        if (renderizadorCapa.sharedMesh == null)
        {
            return;
        }

        // Tomamos todos los vertices del mesh base.
        Vector3[] vertices = renderizadorCapa.sharedMesh.vertices;

        // Si el mesh no tiene vertices, no seguimos.
        if (vertices == null || vertices.Length == 0)
        {
            return;
        }

        // Calculamos altura minima y maxima para detectar hombros y borde bajo.
        float alturaMinima = float.MaxValue;
        float alturaMaxima = float.MinValue;

        for (int indiceVertice = 0; indiceVertice < vertices.Length; indiceVertice++)
        {
            float alturaActual = vertices[indiceVertice].y;

            if (alturaActual < alturaMinima)
            {
                alturaMinima = alturaActual;
            }

            if (alturaActual > alturaMaxima)
            {
                alturaMaxima = alturaActual;
            }
        }

        // Si el rango es invalido, usamos un fallback uniforme seguro.
        if (Mathf.Abs(alturaMaxima - alturaMinima) < 0.0001f)
        {
            ClothSkinningCoefficient[] coeficientesUniformes = new ClothSkinningCoefficient[vertices.Length];

            for (int indiceCoeficiente = 0; indiceCoeficiente < coeficientesUniformes.Length; indiceCoeficiente++)
            {
                coeficientesUniformes[indiceCoeficiente].maxDistance = 0.12f;
                coeficientesUniformes[indiceCoeficiente].collisionSphereDistance = 0.02f;
            }

            clothCapa.coefficients = coeficientesUniformes;
            return;
        }

        // Armamos coeficientes con mayor anclaje arriba y mayor libertad abajo.
        ClothSkinningCoefficient[] coeficientes = new ClothSkinningCoefficient[vertices.Length];

        for (int indiceCoeficiente = 0; indiceCoeficiente < coeficientes.Length; indiceCoeficiente++)
        {
            // Normalizamos la altura del vertice entre 0 y 1.
            float alturaNormalizada = Mathf.InverseLerp(alturaMinima, alturaMaxima, vertices[indiceCoeficiente].y);

            // Las partes mas altas quedan casi fijas para no desprenderse de hombros/cuello.
            if (alturaNormalizada > 0.78f)
            {
                coeficientes[indiceCoeficiente].maxDistance = 0.01f;
            }
            else
            {
                // Las partes bajas tienen mas juego para no chocar con las piernas.
                float libertad = Mathf.Lerp(0.05f, 0.22f, 1f - alturaNormalizada);
                coeficientes[indiceCoeficiente].maxDistance = libertad;
            }

            // Dejamos una pequeña separacion de colision para que no se pegue al collider.
            coeficientes[indiceCoeficiente].collisionSphereDistance = 0.02f;
        }

        // Aplicamos los coeficientes al Cloth.
        clothCapa.coefficients = coeficientes;
    }
}
