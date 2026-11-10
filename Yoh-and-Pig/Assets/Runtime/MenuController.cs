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
    [SerializeField] private SaveMenuController saveMenu;
    private bool menuOpen;
    private void Start()
    {
        if (menuCanvas == null || tabController == null || controls == null || menuCanvas == gameObject)
        { Debug.LogError("Assign MenuController references on an always-active manager.", this); enabled = false; return; }
        menuCanvas.SetActive(false);
    }
    private void Update()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        if ((k != null && k.tabKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame))
        { ToggleMenu(); return; }
        if (menuOpen && ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))) CloseMenu();
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
    public void OpenLoadMenu()
    {
        if (saveMenu == null) return;
        CloseMenu(); saveMenu.OpenLoad();
    }
    private void OnDisable() { if (menuOpen) CloseMenu(); }
}
