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

    // Esta funcion se ejecuta en el editor cuando cambian valores del inspector.
    private void OnValidate()
    {
        // Evitamos valores invalidos menores o iguales a cero cuando es manual.
        if (multiplicadorManual != -1f && multiplicadorManual <= 0f)
        {
            multiplicadorManual = 1f;
        }
    }
}

