using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Shape = CatTanks.VfxKit.WebVfxBatch.Shape;

namespace CatTanks.VfxKit
{
    // World units and timings come from tank_cat/src/visuals.js.
    public sealed class WebCombatVfx : IDisposable
    {
        const string Path = "CatTanksVfxKit/";
        public const int ParticleLimit = 2400;
        const int TransientLimit = 2048;

        struct Layer
        {
            public Shape shape;
            public Vector2 point, size;
            public Color color;
            public float birth, life, expand, rise, angle;
            public bool fire;
        }

        struct Particle
        {
            public Vector2 point, velocity;
            public Color color;
            public float birth, life, maxLife, size;
        }

        static readonly Color Hot = Catalog.Color("#fff2be"), Fire = Catalog.Color("#ff5a18"),
            Soot = Catalog.Color("#28313c"), Ember = Catalog.Color("#ff922f"),
            Spark = Catalog.Color("#ffe6a1"), Kill = Catalog.Color("#ffad37"),
            Repair = Catalog.Color("#83ffb0"), RepairParticle = Catalog.Color("#9bffb1"),
            Ice = Catalog.Color("#a2f6ff"), IceParticle = Catalog.Color("#c2ffff"),
            Dust = Catalog.Color("#d3cbb1"), Warning = Catalog.Color("#ff685f"),
            Burn = Catalog.Color("#ff7b24"), Aura = Catalog.Color("#9bffc0"),
            AuraParticle = Catalog.Color("#bbffac"), SpawnRing = Catalog.Color("#a18aff"),
            SpawnParticle = Catalog.Color("#9eafff"), Casing = Catalog.Color("#ffcf70"),
            WhiteCore = Catalog.Color("#efffff"), Mortar = Catalog.Color("#30374a"),
            Metal = Catalog.Color("#c3d8df"), SawCore = Catalog.Color("#ffce70"),
            Shield = Catalog.Color("#79efff"), DroneGlow = Catalog.Color("#66eaff");

        readonly WebVfxBatch shield, flightGlow, flight, smoke, effects, sparks;
        readonly Sprite rocket, arrow, drone;
        readonly SpriteRenderer support;
        readonly WebVfxBatch[] batches;
        readonly List<Particle> particles = new List<Particle>(ParticleLimit);
        readonly List<Layer> layers = new List<Layer>(TransientLimit);
        readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();
        System.Random random;
        Battle battle;
        float time, frame, shake;
        int lastEvent;
        bool changed;

        public Vector2 CameraKick { get; private set; }
        public int ParticleCount => particles.Count;
        public int DrawnQuads => shield.Count + flightGlow.Count + flight.Count + smoke.Count + effects.Count + sparks.Count;

        WebCombatVfx(Transform parent, Shader shader, Texture2D rocket, Texture2D arrow, Texture2D drone)
        {
            shield = new WebVfxBatch(parent, shader, "Web VFX shield", 29, 8, true);
            flightGlow = new WebVfxBatch(parent, shader, "Web VFX projectile glow", 34, 512, true);
            flight = new WebVfxBatch(parent, shader, "Web VFX projectile body", 35, 768, false);
            smoke = new WebVfxBatch(parent, shader, "Web VFX smoke", 42, TransientLimit, false);
            effects = new WebVfxBatch(parent, shader, "Web VFX flashes and rings", 46, TransientLimit, true);
            sparks = new WebVfxBatch(parent, shader, "Web VFX particles", 48, ParticleLimit, true);
            batches = new[] { shield, flightGlow, flight, smoke, effects, sparks };
            this.rocket = CreateSprite(rocket);
            this.arrow = CreateSprite(arrow);
            this.drone = CreateSprite(drone);
            support = new GameObject("Web VFX support drone", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            support.transform.SetParent(parent, false);
            support.sprite = this.drone;
            support.sortingOrder = 24;
            support.transform.localScale = Vector3.one * 105;
            support.enabled = false;
        }

        public static WebCombatVfx Create(Transform parent)
        {
            var shader = Resources.Load<Shader>(Path + "WebVfxShapes");
            var rocket = Resources.Load<Texture2D>(Path + "WebReference/rocket");
            var arrow = Resources.Load<Texture2D>(Path + "WebReference/arrow");
            var drone = Resources.Load<Texture2D>(Path + "WebReference/drone");
            return shader && shader.isSupported && rocket && arrow && drone ? new WebCombatVfx(parent, shader, rocket, arrow, drone) : null;
        }

        static Sprite CreateSprite(Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            Vector2.one * .5f, texture.width, 0, SpriteMeshType.FullRect, Vector4.zero, false);

        public Sprite ProjectileArt(string kind) => kind == "rocket" ? rocket : kind == "arrow" ? arrow : null;
        public static float ProjectileWidth(string kind) => kind == "rocket" ? 62 : 78;

        public bool Begin(Battle current)
        {
            bool reset = battle != current || current.time < time;
            if (reset)
            {
                battle = current;
                random = new System.Random(19);
                time = current.time;
                layers.Clear();
                particles.Clear();
                lastEvent = 0;
                shake = 0;
            }
            frame = current.time - time;
            time = current.time;
            changed = reset || frame > 0;
            foreach (var effect in current.effects)
            {
                if (effect.id <= lastEvent || !Handles(effect)) continue;
                Event(effect, time - Mathf.Max(0, effect.maxLife - effect.life));
                changed = true;
            }
            if (!changed) return false;
            foreach (var batch in batches) batch.Clear();
            Continuous(current);
            return true;
        }

        public static bool Handles(BattleEffect effect)
        {
            switch (effect.kind)
            {
                case "flash":
                case "impact":
                case "explosion":
                case "lightning":
                case "beam":
                case "web":
                    return true;
                case "ring":
                    return effect.visual == "heal" || effect.visual == "web-aura";
                default:
                    return false;
            }
        }

        public void Projectile(Projectile shot)
        {
            if (!changed) return;
            var kind = shot.weapon.kind;
            var pos = shot.position;
            var tint = WeaponColor(shot.weapon.color);
            switch (kind)
            {
                case "rocket":
                case "arrow":
                    break;
                case "saw":
                    flight.Add(Shape.Saw, pos, Vector2.one * 52, Metal, shot.angle + shot.age * 20);
                    flight.Add(Shape.Disk, pos, Vector2.one * 20, SawCore);
                    break;
                case "mortar":
                case "bomb":
                    flight.Add(Shape.Disk, pos, Vector2.one * 40, Mortar);
                    flightGlow.Add(Shape.Glow, pos, Vector2.one * 78, tint);
                    break;
                case "plasma":
                case "gravity":
                case "poison":
                case "ice":
                case "drone":
                    flightGlow.Add(Shape.Glow, pos, Vector2.one * (kind == "plasma" ? 102 : 68), tint);
                    flight.Add(Shape.Disk, pos, Vector2.one * 20, tint);
                    break;
                default:
                    bool flame = kind == "flame";
                    flight.Add(Shape.Solid, pos, new Vector2(flame ? 44 : kind == "enemy" ? 36 : 40, flame ? 24 : 8), tint, shot.angle);
                    flightGlow.Add(Shape.Glow, pos, Vector2.one * (flame ? 110 : 50), tint);
                    break;
            }
            if ((kind == "rocket" || kind == "plasma" || kind == "flame" || kind == "poison") && Chance(45))
                Burst(pos - Axis(shot.angle) * 25, tint, 2, 45, 42, .4f, time);
        }

        public void End()
        {
            if (!changed) return;
            DrawLayers();
            DrawParticles();
            shake = Mathf.Max(0, shake - frame * 20);
            CameraKick = new Vector2((Next() - .5f) * shake, (Next() - .5f) * shake * .6f);
            foreach (var batch in batches) batch.Upload();
        }

        void Event(BattleEffect effect, float start)
        {
            lastEvent = effect.id;
            var p = effect.position;
            switch (effect.kind)
            {
                case "flash":
                    var direction = p - effect.end;
                    Layered(Shape.Glow, p, new Vector2(100 + Next() * 60, 65 + Next() * 45), effect.color,
                        start, .12f, angle: Mathf.Atan2(direction.y, direction.x));
                    Burst(p, effect.color, effect.weaponKind == "rapid" ? 4 : 9, 150, 28, .3f, start);
                    break;
                case "impact":
                    if (effect.visual == "web-freeze") Freeze(p, start);
                    else if (effect.size > 40) Explosion(p, effect.size * 1.65f, effect.color, start);
                    else
                    {
                        Burst(p, effect.color, 14, 190, 32, .5f, start);
                        Layered(Shape.Glow, p, Vector2.one * effect.size * 4, effect.color, start, .18f, 2);
                    }
                    break;
                case "explosion":
                    bool boss = effect.visual == "web-boss";
                    Explosion(p, Mathf.Max(65, effect.size), Kill, start);
                    Burst(p, effect.color, 24, 240, 30, .9f, start);
                    shake = Mathf.Max(shake, boss ? 16 : 4);
                    break;
                case "beam": Beam(effect, start); break;
                case "lightning": Lightning(effect, start); break;
                case "ring":
                    if (effect.visual == "heal") Heal(p.x, start);
                    else
                    {
                        Ring(p, 140, Aura, .8f, start);
                        Burst(p, AuraParticle, 15, 70, 28, 1, start);
                    }
                    break;
                case "web": Auxiliary(effect, start); break;
            }
        }

        void Auxiliary(BattleEffect effect, float start)
        {
            var pos = effect.position;
            switch (effect.visual)
            {
                case "casing": Burst(pos, Casing, 2, 65, 5, .5f, start); break;
                case "spawn":
                    Ring(pos, 85, SpawnRing, .8f, start);
                    Burst(pos + Vector2.up * 5, SpawnParticle, 25, 100, 24, .9f, start);
                    break;
                case "repair": Heal(pos.x, start); break;
                case "melee": Burst(pos, effect.color, 12, 130, 20, .5f, start); break;
                case "shake": shake = Mathf.Max(shake, effect.size); break;
            }
        }

        void Heal(float x, float start)
        {
            Ring(new Vector2(x, 370), 200, Repair, .9f, start);
            Burst(new Vector2(x, 350), RepairParticle, 50, 220, 26, 1.3f, start);
        }

        void Freeze(Vector2 pos, float start)
        {
            Ring(pos, 100, Ice, .6f, start);
            Burst(pos, IceParticle, 20, 160, 20, .8f, start);
        }

        void Explosion(Vector2 point, float radius, Color tint, float start)
        {
            Layered(Shape.Glow, point, Vector2.one * radius * 2.5f, Hot, start, .16f, 3);
            Ring(point, radius * .7f, tint, .55f, start);
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector2((Next() - .5f) * radius * .75f, (Next() - .5f) * radius * .5f);
                Layered(Shape.Glow, point + offset, Vector2.one * radius * (1.1f + Next() * .6f),
                    i % 2 == 0 ? tint : Fire, start, .45f + Next() * .3f, 1.2f, 45 + i * 15, fire: true);
                Layered(Shape.Smoke, point + offset + Vector2.up * radius * .2f, new Vector2(radius * 1.2f, radius),
                    WithAlpha(Soot, .6f), start, .95f + Next() * .45f, .65f, 65, Next() * 6);
            }
            Burst(point, Ember, 36, radius * 2.1f, 42, .85f, start);
            Burst(point, Spark, 22, radius * 3, 15, .8f, start);
            shake = Mathf.Max(shake, Mathf.Min(12, radius * .065f));
        }

        void Beam(BattleEffect effect, float start)
        {
            bool sniper = effect.size <= 4;
            float life = effect.size > 16 ? effect.maxLife : sniper ? .14f : .24f;
            float width = sniper ? 6 : effect.size > 16 ? effect.size : 16;
            Segment(effect.position, effect.end, width, WithAlpha(effect.color, .65f), start, life);
            Segment(effect.position, effect.end, sniper ? 2 : width * .375f, WhiteCore, start, life);
            Layered(Shape.Glow, effect.position, Vector2.one * 150, effect.color, start, life);
        }

        void Lightning(BattleEffect effect, float start)
        {
            Vector2 previous = effect.position;
            for (int i = 1; i <= 12; i++)
            {
                var point = Vector2.Lerp(effect.position, effect.end, i / 12f);
                if (i < 12) point += new Vector2((Next() - .5f) * 28, (Next() - .5f) * 30);
                Segment(previous, point, 1.45f, effect.color, start, .22f);
                previous = point;
            }
            Burst(effect.end, effect.color, 12, 90, 18, .35f, start);
        }

        void Continuous(Battle current)
        {
            if (current.Buff("shield") > 0)
            {
                var point = new Vector2(current.playerX, 440);
                shield.Add(Shape.Disk, point, new Vector2(410, 660), WithAlpha(Shield, .06f + Mathf.Sin(time * 8) * .025f));
                shield.Add(Shape.Ring, point, new Vector2(410, 660), WithAlpha(Shield, .6f), inner: .985f);
            }
            support.enabled = current.Buff("drone") > 0;
            if (support.enabled)
            {
                var point = new Vector2(current.playerX + 190, 660 + Mathf.Sin(time * 4) * 14);
                support.transform.localPosition = point;
                shield.Add(Shape.Glow, point - Vector2.up * 25, Vector2.one * 80, DroneGlow);
            }
            foreach (var enemy in current.enemies)
            {
                float size = enemy.def.size;
                if (enemy.warning && Chance(8))
                    Burst(enemy.position + Vector2.up * size * .3f, Warning, 2, 35, 22, .4f, time);
                if (enemy.burn > 0 && Chance(35))
                    Burst(enemy.position + new Vector2((Next() - .5f) * size * .65f, -size * .15f), Burn, 3, 100, 64, .7f, time);
            }
            if (Chance(14))
                Burst(new Vector2(current.playerX - 125, 189), Dust, 2, 34, 28, .8f, time);
        }

        void Ring(Vector2 point, float radius, Color tint, float life, float start) =>
            Layered(Shape.Ring, point, Vector2.one * radius * 2, tint, start, life, 2);

        void Segment(Vector2 from, Vector2 to, float width, Color tint, float start, float life)
        {
            var delta = to - from;
            Layered(Shape.Solid, (from + to) * .5f, new Vector2(delta.magnitude + .5f, width),
                tint, start, life, angle: Mathf.Atan2(delta.y, delta.x));
        }

        void Layered(Shape shape, Vector2 point, Vector2 size, Color tint, float start, float life,
            float expand = 0, float rise = 0, float angle = 0, bool fire = false)
        {
            if (layers.Count == TransientLimit) return;
            layers.Add(new Layer { shape = shape, point = point, size = size, color = tint, birth = start,
                life = life, expand = expand, rise = rise, angle = angle, fire = fire });
        }

        void Burst(Vector2 point, Color tint, int count, float force, float size, float life, float start)
        {
            for (int i = 0; i < count && particles.Count < ParticleLimit; i++)
            {
                float angle = Next() * Mathf.PI * 2, speed = force * (.15f + Next());
                particles.Add(new Particle { point = point, velocity = Axis(angle) * speed + Vector2.up * force * .2f,
                    birth = start, life = life * (.5f + Next()), maxLife = life, size = size * (.3f + Next()), color = tint });
            }
        }

        void DrawLayers()
        {
            int keep = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                float age = time - layer.birth;
                if (age >= layer.life) continue;
                layers[keep++] = layer;
                float alpha = 1 - age / layer.life;
                var color = layer.fire ? new Color(1, .12f + alpha * .48f, .015f + alpha * .08f) : layer.color;
                var batch = layer.shape == Shape.Smoke ? smoke : effects;
                var size = layer.size * Mathf.Pow(1 + layer.expand / 60, age * 60);
                batch.Add(layer.shape, layer.point + Vector2.up * (layer.rise * age), size, WithAlpha(color, alpha), layer.angle);
            }
            if (keep < layers.Count) layers.RemoveRange(keep, layers.Count - keep);
        }

        void DrawParticles()
        {
            int keep = 0;
            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                float age = time - particle.birth;
                if (age >= particle.life) continue;
                particles[keep++] = particle;
                var point = particle.point + particle.velocity * age - Vector2.up * (75 * age * Mathf.Max(0, age - 1f / 60));
                sparks.Add(Shape.Particle, point, Vector2.one * particle.size, WithAlpha(particle.color, (particle.life - age) / particle.maxLife));
            }
            if (keep < particles.Count) particles.RemoveRange(keep, particles.Count - keep);
        }

        Color WeaponColor(string key)
        {
            if (string.IsNullOrEmpty(key)) return Casing;
            if (!colors.TryGetValue(key, out var tint)) colors.Add(key, tint = Catalog.Color(key));
            return tint;
        }

        float Next() => (float)random.NextDouble();
        bool Chance(float rate) => frame > 0 && Next() < frame * rate;
        static Vector2 Axis(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        static Color WithAlpha(Color color, float alpha) { color.a *= Mathf.Clamp01(alpha); return color; }

        public void Dispose()
        {
            foreach (var batch in batches) batch.Dispose();
            Object.Destroy(support.gameObject);
            Object.Destroy(rocket); Object.Destroy(arrow); Object.Destroy(drone);
        }
    }
}
