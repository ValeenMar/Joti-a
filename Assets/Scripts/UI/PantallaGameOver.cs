using UnityEngine;

namespace RealmBrawl
{
    public class PantallaGameOver : MonoBehaviour
    {
        bool mostrar;

        void OnEnable()
        {
            Eventos.AlMorirJugador += Mostrar;
        }

        void OnDisable()
        {
            Eventos.AlMorirJugador -= Mostrar;
        }

        void Mostrar()
        {
            mostrar = true;
        }

        void OnGUI()
        {
            if (!mostrar) return;

            var gm = GameManager.Instancia;
            if (gm == null) return;

            // Fondo oscuro
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Panel central
            float panelW = 400;
            float panelH = 350;
            float px = (Screen.width - panelW) * 0.5f;
            float py = (Screen.height - panelH) * 0.5f;

            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);

            // Titulo
            GUIStyle titulo = new GUIStyle(GUI.skin.label);
            titulo.fontSize = 36;
            titulo.fontStyle = FontStyle.Bold;
            titulo.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
            titulo.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(px, py + 15, panelW, 50), "GAME OVER", titulo);

            // Stats
            GUIStyle stats = new GUIStyle(GUI.skin.label);
            stats.fontSize = 18;
            stats.normal.textColor = Color.white;
            stats.alignment = TextAnchor.MiddleCenter;

            float sy = py + 80;
            float lineH = 35;

            GUI.Label(new Rect(px, sy, panelW, lineH), $"Oleada alcanzada: {gm.OleadaActual}", stats);
            GUI.Label(new Rect(px, sy + lineH, panelW, lineH), $"Kills totales: {gm.TotalKills}", stats);

            int minutos = Mathf.FloorToInt(gm.TiempoPartida / 60f);
            int segundos = Mathf.FloorToInt(gm.TiempoPartida % 60f);
            GUI.Label(new Rect(px, sy + lineH * 2, panelW, lineH), $"Tiempo: {minutos:00}:{segundos:00}", stats);

            GUI.Label(new Rect(px, sy + lineH * 3, panelW, lineH), $"Nivel: {gm.NivelJugador}", stats);
            GUI.Label(new Rect(px, sy + lineH * 4, panelW, lineH), $"Mejor racha: {gm.MejorRacha}", stats);

            // Boton reiniciar
            GUI.color = new Color(0.8f, 0.15f, 0.15f, 1f);
            float btnW = 200;
            float btnH = 45;
            float btnX = px + (panelW - btnW) * 0.5f;
            float btnY = py + panelH - btnH - 20;

            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), ""))
            {
                gm.ReiniciarPartida();
            }

            GUI.color = Color.white;
            GUIStyle btnTxt = new GUIStyle(GUI.skin.label);
            btnTxt.fontSize = 20;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(btnX, btnY, btnW, btnH), "REINICIAR", btnTxt);
        }
    }
}
