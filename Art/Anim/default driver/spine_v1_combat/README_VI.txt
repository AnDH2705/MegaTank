DEFAULT DRIVER V1 + COMBAT — Spine 4.2.43

Mở default-driver-combat.spine. Giữ nguyên images/ cạnh project.
Giữ nguyên toàn bộ 24 PNG và 4 clip v1. Chỉ thêm xẻng, bone và 4 clip:
shovel_idle (loop 1.2s), shovel_attack (1.05s, không loop), hurt (0.44s), death (1.3s).
melee_hit tại 0.49s để game kiểm tra vùng đánh. hurt tại 0s. death tại 0.75s.
Động tác chết không loop; game giữ khung cuối hoặc dọn entity theo logic riêng. Không tự fade.
Hai tay bám cùng một xẻng; chuyển động là bone animation. Chỉ đổi nét mặt và kiểu nắm tay.
Source/prompt và metadata kèm theo; runtime/ là JSON + atlas do Spine xuất.
previews/ được ghép từ PNG render thực bằng Spine. Không có sát thương hay AI trong gói art.
