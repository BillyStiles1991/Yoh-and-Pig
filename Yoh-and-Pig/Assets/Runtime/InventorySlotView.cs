using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FreezerGameplay
{
    public class InventorySlotView : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action focused; public TMP_Text newLabel; public string key;
        public void OnSelect(BaseEventData data) { focused?.Invoke(); }
        public void OnPointerEnter(PointerEventData data) { focused?.Invoke(); }
        Material glowMaterial;
        void Update()
        {
            if (!newLabel || !newLabel.gameObject.activeSelf) return;
            if (!glowMaterial && newLabel.fontSharedMaterial)
            {
                glowMaterial = new Material(newLabel.fontSharedMaterial);
                if (glowMaterial.HasProperty("_OutlineWidth")) glowMaterial.SetFloat("_OutlineWidth", .2f);
                if (glowMaterial.HasProperty("_OutlineSoftness")) glowMaterial.SetFloat("_OutlineSoftness", .2f);
                newLabel.fontSharedMaterial = glowMaterial;
            }
            Color color = Color.Lerp(new Color(1,.65f,.1f), Color.white, .5f + .5f * Mathf.Sin(Time.unscaledTime * 4));
            newLabel.color = color;
            if (glowMaterial && glowMaterial.HasProperty("_OutlineColor")) glowMaterial.SetColor("_OutlineColor", color);
        }
        void OnDestroy() { if (glowMaterial) Destroy(glowMaterial); }
    }
}
