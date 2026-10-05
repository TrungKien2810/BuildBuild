# Pháo Hắc Tinh — hút, tích sức mạnh rồi phát nổ

Thiết kế để duyệt. Các số là mốc thử, chưa được cân bằng trong game.

## Cộng dồn từ quái chết

Trong giai đoạn hút, mỗi quái địch chết trong bán kính hố đen tăng một cộng dồn. Sát thương kết liễu có thể đến từ bất kỳ pháo hoặc đơn vị cùng phe nào. Pháo Hắc Tinh trở thành công cụ gom quái và tận dụng các phát kết liễu của đội hình.

Số cộng dồn s = min(5, số quái chết hợp lệ). Sát thương cuối = D × (1 + 0,15 × s). Đây là tăng tuyến tính trên D, không nhân lũy tiến sau mỗi cái chết.

| Cộng dồn | Sát thương khi D = 100 |
|---:|---:|
| 0 | 100 |
| 1 | 115 |
| 2 | 130 |
| 3 | 145 |
| 4 | 160 |
| 5 trở lên | 175 |

Cộng dồn chỉ tăng sát thương vụ nổ cuối. Không tăng lực hút, bán kính hoặc kéo dài thời gian. Quái nhỏ và boss đều đóng góp một cộng dồn mỗi cái chết; không dùng lượng HP hoặc sát thương kết liễu làm hệ số.

## Thông số ban đầu

| Tham số | Giá trị thử |
|---|---:|
| Sát thương nổ gốc D, level 1 | 100 |
| Cộng dồn tối đa | 5 |
| Bonus mỗi cộng dồn | +15% D |
| Thời gian hút | 1,5 s |
| Bán kính hút và nổ | 1,6 đơn vị world |
| Tầm chọn vùng | 8 đơn vị world |
| Khoảng cách tối thiểu giữa hai phát thành công | 10 s |
| Bay đến vùng đã chọn | khoảng 0,3–0,5 s |

Giữ cùng bán kính hút/nổ để cảnh báo thể hiện đúng diện tích trúng. Trần sát thương một mục tiêu ở level 1 là 175/10 = 17,5 DPS nếu đủ cộng dồn mọi chu kỳ; giá trị SSR chính nằm ở gom quái và sát thương trên nhiều mục tiêu. Cần thử cùng pháo nổ, boss và đợt quái đông trước khi chốt số.

## Trình tự một phát

1. Chọn vùng đông quái trong tầm, ưu tiên vùng gần thành phe mình nếu cùng mật độ. Không có quái thì giữ idle.
2. Khóa điểm đến khi phóng lõi. Lõi bay đến điểm, tạo hố đen cố định trên chiến trường; không chạy theo mục tiêu sau đó.
3. Hố đen mở và hút trong 1,5 s. Bản đầu không gây sát thương liên tục trong giai đoạn này: các pháo/đơn vị khác kết liễu quái để nạp sức mạnh.
4. Khi một cái chết hợp lệ xảy ra, tăng s và phát VFX hấp thu nhỏ. Đủ 5 thì giữ mức tối đa, không kéo dài hút.
5. Hết 1,5 s, khóa số cộng dồn, chuyển sang co lõi. Thời gian VFX co lõi tham khảo 0,2 s; giai đoạn này không tính thêm cộng dồn.
6. Nổ một lần vào quái còn sống nằm trong bán kính tại thời điểm nổ, rồi xóa hố đen. Mỗi quái nhận đúng một hit của vụ nổ này. Chu kỳ nạp bắt đầu từ lúc phóng lõi thành công; giữa các phát thân pháo dùng idle.

## Quy tắc tránh cộng trùng

- Xét vị trí chiến trường của quái tại thời điểm chết, không kéo xác chết vào hố rồi tính lại.
- Mỗi ID quái chỉ đóng góp một lần. Callback death/animation chết lặp không tăng thêm.
- Cùng một cái chết nằm trong nhiều hố đang hút: chỉ hố có tâm gần nhất nhận cộng dồn; cùng khoảng cách thì chọn hố tạo trước, sau đó ID ổn định.
- Quái bị vụ nổ cuối giết không tăng sức mạnh của chính vụ nổ đã tính. Nếu quái đó nằm trong một hố khác vẫn đang hút, hố kia có thể nhận một cộng dồn theo quy tắc trên.
- Mỗi hố có bộ đếm độc lập. Không giữ cộng dồn sang phát sau, không tích trữ qua wave hoặc lưu vào thân pháo.
- Các cái chết cùng bước mô phỏng phải được xử lý theo quy tắc nhất quán. Khi đóng giai đoạn hút, xử lý sự kiện chết thuộc bước đó trước khi chốt s; các sự kiện sau mốc đóng không tính.

## Hút và kháng khống chế

Quái thường bị kéo dần về tâm với tốc độ có giới hạn, không dịch chuyển tức thì hoặc đi xuyên vật cản mà game coi là không thể vượt. Boss có thể chịu lực hút bằng khoảng 25% quái thường; quái có miễn nhiễm kéo giữ nguyên miễn nhiễm. Boss vẫn nhận sát thương nổ theo pipeline hiện có.

Quái bay cần giữ độ cao và chỉ đổi vị trí ngang/mặt đất theo hệ thống chiến trường; không tự biến thành quái đất. Phạm vi ảnh hưởng lên quái bay phải được chốt cùng quy tắc AOE của dự án trước khi tích hợp.

## Cách thể hiện cộng dồn bằng art

0 cộng dồn: lõi navy tối, vành đỏ trầm và ít hạt. 1–2: tăng độ sáng và vài hạt nóng. 3–5: vành scarlet/ivory mạnh, vòng hạt năng lượng dày hơn. Cả ba mức giữ footprint giống nhau. Không gắn số/counter vào PNG VFX; UI nếu cần là phần riêng của người tích hợp.

Mỗi lần quái chết hợp lệ: hạt đỏ nhỏ chạy vào tâm, vành khép sáng một nhịp. Khi nổ: lõi co nhỏ rồi biến mất, chớp đỏ–ivory và sóng xung lực bật ra. Vụ nổ không kéo dài thành lửa phun.

## Tình huống cần kiểm tra khi triển khai

Không ai chết: vẫn nổ D. Ba quái chết: 1,45D. Mười quái chết: 1,75D. Quái bị pháo khác giết: tính. Xác chết trôi vào vùng: không tính. Callback chết lặp: tính một lần. Hai hố chồng nhau: một cái chết chỉ nạp một hố. Nổ cuối giết quái: không tự tăng hit đang diễn ra. Quái chạy khỏi vùng trước nổ: không nhận hit. Boss miễn kéo: không bị kéo, vẫn kiểm tra hit nổ.

Chưa có yêu cầu sửa code trong phiên này; đây là quy tắc bàn giao để triển khai sau khi duyệt.
