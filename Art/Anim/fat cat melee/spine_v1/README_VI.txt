FAT CAT MELEE — SPINE 4.2.43

Mèo béo mắt híp dẹt dễ thương, mũ cối sắt sơn xanh, dùng xẻng cận chiến.
Mở fat-cat-melee.spine; giữ images/ cùng thư mục tương đối.
5 clip: idle, walk, shovel_attack, hurt, death.
Hai tay IK bám cán xẻng; constraint xoay tay giữ tâm nắm trên cán.
Đầu, vai, khuỷu, cổ tay giữ local cố định; eo dùng mesh có trọng số.
melee_hit tại 0.59s để game kiểm tra vùng đánh.
weapon_drop ở 0s của death: rig ẩn xẻng; game có thể tạo vật rơi tại xương shovel.
Death không loop, giữ khung cuối. Walk/idle loop.
runtime/ do Spine 4.2.43 xuất; previews/ từ PNG render thực bằng Spine.
Đã giữ cỡ đầu 300 world units và giày 105 tương tự các nhân vật hiện có.
Nguồn tạo/sửa art: ImageGen tích hợp; prompt ở source/generation-prompts.json.

Pelvis cập nhật theo hình mới: đáy bo tròn, giữ bẹn/háng, không cuống đùi. Draw order: thigh_far < pelvis < thigh_near. Cap chồng của đùi trước không có viền đen.
