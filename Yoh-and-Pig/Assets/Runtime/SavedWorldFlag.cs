using UnityEngine;
using UnityEngine.Events;

// Visual/state adapter: use for an opened chest, unlocked door, completed event.
// Keep this component on an active parent; its events should change children only.
public class SavedWorldFlag : MonoBehaviour
{
    public enum Kind { Chest, Event }
    public GameProgressStore progress;
    public Kind kind;
    public string persistentId;
    public UnityEvent onReached, onNotReached;
    private bool? previous;
    private void OnEnable() { if (progress != null) { progress.Changed += Refresh; Refresh(); } }
    private void OnDisable() { if (progress != null) progress.Changed -= Refresh; previous = null; }
    public bool IsReached => progress != null && (kind == Kind.Chest ? progress.IsChestOpen(persistentId) : progress.HasEvent(persistentId));
    public void MarkReached()
    {
        if (progress == null || IsReached) return;
        if (kind == Kind.Chest) progress.MarkChestOpened(persistentId); else progress.MarkEvent(persistentId);
    }
    private void Refresh()
    {
        bool reached = IsReached;
        if (previous.HasValue && previous.Value == reached) return;
        previous = reached;
        if (reached) onReached.Invoke(); else onNotReached.Invoke();
    }
}
