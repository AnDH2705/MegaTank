DEFAULT DRIVER GUIDED — Spine 4.2.43

Cắt lại theo user-cutting-guide.png; tay ngắn, part capsule có phần chồng khớp.
Upperarm 64, forearm 52, thigh 70, calf 54 world units.
Calf nằm sau thigh; ankle của boot nằm sau calf. Foreground thigh trên pelvis, far thigh dưới.
8 anim: idle/run/sit_drive/sit_fire/shovel_idle/shovel_attack/hurt/death.
melee_hit=0.49s, fire=0.1s. Giữ last frame death, không loop.
Pelvis bo tròn giữ bẹn/háng. Quần dài cùng chất liệu đùi và calf.
Spine .spine + runtime JSON/atlas; body animation bằng xương và mesh eo, không đổi part thân theo frame.
Art tạo bằng ImageGen tích hợp; prompt/source và guide ở source/.

Pelvis cập nhật theo hình mới: đáy bo tròn, giữ bẹn/háng, không cuống đùi. Draw order: thigh_far < pelvis < thigh_near. Cap chồng của đùi trước không có viền đen.
