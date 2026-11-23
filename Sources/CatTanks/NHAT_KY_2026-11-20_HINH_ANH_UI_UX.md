# Nhật ký CatTanks ngày 20/11/2026

## Phạm vi riêng của agent này

**Agent này chỉ phụ trách hình ảnh và UI/UX của CatTanks** tại `Sources/CatTanks`. Phạm vi gồm sprite, bối cảnh, poster, ảnh khởi động, icon, bố cục và tương tác giao diện, cùng phần code Unity cần để hiển thị và kiểm tra các hạng mục đó. Phân công này áp dụng riêng cho agent của phiên này.

## Mở Layout Workshop để làm tiếp ngày 21/11

- Mở project Unity tại `F:\tank\MegaTank\Sources\CatTanks` và mở [Main.unity](Assets/CatTanks/Scenes/Main.unity).
- Nhấn **Play**, vào màn hoặc popup cần sửa. Trong menu Unity, chọn **CatTanks → UI → Layout Workshop**, rồi bấm **Chụp UI đang mở**.
- Kéo phần tử để đổi vị trí, kéo góc dưới bên phải để đổi kích thước, giữ **Shift** để giữ tỷ lệ. Có thể thử sprite khác hoặc ghi chú hình cần vẽ ngay trong tool.
- Bấm **Xuất JSON** và chọn nơi lưu. Tên file gợi ý là `CatTanks-UI-Layout.json`. Phiên sau gửi file này hoặc mở lại bằng **Mở JSON**. File lưu ID màn, ID phần tử, tọa độ, sprite thay và ghi chú để áp dụng vào prefab hoặc code UI.
- [Mã nguồn Layout Workshop](Assets/CatTanks/Editor/UiLayoutWorkshop.cs) · [Hướng dẫn chi tiết](Documentation/MOCKUP_GAME_UI.md#chỉnh-bản-nháp-bằng-layout-workshop)

## Cập nhật cuối phiên

- Mẫu màu ải 01 đã được duyệt. Đã dùng mẫu đó cho toàn bộ 20 nền chiến đấu và đồng bộ ảnh chọn ải. Nét mảnh, mỗi ải có bảng màu riêng, vùng giao tranh giảm độ rực và tương phản để mèo và quái dễ nhìn. [Ảnh mẫu ải 01](Captures/Stage01-Color-Comparison.jpg) · [Nền chiến đấu](Assets/CatTanks/Resources/Scenery) · [Ảnh chọn ải](Assets/CatTanks/Resources/Art/StagePreviews)
- Cập nhật nền sảnh, xưởng, màn chọn ải, nền dự phòng trong trận và ảnh thu nhỏ dùng chung của Mockup Kit. [Asset Mockup Kit](Assets/CatTanksMockupKit/Textures/Generated)
- Đổi UI trong game sang Nunito Bold 700 cho chữ thường và Nunito Black 900 cho nút, tiêu đề. Đã đối chiếu dấu tiếng Việt trong cả hai file font và đo tám nhãn tab của màn Doanh trại. [Font](Assets/CatTanks/Resources/UI/Fonts) · [Code Canvas](Assets/CatTanks/Scripts/StudioCanvas.cs)
- Khi có Unity Editor, mở `Main.unity` để kiểm tra chữ trên nút hẹp như **CỘNG HƯỞNG**, dấu tiếng Việt trong HUD và popup, cùng độ nổi của mèo và quái khi nền chuyển động. Phiên này đã kiểm tra file và cú pháp, còn bước kiểm tra trực tiếp trong game.

## Sprite nhân vật, trang bị và pháo đài

- Dùng ImageGen vẽ lại 110 sprite mèo, vũ khí cá nhân, tháp pháo, module, trang bị, đạn và xe hỗ trợ. Thay PNG tại chỗ trong cả thư mục game và Mockup Kit, giữ tên và GUID Unity.
- Thêm 29 sprite gồm vật phẩm, kỹ năng, giày hành quân, gầm xe, tầng lặp và buồng chỉ huy. Pháo đài trong trận ghép ảnh theo cấu hình từ 1 đến 5 tầng.
- Cập nhật 6 sprite sheet của Mockup Kit, giữ các định danh ô sprite hiện có. [Sprite trong game](Assets/CatTanks/Resources/Art) · [Sprite sheet Mockup Kit](Assets/CatTanksMockupKit/Textures/Generated)

## Bối cảnh, poster và ảnh kết quả

- Hoàn thiện 20 nền chiến đấu cho khung 1920 × 1080. Mỗi ải có bốn lớp gồm bầu trời, hậu cảnh xa, trung cảnh và mặt đường. Ảnh được nạp theo ải và các lớp dịch ngang theo di chuyển của xe. [Nền chiến đấu](Assets/CatTanks/Resources/Scenery)
- Hoàn thiện 20 ảnh giới thiệu ải, với minh họa boss riêng cho ải 5, 10, 15 và 20. Hành quân và Chuẩn bị xuất kích dùng ảnh của ải đang chọn. [Ảnh giới thiệu](Assets/CatTanks/Resources/Art/StagePreviews)
- Lưu poster 1600 × 2400 và ảnh khởi động 1920 × 1080. Cấu hình Unity dùng ảnh ngang khi khởi động và poster làm ảnh dọc, với logo Unity theo cấu hình tương thích Unity 2022.3 Personal. [Poster và ảnh khởi động](Assets/CatTanks/Branding) · [Cấu hình Unity](ProjectSettings/ProjectSettings.asset)
- Gắn hai sprite minh họa vào popup kết quả theo trạng thái thắng hoặc thua. [Minh họa kết quả](Assets/CatTanks/Resources/Art/Screens)

## Icon và nút UI

- Đổi 20 nút chữ ở Hành quân thành thẻ có ảnh ải, số ải và dấu khóa, đã qua hoặc boss. Chuyển chân dung mèo và hình pháo thành thẻ bấm trực tiếp.
- Đưa icon vào tab và ô chọn đội hình, module, kỹ năng. Các thao tác xuất kích, triệu hồi, nâng cấp và nhận thưởng giữ nhãn và giá khi có.
- Dùng hình lồng thú và lồng trăng ở HUD, kèm số lượng và phím tắt. Gộp đường dẫn thoát trận vào menu Tạm dừng.
- Giữ cơ chế xử lý click, Tab và Enter của Canvas. Bổ sung kiểm tra cho thẻ ải khóa, chọn ải và chọn mèo qua thẻ. [Code Canvas](Assets/CatTanks/Scripts/StudioCanvas.cs) · [Bố cục nút](Assets/CatTanks/Scripts/StudioButtonLayout.cs) · [Kiểm tra UI](Assets/CatTanks/Editor/MockupGameUIValidation.cs)

## Kết quả kiểm tra và phần còn mở

- Đã kiểm tra mép trong suốt của 139 PNG, 65 ô trong các sprite sheet, tham chiếu icon và ảnh ghép pháo đài từ 1 đến 5 tầng.
- Đã đối chiếu đủ 20 ảnh nền với danh mục ải và kiểm tra 80 lớp phủ kín khung 1920 × 1080 tại ba vị trí xe trong bản ghép kiểm tra. Kiểm tra cú pháp C# và `git diff --check` đạt.
- Môi trường phiên chưa có Unity Editor. Kiểm tra Play Mode và bản build còn mở. Khi có Editor, dùng `Assets/CatTanks/Scenes/Main.unity` để kiểm tra hiển thị thẻ, khóa ải, điều hướng, nền chuyển động và popup kết quả. Xem ảnh khởi động trên bản build.
