DEFAULT DRIVER — SPINE V2 / GIỮ DÁNG VÀ NHỊP GIF DEMO

Mở default-driver-v2.spine bằng Spine 4.2.43 hoặc bản 4.2 phù hợp.
Giữ nguyên images/ cạnh file .spine. Runtime export nằm trong runtime/.

BẢN NÀY HOẠT ĐỘNG NHƯ THẾ NÀO
Artwork nhìn thấy được cắt trực tiếp từ 16 pose trong GIF demo ban đầu.
9 cụm part: đầu+mũ+kính, thân, hai tay, hai chân, đuôi, khăn trước và khăn sau.
Có một lớp ink_details cho các nét mảnh tách rời như đầu ria và nét viền.
Tổng 160 PNG tương ứng 10 lớp x 16 pose; không có attachment là cả con mèo.
Tay/chân có 64 mesh được gắn trọng số vào xương vai/khuỷu/tay hoặc hông/gối/chân.
Từng clip kết hợp pose xương với đổi attachment để giữ đúng nét vẽ tay và độ co duỗi của GIF.
Phần bị che được bổ sung từ artwork ImageGen v1 ở vị trí đã được lớp trước che kín.

CLIP
run: 8 pose, 0.8 giây, vòng chạy tại chỗ. Di chuyển root bằng code gameplay.
sit_drive: 4 pose giữ 0.2 giây mỗi pose, vòng 0.8 giây.
sit_fire: nhịp 1.6 giây giống demo; event fire ở 0.1 giây.
idle: giữ tư thế ngồi sẵn sàng của pháo thủ.

CÁCH CHỈNH TIẾP
Có thể chỉnh mesh, xương và timing trong Spine. Thêm động tác mới lớn cần thêm/chỉnh pose attachment.
Các bề mặt bị che được hoàn thiện cho những pose đang có; xoay nhiều làm lộ vùng mới có thể cần vẽ bù.
Các marker seat_anchor, hand_contact_near, hand_contact_far dùng làm điểm tham chiếu ban đầu; căn lại theo tank.
Bộ này giữ độ phân giải gốc của ô demo (313–314 px), phù hợp bản thử ở kích thước nhỏ.

KIỂM TRA
previews/spine-*.gif được dựng từ PNG xuất độc lập bằng chính Spine 4.2.43.
spine-v2-in-tank.gif ghép các frame Spine đó vào background, tank và VFX có sẵn.
metadata/native-comparison.json lưu kết quả so với frame demo: cả 48 frame đã qua kiểm tra.
Alpha rất thấp được làm sạch, phần gần kín được chuẩn hóa alpha; xem ghi chú trong metadata.

NGUỒN
source/original-demo-sheet.png: bảng pose tạo bằng ImageGen tích hợp ở bản demo ban đầu.
Các prompt nguồn nằm source/*generation-prompt.txt. V2 dùng cắt/ghép và rig từ artwork có sẵn.
