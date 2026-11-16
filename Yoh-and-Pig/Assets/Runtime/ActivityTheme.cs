using TMPro;
using UnityEngine;
namespace FreezerGameplay
{
    [DefaultExecutionOrder(-150)]
    public class ActivityTheme:MonoBehaviour
    {
        public Sprite panelSprite,buttonSprite;public TMP_FontAsset font;
        public Color paper=new Color(.95f,.91f,.83f),ink=new Color(.18f,.12f,.09f),button=new Color(.83f,.74f,.57f);
        public static ActivityTheme Current{get;private set;}
        void Awake(){Current=this;}void OnDestroy(){if(Current==this)Current=null;}
    }
}
