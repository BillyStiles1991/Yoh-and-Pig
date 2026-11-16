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
        public InventoryController inventory;
        [Header("Successive pickup counter")]
        [Tooltip("Optional border inside Toast. Defaults to the Toast RectTransform.")]
        public RectTransform counterBorder;
        public Vector2 counterOffset = new Vector2(-35, 2);
        public float counterAngle = 12;
        public Vector2 counterSize = new Vector2(90, 44);
        [Min(10)] public float counterFontSize = 26;
        public Color secondColor = new Color(.3f, 1f, .4f);
        public Color thirdColor = new Color(.75f, .35f, 1f);
        public Color fourthColor = new Color(1f, .78f, .15f);
        TMP_Text counterGlow;
        Material counterMaterial;
        float holdRemaining, discoveryTime, duration, counterPulse;
        bool fadeOut;
        public int RecentQuantity => burst;
        int burst;
        string lastItem;
        bool showing, discovering;
        Vector3 iconScale;


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
            showing = false; lastItem = null; burst = 0; fadeOut = false;
            if (countLabel) countLabel.text = "";
            if (counterGlow) counterGlow.text = "";
            if (toast) { toast.alpha = 0; toast.interactable = false; toast.blocksRaycasts = false; }
            EndDiscovery();
        }

        public void ShowItem(ItemDefinition item, int quantity)
        {
            burst = showing && lastItem == item.id ? (int)System.Math.Min(int.MaxValue, (long)burst + quantity) : quantity;
            lastItem = item.id;
            if (icon)
            {
                icon.sprite = inventory ? inventory.Icon(item.id) : item.icon;
                icon.enabled = icon.sprite != null;
                ItemRarityIcon.Decorate(icon, item);
            }
            if (nameLabel) nameLabel.text = inventory ? inventory.Name(item.id) : item.displayName;
            PrepareCounter();
            if (countLabel) countLabel.text = "×" + burst;
            if (counterGlow) counterGlow.text = countLabel ? countLabel.text : "";
            if (keyBadge) keyBadge.enabled = item.isKeyItem;
            counterPulse = 0;
            BeginToast();
            if (audioSource && pickupSound) audioSource.PlayOneShot(pickupSound);
        }

        public void ShowMessage(string message)
        {
            lastItem = null; burst = 0;
            if (icon) icon.enabled = false;
            if (countBadge) countBadge.enabled = false;
            if (keyBadge) keyBadge.enabled = false;
            if (countLabel) countLabel.text = "";
            if (counterGlow) counterGlow.text = "";
            if (nameLabel) nameLabel.text = message;
            BeginToast();
        }
        public void ShowFull()
        {
            lastItem = null;
            burst = 0;
            if (icon) icon.enabled = false;
            if (countBadge) countBadge.enabled = false;
            if (keyBadge) keyBadge.enabled = false;
            if (countLabel) countLabel.text = "";
            if (counterGlow) counterGlow.text = "";
            if (nameLabel) nameLabel.text = "Inventory full";
            BeginToast();
        }
        void PrepareCounter()
        {
            // The old badge field stays serialized for existing scenes.
            if (countBadge) countBadge.enabled = false;
            if (!countLabel || !nameLabel) return;
            var border = counterBorder ? counterBorder : toast ? toast.transform as RectTransform : nameLabel.rectTransform;
            var rt = countLabel.rectTransform;
            rt.SetParent(border, false);
            rt.anchorMin = rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(.5f, .5f);
            rt.localRotation = Quaternion.Euler(0, 0, counterAngle);
            rt.anchoredPosition = counterOffset;
            rt.sizeDelta = counterSize;
            countLabel.gameObject.SetActive(true);
            countLabel.enabled = true;
            countLabel.alignment = TextAlignmentOptions.Center;
            countLabel.margin = Vector4.zero;
            countLabel.maskable = false;
            countLabel.enableAutoSizing = true;
            countLabel.fontSizeMin = 10;
            countLabel.fontSizeMax = counterFontSize;
            countLabel.fontSize = counterFontSize;
            countLabel.raycastTarget = false;

            if (!counterGlow)
            {
                var go = new GameObject("Pickup counter glow", typeof(RectTransform));
                go.transform.SetParent(border, false);
                counterGlow = go.AddComponent<TextMeshProUGUI>();
                counterGlow.font = countLabel.font;
                if (countLabel.fontSharedMaterial)
                {
                    counterMaterial = new Material(countLabel.fontSharedMaterial);
                    // A local material keeps the glow independent from other labels.
                    if (counterMaterial.HasProperty("_OutlineWidth")) counterMaterial.SetFloat("_OutlineWidth", .22f);
                    if (counterMaterial.HasProperty("_OutlineSoftness")) counterMaterial.SetFloat("_OutlineSoftness", .2f);
                    counterGlow.fontSharedMaterial = counterMaterial;
                }
            }
            var gr = counterGlow.rectTransform;
            gr.SetParent(border, false);
            gr.anchorMin = gr.anchorMax = Vector2.one;
            gr.pivot = new Vector2(.5f, .5f);
            gr.localRotation = Quaternion.Euler(0, 0, counterAngle);
            gr.anchoredPosition = counterOffset;
            gr.sizeDelta = counterSize;
            counterGlow.alignment = TextAlignmentOptions.Center;
            counterGlow.margin = Vector4.zero;
            counterGlow.maskable = false;
            counterGlow.enableAutoSizing = true;
            counterGlow.fontSizeMin = 10;
            counterGlow.fontSizeMax = counterFontSize;
            counterGlow.fontSize = counterFontSize;
            counterGlow.raycastTarget = false;
            gr.SetAsLastSibling();
            rt.SetAsLastSibling();
        }

        void BeginToast()
        {
            if (!showing && toast) toast.alpha = 0;
            showing = true;
            fadeOut = false;
            holdRemaining = Mathf.Max(.1f, visibleSeconds);
        }

        void UpdateCounter()
        {
            if (!countLabel || burst < 1) return;
            counterPulse += Time.deltaTime;
            Color color = Color.white;
            if (burst == 2) color = secondColor;
            else if (burst == 3) color = thirdColor;
            else if (burst == 4) color = fourthColor;
            bool rainbow = burst >= 5;
            float hue = Mathf.Repeat(Time.time * .35f, 1);
            countLabel.color = rainbow ? Color.white : color;
            countLabel.enableVertexGradient = rainbow;
            if (rainbow)
                countLabel.colorGradient = new VertexGradient(
                    Color.HSVToRGB(hue, .75f, 1),
                    Color.HSVToRGB(Mathf.Repeat(hue + .25f, 1), .75f, 1),
                    Color.HSVToRGB(Mathf.Repeat(hue + .5f, 1), .75f, 1),
                    Color.HSVToRGB(Mathf.Repeat(hue + .75f, 1), .75f, 1));
            float scale = 1 + .16f * Mathf.Exp(-counterPulse * 8);
            countLabel.rectTransform.localScale = Vector3.one * scale;
            if (counterGlow)
            {
                Color tint = rainbow ? Color.HSVToRGB(hue, .65f, 1) : color;
                tint.a = .4f + .15f * Mathf.Sin(Time.time * 5);
                counterGlow.color = tint;
                counterGlow.rectTransform.localScale = Vector3.one * (scale + .025f);
                if (counterMaterial && counterMaterial.HasProperty("_OutlineColor"))
                    counterMaterial.SetColor("_OutlineColor", tint);
            }
        }

        void OnDestroy()
        {
            if (counterGlow) Destroy(counterGlow.gameObject);
            if (counterMaterial) Destroy(counterMaterial);
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
                float step = Time.deltaTime / Mathf.Max(.01f, fadeSeconds);
                if (!fadeOut)
                {
                    if (toast) toast.alpha = Mathf.MoveTowards(toast.alpha, 1, step);
                    holdRemaining -= Time.deltaTime;
                    if (holdRemaining <= 0) fadeOut = true;
                }
                else
                {
                    if (toast) toast.alpha = Mathf.MoveTowards(toast.alpha, 0, step);
                    if (!toast || toast.alpha <= 0)
                    {
                        showing = false;
                        lastItem = null;
                        burst = 0;
                    }
                }
                UpdateCounter();
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
