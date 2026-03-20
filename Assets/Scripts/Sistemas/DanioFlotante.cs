using System.Collections;
using UnityEngine;

// Esta clase crea numeros flotantes en el punto donde impacta un golpe.
public class DanioFlotante : MonoBehaviour
{
    // Esta referencia guarda la camara que mira los numeros.
    [SerializeField] private Camera camaraPrincipal;

    // Este valor define cuanto sube el numero por segundo.
    [SerializeField] private float velocidadAscenso = 1.6f;

    // Este valor define cuanto dura visible cada numero.
    [SerializeField] private float duracionVisible = 0.8f;

    // Este valor define una pequena dispersion horizontal para que no se encimen.
    [SerializeField] private float dispersionHorizontal = 0.25f;

    // Este offset separa el numero un poco del punto exacto de impacto.
    [SerializeField] private Vector3 offsetMundo = new Vector3(0f, 0.25f, 0f);

    // Este color se usa para golpes normales.
    [SerializeField] private Color colorDanioNormal = Color.white;

    // Este color se usa para golpes criticos.
    [SerializeField] private Color colorDanioCritico = new Color(1f, 0.85f, 0.2f);

    // Este color se usa para los golpes de espalda.
    [SerializeField] private Color colorDanioEspalda = new Color(1f, 0.55f, 0.18f, 1f);

    // Este tamano de fuente se usa para danio normal.
    [SerializeField] private int tamanoFuenteNormal = 48;

    // Este tamano de fuente se usa para danio critico.
    [SerializeField] private int tamanoFuenteCritico = 64;

    // Esta funcion se ejecuta cuando se activa el componente.
    private void OnEnable()
    {
        // Nos suscribimos al evento de danio aplicado.
        EventosJuego.AlAplicarDanio += ManejarDanioAplicado;
    }

    // Esta funcion se ejecuta cuando se desactiva el componente.
    private void OnDisable()
    {
        // Nos desuscribimos para evitar referencias invalidas.
        EventosJuego.AlAplicarDanio -= ManejarDanioAplicado;
    }

    // Este metodo procesa los datos de danio para crear un numero flotante.
    private void ManejarDanioAplicado(DatosDanio datosDanio)
    {
        // Si no hay camara asignada, intentamos tomar la principal de la escena.
        if (camaraPrincipal == null)
        {
            // Buscamos la camara principal una sola vez cuando hace falta.
            camaraPrincipal = Camera.main;
        }

        // Si no encontramos camara, no podremos orientar el texto correctamente.
        if (camaraPrincipal == null)
        {
            // Cortamos para evitar errores de null reference.
            return;
        }

        // Definimos la posicion inicial usando el punto de impacto.
        Vector3 posicionInicial = datosDanio.puntoImpacto + offsetMundo;

        // Agregamos una variacion horizontal aleatoria para separar numeros simultaneos.
        posicionInicial += new Vector3(Random.Range(-dispersionHorizontal, dispersionHorizontal), 0f, Random.Range(-dispersionHorizontal, dispersionHorizontal));

        // Creamos un objeto vacio que contendra el texto flotante.
        GameObject objetoDanio = new GameObject("DanioFlotante_" + Mathf.RoundToInt(datosDanio.DanioFinalCalculado));

        // Ubicamos el objeto en el mundo.
        objetoDanio.transform.position = posicionInicial;

        // Agregamos un TextMesh para renderizar texto 3D simple sin prefabs extra.
        TextMesh texto = objetoDanio.AddComponent<TextMesh>();

        // Mostramos el valor de danio redondeado.
        texto.text = ConstruirTextoDanio(datosDanio);

        // Centramos el ancla para mejor lectura.
        texto.anchor = TextAnchor.MiddleCenter;

        // Alineamos al centro.
        texto.alignment = TextAlignment.Center;

        // Si fue critico usamos tamano mayor, si no usamos normal.
        texto.fontSize = ObtenerTamanoFuente(datosDanio);

        // Si fue critico usamos color dorado, si no blanco.
        texto.color = ObtenerColorDanio(datosDanio);

        // Ajustamos el tamano en mundo para que no quede gigante.
        texto.characterSize = 0.05f;

        // Si fue critico aumentamos levemente la escala para dar impacto visual.
        objetoDanio.transform.localScale = datosDanio.FueCritico ? Vector3.one * 1.2f : Vector3.one;

        // Iniciamos animacion de ascenso y desvanecimiento.
        StartCoroutine(CorrutinaAnimarDanio(objetoDanio.transform, texto, datosDanio.FueCritico));
    }

    // Esta corrutina anima el numero flotante hasta destruirlo.
    private IEnumerator CorrutinaAnimarDanio(Transform transformDanio, TextMesh texto, bool esCritico)
    {
        // Guardamos el color base para cambiar solo el alpha.
        Color colorBase = texto.color;

        // Guardamos una escala inicial para efecto pop en criticos.
        Vector3 escalaInicial = transformDanio.localScale;

        // Iniciamos contador de tiempo local.
        float tiempo = 0f;

        // Mientras no termine la duracion, seguimos animando.
        while (tiempo < duracionVisible)
        {
            // Sumamos tiempo normal para acompanar el ritmo del juego.
            tiempo += Time.deltaTime;

            // Calculamos progreso de 0 a 1.
            float progreso = Mathf.Clamp01(tiempo / duracionVisible);

            // Movemos hacia arriba de forma constante.
            transformDanio.position += Vector3.up * (velocidadAscenso * Time.deltaTime);

            // Hacemos que el texto mire siempre a la camara.
            transformDanio.forward = camaraPrincipal.transform.forward;

            // Si es critico, aplicamos un pequeno pulso de escala al inicio.
            if (esCritico)
            {
                // Curva de pulso que cae suavemente.
                float pulso = 1f + Mathf.Sin(progreso * Mathf.PI) * 0.15f;

                // Aplicamos la escala final del frame.
                transformDanio.localScale = escalaInicial * pulso;
            }

            // Reducimos alpha a medida que avanza el tiempo.
            float alpha = 1f - progreso;

            // Aplicamos color con alpha actualizado.
            texto.color = new Color(colorBase.r, colorBase.g, colorBase.b, alpha);

            // Esperamos al frame siguiente.
            yield return null;
        }

        // Destruimos el objeto para liberar memoria visual.
        if (transformDanio != null)
        {
            // Eliminamos el GameObject del texto flotante.
            Destroy(transformDanio.gameObject);
        }
    }

    // Este metodo arma un texto mas informativo segun la zona golpeada.
    private string ConstruirTextoDanio(DatosDanio datosDanio)
    {
        // Arrancamos con el valor del dano redondeado.
        string valorDanio = Mathf.RoundToInt(datosDanio.DanioFinalCalculado).ToString();

        // Si fue cabeza, lo marcamos explicitamente.
        if (datosDanio.tipoZona == TipoZonaDanio.Cabeza)
        {
            return valorDanio + " CABEZA";
        }

        // Si fue espalda, tambien lo contamos.
        if (datosDanio.tipoZona == TipoZonaDanio.Espalda)
        {
            return valorDanio + " ESPALDA";
        }

        // Para cuerpo dejamos solo el numero para no ensuciar demasiado.
        return valorDanio;
    }

    // Este metodo elige el color segun el tipo de impacto.
    private Color ObtenerColorDanio(DatosDanio datosDanio)
    {
        // Cabeza usa el color critico.
        if (datosDanio.tipoZona == TipoZonaDanio.Cabeza)
        {
            return colorDanioCritico;
        }

        // Espalda usa un naranja distinto.
        if (datosDanio.tipoZona == TipoZonaDanio.Espalda)
        {
            return colorDanioEspalda;
        }

        // Cuerpo queda blanco.
        return colorDanioNormal;
    }

    // Este metodo define el tamano de fuente segun la importancia de la zona.
    private int ObtenerTamanoFuente(DatosDanio datosDanio)
    {
        // Cabeza resalta mas.
        if (datosDanio.tipoZona == TipoZonaDanio.Cabeza)
        {
            return tamanoFuenteCritico;
        }

        // Espalda queda a mitad de camino entre normal y critico.
        if (datosDanio.tipoZona == TipoZonaDanio.Espalda)
        {
            return Mathf.RoundToInt(Mathf.Lerp(tamanoFuenteNormal, tamanoFuenteCritico, 0.45f));
        }

        // Cuerpo queda normal.
        return tamanoFuenteNormal;
    }
}
