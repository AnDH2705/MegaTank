using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MegaTanks.Editor
{
    public static class CanvasUIAudit
    {
        static int step;
        static double since;
        static bool errors,capture;
        static float sound,music;
        static bool vibration;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(){Begin(false);}
        public static void Capture(){Begin(true);}
        static void Begin(bool screenshots)
        {
            capture=screenshots;EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            if(capture){var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();}
            EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;step=0;errors=false;since=EditorApplication.timeSinceStartup;
            sound=GameAudioSettings.Sound;music=GameAudioSettings.Music;vibration=GameAudioSettings.Vibration;
            Application.logMessageReceived+=Log;EditorApplication.update+=Check;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static Button Find(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.name==name&&b.gameObject.activeInHierarchy);
        static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();var button=Find(name);Require(button.interactable,name+" must be interactable.");
            var r=button.GetComponent<RectTransform>();var local=name=="Dismiss Backdrop"?new Vector2(r.rect.xMin+8,r.rect.yMax-8):r.rect.center;
            var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Raycast must hit "+name+" rather than another panel.");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static void Shot(string name){if(!capture)return;System.IO.Directory.CreateDirectory("Checks/CanvasUI");ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Checks/CanvasUI/"+name+".png"));}
        static void Next(){step++;since=EditorApplication.timeSinceStartup;}
        static void Check()
        {
            if(EditorApplication.timeSinceStartup-since<.5)return;
            try
            {
                var game=BattleGame.Instance;Require(game!=null&&game.UI!=null,"Canvas UI must initialize.");var ui=game.UI;
                switch(step)
                {
                    case 0:
                        Require(!game.Deployed&&!game.Running,"Home must not start a battle.");Require(game.Frontend.inventory.All(i=>i.Type!=EquipmentType.Accessory&&i.Type!=EquipmentType.Armor),"No invented accessory/armor items.");
                        Require(ui.GetComponent<Canvas>()!=null&&ui.GetComponent<GraphicRaycaster>()!=null,"Use Canvas and uGUI raycasts.");
                        Require(ui.Skin!=null&&Find("Play").image.sprite==ui.Skin.buttonYellow,"Home must use the Layer Lab skin and gold Play button.");
                        Require(ui.Skin.buttonBlue.border!=Vector4.zero&&ui.Skin.panel.border!=Vector4.zero,"Scalable source borders must be preserved.");Shot("Home");Next();break;
                    case 1:Click("Avatar Menu");Next();break;
                    case 2:
                        Require(ui.Popup=="Settings","Avatar opens settings.");
                        Require(UnityEngine.Object.FindObjectsOfType<Slider>().All(s=>s.GetComponent<Image>().sprite==ui.Skin.sliderBackground),"Settings sliders must use pack artwork.");
                        UnityEngine.Object.FindObjectsOfType<Slider>().First(s=>s.name=="Sound Slider").value=.31f;
                        UnityEngine.Object.FindObjectsOfType<Slider>().First(s=>s.name=="Music Slider").value=.47f;
                        UnityEngine.Object.FindObjectOfType<Toggle>().isOn=!vibration;
                        Require(Mathf.Abs(GameAudioSettings.Sound-.31f)<.001f&&Mathf.Abs(GameAudioSettings.Music-.47f)<.001f&&GameAudioSettings.Vibration!=vibration,"Settings controls persist independently.");Shot("Settings");Next();break;
                    case 3:Click("Dismiss Backdrop");Require(ui.Popup=="","Outside click closes popup.");Click("Equipment");Next();break;
                    case 4:
                        Require(ui.InventoryVisibleCount==game.Frontend.inventory.Length,"Inventory shows available equipment.");
                        Require(game.Equipment.Accessories.Length==2&&!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.name=="Slot Accessory 1"),"Mortar replaces only accessory slot 1.");
                        foreach(var item in game.Frontend.inventory)
                        {
                            var cell=Find("Item "+item.name);var indicator=cell.transform.Find("Equipped Indicator");
                            Require((indicator!=null)==game.Equipment.IsEquipped(item),"Inventory equipped indicators must match the loadout.");
                            Require(cell.image.sprite==ui.Skin.itemFrame&&cell.image.type==Image.Type.Sliced,"Inventory uses scalable pack item frames.");
                            if(indicator!=null)Require(indicator.GetComponent<Image>().sprite==ui.Skin.check,"Equipped marker uses pack check icon.");
                        }
                        foreach(var slot in new[]{EquipmentType.Mortar,EquipmentType.MachineGun,EquipmentType.Cannon})
                        {
                            string name=slot==EquipmentType.MachineGun?"MG":slot.ToString();
                            var item=game.Equipment.GetEquipped(slot);
                            Require(item.icon!=null&&Find("Slot "+name).transform.Find("Equipped Item Icon").GetComponent<Image>().sprite==item.icon,"Slot must show its equipped item's icon.");
                        }
                        Shot("Equipment");Next();break;
                    case 5:Click("Slot Mortar");Next();break;
                    case 6:Require(ui.Filter==EquipmentType.Mortar&&ui.InventoryVisibleCount==1,"Mortar slot filters its inventory.");Click("Item "+game.Equipment.Mortar.name);Next();break;
                    case 7:Click("Equip Item");Next();break;
                    case 8:Click("Slot Armor");Next();break;
                    case 9:Require(ui.Filter==EquipmentType.Armor&&ui.InventoryVisibleCount==0,"Empty armor filter.");Click("Inventory Filter");Next();break;
                    case 10:Click("Filter Accessory");Next();break;
                    case 11:Require(ui.Filter==EquipmentType.Accessory&&ui.InventoryVisibleCount==0,"Empty accessory filter.");Click("Slot MG");Next();break;
                    case 12:Click("Item "+game.Equipment.MachineGun.name);Next();break;
                    case 13:Click("Equip Item");Next();break;
                    case 14:Click("Close Popup");Click("Play");Next();break;
                    case 15:
                        Require(game.Running&&game.Stage.Mode==StageMode.Combat,"Play starts first battle node.");
                        Require(game.Mortar.Definition==game.Equipment.Mortar,"Battle uses the equipped mortar definition.");StageProgressionAudit.DisableWeapons(game);Shot("Battle");Click("Pause");Next();break;
                    case 16:Require(game.Paused&&!game.Running,"Pause stops combat.");Click("Battle Settings");Next();break;
                    case 17:Click("Close Popup");Click("Resume");Require(!game.Paused,"Resume preserves run.");
                        foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())enemy.Damage(99999);Next();break;
                    case 18:Require(game.Rewards.IsOpen,"Victory opens reward cards.");Shot("Rewards");Next();break;
                    case 19:Click("Action");Next();break;
                    case 20:Require(game.Stage.Mode==StageMode.Map,"Reward opens route map.");Shot("Map");Next();break;
                    case 21:
                        int id=game.Stage.Current.next[0];Click("Node "+id);Next();break;
                    case 22:
                        game.ReturnHome();Next();break;
                    case 23:
                        Require(!game.Deployed&&!game.Running&&game.Stage.Mode==StageMode.Inactive,"Home clears encounter mode.");
                        Require(!errors,"No runtime errors permitted.");Debug.Log("MEGATANKS_CANVAS_OK: real uGUI raycasts, Home/settings/equipment/filter/equip/Play, pause/resume, combat/reward/map/node, Home, persisted settings, no armor/accessory instances.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            GameAudioSettings.Set(GameAudioChannel.Sound,sound);GameAudioSettings.Set(GameAudioChannel.Music,music);GameAudioSettings.SetVibration(vibration);
            EditorApplication.update-=Check;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}

