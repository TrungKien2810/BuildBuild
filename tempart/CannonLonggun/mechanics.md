# Hoả Mai Thành — thiết kế cơ chế để duyệt

Vai trò: súng bắn tỉa tự động trên thành, diệt mục tiêu nhiều máu bằng từng phát mạnh, nhịp bắn rất chậm. Người chơi nhận ra mục tiêu qua tâm đỏ trên đầu và nhận ra phát bắn qua chớp nhỏ, khói dài, đạn nhanh và impact tập trung.

## Thông số khởi điểm đề xuất

| Tham số | Giá trị thử | Ý nghĩa |
|---|---:|---|
| Sát thương gốc level 1 | 100 | Một mục tiêu, sát thương vật lý theo pipeline hiện có |
| fireRate | 0,25 phát/s | Khoảng cách tối thiểu giữa hai phát thành công 4 s |
| range | 10 đơn vị world | Dài hơn Nỏ Xuyên đang có tầm 9; cần duyệt với Pháo Cối |
| Thời gian ngắm | 0,6 s | Tâm đỏ báo mục tiêu trước khi bắn |
| projectileSpeed | 30 đơn vị/s | Cảm giác đạn nhanh; không trì hoãn hit theo animation impact |
| Mục tiêu mỗi phát | 1 | AOE=0, pierceCount=0 |
| Tăng sát thương theo level | +8% sát thương gốc/level | Theo quy tắc hiện tại, sao dùng Progression của dự án |

Các số là mốc thử, chưa cân bằng hoặc tích hợp. DPS lý thuyết liên tục ở level 1 là 100/4=25 trước giáp và hao sát thương do kết liễu. Liên Thanh hiện tại 12×2=24 DPS. Lợi thế của Hoả Mai Thành là dồn sát thương và chọn mục tiêu nhiều máu; bất lợi là khoảng trống 4 giây và dễ phí sát thương trên quái nhỏ. Không tự thêm xuyên giáp, crit, hệ số theo % máu hoặc hiệu ứng nguyền. Cần thử với boss, giáp và đợt quái thực tế trước khi chốt 100 damage.

Tầm dùng cùng phép đo AimPoint như Cannon hiện tại để không tạo sai khác chỉ riêng súng này. Tầm 10 là đề xuất; tài liệu giao việc nói Cối xa nhất nên người tích hợp cần giữ thứ tự đó khi chốt tầm của cả bộ.

## Chọn mục tiêu

1. Chỉ lấy quái địch còn sống, có thể nhận sát thương và trong tầm. Quái bay và dưới đất đều được chọn nếu game coi là mục tiêu hợp lệ.
2. Ưu tiên **máu hiện tại tuyệt đối cao nhất**, không dùng phần trăm máu hoặc máu tối đa. Ví dụ boss còn 80/1000 HP và quái còn 120/120 HP: chọn quái 120 HP.
3. Cùng máu: chọn quái gần thành của phe mình hơn. Nếu vẫn bằng nhau, dùng ID spawn ổn định để tránh tâm đỏ nhảy qua lại.
4. Khoá mục tiêu trong 0,6 s ngắm. Quái mới có nhiều máu hơn xuất hiện trong khoảng này không kéo tâm sang quái khác.
5. Mỗi chu kỳ bắn chọn lại; không cố giữ mục tiêu của phát trước. Nhiều Hoả Mai Thành được phép cùng chọn một quái; không bổ sung đặt chỗ sát thương ở bản đầu.

## Chu kỳ và quy tắc huỷ

Ready → Aim (0,6 s) → Fire (đúng 1 phát) → Recover/Reload (đủ cooldown) → Ready.

- Khi vừa lắp súng, cooldown bằng 0; có mục tiêu thì ngắm 0,6 s rồi mới bắn.
- Tâm đỏ xuất hiện khi bắt đầu Aim, bám trên đầu quái đã khoá, biến mất khi Fire hoặc huỷ ngắm. Súng không bắn khi chưa hoàn tất ngắm.
- Khi Fire, kiểm tra lại quái còn sống và trong tầm. Animation hit-frame phát một viên, damage=100 ở level 1. Cooldown bắt đầu tại thời điểm phát đạn thành công; animation hay callback lặp không được phát thêm viên.
- Cooldown giảm liên tục trong Recover/Reload. Sau 3,4 s kể từ Fire có thể bắt đầu Aim kế tiếp để viên sau ra ở mốc 4 s. Điều kiện bắt đầu ngắm: cooldown còn ≤0,6 s; điều kiện Fire: cooldown ≤0. Nếu lúc đó không có mục tiêu, chờ; thời gian rảnh không tích luỹ nhiều phát.
- Mục tiêu chết, mất khả năng bị đánh hoặc ra ngoài tầm trước Fire: bỏ tâm ngay, huỷ Aim và chọn lại nếu cooldown cho phép. Mục tiêu mới phải ngắm đủ 0,6 s; phát bị huỷ không tiêu cooldown.
- Mục tiêu chỉ mất HP nhưng còn hợp lệ: giữ khoá đến Fire, không so lại thứ tự máu giữa chừng.
- Sau khi viên đạn đã ra: giữ target riêng của viên đó, không đổi sang quái có máu cao hơn. Đạn theo cơ chế projectile đơn mục tiêu hiện có. Nếu target chết trước impact thì viên đạn bị huỷ mà không gây sát thương hoặc chuyển sang mục tiêu mới; cooldown đã tiêu vẫn giữ.
- Đạn không bị quái khác chắn đường. Sát thương áp dụng đúng một lần tại hit; sprite impact không tự gây damage. Tâm đỏ không làm tăng sát thương.
- Tắt/tháo súng hoặc kết thúc trận: bỏ marker và huỷ Aim, không có callback bắn muộn. Pause dùng thời gian gameplay nên ngắm, cooldown và VFX cùng dừng.

## Nhịp animation và gameplay

Sáu frame attack dùng thời lượng khác nhau: attack-1 ngắm 0,4 s; attack-2 siết cò 0,2 s; bắt đầu attack-3 phát đạn; attack-3 chớp 0,06 s; attack-4 giật/khói 0,12 s; attack-5 khói tan 0,16 s; attack-6 hồi về 0,16 s. Timeline đây là đề xuất, không buộc cooldown kéo dài theo tổng thời lượng clip.

Nếu SpriteAnimator chỉ cho FPS cố định, chạy Aim riêng trong logic và dùng attack-1/2 làm pose trước Fire, hoặc thêm hỗ trợ timing khi tích hợp. Không kéo sáu frame đều nhau rồi phát đạn sai mốc 0,6 s. Chỉ một điểm release; xác nhận hitFrame của dự án tính từ 0 hay 1 trước khi khai asset.

## Việc người tích hợp cần làm sau duyệt

Code đang chọn quái gần thành nhất trong Cannon.FindTarget(), chưa có chế độ highest-current-health, trạng thái Aim hoặc marker khóa mục tiêu. CannonDefinition chưa có cấu hình riêng cho các phần này. Đây là danh sách yêu cầu triển khai, chưa phải chức năng đã chạy.

- Thêm lựa chọn target priority cho từng loại pháo, mặc định giữ cách chọn hiện tại cho các loại cũ.
- Thêm trạng thái ngắm, kiểm tra target tại Fire và chỉ bắt đầu cooldown khi thực sự phát đạn theo timeline trên.
- Gắn marker theo head/status anchor của quái, xử lý cả hai phe; nhiều súng khoá cùng quái nên marker có owner/ref-count để một súng huỷ không xoá marker của súng khác.
- Tạo Cannon_Longgun và bộ animation sau khi art được duyệt; xác định muzzle chính xác từ identity chọn, với pivot tại điểm lắp/bệ thay vì mặc định tâm canvas vì bệ nằm lệch phải.
- Mirror muzzle, bullet và smoke theo hướng phe. Dùng projectile không AOE/không xuyên và huỷ an toàn khi target chết.
- Dùng quy tắc nâng cấp, level và sao hiện có; cân bằng chi phí/độ hiếm cùng các pháo khác.

## Các tình huống nghiệm thu khi triển khai

| Tình huống | Kết quả cần thấy |
|---|---|
| Ba quái trong tầm: HP 50, 120, 80 | Khoá quái HP 120 |
| Boss 80/1000 và quái 120/120 | Khoá quái HP hiện tại 120 |
| Quái HP 500 ngoài tầm, HP 100 trong tầm | Khoá HP 100 |
| Hai quái cùng HP | Ưu tiên gần thành phe mình, rồi ID ổn định |
| Quái HP cao hơn xuất hiện trong Aim | Giữ target đang khoá; xét lại chu kỳ sau |
| Target chết/ra khỏi tầm lúc Aim | Tâm cũ mất; chọn mới và ngắm lại đủ 0,6 s |
| Target chết khi đạn đang bay | Không damage lên target khác; không hoàn cooldown |
| Đủ target liên tục trong 12 s | Các phát ở t=0,6; 4,6; 8,6 s, mỗi phát đúng một hit |
| Không có target trong 20 s rồi xuất hiện | Ngắm 0,6 s và bắn một viên, không dồn 5 viên |
| Nhiều súng khoá một quái, một súng bị tháo | Marker vẫn tồn tại nếu còn súng đang ngắm |
| Hai phe / quái bay / mục tiêu đang chạy | Tâm trên đầu; nòng, đạn và khói đúng phía |
| Pause hoặc kết thúc trận khi ngắm | Không phát đạn muộn hoặc để lại marker |

Các tình huống trên là kế hoạch nghiệm thu; chưa chạy vì phiên này chỉ bàn giao art Đợt A và thiết kế cơ chế.
