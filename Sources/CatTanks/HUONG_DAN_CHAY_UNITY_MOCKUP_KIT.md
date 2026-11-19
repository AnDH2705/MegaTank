# Hướng dẫn chạy bộ mockup CatTanks trong Unity

## Trạng thái đã hoàn thành

- Bộ asset đã được chép vào `Assets/CatTanksMockupKit` của project này.
- Có 259 sprite key: 127 sprite mới và 132 PNG gốc dùng lại.
- Có dữ liệu ghép 21 màn hình/popup và công cụ tạo prefab preview.
- Đã kiểm tra ảnh, alpha, tên sprite, tọa độ và cú pháp C#.
- Chưa kiểm tra compile, import hoặc render trong Unity Editor.

## Các bước chạy trong Unity

1. Mở project `F:\tank\MegaTank\Sources\CatTanks` bằng Unity 2022.3.62f2.
2. Đợi Unity import và compile xong. Nếu Console có lỗi đỏ, xử lý lỗi trước khi tiếp tục.
3. Vào **CatTanks → Mockup Kit → 1 - Import and Slice Assets**.
4. Chạy **4 - Validate Sprite and Layout Coverage**. Console cần có `MOCKUP_KIT_VALIDATE_OK`.
5. Chạy **2 - Build All Preview Prefabs** để tạo đủ 21 prefab.
6. Chạy **3 - Create Preview Scene**.
7. Mở riêng scene vừa tạo, không chạy đồng thời với scene game cũ, rồi nhấn **Play**.
8. Duyệt menu trái, popup Cài đặt/Bách khoa và luồng Hành quân → Chuẩn bị xuất kích → Chiến đấu.

## Kết quả nằm ở đâu?

- Sprite library và atlas: `Assets/CatTanksMockupKit/Generated/`.
- Prefab: `Assets/CatTanksMockupKit/Generated/Prefabs/`.
- Scene: `Assets/CatTanksMockupKit/Generated/MockupKitPreview.unity`, hoặc tên có hậu tố nếu đã tạo trước đó.
- Rect và border sprite sau khi import: `Assets/CatTanksMockupKit/Generated/sprite-rects.csv`.

## Lưu ý khi xem preview

- Các nút chuyển màn hoạt động trong bộ preview.
- Action `game:*` là điểm nối controller, chỉ phát sự kiện hoặc ghi log; chưa chạy kinh tế, chiến đấu hay lưu profile thật.
- Không thay Canvas game hiện tại bằng preview trước khi nối dữ liệu và hành vi.
- Nếu menu Mockup Kit không xuất hiện, xem lỗi compile trong Console và kiểm tra các package `com.unity.ugui`, `com.unity.2d.sprite`.
- Nếu gặp lỗi đỏ, lưu nguyên nội dung lỗi để xử lý tiếp.

## Tài liệu chi tiết và bộ bàn giao

Thư mục:
`F:\tank\MegaTank\Art\CatTanks_Visual_Reference\06-UI-UX\04-Unity-Asset-Kit-20261118`

Đọc `00_BAT_DAU_O_DAY.txt` trước. Các thư mục `Docs/Screens` và `Data` có hướng dẫn từng màn, tọa độ, thứ tự lớp, sprite key và action.

File ZIP:
`F:\tank\MegaTank\Art\CatTanks_Visual_Reference\06-UI-UX\04-Unity-Asset-Kit-20261118.zip`