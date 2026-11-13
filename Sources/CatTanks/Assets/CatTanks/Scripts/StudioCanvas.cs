using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        sealed class StudioTextStyle
        {
            public int fontSize; public FontStyle fontStyle; public Color color;
            public StudioTextStyle(int size,Color tint,FontStyle weight=FontStyle.Normal){fontSize=size;color=tint;fontStyle=weight;}
        }
        sealed class Widget
        {
            public StudioWidgetBinding binding; public RectTransform rect; public Image image;
            public Text text; public UnityEngine.UI.Button button; public ScrollRect scroll;
            public RectTransform content; public InputField input;
        }
        readonly Dictionary<string,Widget> uiWidgets=new Dictionary<string,Widget>();
        readonly HashSet<string> usedWidgets=new HashSet<string>();
        readonly HashSet<string> usedViews=new HashSet<string>();
        readonly List<UnityEngine.UI.Button> focusable=new List<UnityEngine.UI.Button>();
        StudioUIRoot studioRoot;
        Canvas studioCanvas;
        RectTransform uiRoot,uiParent;
        StudioUITheme theme;
        Widget lastButton;
        string pendingButton,drawnContext="",renderViewKey="",uiContainer="",scrollContext="";
        bool uiInputAllowed=true,renderingUi,dispatchOnly,authoringUi,reduceMotion;
        float nextUiRefresh;
        int lastScreenWidth,lastScreenHeight;
        Rect lastSafeArea;
        Color textTint=Color.white;
        StudioTextStyle hudText,hudMeta;
        public bool UiReady=>studioCanvas&&theme&&!renderingUi;
        public StudioUIRoot CanvasRoot=>studioRoot;
        string UiContext=>page+"/"+tab+"/"+modal+"/"+inBattle+"/"+paused+"/"+resolved;
        static Color C(string hex)=>Catalog.Color(hex);
        void EnsureCanvas()
        {
            if(studioCanvas)return;
            theme=Resources.Load<StudioUITheme>("StudioUITheme");Styles();
            if(!authoringUi)studioRoot=FindObjectOfType<StudioUIRoot>();
            if(!studioRoot&&!authoringUi)
            {
                var prefab=Resources.Load<StudioUIRoot>("UI/CatTanksUI");
                if(prefab)studioRoot=Instantiate(prefab);
            }
#if UNITY_EDITOR
            if(!studioRoot){authoringUi=true;CreateAuthoringCanvas();}
#endif
            if(!studioRoot)throw new InvalidOperationException("CatTanks UI prefab is missing. Build the UI prefabs from CatTanks > UI.");
            studioCanvas=studioRoot.GetComponent<Canvas>();uiRoot=studioRoot.viewport;
            foreach(var view in studioRoot.GetComponentsInChildren<StudioUIView>(true))
                foreach(var binding in view.GetComponentsInChildren<StudioWidgetBinding>(true))
                    uiWidgets[view.key+"/"+binding.id]=Bind(binding);
            if(!FindObjectOfType<EventSystem>())new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            reduceMotion=PlayerPrefs.GetInt("CatTanks.ReducedMotion",0)!=0;
        }
#if UNITY_EDITOR
        public void BeginUiAuthoring()
        {
            if(studioRoot)studioRoot.gameObject.SetActive(false);
            studioRoot=null;studioCanvas=null;uiWidgets.Clear();authoringUi=true;EnsureCanvas();
        }
        void CreateAuthoringCanvas()
        {
            var go=new GameObject("CatTanks Studio UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(StudioUIRoot));
            studioRoot=go.GetComponent<StudioUIRoot>();go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=100;
            go.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            studioRoot.viewport=MakeRect("Safe Area • 1600 × 900",go.transform);
            studioRoot.screens=MakeRect("Screens",studioRoot.viewport);studioRoot.popups=MakeRect("Popups",studioRoot.viewport);studioRoot.notifications=MakeRect("Notifications",studioRoot.viewport);
            foreach(var layer in new[]{studioRoot.screens,studioRoot.popups,studioRoot.notifications})Place(layer,0,0,1600,900);
        }
        static RectTransform MakeRect(string name,Transform parent)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);return rt;
        }
#endif
        void LateUpdate()
        {
            EnsureCanvas();
            if(Time.unscaledTime>=nextUiRefresh||drawnContext!=UiContext||Screen.width!=lastScreenWidth||Screen.height!=lastScreenHeight||Screen.safeArea!=lastSafeArea)RefreshUi();
            if(Input.GetKeyDown(KeyCode.Tab))MoveFocus(Input.GetKey(KeyCode.LeftShift)?-1:1);
        }
        void MoveFocus(int direction)
        {
            var choices=focusable.FindAll(b=>b&&b.IsActive()&&b.interactable);if(choices.Count==0||!EventSystem.current)return;
            int at=choices.FindIndex(b=>b.gameObject==EventSystem.current.currentSelectedGameObject);
            choices[at<0?(direction>0?0:choices.Count-1):(at+direction+choices.Count)%choices.Count].Select();
        }
        public void RefreshCanvas()=>RefreshUi();
        void RefreshUi()
        {
            if(renderingUi||db==null)return;EnsureCanvas();renderingUi=true;
            try
            {
                if(pendingButton!=null){dispatchOnly=true;RenderPass();dispatchOnly=false;pendingButton=null;}
                nextUiRefresh=Time.unscaledTime+(inBattle?.05f:.2f);lastScreenWidth=Screen.width;lastScreenHeight=Screen.height;lastSafeArea=Screen.safeArea;
                var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1600f,safe.height/900f);
                uiRoot.anchorMin=uiRoot.anchorMax=uiRoot.pivot=new Vector2(.5f,.5f);uiRoot.sizeDelta=new Vector2(1600,900);uiRoot.localScale=Vector3.one*scale;
                uiRoot.anchoredPosition=safe.center-new Vector2(Screen.width,Screen.height)/2;
                usedWidgets.Clear();usedViews.Clear();focusable.Clear();drawnContext=UiContext;RenderPass();
                foreach(var pair in uiWidgets)if(pair.Value.rect)pair.Value.rect.gameObject.SetActive(usedWidgets.Contains(pair.Key));
                foreach(var view in studioRoot.GetComponentsInChildren<StudioUIView>(true))view.gameObject.SetActive(usedViews.Contains(view.key));
            }
            finally{dispatchOnly=false;renderingUi=false;}
        }
        void RenderPass()
        {
            string key=inBattle?"battle":page=="barracks"?"barracks-"+NormalizeBarracksTab():page=="town"?"town-"+NormalizeTownTab():page;
            SelectView(key,"screen");uiInputAllowed=modal==""&&(!inBattle||(!paused&&!resolved));
            if(inBattle)BattleHUD();else HubUI();
            if(modal!="")
            {
                SelectView(modal,"popup");uiInputAllowed=true;Scrim();
                if(modal=="book")Book();else if(modal=="settings")Settings();else if(modal=="prebattle")Prebattle();else if(modal=="choice")ChoiceModal();
            }
            else if(inBattle&&resolved){SelectView("result","popup");uiInputAllowed=true;Scrim();Result();}
            else if(inBattle&&paused){SelectView("pause","popup");uiInputAllowed=true;Scrim();PausePopup();}
            if(Time.unscaledTime<toastUntil)
            {
                SelectView("toast","notification");Surface(438,806,870,64,C("#183e53"));Icon(454,820,32,"Check");Text(500,822,780,toast,label,40);
            }
        }
        string NormalizeBarracksTab()
        {
            if(Array.IndexOf(new[]{"cats","fortress","squad","skills","monsters","gear","presets","nexus"},tab)<0)tab="cats";return tab;
        }
        string NormalizeTownTab(){if(tab!="summon"&&tab!="shop"&&tab!="arena")tab="summon";return tab;}
        void SelectView(string key,string layer)
        {
            renderViewKey=key;uiContainer="";if(dispatchOnly)return;
            var view=studioRoot.Find(key);
#if UNITY_EDITOR
            if(!view&&authoringUi)
            {
                var parent=layer=="screen"?studioRoot.screens:layer=="popup"?studioRoot.popups:studioRoot.notifications;
                var rt=MakeRect((layer=="popup"?"Popup_":"Screen_")+key,parent);Place(rt,0,0,1600,900);
                view=rt.gameObject.AddComponent<StudioUIView>();view.key=key;view.layer=layer;
            }
#endif
            if(!view)throw new InvalidOperationException("Missing UI view prefab: "+key);
            view.gameObject.SetActive(true);uiParent=(RectTransform)view.transform;usedViews.Add(key);
        }
        string WidgetId(string kind,float x,float y)=>kind+":"+uiContainer+":"+x.ToString("0.###",CultureInfo.InvariantCulture)+":"+y.ToString("0.###",CultureInfo.InvariantCulture);
        Widget GetWidget(string kind,float x,float y,float w,float h,bool dynamicLayout=false)
        {
            string id=WidgetId(kind,x,y),key=renderViewKey+"/"+id;usedWidgets.Add(key);
            if(!uiWidgets.TryGetValue(key,out var item))
            {
                var prefab=studioRoot.Component(kind);
                if(prefab&&!authoringUi)
                {
                    var go=Instantiate(prefab,uiParent,false);var binding=go.GetComponent<StudioWidgetBinding>();binding.id=id;binding.authored=false;item=Bind(binding);
                }
#if UNITY_EDITOR
                else if(authoringUi)item=CreateWidget(kind,id);
#endif
                else throw new InvalidOperationException("Missing UI component prefab: "+kind);
                uiWidgets[key]=item;
            }
            item.rect.gameObject.SetActive(true);
            if(item.rect.parent!=uiParent)item.rect.SetParent(uiParent,false);
            if(authoringUi||!item.binding.authored||dynamicLayout)Place(item.rect,x,y,w,h);
            item.rect.SetAsLastSibling();return item;
        }
        Widget Bind(StudioWidgetBinding binding)
        {
            var go=binding.gameObject;var item=new Widget{binding=binding,rect=(RectTransform)binding.transform,image=go.GetComponent<Image>(),button=go.GetComponent<UnityEngine.UI.Button>(),scroll=go.GetComponent<ScrollRect>(),input=go.GetComponent<InputField>()};
            item.text=binding.kind=="text"?go.GetComponent<Text>():go.GetComponentInChildren<Text>(true);
            if(item.scroll)item.content=item.scroll.content;
            if(item.input){item.input.onEndEdit.RemoveAllListeners();item.input.onEndEdit.AddListener(value=>{profile.name=string.IsNullOrWhiteSpace(value)?"Chỉ huy mèo":value.Trim();profile.Save();});}
            return item;
        }
#if UNITY_EDITOR
        Widget CreateWidget(string kind,string id)
        {
            var go=new GameObject(kind,typeof(RectTransform),typeof(StudioWidgetBinding));go.transform.SetParent(uiParent,false);
            var binding=go.GetComponent<StudioWidgetBinding>();binding.id=id;binding.kind=kind;binding.authored=false;
            if(kind=="text")go.AddComponent<Text>();
            else
            {
                var image=go.AddComponent<Image>();image.raycastTarget=kind=="button"||kind=="scrim"||kind=="input"||kind=="scroll";
                if(kind=="scroll")
                {
                    image.color=new Color(0,0,0,.01f);go.AddComponent<RectMask2D>();var sc=go.AddComponent<ScrollRect>();sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=45;
                    sc.viewport=(RectTransform)go.transform;sc.content=MakeRect("Content",go.transform);
                    var track=MakeRect("Scrollbar",go.transform);track.anchorMin=new Vector2(1,0);track.anchorMax=Vector2.one;track.pivot=new Vector2(1,.5f);track.sizeDelta=new Vector2(7,-12);track.anchoredPosition=new Vector2(-3,0);
                    track.gameObject.AddComponent<Image>().color=C("#111a2e");var sb=track.gameObject.AddComponent<Scrollbar>();var handle=MakeRect("Handle",track);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
                    var hi=handle.gameObject.AddComponent<Image>();hi.color=C("#91b7e0");sb.handleRect=handle;sb.targetGraphic=hi;sb.direction=Scrollbar.Direction.BottomToTop;sc.verticalScrollbar=sb;sc.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
                }
                if(kind=="button"||kind=="input")
                {
                    var rt=MakeRect(kind=="input"?"Value":"Label",go.transform);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(12,5);rt.offsetMax=new Vector2(-12,-5);var text=rt.gameObject.AddComponent<Text>();
                    if(kind=="button"){var b=go.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;}
                    else{var input=go.AddComponent<InputField>();input.textComponent=text;input.targetGraphic=image;input.characterLimit=24;}
                }
            }
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.raycastTarget=false;text.font=font;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
            return Bind(binding);
        }
#endif
        static void Place(RectTransform rt,float x,float y,float w,float h)
        {
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);rt.localScale=Vector3.one;
        }
        bool Configure(Widget item)=>authoringUi||!item.binding.authored;
        void SpriteBox(float x,float y,float w,float h,string key,Color color,bool preserve=false,string kind="image")
        {
            if(dispatchOnly)return;var item=GetWidget(kind,x,y,w,h,key=="fill");
            if(Configure(item)||key=="fill")
            {
                StudioNineSlice.Apply(item.image,theme?theme.Get(key):null,key=="bar"||key=="fill"?5:key=="panel"?16:14,preserve);item.image.color=color;
            }
            else if(theme&&item.image.sprite!=theme.Get(key))StudioNineSlice.Apply(item.image,theme.Get(key),key=="bar"?5:key=="panel"?16:14,preserve);
        }
        void Surface(float x,float y,float w,float h,Color color)=>SpriteBox(x,y,w,h,"panel",color);
        void Icon(float x,float y,float size,string key)=>SpriteBox(x,y,size,size,key,Color.white,true);
        void Text(float x,float y,float w,string value,StudioTextStyle style=null,float h=35)
        {
            if(dispatchOnly)return;var s=style??label;var item=GetWidget("text",x,y,w,Mathf.Max(h,s.fontSize*1.45f));item.text.text=value;
            if(Configure(item)){item.rect.name="Text • "+value.Replace('\n',' ');item.text.fontSize=s.fontSize;item.text.fontStyle=s.fontStyle;item.text.color=s.color*textTint;item.text.alignment=TextAnchor.UpperLeft;item.text.resizeTextForBestFit=false;}
        }
        bool Button(float x,float y,float w,float h,string value,bool enabled=true,bool primary=false)
        {
            string id=renderViewKey+"/"+WidgetId("button",x,y);
            if(dispatchOnly){if(pendingButton!=id||!enabled||!uiInputAllowed)return false;pendingButton=null;return true;}
            var item=GetWidget("button",x,y,w,h);lastButton=item;
            if(Configure(item))
            {
                item.rect.name="Button • "+value.Replace('\n',' ');item.text.fontSize=h<55?17:19;item.text.fontStyle=FontStyle.Bold;item.text.alignment=TextAnchor.MiddleCenter;
                var tr=(RectTransform)item.text.transform;tr.offsetMin=new Vector2(12,5);tr.offsetMax=new Vector2(-12,-5);
            }
            SetButtonSprite(item,primary?"primary":"button");item.text.text=value;item.text.color=!enabled?C("#c6d2e8"):primary?C("#44300e"):C("#f5f6ff");item.button.interactable=enabled&&uiInputAllowed;
            var colors=item.button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.75f,.8f,.9f);colors.selectedColor=C("#9ae6ff");colors.disabledColor=new Color(.5f,.54f,.65f,.75f);colors.fadeDuration=reduceMotion?0:.08f;item.button.colors=colors;
            string context=UiContext;item.button.onClick.RemoveAllListeners();item.button.onClick.AddListener(()=>{if(context!=UiContext)return;pendingButton=id;RefreshUi();});focusable.Add(item.button);return false;
        }
        void SetButtonSprite(Widget item,string key){if(item!=null)StudioNineSlice.Apply(item.image,theme?theme.Get(key):null,14);}
        void Panel(float x,float y,float w,float h,float alpha=1)=>Surface(x,y,w,h,new Color(.13f,.18f,.30f,alpha));
        void Art(float x,float y,float w,float h,string key)
        {
            if(dispatchOnly)return;var item=GetWidget("image",x,y,w,h);var sprite=Resources.Load<Sprite>("Art/"+key);StudioNineSlice.Apply(item.image,sprite?sprite:view.Art(key),14,true);item.image.color=Color.white;
        }
        void Bar(float x,float y,float w,float h,float ratio,Color color)
        {
            SpriteBox(x,y,w,h,"bar",Color.white);if(ratio>0)SpriteBox(x+3,y+3,Mathf.Max(1,(w-6)*Mathf.Clamp01(ratio)),h-6,"fill",color);
        }
        Vector2 BeginScroll(Rect viewport,Vector2 value,Rect content)
        {
            if(dispatchOnly){uiContainer=WidgetId("scroll",viewport.x,viewport.y);return value;}
            var item=GetWidget("scroll",viewport.x,viewport.y,viewport.width,viewport.height);
            Place(item.content,0,-item.content.anchoredPosition.y,content.width,Mathf.Max(viewport.height,content.height));item.scroll.vertical=content.height>viewport.height;item.scroll.enabled=uiInputAllowed;
            if(scrollContext!=renderViewKey+modal+bookTab){item.content.anchoredPosition=Vector2.zero;scrollContext=renderViewKey+modal+bookTab;}
            uiContainer=item.binding.id;uiParent=item.content;return value;
        }
        void EndScroll(){uiContainer="";if(!dispatchOnly)uiParent=(RectTransform)studioRoot.Find(renderViewKey).transform;}
        void Scrim()=>SpriteBox(-500,-500,2600,1900,"",new Color(.025f,.035f,.075f,.82f),false,"scrim");
        void NameInput(float x,float y,float w,float h)
        {
            if(dispatchOnly)return;var item=GetWidget("input",x,y,w,h);
            if(Configure(item)){StudioNineSlice.Apply(item.image,theme?theme.Get("input"):null,12);item.image.color=C("#d9e9ff");item.text.fontSize=22;item.text.color=C("#16213b");item.text.alignment=TextAnchor.MiddleLeft;}
            if(!item.input.isFocused)item.input.SetTextWithoutNotify(profile.name);
        }
        void Styles()
        {
            if(label!=null)return;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label=new StudioTextStyle(20,C("#e4eafa"));small=new StudioTextStyle(17,C("#aabbd6"));title=new StudioTextStyle(34,C("#fff0bc"),FontStyle.Bold);subtitle=new StudioTextStyle(23,C("#e4eafa"),FontStyle.Bold);hudText=new StudioTextStyle(16,Color.white,FontStyle.Bold);hudMeta=new StudioTextStyle(15,C("#e8f4ff"));
        }
    }
}
