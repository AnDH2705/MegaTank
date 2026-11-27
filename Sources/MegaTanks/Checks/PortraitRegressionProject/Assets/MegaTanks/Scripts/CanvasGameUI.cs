using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    [RequireComponent(typeof(UICanvasLayout))]
    public sealed partial class CanvasGameUI : MonoBehaviour
    {
        public const string DesignUrl="https://docs.google.com/presentation/d/1wYHK3rKs4jGDTON6XZ8SG-Dip3ueG96ekwUVsSqcGwk/edit";
        [SerializeField] CanvasUISkin skin;
        [SerializeField] EquipmentUISkin equipmentSkin;
        [SerializeField] RouteUISkin routeSkin;
        public CanvasUISkin Skin=>skin;
        public EquipmentUISkin EquipmentSkin=>equipmentSkin;
        public RouteUISkin RouteSkin=>routeSkin;
        BattleGame game;
        UICanvasLayout layout;
        RectTransform root;
        string popup="",screenKey="";
        int stageIndex,accessoryIndex,sort;
        EquipmentType? filter,activeSlot;
        EquipmentDefinition selected;
        bool descending;
        RenderTexture portrait;
        Camera portraitCamera;
        GameObject portraitTank;
        Vector2 lastSize;
        Rect lastSafe;
        public string Popup=>popup;
        public int StageIndex=>stageIndex;
        public EquipmentType? Filter=>filter;
        public int InventoryVisibleCount{get;private set;}
        UIView View(UIScreen screen)=>layout.View(screen);
        static void Text(UIView v,string key,string text)=>v.Get<Text>(key).text=text;
        static void Active(UIView v,string key,bool value)=>v.Get<RectTransform>(key).gameObject.SetActive(value);
        static void Click(UIView v,string key,Action action)
        {
            var button=v.Get<Button>(key);button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>action());
        }
        public static CanvasGameUI Create(BattleGame battle)
        {
            var ui=FindObjectOfType<CanvasGameUI>();
            if(ui==null)throw new InvalidOperationException("The scene needs the MegaTanksCanvas UI prefab.");
            ui.game=battle;ui.layout=ui.GetComponent<UICanvasLayout>();ui.root=ui.layout.safeArea;
            ui.layout.HideScreens();ui.MakePortrait();ui.BindHome();ui.BindHud();ui.BindSettings();ui.BindCurrencies();ui.BindProgression();
            ui.FitSafeArea();ui.RefreshStage();ui.Refresh();return ui;
        }
        void BindHome()
        {
            var v=View(UIScreen.Home);
            Click(v,"Avatar Menu",()=>OpenPopup("Settings"));Click(v,"Equipment",OpenEquipment);
            Click(v,"Crew",()=>OpenPopup("Crew"));Click(v,"Tech",()=>OpenPopup("Tech"));
            Click(v,"Previous Stage",()=>ChangeStage(-1));Click(v,"Next Stage",()=>ChangeStage(1));Click(v,"Play",Play);
            v.Get<StageSwipeInput>("Stage Swipe Area").target=this;
        }
        void FitSafeArea()
        {
            var canvasRect=(RectTransform)transform;var safe=Screen.safeArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,safe.min,null,out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,safe.max,null,out var max);
            root.anchoredPosition=(min+max)*.5f;root.localScale=Vector3.one*Mathf.Min((max.x-min.x)/1920,(max.y-min.y)/1080);
            // Controls retain their safe-area layout; the route background covers the
            // entire canvas, including the margins on wide/tall displays and cutouts.
            var background=View(UIScreen.Route).Get<RectTransform>("Radar Route Background");
            var parent=(RectTransform)background.parent;
            var canvasMin=(Vector2)parent.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.min));
            var canvasMax=(Vector2)parent.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.max));
            background.anchorMin=background.anchorMax=background.pivot=Vector2.one*.5f;
            background.anchoredPosition=(canvasMin+canvasMax)*.5f-parent.rect.center;
            background.sizeDelta=canvasMax-canvasMin;
            lastSafe=safe;lastSize=new Vector2(Screen.width,Screen.height);
        }
        public void ChangeStage(int delta){if(HtmlBattleScreen.Available){stageIndex=Mathf.Clamp(stageIndex+delta,0,HtmlBattleScreen.ReadConfig().stages.Length-1);RefreshStage();return;}int count=game.Frontend?.stages?.Length??1;stageIndex=Mathf.Clamp(stageIndex+delta,0,Mathf.Max(0,count-1));RefreshStage();}
        void RefreshStage()
        {
            var v=View(UIScreen.Home);if(HtmlBattleScreen.Available){var config=HtmlBattleScreen.ReadConfig();int count=config.stages.Length;stageIndex=Mathf.Clamp(stageIndex,0,count-1);Text(v,"Stage Name Plate/Selected Stage","Stage "+(stageIndex+1));Text(v,"Stage Swipe Area/Stage Count","STAGE "+(stageIndex+1)+" / "+count);Text(v,"Stage Swipe Area/Route Name",config.stages[stageIndex].title);v.Get<Button>("Previous Stage").interactable=stageIndex>0;v.Get<Button>("Next Stage").interactable=stageIndex<count-1;Active(v,"Previous Stage",count>1);Active(v,"Next Stage",count>1);v.Get<Image>("Stage Swipe Area").sprite=skin.homeStageSelector;return;}var stages=game.Frontend?.stages;
            string title=stages!=null&&stages.Length>0?stages[stageIndex].stageName:"Stage 1";
            Text(v,"Stage Name Plate/Selected Stage",title);Text(v,"Stage Swipe Area/Stage Count","ROUND 1 / "+(stages!=null&&stages.Length>0?stages[stageIndex].rounds:game.Stage.Definition.rounds));Text(v,"Stage Swipe Area/Route Name",title);
            v.Get<Button>("Previous Stage").interactable=stageIndex>0;v.Get<Button>("Next Stage").interactable=stageIndex<(stages?.Length??1)-1;
            Active(v,"Previous Stage",(stages?.Length??1)>1);Active(v,"Next Stage",(stages?.Length??1)>1);
            v.Get<Image>("Stage Swipe Area").sprite=(stages?.Length??1)>1?skin.homeStageSelector:skin.homeButtonBase;
        }
        public void Play(){if(HtmlBattleScreen.Available){ClosePopup();HtmlBattleScreen.Begin(game,stageIndex);return;}ClosePopup();if(game.Frontend?.stages?.Length>0)game.SelectStage(game.Frontend.stages[stageIndex]);game.StartBattle();screenKey="";Refresh();UpdateHud();}
        public void OpenEquipment(){activeSlot=null;filter=null;selected=null;OpenPopup("Equipment");}
        public void OpenPopup(string value)
        {
            if(designNavigating)return;designOpening=true;ClosePopup();designOpening=false;popup=value;
            UIScreen screen=value=="Equipment"?UIScreen.Equipment:value=="Settings"?UIScreen.Settings:value=="Crew"?UIScreen.Crew:UIScreen.Tech;
            var v=View(screen);v.gameObject.SetActive(true);
            Click(v,"Close Popup",ClosePopup);
            if(value=="Equipment")DrawEquipment();
            else if(value=="Crew"){crewStation=-1;crewSelected=null;crewFilter=0;DrawCrew();}
            else { Click(v,"Dismiss Backdrop",ClosePopup);if(value=="Settings")SyncSettings(); }
            if(FullScreenPopup(value))StartCoroutine(DesignTransition(View(UIScreen.Home).gameObject,v.gameObject,false));
        }
        public void ClosePopup()
        {
            if(!designOpening&&designNavigating)return;
            if(!designOpening&&FullScreenPopup(popup))
            {var old=View(popup=="Crew"?UIScreen.Crew:popup=="Equipment"?UIScreen.Equipment:UIScreen.Tech);popup="";StartCoroutine(DesignTransition(old.gameObject,View(UIScreen.Home).gameObject,true));return;}
            popup="";if(layout==null)return;
            foreach(var s in new[]{UIScreen.Equipment,UIScreen.Settings,UIScreen.Crew,UIScreen.Tech,UIScreen.LeaveRun})View(s).gameObject.SetActive(false);
            ResetMainNavigation();
        }
        void ConfirmHome()
        {
            ClosePopup();popup="Leave run";var v=View(UIScreen.LeaveRun);v.gameObject.SetActive(true);
            Click(v,"Dismiss Backdrop",ClosePopup);Click(v,"Close Popup",ClosePopup);Click(v,"Cancel Leave",ClosePopup);
            Click(v,"Confirm Leave",()=>{ClosePopup();game.ReturnHome();});
        }
        void BindSettings()
        {
            var v=View(UIScreen.Settings);
            foreach(var channel in new[]{GameAudioChannel.Sound,GameAudioChannel.Music})
            {
                var slider=v.Get<Slider>(channel+" Slider");slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(value=>GameAudioSettings.Set(channel,value));
            }
            var toggle=v.Get<Toggle>("Vibration Toggle");toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value=>{GameAudioSettings.SetVibration(value);ShowVibration(value);});
        }
        void SyncSettings()
        {
            var v=View(UIScreen.Settings);v.Get<Slider>("Sound Slider").SetValueWithoutNotify(GameAudioSettings.Sound);v.Get<Slider>("Music Slider").SetValueWithoutNotify(GameAudioSettings.Music);
            v.Get<Toggle>("Vibration Toggle").SetIsOnWithoutNotify(GameAudioSettings.Vibration);ShowVibration(GameAudioSettings.Vibration);
        }
        void ShowVibration(bool enabled)
        {
            var v=View(UIScreen.Settings);var handle=v.Get<RectTransform>("Vibration Toggle/Switch Handle");
            handle.anchoredPosition=v.Get<RectTransform>(enabled?"Vibration Toggle/On Position":"Vibration Toggle/Off Position").anchoredPosition;
            Text(v,"Vibration Toggle/Toggle Hint",enabled?"ON":"OFF");Active(v,"Vibration Toggle/Switch Fill",enabled);
        }
        static string TypeName(EquipmentType t)=>t==EquipmentType.MachineGun?"MG":t==EquipmentType.Accessory?"ACCESSORIES":t.ToString().ToUpperInvariant();
        void MakePortrait()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            portrait=new RenderTexture(512,640,16,RenderTextureFormat.ARGB32);portrait.name="Tank UI Portrait";portrait.Create();
            game.TankCamera.cullingMask&=~(1<<30);
            portraitCamera=new GameObject("Tank Portrait Camera").AddComponent<Camera>();portraitCamera.orthographic=true;portraitCamera.cullingMask=1<<30;
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.targetTexture=portrait;portraitCamera.orthographicSize=5.8f;
            RebuildPortrait();portraitCamera.enabled=false;
        }
        void RebuildPortrait()
        {
            if(portrait==null||portraitCamera==null)return;
            if(portraitTank!=null){portraitTank.SetActive(false);Destroy(portraitTank);}
            portraitTank=Instantiate(game.Artwork.gameObject);portraitTank.name="UI Tank Portrait";portraitTank.transform.position=new Vector3(0,1000,0);portraitTank.transform.localScale=Vector3.one;
            portraitTank.GetComponent<TankArtwork>().ResetVisual();foreach(var b in portraitTank.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            // Prepare hidden weapon parts too, so swapping equipment preserves portrait visibility.
            foreach(var t in portraitTank.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            FramePortrait();portraitCamera.Render();
        }
        void FramePortrait()
        {
            var renderers=portraitTank.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.sqrMagnitude>0).ToArray();
            if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            portraitCamera.orthographicSize=Mathf.Max(bounds.extents.y+.25f,(bounds.extents.x+.25f)/((float)portrait.width/portrait.height));
            portraitCamera.transform.position=bounds.center+Vector3.back*10;
        }
        public void RefreshEquipmentArtwork()
        {
            game.RefreshEquipmentArtwork();
            if(portraitTank==null)return;
            portraitTank.GetComponent<TankArtwork>().EquipMachineGunVisual(game.Equipment.MachineGun);
            portraitTank.GetComponent<TankArtwork>().EquipMortarVisual(game.Equipment.MissilePack);
            foreach(var behaviour in portraitTank.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            FramePortrait();portraitCamera.Render();
        }
        void OnDestroy(){if(game!=null&&game.Progression!=null){game.Progression.Changed-=RefreshProgressionViews;game.Wallet.Changed-=RefreshUpgradeControls;}if(game!=null&&game.Wallet!=null)game.Wallet.Changed-=RefreshCurrencies;if(portraitTank!=null)Destroy(portraitTank);if(portraitCamera!=null)Destroy(portraitCamera.gameObject);if(portrait!=null){portrait.Release();Destroy(portrait);}}
    }
}

