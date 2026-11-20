using System.Collections.Generic;

namespace CatTanks.VfxKit
{
    // These keys can be written to BattleEffect.visual without referencing the renderer.
    public static class BattleVfxTags
    {
        static readonly Dictionary<string, WeaponKeys> weaponKeys = new Dictionary<string, WeaponKeys>();

        struct WeaponKeys
        {
            public string muzzle, impact;
        }

        static WeaponKeys Keys(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return default;
            if (!weaponKeys.TryGetValue(kind, out var keys))
            {
                keys = new WeaponKeys { muzzle = "K/Muzzle/" + kind, impact = "K/Impact/" + kind };
                weaponKeys.Add(kind, keys);
            }
            return keys;
        }

        public static string Muzzle(string kind) => Keys(kind).muzzle;
        public static string Impact(string kind) => Keys(kind).impact;

        public static string Skill(string id)
        {
            switch (id)
            {
                case "heal": return "heal";
                case "barrage": return "ignition";
                case "shield": return "ward";
                case "rally": return "rally";
                case "freeze": return "frost";
                case "drone": return "pulse";
                case "orbital": return "pulse";
                case "decoy": return "portal";
                case "magnet": return "drop";
                case "kamikaze": return "rocketblast";
                default: return null;
            }
        }

        public static string MonsterContact(string behavior)
        {
            switch (behavior)
            {
                case "web": return "web";
                case "venom":
                case "moth":
                case "queen": return "poison";
                case "frost": return "frost";
                case "lava":
                case "boss": return "fire";
                case "crystal": return "crystal";
                case "charge":
                case "rhino": return "dust";
                case "wolf":
                case "tiger":
                case "leap": return "slash";
                default: return "hit";
            }
        }
    }
}
