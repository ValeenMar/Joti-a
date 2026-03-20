using UnityEngine;

namespace RealmBrawl
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class MovimientoJugador : MonoBehaviour
    {
        [Header("Velocidad")]
        [SerializeField] float velocidadBase = 5f;
        [SerializeField] float multiplicadorSprint = 1.6f;
        [SerializeField] float penalizacionAgotado = 0.6f;

        [Header("Rotacion")]
        [SerializeField] float velocidadRotacion = 10f;

        Rigidbody rb;
        Animator animator;
        Estamina estamina;

        Vector3 direccionMovimiento;
        bool quiereSprint;
        bool muerto;

        // Cache de Animator hashes
        static readonly int hashVelocidad = Animator.StringToHash("Velocidad");
        static readonly int hashSprint = Animator.StringToHash("Sprint");

        // Nivel de stats
        int nivelActual = 1;
        float bonusVelocidadPorNivel = 0.3f;

        public float VelocidadActual
        {
            get
            {
                float vel = velocidadBase + (nivelActual - 1) * bonusVelocidadPorNivel;
                if (quiereSprint && estamina != null && !estamina.EstaAgotado && estamina.TieneEstamina)
                    vel *= multiplicadorSprint;
                else if (estamina != null && estamina.EstaAgotado)
                    vel *= penalizacionAgotado;
                return vel;
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            animator = GetComponentInChildren<Animator>();
            estamina = GetComponent<Estamina>();
        }

        void OnEnable()
        {
            Eventos.AlSubirNivel += OnSubirNivel;
            Eventos.AlMorirJugador += OnMorir;
        }

        void OnDisable()
        {
            Eventos.AlSubirNivel -= OnSubirNivel;
            Eventos.AlMorirJugador -= OnMorir;
        }

        void Update()
        {
            if (muerto) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            quiereSprint = Input.GetKey(KeyCode.LeftShift);

            // Movimiento relativo a la camara
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam != null)
            {
                Vector3 adelante = cam.forward;
                Vector3 derecha = cam.right;
                adelante.y = 0f;
                derecha.y = 0f;
                adelante.Normalize();
                derecha.Normalize();
                direccionMovimiento = (adelante * v + derecha * h).normalized;
            }
            else
            {
                direccionMovimiento = new Vector3(h, 0f, v).normalized;
            }

            // Consumir stamina al sprintar
            if (quiereSprint && direccionMovimiento.sqrMagnitude > 0.01f && estamina != null)
                estamina.ConsumirSprint(Time.deltaTime);

            // Animacion
            if (animator != null)
            {
                float velNorm = direccionMovimiento.sqrMagnitude > 0.01f ?
                    (quiereSprint && estamina != null && !estamina.EstaAgotado ? 1f : 0.5f) : 0f;
                animator.SetFloat(hashVelocidad, velNorm, 0.1f, Time.deltaTime);
                animator.SetBool(hashSprint, quiereSprint && estamina != null && !estamina.EstaAgotado);
            }
        }

        void FixedUpdate()
        {
            if (muerto) return;
            if (direccionMovimiento.sqrMagnitude < 0.01f) return;

            // Mover
            Vector3 movimiento = direccionMovimiento * VelocidadActual * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movimiento);

            // Rotar hacia la direccion de movimiento
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            rb.rotation = Quaternion.Slerp(rb.rotation, rotacionObjetivo, velocidadRotacion * Time.fixedDeltaTime);
        }

        void OnSubirNivel(int nivel)
        {
            nivelActual = nivel;
        }

        void OnMorir()
        {
            muerto = true;
            direccionMovimiento = Vector3.zero;
            if (animator != null)
                animator.SetFloat(hashVelocidad, 0f);
        }

        public void Revivir()
        {
            muerto = false;
        }
    }
}
