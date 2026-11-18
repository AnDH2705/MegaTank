using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegaTanks
{
    public static class ProgressionRules
    {
        public const int MaxStars = 20;
        // -1 means the next level cannot be represented by the integer wallet.
        public static int LevelCost(int level)
        {
            if (level < 1) return -1;
            double cost = Math.Ceiling(100 * Math.Pow(1.1, level - 1) - 1e-8);
            return double.IsInfinity(cost) || cost > int.MaxValue ? -1 : (int)cost;
        }
        public static int StarCost(int stars) => stars < 0 || stars >= MaxStars ? 0 : stars < 5 ? 20 : stars < 10 ? 40 : stars < 15 ? 70 : 100;
        public static float Scale(float baseValue, float perLevel, int level, int stars) => (Mathf.Max(0, baseValue) + Mathf.Max(0, perLevel) * Math.Max(0, level - 1)) * (1 + .1f * Mathf.Clamp(stars, 0, MaxStars));
        public static float Damage(float baseDamage, int level, int stars) => Scale(baseDamage, 1, level, stars);
    }
    public readonly struct ProgressionState
    {
        public readonly int Level, Stars, Fragments;
        public ProgressionState(int level, int stars, int fragments) { Level = level; Stars = stars; Fragments = fragments; }
    }
    public sealed class PlayerProgression
    {
        public const string DefaultSaveKey = "MegaTanks.Progression";
        [Serializable] sealed class Entry { public string id; public int level = 1, stars, fragments; }
        [Serializable] sealed class SaveData { public int version = 1; public List<Entry> entries = new List<Entry>(); }
        readonly Dictionary<Object, string> owned = new Dictionary<Object, string>();
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        readonly PlayerWallet wallet;
        readonly string saveKey;
        public event Action Changed;

        public PlayerProgression(FrontendCatalog catalog, PlayerWallet wallet, string preferenceKey = DefaultSaveKey)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (string.IsNullOrWhiteSpace(preferenceKey)) throw new ArgumentException("Save key is required.");
            saveKey = preferenceKey;
            foreach (var weapon in (catalog?.inventory ?? Array.Empty<EquipmentDefinition>()).OfType<WeaponDefinition>().Distinct()) Register(weapon, "weapon:" + weapon.id, weapon.id);
            foreach (var member in (catalog?.crew?.members ?? Array.Empty<CrewDefinition>()).Where(m => m != null).Distinct()) Register(member, "crew:" + member.id, member.id);
            foreach (var armor in (catalog?.inventory ?? Array.Empty<EquipmentDefinition>()).OfType<ArmorDefinition>().Distinct()) Register(armor, "armor:" + armor.id, armor.id);
            foreach(var accessory in (catalog?.inventory??Array.Empty<EquipmentDefinition>()).OfType<AccessoryDefinition>().Distinct())Register(accessory,"accessory:"+accessory.id,accessory.id);
            SaveData saved = null;
            try { saved = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey, "{}")); }
            catch (ArgumentException) { }
            foreach (var entry in saved?.entries ?? new List<Entry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entries.ContainsKey(entry.id)) continue;
                entry.level = Math.Max(1, entry.level); entry.stars = Mathf.Clamp(entry.stars, 0, ProgressionRules.MaxStars); entry.fragments = Math.Max(0, entry.fragments);
                entries.Add(entry.id, entry);
            }
        }
        void Register(Object definition, string key, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || owned.Values.Contains(key)) throw new InvalidOperationException("Progression definitions need unique permanent IDs: " + definition.name);
            owned.Add(definition, key);
        }
        Entry Find(Object definition)
        {
            if (definition == null || !owned.TryGetValue(definition, out string id)) return null;
            if (!entries.TryGetValue(id, out var entry)) entries.Add(id, entry = new Entry { id = id });
            return entry;
        }
        public bool Supports(Object definition) => definition != null && owned.ContainsKey(definition);
        public ProgressionState Get(Object definition)
        {
            var e = Find(definition);
            return e == null ? new ProgressionState(1, 0, 0) : new ProgressionState(e.level, e.stars, e.fragments);
        }
        public bool TryGrantFragments(Object definition, int amount)
        {
            var e = Find(definition);
            if (e == null || amount <= 0 || amount > int.MaxValue - e.fragments) return false;
            e.fragments += amount; SavePending(); PlayerPrefs.Save(); Changed?.Invoke(); return true;
        }
        public bool CanLevelUp(Object definition)
        {
            if (!Supports(definition)) return false;
            int cost = ProgressionRules.LevelCost(Get(definition).Level);
            return cost > 0 && wallet.CanAfford(definition is CrewDefinition ? CurrencyType.PetFood : CurrencyType.Parts, cost);
        }
        public bool TryLevelUp(Object definition)
        {
            if (!CanLevelUp(definition)) return false;
            var e = Find(definition);
            // Both keys are staged before one local save; listeners only see the completed upgrade.
            bool result = wallet.TrySpendWithCommit(definition is CrewDefinition ? CurrencyType.PetFood : CurrencyType.Parts,
                ProgressionRules.LevelCost(e.level), () => { e.level++; SavePending(); });
            if (result) Changed?.Invoke();
            return result;
        }
        public bool CanStarUp(Object definition)
        {
            var s = Get(definition); int cost = ProgressionRules.StarCost(s.Stars);
            return !(definition is AccessoryDefinition) && Supports(definition) && cost > 0 && s.Fragments >= cost;
        }
        public bool TryStarUp(Object definition)
        {
            if (!CanStarUp(definition)) return false;
            var e = Find(definition); e.fragments -= ProgressionRules.StarCost(e.stars); e.stars++;
            SavePending(); PlayerPrefs.Save(); Changed?.Invoke(); return true;
        }
        void SavePending() => PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(new SaveData { entries = entries.Values.OrderBy(e => e.id).ToList() }));
        public static float BaseDamage(Object definition) => definition is GunDefinition gun ? gun.stats.damage : definition is MortarDefinition mortar ? mortar.stats.damage : definition is CrewDefinition crew ? crew.stats.damage.value : 0;
        public float Damage(Object definition)
        {
            var s = Get(definition); return definition is CrewDefinition crew ? crew.stats.damage.AtLevel(s.Level,s.Stars) : ProgressionRules.Damage(BaseDamage(definition), s.Level, s.Stars);
        }
        public float ArmorHP(ArmorDefinition armor)
        {
            if (armor == null) return 0;
            var state = Get(armor); return armor.HitPoints(state.Level, state.Stars);
        }
        public float CrewDamage(PlayerCrew crew)
        {
            // Compatibility for older editor integrations. There is no shared crew damage pool.
            return 0;
        }
    }
}
