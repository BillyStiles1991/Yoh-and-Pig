using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SavePoint : MonoBehaviour
{
    public string savePointId = "kitchen_freezer_01";
    public string locationName = "Kitchen Freezer";
    public SaveMenuController saveMenu;
    public GameObject interactionPrompt;
    private readonly HashSet<Collider2D> players = new HashSet<Collider2D>();
    private void Awake()
    {
        if (!GetComponent<Collider2D>().isTrigger) Debug.LogError("SavePoint needs a trigger collider.", this);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) players.Add(other);
    }
    private void OnTriggerExit2D(Collider2D other) { players.Remove(other); }
    private void Update()
    {
        players.RemoveWhere(x => x == null || !x.enabled || !x.gameObject.activeInHierarchy);
        bool available = players.Count > 0 && saveMenu != null && saveMenu.CanOpen;
        if (interactionPrompt != null) interactionPrompt.SetActive(available);
        if (!available) return;
        bool pressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                       (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        if (pressed && !FreezerGameplay.PickupHands.UsedThisFrame)
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            saveMenu.OpenSave(this);
        }
    }
    private void OnDisable()
    {
        players.Clear(); if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }
}
