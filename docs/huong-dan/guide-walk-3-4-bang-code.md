# Dựng guide walk đúng góc nhìn 3/4 bằng code

Bài này dạy bạn **tự dựng "guide walk"** — ảnh khung xương 8 pha để đưa cho model vẽ sprite đi bộ —
sao cho chân khớp với góc nhìn 3/4 của thân, không phải chân nhìn ngang dán vào thân 3/4.

Guide không phải art cuối: thân trên cắt từ identity đã duyệt, hai chân là que pose map + ủng thật của
nhân vật. Model sau đó vẽ lại đầy đủ quần/ủng theo guide này (xem `docs/huong-dan/nghiem-thu-walk-cycle.md`
để chấm thành phẩm).

## 1. Guide cũ sai ở đâu

`tempart/SSRChosen-20261006/build_walk_guide_v3.py` vẽ chân bằng hình chiếu nghiêng 90°:

- hai hông nằm quanh **một** tâm x (lệch tổng 1 px), không có chiều sâu;
- đế luôn đóng đinh ở `y = 113` (chân xa) và `115` (chân gần) cho mọi pha;
- bàn chân là **cùng một polygon ngang** 5×3 px cho cả hai chân — đó là bàn chân nhìn ngang;
- gối luôn chọn nghiệm x nhỏ nhất, không có gì ràng buộc theo hướng nhìn.

Ghép thân 3/4 với chân như vậy thì mắt đọc ra ngay là hai bộ phận của hai camera khác nhau.

## 2. Chạy generator mới

```powershell
python tempart/walk-guide-3q/build_walk_guide_3q.py            # mọi unit trong CONFIG
python tempart/walk-guide-3q/build_walk_guide_3q.py HauNghe    # một unit
```

Kết quả nằm ở `tempart/walk-guide-3q/<Unit>/`:

| File | Để làm gì |
|---|---|
| `guide-1..8.png` | 8 ô 128×128 (ô đưa vào `image_gen` sau khi được duyệt) |
| `posed-anchor-2x4.png` | atlas 2×4 trên nền magenta — ảnh duy nhất nạp làm Image 1 khi sinh walk |
| `legs-8x.png` | vùng chân 8×, 8 pha — chỗ soi lỗi chân |
| `legs-vs-v3-8x.png` | guide v3 (trên) so guide mới (dưới), cùng crop cùng zoom |
| `camera-board-3x.png` | Frostweaver idle (camera chuẩn) · identity · 3 pha |
| `identity-and-phases-2x.png`, `guide-preview.gif`, `moving-preview.gif` | đọc nhịp |
| `guide-meta.json` | toạ độ hông/gối/cổ chân từng pha + số đo lấy từ identity |
| `camera-checks.json` | 10 phép kiểm camera + gait, có `pass_` từng phép |
| `index.html` | trang xem gọn; `tempart/REVIEW.html` mục `walk-guide-3q-20261006` trỏ vào đây |

## 3. Thêm một nhân vật

Mở `CONFIG` trong `tempart/walk-guide-3q/build_walk_guide_3q.py` và thêm một mục:

```python
'HauNghe': dict(
    title='Hậu Nghệ',
    identity='tempart/SSRChosen-20261006/HauNghe/scale-v2/identity-1.png',  # 128×128, đã duyệt
    old_guide='tempart/SSRChosen-20261006/walk-guide-v3/HauNghe',           # chỉ để vẽ ảnh so sánh
    hip=(65, 94),                    # tâm hông: x = trục thân, y = hàng hông dưới vạt áo
    stride=7,                        # nửa sải chân, art-px
    leg_rects=((54, 104, 65, 116), (65, 106, 76, 116)),   # (x0,y0,x1,y1) vùng hai chân
),
```

Cách đo ba thứ đó trên chính identity (không lấy tâm bbox — bbox còn vũ khí):

1. **`leg_rects`**: cắt đúng hai chân, không cắt vạt áo/cung/kiếm. Đây là vùng để code **xoá chân cũ**;
   phần xoá thật còn lọc theo màu (xem mục 5) nên áo choàng xanh trong rect vẫn được giữ.
2. **`hip`**: `x` = trục dọc thân (đường giữa thân trên, không phải giữa cả ảnh), `y` = hàng mà chân
   rời khỏi vạt áo; code vẽ đùi từ đây xuống nên phần trên bị thân che.
3. **`stride`**: nửa sải chân. Bàn chân trụ lùi `stride/2` mỗi pha, chân vung đi từ `+stride` tới
   `-0,65·stride`. Đo `footspan` pha contact rồi so `≤ 1,45 × footspan` của chính identity đó.

Muốn đo nhanh trong ô 128: `python tempart/SSRChosen-20261006/color_cell.py <identity> <x0> <y0> <x1> <y1>`
in ra bản đồ màu từng pixel (`Y` vàng, `b`/`o` nâu-cam, `C` xanh, `k` viền tối) — nhìn là biết đâu là
ủng, đâu là vạt áo, đâu là cung.

## 4. Đọc `camera-checks.json`

Mười phép kiểm, phép nào cũng phải `true` trước khi trình người dùng:

| Phép kiểm | Nó chứng minh gì |
|---|---|
| `near_hip_below_far_hip`, `hips_separated_in_x_and_y` | hai hông ở hai mặt phẳng khác nhau, không chồng x |
| `far_sole_offset_is_identity_measured` | khoảng cách hai đế **bằng số đo từ ủng identity**, không phải số tự chọn |
| `depth_plane_is_constant_per_leg` | mặt phẳng chiều sâu đứng yên suốt chu kỳ (chân trước/chân sau không lệch hàng) |
| `far_leg_foreshortened`, `far_leg_composited_before_near_leg` | chân xa ngắn hơn và bị chân gần che |
| `boots_are_identity_pixels` | ủng là **đúng pixel ủng identity** (silhouette + RGB), không lật/không vẽ lại |
| `support_foot_slides_back_evenly`, `swing_foot_lifts_off_the_ground` | nhịp: chân trụ lùi đều `stride/2`, chân vung nhấc khỏi đất |
| `boot_pixels_land_on_their_plane`, `cell_ground_baseline_kept` | pixel ủng rơi đúng hàng đã tính, không có chân nào lún dưới baseline của ô |

`guide-meta.json` giữ `depth_planes` (số đo gốc), toạ độ từng khớp từng pha và lý do của từng hằng số.

## 5. Code làm gì bên trong

**Đo từ identity (`measure`).** Lọc pixel trong `leg_rects`: giữ màu "chân" (nâu/vàng/da, hoặc trắng-xám
kim loại) rồi loang thêm viền tối dính nó; **bỏ** pixel xanh/tím/đỏ (áo choàng, vũ khí). Tách 4-liên-thông
thành hai khối chân; khối **sáng hơn là chân gần** (quy ước chân gần vẽ sáng hơn), khối kia là chân xa.
Đáy mỗi khối là **hàng đế**, và khoảng cách hai hàng đế chính là `depth` — số đo mặt phẳng chiều sâu.
Đuôi ủng = 60% dưới của khối chân (phần trên là ống chân, sẽ được vẽ lại).

**Dựng hình (`build`).**

- `hip_near = (hx + side, hy)`, `hip_far = (hx - side, hy - depth)`: chân xa là mặt phẳng của chân gần
  **nâng lên đúng `depth`**, áp cho cả hông lẫn cổ chân — nhờ vậy ống chân xa song song và ngắn hơn.
- Cổ chân = `đế của chân đó - lift`; `lift` chỉ khác 0 ở pha chân vung ([0, 2, 3, 1] art-px, đủ để thấy
  hở 2–3 px dưới đế như bài nghiệm thu yêu cầu).
- Gối giải IK hai đoạn; **chân trụ** chọn nghiệm gối ra trước, **chân vung** chọn gối ra sau ở pha nén
  rồi mới ra trước ở pha qua hông/với tới.
- Ủng dán theo `paste_y = đế - (cao_ủng - 1)`, xê dịch ngón/gót 1 px bằng shear khi cần (không vẽ lại hình).
- Vẽ **chân xa trước, chân gần sau**, rồi dán thân trên (identity đã xoá chân) lên trên: đùi bị vạt áo che,
  chân gần che chân xa đúng thứ tự che khuất. Cuối cùng vẽ lại khớp bằng que 1 px (x-ray) để model thấy gối.

**Ghi ra.** 8 ô PNG, atlas magenta 2×4, ảnh soi 8×, ảnh so v3, board camera, GIF tại chỗ + GIF dịch frame,
`guide-meta.json`, `camera-checks.json`, `index.html`, và **cập nhật mục REVIEW** cùng lượt (sửa tại chỗ
nếu mục đã có, nên chạy lại generator không sinh mục trùng).

## 6. Trình duyệt và gate

Mở `tempart/walk-guide-3q/<Unit>/index.html` (hoặc mục `walk-guide-3q-20261006` trong `tempart/REVIEW.html`),
soi theo thứ tự: `legs-8x.png` (chân có gập gối, ủng đúng chiều, chân xa thấp/cao hơn chân gần đúng như
identity) → `legs-vs-v3-8x.png` (khác gì bản cũ) → `camera-board-3x.png` (cùng camera với Frostweaver
idle và identity) → GIF ở cỡ thật.

**Chỉ sinh sprite walk sau khi người dùng duyệt guide này** — đó là gate của dự án, không được gộp
("tiện thể sinh luôn"). Khi sinh, nạp đúng **một** ảnh: `posed-anchor-2x4.png`.
