using UnityEngine;
namespace FreezerGameplay
{
    public class OutfitAppearance : MonoBehaviour
    {
        public InventoryController inventory; public Animator animator;
        RuntimeAnimatorController original; string last;
        void Awake() { if (!animator) animator = GetComponentInChildren<Animator>(); if (animator) original = animator.runtimeAnimatorController; }
        void LateUpdate()
        {
            if (!inventory || !animator) return;
            string id = inventory.store.Data.outfit;
            if (id == last) return; last = id;
            animator.runtimeAnimatorController = inventory.database.Item(id)?.outfitAnimator ?? original;
        }
    }
}
