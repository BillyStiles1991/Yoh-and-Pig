using UnityEngine;
namespace FreezerGameplay
{
    // Inspector-friendly hooks for UnityEvents. Progress actions only; never grant rewards from visual refresh callbacks.
    public class GameplayEventBridge : MonoBehaviour
    {
        public QuestController quests; public RecipeController recipes;
        public void StartQuest(string id) { quests.StartQuest(id); }
        public void CompleteQuest(string id) { quests.CompleteQuest(id); }
        public void ReportEvent(string id) { quests.ReportEvent(id); }
        public void LearnRecipe(string id) { recipes.Learn(id); }
        public void TriggerWorldEvent(string id)
        {
            var store = quests.inventory.store;
            if (string.IsNullOrWhiteSpace(id) || store.HasEvent(id)) return;
            store.MarkEvent(id); quests.ReportEvent("event:" + id);
        }
    }
}
