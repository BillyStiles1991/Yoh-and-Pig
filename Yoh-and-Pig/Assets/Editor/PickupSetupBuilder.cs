using FreezerGameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PickupSetupBuilder
{
    const string Root = "Assets/YohPickups/Assets/";
    [MenuItem("Tools/Freezer Game/Add Pickup Feedback To Selected Player")]
    static void Build()
    {
        var player = Selection.activeGameObject;
        if (!player || !player.scene.IsValid() || !player.GetComponent<PlayerControlGate>())
        { Debug.LogError("Select the gameplay Player with PlayerControlGate in the Hierarchy."); return; }
        if (player.GetComponent<PickupFeedback>())
        { Debug.LogWarning("Pickup feedback already exists. Edit its Inspector assignments.", player); return; }
        var store = Object.FindAnyObjectByType<GameProgressStore>();
        if (!store) { Debug.LogError("Add the gameplay progression store before building the feedback."); return; }
        var key = SpriteAsset("KeyBadge");
        var badge = SpriteAsset("CountBadge");
        var glowSprite = SpriteAsset("DiscoveryGlow");
        var collect = AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Collect.wav");
        var discovery = AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Discovery.wav");
        if (!key || !badge || !glowSprite || !collect || !discovery)
        { Debug.LogError("Copy all five starter assets to "+Root+" before running the builder."); return; }

        var feedback = Undo.AddComponent<PickupFeedback>(player);
        var hands = player.GetComponent<PickupHands>();
        if (!hands) hands = Undo.AddComponent<PickupHands>(player);
        Undo.RecordObject(hands,"Connect pickup feedback");
        hands.feedback = feedback;
        hands.animator = player.GetComponentInChildren<Animator>();
        hands.playerCollider = player.GetComponent<Collider2D>();
        if (!hands.playerCollider) hands.playerCollider = player.GetComponentInChildren<Collider2D>();
        feedback.store = store;
        feedback.audioSource = Undo.AddComponent<AudioSource>(player);
        feedback.audioSource.playOnAwake = false;
        feedback.pickupSound = collect; feedback.discoverySound = discovery;

        var hud = Node("Pickup HUD", null, true);
        var canvas = hud.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=8;
        var scaler=hud.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var panel=Rect("Toast",hud.transform, new Vector2(1,1),new Vector2(-235,-108),new Vector2(430,104));
        var backing=panel.gameObject.AddComponent<Image>(); backing.color=new Color(.13f,.095f,.08f,.91f); backing.raycastTarget=false;
        feedback.toast=panel.gameObject.AddComponent<CanvasGroup>(); feedback.toast.alpha=0; feedback.toast.blocksRaycasts=false;
        feedback.icon=Picture("Icon",panel,new Vector2(0,.5f),new Vector2(61,0),new Vector2(64,64),null);
        feedback.nameLabel=Label("Item name",panel,new Vector2(0,.5f),new Vector2(260,0),new Vector2(300,86),26);
        feedback.keyBadge=Picture("Key",feedback.icon.transform,Vector2.one,new Vector2(0,4),new Vector2(24,24),key);
        feedback.countBadge=Picture("Quantity badge",feedback.icon.transform,Vector2.one,new Vector2(3,4),new Vector2(44,23),badge);
        feedback.countLabel=Label("Quantity",feedback.countBadge.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,23),15);
        feedback.countLabel.alignment=TextAlignmentOptions.Center;
        var anchor=Node("Held item anchor",player.transform,false); anchor.transform.localPosition=new Vector3(0,1.25f,0);
        var playerArt=player.GetComponentInChildren<SpriteRenderer>();
        feedback.glow=Node("Glow",anchor.transform,false).AddComponent<SpriteRenderer>();
        feedback.glow.sprite=glowSprite; feedback.glow.transform.localScale=Vector3.one*(1.2f/glowSprite.bounds.size.x);
        feedback.heldIcon=Node("Held item",anchor.transform,false).AddComponent<SpriteRenderer>();
        if(playerArt)
        {
            feedback.glow.sortingLayerID=feedback.heldIcon.sortingLayerID=playerArt.sortingLayerID;
            feedback.glow.sortingOrder=playerArt.sortingOrder+1;
        }
        feedback.heldIcon.sortingOrder=feedback.glow.sortingOrder+1;
        feedback.heldIcon.enabled=feedback.glow.enabled=false;
        EditorUtility.SetDirty(hands); EditorUtility.SetDirty(feedback);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject=player;
    }
    static Sprite SpriteAsset(string name)
    {
        string path=Root+name+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) return null;
        if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
        {
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static GameObject Node(string name,Transform parent,bool ui)
    {
        var go=ui ? new GameObject(name,typeof(RectTransform)) : new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go,"Build pickup feedback"); go.transform.SetParent(parent,false); return go;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size)
    {
        var rt=(RectTransform)Node(name,parent,true).transform;
        rt.anchorMin=rt.anchorMax=anchor; rt.sizeDelta=size; rt.anchoredPosition=offset; return rt;
    }
    static Image Picture(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Sprite sprite)
    {
        var image=Rect(name,parent,anchor,offset,size).gameObject.AddComponent<Image>();
        image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false; return image;
    }
    static TMP_Text Label(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,float fontSize)
    {
        var label=Rect(name,parent,anchor,offset,size).gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize=fontSize; label.alignment=TextAlignmentOptions.MidlineLeft; label.raycastTarget=false; return label;
    }
}
