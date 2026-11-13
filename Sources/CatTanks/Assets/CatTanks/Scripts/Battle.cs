using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CatTanks
{
    public enum BattleState { Playing, Won, Lost, Retreated }
    public class Actor
    {
        public int id, index;
        public Vector2 position;
        public float hp, maxHp, age, timer, angle, recoil, defense, slow, burn, poison, stun, attack, summon = 9, flash, deathTime, phase;
        public bool dead, visible = true, burrowed, charging, warning;
        public string role, asset, cat;
        public Weapon weapon;
        public MonsterDef def;
    }
    public class Projectile
    {
        public int id, pierce;
        public Vector2 position, previous, start, end;
        public float angle, age, flight;
        public bool dead, enemy;
        public Weapon weapon;
        public Actor target;
        public HashSet<int> hit = new HashSet<int>();
    }
    public class BattleEffect
    {
        public int id;
        public string kind, text, visual;
        public Vector2 position, end;
        public Color color;
        public float size, life, maxLife;
    }
    public class LootOrb { public int id, coins; public Vector2 position; public float age; public bool collected; }
    public class Opponent
    {
        public string name; public int rating, level;
        public static readonly Opponent[] All = {
            new Opponent {name="Bánh Bao",rating=950,level=4}, new Opponent {name="Milo",rating=1000,level=7},
            new Opponent {name="Mực Ống",rating=1060,level=12}, new Opponent {name="Gấu Béo",rating=1120,level=18},
            new Opponent {name="Lúa Mạch",rating=1190,level=25}, new Opponent {name="Đậu Đen",rating=1280,level=40}};
    }
    public sealed class Battle
    {
        int nextEffectId=1;
        public readonly Catalog db;
        public readonly Profile profile;
        public readonly bool classic, arena;
        public readonly Stage stage;
        public readonly Opponent opponent;
        public List<Actor> enemies = new List<Actor>(), units = new List<Actor>();
        public List<Projectile> bullets = new List<Projectile>();
        public List<BattleEffect> effects = new List<BattleEffect>();
        public List<string> caught = new List<string>();
        public List<LootOrb> orbs = new List<LootOrb>();
        public Actor decoy;
        public HashSet<string> seen = new HashSet<string>();
        public List<SkillDef> skills = new List<SkillDef>();
        public Dictionary<string, float> cooldowns = new Dictionary<string, float>(), buffs = new Dictionary<string, float>();
        public List<Wave> waves;
        public BattleState state = BattleState.Playing;
        public float time, hp, maxHp, energy, energyMax = 600, playerX = 290, move, spawnTimer = .8f, waveGap, droneTimer;
        float energyRegen = 16, enemyEnergy = 200, enemyClock, enemyDroneTimer, enemyDecoyHp;
        Dictionary<string, float> enemyCooldowns = new Dictionary<string, float>(), enemyBuffs = new Dictionary<string, float>();
        public int wave, spawned, kills, coins, targetId = -1, nextId = 1, bank = -1, throwsUsed;
        int[] burstCounts;
        Dictionary<string, int> finishedRewards;
        public int cages, moonCages, cageUsed, moonUsed;
        public string message;
        public Action<Weapon> onFire;
        public Battle(Catalog catalog, Profile save, int stageIndex = 1, bool isClassic = false, bool isArena = false, int opponentIndex = 0)
        {
            db = catalog; profile = save; classic = isClassic; arena = isArena;
            stage = db.stages[Mathf.Clamp(stageIndex - 1, 0, 19)]; waves = classic ? db.waves : stage.waves;
            if (arena)
            {
                opponent = Opponent.All[Mathf.Clamp(opponentIndex, 0, Opponent.All.Length - 1)];
                waves = JsonConvert.DeserializeObject<List<Wave>>(JsonConvert.SerializeObject(waves));
                float factor = Mathf.Max(.4f, Mathf.Pow(10, (opponent.rating - save.rating) / 800f));
                foreach (var w in waves)
                {
                    w.enemyHpMult *= factor; w.enemyAtkMult *= factor; w.boss = false;
                    w.types = new Newtonsoft.Json.Linq.JArray(w.types.Where(t => (string)t != "beast-8"));
                }
            }
            maxHp = classic ? 48000 : save.MaxHp; hp = maxHp; energy = classic ? 600 : 200;
            moonCages = classic || arena ? 0 : Mathf.Min(5, profile.Get("moonCage")); cages = classic || arena ? 0 : Mathf.Min(5 - moonCages, profile.Get("beastCage"));
            if (classic)
            {
                for (int i = 0; i < 5; i++) AddUnit("tower", i, "cat-" + i, db.Weapon("turret-" + i).Copy(), 1000);
                for (int i = 0; i < 10; i++) AddUnit("infantry", i, "cat-" + i, db.Weapon("gun-" + i).Copy(), 1000);
                for (int i = 0; i < 4; i++) AddUnit("aux", i, "", db.Weapon("pack-" + i).Copy(), 1000);
                skills = db.classicSkills.Select(s => s.Copy()).ToList(); AssignBanks();
            }
            else
            {
                for (int i = 0; i < save.FloorCount; i++)
                {
                    var f = save.floors[i]; var turret = save.turrets.Find(t => t.id == f.turret); var cat = save.cats.Find(c => c.id == f.cat);
                    var w = db.Weapon(turret.id).Copy(); var stats = cat == null ? null : save.Stats(cat, db);
                    float damage = w.damage * (1 + .08f * (turret.level - 1)) * (1 + .14f * turret.stars);
                    w.damage = stats == null ? damage * .7f : (damage * (1 + stats["atk"] / 50 * .5f) + stats["atk"] * .15f) * (db.Cat(cat.id).specialty == "operator" ? 1.15f : 1);
                    if (stats != null) w.rate = Mathf.Max(.15f, w.rate / (1 + Mathf.Min(.3f, stats["speed"] / 100 * .25f)));
                    var u = AddUnit("tower", i, cat?.id, w, stats == null ? 240 : save.Stats(cat, db, "operator")["maxHp"]); u.defense = stats == null ? 0 : stats["def"];
                    foreach (var id in turret.modules) if (!string.IsNullOrEmpty(id)) AddUnit("aux", units.Count(a => a.role == "aux"), "", db.Weapon(id).Copy(), 300);
                }
                for (int i = 0; i < 5; i++)
                {
                    var c = i < save.SquadCount ? save.cats.Find(v => v.id == save.squad[i]) : null;
                    var stats = c == null ? null : save.Stats(c, db, "infantry");
                    string gun = c == null ? "gun-0" : c.id.Replace("cat-", "gun-");
                    if (c != null && c.gear.TryGetValue("weapon", out var uid)) gun = save.gear.Find(g => g.uid == uid)?.id ?? gun;
                    var w = db.Weapon(gun).Copy(); w.damage *= stats == null ? .75f : 1 + stats["atk"] / 50;
                    if (stats != null) w.rate = Mathf.Max(.15f, w.rate / (1 + stats["speed"] / 500));
                    var u = AddUnit("infantry", i, c?.id ?? "cat-0", w, stats == null ? 240 : stats["maxHp"]); u.defense = stats == null ? 0 : stats["def"];
                }
                for (int i = 0; i < save.PetCount; i++)
                {
                    var pet = save.pets.Find(p => p.id == save.petSlots[i]); if (pet == null) continue;
                    var entry = db.monsterBook.First(e => (string)e["speciesId"] == pet.id);
                    var w = entry["weapon"].ToObject<Weapon>(); w.asset = pet.id;
                    var stats = save.PetStats(pet, db); w.damage = stats["atk"]; w.rate = stats["rate"];
                    AddUnit("companion", i, pet.id, w, stats["maxHp"]).defense = stats["def"];
                    if (entry["passive"] is Newtonsoft.Json.Linq.JObject passive)
                    {
                        if (passive["maxHpPct"] != null) maxHp += save.MaxHp * (float)passive["maxHpPct"];
                        if (passive["energyRegen"] != null) energyRegen += (float)passive["energyRegen"];
                    }
                }
                maxHp = Mathf.Floor(Mathf.Min(maxHp, save.MaxHp * 1.08f)); hp = maxHp; energyRegen = Mathf.Min(16.8f, energyRegen);
                foreach (var id in save.skillSlots.Where(id => !string.IsNullOrEmpty(id)).Distinct())
                {
                    var s = db.skills.Find(v => v.id == id); if (s == null || !save.SkillUnlocked(s)) continue;
                    var copy = s.Copy(); int level = save.skillLevels.TryGetValue(id, out int n) ? n : 1;
                    copy.cost = Mathf.FloorToInt(copy.baseCost * Mathf.Max(.55f, 1 - .06f * (level - 1)));
                    copy.cooldown = Mathf.Max(5, copy.baseCd - .25f * (level - 1));
                    copy.damage *= 1 + .1f * (level - 1); copy.healVehicle *= 1 + .1f * (level - 1); copy.rocketDamage *= 1 + .1f * (level - 1); skills.Add(copy);
                }
            }
        }
        Actor AddUnit(string role, int index, string asset, Weapon w, float health)
        {
            var u = new Actor {id = nextId++, role = role, index = index, asset = asset, cat = asset, weapon = w, hp = health, maxHp = health, timer = .15f + index * .24f, position = new Vector2(110 + index * 35, 196)};
            units.Add(u); return u;
        }
        public Actor Spawn(string species, float x = -1)
        {
            var d = db.Monster(species); var w = waves[wave];
            float health = d.hp * (classic ? 1 : d.boss ? w.bossHpMult : w.enemyHpMult);
            var e = new Actor {id = nextId++, asset = species, def = d, role = "enemy", position = new Vector2(x < 0 ? 1590 + UnityEngine.Random.value * 100 : x, 175 + d.size * .43f + (d.flying ? 200 : 0)), hp = health, maxHp = health, phase = UnityEngine.Random.value * 6.28f, attack = 1.6f + UnityEngine.Random.value * 2};
            enemies.Add(e); seen.Add(species); return e;
        }
        void AssignBanks()
        {
            int b = Mathf.FloorToInt(time / 10) % 10; if (b == bank) return; bank = b;
            foreach (var u in units)
                u.weapon = db.Weapon(u.role == "tower" ? "turret-" + (u.index + bank * 5) % 10 : u.role == "infantry" ? "gun-" + u.index : "pack-" + (bank * 4 + u.index) % 20).Copy();
        }
        public Actor Target(Actor unit)
        {
            var candidates = enemies.Where(e => !e.dead && e.position.x > -100 && e.position.x < 1720 && Vector2.Distance(e.position, unit.position) < unit.weapon.range);
            var locked = candidates.FirstOrDefault(e => e.id == targetId); if (locked != null) return locked;
            if (unit.weapon.kind == "sniper") return candidates.OrderByDescending(e => (e.def.boss ? 900 : 0) + (e.def.armor > 0 ? 200 : 0)).ThenBy(e => e.hp).FirstOrDefault();
            return candidates.OrderBy(e => Vector2.Distance(e.position, unit.position)).FirstOrDefault();
        }
        public void Step(float dt)
        {
            if (state != BattleState.Playing) return;
            dt = Mathf.Min(dt, .05f); time += dt;
            if (classic) AssignBanks();
            energy = Mathf.Min(energyMax, energy + dt * (classic ? 9 : energyRegen));
            Tick(cooldowns, dt); Tick(buffs, dt);
            if (Buff("poison") > 0) HurtPlayer(dt * 52);
            playerX = Mathf.Clamp(playerX + move * 110 * dt * (Buff("slow") > 0 ? .42f : 1), 175, 550);
            UpdateWave(dt);
            if (arena) UpdateEnemyAI(dt);
            foreach (var u in units)
            {
                if (u.dead) {u.visible = false; continue;}
                u.stun = Mathf.Max(0, u.stun - dt); u.recoil = Mathf.Max(0, u.recoil - dt * 55);
                if (u.role == "tower") {u.position = new Vector2(playerX + 35, 258 + u.index * 80); u.visible = true;}
                else if (u.role == "aux") {u.position = new Vector2(playerX - 88 + u.index % 2 * 172, 232 + u.index / 2 * 214); u.visible = true;}
                else if (u.role == "companion") u.position = Vector2.Lerp(u.position, new Vector2(playerX - 70 + u.index * 60, 200 + (db.Monster(u.asset).flying ? 160 : 0)), dt * 2.8f);
                else
                {
                    u.visible = !classic || u.index / 5 == Mathf.FloorToInt(time / 18) % 2;
                    u.position = new Vector2(Mathf.Lerp(u.position.x, playerX - 180 + u.index % 5 * 88, dt * 2.8f), 194 + Mathf.Abs(Mathf.Sin(time * 9 + u.index * 2)) * 5);
                }
                if (u.stun > 0 || !u.visible) continue;
                u.timer -= dt * (Buff("rally") > 0 ? 1.8f : 1) * (Buff("frost") > 0 ? .65f : 1) * (EnemyBuff("freeze") > 0 ? .25f : 1);
                var target = Target(u);
                if (target != null) u.angle = Mathf.Atan2(target.position.y - u.position.y, target.position.x - u.position.x);
                if (u.timer > 0) continue;
                if (u.weapon.kind == "heal") {hp = Mathf.Min(maxHp, hp + u.weapon.damage); energy = Mathf.Min(600, energy + 15); u.timer = u.weapon.rate; continue;}
                if (u.weapon.kind == "shield") {buffs["shield"] = Mathf.Max(Buff("shield"), 2); u.timer = u.weapon.rate; continue;}
                if (target != null) {Fire(u, target); u.timer = u.weapon.rate; u.recoil = u.role == "tower" ? 10 : 5;}
            }
            if (Buff("drone") > 0 && (droneTimer -= dt) <= 0)
            {
                var u = new Actor {position = new Vector2(playerX + 190, 650), weapon = db.Weapon("gun-9"), role = "drone"}; var t = Target(u); if (t != null) Fire(u, t); droneTimer = .3f;
            }
            for (int i = 0; i < enemies.Count; i++) UpdateEnemy(enemies[i], dt);
            for (int i = 0; i < bullets.Count; i++) UpdateBullet(bullets[i], dt);
            if (decoy != null && (decoy.hp <= 0 || time > decoy.age)) decoy = null;
            foreach (var orb in orbs) {orb.age += dt; if (Mathf.Abs(orb.position.x - playerX) < 150) CollectOrb(orb);}
            orbs.RemoveAll(o => o.collected);
            enemies.RemoveAll(e => e.dead && e.age - e.deathTime > .6f); bullets.RemoveAll(b => b.dead);
            foreach (var fx in effects) fx.life -= dt;
            effects.RemoveAll(fx => fx.life <= 0);
            if (hp <= 0) state = BattleState.Lost;
        }
        static void Tick(Dictionary<string, float> dict, float dt) {foreach (string k in dict.Keys.ToArray()) dict[k] = Mathf.Max(0, dict[k] - dt);}
        public float Buff(string id) => buffs.TryGetValue(id, out float n) ? n : 0;
        public float Cooldown(string id) => cooldowns.TryGetValue(id, out float n) ? n : 0;
        float EnemyBuff(string id) => enemyBuffs.TryGetValue(id, out float n) ? n : 0;
        public static int RatingDelta(int mine, int theirs, bool won)
        {
            float expected = 1 / (1 + Mathf.Pow(10, (theirs - mine) / 400f));
            return Mathf.FloorToInt(32 * ((won ? 1 : 0) - expected) + .5f);
        }
        void HurtUnit(Actor unit, float damage)
        {
            if (unit.dead) return;
            unit.hp = Mathf.Max(0, unit.hp - damage * 80 / (80 + unit.defense) * (Buff("shield") > 0 ? .06f : 1)); unit.dead = unit.hp <= 0;
        }
        void UpdateEnemyAI(float dt)
        {
            enemyEnergy = Mathf.Min(600, enemyEnergy + dt * 16); enemyClock += dt;
            Tick(enemyCooldowns, dt); Tick(enemyBuffs, dt);
            if (EnemyBuff("drone") > 0 && (enemyDroneTimer -= dt) <= 0) {EnemyHit(new Vector2(playerX + 50, 250), 35); enemyDroneTimer = .3f;}
            if (enemyClock < .5f) return; enemyClock = 0;
            var live = units.Where(u => !u.dead).ToArray(); var liveEnemies = enemies.Where(e => !e.dead).ToArray();
            int near = live.Count(u => u.position.x < 690);
            float health = liveEnemies.Sum(e => e.hp), maximum = Mathf.Max(1, liveEnemies.Sum(e => e.maxHp));
            var conditions = new Dictionary<string, bool> {
                ["heal"] = health < maximum * .5f, ["shield"] = health < maximum * .35f || near >= 3,
                ["decoy"] = near >= 2, ["orbital"] = live.Any(u => u.hp / u.maxHp > .4f),
                ["barrage"] = live.Length >= 3, ["freeze"] = live.Length >= 4, ["kamikaze"] = live.Length >= 3,
                ["rally"] = live.Length >= 3 && health >= maximum * .5f, ["drone"] = true, ["magnet"] = false
            };
            foreach (string id in new[] {"heal", "shield", "decoy", "orbital", "barrage", "freeze", "kamikaze", "rally", "drone", "magnet"})
            {
                var s = skills.Find(v => v.id == id);
                if (s == null || !conditions[id] || enemyCooldowns.TryGetValue(id, out float cd) && cd > 0 || enemyEnergy < s.cost + (id == "drone" ? 40 : 0)) continue;
                enemyEnergy -= s.cost; enemyCooldowns[id] = s.cooldown;
                if (id == "heal") foreach (var e in liveEnemies) e.hp = Mathf.Min(e.maxHp, e.hp + e.maxHp * .4f);
                if (id == "shield" || id == "freeze" || id == "rally" || id == "drone") enemyBuffs[id] = s.duration;
                if (id == "barrage") foreach (var u in live) HurtUnit(u, s.rocketDamage * .3f);
                if (id == "decoy") enemyDecoyHp = 800;
                if (id == "orbital") EnemyHit(new Vector2(playerX + 50, 250), s.damage);
                if (id == "kamikaze") foreach (var u in live) HurtUnit(u, s.damage * .3f);
                message = opponent.name + ": " + s.name; break;
            }
        }
        void UpdateWave(float dt)
        {
            var w = waves[wave]; spawnTimer -= dt;
            if (classic)
            {
                if (spawned < w.count && spawnTimer <= 0)
                {
                    var d = db.monsters[(int)w.types[spawned % w.types.Count]];
                    Spawn(d.asset, spawned % 3 == 2 && wave < 5 ? 650 + UnityEngine.Random.value * 95 : -1); spawned++; spawnTimer = w.spacing;
                }
            }
            else
            {
                if (burstCounts == null) burstCounts = new int[w.bursts.Count];
                for (int i = 0; i < w.bursts.Count; i++)
                {
                    var b = w.bursts[i]; if (burstCounts[i] >= b.count || time < b.after) continue;
                    if (spawnTimer <= 0)
                    {
                        string species = (string)w.types[spawned % w.types.Count];
                        if (w.boss && spawned == w.count - 1) species = "beast-8";
                        else if (species == "beast-8") species = (string)w.types.First(t => (string)t != "beast-8");
                        Spawn(species); spawned++; burstCounts[i]++; spawnTimer = b.spacing;
                    }
                    break;
                }
            }
            if (spawned < w.count || enemies.Any(e => !e.dead)) return;
            if (waveGap <= 0) {waveGap = 4; energy = Mathf.Min(600, energy + 90); hp = Mathf.Min(maxHp, hp + (classic ? 1600 : 0));}
            waveGap -= dt;
            if (waveGap > 0) return;
            if (wave == waves.Count - 1) {foreach (var orb in orbs) CollectOrb(orb); state = BattleState.Won; return;}
            wave++; spawned = 0; spawnTimer = 1; burstCounts = null; waveGap = 0;
        }
        void UpdateEnemy(Actor e, float dt)
        {
            e.age += dt; if (e.dead) return;
            e.flash = Mathf.Max(0, e.flash - dt); e.attack -= dt; e.summon -= dt;
            e.slow = Mathf.Max(0, e.slow - dt); e.burn = Mathf.Max(0, e.burn - dt); e.poison = Mathf.Max(0, e.poison - dt); e.stun = Mathf.Max(0, e.stun - dt);
            if (e.burn > 0) Damage(e, 24 * dt, "flame"); if (e.poison > 0) Damage(e, 30 * dt, "poison"); if (e.dead) return;
            var d = e.def; float ratio = e.hp / e.maxHp;
            e.burrowed = d.behavior == "burrow" && e.age % 7 < 2.4f;
            e.charging = (d.behavior == "charge" || d.behavior == "rhino") && e.age % 7 > 5.3f;
            e.warning = (d.behavior == "charge" || d.behavior == "rhino") && e.age % 7 > 4 && !e.charging;
            float leap = d.behavior == "leap" ? Mathf.Max(0, Mathf.Sin(e.age * 2.3f)) * 95 : 0;
            float y = 175 + d.size * .43f + (d.flying ? 190 + Mathf.Sin(e.age * 2 + e.phase) * 58 : leap);
            if (d.behavior == "bee") y = 265 + Mathf.Sin(e.age * 1.4f + e.phase) * 140;
            float speed = d.speed * (e.charging ? 5 : 1);
            if (d.behavior == "tiger" && e.age % 5 > 3.8f) speed *= 3.5f;
            if (d.behavior == "wolf" && enemies.Any(o => o != e && !o.dead && Vector2.Distance(o.position, e.position) < 180)) speed *= 1.45f;
            if (d.behavior == "enrage" || d.behavior == "boss") speed *= 1 + (1 - ratio) * 1.5f;
            if (e.slow > 0 || Buff("freeze") > 0) speed *= .35f; if (e.stun > 0) speed = 0;
            float stop = playerX + (d.range > 0 ? d.range : 150) + d.size * .25f;
            e.position = new Vector2(e.position.x - (e.position.x > stop ? speed * dt : 0), y);
            if (e.attack <= 0 && e.position.x < stop + 25 && e.stun <= 0)
            {
                e.attack = d.flying ? 2.4f : 1.8f;
                float damage = d.damage * (classic ? 1 : waves[wave].enemyAtkMult);
                if (d.range > 0 || d.flying)
                {
                    int count = d.behavior == "moth" || d.behavior == "griffin" || d.boss ? 3 : 1;
                    for (int i = 0; i < count; i++)
                    {
                        float a = Mathf.Atan2(300 + UnityEngine.Random.value * 280 - e.position.y, playerX - e.position.x) + (i - (count - 1) / 2f) * .14f;
                        string kind = d.behavior == "frost" ? "ice" : d.behavior == "venom" || d.behavior == "queen" || d.behavior == "moth" ? "poison" : d.behavior == "web" ? "web" : d.boss ? "flame" : "enemy";
                        MakeBullet(e.position, a, new Weapon {kind = kind, color = d.color, damage = damage, speed = 280, range = 1900}, null, true);
                    }
                }
                else EnemyHit(e.position, damage * (e.charging ? 2 : 1) * (d.behavior == "enrage" ? 2 - ratio : 1));
                if (d.behavior == "lava" && e.position.x < playerX + 350) HurtPlayer(120 * (classic ? 1 : waves[wave].enemyAtkMult));
            }
            if (d.behavior == "healer" && e.summon <= 0)
            {
                foreach (var o in enemies) if (!o.dead && Vector2.Distance(o.position, e.position) < 460) o.hp = Mathf.Min(o.maxHp, o.hp + 180);
                e.summon = 3; Effect("ring", e.position, e.position, "#99ffc0", 150, .6f);
            }
            if ((d.behavior == "queen" || d.boss) && e.summon <= 0)
            {
                Spawn("bug-0", e.position.x - 100); Spawn(d.boss && ratio < .35f ? "bug-4" : "bug-0", e.position.x + 30);
                e.summon = d.boss && ratio < .35f ? 6 : 11;
            }
        }
        public void Fire(Actor u, Actor target)
        {
            var w = u.weapon; onFire?.Invoke(w);
            if (w.kind == "laser" || w.kind == "sniper")
            {
                Vector2 dir = (target.position - u.position).normalized;
                var line = enemies.Where(e => !e.dead && Vector2.Dot(e.position - u.position, dir) > 0 && Mathf.Abs((e.position.x - u.position.x) * dir.y - (e.position.y - u.position.y) * dir.x) < e.def.size * .38f).OrderBy(e => Vector2.Distance(e.position, u.position)).Take(w.pierce).ToArray();
                foreach (var e in line) Damage(e, w.damage, w.kind);
                Effect("beam", u.position, u.position + dir * 1500, w.color, w.kind == "laser" ? 12 : 4, .18f); return;
            }
            if (w.kind == "tesla")
            {
                Actor current = target; Vector2 origin = u.position; var hit = new HashSet<int>();
                for (int i = 0; i < w.chain && current != null; i++)
                {
                    Effect("lightning", origin, current.position, w.color, 5, .22f); Damage(current, w.damage * (1 - i * .14f), "tesla"); current.stun = w.stun > 0 ? w.stun : .4f; hit.Add(current.id); origin = current.position;
                    current = enemies.Where(e => !e.dead && !hit.Contains(e.id) && Vector2.Distance(e.position, origin) < 350).OrderBy(e => Vector2.Distance(e.position, origin)).FirstOrDefault();
                }
                return;
            }
            int count = w.kind == "double" ? 2 : Mathf.Max(1, w.count);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Atan2(target.position.y - u.position.y, target.position.x - u.position.x);
                if (w.kind == "shotgun" || w.kind == "crystal" || w.kind == "claw") angle += (i - (count - 1) / 2f) * .09f;
                else if (w.kind == "rocket") angle += (i - (count - 1) / 2f) * .3f;
                else angle += (UnityEngine.Random.value - .5f) * .035f;
                MakeBullet(u.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (u.role == "tower" ? 70 : 40), angle, w, target);
            }
            Effect("flash", u.position + new Vector2(Mathf.Cos(u.angle), Mathf.Sin(u.angle)) * 55, u.position, w.color, 24, .1f);
        }
        public void MakeBullet(Vector2 pos, float angle, Weapon w, Actor target, bool enemy = false)
        {
            bullets.Add(new Projectile {id = nextId++, position = pos, previous = pos, start = pos, end = target?.position ?? pos, angle = angle, weapon = w, target = target, enemy = enemy, pierce = Mathf.Max(1, w.pierce), flight = target == null ? 1 : Mathf.Max(.5f, Vector2.Distance(pos, target.position) / w.speed)});
        }
        void UpdateBullet(Projectile b, float dt)
        {
            if (b.dead) return; b.age += dt; b.previous = b.position; var w = b.weapon;
            if ((w.kind == "rocket" || w.kind == "drone") && b.target != null && !b.target.dead)
            {
                float desired = Mathf.Atan2(b.target.position.y - b.position.y, b.target.position.x - b.position.x);
                b.angle += Mathf.Clamp(Mathf.DeltaAngle(b.angle * Mathf.Rad2Deg, desired * Mathf.Rad2Deg) * Mathf.Deg2Rad, -dt * 3.5f, dt * 3.5f);
            }
            b.position += new Vector2(Mathf.Cos(b.angle), Mathf.Sin(b.angle)) * w.speed * dt;
            if (w.kind == "mortar" || w.kind == "bomb")
            {
                float t = Mathf.Min(1, b.age / b.flight); b.position = Vector2.Lerp(b.start, b.end, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 220;
                if (t >= 1) Impact(b, b.target);
            }
            if (b.enemy)
            {
                if (b.position.x < playerX + 115 && b.position.x > playerX - 145 && b.position.y > 175 && b.position.y < 700)
                {
                    EnemyHit(b.position, w.damage); if (w.kind == "poison") buffs["poison"] = 4; if (w.kind == "web") buffs["slow"] = 4; if (w.kind == "ice") buffs["frost"] = 4;
                    b.dead = true; Effect("impact", b.position, b.position, w.color, 35, .3f, visual:ImpactVisual(w));
                }
            }
            else if (!b.dead && w.kind != "mortar" && w.kind != "bomb")
            {
                foreach (var e in enemies)
                {
                    if (e.dead || b.hit.Contains(e.id)) continue;
                    Vector2 delta = b.position - b.previous; float q = Mathf.Clamp01(Vector2.Dot(e.position - b.previous, delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                    if (Vector2.Distance(e.position, b.previous + delta * q) < e.def.size * .36f + 7) {Impact(b, e); if (b.dead) break;}
                }
            }
            if (b.age > 6 || Vector2.Distance(b.position, b.start) > w.range || b.position.x < -100 || b.position.x > 1850 || b.position.y < 120 || b.position.y > 1200) b.dead = true;
        }
        void Impact(Projectile b, Actor e)
        {
            var w = b.weapon;
            if (e != null && !e.dead) {b.hit.Add(e.id); Damage(e, w.damage, w.kind); e.slow = Mathf.Max(e.slow, w.slow); e.burn = Mathf.Max(e.burn, w.burn); e.poison = Mathf.Max(e.poison, w.poison);}
            if (w.splash > 0)
            {
                foreach (var o in enemies)
                {
                    if (o.dead || o == e || Vector2.Distance(o.position, b.position) > w.splash + o.def.size * .2f) continue;
                    Damage(o, w.damage * .6f, w.kind); o.slow = Mathf.Max(o.slow, w.slow); o.poison = Mathf.Max(o.poison, w.poison);
                    if (w.kind == "gravity") o.position = Vector2.Lerp(o.position, b.position, .22f);
                }
                b.dead = true;
            }
            if (--b.pierce <= 0) b.dead = true;
            Effect("impact", b.position, b.position, w.color, w.splash > 0 ? Mathf.Min(100, w.splash * .55f) : w.kind == "flame" ? 26 : 15, .32f, visual:ImpactVisual(w));
        }
        static string ImpactVisual(Weapon w)=>w.kind=="flame"?"fire":w.kind=="ice"?"ice":w.kind=="poison"?"poison":w.splash>0?"blast":"impact";
        public void Damage(Actor e, float amount, string kind)
        {
            if (e.dead) return; float defense = e.def.armor;
            if (new[] {"sniper", "drill", "laser", "poison", "arrow"}.Contains(kind)) defense *= .2f;
            if (e.def.behavior == "crystal" && new[] {"laser", "plasma", "tesla", "ice", "gravity"}.Contains(kind)) defense = .58f;
            if (e.def.behavior == "frost" && kind == "ice") defense = .8f;
            if (e.burrowed) defense = .9f; if (e.def.behavior == "tiger" && e.age % 5 > 3.8f) defense = Mathf.Max(defense, .55f);
            float absorbed = Mathf.Min(enemyDecoyHp, amount); enemyDecoyHp -= absorbed; amount -= absorbed;
            float dealt = amount * (1 - defense) * (EnemyBuff("shield") > 0 ? .5f : 1); e.hp -= dealt; e.flash = .1f;
            if (amount >= 12) Effect("text", e.position + Vector2.up * e.def.size * .3f, Vector2.zero, "#ffe7a2", 18, .65f, Mathf.RoundToInt(dealt).ToString());
            if (e.hp > 0) return;
            e.dead = true; e.deathTime = e.age; kills++;
            if (classic) coins += Mathf.RoundToInt(e.maxHp / 15);
            else orbs.Add(new LootOrb {id = nextId++, coins = e.def.boss ? 10 : 1, position = new Vector2(e.position.x, 200)});
            energy = Mathf.Min(600, energy + (classic ? 6 : 12)); Effect("explosion", e.position, e.position, e.def.color, e.def.size * .35f, .6f);
            if (targetId == e.id) targetId = -1;
        }
        void HurtPlayer(float amount) {if (state == BattleState.Playing) hp = Mathf.Max(0, hp - amount * (Buff("shield") > 0 ? .06f : 1));}
        void EnemyHit(Vector2 position, float amount)
        {
            amount *= EnemyBuff("rally") > 0 ? 1.3f : 1;
            if (decoy != null && Vector2.Distance(position, decoy.position) < 420) {decoy.hp -= amount; return;}
            if (!classic)
            {
                var u = units.Where(a => !a.dead && a.visible && a.role != "aux" && Vector2.Distance(a.position, new Vector2(Mathf.Min(position.x, playerX + 80), Mathf.Min(position.y, 650))) < 140).OrderBy(a => a.role == "tower" ? 1 : 0).ThenBy(a => Vector2.Distance(a.position, position)).FirstOrDefault();
                if (u != null)
                {
                    u.hp = Mathf.Max(0, u.hp - amount * 80 / (80 + u.defense) * (Buff("shield") > 0 ? .06f : 1)); u.dead = u.hp <= 0; u.flash = .1f;
                    if (amount > 100) {u.position += Vector2.left * (u.role == "infantry" ? 78 : 130); u.stun = .6f;}
                    return;
                }
            }
            HurtPlayer(amount);
        }
        public bool UseSkill(string id)
        {
            var s = skills.Find(v => v.id == id);
            if (state != BattleState.Playing || s == null) return false;
            if (Cooldown(id) > 0) {message = "Kỹ năng đang hồi chiêu"; return false;}
            if (energy < s.cost) {message = "Chưa đủ năng lượng"; return false;}
            energy -= s.cost; cooldowns[id] = s.cooldown; if (s.duration > 0) buffs[id] = s.duration;
            if (id == "heal") {hp = Mathf.Min(maxHp, hp + (classic ? 9000 : s.healVehicle)); buffs["poison"] = 0; foreach (var u in units) if (!u.dead) u.hp = Mathf.Min(u.maxHp, u.hp + u.maxHp * .4f);}
            if (id == "freeze") foreach (var e in enemies.Where(e => !e.dead).ToArray()) {e.slow = 6; Damage(e, classic ? 120 : s.damage, "ice");Effect("impact",e.position,e.position,"#99efff",e.def.size*.4f,.65f,visual:"ice");}
            if (id == "barrage")
            {
                var targets = enemies.Where(e => !e.dead).ToArray();
                for (int i = 0; i < (classic ? 18 : s.rockets) && targets.Length > 0; i++)
                {
                    var w = db.Weapon("turret-2").Copy(); w.damage = classic ? 190 : s.rocketDamage; w.splash = 145; w.speed = 650;
                    MakeBullet(new Vector2(playerX + 80 + i * 40, 950 + i * 18), -Mathf.PI / 2, w, targets[i % targets.Length]);
                }
            }
            if (id == "orbital" || id == "kamikaze")
            {
                var target = enemies.Find(e => !e.dead && e.id == targetId) ?? enemies.Where(e => !e.dead).OrderByDescending(e => e.hp).FirstOrDefault();
                if (target != null)
                {
                    if (id == "orbital") {Damage(target, s.damage * (target.def.boss ? s.bossMult : 1), "laser"); Effect("beam", new Vector2(target.position.x, 1000), target.position, "#87f7ff", 35, .5f);}
                    else foreach (var e in enemies.Where(e => !e.dead && Vector2.Distance(e.position, target.position) < s.splash + e.def.size * .2f).ToArray()) Damage(e, s.damage, "bomb");
                    Effect("impact",target.position,target.position,"#ffcf75",90,.65f,visual:"blast");
                }
            }
            if (id == "decoy") decoy = new Actor {position = new Vector2(playerX + 150, 220), hp = 800, maxHp = 800, age = time + s.duration};
            if (id == "magnet") foreach (var orb in orbs) CollectOrb(orb);
            if(id!="freeze")Effect("ring", new Vector2(playerX, 350), Vector2.zero, id == "heal" ? "#99ffc0" : "#69efff", 240, .7f, visual:id=="heal"?"heal":id=="shield"?"shield":"buff"); message = s.name; return true;
        }
        public bool Catch(bool moon)
        {
            if (classic || arena || state != BattleState.Playing) return false;
            var e = enemies.Find(v => v.id == targetId && !v.dead);
            if (e == null) {message = "Chọn mục tiêu trước khi ném lồng"; return false;}
            if (throwsUsed >= 5 || (moon ? moonCages : cages) <= 0) {message = "Hết lồng đã mang"; return false;}
            if (e.def.boss && !moon) {message = "Cần Lồng trăng"; return false;}
            float ratio = e.hp / e.maxHp, threshold = e.def.boss ? .1f : .25f;
            if (ratio > threshold || e.def.boss && ratio >= threshold) {message = e.def.boss ? "Trùm còn trên 10% máu" : "Cần giảm máu mục tiêu xuống 25%"; return false;}
            var entry = db.monsterBook.First(x => (string)x["speciesId"] == e.asset); string rarity = (string)entry["catchRarity"];
            float chance = e.def.boss ? .02f : db.B("monsters.capture.baseChance." + (moon ? "moonCage" : "beastCage") + "." + rarity);
            chance *= .45f + .55f * (threshold - ratio) / threshold;
            throwsUsed++; if (moon) {moonCages--; moonUsed++; profile.Add("moonCage", -1);} else {cages--; cageUsed++; profile.Add("beastCage", -1);} profile.Save();
            if (UnityEngine.Random.value < Mathf.Min(.9f, chance))
            {
                caught.Add(e.asset); e.dead = true; e.deathTime = e.age; kills++; targetId = -1; message = "Đã bắt tạm " + e.def.name + " · Thắng trận để nhận!"; Effect("ring", e.position, Vector2.zero, "#ffce75", e.def.size, .8f, visual:"capture"); return true;
            }
            e.stun = .4f; message = "Thoát rồi!"; return false;
        }
        public void Effect(string kind, Vector2 pos, Vector2 end, string color, float size, float life, string text = null, string visual = null)
        {
            if (effects.Count >= 350) effects.RemoveAt(0);
            effects.Add(new BattleEffect {id=nextEffectId++,kind = kind, position = pos, end = end, color = Catalog.Color(color), size = size, life = life, maxLife = life, text = text,visual=visual});
        }
        void CollectOrb(LootOrb orb)
        {
            if (orb.collected) return; orb.collected = true; coins += orb.coins;
            Effect("text", orb.position, Vector2.zero, "#ffcf75", 18, .8f, "+" + orb.coins + " xu");
        }
        public Dictionary<string, int> Finish()
        {
            if (finishedRewards != null) return finishedRewards;
            var reward = new Dictionary<string, int>();
            foreach (var id in seen) if (!profile.seen.Contains(id)) profile.seen.Add(id);
            if (state == BattleState.Won) foreach (var id in caught) profile.GainPet(id, db);
            if (arena)
            {
                bool won = state == BattleState.Won;
                profile.rating = Mathf.Max(0, profile.rating + RatingDelta(profile.rating, opponent.rating, won));
                int cap = profile.daily.TryGetValue("arenaTokens", out int n) ? n : 0, earned = Mathf.Min(40 - cap, won ? 8 : 2);
                reward["arenaToken"] = earned; reward["coins"] = won ? 200 : 0; reward["spiritDust"] = won ? 80 : 0; reward["accountXp"] = won ? 25 : 0;
                profile.daily["arenaTokens"] = cap + earned; profile.Track("pvp");
            }
            else if (classic) {if (state == BattleState.Won) profile.classicCleared = true;}
            else
            {
                reward["coins"] = coins;
                if (state == BattleState.Won)
                {
                    bool first = !profile.clears.ContainsKey(stage.id);
                    foreach (var r in first ? stage.firstClearRewards : stage.repeatRewards) reward[r.Key] = reward.TryGetValue(r.Key, out int v) ? v + r.Value : r.Value;
                    profile.clears[stage.id] = first ? 1 : profile.clears[stage.id] + 1; profile.highestCleared = Mathf.Max(profile.highestCleared, stage.index);
                    reward["beastCage"] = (first ? 2 : 1) + (UnityEngine.Random.value < .15f ? 1 : 0); reward["monsterChow"] = 8 + kills / 5;
                    if (stage.boss && UnityEngine.Random.value < .08f) reward["moonCage"] = 1;
                    if (first && stage.moduleReward != null && !profile.modules.Contains(stage.moduleReward)) profile.modules.Add(stage.moduleReward);
                    if (first && stage.gearReward != null) profile.gear.Add(new Gear {id = stage.gearReward});
                    if (first && stage.boss) profile.artifacts.Add(new Artifact {id = "artifact-" + stage.index, name = "Huy hiệu Trăng " + stage.index, maxHp = 40 * stage.index, atk = stage.index});
                    profile.Track("wins");
                }
            }
            profile.Grant(reward); profile.Save(); finishedRewards = reward; return reward;
        }
    }
}
