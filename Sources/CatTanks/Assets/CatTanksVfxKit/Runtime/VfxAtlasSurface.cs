using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatTanks.VfxKit
{
    [Serializable]
    public sealed class VfxRecipe
    {
        public string id, label, group, mode, color;
        public int tile, frames, grain, count, priority;
        public float seconds, size;
        [NonSerialized] public Color tint;
    }

    [Serializable]
    public sealed class WeaponRecipe
    {
        public string kind, muzzle, flight, impact;
    }

    [Serializable]
    sealed class VfxData
    {
        public int columns, rows, cell;
        public VfxRecipe[] recipes;
        public WeaponRecipe[] weapons;
    }

    public enum VfxQuality { Low, Medium, High }

    public sealed class VfxAtlasSurface : IDisposable
    {
        const string AssetPath = "CatTanksVfxKit/";
        const int MaxQuads = 640;
        const int CriticalReserve = 96;
        readonly Dictionary<string, VfxRecipe> recipes = new Dictionary<string, VfxRecipe>(StringComparer.Ordinal);
        readonly Dictionary<string, WeaponRecipe> weapons = new Dictionary<string, WeaponRecipe>(StringComparer.Ordinal);
        readonly List<Vector3> vertices = new List<Vector3>(MaxQuads * 4);
        readonly List<Color32> colors = new List<Color32>(MaxQuads * 4);
        readonly List<Vector2> uv = new List<Vector2>(MaxQuads * 4);
        readonly List<int> indices = new List<int>(MaxQuads * 6);
        readonly Rect[] tiles;
        readonly GameObject root;
        readonly Material material;
        readonly Mesh mesh;
        Rect visible;
        int limit, ordinary;
        VfxQuality quality;

        public int DrawnQuads => vertices.Count / 4;
        public int DroppedQuads { get; private set; }
        public string AtlasName => material.mainTexture.name;

        VfxAtlasSurface(Transform parent, VfxData data, Texture2D atlas, Shader shader)
        {
            foreach (var recipe in data.recipes)
            {
                recipe.tint = ColorUtility.TryParseHtmlString(recipe.color, out var color) ? color : Color.white;
                recipes.Add(recipe.id, recipe);
            }
            foreach (var weapon in data.weapons)
            {
                weapons.Add(weapon.kind, weapon);
                if (!string.IsNullOrEmpty(weapon.muzzle)) recipes.Add(BattleVfxTags.Muzzle(weapon.kind), recipes[weapon.muzzle]);
                if (!string.IsNullOrEmpty(weapon.impact)) recipes.Add(BattleVfxTags.Impact(weapon.kind), recipes[weapon.impact]);
            }
            tiles = new Rect[data.columns * data.rows];
            float halfU = .5f / atlas.width, halfV = .5f / atlas.height;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new Rect(i % data.columns / (float)data.columns + halfU,
                    (data.rows - 1 - i / data.columns) / (float)data.rows + halfV,
                    1f / data.columns - 2 * halfU, 1f / data.rows - 2 * halfV);

            root = new GameObject("CatTanks VFX Atlas", typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(parent, false);
            mesh = new Mesh { name = "CatTanks VFX quads" };
            mesh.MarkDynamic();
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(shader) { name = "CatTanks VFX Atlas", mainTexture = atlas };
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 43;
        }

        public static VfxAtlasSurface Create(Transform parent)
        {
            var dataFile = Resources.Load<TextAsset>(AssetPath + "effects");
            var atlas = Resources.Load<Texture2D>(AssetPath + "atlas");
            var shader = Resources.Load<Shader>(AssetPath + "AtlasVfx");
            if (!dataFile || !atlas || !shader)
                throw new InvalidOperationException("CatTanks VFX needs effects.json, atlas.png and AtlasVfx.shader in Resources/CatTanksVfxKit.");
            var data = JsonUtility.FromJson<VfxData>(dataFile.text);
            return new VfxAtlasSurface(parent, data, atlas, shader);
        }

        public VfxRecipe Find(string id) => !string.IsNullOrEmpty(id) && recipes.TryGetValue(id, out var recipe) ? recipe : null;
        public WeaponRecipe Weapon(string kind) => !string.IsNullOrEmpty(kind) && weapons.TryGetValue(kind, out var weapon) ? weapon : null;

        public void Begin(Rect viewportInBattleSpace, VfxQuality detail)
        {
            quality = detail;
            limit = detail == VfxQuality.Low ? 240 : detail == VfxQuality.Medium ? 400 : MaxQuads;
            visible = viewportInBattleSpace;
            ordinary = DroppedQuads = 0;
            vertices.Clear();
            colors.Clear();
            uv.Clear();
            indices.Clear();
        }

        public bool Draw(string key, Vector2 position, Vector2 directionOrEnd, float phase, float scale, int seed)
        {
            var recipe = Find(key);
            if (recipe == null) return false;
            Draw(recipe, position, directionOrEnd, phase, scale, seed);
            return true;
        }

        public void Draw(VfxRecipe recipe, Vector2 position, Vector2 directionOrEnd, float phase, float scale, int seed)
        {
            float p = Mathf.Clamp01(phase);
            float size = Mathf.Clamp(scale, .2f, 3f) * recipe.size;
            bool critical = recipe.priority >= 3;
            if (recipe.mode == "beam" || recipe.mode == "lightning")
            {
                if (!Onscreen(position, directionOrEnd, size * 4)) return;
                DrawBeam(recipe, position, directionOrEnd, p, size, seed, critical);
                Debris(recipe, directionOrEnd, size * 3, p, seed);
                return;
            }
            if (!Onscreen(position, position, size * 1.5f)) return;
            if (recipe.mode == "particles")
            {
                Debris(recipe, position, size, p, seed);
                return;
            }

            float angle = 0, opacity = 1;
            Vector2 extent = Vector2.one * size;
            if (recipe.mode == "aim" || recipe.mode == "trail" || recipe.mode == "spin")
                angle = Mathf.Atan2(directionOrEnd.y, directionOrEnd.x);
            if (recipe.mode == "spin") angle += p * Mathf.PI * 4;

            if (recipe.mode == "trail" && directionOrEnd.sqrMagnitude > .0001f)
            {
                var axis = directionOrEnd.normalized;
                int trailTile = TrailTile(recipe);
                var color = recipe.id == "shot-rocket" ? new Color(1, .49f, .26f, .90f) : new Color(1, 1, 1, .68f);
                Quad(trailTile, position - axis * size * .35f, new Vector2(size * 1.24f, size * .38f), angle, color, false);
            }

            if (recipe.mode == "loop")
            {
                float pulse = .5f + .5f * Mathf.Sin(p * Mathf.PI * 2);
                opacity = .72f + .19f * pulse;
                extent *= .95f + .10f * pulse;
                if (recipe.id == "shield-loop") extent.y *= 1.50f;
                if (recipe.id == "fall-shadow") extent.y *= .32f;
                if (recipe.id == "danger-zone") angle = Mathf.Atan2(directionOrEnd.y, directionOrEnd.x);
            }

            int frame = Mathf.Min(recipe.frames - 1, (int)(p * recipe.frames));
            var tint = recipe.tint;
            tint.a *= opacity;
            Quad(recipe.tile + frame, position, extent, angle, tint, critical);
            if (recipe.frames > 1) Debris(recipe, position, size, p, seed);
        }

        public void DrawTrailParticle(VfxRecipe recipe, Vector2 position, float phase, int seed)
        {
            if (recipe.grain < 0 || quality == VfxQuality.Low && recipe.id != "shot-rocket") return;
            float p = Mathf.Clamp01(phase);
            float size = recipe.size * Mathf.Lerp(.30f, .75f, p);
            if (!Onscreen(position, position, size)) return;
            var color = new Color(1, 1, 1, (.54f - .15f * Hash(seed, 9)) * (1 - p));
            Quad(recipe.grain, position + Vector2.up * (p * 12), Vector2.one * size, Hash(seed, 2) * 6.28f, color, false);
        }

        int TrailTile(VfxRecipe recipe)
        {
            if (recipe.id == "shot-rocket" || recipe.id == "shot-fire") return Find("shot-fire").tile;
            return recipe.id == "shot-arrow" || recipe.id == "shot-shell" || recipe.id == "shot-fast"
                ? Find("shot-fast").tile : recipe.tile;
        }

        void DrawBeam(VfxRecipe recipe, Vector2 start, Vector2 end, float p, float size, int seed, bool critical)
        {
            var outer = recipe.tint;
            outer.a = (1 - p) * .85f;
            var core = Color.Lerp(outer, Color.white, .68f);
            core.a = 1 - p;
            if (recipe.mode == "beam")
            {
                Segment(recipe.tile, start, end, size * 2.3f, outer, critical);
                Segment(recipe.tile, start, end, size * .77f, core, critical);
                return;
            }

            int count = quality == VfxQuality.Low ? 4 : 7;
            var delta = end - start;
            Vector2 perpendicular = new Vector2(-delta.y, delta.x).normalized;
            var previous = start;
            for (int i = 1; i <= count; i++)
            {
                Vector2 point = Vector2.Lerp(start, end, i / (float)count);
                if (i < count)
                    point += perpendicular * ((Hash(seed + (int)(p * 6), i) - .5f) * size * 3.2f);
                Segment(recipe.tile, previous, point, size * 1.92f, outer, critical);
                Segment(recipe.tile, previous, point, size * .68f, core, critical);
                previous = point;
            }
        }

        void Segment(int tile, Vector2 start, Vector2 end, float width, Color color, bool critical)
        {
            var delta = end - start;
            Quad(tile, (start + end) * .5f, new Vector2(delta.magnitude + 1.5f, width),
                Mathf.Atan2(delta.y, delta.x), color, critical);
        }

        void Debris(VfxRecipe recipe, Vector2 position, float size, float p, int seed)
        {
            if (recipe.grain < 0 || p >= .95f) return;
            int count = quality == VfxQuality.Low ? recipe.count / 3 : quality == VfxQuality.Medium ? (recipe.count + 1) / 2 : recipe.count;
            for (int i = 0; i < count; i++)
            {
                float angle = Hash(seed, i) * Mathf.PI * 2;
                float distance = Mathf.Lerp(.28f, .68f, Hash(seed, i + 19)) * size;
                float travel = (1 - (1 - p) * (1 - p)) * distance;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * travel;
                offset.y -= p * p * size * .17f;
                float radius = size * Mathf.Lerp(.08f, .19f, Hash(seed, i + 53)) * (1 - p * .55f);
                var color = new Color(1, 1, 1, (1 - p) * .92f);
                Quad(recipe.grain, position + offset, Vector2.one * radius, angle + p * 3, color, false);
            }
        }

        bool Onscreen(Vector2 a, Vector2 b, float margin) =>
            Mathf.Max(a.x, b.x) >= visible.xMin - margin && Mathf.Min(a.x, b.x) <= visible.xMax + margin &&
            Mathf.Max(a.y, b.y) >= visible.yMin - margin && Mathf.Min(a.y, b.y) <= visible.yMax + margin;

        void Quad(int tile, Vector2 center, Vector2 size, float angle, Color32 color, bool critical)
        {
            if (color.a == 0) return;
            if (DrawnQuads >= limit || !critical && ordinary >= limit - CriticalReserve)
            {
                DroppedQuads++;
                return;
            }
            if (!critical) ordinary++;
            var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var up = new Vector2(-right.y, right.x) * (.5f * size.y);
            right *= .5f * size.x;
            int v = vertices.Count;
            vertices.Add(center - right - up);
            vertices.Add(center - right + up);
            vertices.Add(center + right + up);
            vertices.Add(center + right - up);
            colors.Add(color); colors.Add(color); colors.Add(color); colors.Add(color);
            var rect = tiles[tile];
            uv.Add(new Vector2(rect.xMin, rect.yMin)); uv.Add(new Vector2(rect.xMin, rect.yMax));
            uv.Add(new Vector2(rect.xMax, rect.yMax)); uv.Add(new Vector2(rect.xMax, rect.yMin));
            indices.Add(v); indices.Add(v + 1); indices.Add(v + 2);
            indices.Add(v); indices.Add(v + 2); indices.Add(v + 3);
        }

        public void End()
        {
            mesh.Clear(true);
            if (vertices.Count == 0) return;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(visible.center, new Vector3(visible.width + 512, visible.height + 512, 4));
        }

        public void Dispose()
        {
            Object.Destroy(root);
            Object.Destroy(mesh);
            Object.Destroy(material);
        }

        static float Hash(int seed, int sample)
        {
            unchecked
            {
                uint x = (uint)seed * 747796405u + (uint)sample * 2891336453u + 277803737u;
                x = (x ^ (x >> 16)) * 2246822519u;
                return ((x ^ (x >> 13)) & 65535u) / 65535f;
            }
        }
    }
}
