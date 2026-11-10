using UnityEngine;
namespace FreezerGameplay
{
    public class RecipeController : MonoBehaviour
    {
        public InventoryController inventory; public QuestController quests;
        public bool Learn(string id)
        { if (inventory.database.Recipe(id) == null) return false; inventory.store.CollectRecipe(id); return true; }
        public bool CanCraft(string id)
        { var r = inventory.database.Recipe(id); return r != null && inventory.store.Data.recipes.Contains(id) && inventory.CanExchange(r.ingredients, r.results); }
        public bool Craft(string id)
        {
            if (!CanCraft(id)) return false;
            var r = inventory.database.Recipe(id);
            if (!inventory.TryExchange(r.ingredients, r.results)) return false;
            if (quests != null) quests.ReportEvent("craft:" + id); return true;
        }
    }
}
