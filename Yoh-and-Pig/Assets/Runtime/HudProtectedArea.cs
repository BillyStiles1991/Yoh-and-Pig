using System.Collections.Generic;
using UnityEngine;
namespace FreezerGameplay
{
    public class HudProtectedArea:MonoBehaviour
    {
        public Rect viewportRect=new Rect(.1f,.7f,.8f,.3f);
        public static readonly List<HudProtectedArea> Active=new List<HudProtectedArea>();
        void OnEnable(){Active.Add(this);}void OnDisable(){Active.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Active.Clear();
    }
}
