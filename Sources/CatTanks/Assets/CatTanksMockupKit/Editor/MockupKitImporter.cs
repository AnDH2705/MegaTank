using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;

namespace CatTanks.MockupKit.Editor
{
    [Serializable] public sealed class KitManifest { public KitSheet[] sheets; public KitOriginal[] originals; }
    [Serializable] public sealed class KitSheet
    {
        public string id, file;
        public int columns, rows;
        public bool alpha;
        public float border;
        public string[] keys;
    }
    [Serializable] public sealed class KitOriginal { public string key, file; }

    public static class MockupKitImporter
    {
        public const string Root = "Assets/CatTanksMockupKit";
        const string Generated = Root + "/Generated";
        const string LibraryPath = Generated + "/MockupKitLibrary.asset";

        [MenuItem("CatTanks/Mockup Kit/1 - Import and Slice Assets")]
        public static void Import()
        {
            var manifest = JsonUtility.FromJson<KitManifest>(File.ReadAllText(Root + "/Data/asset-manifest.json"));
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var library = AssetDatabase.LoadAssetAtPath<MockupKitLibrary>(LibraryPath);
            if (!library) { library = ScriptableObject.CreateInstance<MockupKitLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath); }
            library.entries.Clear();
            var keys = new HashSet<string>();
            var csv = new StringBuilder("key,texture,x_bottom_left,y_bottom_left,width,height,border_left,border_bottom,border_right,border_top\n");
            try
            {
                foreach (var sheet in manifest.sheets)
                {
                    var path = Root + "/" + sheet.file;
                    if (sheet.keys.Length != sheet.columns * sheet.rows) throw new InvalidOperationException("Grid count mismatch: " + path);
                    var texture = ReadPng(path);
                    try
                    {
                        var importer = Configure(path, sheet.keys.Length > 1);
                        if (sheet.keys.Length == 1)
                        {
                            importer.SaveAndReimport();
                            Register(library, keys, sheet.keys[0], AssetDatabase.LoadAssetAtPath<Sprite>(path));
                            csv.AppendLine(sheet.keys[0] + "," + path + ",0,0," + texture.width + "," + texture.height + ",0,0,0,0");
                            continue;
                        }
                        var factory = new SpriteDataProviderFactories(); factory.Init();
                        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
                        provider.InitSpriteEditorDataProvider();
                        var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
                        var rects = new List<SpriteRect>();
                        var pixels = texture.GetPixels32();
                        for (int index = 0; index < sheet.keys.Length; index++)
                        {
                            int column = index % sheet.columns, topRow = index / sheet.columns;
                            int left = column * texture.width / sheet.columns;
                            int right = (column + 1) * texture.width / sheet.columns;
                            int bottom = texture.height - (topRow + 1) * texture.height / sheet.rows;
                            int top = texture.height - topRow * texture.height / sheet.rows;
                            var cell = new RectInt(left, bottom, right - left, top - bottom);
                            var bounds = AlphaBounds(pixels, texture.width, cell, sheet.alpha);
                            float edge = Mathf.Floor(Mathf.Min(bounds.width, bounds.height) * sheet.border);
                            UnityEditor.GUID id;
                            if (!previous.TryGetValue(sheet.keys[index], out id)) id = UnityEditor.GUID.Generate();
                            var rect = new SpriteRect
                            {
                                name = sheet.keys[index], spriteID = id,
                                rect = new Rect(bounds.x, bounds.y, bounds.width, bounds.height),
                                alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                                border = new Vector4(edge, edge, edge, edge)
                            };
                            // Bar tracks must stretch without bending their endcaps.
                            if (rect.name.StartsWith("bar_") || rect.name.StartsWith("scroll_"))
                            {
                                float cap = Mathf.Floor(Mathf.Min(bounds.width, bounds.height) * .42f);
                                rect.border = new Vector4(cap, cap, cap, cap);
                            }
                            rects.Add(rect);
                            csv.AppendLine(rect.name + "," + path + "," + bounds.x + "," + bounds.y + "," + bounds.width + "," + bounds.height + "," + rect.border.x + "," + rect.border.y + "," + rect.border.z + "," + rect.border.w);
                        }
                        provider.SetSpriteRects(rects.ToArray());
                        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                        if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                        provider.Apply(); importer.SaveAndReimport();
                        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) Register(library, keys, sprite.name, sprite);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
                foreach (var original in manifest.originals)
                {
                    string path = Root + "/" + original.file;
                    var importer = Configure(path, false); importer.SaveAndReimport();
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Register(library, keys, original.key, sprite);
                    csv.AppendLine(original.key + "," + path + ",0,0," + sprite.rect.width + "," + sprite.rect.height + ",0,0,0,0");
                }
                EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
                File.WriteAllText(Generated + "/sprite-rects.csv", csv.ToString(), new UTF8Encoding(false));
                BuildAtlas(manifest);
                AssetDatabase.Refresh();
                Debug.Log("MOCKUP_KIT_IMPORT_OK: " + library.entries.Count + " named sprites. Rects: " + Generated + "/sprite-rects.csv");
            }
            catch (Exception ex) { Debug.LogError("MOCKUP_KIT_IMPORT_FAILED: " + ex); throw; }
        }
        static TextureImporter Configure(string path, bool multiple)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Texture not found: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.spritePivot = new Vector2(.5f, .5f);
            importer.alphaIsTransparency = true; importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false; importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 8192; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            return importer;
        }
        static Texture2D ReadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException("Cannot decode PNG: " + path);
            return texture;
        }
        static RectInt AlphaBounds(Color32[] pixels, int stride, RectInt cell, bool trim)
        {
            if (!trim) return cell;
            int xmin = cell.xMax, ymin = cell.yMax, xmax = -1, ymax = -1;
            int transparent = 0;
            for (int y = cell.yMin; y < cell.yMax; y++)
                for (int x = cell.xMin; x < cell.xMax; x++)
                {
                    if (pixels[y * stride + x].a <= 8) { transparent++; continue; }
                    xmin = Mathf.Min(xmin, x); ymin = Mathf.Min(ymin, y);
                    xmax = Mathf.Max(xmax, x); ymax = Mathf.Max(ymax, y);
                }
            if (xmax < xmin || ymax < ymin) throw new InvalidDataException("Blank sprite cell: " + cell);
            if (transparent < cell.width * cell.height / 50) throw new InvalidDataException("Missing transparent gutters: " + cell);
            if (xmin <= cell.xMin || xmax >= cell.xMax - 1 || ymin <= cell.yMin || ymax >= cell.yMax - 1)
                Debug.LogWarning("MockupKit: visible alpha reaches a cell boundary; inspect this sprite: " + cell);
            // Keep two transparent pixels around antialiased edges; never trim edge pixels.
            xmin = Mathf.Max(cell.xMin, xmin - 2); ymin = Mathf.Max(cell.yMin, ymin - 2);
            xmax = Mathf.Min(cell.xMax - 1, xmax + 2); ymax = Mathf.Min(cell.yMax - 1, ymax + 2);
            return new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1);
        }
        static void Register(MockupKitLibrary library, HashSet<string> keys, string key, Sprite sprite)
        {
            if (!sprite) throw new InvalidDataException("Missing imported sprite: " + key);
            if (!keys.Add(key)) throw new InvalidDataException("Duplicate sprite key: " + key);
            library.entries.Add(new MockupKitLibrary.Entry { key = key, sprite = sprite });
        }
        static void BuildAtlas(KitManifest manifest)
        {
            string path = Generated + "/MockupUI.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (!atlas) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, path); }
            var packing = atlas.GetPackingSettings(); packing.enableRotation = false; packing.enableTightPacking = false; packing.padding = 8;
            atlas.SetPackingSettings(packing);
            var texture = atlas.GetTextureSettings(); texture.generateMipMaps = false; texture.filterMode = FilterMode.Bilinear;
            atlas.SetTextureSettings(texture);
            var old = atlas.GetPackables(); if (old.Length > 0) atlas.Remove(old);
            atlas.Add(manifest.sheets.Where(s => s.id.StartsWith("ui_") || s.id.StartsWith("icons_"))
                .Select(s => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + s.file)).Cast<UnityEngine.Object>().ToArray());
            atlas.SetIncludeInBuild(true); EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssets();
        }
        [MenuItem("CatTanks/Mockup Kit/2 - Build All Preview Prefabs")]
        public static void BuildAll()
        {
            var library = AssetDatabase.LoadAssetAtPath<MockupKitLibrary>(LibraryPath);
            if (!library) throw new InvalidOperationException("Run Import and Slice Assets first.");
            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/screen-layouts.json");
            var layouts = JsonUtility.FromJson<KitLayouts>(data.text);
            Directory.CreateDirectory(Generated + "/Prefabs"); AssetDatabase.Refresh();
            foreach (var screen in layouts.screens)
            {
                var root = new GameObject("Mockup_" + screen.key, typeof(RectTransform));
                try
                {
                    var preview = root.AddComponent<MockupKitPreview>(); preview.library = library; preview.layoutData = data;
                    preview.Build(screen.key);
                    PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Prefabs/Mockup_" + screen.key + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets(); Debug.Log("MOCKUP_KIT_PREFABS_OK: " + layouts.screens.Length + " assembled previews.");
        }
        [MenuItem("CatTanks/Mockup Kit/3 - Create Preview Scene")]
        public static void CreateScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Generated + "/Prefabs/Mockup_march.prefab");
            if (!prefab) throw new InvalidOperationException("Build the preview prefabs first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save untitled scenes before creating a preview scene. Existing scenes have not been changed.");
            // Additive: do not close or overwrite a user's current scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            var events = new GameObject("MockupKit EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(events, scene);
            var camera = new GameObject("MockupKit Preview Camera", typeof(Camera), typeof(AudioListener));
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.02f, .04f, .08f);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera, scene);
            string path = AssetDatabase.GenerateUniqueAssetPath(Generated + "/MockupKitPreview.unity");
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("MockupKit preview scene: " + path + ". Open this scene alone before entering Play Mode.");
        }
        [MenuItem("CatTanks/Mockup Kit/4 - Validate Sprite and Layout Coverage")]
        public static void Validate()
        {
            MockupKitValidation.ValidateData();
        }
        [MenuItem("CatTanks/Mockup Kit/5 - Export Unitypackage")]
        public static void Export()
        {
            string path = EditorUtility.SaveFilePanel("Export Mockup Kit", "", "CatTanksMockupKit", "unitypackage");
            if (!string.IsNullOrEmpty(path)) AssetDatabase.ExportPackage(Root, path, ExportPackageOptions.Recurse);
        }
    }
}
