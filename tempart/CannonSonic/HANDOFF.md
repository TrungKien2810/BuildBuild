# Pháo Âm Thanh SR — đầy đủ asset form B

Form B đã được người giao duyệt và xuất đủ bộ. Toàn bộ nằm trong tempart/CannonSonic/. Identity giữ nguyên B; không khôi phục form cũ.

## Xem thử

- qc/preview.gif: idle hai vòng → ba phát thường → phát thứ tư xung kích → trở về idle. Ba đèn lần lượt sáng1/2/3 rồi tắt.
- qc/overview.png: từng thành phần ở tỉ lệ3×; icon6×.
- qc/all-assets.png: tất cả frame, mỗi clip một hàng.
- qc/clips/: GIF riêng cho từng clip; rhythm/lamp-overlay là bảng trạng thái tĩnh.
- qc/preview-3-lamps.png và qc/preview-shock.png: ảnh chụp mốc cộng hưởng/phát mạnh.
- qc/validation-production.md: kết quả kiểm tra alpha, bệ, tư thế nghỉ, đèn và GIF.
- qc/frame-index.tsv: danh sách43frame, kích thước, anchor và số nhịp của các trạng thái tĩnh.

## File sử dụng

| Thư mục | File frame | Số frame | Canvas |
| --- | --- | ---: | --- |
| identity | identity-1.png | 1 | 128×128 |
| idle | idle-1.png … idle-4.png | 4 | 128×128 |
| attack | attack-1.png … attack-6.png | 6 | 128×128 |
| attack-shock | attack-shock-1.png … attack-shock-6.png | 6 | 128×128 |
| rhythm | rhythm-1.png … rhythm-4.png | 4 trạng thái | 128×128 |
| lamp-overlay | lamps-1.png … lamps-4.png | 4 trạng thái | 128×128 |
| wave-normal | wave-1.png … wave-4.png | 4 | 128×128 |
| wave-shock | wave-1.png … wave-6.png | 6 | 128×128 |
| hit-normal | hit-1.png … hit-4.png | 4 | 128×128 |
| hit-shock | hit-1.png … hit-4.png | 4 | 128×128 |
| icon | icon-cannon-sonic.png | 1 | 48×48 |

43 frame/trạng thái ngoài identity. Mỗi thư mục có sheet-transparent.png: bốn frame2×2, sáu frame3×2, icon/identity một ô. Đọc trái→phải rồi trên→dưới. PNG alpha0/255, palette hạn chế, không đen thuần.

## Cách ghép thân và đèn

Idle/attack/attack-shock là thân B với ba đèn tắt. Ghép lamp-overlay ở cùng vị trí canvas để hiển thị số nhịp hiện tại.

| Số nhịp đã tích | Lớp đèn | Thân tĩnh tham khảo |
| ---: | --- | --- |
| 0 | lamps-1.png | rhythm-1.png |
| 1 | lamps-2.png | rhythm-2.png |
| 2 | lamps-3.png | rhythm-3.png |
| 3 | lamps-4.png | rhythm-4.png |

Lamp-overlay gồm vùng ổ đèn tắt/sáng, không chỉ điểm sáng, nên có thể ghi đè trạng thái trước. Chỉ dùng một lớp đèn tại một thời điểm. Rhythm là bốn thân tĩnh đầy đủ để tham khảo/trạng thái đứng chờ, **không phải clip chạy tự động**.

Tại frame phát sóng (frame3, index2): phát thường tăng số nhịp lên1/2/3; phát xung kích xóa nhịp về0. Lúc ngắm/nạp giữ nhịp trước. Không có quái thì giữ lớp đèn đang có, dùng idle.

Thân giật sangPHẢI; lớp đèn thuộc hộp thân phải đi theo:
- attack: chỉ frame4 dịch đèn+1px theoX; các frame khác0.
- attack-shock: frame4 dịch+2px, frame5 dịch+1px; các frame khác0.
- idle/rhythm: không dịch.

Các pixel đèn lấy từ nguồn B tắt/sáng đã tạo bằng image_gen, đặt lên identity cố định. Đèn khác nhau theo số lượng sáng tích lũy, không đổi màu hoặc tự nhấp nháy làm mất ý nghĩa nhịp.

## Sóng và hiệu ứng trúng đòn

Phát thường: attack + wave-normal + hit-normal. Phát thứ4: attack-shock + wave-shock + hit-shock.

Sóng xuất hiện một lần tại frame phát, từ gốc chung giữa hai loa, lan vềTRÁI rồi tan. Cả hai chuỗi dùng cùng envelope88×48 và gốc phát(112,64) trong canvas128×128; sóng mạnh dày/sáng hơn, không tăng vùng hit. Đặt gốc này vào miệng loa chung của thân. Hướng mục tiêu thực tế do game xoay/lật.

Hit-normal/hit-shock đặt tâm(64,64) lên vị trí từng quái bị trúng. Chỉ là phản hồi hình ảnh, không tạo thêm damage hay một hit mỗi frame. Không có bullet vật thể riêng: hai loa phát chung một nhịp, damage hình nón tính một lần/mục tiêu theo mechanics.md.

## Anchor và nhịp

Thân128×128, điểm phát chung đề xuất(32,50), đáy bệy96. Bệ/cổ từy78 trở xuống giữ nguyên pixel B ở mọi frame. Frame nghỉ idle1/4, attack1/6 và attack-shock1/6 khớp identity chính xác.

Preview dùng một phát/giây: attack6 frame lần lượt100/120/90/100/100/90ms, sau đó idle400ms. Wave/hit bắt đầu ở frame3; sóng thường4frame, xung kích6frame (hai frame cuối còn tan khi thân trở vềidle). Đây là nhịp duyệt art; mechanics.md là đề xuất gameplay.

Quái RatRunner trong GIF là ảnh tham chiếu chỉ đọc, không nằm trong asset bàn giao. Vị trí/đẩy lùi trong GIF chỉ diễn tả phản hồi, không mô phỏng tuyến đường của game. Cú đẩy quái thường nay minh họa bằng10px logic (30px ởGIF3×), tương ứng đề xuất0,8ô ở tỉ lệ minh họa12,5px/ô.

## Cơ chế và phạm vi

3 phát thường20 → phát thứ4 sát thương35 + đẩy lùi. Hai loa một hit/mục tiêu. Không quái thì giữ nhịp. Quái thường lùi0,8ô, nặng0,4ô theo đường đi; boss không dịch chuyển, đề xuất chậm15%/1giây; miễn đẩy2giây dùng chung. Chi tiết và giới hạn cân bằng ở mechanics.md.

**Không có asset/animation cooldown.** Sau mỗi phát, thân trở về idle với đèn giữ đúng số nhịp.

Chỉ asset/tài liệu local; chưa import Unity, tạo.meta hoặc sửa gameplay. tempart/ bị Git ignore.

## Tái xuất và nguồn

- qc/export-production.ps1: xuất bộ43frame, sheet, GIF và kiểm tra từ identity/raw/nguồn đèn B.
- production-prompts.md: tám prompt nguồn animation/icon, dùng built-in image_gen và nền alpha.
- qc/production-sources.md: đường dẫn nguồn sinh ảnh, bản sao nằm trongraw/.
- prompts.md, concept-prompts.md và qc/export-concept.ps1: nguồn/tái xuất concept B.
- concept/: mẫu tĩnh ban đầu, giữ làm tư liệu; dùng các thư mục animation ở bảng trên để lấy bộ hoàn chỉnh.
