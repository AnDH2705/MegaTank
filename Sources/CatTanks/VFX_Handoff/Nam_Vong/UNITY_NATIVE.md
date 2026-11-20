# Thử ba vũ khí bằng VFX của Unity

CatTanks đang dùng **Built-in Render Pipeline** trên Unity 2022.3.62f2. Mẫu mới giữ pipeline này và dùng các tính năng có ích trực tiếp cho combat 2D. Visual Effect Graph, Shader Graph và Light2D cần pipeline khác. Để thử các công cụ đó, tạo nhánh URP riêng rồi kiểm tra lại renderer/UI, shader pack cũ và thiết bị mục tiêu trước khi chuyển dự án. [So sánh render pipeline Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/render-pipelines-feature-comparison.html)

## Đường chạy Unity

1. Mở **CatTanks → Combat → Open Native Weapon Preview**, sau đó vào Play Mode. Màn thử dùng đúng phát bắn của `Battle`, cùng Gatling, đại bác và tên lửa trong catalog. Dòng trạng thái phải hiện **Unity Native**.
2. Phím **1–3** thử từng vũ khí. **4** bắn đồng thời, **P** tạm dừng, **N** bước từng frame, **S** xem 0,25×. Mở Hierarchy để xem nhóm Native weapon VFX pool và các ParticleSystem, SpriteRenderer, LineRenderer đang dùng.
3. Chạy **CatTanks → Combat → Validate Native Weapon VFX** trong Play Mode. Mã kiểm tra việc tạo trail, phát hạt, giữ trạng thái khi pause, thu hồi và tái sử dụng pool. Log thành công là `CATTANKS_NATIVE_WEAPON_VFX_PASSED`.
4. **Use Native Weapon Vfx** đã bật mặc định ở component **CatTanks Game** trong `Main.unity`. Mở một trận có Gatling, đại bác hoặc tên lửa để xem. Có thể tắt ô này khi đo trước/sau bằng cùng trận, cấu hình và thiết bị. Các hệ effect nằm ngoài ba vũ khí này tiếp tục qua route hiện tại.

## Tính năng đang được gọi

| Phần hình | Tính năng Unity | Cách điều khiển |
| --- | --- | --- |
| Thân đạn / tư thế vụ nổ | SpriteRenderer, texture atlas và AnimationCurve | Mỗi thân đạn có nguyên họa riêng. Định hướng theo projectile, tăng scale theo tuổi và đổi tint ở lớp hắt sáng. |
| Chớp nòng | SpriteRenderer flipbook | Ba dãy 8 frame Gatling/pháo/rocket được chọn từ tuổi event. |
| Vệt bay | LineRenderer và lịch sử điểm đạn | Tối đa 10 điểm/viên. Lấy mẫu khi `Projectile.age` thay đổi, giúp trail dừng cùng trận. Sét và beam đã dùng LineRenderer ở BattleView. |
| Tia lửa | ParticleSystem Emission Burst, Shape, Size/Color over Lifetime, Gravity, Texture Sheet Animation | Dùng sprite atlas, có hạt lớn nhỏ và thời gian rơi khác nhau. |
| Vệt của hạt / tàn tắt | Particle System Trails và Death Sub Emitter | Một phần tia lửa kéo vệt. Hạt chết có thể bật tàn nhỏ giới hạn. |
| Khói | ParticleSystem Noise và flipbook | Trang bụi gốc 8 frame, phát trễ. Noise một octave để giảm chi phí. |
| Vòng xung lực / hắt sáng | SpriteRenderer, HLSL alpha & additive, sorting order | Vòng ép dẹt theo mặt đất. Ánh sáng thủ tục vùng nhỏ nằm trên nền. |
| Cảm giác lực | Recoil và hit tint của BattleView, camera offset theo tuổi | Chọn một cú rung mạnh nhất tại thời điểm đó để chùm rocket không cộng dồn rung. |

Pool native tạo sẵn 12 đạn và 8 cụm khi dựng view, tăng tối đa đến **48 đạn, 24 cụm chớp/va chạm**. Mỗi cụm đặt cap **10 tia, 4 khói, 6 tàn**. Khi đầy, BattleView có thể dùng sprite prototype cho phần dư. Tiêu thụ alpha, CPU/GPU và allocations vẫn cần đo thực tế. `ParticleSystem.Simulate` và trail được lấy theo thời gian trận. [Unity Particle System modules](https://docs.unity3d.com/2022.3/Documentation/Manual/ParticleSystemModules.html) · [ParticleSystem.Simulate](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ParticleSystem.Simulate.html)

## Bàn giao sang checkout khác

- Chép các file thêm trong `Assets/CatTanksVfxKit` cùng `.meta`. Texture `CombatPoses.png` cần giữ **1774 × 887**, `nPOTScale=0`. Vùng cắt dùng hệ tọa độ từ trên xuống trong JSON và được đổi khi tạo sprite Unity.
- Áp đúng các hook trong `BattleView.cs`, `WeaponEffectsPreview.cs` và `CatTanksGame.cs`. Đối chiếu constructor `BattleView` vì các checkout có thể khác tham số lựa cảnh. Nhánh native nhận `BattleEffect.weaponKind` và giữ prototype làm fallback. Nếu checkout còn thiếu tag, gắn `w.kind` cho event flash/impact của rapid, shell và rocket như phiên bản prototype.
- Chạy kiểm thử cũ và mới, rồi mở Profiler/Frame Debugger. Kiểm tra cấp phát lần đầu, render pause, overdraw khi ba rocket nổ sát nhau và stress 60 quái. Dùng cùng camera/độ phân giải khi so p95/p99.

Năm clip trong thư mục là bản duyệt dựng ngoài Unity. Mã native, atlas và hook đã được chuẩn bị. Máy dựng clip thiếu Unity Editor, nên compile, shader, Play Mode và số đo hiệu năng cần xác nhận trên máy đích.
