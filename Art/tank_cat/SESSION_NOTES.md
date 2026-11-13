## Mới nhất: tiếp tục chiến đấu khi chuyển cửa sổ/tab ngày 12/11/2026

- Người dùng xác nhận muốn game vẫn chạy khi trình duyệt mất focus hoặc chuyển tab. `src/game.js` bỏ tự pause khi blur, vẫn xóa phím và cảm ứng di chuyển đang giữ.
- Tách tick chiến đấu khỏi requestAnimationFrame, thêm timer 250ms cho tab ẩn. Tính đủ thời gian đã trôi qua khi timer nền bị trình duyệt giảm tần suất; khi trở lại tab đồng bộ đồng hồ, không chạy hai lần. Nút pause/phím P và bách khoa vẫn dừng game.
- `node scripts/check-background.mjs` PASS trong Chrome headless: blur, nhả di chuyển, giả lập document.hidden và ngừng callback RAF, timer trễ, pause/resume, bách khoa; không pageerror. Đây là kiểm tra tab ẩn giả lập, chưa thao tác chuyển tab/cửa sổ bằng tay hoặc kiểm tra treo tab dài hạn. Khi trình duyệt đóng, tab bị đóng băng/discard hoặc máy ngủ, JavaScript không thể tiếp tục chạy.
- `npm.cmd run check` PASS: thắng 532s, 121 kills, đủ 20 loài và 40 vũ khí, còn 14758 HP. Build được chạy lại để cập nhật dist.
# Bàn giao phiên làm việc — MEOW BASTION

## Mới nhất: chỉnh kích thước và chỉ số quái ngày 11/11/2026

- Người dùng sửa yêu cầu: đạn chỉ to 2 lần so với ban đầu; quái to 2 lần, máu 1.5 lần so với bản trước thay đổi này, sát thương quái còn 0.8 lần. Đã thông báo cách hiểu damage là sát thương của quái. Giữ hiệu ứng cháy nổ mạnh của lượt trước và sát thương vũ khí mèo.
- `src/visuals.js`: scale đạn 4 → 2; laser/sniper dày 2 lần so với ban đầu. `src/data.js`: `MONSTER_BALANCE` áp dụng cho đủ 20 loài, cả boss và quái triệu hồi, bảng bách khoa dùng cùng dữ liệu. Máu cũ của quái thường đã nhân 2 từ trước, nay nhân tiếp 1.5; boss 60000 → 90000. Size dùng chung cho hình ảnh, hitbox, khoảng tiếp cận và chiều cao đứng. `src/battle.js`: giảm cả sát thương độc theo thời gian và xung lửa dung nham về 80%, cùng đòn cận chiến/đạn quái qua data.
- `npm.cmd run check` PASS: thêm kiểm tra chỉ số, chân chạm đất, damage cận chiến/tầm xa/độc/dung nham; mô phỏng thắng 532s, 121 kills, đủ 20 loài và 40 vũ khí, còn 14758 HP. `node scripts/preview-effects.mjs` và `node scripts/check-game-animation.mjs` PASS trong Chrome WebGL; đã trực tiếp xem screenshot `test-results/effects-large.png` với đạn 2x và quái 2x. `npm.cmd run build` PASS. Chưa chơi thủ công hết màn hoặc đo FPS.

## Mới nhất: tăng cường hiệu ứng chiến đấu ngày 11/11/2026

- Người dùng yêu cầu đạn lớn 3–5 lần và cháy nổ dữ dội hơn. `src/visuals.js` tăng scale hiển thị mọi viên đạn lên 4 lần, tia laser/sniper dày 4 lần, chớp đầu nòng và vệt lửa lớn hơn, cháy trên quái nhiều hạt hơn. Giữ nguyên sát thương, hitbox, bán kính gây sát thương, tốc độ đạn và cân bằng.
- Vụ nổ lan và quái chết có chớp sáng, 4 lớp lửa đổi từ vàng sang đỏ, khói tối bay lên, vòng xung kích và tia lửa văng xa; tăng rung camera có giới hạn. Pool hạt tăng 1400 → 2400, vẫn dùng một point cloud. Không sửa ảnh nguồn, bố trí mèo hay animation sói.
- Đã render Chrome WebGL và trực tiếp xem `test-results/effects-large.png`, `effects-smoke.png`, cùng screenshot gameplay tự nhiên `game-wolf-natural.png`. QA hiệu ứng có cảnh kiểm soát ở 1600×900 và 900×506; xác nhận scale đạn 4 lần, không pageerror, geometry vụ nổ được giải phóng. Script tái hiện: `node scripts/preview-effects.mjs`.
- `npm.cmd run check`, `node scripts/check-game-animation.mjs`, `node scripts/preview-effects.mjs`, `npm.cmd run build`: PASS. Chưa đo FPS hoặc chơi thủ công toàn màn với hiệu ứng mới.

## Mới nhất: shortcut xem animation

- `XEM ANIMATION.lnk` ở gốc project mở thẳng `/wolf-frames-preview.html`, tự dùng lại/bật server qua launcher chung. `XEM_ANIMATION.cmd` là bản dự phòng.
- `scripts/open-game.ps1` thêm `-View animation` (và các route được cho phép game/wolfRig/catRig); mặc định vẫn mở game. Đã kiểm tra route animation với `-NoBrowser`, HTTP200 và cấu hình shortcut; không tuyên bố đã bấm mở cửa sổ desktop bằng công cụ.

## Mới nhất: shortcut mở game

- Đã tạo `CHOI GAME.lnk` ở thư mục gốc project (thư mục tank_cat trên Desktop). Target Windows PowerShell, chạy ẩn `scripts/open-game.ps1`, working directory là project, biểu tượng Chrome.
- Launcher nhận diện server đúng project qua `/__meow_health` (app/root/pid), tái sử dụng server đang chạy hoặc bật Node ẩn, chờ sẵn sàng rồi mở trình duyệt. Không tải mạng. `CHOI_GAME.cmd` gọi cùng launcher. Lỗi hiện popup.
- Đã thử cold-start với `-NoBrowser`, kiểm tra health endpoint trả đúng project; thử lần hai giữ nguyên PID22120, không bật thêm server; kiểm tra target .lnk tồn tại và cú pháp server qua. Chưa xác minh thao tác bấm shortcut mở cửa sổ trình duyệt trong desktop session; test khởi chạy .lnk qua tool bị sandbox chặn. File shortcut đã tạo xong, không cần thêm quyền để giao file cho người dùng.
- Server hiện chạy ẩn do launcher, PID22120 tại localhost5173, thay cho exec session66264 đã dừng. Có thể chỉ bấm shortcut cho những lần sau.

## Mới nhất: animation sói đã vào gameplay ngày 11/11/2026

- Người dùng yêu cầu đưa animation mới vào game. Đã tích hợp 24 PNG cho riêng Sói bóng đêm (`beast-0`, MONSTERS[1]), xuất hiện từ đợt 1. Không đổi balance, lịch spawn, bộ binh, mèo cố định mỗi tầng hoặc ảnh gốc.
- `src/enemy-animation.js`: cấu hình 4 clip và controller dựa trên tuổi mô phỏng; idle/run theo chuyển động và stun, attack từ melee, hit từ damage. Clip cắn có ưu tiên; hit nghỉ 1.05s để rapid fire không reset liên tục frame đầu. Chạy theo vận tốc thực. Pause không tăng thời gian animation, chết giữ frame cuối rồi dùng fade cũ.
- `src/game.js` preload 24 PNG cùng sprite cũ. `src/visuals.js` chia sẻ texture preload và đổi map theo frame, không cấp texture mỗi frame; cùng scale 272/235 và tỉ lệ 256/272 cho mọi frame, gốc chân local y217 bám mặt đất175. Sói quay về phía xe. Bỏ procedural bob/squash/rotation của sói đang sống. Loài khác dùng renderer cũ.
- `window.__MEOW__.state.animations` bổ sung snapshot chỉ đọc phục vụ QA.
- ĐÃ render gameplay WebGL và trực tiếp xem screenshot Chrome thật 1600×900 và 900×506 bằng vendor local. Lượt chơi thường đã nạp/phát animation sói; cảnh kiểm soát với Battle và renderer production đã kiểm tra đủ idle/run/attack/hit, PNG map đúng state/frame, tỉ lệ/đường chân không đổi, pause, recovery, chết/dọn, renderer clear/recreate. Không pageerror. `node scripts/check-game-animation.mjs` PASS; ảnh `test-results/game-wolf-*.png`.
- `npm.cmd run check` vẫn PASS (thắng386s,99 kills,20 loài,40 vũ khí,15185HP), bổ sung kiểm tra file animation và cú pháp module mới. Build cần/đã chạy lại trong lượt này để chứa code và 24 PNG.
- Các ghi chú cũ nói chưa render gameplay hoặc chưa tích hợp frame chỉ là lịch sử. Chưa kiểm chứng chơi thủ công hết màn, toàn bộ điều khiển và hiệu năng cuối màn. Không khẳng định game hoàn thiện hoàn toàn.

## Bổ sung mới nhất: frame-by-frame ngày 11/11/2026

- Người dùng chê mẫu rig và yêu cầu vẽ từng frame, ghép chuyển động, giữ tỉ lệ/hình dạng quái. Đã làm mẫu frame-by-frame riêng thay vì mở rộng rig. Không xóa các bản trước.
- Dùng built-in imagegen với ảnh tham chiếu DUY NHẤT `public/assets/beast-0.png` để tránh dáng cổ cao của rig trước. Sheet `public/assets/wolf-frames-v1.png` 1536×1024 RGBA, 6 cột × 4 hàng: thở/chạy/tấn công/trúng đòn, 6 frame mỗi động tác. Prompt chính xác và quy trình tại `public/assets/wolf-frames-prompt.md`. Giữ nguyên nguồn sinh và mọi ảnh gốc.
- `scripts/assemble-wolf-frames.py`: nhận diện 24 silhouette alpha nối liền; lấy đủ bounds từng con (mõm attack frame 4 tràn sang ô kế 5px), tạo 24 PNG trong `public/assets/wolf-frames/`, dùng gốc ô cố định và canvas 272×256 với lề an toàn 8px, không scale riêng từng frame. Chỉ loại haze alpha thấp ở bản tách, không vẽ thêm hay biến dạng bộ phận. Xuất manifest, 4 GIF riêng và `wolf-actions.gif` ghép 4 hành động (hai vòng mỗi hành động).
- `/wolf-frames-preview.html`, `src/wolf-frames-preview.js`: Canvas2D, ảnh gốc bên trái và frame bên phải ở scale cố định, 4 động tác, pause/play, frame trước/sau, click thumbnail, tốc độ, nền sáng, khung so tỉ lệ. Link từ trang rig cũ. Mặc định chạy. Chưa tích hợp gameplay.
- `node scripts/check-wolf-frames.mjs` PASS: đủ 24 hình khác nhau, state/frame switching, pause/play, bước frame trước/sau, không tràn ngang 740×480, GIF HTTP 200 image/gif, không pageerror. Đã trực tiếp xem screenshot bản chạy, tấn công, GIF và sheet nguồn; dáng thấp/cổ ngắn gần ảnh gốc hơn mẫu rig. Vẫn có dao động nhỏ nét vẽ và tỉ lệ giữa ảnh sinh, chưa được người dùng duyệt, không khẳng định trùng tuyệt đối.
- Ảnh QA `test-results/wolf-frames-*.png`. Build đã mở rộng để chép trang mới; server bổ sung MIME .gif. Server cũ được dừng và mở lại bằng `npm.cmd run dev`, session 66264, localhost:5173. Các giới hạn xác minh gameplay cũ vẫn áp dụng.

## Bổ sung tiếp: sói vẽ lại cho rig ngày 11/11/2026

- Yêu cầu mới: làm thử một quái vật, lần này vẽ lại ảnh để tránh lộ các khối khi chuyển động.
- Đã dùng built-in imagegen, tham chiếu `public/assets/beast-0.png` (sói tím), tạo `public/assets/wolf-rig-atlas-v1.png` (1536×1024 RGBA, alpha thực). Giữ nguyên ảnh gốc. Prompt đầy đủ tại `public/assets/wolf-rig-prompt.md`.
- Atlas gồm 6 bộ phận vẽ kín: thân, đầu kèm bờm, đuôi, chân trước, chân sau, bờm ngực. Có phần khuất/gốc khớp vẽ thêm để chồng dưới thân. Không cắt lại pixel hay xóa nền bằng script; rig lấy UV trực tiếp từ atlas, alphaTest .45 bỏ viền glow bán trong suốt của ảnh sinh.
- `/monster-preview.html`, `src/wolf-rig.js`, `src/monster-preview.js`: mẫu riêng với 9 Bone, 4 động tác thở/chạy tại chỗ/lao tấn công/trúng đòn. Có pause, nền sáng/tối, tốc độ, hiện xương, tách lớp. Mẫu mèo có link sang mẫu này. Chưa đưa vào gameplay.
- `node scripts/check-monster.mjs` PASS trong Chrome: 9 khớp, 4 tư thế khác nhau, pause giữ thời gian, không tràn ngang ở 740×480, không pageerror. Đã trực tiếp xem ảnh nền tối, nền sáng 4 trạng thái, ảnh xương/tách lớp, màn nhỏ và contact sheet 8 thời điểm chạy/tấn công. Không thấy khe hở/mép cắt thẳng lộ ra ở cổ/vai/gốc chân trong các tư thế đã kiểm tra; không khẳng định đúng với mọi chuyển động mở rộng ngoài mẫu.
- Ảnh xác minh `test-results/wolf-*.png`; `test-results/wolf-motion-contact.png` là 8 mẫu chu kỳ. Build chép trang mẫu mới và atlas. Các giới hạn xác minh gameplay cũ vẫn giữ nguyên.

## Bổ sung: mẫu rig 2D ngày 11/11/2026

- Người dùng hỏi cắt ảnh, gắn xương và tạo vài animation đơn giản. Đã làm mẫu riêng cho `cat-0.png` tại `/animation-preview.html`; chưa gắn vào gameplay.
- `src/cat-rig.js`: 7 `THREE.Bone` điều khiển polygon lấy UV từ sprite gốc (đầu, ngực, hông, đuôi, 3 chân). Cắt bằng geometry, không tạo lại hay ghi đè bitmap. Đây là cutout rig cứng, chưa có trọng số skinning hay vẽ bù phần khuất; chuyển động lớn có thể lộ đường nối.
- `src/animation-preview.js`: 4 trạng thái thở, bước tại chỗ, giật khi bắn, trúng đòn; có tạm dừng, hiện xương và tách lớp. Dùng Three.js local.
- Đã render trong Chrome và trực tiếp xem screenshot idle/walk/shoot/hit/bones ở 1200×900, cùng màn nhỏ 740×480. `node scripts/check-animation.mjs` PASS: 4 tư thế khác nhau, pause giữ thời gian, không pageerror. Ảnh trong `test-results/rig-*.png`.
- Build chép cả trang mẫu. Bản mẫu không thay đổi renderer game, bố trí mèo trên tầng hay bộ binh. Các giới hạn xác minh gameplay và thứ tự công việc cũ bên dưới vẫn áp dụng.

Cập nhật: 11/11/2026, múi giờ Asia/Bangkok.

## Trạng thái thực tế

Đã viết code game và build tĩnh; kiểm thử mô phỏng đã qua. **Chưa xác nhận hoàn tất game vì chưa chạy kiểm tra hình ảnh gameplay WebGL thực tế.**

Ở cuối lượt làm game, môi trường chặn tải thư viện nên chưa thể kiểm tra WebGL. Khi lưu phiên, kiểm tra lại thấy `public/vendor/three.module.js` (603.113 byte) và `public/vendor/three.core.js` (1.403.455 byte) đã có sẵn, thời gian sửa 11:19:19 ngày 11/11/2026. Header core ghi REVISION 180. Không cần tải lại nếu hai file này hoạt động. Không suy diễn rằng gameplay đã chạy chỉ vì file đã có.

Người dùng yêu cầu lưu phiên để lần sau làm tiếp. Lượt lưu này chỉ kiểm tra trạng thái và ghi bàn giao, không thực hiện thêm kiểm thử trình duyệt gameplay.

## Yêu cầu đã chốt

- Game xe tăng bắn quái, màn hình ngang, dùng Three.js.
- Bám ảnh mới `Moonlit Cat-Dog Tank Battle.png`: đêm xanh, trăng lớn, xe tháp vàng bên trái, chiến trường bên phải, HUD trên và kỹ năng dưới.
- Mỗi tầng có đúng một mèo gắn cố định điều khiển; cụm súng xoay riêng để ngắm mục tiêu.
- Mèo bộ binh mang súng cá nhân, chạy theo xe, tự ngắm riêng.
- Dùng toàn bộ họ súng và toàn bộ 20 loài quái trong các ảnh, mỗi loài khác kích thước và hành vi chiến đấu. Hiệu ứng bắt mắt.
- Giữ nguyên ảnh nguồn. Không tự xuất bản website; đang làm project local trong folder người dùng.

## Đã triển khai

- 5 tầng / 5 mèo xạ thủ cố định.
- 10 mèo bộ binh, 2 tổ luân phiên mỗi 18 giây, 5 mèo hoạt động đồng thời.
- 10 họ tháp pháo (hai bộ ảnh tháp dùng làm hai biến thể ngoại hình), 10 súng cá nhân, 20 module phụ; cấu hình tự đổi mỗi 10 giây để đủ 40 vũ khí trong một màn.
- 20 loài quái, 6 đợt, trùm Bạo long Hắc Nguyệt 60.000 máu.
- Đạn có va chạm, dẫn đường, nổ lan, xuyên, laser, sét chuyền, hiệu ứng độc/cháy/chậm/choáng. Có phục kích gần xe để vũ khí ngắn tầm được dùng.
- Điều khiển di chuyển, khóa mục tiêu, 6 kỹ năng, năng lượng, tạm dừng, âm thanh tổng hợp, toàn màn hình, bách khoa, thắng/thua và chơi lại.
- 130 sprite cắt từ 12 bộ ảnh, kiểm tra qua contact sheet; ảnh nguồn nguyên vẹn. Riêng mèo có alpha gốc, không xóa màu trắng để giữ lông trắng.

## File chính

- `src/data.js`: vũ khí, quái, đợt, kỹ năng.
- `src/battle.js`: mô phỏng chiến đấu, độc lập renderer.
- `src/visuals.js`: Three.js, nền và khung xe procedural, sprite, hiệu ứng.
- `src/game.js`: tải ảnh, điều khiển, HUD, bách khoa, vòng lặp.
- `src/audio.js`, `src/boot.js`: âm thanh và nạp Three.js local/CDN.
- `index.html`, `style.css`: giao diện.
- `CHOI_GAME.cmd`: tải Three.js nếu thiếu, mở game local bằng Node.
- `scripts/server.mjs`: server localhost:5173 không cần npm install.
- `scripts/vendor.mjs`: tải hai file Three.js 0.180.0.
- `scripts/check.mjs`: kiểm thử có ý nghĩa, gồm chạy hết màn.
- `scripts/build.mjs`: chép source và public thành dist.
- `scripts/prepare_assets.py`: tạo sprite với Pillow.
- `README.md`: cách chạy, tính năng, trạng thái xác minh cũ.

## Kiểm tra đã thực hiện

- `npm.cmd run check`: PASS sau thay đổi source cuối cùng. Seed 42: thắng sau khoảng 386 giây, 99 quái bị hạ, đủ 20 loài, đủ 40 vũ khí, còn khoảng 15.185 máu. Bot khóa trùm và dùng heal/shield/barrage có điều kiện; không phải xác minh khả năng chơi thủ công.
- Kiểm tra cú pháp JS, sprite tồn tại, kích thước loài khác nhau, khóa mục tiêu, hồi máu, cooldown, khiên, chậm, beam/lightning.
- `npm.cmd run build`: PASS, xuất dist. **Build đó được tạo trước khi hai file vendor xuất hiện, nên cần build lại để dist có Three.js local.**
- Chrome headless đã kiểm tra HTML, các module và ảnh trả HTTP 200.
- Khi chưa có vendor: kiểm tra thông báo không tải được Three.js trong Chrome. `test-results/loading-state.png` và `initial.png` chỉ là trạng thái tải/lỗi, **không phải ảnh gameplay đã render thành công**.
- Server kiểm thử đã dừng bằng Ctrl+C cuối lượt làm game.

## Việc tiếp theo, theo thứ tự

1. Chạy `npm.cmd run dev`, mở localhost:5173 với hai file Three.js local hiện có.
2. Kiểm tra console/pageerror và bấm XUẤT KÍCH. Sửa lỗi runtime nếu có.
3. Chụp và xem gameplay ở 1600×900 và màn hình ngang nhỏ. So trực tiếp với ảnh concept.
4. Kiểm tra 5 mèo ở đúng tầng, chân/bàn chân/đồng phục không lệch, súng quay đúng hướng, vũ khí phụ không che khuất quá nhiều; đội bộ binh chạy theo và cầm súng đúng.
5. Chơi thử khóa quái bằng chuột, A/D, kỹ năng, pause/resume, bách khoa, âm thanh, chơi lại, giao diện cảm ứng. Kiểm tra khung hình và rò rỉ tài nguyên nếu có dấu hiệu.
6. Kiểm tra cân bằng cuối màn với người chơi; sửa sau khi có bằng chứng. Chạy lại `npm.cmd run check` sau thay đổi gameplay.
7. Chạy `npm.cmd run build` lại để đưa vendor vào dist; cập nhật README và bàn giao với mức xác minh đúng thực tế.

## Công cụ có sẵn trong máy

- Node v24.16.0, npm 11.13.0. Trong PowerShell dùng `npm.cmd`.
- Chrome: `C:/Program Files/Google/Chrome/Application/chrome.exe`.
- Playwright có sẵn tại `C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs` (có thể import bằng file URL).
- Python có Pillow: `C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.
- Không cần cài npm package. Lần thử npm install ban đầu bị chặn; `.npm-cache` là dữ liệu phát sinh, đã ignore.
- Hạn chế quyền đọc ngoài workspace và mạng vẫn có thể tồn tại; ưu tiên tài nguyên local đang có.

## Cách người dùng tiếp tục

“Đọc SESSION_NOTES.md rồi chạy thử, sửa và hoàn thiện game theo concept.”
