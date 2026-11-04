MÈO VÀNG + MÈO MƯỚP / ANIM V2 — KHỚP HÔNG TRÒN

Hai nhân vật theo form phác: mèo vàng mặc giáp vảy/mũ sừng/khăn xanh ngọc,
và mèo mướp xám nâu có vằn, áo túi cùng mũ xanh.

Mỗi nhân vật có 8 part PNG: đầu, mũ, thân kèm trang phục, hai tay, hai chân,
đuôi. Mắt/mũi/miệng dính vào đầu; phụ kiện trang phục tĩnh dính vào thân.
Phần khớp vai/hông và đầu dưới mũ đã có vùng chồng để ghép anim.

Mỗi thư mục gồm:
- parts/: 8 PNG RGBA nền trong suốt.
- .psd / .ora: 8 layer ở tư thế đứng, canvas 512 × 720 px.
- idle.png: ảnh ghép đứng.
- run_frames/: 16 frame chạy PNG trong suốt; 20 FPS, vòng lặp 0.8 giây.
- run_sheet_4x4.png: sprite sheet chạy, 4 cột × 4 hàng, 2048 × 2880 px.
- metadata/rig_and_run.json: điểm xoay, vị trí, xương và ma trận các frame.

previews/: GIF chạy riêng mỗi con, GIF chung, ảnh hai nhân vật và bảng part.
GIF dùng nền sáng để dễ xem; PNG/sprite sheet mới là tài nguyên có alpha.
Demo là vòng chạy cutout 2D theo các part, chưa phải project Spine đã bind.
Tọa độ JSON tính từ góc trên trái; góc xoay theo chiều kim đồng hồ.

SỬA Ở V2
- Cả bốn chân có đầu khớp hông tròn LỒI, kín đầy đủ, không còn đường cắt lõm.
- Điểm xoay đã đặt theo tâm cap tròn; cập nhật cả metadata, PSD và các frame.
- Phần bàn chân được giữ nguyên pixel từ bản cũ. Mỗi con vẫn có 8 part.
- Sáu part còn lại của mỗi con được giữ nguyên file; không vẽ lại đầu/mũ/thân/tay/đuôi.
- Canvas, trang phục, màu lông và thời lượng vòng chạy không đổi.
