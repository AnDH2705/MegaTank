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
            public StudioButtonLayout layout;public bool clickBound;public string context;
        }
        readonly Dictionary<string,Widget> uiWidgets=new Dictionary<string,Widget>();
        readonly Dictionary<string,Sprite> uiArtSprites=new Dictionary<string,Sprite>();
        struct WidgetAddress:IEquatable<WidgetAddress>
        {
            public string view,kind,container;public float x,y;
            public bool Equals(WidgetAddress other)=>view==other.view&&kind==other.kind&&container==other.container&&x==other.x&&y==other.y;
            public override bool Equals(object value)=>value is WidgetAddress other&&Equals(other);
            public override int GetHashCode(){unchecked{return ((((view.GetHashCode()*397)^kind.GetHashCode())*397^container.GetHashCode())*397^x.GetHashCode())*397^y.GetHashCode();}}
        }
        struct WidgetIdentity{public string id,key;}
        readonly Dictionary<WidgetAddress,WidgetIdentity> widgetIdentities=new Dictionary<WidgetAddress,WidgetIdentity>();
        readonly HashSet<string> usedWidgets=new HashSet<string>();
        readonly HashSet<string> usedViews=new HashSet<string>();
        readonly List<UnityEngine.UI.Button> focusable=new List<UnityEngine.UI.Button>();
        readonly List<StudioButtonLayout> activeLayouts=new List<StudioButtonLayout>();
        readonly Dictionary<Transform,int> siblingCursors=new Dictionary<Transform,int>();
        StudioUIRoot studioRoot;
        Canvas studioCanvas;
        RectTransform uiRoot,uiParent;
        RectTransform floatingLayer;
        StudioUITheme theme;
        Widget lastButton;
        string pendingButton,drawnContext="",renderViewKey="",uiContainer="",scrollContext="";
        bool uiInputAllowed=true,renderingUi,dispatchOnly,authoringUi,reduceMotion,useCodeGeometry,drawingHubChrome,popupLayer;
        float nextUiRefresh;
        int lastScreenWidth,lastScreenHeight;
        Rect lastSafeArea;
        Color textTint=Color.white;
        StudioTextStyle hudText,hudMeta;
        Font strongFont;
        public bool UiReady=>studioCanvas&&theme&&!renderingUi;
        public StudioUIRoot CanvasRoot=>studioRoot;
        string contextPage,contextTab,contextModal,cachedContext;
        bool contextBattle,contextPaused,contextResolved;
        string UiContext
        {
            get
            {
                if(cachedContext==null||contextPage!=page||contextTab!=tab||contextModal!=modal||contextBattle!=inBattle||contextPaused!=paused||contextResolved!=resolved)
                {
                    contextPage=page;contextTab=tab;contextModal=modal;contextBattle=inBattle;contextPaused=paused;contextResolved=resolved;
                    cachedContext=page+"/"+tab+"/"+modal+"/"+inBattle+"/"+paused+"/"+resolved;
                }
                return cachedContext;
            }
        }
        static Color C(string hex)=>Catalog.Color(hex);
        void EnsureCanvas()
        {
            if(studioCanvas)return;
            theme=Resources.Load<StudioUITheme>("StudioUITheme");Styles();
            if(!authoringUi)studioRoot=FindObjectOfType<StudioUIRoot>();
            if(!studioRoot&&!authoringUi)
            {
                var prefab=Resources.Load<StudioUIRoot>("UI/Mockup/CatTanksUI");
                if(!prefab)prefab=Resources.Load<StudioUIRoot>("UI/CatTanksUI");
                if(prefab)studioRoot=Instantiate(prefab);
            }
#if UNITY_EDITOR
            if(!studioRoot){authoringUi=true;CreateAuthoringCanvas();}
#endif
            if(!studioRoot)throw new InvalidOperationException("CatTanks UI prefab is missing. Build the UI prefabs from CatTanks > UI.");
            if(studioRoot.theme)theme=studioRoot.theme;
            if(studioRoot.mockupKit){title.color=C("#ffcc32");subtitle.color=label.color=small.color=Color.white;}
            studioCanvas=studioRoot.GetComponent<Canvas>();uiRoot=studioRoot.viewport;
            foreach(var text in studioRoot.GetComponentsInChildren<Text>(true))
                ApplyFont(text,text.fontStyle);
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
            studioRoot=null;studioCanvas=null;uiWidgets.Clear();widgetIdentities.Clear();lastScreenWidth=lastScreenHeight=0;authoringUi=true;EnsureCanvas();
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
            using(new GamePerformance.Scope(GamePerformance.Area.UI))RefreshUiCore();
        }
        void RefreshUiCore()
        {
            if(renderingUi||db==null)return;EnsureCanvas();renderingUi=true;
            try
            {
                if(pendingButton!=null){dispatchOnly=true;RenderPass();dispatchOnly=false;pendingButton=null;}
                bool resized=Screen.width!=lastScreenWidth||Screen.height!=lastScreenHeight||Screen.safeArea!=lastSafeArea;
                nextUiRefresh=Time.unscaledTime+(inBattle?.05f:.2f);lastScreenWidth=Screen.width;lastScreenHeight=Screen.height;lastSafeArea=Screen.safeArea;
                if(resized)
                {
                    var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1600f,safe.height/900f);
                    uiRoot.anchorMin=uiRoot.anchorMax=uiRoot.pivot=new Vector2(.5f,.5f);uiRoot.sizeDelta=new Vector2(1600,900);uiRoot.localScale=Vector3.one*scale;
                    uiRoot.anchoredPosition=safe.center-new Vector2(Screen.width,Screen.height)/2;
                }
                usedWidgets.Clear();usedViews.Clear();focusable.Clear();activeLayouts.Clear();siblingCursors.Clear();drawnContext=UiContext;RenderPass();
                foreach(var pair in uiWidgets)if(pair.Value.rect)pair.Value.rect.gameObject.SetActive(usedWidgets.Contains(pair.Key));
                foreach(var view in studioRoot.Views)
                {
                    view.gameObject.SetActive(usedViews.Contains(view.key));
                    if(studioRoot.mockupKit)
                    {
                        var portrait=view.transform.Find("Mockup Commander Portrait");if(portrait)portrait.SetAsLastSibling();
                        var workshop=view.transform.Find("Mockup Workshop");if(workshop){workshop.gameObject.SetActive(profile.gear.Count==0);workshop.SetAsLastSibling();}
                    }
                }
                if(studioRoot.mockupKit)foreach(var layout in activeLayouts)layout.Apply();
            }
            finally{dispatchOnly=false;renderingUi=false;}
        }
        void RenderPass()
        {
            string key=inBattle?"battle":page=="march"?HomeView:page=="barracks"?NormalizeBarracksTab():page=="town"?NormalizeTownTab():page=="quests"?QuestView:page;
            SelectView(key,"screen");uiInputAllowed=modal==""&&(!inBattle||(!paused&&!resolved));
            if(inBattle){useCodeGeometry=true;BattleHUD();}else HubUI();
            if(modal!="")
            {
                BeginPopup(modal,modal is "settings" or "book" or "prebattle" or "choice" or MapView);
                if(modal=="book")Book();else if(modal=="settings")Settings();else if(modal=="prebattle")Prebattle();else if(modal=="choice")ChoiceModal();else if(modal==MapView)MapPopup();
            }
            else if(inBattle&&resolved){BeginPopup("result");Result();}
            else if(inBattle&&paused){BeginPopup("pause");PausePopup();}
            if(Time.unscaledTime<toastUntil)
            {
                SelectView("toast","notification");Surface(438,806,870,64,C("#183e53"));Icon(454,820,32,"Check");Text(500,822,780,toast,label,40);
            }
        }
        void BeginPopup(string key,bool closable=false)
        {
            SelectView(key,"popup");uiInputAllowed=true;useCodeGeometry=true;
            if(closable){if(ScrimButton())modal="";}else Scrim();
        }
        string NormalizeBarracksTab()
        {
            // Trả về key view; tab "gear" cũ đã gộp vào trang chi tiết mèo.
            if(Array.IndexOf(BarracksTabs,tab)<0)tab="cats";return BarracksViewKey();
        }
        string NormalizeTownTab(){if(Array.IndexOf(TownTabs,tab)<0)tab="hub";return TownViewKey();}
        void SelectView(string key,string layer)
        {
            renderViewKey=key;uiContainer="";useCodeGeometry=false;drawingHubChrome=false;popupLayer=layer=="popup";if(dispatchOnly)return;
            var view=studioRoot.Find(key);
            // Màn chính, chrome trang con và popup Bản đồ chưa có prefab: tạo view rỗng lúc chạy, widget do code đặt vị trí.
            if(!view&&(authoringUi||IsRuntimeView(key)))
            {
                var parent=layer=="screen"?studioRoot.screens:layer=="popup"?studioRoot.popups:studioRoot.notifications;
                var rt=new GameObject((layer=="popup"?"Popup_":"Screen_")+key,typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(parent,false);Place(rt,0,0,1600,900);
                view=rt.gameObject.AddComponent<StudioUIView>();view.key=key;view.layer=layer;
                studioRoot.InvalidateViews();
            }
            if(!view)throw new InvalidOperationException("Missing UI view prefab: "+key);
            view.gameObject.SetActive(true);uiParent=(RectTransform)view.transform;usedViews.Add(key);
            if(layer=="screen")FrameView(uiParent,key);
        }
        WidgetIdentity Identity(string kind,float x,float y)
        {
            var address=new WidgetAddress{view=renderViewKey,kind=kind,container=uiContainer,x=x,y=y};
            if(!widgetIdentities.TryGetValue(address,out var identity))
            {
                string id=kind+":"+uiContainer+":"+x.ToString("0.###",CultureInfo.InvariantCulture)+":"+y.ToString("0.###",CultureInfo.InvariantCulture);
                identity=new WidgetIdentity{id=id,key=renderViewKey+"/"+id};widgetIdentities.Add(address,identity);
            }
            return identity;
        }
        string WidgetId(string kind,float x,float y)=>Identity(kind,x,y).id;
        Widget GetWidget(string kind,float x,float y,float w,float h,bool dynamicLayout=false,bool placeGeometry=true)
        {
            var identity=Identity(kind,x,y);string id=identity.id,key=identity.key;usedWidgets.Add(key);
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
            var owner=item.binding.layoutOwner?item.binding.layoutOwner:uiParent;
            if(item.rect.parent!=owner)item.rect.SetParent(owner,false);
            if(placeGeometry&&(authoringUi||!item.binding.authored||dynamicLayout||(useCodeGeometry&&!item.binding.layoutOwner)))Place(item.rect,x,y,w,h);
            if(!item.binding.layoutOwner)
            {
                siblingCursors.TryGetValue(owner,out int index);
                if(item.rect.GetSiblingIndex()!=index)item.rect.SetSiblingIndex(index);
                siblingCursors[owner]=index+1;
            }
            return item;
        }
        Widget Bind(StudioWidgetBinding binding)
        {
            var go=binding.gameObject;var item=new Widget{binding=binding,rect=(RectTransform)binding.transform,image=go.GetComponent<Image>(),button=go.GetComponent<UnityEngine.UI.Button>(),scroll=go.GetComponent<ScrollRect>(),input=go.GetComponent<InputField>()};
            item.text=binding.kind=="text"?go.GetComponent<Text>():go.GetComponentInChildren<Text>(true);
            if(item.button&&item.text)ApplyFont(item.text,FontStyle.Bold);
            if(item.scroll)item.content=item.scroll.content;
            if(item.button&&studioRoot.mockupKit)
            {
                var layout=go.GetComponent<StudioButtonLayout>();if(!layout)layout=go.AddComponent<StudioButtonLayout>();
                if(!layout.label)layout.label=item.text;
                item.layout=layout;
            }
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
        void FloatingText(int slot,Vector2 position,BattleEffect effect)
        {
            if(dispatchOnly)return;
            if(!floatingLayer)
            {
                floatingLayer=new GameObject("Pooled Damage Text",typeof(RectTransform),typeof(Canvas)).GetComponent<RectTransform>();
                floatingLayer.SetParent(studioRoot.Find("battle").transform,false);
                floatingLayer.anchorMin=Vector2.zero;floatingLayer.anchorMax=Vector2.one;
                floatingLayer.offsetMin=floatingLayer.offsetMax=Vector2.zero;
            }
            var parent=uiParent;string container=uiContainer;uiParent=floatingLayer;uiContainer="floating";
            var item=GetWidget("text",slot,0,110,35,true,false);
            uiParent=parent;uiContainer=container;
            Place(item.rect,position.x-25,position.y,110,35);
            item.text.text=effect.text;item.text.color=small.color*effect.color;item.text.fontSize=small.fontSize;
            ApplyFont(item.text,small.fontStyle);item.text.alignment=TextAnchor.UpperLeft;
            floatingLayer.SetAsLastSibling();
        }
        void SpriteBox(float x,float y,float w,float h,string key,Color color,bool preserve=false,string kind="image",string marchFrame=null,string catsFrame=null)
        {
            if(dispatchOnly)return;bool fill=key=="fill"||key.StartsWith("bar_fill_");var item=GetWidget(kind,x,y,w,h,fill);
            // Bảng trong popup chặn chạm để chạm ra ngoài (vào nền mờ) mới đóng popup.
            if(popupLayer&&key=="panel"&&item.image)item.image.raycastTarget=true;
            string managementFrame=catsFrame??(useCodeGeometry&&key=="panel"?(renderViewKey=="battle"?"CombatPlate":"Detail"):null);
            if(UsesSharedArt&&managementFrame!=null&&CatsFrame(managementFrame) is Sprite catsSprite&&catsSprite)
            {
                StudioNineSlice.Apply(item.image,catsSprite,18);
                item.image.color=Color.white;
                return;
            }
            if(UsesMarchArt&&marchFrame!=null&&MarchFrame(marchFrame) is Sprite frame&&frame)
            {
                StudioNineSlice.Apply(item.image,frame,18);
                item.image.color=Color.white;
                return;
            }
            if(studioRoot.mockupKit)
            {
                string skin=string.IsNullOrEmpty(item.binding.spriteKey)?key:item.binding.spriteKey;
                StudioNineSlice.Apply(item.image,theme.Get(skin),skin.StartsWith("bar_")?5:skin=="panel"?16:14,preserve);
                item.image.color=kind=="scrim"?color:Color.white;
                return;
            }
            if(Configure(item)||key=="fill")
            {
                StudioNineSlice.Apply(item.image,theme?theme.Get(key):null,key=="bar"||key=="fill"?5:key=="panel"?16:14,preserve);item.image.color=color;
            }
            else if(theme&&item.image.sprite!=theme.Get(key))StudioNineSlice.Apply(item.image,theme.Get(key),key=="bar"?5:key=="panel"?16:14,preserve);
        }
        void Surface(float x,float y,float w,float h,Color color,string marchFrame=null,string catsFrame=null)=>SpriteBox(x,y,w,h,"panel",color,marchFrame:marchFrame,catsFrame:catsFrame);
        void Icon(float x,float y,float size,string key)=>SpriteBox(x,y,size,size,key,Color.white,true);
        void Text(float x,float y,float w,string value,StudioTextStyle style=null,float h=35,bool dynamicLayout=false,bool richText=false,TextAnchor? align=null,int? size=null)
        {
            if(dispatchOnly)return;var s=style??label;var item=GetWidget("text",x,y,w,Mathf.Max(h,s.fontSize*1.45f),dynamicLayout);item.text.text=value;
            if(richText)item.text.supportRichText=true;
            if(Configure(item)){item.rect.name="Text • "+value.Replace('\n',' ');item.text.fontSize=s.fontSize;ApplyFont(item.text,s.fontStyle);item.text.color=s.color*textTint;item.text.alignment=TextAnchor.UpperLeft;item.text.resizeTextForBestFit=false;}
            if(align.HasValue)item.text.alignment=align.Value;
            if((UsesMarchArt||UsesSharedArt)&&item.text.color.a>0)
            {
                item.text.color=s==title?C("#ffe461"):C("#fff8f3");
                if(s==title)
                {
                    ApplyFont(item.text,FontStyle.BoldAndItalic);
                    item.text.fontSize=(UsesSharedArt&&h>=55)||h>=70?40:36;
                }
                MarchLettering(item.text,s==title?3:1);
            }
            // Cỡ chữ riêng: tự co (tối thiểu 16) để tên dài không xuống dòng.
            if(size.HasValue){item.text.fontSize=size.Value;item.text.resizeTextForBestFit=true;item.text.resizeTextMaxSize=size.Value;item.text.resizeTextMinSize=16;}
        }
        bool Button(float x,float y,float w,float h,string value,bool enabled=true,bool primary=false,string normalSpriteKey="button")
        {
            string id=Identity("button",x,y).key;
            if(dispatchOnly){if(pendingButton!=id||!enabled||!uiInputAllowed)return false;pendingButton=null;return true;}
            var item=GetWidget("button",x,y,w,h);lastButton=item;
            if(Configure(item))
            {
                item.rect.name="Button • "+value.Replace('\n',' ');item.text.fontSize=h<55?17:19;item.text.alignment=TextAnchor.MiddleCenter;
                var tr=(RectTransform)item.text.transform;tr.offsetMin=new Vector2(12,5);tr.offsetMax=new Vector2(-12,-5);
            }
            SetButtonSprite(item,primary?"primary":normalSpriteKey);item.text.text=value;item.text.color=!enabled?C("#c6d2e8"):primary?C("#44300e"):C("#f5f6ff");item.button.interactable=enabled&&uiInputAllowed;
            if(UsesMarchArt||UsesSharedArt)
            {
                bool goldLabel=primary&&(enabled||!useCodeGeometry);
                if(goldLabel)item.text.color=C("#241333");
                MarchLettering(item.text,goldLabel?0:1);
                if(UsesSharedArt&&useCodeGeometry&&item.layout)item.layout.fontSize=h>=55?22:18;
            }
            var colors=item.button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.75f,.8f,.9f);colors.selectedColor=C("#9ae6ff");colors.disabledColor=new Color(.5f,.54f,.65f,.75f);colors.fadeDuration=reduceMotion?0:.08f;item.button.colors=colors;
            item.context=UiContext;
            if(!item.clickBound)
            {
                item.clickBound=true;item.button.onClick.RemoveAllListeners();
                item.button.onClick.AddListener(()=>{if(item.context!=UiContext)return;pendingButton=id;RefreshUi();});
            }
            if(item.layout)activeLayouts.Add(item.layout);
            focusable.Add(item.button);return false;
        }
        bool UsesMarchArt => renderViewKey=="march";
        bool UsesCatsArt => renderViewKey=="barracks-cats";
        bool UsesManagementArt => renderViewKey.StartsWith("barracks-",StringComparison.Ordinal)||
            renderViewKey.StartsWith("town-",StringComparison.Ordinal)||renderViewKey is "chest" or "quests";
        bool UsesSharedArt => drawingHubChrome||UsesManagementArt||IsRuntimeView(renderViewKey)||renderViewKey is "battle" or "prebattle" or "result" or "settings" or "book" or "choice" or "pause";
        Sprite MarchFrame(string name)=>UiArt("Interface/March/"+name);
        Sprite CatsFrame(string name)=>UiArt("Interface/Cats/"+name);
        static void MarchLettering(Text text,float width)
        {
            if(!text.TryGetComponent<Outline>(out var outline))
            {
                if(width<=0)return;
                outline=text.gameObject.AddComponent<Outline>();
            }
            outline.effectColor=C("#140c30");
            outline.effectDistance=new Vector2(width,-width);
            outline.enabled=width>0;
        }
        void SetButtonSprite(Widget item,string key)
        {
            if(item==null)return;
            var sprite=UsesSharedArt?CatsFrame(key switch
            {
                "primary" or "selected"=>"Gold",
                "blue"=>"Blue",
                "cats-tab-selected" or "cats-tile-selected"=>"Cyan",
                "cats-tile"=>"Tile",
                _=>"Violet"
            }):UsesMarchArt?MarchFrame(key switch
            {
                "button"=>"VioletButton",
                "blue"=>"BluePanel",
                "stage"=>"BluePanel",
                _=>"GoldButton"
            }):null;
            StudioNineSlice.Apply(item.image,sprite?sprite:theme?theme.Get(key):null,UsesMarchArt||UsesSharedArt?18:14);
        }
        bool IllustratedButton(float x, float y, float w, float h, string value, string artKey, bool enabled = true,
            bool primary = false, StudioButtonLayout.Content content = StudioButtonLayout.Content.Illustrated, string badgeKey = null, bool selected = false,
            string normalSpriteKey = "button")
        {
            string frameKey = UsesSharedArt && content == StudioButtonLayout.Content.Tile ? "cats-tile" :
                UsesMarchArt && content == StudioButtonLayout.Content.Stage ? "stage" : normalSpriteKey;
            bool clicked = Button(x, y, w, h, value, enabled, primary, frameKey);
            if (dispatchOnly) return clicked;

            var item = lastButton;
            if (selected) SetButtonSprite(item, UsesSharedArt && content == StudioButtonLayout.Content.Tile ? "cats-tile-selected" : "selected");
            var layout = item.layout;
            if (!layout)
            {
                layout = item.rect.gameObject.AddComponent<StudioButtonLayout>();
                item.layout = layout;
                layout.label = item.text;
            }
            layout.content = content;
            if(UsesMarchArt&&content==StudioButtonLayout.Content.Illustrated)layout.fontSize=24;
            if(UsesSharedArt)layout.fontSize=content==StudioButtonLayout.Content.Tile?20:w>=200?22:18;
            if (!layout.icon)
                layout.icon = ButtonImage("Artwork", item.rect);
            layout.icon.sprite = UiArt(artKey);
            layout.icon.preserveAspect = true;
            layout.icon.color = enabled ? Color.white : new Color(.76f, .82f, .92f, .58f);
            if (content == StudioButtonLayout.Content.Stage)
            {
                if (!layout.badge)
                {
                    layout.badge = ButtonImage("Stage status", item.rect);
                    layout.badge.transform.SetAsLastSibling();
                }
                layout.badge.sprite = string.IsNullOrEmpty(badgeKey) ? null : UiArt(badgeKey);
                layout.badge.enabled = layout.badge.sprite;
                item.text.color = Color.white;
                if (!item.text.TryGetComponent<Outline>(out var outline))
                {
                    outline = item.text.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(.02f, .05f, .12f, 1);
                    outline.effectDistance = new Vector2(2, -2);
                }
                if(UsesMarchArt)MarchLettering(item.text,2);
                if (!enabled) layout.icon.color = new Color(.64f, .71f, .83f, .38f);
            }
            if (content == StudioButtonLayout.Content.Symbol) item.text.color = Color.clear;
            if (!studioRoot.mockupKit) layout.Apply();
            return clicked;
        }
        static Image ButtonImage(string name, RectTransform parent)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            child.transform.SetAsFirstSibling();
            var image = child.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }
        Sprite UiArt(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var sprite = theme ? theme.Get(key) : null;
            if (sprite) return sprite;
            if (uiArtSprites.TryGetValue(key, out sprite)) return sprite;
            sprite = Resources.Load<Sprite>("Art/" + key);
            if (sprite) uiArtSprites.Add(key, sprite);
            return sprite;
        }
        void Panel(float x,float y,float w,float h,float alpha=1)=>Surface(x,y,w,h,new Color(.13f,.18f,.30f,alpha));
        void Art(float x,float y,float w,float h,string key)
        {
            if(dispatchOnly)return;var item=GetWidget("image",x,y,w,h);var sprite=UiArt(key);StudioNineSlice.Apply(item.image,sprite?sprite:view.Art(key),14,true);item.image.color=Color.white;
        }
        void Bar(float x,float y,float w,float h,float ratio,Color color)
        {
            SpriteBox(x,y,w,h,"bar",Color.white);string fill=studioRoot&&studioRoot.mockupKit?(color.g>color.r&&color.g>color.b?"bar_fill_green":color.r>color.b?"bar_fill_gold":"bar_fill_cyan"):"fill";
            if(ratio>0)SpriteBox(x+3,y+3,Mathf.Max(1,(w-6)*Mathf.Clamp01(ratio)),h-6,fill,color);
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
        void Backdrop(Color color)=>SpriteBox(-500,-500,2600,1900,"",color,false,"scrim");
        void NameInput(float x,float y,float w,float h)
        {
            if(dispatchOnly)return;var item=GetWidget("input",x,y,w,h);
            if(Configure(item)){StudioNineSlice.Apply(item.image,theme?theme.Get("input"):null,12);item.image.color=C("#d9e9ff");item.text.fontSize=22;item.text.color=C("#16213b");item.text.alignment=TextAnchor.MiddleLeft;}
            if(!item.input.isFocused)item.input.SetTextWithoutNotify(profile.name);
        }
        void ApplyFont(Text text,FontStyle style)
        {
            bool strong=strongFont&&(style==FontStyle.Bold||style==FontStyle.BoldAndItalic);
            text.font=strong?strongFont:font;
            text.fontStyle=strong?(style==FontStyle.BoldAndItalic?FontStyle.Italic:FontStyle.Normal):style;
        }
        void Styles()
        {
            if(label!=null)return;
            font=Resources.Load<Font>("UI/Fonts/Nunito-Bold");
            strongFont=Resources.Load<Font>("UI/Fonts/Nunito-Black");
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label=new StudioTextStyle(20,C("#e4eafa"));small=new StudioTextStyle(17,C("#aabbd6"));title=new StudioTextStyle(34,C("#fff0bc"),FontStyle.Bold);subtitle=new StudioTextStyle(23,C("#e4eafa"),FontStyle.Bold);hudText=new StudioTextStyle(16,Color.white,FontStyle.Bold);hudMeta=new StudioTextStyle(15,C("#e8f4ff"));
        }
    }
}
