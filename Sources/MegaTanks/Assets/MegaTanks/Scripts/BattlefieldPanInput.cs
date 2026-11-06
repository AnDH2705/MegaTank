using UnityEngine;
using UnityEngine.EventSystems;
namespace MegaTanks
{
    // A transparent Canvas surface beneath the HUD handles both touch and mouse drags.
    public sealed class BattlefieldPanInput : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public BattleCameraPan cameraPan;
        bool dragging;
        int pointer;
        public void OnBeginDrag(PointerEventData data)
        {
            if(dragging||cameraPan==null||!cameraPan.CanPan||data.button!=PointerEventData.InputButton.Left)return;
            dragging=true;pointer=data.pointerId;
        }
        public void OnDrag(PointerEventData data)
        {
            if(!dragging||data.pointerId!=pointer)return;
            if(!cameraPan.CanPan){dragging=false;return;}
            cameraPan.PanByPixels(data.delta.x);
        }
        public void OnEndDrag(PointerEventData data){if(data.pointerId==pointer)dragging=false;}
        void Update(){if(dragging&&(cameraPan==null||!cameraPan.CanPan))dragging=false;}
        void OnDisable(){dragging=false;}
    }
}
