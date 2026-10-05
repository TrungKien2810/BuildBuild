# Longgun — Hoả Mai Thành — redesign v3 đã chốt

Người giao đã chốt redesign-v3 ngày 05-10-2026. Các thư mục asset chính chứa bộ v3; bản thiết kế cũ đã được xóa theo yêu cầu. Bộ production được bàn giao cùng 5 cannon trong tempart trên nhánh asset/phao-thaituan05.

## Hình dáng và chuyển động

Form B: một nòng thép dài nằm ngang hướng trái, thân gỗ gọn, scope đồng, dải vải đỏ và bệ đá bát giác.

Chỉ nòng trượt vào vỏ khi giật +1/+3/+1 px; scope, thân gỗ, dải vải, giá đỡ và bệ cố định. Đạn thép và impact đơn mục tiêu; tâm đỏ nhấp nháy màu, khói tách lớp lan trái rồi tan.

## Bộ asset

- `identity/`: 1 frame 144×128.
- `idle/`: 4 frame 144×128.
- `attack/`: 6 frame 144×128.
- `bullet/`: 4 frame 128×128, mũi hướng trái.
- `bullet-impact/`: 4 frame 128×128.
- `target-mark/`: 2 frame 128×128.
- `muzzle-smoke/`: 4 frame 128×128.
- `icon/`: 1 icon 48×48.

25 frame ngoài identity; mỗi nhóm có sheet-transparent.png. Attack dùng 3 cột × 2 hàng; nhóm 4 frame dùng 2 cột × 2 hàng. Bệ chạm y=96 và không trôi giữa các frame. Idle-1/4 và attack-1/6 khớp identity.

Muzzle: (11,55), cộng độ dịch x khi giật, tọa độ từ góc trên trái. VFX dùng neo (64,64); giữ toàn bộ canvas khi import. Không tạo animation cooldown.
Điểm phát khói hiện dùng (64,64), không dùng neo (86,64) của bản cũ.

## Bàn giao

- [Bàn giao kỹ thuật](PRODUCTION-B.md).
- [Toàn bộ frame](qc/all-assets.png), [GIF preview](qc/preview.gif), [attack quay chậm](qc/attack-slow.gif).
- [So sánh với 4 pháo cũ](qc/compare.png), [QC](qc/validation.md).
- [Thông tin nguồn](prompts.md): bộ bàn giao hiện không kèm source/exporter v3.
- [Cơ chế đề xuất](mechanics.md); chốt hình ảnh không đồng nghĩa chốt thông số gameplay.

QC file đã hoàn thành; chưa import hoặc thử trong Unity.