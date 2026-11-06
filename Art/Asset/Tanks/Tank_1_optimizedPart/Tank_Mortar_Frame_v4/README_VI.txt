TANK – BẢN TƯƠNG PHẢN CAO, VIỀN ĐẬM

Giáp đổi sang xanh teal/emerald, bóng xanh đen, cạnh sáng mint.
Nội thất kem sáng, ghế cam đồng, điểm nhấn vàng và xích xanh than.
Đã đưa cùng màu/nét vào 11 part, giữ các cụm bánh và vũ khí riêng.

CANVAS VÀ ĐIỂM GẮN
Canvas tổng vẫn 1351×1277. Pivot theo canvas và socket mortar_muzzle giữ nguyên.
Mỗi PNG được thêm 12 px đệm quanh để nét viền mới không bị cắt.
Vì vậy cần dùng position_px, size_px và pivot_local_px trong JSON mới.
Không dùng lại các giá trị vị trí local của bản cũ.
metadata/layer_order.tsv có cùng thứ tự lớp và các vị trí mới.

LỚP NHÂN VẬT
Đặt nhân vật trên 07_body_static và dưới 11_front_hull_frame.
Hai khoảng trống của khung vẫn có alpha trong suốt.
Tank_Front_Frame.png là khung đã đặt đúng canvas, dùng tại (0,0).

FILE
- parts/: 11 PNG trong suốt.
- metadata/: JSON/TSV vị trí, pivot, thứ tự lớp và thông số ghép thử.
- Tank_Mortar_Frame_v4.psd / .ora: cùng 11 layer đã cập nhật màu/nét.
- previews/tank_frame_v4.png: xe ghép đủ part, nền trong suốt.
- previews/bg1_after.png / bg2_after.png: thử trên hai BG.
- previews/bg1_before.png / bg2_before.png: bản cũ cùng kích thước/vị trí.
- previews/Tank_compare_2_backgrounds.jpg: so sánh 4 ô.

LƯU Ý
Đây là bản vẽ lại màu và nét, có thể có khác biệt nhỏ trong chi tiết nội thất.
Cấu trúc 11 part, canvas và các pivot theo canvas được giữ để ghép/animate.
Phần nội thất tĩnh vẫn chung trong 07_body_static.
Không bổ sung các hạng mục khác ngoài màu và viền ở yêu cầu này.
