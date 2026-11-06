using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class ProgressionAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static int mgShots, cannonShots;
        static CardCatalog catalog;
        static float baseMgDamage, baseCannonDamage;
        static int startingMgAmmo, startingCannonAmmo;
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; errors = false;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static CardDefinition Card(string id) => catalog.cards.Single(card => card.id == id);
        static void Empty(AutoTurret gun) { while (gun.TryConsumeRound()) { } }
        static void RequireUniqueOffers(CardRewardSystem rewards)
        {
            Require(rewards.Offers.Select(offer => offer.card.id).Distinct().Count() == rewards.Offers.Length,
                "A reward selection or shop must never contain identical cards.");
        }
        static int ClearWave(BattleGame game)
        {
            int scraps = 0;
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
            {
                if (!enemy.Alive) continue;
                scraps += enemy.ScrapsReward;
                enemy.Damage(1000000);
            }
            return scraps;
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath);
                    catalog.Validate();
                    Require(catalog.cards.Length == 18 && catalog.cards.All(card => card.art != null && card.art.rect.xMax <= card.art.texture.width && card.art.rect.yMax <= card.art.texture.height), "All 18 sheet cards need correctly sliced art.");
                    Require(Card("4").price == 1000 && Card("4").fraction == 1 && Card("13").price == 100 && Mathf.Approximately(Card("13").fraction, .33f), "Catalog must preserve spreadsheet prices and percentages.");
                    int[] counts = new int[4];
                    for (int i = 0; i < 100000; i++) counts[(int)CardCatalog.RollRarity((i + .5f) / 100000)]++;
                    Require(counts[0] == 50000 && counts[1] == 30000 && counts[2] == 15000 && counts[3] == 5000, "Rarity thresholds must match 50/30/15/5 percent.");
                    var randomState = UnityEngine.Random.state;
                    try
                    {
                        UnityEngine.Random.InitState(74125);
                        for (int sample = 0; sample < 2000; sample++)
                        {
                            foreach (int count in new[] { 3, 8, 18 })
                            {
                                var draw = catalog.RollUniqueCards(count);
                                Require(draw.Length == count && draw.Select(card => card.id).Distinct().Count() == count,
                                    "Unique draws must fill all slots, even after rarity pools are exhausted.");
                            }
                        }
                        bool rejected = false;
                        try { catalog.RollUniqueCards(19); }
                        catch (ArgumentOutOfRangeException) { rejected = true; }
                        Require(rejected, "Oversized unique offers must fail clearly instead of looping or duplicating.");
                    }
                    finally { UnityEngine.Random.state = randomState; }
                    game.StartBattle(); game.Mortar.enabled = false;
                    startingMgAmmo = game.MachineGun.ReserveCapacity; startingCannonAmmo = game.MainGun.ReserveCapacity;
                    Require(game.MachineGun.Ammo == startingMgAmmo && game.MainGun.Ammo == startingCannonAmmo, "Run must start with configured reserves.");
                    baseMgDamage = game.MachineGun.DamagePerShot; baseCannonDamage = game.MainGun.DamagePerShot;
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) { game.RemoveEnemy(enemy, false); enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
                    var flyer = game.SpawnEnemy("FlyingEnemy1", game.TankAimPoint + new Vector2(12, 3));
                    var ground = game.SpawnEnemy("GroundEnemy1", game.TankAimPoint + new Vector2(14, -2));
                    flyer.Weapon.enabled = ground.Weapon.enabled = false;
                    Require(flyer.ScrapsReward == 20 && ground.ScrapsReward == 30, "First-wave scraps must start at 20 and 30.");
                    game.MachineGun.ConfigureAmmunition(2); game.MainGun.ConfigureAmmunition(1);
                    Next();
                }
                else if (stage == 1 && elapsed > .7)
                {
                    Require(game.MachineGun.ShotsFired == 2 && game.MainGun.ShotsFired == 1 && game.MachineGun.Ammo == 0 && game.MainGun.Ammo == 0, "Every actual salvo round must spend one bullet and stop at zero.");
                    mgShots = game.MachineGun.ShotsFired; cannonShots = game.MainGun.ShotsFired;
                    Next();
                }
                else if (stage == 2 && elapsed > Math.Max(game.MachineGun.ReloadTime, game.MainGun.ReloadTime) + .25)
                {
                    Require(game.MachineGun.ShotsFired == mgShots && game.MainGun.ShotsFired == cannonShots, "Reload completion must not refill empty reserves.");
                    game.MachineGun.Resupply(.5f);
                    Next();
                }
                else if (stage == 3 && elapsed > .25)
                {
                    Require(game.MachineGun.ShotsFired == mgShots + 1, "Resupplied ammo must allow a depleted gun to fire again.");
                    game.MachineGun.enabled = game.MainGun.enabled = false;
                    game.MachineGun.ConfigureAmmunition(120); game.MainGun.ConfigureAmmunition(20);
                    Empty(game.MachineGun); Empty(game.MainGun);
                    game.ApplyCardEffect(Card("13"));
                    Require(game.MachineGun.Ammo == 40 && game.MainGun.Ammo == 7, "Normal resupply must restore 33 percent, rounded up.");
                    game.ApplyCardEffect(Card("14"));
                    Require(game.MachineGun.Ammo == 120 && game.MainGun.Ammo == 20, "Resupply must cap at reserve capacity.");
                    game.ApplyCardEffect(Card("1")); game.ApplyCardEffect(Card("8"));
                    Require(Mathf.Approximately(game.MainGun.DamagePerShot, baseCannonDamage * 1.1f) && Mathf.Approximately(game.MachineGun.DamagePerShot, baseMgDamage * 2), "Damage upgrades must affect the intended gun.");
                    game.DamageHull(30); game.ApplyCardEffect(Card("9"));
                    Require(Mathf.Approximately(game.MaxHull, 110) && Mathf.Approximately(game.Hull, 80), "HP upgrade must increase max/current HP by the added capacity.");
                    game.ApplyCardEffect(Card("1")); game.ApplyCardEffect(Card("2"));
                    game.ApplyCardEffect(Card("5")); game.ApplyCardEffect(Card("6"));
                    Require(Mathf.Approximately(game.MainGun.DamagePerShot, baseCannonDamage * 1.4f)
                        && Mathf.Approximately(game.MachineGun.DamagePerShot, baseMgDamage * 2.3f),
                        "Repeated and mixed-rarity damage cards must add percentages of starting damage independently for each gun.");
                    game.ApplyCardEffect(Card("9")); game.ApplyCardEffect(Card("10"));
                    Require(Mathf.Approximately(game.MaxHull, 140) && Mathf.Approximately(game.Hull, 110),
                        "Repeated and mixed-rarity HP cards must add percentages of starting HP and preserve missing health.");
                    game.ApplyCardEffect(Card("16")); Require(game.Hull == game.MaxHull, "Repair must recover HP and cap at maximum.");
                    int expected = ClearWave(game);
                    Require(game.Rewards.Scraps == expected && expected == 50 && game.Rewards.Mode == IntermissionMode.Reward && game.Rewards.Offers.Length == 3 && !game.Running, "Wave one must award scraps and pause for three free options.");
                    RequireUniqueOffers(game.Rewards);
                    game.MachineGun.TryConsumeRound(); game.MainGun.TryConsumeRound();
                    game.Rewards.Offers[0].card = Card("1");
                    Require(game.Rewards.ChooseReward(0) && game.Stage.Mode == StageMode.Map && !game.Rewards.ChooseReward(0), "Exactly one free reward must open the route map.");
                    StageProgressionAudit.EnterNextBattle(game);
                    Require(game.MachineGun.Ammo == 119 && game.MainGun.Ammo == 19, "Advancing a wave must preserve ammo rather than refilling reserves.");
                    for (int wave = 2; wave <= 5; wave++)
                    {
                        Require(game.Wave == wave, "Wave order must remain sequential.");
                        foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                            if (enemy.Alive) Require(enemy.ScrapsReward == (enemy.Kind == EnemyKind.Flyer ? 20 + 2 * (wave - 1) : 30 + 3 * (wave - 1)), "Enemy scraps must scale with their own wave number.");
                        expected += ClearWave(game);
                        Require(game.Rewards.Scraps == expected, "Kill rewards must be credited exactly once.");
                        RequireUniqueOffers(game.Rewards);
                        if (wave < 5)
                        {
                            Require(game.Rewards.ChooseReward(0), "Battle nodes must offer a free reward.");
                            StageProgressionAudit.EnterNextBattle(game);
                        }
                    }
                    Require(game.Rewards.Mode == IntermissionMode.Reward && game.Rewards.Offers.Length == 3 && !game.Running,
                        "Wave five must retain three unique battle rewards; stage nodes replace the automatic shop.");
                    int savedScraps = game.Rewards.Scraps;
                    Require(game.Rewards.ChooseReward(0) && game.Stage.Mode == StageMode.Map && game.Rewards.Scraps == savedScraps,
                        "Taking a reward must preserve scraps and return to the map.");
                    game.StartBattle(); game.Mortar.enabled = false;
                    Require(game.Wave == 1 && game.Rewards.Scraps == 0 && game.MaxHull == 100 && game.Hull == 100 && game.MachineGun.Ammo == startingMgAmmo && game.MainGun.Ammo == startingCannonAmmo
                        && Mathf.Approximately(game.MachineGun.DamagePerShot, baseMgDamage) && Mathf.Approximately(game.MainGun.DamagePerShot, baseCannonDamage), "Restart must reset currency, upgrades, HP and ammo.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_PROGRESSION_OK: additive upgrades, reset, 6000 unique draws, stage battle rewards/map return, ammo, rarity odds, repair/resupply and scaled scraps.");
                    Finish(0);
                }
                if (stage > 0 && elapsed > 30) throw new Exception("Progression audit timed out.");
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
