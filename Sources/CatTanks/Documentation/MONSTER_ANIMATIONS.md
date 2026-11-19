# Tự nạp animation quái

Nguồn art: `D:\SeagullGame\MegaTanks\MegaTank\Art\CatTanks_Visual_Reference\01-Quai-va-animation`. Importer tính đường dẫn tương đối từ project tới thư mục Art này; không ghi vào thư mục nguồn.

Khi mở project trong Editor, `MonsterAnimationImporter` theo dõi thay đổi file/thư mục và tự đồng bộ sau khi nguồn yên trong 2 giây. Nếu đang Play, cập nhật được xếp chờ đến khi dừng Play. Nếu nguồn không có trên máy, game vẫn dùng thư viện đã import trong Assets. Có thể chạy thủ công **CatTanks > Combat > Import Latest Monster Animations**.

Mỗi loài được nhận theo hậu tố folder `bug-N` hoặc `beast-N`. Importer chọn sheet đầy đủ `*sheet-idle-run-attack-hit*.png` có số phiên bản V cao nhất; thời gian sửa file chỉ phân xử giữa các sheet cùng phiên bản. Bản nháp không có V không thắng bản đã đánh phiên bản. Thêm folder V5 hoặc thay PNG của bản hiện có sẽ được nhận tự động; không chọn riêng ảnh thử/GIF làm animation game.

Sheet phải có 6 cột × 4 hàng, ô vuông cùng kích thước. Bốn hàng từ trên xuống là idle, run, attack, hit. Dải `*4x1.png` trong cùng folder, tên chứa `vao` hoặc `idle-sang-run`, cung cấp hai hình chuyển tiếp ở cột 2 và 3. Importer giữ nguyên ô và một pivot chân chung để không căn lại riêng từng hình gây rung/trượt. Tất cả hình trong clip giữ cùng tỉ lệ từ tư thế idle tham chiếu.

- PNG và metadata Unity: `Assets/CatTanks/Art/MonsterAnimations/<species>/`.
- Thư viện runtime: `Assets/CatTanks/Resources/MonsterAnimations.asset`.
- Báo cáo nguồn/phiên bản: `Logs/monster-animation-import.json`.
- Kiểm tra clip, pivot, trạng thái, pause: `Logs/monster-animation-validation.json`.
- Ảnh 20 loài đi qua renderer combat: `Captures/MonsterAnimations/combat-gallery.png`.

Runtime lấy hình theo tuổi mô phỏng của actor, nhận sự kiện tấn công từ lần reset cooldown và trúng đòn từ flash. Đoạn vào chạy phát một lần; chạy/idle lặp, hit/attack phát một lần. Dừng combat cũng dừng animation. Các file trong Art không cần tồn tại khi chạy bản build. Không thay chỉ số, hitbox, tốc độ di chuyển hoặc profile.

Kết quả 19/11/2026: Kiến V4, 19 loài V3; 480 hình trong bốn trạng thái và 40 hình chuyển tiếp đã import. 223 kiểm tra animation đạt; đủ 20 loài render qua BattleView. 124 kiểm tra gameplay vẫn đạt.
