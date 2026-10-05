# Pháo đầu rồng SSR — Long Viêm
Slug: dragonhead · mục 4.16 · form B dùng để xem thử, chưa duyệt dáng cuối.
Trạng thái: thử cơ chế và asset; chưa sửa code hoặc import vào game.
Cơ chế dưới đây thay cho bản cầu lửa / phát thứ năm trước đó.

## Đánh giá
Hợp với SSR chuyên dọn đàn: phun diện rộng → tích Long Nộ → Long Tức → quái chết nổ lan.
Điểm hấp dẫn là sức mạnh tăng rõ trong một đợt phun và vụ nổ nối tiếp khi quái tụ đông.
Cần giới hạn tốc độ tích thanh và độ sâu nổ lan. Nếu mỗi quái trúng đều cộng thanh độc lập,
đàn đông sẽ tạo Long Tức gần như liên tục, trong khi sát thương diện rộng đã có lợi sẵn.
Bản đầu chưa thêm giảm giáp boss / 5 tầng Thiêu Đốt; thử bộ chính trước để dễ xác định sức mạnh.

## Nhịp đánh đề xuất để thử, chưa phải số cân bằng đã chốt
- Có mục tiêu phía trước: phun hình nón liên tục 2,5 giây, rồi nghỉ 0,5 giây.
- Phun đánh mọi quái trong nón, không phải chọn một quái rồi áp sát thương lên cả màn hình.
- Sát thương tính theo thời gian (DPS), độc lập với số frame hoặc tốc độ animation.
- Mất hết mục tiêu thì ngừng phun, trở về idle. Nghỉ lấy hơi là thời gian gameplay; không có asset cooldown.
- Dùng D làm sát thương trực tiếp mỗi giây ở mức đầu; chưa chốt giá trị D, tầm hoặc giá xây.
- Ba mức Long Nộ: 0–32 / 33–65 / 66–99. Hệ số DPS thử: 1D / 1,4D / 1,8D.
- Tầm tương đối thử: 1 / 1,15 / 1,3 lần tầm gốc. Nón dài hơn, không biến thành vòng AOE quanh trụ.

## Long Nộ
- Thanh 0–100. Khi luồng phun có trúng ít nhất một quái, cộng 40 điểm/giây.
- Nhiều quái trúng cùng lúc vẫn chỉ cộng 40 điểm/giây; không nhân với số quái hoặc số tick.
- Thiêu Đốt, Hỏa Bạo, Long Tức và đất cháy không cộng thanh.
- Giữ thanh qua khoảng nghỉ ngắn và đổi mục tiêu. Không trúng quái trong 2 giây thì bắt đầu giảm 20 điểm/giây.
- Thanh đầy và có mục tiêu hợp lệ: ngắt đợt phun thường, lấy hơi 0,3 giây rồi phóng Long Tức.
- Mắt, khe má và ánh trong miệng tăng sáng; một ít khói từ mũi ở mức cao. Không nhuộm toàn thân thành màu nóng.

## Thiêu Đốt
- Quái trúng phun / Long Tức / đất cháy nhận Thiêu Đốt 3 giây, gây 0,2D mỗi giây.
- Một hiệu ứng Thiêu Đốt của Long Viêm trên mỗi quái; trúng lại làm mới thời gian, không cộng tầng.
- Quái ra khỏi vùng phun vẫn cháy cho đến khi hết thời gian.
- Cần ghi nguồn trụ và thông tin chuỗi lan trên debuff để biết ai sở hữu Hỏa Bạo.

## Long Tức
- Động tác lấy hơi: đầu nâng nhẹ quanh khớp, mắt lóe, hàm cơ khí mở; bệ đứng yên.
- Phun luồng lớn xuyên các quái phía trước trong khoảng 0,6 giây.
- Mỗi quái nhận 3D sát thương trực tiếp MỘT LẦN cho mỗi lần Long Tức, không nhận 3D ở mọi frame.
- Để lại vùng đất cháy 3 giây dưới dải phun: 0,25D/giây và làm mới Thiêu Đốt.
- Nhiều vùng đất cháy của cùng cơ chế chồng lên nhau lấy mức mạnh nhất, không cộng vô hạn.
- Sau khi phát: thanh Long Nộ về 0, nghỉ nhịp ngắn rồi quay lại đánh thường.
- Đất cháy phải đặt theo mặt đất trong game; không gắn vào miệng nòng.

## Hỏa Bạo
- Quái chết trong khi có Thiêu Đốt do Long Viêm: phát một vụ nổ lửa và truyền Thiêu Đốt cho quái gần đó.
- Đề xuất thử bán kính 1,2 đơn vị; sát thương vụ đầu 1D.
- Một chuỗi tối đa BA lượt nổ tính cả vụ đầu: 1D → 0,5D → 0,25D; sau lượt ba dừng truyền chuỗi.
- Thông tin chuỗi / lượt phải đi theo Thiêu Đốt được truyền, không khởi tạo lại từ 0 ở mỗi quái.
- Mỗi quái chỉ phát Hỏa Bạo một lần khi chết; đánh dấu đã xử lý trước khi gây sát thương nổ.
- Quái nhận Thiêu Đốt do lan vẫn có thể chết và nối chuỗi trong giới hạn trên.
- VFX nổ là sát thương vùng thật của cơ chế này, khác spark impact của các pháo thường.
- Cần thử thực tế với đàn đông, nhiều Long Viêm và boss; các tỷ lệ này là điểm bắt đầu.

## Màu và hình dáng bản thử
- Dùng đầu rồng đá form B, vành miệng / đai sau bằng đồng, khớp sắt, hai sừng ngắn.
- Nòng hướng trái, bệ bát giác đá xám ấm, viền navy.
- Ba mức lửa: cam → đỏ → trắng-vàng viền đỏ.
- Trắng-vàng là lựa chọn tạm khi chưa có phản hồi màu; task đang dùng cyan/xanh băng cho làm chậm.
  Nếu chọn xanh-trắng theo ý tưởng ban đầu thì đổi palette VFX, không đổi cơ chế.
- Lửa là layer riêng, gắn vào tâm miệng; không có đạn viên hoặc bullet-impact trong bộ cơ chế mới.

## Asset thử hiện hành
| Nhóm | Frame | Canvas / nội dung |
| --- | ---: | --- |
| identity/identity-1.png | 1 | Form B, 144x128 |
| attack/ | 6 | Thân phun tích sáng dần, 144x128, bệ cố định |
| long-tuc-body/ | 6 | Lấy hơi → mở hàm/phát mạnh → về idle, 144x128 |
| breath-1/ | 6 | Lửa thấp, vật thể 96x24 trong 128x128 |
| breath-2/ | 6 | Lửa trung, vật thể 112x36 trong 128x128 |
| breath-3/ | 6 | Lửa cao, vật thể 128x44 trong 128x128 |
| long-tuc/ | 6 | Luồng lớn 128x56 trong 128x128; có thể scale theo vùng phun |
| burn-status/ | 4 | Cụm lửa tối đa 18x24 trong 128x128, bám lên quái |
| scorched-ground/ | 4 | Đất cháy 112x28 trong 128x128 |
| fire-burst/ | 4 | Hỏa Bạo, phát triển rồi tàn, tối đa 48x48 trong 128x128 |

- Các nhóm có sheet-transparent.png; sheet 6 frame: 3 cột × 2 hàng, 4 frame: 2 × 2.
- Tất cả PNG frame nền trong suốt, alpha nhị phân, nearest-neighbor; thân đáy bệ y=96.
- Thân dùng lại chính xác bệ identity từ y=78 trở xuống. Hiệu ứng phun có điểm phát (127,64), hướng trái.
- Nối mức lửa khi vượt ngưỡng, không chạy toàn bộ attack rồi mới được gây sát thương.
- Không có cooldown, attack-warm hoặc attack-hot. Hiệu ứng ba mức là biểu hiện Long Nộ của cơ chế mới.
- qc/long-viem-preview.gif: minh họa ba mức phun → lấy hơi → Long Tức → đất cháy / nổ lan.
- qc/long-viem-sequence.png: bảng sáu cảnh; qc/long-viem-firing.png: ảnh Long Tức.
- Goblin có sẵn trong game chỉ được ghép vào preview, không sửa hoặc đóng gói như asset mới.
- GIF là dàn cảnh giải thích cơ chế, không mô phỏng máu, hitbox hay chứng minh thông số đã cân bằng.
- Đây là bộ xem thử; chưa làm idle 4 frame / icon mới / Unity import / gameplay.
- identity-variant-a.png, attack-strong/, breath/ và qc/long-no-* là bản thử cũ, không nằm trong gói Long Viêm hiện hành.
