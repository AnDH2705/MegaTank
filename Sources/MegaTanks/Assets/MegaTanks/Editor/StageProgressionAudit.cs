using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class StageProgressionAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static int waveBefore, mgBefore;
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; errors = false; since = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        public static void DisableWeapons(BattleGame game) { game.MachineGun.enabled = game.MainGun.enabled = game.Mortar.enabled = false; }
        public static void ClearWave(BattleGame game)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) if (enemy.Alive) enemy.Damage(1000000);
        }
        public static void EnterNextBattle(BattleGame game) { EnterType(game, StageNodeType.Battle); }
        public static void EnterType(BattleGame game, StageNodeType type)
        {
            var node = game.Stage.Route.Get(game.Stage.Current.next[0]);
            if (node.type != StageNodeType.Boss) node.type = type;
            Require(game.Stage.SelectNode(node.id), "Only the connected next node should be selectable.");
            DisableWeapons(game);
        }
        static void CheckMaps(StageDefinition definition)
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                var map = StageRoute.Generate(definition, seed);
                Require(map.rounds.Count == definition.rounds && map.rounds[0].Count == 1 && map.rounds[0][0].type == StageNodeType.Battle
                    && map.rounds.Last().Count == 1 && map.rounds.Last()[0].type == StageNodeType.Boss, "Stage endpoints must be a single battle and boss.");
                var reachable = new HashSet<int> { map.rounds[0][0].id };
                for (int column = 0; column < map.rounds.Count - 1; column++)
                {
                    var from = map.rounds[column]; var to = map.rounds[column + 1];
                    if (column > 0) Require(from.Count >= 2 && from.Count <= 4 && from.All(node => node.type != StageNodeType.Boss), "Intermediate rounds need 2-4 non-boss nodes.");
                    foreach (var node in from)
                    {
                        Require(reachable.Contains(node.id) && node.next.Count > 0 && node.next.Distinct().Count() == node.next.Count, "Every node must be reachable and have unique outgoing paths.");
                        float nearest = to.Min(end => Mathf.Abs(end.height - node.height));
                        Require(node.next.Any(id => Mathf.Abs(Mathf.Abs(map.Get(id).height - node.height) - nearest) < .00001f), "Every node must connect to a nearest next-round node.");
                        foreach (int id in node.next)
                        {
                            var end = map.Get(id);
                            Require(end.round == column + 1, "Connections must advance exactly one round."); reachable.Add(id);
                            foreach (var other in from)
                                foreach (int otherId in other.next)
                                    Require((node.lane - other.lane) * (end.lane - map.Get(otherId).lane) >= 0, "Connections between columns must never cross.");
                        }
                    }
                    Require(to.All(node => reachable.Contains(node.id)), "No next-round node may be unreachable.");
                }
                Require(map.rounds[map.rounds.Count - 2].All(node => node.next.Contains(map.rounds.Last()[0].id)), "Every penultimate node must lead to the boss.");
            }
            var first = StageRoute.Generate(definition, 10427); var second = StageRoute.Generate(definition, 10427);
            Require(string.Join(";", first.rounds.SelectMany(round => round).Select(node => node.id + ":" + node.type + ":" + node.height + ":" + string.Join(",", node.next)))
                == string.Join(";", second.rounds.SelectMany(round => round).Select(node => node.id + ":" + node.type + ":" + node.height + ":" + string.Join(",", node.next))), "A fixed map seed must reproduce the same route.");
        }
        static void Empty(AutoTurret gun) { while (gun.TryConsumeRound()) { } }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance; var progress = game.Stage;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    CheckMaps(progress.Definition);
                    game.StartBattle(); DisableWeapons(game);
                    Require(progress.Route.rounds.Count == 10 && progress.Current.round == 0 && game.Wave == 1 && progress.Mode == StageMode.Combat, "Round one must begin with a battle.");
                    Require(!progress.SelectNode(progress.Route.rounds.Last()[0].id), "The player cannot skip directly to the boss.");
                    game.DamageHull(40); ClearWave(game);
                    Require(progress.Mode == StageMode.Reward && game.Rewards.Offers.Length == 3 && !game.Running, "Battle victory must pause for three reward choices.");
                    game.Rewards.Offers[0].card = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath).cards.First(card => card.id == "1");
                    game.Rewards.ChooseReward(0);
                    Require(progress.Mode == StageMode.Map && game.Wave == 1 && !game.Rewards.ChooseReward(0), "A reward must open the map once without spawning another wave.");
                    waveBefore = game.Wave; game.SetStance(TankStance.Aggressive); Next();
                }
                else if (stage == 1 && elapsed > .2)
                {
                    Require(game.Wave == waveBefore && game.TankVelocity == Vector2.zero && !game.Running, "The route map must freeze combat and wait for a node selection.");
                    EnterType(game, StageNodeType.Supply);
                    Require(progress.Mode == StageMode.Service && game.Wave == waveBefore, "Supply nodes must not spawn enemies.");
                    Require(progress.FreeServiceCard.effect == CardEffect.Resupply && progress.FreeServiceCard.rarity == CardRarity.Normal
                        && progress.PaidServiceCard.effect == CardEffect.Resupply && progress.FreeServiceCard.id != progress.PaidServiceCard.id, "Supply offers must be distinct cards of the right effect, with Normal free.");
                    Empty(game.MachineGun); Empty(game.MainGun);
                    int capacity = game.Mortar.Stats.reserveCapacity; game.Mortar.Stats.reserveCapacity = 0; game.Mortar.ResetWeapon(); game.Mortar.Stats.reserveCapacity = capacity;
                    Require(progress.ClaimFreeService() && !progress.ClaimFreeService(), "The free service card can be claimed only once.");
                    Require(game.MachineGun.Ammo == Mathf.CeilToInt(game.MachineGun.ReserveCapacity * .33f)
                        && game.MainGun.Ammo == Mathf.CeilToInt(game.MainGun.ReserveCapacity * .33f)
                        && game.Mortar.Ammo == Mathf.CeilToInt(capacity * .33f), "Free resupply must restore all weapons correctly.");
                    Require(!progress.BuyService(), "A service purchase must fail when scraps are insufficient.");
                    game.Rewards.AddScraps(1000); int scraps = game.Rewards.Scraps, price = progress.PaidServiceCard.price;
                    Require(progress.BuyService() && game.Rewards.Scraps == scraps - price && !progress.BuyService(), "Paid service must charge its catalog price exactly once.");
                    Require(progress.LeaveService() && !progress.LeaveService(), "A service node can be completed only once.");
                    EnterType(game, StageNodeType.Repair);
                    Require(progress.FreeServiceCard.effect == CardEffect.Repair && progress.PaidServiceCard.effect == CardEffect.Repair, "Repair nodes need repair cards.");
                    float hp = game.Hull; mgBefore = game.MachineGun.Ammo;
                    Require(progress.ClaimFreeService() && game.Hull > hp && game.MachineGun.Ammo == mgBefore, "Repair must restore hull without refilling ammunition.");
                    game.Rewards.AddScraps(1000); Require(progress.BuyService() && game.Hull == game.MaxHull, "Paid repairs must cap at maximum hull.");
                    progress.LeaveService();
                    var hidden = progress.Route.Get(progress.Current.next[0]); hidden.type = StageNodeType.Random;
                    Require(!hidden.revealedType.HasValue, "Unknown nodes must stay hidden before entry.");
                    progress.SelectNode(hidden.id);
                    Require(progress.Mode == StageMode.Reveal && hidden.revealedType.HasValue && hidden.EncounterType != StageNodeType.Boss && hidden.EncounterType != StageNodeType.Random, "Unknown entry must reveal a non-boss encounter.");
                    hidden.revealedType = StageNodeType.HardBattle; progress.ContinueReveal(); DisableWeapons(game);
                    progress.Definition.waveBreakSeconds = .1f;
                    Require(progress.EncounterWave == 1 && progress.EncounterWaves == 3, "Hard battles must contain three waves.");
                    ClearWave(game);
                    Require(progress.Mode == StageMode.WaveBreak && !game.Rewards.IsOpen, "Hard wave one must continue without giving a card or map choice."); Next();
                }
                else if (stage == 2 && elapsed > .2)
                {
                    Require(progress.Mode == StageMode.Combat && progress.EncounterWave == 2, "Hard wave two must spawn automatically.");
                    ClearWave(game); Require(progress.Mode == StageMode.WaveBreak && !game.Rewards.IsOpen, "Hard wave two must also continue without a reward."); Next();
                }
                else if (stage == 3 && elapsed > .2)
                {
                    Require(progress.Mode == StageMode.Combat && progress.EncounterWave == 3, "Hard wave three must spawn automatically.");
                    ClearWave(game); Require(progress.Mode == StageMode.Reward && game.Rewards.Offers.Length == 3, "The final hard wave must award one card selection.");
                    game.Rewards.ChooseReward(0);
                    while (progress.Current.round < progress.Route.rounds.Count - 2)
                    {
                        EnterNextBattle(game); ClearWave(game);
                        Require(game.Rewards.Mode == IntermissionMode.Reward && game.Rewards.Offers.Length == 3
                            && game.Rewards.Offers.Select(offer => offer.card.id).Distinct().Count() == 3, "Every battle node, including wave five, must offer three unique cards instead of an automatic shop.");
                        game.Rewards.ChooseReward(0);
                    }
                    var boss = progress.Route.rounds.Last()[0];
                    Require(progress.Current.next.Contains(boss.id) && progress.SelectNode(boss.id) && progress.EncounterWaves == 1, "The final round must enter a single placeholder boss wave.");
                    DisableWeapons(game); ClearWave(game);
                    Require(progress.Mode == StageMode.Victory && progress.Current.completed && !game.Running && !game.Rewards.IsOpen, "Boss victory must show the dedicated victory screen.");
                    game.StartBattle(); DisableWeapons(game);
                    Require(progress.Current.round == 0 && progress.Mode == StageMode.Combat && game.Wave == 1 && game.Rewards.Scraps == 0
                        && game.Hull == game.MaxHull && game.MachineGun.Ammo == game.MachineGun.ReserveCapacity, "Restart must reset the route and run resources.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_STAGE_OK: 1000 connected noncrossing maps, nearest links/reachability, 10 rounds, unique battle rewards, supply/repair pricing and one-time claims, hidden reveal, three-wave hard battle, boss victory and restart.");
                    Finish(0);
                }
                if (elapsed > 30) throw new Exception("Stage progression audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}
