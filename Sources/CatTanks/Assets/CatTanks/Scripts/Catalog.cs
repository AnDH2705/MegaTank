using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    [Serializable] public class Weapon
    {
        public string id, asset, name, kind, description, color = "#ffd675", family;
        public float rate = 1, damage, range = 1500, speed = 780, splash, slow, burn, poison, stun;
        public int count = 1, pierce = 1, chain = 3;
        public bool flip;
        public Weapon Copy() => (Weapon)MemberwiseClone();
    }
    [Serializable] public class MonsterDef
    {
        public string name, asset, behavior, description, color;
        public float size, hp, speed, damage, armor, range;
        public bool flying, boss;
    }
    [Serializable] public class CatDef
    {
        public string id, name, specialty, role, rarity, sprite;
        public Dictionary<string, float> baseStats, growth;
    }
    [Serializable] public class Burst { public float after, spacing; public int count; }
    [Serializable] public class Wave
    {
        public string name;
        public JArray types;
        public int count;
        public float spacing = 1, enemyHpMult = 1, enemyAtkMult = 1, bossHpMult = 1;
        public bool boss;
        public List<Burst> bursts;
    }
    [Serializable] public class Stage
    {
        public string id, name, gearReward, moduleReward;
        public int index;
        public bool boss;
        public float recommendedPower;
        public List<Wave> waves;
        public Dictionary<string, int> firstClearRewards, repeatRewards;
    }
    [Serializable] public class SkillDef
    {
        public string id, name, kind;
        public int baseCost, cost, rockets;
        public float baseCd, cooldown, duration, healVehicle, healUnitPct, slow, damage, rocketDamage, bossMult = 1, splash;
        public JObject unlock;
        public SkillDef Copy() => (SkillDef)MemberwiseClone();
    }
    public sealed class Catalog
    {
        public List<Weapon> weapons;
        public List<MonsterDef> monsters;
        public List<CatDef> cats;
        public List<Stage> stages;
        public List<SkillDef> skills, classicSkills;
        public List<Wave> waves;
        public JObject balance;
        public JArray monsterBook;
        static Catalog current;
        public static Catalog Load() => current ?? (current = JsonConvert.DeserializeObject<Catalog>(Resources.Load<TextAsset>("Data/catalog").text));
        public Weapon Weapon(string id) => weapons.First(w => w.id == id);
        public CatDef Cat(string id) => cats.First(c => c.id == id);
        public MonsterDef Monster(string id) => monsters.First(m => m.asset == id);
        public float B(string path) => balance.SelectToken(path).Value<float>();
        public int I(string path) => Mathf.RoundToInt(B(path));
        public static Color Color(string value) => ColorUtility.TryParseHtmlString(value ?? "#ffffff", out var c) ? c : UnityEngine.Color.white;
    }
}
