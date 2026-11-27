using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class EquipmentHtmlReview
    {
        const string Output="Checks/EquipmentHtml";
        static readonly string[] States={"slot-locks","cannon","mortar","wheels","wheels-equipped","hull-speed","hull-heavy","crew","main-offroad","gameplay-offroad","boss-offroad"};
        static readonly Dictionary<string,string> saved=new Dictionary<string,string>();static readonly HashSet<string> existed=new HashSet<string>();
        static StageRewardSaveScope scope;static EquipmentHtmlScreen ui;static HtmlBattleScreen battle;
        static int index,wait,lastFrame;static bool errors,oldEnabled;static EnterPlayModeOptions oldOptions;
        static EquipmentHtmlReview(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            const string file="Checks/equipment-html-review.pending";
            if(!File.Exists(file)||EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||File.Exists("Checks/equipment-html-apply.pending")||!File.Exists("Checks/equipment-html-ready.flag")||!OriginalPortSetup.AssembliesCurrent())return;
            File.Delete(file);Run();
        }
        [MenuItem("MegaTanks/Equipment/Check approved native screen")]
        public static void Run()
        {
            scope=new StageRewardSaveScope();saved.Clear();existed.Clear();
            foreach(string key in new[]{PlayerTankProfile.SaveKey,BattleGame.SelectedTankSaveKey,"MegaTanks.Crew.Loadout","MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar","MegaTanks.Equipment.Wheels"})
            {if(PlayerPrefs.HasKey(key))existed.Add(key);saved[key]=PlayerPrefs.GetString(key,"");}
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt","");File.WriteAllText(Output+"/runtime-errors.txt","");
            index=-1;wait=20;lastFrame=-1;errors=false;ui=null;
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));MegaTanksSetup.SetLandscapeGameView(view);view.Show();
            Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
            scope?.Dispose();foreach(var entry in saved){if(existed.Contains(entry.Key))PlayerPrefs.SetString(entry.Key,entry.Value);else PlayerPrefs.DeleteKey(entry.Key);}PlayerPrefs.Save();
            EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
            File.AppendAllText(Output+"/validation.txt",errors?"FAIL: inspect runtime-errors.txt\n":"PASS: all native Equipment states completed with no Error, Exception or Assert. Player save restored.\n");
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){errors=true;File.AppendAllText(Output+"/runtime-errors.txt",message+"\n"+stack+"\n");}}
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static void Pass(string message)=>File.AppendAllText(Output+"/validation.txt","PASS: "+message+"\n");
        static void Tick()
        {
            try
            {
                if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(--wait>0)return;
                var game=BattleGame.Instance;if(game==null){wait=5;return;}
                if(index<0){game.UI.OpenEquipment();ui=Resources.FindObjectsOfTypeAll<EquipmentHtmlScreen>().First(c=>c.gameObject.scene.IsValid());index=0;wait=45;return;}
                if(index>=States.Length*2){EditorApplication.isPlaying=false;return;}
                if(index%2==0){Build(States[index/2],game);wait=45;}
                else{ScreenCapture.CaptureScreenshot(Output+"/"+States[index/2]+".png");wait=8;}
                index++;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.isPlaying=false;}
        }
        static void Upgrade(BattleGame game,Object item,Action action,bool crew)
        {
            int coins=game.Wallet.Parts,gears=game.Wallet.PetFood,level=game.Progression.Get(item).Level,cost=ProgressionRules.LevelCost(level),xp=new PlayerTankProfile(HtmlBattleConfig.Load()).TotalXP;
            action();Require(game.Progression.Get(item).Level==level+1,"Upgrade must increase exactly one level.");
            Require(game.Wallet.Parts==coins-(crew?cost:0)&&game.Wallet.PetFood==gears-(crew?0:cost),"Upgrade spent the wrong currency.");
            Require(new PlayerProgression(game.Frontend,new PlayerWallet()).Get(item).Level==level+1,"Upgrade level must persist through reload.");
            Require(new PlayerTankProfile(HtmlBattleConfig.Load()).TotalXP==xp,"Item level must not change Tank EXP.");
        }
        static void Build(string state,BattleGame game)
        {
            if(state=="slot-locks")CheckSlotLocks(game);
            else if(state=="cannon")
            {
                ui.SelectPart(1);var equipped=game.Equipment.Cannon;ui.SelectCandidate(1);Require(game.Equipment.Cannon==equipped,"Preview must not equip.");
                var entry=ui.catalog.Weapon((WeaponDefinition)ui.Selected);float before=EquipmentHtmlCatalog.WeaponBaseDamage(HtmlBattleConfig.Load(),game.Progression,entry);
                Upgrade(game,ui.Selected,()=>ui.UpgradeButton.onClick.Invoke(),false);Require(EquipmentHtmlCatalog.WeaponBaseDamage(HtmlBattleConfig.Load(),game.Progression,entry)>before,"Weapon upgrade must affect resolved battle damage.");
                ui.EquipButton.onClick.Invoke();Require(game.Equipment.Cannon==ui.Selected,"Equip must assign the selected cannon.");
                Pass("cannon preview/equip separate; gun upgrade spends gears only, persists, improves combat damage and preserves Tank EXP.");
            }
            else if(state=="mortar")
            {
                SetTankLevel(HtmlBattleConfig.Load().equipmentUnlock.EquipmentLevels.Max());
                ui.SelectPart(2);Require(ui.VisibleCount==1,"Mortar must only offer the original green tube.");
                Require(ui.catalog.Weapon((WeaponDefinition)ui.Selected).artKey=="tank_base_09_mortar_barrel","Mortar source mismatch.");
                Pass("only original green mortar; no legacy missile weapon images.");
            }
            else if(state=="wheels")
            {
                ui.SelectPart(4);var before=game.Equipment.Wheels;ui.SelectCandidate(1);Require(game.Equipment.Wheels==before,"Wheel preview may not equip.");
                Require(ui.transform.Find("Equipment Design 770x436/Part 4/Label").GetComponent<CrewBitmapText>().Value=="WHEELS","Wheels button must be English.");
                Pass("WHEELS opens STANDARD/OFFROAD; preview leaves equipped wheels unchanged.");
            }
            else if(state=="wheels-equipped")
            {
                var cannon=game.Equipment.Cannon;var tank=game.TankProfile;
                Upgrade(game,ui.Selected,()=>ui.UpgradeButton.onClick.Invoke(),false);ui.EquipButton.onClick.Invoke();
                Require(game.Equipment.Wheels==ui.Selected&&game.Equipment.Cannon==cannon&&game.TankProfile==tank,"Wheel equip must be independent.");
                var loaded=new PlayerEquipment(game.StartingLoadout,game.Frontend);loaded.ConfigureTank(game.TankProfile);Require(loaded.Wheels==ui.Selected,"Wheel selection must persist after reload.");
                Pass("wheel upgrade/equip spends gears only, saves and keeps hull/guns unchanged.");
            }
            else if(state=="hull-speed"||state=="hull-heavy")
            {
                ui.SelectPart(3);var hull=game.TankProfile;var wheel=game.Equipment.Wheels;var cannon=game.Equipment.Cannon;ui.SelectCandidate(state=="hull-speed"?1:2);
                Require(game.TankProfile==hull,"Hull preview may not equip.");
                Upgrade(game,ui.Selected,()=>ui.UpgradeButton.onClick.Invoke(),false);ui.EquipButton.onClick.Invoke();
                Require(game.TankProfile==ui.Selected&&game.Equipment.Wheels==wheel&&game.Equipment.Cannon==cannon,"Hull equip must keep guns and wheels.");
                foreach(string skin in new[]{"base","speed","heavy"})
                {
                    var parts=EquipmentHtmlCatalog.Parts(ui.art,skin,"offroad");Require(parts.Count(p=>p.name.Contains("offroad_wheel"))==2&&parts.All(p=>p.name!="01_track_chassis"&&!p.name.Contains("wheel_0")),"Offroad must fully replace tracks/four wheels for "+skin);
                    Require(parts.All(p=>EquipmentHtmlCatalog.Sprite(ui.art,p.key)!=null),"Missing native Offroad sprite for "+skin);
                }
                Pass(state+": gears upgrade saves; Equip keeps wheels/guns; all 3 original native Offroad fits have exactly 2 wheels, no old track.");
            }
            else if(state=="crew")
            {
                game.UI.OpenPopup("Crew");var crew=Resources.FindObjectsOfTypeAll<CrewHtmlScreen>().First(c=>c.gameObject.scene.IsValid());crew.Show(game);
                Upgrade(game,crew.Selected,()=>crew.levelUp.onClick.Invoke(),true);Pass("actual Crew Level Up button spends coins only and saves independently of Tank EXP.");
            }
            else if(state=="main-offroad")game.UI.ClosePopup();
            else
            {
                if(battle==null)battle=HtmlBattleScreen.Begin(game,0);battle.ReviewState(state=="boss-offroad"?"boss":"gameplay");
                var model=battle.Simulation;Require(model.player.skin==EquipmentHtmlCatalog.Skin(game.TankProfile)&&model.player.wheelSkin=="offroad","Main loadout must carry to real Play.");
                var entry=ui.catalog.Weapon(game.Equipment.Cannon);Require(Mathf.Abs(model.playerGuns[1].damage-EquipmentHtmlCatalog.WeaponBaseDamage(model.config,game.Progression,entry))<.001f&&model.weaponVariants[1]=="cannon_ap","Battle gun upgrade/variant must match Equipment resolver.");
                Require(model.player.maxHP>ui.art.Profile(model.player.skin).hp&&model.wheelSpeedMultiplier>1.15f,"Hull/wheel upgrades must carry into combat.");
                Require(model.MissileStats==null,"Legacy missile save may not replace green mortar.");
                var standard=ui.art.Profile(model.player.skin).parts.Where(p=>EquipmentHtmlCatalog.IsWheel(p)||p.name=="01_track_chassis");
                var offroad=EquipmentHtmlCatalog.Parts(ui.art,model.player.skin,"offroad").Where(p=>p.name.Contains("offroad"));
                float expected=model.config.groundY+(standard.Max(p=>p.position[1]+p.size[1])-1258)*model.player.width/1351;
                float actual=model.player.y+(offroad.Max(p=>p.position[1]+p.size[1])-1258)*model.player.width/1351;
                Require(Mathf.Abs(expected-actual)<.1f,"Offroad tires must keep the original road contact height.");
                Pass(state+": real battle uses selected hull, Offroad visuals/stats, permanent gun upgrades and AP slot variant; green mortar retained.");
            }
        }
        static void SetTankLevel(int level)
        {
            var config=HtmlBattleConfig.Load();PlayerPrefs.DeleteKey(PlayerTankProfile.SaveKey);
            var profile=new PlayerTankProfile(config);int xp=0;for(int i=1;i<level;i++)xp+=profile.Cost(i);
            if(xp>0)profile.Award("equipment-unlock-review",xp);
        }
        static void CheckSlotLocks(BattleGame game)
        {
            var config=HtmlBattleConfig.Load();bool had=PlayerPrefs.HasKey(PlayerTankProfile.SaveKey);string original=PlayerPrefs.GetString(PlayerTankProfile.SaveKey,"");
            string wallet=PlayerPrefs.GetString(PlayerWallet.DefaultSaveKey,""),progression=PlayerPrefs.GetString(PlayerProgression.DefaultSaveKey,"");
            var loadout=new Object[]{game.Equipment.MachineGun,game.Equipment.Cannon,game.Equipment.Mortar};
            try
            {
                foreach(int level in config.equipmentUnlock.EquipmentLevels.SelectMany(n=>new[]{Math.Max(1,n-1),n}).Append(1).Distinct())
                {
                    SetTankLevel(level);ui.Refresh();
                    for(int part=0;part<5;part++)
                    {
                        int required=config.equipmentUnlock.LevelForPart(part);bool unlocked=level>=required;var button=ui.transform.Find("Equipment Design 770x436/Part "+part).GetComponent<UnityEngine.UI.Button>();
                        Require(button.interactable==unlocked,"Part button must lock exactly below its configured Tank Level: "+part+" at "+level);
                        int previous=ui.ActivePart;var selection=ui.Selected;button.onClick.Invoke();
                        Require(unlocked?ui.ActivePart==part:ui.ActivePart==previous&&ui.Selected==selection,"Locked slot must reject selection even through a direct callback.");
                        Require(button.transform.Find("Lock Icon").gameObject.activeSelf==!unlocked,"Lock icon must follow the slot unlock state.");
                        if(!unlocked)Require(button.transform.Find("Label").GetComponent<CrewBitmapText>().Value.Contains("LV."+required),"Locked part must show the required Tank Level.");
                    }
                }
                Require(PlayerPrefs.GetString(PlayerWallet.DefaultSaveKey,"")==wallet&&PlayerPrefs.GetString(PlayerProgression.DefaultSaveKey,"")==progression,"Checking locked parts must not spend or upgrade.");
                Require(loadout.SequenceEqual(new Object[]{game.Equipment.MachineGun,game.Equipment.Cannon,game.Equipment.Mortar}),"Locking slots must preserve saved weapon assignments.");
                Require(config.unlockLevels[2]==config.equipmentUnlock.mortar,"Combat and garage must share the configured Mortar unlock.");
                Pass("all 5 part buttons use named config; lock icons/disabled state and callbacks checked below/at each unlock, including Mortar/Wheels/Hull; combat shares gun unlocks; assignments/wallet/item levels preserved.");
            }
            finally{if(had)PlayerPrefs.SetString(PlayerTankProfile.SaveKey,original);else PlayerPrefs.DeleteKey(PlayerTankProfile.SaveKey);PlayerPrefs.Save();ui.SelectPart(1);ui.Refresh();}
        }
    }
}
