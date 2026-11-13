# MCP trong CatTanks

Package Editor `com.coplaydev.unity-mcp` **10.1.0**, khai báo hỗ trợ Unity từ 2021.3, đã được chép thành embedded package trong project. `Packages/packages-lock.json` xác nhận nguồn `embedded`. Các dependency được Unity Package Manager resolve và đã biên dịch thành công trên **2022.3.62f1**.

Package lấy từ bản đã có trên máy tại `AdMaker/AdMaker/Packages/com.coplaydev.unity-mcp`, nên project không phụ thuộc đường dẫn tuyệt đối đó khi chuyển máy.

## Kết nối hiện tại

- HTTP server base URL: `http://127.0.0.1:8080`
- MCP client URL: `http://127.0.0.1:8080/mcp`
- Client Codex trên máy này đã có server `unityMCP`; không cần tạo thêm server trùng cổng.
- `Assets/CatTanks/Editor/ProjectSetup.cs` gọi kết nối khi mở Editor và sau domain reload. Nếu server local chưa chạy, khởi động server qua package và chờ tối đa 60 giây trước khi nối bridge. Không gọi khi Unity ở batch mode; không khởi động lại server đang có kết nối.
- Menu **CatTanks → Connect MCP** để thử kết nối lại.
- **Window → MCP for Unity** để quản lý server/transport hoặc chẩn đoán kết nối.

Đã xác minh project xuất hiện trong `mcpforunity://instances` với Unity **2022.3.62f1**, chọn được instance, đọc Console, chạy C#, bật/tắt Play Mode, chụp screenshot và build macOS qua MCP.

### Kiểm tra trên Windows ngày 2026-11-13

- Đã cài server `mcpforunityserver==10.1.0` bằng `uvx`, đúng phiên bản embedded package. Cấu hình Codex `unityMCP` hiện có trỏ đúng endpoint nên không cần thay đổi.
- Khởi động server nền tại `127.0.0.1:8080`; `/health` trả về `healthy`, phiên bản `10.1.0`.
- Unity **2022.3.62f2** kết nối thành công với instance `CatTanks@391bc8dd3400071e`; Console báo `CatTanks MCP connected: True`.
- Nếu cần cài lại server trước khi mở Unity: `uvx --from mcpforunityserver==10.1.0 mcp-for-unity --help`.

## Khi chuyển sang máy khác

1. Mở project bằng Unity đúng phiên bản, đợi Package Manager và compilation hoàn tất.
2. Mở **Window → MCP for Unity**, dùng hướng dẫn của package để cài Python/uv nếu máy chưa có và khởi động server local.
3. Đặt transport HTTP local, base URL `http://127.0.0.1:8080`.
4. Trong MCP client, thêm HTTP endpoint `http://127.0.0.1:8080/mcp`.
5. Chọn **CatTanks → Connect MCP**. Nếu có nhiều Unity project đang kết nối, chọn instance `CatTanks` ở client.

Tham khảo nhà phát triển: [MCP for Unity — Install](https://coplaydev.github.io/unity-mcp/getting-started/install). Runtime build không cần MCP/Python/server để chơi.
