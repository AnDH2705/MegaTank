using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void HubUI()
        {
            Surface(-500,-500,2600,1900,C("#0d152a"));
            if(UsesMarchArt)Art(0,0,1600,112,"Interface/March/ComicHeader");
            if(UsesManagementArt)Art(0,0,1600,117,"Interface/Cats/Header");
            Surface(254,125,1322,740,C("#17243e"),"BluePanel",UsesCatsArt?"Board":"CommandBoard");
            if(UsesMarchArt)
            {
                Art(264,108,1324,780,"Interface/March/ComicPanelGlints");
                Art(278,821,1296,55,"Interface/March/ComicFooter");
            }
            if(UsesManagementArt)
            {
                Art(264,108,1324,780,UsesCatsArt?"Interface/Cats/BoardAccent":"Interface/Cats/CommandAccent");
                Surface(13,114,235,165,Color.white,catsFrame:"Sidebar");
                Art(43,5,394,88,"Interface/Cats/Brand");
            }
            else
            {
                Surface(24,125,210,740,C("#1e2c49"),"VioletPanel");
                Icon(30,23,64,"Badge_Crown");
                Text(110,22,550,UsesMarchArt?"<color=#fff8f3>MEOW</color> BASTION":"MEOW BASTION",title,richText:UsesMarchArt);
                Text(112,68,550,"PHÁO ĐÀI TRĂNG XANH",small);
            }
            Resource(770,"item-coins",N(profile.Get("coins")),"XU");
            Resource(983,"item-gem",N(profile.Get("gem")),"NGỌC");
            Resource(1196,"item-summonScroll",N(profile.Get("summonScroll")),"VÉ GỌI MÈO");
            if (IllustratedButton(1432,27,140,58,"Cài đặt",UsesManagementArt?"Interface/Cats/SettingsIcon":"Setting",normalSpriteKey:UsesMarchArt||UsesManagementArt?"blue":"button")) modal="settings";
            Text(44,150,170,"CHỈ HUY  /  CẤP "+profile.accountLevel,small);
            Text(44,181,170,profile.name,subtitle,56);
            string[] nav={"march","barracks","town","chest","quests","book"};
            string[] names={"Hành quân","Doanh trại","Thành mèo","Rương AFK","Nhiệm vụ","Bách khoa"};
            string[] icons={"Map","Battle","Bag_Brown","Chest_Gold","Calendar_Check","Book_1_Purple"};
            for(int i=0;i<nav.Length;i++)
            {
                float y=258+i*76;
                if(TabButton(38,y,182,62,"      "+names[i],page==nav[i]))
                {
                    if(nav[i]=="book")modal="book";
                    else {page=nav[i];scroll=Vector2.zero;}
                }
                Icon(48,y+12,38,icons[i]);
            }
            Surface(38,748,182,96,C("#111d34"),"BluePanel","Sidebar");
            if(UsesManagementArt)
            {
                Text(61,776,161,"LỰC CHIẾN",subtitle);
                Art(37,802,41,49,"Interface/Cats/PowerIcon");
                Text(80,805,139,N(profile.Power(db)),subtitle,42);
            }
            else
            {
                Text(54,762,155,"LỰC CHIẾN",small); Text(54,791,155,N(profile.Power(db)),subtitle);
            }
            bool insetFooter=UsesMarchArt||UsesManagementArt;
            if(UsesManagementArt)Art(295,829,39,39,"Interface/Cats/InfoIcon");
            Text(UsesManagementArt?347:insetFooter?286:280,insetFooter?836:875,UsesManagementArt?1200:1250,"TIẾN TRÌNH TỰ LƯU    •    Esc: quay lại    •    Tab: chọn nút    •    Enter: xác nhận",small,insetFooter?25:35);
            if(page=="march")March(); else if(page=="barracks")Barracks(); else if(page=="town")Town();
            else if(page=="chest"||page=="quests")
            {
                useCodeGeometry=true;
                if(page=="chest")Chest();else Quests();
            }
        }
        void Resource(float x,string icon,string value,string caption)
        {
            if(UsesManagementArt)icon=icon switch
            {
                "item-coins"=>"Interface/Cats/CoinIcon",
                "item-gem"=>"Interface/Cats/GemIcon",
                "item-summonScroll"=>"Interface/Cats/TicketIcon",
                _=>icon
            };
            Surface(x,22,195,76,C("#24324e"),"InkPanel","Ink"); Art(x+10,37,44,44,icon);
            Text(x+64,30,125,value,subtitle); Text(x+64,63,125,caption,small);
        }
        void March()
        {
            Text(286,140,800,"Chuyến phiêu lưu dưới trăng",title);
            Text(286,192,800,"Chọn vùng đất. Sắp đội hình. Cùng mèo giữ vững pháo đài.",label);
            Surface(286,244,800,482,C("#111e36"),"VioletPanel");
            Text(310,266,520,"CHIẾN DỊCH  /  20 VÙNG ĐẤT",small);
            Text(866,266,196,profile.highestCleared+" / 20 ĐÃ QUA",small);
            for(int row=0;row<4;row++)
            {
                float y=304+row*104;
                Surface(350,y+41,670,6,C("#2d4565"));
                for(int col=0;col<5;col++)
                {
                    int i=row*5+col; var stage=db.stages[i]; float x=310+col*153;
                    bool unlocked=i<=profile.highestCleared, selected=profile.selected==i+1;
                    string badge = !unlocked ? "Lock" : i < profile.highestCleared ? "Check" : stage.boss ? "Skull" : null;
                    if(IllustratedButton(x,y,140,88,$"{stage.index:00}",StagePreview(stage),unlocked,selected,
                        StudioButtonLayout.Content.Stage,badge)) {profile.selected=i+1;profile.Save();}
                }
            }
            var selectedStage=db.stages[profile.selected-1];
            Surface(1106,244,450,578,C("#253455"),"VioletPanel");
            Text(1130,256,402,"ẢI "+selectedStage.index.ToString("00")+"  /  "+(selectedStage.boss?"TRẬN TRÙM":"HÀNH QUÂN"),small,25);
            Text(1130,284,402,selectedStage.name,title,76);
            Art(1130,366,402,164,StagePreview(selectedStage));
            Text(1130,538,402,"LỰC CHIẾN ĐỀ NGHỊ",small,25);
            Text(1130,563,402,N(selectedStage.recommendedPower),title);
            Text(1130,613,402,profile.Power(db)>=selectedStage.recommendedPower?"Đội của bạn đã sẵn sàng":"Gợi ý: nâng cấp mèo trước khi đi",small,25);
            bool cleared=selectedStage.index<=profile.highestCleared;
            Text(1130,648,402,cleared?"Thưởng khi chơi lại":"Thưởng lần đầu",small,25);
            Text(1130,677,402,Rewards(cleared?selectedStage.repeatRewards:selectedStage.firstClearRewards),small,51);
            if(IllustratedButton(1130,736,402,64,"CHUẨN BỊ XUẤT KÍCH  ›","Battle",primary:true))modal="prebattle";
            Surface(286,736,800,68,C("#20304b"),"VioletPanel"); Art(306,745,50,50,"item-afkChest");
            Text(376,740,686,"Mỗi 5 ải là một trận trùm",subtitle);
            Text(376,769,686,"Thu thập mèo, trang bị và mở rộng pháo đài trên hành trình.",small,25);
        }
        static string StagePreview(Stage stage) => $"StagePreviews/stage-{stage.index:00}";
        bool TabButton(float x,float y,float w,float h,string value,bool selected,string artKey=null)
        {
            bool clicked=artKey==null ? Button(x,y,w,h,value) : IllustratedButton(x,y,w,h,value,artKey);
            if(dispatchOnly)return clicked;
            var item=lastButton;
            if(selected)
            {
                bool useCyan=renderViewKey=="book"||(UsesManagementArt&&x>250);
                SetButtonSprite(item,useCyan?"cats-tab-selected":"selected");
                if(UsesMarchArt||(UsesManagementArt&&x<250))
                {
                    item.text.color=C("#241333");
                    MarchLettering(item.text,0);
                }
            }
            if(w<200 && x<250)
            {
                item.text.text=value.Trim();
                if((UsesMarchArt||UsesManagementArt)&&item.layout)item.layout.fontSize=22;
                if(Configure(item))
                {
                    item.text.alignment=TextAnchor.MiddleLeft;
                    ((RectTransform)item.text.transform).offsetMin=new Vector2(55,5);
                    item.text.fontSize=18;
                }
            }
            return clicked;
        }
    }
}
