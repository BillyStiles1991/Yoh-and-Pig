using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FreezerGameplay
{
    public class ItemActions : MonoBehaviour
    {
        public InventoryController inventory; public PigCompanion pig; public Transform player;
        public SpriteRenderer handsArt, overheadArt, tinyGlow; public Animator animator;
        public string tinyPose = "HoldTiny", smallPose = "HoldSmall", midPose = "HoldMid", sickBool = "Sick";
        public float sicknessSeconds = 15, sickSpeedMultiplier = .7f;
        public string Message { get; private set; }
        public bool Hold(string id, int weight)
        {
            var d = inventory.database.Item(id);
            if (d == null || inventory.Count(id) < 1 || (d.itemType != ItemType.Outfit && d.itemType != ItemType.Tool && inventory.Count(id, weight) < 1)) return Say("Item unavailable.", false);
            if (d.itemType != ItemType.Tool && ItemSizing.Size(weight) == ItemSize.Big && (!pig || inventory.store.Data.pigSeparated)) return Say("The pig is unavailable to carry this item.", false);
            inventory.store.Data.heldItem = id; inventory.store.Data.heldWeight = weight; inventory.store.NotifyChanged();
            return Say("Holding " + inventory.Name(id), true);
        }
        public void PutAway() { inventory.store.Data.heldItem = ""; inventory.store.Data.heldWeight = 0; inventory.store.NotifyChanged(); }
        public bool Taste(string id, int weight, bool useDevice)
        {
            var d = inventory.database.Item(id);
            if (d == null || d.itemType != ItemType.Ingredient || inventory.Count(id, weight) < 1) return Say("Select an ingredient.", false);
            if (inventory.Identified(id)) return Say("Already identified.", false);
            if (inventory.store.Data.chefLevel < d.identificationLevel) return Say("Requires Chef level " + d.identificationLevel, false);
            CookingDevice device = null;
            if (useDevice && !string.IsNullOrEmpty(d.identificationDevice))
            {
                device = CookingDevice.Find(d.identificationDevice, player.position, true);
                if (!device) return Say("Cannot identify without equipment.", false);
                if (inventory.store.Data.chefLevel < device.minimumChefLevel) return Say("Requires Chef level " + device.minimumChefLevel, false);
            }
            if (!useDevice && !d.dangerousRaw && !d.poisonousRaw && !string.IsNullOrEmpty(d.identificationDevice))
                return Say("Cannot identify without equipment.", false);
            if ((!device || device.consumesIdentification) && !inventory.TryExchange(new[] { new ItemAmount { itemId = id, quantity = 1, unitWeight = weight } }, null))
                return Say("Put away the reserved ingredient first.", false);
            if (!device && (d.dangerousRaw || d.poisonousRaw))
            { CulinarySkills.Exposure(inventory.store.Data,d,1); inventory.store.Data.sicknessRemaining = sicknessSeconds; inventory.store.NotifyChanged(); return Say("Tasting this raw made you sick. Cannot identify without equipment.", false); }
            if (!string.IsNullOrEmpty(d.identificationDevice) && !device) return Say("Cannot identify without equipment.", false);
            inventory.store.Data.identifiedItems.Add(id); inventory.store.NotifyChanged(); return Say("Identified " + d.displayName, true);
        }
        public bool Remove(string id, int weight, int count, bool drop)
        {
            if (!drop) { var feeder=FindAnyObjectByType<PigFeeding>();return Say(feeder&&feeder.Begin(id,weight,count)?"Calling the pig…":"Pig unavailable or player busy.",feeder&&feeder.Busy); }
            return RemoveImmediate(id,weight,count,true);
        }
        internal bool ConsumeForPig(string id,int weight,int count) => RemoveImmediate(id,weight,count,false);
        bool RemoveImmediate(string id,int weight,int count,bool drop)
        {
            var g = inventory.store.Data; bool wasHeld = g.heldItem == id && g.heldWeight == weight;
            if (wasHeld) g.heldItem = "";
            if (!inventory.CanDiscard(id, count, weight) || !inventory.TryExchange(new[] { new ItemAmount { itemId = id, quantity = count, unitWeight = weight } }, null))
            { if (wasHeld) g.heldItem = id; return Say("Protected or unavailable items cannot be removed.", false); }
            if (wasHeld) g.heldWeight = 0;
            if (drop)
                inventory.store.Data.drops.Add(new WorldDropRecord { id = Guid.NewGuid().ToString("N"), itemId = id, unitWeight = weight, quantity = count,
                    scenePath = player.gameObject.scene.path, position = player.position + Vector3.down * .5f, expiresAt = inventory.store.Data.secondsPlayed + 300 });
            inventory.store.NotifyChanged(); return Say(drop ? "Dropped items." : "Destroyed items.", true);
        }
        bool Say(string text, bool success) { Message = text; return success; }
        public static void SetBool(Animator a, string name, bool value)
        { if (!a || string.IsNullOrEmpty(name)) return; foreach (var p in a.parameters) if (p.name == name && p.type == AnimatorControllerParameterType.Bool) { a.SetBool(name, value); break; } }
        void LateUpdate()
        {
            if (!inventory) return;
            var g = inventory.store.Data; var d = inventory.database.Item(g.heldItem); bool held = d != null;
            ItemSize size = held && d.itemType == ItemType.Tool ? ItemSize.Small : ItemSizing.Size(g.heldWeight);
            SetBool(animator,"HoldTool",held && d.itemType==ItemType.Tool);
            SetBool(animator, sickBool, g.sicknessRemaining > 0 || g.sicknessLevel > 0);
            SetBool(animator, tinyPose, held && size == ItemSize.Tiny); SetBool(animator, smallPose, held && size == ItemSize.Small); SetBool(animator, midPose, held && size == ItemSize.Mid);
            if (handsArt)
            {
                handsArt.enabled = held && (size == ItemSize.Tiny || size == ItemSize.Small);
                handsArt.sprite = held ? inventory.Icon(d.id) : null;
                handsArt.color = held && size == ItemSize.Tiny ? Color.Lerp(new Color(1,.85f,.5f), Color.white, Mathf.PingPong(Time.time * 2, 1)) : Color.white;
            }
            if (handsArt && handsArt.sprite) handsArt.transform.localScale = Vector3.one * ((size == ItemSize.Tiny ? .35f : .55f) / Mathf.Max(.01f, handsArt.sprite.bounds.size.x, handsArt.sprite.bounds.size.y));
            if (tinyGlow) { tinyGlow.enabled = held && size == ItemSize.Tiny; tinyGlow.color = new Color(1, .85f, .45f, .35f + .15f * Mathf.Sin(Time.time * 5)); }
            if (overheadArt) { overheadArt.enabled = held && size == ItemSize.Mid; overheadArt.sprite = held ? inventory.Icon(d.id) : null; }
            if (overheadArt && overheadArt.sprite) overheadArt.transform.localScale = Vector3.one * (.7f / Mathf.Max(.01f, overheadArt.sprite.bounds.size.x, overheadArt.sprite.bounds.size.y));
            if (pig) pig.SetCargo(held && size == ItemSize.Big ? inventory.Icon(d.id) : null);
        }
    }
}
