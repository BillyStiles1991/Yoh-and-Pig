using UnityEngine;
namespace FreezerGameplay
{
    public class NpcDialogueInteraction : MonoBehaviour
    {
        public DialogueController dialogue;
        [Tooltip("First eligible unfinished conversation wins. Put later quest conversations first.")]
        public string[] conversationIds = new string[0];
        public void Talk()
        {
            string replay = null;
            foreach (string id in conversationIds)
            {
                if (!dialogue.CanOpen(id)) continue;
                if (replay == null) replay = id;
                if (!dialogue.quests.inventory.store.HasEvent("dialogue:" + id)) { dialogue.Open(id); return; }
            }
            if (replay != null) dialogue.Open(replay);
        }
    }
}
