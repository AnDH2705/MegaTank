using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    // Explicit visual QA entry point. Routine progression checks do not render screenshots.
    public static class RewardScreenAudit
    {
        static int stage;
        static double since;
        static CardCatalog catalog;
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            var window = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            window.position = new Rect(50, 20, 1000, 600);
            MegaTanksSetup.SetLandscapeGameView(window);
            window.Show(); window.Focus();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; since = EditorApplication.timeSinceStartup;
                EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void SetOffers(params string[] ids)
        {
            for (int i = 0; i < ids.Length; i++) BattleGame.Instance.Rewards.Offers[i].card = catalog.cards.Single(card => card.id == ids[i]);
        }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath);
                    game.StartBattle(); game.Mortar.enabled = false;
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) { game.RemoveEnemy(enemy, false); enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
                    game.Rewards.OpenForWave(1);
                    SetOffers("13", "11", "4");
                    Directory.CreateDirectory("Checks");
                    Next();
                }
                else if (stage == 1 && elapsed > .6) { ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/Reward.png")); Next(); }
                else if (stage == 2 && elapsed > 1)
                {
                    game.Rewards.ResetRun(); game.Rewards.AddScraps(2400); game.Rewards.OpenForWave(5);
                    SetOffers("13", "2", "7", "12", "16", "14", "18", "8");
                    Next();
                }
                else if (stage == 3 && elapsed > .6) { ScreenCapture.CaptureScreenshot(Path.GetFullPath("Checks/Shop.png")); Next(); }
                else if (stage == 4 && elapsed > 1)
                {
                    if (!File.Exists("Checks/Reward.png") || !File.Exists("Checks/Shop.png")) throw new Exception("UI QA screenshots were not captured.");
                    Debug.Log("MEGATANKS_REWARD_UI_CAPTURED"); Finish(0);
                }
                if (elapsed > 20) throw new Exception("UI QA timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int result)
        {
            EditorApplication.update -= Check;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.Exit(result);
        }
    }
}
