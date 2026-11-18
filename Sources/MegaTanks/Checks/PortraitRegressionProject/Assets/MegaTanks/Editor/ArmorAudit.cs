using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    public static class ArmorAudit
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float actual,float expected,string message)=>Require(Mathf.Abs(actual-expected)<.001f,message+" "+actual+" / "+expected);
        static ArmorDefinition basic,light;static FrontendCatalog catalog;
        static readonly Dictionary<string,string> saved=new Dictionary<string,string>();static readonly HashSet<string> existed=new HashSet<string>();
        static readonly string[] keys={PlayerWallet.DefaultSaveKey,PlayerProgression.DefaultSaveKey,PlaytestResources.SaveKey,PlaytestResources.ArmorSaveKey,"MegaTanks.Equipment.Armor","MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar"};
        static bool oldOptions,errors;static EnterPlayModeOptions options;static int phase;static double since;
        static void Model()
        {
            Require(basic.hp==100&&basic.armor==10&&basic.speed==0&&basic.evasion==0&&basic.hpPerLevel==10,"Basic Armor defaults.");
            Require(light.hp==50&&light.armor==5&&light.speed==.5f&&light.evasion==5&&light.hpPerLevel==5,"Light Armor defaults.");
            Require(basic.icon!=null&&light.icon!=null&&basic.icon!=light.icon,"Distinct generated armor icons.");
            string key="MegaTanks.Armor.Audit."+Guid.NewGuid(),walletKey=key+".wallet";
            try
            {
                var wallet=new PlayerWallet(walletKey);var progression=new PlayerProgression(catalog,wallet,key);
                Near(progression.ArmorHP(basic),100,"Basic initial HP");Near(progression.ArmorHP(light),50,"Light initial HP");
                Require(!progression.TryLevelUp(basic)&&!progression.TryStarUp(basic),"Armor requires materials.");
                wallet.TryAdd(CurrencyType.Parts,200);
                Require(progression.TryLevelUp(basic)&&progression.TryLevelUp(light)&&wallet.Parts==0,"Armor levels spend Parts.");
                progression.TryGrantFragments(basic,20);progression.TryGrantFragments(light,20);
                Require(progression.TryStarUp(basic)&&progression.TryStarUp(light),"Each armor has its own blueprints.");
                Near(progression.ArmorHP(basic),121,"Basic level then star");Near(progression.ArmorHP(light),60.5f,"Light level then star");
                var tank=new TankStats{maxHull=200,armor=3,advanceSpeed=.8f,evasion=2};
                light.ApplyTo(tank,progression.ArmorHP(light));
                Near(tank.maxHull,260.5f,"Add HP");Near(tank.armor,8,"Add armor");Near(tank.advanceSpeed,1.3f,"Add speed");Near(tank.evasion,7,"Add evasion");
                var attack=new DamageAttack(30,new WeaponCombatStats{accuracy=100});
                Near(CombatDamage.Calculate(attack,tank,.1f,.9f).Damage,22,"Armor reduces incoming damage.");
                Require(!CombatDamage.Calculate(attack,tank,.95f,.9f).Hit,"Evasion affects hit chance.");
                var loaded=new PlayerProgression(catalog,new PlayerWallet(walletKey),key);
                Near(loaded.ArmorHP(light),60.5f,"Armor progression survives reload.");
                Near(basic.HitPoints(1,20),300,"Basic max star HP");Near(light.HitPoints(1,20),150,"Light max star HP");
                Require(basic.hp==100&&light.hp==50&&basic.armor==10&&light.armor==5,"Progression never mutates definition stats.");
                Debug.Log("ARMOR_MODEL_PASS");
            }
            finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(walletKey);PlayerPrefs.Save();}
        }
        public static void Run()
        {
            try
            {
                ArmorSetup.Install();ArmorSetup.Install();
                basic=AssetDatabase.LoadAssetAtPath<ArmorDefinition>(ArmorSetup.BasicPath);light=AssetDatabase.LoadAssetAtPath<ArmorDefinition>(ArmorSetup.LightPath);
                catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>("Assets/MegaTanks/Data/UI/Frontend.asset");Model();
                foreach(string key in keys){if(PlayerPrefs.HasKey(key))existed.Add(key);saved[key]=PlayerPrefs.GetString(key,"");PlayerPrefs.DeleteKey(key);}
                // Fresh progression for screenshots, restoring the player's actual upgrades afterwards.
                PlayerPrefs.Save();EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode){phase=0;since=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.7)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;Require(game!=null&&game.UI!=null,"Game starts.");
                var view=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
                switch(phase++)
                {
                    case 0: game.UI.OpenEquipment();break;
                    case 1:
                        ItemProgressionAudit.Press(view.Get<Button>("Inventory Filter"));
                        break;
                    case 2: ItemProgressionAudit.Press(view.Get<Button>("Filter List/Filter Armor"));break;
                    case 3:
                        Require(game.UI.InventoryVisibleCount==2,"Armor filter lists both items.");
                        ItemProgressionAudit.Press(view.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().Single(b=>b.name=="Item "+basic.name));break;
                    case 4:
                        ItemProgressionAudit.Press(view.Get<Button>("Equip Item"));
                        Require(game.Equipment.Armor==basic&&PlayerPrefs.GetString("MegaTanks.Equipment.Armor")==basic.id,"Armor equips and saves by ID.");
                        Near(game.MaxHull,game.CalculateTankStats(null).maxHull+100,"Equipping immediately adds HP at Home.");
                        ItemProgressionAudit.Capture(game,"Armor-Basic");break;
                    case 5:
                        ItemProgressionAudit.Press(view.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().Single(b=>b.name=="Item "+light.name));break;
                    case 6:
                        Require(view.Get<Text>("Item Stats").text.Contains("EVASION")&&view.Get<Text>("Progression Summary").text.Contains("BONUS HP"),"Armor previews show defense stats and HP progression.");
                        ItemProgressionAudit.Press(view.Get<Button>("Level Up"));ItemProgressionAudit.Press(view.Get<Button>("Star Up"));ItemProgressionAudit.Press(view.Get<Button>("Equip Item"));
                        Near(game.Progression.ArmorHP(light),60.5f,"Light Armor UI upgrades HP.");
                        var baseStats=game.CalculateTankStats(null);var actual=game.BaseTankStats;
                        Near(actual.maxHull,baseStats.maxHull+60.5f,"Replacement does not stack previous armor.");
                        Near(actual.armor,baseStats.armor+5,"Light armor bonus");Near(actual.advanceSpeed,baseStats.advanceSpeed+.5f,"Light speed");Near(actual.evasion,baseStats.evasion+5,"Light evasion");
                        Require(view.Get<UIStarStrip>("Slot Armor/Stars").Stars==1,"Equipped armor stars update.");
                        Require(view.Get<Text>("Item Stats").preferredHeight<=130,"Armor stat text must fit above the equipped indicator.");
                        ItemProgressionAudit.Capture(game,"Armor-Light-Upgraded");
                        // Verify a new loadout instance recognizes the saved stable armor ID.
                        var defaults=ScriptableObject.CreateInstance<WeaponLoadout>();defaults.machineGun=game.Equipment.MachineGun;defaults.cannon=game.Equipment.Cannon;defaults.mortar=game.Equipment.Mortar;
                        Require(new PlayerEquipment(defaults,catalog).Armor==light,"Armor selection persists.");UnityEngine.Object.Destroy(defaults);
                        game.UI.ClosePopup();game.StartBattle();Near(game.MaxHull,baseStats.maxHull+60.5f,"Battle starts with armor HP.");Near(game.Hull,game.MaxHull,"Armor HP is filled at run start.");
                        game.ReturnHome();game.StartBattle();Near(game.MaxHull,baseStats.maxHull+60.5f,"Retry does not stack armor.");
                        Require(!errors,"No runtime errors.");Debug.Log("ARMOR_PLAY_PASS");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
            foreach(var pair in saved){if(existed.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            PlayerPrefs.Save();EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
