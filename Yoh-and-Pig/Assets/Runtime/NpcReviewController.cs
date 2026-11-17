using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    [DefaultExecutionOrder(20)]
    public class NpcReviewController:MonoBehaviour
    {
        public InventoryController inventory;bool applying;
        void OnEnable(){if(inventory&&inventory.store){inventory.store.Changed+=Refresh;Refresh();}}
        void OnDisable(){if(inventory&&inventory.store)inventory.store.Changed-=Refresh;}
        public void Refresh()
        {
            if(applying||!inventory||!inventory.database||!inventory.store)return;
            applying=true;bool changed=false;
            try
            {
                foreach(var profile in inventory.database.npcProfiles.Where(p=>p!=null&&!string.IsNullOrWhiteSpace(p.id)))
                {
                    var record=NpcReviewRules.Get(inventory.store.Data,profile.id);
                    changed|=NpcReviewRules.RefreshUnlocks(record,profile);
                    foreach(var fact in profile.facts.Where(f=>f!=null&&record.points>=f.requiredReviewPoints))
                    {
                        var npc=inventory.store.Data.characters.Find(n=>n.characterId==profile.id);
                        // Do not reveal undiscovered NPCs just by loading all their definitions.
                        if(npc==null||npc.profile.Any(f=>f.factId==fact.factId))continue;
                        inventory.store.UnlockProfileFact(profile.id,profile.displayName,fact.factId,fact.category,fact.text,"review:"+profile.id);
                    }
                }
                if(changed)inventory.store.NotifyChanged();
            }
            finally{applying=false;}
        }
    }
}
