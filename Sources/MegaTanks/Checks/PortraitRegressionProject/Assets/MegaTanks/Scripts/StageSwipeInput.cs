using UnityEngine;
using UnityEngine.EventSystems;
namespace MegaTanks
{
    public sealed class StageSwipeInput : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public CanvasGameUI target;
        Vector2 start;
        public void OnBeginDrag(PointerEventData data){start=data.position;}
        public void OnDrag(PointerEventData data){}
        public void OnEndDrag(PointerEventData data){var d=data.position-start;if(Mathf.Abs(d.x)>60&&Mathf.Abs(d.x)>Mathf.Abs(d.y))target.ChangeStage(d.x<0?1:-1);}
    }
}
