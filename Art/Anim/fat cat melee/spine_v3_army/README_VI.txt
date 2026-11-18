FAT CAT — BRIGHT ARMY · SPINE 4.2.43

Mèo béo cận chiến bằng xẻng, mũ trụ tactical, mắt híp dẹt đáng yêu.
Trang phục army theo hướng arcade vui và sáng màu: plate carrier hai túi khaki lớn,
MOLLE, belt tactical, quần camo olive/vàng ấm, đệm gối, boots coyote và găng gia cố.
Phong cách 2D comic mượt, đồng bộ với bộ nhân vật hiện có.

Mở fat-cat-army.spine. Giữ images/ cùng vị trí tương đối với project.
5 anim: idle (loop), walk (loop), shovel_attack, hurt, death.
melee_hit tại 0.59s: game kiểm tra vùng đánh gần melee_tip.
weapon_drop tại 0s của death: rig ẩn xẻng, game có thể tạo vật rơi tại xương shovel.
Death không loop, giữ khung cuối.

Pelvis bo tròn giữ bẹn/háng. Draw order: thigh_far < pelvis < thigh_near.
Mặt, mũ, đuôi và xẻng được giữ nguyên; 16 PNG trang phục được sơn lại.
Alpha cắt part, kích thước, pivot, rig và các timeline giữ nguyên.
Hai tay có IK và constraint xoay để giữ điểm nắm trên cán xẻng.
Thân dùng mesh eo; 5 anim đã render kiểm tra bằng Spine 4.2.43.

runtime/: JSON, atlas và texture Spine xuất để dùng runtime 4.2.
previews/: PNG và GIF từ frame render thực bằng Spine.
source/bright-army-prompts.json: prompt tạo/sửa art bằng ImageGen tích hợp.
metadata/outfit-change-audit.json và validation-report.json: kiểm tra asset/rig.