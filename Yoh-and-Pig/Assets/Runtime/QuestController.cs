using System;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class QuestController : MonoBehaviour
    {
        public InventoryController inventory;
        public QuestRecord Get(string id) => inventory.store.Data.quests.Find(x => x.questId == id);
        public bool CanStartQuest(string id)
        {
            var d = inventory.database.Quest(id); if (d == null || Get(id) != null) return false;
            if (d.objectives.Any(x => x == null || string.IsNullOrWhiteSpace(x.id) || string.IsNullOrWhiteSpace(x.eventId) || x.required < 1) ||
                d.objectives.Select(x => x.id).Distinct().Count() != d.objectives.Length) return false;
            return true;
        }
        public bool StartQuest(string id)
        {
            if (!CanStartQuest(id)) return false;
            var d = inventory.database.Quest(id);
            var q = new QuestRecord { questId = id, title = d.title, status = "Active" };
            foreach (var o in d.objectives) q.objectives.Add(new ObjectiveRecord { objectiveId = o.id, required = o.required });
            inventory.store.Data.quests.Add(q);
            if (!string.IsNullOrWhiteSpace(d.characterId)) inventory.store.LinkCharacterQuest(d.characterId, d.characterName, id);
            else inventory.store.NotifyChanged();
            return true;
        }
        public void ReportEvent(string eventId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(eventId)) return;
            bool changed = false;
            foreach (var q in inventory.store.Data.quests.Where(x => x.status == "Active"))
            {
                var d = inventory.database.Quest(q.questId); if (d == null) continue;
                foreach (var o in q.objectives)
                {
                    var def = Array.Find(d.objectives, x => x.id == o.objectiveId);
                    if (def == null || def.eventId != eventId) continue;
                    o.current = (int)Math.Min(o.required, (long)o.current + amount); changed = true;
                }
            }
            if (changed) inventory.store.NotifyChanged();
        }
        public bool Ready(string id)
        { var q = Get(id); return q != null && q.status == "Active" && q.objectives.All(x => x.current >= x.required); }
        public bool CanComplete(string id)
        {
            var d = inventory.database.Quest(id);
            return d != null && Ready(id) && d.recipeRewards.All(x => inventory.database.Recipe(x) != null) && inventory.CanExchange(null,d.rewards);
        }
        public bool CompleteQuest(string id)
        {
            if (!CanComplete(id)) return false;
            var d = inventory.database.Quest(id);
            if (!inventory.ExchangeQuiet(null, d.rewards)) return false;
            Get(id).status = "Completed";
            foreach (var r in d.recipeRewards) if (!inventory.store.Data.recipes.Contains(r)) inventory.store.Data.recipes.Add(r);
            inventory.store.NotifyChanged(); return true;
        }
        public void FailQuest(string id)
        { var q = Get(id); if (q == null || q.status != "Active") return; q.status = "Failed"; inventory.store.NotifyChanged(); }
    }
}
