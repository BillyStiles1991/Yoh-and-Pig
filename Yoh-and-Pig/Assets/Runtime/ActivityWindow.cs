using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FreezerGameplay
{
    // A single owner for short activity windows. It never creates another inventory or save store.
    public class ActivityWindow:MonoBehaviour
    {
        public InventoryController inventory;public ItemActions player;public PlayerControlGate gate;public QuickBarController bar;public PigFeeding feeding;
        public ChoppingBoard Board{get;private set;}
        Canvas canvas;RectTransform panel,rows;TMP_Text title,status;GameObject oldFocus;int openedFrame;bool paused;
        SharpeningDevice stone;ItemDefinition knife;ToolRecord knifeState;RectTransform track,cursor,knifeArt;RectTransform[] segments;
        float elapsed,duration,position=.5f,seed;int startingSkill;TMP_Text performance;AudioSource workingAudio;
        public bool IsOpen=>canvas;
        public bool AcceptBarAction{get;private set;}
        bool Begin(string name,bool pause)
        {
            if(canvas)return false;if(!gate||!gate.TryLock(this,pause))return false;
            paused=pause;openedFrame=Time.frameCount;oldFocus=UnityEngine.EventSystems.EventSystem.current?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;
            canvas=ActivityUI.Canvas("Activity window",180);
            panel=ActivityUI.Rect("Panel",canvas.transform,new Vector2(.15f,.22f),new Vector2(.85f,.78f),new Color(.96f,.92f,.84f,.98f));
            if(ActivityTheme.Current){var skin=panel.GetComponent<Image>();skin.color=ActivityTheme.Current.paper;if(ActivityTheme.Current.panelSprite){skin.sprite=ActivityTheme.Current.panelSprite;skin.type=Image.Type.Sliced;}}
            title=ActivityUI.Label("Title",ActivityUI.Rect("Title",panel,new Vector2(0,.85f),Vector2.one,Color.clear),name,24);
            status=ActivityUI.Label("Status",ActivityUI.Rect("Status",panel,new Vector2(0,.10f),new Vector2(1,.18f),Color.clear),"",15);
            rows=ActivityUI.List(panel);rows.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>().anchorMin=new Vector2(.04f,.20f);
            var foot=ActivityUI.Rect("Footer",panel,new Vector2(.3f,.02f),new Vector2(.7f,.10f),Color.clear);var back=ActivityUI.Button("Close / return to play",foot,()=>{if(Time.frameCount>openedFrame)Close();});ActivityUI.Focus(back);
            return true;
        }
        Button Row(string text,Action click)
        {
            var b=ActivityUI.Button(text,rows,()=>{if(Time.frameCount>openedFrame)click();});b.gameObject.AddComponent<ActivityListFocus>().scroll=rows.GetComponentInParent<ScrollRect>();return b;
        }
        void ClearRows(){foreach(Transform child in rows){child.gameObject.SetActive(false);Destroy(child.gameObject);}openedFrame=Time.frameCount;}
        public void OpenBoard(ChoppingBoard board)
        {
            if(Vector2.Distance(player.player.position,board.transform.position)>board.reach||!Begin("Chopping board",false))return;
            Board=board;bar.ChooseKnife();RefreshBoard();
        }
        public void RefreshBoard()
        {
            if(!Board||!canvas)return;AcceptBarAction=true;ClearRows();if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);title.text="Chopping board · "+(string.IsNullOrEmpty(bar.activeKnife)?"Assign a knife to this bar":inventory.Name(bar.activeKnife));
            Row("Choose an ingredient using the quick bar",()=>{}).interactable=false;
            foreach(var s in inventory.store.Data.quickSlots.Where(s=>s.slot/10==inventory.store.Data.activeBar&&QuickBarRules.ToolSlot(s.slot)).ToArray())
            {
                var d=inventory.database.Item(s.itemId);if(d?.toolKind!=ToolKind.Knife)continue;string id=s.itemId;
                Row("Use "+inventory.Name(id),()=>{bar.activeKnife=id;RefreshBoard();});
            }
            status.text="Faded ingredients cannot be prepared with this knife. Cancel leaves the board.";
        }
        public void ShowPreparations(QuickAssignment slot)
        {
            if(!Board||!canvas)return;AcceptBarAction=false;ClearRows();
            foreach(var p in inventory.database.preparations.Where(p=>p.ingredientId==slot.itemId&&p.deviceId==Board.deviceId))
            {
                var recipe=p;int q=ChoppingBoard.Quality(inventory,bar.activeKnife,p);
                var b=Row(p.displayName+" · "+(q<0?"Unavailable":q==0?"Poor — wears knife":q==1?"Regular":"Fine"),()=>
                {
                    if(q==0){ClearRows();Row("Confirm poor preparation and sharpness loss",()=>PerformChop(recipe,slot));Row("Back",()=>ShowPreparations(slot));ActivityUI.Focus(rows.GetComponentsInChildren<Button>().First(b=>b.interactable));}
                    else PerformChop(recipe,slot);
                });b.interactable=q>=0&&inventory.Count(slot.itemId,slot.unitWeight)>=p.ingredientQuantity;
            }
            Row("Back to board",RefreshBoard);
            var first=rows.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.interactable);if(first)ActivityUI.Focus(first);
        }
        void PerformChop(PreparationDefinition p,QuickAssignment s){Board.Chop(inventory,player.player,bar.activeKnife,p,s.unitWeight,out string m);status.text=m;bar.SetStatus(m);}
        public void OpenItemActions(QuickAssignment slot)
        {
            var d=inventory.database.Item(slot.itemId);if(d==null||!Begin(inventory.Name(slot.itemId),true))return;
            if(d.itemType==ItemType.Tool)
            {
                Row("Use tool",()=>{string id=d.id;Close();UseTool(id);});return;
            }
            if(d.itemType==ItemType.Ingredient&&!inventory.Identified(d.id))Row("Taste one",()=>
            {
                player.PutAway();player.Taste(d.id,slot.unitWeight,false);status.text=player.Message;
            });
            if(!d.isKeyItem&&d.itemType!=ItemType.Outfit)
            {Row("Feed pig (bin)",()=>Quantities(slot,false));Row("Drop on ground",()=>Quantities(slot,true));}
            Row("Put away",()=>{player.PutAway();Close();});
        }
        void Quantities(QuickAssignment slot,bool drop)
        {
            ClearRows();int max=inventory.Count(slot.itemId,slot.unitWeight),qty=1;
            TMP_Text label=null;var display=Row("Quantity: 1",()=>{});display.interactable=false;label=display.GetComponentInChildren<TMP_Text>();
            Row("− One",()=>{qty=Mathf.Max(1,qty-1);label.text="Quantity: "+qty;});Row("+ One",()=>{qty=Mathf.Min(max,qty+1);label.text="Quantity: "+qty;});
            Row("One stack",()=>{qty=Mathf.Min(max,ItemSizing.Limit(slot.unitWeight));label.text="Quantity: "+qty;});Row("All",()=>{qty=max;label.text="Quantity: "+qty;});
            Row(drop?"Confirm drop":"Confirm feed pig",()=>
            {
                if(drop){player.Remove(slot.itemId,slot.unitWeight,qty,true);bar.SetStatus(player.Message);Close();}
                else{Close();if(!feeding.Begin(slot.itemId,slot.unitWeight,qty))bar.SetStatus("Pig unavailable, or items protected. Nothing consumed.");}
            });
        }
        void UseTool(string id)
        {
            var d=inventory.database.Item(id);if(d==null)return;
            if(d.toolKind==ToolKind.Basket){var node=ForageNode.Nearest(player.player.position);if(node){node.Harvest(player.player,out string m);bar.SetStatus(m);}else bar.SetStatus("No reachable forage nearby.");}
            else if(d.toolKind==ToolKind.Knife)
            {
                var board=FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None).Where(b=>Vector2.Distance(player.player.position,b.transform.position)<=b.reach).OrderBy(b=>Vector2.Distance(player.player.position,b.transform.position)).FirstOrDefault();
                if(board){bar.activeKnife=id;OpenBoard(board);}else bar.SetStatus("Use this knife at a chopping board.");
            }
            else bar.SetStatus("Protective gloves prevent harvesting exposure while assigned to this activity bar.");
        }
        public void OpenShop(Shopkeeper shop)
        {
            if(Vector2.Distance(player.player.position,shop.transform.position)>shop.reach||!Begin("Shop · Cashola "+inventory.store.Data.cashola,true))return;
            foreach(var offer in shop.offers)
            {
                var o=offer;if(inventory.database.Item(o.itemId)==null)continue;
                Row("Buy "+inventory.Name(o.itemId)+" · "+o.buyPrice,()=>{shop.Trade(player.player,o,1,true,0,out string m);status.text=m;title.text="Shop · Cashola "+inventory.store.Data.cashola;});
                foreach(var entry in inventory.store.Data.inventory.Where(e=>e.itemId==o.itemId).ToArray())
                {int w=inventory.Weight(entry);Row("Sell "+inventory.Name(o.itemId)+" · "+o.sellPrice+" ("+ItemSizing.Size(w)+")",()=>{shop.Trade(player.player,o,1,false,w,out string m);status.text=m;title.text="Shop · Cashola "+inventory.store.Data.cashola;});}
            }
            foreach(var t in inventory.store.Data.tools)
            {string id=t.itemId;var d=inventory.database.Item(id);if(d?.toolKind==ToolKind.Knife&&!d.ultimate&&!d.ultimatePlus)Row("Sharpen "+d.displayName+" · "+shop.sharpeningPrice,()=>{shop.Sharpen(player.player,id,out string m);status.text=m;title.text="Shop · Cashola "+inventory.store.Data.cashola;});}
        }
        public void OpenSharpening(SharpeningDevice device)
        {
            if(Vector2.Distance(player.player.position,device.transform.position)>device.reach||!Begin(device.rotatingStone?"Rotating stone":"Whetstone",true))return;
            foreach(var t in inventory.store.Data.tools)
            {var d=inventory.database.Item(t.itemId);if(d?.toolKind!=ToolKind.Knife||d.ultimate||d.ultimatePlus)continue;var record=t;Row(d.displayName+" · "+Mathf.RoundToInt(t.sharpness)+"/"+d.maximumSharpness,()=>StartSharpening(device,d,record));}
        }
        void StartSharpening(SharpeningDevice device,ItemDefinition definition,ToolRecord record)
        {
            if(record.sharpness>=definition.maximumSharpness){status.text="Already sharp.";return;}
            ClearRows();rows.parent.gameObject.SetActive(false);stone=device;knife=definition;knifeState=record;elapsed=0;position=.5f;seed=UnityEngine.Random.Range(0f,1000f);
            duration=Mathf.Max(3,knife.sharpeningSeconds)*(stone.rotatingStone?2:1);startingSkill=CulinarySkills.Level(inventory.store.Data,"Sharpening",inventory.database);
            track=ActivityUI.Rect("Tracking surface",panel,new Vector2(.1f,.30f),new Vector2(.9f,.83f),new Color(.27f,.29f,.27f));
            var backdrop=ActivityUI.Rect("Character back",panel,new Vector2(.02f,.18f),new Vector2(.22f,.48f),Color.white).GetComponent<Image>();backdrop.sprite=stone.headAndBack;backdrop.preserveAspect=true;backdrop.enabled=backdrop.sprite;
            var surface=ActivityUI.Rect("Stone",panel,new Vector2(.35f,.18f),new Vector2(.65f,.31f),new Color(.5f,.5f,.48f)).GetComponent<Image>();if(stone.stoneArt){surface.sprite=stone.stoneArt;surface.preserveAspect=true;}
            knifeArt=ActivityUI.Rect("Knife",surface.transform,new Vector2(.4f,.05f),new Vector2(.6f,.95f),Color.white);var art=knifeArt.GetComponent<Image>();art.sprite=knife.icon;art.preserveAspect=true;art.enabled=art.sprite;
            segments=new RectTransform[40];for(int i=0;i<segments.Length;i++){segments[i]=ActivityUI.Rect("Guide",track,Vector2.zero,Vector2.zero,new Color(.35f,1,.55f));segments[i].pivot=new Vector2(0,.5f);}
            cursor=ActivityUI.Rect("Tracking cursor",track,Vector2.zero,Vector2.zero,Color.yellow);cursor.sizeDelta=new Vector2(16,16);
            performance=ActivityUI.Label("Accuracy",ActivityUI.Rect("Performance",panel,new Vector2(.1f,.10f),new Vector2(.9f,.19f),Color.clear),"",16);
            if(stone.workingSound){workingAudio=canvas.gameObject.AddComponent<AudioSource>();workingAudio.clip=stone.workingSound;workingAudio.loop=true;workingAudio.Play();}
            status.text="Steer left/right along the green line. Close keeps earned sharpness and XP.";
        }
        float Target(float t)
        {
            float frequency=Mathf.Max(.15f,knife.sharpeningFrequency);float wave=Mathf.Sin(seed+t*frequency)*.62f+Mathf.Sin(seed*.43f+t*frequency*2.17f)*.28f+Mathf.Sin(t*.47f+seed)*.1f;
            return Mathf.Clamp01(.5f+wave*Mathf.Clamp(knife.sharpeningVariance,.05f,1)*.43f);
        }
        void Update()
        {
            if(!canvas)return;var controls=RuntimeControls.Current;
            if(controls&&controls.SuppressInput)return;
            if(Time.frameCount>openedFrame&&controls&&controls.Pressed("System/Cancel")){if(Board&&!AcceptBarAction)RefreshBoard();else Close();return;}
            if(!stone)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);elapsed+=dt;position=Mathf.Clamp01(position+(controls?controls.Axis("System/SharpenSteer"):0)*dt*.75f);
            float accuracy=SharpeningRules.Accuracy(position-Target(elapsed),startingSkill);
            knifeState.sharpness=Mathf.Min(knife.maximumSharpness,knifeState.sharpness+SharpeningRules.Restoration(accuracy,dt,stone.restorationPerSecond,startingSkill));
            CulinarySkills.Add(inventory.store.Data,"Sharpening",Mathf.Max(0,stone.xpPerSecond)*accuracy*dt);
            float w=track.rect.width,h=track.rect.height;
            for(int i=0;i<segments.Length;i++)
            {
                float y=i/(float)segments.Length,ny=(i+1)/(float)segments.Length;
                Vector2 a=new Vector2(Target(elapsed+y*4)*w,y*h),b=new Vector2(Target(elapsed+ny*4)*w,ny*h),v=b-a;
                segments[i].anchoredPosition=a;segments[i].sizeDelta=new Vector2(v.magnitude,3);segments[i].localEulerAngles=new Vector3(0,0,Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);
            }
            cursor.anchoredPosition=new Vector2(position*w,0);cursor.GetComponent<Image>().color=Color.Lerp(Color.red,Color.green,accuracy);
            knifeArt.anchoredPosition=new Vector2((position-.5f)*60,Mathf.Sin(elapsed*5)*12);
            performance.text=$"Accuracy {accuracy:P0} · Sharpness {knifeState.sharpness:F1}/{knife.maximumSharpness} · {Mathf.Max(0,duration-elapsed):F0}s";
            if(elapsed>=duration||knifeState.sharpness>=knife.maximumSharpness){bar.SetStatus("Sharpening complete. Progress saved in the current session.");Close();}
        }
        public void Close()
        {
            if(!canvas)return;if(stone)inventory.store.NotifyChanged();stone=null;Board=null;AcceptBarAction=false;knife=null;knifeState=null;
            Destroy(canvas.gameObject);canvas=null;gate.Unlock(this);
            if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(oldFocus&&oldFocus.activeInHierarchy?oldFocus:null);
        }
        void OnDisable(){Close();}
    }
}
