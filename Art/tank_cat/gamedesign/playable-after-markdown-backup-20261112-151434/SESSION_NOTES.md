# Bàn giao — 12/11/2026

Người dùng yêu cầu triển khai tiếp theo spec, bám concept UI/UX và giữ combat hiện tại. Đã thực hiện trong `gamedesign/playable`, vì chỉ `gamedesign` có quyền ghi. Bản gốc `tank_cat` không bị ghi đè.

## Đã triển khai

- Nhập 10 catalog JSON từ archive vào `public/data`, thêm năm gear reward đúng gợi ý phụ lục. Bản tài liệu giải nén giữ tại `../afk_clone_spec`.
- Ví 7 tài nguyên, trừ atomic; rương offline/online tối đa 12 giờ, claim thủ công, không trùng.
- Hồ sơ có checksum, backup, kiểm tra schema/reference/UID, migration field tùy chọn, báo lỗi thay vì tự reset; nhập/xuất JSON và chơi mới.
- Roster 12 định nghĩa, starter 3; lên cấp +1/+10/Max, thăng bậc có chọn/xác nhận fodder, bảo vệ tướng đang dùng, tái chế copy/hero, khóa.
- StatCalculator theo pipeline spec; 4 ô gear, bonus phe, sao, equip/swap độc quyền; relic và artifact.
- Nexus tự chọn 5 neo, cấp hiệu lực, ô mở theo ải, chặn follower lên cấp gốc.
- Gacha seed/state được lưu, pity 10/40, 10x guarantee, rate disclosure, gem→scroll.
- 20 ải khóa tuyến; first-clear/repeat/loss rewards; gear/artifact drops; ID trận ngăn trả thưởng trùng. Giao dịch Web Locks tránh hai tab claim cùng rương.
- UI thực trên concept xưởng xanh thép/viền vàng/nút xanh: rương, ví, roster/detail, nâng cấp, gear, summon/reveal, nexus, đội hỗ trợ, stage picker, kết quả, settings/profile. Có responsive và vùng cuộn điện thoại.

## Combat

Không port Godot, không dùng 5v5 AutoBattle. Giữ byte-identical `battle.js`, `data.js`, `visuals.js`, `enemy-animation.js` so với `../../src`. `meta/tank-adapter.js` áp snapshot vào HP chung, sát thương, phòng thủ, tốc độ bắn, hút máu và bonus kỹ năng. Crit quy về hệ số kỳ vọng, chuẩn hóa theo starter. Stage scale theo campaign; mỗi ải vẫn gồm 6 đợt/20 loài/40 vũ khí cũ.

Roster là đội hỗ trợ, không thay năm mèo cố định trên xe hoặc mười bộ binh. Nâng cấp chỉ áp dụng khi xuất kích trận mới, không đổi snapshot của trận đang tạm dừng. `game.js` nối service với trận/kết quả và HUD HP động. Không lưu derived stats/live simulation.

Combat tiếp tục khi đổi tab, timer tính elapsed thật. Không redraw WebGL khi tab ẩn hoặc ở xưởng; visual events ẩn bị bỏ qua. Về xưởng/pause/bách khoa dừng trận. Đóng trình duyệt không giữ trận dở, chỉ giữ tiến trình và thời điểm tích rương.

## Server và mở game

- Bản gốc 5173: health root `C:\Users\trieu\Desktop\tank_cat`, PID kiểm tra 18732.
- Bản mới 5174: health root `C:\Users\trieu\Desktop\tank_cat\gamedesign\playable`, PID sau restart 25196. PID có thể thay đổi, luôn kiểm tra health trước khi dừng server.
- `../CHOI_BAN_UIUX.cmd` gọi launcher ẩn có health check/tái sử dụng server. Chỉ là HTTP static local, không có backend tài khoản hay combat server.
- Localhost và 127.0.0.1 có localStorage riêng. Giữ một origin hoặc dùng xuất/nhập.

## Đã kiểm tra

- `npm.cmd run check`: original combat + tests core theo milestone; PASS. Starter ải 1 vẫn thắng 532s/121 kills/20 loài/40 vũ khí/14758 HP. Có test save quota rollback, multicaller claim, reward idempotency, migration, unsafe IDs/future schema, upgrade adapter.
- `npm.cmd run check:ui`: workshop và progression; PASS. Browser tests dùng profile mới/fixture riêng, không sửa hồ sơ trình duyệt của người dùng. Lưu/reload, xuất/nhập/nhập lỗi, rương, lên cấp, đồ/enhance, relic/artifact, thăng bậc/recycle, summon 1x/10x, Nexus, stage locks, full adapted battle victory → next stage và recovery được kiểm tra.
- `node scripts/check-background.mjs`: PASS, tab nền giả lập/blur/timer trễ/manual pause/bách khoa.
- `node scripts/check-game-animation.mjs`: PASS, WebGL gameplay và 4 trạng thái sói/pause/death/reset.
- Đã trực tiếp xem ảnh desktop 1600×900, landscape 900×506, portrait 390×844, hero detail, summon, nexus, result. Đã sửa content overflow dọc ở trang bị/triệu hồi; nav giữ cố định ngoài vùng cuộn.
- `npm.cmd run build`: đã chạy; phải chạy lại nếu thay source sau bản build gần nhất.

Ảnh và export fixture trong `test-results/progression-*`, `workshop-*`. Playwright/Chrome runtime dùng đường dẫn local có trong script.

## Giới hạn

Chưa chơi thủ công hết 20 ải, chưa cân bằng toàn chiến dịch hoặc đo FPS. 20 ải dùng cùng chiến trường/sáu đợt với hệ số khó khác nhau, không phải 20 background riêng. Không thực hiện cơ chế hàng trước/sau, ATB, khắc chế phe từ AutoBattle vì giữ combat xe tăng. Start-energy artifact không tăng quá trần 600 hiện có. Không có phase 2, IAP/cloud/backend. Dữ liệu và sprite/concept nguồn được giữ nguyên.
