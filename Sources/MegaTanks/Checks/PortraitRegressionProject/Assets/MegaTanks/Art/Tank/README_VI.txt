TANK CUTAWAY — BỘ PART LÀM ANIM / V1

Đã xóa đủ 3 nhân vật, phục hồi ghế, bộ điều khiển và tường sau.
Giữ thiết kế mặt cắt hai phòng, hướng xe quay sang phải.
Kích thước lắp ráp gốc: 1231 × 1277 px.

FILE CHÍNH
- Tank_Cutaway_Layers.psd: 39 layer độc lập, giữ đúng vị trí trên canvas.
- Tank_Cutaway_Layers.ora: bản nhiều layer lossless dự phòng, mở bằng Krita/GIMP.
- parts/: 39 PNG RGBA nền trong suốt, crop gọn, giữ tỉ lệ gốc.
- metadata/parts_manifest.json: vị trí lắp ráp, điểm xoay, thứ tự lớp, nhóm cha.
- metadata/layer_order.tsv: bảng vị trí đơn giản, đọc bằng bảng tính hoặc editor.
- previews/tank_assembled.png: bản ghép từ chính các part xuất ra.
- previews/parts_contact_sheet.png: bảng xem toàn bộ các part.
- previews/tank_parts_motion_test.gif: thử quay bánh và recoil hai nòng súng.
- previews/tank_empty_reference.png: bản xe đã xóa người để đối chiếu thiết kế.

CÁCH LẮP
1. Dùng PSD/ORA để giữ nguyên vị trí của toàn bộ layer. Hoặc đặt từng PNG ở
   position_px trong JSON; tọa độ tính từ góc trên trái của canvas.
2. pivot_local_px là điểm xoay tính trong PNG đã crop; pivot_canvas_px là cùng
   điểm đó trên canvas lắp ráp. Thứ tự layers trong JSON: sau → trước.
3. Nhóm root: gầm, xích, bánh. Nhóm body: vỏ xe và nội thất. cannon: cụm pháo.
   roof_gun: súng máy nóc. driver_controls: cần điều khiển/pedal.
4. Đặt nhân vật mới trước tường sau, sau viền vỏ 32_front_hull_frame.
   Ghế và tay điều khiển đã riêng layer, có thể sắp xếp để tay/người nằm đúng lớp.
5. Nếu engine dùng gốc dưới trái, đổi y theo chiều cao canvas. Với một điểm
   pivot: y_up = 1277 - y_down. PNG và vị trí trong gói không bị lật.

PHẠM VI
- Đây là bộ artwork đã tách, chưa phải project Spine/Unity đã bind xương.
- Có phần tường phục hồi dưới các chi tiết tháo rời; các bộ phận chỉ thấy một
  phần do vỏ che vẫn là hình cắt theo góc ngang hiện tại, không phải hình 360°.
- Bánh xe đã riêng từng chiếc. Băng xích là một vòng riêng; để chạy mắt xích
  liên tục cần UV scrolling/mesh hoặc làm thêm chu kỳ mắt xích.
- GIF chỉ kiểm tra hoạt động layer/pivot và recoil, không phải animation hoàn chỉnh.
