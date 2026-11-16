using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FreezerGameplay
{
    public static class ActivityUI
    {
        public static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=color.a>0;return r;
        }
        public static TMP_Text Label(string name,Transform parent,string text,int size=20)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,3);r.offsetMax=new Vector2(-8,-3);
            var label=go.AddComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;label.color=ActivityTheme.Current?ActivityTheme.Current.ink:new Color(.16f,.10f,.07f);if(ActivityTheme.Current&&ActivityTheme.Current.font)label.font=ActivityTheme.Current.font;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
        public static Button Button(string text,Transform parent,Action click)
        {
            var r=Rect(text,parent,Vector2.zero,Vector2.one,new Color(.85f,.76f,.58f));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();if(ActivityTheme.Current){var art=r.GetComponent<Image>();art.color=ActivityTheme.Current.button;if(ActivityTheme.Current.buttonSprite){art.sprite=ActivityTheme.Current.buttonSprite;art.type=Image.Type.Sliced;}}var colors=b.colors;colors.selectedColor=new Color(1,.8f,.35f);colors.highlightedColor=colors.selectedColor;b.colors=colors;Label("Label",r,text,18);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight=48;b.onClick.AddListener(()=>click());return b;
        }
        public static RectTransform List(Transform parent)
        {
            var area=Rect("Scroll",parent,new Vector2(.04f,.12f),new Vector2(.96f,.83f),Color.clear);var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
            var viewport=Rect("Viewport",area,Vector2.zero,Vector2.one,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Rows",viewport,new Vector2(0,1),Vector2.one,Color.clear);content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=7;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;scroll.viewport=viewport;return content;
        }
        public static Canvas Canvas(string name,int order)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;return c;
        }
        public static void Focus(Button button){if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);}
    }
}
