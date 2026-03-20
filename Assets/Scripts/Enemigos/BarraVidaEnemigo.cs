using UnityEngine;

namespace RealmBrawl
{
    public class BarraVidaEnemigo : MonoBehaviour
    {
        [SerializeField] Vector3 offset = new Vector3(0f, 2.2f, 0f);

        EnemigoBase enemigo;
        float porcentajeMostrado = 1f;
        float timerMostrar;
        bool muerto;

        Camera cam;

        void Awake()
        {
            enemigo = GetComponent<EnemigoBase>();
        }

        void OnEnable()
        {
            if (enemigo != null)
            {
                enemigo.AlCambiarVida += OnCambiarVida;
                enemigo.AlMorir += OnMorir;
            }
        }

        void OnDisable()
        {
            if (enemigo != null)
            {
                enemigo.AlCambiarVida -= OnCambiarVida;
                enemigo.AlMorir -= OnMorir;
            }
        }

        void Start()
        {
            cam = Camera.main;
        }

        void OnCambiarVida(float porcentaje)
        {
            porcentajeMostrado = porcentaje;
            timerMostrar = 3f;
        }

        void OnMorir()
        {
            muerto = true;
        }

        void Update()
        {
            if (timerMostrar > 0f)
                timerMostrar -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (muerto || timerMostrar <= 0f) return;
            if (cam == null) { cam = Camera.main; if (cam == null) return; }

            Vector3 posWorld = transform.position + offset;
            Vector3 posScreen = cam.WorldToScreenPoint(posWorld);

            if (posScreen.z < 0) return;

            float barraAncho = 60f;
            float barraAlto = 8f;
            float x = posScreen.x - barraAncho * 0.5f;
            float y = Screen.height - posScreen.y - barraAlto * 0.5f;

            // Fondo
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, barraAncho, barraAlto), Texture2D.whiteTexture);

            // Vida
            Color colorVida = Color.Lerp(Color.red, Color.green, porcentajeMostrado);
            GUI.color = colorVida;
            GUI.DrawTexture(new Rect(x, y, barraAncho * porcentajeMostrado, barraAlto), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }
}
