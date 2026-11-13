using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Place on an always-active manager, never on root or confirmationPanel.
public class SaveMenuController : MonoBehaviour
{
    [Tooltip("Enable only on the load panel manager in the start-menu scene.")]
    public bool startMenuOnly;
    public GameObject startMenuPanel;
    public Button startLoadButton;
    public SaveController saves;
    public PlayerControlGate controls;
    public GameObject root;
    public TMP_Text header, status, confirmationText;
    public SaveSlotView[] slots = new SaveSlotView[4];
    public Button closeButton, modeButton, confirmButton, cancelButton;
    public TMP_Text modeButtonLabel;
    public GameObject confirmationPanel;
    public bool IsOpen { get; private set; }
    public bool CanOpen => !startMenuOnly && ready && !IsOpen && controls != null && !controls.IsLocked && Time.timeScale > 0f;
    private bool saveMode, capturing;
    private SavePoint point;
    private string screenshot;
    private int pendingSlot;
    private bool ready;
    private int page;
    private Button pageButton;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (root == null || root == gameObject || (startMenuOnly ? (!startMenuPanel || !startLoadButton) : (!saves || !controls)) || slots == null || slots.Length != 4 ||
            closeButton == null || confirmButton == null || cancelButton == null ||
            confirmationPanel == null || confirmationText == null || header == null || status == null)
        { Debug.LogError("Complete SaveMenuController references; manager must be outside menu root.", this); return; }
        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == null || slots[i].button == null) { Debug.LogError("Assign all four slot views/buttons.", this); return; }
            int row = i; slots[i].button.onClick.AddListener(() => Choose(page * 4 + row + 1));
        }
        closeButton.onClick.AddListener(Close);
        if (modeButton) modeButton.gameObject.SetActive(false);
        var navigation = closeButton.navigation;
        if (navigation.selectOnLeft == modeButton) navigation.selectOnLeft = null;
        closeButton.navigation = navigation;
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(CancelConfirmation);
        // Reuse the installed four-row layout, including its navigation.
        var pager = new GameObject("OtherSlots", typeof(RectTransform), typeof(Image), typeof(Button));
        pager.transform.SetParent(closeButton.transform.parent, false);
        pageButton = pager.GetComponent<Button>();
        var graphic = pager.GetComponent<Image>();
        if (closeButton.targetGraphic is Image sourceImage)
        {
            graphic.sprite = sourceImage.sprite;
            graphic.color = sourceImage.color;
            graphic.type = sourceImage.type;
        }
        pageButton.targetGraphic = graphic;
        pageButton.colors = closeButton.colors;
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(pager.transform, false);
        var sourceLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
        if(sourceLabel) { label.font = sourceLabel.font; label.color = sourceLabel.color; label.fontSize = sourceLabel.fontSize; }
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        pageButton.onClick.AddListener(() => { page = 1 - page; Refresh(); Select(slots[0].button); });
        var rect = (RectTransform)pageButton.transform;
        var original = (RectTransform)closeButton.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.offsetMin = new Vector2(32, original.offsetMin.y);
        rect.offsetMax = new Vector2(332, original.offsetMax.y);
        var nav = closeButton.navigation;
        nav.selectOnLeft = pageButton; closeButton.navigation = nav;
        nav.selectOnLeft = null; nav.selectOnRight = closeButton; pageButton.navigation = nav;
        ready = true;
        if (startMenuOnly)
        {
            startLoadButton.onClick.AddListener(OpenLoad);
            string failure = TitleSlotLoader.TakeFailure();
            if (!string.IsNullOrEmpty(failure)) { OpenLoad(); status.text = failure; }
        }
    }
    private void Update()
    {
        if (!IsOpen || TitleSlotLoader.Busy) return;
        bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                      (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (cancel) { if (confirmationPanel.activeSelf) CancelConfirmation(); else Close(); }
    }
    public void OpenSave(SavePoint source)
    {
        if (!ready || source == null || !CanOpen || !controls.TryLock(this, true)) return;
        IsOpen = true; point = source; saveMode = true; capturing = true;
        StartCoroutine(CaptureThenOpen());
    }
    // Called by the title screen button.
    public void OpenLoad()
    {
        if (!startMenuOnly || !ready || IsOpen || TitleSlotLoader.Busy) return;
        startMenuPanel.SetActive(false);
        IsOpen = true; point = null; screenshot = null; saveMode = false;
        ShowMenu();
    }
    private IEnumerator CaptureThenOpen()
    {
        // Menu and freezer prompt are hidden, world is paused at this moment.
        yield return new WaitForEndOfFrame();
        try { screenshot = CapturePreview(); }
        catch (Exception e)
        {
            Debug.LogError("Screenshot capture failed: " + e.Message, this);
            screenshot = null;
        }
        capturing = false;
        ShowMenu();
        if (string.IsNullOrEmpty(screenshot)) status.text = "Could not capture preview. Close and reopen the freezer menu.";
    }
    private string CapturePreview()
    {
        Texture2D full = null, small = null;
        RenderTexture target = null, previous = RenderTexture.active;
        try
        {
            full = ScreenCapture.CaptureScreenshotAsTexture();
            int width = 480, height = Mathf.Max(1, Mathf.RoundToInt(480f * full.height / full.width));
            target = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(full, target); RenderTexture.active = target;
            small = new Texture2D(width, height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, width, height), 0, 0); small.Apply();
            return Convert.ToBase64String(small.EncodeToJPG(75));
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (full != null) Destroy(full); if (small != null) Destroy(small);
        }
    }
    private void ShowMenu()
    {
        page = 0; root.SetActive(true); confirmationPanel.SetActive(false);
        status.text = ""; SetMainInteractable(true); Refresh();
        StartCoroutine(FocusNextFrame(slots[0].button));
    }
    private void Refresh()
    {
        var pageLabel = pageButton.GetComponentInChildren<TMP_Text>(true);
        if (pageLabel) pageLabel.text = page == 0 ? "Slots 5–8" : "Slots 1–4";
        header.text = saveMode ? "SAVE GAME" : "LOAD GAME";
        for (int i = 0; i < 4; i++)
        {
            SaveData data = null; string error = null;
            try { data = SaveRepository.Read(page * 4 + i + 1); }
            catch (Exception e) { error = e.Message; }
            slots[i].Show(page * 4 + i + 1, data, error);
            // Empty/unreadable load slots remain focusable to show a useful message.
        }
    }
    private void Choose(int slot)
    {
        if (!IsOpen || TitleSlotLoader.Busy || capturing || confirmationPanel.activeSelf) return;
        bool exists;
        try { exists = SaveRepository.Exists(slot); }
        catch (Exception e) { status.text = e.Message; return; }
        if (!saveMode && !exists) { status.text = "This slot is empty."; return; }
        if (!saveMode)
        {
            try { SaveRepository.Read(slot); }
            catch (Exception e) { status.text = "Cannot load: " + e.Message; return; }
        }
        pendingSlot = slot;
        if (saveMode && !exists) { Execute(); return; }
        confirmationText.text = saveMode
            ? "Overwrite slot " + slot + "? Your previous save will be replaced."
            : "Load slot " + slot + "?";
        SetMainInteractable(false); confirmationPanel.SetActive(true); Select(cancelButton);
    }
    private void Confirm()
    {
        if (!IsOpen || !confirmationPanel.activeSelf) return;
        confirmationPanel.SetActive(false); SetMainInteractable(true); Execute();
    }
    private void Execute()
    {
        string message;
        bool success = saveMode
            ? saves.SaveSlot(pendingSlot, point, screenshot, out message)
            : TitleSlotLoader.Begin(pendingSlot, out message);
        if (success && !saveMode) { status.text = "Loading…"; SetMainInteractable(false); return; }
        Refresh(); status.text = message; Select(slots[(pendingSlot - 1) % 4].button);
    }
    private void CancelConfirmation()
    {
        confirmationPanel.SetActive(false); SetMainInteractable(true);
        Select(slots[(pendingSlot - 1) % 4].button);
    }
    private void SetMainInteractable(bool value)
    {
        foreach (var slot in slots) slot.button.interactable = value;
        closeButton.interactable = value; if (pageButton) pageButton.interactable = value;
    }
    private IEnumerator FocusNextFrame(Button button) { yield return null; if (IsOpen) Select(button); }
    private void Select(Button button)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    public void Close()
    {
        if (!IsOpen) return;
        StopAllCoroutines(); capturing = false; IsOpen = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        confirmationPanel.SetActive(false); root.SetActive(false);
        screenshot = null; point = null;
        if (startMenuOnly)
        {
            if (startMenuPanel) startMenuPanel.SetActive(true);
            if (startLoadButton && isActiveAndEnabled) Select(startLoadButton);
        }
        else controls.Unlock(this);
    }
    private void OnDisable() { if (IsOpen && !startMenuOnly) Close(); }
}
