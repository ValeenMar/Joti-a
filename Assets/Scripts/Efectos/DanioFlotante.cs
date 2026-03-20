using UnityEngine;
using System.Collections.Generic;

namespace RealmBrawl
{
    public class DanioFlotante : MonoBehaviour
    {
        struct TextoDanio
        {
            public Vector3 posicion;
            public float cantidad;
            public bool critico;
            public float tiempoCreacion;
        }

        List<TextoDanio> textos = new List<TextoDanio>();
        const float DURACION = 1.2f;
        Camera cam;

        void OnEnable()
        {
            Eventos.AlAplicarDanio += OnDanio;
        }

        void OnDisable()
        {
            Eventos.AlAplicarDanio -= OnDanio;
        }

        void Start()
        {
            cam = Camera.main;
        }

        void OnDanio(DatosDanio datos)
        {
            // Agregar offset random para que no se superpongan
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.3f), 0f);
            textos.Add(new TextoDanio
            {
                posicion = datos.puntoImpacto + offset,
                cantidad = datos.cantidad,
                critico = datos.esCritico,
                tiempoCreacion = Time.time
            });
        }

        void Update()
        {
            textos.RemoveAll(t => Time.time - t.tiempoCreacion > DURACION);
        }

        void OnGUI()
        {
            if (cam == null) { cam = Camera.main; if (cam == null) return; }

            foreach (var t in textos)
            {
                float edad = Time.time - t.tiempoCreacion;
                float progreso = edad / DURACION;

                // Sube y se desvanece
                Vector3 pos = t.posicion + Vector3.up * progreso * 1.5f;
                Vector3 screen = cam.WorldToScreenPoint(pos);
                if (screen.z < 0) continue;

                float alpha = 1f - progreso;

                GUIStyle estilo = new GUIStyle(GUI.skin.label);
                estilo.fontStyle = FontStyle.Bold;
                estilo.alignment = TextAnchor.MiddleCenter;

                if (t.critico)
                {
                    estilo.fontSize = 24;
                    GUI.color = new Color(1f, 0.85f, 0f, alpha); // oro
                }
                else
                {
                    estilo.fontSize = 18;
                    GUI.color = new Color(1f, 1f, 1f, alpha);
                }

                float sx = screen.x;
                float sy = Screen.height - screen.y;

                // Sombra
                GUI.color = new Color(0, 0, 0, alpha * 0.5f);
                GUI.Label(new Rect(sx - 49, sy - 14, 100, 30), Mathf.RoundToInt(t.cantidad).ToString(), estilo);

                // Texto
                GUI.color = t.critico ? new Color(1f, 0.85f, 0f, alpha) : new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(sx - 50, sy - 15, 100, 30), Mathf.RoundToInt(t.cantidad).ToString(), estilo);
            }

            GUI.color = Color.white;
        }
    }
}
