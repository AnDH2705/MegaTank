using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        void HubUI()
        {
            Surface(-500,-500,2600,1900,C("#0d152a"));
            Surface(254,125,1322,740,C("#17243e"));
            Surface(24,125,210,740,C("#1e2c49"));
            Icon(30,23,64,"Badge_Crown"); Text(110,22,550,"MEOW BASTION",title);
            Text(112,68,550,"PHÁO ĐÀI TRĂNG XANH",small);
            Resource(770,"Money_Coin",N(profile.Get("coins")),"XU");
            Resource(983,"Gem_Diamond_Purple",N(profile.Get("gem")),"NGỌC");
            Resource(1196,"Ticket_Gold",N(profile.Get("summonScroll")),"VÉ GỌI MÈO");
            if (Button(1432,27,140,58,"Cài đặt")) modal="settings";
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
            Surface(38,748,182,96,C("#111d34"));
            Text(54,762,155,"LỰC CHIẾN",small); Text(54,791,155,N(profile.Power(db)),subtitle);
            Text(280,875,1250,"TIẾN TRÌNH TỰ LƯU    •    Esc: quay lại    •    Tab: chọn nút    •    Enter: xác nhận",small);
            if(page=="march")March(); else if(page=="barracks")Barracks(); else if(page=="town")Town(); else if(page=="chest")Chest(); else if(page=="quests")Quests();
        }
        void Resource(float x,string icon,string value,string caption)
        {
            Surface(x,22,195,76,C("#24324e")); Icon(x+10,37,44,icon);
            Text(x+64,30,125,value,subtitle); Text(x+64,63,125,caption,small);
        }
        void March()
        {
            Text(290,152,800,"Chuyến phiêu lưu dưới trăng",title);
            Text(292,204,790,"Chọn vùng đất. Sắp đội hình. Cùng mèo giữ vững pháo đài.",label);
            Surface(286,260,795,450,C("#111e36"));
            Text(310,280,520,"CHIẾN DỊCH  /  20 VÙNG ĐẤT",small);
            Text(850,280,205,profile.highestCleared+" / 20 ĐÃ QUA",small);
            for(int row=0;row<4;row++)
            {
                float y=330+row*89;
                Surface(350,y+27,670,6,C("#2d4565"));
                for(int col=0;col<5;col++)
                {
                    int i=row*5+col; var stage=db.stages[i]; float x=310+col*151;
                    bool unlocked=i<=profile.highestCleared, selected=profile.selected==i+1;
                    string status=i<profile.highestCleared?"ĐÃ QUA":unlocked?stage.boss?"TRÙM":"SẴN SÀNG":"KHÓA";
                    if(Button(x,y,137,66,$"{stage.index:00}  {(stage.boss?"• TRÙM":"")}\n{status}",unlocked,selected)) {profile.selected=i+1;profile.Save();}
                }
            }
            var selectedStage=db.stages[profile.selected-1];
            Surface(1103,155,438,665,C("#253455"));
            Text(1131,182,370,"ẢI "+selectedStage.index.ToString("00")+"  /  "+(selectedStage.boss?"TRẬN TRÙM":"HÀNH QUÂN"),small);
            Text(1131,226,370,selectedStage.name,title,100);
            Icon(1207,332,180,selectedStage.boss?"Skull":"Battle");
            Text(1131,528,370,"LỰC CHIẾN ĐỀ NGHỊ",small);
            Text(1131,558,370,N(selectedStage.recommendedPower),title);
            Text(1131,612,370,profile.Power(db)>=selectedStage.recommendedPower?"Đội của bạn đã sẵn sàng":"Gợi ý: nâng cấp mèo trước khi đi",small,52);
            bool cleared=selectedStage.index<=profile.highestCleared;
            Text(1131,669,370,cleared?"Thưởng khi chơi lại":"Thưởng lần đầu",small);
            Text(1131,697,370,Rewards(cleared?selectedStage.repeatRewards:selectedStage.firstClearRewards),small,55);
            if(Button(1131,758,382,58,"CHUẨN BỊ XUẤT KÍCH  ›",true,true))modal="prebattle";
            Surface(286,730,795,91,C("#20304b")); Icon(306,748,50,"Chest_Gold");
            Text(376,746,650,"Mỗi 5 ải là một trận trùm",subtitle);
            Text(376,782,650,"Thu thập mèo, trang bị và mở rộng pháo đài trên hành trình.",small);
        }
        bool TabButton(float x,float y,float w,float h,string value,bool selected)
        {
            bool clicked=Button(x,y,w,h,value);
            if(dispatchOnly)return clicked;
            var item=lastButton;
            if(selected)SetButtonSprite(item,"selected");
            if(w<200 && x<250)
            {
                item.text.text=value.Trim();
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
