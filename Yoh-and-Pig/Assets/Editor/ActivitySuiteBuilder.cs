using System;
using System.Linq;
using FreezerGameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class ActivitySuiteBuilder
{
    static T Ensure<T>(GameObject go) where T:Component
    {var c=go.GetComponent<T>();if(!c)c=Undo.AddComponent<T>(go);Undo.RecordObject(c,"Configure activities");return c;}
    [MenuItem("Tools/Freezer Game/Configure Activities v1.8 In Active Scene")]
    public static void Configure()
    {
        if(Application.isPlaying)return;
        var inv=Object.FindAnyObjectByType<InventoryController>();var gate=Object.FindAnyObjectByType<PlayerControlGate>();
        if(!inv||!inv.store||!inv.database||!gate){Debug.LogError("Configure the existing inventory and PlayerControlGate first.");return;}
        if(gate.GetComponent<PlayerInput>().actions?.FindAction("System/Pause",false)==null){Debug.LogError("Replace PlayerActions.inputactions with v1.8 and assign it to PlayerInput before configuring activities.");return;}
        var actions=gate.GetComponent<ItemActions>();if(!actions){Debug.LogError("Run Configure Inventory On Selected Page first.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
        var theme=Ensure<ActivityTheme>(inv.gameObject);var inventoryPage=Object.FindObjectsByType<InventoryPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        if(inventoryPage){theme.panelSprite=inventoryPage.panelSprite;theme.buttonSprite=inventoryPage.buttonSprite;theme.paper=inventoryPage.paper;theme.ink=inventoryPage.ink;}
        var controls=Ensure<RuntimeControls>(inv.gameObject);controls.player=gate.GetComponent<PlayerInput>();controls.inputAsset=controls.player.actions;Undo.RecordObject(controls.player,"Route UI through runtime controls");controls.player.uiInputModule=null;EditorUtility.SetDirty(controls.player);
        var feed=Ensure<PigFeeding>(inv.gameObject);feed.actions=actions;feed.gate=gate;
        var bar=Ensure<QuickBarController>(inv.gameObject);var window=Ensure<ActivityWindow>(inv.gameObject);var climate=Ensure<RegionalClimate>(inv.gameObject);
        bar.inventory=inv;bar.player=actions;bar.gate=gate;bar.window=window;
        window.inventory=inv;window.player=actions;window.gate=gate;window.bar=bar;window.feeding=feed;
        climate.inventory=inv;if(inv.database.regions.Length>0)climate.regionId=inv.database.regions[0].id;
        foreach(var c in new Component[]{theme,controls,feed,bar,window,climate}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(inv.gameObject.scene);
        Debug.Log("Activities connected. Use the v1.8 input asset, seed/validate your database, then save the scene. Keep SaveSystems active outside menu pages.",inv);
    }
    [MenuItem("Tools/Freezer Game/Configure Title Controls v1.8")]
    static void TitleControls()
    {
        if(Application.isPlaying)return;
        var title=Object.FindAnyObjectByType<PlayableTitle>();var input=Selection.activeObject as InputActionAsset;
        if(!title||!input){Debug.LogError("Open KitchenTitle and select the updated PlayerActions.inputactions asset in Project.");return;}
        var controls=Ensure<RuntimeControls>(title.gameObject);controls.inputAsset=input;EditorUtility.SetDirty(controls);EditorSceneManager.MarkSceneDirty(title.gameObject.scene);
        Debug.Log("Title shares the gameplay rebinding settings. Save KitchenTitle.",title);
    }
    [MenuItem("Tools/Freezer Game/Add Controls Button To Selected Settings Page")]
    static void Settings()
    {
        var page=Selection.activeGameObject;var controls=Object.FindAnyObjectByType<RuntimeControls>();
        if(Application.isPlaying||!page||!page.GetComponent<RectTransform>()||!controls){Debug.LogError("Select a Settings page in the configured gameplay scene.");return;}
        if(page.transform.Find("Controls and quick bar")){Debug.LogWarning("Controls button already exists.");return;}
        var area=ActivityUI.Rect("Controls and quick bar",page.transform,new Vector2(.2f,.1f),new Vector2(.8f,.2f),Color.clear);
        Undo.RegisterCreatedObjectUndo(area.gameObject,"Add controls button");var b=ActivityUI.Button("Controls and quick bar",area,()=>{});
        UnityEventTools.AddPersistentListener(b.onClick,controls.ShowSettings);EditorSceneManager.MarkSceneDirty(page.scene);
    }
    [MenuItem("Tools/Freezer Game/Add Activity Examples To Selected Database")]
    static void Seed()
    {
        var db=Selection.activeObject as GameplayDatabase;if(!db){Debug.LogError("Select the gameplay definition database asset, not a current-inventory store.");return;}
        Undo.RecordObject(db,"Add activity examples");var items=db.items.ToList();
        Action<ItemDefinition> add=d=>{if(items.All(x=>x.id!=d.id))items.Add(d);};
        string[] ids={"chef","paring","bread","utility","boning","slicer","santoku","nakiri","cleaver","scandinavian","ultimate","ultimate_plus"};
        string[] names={"Chef's Knife / Gyuto","Paring Knife","Bread Knife","Utility / Petty Knife","Boning Knife / Honesuki","Slicer / Sujihiki","Santoku / Bunka","Nakiri","Cleaver","Scandinavian Knife","Ultimate Knife","Ultimate Knife+"};
        for(int i=0;i<ids.Length;i++)add(new ItemDefinition{id="tool.knife."+ids[i],displayName=names[i],description="Preparation tool. Use at a chopping board; not a weapon.",itemType=ItemType.Tool,toolKind=ToolKind.Knife,size=ItemSize.Small,ultimate=ids[i]=="ultimate",ultimatePlus=ids[i]=="ultimate_plus",sharpeningFrequency=.6f+i*.08f,sharpeningVariance=.3f+(i%5)*.12f});
        add(new ItemDefinition{id="tool.basket",displayName="Foraging Basket",itemType=ItemType.Tool,toolKind=ToolKind.Basket,size=ItemSize.Small});
        add(new ItemDefinition{id="tool.gloves",displayName="Protective Gloves",description="Assign to a tool slot on the active bar to avoid harvesting exposure.",itemType=ItemType.Tool,toolKind=ToolKind.ProtectiveGloves});
        add(new ItemDefinition{id="item.example.carrot",displayName="Example Carrot",itemType=ItemType.Ingredient,size=ItemSize.Small});
        foreach(var quality in new[]{"fine","regular","poor"})add(new ItemDefinition{id="item.example.carrot."+quality,displayName=quality+" chopped carrot",itemType=ItemType.Ingredient,size=ItemSize.Tiny});
        db.items=items.ToArray();
        if(!db.preparations.Any(p=>p.id=="prep.example.carrot.dice"))db.preparations=db.preparations.Concat(new[]{new PreparationDefinition{id="prep.example.carrot.dice",displayName="Dice carrot",ingredientId="item.example.carrot",optimalKnives=new[]{"tool.knife.chef","tool.knife.nakiri"},fineKnives=new[]{"tool.knife.utility","tool.knife.santoku"},badKnives=new[]{"tool.knife.bread"},fineResults=new[]{new ItemAmount{itemId="item.example.carrot.fine",quantity=3}},regularResults=new[]{new ItemAmount{itemId="item.example.carrot.regular",quantity=3}},poorResults=new[]{new ItemAmount{itemId="item.example.carrot.poor",quantity=3}}}}).ToArray();
        if(!db.regions.Any(r=>r.id=="kitchen"))db.regions=db.regions.Concat(new[]{new RegionalClimateDefinition{id="kitchen",seasons=new[]{"spring","summer","autumn","winter"}.Select(id=>new SeasonChoice{id=id,weather=new[]{new WeatherChoice{id="clear",weight=6},new WeatherChoice{id="rain",weight=3},new WeatherChoice{id="fog",weight=1}}}).ToArray()}}).ToArray();
        EditorUtility.SetDirty(db);AssetDatabase.SaveAssets();Debug.Log("Added example definitions without replacing existing IDs. Assign your sprites and tune recipes, regional weights and tool properties. These examples do not automatically unlock tools.",db);
    }
}
