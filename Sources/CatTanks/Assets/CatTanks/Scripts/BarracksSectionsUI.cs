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

        // ================= Kỹ năng (Battle Deck) =================
        void SkillDeckUI()
        {
            Text(130,108,1000,"Kỹ năng chiến thuật",title,55);
            Text(830,122,650,"Lõi kỹ năng nhận ở ải trùm · phím 1–4 trong trận",small,30,align:TextAnchor.UpperRight);

            // Bộ đang dùng: 4 ô.
            Surface(118,166,1364,150,Color.white,catsFrame:"Ink");
            Text(134,176,300,"BỘ KỸ NĂNG",subtitle,36);
            Text(134,214,300,"Chọn một ô, rồi chọn kỹ năng ở dưới và bấm DÙNG.",small,80);
            for(int slot=0;slot<4;slot++)
            {
                string id=profile.skillSlots[slot];var skill=db.skills.Find(s=>s.id==id);
                if(IllustratedButton(450+slot*254,176,240,128,$"[{slot+1}] {(skill==null?"Trống":skill.name)}",skill==null?"Gear":"skill-"+id,selected:skillSlotPick==slot))
                {
                    skillSlotPick=slot;int at=db.skills.FindIndex(s=>s.id==id);if(at>=0)selectedSkill=at;
                }
                if(!dispatchOnly&&lastButton.layout)lastButton.layout.iconSize=72;
                Enlarge(20);
            }

            // Bộ sưu tập.
            Surface(118,330,900,560,Color.white,catsFrame:"Roster");
            for(int i=0;i<db.skills.Count&&i<10;i++)
            {
                var s=db.skills[i];bool open=profile.SkillUnlocked(s);int level=SkillLevel(s.id);
                float x=134+i%5*176,y=346+i/5*268;
                if(IllustratedButton(x,y,164,254,s.name,"skill-"+s.id,content:StudioButtonLayout.Content.Tile,selected:selectedSkill==i))selectedSkill=i;
                if(!dispatchOnly&&!open&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
                Enlarge(19);
                Text(x+10,y+8,90,open?"Lv "+level:"KHÓA",subtitle,32);
                if(profile.skillSlots.Contains(s.id))Text(x+60,y+8,94,"["+(Array.IndexOf(profile.skillSlots,s.id)+1)+"]",title,40,align:TextAnchor.UpperRight,size:26);
            }

            // Chi tiết kỹ năng đang chọn.
            selectedSkill=Mathf.Clamp(selectedSkill,0,db.skills.Count-1);
            var pick=db.skills[selectedSkill];bool unlocked=profile.SkillUnlocked(pick);int lv=SkillLevel(pick.id);
            Surface(1034,330,448,560,Color.white,catsFrame:"Detail");
            Text(1058,344,400,pick.name,title,56,size:38);
            Art(1178,404,160,160,"skill-"+pick.id);
            Text(1058,576,400,unlocked?$"Cấp {lv} / 8\n{pick.baseCost} năng lượng · hồi {pick.baseCd}s\n{(pick.duration>0?pick.duration+" giây hiệu lực":"Kích hoạt tức thì")}":"Mở tại "+(pick.unlock["stage"]!=null?"ải "+pick.unlock["stage"]:"cấp "+pick.unlock["accountLevel"]),label,110);
            int inDeck=Array.IndexOf(profile.skillSlots,pick.id);
            if(inDeck>=0)
            {
                if(Button(1050,694,416,56,$"GỠ KHỎI Ô [{inDeck+1}]",true,normalSpriteKey:"blue"))
                {
                    if(profile.skillSlots.Count(v=>!string.IsNullOrEmpty(v))==1)Notify("Giữ ít nhất một kỹ năng trong đội hình");
                    else{profile.skillSlots[inDeck]="";Changed("Đã gỡ kỹ năng");}
                }
            }
            else if(Button(1050,694,416,56,$"DÙNG VÀO Ô [{skillSlotPick+1}]",unlocked,normalSpriteKey:"blue")){profile.skillSlots[skillSlotPick]=pick.id;Changed("Đã đổi kỹ năng");}
            Enlarge(20);
            if(Button(1050,762,416,108,lv>=8?"ĐẠT CẤP TỐI ĐA":$"NÂNG CẤP · {lv} lõi",unlocked&&lv<8,true)){if(Pay(("skillCore",lv))){profile.skillLevels[pick.id]=lv+1;Changed();}}
            Enlarge(26);
        }
        int SkillLevel(string id)=>profile.skillLevels.TryGetValue(id,out int n)?n:1;

        // ================= Pháo đài (Hangar / Workshop) =================
        void FortWorkshopUI()
        {
            int hpCost=Mathf.FloorToInt(400*Mathf.Pow(profile.hpLevel,1.4f));
            Surface(118,108,1364,96,Color.white,catsFrame:"Ink");
            Art(134,116,240,80,"fortress-chassis");
            Text(392,116,700,$"PHÁO ĐÀI · HP {N(profile.MaxHp)} · {profile.FloorCount}/5 tầng",subtitle,36);
            Text(392,154,700,$"Giáp cấp {profile.hpLevel} · Mở tầng tại cấp chỉ huy 20 / 40 / 60 / 80",small,30);
            if(Button(1110,120,356,72,$"GIA CỐ · {N(hpCost)} xu",profile.hpLevel<240,normalSpriteKey:"blue")){if(Pay(("coins",hpCost))){profile.hpLevel++;profile.Track("upgrade");Changed("Pháo đài được gia cố");}}
            Enlarge(22);

            // Danh sách tháp pháo.
            Surface(118,218,360,672,Color.white,catsFrame:"Roster");
            for(int i=0;i<profile.turrets.Count&&i<10;i++)
            {
                var t=profile.turrets[i];
                if(IllustratedButton(134+i%2*168,234+i/2*128,156,118,"Cấp "+t.level,t.id,content:StudioButtonLayout.Content.Tile,selected:selectedTurret==i))selectedTurret=i;
                Enlarge(17);
                if(t.stars>0)Text(140+i%2*168,236+i/2*128,144,new string('★',t.stars),title,30,align:TextAnchor.UpperRight,size:18);
            }

            selectedTurret=Mathf.Clamp(selectedTurret,0,profile.turrets.Count-1);
            var turret=profile.turrets[selectedTurret];var weapon=db.Weapon(turret.id);
            int cap=40+8*turret.stars;
            // Pháo lớn ở giữa, hai ô chim chiến (module) bên dưới.
            Art(500,240,546,340,turret.id);
            int floor=profile.floors.FindIndex(v=>v.turret==turret.id);
            Text(500,586,546,floor>=0?$"ĐANG LẮP Ở TẦNG {floor+1}":"CHƯA LẮP · đổi ở mục Đội hình",small,30,align:TextAnchor.UpperCenter);
            for(int slot=0;slot<2;slot++)
            {
                string id=turret.modules[slot];bool empty=string.IsNullOrEmpty(id);
                if(IllustratedButton(560+slot*236,630,220,250,empty?$"Chim {slot+1}":db.Weapon(id).name,empty?"Gear":id,content:StudioButtonLayout.Content.Tile))
                {
                    var options=new[]{""}.Concat(profile.modules).ToArray();int chosen=slot;
                    Pick("Chọn chim "+(slot+1),options.Select(v=>string.IsNullOrEmpty(v)?"Gỡ chim":db.Weapon(v).name),at=>{turret.modules[chosen]=options[at];Changed("Đã đổi chim");},options.Select(v=>string.IsNullOrEmpty(v)?"Gear":v));
                }
                if(!dispatchOnly&&empty&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
                Enlarge(18);
                Text(560+slot*236,636,220,"MODULE "+(slot+1),small,26,align:TextAnchor.UpperCenter);
            }

            // Bảng nâng cấp bên phải.
            Surface(1066,218,416,672,Color.white,catsFrame:"Detail");
            Text(1088,232,380,weapon.name,title,56,size:36);
            Text(1088,290,380,turret.stars>0?new string('★',turret.stars)+new string('☆',5-turret.stars):"☆☆☆☆☆",title,40,size:28);
            Text(1088,334,380,$"Cấp {turret.level} / {cap}",subtitle,36);
            Text(1088,376,380,weapon.description,small,170);
            int coin=Mathf.FloorToInt(60*Mathf.Pow(turret.level,1.4f)),dust=Mathf.FloorToInt(30*Mathf.Pow(turret.level,1.3f));
            int starCoin=200*(turret.stars+1),starStone=5+5*turret.stars;
            if(Button(1082,560,384,60,turret.stars<5?"NÂNG SAO · "+Cost(("coins",starCoin),("forgeStone",starStone)):"ĐÃ ĐỦ 5 SAO",turret.stars<5,normalSpriteKey:"blue"))
            {
                if(Pay(("coins",starCoin),("forgeStone",starStone))){turret.stars++;Changed("Đã nâng sao pháo");}
            }
            Surface(1082,636,384,240,Color.white,catsFrame:"Ink");
            Art(1100,652,38,38,"item-coins");Text(1144,656,120,N(coin),subtitle,36);
            Art(1266,652,38,38,"item-spiritDust");Text(1310,656,140,N(dust),subtitle,36);
            Text(1100,704,350,turret.level>=cap?"Cần nâng sao để tăng giới hạn cấp":$"Cấp {turret.level} → {turret.level+1}",small,30);
            if(Button(1098,756,352,104,"NÂNG CẤP",turret.level<cap&&profile.Get("coins")>=coin&&profile.Get("spiritDust")>=dust,true))
            {
                if(Pay(("coins",coin),("spiritDust",dust))){turret.level++;profile.Track("upgrade");Changed();}
            }
            Enlarge(30);
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

        // ================= Cộng hưởng (Resonating Crystal) =================
        void CrystalUI()
        {
            var anchors=profile.cats.OrderByDescending(c=>c.level).Take(5).ToList();
            for(int i=0;i<profile.nexus.Length;i++)if(anchors.Any(c=>c.id==profile.nexus[i]))profile.nexus[i]="";
            int level=anchors.Count==5?anchors.Min(c=>c.level):0;

            Text(130,108,700,"Cộng hưởng ánh trăng",title,55);
            Text(830,122,650,"5 mèo cấp cao nhất làm neo",small,30,align:TextAnchor.UpperRight);
            // 5 mèo neo xếp vòng cung quanh pha lê.
            float[] ax={232,452,720,988,1208},ay={330,214,166,214,330};
            for(int i=0;i<5;i++)
            {
                var c=i<anchors.Count?anchors[i]:null;
                Surface(ax[i],ay[i],160,180,Color.white,catsFrame:c==null?"Ink":"Tile");
                if(c!=null)Art(ax[i]+12,ay[i]+8,136,120,c.id);
                Text(ax[i],ay[i]+132,160,c==null?"Cần thêm mèo":db.Cat(c.id).name+"\nCấp "+c.level,small,48,align:TextAnchor.UpperCenter);
            }
            Art(700,360,200,190,"item-soulEssence");
            Text(520,548,560,"CẤP CỘNG HƯỞNG",subtitle,36,align:TextAnchor.UpperCenter);
            Text(520,580,560,level.ToString(),title,60,align:TextAnchor.UpperCenter,size:48);

            // Hàng ô cộng hưởng.
            Surface(118,648,1364,242,Color.white,catsFrame:"Ink");
            Text(134,656,800,$"Ô CỘNG HƯỞNG · {profile.NexusSlots}/8 ô mở",subtitle,36);
            for(int i=0;i<8;i++)
            {
                float x=134+i*168;string current=profile.nexus[i];bool open=i<profile.NexusSlots;bool empty=string.IsNullOrEmpty(current);
                if(IllustratedButton(x,694,156,158,!open?"Chưa mở":empty?"Trống":db.Cat(current).name,!open?"Lock":empty?"item-soulEssence":current,open,content:StudioButtonLayout.Content.Tile))
                {
                    var options=new[]{""}.Concat(profile.cats.Where(c=>!anchors.Contains(c)&&(!profile.nexus.Contains(c.id)||c.id==current)).Select(c=>c.id)).ToArray();int chosen=i;
                    Pick("Chọn mèo Cộng hưởng",options.Select(id=>string.IsNullOrEmpty(id)?"Để trống ô":db.Cat(id).name),at=>{profile.nexus[chosen]=options[at];Changed("Đã cập nhật Cộng hưởng");},options.Select(id=>string.IsNullOrEmpty(id)?"item-soulEssence":id));
                }
                if(!dispatchOnly&&open&&empty&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
                Enlarge(17);
            }
            Text(134,858,1340,"Thêm ô khi vượt ải 4 / 6 / 8 / 10 / 13. Mèo trong ô nhận cấp của neo thấp nhất, giới hạn theo bậc; không tiêu XP.",small,30);
        }
    }
}
