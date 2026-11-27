TANK LEVEL UP AURA
Các texture trắng RGBA để tint vàng/xanh theo demo: glow, ring, spark, arrow_up.
Glow là quad/additive ở sau tank; ring là ellipse sát đất; spark/arrow đi lên rồi fade.
Dùng Particle System hoặc animation SpriteRenderer. Chặn timer khi pause.
Điểm ApplyOnce và kết thúc Depart điều khiển bằng state/event, không dựa vào particle va chạm.
Đây là texture/source SVG và note setup, chưa là Unity prefab/material. Xem ảnh upgrade-station.png.
