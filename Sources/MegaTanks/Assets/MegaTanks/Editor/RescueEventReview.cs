using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    [InitializeOnLoad] public static class RescueEventReview
    {
        const string Output="Checks/RescueEvent";
        static readonly string[] States={"stage1-unlocks-mg","stage1-rescue","stage1-rescue-walk","stage1-mg-online","stage1-dialogue","stage1-common","stage2-unlocks-mortar","stage2-rescue","stage2-mortar-online","stage2-dialogue","stage2-common","recruited-main"};
        static StageRewardSaveScope scope;static HtmlBattleScreen screen;static int index,wait,lastFrame;static bool errors,oldEnabled,profileExisted;static string profileJson;static EnterPlayModeOptions oldOptions;
        static string untouchedRoster,untouchedRecruitment;
        static RescueEventReview(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            const string flag="Checks/rescue-review.pending";
            if(!File.Exists(flag)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||File.Exists("Checks/rescue-apply.pending")||!File.Exists("Checks/rescue-ready.flag")||!OriginalPortSetup.AssembliesCurrent())return;
            File.Delete(flag);Run();
        }
        [MenuItem("MegaTanks/Rescue/Check two-stage story")]
        public static void Run()
        {
            scope=new StageRewardSaveScope();profileExisted=PlayerPrefs.HasKey(PlayerTankProfile.SaveKey);profileJson=PlayerPrefs.GetString(PlayerTankProfile.SaveKey,"");
            PlayerPrefs.DeleteKey(PlayerTankProfile.SaveKey);PlayerPrefs.DeleteKey(PlayerCrew.RecruitmentSaveKey);PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");PlayerPrefs.Save();
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt","");File.WriteAllText(Output+"/runtime-errors.txt","");
            screen=null;index=0;wait=25;lastFrame=-1;errors=false;
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));MegaTanksSetup.SetLandscapeGameView(view);view.Show();
            Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
            scope.Dispose();if(profileExisted)PlayerPrefs.SetString(PlayerTankProfile.SaveKey,profileJson);else PlayerPrefs.DeleteKey(PlayerTankProfile.SaveKey);PlayerPrefs.Save();
            EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
            File.AppendAllText(Output+"/validation.txt",errors?"FAIL: inspect runtime-errors.txt\n":"PASS: 12 rescue/story/recruitment states; no Error, Exception or Assert. Player save restored.\n");
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors=true;File.AppendAllText(Output+"/runtime-errors.txt",message+"\n"+stack+"\n");}}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Pass(string message)=>File.AppendAllText(Output+"/validation.txt","PASS: "+message+"\n");
        static void Freeze()=>typeof(HtmlBattleScreen).GetField("reviewFreeze",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(screen,true);
        static void Until(Func<bool> ready,int max=12000,bool clearEnemies=false,bool checkOpening=false)
        {
            var s=screen.Simulation;
            for(int i=0;i<max&&!ready();i++)
            {
                Require(s.phase!="lost","Natural story route lost before "+States[Math.Min(index/2,States.Length-1)]+" at wave "+s.wave);
                if(checkOpening)Require(!s.enemies.Any(e=>e.kind=="dog"||e.kind=="trojan"),"Stage 1 opening may not contain melee dogs/Trojan payloads.");
                if(clearEnemies){if(s.phase=="choice")s.Choose("skip");foreach(var e in s.enemies.Where(e=>!e.dead).ToArray())s.Damage(e,100000);}
                s.Tick(.05f);
            }
            Require(ready(),"Story route did not reach expected state; phase="+s.phase+", wave="+s.wave+", enemies="+s.enemies.Count+", hp="+s.player.hp);
        }
        static void Tick()
        {
            try
            {
                if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(--wait>0)return;var game=BattleGame.Instance;if(game==null){wait=5;return;}
                if(index>=States.Length*2){EditorApplication.isPlaying=false;return;}
                if(index%2==0){Build(States[index/2],game);wait=40;}else{if(States[index/2].EndsWith("-rescue"))PrisonerDiagnostic();ScreenCapture.CaptureScreenshot(Output+"/"+States[index/2]+".png");wait=8;}index++;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.isPlaying=false;}
        }
        static void ClickRescue()
        {
            var card=screen.transform.Find("Choice and Pause/Card rescue").GetComponent<Button>();card.onClick.Invoke();card.onClick.Invoke();
            Require(screen.Simulation.phase=="rescue_release","Actual Rescue button must start release once and close choice.");
        }
        static void PrisonerDiagnostic()
        {
            var world=(HtmlBattleWorld)typeof(HtmlBattleScreen).GetField("nativeWorld",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(screen);
            var people=(System.Collections.IDictionary)typeof(HtmlBattleWorld).GetField("people",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            var person=people[screen.Simulation.prisonerToken];Require(person!=null,"Prisoner render instance missing.");
            var spine=(Spine.Unity.SkeletonAnimation)person.GetType().GetField("spine").GetValue(person);var renderer=spine.GetComponent<MeshRenderer>();
            var camera=(Camera)typeof(HtmlBattleWorld).GetField("worldCamera",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            var area=new Rect(screen.Simulation.landmarkX-150,1080-screen.Simulation.config.groundY,300,300);
            camera.Render();var visible=CagePixels(camera.targetTexture,area);Color32[] hidden;
            try{renderer.enabled=false;camera.Render();hidden=CagePixels(camera.targetTexture,area);}
            finally{renderer.enabled=true;camera.Render();}
            int changed=0;for(int i=0;i<visible.Length;i++)if(Math.Abs(visible[i].r-hidden[i].r)+Math.Abs(visible[i].g-hidden[i].g)+Math.Abs(visible[i].b-hidden[i].b)>30)changed++;
            Require(changed>400,"Prisoner is occluded or missing behind bars in stage "+(screen.Simulation.stageIndex+1)+": "+changed+" visible pixels.");
            Pass("stage "+(screen.Simulation.stageIndex+1)+" live Spine prisoner visible behind bars: "+changed+" rendered pixels checked.");
        }
        static Color32[] CagePixels(RenderTexture target,Rect area)
        {
            var previous=RenderTexture.active;var image=new Texture2D(300,300,TextureFormat.RGB24,false);
            try{RenderTexture.active=target;image.ReadPixels(area,0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=previous;UnityEngine.Object.Destroy(image);}
        }
        static void CompleteDialogue(BattleGame game,int stage)
        {
            var s=screen.Simulation;var story=s.RescueStory;var member=game.Crew.Member(story.crewId);
            Require(s.phase=="won"&&new PlayerTankProfile(s.config).Level>=s.config.unlockLevels[story.weaponSlot],"Stage reward must permanently unlock its weapon.");
            Require(!game.Crew.IsRecruited(member)&&game.Crew.CommonCardCount(member)==0,"Companion may not join permanently before claiming Common card.");
            var loaded=new PlayerCrew(game.Frontend.crew);Require(loaded.Pending?.crewId==member.id&&!loaded.IsRecruited(member),"Unclaimed Common reward must persist through restart.");
            var ui=UnityEngine.Object.FindObjectOfType<CrewRecruitmentRewardUI>();Require(ui!=null&&!ui.ShowingCard,"Post-boss dialogue must appear first.");
            Require(!screen.transform.Find("Choice and Pause").gameObject.activeSelf,"Stage result panel must stay hidden behind dialogue/card.");
            for(int i=0;i<story.dialogue.Length;i++)ui.transform.Find("Next").GetComponent<Button>().onClick.Invoke();
            Require(ui.ShowingCard&&ui.transform.Find("Common Crew Card/Rarity").GetComponent<Text>().text=="COMMON","Dialogue must lead to Common crew card.");
            Pass("stage "+(stage+1)+" boss: sufficient permanent Tank EXP; pending Common reward survives reload; dialogue comes before claim; no early recruitment.");
        }
        static void Claim(BattleGame game)
        {
            var s=screen.Simulation;var member=s.rescuedMember;var ui=UnityEngine.Object.FindObjectOfType<CrewRecruitmentRewardUI>();
            ui.transform.Find("Common Crew Card/Claim").GetComponent<Button>().onClick.Invoke();
            Require(game.Crew.IsRecruited(member)&&game.Crew.Get(member.role)==member&&game.Crew.CommonCardCount(member)==1,"Claim must grant exactly one Common card and assign crew.");
            Require(!game.Crew.ClaimCommonRecruit(member.id)&&!game.Crew.QueueCommonRecruit(member,s.runId,s.stageIndex),"Claim/reward callbacks must not duplicate the card.");
            var loaded=new PlayerCrew(game.Frontend.crew);Require(loaded.IsRecruited(member)&&loaded.Get(member.role)==member&&loaded.CommonCardCount(member)==1,"Common ownership/assignment must persist.");
            var profile=new PlayerTankProfile(s.config);int xp=profile.TotalXP;Require(!profile.Award(s.runId,s.StageExperienceReward(profile))&&profile.TotalXP==xp,"Stage EXP must not be awarded twice.");
            Pass("Common card claimed once, ownership/loadout survive reload, duplicate card and EXP rejected.");
        }
        static void Build(string state,BattleGame game)
        {
            if(state=="stage1-unlocks-mg")
            {
                screen=HtmlBattleScreen.Begin(game,0);Freeze();
                untouchedRoster=PlayerPrefs.GetString("MegaTanks.Crew.Loadout","");untouchedRecruitment=PlayerPrefs.GetString(PlayerCrew.RecruitmentSaveKey,"");
                Require(game.Crew.Get(CrewRole.Gunner)!=null&&game.Crew.Get(CrewRole.Driver)!=null&&game.Crew.Get(CrewRole.SubGunner)==null,"Fresh story starts with Cannon/Driver, no MG crew.");
                Until(()=>screen.Simulation.phase=="wave");var s=screen.Simulation;
                Require(s.SlotUnlocked(0)&&s.player.weapons[0]&&!s.HasAssignedOperator(0)&&!s.CanFire(0),"First station unlocks mounted MG but cannot create/fire a crewless gun.");
                Pass("stage 1 entry upgrade opens MG; empty seat/gun remains inert.");
            }
            else if(state=="stage1-rescue"||state=="stage2-rescue")
            {
                var s=screen.Simulation;int ammo=s.ammo[s.RescueStory.weaponSlot];Until(()=>s.phase=="choice",checkOpening:s.stageIndex==0);
                Require(s.encounter=="rescue"&&s.ammo[s.RescueStory.weaponSlot]==ammo,"First encounter must rescue the missing gunner; empty gun never spends ammo.");
                Require(RescueEventArt.Cage!=null&&RescueEventArt.CrewPortrait!=null,"Rescue must use original cage and HTML-style cat portrait.");
                var skipped=new HtmlBattleSimulation(s.config,s.stageIndex);skipped.ConfigureCrew(game.Crew,game.Progression);skipped.ConfigureProfile(s.EffectiveLevel);skipped.encounter="rescue";skipped.phase="choice";skipped.Choose("skip");
                Require(skipped.phase=="depart"&&skipped.rescuedMember==null&&skipped.RescueNeeded&&skipped.EncounterType(s.config.rescue.firstEncounter+1)=="rescue","Skip must recruit nothing and offer rescue at a later stop.");
                Pass("stage "+(s.stageIndex+1)+" natural opening reaches RESCUE; no shots/ammo consumed by unstaffed weapon.");
            }
            else if(state=="stage1-rescue-walk")
            {
                ClickRescue();for(int i=0;i<35;i++)screen.Simulation.Tick(.05f);
                Require(screen.Simulation.rescuedFighter!=null&&!screen.Simulation.rescuedFighter.aboard&&!screen.Simulation.CanFire(0),"Rescued gunner must walk/board before MG can fire.");
                screen.transform.Find("Battle HUD/Pause").GetComponent<Button>().onClick.Invoke();
                typeof(HtmlBattleScreen).GetField("reviewFreeze",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(screen,false);
                float clock=screen.Simulation.rescueClock,x=screen.Simulation.rescuedFighter.x;
                typeof(HtmlBattleScreen).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(screen,null);
                Require(screen.Simulation.rescueClock==clock&&screen.Simulation.rescuedFighter.x==x,"Pause must freeze rescue walk and release timing.");
                Freeze();screen.transform.Find("Choice and Pause/Primary").GetComponent<Button>().onClick.Invoke();
                Pass("skip reoffers without recruiting; pause freezes rescue animation and walking.");
            }
            else if(state=="stage1-mg-online"||state=="stage2-mortar-online")
            {
                var s=screen.Simulation;if(s.stageIndex==1)ClickRescue();Until(()=>s.rescuedFighter?.aboard==true);
                var member=s.rescuedMember;Require(!game.Crew.IsRecruited(member)&&game.Crew.Get(member.role)==null,"Rescue grants a stage-scoped companion only.");
                if(s.stageIndex==0)Require(PlayerPrefs.GetString("MegaTanks.Crew.Loadout","")==untouchedRoster&&PlayerPrefs.GetString(PlayerCrew.RecruitmentSaveKey,"")==untouchedRecruitment,"Rescuing alone may not modify permanent crew saves.");
                var fresh=new HtmlBattleSimulation(s.config,s.stageIndex);fresh.ConfigureCrew(game.Crew,game.Progression);fresh.ConfigureProfile(s.baseTankLevel);
                Require(!fresh.HasAssignedOperator(s.RescueStory.weaponSlot)&&fresh.RescueNeeded,"Leaving before boss must discard temporary companion.");
                int slot=s.RescueStory.weaponSlot;Require(s.HasOperator(slot),"Gunner must operate after boarding.");
                if(slot==0||slot==2)
                {
                    int ammo=s.ammo[slot];string phase=s.phase;var target=s.Spawn(slot==0?"plane":"car");target.x=s.player.x+(slot==0?700:1350);target.hp=target.maxHP=999;s.phase="wave";s.Tick(.05f);s.phase=phase;
                    Require(s.ammo[slot]<ammo,"Rescued gunner must actually fire after boarding.");s.Damage(target,100000);
                }
                Pass("temporary companion boards without respawn/save; gun operates; abort/restart discards companion.");
            }
            else if(state=="stage1-dialogue"||state=="stage2-dialogue")Until(()=>screen.Simulation.Terminal,clearEnemies:true);
            else if(state=="stage1-common"||state=="stage2-common")CompleteDialogue(game,screen.Simulation.stageIndex);
            else if(state=="stage2-unlocks-mortar")
            {
                Claim(game);typeof(HtmlBattleScreen).GetMethod("StartStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(screen,new object[]{1});Freeze();
                Until(()=>screen.Simulation.phase=="wave");var s=screen.Simulation;
                Require(s.SlotUnlocked(2)&&!s.CanFire(2)&&s.HasOperator(0),"Stage 2 station opens Mortar, keeps permanent MG cat and leaves Mortar seat empty.");
                Pass("stage 2 entry upgrade opens next slot (Mortar); MG retained from stage 1 Common card.");
            }
            else if(state=="recruited-main"){Claim(game);screen.Exit();}
        }
    }
}
