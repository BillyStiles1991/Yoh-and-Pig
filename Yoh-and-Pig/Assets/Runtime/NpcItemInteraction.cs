using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class NpcItemInteraction:MonoBehaviour
    {
        public string npcId;public ItemActions actions;public DialogueController dialogue;public QuestController quests;
        bool approaching;PigCompanion attendingPig;
        NpcDialogueAnchor Anchor=>GetComponent<NpcDialogueAnchor>();
        bool Say(string who,string text,string response,Action accept)=>dialogue.OfferFrom(npcId,who,text,response,accept,Anchor);
        public bool TryPresent()
        {
            if(!actions||!dialogue||!quests||string.IsNullOrEmpty(actions.inventory.store.Data.heldItem))return false;
            if(actions.inventory.database.Npc(npcId)==null)return false;
            if(!approaching)StartCoroutine(Present());return true;
        }
        IEnumerator Present()
        {
            approaching=true;var g=actions.inventory.store.Data;
            if(ItemSizing.Size(g.heldWeight)==ItemSize.Big)
            {
                var pig=actions.pig;if(!pig||g.pigSeparated){approaching=false;yield break;}
                attendingPig=pig;pig.Attending=true;float until=Time.time+8;
                while(pig&&!pig.Near(transform.position)&&Time.time<until&&!g.pigSeparated)yield return null;
                if(pig)pig.Attending=false;attendingPig=null;
                if(!pig||!pig.Near(transform.position)){Say(npcId,"Bring your pig a little closer.","Okay",null);approaching=false;yield break;}
            }
            if(Vector2.Distance(actions.player.position,transform.position)<=3f)Offer();approaching=false;
        }
        void Offer()
        {
            var inv=actions.inventory;var g=inv.store.Data;var profile=inv.database.Npc(npcId);var item=inv.database.Item(g.heldItem);
            if(profile==null||item==null)return;string itemId=item.id;int weight=g.heldWeight;
            var hand=profile.handIns.FirstOrDefault(h=>h.itemId==itemId&&quests.Get(h.questId)?.status=="Active"&&!g.triggeredEvents.Contains("handin:"+npcId+":"+h.questId+":"+itemId));
            if(hand!=null)
            {
                Say(profile.displayName,"Is that for my request?",hand.consume?"Give "+hand.quantity:"Show item",()=>
                {
                    if(g.heldItem!=itemId||g.heldWeight!=weight||quests.Get(hand.questId)?.status!="Active")return;
                    string old=g.heldItem;g.heldItem="";
                    bool ok=hand.quantity>0&&inv.Count(itemId)>=hand.quantity&&(!hand.consume||inv.ExchangeQuiet(new[]{new ItemAmount{itemId=itemId,quantity=hand.quantity}},null));
                    if(!ok){g.heldItem=old;inv.store.NotifyChanged();return;}
                    g.heldWeight=0;g.triggeredEvents.Add("handin:"+npcId+":"+hand.questId+":"+itemId);
                    quests.ReportEvent(hand.objectiveEvent,hand.quantity);if(quests.Ready(hand.questId))quests.CompleteQuest(hand.questId);inv.store.NotifyChanged();
                });return;
            }
            var record=NpcReviewRules.Get(g,npcId);
            var reaction=profile.reactions.FirstOrDefault(r=>r.itemId==itemId)??profile.reactions.FirstOrDefault(r=>!string.IsNullOrEmpty(r.flavour)&&item.flavours.Contains(r.flavour))??profile.reactions.FirstOrDefault(r=>r.matchCategory&&r.category==item.itemType);
            var lines=reaction!=null&&reaction.lines.Length>0?reaction.lines:profile.personalityLines;
            string text=lines.Length>0?lines[UnityEngine.Random.Range(0,lines.Length)]:"That's interesting.";
            bool masterpiece=item.itemType==ItemType.Masterpiece;
            bool canReview=!item.isKeyItem&&(masterpiece?!record.masterpieceGiven&&profile.lovedMasterpieces.Contains(itemId):item.itemType==ItemType.Food&&record.lastMealDay!=inv.store.Day);
            if(!canReview){Say(profile.displayName,text,"Put item away",actions.PutAway);return;}
            Say(profile.displayName,text+"\nWould you like me to review this?","Give for review",()=>
            {
                if(g.heldItem!=itemId||g.heldWeight!=weight||(masterpiece?record.masterpieceGiven:record.lastMealDay==inv.store.Day))return;
                int points=reaction?.reviewPoints??(masterpiece?profile.masterpieceReviewPoints:profile.defaultMealReviewPoints);
                if(!NpcReviewRules.ValidThresholds(profile)){Say(profile.displayName,"This NPC's Review levels need valid thresholds.","Okay",null);return;}
                g.heldItem="";
                if(!inv.ExchangeQuiet(new[]{new ItemAmount{itemId=itemId,quantity=1,unitWeight=weight}},null)){g.heldItem=itemId;return;}
                g.heldWeight=0;bool wasKey=record.keyNpcUnlocked;
                if(masterpiece)record.masterpieceGiven=true;else record.lastMealDay=inv.store.Day;
                NpcReviewRules.Award(g,profile,points,masterpiece?"masterpiece":"meal:"+inv.store.Day);
                string result="Review "+(points>=0?"+":"")+points+" · Level "+NpcReviewRules.Level(profile,record.points)+(record.keyNpcUnlocked&&!wasKey?"\nKey NPC unlocked!":"");
                inv.store.RecordConversation(npcId,profile.displayName,"review:"+Guid.NewGuid().ToString("N"),"Food review: "+item.displayName,text+"\n"+result);
                quests.ReportEvent("review:"+npcId);inv.store.NotifyChanged();Say(profile.displayName,result,"Continue",null);
            });
        }
        void OnDisable(){StopAllCoroutines();if(attendingPig)attendingPig.Attending=false;attendingPig=null;approaching=false;}
    }
}
