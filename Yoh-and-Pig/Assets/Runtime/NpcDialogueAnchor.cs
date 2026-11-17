using UnityEngine;
namespace FreezerGameplay
{
    public class NpcDialogueAnchor:MonoBehaviour
    {
        public string npcId;public Transform head;
        public Vector3 fallbackHeadOffset=new Vector3(0,1.1f,0);
        public Vector3 Position=>head?head.position:transform.position+fallbackHeadOffset;
    }
}
