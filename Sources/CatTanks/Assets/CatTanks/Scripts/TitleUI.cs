using UnityEngine;

namespace CatTanks
{
    // Màn mở đầu kiểu game idle: cảnh ải làm nền, logo lớn, thanh tải kèm mẹo, rồi "CHẠM ĐỂ BẮT ĐẦU".
    // Vẽ đè lên màn chính ở lớp popup; màn chính bị khoá thao tác cho tới khi người chơi chạm.
    public sealed partial class CatTanksGame
    {
        const string TitleView="title";
        const float TitleLoadSeconds=1.6f;
        // Công cụ kiểm tra UI trong Editor tắt màn này để vào thẳng màn chính.
        public bool titleScreen=true;
        float titleStart=-1;
        static readonly string[] TitleTips=
        {
            "Mèo đúng chuyên môn nhận thêm chỉ số khi vận hành pháo.",
            "Rương AFK tích tài nguyên tối đa 12 giờ, nhớ quay lại nhận.",
            "Mỗi 5 ải có một trận trùm. Chuẩn bị Lồng trăng nếu muốn bắt trùm.",
            "Gọi đồng đội 10 lần chắc chắn có mèo Elite.",
            "Tiếp tế làm mới hằng ngày, huy hiệu đấu trường đổi được lõi kỹ năng."
        };

        void TitleOverlay()
        {
            if(titleStart<0)titleStart=Time.unscaledTime;
            float progress=Mathf.Clamp01((Time.unscaledTime-titleStart)/TitleLoadSeconds);
            SelectView(TitleView,"popup");uiInputAllowed=true;useCodeGeometry=true;
            if(!dispatchOnly&&uiParent.GetSiblingIndex()!=uiParent.parent.childCount-1)uiParent.SetAsLastSibling();
            // Cả màn là một nút chạm (chỉ nhận khi đã tải xong).
            if(ScrimButton()&&progress>=1){titleScreen=false;return;}
            if(!dispatchOnly&&lastButton!=null&&lastButton.image)lastButton.image.color=new Color(.05f,.04f,.14f,.35f);

            Art(380,110,840,210,"Interface/Cats/Brand");
            Text(300,330,1000,"Giữ pháo đài dưới ánh trăng xanh",subtitle,40,align:TextAnchor.UpperCenter,size:30);
            Art(650,390,300,240,MascotArt("mascot_ready"));

            if(progress<1)
            {
                Bar(400,690,800,24,progress,gold);
                Text(400,722,800,$"Đang chuẩn bị pháo đài… {Mathf.RoundToInt(progress*100)}%",small,30,align:TextAnchor.UpperCenter);
            }
            else
            {
                Surface(520,664,560,84,Color.white,catsFrame:"Gold");
                Text(520,664,560,"CHẠM ĐỂ BẮT ĐẦU",title,84,align:TextAnchor.MiddleCenter,size:40);
            }
            int tip=Mathf.FloorToInt((Time.unscaledTime-titleStart)/4f)%TitleTips.Length;
            Surface(330,780,940,64,Color.white,catsFrame:"Ink");
            Text(350,792,900,"MẸO: "+TitleTips[tip],small,40,align:TextAnchor.UpperCenter);
            Text(1380,858,200,"v"+Application.version,small,30,align:TextAnchor.UpperRight);
        }
    }
}
