using UnityEngine;
using UnityEngine.EventSystems;
namespace FreezerGameplay
{
    public class QuickBarSlotHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public QuickBarController bar;public int index;
        public void OnPointerEnter(PointerEventData e){if(!(e is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended) || extended.pointerType == UnityEngine.InputSystem.UI.UIPointerType.MouseOrPen)bar.Hover(index,true);}
        public void OnPointerExit(PointerEventData e){bar.Hover(index,false);}
    }
}
