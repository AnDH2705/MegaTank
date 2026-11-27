# 26/11/2026 – Mô phỏng màn chơi chính bằng code và chụp ảnh

Phạm vi: chỉ trong `Sources/CatTanks`, không sửa file nào của MegaTank. Không sửa code game.

## Yêu cầu

Người dùng cần ảnh chụp màn chơi chính (màn chiến đấu) được mô phỏng bằng code.

## Bối cảnh

- Máy hiện tại **không cài Unity Editor**. Dự án dùng bản 2022.3.62f2.
- Vì vậy không chụp được từ game thật. Thay vào đó, màn chiến đấu được dựng lại ngoài Unity.

## Cách làm

1. **Logic trận**
   - Dùng bản port `Battle.cs` sẵn có: `Tools/BalanceSim/battle.js`.
   - Script `dump.js` dựng một đội mẫu:
     - Pháo đài 3 tầng: `turret-0/1/2`, mèo vận hành `cat-0/1/2`, cấp 12.
     - 2 chim chiến: `pack-0` và `pack-1`.
     - 5 mèo bộ binh: `cat-3` đến `cat-7`, mỗi con cầm súng riêng theo catalog.
     - 4 kỹ năng: Sửa chữa, Mưa tên lửa, Lá chắn, Bão băng. Bật AI tự dùng kỹ năng.
   - Chạy trận rồi tự chọn khung hình đẹp nhất. Tiêu chí: nhiều quái nằm ở vùng giữa màn (x 560–1480), quái đứng cách nhau, nhiều loại quái, nhiều đạn đang bay.
   - Xuất trạng thái khung hình đã chọn ra JSON.
2. **Vẽ**
   - `render.html` vẽ trên canvas 1600×900, dùng ảnh thật trong `Resources/Art` và `Resources/Scenery`.
   - **Nền:** 4 lớp cắt từ atlas `stage-XX.png` theo `Scenery/Layouts.json`. Tọa độ `atlasY` tính từ đáy ảnh, giống `Rect` của Unity. Hệ số scale là 1600/1920, có parallax theo `playerX`.
   - **Pháo đài:** khung xe, các tầng và mái theo kích thước trong `BattleView.Fortress`.
   - **Đơn vị:** mèo, áo, mũ, súng hoặc tháp pháo đặt theo offset trong `BattleView.ActorView`. Chim chiến lơ lửng và không xoay theo hướng ngắm.
   - **Quái:** lật ngang, có bóng đổ, thanh máu, nhuộm màu khi bị làm chậm/độc/cháy. Sói `beast-0` dùng `wolf-frames/run-N`.
   - **Thứ tự lớp** theo `sortingOrder` trong code.
   - **HUD** theo tọa độ trong `StudioBattleHUD.cs`: xu, tên ải và thời gian, BÁCH KHOA, Âm thanh, máu pháo đài, quái đang tiến đến, năng lượng, 4 nút kỹ năng, lồng thú và lồng trăng.
3. **Chụp** bằng Chrome headless (`--screenshot`, cửa sổ 1600×900).

## Kết quả

Toàn bộ nằm trong thư mục `Captures/SimBattle/`. Chưa commit.

| File | Nội dung |
|---|---|
| `battle-stage-1.png` | Ải 1 Thung lũng Trăng, giây 22, 6 quái |
| `battle-stage-3.png` | Ải 3 Bìa rừng xanh, giây 18, 6 quái |
| `battle-stage-4.png` | Ải 4 Cổng đá cổ, giây 28, 9 quái, mưa tên lửa đang bay |
| `dump.js` | Chạy trận và xuất JSON: `node dump.js <ải> <seed> > state.json` |
| `render.html` | Đọc `state.js` (gán `window.STATE` và `window.LAYOUT`), vẽ cảnh và HUD |

## Chỗ khác với game thật

- Khung panel và nút HUD vẽ gần giống, không dùng sprite khung trong `MockupGameTheme`.
- Số xu là số giả (số quái bị hạ × 37).
- Đạn là hình vẽ đơn giản. Không có VFX nổ, khói, hay hiệu ứng `WebCombatVfx`.
- Quái là ảnh tĩnh, không dùng `MonsterAnimationLibrary`.
- Hướng ngắm của súng tính theo quái gần nhất, vì bản port không lưu `angle`.

## Quan sát từ ảnh (nên kiểm tra trong Unity)

- **Quái dồn cụm ở x khoảng 1100–1500.** Đây là đúng hành vi của cân bằng v1: tầm đánh 480–620, quái đứng lại bắn. Hệ quả là quái to (bug-1, beast-1, bug-2) chồng lên nhau, khó nhìn, khó chọn mục tiêu.
- **Chim chiến ở y=232 đè lên hàng mèo bộ binh**, khó phân biệt.
- **Trong khung hình đã chọn, pháo đài còn đầy máu.** Lý do là script chỉ chọn khung có máu trên 50%. Kết quả này không dùng để đánh giá độ khó.

## Còn mở

- Chụp lại bằng Unity thật khi có máy cài Editor để so sánh với ảnh mô phỏng.
- Cân nhắc giãn vị trí quái dừng lại, hoặc cho chúng đứng so le theo chiều dọc để bớt chồng nhau.
