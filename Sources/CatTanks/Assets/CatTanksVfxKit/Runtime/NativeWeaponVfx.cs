using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CatTanks.VfxKit
{
    // Optional Shuriken and SpriteRenderer path for the three hero weapons.
    public sealed class NativeWeaponVfx : IDisposable
    {
        const string ResourcePath = "CatTanksVfxKit/";
        const int MaxShots = 48;
        const int MaxBursts = 24;
        const int MaxTrailPoints = 10;

        [Serializable] sealed class PoseSheet { public int width, height; public Pose[] poses; }
        [Serializable] sealed class Pose { public string key; public int x, y, w, h; }

        sealed class ShotView
        {
            public GameObject root;
            public SpriteRenderer body;
            public LineRenderer trail;
            public readonly Vector3[] points = new Vector3[MaxTrailPoints];
            public int count;
            public float lastAge;
        }

        sealed class BurstView
        {
            public GameObject root;
            public SpriteRenderer body, light, shockwave;
            public ParticleSystem sparks, smoke;
            public float age;
        }

        readonly Transform parent;
        readonly Dictionary<string, Sprite> poses = new Dictionary<string, Sprite>();
        readonly Dictionary<int, ShotView> shots = new Dictionary<int, ShotView>();
        readonly Dictionary<int, BurstView> bursts = new Dictionary<int, BurstView>();
        readonly Stack<ShotView> freeShots = new Stack<ShotView>();
        readonly Stack<BurstView> freeBursts = new Stack<BurstView>();
        readonly HashSet<int> active = new HashSet<int>();
        readonly List<int> retired = new List<int>();
        readonly List<Sprite> generated = new List<Sprite>();
        readonly Material alpha, additive, atlasAlpha, atlasAdditive, radial;
        readonly Sprite lightSprite, ring;
        readonly Sprite[] muzzle, dust, grains;
        readonly AnimationCurve growth = AnimationCurve.EaseInOut(0, .32f, .38f, 1);
        Battle battle;
        int madeShots, madeBursts;
        Vector2 cameraKick;

        public Vector2 CameraKick => cameraKick;
        public int CreatedShots => madeShots;
        public int CreatedBursts => madeBursts;

        NativeWeaponVfx(Transform parent, Shader shader, Texture2D sheet, PoseSheet data, Texture2D atlas)
        {
            this.parent = Child("Native weapon VFX pool", parent).transform;
            foreach (var pose in data.poses)
                poses.Add(pose.key, Sprite(sheet, new Rect(
                    pose.x, sheet.height - pose.y - pose.h, pose.w, pose.h)));

            alpha = Material(shader, Texture2D.whiteTexture, false);
            additive = Material(shader, Texture2D.whiteTexture, true);
            atlasAlpha = Material(shader, atlas, false);
            atlasAdditive = Material(shader, atlas, true);
            radial = Material(shader, Texture2D.whiteTexture, true);
            radial.SetFloat("_Radial", 1);
            lightSprite = Sprite(Texture2D.whiteTexture, new Rect(0, 0, 1, 1));
            muzzle = new Sprite[24];
            for (int i = 0; i < muzzle.Length; i++) muzzle[i] = AtlasSprite(atlas, i);
            ring = AtlasSprite(atlas, 218);
            dust = new Sprite[8];
            for (int i = 0; i < dust.Length; i++) dust[i] = AtlasSprite(atlas, 152 + i);
            grains = new[] { AtlasSprite(atlas, 200), AtlasSprite(atlas, 201), AtlasSprite(atlas, 202) };
        }

        public static NativeWeaponVfx Create(Transform parent)
        {
            var atlas = Resources.Load<Texture2D>(ResourcePath + "atlas");
            var sheet = Resources.Load<Texture2D>(ResourcePath + "CombatPoses");
            var json = Resources.Load<TextAsset>(ResourcePath + "CombatPoses");
            var shader = Resources.Load<Shader>(ResourcePath + "NativeCombatFx");
            if (!atlas || !sheet || !json || !shader || !shader.isSupported) return null;
            var data = JsonUtility.FromJson<PoseSheet>(json.text);
            if (data == null || data.poses == null || data.width != sheet.width || data.height != sheet.height) return null;
            return new NativeWeaponVfx(parent, shader, sheet, data, atlas);
        }

        static Material Material(Shader shader, Texture texture, bool glow)
        {
            var material = new Material(shader) { name = glow ? "Combat FX Additive" : "Combat FX Alpha", mainTexture = texture };
            material.SetInt("_DstBlend", (int)(glow ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            return material;
        }

        Sprite Sprite(Texture2D texture, Rect rect)
        {
            var sprite = UnityEngine.Sprite.Create(texture, rect, Vector2.one * .5f, rect.width, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            generated.Add(sprite);
            return sprite;
        }

        Sprite AtlasSprite(Texture2D atlas, int tile)
        {
            int size = atlas.width / 16;
            return Sprite(atlas, new Rect(tile % 16 * size, (15 - tile / 16) * size, size, size));
        }

        public void Warm()
        {
            while (madeShots < 12)
            {
                var shot = BuildShot();
                shot.root.SetActive(false);
                freeShots.Push(shot);
            }
            while (madeBursts < 8)
            {
                var burst = BuildBurst();
                burst.root.SetActive(false);
                freeBursts.Push(burst);
            }
        }

        public void Begin(Battle current)
        {
            cameraKick = Vector2.zero;
            active.Clear();
            if (battle == current)
                foreach (var shot in current.bullets) active.Add(shot.id);
            retired.Clear();
            foreach (int id in shots.Keys) if (!active.Contains(id)) retired.Add(id);
            foreach (int id in retired)
            {
                var view = shots[id];
                view.root.SetActive(false);
                shots.Remove(id);
                freeShots.Push(view);
            }
            active.Clear();
            if (battle == current)
                foreach (var effect in current.effects) active.Add(effect.id);
            retired.Clear();
            foreach (int id in bursts.Keys) if (!active.Contains(id)) retired.Add(id);
            foreach (int id in retired)
            {
                var view = bursts[id];
                view.sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                view.smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                view.root.SetActive(false);
                bursts.Remove(id);
                freeBursts.Push(view);
            }
            battle = current;
        }

        public bool Draw(Projectile shot)
        {
            var kind = shot.weapon.kind;
            if (shot.enemy || !Battle.HasWeaponEffects(kind)) return false;
            if (!shots.TryGetValue(shot.id, out var view))
            {
                if (freeShots.Count > 0) view = freeShots.Pop();
                else if (madeShots < MaxShots) view = BuildShot();
                else return false;
                view.count = 0;
                view.lastAge = -1;
                view.root.SetActive(true);
                shots.Add(shot.id, view);
            }

            float size = kind == "rapid" ? 68 : kind == "rocket" ? 108 : 112;
            size *= Mathf.Lerp(.75f, 1, Mathf.Clamp01(shot.age / .09f));
            var axis = new Vector2(Mathf.Cos(shot.angle), Mathf.Sin(shot.angle));
            view.body.sprite = poses[kind];
            var body = view.body.transform;
            body.localPosition = shot.position - axis * (size * .21f);
            body.localScale = new Vector3(size, size, 1);
            body.localRotation = Quaternion.Euler(0, 0, shot.angle * Mathf.Rad2Deg);

            if (shot.age != view.lastAge)
            {
                if (view.count == MaxTrailPoints)
                    Array.Copy(view.points, 1, view.points, 0, MaxTrailPoints - 1);
                else view.count++;
                view.points[view.count - 1] = shot.position;
                view.lastAge = shot.age;
            }
            var line = view.trail;
            line.positionCount = view.count;
            for (int i = 0; i < view.count; i++) line.SetPosition(i, view.points[i]);
            line.startWidth = .4f;
            line.endWidth = kind == "rapid" ? 4 : 9;
            line.startColor = Color.clear;
            line.endColor = kind == "rocket" ? new Color(1, .41f, .19f, .67f) : new Color(1, .71f, .28f, .72f);
            return true;
        }

        public bool Draw(BattleEffect effect)
        {
            if (!Battle.HasWeaponEffects(effect.weaponKind) || effect.maxLife <= 0) return false;
            bool flash = effect.kind == "flash";
            bool impact = effect.kind == "impact";
            if (!flash && !impact) return false;
            bool rapid = effect.weaponKind == "rapid";
            bool rocket = effect.weaponKind == "rocket";
            if (!bursts.TryGetValue(effect.id, out var view))
            {
                if (freeBursts.Count > 0) view = freeBursts.Pop();
                else if (madeBursts < MaxBursts) view = BuildBurst();
                else return false;
                view.age = -1;
                view.root.SetActive(true);
                Reseed(view.sparks, effect.id);
                Reseed(view.smoke, effect.id + 319);
                bursts.Add(effect.id, view);
            }

            float age = Mathf.Max(0, effect.maxLife - effect.life);
            float p = Mathf.Clamp01(age / effect.maxLife);
            float size = flash ? (rapid ? 52 : 112) : rapid ? 70 : rocket ? 175 : 205;
            view.root.transform.localPosition = effect.position;
            view.root.transform.localScale = new Vector3(size, size, 1);
            var axis = effect.position - effect.end;
            float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            view.body.transform.localRotation = Quaternion.Euler(0, 0, flash || rapid ? angle : angle * .19f);
            int muzzleFrame = (rapid ? 0 : rocket ? 16 : 8) + Mathf.Min(7, Mathf.FloorToInt(p * 8));
            view.body.sprite = flash ? muzzle[muzzleFrame] : poses[rapid ? "hit" : rocket ? "rocketblast" : "blast"];
            float expansion = growth.Evaluate(Mathf.Min(.38f, p));
            view.body.transform.localScale = new Vector3(expansion, expansion, 1);
            view.body.color = new Color(1, 1, 1, Mathf.Clamp01((1 - p) * (flash ? 1.5f : 2.3f)));
            view.light.color = new Color(1, rocket ? .42f : .62f, .26f, .63f * Mathf.Max(0, 1 - p * 3.2f));
            view.shockwave.enabled = impact && !rapid && age < .25f;
            if (view.shockwave.enabled)
            {
                float wave = Mathf.Clamp01(age / .25f);
                float spread = Mathf.Lerp(.30f, 1.55f, 1 - (1-wave)*(1-wave));
                view.shockwave.transform.localScale = new Vector3(spread, spread * .46f, 1);
                view.shockwave.color = new Color(1, rocket ? .44f : .71f, .30f, .65f * (1 - wave));
            }
            if (age != view.age)
            {
                Advance(view.sparks, age, view.age);
                if (!rapid || !flash)
                    Advance(view.smoke, age, view.age);
                view.age = age;
            }
            if (impact && !rapid && age < .16f)
            {
                var kick = new Vector2(Mathf.Sin(effect.id + age * 78), Mathf.Cos(effect.id - age * 92))
                    * ((rocket ? 4.2f : 5.2f) * (1 - age / .16f));
                if (kick.sqrMagnitude > cameraKick.sqrMagnitude) cameraKick = kick;
            }
            return true;
        }

        static void Reseed(ParticleSystem system, int id)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false;
            system.randomSeed = (uint)id + 1;
        }

        static void Advance(ParticleSystem system, float age, float previous)
        {
            bool reset = previous < 0 || age < previous;
            float elapsed = reset ? age + .007f : age - previous;
            system.Simulate(elapsed, true, reset, false);
            system.Pause(true);
        }

        ShotView BuildShot()
        {
            madeShots++;
            var root = Child("Native hero projectile", parent);
            var trail = root.AddComponent<LineRenderer>();
            trail.sharedMaterial = additive;
            trail.useWorldSpace = false;
            trail.sortingOrder = 33;
            trail.positionCount = 0;
            trail.numCornerVertices = 1;
            var body = Renderer("Projectile core", root.transform, alpha, 36);
            return new ShotView { root = root, body = body, trail = trail };
        }

        BurstView BuildBurst()
        {
            madeBursts++;
            var root = Child("Native hero burst", parent);
            var body = Renderer("Painted flipbook core", root.transform, alpha, 46);
            var light = Renderer("Additive local light", root.transform, radial, 45);
            light.sprite = lightSprite;
            light.transform.localScale = Vector3.one * 1.65f;
            var shockwave = Renderer("Ground shockwave", root.transform, additive, 45);
            shockwave.sprite = ring;
            var sparks = Particles("Sparks and trails", root.transform, grains, atlasAdditive, false);
            var smoke = Particles("Smoke flipbook", root.transform, dust, atlasAlpha, true);
            var cinders = Child("Spark death cinders", sparks.transform).AddComponent<ParticleSystem>();
            cinders.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var cinderMain = cinders.main;
            cinderMain.playOnAwake = false;
            cinderMain.loop = false;
            cinderMain.startLifetime = .085f;
            cinderMain.startSpeed = .22f;
            cinderMain.startSize = .027f;
            cinderMain.startColor = new Color(1, .49f, .19f, .73f);
            cinderMain.maxParticles = 6;
            cinderMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = cinders.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)1) });
            var shape = cinders.shape;
            shape.enabled = false;
            var renderer = cinders.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = additive;
            renderer.sortingOrder = 46;
            cinders.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sub = sparks.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(cinders, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
            return new BurstView { root = root, body = body, light = light, shockwave = shockwave, sparks = sparks, smoke = smoke };
        }

        ParticleSystem Particles(string name, Transform root, Sprite[] sprites, Material material, bool smoke)
        {
            var gameObject = Child(name, root);
            var system = gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1;
            main.startDelay = smoke ? .08f : 0;
            main.startLifetime = smoke ? new ParticleSystem.MinMaxCurve(.22f, .42f) : new ParticleSystem.MinMaxCurve(.12f, .28f);
            main.startSpeed = smoke ? new ParticleSystem.MinMaxCurve(.14f, .29f) : new ParticleSystem.MinMaxCurve(.92f, 2.35f);
            main.startSize = smoke ? new ParticleSystem.MinMaxCurve(.26f, .49f) : new ParticleSystem.MinMaxCurve(.044f, .11f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.gravityModifier = smoke ? -.04f : .38f;
            main.maxParticles = smoke ? 4 : 10;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = smoke ? .08f : .04f;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)main.maxParticles) });
            var life = system.sizeOverLifetime;
            life.enabled = true;
            life.size = new ParticleSystem.MinMaxCurve(1, smoke ? AnimationCurve.EaseInOut(0, .45f, 1, 1.2f) : AnimationCurve.Linear(0, 1, 1, .16f));
            var tint = system.colorOverLifetime;
            tint.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(smoke ? new Color(.63f,.7f,.85f) : new Color(1,.45f,.21f), 1) },
                new[] { new GradientAlphaKey(smoke ? .57f : 1, 0), new GradientAlphaKey(.67f, .38f), new GradientAlphaKey(0, 1) });
            tint.color = gradient;

            var sheets = system.textureSheetAnimation;
            sheets.enabled = true;
            sheets.mode = ParticleSystemAnimationMode.Sprites;
            sheets.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1));
            foreach (var sprite in sprites) sheets.AddSprite(sprite);

            var draw = gameObject.GetComponent<ParticleSystemRenderer>();
            draw.sharedMaterial = material;
            draw.sortingOrder = smoke ? 44 : 47;
            draw.shadowCastingMode = ShadowCastingMode.Off;
            if (!smoke)
            {
                var trails = system.trails;
                trails.enabled = true;
                trails.ratio = .35f;
                trails.lifetime = .32f;
                trails.minVertexDistance = .075f;
                draw.trailMaterial = additive;
            }
            else
            {
                var noise = system.noise;
                noise.enabled = true;
                noise.strength = .10f;
                noise.frequency = .53f;
                noise.octaveCount = 1;
            }
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }

        static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        static SpriteRenderer Renderer(string name, Transform parent, Material material, int order)
        {
            var r = Child(name, parent).AddComponent<SpriteRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            return r;
        }

        public void Dispose()
        {
            if (parent) Object.Destroy(parent.gameObject);
            foreach (var sprite in generated) Object.Destroy(sprite);
            Object.Destroy(alpha); Object.Destroy(additive); Object.Destroy(atlasAlpha); Object.Destroy(atlasAdditive); Object.Destroy(radial);
        }
    }
}
