using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
namespace FreezerGameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class NearbyInteraction : MonoBehaviour
    {
        public PlayerControlGate player; public GameObject prompt; public UnityEvent onInteract;
        static int usedFrame = -1;
        readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }
        void OnTriggerEnter2D(Collider2D other) { if (other.GetComponentInParent<PlayerControlGate>() == player) contacts.Add(other); }
        void OnTriggerExit2D(Collider2D other) { contacts.Remove(other); }
        void Update()
        {
            contacts.RemoveWhere(x => x == null || !x.enabled || !x.gameObject.activeInHierarchy);
            bool ready = player != null && contacts.Count > 0 && !player.IsLocked && Time.timeScale > 0;
            if (prompt != null) prompt.SetActive(ready);
            if (ready && !PickupHands.UsedThisFrame && usedFrame != Time.frameCount && ((Keyboard.current?.eKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonWest.wasPressedThisFrame ?? false))) { usedFrame = Time.frameCount; onInteract.Invoke(); }
        }
        void OnDisable() { contacts.Clear(); if (prompt != null) prompt.SetActive(false); }
    }
}
