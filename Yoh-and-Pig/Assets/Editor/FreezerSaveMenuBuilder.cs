#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds a wired menu in the current scene; uses your sprites/font when supplied.
public class FreezerSaveMenuBuilder : EditorWindow
{
    private Sprite panelSprite, slotSprite;
    private TMP_FontAsset font;
    private Color border = new Color(0.77f, 0.43f, 0.25f);
    private Color paper = new Color(0.95f, 0.92f, 0.94f);
    private Color ink = new Color(0.20f, 0.14f, 0.12f);
    [MenuItem("Tools/Freezer Saves/Create Save Menu")]
    private static void Open() { GetWindow<FreezerSaveMenuBuilder>("Freezer Saves"); }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Four-slot save menu", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Optional: choose the same panel/button sprites and TMP font as your existing menu. Run in Edit mode. All generated objects support Undo.", MessageType.Info);
        panelSprite = (Sprite)EditorGUILayout.ObjectField("Menu frame sprite", panelSprite, typeof(Sprite), false);
        slotSprite = (Sprite)EditorGUILayout.ObjectField("Slot frame sprite", slotSprite, typeof(Sprite), false);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("Menu font", font, typeof(TMP_FontAsset), false);
        if (GUILayout.Button("Create and wire menu")) Build();
    }
    private GameObject Obj(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Create save menu");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }
    private RectTransform Rect(GameObject go, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = (RectTransform)go.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = offsetMin; r.offsetMax = offsetMax; return r;
    }
    private void Fill(GameObject go) { Rect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); }
    private Image AddImage(GameObject go, Color colour, Sprite sprite = null)
    {
        var image = go.AddComponent<Image>(); image.color = colour;
        if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
        return image;
    }
    private TMP_Text Text(string name, Transform parent, string value, int size, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var go = Obj(name, parent); var label = go.AddComponent<TextMeshProUGUI>();
        label.text = value; label.fontSize = size; label.alignment = align; label.color = ink; label.raycastTarget = false;
        label.richText = false; if (font != null) label.font = font; return label;
    }
    private Button Button(string name, Transform parent, string title)
    {
        var go = Obj(name, parent); var image = AddImage(go, paper, slotSprite);
        var button = go.AddComponent<Button>(); button.targetGraphic = image;
        var label = Text("Label", go.transform, title, 23, TextAlignmentOptions.Center); Fill(label.gameObject);
        var colors = button.colors; colors.selectedColor = new Color(1f, .83f, .5f); colors.highlightedColor = colors.selectedColor; button.colors = colors;
        return button;
    }
    private void SetReference(Object component, string field, Object value)
    {
        var serialized = new SerializedObject(component); var property = serialized.FindProperty(field);
        if (property != null) { property.objectReferenceValue = value; serialized.ApplyModifiedProperties(); }
    }
    private void InteriorIndicator(Button button)
    {
        var glow = Obj("SelectionGlow", button.transform); Fill(glow);
        AddImage(glow, new Color(1f, .68f, .18f, .18f)).raycastTarget = false;
        var outline = glow.AddComponent<Outline>(); outline.effectColor = new Color(1f, .65f, .1f, 1f); outline.effectDistance = new Vector2(3, -3);
        var indicator = button.gameObject.AddComponent<TabIndicator>();
        var serialized = new SerializedObject(indicator); serialized.FindProperty("style").enumValueIndex = 1;
        serialized.FindProperty("interiorGlow").objectReferenceValue = glow; serialized.ApplyModifiedProperties(); glow.SetActive(false);
    }
    private void OutsideIndicator(Button button, Transform panel)
    {
        var root = Obj("ExitIndicator", panel); var r = (RectTransform)root.transform; r.sizeDelta = new Vector2(24, 24);
        var icon = Obj("Icon", root.transform); Fill(icon); var image = AddImage(icon, ink, Arrow()); image.type = Image.Type.Simple; image.raycastTarget = false;
        // Generated sprite points up: suitable for a below-border indicator.
        var indicator = button.gameObject.AddComponent<TabIndicator>();
        var serialized = new SerializedObject(indicator);
        serialized.FindProperty("menuBorder").objectReferenceValue = panel;
        serialized.FindProperty("indicatorRoot").objectReferenceValue = r;
        serialized.FindProperty("side").enumValueIndex = 1; serialized.ApplyModifiedProperties(); root.SetActive(false);
    }
    private Sprite Arrow()
    {
        const string directory = "Assets/FreezerSaveGenerated";
        const string path = directory + "/SelectionArrow.png";
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets", "FreezerSaveGenerated");
        if (!File.Exists(path))
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                t.SetPixel(x, y, y >= 3 && y <= 13 && Mathf.Abs(x - 7.5f) <= (14-y)*0.65f ? Color.white : Color.clear);
            t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); DestroyImmediate(t); AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point; importer.spritePixelsPerUnit = 100; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/Freezer Saves/Create Character Journal In Selected UI")]
    private static void CreateJournal()
    {
        var parent = Selection.activeTransform as RectTransform;
        if (parent == null) { EditorUtility.DisplayDialog("Select a page", "Select the RectTransform of your Character Records page first.", "OK"); return; }
        var builder = CreateInstance<FreezerSaveMenuBuilder>();
        builder.BuildJournal(parent); DestroyImmediate(builder);
    }
    private void BuildJournal(RectTransform parent)
    {
        var page = Obj("CharacterJournal",parent); Fill(page);
        var view = page.AddComponent<CharacterJournalView>(); view.progress=FindAnyObjectByType<GameProgressStore>();
        var dropdown=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        Undo.RegisterCreatedObjectUndo(dropdown,"Create journal dropdown");dropdown.transform.SetParent(page.transform,false);
        dropdown.name="DiscoveredCharacters";Rect(dropdown,new Vector2(0,1),Vector2.one,new Vector2(30,-75),new Vector2(-30,-20));
        view.characterDropdown=dropdown.GetComponent<TMP_Dropdown>();
        string[] names={"Conversations","Quests","Profile"};
        for(int i=0;i<3;i++)
        {
            var b=Button(names[i],page.transform,names[i]);
            Rect(b.gameObject,new Vector2(i/3f,1),new Vector2((i+1)/3f,1),new Vector2(20,-140),new Vector2(-20,-90));InteriorIndicator(b);
            if(i==0) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,view.ShowConversations);
            if(i==1) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,view.ShowQuests);
            if(i==2) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,view.ShowProfile);
        }
        var scroll=Obj("RecordScroll",page.transform);Rect(scroll,Vector2.zero,Vector2.one,new Vector2(30,30),new Vector2(-30,-165));
        var scrollRect=scroll.AddComponent<ScrollRect>();scrollRect.horizontal=false;scrollRect.movementType=ScrollRect.MovementType.Clamped;scrollRect.scrollSensitivity=30;
        var viewport=Obj("Viewport",scroll.transform);Fill(viewport);AddImage(viewport,new Color(1,1,1,.01f));viewport.AddComponent<RectMask2D>();
        scrollRect.viewport=(RectTransform)viewport.transform;
        var text=Text("Records",viewport.transform,"No characters discovered yet.",23,TextAlignmentOptions.TopLeft);
        var textRect=Rect(text.gameObject,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);textRect.pivot=new Vector2(.5f,1);
        text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content=textRect;view.content=text;view.recordScroll=scrollRect;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());Selection.activeGameObject=page;
    }
    private void Build()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Stop Play mode", "Create the menu in Edit mode.", "OK"); return; }
        if (FindAnyObjectByType<SaveMenuController>() != null)
        { EditorUtility.DisplayDialog("Menu exists", "A SaveMenuController already exists. Edit it instead of making a duplicate.", "OK"); return; }
        var player = FindAnyObjectByType<PlayerMovement>();
        var confiner = FindAnyObjectByType<Unity.Cinemachine.CinemachineConfiner2D>();
        if (player == null || confiner == null)
        { EditorUtility.DisplayDialog("References needed", "Open the gameplay scene with PlayerMovement and CmCam first.", "OK"); return; }
        int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build freezer save UI");
        var gate = player.GetComponent<PlayerControlGate>(); if (gate == null) gate = Undo.AddComponent<PlayerControlGate>(player.gameObject);
        var manager = new GameObject("SaveSystems"); Undo.RegisterCreatedObjectUndo(manager, "Save systems");
        var progress = manager.AddComponent<GameProgressStore>();
        var save = FindAnyObjectByType<SaveController>(); if (save == null) save = manager.AddComponent<SaveController>();
        Undo.RecordObject(save, "Wire save controller");
        save.progress = progress; save.playerBody = player.GetComponent<Rigidbody2D>(); save.confiner2D = confiner;
        var controller = manager.AddComponent<SaveMenuController>(); controller.controls = gate; controller.saves = save;
        var canvasObject = Obj("SaveMenuCanvas", null); var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        canvasObject.AddComponent<GraphicRaycaster>();
        var scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
        var root = Obj("SaveMenuRoot", canvasObject.transform); Fill(root); AddImage(root, new Color(0, 0, 0, .5f)); controller.root = root;
        var panel = Obj("MenuBorder", root.transform);
        var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(1040, 900);
        AddImage(panel, panelSprite != null ? Color.white : border, panelSprite);
        if (panelSprite == null)
        {
            var inner = Obj("Paper", panel.transform); Rect(inner, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12)); AddImage(inner, paper);
        }
        controller.header = Text("Header", panel.transform, "SAVE GAME", 38, TextAlignmentOptions.Center);
        Rect(controller.header.gameObject, new Vector2(0,1), Vector2.one, new Vector2(20,-80), new Vector2(-20,-15));
        controller.slots = new SaveSlotView[4];
        for (int i=0; i<4; i++)
        {
            var button = Button("Slot"+(i+1), panel.transform, "");
            Rect(button.gameObject, new Vector2(0,1), Vector2.one, new Vector2(40,-245-i*155), new Vector2(-40,-100-i*155));
            var view = button.gameObject.AddComponent<SaveSlotView>(); view.button = button; controller.slots[i] = view;
            var previewFrame = Obj("PreviewFrame", button.transform); Rect(previewFrame, Vector2.zero, new Vector2(0,1), new Vector2(12,12), new Vector2(225,-12)); AddImage(previewFrame, new Color(.23f,.18f,.16f));
            var preview = Obj("Preview", previewFrame.transform); Fill(preview); view.preview = preview.AddComponent<RawImage>(); view.preview.raycastTarget = false;
            preview.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            view.slotLabel = Text("SlotNumber", button.transform, "SLOT "+(i+1), 17); Rect(view.slotLabel.gameObject,new Vector2(0,1),new Vector2(1,1),new Vector2(245,-30),new Vector2(-15,-5));
            view.saveName = Text("SaveName", button.transform, "Save "+(i+1)+" — Empty", 26); Rect(view.saveName.gameObject,new Vector2(0,1),new Vector2(1,1),new Vector2(245,-66),new Vector2(-15,-29));
            view.savedDate = Text("SavedDate",button.transform,"",18); Rect(view.savedDate.gameObject,Vector2.zero,Vector2.one,new Vector2(245,40),new Vector2(-15,-70));
            view.location = Text("Location",button.transform,"",18); Rect(view.location.gameObject,Vector2.zero,Vector2.one,new Vector2(245,12),new Vector2(-275,-100));
            view.playTime = Text("PlayTime",button.transform,"",17,TextAlignmentOptions.MidlineRight); Rect(view.playTime.gameObject,Vector2.zero,Vector2.one,new Vector2(680,12),new Vector2(-15,-100));
            InteriorIndicator(button);
        }
        controller.status = Text("Status", panel.transform, "", 18);
        Rect(controller.status.gameObject,Vector2.zero,new Vector2(1,0),new Vector2(40,90),new Vector2(-40,140));
        controller.modeButton = Button("Mode",panel.transform,"Load Game");
        Rect(controller.modeButton.gameObject,Vector2.zero,Vector2.zero,new Vector2(40,25),new Vector2(300,80));
        controller.modeButtonLabel = controller.modeButton.GetComponentInChildren<TMP_Text>(); InteriorIndicator(controller.modeButton);
        controller.closeButton = Button("Exit",panel.transform,"Exit");
        Rect(controller.closeButton.gameObject,new Vector2(1,0),new Vector2(1,0),new Vector2(-230,25),new Vector2(-40,80)); OutsideIndicator(controller.closeButton,panel.transform);
        var modal = Obj("Confirmation",root.transform); Fill(modal); AddImage(modal,new Color(0,0,0,.7f)); controller.confirmationPanel = modal;
        var box = Obj("Dialog",modal.transform); var boxRect=(RectTransform)box.transform; boxRect.anchorMin=boxRect.anchorMax=new Vector2(.5f,.5f);boxRect.sizeDelta=new Vector2(700,300);AddImage(box,paper, panelSprite);
        controller.confirmationText=Text("Question",box.transform,"Overwrite this save?",25,TextAlignmentOptions.Center);
        Rect(controller.confirmationText.gameObject,new Vector2(0,.4f),Vector2.one,new Vector2(30,0),new Vector2(-30,-20));
        controller.confirmButton=Button("Confirm",box.transform,"Confirm");Rect(controller.confirmButton.gameObject,Vector2.zero,new Vector2(.5f,0),new Vector2(30,30),new Vector2(-15,90));InteriorIndicator(controller.confirmButton);
        controller.cancelButton=Button("Cancel",box.transform,"Cancel");Rect(controller.cancelButton.gameObject,new Vector2(.5f,0),new Vector2(1,0),new Vector2(15,30),new Vector2(-30,90));InteriorIndicator(controller.cancelButton);
        // Explicit graph: slots up/down, final row to footer; no accidental focus outside menu.
        for (int i=0;i<4;i++)
        {
            var n = new Navigation { mode=Navigation.Mode.Explicit,
                selectOnUp=i==0?controller.closeButton:controller.slots[i-1].button,
                selectOnDown=i==3?controller.closeButton:controller.slots[i+1].button };
            controller.slots[i].button.navigation=n;
        }
        controller.closeButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=controller.slots[3].button,selectOnDown=controller.slots[0].button,selectOnLeft=controller.modeButton};
        controller.modeButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=controller.slots[3].button,selectOnDown=controller.slots[0].button,selectOnRight=controller.closeButton};
        controller.confirmButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnRight=controller.cancelButton};
        controller.cancelButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnLeft=controller.confirmButton};
        modal.SetActive(false);root.SetActive(false);
        var eventSystem=FindAnyObjectByType<EventSystem>();
        if(eventSystem==null)
        {
            var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));Undo.RegisterCreatedObjectUndo(es,"Event system");
            var module=es.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();module.deselectOnBackgroundClick=false;
        }
        var normalMenu=FindAnyObjectByType<MenuController>();
        if(normalMenu!=null) {Undo.RecordObject(normalMenu,"Wire menu control gate");SetReference(normalMenu,"controls",gate);SetReference(normalMenu,"saveMenu",controller);}
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());Undo.CollapseUndoOperations(group);Selection.activeGameObject=manager;
        EditorUtility.DisplayDialog("Created", "Save UI created. Add SaveArea to each boundary and SavePoint to the freezer trigger. Verify the active confiner and EventSystem Submit bindings. Save the scene.", "OK");
    }
}
#endif
