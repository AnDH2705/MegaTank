using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class TankArtImporter
    {
        public const string PrefabPath = "Assets/MegaTanks/Prefabs/TankCutaway.prefab";
        const string ArtPath = "Assets/MegaTanks/Art/";
        const float PixelsPerUnit = 180;
        [Serializable] sealed class Group { public float[] pivot_px; public string parent; }
        [Serializable] sealed class Groups { public Group root, body, cannon, roof_gun, driver_controls; }
        [Serializable] sealed class Part
        {
            public string name, file, parent;
            public float[] size_px, pivot_canvas_px, pivot_local_px;
            public int z_order;
        }
        [Serializable] sealed class PartsManifest { public Groups groups; public Part[] layers; }
        [Serializable] sealed class Frame { public int index; public float[] sheet_rect_px, pivot_px; }
        [Serializable] sealed class Clip
        {
            public string name, sheet;
            public float duration_ms, preview_scale, rotation_cw_deg;
            public float[] socket;
            public Frame[] frames;
        }
        [Serializable] sealed class VfxManifest { public float[] sheet_px; public Clip[] clips; }

        static Vector2 Point(float[] p) => new Vector2(p[0], -p[1]) / PixelsPerUnit;

        [MenuItem("MegaTanks/Import Tank Artwork")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var manifest = JsonUtility.FromJson<PartsManifest>(File.ReadAllText(ArtPath + "Tank/metadata/parts_manifest.json"));
            var vfx = JsonUtility.FromJson<VfxManifest>(File.ReadAllText(ArtPath + "VFX/metadata/vfx_manifest.json"));
            foreach (Part part in manifest.layers)
            {
                string path = ArtPath + "Tank/" + part.file;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                ConfigureTexture(importer);
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = 9; // custom
                settings.spritePivot = new Vector2(part.pivot_local_px[0] / part.size_px[0], 1 - part.pivot_local_px[1] / part.size_px[1]);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            foreach (Clip clip in vfx.clips)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath + "VFX/" + clip.sheet);
                ConfigureTexture(importer);
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = clip.frames.Select(frame => new SpriteMetaData
                {
                    name = clip.name + "_" + frame.index.ToString("00"),
                    rect = new Rect(frame.sheet_rect_px[0], vfx.sheet_px[1] - frame.sheet_rect_px[1] - frame.sheet_rect_px[3], frame.sheet_rect_px[2], frame.sheet_rect_px[3]),
                    alignment = 9,
                    pivot = new Vector2(frame.pivot_px[0] / frame.sheet_rect_px[2], 1 - frame.pivot_px[1] / frame.sheet_rect_px[3])
                }).ToArray();
                importer.SaveAndReimport();
            }

            var root = new GameObject("Tank Cutaway");
            try
            {
                var definitions = new Dictionary<string, Group>
                {
                    {"root", manifest.groups.root}, {"body", manifest.groups.body},
                    {"cannon", new Group { parent = "body", pivot_px = manifest.layers.First(p => p.name == "08_main_gun_barrel").pivot_canvas_px }},
                    {"roof_gun", manifest.groups.roof_gun},
                    {"driver_controls", manifest.groups.driver_controls}
                };
                var groups = new Dictionary<string, Transform> { {"root", root.transform} };
                foreach (string name in new[] { "body", "cannon", "roof_gun", "driver_controls" })
                {
                    Group definition = definitions[name];
                    var group = new GameObject(name).transform;
                    group.SetParent(groups[definition.parent], false);
                    group.localPosition = Point(definition.pivot_px) - Point(definitions[definition.parent].pivot_px);
                    groups.Add(name, group);
                }
                var parts = new Dictionary<string, Transform>();
                foreach (Part part in manifest.layers)
                {
                    var obj = new GameObject(part.name);
                    string parent = part.name == "07_main_gun_mantlet" ? "body" : part.parent;
                    obj.transform.SetParent(groups[parent], false);
                    obj.transform.localPosition = Point(part.pivot_canvas_px) - Point(definitions[parent].pivot_px);
                    var renderer = obj.AddComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Tank/" + part.file);
                    renderer.sortingOrder = part.z_order + 10;
                    parts.Add(part.name, obj.transform);
                }
                GunArtwork machineGun = null, cannon = null;
                SpriteClipPlayer[] smoke = null;
                foreach (Clip clip in vfx.clips)
                {
                    Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(ArtPath + "VFX/" + clip.sheet).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
                    if (frames.Length != 8) throw new Exception("Expected eight frames for " + clip.name);
                    if (clip.name == "exhaust_smoke")
                    {
                        smoke = new SpriteClipPlayer[3];
                        var exhaust = parts["06_exhaust_pipe"];
                        Part exhaustPart = manifest.layers.First(p => p.name == "06_exhaust_pipe");
                        for (int i = 0; i < smoke.Length; i++)
                            smoke[i] = CreateClip("Exhaust puff " + (i + 1), exhaust, Point(clip.socket) - Point(exhaustPart.pivot_canvas_px), clip, frames, 5);
                        continue;
                    }
                    bool main = clip.name == "cannon_muzzle";
                    string barrelName = main ? "08_main_gun_barrel" : "36_roof_gun_barrel";
                    Part barrelPart = manifest.layers.First(p => p.name == barrelName);
                    var socket = new GameObject(main ? "Main Gun muzzle" : "MG-AA muzzle").transform;
                    socket.SetParent(parts[barrelName], false);
                    socket.localPosition = Point(clip.socket) - Point(barrelPart.pivot_canvas_px);
                    var flash = CreateClip(clip.name, socket, Vector2.zero, clip, frames, 80);
                    var gun = groups[main ? "cannon" : "roof_gun"].gameObject.AddComponent<GunArtwork>();
                    gun.Configure(parts[barrelName], socket, flash, -clip.rotation_cw_deg, (main ? 12 : 4) / PixelsPerUnit);
                    if (main) cannon = gun; else machineGun = gun;
                }
                root.AddComponent<TankArtwork>().Configure(machineGun, cannon, groups["body"], smoke);
                Directory.CreateDirectory("Assets/MegaTanks/Prefabs");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            // Update just the art reference; retain the user's scene and gameplay settings.
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<BattleGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("tankArtworkPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TankArtwork>(PrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_ART_OK: 39 tank layers, custom pivots, dual muzzle sockets, three eight-frame VFX clips and prefab linked to scene.");
        }

        [MenuItem("MegaTanks/Fix Cannon Mount")]
        public static void FixCannonMount()
        {
            // Migrate the existing prefab without regenerating sprites or replacing custom art settings.
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform cannon = root.GetComponent<TankArtwork>().MainGun.transform;
                Transform mantlet = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "07_main_gun_mantlet");
                Transform barrel = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "08_main_gun_barrel");
                mantlet.SetParent(cannon.parent, true);
                var children = cannon.Cast<Transform>().ToArray();
                var positions = children.Select(child => child.position).ToArray();
                cannon.position = barrel.position;
                for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_CANNON_MOUNT_OK: mantlet fixed to body; cannon aiming pivot moved to barrel base.");
        }

        static void ConfigureTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.isReadable = false;
        }

        static SpriteClipPlayer CreateClip(string name, Transform parent, Vector2 position, Clip clip, Sprite[] frames, int order)
        {
            var effect = new GameObject(name).AddComponent<SpriteClipPlayer>();
            effect.transform.SetParent(parent, false);
            effect.transform.localPosition = position;
            effect.transform.localRotation = Quaternion.Euler(0, 0, -clip.rotation_cw_deg);
            effect.transform.localScale = Vector3.one * clip.preview_scale;
            effect.Configure(frames, clip.duration_ms / 1000, order);
            return effect;
        }
    }
}
