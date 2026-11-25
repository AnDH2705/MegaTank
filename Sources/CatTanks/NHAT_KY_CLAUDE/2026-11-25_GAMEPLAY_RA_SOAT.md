# Nhật ký Claude – CatTanks – 25/11/2026 – Rà soát gameplay và cân bằng

**Người làm: Claude (Claude Code).** Chỉ làm trên CatTanks (`Sources/CatTanks`), không sửa gì của MegaTank.

## Phạm vi và cách làm

- Đã đọc [Battle.cs](../Assets/CatTanks/Scripts/Battle.cs), [Profile.cs](../Assets/CatTanks/Scripts/Profile.cs), [Catalog.cs](../Assets/CatTanks/Scripts/Catalog.cs) và toàn bộ [catalog.json](../Assets/CatTanks/Resources/Data/catalog.json): 20 ải, 20 quái, 40 vũ khí, 10 mèo, 10 kỹ năng và các bảng balance.
- Máy này không có Unity Editor, nên chưa chạy được Play Mode. Để đo độ khó, mình viết bộ mô phỏng gần đúng bằng Node tại [Tools/BalanceSim](../Tools/BalanceSim):
  - `sim.js` mô phỏng các phần chính của `Battle.cs`: đội hình, công thức sát thương, lịch sinh quái, quái tiến lên và đánh, máy tự dùng Sửa chữa và Mưa tên lửa.
  - `tune.js` đổi hệ số trên một bản sao catalog, rồi chạy thử với "hồ sơ tiến độ dự kiến" và hồ sơ "thiếu cấp" (60% cấp dự kiến).
  - Chạy: `node tune.js ../../Assets/CatTanks/Resources/Data/catalog.json '{"hpBase":0.35}'`.
  - Giới hạn: bỏ qua né đạn, phần lớn hành vi đặc biệt của quái và kỹ năng ngoài 2 kỹ năng trên. Chỉ dùng để thấy xu hướng, không dùng để chốt số.
- **Không sửa code hay dữ liệu gameplay.** Phiên này chỉ thêm bộ mô phỏng và nhật ký này.

## Kết quả đo với số liệu hiện tại

| Hồ sơ | Kết quả |
|---|---|
| Hồ sơ mới, cấp 1, **không dùng kỹ năng** | Thắng ải 1–10 trong 10/10 lượt, pháo đài còn ~100% HP |
| Cấp 10, 2 tầng | Thắng ải 1–19. Ải 20 thua 10/10, nên độ khó dồn hết vào ải cuối |
| Tiến độ dự kiến và cả thiếu cấp | Thắng mọi ải 1–20, pháo đài 100% HP, Sửa chữa không lần nào phải dùng |

Kết luận: độ khó gần như phẳng, người chơi không cần nâng cấp hay dùng kỹ năng.

## Vấn đề tìm thấy

### A. Cấu trúc trận đánh (sửa số liệu thôi không giải quyết được)

1. **Tầm bắn phủ hết đường.** Phần lớn vũ khí có tầm 1450, từ x≈325 bắn tới x≈1775, trong khi quái sinh ở x 1590–1690. Quái vừa xuất hiện đã bị bắn, quái cận chiến và quái chậm (Rùa dung nham tốc độ 12, Tê giác 17) chết trước khi tới gần. Việc di chuyển pháo đài gần như vô nghĩa.
   - Thử giảm tầm còn 1000: boss (tầm 850) và quái tầm xa (Hươu 880, Đại bàng 800) đứng ngoài tầm đội mình, nên trận bị kẹt. Muốn đổi tầm thì phải chỉnh cùng tầm quái hoặc vị trí sinh quái.
2. **HP pháo đài gần như không có tác dụng.** `EnemyHit` đổ sát thương vào mèo ở tháp trước. Khi tháp chết, khẩu pháo đó ngừng bắn vĩnh viễn trong trận, nên thua thường là thua dây chuyền chứ không thua từ từ. Sửa chữa hồi 9000 HP, gần 70% pháo đài lúc đầu, cứ mỗi 8 giây với 80 năng lượng, trong khi hồi năng lượng được 128 sau 8 giây. Vì vậy có thể dùng Sửa chữa liên tục.
3. **Mưa tên lửa quá mạnh ở đầu game.** 18 quả × 190 sát thương cộng nổ lan, mà tổng HP quái ải 1 chỉ 3744. Năng lượng khởi đầu 200 đã đủ bắn ngay giây đầu.
4. **Bắt quái gần như không làm được.**
   - Ải 1 Kiến có 101 HP, một phát pháo của Mướp gây ~166. Quái không bao giờ dừng ở vùng 0–25% HP.
   - Khóa mục tiêu làm **mọi khẩu dồn bắn** vào chính con định bắt (`Target()` trả về mục tiêu khóa trước tiên).
   - Boss: thời gian ở dưới 10% HP chỉ khoảng 0,1–1,2 giây. Tỉ lệ bắt mỗi lần ném khoảng 1–2% (gốc 2%). Lồng trăng phải mua bằng gem.
5. **Boss quá yếu so với quái thường.** `bossHpBase` 0.018 so với `hpBase` 0.13, nên boss ở ải 20 có ~13.950 HP, **ít hơn** Bọ chúa giáp gai thường cùng ải (~14.100). Boss chết trong 5–10 giây.
6. **Mỗi ải chỉ có 1 đợt**, nên phần thưởng giữa đợt (+90 năng lượng) không bao giờ được dùng. Lịch sinh quái của mọi ải đều là 3 cụm ở 0/25/55 giây, chỉ khác số lượng.
7. **Xu rơi trong trận ít khi nhặt được.** Xu chỉ được nhặt khi pháo đài cách xu dưới 150px, mà quái chết ở xa bên phải. Thắng thì được cộng hết, thua hoặc rút lui thì mất hết.

### B. Lỗi hoặc dữ liệu không được dùng

8. Passive `fortressDefPct` (Bọ sừng đỏ, Tê giác) **có trong dữ liệu nhưng `Battle.cs` không đọc**. Pet loại này không có hiệu ứng.
9. Gacha (`Profile.Summon`) dùng số cứng: 51,5% COMMON / 36% RARE / 12,4% ELITE. Code **bỏ qua `balance.gacha.weights`**, và có `pityHigh` 40 nhưng không có bậc LEGENDARY để bảo hiểm.
10. Chỉ số **Power** nhân tốc độ ×2, nên tốc độ chiếm phần lớn, trong khi tốc độ ảnh hưởng chiến đấu rất ít (giảm hồi chiêu tối đa 30%). Power còn cộng **mọi mèo đang sở hữu**, kể cả mèo không ra trận. Hồ sơ mới có Power ~1050 trong khi ải 1 đề xuất 500, nên chỉ số này không giúp người chơi đoán độ khó.
11. HUD bắt boss: lỗi Codex ghi ngày 20/11 **đã được sửa**. HUD giờ dùng `CaptureHealthThreshold` và hiện `Shift+C`.

## Thử cân bằng bằng hệ số chung (chưa áp dụng)

| Bộ số | Kết quả |
|---|---|
| Hiện tại | Ải nào cũng thắng 100% |
| hpBase 0.35, atkBase 0.6, bossHpBase 0.08, Sửa chữa 4000, Mưa tên lửa 10 quả | Ải 1–3 vẫn dễ. Ải 5 hồ sơ dự kiến còn ~80% HP, thiếu cấp còn ~64%. Ải 10 dự kiến còn ~47%, thiếu cấp thua. Ải 15–20 thua 0/12 |
| Các bộ mạnh hơn | Các ải boss và ải cuối thua 0/12, ải thường vẫn dễ |

Kết luận: **hệ số chung không cân bằng được**. Đường độ khó bị răng cưa vì ba lý do: boss bắn 3 viên, quái tầm xa ở cuối game tập trung bắn tháp, và tháp chết thì mất khẩu pháo. Cần chỉnh riêng từng ải và đo bằng `Battle.cs` thật.

## Việc đề xuất tiếp

1. Viết bài test Unity EditMode chạy `Battle.Step` thật cho 20 ải với 2–3 hồ sơ chuẩn. Dự án đã có sẵn "20 mô phỏng hữu hạn". Chạy trên máy có Unity 2022.3.62f2.
2. Sửa cơ chế bắt quái:
   - Khi mục tiêu khóa đã đủ điều kiện bắt thì các khẩu tự chuyển sang con khác, hoặc có nút "giữ lửa".
   - Hoặc dùng ngưỡng HP tuyệt đối thay cho %.
   - Đặt tỉ lệ bắt boss khoảng 10–15% mỗi lần ném bằng Lồng trăng.
3. Boss: tăng `bossHpBase` lên khoảng 0.06–0.08 và cho boss có pha (hiện chỉ triệu hồi kiến).
4. Sửa chữa đổi từ 9000 cố định sang theo % HP pháo đài, khoảng 30%. Mưa tên lửa giảm còn 10–12 quả. Năng lượng khởi đầu thấp hơn chi phí Mưa tên lửa.
5. Làm cho vị trí có ý nghĩa: chỉnh tầm bắn của cả đội mình và quái, hoặc đẩy điểm sinh quái ra xa. Cho xu tự bay về pháo đài.
6. Đọc passive `fortressDefPct`. Cho gacha dùng `balance.gacha.weights`. Sửa công thức Power chỉ tính đội đang ra trận và giảm trọng số tốc độ.
