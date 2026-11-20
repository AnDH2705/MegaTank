# VFX tham chiếu Meow Bastion Web

Shortcut `CHOI GAME.lnk` trong thư mục `tank_cat` mở bản cục bộ Three.js. Thông số chuyển từ `src/visuals.js` và các sự kiện trong `src/battle.js`. Ba ảnh gốc `ammo-2.png`, `ammo-8.png` và `vehicle-9.png` được chép vào `Resources/CatTanksVfxKit/WebReference` và kiểm tra SHA-256 trùng nguồn.

- Đạn hiển thị gấp hai lần kích thước cơ sở. Rocket rộng 62 đơn vị, mũi tên rộng 78, cả hai xoay thêm `-.48` radian như bản web. Các nhóm còn lại dùng hình chữ nhật, đĩa, đa giác cưa hoặc quầng sáng theo loại.
- Va chạm có `size > 40` dùng bán kính `size × 1.65`, chớp 0,16 giây, vòng 0,55 giây, bốn lớp lửa, bốn lớp khói và hai đợt tia lửa 36 + 22 hạt. Khói tồn tại 0,95–1,40 giây.
- Bản chuyển dùng độ dày laser 16 và lõi 6, sniper 6 và lõi 2. Sét có 12 đoạn, giữ hình ngoằn ngoèo đến hết vòng đời và phát hạt ở điểm cuối. Các lớp phụ gồm chớp nòng, tia vỏ đạn, lá chắn, hồi máu, đóng băng, drone hỗ trợ, đối tượng cháy, cảnh báo và bụi xe.
- Game Unity giữ vật lý chiến đấu, kích thước va chạm và hiệu ứng riêng như bắt quái. Shader tái tạo các profile alpha của WebGL. Các lớp dùng mesh tái sử dụng và bộ đệm chung tối đa 2.400 hạt. Bản Unity lấy thời gian từ `Battle` và dùng seed riêng cho VFX, vì bản web gọi `Math.random()` theo frame.

## Duyệt trong Unity

- [Main.unity](../Assets/CatTanks/Scenes/Main.unity) bật **Use Web Weapon Vfx** theo mặc định. Bắt đầu một trận mới để xem. Khi thiếu tài nguyên hoặc shader, đường render Unity Native có thể được dùng nếu ô đó đang bật.
- **CatTanks → Combat → Open Native Weapon Preview** mở scene thử. **W** chuyển giữa bộ Web và Native. **P** tạm dừng, **N** bước từng frame, **S** xem tốc độ 0,25×.
- Trong Play Mode, chạy **CatTanks → Combat → Validate Web Reference VFX**. Log `CATTANKS_WEB_REFERENCE_VFX_PASSED` xác nhận đường shader/asset, một vụ nổ với bốn lớp khói, đạn rocket 62 đơn vị, pause/resume và giới hạn hạt.
- Để so hình với WebGL, lấy mốc 0,08 giây từ cảnh `tank_cat/scripts/preview-effects.mjs` ở 1600×900 và 900×506. So vị trí, bề rộng, lớp sáng và nhịp tan của từng đối tượng. Màu tổng thể còn phụ thuộc nền màn, blend và color space.

Lượt chuyển này thực hiện trên máy thiếu Unity Editor. Cần chạy Play Mode để kiểm tra shader, hình ảnh và FPS của trận chính.
