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
    public static class CombatAccessoriesAudit
    {
        static StageRewardSaveScope scope;
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static readonly string[] keys={BattleGame.SelectedTankSaveKey,"MegaTanks.Accessory.1","MegaTanks.Accessory.2"};
        static AccessoryDefinition computer,camo,loader;
        static string computerBefore,camoBefore,weaponBefore;
        static int phase,frame;
        static double since;
        static bool errors;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Near(float actual,float expected,string message){Require(Mathf.Abs(actual-expected)<.0001f,message+": "+actual+" expected "+expected);}
        static void Next(){phase++;frame=Time.frameCount;since=EditorApplication.timeSinceStartup;}
        static UIView Garage(BattleGame game)=>game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
        static void Select(BattleGame game,AccessoryDefinition item,string slot)
        {
            var view=Garage(game);view.Get<Button>(slot).onClick.Invoke();
            var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(child=>child.gameObject.activeSelf&&child.name=="Item "+item.name);
            cell.GetComponent<Button>().onClick.Invoke();
        }
        static void Equip(BattleGame game,AccessoryDefinition item,string slot){Select(game,item,slot);Garage(game).Get<Button>("Equip Item").onClick.Invoke();}
        static float BaseEvasion(BattleGame game)=>game.CalculateTankStats(game.Equipment.Armor,game.Equipment.ArmorSlotCount>1?game.Equipment.SecondArmor:null).evasion;
        static void CheckBonuses(BattleGame game,float accuracy,float evasion,float reload=0)
        {
            Near(game.AccuracyBonus,accuracy,"Accuracy accessory total");Near(game.EvasionBonus,evasion,"Evasion accessory total");Near(game.ReloadSpeedBonus,reload,"Reload accessory total");
            Near(game.MachineGun.Stats.accuracy,game.Equipment.MachineGun.stats.accuracy+accuracy,"MG accuracy");
            Near(game.MainGun.Stats.accuracy,game.Equipment.Cannon.stats.accuracy+accuracy,"Cannon accuracy");
            Near(game.Mortar.Stats.accuracy,game.Equipment.Mortar.stats.accuracy+accuracy,"Mortar accuracy");
            Near(game.BaseTankStats.evasion,BaseEvasion(game)+evasion,"Tank evasion must combine paint and armor");
            Near(game.MachineGun.ReloadTime,AccessoryDefinition.ReloadTime(game.Equipment.MachineGun.stats.reloadTime,game.Equipment.MachineGun.stats.reloadSpeed+reload),"Bonus types must not leak into reload");
        }
        public static void Run()
        {
            scope=new StageRewardSaveScope();foreach(var key in keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
            PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");PlayerPrefs.DeleteKey("MegaTanks.Accessory.1");PlayerPrefs.DeleteKey("MegaTanks.Accessory.2");PlayerPrefs.DeleteKey(PlayerProgression.DefaultSaveKey);PlayerPrefs.Save();
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
                var game=BattleGame.Instance;var view=Garage(game);
                switch(phase)
                {
                    case 0:
                        computer=game.Frontend.inventory.OfType<AccessoryDefinition>().Single(item=>item.id=="accessory.targeting-computer");
                        camo=game.Frontend.inventory.OfType<AccessoryDefinition>().Single(item=>item.id=="accessory.camo-paint");
                        loader=game.Frontend.inventory.OfType<AccessoryDefinition>().Single(item=>item.id=="accessory.autoloader");
                        Require(computer.icon!=null&&camo.icon!=null&&computer.icon!=camo.icon,"Each accessory must have a distinct generated icon.");
                        computerBefore=JsonUtility.ToJson(computer);camoBefore=JsonUtility.ToJson(camo);weaponBefore=JsonUtility.ToJson(game.Equipment.MachineGun);
                        Near(computer.Bonus(1),10,"Computer level 1");Near(computer.Bonus(2),10.5f,"Computer level 2");Near(camo.Bonus(11),15,"Camo growth");Near(loader.ReloadSpeed(1),10,"Auto Loader migration must retain its bonus");
                        game.UI.OpenEquipment();Equip(game,computer,"Slot Accessory 2");Equip(game,camo,"Slot Accessory 3");CheckBonuses(game,10,10);
                        Require(view.Get<Image>("Slot Accessory 2/Equipped Item Icon").sprite==computer.icon&&view.Get<Image>("Slot Accessory 3/Equipped Item Icon").sprite==camo.icon,"Both generated icons must appear in their actual garage slots.");
                        if(game.Wallet.Parts<1000)game.Wallet.TryAdd(CurrencyType.Parts,1000);
                        int parts=game.Wallet.Parts;
                        Select(game,computer,"Slot Accessory 2");view.Get<Button>("Level Up").onClick.Invoke();
                        Select(game,camo,"Slot Accessory 3");view.Get<Button>("Level Up").onClick.Invoke();
                        Require(game.Wallet.Parts==parts-200,"Both level upgrades must charge the existing Parts costs.");CheckBonuses(game,10.5f,10.5f);
                        var progression=new PlayerProgression(game.Frontend,game.Wallet);var equipment=new PlayerEquipment(game.StartingLoadout,game.Frontend);
                        Require(progression.Get(computer).Level==2&&progression.Get(camo).Level==2&&equipment.Accessories[0]==computer&&equipment.Accessories[1]==camo,"Both accessory IDs and levels must survive reload.");
                        Next();break;
                    case 1:
                        Directory.CreateDirectory("Checks/Accessories");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/Accessories/CamoPaintGarage.png"));Next();break;
                    case 2:
                        Select(game,computer,"Slot Accessory 2");Next();break;
                    case 3:
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/Accessories/TargetingComputerGarage.png"));
                        Next();break;
                    case 4:
                        game.UI.ChangeTank();CheckBonuses(game,10.5f,0);Require(!game.Equipment.Equip(camo,EquipmentType.Accessory,1),"Heavy must reject its replaced accessory slot.");
                        game.UI.ChangeTank();CheckBonuses(game,10.5f,10.5f);
                        Equip(game,camo,"Slot Accessory 2");Require(game.Equipment.Accessories[0]==camo&&game.Equipment.Accessories[1]==computer,"Moving accessories must swap rather than duplicate.");
                        game.UI.ChangeTank();CheckBonuses(game,0,10.5f);game.UI.ChangeTank();
                        Equip(game,computer,"Slot Accessory 2");Equip(game,loader,"Slot Accessory 3");CheckBonuses(game,10.5f,0,10);
                        Equip(game,camo,"Slot Accessory 3");game.RefreshEquipmentArtwork();game.RefreshEquipmentArtwork();CheckBonuses(game,10.5f,10.5f);
                        game.UI.ClosePopup();game.UI.Play();StageProgressionAudit.DisableWeapons(game);game.SetStance(TankStance.Defensive);CheckBonuses(game,10.5f,10.5f);
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                        var target=game.SpawnEnemy(EnemyKind.Ground,(Vector2)game.Artwork.transform.position+new Vector2(5,1));target.enabled=target.Weapon.enabled=false;
                        game.MachineGun.Stats.damage=game.MainGun.Stats.damage=game.Mortar.Stats.damage=0;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");game.Mortar.SendMessage("Update");
                        var shots=Object.FindObjectsOfType<Projectile2D>();
                        Require(shots.Length==3,"All three weapons must fire real projectiles.");
                        foreach(var shot in shots)
                        {
                            float expected=shot.IsMortar?game.Mortar.Stats.accuracy:shot.name==game.MachineGun.DisplayName+" shell"?game.MachineGun.Stats.accuracy:game.MainGun.Stats.accuracy;
                            Near(shot.Attack.Accuracy,expected,"Projectile attack must snapshot boosted accuracy");
                        }
                        var victim=new UnitDefenseStats {evasion=20};
                        var plain=new DamageAttack(1,new WeaponCombatStats {accuracy=100});
                        var boosted=new DamageAttack(1,new WeaponCombatStats {accuracy=100+game.AccuracyBonus});
                        Require(!CombatDamage.Calculate(plain,victim,.85f,1).Hit&&CombatDamage.Calculate(boosted,victim,.85f,1).Hit,"Targeting bonus must improve the actual hit calculation against evasion.");
                        var defense=game.CalculateTankStats(game.Equipment.Armor,game.Equipment.ArmorSlotCount>1?game.Equipment.SecondArmor:null);
                        float roll=(Mathf.Clamp(100-defense.evasion,0,100)+Mathf.Clamp(100-game.BaseTankStats.evasion,0,100))*.005f;
                        Require(CombatDamage.Calculate(plain,defense,roll,1).Hit&&!CombatDamage.Calculate(plain,game.BaseTankStats,roll,1).Hit,"Camo Paint must reduce incoming hit chance through the actual combat formula.");
                        Require(JsonUtility.ToJson(computer)==computerBefore&&JsonUtility.ToJson(camo)==camoBefore&&JsonUtility.ToJson(game.Equipment.MachineGun)==weaponBefore,"Upgrades and battle stats must not mutate saved definitions.");
                        Require(!errors,"Combat accessory audit must have no runtime errors.");
                        Debug.Log("COMBAT_ACCESSORIES_OK: distinct garage icons, +10% base/+0.5pp levels, real Parts upgrades, saved IDs/levels, all three accuracy snapshots, actual hit/evasion formulas, two-slot combinations, Heavy masking, Auto Loader compatibility and immutable definitions.");Finish(0);break;
                }
                if(EditorApplication.timeSinceStartup-since>30)throw new Exception("Combat accessory audit timed out.");
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
