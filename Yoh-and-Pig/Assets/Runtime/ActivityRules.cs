using System;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public static class QuickBarRules
    {
        public static int Bars(InventoryController inv) => Mathf.Clamp(inv.Capacity / 10,1,5);
        public static bool AutoEnabled { get => PlayerPrefs.GetInt("Yoh.AutoQuickAssign",1)!=0; set {PlayerPrefs.SetInt("Yoh.AutoQuickAssign",value?1:0);PlayerPrefs.Save();} }
        public static bool ToolSlot(int index) => index%10<2;
        public static bool Assign(InventoryController inv,int index,string id,int weight)
        {
            if(index<0 || index>=Bars(inv)*10)return false;
            if(string.IsNullOrEmpty(id)){inv.store.Data.quickSlots.RemoveAll(s=>s.slot==index);inv.store.NotifyChanged();return true;}
            var d=inv.database.Item(id);
            if(d==null||d.itemType==ItemType.Outfit||ToolSlot(index)!=(d.itemType==ItemType.Tool)||inv.Count(id)<1)return false;
            if(d.itemType!=ItemType.Tool&&inv.Count(id,weight)<1)return false;
            inv.store.Data.quickSlots.RemoveAll(s=>s.slot==index);
            inv.store.Data.quickSlots.Add(new QuickAssignment{slot=index,itemId=id,unitWeight=weight});inv.store.NotifyChanged();return true;
        }
        public static void AutoAssign(InventoryController inv,string id,int weight)
        {
            var d=inv.database.Item(id);if(!AutoEnabled||d==null||d.itemType==ItemType.Outfit)return;
            var list=inv.store.Data.quickSlots;
            list.RemoveAll(s=>inv.Count(s.itemId)<1 || (inv.database.Item(s.itemId)?.itemType!=ItemType.Tool && inv.Count(s.itemId,s.unitWeight)<1));
            if(list.Any(s=>s.itemId==id&&(d.itemType==ItemType.Tool||s.unitWeight==weight)))return;
            for(int i=0;i<Bars(inv)*10;i++)if(ToolSlot(i)==(d.itemType==ItemType.Tool)&&!list.Any(s=>s.slot==i))
            {list.Add(new QuickAssignment{slot=i,itemId=id,unitWeight=weight});return;}
        }
        public static bool Gloves(InventoryController inv) => inv.store.Data.quickSlots.Any(s=>s.slot/10==inv.store.Data.activeBar&&ToolSlot(s.slot)&&inv.Count(s.itemId)>0&&inv.database.Item(s.itemId)?.toolKind==ToolKind.ProtectiveGloves);
    }
    public static class CulinarySkills
    {
        public static float TotalXP(GameProgress g,string id)=>g.skills.Find(s=>s.id==id)?.xp??0;
        public static int Level(GameProgress g,string id,GameplayDatabase database=null)
        {
            float xp=TotalXP(g,id),needed=database?Mathf.Max(1,database.skillXPBase):100;int level=1;int cap=database?Mathf.Clamp(database.skillLevelCap,2,100):100;float growth=database?Mathf.Max(1,database.skillXPGrowth):1.25f;
            while(level<cap&&xp>=needed){xp-=needed;level++;needed*=growth;}return level;
        }
        public static void Add(GameProgress g,string id,float xp)
        {
            if(xp<=0||float.IsNaN(xp)||float.IsInfinity(xp))return;
            var s=g.skills.Find(x=>x.id==id);if(s==null){s=new CulinarySkillRecord{id=id};g.skills.Add(s);}s.xp=Mathf.Min(1e12f,s.xp+xp);
        }
        public static void Exposure(GameProgress g,ItemDefinition item,int quantity,float baseAmount=10)
        {if(item.poisonousRaw||item.dangerousRaw)g.sicknessLevel=Mathf.Clamp(g.sicknessLevel+Mathf.Max(0,quantity)*baseAmount*(item.poisonousRaw?3:1),0,100);}
    }
}
