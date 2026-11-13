using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class TrackAnimationAudit
    {
        static int stage;
        static double since;
        static bool capture, errors;
        static TankTrackAnimator tracks;
        
        static Vector2 rest, held;
        static float startX, began, heldPhase;
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        public static void Capture() { capture = true; Run(); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            if (capture)
            {
                var window = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
                window.position = new Rect(50, 20, 1000, 600); MegaTanksSetup.SetLandscapeGameView(window); window.Show(); window.Focus();
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
        static void Screenshot(string name)
        {
            if (!capture) return;
            System.IO.Directory.CreateDirectory("Checks"); ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Checks/" + name + ".png"));
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance; double elapsed = EditorApplication.timeSinceStartup - since;
                if (stage == 0)
                {
                    game.StartBattle(); game.SetStance(TankStance.Defensive); StageProgressionAudit.DisableWeapons(game);
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) enemy.enabled = enemy.Weapon.enabled = false;
                    tracks = game.Artwork.Tracks;
                    Require(tracks != null && tracks.LinkCount == 36 && tracks.Data.path.Length == 577, "Track animation must use all 36 source links and the sampled source path.");
                    Require(tracks.Data.sourceAnimation == "running" && tracks.Data.sourceVersion == "3.8.99"
                        && Mathf.Abs(tracks.Data.sourceDuration - 6.6666665f) < .001f, "Imported animation metadata must match the supplied running loop.");
                    Require(Vector2.Distance(tracks.Data.path[0], tracks.Data.path.Last()) < .00001f
                        && tracks.Data.Length > tracks.Data.linkOffsets.Last(), "The loop must close and contain all source-spaced links.");
                    Require(Mathf.Abs(game.Artwork.transform.position.x + 7.3f) < .001f && game.TankCamera.transform.position.x==game.CameraPan.StartX,
                        "Tank spawn must remain at -7.3 while the camera uses its configured initial framing.");
                    Require(tracks.Animation.Skeleton.Data.Version.StartsWith("4.2") && tracks.Animation.Skeleton.Bones.Count == 38, "Tracks must run through Spine 4.2 with all original bones.");
                    rest = tracks.FirstLinkPosition; startX = game.Artwork.transform.position.x; Next();
                }
                else if (stage == 1 && elapsed > .2)
                {
                    Require(tracks.TravelledDistance == 0 && tracks.FirstLinkPosition == rest, "Defensive tracks must hold their idle pose.");
                    Screenshot("TrackIdle"); Next();
                }
                else if (stage == 2 && elapsed > .2) { game.SetStance(TankStance.Aggressive); began = Time.time; Next(); }
                else if (stage == 3 && elapsed > .6)
                {
                    float moved = game.Artwork.transform.position.x - startX;
                    Require(Mathf.Abs(moved*game.EnemySpawnDirection - (Time.time - began) * .5f) < .02f && Vector2.Distance(tracks.FirstLinkPosition, rest) > .02f,
                        "The tank and imported links must advance together.");
                    Require(Mathf.Abs(tracks.TravelledDistance - Mathf.Repeat(moved / tracks.transform.lossyScale.x,tracks.Data.Length)) < .005f,
                        "Track speed must match actual travel and tank scale.");
                    Screenshot("TrackRunning"); Next();
                }
                else if (stage == 4 && elapsed > .2)
                {
                    game.SetStance(TankStance.Defensive); held = tracks.FirstLinkPosition; heldPhase = tracks.TravelledDistance; Next();
                }
                else if (stage == 5 && elapsed > .2)
                {
                    Require(tracks.FirstLinkPosition == held && tracks.TravelledDistance == heldPhase, "Switching to Defensive must stop the links without snapping.");
                    game.SetStance(TankStance.Aggressive); game.SetPaused(true); Next();
                }
                else if (stage == 6 && elapsed > .2)
                {
                    Require(tracks.FirstLinkPosition == held && tracks.TravelledDistance == heldPhase, "Pausing must freeze the track loop.");
                    game.SetPaused(false); Next();
                }
                else if (stage == 7 && elapsed > .2)
                {
                    Require(Mathf.DeltaAngle(heldPhase/tracks.Data.Length*360,tracks.TravelledDistance/tracks.Data.Length*360)*game.EnemySpawnDirection > .001f, "Resuming advance must resume the track loop in the travel direction.");
                    game.StartBattle(); game.SetStance(TankStance.Defensive); StageProgressionAudit.DisableWeapons(game);
                    Require(tracks.TravelledDistance == 0 && tracks.FirstLinkPosition == rest && Mathf.Abs(game.Artwork.transform.position.x + 7.3f) < .001f,
                        "Restart must reset the track pose and shifted starting position.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_TRACK_ANIMATION_OK: preserved tank spawn, single-camera spawn framing, Spine 4.2 SkeletonAnimation, 36 constrained bones, closed path, terrain-matched motion, idle/Defensive hold, pause/resume and restart.");
                    Finish(0);
                }
                if (elapsed > 30) throw new Exception("Track audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}


