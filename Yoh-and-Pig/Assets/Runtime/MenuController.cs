using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Replacement for the earlier MenuController. Reassign the new Controls field.
public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    [SerializeField] private TabController tabController;
    [SerializeField] private PlayerControlGate controls;
    private bool menuOpen;
    private void Start()
    {
        if (menuCanvas == null || tabController == null || controls == null || menuCanvas == gameObject)
        { Debug.LogError("Assign MenuController references on an always-active manager.", this); enabled = false; return; }
        menuCanvas.SetActive(false);
    }
    private void Update()
    {
        var controlsMap=FreezerGameplay.RuntimeControls.Current;
        if(controlsMap&&controlsMap.SuppressInput)return;
        bool pause=controlsMap?controlsMap.Pressed("System/Pause"):(Keyboard.current?.mKey.wasPressedThisFrame??false)||(Gamepad.current?.startButton.wasPressedThisFrame??false);
        bool cancel=controlsMap?controlsMap.Pressed("System/Cancel"):(Keyboard.current?.escapeKey.wasPressedThisFrame??false)||(Gamepad.current?.buttonEast.wasPressedThisFrame??false);
        if(pause){ToggleMenu();return;}
        if(menuOpen&&cancel){var page=FreezerGameplay.InventoryPanel.Active;if(page&&page.HasSubpage)page.Back();else CloseMenu();}
    }

    public void ToggleMenu() { if (menuOpen) CloseMenu(); else OpenMenu(); }
    public void OpenMenu()
    {
        if (menuOpen || controls == null || !controls.TryLock(this, true)) return;
        menuOpen = true; menuCanvas.SetActive(true); StartCoroutine(Focus());
    }
    private IEnumerator Focus() { yield return null; if (menuOpen) tabController.FocusActiveTab(); }
    public void CloseMenu()
    {
        if (!menuOpen) return;
        StopAllCoroutines(); menuOpen = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        menuCanvas.SetActive(false); controls.Unlock(this);
    }
    private void OnDisable() { if (menuOpen) CloseMenu(); }
}
