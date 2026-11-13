using System;
using System.IO;
using System.Text;
using UnityEngine;

// Desktop file backend: four independent files, with temp + previous-version backup.
public static class SaveRepository
{
    public const int SlotCount = 8;
#if UNITY_EDITOR
    public static string TestDirectoryOverride;
#endif
    public static string DirectoryPath
    {
        get
        {
#if UNITY_EDITOR
            if (TestDirectoryOverride != null) return TestDirectoryOverride;
#endif
            return Path.Combine(Application.persistentDataPath, "FreezerSaves");
        }
    }
    public static string SlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(DirectoryPath, "slot" + slot + ".json");
    }
    public static bool Exists(int slot) { return File.Exists(SlotPath(slot)); }
    public static SaveData Read(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return null;
        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        Validate(data, slot);
        return data;
    }
    public static void Validate(SaveData data, int slot)
    {
        if (data == null || data.format != "FreezerSave" || data.version != 1 || data.slot != slot)
            throw new InvalidDataException("Unsupported or damaged save file.");
        if (string.IsNullOrWhiteSpace(data.sceneName) || string.IsNullOrWhiteSpace(data.areaId) ||
            !DateTimeOffset.TryParse(data.savedUtc, out _) || data.progress == null)
            throw new InvalidDataException("Save metadata is incomplete.");
        Vector3 p = data.playerPosition;
        if (!Finite(p.x) || !Finite(p.y) || !Finite(p.z) ||
            double.IsNaN(data.progress.secondsPlayed) || double.IsInfinity(data.progress.secondsPlayed) || data.progress.secondsPlayed < 0)
            throw new InvalidDataException("Save contains invalid position or time.");
        var g = data.progress;
        if (g.inventory == null || g.equipment == null || g.recipes == null || g.quests == null ||
            g.openedChests == null || g.triggeredEvents == null || g.characters == null || g.unlockedMenuSections == null ||
            g.inventory.Exists(x => x == null || string.IsNullOrEmpty(x.itemId) || x.quantity <= 0) ||
            g.equipment.Exists(x => x == null || string.IsNullOrEmpty(x.slotId)) ||
            g.quests.Exists(x => x == null || string.IsNullOrEmpty(x.questId) || x.objectives == null || x.objectives.Exists(o => o == null)) ||
            g.characters.Exists(x => x == null || string.IsNullOrEmpty(x.characterId) || x.conversations == null ||
                x.questIds == null || x.profile == null || x.profile.Exists(f => f == null) || x.conversations.Exists(c => c == null)))
            throw new InvalidDataException("Save progression is incomplete.");
    }
    private static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    public static void Write(SaveData data, bool newSlotOnly = false)
    {
        Validate(data, data.slot);
        Directory.CreateDirectory(DirectoryPath);
        string path = SlotPath(data.slot), temp = path + ".tmp", backup = path + ".bak";
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
        try
        {
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (!newSlotOnly && File.Exists(path)) File.Replace(temp, path, backup);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
