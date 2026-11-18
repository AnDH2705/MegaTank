DOG PILOT — Spine 4.2.43 Professional

Stocky charcoal/chocolate bulldog, black short spiky forelock, cream jowls, pale eyebrows, blue-gray eyes, burgundy flight cap and blue goggles, slate-blue shearling flight jacket and long trousers, golden scarf, tan harness, brown gloves and boots

25 PNG parts, 26 bones. Cùng tỷ lệ và 8 clip với mèo default driver guided:
idle 1.6s; run 0.8s; sit_drive 1s; sit_fire 0.7s; shovel_idle 1.2s;
shovel_attack 1.05s; hurt 0.44s; death 1.3s.

Animation bằng bone + mesh eo có weight + 2 tay IK cầm xẻng; không thay
ảnh part thân theo frame. Mặt có neutral và recoil; bàn tay có relaxed/grip/trigger.
Quần dài ống suông, pelvis giữ bẹn/háng, đùi xa dưới pelvis và đùi gần trên pelvis.
Tay upperarm 64 + forearm 52; chân thigh 70 + calf 54 world units.

Event: fire 0.1s; melee_hit 0.49s; weapon_drop 0s; death 0.8s.
Attack/hurt/death phát một lần; GIF demo lặp để dễ xem. sit_drive/sit_fire là
pose nhân vật dùng trong khoang xe; vô lăng/súng và đạn do game gắn/spawn riêng.
Điểm seat_anchor nằm tại pelvis; feet_anchor nằm ở root.

File .spine là project native; runtime/ là JSON + atlas + texture để import game.
previews/ là các clip render thật bằng Spine và GIF tổng hợp.
source/ lưu art nguồn, guide và prompt ImageGen tích hợp.
