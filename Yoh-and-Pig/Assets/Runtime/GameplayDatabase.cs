using System;
using UnityEngine;
namespace FreezerGameplay
{
    public enum ToolKind { None, Basket, Knife, ProtectiveGloves }
    [Serializable] public class PreparationDefinition
    {
        public string id, displayName, ingredientId, deviceId = "chopping_board";
        public int ingredientQuantity = 1, knifeLevel = 1;
        public string[] optimalKnives = new string[0], fineKnives = new string[0], badKnives = new string[0];
        public ItemAmount[] fineResults = new ItemAmount[0], regularResults = new ItemAmount[0], poorResults = new ItemAmount[0];
        public float fineXP = 15, regularXP = 8, poorXP = 2, badSharpnessLoss = 5;
    }
    [Serializable] public class WeatherChoice { public string id; public float weight = 1; }
    [Serializable] public class SeasonChoice
    {
        public string id; public float weight = 1; public int minimumStoryProgress;
        public WeatherChoice[] weather = new WeatherChoice[0];
    }
    [Serializable] public class ClimateStoryRule
    {
        public string id, eventId, seasonId, weatherId;
        public float durationDays = 1; public float durationSeconds;
        public int priority;
    }
    [Serializable] public class RegionalClimateDefinition
    {
        public string id;
        public float[] seasonCountWeights = { .2f, 2, 25, 40, 25, 6, 1, .2f };
        public SeasonChoice[] seasons = new SeasonChoice[0];
        public string[] storyProgressEvents = new string[0];
        public ClimateStoryRule[] storyRules = new ClimateStoryRule[0];
    }
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
    public enum ItemType { General = 0, Ingredient = 1, Food = 2, Outfit = 3, Masterpiece = 4, Tool = 5 }
    [Serializable] public class ItemDefinition
    {
        public string id, displayName; [TextArea] public string description; public Sprite icon;
        public ItemType itemType;
        public ItemRarity rarity;
        public ItemSize size;
        public bool startsUnidentified, dangerousRaw, poisonousRaw;
        public int forageReferenceLevel = 1;
        public ToolKind toolKind;
        public bool ultimate, ultimatePlus;
        [Range(.05f,.10f)] public float ultimateBonusChance = .05f;
        public float maximumSharpness = 100;
        public float sharpeningVariance = .6f, sharpeningFrequency = 1, sharpeningSeconds = 20;
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
        public int chefLevelReward, casholaReward;
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
        [Header("Skill Progression")]
        [Min(1)] public float skillXPBase=100;
        [Min(1)] public float skillXPGrowth=1.25f;
        [Range(2,100)] public int skillLevelCap=100;
        public PreparationDefinition[] preparations = new PreparationDefinition[0];
        public RegionalClimateDefinition[] regions = new RegionalClimateDefinition[0];
        public int[] rarityForageXP = { 5, 8, 15, 25, 40, 5 };
        public float bonusForagePerLevel = .03f, maximumForageBonus = .5f;
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
