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
        static int step,routeBackStep,equipmentSortStep;
        static double since;
        static bool errors,capture,homeOnly,gatlingOnly,routeOnly,equipmentOnly,hadSavedMG;
        static string savedMG;
        static EnemyEntity firstEnemy;
        static float enemyStartX;
        static float sound,music;
        static bool vibration;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(){Begin(false);}
        public static void Capture(){Begin(true);}
        public static void CaptureHome(){homeOnly=true;Begin(true);}
        public static void CaptureGatling(){gatlingOnly=true;Begin(true);}
        public static void CaptureRoute(){routeOnly=true;Begin(true);}
        public static void CaptureEquipment(){equipmentOnly=true;Begin(true);}
        static void Begin(bool screenshots)
        {
            capture=screenshots;EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            if(capture){var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();}
            EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;step=routeBackStep=equipmentSortStep=0;errors=false;since=EditorApplication.timeSinceStartup;
            sound=GameAudioSettings.Sound;music=GameAudioSettings.Music;vibration=GameAudioSettings.Vibration;
            hadSavedMG=PlayerPrefs.HasKey("MegaTanks.Equipment.MachineGun");savedMG=PlayerPrefs.GetString("MegaTanks.Equipment.MachineGun","");
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
        static void Shot(string name){if(!capture||(homeOnly&&name!="Home")||(gatlingOnly&&name!="Gatling")||(routeOnly&&name!="Map")||(equipmentOnly&&!name.EndsWith("Equipment")))return;System.IO.Directory.CreateDirectory("Checks/CanvasUI");ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Checks/CanvasUI/"+name+".png"));}
        static void CheckRoute(BattleGame game)
        {
            var ui=game.UI;var skin=ui.RouteSkin;var stage=game.Stage;
            Require(skin!=null&&skin.legend.border!=Vector4.zero,"Route skin must preserve the supplied sprite borders.");
            Require(ui.transform.Find("Safe Area/Encounter Screens/Route/Radar Route Background").GetComponent<Image>().sprite==skin.background,"Map must use the radar background.");
            foreach(var node in stage.Route.rounds.SelectMany(column=>column))
            {
                var button=Find("Node "+node.id);bool current=node==stage.Current,available=stage.CanSelect(node.id);
                var frame=current?skin.nodeCurrent:available?skin.nodeAvailable:node.completed?skin.nodeNormal:skin.nodeLocked;
                Require(button.image.sprite==frame&&button.interactable==available,"Node artwork and click availability must match progression.");
                var type=node.type==StageNodeType.Random&&!node.entered?StageNodeType.Random:node.EncounterType;
                Require(button.transform.Find("Route Node Icon").GetComponent<Image>().sprite==skin.IconFor(type),"Node icons must match types without exposing unknown nodes.");
            }
            var scroll=UnityEngine.Object.FindObjectsOfType<ScrollRect>().Single(s=>s.name=="Route Scroll");
            Require(scroll.horizontal&&!scroll.vertical,"The route must retain horizontal scrolling.");
            Require(scroll.content.GetComponentsInChildren<Image>().Count(i=>i.name=="Route Connection")==stage.Route.rounds.SelectMany(c=>c).Sum(n=>n.next.Count),"Every procedural route edge must still be drawn.");
            Require(scroll.content.Find("Current Player Tank").GetComponent<Image>().sprite==skin.playerTank,"The supplied tank marker must mark the current node.");
            Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=0;
            var start=RectTransformUtility.WorldToScreenPoint(null,scroll.viewport.TransformPoint(scroll.viewport.rect.center));
            foreach(float delta in new[]{-1200f,1200f})
            {
                var data=new PointerEventData(EventSystem.current){position=start,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(scroll.gameObject,data,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(scroll.gameObject,data,ExecuteEvents.beginDragHandler);
                data.position=start+Vector2.right*delta;data.delta=Vector2.right*delta;
                ExecuteEvents.Execute(scroll.gameObject,data,ExecuteEvents.dragHandler);ExecuteEvents.Execute(scroll.gameObject,data,ExecuteEvents.endDragHandler);
                Require(delta<0?scroll.horizontalNormalizedPosition>.99f:scroll.horizontalNormalizedPosition<.01f,"Dragging must reach both route scroll limits.");
            }
        }
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
                        Require(game.Artwork.transform.position==game.TankSpawnPosition&&Vector3.Distance(game.TankSpawnPosition,new Vector3(-7.3f,-4.483333f,0))<.001f&&game.Artwork.transform.localScale==Vector3.one*.75f,"Home must preserve the approved lower spawn and 0.75 tank scale.");
                        Require(ui.GetComponent<Canvas>()!=null&&ui.GetComponent<GraphicRaycaster>()!=null,"Use Canvas and uGUI raycasts.");
                        Require(ui.Skin!=null&&Find("Play").transform.Find("Label").GetComponent<Text>().text=="PLAY"&&Find("Play").image.sprite==ui.Skin.homeYellowPanel,"Home must use the supplied yellow Play artwork.");
                        Require(Find("Avatar Menu").image.sprite==ui.Skin.homeMenu&&Find("Equipment").image.sprite==ui.Skin.homeButtonBase,"Home menu and icon buttons must use the supplied artwork.");
                        Require(Find("Equipment").transform.Find("Home Icon").GetComponent<Image>().sprite==ui.Skin.homeGarage&&Find("Crew").transform.Find("Home Icon").GetComponent<Image>().sprite==ui.Skin.homeCrew&&Find("Tech").transform.Find("Home Icon").GetComponent<Image>().sprite==ui.Skin.homeTech,"Garage, Crew and Tech icons must match the reference.");
                        Require(ui.transform.Find("Safe Area/Frontend Screens/Home/Stage Swipe Area").GetComponent<Image>().sprite==ui.Skin.homeStageSelector,"The stage selector must use the supplied combined arrow panel.");
                        Require(ui.transform.Find("Safe Area/Frontend Screens/Home/Background")==null,"Home must show the live battlefield.");
                        Require(Find("Equipment").GetComponent<RectTransform>().anchoredPosition.x>1500&&Find("Play").GetComponent<RectTransform>().anchoredPosition.y< -750,"Garage and Start must occupy the right column.");
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
                        Require(ui.EquipmentSkin!=null&&ui.transform.Find("Safe Area/Popup Layer/Equipment/Equipment Background").GetComponent<Image>().sprite==ui.EquipmentSkin.background,"Equipment must use the supplied garage background.");
                        Require(Find("Close Popup").image.sprite==ui.EquipmentSkin.back&&Find("Slot Armor").transform.Find("Empty Slot Icon").GetComponent<Image>().sprite==ui.EquipmentSkin.plus,"Equipment must use the supplied Back and empty-slot artwork.");
                        Require(game.Equipment.Accessories.Length==2&&!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.name=="Slot Accessory 1"),"Mortar replaces only accessory slot 1.");
                        foreach(var item in game.Frontend.inventory)
                        {
                            var cell=Find("Item "+item.name);var indicator=cell.transform.Find("Equipped Indicator");
                            Require((indicator!=null&&indicator.gameObject.activeSelf)==game.Equipment.IsEquipped(item),"Inventory equipped indicators must match the loadout.");
                            Require(cell.image.sprite==ui.EquipmentSkin.itemCard,"Inventory uses the supplied orange item cards.");
                            if(indicator!=null)Require(indicator.GetComponent<Image>().sprite==ui.EquipmentSkin.equippedBadge,"Equipped marker uses the supplied tick.");
                        }
                        foreach(var slot in new[]{EquipmentType.Mortar,EquipmentType.MachineGun,EquipmentType.Cannon})
                        {
                            string name=slot==EquipmentType.MachineGun?"MG":slot.ToString();
                            var item=game.Equipment.GetEquipped(slot);
                            Require(item.icon!=null&&Find("Slot "+name).transform.Find("Equipped Item Icon").GetComponent<Image>().sprite==item.icon,"Slot must show its equipped item's icon.");
                        }
                        Shot("Equipment");Next();break;
                    case 5:
                        if(equipmentSortStep==0){Click("Sort Direction");equipmentSortStep=1;since=EditorApplication.timeSinceStartup;break;}
                        if(equipmentSortStep==1){Click("Inventory Sort");equipmentSortStep=2;since=EditorApplication.timeSinceStartup;break;}
                        Require(Find("Sort Direction").transform.Find("Label").GetComponent<Text>().text=="DESC"&&Find("Inventory Sort").transform.Find("Label").GetComponent<Text>().text.Contains("TYPE"),"Sort direction and order controls must remain clickable and update their labels.");
                        Click("Slot Mortar");Next();break;
                    case 6:
                        Require(ui.Filter==EquipmentType.Mortar&&ui.InventoryVisibleCount==1,"Mortar slot filters its inventory.");
                        Require(Find("Slot Mortar").image.sprite==ui.EquipmentSkin.slotSelected,"The selected equipment slot must use the yellow frame.");
                        Click("Item "+game.Equipment.Mortar.name);Shot("MortarEquipment");Next();break;
                    case 7:Click("Equip Item");Next();break;
                    case 8:Click("Slot Armor");Next();break;
                    case 9:Require(ui.Filter==EquipmentType.Armor&&ui.InventoryVisibleCount==0,"Empty armor filter.");Click("Inventory Filter");Next();break;
                    case 10:Click("Filter Accessory");Next();break;
                    case 11:Require(ui.Filter==EquipmentType.Accessory&&ui.InventoryVisibleCount==0,"Empty accessory filter.");Click("Slot MG");Next();break;
                    case 12:Click("Item GatlingGun");Next();break;
                    case 13:
                        Click("Equip Item");
                        Require(game.Equipment.MachineGun.name=="GatlingGun"&&game.Artwork.MachineGun.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.sprite==game.Equipment.MachineGun.icon),"Gatling equip must change the live roof weapon.");
                        Require(Find("Slot MG").transform.Find("Equipped Item Icon").GetComponent<Image>().sprite==game.Equipment.MachineGun.icon,"MG slot must show the Gatling icon.");
                        Require(Find("Item GatlingGun").transform.Find("Equipped Indicator")!=null,"Gatling must show its equipped badge.");
                        Require(new PlayerEquipment(game.StartingLoadout,game.Frontend).MachineGun==game.Equipment.MachineGun,"Gatling selection must persist.");
                        Shot("GatlingEquipment");Next();break;
                    case 14:
                        Click("Close Popup");var homeCamera=game.TankCamera.transform.position;int homeBackdrop=game.Backgrounds.CurrentIndex;
                        Click("Play");
                        Require(game.Artwork.transform.position==game.TankSpawnPosition&&game.Artwork.transform.localScale==Vector3.one*.75f,"Start must retain the approved tank position and scale.");
                        Require(!ui.transform.Find("Safe Area/Frontend Screens/Home").gameObject.activeSelf&&game.Running&&game.EnemyCount>0,"Start must hide home and spawn a running wave in the same click.");
                        Require(game.TankCamera.transform.position==homeCamera&&game.Backgrounds.CurrentIndex==homeBackdrop,"Start must preserve the live home camera and backdrop.");
                        firstEnemy=UnityEngine.Object.FindObjectsOfType<EnemyEntity>().First();enemyStartX=firstEnemy.transform.position.x;
                        Require(UnityEngine.Object.FindObjectsOfType<EnemyEntity>().All(e=>e.transform.position.x>game.Artwork.transform.position.x&&e.Velocity.x<0),"All enemies must spawn ahead on the right and immediately advance left.");
                        var probe=game.SpawnEnemy(EnemyKind.Flyer,(Vector2)game.Artwork.MachineGun.transform.position+Vector2.right*15);
                        probe.enabled=probe.Weapon.enabled=false;
                        Next();break;
                    case 15:
                        Require(game.Running&&game.Stage.Mode==StageMode.Combat,"Play starts first battle node.");
                        Require(game.Stance==TankStance.Aggressive,"New runs must start in Aggressive.");
                        Require(ui.transform.Find("Safe Area/Battle Screens/BattleHUD/Footer")==null&&ui.transform.Find("Safe Area/Battle Screens/BattleHUD/Defensive")==null&&ui.transform.Find("Safe Area/Battle Screens/BattleHUD/Aggressive")==null,"The stance panel must not cover the battlefield.");
                        Require(game.MachineGun.Definition==game.Equipment.MachineGun&&game.MachineGun.Artwork==game.Artwork.MachineGun,"Combat must use the equipped Gatling and its muzzle.");
                        Require(JsonUtility.ToJson(game.MachineGun.Stats)==JsonUtility.ToJson(game.Equipment.MachineGun.stats)&&!ReferenceEquals(game.MachineGun.Stats,game.Equipment.MachineGun.stats),"Gatling must use an independent copy of all current MG stats.");
                        Require(game.MachineGun.ShotsFired>=3&&game.Artwork.MachineGun.Muzzle.position.x>game.Artwork.MachineGun.transform.position.x,"Gatling must fire a salvo from its forward muzzle.");
                        Require(firstEnemy!=null&&firstEnemy.transform.position.x<enemyStartX,"Enemies must actually advance toward the tank from the right.");
                        Require(firstEnemy.Artwork.transform.localScale.x<0,"Enemy artwork must face its leftward approach.");
                        Require(game.Mortar.ShotsFired>0&&game.Mortar.LastLaunchVelocity.x>0,"Mortar must fire forward at the right-side formation.");
                        Require(game.Mortar.Definition==game.Equipment.Mortar,"Battle uses the equipped mortar definition.");StageProgressionAudit.DisableWeapons(game);Shot("Battle");Click("Pause");Next();break;
                    case 16:Require(game.Paused&&!game.Running,"Pause stops combat.");Click("Battle Settings");Next();break;
                    case 17:Click("Close Popup");Click("Resume");Require(!game.Paused,"Resume preserves run.");
                        foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())enemy.Damage(99999);Next();break;
                    case 18:Require(game.Rewards.IsOpen,"Victory opens reward cards.");Shot("Rewards");Next();break;
                    case 19:Click("Action");Next();break;
                    case 20:Require(game.Stage.Mode==StageMode.Map,"Reward opens route map.");CheckRoute(game);Shot("Map");Next();break;
                    case 21:
                        if(routeBackStep==0){Click("Route Back");routeBackStep=1;since=EditorApplication.timeSinceStartup;break;}
                        if(routeBackStep==1)
                        {
                            Require(ui.Popup=="Leave run","Route Back must confirm leaving before discarding a run.");
                            int current=game.Stage.Current.id;Click("Cancel Leave");
                            Require(ui.Popup==""&&game.Stage.Mode==StageMode.Map&&game.Stage.Current.id==current,"Cancel must retain the route and current node.");
                            routeBackStep=2;since=EditorApplication.timeSinceStartup;break;
                        }
                        int id=game.Stage.Current.next[0];Click("Node "+id);Next();break;
                    case 22:
                        game.ReturnHome();Next();break;
                    case 23:
                        Require(!game.Deployed&&!game.Running&&game.Stage.Mode==StageMode.Inactive,"Home clears encounter mode.");
                        Require(game.Artwork.transform.position==game.TankSpawnPosition&&game.Artwork.transform.localScale==Vector3.one*.75f,"Returning Home must restore the lower tank pose.");
                        Shot("Gatling");Next();break;
                    case 24:
                        game.Equipment.Equip(game.StartingLoadout.machineGun,EquipmentType.MachineGun);ui.RefreshEquipmentArtwork();
                        Require(game.Artwork.MachineGun.name=="roof_gun"&&game.MachineGun.Artwork==game.Artwork.MachineGun,"Re-equipping the original MG must restore its artwork and turret binding.");
                        game.StartBattle();
                        Require(game.Artwork.transform.position==game.TankSpawnPosition&&game.Artwork.transform.localScale==Vector3.one*.75f,"Restart must restore the approved tank pose.");
                        Require(!errors,"No runtime errors permitted.");Debug.Log("MEGATANKS_CANVAS_OK: real uGUI raycasts, Home/settings/equipment/filter/equip/Play, pause/resume, combat/reward/map/node, Home, persisted settings, no armor/accessory instances.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            GameAudioSettings.Set(GameAudioChannel.Sound,sound);GameAudioSettings.Set(GameAudioChannel.Music,music);GameAudioSettings.SetVibration(vibration);
            if(hadSavedMG)PlayerPrefs.SetString("MegaTanks.Equipment.MachineGun",savedMG);else PlayerPrefs.DeleteKey("MegaTanks.Equipment.MachineGun");PlayerPrefs.Save();
            EditorApplication.update-=Check;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
