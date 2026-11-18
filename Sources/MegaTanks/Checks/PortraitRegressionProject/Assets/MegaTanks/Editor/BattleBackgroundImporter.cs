using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BattleBackgroundImporter
    {
        public const string CatalogPath = "Assets/MegaTanks/Data/Backgrounds/BattleBackgrounds.asset";
        const string ArtRoot = "Assets/MegaTanks/Art/Backgrounds/";
        [Serializable] sealed class Layer { public string file, repeat_x; public float parallax_factor; }
        [Serializable] sealed class Config { public Layer[] layers; }
        [MenuItem("MegaTanks/Import Battle Backgrounds")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            EnsureLayer(BattleBackgroundController.TankLayerName); EnsureLayer(BattleBackgroundController.EnemyLayerName);
            var catalog = AssetDatabase.LoadAssetAtPath<BattleBackgroundCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)); AssetDatabase.Refresh();
                catalog = ScriptableObject.CreateInstance<BattleBackgroundCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.presets = new[] { ImportPreset("Industrial"), ImportPreset("RuinedCity") };
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<BattleGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("backgroundCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
            Debug.Log("MEGATANKS_BACKGROUNDS_IMPORTED: Industrial 6 layers, RuinedCity 7 layers, original canvas/alpha, parallax factors, isolated camera layers and scene catalog linked.");
        }
        static BattleBackgroundPreset ImportPreset(string id)
        {
            string root = ArtRoot + id + "/";
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(root + "parallax_config.json"));
            return new BattleBackgroundPreset { id = id, layers = config.layers.Select(layer =>
            {
                string path = root + layer.file;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spritePivot = Vector2.one * .5f;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
                return new BattleBackgroundLayer { sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                    parallaxFactor = layer.parallax_factor, repeatHorizontally = layer.repeat_x != "none" };
            }).ToArray() };
        }
        static void EnsureLayer(string name)
        {
            if (LayerMask.NameToLayer(name) >= 0) return;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 29; i >= 8; i--)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layers.GetArrayElementAtIndex(i).stringValue = name; tags.ApplyModifiedPropertiesWithoutUndo(); return; }
            throw new InvalidOperationException("No free Unity layer for " + name);
        }
    }
}
