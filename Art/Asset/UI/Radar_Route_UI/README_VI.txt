RADAR ROUTE UI

Reference/route_scene_1920x1080.png: bản thiết kế scene chọn tuyến đường.
Background/radar_coordinate_bg_1920x1080.png: texture radar gốc để dùng
bên trong route, không phải nền toàn scene.
Background/radar_in_route_frame_1920x1080.png: lớp radar đã giới hạn trong
khung route, trong suốt ở toàn bộ phần ngoài khung.
Background/scene_background_1920x1080.png: nền tối trơn với radar chỉ trong
khung route. Không có vòng tròn, cung quét hay đường địa hình.
Sprites/Icons/: 5 PNG nền trong suốt, cùng nét comic viền đậm:
  Battle — kiếm chéo xanh.
  Hard battle — đầu lâu đỏ.
  Supply — đúng ba viên đạn.
  Unknown — dấu hỏi tím.
  Repair — chữ thập xanh hồi máu.
Sprites/Frames/: khung node thường, đang chọn, có thể chọn, khóa;
thanh header ghi toàn chiều ngang, ô Stage vàng ghép liền, vùng nút quay
lại và panel chú giải đều chưa gắn chữ/icon.
Sprites/Controls/: mũi tên và marker tank.
Data/route_graph.json: node, đường nối và vị trí, tọa độ từ góc trên trái.
Data/sprite_manifest.json: thông tin sprite và border gợi ý.
Data/ui_layout.json: vị trí header và vùng giới hạn radar.
Source_SVG/: phần nền và khung có thể chỉnh sửa bằng vector.

Bản scene là thiết kế hình ảnh, chưa có logic chọn node hay cuộn màn hình.
Đường nối và chữ được vẽ riêng khỏi các khung nút để tiện lắp trong Unity.
