using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        // Màn chính kiểu game: cảnh ải và pháo đài làm nền, nút lớn hai bên, chọn ải ở góc dưới.
        // Các trang con mở toàn màn hình; phần chrome (Back, tiêu đề, tiền tệ) vẽ ở view "hub-chrome" phía trên.
        const string HomeView="home",ChromeView="hub-chrome",MapView="map";
        const string TownCatChromeView = "town-cat-chrome";
        // Khung nội dung cũ (254,125)-(1576,865) được dời/thu phóng vào vùng trống sau khi bỏ sidebar và header.
        const float SubPageScale=1.06f,SubPageLeft=100,SubPageTop=98;

        void HubUI()
        {
            if(page=="march"){HomeUI();return;}
            Backdrop(new Color(.17f,.13f,.42f,.97f));
            if(IsRuntimeView(renderViewKey))
            {
                // Các màn theo bố cục game idle: view tạo lúc chạy, toạ độ 1600×900 không thu phóng.
                useCodeGeometry=true;
                if (renderViewKey != TownHubView && renderViewKey != CatView)
                    Surface(100,84,1400,830,Color.white,catsFrame:"Board");
                switch(renderViewKey)
                {
                    case CatView:CatDetailUI();break;
                    case TownHubView:TownHubUI();break;
                    case TavernView:TavernUI();break;
                    case StoreView:StoreUI();break;
                    case PvpView:PvpUI();break;
                    case QuestView:QuestBoardUI();break;
                    case FortView:FortWorkshopUI();break;
                    case TeamView:TeamUI();break;
                    case SavesView:SavesUI();break;
                    case SkillView:SkillDeckUI();break;
                    case PetView:PetStorageUI();break;
                    case CrystalView:CrystalUI();break;
                    default:RosterUI();break;
                }
                SubPageChrome();
                return;
            }
            Surface(254,125,1322,740,C("#17243e"),"BluePanel",UsesCatsArt?"Board":"CommandBoard");
            Art(264,108,1324,780,UsesCatsArt?"Interface/Cats/BoardAccent":"Interface/Cats/CommandAccent");
            SubPageChrome();
        }
        void HomeUI()
        {
            var stage=db.stages[Mathf.Clamp(profile.selected,1,db.stages.Count)-1];
            int lastOpen=Mathf.Min(db.stages.Count,profile.highestCleared+1);
            float power=profile.Power(db);

            // Thanh trên: chỉ huy (cấp + thanh kinh nghiệm), ví tiền, cài đặt.
            Art(0,0,1600,92,"Interface/Cats/Header");
            Surface(14,10,330,84,Color.white,catsFrame:"Ink");
            Art(24,16,70,70,"cat-0");
            Text(102,14,232,profile.name,subtitle,32);
            Text(102,48,70,"CẤP "+profile.accountLevel,small,26);
            Bar(172,54,154,14,profile.accountXp/(float)(15+profile.accountLevel*4),cyan);
            HomeWallet(820,"CoinIcon",N(profile.Get("coins")));
            HomeWallet(1035,"GemIcon",N(profile.Get("gem")));
            HomeWallet(1250,"TicketIcon",N(profile.Get("summonScroll")));
            if(IllustratedButton(1468,14,114,68,"Cài đặt","Interface/Cats/SettingsIcon",normalSpriteKey:"blue",content:StudioButtonLayout.Content.Symbol))modal="settings";

            // Lối tắt phụ bên trái.
            if(HomeShortcut(116,"Bản đồ","Map"))modal=MapView;
            if(HomeShortcut(230,"Bách khoa","Book_1_Purple"))ToggleBook();
            if(HomeShortcut(344,"Thử thách","Badge_Crown"))StartBattle(true);

            // Rương AFK ngay trên màn chính: thời gian treo, thanh 12 giờ, phần thưởng đang tích.
            float elapsed=Mathf.Clamp(Profile.Now-profile.lastIdle,0,43200);var idle=profile.Idle(Profile.Now);
            Surface(14,566,350,206,Color.white,catsFrame:"Ink");
            // Chạm rương để mở bảng chiến lợi phẩm chi tiết.
            if(IllustratedButton(26,580,124,124,"Rương AFK","item-afkChest",content:StudioButtonLayout.Content.Symbol))modal=ChestModal;
            if(!dispatchOnly&&lastButton.image)lastButton.image.color=Color.clear;
            Text(160,578,196,"RƯƠNG AFK",subtitle,32);
            Text(160,608,196,$"{Mathf.FloorToInt(elapsed/3600):00}:{Mathf.FloorToInt(elapsed%3600/60):00}:{Mathf.FloorToInt(elapsed%60):00}",title,48,size:34);
            Bar(160,660,188,14,elapsed/43200,gold);
            var gains=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,int>>();
            foreach(var r in idle)if(r.Value>0&&gains.Count<3)gains.Add(r);
            for(int i=0;i<gains.Count;i++)
            {
                string icon=RewardIcon(gains[i].Key);
                if(icon!=null)Art(26+i*76,714,34,34,icon);
                Text(62+i*76,718,44,"+"+CompactNumber(gains[i].Value),small,26);
            }
            if(Button(262,706,92,54,"NHẬN",elapsed>=60,true))ClaimIdle();
            Enlarge(20);

            // Ải hiện tại và nút xuất kích ở góc phải dưới.
            Surface(1176,548,410,226,Color.white,catsFrame:"Board");
            Text(1196,560,370,$"ẢI {stage.index:00} / {db.stages.Count}   ·   "+(stage.boss?"TRẬN TRÙM":"HÀNH QUÂN"),small,26,align:TextAnchor.UpperCenter);
            if(Button(1196,592,48,58,"‹",profile.selected>1,true))SelectStage(profile.selected-1);
            Enlarge(36);
            Text(1250,592,262,stage.name,title,58,align:TextAnchor.MiddleCenter,size:30);
            if(Button(1518,592,48,58,"›",profile.selected<lastOpen,true))SelectStage(profile.selected+1);
            Enlarge(36);
            Text(1196,648,370,$"Lực chiến {N(power)} / {N(stage.recommendedPower)}   ·   "+(power>=stage.recommendedPower?"Sẵn sàng":"Nên nâng cấp"),small,26,align:TextAnchor.UpperCenter);
            if(IllustratedButton(1196,684,370,78,"XUẤT KÍCH","Battle",primary:true))modal="prebattle";
            Enlarge(30);

            BottomNav();
        }
        // Thanh điều hướng cố định ở đáy theo bố cục quen thuộc của game idle; Hành quân nằm giữa.
        void BottomNav(bool townArtwork = false)
        {
            if (!townArtwork)
            {
                SpriteBox(0,786,1600,114,"",new Color(.12f,.1f,.33f,1),false,"scrim");
                SpriteBox(0,782,1600,6,"",C("#140c30"),false,"scrim");
            }
            if(NavTab(0,"Thành mèo","Interface/Cats/Tab-fortress",page=="town"&&tab!="arena",townArtwork)){page="town";tab="hub";scroll=Vector2.zero;}
            if(NavTab(1,"Đấu trường","item-arenaToken",page=="town"&&tab=="arena",townArtwork)){page="town";tab="arena";scroll=Vector2.zero;}
            if(NavTab(2,"Hành quân","Map",page=="march",townArtwork)){page="march";modal="";}
            if(NavTab(3,"Doanh trại","Interface/Cats/Tab-cats",page=="barracks",townArtwork)){page="barracks";tab="cats";scroll=Vector2.zero;}
            if(NavTab(4,"Nhiệm vụ","Calendar_Check",page=="quests",townArtwork)){page="quests";scroll=Vector2.zero;}
        }
        // Tab đang mở: ô vàng lớn nhô lên khỏi thanh; các tab khác là nút tím.
        bool NavTab(int index,string value,string icon,bool active,bool townArtwork)
        {
            if (townArtwork) return TownArtworkButton(TownNavBounds[index], value);
            if(active)
            {
                bool open=IllustratedButton(index*320+30,762,260,136,value,icon,primary:true,content:StudioButtonLayout.Content.Tile);
                Enlarge(24);
                return open;
            }
            bool clicked=IllustratedButton(index*320+24,796,272,98,value,icon);
            Enlarge(22);
            if(!dispatchOnly&&lastButton.layout)lastButton.layout.iconSize=72;
            return clicked;
        }
        bool HomeShortcut(float y,string value,string icon)
        {
            bool clicked=IllustratedButton(14,y,104,104,value,icon,content:StudioButtonLayout.Content.Tile);
            if(!dispatchOnly)SetButtonSprite(lastButton,"button");
            Enlarge(16);
            return clicked;
        }
        void HomeWallet(float x,string icon,string value)
        {
            Surface(x,16,200,64,Color.white,catsFrame:"Ink");
            Art(x+10,26,44,44,"Interface/Cats/"+icon);
            Text(x+62,26,130,value,subtitle,40);
        }
        static string CompactNumber(int n)=>n>=1000000?(n/1000000f).ToString("0.#")+"M":n>=10000?(n/1000)+"K":n.ToString("N0");
        // Nút lớn ở màn chính cần chữ to hơn mức mặc định của nút.
        void Enlarge(int size)
        {
            if(dispatchOnly||lastButton==null)return;
            lastButton.text.fontSize=size;if(lastButton.layout)lastButton.layout.fontSize=size;
        }
        void SubPageChrome()
        {
            bool upgradedChrome = renderViewKey == TownHubView || renderViewKey == CatView;
            SelectView(upgradedChrome ? TownCatChromeView : ChromeView, "screen");
            // View tạo lúc chạy sau chrome sẽ nằm đè lên; luôn đưa chrome lên trên cùng.
            if(!dispatchOnly&&uiParent.GetSiblingIndex()!=uiParent.parent.childCount-1)uiParent.SetAsLastSibling();
            if (upgradedChrome)
            {
                useCodeGeometry = true;
                drawingHubChrome = true;
                UpgradedTownCatChrome();
                return;
            }
            Art(0,0,1600,100,"Interface/Cats/Header");
            // Trong Doanh trại, ‹ quay về danh sách mèo trước rồi mới về màn chính.
            // Trong Thành mèo, Gọi đồng đội / Tiếp tế quay về thị trấn.
            if(Button(18,14,76,70,"‹",true,true))StepBack();
            Text(112,16,660,PageTitle,title,60);
            HubWallet();
        }
        void UpgradedTownCatChrome()
        {
            Art(0, 0, 1600, 96 * TownArtworkY, "Interface/Cats/TownCatHeader");
            if (TownArtworkButton(new Rect(10, 10, 92, 71), "‹")) StepBack();
            Text(112, 5, 500, PageTitle, title, 73, align: TextAnchor.MiddleLeft, size: 48);
            Text(857 * TownArtworkX, 14, 124 * TownArtworkX, N(profile.Get("coins")), subtitle, 35, align: TextAnchor.MiddleLeft, size: 26);
            Text(1094 * TownArtworkX, 14, 123 * TownArtworkX, N(profile.Get("gem")), subtitle, 35, align: TextAnchor.MiddleLeft, size: 26);
            Text(1327 * TownArtworkX, 14, 114 * TownArtworkX, N(profile.Get("summonScroll")), subtitle, 35, align: TextAnchor.MiddleLeft, size: 26);
            if (TownArtworkButton(new Rect(1480, 9, 181, 73), "Cài đặt")) modal = "settings";
        }
        void StepBack()
        {
            scroll=Vector2.zero;
            // Vào Đội hình từ popup Chuẩn bị xuất kích thì ‹ quay lại popup đó.
            if(backToPrebattle&&page=="barracks"){backToPrebattle=false;page="march";tab="cats";modal="prebattle";return;}
            backToPrebattle=false;
            if(page=="barracks"&&tab!="cats")tab="cats";
            else if(page=="town"&&tab is "summon" or "shop")tab="hub";
            else{page="march";tab="cats";}
        }
        string PageTitle=>page switch{"barracks"=>BarracksTitle,"town"=>TownTitle,"chest"=>"Rương AFK","quests"=>"Nhiệm vụ",_=>""};
        void HubWallet()
        {
            Resource(800,"CoinIcon",N(profile.Get("coins")),"XU");
            Resource(1010,"GemIcon",N(profile.Get("gem")),"NGỌC");
            Resource(1220,"TicketIcon",N(profile.Get("summonScroll")),"VÉ GỌI MÈO");
            if(IllustratedButton(1432,22,150,62,"Cài đặt","Interface/Cats/SettingsIcon",normalSpriteKey:"blue"))modal="settings";
        }
        void Resource(float x,string icon,string value,string caption)
        {
            Surface(x,22,195,76,Color.white,catsFrame:"Ink"); Art(x+10,37,44,44,"Interface/Cats/"+icon);
            Text(x+64,30,125,value,subtitle); Text(x+64,63,125,caption,small);
        }
        void SelectStage(int index)
        {
            profile.selected=Mathf.Clamp(index,1,db.stages.Count);profile.Save();RebuildHubScene();
        }
        // ---- Khung popup kiểu game: bảng tím, băng tiêu đề giữa mép trên, nút X ở góc, chạm nền mờ để đóng ----
        static readonly Color ScrimColor=new Color(.025f,.035f,.075f,.82f);
        bool ScrimButton()
        {
            bool clicked=Button(-500,-500,2600,1900,"");
            if(dispatchOnly)return clicked;
            var item=lastButton;
            item.image.sprite=null;item.image.type=UnityEngine.UI.Image.Type.Simple;item.image.color=ScrimColor;item.text.text="";
            item.button.transition=UnityEngine.UI.Selectable.Transition.None;
            focusable.Remove(item.button);if(item.layout)activeLayouts.Remove(item.layout);
            return clicked;
        }
        void PopupFrame(float x,float y,float w,float h,string heading,bool closable)
        {
            Panel(x,y,w,h);
            float bw=Mathf.Clamp(heading.Length*24+90,360,w-150);
            Surface(x+(w-bw)/2,y-34,bw,76,Color.white,catsFrame:"Violet");
            Text(x+(w-bw)/2,y-34,bw,heading,title,76,align:TextAnchor.MiddleCenter);
            if(closable&&Button(x+w-54,y-26,66,66,"X",normalSpriteKey:"blue"))modal="";
            if(closable)Enlarge(30);
        }
        // Một dòng cài đặt: khung tối, icon, nhãn; phần điều khiển đặt ở bên phải (x≥920).
        void SettingRow(float y,string name,string icon)
        {
            Surface(470,y,660,74,Color.white,catsFrame:"Ink");
            Art(488,y+13,48,48,icon);
            Text(550,y+18,330,name,subtitle,38);
        }
        bool TogglePill(float y,bool on)
        {
            bool clicked=Button(930,y+10,184,54,on?"BẬT":"TẮT",true,on,on?"button":"blue");
            Enlarge(22);
            return clicked;
        }
        static string RewardIcon(string key)=>key switch
        {
            "accountXp"=>"Badge_Crown",
            "coins" or "gem" or "catXp" or "spiritDust" or "forgeStone" or "soulEssence" or "summonScroll" or "skillCore" or "beastCage" or "moonCage" or "monsterChow" or "arenaToken" or "relicStone" or "artifact"=>"item-"+key,
            _=>null
        };
        static string StagePreview(Stage stage) => $"StagePreviews/stage-{stage.index:00}";
        static bool IsSubPage(string key)=>!IsRuntimeView(key)&&(key.StartsWith("barracks-",System.StringComparison.Ordinal)||key.StartsWith("town-",System.StringComparison.Ordinal)||key is "chest" or "quests");
        static bool IsRuntimeView(string key)=>key is HomeView or ChromeView or TownCatChromeView or MapView or RosterView or CatView or TownHubView or TavernView or StoreView or PvpView or QuestView
            or FortView or TeamView or SavesView or SkillView or PetView or CrystalView or ChestModal or TitleView;
        // Trang con được dời/thu phóng vào vùng trống; các view khác giữ nguyên khung 1600×900.
        static void FrameView(RectTransform rt,string key)
        {
            bool sub=IsSubPage(key);
            var pos=sub?new Vector2(SubPageLeft-SubPageScale*254,-(SubPageTop-SubPageScale*125)):Vector2.zero;
            var scale=Vector3.one*(sub?SubPageScale:1);var corner=new Vector2(0,1);
            if(rt.anchorMin!=corner||rt.anchorMax!=corner||rt.pivot!=corner){rt.anchorMin=rt.anchorMax=rt.pivot=corner;rt.sizeDelta=new Vector2(1600,900);}
            if(rt.anchoredPosition!=pos)rt.anchoredPosition=pos;
            if(rt.localScale!=scale)rt.localScale=scale;
        }
        bool TabButton(float x,float y,float w,float h,string value,bool selected,string artKey=null)
        {
            bool clicked=artKey==null ? Button(x,y,w,h,value) : IllustratedButton(x,y,w,h,value,artKey);
            if(dispatchOnly)return clicked;
            var item=lastButton;
            if(selected)
            {
                bool useCyan=renderViewKey=="book"||(UsesManagementArt&&!drawingHubChrome);
                SetButtonSprite(item,useCyan?"cats-tab-selected":"selected");
                if(drawingHubChrome)
                {
                    item.text.color=C("#241333");
                    MarchLettering(item.text,0);
                }
            }
            return clicked;
        }
    }
}
