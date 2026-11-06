using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class EnemyCombatAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static EnemyEntity flyer, ground;
        static Vector3 flyerStart, groundStart;
        static float hullBefore, flyerHealth, groundHealth;
        static Transform carWheel;
        static Quaternion wheelStart, stoppedWheel;
        static SpriteRenderer planePropeller;
        static Sprite initialPropeller;
        static bool propellerAnimated;

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
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static Vector2 PositionAtRange(BattleGame game, EnemyDefinition data, float extra)
        {
            float y = data.kind == EnemyKind.Flyer ? 4.8f : -2.25f;
            float height = y - game.TankAimPoint.y;
            float distance = data.weapon.range + extra;
            return new Vector2(game.TankAimPoint.x + Mathf.Sqrt(distance * distance - height * height), y);
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage > 0 && planePropeller != null && planePropeller.sprite != initialPropeller) propellerAnimated = true;
                if (stage == 0)
                {
                    game.StartBattle(); game.Mortar.enabled = false;
                    game.MachineGun.enabled = game.MainGun.enabled = false;
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                    {
                        game.RemoveEnemy(enemy, false); enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject);
                    }
                    var flyingData = game.EnemyDatabase.Get("FlyingEnemy1");
                    var groundData = game.EnemyDatabase.Get("GroundEnemy1");
                    Require(flyingData.weapon.type == EnemyWeaponType.MachineGun && groundData.weapon.type == EnemyWeaponType.Cannon, "Named enemy definitions need their intended weapon types.");
                    flyer = game.SpawnEnemy(flyingData.id, PositionAtRange(game, flyingData, 5));
                    ground = game.SpawnEnemy(groundData.id, PositionAtRange(game, groundData, 5));
                    Require(flyer.Artwork != null && ground.Artwork != null, "Both enemy types must use imported vehicle prefabs.");
                    foreach (var enemy in new[] { flyer, ground })
                    {
                        Require(enemy.AdvanceInsideRange >= 1 && enemy.AdvanceInsideRange <= 5, "Each enemy must sample 1-5 units of extra advance.");
                        float height = enemy.transform.position.y - game.TankAimPoint.y;
                        float edge = game.TankAimPoint.x + Mathf.Sqrt(enemy.Data.weapon.range * enemy.Data.weapon.range - height * height);
                        Require(Mathf.Abs(edge - enemy.StoppingX - enemy.AdvanceInsideRange) < .001f,
                            "The destination must be the sampled horizontal distance inside range.");
                    }
                    Require(flyer.Artwork.transform.localScale.x < 0 && ground.Artwork.transform.localScale.x < 0, "Vehicles must face left toward the tank.");
                    Require(Mathf.Approximately(ground.Data.scale, 1.25f) && Mathf.Approximately(flyer.Data.scale, 1.25f), "Both enemies must default to 1.25 scale.");
                    Require(Mathf.Abs(ground.Artwork.OwnerLocalBounds.size.x - ground.Data.ScaledArtworkWidth) < .01f
                        && Mathf.Abs(flyer.Artwork.OwnerLocalBounds.size.x - flyer.Data.ScaledArtworkWidth) < .01f, "Vehicle widths must include the configured scale without stretching.");
                    Require(Mathf.Abs(ground.transform.position.y + ground.Artwork.OwnerLocalBounds.min.y - BattleGame.GroundHeight) < .001f, "Car wheels must rest on the ground.");
                    Require(ground.GetComponent<BoxCollider2D>() != null && flyer.GetComponent<BoxCollider2D>() != null, "Vehicle hitboxes must fit the imported art.");
                    Require(ground.Artwork.Muzzle.position.x < ground.transform.position.x && flyer.Artwork.Muzzle.position.x < flyer.transform.position.x, "Illustrated muzzle sockets must be at the leading end of each vehicle.");
                    carWheel = Array.Find(ground.Artwork.GetComponentsInChildren<Transform>(), part => part.name == "02_wheel_rear");
                    planePropeller = Array.Find(flyer.Artwork.GetComponentsInChildren<SpriteRenderer>(), part => part.name == "01_propeller_blades");
                    wheelStart = carWheel.localRotation; initialPropeller = planePropeller.sprite; propellerAnimated = false;
                    flyerStart = flyer.transform.position; groundStart = ground.transform.position;
                    Next();
                }
                else if (stage == 1 && elapsed > .2)
                {
                    Require(flyer.transform.position.x < flyerStart.x && ground.transform.position.x < groundStart.x, "Both enemies must approach while out of range.");
                    Require(flyer.Weapon.ShotsFired == 0 && ground.Weapon.ShotsFired == 0 && game.Hull == 100, "Enemies must not shoot or deal contact damage out of range.");
                    Require(Quaternion.Angle(wheelStart, carWheel.localRotation) > .1f, "Car wheels must rotate while moving.");
                    foreach (var enemy in new[] { flyer, ground })
                    {
                        Vector3 position = enemy.transform.position;
                        float height = position.y - game.TankAimPoint.y;
                        float range = enemy.Data.weapon.range;
                        position.x = game.TankAimPoint.x + Mathf.Sqrt(range * range - height * height) - .05f;
                        enemy.transform.position = position;
                    }
                    Next();
                }
                else if (stage == 2 && elapsed > .7)
                {
                    Require(flyer.InWeaponRange && ground.InWeaponRange && flyer.Velocity != Vector2.zero && ground.Velocity != Vector2.zero, "Enemies must continue advancing after entering weapon range.");
                    Require(flyer.Weapon.ShotsFired >= 3 && ground.Weapon.ShotsFired >= 1, "MG salvos and cannon rounds must fire automatically.");
                    Require(propellerAnimated, "Plane must play its supplied propeller frames.");
                    Require(Quaternion.Angle(wheelStart, carWheel.localRotation) > .1f, "Car wheels must animate while firing on the move.");
                    // Advance to just before each destination to verify the final movement clamp without a long wait.
                    foreach (var enemy in new[] { flyer, ground })
                    {
                        Vector3 position = enemy.transform.position;
                        position.x = enemy.StoppingX + .05f;
                        enemy.transform.position = position;
                    }
                    Next();
                }
                else if (stage == 3 && elapsed > .7)
                {
                    Require(flyer.Velocity == Vector2.zero && ground.Velocity == Vector2.zero
                        && Mathf.Abs(flyer.transform.position.x - flyer.StoppingX) < .001f
                        && Mathf.Abs(ground.transform.position.x - ground.StoppingX) < .001f,
                        "Enemies must stop exactly at their personal destinations without overshooting.");
                    stoppedWheel = carWheel.localRotation;
                    flyerStart = flyer.transform.position; groundStart = ground.transform.position;
                    flyerHealth = flyer.Health; groundHealth = ground.Health;
                    Next();
                }
                else if (stage == 4 && elapsed > 3)
                {
                    Require(game.Hull < 100 && game.Hull > 0, "Actual enemy projectiles must damage the tank hull.");
                    Require(Vector3.Distance(flyerStart, flyer.transform.position) < .02f && Vector3.Distance(groundStart, ground.transform.position) < .02f, "Attacking enemies must hold position instead of ramming.");
                    Require(flyer.Health == flyerHealth && ground.Health == groundHealth, "Enemy shots must not damage friendly enemies.");
                    Require(Quaternion.Angle(stoppedWheel, carWheel.localRotation) < .1f, "Car wheels must stop when it holds firing position.");
                    flyer.Weapon.enabled = ground.Weapon.enabled = false;
                    foreach (var shot in UnityEngine.Object.FindObjectsOfType<Projectile2D>()) { shot.gameObject.SetActive(false); UnityEngine.Object.Destroy(shot.gameObject); }
                    hullBefore = game.Hull;
                    var friendly = new GameObject("Enemy friendly-fire probe").AddComponent<Projectile2D>();
                    friendly.Initialize((Vector2)ground.transform.position + Vector2.right * 2, Vector2.left, 100, 50, Color.red, false, 10, 0, ProjectileTeam.Enemy);
                    var player = new GameObject("Player hull-ignore probe").AddComponent<Projectile2D>();
                    player.Initialize(game.TankAimPoint, Vector2.right, 100, 90, Color.cyan, false, 1);
                    Next();
                }
                else if (stage == 5 && elapsed > .25)
                {
                    Require(game.Hull == hullBefore && ground.Health == groundHealth, "Projectile teams must ignore their own side's hitboxes.");
                    int expectedScore = flyer.Data.score + ground.Data.score;
                    flyer.Damage(10000); ground.Damage(10000);
                    Require(game.EnemyCount == 0 && game.Score == expectedScore, "Enemy deaths must retain manager-defined score rewards.");
                    Require(!errors, "Runtime errors were reported.");
                    Debug.Log("MEGATANKS_ENEMY_COMBAT_OK: random 1-5 unit stopping destinations, moving fire, exact stopping and holding, layered art/scale, grounding, hitboxes, animation, tank damage and rewards.");
                    Finish(0);
                }
                if (stage > 0 && elapsed > 20) throw new Exception("Enemy combat audit timed out.");
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
