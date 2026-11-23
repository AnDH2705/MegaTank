using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        // Màn chính kiểu game: cảnh ải và pháo đài làm nền, nút lớn hai bên, chọn ải ở góc dưới.
        // Các trang con mở toàn màn hình; phần chrome (Back, tiêu đề, tiền tệ) vẽ ở view "hub-chrome" phía trên.
        const string HomeView="home",ChromeView="hub-chrome",MapView="map";
        // Khung nội dung cũ (254,125)-(1576,865) được dời/thu phóng vào vùng trống sau khi bỏ sidebar và header.
        const float SubPageScale=1.06f,SubPageLeft=100,SubPageTop=98;

        void HubUI()
        {
            if(page=="march"){HomeUI();return;}
            Backdrop(new Color(.17f,.13f,.42f,.97f));
            Surface(254,125,1322,740,C("#17243e"),"BluePanel",UsesCatsArt?"Board":"CommandBoard");
            Art(264,108,1324,780,UsesCatsArt?"Interface/Cats/BoardAccent":"Interface/Cats/CommandAccent");
            if(page=="barracks")Barracks(); else if(page=="town")Town();
            else if(page=="chest"||page=="quests")
            {
                useCodeGeometry=true;
                if(page=="chest")Chest();else Quests();
            }
            SubPageChrome();
        }
        void HomeUI()
        {
            var stage=db.stages[Mathf.Clamp(profile.selected,1,db.stages.Count)-1];
            int lastOpen=Mathf.Min(db.stages.Count,profile.highestCleared+1);
            Art(0,0,1600,100,"Interface/Cats/Header");
            Art(12,6,322,72,"Interface/Cats/Brand");
            HubWallet();

            Surface(18,108,320,86,Color.white,catsFrame:"Ink");
            Art(30,116,70,70,"cat-0");
            Text(108,116,222,profile.name,subtitle,34);
            Text(108,152,222,"CHỈ HUY  ·  CẤP "+profile.accountLevel,small,28);

            if(HomeTile(18,214,146,124,"Bản đồ","Map"))modal="map";
            if(HomeTile(18,350,146,124,"Nhiệm vụ","Calendar_Check")){page="quests";scroll=Vector2.zero;}
            if(HomeTile(18,486,146,124,"Bách khoa","Book_1_Purple"))ToggleBook();
            if(HomeTile(1438,120,146,134,"Doanh trại","Battle")){page="barracks";scroll=Vector2.zero;}
            if(HomeTile(1438,266,146,134,"Thành mèo","Interface/Cats/Tab-fortress")){page="town";scroll=Vector2.zero;}
            if(HomeTile(1438,412,146,134,"Rương AFK","Chest_Gold")){page="chest";scroll=Vector2.zero;}

            Surface(18,760,790,124,Color.white,catsFrame:"Board");
            if(Button(34,776,58,92,"‹",profile.selected>1,true))SelectStage(profile.selected-1);
            Enlarge(44);
            Art(104,774,150,96,StagePreview(stage));
            Text(268,770,450,$"ẢI {stage.index:00} / {db.stages.Count}   ·   "+(stage.boss?"TRẬN TRÙM":"HÀNH QUÂN"),small,26);
            Text(268,792,450,stage.name,title,44);
            bool cleared=stage.index<=profile.highestCleared;
            Text(268,844,450,(cleared?"Chơi lại: ":"Lần đầu: ")+Rewards(cleared?stage.repeatRewards:stage.firstClearRewards),small,28);
            if(Button(734,776,58,92,"›",profile.selected<lastOpen,true))SelectStage(profile.selected+1);
            Enlarge(44);

            float power=profile.Power(db);
            Surface(824,790,330,94,Color.white,catsFrame:"Ink");
            Art(840,812,40,48,"Interface/Cats/PowerIcon");
            Text(890,800,250,power>=stage.recommendedPower?"LỰC CHIẾN  ·  SẴN SÀNG":"LỰC CHIẾN  ·  NÊN NÂNG CẤP",small,26);
            Text(890,828,250,N(power)+" / "+N(stage.recommendedPower),subtitle,40);

            if(IllustratedButton(1300,730,284,154,"XUẤT KÍCH","Battle",primary:true,content:StudioButtonLayout.Content.Tile))modal="prebattle";
            Enlarge(30);
        }
        // Nút lớn ở màn chính cần chữ to hơn mức mặc định của nút.
        void Enlarge(int size)
        {
            if(dispatchOnly||lastButton==null)return;
            lastButton.text.fontSize=size;if(lastButton.layout)lastButton.layout.fontSize=size;
        }
        bool HomeTile(float x,float y,float w,float h,string value,string icon)
        {
            bool clicked=IllustratedButton(x,y,w,h,value,icon,content:StudioButtonLayout.Content.Tile);
            if(!dispatchOnly)SetButtonSprite(lastButton,"button");
            return clicked;
        }
        void SubPageChrome()
        {
            SelectView(ChromeView,"screen");
            Art(0,0,1600,100,"Interface/Cats/Header");
            if(Button(18,14,76,70,"‹",true,true)){page="march";scroll=Vector2.zero;}
            Text(112,16,660,PageTitle,title,60);
            HubWallet();
        }
        string PageTitle=>page switch{"barracks"=>"Doanh trại","town"=>"Thành mèo","chest"=>"Rương AFK","quests"=>"Nhiệm vụ",_=>""};
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
        void MapPopup()
        {
            PopupFrame(130,80,1340,760,"Bản đồ chiến dịch",true);
            Text(170,124,1260,$"{profile.highestCleared} / {db.stages.Count} ẢI ĐÃ QUA   ·   Mỗi 5 ải là một trận trùm   ·   Chọn ải để xem trên màn chính",small,28,align:TextAnchor.UpperCenter);
            for(int i=0;i<db.stages.Count&&i<20;i++)
            {
                var stage=db.stages[i];float x=170+i%5*256,y=196+i/5*156;
                bool unlocked=i<=profile.highestCleared;
                string badge=!unlocked?"Lock":i<profile.highestCleared?"Check":stage.boss?"Skull":null;
                if(IllustratedButton(x,y,240,140,$"{stage.index:00}",StagePreview(stage),unlocked,false,
                    StudioButtonLayout.Content.Stage,badge,profile.selected==i+1)){modal="";SelectStage(i+1);}
            }
        }
        static string StagePreview(Stage stage) => $"StagePreviews/stage-{stage.index:00}";
        static bool IsSubPage(string key)=>key.StartsWith("barracks-",System.StringComparison.Ordinal)||key.StartsWith("town-",System.StringComparison.Ordinal)||key is "chest" or "quests";
        static bool IsRuntimeView(string key)=>key is HomeView or ChromeView or MapView;
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
