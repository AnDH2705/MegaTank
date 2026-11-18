using System;
using System.IO;
using MegaTankVFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MegaTankVFX.Editor
{
    public static class MegaTankVfxBuilder
    {
        private const string Root = "Assets/MegaTankVFX";
        private static readonly string[] Names = { "ExplosionSmall", "ExplosionBig", "ClusterBurst", "HitSpark", "MuzzleFlash", "SmokePuff", "DustPuff", "FireBolt" };

        [MenuItem("MegaTank VFX/1. Build prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Generated/Materials");
            Directory.CreateDirectory(Root + "/Generated/Prefabs");
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Root + "/Art", "*.png", SearchOption.AllDirectories))
            {
                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = assetPath.Contains("/Frames/") ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }

            foreach (string name in Names)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/Prefabs/" + name + ".prefab")) continue;
                var root = new GameObject(name);
                var release = root.AddComponent<VfxAutoRelease>();
                release.Lifetime = 1.5f;
                if (name == "ExplosionSmall") ExplosionLayers(root.transform, Vector3.zero, 0f, 2.6f, 18);
                if (name == "ExplosionBig") ExplosionLayers(root.transform, Vector3.zero, 0f, 3.8f, 26);
                if (name == "ClusterBurst")
                {
                    ExplosionLayers(root.transform, new Vector3(-1.7f, -0.3f), 0f, 2.8f, 12);
                    ExplosionLayers(root.transform, new Vector3(-0.5f, 0.9f), 0.08f, 3.1f, 12);
                    ExplosionLayers(root.transform, new Vector3(1.25f, 0.25f), 0.17f, 2.9f, 12);
                    ExplosionLayers(root.transform, new Vector3(0.5f, 1.7f), 0.27f, 2.7f, 12);
                }
                if (name == "HitSpark") { Particle(root.transform, "Impact", "Hit", 4, 1, 0.14f, 0.9f, 0f, 1, true, 112); Sparks(root.transform, Vector3.zero, 0f, 8, 1.4f); release.Lifetime = 0.5f; }
                if (name == "MuzzleFlash") { Particle(root.transform, "Flash", "Muzzle", 2, 1, 0.065f, 1.1f, 0f, 1, true, 112); release.Lifetime = 0.15f; }
                if (name == "SmokePuff") { Smoke(root.transform, Vector3.zero, 0f, 1.9f); release.Lifetime = 1f; }
                if (name == "DustPuff")
                {
                    var dust = Particle(root.transform, "Dust", "Dust", 1, 1, 0.5f, 1.4f, 0f, 1, false, 98);
                    Grow(dust, 0.5f, 1.5f); Fade(dust, 0.8f, 0.25f);
                    var velocity = dust.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World; velocity.y = 0.25f;
                    release.Lifetime = 0.6f;
                }
                if (name == "FireBolt") { FireBolt(root); release.Lifetime = 1f; }
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Generated/Prefabs/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MegaTank VFX: 8 particle prefabs ready; existing prefab edits retained. Use MegaTank VFX > 2. Open demo scene.");
        }

        private static Material MaterialFor(string atlas, bool additive)
        {
            string file = Root + "/Generated/Materials/" + atlas + (additive ? "_Add.mat" : "_Alpha.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(file);
            if (!material)
            {
                var shader = Shader.Find("MegaTankVFX/ComicParticles");
                if (!shader) throw new InvalidOperationException("Import MegaTankVFX/Shaders/ComicParticles.shader first.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, file);
            }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Atlases/" + atlas + ".png");
            if (!material.mainTexture) throw new InvalidOperationException("Missing atlas: " + atlas);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ParticleSystem Particle(Transform parent, string name, string atlas, int columns, int rows,
            float life, float size, float delay, int count, bool additive, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.duration = Mathf.Max(life, 0.05f); main.startLifetime = life;
            main.startSpeed = 0f; main.startSize = size; main.startDelay = delay; main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 96; main.playOnAwake = true;
            var shape = ps.shape; shape.enabled = false;
            var emission = ps.emission; emission.rateOverTime = 0f;
            emission.SetBursts(count > 0 ? new[] { new ParticleSystem.Burst(0f, (short)count) } : new ParticleSystem.Burst[0]);
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = columns * rows > 1;
            if (sheet.enabled)
            {
                sheet.mode = ParticleSystemAnimationMode.Grid;
                sheet.numTilesX = columns; sheet.numTilesY = rows;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                sheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
                sheet.frameOverTime = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                sheet.cycleCount = 1;
            }
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MaterialFor(atlas, additive);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return ps;
        }

        private static void Fade(ParticleSystem ps, float alpha, float hold)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, hold), new GradientAlphaKey(0f, 1f) });
            var color = ps.colorOverLifetime; color.enabled = true; color.color = gradient;
        }
        private static void Grow(ParticleSystem ps, float from, float to)
        {
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
        }
        private static void Smoke(Transform parent, Vector3 at, float delay, float size)
        {
            var ps = Particle(parent, "Smoke", "Smoke", 4, 2, 0.85f, size, delay, 1, false, 100);
            ps.transform.localPosition = at;
            Fade(ps, 0.9f, 0.6f); Grow(ps, 0.75f, 1.15f);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = 0.45f;
        }
        private static void Sparks(Transform parent, Vector3 at, float delay, int count, float speed)
        {
            var ps = Particle(parent, "Sparks", "Spark", 1, 1, 0.42f, 0.32f, delay, count, true, 111);
            ps.transform.localPosition = at;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-speed, speed);
            velocity.y = new ParticleSystem.MinMaxCurve(-speed * 0.7f, speed);
            var force = ps.forceOverLifetime; force.enabled = true; force.space = ParticleSystemSimulationSpace.World; force.y = -3f;
            Fade(ps, 1f, 0.25f); Grow(ps, 1f, 0.2f);
        }
        private static void ExplosionLayers(Transform parent, Vector3 at, float delay, float size, int sparks)
        {
            var core = Particle(parent, "ExplosionCore", "Explosion", 4, 3, 0.48f, size, delay, 1, false, 106);
            core.transform.localPosition = at; Fade(core, 1f, 0.8f);
            var glow = Particle(parent, "HotGlow", "Glow", 1, 1, 0.23f, size * 1.4f, delay, 1, true, 102);
            glow.transform.localPosition = at; Fade(glow, 0.65f, 0.05f);
            var ring = Particle(parent, "Shockwave", "Shockwave", 3, 1, 0.26f, size * 1.22f, delay + 0.025f, 1, true, 108);
            ring.transform.localPosition = at; Fade(ring, 0.9f, 0.2f);
            Smoke(parent, at + Vector3.up * size * 0.07f, delay + 0.12f, size * 0.95f);
            Sparks(parent, at, delay + 0.03f, sparks, size * 1.25f);
        }
        private static void FireBolt(GameObject root)
        {
            var head = Particle(root.transform, "FireHead", "FireBoltHead", 3, 1, 0.10f, 1.15f, 0f, 0, true, 114);
            var main = head.main; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.loop = true; main.duration = 0.1f;
            var emission = head.emission; emission.rateOverTime = 10f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            var hotGlow = Particle(root.transform, "HeadGlow", "Glow", 1, 1, 0.1f, 1.8f, 0f, 0, true, 110);
            main = hotGlow.main; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.loop = true; main.duration = 0.1f;
            emission = hotGlow.emission; emission.rateOverTime = 10f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            Fade(hotGlow, 0.35f, 0.1f);
            var smoke = Particle(root.transform, "SmokeTrail", "Smoke", 4, 2, 0.55f, 0.65f, 0f, 0, false, 101);
            main = smoke.main; main.loop = true; main.duration = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            emission = smoke.emission; emission.rateOverDistance = 3.5f;
            Fade(smoke, 0.65f, 0.4f); Grow(smoke, 0.5f, 1.1f);
            var velocity = smoke.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World; velocity.y = 0.35f;
            var sparks = Particle(root.transform, "TrailingSparks", "Spark", 1, 1, 0.25f, 0.16f, 0f, 0, true, 113);
            main = sparks.main; main.loop = true; main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            emission = sparks.emission; emission.rateOverDistance = 4f;
            velocity = sparks.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-2f, -0.5f); velocity.y = new ParticleSystem.MinMaxCurve(-1f, 1f);
            Fade(sparks, 1f, 0.1f);
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = MaterialFor("BeamTrail", true);
            trail.time = 0.13f; trail.minVertexDistance = 0.025f;
            trail.startWidth = 0.25f; trail.endWidth = 0f;
            trail.numCornerVertices = 2; trail.numCapVertices = 1;
            trail.textureMode = LineTextureMode.Stretch; trail.alignment = LineAlignment.View;
            trail.sortingOrder = 112; trail.shadowCastingMode = ShadowCastingMode.Off;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            var motion = root.AddComponent<FireBoltDemoMotion>();
            motion.ImpactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/Prefabs/ExplosionSmall.prefab");
        }

        [MenuItem("MegaTank VFX/2. Open demo scene")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            string demoPath = Root + "/Generated/Demo/MegaTankVFXDemo.unity";
            if (File.Exists(demoPath)) { EditorSceneManager.OpenScene(demoPath, OpenSceneMode.Single); return; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("VFX Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true; camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.12f, 0.10f);
            var demo = new GameObject("VFX Gallery").AddComponent<VfxDemoController>();
            demo.Effects = new GameObject[Names.Length];
            for (int i = 0; i < Names.Length; i++) demo.Effects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/Prefabs/" + Names[i] + ".prefab");
            Directory.CreateDirectory(Root + "/Generated/Demo");
            EditorSceneManager.SaveScene(scene, demoPath);
            Selection.activeGameObject = demo.gameObject;
            Debug.Log("VFX demo ready. Press Play. MainBattle was not edited.");
        }
    }
}
