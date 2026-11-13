# Phụ lục 03 — UI: Hub kiểu AFK (3 chỗ), HUD trận classic giữ nguyên

Game đã có HUD landscape khớp `Moonlit Cat-Dog Tank Battle.png`. **Classic không vẽ lại.** Hub meta = layout 3 tab kiểu AFK Arena, **luôn landscape**. Trận loadout dùng HUD tối giản (mục 1). Palette: `#041828`, `#ffd278` / `#ffca64`, `#4ee9ff`, `#4ac835`, `#3fa5d5`, Nunito + Barlow Condensed. Tái sử dụng `.square`, `.primary`, `dialog`, `#toast`, `.overlay`.

---

## 1. HUD trận

Giữ nguyên (xem `index.html`):

- `#pause`, `#coins` + “CHIẾN LỢI PHẨM”, `#kills` + “QUÁI ĐÃ HẠ”
- “CHIẾN DỊCH …”, `#wave-label`: Hành quân hiện **ẢI N** (1 đợt, có thể ẩn `#wave-dots`); classic (nếu mở sau) mới “ĐỢT x / 6”
- `#codex` BÁCH KHOA, `#sound`, `#fullscreen`
- “PHÁO ĐÀI MÈO” `#hp-text` / `#hp-fill`; thanh quái `#enemy-title` `#enemy-fill`
- `#aim-label` NGẮM TỰ ĐỘNG / ĐANG KHÓA MỤC TIÊU
- `#bank-label` — classic: `CẤU HÌNH VŨ KHÍ 01 · n/40`; **meta:** `CẤU HÌNH TÙY CHỈNH` (chỉ đổi text trong `hud()`, không đổi vị trí)
- Năng lượng `#energy-text` dùng `model.energy` / `model.energyMax` (classic 600). Thanh `#hp-text` dùng `model.maxHp`, **không** cứng `48.000`.
- **Classic:** giữ skill `data-skill="rally|heal|freeze|drone|barrage|shield"`, phím 1–4 Q E, **không đổi**.
- **Loadout** (`#game.mode-loadout`) — HUD **tối giản**: ẩn `.skillbar` + `.specials`, ẩn `#bank-label` và `#wave-dots`, có thể ẩn `#aim-label`. Giữ `#pause`, “PHÁO ĐÀI MÈO” `#hp-text`/`#hp-fill` (dùng `maxHp`), thanh quái, `#wave-label` = ẢI N, năng lượng theo `energyMax`. Hiện `#skillbar-loadout` **4 thẻ** (phím 1–4) + `#catch` (BẮT, C) tách riêng, không tốn energy, số lồng, nháy khi `catchable`. Ẩn bắt khi hết lồng / PvP. Có thể giữ coins/kills gọn. **Không** portrait, không bottom-nav trong trận.
- `#rotate-hint`, `.touch-movement`, `#book` 3 tab, overlay `#start`

Khi vào Hub: thêm class `#game.mode-hub` — ẩn `#hud` trận (hoặc `visibility`) và hiện `#hub`. Khi xuất kích: ngược lại, `#overlay` dùng như hiện tại cho classic; meta có thể skip intro dài, vẫn hiện `#hud`.

---

## 2. `#hub` — 3 chỗ, landscape

Toàn màn Hub, pointer-events tự quản. `#game.mode-hub` ẩn `#hud` trận, hiện `#hub`. Xuất kích: ngược lại. Meta skip intro dài.

```
#hub
├── .hub-top    ví (coins, dust, catXp, scroll, gem; arenaToken ở đấu trường)
│               + cấp tài khoản + settings + icon daily
├── .hub-main   nội dung 1 trong 3 chỗ
└── .hub-nav    [ Hành quân | Doanh trại | Thành mèo ]   ← bottom nav, 3 nút, chỉ Hub
```

Cảm ứng: nút đủ lớn. Portrait: `#rotate-hint`, không layout dọc.

### 2.1. Hành quân

Bản đồ ải **cuộn** (`STAGES.length`; v1 = 20). Node ải, huy hiệu TRÙM theo `stage.boss`. Ải chưa mở: khóa. Ải hiện tại nổi.

Hàng dưới bản đồ: **rương idle** (peek ≥ 60s, gồm catXp) cạnh nút lớn **Chiến đấu** (vào ải đang mở → màn trước trận 2.4). Claim rương = `dialog`. Panel daily quest (5 việc + bonus): mỗi dòng nút **Đi** gọi `hub.go(...)`. Classic “Thử thách Trăng” không phải CTA (mục phụ, sau v1).

### 2.2. Doanh trại

Một chỗ, **4 tab** (cuộn theo data):

**Mèo** — lưới card roster (sprite theo id, fallback). Badge `specialty`. Bấm card → chi tiết 2.5.

**Xe** — cột trái 5 tầng (tầng 5 trên, khớp xe trận). Tầng khóa mờ: “Mở ở cấp 20/40/60/80”. Phải: súng `TURRETS[].name`, cấp/sao, chỉ số đã gộp operator, 2 ô `PACKS`, gán mèo vận hành (picker loại trừ squad). Nâng HP pháo đài.

**Quái** — cùng màn `#den` (mục 4): sổ, mở lồng, cho ăn, 3 ô đồng hành. Toà Chuồng ở Thành mèo mở đúng màn này (`hub.go('town.den')` / `barracks.monsters`).

**Kỹ năng** — lưới `SKILL_DEFS` cuộn, khóa ghi điều kiện, nâng `skillCore` (preview cost/cd/hiệu lực). 4 ô xuất kích chỉnh trên màn trước trận.

### 2.3. Thành mèo

Bản đồ thị trấn (nền trăng/đá sẵn). Bấm toà:

- **Triệu hồi** — ×1 / ×10, tỷ lệ, pity, đổi ngọc, lật thẻ sprite sẵn.
- **Đấu trường** — rating, list mock, tấn công (AI tự skill), lưu phòng thủ, shop token.
- **Chợ** — refresh daily (tab coins/gem); tab token; hiện trần mua còn lại.
- **Chuồng quái** — màn `#den` (mục 4).

Toà/tab chưa mở: mờ, stamp **“Mở ở ải X”**, không vào. Mốc (TUNABLE): Quái/Chuồng sau ải 1; Kỹ năng sau ải 2; Thành mèo + Chợ sau ải 3; Triệu hồi sau ải 4; Đấu trường sau ải 5. Hành quân + tab Mèo/Xe có sẵn.

**Lần đầu mở:** tutorial chạm bắt buộc 2–4 bước (highlight + “Bấm đây”), `flags.tutorials[id]`, không hiện lại. Settings: tên, xóa save (2 confirm), `?debug`.

### 2.4. Một màn trước trận

Một màn, không wizard nhiều bước:

```
        [preset 1 2 3]     [Đội mạnh nhất]
                  (xe giữa, 5 tầng có mèo + súng)
         [5 ô bộ binh]     [3 ô quái đồng hành]
                    [ 4 ô kỹ năng ]
                    [  XUẤT KÍCH  ]
```

Bấm ô → picker (đúng specialty hiện +15%; sai chỗ cho phép, không phạt). Ô khóa ghi điều kiện. **Đội mạnh nhất:** auto gán power cao, ưu tiên đúng `specialty`, 3 pet mạnh, 4 skill (luôn gồm heal nếu đã mở) — TUNABLE, được stub rồi tinh. Preset lưu 3 loadout (tầng, bộ binh, skill, pet). Nút Xuất kích vào trận loadout (bắt nếu còn lồng).

### 2.5. Chi tiết mèo — một màn

Sprite lớn; tab **Chỉ số / Đồ / Bậc** (thêm tab thì cuộn). Nút to **Lên cấp** (preview `catXp` + `coins`). **Mặc nhanh** — mặc đồ tốt nhất đang trống/không tranh bị. **Nâng nhanh** — lặp lên cấp đến khi hết resource hoặc cap. Badge đúng/sai chỗ nếu đang gán. Lối gán tầng hoặc bộ binh.

### 2.6. Chấm đỏ và daily Đi

Chấm đỏ **chỉ** khi `canAfford` (đủ resource và còn nâng hợp lệ): lên cấp mèo/súng/skill, cho ăn, rương ≥ 60s, quà daily chưa nhận, shop slot miễn phí (nếu có). Không chấm “new” sau tutorial; không chấm khi thiếu tiền.

Daily: **Đi** deep-link `hub.go(target)` với target `campaign` | `prebattle` | `chest` | `barracks.cats` | `barracks.vehicle` | `barracks.skills` | `barracks.monsters` | `town.summon` | `town.arena` | `town.shop` | `town.den`.

### 2.7. Kết quả trận

Giữ overlay thắng/thua (kills, coins/đồ đã nhặt, thời gian; dòng “mèo/pet ngã” nếu có). **Thua vẫn hiện loot giữ được.** Classic 6 đợt nếu mở sau mới hiện 40 vũ khí. Thêm dòng thưởng ví / XP / ±rating / token **khi thắng**. **Chỉ thắng** và pending > 0 mới lật bài (flow 4.2). Thua: không lật, toast “Quái thoát khi thua trận”. “Về căn cứ” → tab Hành quân; “Chơi lại” giữ.

---

## 3. Cấm

Không bottom-nav trên màn trận, không nav portrait, không đổi “PHÁO ĐÀI MÈO”, không ẩn A/D, không HUD trận kiểu AFK dọc. Không xóa/sửa skillbar 6 ô classic — chỉ ẩn khi `mode-loadout`.

---

## 4. Chuồng quái (`#den`)

Mở từ toà **Chuồng quái** (Thành mèo) hoặc tab **Quái** (Doanh trại) — cùng `#den`. Palette / `dialog` như Hub. Không đè HUD trận.

### 4.1. Màn sổ / chuồng

```
Trái: túi Lồng thú / Lồng trăng / Thức ăn (số) + nút Mở lồng (chọn loại) + Đổi shop
Giữa: lưới sổ **cuộn** (không cứng 5×4). Số ô = `MONSTERS.length`. Sprite asset, viền rarity; thiếu art = fallback.
      chưa gặp: đen + ??? ; đã gặp chưa bắt: trắng xám + tên; đã bắt: màu + sao/cấp
Phải hoặc dưới: thanh `caught/MONSTERS.length` ĐÃ BẮT, mốc thưởng theo data (v1: 5/10/15/20)
Dưới: 3 ô Đồng hành (ô khóa ghi điều kiện). Bấm ô → picker đã bắt, không trùng loài.
```

Bách khoa tab quái: cùng quy tắc seen/caught; bấm loài đã bắt có thể `hub.openDen(speciesId)`.

### 4.2. Luồng mở lồng (reveal)

1. Bấm Mở (thiếu lồng → toast, không trừ). Có Mở ×10 nếu đủ (lồng thú).
2. Dialog full-center, nền tối: lồng lắc (CSS/tween 0.6–1s). Nút Bỏ qua hiện với ×10.
3. Lật thẻ: màu rarity (xám/lục/lam/tím/vàng-cam cho MYTHIC), sprite, tên. Mới: badge **MỚI!**; trùng: **+N mảnh**.
4. ×10: lưới, lần lượt 0.2s, Skip lật hết. OK đóng, cập nhật sổ.
5. Trùm (MYTHIC): hiệu ứng dài hơn, âm thanh nếu AudioEngine có hook (không bắt buộc file mới).

Không chặn `Battle` / không pause nhạc game nếu đang ở Hub (Hub không chạy trận).

### 4.3. Chi tiết / nâng cấp

Dialog hoặc panel: sprite lớn, element, IV 4 cột (số 0–15 + bar), cấp, sao, evo, chỉ số đồng hành đã tính, mô tả loài (lấy `MONSTERS[].description`).

Nút: **Cho ăn** +1 / +10 / Tối đa (preview chow); **Nâng sao** (cost mảnh); **Tiến hóa** (nếu đủ điều kiện). Disabled + lý do rõ.

Gán/gỡ đồng hành ngay trên trang này.

### 4.4. Picker đồng hành

Sheet: list đã bắt, ẩn loài đang ở ô khác, hiện power. Xác nhận thay thế.


### 4.5. Bắt trên sân (HUD trận)

Không vẽ lại topbar hay skillbar classic.

Loadout:
```
[thanh năng lượng 200/600]  [ 4 thẻ skill  1 2 3 4 ]  [ #catch  Lồng thú ×2  (C) ]
```

Classic (nếu mở sau): 6 ô cũ, không nút bắt.

Khi khóa quái `catchable`: nút nháy + icon chân trên sprite (visuals). Thiếu điều kiện: toast (`Chưa đủ yếu` / `Hết lồng` / `Cần Lồng trăng` / `Trùm còn trên 10% máu`). Lần đầu: tooltip một lần (`flags.sawCatchHint`).

**Onboarding:** hồ sơ mới 3 lồng + 15 thức ăn. Sau ải 1: tutorial ngắn mở Chuồng + “Hạ máu quái rồi ném lồng (C)”.


### 4.6. Máu đơn vị trên sân (chỉ trận loadout)

Không đổi layout topbar. Thanh máu nhỏ trên mèo tầng, bộ binh, đồng hành (`visuals.js`). Xe vẫn dùng `#hp-text` / `#hp-fill`. Đơn vị chết: ẩn hoặc đổ, không respawn trong trận. Skill Sửa chữa không hiện hiệu ứng trên xác.


---

## 5. Sẵn sàng mở rộng (UI)

Mọi list meta (mèo, ải, skill, sách, Chuồng, shop, gacha) phải cuộn/paginated theo `length` của data. Tab Bách khoa: nhãn `${WEAPONS.length} VŨ KHÍ` / `${MONSTERS.length} LOÀI` — không ghi chết 40/20 trong HTML (intro classic được giữ flavor). Thiếu sprite: fallback, không vỡ layout. Thêm 30 entry sau này không sửa `hub.js` / CSS ngoài có thể tinh chỉnh mật độ.
