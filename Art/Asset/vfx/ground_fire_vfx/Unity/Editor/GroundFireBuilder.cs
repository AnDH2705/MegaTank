using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MegaTankVFX.GroundFire.Editor
{
    // Import the entire ground_fire_vfx folder under Assets, then run this menu.
    // Only assets inside Unity/Generated are rebuilt; scene/camera/volumes are untouched.
    public static class GroundFireBuilder
    {
        private const string Suffix = "/Unity/Editor/GroundFireBuilder.cs";
        private const float Width = 7.36f, Height = 2.56f;

        [MenuItem("MegaTank VFX/Build Ground Fire")]
        public static void Build()
        {
            string root = null;
            foreach (var guid in AssetDatabase.FindAssets("GroundFireBuilder t:MonoScript"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(Suffix, StringComparison.Ordinal)) { root = p.Substring(0, p.Length - Suffix.Length); break; }
            }
            if (root == null) throw new InvalidOperationException("Keep the ground_fire_vfx folder structure intact under Assets.");
            string generated = root + "/Unity/Generated";
            Directory.CreateDirectory(generated);
            AssetDatabase.Refresh();
            foreach (string name in new[] { "fire_strip_8f.png", "glow.png", "spark.png" })
            {
                var importer = AssetImporter.GetAtPath(root + "/assets/" + name) as TextureImporter;
                if (importer == null) throw new FileNotFoundException(name);
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var shader = Shader.Find("MegaTankVFX/GroundFireParticles");
            if (!shader) throw new InvalidOperationException("GroundFireParticles shader has not imported yet.");
            var coreMat = Material(root, generated, shader, "FireCore", "fire_strip_8f.png", false, Color.white);
            var glowMat = Material(root, generated, shader, "WarmGlow", "glow.png", true, new Color(2.2f, 1.2f, .28f, .3f));
            var sparkMat = Material(root, generated, shader, "Embers", "spark.png", true, new Color(2.1f, 1.5f, .55f, .85f));
            var go = new GameObject("GroundFire");
            try
            {
                var core = Create(go, "FlameFlipbook", coreMat, 20);
                core.transform.localPosition = new Vector3(0, Height * .375f, 0); // Floor pivot: y=224/256 from top.
                var main = core.main;
                main.duration = 8f / 12f; main.startLifetime = 8f / 12f;
                main.startSize3D = true; main.startSizeX = Width; main.startSizeY = Height; main.startSizeZ = 1;
                main.maxParticles = 2;
                var emission = core.emission; emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)1) });
                var sheet = core.textureSheetAnimation;
                sheet.enabled = true; sheet.numTilesX = 2; sheet.numTilesY = 4;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1));
                sheet.cycleCount = 1;

                var glow = Create(go, "WarmGroundGlow", glowMat, 19);
                glow.transform.localPosition = new Vector3(0, .16f, 0);
                main = glow.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.1f);
                main.startSize3D = true; main.startSizeX = new ParticleSystem.MinMaxCurve(2.3f, 3.1f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(1.0f, 1.35f); main.startSizeZ = 1;
                main.maxParticles = 12;
                emission = glow.emission; emission.rateOverTime = 7;
                Area(glow, Width * .76f, .03f);
                Fade(glow, .8f);

                var embers = Create(go, "RisingEmbers", sparkMat, 21);
                main = embers.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(.045f, .11f);
                main.startRotation = new ParticleSystem.MinMaxCurve(-.4f, .4f);
                main.maxParticles = 40;
                emission = embers.emission; emission.rateOverTime = 19;
                Area(embers, Width * .88f, .12f);
                var velocity = embers.velocityOverLifetime; velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = new ParticleSystem.MinMaxCurve(-.22f, .22f);
                velocity.y = new ParticleSystem.MinMaxCurve(.85f, 1.85f);
                velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
                Fade(embers, 1);
                var size = embers.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .25f));

                var glints = Create(go, "TinyGlints", sparkMat, 22);
                glints.transform.localPosition = new Vector3(0, .75f, 0);
                main = glints.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.12f, .24f);
                main.startSize = new ParticleSystem.MinMaxCurve(.09f, .17f); main.maxParticles = 8;
                emission = glints.emission; emission.rateOverTime = 3;
                Area(glints, Width * .85f, 1.0f); Fade(glints, 1);

                PrefabUtility.SaveAsPrefabAsset(go, generated + "/GroundFire.prefab");
                AssetDatabase.SaveAssets();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(generated + "/GroundFire.prefab");
                Debug.Log("GroundFire ready: alpha flipbook + additive glow + rising embers + glints. Floor pivot is at the prefab origin.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static Material Material(string root, string generated, Shader shader, string name, string texture, bool additive, Color tint)
        {
            string p = generated + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, p); }
            mat.shader = shader;
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "/assets/" + texture);
            mat.SetColor("_Tint", tint);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            EditorUtility.SetDirty(mat); return mat;
        }
        private static ParticleSystem Create(GameObject parent, string name, Material material, int order)
        {
            var child = new GameObject(name); child.transform.SetParent(parent.transform, false);
            var ps = child.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.duration = 2;
            main.startSpeed = 0; main.startColor = Color.white; main.gravityModifier = 0;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape; shape.enabled = false;
            var emission = ps.emission; emission.enabled = true;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = order; renderer.maxParticleSize = 10;
            return ps;
        }
        private static void Area(ParticleSystem ps, float width, float height)
        {
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, height, .001f);
        }
        private static void Fade(ParticleSystem ps, float peak)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1, .65f, .3f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(peak, .18f), new GradientAlphaKey(peak * .65f, .55f), new GradientAlphaKey(0, 1) });
            var color = ps.colorOverLifetime; color.enabled = true; color.color = gradient;
        }
    }
}
