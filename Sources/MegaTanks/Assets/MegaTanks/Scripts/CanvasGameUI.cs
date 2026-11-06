using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed partial class CanvasGameUI : MonoBehaviour
    {
        public const string DesignUrl="https://docs.google.com/presentation/d/1wYHK3rKs4jGDTON6XZ8SG-Dip3ueG96ekwUVsSqcGwk/edit";
        readonly Color ink=new Color(.035f,.065f,.105f), panel=new Color(.10f,.15f,.20f), edge=new Color(.23f,.32f,.39f);
        readonly Color cyan=new Color(.32f,.88f,.95f), gold=new Color(1,.72f,.3f), muted=new Color(.59f,.68f,.74f);
        BattleGame game;
        RectTransform root,home,hud,overlay,modal,damageLayer;
        readonly Dictionary<string,Text> live=new Dictionary<string,Text>();
        readonly Dictionary<string,Image> bars=new Dictionary<string,Image>();
        Font font;
        string screenKey="",popup="";
        int stageIndex,accessoryIndex,sort;
        EquipmentType? filter,activeSlot;
        EquipmentDefinition selected;
        bool descending;
        RenderTexture portrait;
        Camera portraitCamera;
        GameObject portraitTank;
        Button defensive,aggressive;
        Vector2 lastSize;
        Rect lastSafe;
        public string Popup=>popup;
        public int StageIndex=>stageIndex;
        public EquipmentType? Filter=>filter;
        public int InventoryVisibleCount{get;private set;}
        public static CanvasGameUI Create(BattleGame battle)
        {
            var ui=FindObjectOfType<CanvasGameUI>();
            var go=ui!=null?ui.gameObject:new GameObject("MegaTanks UI Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(FindObjectOfType<EventSystem>()==null)new GameObject("UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            if(ui==null)ui=go.AddComponent<CanvasGameUI>();ui.game=battle;ui.Build();return ui;
        }
        RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Image Fill(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var image=Box(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;SkinPanel(image,name);return image;}
        Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size=28,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=Box(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;
            t.color=color??Color.white;t.alignment=align;t.raycastTarget=false;t.supportRichText=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        Button Button(Transform parent,string name,string title,float x,float y,float w,float h,Action click,Color? color=null)
        {
            var image=Fill(parent,name,x,y,w,h,color??edge);var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var colors=b.colors;colors.highlightedColor=new Color(1,1,1,.85f);colors.pressedColor=new Color(.7f,.8f,.85f);colors.disabledColor=new Color(.35f,.4f,.44f,.55f);b.colors=colors;
            Label(image.transform,"Label",title,10,3,w-20,h-6,27,Color.white,TextAnchor.MiddleCenter);SkinButton(b,name,title,color.HasValue&&color.Value!=edge);b.onClick.AddListener(()=>click());return b;
        }
        void Clear(Transform t){foreach(Transform child in t){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root=Box(transform,"Safe Area",0,0,1920,1080);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);
            home=Box(root,"Home Screen",0,0,1920,1080);hud=Box(root,"Battle HUD",0,0,1920,1080);
            overlay=Box(root,"Encounter Screens",0,0,1920,1080);damageLayer=Box(root,"Floating Damage",0,0,1920,1080);modal=Box(root,"Popup Layer",0,0,1920,1080);
            MakePortrait();BuildHome();BuildHud();FitSafeArea();Refresh();
        }
        void FitSafeArea()
        {
            var canvasRect=(RectTransform)transform;var safe=Screen.safeArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,safe.min,null,out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,safe.max,null,out var max);
            root.anchoredPosition=(min+max)*.5f;root.localScale=Vector3.one*Mathf.Min((max.x-min.x)/1920,(max.y-min.y)/1080);
            lastSafe=safe;lastSize=new Vector2(Screen.width,Screen.height);
        }
        void BuildHome()
        {
            Fill(home,"Background",0,0,1920,1080,ink);Fill(home,"Top Accent",0,0,1920,5,cyan);
            var avatar=Button(home,"Avatar Menu","",48,40,170,110,()=>OpenPopup("Settings"),panel);
            var player=Box(avatar.transform,"Player Avatar",45,2,80,78).gameObject.AddComponent<RawImage>();player.texture=portrait;player.raycastTarget=false;
            Label(avatar.transform,"Menu Label","MENU",0,78,170,30,22,Color.white,TextAnchor.MiddleCenter);
            if(skin!=null)Icon(avatar.transform,"Menu Icon",skin.menu,134,79,24,24);
            Label(home,"Brand","MEGATANKS",260,38,900,95,65);Label(home,"Tagline","PREPARE YOUR TANK. CHOOSE YOUR ROUTE.",265,125,1250,50,25,muted);
            var area=Fill(home,"Stage Swipe Area",290,225,1340,485,new Color(.07f,.11f,.16f));area.gameObject.AddComponent<StageSwipeInput>().target=this;
            var badge=Box(area.transform,"Stage Badge",450,20,440,440).gameObject.AddComponent<CanvasCircleImage>();badge.color=panel;badge.gameObject.AddComponent<Outline>().effectColor=cyan;
            live["stage"]=Label(area.transform,"Selected Stage","",450,145,440,110,52,Color.white,TextAnchor.MiddleCenter);
            live["stageCount"]=Label(area.transform,"Stage Count","",450,265,440,65,26,muted,TextAnchor.MiddleCenter);
            Button(home,"Previous Stage","<",125,410,125,130,()=>ChangeStage(-1));Button(home,"Next Stage",">",1670,410,125,130,()=>ChangeStage(1));
            Label(home,"Swipe Hint","Swipe or use the arrows to select a stage",410,720,1100,45,24,muted,TextAnchor.MiddleCenter);
            Button(home,"Play","PLAY",690,795,540,100,Play,new Color(.12f,.44f,.48f));
            Button(home,"Crew","CREW / COMING SOON",210,965,460,78,()=>OpenPopup("Crew"));
            Button(home,"Equipment","EQUIPMENT",730,965,460,78,OpenEquipment);
            Button(home,"Tech","TECH / COMING SOON",1250,965,460,78,()=>OpenPopup("Tech"));RefreshStage();
        }
        public void ChangeStage(int delta){int count=game.Frontend?.stages?.Length??1;stageIndex=Mathf.Clamp(stageIndex+delta,0,Mathf.Max(0,count-1));RefreshStage();}
        void RefreshStage()
        {
            var stages=game.Frontend?.stages;live["stage"].text=stages!=null&&stages.Length>0?stages[stageIndex].stageName:"Stage 1";
            live["stageCount"].text=(stageIndex+1)+" / "+Mathf.Max(1,stages?.Length??1);
            home.Find("Previous Stage").GetComponent<Button>().interactable=stageIndex>0;home.Find("Next Stage").GetComponent<Button>().interactable=stageIndex<(stages?.Length??1)-1;
            foreach(string name in new[]{"Previous Stage","Next Stage"})
            {
                var control=home.Find(name).GetComponent<Button>();var icon=control.transform.Find("Control Icon");
                if(icon!=null)icon.GetComponent<Image>().color=new Color(1,1,1,control.interactable?1:.3f);
            }
        }
        public void Play(){ClosePopup();if(game.Frontend?.stages?.Length>0)game.SelectStage(game.Frontend.stages[stageIndex]);game.StartBattle();screenKey="";}
        public void OpenEquipment(){activeSlot=null;filter=null;selected=null;OpenPopup("Equipment");}
        public void OpenPopup(string value){popup=value;DrawPopup();}
        public void ClosePopup(){popup="";Clear(modal);}
        void PopupShell(string title,float x,float y,float w,float h,Action close)
        {
            Button(modal,"Dismiss Backdrop","",0,0,1920,1080,close,new Color(0,0,0,.78f));Fill(modal,"Popup Panel",x,y,w,h,panel);
            if(skin!=null){var header=Fill(modal,"Popup Header",x+5,y+5,w-10,88,new Color(.14f,.24f,.32f));SpriteImage(header,skin.panelHeader,header.color);header.raycastTarget=false;}
            Label(modal,"Popup Title",title,x+40,y+18,w-160,65,38);Button(modal,"Close Popup","X",x+w-100,y+20,65,60,close);
        }
        void DrawPopup()
        {
            Clear(modal);if(popup=="")return;if(popup=="Equipment"){DrawEquipment();return;}
            PopupShell(popup.ToUpperInvariant(),410,170,1100,740,ClosePopup);
            if(popup=="Settings")
            {
                SettingsSlider("Sound",GameAudioSettings.Sound,330,GameAudioChannel.Sound);SettingsSlider("Music",GameAudioSettings.Music,470,GameAudioChannel.Music);
                if(skin!=null)Icon(modal,"Settings Icon",skin.settings,1320,194,50,50);
                Label(modal,"Vibration Label","VIBRATION",480,640,350,80,30);
                var bg=Fill(modal,"Vibration Toggle",1050,650,280,60,edge);var toggle=bg.gameObject.AddComponent<Toggle>();
                if(skin!=null)SpriteImage(bg,skin.switchBackground,new Color(.20f,.25f,.33f));
                Image switchFill=null;if(skin!=null){switchFill=Fill(bg.transform,"Switch Fill",6,6,268,48,cyan);SpriteImage(switchFill,skin.switchFill,cyan);switchFill.raycastTarget=false;}
                var handleBox=Box(bg.transform,"Switch Handle",7,7,46,46);Image check=skin!=null?handleBox.gameObject.AddComponent<Image>():handleBox.gameObject.AddComponent<CanvasCircleImage>();check.color=cyan;
                if(skin!=null)SpriteImage(check,skin.switchHandle,Color.white);
                var hint=Label(bg.transform,"Toggle Hint","",70,0,140,60,24,Color.white,TextAnchor.MiddleCenter);
                toggle.targetGraphic=bg;toggle.graphic=null;toggle.isOn=GameAudioSettings.Vibration;
                Action<bool> show=value=>{check.rectTransform.anchoredPosition=new Vector2(value?227:7,-7);hint.text=value?"ON":"OFF";hint.color=value?ink:Color.white;if(switchFill!=null)switchFill.gameObject.SetActive(value);};show(toggle.isOn);
                toggle.onValueChanged.AddListener(value=>{GameAudioSettings.SetVibration(value);show(value);});
                Label(modal,"Audio Note","Sound and music levels are saved for this device.",480,790,920,55,24,muted);
            }
            else Label(modal,"Placeholder",popup+" will be available in a future update.",480,400,950,180,36,muted,TextAnchor.MiddleCenter);
        }
        void SettingsSlider(string title,float value,float y,GameAudioChannel channel)
        {
            Label(modal,title+" Label",title.ToUpperInvariant(),skin!=null?550:480,y,skin!=null?280:350,80,30);
            if(skin!=null)Icon(modal,title+" Icon",channel==GameAudioChannel.Sound?skin.sound:skin.music,480,y+16,48,48);
            var bg=Fill(modal,title+" Slider",900,y+20,450,34,edge);var slider=bg.gameObject.AddComponent<Slider>();
            var fill=Fill(bg.transform,"Fill",0,0,450,34,cyan);var handleBox=Box(bg.transform,"Handle",0,-13,60,60);Image handle=skin!=null?handleBox.gameObject.AddComponent<Image>():handleBox.gameObject.AddComponent<CanvasCircleImage>();handle.color=Color.white;
            if(skin!=null){SpriteImage(bg,skin.sliderBackground,new Color(.20f,.25f,.33f));SpriteImage(fill,skin.sliderFill,cyan);SpriteImage(handle,skin.sliderHandle,Color.white,false);}
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=value;
            slider.onValueChanged.AddListener(v=>GameAudioSettings.Set(channel,v));
        }
        void DrawEquipment()
        {
            PopupShell("EQUIPMENT",90,30,1740,1020,ClosePopup);
            var tank=Box(modal,"Tank Avatar",785,135,350,430).gameObject.AddComponent<RawImage>();tank.texture=portrait;tank.raycastTarget=false;
            Slot("Mortar",EquipmentType.Mortar,0,265,150);Slot("Accessory 2",EquipmentType.Accessory,0,265,300);Slot("Accessory 3",EquipmentType.Accessory,1,265,450);
            Slot("MG",EquipmentType.MachineGun,0,1300,150);Slot("Cannon",EquipmentType.Cannon,0,1300,300);Slot("Armor",EquipmentType.Armor,0,1300,450);
            Button(modal,"Inventory Filter","FILTER: "+(filter.HasValue?TypeName(filter.Value):"ALL"),135,610,425,64,OpenFilter);
            string[] sorts={"NAME","TYPE","DAMAGE"};Button(modal,"Inventory Sort","ORDER BY: "+sorts[sort]+(descending?" v":" ^"),1200,610,560,64,()=>{sort=(sort+1)%3;DrawPopup();});
            Button(modal,"Sort Direction",descending?"DESC":"ASC",600,610,170,64,()=>{descending=!descending;DrawPopup();});
            Scroll(modal,"Inventory",135,700,1000,290,out var content);
            var items=(game.Frontend?.inventory??new EquipmentDefinition[0]).Where(i=>i!=null&&(!filter.HasValue||i.Type==filter));
            Func<EquipmentDefinition,string> name=i=>string.IsNullOrEmpty(i.displayName)?i.name:i.displayName;
            IOrderedEnumerable<EquipmentDefinition> sorted=sort==1?items.OrderBy(i=>i.Type).ThenBy(name):sort==2?items.OrderBy(i=>i is GunDefinition g?g.stats.damage:i is MortarDefinition m?m.stats.damage:0).ThenBy(name):items.OrderBy(name);
            var list=(descending?sorted.Reverse():sorted).ToArray();InventoryVisibleCount=list.Length;content.sizeDelta=new Vector2(1000,Mathf.Max(290,Mathf.Ceil(list.Length/6f)*140));
            for(int i=0;i<list.Length;i++)
            {
                var item=list[i];var cell=Button(content,"Item "+item.name,"",i%6*162,i/6*140,150,125,()=>{selected=item;DrawPopup();},selected==item?new Color(.13f,.39f,.43f):edge);
                if(item.icon!=null){var icon=Fill(cell.transform,"Icon",12,7,126,65,Color.white);icon.sprite=item.icon;icon.preserveAspect=true;icon.raycastTarget=false;icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.anchoredPosition=new Vector2(75,-39.5f);}
                Label(cell.transform,"Item Name",name(item),5,item.icon!=null?73:20,140,48,21,Color.white,TextAnchor.MiddleCenter);
                if(game.Equipment.IsEquipped(item)) EquippedBadge(cell.transform);
            }
            if(list.Length==0)Label(content,"Empty Inventory","No "+(filter.HasValue?TypeName(filter.Value).ToLowerInvariant():"equipment")+" items available yet.",25,45,950,110,30,muted,TextAnchor.MiddleCenter);
            DrawItemDetail(name);
        }
        void Slot(string title,EquipmentType type,int index,float x,float y)
        {
            EquipmentDefinition item=game.Equipment.GetEquipped(type,index);
            var slot=Button(modal,"Slot "+title,"",x,y,350,105,()=>{activeSlot=type;accessoryIndex=index;filter=type;selected=null;DrawPopup();},activeSlot==type&&(type!=EquipmentType.Accessory||index==accessoryIndex)?new Color(.12f,.4f,.44f):edge);
            bool hasIcon=item!=null&&item.icon!=null;
            if(hasIcon){var icon=Fill(slot.transform,"Equipped Item Icon",12,12,82,81,Color.white);icon.sprite=item.icon;icon.preserveAspect=true;icon.raycastTarget=false;icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.anchoredPosition=new Vector2(53,-52.5f);}
            Label(slot.transform,"Slot Description",title.ToUpperInvariant()+"\n"+(item!=null?item.displayName:"EMPTY"),hasIcon?104:10,5,hasIcon?236:330,95,25,Color.white,TextAnchor.MiddleCenter);
            Vector2 start=new Vector2(x<900?x+350:x,y+52);
            Vector2 end=type==EquipmentType.MachineGun?new Vector2(1010,315):type==EquipmentType.Cannon?new Vector2(1080,378):type==EquipmentType.Mortar?new Vector2(850,355):type==EquipmentType.Armor?new Vector2(975,442):new Vector2(860,407+index*72);
            Line(modal,"Slot Connector",start,end,muted,2);
            Vector2 arrow=(start-end).normalized*12,side=new Vector2(-arrow.y,arrow.x)*.5f;
            Line(modal,"Slot Arrow",end,end+arrow+side,muted,2);Line(modal,"Slot Arrow",end,end+arrow-side,muted,2);
        }
        void EquippedBadge(Transform parent)
        {
            if(skin!=null&&skin.check!=null)
            {
                var background=Box(parent,"Equipped Badge Background",113,2,35,35).gameObject.AddComponent<CanvasCircleImage>();background.color=new Color(.05f,.42f,.24f);background.raycastTarget=false;
                Icon(parent,"Equipped Indicator",skin.check,116,5,29,29);return;
            }
            var badge=Box(parent,"Equipped Indicator",116,5,29,29).gameObject.AddComponent<CanvasCircleImage>();badge.color=new Color(.12f,.65f,.38f);badge.raycastTarget=false;
            Line(badge.transform,"Check",new Vector2(6,15),new Vector2(12,21),Color.white,3);
            Line(badge.transform,"Check",new Vector2(12,21),new Vector2(23,8),Color.white,3);
        }
        void OpenFilter()
        {
            var menu=Fill(modal,"Filter List",135,680,425,360,ink);var values=new EquipmentType?[]{null,EquipmentType.Cannon,EquipmentType.MachineGun,EquipmentType.Mortar,EquipmentType.Armor,EquipmentType.Accessory};
            for(int i=0;i<values.Length;i++){var value=values[i];Button(menu.transform,"Filter "+(value?.ToString()??"All"),value.HasValue?TypeName(value.Value):"ALL",8,8+i*58,409,52,()=>{filter=value;selected=null;DrawPopup();});}
        }
        void DrawItemDetail(Func<EquipmentDefinition,string> name)
        {
            Fill(modal,"Item Detail",1185,700,575,290,ink);
            if(selected==null){Label(modal,"Detail Prompt","Select an item to inspect or equip it.",1210,730,525,160,28,muted,TextAnchor.MiddleCenter);return;}
            Label(modal,"Detail Name",name(selected),1210,712,525,60,30);string detail=TypeName(selected.Type);
            if(selected is GunDefinition g)detail+=" / DMG "+g.stats.damage.ToString("0.#")+"\nRange "+g.stats.range+" | Salvo "+g.stats.salvoSize+" | Reload "+g.stats.reloadTime.ToString("0.##")+"s";
            if(selected is MortarDefinition m)detail+=" / DMG "+m.stats.damage.ToString("0.#")+"\nRange "+m.stats.range+" | Reload "+m.stats.reloadTime.ToString("0.##")+"s";
            Label(modal,"Item Stats",detail,1210,775,525,100,24,muted);var slot=activeSlot??selected.Type;bool compatible=selected.Type==slot;
            Button(modal,"Equip Item",compatible?"EQUIP":"INCOMPATIBLE SLOT",1210,900,525,65,()=>{game.Equipment.Equip(selected,slot,accessoryIndex);DrawPopup();},new Color(.12f,.42f,.46f)).interactable=compatible;
        }
        static string TypeName(EquipmentType t)=>t==EquipmentType.MachineGun?"MG":t==EquipmentType.Accessory?"ACCESSORIES":t.ToString().ToUpperInvariant();
        ScrollRect Scroll(Transform parent,string name,float x,float y,float w,float h,out RectTransform content,bool horizontal=false)
        {
            var v=Fill(parent,name,x,y,w,h,new Color(.055f,.09f,.13f));v.gameObject.AddComponent<RectMask2D>();content=Box(v.transform,"Content",0,0,w,h);
            var s=v.gameObject.AddComponent<ScrollRect>();s.viewport=v.rectTransform;s.content=content;s.horizontal=horizontal;s.vertical=!horizontal;s.movementType=ScrollRect.MovementType.Clamped;s.scrollSensitivity=60;return s;
        }
        void Line(Transform parent,string name,Vector2 a,Vector2 b,Color color,float width)
        {var d=b-a;var image=Fill(parent,name,a.x,a.y,d.magnitude,width,color);image.raycastTarget=false;image.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
        void MakePortrait()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            portrait=new RenderTexture(512,640,16,RenderTextureFormat.ARGB32);portrait.name="Tank UI Portrait";portrait.Create();
            portraitTank=Instantiate(game.Artwork.gameObject);portraitTank.name="UI Tank Portrait";portraitTank.transform.position=new Vector3(0,1000,0);portraitTank.transform.localScale=Vector3.one;
            portraitTank.GetComponent<TankArtwork>().ResetVisual();foreach(var b in portraitTank.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
            foreach(var t in portraitTank.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            game.TankCamera.cullingMask&=~(1<<30);
            portraitCamera=new GameObject("Tank Portrait Camera").AddComponent<Camera>();portraitCamera.orthographic=true;portraitCamera.cullingMask=1<<30;
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.targetTexture=portrait;portraitCamera.orthographicSize=5.8f;
            portraitCamera.transform.position=portraitTank.transform.position+new Vector3(.2f,4.5f,-10);portraitCamera.Render();portraitCamera.enabled=false;
        }
        void OnDestroy(){if(portraitTank!=null)Destroy(portraitTank);if(portraitCamera!=null)Destroy(portraitCamera.gameObject);if(portrait!=null){portrait.Release();Destroy(portrait);}}
    }
}
