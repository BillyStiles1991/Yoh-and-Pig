using System.Collections;
using UnityEngine;
namespace FreezerGameplay
{
    public class PigFeeding:MonoBehaviour
    {
        public ItemActions actions; public PlayerControlGate gate; public float approachTimeout=8, animationSeconds=.6f;
        public bool Busy{get;private set;} bool committed;
        public bool Begin(string id,int weight,int quantity)
        {
            if(Busy||!actions.pig||actions.inventory.store.Data.pigSeparated||quantity<1)return false;
            var d=actions.inventory.database.Item(id);
            if(d==null||d.isKeyItem||d.itemType==ItemType.Tool||d.itemType==ItemType.Outfit||actions.inventory.Count(id,weight)<quantity)return false;
            if(!gate.TryLock(this,false))return false;
            Busy=true;committed=false;StartCoroutine(Feed(id,weight,quantity));return true;
        }
        IEnumerator Feed(string id,int weight,int quantity)
        {
            actions.pig.Attending=true;float elapsed=0;
            while(!actions.pig.Near(actions.player.position,.95f))
            {
                if(Cancelled()||elapsed>approachTimeout){Finish("The pig could not reach you. Items kept.");yield break;}
                elapsed+=Time.deltaTime;yield return null;
            }
            ItemActions.SetBool(actions.animator,"ThrowToPig",true);ItemActions.SetBool(actions.pig.animator,"Eating",true);elapsed=0;
            while(elapsed<animationSeconds){if(Cancelled()){Finish("Feeding cancelled. Items kept.");yield break;}elapsed+=Time.deltaTime;yield return null;}
            committed=actions.ConsumeForPig(id,weight,quantity);
            Finish(committed?"Fed the pig.":"Item unavailable. Nothing consumed.");
        }
        bool Cancelled()=>!actions.pig||actions.inventory.store.Data.pigSeparated||(RuntimeControls.Current&&RuntimeControls.Current.Pressed("System/Cancel"));
        void Finish(string message)
        {
            if(actions){ItemActions.SetBool(actions.animator,"ThrowToPig",false);if(actions.pig){actions.pig.Attending=false;ItemActions.SetBool(actions.pig.animator,"Eating",false);}}
            if(gate)gate.Unlock(this);Busy=false;var bar=FindAnyObjectByType<QuickBarController>();if(bar)bar.SetStatus(message);
        }
        void OnDisable(){StopAllCoroutines();if(Busy)Finish("Feeding interrupted.");}
    }
}
