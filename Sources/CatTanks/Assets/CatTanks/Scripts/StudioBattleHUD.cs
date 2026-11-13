using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void BattleHUD()
        {
            Panel(24,20,440,84,.97f);
            if(Button(36,34,62,54,"II"))paused=!paused;
            Icon(112,36,44,"Money_Coin");Text(172,36,270,N(battle.coins)+" xu",subtitle);Text(172,70,270,battle.kills+" mục tiêu bị hạ",small);
            Panel(550,20,500,84,.97f);
            Text(572,30,455,battle.classic?$"THỬ THÁCH • ĐỢT {battle.wave+1}/6":battle.arena?"ĐẤU TRƯỜNG • MÔ PHỎNG":$"ẢI {battle.stage.index} • {battle.stage.name}",subtitle);
            Text(572,69,455,$"{Mathf.FloorToInt(battle.time/60):00}:{Mathf.FloorToInt(battle.time%60):00}   •   {battle.spawned}/{battle.waves[battle.wave].count} đã xuất hiện",small);
            if(Button(1160,34,190,54,"BÁCH KHOA"))modal="book";
            if(Button(1363,34,103,54,muted?"Âm: Tắt":"Âm: Bật"))ToggleSound();
            if(Button(1478,34,96,54,"Thoát"))paused=true;
            Panel(30,116,510,78,.96f);Text(48,126,470,$"PHÁO ĐÀI   {N(battle.hp)} / {N(battle.maxHp)} HP",small);
            Bar(48,162,474,16,battle.hp/battle.maxHp,C("#81e65a"));
            float hp=battle.enemies.Where(e=>!e.dead).Sum(e=>e.hp),max=battle.enemies.Where(e=>!e.dead).Sum(e=>e.maxHp);
            Panel(1060,116,510,78,.96f);Text(1078,126,470,battle.enemies.Count(e=>!e.dead)+" QUÁI ĐANG TIẾN ĐẾN",small);
            Bar(1078,162,474,16,hp/Mathf.Max(1,max),C("#ff6973"));
            Panel(24,728,1552,139,.98f);
            Text(46,749,264,"NĂNG LƯỢNG",small);Text(46,779,264,N(battle.energy)+" / 600",subtitle);
            Bar(46,819,254,18,battle.energy/600,gold);
            for(int i=0;i<battle.skills.Count;i++)
            {
                var skill=battle.skills[i];float x=330+i*145,cd=battle.Cooldown(skill.id);
                bool available=cd<=0&&battle.energy>=skill.cost&&!paused&&modal=="";
                if(Button(x,742,135,112,"",available))battle.UseSkill(skill.id);
                if(!dispatchOnly&&available)SetButtonSprite(lastButton,"selected");
                Icon(x+48,750,34,i%2==0?"Gear":"Damage");Text(x+10,750,30,(i+1).ToString(),hudMeta);
                Text(x+10,789,117,skill.name,hudText,38);
                Text(x+10,828,117,cd>0?Mathf.CeilToInt(cd)+"s hồi":skill.cost+" NL",hudMeta,26);
            }
            if(!battle.classic&&!battle.arena)
            {
                if(Button(1250,744,145,108,$"[C] BẮT QUÁI\n{battle.cages} lồng thú",!paused&&battle.cages>0))battle.Catch(false);
                if(Button(1410,744,150,108,$"[Shift+C]\n{battle.moonCages} lồng trăng",!paused&&battle.moonCages>0))battle.Catch(true);
            }
            var target=battle.enemies.Find(e=>e.id==battle.targetId&&!e.dead);
            if(target!=null)
            {
                Panel(555,208,490,76,.95f);Text(574,219,450,target.def.name+" • "+(target.hp/target.maxHp).ToString("P0")+" HP",subtitle);
                Text(574,253,450,target.hp/target.maxHp<=.25f?"Đã đủ yếu để bắt! Nhấn C.":"Làm yếu xuống dưới 25% HP để bắt.",small);
            }
            foreach(var fx in battle.effects.Where(e=>e.kind=="text"))
            {
                var p=Camera.main.WorldToScreenPoint(fx.position+Vector2.up*(1-fx.life/fx.maxLife)*50);var ui=ToUI(new Vector2(p.x,Screen.height-p.y));
                textTint=fx.color;Text(ui.x-25,ui.y,110,fx.text,small);textTint=Color.white;
            }
            Text(36,875,1530,"A / D  Di chuyển     Chuột  Khóa mục tiêu     1–4  Kỹ năng     C  Bắt quái     P  Tạm dừng     B  Bách khoa",small);
        }
        void PausePopup()
        {
            Panel(490,270,620,330);Text(555,310,490,"NGHỈ MỘT CHÚT NHÉ!",title);
            if(Button(550,401,500,60,"Tiếp tục",true,true))paused=false;
            if(Button(550,486,500,60,"Rút lui về doanh trại")){Hub();page="barracks";tab="cats";}
        }
    }
}
