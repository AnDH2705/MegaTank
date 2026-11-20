using System.Collections.Generic;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class BattleView
    {
        static readonly Color WeaponHitTint = new Color(1f, .92f, .72f);
        static readonly Color HotGold = new Color(1f, .61f, .19f);
        static readonly Color HotCore = new Color(1f, .96f, .74f);
        const float RocketSmokeInterval = .042f;
        const int MaxRocketSmoke = 96;
        readonly Dictionary<string, Sprite[]> weaponSheets = new Dictionary<string, Sprite[]>();
        readonly List<SmokePuff> rocketSmoke = new List<SmokePuff>(MaxRocketSmoke);
        Sprite streak;
        Battle smokeBattle;
        float nextSmokeSample;
        Vector2 weaponShake;

        struct SmokePuff
        {
            public Vector2 position;
            public float birth, angle;
        }

        Sprite[] WeaponSheet(string name, int columns = 4, int rows = 4)
        {
            if (weaponSheets.TryGetValue(name, out var sprites)) return sprites;
            var texture = Resources.Load<Texture2D>("WeaponVFX/" + name);
            int width = texture.width / columns;
            int height = texture.height / rows;
            sprites = new Sprite[columns * rows];
            for (int i = 0; i < sprites.Length; i++)
            {
                var rect = new Rect(i % columns * width, (rows - 1 - i / columns) * height, width, height);
                sprites[i] = UnityEngine.Sprite.Create(texture, rect, Vector2.one * .5f, width, 0, SpriteMeshType.FullRect);
            }
            weaponSheets.Add(name, sprites);
            return sprites;
        }

        static Sprite Frame(Sprite[] frames, float progress, int first, int count) =>
            frames[first + Mathf.Clamp(Mathf.FloorToInt(progress * count), 0, count - 1)];

        Sprite Streak()
        {
            if (streak) return streak;
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Weapon Streak", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float along = (x + .5f) / size;
                float across = Mathf.Abs((y + .5f) / size * 2 - 1);
                float halfWidth = Mathf.Max(0, Mathf.Min(along * 1.25f, (1 - along) * 4));
                float alpha = Mathf.Clamp01((halfWidth - across) * 16) * Mathf.Clamp01(along * 2);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * alpha));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            streak = UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, size);
            return streak;
        }

        Transform CreateProjectileView(Projectile shot)
        {
            if (!shot.enemy)
            {
                switch (shot.weapon.kind)
                {
                    case "rapid":
                        return ProjectileSprite("Gatling round", Streak(), shot.position, new Vector2(29, 8), HotCore, 35);
                    case "shell":
                        return ProjectileSprite("Cannon shell", disk, shot.position, new Vector2(20, 10), HotCore, 35);
                    case "rocket":
                        return ProjectileSprite("Rocket", WeaponSheet("rocket-body", 1, 1)[0], shot.position, new Vector2(33, 20), Color.white, 35);
                }
            }

            string kind = shot.weapon.kind;
            bool round = kind == "plasma" || kind == "poison" || kind == "ice" || kind == "mortar" || kind == "bomb" || kind == "gravity";
            return ProjectileSprite("Projectile", round ? disk : square, shot.position, round ? Vector2.one * 22 : new Vector2(26, 7), Catalog.Color(shot.weapon.color), 35);
        }

        bool SampleRocketSmoke(Battle battle)
        {
            if (battle != smokeBattle)
            {
                rocketSmoke.Clear();
                nextSmokeSample = 0;
                smokeBattle = battle;
            }
            if (battle.time < nextSmokeSample) return false;
            nextSmokeSample = battle.time + RocketSmokeInterval;
            return true;
        }

        void DrawProjectileTrail(Projectile shot, bool emitSmoke)
        {
            var direction = new Vector2(Mathf.Cos(shot.angle), Mathf.Sin(shot.angle));
            float angle = shot.angle * Mathf.Rad2Deg;
            switch (shot.weapon.kind)
            {
                case "rapid":
                    FxSprite("Gatling tracer", Streak(), shot.position - direction * 17, new Vector2(48, 7), HotGold, angle, 34);
                    break;
                case "shell":
                    FxSprite("Cannon tracer", Streak(), shot.position - direction * 21, new Vector2(56, 13), HotGold, angle, 34);
                    break;
                case "rocket":
                    float flicker = 33 + 7 * Mathf.Sin(shot.age * 83 + shot.id);
                    FxSprite("Rocket exhaust", Streak(), shot.position - direction * 25, new Vector2(flicker, 17), HotGold, angle, 34);
                    FxSprite("Rocket exhaust core", Streak(), shot.position - direction * 21, new Vector2(flicker * .6f, 9), HotCore, angle, 34);
                    if (emitSmoke && rocketSmoke.Count < MaxRocketSmoke)
                    {
                        rocketSmoke.Add(new SmokePuff
                        {
                            position = shot.position - direction * 23,
                            birth = nextSmokeSample - RocketSmokeInterval,
                            angle = angle + (shot.id % 7 - 3) * 17
                        });
                    }
                    break;
            }
        }

        void DrawRocketSmoke(float time)
        {
            const float duration = .34f;
            int expired = 0;
            while (expired < rocketSmoke.Count && time - rocketSmoke[expired].birth >= duration) expired++;
            if (expired > 0) rocketSmoke.RemoveRange(0, expired);
            foreach (var puff in rocketSmoke)
            {
                float progress = Mathf.Clamp01((time - puff.birth) / duration);
                DrawSmoke("Rocket trail", puff.position + Vector2.up * (progress * 16), progress, Mathf.Lerp(21, 44, progress), puff.angle, .57f, 33);
            }
        }

        void DrawSmoke(string name, Vector2 position, float progress, float size, float angle, float opacity, int order)
        {
            var smoke = Frame(WeaponSheet("smoke"), progress, 0, 11);
            var color = new Color(.64f, .7f, .77f, opacity * (1 - progress));
            FxSprite(name, smoke, position, Vector2.one * size, color, angle, order);
        }

        void DrawWeaponEffect(BattleEffect effect)
        {
            float age = effect.maxLife - effect.life;
            float progress = Mathf.Clamp01(age / effect.maxLife);
            Vector2 direction = (effect.position - effect.end).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (effect.kind == "flash")
            {
                DrawMuzzle(effect, direction, angle, progress);
                return;
            }

            bool rocket = effect.weaponKind == "rocket";
            if (effect.weaponKind == "rapid")
            {
                var frame = Frame(WeaponSheet("gatling-impact", 4, 2), progress, 1, 5);
                FxSprite("Gatling hit", frame, effect.position, Vector2.one * 47, Color.white, angle, 46);
                DrawSparks(effect, angle, progress, 3, 26);
                return;
            }

            float diameter = Mathf.Clamp(effect.size * (rocket ? 2.8f : 2.6f), 75, 155);
            var blastFrames = WeaponSheet(rocket ? "rocket-blast" : "cannon-blast");
            var blast = age <= .05f ? blastFrames[0] : Frame(blastFrames, Mathf.InverseLerp(.05f, effect.maxLife, age), 1, 13);
            FxSprite(rocket ? "Rocket explosion" : "Cannon explosion", blast, effect.position, diameter, Color.white, 46);

            float shock = Mathf.Clamp01(age / .18f);
            if (shock < 1)
            {
                float width = diameter * Mathf.Lerp(.4f, 1.16f, shock);
                var color = rocket ? new Color(1f, .65f, .36f, .65f * (1 - shock)) : new Color(1f, .84f, .54f, .55f * (1 - shock));
                FxSprite("Blast pressure ring", ring, effect.position, width, color, 45);
                Vector2 shake = new Vector2(Mathf.Cos(age * 68), Mathf.Sin(age * 83)) * ((rocket ? 4f : 2.7f) * (1 - shock));
                if (shake.sqrMagnitude > weaponShake.sqrMagnitude) weaponShake = shake;
                DrawSparks(effect, angle, shock, rocket ? 6 : 5, diameter * .48f);
            }

            float dust = Mathf.InverseLerp(.09f, effect.maxLife, age);
            if (dust > 0)
            {
                var rise = Vector2.up * (diameter * (.12f + .38f * dust));
                DrawSmoke("Blast smoke", effect.position + rise, dust, diameter * (.48f + .22f * dust), effect.id % 31 - 15, .68f, 44);
                DrawSmoke("Blast dust", effect.position - Vector2.up * diameter * .16f, dust, diameter * .56f, 20, .35f, 44);
            }
        }

        void DrawMuzzle(BattleEffect effect, Vector2 direction, float angle, float progress)
        {
            if (effect.weaponKind == "rapid")
            {
                int frame = (effect.id + Mathf.FloorToInt(progress * 2)) % 8;
                var color = new Color(1, 1, 1, 1 - .75f * progress);
                FxSprite("Gatling muzzle", WeaponSheet("gatling-muzzle", 4, 2)[frame],
                    effect.position + direction * 10, new Vector2(43, 34), color, angle, 36);
                return;
            }

            if (effect.weaponKind == "shell")
            {
                var frames = WeaponSheet("cannon-muzzle", 4, 2);
                var flash = progress <= .2f ? frames[0] : Frame(frames, Mathf.InverseLerp(.2f, 1, progress), 1, 7);
                FxSprite("Cannon muzzle", flash,
                    effect.position + direction * 13, Vector2.one * 91, Color.white, angle, 36);
                return;
            }

            float flare = Mathf.Clamp01(1 - progress * 2.5f);
            if (flare > 0)
            {
                var color = new Color(HotGold.r, HotGold.g, HotGold.b, flare);
                FxSprite("Rocket ignition", Streak(), effect.position + direction * 6, new Vector2(63, 31), color, angle, 36);
            }
            DrawSmoke("Rocket backblast", effect.position - direction * (7 + progress * 30),
                progress, Mathf.Lerp(38, 68, progress), angle, .65f, 34);
        }

        void DrawSparks(BattleEffect effect, float angle, float progress, int count, float reach)
        {
            var color = new Color(HotCore.r, HotGold.g + .25f, HotGold.b, 1 - progress);
            for (int i = 0; i < count; i++)
            {
                float sparkAngle = angle + (i + 1) * 360f / count + effect.id % 29;
                float radians = sparkAngle * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                float distance = reach * Mathf.Lerp(.2f, 1, progress) * (i % 2 == 0 ? 1 : .65f);
                var position = effect.position + direction * distance + Vector2.down * (13 * progress * progress);
                FxSprite("Impact spark", Streak(), position, new Vector2(18 - progress * 12, 5), color, sparkAngle, 47);
            }
        }

        void DisposeWeaponEffects()
        {
            foreach (var sheet in weaponSheets.Values)
            foreach (var sprite in sheet)
                Object.Destroy(sprite);
            if (!streak) return;
            Object.Destroy(streak.texture);
            Object.Destroy(streak);
        }
    }
}
