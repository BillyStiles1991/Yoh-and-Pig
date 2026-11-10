using UnityEngine;
namespace FreezerGameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class ProgressPickup : MonoBehaviour
    {
        public InventoryController inventory; public QuestController quests;
        [Tooltip("Unique per placed pickup, e.g. kitchen.flour.01")] public string pickupId;
        public ItemAmount item = new ItemAmount(); public GameObject visual;
        Collider2D trigger;
        string Key => "pickup:" + pickupId;
        void Awake() { trigger = GetComponent<Collider2D>(); trigger.isTrigger = true; }
        void OnEnable() { inventory.store.Changed += Refresh; Refresh(); }
        void OnDisable() { if (inventory != null && inventory.store != null) inventory.store.Changed -= Refresh; }
        void Refresh()
        { bool available = !inventory.store.HasEvent(Key); trigger.enabled = available; if (visual != null && visual != gameObject) visual.SetActive(available); }
        void OnTriggerEnter2D(Collider2D other)
        {
            var gate = other.GetComponentInParent<PlayerControlGate>();
            if (gate == null || gate.IsLocked || string.IsNullOrWhiteSpace(pickupId) || inventory.store.HasEvent(Key)) return;
            if (!inventory.ExchangeQuiet(null, new[] { item })) return;
            inventory.store.Data.triggeredEvents.Add(Key); inventory.store.NotifyChanged();
            if (quests != null) quests.ReportEvent("collect:" + item.itemId, item.quantity);
        }
    }
}
