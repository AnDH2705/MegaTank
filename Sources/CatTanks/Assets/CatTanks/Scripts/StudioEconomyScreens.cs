using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        const string SummonTabLabel = "Gọi đồng đội";
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
    }
}
