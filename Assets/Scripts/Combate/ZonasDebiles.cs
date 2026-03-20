using UnityEngine;

// Esta clase marca una zona del cuerpo para multiplicar el danio recibido.
[RequireComponent(typeof(Collider))]
public class ZonasDebiles : MonoBehaviour
{
    // Esta variable define que tipo de zona representa este collider.
    [SerializeField] private TipoZonaDanio tipoZona = TipoZonaDanio.Cuerpo;

    // Esta variable permite un multiplicador manual opcional.
    // Si queda en -1, se usa el valor por defecto segun la zona.
    [SerializeField] private float multiplicadorManual = -1f;

    // Esta propiedad publica deja leer la zona desde otros scripts.
    public TipoZonaDanio TipoZona => tipoZona;

    // Esta funcion devuelve el multiplicador final para esta zona.
    public float ObtenerMultiplicador()
    {
        // Si el diseniador puso un valor manual valido, usamos ese valor.
        if (multiplicadorManual > 0f)
        {
            return multiplicadorManual;
        }

        // Si no hay valor manual, usamos el multiplicador por defecto.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                return 3f;

            case TipoZonaDanio.Espalda:
                return 2f;

            default:
                return 1f;
        }
    }

    // Esta funcion devuelve una etiqueta corta y humana para usar en UI y debug.
    public string ObtenerEtiquetaZona()
    {
        // Traducimos el enum a una palabra simple.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                return "CABEZA";

            case TipoZonaDanio.Espalda:
                return "ESPALDA";

            default:
                return "CUERPO";
        }
    }

    // Esta funcion se ejecuta en el editor cuando cambian valores del inspector.
    private void OnValidate()
    {
        // Evitamos valores invalidos menores o iguales a cero cuando es manual.
        if (multiplicadorManual != -1f && multiplicadorManual <= 0f)
        {
            multiplicadorManual = 1f;
        }
    }

    // Este metodo dibuja gizmos para ver facil en el editor donde esta cada zona real del enemigo.
    private void OnDrawGizmosSelected()
    {
        // Elegimos un color por zona para reconocerla rapido.
        switch (tipoZona)
        {
            case TipoZonaDanio.Cabeza:
                Gizmos.color = new Color(1f, 0.88f, 0.25f, 0.7f);
                break;

            case TipoZonaDanio.Espalda:
                Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.7f);
                break;

            default:
                Gizmos.color = new Color(0.9f, 0.95f, 1f, 0.45f);
                break;
        }

        // Dibujamos una esfera simple de referencia en la posicion de esta zona.
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.22f);
    }
}
