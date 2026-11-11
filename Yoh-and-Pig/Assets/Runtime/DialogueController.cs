using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace FreezerGameplay
{
    // Attach to an always-active controller, not to panel itself.
    public class DialogueController : MonoBehaviour
    {
        public QuestController quests; public RecipeController recipes; public PlayerControlGate player;
        public GameObject panel; public TMP_Text speaker, body; public Button nextButton, closeButton;
        DialogueDefinition current; int line; GameObject previousFocus;
        GameProgressStore Store => quests.inventory.store;
        GameplayDatabase Database => quests.inventory.database;
        void Awake()
        { panel.SetActive(false); nextButton.onClick.AddListener(Next); closeButton.onClick.AddListener(Close); }
        public bool CanOpen(string id)
        {
            var d = Database.Dialogue(id);
            return d != null && !string.IsNullOrWhiteSpace(d.characterId) && d.lines.Length > 0 && d.lines.All(x => x != null) &&
                (string.IsNullOrWhiteSpace(d.completeQuest) || quests.Get(d.completeQuest)?.status == "Completed" || quests.CanComplete(d.completeQuest)) &&
                (string.IsNullOrWhiteSpace(d.requiredEvent) || Store.HasEvent(d.requiredEvent)) &&
                (string.IsNullOrWhiteSpace(d.requiredCompletedQuest) || quests.Get(d.requiredCompletedQuest)?.status == "Completed");
        }
        public void Open(string id)
        {
            if (current != null || !CanOpen(id) || !player.TryLock(this, true)) return;
            previousFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            current = Database.Dialogue(id); line = 0; panel.SetActive(true); Show();
        }
        void Show()
        { speaker.text = current.lines[line].speaker; body.text = current.lines[line].text; if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject); }
        void Update()
        { if (current != null && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false))) Close(); }
        public void Next()
        {
            if (current == null) return;
            if (line + 1 < current.lines.Length) { line++; Show(); return; }
            string marker = "dialogue:" + current.id;
            if (!Store.HasEvent(marker))
            {
                // Check authoring mistakes/turn-in readiness before granting anything.
                if ((!string.IsNullOrEmpty(current.completeQuest) && (quests.Get(current.completeQuest)?.status != "Completed" && !quests.CanComplete(current.completeQuest))) ||
                    (!string.IsNullOrEmpty(current.startQuest) && (quests.Get(current.startQuest) == null && !quests.CanStartQuest(current.startQuest))) ||
                    (!string.IsNullOrEmpty(current.learnRecipe) && Database.Recipe(current.learnRecipe) == null))
                { body.text = "This conversation cannot finish yet. Check quest requirements or dialogue setup."; return; }
                // Set marker first so callbacks cannot grant the same completion twice.
                Store.Data.triggeredEvents.Add(marker);
                if (!string.IsNullOrEmpty(current.startQuest)) quests.StartQuest(current.startQuest);
                if (!string.IsNullOrEmpty(current.completeQuest)) quests.CompleteQuest(current.completeQuest);
                if (!string.IsNullOrEmpty(current.learnRecipe)) recipes.Learn(current.learnRecipe);
                if (!string.IsNullOrEmpty(current.triggerEvent)) Store.MarkEvent(current.triggerEvent);
                var transcript = new StringBuilder();
                foreach (var l in current.lines) transcript.AppendLine(l.speaker + ": " + l.text);
                Store.RecordConversation(current.characterId,current.characterName,current.id,current.title,transcript.ToString());
                quests.ReportEvent("talk:" + current.id);
            }
            Close();
        }
        public void Close()
        {
            if (current == null) return;
            current = null; panel.SetActive(false); player.Unlock(this);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousFocus != null && previousFocus.activeInHierarchy ? previousFocus : null);
        }
        void OnDisable() { Close(); }
    }
}
