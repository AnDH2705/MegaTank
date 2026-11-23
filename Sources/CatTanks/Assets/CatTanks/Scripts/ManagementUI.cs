using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        int gearCat;
        void Barracks()
        {
            string[] tabs = {"cats", "fortress", "squad", "skills", "monsters", "gear", "presets", "nexus"};
            string[] names = {"MÈO", "PHÁO ĐÀI", "ĐỘI HÌNH", "KỸ NĂNG", "QUÁI THÚ", "TRANG BỊ", "ĐỘI LƯU", "CỘNG HƯỞNG"};
            string[] icons = {"cat-0", "vehicle-0", "skill-rally", "item-skillCore", "item-beastCage", "armor-0", "Book_1_Purple", "item-soulEssence"};
            if (!tabs.Contains(tab)) tab = "cats";
            for (int i = 0; i < tabs.Length; i++)
            {
                string icon=UsesManagementArt?"Interface/Cats/Tab-"+tabs[i]:icons[i];
                if (TabButton(283 + i * 158, 148, 146, 50, names[i], tab == tabs[i], icon)) {tab = tabs[i]; scroll = Vector2.zero;}
            }
            useCodeGeometry=!UsesCatsArt;
            switch (tab)
            {
                case "cats": Cats(); break; case "fortress": FortressUI(); break; case "squad": SquadUI(); break;
                case "skills": SkillsUI(); break; case "monsters": MonstersUI(); break; case "gear": GearUI(); break; case "presets": PresetsUI(); break; case "nexus": NexusUI(); break;
            }
        }
        void Cats()
        {
            Text(290, 228, 700, "Những người giữ pháo đài", title, 57);
            Art(290, 281, 29, 29, "item-catXp");
            Text(327, 285, 1143, $"{profile.cats.Count}/10 mèo · {N(profile.Get("catXp"))} XP mèo · {profile.Get("soulEssence")} tinh chất · Mở thêm mèo tại Thành mèo / {SummonTabLabel}", small);
            Surface(282, 322, 756, 410, new Color(.13f,.18f,.30f), catsFrame:"Roster");
            for (int i = 0; i < profile.cats.Count; i++)
            {
                var c = profile.cats[i]; float x = 298 + i % 5 * 147, y = 334 + i / 5 * 198;
                if (IllustratedButton(x, y, 136, 184, db.Cat(c.id).name + " · Cấp " + c.level, c.id,
                    content: StudioButtonLayout.Content.Tile, selected: selectedCat == i)) selectedCat = i;
            }
            selectedCat = Mathf.Clamp(selectedCat, 0, profile.cats.Count - 1); var cat = profile.cats[selectedCat]; var def = db.Cat(cat.id); var stats = profile.Stats(cat, db);
            Surface(1054, 322, 494, 477, new Color(.13f,.18f,.30f), catsFrame:"Detail");
            Text(1078, 342, 446, def.name, title, 57);
            Surface(1074, 389, 96, 28, Color.white, catsFrame:"Rarity");
            Text(1085, 392, 85, def.rarity, small, 25);
            Text(1078, 419, 446, $"{def.role}  /  {cat.tier}\nCấp thực {cat.level} · Cấp cộng hưởng {profile.EffectiveLevel(cat)}", label, 54);
            CatStat(1078, 479, "Heart", "HP", stats["maxHp"]);
            CatStat(1302, 479, "Sword", "ATK", stats["atk"]);
            CatStat(1078, 513, "Shield", "DEF", stats["def"]);
            CatStat(1302, 513, "Speed", "SPD", stats["speed"]);
            Text(1078, 548, 446, $"Bản sao: {cat.copies} · Di vật: {cat.relic}", label);
            int goldCost = Mathf.FloorToInt(80 * Mathf.Pow(cat.level, 1.45f)), xpCost = Mathf.FloorToInt(25 * Mathf.Pow(cat.level, 1.22f));
            if (IllustratedButton(1078, 590, 446, 58, "Nâng cấp · " + Cost(("coins", goldCost), ("catXp", xpCost)), "item-catXp", profile.Get("coins") >= goldCost && profile.Get("catXp") >= xpCost, true)) {if (profile.LevelCat(cat, db)) Changed("Mèo đã lên cấp"); else Notify("Thiếu tài nguyên hoặc đã đạt giới hạn cấp");}
            var tiers = ((JObject)db.balance["ascension"]["levelCaps"]).Properties().Select(p => p.Name).ToArray(); int rank = Array.IndexOf(tiers, cat.tier); int copies = rank < 11 ? (int)db.balance["ascension"]["needSelf"][rank] : 0;
            if (Button(1078, 658, 446, 55, $"Thăng bậc · {copies} bản sao / {10 + rank * 15} tinh chất", rank < tiers.Length - 1))
            {
                if (cat.copies < copies) Notify("Thiếu bản sao");
                else if (Pay(("soulEssence", 10 + rank * 15))) {cat.copies -= copies; cat.tier = tiers[rank + 1]; Changed("Đã thăng bậc " + cat.tier);}
            }
            if (Button(1078, 724, 215, 52, "Tái chế 1 bản sao", cat.copies > 0, normalSpriteKey:"blue")) Pick("Tái chế bản sao " + def.name + "?", new[] {"Giữ lại bản sao", "Tái chế • Nhận 20 tinh chất"}, at => {if(at == 1) {cat.copies--; profile.Add("soulEssence", 20); Changed("+20 tinh chất");}});
            int relicCost = Mathf.Max(10, cat.relic * 10);
            if (IllustratedButton(1309, 724, 215, 52, "Di vật · " + relicCost + " đá", "item-relicStone", cat.relic < 10)) {if (Pay(("relicStone", relicCost))) {cat.relic++; Changed();}}
            Text(298, 748, 724, "Cộng hưởng: xếp mèo vào ô Cộng hưởng để nhận cấp của mèo thấp nhất trong nhóm 5 mèo neo.", small, 48);
        }
        void CatStat(float x,float y,string icon,string caption,float value)
        {
            Art(x,y,32,32,"Interface/Cats/"+icon);
            Text(x+42,y+2,164,caption+" "+N(value),label);
        }
        void FortressUI()
        {
            Text(290, 225, 900, "Pháo đài di động", title, 57);
            Text(290, 283, 1160, $"HP {N(profile.MaxHp)} · {profile.FloorCount}/5 tầng · Mở tầng tại cấp chỉ huy 20 / 40 / 60 / 80", small);
            Surface(282, 322, 756, 478, Color.white, catsFrame:"Roster");
            Surface(1054, 322, 494, 478, Color.white, catsFrame:"Detail");
            int hpCost = Mathf.FloorToInt(400 * Mathf.Pow(profile.hpLevel, 1.4f));
            if (Button(298, 339, 724, 56, $"Giáp cấp {profile.hpLevel} → {profile.hpLevel + 1} · {N(hpCost)} xu", normalSpriteKey:"blue")) {if (profile.hpLevel < 240 && Pay(("coins", hpCost))) {profile.hpLevel++; profile.Track("upgrade"); Changed("Pháo đài được gia cố");}}
            for (int i = 0; i < 10; i++)
            {
                float x = 298 + i % 5 * 147, y = 410 + i / 5 * 172; var t = profile.turrets[i];
                if (IllustratedButton(x, y, 136, 156, "Cấp " + t.level + " · " + t.stars + "★", t.id,
                    content: StudioButtonLayout.Content.Tile, selected: selectedTurret == i)) selectedTurret = i;
            }
            var turret = profile.turrets[selectedTurret]; var weapon = db.Weapon(turret.id);
            Text(1078, 344, 298, weapon.name, subtitle, 55);
            Art(1392, 340, 124, 82, turret.id);
            Text(1078, 423, 446, weapon.description, small, 77);
            int coin = Mathf.FloorToInt(60 * Mathf.Pow(turret.level, 1.4f)), dust = Mathf.FloorToInt(30 * Mathf.Pow(turret.level, 1.3f));
            if (Button(1078, 512, 446, 58, "Nâng cấp · " + Cost(("coins", coin), ("spiritDust", dust)), primary:true))
            {
                if (turret.level >= 40 + 8 * turret.stars) Notify("Cần nâng sao để tăng giới hạn cấp");
                else if (Pay(("coins", coin), ("spiritDust", dust))) {turret.level++; profile.Track("upgrade"); Changed();}
            }
            if (Button(1078, 582, 446, 58, "Nâng sao · " + Cost(("coins", 200 * (turret.stars + 1)), ("forgeStone", 5 + 5 * turret.stars)), turret.stars < 5))
            {
                if (Pay(("coins", 200 * (turret.stars + 1)), ("forgeStone", 5 + 5 * turret.stars))) {turret.stars++; Changed("Đã nâng sao pháo");}
            }
            for (int slot = 0; slot < 2; slot++)
            {
                string id = turret.modules[slot]; string name = string.IsNullOrEmpty(id) ? "Trống" : db.Weapon(id).name;
                float y = 653 + slot * 67;
                if (IllustratedButton(1078, y, 446, 56, $"Module {slot + 1}: {name}  ›", string.IsNullOrEmpty(id) ? "Gear" : id))
                {
                    var options = new[] {""}.Concat(profile.modules).ToArray(); int chosenSlot = slot;
                    Pick("Chọn module " + (slot + 1), options.Select(v => string.IsNullOrEmpty(v) ? "Gỡ module" : db.Weapon(v).name), at => {turret.modules[chosenSlot] = options[at]; Changed("Đã đổi module");});
                }
            }
            Text(298, 750, 724, "Module nhận từ chiến dịch. Bấm ô module để chuyển giữa những module đang sở hữu.", small, 46);
        }
        void CycleCat(int index, bool operatorSlot)
        {
            string current = operatorSlot ? profile.floors[index].cat : profile.squad[index];
            var taken = profile.floors.Select(f => f.cat).Concat(profile.squad).Where(v => v != current).ToHashSet();
            var choices = new[] {""}.Concat(profile.cats.Select(c => c.id).Where(id => !taken.Contains(id))).ToArray();
            Pick(operatorSlot ? "Chọn mèo vận hành pháo" : "Chọn mèo bộ binh", choices.Select(id => string.IsNullOrEmpty(id) ? "Để trống vị trí" : db.Cat(id).name), at =>
            {
                if (operatorSlot) profile.floors[index].cat = choices[at]; else profile.squad[index] = choices[at];
                Changed("Đội hình đã cập nhật");
            });
        }
        void SquadUI()
        {
            Text(290, 225, 1000, "Bố trí trước giờ xuất kích", title, 57);
            Text(290, 285, 1160, "Bấm các ô để đổi pháo và mèo. Mỗi mèo giữ một vị trí. Mèo đúng chuyên môn nhận thêm 15% chỉ số.", small);
            Surface(282, 322, 844, 453, Color.white, catsFrame:"Roster");
            Surface(1142, 322, 406, 453, Color.white, catsFrame:"Detail");
            Text(302, 334, 85, "TẦNG", small);
            Text(400, 334, 360, "THÁP PHÁO", small);
            Text(850, 334, 252, "MÈO VẬN HÀNH", small);
            Text(1160, 334, 370, "BỘ BINH", small);
            for (int i = 0; i < 5; i++)
            {
                float y = 370 + i * 78; var f = profile.floors[i]; bool unlocked = i < profile.FloorCount;
                Text(302, y + 15, 92, "Tầng " + (i + 1), subtitle);
                if (IllustratedButton(400, y, 435, 60, unlocked ? db.Weapon(f.turret).name + "  ›" : "Cấp " + i * 20, unlocked ? f.turret : "Lock", unlocked))
                {
                    Pick("Chọn pháo cho tầng " + (i + 1), profile.turrets.Select(t => db.Weapon(t.id).name + " · Cấp " + t.level), at =>
                    {
                        string next = profile.turrets[at].id; var other = profile.floors.Find(v => v != f && v.turret == next);
                        if (other != null) other.turret = f.turret; f.turret = next; Changed("Đã đổi pháo");
                    });
                }
                bool emptySeat = string.IsNullOrEmpty(f.cat);
                if (IllustratedButton(850, y, 258, 60, emptySeat ? "Ghế trống  ›" : db.Cat(f.cat).name + "  ›", !unlocked ? "Lock" : emptySeat ? "hat-0" : f.cat, unlocked, normalSpriteKey:"blue")) CycleCat(i, true);
                bool squadUnlocked = i < profile.SquadCount;
                string catId = profile.squad[i];
                if (IllustratedButton(1160, y, 370, 60, !squadUnlocked ? "Bộ binh khóa · Cấp " + new[] {1, 1, 8, 15, 25}[i] : "Bộ binh " + (i + 1) + ": " + (string.IsNullOrEmpty(catId) ? "Dân quân" : db.Cat(catId).name) + "  ›",
                    !squadUnlocked ? "Lock" : string.IsNullOrEmpty(catId) ? "cat-0" : catId, squadUnlocked)) CycleCat(i, false);
            }
            Text(302, 782, 1210, "Ghế trống: pháo giữ 70% sát thương. Ô bộ binh trống dùng dân quân. Đội hình khóa lúc bắt đầu trận.", small, 25);
        }
        void SkillsUI()
        {
            Text(290, 225, 1100, "Kỹ năng chiến thuật", title, 57);
            Text(290, 282, 1100, "Lõi kỹ năng nhận ở ải trùm. Chọn tối đa 4 kỹ năng; phím 1–4 kích hoạt trong trận.", small);
            Surface(282, 318, 1266, 80, Color.white, catsFrame:"Roster");
            for (int slot = 0; slot < 4; slot++)
            {
                string id = profile.skillSlots[slot]; var skill = db.skills.Find(s => s.id == id);
                if (IllustratedButton(298 + slot * 310, 330, 300, 56, $"[{slot + 1}] {(skill == null ? "Trống" : skill.name)}  ›", skill == null ? "Gear" : "skill-" + id, normalSpriteKey:"blue"))
                {
                    var options = new[] {""}.Concat(db.skills.Where(s => profile.SkillUnlocked(s) && (!profile.skillSlots.Contains(s.id) || s.id == id)).Select(s => s.id)).ToArray();
                    int chosenSlot = slot;
                    Pick("Chọn kỹ năng " + (slot + 1), options.Select(v => string.IsNullOrEmpty(v) ? "Gỡ kỹ năng" : db.skills.First(s => s.id == v).name), at =>
                    {
                        string next = options[at];
                        if (string.IsNullOrEmpty(next) && profile.skillSlots.Count(v => !string.IsNullOrEmpty(v)) == 1) {Notify("Giữ ít nhất một kỹ năng trong đội hình");return;}
                        profile.skillSlots[chosenSlot] = next; Changed("Đã đổi kỹ năng");
                    });
                }
            }
            for (int i = 0; i < db.skills.Count; i++)
            {
                float x = 283 + i % 5 * 255, y = 416 + i / 5 * 191; var s = db.skills[i]; bool unlocked = profile.SkillUnlocked(s); int level = profile.skillLevels.TryGetValue(s.id, out int n) ? n : 1;
                Panel(x, y, 244, 178);
                Art(x + 13, y + 12, 40, 40, "skill-" + s.id);
                Text(x + 62, y + 10, 170, s.name + " · Lv " + level, label, 55);
                Text(x + 13, y + 68, 220, unlocked ? $"{s.baseCost} NL · {s.baseCd}s hồi\n{(s.duration > 0 ? s.duration + " giây hiệu lực" : "Kích hoạt tức thì")}" : "Mở tại " + (s.unlock["stage"] != null ? "ải " + s.unlock["stage"] : "cấp " + s.unlock["accountLevel"]), small, 47);
                if (Button(x + 12, y + 118, 220, 48, "Nâng cấp · " + level + " lõi", unlocked && level < 8)) {if (Pay(("skillCore", level))) {profile.skillLevels[s.id] = level + 1; Changed();}}
            }
        }
        void MonstersUI()
        {
            Text(290, 225, 1000, "Hang quái đồng hành", title, 57);
            Text(290, 281, 1160, $"{profile.pets.Count}/20 loài · {profile.Get("beastCage")} lồng thú · {profile.Get("moonCage")} lồng trăng · {profile.Get("monsterChow")} thức ăn", small);
            if (IllustratedButton(290, 325, 330, 56, "Mở lồng thú", "item-beastCage", profile.Get("beastCage") > 0)) OpenCage(false);
            if (IllustratedButton(640, 325, 330, 56, "Mở lồng trăng", "item-moonCage", profile.Get("moonCage") > 0)) OpenCage(true);
            int collection = new[] {5, 10, 15, 20}.FirstOrDefault(n => profile.pets.Count >= n && !profile.collectionClaimed.Contains(n));
            if (Button(990, 325, 510, 56, collection > 0 ? "Nhận thưởng bộ sưu tập · " + collection + " loài" : "Bộ sưu tập · Mốc 5 / 10 / 15 / 20 loài", collection > 0))
            {
                profile.collectionClaimed.Add(collection); profile.Grant(db.balance["monsters"]["rewards"][collection.ToString()].ToObject<Dictionary<string, int>>()); Changed("Đã nhận thưởng bộ sưu tập");
            }
            for (int slot = 0; slot < 3; slot++)
            {
                var pet = profile.pets.Find(p => p.id == profile.petSlots[slot]); bool unlocked = slot < profile.PetCount;
                if (Button(290 + slot * 406, 400, 388, 60, !unlocked ? "Ô khóa · " + (slot == 0 ? "Cần có quái" : slot == 1 ? "5 loài hoặc cấp 10" : "10 loài hoặc cấp 25") : $"Đồng hành {slot + 1}: {(pet == null ? "Trống" : db.Monster(pet.id).name)}  ›", unlocked))
                {
                    var choices = new[] {""}.Concat(profile.pets.Select(p => p.id).Where(id => !profile.petSlots.Contains(id) || id == profile.petSlots[slot])).ToArray(); int chosenSlot = slot;
                    Pick("Chọn quái đồng hành", choices.Select(v => string.IsNullOrEmpty(v) ? "Để trống" : db.Monster(v).name), at => {profile.petSlots[chosenSlot] = choices[at]; Changed("Đã đổi đồng hành");});
                }
            }
            Surface(282, 478, 756, 321, Color.white, catsFrame:"Roster");
            Surface(1054, 478, 494, 321, Color.white, catsFrame:"Detail");
            scroll = BeginScroll(new Rect(298, 494, 724, 289), scroll, new Rect(0, 0, 702, Mathf.Ceil(profile.pets.Count / 4f) * 147));
            for (int i = 0; i < profile.pets.Count; i++)
            {
                float x = i % 4 * 176, y = i / 4 * 147; var p = profile.pets[i];
                if (IllustratedButton(x, y, 166, 137, db.Monster(p.id).name + " · Lv " + p.level, p.id,
                    content:StudioButtonLayout.Content.Tile, selected:selectedPet == i)) selectedPet = i;
            }
            EndScroll();
            if (profile.pets.Count == 0) {Text(320, 540, 670, "Mở lồng tại đây hoặc làm yếu mục tiêu rồi nhấn C trong trận để bắt quái đầu tiên.", label, 100); return;}
            selectedPet = Mathf.Clamp(selectedPet, 0, profile.pets.Count - 1); var chosen = profile.pets[selectedPet];
            Text(1078, 493, 446, db.Monster(chosen.id).name, subtitle);
            Text(1078, 535, 446, $"Cấp {chosen.level} · {chosen.stars}★ · Tiến hóa {chosen.evolution}\nMảnh: {chosen.shards} · Trait: {chosen.trait}\nIV HP/ATK/DEF/SPD: {string.Join("/", chosen.ivs)}", small, 74);
            int chow = Mathf.FloorToInt(12 * Mathf.Pow(chosen.level, 1.3f));
            if (Button(1078, 613, 446, 50, "Huấn luyện · " + chow + " thức ăn", chosen.level < 20 + 16 * chosen.stars, true)) {if (Pay(("monsterChow", chow))) {chosen.level++; profile.Track("upgrade"); Changed();}}
            int shards = new[] {20, 40, 80, 160, 280, 0}[chosen.stars];
            if (Button(1078, 672, 446, 50, "Nâng sao · " + shards + " mảnh", chosen.stars < 5)) {if (chosen.shards >= shards) {chosen.shards -= shards; chosen.stars++; Changed();} else Notify("Thiếu mảnh quái");}
            if (Button(1078, 731, 446, 50, "Tiến hóa · " + (chosen.evolution == 0 ? "3★ / Lv40 / 50 mảnh" : "5★ / Lv80 / 100 mảnh"), chosen.evolution < 2))
            {
                bool first = chosen.evolution == 0; int need = first ? 50 : 100;
                if (chosen.stars >= (first ? 3 : 5) && chosen.level >= (first ? 40 : 80) && chosen.shards >= need) {chosen.shards -= need; chosen.evolution++; Changed();} else Notify("Chưa đủ điều kiện tiến hóa");
            }
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
        void GearUI()
        {
            Text(290, 225, 1100, "Kho trang bị & xưởng rèn", title, 57);
            Text(290, 283, 1150, "Trang bị nhận từ chiến dịch. Mỗi món chỉ gán cho một mèo. Nâng sao bằng xu và đá rèn.", small);
            gearCat = Mathf.Clamp(gearCat, 0, profile.cats.Count - 1); var cat = profile.cats[gearCat];
            Surface(282, 320, 1266, 82, Color.white, catsFrame:"Roster");
            if (IllustratedButton(298, 334, 568, 54, "Mèo đang chọn: " + db.Cat(cat.id).name + "  ›", cat.id, normalSpriteKey:"blue")) Pick("Chọn mèo để trang bị", profile.cats.Select(c => db.Cat(c.id).name), at => {gearCat = at;});
            var artifact = profile.artifacts.Find(a => a.id == cat.artifact);
            if (IllustratedButton(882, 334, 650, 54, "Cổ vật: " + (artifact?.name ?? "Trống") + "  ›", "item-artifact"))
            {
                var choices = new[] {""}.Concat(profile.artifacts.Select(a => a.id)).ToArray();
                Pick("Chọn cổ vật", choices.Select(id => string.IsNullOrEmpty(id) ? "Gỡ cổ vật" : profile.artifacts.First(a => a.id == id).name), at =>
                {
                    string next = choices[at]; foreach (var c in profile.cats) if (c.artifact == next) c.artifact = "";
                    cat.artifact = next; Changed("Đã cập nhật cổ vật");
                });
            }
            Surface(282, 418, 1266, 381, Color.white, catsFrame:"Detail");
            if (profile.gear.Count == 0) {Text(306, 445, 1100, "Chưa có trang bị. Vượt ải 2–4 và 6–7 để nhận những món đầu tiên.", label); return;}
            scroll = BeginScroll(new Rect(298, 434, 1234, 349), scroll, new Rect(0, 0, 1212, Mathf.Ceil(profile.gear.Count / 4f) * 280));
            for (int i = 0; i < profile.gear.Count; i++)
            {
                var g = profile.gear[i]; float x = i % 4 * 303, y = i / 4 * 280; Panel(x, y, 291, 266); Art(x + 36, y + 10, 219, 75, GearAsset(g));
                string gearName = g.id.StartsWith("gun") ? db.Weapon(g.id).name : g.id == "gear-helm" ? "Mũ giáp" : g.id == "gear-chest" ? "Áo giáp" : "Giày hành quân";
                Text(x + 15, y + 92, 261, gearName + " · " + g.stars + "★", label, 48); Text(x + 15, y + 139, 261, g.rarity, small, 25);
                if (Button(x + 15, y + 169, 261, 40, cat.gear.Values.Contains(g.uid) ? "Tháo trang bị" : "Trang bị cho " + db.Cat(cat.id).name, normalSpriteKey:"blue"))
                {
                    if (cat.gear.Values.Contains(g.uid)) cat.gear.Remove(GearSlot(g));
                    else {foreach (var c in profile.cats) foreach (var key in c.gear.Where(v => v.Value == g.uid).Select(v => v.Key).ToArray()) c.gear.Remove(key); cat.gear[GearSlot(g)] = g.uid;}
                    Changed();
                }
                if (Button(x + 15, y + 216, 261, 40, $"Rèn · {200 * (g.stars + 1)} xu / {5 + g.stars * 5} đá", g.stars < 5)) {if (Pay(("coins", 200 * (g.stars + 1)), ("forgeStone", 5 + g.stars * 5))) {g.stars++; profile.Track("upgrade"); Changed();}}
            }
            EndScroll();
        }
        void PresetsUI()
        {
            Text(290, 225, 1150, "Ba đội hình dự phòng", title, 57);
            Text(290, 285, 1150, "Lưu bố trí tầng, bộ binh, kỹ năng và quái đồng hành. Nâng cấp nhân vật dùng chung giữa các đội.", small);
            for (int i = 0; i < 3; i++)
            {
                float y = 341 + i * 140;
                Surface(282, y, 1266, 118, Color.white, catsFrame:"Detail");
                Art(308, y + 26, 64, 64, "Book_1_Purple");
                Text(390, y + 39, 460, "ĐỘI " + (i + 1) + (profile.presets[i] == null ? " · Chưa lưu" : " · Sẵn sàng"), subtitle);
                if (Button(862, y + 28, 308, 62, "Lưu đội hiện tại", normalSpriteKey:"blue")) {profile.SavePreset(i); Notify("Đã lưu đội " + (i + 1));}
                if (Button(1186, y + 28, 336, 62, "Dùng đội này", profile.presets[i] != null, true)) {profile.LoadPreset(i); Changed("Đã áp dụng đội " + (i + 1));}
            }
        }
        void NexusUI()
        {
            Text(290, 225, 1150, "Cộng hưởng ánh trăng", title, 57);
            var anchors = profile.cats.OrderByDescending(c => c.level).Take(5).ToList();
            for (int i = 0; i < profile.nexus.Length; i++) if (anchors.Any(c => c.id == profile.nexus[i])) profile.nexus[i] = "";
            int level = anchors.Count == 5 ? anchors.Min(c => c.level) : 0;
            Text(290, 288, 1150, "5 mèo cấp cao nhất làm neo. Mèo trong ô nhận cấp của neo thấp nhất, giới hạn theo bậc; không tiêu XP.", small);
            Surface(282, 323, 1266, 190, Color.white, catsFrame:"Roster");
            for (int i = 0; i < 5; i++)
            {
                float x = 298 + i * 248; var c = i < anchors.Count ? anchors[i] : null;
                Surface(x, 337, 232, 160, Color.white, catsFrame:"Tile");
                if (c != null) Art(x + 41, 343, 150, 108, c.id);
                Text(x + 15, 456, 206, c == null ? "Cần thêm mèo neo" : db.Cat(c.id).name + " · Cấp " + c.level, label);
            }
            Surface(282, 526, 1266, 242, Color.white, catsFrame:"Detail");
            Text(298, 542, 1234, "CẤP CỘNG HƯỞNG: " + level + "  ·  " + profile.NexusSlots + "/8 ô mở", subtitle);
            for (int i = 0; i < 8; i++)
            {
                float x = 298 + i % 4 * 310, y = 590 + i / 4 * 81; string current = profile.nexus[i];
                if (IllustratedButton(x, y, 300, 65, i >= profile.NexusSlots ? "Chưa mở" : "Ô " + (i + 1) + ": " + (string.IsNullOrEmpty(current) ? "Trống" : db.Cat(current).name) + "  ›", i >= profile.NexusSlots ? "Lock" : string.IsNullOrEmpty(current) ? "item-soulEssence" : current, i < profile.NexusSlots))
                {
                    var options = new[] {""}.Concat(profile.cats.Where(c => !anchors.Contains(c) && (!profile.nexus.Contains(c.id) || c.id == current)).Select(c => c.id)).ToArray();
                    int chosenSlot = i;
                    Pick("Chọn mèo Cộng hưởng", options.Select(id => string.IsNullOrEmpty(id) ? "Để trống ô" : db.Cat(id).name), at => {profile.nexus[chosenSlot] = options[at]; Changed("Đã cập nhật Cộng hưởng");});
                }
            }
            Text(298, 781, 1210, "Thêm ô khi vượt ải 4 / 6 / 8 / 10 / 13. Mèo đang Cộng hưởng phải rời ô trước khi nâng cấp trực tiếp.", small, 25);
        }
        void Arena()
        {
            Text(290, 227, 1210, "Đấu trường trăng · mô phỏng local", title, 57);
            Text(290, 286, 1224, "Đối thủ mô phỏng dùng đội quái theo tiến độ, có AI kỹ năng. Điểm Elo tính theo rating đối thủ. Chưa có online.", label, 56);
            Surface(282, 347, 1266, 266, Color.white, catsFrame:"Roster");
            for (int i = 0; i < Opponent.All.Length; i++)
            {
                var o = Opponent.All[i]; float x = 298 + i % 3 * 414, y = 365 + i / 3 * 114;
                if (Button(x, y, 398, 100, $"{o.name} · Cấp {o.level}\n{o.rating} điểm", true, selectedOpponent == i, "blue")) selectedOpponent = i;
            }
            var selected = Opponent.All[selectedOpponent];
            Surface(282, 631, 1266, 168, Color.white, catsFrame:"Detail");
            Text(306, 653, 1198, $"Bạn: {profile.rating} điểm · Thắng +{Battle.RatingDelta(profile.rating, selected.rating, true)} / Thua {Battle.RatingDelta(profile.rating, selected.rating, false)} · {profile.Get("arenaToken")} huy hiệu · Tối đa 40/ngày", subtitle, 55);
            if (IllustratedButton(306, 719, 1218, 60, profile.highestCleared >= 5 ? "VÀO ĐẤU TRƯỜNG" : "VƯỢT ẢI 5 ĐỂ MỞ KHÓA", "item-arenaToken", profile.highestCleared >= 5, true)) StartBattle(false, true);
        }
    }
}
