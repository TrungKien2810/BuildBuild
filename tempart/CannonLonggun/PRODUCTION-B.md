# Longgun — redesign v3 đã chốt (form B)
Ngày 05-10-2026. Người giao đã chốt redesign-v3. Bàn giao production cùng 5 cannon trong tempart. Giữ form B nòng dài, thân gỗ, ống ngắm đồng và dải vải đỏ.

## Bộ asset
| Nhóm | Frame | Canvas |
|---|---:|---|
| identity | 1 | 144×128 |
| idle | 4 | 144×128 |
| attack | 6 | 144×128 |
| bullet | 4 | 128×128 |
| bullet-impact | 4 | 128×128 |
| target-mark | 2 | 128×128 |
| muzzle-smoke | 4 | 128×128 |
| icon | 1 | 48×48 |

25 frame ngoài identity; PNG rời và sheet trong thư mục chính. Sheet 4 frame = 2 cột × 2 hàng; attack = 3 cột × 2 hàng; target-mark = 2 cột × 1 hàng. Đáy bệ y=96.
Idle có phản quang ống ngắm nhẹ; vải giữ nguyên hình để không nhảy silhouette. Attack ngắm → bắn → chỉ nòng trượt vào vỏ theo +1/+3/+1 px → hồi về identity. Giá đỡ, thân gỗ, scope và bệ không bị cắt/dịch theo hàng ảnh.
Target-mark chỉ nhấp nháy màu đỏ, giữ cùng hình và tâm. Đạn thép, impact đơn mục tiêu, không xuyên hoặc AOE. Khói tách lớp, lan sang trái rồi tan.

## Neo và xuất
Tọa độ từ góc trên trái: muzzle (11,55) ở pose nghỉ; khi nòng giật thì cộng độ dịch x. Đạn, impact, target-mark và điểm phát khói cùng (64,64) trong canvas VFX. Điểm neo khói đã chuẩn hóa từ bản cũ (86,64) sang (64,64); khi tích hợp phải dùng neo mới. Toàn bộ frame giữ nguyên canvas, không trim từng frame.
Bộ bàn giao hiện gồm PNG/GIF production và QC. Thư mục source redesign-v3 hiện không có trong workspace; không kèm exporter để tái sinh asset. Xem [thông tin nguồn](prompts.md).

## Xem và kiểm tra
- [Toàn bộ frame](qc/all-assets.png).
- [GIF preview](qc/preview.gif); [attack quay chậm](qc/attack-slow.gif).
- [So sánh cùng 4 pháo cũ](qc/compare.png).
- [Báo cáo xuất](qc/validation.md).

Đã đối chiếu PNG rời với từng ô sheet, phần cơ khí cố định, silhouette đạn, frame đầu/cuối và baseline. GIF dùng palette cố định; mọi frame giải mã khớp pixel với ảnh nguồn, không còn dithering hạt.
Các bản trước v3, raw cũ, ZIP backup và ảnh QC lặp đã được xóa theo yêu cầu. Các PNG/GIF hiện hành được giữ nguyên; nguồn v3 không có trong bộ bàn giao hiện tại.
Bàn giao art trong tempart; kiểm tra trong Unity chưa thực hiện.
