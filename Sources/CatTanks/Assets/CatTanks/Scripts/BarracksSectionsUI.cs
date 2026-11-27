using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    // Các mục con của Doanh trại, mỗi mục theo bố cục của một màn quen thuộc trong game cùng dòng
    // (chỉ lấy cách sắp xếp; art, khung và chữ vẫn là của CatTanks):
    //  • Đội hình   ← màn Formation của AFK Arena: ô vị trí ở trên, dải mèo ở dưới, chạm để xếp.
    //  • Cộng hưởng ← Resonating Crystal của AFK Arena: pha lê giữa, 5 mèo neo xếp vòng cung, hàng ô ở dưới.
    //  • Kỹ năng    ← Battle Deck của Clash Royale: bộ đang dùng ở trên, lưới bộ sưu tập, bảng chi tiết.
    //  • Pháo đài   ← Hangar/Workshop của War Robots: danh sách pháo trái, pháo lớn giữa, ô module, bảng nâng cấp phải.
    //  • Quái thú   ← kho quái kiểu Pokémon GO: thanh bộ sưu tập, lưới quái, chi tiết với Huấn luyện / Tiến hóa.
    public sealed partial class CatTanksGame
    {
        const string FortView="barracks-fort",TeamView="barracks-team",SavesView="barracks-saves",SkillView="barracks-skill",PetView="barracks-pet",CrystalView="barracks-crystal";
        string teamSlot="op-0";
        int skillSlotPick,selectedSkill,teamCatPage;
        static readonly string[] SkillCardFrames = { "Gold", "Violet", "Cyan", "Blue", "Violet", "Violet", "Blue", "Violet", "Gold", "Detail" };
        static readonly Color[] SkillCardColors = { Color.white, C("#ff94bf"), Color.white, C("#9ef178"), Color.white,
            Color.white, C("#b7ddff"), C("#ff9bd9"), C("#ffaf67"), C("#ffadba") };
        static readonly Vector2[] AnchorCards = { new Vector2(227, 242), new Vector2(468, 160), new Vector2(713, 118),
            new Vector2(958, 160), new Vector2(1199, 242) };

        string BarracksSectionView()=>tab switch
        {
            "fortress"=>FortView,"squad"=>TeamView,"presets"=>SavesView,"skills"=>SkillView,"monsters"=>PetView,"nexus"=>CrystalView,_=>RosterView
        };
        void TeamTabs()
        {
            if(TabButton(1010,104,220,52,"BỐ TRÍ",tab=="squad","Interface/Cats/Tab-squad")){tab="squad";}
            if(TabButton(1246,104,220,52,"ĐỘI LƯU",tab=="presets","Interface/Cats/Tab-presets")){tab="presets";}
        }

        // ================= Đội hình (Formation) =================
        void TeamUI()
        {
            Text(130,108,860,"Bố trí trước giờ xuất kích",title,55);
            TeamTabs();

            // Tháp pháo đài: tầng 1 ở dưới cùng, mỗi tầng có ô pháo và ô mèo vận hành.
            Surface(118,170,660,462,Color.white,catsFrame:"Roster");
            for(int i=0;i<5;i++)
            {
                float y=184+(4-i)*88;var f=profile.floors[i];bool open=i<profile.FloorCount;
                Text(134,y+22,92,"Tầng "+(i+1),subtitle,36);
                if(IllustratedButton(230,y,250,78,open?db.Weapon(f.turret).name:"Cấp "+i*20,open?f.turret:"Lock",open,selected:teamSlot=="tur-"+i))teamSlot="tur-"+i;
                bool empty=string.IsNullOrEmpty(f.cat);
                if(IllustratedButton(494,y,268,78,!open?"Chưa mở":empty?"Ghế trống":db.Cat(f.cat).name,!open?"Lock":empty?"hat-0":f.cat,open,selected:teamSlot=="op-"+i,normalSpriteKey:"blue"))teamSlot="op-"+i;
            }

            // Bộ binh: 5 ô thẻ lớn.
            Surface(794,170,688,462,Color.white,catsFrame:"Detail");
            Text(814,182,640,"BỘ BINH",subtitle,36);
            int[] unlockAt={1,1,8,15,25};
            for(int i=0;i<5;i++)
            {
                bool open=i<profile.SquadCount;string id=profile.squad[i];
                if(IllustratedButton(810+i*132,226,124,168,!open?"Cấp "+unlockAt[i]:string.IsNullOrEmpty(id)?"Dân quân":db.Cat(id).name,!open?"Lock":string.IsNullOrEmpty(id)?"cat-0":id,open,content:StudioButtonLayout.Content.Tile,selected:teamSlot=="inf-"+i))teamSlot="inf-"+i;
                if(!dispatchOnly&&open&&string.IsNullOrEmpty(id)&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
                Enlarge(18);
            }
            float specialty=db.B("cats.specialtyBonus");
            Text(814,406,652,$"Mèo đúng chuyên môn nhận thêm {specialty:0%} ATK và HP. Ghế trống: pháo giữ 70% sát thương. Ô bộ binh trống dùng dân quân. Đội hình khóa lúc bắt đầu trận.",small,80);
            Surface(810,500,656,116,Color.white,catsFrame:"Ink");
            Text(828,512,620,"ĐANG CHỌN: "+TeamSlotName(),subtitle,36);
            bool turretSlot=teamSlot.StartsWith("tur-",StringComparison.Ordinal);
            if(Button(828,554,240,50,"Gỡ khỏi ô",!turretSlot&&!string.IsNullOrEmpty(TeamSlotValue()),normalSpriteKey:"blue")){SetTeamSlot("");}
            Text(1084,562,370,turretSlot?"Chọn pháo ở dải dưới để đổi.":"Chọn mèo ở dải dưới để xếp vào ô.",small,30);

            // Dải chọn ở đáy: pháo khi đang chọn ô pháo, còn lại là mèo.
            Surface(118,646,1364,244,Color.white,catsFrame:"Ink");
            Text(134,654,800,turretSlot?"THÁP PHÁO ĐANG CÓ":"MÈO TRONG DOANH TRẠI",subtitle,36);
            if(turretSlot)
            {
                for(int i=0;i<profile.turrets.Count&&i<10;i++)
                {
                    var t=profile.turrets[i];int floor=profile.floors.FindIndex(v=>v.turret==t.id);
                    if(IllustratedButton(134+i*134,694,124,180,"Cấp "+t.level,t.id,content:StudioButtonLayout.Content.Tile,selected:TeamSlotValue()==t.id))SetTeamSlot(t.id);
                    Enlarge(17);
                    if(floor>=0)Text(134+i*134,700,124,"Tầng "+(floor+1),small,26,align:TextAnchor.UpperCenter);
                }
            }
            else
            {
                // 10 mèo mỗi trang; có hơn 10 mèo thì hiện nút lật trang.
                int pages=Mathf.Max(1,(profile.cats.Count+9)/10);teamCatPage=Mathf.Clamp(teamCatPage,0,pages-1);
                if(pages>1)
                {
                    Text(1180,660,110,$"{teamCatPage+1}/{pages}",small,30,align:TextAnchor.UpperCenter);
                    if(Button(1300,652,80,40,"‹",teamCatPage>0,normalSpriteKey:"blue"))teamCatPage--;
                    if(Button(1390,652,80,40,"›",teamCatPage<pages-1,normalSpriteKey:"blue"))teamCatPage++;
                }
                for(int k=0;k<10;k++)
                {
                    int i=teamCatPage*10+k;if(i>=profile.cats.Count)break;
                    var c=profile.cats[i];string place=TeamPlace(c.id);
                    if(IllustratedButton(134+k*134,694,124,180,db.Cat(c.id).name,c.id,content:StudioButtonLayout.Content.Tile,selected:TeamSlotValue()==c.id))SetTeamSlot(c.id);
                    Enlarge(17);
                    if(place!=null)Text(134+k*134,700,124,place,small,26,align:TextAnchor.UpperCenter);
                }
            }
        }
        string TeamSlotName()
        {
            int i=teamSlot[teamSlot.Length-1]-'0';
            return teamSlot.StartsWith("tur-",StringComparison.Ordinal)?$"Tháp pháo tầng {i+1}":teamSlot.StartsWith("op-",StringComparison.Ordinal)?$"Mèo vận hành tầng {i+1}":$"Bộ binh {i+1}";
        }
        string TeamSlotValue()
        {
            int i=teamSlot[teamSlot.Length-1]-'0';
            return teamSlot.StartsWith("tur-",StringComparison.Ordinal)?profile.floors[i].turret:teamSlot.StartsWith("op-",StringComparison.Ordinal)?profile.floors[i].cat:profile.squad[i];
        }
        // Mèo đang ở đâu trong đội hình (để ghi nhãn trên dải chọn).
        string TeamPlace(string id)
        {
            int floor=profile.floors.FindIndex(f=>f.cat==id);if(floor>=0)return "Tầng "+(floor+1);
            int squad=Array.IndexOf(profile.squad,id);return squad>=0?"Bộ binh "+(squad+1):null;
        }
        void SetTeamSlot(string value)
        {
            int i=teamSlot[teamSlot.Length-1]-'0';
            if(teamSlot.StartsWith("tur-",StringComparison.Ordinal))
            {
                if(i>=profile.FloorCount||string.IsNullOrEmpty(value))return;
                var f=profile.floors[i];if(f.turret==value)return;
                var other=profile.floors.Find(v=>v!=f&&v.turret==value);if(other!=null)other.turret=f.turret;
                f.turret=value;Changed("Đã đổi pháo");return;
            }
            bool op=teamSlot.StartsWith("op-",StringComparison.Ordinal);
            if(op?i>=profile.FloorCount:i>=profile.SquadCount){Notify("Ô này chưa mở");return;}
            string current=op?profile.floors[i].cat:profile.squad[i];
            if(current==value)return;
            // Mỗi mèo chỉ giữ một vị trí: rời chỗ cũ trước khi vào ô mới.
            if(!string.IsNullOrEmpty(value))
            {
                foreach(var f in profile.floors)if(f.cat==value)f.cat="";
                for(int k=0;k<profile.squad.Length;k++)if(profile.squad[k]==value)profile.squad[k]="";
            }
            if(op)profile.floors[i].cat=value;else profile.squad[i]=value;
            Changed("Đội hình đã cập nhật");
        }

        // ================= Đội lưu =================
        void SavesUI()
        {
            Text(130,108,860,"Ba đội hình dự phòng",title,55);
            TeamTabs();
            Text(130,170,1340,"Lưu bố trí tầng, bộ binh, kỹ năng và quái đồng hành. Nâng cấp nhân vật dùng chung giữa các đội.",label,36);
            for(int i=0;i<3;i++)
            {
                float x=118+i*460;var p=profile.presets[i];
                Surface(x,222,444,660,Color.white,catsFrame:p==null?"Detail":"Roster");
                Art(x+20,240,64,64,"Book_1_Purple");
                Text(x+96,244,330,"ĐỘI "+(i+1),title,48,size:36);
                Text(x+96,284,330,p==null?"Chưa lưu":"Sẵn sàng",subtitle,32);
                // Xem nhanh: mèo vận hành và bộ binh của đội đã lưu.
                var cats=p==null?new List<string>():p.floors.Select(f=>f.cat).Concat(p.squad).Where(id=>!string.IsNullOrEmpty(id)).Take(8).ToList();
                if(cats.Count==0)Text(x+20,360,404,p==null?"Bấm LƯU để ghi lại đội đang dùng.":"Đội này chưa có mèo.",small,60,align:TextAnchor.UpperCenter);
                for(int k=0;k<cats.Count;k++)
                {
                    float cx=x+20+k%4*102,cy=330+k/4*110;
                    Surface(cx,cy,96,100,Color.white,catsFrame:"Tile");Art(cx+8,cy+6,80,80,cats[k]);
                }
                if(p!=null)Text(x+20,560,404,"Kỹ năng: "+string.Join(", ",p.skills.Where(s=>!string.IsNullOrEmpty(s)).Select(s=>db.skills.Find(d=>d.id==s)?.name??s)),small,90);
                if(Button(x+20,700,404,66,"LƯU ĐỘI HIỆN TẠI",normalSpriteKey:"blue")){profile.SavePreset(i);Notify("Đã lưu đội "+(i+1));}
                Enlarge(22);
                if(Button(x+20,780,404,80,"DÙNG ĐỘI NÀY",p!=null,true)){profile.LoadPreset(i);Changed("Đã áp dụng đội "+(i+1));}
                Enlarge(26);
            }
        }


        // ================= Kỹ năng =================
        void SkillDeckUI()
        {
            SectionBackdrop();
            Art(52, 91, 64, 66, "Book_1_Purple");
            Text(124, 98, 824, "Kỹ năng chiến thuật", title, 54, size: 46);
            Art(1010, 102, 39, 39, "Interface/Cats/InfoIcon");
            Text(1058, 108, 486, "Lõi kỹ năng nhận ở ải trùm · phím 1–4 trong trận", small, 38, size: 21);

            SectionPanel(40, 158, 1520, 154);
            Surface(58, 175, 308, 118, Color.white, catsFrame: "CombatPlate");
            Text(76, 184, 286, "BỘ KỸ NĂNG", subtitle, 36, size: 29);
            Text(76, 221, 270, "Chọn một ô, rồi chọn kỹ năng ở dưới và bấm DÙNG.", small, 64, size: 19);
            for (int slot = 0; slot < 4; slot++)
            {
                string id = profile.skillSlots[slot];
                var skill = db.skills.Find(s => s.id == id);
                float x = 378 + slot * 291;
                string caption = skill == null ? "Ô " + (slot + 1) + " · Trống" : $"[{slot + 1}] {skill.name}";
                if (IllustratedButton(x, 175, 278, 118, caption, skill == null ? "Interface/Barracks/Plus" : "skill-" + id,
                    selected: skillSlotPick == slot))
                {
                    skillSlotPick = slot;
                    int at = db.skills.FindIndex(s => s.id == id);
                    if (at >= 0) selectedSkill = at;
                }
                if (!dispatchOnly)
                {
                    if (lastButton.layout) lastButton.layout.iconSize = 82;
                    int palette = db.skills.IndexOf(skill);
                    if (skillSlotPick != slot && palette >= 0 && palette < SkillCardFrames.Length)
                    {
                        StudioNineSlice.Apply(lastButton.image, CatsFrame(SkillCardFrames[palette]), 18);
                        lastButton.image.color = SkillCardColors[palette];
                    }
                }
                Enlarge(25);
            }

            SectionPanel(40, 320, 990, 536);
            for (int i = 0; i < db.skills.Count && i < SkillCardFrames.Length; i++)
            {
                var skill = db.skills[i];
                bool open = profile.SkillUnlocked(skill);
                float x = 62 + i % 5 * 191, y = 342 + i / 5 * 244;
                bool selected = selectedSkill == i;
                if (SectionTile(x, y, 179, 232, skill.name, "skill-" + skill.id,
                    selected ? "Gold" : SkillCardFrames[i], tint: selected ? Color.white : SkillCardColors[i]))
                    selectedSkill = i;
                Text(x + 13, y + 10, 112, open ? "Lv " + SkillLevel(skill.id) : "KHÓA", subtitle, 36, size: 25);
                int equippedSlot = Array.IndexOf(profile.skillSlots, skill.id);
                if (equippedSlot >= 0)
                    Text(x + 106, y + 10, 57, $"[{equippedSlot + 1}]", title, 34, align: TextAnchor.UpperRight, size: 25);
                if (!open) Art(x + 68, y + 135, 43, 47, "Lock");
            }

            selectedSkill = Mathf.Clamp(selectedSkill, 0, db.skills.Count - 1);
            var pick = db.skills[selectedSkill];
            bool unlocked = profile.SkillUnlocked(pick);
            int level = SkillLevel(pick.id);
            SectionPanel(1042, 320, 518, 536);
            Surface(1060, 336, 482, 216, Color.white, catsFrame: "Blue");
            Text(1076, 346, 451, pick.name, title, 58, size: 47);
            Art(1194, 393, 216, 142, "skill-" + pick.id);
            Surface(1073, 514, 147, 44, Color.white, catsFrame: "CombatPlate");
            Text(1084, 519, 125, $"Cấp {level} / 8", subtitle, 37, align: TextAnchor.UpperCenter, size: 23);
            if (unlocked)
            {
                Text(1090, 565, 419, $"{pick.baseCost} năng lượng · Hồi {pick.baseCd}s", subtitle, 36, size: 24);
                Text(1090, 606, 419, pick.duration > 0 ? $"{pick.duration}s hiệu lực" : "Kích hoạt tức thì", label, 36, size: 22);
            }
            else
            {
                string requirement = pick.unlock["stage"] != null ? "ải " + pick.unlock["stage"] : "cấp " + pick.unlock["accountLevel"];
                Text(1090, 565, 419, "Mở tại " + requirement, label, 72, size: 25);
            }
            int inDeck = Array.IndexOf(profile.skillSlots, pick.id);
            if (inDeck >= 0)
            {
                if (Button(1073, 665, 456, 64, $"GỠ KHỎI Ô [{inDeck + 1}]", normalSpriteKey: "blue"))
                {
                    if (profile.skillSlots.Count(v => !string.IsNullOrEmpty(v)) == 1) Notify("Giữ ít nhất một kỹ năng trong đội hình");
                    else { profile.skillSlots[inDeck] = ""; Changed("Đã gỡ kỹ năng"); }
                }
            }
            else if (Button(1073, 665, 456, 64, $"DÙNG VÀO Ô [{skillSlotPick + 1}]", unlocked, normalSpriteKey: "blue"))
            {
                profile.skillSlots[skillSlotPick] = pick.id;
                Changed("Đã đổi kỹ năng");
            }
            Enlarge(25);
            if (IllustratedButton(1073, 742, 456, 87, level >= 8 ? "ĐẠT CẤP TỐI ĐA" : $"NÂNG CẤP · {level} LÕI",
                "item-skillCore", unlocked && level < 8, primary: true) && Pay(("skillCore", level)))
            {
                profile.skillLevels[pick.id] = level + 1;
                Changed();
            }
            Enlarge(29);
        }
        int SkillLevel(string id) => profile.skillLevels.TryGetValue(id, out int n) ? n : 1;

        // ================= Pháo đài =================
        void FortWorkshopUI()
        {
            SectionBackdrop();
            int hpCost = Mathf.FloorToInt(400 * Mathf.Pow(profile.hpLevel, 1.4f));
            SectionPanel(64, 100, 1472, 119);
            Art(101, 120, 250, 76, "fortress-chassis");
            Text(376, 117, 740, $"PHÁO ĐÀI · HP {N(profile.MaxHp)} · {profile.FloorCount}/5 tầng", subtitle, 39, size: 30);
            Text(376, 164, 740, $"Giáp cấp {profile.hpLevel} · Mở tầng tại cấp chỉ huy 20 / 40 / 60 / 80", small, 38, size: 21);
            if (IllustratedButton(1130, 122, 372, 71, $"GIA CỐ · {N(hpCost)} xu", "Gear_Sword",
                profile.hpLevel < 240, normalSpriteKey: "blue") && Pay(("coins", hpCost)))
            {
                profile.hpLevel++;
                profile.Track("upgrade");
                Changed("Pháo đài được gia cố");
            }
            Enlarge(26);

            SectionPanel(64, 228, 420, 642);
            for (int i = 0; i < profile.turrets.Count && i < 10; i++)
            {
                var item = profile.turrets[i];
                float x = 88 + i % 2 * 183, y = 247 + i / 2 * 119;
                if (SectionTile(x, y, 174, 111, "Cấp " + item.level, item.id, selectedTurret == i ? "Cyan" : "Tile"))
                    selectedTurret = i;
                if (item.stars > 0)
                    Text(x + 8, y + 3, 104, new string('★', item.stars), title, 32, size: 18);
            }

            selectedTurret = Mathf.Clamp(selectedTurret, 0, profile.turrets.Count - 1);
            var turret = profile.turrets[selectedTurret];
            var weapon = db.Weapon(turret.id);
            int cap = 40 + 8 * turret.stars;
            Art(481, 455, 578, 135, "Interface/Barracks/Podium");
            Art(532, 227, 478, 301, turret.id);
            int floor = profile.floors.FindIndex(v => v.turret == turret.id);
            Surface(585, 592, 373, 49, Color.white, catsFrame: "CombatPlate");
            Text(594, 596, 355, floor >= 0 ? $"ĐANG LẮP Ở TẦNG {floor + 1}" : "CHƯA LẮP · đổi ở mục Đội hình",
                title, 38, align: TextAnchor.UpperCenter, size: 23);
            for (int slot = 0; slot < 2; slot++)
            {
                string id = turret.modules[slot];
                bool empty = string.IsNullOrEmpty(id);
                float x = 526 + slot * 248;
                if (SectionTile(x, 650, 235, 201, empty ? $"Lắp chim {slot + 1}" : db.Weapon(id).name,
                    empty ? "Interface/Barracks/Plus" : id, "Tile"))
                {
                    var options = new[] { "" }.Concat(profile.modules).ToArray();
                    int chosen = slot;
                    Pick("Chọn chim " + (slot + 1), options.Select(v => string.IsNullOrEmpty(v) ? "Gỡ chim" : db.Weapon(v).name),
                        at => { turret.modules[chosen] = options[at]; Changed("Đã đổi chim"); },
                        options.Select(v => string.IsNullOrEmpty(v) ? "Interface/Barracks/Plus" : v));
                }
                Text(x + 12, 657, 211, $"MODULE {slot + 1}", subtitle, 33, align: TextAnchor.UpperCenter, size: 23);
            }

            SectionPanel(1056, 228, 480, 642);
            Text(1082, 245, 434, weapon.name, title, 60, size: 43);
            Text(1082, 307, 425, new string('★', turret.stars) + new string('☆', 5 - turret.stars), title, 42, size: 31);
            Text(1082, 354, 425, $"Cấp {turret.level} / {cap}", subtitle, 39, size: 29);
            Text(1082, 401, 425, weapon.description, small, 106, size: 23);
            int coin = Mathf.FloorToInt(60 * Mathf.Pow(turret.level, 1.4f)), dust = Mathf.FloorToInt(30 * Mathf.Pow(turret.level, 1.3f));
            int starCoin = 200 * (turret.stars + 1), starStone = 5 + 5 * turret.stars;
            if (Button(1080, 523, 432, 65, turret.stars < 5 ? "NÂNG SAO · " + Cost(("coins", starCoin), ("forgeStone", starStone)) :
                "ĐÃ ĐỦ 5 SAO", turret.stars < 5) && Pay(("coins", starCoin), ("forgeStone", starStone)))
            {
                turret.stars++;
                Changed("Đã nâng sao pháo");
            }
            Enlarge(22);
            Surface(1076, 604, 440, 243, Color.white, catsFrame: "CombatPlate");
            Art(1093, 622, 44, 44, RewardIcon("coins"));
            Text(1146, 628, 138, N(coin), subtitle, 40, size: 29);
            Art(1283, 622, 44, 44, "item-spiritDust");
            Text(1336, 628, 155, N(dust), subtitle, 40, size: 29);
            Text(1094, 672, 400, turret.level >= cap ? "Cần nâng sao để tăng giới hạn cấp" : $"Cấp {turret.level} → {turret.level + 1}", small, 38, size: 22);
            if (Button(1092, 715, 408, 109, "NÂNG CẤP",
                turret.level < cap && profile.Get("coins") >= coin && profile.Get("spiritDust") >= dust, true) &&
                Pay(("coins", coin), ("spiritDust", dust)))
            {
                turret.level++;
                profile.Track("upgrade");
                Changed();
            }
            Enlarge(35);
        }


        // ================= Quái thú (kho quái) =================
        void PetStorageUI()
        {
            Surface(118,108,1364,110,Color.white,catsFrame:"Ink");
            Text(134,112,760,"Hang quái đồng hành",title,55,size:38);
            Text(134,166,760,$"{profile.Get("monsterChow")} thức ăn · Bắt quái: làm yếu mục tiêu rồi nhấn C trong trận",small,30);
            if(IllustratedButton(910,122,276,82,$"Lồng thú ×{profile.Get("beastCage")}","item-beastCage",profile.Get("beastCage")>0))OpenCage(false);
            Enlarge(20);
            if(IllustratedButton(1196,122,276,82,$"Lồng trăng ×{profile.Get("moonCage")}","item-moonCage",profile.Get("moonCage")>0,true))OpenCage(true);
            Enlarge(20);

            // Ba ô đồng hành.
            Surface(118,232,900,110,Color.white,catsFrame:"Detail");
            for(int slot=0;slot<3;slot++)
            {
                var pet=profile.pets.Find(p=>p.id==profile.petSlots[slot]);bool open=slot<profile.PetCount;
                string caption=!open?(slot==0?"Cần có quái":slot==1?"5 loài / cấp 10":"10 loài / cấp 25"):pet==null?$"Đồng hành {slot+1}: trống":db.Monster(pet.id).name;
                if(IllustratedButton(134+slot*296,246,282,82,caption,!open?"Lock":pet==null?"item-beastCage":pet.id,open,normalSpriteKey:"blue"))
                {
                    var choices=new[]{""}.Concat(profile.pets.Select(p=>p.id).Where(id=>!profile.petSlots.Contains(id)||id==profile.petSlots[slot])).ToArray();int chosen=slot;
                    Pick("Chọn quái đồng hành",choices.Select(v=>string.IsNullOrEmpty(v)?"Để trống":db.Monster(v).name),at=>{profile.petSlots[chosen]=choices[at];Changed("Đã đổi đồng hành");},choices.Select(v=>string.IsNullOrEmpty(v)?"item-beastCage":v));
                }
                Enlarge(19);
            }

            // Bộ sưu tập: thanh tới mốc kế tiếp.
            int[] marks={5,10,15,20};
            int claimable=marks.FirstOrDefault(n=>profile.pets.Count>=n&&!profile.collectionClaimed.Contains(n));
            int next=marks.FirstOrDefault(n=>!profile.collectionClaimed.Contains(n));
            Surface(1034,232,448,110,Color.white,catsFrame:"Ink");
            Text(1050,240,420,$"BỘ SƯU TẬP {profile.pets.Count}/20",subtitle,36);
            Bar(1050,278,170,18,next>0?Mathf.Clamp01(profile.pets.Count/(float)next):1,cyan);
            Text(1050,302,170,next>0?"Mốc "+next+" loài":"Đã đủ mốc",small,30);
            if(Button(1232,276,236,56,claimable>0?"NHẬN THƯỞNG":"CHƯA ĐẠT",claimable>0,claimable>0))
            {
                profile.collectionClaimed.Add(claimable);profile.Grant(db.balance["monsters"]["rewards"][claimable.ToString()].ToObject<Dictionary<string,int>>());Changed("Đã nhận thưởng bộ sưu tập");
            }

            // Lưới quái và chi tiết.
            Surface(118,356,900,534,Color.white,catsFrame:"Roster");
            Surface(1034,356,448,534,Color.white,catsFrame:"Detail");
            if(profile.pets.Count==0)
            {
                Text(150,560,836,"Chưa có quái. Mở lồng ở trên hoặc làm yếu mục tiêu rồi nhấn C trong trận để bắt quái đầu tiên.",label,100,align:TextAnchor.UpperCenter);
                Text(1058,560,400,"Chọn một quái để xem chi tiết.",label,60,align:TextAnchor.UpperCenter);
                return;
            }
            scroll=BeginScroll(new Rect(134,372,868,502),scroll,new Rect(0,0,850,Mathf.Ceil(profile.pets.Count/5f)*166));
            for(int i=0;i<profile.pets.Count;i++)
            {
                var p=profile.pets[i];float x=i%5*172,y=i/5*166;
                if(IllustratedButton(x,y,160,156,db.Monster(p.id).name,p.id,content:StudioButtonLayout.Content.Tile,selected:selectedPet==i))selectedPet=i;
                Enlarge(17);
                Text(x+8,y+6,70,"Lv "+p.level,subtitle,30);
                if(p.stars>0)Text(x+70,y+6,82,new string('★',p.stars),title,30,align:TextAnchor.UpperRight,size:16);
            }
            EndScroll();

            selectedPet=Mathf.Clamp(selectedPet,0,profile.pets.Count-1);var chosenPet=profile.pets[selectedPet];
            Text(1058,370,400,db.Monster(chosenPet.id).name,title,56,size:34);
            Art(1148,424,220,170,chosenPet.id);
            Text(1058,600,400,$"Cấp {chosenPet.level} · {chosenPet.stars}★ · Tiến hóa {chosenPet.evolution}\nMảnh {chosenPet.shards} · Đặc tính {chosenPet.trait}\nIV HP/ATK/DEF/SPD: {string.Join("/",chosenPet.ivs)}",small,80);
            int chow=Mathf.FloorToInt(12*Mathf.Pow(chosenPet.level,1.3f));
            int shards=new[]{20,40,80,160,280,0}[chosenPet.stars];
            if(Button(1050,690,200,58,"Nâng sao · "+shards,chosenPet.stars<5,normalSpriteKey:"blue")){if(chosenPet.shards>=shards){chosenPet.shards-=shards;chosenPet.stars++;Changed();}else Notify("Thiếu mảnh quái");}
            if(Button(1264,690,202,58,"Tiến hóa",chosenPet.evolution<2,normalSpriteKey:"blue"))
            {
                bool first=chosenPet.evolution==0;int need=first?50:100;
                if(chosenPet.stars>=(first?3:5)&&chosenPet.level>=(first?40:80)&&chosenPet.shards>=need){chosenPet.shards-=need;chosenPet.evolution++;Changed();}
                else Notify(first?"Cần 3★, cấp 40 và 50 mảnh":"Cần 5★, cấp 80 và 100 mảnh");
            }
            if(Button(1050,762,416,112,$"HUẤN LUYỆN · {chow} thức ăn",chosenPet.level<20+16*chosenPet.stars,true)){if(Pay(("monsterChow",chow))){chosenPet.level++;profile.Track("upgrade");Changed();}}
            Enlarge(24);
        }


        // ================= Cộng hưởng =================
        void CrystalUI()
        {
            var anchors = profile.cats.OrderByDescending(c => c.level).Take(5).ToList();
            for (int i = 0; i < profile.nexus.Length; i++)
                if (anchors.Any(c => c.id == profile.nexus[i])) profile.nexus[i] = "";
            int level = anchors.Count == 5 ? anchors.Min(c => c.level) : 0;

            SectionBackdrop();
            Art(103, 99, 50, 49, "Interface/Barracks/Moon");
            Text(166, 101, 540, "Cộng hưởng ánh trăng", title, 53, size: 40);
            Text(1115, 115, 354, "5 mèo cấp cao nhất làm neo", small, 33, align: TextAnchor.UpperRight, size: 22);
            Art(264, 207, 1072, 254, "Interface/Barracks/ResonanceArc");
            Art(354, 337, 892, 239, "Interface/Barracks/Podium");

            for (int i = 0; i < AnchorCards.Length; i++)
            {
                var cat = i < anchors.Count ? anchors[i] : null;
                float x = AnchorCards[i].x, y = AnchorCards[i].y;
                Surface(x, y, 174, 188, Color.white, catsFrame: cat == null ? "CombatPlate" : "Violet");
                if (cat != null)
                {
                    Art(x + 21, y + 12, 132, 111, cat.id);
                    Text(x + 7, y + 118, 160, db.Cat(cat.id).name, subtitle, 37, align: TextAnchor.UpperCenter, size: 23);
                    Text(x + 7, y + 147, 160, $"Cấp {cat.level}", title, 35, align: TextAnchor.UpperCenter, size: 22);
                }
                else
                {
                    Art(x + 58, y + 37, 58, 65, "Lock");
                    Text(x + 7, y + 121, 160, "Cần thêm mèo", small, 44, align: TextAnchor.UpperCenter, size: 19);
                }
            }

            Art(718, 302, 164, 169, "item-soulEssence");
            Text(600, 478, 400, "CẤP CỘNG HƯỞNG", subtitle, 43, align: TextAnchor.UpperCenter, size: 30);
            Surface(702, 517, 196, 62, Color.white, catsFrame: "Violet");
            Text(713, 510, 174, level.ToString(), title, 67, align: TextAnchor.UpperCenter, size: 54);

            SectionPanel(64, 590, 1472, 277);
            Art(87, 608, 45, 40, "Chest_Gold");
            Text(145, 610, 810, $"Ô CỘNG HƯỞNG · {profile.NexusSlots}/8 ô mở", subtitle, 44, size: 28);
            for (int i = 0; i < 8; i++)
            {
                float x = 104 + i * 175;
                string current = profile.nexus[i];
                bool open = i < profile.NexusSlots, empty = string.IsNullOrEmpty(current);
                if (SectionTile(x, 651, 164, 150, !open ? "Chưa mở" : empty ? "Trống" : db.Cat(current).name,
                    !open ? "Lock" : empty ? "item-soulEssence" : current, empty || !open ? "Tile" : "Violet", open))
                {
                    var options = new[] { "" }.Concat(profile.cats
                        .Where(c => !anchors.Contains(c) && (!profile.nexus.Contains(c.id) || c.id == current))
                        .Select(c => c.id)).ToArray();
                    int chosen = i;
                    Pick("Chọn mèo Cộng hưởng", options.Select(id => string.IsNullOrEmpty(id) ? "Để trống ô" : db.Cat(id).name),
                        at => { profile.nexus[chosen] = options[at]; Changed("Đã cập nhật Cộng hưởng"); },
                        options.Select(id => string.IsNullOrEmpty(id) ? "item-soulEssence" : id));
                }
            }
            Art(96, 814, 31, 31, "Interface/Cats/InfoIcon");
            Text(139, 816, 1366, "Thêm ô khi vượt ải 4 / 6 / 8 / 10 / 13. Mèo trong ô nhận cấp của neo thấp nhất, giới hạn theo bậc; không tiêu XP.",
                small, 37, size: 21);
        }

        void SectionBackdrop() => Art(0, 0, 1600, 900, "Interface/Barracks/Hangar");

        void BarracksSectionChrome()
        {
            Surface(0, 0, 1600, 92, Color.white, catsFrame: "Header");
            if (IllustratedButton(12, 10, 83, 69, "‹", "UiBack", primary: true, content: StudioButtonLayout.Content.Symbol)) StepBack();
            Text(111, 3, 605, PageTitle, title, 77, align: TextAnchor.MiddleLeft, size: 49);
            SectionWallet(751, "coins", "XU");
            SectionWallet(971, "gem", "NGỌC");
            SectionWallet(1191, "summonScroll", "VÉ GỌI MÈO");
            if (IllustratedButton(1412, 11, 176, 66, "Cài đặt", "Interface/Cats/SettingsIcon", normalSpriteKey: "blue"))
                modal = "settings";
            Enlarge(23);
        }

        void SectionWallet(float x, string resource, string caption)
        {
            Surface(x, 11, 208, 66, Color.white, catsFrame: "CombatPlate");
            Art(x + 7, 15, 58, 58, RewardIcon(resource));
            Text(x + 68, 12, 131, N(profile.Get(resource)), subtitle, 34, size: 25);
            Text(x + 68, 45, 131, caption, small, 26, size: 17);
        }

        void SectionPanel(float x, float y, float width, float height) =>
            Surface(x, y, width, height, Color.white, catsFrame: "BarracksFrame");

        bool SectionTile(float x, float y, float width, float height, string caption, string icon, string frame,
            bool enabled = true, Color? tint = null)
        {
            bool clicked = IllustratedButton(x, y, width, height, caption, icon, enabled, content: StudioButtonLayout.Content.Tile);
            if (dispatchOnly) return clicked;
            StudioNineSlice.Apply(lastButton.image, CatsFrame(frame), 18);
            lastButton.image.color = tint ?? Color.white;
            Enlarge(height > 180 ? 22 : 20);
            return clicked;
        }


    }
}
