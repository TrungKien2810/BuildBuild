# Nghiệm thu một identity sprite (bước identity-first)

Bài này dạy cách **chấm một frame identity** trước khi khoá nó làm reference cho cả 5 action.

> Trạng thái hiện tại của từng unit nằm ở `docs/project-state/HANDOFF.md`. Bài này chỉ dạy cách làm.

## Vì sao phải chấm identity riêng, không chấm chung với action

Identity là frame **duy nhất** được dùng làm reference cho mọi action sau đó. Nó sai thì cả 5 action sai
theo, mà lúc đó sửa là làm lại toàn bộ tạo hình — đắt hơn nhiều so với vẽ lại một frame.

Vì vậy những phép kiểm của action sau (vd. khe hở chân cho walk ở mục 4) cũng phải làm ngay ở bước
identity: tới lúc làm walk mới phát hiện thì identity đã khoá.

## Làm bằng tay: 5 phép đo, theo đúng thứ tự

Giả sử file đã xử lý nằm ở `tempart/<Unit>/identity/identity-1.png`.

### 1. Kích thước và aspect — đo từ alpha bbox, không đo từ canvas

Canvas luôn là 128×128; cái có nghĩa là bbox của phần đục.

**Làm tay:** mở PNG trong Aseprite/Photoshop, Magic Wand chọn vùng trong suốt → đảo vùng chọn → đọc
kích thước vùng chọn.

**Bằng lệnh:**

```bash
python -c "
from PIL import Image
im=Image.open('tempart/Cragfist/identity/identity-1.png').convert('RGBA')
b=im.split()[3].getbbox(); w,h=b[2]-b[0],b[3]-b[1]
print(f'{w}x{h} aspect={w/h:.3f}')"
```

**Aspect quan trọng hơn chiều cao** vì nó là thứ tách các unit ra ở kích thước game. Để so, aspect các
unit trong dự án:

| Unit | bbox | aspect |
|---|---|---|
| Ironblade (hiệp sĩ) | 45×55 | 0,818 |
| Frostweaver (pháp sư) | 31×41 | 0,756 |
| Hawkfletch (cung thủ) | 31×48 | 0,646 |
| Goblin (quái thường) | 31×40 | 0,775 |
| Ashfang (sói 4 chân) | 49×31 | 1,58 |
| Cragfist (tank thuần) | 57×52 | 1,096 |
| Bonecaller (pháp sư triệu hồi) | 32×48 | 0,667 |

### 2. Chiều cao

So chiều cao bbox với bảng trên theo vai trò unit. Lệch thì thu/phóng lại từ bản vẽ lớn
(xem [sua-scale-va-troi-frame.md](sua-scale-va-troi-frame.md)), đừng phóng từ bản 128 px — vỡ pixel.

### 3. Hướng nhìn — `mirror_overlap.py`

```bash
python tools/art/mirror_overlap.py tempart/Cragfist/identity/identity-1.png
```

Script làm gì: lật ngang sprite, đếm phần pixel chồng lấn giữa bản gốc và bản lật. Càng đối xứng thì
càng cao, tức càng "chính diện".

Đọc số theo thang của dự án, không theo nhãn `PASS`/`FAIL-frontal` của script:

| Unit | mirror_overlap | Nhãn script | Thực tế |
|---|---|---|---|
| Ironblade | 56,41% | PASS | đã giao |
| Frostweaver | 58,69% | PASS | golden reference |
| Hawkfletch | 58,15% | PASS | đã duyệt |
| Goblin | 67,29% | FAIL-frontal | đã duyệt và giao |
| Cragfist v1 | 80,93% | FAIL-frontal | golem đối xứng — chính diện thật |

→ **≤ 65%** là 3/4 tốt; **trên 80%** là chính diện thật.

**Tự vẽ 3/4 thì giữ đủ ba dấu hiệu** (thiếu một là đọc ra chính diện):
1. Vai gần camera thấp hơn vai xa — nghiêng trục vai.
2. Tay gần camera che một phần thân; tay xa nhỏ hơn, lùi về sau.
3. Chân gần đặt trước chân xa theo chiều sâu, không song song.

### 4. Khe hở giữa hai chân — kiểm ngay ở identity

```bash
python -c "
from PIL import Image
import itertools
im=Image.open('tempart/Cragfist/identity/identity-1.png').convert('RGBA')
a=im.split()[3]; b=a.getbbox()
for y in range(b[3]-5,b[3]):
    row=[a.getpixel((x,y))>128 for x in range(b[0],b[2])]
    runs=[(k,len(list(g))) for k,g in itertools.groupby(row)]
    print(y, 'solid=',[l for k,l in runs if k])"
```

Mỗi hàng trong 5 hàng đáy phải ra **đúng 2 đoạn liên tục**, khe hở ≥ 2 px.

**Vì sao:** `gait_sort.py` chấm walk bằng cách tách vùng bàn chân ở **chỗ lõm của profile mật độ** rồi so
độ sáng hai bên. Hai bàn chân dính thành một khối thì không có chỗ lõm để tách — walk phải chấm bằng
`golem_gait.py` (xem `nghiem-thu-locomotion-khong-co-chan.md`).

Ví dụ output khi hai chân dính (Cragfist v1):

```
y=87 solid=[8, 14]    <- còn khe hở
y=88 solid=[15]       <- dính liền từ đây xuống
```

### 5. Sạch mép

Phóng ≥ 7x soi mép sprite: không pixel bán trong suốt lẻ, không vệt màu nền cũ bám viền. Frame vẽ trên
nền chroma `#FF00FF` thì chạy `python tools/art/despill_magenta.py <file>` — script tìm pixel ám magenta ở
mép, kéo màu về màu lân cận rồi in số pixel magenta còn lại (phải = 0).

## Bước cuối: nhìn bằng mắt

Số đo chỉ nói rõ cái giá của từng lựa chọn; quyết định tạo hình là mắt người. Mở identity ở ≥ 4x cạnh
golden reference (`Assets/Art/Generated/Frostweaver-v2/idle-anchored/idle-1.png`) và hỏi: có cùng camera,
cùng độ chi tiết, silhouette đọc được ở 1x không.
