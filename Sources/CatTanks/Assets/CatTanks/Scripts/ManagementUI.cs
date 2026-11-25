using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void CatStat(float x,float y,string icon,string caption,float value)
        {
            Art(x,y,37,37,"Interface/Cats/"+icon);
            Text(x+48,y,194,caption+"  "+N(value),subtitle,37,align:TextAnchor.MiddleLeft,size:24);
        }
        void OpenCage(bool moon)
        {
            if (!Pay((moon ? "moonCage" : "beastCage", 1))) return;
            var weights = (JObject)db.balance["monsters"]["cages"][moon ? "moonCage" : "beastCage"];
            var entries = db.monsterBook.ToList(); string[] rarities = {"COMMON", "RARE", "ELITE", "LEGENDARY", "MYTHIC"};
            string prefix = moon ? "moon" : "beast"; int counter(string name) => profile.cagePity.TryGetValue(prefix + name, out int n) ? n : 0;
            int floor = counter("Elite") + 1 >= 10 ? 2 : 0;
            if (counter("Legendary") + 1 >= 40) floor = 3; if (moon && counter("Mythic") + 1 >= 80) floor = 4;
            var allowed = rarities.Where((r, rank) => rank >= floor && (int)weights[r] > 0 && entries.Any(e => (string)e["catchRarity"] == r)).ToArray();
            float roll = UnityEngine.Random.value * allowed.Sum(r => (int)weights[r]); string rarity = allowed.Last();
            foreach (string r in allowed) {roll -= (int)weights[r]; if (roll < 0) {rarity = r; break;}}
            int rankChosen = Array.IndexOf(rarities, rarity);
            profile.cagePity[prefix + "Elite"] = rankChosen >= 2 ? 0 : counter("Elite") + 1;
            profile.cagePity[prefix + "Legendary"] = rankChosen >= 3 ? 0 : counter("Legendary") + 1;
            if (moon) profile.cagePity[prefix + "Mythic"] = rankChosen >= 4 ? 0 : counter("Mythic") + 1;
            var pool = entries.Where(e => (string)e["catchRarity"] == rarity).ToList(); if (pool.Count == 0) pool = entries.Where(e => (string)e["catchRarity"] == "ELITE").ToList();
            string id = (string)pool[UnityEngine.Random.Range(0, pool.Count)]["speciesId"]; profile.GainPet(id, db); Changed("Nhận được " + db.Monster(id).name + " · " + rarity);
        }
        string GearAsset(Gear g) => g.id.StartsWith("gun") ? g.id : g.id == "gear-helm" ? "hat-0" : g.id == "gear-chest" ? "armor-0" : "gear-boots";
        string GearSlot(Gear g) => g.id.StartsWith("gun") ? "weapon" : g.id == "gear-helm" ? "helm" : g.id == "gear-chest" ? "chest" : "boots";
    }
}
