using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CatTanks
{
    [Serializable] public class OwnedCat
    {
        public string id, tier = "RECRUIT";
        public int level = 1, copies, relic;
        public string artifact;
        public Dictionary<string, string> gear = new Dictionary<string, string>();
    }
    [Serializable] public class OwnedTurret
    {
        public string id;
        public int level = 1, stars;
        public string[] modules = { "", "" };
    }
    [Serializable] public class Floor { public string turret, cat; }
    [Serializable] public class Pet
    {
        public string id;
        public int level = 1, stars, shards, evolution;
        public float iv;
        public int[] ivs = {0, 0, 0, 0};
        public string trait = "none";
    }
    [Serializable] public class Gear
    {
        public string uid = Guid.NewGuid().ToString("N"), id, rarity = "COMMON";
        public int stars;
    }
    [Serializable] public class Artifact {public string id, name; public int maxHp, atk;}
    [Serializable] public class Preset { public List<Floor> floors; public string[] squad, skills, pets; }
    [Serializable] public class Profile
    {
        public int version = 1, accountLevel = 1, accountXp, hpLevel = 1, highestCleared, selected = 1, rating = 1000, pityElite, pityHigh;
        public string name = "Chỉ huy mèo", day;
        public long lastIdle;
        public bool classicCleared;
        public Dictionary<string, int> wallet = new Dictionary<string, int>();
        public List<OwnedCat> cats = new List<OwnedCat>();
        public List<OwnedTurret> turrets = new List<OwnedTurret>();
        public List<Floor> floors = new List<Floor>();
        public List<Pet> pets = new List<Pet>();
        public List<Gear> gear = new List<Gear>();
        public List<Artifact> artifacts = new List<Artifact>();
        public List<string> modules = new List<string>(), seen = new List<string>();
        public string[] squad = {"cat-1", "cat-2", "", "", ""}, petSlots = {"", "", ""}, skillSlots = {"heal", "barrage", "", ""};
        public Preset[] presets = new Preset[3];
        public Dictionary<string, int> skillLevels = new Dictionary<string, int>(), clears = new Dictionary<string, int>(), daily = new Dictionary<string, int>(), bought = new Dictionary<string, int>();
        public Dictionary<string, int> cagePity = new Dictionary<string, int>();
        public List<int> collectionClaimed = new List<int>();
        public string[] nexus = new string[8];
        public List<string> claimed = new List<string>();
        [JsonIgnore] public static string TestSavePath;
        [JsonIgnore] public static string SavePath => TestSavePath ?? Path.Combine(Application.persistentDataPath, "cattanks-profile.json");
        [JsonIgnore] public int FloorCount => Mathf.Min(5, 1 + accountLevel / 20);
        [JsonIgnore] public int SquadCount => 2 + (accountLevel >= 8 ? 1 : 0) + (accountLevel >= 15 ? 1 : 0) + (accountLevel >= 25 ? 1 : 0);
        [JsonIgnore] public int PetCount => pets.Count == 0 ? 0 : 1 + (pets.Count >= 5 || accountLevel >= 10 ? 1 : 0) + (pets.Count >= 10 || accountLevel >= 25 ? 1 : 0);
        [JsonIgnore] public int NexusSlots => 3 + new[] {4, 6, 8, 10, 13}.Count(i => highestCleared >= i);
        [JsonIgnore] public float MaxHp => Mathf.Floor(12000 * Mathf.Pow(1.08f, hpLevel - 1) + 800 * FloorCount);
        public int Get(string key) => wallet.TryGetValue(key, out int n) ? n : 0;
        public void Add(string key, int n) { wallet[key] = Mathf.Clamp(Get(key) + n, 0, 999999999); }
        public bool Spend(params (string key, int amount)[] costs)
        {
            if (costs.Any(c => c.amount < 0 || Get(c.key) < c.amount)) return false;
            foreach (var c in costs) Add(c.key, -c.amount);
            return true;
        }
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static Profile New(Catalog db)
        {
            var p = new Profile {lastIdle = Now};
            p.wallet = db.balance["startingWallet"].ToObject<Dictionary<string, int>>();
            foreach (var v in db.balance["monsters"]["startingBag"].ToObject<Dictionary<string, int>>()) p.Add(v.Key, v.Value);
            foreach (string id in new[] {"cat-0", "cat-1", "cat-2"}) p.GainCat(id, db);
            for (int i = 0; i < 10; i++) p.turrets.Add(new OwnedTurret {id = "turret-" + i});
            for (int i = 0; i < 5; i++) p.floors.Add(new Floor {turret = "turret-" + i, cat = i == 0 ? "cat-0" : ""});
            p.RefreshDay(); return p;
        }
        public static Profile Load(Catalog db)
        {
            foreach (string path in new[] {SavePath, SavePath + ".bak"})
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var p = JsonConvert.DeserializeObject<Profile>(File.ReadAllText(path));
                    if (p == null || p.version != 1 || p.floors.Count != 5 || p.cats.Count == 0) throw new Exception("Hồ sơ không hợp lệ");
                    p.RefreshDay(); return p;
                }
                catch (Exception e) { Debug.LogWarning("Không đọc được " + path + ": " + e.Message); }
            }
            return New(db);
        }
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(this, Formatting.Indented));
            if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", true);
            if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null); else File.Move(tmp, SavePath);
        }
        public void RefreshDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (day == today) return;
            day = today; daily.Clear(); bought.Clear(); claimed.Clear();
        }
        public void Track(string action) { RefreshDay(); daily[action] = daily.TryGetValue(action, out int n) ? n + 1 : 1; }
        public void GainCat(string id, Catalog db)
        {
            var cat = cats.Find(c => c.id == id);
            if (cat != null) cat.copies++;
            else cats.Add(new OwnedCat {id = id, tier = db.Cat(id).rarity == "ELITE" ? "VETERAN" : "RECRUIT"});
        }
        public int EffectiveLevel(OwnedCat cat)
        {
            var anchors = cats.OrderByDescending(c => c.level).Take(5).ToList();
            return anchors.Count == 5 && !anchors.Contains(cat) && nexus.Contains(cat.id) ? Mathf.Min(Catalog.Load().I("ascension.levelCaps." + cat.tier), anchors.Min(c => c.level)) : cat.level;
        }
        public Dictionary<string, float> Stats(OwnedCat cat, Catalog db, string role = "")
        {
            var d = db.Cat(cat.id); int level = EffectiveLevel(cat);
            float tier = db.B("ascension.tierMultipliers." + cat.tier);
            var s = d.baseStats.ToDictionary(k => k.Key, k => (k.Value + d.growth[k.Key] * (level - 1)) * tier);
            s["atk"] += cat.relic * 2; s["maxHp"] += cat.relic * 12;
            var artifact = artifacts.Find(a => a.id == cat.artifact);
            if (artifact != null) {s["atk"] += artifact.atk; s["maxHp"] += artifact.maxHp;}
            foreach (var id in cat.gear.Values)
            {
                var g = gear.Find(v => v.uid == id); if (g == null) continue;
                float mult = db.B("gear.rarityMult." + g.rarity) * (1 + .12f * g.stars);
                string key = g.id.StartsWith("gun") ? "atk" : g.id == "gear-helm" ? "maxHp" : g.id == "gear-chest" ? "def" : "speed";
                s[key] += (key == "atk" ? 20 : key == "maxHp" ? 80 : key == "def" ? 12 : 8) * mult;
            }
            if (d.specialty == role) { s["atk"] *= 1.15f; s["maxHp"] *= 1.15f; }
            return s.ToDictionary(k => k.Key, k => Mathf.Floor(k.Value));
        }
        public float Power(Catalog db) => cats.Sum(c => {var s = Stats(c, db); return s["maxHp"] * .15f + s["atk"] * 1.6f + s["def"] * 1.2f + s["speed"] * 2;});
        public bool LevelCat(OwnedCat cat, Catalog db)
        {
            if (cat.level >= db.I("ascension.levelCaps." + cat.tier) || nexus.Contains(cat.id)) return false;
            if (!Spend(("coins", Mathf.FloorToInt(80 * Mathf.Pow(cat.level, 1.45f))), ("catXp", Mathf.FloorToInt(25 * Mathf.Pow(cat.level, 1.22f))))) return false;
            cat.level++; Track("upgrade"); Save(); return true;
        }
        public Dictionary<string, int> Idle(long now)
        {
            float minutes = Mathf.Clamp(now - lastIdle, 0, 43200) / 60f, m = highestCleared == 0 ? .5f : 1, stage = Mathf.Max(1, highestCleared) - 1;
            return new Dictionary<string, int> {
                ["coins"] = Mathf.FloorToInt(7 * Mathf.Pow(1.065f, stage) * minutes * m),
                ["spiritDust"] = Mathf.FloorToInt(2.6f * Mathf.Pow(1.06f, stage) * minutes * m),
                ["catXp"] = Mathf.FloorToInt(5 * Mathf.Pow(1.055f, stage) * minutes * m),
                ["forgeStone"] = Mathf.FloorToInt((.4f + .05f * highestCleared) * minutes / 60 * m),
                ["summonScroll"] = Mathf.FloorToInt((1 + highestCleared / 5) * minutes / 720 * m),
                ["monsterChow"] = Mathf.FloorToInt(.1f * minutes * m),
                ["beastCage"] = Mathf.FloorToInt(highestCleared / 4 * minutes / 720 * m)
            };
        }
        public void Grant(Dictionary<string, int> reward)
        {
            foreach (var r in reward) { if (r.Key == "accountXp") GrantXp(r.Value); else Add(r.Key, r.Value); }
        }
        public void GrantXp(int xp)
        {
            accountXp += xp;
            while (accountLevel < 300 && accountXp >= 15 + accountLevel * 4) {accountXp -= 15 + accountLevel * 4; accountLevel++;}
        }
        public string Summon(Catalog db, int count)
        {
            if (!Spend(("summonScroll", count))) return "Thiếu vé triệu hồi";
            var names = new List<string>(); bool elite = false;
            for (int i = 0; i < count; i++)
            {
                pityElite++; pityHigh++;
                bool pity = pityElite >= 10 || pityHigh >= 40 || (count == 10 && i == 9 && !elite);
                float roll = UnityEngine.Random.value * 9700;
                string rarity = pity || roll >= 8500 ? "ELITE" : roll >= 5000 ? "RARE" : "COMMON";
                var pool = db.cats.Where(c => c.rarity == rarity).ToList();
                var d = pool[UnityEngine.Random.Range(0, pool.Count)]; GainCat(d.id, db); names.Add(d.name);
                if (rarity == "ELITE") {pityElite = 0; elite = true; pityHigh = 0;}
            }
            Save(); return string.Join(" · ", names);
        }
        public void GainPet(string id, Catalog db)
        {
            var p = pets.Find(v => v.id == id);
            if (p != null)
            {
                string rarity = (string)db.monsterBook.First(m => (string)m["speciesId"] == id)["catchRarity"];
                p.shards += db.I("monsters.shardsByRarity." + rarity);
            }
            else
            {
                pets.Add(new Pet {id = id, iv = UnityEngine.Random.value, ivs = Enumerable.Range(0, 4).Select(_ => UnityEngine.Random.Range(0, 16)).ToArray(), trait = UnityEngine.Random.value < .85f ? "none" : new[] {"swift", "brutal", "sturdy", "gifted"}[UnityEngine.Random.Range(0, 4)]});
                if (string.IsNullOrEmpty(petSlots[0])) petSlots[0] = id;
            }
            if (!seen.Contains(id)) seen.Add(id); Track("capture");
        }
        public bool SkillUnlocked(SkillDef s)
        {
            if (s.unlock == null || s.unlock["start"] != null) return true;
            return (s.unlock["stage"] != null && highestCleared >= (int)s.unlock["stage"]) || (s.unlock["accountLevel"] != null && accountLevel >= (int)s.unlock["accountLevel"]);
        }
        public Dictionary<string, float> PetStats(Pet pet, Catalog db)
        {
            var entry = db.monsterBook.First(m => (string)m["speciesId"] == pet.id);
            float mult = (1 + .08f * (pet.level - 1)) * (1 + .14f * pet.stars) * (1 + .25f * pet.evolution);
            var stats = new Dictionary<string, float> {
                ["maxHp"] = Mathf.Floor((float)entry["baseHp"] * mult * (1 + pet.ivs[0] / 15f * .2f)),
                ["atk"] = Mathf.Floor((float)entry["baseAtk"] * mult * (1 + pet.ivs[1] / 15f * .2f)),
                ["def"] = Mathf.Floor((float)entry["baseDef"] * mult * (1 + pet.ivs[2] / 15f * .2f)),
                ["speed"] = Mathf.Floor((float)entry["baseSpeed"] * (1 + pet.ivs[3] / 15f * .2f)),
                ["rate"] = Mathf.Max(.15f, (float)entry["weapon"]["rate"] * (1 - pet.ivs[3] / 15f * .2f))
            };
            string key = pet.trait == "swift" ? "speed" : pet.trait == "brutal" ? "atk" : pet.trait == "sturdy" ? "maxHp" : pet.trait == "gifted" ? "def" : null;
            if (key != null) stats[key] = Mathf.Floor(stats[key] * 1.08f);
            return stats;
        }
        public void SavePreset(int i)
        {
            presets[i] = new Preset {floors = floors.Select(f => new Floor {turret = f.turret, cat = f.cat}).ToList(), squad = (string[])squad.Clone(), skills = (string[])skillSlots.Clone(), pets = (string[])petSlots.Clone()}; Save();
        }
        public void LoadPreset(int i)
        {
            var v = presets[i]; if (v == null) return;
            floors = v.floors.Select(f => new Floor {turret = f.turret, cat = f.cat}).ToList(); squad = (string[])v.squad.Clone(); skillSlots = (string[])v.skills.Clone(); petSlots = (string[])v.pets.Clone(); Save();
        }
    }
}
