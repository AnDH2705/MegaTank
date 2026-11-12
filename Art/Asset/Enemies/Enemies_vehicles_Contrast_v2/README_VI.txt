ENEMIES VEHICLES — CONTRAST V2

Đã chỉnh màu và nét cho nền phố công nghiệp hoàng hôn:
- Ô tô: xanh cobalt, bóng navy, mảng sáng ivory; giữ mũ đỏ của chó lái xe.
- Máy bay: đỏ rượu thiên tím, bóng plum, mảng sáng ivory; mũ phi công navy.
- Lốp và cánh quạt: than xanh, điểm sáng thép lạnh, vành đồng bộ màu thân.
- Viền ngoài than đen dày hơn, nét trong gọn hơn, cel shading rõ sáng tối.

car/: 3 PNG part, ảnh assembled, PSD nhiều layer, ORA và manifest.
plane/: 4 PNG part, ảnh assembled, PSD nhiều layer, ORA và manifest.
plane/propeller_frames/: 12 frame cánh quạt mới, 160 × 900 px.
previews/: ảnh đặt lên BG, so sánh trước/sau và GIF thử bánh / cánh quạt.
source_edits/: các bản màu gốc được chỉnh bằng imagegen và prompt tương ứng.

Canvas ghép: 1536 × 1024 cho cả hai xe.
Toàn bộ tọa độ pivot_canvas_px và điểm gắn cánh quạt giữ theo bộ gốc.
PNG từng part thêm 16 px đệm trong suốt, vì vậy dùng position_px và
pivot_local_px MỚI trong parts_manifest.json. Tổng position + pivot_local
vẫn bằng pivot_canvas của bộ gốc. Không kéo giãn riêng chiều ngang/dọc.

Hai bánh ô tô và hai bánh máy bay là sprite riêng để quay độc lập.
Cánh quạt giữ sau thân, chóp mũi thuộc thân tĩnh. 12 frame cánh quạt là phép
chiếu ép / lật theo Y quanh trục dọc máy bay, không quay phẳng trên ảnh XY.
Thời gian gợi ý 40 ms/frame; GIF tổng hợp dùng 70 ms/frame để dễ kiểm tra.

Ảnh nền trong previews là BG đã có của bạn, dùng để đối chiếu độ tương phản.
