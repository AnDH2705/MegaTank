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
            ui.layout.HideScreens();ui.MakePortrait();ui.BindHome();ui.BindHud();ui.BindSettings();
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
            lastSafe=safe;lastSize=new Vector2(Screen.width,Screen.height);
        }
        public void ChangeStage(int delta){int count=game.Frontend?.stages?.Length??1;stageIndex=Mathf.Clamp(stageIndex+delta,0,Mathf.Max(0,count-1));RefreshStage();}
        void RefreshStage()
        {
            var v=View(UIScreen.Home);var stages=game.Frontend?.stages;
            string title=stages!=null&&stages.Length>0?stages[stageIndex].stageName:"Stage 1";
            Text(v,"Stage Name Plate/Selected Stage",title);Text(v,"Stage Swipe Area/Stage Count","ROUND 1 / "+(stages!=null&&stages.Length>0?stages[stageIndex].rounds:10));Text(v,"Stage Swipe Area/Route Name",title);
            v.Get<Button>("Previous Stage").interactable=stageIndex>0;v.Get<Button>("Next Stage").interactable=stageIndex<(stages?.Length??1)-1;
        }
        public void Play(){ClosePopup();if(game.Frontend?.stages?.Length>0)game.SelectStage(game.Frontend.stages[stageIndex]);game.StartBattle();screenKey="";Refresh();UpdateHud();}
        public void OpenEquipment(){activeSlot=null;filter=null;selected=null;OpenPopup("Equipment");}
        public void OpenPopup(string value)
        {
            ClosePopup();popup=value;
            UIScreen screen=value=="Equipment"?UIScreen.Equipment:value=="Settings"?UIScreen.Settings:value=="Crew"?UIScreen.Crew:UIScreen.Tech;
            var v=View(screen);v.gameObject.SetActive(true);
            Click(v,"Close Popup",ClosePopup);
            if(value=="Equipment")DrawEquipment();
            else { Click(v,"Dismiss Backdrop",ClosePopup);if(value=="Settings")SyncSettings(); }
        }
        public void ClosePopup()
        {
            popup="";if(layout==null)return;
            foreach(var s in new[]{UIScreen.Equipment,UIScreen.Settings,UIScreen.Crew,UIScreen.Tech,UIScreen.LeaveRun})View(s).gameObject.SetActive(false);
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
            portraitTank=Instantiate(game.Artwork.gameObject);portraitTank.name="UI Tank Portrait";portraitTank.transform.position=new Vector3(0,1000,0);portraitTank.transform.localScale=Vector3.one;
            portraitTank.GetComponent<TankArtwork>().ResetVisual();foreach(var b in portraitTank.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
            foreach(var t in portraitTank.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            game.TankCamera.cullingMask&=~(1<<30);
            portraitCamera=new GameObject("Tank Portrait Camera").AddComponent<Camera>();portraitCamera.orthographic=true;portraitCamera.cullingMask=1<<30;
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.targetTexture=portrait;portraitCamera.orthographicSize=5.8f;
            FramePortrait();portraitCamera.Render();portraitCamera.enabled=false;
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
            foreach(var behaviour in portraitTank.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            FramePortrait();portraitCamera.Render();
        }
        void OnDestroy(){if(portraitTank!=null)Destroy(portraitTank);if(portraitCamera!=null)Destroy(portraitCamera.gameObject);if(portrait!=null){portrait.Release();Destroy(portrait);}}
    }
}
