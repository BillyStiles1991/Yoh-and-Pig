using UnityEngine;
using UnityEngine.UI;

namespace FreezerGameplay
{
    public enum ItemRarity { Common, Uncommon, Rare, SuperRare, HyperRare, KeyItem }

    // Applies a silhouette outline to the source Image without altering its sprite or row focus.
    public class ItemRarityIcon : MonoBehaviour
    {
        Outline edge;
        bool metallic;

        public static void Decorate(Image icon, ItemDefinition item)
        {
            var style = icon.GetComponent<ItemRarityIcon>();
            if (!style) style = icon.gameObject.AddComponent<ItemRarityIcon>();
            style.Apply(item);
        }

        void Apply(ItemDefinition item)
        {
            if (!edge) edge = GetComponent<Outline>();
            if (!edge) edge = gameObject.AddComponent<Outline>();
            edge.effectDistance = new Vector2(2, -2);
            metallic = item.rarity == ItemRarity.HyperRare;
            switch (item.rarity)
            {
                case ItemRarity.Uncommon: edge.effectColor = new Color(.27f,.85f,.36f); break;
                case ItemRarity.Rare: edge.effectColor = new Color(.26f,.6f,1); break;
                case ItemRarity.SuperRare:
                case ItemRarity.KeyItem: edge.effectColor = new Color(1,.77f,.2f); break;
                case ItemRarity.HyperRare: edge.effectColor = Color.white; break;
                default: edge.effectColor = new Color(.85f,.85f,.85f); break;
            }
            enabled = metallic;
        }

        void Update()
        {
            float glint = Mathf.Pow(Mathf.PingPong(Time.unscaledTime * 1.4f, 1), 3);
            edge.effectColor = Color.Lerp(new Color(.4f,.53f,.69f), Color.white, glint);
        }
    }
}
