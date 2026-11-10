using System;
using UnityEditor;
using UnityEngine;
using FreezerGameplay;
public static class GameplaySystemChecks
{
    [MenuItem("Tools/Freezer Game/Run Gameplay Data Checks")]
    static void Run()
    {
        var go = new GameObject("Temporary gameplay checks");
        var db = ScriptableObject.CreateInstance<GameplayDatabase>();
        try
        {
            var store = go.AddComponent<GameProgressStore>();
            var inventory = go.AddComponent<InventoryController>(); inventory.store = store; inventory.database = db;
            var gear = go.AddComponent<EquipmentController>(); gear.inventory = inventory;
            var quests = go.AddComponent<QuestController>(); quests.inventory = inventory;
            var recipes = go.AddComponent<RecipeController>(); recipes.inventory = inventory; recipes.quests = quests;
            db.items = new[] { new ItemDefinition { id="flour" }, new ItemDefinition { id="bread" }, new ItemDefinition { id="hat",equipmentSlot="Head", defence=3 } };
            db.recipes = new[] { new RecipeDefinition { id="bread", ingredients=new[] { Amount("flour",2),Amount("flour",1) },results=new[] { Amount("bread",1) } } };
            db.quests = new[] { new QuestDefinition { id="baker",title="Bake",objectives=new[] { new ObjectiveDefinition { id="bake",eventId="craft:bread",required=1 } },rewards=new[] { Amount("hat",1) } } };
            Check(!gear.Equip("hat"), "Cannot equip unowned item");
            Check(inventory.TryAdd("flour",2),"Add ingredients");
            Check(recipes.Learn("bread"),"Learn recipe");
            Check(!recipes.Craft("bread") && inventory.Count("flour")==2,"Duplicate ingredient costs aggregate and failed craft is unchanged");
            Check(quests.StartQuest("baker") && !quests.StartQuest("baker"),"Quest starts once");
            Check(inventory.TryAdd("flour",1) && recipes.Craft("bread"),"Craft consumes ingredients");
            Check(inventory.Count("flour")==0 && inventory.Count("bread")==1 && quests.Ready("baker"),"Craft advances quest");
            Check(quests.CompleteQuest("baker") && !quests.CompleteQuest("baker") && inventory.Count("hat")==1,"Rewards awarded once");
            Check(gear.Equip("hat") && gear.Defence==3,"Equipment reads saved ownership");
            Check(!inventory.TryRemove("hat") && inventory.Count("hat")==1,"Equipped copy protected");
            store.RecordConversation("chef","Chef","intro","Hello","Chef: Hello.");
            store.UnlockProfileFact("chef","Chef","likes","Likes","Bread","intro");
            store.MarkChestOpened("chest.1"); store.MarkEvent("door.1");
            var json = JsonUtility.ToJson(store.Snapshot());
            gear.Unequip("Head"); inventory.TryRemove("hat");
            store.Restore(JsonUtility.FromJson<GameProgress>(json));
            Check(gear.Defence==3 && inventory.Count("hat")==1 && quests.Get("baker").status=="Completed","JSON restore restores inventory, gear and quests");
            Check(store.Data.characters[0].profile.Count==1 && store.Data.characters[0].conversations.Count==1 && store.IsChestOpen("chest.1") && store.HasEvent("door.1"),"Journal and world flags round-trip");
            store.Restore(new GameProgress());
            Check(inventory.Count("hat")==0 && gear.Defence==0 && quests.Get("baker")==null && store.Data.characters.Count==0,"Older/empty restore removes later progress");
            Debug.Log("Gameplay data checks passed. Also run the Play Mode checklist in README.md.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(db); }
    }
    static ItemAmount Amount(string id,int quantity) => new ItemAmount { itemId=id,quantity=quantity };
    static void Check(bool success,string label) { if (!success) throw new Exception("Gameplay check failed: " + label); }
}
