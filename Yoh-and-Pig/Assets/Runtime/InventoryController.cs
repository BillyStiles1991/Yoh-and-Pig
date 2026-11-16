using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class InventoryController : MonoBehaviour
    {
        public GameProgressStore store; public GameplayDatabase database;
        [HideInInspector] public int capacity = 10; // Legacy Inspector value; saved slot upgrades are authoritative.
        public int Capacity => store.Data.inventorySlots;
        public long UnitsHeld => store.Data.inventory.Sum(e => (long)e.quantity);
        public int Weight(InventoryEntry e) => e.unitWeight > 0 ? e.unitWeight : ItemSizing.Weight(database.Item(e.itemId)?.size ?? ItemSize.Tiny);
        public int Weight(string id) => ItemSizing.Weight(database.Item(id)?.size ?? ItemSize.Tiny);
        public int SlotsUsed => Slots(store.Data.inventory);
        int Slots(IEnumerable<InventoryEntry> entries) => entries.Where(e => !Exempt(e.itemId)).Sum(e => (int)(((long)e.quantity + ItemSizing.Limit(Weight(e)) - 1) / ItemSizing.Limit(Weight(e))));
        bool Exempt(string id) { var d = database.Item(id); return d != null && (d.isKeyItem || d.itemType == ItemType.Outfit || d.itemType == ItemType.Tool); }
        public int Count(string id) => database.Item(id)?.itemType == ItemType.Tool ? (store.Data.tools.Exists(t=>t.itemId==id)?1:0) : database.Item(id)?.itemType == ItemType.Outfit ? (store.Data.outfits.Contains(id) ? 1 : 0) : store.Data.inventory.Where(e => e.itemId == id).Sum(e => e.quantity);
        public int Count(string id, int weight) => store.Data.inventory.Where(e => e.itemId == id && Weight(e) == weight).Sum(e => e.quantity);
        public bool IsEquipped(string id) => store.Data.outfit == id;
        public bool Identified(string id) => database.Item(id) is ItemDefinition d && (!d.startsUnidentified || store.Data.identifiedItems.Contains(id));
        public string Name(string id) { var d = database.Item(id); return d == null ? id : Identified(id) ? d.displayName : d.unidentifiedName; }
        public Sprite Icon(string id) { var d = database.Item(id); return d == null ? null : Identified(id) ? d.icon : d.unidentifiedIcon; }
        public bool IsNew(string id) => store.Data.discoveredItems.Contains(id) && !store.Data.examinedItems.Contains(id);
        public void Examine(string id) { if (!store.Data.examinedItems.Contains(id)) { store.Data.examinedItems.Add(id); store.NotifyChanged(); } }
        public void UpgradeSlots(int slots) { if (slots == 20 || slots == 30 || slots == 50) { store.Data.inventorySlots = Math.Max(Capacity, slots); store.NotifyChanged(); } }
        public bool FitsPickup(string id, int quantity, int weight = 0) => CanExchange(null, new[] { new ItemAmount { itemId = id, quantity = quantity, unitWeight = weight } });
        [Obsolete("Use FitsPickup(itemId, quantity) for size-aware capacity.")]
        public bool FitsPickup(int quantity) => quantity > 0 && SlotsUsed < Capacity;
        public bool CanDiscard(string id, int quantity = 1, int weight = 0)
        {
            var d = database.Item(id);
            return d != null && !d.isKeyItem && d.itemType != ItemType.Outfit && d.itemType != ItemType.Tool && quantity > 0 &&
                CanExchange(new[] { new ItemAmount { itemId = id, quantity = quantity, unitWeight = weight } }, null);
        }
        public bool TryDiscard(string id, int amount = 1) => CanDiscard(id, amount) && TryRemove(id, amount);
        public bool TryRemove(string id, int amount = 1) => TryExchange(new[] { new ItemAmount { itemId = id, quantity = amount } }, null);
        public bool TryAdd(string id, int amount = 1) => TryExchange(null, new[] { new ItemAmount { itemId = id, quantity = amount } });
        public bool CanExchange(ItemAmount[] costs, ItemAmount[] gains) => Plan(costs, gains, out _, out _);
        bool Plan(ItemAmount[] costs, ItemAmount[] gains, out List<InventoryEntry> next, out List<string> outfits)
        {
            next = store.Data.inventory.Select(e => new InventoryEntry { itemId = e.itemId, quantity = e.quantity, unitWeight = Weight(e) }).ToList();
            outfits = new List<string>(store.Data.outfits);
            try
            {
                foreach (var a in (costs ?? Array.Empty<ItemAmount>()).Concat(gains ?? Array.Empty<ItemAmount>()))
                    if (a == null || a.quantity < 1 || a.unitWeight < 0 || database.Item(a.itemId) == null) return false;
                foreach (var a in costs ?? Array.Empty<ItemAmount>())
                {
                    if ((database.Item(a.itemId).itemType == ItemType.Outfit || database.Item(a.itemId).itemType == ItemType.Tool)) return false;
                    int left = a.quantity;
                    foreach (var e in next.Where(e => e.itemId == a.itemId && (a.unitWeight == 0 || e.unitWeight == a.unitWeight)))
                    {
                        int reserved = store.Data.heldItem == e.itemId && store.Data.heldWeight == e.unitWeight ? 1 : 0;
                        int take = Math.Min(left, Math.Max(0, e.quantity - reserved));
                        e.quantity -= take; left -= take;
                    }
                    if (left != 0) return false;
                }
                next.RemoveAll(e => e.quantity == 0);
                foreach (var a in gains ?? Array.Empty<ItemAmount>())
                {
                    if (database.Item(a.itemId).itemType == ItemType.Tool) continue;
                    if (database.Item(a.itemId).itemType == ItemType.Outfit)
                    { if (!outfits.Contains(a.itemId)) outfits.Add(a.itemId); continue; }
                    int weight = a.unitWeight > 0 ? a.unitWeight : Weight(a.itemId);
                    var e = next.Find(x => x.itemId == a.itemId && x.unitWeight == weight);
                    if (e == null) next.Add(new InventoryEntry { itemId = a.itemId, quantity = a.quantity, unitWeight = weight });
                    else e.quantity = checked(e.quantity + a.quantity);
                }
                int before = SlotsUsed, after = Slots(next);
                return after <= Capacity || after <= before;
            }
            catch (OverflowException) { return false; }
        }
        internal bool ExchangeQuiet(ItemAmount[] costs, ItemAmount[] gains)
        {
            if (!Plan(costs, gains, out var next, out var outfits)) return false;
            store.Data.inventory = next; store.Data.outfits = outfits;
            foreach (var a in gains ?? Array.Empty<ItemAmount>())
            {
                var d=database.Item(a.itemId);
                if(d.itemType==ItemType.Tool && !store.Data.tools.Exists(t=>t.itemId==a.itemId))
                    store.Data.tools.Add(new ToolRecord{itemId=a.itemId,sharpness=Mathf.Max(0,d.maximumSharpness)});
                if (!store.Data.discoveredItems.Contains(a.itemId)) store.Data.discoveredItems.Add(a.itemId);
                QuickBarRules.AutoAssign(this,a.itemId,a.unitWeight>0?a.unitWeight:Weight(a.itemId));
            }
            return true;
        }
        public bool TryExchange(ItemAmount[] costs, ItemAmount[] gains)
        { if (!ExchangeQuiet(costs, gains)) return false; store.NotifyChanged(); return true; }
    }
}
