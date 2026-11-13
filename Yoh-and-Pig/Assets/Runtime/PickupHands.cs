using UnityEngine;
using UnityEngine.InputSystem;

namespace FreezerGameplay
{
    // Claim a pickup press before a nearby chest, NPC or freezer processes the same button.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerControlGate), typeof(Rigidbody2D))]
    public class PickupHands : MonoBehaviour
    {
        [Min(0)] public float reach = 0.35f;
        public Collider2D playerCollider;
        public PickupFeedback feedback;
        public Animator animator;
        [Tooltip("Optional. Animator Bool used while the bag is full.")] public string frustrationBool;
        [Tooltip("Optional. Animator Bool for the first discovery pose.")] public string holdingBool;
        [Min(0.1f)] public float discoverySeconds = 2.3f;
        [Min(0.1f)] public float frustrationSeconds = 1f;
        PlayerControlGate gate;
        Rigidbody2D body;
        PlayerInput input;
        float angryUntil;
        Vector2 angryAt;
        bool angry, holding;
        float holdTime;
        static int usedFrame = -1;
        public static bool UsedThisFrame => usedFrame == Time.frameCount;

        void Awake()
        {
            gate = GetComponent<PlayerControlGate>();
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInput>();
            if (!playerCollider) playerCollider = GetComponent<Collider2D>();
            if (!animator) animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (holding)
            {
                holdTime += Time.deltaTime;
                if (holdTime >= discoverySeconds) EndHold();
                return;
            }
            if (angry)
            {
                var move = input && input.currentActionMap != null ? input.currentActionMap.FindAction("Move") : null;
                if (gate.IsLocked || Time.time >= angryUntil || body.position != angryAt ||
                    body.linearVelocity.sqrMagnitude > 0.0001f || (move != null && move.ReadValue<Vector2>().sqrMagnitude > 0.001f))
                    EndFrustration();
            }
            if (gate.IsLocked || Time.timeScale <= 0 || !playerCollider || UsedThisFrame) return;
            var pad = Gamepad.current;
            if (!(Keyboard.current?.eKey.wasPressedThisFrame ?? false)
                && !(pad?.buttonWest.wasPressedThisFrame ?? false)
                && !(pad?.buttonSouth.wasPressedThisFrame ?? false)) return;

            ProgressPickup nearest = null;
            float best = reach;
            foreach (var pickup in ProgressPickup.Active)
            {
                if (!pickup.Solid.enabled) continue;
                var separation = playerCollider.Distance(pickup.Solid);
                if (!separation.isValid || separation.distance > best) continue;
                nearest = pickup;
                best = separation.distance;
            }
            if (!nearest) return;
            usedFrame = Time.frameCount;
            nearest.Collect(this);
        }

        internal void Full()
        {
            angry = true;
            angryUntil = Time.time + frustrationSeconds;
            angryAt = body.position;
            SetPose(frustrationBool, true);
            if (feedback) feedback.ShowFull();
        }

        internal void Received(ItemDefinition item, int count, bool first)
        {
            EndFrustration();
            if (feedback) feedback.ShowItem(item, count);
            if (!first || !gate.TryLock(this)) return;
            holding = true;
            holdTime = 0;
            SetPose(holdingBool, true);
            if (feedback) feedback.Discover(item.icon, discoverySeconds);
        }

        void SetPose(string parameter, bool value)
        {
            if (animator && !string.IsNullOrEmpty(parameter)) animator.SetBool(parameter, value);
        }

        void EndHold()
        {
            if (!holding) return;
            holding = false;
            SetPose(holdingBool, false);
            if (feedback) feedback.EndDiscovery();
            gate.Unlock(this);
        }

        void EndFrustration()
        {
            if (angry) SetPose(frustrationBool, false);
            angry = false;
        }

        void OnDisable() { EndHold(); EndFrustration(); }
    }
}
