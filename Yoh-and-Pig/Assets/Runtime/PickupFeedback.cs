using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FreezerGameplay
{
    public class PickupFeedback : MonoBehaviour
    {
        [Header("HUD toast (this component stays active)")]
        public CanvasGroup toast;
        public Image icon, countBadge, keyBadge;
        public TMP_Text nameLabel, countLabel;
        [Min(.01f)] public float fadeSeconds = .25f;
        [Min(.1f)] public float visibleSeconds = 3f;
        [Header("Player art: both renderers are children of a dedicated head anchor")]
        public SpriteRenderer heldIcon, glow;
        [Min(.01f)] public float heldWorldSize = .65f;
        public AudioSource audioSource;
        public AudioClip pickupSound, discoverySound;
        public GameProgressStore store;
        float age, discoveryTime, duration;
        int burst;
        string lastItem;
        bool showing, discovering;
        Vector3 iconScale;
        Vector2 keyHome;
        bool keyPositionCached;

        void OnEnable()
        {
            if (store) store.Restored += ResetFeedback;
            ResetFeedback();
        }
        void OnDisable()
        {
            if (store) store.Restored -= ResetFeedback;
            ResetFeedback();
        }
        void ResetFeedback()
        {
            showing = false; lastItem = null; burst = 0;
            if (toast) { toast.alpha = 0; toast.interactable = false; toast.blocksRaycasts = false; }
            EndDiscovery();
        }

        public void ShowItem(ItemDefinition item, int quantity)
        {
            burst = showing && lastItem == item.id ? (int)System.Math.Min(int.MaxValue, (long)burst + quantity) : quantity;
            lastItem = item.id;
            if (icon)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
                ItemRarityIcon.Decorate(icon, item);
            }
            if (nameLabel) nameLabel.text = item.displayName;
            if (countBadge) countBadge.enabled = burst > 1;
            if (countLabel) countLabel.text = burst > 1 ? "×" + burst : "";
            if (keyBadge)
            {
                keyBadge.enabled = item.isKeyItem;
                // Share the upper-right corner; shift the key left when the quantity badge appears.
                if (!keyPositionCached) { keyHome = keyBadge.rectTransform.anchoredPosition; keyPositionCached = true; }
                keyBadge.rectTransform.anchoredPosition = keyHome + (burst > 1 && countBadge
                    ? Vector2.left * (countBadge.rectTransform.rect.width / 2 + keyBadge.rectTransform.rect.width / 2) : Vector2.zero);
            }
            BeginToast();
            if (audioSource && pickupSound) audioSource.PlayOneShot(pickupSound);
        }

        public void ShowFull()
        {
            lastItem = null;
            burst = 0;
            if (icon) icon.enabled = false;
            if (countBadge) countBadge.enabled = false;
            if (keyBadge) keyBadge.enabled = false;
            if (countLabel) countLabel.text = "";
            if (nameLabel) nameLabel.text = "Inventory full";
            BeginToast();
        }
        void BeginToast()
        {
            showing = true;
            age = 0;
            if (toast) toast.alpha = 0;
        }

        public void Discover(Sprite sprite, float seconds)
        {
            if (audioSource && discoverySound) audioSource.PlayOneShot(discoverySound);
            if (!heldIcon || !sprite) return;
            heldIcon.sprite = sprite;
            heldIcon.enabled = true;
            var size = sprite.bounds.size;
            iconScale = Vector3.one * (heldWorldSize / Mathf.Max(.001f, size.x, size.y));
            heldIcon.transform.localScale = iconScale;
            if (glow) glow.enabled = true;
            discoveryTime = 0;
            duration = Mathf.Max(.01f, seconds);
            discovering = true;
        }

        public void EndDiscovery()
        {
            discovering = false;
            if (heldIcon)
            {
                if (heldIcon.enabled && iconScale != Vector3.zero) heldIcon.transform.localScale = iconScale;
                heldIcon.transform.localRotation = Quaternion.identity;
                heldIcon.enabled = false;
            }
            if (glow) glow.enabled = false;
        }

        void Update()
        {
            if (showing)
            {
                age += Time.deltaTime;
                float fade = Mathf.Max(.01f, fadeSeconds);
                float remaining = 2 * fade + visibleSeconds - age;
                if (toast) toast.alpha = Mathf.Clamp01(Mathf.Min(age, remaining) / fade);
                if (remaining <= 0) { showing = false; lastItem = null; }
            }
            if (discovering)
            {
                discoveryTime += Time.deltaTime;
                float t = Mathf.Clamp01(discoveryTime / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 4) * Mathf.Sin(t * Mathf.PI);
                heldIcon.transform.localScale = iconScale * (1 + .18f * wave);
                heldIcon.transform.localRotation = Quaternion.Euler(0, 0, 11 * wave);
            }
        }
    }
}
