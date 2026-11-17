using System;
using System.Linq;
using FreezerGameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;
public static class DialogueSuiteBuilder
{
    static T Ensure<T>(GameObject go)where T:Component{var c=go.GetComponent<T>();if(!c)c=Undo.AddComponent<T>(go);Undo.RecordObject(c,"Configure NPC dialogue");return c;}
    [MenuItem("Tools/Freezer Game/Configure Dialogue v1.9 In Active Scene")]
    static void Configure()
    {
        if(Application.isPlaying){Debug.LogError("Exit Play Mode before configuring dialogue.");return;}
        var inv=Object.FindAnyObjectByType<InventoryController>();var quests=Object.FindAnyObjectByType<QuestController>();var recipes=Object.FindAnyObjectByType<RecipeController>();var gate=Object.FindAnyObjectByType<PlayerControlGate>();
        if(!inv||!inv.database||!inv.store||!quests||!recipes||!gate){Debug.LogError("Open a gameplay scene with the shared Inventory/Store/Database, QuestController, RecipeController and PlayerControlGate.");return;}
        var input=gate.GetComponent<PlayerInput>();
        if(!input||input.actions?.FindAction("Dialogue/Advance",false)==null){Debug.LogError("Replace PlayerActions.inputactions with v1.9 and retain its .meta before configuring dialogue.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
        var existing=Object.FindObjectsByType<DialogueController>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(existing.Length>1){Debug.LogError("Keep only one DialogueController per gameplay scene.");return;}
        var dialogue=existing.FirstOrDefault();if(!dialogue)dialogue=Ensure<DialogueController>(inv.gameObject);
        if(!dialogue.gameObject.activeInHierarchy||(dialogue.panel&&dialogue.transform.IsChildOf(dialogue.panel.transform))){Debug.LogError("Move DialogueController to an always-active manager outside menu/panel roots.");return;}
        Undo.RecordObject(dialogue,"Wire dialogue");var view=Ensure<DialoguePresentation>(dialogue.gameObject);
        var reviews=Ensure<NpcReviewController>(inv.gameObject);reviews.inventory=inv;
        dialogue.quests=quests;dialogue.recipes=recipes;dialogue.player=gate;dialogue.presentation=view;
        if(dialogue.panel&&dialogue.panel!=dialogue.gameObject){Undo.RecordObject(dialogue.panel,"Archive fixed dialogue panel");dialogue.panel.SetActive(false);}
        var theme=inv.GetComponent<ActivityTheme>();if(theme){view.bubbleSprite=theme.panelSprite;view.paper=theme.paper;view.ink=theme.ink;}
        if(!view.settingsContent)
        {
            var assets=AssetDatabase.FindAssets("t:IntroContent");
            if(assets.Length==1)view.settingsContent=AssetDatabase.LoadAssetAtPath<IntroContent>(AssetDatabase.GUIDToAssetPath(assets[0]));
            else Debug.LogWarning("Assign DialoguePresentation.Settings Content to the SAME IntroContent used in KitchenTitle.",view);
        }
        foreach(var journal in Object.FindObjectsByType<CharacterJournalView>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Undo.RecordObject(journal,"Connect journal Review levels");journal.database=inv.database;EditorUtility.SetDirty(journal);}
        foreach(var c in new Component[]{dialogue,view,reviews}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(inv.gameObject.scene);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=dialogue.gameObject;Debug.Log("Dialogue v1.9 wired. Assign Settings Content/fonts, then configure each selected NPC and save the scene.",dialogue);
    }
    [MenuItem("Tools/Freezer Game/Configure Selected NPC Dialogue Anchor")]
    static void Npc()
    {
        var go=Selection.activeGameObject;var dialogue=Object.FindAnyObjectByType<DialogueController>();
        if(Application.isPlaying||!go||!go.scene.IsValid()||!dialogue){Debug.LogError("In Edit Mode select an NPC root in the configured gameplay scene.");return;}
        var talk=Ensure<NpcDialogueInteraction>(go);talk.dialogue=dialogue;var anchor=Ensure<NpcDialogueAnchor>(go);
        var item=go.GetComponent<NpcItemInteraction>();
        if(string.IsNullOrWhiteSpace(anchor.npcId))anchor.npcId=item?item.npcId:dialogue.quests.inventory.database.Dialogue(talk.conversationIds.FirstOrDefault()??"")?.characterId;
        if(!anchor.head){var child=new GameObject("DialogueHead");Undo.RegisterCreatedObjectUndo(child,"Create head anchor");child.transform.SetParent(go.transform,false);child.transform.localPosition=Vector3.up*1.1f;anchor.head=child.transform;}
        foreach(var c in new Component[]{talk,anchor}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(go.scene);Debug.Log("Set Anchor.Npc Id and adjust DialogueHead just above the sprite. Configure conversation IDs. Wire the separate NearbyInteraction trigger to Talk().",go);
    }
    [MenuItem("Tools/Freezer Game/Add Jereremy Review Tutorial To Selected Database")]
    static void Tutorial()
    {
        var db=Selection.activeObject as GameplayDatabase;if(Application.isPlaying||!db){Debug.LogError("Select your populated GameplayDatabase asset outside Play Mode.");return;}
        Undo.RecordObject(db,"Add Jereremy tutorial");const string npc="npc.jereremy",quest="quest.jereremy.first_review";
        if(db.Npc(npc)==null)db.npcProfiles=db.npcProfiles.Concat(new[]{new NpcProfileDefinition{id=npc,displayName="Jereremy",defaultMealReviewPoints=2,keyNpcLevel=2,animatedBannerLevel=4,customFontLevel=5,personalityLines=new[]{"I'd like to try your cooking."}}}).ToArray();
        if(db.Quest(quest)==null)db.quests=db.quests.Concat(new[]{new QuestDefinition{id=quest,title="Jereremy's First Review",characterId=npc,characterName="Jereremy",objectives=new[]{new ObjectiveDefinition{id="review",description="Ask Jereremy to review one cooked meal.",eventId="review:"+npc,required=1}},reviewRewards=new[]{new NpcReviewReward{npcId=npc,promoteToKeyNpc=true}}}}).ToArray();
        var list=db.dialogues.ToList();
        Action<DialogueDefinition> add=d=>{if(list.All(x=>x.id!=d.id))list.Add(d);};
        add(new DialogueDefinition{id="jereremy.introduction",title="Meet Jereremy",characterId=npc,characterName="Jereremy",startQuest=quest,lines=new[]{new DialogueLine{speaker="Jereremy",text="Bring me a cooked meal and I'll give you my review."}}});
        add(new DialogueDefinition{id="jereremy.first_review_complete",title="A New Connection",characterId=npc,characterName="Jereremy",requiredEvent="dialogue:jereremy.introduction",completeQuest=quest,lines=new[]{new DialogueLine{speaker="Jereremy",text="Thank you for sharing your cooking with me."}}});
        add(new DialogueDefinition{id="jereremy.key_npc",title="Getting to Know Jereremy",characterId=npc,characterName="Jereremy",requiredCompletedQuest=quest,lines=new[]{new DialogueLine{speaker="Jereremy",text="Come back whenever you'd like to talk."}}});
        db.dialogues=list.ToArray();EditorUtility.SetDirty(db);AssetDatabase.SaveAssets();
        Debug.Log("Example tutorial added without overwriting matching IDs. Assign portrait/art, author the final dialogue, and list IDs in this order: jereremy.key_npc, jereremy.first_review_complete, jereremy.introduction. The player must acquire/cook an ordinary Food item; no food is silently granted.",db);
    }
}
