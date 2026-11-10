using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class EquipmentController : MonoBehaviour
    {
        public InventoryController inventory;
        public bool Equip(string itemId)
        {
            var item = inventory.database.Item(itemId);
            if (item == null || string.IsNullOrWhiteSpace(item.equipmentSlot) || inventory.Count(itemId) < 1) return false;
            inventory.store.SetEquipment(item.equipmentSlot, itemId); return true;
        }
        public void Unequip(string slotId) { inventory.store.SetEquipment(slotId, ""); }
        public string Equipped(string slotId) => inventory.store.Data.equipment.Find(x => x.slotId == slotId)?.itemId;
        public float Power => inventory.store.Data.equipment.Sum(x => inventory.database.Item(x.itemId)?.power ?? 0f);
        public float Defence => inventory.store.Data.equipment.Sum(x => inventory.database.Item(x.itemId)?.defence ?? 0f);
    }
}
