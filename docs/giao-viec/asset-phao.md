# Giao việc: asset 17 pháo mới (chỉ làm ART)

Người nhận: bạn hỗ trợ art + Codex của bạn. Người duyệt: chủ dự án (TrungKien).

---

## 0. LUẬT CỨNG — đọc trước, không có ngoại lệ

> **TUYỆT ĐỐI KHÔNG ĐỤNG VÀO CODE. CHỈ LÀM ASSET.**

- **Chỉ được tạo/sửa file trong `tempart/Cannon<Tên>/`** (thư mục riêng của từng pháo trong danh sách mục 4).
  Ngoài thư mục đó thì **không ghi, không sửa, không xoá bất cứ thứ gì**.
- **Cấm** đụng vào: `Assets/` (gồm `Assets/Scripts`, `Assets/Editor`, `Assets/Data`, `Assets/Scenes`,
  `Assets/Art`), `ProjectSettings/`, `Packages/`, `config/`, `docs/`, `.claude/`, `.govern/`, mọi file `.cs`,
  `.asset`, `.unity`, `.meta`, `.asmdef`, `.json` cấu hình. Thấy chỗ nào "nên sửa" thì **ghi chú lại báo chủ dự án**,
  không tự sửa.
- **Không mở Unity để import.** Đưa ảnh vào game (`Assets/Art/Generated/`, `.meta`, ScriptableObject, code) là
  việc của chủ dự án sau khi duyệt.
- **Không sửa `tempart/REVIEW.html`** và không sửa asset của unit khác, kể cả 4 pháo đã có.
- Git: làm trên **nhánh riêng** `asset/phao-<tên-bạn>`; **không** commit/push lên `dev` hay `main`; không
  `git add -A` (chỉ add đúng thư mục `tempart/Cannon<Tên>/`).
- Nếu bạn giao cho Codex: **dán nguyên mục 0 này vào đầu prompt** của mỗi phiên Codex.

---

## 1. Game và chỗ của pháo

Game thủ thành 2D pixel-art: quái đi từ **phải sang trái** đánh vào pháo đài đá ở mép trái màn hình. Trên pháo đài
có các **bệ pháo**; mỗi bệ lắp 1 pháo tự bắn. Pháo **đứng yên**: không có walk, không di chuyển.

Đã có 4 pháo. Pháo mới phải trông **cùng một bộ** với 4 pháo này (cùng bệ, cùng nét vẽ, cùng cỡ), nhưng nhìn
silhouette là phân biệt được:

| Pháo đã có | Ảnh mẫu (mở xem trước khi vẽ) | Dáng |
|---|---|---|
| Liên Thanh | `Assets/Art/Generated/CannonRapid/identity/identity-1.png` | 3 nòng nhỏ, súng máy |
| Nổ | `Assets/Art/Generated/CannonBlast/identity/identity-1.png` | Nòng to béo chếch lên, cối |
| Băng | `Assets/Art/Generated/CannonFrost/identity/identity-1.png` | Nòng thon + tinh thể cyan |
| Nỏ Xuyên | `Assets/Art/Generated/CannonBallista/` (cả bộ) | Nỏ lớn gỗ-sắt bắn lao |

Raw gốc (ảnh tham chiếu cho model sinh ảnh) nằm ở `tempart/CannonRapid/identity/`, `tempart/CannonBlast/`,
`tempart/CannonFrost/`, `tempart/CannonBallista/`. Brief đã dùng cho Nỏ Xuyên (mẫu cách viết prompt):
`config/agent-bridge/briefs/cannon-ballista-identity.md` và `cannon-ballista-all.md`. **Chỉ đọc, không sửa.**

Luật vẽ toàn dự án: `docs/project-state/art-style.md`, đọc kỹ mục **6 (nét, pixel, shading)**, **7 (màu)**,
**9 (icon)**, **11 (VFX phải giải thích gameplay)**.

---

## 2. Thông số kỹ thuật chung (mọi pháo)

| Mục | Quy định |
|---|---|
| Kích thước frame thân pháo | **128×128 px**; pháo dài ngang được dùng **144×128** (như Nỏ Xuyên) |
| Chân bệ | Đáy bệ chạm **y = 96** (tính từ trên xuống), giống hệt 4 pháo cũ, ở **mọi frame** |
| Cỡ | Thân pháo cao **~60–75 px**, rộng ~60–75 px (4 pháo cũ: 61–76 px rộng, 62–75 px cao) |
| Hướng | Nòng/đầu bắn **hướng sang TRÁI** (quy ước dự án; game tự lật khi cần) |
| Camera | Giống pháo cũ: nhìn từ trước hơi cao (~25–30°), bệ lộ mặt trên hình bát giác |
| Bệ | **Giữ bệ đá bát giác + cổ trụ thép** của Liên Thanh, trừ khi thẻ pháo ghi khác |
| Nền | PNG **trong suốt** (alpha). Raw sinh trên nền `#FF00FF` thì phải tách nền sạch, **không còn viền hồng** |
| Nét | Viền ngoài navy/nâu đậm dày 1 px logic, **không dùng `#000000`**; không gradient, không anti-alias, không blur |
| Màu | 1 màu chính + 1 màu phụ + 1 accent gameplay + nhóm trung tính; sáng từ trên-trái |
| Chi tiết | 4–6 mảng vật liệu lớn; chi tiết nhỏ nhất ≥ 2×2 px logic; không chữ/số/rune viết chữ |
| Cấm vẽ kèm | Bóng đổ dưới đất, vòng chọn, thanh máu, người điều khiển, khung UI |

Ý nghĩa màu (không được dùng sai): **cyan/xanh băng = làm chậm** (đã thuộc Pháo Băng) · đỏ/cam = sát thương/lửa ·
xanh lá = hồi máu · tím = nguyền/triệu hồi · vàng = buff/nâng cấp.

### Bộ ảnh mỗi pháo phải nộp

| Thư mục | Số frame | Kích thước frame | Ghi chú |
|---|---|---|---|
| `identity/` | 1 | 128×128 (hoặc 144×128) | Hình chuẩn để duyệt dáng |
| `idle/` | 4 | như identity | Chờ bắn: nhấp nháy nhẹ/lắc nhẹ 1–2 px, **bệ không xê dịch** |  
| `attack/` | 4–6 | như identity | Nạp → bắn (giật nòng, chớp nòng) → hồi về đúng tư thế idle-1 |
| `bullet/` | 4 | 128×128, vật thể ở giữa | Đạn/vật bắn ra: vẽ **mũi hướng TRÁI** giống thân pháo (game tự lật và nghiêng theo hướng bay); cỡ 12–30 px (đạn dài như lao thì dài hơn) |
| `bullet-impact/` | 4 | 128×128, ở giữa | Nổ/chạm, cỡ 20–50 px; vùng AOE thì to theo vùng |
| `icon/` | 1 | **48×48** | Icon thẻ gacha, đặt tên `icon-cannon-<slug>.png`; mẫu `Assets/Art/Generated/UI/icons/icon-cannon-blast.png` |

Thêm phần **riêng từng pháo** ghi ở mục 4.

**Đặt tên file** (y như pháo cũ): `identity/identity-1.png`, `idle/idle-1.png`…`idle-4.png`,
`attack/attack-1.png`…, `bullet/projectile-1.png`…, `bullet-impact/impact-1.png`…; mỗi thư mục có thêm
`sheet-transparent.png` (ghép các frame, 2 cột: 4 frame = lưới 2×2, 6 frame = lưới 3×2). Giữ raw gốc trong
`raw/` cạnh đó.

---

## 3. Quy trình

1. **Đợt A — identity cả lô:** sinh `identity` cho các pháo (mục 4), mỗi pháo thử 2–3 phương án, chọn 1. Ghép
   một ảnh so sánh `tempart/Cannon<Tên>/qc/compare.png`: pháo mới đứng **cạnh 4 pháo cũ cùng tỉ lệ**, nền
   xám nhạt, phóng 3×. **Gửi chủ dự án duyệt. Chưa duyệt thì không làm bước 2.**
2. **Đợt B — đủ bộ cho pháo đã duyệt:** idle + attack + đạn + impact + phần riêng + icon, **làm trong cùng một
   phiên** cho mỗi pháo, lấy identity đã duyệt làm ảnh tham chiếu ở mọi frame.
3. Nộp: GIF xem thử `tempart/Cannon<Tên>/qc/preview.gif` (idle 2 vòng → attack 1 lần → đạn bay → impact) và
   báo chủ dự án. Chủ dự án sẽ tự đưa vào game.

---

## 4. Danh sách pháo

Thứ tự ưu tiên: **Lưới → Cối → Sét** trước, rồi tới các pháo còn lại theo thứ tự trong bảng. Cột "Cơ chế" để
bạn vẽ VFX **đúng nghĩa** (người chơi nhìn hình phải đoán được pháo làm gì), không phải để code.

### 4.1 Pháo Lưới — `CannonNet` · slug `net` · R
- **Cơ chế:** bắn lưới; quái dưới đất bị trói chân 1,5 s, quái bay bị kéo xuống thấp.
- **Dáng:** giá nỏ ngắn bằng gỗ, trên có **cuộn lưới thừng tròn** và ống phóng loe miệng. Khác Nỏ Xuyên: không
  có cánh cung lớn, không mũi lao.
- **Màu:** nâu thừng + gỗ, sắt tối; accent **vàng đất** ở các quả nặng góc lưới.
- **Đạn:** lưới cuộn tròn bay, xoè dần qua 4 frame; 4 quả nặng ở góc.
- **Impact:** lưới chụp xuống, căng ra.
- **Thêm:** `net-status/` 3 frame 128×128: lưới quấn quanh chân/thân quái (lớp phủ, cỡ ~40×40, ở giữa).

### 4.2 Pháo Cối — `CannonMortar` · slug `mortar` · N
- **Cơ chế:** bắn vòng cầu lên trời, rơi xuống nổ vùng nhỏ, bắn xa nhất.
- **Dáng:** **ống cối ngắn, miệng to, chĩa gần thẳng đứng** (60–75°), chân đế có đai sắt. Phải khác Pháo Nổ
  (nòng béo chếch ~45°): cối thấp, lùn, dựng đứng hơn.
- **Màu:** sắt xám-xanh tối + đai đồng nhỏ; accent cam ở ngòi.
- **Đạn:** quả đạn tròn có ngòi cháy (xoay qua 4 frame).
- **Impact:** nổ cam-đỏ hướng lên, có bụi đất toả hai bên.
- **Thêm:** `target-mark/` 2 frame: vòng đỏ nhấp nháy trên đất báo chỗ đạn sắp rơi (hình elip dẹt, rộng ~40 px).

### 4.3 Tháp Sét — `CannonTesla` · slug `tesla` · R
- **Cơ chế:** sét nảy qua 3 quái liên tiếp.
- **Dáng:** cột đồng có **cuộn dây quấn** và **quả cầu kim loại trên đỉnh** (không nòng).
- **Màu:** đồng nâu-cam + sắt tối; accent **vàng-trắng** của tia sét (KHÔNG dùng cyan).
- **Idle:** tia lửa điện nhỏ chạy quanh quả cầu.
- **Đạn:** thay bằng `beam/` 4 frame: tia sét **nằm ngang, dài 96 px, cao ~16 px**, dạng zigzag; vẽ sao cho
  kéo giãn ngang vẫn đẹp (hai đầu nhọn, thân đều).
- **Impact:** chớp sét toả tia nhỏ.

### 4.4 Đại Bác Công Thành — `CannonSiege` · slug `siege` · SR
- **Cơ chế:** bắn rất chậm, đạn đặc nặng, đánh mạnh vào quái to/boss.
- **Dáng:** **nòng dài, rất to, nằm gần ngang**, có bánh xe gỗ lớn hai bên trên bệ; to nhất bộ (được dùng 144×128).
- **Màu:** sắt đen-xanh + đai đồng; gỗ nâu đậm; accent đỏ ở dải trang trí nòng.
- **Attack:** 6 frame, giật lùi mạnh, khói nòng to.
- **Đạn:** quả đạn sắt đặc lớn (~20 px).
- **Impact:** vỡ đá + mảnh vụn văng, cảm giác nặng.

### 4.5 Ống Gió — `CannonGale` · slug `gale` · N
- **Cơ chế:** thổi gió đẩy lùi quái.
- **Dáng:** **ống thổi hình loa kèn** gắn với **bễ thổi da** phía sau.
- **Màu:** gỗ + da nâu, đồng; accent **trắng-xanh lục nhạt** cho gió (KHÔNG cyan đậm).
- **Đạn:** thay bằng `gust/` 4 frame: cuộn gió xoáy hình nón ngang, rộng ~48×32.
- **Impact:** vệt gió tan + vài chiếc lá/bụi.

### 4.6 Súng Phun Lửa — `CannonFlame` · slug `flame` · R
- **Cơ chế:** phun lửa hình nón tầm ngắn, đốt cháy.
- **Dáng:** **bình nhiên liệu tròn** phía sau + **vòi phun đầu rồng/đầu thú bằng sắt** phía trước.
- **Màu:** sắt tối + đồng; accent cam-đỏ.
- **Đạn:** thay bằng `flame-stream/` 6 frame loop: luồng lửa hình nón ngang, **dài ~80 px**, gốc hẹp ở miệng
  vòi, toả rộng ~40 px ở đầu.
- **Thêm:** `burn-status/` 4 frame: ngọn lửa nhỏ cháy trên thân quái (lớp phủ ~24×32).

### 4.7 Trống Chấn Địa — `CannonWarDrum` · slug `wardrum` · SR
- **Cơ chế:** không bắn; đánh trống tạo sóng chấn làm choáng quái sát tường.
- **Dáng:** **trống trận lớn** trên giá gỗ đặt trên bệ, **hai dùi gỗ cơ khí** đánh vào mặt trống.
- **Màu:** gỗ đỏ nâu + da trống be + đai đồng; accent vàng (sóng chấn).
- **Attack:** 6 frame: dùi nhấc → đập → mặt trống rung.
- **Không có đạn/impact.** Thay bằng `shockwave/` 6 frame: vòng sóng chấn **trên mặt đất** (elip dẹt, nở từ
  ~20 px tới ~120 px rộng), cùng `stun-status/` 4 frame: sao/chim xoay trên đầu quái (~24×12).

### 4.8 Máy Lăn Đá — `CannonRoller` · slug `roller` · N
- **Cơ chế:** thả tảng đá lăn dọc làn, nghiền mọi quái trên đường.
- **Dáng:** **máng gỗ nghiêng** chứa 1 tảng đá tròn, có cần gạt thả.
- **Màu:** gỗ + đá xám-nâu ấm; accent không cần (đá là nhân vật chính).
- **Đạn:** tảng đá **lăn** (xoay 4 frame), ~28 px.
- **Impact:** đá vỡ thành mảnh + bụi.

### 4.9 Pháo Mìn — `CannonMine` · slug `mine` · N
- **Cơ chế:** rải mìn xuống làn, quái giẫm lên thì nổ.
- **Dáng:** **ống phóng ngắn chĩa xuống chếch**, bên hông có giá đựng 3 quả mìn.
- **Màu:** sắt tối + gỗ; accent **đỏ** (đèn mìn).
- **Đạn:** quả mìn bay (4 frame xoay).
- **Thêm:** `mine-ground/` 2 frame: mìn **nằm trên đất** với đèn đỏ nhấp nháy (~16×10).
- **Impact:** nổ vùng cam-đỏ, bụi đất.

### 4.10 Bom Chùm — `CannonCluster` · slug `cluster` · R
- **Cơ chế:** đạn mẹ nổ tung tách thành 4 bom con.
- **Dáng:** **nòng ngắn miệng loe**, bên cạnh thùng gỗ chứa bom.
- **Màu:** sắt + gỗ; accent cam ngòi.
- **Đạn:** `bullet/` = đạn mẹ (~20 px, có vạch chia 4 múi); thêm `bomblet/` 4 frame bom con (~8–10 px).
- **Impact:** `bullet-impact/` = vỡ đạn mẹ (nhỏ); thêm `bomblet-impact/` 4 frame nổ nhỏ (~24 px).

### 4.11 Giàn Hoả Tiễn — `CannonRocket` · slug `rocket` · R
- **Cơ chế:** nạp lâu rồi phóng một loạt 6 tên lửa rơi rải trong vùng.
- **Dáng:** **giàn khung gỗ 6 ống phóng** xếp 2×3, chĩa chếch lên.
- **Màu:** gỗ + sắt; ống đỏ; accent cam lửa đuôi.
- **Attack:** 6 frame: nạp (đèn từng ống sáng dần) → phóng.
- **Đạn:** tên lửa nhỏ có vệt khói đuôi (~24×8).
- **Impact:** nổ nhỏ (~30 px).

### 4.12 Gương Phản Đạn — `CannonMirror` · slug `mirror` · R
- **Cơ chế:** chặn đạn của quái bắn xa và phản lại kẻ bắn.
- **Dáng:** **tấm gương tròn lớn** trong khung đồng, xoay trên trục, bệ đá.
- **Màu:** bạc-trắng mặt gương + khung đồng vàng; accent trắng sáng.
- **Idle:** ánh sáng loé chạy qua mặt gương.
- **Đạn:** tia sáng phản (vệt trắng-vàng ngắn ~32×6).
- **Impact:** loé sáng hình sao.
- **Thêm:** `reflect/` 4 frame: vòng sáng loé trước mặt gương lúc chặn đạn (~32×32).

### 4.13 Tháp Thánh Quang — `CannonHoly` · slug `holy` · SR
- **Cơ chế:** tia sáng từ trời, đánh mạnh lên quái ma/xương.
- **Dáng:** **tháp chuông nhỏ bằng đá trắng** có **chuông vàng** và đỉnh hình mặt trời.
- **Màu:** đá trắng ngà + vàng; accent vàng-trắng.
- **Đạn:** thay bằng `light-pillar/` 4 frame: cột sáng **rơi thẳng đứng** từ trên xuống (rộng ~16, cao ~96).
- **Impact:** vòng sáng loé trên đất + tia toả.

### 4.14 Lăng Kính — `CannonPrism` · slug `prism` · SR
- **Cơ chế:** bắn tia liên tục, càng giữ một mục tiêu càng mạnh.
- **Dáng:** **khối pha lê nhiều mặt màu hồng-trắng** lơ lửng trên khung đồng (khác tinh thể cyan của Pháo Băng:
  màu hồng-trắng ấm, hình khối lăng trụ chứ không phải gai nhọn).
- **Màu:** pha lê hồng-trắng + đồng; accent trắng.
- **Đạn:** thay bằng `beam/` gồm **3 mức** tia ngang (mỗi mức 4 frame loop, dài 96 px): mức 1 mảnh ~4 px hồng
  nhạt, mức 2 ~8 px, mức 3 ~12 px trắng chói có viền hồng. Tên thư mục `beam-1/`, `beam-2/`, `beam-3/`.
- **Impact:** đốm sáng cháy tại điểm chạm.

### 4.15 Tháp Nguyền — `CannonHex` · slug `hex` · SR
- **Cơ chế:** biến 1 quái thành ếch 3 s (đứng im).
- **Dáng:** **cột đá đen có con mắt/viên ngọc tím** ở đỉnh, vài lá bùa vải treo.
- **Màu:** đá đen-tím + vải be; accent **tím** (nguyền) + chút xanh lá độc.
- **Đạn:** quả cầu phép tím xoáy (~16 px).
- **Impact:** khói tím bùng.
- **Thêm:** `frog/` 3 frame: **con ếch xanh đứng yên** (thở/chớp mắt, **không walk**), cao ~20 px, đáy chạm y = 96
  trong khung 128×128; và `poof/` 4 frame khói biến hình (~40 px).

### 4.16 Đầu Rồng Đá — `CannonDragonHead` · slug `dragonhead` · SSR
- **Cơ chế:** phun cầu lửa; mỗi phát thứ 5 phun lửa dài càn cả làn.
- **Dáng:** **đầu rồng tạc đá** trên bệ, miệng há, mắt đỏ phát sáng, sừng cong. Pháo "trùm" — đẹp và chi tiết
  nhất bộ nhưng vẫn theo luật 4–6 mảng (dùng được 144×128).
- **Màu:** đá xám ấm + rêu nhẹ; accent đỏ-cam (mắt, lửa trong miệng).
- **Attack:** `attack/` 4 frame (phun cầu lửa) **và** `attack-strong/` 6 frame (há rộng, phun mạnh).
- **Đạn:** cầu lửa (~24 px) **và** `breath/` 6 frame loop: luồng lửa ngang **dài 128 px**, cao ~32 px.
- **Impact:** nổ lửa (~40 px). Có thể dùng chung `burn-status/` của Súng Phun Lửa.

### 4.17 Đồng Hồ Cát — `CannonChrono` · slug `chrono` · SSR
- **Cơ chế:** tua ngược quái trong vùng về phía sau.
- **Dáng:** **đồng hồ cát lớn** trong khung đồng-vàng chạm hoa văn, có bánh răng; cát chảy.
- **Màu:** đồng-vàng + gỗ đậm; cát màu hổ phách; accent **trắng-vàng** (KHÔNG cyan, KHÔNG tím).
- **Idle:** cát chảy (4 frame).
- **Attack:** 6 frame: đồng hồ lật ngược.
- **Không có đạn/impact.** Thay bằng `rewind-field/` 6 frame: **vòng rune hình mặt đồng hồ trên đất** (elip dẹt,
  rộng ~120 px), kim quay ngược; và `rewind-ghost/` 3 frame: vệt bóng mờ trắng-vàng (~32×40) để code đặt sau quái bị tua.

---

## 5. Đợt sau — CHỈ LÀM KHI CHỦ DỰ ÁN BÁO

- **Pháo đổi hình theo sao (1★ / 3★ / 5★):** mỗi pháo 3 phiên bản, bản sau to hơn một chút, thêm đai đồng/viền
  vàng/rune; mỗi phiên bản làm lại đủ bộ ở mục 2. Áp cho cả 4 pháo cũ + pháo mới đã duyệt. **Chủ dự án chưa chốt
  việc này; chờ báo rồi mới làm.**

---

## 6. Tự kiểm trước khi nộp (mỗi pháo)

- [ ] Chỉ có file mới trong `tempart/Cannon<Tên>/`; `git status` không có thay đổi nào ngoài thư mục đó.
- [ ] Mọi frame thân pháo: đáy bệ đúng y = 96, bệ không trôi giữa các frame.
- [ ] Nền trong suốt, không viền hồng/không pixel lẻ ngoài vật thể.
- [ ] Đứng cạnh 4 pháo cũ trong `qc/compare.png`: cùng cỡ, cùng nét, nhìn silhouette đen vẫn phân biệt được.
- [ ] Nòng hướng TRÁI; mũi đạn, tia, luồng lửa cũng hướng TRÁI.
- [ ] Không chữ, không bóng đổ, không UI, không người điều khiển trong ảnh.
- [ ] Nhìn chuỗi attack → đạn → impact đoán được pháo làm gì.
- [ ] Màu không dùng sai nghĩa (cyan chỉ cho làm chậm).
