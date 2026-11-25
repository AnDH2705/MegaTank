# Nhật ký Claude – CatTanks – 25/11/2026 – Đưa bộ ảnh "nhanvat_dodacmoi" vào game

**Người làm: Claude (Claude Code).** Chỉ làm trên CatTanks. Chưa commit.

## Nguồn
- 6 ảnh lưới do người dùng cung cấp, chép vào [ArtSource/nhanvat_dodacmoi/sheets](../ArtSource/nhanvat_dodacmoi/sheets):

| File | Nội dung |
|---|---|
| `cats.png` | 20 mèo |
| `birds.png` | 18 chim |
| `guns.png` | 20 súng |
| `hats_uniforms.png` | 18 bộ mũ + áo |
| `boots.png` | 20 đôi giày |
| `turrets.png` | 10 tháp pháo |

- Thư mục `ArtSource` nằm ngoài `Assets` nên Unity không nạp.

## Công cụ: [Tools/ArtSlicer](../Tools/ArtSlicer)
Viết bằng Node + `pngjs`. Chạy `npm install` một lần; `node_modules` và `out` không commit.

| File | Việc |
|---|---|
| `slice.js` | Cắt lưới theo ô (`--grid RxC`). Tách nền đen dính cứng (`--black-bg`) bằng cách loang từ mép. Lật ngang (`--flip`). Tách cặp mũ/áo trong một ô (`--split`). Bỏ vụn viền lạc. Xuất `_preview.png` để soát. |
| `assign.js` | Ghi đè key cũ ở cả `Assets/CatTanks/Resources/Art` và `Assets/CatTanksMockupKit/Textures/Original`, giữ nguyên `.meta`/GUID. Key mới chỉ ghi vào `Resources/Art`. Cất phần chưa dùng vào kho. |
| `battle_preview.js` | Dựng ảnh ghép mèo + áo + mũ + súng theo đúng tọa độ của `BattleView`, để soát trước khi mở Unity. |

## Đã gán

| Ảnh mới | Key trong game |
|---|---|
| Mèo hàng 1–2 | `cat-0..9`, đúng thứ tự tên Mướp, Khói, Bông, Tam Thể, Mun, Xiêm, Vằn, Tux, Mochi, Mây |
| Mèo hàng 3–4 | **mèo mới `cat-10..19`** |
| Súng hàng 1–2 | `gun-0..9`, đúng loại súng. Mèo bộ binh vẫn cầm súng như cũ. |
| Tháp pháo (đã lật cho nòng chĩa phải, đã tách nền) | `turret-0..9` |
| 10 bộ quân phục | `hat-0..9`, `uniform-0..9`, `armor-0..9` |
| Giày đầu tiên | `gear-boots` |
| 18 chim (đã tách nền) | module `pack-*`. pack-17 (bom) và pack-18 (tinh thạch) **tạm** dùng Quạ và Giẻ cùi đổi màu, vì lưới chỉ có 18 chim. |

- **Kho** [ArtSource/nhanvat_dodacmoi/unused](../ArtSource/nhanvat_dodacmoi/unused):
  - 10 súng hàng 3–4;
  - 19 đôi giày;
  - 6 bộ mũ áo vui (samurai, phù thủy, hải tặc, thợ mỏ, cao bồi, đầu bếp);
  - 2 bộ bị lặp.

## Code và dữ liệu
- **`catalog.json`**
  - Thêm 10 mèo: Cà Phê, Than, Kem Dâu, Oải Hương (COMMON); Bóng Đêm, Lam, Sô-cô-la (RARE); Bạch Hổ, Báo Đốm, Đào (ELITE).
  - Chỉ số của mèo mới nằm trong khoảng của mèo cùng độ hiếm và cùng chuyên môn.
  - Mọi mèo có thêm trường `gun`. Mèo mới cầm súng có sẵn gun-0..9.
  - 20 module đổi tên thành chim (vd "Sẻ Gatling", "Đại bàng hỏa tiễn", "Bồ câu cứu thương") và đổi nhóm thành "CHIM CHIẾN". Cách bắn và chỉ số giữ nguyên.
- **`Catalog.cs`:** thêm `CatDef.gun`.
- **`Battle.cs`:** súng của mèo đọc từ `gun`. Trước đây đổi `cat-N` thành `gun-N`, nên cat-10 sẽ lỗi vì không có `gun-10`.
- **`BattleView.cs`**
  - Mèo dáng nằm mới: chỉnh vị trí và kích thước của mèo, áo, mũ, đã soát bằng ảnh ghép.
  - Chim (module) không còn xoay theo hướng ngắm: bay nhún lên xuống, nghiêng ±5°, vẽ rộng 80 (trước là 57).
- **UI**
  - Nhãn "Module" đổi thành "Chim" (ô ở màn Pháo đài, popup chọn, thông báo).
  - Tab Bách khoa đổi thành "CHIM CHIẾN". Phần thưởng lần đầu ghi "chim …".
  - Danh sách mèo ở Doanh trại và dải chọn mèo ở Bố trí có nút lật trang ‹ › (10 mèo mỗi trang). Dòng đếm hiện `x/20 mèo`.
- **Kiểm tra trong Editor**
  - `ProjectSetup.ValidatePort` kiểm tra 20 mèo, và mỗi mèo có súng tồn tại.
  - Hai bài test UI bấm theo nhãn mới "Chim 1" và "Gỡ chim".

## Đã kiểm tra
- Ảnh xem trước của từng nhóm: nền sạch, đúng thứ tự, tháp pháo chĩa phải.
- Ảnh ghép trong trận: mũ nằm trên đầu mèo, áo ôm thân.
- Cả 91 key ảnh mà catalog dùng đều có file.
- `Battle.cs`, `Catalog.cs`, `Profile.cs` biên dịch bằng `dotnet` (giả lập Unity): **build succeeded**.
- Các file UI và `BattleView.cs` chưa biên dịch thử, chỉ soát diff bằng mắt.

## Cần người dùng làm trong Unity
- Mở project để Unity nhập ảnh mới và tạo `.meta` cho `cat-10..19`.
- Chạy menu kiểm tra của project.
- Xem các màn: Doanh trại (lật trang mèo), Bố trí, Pháo đài (ô chim), Bách khoa, và một trận (chim bay; mũ áo trên mèo nằm).
- Hai chim pack-17/18 đang là bản đổi màu tạm, cần vẽ thêm.
