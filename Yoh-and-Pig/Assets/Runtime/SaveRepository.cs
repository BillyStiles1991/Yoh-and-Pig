using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Desktop file backend: eight independent files, with temp + previous-version backup.
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
            return Path.Combine(Application.persistentDataPath, "FreezerSaves_v2");
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
        if (data == null || data.format != "FreezerSave" || data.version != 2 || data.slot != slot)
            throw new InvalidDataException("Unsupported or damaged save file.");
        if (string.IsNullOrWhiteSpace(data.sceneName) || string.IsNullOrWhiteSpace(data.areaId) ||
            !DateTimeOffset.TryParse(data.savedUtc, out _) || data.progress == null)
            throw new InvalidDataException("Save metadata is incomplete.");
        Vector3 p = data.playerPosition;
        if (!Finite(p.x) || !Finite(p.y) || !Finite(p.z) ||
            double.IsNaN(data.progress.secondsPlayed) || double.IsInfinity(data.progress.secondsPlayed) || data.progress.secondsPlayed < 0)
            throw new InvalidDataException("Save contains invalid position or time.");
        var g = data.progress;
        GameProgressStore.NormalizeActivities(g);
        if(g.cashola<0 || !Finite(g.sicknessLevel) || g.sicknessLevel<0 || g.sicknessLevel>100 ||
            g.quickSlots.Exists(x=>x==null || x.slot<0 || x.slot>=50 || string.IsNullOrWhiteSpace(x.itemId)) ||
            g.tools.Exists(x=>x==null || string.IsNullOrWhiteSpace(x.itemId) || !Finite(x.sharpness) || x.sharpness<0) ||
            g.skills.Exists(x=>x==null || string.IsNullOrWhiteSpace(x.id) || !Finite(x.xp) || x.xp<0))
            throw new InvalidDataException("Invalid activity data.");
        if(g.quickSlots.Select(x=>x.slot).Distinct().Count()!=g.quickSlots.Count ||
            g.tools.Select(x=>x.itemId).Distinct().Count()!=g.tools.Count ||
            g.skills.Select(x=>x.id).Distinct().Count()!=g.skills.Count ||
            g.forage.Any(x=>x==null||string.IsNullOrWhiteSpace(x.id)||x.nextDay<1) ||
            g.regionalYears.Any(r=>r==null||string.IsNullOrWhiteSpace(r.regionId)||r.year<0||r.seasons==null||r.weather==null||r.weather.Count!=112||r.seasons.Count<1||r.seasons.Count>8||r.seasons.Any(s=>s==null||string.IsNullOrWhiteSpace(s.seasonId)||s.length<1)||r.seasons.Sum(s=>s.length)!=112) ||
            g.climateOverrides.Any(x=>x==null||string.IsNullOrWhiteSpace(x.ruleId)||string.IsNullOrWhiteSpace(x.regionId)||double.IsNaN(x.startTime)||double.IsNaN(x.endTime)||double.IsInfinity(x.startTime)||double.IsInfinity(x.endTime)||x.endTime<=x.startTime))
            throw new InvalidDataException("Invalid saved activity IDs, calendar or override.");
        foreach(var r in g.regionalYears){int start=0;foreach(var s in r.seasons){if(s.startDay!=start)throw new InvalidDataException("Invalid season coverage.");start+=s.length;}}
        if(g.reviews==null || g.reviews.Any(r=>r==null||string.IsNullOrWhiteSpace(r.npcId)||r.points<0||r.lastMealDay< -1||r.awardedSources==null)||g.reviews.Select(r=>r.npcId).Distinct().Count()!=g.reviews.Count)
            throw new InvalidDataException("Invalid NPC Review data.");
        if (g.inventorySlots != 10 && g.inventorySlots != 20 && g.inventorySlots != 30 && g.inventorySlots != 50)
            throw new InvalidDataException("Invalid inventory slot upgrade.");
        if (g.chefLevel < 1 || !Finite(g.sicknessRemaining) || g.sicknessRemaining < 0 ||
            double.IsNaN(g.daySeconds) || double.IsInfinity(g.daySeconds) || g.daySeconds < 0 ||
            !Finite(g.secondsPerDay) || g.secondsPerDay <= 0 || g.outfits == null || g.drops == null ||
            g.identifiedItems == null || g.discoveredItems == null || g.examinedItems == null || g.bonds == null ||
            g.drops.Exists(d => d == null || string.IsNullOrWhiteSpace(d.id) || string.IsNullOrWhiteSpace(d.itemId) ||
                string.IsNullOrWhiteSpace(d.scenePath) || d.quantity <= 0 || d.unitWeight < 1 ||
                double.IsNaN(d.expiresAt) || double.IsInfinity(d.expiresAt) ||
                !Finite(d.position.x) || !Finite(d.position.y) || !Finite(d.position.z)) ||
            g.bonds.Exists(b => b == null || string.IsNullOrWhiteSpace(b.npcId)))
            throw new InvalidDataException("Invalid v2 gameplay state.");
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
