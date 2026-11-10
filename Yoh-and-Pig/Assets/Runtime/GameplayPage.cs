using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace FreezerGameplay
{
    // One component per existing page. Reuses your menu's input module and tab selection.
    public class GameplayPage : MonoBehaviour
    {
        public enum PageKind { Inventory, Equipment, Recipes, Quests }
        public PageKind page;
        public InventoryController inventory; public EquipmentController equipment;
        public RecipeController recipes; public QuestController quests;
        public Transform content; public GameplayRowView rowPrefab; public TMP_Text status;
        readonly List<GameplayRowView> rows = new List<GameplayRowView>();
        bool dirty;
        void OnEnable() { inventory.store.Changed += MarkDirty; dirty = true; }
        void OnDisable() { if (inventory != null && inventory.store != null) inventory.store.Changed -= MarkDirty; }
        void MarkDirty() { dirty = true; }
        void LateUpdate() { if (dirty) { dirty = false; Rebuild(); } }
        public void FocusFirst()
        { if (dirty) { dirty = false; Rebuild(); } if (rows.Count > 0 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(rows[0].button.gameObject); }
        GameplayRowView Add(string key, string text, Sprite sprite, UnityEngine.Events.UnityAction action)
        {
            var row = Instantiate(rowPrefab, content); row.gameObject.SetActive(true); row.key = key;
            row.label.text = text; if (row.icon != null) { row.icon.sprite = sprite; row.icon.enabled = sprite != null; }
            row.button.onClick.AddListener(action);
            if (row.discardButton != null && row.discardButton != row.button)
                row.discardButton.gameObject.SetActive(false);
            rows.Add(row);
            return row;
        }
        void Rebuild()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var focused = rows.Find(x => x.button.gameObject == selected || (x.discardButton != null && x.discardButton.gameObject == selected));
            var key = focused != null ? focused.key : null;
            int index = focused != null ? rows.IndexOf(focused) : 0;
            foreach (var row in rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            rows.Clear();
            if (page == PageKind.Inventory)
                foreach (var entry in inventory.store.Data.inventory)
                {
                    var id = entry.itemId; var d = inventory.database.Item(id);
                    string name = d?.displayName ?? ("Missing definition: " + id);
                    bool unique = !string.IsNullOrEmpty(entry.instanceId);
                    string kind = d != null && d.itemType != ItemType.General ? " [" + d.itemType + "]" : "";
                    string protection = d != null && d.isKeyItem ? " [Key Item]" : "";
                    var row = Add(id + ":" + entry.instanceId, name + " x" + entry.quantity + kind + protection + (inventory.IsEquipped(id) ? " [equipped]" : ""), d?.icon,
                        () => SetStatus(!unique && equipment.Equip(id) ? "Equipped " + name : (d?.description ?? "Item unavailable.")));
                    if (row.discardButton != null && row.discardButton != row.button)
                    {
                        row.discardButton.gameObject.SetActive(true);
                        row.discardButton.interactable = !unique && inventory.CanDiscard(id);
                        row.discardButton.onClick.AddListener(() =>
                            SetStatus(inventory.TryDiscard(id) ? "Discarded one " + name : "Cannot discard " + name));
                    }
                }
            else if (page == PageKind.Equipment)
                foreach (var entry in inventory.store.Data.equipment)
                {
                    var slot = entry.slotId; var d = inventory.database.Item(entry.itemId);
                    Add(slot, slot + ": " + (d?.displayName ?? entry.itemId) + " — select to unequip", d?.icon,
                        () => { equipment.Unequip(slot); SetStatus("Unequipped " + slot); });
                }
            else if (page == PageKind.Recipes)
                foreach (var idValue in inventory.store.Data.recipes)
                {
                    string id = idValue; var d = inventory.database.Recipe(id);
                    if (d == null) { Add(id, "Missing recipe: " + id, null, () => SetStatus("Restore this recipe definition.")); continue; }
                    string needs = string.Join(", ", d.ingredients.Select(x => (inventory.database.Item(x.itemId)?.displayName ?? x.itemId) + " " + inventory.Count(x.itemId) + "/" + x.quantity));
                    Add(id, d.displayName + "\n" + needs, null, () => SetStatus(recipes.Craft(id) ? "Crafted " + d.displayName : "Not enough unequipped ingredients."));
                }
            else
                foreach (var q in inventory.store.Data.quests)
                {
                    string id = q.questId; var d = inventory.database.Quest(id);
                    string objectives = string.Join("\n", q.objectives.Select(o => (d?.objectives.FirstOrDefault(x => x.id == o.objectiveId)?.description ?? o.objectiveId) + " " + o.current + "/" + o.required));
                    Add(id, q.title + " [" + q.status + "]\n" + objectives, null,
                        () => SetStatus(quests.CompleteQuest(id) ? "Quest completed — rewards received." : "Quest is not ready, already finished, or has invalid rewards."));
                }
            if (rows.Count == 0) SetStatus("Nothing discovered yet.");
            // Do not steal focus from a tab/close button while data changes in the background.
            if (focused != null && EventSystem.current != null)
            {
                var replacement = rows.Find(x => x.key == key) ?? (rows.Count > 0 ? rows[Mathf.Min(index, rows.Count - 1)] : null);
                EventSystem.current.SetSelectedGameObject(replacement != null ? replacement.button.gameObject : null);
            }
        }
        void SetStatus(string message) { if (status != null) status.text = message; }
    }
}
