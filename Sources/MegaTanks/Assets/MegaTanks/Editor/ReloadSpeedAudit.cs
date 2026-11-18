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
    public static class ReloadSpeedAudit
    {
        static StageRewardSaveScope scope;
        static readonly string[] keys={BattleGame.SelectedTankSaveKey,"MegaTanks.Accessory.1","MegaTanks.Accessory.2"};
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static int phase,frame;
        static double since;
        static bool errors;
        static string cannonBefore;
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float value,float expected,string message){Require(Mathf.Abs(value-expected)<.0001f,message+": "+value+" expected "+expected);}
        static float Field(object owner,string name)=>(float)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        static void Next(){phase++;frame=Time.frameCount;since=EditorApplication.timeSinceStartup;}
        public static void Run()
        {
            scope=new StageRewardSaveScope();foreach(var key in keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
            PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");PlayerPrefs.DeleteKey("MegaTanks.Accessory.1");PlayerPrefs.DeleteKey("MegaTanks.Accessory.2");PlayerPrefs.Save();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
        static void Check()
        {
            try
            {
                if(phase>0&&(Time.frameCount<=frame+1||EditorApplication.timeSinceStartup-since<.3))return;
                var game=BattleGame.Instance;
                switch(phase)
                {
                    case 0:
                        Near(new GunStats().reloadSpeed,100,"New gun reload speed");Near(new MortarStats().reloadSpeed,100,"New mortar reload speed");Near(new EnemyWeaponStats().reloadSpeed,100,"New enemy reload speed");
                        Near(WeaponCombatStats.CalculateReloadTime(4,0),4,"Zero speed");Near(WeaponCombatStats.CalculateReloadTime(4,100),2,"Default speed formula");Near(WeaponCombatStats.CalculateReloadTime(4,150),1.6f,"Custom speed formula");
                        var growth=new EnemyWeaponStats {reloadTime=4,reloadSpeed=100,reloadSpeedGrow=5}.AtDifficulty(2);
                        Near(growth.reloadSpeed,110,"Enemy reload speed growth");Near(growth.EffectiveReloadTime,4/2.1f,"Enemy growth must use the same formula");
                        cannonBefore=JsonUtility.ToJson(game.Equipment.Cannon);
                        game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                        var target=game.SpawnEnemy(EnemyKind.Ground,(Vector2)game.Artwork.transform.position+new Vector2(5,1));target.enabled=target.Weapon.enabled=false;
                        game.MachineGun.Stats.salvoSize=game.MainGun.Stats.salvoSize=1;
                        game.MachineGun.Stats.damage=game.MainGun.Stats.damage=game.Mortar.Stats.damage=target.Weapon.Stats.damage=0;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");game.Mortar.SendMessage("Update");
                        Require(game.MachineGun.ShotsFired==1&&game.MainGun.ShotsFired==1&&game.Mortar.ShotsFired==1,"All player weapons must fire.");
                        Near(Field(game.MachineGun,"cooldown"),game.MachineGun.Stats.EffectiveReloadTime,"Actual MG cooldown");
                        Near(Field(game.MainGun,"cooldown"),game.MainGun.Stats.EffectiveReloadTime,"Actual cannon cooldown");
                        Near(Field(game.Mortar,"cooldown"),game.Mortar.Stats.EffectiveReloadTime,"Actual mortar cooldown");
                        Near(game.MainGun.Stats.reloadTime,game.Equipment.Cannon.stats.reloadTime,"Base time must remain unchanged in battle");
                        target.Weapon.Stats.salvoSize=1;target.Weapon.SendMessage("Update");Require(target.Weapon.ShotsFired==1,"Enemy must fire for cooldown validation.");
                        Near(Field(target.Weapon,"cooldown"),target.Weapon.Stats.EffectiveReloadTime,"Actual enemy cooldown");
                        game.Stage.Current.type=StageNodeType.Boss;var boss=game.SpawnBoss();Require(boss!=null,"Boss fixture must spawn.");
                        boss.enabled=boss.Boss.enabled=false;var tank=boss.Boss;
                        boss.transform.position+=(Vector3)game.TankAimPoint+Vector3.right*4-tank.Artwork.MainGun.Muzzle.position;
                        tank.MachineGunStats.range=tank.CannonStats.range=tank.MortarStats.range=80;
                        tank.MachineGunStats.damage=tank.CannonStats.damage=tank.MortarStats.damage=0;
                        tank.MachineGunStats.salvoSize=tank.CannonStats.salvoSize=1;
                        tank.MachineGunStats.reloadSpeed=150;tank.CannonStats.reloadSpeed=75;tank.MortarStats.reloadSpeed=0;
                        tank.SendMessage("Update");Require(tank.ShotsFired==3,"All three boss weapons must fire for cooldown validation.");
                        Near(Field(tank,"mgCooldown"),tank.MachineGunStats.EffectiveReloadTime,"Actual boss MG cooldown");
                        Near(Field(tank,"cannonCooldown"),tank.CannonStats.EffectiveReloadTime,"Actual boss cannon cooldown");
                        Near(Field(tank,"mortarCooldown"),tank.MortarStats.EffectiveReloadTime,"Actual boss mortar cooldown");
                        game.ReturnHome();
                        var loader=game.Frontend.inventory.OfType<AccessoryDefinition>().Single(item=>item.id=="accessory.autoloader");
                        Require(game.Equipment.Equip(loader,EquipmentType.Accessory),"Auto Loader must equip.");game.UI.RefreshEquipmentArtwork();game.RefreshEquipmentArtwork();
                        float bonus=loader.Bonus(game.Progression.Get(loader).Level);
                        Near(game.MainGun.ReloadSpeed,game.Equipment.Cannon.stats.reloadSpeed+bonus,"Auto Loader must add to the weapon speed stat");
                        Near(game.MainGun.ReloadTime,WeaponCombatStats.CalculateReloadTime(game.Equipment.Cannon.stats.reloadTime,game.Equipment.Cannon.stats.reloadSpeed+bonus),"Auto Loader must apply the total-speed formula once");
                        Require(JsonUtility.ToJson(game.Equipment.Cannon)==cannonBefore,"Runtime calculations must not mutate weapon definitions.");
                        game.UI.OpenEquipment();var view=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
                        view.Get<Button>("Slot Cannon").onClick.Invoke();
                        var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(child=>child.gameObject.activeSelf&&child.name=="Item "+game.Equipment.Cannon.name);
                        cell.GetComponent<Button>().onClick.Invoke();
                        Require(view.Get<Text>("Item Stats").text.Contains("BASE RELOAD TIME")&&view.Get<Text>("Item Stats").text.Contains("BATTLE RELOAD"),"Garage must distinguish base time, speed and battle reload.");
                        Next();break;
                    case 1:
                        Directory.CreateDirectory("Checks/ReloadSpeed");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/ReloadSpeed/Garage.png"));Next();break;
                    case 2:
                        Require(!errors,"Reload speed audit must have no runtime errors.");
                        Debug.Log("RELOAD_SPEED_AUDIT_OK: 100% defaults, 0/100/150% formula, enemy growth, actual player/enemy/all-three-boss cooldowns, preserved base times, additive Auto Loader, garage labels and immutable source definitions.");Finish(0);break;
                }
                if(EditorApplication.timeSinceStartup-since>30)throw new Exception("Reload speed audit timed out.");
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1);}
        }
        static void Finish(int code)
        {
            foreach(var key in keys){if(saves.TryGetValue(key,out var value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}scope?.Dispose();PlayerPrefs.Save();
            EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
