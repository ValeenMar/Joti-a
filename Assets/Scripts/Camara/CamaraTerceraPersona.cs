using UnityEngine;

namespace RealmBrawl
{
    public class CamaraTerceraPersona : MonoBehaviour
    {
        [Header("Objetivo")]
        [SerializeField] Transform objetivo;
        [SerializeField] Vector3 offsetObjetivo = new Vector3(0f, 1.6f, 0f);

        [Header("Distancia")]
        [SerializeField] float distancia = 5f;
        [SerializeField] float distanciaMin = 2f;
        [SerializeField] float distanciaMax = 10f;
        [SerializeField] float velocidadZoom = 2f;

        [Header("Sensibilidad")]
        [SerializeField] float sensibilidadX = 3f;
        [SerializeField] float sensibilidadY = 2f;
        [SerializeField] float anguloMinY = -20f;
        [SerializeField] float anguloMaxY = 70f;

        [Header("Hombro")]
        [SerializeField] float offsetHombro = 0.6f;
        [SerializeField] KeyCode teclaHombro = KeyCode.Q;

        [Header("Colision")]
        [SerializeField] float radioColision = 0.3f;
        [SerializeField] LayerMask mascaraColision = ~0;

        float rotacionX;
        float rotacionY = 15f;
        float distanciaActual;
        bool hombroDerecho = true;
        float ladoHombro = 1f; // 1 = derecho, -1 = izquierdo

        void Start()
        {
            distanciaActual = distancia;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (objetivo == null)
            {
                var jugador = GameObject.FindGameObjectWithTag("Player");
                if (jugador != null) objetivo = jugador.transform;
            }
        }

        void LateUpdate()
        {
            if (objetivo == null) return;

            // Input del mouse
            rotacionX += Input.GetAxis("Mouse X") * sensibilidadX;
            rotacionY -= Input.GetAxis("Mouse Y") * sensibilidadY;
            rotacionY = Mathf.Clamp(rotacionY, anguloMinY, anguloMaxY);

            // Zoom con scroll
            distancia -= Input.GetAxis("Mouse ScrollWheel") * velocidadZoom;
            distancia = Mathf.Clamp(distancia, distanciaMin, distanciaMax);

            // Cambio de hombro
            if (Input.GetKeyDown(teclaHombro))
                hombroDerecho = !hombroDerecho;

            float ladoObjetivo = hombroDerecho ? 1f : -1f;
            ladoHombro = Mathf.Lerp(ladoHombro, ladoObjetivo, Time.deltaTime * 8f);

            // Calcular posicion
            Quaternion rotacion = Quaternion.Euler(rotacionY, rotacionX, 0f);
            Vector3 puntoObjetivo = objetivo.position + offsetObjetivo;
            Vector3 offsetLateral = rotacion * Vector3.right * offsetHombro * ladoHombro;
            Vector3 posicionDeseada = puntoObjetivo + offsetLateral - rotacion * Vector3.forward * distancia;

            // Colision con paredes
            distanciaActual = distancia;
            if (Physics.SphereCast(puntoObjetivo + offsetLateral, radioColision,
                (posicionDeseada - puntoObjetivo - offsetLateral).normalized,
                out RaycastHit hit, distancia, mascaraColision))
            {
                distanciaActual = hit.distance - 0.1f;
            }

            Vector3 posicionFinal = puntoObjetivo + offsetLateral - rotacion * Vector3.forward * distanciaActual;
            transform.position = posicionFinal;
            transform.LookAt(puntoObjetivo);
        }

        public void AsignarObjetivo(Transform nuevoObjetivo)
        {
            objetivo = nuevoObjetivo;
        }
    }
}
