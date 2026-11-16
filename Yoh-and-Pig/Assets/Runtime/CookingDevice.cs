using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FreezerGameplay
{
    public class CookingDevice : MonoBehaviour
    {
        public string deviceId; public bool canCook = true, canIdentify = true, consumesIdentification = true;
        public int minimumChefLevel = 1; public float reach = 1.8f;
        public string requiredEvent; public GameProgressStore store; public Sprite icon;
        static readonly List<CookingDevice> all = new List<CookingDevice>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => all.Clear();
        void OnEnable() { all.Add(this); } void OnDisable() { all.Remove(this); }
        public bool Available => string.IsNullOrEmpty(requiredEvent) || (store && store.HasEvent(requiredEvent));
        public static CookingDevice Find(string id, Vector3 position, bool identify)
        {
            return all.Where(d => d && d.Available && d.deviceId == id && (identify ? d.canIdentify : d.canCook) &&
                Vector2.Distance(d.transform.position, position) <= d.reach).OrderBy(d => Vector2.Distance(d.transform.position, position)).FirstOrDefault();
        }
    }
}
