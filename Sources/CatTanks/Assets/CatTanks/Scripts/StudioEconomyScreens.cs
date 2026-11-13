using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void Town()
        {
            string[] tabs={"summon","shop","arena"},names={"GỌI ĐỒNG ĐỘI","TIẾP TẾ","ĐẤU TRƯỜNG"};
            if(!tabs.Contains(tab))tab="summon";
            for(int i=0;i<3;i++)if(TabButton(290+i*405,148,385,55,names[i],tab==tabs[i])){tab=tabs[i];scroll=Vector2.zero;}
            if(tab=="shop"){Shop();return;} if(tab=="arena"){Arena();return;}
            Text(290,232,1100,"Một tín hiệu. Một đồng đội mới.",title);
            Text(292,290,1140,"Bảo đảm mèo Elite sau 10 lượt. Bản sao giúp đồng đội thăng bậc.",label);
            string[] cats={"cat-2","cat-0","cat-4"};string[] roles={"BỘ BINH","GIỮ PHÁO ĐÀI","ĐỒNG ĐỘI MỚI"};
            for(int i=0;i<3;i++)
            {
                float x=340+i*374;Surface(x,356,330,279,C(i==1?"#344469":"#202f4d"));
                Art(x+50,374,230,185,cats[i]);Text(x+64,581,240,roles[i],subtitle);
            }
            Text(340,657,1150,$"{profile.Get("summonScroll")} VÉ ĐANG CÓ    •    Còn {10-profile.pityElite} lượt đến mèo Elite",subtitle);
            bool unlocked=profile.highestCleared>=4;
            if(Button(340,724,530,72,"GỌI 1 LẦN  •  1 VÉ",unlocked&&profile.Get("summonScroll")>=1)) {Notify(profile.Summon(db,1));Changed(toast);}
            if(Button(900,724,560,72,"GỌI 10 LẦN  •  10 VÉ",unlocked&&profile.Get("summonScroll")>=10,true)) {Notify(profile.Summon(db,10));Changed(toast);}
            Text(340,813,1130,unlocked?"Vé có thể nhận từ chiến dịch, nhiệm vụ và cửa hàng.":"MỞ KHÓA SAU ẢI 4 • Nâng cấp ba mèo đầu tiên trong Doanh trại để tiến xa hơn.",small);
        }
        void Shop()
        {
            Text(290,230,1100,"Trạm tiếp tế",title);
            Text(292,285,1190,$"Vật phẩm làm mới mỗi ngày  •  {profile.Get("arenaToken")} huy hiệu đấu trường  •  {N(profile.Get("spiritDust"))} bụi",small);
            var stock=new[]{
                ("Lồng thú","coins",1500,"beastCage",1,5,"Bag_Brown"),("Lồng thú","gem",80,"beastCage",1,5,"Bag_Brown"),("Lồng trăng","gem",250,"moonCage",1,2,"Gem_Star_Blue"),
                ("10 thức ăn","coins",200,"monsterChow",10,5,"Bag_Brown"),("Vé triệu hồi","gem",100,"summonScroll",1,10,"Ticket_Gold"),("100 bụi","coins",500,"spiritDust",100,3,"Gem_Circle"),
                ("150 bụi","arenaToken",8,"spiritDust",150,5,"Gem_Star_Blue"),("20 tinh chất","arenaToken",8,"soulEssence",20,3,"Gem_Diamond_Purple"),("5 đá rèn","arenaToken",10,"forgeStone",5,3,"Gear"),
                ("Vé triệu hồi","arenaToken",16,"summonScroll",1,2,"Ticket_Gold"),("15 thức ăn","arenaToken",6,"monsterChow",15,5,"Bag_Brown"),("Lõi kỹ năng","arenaToken",40,"skillCore",1,1,"Gem_Star_Blue")};
            profile.RefreshDay();
            BeginScroll(new Rect(290,333,1230,488),scroll,new Rect(0,0,1190,630));
            for(int i=0;i<stock.Length;i++)
            {
                var v=stock[i];string key="shop-"+i;int bought=profile.bought.TryGetValue(key,out int n)?n:0;
                float x=i%4*298,y=i/4*210;Panel(x,y,280,194);Icon(x+16,y+16,58,v.Item7);
                Text(x+88,y+18,175,v.Item1,subtitle,58);Text(x+88,y+80,175,$"Còn {v.Item6-bought}/{v.Item6}",small);
                bool canBuy=profile.highestCleared>=3&&bought<v.Item6&&profile.Get(v.Item2)>=v.Item3;
                string caption=profile.highestCleared<3?"MỞ SAU ẢI 3":bought>=v.Item6?"HẾT HÔM NAY":Cost((v.Item2,v.Item3));
                if(Button(x+14,y+130,252,50,caption,canBuy,true))
                    if(Pay((v.Item2,v.Item3))){profile.Add(v.Item4,v.Item5);profile.bought[key]=bought+1;Changed("Đã nhận "+v.Item1);}
            }
            EndScroll();
        }
        void Chest()
        {
            Text(290,165,1100,"Chiến lợi phẩm đang chờ bạn",title);
            Text(292,220,1120,"Mèo vẫn thu thập tài nguyên khi bạn vắng mặt. Nhớ trở về nhận nhé!",label);
            float elapsed=Mathf.Clamp(Profile.Now-profile.lastIdle,0,43200);var rewards=profile.Idle(Profile.Now);
            Surface(290,300,510,490,C("#243656"));Icon(365,348,360,"Chest_Gold");
            Text(335,719,420,"RƯƠNG CHIẾN LỢI PHẨM",subtitle);
            Surface(830,300,690,490,C("#1d2c48"));
            Text(865,333,620,$"{Mathf.FloorToInt(elapsed/3600):00} GIỜ  {Mathf.FloorToInt(elapsed%3600/60):00} PHÚT",title);
            Text(865,391,620,"TÍCH LŨY TỐI ĐA 12 GIỜ",small);
            Text(865,451,600,Rewards(rewards),label,128);Bar(865,597,620,24,elapsed/43200,gold);
            if(Button(865,659,620,76,elapsed<60?"ĐANG TÍCH LŨY • ĐỢI 1 PHÚT":"NHẬN TOÀN BỘ",elapsed>=60,true))
            {profile.Grant(rewards);profile.lastIdle=Math.Max(profile.lastIdle,Profile.Now);profile.Track("idle");Changed("Đã nhận chiến lợi phẩm AFK");}
            Text(865,751,620,"Vượt thêm ải để tăng thu nhập.",small);
        }
        void Quests()
        {
            profile.RefreshDay();Text(290,165,1100,"Một ngày đầy chiến công",title);
            Text(292,220,1150,"Hoàn thành 5 nhiệm vụ để nhận rương ngày. Tiến độ làm mới theo ngày trên máy.",label);
            string[] ids={"idle","wins","upgrade","capture","pvp"},names={"Thu nhận chiến lợi phẩm AFK","Thắng 3 trận hành quân","Nâng cấp đồng đội hoặc pháo","Mở lồng hoặc bắt một quái","Tham gia một trận đấu trường"};
            string[] icons={"Chest_Gold","Battle","Gear","Bag_Brown","Badge_Crown"};
            for(int i=0;i<5;i++)
            {
                float y=296+i*88;int need=i==1?3:1,n=profile.daily.TryGetValue(ids[i],out int v)?v:0;
                bool claimed=profile.claimed.Contains(ids[i]),ready=n>=need&&!claimed;
                Surface(290,y,1225,76,C(ready?"#29495a":"#202f4c"));Icon(310,y+12,50,icons[i]);
                Text(381,y+9,680,names[i],subtitle);Text(381,y+43,680,$"{Mathf.Min(n,need)}/{need}    •    40 xu + 10 XP mèo",small);
                if(Button(1190,y+10,305,56,claimed?"ĐÃ NHẬN":ready?"NHẬN THƯỞNG":"ĐANG THỰC HIỆN",ready,ready))
                {profile.claimed.Add(ids[i]);profile.Add("coins",40);profile.Add("catXp",10);Changed("Đã nhận thưởng nhiệm vụ");}
            }
            bool complete=ids.All(profile.claimed.Contains),bonus=profile.claimed.Contains("bonus");
            if(Button(290,761,1225,67,bonus?"ĐÃ NHẬN RƯƠNG NGÀY":$"{ids.Count(profile.claimed.Contains)}/5 NHIỆM VỤ    •    RƯƠNG NGÀY: 800 XU + 20 NGỌC + VẬT LIỆU",complete&&!bonus,true))
            {profile.claimed.Add("bonus");profile.Grant(new Dictionary<string,int>{{"coins",800},{"spiritDust",200},{"gem",20},{"summonScroll",1},{"monsterChow",10},{"accountXp",150}});Changed("Đã nhận rương ngày");}
        }
    }
}
