using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapTransition : MonoBehaviour
{
    private enum TransitionAxis
    {
        Vertical,
        Horizontal
    }

    [Header("Passage Orientation")]
    [SerializeField]
    private TransitionAxis transitionAxis = TransitionAxis.Vertical;

    [Header("Camera")]
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Vertical Map Boundaries")]
    [SerializeField] private PolygonCollider2D upperMapBounds;
    [SerializeField] private PolygonCollider2D lowerMapBounds;

    [Header("Vertical Arrival Positions")]
    [SerializeField] private Transform upperArrival;
    [SerializeField] private Transform lowerArrival;

    [Header("Horizontal Map Boundaries")]
    [SerializeField] private PolygonCollider2D rightMapBounds;
    [SerializeField] private PolygonCollider2D leftMapBounds;

    [Header("Horizontal Arrival Positions")]
    [SerializeField] private Transform rightArrival;
    [SerializeField] private Transform leftArrival;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float moveDuration = 1f;
    [SerializeField, Min(0f)] private float arrivalPause = 0.5f;

    [Header("Optional Area Name Overlay")]
    [SerializeField] private bool showAreaName = true;
    [SerializeField] private CanvasGroup areaOverlay;
    [SerializeField] private TMP_Text areaNameText;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;

    [Header("Vertical Area Names")]
    [SerializeField] private string upperAreaName = "Upper Area";
    [SerializeField] private string lowerAreaName = "Lower Area";

    [Header("Horizontal Area Names")]
    [SerializeField] private string rightAreaName = "Right Area";
    [SerializeField] private string leftAreaName = "Left Area";

    // Shared across all passages to prevent simultaneous transitions.
    private static bool transitionInProgress;

    // Reset static state when starting Play mode.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTransitionState()
    {
        transitionInProgress = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryTransition(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Allows a return transition without leaving and re-entering
        // the trigger after the previous transition finishes.
        TryTransition(collision);
    }

    private void TryTransition(Collider2D collision)
    {
        if (!isActiveAndEnabled || transitionInProgress)
        {
            return;
        }

        Rigidbody2D body = collision.attachedRigidbody;

        if (body == null || !body.CompareTag("Player"))
        {
            return;
        }

        bool vertical = transitionAxis == TransitionAxis.Vertical;

        float movementAlongPassage = vertical
            ? body.linearVelocity.y
            : body.linearVelocity.x;

        if (Mathf.Abs(movementAlongPassage) < 0.01f)
        {
            return;
        }

        bool positiveDirection = movementAlongPassage > 0f;

        PolygonCollider2D destinationBounds;
        Transform arrival;
        string areaName;

        if (vertical)
        {
            destinationBounds = positiveDirection
                ? upperMapBounds
                : lowerMapBounds;

            arrival = positiveDirection
                ? upperArrival
                : lowerArrival;

            areaName = positiveDirection
                ? upperAreaName
                : lowerAreaName;
        }
        else
        {
            destinationBounds = positiveDirection
                ? rightMapBounds
                : leftMapBounds;

            arrival = positiveDirection
                ? rightArrival
                : leftArrival;

            areaName = positiveDirection
                ? rightAreaName
                : leftAreaName;
        }

        if (confiner2D == null ||
            destinationBounds == null ||
            arrival == null)
        {
            Debug.LogError(
                "Assign the confiner, map boundaries, and arrival " +
                "points for this passage's selected orientation.",
                this
            );
            return;
        }

        // Do not repeatedly transition toward the current area.
        if (confiner2D.BoundingShape2D == destinationBounds)
        {
            return;
        }

        PlayerMovement movement = body.GetComponent<PlayerMovement>();
        PlayerInput input = body.GetComponent<PlayerInput>();

        if (movement == null || input == null)
        {
            Debug.LogError(
                "The player's Rigidbody2D object needs both " +
                "PlayerMovement and PlayerInput.",
                body
            );
            return;
        }

        StartCoroutine(Transition(
            body,
            movement,
            input,
            destinationBounds,
            arrival.position,
            areaName
        ));
    }

    private IEnumerator Transition(
        Rigidbody2D body,
        PlayerMovement movement,
        PlayerInput input,
        PolygonCollider2D destinationBounds,
        Vector2 destination,
        string areaName)
    {
        transitionInProgress = true;

        bool movementWasEnabled = movement.enabled;
        bool inputWasActive = input.inputIsActive;

        bool displayTitle = showAreaName &&
                            areaOverlay != null &&
                            areaNameText != null;

        float actualMoveDuration = Mathf.Max(0.1f, moveDuration);
        float actualFadeDuration = Mathf.Max(0.01f, fadeDuration);

        try
        {
            // Cancel input before disabling the movement script.
            input.DeactivateInput();
            movement.enabled = false;
            body.linearVelocity = Vector2.zero;

            Animator animator = body.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            if (displayTitle)
            {
                areaNameText.text = areaName;
                areaOverlay.alpha = 0f;
            }

            // Switch the camera to the destination map.
            confiner2D.BoundingShape2D = destinationBounds;
            confiner2D.InvalidateBoundingShapeCache();

            Vector2 startPosition = body.position;
            float elapsed = 0f;

            WaitForFixedUpdate physicsStep = new WaitForFixedUpdate();

            // Automatically move the player toward the arrival point.
            while (elapsed < actualMoveDuration)
            {
                yield return physicsStep;

                elapsed += Time.fixedDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / actualMoveDuration
                );

                float smoothProgress = Mathf.SmoothStep(
                    0f, 1f, progress
                );

                body.MovePosition(Vector2.Lerp(
                    startPosition,
                    destination,
                    smoothProgress
                ));

                if (displayTitle)
                {
                    areaOverlay.alpha = Mathf.Clamp01(
                        elapsed / actualFadeDuration
                    );
                }
            }

            // Allow the final movement to be processed by physics.
            yield return physicsStep;
            body.linearVelocity = Vector2.zero;

            // Complete the title fade if it takes longer than movement.
            if (displayTitle)
            {
                while (areaOverlay.alpha < 1f)
                {
                    areaOverlay.alpha = Mathf.MoveTowards(
                        areaOverlay.alpha,
                        1f,
                        Time.deltaTime / actualFadeDuration
                    );

                    yield return null;
                }
            }

            yield return new WaitForSeconds(
                Mathf.Max(0f, arrivalPause)
            );

            // Fade away the area name before restoring controls.
            if (displayTitle)
            {
                while (areaOverlay.alpha > 0f)
                {
                    areaOverlay.alpha = Mathf.MoveTowards(
                        areaOverlay.alpha,
                        0f,
                        Time.deltaTime / actualFadeDuration
                    );

                    yield return null;
                }
            }
        }
        finally
        {
            if (areaOverlay != null)
            {
                areaOverlay.alpha = 0f;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            if (movement != null)
            {
                movement.enabled = movementWasEnabled;
            }

            if (input != null && inputWasActive)
            {
                input.ActivateInput();
            }

            // No cooldown: returning is allowed once controls resume.
            transitionInProgress = false;
        }
    }
}