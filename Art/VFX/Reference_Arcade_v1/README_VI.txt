MEGATANK ARCADE VFX V1 — 2026-11-18

Bộ thử vẽ mới theo style cả hai GIF bạn gửi: lõi nổ trắng, lửa vàng/cam,
vòng sáng mảnh, khói nâu có mảng sáng và viên đạn lửa dạng mũi giáo.
Art tạo bằng ImageGen tích hợp. Không cắt hình game, UI hay nhân vật từ GIF.
Prompt nằm trong Assets/MegaTankVFX/Documentation/generation-prompts.json.

CÓ GÌ TRONG BỘ
- 36 PNG frame 256×256, 10 atlas và texture BeamTrail 256×32.
- Tool Unity tạo 8 prefab: ExplosionSmall, ExplosionBig, ClusterBurst,
  HitSpark, MuzzleFlash, SmokePuff, DustPuff, FireBolt.
- Shader alpha/additive cho Built-in Render Pipeline của Unity 2022.3.
- Pool tái sử dụng effect; tự đợi khói/trail kết thúc rồi thu hồi.
- Demo riêng để bấm thử từng effect, không sửa MainBattle.

CÁCH MỞ TRONG PROJECT UNITY
Project đã có: D:/Project/Tnak/MegaTank/Sources/MegaTanks (Unity 2022.3.62f3).
Các file mới nằm tại Assets/MegaTankVFX. Particle System module được bật trong
Packages/manifest.json. Khi mở Unity, đợi compile rồi chọn:
  MegaTank VFX > 2. Open demo scene
  Sau đó bấm Play.
Menu tự tạo material, 8 prefab và scene tại Assets/MegaTankVFX/Generated/.
Có thể chỉ chạy menu 1. Build prefabs nếu muốn giữ scene đang mở.

Nếu import sang project khác: chép thư mục Assets/MegaTankVFX vào Assets.
Project cần com.unity.modules.particlesystem và com.unity.modules.imgui 1.0.0.
Shader hiện nhắm Built-in pipeline của project này. Với URP, đổi material sang
Particles/Unlit và giữ cách chia atlas/timing của các Particle System.

PARTICLE XỬ LÝ NHƯ NÀO
Nổ: core flipbook 12 frame / 0.48s + glow additive 0.23s + shock ring 0.26s.
Sparks: burst 18/26 hạt (small/big), lifetime 0.18–0.48s, vận tốc tỏa ngẫu
nhiên, force Y=-3, nhỏ và mờ dần. Không bật collision để giảm chi phí.
Smoke: bắt đầu trễ 0.12s, flipbook 8 frame / 0.85s, bay lên 0.45 unit/s,
alpha giữ tới 60% lifetime rồi mờ về 0. Dùng alpha blend để khói che nền.
FireBolt: đầu lửa flipbook 3 frame lặp 30fps trong Local space; TrailRenderer
lưu vệt sáng 0.13s; khói World space rate-over-distance=3.5, sparks=4.
Vì khói/tia lửa ở World space nên chúng ở lại đúng chỗ khi viên đạn đi xa.
Mỗi puff khói đuôi có size ngẫu nhiên 0.45–0.95.
Chỉ glow/ring/sparks dùng additive; không áp additive cho cả cụm khói.

GẮN VÀO GAME
Sau khi tạo prefab, kéo prefab vào field GameObject của script gọi effect:
  MegaTankVFX.VfxAutoRelease.Spawn(explosionPrefab, hitPosition, Quaternion.identity);
Muzzle: truyền Quaternion.Euler(0,0,gocNongSung).
FireBoltDemoMotion chỉ dùng để bay thử và nổ sau 0.32s. Khi gắn vào projectile
thật, bỏ component demo motion, cho VFX bám projectile đang có; hit/damage do
game xử lý. Dùng StopEmission() lúc trúng đích, rồi effect tự thu hồi.
Khi dùng Spine: gọi hit VFX ở event melee_hit, muzzle ở event fire, không gọi
trong mỗi Update để tránh nổ lặp theo frame.
Sorting Order mặc định 98–114; chỉnh Sorting Layer/Order theo scene của bạn.

KIỂM TRA
Đã kiểm tra số frame, atlas, padding trong suốt và ảnh/GIF preview.
previews/ là bản ghép kiểm tra artwork và chuyển động ngoài Unity.
Không có Unity Editor khả dụng trong môi trường này để compile/playtest;
cần kiểm tra Console và chạy demo trong Unity trước khi đưa vào build game.
Source GIF gốc chỉ nằm ở source/ để đối chiếu, không nhập vào game.

Menu chỉ tạo prefab/scene còn thiếu, giữ các chỉnh sửa prefab đã có.
Tài liệu Unity về flipbook, emission và World space:
https://docs.unity3d.com/2022.3/Documentation/Manual/PartSysTexSheetAnimModule.html
https://docs.unity3d.com/2022.3/Documentation/Manual/PartSysEmissionModule.html
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ParticleSystemSimulationSpace.World.html
