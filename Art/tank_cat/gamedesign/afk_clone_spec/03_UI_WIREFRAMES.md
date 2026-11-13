# Phụ lục 03 — UI wireframe và cây node (1080×1920)

Mọi tọa độ là **pixel logic** trên viewport 1080×1920. Neo (`anchors`) dùng tỉ lệ 0–1 của cha. Margin tính bằng px. Codex được phép chỉnh ±8–16px cho đẹp, không đổi thứ tự khối hay ẩn thành phần bắt buộc.

Theme màu: xem `AFK_CLONE_SPEC.md` mục 17.16. Font: mặc định Godot nếu chưa nhúng font; ưu tiên font có đủ dấu tiếng Việt nếu bạn thêm được file `.ttf` miễn phí (ví dụ Noto Sans) vào `res://ui/theme/fonts/`. Nếu không có font Việt, vẫn dùng mặc định và chấp nhận fallback.

---

## 0. Khung AppRoot (`res://ui/navigation/AppRoot.tscn`)

Cây:

```
AppRoot (Control) anchors full rect
├── Background (ColorRect) #120E18, full rect
├── SafeColumn (MarginContainer) margin 0,0,0,0  (later: safe area)
│   ├── VBox (VBoxContainer)
│   │   ├── CurrencyBar (instance, min size 1080×112)
│   │   ├── ScreenHost (Control, size flags expand fill)
│   │   └── BottomNav (MarginContainer, min size 1080×160)
│           ├── HBox 4 nút bằng nhau, tách 16px, margin 24,16,24,24
│           └── mỗi nút: VBox icon 64×64 + Label 28px
└── OverlayLayer (CanvasLayer, layer 10)
    ├── Dim (ColorRect #000000AA, hidden by default, full)
    ├── PopupHost (Control, full)
    └── ToastHost (Control, top center, y=160, width 800)
```

`CurrencyBar` và `BottomNav` **ẩn** khi screen id ∈ {`battle`, `summon_reveal`} (Result / Idle Claim là popup nên bar vẫn có thể hiện phía dưới mờ). Battle: ẩn cả bar lẫn nav.

Nút BottomNav: Home / Heroes / March / Summon. Tab đang chọn: accent vàng + gạch dưới 4px. Tab không chọn: chữ `#B0A8C0`.

Nút bấm: `pivot_offset` giữa, tween scale 0.96 / 1.0 trong 0.08s.

---

## 1. CurrencyBar (`res://ui/components/CurrencyBar.tscn`)

Cao 112, nền `#241E33`, padding ngang 24.

```
HBox (separation 16)
├── GoldChip     icon 48 + label (expand)
├── DustChip     icon 48 + label
├── ScrollChip   icon 48 + label + nút "+" 48×48
├── GemChip      icon 48 + label + nút "+" 48×48
└── MoreButton   "⋮" 64×64  → mở sheet Ví
```

Chip: `PanelContainer` góc 12, padding 8. Bấm chip cũng mở sheet Ví. Sheet Ví: popup 900×1000, list 7 dòng (icon, tên Việt, số đầy đủ không compact).

---

## 2. Hub (`res://ui/screens/hub/HubScreen.tscn`)

Trong ScreenHost (diện tích ~1080×1648 trừ bar/nav):

```
HubScreen (Control) full
└── Scroll (nếu cần, thường vừa một màn)
    └── VBox margin 32,16,32,16, separation 24
        ├── TitleRow: Label "Aetherbound" 56px + Label player_name 28px mờ
        ├── CacheCard (Panel, min height 420, corner 24)
        │   ├── ChestVisual (ColorRect hoặc TextureRect 200×200, center top, màu vàng #E0B04A)
        │   ├── Badge (Label "ĐẦY" / số phút, góc phải trên, hidden nếu < 60s)
        │   ├── TimeBar (ProgressBar 0–1, height 28, width 900, dưới chest)
        │   ├── PeekRow: "Vàng +X   Bụi +Y" 32px
        │   └── Hint: "Bấm để xem và nhận" 24px
        ├── PrimaryButton "Hành quân" 984×140, font 48, accent fill
        └── ModesRow (HBox, 4 ô vuông ~230×230, separation 16)
            ├── Nexus (mở)
            ├── Spire (LockedOverlay)
            ├── Labyrinth (Locked)
            └── Contracts (Locked)
```

Rương đầy: tween xoay ±5°, loop. Locked ô: icon ổ khóa, modulate 0.5, bấm toast “Sẽ có ở bản sau”.

---

## 3. Idle Claim popup

Popup giữa màn, panel 920×1200, góc 24, Dim phía sau, bấm Dim = Đóng (không claim).

```
VBox margin 40, separation 20
├── Title "Rương Aether" 48px center
├── TimeLabel "Đã treo: HH:MM:SS" 32px
├── CapBar + Label "x / 12 giờ"
├── RewardGrid (Grid 2 cột): mỗi ô icon 96 + số 36
└── HBox
    ├── Close "Đóng" secondary 400×112
    └── Claim "Nhận" primary 400×112 (disabled nếu < 60s, text "Cần ít nhất 1 phút")
```

---

## 4. Campaign (`res://ui/screens/campaign/CampaignScreen.tscn`)

Chia 2 khối dọc:

**Trên (~520px):** `StageList` ngang (`ScrollContainer` horizontal) hoặc dọc nhỏ. Mỗi ải: thẻ 200×240, số thứ tự 40px, tên 22px (autowrap), trạng thái (khóa / check / “đang chọn”). Ải chọn: viền accent 4px.

**Giữa (~700px):** Panel chi tiết ải:

- Tên + “Lực chiến đề xuất: N” / “Đội bạn: M” (màu xanh/vàng/đỏ theo mốc 100%/80%/50%).
- 5 enemy portrait (2 hàng, cùng sơ đồ slot, nhỏ 140px).
- Thưởng: “Lần đầu: …” và “Đánh lại: …”.

**Dưới (~400px):** Đội hình người chơi.

```
Back row y nhỏ hơn (gần giữa):  3 slot 168×168, cách 24, canh giữa
Front row thấp hơn 40px, lệch giữa: 2 slot 168×168
```

Nhãn nhỏ “Trước” / “Sau”. Nút dưới cùng: `Auto sắp xếp` (secondary, 360×100) + `Chiến đấu` (primary, 560×100). Fight disabled nếu team trống.

Picker tướng: bottom sheet 1080×1200, list card, không hiện tướng đã ngồi slot khác trừ khi “đổi chỗ”.

---

## 5. Battle (`res://ui/screens/battle/BattleScreen.tscn`)

Full screen, **không** CurrencyBar, không BottomNav.

```
BattleScreen
├── Background ColorRect #1A1524
├── EnemyField  anchors top, height ~760, y start 80
│   └── 5 unit widgets (see below) laid out inverted (their front near center)
├── AllyField   anchors bottom, height ~760, y end above buttons
│   └── 5 unit widgets (front near center)
├── FloatTextLayer (Control, ignore mouse)
└── BottomButtons (height 160, y bottom 24)
    ├── SpeedButton "1x" 200×96  (cycles 1x→2x→4x→1x)
    ├── SkipButton 200×96
    ├── PauseButton 200×96 (optional)
    └── Surrender 200×96 (màu danger, confirm dialog)
```

**Unit widget** 168×200:

```
Portrait 168×140 (ColorRect theo placeholder_color + Label 1–2 chữ cái tên)
HPBar height 16, dưới portrait, fg #3CB371, bg #3A3040
EnergyBar height 8, fg #4C8DFF
Name 22px
Death: modulate xám, HP=0.
```

Sơ đồ tọa độ gợi ý (ally, gốc field):

```
Back:   (186, 40), (456, 40), (726, 40)
Front:  (321, 260), (591, 260)
```

Enemy: gương theo trục ngang, front gần giữa màn hơn back.

Floating text: spawn tại portrait mục tiêu, bay lên 60px trong 0.6s, fade. Crit: scale 1.4, màu `#FFC107`. Heal: `#3CB371`. Damage: `#F4F0E6`.

---

## 6. Result popup

Panel 920×1400 (scroll nếu bảng damage dài).

```
Title CHIẾN THẮNG / THẤT BẠI  64px
Time + survivors 28px
RewardGrid (nếu thắng)
Hint (nếu thua) 3 dòng, 26px, màu #B0A8C0
DamageList: HBox tên + bar tương đối + số, 8 tướng tối đa (5+5, chỉ hiện người có damage>0 cũng được)
HBox buttons:
  thắng và còn ải sau: [Về Hành quân] [Ải tiếp]
  thắng ải cuối / farm: [Về Hành quân] [Đánh lại]
  thua: [Về Hành quân] [Đánh lại]
Mỗi nút 400×112
```

---

## 7. Heroes List

```
TopFilter: horizontal chips height 72, scroll (All, 6 phe, 5 rarity) — không nhồi hết một hàng thì scroll.
SortButton góc phải "Sắp xếp: Lực chiến"
Grid 2 cột, cell ~500×280, separation 20, margin 24
NexusButton floating hoặc trên filter bên phải "Nexus"
```

`HeroCard` 500×280:

```
Portrait 200×240 trái
Phải: Name 32px, Lv, bậc, Power, sao rarity, badges, "+N" copy
```

---

## 8. Hero Detail

```
Header height 520
  BackButton 96×96
  LockButton 96×96
  Portrait 280×280 center
  Name 48, FactionBadge + Role, Power
  Level line: "Lv 10" hoặc "Lv 4 → Nexus 20" nếu khác nhau
TabBar height 88: Stats | Thăng bậc | Trang bị | Bảo vật | (Recycle icon)
TabContent expand
```

**Tab Stats:** bảng 2 cột tên chỉ số / số. Cụm nút `+1` `+10` `Tối đa` + preview “Tốn 1234 vàng, 400 bụi → Lv 11”. Lý do disabled hiện đỏ dưới nút.

**Tab Ascend:** sơ đồ bậc hiện tại → bậc sau, cap mới. Ô nguyên liệu (self copies + fodder). Auto-chọn. Nút Thăng bậc.

**Tab Gear:** sơ đồ người + 4 ô 4 hướng + artifact dưới. Bấm ô → Gear Equip.

**Tab Relic:** tên relic, level x/10, thanh, mô tả mốc, cost đá, nút Nâng.

Recycle: nút chữ “Tái chế bản sao” và “Giải tán tướng” cuối tab Stats, màu danger, confirm 2 bước (dialog + nút giữ “Giữ để xác nhận” 1s hoặc ô check “Tôi hiểu đồ đã tiêu không hoàn lại”).

---

## 9. Gear Equip

```
Top: hero mini + slot đang chọn 200
Compare: "ATK +20 → +32" nếu chọn món khác
List Scroll: hàng đồ 984×140 (icon, tên, rarity, stars, equipped_by)
Bottom: [Tháo] [Nâng sao (cost)] [Trang bị]  mỗi 300×112
```

---

## 10. Summon

```
Center portal placeholder 480×480
Scroll count 48px dưới portal
HBox: [×1] [×10]  mỗi 440×140
Links: "Đổi Ngọc lấy vé"  "Tỷ lệ"
PityLabel: "Còn X lần đến đảm bảo Elite" + dòng Legendary
```

**Reveal popup:** thẻ 400×560 (1x) hoặc lưới 2×5 (10x, thẻ nhỏ 180×260). Lật lần lượt. Nút Skip hiện ngay với 10x. Badge NEW / +1 copy. Nút “OK” sau khi lật hết.

**Rates panel:** list % đúng data, + 2 câu pity (copy mục 11.6).

**Shop gem→scroll:** dialog “100 Ngọc = 1 vé”, nhập số hoặc nút +1/+10, confirm.

---

## 11. Nexus

```
Title "Nexus Cộng hưởng"
NexusLevel 72px "Cấp Nexus: 42"
Help 2 dòng 24px (đúng copy mục 17.13)
AnchorRow: 5 portrait 160, nhãn "Neo"
SlotGrid: 5×2, ô 180×180, ô khóa có text điều kiện "Clear ải N"
```

Picker giống campaign.

---

## 12. Settings

Từ Hub icon bánh răng 80×80 góc title.

```
Đổi tên (LineEdit + Lưu)
Thời điểm save gần nhất (local time)
New Game (2 confirm)
Debug (if OS.is_debug_build()):
  + vàng / bụi / scroll / đá
  Giả lập idle 12h
  Thắng ải đang chọn
  Thêm 2 COMMON fodder mỗi phe
```

---

## 13. Components bắt buộc

| Component | Path | Ghi chú |
|---|---|---|
| CurrencyBar | `ui/components/CurrencyBar.tscn` | mục 1 |
| HeroCard | `ui/components/HeroCard.tscn` | list + picker |
| HeroPortrait | `ui/components/HeroPortrait.tscn` | dùng lại battle / slots |
| RarityStars | `ui/components/RarityStars.tscn` | 1–5 chấm màu rarity |
| FactionBadge | `ui/components/FactionBadge.tscn` | hình học + chữ |
| ConfirmDialog | `ui/components/ConfirmDialog.tscn` | title, body, Hủy / OK |
| Toast | `ui/components/Toast.tscn` | 2s, 4 kind màu |
| RewardPopup | `ui/components/RewardPopup.tscn` | lưới icon + OK |
| ProgressBarLabeled | `ui/components/ProgressBarLabeled.tscn` | bar + text giữa |
| LockedOverlay | `ui/components/LockedOverlay.tscn` | ổ khóa mờ |
| BootError | `ui/screens/hub/BootError.tscn` | Label lỗi DataRegistry |

---

## 14. Điều hướng kỹ thuật (`AppRoot.gd`)

```gdscript
func go(screen_id: String, payload: Dictionary = {}) -> void
func push(screen_id: String, payload: Dictionary = {}) -> void  # detail / gear
func pop() -> void
func show_overlay(overlay_id: String, payload: Dictionary = {}) -> void
func hide_overlay() -> void
func toast(message: String, kind: String = "info") -> void
```

Tab đổi: `go()` reset stack. `EventBus.screen_changed.emit`.

Preload hoặc `load()` scene khi đổi tab, cache instance tab chính (Hub, Heroes, Campaign, Summon) để không mất vị trí scroll. Màn con (Detail) có thể queue_free khi pop.

---

## 15. Trạng thái rỗng / lỗi / loading

- List đồ trống: “Chưa có trang bị — đánh Hành quân để nhận.”
- Không đủ vé: nút ×1 xám, toast khi bấm.
- Data boot fail: chỉ hiện BootError, không hiện Hub.
- Save hỏng: toast warning rồi new profile (spec 14.4).

Không hiện spinner mạng (không có mạng).

---

Hết wireframe. Kết hợp với mục 17 của spec chính khi có xung đột kích thước nhỏ — ưu tiên đủ thành phần và luồng bấm, không ưu tiên px cứng.
