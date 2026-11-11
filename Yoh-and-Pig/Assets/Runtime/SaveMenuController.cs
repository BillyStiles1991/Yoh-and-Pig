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
    public SaveController saves;
    public PlayerControlGate controls;
    public GameObject root;
    public TMP_Text header, status, confirmationText;
    public SaveSlotView[] slots = new SaveSlotView[4];
    public Button closeButton, modeButton, confirmButton, cancelButton;
    public TMP_Text modeButtonLabel;
    public GameObject confirmationPanel;
    public bool IsOpen { get; private set; }
    public bool CanOpen => !IsOpen && controls != null && !controls.IsLocked && Time.timeScale > 0f;
    private bool saveMode, capturing;
    private SavePoint point;
    private string screenshot;
    private int pendingSlot;
    private bool ready;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (root == null || root == gameObject || saves == null || controls == null || slots == null || slots.Length != 4 ||
            closeButton == null || modeButton == null || confirmButton == null || cancelButton == null ||
            confirmationPanel == null || confirmationText == null || header == null || status == null || modeButtonLabel == null)
        { Debug.LogError("Complete SaveMenuController references; manager must be outside menu root.", this); return; }
        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == null || slots[i].button == null) { Debug.LogError("Assign all four slot views/buttons.", this); return; }
            int slot = i + 1; slots[i].button.onClick.AddListener(() => Choose(slot));
        }
        closeButton.onClick.AddListener(Close);
        modeButton.onClick.AddListener(SwitchMode);
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(CancelConfirmation);
        ready = true;
    }
    private void Update()
    {
        if (!IsOpen) return;
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
    // Wire a Load button to this, or call MenuController.OpenLoadMenu().
    public void OpenLoad()
    {
        if (!ready || !CanOpen || !controls.TryLock(this, true)) return;
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
        root.SetActive(true); confirmationPanel.SetActive(false);
        status.text = ""; SetMainInteractable(true); Refresh();
        StartCoroutine(FocusNextFrame(slots[0].button));
    }
    private void Refresh()
    {
        header.text = saveMode ? "SAVE GAME" : "LOAD GAME";
        modeButtonLabel.text = saveMode ? "Load Game" : "Save Game";
        modeButton.gameObject.SetActive(point != null);
        for (int i = 0; i < 4; i++)
        {
            SaveData data = null; string error = null;
            try { data = SaveRepository.Read(i + 1); }
            catch (Exception e) { error = e.Message; }
            slots[i].Show(i + 1, data, error);
            // Empty/unreadable load slots remain focusable to show a useful message.
        }
    }
    private void SwitchMode()
    {
        if (!IsOpen || point == null || capturing || confirmationPanel.activeSelf) return;
        saveMode = !saveMode; status.text = ""; Refresh();
        Select(slots[0].button);
    }
    private void Choose(int slot)
    {
        if (!IsOpen || capturing || confirmationPanel.activeSelf) return;
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
            : "Load slot " + slot + "? Unsaved progress will be lost.";
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
            : saves.LoadSlot(pendingSlot, out message);
        if (success && !saveMode) { Close(); return; }
        Refresh(); status.text = message; Select(slots[pendingSlot - 1].button);
    }
    private void CancelConfirmation()
    {
        confirmationPanel.SetActive(false); SetMainInteractable(true);
        Select(slots[Mathf.Clamp(pendingSlot - 1, 0, 3)].button);
    }
    private void SetMainInteractable(bool value)
    {
        foreach (var slot in slots) slot.button.interactable = value;
        modeButton.interactable = value; closeButton.interactable = value;
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
        screenshot = null; point = null; controls.Unlock(this);
    }
    private void OnDisable() { if (IsOpen) Close(); }
}
