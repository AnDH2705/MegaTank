# Nhật ký CatTanks ngày 20/11/2026

## Phạm vi riêng của agent này

**Agent này chỉ phụ trách hình ảnh và UI/UX của CatTanks** tại `Sources/CatTanks`. Phạm vi gồm sprite, bối cảnh, poster, ảnh khởi động, icon, bố cục và tương tác giao diện, cùng phần code Unity cần để hiển thị và kiểm tra các hạng mục đó. Phân công này áp dụng riêng cho agent của phiên này.

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
