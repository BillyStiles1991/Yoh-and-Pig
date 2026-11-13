using System;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Always active. Loading is intentionally restricted to the currently loaded scene.
public class SaveController : MonoBehaviour
{
    public GameProgressStore progress;
    public Rigidbody2D playerBody;
    public CinemachineConfiner2D confiner2D;

    private SaveArea[] Areas() => FindObjectsByType<SaveArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    private SaveArea ResolveArea(string id)
    {
        var matches = Areas().Where(x => x.areaId == id).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Area ID must resolve exactly once: " + id);
        return matches[0];
    }
    private void CheckReferences()
    {
        if (progress == null || playerBody == null || confiner2D == null)
            throw new InvalidOperationException("Assign SaveController's progress, player body and confiner.");
    }
    public bool SaveSlot(int slot, SavePoint point, string jpegBase64, out string message)
    {
        try
        {
            CheckReferences();
            if (point == null || string.IsNullOrWhiteSpace(point.savePointId))
                throw new InvalidOperationException("Assign a stable ID to the save point.");
            if (string.IsNullOrEmpty(jpegBase64)) throw new InvalidOperationException("No screenshot captured. Reopen the save menu.");
            var candidates = Areas().Where(x => x.Boundary == confiner2D.BoundingShape2D).ToArray();
            if (candidates.Length != 1) throw new InvalidOperationException("The current camera boundary needs exactly one SaveArea component.");
            SaveArea area = ResolveArea(candidates[0].areaId);
            var snapshot = progress.Snapshot();
            var data = new SaveData {
                slot = slot,
                displayName = string.IsNullOrWhiteSpace(snapshot.chapterTitle) ? "Save " + slot : snapshot.chapterTitle,
                savedUtc = DateTime.UtcNow.ToString("O"),
                sceneName = SceneManager.GetActiveScene().name,
                areaId = area.areaId,
                locationName = string.IsNullOrWhiteSpace(point.locationName) ? area.displayName : point.locationName,
                savePointId = point.savePointId,
                playerPosition = playerBody.transform.position,
                screenshotJpegBase64 = jpegBase64,
                progress = snapshot
            };
            SaveRepository.Write(data);
            message = "Saved to slot " + slot + "."; return true;
        }
        catch (Exception e) { message = "Save failed: " + e.Message; Debug.LogError(message, this); return false; }
    }
    public bool LoadSlot(int slot, out string message)
    {
        try
        {
            SaveData data = SaveRepository.Read(slot);
            return RestoreSnapshot(data, out message);
        }
        catch (Exception e)
        {
            message = "Load failed: " + e.Message;
            Debug.LogError(message, this);
            return false;
        }
    }

    internal bool RestoreSnapshot(SaveData data, out string message)
    {
        try
        {
            CheckReferences();

            if (data == null)
                throw new InvalidOperationException("That slot is empty.");

            if (data.sceneName != SceneManager.GetActiveScene().name)
                throw new InvalidOperationException(
                    "This save belongs to scene '" + data.sceneName +
                    "'. Open that scene before loading.");

            SaveArea area = ResolveArea(data.areaId);
            var boundary = area.Boundary;

            if (!area.isActiveAndEnabled || boundary == null || !boundary.isActiveAndEnabled)
                throw new InvalidOperationException(
                    "The saved area boundary is missing or inactive.");

            playerBody.linearVelocity = Vector2.zero;
            playerBody.transform.position = data.playerPosition;
            playerBody.position = new Vector2(data.playerPosition.x, data.playerPosition.y);
            Physics2D.SyncTransforms();

            confiner2D.BoundingShape2D = boundary;
            confiner2D.InvalidateBoundingShapeCache();

            var camera = confiner2D.GetComponent<CinemachineCamera>();
            if (camera != null)
                camera.PreviousStateIsValid = false;

            progress.Restore(data.progress);

            message = "Loaded slot " + data.slot + ".";
            return true;
        }
        catch (Exception e)
        {
            message = "Load failed: " + e.Message;
            Debug.LogError(message, this);
            return false;
        }
    }
}
