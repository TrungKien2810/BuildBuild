# Gatling Tự Động — cơ chế đề xuất

Các thông số dưới đây giữ nguyên từ brief trước. Người giao đã chốt hình ảnh v3; các thông số gameplay vẫn là đề xuất, chưa tích hợp.

## Cơ chế đề xuất
Súng máy đơn mục tiêu, tầm trung. Bắn càng lâu càng nhanh; sát thương mỗi viên giữ nguyên D.
Các số sau là mốc thử cân bằng:
- Đà quay s từ 0 đến 1. Khi đang bắn hợp lệ: tăng 1/3 mỗi giây; sau 3 giây đạt tối đa.
- Tốc độ bắn r = 2 + 4*s viên/giây; DPS lý thuyết = D*r.
- Đổi mục tiêu hợp lệ ngay giữ đà quay.
- Ngừng bắn 0,35 giây thì bắt đầu giảm đà với tốc độ 0,5 mỗi giây; từ tối đa về 0 mất thêm 2 giây.
- Không có mục tiêu, ngoài tầm hoặc bị khống chế thì ngừng bắn và giảm đà theo cùng quy tắc.
- Viên đầu có thể phát ngay. Đạn chỉ phát khi có mục tiêu hợp lệ; animation không tự quyết định số đạn.
- Không AOE, xuyên, gây cháy hay quá nhiệt. Không đổi màu nòng khi tăng tốc.
- Điểm mạnh: quái đông/liên tiếp hoặc mục tiêu nhiều máu. Điểm yếu: khoảng nghỉ làm mất đà.
- Khác Liên Thanh ở thời gian tăng tốc. D, tầm và giá cần cân bằng trước khi đưa vào game.