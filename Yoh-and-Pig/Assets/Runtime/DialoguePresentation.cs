using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace FreezerGameplay
{
    // Screen-space rendering, world-space head anchors; all timing remains live while paused.
    public class DialoguePresentation:MonoBehaviour
    {
        public IntroContent settingsContent;public Camera worldCamera;
        public Sprite bubbleSprite;public Color paper=new Color(.98f,.95f,.86f),ink=new Color(.15f,.1f,.07f);
        public Vector2 normalBubbleSize=new Vector2(440,210),keyBubbleSize=new Vector2(520,230),portraitSize=new Vector2(300,390);
        public float headGap=18,safeMargin=16,fontSize=23;
        Canvas canvas;RectTransform root,bubble,portraitRect,viewport,content;Image portrait;Animator portraitAnimator;
        TMP_Text speaker,body,indicator,rankLabel;ScrollRect scroll;DialogueClickSurface clickSurface;DialogueTail tail;
        NpcDialogueAnchor anchor;NpcProfileDefinition profile;NpcReviewRecord review;NpcExpressionDefinition expression;
        Action advance;float visible;int count;bool opened;Vector2 mouth;
        public bool IsRevealing=>opened&&body&&body.maxVisibleCharacters<count;
        public void Show(string who,string text,NpcProfileDefinition npc,NpcReviewRecord record,NpcDialogueAnchor target,string expressionId,string response,Action onAdvance)
        {
            if(!canvas)Build();profile=npc;review=record;anchor=target;advance=onAdvance;opened=true;canvas.gameObject.SetActive(true);
            expression=profile?.expressions==null?null:Array.Find(profile.expressions,e=>e!=null&&e.id==expressionId&&!string.IsNullOrEmpty(expressionId));
            speaker.text=who??"";body.text=text??"";body.maxVisibleCharacters=int.MaxValue;
            indicator.text=string.IsNullOrEmpty(response)?"▼":"▼ "+response;
            bool key=profile!=null&&review!=null&&review.keyNpcUnlocked;portraitRect.gameObject.SetActive(key);
            bubble.sizeDelta=key?keyBubbleSize:normalBubbleSize;
            var font=ChooseFont();speaker.font=font;body.font=font;indicator.font=font;rankLabel.font=font;
            if(key)
            {
                portrait.sprite=expression!=null&&expression.portrait?expression.portrait:profile.portrait;
                mouth=expression!=null&&expression.overrideMouthPosition?expression.mouthPosition:profile.mouthPosition;
                var anim=review.animatedBannerUnlocked?(expression!=null&&expression.animatedBanner?expression.animatedBanner:profile.animatedBanner):null;
                portraitAnimator.runtimeAnimatorController=anim;portraitAnimator.enabled=anim;
                if(anim){portraitAnimator.Rebind();portraitAnimator.Update(0);}
                portrait.enabled=portrait.sprite||anim;
            }
            rankLabel.text=profile!=null&&review!=null?$"Review level {NpcReviewRules.Level(profile,review.points)} · {review.points} points":"";
            Layout();Canvas.ForceUpdateCanvases();Measure();body.ForceMeshUpdate();count=body.textInfo.characterCount;
            visible=0;body.maxVisibleCharacters=0;content.anchoredPosition=Vector2.zero;indicator.gameObject.SetActive(count==0);

        }
        TMP_FontAsset ChooseFont()
        {
            // Accessibility must never fall through to a decorative NPC font.
            if(IntroPrefs.Int("dyslexia")==1)return settingsContent&&settingsContent.dyslexiaFont?settingsContent.dyslexiaFont:TMP_Settings.defaultFontAsset;
            if(IntroPrefs.Int("font")==1)return settingsContent&&settingsContent.alternateFont?settingsContent.alternateFont:TMP_Settings.defaultFontAsset;
            if(profile!=null&&review!=null&&review.customFontUnlocked&&profile.customFont)return profile.customFont;
            return settingsContent&&settingsContent.regularFont?settingsContent.regularFont:TMP_Settings.defaultFontAsset;
        }
        void Build()
        {
            canvas=ActivityUI.Canvas("NPC dialogue",210);root=(RectTransform)canvas.transform;
            tail=new GameObject("Speech tail",typeof(RectTransform),typeof(CanvasRenderer),typeof(DialogueTail)).GetComponent<DialogueTail>();
            tail.transform.SetParent(root,false);var tr=(RectTransform)tail.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;tail.raycastTarget=false;tail.color=paper;
            bubble=ActivityUI.Rect("Speech bubble",root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),paper);bubble.pivot=Vector2.zero;
            var bg=bubble.GetComponent<Image>();if(bubbleSprite){bg.sprite=bubbleSprite;bg.type=Image.Type.Sliced;}
            clickSurface=bubble.gameObject.AddComponent<DialogueClickSurface>();clickSurface.clicked=()=>advance?.Invoke();
            speaker=ActivityUI.Label("Speaker",ActivityUI.Rect("Speaker row",bubble,new Vector2(.05f,.80f),new Vector2(.95f,.97f),Color.clear),"",22);speaker.color=ink;speaker.alignment=TextAlignmentOptions.Left;
            rankLabel=ActivityUI.Label("Review",ActivityUI.Rect("Review row",bubble,new Vector2(.05f,.69f),new Vector2(.95f,.81f),Color.clear),"",13);rankLabel.color=ink;rankLabel.alignment=TextAlignmentOptions.Left;
            var sr=ActivityUI.Rect("Speech scroll",bubble,new Vector2(.05f,.19f),new Vector2(.95f,.69f),Color.clear);scroll=sr.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.inertia=false;
            viewport=ActivityUI.Rect("Viewport",sr,Vector2.zero,Vector2.one,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Image>().raycastTarget=true;
            content=ActivityUI.Rect("Text content",viewport,new Vector2(0,1),Vector2.one,Color.clear);content.pivot=new Vector2(.5f,1);
            body=ActivityUI.Label("Speech",content,"",23);body.fontSize=fontSize;body.color=ink;body.alignment=TextAlignmentOptions.TopLeft;body.richText=true;body.overflowMode=TextOverflowModes.Overflow;
            scroll.viewport=viewport;scroll.content=content;
            indicator=ActivityUI.Label("Continue",ActivityUI.Rect("Continue row",bubble,new Vector2(.05f,.02f),new Vector2(.95f,.18f),Color.clear),"▼",16);indicator.color=ink;indicator.alignment=TextAlignmentOptions.Right;
            portraitRect=ActivityUI.Rect("Portrait",root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Color.white);portraitRect.pivot=Vector2.zero;
            portrait=portraitRect.GetComponent<Image>();portrait.preserveAspect=true;portrait.raycastTarget=false;portraitAnimator=portraitRect.gameObject.AddComponent<Animator>();portraitAnimator.updateMode=AnimatorUpdateMode.UnscaledTime;
        }
        void Measure()
        {
            float width=Mathf.Max(40,viewport.rect.width-16);float h=body.GetPreferredValues(body.text,width,Mathf.Infinity).y+12;
            content.sizeDelta=new Vector2(0,Mathf.Max(viewport.rect.height,h));
        }
        public void RevealAll(){if(!opened)return;visible=count;body.maxVisibleCharacters=count;FollowText();indicator.gameObject.SetActive(true);}
        public void Hide(){opened=false;advance=null;if(canvas)canvas.gameObject.SetActive(false);}
        void Update()
        {
            if(!opened)return;
            if(IsRevealing){visible+=IntroPrefs.SpeechCharactersPerSecond*Time.unscaledDeltaTime;body.maxVisibleCharacters=Mathf.Min(count,Mathf.FloorToInt(visible));FollowText();}
            indicator.gameObject.SetActive(!IsRevealing);
            if(!IsRevealing)
            {
                var controls=RuntimeControls.Current;float y=controls?controls.Axis("Dialogue/Scroll"):0;
                if(Mathf.Abs(y)>.1f)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+y*Time.unscaledDeltaTime*.6f);
            }
        }
        void FollowText()
        {
            if(body.maxVisibleCharacters<1||body.textInfo.characterCount==0)return;
            int i=Mathf.Min(body.maxVisibleCharacters,body.textInfo.characterCount)-1;
            int line=body.textInfo.characterInfo[i].lineNumber;
            float depth=body.rectTransform.rect.yMax-body.textInfo.lineInfo[line].descender+12;
            float max=Mathf.Max(0,content.rect.height-viewport.rect.height);
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(depth-viewport.rect.height,0,max));
        }
        Vector2 Local(Vector2 screen){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var p);return p;}
        Rect Safe()
        {
            var a=Local(Screen.safeArea.min);var b=Local(Screen.safeArea.max);
            return Rect.MinMaxRect(a.x+safeMargin,a.y+safeMargin,b.x-safeMargin,b.y-safeMargin);
        }
        void LateUpdate(){if(opened)Layout();}
        void Layout()
        {
            var safe=Safe();bool key=profile!=null&&review!=null&&review.keyNpcUnlocked;
            Vector2 size=key?keyBubbleSize:normalBubbleSize;size.x=Mathf.Min(size.x,safe.width);size.y=Mathf.Min(size.y,safe.height);
            bool resized=bubble.sizeDelta!=size;bubble.sizeDelta=size;Vector2 tip;
            if(key)
            {
                Vector2 ps=portraitSize;float scale=Mathf.Min(1,Mathf.Min(safe.width/Mathf.Max(1,ps.x),safe.height/Mathf.Max(1,ps.y)));ps*=scale;
                portraitRect.sizeDelta=ps;portraitRect.anchoredPosition=new Vector2(safe.xMax-ps.x,safe.yMin);
                Vector2 drawn=ps;
                if(portrait.sprite){float aspect=portrait.sprite.rect.width/portrait.sprite.rect.height;if(ps.x/ps.y>aspect)drawn.x=ps.y*aspect;else drawn.y=ps.x/aspect;}
                Vector2 uv=new Vector2(Mathf.Clamp01(mouth.x),Mathf.Clamp01(mouth.y));
                tip=portraitRect.anchoredPosition+(ps-drawn)*.5f+Vector2.Scale(drawn,uv);
            }
            else
            {
                var cam=worldCamera?worldCamera:Camera.main;
                if(anchor&&cam){var screen=cam.WorldToScreenPoint(anchor.Position);tip=screen.z>0?Local(screen):safe.center;}
                else tip=safe.center;
            }
            Vector2 desired=new Vector2(tip.x-size.x-12,tip.y+headGap);
            if(!key&&desired.x<safe.xMin)desired.x=tip.x+12;
            desired.x=Mathf.Clamp(desired.x,safe.xMin,Mathf.Max(safe.xMin,safe.xMax-size.x));
            desired.y=Mathf.Clamp(desired.y,safe.yMin,Mathf.Max(safe.yMin,safe.yMax-size.y));bubble.anchoredPosition=desired;
            Rect box=new Rect(desired,size);tip.x=Mathf.Clamp(tip.x,safe.xMin,safe.xMax);tip.y=Mathf.Clamp(tip.y,safe.yMin,safe.yMax);
            Vector2 edge=new Vector2(Mathf.Clamp(tip.x,box.xMin+20,box.xMax-20),tip.y<box.center.y?box.yMin:box.yMax);
            bool horizontal=true;
            if(tip.y>box.yMin&&tip.y<box.yMax){edge=new Vector2(tip.x<box.center.x?box.xMin:box.xMax,Mathf.Clamp(tip.y,box.yMin+20,box.yMax-20));horizontal=false;}
            tail.Set(edge,tip,horizontal,!box.Contains(tip));
            if(resized&&body){Canvas.ForceUpdateCanvases();Measure();body.ForceMeshUpdate();FollowText();}
        }
        void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
    }
    public class DialogueClickSurface:MonoBehaviour,IPointerClickHandler
    {
        public Action clicked;
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)clicked?.Invoke();}
    }
    public class DialogueTail:MaskableGraphic
    {
        Vector2 edge,tip;bool horizontal,show;
        public void Set(Vector2 a,Vector2 b,bool across,bool visible){edge=a;tip=b;horizontal=across;show=visible;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!show)return;Vector2 offset=horizontal?Vector2.right*12:Vector2.up*12;
            var v=UIVertex.simpleVert;v.color=color;v.position=edge-offset;vh.AddVert(v);v.position=edge+offset;vh.AddVert(v);v.position=tip;vh.AddVert(v);vh.AddTriangle(0,1,2);
        }
    }
}
