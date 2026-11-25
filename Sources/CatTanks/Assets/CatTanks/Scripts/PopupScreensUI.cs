using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    // Popup Rương AFK (chạm rương trên màn chính, như bảng chiến lợi phẩm của game idle) và Bách khoa dạng
    // sổ tay sưu tầm (lưới thẻ bên trái, chi tiết bên phải). Chỉ lấy cách sắp xếp; art vẫn là của CatTanks.
    public sealed partial class CatTanksGame
    {
        const string ChestModal="afk";
        int bookPick;
        bool backToPrebattle;

        // ---- Rương AFK ----
        void ChestPopup()
        {
            float elapsed=Mathf.Clamp(Profile.Now-profile.lastIdle,0,43200);var rewards=profile.Idle(Profile.Now);
            PopupFrame(300,150,1000,610,"Rương AFK",true);
            Art(340,214,300,250,"item-afkChest");
            Text(320,470,340,$"{Mathf.FloorToInt(elapsed/3600):00}:{Mathf.FloorToInt(elapsed%3600/60):00}:{Mathf.FloorToInt(elapsed%60):00}",title,60,align:TextAnchor.UpperCenter,size:44);
            Text(320,530,340,"TÍCH LŨY TỐI ĐA 12 GIỜ",small,28,align:TextAnchor.UpperCenter);
            Bar(340,566,300,20,elapsed/43200,gold);
            Text(320,600,340,"Vượt thêm ải để tăng thu nhập.",small,30,align:TextAnchor.UpperCenter);

            Surface(680,206,580,410,Color.white,catsFrame:"Roster");
            Text(700,218,540,"CHIẾN LỢI PHẨM ĐANG CHỜ",subtitle,36);
            var gains=rewards.Where(r=>r.Value>0).Take(6).ToList();
            if(gains.Count==0)Text(700,380,540,"Chưa có gì để nhận · quay lại sau ít phút nhé!",label,40,align:TextAnchor.UpperCenter);
            for(int i=0;i<gains.Count;i++)
            {
                float x=700+i%2*276,y=266+i/2*112;string icon=RewardIcon(gains[i].Key);
                Surface(x,y,262,100,Color.white,catsFrame:"Tile");
                if(icon!=null)Art(x+12,y+18,64,64,icon);
                Text(x+86,y+18,166,"+"+N(gains[i].Value),subtitle,36);
                Text(x+86,y+54,166,Currency(gains[i].Key),small,28);
            }
            if(IllustratedButton(680,636,580,90,elapsed<60?"ĐANG TÍCH LŨY • ĐỢI 1 PHÚT":"NHẬN TOÀN BỘ","item-afkChest",elapsed>=60,true)){ClaimIdle();modal="";}
            Enlarge(26);
        }

        // ---- Bách khoa ----
        struct BookEntry{public string asset,name,info;}
        List<BookEntry> BookEntries()
        {
            if(bookTab==3)return db.monsters.Select(d=>new BookEntry{asset=d.asset,name=d.name,info=$"HP {N(d.hp)} · ATK {N(d.damage)}"+(d.armor>0?$" · Giáp {d.armor:0%}":"")+$"\n\n{d.description}"}).ToList();
            if(bookTab==4)return db.cats.Select(d=>new BookEntry{asset=d.id,name=d.name,info=$"{d.rarity} · {RoleName(d.role)}\n{(d.specialty=="operator"?"Chuyên vận hành pháo":"Chuyên bộ binh")}"}).ToList();
            string prefix=bookTab==0?"turret":bookTab==1?"gun":"pack";
            return db.weapons.Where(w=>w.id.StartsWith(prefix)).Select(d=>new BookEntry{asset=d.asset,name=d.name,info=$"ATK {N(d.damage)} · {d.rate:0.##}s/phát\n\n{d.description}"}).ToList();
        }
        void Book()
        {
            PopupFrame(98,90,1404,740,"Bách khoa trăng xanh",true);
            string[] names={"THÁP PHÁO","VŨ KHÍ MÈO","MODULE","QUÁI VẬT","MÈO"};
            string[] icons={"turret-0","gun-0","pack-0","bug-0","cat-0"};
            for(int i=0;i<5;i++)if(TabButton(134+i*268,165,258,54,names[i],bookTab==i,icons[i])){bookTab=i;bookPick=0;scroll=Vector2.zero;}
            var entries=BookEntries();
            Surface(124,232,900,575,Color.white,catsFrame:"Roster");
            scroll=BeginScroll(new Rect(140,248,868,543),scroll,new Rect(0,0,850,Mathf.Ceil(entries.Count/5f)*190));
            for(int i=0;i<entries.Count;i++)
            {
                if(IllustratedButton(i%5*172,i/5*190,164,180,entries[i].name,entries[i].asset,content:StudioButtonLayout.Content.Tile,selected:bookPick==i))bookPick=i;
                Enlarge(17);
            }
            EndScroll();
            Surface(1040,232,436,575,Color.white,catsFrame:"Detail");
            if(entries.Count==0)return;
            bookPick=Mathf.Clamp(bookPick,0,entries.Count-1);var pick=entries[bookPick];
            Text(1062,246,392,pick.name,title,56,size:34);
            Art(1070,310,376,220,pick.asset);
            Text(1064,546,388,pick.info,small,250);
        }

        // ---- Quay về sau trận: trận đấu trường về lại Đấu trường, còn lại về màn chính ----
        void ReturnFromBattle()
        {
            bool arena=battle!=null&&battle.arena;
            Hub();
            if(arena){page="town";tab="arena";}else page="march";
            scroll=Vector2.zero;
        }
    }
}
