using UnityEngine;
using UnityEngine.InputSystem;

// One gate shared by the normal menu, save menu and map transitions.
[RequireComponent(typeof(PlayerMovement), typeof(PlayerInput), typeof(Rigidbody2D))]
public class PlayerControlGate : MonoBehaviour
{
    private PlayerMovement movement;
    private PlayerInput input;
    private Rigidbody2D body;
    private Object owner;
    private bool held, movementWasEnabled, inputWasActive, paused;
    private float previousTimeScale;
    public bool IsLocked => held;
    private void Awake()
    {
        movement = GetComponent<PlayerMovement>(); input = GetComponent<PlayerInput>(); body = GetComponent<Rigidbody2D>();
    }
    public bool TryLock(Object requester, bool pauseWorld = false)
    {
        if (requester == null || held || !movement.isActiveAndEnabled || !input.inputIsActive) return false;
        owner = requester; held = true;
        movementWasEnabled = movement.enabled; inputWasActive = input.inputIsActive;
        input.DeactivateInput(); movement.enabled = false; body.linearVelocity = Vector2.zero;
        var animator = GetComponent<Animator>();
        if (animator != null) animator.SetBool("isWalking", false);
        paused = pauseWorld; previousTimeScale = Time.timeScale;
        if (paused) Time.timeScale = 0f;
        return true;
    }
    public void Unlock(Object requester)
    {
        if (!held || owner != requester) return;
        Release();
    }
    private void Update() { if (held && owner == null) Release(); }
    private void Release()
    {
        if (paused) Time.timeScale = previousTimeScale;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (movement != null) movement.enabled = movementWasEnabled;
        if (input != null && inputWasActive) input.ActivateInput();
        owner = null; held = false; paused = false;
    }
    private void OnDisable() { if (held) Release(); }
}
