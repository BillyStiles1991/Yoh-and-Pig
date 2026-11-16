using System;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class ChoppingBoard : MonoBehaviour
    {
        public string deviceId="chopping_board";public float reach=2;
        public void Interact(){var ui=FindAnyObjectByType<ActivityWindow>();if(ui)ui.OpenBoard(this);}
        public static int Quality(InventoryController inv,string knifeId,PreparationDefinition p)
        {
            var knife=inv.database.Item(knifeId);var state=inv.store.Data.tools.Find(t=>t.itemId==knifeId);
            if(knife==null||knife.toolKind!=ToolKind.Knife||state==null)return -1;
            if(knife.ultimate||knife.ultimatePlus)return 2;
            if(state.sharpness<=0||CulinarySkills.Level(inv.store.Data,"Knife",inv.database)<p.knifeLevel)return -1;
            if((p.optimalKnives??Array.Empty<string>()).Contains(knifeId))return 2;if((p.fineKnives??Array.Empty<string>()).Contains(knifeId))return 1;if((p.badKnives??Array.Empty<string>()).Contains(knifeId))return 0;return -1;
        }
        public bool Chop(InventoryController inv,Transform player,string knifeId,PreparationDefinition p,int weight,out string message)
        {
            message="This preparation is unavailable.";int q=Quality(inv,knifeId,p);
            if(q<0||p.deviceId!=deviceId||Vector2.Distance(player.position,transform.position)>reach||p.ingredientQuantity<1)return false;
            var outputs=q==2?p.fineResults:q==1?p.regularResults:p.poorResults;
            if(outputs.Length==0)return false;
            var knife=inv.database.Item(knifeId);var g=inv.store.Data;string held=g.heldItem;int heldWeight=g.heldWeight;
            g.heldItem="";
            var costs=new[]{new ItemAmount{itemId=p.ingredientId,quantity=p.ingredientQuantity,unitWeight=weight}};
            try
            {
                var maximum=outputs.Select(a=>new ItemAmount{itemId=a.itemId,quantity=a.quantity,unitWeight=a.unitWeight}).ToArray();
                if(knife.ultimatePlus)maximum[0].quantity=checked(maximum[0].quantity+1);
                if(!inv.CanExchange(costs,maximum)){message="Not enough ingredients or room for the result.";return false;}
                bool bonus=knife.ultimatePlus&&UnityEngine.Random.value<Mathf.Clamp(knife.ultimateBonusChance,.05f,.10f);
                var gains=bonus?maximum:outputs;
                if(!inv.ExchangeQuiet(costs,gains))return false;
                if(q==0)g.tools.Find(t=>t.itemId==knifeId).sharpness=Mathf.Max(0,g.tools.Find(t=>t.itemId==knifeId).sharpness-p.badSharpnessLoss);
                CulinarySkills.Add(g,"Knife",q==2?p.fineXP:q==1?p.regularXP:p.poorXP);
                message=(q==2?"Fine":q==1?"Regular":"Poor")+" preparation"+(bonus?" + bonus ingredient!":".");return true;
            }
            finally
            {
                if(inv.Count(held,heldWeight)>0){g.heldItem=held;g.heldWeight=heldWeight;}else{g.heldItem="";g.heldWeight=0;}
                inv.store.NotifyChanged();
            }
        }
    }
}
