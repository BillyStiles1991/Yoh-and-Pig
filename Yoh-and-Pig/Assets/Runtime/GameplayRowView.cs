using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FreezerGameplay
{
    public class GameplayRowView : MonoBehaviour
    {
        public Button button; public TMP_Text label; public Image icon;
        [Tooltip("Optional separate Discard 1 button on the row.")]
        public Button discardButton;
        [HideInInspector] public string key;
    }
}
