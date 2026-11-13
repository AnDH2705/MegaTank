using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BattleBackgroundAudit
    {
        static int stage, selections, retained;
        static double since;
        static bool capture, errors;
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
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static SpriteRenderer[] Tiles(BattleGame game, string layer)
        {
            int index = LayerMask.NameToLayer(layer);
            return game.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.gameObject.activeInHierarchy && renderer.gameObject.layer == index).ToArray();
        }
        static void CheckGeometry(BattleGame game)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleBackgroundCatalog>(BattleBackgroundImporter.CatalogPath);
            Require(catalog.presets.Length == 2 && catalog.presets[0].layers.Length == 6 && catalog.presets[1].layers.Length == 7, "Both layered background sets must be imported.");
            foreach (var preset in catalog.presets)
                foreach (var layer in preset.layers)
                    Require(layer.sprite != null && layer.sprite.rect.width == 1920 && layer.sprite.rect.height == 1080
                        && Mathf.Abs(layer.sprite.bounds.size.y - 10.8f) < .001f, "Every sprite must retain its full original canvas.");
            int tankLayer = LayerMask.NameToLayer(BattleBackgroundController.TankLayerName), enemyLayer = LayerMask.NameToLayer(BattleBackgroundController.EnemyLayerName);
            Require((game.TankCamera.cullingMask & (1 << tankLayer)) != 0 && (game.TankCamera.cullingMask & (1 << enemyLayer)) == 0,
                "The main camera must see its single backdrop set.");
            game.Backgrounds.ApplyPreset(0);
            game.TankCamera.transform.position = new Vector3(0, game.TankCamera.transform.position.y, -10); game.Backgrounds.RefreshViews();
            var sprites = Tiles(game, BattleBackgroundController.TankLayerName);
            var sky = sprites.Single(renderer => renderer.sprite.name == "00_sky_static");
            var road = sprites.Where(renderer => renderer.sprite.name == "03_road").OrderBy(renderer => Mathf.Abs(renderer.transform.position.x)).First();
            var far = sprites.Where(renderer => renderer.sprite.name == "01_far_factories").OrderBy(renderer => Mathf.Abs(renderer.transform.position.x)).First();
            Require(Mathf.Approximately(game.Backgrounds.SceneryScale,.5f)&&Mathf.Abs(road.bounds.size.y-game.TankCamera.orthographicSize)<.001f,"Scenery must render at half the previous height and width.");
            float roadLine=road.transform.position.y+(BattleGame.SceneryGroundHeight-game.TankCamera.transform.position.y)*game.Backgrounds.SceneryScale;
            Require(Mathf.Abs(roadLine-BattleGame.SceneryGroundHeight)<.001f,"Shrinking scenery must preserve the ground alignment.");
            var extension=sprites.First(r=>r.sprite.name=="Road lower edge");
            Require(extension.bounds.min.y<=game.TankCamera.transform.position.y-game.TankCamera.orthographicSize&&extension.bounds.max.y>=road.bounds.min.y,"The road must continue to the bottom of the screen without a gap.");
            game.TankCamera.transform.position += Vector3.right; game.Backgrounds.RefreshViews();
            Require(Mathf.Abs(sky.transform.position.x - 1) < .001f && Mathf.Abs(road.transform.position.x) < .001f
                && Mathf.Abs(far.transform.position.x - .85f) < .001f,
                "Sky must stay screen-fixed; road/far layers must scroll at 1x/.15x while panning.");
            var roadTiles = sprites.Where(renderer => renderer.sprite.name == "03_road").OrderBy(renderer => renderer.transform.position.x).ToArray();
            for (int i = 1; i < roadTiles.Length; i++)
                Require(roadTiles[i].flipX != roadTiles[i - 1].flipX && Mathf.Abs(roadTiles[i - 1].bounds.max.x - roadTiles[i].bounds.min.x) < .001f,
                    "Repeating tiles must mirror alternately and meet without gaps.");
            Require(sprites.All(renderer => renderer.sortingOrder < 0), "Every backdrop layer must stay behind gameplay entities.");
            game.TankCamera.aspect = 2.7f; game.Backgrounds.RefreshViews();
            Require(sky.bounds.size.x >= game.TankCamera.orthographicSize * 2 * game.TankCamera.aspect - .001f, "Static sky must also fill extra-wide viewports.");
            game.TankCamera.ResetAspect();
            CheckViewportAnchors(game);
        }
        static void CheckViewportAnchors(BattleGame game)
        {
            var camera=game.TankCamera;var originalPosition=camera.transform.position;float originalSize=camera.orthographicSize;
            game.Backgrounds.ApplyPreset(1);
            for(int pass=0;pass<2;pass++)
            {
                if(pass==1){camera.transform.position+=new Vector3(13,2,0);camera.orthographicSize*=1.3f;}
                game.Backgrounds.RefreshViews();
                var tiles=Tiles(game,BattleBackgroundController.TankLayerName);
                foreach(var tile in tiles.Where(r=>r.sprite.name=="05_foreground_debris"))
                    Require(Mathf.Abs(tile.bounds.min.y-(camera.transform.position.y-camera.orthographicSize))<.001f,"Debris must remain on the camera viewport bottom while panning or resizing.");
                foreach(var tile in tiles.Where(r=>r.sprite.name=="06_overhead_cables"))
                    Require(Mathf.Abs(tile.bounds.max.y-(camera.transform.position.y+camera.orthographicSize))<.001f,"Cables must remain on the camera viewport top while panning or resizing.");
                Require(tiles.Any(r=>r.sprite.name=="05_foreground_debris")&&tiles.Any(r=>r.sprite.name=="06_overhead_cables"),"Both anchored art layers must be present.");
            }
            camera.transform.position=originalPosition;camera.orthographicSize=originalSize;
            game.Backgrounds.ApplyPreset(0);
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
                    game.StartBattle(); game.SetStance(TankStance.Defensive); StageProgressionAudit.DisableWeapons(game); CheckGeometry(game);
                    var savedRandom = UnityEngine.Random.state; UnityEngine.Random.InitState(421);
                    bool[] seen = new bool[2];
                    for (int i = 0; i < 16; i++) { game.Backgrounds.SelectRandom(); seen[game.Backgrounds.CurrentIndex] = true; }
                    UnityEngine.Random.state = savedRandom;
                    Require(seen.All(value => value), "Repeated random selection must make both presets available.");
                    game.Backgrounds.ApplyPreset(0); Next();
                }
                else if (stage == 1 && elapsed > .2) { Screenshot("BattleIndustrial"); Next(); }
                else if (stage == 2 && elapsed > .3) { game.Backgrounds.ApplyPreset(1); Next(); }
                else if (stage == 3 && elapsed > .2) { Screenshot("BattleRuinedCity"); Next(); }
                else if (stage == 4 && elapsed > .3)
                {
                    selections = game.Backgrounds.SelectionCount;
                    StageProgressionAudit.ClearWave(game); game.Rewards.ChooseReward(0);
                    Require(game.Backgrounds.SelectionCount == selections, "Reward/map screens must not reroll the background.");
                    StageProgressionAudit.EnterType(game, StageNodeType.Supply);
                    Require(game.Backgrounds.SelectionCount == selections, "Supply/repair services must not reroll the background.");
                    game.Stage.LeaveService(); StageProgressionAudit.EnterType(game, StageNodeType.HardBattle);
                    Require(game.Backgrounds.SelectionCount == ++selections, "A new hard battle must roll a background once.");
                    retained = game.Backgrounds.CurrentIndex; game.Stage.Definition.waveBreakSeconds = .1f;
                    StageProgressionAudit.ClearWave(game); Next();
                }
                else if (stage == 5 && elapsed > .2)
                {
                    Require(game.Stage.EncounterWave == 2 && game.Backgrounds.SelectionCount == selections && game.Backgrounds.CurrentIndex == retained,
                        "Hard wave two must retain the encounter background.");
                    StageProgressionAudit.ClearWave(game); Next();
                }
                else if (stage == 6 && elapsed > .2)
                {
                    Require(game.Stage.EncounterWave == 3 && game.Backgrounds.SelectionCount == selections && game.Backgrounds.CurrentIndex == retained,
                        "Hard wave three must retain the encounter background.");
                    StageProgressionAudit.ClearWave(game); game.Rewards.ChooseReward(0);
                    while (game.Stage.Mode != StageMode.Victory)
                    {
                        StageProgressionAudit.EnterNextBattle(game);
                        Require(game.Backgrounds.SelectionCount == ++selections, "Every subsequent battle/boss encounter must roll once.");
                        StageProgressionAudit.ClearWave(game);
                        if (game.Stage.Mode != StageMode.Victory) game.Rewards.ChooseReward(0);
                    }
                    game.StartBattle(); game.SetStance(TankStance.Defensive); StageProgressionAudit.DisableWeapons(game);
                    Require(game.Backgrounds.SelectionCount == selections + 1, "Restart must select a background for the new first battle.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("MEGATANKS_BACKGROUNDS_OK: 6/7-layer art, original canvas, independent cameras, parallax factors, gap-free mirror repeats, wide-screen sky, random battle/boss/restart selection and hard-wave retention.");
                    Finish(0);
                }
                if (elapsed > 30) throw new Exception("Background audit timed out.");
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorApplication.update -= Check; EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}

