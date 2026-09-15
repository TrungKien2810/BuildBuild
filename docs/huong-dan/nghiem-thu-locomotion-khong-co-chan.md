# Chấm walk cho nhân vật KHÔNG có hai bàn chân rời nhau

> Áp dụng cho golem / khối đá / slime / pháp sư áo choàng — bất cứ unit nào mà ở kích thước game bạn
> **không tách được hai bàn chân**. Nhân vật hai chân bình thường thì dùng `tools/art/gait_sort.py` — xem
> [nghiem-thu-walk-cycle.md](nghiem-thu-walk-cycle.md).

## 1. Vì sao `gait_sort.py` không dùng được ở đây

`gait_sort.py` đọc **vai trò chân** bằng độ sáng (chân gần camera được vẽ sáng hơn một bậc ramp) và
đo `footspan` ở 5 hàng pixel đáy. Cả hai phép đều cần một chỗ **lõm** trong profile mật độ để biết
"đây là ranh giới giữa hai bàn chân".

Hai bàn chân dính liền ở các hàng đáy thì không còn chỗ lõm: script vẫn chạy, vẫn in ra số, nhưng con
số đó là **nhiễu** — không có exception nào báo cho bạn biết.

**Tự kiểm trước:** mở frame ở ≥ 4x, nhìn 5 hàng pixel dưới cùng. Mỗi hàng có đúng 2 đoạn opaque tách
nhau → dùng `gait_sort.py`. Chỉ có 1 đoạn liền → dùng bài này.

## 2. Làm bằng tay — 4 thứ mắt bạn đang thật sự đọc

Mở 6 frame ở ≥ 2x, nhìn theo đúng 4 câu hỏi này. Script ở mục 3 chỉ là 4 câu hỏi này viết bằng số.

1. **Đỉnh đầu có lên xuống không? (BOB)** Bước đi là chuỗi "ngã rồi đỡ". Không nhún thì nó đọc ra như
   đang trượt trên băng chuyền. Nhìn mép trên silhouette qua 6 frame.
2. **Trọng lượng có đổi bên không? (SWAY)** Không thấy chân thì đây là tín hiệu còn lại để biết nó vừa
   đổi chân: khối tiếp đất lệch sang một bên ở nửa đầu chu kỳ, sang bên kia ở nửa sau.
3. **Khối tiếp đất có phình/thu không? (WIDTH)** Thay cho `footspan`: đo cả khối, không cần tách chân.
4. **Có frame nào lặp lại frame khác không? (IoU)** Ba câu trên vẫn có thể qua được nếu frame chỉ nhích
   vài pixel rồi copy.

Cách ghi số: mở từng frame trong editor bất kỳ (Aseprite, Photoshop, Paint.NET), đọc toạ độ y của pixel
trên cùng và mép trái/phải của 5 hàng đáy. 6 frame × 3 số = 18 số, chép ra giấy là đủ kết luận.

**Kiểm nửa sau có đổi chân không (hàng đáy):** so hàng pixel opaque dưới cùng của `walk-1` với `walk-4`
(rồi `walk-2`/`walk-5`, `walk-3`/`walk-6`). Ghi khoảng x của từng khối liền. Hai frame cùng pha phải cho
khối dài nằm ở **hai bên ngược nhau**; nếu giống nhau như đúc thì nửa sau đang lặp nửa đầu.

**Khi đạo cụ là điểm cao nhất** (gậy, cờ, giáo cao hơn đầu): mép trên bbox là đỉnh đạo cụ, không phải
đỉnh đầu. Đo đỉnh của riêng vùng thân — hàng opaque cao nhất trong các cột nằm bên kia đạo cụ.

## 3. Chạy script

```bash
python tools/art/golem_gait.py tempart/Cragfist/walk
python tools/art/golem_gait.py tempart/Cragfist/idle --prefix idle   # idle SẼ báo FAIL — xem mục 5
python tools/art/golem_gait.py <dir> --raw                           # chỉ in bảng số, không chấm
```

Script làm gì bên trong, theo từng cột của bảng in ra:

| Cột | Cách tính |
|---|---|
| `top` | hàng opaque cao nhất (mép trên alpha bbox) |
| `height`, `width` | kích thước alpha bbox |
| `contact_w` | bề rộng vùng opaque ở 5 hàng pixel đáy |
| `sway` | tâm x của 5 hàng đáy trừ tâm x toàn thân |
| `bob` | chênh lệch lớn nhất của `top` qua các frame |
| `IoU` | độ trùng alpha mask giữa từng cặp frame, lấy cặp cao nhất |

Ngưỡng (đo trên output 128×128): bob **2–9 px** · |sway| mỗi nửa **≥ 0,8 px và trái dấu** ·
contact_w range **≥ 3 px** · IoU max **≤ 0,985**.

## 4. Vì sao sway phải trừ trung bình của chính sheet

Nhân vật cầm khiên/gậy một bên thì silhouette **lệch sẵn**: tâm 5 hàng đáy lệch khỏi tâm thân ở cả 6
frame, cùng một dấu. Đo sway tuyệt đối thì nửa A và nửa B cùng âm → báo "không đổi bên" dù bước đi đúng.

Nên script lấy **trung bình sway của cả sheet**, trừ đi, rồi mới chia hai nửa. Ví dụ Ironblade walk-v3:

```
sway thô:  -0,95  -3,62  -3,36  -0,11  -0,90  +0,29    (âm cả sheet vì cầm khiên)
đã trừ trung bình -1,44:  nửa A = -1,20 px, nửa B = +1,20 px    PASS
```

Làm tay cũng vậy: đo cái **đổi** qua chu kỳ, đừng so với một giá trị tuyệt đối.

## 5. Đọc kết quả cho đúng

Số đo trên các sheet của dự án, để biết một con số là cao hay thấp:

| Sheet | bob | sway A / B | contact_w range | IoU max | Kết quả |
|---|---|---|---|---|---|
| Ironblade `walk-v3` | 2,0 px | −1,20 / +1,20 | 17 px | 0,934 | PASS 4/4 |
| Cragfist `walk` | 3,0 px | −2,75 / +2,75 | 23 px | 0,914 | PASS 4/4 |
| Frostweaver `walk-v3` (đã duyệt) | 1,0 px | +0,01 / −0,01 | 10 px | 0,989 | FAIL 3/4 |
| Ironblade `idle` | 1,0 px | +0,02 / −0,02 | 0 px | 0,997 | FAIL 4/4 |

- **Unit áo choàng lướt đi (Frostweaver, Bonecaller) gần như không có SWAY** — gate này đo bước đi có
  trọng lượng dồn ngang. Với loại unit này, quyết bằng phép kiểm hàng đáy ở mục 2 + xem GIF.
- **Idle FAIL là đúng.** Chạy trên idle chỉ để lấy bảng số: idle cần bob 1–2 px, `contact_w` giống hệt
  nhau ở mọi frame (chân không trượt), IoU ≤ 0,998. IoU = 1,000 nghĩa là các frame trùng khít — trong
  game là một tấm ảnh tĩnh.

## 6. Bước cuối: xem GIF

Mở `animation.gif` ở tốc độ thật. Gate số chỉ loại được lỗi **cấu trúc** (thiếu pha, lật hướng, lặp
frame); có mượt, có trọng lượng hay không thì chỉ mắt trả lời được.

## 7. Chân phải đứng yên trong clip

Đọc đáy từng frame (`aligned_bbox[3]` trong `pipeline-meta.json`, hoặc tự đo hàng alpha thấp nhất) — trong
một clip phải bằng nhau; lệch 2 px là nhân vật lơ lửng ở frame đó, dịch nguyên frame xuống cho khớp.

**Đáy clip này khác clip kia là bình thường.** `GeneratedArtImporter` tính pivot chân **riêng cho từng
action**, lấy trung vị `aligned_bbox[3]` của chính action đó.
