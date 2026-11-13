# MEOW BASTION — Bộ tài liệu meta cho repo hiện có

Đây là spec **cho project web** `/workspace/tank_cat/` (MEOW BASTION — Pháo đài trăng xanh): vanilla JS + Three.js 0.180.0, **không phải Godot**. Combat và HUD trận **đã có** và khớp concept `Moonlit Cat-Dog Tank Battle.png`. Bộ tài liệu mô tả phần **meta** (idle, gacha, nâng cấp, tầng/operator, PvP mock, **Chuồng quái / sổ thu thập**, localStorage) như các module JS gắn vào code đang chạy, đồng thời giữ `npm run check` và xuất kích gốc.

Nếu có bản spec Godot / Aetherbound / AFK cũ: **bỏ**, không làm theo.

## File

| File | Vai trò |
|---|---|
| `TANK_CAT_SPEC.md` | Nguồn sự thật: game đang có, đã có vs cần thêm, mở rộng `Battle`, meta, milestone, luật AGENTS.md |
| `01_CODE_SKELETONS.md` | Skeleton ES module (`src/meta/*`, options `Battle`) |
| `02_DATA_AND_BALANCE.md` | Chỉ phần **thêm** vào `data.js`; không copy lại 40 vũ khí / 20 quái |
| `03_UI_WIREFRAMES.md` | Hub 3 chỗ kiểu AFK; HUD classic giữ, loadout tối giản |
| `README.md` | File này |

## Prompt gợi ý

> Đọc `/workspace/tank_cat/AGENTS.md`, `SESSION_NOTES.md`, rồi `TANK_CAT_SPEC.md` (v3.7). Làm meta trên đúng repo tank_cat (JS + Three.js local). Giữ `new Battle()` và HUD trận như hiện tại; `npm run check` phải PASS. Thêm `src/meta/*`, Hub 3 chỗ (Hành quân / Doanh trại / Thành mèo), Chuồng quái, bắt trên sân, HP đơn vị, localStorage schema 4 (`catXp`, `arenaToken`). Tái sử dụng CAT_NAMES, TURRETS, HANDHELD, PACKS, SKILLS, MONSTERS, WAVES (lookup theo id). Một mèo/tầng cố định, bộ binh theo xe; `specialty` +15% đúng chỗ. Data-driven: sẵn sàng +30 mỗi loại nội dung (mục 16). Không Godot, không viết lại combat, Hub kiểu AFK landscape; không đè UI AFK lên màn trận. Làm theo milestone, bắt đầu M1.

## Luật ngắn

Giữ ảnh gốc, vendor Three.js local, không khai đã xem gameplay nếu chưa mở trình duyệt. Idle không mô phỏng trận. `MAX_FLOORS = 5` là trần cố định. Vòng chính v1 = Hành quân: 20 ải × 1 đợt (trùm 5/10/15/20). **10 kỹ năng, chọn 4 mỗi trận**; energy 1/3, hồi nhanh; nâng `skillCore`. Bắt tách nút, không tốn energy. Xe/mèo/pet có HP; pet **tự đánh** + knockback trùm. 10 mèo có `specialty` operator/infantry, đúng chỗ +15%. PvP AI tự dùng skill (chỉnh sau). Kinh tế kiểu AFK: idle ~70–80% coins/dust/catXp; daily quest; shop + arena token. Thua: giữ đồ đã nhặt, mất quái bắt trận đó. Classic 6 đợt + HUD 6 skill cũ là mode riêng, `check.mjs` phải PASS. Tầng 1/20/40/60/80; late ~5 ngày/tầng (TUNABLE). Sẵn sàng +30 mỗi loại nội dung: data-driven, id chuỗi, UI cuộn, save chịu id mới, sprite fallback; `MAX_FLOORS` vẫn 5. Hub 3 chỗ kiểu AFK (Hành quân có rương cạnh Chiến đấu; Doanh trại; Thành mèo), màn trước trận, chấm đỏ chỉ khi đủ resource; classic HUD không đổi.
