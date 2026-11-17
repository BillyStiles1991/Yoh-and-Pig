using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FreezerGameplay
{
    // Always-active manager. The presentation builds speech bubbles/portraits, not a fixed menu page.
    public class DialogueController:MonoBehaviour
    {
        public QuestController quests;public RecipeController recipes;public PlayerControlGate player;
        public DialoguePresentation presentation;
        [HideInInspector] public GameObject panel;
        [HideInInspector] public TMP_Text speaker,body;
        [HideInInspector] public Button nextButton,closeButton;
        DialogueDefinition current;int line,openedFrame,lastAdvanceFrame=-1;bool offering;
        Action offer;GameObject previousFocus;NpcDialogueAnchor source;string activeNpcId;
        GameProgressStore Store=>quests.inventory.store;
        GameplayDatabase Database=>quests.inventory.database;
        public bool IsOpen=>current!=null||offering;
        void Awake()
        {
            if(panel&&!transform.IsChildOf(panel.transform))panel.SetActive(false);
            if(!presentation)presentation=GetComponent<DialoguePresentation>();
            if(presentation)presentation.Hide();
        }
        bool Ready()
        {
            if(quests&&quests.inventory&&quests.inventory.store&&quests.inventory.database&&recipes&&player&&presentation)return true;
            Debug.LogError("Assign DialogueController Quests, Recipes, Player and Presentation. Run Configure Dialogue v1.9 in the gameplay scene.",this);return false;
        }
        public bool CanOpen(string id)
        {
            if(!Ready())return false;var d=Database.Dialogue(id);
            return d!=null&&!string.IsNullOrWhiteSpace(d.characterId)&&d.lines!=null&&d.lines.Length>0&&d.lines.All(x=>x!=null)&&
                (string.IsNullOrWhiteSpace(d.completeQuest)||quests.Get(d.completeQuest)?.status=="Completed"||quests.CanComplete(d.completeQuest))&&
                (string.IsNullOrWhiteSpace(d.requiredEvent)||Store.HasEvent(d.requiredEvent))&&
                (string.IsNullOrWhiteSpace(d.requiredCompletedQuest)||quests.Get(d.requiredCompletedQuest)?.status=="Completed");
        }
        bool Lock(string npcId,NpcDialogueAnchor target)
        {
            if(IsOpen||!Ready()||!player.TryLock(this,true))return false;
            openedFrame=Time.frameCount;activeNpcId=npcId;source=target;
            if(!source)source=FindObjectsByType<NpcDialogueAnchor>(FindObjectsSortMode.None).FirstOrDefault(a=>a.npcId==npcId);
            previousFocus=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);return true;
        }
        public void Open(string id)=>Open(id,null);
        public void Open(string id,NpcDialogueAnchor target)
        {
            if(!CanOpen(id))return;var d=Database.Dialogue(id);if(!Lock(d.characterId,target))return;
            current=d;line=0;Show();
        }
        void Render(string who,string text,string expression,string response="")
        {
            var profile=Database.Npc(activeNpcId);NpcReviewRecord record=null;
            if(profile!=null){record=NpcReviewRules.Get(Store.Data,profile.id);NpcReviewRules.RefreshUnlocks(record,profile);}
            presentation.Show(who,text,profile,record,source,expression,response,Next);
        }
        void Show(){var l=current.lines[line];Render(l.speaker,l.text,l.expressionId);}
        // Compatibility for external scripts. Prefer OfferFrom with a stable NPC ID.
        public bool Offer(string who,string text,string response,Action accept)
        {
            if(!Ready())return false;var profile=Database.npcProfiles.FirstOrDefault(p=>p!=null&&(p.id==who||p.displayName==who));
            return OfferFrom(profile?.id??who,who,text,response,accept,null);
        }
        public bool OfferFrom(string npcId,string who,string text,string response,Action accept,NpcDialogueAnchor target=null)
        {
            if(!Lock(npcId,target))return false;offering=true;offer=accept;Render(who,text,"",response);return true;
        }
        void Update()
        {
            if(!IsOpen)return;
            if(RuntimeControls.Current&&RuntimeControls.Current.SuppressInput)return;
            if(GameInput.CancelPressed&&Time.frameCount>openedFrame){Close();return;}
            if(RuntimeControls.Current&&RuntimeControls.Current.Pressed("Dialogue/Advance"))Next();
        }
        public void Next()
        {
            if(!IsOpen||Time.frameCount<=openedFrame||lastAdvanceFrame==Time.frameCount)return;
            lastAdvanceFrame=Time.frameCount;
            if(presentation.IsRevealing){presentation.RevealAll();return;}
            if(offering){var accept=offer;Close();accept?.Invoke();return;}
            if(line+1<current.lines.Length){line++;Show();return;}
            FinishConversation();
        }
        void FinishConversation()
        {
            string npcId=current.characterId;var beforeProfile=Database.Npc(npcId);
            bool previouslyKey=beforeProfile!=null&&NpcReviewRules.Get(Store.Data,npcId).keyNpcUnlocked;
            var origin=source;string displayName=current.characterName;
            string marker="dialogue:"+current.id;
            if(!Store.HasEvent(marker))
            {
                if((!string.IsNullOrEmpty(current.completeQuest)&&quests.Get(current.completeQuest)?.status!="Completed"&&!quests.CanComplete(current.completeQuest))||
                    (!string.IsNullOrEmpty(current.startQuest)&&quests.Get(current.startQuest)==null&&!quests.CanStartQuest(current.startQuest))||
                    (!string.IsNullOrEmpty(current.learnRecipe)&&Database.Recipe(current.learnRecipe)==null))
                {Render(current.characterName,"This conversation cannot finish yet. Check quest requirements or dialogue setup.","");return;}
                // Preserve transaction order and prevent repeated rewards on replay.
                Store.Data.triggeredEvents.Add(marker);
                if(!string.IsNullOrEmpty(current.startQuest))quests.StartQuest(current.startQuest);
                if(!string.IsNullOrEmpty(current.completeQuest))quests.CompleteQuest(current.completeQuest);
                if(!string.IsNullOrEmpty(current.learnRecipe))recipes.Learn(current.learnRecipe);
                if(!string.IsNullOrEmpty(current.triggerEvent))Store.MarkEvent(current.triggerEvent);
                var profile=Database.Npc(current.characterId);
                if(profile!=null)NpcReviewRules.Award(Store.Data,profile,current.overrideReviewPoints?current.reviewPoints:profile.conversationReviewPoints,marker);
                var transcript=new StringBuilder();foreach(var l in current.lines)transcript.AppendLine(l.speaker+": "+l.text);
                Store.RecordConversation(current.characterId,current.characterName,current.id,current.title,transcript.ToString());
                quests.ReportEvent("talk:"+current.id);Store.NotifyChanged();
            }
            bool promoted=beforeProfile!=null&&!previouslyKey&&NpcReviewRules.Get(Store.Data,npcId).keyNpcUnlocked;
            Close();
            if(promoted)OfferFrom(npcId,displayName,"Key NPC unlocked: "+displayName,"Continue",null,origin);
        }
        public void Close()
        {
            if(!IsOpen)return;offering=false;offer=null;current=null;source=null;
            if(presentation)presentation.Hide();if(panel&&!transform.IsChildOf(panel.transform))panel.SetActive(false);if(player)player.Unlock(this);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(previousFocus&&previousFocus.activeInHierarchy?previousFocus:null);
        }
        void OnDisable(){Close();}
    }
}
