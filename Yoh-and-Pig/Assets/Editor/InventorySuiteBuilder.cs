using FreezerGameplay;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InventorySuiteBuilder
{
    [MenuItem("Tools/Freezer Game/Configure Inventory v1.7 On Selected Page")]
    static void Configure()
    {
        var page=Selection.activeGameObject;
        if(Application.isPlaying || !page || !page.scene.IsValid() || !page.GetComponent<RectTransform>())
        { Debug.LogError("In Edit Mode select your existing InventoryPage UI object.");return; }
        var inv=Object.FindAnyObjectByType<InventoryController>();var gate=Object.FindAnyObjectByType<PlayerControlGate>();
        var equipment=Object.FindAnyObjectByType<EquipmentController>();var recipes=Object.FindAnyObjectByType<RecipeController>();
        if(!inv || !inv.store || !inv.database || !gate || !equipment || !recipes)
        { Debug.LogError("Assign the shared inventory store/database and existing outfit (EquipmentController), recipe and PlayerControlGate components first.");return; }
        if(page.GetComponent<InventoryPanel>()){Debug.LogWarning("InventoryPanel already exists. Edit its references in the Inspector.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Configure new inventory");
        var old=page.GetComponent<GameplayPage>();if(old){Undo.RecordObject(old,"Disable old inventory list");old.enabled=false;}
        foreach(Transform child in page.transform){Undo.RecordObject(child.gameObject,"Archive old page content");child.gameObject.SetActive(false);}
        var actions=gate.GetComponent<ItemActions>();if(!actions)actions=Undo.AddComponent<ItemActions>(gate.gameObject);
        Undo.RecordObject(actions,"Connect item actions");actions.inventory=inv;actions.player=gate.transform;actions.animator=gate.GetComponentInChildren<Animator>();
        if(!actions.handsArt)actions.handsArt=Art("Held in hands",gate.transform,new Vector3(0,.35f,0),actions.animator);
        if(!actions.overheadArt)actions.overheadArt=Art("Held above head",gate.transform,new Vector3(0,1.1f,0),actions.animator);
        if(!actions.tinyGlow)
        {
            actions.tinyGlow=Art("Tiny held glow",gate.transform,new Vector3(0,.35f,0),actions.animator);
            var feedback=gate.GetComponent<PickupFeedback>();
            if(feedback&&feedback.glow){actions.tinyGlow.sprite=feedback.glow.sprite;actions.tinyGlow.transform.localScale=feedback.glow.transform.localScale*.5f;}
            actions.tinyGlow.sortingOrder=actions.handsArt.sortingOrder-1;
        }
        var appearance=gate.GetComponent<OutfitAppearance>();if(!appearance)appearance=Undo.AddComponent<OutfitAppearance>(gate.gameObject);
        Undo.RecordObject(appearance,"Connect outfits");appearance.inventory=inv;appearance.animator=actions.animator;
        var pig=Object.FindAnyObjectByType<PigCompanion>();
        if(!pig)
        {
            var go=new GameObject("Pig companion",typeof(Rigidbody2D),typeof(SpriteRenderer));Undo.RegisterCreatedObjectUndo(go,"Create pig");
            go.transform.position=gate.transform.position+Vector3.left;var body=go.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            pig=Undo.AddComponent<PigCompanion>(go);pig.cargo=Art("Cargo",go.transform,new Vector3(0,.5f,0),null);
        }
        Undo.RecordObject(pig,"Connect pig");pig.player=gate.transform;pig.store=inv.store;actions.pig=pig;
        var drop=inv.GetComponent<WorldDropManager>();if(!drop)drop=Undo.AddComponent<WorldDropManager>(inv.gameObject);
        Undo.RecordObject(drop,"Connect drops");drop.inventory=inv;drop.player=gate.transform;drop.gate=gate;drop.feedback=gate.GetComponent<PickupFeedback>();
        if(drop.feedback){Undo.RecordObject(drop.feedback,"Connect notification identities");drop.feedback.inventory=inv;EditorUtility.SetDirty(drop.feedback);}
        var panel=Undo.AddComponent<InventoryPanel>(page);panel.inventory=inv;panel.actions=actions;panel.outfits=equipment;panel.recipes=recipes;
        Undo.RecordObject(recipes,"Connect cooking position");recipes.player=gate.transform;
        Undo.RecordObject(equipment,"Connect outfit inventory");equipment.inventory=inv;
        foreach(var obj in new Object[]{actions,appearance,pig,drop,panel,recipes,equipment}){EditorUtility.SetDirty(obj);PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(page.scene);
        Debug.Log("Inventory v1.7 connected. Save scene. Assign pig artwork/Animator/obstacle layers, holding art anchors and outfit animation assets; add database definitions. Existing tab wiring stays on this page.",page);
    }
    [MenuItem("Tools/Freezer Game/Validate Inventory v1.7 Database")]
    static void ValidateDatabase()
    {
        var inv=Object.FindAnyObjectByType<InventoryController>();
        if(!inv || !inv.database){Debug.LogError("Open the gameplay scene and assign InventoryController.Database.");return;}
        var db=inv.database;int errors=0;
        System.Action<string> fail=message=>{errors++;Debug.LogError(message,db);};
        foreach(var group in db.items.Where(i=>i!=null).GroupBy(i=>i.id))
            if(string.IsNullOrWhiteSpace(group.Key)||group.Count()>1)fail("Missing/duplicate item ID: "+group.Key);
        foreach(var item in db.items)
        {
            if(item==null){fail("Null item definition.");continue;}
            if(item.dangerousRaw&&string.IsNullOrWhiteSpace(item.identificationDevice))fail(item.id+": raw-dangerous items need an identification device ID.");
            if(item.identificationLevel<1)fail(item.id+": identification level must be positive.");
            if(item.goesWellWith.Any(id=>db.Item(id)==null))fail(item.id+": Goes Well With references an unknown item.");
            if(item.itemType==ItemType.Outfit&&!item.outfitAnimator)Debug.LogWarning(item.id+": assign an outfit Animator controller for visible character changes.",db);
        }
        if(db.Item(db.unidentifiedMessId)==null)fail("Assign an existing Unidentified Mess item ID.");
        foreach(var recipe in db.recipes)
        {
            if(recipe==null||string.IsNullOrWhiteSpace(recipe.id)){fail("Recipe missing ID.");continue;}
            if(recipe.ingredients.Length==0||recipe.results.Length==0)fail(recipe.id+": needs ingredients and results.");
            foreach(var amount in recipe.ingredients.Concat(recipe.results))
                if(amount==null||amount.quantity<1||db.Item(amount.itemId)==null)fail(recipe.id+": invalid amount/item reference.");
        }
        foreach(var q in db.quests)
            if(q!=null&&q.inventorySlotReward!=0&&q.inventorySlotReward!=20&&q.inventorySlotReward!=30&&q.inventorySlotReward!=50)fail(q.id+": slot reward must be 0, 20, 30 or 50.");
        foreach(var npc in db.npcProfiles)
        {
            if(npc==null||string.IsNullOrWhiteSpace(npc.id)){fail("NPC profile missing ID.");continue;}
            foreach(var id in npc.lovedMasterpieces)if(db.Item(id)?.itemType!=ItemType.Masterpiece)fail(npc.id+": loved Masterpiece must reference a Masterpiece item.");
            foreach(var hand in npc.handIns)if(hand==null||db.Quest(hand.questId)==null||db.Item(hand.itemId)==null||hand.quantity<1||string.IsNullOrWhiteSpace(hand.objectiveEvent))fail(npc.id+": invalid quest hand-in.");
        }
        if(errors==0)Debug.Log("Inventory database checks passed. Verify sprites, animation controllers and scene device IDs in Play Mode.",db);
    }

    static SpriteRenderer Art(string name,Transform parent,Vector3 position,Animator animator)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));Undo.RegisterCreatedObjectUndo(go,"Create art anchor");go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var sr=go.GetComponent<SpriteRenderer>();var original=animator?animator.GetComponent<SpriteRenderer>():null;
        if(original){sr.sortingLayerID=original.sortingLayerID;sr.sortingOrder=original.sortingOrder+2;}sr.enabled=false;return sr;
    }
}
