using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class MortarArtImporter
    {
        const string ArtRoot = "Assets/MegaTanks/Art/Mortar/";
        [Serializable] sealed class Part { public string name, file; public float[] size_px, pivot_canvas_px, pivot_local_px; }
        [Serializable] sealed class Socket { public float[] position_canvas_px; }
        [Serializable] sealed class Manifest { public Part[] layers; public float[] original_tank_offset_px; public Socket mortar_socket; }
        static Vector2 Point(float[] value) => new Vector2(value[0], -value[1]) / 180;

        [MenuItem("MegaTanks/Import Mortar Artwork")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var manifest = ReadManifest();
            foreach (Part part in manifest.layers.Where(part => part.name == "09_mortar_barrel" || part.name == "10_mortar_front_support"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(ArtRoot + part.file);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 180; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = 9;
                settings.spritePivot = new Vector2(part.pivot_local_px[0] / part.size_px[0], 1 - part.pivot_local_px[1] / part.size_px[1]);
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            var root = PrefabUtility.LoadPrefabContents(TankArtImporter.PrefabPath);
            try { AttachTo(root); PrefabUtility.SaveAsPrefabAsset(root, TankArtImporter.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_MORTAR_ART_OK: barrel/support imported at native pivots, v4 canvas offset aligned to existing tank, muzzle socket and recoil bound.");
        }
        static Manifest ReadManifest() => JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtRoot + "metadata/parts_manifest.json"));
        public static void AttachTo(GameObject tank)
        {
            if (!File.Exists(ArtRoot + "metadata/parts_manifest.json")) return;
            var manifest = ReadManifest();
            var body = tank.GetComponentsInChildren<Transform>().Single(part => part.name == "body");
            var existing = body.Find("mortar");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            Part tube = manifest.layers.Single(part => part.name == "09_mortar_barrel");
            Part support = manifest.layers.Single(part => part.name == "10_mortar_front_support");
            var root = new GameObject("mortar").transform; root.SetParent(body, false);
            // v4 shifts the original tank 120 pixels right; the existing body's native pivot is (525,1040).
            root.localPosition = Point(tube.pivot_canvas_px) - Point(manifest.original_tank_offset_px) - Point(new float[] { 525, 1040 });
            Transform barrel = CreatePart(root, tube, Vector2.zero, 55);
            CreatePart(root, support, Point(support.pivot_canvas_px) - Point(tube.pivot_canvas_px), 56);
            var muzzle = new GameObject("Mortar muzzle").transform; muzzle.SetParent(barrel, false);
            muzzle.localPosition = Point(manifest.mortar_socket.position_canvas_px) - Point(tube.pivot_canvas_px);
            var artwork = root.gameObject.AddComponent<MortarArtwork>(); artwork.Configure(barrel, muzzle);
            tank.GetComponent<TankArtwork>().BindMortar(artwork);
        }
        static Transform CreatePart(Transform parent, Part part, Vector2 position, int order)
        {
            var obj = new GameObject(part.name); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + part.file); renderer.sortingOrder = order;
            return obj.transform;
        }
    }
}
