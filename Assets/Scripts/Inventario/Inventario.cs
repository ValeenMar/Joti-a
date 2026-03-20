using UnityEngine;

namespace RealmBrawl
{
    public class Inventario : MonoBehaviour
    {
        [SerializeField] int cantidadSlots = 5;

        ItemData[] items;
        int[] cantidades;
        float cooldownUso;

        const float COOLDOWN_ENTRE_USOS = 0.3f;

        VidaJugador vidaJugador;

        public int CantidadSlots => cantidadSlots;

        void Awake()
        {
            items = new ItemData[cantidadSlots];
            cantidades = new int[cantidadSlots];
            vidaJugador = GetComponent<VidaJugador>();
        }

        void Update()
        {
            if (cooldownUso > 0f)
                cooldownUso -= Time.deltaTime;

            // Teclas 1-5 para usar items
            for (int i = 0; i < cantidadSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UsarItem(i);
                    break;
                }
            }
        }

        public void AgregarItem(ItemData item, int cantidad = 1)
        {
            // Buscar slot existente con el mismo item
            for (int i = 0; i < cantidadSlots; i++)
            {
                if (items[i] == item && cantidades[i] < item.cantidadMaxima)
                {
                    cantidades[i] = Mathf.Min(cantidades[i] + cantidad, item.cantidadMaxima);
                    Eventos.AlCambiarSlot?.Invoke(i, items[i]);
                    return;
                }
            }

            // Buscar slot vacio
            for (int i = 0; i < cantidadSlots; i++)
            {
                if (items[i] == null)
                {
                    items[i] = item;
                    cantidades[i] = Mathf.Min(cantidad, item.cantidadMaxima);
                    Eventos.AlCambiarSlot?.Invoke(i, items[i]);
                    return;
                }
            }
        }

        public void UsarItem(int slot)
        {
            if (slot < 0 || slot >= cantidadSlots) return;
            if (items[slot] == null || cantidades[slot] <= 0) return;
            if (cooldownUso > 0f) return;

            var item = items[slot];

            switch (item.tipo)
            {
                case ItemData.TipoItem.Pocion:
                    if (vidaJugador != null && vidaJugador.VidaLlena()) return;
                    vidaJugador?.Curar(item.valor);
                    break;
            }

            cantidades[slot]--;
            cooldownUso = COOLDOWN_ENTRE_USOS;

            if (cantidades[slot] <= 0)
            {
                items[slot] = null;
                cantidades[slot] = 0;
            }

            Eventos.AlCambiarSlot?.Invoke(slot, items[slot]);
            Eventos.AlUsarItem?.Invoke(slot);
        }

        public ItemData ObtenerItem(int slot)
        {
            if (slot < 0 || slot >= cantidadSlots) return null;
            return items[slot];
        }

        public int ObtenerCantidad(int slot)
        {
            if (slot < 0 || slot >= cantidadSlots) return 0;
            return cantidades[slot];
        }

        // Para que el setup inicial meta pociones
        public void DarPociones(ItemData pocion, int cantidad)
        {
            AgregarItem(pocion, cantidad);
        }
    }
}
