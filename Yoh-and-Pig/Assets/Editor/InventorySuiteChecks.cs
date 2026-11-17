using System;
using System.IO;
using FreezerGameplay;
using UnityEditor;
using UnityEngine;
public static class InventorySuiteChecks
{
    [MenuItem("Tools/Freezer Game/Run Inventory v1.7 Checks")]
    static void Run()
    {
        if(Application.isPlaying){Debug.LogError("Run outside Play Mode.");return;}
        var go=new GameObject("Temporary inventory suite checks");var db=ScriptableObject.CreateInstance<GameplayDatabase>();
        string dir=Path.Combine(Path.GetTempPath(),"YohV2_"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=go.AddComponent<GameProgressStore>();var inv=go.AddComponent<InventoryController>();inv.store=store;inv.database=db;
            var recipe=go.AddComponent<RecipeController>();recipe.inventory=inv;
            db.items=new[]{new ItemDefinition{id="flour",itemType=ItemType.Ingredient},new ItemDefinition{id="mystery",itemType=ItemType.Ingredient,startsUnidentified=true},new ItemDefinition{id="dish",itemType=ItemType.Food},new ItemDefinition{id="mess",itemType=ItemType.Food}};
            db.unidentifiedMessId="mess";db.recipes=new[]{new RecipeDefinition{id="dish",ingredients=new[]{A("flour",198)},results=new[]{A("dish",2)}},new RecipeDefinition{id="bad",ingredients=new[]{A("flour",5),A("mystery",1)},results=new[]{A("dish",1)}}};
            inv.TryAdd("flour",204);inv.TryAdd("mystery");recipe.Learn("dish");recipe.Learn("bad");
            Check(recipe.Craft("dish")&&inv.Count("dish",99)==2,"Portions divide total weight");
            Check(recipe.Craft("bad")&&inv.Count("mess")==1,"One unknown contaminates entire dish");
            Check(inv.Count("flour")==1&&inv.Count("mystery")==0,"All costs consumed once");
            store.Data.identifiedItems.Add("mystery");store.Data.inventorySlots=30;store.Data.chefLevel=4;
            store.Data.secondsPlayed=187;store.Data.daySeconds=2401;store.Data.sicknessRemaining=8;
            store.Data.drops.Add(new WorldDropRecord{id="drop1",itemId="flour",quantity=1,unitWeight=1,scenePath="Assets/Other.unity",expiresAt=300});
            store.Data.bonds.Add(new NpcBondRecord{npcId="chef",lastMealDay=3,masterpieceGiven=true,bond=12});
            SaveRepository.TestDirectoryOverride=dir;
            var data=new SaveData{slot=1,savedUtc=DateTime.UtcNow.ToString("O"),sceneName="Test",areaId="test",progress=store.Snapshot()};
            SaveRepository.Write(data);var read=SaveRepository.Read(1);store.Restore(read.progress);
            Check(read.version==2&&read.progress.drops[0].expiresAt-read.progress.secondsPlayed==113,"Drop resumes with 1m53s remaining");
            Check(store.Day==3&&store.Data.reviews[0].masterpieceGiven&&store.Data.chefLevel==4&&store.Data.inventorySlots==30,"Calendar, gifts, skill and upgrades round trip");
            data.version=1;bool rejected=false;try{SaveRepository.Validate(data,1);}catch(InvalidDataException){rejected=true;}Check(rejected,"Legacy schema refused");
            store.Restore(new GameProgress());Check(store.Data.identifiedItems.Count==0&&store.Data.reviews.Count==0&&store.Data.inventorySlots==10,"Fresh game resets discoveries and quotas");
            Debug.Log("Inventory v1.7 data checks passed. Scene/UI/animation tests remain required.");
        }
        finally{SaveRepository.TestDirectoryOverride=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    static ItemAmount A(string id,int n)=>new ItemAmount{itemId=id,quantity=n};
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
}
