# Tổng kết phiên 25/11/2026 – Gameplay và bộ ảnh nhân vật mới

Phạm vi: chỉ trong `Sources/CatTanks`, không sửa file nào của MegaTank.

## Việc đã làm (theo thứ tự)

| # | Việc | Kết quả | Nhật ký chi tiết |
|---|---|---|---|
| 1 | Đọc tài liệu thiết kế gameplay | Nắm cơ chế trận: pháo đài, bộ binh, module, thú cưng, bắt quái, kỹ năng | – |
| 2 | Rà soát gameplay và cân bằng | Phát hiện: đội khởi đầu cấp 1 qua được ải 1–15, quái gần như không gây sát thương | [GAMEPLAY_RA_SOAT](2026-11-25_GAMEPLAY_RA_SOAT.md) |
| 3 | Chạy thử từng quân, từng súng, từng quái | Viết bản mô phỏng Node của `Battle.cs` (`Tools/BalanceSim`), đưa ra phương án cân bằng v1 | [GAMEPLAY_CHIEN_DAU](2026-11-25_GAMEPLAY_CHIEN_DAU.md) |
| 4 | Áp cân bằng v1 vào game | Commit `a52b0fb2` | như trên, mục 5 |
| 5 | Đọc bộ ảnh `nhanvat_dodacmoi` | 6 tấm lưới: mèo, chim, súng, mũ áo, giày, tháp pháo | [ASSET_NHAN_VAT_MOI](2026-11-25_ASSET_NHAN_VAT_MOI.md) |
| 6 | Đưa ảnh mới vào game | Commit `dc81cf37` | như trên |

## Cân bằng v1 (commit `a52b0fb2`)

- **Quái mạnh hơn và đánh vào pháo đài:**
  - HP và ATK theo ải được tính lại.
  - Quái chạy nhanh gấp 2,5 lần.
  - Tầm đánh của quái là 480–620.
- **Bắt quái theo cơ chế kiệt sức:**
  - Quái đang nhắm, khi đủ yếu, sẽ bị giữ ở 1% HP trong 4 giây để kịp ném lồng.
  - Tỉ lệ bắt trùm cơ bản là 12%.
- **Vũ khí:**
  - Chỉnh tầm bắn của tháp pháo, súng và module.
  - Sửa các súng quá yếu hoặc quá mạnh.
  - Thêm hệ số chống bay.
  - Sát thương cháy và độc tính theo sát thương súng.
- **Module** mạnh lên theo cấp và sao của tháp pháo gắn nó.
- **Kỹ năng hồi máu** hồi theo % máu tối đa.
- **Thưởng chuyên môn** của mèo là +30%.
- **Kết quả mô phỏng:**
  - Đội đúng cấp thắng cả 20 ải, còn 71–100% máu.
  - Đội thiếu cấp bắt đầu thua từ ải 13.

## Bộ ảnh mới (commit `dc81cf37`)

- **Mèo:**
  - Thay ảnh cat-0..9.
  - Thêm 10 mèo cat-10..19: Cà Phê, Than, Kem Dâu, Oải Hương, Bóng Đêm, Bạch Hổ, Báo Đốm, Lam, Sô-cô-la, Đào.
  - Mỗi mèo có trường `gun` riêng. Mèo bộ binh vẫn cầm súng.
- **Chim thay module (pack-0..19):**
  - Đổi tên thành tên chim, nhãn "CHIM CHIẾN"/"Chim".
  - Trong trận chim bay lơ lửng, không xoay theo hướng ngắm.
- **Đồ và vũ khí:** mũ và áo thay cả icon trang bị lẫn đồ mặc trên mèo trong trận. Súng, tháp pháo và giày cũng được thay.
- **Màn hình:** Doanh trại có nút lật trang để xem đủ 20 mèo.
- **Cất kho, chưa dùng** (`ArtSource/nhanvat_dodacmoi/unused`): 10 súng thừa, 19 đôi giày, 8 bộ mũ áo.
- **Công cụ cắt ảnh:** `Tools/ArtSlicer`.

## Trạng thái cuối phiên

- **Commit:** cả hai commit đã nằm trên `main`.
  - Ngay sau commit, Claude **không push**.
  - Lúc ghi nhật ký này, `main` ở máy đã khớp `origin/main` (2b0f562b). Người dùng đã tự push.
  - Remote đã có thêm commit mới (`a026a30c`) chưa kéo về.
- **Đã kiểm tra:**
  - Biên dịch thử bằng dotnet với bản giả lập UnityEngine.
  - Mô phỏng Node.
  - Ảnh xem trước.
  - Đủ 91 key ảnh trong catalog.
- **Chưa kiểm tra trong Unity:**
  - Chế độ classic.
  - Đấu trường.
  - Vị trí mũ áo trên mèo.
  - Chim bay trong trận.
  - Nút lật trang Doanh trại.
- **Còn mở:**
  - pack-17 (bomb) và pack-18 (crystal) đang dùng chim đổi tông màu tạm, cần ảnh chim thật.
