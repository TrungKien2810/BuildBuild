# Weakening Ghost

Bộ pixel art đã bổ sung và đóng gói theo cấu trúc AcidLizard. Tham chiếu tạo hình: ma tím với bùa kiếm gãy; hướng 3/4 sang trái. Asset mới sinh bằng công cụ image_gen tích hợp; các sheet nguồn, prompt và GIF preview giữ tại `Temp/tempart/Weakening Ghost/`.

| Thư mục | File frame | Số frame |
|---|---|---:|
| identity | identity-1.png | 1 |
| idle | idle-1..4.png | 4 |
| walk | walk-1..4.png | 4 |
| attack | attack-1..8.png | 8 |
| hurt | hurt-1..4.png | 4 |
| death | death-1..6.png | 6 |
| skill | skill-1..8.png | 8 |
| curse | projectile-1..4.png | 4 |
| curse-impact | impact-1..4.png | 4 |
| skill-projectile | projectile-1..4.png | 4 |
| armor-down-status | status-1..4.png | 4 |

Tổng 51 frame PNG. Mỗi thư mục có `sheet-transparent.png`, `pipeline-meta.json` và `.meta` Unity. `curse` / `curse-impact` tương ứng vai trò của `acid` / `acid-impact` trong AcidLizard; tên khác để phù hợp phép nguyền của ma. `skill`, `skill-projectile` và `armor-down-status` là phần bổ sung cho skill mana và giảm giáp.

Frame xuất 128×128, alpha nhị phân, lọc Point, 100 PPU; không mipmap. Thân dùng pivot (0.5, 0.21875); hiệu ứng dùng pivot tâm (0.5, 0.5). Thân được căn theo bùa ngực tại (64, 82), tỷ lệ giữ cố định trong mỗi clip. Walk là lướt, không có chân; death giữ nguyên tỷ lệ khi tan thành hạt.

Frame phóng đạn gợi ý theo chỉ số bắt đầu từ 0: attack 4, skill 5. GIF minh họa giảm giáp 15% / 35% là số thử ý tưởng.

Bộ đã kiểm tra kích thước, alpha, GUID, frame trống, chạm mép và xem trực tiếp contact sheet. Chưa tạo ScriptableObject, prefab hoặc kết nối gameplay; chưa xác minh import trong Unity Editor.
