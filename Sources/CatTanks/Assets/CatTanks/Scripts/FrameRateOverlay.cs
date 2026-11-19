using UnityEngine;
using UnityEngine.UI;

namespace CatTanks
{
    // Independent Canvas: refreshing the counter never rebuilds the combat HUD.
    public sealed class FrameRateOverlay:MonoBehaviour
    {
        Text label;
        float elapsed,worst,recentWorst,nextWindow;
        int frames;
        bool visible=true;
        public Text Label=>label;
        void Awake()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var panel=new GameObject("FPS Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.sizeDelta=new Vector2(310,72);rect.anchoredPosition=new Vector2(-24,-208);
            var image=panel.GetComponent<Image>();image.color=new Color(.01f,.025f,.05f,.82f);image.raycastTarget=false;
            label=new GameObject("FPS",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(panel.transform,false);
            var textRect=label.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(10,5);textRect.offsetMax=new Vector2(-10,-5);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=19;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;label.supportRichText=false;
            label.text="FPS …\nF3: Ẩn/hiện";nextWindow=Time.unscaledTime+2;
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F3)){visible=!visible;label.transform.parent.gameObject.SetActive(visible);}
            float dt=Time.unscaledDeltaTime;if(dt<=0)return;
            elapsed+=dt;frames++;worst=Mathf.Max(worst,dt*1000);
            if(Time.unscaledTime>=nextWindow){recentWorst=worst;worst=0;nextWindow=Time.unscaledTime+2;}
            if(elapsed<.25f)return;
            float fps=frames/elapsed,peak=Mathf.Max(worst,recentWorst);
            label.text=$"FPS {fps:0}  |  {elapsed/frames*1000:0.0} ms\nKhung chậm: {peak:0.0} ms  |  F3";
            label.color=peak>33.3f?new Color(1,.5f,.35f):fps<100?new Color(1,.85f,.3f):new Color(.55f,1,.65f);
            elapsed=0;frames=0;
        }
    }
}
