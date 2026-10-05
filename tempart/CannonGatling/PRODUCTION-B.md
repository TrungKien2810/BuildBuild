# Gatling — redesign v3 đã chốt (form B)
Ngày 05-10-2026. Người giao đã chốt redesign-v3. Bàn giao production cùng 5 cannon trong tempart. Form B được giữ; mặt sáu nòng được làm sạch và dùng chung các pha quay sinh mới.

## Bộ asset
| Nhóm | Frame | Canvas |
|---|---:|---|
| identity | 1 | 144×128 |
| idle | 4 | 144×128 |
| attack | 6 | 144×128 |
| bullet | 4 | 128×128 |
| bullet-impact | 4 | 128×128 |
| icon | 1 | 48×48 |

19 frame ngoài identity; PNG rời và sheet đều có trong thư mục chính. Sheet 4 frame = 2 cột × 2 hàng; attack = 3 cột × 2 hàng. Đáy bệ y=96.
Idle có phản quang đồng nhẹ. Attack đổi pha đầu nòng trong cùng silhouette; vòng đồng, thân, trống đạn, trụ và bệ cố định. Chớp từ tâm cụm nòng tại frame 2/3/5; không quá nhiệt. Đạn đồng, impact đơn mục tiêu.

## Neo và xuất
Tọa độ từ góc trên trái: muzzle (31,60); đạn và impact (64,64). Toàn bộ frame giữ nguyên canvas, không trim theo từng frame.
Bộ bàn giao hiện gồm PNG/GIF production và QC. Thư mục source redesign-v3 hiện không có trong workspace; không kèm exporter để tái sinh asset. Xem [thông tin nguồn](prompts.md).

## Xem và kiểm tra
- [Toàn bộ frame](qc/all-assets.png).
- [GIF preview](qc/preview.gif); [attack quay chậm](qc/attack-slow.gif).
- [So sánh cùng 4 pháo cũ](qc/compare.png).
- [Báo cáo xuất](qc/validation.md).

Đã đối chiếu PNG rời với từng ô sheet, pixel của phần cơ khí cố định, silhouette đạn, frame đầu/cuối và baseline. GIF dùng palette cố định; mọi frame giải mã khớp pixel với ảnh nguồn, không còn dithering hạt.
Các bản trước v3, raw cũ, ZIP backup và ảnh QC lặp đã được xóa theo yêu cầu. Các PNG/GIF hiện hành được giữ nguyên; nguồn v3 không có trong bộ bàn giao hiện tại.
Bàn giao art trong tempart; kiểm tra trong Unity chưa thực hiện.
