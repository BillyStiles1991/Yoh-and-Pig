using UnityEngine;
namespace FreezerGameplay
{
    // Compatibility name retained to preserve existing scene references. Current behaviour is outfits only.
    public class EquipmentController : MonoBehaviour
    {
        public InventoryController inventory;
        public bool Equip(string itemId)
        {
            if (inventory.database.Item(itemId)?.itemType != ItemType.Outfit || !inventory.store.Data.outfits.Contains(itemId)) return false;
            inventory.store.Data.outfit = itemId; inventory.store.NotifyChanged(); return true;
        }
        public void Unequip(string unused = "Outfit") { inventory.store.Data.outfit = ""; inventory.store.NotifyChanged(); }
        public string Equipped(string unused = "Outfit") => inventory.store.Data.outfit;
        // Dormant compatibility surface; no stats are applied by this version.
        public float Power => 0; public float Defence => 0;
#if YOH_FUTURE_GEAR
        // Dormant opt-in metadata hook only. No combat/skill effects run in v1.7.
        public string[] GearSkillTags => inventory.database.Item(inventory.store.Data.outfit)?.futureGearSkills ?? System.Array.Empty<string>();
#endif
    }
}
