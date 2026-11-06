using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class TankStanceAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static EnemyEntity target;
        static float initialX, heldX, began, hullOffset, mortarRange;
        static Transform wheel;
        static Quaternion wheelStart;
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
        static void DisableWeapons(BattleGame game) { game.MachineGun.enabled = game.MainGun.enabled = game.Mortar.enabled = false; }
        static void Clear(BattleGame game)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
            { game.RemoveEnemy(enemy, false); enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
        }
        static EnemyEntity Dummy(BattleGame game)
        {
            var data = game.EnemyDatabase.Get(EnemyKind.Ground);
            float speed = data.baseSpeed, bonus = data.maxSpeedBonus;
            try
            {
                data.baseSpeed = data.maxSpeedBonus = 0;
                var enemy = game.SpawnEnemy(data.id, (Vector2)game.Artwork.transform.position + Vector2.right * 60);
                enemy.enabled = enemy.Weapon.enabled = false;
                return enemy;
            }
            finally { data.baseSpeed = speed; data.maxSpeedBonus = bonus; }
        }
        static void PlaceJustOutside(Vector2 origin, float range)
        {
            Vector3 position = target.transform.position;
            float dy = position.y - origin.y;
            position.x = origin.x + Mathf.Sqrt(range * range - dy * dy) + .15f;
            target.transform.position = position;
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    game.StartBattle(); DisableWeapons(game); Clear(game); target = Dummy(game);
                    Require(game.Stance == TankStance.Defensive && game.TankAdvanceSpeed == .5f, "Start in Defensive with 0.5 unit/second advance speed.");
                    initialX = heldX = game.Artwork.transform.position.x;
                    hullOffset = game.TankAimPoint.x - initialX;
                    wheel = game.Artwork.GetComponentsInChildren<Transform>().Single(part => part.name == "03_wheel_01");
                    wheelStart = wheel.localRotation;
                    Next();
                }
                else if (stage == 1 && elapsed > .3)
                {
                    Require(game.Artwork.transform.position.x == heldX && game.TankVelocity == Vector2.zero, "Defensive must hold position.");
                    game.SetStance(TankStance.Aggressive); began = Time.time; Next();
                }
                else if (stage == 2 && elapsed > .6)
                {
                    float moved = game.Artwork.transform.position.x - heldX;
                    Require(Mathf.Abs(moved - (Time.time - began) * .5f) < .02f, "Aggressive must advance at exactly 0.5 units/second.");
                    Require(Mathf.Approximately(game.TankVelocity.x, .5f) && Quaternion.Angle(wheelStart, wheel.localRotation) > .1f, "Movement velocity and wheel animation must match travel.");
                    Require(Mathf.Abs(game.TankAimPoint.x - game.Artwork.transform.position.x - hullOffset) < .001f,
                        "The hull hitbox must move with the tank.");
                    Require(game.TankCamera.transform.position.x==game.CameraPan.StartX && game.TankCamera.transform.position.y==1.4f,
                        "Tank movement must not override the manually panned camera or alter its horizon.");
                    game.SetStance(TankStance.Defensive); heldX = game.Artwork.transform.position.x; wheelStart = wheel.localRotation; Next();
                }
                else if (stage == 3 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x == heldX && Quaternion.Angle(wheelStart, wheel.localRotation) < .001f, "Defensive must immediately stop movement and wheels.");
                    game.SetStance(TankStance.Aggressive); game.SetPaused(true); Next();
                }
                else if (stage == 4 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x == heldX && game.TankVelocity == Vector2.zero, "Pause must halt Aggressive movement.");
                    game.SetPaused(false);
                    PlaceJustOutside(game.MachineGun.transform.position, game.MachineGun.MaxRange);
                    Next();
                }
                else if (stage == 5 && elapsed > .7)
                {
                    Require(game.AllWeaponsInRange && game.TankVelocity == Vector2.zero, "Aggressive must stop when the limiting MG reaches range.");
                    heldX = game.Artwork.transform.position.x; Next();
                }
                else if (stage == 6 && elapsed > .2)
                {
                    Require(Mathf.Abs(game.Artwork.transform.position.x - heldX) < .01f, "Aggressive must hold while all weapons remain in range.");
                    target.transform.position += Vector3.right * 2; Next();
                }
                else if (stage == 7 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x > heldX && game.Stance == TankStance.Aggressive, "Aggressive must resume when an enemy leaves range.");
                    mortarRange = game.Mortar.Stats.range; game.Mortar.Stats.range = 8;
                    PlaceJustOutside(game.Artwork.Mortar.Muzzle.position, 8);
                    Require(!game.AllWeaponsInRange, "The mortar's range must also gate movement.");
                    Next();
                }
                else if (stage == 8 && elapsed > .7)
                {
                    Require(game.AllWeaponsInRange && game.TankVelocity == Vector2.zero, "Aggressive must stop at the mortar's range when it is limiting.");
                    game.Mortar.Stats.range = mortarRange;
                    var gun = game.MainGun;
                    gun.Configure(gun.DisplayName, EnemyKind.Ground, gun.ReloadTime, gun.DamagePerShot, gun.SalvoSize, gun.ShotDelay, 8, gun.ProjectileGravity);
                    PlaceJustOutside(gun.transform.position, 8);
                    Require(!game.AllWeaponsInRange, "The cannon's range must also gate movement."); Next();
                }
                else if (stage == 9 && elapsed > .7)
                {
                    Require(game.AllWeaponsInRange && game.TankVelocity == Vector2.zero, "Aggressive must stop at the cannon's range when it is limiting.");
                    target.Damage(10000);
                    Require(game.Rewards.IsOpen, "Clearing the wave must still open card rewards.");
                    heldX = game.Artwork.transform.position.x; Next();
                }
                else if (stage == 10 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x == heldX && game.TankVelocity == Vector2.zero, "Reward screens must halt movement.");
                    game.Rewards.ChooseReward(0); DisableWeapons(game);
                    StageProgressionAudit.EnterNextBattle(game);
                    float spawnDistance = new SerializedObject(game).FindProperty("enemySpawnDistance").floatValue;
                    float nearestX = UnityEngine.Object.FindObjectsOfType<EnemyEntity>().Where(enemy => enemy.Alive).Min(enemy => enemy.transform.position.x);
                    Require(game.Wave == 2 && Mathf.Abs(nearestX - heldX - spawnDistance) < .001f, "The next wave must spawn relative to the advanced tank.");
                    game.StartBattle(); DisableWeapons(game);
                    Require(game.Stance == TankStance.Defensive && game.Artwork.transform.position.x == initialX, "Restart must restore starting position and Defensive stance.");
                    Clear(game); game.SetStance(TankStance.Aggressive); heldX = initialX; Next();
                }
                else if (stage == 11 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x == heldX, "An empty battlefield must halt advance.");
                    game.DamageHull(10000); Next();
                }
                else if (stage == 12 && elapsed > .2)
                {
                    Require(game.Artwork.transform.position.x == heldX && game.TankVelocity == Vector2.zero, "Defeat must halt advance.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_STANCES_OK: 0.5 unit/s, Defensive hold, all three range limits, automatic resume, pause/rewards/empty/defeat, moving hull/cameras/wheels, relative wave spawn and restart.");
                    Finish(0);
                }
                if (elapsed > 20) throw new Exception("Tank stance audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}
