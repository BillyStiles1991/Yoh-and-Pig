using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class NpcItemInteraction : MonoBehaviour
    {
        public string npcId; public ItemActions actions; public DialogueController dialogue; public QuestController quests;
        bool approaching;
        public bool TryPresent()
        {
            if (!actions || string.IsNullOrEmpty(actions.inventory.store.Data.heldItem)) return false;
            if (!approaching) StartCoroutine(Present()); return true;
        }
        IEnumerator Present()
        {
            approaching = true;
            var g = actions.inventory.store.Data;
            if (ItemSizing.Size(g.heldWeight) == ItemSize.Big)
            {
                var pig = actions.pig;
                if (!pig || g.pigSeparated) { approaching = false; yield break; }
                pig.Attending = true; float until = Time.time + 8;
                while (!pig.Near(transform.position) && Time.time < until && !g.pigSeparated) yield return null;
                pig.Attending = false;
                if (!pig.Near(transform.position)) { dialogue.Offer(npcId, "Bring your pig a little closer.", "Okay", null); approaching = false; yield break; }
            }
            if (Vector2.Distance(actions.player.position, transform.position) <= 3f) Offer(); approaching = false;
        }
        void Offer()
        {
            var inv = actions.inventory; var g = inv.store.Data;
            var profile = inv.database.Npc(npcId); var item = inv.database.Item(g.heldItem);
            if (profile == null || item == null) return;
            string itemId = item.id; int weight = g.heldWeight;
            var hand = profile.handIns.FirstOrDefault(h => h.itemId == itemId && quests.Get(h.questId)?.status == "Active" &&
                !g.triggeredEvents.Contains("handin:" + npcId + ":" + h.questId + ":" + itemId));
            if (hand != null)
            {
                dialogue.Offer(profile.displayName, "Is that for my request?", hand.consume ? "Give " + hand.quantity : "Show item", () =>
                {
                    if (g.heldItem != itemId || quests.Get(hand.questId)?.status != "Active") return;
                    string old = g.heldItem; g.heldItem = "";
                    bool ok = hand.quantity > 0 && inv.Count(itemId) >= hand.quantity && (!hand.consume || inv.ExchangeQuiet(new[] { new ItemAmount { itemId = itemId, quantity = hand.quantity } }, null));
                    if (!ok) { g.heldItem = old; inv.store.NotifyChanged(); return; }
                    g.heldWeight = 0; g.triggeredEvents.Add("handin:" + npcId + ":" + hand.questId + ":" + itemId);
                    quests.ReportEvent(hand.objectiveEvent, hand.quantity); if (quests.Ready(hand.questId)) quests.CompleteQuest(hand.questId);
                    inv.store.NotifyChanged();
                }); return;
            }
            var bond = g.bonds.Find(b => b.npcId == npcId);
            if (bond == null) { bond = new NpcBondRecord { npcId = npcId }; g.bonds.Add(bond); }
            var reaction = profile.reactions.FirstOrDefault(r => r.itemId == itemId) ??
                profile.reactions.FirstOrDefault(r => !string.IsNullOrEmpty(r.flavour) && item.flavours.Contains(r.flavour)) ??
                profile.reactions.FirstOrDefault(r => r.matchCategory && r.category == item.itemType);
            var lines = reaction != null && reaction.lines.Length > 0 ? reaction.lines : profile.personalityLines;
            string text = lines.Length > 0 ? lines[UnityEngine.Random.Range(0, lines.Length)] : "That's interesting.";
            bool masterpiece = item.itemType == ItemType.Masterpiece;
            bool gift = (masterpiece ? !bond.masterpieceGiven && profile.lovedMasterpieces.Contains(itemId) : item.itemType == ItemType.Food && bond.lastMealDay != inv.store.Day);
            if (!gift) { dialogue.Offer(profile.displayName, text, "Put item away", actions.PutAway); return; }
            dialogue.Offer(profile.displayName, text + "\nWould you like to give me this?", "Give as a gift", () =>
            {
                if (g.heldItem != itemId || (masterpiece ? bond.masterpieceGiven : bond.lastMealDay == inv.store.Day)) return;
                g.heldItem = "";
                if (!inv.ExchangeQuiet(new[] { new ItemAmount { itemId = itemId, quantity = 1, unitWeight = weight } }, null)) { g.heldItem = itemId; return; }
                g.heldWeight = 0;
                if (masterpiece) bond.masterpieceGiven = true; else bond.lastMealDay = inv.store.Day;
                bond.bond += reaction?.bondChange ?? (masterpiece ? profile.masterpieceBond : profile.defaultMealBond);
                foreach (var fact in profile.facts.Where(f => bond.bond >= f.requiredBond))
                    inv.store.UnlockProfileFact(npcId, profile.displayName, fact.factId, fact.category, fact.text, "gift:" + itemId);
                inv.store.RecordConversation(npcId, profile.displayName, "gift:" + Guid.NewGuid().ToString("N"), "Gift: " + item.displayName, text);
                inv.store.NotifyChanged();
            });
        }
    }
}
