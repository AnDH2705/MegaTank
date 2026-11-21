---
name: cattanks-ui-layout
description: Thiết kế hoặc duyệt bố cục và art giao diện của từng màn, popup hoặc HUD CatTanks trong Unity. Hoàn tất bố cục trước khi chỉnh art giao diện được yêu cầu, rồi dừng sau một màn để người dùng duyệt.
---

# Bố cục UI/UX CatTanks

## Phạm vi

Hoàn tất vị trí, kích thước khung, căn lề, khoảng cách, nhóm và thứ tự hiển thị trong màn đã chọn. Sau đó, khi người dùng yêu cầu đổi art giao diện, có thể tạo mới hoặc tách từ ảnh mẫu đã cung cấp các khung, hình nút, tab và icon UI của màn đó. Chỉnh màu và kiểu hiển thị chữ theo mẫu. Giữ nguyên asset và tham chiếu ảnh ải, nhân vật, vũ khí, quái và nền. Giữ nguyên nội dung, vùng bấm, hành vi, dữ liệu lưu, cơ chế điều hướng và bố cục màn khác. Nếu dùng chung một thành phần, áp dụng kiểu mới riêng cho màn đang duyệt.

## Nguyên tắc quyết định bố cục

- Xác định quyết định hoặc thao tác chính của người chơi. Tạo thứ bậc bằng vị trí, kích thước khung và khoảng trắng. Căn theo lưới, gom thông tin đi cùng nhau như tên, trạng thái và phần thưởng, rồi dùng khoảng cách lớn hơn giữa các nhóm. [Nielsen Norman Group, Visual Design Glossary](https://www.nngroup.com/articles/visual-design-cheat-sheet/)
- Ưu tiên khung đủ rộng và đủ cao cho chữ Việt có dấu, tên dài và số thực tế. Đo ở kích thước hiển thị nhỏ đang hỗ trợ và thử dữ liệu dài nhất. Ưu tiên nới khung hoặc bố trí lại để giữ cỡ chữ. [Xbox Accessibility Guideline 101](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101)
- Cho vùng bấm bám theo hình nút và chừa khe giữa các nút cạnh nhau. Kiểm tra kích thước sau khi Canvas co theo độ phân giải thực, theo thiết bị và phương thức nhập của game. [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/)
- Giữ lối đọc phù hợp với thứ tự Tab hiện có, kiểm tra focus nhìn thấy được và trạng thái khóa, chọn, sẵn sàng khi đổi vị trí. Chừa chỗ cho thông báo và lớp nổi, đặc biệt quanh thao tác chính. [Xbox Accessibility Guideline 112](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112) · [Xbox Accessibility Guideline 113](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113)

## Áp dụng trong Unity

1. Chọn đúng một màn hoặc popup. Mở `Assets/CatTanks/Scenes/Main.unity` và chụp hiện trạng nếu có Editor. Có thể dùng `CatTanks → UI → Layout Workshop` để thử hình học trên Canvas tham chiếu 1600 × 900. [Hướng dẫn Layout Workshop](../../../Documentation/MOCKUP_GAME_UI.md#chỉnh-bản-nháp-bằng-layout-workshop)
2. Xác định nguồn chi phối hình học lúc chạy. CatTanks ưu tiên RectTransform đã author trong prefab. Nếu chỉnh lời gọi dựng UI có ID sinh từ tọa độ, đồng bộ ID với prefab hiện hành, prefab dự phòng và kiểm tra tham chiếu ID đó. Giữ thứ tự xử lý nút, sprite và quyền tương tác.
3. Soát phần tử ngoài vùng an toàn, va chạm giữa các khung, chuỗi dài và vùng bấm. Trong Play Mode, thử chuột, Tab và Enter theo hành vi hiện có, các trạng thái điển hình và độ phân giải nhỏ. Ghi rõ phần nào đã kiểm tĩnh và phần nào cần xác nhận trực tiếp nếu chưa chạy được Unity.
4. Lưu ảnh xem trước của mỗi màn vào `C:\Users\trieu\Desktop\xem trc`. Khi người dùng giao làm tự động, tiếp tục tuần tự với bố cục trước, art sau, và kiểm tra từng màn. Khi chế độ duyệt từng màn được yêu cầu, dừng sau mỗi ảnh để người dùng duyệt.
5. Dùng thư viện `C:\Users\trieu\Desktop\hocARTmoi` để học phân cấp, khoảng cách, nét viền và cách dùng màu cho trạng thái. Ưu tiên tạo art phù hợp nhận diện CatTanks, với mốc thẩm mỹ Hành quân V2 và Doanh trại → Mèo đã duyệt. Giữ các màn đã duyệt khi đổi nhóm khác.
