# Bàn giao VFX CatTanks cho agent Codex

## Mục tiêu

Tích hợp atlas VFX hoạt hình 2D vào CatTanks Unity 2022.3.62f2. Bám theo vị trí, tuổi, va chạm, trạng thái và thời gian chiến đấu hiện có. Bộ này gồm 58 mẫu, dữ liệu định tuyến 25 loại đòn bắn, renderer atlas, bridge đọc `Battle`, công cụ kiểm tra Unity và gallery chạy offline. Các mô-đun trong `Assets/CatTanksVfxKit` đã sẵn đường sử dụng. Máy đích cần nối chúng vào `BattleView` và gắn tag cho các điểm phát sinh hiệu ứng bên dưới.

- [DANH_MUC_VFX.md](DANH_MUC_VFX.md) liệt kê vũ khí, thú đồng hành, quái, kỹ năng và kỹ thuật cho từng mẫu.
- [Preview.html](Preview.html) cho phép duyệt từng hiệu ứng và tình huống bắn đồng thời.
- [THAM_KHAO_HINH_ANH.md](THAM_KHAO_HINH_ANH.md) ghi nguồn hình và các quyết định mỹ thuật.

## Nhập asset

1. Giải nén giữ nguyên cấu trúc. Chép `Assets/CatTanksVfxKit` và `Assets/CatTanksVfxKit.meta` vào project CatTanks đích, giữ toàn bộ `.meta` bên trong. `VFX_Handoff` nằm cạnh `Assets`; thư mục này chứa tài liệu, gallery, bảng mỹ thuật và source tạo atlas.
2. Mở Unity 2022.3.62f2, đợi import, kiểm tra Console. `atlas.png` là RGBA 2048×2048, lưới 16×16, ô 128×128, đọc từ trái sang phải và từ trên xuống. `effects.json` và `AtlasVfx.shader` ở chung namespace Resources với atlas. Dùng asset mặc định kiểu Texture, sRGB, Bilinear, Clamp, No Mip Maps, Read/Write tắt. Giữ kích thước 2048 khi duyệt hình lần đầu. Có thể đặt ASTC 6×6 trên thiết bị hỗ trợ sau khi nhìn lại các nét mảnh và alpha, hoặc giữ RGBA nếu cần bảo toàn hình.
3. Trong Play Mode, chạy **CatTanks → Combat → Validate VFX Atlas Kit**. Menu hoạt động với dữ liệu mẫu ngay sau import. Đọc log `CATTANKS_VFX_KIT_PASSED` và lỗi Console nếu có.

Atlas làm một texture chung cho frame, đạn và glyph. Shader đi kèm được nạp bằng Resources để Unity giữ shader khi build. Production renderer dùng các quad mesh dùng chung; các lớp particle trong bộ mẫu được tính theo tuổi bằng C# và vào cùng mesh.

## Nối BattleView

Mở `Assets/CatTanks/Scripts/BattleView.cs`. Đối chiếu nhánh ở máy đích trước khi sửa. Bản đã từng thử Gatling/pháo/tên lửa có `BattleView.WeaponEffects.cs`; bản cũ chỉ có `CombatVfxPlayer` và các sprite/line dựng trong `BattleView`.

1. Thêm `using CatTanks.VfxKit;`, giữ một `BattleVfxBridge` cho vòng đời mỗi view. Khởi tạo một lần sau khi tạo `root`. Gọi `Dispose()` cùng vòng đời view. Mức mặc định nên bắt đầu từ `Medium` trên thiết bị cần ưu tiên khung hình. Nếu lưu tuỳ chọn giảm chuyển động, nối nó vào `ReducedMotion`.
2. Ở vòng lặp `b.bullets`, gọi `kit.Handles(p)` **trước** `alive.Add(p.id)`. Bỏ qua renderer projectile cũ cho viên kit nhận được để cleanup cũ có thể thu hồi các view còn sót khi đổi chế độ. Cứ để nhánh orb, actor, health bar, damage text và HUD chạy như hiện tại.
3. Ở vòng lặp hiệu ứng cũ, sau khi lọc text và trước `DrawWeaponEffect`, LineRenderer hoặc sprite fallback, kiểm tra `kit.Handles(e)` rồi `continue` cho effect được kit nhận. Khi giữ ParticleSystem cũ làm fallback, thêm `Predicate<BattleEffect> skip = null` vào `CombatVfxPlayer.Render`, lọc cả vòng thu hồi `aliveIds` lẫn vòng lấy instance. Cache delegate `kit.Handles` ở constructor của view và truyền vào. Đặt bridge dưới công tắc thử nghiệm để đo cùng một build với route cũ.
4. Renderer kit đã đọc dấu khóa mục tiêu và mái chắn. Khi bật kit, tắt hai hình cũ để tránh viền chồng. Giữ hit flash và recoil của actor hiện có. Nếu dùng prototype cũ, dừng việc lấy mẫu khói prototype khi kit đã nhận projectile.
5. Cuối `RenderCore`, sau khi `Resize()` đặt camera, gọi bridge. Áp camera offset vào vị trí vừa được đặt cho frame đó. Giữ root của BattleView tại gốc, camera trực giao cùng hệ tọa độ. Mesh dùng sorting order 43 để ở trên đa số sprite chiến đấu và dưới health bar. Nếu dự án đích dùng Sorting Layer riêng, đặt cùng layer với BattleView.

```csharp
// Sau khi tạo root trong constructor khi bật kit
kit = new BattleVfxBridge(root.transform) { Quality = VfxQuality.Medium };
skipKitEffects = kit.Handles; // field kiểu System.Predicate<BattleEffect>

// Sau Resize() trong RenderCore
if (kit != null)
{
    kit.Render(b, camera);
    camera.transform.position += (Vector3)kit.CameraOffset;
}

// Trong Dispose()
kit?.Dispose();
```

Trường hợp tắt kit, bỏ qua các guard dành cho kit và gọi particle player như cũ. Khi dùng `skip`, giữ điều kiện lọc riêng trước đó của prototype. Ví dụ `if (skip != null && skip(effect)) continue;` ở cả hai lượt, hoặc thêm trực tiếp vào điều kiện ở lượt thu hồi. Như vậy mỗi lượt phát chỉ có một renderer sở hữu.

## Nối sự kiện hình ảnh còn thiếu

`BattleVfxBridge` bắt sẵn đạn, beam, Tesla, text lọc qua HUD, nhóm `flash/impact/explosion/ring` cũ và một số trạng thái. Để thấy đúng từng vũ khí, gắn tag trong `Battle.cs`. Các tag đi vào tham số có tên `visual:` của `Effect`. Có thể giữ trường `weaponKind` nếu phiên bản đích đã có. Bridge ưu tiên tag và định tuyến đầy đủ bằng manifest.

| Điểm code | Sửa riêng phần trình bày |
| --- | --- |
| `Fire`, nhánh projectile | Đổi `visual` của `flash` thành `BattleVfxTags.Muzzle(w.kind)`. Giữ điểm nòng và origin thực dùng để xoay. Mốc tinh chỉnh cho Gatling 0,11 s, pháo 0,22 s, phóng tên lửa 0,20 s. |
| `Impact(Projectile, Actor)` và nhánh đạn quái trúng xe | Đổi tag thành `BattleVfxTags.Impact(w.kind)`. Với mẫu định hướng, truyền điểm cuối là `contact - direction` để bridge lấy hướng. Dùng điểm va từ phép chiếu lên đoạn `previous → position` nếu cần tốt hơn ở tốc độ đạn cao. Giữ bán kính damage, số lượng và tình trạng xuyên hiện có. |
| `Fire`, ion/sniper/Tesla | Gắn `beam-ion`, `beam-sniper`, `chain` ở đúng điểm đầu/cuối hiện tại. Nếu phát hit riêng cho ion/sniper, emit trên mục tiêu thực sự chịu sát thương. `chain` đã tự vẽ `arc` ở đầu đến. |
| `UseSkill` | Gắn `BattleVfxTags.Skill(id)` vào xung kích hoạt. Freeze gắn `frost` từng quái đã dính. Đổi beam không kích thành `beam-orbital`, dùng `blast` cho nổ orbital, `rocketblast` cho kamikaze ở vị trí game chọn. Nếu đã có blast chính của kamikaze, dùng xung kích hoạt nhỏ hoặc bỏ xung phụ để tránh trùng hình. |
| `UpdateEnemyAI` | Sau khi điều kiện kỹ năng đã thực sự áp dụng, thêm xung tương ứng với điểm tác động PvP. Chọn vị trí từ đối tượng vừa nhận hiệu ứng. Kỹ năng diện rộng nhiều nạn nhân dùng một lõi chính và chỉ thêm hit phụ khi đọc được hành động. |
| `EnemyHit`, hồi máu quái/module, quái triệu hồi | Gắn `BattleVfxTags.MonsterContact(d.behavior)` cho contact từ nơi biết `d`; không emit thêm một hit khi nhánh projectile đã ghi va chạm của chính nó. Healer gắn `heal` vào vòng có sẵn; module hồi máu và lá chắn gắn `heal` hoặc `ward` khi thật sự kích hoạt, có thể giới hạn tần suất hình ảnh. Xung `spawn` ở lượt triệu hồi hoặc điểm xuất hiện cần nhấn. |
| `Catch`, `CollectOrb`, decoy hết hạn | Thành công dùng `cage`; thất bại dùng `p-sparks` nếu game có điểm lồng rơi. Orb giữ hình và số xu, thêm `p-loot` khi collect. Decoy xuất hiện và biến mất dùng `portal` tại vị trí actor. |
| Bước chạy, đáp đất, đào đất, đổi phase | So dấu hiệu/animation hoặc cờ cũ và mới để emit đúng một lần mỗi chuyển tiếp. Dùng `p-dust`, `dust`, `warning` hoặc `rally`. Bridge đã hiển thị `danger-zone` từ `warning/charging` và `fall-shadow` từ quỹ đạo đạn rơi hiện có. |

Ví dụ một impact theo hướng. `Catalog.Color` tiếp tục nhận mã màu với dấu # như hiện tại.

```csharp
var direction = new Vector2(Mathf.Cos(b.angle), Mathf.Sin(b.angle));
Effect("impact", contact, contact - direction, w.color, size, life,
    visual: BattleVfxTags.Impact(w.kind));
```

Để tinh chỉnh mỹ thuật, sửa `effects.json` cho kích thước/mật độ. Giữ state chết/chạy/idle của quái trong `MonsterSpriteAnimator`. Đặt hit tint về màu thường sau khoảng 60–90 ms nếu thêm cho các nhóm ngoài ba mẫu cũ. Bụi bước chân phát theo khoảng không gian hoặc mốc frame animation. Engine xử lý sát thương, collider, timer, skill và hành vi trong đường gameplay hiện tại.

## Kiểm tra hình và hiệu năng

1. Chạy `Main.unity` và scene bắn thử cũ nếu máy đích có. Với Gatling, pháo và tên lửa, kiểm tra ở nhịp catalog 0,15 s / 1,60 s / 3,10 s. Quay một lần bắn tách, một lần bắn đồng thời, rồi kiểm tra với sét, hồi máu, băng và vòng khóa mục tiêu xuất hiện cùng lúc.
2. Kiểm tra Classic, PvE, Arena, bắt thường/lồng trăng, kết thúc trận, pause/resume và trở về menu. Bấm pause khi rocket đang cong, khi vụ nổ bùng và khi sét hiện. Quỹ đạo, particle, vị trí wave và camera phải đứng yên cùng battle time. Khi hạ chất lượng, ưu tiên dấu húc, mốc khóa, cảnh báo đạn rơi và lõi của đòn lớn.
3. Dùng cùng build, cấu hình máy, cấp độ và độ phân giải để đo trước/sau. Có thể dùng các profile `stress_60`, `configured_stress_60` trong `PerformanceBenchmark.cs`. So p95/p99 frame time, spike >33 ms, main/GPU, GC/frame, draw calls và số quad rơi. Đo cả lần bắn đầu sau khởi tạo và các lượt đã nóng.
4. Trong Frame Debugger, tìm draw `CatTanks VFX Atlas` và xác nhận mỗi view dùng chung một mesh, texture và material. Mốc ban đầu là 240/400/640 quad mỗi view ở Thấp/Vừa/Cao, 96 slot dành cho lớp quan trọng và tối đa 112 hạt vệt tồn tại. Ở máy cần 60 FPS, dùng 16,67 ms làm ngân sách cả frame. Mục tiêu thử cho riêng VFX là không thêm allocation mỗi frame sau khởi tạo và không tăng p95 quá khoảng 1 ms so với cấu hình gốc. Ghi số đo thực tế trước khi chốt mức mặc định.
5. Nếu GPU bị overdraw, thu mảng khói lớn, giảm lớp alpha quanh vụ nổ và giảm lifetime hạt trước. Nếu CPU bị nghẽn, kiểm tra vòng lấy mẫu trail, số effect và việc có cả player cũ lẫn bridge chạy đồng thời. Giữ phần báo hiệu còn đọc được trước khi hạ mật độ.

## Tạo lại bản mẫu

- Chạy `python VFX_Handoff/Source/build_atlas.py` với Pillow để sinh lại `atlas.png`.
- Chạy `node VFX_Handoff/Source/build_gallery.mjs` để đồng bộ `Preview.html` với JSON.
- Chạy `python VFX_Handoff/Source/check_kit.py` để kiểm tra PNG RGBA, ô atlas, coverage catalog và dữ liệu gallery.

Bản bàn giao đã được kiểm tra tĩnh và mở bằng trình duyệt. Unity Editor chưa có trên máy chuẩn bị bản kit, nên agent tích hợp cần chạy compile, Play Mode, capture và Profiler trên máy đích trước khi đưa vào bản phát hành.
