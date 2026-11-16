using UnityEngine;
using UnityEngine.UI;

namespace FreezerGameplay
{
    public enum ItemRarity { Common, Uncommon, Rare, SuperRare, HyperRare, KeyItem }

    // A solid UI frame keeps rarity readable even on padded or dark sprites.
    public class ItemRarityIcon : MonoBehaviour
    {
        [Min(1)] public float frameThickness = 3;
        [Min(0)] public float framePadding = 4;
        public bool showFrame = true;
        Outline edge;
        Image source;
        RectTransform frame;
        Image[] sides;
        Color rarityColor;
        bool metallic;

        public static void Decorate(Image icon, ItemDefinition item)
        {
            if (!icon || item == null) return;
            var style = icon.GetComponent<ItemRarityIcon>();
            if (!style) style = icon.gameObject.AddComponent<ItemRarityIcon>();
            style.Apply(item);
        }

        void Apply(ItemDefinition item)
        {
            source = GetComponent<Image>();
            if (!edge) edge = GetComponent<Outline>();
            if (!edge) edge = gameObject.AddComponent<Outline>();
            edge.enabled = true;
            edge.useGraphicAlpha = true;
            edge.effectDistance = new Vector2(2, -2);
            metallic = item.rarity == ItemRarity.HyperRare;
            switch (item.rarity)
            {
                case ItemRarity.Uncommon: rarityColor = new Color(.27f,.85f,.36f); break;
                case ItemRarity.Rare: rarityColor = new Color(.26f,.6f,1); break;
                case ItemRarity.SuperRare:
                case ItemRarity.KeyItem: rarityColor = new Color(1,.77f,.2f); break;
                case ItemRarity.HyperRare: rarityColor = Color.white; break;
                default: rarityColor = new Color(.85f,.85f,.85f); break;
            }
            EnsureFrame();
            enabled = true;
            UpdateAppearance();
        }

        void EnsureFrame()
        {
            if (frame) return;
            var go = new GameObject("Rarity frame", typeof(RectTransform), typeof(LayoutElement));
            frame = (RectTransform)go.transform;
            frame.SetParent(transform, false);
            frame.SetAsFirstSibling();
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            sides = new Image[4];
            string[] names = { "Top", "Bottom", "Left", "Right" };
            for (int i = 0; i < 4; i++)
            {
                var side = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                side.transform.SetParent(frame, false);
                sides[i] = side.GetComponent<Image>();
                sides[i].raycastTarget = false;
                // No sprite or custom material: an opaque UI quad shows the exact rarity colour.
            }
        }

        void LateUpdate() { UpdateAppearance(); }

        void UpdateAppearance()
        {
            if (!source || !frame) return;
            bool visible = showFrame && source.enabled && source.sprite != null;
            frame.gameObject.SetActive(visible);
            Color color = rarityColor;
            if (metallic)
            {
                float glint = Mathf.Pow(Mathf.PingPong(Time.unscaledTime * 1.4f, 1), 3);
                color = Color.Lerp(new Color(.4f,.53f,.69f), Color.white, glint);
            }
            if (edge) edge.effectColor = color;
            if (!visible) return;
            frame.offsetMin = Vector2.one * -framePadding;
            frame.offsetMax = Vector2.one * framePadding;
            for (int i = 0; i < 4; i++)
            {
                sides[i].color = color;
                RectTransform rt = sides[i].rectTransform;
                bool horizontal = i < 2;
                rt.anchorMin = horizontal ? new Vector2(0, i == 0 ? 1 : 0) : new Vector2(i == 2 ? 0 : 1, 0);
                rt.anchorMax = horizontal ? new Vector2(1, i == 0 ? 1 : 0) : new Vector2(i == 2 ? 0 : 1, 1);
                rt.pivot = horizontal ? new Vector2(.5f, i == 0 ? 1 : 0) : new Vector2(i == 2 ? 0 : 1, .5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = horizontal ? new Vector2(0, frameThickness) : new Vector2(frameThickness, 0);
            }
        }

        void OnDisable() { if (frame) frame.gameObject.SetActive(false); }
        void OnDestroy() { if (frame) Destroy(frame.gameObject); }
    }
}
