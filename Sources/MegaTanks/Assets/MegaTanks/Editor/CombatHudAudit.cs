using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks.Editor
{
    public static class CombatHudAudit
    {
        static StageRewardSaveScope saves;
        static double since;
        static int phase;
        static float before;
        static float eventX;
        static bool errors;
        public static void Run()
        {
            CombatHudSetup.Apply();saves=new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            Application.logMessageReceived+=Log;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Advance(BattleGame game,float seconds) => typeof(StageProgression).GetMethod("Advance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game.Stage,new object[]{seconds});
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.6)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;var hud=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.BattleHUD);
                var art=hud.GetComponent<CombatHudPresentation>();
                switch(phase++)
                {
                    case 0:
                        var stage=UnityEngine.Object.Instantiate(game.Stage.Definition);stage.rounds=3;stage.hardBattleWeight=0;
                        game.SelectStage(stage);game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);break;
                    case 1:
                        Require(art!=null&&hud.transform.Find("Stage horizon/Upcoming stops").GetComponent<RectMask2D>()!=null,"Upcoming stops must clip at horizon edges.");
                        Require(hud.Get<RectTransform>("Hull Anchor").anchoredPosition==new Vector2(30,-968),"HP stays at lower left.");
                        Require(hud.Get<Text>("cannon Value").text==game.MainGun.Ammo.ToString(),"Cannon reserve is bound to live ammo.");
                        Require(hud.Get<RectTransform>("cannon Value").anchoredPosition.x>850,"Ammo is beside HP.");
                        Require(hud.transform.Find("Hull Anchor/HP plate").GetComponent<CanvasRenderer>()!=null,"HP plate has a renderer.");
                        Require(hud.Get<Text>("Hull Anchor/Hull").color==Color.white,"HP value remains readable.");
                        Require(!hud.Get<Text>("scraps Value").gameObject.activeInHierarchy,"Legacy scrap amount is hidden.");
                        Require(!hud.transform.Find("Stage horizon/Upcoming stops/Stop 4").gameObject.activeSelf,"Distant boss stays outside upcoming window.");
                        ItemProgressionAudit.Capture(game,"CombatHUD-Battle");
                        StageProgressionAudit.ClearWave(game,false);Advance(game,.01f);
                        before=game.Stage.HudPosition;art.Refresh(game);eventX=((RectTransform)hud.transform.Find("Stage horizon/Upcoming stops/Stop 1")).anchoredPosition.x;break;
                    case 2:
                        Advance(game,.2f);
                        Require(game.Stage.HudPosition>before,"Stage indicator moves with camp approach.");
                        art.Refresh(game);Require(((RectTransform)hud.transform.Find("Stage horizon/Upcoming stops/Stop 1")).anchoredPosition.x<eventX,"Event icons traverse left as the tank travels.");
                        StageProgressionAudit.OpenCampRewards(game);if(game.Rewards.Mode==IntermissionMode.Shop)game.Rewards.LeaveShop();else game.Rewards.ChooseReward(0);
                        Advance(game,.5f);Require(game.Stage.HudPosition>=1.5f,"Departure advances past event.");
                        Advance(game,1f);StageProgressionAudit.DisableWeapons(game);break;
                    case 3:
                        Require(game.Stage.Current.round==1,"HUD changes do not alter encounter flow.");
                        Require(hud.transform.Find("Stage horizon/Upcoming stops/Stop 3/Caption").GetComponent<Text>().text=="BOSS","Boss appears in the final upcoming section.");
                        ItemProgressionAudit.Capture(game,"CombatHUD-BossAhead");
                        ItemProgressionAudit.Press(hud.Get<Button>("Pause"));break;
                    case 4:
                        Require(game.Paused,"Pause button retains behavior.");game.SetPaused(false);
                        StageProgressionAudit.ClearWave(game);game.Rewards.LeaveShop();Advance(game,1f);StageProgressionAudit.DisableWeapons(game);break;
                    case 5:
                        Require(game.Stage.Current.EncounterType==StageNodeType.Boss,"Final boss remains final encounter.");
                        ItemProgressionAudit.Capture(game,"CombatHUD-Boss");
                        StageProgressionAudit.ClearWave(game);Require(game.Stage.Mode==StageMode.Victory,"Boss clear still wins stage.");game.ReturnHome();
                        Require(!errors,"No runtime errors.");Debug.Log("COMBAT_HUD_PASS: clipped upcoming stops, approach/departure movement, live ammo, fixed HP, pause and boss victory.");Finish(0);break;
                }
            }
            catch(Exception error){Debug.LogException(error);Finish(1);}
        }
        static void Finish(int code)
        {EditorApplication.update-=Tick;saves?.Dispose();EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);}
    }
}
