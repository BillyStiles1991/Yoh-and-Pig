using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FreezerGameplay
{
    [DefaultExecutionOrder(-60)]
    public class WorldDropManager : MonoBehaviour
    {
        public InventoryController inventory; public Transform player; public PlayerControlGate gate; public PickupFeedback feedback;
        public float reach = 1; public string sortingLayer = "Default"; public int sortingOrder = 10;
        readonly Dictionary<string, GameObject> visuals = new Dictionary<string, GameObject>();
        void Update()
        {
            if (!inventory || !player) return;
            var data = inventory.store.Data;
            var drops = data.drops.Where(d => d.scenePath == player.gameObject.scene.path && d.expiresAt > data.secondsPlayed).ToList();
            foreach (string id in visuals.Keys.ToArray()) if (!drops.Any(d => d.id == id)) { Destroy(visuals[id]); visuals.Remove(id); }
            foreach (var d in drops)
                if (!visuals.ContainsKey(d.id))
                {
                    var go = new GameObject("Dropped " + d.itemId); SceneManager.MoveGameObjectToScene(go, player.gameObject.scene);
                    go.transform.position = d.position;
                    var art = go.AddComponent<SpriteRenderer>(); art.sprite = inventory.Icon(d.itemId); art.sortingLayerName = sortingLayer; art.sortingOrder = sortingOrder;
                    if (art.sprite) { float n = .6f / Mathf.Max(.01f, art.sprite.bounds.size.x, art.sprite.bounds.size.y); go.transform.localScale = Vector3.one * n; }
                    visuals.Add(d.id, go); // Intentionally no collider: dropped items never block movement.
                }
            if (!gate || gate.IsLocked || Time.timeScale <= 0 || PickupHands.UsedThisFrame || !GameInput.InteractPressed(player.GetComponent<UnityEngine.InputSystem.PlayerInput>())) return;
            var nearest = drops.Where(d => Vector2.Distance(player.position, d.position) <= reach).OrderBy(d => Vector2.Distance(player.position, d.position)).FirstOrDefault();
            if (nearest == null) return;
            var collider = player.GetComponent<Collider2D>();
            var hands = player.GetComponent<PickupHands>();
            if (collider && hands && ProgressPickup.Active.Any(p => p && p.Solid.enabled && collider.Distance(p.Solid).distance <= hands.reach && collider.Distance(p.Solid).distance < Vector2.Distance(player.position, nearest.position))) return;
            PickupHands.ClaimPress();
            if (!inventory.ExchangeQuiet(null, new[] { new ItemAmount { itemId = nearest.itemId, quantity = nearest.quantity, unitWeight = nearest.unitWeight } })) { if (feedback) feedback.ShowFull(); return; }
            data.drops.Remove(nearest); inventory.store.NotifyChanged();
            if (feedback) feedback.ShowItem(inventory.database.Item(nearest.itemId), nearest.quantity);
        }
        void OnDisable() { foreach (var go in visuals.Values) if (go) Destroy(go); visuals.Clear(); }
    }
}
