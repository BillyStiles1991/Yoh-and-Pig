using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string format = "FreezerSave";
    public int version = 2;
    public int slot;
    public string displayName;
    public string savedUtc;
    public string sceneName;
    public string areaId;
    public string locationName;
    public string savePointId;
    public Vector3 playerPosition;
    public string screenshotJpegBase64;
    public GameProgress progress = new GameProgress();
}

[Serializable]
public class GameProgress
{
    public int reviewVersion;
    public List<NpcReviewRecord> reviews = new List<NpcReviewRecord>();
    public string chapterTitle = "";
    public int activityVersion = 1, cashola, activeBar, selectedQuickSlot, worldSeed;
    public float sicknessLevel;
    public List<QuickAssignment> quickSlots = new List<QuickAssignment>();
    public List<ToolRecord> tools = new List<ToolRecord>();
    public List<CulinarySkillRecord> skills = new List<CulinarySkillRecord>();
    public List<ForageRecord> forage = new List<ForageRecord>();
    public List<RegionYearRecord> regionalYears = new List<RegionYearRecord>();
    public List<ClimateOverrideRecord> climateOverrides = new List<ClimateOverrideRecord>();
    public int inventorySlots = 10, chefLevel = 1;
    public double daySeconds; public float secondsPerDay = 1200;
    public float sicknessRemaining;
    public List<string> outfits = new List<string>();
    public string outfit = "", heldItem = ""; public int heldWeight;
    public bool pigSeparated;
    public List<string> discoveredItems = new List<string>();
    public List<string> examinedItems = new List<string>();
    public List<string> identifiedItems = new List<string>();
    public List<WorldDropRecord> drops = new List<WorldDropRecord>();
    public List<NpcBondRecord> bonds = new List<NpcBondRecord>();
    public double secondsPlayed;
    public List<InventoryEntry> inventory = new List<InventoryEntry>();
    public List<EquipmentEntry> equipment = new List<EquipmentEntry>();
    public List<string> recipes = new List<string>();
    public List<QuestRecord> quests = new List<QuestRecord>();
    public List<string> openedChests = new List<string>();
    public List<string> triggeredEvents = new List<string>();
    public List<NpcRecord> characters = new List<NpcRecord>();
    public List<string> unlockedMenuSections = new List<string>();
}
[Serializable] public class InventoryEntry
{
    public string itemId; public int quantity; public int unitWeight;
    // Stable instance ID for unique items; empty for ordinary stackable items.
    public string instanceId; public float durability;
}
[Serializable] public class EquipmentEntry
{
    public string slotId; public string itemId; public string instanceId;
}
[Serializable] public class QuestRecord
{
    public string questId; public string title;
    public string status = "Active"; // Active, Completed, Failed
    public List<ObjectiveRecord> objectives = new List<ObjectiveRecord>();
}
[Serializable] public class ObjectiveRecord
{
    public string objectiveId; public int current; public int required;
}
[Serializable] public class NpcRecord
{
    public string characterId; public string displayName;
    public List<ConversationRecord> conversations = new List<ConversationRecord>();
    public List<string> questIds = new List<string>();
    // Only discovered facts are stored here. Never copy a complete secret profile.
    public List<ProfileFact> profile = new List<ProfileFact>();
}
[Serializable] public class ConversationRecord
{
    public string conversationId; public string title;
    public string transcript; public string unlockedUtc;
}
[Serializable] public class ProfileFact
{
    public string factId; public string category; // Bio, Likes, Dislikes, Secrets
    public string text; public string sourceId;
}

[Serializable] public class WorldDropRecord
{
    public string id, itemId, scenePath; public int quantity, unitWeight;
    public Vector3 position; public double expiresAt;
}
[Serializable] public class NpcBondRecord
{
    public string npcId; public int bond, lastMealDay = -1; public bool masterpieceGiven;
}

[Serializable] public class QuickAssignment { public int slot; public string itemId; public int unitWeight; }
[Serializable] public class ToolRecord { public string itemId; public float sharpness; }
[Serializable] public class CulinarySkillRecord { public string id; public float xp; }
[Serializable] public class ForageRecord { public string id; public int nextDay; }
[Serializable] public class RegionalSeasonRecord { public string seasonId; public int startDay, length; }
[Serializable] public class RegionYearRecord
{
    public string regionId; public int year;
    public List<RegionalSeasonRecord> seasons = new List<RegionalSeasonRecord>();
    public List<string> weather = new List<string>();
}
[Serializable] public class ClimateOverrideRecord
{
    public string ruleId, regionId, seasonId, weatherId; public double startTime, endTime;
}

[Serializable] public class NpcReviewRecord
{
    public string npcId;public int points,lastMealDay=-1;
    public bool masterpieceGiven,keyNpcUnlocked,animatedBannerUnlocked,customFontUnlocked;
    public List<string> awardedSources = new List<string>();
}
