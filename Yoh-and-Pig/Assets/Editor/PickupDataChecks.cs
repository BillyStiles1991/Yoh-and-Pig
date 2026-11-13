using System;
using FreezerGameplay;
using UnityEditor;
using UnityEngine;

public static class PickupDataChecks
{
    [MenuItem("Tools/Freezer Game/Run Pickup Capacity Checks")]
    static void Run()
    {
        var go = new GameObject("Temporary pickup checks");
        var db = ScriptableObject.CreateInstance<GameplayDatabase>();
        try
        {
            var inventory = go.AddComponent<InventoryController>();
            inventory.store = go.AddComponent<GameProgressStore>();
            inventory.database = db;
            inventory.capacity = 3;
            db.items = new[] { new ItemDefinition { id="flour" }, new ItemDefinition { id="hat", rarity=ItemRarity.KeyItem } };
            Check(inventory.TryAdd("flour", 3), "Fill bag");
            Check(!inventory.TryAdd("hat") && inventory.Count("hat")==0, "Reject entire gain");
            Check(inventory.TryExchange(new[] { Amount("flour",2) }, new[] { Amount("hat",1) }), "Exchange final balances");
            Check(!inventory.TryDiscard("hat"), "Key rarity protects from discard");
            inventory.store.Data.inventory.Add(new InventoryEntry { itemId="hat", instanceId="unique", quantity=2 });
            Check(inventory.UnitsHeld==4 && !inventory.FitsPickup(1), "Count unique records and preserve oversized saves");
            Check(inventory.TryDiscard("flour") && inventory.UnitsHeld==3, "Allow reductions while over limit");
            inventory.store.MarkEvent("pickupSeen:flour");
            var saved = JsonUtility.ToJson(inventory.store.Snapshot());
            inventory.store.Restore(JsonUtility.FromJson<GameProgress>(saved));
            Check(inventory.store.HasEvent("pickupSeen:flour"), "Round-trip discovery through existing save format");
            Debug.Log("Pickup capacity and discovery data checks passed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(db); }
    }
    static ItemAmount Amount(string id,int n) => new ItemAmount { itemId=id, quantity=n };
    static void Check(bool result,string message) { if (!result) throw new Exception(message); }
}
