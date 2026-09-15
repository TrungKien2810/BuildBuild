# Nghiệm thu walk cycle của sprite — đo cái gì, và vì sao số đẹp vẫn có thể sai

Bài này về **cách chấm một bộ frame đi bộ** của nhân vật hai chân tách được, không phải cách sinh nó.
Unit không tách được hai bàn chân → [nghiem-thu-locomotion-khong-co-chan.md](nghiem-thu-locomotion-khong-co-chan.md).

## Một walk cycle hỏng theo hai trục độc lập nhau

| Trục | Câu hỏi | Hỏng thì mắt thấy gì |
|---|---|---|
| **Cấu trúc** | Có đủ 6 pha khác nhau không? Nửa sau có đổi chân dẫn không? | Đi khập khiễng, hoặc nửa chu kỳ lặp lại y hệt |
| **Biên độ** | Bước rộng bao nhiêu so với thế đứng? Giữa hai frame nhảy bao xa? | Tấn thủ giậm chân tại chỗ, hoặc giật cục |

Hai trục không thay được cho nhau: một sheet đúng cấu trúc hoàn toàn vẫn có thể hỏng vì bước quá rộng.
`gait_sort.py` chỉ đo trục cấu trúc; trục biên độ đo bằng `footspan` dưới đây.

## Trục cấu trúc: `gait_sort.py`

```bash
python tools/art/gait_sort.py tempart/<Unit>/walk
```

Script làm gì: với mỗi frame, lấy 5 hàng pixel đáy, tìm chỗ lõm trong profile mật độ để tách hai bàn
chân, rồi so **độ sáng** hai bên — bên sáng hơn là chân gần camera. Frame hai chân chụm (span nhỏ) được
xếp là passing. Nó in thứ tự pha đề xuất và `GATE: PASS/FAIL`.

- `PASS` + hoán vị khác `[1,2,3,4,5,6]` → pose đủ, chỉ sai thứ tự: đổi tên file frame theo thứ tự script đề xuất.
- `FAIL` với `A=0` hoặc `B=0` → nửa sau không đổi chân, phải vẽ lại nửa sau.

## Trục biên độ: footspan

`footspan` = bề rộng vùng bàn chân, đo ở **5 hàng pixel dưới cùng** của frame.

Vì sao 5 hàng đáy chứ không phải bbox cả frame: bbox cả frame bị tay/vũ khí/khiên kéo rộng ra, nên
một frame vung kiếm và một frame dạng chân cho cùng một con số. Chỉ vùng sát đất mới thuần là chân.

### Làm bằng tay

1. Mở frame trong trình xem ảnh, phóng to ≥ 4x.
2. Nhìn hàng pixel đáy, đếm từ mép trái bàn chân trái tới mép phải bàn chân phải.
3. Làm đúng thế với **một frame idle** của chính nhân vật đó.
4. Chia hai số: tỉ số đó là thứ cần xét.

### Bằng script

```python
from PIL import Image
import numpy as np
a = np.array(Image.open('walk-1.png').convert('RGBA'))[..., 3] > 128
ys = np.where(a.any(1))[0]
band = a[ys.max() - 4 : ys.max() + 1]      # 5 hàng đáy
cx = np.where(band.any(0))[0]
print('footspan =', cx.max() - cx.min() + 1)
```

### Neo là idle, không phải một con số tuyệt đối

**Idle của chính nhân vật đó là thế đứng chuẩn** (ví dụ Ironblade idle: 26 px, giống hệt cả 4 frame).
Người đi bộ bình thường mở chân rộng hơn thế đứng khoảng **1,25–1,40 lần**; rộng hơn 1,5 lần là đang tấn.

Ba ngưỡng:

- `max footspan ≤ 1,45 × footspan idle`
- `min footspan ≤ 20 px` (phải có frame chụm chân thật)
- **không frame liền kề nào nhảy quá ~16 px** — nhảy lớn trong một frame mắt đọc ra "pop", không ra luân chuyển

## Bốn thứ làm animation "có trọng lượng" mà phép đo không bắt được

Kiểm bằng mắt, từng frame ở ≥ 2x:

1. **Chân sau nhấc khỏi đất** ở frame recoil (hở 2–3 px dưới đế giày). Hai chân bết đất mọi frame → trượt.
2. **Hông nhấp nhô theo pha**: cao nhất ở passing, thấp nhất ở contact, chênh 2–3 px.
3. **Tay vung đối pha với chân.**
4. **Thân nghiêng tới trước ~5 độ**, đều cả 6 frame.

## Khi thay một sheet đã import vào Unity

Đối chiếu `aligned_bbox[3]` trong `pipeline-meta.json` cũ và mới. Bằng nhau thì giữ nguyên `.meta` cũ
(GUID và pivot không đổi). Lệch thì phải sửa pivot, không thì sprite bị chôn xuống đất hoặc bay lơ lửng —
xem `unity-import-sprite.md`.

## Quyết định cuối cùng là con mắt

Gate số là **bộ lọc loại bỏ**, không phải tiêu chí chọn: nó chặn lỗi cấu trúc mắt dễ bỏ sót. Bản cuối
chọn bằng cách xem `animation.gif` chạy ở tốc độ thật.
