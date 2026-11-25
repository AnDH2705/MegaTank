using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void BattleHUD()
        {
            Panel(24,19,434,75,.97f);
            if(IllustratedButton(35,27,62,56,"II","Pause",content:StudioButtonLayout.Content.Symbol,normalSpriteKey:"blue"))paused=!paused;
            Art(111,31,44,44,"item-coins");Text(169,27,271,N(battle.coins)+" xu",subtitle);Text(169,61,271,battle.kills+" mục tiêu bị hạ",small,25);
            Panel(490,19,620,75,.97f);
            Text(514,27,572,battle.classic?$"THỬ THÁCH • ĐỢT {battle.wave+1}/6":battle.arena?"ĐẤU TRƯỜNG • MÔ PHỎNG":$"ẢI {battle.stage.index} • {battle.stage.name}",subtitle);
            Text(514,61,572,$"{Mathf.FloorToInt(battle.time/60):00}:{Mathf.FloorToInt(battle.time%60):00}   •   {battle.spawned}/{battle.waves[battle.wave].count} đã xuất hiện",small,25);
            if(IllustratedButton(1212,27,198,56,"BÁCH KHOA","Book_1_Purple",normalSpriteKey:"blue"))ToggleBook();
            if(IllustratedButton(1422,27,154,56,muted?"Âm: Tắt":"Âm: Bật","UI/music",normalSpriteKey:"blue"))ToggleSound();
            Panel(24,106,496,65,.96f);Text(42,111,460,$"PHÁO ĐÀI   {N(battle.hp)} / {N(battle.maxHp)} HP",small,26);
            Bar(42,142,460,15,battle.hp/battle.maxHp,C("#81e65a"));
            float hp=battle.enemies.Where(e=>!e.dead).Sum(e=>e.hp),max=battle.enemies.Where(e=>!e.dead).Sum(e=>e.maxHp);
            Panel(1080,106,496,65,.96f);Text(1098,111,460,battle.enemies.Count(e=>!e.dead)+" QUÁI ĐANG TIẾN ĐẾN",small,26);
            Bar(1098,142,460,15,hp/Mathf.Max(1,max),C("#ff6973"));
            Panel(24,728,1552,143,.98f);
            Text(46,749,264,"NĂNG LƯỢNG",small);Text(46,779,264,N(battle.energy)+" / 600",subtitle);
            Bar(46,819,254,18,battle.energy/600,gold);
            for(int i=0;i<battle.skills.Count;i++)
            {
                var skill=battle.skills[i];float x=330+i*145,cd=battle.Cooldown(skill.id);
                bool available=cd<=0&&battle.energy>=skill.cost&&!paused&&modal=="";
                if(Button(x,742,135,112,"",available))battle.UseSkill(skill.id);
                if(!dispatchOnly&&available)SetButtonSprite(lastButton,"cats-tab-selected");
                Art(x+48,750,34,34,"skill-"+skill.id);Text(x+10,750,30,(i+1).ToString(),hudMeta);
                Text(x+10,789,117,skill.name,hudText,38);
                Text(x+10,828,117,cd>0?Mathf.CeilToInt(cd)+"s hồi":skill.cost+" NL",hudMeta,26);
            }
            if(battle.CaptureEnabled)
            {
                if(IllustratedButton(1250,744,145,108,$"Lồng thú ×{battle.cages}\n[C]","item-beastCage",
                    !paused&&battle.cages>0,content:StudioButtonLayout.Content.Tile))battle.Catch(false);
                if(IllustratedButton(1410,744,150,108,$"Lồng trăng ×{battle.moonCages}\n[Shift+C]","item-moonCage",
                    !paused&&battle.moonCages>0,content:StudioButtonLayout.Content.Tile))battle.Catch(true);
            }
            var target=battle.enemies.Find(e=>e.id==battle.targetId&&!e.dead);
            if(target!=null)
            {
                Panel(550,111,500,61,.95f);Text(570,116,460,target.def.name+" • "+(target.hp/target.maxHp).ToString("P0")+" HP",label,27);
                if(battle.CaptureEnabled)Text(570,141,460,CaptureHint(target),small,25);
            }
            int floating=0;
            foreach(var fx in battle.effects)
            {
                if(fx.kind!="text")continue;
                var p=Camera.main.WorldToScreenPoint(fx.position+Vector2.up*(1-fx.life/fx.maxLife)*50);var ui=ToUI(new Vector2(p.x,Screen.height-p.y));
                FloatingText(floating++,ui,fx);
            }
        }
        string CaptureHint(Actor target)
        {
            bool boss=target.def.boss;
            float threshold=Battle.CaptureHealthThreshold(boss);
            if(!Battle.IsWeakEnoughToCapture(target))
                return boss?$"Trùm: dưới {threshold:0%} HP, cần Lồng trăng.":$"Giảm HP còn {threshold:0%} hoặc thấp hơn để bắt.";
            if(!boss&&battle.cages>0)return "Đủ điều kiện ném Lồng thú [C].";
            if(battle.moonCages>0)return "Đủ điều kiện ném Lồng trăng [Shift+C].";
            return boss?"Cần Lồng trăng để bắt trùm.":"Hết lồng đã mang.";
        }
        void PausePopup()
        {
            PopupFrame(480,230,640,450,"Tạm dừng",false);
            Art(512,290,170,170,"cat-0");
            Text(700,300,390,"Nghỉ một chút nhé!",subtitle,40);
            Text(700,344,390,(battle.classic?"Thử thách trăng":battle.arena?"Đấu trường":$"Ải {battle.stage.index:00}")+$" · {Mathf.FloorToInt(battle.time/60):00}:{Mathf.FloorToInt(battle.time%60):00}\nPháo đài {N(battle.hp)} / {N(battle.maxHp)} HP",small,60);
            if(IllustratedButton(520,480,560,80,"TIẾP TỤC","Play",primary:true))paused=false;
            Enlarge(26);
            if(IllustratedButton(520,578,270,66,"Cài đặt","Interface/Cats/SettingsIcon",normalSpriteKey:"blue"))modal="settings";
            if(Button(810,578,270,66,"Rút lui"))ReturnFromBattle();
        }
    }
}
