# MEOW BASTION — Xưởng và tiến trình

Bấm `../CHOI_BAN_UIUX.cmd` hoặc chạy `npm.cmd run dev` rồi mở **http://localhost:5174**. Dùng Node.js, Chrome và Three.js đã có trên máy, không cần tải thư viện. Build tĩnh ở `dist/` có thể chạy bằng một server tĩnh.

Đây là bản độc lập trong `gamedesign/playable`, vùng được phép ghi của phiên. Game gốc và shortcut ở `tank_cat` vẫn dùng bản gốc tại 5173. Hai server chỉ phục vụ file trên máy. Launcher tái sử dụng đúng server, không mở thêm bản trùng.

## Chơi

1. Mở **Rương** để xem/nhận tài nguyên tích lũy, tối đa 12 giờ. Cần ít nhất một phút; không tự nhận khi mở game.
2. Trong **Đội mèo**, bấm một mèo để lên cấp +1/+10/Max, thăng bậc bằng nguyên liệu đã xác nhận, mặc đồ, nâng bảo vật hoặc quản lý bản sao. Tướng khóa, neo, trong đội/cộng hưởng được bảo vệ khỏi tiêu hao cả instance.
3. **Triệu hồi** dùng vé. Ngọc đổi thành vé với tỷ giá hiển thị. Có công bố tỷ lệ, bảo hiểm Elite+ 10 lần và Legendary+ 40 lần; quay trùng thành bản sao.
4. **Cộng hưởng** lấy cấp thấp nhất của 5 tướng cấp gốc cao nhất. Tướng trong ô hưởng cấp này, giới hạn theo bậc, không lên cấp gốc khi còn trong ô. Chưa đủ 5 neo thì chưa được tăng cấp cộng hưởng.
5. Trong **Chiến dịch**, chọn một trong 20 ải, chỉnh đội hỗ trợ tối đa 5 tướng và xuất kích. Thắng mới nhận thưởng/mở ải sau; đánh lại có thưởng nhỏ hơn. Đồ rơi lần đầu ở ải 2/3/4/6/7; cổ vật ở 5/10/15/20.

Đội hỗ trợ tăng chỉ số chung của xe và hỏa lực. Giữ nguyên chiến xa năm tầng, một mèo cố định mỗi tầng, mười bộ binh, luân phiên 40 vũ khí và sáu đợt quái mỗi trận. **Nâng cấp áp dụng khi bắt đầu trận mới**; tiếp tục trận đang dở giữ bộ chỉ số lúc xuất kích.

Điều khiển: A/D di chuyển, chuột khóa mục tiêu, 1–4/Q/E dùng kỹ năng, P tạm dừng. **Về xưởng** tạm dừng trận; có thể tiếp tục hoặc xuất kích lại theo ải đang chọn.

## Lưu và chạy nền

Hồ sơ tự lưu trong localStorage của trình duyệt/origin đang mở, có bản dự phòng và kiểm tra toàn vẹn. Tài nguyên, đội hình, tướng, đồ, pity và trạng thái RNG được lưu ngay khi thao tác thành công. Lỗi ghi không được coi là giao dịch thành công. Web Locks tuần tự hóa thay đổi giữa nhiều tab; kết quả trận chỉ được nhận một lần. Xuất/nhập bản lưu và chơi mới nằm trong Menu. Nếu cả hai bản lưu hỏng, game báo lỗi và cho xuất dữ liệu trước khi chủ động tạo hồ sơ mới.

**Đổi tab:** combat tiếp tục bằng timer; không vẽ WebGL ở tab ẩn hoặc màn xưởng. **Đóng tab/trình duyệt:** trận hiện tại không được khôi phục, nhưng rương vẫn tích theo thời gian thật. Mở lại hồ sơ, đánh trận mới và nhận rương. Không có server chiến đấu hay tài khoản đám mây. Mở bằng `localhost` và `127.0.0.1` là hai origin khác nhau; hãy dùng nhất quán hoặc chuyển hồ sơ bằng xuất/nhập.

## Áp dụng tài liệu thiết kế

Tất cả catalog, công thức kinh tế và mốc thưởng lấy từ `../afk_clone_spec`; `scripts/import-spec.mjs` nhập JSON và bổ sung năm món thưởng ải được chính phụ lục đề nghị. Có 12 định nghĩa tướng (8 chơi được, 4 nguyên liệu), 12 món đồ và 4 cổ vật. Tên/id từ spec được giữ; UI dùng sprite mèo sẵn có và phong cách thép xanh/vàng/xanh lá của `concept_UIUX`.

Triển khai bằng JavaScript/Three.js theo game hiện tại. Không chuyển sang Godot hoặc dùng AutoBattle 5v5. Hai mươi ải dùng trận sáu đợt hiện tại với hệ số khó tăng theo công thức campaign; không tạo hai mươi bản đồ hay thay quái bằng năm enemy template trong tài liệu.

`src/battle.js`, `data.js`, `visuals.js`, `enemy-animation.js` giữ nguyên từng byte so với nguồn lúc sao chép. `src/meta/tank-adapter.js` nhận snapshot, tăng máu xe, sát thương, giảm sát thương theo phòng thủ, nhịp bắn theo tốc độ và hút máu. Chí mạng quy về hệ số sát thương kỳ vọng, chuẩn hóa theo đội khởi đầu để không đổi combat ở ải 1. Bảo vật/cổ vật chuyển thành bonus trận; ult damage áp vào Bão băng/Mưa tên lửa, bonus hồi máu áp vào Sửa chữa. Năng lượng đầu trận đã đầy 600 nên bonus start-energy bị chặn ở trần. Các công thức ATB, hàng trước/sau và khắc chế phe của AutoBattle không được đưa vào combat xe tăng.

## Mã và kiểm tra

- `public/data/`: cân bằng, tướng, đồ, relic, artifact, pool, 20 ải; `tank_bridge.json` là cấu hình nối vào combat xe tăng.
- `src/meta/`: ví, rương, save, tướng/stat, đồ, gacha, campaign, kho trạng thái và adapter. UI chỉ gọi service.
- `src/meta-ui.js`, `meta.css`: giao diện tiến trình. `workshop.js`/`workshop.css`: khung xưởng và kho 40 vũ khí gốc.

```text
npm.cmd run check
npm.cmd run check:ui
node scripts/check-background.mjs
node scripts/check-game-animation.mjs
npm.cmd run build
```

Các kiểm tra trình duyệt cần server 5174. Script dùng Chrome/Playwright đã cài trên máy với đường dẫn tuyệt đối; bài so sánh nguồn cần cấu trúc thư mục hiện tại. Ảnh QA ở `test-results/progression-*.png` và `workshop-*.png`.

Đã kiểm tra lưu qua reload/xuất/nhập, thiếu tài nguyên không trừ một phần, nhận rương/kết quả không trùng, hai ngưỡng pity, chi phí/cap cấp, gear exclusive, thăng bậc, relic/artifact, Nexus, lỗi lưu, phục hồi dự phòng và trận thắng WebGL thật mở ải tiếp. Đã xem giao diện desktop 1600×900, ngang 900×506 và dọc 390×844. Fixture combat khởi đầu vẫn thắng 532 giây, 121 quái, đủ 20 loài và 40 vũ khí, còn 14.758 HP.

Chưa chơi thủ công/cân bằng toàn bộ 20 ải hoặc đo FPS. Không triển khai mode phase 2, IAP, tài khoản hay đồng bộ đám mây.
