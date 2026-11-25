# Nhật ký Claude – CatTanks – 25/11/2026 – Thử nghiệm chiến đấu và phương án tối ưu

**Người làm: Claude (Claude Code).** Chỉ làm trên CatTanks, không sửa gì của MegaTank. Đây là phần tiếp theo của [2026-11-25_GAMEPLAY_RA_SOAT.md](2026-11-25_GAMEPLAY_RA_SOAT.md).

**Chưa sửa code hay dữ liệu của game.** Mọi kết quả dưới đây đến từ mô phỏng.

## Công cụ

Thư mục [Tools/BalanceSim](../Tools/BalanceSim):

| File | Vai trò |
|---|---|
| `battle.js` | Port gần như từng dòng phần chiến dịch của `Battle.cs`: đường đạn, va chạm quét, nổ lan, xuyên, chuỗi sét, đốt và độc, giáp, hành vi riêng của 20 loại quái, kỹ năng, lịch sinh quái. Có thêm phần đếm sát thương theo nguồn và sát thương thừa (overkill). |
| `lab.js` | Các bài thử: `weapons`, `cats`, `monsters`, `skills`, `stages`, `capture`. |
| `proposal.js` | Phương án v1: sửa dữ liệu và bật các luật mới. |

Cách chạy:
- Số liệu hiện tại: `node lab.js stages`
- Phương án v1: `PROPOSAL=1 node lab.js stages`

Giới hạn: không có đồ họa, AI người chơi đơn giản, không có pet. Kết quả dùng để so sánh tương đối. Số cuối cùng vẫn phải chốt bằng chơi thử trong Unity.

## 1. Kết quả với số liệu hiện tại

### Ải và quái
- **Đội khởi đầu ở cấp 1, không nâng cấp gì, vẫn qua ải 1–15.** Đội này gồm 1 tầng Mướp + pháo `turret-0`, cùng Khói và Bông. Ải 20 là bức tường duy nhất.
- Với đội tiến độ bình thường: thắng 20/20 ải, **pháo đài 100% HP, không con mèo nào chết, không lần nào phải dùng Sửa chữa**.
- Bài thử quái ở mức ải 10: **cả 20 loại quái gây đúng 0 sát thương.** Mọi con đều chết khi còn cách pháo đài ≥ 890px. Quái tầm xa có tầm 680–880 cũng chưa kịp vào vị trí bắn.
- Nguyên nhân:
  - Tầm bắn của đội mình là 1450, phủ cả điểm sinh quái (x 1590–1690).
  - Quái đi rất chậm (12–58 px/s) trên quãng đường khoảng 1.100px.

### Súng: DPS hữu ích trong hỗn hợp quái, khoảng chênh lệch tới 6 lần

| Nhóm | Mạnh nhất | Yếu nhất |
|---|---|---|
| Tháp pháo | `turret-7` sniper **1130**, `turret-4` laser 1024 | `turret-9` drone **179**, `turret-5` flame 192 |
| Súng bộ binh | `gun-8` arrow 474, `gun-0` rapid 317 | `gun-9` drone **89**, `gun-6` flame 100 |
| Module | 46–173 | — |

- Sniper đứng đầu gần mọi tình huống: xuyên giáp, xuyên 3 mục tiêu, tự ưu tiên boss và quái có giáp. Với quái giáp nó mạnh gấp 6,7 lần pháo khởi đầu.
- Flame, shotgun và claw có tầm ngắn, nên gây **0 sát thương** lên nhóm quái tầm xa.
- Đốt và độc gây sát thương cố định 24 và 30/giây, không tăng theo cấp, nên vô dụng về sau.
- Module (`pack-*`) **không tăng theo bất kỳ chỉ số nào**. Cuối game chúng chỉ bằng khoảng 10% một tháp pháo.

### Mèo (cấp 20)
- **Vai trò chuyên môn bị ngược.** Mèo chuyên bộ binh lại cầm súng yếu:

  | Mèo | DPS khi làm bộ binh |
  |---|---|
  | Mây | 89 |
  | Bông | 121 |
  | Mun | 145 |
  | Khói | 164 |

  Trong khi mèo chuyên vận hành pháo làm bộ binh lại mạnh hơn: Mochi 370, Tam Thể 305.
- Mun (ELITE, chuyên bộ binh) lại là **người vận hành pháo tốt nhất** (478) nhờ ATK cao.
- Thưởng chuyên môn chỉ +15%, nên không ảnh hưởng tới quyết định xếp đội.

### Kỹ năng và bắt quái
- Kỹ năng: ở ải 12, đội **không mang kỹ năng nào vẫn thắng 10/10**. Kỹ năng hiện là thứ thừa.
- Bắt quái: khi đã khóa mục tiêu, cả đội dồn bắn nên con đó chỉ ở vùng ≤25% HP **khoảng 0,1–0,9 giây**. Boss chỉ ở dưới 10% HP khoảng 0,05–0,4 giây.
- Di chuyển pháo đài **không né được đạn**. Đạn quái bay ngang, còn vùng trúng đạn di chuyển cùng pháo đài, nên di chuyển chỉ có tác dụng nhặt xu.
- **Thua kiểu vách đá.** Mèo ở tháp đỡ mọi đòn. Tháp chết thì mất khẩu pháo đến hết trận, nên chỉ cần qua 1–2 ải là kết quả chuyển từ "100% HP" sang "thua 10/10". Đổi hệ số chung không làm mượt được đường này.

## 2. Phương án v1 (đã chạy thử trong mô phỏng)

### Luật mới (cần sửa code)

| # | Thay đổi | Lý do và kết quả mô phỏng |
|---|---|---|
| L1 | **Đòn đánh trúng tháp trừ vào HP pháo đài.** Mèo chỉ bị choáng hoặc đẩy lùi, không chết vĩnh viễn. | Bỏ được kiểu thua vách đá. HP pháo đài giảm dần qua từng ải, và Sửa chữa có giá trị. |
| L2 | **Giữ lửa khi bắt:** mục tiêu đã khóa mà ≤25% HP (boss <10%) thì các khẩu tự chuyển sang con khác. | Kết hợp với L3. |
| L3 | **Kiệt sức 4 giây:** trong 4 giây đó, mục tiêu đã khóa không bị hạ dưới 1% HP. | Cửa sổ bắt tăng từ ~0,2 giây lên **4,5–6,9 giây**, kể cả boss. 87–100% lần khóa có cửa sổ ≥ 1 giây. |
| L4 | Đốt và độc gây sát thương theo % sát thương vũ khí (35%), không còn số cố định. | Súng gây đốt/độc vẫn có tác dụng về cuối game. |
| L5 | Module tăng theo cấp và sao của tháp gắn nó. | Module không còn bị bỏ xó cuối game. |
| L6 | Hệ số với quái bay: rapid, drone ×1,5; double, arrow ×1,3; rocket ×1,2. Ngược lại mortar, bomb ×0,4; flame, shotgun, claw ×0,6; shell ×0,7. | Tạo lựa chọn đối kháng giữa đội chống bay và đội chống bộ. |
| L7 | Thưởng chuyên môn từ +15% lên +30%. | Xếp đúng vai có ý nghĩa. |
| L8 | Sửa chữa hồi 30% HP pháo đài (+10% mỗi cấp kỹ năng), thay cho 9000 cố định. Năng lượng khởi đầu 200 → 120. | — |

### Sửa dữ liệu (`catalog.json`)

**Tầm bắn đội mình**
| Loại | Tầm mới |
|---|---|
| Tầm xa: sniper, laser, mortar, rocket | 1150–1200 |
| Tiêu chuẩn: shell, double, rapid, tesla, drone | 1000–1050 |
| Súng bộ binh | 900–1100 |
| Flame, shotgun | 650–720 |

**Tầm bắn quái**
| Quái | Tầm mới |
|---|---|
| Nhện, Bọ cạp, Khỉ băng, Bướm | 500–560 |
| Bọ tinh thạch | 480 |
| Đại bàng | 560 |
| Hươu | 620 |
| Bọ chúa | 540 |
| Boss | 480 |

**Tốc độ quái ×2,5.**

**Vũ khí**
| Vũ khí | Thay đổi |
|---|---|
| `turret-7` sniper | sát thương 200, chu kỳ 3,0 giây, xuyên 2 (đang 230 / 2,6 / 3) |
| `turret-4` laser | 115 → 100 |
| `turret-9` drone | 85 × 3 viên, chu kỳ 2,6 giây (đang 70 × 2, 3,8 giây) |
| `turret-5` flame | 28 → 40 |
| `turret-0` | nổ lan 90 → 110 |
| `gun-1` | 9 → 13 |
| `gun-2` | chu kỳ 3,6 → 2,6 giây, nổ lan 130 |
| `gun-4` | 24 → 34 |
| `gun-6` | 18 → 28 |
| `gun-8` | 95 → 70 |
| `gun-9` | 45 → 70, chu kỳ 1,3 giây |

**Kỹ năng:** Mưa tên lửa từ 18 × 190 thành 10 × 240.

**Hệ số ải:**

| Hệ số | Hiện tại | Đề xuất |
|---|---|---|
| `hpBase` | 0.13 | 0.8 |
| `atkBase` | 0.17 | 1.6 |
| `growth` | 1.12 | 1.075 |
| `bossHpBase` | 0.018 | 0.05 |

### Kết quả mô phỏng v1

- **Đường độ khó**
  - Đội tiến độ bình thường thắng 20/20 ải, pháo đài còn **64–100% HP**. Trận dài 55–125 giây, so với 18–45 giây hiện tại.
  - Đội thiếu cấp (60%) bắt đầu thua từ ải 13, nên nâng cấp có ý nghĩa trở lại.
- **Kỹ năng ở ải 12**
  - Không mang kỹ năng: 0/10.
  - Tiếp viện 10/10, Mèo cảm tử 10/10, Lá chắn 9/10, Bão băng 6/10, Không kích 5/10, Mưa tên lửa 3/10, Sửa chữa 1/10.
  - **Drone 0/10 và Mồi nhử 0/10.**
- **Súng** (DPS trong hỗn hợp quái)
  - Tháp pháo còn chênh 231–681, giảm từ mức 6 lần xuống khoảng 3 lần.
  - Súng bộ binh còn chênh 148–376.
- **Mèo:** DPS khi vận hành pháo nằm trong khoảng 187–285. Mèo chuyên bộ binh giờ mạnh hơn khi làm bộ binh.

### Còn lệch, cần chỉnh tiếp
- `turret-5` flame giờ mạnh nhất nhóm tháp (681) vì quái đã tới gần. Nên giảm còn khoảng 32.
- `turret-4` laser 538 hơi cao.
- `gun-9` drone vẫn yếu nhất (148).
- Kỹ năng Drone dùng `gun-9` gốc, không tăng theo cấp, nên cần cho tăng theo cấp kỹ năng.
- Mồi nhử có 800 HP cố định, cần tăng theo ải.
- Sửa chữa 30% vẫn yếu. Nên thử 40% hoặc hồi dần trong 4 giây.
- Ải boss (5/10/15/20) chưa phải đỉnh khó. Các ải 7–9, 11, 14 vẫn là "thung lũng" dễ. Cần chỉnh riêng từng ải chứ không chỉ dùng hệ số chung.

## 3. Đề xuất làm game hấp dẫn hơn (ngoài cân bằng số)

1. **Cho di chuyển có ý nghĩa.**
   - Đổi đạn của quái pháo binh (Nhện, Bọ cạp, Khỉ băng, boss) thành đạn cầu vồng, có vòng cảnh báo trên mặt đất. Người chơi có thể né bằng cách tiến hoặc lùi.
   - Xu rơi gần pháo đài hơn, tạo lựa chọn "tiến lên nhặt xu hay đứng an toàn".
2. **Chủ đề cho từng ải và nhiều đợt mỗi ải**, ví dụ:
   - đợt "bầy" (nhiều quái yếu, cần nổ lan);
   - đợt "giáp" (cần xuyên giáp);
   - đợt "trên không" (cần súng chống bay);
   - đợt "pháo binh" (cần né và cần Lá chắn).

   Hiện mọi ải đều là 1 đợt với cùng lịch 0/25/55 giây, và phần thưởng giữa đợt chưa bao giờ được dùng.
3. **Boss có 3 pha**, ví dụ:
   - 100–60% HP: bắn quạt 3 viên;
   - 60–30%: lao tới và gọi Ong;
   - dưới 30%: điên cuồng, kèm cảnh báo trước đòn lớn.

   Thắng boss mở cửa sổ bắt bằng Lồng trăng, với tỉ lệ bắt boss 10–15% mỗi lần ném, thay cho 2%.
4. **Phản khắc rõ ràng, hiển thị cho người chơi.**
   - Trước trận hiện "Ải này nhiều: quái bay / quái giáp".
   - Súng có nhãn vai trò: Chống bầy, Xuyên giáp, Chống bay, Khống chế.

   Khi đó việc xếp tháp và xếp đội trở thành quyết định thật, không còn kiểu "sniper cho mọi ải".
5. **Chọn nâng cấp giữa các đợt**: chọn 1 trong 3 buff tạm thời cho trận đó, ví dụ +20% tốc bắn cho khẩu rapid, đạn mortar để lại vũng lửa, Lá chắn giảm 30% hồi chiêu. Đây là cơ chế phổ biến của dòng thủ thành và survivor, giúp mỗi lần chơi lại có khác biệt.
6. **Khóa mục tiêu thành công cụ chiến thuật.** Hươu hồi máu và Bọ chúa gọi quái phải được ưu tiên hạ trước. Nên hiển thị biểu tượng "nguy hiểm" để người chơi muốn khóa chúng.

## 4. Thứ tự làm đề xuất

1. L2 + L3 (bắt quái) và L1 (sát thương vào pháo đài): sửa nhỏ trong `Battle.cs`, tác động lớn nhất.
2. Sửa dữ liệu tầm bắn, tốc độ, vũ khí, hệ số ải theo bảng trên. Làm trên nhánh riêng và chơi thử trong Unity.
3. L4–L8, rồi chỉnh riêng từng ải theo chủ đề.
4. Boss nhiều pha, đạn cầu vồng né được, chọn buff giữa đợt. Đây là tính năng mới, cần thiết kế UI.

## 5. Đã áp dụng phương án v1 vào game (25/11, theo yêu cầu "làm thẳng vào")

Sửa trực tiếp trên `main`, **chưa commit**.

**Code**

`Battle.cs`:
- Đòn đánh trúng mèo ở tháp trừ vào HP pháo đài, giảm theo DEF của mèo đó. Mèo chỉ bị đẩy lùi hoặc choáng, không chết. (L1)
- Cơ chế bắt quái mới (L2 + L3):
  - Mục tiêu đang khóa sẽ không bị hạ hẳn nếu người chơi còn lồng phù hợp. Nó rơi xuống 1% HP và **kiệt sức** trong `monsters.capture.dazeSeconds` (4 giây): đứng yên, không đánh, và các khẩu tự chuyển sang bắn con khác.
  - Hết thời gian kiệt sức thì mục tiêu bị đánh bình thường trở lại.
  - Tỉ lệ bắt boss đọc từ `bossBaseChance` (0.12), không còn số cứng 0.02.
- Đốt và độc gây sát thương bằng `combat.dotScale` (35%) sát thương vũ khí, tối thiểu bằng mức cũ 24 và 30/giây. (L4)
- Module tăng theo cấp và sao của tháp gắn nó. (L5)
- Hệ số chống quái bay đọc từ `combat.antiAir`. (L6)
- Thưởng chuyên môn đọc từ `cats.specialtyBonus` (0.3). (L7)
- Sửa chữa hồi `healPct` (30%) HP pháo đài, tăng 10% mỗi cấp kỹ năng. Năng lượng khởi đầu tính bằng `loadoutEnergy.startFrac` (0.2, tức 120). (L8)
- Tốc độ quái nhân `enemySpeedMult` của đợt. Chỉ áp dụng cho chiến dịch và đấu trường; chế độ Thử thách trăng giữ tốc độ cũ.

`Profile.cs`: thưởng chuyên môn đọc từ catalog.

`Catalog.cs`: thêm trường `Wave.enemySpeedMult` và `SkillDef.healPct`.

UI (chỉ đổi chữ):
- `StudioBattleHUD.cs`: gợi ý "Kiệt sức · còn X giây để ném lồng".
- `CatTanksGame.cs`: dòng hướng dẫn bắt quái ở màn chuẩn bị xuất kích.

**Dữ liệu `catalog.json`:** đúng bảng ở mục 2, gồm:
- tầm bắn và chỉ số vũ khí;
- tầm bắn quái;
- hệ số các ải: `hpBase` 0.8, `atkBase` 1.6, `growth` 1.075, `bossHpBase` 0.05, tốc độ ×2,5;
- Mưa tên lửa 10 × 240;
- các khóa balance mới: `combat.dotScale`, `combat.antiAir`, `capture.dazeSeconds`, `capture.bossBaseChance` 0.12.

**Kiểm tra**
- Biên dịch thử `Battle.cs`, `Catalog.cs`, `Profile.cs`, `GamePerformance.cs` bằng `dotnet` (bản giả lập UnityEngine và Newtonsoft 13): **build succeeded**. `StudioBattleHUD.cs` và `CatTanksGame.cs` chưa biên dịch thử, nhưng chỉ đổi chuỗi và gọi 2 hàm `internal` mới.
- Chạy `LIVE=1 node Tools/BalanceSim/lab.js stages` với catalog thật cho kết quả trùng phương án v1:
  - đội đủ cấp thắng 20/20 ải, còn 71–100% HP;
  - đội thiếu cấp thua từ ải 13;
  - cửa sổ bắt quái 4,5–7 giây.
- **Chưa chơi thử trong Unity.**

**Rủi ro cần xem khi chơi thử**
- Chế độ Thử thách trăng (classic) dùng chung bảng vũ khí và quái, nên tầm bắn bị đổi theo. Chế độ này chưa được mô phỏng.
- Đấu trường nhận theo hệ số HP, ATK và tốc độ mới của các ải.
- Mèo không còn chết trong chiến dịch, nên thanh HP của mèo trên tháp luôn đầy.
- Khóa mục tiêu để dồn hỏa lực vào quái nguy hiểm sẽ chậm hạ nó 4 giây khi người chơi còn lồng. Nếu thấy khó chịu, có thể chỉ cho kiệt sức khi bấm một nút "chế độ bắt".
