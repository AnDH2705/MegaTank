using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class EnemyVehicleImporter
    {
        const string ArtRoot = "Assets/MegaTanks/Art/EnemyVehicles/";
        const string PrefabRoot = "Assets/MegaTanks/Prefabs/Enemies/";
        const float Ppu = 600;
        [Serializable] sealed class Part { public string name, file; public float[] size_px, pivot_canvas_px, pivot_local_px; }
        [Serializable] sealed class Manifest { public Part[] layers; }
        static Vector2 Position(float x, float y) => new Vector2(x - 768, 512 - y) / Ppu;

        [MenuItem("MegaTanks/Import Enemy Vehicle Art")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(PrefabRoot);
            var database = AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            if (database == null) throw new InvalidOperationException("Enemy data manager is missing.");
            ImportVehicle("car", "GroundEnemy1", 2.2f, database);
            ImportVehicle("plane", "FlyingEnemy1", 2.4f, database);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_ENEMY_ART_OK: car/plane layered prefabs, custom pivots, 12-frame propeller, muzzle sockets and enemy definitions linked.");
        }

        static void ConfigureSprite(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = 9; settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void ImportVehicle(string folder, string id, float width, EnemyDataManager database)
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtRoot + folder + "/parts_manifest.json"));
            foreach (Part part in manifest.layers)
                ConfigureSprite(ArtRoot + folder + "/" + part.file, new Vector2(part.pivot_local_px[0] / part.size_px[0], 1 - part.pivot_local_px[1] / part.size_px[1]));
            Sprite[] frames = new Sprite[0];
            if (folder == "plane")
            {
                frames = new Sprite[12];
                for (int i = 0; i < frames.Length; i++)
                {
                    string path = ArtRoot + folder + "/propeller_frames/propeller_" + (i + 1).ToString("00") + ".png";
                    ConfigureSprite(path, Vector2.one * .5f);
                    frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            var root = new GameObject(id + " Vehicle Art");
            try
            {
                SpriteRenderer propeller = null;
                for (int i = 0; i < manifest.layers.Length; i++)
                {
                    Part part = manifest.layers[i];
                    var obj = new GameObject(part.name);
                    obj.transform.SetParent(root.transform, false);
                    obj.transform.localPosition = Position(part.pivot_canvas_px[0], part.pivot_canvas_px[1]);
                    var renderer = obj.AddComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + folder + "/" + part.file);
                    renderer.sortingOrder = 10 + i;
                    if (part.name.Contains("propeller")) { propeller = renderer; propeller.sprite = frames[0]; }
                }
                var muzzle = new GameObject("Illustrated weapon muzzle").transform;
                muzzle.SetParent(root.transform, false);
                muzzle.localPosition = folder == "car" ? Position(1374, 391) : Position(1214, 744);
                var wheels = root.GetComponentsInChildren<Transform>().Where(part => part.name.Contains("wheel")).ToArray();
                root.AddComponent<EnemyVehicleArtwork>().Configure(muzzle, wheels, propeller, frames, folder == "car");
                string path = PrefabRoot + id + "Vehicle.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var definition = database.Get(id);
                definition.artworkPrefab = AssetDatabase.LoadAssetAtPath<EnemyVehicleArtwork>(path);
                definition.artworkWidth = width;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
