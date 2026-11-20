using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatTanks.VfxKit.Editor
{
    public static class NativeWeaponVfxValidation
    {
        [MenuItem("CatTanks/Combat/Open Native Weapon Preview")]
        public static void Open()
        {
            if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/WeaponEffectsPreview.unity");
        }

        [MenuItem("CatTanks/Combat/Validate Web Reference VFX")]
        public static void ValidateWeb()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var camera = new GameObject("Web VFX test camera", typeof(Camera)).GetComponent<Camera>();
            camera.enabled = false;
            var view = new BattleView(camera, useNativeWeaponVfx: true, useWebWeaponVfx: true);
            var seed = UnityEngine.Random.state;
            try
            {
                if (!view.UsesWebWeaponVfx || view.UsesNativeWeaponVfx)
                    throw new InvalidOperationException("The reference shader or original projectile textures failed to load.");
                var db = Catalog.Load();
                var battle = new Battle(db, Profile.New(db));
                battle.units.Clear();
                battle.waves = new List<Wave> { new Wave { count = 1, bursts = new List<Burst>() } };
                view.Render(battle);
                var target = battle.Spawn("bug-0", 1100);
                target.stun = float.PositiveInfinity;
                var source = new Actor { role = "tower", position = new Vector2(460, 300), weapon = db.Weapon("turret-4") };
                battle.Fire(source, target);
                if (!battle.effects.Any(e => e.kind == "flash") || !battle.effects.Any(e => e.kind == "beam"))
                    throw new InvalidOperationException("Ray shots must produce both their Web muzzle and beam.");
                battle.MakeBullet(new Vector2(690, 520), -.25f, db.Weapon("turret-2"), null);
                battle.Effect("impact", new Vector2(1160, 260), Vector2.zero, "#ffad37", 90, .5f);
                view.Render(battle);

                Mesh MeshFor(string name) => view.root.transform.Find(name).GetComponent<MeshFilter>().sharedMesh;
                var smoke = MeshFor("Web VFX smoke");
                var particles = MeshFor("Web VFX particles");
                var bursts = MeshFor("Web VFX flashes and rings");
                var rocket = view.root.transform.Find("Web rocket");
                if (!rocket || !Mathf.Approximately(rocket.localScale.x, 62) || smoke.vertexCount != 16
                    || particles.vertexCount < 58 * 4 || bursts.vertexCount < 6 * 4 || view.combatVfx.ActiveCount != 0)
                    throw new InvalidOperationException("Expected a double-size rocket, four smoke layers and a single Web renderer per effect.");
                var before = particles.vertices;
                view.Render(battle);
                if (!before.SequenceEqual(particles.vertices)) throw new InvalidOperationException("Paused Web particles changed.");
                battle.Step(.033f);
                view.Render(battle);
                if (before.SequenceEqual(particles.vertices)) throw new InvalidOperationException("Web particles did not resume.");
                for (int i = 0; i < 150; i++)
                    battle.Effect("impact", new Vector2(1160, 260), Vector2.zero, "#ffad37", 90, .5f);
                view.Render(battle);
                if (particles.vertexCount != WebCombatVfx.ParticleLimit * 4)
                    throw new InvalidOperationException("Web particles did not honor the shared 2,400-particle cap.");
                Debug.Log("CATTANKS_WEB_REFERENCE_VFX_PASSED");
            }
            finally
            {
                UnityEngine.Random.state = seed;
                view.Dispose();
                Object.Destroy(camera.gameObject);
            }
        }

        [MenuItem("CatTanks/Combat/Validate Native Weapon VFX")]
        public static void Validate()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var test = new GameObject("Native weapon VFX validation");
            var random = UnityEngine.Random.state;
            try
            {
                var native = NativeWeaponVfx.Create(test.transform);
                if (native == null) throw new InvalidOperationException("Native Weapon VFX resources or shader are unavailable.");
                using (native)
                {
                    var db = Catalog.Load();
                    var battle = new Battle(db, Profile.New(db));
                    battle.units.Clear();
                    battle.waves = new List<Wave> { new Wave { count = 1, bursts = new List<Burst>() } };
                    var target = battle.Spawn("bug-0", 950);
                    target.stun = float.PositiveInfinity;
                    for (int round = 0; round < 2; round++)
                    {
                        foreach (string id in new[] { "turret-3", "turret-0", "turret-2" })
                        {
                            var weapon = db.Weapon(id);
                            battle.MakeBullet(new Vector2(400, 300), 0, weapon, target);
                            battle.Effect("impact", new Vector2(780, 290), new Vector2(750, 285), weapon.color, 62, .58f, weaponKind: weapon.kind);
                        }
                        Render();
                        battle.Step(.033f);
                        Render();
                        var lines = test.GetComponentsInChildren<LineRenderer>();
                        var emitters = test.GetComponentsInChildren<ParticleSystem>();
                        if (lines.Length != 3 || lines.Any(line => line.positionCount < 2) || !emitters.Any(p => p.particleCount > 0))
                            throw new InvalidOperationException("Projectile trails or impact particles failed to render.");
                        var counts = emitters.Select(p => p.particleCount).ToArray();
                        var times = emitters.Select(p => p.time).ToArray();
                        int points = lines.Sum(line => line.positionCount);
                        Render();
                        if (!counts.SequenceEqual(emitters.Select(p => p.particleCount))
                            || !times.SequenceEqual(emitters.Select(p => p.time)) || points != lines.Sum(line => line.positionCount))
                            throw new InvalidOperationException("Pause changed particle or trail state.");
                        battle.bullets.Clear();
                        battle.effects.Clear();
                        Render();
                        if (native.CreatedShots != 3 || native.CreatedBursts != 3 || test.GetComponentsInChildren<ParticleSystem>().Length != 0)
                            throw new InvalidOperationException("Native VFX pool failed to despawn and reuse.");
                    }

                    void Render()
                    {
                        native.Begin(battle);
                        foreach (var shot in battle.bullets) native.Draw(shot);
                        foreach (var effect in battle.effects) native.Draw(effect);
                    }
                }
                Debug.Log("CATTANKS_NATIVE_WEAPON_VFX_PASSED");
            }
            finally
            {
                UnityEngine.Random.state = random;
                Object.Destroy(test);
            }
        }
    }
}
