using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FreezerGameplay
{
    [DefaultExecutionOrder(-40)]
    public class QuickBarController:MonoBehaviour
    {
        public InventoryController inventory;public ItemActions player;public PlayerControlGate gate;public ActivityWindow window;
        public bool preferTop;public bool automaticPlacement=true;public float minimumMoveInterval=1;
        public string activeKnife;
        Canvas canvas;RectTransform panel;CanvasGroup group;TMP_Text[] labels=new TMP_Text[10];Image[] icons=new Image[10],backs=new Image[10];
        TMP_Text status,tooltip,heading;Image sicknessFill;float movedAt;bool top;int hover=-1;string message="";
        public int Index=>inventory.store.Data.activeBar*10+inventory.store.Data.selectedQuickSlot;
        public QuickAssignment Selected=>inventory.store.Data.quickSlots.Find(s=>s.slot==Index);
        public bool CanControl=>gate&&(!gate.IsLocked||(window&&window.Board!=null&&gate.OwnedBy(window)))&&!(RuntimeControls.Current&&RuntimeControls.Current.SuppressInput);
        void Start(){Build();top=preferTop;Place(top);ChooseKnife();}
        void Build()
        {
            canvas=ActivityUI.Canvas("Quick bars",60);panel=ActivityUI.Rect("Quick bar",canvas.transform,new Vector2(.1f,.02f),new Vector2(.9f,.19f),new Color(.16f,.12f,.09f,.92f));group=panel.gameObject.AddComponent<CanvasGroup>();
            for(int i=0;i<10;i++)
            {
                int k=i;float x=.008f+i*.099f;
                var r=ActivityUI.Rect("Slot "+(i+1),panel,new Vector2(x,.24f),new Vector2(x+.092f,.88f),new Color(.9f,.82f,.66f));backs[i]=r.GetComponent<Image>();
                var button=r.gameObject.AddComponent<Button>();button.targetGraphic=backs[i];button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>{if(CanControl)Select(k);});
                var ir=ActivityUI.Rect("Icon",r,new Vector2(.22f,.28f),new Vector2(.78f,.94f),Color.white);icons[i]=ir.GetComponent<Image>();icons[i].preserveAspect=true;icons[i].raycastTarget=false;
                var lr=ActivityUI.Rect("Label",r,new Vector2(0,0),new Vector2(1,.30f),Color.clear);labels[i]=ActivityUI.Label("Text",lr,"",12);
                var tip=r.gameObject.AddComponent<QuickBarSlotHover>();tip.bar=this;tip.index=i;
            }
            heading=ActivityUI.Label("Heading",ActivityUI.Rect("Heading",panel,new Vector2(0,.88f),Vector2.one,Color.clear),"",13);heading.color=Color.white;
            status=ActivityUI.Label("Status",ActivityUI.Rect("Status",panel,Vector2.zero,new Vector2(1,.24f),Color.clear),"",13);status.color=Color.white;
            tooltip=ActivityUI.Label("Tooltip",ActivityUI.Rect("Tooltip",canvas.transform,new Vector2(.2f,.25f),new Vector2(.8f,.38f),new Color(.96f,.92f,.83f,.96f)),"",18);tooltip.transform.parent.gameObject.SetActive(false);
            var meter=ActivityUI.Rect("Sickness background",panel,new Vector2(.8f,.02f),new Vector2(.99f,.07f),Color.gray);
            sicknessFill=ActivityUI.Rect("Sickness",meter,Vector2.zero,Vector2.one,new Color(.4f,.8f,.15f)).GetComponent<Image>();
        }
        public void SetStatus(string text){message=text;}
        public void Hover(int index,bool show){hover=show?index:-1;}
        public void Select(int index)
        {
            inventory.store.Data.selectedQuickSlot=(index+10)%10;var s=Selected;
            if(s==null){player.PutAway();return;}
            var d=inventory.database.Item(s.itemId);
            if(d?.toolKind==ToolKind.Knife)activeKnife=d.id;
            if(window&&window.Board){window.RefreshBoard();return;}
            if(inventory.Count(s.itemId)>0){player.Hold(s.itemId,s.unitWeight);message=player.Message;}else player.PutAway();
        }
        public void ChangeBar(int delta)
        {
            int count=QuickBarRules.Bars(inventory);inventory.store.Data.activeBar=(inventory.store.Data.activeBar+delta+count)%count;ChooseKnife();Select(inventory.store.Data.selectedQuickSlot);
        }
        public void ChooseKnife()
        {
            var knives=inventory.store.Data.quickSlots.Where(s=>s.slot/10==inventory.store.Data.activeBar&&QuickBarRules.ToolSlot(s.slot)&&inventory.Count(s.itemId)>0&&inventory.database.Item(s.itemId)?.toolKind==ToolKind.Knife).Select(s=>s.itemId).ToArray();
            if(!knives.Contains(activeKnife))activeKnife=knives.FirstOrDefault();
        }
        public bool Usable(QuickAssignment s)
        {
            if(s==null||inventory.Count(s.itemId)<1)return false;
            if(window&&window.Board)
            {
                var d=inventory.database.Item(s.itemId);if(d?.toolKind==ToolKind.Knife)return true;
                return inventory.database.preparations.Any(p=>p.ingredientId==s.itemId&&p.deviceId==window.Board.deviceId&&ChoppingBoard.Quality(inventory,activeKnife,p)>=0);
            }
            return true;
        }
        void Update()
        {
            if(!canvas||!inventory)return;var input=RuntimeControls.Current;
            inventory.store.Data.activeBar=Mathf.Clamp(inventory.store.Data.activeBar,0,QuickBarRules.Bars(inventory)-1);
            if(CanControl&&input)
            {
                if(input.Pressed("QuickBar/PreviousSlot"))Select(inventory.store.Data.selectedQuickSlot-1);
                else if(input.Pressed("QuickBar/NextSlot"))Select(inventory.store.Data.selectedQuickSlot+1);
                if(input.Pressed("QuickBar/NextBar"))ChangeBar(1);else if(input.Pressed("QuickBar/PreviousBar"))ChangeBar(-1);
                if(window&&(!window.Board||window.AcceptBarAction)&&input.Pressed("System/ActionMenu"))
                {
                    var s=Selected;if(s!=null&&Usable(s))
                    {
                        if(window.Board!=null){if(inventory.database.Item(s.itemId)?.toolKind==ToolKind.Knife){activeKnife=s.itemId;window.RefreshBoard();}else window.ShowPreparations(s);}
                        else window.OpenItemActions(s);
                    }
                }
            }
            bool enabled=CanControl;group.alpha=enabled?1:.6f;group.interactable=enabled;group.blocksRaycasts=true;
            heading.text=$"Bar {inventory.store.Data.activeBar+1}/{QuickBarRules.Bars(inventory)} · 2 tools / 8 items · Cashola {inventory.store.Data.cashola}";
            for(int i=0;i<10;i++)
            {
                var s=inventory.store.Data.quickSlots.Find(x=>x.slot==inventory.store.Data.activeBar*10+i);var d=s==null?null:inventory.database.Item(s.itemId);
                bool use=Usable(s);icons[i].sprite=d==null?null:inventory.Icon(d.id);icons[i].enabled=icons[i].sprite;icons[i].color=use?Color.white:new Color(.4f,.4f,.4f,.5f);
                labels[i].text=d==null?(i<2?"Tool":"Item"):inventory.Name(d.id)+(d.itemType==ItemType.Tool?"":" ×"+inventory.Count(d.id,s.unitWeight));
                backs[i].color=i==inventory.store.Data.selectedQuickSlot?new Color(1,.75f,.3f):use?new Color(.9f,.82f,.66f):new Color(.5f,.48f,.43f);
            }
            status.text=message;sicknessFill.rectTransform.anchorMax=new Vector2(inventory.store.Data.sicknessLevel/100,1);
            tooltip.transform.parent.gameObject.SetActive(hover>=0);
            if(hover>=0){var s=inventory.store.Data.quickSlots.Find(x=>x.slot==inventory.store.Data.activeBar*10+hover);var d=s==null?null:inventory.database.Item(s.itemId);tooltip.text=d==null?"Empty slot":inventory.Name(d.id)+"\n"+(inventory.Identified(d.id)?d.description:d.unidentifiedDescription);}
            Reposition();
        }
        void Reposition()
        {
            if(!automaticPlacement||Time.unscaledTime-movedAt<minimumMoveInterval)return;
            var camera=Camera.main;Vector3 p=camera?camera.WorldToViewportPoint(player.transform.position):new Vector3(.5f,.5f,0);
            var topRect=new Rect(.1f,.81f,.8f,.17f);var bottomRect=new Rect(.1f,.02f,.8f,.17f);
            float a=preferTop?0:1,b=preferTop?1:0;
            if(topRect.Contains(p))a+=10;if(bottomRect.Contains(p))b+=10;
            foreach(var zone in HudProtectedArea.Active)if(zone){if(zone.viewportRect.Overlaps(topRect))a+=20;if(zone.viewportRect.Overlaps(bottomRect))b+=20;}
            bool next=a==b?top:a<b;if(next!=top)Place(next);
        }
        void Place(bool value)
        {
            top=value;movedAt=Time.unscaledTime;panel.anchorMin=new Vector2(.1f,top?.81f:.02f);panel.anchorMax=new Vector2(.9f,top?.98f:.19f);panel.offsetMin=panel.offsetMax=Vector2.zero;
            var tip=(RectTransform)tooltip.transform.parent;tip.anchorMin=new Vector2(.2f,top?.64f:.22f);tip.anchorMax=new Vector2(.8f,top?.77f:.35f);
        }
        void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
    }
}
