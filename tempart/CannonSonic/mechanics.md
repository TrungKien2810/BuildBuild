# Pháo Âm Thanh — SR · CannonSonic · sonic

Đề xuất thiết kế theo ý tưởng người giao, chưa triển khai gameplay.

## Vì sao hợp SR

Pháo gây sát thương diện hình nón và đẩy lùi theo nhịp, mạnh nhất khi đánh đàn quái và phối hợp với pháo gây sát thương khác. Chu kỳ ba phát thường rồi một phát mạnh có tín hiệu rõ qua ba đèn. Giữ SR với tầm vừa, hướng đánh hữu hạn, sát thương vừa phải và chỉ phát thứ tư có khống chế; không thêm hút, choáng, xuyên toàn bản đồ hay nổ dây chuyền.

Ba đèn phải báo số phát thường đã tích. Người chơi có thể biết phát kế tiếp là xung kích khi cả ba đèn sáng.

## Thông số thử

| Thuộc tính | Giá trị mẫu |
| --- | --- |
| Nhịp bắn | 1 phát/giây; hai loa cùng phát một nhịp |
| Đánh thường | 20 sát thương/mục tiêu |
| Phát thứ tư | 35 sát thương/mục tiêu |
| Tầm chọn mục tiêu | 4,5 ô; đo từ điểm phát chung của cặp loa |
| Hình nón | Bán kính4,5 ô, góc mở60°; cùng vùng cho hai loại phát |
| Số phát thường trước xung kích | 3 |
| Quái thường | Lùi0,8 ô theo đường đi |
| Quái nặng | Lùi0,4 ô theo đường đi |
| Boss/miễn đẩy | Không dịch chuyển |
| Thay thế trên boss | Làm chậm15% trong1 giây; giá trị thử |
| Miễn đẩy sau một lần đẩy thành công | 2 giây, dùng chung mọi nguồn Pháo Âm Thanh |
| VFX sóng thường | Xanh nhạt pha bạc, đường cong mảnh |
| VFX xung kích | Vàng trắng, đường cong dày và loa rung rõ |
| Cooldown art | Không có; thân trở về idle |

Các giá trị ô/giây là đề xuất để thử, chưa đo theo bản đồ thực tế.

## Nhịp cộng hưởng và thời điểm tính

Bộ đếm c thuộc {0,1,2,3}. Bắt đầu c=0.

1. Khi c<3, phát thật sự được phóng là phát thường. Sau khi phát, c tăng1 và một đèn mới sáng.
2. Khi c=3, phát kế tiếp là Sóng Xung Kích. Sau khi phát, c trở về0 và cả ba đèn tắt.
3. Không có mục tiêu hợp lệ thì giữ c. Không tích theo thời gian, không mất nhịp vì idle lâu.
4. Bắt đầu animation ngắm/nạp chưa tính là bắn. Chỉ cập nhật c tại thời điểm phát sóng thật sự.
5. Nếu mất mọi mục tiêu trước khi phát, hủy phát và giữ c; nếu sóng đã phát thì tính phát dù mục tiêu chết trước lúc xử lý hiệu ứng tiếp theo.
6. Bản thử giữ c qua khoảng nghỉ và sang đợt quái tiếp theo; tháo/thay pháo hoặc bắt đầu trận mới thì về0.

Ví dụ: t=0/1/2 giây lần lượt bắn thường và sáng1/2/3 đèn; t=3 giây bắn mạnh và tắt đèn. Nếu đánh liên tục, các xung kích cách nhau4 giây. Chỉ có ba phát tổng cộng mà sau đó không còn quái thì đèn giữ3, đợi mục tiêu mới để bắn mạnh.

## Một hình nón, một lần sát thương

Hai loa là một khẩu pháo, cùng phát một nhịp. Dùng một điểm phát chung nằm giữa hai miệng loa; không tạo hai hit độc lập để một quái bị ăn sát thương gấp đôi.

Chọn mục tiêu hợp lệ trong tầm, lấy hướng từ điểm phát đến mục tiêu và khóa hướng ở thời điểm phát. Tại thời điểm đó, chụp danh sách quái trong hình nón. Mỗi unitId nhận sát thương tối đa một lần cho shotId này.

Các đường cong lan ra chỉ diễn tả sóng, không phải nhiều viên đạn hoặc các vùng sát thương liên tục. Quái vào hình nón sau thời điểm chụp không bị đánh bởi phát đã xảy ra. Quái trong nón ở lúc phát nhận sát thương ngay, dù frame sóng sau đó đang tan. Không tạo projectile gây damage trên mỗi frame, không tạo hit riêng cho từng đường cong.

Ví dụ: năm quái trong nón thường → mỗi con20; tổng100. Phát mạnh → mỗi con35; tổng175, sau đó xử lý đẩy lùi cho từng con còn sống.

Sát thương trung bình mẫu nếu một mục tiêu luôn ở trong nón: (20+20+20+35)/4 =23,75 mỗi giây. Đây chưa tính giáp, crit, thời gian tìm mục tiêu hoặc số quái.

Quy tắc chọn mục tiêu bản thử: ưu tiên quái gần tới thành nhất theo tiến độ đường đi; nếu bằng nhau dùng khoảng cách rồi unitId để tránh hướng ngắm rung. Chỉ đánh nhóm trong nón hướng về mục tiêu đó, không AOE360°. Quy tắc lớp quái/đất/bay cần dùng cùng hệ thống targeting của dự án; art không gắn giả định “chỉ quái đất”.

## Đẩy theo đường đi

Áp sát thương trước. Chỉ quái còn sống sau hit mới nhận khống chế.

Đẩy giảm tiến độ dọc tuyến đường mà chính quái đang đi, không lấy vector thẳng từ loa để hất quái ra ngoài làn. Đi qua góc cua theo tuyến; không xuyên tường, đổi làn hoặc đẩy trước điểm spawn. Giới hạn lùi tới đầu đường.

- Thường: lùi0,8 ô.
- Nặng: lùi0,4 ô.
- Boss hoặc unit có miễn đẩy: không dịch chuyển. Boss trong bản thử nhận chậm15%/1giây thay thế; vẫn nhận damage35.
- Quái đang miễn đẩy2 giây: vẫn nhận damage35; không đẩy, không reset/kéo dài miễn đẩy, không tự chuyển sang hiệu ứng chậm dành cho boss.
- Chỉ đẩy thành công với khoảng cách thực tế>0 mới đặt miễn đẩy2 giây.
- Nhiều xung kích cùng lúc: sau cú đẩy đầu, các cú còn lại không đẩy thêm trong2 giây.
- Chậm boss không cộng dồn phần trăm khi nhiều pháo bắn. Dùng mức chậm mạnh hơn đang có; cùng mức chỉ làm mới thời gian theo quy tắc debuff chung, không ghi đè hiệu ứng chậm mạnh hơn bằng15%.

Miễn đẩy2 giây giới hạn tần suất, chưa tự đảm bảo quái không bị giữ mãi: nhiều pháo lệch nhịp vẫn có thể lùi tối đa0,8 ô mỗi2 giây. Quái có tốc độ tiến dưới0,4 ô/giây có thể bị giữ tại chỗ. Khi thử nhiều pháo trên một làn, cần xem tiến độ quái; nếu bị khóa thì thử kéo dài miễn đẩy hoặc giảm khoảng cách của các cú đẩy liên tiếp, không tăng damage để bù.

## Hình ảnh phải giải thích cơ chế

- Hai miệng loa, không có viên đạn đặc hoặc nòng súng dài.
- Ba đèn riêng rẽ, sáng tích lũy đúng1→2→3 rồi cùng tắt sau phát thứ4.
- Sóng thường: các cung cong mảnh lan về TRÁI thành hình nón; màu xanh nhạt theo yêu cầu người giao. Không vẽ gai băng, tuyết, tinh thể hoặc đóng băng.
- Xung kích: cung cong vàng trắng dày/sáng hơn, vẫn cùng độ dài và góc mở. “Sóng lớn” nghĩa là nét dày, biên độ mạnh, không tăng vùng hit.
- Loa rung/recoil mạnh hơn ở phát thứ4 nhưng bệ đứng yên.
- Không chữ/số đếm trong sprite; nhãn và vùng hit chỉ xuất hiện ở bảng QC.
- Không vẽ cooldown. Thời gian giữa các phát do cơ chế xử lý; thân dùng idle.
