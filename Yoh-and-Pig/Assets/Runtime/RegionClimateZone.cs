using UnityEngine;
namespace FreezerGameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class RegionClimateZone : MonoBehaviour
    {
        public RegionalClimate climate;public string regionId;
        void Awake(){GetComponent<Collider2D>().isTrigger=true;}
        void OnTriggerEnter2D(Collider2D other){if(climate&&other.GetComponentInParent<PlayerControlGate>())climate.SetRegion(regionId);}
    }
}
