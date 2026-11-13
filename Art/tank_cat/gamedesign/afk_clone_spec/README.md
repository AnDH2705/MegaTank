# Aetherbound — Bộ tài liệu triển khai (giao cho OpenAI Codex)

Bộ tài liệu này để **Codex (hoặc lập trình viên)** làm một game idle thu thập tướng trên **Godot 4.3+ / GDScript**, lấy cảm hứng *kiểu* AFK Arena nhưng **tên, số liệu, hệ thống đều gốc** (game tên **Aetherbound**). Combat là module tách, sau này thay được.

Người nhận tài liệu: lập trình viên mới bắt đầu. Mọi thứ đã viết cụ thể — Codex **không được đoán** hệ thống, công thức, hay thứ tự làm.

## File trong thư mục này

| File | Dùng để làm gì |
|---|---|
| `AFK_CLONE_SPEC.md` | **Nguồn sự thật.** Đọc trước. Logic, gameplay, công thức, luật, roadmap 0–14. |
| `01_CODE_SKELETONS.md` | Skeleton GDScript copy vào `res://`. Giữ đúng tên hàm. |
| `02_EXAMPLE_DATA.md` | JSON đầy đủ: balance, 12 tướng, đồ, relic, artifact, gacha, 20 ải, enemy. Copy vào `res://data/`. |
| `03_UI_WIREFRAMES.md` | Cây node, layout, kích thước từng màn hình 1080×1920. |
| `README.md` | File này. |

## Cách giao cho Codex (prompt gợi ý)

Dán (hoặc gắn) cả thư mục, rồi nói tối thiểu:

> Đọc `AFK_CLONE_SPEC.md` (mục 1, 18, 19) rồi làm game Godot 4 theo đúng tài liệu. Bắt đầu Milestone 0, làm lần lượt, đừng nhảy cóc. Copy skeleton từ `01_CODE_SKELETONS.md` và data từ `02_EXAMPLE_DATA.md`. Combat phải đứng sau `CombatInterface`. UI theo `03_UI_WIREFRAMES.md`. Không implement phase 2. Identifier và comment trong code bằng tiếng Anh; text UI tiếng Việt.

Làm việc dài: bảo Codex **chỉ làm một (hoặc vài) milestone mỗi lần**, gửi kết quả + cách test, rồi mới làm tiếp.

## Việc không làm

- Không sao chép tên riêng, art, lore, hay số cân bằng của AFK Arena. Số trong tài liệu là đề xuất TUNABLE.
- Không viết combat dính vào túi đồ / `GameState`.
- Không làm Tháp, Mê cung, Bounty, Furniture, Engraving ở v1.

## Ghi chú số liệu

Các bảng trong mục 16 của spec đã tính bằng `floor` (Python / tương đương Godot). Test phải khớp các mốc đó (sai số `pow` float cho phép ±1 nếu ghi rõ, nhưng các mốc đã chốt thì implement rồi đối chiếu).
