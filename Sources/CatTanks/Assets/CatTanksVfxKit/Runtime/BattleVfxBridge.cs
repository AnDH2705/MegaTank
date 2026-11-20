using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTanks.VfxKit
{
    // BattleView owns one bridge. Read-only sampling keeps gameplay and pause independent of rendering.
    public sealed class BattleVfxBridge : IDisposable
    {
        const float TrailInterval = .045f;
        const int MaxTrailParticles = 112;
        readonly VfxAtlasSurface surface;
        readonly List<TrailParticle> trails = new List<TrailParticle>(MaxTrailParticles);
        Battle lastBattle;
        float nextTrailSample;

        struct TrailParticle
        {
            public VfxRecipe recipe;
            public Vector2 position;
            public float birth, life;
            public int seed;
        }

        public VfxQuality Quality { get; set; } = VfxQuality.High;
        public bool ReducedMotion { get; set; }
        public Vector2 CameraOffset { get; private set; }
        public int DrawnQuads => surface.DrawnQuads;
        public int DroppedQuads => surface.DroppedQuads;

        public BattleVfxBridge(Transform parent) => surface = VfxAtlasSurface.Create(parent);
        public WeaponRecipe Weapon(string kind) => surface.Weapon(kind);
        public bool Handles(BattleEffect effect) => Resolve(effect) != null;
        public bool Handles(Projectile shot) => surface.Find(surface.Weapon(shot.weapon.kind)?.flight) != null;

        VfxRecipe Resolve(BattleEffect effect)
        {
            if (effect.kind == "text") return null;
            var tagged = surface.Find(effect.visual);
            if (tagged != null) return tagged;
            switch (effect.kind)
            {
                case "flash": return surface.Find("flash");
                case "impact":
                    switch (effect.visual)
                    {
                        case "ice": return surface.Find("frost");
                        case "electric": return surface.Find("arc");
                        case "blast": return surface.Find("blast");
                        default: return surface.Find("hit");
                    }
                case "explosion": return surface.Find("death");
                case "beam": return surface.Find(effect.size > 20 ? "beam-orbital" : effect.size >= 10 ? "beam-ion" : "beam-sniper");
                case "lightning": return surface.Find("chain");
                case "ring":
                    switch (effect.visual)
                    {
                        case "shield": return surface.Find("ward");
                        case "capture": return surface.Find("cage");
                        case "heal": return surface.Find("heal");
                        default: return surface.Find("rally");
                    }
                default: return null;
            }
        }

        public void Render(Battle battle, Camera camera)
        {
            if (lastBattle != battle)
            {
                lastBattle = battle;
                nextTrailSample = 0;
                trails.Clear();
            }
            float height = camera.orthographicSize;
            float width = height * camera.aspect;
            Vector3 center = camera.transform.position;
            var viewport = Rect.MinMaxRect(center.x - width, center.y - height, center.x + width, center.y + height);
            surface.Begin(viewport, Quality);
            CameraOffset = Vector2.zero;

            SampleTrails(battle);
            DrawTrails(battle.time);
            foreach (var shot in battle.bullets)
                DrawProjectile(shot);

            DrawStatuses(battle);
            foreach (var effect in battle.effects)
            {
                var recipe = Resolve(effect);
                if (recipe == null || effect.maxLife <= 0) continue;
                float phase = 1 - effect.life / effect.maxLife;
                bool linked = recipe.mode == "beam" || recipe.mode == "lightning";
                Vector2 direction = linked ? effect.end : effect.position - effect.end;
                if (!linked && effect.end == Vector2.zero) direction = Vector2.right;
                Vector2 position = recipe.id == "arc" && effect.kind == "lightning" ? effect.end : effect.position;
                float scale = effect.kind == "explosion" ? Mathf.Clamp(effect.size / 38f, .65f, 2.1f) :
                    effect.kind == "impact" ? Mathf.Clamp(effect.size / (recipe.size >= 100 ? 48f : 20f), .65f, 2.1f) :
                    recipe.id == "cage" ? Mathf.Clamp(effect.size / 100f, .65f, 2.1f) : 1;
                surface.Draw(recipe, position, direction, phase, scale, effect.id);
                if (recipe.id == "chain")
                    surface.Draw("arc", effect.end, Vector2.right, phase, .62f, effect.id);
                if (!ReducedMotion && viewport.Contains(linked ? effect.end : position)
                    && (recipe.id == "rocketblast" || recipe.id == "blast" || recipe.id == "beam-orbital"))
                {
                    float age = effect.maxLife - effect.life;
                    float envelope = Mathf.Clamp01(1 - age / .16f);
                    var shake = new Vector2(Mathf.Sin(age * 77 + effect.id), Mathf.Cos(age * 83 + effect.id))
                        * (envelope * (recipe.id == "blast" ? 2.2f : 3.4f));
                    if (shake.sqrMagnitude > CameraOffset.sqrMagnitude) CameraOffset = shake;
                }
            }
            surface.End();
        }

        VfxRecipe Flight(Projectile shot) => surface.Find(surface.Weapon(shot.weapon.kind)?.flight);

        static bool IsFalling(Projectile shot) => shot.weapon.kind == "mortar" || shot.weapon.kind == "bomb";

        static Vector2 Direction(Projectile shot)
        {
            var movement = shot.position - shot.previous;
            if (IsFalling(shot) && movement.sqrMagnitude > .001f) return movement.normalized;
            return new Vector2(Mathf.Cos(shot.angle), Mathf.Sin(shot.angle));
        }

        void DrawProjectile(Projectile shot)
        {
            var recipe = Flight(shot);
            if (recipe == null || shot.dead) return;
            if (IsFalling(shot) && shot.flight - shot.age <= .38f)
            {
                float radius = Mathf.Max(28, shot.weapon.splash);
                surface.Draw("fall-shadow", shot.end, Vector2.right, Mathf.Clamp01((shot.flight - shot.age) / .38f),
                    Mathf.Clamp(radius / 40f, .7f, 3), shot.id);
            }

            surface.Draw(recipe, shot.position, Direction(shot), Loop(shot.age, recipe.seconds), 1, shot.id);
        }

        void SampleTrails(Battle battle)
        {
            float time = battle.time;
            if (time < nextTrailSample) return;
            nextTrailSample = time + TrailInterval;
            foreach (var shot in battle.bullets)
            {
                if (trails.Count >= MaxTrailParticles) break;
                if (shot.dead) continue;
                var recipe = Flight(shot);
                if (recipe == null || recipe.grain < 0 || recipe.count == 0) continue;
                if (recipe.id == "shot-fast" && (shot.id & 3) != 0) continue;
                if (Quality == VfxQuality.Low && recipe.id != "shot-rocket") continue;
                trails.Add(new TrailParticle
                {
                    recipe = recipe, position = shot.position - Direction(shot) * (recipe.size * .30f), birth = time,
                    life = recipe.id == "shot-rocket" ? .34f : .19f, seed = shot.id
                });
            }
        }

        void DrawTrails(float now)
        {
            int keep = 0;
            for (int i = 0; i < trails.Count; i++)
            {
                var puff = trails[i];
                float age = now - puff.birth;
                if (age >= puff.life) continue;
                trails[keep++] = puff;
                surface.DrawTrailParticle(puff.recipe, puff.position, age / puff.life, puff.seed);
            }
            if (keep < trails.Count) trails.RemoveRange(keep, trails.Count - keep);
        }

        void DrawStatuses(Battle battle)
        {
            if (battle.Buff("shield") > 0)
                surface.Draw("shield-loop", new Vector2(battle.playerX, 350), Vector2.right, Loop(battle.time, .8f), 2.4f, 0);
            if (battle.Buff("rally") > 0)
                surface.Draw("rally-loop", new Vector2(battle.playerX - 93, 307), Vector2.up, Loop(battle.time, .7f), 1.2f, 0);
            if (battle.Buff("poison") > 0 || battle.Buff("frost") > 0 || battle.Buff("slow") > 0)
                surface.Draw(battle.Buff("poison") > 0 ? "poison-loop" : "slow-loop", new Vector2(battle.playerX - 90, 275),
                    Vector2.right, Loop(battle.time, .7f), 1.2f, 0);
            foreach (var actor in battle.enemies)
            {
                if (actor.dead || !actor.visible) continue;
                float size = actor.def.size;
                if (actor.warning || actor.charging)
                    surface.Draw("danger-zone", actor.position + new Vector2(-size * .77f, -size * .22f), Vector2.left,
                        Loop(actor.age, .5f), Mathf.Clamp(size / 80f, .8f, 2f), actor.id);
                if (actor.id == battle.targetId)
                    surface.Draw("target-lock", actor.position, Vector2.right, Loop(actor.age, .8f), size / 88f, actor.id);
                Vector2 anchor = actor.position + Vector2.up * (size * .42f);
                if (actor.stun > 0) Status("stun-loop", anchor, actor);
                if (actor.slow > 0 || battle.Buff("freeze") > 0) Status("slow-loop", anchor + new Vector2(-20, -19), actor);
                if (actor.burn > 0) Status("burn-loop", anchor + new Vector2(0, -31), actor);
                if (actor.poison > 0) Status("poison-loop", anchor + new Vector2(19, -19), actor);
            }
        }

        void Status(string key, Vector2 position, Actor actor) =>
            surface.Draw(key, position, Vector2.right, Loop(actor.age, .7f), .8f, actor.id);

        static float Loop(float time, float duration) => Mathf.Repeat(time, duration) / duration;
        public void Dispose() => surface.Dispose();
    }
}
