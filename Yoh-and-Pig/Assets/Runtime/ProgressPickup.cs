using System.Collections.Generic;
using UnityEngine;

namespace FreezerGameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class ProgressPickup : MonoBehaviour
    {
        public InventoryController inventory;
        public QuestController quests;
        public string pickupId;
        public ItemAmount item = new ItemAmount();
        public GameObject visual;
        internal static readonly List<ProgressPickup> Active = new List<ProgressPickup>();
        internal Collider2D Solid { get; private set; }
        string Key => "pickup:" + pickupId;

        void Awake()
        {
            Solid = GetComponent<Collider2D>();
            Solid.isTrigger = false;
        }

        [ContextMenu("Check Pickup Setup")]
        void CheckPickupSetup()
        {
            ReportSetup(true);
        }

        bool ReportSetup(bool reportSuccess)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(pickupId))
                issues.Add("Progress Pickup > Pickup Id is empty. Give this placement a unique ID.");
            if (!inventory)
                issues.Add("Progress Pickup > Inventory is empty. Assign the gameplay InventoryController.");
            else
            {
                if (!inventory.store)
                    issues.Add($"InventoryController on '{inventory.name}' > Store is empty. Assign the shared GameProgressStore.");
                if (!inventory.database)
                    issues.Add($"InventoryController on '{inventory.name}' > Database is empty. Assign the populated GameplayDatabase asset.");
            }

            if (item == null)
                issues.Add("Progress Pickup > Item is missing. Set its Item Id and Quantity.");
            else
            {
                if (string.IsNullOrWhiteSpace(item.itemId))
                    issues.Add("Progress Pickup > Item > Item Id is empty.");
                else if (inventory && inventory.database &&
                         (inventory.database.items == null || inventory.database.Item(item.itemId) == null))
                    issues.Add($"Item Id '{item.itemId}' was not found in database '{inventory.database.name}'. Match the ID exactly, including case and spaces.");
                if (item.quantity < 1)
                    issues.Add($"Progress Pickup > Item > Quantity is {item.quantity}. Enter 1 or more.");
            }

            if (issues.Count > 0)
            {
                Debug.LogError($"Pickup '{name}':\n- " + string.Join("\n- ", issues), this);
                return false;
            }
            if (reportSuccess) Debug.Log($"Pickup '{name}': setup valid for '{item.itemId}' x{item.quantity}.", this);
            return true;
        }

        void OnEnable()
        {
            if (!ReportSetup(false)) return;
            Active.Add(this);
            inventory.store.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (inventory && inventory.store) inventory.store.Changed -= Refresh;
        }

        void Refresh()
        {
            bool available = !inventory.store.HasEvent(Key);
            Solid.enabled = available;
            if (visual && visual != gameObject) visual.SetActive(available);
        }

        internal void Collect(PickupHands hands)
        {
            if (!Solid.enabled || inventory.store.HasEvent(Key)) return;
            var def = inventory.database.Item(item.itemId);
            if (!inventory.FitsPickup(item.quantity))
            {
                hands.Full();
                return;
            }

            string seen = "pickupSeen:" + item.itemId;
            bool first = !inventory.store.HasEvent(seen) && !inventory.store.Data.inventory.Exists(x => x.itemId == item.itemId);
            if (!inventory.ExchangeQuiet(null, new[] { item })) return;
            var events = inventory.store.Data.triggeredEvents;
            events.Add(Key);
            if (!events.Contains(seen)) events.Add(seen);
            inventory.store.NotifyChanged();
            if (quests) quests.ReportEvent("collect:" + item.itemId, item.quantity);
            hands.Received(def, item.quantity, first);
        }
    }
}



