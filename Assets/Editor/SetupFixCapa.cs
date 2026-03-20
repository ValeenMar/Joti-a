using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor busca la capa del jugador y le aplica una solucion
// practica con Cloth y colliders simples en cadera y piernas.

// Esta clase expone un menu para corregir automaticamente la capa del jugador.
public static class SetupFixCapa
{
    // Este nombre define el menu de editor pedido para la solucion de la capa.
    [MenuItem("Realm Brawl/Setup/Fix Capa")]
    public static void EjecutarFixCapa()
    {
        // Buscamos el jugador principal de la escena.
        GameObject jugador = GameObject.Find("Jugador");
        if (jugador == null)
        {
            Debug.LogWarning("[SetupFixCapa] No se encontro el objeto 'Jugador' en la escena.");
            EditorUtility.DisplayDialog("Fix Capa", "No se encontro 'Jugador' en la escena abierta.", "OK");
            return;
        }

        // Buscamos el Animator humanoide visual dentro del modelo del jugador.
        Animator animatorJugador = BuscarAnimatorHumanoide(jugador.transform);
        if (animatorJugador == null)
        {
            Debug.LogWarning("[SetupFixCapa] No se encontro un Animator humanoide valido dentro del jugador.");
            EditorUtility.DisplayDialog("Fix Capa", "No se encontro un Animator humanoide dentro de 'Jugador'.", "OK");
            return;
        }

        // Buscamos el renderer de la capa por nombres comunes.
        SkinnedMeshRenderer rendererCapa = BuscarRendererCapa(jugador.transform);
        if (rendererCapa == null)
        {
            Debug.LogWarning("[SetupFixCapa] No se encontro una capa compatible en los hijos del jugador.");
            EditorUtility.DisplayDialog("Fix Capa", "No se encontro un SkinnedMeshRenderer de capa (cape/capa/cloth/cloak).", "OK");
            return;
        }

        // Buscamos los huesos base de cadera y piernas.
        Transform huesoCadera = animatorJugador.GetBoneTransform(HumanBodyBones.Hips);
        Transform huesoPiernaIzquierda = animatorJugador.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform huesoPiernaDerecha = animatorJugador.GetBoneTransform(HumanBodyBones.RightUpperLeg);

        if (huesoCadera == null || huesoPiernaIzquierda == null || huesoPiernaDerecha == null)
        {
            Debug.LogWarning("[SetupFixCapa] El Animator no expone huesos humanoides suficientes para configurar la capa.");
            EditorUtility.DisplayDialog("Fix Capa", "Faltan huesos humanoides (Hips/LeftUpperLeg/RightUpperLeg).", "OK");
            return;
        }

        // Creamos o recuperamos colliders simples sobre cadera y piernas.
        CapsuleCollider colliderCadera = ObtenerOCrearCapsula(huesoCadera, "ColisionCapaCadera", 0.15f, 0.24f, new Vector3(0f, -0.02f, -0.02f));
        CapsuleCollider colliderPiernaIzquierda = ObtenerOCrearCapsula(huesoPiernaIzquierda, "ColisionCapaPiernaIzquierda", 0.12f, 0.26f, new Vector3(0f, -0.10f, 0f));
        CapsuleCollider colliderPiernaDerecha = ObtenerOCrearCapsula(huesoPiernaDerecha, "ColisionCapaPiernaDerecha", 0.12f, 0.26f, new Vector3(0f, -0.10f, 0f));

        // Recuperamos o agregamos el Cloth directamente sobre el renderer de la capa.
        Cloth clothCapa = rendererCapa.GetComponent<Cloth>();
        if (clothCapa == null)
        {
            clothCapa = rendererCapa.gameObject.AddComponent<Cloth>();
        }

        // Ajustamos el Cloth con valores seguros y performantes.
        clothCapa.stretchingStiffness = 0.8f;
        clothCapa.bendingStiffness = 0.5f;
        clothCapa.damping = 0.1f;
        clothCapa.useGravity = true;
        // Unity 2022 ya no deja escribir self collision en Cloth.
        // La solucion de esta capa se apoya en colliders simples de piernas y cadera.
        clothCapa.friction = 0.5f;
        clothCapa.useTethers = true;
        clothCapa.capsuleColliders = new[] { colliderCadera, colliderPiernaIzquierda, colliderPiernaDerecha };
        clothCapa.enabled = true;

        // Agregamos el componente runtime que mantiene la configuracion viva.
        ConstraintCapa constraintCapa = rendererCapa.GetComponent<ConstraintCapa>();
        if (constraintCapa == null)
        {
            constraintCapa = rendererCapa.gameObject.AddComponent<ConstraintCapa>();
        }

        // Le pasamos todas las referencias al componente runtime.
        constraintCapa.Configurar(
            animatorJugador,
            rendererCapa,
            clothCapa,
            huesoCadera,
            huesoPiernaIzquierda,
            huesoPiernaDerecha,
            colliderCadera,
            colliderPiernaIzquierda,
            colliderPiernaDerecha);

        // Marcamos todo como modificado para que Unity lo guarde en la escena.
        EditorUtility.SetDirty(rendererCapa.gameObject);
        EditorUtility.SetDirty(rendererCapa);
        EditorUtility.SetDirty(clothCapa);
        EditorUtility.SetDirty(constraintCapa);
        EditorUtility.SetDirty(jugador);
        EditorSceneManager.MarkSceneDirty(jugador.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupFixCapa] Capa configurada con Cloth y colision simple de piernas.");
        EditorUtility.DisplayDialog("Fix Capa", "Se configuro la capa con Cloth y colision simple en piernas.", "OK");
    }

    // Este metodo busca el Animator humanoide mas util dentro de la jerarquia del jugador.
    private static Animator BuscarAnimatorHumanoide(Transform raiz)
    {
        Animator[] animators = raiz.GetComponentsInChildren<Animator>(true);
        for (int indiceAnimator = 0; indiceAnimator < animators.Length; indiceAnimator++)
        {
            if (animators[indiceAnimator] == null)
            {
                continue;
            }

            if (!animators[indiceAnimator].isHuman)
            {
                continue;
            }

            if (animators[indiceAnimator].GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                continue;
            }

            return animators[indiceAnimator];
        }

        return raiz.GetComponentInChildren<Animator>(true);
    }

    // Este metodo busca un renderer de capa por nombre dentro del modelo del jugador.
    private static SkinnedMeshRenderer BuscarRendererCapa(Transform raiz)
    {
        SkinnedMeshRenderer[] renderers = raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int indiceRenderer = 0; indiceRenderer < renderers.Length; indiceRenderer++)
        {
            if (renderers[indiceRenderer] == null)
            {
                continue;
            }

            string nombre = renderers[indiceRenderer].name.ToLowerInvariant();

            if (nombre.Contains("cape") || nombre.Contains("capa") || nombre.Contains("cloth") || nombre.Contains("cloak"))
            {
                return renderers[indiceRenderer];
            }
        }

        return null;
    }

    // Este metodo crea o reutiliza una capsula simple como obstaculo para la tela.
    private static CapsuleCollider ObtenerOCrearCapsula(Transform huesoPadre, string nombreCollider, float radio, float altura, Vector3 centroLocal)
    {
        Transform hijoExistente = huesoPadre.Find(nombreCollider);
        GameObject objetoCollider;

        if (hijoExistente != null)
        {
            objetoCollider = hijoExistente.gameObject;
        }
        else
        {
            objetoCollider = new GameObject(nombreCollider);
            objetoCollider.transform.SetParent(huesoPadre, false);
        }

        objetoCollider.transform.localPosition = centroLocal;
        objetoCollider.transform.localRotation = Quaternion.identity;
        objetoCollider.transform.localScale = Vector3.one;

        CapsuleCollider capsula = objetoCollider.GetComponent<CapsuleCollider>();
        if (capsula == null)
        {
            capsula = objetoCollider.AddComponent<CapsuleCollider>();
        }

        capsula.isTrigger = false;
        capsula.radius = radio;
        capsula.height = Mathf.Max(altura, radio * 2f);
        capsula.center = Vector3.zero;
        capsula.direction = 1;

        return capsula;
    }
}
