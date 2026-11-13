using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MCPForUnity.Editor.Services;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private static bool connectingMcp;
        static ProjectSetup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += ConnectMcp;
        }
        [MenuItem("CatTanks/Connect MCP")]
        public static async void ConnectMcp()
        {
            if (Application.isBatchMode || connectingMcp) return;
            connectingMcp = true;
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            try
            {
                var server = MCPServiceLocator.Server;
                if (!server.IsLocalHttpServerReachable())
                {
                    if (!server.StartLocalHttpServer(quiet: true))
                    {
                        Debug.LogWarning("CatTanks MCP: could not start the local server. Use Window > MCP for Unity to check Python/uv settings.");
                        return;
                    }
                    for (int attempt = 0; attempt < 120 && !server.IsLocalHttpServerReachable(); attempt++)
                        await Task.Delay(500);
                    if (!server.IsLocalHttpServerReachable())
                    {
                        Debug.LogWarning("CatTanks MCP: local server did not become ready within 60 seconds. Check " + server.GetLocalHttpServerLaunchLogPath());
                        return;
                    }
                }
                bool connected = await MCPServiceLocator.Bridge.StartAsync();
                if (connected) Debug.Log("CatTanks MCP connected: True");
                else Debug.LogWarning("CatTanks MCP: bridge could not connect. Use CatTanks > Connect MCP to retry.");
            }
            catch (Exception e) {Debug.LogWarning("CatTanks MCP: " + e.Message + ". Use Window > MCP for Unity to reconnect.");}
            finally { connectingMcp = false; }
        }
        [MenuItem("CatTanks/Create Main Scene")]
        public static void CreateProject()
        {
            PlayerSettings.companyName = "MegaTank"; PlayerSettings.productName = "Meow Bastion - CatTanks";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = false; PlayerSettings.resizableWindow = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            QualitySettings.vSyncCount = 1; QualitySettings.antiAliasing = 2;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(800, 450, -1000); var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 450; cam.farClipPlane = 3000; cam.backgroundColor = Catalog.Color("#041828");
            var light = new GameObject("Moonlight", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional; light.transform.eulerAngles = new Vector3(35, -30, 0);
            var game = new GameObject("CatTanks Game", typeof(CatTanksGame));
            PrefabUtility.SaveAsPrefabAsset(game, "Assets/CatTanks/Prefabs/CatTanksGame.prefab");
            EditorSceneManager.SaveScene(scene, "Assets/CatTanks/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene("Assets/CatTanks/Scenes/Main.unity", true)};
            AssetDatabase.SaveAssets(); Debug.Log("CATTANKS_PROJECT_READY Unity " + Application.unityVersion);
        }
        [MenuItem("CatTanks/Validate Port")]
        public static void ValidatePort()
        {
            var assertions = new List<string>();
            void Check(bool condition, string name) {if (!condition) throw new Exception("FAILED: " + name); assertions.Add(name);}
            var db = Catalog.Load();
            Check(db.weapons.Count == 40 && db.monsters.Count == 20 && db.cats.Count == 10 && db.stages.Count == 20, "Original catalogs: 40 weapons, 20 monsters, 10 cats, 20 stages");
            foreach (var w in db.weapons) Check(Resources.Load<Texture2D>("Art/" + w.asset) != null, "Weapon art: " + w.id);
            foreach (var m in db.monsters) Check(Resources.Load<Texture2D>("Art/" + m.asset) != null, "Monster art: " + m.asset);
            var p = Profile.New(db); Check(p.cats.Count == 3 && p.FloorCount == 1 && p.Get("coins") == 2000, "Starter profile and wallet");
            Check(Battle.RatingDelta(1000, 1000, true) == 16 && Battle.RatingDelta(1000, 1000, false) == -16, "Arena Elo at equal ratings");
            var petProfile = Profile.New(db); petProfile.GainPet("beast-5", db); var petStats = petProfile.PetStats(petProfile.pets[0], db);
            Check(petProfile.pets[0].ivs.Length == 4 && petProfile.pets[0].ivs.All(iv => iv >= 0 && iv <= 15) && petStats["atk"] >= 44, "Companion four IVs and stat calculation");
            var withPet = new Battle(db, petProfile);
            Check(withPet.units.Any(u => u.role == "companion") && Mathf.Abs(withPet.maxHp - 13056) < .1f, "Companion roster and species HP passive");
            var arenaBattle = new Battle(db, p, 5, false, true, 2);
            Check(arenaBattle.opponent.name == "Mực Ống" && !arenaBattle.waves[0].boss && arenaBattle.waves[0].types.All(t => (string)t != "beast-8") && db.stages[4].waves[0].boss, "Arena wave isolation and boss removal");
            Check(!p.Spend(("coins", 3000), ("gem", 1)) && p.Get("gem") == 300 && p.Get("coins") == 2000, "Spending is atomic");
            p.lastIdle = Profile.Now - 90000; var idle = p.Idle(Profile.Now); p.lastIdle = Profile.Now - 43200;
            Check(idle["coins"] == p.Idle(Profile.Now)["coins"], "Offline income caps at 12 hours");
            p.lastIdle = Profile.Now + 1000; Check(p.Idle(Profile.Now)["coins"] == 0, "Clock rollback cannot create negative income");
            string json = JsonConvert.SerializeObject(p); var copy = JsonConvert.DeserializeObject<Profile>(json); Check(copy.cats[0].id == "cat-0" && copy.wallet.Count == p.wallet.Count, "Profile serialization round trip");
            var battle = new Battle(db, p); Check(battle.units.Count == 6 && battle.energy == 200 && battle.maxHp == 12800, "Campaign loadout starter values");
            float energy = battle.energy; Check(battle.UseSkill("heal") && battle.energy == energy - 80, "Skill energy cost");
            Check(!battle.UseSkill("heal"), "Skill cooldown rejects duplicate activation");
            var wolf = battle.Spawn("beast-0", 800); Check(Mathf.Abs(wolf.maxHp - 234) < .1f, "Stage-1 enemy health multiplier");
            var shell = db.Weapon("turret-0").Copy(); shell.damage = 10000; battle.MakeBullet(wolf.position, 0, shell, wolf); battle.Step(.01f);
            Check(wolf.dead && battle.kills == 1, "Projectile swept collision and kill rewards");
            var classic = new Battle(db, p, 1, true); Check(classic.units.Count == 19 && classic.hp == 48000 && classic.skills.Count == 6, "Classic loadout and six skills");
            classic.time = 11; classic.Step(.01f); Check(classic.units[0].weapon.id == "turret-5", "Classic weapon rotation every 10 seconds");
            p.Add("moonCage", 3); p.Add("beastCage", 10); var capture = new Battle(db, p);
            Check(capture.cages == 2 && capture.moonCages == 3, "Capture loadout caps five total cages and prioritizes moon cages");
            int cageCount = p.Get("beastCage"); Check(!capture.Catch(false) && p.Get("beastCage") == cageCount, "Capture without a selected enemy never spends a cage");
            var candidate = capture.Spawn("bug-0", 800); capture.targetId = candidate.id;
            Check(!capture.Catch(false) && p.Get("beastCage") == cageCount, "Healthy targets cannot consume cages");
            try
            {
                Profile.TestSavePath = Path.GetFullPath("Library/PortValidation/profile.json");
                var diskProfile = Profile.New(db); diskProfile.Save(); diskProfile.Add("coins", 1); diskProfile.Save();
                Check(Profile.Load(db).Get("coins") == 2001 && File.Exists(Profile.SavePath + ".bak"), "Atomic disk save and backup reload");
                var resultProfile = Profile.New(db); var result = new Battle(db, resultProfile); result.state = BattleState.Won;
                result.Finish(); int awarded = resultProfile.Get("coins"); result.Finish();
                Check(resultProfile.Get("coins") == awarded && resultProfile.clears["stage-1"] == 1, "Battle result settles once without duplicate rewards");
                var escapedProfile = Profile.New(db); var escaped = new Battle(db, escapedProfile); escaped.caught.Add("bug-0"); escaped.state = BattleState.Lost; escaped.Finish();
                Check(escapedProfile.pets.Count == 0, "Captured monsters escape after a lost battle");
                candidate.hp = candidate.maxHp * .1f; capture.Catch(false);
                Check(p.Get("beastCage") == cageCount - 1 && capture.throwsUsed == 1, "Valid capture spends exactly one cage and persists it");
            }
            finally {Profile.TestSavePath = null;}
            var results = new List<object>();
            for (int i = 1; i <= 20; i++)
            {
                UnityEngine.Random.InitState(100 + i); var profile = Profile.New(db); profile.accountLevel = 100;
                profile.cats.ForEach(c => c.level = 40); profile.floors[1].cat = "";
                var simulation = new Battle(db, profile, i); int frames = 0;
                while (simulation.state == BattleState.Playing && frames++ < 10000)
                {
                    foreach (var s in simulation.skills) if (simulation.Cooldown(s.id) <= 0 && simulation.energy >= s.cost && (s.id != "heal" || simulation.hp < simulation.maxHp * .7f)) simulation.UseSkill(s.id);
                    simulation.Step(.05f);
                }
                Check(simulation.state != BattleState.Playing, "Stage " + i + " reaches a terminal state without stalling");
                Check(!float.IsNaN(simulation.hp) && simulation.bullets.Count < 1000 && simulation.effects.Count < 500, "Stage " + i + " finite and bounded simulation");
                results.Add(new {stage = i, state = simulation.state.ToString(), seconds = simulation.time, simulation.kills, hp = simulation.hp});
            }
            var report = new {unityVersion = Application.unityVersion, time = DateTime.UtcNow, passed = assertions.Count, assertions, simulations = results};
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/port-validation.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            Debug.Log("CATTANKS_VALIDATION_PASSED " + assertions.Count + " checks; report: Logs/port-validation.json");
        }
        public static void BuildMac()
        {
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/macOS/CatTanks.app", BuildTarget.StandaloneOSX, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("CATTANKS_BUILD_SUCCEEDED " + report.summary.totalSize);
        }
        public static void SetupAndValidate() {CreateProject(); ValidatePort();}
    }
    public class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/CatTanks/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter; importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
