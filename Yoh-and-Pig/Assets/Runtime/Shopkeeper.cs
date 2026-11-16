using System;
using UnityEngine;
namespace FreezerGameplay
{
    [Serializable] public class ShopOffer { public string itemId; public int buyPrice=10, sellPrice=3; }
    public class Shopkeeper:MonoBehaviour
    {
        public InventoryController inventory; public ShopOffer[] offers=Array.Empty<ShopOffer>();
        public float reach=2; public int sharpeningPrice=20;
        public void Interact(){var ui=FindAnyObjectByType<ActivityWindow>();if(ui)ui.OpenShop(this);}
        public bool Trade(Transform player,ShopOffer offer,int amount,bool buy,int weight,out string message)
        {
            message="Trade unavailable.";
            if(!inventory||Array.IndexOf(offers,offer)<0||amount<1||Vector2.Distance(transform.position,player.position)>reach)return false;
            var d=inventory.database.Item(offer.itemId);if(d==null)return false;var g=inventory.store.Data;
            if(buy&&(d.itemType==ItemType.Tool||d.itemType==ItemType.Outfit)&&(inventory.Count(d.id)>0||amount!=1)){message="Already unlocked, or select a quantity of one.";return false;}
            if(!buy&&(d.isKeyItem||d.itemType==ItemType.Outfit||d.itemType==ItemType.Tool)){message="This item cannot be sold.";return false;}
            long price=(long)(buy?offer.buyPrice:offer.sellPrice)*amount;
            if(price<0||price>int.MaxValue||(buy&&g.cashola<price)||(!buy&&(long)g.cashola+price>int.MaxValue)){message="Not enough Cashola, or invalid price.";return false;}
            var items=new[]{new ItemAmount{itemId=d.id,quantity=amount,unitWeight=buy?0:weight}};
            if(!inventory.ExchangeQuiet(buy?null:items,buy?items:null)){message="Not enough room or available items. Put away reserved items before selling.";return false;}
            g.cashola+=(int)price*(buy?-1:1);inventory.store.NotifyChanged();message=buy?"Purchased.":"Sold.";return true;
        }
        public bool Sharpen(Transform player,string id,out string message)
        {
            message="Sharpening unavailable.";if(Vector2.Distance(transform.position,player.position)>reach||sharpeningPrice<0)return false;
            var d=inventory.database.Item(id);var g=inventory.store.Data;var t=g.tools.Find(x=>x.itemId==id);
            if(d==null||d.toolKind!=ToolKind.Knife||t==null||d.ultimate||d.ultimatePlus||t.sharpness>=d.maximumSharpness||g.cashola<sharpeningPrice)return false;
            g.cashola-=sharpeningPrice;t.sharpness=d.maximumSharpness;inventory.store.NotifyChanged();message="Knife sharpened.";return true;
        }
    }
}
