# Mockup Kit trong game thật

Mở `Assets/CatTanks/Scenes/Main.unity` để chơi bằng UI mới. Các scene trong `Assets/CatTanksMockupKit/Generated` vẫn là preview cách ly, không dùng để chơi hoặc lưu profile.

## Canvas và prefab

- Canvas live: `Assets/CatTanks/Resources/UI/Mockup/CatTanksUI.prefab`.
- Theme: `Assets/CatTanks/Resources/UI/Mockup/MockupGameTheme.asset`.
- Screen: `Assets/CatTanks/Resources/UI/Mockup/Screens/`.
- Popup: `Assets/CatTanks/Resources/UI/Mockup/Popups/` — mỗi popup `settings`, `prebattle`, `book`, `choice`, `pause`, `result` là một prefab riêng, được lồng vào Canvas.
- Widget cho nội dung động: `Assets/CatTanks/Resources/UI/Mockup/Components/`.

Prefab live giữ `StudioUIView` và `StudioWidgetBinding` của game, dùng sprite, border, nền, chân dung và theme Mockup Kit. Đây là các view của controller gameplay, không phải `MockupKitPreview` hoặc bản JSON với số liệu mẫu. Không gắn `PreviewIsolation` vào Canvas live.

Các nút dùng lại lệnh trong `StudioCanvas`, `StudioScreens`, `ManagementUI`, `StudioEconomyScreens`, `StudioBattleHUD` và `StudioPicker`. Vì vậy giá nâng cấp, điều kiện mở khóa, giao dịch, lựa chọn đội hình, hồi chiêu, phần thưởng và lưu profile vẫn do gameplay hiện tại quyết định. UI đọc ví tiền, nhân vật, kho đồ và trạng thái trận từ profile/battle đang chạy. Nút xem kết quả mẫu chỉ tồn tại trong preview; game thật mở popup kết quả khi trận kết thúc.

Có thể chỉnh RectTransform/font trên prefab; cập nhật dữ liệu giữ vị trí đã author. Giữ nguyên `StudioWidgetBinding.id` và `kind` để controller tìm đúng widget. `spriteKey` cho phép panel dùng biến thể Mockup Kit riêng. 9-slice được áp dụng cho ảnh có border. Nội dung danh sách dài được sinh thêm từ các widget prefab tương ứng.

`StudioButtonLayout` giữ text và icon trong hệ tọa độ của nút: nút thường căn giữa, nút menu dành chỗ riêng cho icon bên trái, nút kỹ năng chia vùng phím tắt/icon/tên/chi phí. Font được giảm vừa đủ để giữ nguyên các xuống dòng có chủ ý, không tự đẩy chữ ra ngoài nút. Builder lưu các liên kết này vào prefab; runtime cũng áp dụng cho nút sinh thêm từ component prefab. `ButtonAlignmentAudit` kiểm tra khung text/icon và độ vừa chữ khi chụp cả 21 màn/popup.

## Chỉnh bản nháp bằng Layout Workshop

1. Mở `Assets/CatTanks/Scenes/Main.unity`, nhấn Play và mở màn hoặc popup cần chỉnh.
2. Vào **CatTanks → UI → Layout Workshop** rồi bấm **Chụp UI đang mở**. Cửa sổ đọc Canvas của game và các phần tử do code tạo trên màn đó.
3. Chọn phần tử trên ảnh hoặc tìm bằng tên/ID trong danh sách. Kéo phần tử để di chuyển, kéo ô vuông góc dưới bên phải để đổi kích thước, giữ Shift để giữ tỷ lệ. Có thể nhập X, Y, Rộng và Cao bằng số.
4. Kéo một Sprite từ Project vào ô **Sprite thay** để thử ảnh khác. Dùng ô ghi chú để mô tả hình cần vẽ hoặc nội dung muốn đổi. Chuyển sang màn khác trong Game View rồi chụp lại để chỉnh tiếp trong cùng một bản nháp.
5. Bấm **Xuất JSON** và gửi file. Dùng **Mở JSON** để nạp lại bản nháp. Mỗi mục lưu ID màn, ID phần tử, tọa độ trước và sau, ảnh thay cùng ghi chú. Tọa độ bắt đầu từ góc trái trên của phần tử cha, theo khung thiết kế 1600 × 900.

Các thay đổi hiển thị trong cửa sổ Layout Workshop. JSON dùng để bàn giao vị trí và hình ảnh cần áp dụng vào prefab hoặc code UI.

## Tạo lại asset

Mở Main trong Edit Mode, chạy **CatTanks > Mockup Kit > 7 - Build and Install Live Game UI**. Lệnh này tạo lại bộ prefab live từ template có binding của game và thư viện sprite đã import, rồi cài Canvas vào Main. Nó không đọc/ghi profile. Các chỉnh sửa trực tiếp trên prefab live sẽ bị thay thế khi tạo lại; cập nhật builder nếu muốn giữ chúng sau khi rebuild.

Theme LayerLab và bộ prefab cũ được giữ tại vị trí cũ làm template. Main và cơ chế tạo UI runtime ưu tiên bộ Mockup live. Không cần thay phiên bản hoặc chuyển đổi cấu trúc profile.

## Kiểm tra an toàn

Đóng riêng project CatTanks trước khi chạy Unity batch. Dùng Unity 2022.3.62f2 với các tham số:

```text
-batchmode -projectPath <CatTanks> -executeMethod CatTanks.Editor.MockupGameUIValidation.BuildAndTest -catTanksTestProfile <CatTanks>/Logs/mockup-live-profile.json -screen-width 1600 -screen-height 900 -logFile <CatTanks>/Logs/mockup-game-ui-unity.log
```

Không thêm `-quit` hoặc `-nographics`: bài kiểm tra vào/ra Play Mode, render rồi tự thoát. `-catTanksTestProfile` chỉ có trong Editor, chỉ chấp nhận đường dẫn trong Logs của project, được áp dụng trước `Awake`/`Profile.Load`. Lượt kiểm tra không dùng file profile đang chơi. Không chạy kiểm thử giao dịch bằng cách vào Play bình thường vì đó là chế độ chơi thật và sẽ tự lưu như trước.

Báo cáo: `Logs/mockup-game-prefabs.json`, `Logs/mockup-game-ui-validation.json`, `Logs/ui-smoke-validation.json`. Ảnh game thật dùng dữ liệu test: `Captures/MockupGameUI/`. SHA-256 của profile hiện tại, `.bak` và trạng thái `.tmp` được đối chiếu trước và sau khi dừng Play Mode. Không khôi phục đè hoặc reset profile.

## Kết quả kiểm tra 19/11/2026

- Unity 2022.3.62f2 compile thành công; 81 kiểm tra UI live và 36 smoke test đạt, không có lỗi runtime.
- Render 21 màn/popup ở 1600×900; 6 popup prefab riêng và 10 kiểm tra mesh giữ góc 9-slice khi đổi kích thước đạt.
- Nút nâng cấp mèo/kỹ năng, triệu hồi, mua đồ, nhận AFK/nhiệm vụ, đổi đội hình, lưu/nạp preset, đổi tên, xuất kích, kỹ năng combat, pause/tiếp tục/rút lui và sang ải tiếp theo được kiểm tra trên hồ sơ test.
- 124 kiểm tra gameplay hồi quy đạt. Nạp bản sao profile hiện có giữ nguyên tiến độ, ví tiền, mèo, tham chiếu trang bị và đội hình; báo cáo `Logs/mockup-game-regression.json`.
- File profile thật và `.bak` giữ nguyên SHA-256; không có file `.tmp` mới ở thư mục profile thật.
