# MEOW BASTION — Pháo đài trăng xanh

Game xe tăng mèo 2.5D chạy màn hình ngang bằng **Three.js 0.180.0**, dựng theo `Moonlit Cat-Dog Tank Battle.png`: thung lũng đêm xanh, trăng lớn, chiến xa vàng bên trái, HUD và hàng kỹ năng phía dưới. Quái vật lấy từ cả hai bộ ảnh quái gốc.

## Chạy game

Để xem animation, bấm đúp **XEM ANIMATION.lnk** trong thư mục project (hoặc `XEM_ANIMATION.cmd`). Nó tự bật server và mở thẳng trang sói vẽ từng frame, có chọn hành động và so sánh ảnh gốc.

Trên Windows, bấm đúp **CHOI GAME.lnk** ngay trong thư mục `tank_cat` trên Desktop. Shortcut tự bật server local ẩn nếu chưa chạy, dùng lại server khi đã chạy và mở game trong trình duyệt. **CHOI_GAME.cmd** cũng dùng cùng launcher. Cần Node.js 20+; hai file Three.js đã có sẵn trong `public/vendor/`, không cần npm install hay tải mạng.

Hoặc chạy trong terminal:

```sh
npm run vendor
npm run dev
```

Mở **http://localhost:5173**. `npm run vendor` chỉ tải bản Three.js cố định vào `public/vendor/`. Khi chưa có bản local, trình duyệt thử tải từ jsDelivr và unpkg. Các ảnh, âm thanh tổng hợp và gameplay đều chạy tại máy, không có tài khoản hay backend.

## Điều khiển

- **A / D**, mũi tên: di chuyển xe. Mèo bộ binh chạy theo vị trí xe.
- **Bấm vào quái**: khóa mục tiêu cho cả đội; bấm chỗ trống để tự ngắm lại. Súng vẫn tự chọn mục tiêu khác nếu quái được khóa ngoài tầm bắn.
- **1** tiếp viện (tăng tốc bắn), **2** sửa chữa, **3** bão băng, **4** drone.
- **Q** mưa tên lửa, **E** lá chắn.
- **P / Space** tạm dừng; **B** bách khoa; **F** toàn màn hình.
- Thiết bị cảm ứng có nút di chuyển; nên xoay ngang.

## Nội dung màn chơi

- **5 tầng**, mỗi tầng có **một mèo cố định**, cụm súng xoay độc lập theo mục tiêu, có giật nòng và chớp lửa. Mèo không xoay cả người theo nòng.
- **10 mèo bộ binh** mang đủ 10 loại vũ khí cá nhân, chia tổ Alpha/Bravo, 5 con hoạt động cùng lúc, thay tổ mỗi 18 giây; có chân chạy, mũ, đồng phục và súng ngắm độc lập.
- **10 họ tháp pháo** (hai bộ ảnh tháp là hai biến thể ngoại hình của cùng 10 họ), thay cấu hình 10 giây/lần; **20 module phụ** chia 4 bệ, luân phiên tự động. Tất cả 40 vũ khí được dùng trong cùng một màn.
- **20 loài quái**, mỗi loài có kích thước và hành vi riêng: bầy đàn, tăng tốc, giáp, nhảy, chui đất, bay, độc, băng, hồi máu, triệu hồi, v.v. Quái thỉnh thoảng mở cổng phục kích gần xe để vũ khí ngắn tầm tham chiến.
- **6 đợt**; trùm cuối Bạo long Hắc Nguyệt có 60.000 máu, phun lửa, triệu hồi và cuồng nộ khi yếu máu. Tiêu diệt tất cả để thắng; xe hết máu thì thua. Có chơi lại.
- Đạn thật có đường bay, va chạm theo đoạn, nổ lan, dẫn đường, xuyên giáp/xuyên mục tiêu, tia laser, sét chuyền, cháy, độc, chậm và choáng. Khiên giảm 94% sát thương, sửa chữa xóa độc. Năng lượng hồi theo thời gian và khi hạ quái.
- Hiệu ứng gồm tia sáng, hạt lửa và bụi, vòng nổ, mảnh vỡ, số sát thương, rung màn hình, ánh sáng khiên và âm thanh Web Audio tổng hợp. Có tắt âm thanh, tạm dừng và bách khoa xem đầy đủ nhân vật/vũ khí/quái.

## Mã nguồn và kiểm tra

`src/data.js`: thông số và hành vi; `src/battle.js`: mô phỏng độc lập khung hình; `src/visuals.js`: Three.js, cảnh và hiệu ứng; `src/game.js`: vòng lặp, điều khiển và UI; `src/audio.js`: âm thanh.

```sh
npm run check
npm run build
```

`check` chạy kiểm tra cú pháp, sprite, toàn bộ màn chơi bằng seed cố định, xác nhận đủ 20 loài/40 vũ khí, ngắm mục tiêu, kỹ năng, khiên và làm chậm. `build` xuất thư mục tĩnh `dist/`. Hai file Three.js local đã có trong `public/vendor/`; chỉ cần `npm run vendor` nếu thiếu chúng.

130 sprite trong `public/assets/` được cắt từ 12 bộ ảnh, giữ nguyên ảnh gốc ở thư mục chính. Hai ảnh sơ đồ và concept là tài liệu thiết kế. `scripts/prepare_assets.py` có thể tạo lại sprite bằng Python + Pillow; không cần Python để chơi. `public/assets/contact.jpg` dùng để kiểm tra các lát cắt.

Font có bản dự phòng trên máy khi không tải được Google Fonts.

## Mẫu cắt ảnh và gắn xương 2D

**Animation sói đã tích hợp vào game:** Sói bóng đêm (`beast-0`, xuất hiện từ đợt 1) dùng 24 frame mới. Chạy theo vận tốc thực; đứng khi dừng/choáng, cắn theo sự kiện melee, trúng đòn theo damage. Một clip cắn được phát hết; hit có thời gian nghỉ để nhiều phát đạn không kẹt ở frame đầu. Pause dừng animation. Scale cố định cho cả 24 frame và chân bám mặt đất; không còn co giãn sprite procedural trên sói. Thông số chiến đấu không đổi.

**Trang so sánh từng frame:** `http://127.0.0.1:5173/wolf-frames-preview.html`. Sheet 24 hình nguyên con, 6 frame cho mỗi động tác thở/chạy/tấn công/trúng đòn, tham chiếu trực tiếp sói gốc để giữ dáng thấp và cổ ngắn. Có ảnh gốc bên cạnh, frame trước/sau, chọn từng frame, tốc độ và nền sáng. Bản GIF ghép 4 động tác: `public/assets/wolf-frames/wolf-actions.gif`; GIF riêng mỗi động tác nằm cùng thư mục.

`public/assets/wolf-frames-v1.png` là sheet sinh bằng imagegen, giữ nguyên; prompt ở `public/assets/wolf-frames-prompt.md`. `scripts/assemble-wolf-frames.py` tách theo silhouette đầy đủ, giữ một scale và tọa độ theo ô cho toàn bộ frame (không tự fit từng ảnh), giữ cả mõm tràn ô ở frame cắn, loại haze alpha thấp ở bản tách. Ảnh gốc không sửa. Có dao động nhỏ về nét vẽ/tỉ lệ giữa các hình sinh; không bảo đảm trùng tuyệt đối. Đã xem bản so sánh và GIF trong Chrome. `node scripts/check-wolf-frames.mjs` kiểm tra 24 frame khác nhau, playback/pause/bước frame, màn nhỏ, GIF trả đúng MIME và lỗi trình duyệt.

Mẫu quái vật dùng ảnh vẽ lại: mở `http://127.0.0.1:5173/monster-preview.html` khi server local đang chạy. Sói tím có 6 bộ phận vẽ riêng trên atlas RGBA `public/assets/wolf-rig-atlas-v1.png`, 9 khớp, động tác thở/chạy tại chỗ/tấn công/trúng đòn. Phần khuất được vẽ đủ để chồng kín khớp; bản mẫu có nền sáng, tốc độ, xương và tách lớp. Đã trực tiếp xem các tư thế trên Chrome, gồm 8 mẫu trong chu kỳ chạy/tấn công; không thấy khe hở hoặc mép cắt thẳng ở các tư thế đã kiểm tra. Chưa tích hợp vào gameplay. Chạy `node scripts/check-monster.mjs` để kiểm tra và chụp lại ảnh. Prompt tạo ảnh bằng built-in imagegen được lưu trong `public/assets/wolf-rig-prompt.md`.

Chạy `npm.cmd run dev`, mở `http://127.0.0.1:5173/animation-preview.html` để xem mèo cam với 7 khớp và 4 động tác: thở, bước tại chỗ, giật khi bắn, trúng đòn. Có tạm dừng, hiện xương và tách lớp. Các mảnh là polygon lấy texture trực tiếp từ sprite gốc, gắn cứng vào `THREE.Bone`; chưa có skinning mềm hay vẽ bù phần khuất. Không sửa ảnh nguồn và chưa tích hợp rig vào gameplay.

Đã render và xem ảnh mẫu trong Chrome ở 1200×900 và 740×480. Kiểm tra bằng `node scripts/check-animation.mjs` khi server đang chạy (dùng Chrome và Playwright local theo đường dẫn trong SESSION_NOTES.md). Việc này chỉ xác minh trang mẫu animation; trạng thái xác minh gameplay bên dưới vẫn giữ nguyên.

## Trạng thái xác minh trong môi trường phát triển

Kiểm tra tự động mô phỏng đủ sáu đợt đã qua: 20 loài xuất hiện và 40 vũ khí được triển khai; một lượt có khóa trùm và dùng kỹ năng kết thúc sau khoảng 386 giây với khoảng 15.185 máu. Bản build tĩnh đã tạo thành công.

Gameplay WebGL đã render trong Chrome bằng Three.js local và đã trực tiếp xem ảnh ở 1600×900, 900×506. `node scripts/check-game-animation.mjs` (khi server đang chạy) kiểm tra sói mới trong lượt chơi thường và cả 4 trạng thái trong cảnh có kiểm soát dùng Battle/renderer thật, giữ tỉ lệ/đường chân, pause, phục hồi sau cắn, dọn khi chết, reset renderer, texture PNG đúng frame và không pageerror. Ảnh xác minh ở `test-results/game-wolf-*.png`. Đây là xác minh render và tích hợp animation; chưa chơi thủ công toàn màn hoặc kiểm chứng toàn bộ điều khiển, bố cục và hiệu năng cuối màn.
