using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class ProjectileArtAudit
    {
        static Projectile2D cannon, mg, groundProbe;
        static SpriteRenderer cannonVisual;
        static Quaternion initialRotation;
        static double since;
        static int stage;
        static bool errors;

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

        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                if (stage == 0)
                {
                    game.StartBattle();
                    cannon = new GameObject("Cannon art audit").AddComponent<Projectile2D>();
                    cannon.Initialize(new Vector2(500, 500), new Vector2(1, .3f), 18, 1, Color.yellow, true, 1000, 3);
                    mg = new GameObject("MG art audit").AddComponent<Projectile2D>();
                    mg.Initialize(new Vector2(502, 500), Vector2.right, 18, 1, Color.cyan, false, 1000, .35f);
                    groundProbe = new GameObject("Cannon ground audit").AddComponent<Projectile2D>();
                    groundProbe.Initialize(new Vector2(504, BattleGame.GroundHeight + .5f), Vector2.down, 100, 1, Color.yellow, true, 1000, 3);
                    cannonVisual = cannon.GetComponentInChildren<SpriteRenderer>();
                    Require(cannonVisual.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(CannonProjectileImporter.SpritePath), "Cannon must use the supplied image.");
                    Require(Mathf.Abs(cannonVisual.sprite.bounds.size.x * cannonVisual.transform.lossyScale.x - game.CannonProjectileWidth) < .001f, "Projectile width must match its configured world size.");
                    Require(Mathf.Approximately(cannonVisual.transform.lossyScale.x, cannonVisual.transform.lossyScale.y)
                        && cannonVisual.color == Color.white, "Projectile must retain its original proportions and colors.");
                    Require(mg.GetComponentInChildren<SpriteRenderer>().sprite != cannonVisual.sprite, "MG must retain its existing visual.");
                    initialRotation = cannonVisual.transform.rotation;
                    since = EditorApplication.timeSinceStartup;
                    stage = 1;
                }
                else if (EditorApplication.timeSinceStartup - since > .25)
                {
                    Require(Vector2.Dot(cannonVisual.transform.right, cannon.Velocity.normalized) > .9999f
                        && Quaternion.Angle(initialRotation, cannonVisual.transform.rotation) > .1f, "Shell nose must rotate with curved flight.");
                    Require(groundProbe == null, "Art projectile must still be destroyed on ground contact.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_PROJECTILE_ART_OK: supplied sprite, configured size, original aspect/colors, curved-flight orientation, unchanged MG visual and ground contact.");
                    Finish(0);
                }
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
