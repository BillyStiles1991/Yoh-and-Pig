using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayableTitle : MonoBehaviour
{
    public IntroContent content;
    public Camera kitchenCamera;
    enum Page { Home, Load, Confirm, Settings, Audio, Language, Font, Speed, Keys, Access, Music, Credits, Recipes, Quit }
    Page page;
    Stack<Page> history=new Stack<Page>();
    Canvas canvas;
    RectTransform layer, list;
    CanvasGroup homeControls;
    IntroKitchenRoom room;
    TMP_Text hint,details,marquee,preview,playLabel;
    Button first;
    AudioSource music;
    List<IntroTrack> unlocked=new List<IntroTrack>();
    int track, recipe=-1, slot, capture=-1;
    float previousScale, previewStart, scrollStart;
    bool walking, leaving, ownsPause, shuffle, playing;
    string message="";
    static string L(string en,string el)=>IntroPrefs.L(en,el);
    Color Accent => new[]{new Color(.62f,.32f,.15f),new Color(.12f,.35f,.60f),new Color(.30f,.26f,.60f),new Color(.55f,.23f,.28f)}[Mathf.Clamp(IntroPrefs.Int("palette"),0,3)];
    void Start()
    {
        Time.timeScale=1f;
        if(!content || content.pairs==null || content.pairs.Length!=3 || content.pairs.Any(x=>x==null)){
            Debug.LogError("Assign IntroContent with three kitchen pairs.",this);enabled=false;return;
        }
        if(!kitchenCamera)kitchenCamera=Camera.main;
        if(!kitchenCamera){var g=new GameObject("Intro camera");kitchenCamera=g.AddComponent<Camera>();g.AddComponent<AudioListener>();}
        kitchenCamera.rect=new Rect(0,0,.7f,1);kitchenCamera.orthographic=true;
        kitchenCamera.transform.SetPositionAndRotation(new Vector3(0,0,-10),Quaternion.identity);
        kitchenCamera.clearFlags=CameraClearFlags.SolidColor;kitchenCamera.backgroundColor=new Color(.12f,.09f,.08f);
        if(!EventSystem.current){var g=new GameObject("Title EventSystem",typeof(EventSystem));g.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();}
        var roomRoot=new GameObject("Playable kitchen");room=IntroRoomFactory.Build(roomRoot.transform,content);
        var ui=new GameObject("IntroCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
        var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.ignoreListenerPause=true;
        message=IntroNewGame.Error??TitleSlotLoader.TakeFailure()??"";IntroNewGame.Error=null;
        Draw();
    }
    RectTransform Box(string name,Transform parent,float x,float y,float right,float top,Color? colour=null)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
        r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(right,top);r.offsetMin=r.offsetMax=Vector2.zero;
        if(colour.HasValue)r.gameObject.AddComponent<Image>().color=colour.Value;
        return r;
    }
    TMP_Text Text(RectTransform parent,string value,int size=30)
    {
        var r=Box("Text",parent,0,0,1,1);r.offsetMin=new Vector2(8,2);r.offsetMax=new Vector2(-8,-2);
        var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.alignment=TextAlignmentOptions.Center;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=14;t.fontSizeMax=size;t.raycastTarget=false;t.richText=false;
        var font=IntroPrefs.Int("dyslexia")==1?content.dyslexiaFont:IntroPrefs.Int("font")==1?content.alternateFont:content.regularFont;
        if(font)t.font=font;return t;
    }
    Button Btn(RectTransform p,string label,Action action)
    {
        var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p.GetComponent<Image>();
        var colours=b.colors;colours.selectedColor=new Color(1,.85f,.50f);colours.highlightedColor=colours.selectedColor;b.colors=colours;
        Text(p,label);b.onClick.AddListener(()=>{if(!leaving && (capture<0 || label==L("Back","Πίσω") || label==L("Main Menu","Αρχικό μενού")))action();});if(!first)first=b;return b;
    }
    Button At(string label,Action action,float x,float y,float r,float t,RectTransform parent=null)
        =>Btn(Box(label,parent?parent:layer,x,y,r,t,Accent),label,action);
    void Row(string label,Action action)
    {
        var r=Box(label,list,0,0,1,1,Accent);r.gameObject.AddComponent<LayoutElement>().preferredHeight=53;Btn(r,label,action);
    }
    void PauseKitchen(bool pause)
    {
        if(pause && !ownsPause){previousScale=Time.timeScale;Time.timeScale=0;ownsPause=true;}
        if(!pause && ownsPause){Time.timeScale=previousScale;ownsPause=false;}
        room.CanWalk=page==Page.Home && walking && !leaving;room.hero.linearVelocity=Vector2.zero;
    }
    void OnDestroy(){if(ownsPause)Time.timeScale=previousScale;}
    void Go(Page next){history.Push(page);page=next;message="";Draw();}
    void Back(){if(leaving)return;capture=-1;page=history.Count>0?history.Pop():Page.Home;message="";Draw();}
    void Home(){if(leaving)return;history.Clear();page=Page.Home;walking=false;message="";Draw();}
    void Focus(){EventSystem.current?.SetSelectedGameObject(walking&&page==Page.Home?null:first?first.gameObject:null);}
    void Draw()
    {
        if(layer){layer.gameObject.SetActive(false);Destroy(layer.gameObject);}
        first=null;hint=null;details=null;marquee=null;preview=null;playLabel=null;
        layer=Box("Title page",canvas.transform,0,0,1,1);
        PauseKitchen(page!=Page.Home);
        if(page==Page.Home)
        {
            var panel=Box("Main menu — right 30%",layer,.7f,0,1,1,new Color(.16f,.105f,.08f));
            homeControls=panel.gameObject.AddComponent<CanvasGroup>();
            At(L("New Game","Νέο παιχνίδι"),NewGame,.08f,.64f,.92f,.75f,panel);
            At(L("Load Game","Φόρτωση"),()=>Go(Page.Load),.08f,.50f,.92f,.61f,panel);
            At(L("Settings","Ρυθμίσεις"),()=>Go(Page.Settings),.08f,.36f,.92f,.47f,panel);
            At(L("Close Game","Έξοδος"),()=>Go(Page.Quit),.08f,.22f,.92f,.33f,panel);
            // Footer actions are outside the four primary rows.
            At(L("Music","Μουσική"),()=>Go(Page.Music),.012f,.012f,.14f,.074f);
            At(L("Recipes","Συνταγές"),()=>Go(Page.Recipes),.71f,.012f,.84f,.074f);
            At(L("Credits","Συντελεστές"),()=>Go(Page.Credits),.85f,.012f,.99f,.074f);
            hint=Text(Box("Control hint",layer,.01f,.89f,.69f,.99f,new Color(0,0,0,.7f)),"",24);
            details=Text(Box("Kitchen status",layer,.016f,.09f,.685f,.19f,new Color(0,0,0,.72f)),"",22);
            var switchButton = hint.transform.parent.gameObject.AddComponent<Button>();
            switchButton.targetGraphic = switchButton.GetComponent<Image>();
            switchButton.onClick.AddListener(() => { walking = !walking; message = ""; SetControl(); });
            switchButton.navigation = new Navigation { mode = Navigation.Mode.None };
            SetControl();return;
        }
        var background=Box("Page background",layer,0,0,1,1,new Color(.12f,.09f,.085f,.98f));
        if(page==Page.Credits && content.creditsBackground){var im=background.GetComponent<Image>();im.sprite=content.creditsBackground;im.color=Color.white;}
        Text(Box("Heading",layer,.1f,.87f,.9f,.99f),Title(),43);
        list=Box("Options",layer,.12f,.23f,.88f,.86f);
        var v=list.gameObject.AddComponent<VerticalLayoutGroup>();v.spacing=4;v.childForceExpandHeight=false;v.childControlHeight=true;v.childAlignment=TextAnchor.MiddleCenter;
        Body();
        details=Text(Box("Message",layer,.06f,.14f,.94f,.22f),message,23);
        At(L("Back","Πίσω"),Back,.04f,.035f,.24f,.12f);
        At(L("Main Menu","Αρχικό μενού"),Home,.72f,.035f,.96f,.12f);
        Focus();
    }
    void SetControl()
    {
        room.CanWalk=walking && !leaving;
        homeControls.interactable=!walking;homeControls.alpha=walking?.82f:1f;
        foreach(var b in layer.GetComponentsInChildren<Button>())b.interactable=!walking || b.transform == hint.transform.parent;
        Focus();
    }
    string Title()
    {
        string[] en={"","Load Game","Load this save?","Settings","Audio","Language","Font","Text display speed","Keyboard mapping","Accessibility","Music player","Credits","Random recipe","Close game?"};
        string[] el={"","Φόρτωση","Φόρτωση αποθήκευσης;","Ρυθμίσεις","Ήχος","Γλώσσα","Γραμματοσειρά","Ταχύτητα κειμένου","Πλήκτρα","Προσβασιμότητα","Μουσική","Συντελεστές","Τυχαία συνταγή","Έξοδος;"};
        return L(en[(int)page],el[(int)page]);
    }
    void NewGame()
    {
        try{
            slot=IntroNewGame.FirstFree();if(slot==0)throw new Exception(L("All eight slots are occupied.","Και οι οκτώ θέσεις είναι γεμάτες."));
            CheckScene(content.gameplayScene);
            if(room.Baked && string.IsNullOrWhiteSpace(content.rewardItemId))throw new Exception("Set Reward Item Id.");
            leaving=true;PauseKitchen(false);IntroNewGame.StartRun(content.gameplayScene,slot,room.Baked?content.rewardItemId:null);
        }catch(Exception e){leaving=false;message=e.Message;}
    }
    void CheckScene(string scene)
    {
        if(SceneManager.GetActiveScene().buildIndex<0||scene==SceneManager.GetActiveScene().name||!Application.CanStreamedLevelBeLoaded(scene))
            throw new Exception(L("Check title and gameplay scenes in Build Profiles.","Έλεγξε τις σκηνές στο Build Profiles."));
    }
    int rebindFrame;
    int loadPage;
    void Body()
    {
        switch(page){
        case Page.Load:
            for(int i=0;i<4;i++) SlotRow(loadPage*4+i+1);
            Row(loadPage==0?"5–8 →":"← 1–4",()=>{loadPage=1-loadPage;Draw();});break;
        case Page.Confirm:
            Row(L("Load slot ","Φόρτωση θέσης ")+slot,()=>{
                if(TitleSlotLoader.Begin(slot,out var msg)){leaving=true;PauseKitchen(false);}
                else {message=msg;Draw();}
            });break;
        case Page.Quit: Row(L("Confirm exit","Έξοδος"),()=>{Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
        });break;
        case Page.Settings:
            Row(L("Audio","Ήχος"),()=>Go(Page.Audio));Row(L("Language","Γλώσσα"),()=>Go(Page.Language));
            Row(L("Font","Γραμματοσειρά"),()=>Go(Page.Font));Row(L("Text display speed","Ταχύτητα κειμένου"),()=>Go(Page.Speed));
            Row(L("Keyboard mapping","Πλήκτρα"),()=>Go(Page.Keys));Row(L("Accessibility","Προσβασιμότητα"),()=>Go(Page.Access));break;
        case Page.Audio:
            Row("−",()=>Volume(-1));Row("+",()=>Volume(1));message=L("Master volume ","Ένταση ")+IntroPrefs.Int("volume",8)*10+"%";break;
        case Page.Language:
            Row("English",()=>Option("greek",0));Row("Ελληνικά",()=>Option("greek",1));break;
        case Page.Font:
            Row(L("Default font","Βασική γραμματοσειρά"),()=>Option("font",0));
            Row(L("Alternate font","Εναλλακτική γραμματοσειρά"),()=>FontOption(false));break;
        case Page.Speed:
            for(int i=0;i<3;i++){int value=i;Row(L(new[]{"Slow","Medium","Fast"}[i],new[]{"Αργά","Μέτρια","Γρήγορα"}[i]),()=>Option("speed",value));}
            preview=Text(Box("Live preview",layer,.12f,.20f,.88f,.32f),"",29);previewStart=Time.unscaledTime;break;
        case Page.Keys:
            string[] labels={L("Up","Πάνω"),L("Down","Κάτω"),L("Left","Αριστερά"),L("Right","Δεξιά"),L("Interact","Χρήση"),L("Switch focus","Εναλλαγή")};
            for(int i=0;i<labels.Length;i++){int k=i;Row(labels[i]+"  ·  "+IntroPrefs.Binding(i),()=>{capture=k;rebindFrame=Time.frameCount;message=L("Press a key. Esc cancels.","Πάτησε πλήκτρο. Esc για ακύρωση.");});}
            break;
        case Page.Access:
            string[] modes={L("Standard","Τυπικό"),L("Protanopia","Πρωτανωπία"),L("Deuteranopia","Δευτερανωπία"),L("Tritanopia","Τριτανωπία")};
            for(int i=0;i<4;i++){int n=i;Row((IntroPrefs.Int("palette")==i?"✓ ":"")+modes[i],()=>Option("palette",n));}
            Row(L("Dyslexia font","Γραμματοσειρά δυσλεξίας")+(IntroPrefs.Int("dyslexia")==1?" ✓":""),()=>FontOption(true));break;
        case Page.Credits: Credits();break;
        case Page.Recipes:
            Row(L("Show a recipe","Εμφάνιση συνταγής"),PickRecipe);
            if(recipe>=0){var r=content.recipes[recipe];message="";
                Text(Box("Recipe card",layer,.09f,.18f,.91f,.60f,new Color(.12f,.1f,.09f)),L(r.titleEn,r.titleEl)+"\n"+L(r.instructionsEn,r.instructionsEl),27);
                list.anchorMin=new Vector2(.12f,.6f);}
            break;
        case Page.Music: MusicPage();break;
        }
    }
    void SlotRow(int n)
    {
        SaveData data=null;string error=null;
        try{data=SaveRepository.Read(n);}catch(Exception e){error=e.Message;}
        var row=Box("Slot "+n,list,0,0,1,1,Accent);row.gameObject.AddComponent<LayoutElement>().preferredHeight=83;
        var b=row.gameObject.AddComponent<Button>();b.targetGraphic=row.GetComponent<Image>();if(!first)first=b;
        var view=row.gameObject.AddComponent<SaveSlotView>();view.button=b;
        view.preview=Box("Screenshot",row,.01f,.04f,.17f,.96f).gameObject.AddComponent<RawImage>();
        view.slotLabel=Text(Box("Number",row,.18f,.5f,.30f,1),"",18);view.saveName=Text(Box("Name",row,.30f,.45f,.7f,1),"",26);
        view.savedDate=Text(Box("Date",row,.7f,.45f,1,1),"",20);view.location=Text(Box("Location",row,.18f,0,.68f,.48f),"",20);
        view.playTime=Text(Box("Time",row,.68f,0,1,.48f),"",20);view.Show(n,data,error);
        if(IntroPrefs.Int("greek")==1){view.slotLabel.text="Θέση "+n;if(data==null)view.saveName.text=error==null?"Κενή":"Μη αναγνώσιμη";}
        b.onClick.AddListener(()=>{if(leaving)return;if(data==null){message=error??L("Empty slot","Κενή θέση");return;}slot=n;Go(Page.Confirm);});
    }
    void Option(string key,int value){IntroPrefs.Set(key,value);Draw();}
    void Volume(int delta){IntroPrefs.Set("volume",Mathf.Clamp(IntroPrefs.Int("volume",8)+delta,0,10));IntroPrefs.ApplyAudio();Draw();}
    void FontOption(bool dys){
        if(!(dys?content.dyslexiaFont:content.alternateFont)){message=L("Font asset required.","Απαιτείται αρχείο γραμματοσειράς.");return;}
        Option(dys?"dyslexia":"font",dys?1-IntroPrefs.Int("dyslexia"):1);
    }
    void PickRecipe()
    {
        var choices=Enumerable.Range(0,Math.Min(30,content.recipes.Length)).Where(i=>content.recipes[i]!=null&&!string.IsNullOrWhiteSpace(content.recipes[i].titleEn)).ToList();
        if(choices.Count==0){message=L("No recipes entered yet.","Δεν υπάρχουν συνταγές ακόμη.");return;}
        if(choices.Count>1)choices.Remove(recipe);recipe=choices[UnityEngine.Random.Range(0,choices.Count)];Draw();
    }
    void Credits()
    {
        var area=Box("Credits scroll",layer,.1f,.23f,.9f,.86f,new Color(0,0,0,.7f));area.gameObject.AddComponent<RectMask2D>();
        var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.viewport=area;scroll.horizontal=false;
        list=Box("Credits content",area,0,1,1,1);list.pivot=new Vector2(.5f,1);
        var layout=list.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.spacing=6;
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=list;
        var card=Box("Thank you and credits",list,0,0,1,1);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight=Mathf.Max(100,(content.credits.Split('\n').Length+4)*36);
        Text(card,L(content.thanks,content.thanksEl)+"\n\n"+content.credits,28);
        foreach(var link in content.links){if(link==null)continue;var url=link.httpsUrl;Row(link.label,()=>{if(Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme=="https")Application.OpenURL(uri.AbsoluteUri);});}
        creditsScroll=scroll;
    }
    ScrollRect creditsScroll;
    void MusicPage()
    {
        list.anchorMax=new Vector2(.88f,.68f);
        unlocked=content.tracks.Where(t=>t!=null && t.clip && (t.availableAtStart||PlayerPrefs.HasKey("intro.music."+t.id))).ToList();
        if(unlocked.Count==0){message=L("No tracks unlocked yet.","Δεν υπάρχουν διαθέσιμα κομμάτια.");return;}
        track=Mathf.Clamp(track,0,unlocked.Count-1);
        marquee=Text(Box("Track title",layer,.2f,.71f,.8f,.81f),"",33);
        Row(L("Previous","Προηγούμενο"),()=>StepTrack(-1));
        var play=Box("Play or Stop",list,0,0,1,1,Accent);play.gameObject.AddComponent<LayoutElement>().preferredHeight=53;
        var b=Btn(play,"",()=>{if(playing){music.Stop();playing=false;}else PlayTrack();});playLabel=b.GetComponentInChildren<TMP_Text>();
        Row(L("Next","Επόμενο"),()=>StepTrack(1));Row(L("Shuffle","Τυχαία σειρά")+(shuffle?" ✓":""),()=>{shuffle=!shuffle;Draw();});
    }
    void StepTrack(int d){
        int count=unlocked.Count;if(count==0)return;
        int step=shuffle&&count>1?UnityEngine.Random.Range(1,count):d;
        track=(track+step+count)%count;scrollStart=Time.unscaledTime;if(playing)PlayTrack();
    }
    void PlayTrack(){music.clip=unlocked[track].clip;music.Play();playing=true;scrollStart=Time.unscaledTime;}
    static string Window(string title,int offset){
        var s=new StringInfo(title??"");int count=s.LengthInTextElements;if(count<=25)return title;
        var loop=new StringInfo(title+"   "+title+"   ");int start=(count+3-offset%(count+3))%(count+3);
        return loop.SubstringByTextElements(start,25);
    }
    void Update()
    {
        if(!room||leaving)return;
        kitchenCamera.orthographicSize=Mathf.Max(4.5f,8f/Mathf.Max(.1f,(float)kitchenCamera.pixelWidth/Mathf.Max(1,kitchenCamera.pixelHeight)));
        var k=Keyboard.current;var pad=Gamepad.current;
        if(capture>=0 && (pad?.buttonEast.wasPressedThisFrame??false)){capture=-1;Draw();return;}
        if(capture>=0&&k!=null){
            if(Time.frameCount==rebindFrame)return;
            if(k.escapeKey.wasPressedThisFrame){capture=-1;message="";Draw();return;}
            foreach(var key in k.allKeys)if(key.wasPressedThisFrame){
                if(key.keyCode==Key.Enter ||key.keyCode==Key.Space||Enumerable.Range(0,6).Any(i=>i!=capture&&IntroPrefs.Binding(i)==key.keyCode)){
                    message=L("Choose an unused key.","Επίλεξε ελεύθερο πλήκτρο.");break;}
                IntroPrefs.Set("key"+capture,(int)key.keyCode);capture=-1;message="";Draw();break;
            }
            if(details)details.text=message;return;
        }
        if(page==Page.Home){
            if(IntroPrefs.Pressed(5)||(pad?.startButton.wasPressedThisFrame??false)){walking=!walking;message="";SetControl();}
            hint.text=(walking?L("Kitchen controls","Χειρισμός κουζίνας"):L("Menu controls","Χειρισμός μενού"))+"    ·    "+IntroPrefs.Binding(5)+" / Menu  →  "+(walking?L("Select menu","Επιλογές"):L("Play kitchen","Παίξε στην κουζίνα"));
            int near=room.Nearest();details.text=message!=""?message:walking?(near>=0?IntroPrefs.Binding(4)+" / X: "+room.StationName(near)+"    ":"")+L("Holding: ","Κρατάς: ")+room.Hands+"    "+room.Products+"\n"+room.Notice:room.Notice;
        }else{
            if((k?.escapeKey.wasPressedThisFrame??false)||(pad?.buttonEast.wasPressedThisFrame??false)){Back();return;}
            if(details)details.text=message;
            if(page==Page.Credits && creditsScroll){float y=(pad?.rightStick.ReadValue().y??0)+(k?.pageUpKey.isPressed==true?1:0)-(k?.pageDownKey.isPressed==true?1:0);creditsScroll.verticalNormalizedPosition=Mathf.Clamp01(creditsScroll.verticalNormalizedPosition+y*Time.unscaledDeltaTime);}
            if(preview){string sample=L("The kitchen smells of fresh bread.","Η κουζίνα μυρίζει φρέσκο ψωμί.");preview.text=sample;preview.maxVisibleCharacters=(int)((Time.unscaledTime-previewStart)*new[]{10,22,40}[IntroPrefs.Int("speed",1)])%(sample.Length+12);}
            if(marquee)marquee.text=Window(unlocked[track].title,(int)((Time.unscaledTime-scrollStart)*4));
            if(playLabel)playLabel.text=playing?L("Stop","Διακοπή"):L("Play","Αναπαραγωγή");
        }
        if(playing&&!music.isPlaying&&unlocked.Count>0){StepTrack(1);}
    }
}
