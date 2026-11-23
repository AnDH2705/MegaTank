DOG ENEMY V3 — SPINE 4.2.43

Mở dog-enemy-v3.spine; giữ images/ cùng folder tương đối.
Mặt phản diện: mắt nheo, nhe nanh, sẹo lông mày; có nét mặt chịu đòn.
Sửa vị trí cổ và hai vai theo artwork. Cổ/đầu, vai, khuỷu và cổ tay giữ tọa độ local cố định.
Đầu và hai tay kế thừa chuyển động của thân. Chỉ pelvis có timeline translate.
Vũ khí và lựu đạn là con của tay gần; không dùng bone vũ khí ở root để đuổi theo tay.
7 clip: idle, walk, melee_attack, throw_grenade, throw_grenade_overhand, hurt, death. Không đổi part thân/tay/chân theo frame.
melee_hit=0.47s; grenade_release=0.5s; hurt=0s; death=0.75s.
Lựu cầm trên tay ẩn tại release; game sinh projectile tại grenade_spawn.
Death không loop, giữ khung cuối. GIF có thể lặp để xem thử.
Runtime JSON/atlas và preview render bằng Spine 4.2.43.
Nguồn sửa ảnh: ImageGen tích hợp; prompt ở source/edit-prompts.json.

Thân dùng mesh có trọng số: phần gấu áo bám pelvis, phần ngực theo torso; tránh hở eo khi nghiêng người.

Pelvis cập nhật theo hình mới: đáy bo tròn, giữ bẹn/háng, không cuống đùi. Draw order: thigh_far < pelvis < thigh_near. Cap chồng của đùi trước không có viền đen.

Thân dùng mesh có trọng số: phần gấu áo bám pelvis, phần ngực theo torso; tránh hở eo khi nghiêng người.

ANIM THÊM: throw_grenade_overhand
Lấy đà dài hơn, ném nhanh qua vai, dồn trọng lượng về trước rồi thu tay. Phát một lần, không loop.
Event grenade_release tại 0.64s: game sinh lựu đạn tại world position của bone grenade_spawn; lựu cầm trên tay ẩn đúng mốc này. Dùng event để spawn một lần, tránh tạo thêm projectile theo timer trùng.
Clip throw_grenade cũ vẫn giữ nguyên (release 0.5s). Các clip khác và part/mesh/atlas không đổi.
previews/spine-throw_grenade_overhand.gif: chỉ render nhân vật từ Spine.
previews/throw_grenade_overhand-demo.gif: cùng frame Spine, ghép thêm đường bay lựu minh họa; projectile trong game do code xử lý.
