using System;
using System.Linq;
using FreezerGameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ActivitySuiteChecks:BuildPlayerProcessor,IPreprocessBuildWithReport,IProcessSceneWithReport
{
    public override int callbackOrder=>101;
    public override void PrepareForBuild(BuildPlayerContext context)
    {
        var ids=new Dictionary<string,string>();
        foreach(string path in context.BuildPlayerOptions.scenes.Distinct())
        {
            var loaded=SceneManager.GetSceneByPath(path);if(loaded.IsValid()&&loaded.isDirty)throw new BuildFailedException("Save scene before activity validation: "+path);
            Scene preview=default;
            try
            {
                preview=EditorSceneManager.OpenPreviewScene(path);
                foreach(var node in preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ForageNode>(true)))
                {
                    if(string.IsNullOrWhiteSpace(node.nodeId))throw new BuildFailedException("Forage ID missing: "+path+" / "+node.name);
                    if(ids.TryGetValue(node.nodeId,out string prior))throw new BuildFailedException("Duplicate forage ID "+node.nodeId+" in "+prior+" and "+path);
                    ids.Add(node.nodeId,path);
                }
            }
            finally{if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
    public void OnPreprocessBuild(BuildReport report){ValidateDatabases(true);}
    public void OnProcessScene(Scene scene,BuildReport report)
    {
        if(report==null)return;
        var nodes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ForageNode>(true)).ToArray();
        foreach(var group in nodes.GroupBy(n=>n.nodeId))if(string.IsNullOrWhiteSpace(group.Key)||group.Count()>1)throw new BuildFailedException("Missing/duplicate forage ID in "+scene.path+": "+group.Key);
        foreach(var n in nodes)if(!n.inventory||!n.climate||n.readyVisual==n.gameObject||n.emptyVisual==n.gameObject)throw new BuildFailedException("Invalid forage references or root assigned as Visual: "+n.name);
    }
    [MenuItem("Tools/Freezer Game/Validate Activities v1.8")]
    static void ValidateMenu(){ValidateDatabases(false);}
    static void ValidateDatabases(bool build)
    {
        int errors=0;Action<string> fail=m=>{errors++;Debug.LogError(m);};
        foreach(string guid in AssetDatabase.FindAssets("t:GameplayDatabase"))
        {
            var db=AssetDatabase.LoadAssetAtPath<GameplayDatabase>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var p in db.preparations)
            {
                if(p==null||string.IsNullOrWhiteSpace(p.id)||db.Item(p.ingredientId)==null||p.ingredientQuantity<1||p.knifeLevel<1){fail(db.name+": invalid preparation.");continue;}
                foreach(var result in new[]{p.fineResults,p.regularResults,p.poorResults})if(result==null||result.Length==0||result.Any(a=>a==null||a.quantity<1||a.unitWeight<0||db.Item(a.itemId)==null))fail(p.id+": invalid output.");
                var matches=p.optimalKnives.Concat(p.fineKnives).Concat(p.badKnives).ToArray();
                if(matches.Distinct().Count()!=matches.Length||matches.Any(id=>db.Item(id)?.toolKind!=ToolKind.Knife))fail(p.id+": unknown or repeated knife match.");
            }
            foreach(var r in db.regions)
            {
                if(r==null||string.IsNullOrWhiteSpace(r.id)||r.seasons.Length==0||!r.seasons.Any(s=>s.minimumStoryProgress==0&&s.weight>0)){fail(db.name+": region needs an initially eligible season.");continue;}
                if(r.seasonCountWeights.Length!=8||r.seasonCountWeights.Any(x=>float.IsNaN(x)||float.IsInfinity(x)||x<0)||r.seasonCountWeights.Sum()<=0)fail(r.id+": supply eight nonnegative season-count weights.");
                if(r.seasons.Select(s=>s.id).Distinct().Count()!=r.seasons.Length)fail(r.id+": duplicate season IDs.");
                foreach(var s in r.seasons)if(string.IsNullOrWhiteSpace(s.id)||s.weather.Any(w=>w==null||string.IsNullOrWhiteSpace(w.id)||w.weight<0))fail(r.id+": invalid season/weather.");
            }
            foreach(var t in db.items.Where(i=>i!=null&&i.itemType==ItemType.Tool))if(t.toolKind==ToolKind.None||t.maximumSharpness<=0||t.sharpeningSeconds<=0)fail(t.id+": invalid tool settings.");
        }
        if(errors>0&&build)throw new BuildFailedException(errors+" activity database errors. See Console.");
        if(errors==0)Debug.Log("Activity database validation passed. Run Activity data checks and the Play Mode checklist.");
    }
    [MenuItem("Tools/Freezer Game/Run Activity Data Checks v1.8")]
    static void DataChecks()
    {
        if(Application.isPlaying){Debug.LogError("Run data checks outside Play Mode.");return;}
        int assertions=0;Action<bool,string> check=(v,m)=>{assertions++;if(!v)throw new Exception(m);};
        var g=new GameProgress{worldSeed=124};GameProgressStore.NormalizeActivities(g);
        var region=new RegionalClimateDefinition{id="test",seasons=new[]{new SeasonChoice{id="a"},new SeasonChoice{id="b"}}};
        for(int year=0;year<100;year++)
        {var r=RegionalClimate.Generate(g,region,year);check(r.seasons.Count>=1&&r.seasons.Count<=8,"Season count");check(r.seasons.Sum(s=>s.length)==112&&r.weather.Count==112,"Year coverage");check(JsonUtility.ToJson(r)==JsonUtility.ToJson(RegionalClimate.Generate(g,region,year)),"Deterministic schedule");}
        CulinarySkills.Exposure(g,new ItemDefinition{dangerousRaw=true},1);check(g.sicknessLevel==10,"Dangerous exposure");g.sicknessLevel=0;CulinarySkills.Exposure(g,new ItemDefinition{poisonousRaw=true},1);check(g.sicknessLevel==30,"Poison exposure");
        check(SharpeningRules.Accuracy(.06f,10)>SharpeningRules.Accuracy(.06f,1),"Skill widens tolerance");
        check(SharpeningRules.Restoration(1,2,4,1)==8,"Partial sharpening");
        check(SharpeningRules.Restoration(0,2,4,1)==0,"Mistakes never damage knife");
        check(QuickBarRules.ToolSlot(10)&&!QuickBarRules.ToolSlot(12),"Two tool slots each bar");
        var restored=JsonUtility.FromJson<GameProgress>(JsonUtility.ToJson(g));check(restored.sicknessLevel==g.sicknessLevel,"Save roundtrip");
        var go=new GameObject("Temporary activity checks");var db=ScriptableObject.CreateInstance<GameplayDatabase>();bool auto=QuickBarRules.AutoEnabled;
        try
        {
            QuickBarRules.AutoEnabled=true;var store=go.AddComponent<GameProgressStore>();var inv=go.AddComponent<InventoryController>();inv.store=store;inv.database=db;
            db.items=new[]{new ItemDefinition{id="carrot",itemType=ItemType.Ingredient},new ItemDefinition{id="knife",itemType=ItemType.Tool,toolKind=ToolKind.Knife},new ItemDefinition{id="ultimate",itemType=ItemType.Tool,toolKind=ToolKind.Knife,ultimate=true},new ItemDefinition{id="key",isKeyItem=true}};
            check(inv.TryAdd("knife")&&inv.TryAdd("carrot",3),"Ownership acquisition");
            check(store.Data.quickSlots.Find(x=>x.itemId=="knife").slot==0&&store.Data.quickSlots.Find(x=>x.itemId=="carrot").slot==2,"Leftmost typed assignment");
            check(!QuickBarRules.Assign(inv,0,"carrot",1),"Reject ingredient in tool slot");
            inv.UpgradeSlots(20);check(QuickBarRules.Assign(inv,10,"knife",1)&&inv.Count("knife")==1,"Shared tool ownership");
            var prep=new PreparationDefinition{knifeLevel=99,optimalKnives=new[]{"knife"}};check(ChoppingBoard.Quality(inv,"knife",prep)<0,"Ordinary skill gate");
            inv.TryAdd("ultimate");store.Data.tools.Find(t=>t.itemId=="ultimate").sharpness=0;check(ChoppingBoard.Quality(inv,"ultimate",prep)==2,"Ultimate bypasses skill and sharpness");
            var shop=go.AddComponent<Shopkeeper>();shop.inventory=inv;var offer=new ShopOffer{itemId="carrot",buyPrice=3,sellPrice=1};shop.offers=new[]{offer};store.Data.cashola=2;
            check(!shop.Trade(go.transform,offer,1,true,1,out _)&&inv.Count("carrot")==3&&store.Data.cashola==2,"Unaffordable trade is atomic");
            store.Data.cashola=3;check(shop.Trade(go.transform,offer,1,true,1,out _)&&inv.Count("carrot")==4&&store.Data.cashola==0,"Buy commits once");
            inv.TryAdd("key");var key=new ShopOffer{itemId="key",sellPrice=10};shop.offers=new[]{key};check(!shop.Trade(go.transform,key,1,false,1,out _)&&inv.Count("key")==1,"Key cannot be sold");
        }
        finally{QuickBarRules.AutoEnabled=auto;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);}
        Debug.Log(assertions+" activity data assertions passed. This does not replace scene/input testing.");
    }
}
