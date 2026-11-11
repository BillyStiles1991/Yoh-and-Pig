using System;
using System.Linq;
using UnityEngine;

// Declarative unlock: attach to an always-active object, not a hidden journal page.
public class NpcDiscoveryRule : MonoBehaviour
{
    public enum TriggerKind { EventTriggered, QuestCompleted, ConversationRecorded }
    public enum ProfileCategory { Bio, Likes, Dislikes, Secrets }
    [Serializable] public class Fact
    {
        public string factId;
        public ProfileCategory category;
        [TextArea] public string text;
    }
    public GameProgressStore progress;
    public TriggerKind trigger;
    public string sourceId;
    public string characterId;
    public string characterName;
    public Fact[] facts = new Fact[0];
    public string[] unlockMenuSections = new string[0];
    private bool applying;
    private void OnEnable() { if(progress!=null) {progress.Changed+=Evaluate;Evaluate();} }
    private void OnDisable() {if(progress!=null)progress.Changed-=Evaluate;}
    private void Evaluate()
    {
        if(applying || progress==null || string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(characterId))return;
        var npc=progress.Data.characters.Find(x=>x.characterId==characterId);
        bool reached=trigger==TriggerKind.EventTriggered?progress.HasEvent(sourceId):
            trigger==TriggerKind.QuestCompleted?progress.Data.quests.Any(x=>x.questId==sourceId && x.status=="Completed"):
            npc!=null && npc.conversations.Any(x=>x.conversationId==sourceId);
        if(!reached)return;
        applying=true;
        try
        {
            foreach(var fact in facts)
            {
                npc=progress.Data.characters.Find(x=>x.characterId==characterId);
                if(fact==null || string.IsNullOrWhiteSpace(fact.factId) || (npc!=null && npc.profile.Any(x=>x.factId==fact.factId)))continue;
                progress.UnlockProfileFact(characterId,characterName,fact.factId,fact.category.ToString(),fact.text,sourceId);
            }
            foreach(var section in unlockMenuSections)
                if(!string.IsNullOrWhiteSpace(section) && !progress.Data.unlockedMenuSections.Contains(section)) progress.UnlockMenu(section);
        }
        finally {applying=false;}
    }
}
