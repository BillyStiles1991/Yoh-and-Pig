using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class InventoryController : MonoBehaviour
    {
        public GameProgressStore store; public GameplayDatabase database;
        // This starter uses unlimited stacks. Unique instance records from other systems are preserved.
        public int Count(string id) => store.Data.inventory.Where(x => x.itemId == id && string.IsNullOrEmpty(x.instanceId)).Sum(x => x.quantity);
        public bool IsEquipped(string id) => store.Data.equipment.Any(x => x.itemId == id && string.IsNullOrEmpty(x.instanceId));
        public bool TryAdd(string id, int amount = 1) => TryExchange(null, new[] { new ItemAmount { itemId = id, quantity = amount } });
        public bool CanDiscard(string id, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || amount <= 0) return false;
            var item = database.Item(id);
            if (item == null || item.isKeyItem) return false;
            int available = Count(id) - (IsEquipped(id) ? 1 : 0);
            return amount <= available;
        }
        public bool TryDiscard(string id, int amount = 1)
        {
            if (!CanDiscard(id, amount)) return false;
            return TryExchange(new[] { new ItemAmount { itemId = id, quantity = amount } }, null);
        }
        public bool TryRemove(string id, int amount = 1) => TryDiscard(id, amount);
        public bool CanExchange(ItemAmount[] costs, ItemAmount[] gains)
        {
            try { return Plan(costs, gains) != null; } catch (OverflowException) { return false; }
        }
        Dictionary<string,int> Plan(ItemAmount[] costs, ItemAmount[] gains)
        {
            var balances = new Dictionary<string,int>();
            foreach (var group in new[] { costs, gains })
                foreach (var a in group ?? Array.Empty<ItemAmount>())
                {
                    if (a == null || string.IsNullOrWhiteSpace(a.itemId) || database.Item(a.itemId) == null || a.quantity <= 0) return null;
                    if (!balances.ContainsKey(a.itemId)) balances[a.itemId] = Count(a.itemId);
                }
            foreach (var a in costs ?? Array.Empty<ItemAmount>())
            {
                balances[a.itemId] = checked(balances[a.itemId] - a.quantity);
                // Reserve the equipped copy; rewards cannot pay an ingredient cost.
                if (balances[a.itemId] < (IsEquipped(a.itemId) ? 1 : 0)) return null;
            }
            foreach (var a in gains ?? Array.Empty<ItemAmount>()) balances[a.itemId] = checked(balances[a.itemId] + a.quantity);
            return balances;
        }
        public bool TryExchange(ItemAmount[] costs, ItemAmount[] gains)
        {
            Dictionary<string,int> plan;
            try { plan = Plan(costs, gains); } catch (OverflowException) { return false; }
            if (plan == null) return false;
            Apply(plan); store.NotifyChanged(); return true;
        }
        // Internal operations let a chest/quest commit its flag and rewards before notifying views.
        internal bool ExchangeQuiet(ItemAmount[] costs, ItemAmount[] gains)
        {
            Dictionary<string,int> plan;
            try { plan = Plan(costs, gains); } catch (OverflowException) { return false; }
            if (plan == null) return false; Apply(plan); return true;
        }
        void Apply(Dictionary<string,int> plan)
        {
            foreach (var pair in plan)
            {
                store.Data.inventory.RemoveAll(x => x.itemId == pair.Key && string.IsNullOrEmpty(x.instanceId));
                if (pair.Value > 0) store.Data.inventory.Add(new InventoryEntry { itemId = pair.Key, quantity = pair.Value });
            }
        }
    }
}
