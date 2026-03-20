using UnityEngine;

namespace RealmBrawl
{
    public class DarItemsIniciales : MonoBehaviour
    {
        [SerializeField] ItemData pocion;
        [SerializeField] int cantidadPociones = 3;

        void Start()
        {
            var inv = GetComponent<Inventario>();
            if (inv != null && pocion != null)
                inv.DarPociones(pocion, cantidadPociones);
        }
    }
}
