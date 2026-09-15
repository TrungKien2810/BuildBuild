# Đưa asset công trình (lâu đài, nền) vào đúng khung toạ độ gameplay

Bài này về loại asset **không phải nhân vật**: lâu đài, dải nền, panel — thứ mà code đọc toạ độ
bên trong nó (chân tường ở hàng nào, bệ pháo ở đâu, mép tường quái đánh vào ở x nào). Sai một
con số ở đây không làm xấu ảnh, nó làm **lệch gameplay** mà không có lỗi nào báo.

Nhân vật/animation thì xem `nghiem-thu-identity-sprite.md` và `unity-import-sprite.md`.

---

## 1. Vì sao asset công trình khó hơn sprite nhân vật

Sprite nhân vật chỉ cần một điểm neo: **chân**. Pipeline tự đo `aligned_bbox[3]` rồi tính pivot.

Asset công trình có **nhiều điểm neo cùng lúc**, và chúng ràng buộc nhau:

| Điểm neo của lâu đài BuildBuild | Giá trị | Ai đọc |
|---|---|---|
| `baseline_y` — chân tường | 348 (trong ô 384) | vị trí đặt lâu đài trên mặt đất |
| mép phải thân | x = 338 | chỗ quái dừng lại và đánh |
| mép trái thân | x = 12 | rìa màn hình |
| 3 bệ pháo (tâm x, mặt y, rộng) | `mounts.json` | chỗ đặt pháo lúc chạy game |

Vẽ trúng những số này ngay từ đầu rất khó. Dễ hơn nhiều: **vẽ tự do trên canvas rộng, rồi crop + scale
vào khung**, thay vì cố vẽ đúng 384×384 với chân tường ở hàng 348.

---

## 3. Làm bằng tay: 5 bước, đo ở mỗi bước

Giả sử có bản vẽ lớn `castle-raw.png` 1774×887.

**Bước 1 — tách nền, trên bản LỚN, trước khi scale.** Vẽ trên nền trong suốt thì bỏ qua. Vẽ trên nền
`#FF00FF` thì cắt nền rồi kéo kênh green cho viền còn ám tím:

```python
a[(r > 150) & (b > 150) & (g < 110)] = (0, 0, 0, 0)   # cắt nền
deficit = np.minimum(r, b) - g
sp = (a[...,3] > 0) & (deficit > 10)                  # viền ám tím: green tụt dưới min(r,b)
a[...,1][sp] = np.minimum(r, b)[sp]
```

Vì sao đo **thiếu green** chứ không đo độ sáng: outline navy `(21,30,68)` có `g ≥ min(r,b)`, còn pixel nhiễm
magenta thì `g` tụt dưới `min(r,b)` — kể cả pixel tối. Làm **trước** khi resize: sau resize magenta đã trộn
vào pixel lân cận.

**Bước 2 — đo bbox alpha của chủ thể.** Lâu đài này: `835×780`, tức aspect 1,071.

**Bước 3 — chọn fit theo CHIỀU RỘNG hay CHIỀU CAO. Đây là bước quyết định.**

Khung đích: rộng 327 (x 12..338), cao 336 (đáy 348, đỉnh 12). Aspect đích 0,973 — hẹp hơn raw.

- Fit theo chiều cao (336): rộng thành **360 px**, tràn quá x=338. Mép tường là chỗ quái dừng lại
  → **dời mép tường = dời điểm va chạm của cả game**.
- Fit theo chiều rộng (327): cao thành **305 px**, thấp hơn khung 31 px. Chỉ là lâu đài lùn hơn.

Chọn cái thứ hai. **Nguyên tắc: chiều nào có code đọc thì chiều đó là ràng buộc cứng; chiều còn
lại được phép hụt.**

**Bước 4 — resize. Dùng `NEAREST`, không dùng `LANCZOS`.** `LANCZOS` nội suy màu nền (RGB vẫn còn dưới
alpha 0) vào pixel viền → viền mềm, ám màu nền; dự án muốn viền pixel cứng.

**Bước 5 — dán vào ô theo ĐÁY, không theo tâm.**

```python
out.alpha_composite(sub, (X0, BASELINE + 1 - h))   # +1: đáy là hàng 348, không phải 347
```

Vì sao `+1`: dán bằng `BASELINE - h` thì hàng đáy của sprite rơi vào 347, lâu đài lơ lửng trên mặt đất
1 pixel. Kiểm bằng cách in `ys.max()` của ảnh đã dán và so **bằng đúng** `baseline_y`.

---

## 4. Bước ai cũng quên: đo lại các điểm neo BÊN TRONG asset

Crop đúng khung không có nghĩa là bệ pháo nằm đúng chỗ. Phải đo lại.

Cách đo mặt bệ (mặt phẳng ngang nhìn từ camera 3/4 thì **sáng hơn** mặt tường đứng):

1. Lấy ngưỡng `th = percentile(lum[opaque], 80)`.
2. Với mỗi hàng y, tìm **đoạn liên tục dài nhất** có `lum > th`; giữ hàng nào đoạn đó ≥ 60 px.
3. Gom các hàng liền nhau (cách ≤ 2) thành một cụm = một bệ. Hàng trên cùng của cụm = mặt bệ.
4. Tâm x và bề rộng lấy ở hàng rộng nhất trong cụm.

Script: `tools/art/castle/measure_mounts.py`.

Ví dụ castle v2 so với `mounts.json` đang khai:

| tầng | mặt bệ y (đo / khai) | tâm x (đo / khai) | rộng (đo / khai) |
|---|---|---|---|
| 1 | **245** / 280 | 196 / 197 | 118 / 115 |
| 2 | **188** / 205 | 158 / 145 | 128 / 120 |
| 3 | 134 / 135 | 201 / 190 | 109 / 120 |

Rộng và x khớp trong ±13 px — bố cục đúng. **y lệch vì lâu đài thấp hơn 31 px**, và lệch
dồn từ trên xuống: tầng 3 khớp, tầng 2 −17, tầng 1 −35.

**Sửa `mounts.json` theo art, đừng sửa art theo `mounts.json`.** Lý do: vẽ lại để bệ rơi đúng hàng 280
tốn công hơn nhiều so với sửa một con số. Điều kiện để
làm vậy an toàn: **grep xem đã có code nào đọc file toạ độ chưa** — `grep -rn "mounts" Assets/Scripts`.
0 hit → sửa file toạ độ thoải mái. Đã có code đọc → phải sửa cả hai đầu cùng lúc.

**Đừng đo "khoảng trống phía trên bệ".** Ở bố cục bậc thang, ngay phía trên mặt bệ là **mặt tường của
tầng sau** — nền, không phải mái che — nên phép đo ra 0 cho mọi tầng và con số đó vô nghĩa: pháo vẽ đè
lên tường sau là đúng phối cảnh. Thứ cần kiểm là câu hỏi có/không "có mái/cờ che bệ không", bằng mắt.

---

## 4b. Đo PITCH của thế giới trước khi vẽ bất kỳ công trình nào

"Camera 3/4 elevated-front" là chữ, không phải số. Vẽ cần **một góc cụ thể**, và góc đó đọc được từ
chính cái nền đã duyệt.

**Cách đo — dùng vật tròn nằm ngang trên mặt đất.** Một hình tròn nằm trên mặt phẳng, nhìn từ
camera nghiêng xuống góc θ, chiếu thành ellipse có `h/w = sin(θ)`. `bg-ground` có sẵn đá cuội:

```python
stone = (b > 85) & (r - b < 80) & (r > 120)      # đá xám-ấm trên nền đất cam
# gom blob 4-liên thông, giữ blob 10..70 px rộng, 3..30 px cao, >=30 px
```

Đo thật trên `bg-ground` đã giao: **20 hòn, `h/w` trung vị 0,351 → pitch ≈ 21°**. Và quan trọng
không kém: tỉ lệ này **không đổi** từ hàng y=6 tới y=225, tức mặt đất là **phép chiếu trực giao
nghiêng đều**, không phải phối cảnh có điểm tụ. Ellipse cũng **đối xứng, không xiên** → thế giới
**không có yaw**, không có hướng lùi trái/phải.

Ba con số đó là ràng buộc cho **mặt đất**. **Không bê nguyên pitch 21° sang vật đứng trên mặt đất** —
xem mục 4c.

| | giá trị | hệ quả cho công trình |
|---|---|---|
| pitch | **21°** | mặt phẳng ngang sâu `D` px hiện lên cao `0,351 × D` px |
| yaw | **0°** | chỉ thấy MỘT mặt đứng; cạnh ngang phải nằm ngang thật |
| phép chiếu | trực giao | mặt trên ở chân và ở đỉnh công trình **nén bằng nhau** |

**Lỗi cần soi: hai camera trong một asset.** Mảng lớn (cổng, mặt tường, mép bệ) vẽ chính diện yaw 0°
nhưng khối nhỏ (răng cưa, mũ tường, thân tháp) lại vẽ kiểu isometric lộ **hai mặt đứng**. Nhìn tổng thể
thấy "sai sai" mà không chỉ ra được chỗ nào. Khi vẽ, giữ bằng số: *chính diện, yaw = 0°, chỉ
thấy MỘT mặt đứng của mọi khối kể cả răng cưa, mọi cạnh ngang nằm ngang.*

**Cách nghiệm thu:** đo độ dốc mép ngang (phải ≈ 0°), rồi **soi khối nhỏ ở 5–7×** xem có lộ hai mặt
đứng không. Đừng đo tỉ lệ mặt trên của răng cưa bằng toạ độ tự đoán — khối nhỏ vài chục pixel thì
phép đo dễ bắt trúng mặt tường chứ không phải mặt trên (các khối cùng hàng có thể ra 4° / 68° / 90°).
Chỗ này nhìn bằng mắt ở 5–7× đáng tin hơn số.

---

## 4c. Pitch đo từ MẶT ĐẤT không áp cho VẬT ĐỨNG TRÊN mặt đất

**Vì sao:** trong một game side-scroller 2D, mặt đất và vật thể **không dùng cùng một pitch**, và đó
không phải lỗi của nền — đó là quy ước của cả thể loại. Đo thật trong chính màn này:

| Vật trong cùng một màn | Pitch đang được vẽ |
|---|---|
| Mặt đất (`bg-ground`) — đá cuội `h/w` 0,351 | **≈ 21°**, nhìn chếch từ trên |
| Cây, núi, bụi (`bg-mid`, `bg-far`) | **0°** — không thấy tán cây từ trên |
| Hero, quái, prop đá | **0°** — không một mặt trên nào |
| Lâu đài vẽ theo pitch mặt đất | **≈ 21°** ← lệch khỏi mọi thứ khác trong màn |

Chỉ **MẶT SÀN** được vẽ chếch từ trên, để người chơi đọc được chiều sâu của lane. **Mọi vật đứng
trên nó vẽ mặt đứng thuần.** Lâu đài đi theo camera của mặt đất nên nó đứng cạnh hero mà như hai
bức tranh khác nhau: hero vẽ ngang tầm mắt, lâu đài vẽ như mô hình trên bàn nhìn xuống.

Và có một lý do hình học nữa, độc lập với quy ước: lâu đài cao **384 px**, gấp ~3,5 lần hero. Camera
đặt ngang hero thì phần trên của lâu đài nằm **cao hơn mắt camera** — chỗ đó phải thấy **mặt dưới**
của lan can và gờ mái, không thể thấy mặt sàn.

**Cách đo lỗi này bằng số** — tìm các dải sáng nằm ngang dày và rộng, chúng chính là mặt sàn:

```python
lum = rgb.sum(2) / 3
thr = np.percentile(lum[alpha], 92)          # 8% sáng nhất = mặt được chiếu trực diện
bright = alpha & (lum >= thr)
# với mỗi cột, đo độ dày các đoạn liên tục; rồi đếm số pixel sáng theo từng HÀNG
```

Asset đúng pitch thì không có hàng nào vượt vài chục px sáng liên tục theo chiều ngang; có hàng 150–200 px
sáng liên tục, dày 10–16 px là mặt sàn phơi lên trời.

**Phép thử không cần code — lật ảnh dọc.** Lật ngược asset. Nếu vẫn đọc ra ngay đâu là "trên" nhờ
các mảng sáng nằm ngang nằm ở mép trên mỗi khối → còn mặt sàn → sai pitch.

**Vậy bệ pháo đọc ra bằng gì khi không có mặt sàn:** mỗi bệ là một **gờ**, đọc bằng ba dấu hiệu
**giá trị** chứ không bằng diện tích mặt sàn:

1. mặt TRƯỚC của lan can — một hình chữ nhật đứng,
2. **một đường mặt dưới tối 2–3 px ngay dưới môi gờ** — dấu hiệu chính nói "gờ này nhô ra và ta
   đang nhìn nó từ dưới lên",
3. một **bậc giá trị** giữa mảng tường phía sau (tối hơn 2 bậc ramp) và mảng tường phía trước.

**Tóm lại:** đo pitch từ nền là đúng, nhưng phải đo **hai lần** — một lần trên mặt đất,
một lần trên **vật thể đã duyệt đứng trên mặt đất đó** (cây, hero). Hai số đó khác nhau, và công
trình phải khớp số thứ **hai**.

---

## 6. Nghiệm thu cuối: dựng cảnh thật, đừng chấm asset một mình

Asset công trình chỉ sai lộ ra khi **đứng cạnh nền và nhân vật**. Dựng đúng thứ tự layer của
`layout.json`, đặt asset vào đúng toạ độ world, và **thả 2–3 unit vào cho có tỉ lệ**:

```python
comp.alpha_composite(castle, (0, 1000 - 348))    # baseline 348 -> chân lâu đài ở world y=1000
```

Script: `tools/art/castle/qc_composite.py` — nó dựng **hai** ảnh, bản mới và bản cũ, cùng bố cục.
Xem cạnh nhau mới thấy được thứ mà số không nói (tông màu, camera). Không có ảnh so sánh thì rất dễ
duyệt một bản "cũng ổn mà".

Ba câu hỏi bằng mắt, hỏi trên ảnh ghép chứ không hỏi trên file PNG rời:

1. **Cùng camera với vật đứng trên sân (hero, cây) chưa?** Chỉ thấy mặt đứng, không có mặt sàn phơi lên
   trời, không khối nào lộ hai mặt đứng (mục 4b, 4c).
2. **Tỉ lệ có đọc ra không?** Cổng phải cao ít nhất bằng một humanoid, không thì lâu đài thành đồ chơi.
3. **Hero đứng trước nó có nổi không?** Công trình là nền: bão hoà và tương phản phải **thấp hơn**
   nhân vật.
