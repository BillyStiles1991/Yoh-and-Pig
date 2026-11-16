using System;
using FreezerGameplay;
using UnityEditor;
using UnityEngine;
public static class PickupDataChecks
{
    [MenuItem("Tools/Freezer Game/Run Pickup Capacity Checks")]
    static void Run()
    {
        var go=new GameObject("Temporary capacity checks");var db=ScriptableObject.CreateInstance<GameplayDatabase>();
        try
        {
            var inv=go.AddComponent<InventoryController>();inv.store=go.AddComponent<GameProgressStore>();inv.database=db;
            db.items=new[]{new ItemDefinition{id="tiny"},new ItemDefinition{id="small",size=ItemSize.Small},new ItemDefinition{id="key",isKeyItem=true},new ItemDefinition{id="outfit",itemType=ItemType.Outfit}};
            Check(inv.TryAdd("tiny",990)&&inv.SlotsUsed==10,"Ten stacks of 99 fill ten slots");
            Check(!inv.TryAdd("tiny")&&inv.Count("tiny")==990,"Overflow leaves inventory unchanged");
            Check(inv.TryAdd("key",5)&&inv.TryAdd("outfit")&&inv.SlotsUsed==10,"Keys and outfits need no slots");
            Check(!inv.CanDiscard("key")&&!inv.CanDiscard("outfit"),"Protected categories cannot be destroyed");
            inv.UpgradeSlots(20);Check(inv.Capacity==20&&inv.TryAdd("small",12)&&inv.SlotsUsed==11,"Upgrade and Small stack limit");
            Check(inv.TryAdd("small")&&inv.SlotsUsed==12,"Thirteenth Small starts second slot");
            foreach(int value in new[]{1,98})Check(ItemSizing.Size(value)==ItemSize.Tiny,"Tiny threshold");
            Check(ItemSizing.Size(99)==ItemSize.Small&&ItemSizing.Size(198)==ItemSize.Small&&ItemSizing.Size(1210)==ItemSize.Mid&&ItemSizing.Size(3564)==ItemSize.Big,"User size examples");
            Check(inv.CanExchange(new[]{new ItemAmount{itemId="tiny",quantity=99}},new[]{new ItemAmount{itemId="small",quantity=1}}),"Net slot transaction");
            inv.store.Data.heldItem="small";inv.store.Data.heldWeight=99;Check(!inv.TryRemove("small",13),"Held copy reserved");
            Debug.Log("Slot capacity, protection and size checks passed.");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);}
    }
    [MenuItem("Tools/Freezer Game/Run Pickup Counter Checks")]
    static void Counter()
    {
        var go=new GameObject("Temporary counter checks");go.SetActive(false);
        try
        {
            var store=go.AddComponent<GameProgressStore>();var f=go.AddComponent<PickupFeedback>();f.store=store;
            f.SendMessage("OnDisable");f.SendMessage("OnEnable");
            var item=new ItemDefinition{id="tiny"};for(int i=1;i<=12;i++){f.ShowItem(item,1);Check(f.RecentQuantity==i,"Count "+i);}
            store.Restore(new GameProgress());Check(f.RecentQuantity==0,"Restore clears popup");Debug.Log("Counter checks passed.");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
}
