using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    Rigidbody2D rb;
    Vector2 moveInput;
    Animator animator;
    FreezerGameplay.ItemActions itemActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        itemActions = GetComponent<FreezerGameplay.ItemActions>();
    }
    void FixedUpdate()
    {
        float factor = itemActions && itemActions.inventory && (itemActions.inventory.store.Data.sicknessRemaining > 0 || itemActions.inventory.store.Data.sicknessLevel > 0)
            ? itemActions.sickSpeedMultiplier : 1;
        rb.linearVelocity = moveInput * moveSpeed * factor;
    }
    void OnDisable()
    {
        moveInput = Vector2.zero;
        if (rb) rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool("isWalking", false);
    }
    public void Move(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled) return;
        Vector2 previous = moveInput;
        moveInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
        if (!animator) return;
        animator.SetBool("isWalking", moveInput.sqrMagnitude > 0);
        if (context.canceled && previous.sqrMagnitude > 0)
        {
            animator.SetFloat("LastInputX", previous.x);
            animator.SetFloat("LastInputY", previous.y);
        }
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }
}
