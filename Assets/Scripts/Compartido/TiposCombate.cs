using UnityEngine;

// Este enum representa la zona del cuerpo que recibió el golpe.
public enum TipoZonaDanio
{
    // Golpe normal en el cuerpo.
    Cuerpo,

    // Golpe en la espalda que hace daño aumentado.
    Espalda,

    // Golpe en la cabeza que hace daño crítico.
    Cabeza
}

// Esta clase guarda toda la información importante de un golpe.
[System.Serializable]
public class DatosDanio
{
    // Este objeto es quien causó el daño.
    public GameObject atacante;

    // Este objeto es quien recibió el daño.
    public GameObject objetivo;

    // Este valor es el daño base antes de aplicar multiplicadores.
    public float danioBase;

    // Este valor multiplica el daño según la zona golpeada.
    public float multiplicadorZona = 1f;

    // Este valor indica qué parte del cuerpo fue golpeada.
    public TipoZonaDanio tipoZona = TipoZonaDanio.Cuerpo;

    // Este punto representa dónde ocurrió el impacto en el mundo.
    public Vector3 puntoImpacto;

    // Esta dirección representa desde dónde vino el golpe.
    public Vector3 direccionImpacto;

    // Este valor indica si el golpe que conecto era un golpe fuerte.
    public bool esGolpeFuerte;

    // Esta propiedad devuelve el daño final ya multiplicado por la zona.
    public float DanioFinalCalculado
    {
        get
        {
            // Calculamos el daño final multiplicando el daño base por el multiplicador de la zona.
            return danioBase * multiplicadorZona;
        }
    }

    // Esta propiedad indica si el golpe cuenta como crítico visualmente.
    public bool FueCritico
    {
        get
        {
            // Consideramos crítico cualquier golpe con multiplicador mayor a 1.
            return multiplicadorZona > 1f;
        }
    }
}

// Esta interfaz la implementan los objetos que pueden recibir daño.
public interface IRecibidorDanio
{
    // Este método permite aplicar un golpe a cualquier objetivo compatible.
    void RecibirDanio(DatosDanio datosDanio);
}
