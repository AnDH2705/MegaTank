using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MegaTanks
{
    public sealed class HtmlChoiceCardMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        RectTransform rect;Vector2 home;CanvasGroup group;Button button;Outline focus;float age,lift;bool hover,pressed;public float opacity=1;
        public void Initialize(Button target,Image background)
        {rect=(RectTransform)transform;home=rect.anchoredPosition;button=target;group=gameObject.AddComponent<CanvasGroup>();focus=background.gameObject.AddComponent<Outline>();focus.effectColor=new Color(1,.91f,.36f);focus.effectDistance=new Vector2(4,-4);focus.enabled=false;}
        void Update()
        {
            if(rect==null)return;age+=Time.unscaledDeltaTime;float t=1-Mathf.Pow(1-Mathf.Clamp01(age/.24f),3);lift=Mathf.MoveTowards(lift,button.interactable&&hover?7:0,Time.unscaledDeltaTime*70);
            rect.anchoredPosition=home+new Vector2(76*(1-t),lift);rect.localScale=Vector3.one*(pressed&&button.interactable?.985f:1);group.alpha=t*opacity;
        }
        public void OnPointerEnter(PointerEventData data){hover=true;}public void OnPointerExit(PointerEventData data){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData data){pressed=true;}public void OnPointerUp(PointerEventData data){pressed=false;}
        public void OnSelect(BaseEventData data){if(focus!=null)focus.enabled=button.interactable;}public void OnDeselect(BaseEventData data){if(focus!=null)focus.enabled=false;}
    }
}
