BỘ NÂNG CẤP TANK — BÁNH ĐỊA HÌNH / V1

1. Bánh địa hình thiên tốc độ: thay TOÀN BỘ cụm xích bằng hai bánh lốp gai
   nhìn ngang, với gầm và hai giảm xóc riêng. Không còn bánh nhỏ trong xích.
2. Machine gun nặng: mỗi phát mạnh hơn, nhịp bắn chậm hơn.
3. Pháo hạng nặng: nòng lớn, sát thương mạnh hơn.
4. Mortar độc: tím–xanh, gây độc theo thời gian.

master/: 5 PNG RGBA độ phân giải cao — một mẫu bánh, một gầm giảm xóc,
3 module súng. Hai bánh dùng chung artwork và quay độc lập quanh tâm.
Tank_Upgrade_Parts.psd / .ora: 5 layer riêng; PNG board là bản xem tất cả.
metadata/modules_manifest.json: kích thước, điểm xoay, điểm phát đạn.

native_fits/: bộ căn sẵn cho tank gốc, tank đỏ và tank xanh, mỗi bộ 6 PNG.
Gỡ 01_track_chassis và cả 4 layer bánh cũ 02_wheel_01–05_wheel_04.
Thêm 01_offroad_chassis, 02_offroad_wheel_rear, 03_offroad_wheel_front
ở phía SAU lớp thân và khung; bánh quay, gầm giảm xóc giữ tĩnh.
Thay 06_main_cannon_barrel, 08_roof_machine_gun, 09_mortar_barrel.
Giữ thân, nội thất, khung viền và đai kẹp mortar của bộ tank đã có.
placement_manifest.json ghi vị trí, pivot và danh sách layer cần gỡ.
Ảnh preview có thêm lề để chứa bánh lớn và nòng pháo dài; JSON dùng tọa độ
canvas tank gốc, không cộng lề preview. Khung viền vẫn che phía trước nhân vật.

previews/: ảnh bốn loại nâng cấp, ảnh ghép thử trên cả ba thân tank và GIF.
GIF minh họa bánh địa hình quay nhanh, machine gun giật thưa, pháo và mortar
recoil. Các tire master có toàn bộ lốp, gai và mâm; không còn xích trong GIF.

Đây là bộ hình và điểm gắn, chưa chỉnh project Spine/Unity. Tốc độ,
sát thương, nhịp bắn, thời gian và lượng sát thương độc đặt trong game;
chưa gán số cân bằng. Ba module súng giữ thiết kế đã thống nhất ở trên.
