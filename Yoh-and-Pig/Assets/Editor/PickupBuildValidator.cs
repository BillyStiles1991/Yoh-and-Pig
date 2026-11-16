using System;
using System.Collections.Generic;
using System.Linq;
using FreezerGameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor only. Place under Assets/YohPickups/Editor; do not attach to a GameObject.
public sealed class PickupBuildValidator : BuildPlayerProcessor
{
    public override int callbackOrder => 10000;

    public override void PrepareForBuild(BuildPlayerContext context)
    {
        // Use the actual requested scene list, including scripted/CI builds.
        ValidateOrThrow(context.BuildPlayerOptions.scenes);
    }

    [MenuItem("Tools/Freezer Game/Validate Pickup IDs In Build Scenes")]
    static void ValidateFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Exit Play Mode before validating pickup IDs.");
            return;
        }
        try
        {
            ValidateOrThrow(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
        }
        catch (BuildFailedException ex) { Debug.LogError(ex.Message); }
    }

    static void ValidateOrThrow(IEnumerable<string> scenePaths)
    {
        var paths = (scenePaths ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
        if (paths.Length == 0)
            throw new BuildFailedException("Pickup validation: no scenes supplied. Add your scenes to the active Build Profile.");

        var issues = new List<string>();
        var records = new List<Placement>();
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
            {
                issues.Add("Missing scene asset: '" + path + "'.");
                continue;
            }
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
            {
                issues.Add("Save this scene before validation/build: " + path);
                continue;
            }

            // Inspect saved scenes in isolated previews, even on incremental builds.
            // Keep the user's open scenes and selection intact. Never save or repair here.
            Scene preview = default;
            try
            {
                preview = EditorSceneManager.OpenPreviewScene(path);
                Collect(preview, path, records);
            }
            catch (Exception ex) { issues.Add("Could not inspect " + path + ": " + ex.Message); }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        CheckRecords(records, issues);
        if (issues.Count != 0)
            throw new BuildFailedException("Pickup ID validation blocked the build/check (" + issues.Count + " issue(s)):\n- " +
                string.Join("\n- ", issues) +
                "\nOpen the listed scenes. Repair blank/within-scene duplicate IDs with Tools > Freezer Game > Repair Pickup IDs In Active Scene, then SAVE. " +
                "For cross-scene duplicates, assign one placement a new permanent Pickup Id manually. Keep Item IDs unchanged. Run validation again.");

        Debug.Log("Pickup ID validation passed: " + records.Count + " placement(s) across " + paths.Length + " scene(s).");
    }

    struct Placement
    {
        public string Id, Location;
        public Placement(string id, string location) { Id = id; Location = location; }
    }

    static void Collect(Scene scene, string path, List<Placement> records)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var pickup in root.GetComponentsInChildren<ProgressPickup>(true))
        {
            bool editorOnly = false;
            var parts = new List<string>();
            for (Transform t = pickup.transform; t != null; t = t.parent)
            {
                if (t.CompareTag("EditorOnly")) editorOnly = true;
                parts.Add(t.name + "[" + t.GetSiblingIndex() + "]");
            }
            if (editorOnly) continue;
            parts.Reverse();
            int component = Array.IndexOf(pickup.GetComponents<ProgressPickup>(), pickup);
            records.Add(new Placement(pickup.pickupId,
                path + " :: " + string.Join("/", parts) + " :: ProgressPickup[" + component + "]"));
        }
    }

    static void CheckRecords(IEnumerable<Placement> records, List<string> issues)
    {
        var first = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Id))
                issues.Add("Missing Pickup Id at " + record.Location);
            else if (first.TryGetValue(record.Id, out string previous))
                issues.Add("Duplicate Pickup Id '" + record.Id + "':\n  First: " + previous + "\n  Also: " + record.Location);
            else first.Add(record.Id, record.Location);
        }
    }

    [MenuItem("Tools/Freezer Game/Run Pickup Build Validator Checks")]
    static void RunChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogError("Exit Play Mode before running validator checks."); return; }

        var issues = new List<string>();
        CheckRecords(new[] { new Placement(null, "A"), new Placement("  ", "B") }, issues);
        Require(issues.Count == 2, "Null and whitespace IDs must fail.");
        issues.Clear();
        CheckRecords(new[] { new Placement("flour.01", "SceneA :: Pickup"), new Placement("flour.01", "SceneB :: Pickup") }, issues);
        Require(issues.Count == 1 && issues[0].Contains("SceneA") && issues[0].Contains("SceneB"), "Duplicates must identify both placements.");
        issues.Clear();
        CheckRecords(new[] { new Placement("flour.01", "A"), new Placement("flour.02", "B") }, issues);
        Require(issues.Count == 0, "Distinct placement IDs must pass.");

        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Inactive pickup check");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, preview);
            root.AddComponent<BoxCollider2D>();
            var pickup = root.AddComponent<ProgressPickup>();
            pickup.enabled = false;
            pickup.pickupId = "test.inactive";
            var records = new List<Placement>();
            Collect(preview, "Temporary preview", records);
            Require(records.Count == 1 && records[0].Id == "test.inactive", "Inactive/disabled pickups must be included.");
            root.tag = "EditorOnly";
            records.Clear();
            Collect(preview, "Temporary preview", records);
            Require(records.Count == 0, "EditorOnly objects must be excluded.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Debug.Log("Pickup build validator checks passed. Verify the actual build-blocking checklist in INTEGRATION.md.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
