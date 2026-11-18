using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class AccessoryAudit
    {
        static StageRewardSaveScope scope;
        static readonly string[] keys={BattleGame.SelectedTankSaveKey,"MegaTanks.Accessory.1","MegaTanks.Accessory.2"};
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static int phase,frame;
        static double since;
        static bool errors;
        static AccessoryDefinition loader;
        static string definitionBefore,weaponBefore;
        static float reloadMG,reloadCannon,reloadMortar,shotDelay;
        static int reserve;
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float value,float expected,string message){Require(Mathf.Abs(value-expected)<.0001f,message+": "+value+" expected "+expected);}
        static void Next(){phase++;frame=Time.frameCount;since=EditorApplication.timeSinceStartup;}
        public static void Run()
        {
            scope=new StageRewardSaveScope();foreach(var key in keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
            PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");PlayerPrefs.DeleteKey(PlayerProgression.DefaultSaveKey);
            PlayerPrefs.DeleteKey("MegaTanks.Accessory.1");PlayerPrefs.DeleteKey("MegaTanks.Accessory.2");PlayerPrefs.Save();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
        static UIView Garage(BattleGame game)=>game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
        static void SelectLoader(BattleGame game,string slot)
        {
            var view=Garage(game);view.Get<Button>(slot).onClick.Invoke();
            var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(child=>child.gameObject.activeSelf&&child.name=="Item "+loader.name);
            cell.GetComponent<Button>().onClick.Invoke();
        }
        static void EquipLoader(BattleGame game,string slot){SelectLoader(game,slot);Garage(game).Get<Button>("Equip Item").onClick.Invoke();}
        static void CheckReloads(BattleGame game,float bonus)
        {
            Near(game.ReloadSpeedBonus,bonus,"Total reload speed bonus");
            Near(game.MachineGun.ReloadTime,WeaponCombatStats.CalculateReloadTime(reloadMG,game.Equipment.MachineGun.stats.reloadSpeed+bonus),"MG reload must use total speed");
            Near(game.MainGun.ReloadTime,WeaponCombatStats.CalculateReloadTime(reloadCannon,game.Equipment.Cannon.stats.reloadSpeed+bonus),"Cannon reload must use total speed");
            Near(game.Mortar.Stats.EffectiveReloadTime,WeaponCombatStats.CalculateReloadTime(reloadMortar,game.Equipment.Mortar.stats.reloadSpeed+bonus),"Mortar reload must use total speed");
        }
        static void Check()
        {
            try
            {
                if(phase>0&&(Time.frameCount<=frame+1||EditorApplication.timeSinceStartup-since<.3))return;
                var game=BattleGame.Instance;var view=Garage(game);
                switch(phase)
                {
                    case 0:
                        loader=game.Frontend.inventory.OfType<AccessoryDefinition>().Single(item=>item.id=="accessory.autoloader");
                        Require(loader.icon!=null&&loader.displayName=="Auto Loader","Auto Loader and its generated icon must be in the live inventory.");
                        definitionBefore=JsonUtility.ToJson(loader);weaponBefore=JsonUtility.ToJson(game.Equipment.MachineGun);
                        reloadMG=game.Equipment.MachineGun.stats.reloadTime;reloadCannon=game.Equipment.Cannon.stats.reloadTime;reloadMortar=game.Equipment.Mortar.stats.reloadTime;
                        shotDelay=game.MachineGun.ShotDelay;reserve=game.MachineGun.ReserveCapacity;
                        Near(loader.ReloadSpeed(1),10,"Level 1 bonus");Near(loader.ReloadSpeed(2),10.5f,"Level 2 bonus");Near(loader.ReloadSpeed(11),15,"Level growth must use percentage points");
                        game.UI.OpenEquipment();EquipLoader(game,"Slot Accessory 2");CheckReloads(game,10);
                        Require(view.Get<Image>("Slot Accessory 2/Equipped Item Icon").sprite==loader.icon,"The equipped accessory slot must show the generated icon.");
                        Require(PlayerPrefs.GetString("MegaTanks.Accessory.1")==loader.id,"Accessory equipment must save by permanent ID.");
                        Require(view.Get<Text>("Item Stats").text.Contains("10%")&&!view.Get<RectTransform>("Star Up").gameObject.activeSelf,"Garage must show reload bonuses and level-only upgrades.");
                        Next();break;
                    case 1:
                        if(game.Wallet.Parts<100)game.Wallet.TryAdd(CurrencyType.Parts,100);
                        int parts=game.Wallet.Parts;view.Get<Button>("Level Up").onClick.Invoke();
                        Require(game.Progression.Get(loader).Level==2&&game.Wallet.Parts==parts-100,"A real garage level-up must charge Parts and advance one level.");
                        CheckReloads(game,10.5f);
                        Require(!game.Progression.TryStarUp(loader),"Auto Loader must not introduce unspecified star bonuses.");
                        var reload=new PlayerProgression(game.Frontend,game.Wallet);Require(reload.Get(loader).Level==2,"Accessory levels must persist.");
                        var equipment=new PlayerEquipment(game.StartingLoadout,game.Frontend);equipment.ConfigureTank(game.TankProfile);
                        Require(equipment.Accessories[0]==loader,"Accessory selection must persist.");
                        EquipLoader(game,"Slot Accessory 3");CheckReloads(game,10.5f);
                        Require(game.Equipment.Accessories[0]==null&&game.Equipment.Accessories[1]==loader,"Moving an accessory must not duplicate it across slots.");
                        game.UI.ChangeTank();Require(game.TankProfile.id=="tank.heavy","Switch to Heavy Tank for slot masking.");CheckReloads(game,0);
                        Require(!game.Equipment.Equip(loader,EquipmentType.Accessory,1),"Heavy Tank must reject its replaced accessory slot.");
                        EquipLoader(game,"Slot Accessory 2");CheckReloads(game,10.5f);
                        game.UI.ChangeTank();SelectLoader(game,"Slot Accessory 2");CheckReloads(game,10.5f);
                        Next();break;
                    case 2:
                        Directory.CreateDirectory("Checks/Accessories");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/Accessories/AutoLoaderGarage.png"));Next();break;
                    case 3:
                        game.UI.ClosePopup();game.UI.Play();StageProgressionAudit.DisableWeapons(game);CheckReloads(game,10.5f);
                        Near(game.MachineGun.ShotDelay,shotDelay,"Reload bonuses must not alter burst shot delay");
                        Require(game.MachineGun.ReserveCapacity==reserve,"Reload speed must not alter ammunition capacity.");
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>().Where(enemy=>enemy.Weapon!=null))
                            Near(enemy.Weapon.Stats.reloadTime,enemy.Data.weapon.reloadTime,"Player accessory must not affect enemy reloads");
                        Require(JsonUtility.ToJson(loader)==definitionBefore&&JsonUtility.ToJson(game.Equipment.MachineGun)==weaponBefore,"Equipping/upgrading must not mutate saved item/weapon definitions.");
                        game.ReturnHome();game.RefreshEquipmentArtwork();game.RefreshEquipmentArtwork();CheckReloads(game,10.5f);
                        Require(!errors,"Auto Loader must have no runtime errors.");
                        Debug.Log("AUTO_LOADER_AUDIT_OK: generated garage icon, +10% base/+0.5pp per level, all three reload timers, real Parts level-up, saved IDs/levels, unique slot movement, Heavy masking, no refresh compounding, unchanged burst/ammo/enemy stats and immutable definitions.");Finish(0);break;
                }
                if(EditorApplication.timeSinceStartup-since>30)throw new Exception("Accessory audit timed out.");
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
