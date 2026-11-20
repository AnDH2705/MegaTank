# Nhật ký gameplay CatTanks ngày 20/11/2026

**Agent này chỉ phụ trách gameplay của CatTanks trong `Sources/CatTanks`.** Phạm vi gồm vòng chơi, chiến đấu, điều khiển, kỹ năng, bắt quái, thiết kế ải, tiến trình và cân bằng gameplay.

## Đã làm hôm nay

- Đọc mã chiến đấu, hồ sơ khởi đầu, dữ liệu ải và báo cáo kiểm thử. Vòng chơi hiện tại gồm xếp đội, xuất kích, nhận thưởng và nâng đội. Trong trận, đội tự bắn. Người chơi di chuyển pháo đài, khóa mục tiêu, dùng kỹ năng và ném lồng. Hồ sơ mới có Hồi máu, Oanh kích và ba lồng thú. [Mã chiến đấu](Assets/CatTanks/Scripts/Battle.cs) · [Hồ sơ khởi đầu](Assets/CatTanks/Scripts/Profile.cs)
- Xác định điểm cần thử trong cơ chế bắt quái. Khóa mục tiêu khiến các khẩu trong tầm ưu tiên bắn vào chính con định bắt. Tự động bắn và đạn đang bay có thể làm cửa sổ ném lồng ngắn. Lồng bị tiêu ngay khi ném. Quái bắt được chỉ nhập đội khi thắng trận. [Chọn mục tiêu và bắt quái](Assets/CatTanks/Scripts/Battle.cs)
- Phát hiện HUD hướng dẫn sai khi khóa boss. HUD báo đủ yếu ở ngưỡng 25% HP và hướng dẫn nhấn `C`. Luật thực tế yêu cầu boss dưới 10% HP và dùng Lồng trăng bằng `Shift+C`. [HUD chiến đấu](Assets/CatTanks/Scripts/StudioBattleHUD.cs) · [Luật bắt quái](Assets/CatTanks/Scripts/Battle.cs)
- Đối chiếu nhịp mở đầu. Bốn ải đầu đều có 24 quái chia ba cụm với mốc sinh gốc 0, 25 và 55 giây. Dọn sạch sớm có thể kéo cụm kế tiếp tới sớm hơn. Các ải thêm dần loại quái và tăng HP. Boss đầu tiên ở ải 5. Cần kiểm tra xem các loại quái mới có khiến người chơi đổi mục tiêu, di chuyển hoặc giữ kỹ năng. [Dữ liệu ải](Assets/CatTanks/Resources/Data/catalog.json)

## Kiểm chứng tiếp

- Ưu tiên sửa hướng dẫn bắt boss theo đúng luật trước buổi playtest.
- Dùng hồ sơ mới chơi ải 1–5 hai lượt, một lượt ưu tiên thắng và một lượt chủ động bắt quái. Ghi số lần đổi mục tiêu có chủ đích, thời gian từ lúc quái đủ điều kiện bắt đến khi bị hạ và tình huống khiến người chơi giữ kỹ năng.
- Phiên hôm nay dừng ở việc đọc mã và đối chiếu dữ liệu. Chưa chạy Unity Play Mode hoặc đo cân bằng vì chưa tìm thấy Unity Editor. Báo cáo cũ mô phỏng bằng đội mạnh cố định, nên cần kiểm chứng riêng với đội mặc định. Chưa sửa mã hoặc dữ liệu gameplay. [Phạm vi kiểm thử đã ghi nhận](Documentation/PORT_NOTES.md)
