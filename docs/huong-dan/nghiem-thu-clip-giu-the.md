# Chấm clip "GIỮ THẾ" — loại clip mà 90% thời lượng là đứng yên

Áp dụng cho clip chiêu dạng **buff kéo dài**: Hoá Đá của Cragfist (bất động 5s), khiên, tàng hình,
hoá đá/đóng băng của quái. Khác hẳn walk/attack ở một điểm: **cái cần chấm là lúc nó ĐỨNG YÊN**.

## 1. Vì sao cách chấm walk/attack cho kết quả ngược ở loại clip này

Cách chấm walk/attack — IoU giữa các frame phải THẤP, bbox phải đổi — đều đo **độ động**. Đem sang clip
giữ thế thì mọi thứ đảo chiều: clip càng đúng thì IoU càng cao, bbox càng đứng im, và phép đo sẽ báo
FAIL đúng cái bản tốt nhất.

Cái hỏng thật của loại clip này nằm ở nửa sau:

- Nửa sau vẫn ngọ nguậy → đọc ra "đang chuẩn bị" chứ không phải "đã bất tử". Nửa sau chiếm gần hết
  thời lượng (Hoá Đá: 3 frame giữ trên tổng 5 giây, so với 3 frame vào thế ~0,3s).
- Nửa sau không gọn hơn tư thế đứng → người chơi không đọc ra trạng thái đã đổi.
- Vòng lặp của riêng nửa sau (f6 → f4) có một bậc nhảy → thấy "giật" mỗi 3 frame.

**Nên clip giữ thế chia đôi mà chấm:** nửa đầu chấm như action thường; nửa sau chấm độ ĐỨNG YÊN.

## 2. Làm bằng tay — 4 phép đo trên Ô RAW

**Đo trên bản vẽ lớn, TRƯỚC khi thu về 128 px.** Bản thu nhỏ đã được chuẩn scale và căn chân nên giấu bớt
sai lệch tỉ lệ giữa các frame.

Các bước:
1. Mở bản vẽ lớn (sheet nhiều ô thì chia lưới đúng số hàng × cột, vd. 3×2 trên 1024×1536 → mỗi ô 512×512).
2. Với từng ô, dùng Magic Wand chọn nền, đảo vùng chọn → đọc `w`/`h` của vùng nhân vật.
3. Tính 4 tỉ số:

| Phép | Ngưỡng | Nó chứng minh điều gì |
|---|---|---|
| `w(f_cuối) / w(f1)` | ≤ 0,94 | Nhân vật **đã thu người lại** — silhouette đổi thật |
| `h(nửa sau)` chênh nhau | ≤ 2% | Nửa sau **không nhún, không thở** |
| `w(nửa sau)` chênh nhau | ≤ 2% | Nửa sau **không xoay vai, không nhúc nhích tay** |
| `h(f_cuối) / h(f1)` | 0,90–0,95 | Thu người **chứ không phải quỳ xuống** |

Phép thứ tư phân biệt "thu người" với "quỳ sụp" — hai thứ cho ra cùng con số ở ba phép đầu.

Ví dụ `tempart/Cragfist/petrify-body/raw-sheet.png`:

```
f1: w=297 h=269      w(f6)/w(f1) = 0,616   PASS
f2: w=231 h=251      h(f6)/h(f1) = 0,929   PASS
f3: w=183 h=250      h(f3..f6) spread = 0,0%  PASS
f4: w=184 h=250      w(f4..f6) spread = 0,5%  PASS
f5: w=183 h=250
f6: w=183 h=250
```

## 3. Sau xử lý: pivot phải bằng pivot của idle

`GeneratedArtImporter` tính pivot chân **riêng từng action** từ đáy `aligned_bbox` trong
`pipeline-meta.json` của chính action đó. Với walk/attack/death, đáy khác idle là bình thường — tư thế
đổi thật. Nhưng clip giữ thế thì nhân vật **đứng nguyên tại chỗ**: đáy lệch idle thì lúc game chuyển
idle → clip này hero **nhảy vị trí** ở khung hình đầu tiên, không có log nào báo.

Ví dụ bundle Cragfist:

| Action | Đáy `aligned_bbox` | Pivot y |
|---|---|---|
| idle | 93 | 0,2734375 |
| **petrify-body** | **93** | **0,2734375** ← phải bằng idle |
| walk | 96 | 0,25 |
| attack | 107 | 0,1640625 |

Cách làm cho trùng: thu nhỏ bằng **đúng hệ số của idle** và dán đáy chân vào **đúng hàng đáy của idle**. Kiểm:
so `aligned_bbox[3]` (hoặc đáy alpha) của hai action — phải bằng nhau.

## 4. Đo thứ duy nhất được phép động trong nửa sau: bằng MÀU, không bằng bbox

Nửa sau đứng yên nên phải có một thứ còn sống, nếu không clip chết cứng — vd. khe nứt sáng lên rồi tối
lại theo nhịp `f4 → f5 (sáng nhất) → f6 (về mức f4)` để vòng lặp `f6 → f4` liền mạch.

Bbox không đo được: cả 3 frame bbox y hệt. Luminance cả frame cũng không (chênh chưa tới 2/255, chìm
trong khối đá). Phải đo **luminance của riêng cụm pixel đó**, lọc theo màu:

```python
from PIL import Image
import numpy as np
p = np.array(Image.open('petrify-4.png').convert('RGBA')).astype(int)
r, g, b, a = p[..., 0], p[..., 1], p[..., 2], p[..., 3]
crack = (a > 0) & (r > 150) & (g > 130) & (b < 150) & (r > b + 30)   # vàng-kem
print('crack_lum =', p[..., :3][crack].mean())
```

Chạy cho f4, f5, f6 rồi so với nhịp mong muốn: f5 cao nhất, f6 ≈ f4.

## 5. Chỗ script không thay được mắt

Bốn phép ở mục 2 chứng minh nửa sau **đứng yên**, không chứng minh nó **đẹp khi đứng yên**. Mở từng ô và
trả lời hai câu:

1. Nửa sau có đọc ra là **một khối bịt kín** không, hay vẫn là một người đang đứng thủ thế?
2. f_cuối so với f1 có thấy **trạng thái đã đổi** không — hay chỉ thấy nhân vật co người lại?

Câu 2 quyết định clip dùng được hay không.

## 6. Liên quan

- `docs/huong-dan/nghiem-thu-identity-sprite.md` — sửa chiều cao bằng `--fit-scale`
- `docs/huong-dan/nghiem-thu-locomotion-khong-co-chan.md` — BOB/SWAY cho unit không tách được bàn chân
- `docs/huong-dan/unity-import-sprite.md` — `GeneratedArtImporter` tính pivot chân thế nào
