using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

// Replacement preserving previous Inspector field names and behaviour.
public class MapTransition : MonoBehaviour
{
    private enum TransitionAxis { Vertical, Horizontal }
    [SerializeField] private TransitionAxis transitionAxis = TransitionAxis.Vertical;
    [SerializeField] private CinemachineConfiner2D confiner2D;
    [SerializeField] private PolygonCollider2D upperMapBounds, lowerMapBounds, rightMapBounds, leftMapBounds;
    [SerializeField] private Transform upperArrival, lowerArrival, rightArrival, leftArrival;
    [SerializeField, Min(0.1f)] private float moveDuration = 1f;
    [SerializeField, Min(0f)] private float arrivalPause = 0.5f;
    [SerializeField] private bool showAreaName = true;
    [SerializeField] private CanvasGroup areaOverlay;
    [SerializeField] private TMP_Text areaNameText;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;
    [SerializeField] private string upperAreaName = "Upper Area", lowerAreaName = "Lower Area";
    [SerializeField] private string rightAreaName = "Right Area", leftAreaName = "Left Area";
    private PlayerControlGate activeGate;
    private void OnTriggerEnter2D(Collider2D other) { TryTransition(other); }
    private void OnTriggerStay2D(Collider2D other) { TryTransition(other); }
    private void TryTransition(Collider2D other)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || activeGate != null) return;
        var body = other.attachedRigidbody;
        if (body == null || !body.CompareTag("Player")) return;
        var gate = body.GetComponent<PlayerControlGate>();
        if (gate == null || gate.IsLocked) return;
        bool vertical = transitionAxis == TransitionAxis.Vertical;
        float direction = vertical ? body.linearVelocity.y : body.linearVelocity.x;
        if (Mathf.Abs(direction) < 0.01f) return;
        bool positive = direction > 0;
        var bounds = vertical ? (positive ? upperMapBounds : lowerMapBounds) : (positive ? rightMapBounds : leftMapBounds);
        var arrival = vertical ? (positive ? upperArrival : lowerArrival) : (positive ? rightArrival : leftArrival);
        string title = vertical ? (positive ? upperAreaName : lowerAreaName) : (positive ? rightAreaName : leftAreaName);
        if (confiner2D == null || bounds == null || arrival == null)
        { Debug.LogError("Assign transition references.", this); return; }
        if (confiner2D.BoundingShape2D == bounds || !gate.TryLock(this)) return;
        activeGate = gate;
        StartCoroutine(Transition(body, bounds, arrival.position, title));
    }
    private IEnumerator Transition(Rigidbody2D body, PolygonCollider2D bounds, Vector2 destination, string title)
    {
        bool display = showAreaName && areaOverlay != null && areaNameText != null;
        float duration = Mathf.Max(0.1f, moveDuration), fade = Mathf.Max(0.01f, fadeDuration);
        try
        {
            if (display) { areaNameText.text = title; areaOverlay.alpha = 0; }
            confiner2D.BoundingShape2D = bounds; confiner2D.InvalidateBoundingShapeCache();
            Vector2 start = body.position; float elapsed = 0;
            var step = new WaitForFixedUpdate();
            while (elapsed < duration)
            {
                yield return step;
                elapsed += Time.fixedDeltaTime;
                body.MovePosition(Vector2.Lerp(start, destination, Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration))));
                if (display) areaOverlay.alpha = Mathf.Clamp01(elapsed / fade);
            }
            yield return step; body.linearVelocity = Vector2.zero;
            if (display)
                while (areaOverlay.alpha < 1) { areaOverlay.alpha = Mathf.MoveTowards(areaOverlay.alpha, 1, Time.deltaTime / fade); yield return null; }
            yield return new WaitForSeconds(Mathf.Max(0, arrivalPause));
            if (display)
                while (areaOverlay.alpha > 0) { areaOverlay.alpha = Mathf.MoveTowards(areaOverlay.alpha, 0, Time.deltaTime / fade); yield return null; }
        }
        finally { Release(); }
    }
    private void Release()
    {
        if (areaOverlay != null) areaOverlay.alpha = 0;
        if (activeGate != null) activeGate.Unlock(this);
        activeGate = null;
    }
    private void OnDisable() { StopAllCoroutines(); Release(); }
}
