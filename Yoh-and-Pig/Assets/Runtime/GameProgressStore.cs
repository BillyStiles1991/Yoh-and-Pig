using System;
using System.Linq;
using UnityEngine;

// Keep one of these on an always-active scene object.
public class GameProgressStore : MonoBehaviour
{
    public GameProgress Data { get; private set; } = new GameProgress();
    public event Action Changed;
    // Subscribe custom inventory/equipment/etc. views to this after loading.
    public event Action Restored;

    private void Update()
    {
        // Active gameplay time: excludes paused menus and lost application focus.
        if (Application.isFocused && Time.timeScale > 0f)
            Data.secondsPlayed += Time.unscaledDeltaTime;
    }
    public GameProgress Snapshot()
    {
        return JsonUtility.FromJson<GameProgress>(JsonUtility.ToJson(Data));
    }
    public void Restore(GameProgress data)
    {
        Data = data;
        Notify(Restored);
        Notify(Changed);
    }
    public void NotifyChanged() { Notify(Changed); }
    private void Notify(Action listeners)
    {
        if (listeners == null) return;
        foreach (Action listener in listeners.GetInvocationList())
            try { listener(); } catch (Exception e) { Debug.LogException(e, this); }
    }
    public void SetItemQuantity(string itemId, int quantity)
    {
        RequireId(itemId);
        var entry = Data.inventory.Find(x => x.itemId == itemId && string.IsNullOrEmpty(x.instanceId));
        if (quantity <= 0) { if (entry != null) Data.inventory.Remove(entry); }
        else if (entry == null) Data.inventory.Add(new InventoryEntry { itemId = itemId, quantity = quantity });
        else entry.quantity = quantity;
        NotifyChanged();
    }
    public void SetEquipment(string slotId, string itemId, string instanceId = "")
    {
        RequireId(slotId);
        Data.equipment.RemoveAll(x => x.slotId == slotId);
        if (!string.IsNullOrEmpty(itemId)) Data.equipment.Add(new EquipmentEntry
            { slotId = slotId, itemId = itemId, instanceId = instanceId });
        NotifyChanged();
    }
    public void CollectRecipe(string id) { AddUnique(Data.recipes, id); NotifyChanged(); }
    public void UnlockMenu(string id) { AddUnique(Data.unlockedMenuSections, id); NotifyChanged(); }
    public bool HasEvent(string id) { return Data.triggeredEvents.Contains(id); }
    public bool IsChestOpen(string id) { return Data.openedChests.Contains(id); }
    public void MarkChestOpened(string id) { AddUnique(Data.openedChests, id); NotifyChanged(); }
    public void MarkEvent(string id) { AddUnique(Data.triggeredEvents, id); NotifyChanged(); }
    public void SetQuest(string id, string title, string status)
    {
        RequireId(id);
        if (status != "Active" && status != "Completed" && status != "Failed")
            throw new ArgumentException("Quest status must be Active, Completed or Failed.");
        var quest = Data.quests.Find(x => x.questId == id);
        if (quest == null) { quest = new QuestRecord { questId = id }; Data.quests.Add(quest); }
        quest.title = title; quest.status = status; NotifyChanged();
    }
    public void SetObjective(string questId, string objectiveId, int current, int required)
    {
        RequireId(objectiveId);
        var quest = Data.quests.Find(x => x.questId == questId);
        if (quest == null) throw new ArgumentException("Call SetQuest before SetObjective.");
        var objective = quest.objectives.Find(x => x.objectiveId == objectiveId);
        if (objective == null) { objective = new ObjectiveRecord { objectiveId = objectiveId }; quest.objectives.Add(objective); }
        objective.required = Mathf.Max(0, required);
        objective.current = Mathf.Clamp(current, 0, objective.required);
        NotifyChanged();
    }
    private NpcRecord Character(string id, string name)
    {
        RequireId(id);
        var npc = Data.characters.Find(x => x.characterId == id);
        if (npc == null) { npc = new NpcRecord { characterId = id, displayName = name }; Data.characters.Add(npc); }
        if (!string.IsNullOrEmpty(name)) npc.displayName = name;
        return npc;
    }
    public void RecordConversation(string characterId, string name, string conversationId, string title, string transcript)
    {
        RequireId(conversationId);
        var npc = Character(characterId, name);
        if (!npc.conversations.Any(x => x.conversationId == conversationId))
            npc.conversations.Add(new ConversationRecord { conversationId = conversationId, title = title,
                transcript = transcript, unlockedUtc = DateTime.UtcNow.ToString("O") });
        NotifyChanged();
    }
    public void LinkCharacterQuest(string characterId, string name, string questId)
    {
        AddUnique(Character(characterId, name).questIds, questId); NotifyChanged();
    }
    public void UnlockProfileFact(string characterId, string name, string factId, string category, string text, string sourceId)
    {
        RequireId(factId);
        if (category != "Bio" && category != "Likes" && category != "Dislikes" && category != "Secrets")
            throw new ArgumentException("Unknown profile category.");
        var npc = Character(characterId, name);
        if (!npc.profile.Any(x => x.factId == factId))
            npc.profile.Add(new ProfileFact { factId = factId, category = category, text = text, sourceId = sourceId });
        NotifyChanged();
    }
    private static void RequireId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable non-empty ID is required.");
    }
    private static void AddUnique(System.Collections.Generic.List<string> list, string id)
    {
        RequireId(id); if (!list.Contains(id)) list.Add(id);
    }
}
