using System.Collections.Generic;
using UnityEngine;
namespace FreezerGameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PigCompanion : MonoBehaviour
    {
        public Transform player; public GameProgressStore store; public SpriteRenderer cargo; public Animator animator;
        public float speed = 5.5f, followDistance = 1.1f; public LayerMask obstacles;
        Rigidbody2D body; readonly Queue<Vector2> trail = new Queue<Vector2>(); Vector2 lastPlayer;
        public bool Attending { get; set; }
        void Awake() { body = GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true; }
        public void SetSeparated(bool value) { store.Data.pigSeparated = value; if (value && ItemSizing.Size(store.Data.heldWeight) == ItemSize.Big) { store.Data.heldItem = ""; store.Data.heldWeight = 0; } trail.Clear(); store.NotifyChanged(); }
        public void SetCargo(Sprite sprite) { ItemActions.SetBool(animator, "Carrying", sprite && !store.Data.pigSeparated); if (cargo) { cargo.sprite = sprite; cargo.enabled = sprite && !store.Data.pigSeparated; } }
        public bool Near(Vector3 target, float range = 2.5f) => !store.Data.pigSeparated && Vector2.Distance(transform.position, target) <= range;
        void FixedUpdate()
        {
            if (!player || !store || store.Data.pigSeparated) { ItemActions.SetBool(animator, "Walking", false); return; }
            Vector2 now = player.position;
            if (Vector2.Distance(lastPlayer, now) > .25f) { trail.Enqueue(now); lastPlayer = now; }
            while (trail.Count > 150) trail.Dequeue();
            while (trail.Count > 1 && Vector2.Distance(body.position, trail.Peek()) < .2f) trail.Dequeue();
            float gap = Attending ? .65f : followDistance;
            bool moving = Vector2.Distance(body.position, now) > gap;
            ItemActions.SetBool(animator, "Walking", moving);
            if (!moving) { body.linearVelocity = Vector2.zero; return; }
            Vector2 goal = trail.Count > 0 ? trail.Peek() : now;
            Vector2 step = Vector2.MoveTowards(body.position, goal, speed * Time.fixedDeltaTime) - body.position;
            if (!Physics2D.CircleCast(body.position, .15f, step.normalized, step.magnitude, obstacles)) body.MovePosition(body.position + step);
            var camera = Camera.main;
            if (camera)
            {
                Vector3 view = camera.WorldToViewportPoint(transform.position);
                if ((view.x < -.1f || view.x > 1.1f || view.y < -.1f || view.y > 1.1f) && !Physics2D.OverlapCircle(now, .2f, obstacles))
                { body.position = now; trail.Clear(); }
            }
        }
    }
}
