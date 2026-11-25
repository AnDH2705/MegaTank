# 25/11/2026 – Chuẩn hoá ảnh nhân vật, đồ, vũ khí, quái

Nhiệm vụ thường trực mới: tìm, làm sạch và làm nét các ảnh chưa chuẩn hoá. Làm trên nhánh `art/chuan-hoa-anh` (worktree `F:\tank\MegaTank-art-clean`). **Chưa chép ảnh nào vào game**, đang chờ duyệt.

## Đã chốt với người dùng
- Phạm vi: nhân vật, đồ, vũ khí (cat, hat, armor, uniform, gear-boots, gun, turret, turret_alt, pack), cộng thêm quái vì đang lỗi nặng.
- Viền sticker trắng của chim và turret-1/3: chưa làm.
- Làm nét: xử lý cục bộ, không dùng AI upscale.
- Gốc là bộ ảnh mới đã commit ở `dc81cf37` (của phiên Claude khác, xem [2026-11-25_ASSET_NHAN_VAT_MOI.md](2026-11-25_ASSET_NHAN_VAT_MOI.md)).

## Kết quả quét bộ ảnh hiện tại (87/121 ảnh có cờ lỗi)
| Mức | Ảnh | Lỗi |
|---|---|---|
| Nặng | bug-0..9, beast-0..9 | Cắt theo ô lưới đều (rộng 355px = 1774/5) nên hụt chân, cánh, sừng. Có mảnh con bên cạnh dính ở mép. |
| Vừa | pack-0..19 (chim), turret-1, turret-3 | Viền sticker trắng đã bị tách nền ăn mất. Còn sót đường nét đứt mảnh lơ lửng cách thân ~6px (5–85 mảnh mỗi ảnh). |
| Vừa | cat-4, cat-11, cat-14, armor/uniform-6, hat-6/7, pack-4/5/8/15/17 | Mờ (< 60% độ nét trung bình nhóm). |
| Vừa | cat, hat, armor, uniform, gun, gear-boots | Thân hình có alpha 250–254, tức là hơi trong suốt (nhìn xuyên 1–2%). |
| Nhẹ | 15 con mèo, hat-6/7/9, armor-4/7 | Lề lệch 0–13px. Nét bút thừa ở mép trên các con mèo mới. |

## Đã làm
- Công cụ `Tools/ArtClean/` gồm `audit.py`, `clean.py`, `reslice_monsters.py`, `compare.py` và README. Chuẩn chi tiết ghi trong README.
- Tìm lại ảnh gốc của quái ở bản web gốc `Desktop/tank_cat` rồi chép vào `ArtSource/quai_goc/` (bugs.png, beasts.png). Đã cắt lại đủ 20 con.
- Chạy ra `Tools/ArtClean/out/Art`: 101 ảnh được làm sạch và 20 quái được cắt lại. Kiểm tra lại cho kết quả **0 cờ lỗi**.
- Bảng trước/sau lưu ở `Tools/ArtClean/out/compare/` và chép sang `Desktop\xem trc\Anh-chuan-hoa`.

## Còn tồn (không xử lý được bằng làm sạch)
- turret-1, turret-3 và các chim không có viền đen như các hình khác, vì viền sticker đã mất. Muốn đồng bộ thì phải vẽ hoặc tạo lại viền (người dùng đã bảo chưa làm).
- hat-6, hat-7 bị cắt phẳng ở đáy từ lúc cắt ảnh gốc. Cần cắt lại từ `ArtSource/nhanvat_dodacmoi/sheets/hats_uniforms.png`.
- armor-* và uniform-* là cùng một ảnh (assign.js cố ý dùng chung).
- Quái cắt lại trông nhỏ hơn 3–8% trong trận, vì game co theo chiều rộng ảnh và giờ ảnh đủ phần trước bị cắt.

## Bước tiếp
Người dùng duyệt bảng trước/sau → chạy `clean.py --apply` trên nhánh → commit → merge vào main (không push).
