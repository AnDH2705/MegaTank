TANK FRAME V4

Đã bổ sung khung viền vỏ xe riêng, gồm vành ngoài, thanh ngăn hai tầng,
chân đỡ cố định trên nóc và chân đế ống xả.
Hai khoảng trong khoang là nền trong suốt thật.

- Tank_Front_Frame.png: khung trên đúng canvas 1351 × 1277 px; đặt ở (0,0).
- parts/11_front_hull_frame.png: cùng khung đã crop gọn; dùng vị trí trong JSON.
- Tank_Mortar_Frame_v4.psd / .ora: 11 layer, đã thêm layer khung.
- parts/07_body_static.png: nội thất tĩnh vẫn dính chung; khung và thanh ngăn
  đã được lấy ra khỏi ảnh thân để không bị lặp khung khi ghép.
- previews/frame_checker_preview.png: ảnh kiểm tra alpha, KHÔNG dùng làm sprite.

Khi thêm nhân vật, đặt nhân vật trên 07_body_static và dưới 11_front_hull_frame.
Mortar vẫn áp sát vách như v3, không đổi tỷ lệ/vị trí xe. Các cụm chuyển động
giữ nguyên. Canvas và các điểm gắn VFX không đổi so với v3.

Cửa tiếp đạn và nút bắn từ yêu cầu trước vẫn chưa được bổ sung trong bản này.
