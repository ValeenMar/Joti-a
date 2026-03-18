using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script marca un punto de patrulla del enemigo y permite
// configurar tiempos de espera y gizmos visuales.

// Esta clase representa un punto de patrulla simple para enemigos.
public class PuntoPatrulla : MonoBehaviour
{
    // Este valor permite personalizar cuánto espera el enemigo en este punto.
    [SerializeField] private float tiempoEsperaPersonalizado = -1f;

    // Este radio define el tamaño del gizmo en escena.
    [SerializeField] private float radioGizmo = 0.25f;

    // Este color se usa para dibujar el punto en el editor.
    [SerializeField] private Color colorGizmo = new Color(0.2f, 0.85f, 1f, 1f);

    // Esta propiedad expone la posición del punto en el mundo.
    public Vector3 Posicion => transform.position;

    // Este metodo devuelve el tiempo de espera real usando el valor local o uno por defecto.
    public float ObtenerTiempoEspera(float tiempoEsperaPorDefecto)
    {
        // Si el tiempo personalizado es válido, devolvemos ese.
        if (tiempoEsperaPersonalizado >= 0f)
        {
            return tiempoEsperaPersonalizado;
        }

        // Si no, devolvemos el valor por defecto pasado por el enemigo.
        return tiempoEsperaPorDefecto;
    }

    // Esta funcion dibuja una referencia visual del punto en la escena.
    private void OnDrawGizmos()
    {
        // Usamos el color configurado para el gizmo.
        Gizmos.color = colorGizmo;

        // Dibujamos una esfera donde esta este punto.
        Gizmos.DrawSphere(transform.position, radioGizmo);
    }

    // COPILOT-EXPAND: Aqui podes agregar tipos de punto, acciones especiales y eventos contextuales por waypoint.
}
