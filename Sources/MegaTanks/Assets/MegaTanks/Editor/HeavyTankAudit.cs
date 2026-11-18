using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class HeavyTankAudit
    {
        static readonly string[] keys={BattleGame.SelectedTankSaveKey,"MegaTanks.Equipment.Armor","MegaTanks.Equipment.Armor.1","MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar",
            "MegaTanks.Accessory.1","MegaTanks.Accessory.2",PlayerWallet.DefaultSaveKey,PlayerProgression.DefaultSaveKey,PlaytestResources.SaveKey,PlaytestResources.ArmorSaveKey};
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static bool errors;
        static int phase,frame;
        static double since;
        static TankDefinition medium,heavy;
        static ArmorDefinition basic,light;
        static EnemyEntity target;
        static float startX,startCamera,startTime;
        static Transform wheel;
        static Quaternion wheelRotation;
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float actual,float expected,string message){Require(Mathf.Abs(actual-expected)<.01f,message+": "+actual+" expected "+expected);}
        static void Next(){phase++;frame=Time.frameCount;since=EditorApplication.timeSinceStartup;}
        public static void Run()
        {
            foreach(var key in keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
            PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");
            PlayerPrefs.DeleteKey("MegaTanks.Equipment.Armor");PlayerPrefs.DeleteKey("MegaTanks.Equipment.Armor.1");
            PlayerPrefs.SetString("MegaTanks.Equipment.MachineGun",AssetDatabase.LoadAssetAtPath<GunDefinition>("Assets/MegaTanks/Data/Weapons/MachineGun.asset").id);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        public static void InstallAndRun(){HeavyTankSetup.Install();Run();}
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
        static UIView Garage(BattleGame game)=>game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
        static void Equip(BattleGame game,ArmorDefinition item,string slot)
        {
            var view=Garage(game);view.Get<Button>(slot).onClick.Invoke();
            var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(t=>t.gameObject.activeSelf&&t.name=="Item "+item.name);
            cell.GetComponent<Button>().onClick.Invoke();view.Get<Button>("Equip Item").onClick.Invoke();
        }
        static void Capture(string name)
        {Directory.CreateDirectory("Checks/HeavyTank");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/HeavyTank/"+name+".png"));}
        static void Check()
        {
            try
            {
                if(phase>0&&(Time.frameCount<=frame+1||EditorApplication.timeSinceStartup-since<.25))return;
                var game=BattleGame.Instance;var ui=game.UI;
                switch(phase)
                {
                    case 0:
                        medium=game.Frontend.tanks.Single(tank=>tank.id=="tank.medium");heavy=game.Frontend.tanks.Single(tank=>tank.id=="tank.heavy");
                        basic=game.Frontend.inventory.OfType<ArmorDefinition>().Single(armor=>armor.id=="armor.basic");
                        light=game.Frontend.inventory.OfType<ArmorDefinition>().Single(armor=>armor.id=="armor.light");
                        Require(game.TankProfile==medium&&medium.displayName=="Medium Tank"&&heavy.displayName=="Heavy Tank","Startup must honor the saved tank and both tank names.");
                        Near(heavy.stats.maxHull,medium.stats.maxHull*2,"Heavy base HP must be double Medium");Near(heavy.stats.advanceSpeed,.25f,"Heavy base speed");
                        ui.OpenEquipment();Garage(game).Get<Button>("Tank Selection").onClick.Invoke();
                        Require(game.TankProfile==heavy&&PlayerPrefs.GetString(BattleGame.SelectedTankSaveKey)==heavy.id,"Garage button must select and save Heavy Tank.");
                        Require(game.Artwork.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.Contains("_wheel_"))==4,"Heavy Tank needs four independent road wheels.");
                        Require(game.Equipment.ArmorSlotCount==2&&game.Equipment.AccessorySlotCount==1,"Heavy needs two armor slots and one accessory.");
                        Require(Garage(game).Get<Text>("Slot Accessory 3/Slot Description").text.StartsWith("Armor 2"),"Second armor must replace the last accessory slot.");
                        Near(game.MaxHull,heavy.stats.maxHull,"Unarmored Heavy HP");
                        Equip(game,basic,"Slot Armor");Equip(game,light,"Slot Accessory 3");
                        Require(game.Equipment.Armor==basic&&game.Equipment.GetEquipped(EquipmentType.Armor,1)==light,"Both armor plates must equip independently through the real Garage UI.");
                        Near(game.MaxHull,heavy.stats.maxHull+game.Progression.ArmorHP(basic)+game.Progression.ArmorHP(light),"Both armor HP bonuses must apply");
                        Near(game.BaseTankStats.armor,heavy.stats.armor+basic.armor+light.armor,"Both defense bonuses must apply");
                        Near(game.TankAdvanceSpeed,heavy.stats.advanceSpeed+basic.speed+light.speed,"Both speed bonuses must apply");
                        var reload=new PlayerEquipment(game.StartingLoadout,game.Frontend);reload.ConfigureTank(heavy);
                        Require(reload.Armor==basic&&reload.SecondArmor==light,"Both armor slots must survive loadout reload.");
                        var portrait=(GameObject)typeof(CanvasGameUI).GetField("portraitTank",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
                        Require(portrait.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="11_front_hull_frame")&&portrait.GetComponentsInChildren<MonoBehaviour>(true).All(b=>!b.enabled),"Heavy garage portrait must show the correct artwork with gameplay disabled.");
                        Next();break;
                    case 1:
                        Capture("HeavyGarage");Next();break;
                    case 2:
                        ui.ClosePopup();ui.Play();StageProgressionAudit.DisableWeapons(game);game.SetStance(TankStance.Defensive);
                        Require(!game.SelectTank(medium)&&game.TankProfile==heavy,"Tank switching must be blocked during a run.");
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                        target=game.SpawnEnemy(EnemyKind.Ground,(Vector2)game.Artwork.transform.position+new Vector2(5,1));target.enabled=false;target.Weapon.enabled=false;
                        game.MachineGun.Stats.damage=game.MainGun.Stats.damage=game.Mortar.Stats.damage=0;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");game.Mortar.SendMessage("Update");
                        Require(game.MachineGun.ShotsFired>0&&game.MainGun.ShotsFired>0&&game.Mortar.ShotsFired>0,"Heavy MG, cannon and mortar must all aim and fire real projectiles.");
                        Require(game.Artwork.MachineGun.EffectPlaying&&game.Artwork.MainGun.EffectPlaying,"Heavy gun muzzle effects must be attached.");
                        Near(game.Hull,game.MaxHull,"Battle starts with armored Heavy HP");
                        Next();break;
                    case 3:
                        Capture("HeavyBattle");
                        target.transform.position=(Vector2)game.Artwork.transform.position+Vector2.right*50;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");
                        Require(Quaternion.Angle(game.Artwork.MainGun.transform.localRotation,Quaternion.identity)<.001f,"Heavy cannon must return to rest outside target range.");
                        startX=game.Artwork.transform.position.x;startCamera=game.CameraPan.X;startTime=Time.time;
                        wheel=game.Artwork.GetComponentsInChildren<Transform>().First(t=>t.name.Contains("_wheel_"));wheelRotation=wheel.localRotation;
                        game.SetStance(TankStance.Aggressive);Next();break;
                    case 4:
                        float advanced=game.Artwork.transform.position.x-startX;
                        Near(advanced,game.TankAdvanceSpeed*(Time.time-startTime),"Heavy movement must use its configured armored speed");
                        Require(advanced>0&&Quaternion.Angle(wheelRotation,wheel.localRotation)>.01f,"Heavy road wheels must animate while advancing.");
                        Near(game.CameraPan.X-startCamera,advanced,"Camera must follow Heavy Tank");
                        game.ReturnHome();ui.OpenEquipment();Garage(game).Get<Button>("Tank Selection").onClick.Invoke();
                        Require(game.TankProfile==medium&&game.Equipment.ArmorSlotCount==1&&game.Equipment.AccessorySlotCount==2,"Switching back must restore Medium's slot layout.");
                        Require(!game.Equipment.IsEquipped(light)&&game.Equipment.SecondArmor==light,"Hidden second armor must be retained without contributing to Medium.");
                        Near(game.MaxHull,medium.stats.maxHull+game.Progression.ArmorHP(basic),"Medium HP must exclude Heavy's second armor");
                        Require(!game.Equipment.Equip(light,EquipmentType.Armor,1),"Medium must reject equipping a second armor slot.");
                        Require(Garage(game).Get<Text>("Slot Accessory 3/Slot Description").text.StartsWith("Accessory 2"),"Medium's second accessory slot must be restored.");
                        Next();break;
                    case 5:
                        Capture("MediumGarage");
                        Next();break;
                    case 6:
                        Garage(game).Get<Button>("Tank Selection").onClick.Invoke();
                        Require(game.TankProfile==heavy&&game.Equipment.SecondArmor==light,"Switching back to Heavy must retain the second armor.");
                        Require(!errors,"Tank selection must have no runtime errors.");
                        Debug.Log("HEAVY_TANK_AUDIT_OK: garage selection/portrait, doubled base HP, 0.25 speed, two distinct armor bonuses and persistence, Medium slot restoration, real three-weapon fire, idle pose, wheels, camera follow and in-run selection guard.");Finish(0);break;
                }
                if(EditorApplication.timeSinceStartup-since>30)throw new Exception("Heavy Tank audit timed out.");
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1);}
        }
        static void Finish(int code)
        {
            foreach(var key in keys){if(saves.TryGetValue(key,out var value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();
            EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
