using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    // Thành mèo, Đấu trường và Nhiệm vụ theo bố cục quen thuộc của game idle (chỉ cách sắp xếp, art vẫn là của CatTanks):
    // Thành mèo là thị trấn với các công trình lớn → Gọi đồng đội / Tiếp tế mở riêng, nút ‹ về thị trấn;
    // Đấu trường là danh sách đối thủ có nút THÁCH ĐẤU từng dòng; Nhiệm vụ có thanh rương ngày ở trên và nút ĐI / NHẬN từng dòng.
    public sealed partial class CatTanksGame
    {
        const string TownHubView="town-hub",TavernView="town-tavern",StoreView="town-store",PvpView="town-pvp",QuestView="quests-board";
        static readonly string[] TownTabs={"hub","summon","shop","arena"};
        string shopFilter="";

        string TownViewKey()=>tab switch{"summon"=>TavernView,"shop"=>StoreView,"arena"=>PvpView,_=>TownHubView};
        string TownTitle=>tab switch{"summon"=>SummonTabLabel,"shop"=>"Tiếp tế","arena"=>"Đấu trường",_=>"Thành mèo"};
        bool TownUnlocked(int stage)=>profile.highestCleared>=stage;

        const float TownArtworkX = 1600f / 1672f;
        const float TownArtworkY = 900f / 941f;
        static readonly Rect[] TownCardBounds =
        {
            new Rect(108, 105, 481, 675),
            new Rect(596, 105, 478, 675),
            new Rect(1081, 105, 482, 675)
        };
        static readonly Rect[] TownNavBounds =
        {
            new Rect(27, 800, 273, 127),
            new Rect(338, 825, 299, 103),
            new Rect(677, 825, 299, 103),
            new Rect(1012, 825, 298, 103),
            new Rect(1355, 825, 295, 103)
        };

        // ---- Thị trấn: ba công trình lớn ----
        void TownHubUI()
        {
            const float artTop = 96 * TownArtworkY;
            Art(0, artTop, 1600, 900 - artTop, "Interface/Cats/TownHubArtwork");
            string[] tabs={"summon","shop","arena"};
            string[] names={SummonTabLabel,"Tiếp tế","Đấu trường"};
            string[] status=
            {
                TownUnlocked(4)?$"{profile.Get("summonScroll")} vé · còn {10-profile.pityElite} lượt đến <color=#ffe45b>Elite</color>":"Mở sau ải 4",
                TownUnlocked(3)?"Hàng làm mới mỗi ngày":"Mở sau ải 3",
                TownUnlocked(5)?$"{profile.rating} điểm · {profile.Get("arenaToken")} huy hiệu":"Mở sau ải 5"
            };
            for (int i = 0; i < TownCardBounds.Length; i++)
            {
                var card = TownCardBounds[i];
                if (TownArtworkButton(card, names[i]))
                {
                    tab = tabs[i];
                    scroll = Vector2.zero;
                }
                Text((card.x + 35) * TownArtworkX, 132 * TownArtworkY, (card.width - 70) * TownArtworkX,
                    status[i], subtitle, 55 * TownArtworkY, richText: true, align: TextAnchor.MiddleCenter, size: 25);
                Text((card.center.x - 129) * TownArtworkX, 688 * TownArtworkY, 258 * TownArtworkX,
                    names[i], subtitle, 61 * TownArtworkY, align: TextAnchor.MiddleCenter, size: 32);
            }
            BottomNav(townArtwork: true);
        }

        bool TownArtworkButton(Rect bounds, string name)
        {
            bool clicked = Button(bounds.x * TownArtworkX, bounds.y * TownArtworkY,
                bounds.width * TownArtworkX, bounds.height * TownArtworkY, name);
            if (dispatchOnly) return clicked;

            StudioNineSlice.Apply(lastButton.image, null);
            lastButton.image.color = Color.white;
            lastButton.text.color = Color.clear;
            var colors = lastButton.button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(.61f, .83f, 1f, .13f);
            colors.pressedColor = new Color(.04f, .09f, .25f, .25f);
            colors.selectedColor = new Color(.61f, .83f, 1f, .22f);
            colors.disabledColor = Color.clear;
            colors.fadeDuration = 0;
            lastButton.button.colors = colors;
            return clicked;
        }

        // ---- Gọi đồng đội: linh vật lớn ở giữa, bảo hiểm Elite, hai nút gọi ở đáy ----
        void TavernUI()
        {
            bool unlocked=TownUnlocked(4);
            Text(130,108,1100,"Một tín hiệu. Một đồng đội mới.",title,55);
            Text(130,166,1340,unlocked?"Bảo đảm mèo Elite sau 10 lượt. Bản sao giúp đồng đội thăng bậc.":"MỞ KHÓA SAU ẢI 4 • Nâng cấp ba mèo đầu tiên trong Doanh trại để tiến xa hơn.",label,36);
            Surface(230,214,1140,430,Color.white,catsFrame:"Roster");
            Art(290,300,300,300,"cat-2");
            Art(600,236,400,390,"cat-0");
            Art(1010,300,300,300,"cat-4");
            Surface(400,656,800,84,Color.white,catsFrame:"Ink");
            Text(420,664,760,$"{profile.Get("summonScroll")} VÉ ĐANG CÓ   •   Còn {10-profile.pityElite} lượt đến mèo Elite",subtitle,36,align:TextAnchor.UpperCenter);
            Bar(440,708,720,18,profile.pityElite/10f,gold);
            if(IllustratedButton(230,762,540,90,"GỌI 1 LẦN  •  1 VÉ","item-summonScroll",unlocked&&profile.Get("summonScroll")>=1,normalSpriteKey:"blue")){Notify(profile.Summon(db,1));Changed(toast);}
            Enlarge(26);
            if(IllustratedButton(830,762,540,90,"GỌI 10 LẦN  •  10 VÉ","item-summonScroll",unlocked&&profile.Get("summonScroll")>=10,true)){Notify(profile.Summon(db,10));Changed(toast);}
            Enlarge(26);
        }

        // ---- Tiếp tế: danh mục bên trái, lưới hàng bên phải ----
        static readonly (string name,string currency,int price,string item,int amount,int limit)[] ShopStock=
        {
            ("Lồng thú","coins",1500,"beastCage",1,5),("Lồng thú","gem",80,"beastCage",1,5),("Lồng trăng","gem",250,"moonCage",1,2),
            ("10 thức ăn","coins",200,"monsterChow",10,5),("Vé triệu hồi","gem",100,"summonScroll",1,10),("100 bụi","coins",500,"spiritDust",100,3),
            ("150 bụi","arenaToken",8,"spiritDust",150,5),("20 tinh chất","arenaToken",8,"soulEssence",20,3),("5 đá rèn","arenaToken",10,"forgeStone",5,3),
            ("Vé triệu hồi","arenaToken",16,"summonScroll",1,2),("15 thức ăn","arenaToken",6,"monsterChow",15,5),("Lõi kỹ năng","arenaToken",40,"skillCore",1,1)
        };
        void StoreUI()
        {
            profile.RefreshDay();
            Text(130,108,700,"Trạm tiếp tế",title,55);
            Text(700,120,780,$"Làm mới mỗi ngày  •  {profile.Get("arenaToken")} huy hiệu  •  {N(profile.Get("spiritDust"))} bụi",small,30,align:TextAnchor.UpperRight);
            string[] filters={"","coins","gem","arenaToken"},names={"Tất cả","Xu","Ngọc","Huy hiệu"};
            string[] icons={"Chest_Gold","Interface/Cats/CoinIcon","Interface/Cats/GemIcon","item-arenaToken"};
            Surface(118,210,222,660,Color.white,catsFrame:"Ink");
            for(int i=0;i<filters.Length;i++)
                if(TabButton(134,226+i*88,190,74,names[i],shopFilter==filters[i],icons[i])){shopFilter=filters[i];}

            Surface(356,210,1126,660,Color.white,catsFrame:"Roster");
            int shown=0;
            for(int i=0;i<ShopStock.Length;i++)
            {
                var v=ShopStock[i];if(shopFilter!=""&&v.currency!=shopFilter)continue;
                string key="shop-"+i;int bought=profile.bought.TryGetValue(key,out int n)?n:0;
                float x=372+shown%4*276,y=226+shown/4*212;shown++;
                Surface(x,y,264,200,Color.white,catsFrame:"Tile");
                Art(x+14,y+14,76,76,"item-"+v.item);
                Text(x+100,y+14,152,v.name,subtitle,52,size:24);
                Text(x+100,y+72,152,$"Còn {v.limit-bought}/{v.limit}",small,26);
                bool open=TownUnlocked(3);
                bool canBuy=open&&bought<v.limit&&profile.Get(v.currency)>=v.price;
                string caption=!open?"MỞ SAU ẢI 3":bought>=v.limit?"HẾT HÔM NAY":Cost((v.currency,v.price));
                if(Button(x+14,y+132,236,54,caption,canBuy,true))
                    if(Pay((v.currency,v.price))){profile.Add(v.item,v.amount);profile.bought[key]=bought+1;Changed("Đã nhận "+v.name);}
            }
        }

        // ---- Đấu trường: hồ sơ bên trái, danh sách đối thủ bên phải ----
        void PvpUI()
        {
            bool unlocked=TownUnlocked(5);
            Surface(118,112,440,660,Color.white,catsFrame:"Detail");
            Art(198,136,280,240,"cat-0");
            Text(138,384,400,profile.name,title,56,align:TextAnchor.UpperCenter,size:36);
            Text(138,444,400,$"{N(profile.rating)} ĐIỂM",subtitle,40,align:TextAnchor.UpperCenter,size:30);
            Surface(150,500,376,64,Color.white,catsFrame:"Ink");
            Art(166,508,48,48,"item-arenaToken");
            Text(224,516,290,$"{profile.Get("arenaToken")} huy hiệu",subtitle,36);
            Text(150,582,376,unlocked?"Đối thủ mô phỏng dùng đội quái theo tiến độ, có AI kỹ năng. Điểm tính theo rating đối thủ. Tối đa 40 huy hiệu mỗi ngày.":"VƯỢT ẢI 5 ĐỂ MỞ KHÓA ĐẤU TRƯỜNG",small,170);

            Text(586,112,700,"Chọn đối thủ",title,55);
            for(int i=0;i<Opponent.All.Length;i++)
            {
                var o=Opponent.All[i];float y=176+i*100;
                Surface(578,y,904,92,Color.white,catsFrame:selectedOpponent==i?"Roster":"Tile");
                Art(594,y+8,76,76,"beast-"+i);
                Text(684,y+10,330,$"{o.name} · Cấp {o.level}",subtitle,36);
                Text(684,y+50,330,$"{N(o.rating)} điểm",small,30);
                Text(1010,y+16,230,$"Thắng +{Battle.RatingDelta(profile.rating,o.rating,true)}\nThua {Battle.RatingDelta(profile.rating,o.rating,false)}",small,60);
                if(Button(1260,y+16,206,60,"THÁCH ĐẤU",unlocked,true)){selectedOpponent=i;StartBattle(false,true);}
                Enlarge(22);
            }
            BottomNav();
        }

        // ---- Nhiệm vụ: rương ngày ở trên, mỗi dòng có tiến độ, phần thưởng và nút ĐI / NHẬN ----
        void QuestBoardUI()
        {
            profile.RefreshDay();
            string[] ids={"idle","wins","upgrade","capture","pvp"};
            string[] names={"Thu nhận chiến lợi phẩm AFK","Thắng 3 trận hành quân","Nâng cấp đồng đội hoặc pháo","Mở lồng hoặc bắt một quái","Tham gia một trận đấu trường"};
            string[] icons={"Chest_Gold","Battle","item-catXp","item-beastCage","Badge_Crown"};
            bool complete=ids.All(profile.claimed.Contains),bonus=profile.claimed.Contains("bonus");
            int done=ids.Count(profile.claimed.Contains);

            Surface(118,108,1364,128,Color.white,catsFrame:"Ink");
            Art(136,116,112,112,"item-afkChest");
            Text(266,118,500,"RƯƠNG NGÀY",title,48,size:34);
            Text(266,164,620,"800 xu · 20 ngọc · 200 bụi · 1 vé · 10 thức ăn",small,26);
            Bar(266,196,640,24,done/5f,gold);
            Text(920,190,240,$"{done} / 5 NHIỆM VỤ",subtitle,36);
            if(Button(1200,140,262,70,bonus?"ĐÃ NHẬN":complete?"MỞ RƯƠNG":"CHƯA ĐỦ",complete&&!bonus,complete&&!bonus))
            {profile.claimed.Add("bonus");profile.Grant(new Dictionary<string,int>{{"coins",800},{"spiritDust",200},{"gem",20},{"summonScroll",1},{"monsterChow",10},{"accountXp",150}});Changed("Đã nhận rương ngày");}
            Enlarge(24);

            for(int i=0;i<5;i++)
            {
                float y=250+i*104;int need=i==1?3:1,n=profile.daily.TryGetValue(ids[i],out int v)?v:0;
                bool claimed=profile.claimed.Contains(ids[i]),ready=n>=need&&!claimed;
                Surface(118,y,1364,96,Color.white,catsFrame:ready?"Roster":"Detail");
                Art(138,y+13,70,70,icons[i]);
                Text(226,y+10,640,names[i],subtitle,36);
                Bar(226,y+56,420,20,Mathf.Min(n,need)/(float)need,cyan);
                Text(660,y+52,100,$"{Mathf.Min(n,need)}/{need}",small,28);
                Art(800,y+24,46,46,"item-coins");Text(852,y+32,70,"40",subtitle,36);
                Art(930,y+24,46,46,"item-catXp");Text(982,y+32,70,"10",subtitle,36);
                if(Button(1220,y+18,242,60,claimed?"ĐÃ NHẬN":ready?"NHẬN":"ĐI",!claimed,ready,ready?"button":"blue"))
                {
                    if(ready){profile.claimed.Add(ids[i]);profile.Add("coins",40);profile.Add("catXp",10);Changed("Đã nhận thưởng nhiệm vụ");}
                    else GoToQuest(ids[i]);
                }
                Enlarge(24);
            }
            BottomNav();
        }
        // Nút ĐI đưa tới nơi làm nhiệm vụ.
        void GoToQuest(string id)
        {
            scroll=Vector2.zero;
            switch(id)
            {
                case "upgrade":page="barracks";tab="cats";break;
                case "capture":page="barracks";tab="monsters";break;
                case "pvp":page="town";tab="arena";break;
                default:page="march";tab="cats";break;
            }
        }
    }
}
