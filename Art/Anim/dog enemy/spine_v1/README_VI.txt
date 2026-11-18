DOG ENEMY — Spine 4.2.43

Mở dog-enemy.spine, giữ images/ cạnh project.
24 PNG part cố định, rig bone, 6 clip:
idle (1.4s loop), walk (1.1s loop), melee_attack (0.9s), throw_grenade (1.15s), hurt (0.44s), death (1.3s).
melee_hit tại 0.47s: game kiểm tra vùng đánh gần melee_tip.
grenade_release tại 0.5s: game sinh projectile tại vị trí world của grenade_spawn; xóa grenade cầm trên tay.
footstep tại 0.275s và 0.825s. death tại 0.75s.
Hurt, death và các attack không loop. Giữ khung cuối death.
Chỉ nét mặt, bàn tay và vật cầm thay attachment, thân/chân/tay di chuyển bằng xương.
runtime/ do Spine xuất; previews/ từ PNG render thực bằng Spine.
Gói này là asset art và animation; AI, va chạm, projectile và damage cần nối vào game.
Nguồn ảnh: ImageGen tích hợp, prompt ở source/generation-prompts.json.
