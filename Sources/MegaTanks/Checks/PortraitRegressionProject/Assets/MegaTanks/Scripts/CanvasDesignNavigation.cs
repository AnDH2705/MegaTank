using System.Collections;
using UnityEngine;

namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        bool designNavigating,designOpening;
        static bool FullScreenPopup(string name)=>name=="Crew"||name=="Equipment"||name=="Tech";
        static CanvasGroup NavigationGroup(GameObject view){var group=view.GetComponent<CanvasGroup>();if(group==null)group=view.AddComponent<CanvasGroup>();return group;}
        void ResetMainNavigation()
        {var group=NavigationGroup(View(UIScreen.Home).gameObject);group.alpha=1;group.blocksRaycasts=group.interactable=true;}
        IEnumerator DesignTransition(GameObject previous,GameObject next,bool hidePrevious,bool destroyPrevious=false)
        {
            designNavigating=true;var outgoing=NavigationGroup(previous);var incoming=NavigationGroup(next);var rect=(RectTransform)next.transform;Vector2 home=rect.anchoredPosition;
            next.SetActive(true);previous.SetActive(true);outgoing.blocksRaycasts=outgoing.interactable=false;incoming.blocksRaycasts=incoming.interactable=false;
            float time=0;while(time<.36f)
            {
                time+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(time/.36f),ease=1-Mathf.Pow(1-t,3);outgoing.alpha=1-Mathf.Clamp01(time/.2f);incoming.alpha=ease;rect.anchoredPosition=home+Vector2.right*(115.2f*(1-ease));yield return null;
            }
            rect.anchoredPosition=home;incoming.alpha=1;incoming.blocksRaycasts=incoming.interactable=true;
            if(destroyPrevious)Destroy(previous);else if(hidePrevious){previous.SetActive(false);outgoing.alpha=1;}
            designNavigating=false;
        }
        public void ReturnFromHtmlBattle(GameObject battle)
        {if(designNavigating)return;StartCoroutine(DesignTransition(battle,View(UIScreen.Home).gameObject,false,true));}
    }
}
