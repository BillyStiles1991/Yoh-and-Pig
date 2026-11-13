using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Single-scene title flow. Hold the selected snapshot only until the destination is ready.
internal static class TitleSlotLoader
{
    private static SaveData pending;
    private static string titlePath;
    private static string failure;
    public static bool Busy => pending != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= Apply;
        pending = null;
        titlePath = null;
        failure = null;
    }

    public static string TakeFailure()
    {
        string message = failure;
        failure = null;
        return message;
    }

    public static bool Begin(int slot, out string message)
    {
        message = "";
        if (Busy) { message = "A slot is already loading."; return false; }
        try
        {
            var data = SaveRepository.Read(slot);
            if (data == null) throw new InvalidOperationException("That slot is empty.");
            var title = SceneManager.GetActiveScene();
            if (title.buildIndex < 0 || title.name == data.sceneName || !Application.CanStreamedLevelBeLoaded(data.sceneName))
                throw new InvalidOperationException("Include the title and saved gameplay scene in the build scene list, with distinct scene names.");
            titlePath = title.path;
            pending = data;
            SceneManager.sceneLoaded += Apply;
            if (SceneManager.LoadSceneAsync(data.sceneName, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Could not open the gameplay scene.");
            return true;
        }
        catch (Exception e)
        {
            Reset();
            message = e.Message;
            return false;
        }
    }

    private static void Apply(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= Apply;
        var data = pending;
        pending = null;
        try
        {
            SaveController saves = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<SaveController>(false))
            {
                if (saves != null) throw new InvalidOperationException("Multiple active SaveControllers in the gameplay scene.");
                saves = candidate;
            }
            if (!saves) throw new InvalidOperationException("The gameplay scene needs a SaveController.");
            if (!saves.RestoreSnapshot(data, out string message)) throw new InvalidOperationException(message);
        }
        catch (Exception e)
        {
            failure = e.Message;
            Debug.LogError(failure);
            SceneManager.LoadSceneAsync(titlePath, LoadSceneMode.Single);
        }
    }
}
