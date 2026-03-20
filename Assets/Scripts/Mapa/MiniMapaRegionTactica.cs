using UnityEngine;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este componente representa una region abstracta del minimapa tactico.
// No dibuja nada por si solo: solo describe tamano, color y categoria
// para que MiniMapaJugadorUI la convierta en una forma UI.

public class MiniMapaRegionTactica : MonoBehaviour
{
    public enum TipoRegionTactica
    {
        Bosque,
        Landmark,
        Carril,
        Choke,
        Spawn,
        Camino
    }

    public enum FormaRegionTactica
    {
        Rectangulo,
        Diamante,
        Capsula
    }

    [Header("Mapa Tactico")]
    [SerializeField] private TipoRegionTactica tipo = TipoRegionTactica.Bosque;
    [SerializeField] private FormaRegionTactica forma = FormaRegionTactica.Rectangulo;
    [SerializeField] private Vector2 tamanoMundo = new Vector2(6f, 6f);
    [SerializeField] private Color colorBase = new Color(0.20f, 0.34f, 0.18f, 0.92f);
    [SerializeField] private float rotacionPlano;

    public TipoRegionTactica Tipo => tipo;
    public FormaRegionTactica Forma => forma;
    public Vector2 TamanoMundo => tamanoMundo;
    public Color ColorBase => colorBase;
    public float RotacionPlano => rotacionPlano;

    private void OnValidate()
    {
        tamanoMundo.x = Mathf.Max(0.5f, tamanoMundo.x);
        tamanoMundo.y = Mathf.Max(0.5f, tamanoMundo.y);
    }
}
