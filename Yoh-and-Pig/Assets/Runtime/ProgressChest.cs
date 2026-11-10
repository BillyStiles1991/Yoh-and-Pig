using UnityEngine;
using UnityEngine.Events;
namespace FreezerGameplay
{
    public class ProgressChest : MonoBehaviour
    {
        public InventoryController inventory; public QuestController quests;
        public string chestId; public ItemAmount[] contents = new ItemAmount[0];
        public UnityEvent onOpenVisual, onClosedVisual;
        bool? shownOpen;
        void OnEnable() { shownOpen = null; inventory.store.Changed += Refresh; Refresh(); }
        void OnDisable() { if (inventory != null && inventory.store != null) inventory.store.Changed -= Refresh; }
        void Refresh()
        { bool open = inventory.store.IsChestOpen(chestId); if (shownOpen == open) return; shownOpen = open; if (open) onOpenVisual.Invoke(); else onClosedVisual.Invoke(); }
        public void Open()
        {
            if (string.IsNullOrWhiteSpace(chestId) || inventory.store.IsChestOpen(chestId)) return;
            if (!inventory.ExchangeQuiet(null, contents)) return;
            inventory.store.Data.openedChests.Add(chestId); inventory.store.NotifyChanged();
            if (quests != null) quests.ReportEvent("open:" + chestId);
        }
    }
}
