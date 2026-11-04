Ô TÔ + MÁY BAY / PARTS ANIM V1

Đã tách 2 bánh ô tô, 2 bánh càng đáp máy bay và cánh quạt. Thân, người lái,
súng và chi tiết tĩnh giữ chung một ảnh, theo kiểu ít layer đã thống nhất.

car/: 3 PNG/layer — thân tĩnh, bánh sau, bánh trước.
plane/: 4 PNG/layer — cánh quạt, bánh sau, bánh chính, thân tĩnh.
Mỗi thư mục có PSD, ORA, ảnh ghép và parts_manifest.json ghi tọa độ/pivot.
Canvas gốc của cả hai: 1536 × 1024 px. PNG đều có alpha trong suốt.

Hốc bánh đã được phục hồi. Hai bánh máy bay có phần hoàn chỉnh bên dưới
thanh càng; thanh càng ở lớp thân nên không quay cùng bánh. Trục/chóp mũi
xanh nằm trong thân máy bay, chỉ lá cánh quạt quay ở lớp phía sau.

plane/propeller_frames/: 12 frame cánh quạt nhìn ngang, canvas 160 × 900 px,
pivot (80,450), gắn tại (1326,576) trên canvas máy bay. Thời lượng gợi ý
40 ms/frame. Đây là phép chiếu quay quanh trục dọc máy bay: ép/phản chiếu
theo chiều Y. Không xoay lá cánh quạt này thành vòng tròn trên mặt phẳng ảnh.

previews/: GIF thử bánh ô tô, GIF cánh quạt + bánh máy bay, bảng xem các part.
GIF có nền sáng để dễ xem; PNG mới là tài nguyên có alpha. Đây là bộ artwork
đã tách với điểm xoay, chưa bind vào project Spine/Unity.
