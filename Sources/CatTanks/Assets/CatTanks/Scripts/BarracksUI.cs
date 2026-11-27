using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CatTanks
{
    // Doanh trại theo bố cục quen thuộc của game idle (chỉ cách sắp xếp, art vẫn là của CatTanks):
    // danh sách mèo là màn mặc định → bấm thẻ mở trang chi tiết (trang bị quanh mèo, nút NÂNG CẤP lớn);
    // các mục Đội hình, Kỹ năng, Pháo đài, Quái thú, Cộng hưởng vào từ cột phải (xem BarracksSectionsUI.cs).
    public sealed partial class CatTanksGame
    {
        const string RosterView="barracks-roster",CatView="barracks-cat";
        static readonly string[] BarracksTabs={"cats","cat","fortress","squad","skills","monsters","presets","nexus"};
        static readonly string[] RoleKeys={"","TANK","WARRIOR","RANGER","MAGE","HEALER","ASSASSIN"};
        string rosterRole="";int rosterPage;

        string BarracksViewKey()=>tab=="cat"?CatView:BarracksSectionView();
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

        // ---- Danh sách mèo ----
        void RosterUI()
        {
            Text(130,108,700,"Những người giữ pháo đài",title,55);
            Text(980,120,200,$"{profile.cats.Count}/{db.cats.Count} mèo",subtitle,40,align:TextAnchor.UpperRight);
            for(int i=0;i<RoleKeys.Length;i++)
                if(TabButton(130+i*144,166,136,48,RoleName(RoleKeys[i]),rosterRole==RoleKeys[i])){rosterRole=RoleKeys[i];rosterPage=0;}
            // 10 thẻ mỗi trang (lưới 5x2); lật trang khi có hơn 10 mèo khớp bộ lọc.
            int matching=profile.cats.Count(c=>rosterRole==""||db.Cat(c.id).role==rosterRole),pages=Mathf.Max(1,(matching+9)/10);
            rosterPage=Mathf.Clamp(rosterPage,0,pages-1);
            if(pages>1)
            {
                // đặt ở hàng tiêu đề, giữa tiêu đề và dòng đếm mèo (hàng tab vai trò đã kín tới x≈1130)
                Text(840,120,56,$"{rosterPage+1}/{pages}",small,30,align:TextAnchor.UpperCenter);
                if(Button(898,108,38,44,"‹",rosterPage>0,normalSpriteKey:"blue"))rosterPage--;
                if(Button(940,108,38,44,"›",rosterPage<pages-1,normalSpriteKey:"blue"))rosterPage++;
            }
            int skip=rosterPage*10;

            Surface(118,226,1078,548,Color.white,catsFrame:"Roster");
            int shown=0;
            for(int i=0;i<profile.cats.Count&&shown<10;i++)
            {
                var c=profile.cats[i];var def=db.Cat(c.id);
                if(rosterRole!=""&&def.role!=rosterRole)continue;
                if(skip>0){skip--;continue;}
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

            Art(0, 0, 1600, 900, "Interface/Cats/CatDetailHangar");
            if (!dispatchOnly)
            {
                const float portraitWidth = 352;
                const float podiumY = 573;
                var portrait = UiArt(cat.id);
                if (!portrait) portrait = view.Art(cat.id);
                float portraitHeight = Mathf.Min(350, portraitWidth * portrait.rect.height / portrait.rect.width);
                Art(329, podiumY - portraitHeight, portraitWidth, portraitHeight, cat.id);
            }
            if(Button(108,382,66,105,"‹",count>1,true))selectedCat=(selectedCat+count-1)%count;
            Enlarge(46);
            if(Button(831,382,66,105,"›",count>1,true))selectedCat=(selectedCat+1)%count;
            Enlarge(46);
            GearSlotButton(194,200,cat,"weapon","Vũ khí");
            GearSlotButton(194,360,cat,"helm","Mũ");
            GearSlotButton(675,200,cat,"chest","Áo giáp");
            GearSlotButton(675,360,cat,"boots","Giày");
            ArtifactSlotButton(442,669,cat);

            // Bảng thông tin bên phải.
            Art(909, 105, 622, 742, "Interface/Cats/CatDetailFrame");
            Text(962,133,194,def.name,title,64,size:49);
            Surface(1164,149,117,45,Color.white,catsFrame:"Rarity");
            Text(1164,149,117,def.rarity,subtitle,45,align:TextAnchor.MiddleCenter,size:18);
            for (int i = 0; i < RarityRank(def.rarity); i++)
                Art(1298 + i * 35, 150, 34, 39, "Interface/Cats/CatDetailStar");
            Text(964,198,520,RoleName(def.role).ToUpper()+"  ·  Bậc "+cat.tier,subtitle,41,size:23);

            int cap=(int)db.balance["ascension"]["levelCaps"][cat.tier];int effective=profile.EffectiveLevel(cat);
            Surface(946,251,550,79,Color.white,catsFrame:"Ink");
            Art(968,263,52,52,"Interface/Cats/PowerIcon");
            Text(1028,257,200,"LỰC CHIẾN",subtitle,30,size:20);
            Text(1028,278,223,N(stats["maxHp"]*.15f+stats["atk"]*1.6f+stats["def"]*1.2f+stats["speed"]*2),title,44,size:34);
            Text(1252,257,227,$"Cấp {cat.level} / {cap}"+(effective!=cat.level?"\nCộng hưởng "+effective:""),subtitle,61,align:TextAnchor.MiddleRight,size:23);
            CatStat(966,343,"Heart","HP",stats["maxHp"]);
            CatStat(1243,343,"Sword","ATK",stats["atk"]);
            CatStat(966,388,"Shield","DEF",stats["def"]);
            CatStat(1243,388,"Speed","SPD",stats["speed"]);

            var tiers=((JObject)db.balance["ascension"]["levelCaps"]).Properties().Select(p=>p.Name).ToArray();
            int rank=Array.IndexOf(tiers,cat.tier);int copies=rank<11?(int)db.balance["ascension"]["needSelf"][rank]:0;int essence=10+rank*15;
            Text(966,432,515,$"Bản sao {cat.copies}/{copies}  ·  Tinh chất {N(profile.Get("soulEssence"))}/{essence}  ·  Di vật {cat.relic}/10",subtitle,38,size:19);
            if(Button(957,476,255,63,"Thăng bậc",rank<tiers.Length-1))
            {
                if(cat.copies<copies)Notify("Thiếu bản sao");
                else if(Pay(("soulEssence",essence))){cat.copies-=copies;cat.tier=tiers[rank+1];Changed("Đã thăng bậc "+cat.tier);}
            }
            int relicCost=Mathf.Max(10,cat.relic*10);
            if(IllustratedButton(1228,476,263,63,$"Di vật · {relicCost} đá","item-relicStone",cat.relic<10)){if(Pay(("relicStone",relicCost))){cat.relic++;Changed();}}
            if(Button(957,548,534,55,"Tái chế 1 bản sao · +20 tinh chất",cat.copies>0,normalSpriteKey:"blue"))
                Pick("Tái chế bản sao "+def.name+"?",new[]{"Giữ lại bản sao","Tái chế • Nhận 20 tinh chất"},at=>{if(at==1){cat.copies--;profile.Add("soulEssence",20);Changed("+20 tinh chất");}});

            // Khối nâng cấp: giá ngay trên nút NÂNG CẤP lớn.
            int goldCost=Mathf.FloorToInt(80*Mathf.Pow(cat.level,1.45f)),xpCost=Mathf.FloorToInt(25*Mathf.Pow(cat.level,1.22f));
            Surface(945,617,554,182,Color.white,catsFrame:"Ink");
            Art(968,633,44,44,RewardIcon("coins"));
            Text(1018,633,120,N(goldCost),subtitle,44,align:TextAnchor.MiddleLeft,size:24);
            Art(1142,633,40,44,"item-catXp");
            Text(1187,633,105,N(xpCost),subtitle,44,align:TextAnchor.MiddleLeft,size:24);
            Text(1298,633,181,$"Cấp {cat.level} › {cat.level+1}",subtitle,44,align:TextAnchor.MiddleRight,size:22);
            if(Button(963,685,518,100,"NÂNG CẤP",profile.Get("coins")>=goldCost&&profile.Get("catXp")>=xpCost,true))
            {
                if(profile.LevelCat(cat,db))Changed("Mèo đã lên cấp");else Notify("Thiếu tài nguyên hoặc đã đạt giới hạn cấp");
            }
            Enlarge(38);
        }

        string GearName(Gear g)=>g.id.StartsWith("gun")?db.Weapon(g.id).name:g.id=="gear-helm"?"Mũ giáp":g.id=="gear-chest"?"Áo giáp":"Giày hành quân";

        // Ô trang bị quanh mèo: bấm để rèn, tháo hoặc đổi món cùng loại.
        void GearSlotButton(float x,float y,OwnedCat cat,string slot,string caption)
        {
            var current=cat.gear.TryGetValue(slot,out var uid)?profile.gear.Find(g=>g.uid==uid):null;
            if(EquipmentSlotButton(x,y,143,139,caption,current==null?null:GearAsset(current)))PickGear(cat,slot,caption,current);
            if (current != null)
                for (int i = 0; i < current.stars; i++)
                    Art(x + 10 + i * 18, y + 8, 18, 18, "Interface/Cats/CatDetailStar");
        }
        bool EquipmentSlotButton(float x,float y,float w,float h,string caption,string icon)
        {
            bool empty=icon==null;
            bool clicked=IllustratedButton(x,y,w,h,empty?caption+"\nÔ trống":caption,icon,
                content:empty?StudioButtonLayout.Content.Centered:StudioButtonLayout.Content.Tile);
            if(!dispatchOnly)SetButtonSprite(lastButton,empty?"blue":"button");
            Enlarge(20);
            return clicked;
        }
        void PickGear(OwnedCat cat,string slot,string caption,Gear current)
        {
            var options=new List<string>();var actions=new List<Action>();var icons=new List<string>();
            if(current!=null)
            {
                int coin=200*(current.stars+1),stone=5+current.stars*5;
                options.Add(current.stars<5?$"Rèn · {coin} xu / {stone} đá":"Đã rèn tối đa 5★");icons.Add("item-forgeStone");
                actions.Add(()=>{if(current.stars<5&&Pay(("coins",coin),("forgeStone",stone))){current.stars++;profile.Track("upgrade");Changed("Đã rèn trang bị");}});
                options.Add("Tháo "+GearName(current));icons.Add(GearAsset(current));
                actions.Add(()=>{cat.gear.Remove(slot);Changed("Đã tháo trang bị");});
            }
            foreach(var g in profile.gear.Where(v=>GearSlot(v)==slot&&v!=current))
            {
                var owner=profile.cats.Find(c=>c!=cat&&c.gear.ContainsValue(g.uid));
                options.Add($"{GearName(g)} · {g.stars}★"+(owner!=null?" · của "+db.Cat(owner.id).name:""));icons.Add(GearAsset(g));
                actions.Add(()=>
                {
                    foreach(var c in profile.cats)foreach(var key in c.gear.Where(v=>v.Value==g.uid).Select(v=>v.Key).ToArray())c.gear.Remove(key);
                    cat.gear[slot]=g.uid;Changed("Đã trang bị");
                });
            }
            if(options.Count==0){Notify("Chưa có "+caption.ToLower()+". Vượt ải 2–4 và 6–7 để nhận trang bị.");return;}
            Pick(caption+" · "+db.Cat(cat.id).name,options,at=>actions[at](),icons);
        }
        void ArtifactSlotButton(float x,float y,OwnedCat cat)
        {
            var artifact=profile.artifacts.Find(a=>a.id==cat.artifact);
            if(EquipmentSlotButton(x,y,152,141,artifact?.name??"Cổ vật",artifact==null?null:"item-artifact"))
            {
                if(profile.artifacts.Count==0){Notify("Chưa có cổ vật.");return;}
                var choices=new[]{""}.Concat(profile.artifacts.Select(a=>a.id)).ToArray();
                Pick("Chọn cổ vật",choices.Select(id=>string.IsNullOrEmpty(id)?"Gỡ cổ vật":profile.artifacts.First(a=>a.id==id).name),at=>
                {
                    string next=choices[at];foreach(var c in profile.cats)if(c.artifact==next)c.artifact="";
                    cat.artifact=next;Changed("Đã cập nhật cổ vật");
                },choices.Select(id=>"item-artifact"));
            }
        }
    }
}
