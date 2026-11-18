DEFAULT DRIVER — BỘ PART VÀ RIG SPINE 4.2

MỞ default-driver.spine BẰNG SPINE 4.2.43 HOẶC 4.2 MỚI HƠN.
Giữ nguyên thư mục images/ cùng vị trí tương đối với project.

images/: 24 PNG alpha, 8 px đệm. Các phần bị che đã được ImageGen vẽ bù theo concept.
source/: bảng part gốc và prompt.
metadata/: kích thước, pivot, thứ tự lớp, bone và thông số clip.
previews/: ảnh ghép và GIF dựng trực tiếp từ bone/part của rig.
default-driver.json: dữ liệu rig để Import Data trong Spine 4.2.
runtime/: JSON, atlas và texture do Spine 4.2 xuất ra để dùng với runtime 4.2.

Animation: idle, run, sit_drive, sit_fire. Đây là các động tác khởi đầu để tinh chỉnh tiếp.
Chạy là vòng tại chỗ; game tự di chuyển root. sit_drive và sit_fire giữ hông tại seat_anchor.
Các hand_contact là điểm tham chiếu cho cần lái/tay cầm; cần chỉnh vị trí theo từng mẫu tank.
Event fire tại 0.1s; footstep tại 0.2s và 0.6s.

Đầu, mũ, kính, tai nghe gộp thành một attachment; có đầu happy và recoil thay thế.
Tay mỗi bên: relaxed / grip / trigger. Các khớp sử dụng region PNG với phần chồng, chưa làm mesh/weight.
Mặt trước scarf và scarf tails tách riêng để có thể rung/lắc.

Nguồn artwork: ImageGen tích hợp; prompt nằm source/parts-generation-prompt.txt.
