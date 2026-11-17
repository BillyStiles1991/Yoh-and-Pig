using System;
using System.Linq;
using FreezerGameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public class DialogueSuiteChecks:IPreprocessBuildWithReport
{
    public int callbackOrder=>110;
    public void OnPreprocessBuild(BuildReport report){Validate(true);}
    [MenuItem("Tools/Freezer Game/Validate Dialogue and Reviews v1.9")]
    static void MenuValidate()=>Validate(false);
    static void Validate(bool build)
    {
        int errors=0;Action<string> fail=m=>{errors++;Debug.LogError(m);};
        foreach(var guid in AssetDatabase.FindAssets("t:GameplayDatabase"))
        {
            var db=AssetDatabase.LoadAssetAtPath<GameplayDatabase>(AssetDatabase.GUIDToAssetPath(guid));
            if(db.npcProfiles.Where(x=>x!=null).GroupBy(x=>x.id).Any(g=>string.IsNullOrWhiteSpace(g.Key)||g.Count()>1))fail(db.name+": missing/duplicate NPC profile ID.");
            foreach(var p in db.npcProfiles)
            {
                if(p==null){fail(db.name+": null NPC profile.");continue;}
                if(!NpcReviewRules.ValidThresholds(p)){fail(p.id+": Review thresholds must start at zero and strictly increase.");continue;}
                int n=p.reviewLevelPoints.Length;
                if(p.keyNpcLevel<1||p.keyNpcLevel>n||p.animatedBannerLevel<1||p.animatedBannerLevel>n||p.customFontLevel<1||p.customFontLevel>n)fail(p.id+": unlock levels must be inside the configured level range.");
                if(!p.portrait)Debug.LogWarning(p.id+": assign a portrait for Key NPC presentation.",db);
                if(p.expressions==null||p.expressions.Any(e=>e==null||string.IsNullOrWhiteSpace(e.id))||p.expressions.GroupBy(e=>e?.id).Any(g=>g.Count()>1))fail(p.id+": invalid/duplicate expression IDs.");
                if(p.facts==null||p.facts.Any(f=>f==null||f.requiredReviewPoints<0||string.IsNullOrWhiteSpace(f.factId)))fail(p.id+": invalid Review fact unlock.");
            }
            foreach(var q in db.quests)
            {
                if(q==null)continue;
                if(q.reviewRewards==null||q.reviewRewards.Any(r=>r==null||db.Npc(r.npcId)==null)||q.reviewRewards.Where(r=>r!=null).GroupBy(r=>r.npcId).Any(g=>g.Count()>1))fail(q.id+": quest Review rewards need known, distinct NPC IDs.");
            }
            foreach(var d in db.dialogues)
            {
                if(d==null||string.IsNullOrWhiteSpace(d.characterId)||d.lines==null||d.lines.Length==0||d.lines.Any(l=>l==null||string.IsNullOrWhiteSpace(l.text))){fail(db.name+": dialogue needs a character ID and nonempty lines.");continue;}
                var p=db.Npc(d.characterId);
                if(p==null)Debug.LogWarning(d.id+": no NPC profile; normal bubble works, but there are no Review/presentation unlocks.",db);
                else foreach(var line in d.lines)if(!string.IsNullOrEmpty(line.expressionId)&&(p.expressions==null||!p.expressions.Any(e=>e!=null&&e.id==line.expressionId)))fail(d.id+": unknown expression "+line.expressionId);
            }
        }
        if(errors>0&&build)throw new BuildFailedException(errors+" dialogue/Review definition errors. See Console.");
        if(errors==0)Debug.Log("Dialogue/Review definitions passed. Scene references, art and Play Mode still need checking.");
    }
    [MenuItem("Tools/Freezer Game/Run Dialogue Review Data Checks v1.9")]
    static void Tests()
    {
        if(Application.isPlaying){Debug.LogError("Run checks outside Play Mode.");return;}
        int checks=0;Action<bool,string> check=(ok,m)=>{checks++;if(!ok)throw new Exception(m);};
        var p=new NpcProfileDefinition{id="npc.test"};var g=new GameProgress();
        g.bonds.Add(new NpcBondRecord{npcId=p.id,bond=190,lastMealDay=4,masterpieceGiven=true});
        NpcReviewRules.Migrate(g);var r=NpcReviewRules.Get(g,p.id);
        check(r.points==190&&r.lastMealDay==4&&r.masterpieceGiven&&g.bonds.Count==0,"Legacy migration preserves points and limits");
        NpcReviewRules.Migrate(g);check(g.reviews.Count==1&&r.points==190,"Migration is idempotent");
        NpcReviewRules.RefreshUnlocks(r,p);check(r.keyNpcUnlocked&&r.animatedBannerUnlocked&&r.customFontUnlocked,"Threshold unlocks");
        NpcReviewRules.Award(g,p,-999,"bad");check(r.points==0&&NpcReviewRules.Level(p,r.points)==1,"Negative Review floor");
        check(r.keyNpcUnlocked&&r.animatedBannerUnlocked&&r.customFontUnlocked,"Unlock permanence after level loss");
        check(!NpcReviewRules.Award(g,p,50,"bad")&&r.points==0,"Same completion source cannot award twice");
        var fresh=new GameProgress();NpcReviewRules.Award(fresh,p,0,"tutorial",true);var promoted=NpcReviewRules.Get(fresh,p.id);
        check(promoted.keyNpcUnlocked&&promoted.points==70,"Tutorial promotion raises points to configured threshold");
        var start=new NpcProfileDefinition{id="npc.start",startsAsKeyNpc=true};var sr=NpcReviewRules.Get(fresh,start.id);NpcReviewRules.RefreshUnlocks(sr,start);check(sr.keyNpcUnlocked&&!sr.animatedBannerUnlocked,"Start as Key NPC only");
        var restored=JsonUtility.FromJson<GameProgress>(JsonUtility.ToJson(g));NpcReviewRules.Migrate(restored);var saved=restored.reviews[0];check(saved.keyNpcUnlocked&&saved.lastMealDay==4&&saved.masterpieceGiven&&saved.points==0,"Review save roundtrip");
        check(!NpcReviewRules.ValidThresholds(new NpcProfileDefinition{reviewLevelPoints=new[]{0,10,10}}),"Duplicate thresholds rejected");
        var go=new GameObject("Temporary Review checks");var db=ScriptableObject.CreateInstance<GameplayDatabase>();
        try
        {
            var store=go.AddComponent<GameProgressStore>();var inv=go.AddComponent<InventoryController>();inv.store=store;inv.database=db;
            var quests=go.AddComponent<QuestController>();quests.inventory=inv;db.npcProfiles=new[]{p};
            db.quests=new[]{new QuestDefinition{id="tutorial",reviewRewards=new[]{new NpcReviewReward{npcId=p.id,promoteToKeyNpc=true}}}};
            check(quests.StartQuest("tutorial")&&quests.CompleteQuest("tutorial"),"Quest completion transaction");
            check(NpcReviewRules.Get(store.Data,p.id).keyNpcUnlocked&&NpcReviewRules.Get(store.Data,p.id).points==70,"Quest guarantees promotion");
            check(!quests.CompleteQuest("tutorial"),"Completed quest cannot pay twice");
            store.Restore(new GameProgress());check(store.Data.reviews.Count==0,"New save clears Review progression");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);}
        Debug.Log(checks+" dialogue/Review data assertions passed. Input, typewriter, anchoring and animated portraits require Play Mode.");
    }
}
