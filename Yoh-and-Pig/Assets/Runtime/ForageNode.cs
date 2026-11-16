using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    [Serializable] public class SeasonalForage { public string seasonId,itemId;public int quantity=1; }
    public class ForageNode : MonoBehaviour
    {
        public string nodeId;public InventoryController inventory;public RegionalClimate climate;
        public SeasonalForage[] harvests=new SeasonalForage[0];public int replenishDays=1;public float reach=1.8f,exposurePerItem=10;
        public GameObject readyVisual,emptyVisual;
        static readonly List<ForageNode> all=new List<ForageNode>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>all.Clear();
        void OnEnable(){all.Add(this);}void OnDisable(){all.Remove(this);}
        SeasonalForage Available()
        {
            if(!inventory||!climate||string.IsNullOrWhiteSpace(nodeId))return null;
            var state=inventory.store.Data.forage.Find(f=>f.id==nodeId);
            if(state!=null&&inventory.store.Day<state.nextDay)return null;
            return harvests.FirstOrDefault(h=>h.seasonId==climate.Season&&h.quantity>0&&inventory.database.Item(h.itemId)!=null);
        }
        void Update(){bool ready=Available()!=null;if(readyVisual)readyVisual.SetActive(ready);if(emptyVisual)emptyVisual.SetActive(!ready);}
        public static ForageNode Nearest(Vector3 position)=>all.Where(n=>n&&Vector2.Distance(n.transform.position,position)<=n.reach).OrderBy(n=>Vector2.Distance(n.transform.position,position)).FirstOrDefault();
        public bool Harvest(Transform player,out string message)
        {
            message="Nothing available this season, or this plant is regrowing.";var h=Available();
            if(h==null||Vector2.Distance(player.position,transform.position)>reach)return false;
            var item=inventory.database.Item(h.itemId);var g=inventory.store.Data;
            int level=CulinarySkills.Level(g,"Foraging",inventory.database);float chance=Mathf.Clamp((level-item.forageReferenceLevel)*inventory.database.bonusForagePerLevel,0,inventory.database.maximumForageBonus);
            int maximum=h.quantity+(chance>0?1:0);
            if(!inventory.FitsPickup(h.itemId,maximum)){message="Make space for this harvest and its possible bonus.";return false;}
            int count=h.quantity+(UnityEngine.Random.value<chance?1:0);
            if(!inventory.ExchangeQuiet(null,new[]{new ItemAmount{itemId=h.itemId,quantity=count}}))return false;
            var record=g.forage.Find(f=>f.id==nodeId);if(record==null){record=new ForageRecord{id=nodeId};g.forage.Add(record);}
            int period=Mathf.Max(1,replenishDays);record.nextDay=((inventory.store.Day-1)/period+1)*period+1;
            int r=(int)item.rarity;float xp=r<inventory.database.rarityForageXP.Length?inventory.database.rarityForageXP[r]:5;
            CulinarySkills.Add(g,"Foraging",xp*count);
            if(!QuickBarRules.Gloves(inventory))CulinarySkills.Exposure(g,item,count,exposurePerItem);
            inventory.store.NotifyChanged();var feedback=player.GetComponent<PickupFeedback>();if(feedback)feedback.ShowItem(item,count);var quests=FindAnyObjectByType<QuestController>();if(quests&&quests.inventory==inventory)quests.ReportEvent("forage:"+h.itemId,count);message="Foraged "+count+" × "+inventory.Name(h.itemId);return true;
        }
        public void Interact()
        {var bar=FindAnyObjectByType<QuickBarController>();if(bar){Harvest(bar.player.transform,out string message);bar.SetStatus(message);}}
    }
}
