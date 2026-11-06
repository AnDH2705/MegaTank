using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class LandscapeDamageAudit
    {
        static int stage;
        static double since;
        static bool capture, errors;
        static EnemyEntity enemy;
        static BattleGame.DamageNumber tankNumber, enemyNumber;
        static Vector2 startPosition;
        static float pausedAge;

        public static void Capture() { capture = true; Run(); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            if (capture)
            {
                var window = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
                window.position = new Rect(50, 20, 1000, 600);
                MegaTanksSetup.SetLandscapeGameView(window);
                window.Show(); window.Focus();
            }
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0; errors = false; since = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    Require(PlayerSettings.defaultScreenWidth == 1920 && PlayerSettings.defaultScreenHeight == 1080
                        && PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeLeft, "Landscape project settings are required.");
                    Require(BattleGame.ReferenceWidth == 1920 && BattleGame.ReferenceHeight == 1080, "HUD must use landscape dimensions.");
                    game.StartBattle(); game.Mortar.enabled = false;
                    game.MachineGun.enabled = game.MainGun.enabled = false;
                    var mantlet = game.Artwork.GetComponentsInChildren<Transform>().Single(part => part.name == "07_main_gun_mantlet");
                    var barrel = game.Artwork.GetComponentsInChildren<Transform>().Single(part => part.name == "08_main_gun_barrel");
                    var gun = game.MainGun.Artwork;
                    Require(!mantlet.IsChildOf(gun.transform) && barrel.IsChildOf(gun.transform)
                        && Vector3.Distance(barrel.localPosition, Vector3.right * TankArtImporter.CannonPivotInset) < .0001f,
                        "Cannon pivot must sit 0.45 units behind the barrel's original pivot while the mantlet remains fixed.");
                    Require(barrel.GetComponent<SpriteRenderer>().sortingOrder < mantlet.GetComponent<SpriteRenderer>().sortingOrder,
                        "The fixed mantlet must render over the rotating barrel.");
                    Vector3 mantletPosition = mantlet.position, barrelPosition = barrel.position, muzzlePosition = gun.Muzzle.position;
                    Quaternion mantletRotation = mantlet.rotation;
                    foreach (float angle in new[] { -35f, 35f })
                    {
                        gun.Aim(Quaternion.Euler(0, 0, angle) * Vector2.right);
                        Require(Vector3.Distance(mantlet.position, mantletPosition) < .0001f && Quaternion.Angle(mantlet.rotation, mantletRotation) < .001f,
                            "Aiming must leave the mantlet fixed.");
                        Require(Vector3.Distance(barrel.position, barrelPosition) > .01f, "Barrel must swing around the inset pivot.");
                    }
                    gun.ResetVisual();
                    Require(Vector3.Distance(barrel.position, barrelPosition) < .0001f && Vector3.Distance(gun.Muzzle.position, muzzlePosition) < .0001f,
                        "Reset must preserve the original barrel and muzzle rest positions.");
                    foreach (var entity in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) { entity.enabled = false; entity.Weapon.enabled = false; }
                    enemy = UnityEngine.Object.FindObjectsOfType<EnemyEntity>().First(entity=>entity.Alive);
                    game.DamageHull(7.5f); enemy.Damage(12.5f);
                    Require(game.DamageNumbers.Count == 2, "Both tank and enemy hits must create one number each.");
                    tankNumber = game.DamageNumbers.Single(number => number.IsTank);
                    enemyNumber = game.DamageNumbers.Single(number => !number.IsTank);
                    Require(tankNumber.Amount == 7.5f && enemyNumber.Amount == 12.5f, "Damage labels must retain fractional hit values.");
                    Require(tankNumber.DisplayText == "-7" && enemyNumber.DisplayText == "-12", "Visible damage numbers must round down without altering actual damage.");
                    startPosition = enemyNumber.Position;
                    game.DamageHull(0); enemy.Damage(0);
                    Require(game.DamageNumbers.Count == 2, "Zero damage must not create labels.");
                    if (capture) { System.IO.Directory.CreateDirectory("Checks"); ScreenCapture.CaptureScreenshot("Checks/LandscapeBattle.png"); }
                    Next();
                }
                else if (stage == 1 && elapsed > .2)
                {
                    Require(enemyNumber.Position.y > startPosition.y && enemyNumber.Age > 0, "Numbers must float upward over time.");
                    Require(Mathf.Approximately(game.TankCamera.rect.width,Screen.safeArea.width/Screen.width)
                        && game.TankCamera.rect.height > .5f, "The landscape battlefield must use one full-width view.");
                    game.SetPaused(true); pausedAge = enemyNumber.Age;
                    Next();
                }
                else if (stage == 2 && elapsed > .2)
                {
                    Require(enemyNumber.Age == pausedAge, "Damage numbers must pause with combat.");
                    game.SetPaused(false); Next();
                }
                else if (stage == 3 && elapsed > BattleGame.DamageNumberLifetime + .2)
                {
                    Require(game.DamageNumbers.Count == 0, "Expired labels must be removed.");
                    float remainingHealth = enemy.Health;
                    enemy.Damage(100000); enemy.Damage(100000);
                    Require(game.DamageNumbers.Count == 1 && game.DamageNumbers[0].Amount == remainingHealth,
                        "Lethal damage must show actual health lost, once, and survive entity destruction.");
                    game.StartBattle(); game.Mortar.enabled = false;
                    Require(game.DamageNumbers.Count == 0, "Restart must clear damage labels.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_LANDSCAPE_DAMAGE_OK: inset cannon pivot, fixed mantlet/layering, barrel/muzzle reset, rounded-down labels, actual fractional damage, movement/pause/expiry, landscape and restart.");
                    Finish(0);
                }
                if (elapsed > 20) throw new Exception("Landscape damage audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code)
        {
            EditorApplication.update -= Check;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.Exit(code);
        }
    }
}
