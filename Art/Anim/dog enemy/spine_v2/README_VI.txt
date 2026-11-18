DOG ENEMY V2 — SPINE 4.2.43

Mở dog-enemy-v2.spine; giữ images/ cùng folder tương đối.
Mặt phản diện: mắt nheo, nhe nanh, sẹo lông mày; có nét mặt chịu đòn.
Sửa vị trí cổ và hai vai theo artwork. Cổ/đầu, vai, khuỷu và cổ tay giữ tọa độ local cố định.
Đầu và hai tay kế thừa chuyển động của thân. Chỉ pelvis có timeline translate.
Vũ khí và lựu đạn là con của tay gần; không dùng bone vũ khí ở root để đuổi theo tay.
6 clip: idle, walk, melee_attack, throw_grenade, hurt, death. Không đổi part thân/tay/chân theo frame.
melee_hit=0.47s; grenade_release=0.5s; hurt=0s; death=0.75s.
Lựu cầm trên tay ẩn tại release; game sinh projectile tại grenade_spawn.
Death không loop, giữ khung cuối. GIF có thể lặp để xem thử.
Runtime JSON/atlas và preview render bằng Spine 4.2.43.
Nguồn sửa ảnh: ImageGen tích hợp; prompt ở source/edit-prompts.json.

Thân dùng mesh có trọng số: phần gấu áo bám pelvis, phần ngực theo torso; tránh hở eo khi nghiêng người.
