# ArtClean — chuẩn hoá ảnh nhân vật / đồ / vũ khí / quái

Phạm vi: `cat-*`, `hat-*`, `armor-*`, `uniform-*`, `gear-boots`, `gun-*`, `turret-*`, `turret_alt-*`, `pack-*`, `bug-*`, `beast-*` trong `Assets/CatTanks/Resources/Art`.

Chạy bằng Python 3.12 có sẵn trên máy (Pillow, OpenCV, numpy):

```
set PY=%LOCALAPPDATA%\Python\pythoncore-3.12-64\python.exe
cd Sources\CatTanks\Tools\ArtClean
%PY% -I audit.py                 :: quét ảnh trong game, gắn cờ lỗi -> out/audit/
%PY% -I reslice_monsters.py      :: cắt lại bug/beast từ ArtSource/quai_goc -> out/Art
%PY% -I clean.py                 :: làm sạch các nhóm còn lại -> out/Art (KHÔNG đụng game)
%PY% -I compare.py               :: bảng trước/sau -> out/compare/
%PY% -I compare.py --zoom cat-11 :: phóng to 3x một ảnh
%PY% -I audit.py --dir out/Art   :: kiểm tra lại kết quả (mục tiêu: 0 cờ lỗi)
%PY% -I clean.py --apply         :: CHỈ chạy sau khi đã duyệt: chép out/Art vào game
```

## Chuẩn
| Bước | Nội dung |
|---|---|
| Mảnh rác | Xoá mảnh rời là nét mảnh < 4px (đường viền sticker đứt quãng) hoặc < 0,2% diện tích hình chính. Mảnh dày (mỏ, chân, nòng tách rời) được giữ. |
| Bụi | Xoá điểm alpha < 48 không nằm sát hình, và mọi điểm alpha < 4. |
| Thân trong suốt | Thân hình alpha 240–254 → 255 (bộ ảnh mới bị nhìn xuyên 1–2%). |
| Làm nét | Chỉ ảnh có độ nét < 60% trung vị nhóm. Unsharp mask trong lòng hình (cách mép 2px), tăng dần tới ~65% trung vị. |
| Lề | Cắt sát rồi chừa đều 8px mọi phía. Không phóng to hay thu nhỏ hình. |
| Chống quầng | Loang màu mép ra vùng alpha = 0 để lọc bilinear của Unity không sinh viền đen/trắng. |
| Quái | `bug-*` / `beast-*` cũ bị cắt theo ô lưới đều nên hụt chân, cánh, sừng. Cắt lại từ ảnh gốc nền trắng (tách nền loang từ mép, mép lấy alpha mềm theo độ sáng). |

Giữ nguyên tên file và `.meta` (GUID). `--apply` chép luôn vào bản sao `CatTanksMockupKit/Textures/Original` nếu file có ở đó.

## Lưu ý
- Game co sprite theo **chiều rộng ảnh** (`BattleView.Art` → `Sprite.Create(..., tx.width)`). Đổi lề hoặc cắt lại làm hình trong trận nhỏ hay to hơn vài %. Quái cắt lại trông nhỏ hơn bản cũ khoảng 3–8% vì giờ đủ cả phần trước đây bị cắt.
- Viền sticker trắng của chim (`pack-*`) và `turret-1/3` vốn đã bị tách nền ăn mất từ trước. Công cụ chỉ xoá đường nét đứt còn sót lại, không vẽ thêm viền.
- Ảnh gốc của quái: `ArtSource/quai_goc/bugs.png`, `beasts.png` (chép từ bản web gốc `Desktop/tank_cat`).
