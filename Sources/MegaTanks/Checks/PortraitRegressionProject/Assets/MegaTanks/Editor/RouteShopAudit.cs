using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class RouteShopAudit
    {
        static StageRewardSaveScope saves;
        static bool errors, oldOptions;
        static EnterPlayModeOptions options;
        static int phase;
        static double deadline;
        static StageDefinition stage;
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            var definition = ScriptableObject.CreateInstance<StageDefinition>();
            try
            {
                foreach (int length in new[] { 2, 3, 5, 10, 25 })
                foreach (int weight in new[] { 0, 10, 10000 })
                {
                    definition.rounds = length; definition.shopWeight = weight;
                    for (int seed = 0; seed < 200; seed++)
                    {
                        var map = StageRoute.Generate(definition, seed);
                        Require(map.rounds.Count == Math.Max(3, length), "Minimum length reserves opening battle, shop, boss.");
                        Require(map.rounds[0].Single().type == StageNodeType.Battle && map.rounds.Last().Single().type == StageNodeType.Boss, "Endpoints preserved.");
                        Require(map.rounds[map.rounds.Count - 2].All(n => n.type == StageNodeType.Shop), "Every path must visit a pre-boss shop.");
                        int last = -3;
                        var reachable = new System.Collections.Generic.HashSet<int> { 0 };
                        for (int round = 0; round < map.rounds.Count; round++)
                        {
                            var column = map.rounds[round];
                            Require(column.All(n => reachable.Contains(n.id)), "All nodes reachable.");
                            if (round > 0 && round < map.rounds.Count - 1) Require(column.Count >= 2 && column.Count <= 4, "Intermediate lane counts preserved.");
                            if (round < map.rounds.Count - 2 && column.Any(n => n.type == StageNodeType.Shop))
                            {
                                Require(weight > 0 && round - last >= 3, "Random shops respect weight and three-step cooldown."); last = round;
                            }
                            foreach (var node in column)
                            foreach (int id in node.next)
                            {
                                var end = map.Get(id); reachable.Add(id);
                                Require(end.round == round + 1, "Links advance one round.");
                                foreach (var other in column) foreach (int otherId in other.next)
                                    Require((node.lane - other.lane) * (end.lane - map.Get(otherId).lane) >= 0, "No crossed links.");
                            }
                        }
                        var again = StageRoute.Generate(definition, seed);
                        Require(string.Join(",", map.rounds.SelectMany(c => c).Select(n => n.type)) == string.Join(",", again.rounds.SelectMany(c => c).Select(n => n.type)), "Seeded shops are deterministic.");
                    }
                }
            }
            finally { Object.DestroyImmediate(definition); }
            Debug.Log("ROUTE_SHOP_MAP_PASS: 3000 maps, spacing, guaranteed shop, weights, reachability, noncrossing paths.");
            saves = new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            oldOptions = EditorSettings.enterPlayModeOptionsEnabled; options = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived += Log; EditorApplication.playModeStateChanged += State;
            deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.isPlaying = true;
        }
        static void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors = true; }
        static void State(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += Tick;
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                saves?.Dispose(); EditorSettings.enterPlayModeOptionsEnabled = oldOptions; EditorSettings.enterPlayModeOptions = options;
                EditorApplication.Exit(errors ? 1 : 0);
            }
        }
        static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Shop audit timeout.");
                var game = BattleGame.Instance;
                if (game == null || game.UI == null) return;
                if (phase == 0)
                {
                    stage = Object.Instantiate(game.Stage.Definition); stage.rounds = 3; stage.hardBattleWeight = 0; game.SelectStage(stage); game.StartBattle();
                    StageProgressionAudit.DisableWeapons(game); StageProgressionAudit.ClearWave(game);
                    Require(game.Rewards.Mode == IntermissionMode.Reward, "Opening victory still gives card reward.");
                    StageProgressionAudit.EnterNextBattle(game);
                    StageProgressionAudit.ClearWave(game);
                    game.Rewards.TrySpendScraps(game.Rewards.Scraps);
                    Require(game.Stage.Mode == StageMode.Shop && game.Rewards.Mode == IntermissionMode.Shop && game.EnemyCount == 0 && game.Wave == 2 && !game.Running, "The pre-boss vendor pauses combat without spawning a wave.");
                    phase++; return;
                }
                if (phase == 1)
                {
                    var offers = game.Rewards.Offers;
                    Require(offers.Length == 8 && offers.Select(o => o.card.id).Distinct().Count() == 8, "Eight distinct cards.");
                    Require(offers[0].card.effect == CardEffect.Repair && offers[0].card.rarity == CardRarity.Normal && offers[1].card.effect == CardEffect.Resupply && offers[1].card.rarity == CardRarity.Normal, "Two fixed Normal service cards.");
                    Require(game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Shop).gameObject.activeInHierarchy, "Prefab shop visible.");
                    Require(!game.Rewards.Buy(0), "Insufficient scraps blocks purchase.");
                    game.Rewards.AddScraps(100000);
                    for (int i = 0; i < offers.Length; i++)
                    {
                        int before = game.Rewards.Scraps;
                        Require(game.Rewards.Buy(i) && game.Rewards.Scraps == before - offers[i].card.price && !game.Rewards.Buy(i), "Purchases spend exact price once.");
                    }
                    game.Rewards.LeaveShop();
                    Require(game.Stage.Mode == StageMode.Advancing && game.Stage.Current.completed && !game.Rewards.IsOpen, "Leaving the shop starts travel.");
                    int balance = game.Rewards.Scraps; game.Rewards.LeaveShop();
                    Require(game.Rewards.Scraps == balance && !game.Stage.SelectNode(game.Stage.Current.id), "Cannot reenter or reset shop.");
                    StageProgressionAudit.EnterNextBattle(game);
                    Require(game.Stage.Current.type == StageNodeType.Boss && game.Stage.Mode == StageMode.Combat, "Shop leads directly to boss.");
                    StageProgressionAudit.DisableWeapons(game); game.ReturnHome();
                    Require(!errors, "No runtime errors.");
                    Debug.Log("ROUTE_SHOP_PLAY_PASS: visible vendor shop, fixed and unique stock, purchases, repeat protection, and travel to the boss.");
                    Finish();
                }
            }
            catch (Exception e) { errors = true; Debug.LogException(e); Finish(); }
        }
        static void Finish()
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
            EditorApplication.isPlaying = false;
        }
    }
}
