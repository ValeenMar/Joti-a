using UnityEngine;
using System.Collections;

namespace RealmBrawl
{
    public class FeedbackPantalla : MonoBehaviour
    {
        float flashAlpha;
        Color flashColor = Color.white;

        void OnEnable()
        {
            Eventos.AlAplicarDanio += OnDanio;
            Eventos.AlSubirNivel += OnNivel;
        }

        void OnDisable()
        {
            Eventos.AlAplicarDanio -= OnDanio;
            Eventos.AlSubirNivel -= OnNivel;
        }

        void OnDanio(DatosDanio datos)
        {
            if (datos.esCritico)
            {
                flashColor = new Color(1f, 0.9f, 0.3f);
                flashAlpha = 0.3f;
                StartCoroutine(FreezeFrame(0.08f));
            }
            else
            {
                flashColor = new Color(1f, 0.3f, 0.3f);
                flashAlpha = 0.15f;
            }
        }

        void OnNivel(int _)
        {
            flashColor = new Color(1f, 1f, 0.3f);
            flashAlpha = 0.4f;
            StartCoroutine(FreezeFrame(0.1f));
        }

        IEnumerator FreezeFrame(float duracion)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duracion);
            if (GameManager.Instancia != null && GameManager.Instancia.Estado == EstadoJuego.Jugando)
                Time.timeScale = 1f;
        }

        void Update()
        {
            if (flashAlpha > 0f)
                flashAlpha -= Time.unscaledDeltaTime * 2f;
        }

        void OnGUI()
        {
            if (flashAlpha <= 0f) return;

            GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
