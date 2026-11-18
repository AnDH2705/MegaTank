TANK VFX UNITY — LỬA, NỔ, ĐƯỜNG ĐẠN

6 clip / 48 PNG frame có alpha, kích thước cố định 320 × 320:
- ground_fire: 16 frame, loop 16 fps.
- explosion: 16 frame, 24 fps, phát một lần.
- mg_tracer: 4 frame, loop 24 fps, hướng sang phải.
- muzzle_flash: 4 frame, 30 fps, phát một lần.
- shell_smoke_trail: 4 frame, loop 12 fps, dùng cho cannon / mortar.
- impact_sparks: 4 frame, 24 fps, phát một lần.

Frames/: PNG từng frame, đánh số 00... để giữ thứ tự animation.
Sheets/: sheet chuẩn ô 320 × 320; đọc từ trái sang phải, từ trên xuống dưới.
Preview/VFX_showcase.gif: xem thử sáu chuyển động.
Source_Artwork/: ảnh gốc sinh bằng imagegen và prompt. Các file gốc 1254 × 1254
được tách lưới và xuất lại thành sheet chuẩn; không dùng ảnh gốc để slice ô 320.

IMPORT
- PNG: Sprite (2D and UI), có Alpha Is Transparency.
- Chọn dùng Frames/ hoặc slice Sheets/ bằng Grid By Cell Size: 320 × 320.
- Pivot / fps / rect của từng frame có trong vfx_manifest.json.
- Fire và Explosion: pivot custom (0.5, 0.125), tính từ góc dưới trái.
- Vệt đạn hướng phải; xoay cả GameObject theo hướng bay.
- Tốc độ fps là cấu hình gợi ý cho các frame đã xuất, có thể thay theo game.

SCRIPT MẪU
SpriteFlipbook2D.cs: kéo các sprite đã sắp đúng thứ tự vào Frames, đặt fps và Loop.
GroundFireStrip2D.cs: gắn vào một GameObject nằm tại tâm vùng cháy trên mặt đất.
Kéo 16 fire frame vào Frames, giữ pivot (0.5, 0.125). Script dùng SpriteRenderer,
SortingGroup và SpriteMask để ghép cụm lửa và cắt gọn phần nhìn thấy theo width.

Ví dụ: fireStrip.SetWidth(4.5f);
width tính theo local world unit, chiều rộng thực tế còn chịu scale của GameObject.
cellHeight là chiều cao toàn ô sprite; chỉnh để đổi kích thước ngọn lửa.
Đổi width không tự đổi hitbox; đồng bộ collider / vùng gây sát thương trong game.
Các script là mẫu tích hợp; chưa chạy thử trong project Unity của bạn.

TÀI LIỆU UNITY THAM CHIẾU
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SpriteMask.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SpriteRenderer-maskInteraction.html
