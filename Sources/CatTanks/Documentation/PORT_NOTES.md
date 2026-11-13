# Phạm vi bản port

Đây là bản C# native có thể chơi, dựa trên asset và balance trích từ HTML v3.7. Không phải thực thi nguyên JavaScript bằng browser nhúng, và chưa được chứng minh tương đương từng frame/từng kết quả ngẫu nhiên với bản web.

## Giữ lại từ nguồn

Catalog vũ khí, mèo, quái, 20 ải, mốc tiến trình, thưởng cơ bản, mức giá/nâng cấp chính và kỹ năng được xuất sang `Resources/Data/catalog.json`. 156 ảnh PNG được giải mã từ data bundle, không vẽ lại. Bản HTML gốc và project MegaTanks cũ không bị sửa.

Chiến đấu giữ bố cục 1600×900, camera trực giao, tự động bắn, chọn mục tiêu, di chuyển, cấu trúc wave/burst, đạn và trạng thái; UI được dựng bằng Unity IMGUI. Phong cảnh trăng, pháo đài nhiều tầng, hiệu ứng và âm thanh được tạo bằng Unity/C#.

## Khác biệt cần biết

- UI, khung xe, hạt/ánh sáng và âm thanh không giống từng pixel/từng mẫu âm thanh của HTML. Sói dùng bộ frame gốc; các nhân vật khác dùng sprite, chuyển động và biến dạng đơn giản thay vì toàn bộ rig web.
- Đấu trường dùng 6 đối thủ mock gốc, hệ số HP/ATK theo rating, AI dùng bộ kỹ năng đang trang bị và công thức Elo gốc. Đây là phần mô phỏng, không có online multiplayer hay dịch vụ gửi/nhận đội phòng thủ.
- Quái đồng hành có 4 IV và trait, chỉ số theo level/sao/tiến hóa; passive HP/energy áp dụng theo catalog, cap 8% HP / 0,8 hồi năng lượng như source. Các trường passive khác chưa được source loadout sử dụng nên không thêm hiệu ứng mới.
- UI Cộng hưởng và cách chọn trang bị dùng thao tác chuyển tuần tự qua những lựa chọn sở hữu. Không sao chép nguyên dialog/chuyển trang của web.
- Tiến trình dùng JSON riêng. Không có tính năng nhập save localStorage của HTML, đồng bộ nhiều cửa sổ hoặc chuyển tiếp trận đang chơi sau khi thoát app. Lồng đã ném được lưu ngay; kết quả trận được lưu khi kết thúc/rút lui. Quái bắt tạm chỉ được nhận khi thắng.
- Đích đã kiểm thử là desktop macOS với bàn phím/chuột. Chưa port cụm điều khiển cảm ứng và chưa kiểm thử build mobile.

## Bằng chứng kiểm tra

- Unity **2022.3.62f1** biên dịch runtime và embedded MCP package.
- **124 kiểm tra đạt**: catalog/asset, wallet atomic spending, cap AFK và clock rollback, serialize/save/backup, loadout, năng lượng/hồi chiêu, HP quái theo ải, swept projectile collision, đổi bank classic, điều kiện/chi phí bắt quái, thưởng trận không nhận trùng, 20 mô phỏng hữu hạn.
- Mô phỏng dùng đội hình mạnh cố định để phát hiện lỗi/stall; có cả thắng và thua ở ải cao. Chưa kiểm thử cân bằng hoặc tất cả tổ hợp đội hình.
- Play Mode đã kiểm tra hình ảnh Hành quân, Doanh trại, bách khoa và chiến đấu; Console không có lỗi runtime trong các luồng đã kiểm tra.
- Build **StandaloneOSX / Mono / Strict Mode** thành công, 0 errors và 0 warnings. Chưa có kiểm thử tự động click qua mọi nút.
- MCP HTTP kết nối được và đã dùng để điều khiển đúng Editor CatTanks.

Báo cáo máy đọc: `validation-report.json`. Ảnh: thư mục `Captures` ở gốc project.
