using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTanks.MockupKit.Editor
{
    [InitializeOnLoad]
    public static class MockupKitValidation
    {
        const string Root = MockupKitImporter.Root;
        const string Pending = "MockupKit.ValidationPending";
        const string Result = "MockupKit.ValidationResult";
        const string Before = "MockupKit.ProfileBefore";
        const string Report = "Logs/mockup-kit-validation.json";
        static readonly List<string> Checks = new List<string>();
        static readonly List<string> Errors = new List<string>();

        static MockupKitValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) RunPlayChecks();
                if (state == PlayModeStateChange.EnteredEditMode) Finish();
            };
        }
        static KitLayouts Layouts() => JsonUtility.FromJson<KitLayouts>(File.ReadAllText(Root + "/Data/screen-layouts.json"));
        static void Check(bool pass, string description)
        {
            if (!pass) throw new InvalidOperationException(description);
            Checks.Add(description);
        }
        public static void ValidateData()
        {
            var library = AssetDatabase.LoadAssetAtPath<MockupKitLibrary>(Root + "/Generated/MockupKitLibrary.asset");
            if (!library) throw new InvalidOperationException("Import the assets first.");
            var manifest = JsonUtility.FromJson<KitManifest>(File.ReadAllText(Root + "/Data/asset-manifest.json"));
            var expected = manifest.sheets.SelectMany(s => s.keys).Concat(manifest.originals.Select(o => o.key)).ToArray();
            Check(expected.Length == expected.Distinct().Count(), "Manifest sprite keys are unique");
            Check(library.entries.Count == expected.Length && library.entries.All(e => e.sprite) &&
                library.entries.Select(e => e.key).Distinct().Count() == expected.Length && expected.All(k => library.Get(k)), "Imported library exactly covers manifest");
            var layouts = Layouts();
            var screens = layouts.screens.ToDictionary(s => s.key);
            var skills = new HashSet<string>(CatTanks.Catalog.Load().skills.Select(s => s.id));
            var kinds = new HashSet<string>(new[] { "image", "label", "button", "bar", "viewport", "input", "group" });
            foreach (var screen in layouts.screens)
            {
                Check(!screen.popup || (screens.ContainsKey(screen.parentKey) && !screens[screen.parentKey].popup), screen.key + " has valid popup background");
                var ids = new HashSet<string>();
                foreach (var node in screen.nodes)
                {
                    if (string.IsNullOrEmpty(node.id) || !ids.Add(node.id)) throw new InvalidDataException(screen.key + ": duplicate or empty node id");
                    if (!string.IsNullOrEmpty(node.parent) && (!ids.Contains(node.parent) || node.parent == node.id)) throw new InvalidDataException(screen.key + ": missing or unordered parent " + node.parent);
                    if (!kinds.Contains(node.kind) || !(node.w > 0 && node.h > 0) || float.IsInfinity(node.w + node.h + node.x + node.y) || float.IsNaN(node.x + node.y)) throw new InvalidDataException(screen.key + ": invalid node " + node.id);
                    if (!string.IsNullOrEmpty(node.sprite) && !library.Get(node.sprite)) throw new InvalidDataException(screen.key + ": missing sprite " + node.sprite);
                    if (node.kind == "bar" && !library.Get(node.text)) throw new InvalidDataException(screen.key + ": missing fill " + node.text);
                    if (string.IsNullOrEmpty(node.action)) continue;
                    if (node.action.StartsWith("screen:") && !screens.ContainsKey(node.action.Substring(7))) throw new InvalidDataException("Unknown screen action: " + node.action);
                    if (node.action.StartsWith("game:upgradeSkill:") && !skills.Contains(node.action.Substring(18))) throw new InvalidDataException("Unknown game skill: " + node.action);
                    if (node.action != "close" && !node.action.StartsWith("screen:") && !node.action.StartsWith("game:")) throw new InvalidDataException("Unknown action: " + node.action);
                }
            }
            var reached = new HashSet<string> { "march" }; var queue = new Queue<string>(); queue.Enqueue("march");
            while (queue.Count > 0)
                foreach (var node in screens[queue.Dequeue()].nodes)
                    if (node.action != null && node.action.StartsWith("screen:") && reached.Add(node.action.Substring(7))) queue.Enqueue(node.action.Substring(7));
            Check(reached.Count == screens.Count, "All screens are reachable from march");
            Debug.Log("MOCKUP_KIT_VALIDATE_OK: " + library.entries.Count + " sprites / " + screens.Count + " screens, including actions and navigation.");
        }

        // Run in a dedicated editor process; never replace a user's open scenes.
        public static void RunPipeline()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this validation in a dedicated Unity batch process.");
            try
            {
                Directory.CreateDirectory("Logs");
                SessionState.SetString(Before, ProfileFingerprint());
                MockupKitImporter.Import(); ValidateData(); MockupKitImporter.BuildAll();
                EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity", OpenSceneMode.Single);
                MockupKitImporter.CreateScene();
                string path = SceneManager.GetSceneAt(SceneManager.sceneCount - 1).path;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Check(UnityEngine.Object.FindObjectsOfType<CatTanks.PreviewIsolation>(true).Length == 1, "Generated preview has isolation marker");
                Check(Layouts().screens.All(s => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/Prefabs/Mockup_" + s.key + ".prefab")), "All 21 preview prefabs generated");
                SessionState.SetBool(Pending, true);
                SessionState.SetString(Result, "");
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static async void RunPlayChecks()
        {
            Application.logMessageReceived += RecordError;
            GameObject cameraObject = null;
            RenderTexture target = null;
            try
            {
                await Task.Delay(150);
                Check(CatTanks.PreviewIsolation.IsActive, "Preview isolation survives Play Mode domain reload");
                Check(UnityEngine.Object.FindObjectsOfType<CatTanks.CatTanksGame>(true).Length == 0, "Game bootstrap did not create CatTanksGame");
                Check(UnityEngine.Object.FindObjectsOfType<CatTanks.StudioUIRoot>(true).Length == 0, "No live-game Canvas was created");
                Check(!GameObject.Find("Battle World"), "No live battle world was created");
                bool loadBlocked = false, saveBlocked = false;
                try { CatTanks.Profile.Load(null); } catch (InvalidOperationException) { loadBlocked = true; }
                try { new CatTanks.Profile().Save(); } catch (InvalidOperationException) { saveBlocked = true; }
                Check(loadBlocked && saveBlocked, "Live profile load and save are blocked before filesystem access");
                var probe = new GameObject("Accidental game in preview").AddComponent<CatTanks.CatTanksGame>();
                Check(!probe.enabled && probe.profile == null && probe.view == null, "Explicit game component also refuses to initialize in preview");
                UnityEngine.Object.DestroyImmediate(probe.gameObject);
                var preview = UnityEngine.Object.FindObjectOfType<MockupKitPreview>();
                var canvas = preview.GetComponent<Canvas>();
                cameraObject = new GameObject("Mockup validation camera");
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.02f, .04f, .08f);
                camera.orthographic = true; camera.orthographicSize = 450; camera.nearClipPlane = .01f; camera.farClipPlane = 100;
                target = new RenderTexture(1600, 900, 24); camera.targetTexture = target;
                Directory.CreateDirectory("Captures/MockupKit");
                foreach (var screen in Layouts().screens)
                {
                    preview.Build(screen.key); await Task.Delay(50);
                    Canvas.ForceUpdateCanvases();
                    Check(preview.CurrentScreen == screen.key, screen.key + " builds in Play Mode");
                    Check(preview.GetComponentsInChildren<Image>().All(i => i.sprite || i.name == "ModalScrim"), screen.key + " has no missing image references");
                    Check(preview.GetComponentsInChildren<Image>().Where(i => i.sprite && i.sprite.border.sqrMagnitude > 0 && !i.preserveAspect && i.name != "Fill").All(i => i.type == Image.Type.Sliced), screen.key + " uses sliced panel and button geometry");
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                    Canvas.ForceUpdateCanvases(); camera.Render(); Capture(target, screen.key);
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                }
                await Click(preview, "march", "screen:prebattle", "prebattle");
                await Click(preview, "prebattle", "screen:battle", "battle");
                await Click(preview, "battle", "screen:result", "result");
                await Click(preview, "battle", "screen:pause", "pause");
                await Click(preview, "pause", "close", "battle");
                await Click(preview, "town-shop", "screen:settings", "settings");
                preview.Navigate("close"); Check(preview.CurrentScreen == "town-shop", "Popup returns to its actual opening screen");
                string received = null; preview.onGameAction = action => received = action;
                await Click(preview, "barracks-skills", "game:upgradeSkill:heal", "barracks-skills");
                Check(received == "game:upgradeSkill:heal", "Canonical skill action reaches preview callback");
                preview.onGameAction = null;
                Check(Errors.Count == 0, "No runtime errors during rendering and navigation");
                Check(ProfileFingerprint() == SessionState.GetString(Before, ""), "Profile and backup unchanged during Play Mode");
                SessionState.SetString(Result, JsonConvert.SerializeObject(new { verdict = "PASS", unity = Application.unityVersion, renderedScreens = Layouts().screens.Length, passed = Checks.Count, checks = Checks, errors = Errors }));
            }
            catch (Exception e)
            {
                SessionState.SetString(Result, JsonConvert.SerializeObject(new { verdict = "FAIL", error = e.ToString(), checks = Checks, errors = Errors }));
                Debug.LogException(e);
            }
            finally
            {
                Application.logMessageReceived -= RecordError;
                if (cameraObject) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                EditorApplication.isPlaying = false;
            }
        }
        static async Task Click(MockupKitPreview preview, string from, string action, string expected)
        {
            preview.Build(from); await Task.Delay(70);
            var binding = preview.GetComponentsInChildren<MockupKitAction>().First(a => a.action == action && a.GetComponent<Button>().interactable);
            binding.GetComponent<Button>().onClick.Invoke();
            Check(preview.CurrentScreen == expected, "Button " + from + " / " + action + " -> " + expected);
        }
        static void Capture(RenderTexture target, string key)
        {
            var old = RenderTexture.active; var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); var colors = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i += 37) colors.Add((pixels[i].r << 16) | (pixels[i].g << 8) | pixels[i].b);
                Check(colors.Count > 100, key + " render contains UI pixels");
                File.WriteAllBytes("Captures/MockupKit/" + key + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void RecordError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Errors.Add(message);
        }
        static string ProfileFingerprint()
        {
            string path = CatTanks.Profile.SavePath;
            using (var sha = SHA256.Create())
                return string.Join("|", new[] { path, path + ".bak", path + ".tmp" }.Select(p => File.Exists(p) ? Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))) : "absent"));
        }
        public static void RunLiveGameRegression()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a dedicated batch process for this regression check.");
            string fingerprint = ProfileFingerprint(); string savedPath = CatTanks.Profile.TestSavePath;
            try
            {
                EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity", OpenSceneMode.Single);
                Check(!CatTanks.PreviewIsolation.IsActive, "Main game scene is not isolated");
                CatTanks.Profile.TestSavePath = Path.GetFullPath("Logs/mockup-profile-regression.json");
                var profile = CatTanks.Profile.New(CatTanks.Catalog.Load()); profile.selected = 7; profile.Save();
                Check(CatTanks.Profile.Load(CatTanks.Catalog.Load()).selected == 7, "Live profile API works outside preview, using a test file only");
                CatTanks.Editor.ProjectSetup.ValidatePort();
            }
            finally { CatTanks.Profile.TestSavePath = savedPath; }
            Check(ProfileFingerprint() == fingerprint, "Existing profile unchanged by live-game regression");
            Debug.Log("MOCKUP_LIVE_GAME_REGRESSION_PASS: profile API and 124 gameplay checks; existing profile unchanged.");
        }
        static void Finish()
        {
            SessionState.SetBool(Pending, false);
            string result = SessionState.GetString(Result, "");
            bool unchanged = ProfileFingerprint() == SessionState.GetString(Before, "");
            var report = string.IsNullOrEmpty(result) ? new Newtonsoft.Json.Linq.JObject { ["verdict"] = "FAIL", ["error"] = "Play validation did not finish" } : Newtonsoft.Json.Linq.JObject.Parse(result);
            report["profileUnchangedAfterStop"] = unchanged;
            if (!unchanged) report["verdict"] = "FAIL";
            File.WriteAllText(Report, report.ToString());
            bool pass = (string)report["verdict"] == "PASS";
            Debug.Log("MOCKUP_KIT_UNITY_VALIDATION_" + (pass ? "PASS" : "FAIL") + ": " + Report);
            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
