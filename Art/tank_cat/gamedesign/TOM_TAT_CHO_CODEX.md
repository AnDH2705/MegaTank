# Điểm bắt đầu cho Codex tiếp theo — 13/11/2026

Đọc bàn giao chính: **`tank-cat-meta-v37/SESSION_NOTES.md`**, sau đó `tank-cat-meta-v37/AGENTS.md` và `README.md`.

- Đang phát triển: `tank-cat-meta-v37`, nhánh `feature/tank-cat-meta-v3.7`, port **5175**. Commit triển khai cuối: `4f7323b`. Mở `tank-cat-meta-v37/MO_BAN_MOI.cmd`.
- Bản sáng giữ nguyên: `playable`, nhánh `main`, commit `27f59f2`, tag `ban-sang-2026-11-12`, port **5174**. Không tự merge hoặc sửa vào đây.
- Thiết kế chuẩn: `tank-cat-meta-v37/docs/design-v3.7/`. **Cấu trúc theo markdown/AFK Arena, chỉ style theo concept, giữ combat classic**. Không dùng lại spec Godot/Aetherbound cũ ở `afk_clone_spec/`.
- Đã triển khai Hub3 nơi, quản lý mèo/xe,20 ải, idle/daily/shop, summon, bắt/nuôi pet,10 skill chọn4, preset và PvP B mock local. Test/build và browser QA đã qua ngày12/11; ảnh ở `docs/preview-v37/`. Chưa người dùng duyệt UI cuối, chưa xác minh cân bằng dài hạn hoặc chơi tay đủ20 ải.
- Source bundle và build zip ở `backups/`; chi tiết/hash trong bàn giao chính. Save người chơi là localStorage, không nằm trong Git.
- Script tắt máy đã thử nhưng Windows báo **Access is denied (5)**. Không tắt được. Không tự chạy lại từ ghi chú này.

Yêu cầu mới nhất chỉ là lưu tóm tắt cho agent sau. Đọc yêu cầu tiếp theo của người dùng trước khi thay đổi sản phẩm; kiểm tra branch/status và trạng thái server hiện tại trước khi làm.
