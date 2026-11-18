using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

namespace MegaTanks.Editor
{
    public static class TrackAnimationImporter
    {
        public const string DataPath = "Assets/MegaTanks/Data/Tracks/TankTracks.asset";
        const string ArtRoot = "Assets/MegaTanks/Art/Tracks/";
        const float Ppu = 180;
        // Match the grey backing at both ends, retaining the accepted vertical wheel fit.
        public static readonly Vector2 TrackFitScale = new Vector2(1.03f, .89f);
        [MenuItem("MegaTanks/Import Track Animation")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var source = TrackSpineReader.Read(ArtRoot + "ChainTrackSource.bytes");
            var belt = source.attachments.Single(attachment => attachment.path == "02_track_belt");
            var path = source.attachments.Single(attachment => attachment.kind == 4);
            var pin = source.attachments.First(attachment => attachment.path == "chain pin");
            var constraint = source.constraints.Single();
            var running = source.animations.Single(animation => animation.name == "running");
            var timeline = running.timelines.Single();
            if (!path.closed || !path.constantSpeed || constraint.positionMode != 1 || constraint.spacingMode != 0 || constraint.rotateMode != 0
                || timeline.type != 0 || timeline.frames.Length != 2 || timeline.frames[0].values[0] != 0 || timeline.frames[1].values[0] != 1
                || timeline.frames[0].curve.Length != 0)
                throw new InvalidOperationException("Unexpected track path or running animation configuration.");
            ConfigureSprite(ArtRoot + "TrackBelt.png", new Vector2(400f / 795, 1 - 109f / 211));
            ConfigureSprite(ArtRoot + "ChainPin.png", Vector2.one * .5f);
            var data = AssetDatabase.LoadAssetAtPath<TrackAnimationData>(DataPath);
            if (data == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DataPath)); AssetDatabase.Refresh();
                data = ScriptableObject.CreateInstance<TrackAnimationData>(); AssetDatabase.CreateAsset(data, DataPath);
            }
            data.belt = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "TrackBelt.png");
            data.chainPin = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "ChainPin.png");
            data.sourceVersion = source.version; data.sourceAnimation = running.name; data.sourceDuration = timeline.frames[1].time;
            var matrices = new Matrix4x4[source.bones.Length];
            for (int i = 0; i < matrices.Length; i++)
            {
                var bone = source.bones[i]; var local = Matrix4x4.identity;
                float xAngle = (bone.rotation + bone.shearX) * Mathf.Deg2Rad, yAngle = (bone.rotation + bone.shearY + 90) * Mathf.Deg2Rad;
                local.SetColumn(0, new Vector4(Mathf.Cos(xAngle) * bone.scaleX, Mathf.Sin(xAngle) * bone.scaleX, 0, 0));
                local.SetColumn(1, new Vector4(Mathf.Cos(yAngle) * bone.scaleY, Mathf.Sin(yAngle) * bone.scaleY, 0, 0));
                local.SetColumn(3, new Vector4(bone.x, bone.y, 0, 1)); matrices[i] = bone.parent < 0 ? local : matrices[bone.parent] * local;
            }
            Matrix4x4 beltSpace = matrices[source.slots[belt.slot].bone]
                * Matrix4x4.TRS(new Vector3(belt.x, belt.y), Quaternion.Euler(0, 0, belt.rotation), new Vector3(belt.scaleX, belt.scaleY, 1));
            Vector2[] vertices = path.vertices.Select(vertex =>
            {
                Vector3 point = Vector3.zero;
                if (vertex.weights == null) point = matrices[source.slots[path.slot].bone].MultiplyPoint3x4(new Vector3(vertex.x, vertex.y));
                else foreach (var weight in vertex.weights) point += matrices[weight.bone].MultiplyPoint3x4(new Vector3(weight.x, weight.y)) * weight.weight;
                return (Vector2)(beltSpace.inverse.MultiplyPoint3x4(point) / Ppu) + (Vector2)data.belt.bounds.center;
            }).ToArray();
            var samples = new List<Vector2>();
            for (int curve = 0; curve < vertices.Length / 3; curve++)
            {
                int start = 1 + curve * 3;
                Vector2 a = vertices[start % vertices.Length], b = vertices[(start + 1) % vertices.Length],
                    c = vertices[(start + 2) % vertices.Length], d = vertices[(start + 3) % vertices.Length];
                for (int step = 0; step < 96; step++)
                {
                    float t = step / 96f, s = 1 - t;
                    samples.Add(s * s * s * a + 3 * s * s * t * b + 3 * s * t * t * c + t * t * t * d);
                }
            }
            samples.Add(samples[0]); data.path = samples.ToArray();
            data.distances = new float[data.path.Length];
            for (int i = 1; i < data.path.Length; i++) data.distances[i] = data.distances[i - 1] + Vector2.Distance(data.path[i - 1], data.path[i]);
            data.linkOffsets = new float[constraint.bones.Length];
            for (int i = 1; i < data.linkOffsets.Length; i++)
            {
                int index = constraint.bones[i - 1];
                float scale = matrices[index].MultiplyVector(Vector3.right).magnitude;
                data.linkOffsets[i] = data.linkOffsets[i - 1] + (source.bones[index].length + constraint.spacing) * scale / Ppu;
            }
            data.linkOffset = new Vector2(pin.x, pin.y) / Ppu; data.linkRotation = pin.rotation + constraint.rotation;
            data.linkScale = new Vector2(pin.scaleX * pin.width / data.chainPin.rect.width, pin.scaleY * pin.height / data.chainPin.rect.height);
            ImportSpineAssets(source, data);
            EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            var root = PrefabUtility.LoadPrefabContents(TankArtImporter.PrefabPath);
            try { AttachTo(root); PrefabUtility.SaveAsPrefabAsset(root, TankArtImporter.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_TRACKS_IMPORTED: Spine 4.2 SkeletonAnimation, 38 bones, 36 chain pins, idle/running loops, original atlas and source alignment.");
        }
        static void ImportSpineAssets(TrackSpineReader.Data source, TrackAnimationData data)
        {
            const string folder = "Assets/MegaTanks/Art/Tracks/Spine42/";
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "Tracks.json", TrackSpine42Converter.Convert(source));
            AssetDatabase.Refresh();
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(folder + "chain track.png");
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.alphaIsTransparency = true; textureImporter.mipmapEnabled = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None; textureImporter.maxTextureSize = 1024;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "chain track_Material.mat");
            if (material == null) throw new InvalidOperationException("Spine's atlas importer did not create the track material.");
            material.shader = Shader.Find("Spine/Skeleton");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "chain track.png");
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT"); material.SetFloat("_StraightAlphaInput", 1);
            EditorUtility.SetDirty(material);
            var skeleton = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(folder + "Tracks_SkeletonData.asset");
            if (skeleton == null) throw new InvalidOperationException("Spine's importer did not create the track SkeletonDataAsset.");
            skeleton.scale = 1 / Ppu;
            skeleton.Clear(); EditorUtility.SetDirty(skeleton);
            if (skeleton.GetSkeletonData(false) == null) throw new InvalidOperationException("Converted tracks failed to load in Spine 4.2.");
            data.skeletonData = skeleton;
        }
        static void ConfigureSprite(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = Ppu;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = 9; settings.spritePivot = pivot; settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        public static void AttachTo(GameObject tank)
        {
            var data = AssetDatabase.LoadAssetAtPath<TrackAnimationData>(DataPath); if (data == null) return;
            var belt = tank.GetComponentsInChildren<SpriteRenderer>().Single(renderer => renderer.name == "02_track_belt");
            belt.sprite = data.belt; belt.enabled = false;
            var old = belt.transform.Find("Animated chain links"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            old = belt.transform.Find("Spine tracks"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var visual = SkeletonAnimation.NewSkeletonAnimationGameObject(data.skeletonData);
            visual.name = "Spine tracks"; visual.transform.SetParent(belt.transform, false);
            var source = TrackSpineReader.Read(ArtRoot + "ChainTrackSource.bytes");
            var region = source.attachments.Single(a => a.path == "02_track_belt");
            visual.transform.localScale = new Vector3(TrackFitScale.x, TrackFitScale.y, 1);
            visual.transform.localPosition = (Vector2)data.belt.bounds.center - Vector2.Scale(new Vector2(region.x, region.y) / Ppu, TrackFitScale);
            visual.GetComponent<MeshRenderer>().sortingOrder = belt.sortingOrder;
            var animator = visual.gameObject.AddComponent<TankTrackAnimator>(); animator.Configure(data, visual);
            tank.GetComponent<TankArtwork>().BindTracks(animator);
        }
    }
}
