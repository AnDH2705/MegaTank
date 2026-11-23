# Nhật ký phiên CatTanks — 20/11/2026

> **Phạm vi riêng của agent này là VFX.** Chỉ nhận việc về hiệu ứng chiến đấu CatTanks, gồm tham chiếu mỹ thuật VFX, asset hiệu ứng, mô phỏng, tích hợp render và đo hiệu năng VFX. UI, gameplay, cân bằng và art ngoài hiệu ứng thuộc phạm vi của các agent phụ trách tương ứng.

## Cập nhật cuối ngày — chuyển VFX Meow Bastion Web

- `C:\Users\trieu\Desktop\tank_cat\CHOI GAME.lnk` mở bản Meow Bastion chạy cục bộ bằng Three.js. Đã đối chiếu `src/visuals.js`, `src/battle.js` và ảnh QA gốc. Ba PNG rocket, mũi tên và drone đã được chép vào Unity, với SHA-256 trùng nguồn.
- Đã bật **Use Web Weapon Vfx** mặc định trong [Main.unity](Assets/CatTanks/Scenes/Main.unity). Trình render mới dùng cỡ đạn hiển thị 2×, chớp nòng, vòng xung kích, bốn lớp lửa và bốn lớp khói, laser, sét, hồi máu, đóng băng và hạt trạng thái. Hạt đi theo thời gian trận và có giới hạn 2.400. Sát thương, hitbox và tốc độ đạn giữ theo gameplay Unity. Bộ Unity Native cũ vẫn là đường dự phòng.
- Đã kiểm tra cú pháp C# và chạy trình render C# với lớp giả lập API Unity để xác nhận số lớp vụ nổ, kích thước đạn, pause, hết vòng đời và giới hạn hạt. Máy này thiếu Editor nên shader, hình ảnh trong trận và FPS còn cần kiểm tra trên Unity thật.
- Phần chuyển Web nằm trong commit `d612a5ba` (`background_effect`). Khi ghi mục này, `main` và ref theo dõi `origin/main` đều ở `c2a42c9e`. Xem [tài liệu VFX Web](VFX_Handoff/WEB_REFERENCE.md) để biết công thức và mốc so hình.

### Bắt đầu ngày mai

1. Mở project CatTanks bằng **Unity 2022.3.62f2** và kiểm tra Console sau khi import. Mở **CatTanks → Combat → Open Native Weapon Preview**, bấm Play. Dòng trạng thái mặc định hiện **Tham chiếu Web**. Dùng **W** để so với bộ Unity Native, **P** để dừng, **N** để bước frame và **S** để xem ở 0,25×.
2. Trong Play Mode, chạy **CatTanks → Combat → Validate Web Reference VFX**. Sau đó vào trận mới từ `Main.unity` để xác nhận đường render mặc định và kiểm tra lúc bắn dồn.
3. So ảnh Unity với cảnh `tank_cat/scripts/preview-effects.mjs` tại mốc **0,08 giây** ở **1600×900** và **900×506**. Ưu tiên nhìn cỡ và góc rocket, độ sáng lúc nổ, nhịp khói tan và độ rõ mục tiêu. Đo FPS, p95/p99 và overdraw trên cùng cấu hình sau khi chốt hình.

## Kết quả

- Đọc [nhật ký 19/11](NHAT_KY_2026-11-19.md) và bắt đầu từ Gatling, đại bác và chùm tên lửa. Hoàn thành [danh mục hiệu ứng chiến đấu](VFX_Handoff/DANH_MUC_VFX.md) cho 40 vũ khí, 20 thú đồng hành, 20 hành vi quái và 10 kỹ năng. Bộ thử có 58 mẫu, phân theo frame-by-frame, particle, sprite có vệt và hình học động.
- Làm [phòng thử offline](VFX_Handoff/Preview.html), rồi dựng [năm vòng mô phỏng](VFX_Handoff/Nam_Vong/So_Sanh.html) trên cùng một timeline. Các vòng tăng kích thước đạn, bổ sung đuôi, thay hình va chạm, tách ba đường bay rocket và tinh chỉnh đợt bắn dồn. [Vòng 5](VFX_Handoff/Nam_Vong/Vong_05.mp4) dài 12 giây, 60 fps, có âm thanh. Nguồn hình, các quyết định mỹ thuật và prompt ImageGen nằm trong [tài liệu tham khảo](VFX_Handoff/Nam_Vong/THAM_KHAO.md).
- Tích hợp VFX Unity native cho **Gatling, đại bác và tên lửa**. Dùng SpriteRenderer flipbook, LineRenderer, Particle System bursts, trails, noise, sub-emitter, shader additive, vòng xung lực và rung camera. [Scene chính](Assets/CatTanks/Scenes/Main.unity) đã bật `useNativeWeaponVfx: 1` và tạo trình render mới khi bắt đầu trận. Sprite prototype là đường dự phòng.
- Bộ 58 mẫu là thư viện thử và tài liệu bàn giao. Phạm vi tích hợp mặc định trong trận chính hiện gồm ba nhóm vũ khí trên. Năm clip là mô phỏng dựng ngoài Unity.

## Trạng thái Git

- Commit VFX là `312889f0` (`Enable native weapon VFX in CatTanks battles`).
- Lần push đầu trong phiên trả `403`. Khi ghi nhật ký, `main` và ref theo dõi `origin/main` trong checkout đều ở `54136888` (`vfx+clean image`). Đã xác nhận `312889f0` là commit tổ tiên của `origin/main`.

## Checklist của bản Unity Native trước khi chuyển Web

1. Mở `Sources/CatTanks` bằng Unity **2022.3.62f2**. Mở **CatTanks → Combat → Open Native Weapon Preview**, vào Play Mode và kiểm tra dòng **Unity Native**.
2. Chạy **CatTanks → Combat → Validate Native Weapon VFX** để kiểm tra phát hạt, trail, pause/resume và tái sử dụng pool. Sau đó chạy `Main.unity` để kiểm tra đường chơi chính với ba loại vũ khí.
3. Đo FPS, p95/p99 frame time, overdraw và cấp phát khi nhiều quái, đạn và vụ nổ xuất hiện đồng thời. Dùng cùng máy, độ phân giải và kịch bản khi so với bản cũ.

Máy thực hiện phiên này thiếu Unity Editor. Đã kiểm tra cú pháp C#, atlas, cấu hình scene và giải mã đủ năm video. Compile trong Unity, shader, Play Mode và FPS vẫn cần xác nhận. Xem [hướng dẫn Unity native](VFX_Handoff/Nam_Vong/UNITY_NATIVE.md) để tiếp tục.
