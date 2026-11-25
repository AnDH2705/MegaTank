using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    // Doanh trại theo bố cục quen thuộc của game idle (chỉ cách sắp xếp, art vẫn là của CatTanks):
    // danh sách mèo là màn mặc định → bấm thẻ mở trang chi tiết (trang bị quanh mèo, nút NÂNG CẤP lớn);
    // các mục Đội hình, Kỹ năng, Pháo đài, Quái thú, Cộng hưởng vào từ cột phải và giữ nội dung cũ.
    public sealed partial class CatTanksGame
    {
        const string RosterView="barracks-roster",CatView="barracks-cat";
        static readonly string[] BarracksTabs={"cats","cat","fortress","squad","skills","monsters","presets","nexus"};
        static readonly string[] RoleKeys={"","TANK","WARRIOR","RANGER","MAGE","HEALER","ASSASSIN"};
        string rosterRole="";

        string BarracksViewKey()=>tab=="cats"?RosterView:tab=="cat"?CatView:"barracks-"+tab;
        static string RoleName(string role)=>role switch
        {
            "TANK"=>"Đỡ đòn","WARRIOR"=>"Chiến binh","RANGER"=>"Xạ thủ","MAGE"=>"Phép","HEALER"=>"Hồi máu","ASSASSIN"=>"Sát thủ",_=>"Tất cả"
        };
        static int RarityRank(string rarity)=>rarity switch{"RARE"=>2,"ELITE"=>3,"LEGENDARY"=>4,"MYTHIC"=>5,_=>1};
        string BarracksTitle=>tab switch
        {
            "cat"=>profile.cats.Count>0?db.Cat(profile.cats[Mathf.Clamp(selectedCat,0,profile.cats.Count-1)].id).name:"Doanh trại",
            "fortress"=>"Pháo đài","squad" or "presets"=>"Đội hình","skills"=>"Kỹ năng","monsters"=>"Quái thú","nexus"=>"Cộng hưởng",
            _=>"Doanh trại"
        };

        // Các mục cũ (Pháo đài, Đội hình…) giữ nội dung prefab; Đội hình và Đội lưu dùng chung hai tab.
        void BarracksSection()
        {
            if(tab is "squad" or "presets")
            {
                if(TabButton(283,148,300,50,"BỐ TRÍ",tab=="squad","Interface/Cats/Tab-squad")){tab="squad";scroll=Vector2.zero;}
                if(TabButton(599,148,300,50,"ĐỘI LƯU",tab=="presets","Interface/Cats/Tab-presets")){tab="presets";scroll=Vector2.zero;}
            }
            useCodeGeometry=true;
            switch(tab)
            {
                case "fortress":FortressUI();break;case "squad":SquadUI();break;case "skills":SkillsUI();break;
                case "monsters":MonstersUI();break;case "presets":PresetsUI();break;case "nexus":NexusUI();break;
            }
        }

        // ---- Danh sách mèo ----
        void RosterUI()
        {
            Text(130,108,700,"Những người giữ pháo đài",title,55);
            Text(980,120,200,$"{profile.cats.Count}/10 mèo",subtitle,40,align:TextAnchor.UpperRight);
            for(int i=0;i<RoleKeys.Length;i++)
                if(TabButton(130+i*144,166,136,48,RoleName(RoleKeys[i]),rosterRole==RoleKeys[i])){rosterRole=RoleKeys[i];}

            Surface(118,226,1078,548,Color.white,catsFrame:"Roster");
            int shown=0;
            for(int i=0;i<profile.cats.Count&&shown<10;i++)
            {
                var c=profile.cats[i];var def=db.Cat(c.id);
                if(rosterRole!=""&&def.role!=rosterRole)continue;
                float x=140+shown%5*210,y=246+shown/5*258;shown++;
                int rank=RarityRank(def.rarity);
                if(IllustratedButton(x,y,196,244,def.name,c.id,content:StudioButtonLayout.Content.Tile,selected:selectedCat==i)){selectedCat=i;tab="cat";}
                if(!dispatchOnly&&selectedCat!=i)StudioNineSlice.Apply(lastButton.image,CatsFrame(rank>=3?"Violet":rank==2?"Blue":"Ink"),18);
                Enlarge(25);
                Text(x+14,y+10,80,"Lv "+c.level,subtitle,32);
                Text(x+92,y+6,94,new string('★',rank),title,36,align:TextAnchor.UpperRight,size:22);
            }
            if(shown==0)Text(140,440,1036,"Chưa có mèo vai trò "+RoleName(rosterRole)+". Gọi thêm đồng đội ở Thành mèo.",label,40,align:TextAnchor.UpperCenter);

            // Lối vào các mục còn lại của Doanh trại.
            var anchors=profile.cats.OrderByDescending(c=>c.level).Take(5).ToList();
            string[] keys={"squad","skills","fortress","monsters","nexus"};
            string[] names={"Đội hình","Kỹ năng","Pháo đài","Quái thú","Cộng hưởng"};
            string[] info=
            {
                $"{profile.FloorCount}/5 tầng",
                $"{profile.skillSlots.Count(s=>!string.IsNullOrEmpty(s))}/4 ô",
                "HP "+N(profile.MaxHp),
                $"{profile.pets.Count}/20 loài",
                anchors.Count==5?"Cấp "+anchors.Min(c=>c.level):"Cần 5 mèo neo"
            };
            for(int i=0;i<keys.Length;i++)
            {
                if(IllustratedButton(1214,226+i*110,270,100,names[i]+"\n"+info[i],"Interface/Cats/Tab-"+keys[i])){tab=keys[i];scroll=Vector2.zero;}
                if(!dispatchOnly&&lastButton.layout)lastButton.layout.iconSize=74;
                Enlarge(24);
            }
            BottomNav();
        }

        // ---- Chi tiết một mèo ----
        void CatDetailUI()
        {
            if(profile.cats.Count==0){tab="cats";return;}
            int count=profile.cats.Count;
            selectedCat=Mathf.Clamp(selectedCat,0,count-1);
            var cat=profile.cats[selectedCat];var def=db.Cat(cat.id);var stats=profile.Stats(cat,db);

            // Mèo to ở giữa, ‹ › đổi mèo, trang bị và cổ vật xếp quanh.
            Art(340,190,340,420,cat.id);
            if(Button(128,370,64,100,"‹",count>1,true))selectedCat=(selectedCat+count-1)%count;
            Enlarge(46);
            if(Button(828,370,64,100,"›",count>1,true))selectedCat=(selectedCat+1)%count;
            Enlarge(46);
            GearSlotButton(208,200,cat,"weapon","Vũ khí","gun-0");
            GearSlotButton(208,346,cat,"helm","Mũ","hat-0");
            GearSlotButton(692,200,cat,"chest","Áo giáp","armor-0");
            GearSlotButton(692,346,cat,"boots","Giày","gear-boots");
            ArtifactSlotButton(450,636,cat);

            // Bảng thông tin bên phải.
            Surface(930,112,552,662,Color.white,catsFrame:"Detail");
            Text(960,122,180,def.name,title,64,size:46);
            Surface(1140,146,112,38,Color.white,catsFrame:"Rarity");
            Text(1140,147,112,def.rarity,small,36,align:TextAnchor.MiddleCenter);
            Text(1262,138,200,new string('★',RarityRank(def.rarity)),title,48,size:30);
            Text(962,198,480,RoleName(def.role).ToUpper()+"  ·  Bậc "+cat.tier,subtitle,36);

            int cap=(int)db.balance["ascension"]["levelCaps"][cat.tier];int effective=profile.EffectiveLevel(cat);
            Surface(950,238,512,70,Color.white,catsFrame:"Ink");
            Art(966,250,46,46,"Interface/Cats/PowerIcon");
            Text(1020,244,200,"LỰC CHIẾN",small,24);
            Text(1020,262,220,N(stats["maxHp"]*.15f+stats["atk"]*1.6f+stats["def"]*1.2f+stats["speed"]*2),title,44,size:30);
            Text(1220,246,220,$"Cấp {cat.level} / {cap}"+(effective!=cat.level?"\nCộng hưởng "+effective:""),subtitle,58,align:TextAnchor.UpperRight);
            CatStat(966,324,"Heart","HP",stats["maxHp"]);
            CatStat(1216,324,"Sword","ATK",stats["atk"]);
            CatStat(966,370,"Shield","DEF",stats["def"]);
            CatStat(1216,370,"Speed","SPD",stats["speed"]);

            var tiers=((JObject)db.balance["ascension"]["levelCaps"]).Properties().Select(p=>p.Name).ToArray();
            int rank=Array.IndexOf(tiers,cat.tier);int copies=rank<11?(int)db.balance["ascension"]["needSelf"][rank]:0;int essence=10+rank*15;
            Text(966,420,490,$"Bản sao {cat.copies}/{copies}  ·  Tinh chất {N(profile.Get("soulEssence"))}/{essence}  ·  Di vật {cat.relic}/10",small,30);
            if(Button(960,458,240,58,"Thăng bậc",rank<tiers.Length-1))
            {
                if(cat.copies<copies)Notify("Thiếu bản sao");
                else if(Pay(("soulEssence",essence))){cat.copies-=copies;cat.tier=tiers[rank+1];Changed("Đã thăng bậc "+cat.tier);}
            }
            int relicCost=Mathf.Max(10,cat.relic*10);
            if(IllustratedButton(1214,458,234,58,$"Di vật · {relicCost} đá","item-relicStone",cat.relic<10)){if(Pay(("relicStone",relicCost))){cat.relic++;Changed();}}
            if(Button(960,526,488,48,"Tái chế 1 bản sao · +20 tinh chất",cat.copies>0,normalSpriteKey:"blue"))
                Pick("Tái chế bản sao "+def.name+"?",new[]{"Giữ lại bản sao","Tái chế • Nhận 20 tinh chất"},at=>{if(at==1){cat.copies--;profile.Add("soulEssence",20);Changed("+20 tinh chất");}});

            // Khối nâng cấp: giá ngay trên nút NÂNG CẤP lớn.
            int goldCost=Mathf.FloorToInt(80*Mathf.Pow(cat.level,1.45f)),xpCost=Mathf.FloorToInt(25*Mathf.Pow(cat.level,1.22f));
            Surface(950,590,512,168,Color.white,catsFrame:"Ink");
            Art(972,608,38,38,"item-coins");
            Text(1016,610,110,N(goldCost),subtitle,36);
            Art(1124,608,38,38,"item-catXp");
            Text(1168,610,110,N(xpCost),subtitle,36);
            Text(1290,614,150,$"Cấp {cat.level} → {cat.level+1}",small,30,align:TextAnchor.UpperRight);
            if(Button(966,662,480,82,"NÂNG CẤP",profile.Get("coins")>=goldCost&&profile.Get("catXp")>=xpCost,true))
            {
                if(profile.LevelCat(cat,db))Changed("Mèo đã lên cấp");else Notify("Thiếu tài nguyên hoặc đã đạt giới hạn cấp");
            }
            Enlarge(32);
        }

        string GearName(Gear g)=>g.id.StartsWith("gun")?db.Weapon(g.id).name:g.id=="gear-helm"?"Mũ giáp":g.id=="gear-chest"?"Áo giáp":"Giày hành quân";

        // Ô trang bị quanh mèo: bấm để rèn, tháo hoặc đổi món cùng loại.
        void GearSlotButton(float x,float y,OwnedCat cat,string slot,string caption,string placeholder)
        {
            var current=cat.gear.TryGetValue(slot,out var uid)?profile.gear.Find(g=>g.uid==uid):null;
            if(IllustratedButton(x,y,120,120,caption,current==null?placeholder:GearAsset(current),content:StudioButtonLayout.Content.Tile))PickGear(cat,slot,caption,current);
            if(!dispatchOnly&&current==null&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
            Enlarge(17);
            if(current!=null&&current.stars>0)Text(x+10,y+4,100,new string('★',current.stars),title,30,size:18);
        }
        void PickGear(OwnedCat cat,string slot,string caption,Gear current)
        {
            var options=new List<string>();var actions=new List<Action>();
            if(current!=null)
            {
                int coin=200*(current.stars+1),stone=5+current.stars*5;
                options.Add(current.stars<5?$"Rèn {GearName(current)} · {coin} xu / {stone} đá":GearName(current)+" đã rèn tối đa 5★");
                actions.Add(()=>{if(current.stars<5&&Pay(("coins",coin),("forgeStone",stone))){current.stars++;profile.Track("upgrade");Changed("Đã rèn trang bị");}});
                options.Add("Tháo "+GearName(current));
                actions.Add(()=>{cat.gear.Remove(slot);Changed("Đã tháo trang bị");});
            }
            foreach(var g in profile.gear.Where(v=>GearSlot(v)==slot&&v!=current))
            {
                var owner=profile.cats.Find(c=>c!=cat&&c.gear.ContainsValue(g.uid));
                options.Add($"{GearName(g)} · {g.stars}★ · {g.rarity}"+(owner!=null?" · đang dùng: "+db.Cat(owner.id).name:""));
                actions.Add(()=>
                {
                    foreach(var c in profile.cats)foreach(var key in c.gear.Where(v=>v.Value==g.uid).Select(v=>v.Key).ToArray())c.gear.Remove(key);
                    cat.gear[slot]=g.uid;Changed("Đã trang bị");
                });
            }
            if(options.Count==0){Notify("Chưa có "+caption.ToLower()+". Vượt ải 2–4 và 6–7 để nhận trang bị.");return;}
            Pick(caption+" · "+db.Cat(cat.id).name,options,at=>actions[at]());
        }
        void ArtifactSlotButton(float x,float y,OwnedCat cat)
        {
            var artifact=profile.artifacts.Find(a=>a.id==cat.artifact);
            if(IllustratedButton(x,y,120,120,artifact==null?"Cổ vật":artifact.name,"item-artifact",content:StudioButtonLayout.Content.Tile))
            {
                if(profile.artifacts.Count==0){Notify("Chưa có cổ vật.");return;}
                var choices=new[]{""}.Concat(profile.artifacts.Select(a=>a.id)).ToArray();
                Pick("Chọn cổ vật",choices.Select(id=>string.IsNullOrEmpty(id)?"Gỡ cổ vật":profile.artifacts.First(a=>a.id==id).name),at=>
                {
                    string next=choices[at];foreach(var c in profile.cats)if(c.artifact==next)c.artifact="";
                    cat.artifact=next;Changed("Đã cập nhật cổ vật");
                });
            }
            if(!dispatchOnly&&artifact==null&&lastButton.layout&&lastButton.layout.icon)lastButton.layout.icon.color=new Color(1,1,1,.35f);
            Enlarge(17);
        }
    }
}
