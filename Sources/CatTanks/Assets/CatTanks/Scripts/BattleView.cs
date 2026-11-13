using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed class BattleView
    {
        public readonly GameObject root;
        public Camera camera;
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        readonly Dictionary<int, Transform> actors = new Dictionary<int, Transform>(), projectiles = new Dictionary<int, Transform>();
        readonly List<GameObject> spriteFxPool = new List<GameObject>(), lineFxPool = new List<GameObject>();
        int spriteFxCursor, lineFxCursor;
        Transform tank, shield, decoyView, targetRing;
        Sprite square, disk, ring;
        Material lineMaterial;
        int shownFloors = -1;
        public BattleView(Camera cam)
        {
            camera = cam; root = new GameObject("Battle World");
            square = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            disk = Shape(false); ring = Shape(true); lineMaterial = new Material(Shader.Find("Sprites/Default"));
            Landscape();
        }
        Sprite Shape(bool hollow)
        {
            var tx = new Texture2D(128, 128, TextureFormat.RGBA32, false); tx.name = hollow ? "Shield Ring" : "Circle";
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64));
                float a = Mathf.Clamp01(64 - d) * (hollow ? Mathf.Clamp01(d - 60) : 1); pixels[y * 128 + x] = new Color(1, 1, 1, a);
            }
            tx.SetPixels(pixels); tx.Apply(); return UnityEngine.Sprite.Create(tx, new Rect(0, 0, 128, 128), Vector2.one * .5f, 128);
        }
        public Sprite Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return square;
            if (sprites.TryGetValue(name, out var s)) return s;
            var tx = Resources.Load<Texture2D>("Art/" + name); if (!tx) return square;
            s = UnityEngine.Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), Vector2.one * .5f, tx.width); sprites[name] = s; return s;
        }
        Transform Sprite(string name, Sprite art, Vector2 pos, Vector2 size, Color color, int order, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent ?? root.transform, false); go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = art; sr.color = color; sr.sortingOrder = order;
            float ratio = art.rect.height / art.rect.width;
            go.transform.localScale = new Vector3(size.x, size.y / ratio, 1); return go.transform;
        }
        Transform Rect(string name, Vector2 pos, Vector2 size, string color, int order, Transform parent = null) => Sprite(name, square, pos, size, Catalog.Color(color), order, parent);
        Transform Image(string name, Vector2 pos, float width, int order, Transform parent = null, bool flip = false)
        {
            var s = Art(name); var t = Sprite(name, s, pos, new Vector2(width, width * s.rect.height / s.rect.width), Color.white, order, parent); t.GetComponent<SpriteRenderer>().flipX = flip; return t;
        }
        void FxSprite(string name, Sprite art, Vector2 pos, float size, Color color, int order)
        {
            if (spriteFxCursor == spriteFxPool.Count)
            {
                var go = new GameObject("Pooled Sprite FX", typeof(SpriteRenderer)); go.transform.SetParent(root.transform, false); spriteFxPool.Add(go);
            }
            var item = spriteFxPool[spriteFxCursor++]; item.SetActive(true); item.name = name;
            item.transform.localPosition = pos; item.transform.localScale = new Vector3(size, size, 1);
            var sr = item.GetComponent<SpriteRenderer>(); sr.sprite = art; sr.color = color; sr.sortingOrder = order;
        }
        LineRenderer FxLine()
        {
            if (lineFxCursor == lineFxPool.Count)
            {
                var go = new GameObject("Pooled Beam FX", typeof(LineRenderer)); go.transform.SetParent(root.transform, false);
                var lr = go.GetComponent<LineRenderer>(); lr.sharedMaterial = lineMaterial; lr.sortingOrder = 48; lineFxPool.Add(go);
            }
            var item = lineFxPool[lineFxCursor++]; item.SetActive(true); return item.GetComponent<LineRenderer>();
        }
        void Polygon(string name, Vector2[] points, string color, int order)
        {
            // Mountain strip triangulated against the ground line.
            var vertices = new List<Vector3>(); var indices = new List<int>();
            for (int i = 0; i < points.Length; i++) {vertices.Add(points[i]); vertices.Add(new Vector3(points[i].x, 0, 0));}
            for (int i = 0; i < points.Length - 1; i++) {int a = i * 2; indices.AddRange(new[] {a, a + 2, a + 1, a + 1, a + 2, a + 3});}
            var mesh = new Mesh {name = name}; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false); go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) {color = Catalog.Color(color)}; renderer.sortingOrder = order;
        }
        void Landscape()
        {
            var texture = new Texture2D(2, 256); var pixels = new Color[512];
            for (int y = 0; y < 256; y++) for (int x = 0; x < 2; x++)
            {
                float t = y / 255f;
                pixels[y * 2 + x] = t < .6f ? Color.Lerp(Catalog.Color("#062b48"), Catalog.Color("#079cc5"), t / .6f) : Color.Lerp(Catalog.Color("#079cc5"), Catalog.Color("#0872b1"), (t - .6f) / .4f);
            }
            texture.SetPixels(pixels); texture.Apply();
            var bg = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 256), Vector2.one * .5f, 2);
            Sprite("Blue Moon Sky", bg, new Vector2(800, 450), new Vector2(2400, 1400), Color.white, -110);
            Sprite("Moon Halo", disk, new Vector2(790, 580), new Vector2(460, 460), new Color(.2f, 1, .9f, .08f), -108);
            Sprite("Moon", disk, new Vector2(790, 580), new Vector2(354, 354), Catalog.Color("#57e9df"), -107);
            var rng = new System.Random(19);
            for (int i = 0; i < 18; i++) Sprite("Moon Crater", disk, new Vector2(790 + (float)(rng.NextDouble() - .5) * 240, 580 + (float)(rng.NextDouble() - .5) * 240), new Vector2(28 + i % 4 * 12, 20 + i % 3 * 9), new Color(.2f, .7f, .75f, .13f), -106);
            for (int i = 0; i < 90; i++) Sprite("Star", disk, new Vector2((float)rng.NextDouble() * 2000 - 200, 400 + (float)rng.NextDouble() * 500), Vector2.one * (1 + i % 3), new Color(1, .95f, .7f, .3f + i % 4 * .15f), -105);
            for (int layer = 0; layer < 3; layer++)
            {
                var points = new List<Vector2>();
                for (float x = -400; x < 2100; x += 100)
                {
                    float y = 400 - layer * 90 + (float)rng.NextDouble() * 150;
                    points.Add(new Vector2(x, y - 80)); points.Add(new Vector2(x + 20, y - 30)); points.Add(new Vector2(x + 45, y)); points.Add(new Vector2(x + 70, y - 25));
                }
                Polygon("Mountain Layer " + layer, points.ToArray(), new[] {"#0c6790", "#0b5477", "#103e5f"}[layer], -100 + layer);
            }
            Rect("Moon Road", new Vector2(800, 150), new Vector2(2400, 50), "#5086a1", -80);
            Rect("Road Edge", new Vector2(800, 72), new Vector2(2400, 105), "#32627f", -79);
            for (int i = 0; i < 100; i++) Sprite("Road Stone", disk, new Vector2((float)rng.NextDouble() * 2200 - 300, (float)rng.NextDouble() * 160), new Vector2(5 + i % 6 * 8, 2 + i % 3), Catalog.Color(i % 2 == 0 ? "#224d6c" : "#6698aa"), -78);
            foreach (float x in new[] {42, 610, 1000, 1480})
                for (int i = 0; i < 3; i++) {var t = Rect("Moon Crystal", new Vector2(x + i * 13, 193 + i * 6), new Vector2(12, 40 + i * 10), i % 2 == 0 ? "#70e5e4" : "#42b8d2", -20); t.localEulerAngles = new Vector3(0, 0, (i - 1) * 15);}
            tank = new GameObject("Modular Cat Fortress").transform; tank.SetParent(root.transform, false);
            shield = Sprite("Energy Shield", ring, new Vector2(290, 440), new Vector2(410, 660), new Color(.4f, 1, 1, .6f), 29);
            decoyView = Image("cat-0", Vector2.zero, 95, 25); decoyView.name = "Decoy"; decoyView.gameObject.SetActive(false);
            targetRing = Sprite("Locked Target", ring, Vector2.zero, Vector2.one * 150, Catalog.Color("#ffcf75"), 49); targetRing.gameObject.SetActive(false);
        }
        void Fortress(int floors)
        {
            foreach (Transform child in tank) Object.Destroy(child.gameObject);
            int height = floors * 80;
            Rect("Chassis Outline", new Vector2(0, 31), new Vector2(310, 65), "#102536", 10, tank);
            Rect("Armored Chassis", new Vector2(0, 45), new Vector2(294, 51), "#ffbd3a", 11, tank);
            Rect("Chassis Highlight", new Vector2(0, 63), new Vector2(270, 8), "#ffe795", 12, tank);
            for (int i = 0; i < 4; i++)
            {
                Sprite("Wheel", disk, new Vector2(-112 + i * 73, 24), Vector2.one * 50, Catalog.Color("#152e3c"), 13, tank);
                Sprite("Hub", disk, new Vector2(-112 + i * 73, 24), Vector2.one * 29, Catalog.Color("#7e9297"), 14, tank);
                Sprite("Axle", disk, new Vector2(-112 + i * 73, 24), Vector2.one * 11, Catalog.Color("#344a54"), 15, tank);
            }
            for (int i = 0; i < floors; i++)
            {
                float y = 94 + i * 80;
                Rect("Floor " + i, new Vector2(0, y), new Vector2(248, 78), "#122c3c", 11, tank);
                Rect("Rear Armor", new Vector2(-107, y + 3), new Vector2(56, 75), "#ffbe39", 12, tank);
                Rect("Panel", new Vector2(-107, y + 9), new Vector2(35, 31), new[] {"#f19035", "#5cc571", "#f18c32", "#5eb7e7", "#ff9344"}[i], 13, tank);
                Rect("Deck", new Vector2(6, y - 35), new Vector2(260, 12), "#ffbf37", 13, tank);
                Rect("Deck Highlight", new Vector2(6, y - 31), new Vector2(250, 4), "#ffe59c", 14, tank);
                Rect("Seat", new Vector2(-28, y - 10), new Vector2(44, 22), "#465e6a", 12, tank);
                Rect("Front Strut", new Vector2(121, y - 2), new Vector2(12, 39), "#a9b9bf", 12, tank);
            }
            Rect("Command Roof", new Vector2(0, height + 57), new Vector2(270, 17), "#ffbd3a", 13, tank);
            Rect("Antenna", new Vector2(-100, height + 85), new Vector2(5, 42), "#a9b9bf", 13, tank);
            Sprite("Signal Light", disk, new Vector2(-100, height + 109), Vector2.one * 13, Catalog.Color("#79efff"), 14, tank);
            shownFloors = floors;
        }
        Transform ActorView(Actor a)
        {
            var group = new GameObject(a.role + " " + a.id).transform; group.SetParent(root.transform, false);
            if (a.def != null)
            {
                Sprite("Shadow", disk, new Vector2(0, -a.def.size * .4f), new Vector2(a.def.size * .9f, a.def.size * .12f), new Color(0, .1f, .2f, .3f), 5, group);
                Image(a.asset, Vector2.zero, a.def.size, 16, group, true).name = "Body";
                Rect("Health Back", new Vector2(0, a.def.size * .52f), new Vector2(a.def.size * .64f, 7), "#10263b", 50, group);
                Rect("Health Fill", new Vector2(0, a.def.size * .52f), new Vector2(a.def.size * .64f, 4), a.def.boss ? "#ff6a76" : "#e8b46a", 51, group);
            }
            else
            {
                if (!string.IsNullOrEmpty(a.cat))
                {
                    float width = a.role == "companion" ? 110 : a.role == "tower" ? 64 : 52;
                    Image(a.cat, new Vector2(a.role == "tower" ? -43 : -5, a.role == "tower" ? 10 : 17), width, 20, group).name = "Body";
                    if (a.role != "companion")
                    {
                        Image("uniform-" + a.index % 10, new Vector2(a.role == "tower" ? -43 : -4, 0), a.role == "tower" ? 43 : 39, 21, group);
                        Image("hat-" + a.index % 10, new Vector2(a.role == "tower" ? -43 : -4, 39), a.role == "tower" ? 48 : 43, 22, group);
                    }
                }
                if (a.role != "companion")
                {
                    var pivot = new GameObject("Weapon Pivot").transform; pivot.SetParent(group, false);
                    Image(a.weapon.asset, new Vector2(a.role == "tower" ? 20 : 13, 0), a.role == "tower" ? 117 : 57, 24, pivot, a.weapon.flip).name = "Weapon";
                }
                Rect("Health Back", new Vector2(0, a.role == "companion" ? 85 : 55), new Vector2(60, 7), "#10263b", 50, group);
                Rect("Health Fill", new Vector2(0, a.role == "companion" ? 85 : 55), new Vector2(58, 4), "#8ef587", 51, group);
            }
            actors[a.id] = group; return group;
        }
        public void Render(Battle b)
        {
            int floors = b.classic ? 5 : b.profile.FloorCount; if (shownFloors != floors) Fortress(floors);
            tank.localPosition = new Vector3(b.playerX, 175 + Mathf.Sin(b.time * 9) * .8f, 0);
            shield.gameObject.SetActive(b.Buff("shield") > 0); shield.localPosition = new Vector3(b.playerX, 440, 0);
            decoyView.gameObject.SetActive(b.decoy != null);
            if (b.decoy != null) decoyView.localPosition = b.decoy.position;
            var target = b.enemies.Find(e => !e.dead && e.id == b.targetId); targetRing.gameObject.SetActive(target != null);
            if (target != null) {targetRing.localPosition = target.position; targetRing.localScale = Vector3.one * (target.def.size * 1.1f);}
            var alive = new HashSet<int>();
            foreach (var a in b.enemies.Concat(b.units))
            {
                alive.Add(a.id); if (!actors.TryGetValue(a.id, out var t)) t = ActorView(a);
                t.gameObject.SetActive(a.visible && (!a.dead || a.def != null)); t.localPosition = new Vector3(a.position.x, a.position.y, 0);
                var body = t.Find("Body");
                if (a.def != null)
                {
                    var sr = body.GetComponent<SpriteRenderer>();
                    if (a.asset == "beast-0")
                    {
                        string pose = a.flash > 0 ? "hit" : a.position.x > b.playerX + 150 + a.def.size * .25f ? "run" : a.attack > 1.4f ? "attack" : "idle";
                        sr.sprite = Art("wolf-frames/" + pose + "-" + Mathf.FloorToInt(a.age * 10) % 6);
                        float width = a.def.size * 272 / 235; body.localScale = new Vector3(width, width, 1);
                        body.localPosition = new Vector3(0, -a.def.size * .43f + 89 * a.def.size / 235, 0);
                    }
                    else body.localEulerAngles = new Vector3(0, 0, Mathf.Sin(a.age * 6 + a.phase) * (a.def.flying ? 4 : 2));
                    Color c = a.slow > 0 || b.Buff("freeze") > 0 ? Catalog.Color("#8ad7ff") : a.poison > 0 ? Catalog.Color("#bbf78b") : a.burn > 0 ? Catalog.Color("#ffb479") : Color.white;
                    c.a = a.dead ? Mathf.Clamp01(1 - (a.age - a.deathTime) * 2) : a.burrowed ? .25f : 1; sr.color = c;
                    if (a.dead) {body.localPosition += Vector3.down * (a.age - a.deathTime) * 40; body.localEulerAngles = new Vector3(0, 0, (a.age - a.deathTime) * -50);}
                    var shadow = t.Find("Shadow"); shadow.localPosition = new Vector3(0, 175 - a.position.y, 0);
                }
                else
                {
                    var pivot = t.Find("Weapon Pivot");
                    if (pivot)
                    {
                        pivot.localEulerAngles = new Vector3(0, 0, a.angle * Mathf.Rad2Deg);
                        var weapon = pivot.Find("Weapon").GetComponent<SpriteRenderer>(); weapon.sprite = Art(a.weapon.asset); weapon.flipX = a.weapon.flip;
                        pivot.Find("Weapon").localPosition = new Vector3((a.role == "tower" ? 20 : 13) - a.recoil, 0, 0);
                    }
                }
                var fill = t.Find("Health Fill"); float full = a.def != null ? a.def.size * .64f : 58, ratio = Mathf.Clamp01(a.hp / a.maxHp);
                fill.localScale = new Vector3(Mathf.Max(.001f, full * ratio), 4, 1); fill.localPosition = new Vector3(-(1 - ratio) * full / 2, fill.localPosition.y, 0);
                fill.gameObject.SetActive(!a.dead && (!b.classic || a.def != null)); t.Find("Health Back").gameObject.SetActive(fill.gameObject.activeSelf);
            }
            foreach (var id in actors.Keys.Where(k => !alive.Contains(k)).ToArray()) {Object.Destroy(actors[id].gameObject); actors.Remove(id);}
            alive.Clear();
            foreach (var p in b.bullets)
            {
                alive.Add(p.id);
                if (!projectiles.TryGetValue(p.id, out var t))
                {
                    bool round = p.weapon.kind == "plasma" || p.weapon.kind == "poison" || p.weapon.kind == "ice" || p.weapon.kind == "mortar" || p.weapon.kind == "bomb" || p.weapon.kind == "gravity";
                    t = Sprite("Projectile " + p.id, round ? disk : square, p.position, round ? Vector2.one * 22 : new Vector2(26, 7), Catalog.Color(p.weapon.color), 35);
                    projectiles[p.id] = t;
                }
                t.localPosition = new Vector3(p.position.x, p.position.y, 0); t.localEulerAngles = new Vector3(0, 0, p.angle * Mathf.Rad2Deg);
            }
            foreach (var orb in b.orbs.Where(o => !o.collected))
            {
                alive.Add(orb.id);
                if (!projectiles.TryGetValue(orb.id, out var t)) {t = Sprite("Loot Orb", disk, orb.position, Vector2.one * 14, Catalog.Color("#ffcf75"), 40); projectiles[orb.id] = t;}
                t.localPosition = orb.position + Vector2.up * Mathf.Sin(orb.age * 4) * 7;
            }
            foreach (var id in projectiles.Keys.Where(k => !alive.Contains(k)).ToArray()) {Object.Destroy(projectiles[id].gameObject); projectiles.Remove(id);}
            spriteFxCursor = 0; lineFxCursor = 0;
            foreach (var e in b.effects)
            {
                if (e.kind == "text") continue;
                float frac = 1 - e.life / e.maxLife; Color color = e.color; color.a = Mathf.Clamp01(e.life / e.maxLife);
                if (e.kind == "beam" || e.kind == "lightning")
                {
                    var line = FxLine(); line.startColor = line.endColor = color; line.startWidth = line.endWidth = e.size;
                    int count = e.kind == "lightning" ? 12 : 2; line.positionCount = count;
                    for (int i = 0; i < count; i++) {Vector2 pos = Vector2.Lerp(e.position, e.end, i / (float)(count - 1)); if (i > 0 && i < count - 1) pos.y += Mathf.Sin(i * 39 + b.time * 13) * 18; line.SetPosition(i, pos);}
                }
                else
                {
                    float size = e.size * (1 + frac * 2);
                    FxSprite(e.kind, e.kind == "ring" || e.kind == "explosion" ? ring : disk, e.position, size, color, 46);
                    if (e.kind == "explosion" || e.kind == "impact")
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * 1.047f; var pos = e.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * e.size * frac;
                            FxSprite("Spark", disk, pos, Mathf.Max(2, 12 * (1 - frac)), color, 47);
                        }
                }
            }
            for (int i = spriteFxCursor; i < spriteFxPool.Count; i++) spriteFxPool[i].SetActive(false);
            for (int i = lineFxCursor; i < lineFxPool.Count; i++) lineFxPool[i].SetActive(false);
            Resize();
        }
        public void Resize()
        {
            camera.orthographic = true; camera.orthographicSize = Mathf.Max(450, 800 / camera.aspect); camera.transform.position = new Vector3(800, 450, -1000); camera.transform.rotation = Quaternion.identity;
        }
        public void Dispose()
        {
            Object.Destroy(root); foreach (var sprite in sprites.Values) Object.Destroy(sprite);
            Object.Destroy(disk.texture); Object.Destroy(ring.texture); Object.Destroy(disk); Object.Destroy(ring); Object.Destroy(square); Object.Destroy(lineMaterial);
        }
    }
}
