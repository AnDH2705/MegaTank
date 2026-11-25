using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    // Luồng vào/ra trận theo bố cục quen thuộc của game idle (chỉ cách sắp xếp, art vẫn là của CatTanks):
    //  • Bản đồ chiến dịch ← bản đồ chương: các ải nối thành đường đi theo chương, bảng chi tiết ải bên phải.
    //  • Chuẩn bị xuất kích ← màn chuẩn bị trận: đội mình bên trái, quái sẽ gặp bên phải, nút XUẤT KÍCH lớn.
    //  • MVP ở màn kết quả ← bảng sát thương sau trận.
    public sealed partial class CatTanksGame
    {
        const int StagesPerChapter=5;
        int mapChapter=-1,mapPick=-1;
        static readonly float[] NodeX={190,360,530,700,830},NodeY={600,440,600,440,240},NodeW={150,150,150,150,170},NodeH={110,110,110,110,130};

        // ================= Bản đồ chiến dịch =================
        void MapPopup()
        {
            int chapters=Mathf.CeilToInt(db.stages.Count/(float)StagesPerChapter);
            if(mapChapter<0||mapChapter>=chapters)mapChapter=Mathf.Clamp((profile.selected-1)/StagesPerChapter,0,chapters-1);
            if(mapPick<0||mapPick>=db.stages.Count)mapPick=profile.selected-1;
            PopupFrame(130,80,1340,760,"Bản đồ chiến dịch",true);
            for(int c=0;c<chapters;c++)
            {
                bool open=c*StagesPerChapter<=profile.highestCleared;
                if(TabButton(170+c*230,128,220,52,"CHƯƠNG "+(c+1),mapChapter==c,open?null:"Lock")){mapChapter=c;mapPick=c*StagesPerChapter;}
            }
            Text(1110,140,330,$"{profile.highestCleared} / {db.stages.Count} ải đã qua",small,30,align:TextAnchor.UpperRight);

            // Đường đi các ải trong chương: chấm nối giữa các nút, ải trùm lớn ở cuối.
            Surface(160,196,862,616,Color.white,catsFrame:"Roster");
            int first=mapChapter*StagesPerChapter;
            for(int k=0;k+1<StagesPerChapter&&first+k+1<db.stages.Count;k++)
            {
                Vector2 a=new Vector2(NodeX[k]+NodeW[k]/2,NodeY[k]+NodeH[k]/2),b=new Vector2(NodeX[k+1]+NodeW[k+1]/2,NodeY[k+1]+NodeH[k+1]/2);
                for(int d=1;d<8;d++){var p=Vector2.Lerp(a,b,d/8f);Surface(p.x-8,p.y-8,16,16,Color.white,catsFrame:first+k<profile.highestCleared?"Gold":"Ink");}
            }
            for(int k=0;k<StagesPerChapter&&first+k<db.stages.Count;k++)
            {
                int i=first+k;var stage=db.stages[i];bool unlocked=i<=profile.highestCleared;
                string badge=!unlocked?"Lock":i<profile.highestCleared?"Check":stage.boss?"Skull":null;
                if(IllustratedButton(NodeX[k],NodeY[k],NodeW[k],NodeH[k],$"{stage.index:00}",StagePreview(stage),unlocked,false,
                    StudioButtonLayout.Content.Stage,badge,mapPick==i))mapPick=i;
                Text(NodeX[k]+6,NodeY[k]+4,NodeW[k]-40,stage.name,small,44,size:16);
            }

            // Chi tiết ải đang chọn.
            var pick=db.stages[mapPick];bool pickOpen=mapPick<=profile.highestCleared;float power=profile.Power(db);
            Surface(1038,196,402,616,Color.white,catsFrame:"Detail");
            Art(1056,212,366,212,StagePreview(pick));
            Text(1058,432,362,pick.name,title,52,size:34);
            Text(1058,484,362,$"ẢI {pick.index:00}"+(pick.boss?"  ·  TRẬN TRÙM":"")+"  ·  "+(mapPick<profile.highestCleared?"ĐÃ QUA":pickOpen?"ĐANG MỞ":"CHƯA MỞ"),subtitle,36);
            Text(1058,524,362,$"Lực chiến đề nghị {N(pick.recommendedPower)}\nĐội bạn {N(power)} · "+(power>=pick.recommendedPower?"Sẵn sàng":"Nên nâng cấp"),small,56);
            var firstClear=new List<string>();
            if(!string.IsNullOrEmpty(pick.gearReward))firstClear.Add("trang bị");
            if(!string.IsNullOrEmpty(pick.moduleReward))firstClear.Add("module "+db.Weapon(pick.moduleReward).name);
            Text(1058,586,362,firstClear.Count>0?"Thưởng lần đầu: "+string.Join(", ",firstClear):"Thưởng: xu, XP mèo, bụi",small,56);
            if(Button(1056,656,366,60,"CHỌN ẢI NÀY",pickOpen,normalSpriteKey:"blue")){modal="";SelectStage(mapPick+1);}
            Enlarge(22);
            if(IllustratedButton(1056,726,366,76,"XUẤT KÍCH","Battle",pickOpen,true)){SelectStage(mapPick+1);modal="prebattle";}
            Enlarge(28);
        }

        // ================= Chuẩn bị xuất kích =================
        void Prebattle()
        {
            var s=db.stages[profile.selected-1];float power=profile.Power(db);
            PopupFrame(120,90,1360,750,$"Ải {s.index:00} · {s.name}",true);
            Text(150,138,1300,$"LỰC CHIẾN ĐỘI BẠN {N(power)}   ·   ĐỀ NGHỊ {N(s.recommendedPower)}   ·   "+(power>=s.recommendedPower?"SẴN SÀNG":"NÊN NÂNG CẤP"),subtitle,36,align:TextAnchor.UpperCenter);

            // Đội của bạn: chiếc xe pháo đài ghép đúng như trong trận, bộ binh đứng trước xe.
            // Chạm một tầng hoặc một mèo để mở Đội hình đúng ô đó (‹ quay lại đây).
            Surface(150,184,640,430,Color.white,catsFrame:"Roster");
            Text(168,194,600,"ĐỘI CỦA BẠN · chạm để chỉnh",subtitle,36);
            FortressPortrait(330,594,Mathf.Min(1.05f,360f/(profile.FloorCount*80+129)));
            Text(500,238,272,"PHÁO ĐÀI",subtitle,36);
            Text(500,272,272,$"{profile.FloorCount}/5 tầng · HP {N(profile.MaxHp)}\nChạm một tầng để đổi pháo hoặc mèo vận hành.",small,80);
            Text(500,462,272,"BỘ BINH",subtitle,36);
            for(int i=0;i<5;i++)
            {
                float x=500+i*56;bool open=i<profile.SquadCount;string id=profile.squad[i];
                if(!open)Art(x+10,540,36,36,"Lock");
                else Art(x,526,54,62,string.IsNullOrEmpty(id)?"cat-0":id);
                if(HotSpot(x,506,54,90))EditFormation("inf-"+i);
            }

            // Quái sẽ gặp.
            var species=s.waves.SelectMany(w=>w.types.Select(t=>t.ToString())).Where(t=>t!="beast-8").Distinct().Take(s.boss?7:8).ToList();
            if(s.boss)species.Add("beast-8");
            Surface(810,184,640,430,Color.white,catsFrame:"Detail");
            Text(828,194,600,$"QUÁI SẼ GẶP · {s.waves.Count} đợt · {s.waves.Sum(w=>w.count)} con",subtitle,36);
            for(int k=0;k<species.Count;k++)
            {
                float x=826+k%4*154,y=236+k/4*186;bool boss=species[k]=="beast-8";
                Surface(x,y,146,176,Color.white,catsFrame:boss?"Gold":"Tile");
                Art(x+10,y+8,126,110,species[k]);
                Text(x+4,y+122,138,boss?"TRÙM":db.Monster(species[k]).name,small,48,align:TextAnchor.UpperCenter);
            }

            // Kỹ năng mang theo, lồng, và nút xuất kích.
            int moonCages=Mathf.Min(5,profile.Get("moonCage")),beastCages=Mathf.Min(5-moonCages,profile.Get("beastCage"));
            Surface(150,628,640,120,Color.white,catsFrame:"Ink");
            Text(168,636,300,"KỸ NĂNG",small,26);
            for(int k=0;k<4;k++)
            {
                string id=profile.skillSlots[k];
                Surface(168+k*84,662,74,74,Color.white,catsFrame:"Tile");
                if(!string.IsNullOrEmpty(id))Art(174+k*84,668,62,62,"skill-"+id);
            }
            Art(530,640,40,40,"item-beastCage");Text(578,646,200,$"{beastCages} lồng thú",label,32);
            Art(530,690,40,40,"item-moonCage");Text(578,696,200,$"{moonCages} lồng trăng",label,32);
            if(IllustratedButton(810,628,640,120,"XUẤT KÍCH","Battle",primary:true))StartBattle();
            Enlarge(36);
            Text(150,760,1300,$"Bắt quái: khóa mục tiêu, còn {Battle.CaptureHealthThreshold(false):0%} HP (trùm {Battle.CaptureHealthThreshold(true):0%}) thì nó kiệt sức vài giây · ném lồng [C] / Lồng trăng [Shift+C]",small,28,align:TextAnchor.UpperCenter);
        }
        // Vẽ xe pháo đài trong UI theo đúng kích thước khi ghép trong trận (BattleView.Fortress, đơn vị thế giới),
        // thu nhỏ theo scale; (cx, groundY) là tâm ngang và mặt đất. Mỗi tầng có pháo và mèo vận hành.
        void FortressPortrait(float cx,float groundY,float scale)
        {
            int floors=profile.FloorCount;
            void Piece(string key,float worldY,float w,float h)=>Art(cx-w*scale/2,groundY-(worldY+h/2)*scale,w*scale,h*scale,key);
            for(int i=0;i<floors;i++)Piece("fortress-floor",94+i*80,272,101);
            Piece("fortress-chassis",35,320,73);
            Piece("fortress-roof",floors*80+88,274,82);
            for(int i=0;i<floors;i++)
            {
                var f=profile.floors[i];float centerY=groundY-(94+i*80)*scale;
                if(!string.IsNullOrEmpty(f.cat))Art(cx-(63+40)*scale,centerY-46*scale,80*scale,80*scale,f.cat);
                Art(cx+(20-75)*scale,centerY-50*scale,150*scale,94*scale,db.Weapon(f.turret).asset);
                if(HotSpot(cx-136*scale,centerY-40*scale,272*scale,80*scale))EditFormation("op-"+i);
            }
        }
        // Vùng chạm trong suốt đặt lên trên hình.
        bool HotSpot(float x,float y,float w,float h)
        {
            bool clicked=Button(x,y,w,h,"");
            if(!dispatchOnly&&lastButton!=null){lastButton.image.color=Color.clear;lastButton.text.text="";}
            return clicked;
        }
        void EditFormation(string slot)
        {
            teamSlot=slot;modal="";page="barracks";tab="squad";backToPrebattle=true;
        }

        // ================= MVP sau trận =================
        void ResultMvp(float x,float y)
        {
            var rows=battle.units.Where(u=>u.weapon!=null&&battle.damageBy.ContainsKey(u.weapon))
                .Select(u=>(unit:u,damage:battle.damageBy[u.weapon])).OrderByDescending(r=>r.damage).Take(3).ToList();
            Surface(x,y,310,170,Color.white,catsFrame:"Ink");
            Text(x+16,y+8,280,"MVP · SÁT THƯƠNG",subtitle,34);
            if(rows.Count==0){Text(x+16,y+60,280,"Chưa gây sát thương.",small,30);return;}
            float max=Mathf.Max(1,rows[0].damage);
            for(int k=0;k<rows.Count;k++)
            {
                var u=rows[k].unit;float ry=y+46+k*40;
                Art(x+12,ry,36,36,UnitIcon(u));
                Text(x+56,ry-2,150,UnitName(u),small,26);
                Text(x+200,ry-2,96,N(rows[k].damage),small,26,align:TextAnchor.UpperRight);
                Bar(x+56,ry+22,240,10,rows[k].damage/max,k==0?gold:cyan);
            }
        }
        string UnitIcon(Actor u)=>u.role=="tower"||u.role=="aux"?u.weapon.id:string.IsNullOrEmpty(u.asset)?"cat-0":u.asset;
        string UnitName(Actor u)
        {
            if(u.role=="companion")return db.Monster(u.asset).name;
            if(u.role=="infantry")return u.index<profile.squad.Length&&!string.IsNullOrEmpty(profile.squad[u.index])?db.Cat(u.asset).name:"Dân quân";
            return u.weapon.name;
        }
    }
}
