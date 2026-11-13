#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveSystemChecks
{
    [MenuItem("Tools/Freezer Saves/Run Save Data Checks")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("Run data checks outside Play mode."); return; }
        string temp = Path.Combine(Path.GetTempPath(), "FreezerSaveTests_" + Guid.NewGuid().ToString("N"));
        GameObject host = null;
        try
        {
            SaveRepository.TestDirectoryOverride = temp;
            host = new GameObject("TemporarySaveTest"); var store = host.AddComponent<GameProgressStore>();
            store.SetItemQuantity("apple", 3); store.SetEquipment("weapon", "sword", "sword_001");
            store.CollectRecipe("soup"); store.CollectRecipe("soup");
            store.SetQuest("q1", "Find lunch", "Active"); store.SetObjective("q1", "apples", 2, 3);
            store.MarkChestOpened("chest1"); store.MarkEvent("door1"); store.UnlockMenu("cookbook");
            store.RecordConversation("chef", "Chef", "intro", "Hello", "Find three apples.");
            store.RecordConversation("chef", "Chef", "intro", "Hello", "Duplicate must not be added.");
            store.LinkCharacterQuest("chef", "Chef", "q1");
            store.UnlockProfileFact("chef", "Chef", "likes_soup", "Likes", "Soup", "intro");
            store.Data.secondsPlayed = 12345.6;
            var data = new SaveData { slot=1, displayName="Chapter 1 Act 1", savedUtc=DateTime.UtcNow.ToString("O"),
                sceneName="Test",areaId="kitchen",playerPosition=new Vector3(4,5,0),progress=store.Snapshot(), screenshotJpegBase64="" };
            SaveRepository.Write(data); var read=SaveRepository.Read(1);
            Assert(read.progress.inventory[0].quantity==3 && read.progress.equipment[0].instanceId=="sword_001", "Inventory/equipment round trip");
            Assert(read.progress.recipes.Count==1 && read.progress.quests[0].objectives[0].current==2,"Recipe deduplication/quest progress");
            Assert(read.progress.characters[0].conversations.Count==1 && read.progress.characters[0].profile.Count==1,"NPC discovery/deduplication");
            Assert(read.progress.openedChests.Contains("chest1") && read.progress.triggeredEvents.Contains("door1"),"World flags");
            Assert(read.progress.unlockedMenuSections.Contains("cookbook") && read.progress.secondsPlayed==12345.6,"Menu unlock/time");
            data.displayName="Overwritten";SaveRepository.Write(data);
            Assert(File.Exists(SaveRepository.SlotPath(1)+".bak") && SaveRepository.Read(1).displayName=="Overwritten","Overwrite and backup");
            for(int slot=2;slot<=4;slot++){data.slot=slot;SaveRepository.Write(data);}
            Assert(SaveRepository.Read(1).slot==1 && SaveRepository.Read(4).slot==4,"Independent slots");
            bool rejected=false;try{SaveRepository.SlotPath(SaveRepository.SlotCount + 1);}catch(ArgumentOutOfRangeException){rejected=true;}Assert(rejected,"Slot range");
            File.WriteAllText(SaveRepository.SlotPath(2),"{}");rejected=false;try{SaveRepository.Read(2);}catch{rejected=true;}Assert(rejected,"Malformed data rejected");
            data.version=99;rejected=false;try{SaveRepository.Validate(data,4);}catch{rejected=true;}Assert(rejected,"Future schema rejected");
            store.MarkEvent("later_event");store.UnlockProfileFact("chef","Chef","secret","Secrets","Hidden fact","later_event");
            store.Restore(read.progress);
            Assert(!store.HasEvent("later_event") && store.Data.characters[0].profile.Count==1,"Loading replaces later discoveries");
            Debug.Log("Freezer saves: all 11 data checks passed. Play-mode integration checks are still required.");
        }
        finally
        {
            SaveRepository.TestDirectoryOverride=null;
            if(host!=null) UnityEngine.Object.DestroyImmediate(host);
            if(Directory.Exists(temp)) Directory.Delete(temp,true);
        }
    }
    private static void Assert(bool condition,string name){if(!condition)throw new Exception("Save test failed: "+name);}
}
#endif
