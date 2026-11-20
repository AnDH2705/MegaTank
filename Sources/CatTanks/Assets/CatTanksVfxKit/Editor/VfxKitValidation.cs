using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatTanks.VfxKit.Editor
{
    public static class VfxKitValidation
    {
        [Serializable]
        sealed class Manifest
        {
            public VfxRecipe[] recipes;
        }

        [MenuItem("CatTanks/Combat/Validate VFX Atlas Kit")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var root = new GameObject("VFX Atlas Validation");
            var camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 450;
            camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(800, 450, -1000);
            var testSpace = new GameObject("Test space").transform;
            testSpace.SetParent(root.transform, false);
            testSpace.position = Vector3.zero;
            var random = UnityEngine.Random.state;
            int passed = 0;
            void Check(bool ok, string message)
            {
                if (!ok) throw new InvalidOperationException(message);
                passed++;
            }

            try
            {
                var manifest = JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("CatTanksVfxKit/effects").text);
                var bounds = new Rect(0, 0, 1600, 900);
                using (var surface = VfxAtlasSurface.Create(testSpace))
                {
                    foreach (var recipe in manifest.recipes)
                    {
                        surface.Begin(bounds, VfxQuality.High);
                        Check(surface.Draw(recipe.id, new Vector2(600, 350), new Vector2(800, 375), .4f, 1, 45),
                            "Unknown recipe " + recipe.id);
                        surface.End();
                        Check(surface.DrawnQuads > 0, "Empty recipe " + recipe.id);
                    }

                    VfxQuality[] qualities = { VfxQuality.Low, VfxQuality.Medium, VfxQuality.High };
                    int[] limits = { 240, 400, 640 };
                    for (int tier = 0; tier < qualities.Length; tier++)
                    {
                        surface.Begin(bounds, qualities[tier]);
                        for (int i = 0; i < 350; i++)
                            surface.Draw("rocketblast", new Vector2(850, 400), Vector2.right, .4f, 1, i);
                        int saturated = surface.DrawnQuads;
                        surface.Draw("target-lock", new Vector2(850, 400), Vector2.right, .4f, 1, 0);
                        surface.End();
                        Check(surface.DroppedQuads > 0 && surface.DrawnQuads <= limits[tier], "Tier cap " + qualities[tier]);
                        Check(surface.DrawnQuads == saturated + 1, "Target lock has priority " + qualities[tier]);
                    }

                    surface.Begin(bounds, VfxQuality.High);
                    surface.Draw("blast", new Vector2(-2000, -2000), Vector2.right, .4f, 1, 45);
                    surface.End();
                    Check(surface.DrawnQuads == 0, "Cull an offscreen explosion");
                }

                var db = Catalog.Load();
                var battle = new Battle(db, Profile.New(db));
                battle.units.Clear();
                battle.waves = new List<Wave> { new Wave { count = 1, bursts = new List<Burst>() } };
                var target = battle.Spawn("bug-0", 950);
                target.stun = float.PositiveInfinity;
                var rocket = db.Weapon("turret-2");
                battle.MakeBullet(new Vector2(460, 230), 0, rocket, target);
                battle.Effect("flash", new Vector2(440, 230), new Vector2(380, 230), "#fff2c2", 42, .2f, visual: BattleVfxTags.Muzzle(rocket.kind));
                var bridgeSpace = new GameObject("Bridge space").transform;
                bridgeSpace.SetParent(testSpace, false);
                using (var bridge = new BattleVfxBridge(bridgeSpace))
                {
                    Check(bridge.Handles(battle.bullets[0]) && bridge.Handles(battle.effects[0]), "Rocket and tagged muzzle route");
                    bridge.Render(battle, camera);
                    var filter = bridgeSpace.GetComponentInChildren<MeshFilter>();
                    var first = new List<Vector3>();
                    var next = new List<Vector3>();
                    filter.sharedMesh.GetVertices(first);
                    bridge.Render(battle, camera);
                    filter.sharedMesh.GetVertices(next);
                    Check(first.Count > 0 && first.Count == next.Count, "Pause preserves vertex count");
                    for (int i = 0; i < first.Count; i++)
                        if (first[i] != next[i]) throw new InvalidOperationException("Pause changed VFX geometry");
                    passed++;
                    battle.Step(.033f);
                    bridge.Render(battle, camera);
                    filter.sharedMesh.GetVertices(next);
                    bool changed = first.Count != next.Count;
                    for (int i = 0; !changed && i < first.Count; i++) changed = first[i] != next[i];
                    Check(changed, "Resume advances VFX");
                    battle.bullets.Clear();
                    battle.effects.Clear();
                    for (int i = 0; i < 26; i++) battle.Step(1f / 60);
                    bridge.Render(battle, camera);
                    Check(bridge.DrawnQuads == 1, "Transient meshes expire; the stunned test target still has its status glyph");
                }
                Debug.Log("CATTANKS_VFX_KIT_PASSED " + passed);
            }
            finally
            {
                UnityEngine.Random.state = random;
                Object.Destroy(root);
            }
        }
    }
}
