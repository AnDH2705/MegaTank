using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    // Explicit internal visual QA for the map, node services, reveal, and victory screens.
    public static class StageScreensAudit
    {
        static int stage;
        static double since;
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            var window = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            window.position = new Rect(50, 20, 1000, 600);
            MegaTanksSetup.SetLandscapeGameView(window); window.Show(); window.Focus();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; since = EditorApplication.timeSinceStartup; EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static void Capture(string name) { Directory.CreateDirectory("Checks"); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/" + name + ".png")); }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance; var progress = game.Stage;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    game.StartBattle(); StageProgressionAudit.DisableWeapons(game); StageProgressionAudit.ClearWave(game); game.Rewards.ChooseReward(0); Next();
                }
                else if (elapsed > .4)
                {
                    if (stage == 1) Capture("StageMap");
                    else if (stage == 2) StageProgressionAudit.EnterType(game, StageNodeType.Supply);
                    else if (stage == 3) Capture("StageSupply");
                    else if (stage == 4) { progress.LeaveService(); StageProgressionAudit.EnterType(game, StageNodeType.Repair); }
                    else if (stage == 5) Capture("StageRepair");
                    else if (stage == 6) { progress.LeaveService(); StageProgressionAudit.EnterType(game, StageNodeType.Random); }
                    else if (stage == 7) Capture("StageReveal");
                    else if (stage == 8)
                    {
                        progress.Current.revealedType = StageNodeType.Battle; progress.ContinueReveal(); StageProgressionAudit.DisableWeapons(game);
                        StageProgressionAudit.ClearWave(game); game.Rewards.ChooseReward(0);
                        while (progress.Mode != StageMode.Victory)
                        {
                            StageProgressionAudit.EnterNextBattle(game); StageProgressionAudit.ClearWave(game);
                            if (progress.Mode != StageMode.Victory) game.Rewards.ChooseReward(0);
                        }
                    }
                    else if (stage == 9) Capture("StageVictory");
                    else if (stage == 10)
                    {
                        foreach (string name in new[] { "StageMap", "StageSupply", "StageRepair", "StageReveal", "StageVictory" })
                            if (!File.Exists("Checks/" + name + ".png")) throw new Exception("Stage UI screenshot missing: " + name);
                        Debug.Log("MEGATANKS_STAGE_UI_OK: map, supply, repair, reveal and victory screens captured."); Finish(0); return;
                    }
                    Next();
                }
                if (elapsed > 30) throw new Exception("Stage UI audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}
