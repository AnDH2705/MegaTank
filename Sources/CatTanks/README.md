# CatTanks — Pháo đài trăng xanh

Bản port C# native của `Art/complete_cat/CHOI_NGAY.html` (Meow Bastion v3.7), dùng **Unity 2022.3.62f1** và Built-in Render Pipeline. Camera trực giao, SpriteRenderer và mesh tạo phong cảnh 2.5D theo bản Three.js. Game chạy trực tiếp trong Unity và bản build; không dùng WebView hay JavaScript runtime.

## Mở và chơi

1. Unity Hub → Add project from disk → chọn thư mục `Sources/CatTanks`.
2. Mở bằng **2022.3.62f1**.
3. Mở `Assets/CatTanks/Scenes/Main.unity`, nhấn **Play**.
4. Hành quân → chọn ải → Chuẩn bị xuất kích → Xuất kích.

Bản macOS đã build sẵn tại `Builds/macOS/CatTanks.app`. Bản build nằm trong `.gitignore`; khi chuyển máy, build lại từ Unity hoặc chép riêng thư mục app.

| Điều khiển | Hành động |
|---|---|
| A / D hoặc ← / → | Di chuyển pháo đài |
| Chuột trái vào quái | Khóa mục tiêu cho toàn đội |
| 1–4 | Kỹ năng đã trang bị; thử thách cổ điển dùng 1–6 |
| C / Shift+C | Ném lồng thú / lồng trăng |
| P / Space / Escape | Tạm dừng |
| B | Bách khoa |
| F | Toàn màn hình |

## Nội dung

- Dữ liệu trích từ HTML: **156 ảnh PNG, 40 vũ khí, 20 quái, 10 mèo, 20 ải**, 10 kỹ năng chiến dịch và 6 kỹ năng cổ điển.
- Chiến dịch theo đợt, tự động nhắm/bắn, đạn dẫn đường, đạn cầu vồng, xuyên giáp, sát thương vùng, beam, Tesla, burn/poison/slow/stun và các hành vi quái/trùm.
- Doanh trại: nâng cấp/thăng bậc mèo, tái chế bản sao, di vật, 5 tầng pháo, sao pháo, 2 module/pháo, 5 ô bộ binh, 4 kỹ năng, trang bị và rèn, 3 đội lưu, Cộng hưởng.
- Bắt quái khi yếu, lồng thú/lồng trăng, mở lồng với bảo đảm độ hiếm, huấn luyện/nâng sao/tiến hóa, đồng hành và thưởng bộ sưu tập.
- Rương AFK tối đa 12 giờ, triệu hồi, cửa hàng giới hạn theo ngày, nhiệm vụ hằng ngày, đấu trường local, thử thách trăng 6 đợt và bách khoa.

Giao diện và phong cảnh đã dựng lại cho Unity. Xem [Documentation/PORT_NOTES.md](Documentation/PORT_NOTES.md) về các khác biệt với HTML và phạm vi đã kiểm chứng.

## Lưu tiến trình

Lưu JSON độc lập với localStorage của HTML, tại `Application.persistentDataPath/cattanks-profile.json`.

Trên máy Mac này: `~/Library/Application Support/MegaTank/Meow Bastion - CatTanks/cattanks-profile.json`. Có `.bak` tự động và nút **Cài đặt → Xuất hồ sơ JSON**. Không tự nhập/chuyển save từ trình duyệt.

## MCP

Package **MCP for Unity 10.1.0** đã được nhúng tại `Packages/com.coplaydev.unity-mcp`. Mở project sẽ tự thử kết nối server local đang có trên máy; có thể kết nối lại bằng **CatTanks → Connect MCP**. Đã xác minh instance `CatTanks` trên Unity 2022.3.62f1 trả lời được lệnh, điều khiển Play Mode và chụp ảnh.

Chi tiết kết nối và cách dùng trên máy khác: [Documentation/MCP.md](Documentation/MCP.md).

## Cấu trúc và kiểm tra

| Thư mục / file | Vai trò |
|---|---|
| `Assets/CatTanks/Scripts/Catalog.cs` | Định nghĩa và đọc dữ liệu gốc |
| `Assets/CatTanks/Scripts/Profile.cs` | Hồ sơ, tài nguyên, tính chỉ số, lưu JSON |
| `Assets/CatTanks/Scripts/Battle.cs` | Mô phỏng chiến đấu C# |
| `Assets/CatTanks/Scripts/BattleView.cs` | Camera, sprite, phong cảnh và hiệu ứng Unity |
| `Assets/CatTanks/Scripts/CatTanksGame.cs` | Luồng game, input, HUD và âm thanh |
| `Assets/CatTanks/Scripts/ManagementUI.cs` | Các màn quản lý |
| `Assets/CatTanks/Resources/Art` | Ảnh trích nguyên dữ liệu PNG |
| `Assets/CatTanks/Resources/Data/catalog.json` | Catalog và balance của HTML |
| `Assets/CatTanks/Editor/ProjectSetup.cs` | Tạo scene, MCP, validation, build |
| `Documentation/OriginalWeb` | Source JS/CSS tham chiếu; import alias đổi sang đường dẫn tương đối để xuất dữ liệu |
| `Captures` | Ảnh kiểm chứng trong Play Mode |

Chạy **CatTanks → Validate Port** trong Editor. Báo cáo sinh ở `Logs/port-validation.json`; bản đã kiểm tra lưu tại `Documentation/validation-report.json`. Có **124 kiểm tra đạt**, gồm mô phỏng cả 20 ải đến trạng thái kết thúc. Mô phỏng kiểm tra tính ổn định, không có nghĩa mọi đội hình đều thắng.

Build đã kiểm chứng: macOS, Mono, Main scene, Strict Mode, **0 errors / 0 warnings**. Windows/Android/iOS chưa build hoặc kiểm thử.

Để chạy validation từ terminal khi project chưa mở trong Editor:

```sh
"/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath "$PWD" \
  -executeMethod CatTanks.Editor.ProjectSetup.ValidatePort \
  -quit -logFile /tmp/cattanks-validation.log
```

Tạo lại scene nếu cần: **CatTanks → Create Main Scene**. Tác vụ này thay scene chính và build settings; không cần chạy khi mở project bình thường.

Nguồn, SHA-256 và version package: [Documentation/provenance.json](Documentation/provenance.json). Giấy phép đi kèm HTML: [Documentation/THIRD_PARTY_LICENSES.txt](Documentation/THIRD_PARTY_LICENSES.txt).
