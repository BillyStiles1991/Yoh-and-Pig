using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FreezerGameplay
{
    public class ActivityListFocus:MonoBehaviour,ISelectHandler
    {
        public ScrollRect scroll;
        public void OnSelect(BaseEventData data)
        {
            if(!scroll)return;Canvas.ForceUpdateCanvases();var b=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,transform);
            float delta=b.min.y<scroll.viewport.rect.yMin?scroll.viewport.rect.yMin-b.min.y:b.max.y>scroll.viewport.rect.yMax?scroll.viewport.rect.yMax-b.max.y:0;
            scroll.content.anchoredPosition+=Vector2.up*delta;
        }
    }
}
