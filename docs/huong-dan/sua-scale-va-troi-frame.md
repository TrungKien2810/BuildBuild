# Sửa lệch scale và trôi vị trí giữa các frame animation

Hai lỗi này soi từng frame tĩnh thì không thấy, chạy animation mới thấy nhân vật phồng xẹp hoặc trôi ngang.
Bài này dạy cách đo ra chúng và sửa thẳng trên frame.

## Đo trước khi sửa

Dùng thước **không đổi theo dáng**, đừng dùng bbox:

| Lỗi | Thước đo | Vì sao |
|---|---|---|
| Lệch scale | Diện tích pixel đặc (alpha nhị phân) từng frame | Diện tích thân ít đổi theo dáng hơn nhiều so với bbox |
| Trôi ngang (nhân vật) | Trọng tâm x của vùng thân chính (lọc theo màu thân, vd. áo tối `lum < 110` ở vài hàng đáy, hoặc tunic sáng `R+G+B > 450`) | Thân luôn có mặt, vũ khí/đạo cụ không kéo lệch |
| Trôi (projectile) | Độ dịch làm thân vật thể chồng khít frame 1 | Vệt gió chỉ nằm một phía, thân thì luôn có |

**Làm tay:** mở từng `*-N.png` trong Aseprite/Photoshop, Magic Wand chọn nền rồi đảo vùng chọn, đọc số
pixel ở thanh Info. Chênh > ~15% giữa các frame cùng dáng là lỗi.

**Nhị phân hoá alpha (`>= 128`) trước khi đếm.** Nếu ảnh có viền bán trong suốt (vd. vừa resize bằng
LANCZOS), đếm `alpha > 0` sẽ tính cả viền là pixel đặc và ra số to hơn thật ~25%.

**So với idle bằng frame có dáng giống idle nhất** (frame "sẵn sàng" / "về thế thủ"), không bằng frame
giơ vũ khí hay nằm ngã — những frame đó thấp/cao/rộng khác idle là đúng tư thế.

## Sửa lệch scale: thu nhỏ lại từng frame từ bản vẽ lớn

1. Lấy bản vẽ độ phân giải lớn của từng frame (bản vẽ trước khi thu về 128 px), trim theo alpha.
2. Thu nhỏ ô đó bằng **LANCZOS** (giữ cùng bộ lọc với lúc thu nhỏ frame gốc), nhị phân alpha, đếm diện tích.
3. Dò hệ số từng frame cho tới khi diện tích bằng **đích chung**.
4. Dán vào ô 128: giữ tâm x của `aligned_bbox` cũ, đáy sprite = `aligned_bbox[3]` cũ (chân không nhảy).
5. Nguồn vẽ trên nền màu chroma thì chạy `tools/art/despill_magenta.py` trên từng file; nền trong suốt sẵn thì bỏ bước này.
6. Sửa `aligned_bbox`/`output_size` trong `pipeline-meta.json` đi kèm — **importer đọc `aligned_bbox[3]`
   để đặt pivot chân**; để số cũ thì pivot sai mà không có lỗi nào báo.

Thu nhỏ từ bản lớn (~4x) nên không vỡ pixel. Phóng to từ output 128 px thì sẽ vỡ.

### Chọn đích chung

Đừng lấy thẳng diện tích idle làm đích: idle đứng dạng chân, nhiều phần mảnh, nên cùng diện tích thì dáng
gọn/dày trông **to hơn** idle. Tách hai việc:

- **Đều giữa các frame:** cùng một diện tích đích.
- **Mức chung:** neo frame có dáng gần idle nhất cho **bbox** bằng idle. Ví dụ Ashfang attack: f6 rộng 53 px,
  idle 48 → nhân cả bộ ×0,906 → đích = 986 × 0,906² ≈ 809 px.

## Sửa trôi ngang

**Làm tay (Aseprite/Photoshop):**
1. Mở các frame thành layer chồng nhau, bật onion skin (hoặc giảm opacity layer trên xuống 50%).
2. Lấy một mốc trên **thân** của frame 1 (mép áo dưới cùng, tâm thân — không lấy vũ khí/đạo cụ).
3. Move tool dịch **nguyên frame** sang ngang cho mốc trùng frame 1. Không dịch dọc — đáy chân đã đúng.
4. Kiểm không phần nào bị đẩy ra ngoài khung 128 px.
5. Ghép lại `sheet-transparent.png` từ các frame đã dịch, không thì sheet và frame lệch nhau.

**Bằng script:** tính `dx = trọng_tâm_f1 − trọng_tâm_fN` theo thước ở bảng trên, dán frame vào canvas trống
lệch `dx`, kiểm bbox mới không vượt `0..128`.

```python
from PIL import Image
import numpy as np
def body_cx(im):
    p = np.array(im).astype(int); a = p[..., 3] > 0
    y1 = np.nonzero(a)[0].max(); dark = a & (p[..., :3].mean(2) < 110)
    return np.nonzero(dark[y1-5:y1+1])[1].mean()
ims = [Image.open(f'attack-{i}.png').convert('RGBA') for i in range(1, 7)]
ref = body_cx(ims[0])
for i, im in enumerate(ims, 1):
    dx = round(ref - body_cx(im)); out = Image.new('RGBA', im.size); out.paste(im, (dx, 0))
    print(i, 'dx', dx, 'bbox', out.getbbox()); out.save(f'attack-{i}.png')
```

Dịch nguyên frame khác với cắt ghép từng vùng (chân, tay) giữa các frame — dịch nguyên frame không để lộ
mép cắt. Đáy `aligned_bbox[3]` không đổi nên pivot trong Unity vẫn đúng.

Ghi kết quả ra thư mục mới hoặc giữ bản trước khi sửa (`pre-xalign/`, `*-scalefix/`), không đè mất bản gốc.

## Đóng GIF để xem: composite lên nền đặc

GIF chỉ có **một** chỉ số trong suốt, nên pixel mép bán trong suốt phải được trộn lên một nền đặc trước khi lưu:

```python
# ĐÚNG: composite lên nền đặc rồi mới sang RGB
bgc = (44, 44, 52)
big = [Image.alpha_composite(Image.new('RGBA', f.size, bgc + (255,)), f)
       .convert('RGB').resize((f.width * S, f.height * S), Image.NEAREST) for f in fs]
big[0].save(gif, save_all=True, append_images=big[1:], duration=110, loop=0)
```

