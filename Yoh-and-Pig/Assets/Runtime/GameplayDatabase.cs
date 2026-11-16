using System;
using UnityEngine;
namespace FreezerGameplay
{
    public enum ItemSize { Tiny, Small, Mid, Big }
    public static class ItemSizing
    {
        public static int Weight(ItemSize size) => size == ItemSize.Small ? 99 : size == ItemSize.Mid ? 1188 : size == ItemSize.Big ? 3564 : 1;
        public static ItemSize Size(int weight) => weight >= 3564 ? ItemSize.Big : weight >= 1188 ? ItemSize.Mid : weight >= 99 ? ItemSize.Small : ItemSize.Tiny;
        public static int Limit(int weight) => Size(weight) == ItemSize.Tiny ? 99 : Size(weight) == ItemSize.Small ? 12 : Size(weight) == ItemSize.Mid ? 3 : 1;
    }
    [Serializable] public class FlavourDefinition { public string id, family, displayName; }
    [Serializable] public class NpcReaction
    {
        public string itemId, flavour; public bool matchCategory; public ItemType category;
        public int bondChange = 1; public string[] lines = new string[0];
    }
    [Serializable] public class NpcFactUnlock
    {
        public int requiredBond; public string factId, category = "Likes"; [TextArea] public string text;
    }
    [Serializable] public class NpcHandIn
    {
        public string questId, itemId, objectiveEvent; public int quantity = 1; public bool consume = true;
    }
    [Serializable] public class NpcProfileDefinition
    {
        public string id, displayName;
        public string[] personalityLines = { "That's interesting." };
        public NpcReaction[] reactions = new NpcReaction[0];
        public string[] lovedMasterpieces = new string[0];
        public int defaultMealBond = 1, masterpieceBond = 10;
        public NpcFactUnlock[] facts = new NpcFactUnlock[0];
        public NpcHandIn[] handIns = new NpcHandIn[0];
    }
    [Serializable] public class ItemAmount { public string itemId; [Min(1)] public int quantity = 1; public int unitWeight; }
    public enum ItemType { General = 0, Ingredient = 1, Food = 2, Outfit = 3, Masterpiece = 4 }
    [Serializable] public class ItemDefinition
    {
        public string id, displayName; [TextArea] public string description; public Sprite icon;
        public ItemType itemType;
        public ItemRarity rarity;
        public ItemSize size;
        public bool startsUnidentified, dangerousRaw;
        public string unidentifiedName = "Unidentified ingredient";
        [TextArea] public string unidentifiedDescription = "Examine or identify this ingredient.";
        public Sprite unidentifiedIcon, dangerIcon, identifyDeviceIcon;
        [Min(1)] public int identificationLevel = 1;
        public string identificationDevice;
        public string[] flavours = new string[0];
        public string[] goesWellWith = new string[0];
        public RuntimeAnimatorController outfitAnimator, previewAnimator;
        public GameObject detailEffectPrefab;
        public string previewDanceBool = "Dancing";
        // Dormant extension data. No current gameplay stats or abilities are applied.
        public string[] futureGearSkills = new string[0];
        [Tooltip("Prevents manual discard. Authored recipes can still consume this item.")]
        [UnityEngine.Serialization.FormerlySerializedAs("isKeyItem")]
        [SerializeField, InspectorName("Is Key Item")] private bool protectedItem;
        public bool isKeyItem
        {
            get => protectedItem || rarity == ItemRarity.KeyItem;
            set => protectedItem = value;
        }
        [HideInInspector] public string equipmentSlot; // Legacy field reserved for future gear.
        [HideInInspector] public float power, defence;
    }
    [Serializable] public class RecipeDefinition
    { public string id, displayName; public ItemAmount[] ingredients = new ItemAmount[0]; public ItemAmount[] results = new ItemAmount[0]; public string deviceId; [Min(1)] public int chefLevel = 1; }
    [Serializable] public class ObjectiveDefinition
    { public string id, description, eventId; [Min(1)] public int required = 1; }
    [Serializable] public class QuestDefinition
    {
        public string id, title, characterId, characterName;
        public ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];
        [Tooltip("0 = no upgrade; otherwise 20, 30 or 50")] public int inventorySlotReward;
        public int chefLevelReward;
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
        public string unidentifiedMessId = "item.unidentified_mess";
        public string[] flavourFamilies = { "Marine", "Spicy", "Citrusy" };
        public FlavourDefinition[] flavourDefinitions = new FlavourDefinition[0];
        public NpcProfileDefinition[] npcProfiles = new NpcProfileDefinition[0];
        public DialogueDefinition[] dialogues = new DialogueDefinition[0];
        public string FlavourLabel(string id) { var f = Array.Find(flavourDefinitions, x => x != null && x.id == id); return f == null ? id : f.family + "/" + f.displayName; }
        public NpcProfileDefinition Npc(string id) => Array.Find(npcProfiles, x => x != null && x.id == id);
        public ItemDefinition Item(string id) => Array.Find(items, x => x != null && x.id == id);
        public RecipeDefinition Recipe(string id) => Array.Find(recipes, x => x != null && x.id == id);
        public QuestDefinition Quest(string id) => Array.Find(quests, x => x != null && x.id == id);
        public DialogueDefinition Dialogue(string id) => Array.Find(dialogues, x => x != null && x.id == id);
    }
}
