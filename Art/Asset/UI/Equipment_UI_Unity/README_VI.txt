EQUIPMENT UI — CỤM PANEL DƯỚI / CANVAS 1920 × 1080

Sprites/ chứa từng PNG riêng, có kênh alpha.
Reference/ chứa ảnh ghép để đối chiếu bố cục; không cần dùng ảnh ghép làm UI chính.

1. Khung và nút
- Panels/panel_equipment_bottom.png: panel tổng.
- Panels/panel_inventory_inset.png: nền kho đồ.
- Buttons/button_filter.png: nền bộ lọc.
- Buttons/button_sort_direction.png: nền ASC / DESC.
- Buttons/button_order.png: nền sắp xếp theo tên.
- Buttons/button_equip_yellow.png: nút vàng lắp / tháo.
- Slots/item_slot_normal.png và item_slot_selected.png: hai trạng thái ô đồ.
- Badges/: nền dấu đang lắp, dấu tick, và dấu tick ghép sẵn.
- Decor/info_divider.png: vạch chia kho đồ / thông tin.

2. Icon súng
- Icons/icon_cannon.png
- Icons/icon_mg.png
- Icons/icon_mortar.png
- Icons/icon_mortar_with_mount.png: thêm lựa chọn súng cối có đai / bệ giữ.
Ba icon chính lấy nguyên từ các part gốc có alpha, không vẽ lại.

3. Ghép trong Unity
- Chép thư mục Sprites vào Assets và đặt Texture Type = Sprite (2D and UI).
- Các khung không dính chữ; dùng TextMeshPro cho nhãn / nội dung.
- Icon súng để Preserve Aspect, rồi đặt trong một Image con của ô đồ.
- sprite_manifest.json ghi kích thước PNG, phần đệm trong suốt và đề xuất viền
  theo thứ tự left, bottom, right, top để điền trong Sprite Editor nếu dùng Sliced.
- Nút vàng giữ Image Type = Simple để khớp hình nút nghiêng của bản mẫu.
- Bật Alpha Is Transparency khi cần; các PNG đã có sẵn alpha.
- layout_1920x1080.json ghi tọa độ top-left và kích thước phần hình nhìn thấy.
  Tọa độ nằm trên canvas tham chiếu 1920 × 1080, không bao gồm đệm trong suốt.
  Trừ phần đệm khỏi vị trí và cộng phần đệm vào kích thước Image nếu muốn khớp
  đúng từng pixel. Phần đệm của từng PNG nằm trong sprite_manifest.json.

4. Ảnh mẫu
- equipment_bottom_full_preview_1920.png: cụm có chữ và icon để xem cách ghép.
- equipment_bottom_blank_1920.png: cụm khung trống.
Hai ảnh mẫu là phần đáy scene, kích thước 1920 × 300, lấy từ y = 780 của canvas.

Gói này là asset hình ảnh để tự xếp UI; không kèm logic inventory / prefab.
