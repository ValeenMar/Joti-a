using UnityEngine;

namespace RealmBrawl
{
    [CreateAssetMenu(fileName = "NuevoItem", menuName = "Realm Brawl/Item")]
    public class ItemData : ScriptableObject
    {
        public string nombre = "Item";
        public Sprite icono;
        public int cantidadMaxima = 5;
        public TipoItem tipo;
        public float valor = 25f; // para pociones: cuanto cura

        public enum TipoItem
        {
            Pocion,
            // Futuro: Bomba, Buff, etc.
        }
    }
}
