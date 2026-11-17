using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace FreezerGameplay
{
    [DefaultExecutionOrder(-100)]
    public class RuntimeControls : MonoBehaviour
    {
        public PlayerInput player;public InputActionAsset inputAsset;
        public static RuntimeControls Current {get;private set;}
        public InputActionAsset Actions {get;private set;}
        public bool IsRebinding=>rebind!=null;
        int consumedFrame=-1;
        public bool SuppressInput=>IsRebinding||SettingsOpen||consumedFrame==Time.frameCount;
        InputActionRebindingExtensions.RebindingOperation rebind;
        GameObject settings;Action cancel;GameObject previousFocus;TMP_Text speechPreview;float speechPreviewStart;
        readonly List<InputActionReference> uiReferences=new List<InputActionReference>();
        InputActionReference Reference(string path){var r=InputActionReference.Create(Actions.FindAction(path));uiReferences.Add(r);return r;}
        const string Key="Yoh.Bindings.v1";
        void Awake()
        {
            Current=this;if(!player)player=GetComponent<PlayerInput>();
            var source=inputAsset?inputAsset:player?player.actions:null;if(!source){Debug.LogError("Assign RuntimeControls input asset.",this);return;}
            Actions=Instantiate(source);LoadOverrides(Actions);Actions.Enable();
        }
        void Start(){ApplyToLiveActions();}
        public bool Pressed(string path)=>consumedFrame!=Time.frameCount&&!IsRebinding&&Actions!=null&&(Actions.FindAction(path,false)?.WasPressedThisFrame()??false);
        public string Display(string path)=>Actions?.FindAction(path,false)?.GetBindingDisplayString()??path;
        public float Axis(string path)=>!IsRebinding&&Actions!=null?(Actions.FindAction(path,false)?.ReadValue<float>()??0):0;
        public static void LoadOverrides(InputActionAsset asset)
        {if(asset&&PlayerPrefs.HasKey(Key)){try{asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(Key));}catch(Exception e){Debug.LogWarning("Could not read control bindings: "+e.Message);}}}
        public void ApplyToLiveActions()
        {
            foreach(var reference in uiReferences)if(reference)Destroy(reference);uiReferences.Clear();
            if(player&&player.actions){player.actions.RemoveAllBindingOverrides();LoadOverrides(player.actions);}
            foreach(var ui in FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    ui.actionsAsset=Actions;
                    ui.move=Reference("UI/Navigate");
                    ui.submit=Reference("UI/Submit");
                    ui.cancel=Reference("UI/Cancel");
                    ui.point=Reference("UI/Point");
                    ui.leftClick=Reference("UI/Click");
                    ui.scrollWheel=Reference("UI/ScrollWheel");
                }
        }
        void Save(){PlayerPrefs.SetString(Key,Actions.SaveBindingOverridesAsJson());PlayerPrefs.Save();ApplyToLiveActions();}
        public void Rebind(InputAction action,int index,Action done)
        {
            if(IsRebinding)return;bool enabled=action.enabled;action.Disable();
            cancel=()=>{rebind?.Cancel();};
            rebind=action.PerformInteractiveRebinding(index).WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta")
                .OnCancel(op=>{consumedFrame=Time.frameCount;op.Dispose();rebind=null;cancel=null;if(enabled)action.Enable();done?.Invoke();})
                .OnComplete(op=>{consumedFrame=Time.frameCount;op.Dispose();rebind=null;cancel=null;if(enabled)action.Enable();Save();done?.Invoke();});
            if(action.bindings[index].isPartOfComposite)rebind.WithExpectedControlType("Button");
            var groups=action.bindings[index].groups??"";
            if(groups.Contains("Gamepad"))rebind.WithControlsHavingToMatchPath("<Gamepad>");
            else if(groups.Contains("Keyboard"))rebind.WithControlsExcluding("<Gamepad>");
            rebind.Start();
        }
        public void ShowSettings()
        {
            if(!Actions)return;
            if(settings){settings.SetActive(true);return;}
            previousFocus=UnityEngine.EventSystems.EventSystem.current?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;
            var canvas=ActivityUI.Canvas("Control settings",250);settings=canvas.gameObject;
            var panel=ActivityUI.Rect("Settings",canvas.transform,new Vector2(.12f,.06f),new Vector2(.88f,.94f),new Color(.96f,.92f,.84f));
            var title=ActivityUI.Rect("Title",panel,new Vector2(0,.86f),Vector2.one,Color.clear);ActivityUI.Label("Text",title,"Controls and quick-bar settings",24);
            var list=ActivityUI.List(panel);
            UnityEngine.UI.Button auto=null;auto=ActivityUI.Button("",list,()=>{QuickBarRules.AutoEnabled=!QuickBarRules.AutoEnabled;auto.GetComponentInChildren<TMP_Text>().text="Found items: "+(QuickBarRules.AutoEnabled?"Auto-assign quick bars":"Inventory only");});
            auto.GetComponentInChildren<TMP_Text>().text="Found items: "+(QuickBarRules.AutoEnabled?"Auto-assign quick bars":"Inventory only");
            UnityEngine.UI.Button speed=null;speed=ActivityUI.Button("",list,()=>{IntroPrefs.Set("speed",(Mathf.Clamp(IntroPrefs.Int("speed",1),0,2)+1)%3);speed.GetComponentInChildren<TMP_Text>().text="Speech speed: "+new[]{"Slow","Normal","Fast"}[Mathf.Clamp(IntroPrefs.Int("speed",1),0,2)];speechPreviewStart=Time.unscaledTime;});
            speed.GetComponentInChildren<TMP_Text>().text="Speech speed: "+new[]{"Slow","Normal","Fast"}[Mathf.Clamp(IntroPrefs.Int("speed",1),0,2)];
            var sample=ActivityUI.Rect("Speech preview",list,Vector2.zero,Vector2.one,Color.clear);sample.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=65;
            speechPreview=ActivityUI.Label("Preview",sample,"The kitchen smells of fresh bread.",20);speechPreviewStart=Time.unscaledTime;
            UnityEngine.UI.Button font=null;font=ActivityUI.Button("",list,()=>{IntroPrefs.Set("font",IntroPrefs.Int("font")==0?1:0);font.GetComponentInChildren<TMP_Text>().text="Preferred font: "+(IntroPrefs.Int("font")==1?"Alternate":"Regular / unlocked NPC font");});font.GetComponentInChildren<TMP_Text>().text="Preferred font: "+(IntroPrefs.Int("font")==1?"Alternate":"Regular / unlocked NPC font");
            UnityEngine.UI.Button access=null;access=ActivityUI.Button("",list,()=>{IntroPrefs.Set("dyslexia",IntroPrefs.Int("dyslexia")==1?0:1);access.GetComponentInChildren<TMP_Text>().text="Accessibility font: "+(IntroPrefs.Int("dyslexia")==1?"On":"Off");});access.GetComponentInChildren<TMP_Text>().text="Accessibility font: "+(IntroPrefs.Int("dyslexia")==1?"On":"Off");
            foreach(var map in Actions.actionMaps)foreach(var action in map.actions)for(int i=0;i<action.bindings.Count;i++)
            {
                if(action.bindings[i].isComposite)continue;int index=i;var a=action;
                var button=ActivityUI.Button("",list,()=>{});var label=button.GetComponentInChildren<TMP_Text>();
                Action refresh=()=>{if(label)label.text=map.name+" / "+a.name+" "+a.bindings[index].name+": "+a.GetBindingDisplayString(index);};refresh();
                button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(IsRebinding)return;label.text="Press a key/button (Escape cancels)…";Rebind(a,index,refresh);});
                var f=button.gameObject.AddComponent<ActivityListFocus>();f.scroll=list.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            }
            ActivityUI.Button("Reset all bindings",list,()=>{if(IsRebinding)return;Actions.RemoveAllBindingOverrides();Save();CloseSettings();ShowSettings();});
            var closeArea=ActivityUI.Rect("Footer",panel,new Vector2(.25f,.02f),new Vector2(.75f,.10f),Color.clear);var close=ActivityUI.Button("Back",closeArea,CloseSettings);ActivityUI.Focus(close);
        }
        public bool SettingsOpen=>settings;
        public void CloseSettings(){consumedFrame=Time.frameCount;cancel?.Invoke();if(settings)Destroy(settings);settings=null;if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(previousFocus&&previousFocus.activeInHierarchy?previousFocus:null);}
        void Update()
        {
            if(settings&&speechPreview)
            {
                speechPreview.maxVisibleCharacters=(int)((Time.unscaledTime-speechPreviewStart)*IntroPrefs.SpeechCharactersPerSecond)%(speechPreview.text.Length+12);
                var view=FindAnyObjectByType<DialoguePresentation>();var title=FindAnyObjectByType<PlayableTitle>();var content=view?view.settingsContent:title?title.content:null;
                if(content)speechPreview.font=IntroPrefs.Int("dyslexia")==1?(content.dyslexiaFont?content.dyslexiaFont:TMP_Settings.defaultFontAsset):IntroPrefs.Int("font")==1?(content.alternateFont?content.alternateFont:TMP_Settings.defaultFontAsset):(content.regularFont?content.regularFont:TMP_Settings.defaultFontAsset);
            }
            if(settings&&!IsRebinding&&Pressed("System/Cancel"))CloseSettings();
        }
        void OnDestroy(){cancel?.Invoke();foreach(var r in uiReferences)if(r)Destroy(r);if(settings)Destroy(settings);if(Actions)Destroy(Actions);if(Current==this)Current=null;}
    }
}
