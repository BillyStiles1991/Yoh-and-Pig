using System;
using UnityEngine;
namespace FreezerGameplay
{
    [Serializable] public class ItemAmount { public string itemId; [Min(1)] public int quantity = 1; }
    public enum ItemType { General = 0, Ingredient = 1, Food = 2, Equipment = 3 }
    [Serializable] public class ItemDefinition
    {
        public string id, displayName; [TextArea] public string description; public Sprite icon;
        public ItemType itemType;
        [Tooltip("Prevents manual discard. Authored recipes can still consume this item.")]
        public bool isKeyItem;
        [Tooltip("Empty for non-equipment. Examples: Hand, Head, Body.")] public string equipmentSlot;
        public float power, defence;
    }
    [Serializable] public class RecipeDefinition
    { public string id, displayName; public ItemAmount[] ingredients = new ItemAmount[0]; public ItemAmount[] results = new ItemAmount[0]; }
    [Serializable] public class ObjectiveDefinition
    { public string id, description, eventId; [Min(1)] public int required = 1; }
    [Serializable] public class QuestDefinition
    {
        public string id, title, characterId, characterName;
        public ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];
        public ItemAmount[] rewards = new ItemAmount[0]; public string[] recipeRewards = new string[0];
    }
    [Serializable] public class DialogueLine { public string speaker; [TextArea(2,6)] public string text; }
    [Serializable] public class DialogueDefinition
    {
        public string id, title, characterId, characterName;
        public string requiredEvent, requiredCompletedQuest;
        public DialogueLine[] lines = new DialogueLine[0];
        [Tooltip("Granted on first completion only.")] public string startQuest, completeQuest, learnRecipe, triggerEvent;
    }
    [CreateAssetMenu(menuName="Freezer Game/Gameplay Database")]
    public class GameplayDatabase : ScriptableObject
    {
        public ItemDefinition[] items = new ItemDefinition[0];
        public RecipeDefinition[] recipes = new RecipeDefinition[0];
        public QuestDefinition[] quests = new QuestDefinition[0];
        public DialogueDefinition[] dialogues = new DialogueDefinition[0];
        public ItemDefinition Item(string id) => Array.Find(items, x => x != null && x.id == id);
        public RecipeDefinition Recipe(string id) => Array.Find(recipes, x => x != null && x.id == id);
        public QuestDefinition Quest(string id) => Array.Find(quests, x => x != null && x.id == id);
        public DialogueDefinition Dialogue(string id) => Array.Find(dialogues, x => x != null && x.id == id);
    }
}
