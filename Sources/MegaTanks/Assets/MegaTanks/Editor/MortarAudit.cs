using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class MortarAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static EnemyEntity target;
        static Vector3 pausedShell;
        static Projectile2D shell;
        static int ammo, shots;
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
        static void Clear(BattleGame game)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
            { game.RemoveEnemy(enemy, false); enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
        }
        static EnemyEntity Dummy(BattleGame game, Vector2 position)
        {
            var data = game.EnemyDatabase.Get(EnemyKind.Ground);
            float health = data.baseHealth, growth = data.healthPerWave, speed = data.baseSpeed, bonus = data.maxSpeedBonus;
            try
            {
                data.baseHealth = 1000; data.healthPerWave = data.baseSpeed = data.maxSpeedBonus = 0;
                var enemy = game.SpawnEnemy(data.id, position);
                enemy.transform.position = position; enemy.enabled = enemy.Weapon.enabled = false;
                return enemy;
            }
            finally { data.baseHealth = health; data.healthPerWave = growth; data.baseSpeed = speed; data.maxSpeedBonus = bonus; }
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    game.StartBattle(); game.MachineGun.enabled = game.MainGun.enabled = game.Mortar.enabled = false;
                    Require(game.Artwork.Mortar != null && game.Artwork.Mortar.Muzzle != null, "Imported mortar and socket are required.");
                    Require(game.Mortar.Stats.fullDamageRadius == 2.5f && game.Mortar.Stats.halfDamageRadius == 5, "AoE radii must default to 2.5 and 5 units.");
                    foreach (float x in new[] { 5f, 20f, 80f })
                    {
                        Vector2 origin = new Vector2(0, 3), destination = new Vector2(x, BattleGame.GroundHeight);
                        Require(MortarWeapon.TryHighArc(origin, destination, 70, 12, out Vector2 velocity, out float time), "High arc must solve near/far shots.");
                        Require(Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x)) * Mathf.Rad2Deg > 45, "Mortar launch must exceed 45 degrees.");
                        Require(Vector2.Distance(origin + velocity * time + Vector2.down * (6 * time * time), destination) < .001f, "High arc must arrive at the scattered destination.");
                    }
                    bool left = false, right = false;
                    for (int i = 0; i < 1000; i++)
                    {
                        float deviation = MortarWeapon.SampleDeviation(new Vector2(1, 3));
                        Require(Mathf.Abs(deviation) >= 1 && Mathf.Abs(deviation) <= 3, "Scatter must remain 1-3 units.");
                        left |= deviation < 0; right |= deviation > 0;
                    }
                    Require(left && right, "Scatter must cover both sides of the intended point.");
                    Require(BattleGame.MortarDamageAtDistance(2.5f, 20, 2.5f, 5) == 20
                        && BattleGame.MortarDamageAtDistance(2.5001f, 20, 2.5f, 5) == 10
                        && BattleGame.MortarDamageAtDistance(5, 20, 2.5f, 5) == 10
                        && BattleGame.MortarDamageAtDistance(5.001f, 20, 2.5f, 5) == 0, "AoE boundaries must be inclusive with no stacked inner/outer damage.");
                    Clear(game);
                    Vector2 impact = new Vector2(25, 0);
                    var inside = Dummy(game, impact + Vector2.right * 2.5f);
                    var outer = Dummy(game, impact + Vector2.right * 5);
                    var outside = Dummy(game, impact + Vector2.right * 5.01f);
                    game.DetonateMortar(impact, 20, 2.5f, 5);
                    Require(inside.Health == 980 && outer.Health == 990 && outside.Health == 1000 && game.Hull == 100,
                        "Actual explosions must damage each enemy once, respect both zones, and ignore the tank.");
                    // Resolve killing several enemies in one explosion without mutating the active iteration.
                    game.DetonateMortar(impact, 10000, 2.5f, 5);
                    Require(!inside.Alive && !outer.Alive && outside.Alive, "AoE multi-kills must safely remove victims while preserving outsiders.");
                    Clear(game);
                    target = Dummy(game, (Vector2)game.Artwork.Mortar.Muzzle.position + new Vector2(18, 0));
                    target.transform.position = new Vector2(target.transform.position.x, BattleGame.GroundHeight + .9f);
                    var stats = game.Mortar.Stats;
                    stats.damage = 10; stats.reloadTime = .3f; stats.gravity = 40; stats.reserveCapacity = 2;
                    game.Mortar.ResetWeapon(); game.Mortar.enabled = true;
                    Next();
                }
                else if (stage == 1 && elapsed > .6)
                {
                    Require(game.Mortar.ShotsFired == 2 && game.Mortar.Ammo == 0, "Mortar must consume a shell per shot and stop when empty.");
                    Require(Mathf.Abs(game.Mortar.LastAimPoint.x - game.Mortar.LastIntendedTarget.x) >= .999f
                        && Mathf.Abs(game.Mortar.LastAimPoint.x - game.Mortar.LastIntendedTarget.x) <= 3.001f,
                        "Actual launches must apply the configured scatter.");
                    shell = UnityEngine.Object.FindObjectsOfType<Projectile2D>().First(projectile => projectile.IsMortar);
                    Require(shell.Team == ProjectileTeam.Player && shell.Velocity.y > 0, "Mortar shells must rise as player projectiles.");
                    pausedShell = shell.transform.position;
                    game.SetPaused(true); Next();
                }
                else if (stage == 2 && elapsed > .2)
                {
                    Require(shell.transform.position == pausedShell, "Mortar flight must freeze while paused.");
                    game.SetPaused(false); Next();
                }
                else if (stage == 3 && elapsed > .4)
                {
                    Require(game.Mortar.ShotsFired == 2, "Reload must not replenish mortar reserves.");
                    var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath);
                    game.ApplyCardEffect(catalog.cards.Single(card => card.id == "13"));
                    Require(game.Mortar.Ammo == 1, "Existing resupply cards must restore mortar ammunition.");
                    Next();
                }
                else if (stage == 4 && elapsed > 4)
                {
                    Require(game.Mortar.ShotsFired == 3 && game.Mortar.Ammo == 0, "A resupplied mortar must fire again.");
                    Require(!UnityEngine.Object.FindObjectsOfType<Projectile2D>().Any(projectile => projectile.IsMortar), "Impact must destroy each mortar shell.");
                    Require(target.Health >= 970 && target.Health <= 985 && game.Hull == 100,
                        "Three physical impacts must deal the full/half damage once per shell and ignore the tank.");
                    game.StartBattle(); game.MachineGun.enabled = game.MainGun.enabled = game.Mortar.enabled = false;
                    Require(game.Mortar.Ammo == game.StartingLoadout.mortar.stats.reserveCapacity && game.Mortar.ShotsFired == 0, "Restart must restore the starting mortar definition and firing state.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_MORTAR_OK: imported mount, steep ballistic solves, 1000 scatter checks, inclusive AoE boundaries/multikills, physical hits, ammo/depletion/resupply, pause and restart.");
                    Finish(0);
                }
                if (elapsed > 20) throw new Exception("Mortar audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}
