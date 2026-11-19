# Hướng dẫn chạy bộ mockup CatTanks trong Unity

**Game thật đã có Canvas dùng Mockup Kit:** mở `Assets/CatTanks/Scenes/Main.unity`. Hướng dẫn prefab live, binding gameplay và kiểm thử profile riêng: `Documentation/MOCKUP_GAME_UI.md`. Các bước dưới đây vẫn dành cho bộ preview cách ly.

## Trạng thái đã hoàn thành

- Bộ asset đã được chép vào `Assets/CatTanksMockupKit` của project này.
- Có 259 sprite key: 127 sprite mới và 132 PNG gốc dùng lại.
- Có dữ liệu ghép 21 màn hình/popup và công cụ tạo prefab preview.
- Đã kiểm tra ảnh, alpha, tên sprite, tọa độ và cú pháp C#.
- Preview có `PreviewIsolation`: không tự khởi động game thật; API đọc/ghi profile bị chặn trong scene preview, kể cả khi mở thêm scene game.
- Mã action nâng cấp kỹ năng dùng ID trong catalog game: `heal`, `barrage`, `shield`, `rally`, `freeze`, `drone`, `orbital`, `decoy`, `magnet`, `kamikaze`.
- Kiểm tra Unity tự động và ảnh render được ghi tại các đường dẫn bên dưới; đọc kết quả mới nhất thay vì coi kiểm tra dữ liệu là kiểm tra render.

Kết quả ngày 19/11/2026 trên Unity 2022.3.62f2: compile thành công, import 259 sprite, tạo 21 prefab và render 21 màn/popup; 101 kiểm tra preview đạt. Kiểm tra hồi quy ngoài preview đạt 124 mục gameplay và đọc/ghi profile bằng file test riêng. Profile hiện tại và bản sao lưu không đổi sau khi vào/ra Play Mode.

## Các bước chạy trong Unity

1. Mở project `D:\SeagullGame\MegaTanks\MegaTank\Sources\CatTanks` bằng Unity 2022.3.62f2.
2. Đợi Unity import và compile xong. Nếu Console có lỗi đỏ, xử lý lỗi trước khi tiếp tục.
3. Vào **CatTanks → Mockup Kit → 1 - Import and Slice Assets**.
4. Chạy **4 - Validate Sprite and Layout Coverage**. Console cần có `MOCKUP_KIT_VALIDATE_OK`. Lệnh này kiểm tra thư viện sprite, node/parent, action, ID kỹ năng và khả năng đi tới đủ 21 màn từ Hành quân.
5. Chạy **2 - Build All Preview Prefabs** để tạo đủ 21 prefab.
6. Chạy **3 - Create Preview Scene**.
7. Mở riêng scene vừa tạo, không chạy đồng thời với scene game cũ, rồi nhấn **Play**.
8. Duyệt menu trái, popup Cài đặt/Bách khoa và luồng Hành quân → Chuẩn bị xuất kích → Chiến đấu → **Xem kết quả mẫu**. Nút kết quả chỉ mở mẫu popup, không tính thắng/thua hoặc trao thưởng.

## Kết quả nằm ở đâu?

- Sprite library và atlas: `Assets/CatTanksMockupKit/Generated/`.
- Prefab: `Assets/CatTanksMockupKit/Generated/Prefabs/`.
- Scene: `Assets/CatTanksMockupKit/Generated/MockupKitPreview.unity`, hoặc tên có hậu tố nếu đã tạo trước đó.
- Rect và border sprite sau khi import: `Assets/CatTanksMockupKit/Generated/sprite-rects.csv`.
- Báo cáo kiểm tra Unity: `Logs/mockup-kit-validation.json`.
- Log compile/import/chạy kiểm tra: `Logs/mockup-kit-unity.log`.
- Báo cáo hồi quy gameplay: `Logs/port-validation.json`; log: `Logs/mockup-kit-game-regression.log`.
- Ảnh render đủ 21 màn: `Captures/MockupKit/`.

## Lưu ý khi xem preview

- Các nút chuyển màn hoạt động trong bộ preview.
- Action `game:*` là điểm nối controller, chỉ phát sự kiện hoặc ghi log; chưa chạy kinh tế, chiến đấu hay lưu profile thật.
- Marker cách ly được gắn tự động vào mỗi prefab preview. Không xóa marker để nối gameplay. Khi tích hợp thật, chuyển visual sang luồng game riêng và viết controller có kiểm thử thay vì dùng scene preview làm scene game.
- Không thay Canvas game hiện tại bằng preview trước khi nối dữ liệu và hành vi.
- Nếu menu Mockup Kit không xuất hiện, xem lỗi compile trong Console và kiểm tra các package `com.unity.ugui`, `com.unity.2d.sprite`.
- Nếu gặp lỗi đỏ, lưu nguyên nội dung lỗi để xử lý tiếp.

## Chạy lại kiểm tra tự động

Đóng riêng project CatTanks trước khi chạy; không mở hai Unity Editor trên cùng thư mục project. Từ PowerShell tại thư mục project:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -executeMethod CatTanks.MockupKit.Editor.MockupKitValidation.RunPipeline -screen-width 1600 -screen-height 900 -logFile "$($PWD.Path)\Logs\mockup-kit-unity.log"
```

Không thêm `-nographics`: bài kiểm tra cần render bằng Unity. Không thêm `-quit`: bài kiểm tra tự thoát sau khi vào/ra Play Mode và ghi báo cáo. Exit code 0 là PASS, 1 là FAIL.

Bài kiểm tra import/slice, tạo prefab và scene preview mới; kiểm tra không có game/Canvas thật khởi động, từ chối truy cập profile, bấm các nút chuyển màn, render và kiểm tra 9-slice. Mã SHA-256 của profile, `.bak` và `.tmp` được so sánh trước, trong và sau Play Mode; không sửa hoặc khôi phục đè profile.

## Dữ liệu bàn giao có sẵn trong project

- `Assets/CatTanksMockupKit/Data/asset-manifest.json`: file ảnh, lưới sprite và border.
- `Assets/CatTanksMockupKit/Data/screen-layouts.json`: tọa độ, thứ tự lớp, sprite key và action cho từng màn.
- `Assets/CatTanksMockupKit/Runtime/`: dựng Canvas và điều hướng preview.
- `Assets/CatTanksMockupKit/Editor/`: import, prefab, validation và theme candidate.

Đường dẫn `F:\tank\...` trong bản bàn giao gốc thuộc máy khác. Bộ tài liệu/ZIP ngoài project đó hiện không có tại đường dẫn cũ; các bước trên không phụ thuộc vào chúng.
