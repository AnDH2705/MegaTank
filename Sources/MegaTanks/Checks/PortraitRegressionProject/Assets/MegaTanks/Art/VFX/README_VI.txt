TANK VFX V1 — GÓI HIỆU ỨNG BỔ SUNG

3 hiệu ứng, mỗi hiệu ứng 8 frame: tổng cộng 24 PNG nền trong suốt.
1. machine_gun_muzzle: chớp lửa đầu súng máy, 40 ms/frame, một nhịp 320 ms.
2. cannon_muzzle: luồng nổ ĐẦU NÒNG PHÁO và khói tan, 75 ms/frame, 600 ms.
3. exhaust_smoke: khói từ ống xả, 110 ms/frame, 880 ms; phun trái rồi bay lên.
   Đây là hiệu ứng phát súng/pháo, không phải vụ nổ va chạm ở mục tiêu.

FILE
- frames/: 3 thư mục, mỗi thư mục 8 PNG RGBA, canvas cố định 512 × 512 px.
- sheets/: mỗi hiệu ứng một sprite sheet 2048 × 1024, 4 cột × 2 hàng.
- metadata/vfx_manifest.json: thời lượng, thứ tự frame, pivot, tọa độ gắn vào xe.
- previews/tank_vfx_demo.gif: ghép thử cả 3 hiệu ứng trên tank đã tách part.
- previews/ riêng từng hiệu ứng và contact sheet để xem trước.

LẮP VÀO XE
Tất cả tọa độ tính từ góc trên trái canvas xe gốc 1231 × 1277 px.
Súng máy: socket (1015,191); pivot frame (64,256); scale gợi ý 0.44.
Pháo: socket (1206,512); pivot frame (48,256); scale 0.78; xoay 7° thuận chiều kim.
Ống xả: socket (94,124); pivot frame (412,420); scale 0.72.
Đây là vị trí theo ảnh tĩnh. Khi nòng recoil hoặc cụm súng đổi góc, gắn socket
vào part tương ứng để VFX di chuyển cùng nòng súng.

Đọc frame theo hàng: 01 02 03 04, rồi 05 06 07 08. Sau frame 08, ẩn hiệu ứng.
Giữ đúng pivot: không tự căn giữa hoặc trim từng frame nếu không lưu offset.
Có thể trim trong engine nếu engine giữ đầy đủ sprite/pivot offset gốc.
Súng máy có thể gọi lại chuỗi nhanh theo nhịp bắn. Với khói ống xả, phát các
particle chồng thời gian cách nhau khoảng 0.3 giây để tạo dòng khói liên tục.
GIF là bản xem trên nền xám; PNG và sprite sheet mới là tài nguyên có alpha.
Gói bổ sung này không thay đổi PNG/PSD tank đã giao trước đó.
