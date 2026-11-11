using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string format = "FreezerSave";
    public int version = 1;
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
    public string chapterTitle = "";
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
    public string itemId; public int quantity;
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
