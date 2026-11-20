# Danh mục VFX chiến đấu CatTanks

Phạm vi bám theo `catalog.json`, `Battle.cs`, `BattleView.cs`, `StudioBattleHUD.cs` và route hiện có trong `CombatVfxLibrary`. Bộ thử dùng nguyên inventory 40 vũ khí, 20 thú đồng hành, 20 hành vi quái và 10 kỹ năng, trong đó 6 kỹ năng xuất hiện ở Classic. Cột key chỉ tới thiết kế có sẵn trong kit. Xem [Preview.html](Preview.html) để duyệt từng lớp ở tốc độ thường hoặc bước từng frame.

## Khi nào dùng kỹ thuật nào

| Kỹ thuật | Dùng cho | Quy tắc mỹ thuật và hiệu năng |
| --- | --- | --- |
| Frame-by-frame | Lõi chớp nòng, nổ, tan khói thành hình, băng vỡ, cung chém, cổng và lồng bắt | 8 frame mỗi clip. Dùng nhịp mở hình, bùng lực, tan. Giữ pivot theo tâm sự kiện. Kéo dài frame đầu bằng thời gian đời nếu nhịp bắn cao. |
| Particle | Tia lửa rời, mảnh văng, hạt bụi bánh xe, tàn lửa, bọt độc, dấu cộng hồi máu, sao nhặt xu, khói tên lửa | Dùng glyph chung trên atlas. Tuổi hạt bám theo đồng hồ trận. Dùng khoảng tỏa, thay đổi scale và fade để tạo độ sâu. |
| Sprite + vệt | Đạn, tên lửa, plasma, mũi tên, cưa/khoan, drone | Một silhouette đậm tương phản với đường bay. Vệt lấy hướng từ projectile, khói rời lấy vị trí lịch sử. |
| Hình học động | Ion/sniper/orbital, sét Tesla, vùng rơi cối, khóa mục tiêu, chỉ hướng húc | Gắn vào điểm đầu, điểm cuối hoặc vùng dự báo bằng dữ liệu game. Giữ nét chữ và telemetry dưới một lớp HUD rõ ràng. |
| Biến đổi sprite/actor | Lá chắn duy trì, trạng thái choáng/cháy/độc/băng, đẩy lùi, lóe trúng, giật nòng, rung camera, damage text | Dùng vị trí, scale, màu hoặc animation actor sẵn có. Vận động camera ngắn, ưu tiên dấu nguy hiểm khi quá tải. |

## Vũ khí và đòn của thú đồng hành

| `Weapon.kind` | Vũ khí trong catalog | Mở phát bắn | Bay / đường tia | Trúng |
| --- | --- | --- | --- | --- |
| `shell` | `turret-0`, `pack-2` | `cannonflash` | `shot-shell` | `blast` |
| `double` | `turret-1` | `cannonflash` | `shot-shell` | `hit` |
| `rocket` | `turret-2`, `gun-2`, `pack-1`, `pack-4` | `ignition` | `shot-rocket` | `rocketblast` |
| `rapid` | `turret-3`, `gun-0`, `gun-1`, `pack-0` | `flash` | `shot-fast` | `hit` |
| `laser` | `turret-4`, `pack-10` | `pulse` | `beam-ion` | `crystal` |
| `flame` | `turret-5`, `gun-6`, `pack-5` | `fire` | `shot-fire` | `fire` |
| `mortar` | `turret-6`, `gun-7`, `pack-15` | `cannonflash` | `shot-shell` | `blast` |
| `sniper` | `turret-7`, `gun-3`, `pack-16` | `flash` | `beam-sniper` | `hit` |
| `tesla` | `turret-8`, `pack-3` | `pulse` | `chain` | `arc` |
| `drone` | `turret-9`, `gun-9` | `ignition` | `shot-drone` | `pulse` |
| `shotgun` | `gun-4` | `flash` | `shot-pellet` | `hit` |
| `plasma` | `gun-5`, `pack-11` | `pulse` | `shot-plasma` | `crystal` |
| `arrow` | `gun-8` | `flash` | `shot-arrow` | `hit` |
| `ice` | `pack-6` | `pulse` | `shot-ice` | `frost` |
| `poison` | `pack-7` | `pulse` | `shot-poison` | `poison` |
| `saw` | `pack-8` | `flash` | `shot-blade` | `slash` |
| `drill` | `pack-9` | `flash` | `shot-blade` | `hit` |
| `claw` | `pack-12` | `slash` | `shot-blade` | `slash` |
| `shield` | `pack-13` | `ward` | — | — |
| `gravity` | `pack-14` | `pulse` | `shot-gravity` | `vortex` |
| `bomb` | `pack-17` | `cannonflash` | `shot-shell` | `rocketblast` |
| `crystal` | `pack-18` | `pulse` | `shot-ice` | `crystal` |
| `heal` | `pack-19` | `heal` | — | — |
| `web` | Đạn của quái | `web` | `shot-web` | `web` |
| `enemy` | Đạn của quái | `flash` | `shot-enemy` | `hit` |

Các thú đồng hành đọc `monsterBook[].weapon.kind` và dùng cùng route với vũ khí trùng loại. Hồi máu và lá chắn tự động của module phát xung ở điểm tác động. Tuyến ion, sniper và Tesla xử lý ngay, nên lấy điểm va chạm từ mục tiêu thực sự chịu đòn.

## Kỹ năng và PvP

| `Skill.id` | Khi kích hoạt | Hiệu ứng sau | Điểm neo |
| --- | --- | --- | --- |
| `heal` | `heal` | Dấu cộng xanh, xóa glyph độc theo buff hiện có | Xe hoặc quái được hồi trong Arena |
| `barrage` | `ignition` | `shot-rocket` + `rocketblast` | Projectile thật và mục tiêu; Arena dùng điểm trúng các unit bị tính sát thương |
| `shield` | `ward` | `shield-loop`, `status-protect` khi có phản hồi đỡ | Xe; Arena có màu phe địch hoặc gắn icon trên quái |
| `rally` | `rally` | `rally-loop` | Chân xe hoặc bên cạnh quân được buff |
| `freeze` | `frost` | `slow-loop` | Từng quái trúng ở PvE, marker băng trên xe/đơn vị chịu ảnh hưởng ở PvP |
| `drone` | `pulse` | `shot-drone` và `pulse` trúng | Vị trí gọi và đạn thật; Arena gắn phản hồi vào mục tiêu bị đánh |
| `orbital` | `pulse` | `beam-orbital` + `blast` | Từ đỉnh màn đến điểm trúng. Chỉ hiện dấu vùng sớm nếu game có đồng hồ báo trước. |
| `decoy` | `portal` | Sprite mồi nhử, xung khi biến mất | Actor decoy PvE; Arena xung trên cụm quái có decoy HP |
| `magnet` | `drop` | `pick-coin` nếu có quỹ đạo hiển thị, `p-loot` và số xu | Orb thật đến xe theo vị trí × tuổi hiển thị |
| `kamikaze` | `rocketblast` tại vụ nổ chính | `p-sparks` khi cần phản hồi trúng phụ | Vị trí game chọn; tránh nhân cùng một blast cho mỗi quái trong bán kính |

## Quái và hành vi

| Quái | Hành vi | Mẫu | Ứng dụng |
| --- | --- | --- | --- |
| Kiến tiên phong `bug-0` | `swarm` | `hit` / `p-dust` | Bụi bước nhóm thấp, ít hạt. Dùng animation đi/bám hiện tại. |
| Sói bóng đêm `beast-0` | `wolf` | `slash` / `p-dust` | Vệt vuốt theo nhịp animation và bụi bước nhỏ. |
| Ong sát thủ `bug-4` | `bee` | `shot-enemy` / `hit` | Đạn hồng, gợn rất nhẹ dưới cánh. |
| Bọ sừng đỏ `bug-1` | `armor` | `p-sparks` / `status-protect` | Tia kim loại ở chỗ báo đỡ đòn, giáp có glyph ngắn. |
| Lợn rừng cuồng nộ `beast-1` | `charge` | `warning` / `danger-zone` / `dust` | Chevron từ cờ warning, khi húc chuyển sang bụi thấp. |
| Nhện giăng tơ `bug-2` | `web` | `shot-web` / `web` / `slow-loop` | Bọc tơ, bung mạng lưới, biểu hiện làm chậm xe. |
| Bọ ngựa đao `bug-3` | `leap` | `slash` / `dust` | Vệt lưỡi và bụi chân khi đáp. |
| Gấu cuồng chiến `beast-2` | `enrage` | `rally` / `rally-loop` / `hit` | Xung nóng ở lần vượt ngưỡng HP, marker duy trì nếu cần. |
| Ấu trùng đất `bug-5` | `burrow` | `dust` / `p-dust` | Đám bụi khi đổi cờ burrowed và khi trồi lên. |
| Hổ kiếm `beast-4` | `tiger` | `slash` / `status-protect` | Gắn vệt vuốt vào frame tấn công. Biểu hiện phòng thủ theo nhịp giảm sát thương. |
| Bọ cạp độc `bug-6` | `venom` | `shot-poison` / `poison` / `poison-loop` | Đạn độc và giọt xanh vàng. |
| Khỉ băng `beast-3` | `frost` | `shot-ice` / `frost` / `slow-loop` | Mũi băng có mặt phẳng cứng, vỡ thành mảnh nhỏ. |
| Bướm thôi miên `bug-7` | `moth` | `shot-poison` / `poison` | Góc quạt riêng mỗi projectile. Khói thấp giúp nhìn cả ba viên. |
| Rùa dung nham `beast-5` | `lava` | `fire` / `p-embers` / `burn-loop` | Vệt cháy thấp. Khóa nhịp emit riêng cho sát thương gần theo tick. |
| Tê giác bọc giáp `beast-6` | `rhino` | `warning` / `danger-zone` / `dust` | Chevron giữ theo hướng húc. Bụi đáp khi chạm. |
| Bọ tinh thạch `bug-8` | `crystal` | `crystal` / `status-protect` / `p-sparks` | Chớp giáp riêng khi giảm sát thương ion/plasma. |
| Đại bàng sư tử `beast-7` | `griffin` | `shot-enemy` / `hit` / `dust` | Đạn quạt và nhịp cánh, bụi khi đáp nếu có tín hiệu. |
| Hươu cổ thụ `beast-9` | `healer` | `heal` | Vòng hồi máu quanh hươu. Có thể thêm một đốm trên mục tiêu được hồi nếu đủ ngân sách. |
| Bọ chúa giáp gai `bug-9` | `queen` | `shot-poison` / `poison` / `spawn` | Nổ độc và xung khi gọi kiến. |
| Bạo long hắc nguyệt `beast-8` | `boss` | `shot-fire` / `fire` / `spawn` / `death` | Đạn hỏa, xung triệu hồi hoặc hạ gục to vừa phải. Marker vùng nguy hiểm chỉ hiện khi có dữ liệu vùng. |

## Trạng thái và phản hồi chung

| Tín hiệu | VFX |
| --- | --- |
| Nhắm và bắn | Pivot nòng, recoil, muzzle tương ứng, âm bắn hiện có |
| Trúng đòn | Mảnh bung ngay chỗ va, lóe màu actor ngắn, animation hit; knockback chỉ theo logic hiện tại |
| Bị hạ / biến mất | `death`, `dust`; dùng `portal` khi decoy tắt, `cage` khi bắt thành công |
| Bắt quái / lồng trăng | `cage` + sao, tông vàng cho lồng thường và có thể tint lam bạc cho lồng trăng; thất bại dùng `p-sparks` nhỏ tại lồng và phản hồi hiện có |
| Khóa mục tiêu | `target-lock`, ăn theo tâm và cỡ quái |
| Xu chiến lợi phẩm | Orb hiện có hoặc `pick-coin`, khi nhặt `p-loot` và text |
| Buff/debuff thường trực | `shield-loop`, `rally-loop`, `stun-loop`, `slow-loop`, `burn-loop`, `poison-loop`, `status-protect` |
| Mở đợt / xuất hiện / triệu hồi | `spawn` ở quái cần nhấn hoặc đi theo cụm; `p-dust` ở đất |
| Cảnh báo | `danger-zone`, `warning`, `fall-shadow`; giữ hitbox dễ đọc và dùng một dấu rõ trước đòn |
| Chạy ngang, đáp, chui đất | `p-dust` và `dust`; giảm mật độ khi quái đứng đông |
| Sát thương, hồi phục, số xu | Giữ text nổi qua pool UI hiện có. Trạng thái được đọc bằng hình và màu đi kèm. |
| Chuyển wave, thắng, thua, rút lui | Tiếp tục HUD và popup hiện có. Xung `spawn` dùng cho wave hoặc `rally` dùng cho thắng nếu muốn thêm nhấn hình. |

## Mẫu có trong kit

| Key | Hiệu ứng | Kỹ thuật | Frame atlas | Hạt tối đa / lần ở Cao | Giây mẫu |
| --- | --- | --- | --- | --- | --- |
| `flash` | Chớp nòng Gatling | Frame-by-frame | 8 | 0 | 0.11 |
| `cannonflash` | Chớp nòng đại bác | Frame-by-frame + particle | 8 | 2 | 0.22 |
| `ignition` | Phụt nòng tên lửa | Frame-by-frame + particle | 8 | 4 | 0.20 |
| `hit` | Va chạm kim loại | Frame-by-frame + particle | 8 | 5 | 0.19 |
| `blast` | Nổ đại bác / cối | Frame-by-frame + particle | 8 | 9 | 0.52 |
| `rocketblast` | Nổ chùm tên lửa | Frame-by-frame + particle | 8 | 7 | 0.58 |
| `fire` | Lửa và cháy | Frame-by-frame + particle | 8 | 8 | 0.42 |
| `frost` | Băng kết tinh / vỡ | Frame-by-frame + particle | 8 | 8 | 0.62 |
| `poison` | Độc nổ loang | Frame-by-frame + particle | 8 | 6 | 0.66 |
| `arc` | Điện trúng đích | Frame-by-frame + particle | 8 | 4 | 0.30 |
| `vortex` | Co sập trọng lực | Frame-by-frame + particle | 8 | 7 | 0.62 |
| `slash` | Vệt chém / cào | Frame-by-frame | 8 | 0 | 0.28 |
| `crystal` | Vỡ tinh thể / plasma | Frame-by-frame + particle | 8 | 5 | 0.38 |
| `web` | Tơ giăng | Frame-by-frame | 8 | 0 | 0.42 |
| `heal` | Sửa chữa / hồi máu | Frame-by-frame + particle | 8 | 5 | 0.68 |
| `ward` | Xung lá chắn | Frame-by-frame + particle | 8 | 4 | 0.46 |
| `rally` | Cờ tiếp viện | Frame-by-frame + particle | 8 | 5 | 0.48 |
| `cage` | Lồng bắt quái | Frame-by-frame + particle | 8 | 6 | 0.72 |
| `portal` | Mồi nhử / triệu hồi | Frame-by-frame + particle | 8 | 6 | 0.55 |
| `dust` | Bụi đáp / đào đất | Frame-by-frame + particle | 8 | 6 | 0.47 |
| `death` | Tan biến khi hạ gục | Frame-by-frame + particle | 8 | 5 | 0.56 |
| `drop` | Xu rơi / nam châm | Frame-by-frame + particle | 8 | 4 | 0.46 |
| `spawn` | Quái xuất hiện / đợt mới | Frame-by-frame + particle | 8 | 5 | 0.58 |
| `warning` | Cảnh báo húc / vùng nguy hiểm | Frame-by-frame | 8 | 0 | 0.80 |
| `pulse` | Xung năng lượng | Frame-by-frame + particle | 8 | 4 | 0.42 |
| `shot-fast` | Đạn Gatling | Sprite + vệt/hạt | 1 | 2 | 0.13 |
| `shot-shell` | Đạn đại bác | Sprite + vệt/hạt | 1 | 2 | 0.28 |
| `shot-rocket` | Tên lửa | Sprite + vệt/hạt | 1 | 7 | 0.70 |
| `shot-pellet` | Chùm đạn shotgun | Sprite / biến đổi | 1 | 0 | 0.25 |
| `shot-plasma` | Cầu plasma | Sprite + vệt/hạt | 1 | 4 | 0.55 |
| `shot-arrow` | Mũi tên | Sprite / biến đổi | 1 | 0 | 0.48 |
| `shot-fire` | Luồng lửa | Sprite + vệt/hạt | 1 | 4 | 0.34 |
| `shot-ice` | Mũi băng | Sprite + vệt/hạt | 1 | 3 | 0.52 |
| `shot-poison` | Giọt độc | Sprite + vệt/hạt | 1 | 4 | 0.55 |
| `shot-gravity` | Hạt trọng lực | Sprite + vệt/hạt | 1 | 6 | 0.56 |
| `shot-blade` | Cưa / khoan / móng | Sprite + vệt/hạt | 1 | 2 | 0.40 |
| `shot-drone` | Drone bám đuổi | Sprite + vệt/hạt | 1 | 3 | 0.65 |
| `shot-web` | Bọc tơ | Sprite / biến đổi | 1 | 0 | 0.45 |
| `shot-enemy` | Đạn quái | Sprite + vệt/hạt | 1 | 2 | 0.50 |
| `beam-ion` | Đường tia ion | Hình học theo điểm đầu/cuối | 1 | 0 | 0.18 |
| `beam-sniper` | Đường đạn bắn tỉa | Hình học theo điểm đầu/cuối | 1 | 0 | 0.18 |
| `beam-orbital` | Cột không kích | Hình học theo điểm đầu/cuối | 1 | 5 | 0.50 |
| `chain` | Tia sét bám mục tiêu | Hình học theo điểm đầu/cuối | 1 | 0 | 0.22 |
| `shield-loop` | Mái chắn duy trì | Sprite / biến đổi | 1 | 0 | 0.80 |
| `target-lock` | Mốc khóa mục tiêu | Sprite / biến đổi | 1 | 0 | 0.80 |
| `pick-coin` | Xu bay / nhặt | Sprite / biến đổi | 1 | 0 | 0.80 |
| `stun-loop` | Choáng | Sprite / biến đổi | 1 | 0 | 0.65 |
| `slow-loop` | Làm chậm / đóng băng | Sprite / biến đổi | 1 | 0 | 0.80 |
| `burn-loop` | Đang cháy | Sprite / biến đổi | 1 | 0 | 0.48 |
| `poison-loop` | Đang nhiễm độc | Sprite / biến đổi | 1 | 0 | 0.70 |
| `rally-loop` | Đang tăng tốc | Sprite / biến đổi | 1 | 0 | 0.70 |
| `danger-zone` | Dấu hướng tấn công | Sprite / biến đổi | 1 | 0 | 0.62 |
| `fall-shadow` | Vùng đạn cối sắp rơi | Sprite / biến đổi | 1 | 0 | 0.56 |
| `status-protect` | Khiên giáp/giảm sát thương | Sprite / biến đổi | 1 | 0 | 0.52 |
| `p-dust` | Hạt bụi bánh xe / bước chân | Particle | — | 5 | 0.36 |
| `p-sparks` | Tia lửa va chạm / trúng giáp | Particle | — | 6 | 0.20 |
| `p-embers` | Đốm tàn lửa | Particle | — | 5 | 0.30 |
| `p-loot` | Sao nhặt xu | Particle | — | 5 | 0.39 |

Số giây trong bảng dùng cho gallery và làm mốc tinh chỉnh khi gắn sự kiện mới. Khi tích hợp, vòng đời sự kiện thật lấy từ `BattleEffect.life / maxLife`; đạn, buff và marker dùng thời gian sống của đối tượng.
