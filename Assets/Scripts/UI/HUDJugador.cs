using UnityEngine;

namespace RealmBrawl
{
    public class HUDJugador : MonoBehaviour
    {
        // Valores para dibujar
        float vidaPorcentaje = 1f;
        float estaminaPorcentaje = 1f;
        int oleadaActual;
        int kills;
        int nivel = 1;
        float xpActual;
        float xpRequerida = 100f;
        int rachaActual;
        float rachaTimer;
        string mensajeRacha = "";
        Color colorRacha = Color.white;

        // Inventario
        Inventario inventario;

        void OnEnable()
        {
            Eventos.AlCambiarVidaJugador += (act, max) => vidaPorcentaje = act / max;
            Eventos.AlCambiarEstamina += (act, max) => estaminaPorcentaje = act / max;
            Eventos.AlIniciarOleada += o => oleadaActual = o;
            Eventos.AlMatarEnemigo += _ => kills++;
            Eventos.AlCambiarXP += (act, req, niv) => { xpActual = act; xpRequerida = req; nivel = niv; };
            Eventos.AlCambiarRacha += OnRacha;
        }

        void Start()
        {
            inventario = FindObjectOfType<Inventario>();
        }

        void OnRacha(int racha)
        {
            rachaActual = racha;
            if (racha >= 10)
            {
                mensajeRacha = $"LEGENDARIA x{racha}";
                colorRacha = new Color(1f, 0.85f, 0f); // oro
                rachaTimer = 2f;
            }
            else if (racha >= 5)
            {
                mensajeRacha = $"IMPLACABLE x{racha}";
                colorRacha = new Color(1f, 0.5f, 0f); // naranja
                rachaTimer = 2f;
            }
            else if (racha >= 3)
            {
                mensajeRacha = $"RACHA x{racha}";
                colorRacha = Color.white;
                rachaTimer = 2f;
            }
        }

        void Update()
        {
            if (rachaTimer > 0f)
                rachaTimer -= Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            // === BARRA DE VIDA ===
            DibujarBarra(20, 20, 200, 20, vidaPorcentaje, Color.red, "VIDA");

            // === BARRA DE ESTAMINA ===
            DibujarBarra(20, 48, 200, 14, estaminaPorcentaje, new Color(0.2f, 0.6f, 1f), "STAMINA");

            // === BARRA DE XP ===
            float xpPct = xpRequerida > 0 ? xpActual / xpRequerida : 0f;
            DibujarBarra(20, 70, 200, 10, xpPct, new Color(0.5f, 0f, 1f), $"Nivel {nivel}");

            // === INFO OLEADA ===
            GUIStyle estiloInfo = new GUIStyle(GUI.skin.label);
            estiloInfo.fontSize = 16;
            estiloInfo.fontStyle = FontStyle.Bold;
            estiloInfo.normal.textColor = Color.white;
            estiloInfo.alignment = TextAnchor.UpperRight;

            GUI.Label(new Rect(Screen.width - 220, 20, 200, 30), $"Oleada: {oleadaActual}", estiloInfo);
            GUI.Label(new Rect(Screen.width - 220, 45, 200, 30), $"Kills: {kills}", estiloInfo);

            // === RACHA ===
            if (rachaTimer > 0f && rachaActual >= 3)
            {
                GUIStyle estiloRacha = new GUIStyle(GUI.skin.label);
                estiloRacha.fontSize = 28;
                estiloRacha.fontStyle = FontStyle.Bold;
                estiloRacha.normal.textColor = colorRacha;
                estiloRacha.alignment = TextAnchor.UpperCenter;

                float alpha = Mathf.Min(1f, rachaTimer);
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.Label(new Rect(Screen.width * 0.5f - 150, 100, 300, 50), mensajeRacha, estiloRacha);
                GUI.color = Color.white;
            }

            // === INVENTARIO (abajo centro) ===
            DibujarInventario();

            // === CROSSHAIR ===
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.color = new Color(1, 1, 1, 0.7f);
            GUI.DrawTexture(new Rect(cx - 1, cy - 10, 2, 20), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 10, cy - 1, 20, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DibujarBarra(float x, float y, float ancho, float alto, float porcentaje, Color color, string texto)
        {
            // Fondo
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, ancho, alto), Texture2D.whiteTexture);

            // Relleno
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, ancho * Mathf.Clamp01(porcentaje), alto), Texture2D.whiteTexture);

            // Texto
            GUI.color = Color.white;
            GUIStyle estilo = new GUIStyle(GUI.skin.label);
            estilo.fontSize = Mathf.FloorToInt(alto * 0.7f);
            estilo.alignment = TextAnchor.MiddleCenter;
            estilo.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(x, y, ancho, alto), texto, estilo);
        }

        void DibujarInventario()
        {
            if (inventario == null) return;

            float slotSize = 50f;
            float spacing = 5f;
            int slots = inventario.CantidadSlots;
            float totalWidth = slots * slotSize + (slots - 1) * spacing;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - slotSize - 20f;

            for (int i = 0; i < slots; i++)
            {
                float x = startX + i * (slotSize + spacing);
                var item = inventario.ObtenerItem(i);
                int cantidad = inventario.ObtenerCantidad(i);

                // Fondo del slot
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
                GUI.DrawTexture(new Rect(x, startY, slotSize, slotSize), Texture2D.whiteTexture);

                // Borde
                GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                GUI.DrawTexture(new Rect(x, startY, slotSize, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x, startY + slotSize - 2, slotSize, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x, startY, 2, slotSize), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + slotSize - 2, startY, 2, slotSize), Texture2D.whiteTexture);

                // Numero de tecla
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUIStyle numStyle = new GUIStyle(GUI.skin.label);
                numStyle.fontSize = 10;
                numStyle.alignment = TextAnchor.UpperLeft;
                GUI.Label(new Rect(x + 4, startY + 2, 20, 15), (i + 1).ToString(), numStyle);

                if (item != null && cantidad > 0)
                {
                    // Color segun tipo
                    GUI.color = item.tipo == ItemData.TipoItem.Pocion ? new Color(0.2f, 0.8f, 0.2f, 1f) : Color.white;

                    GUIStyle itemStyle = new GUIStyle(GUI.skin.label);
                    itemStyle.fontSize = 20;
                    itemStyle.alignment = TextAnchor.MiddleCenter;
                    itemStyle.fontStyle = FontStyle.Bold;

                    // Icono placeholder (P para pocion)
                    string simbolo = item.tipo == ItemData.TipoItem.Pocion ? "P" : "?";
                    GUI.Label(new Rect(x, startY, slotSize, slotSize), simbolo, itemStyle);

                    // Cantidad
                    GUI.color = Color.white;
                    GUIStyle cantStyle = new GUIStyle(GUI.skin.label);
                    cantStyle.fontSize = 12;
                    cantStyle.fontStyle = FontStyle.Bold;
                    cantStyle.alignment = TextAnchor.LowerRight;
                    GUI.Label(new Rect(x, startY, slotSize - 4, slotSize - 2), cantidad.ToString(), cantStyle);
                }

                GUI.color = Color.white;
            }
        }
    }
}
