using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class MegaTanksSetup
    {
        public const string ScenePath = "Assets/MegaTanks/Scenes/MainBattle.unity";

        [MenuItem("MegaTanks/Create Main Battle Scene")]
        public static void CreateProject()
        {
            System.IO.Directory.CreateDirectory("Assets/MegaTanks/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("MegaTanks 2D Battle").AddComponent<BattleGame>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("tankArtworkPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TankArtwork>(TankArtImporter.PrefabPath);
            serializedGame.FindProperty("cannonProjectileSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CannonProjectileImporter.SpritePath);
            serializedGame.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "SeagullGame";
            PlayerSettings.productName = "MegaTanks";
            PlayerSettings.bundleVersion = "0.1.0";
            ApplyPortraitSettings();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.seagullgame.megatanks");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.seagullgame.megatanks");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_SETUP_OK: 2D scene and mobile settings saved.");
        }

        [MenuItem("MegaTanks/Apply Portrait Settings")]
        public static void ApplyPortraitSettings()
        {
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_PORTRAIT_OK: 1080x1920 reference resolution, portrait orientation.");
        }

        static void SetPortraitGameView(EditorWindow gameView)
        {
            // Render at the phone resolution while keeping the window small enough for a desktop.
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width").GetValue(size) == 1080 && (int)size.GetType().GetProperty("height").GetValue(size) == 1920)
                { index = i; break; }
            }
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), 1080, 1920, "MegaTanks Portrait" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = count;
            }
            gameView.GetType().GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(gameView, index);
        }

        public static void SmokeTest()
        {
            EditorSceneManager.OpenScene(ScenePath);
            // Enter Play Mode without a domain reload so the validation callbacks stay attached.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.isPlaying = true;
        }

        static int stage;
        static double since;
        static bool errors;
        static int pausedShots;
        static Vector3 cameraStart;
        static float cameraStartTime;
        static EnemyEntity removedFocus;
        static int scoreBeforeHits;
        static Projectile2D rangeProbe;
        static Projectile2D mgGravityProbe, cannonGravityProbe;
        static Projectile2D groundMgProbe, groundCannonProbe, groundFallingProbe;
        static readonly List<float> mgShotTimes = new List<float>();
        static readonly List<float> mgShotFrameDeltas = new List<float>();
        static readonly List<float> mainShotTimes = new List<float>();
        static readonly List<float> mainShotFrameDeltas = new List<float>();

        static void CheckShotTiming(List<float> times, List<float> frameDeltas, int size, float delay, float reload)
        {
            for (int i = 1; i < times.Count; i++)
            {
                float expected = i % size == 0 ? reload : delay;
                float actual = times[i] - times[i - 1];
                Require(actual >= expected - .002f && actual <= expected + frameDeltas[i] + .01f,
                    "Shot " + i + " spacing was " + actual + "s; expected " + expected + "s plus at most one frame.");
            }
        }

        static bool ballisticPreview;
        static string PreviewFile => ballisticPreview ? "Preview/Ballistics.png" : "Preview/MainBattle.png";

        public static void CaptureBallisticsPreview()
        {
            ballisticPreview = true;
            CapturePreview();
        }

        // Graphics-enabled batch entry point for checking the rendered game and HUD together.
        public static void CapturePreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            var gameView = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            gameView.position = new Rect(50, 20, 540, 980);
            SetPortraitGameView(gameView);
            gameView.Show();
            gameView.Focus();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                stage = 0;
                since = EditorApplication.timeSinceStartup;
                EditorApplication.update += PreviewUpdate;
            };
            EditorApplication.isPlaying = true;
        }

        static void PreviewUpdate()
        {
            if (BattleGame.Instance == null) return;
            if (stage == 0)
            {
                BattleGame.Instance.StartBattle();
                if (ballisticPreview)
                {
                    // Bring one test target inside cannon range to inspect live shell/tracer rendering.
                    var game = BattleGame.Instance;
                    var target = Array.Find(UnityEngine.Object.FindObjectsOfType<EnemyEntity>(), enemy => enemy.Kind == EnemyKind.Ground);
                    target.transform.position = new Vector3(game.MainGun.transform.position.x + 70, -2.25f, 0);
                    game.EnemyCamera.transform.position = new Vector3(target.transform.position.x, game.TankCamera.transform.position.y, -10);
                }
                stage = 1;
                since = EditorApplication.timeSinceStartup;
            }
            else if (stage == 1 && EditorApplication.timeSinceStartup - since > (ballisticPreview ? 3.75 : .4))
            {
                System.IO.Directory.CreateDirectory("Preview");
                ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath(PreviewFile));
                stage = 2;
                since = EditorApplication.timeSinceStartup;
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup - since > 2)
            {
                EditorApplication.update -= PreviewUpdate;
                Debug.Log("MEGATANKS_PREVIEW: " + System.IO.File.Exists(PreviewFile));
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorApplication.Exit(System.IO.File.Exists(PreviewFile) ? 0 : 1);
            }
        }
        static void OnPlayState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            stage = 0;
            errors = false;
            since = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Check;
        }
        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) errors = true;
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
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
                    Require(game != null && game.MachineGun != null && game.MainGun != null, "Two independent turrets must exist.");
                    Require(game.Artwork != null && game.Artwork.GetComponentsInChildren<SpriteRenderer>().Length >= 39, "Layered tank prefab must be assembled.");
                    float configuredTankScale = new SerializedObject(game).FindProperty("tankScale").floatValue;
                    Require(game.Artwork.transform.localScale == Vector3.one * configuredTankScale, "Tank must use the scene's configured uniform scale.");
                    Require(PlayerSettings.defaultScreenWidth == 1080 && PlayerSettings.defaultScreenHeight == 1920
                        && PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Project must use the 1080x1920 portrait reference.");
                    Require(game.MachineGun.Artwork.Muzzle != null && game.MainGun.Artwork.Muzzle != null, "Both guns require artwork muzzle sockets.");
                    var mantlet = Array.Find(game.Artwork.GetComponentsInChildren<Transform>(), t => t.name == "07_main_gun_mantlet");
                    var cannonBarrel = Array.Find(game.Artwork.GetComponentsInChildren<Transform>(), t => t.name == "08_main_gun_barrel");
                    Require(mantlet != null && !mantlet.IsChildOf(game.MainGun.transform), "Mantlet must stay outside the rotating cannon assembly.");
                    Require(cannonBarrel != null && cannonBarrel.IsChildOf(game.MainGun.transform)
                        && cannonBarrel.localPosition.sqrMagnitude < .000001f, "Cannon must rotate around the barrel base.");
                    Vector3 mantletPosition = mantlet.position;
                    Quaternion mantletRotation = mantlet.rotation;
                    game.MainGun.Artwork.Aim(new Vector2(1, -.6f));
                    Require(Vector3.Distance(mantletPosition, mantlet.position) < .0001f
                        && Quaternion.Angle(mantletRotation, mantlet.rotation) < .001f, "Aiming must not move or rotate the mantlet.");
                    Require(Quaternion.Angle(cannonBarrel.rotation, mantletRotation) > 10, "The barrel must still rotate independently.");
                    game.MainGun.Artwork.ResetVisual();
                    Require(game.Artwork.GetComponentsInChildren<SpriteClipPlayer>().Length == 5, "Two muzzle clips and three pooled exhaust clips must exist.");
                    foreach (var clip in game.Artwork.GetComponentsInChildren<SpriteClipPlayer>()) Require(clip.FrameCount == 8, "Every effect needs eight imported frames.");
                    Require(!game.Running, "Battle must wait for Start Battle.");
                    Require(UnityEngine.Object.FindObjectsOfType<Camera>()[0].orthographic, "Battle camera must be 2D orthographic.");
                    game.StartBattle();
                    Require(game.Wave == 1 && game.EnemyCount == 6, "First wave must contain four ground enemies and two flyers.");
                    Require(Mathf.Approximately(game.EnemyCamera.transform.position.y, game.TankCamera.transform.position.y), "Both cameras must start at the same height.");
                    Require(game.MachineGun.MaxRange == 50 && game.MainGun.MaxRange == 80, "Weapons require independent 50/80 unit ranges.");
                    Require(Mathf.Approximately(game.MachineGun.ProjectileGravity, .35f)
                        && Mathf.Approximately(game.MainGun.ProjectileGravity, 3), "MG and cannon must have independent shallow/strong gravity defaults.");
                    Require(Ballistics2D.TryIntercept(Vector2.zero, new Vector2(75, 0), Vector2.left * .5f, 18, 3, out Vector2 ballisticVelocity)
                        && ballisticVelocity.y > 0 && Mathf.Abs(ballisticVelocity.magnitude - 18) < .01f, "Cannon must find an elevated fixed-speed intercept for a distant moving target.");
                    Require(!Ballistics2D.TryIntercept(Vector2.zero, new Vector2(1000, 0), Vector2.zero, 18, 3, out _), "Unreachable ballistic targets must not produce an invalid shot.");
                    mgGravityProbe = new GameObject("MG gravity probe").AddComponent<Projectile2D>();
                    cannonGravityProbe = new GameObject("Cannon gravity probe").AddComponent<Projectile2D>();
                    mgGravityProbe.Initialize(new Vector2(500, 500), Vector2.right, 18, 1, Color.cyan, false, 1000, .35f);
                    cannonGravityProbe.Initialize(new Vector2(500, 500), Vector2.right, 18, 1, Color.yellow, true, 1000, 3);
                    var cannonVisual = cannonGravityProbe.GetComponentInChildren<SpriteRenderer>();
                    Require(game.CannonProjectileSprite != null && cannonVisual.sprite == game.CannonProjectileSprite, "Cannon rounds must use the imported projectile art.");
                    Require(Mathf.Abs(cannonVisual.bounds.size.x - .65f) < .001f && cannonVisual.color == Color.white,
                        "Cannon shell art must be 0.65 units wide and preserve its original colors.");
                    groundMgProbe = new GameObject("Fast MG ground probe").AddComponent<Projectile2D>();
                    groundCannonProbe = new GameObject("Fast cannon ground probe").AddComponent<Projectile2D>();
                    groundFallingProbe = new GameObject("Falling ground probe").AddComponent<Projectile2D>();
                    groundMgProbe.Initialize(new Vector2(500, BattleGame.GroundHeight + .5f), Vector2.down, 200, 1, Color.cyan, false, 1000, .35f);
                    groundCannonProbe.Initialize(new Vector2(502, BattleGame.GroundHeight + .5f), Vector2.down, 200, 1, Color.yellow, true, 1000, 3);
                    groundFallingProbe.Initialize(new Vector2(504, BattleGame.GroundHeight + .16f), Vector2.right, .1f, 1, Color.yellow, true, 1000, 3);
                    Require(Mathf.Abs(game.TankCamera.rect.width - game.EnemyCamera.rect.width) < .0001f
                        && Mathf.Abs(game.TankCamera.rect.xMax - game.EnemyCamera.rect.xMin) < .0001f, "Cameras must form equal adjacent halves.");
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                        Require(enemy.transform.position.x - game.Artwork.transform.position.x >= 89.99f, "Wave must start 90 units from the tank, with trailing formation spacing.");
                    Require(game.MachineGun.SalvoSize == 3 && Mathf.Approximately(game.MachineGun.ShotDelay, .1f)
                        && Mathf.Approximately(game.MachineGun.ReloadTime, 1.25f), "Saved MG/AA salvo configuration must match the requested defaults.");
                    Require(game.MainGun.SalvoSize == 1 && Mathf.Approximately(game.MainGun.ReloadTime, 1.4f), "Main Gun must retain single-shot behavior.");
                    mgShotTimes.Clear(); mgShotFrameDeltas.Clear();
                    mainShotTimes.Clear(); mainShotFrameDeltas.Clear();
                    game.MachineGun.ShotFired += time => { mgShotTimes.Add(time); mgShotFrameDeltas.Add(Time.deltaTime); };
                    game.MainGun.ShotFired += time => { mainShotTimes.Add(time); mainShotFrameDeltas.Add(Time.deltaTime); };
                    Next();
                }
                else if (stage == 1 && elapsed > .25)
                {
                    Require(groundMgProbe == null && groundCannonProbe == null && groundFallingProbe == null,
                        "Fast MG bullets, cannon rounds and gravity-driven shells must all be destroyed at ground contact.");
                    float mgDrop = 500 - mgGravityProbe.transform.position.y;
                    float cannonDrop = 500 - cannonGravityProbe.transform.position.y;
                    Require(mgDrop > 0 && cannonDrop > mgDrop * 5, "Actual cannon projectiles must curve down much more strongly than MG bullets.");
                    Require(Mathf.Abs(mgGravityProbe.transform.position.x - cannonGravityProbe.transform.position.x) < .001f, "Gravity must not change horizontal velocity.");
                    var cannonVisual = cannonGravityProbe.GetComponentInChildren<SpriteRenderer>();
                    Require(Vector2.Dot(cannonVisual.transform.right, cannonGravityProbe.Velocity.normalized) > .9999f,
                        "Cannon shell nose must follow its curved flight direction.");
                    UnityEngine.Object.Destroy(mgGravityProbe.gameObject);
                    UnityEngine.Object.Destroy(cannonGravityProbe.gameObject);
                    Require(game.MachineGun.Target == null && game.MainGun.Target == null
                        && game.MachineGun.ShotsFired == 0 && game.MainGun.ShotsFired == 0, "Neither gun may fire at the distant spawn formation.");
                    removedFocus = game.TrackedEnemy;
                    Require(removedFocus != null, "Enemy camera must focus the nearest enemy on deployment.");
                    removedFocus.Damage(10000);
                    scoreBeforeHits = game.Score;
                    int groundIndex = 0, flyerIndex = 0;
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                    {
                        if (!enemy.Alive) continue;
                        bool flyer = enemy.Kind == EnemyKind.Flyer;
                        int index = flyer ? flyerIndex++ : groundIndex++;
                        enemy.transform.position = new Vector3((flyer ? 14 : 12) + index * 2, flyer ? 4.8f : -2.25f, 0);
                    }
                    cameraStart = game.EnemyCamera.transform.position;
                    cameraStartTime = Time.time;
                    Next();
                }
                else if (stage == 2 && elapsed > .25)
                {
                    Require(game.TrackedEnemy != null && game.TrackedEnemy != removedFocus, "Camera must select a surviving enemy when its focus dies.");
                    Require(Mathf.Approximately(game.EnemyCamera.transform.position.y, game.TankCamera.transform.position.y), "Retargeting must preserve the tank camera's height.");
                    float expectedMovement = (Time.time - cameraStartTime) * 3;
                    float actualMovement = Vector3.Distance(cameraStart, game.EnemyCamera.transform.position);
                    Require(Mathf.Abs(expectedMovement - actualMovement) < .2f, "Camera transition must move at three world units per second.");
                    Require(game.MachineGun.Target != null && game.MachineGun.Target.Kind == EnemyKind.Flyer, "MG/AA must prioritize flyers.");
                    Require(game.MainGun.Target != null && game.MainGun.Target.Kind == EnemyKind.Ground, "Main Gun must prioritize ground entities.");
                    Require(game.MachineGun.DamagePerShot < game.MainGun.DamagePerShot, "Main Gun must deal heavier damage.");
                    Next();
                }
                else if (stage == 3 && elapsed > 4)
                {
                    Require(mgShotTimes.Count >= 6, "MG/AA must complete at least two salvos automatically.");
                    // The first two salvos have continuous targets, so no target-acquisition gaps are expected.
                    CheckShotTiming(mgShotTimes.GetRange(0, 6), mgShotFrameDeltas.GetRange(0, 6), 3, .1f, 1.25f);
                    CheckShotTiming(mainShotTimes, mainShotFrameDeltas, 1, .1f, 1.4f);
                    Require(game.MachineGun.ShotsFired > game.MainGun.ShotsFired, "MG/AA must fire more rounds than Main Gun without input.");
                    Require(game.MainGun.ShotsFired >= 2, "Main Gun must fire automatically.");
                    Require(game.Score > scoreBeforeHits, "Actual projectiles must hit and kill enemies.");
                    foreach (EnemyEntity enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) enemy.Damage(10000);
                    Require(game.EnemyCount == 0, "Destroyed entities must leave the target list.");
                    Next();
                }
                else if (stage == 4 && elapsed > 3.5)
                {
                    Require(game.Wave == 2 && game.EnemyCount > 0, "Clearing enemies must advance the wave.");
                    cameraStart = game.EnemyCamera.transform.position;
                    cameraStartTime = Time.time;
                    stage = 40;
                    since = EditorApplication.timeSinceStartup;
                }
                else if (stage == 40 && elapsed > .25)
                {
                    float expectedMovement = (Time.time - cameraStartTime) * 10;
                    Require(Mathf.Abs(Vector3.Distance(cameraStart, game.EnemyCamera.transform.position) - expectedMovement) < .2f,
                        "Camera must move at ten units per second when catching up to a different wave.");
                    Require(Mathf.Approximately(game.EnemyCamera.transform.position.y, game.TankCamera.transform.position.y), "Fast wave transitions must preserve camera height.");
                    var target = game.TrackedEnemy;
                    target.transform.position = new Vector3(game.EnemyCamera.transform.position.x + .05f, target.transform.position.y, 0);
                    stage = 41;
                    since = EditorApplication.timeSinceStartup;
                }
                else if (stage == 41 && elapsed > .15)
                {
                    Require(Mathf.Approximately(game.CurrentEnemyCameraSpeed, 3), "Camera must return to normal speed after reaching the new wave.");
                    game.SetPaused(true);
                    pausedShots = game.MachineGun.ShotsFired;
                    cameraStart = game.EnemyCamera.transform.position;
                    stage = 5;
                    since = EditorApplication.timeSinceStartup;
                }
                else if (stage == 5 && elapsed > .5)
                {
                    Require(!game.Running && game.MachineGun.ShotsFired == pausedShots, "Pause must stop combat.");
                    Require(game.EnemyCamera.transform.position == cameraStart, "Pause must stop enemy-camera movement.");
                    Require(Mathf.Approximately(game.EnemyCamera.transform.position.y, game.TankCamera.transform.position.y), "Camera height must stay aligned while paused.");
                    game.SetPaused(false);
                    game.DamageHull(25);
                    Require(game.Hull == 75, "Hull damage failed.");
                    game.DamageHull(1000);
                    Require(game.Hull == 0 && !game.Running, "Hull destruction must end battle.");
                    game.StartBattle();
                    Require(game.Hull == 100 && game.Score == 0 && game.Wave == 1 && game.Running, "Restart must reset battle.");
                    Require(game.MachineGun.ShotsFired == 0 && game.MachineGun.CooldownFraction == 0, "Restart must reset salvo/reload state.");
                    // A 75-unit shot needs over three seconds: exercise the full cannon travel budget.
                    var distant = Array.Find(UnityEngine.Object.FindObjectsOfType<EnemyEntity>(), enemy => enemy.Alive && enemy.Kind == EnemyKind.Ground);
                    distant.transform.position = game.MainGun.transform.position + Vector3.right * 75;
                    rangeProbe = new GameObject("Range expiry probe").AddComponent<Projectile2D>();
                    rangeProbe.Initialize(new Vector2(500, 500), Vector2.right, 100, 1, Color.white, false, 1);
                    Next();
                }
                else if (stage == 6 && elapsed > 6.5)
                {
                    Require(rangeProbe == null, "Shells must expire when their travel-distance budget is exhausted.");
                    Require(game.Score > 0 && game.MainGun.ShotsFired >= 2, "Cannon shells must reach and kill a target 75 units away.");
                    Require(game.MachineGun.ShotsFired == 0, "MG must not attack the target outside its 50-unit range.");
                    Require(!errors, "Runtime errors were reported.");
                    Debug.Log("MEGATANKS_SMOKE_OK: camera transitions at 3 units/s within waves and 10 units/s across waves, normal-speed recovery, height alignment, ground contact, ballistics, salvos, pause and restart.");
                    Finish(0);
                }
                if (elapsed > 25) throw new Exception("Gameplay validation timed out.");
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
