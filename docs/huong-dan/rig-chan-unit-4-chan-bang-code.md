# Làm walk cho unit 4 chân bằng rig chân trong code

Bài này chỉ bạn cách tạo animation walk 6 frame cho một unit cưỡi ngựa hoặc đi 4 chân **không cần sinh ảnh**.
Bạn lấy frame idle đã duyệt, cắt 4 chân thành từng đoạn xương, rồi để script xoay các đoạn quanh khớp theo
quỹ đạo móng của ngựa đi bộ. Thân, người cưỡi và vũ khí giữ nguyên pixel của idle.

Ví dụ trong bài là kỵ sĩ tử thần: `tempart/Bonecaller/death-knight/walk/rig/`.

## Cần có trước

- Frame idle đã duyệt, PNG nền trong suốt, ví dụ `death-knight/idle/idle-1.png` (128×128, đáy y=116).
- Python có `numpy` và `Pillow`.
- Hai script trong `tools/art/rig/`: `rig_masks.py` (cắt chân) và `make_rig.py` (dựng frame). Chép cả hai vào
  `<unit>/walk/rig/`, tức nằm cạnh thư mục `idle/` của unit. Script đọc idle ở `../../idle/` và ghi frame ra ngay
  cạnh chính nó, nên phải đặt đúng chỗ đó.

## Bước 1 — Đo toạ độ khớp trên idle

1. Dựng ảnh phóng to vùng chân có kẻ lưới toạ độ (mỗi 5 px một vạch, mỗi 10 px ghi số). Ví dụ ảnh
   `idle-grid.png` phóng 12 lần vùng x 20–112, y 70–118.
2. Với mỗi chân, ghi 4 điểm theo thứ tự **gốc → gối/khoeo → cổ chân → đáy móng**.
3. Ghi thêm một **vùng bao** (đa giác) ôm trọn chân đó mà không lấn sang chân bên cạnh.

**Vì sao:** script chỉ xoay đúng khi điểm khớp nằm đúng chỗ xương gập. Vùng bao giới hạn chỗ tìm pixel chân,
để không cắt nhầm vào sườn hay áo choàng.

## Bước 2 — Khai báo chân và kiểm mask

1. Mở `rig_masks.py`, sửa `IDLE` trỏ tới frame idle của bạn.
2. Điền bảng `LEGS`, mỗi chân gồm `poly` (vùng bao) và `joints` (4 điểm ở bước 1). Tên chân:
   `far_fore` (trước xa), `near_fore` (trước gần), `far_hind` (sau xa), `near_hind` (sau gần).
   Ngựa quay trái nên "trước" là phía −x.
3. Chạy:

   ```bash
   cd <unit>/walk/rig
   python rig_masks.py
   ```

4. Mở `debug-masks-10x.png`. Bên trái là idle gốc, bên phải mỗi chân được tô một màu, khớp vẽ bằng chấm đỏ.

**Kết quả đúng:** mỗi màu phủ kín đúng một chân từ gốc tới móng, không lem sang áo choàng hay chân khác,
các chấm đỏ nằm đúng chỗ gập.

**Code làm gì bên trong:** `bone_mask` lấy pixel xương sáng (độ sáng trung bình > 95, đỏ ≥ lam), nở ra 1 px để
lấy luôn viền tối, nhưng bỏ pixel tím của áo choàng. `leg_masks` giao mask xương đó với vùng bao của từng chân.

## Bước 3 — Chỉnh thông số chuyển động

Mở `make_rig.py`, các hằng số ở đầu file:

| Hằng số | Ý nghĩa | Giá trị ví dụ |
|---|---|---|
| `FRAMES` | số frame của vòng walk | 6 |
| `DUTY` | phần chu kỳ móng chạm đất (ngựa đi bộ ~0,6) | 0,62 |
| `STRIDE` | quãng móng trượt ra sau khi chống, px ở 1x | 14 |
| `LIFT` | độ nhấc móng khi vung, từng chân | trước 5, sau 4 |
| `BOB` | biên độ nhún thân, 2 lần mỗi chu kỳ | 1 |
| `PHASE` | thời điểm chạm đất của từng chân trong chu kỳ 0..1 | sau gần 0 · trước gần 0,25 · sau xa 0,5 · trước xa 0,75 |
| `ORDER` | thứ tự vẽ lớp, từ sau ra trước | sau xa, trước xa, THÂN, sau gần, trước gần |

`PHASE` như ví dụ là đi bộ 4 phách thứ tự bên. Muốn bước dài hơn thì tăng `STRIDE`, muốn nhấc rõ hơn thì tăng `LIFT`.
Góc gập cổ chân khi vung nằm ở dòng `flex = math.radians(35) * s`.

## Bước 4 — Dựng frame

```bash
python make_rig.py
```

Script in bảng vị trí móng từng frame (`x` là toạ độ ngang, `up` là độ nhấc) và ghi ra:

- `walk-1.png` … `walk-6.png` — frame 128×128 cùng hệ toạ độ với idle.
- `animation-4x.gif` — vòng walk phóng 4 lần, 120 ms/frame.
- `compare-4x.png` — idle cạnh 6 frame.

**Kết quả đúng:** trong bảng in ra, chân nào `up 0.0` thì `x` tăng dần mỗi frame (móng lùi ra sau), chân đang
nhấc thì `x` nhảy về phía trước. Không có hai frame nào có cả 4 móng cùng vị trí.

**Code làm gì bên trong:**

1. **Lớp thân** (`body_layer`): xoá pixel chân và viền sát chân khỏi idle, xoá mọi thứ dưới y=112. Chỗ chân cũ
   nằm trước áo choàng (y ≤ 100) được vá bằng pixel áo choàng gần nhất cùng hàng, để không thủng lỗ.
2. **Chia đoạn** (`seg_index`): mỗi pixel chân được gán vào đoạn gần nhất (đùi, ống, móng). Pixel sát khớp thuộc
   cả hai đoạn, để khi xoay chỗ nối không bị hở.
3. **Quỹ đạo móng** (`hoof_path`): trong pha chống, móng trượt đều từ trước ra sau một quãng `STRIDE`. Trong pha
   vung, móng đi theo cung từ sau lên trước, cao nhất `LIFT` ở giữa pha.
4. **Tìm gối** (`solve_knee`): IK 2 đoạn từ gốc chân tới cổ chân, gập về cùng phía như ở idle. Nếu cổ chân xa quá
   tầm với, chân duỗi thẳng và hai đoạn giãn đều (dưới 1 px).
5. **Xoay đoạn** (`draw_segment`): mỗi pixel đích lấy 4×4 mẫu, chiếu ngược về idle, rồi chọn màu theo đa số. Cách này
   giữ đúng màu gốc, không sinh màu pha trộn.
6. **Dọn chân**: bỏ pixel chỉ có một hàng xóm, tô viền tối `(30,20,28)` cho xương sáng bị lộ mép.
7. **Ghép và cắt alpha**: vẽ theo `ORDER`, thân dịch xuống theo nhún, alpha cắt về 0 hoặc 255, bỏ pixel lẻ.

## Bước 5 — Soi và đưa vào bản chính

1. Mở `animation-4x.gif` xem vòng chạy. Chấm bằng chuyển động, không chấm bằng frame tĩnh.
2. Dựng ảnh zoom vùng chân 6x của cả 6 frame để soi chỗ nối và viền.
3. Khi đạt, copy `walk-1..6.png`, `animation-4x.gif`, `compare-4x.png` từ `walk/rig/` ra `walk/`. Nhớ cất bản cũ vào
   một thư mục con trước khi chép đè.
