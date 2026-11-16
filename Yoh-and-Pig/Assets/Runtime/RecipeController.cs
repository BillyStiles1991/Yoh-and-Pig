using System;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class RecipeController : MonoBehaviour
    {
        public InventoryController inventory; public QuestController quests; public Transform player;
        public string LastMessage { get; private set; }
        public bool Learn(string id) { if (inventory.database.Recipe(id) == null) return false; inventory.store.CollectRecipe(id); return true; }
        public bool DeviceAvailable(RecipeDefinition r) => string.IsNullOrEmpty(r.deviceId) || (player && CookingDevice.Find(r.deviceId, player.position, false));
        bool Plan(string id, out RecipeDefinition recipe, out ItemAmount[] results)
        {
            recipe = inventory.database.Recipe(id); results = null;
            if (recipe == null || !inventory.store.Data.recipes.Contains(id)) { LastMessage = "Recipe not discovered."; return false; }
            if (inventory.store.Data.chefLevel < recipe.chefLevel) { LastMessage = "Requires Chef level " + recipe.chefLevel; return false; }
            if (!DeviceAvailable(recipe)) { LastMessage = "Stand near the required cooking device."; return false; }
            if (recipe.ingredients.Length == 0 || recipe.results.Length == 0) { LastMessage = "Recipe has no ingredients or servings."; return false; }
            try
            {
                long units = 0; bool mess = false;
                foreach (var a in recipe.ingredients)
                {
                    if (a == null || a.quantity < 1 || inventory.database.Item(a.itemId) == null) return false;
                    mess |= !inventory.Identified(a.itemId);
                    // Consume the same ordered size variants used by InventoryController's transaction.
                    // Author repeated ingredient costs are combined before weight calculation below.
                }
                foreach (var group in recipe.ingredients.GroupBy(a => a.itemId))
                {
                    int remaining = checked(group.Sum(a => a.quantity));
                    foreach (var e in inventory.store.Data.inventory.Where(e => e.itemId == group.Key))
                    {
                        int reserved = inventory.store.Data.heldItem == e.itemId && inventory.store.Data.heldWeight == inventory.Weight(e) ? 1 : 0;
                        int take = Math.Min(remaining, Math.Max(0, e.quantity - reserved));
                        units = checked(units + (long)take * inventory.Weight(e)); remaining -= take;
                    }
                    if (remaining != 0) { LastMessage = "Not enough available ingredients. Put down any reserved ingredient first."; return false; }
                }
                if (recipe.results.Any(a => a == null || a.quantity < 1 || inventory.database.Item(a.itemId) == null)) { LastMessage = "Invalid recipe result."; return false; }
                long servings = recipe.results.Sum(a => (long)a.quantity);
                if (servings < 1 || servings > int.MaxValue || recipe.results.Any(a => a.quantity < 1 || inventory.database.Item(a.itemId) == null)) return false;
                int weight = checked((int)Math.Max(1, (units + servings - 1) / servings));
                if (mess)
                {
                    if (inventory.database.Item(inventory.database.unidentifiedMessId) == null) { LastMessage = "Configure the Unidentified Mess item."; return false; }
                    results = new[] { new ItemAmount { itemId = inventory.database.unidentifiedMessId, quantity = (int)servings, unitWeight = weight } };
                }
                else results = recipe.results.Select(a => new ItemAmount { itemId = a.itemId, quantity = a.quantity, unitWeight = weight }).ToArray();
                if (!inventory.CanExchange(recipe.ingredients, results)) { LastMessage = "Not enough ingredients or free slots."; return false; }
                LastMessage = mess ? "Cooked Unidentified Mess." : "Cooked " + recipe.displayName; return true;
            }
            catch (OverflowException) { LastMessage = "Recipe quantities are too large."; return false; }
        }
        public bool CanCraft(string id) => Plan(id, out _, out _);
        public bool Craft(string id)
        {
            if (!Plan(id, out var r, out var gains) || !inventory.TryExchange(r.ingredients, gains)) return false;
            if (quests) quests.ReportEvent(gains.Any(a => a.itemId == inventory.database.unidentifiedMessId) ? "craft:unidentified_mess" : "craft:" + id); return true;
        }
    }
}
