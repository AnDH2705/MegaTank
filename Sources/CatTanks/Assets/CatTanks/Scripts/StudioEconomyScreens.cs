using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        const string SummonTabLabel = "Gọi đồng đội";
        void Town()
        {
            string[] tabs={"summon","shop","arena"},names={SummonTabLabel.ToUpperInvariant(),"TIẾP TẾ","ĐẤU TRƯỜNG"};
            string[] icons={"item-summonScroll","item-afkChest","item-arenaToken"};
            if(!tabs.Contains(tab))tab="summon";
            for(int i=0;i<3;i++)if(TabButton(290+i*405,148,385,55,names[i],tab==tabs[i],icons[i])){tab=tabs[i];scroll=Vector2.zero;}
            useCodeGeometry=true;
            if(tab=="shop"){Shop();return;} if(tab=="arena"){Arena();return;}
            Text(290,226,1220,"Một tín hiệu. Một đồng đội mới.",title,57);
            Text(290,285,1220,"Bảo đảm mèo Elite sau 10 lượt. Bản sao giúp đồng đội thăng bậc.",label);
            Surface(282,326,1266,309,Color.white,catsFrame:"Roster");
            string[] cats={"cat-2","cat-0","cat-4"};string[] roles={"BỘ BINH","GIỮ PHÁO ĐÀI","ĐỒNG ĐỘI MỚI"};
            for(int i=0;i<3;i++)
            {
                float x=302+i*414;Surface(x,345,398,271,Color.white,catsFrame:i==1?"Cyan":"Tile");
                Art(x+81,357,236,194,cats[i]);Text(x+26,571,344,roles[i],subtitle);
            }
            Surface(282,651,1266,152,Color.white,catsFrame:"Detail");
            Text(302,665,1216,$"{profile.Get("summonScroll")} VÉ ĐANG CÓ    •    Còn {10-profile.pityElite} lượt đến mèo Elite",subtitle);
            bool unlocked=profile.highestCleared>=4;
            if(IllustratedButton(302,703,602,60,"GỌI 1 LẦN  •  1 VÉ","item-summonScroll",unlocked&&profile.Get("summonScroll")>=1,normalSpriteKey:"blue")) {Notify(profile.Summon(db,1));Changed(toast);}
            if(IllustratedButton(918,703,610,60,"GỌI 10 LẦN  •  10 VÉ","item-summonScroll",unlocked&&profile.Get("summonScroll")>=10,true)) {Notify(profile.Summon(db,10));Changed(toast);}
            Text(302,771,1216,unlocked?"Vé có thể nhận từ chiến dịch, nhiệm vụ và cửa hàng.":"MỞ KHÓA SAU ẢI 4 • Nâng cấp ba mèo đầu tiên trong Doanh trại để tiến xa hơn.",small,25);
        }
        void Shop()
        {
            Text(290,226,1100,"Trạm tiếp tế",title,54);
            Art(290,280,32,32,"item-arenaToken");
            Text(330,285,1152,$"Vật phẩm làm mới mỗi ngày  •  {profile.Get("arenaToken")} huy hiệu đấu trường  •  {N(profile.Get("spiritDust"))} bụi",small);
            var stock=new[]{
                ("Lồng thú","coins",1500,"beastCage",1,5),("Lồng thú","gem",80,"beastCage",1,5),("Lồng trăng","gem",250,"moonCage",1,2),
                ("10 thức ăn","coins",200,"monsterChow",10,5),("Vé triệu hồi","gem",100,"summonScroll",1,10),("100 bụi","coins",500,"spiritDust",100,3),
                ("150 bụi","arenaToken",8,"spiritDust",150,5),("20 tinh chất","arenaToken",8,"soulEssence",20,3),("5 đá rèn","arenaToken",10,"forgeStone",5,3),
                ("Vé triệu hồi","arenaToken",16,"summonScroll",1,2),("15 thức ăn","arenaToken",6,"monsterChow",15,5),("Lõi kỹ năng","arenaToken",40,"skillCore",1,1)};
            profile.RefreshDay();
            Surface(282,322,1266,478,Color.white,catsFrame:"Roster");
            BeginScroll(new Rect(298,338,1234,446),scroll,new Rect(0,0,1215,588));
            for(int i=0;i<stock.Length;i++)
            {
                var v=stock[i];string key="shop-"+i;int bought=profile.bought.TryGetValue(key,out int n)?n:0;
                float x=i%4*304,y=i/4*196;Surface(x,y,292,183,Color.white,catsFrame:"Tile");Art(x+16,y+15,64,64,"item-"+v.Item4);
                Text(x+95,y+19,181,v.Item1,subtitle,49);Text(x+95,y+72,181,$"Còn {v.Item6-bought}/{v.Item6}",small);
                bool canBuy=profile.highestCleared>=3&&bought<v.Item6&&profile.Get(v.Item2)>=v.Item3;
                string caption=profile.highestCleared<3?"MỞ SAU ẢI 3":bought>=v.Item6?"HẾT HÔM NAY":Cost((v.Item2,v.Item3));
                if(Button(x+14,y+119,264,50,caption,canBuy,true))
                    if(Pay((v.Item2,v.Item3))){profile.Add(v.Item4,v.Item5);profile.bought[key]=bought+1;Changed("Đã nhận "+v.Item1);}
            }
            EndScroll();
        }
        void Chest()
        {
            Text(290,165,1100,"Chiến lợi phẩm đang chờ bạn",title,57);
            Text(292,220,1120,"Mèo vẫn thu thập tài nguyên khi bạn vắng mặt. Nhớ trở về nhận nhé!",label);
            float elapsed=Mathf.Clamp(Profile.Now-profile.lastIdle,0,43200);var rewards=profile.Idle(Profile.Now);
            Surface(282,289,506,510,Color.white,catsFrame:"Roster");Art(361,337,348,340,"item-afkChest");
            Text(318,718,440,"RƯƠNG CHIẾN LỢI PHẨM",subtitle);
            Surface(806,289,742,510,Color.white,catsFrame:"Detail");
            Text(836,324,676,$"{Mathf.FloorToInt(elapsed/3600):00} GIỜ  {Mathf.FloorToInt(elapsed%3600/60):00} PHÚT",title,57);
            Text(836,383,676,"TÍCH LŨY TỐI ĐA 12 GIỜ",small);
            Bar(836,422,676,22,elapsed/43200,gold);
            Surface(836,464,682,146,Color.white,catsFrame:"Roster");
            var gains=rewards.Where(r=>r.Value>0).Take(6).ToList();
            if(gains.Count==0)Text(856,516,638,"Chưa có gì để nhận · quay lại sau ít phút nhé!",label,36);
            for(int i=0;i<gains.Count;i++)
            {
                float x=856+i%3*220,y=476+i/3*64;string icon=RewardIcon(gains[i].Key);
                if(icon!=null)Art(x,y,46,46,icon);
                Text(x+54,y+2,160,"+"+N(gains[i].Value),subtitle,30);
                Text(x+54,y+30,160,Currency(gains[i].Key),small,24);
            }
            if(IllustratedButton(836,649,682,72,elapsed<60?"ĐANG TÍCH LŨY • ĐỢI 1 PHÚT":"NHẬN TOÀN BỘ","item-afkChest",elapsed>=60,true))
            ClaimIdle();
            Text(836,746,676,"Vượt thêm ải để tăng thu nhập.",small);
        }
        // Nhận rương AFK: dùng chung cho trang Rương AFK và ô rương trên màn chính.
        void ClaimIdle()
        {
            var rewards=profile.Idle(Profile.Now);profile.Grant(rewards);profile.lastIdle=Math.Max(profile.lastIdle,Profile.Now);profile.Track("idle");Changed("Đã nhận chiến lợi phẩm AFK");
        }
        void Quests()
        {
            profile.RefreshDay();Text(290,165,1100,"Một ngày đầy chiến công",title,57);
            Text(292,220,1150,"Hoàn thành 5 nhiệm vụ để nhận rương ngày. Tiến độ làm mới theo ngày trên máy.",label);
            string[] ids={"idle","wins","upgrade","capture","pvp"},names={"Thu nhận chiến lợi phẩm AFK","Thắng 3 trận hành quân","Nâng cấp đồng đội hoặc pháo","Mở lồng hoặc bắt một quái","Tham gia một trận đấu trường"};
            string[] icons={"Chest_Gold","Battle","item-catXp","item-beastCage","Badge_Crown"};
            for(int i=0;i<5;i++)
            {
                float y=284+i*86;int need=i==1?3:1,n=profile.daily.TryGetValue(ids[i],out int v)?v:0;
                bool claimed=profile.claimed.Contains(ids[i]),ready=n>=need&&!claimed;
                Surface(282,y,1266,76,Color.white,catsFrame:ready?"Roster":"Detail");Art(307,y+12,50,50,icons[i]);
                Text(377,y+8,775,names[i],subtitle);Text(377,y+43,775,$"{Mathf.Min(n,need)}/{need}    •    40 xu + 10 XP mèo",small,25);
                if(Button(1190,y+12,332,52,claimed?"ĐÃ NHẬN":ready?"NHẬN THƯỞNG":"ĐANG THỰC HIỆN",ready,ready))
                {profile.claimed.Add(ids[i]);profile.Add("coins",40);profile.Add("catXp",10);Changed("Đã nhận thưởng nhiệm vụ");}
            }
            bool complete=ids.All(profile.claimed.Contains),bonus=profile.claimed.Contains("bonus");
            int done=ids.Count(profile.claimed.Contains);
            Surface(282,722,1266,88,Color.white,catsFrame:"Ink");
            Art(300,732,68,68,"item-afkChest");
            Text(384,732,420,"RƯƠNG NGÀY",subtitle,34);
            Text(384,768,420,"800 xu · 20 ngọc · 200 bụi · 1 vé · 10 thức ăn",small,26);
            Text(820,730,360,$"{done} / 5 NHIỆM VỤ",small,26,align:TextAnchor.UpperCenter);
            Bar(820,762,360,24,done/5f,gold);
            if(Button(1206,738,320,60,bonus?"ĐÃ NHẬN":complete?"MỞ RƯƠNG":"CHƯA ĐỦ",complete&&!bonus,complete&&!bonus))
            {profile.claimed.Add("bonus");profile.Grant(new Dictionary<string,int>{{"coins",800},{"spiritDust",200},{"gem",20},{"summonScroll",1},{"monsterChow",10},{"accountXp",150}});Changed("Đã nhận rương ngày");}
        }
    }
}
