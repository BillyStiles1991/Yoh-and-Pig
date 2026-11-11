using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Add to a character-record page. Dropdown lists discovered characters only.
public class CharacterJournalView : MonoBehaviour
{
    public GameProgressStore progress;
    public TMP_Dropdown characterDropdown;
    public TMP_Text content;
    public UnityEngine.UI.ScrollRect recordScroll;
    private void Update()
    {
        if (recordScroll == null) return;
        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        float amount = gamepad != null ? gamepad.rightStick.ReadValue().y : 0f;
        if (keyboard != null)
        {
            if (keyboard.pageUpKey.isPressed) amount = 1;
            if (keyboard.pageDownKey.isPressed) amount = -1;
        }
        if (Mathf.Abs(amount) > .2f)
            recordScroll.verticalNormalizedPosition = Mathf.Clamp01(recordScroll.verticalNormalizedPosition + amount * Time.unscaledDeltaTime * .5f);
    }
    private string selectedId;
    private int category;
    private List<NpcRecord> visible = new List<NpcRecord>();
    private void OnEnable()
    {
        if (progress == null || characterDropdown == null || content == null) return;
        progress.Changed += Refresh;
        characterDropdown.onValueChanged.AddListener(SelectCharacter);
        Refresh();
    }
    private void OnDisable()
    {
        if (progress != null) progress.Changed -= Refresh;
        if (characterDropdown != null) characterDropdown.onValueChanged.RemoveListener(SelectCharacter);
    }
    private void SelectCharacter(int index)
    {
        selectedId = index >= 0 && index < visible.Count ? visible[index].characterId : null; Render();
    }
    // Wire three UI buttons to these category methods.
    public void ShowConversations() { category = 0; Render(); }
    public void ShowQuests() { category = 1; Render(); }
    public void ShowProfile() { category = 2; Render(); }
    private void Refresh()
    {
        visible = progress.Data.characters;
        int index = visible.FindIndex(x => x.characterId == selectedId);
        if (index < 0) index = 0;
        characterDropdown.ClearOptions();
        characterDropdown.AddOptions(visible.Select(x => x.displayName).ToList());
        characterDropdown.interactable = visible.Count > 0;
        characterDropdown.SetValueWithoutNotify(index); characterDropdown.RefreshShownValue();
        selectedId = visible.Count > 0 ? visible[index].characterId : null;
        Render();
    }
    private void Render()
    {
        if (content == null || progress == null) return;
        content.richText = false;
        if (recordScroll != null) recordScroll.verticalNormalizedPosition = 1f;
        var npc = visible.Find(x => x.characterId == selectedId);
        if (npc == null) { content.text = "No characters discovered yet."; return; }
        var text = new StringBuilder(npc.displayName + "\n\n");
        if (category == 0)
        {
            foreach (var conversation in npc.conversations)
                text.AppendLine(conversation.title).AppendLine(conversation.transcript).AppendLine();
            if (npc.conversations.Count == 0) text.AppendLine("No recorded conversations yet.");
        }
        else if (category == 1)
        {
            var quests = progress.Data.quests.Where(x => npc.questIds.Contains(x.questId)).ToList();
            foreach (var quest in quests)
            {
                text.AppendLine(quest.title + " — " + quest.status);
                foreach (var objective in quest.objectives)
                    text.AppendLine(objective.objectiveId + ": " + objective.current + "/" + objective.required);
                text.AppendLine();
            }
            if (quests.Count == 0) text.AppendLine("No discovered quests for this character.");
        }
        else
        {
            foreach (string group in new[] { "Bio", "Likes", "Dislikes", "Secrets" })
            {
                var facts = npc.profile.Where(x => x.category == group).ToList();
                if (facts.Count == 0) continue; // Do not reveal that an undiscovered secret exists.
                text.AppendLine(group);
                foreach (var fact in facts) text.AppendLine("• " + fact.text);
                text.AppendLine();
            }
            if (npc.profile.Count == 0) text.AppendLine("No profile details discovered yet.");
        }
        content.text = text.ToString();
    }
}
