# 🏰 TỔNG QUAN DỰ ÁN: GROW CASTLE CLONE (PROTOTYPE)

---

## 📌 THÔNG TIN CHUNG
* **Tên dự án:** Grow Castle Clone (Prototype)
* **Thể loại:** Tower Defense + Idle / Clicker
* **Nền tảng / Công cụ:** Unity (C#), Đồ họa 2D
* **Quy mô nhóm:** 3 thành viên
* **Thời gian thực hiện:** 2 tháng (8 tuần)

---

## 1. 🎯 MỤC TIÊU DỰ ÁN
* **Xây dựng bản prototype hoàn chỉnh có thể chơi được:** Tái hiện gameplay cốt lõi của dòng game phòng thủ kết hợp clicker/idle.
* **Kiểm tra Core Gameplay Loop:** Bắn quái $\rightarrow$ Nhận vàng $\rightarrow$ Nâng cấp công trình/anh hùng $\rightarrow$ Phòng thủ trước làn sóng quái mạnh hơn.
* **Đánh giá tính giải trí & Khả thi:** Kiểm chứng mức độ lôi cuốn (engagement), độ cân bằng kinh tế cơ bản và khả năng mở rộng nội dung trước khi bước vào giai đoạn sản xuất bản full game.

---

## 2. 🧩 PHẠM VI DỰ ÁN (SCOPE)

### ✅ Trong phạm vi (In-Scope)
* **Màn chơi & Bối cảnh:** 01 màn chơi duy nhất cố định (Lâu đài ở mép trái màn hình, quái vật xuất hiện và di chuyển từ mép phải).
* **Quái vật (3 loại):**
  * *Quái thường:* Máu và tốc độ cơ bản.
  * *Quái nhanh:* Di chuyển tốc độ cao, máu thấp.
  * *Quái trâu (Tanker):* Di chuyển chậm, lượng máu lớn.
* **Anh hùng (3 vị trí cố định trên lâu đài):**
  * *Cung thủ (Archer):* Bắn tên đơn mục tiêu tầm xa, tốc độ bắn nhanh.
  * *Pháp sư (Mage):* Tấn công diện rộng (AOE) hoặc làm chậm quái.
  * *Chiến binh (Warrior):* Tấn công cận chiến chặn quái trước chân thành hoặc buff sức mạnh.
* **Cơ chế chiến đấu & Tương tác:**
  * Click chuột để bắn tên chủ động (Player engagement).
  * Chế độ tự động bắn (Auto-fire) từ cung thủ/thành lũy và các anh hùng.
* **Hệ thống Nâng cấp & Kinh tế:**
  * Đơn vị tiền tệ: Vàng (Gold) rơi khi tiêu diệt quái.
  * Nâng cấp Lâu đài: Tăng máu tối đa (Max HP).
  * Nâng cấp Cung thủ: Tăng sát thương (Damage) và tốc độ đánh (Attack Speed).
  * Nâng cấp Anh hùng: Tăng sát thương và hiệu ứng kỹ năng.
  * Công thức chi phí nâng cấp tịnh tiến theo cấp độ ($Cost = Base \times Multiplier^{Level}$).
* **Giao diện người dùng (UI/HUD):**
  * *HUD In-Game:* Thanh máu lâu đài, số Wave hiện tại, số vàng sở hữu, danh sách nút bấm nâng cấp kèm giá tiền.
  * *Màn hình Game Over:* Thông báo thất bại, nút thử lại (Retry wave) hoặc reset màn chơi.
* **Phản hồi thị giác & Âm thanh (Game Feel & Audio):**
  * Floating Combat Text (Số sát thương nhảy lên khi quái trúng đòn).
  * Hit Flash (Quái chớp sáng trắng/đỏ khi nhận damage).
  * Sound Effects (SFX): Tiếng bắn cung, tiếng quái chết, tiếng rơi/nhận vàng, tiếng bấm nâng cấp, 01 bản nhạc nền lặp lại (BGM).
* **Quản lý dữ liệu & Lưu trữ:**
  * Dùng `ScriptableObject` để cấu hình chỉ số quái, chỉ số nâng cấp và kịch bản Wave.
  * Dùng `PlayerPrefs` để lưu tiến trình cơ bản (Vàng hiện có, level nâng cấp, wave cao nhất đạt được).

### ❌ Ngoài phạm vi (Out-of-Scope)
* Nhiều bản đồ / địa hình khác nhau.
* Hệ thống trang bị, phụ kiện, cây kỹ năng (Skill Tree) phân nhánh.
* Boss với hành vi AI phức tạp (phase chuyển đổi, triệu hồi quái đệ).
* Đồ họa độ phân giải cao, rigging animation phức tạp (chỉ dùng sprite 2D đơn giản).
* Tính năng Online, Cloud Save, Bảng xếp hạng (Leaderboard), Mạng xã hội, In-App Purchase (IAP).

---

## 3. 👥 PHÂN CHIA NHÂN SỰ & VAI TRÒ

| Thành viên / Vai trò | Trách nhiệm chính |
| :--- | :--- |
| **Lập trình viên (Programmer)** | • Xây dựng toàn bộ kiến trúc mã nguồn & logic gameplay.<br>• Triển khai Object Pooling (quái vật, mũi tên, floating text).<br>• Tích hợp UI, Audio, ScriptableObject và hệ thống Save/Load.<br>• Tối ưu hiệu năng và đóng gói (Build) sản phẩm. |
| **Họa sĩ / Thiết kế (2D Artist)** | • Vẽ sprite sheet/tài nguyên đồ họa 2D: Lâu đài, 3 loại quái, 3 anh hùng, đạn/mũi tên.<br>• Thiết kế các icon UI (nút bấm, thanh máu, biểu tượng vàng/cấp độ).<br>• Hiệu ứng hình ảnh cơ bản (Visual feedback: Hit flash, vệt bắn, hiệu ứng vỡ quái). |
| **Thiết kế Game & QA (Game Designer / QA)** | • Thiết kế kịch bản các đợt sóng quái (Wave spawner) và độ khó tăng dần.<br>• Cân bằng công thức kinh tế (Lượng vàng rơi vs Chi phí nâng cấp).<br>• Tìm kiếm, tuyển chọn nguồn âm thanh SFX/BGM miễn phí bản quyền.<br>• Viết Test Cases, thực hiện kiểm thử chức năng, cân bằng và báo cáo lỗi (Bug tracking). |

---

## 4. 🗓️ LỘ TRÌNH PHÁT TRIỂN 8 TUẦN

| Giai đoạn (Tuần) | Mục tiêu & Đầu việc cụ thể |
| :---: | :--- |
| **Tuần 1 – 2**<br>*(Core Loop Thô)* | • Dựng khung cảnh màn chơi cơ bản (Lâu đài bên trái, quái spawn bên phải).<br>• Logic cơ bản: Click bắn tên $\rightarrow$ quái mất máu/chết $\rightarrow$ cộng vàng.<br>• Nâng cấp cơ bản cho lâu đài và sát thương bắn tên.<br>• Dùng placeholder asset (khối hình học/sprite tạm). |
| **Tuần 3 – 4**<br>*(Wave System & Pooling)* | • Xây dựng **Object Pooling** cho đạn và quái vật.<br>• Hoàn thiện 3 loại quái vật với các thuộc tính tốc độ, máu khác nhau.<br>• Quái tiếp cận lâu đài gây sát thương; lâu đài hết máu $\rightarrow$ Game Over.<br>• Hệ thống Spawner quản lý theo đợt (Waves). |
| **Tuần 5 – 6**<br>*(Hệ thống Anh hùng & Auto-fire)* | • Đặt 3 vị trí anh hùng trên lâu đài, kích hoạt chế độ tự động bắn (Auto-attack).<br>• Bổ sung bảng nâng cấp riêng biệt cho từng anh hùng.<br>• Thay thế tài nguyên đồ họa hoàn chỉnh từ Artist vào Unity. |
| **Tuần 7 – 8**<br>*(Đánh bóng, Cân bằng & Đóng gói)* | • Tích hợp Audio (SFX + BGM) và hiệu ứng thị giác (Floating text, hit flash).<br>• Thiết lập hệ thống lưu trữ tiến trình bằng `PlayerPrefs`.<br>• QA cân bằng số liệu kinh tế và nhịp độ độ khó của các đợt quái.<br>• Tối ưu hiệu năng, sửa lỗi tồn đọng và Build file `.exe` / `.apk` hoàn thiện. |

---

## 5. ⚙️ CÁC HỆ THỐNG KỸ THUẬT CỐT LÕI

1. **Hệ thống Quái vật (Enemy System):**
   * Quản lý vòng đời (Spawn $\rightarrow$ Di chuyển $\rightarrow$ Tấn công $\rightarrow$ Nhận sát thương $\rightarrow$ Chết/Rơi vàng).
   * Tối ưu thông qua Quản lý đối tượng (Object Pool).
2. **Hệ thống Chiến đấu (Combat & Projectile System):**
   * Quản lý đạn bắn (Trajectory, Hit detection).
   * Tính toán sát thương, áp dụng hiệu ứng debuff (nếu có).
3. **Hệ thống Anh hùng & Lâu đài (Castle & Hero System):**
   * Theo dõi trạng thái HP của thành, kích hoạt trạng thái thất bại khi $HP \le 0$.
   * Quản lý tầm bắn (Range), tốc độ đánh (Attack Speed) và mục tiêu tự động nhắm tới (Targeting logic).
4. **Hệ thống Quản trị Dữ liệu & Cân bằng (Data-Driven Architecture):**
   * Toàn bộ chỉ số quái, giá nâng cấp, danh sách wave được tách biệt thành `ScriptableObject` để Designer dễ tinh chỉnh.
5. **Hệ thống Kinh tế & Nâng cấp (Economy & Upgrade System):**
   * Xử lý giao dịch nạp/trừ vàng, cập nhật thuộc tính runtime tương ứng sau khi nâng cấp.
6. **Hệ thống Lưu trữ (Persistence System):**
   * Đọc/Ghi dữ liệu trạng thái game vào `PlayerPrefs` khi hoàn thành Wave hoặc thoát game.
