using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class EnemyCameraAudit
    {
        static int stage;
        static double since;
        static bool errors;
        static EnemyEntity ground, flyer, overtaker;
        static Vector3 heldCamera;

        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0;
                errors = false;
                since = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static EnemyEntity ExpectedNearest(BattleGame game)
        {
            EnemyEntity nearest = null;
            float minimum = float.PositiveInfinity;
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
            {
                if (!enemy.Alive) continue;
                float distance = Vector2.Distance(game.Artwork.transform.position, enemy.transform.position);
                if (distance < minimum) { nearest = enemy; minimum = distance; }
            }
            return nearest;
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
                    game.StartBattle();
                    Require(game.TrackedEnemy == ExpectedNearest(game), "Deployment must select the nearest living enemy.");
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                    {
                        game.RemoveEnemy(enemy, false);
                        enemy.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(enemy.gameObject);
                    }
                    Vector2 tank = game.Artwork.transform.position;
                    ground = game.SpawnEnemy(EnemyKind.Ground, tank + new Vector2(60, 0));
                    flyer = game.SpawnEnemy(EnemyKind.Flyer, tank + new Vector2(58, 5));
                    overtaker = game.SpawnEnemy(EnemyKind.Ground, tank + new Vector2(70, 0));
                    Next();
                }
                else if (stage == 1 && elapsed > .1)
                {
                    Require(game.TrackedEnemy == flyer && game.TrackedEnemy == ExpectedNearest(game), "A closer flyer must be selected over ground enemies.");
                    overtaker.transform.position = game.Artwork.transform.position + Vector3.right * 40;
                    Next();
                }
                else if (stage == 2 && elapsed > .1)
                {
                    Require(game.TrackedEnemy == overtaker && game.TrackedEnemy == ExpectedNearest(game), "A living enemy becoming closer must immediately take focus without waiting for a kill.");
                    // A smaller horizontal gap is not necessarily a smaller actual distance.
                    flyer.transform.position = game.Artwork.transform.position + new Vector3(39, 15, 0);
                    Next();
                }
                else if (stage == 3 && elapsed > .1)
                {
                    Require(game.TrackedEnemy == overtaker && game.TrackedEnemy == ExpectedNearest(game), "Nearest must use distance to the tank, including height, rather than enemy type or camera position.");
                    overtaker.Damage(10000);
                    Next();
                }
                else if (stage == 4 && elapsed > .1)
                {
                    Require(game.TrackedEnemy == flyer && game.TrackedEnemy == ExpectedNearest(game), "Destroying the tracked enemy must select the nearest survivor.");
                    flyer.Damage(10000);
                    ground.Damage(10000);
                    heldCamera = game.EnemyCamera.transform.position;
                    Next();
                }
                else if (stage == 5 && elapsed > .1)
                {
                    Require(game.TrackedEnemy == null && game.EnemyCount == 0, "An empty battlefield must not retain a destroyed target.");
                    Require(game.EnemyCamera.transform.position == heldCamera, "Camera must hold its horizontal position between waves.");
                    Next();
                }
                else if (stage == 6 && elapsed > 3.2)
                {
                    Require(game.Wave == 2 && game.TrackedEnemy != null && game.TrackedEnemy == ExpectedNearest(game), "A new wave must select its nearest living enemy.");
                    game.StartBattle();
                    // Unity destroys the previous wave at the end of the frame.
                    Next();
                }
                else if (stage == 7 && elapsed > .1)
                {
                    Require(game.Wave == 1 && game.TrackedEnemy == ExpectedNearest(game), "Restart must replace the old wave's focus.");
                    Require(!errors, "Runtime errors were reported during the audit.");
                    Debug.Log("MEGATANKS_NEAREST_CAMERA_OK: deployment, ground/flyer selection, live overtaking, actual distance to tank, target death, empty battlefield, new wave and restart.");
                    Finish(0);
                }
                Require(Mathf.Approximately(game.EnemyCamera.transform.position.y, game.TankCamera.transform.position.y), "Camera height must remain aligned throughout retargeting.");
                if (elapsed > 15) throw new Exception("Nearest-enemy audit timed out.");
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
