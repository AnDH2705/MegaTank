using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class StageRewardsAudit
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Balances(PlayerWallet wallet,int parts,int food,int gems,string message)
            =>Require(wallet.Parts==parts&&wallet.PetFood==food&&wallet.Gems==gems,message+" actual "+wallet.Parts+"/"+wallet.PetFood+"/"+wallet.Gems);
        static void Model()
        {
            string wkey="MegaTanks.StageRewardAudit.Wallet."+Guid.NewGuid(),key=wkey+".ledger";
            var stage=ScriptableObject.CreateInstance<StageDefinition>();StageCompletionRewards rewards=null;
            try
            {
                var wallet=new PlayerWallet(wkey);rewards=new StageCompletionRewards(wallet,key);
                Require(rewards.CompleteRun()==null,"No reward without an active run.");
                stage.InitializeStage(1);rewards.BeginRun(stage);var first=rewards.CompleteRun();
                Balances(wallet,600,600,100,"Stage 1 first clear includes both payouts.");
                Require(first.firstClear&&first.paid&&first.repeat.parts==100&&first.bonus.parts==500,"First receipt breakdown.");
                Require(ReferenceEquals(first,rewards.CompleteRun()),"Repeated callbacks return the original receipt.");
                Balances(wallet,600,600,100,"Repeated callbacks never duplicate credits.");
                rewards.BeginRun(stage);var repeat=rewards.CompleteRun();Require(!repeat.firstClear&&repeat.bonus.gems==0,"Replay has no first bonus.");
                Balances(wallet,700,700,100,"Repeat clear payout.");
                rewards.Dispose();rewards=new StageCompletionRewards(new PlayerWallet(wkey),key);wallet=new PlayerWallet(wkey);
                Require(rewards.HasCleared(1),"First-completion history persists.");
                rewards.Dispose();rewards=new StageCompletionRewards(wallet,key);
                stage.InitializeStage(2);rewards.BeginRun(stage);rewards.CompleteRun();
                Balances(wallet,1360,1360,200,"Stage 2 uses 110 repeat plus 550 bonus.");
                stage.InitializeStage(10);rewards.BeginRun(stage);rewards.CompleteRun();
                Balances(wallet,2500,2500,300,"Stage 10 uses 190 repeat plus 950 bonus.");
                stage.stageName="Renamed stage";rewards.BeginRun(stage);rewards.CompleteRun();
                Balances(wallet,2690,2690,300,"Renaming cannot reset eligibility.");
                stage.InitializeStage(3);rewards.BeginRun(stage);rewards.EndRun();Require(rewards.CompleteRun()==null&&!rewards.HasCleared(3),"Aborted runs do not pay or consume first completion.");
                stage.InitializeStage(7);stage.firstClearPartsBase=900;stage.firstClearPetFoodBase=800;stage.firstClearResourcesPerStage=25;stage.firstClearGems=75;
                rewards.BeginRun(stage);var custom=rewards.CompleteRun();
                Require(custom.Total.parts==1210&&custom.Total.petFood==1110&&custom.Total.gems==75,"First reward defaults can be edited per stage.");
                rewards.Dispose();PlayerPrefs.DeleteKey(wkey);PlayerPrefs.DeleteKey(key);
                wallet=new PlayerWallet(wkey);wallet.TryAdd(CurrencyType.Parts,int.MaxValue);
                rewards=new StageCompletionRewards(wallet,key);stage.firstClearPartsBase=stage.firstClearPetFoodBase=500;stage.firstClearResourcesPerStage=50;stage.firstClearGems=100;stage.InitializeStage(1);
                rewards.BeginRun(stage);var deferred=rewards.CompleteRun();
                Require(!deferred.paid&&rewards.PendingCount==1&&rewards.HasCleared(1),"Full wallet preserves one earned first reward.");
                Balances(wallet,int.MaxValue,0,0,"A blocked reward never partially credits.");
                rewards.Dispose();wallet=new PlayerWallet(wkey);rewards=new StageCompletionRewards(wallet,key);
                Require(rewards.PendingCount==1,"Pending payout survives restart.");
                Require(wallet.TrySpend(CurrencyType.Parts,600),"Make space for deferred payout.");
                Balances(wallet,int.MaxValue,600,100,"Pending payout automatically credits once after spending.");
                Require(rewards.PendingCount==0,"Paid entitlement is removed.");
                rewards.CollectPending();Balances(wallet,int.MaxValue,600,100,"Repeated collection cannot duplicate credits.");
                rewards.Dispose();PlayerPrefs.SetString(key,"invalid json");
                rewards=new StageCompletionRewards(new PlayerWallet(wkey),key);Require(!rewards.HasCleared(1)&&rewards.PendingCount==0,"Malformed history recovers safely.");
                Debug.Log("STAGE_REWARDS_MODEL_PASS");
            }
            finally{rewards?.Dispose();Object.DestroyImmediate(stage);PlayerPrefs.DeleteKey(wkey);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
        static readonly string[] keys={PlayerWallet.DefaultSaveKey,StageCompletionRewards.DefaultSaveKey,PlaytestResources.SaveKey,PlaytestResources.ArmorSaveKey,"MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar","MegaTanks.Equipment.Armor"};
        static readonly Dictionary<string,string> saved=new Dictionary<string,string>();static readonly HashSet<string> existed=new HashSet<string>();
        static bool oldOptions,errors,hadBest;static int best;static EnterPlayModeOptions options;static StageDefinition testStage;static int phase;static double since;
        public static void Run()
        {
            try
            {
                StageRewardsSetup.Install();StageRewardsSetup.Install();Model();
                foreach(string key in keys){if(PlayerPrefs.HasKey(key))existed.Add(key);saved[key]=PlayerPrefs.GetString(key,"");}
                hadBest=PlayerPrefs.HasKey("MegaTanks.Best2D");best=PlayerPrefs.GetInt("MegaTanks.Best2D");
                PlayerPrefs.DeleteKey(PlayerWallet.DefaultSaveKey);PlayerPrefs.DeleteKey(StageCompletionRewards.DefaultSaveKey);
                PlayerPrefs.SetInt(PlaytestResources.SaveKey,1);PlayerPrefs.SetInt(PlaytestResources.ArmorSaveKey,1);PlayerPrefs.Save();
                oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
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
            if(EditorApplication.timeSinceStartup-since<.65)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;Require(game!=null&&game.UI!=null,"Game starts with stage reward service.");
                var victory=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Victory);
                switch(phase++)
                {
                    case 0:
                        testStage=Object.Instantiate(game.Stage.Definition);testStage.InitializeStage(1);testStage.rounds=2;testStage.randomizeSeed=false;
                        game.SelectStage(testStage);game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                        Balances(game.Wallet,0,0,0,"Starting a stage does not award anything.");break;
                    case 1:
                        StageProgressionAudit.ClearWave(game);Require(game.Stage.Mode==StageMode.Reward,"Opening battle still uses run reward cards.");
                        Balances(game.Wallet,0,0,0,"Intermediate waves do not pay stage rewards.");game.Rewards.ChooseReward(0);break;
                    case 2:
                        Require(game.Stage.Mode==StageMode.Map&&game.Stage.SelectNode(game.Stage.Current.next[0]),"Enter the connected final boss.");
                        StageProgressionAudit.DisableWeapons(game);break;
                    case 3:
                        StageProgressionAudit.ClearWave(game);Require(game.Stage.Mode==StageMode.Victory,"Final boss death enters victory.");
                        Balances(game.Wallet,600,600,100,"Actual first boss clear pays automatically.");
                        game.Stage.OnWaveCleared();game.StageRewards.CompleteRun();Balances(game.Wallet,600,600,100,"Duplicate clear notifications are harmless.");break;
                    case 4:
                        Require(victory.gameObject.activeInHierarchy&&victory.Get<Text>("Total Rewards/Parts/Amount").text=="+600","Victory displays the credited total.");
                        Require(victory.Get<Text>("First Completion Rewards/Gems/Amount").text=="+100","Victory shows the first-clear Gems.");
                        ItemProgressionAudit.Capture(game,"StageRewards-First");
                        ItemProgressionAudit.Press(victory.Get<Button>("Play Again"));StageProgressionAudit.DisableWeapons(game);break;
                    case 5:
                        StageProgressionAudit.ClearWave(game);game.Rewards.ChooseReward(0);break;
                    case 6:
                        Require(game.Stage.SelectNode(game.Stage.Current.next[0]),"Replay reaches the boss through the route.");StageProgressionAudit.DisableWeapons(game);break;
                    case 7:
                        StageProgressionAudit.ClearWave(game);Require(game.Stage.Mode==StageMode.Victory&&!game.StageRewards.LastReceipt.firstClear,"Replay is recognized.");
                        Balances(game.Wallet,700,700,100,"Replay only adds its repeat reward.");break;
                    case 8:
                        Require(victory.Get<Text>("Total Rewards/Parts/Amount").text=="+100"&&victory.Get<Text>("First Completion Rewards/Gems/Amount").text=="+0","Replay UI never advertises another first bonus.");
                        ItemProgressionAudit.Capture(game,"StageRewards-Repeat");ItemProgressionAudit.Press(victory.Get<Button>("Result Home"));break;
                    case 9:
                        CurrencyAudit.CheckCounters(game);Balances(new PlayerWallet(),700,700,100,"Wallet credit persists after Home.");
                        using(var loaded=new StageCompletionRewards(new PlayerWallet()))Require(loaded.HasCleared(1),"Actual first-clear flag survives reload.");
                        testStage.InitializeStage(2);game.SelectStage(testStage);game.StartBattle();StageProgressionAudit.DisableWeapons(game);game.DamageHull(game.MaxHull+1);
                        Require(!game.StageRewards.HasCleared(2),"Defeat does not claim first rewards.");Balances(game.Wallet,700,700,100,"Defeat pays nothing.");
                        game.ReturnHome();testStage.InitializeStage(3);game.SelectStage(testStage);game.StartBattle();game.ReturnHome();
                        Require(!game.StageRewards.HasCleared(3),"Returning Home mid-run pays nothing.");
                        Require(!errors,"No runtime errors.");Debug.Log("STAGE_REWARDS_PLAY_PASS");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
            foreach(var pair in saved){if(existed.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            if(saved.Count>0){if(hadBest)PlayerPrefs.SetInt("MegaTanks.Best2D",best);else PlayerPrefs.DeleteKey("MegaTanks.Best2D");}
            PlayerPrefs.Save();EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
