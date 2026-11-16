using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace FreezerGameplay
{
    // Add to the existing Inventory page. Builds children only; the existing MenuController owns pause/input.
    public class InventoryPanel : MonoBehaviour
    {
        public InventoryController inventory; public EquipmentController outfits; public ItemActions actions; public RecipeController recipes;
        public Sprite panelSprite, buttonSprite; public Color paper = new Color(.95f,.91f,.83f), ink = new Color(.18f,.12f,.09f);
        RectTransform root, main, sub, content, detail; ScrollRect scroll;
        TMP_Text info, title, status, capacityLabel, quantityLabel; Image portrait, danger, deviceIcon;
        GameObject effect; string selectedId; int selectedWeight, selectedStack, quantity = 1, sort, rarity = -1;
        bool dirty, initialized, filtering; readonly bool[] categories = new bool[7];
        readonly List<InventorySlotView> slots = new List<InventorySlotView>();
        readonly List<Toggle> filters = new List<Toggle>();
        Button back, rarityButton, sortButton; string subMode; bool autoRotate = true, dancing = true; int facing; float turnTime;
        Animator preview; string previewId; RuntimeAnimatorController previewController;
        public bool HasSubpage => sub && sub.gameObject.activeSelf;
        public static InventoryPanel Active { get; private set; }
        void OnEnable()
        {
            Active = this;
            if (!inventory) return;
            inventory.store.Changed += Changed;
            if (!initialized) Build(); dirty = true;
        }
        void OnDisable() { if (inventory) inventory.store.Changed -= Changed; if (Active == this) Active = null; }
        void Changed() { dirty = true; }
        public void Back()
        {
            if (!HasSubpage) return; sub.gameObject.SetActive(false); main.gameObject.SetActive(true); subMode = "";
            if (EventSystem.current && slots.Count > 0) EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
        }
        void LateUpdate()
        {
            if (!initialized) return;
            if (dirty) { dirty = false; Rebuild(); }
            if (subMode == "outfit")
            {
                turnTime += Time.unscaledDeltaTime;
                if (autoRotate && turnTime >= 5) { facing = (facing + 1) % 4; turnTime = 0; }
                UpdatePreview();
            }
        }
        void Build()
        {
            initialized = true;
            root = Box("Inventory UI v1.7", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, paper);
            if (panelSprite) { var bg = root.GetComponent<Image>(); bg.sprite = panelSprite; bg.type = Image.Type.Sliced; }
            main = Box("Inventory", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            capacityLabel = Text("Capacity", main, new Vector2(.02f,.94f), new Vector2(.75f,1), "", 20);
            sortButton = Button("Sort: A–Z", main, new Vector2(.02f,.87f), new Vector2(.29f,.94f), () => { sort = (sort + 1) % 3; sortButton.GetComponentInChildren<TMP_Text>().text = "Sort: " + new[] { "A–Z", "Classification", "Rarity" }[sort]; dirty = true; });
            rarityButton = Button("Rarity: All", main, new Vector2(.3f,.87f), new Vector2(.59f,.94f), () => { rarity = rarity == 5 ? -1 : rarity + 1; rarityButton.GetComponentInChildren<TMP_Text>().text = "Rarity: " + (rarity < 0 ? "All" : ((ItemRarity)rarity).ToString()); dirty = true; });
            string[] names = { "Ingredients", "Unidentified", "Cooked food", "Masterpieces", "Outfits", "Key items", "Items" };
            Button("All", main, new Vector2(.02f,.80f), new Vector2(.13f,.86f), () => { filtering = true; Array.Clear(categories, 0, categories.Length); foreach (var t in filters) t.isOn = false; filtering = false; dirty = true; });
            for (int i = 0; i < names.Length; i++)
            {
                int k = i; float x = .02f + (i % 4) * .145f; float y = i < 4 ? .73f : .66f;
                var rt = Box(names[i], main, new Vector2(x,y), new Vector2(x+.14f,y+.065f), Vector2.zero, Vector2.zero, new Color(.8f,.76f,.66f));
                var toggle = rt.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = rt.GetComponent<Image>();
                var check = Box("Check", rt, new Vector2(0,.15f), new Vector2(.12f,.85f), Vector2.zero, Vector2.zero, Color.green).GetComponent<Image>();
                toggle.graphic = check; toggle.isOn = false; Text("Label", rt, new Vector2(.14f,0), Vector2.one, names[i], 13);
                toggle.onValueChanged.AddListener(v => { categories[k] = v; if (!filtering) dirty = true; }); filters.Add(toggle);
            }
            var area = Box("Slots", main, new Vector2(.02f,.10f), new Vector2(.59f,.65f), Vector2.zero, Vector2.zero, Color.clear);
            scroll = area.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Box("Viewport", area, Vector2.zero, new Vector2(.96f,1), Vector2.zero, Vector2.zero, Color.clear); viewport.gameObject.AddComponent<RectMask2D>();
            content = Box("Content", viewport, new Vector2(0,1), Vector2.one, Vector2.zero, Vector2.zero, Color.clear); content.pivot = new Vector2(.5f,1);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4; grid.cellSize = new Vector2(110,100); grid.spacing = new Vector2(10,15); grid.padding = new RectOffset(10,10,18,10);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var bar = Box("Scrollbar", area, new Vector2(.97f,0), Vector2.one, Vector2.zero, Vector2.zero, new Color(.5f,.45f,.35f));
            var handle = Box("Handle", bar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.8f,.6f,.25f));
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = handle.GetComponent<Image>(); scrollbar.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = scrollbar;
            detail = Box("Item details", main, new Vector2(.62f,.10f), new Vector2(.98f,.94f), Vector2.zero, Vector2.zero, Color.clear);
            portrait = Box("Picture", detail, new Vector2(.25f,.72f), new Vector2(.75f,.97f), Vector2.zero, Vector2.zero, Color.white).GetComponent<Image>(); portrait.preserveAspect = true; portrait.enabled = false;
            title = Text("Name", detail, new Vector2(0,.64f), new Vector2(1,.72f), "Select an item", 23);
            info = Text("Description", detail, new Vector2(0,.29f), new Vector2(1,.64f), "", 16); info.alignment = TextAlignmentOptions.TopLeft;
            danger = Box("Raw danger", detail, new Vector2(0,.88f), new Vector2(.15f,.98f), Vector2.zero, Vector2.zero, Color.white).GetComponent<Image>(); danger.preserveAspect = true; danger.enabled = false;
            deviceIcon = Box("Identification device", detail, new Vector2(.85f,.88f), new Vector2(1,.98f), Vector2.zero, Vector2.zero, Color.white).GetComponent<Image>(); deviceIcon.preserveAspect = true; deviceIcon.enabled = false;
            string[] labels = { "Hold", "Put away", "Taste raw", "Identify", "Cook", "Recipes", "Equip outfit", "View Outfit", "Drop", "Destroy" };
            Action[] acts = { () => Act(() => actions.Hold(selectedId,selectedWeight)), () => { actions.PutAway(); Status("Item put away."); }, () => Act(() => actions.Taste(selectedId,selectedWeight,false)), () => Act(() => actions.Taste(selectedId,selectedWeight,true)), () => OpenRecipes(true), () => OpenRecipes(false), () => Status(outfits.Equip(selectedId) ? "Outfit equipped." : "Select an unlocked outfit."), OpenOutfit, () => ConfirmRemoval(true), () => ConfirmRemoval(false) };
            for (int i=0;i<labels.Length;i++) { int k=i; float x=(i%2)*.51f, y=.23f-(i/2)*.075f; Button(labels[i],detail,new Vector2(x,y),new Vector2(x+.49f,y+.07f),()=>{if(!string.IsNullOrEmpty(selectedId)) acts[k]();}); }
            quantityLabel = Text("Quantity", main, new Vector2(.20f,.025f), new Vector2(.42f,.085f), "Quantity: 1", 17);
            Button("−", main, new Vector2(.02f,.025f),new Vector2(.09f,.085f),()=>SetQuantity(quantity-1));
            Button("+", main, new Vector2(.10f,.025f),new Vector2(.17f,.085f),()=>SetQuantity(quantity+1));
            Button("Stack", main,new Vector2(.43f,.025f),new Vector2(.50f,.085f),()=>SetQuantity(selectedStack));
            Button("All", main,new Vector2(.51f,.025f),new Vector2(.59f,.085f),()=>SetQuantity(inventory.Count(selectedId,selectedWeight)));
            status = Text("Status", root, new Vector2(.02f,0), new Vector2(.98f,.025f), "", 14);
            sub = Box("Subpage",root,new Vector2(0,.03f),Vector2.one,Vector2.zero,Vector2.zero,paper); sub.gameObject.SetActive(false);
        }
        public static RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); var rt=(RectTransform)go.transform; rt.SetParent(parent,false);
            rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=offsetMin;rt.offsetMax=offsetMax;var im=go.GetComponent<Image>();im.color=color;im.raycastTarget=color.a>0;return rt;
        }
        TMP_Text Text(string name,Transform parent,Vector2 min,Vector2 max,string text,int size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;
            var label=go.AddComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;label.color=ink;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
        Button Button(string label,Transform parent,Vector2 min,Vector2 max,Action clicked)
        {
            var rt=Box(label,parent,min,max,new Vector2(2,2),new Vector2(-2,-2),new Color(.83f,.74f,.57f));var im=rt.GetComponent<Image>();if(buttonSprite){im.sprite=buttonSprite;im.type=Image.Type.Sliced;}
            var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=im;Text("Label",rt,Vector2.zero,Vector2.one,label,15);b.onClick.AddListener(()=>clicked());return b;
        }
        bool Included(ItemDefinition d)
        {
            if (rarity>=0 && (int)d.rarity!=rarity) return false;
            if (!categories.Any(x=>x)) return true;
            return (categories[5] && d.isKeyItem) || (categories[4] && d.itemType==ItemType.Outfit) ||
                (categories[1] && !inventory.Identified(d.id)) || (categories[0] && d.itemType==ItemType.Ingredient && inventory.Identified(d.id)) ||
                (categories[2] && d.itemType==ItemType.Food) || (categories[3] && d.itemType==ItemType.Masterpiece) || (categories[6] && d.itemType==ItemType.General);
        }
        void Rebuild()
        {
            var old=EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            var focused=old ? old.GetComponent<InventorySlotView>() : null; string key=focused ? focused.key : null;
            foreach(var slot in slots){slot.gameObject.SetActive(false);Destroy(slot.gameObject);} slots.Clear();
            capacityLabel.text=$"Inventory {inventory.SlotsUsed}/{inventory.Capacity} slots · Chef {inventory.store.Data.chefLevel} · Day {inventory.store.Day}";
            var entries=new List<InventoryEntry>(inventory.store.Data.inventory);
            entries.AddRange(inventory.store.Data.outfits.Select(id=>new InventoryEntry{itemId=id,quantity=1,unitWeight=inventory.Weight(id)}));
            entries=entries.Where(e=>inventory.database.Item(e.itemId)!=null && Included(inventory.database.Item(e.itemId))).ToList();
            entries=(sort==1 ? entries.OrderBy(e=>inventory.database.Item(e.itemId).itemType).ThenBy(e=>inventory.Name(e.itemId)) : sort==2 ? entries.OrderByDescending(e=>inventory.database.Item(e.itemId).rarity).ThenBy(e=>inventory.Name(e.itemId)) : entries.OrderBy(e=>inventory.Name(e.itemId))).ToList();
            foreach(var e in entries)
            {
                int limit=ItemSizing.Limit(inventory.Weight(e));
                for(int n=0;n<e.quantity;n+=limit)
                {
                    string id=e.itemId;int weight=inventory.Weight(e), stack=Math.Min(limit,e.quantity-n);string slotKey=id+":"+weight+":"+n;
                    var b=Button("",content,Vector2.zero,Vector2.one,()=>Focus(id,weight,stack));
                    var slot=b.gameObject.AddComponent<InventorySlotView>();slot.key=slotKey;slot.focused=()=>{Focus(id,weight,stack);EnsureVisible(slot.transform as RectTransform);};slots.Add(slot);
                    var pic=Box("Icon",b.transform,new Vector2(.28f,.32f),new Vector2(.72f,.84f),Vector2.zero,Vector2.zero,Color.white).GetComponent<Image>();pic.sprite=inventory.Icon(id);pic.preserveAspect=true;pic.enabled=pic.sprite;ItemRarityIcon.Decorate(pic,inventory.database.Item(id));
                    Text("Name",b.transform,new Vector2(0,0),new Vector2(1,.31f),inventory.Name(id)+" ×"+stack,13);
                    if(inventory.IsEquipped(id)){var badge=Text("Equipped",b.transform,new Vector2(.75f,.8f),new Vector2(1,1.1f),"E",22);badge.transform.localRotation=Quaternion.Euler(0,0,12);badge.outlineColor=Color.yellow;badge.outlineWidth=.15f;}
                    slot.newLabel=Text("New",b.transform,new Vector2(.02f,.86f),new Vector2(.58f,1.16f),"New",20);slot.newLabel.transform.localRotation=Quaternion.Euler(0,0,12);slot.newLabel.gameObject.SetActive(inventory.IsNew(id));
                }
            }
            Canvas.ForceUpdateCanvases();
            var grid=content.GetComponent<GridLayoutGroup>();float width=scroll.viewport.rect.width;grid.cellSize=new Vector2(Mathf.Max(50,(width-50)/4),100);
            if(key!=null && EventSystem.current){var replacement=slots.Find(x=>x.key==key) ?? slots.FirstOrDefault();EventSystem.current.SetSelectedGameObject(replacement ? replacement.gameObject : sortButton.gameObject);}
            if(!string.IsNullOrEmpty(selectedId)) ShowDetails();
        }
        void Focus(string id,int weight,int count)
        {
            selectedId=id;selectedWeight=weight;selectedStack=count;SetQuantity(1);inventory.Examine(id);ShowDetails();
        }
        void EnsureVisible(RectTransform item)
        {
            EnsureVisibleIn(scroll,item);
        }
        static void EnsureVisibleIn(ScrollRect scroll, RectTransform item)
        {
            Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,item);
            float delta=bounds.min.y<scroll.viewport.rect.yMin ? scroll.viewport.rect.yMin-bounds.min.y : bounds.max.y>scroll.viewport.rect.yMax ? scroll.viewport.rect.yMax-bounds.max.y : 0;
            scroll.content.anchoredPosition+=Vector2.up*delta;
        }
        void ShowDetails()
        {
            var d=inventory.database.Item(selectedId);if(d==null)return;bool known=inventory.Identified(d.id);
            title.text=inventory.Name(d.id);portrait.sprite=inventory.Icon(d.id);portrait.enabled=portrait.sprite;ItemRarityIcon.Decorate(portrait,d);
            if(effect)Destroy(effect);if(known && d.detailEffectPrefab)effect=Instantiate(d.detailEffectPrefab,portrait.transform);
            danger.sprite=d.dangerIcon;danger.enabled=d.dangerousRaw && danger.sprite;deviceIcon.sprite=d.identifyDeviceIcon;deviceIcon.enabled=!known && deviceIcon.sprite;
            int count=KnownRecipes(d.id).Count();string matches=known ? string.Join(", ",d.goesWellWith.Take(3).Select(id=>inventory.Identified(id)?inventory.Name(id):"Unknown ingredient")) : "Unknown";
            info.text=(known?d.description:d.unidentifiedDescription)+"\nSize: "+ItemSizing.Size(selectedWeight)+"\nStack: "+selectedStack+"/"+(d.itemType==ItemType.Outfit?1:ItemSizing.Limit(selectedWeight))+"\nRarity: <color=#"+ColorUtility.ToHtmlStringRGB(RarityColor(d.rarity))+">"+d.rarity+"</color>\nFlavour: "+(known?string.Join(", ",d.flavours.Select(inventory.database.FlavourLabel)):"Unknown")+"\nGoes well with: "+matches+"\nDiscovered recipes: "+(known?count:0)+(d.dangerousRaw?"\n⚠ Dangerous raw":"")+(!known&&!string.IsNullOrEmpty(d.identificationDevice)?"\nDevice: "+d.identificationDevice:"");
        }
        public static Color RarityColor(ItemRarity r) => r==ItemRarity.Uncommon?Color.green:r==ItemRarity.Rare?new Color(.2f,.5f,1):r==ItemRarity.SuperRare||r==ItemRarity.KeyItem?new Color(.8f,.5f,0):r==ItemRarity.HyperRare?new Color(.5f,.6f,.75f):Color.gray;
        IEnumerable<RecipeDefinition> KnownRecipes(string id)=>inventory.store.Data.recipes.Select(inventory.database.Recipe).Where(r=>r!=null&&r.ingredients.Any(a=>a.itemId==id));
        void Status(string s){status.text=s;}
        void Act(Func<bool> f){f();Status(actions.Message);dirty=true;}
        void SetQuantity(int n){quantity=Mathf.Clamp(n,1,Mathf.Max(1,inventory.Count(selectedId,selectedWeight)));quantityLabel.text="Quantity: "+quantity;}
        void OpenSub(string mode)
        {
            foreach(Transform t in sub){t.gameObject.SetActive(false);Destroy(t.gameObject);}main.gameObject.SetActive(false);sub.gameObject.SetActive(true);subMode=mode;
            back=Button("Back to inventory",sub,new Vector2(.02f,.90f),new Vector2(.30f,.98f),Back);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(back.gameObject);
        }
        void ConfirmRemoval(bool drop)
        {
            string id=selectedId;int weight=selectedWeight,n=quantity;
            var def=inventory.database.Item(id);
            if(def==null||def.isKeyItem||def.itemType==ItemType.Outfit||inventory.Count(id,weight)<n){Status("Protected or unavailable items cannot be removed.");return;}
            OpenSub("confirm");Text("Question",sub,new Vector2(.1f,.4f),new Vector2(.9f,.7f),(drop?"Drop ":"Permanently destroy ")+n+" × "+inventory.Name(id)+"?",25);
            Button("Confirm",sub,new Vector2(.3f,.2f),new Vector2(.7f,.32f),()=>{actions.Remove(id,weight,n,drop);Back();Status(actions.Message);});
        }
        void OpenRecipes(bool devicesOnly)
        {
            if(!inventory.Identified(selectedId)){Status("Identify this ingredient to reveal its recipes.");return;}
            OpenSub("recipes");var list=KnownRecipes(selectedId).Where(r=>!devicesOnly||recipes.DeviceAvailable(r)).ToArray();
            var area=Box("Recipe list",sub,new Vector2(.06f,.06f),new Vector2(.94f,.87f),Vector2.zero,Vector2.zero,Color.clear);
            var view=area.gameObject.AddComponent<ScrollRect>();view.horizontal=false;var viewport=Box("Viewport",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();
            var rows=Box("Content",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero,Color.clear);rows.pivot=new Vector2(.5f,1);var layout=rows.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.spacing=6;rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;view.viewport=viewport;view.content=rows;
            foreach(var r in list){var b=Button(r.displayName+" — Cook",rows,Vector2.zero,Vector2.one,()=>{recipes.Craft(r.id);Status(recipes.LastMessage);dirty=true;});b.gameObject.AddComponent<LayoutElement>().preferredHeight=60;var focus=b.gameObject.AddComponent<InventorySlotView>();focus.focused=()=>EnsureVisibleIn(view,b.transform as RectTransform);}
            if(list.Length==0)Text("Empty",sub,new Vector2(.1f,.35f),new Vector2(.9f,.65f),"No discovered recipes match this ingredient and device.",20);
        }
        void OpenOutfit()
        {
            var d=inventory.database.Item(selectedId);if(d==null||d.itemType!=ItemType.Outfit){Status("Select an unlocked outfit.");return;}
            previewId=d.id;OpenSub("outfit");facing=0;turnTime=0;
            var art=Box("Outfit preview",sub,new Vector2(.35f,.30f),new Vector2(.65f,.85f),Vector2.zero,Vector2.zero,Color.white).GetComponent<Image>();art.sprite=d.icon;art.preserveAspect=true;art.enabled=art.sprite;
            preview=art.gameObject.AddComponent<Animator>();preview.runtimeAnimatorController=d.previewAnimator;preview.updateMode=AnimatorUpdateMode.UnscaledTime;
            Text("Outfit",sub,new Vector2(.3f,.85f),new Vector2(.8f,.94f),d.displayName,25);
            Button("Play / stop dance",sub,new Vector2(.1f,.16f),new Vector2(.4f,.25f),()=>dancing=!dancing);
            Button("Auto rotation on/off",sub,new Vector2(.5f,.16f),new Vector2(.9f,.25f),()=>autoRotate=!autoRotate);
            Button("← Turn",sub,new Vector2(.1f,.05f),new Vector2(.3f,.13f),()=>{autoRotate=false;facing=(facing+3)%4;});
            Button("Equip",sub,new Vector2(.4f,.05f),new Vector2(.6f,.13f),()=>Status(outfits.Equip(previewId)?"Outfit equipped.":"Unavailable."));
            Button("Turn →",sub,new Vector2(.7f,.05f),new Vector2(.9f,.13f),()=>{autoRotate=false;facing=(facing+1)%4;});
        }
        void UpdatePreview()
        {
            if(!preview||!preview.runtimeAnimatorController)return;
            var d=inventory.database.Item(previewId);ItemActions.SetBool(preview,d.previewDanceBool,dancing);
            foreach(var p in preview.parameters)if(p.name=="Facing"&&p.type==AnimatorControllerParameterType.Int)preview.SetInteger("Facing",facing);
        }
    }
}
