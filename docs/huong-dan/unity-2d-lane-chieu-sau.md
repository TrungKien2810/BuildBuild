# Dải đất có chiều sâu cho game 2D side-view (kiểu Grow Castle)

## Vấn đề

Trong Grow Castle, quái không đi trên **một đường thẳng** mà rải trên cả **dải đất**: con ở gần mép
dưới màn hình trông gần camera hơn và phải **vẽ đè lên** con ở phía trên. Làm sai thì có 2 kiểu lỗi:

- Mọi unit cùng một y → cả đoàn quái chồng khít lên nhau thành một hàng, trông như game 1 làn.
- Rải y nhưng không sort → con ở xa (cao hơn) vẽ đè lên con ở gần; sai chiều sâu, nhìn lộn xộn.

## Bước 1 — Đo dải đất từ art, đừng đoán

Dải đi được phải nằm **trên bãi cỏ thật trong ảnh sân**, giữa tán cây rìa trên và ngọn thông rìa dưới.
Nếu đoán thì quái sẽ đi đè lên tán cây.

**Làm bằng tay:** mở `Assets/Art/Generated/Background/bg-field.png` (1920×1080, đúng bằng canvas nên toạ
độ ảnh = toạ độ canvas) trong phần mềm vẽ. Rà chuột tìm hàng thấp nhất còn tán cây rìa trên, và hàng cao
nhất có ngọn thông rìa dưới (xem nhiều cột, vì các cây thông cao thấp khác nhau).

**Bằng script.** Cây tối hơn hẳn cỏ, nên ta đánh dấu pixel tối. Rìa trên đo bằng **tỉ lệ pixel tối trên mỗi
hàng** (tán cây liền thành một dải). Rìa dưới đo **theo từng cột**, tìm đoạn tối liền ≥ 6 px đầu tiên, vì ngọn
thông thưa và một đốm cỏ lẻ không được tính:

```python
from PIL import Image
import numpy as np
a = np.asarray(Image.open('Assets/Art/Generated/Background/bg-field.png').convert('RGB')).astype(int)
dark = a.sum(axis=2) < 200
frac = dark.mean(axis=1)
print('tan cay tren toi y', max(y for y in range(540) if frac[y] > 0.01))
tips = []
for x in range(0, 1920, 8):
    run = 0
    for i, d in enumerate(dark[540:, x]):
        run = run + 1 if d else 0
        if run >= 6: tips.append(540 + i - 5); break
print('ngon thong cao nhat y', min(tips), '| trung vi', int(np.median(tips)))
```

**Số đo thật của dự án:** tán cây đặc tới y ~224 (viền bụi sáng hơn tới ~244), ngọn thông cao nhất y
**805** (trung vị 978). Chọn dải chân: **250–800**.

Lâu đài đứng ở mép trái, chân ở y **700** để thân thành (cao 335 px) nằm giữa dải. Tường trước là một
đường thẳng đứng, nên quái ở y nào cũng chạm được tường (khoảng cách tới lâu đài chỉ đo theo x).

## Bước 2 — Lưu dải vào một chỗ duy nhất: `Battlefield`

Component `Battlefield` (object cùng tên trong scene `Battle`) giữ 2 số world: `laneTop` / `laneBottom`
(dải đi được). `WaveManager` bốc y spawn bằng `RandomLaneY()`.

Vì sao phải gom về một chỗ: nếu mỗi script tự giữ một bản số dải, sửa một bản mà quên bản kia thì quái spawn
một nơi còn hero đặt một nơi, và không có lỗi nào báo.

Đổi đơn vị: PPU 100, tâm canvas là gốc toạ độ, nên y world = (540 − y px) / 100. Dải 250–800 thành
**2,9 … −2,6**.

**Làm bằng tay:** chọn object `Battlefield` → sửa 4 số trong Inspector.

Hiện quái đi thẳng sang trái từ y lúc spawn. Khi có lính triệu hồi chặn đường (IDEAS #1), quái sẽ phải
đi cả lên xuống (`Vector3.MoveTowards` theo cả x lẫn y) tới chỗ đứng cạnh con chặn.

## Bước 3 — Sort theo trục y

Mọi thứ trên sân nằm chung một sorting layer (Default) và chia **dải số** `sortingOrder`, khai ở
`Assets/Scripts/Core/SortOrder.cs`:

| Dải | Order | Gồm |
|---|---|---|
| `Background` | −100 | `bg-field.png` |
| `Castle` | 0 | thân pháo đài |
| `GroundDecal` | 5 | ô hero, vùng băng, vòng ấn ký |
| unit | 680 … 9320 | hero, quái, lính triệu hồi (đổi mỗi frame) |
| `Air` | 9500 | đạn, vết chém |
| `Foreground` | 9600 | `fg-trees.png` |
| `Overlay` | 9700 | thanh máu / mana |

**Unit tự đặt order theo chân mỗi frame.** Trong `LateUpdate`, `Hero` và `Enemy` gọi `SortOrder.ForUnit(this)`:

```
order = 5000 − round(y_chân × 100) × 8 − (instanceID & 3) × 2
```

- `y × 100` = hàng pixel của chân: con đứng **thấp** hơn trên màn có y nhỏ hơn → order lớn hơn → vẽ đè lên.
- `× 8` chừa khoảng giữa hai hàng pixel; `(id & 3) × 2` cho hai con đứng **cùng hàng** một thứ tự cố định.
- FX gắn trên thân (vỏ đá Cragfist, dấu ấn ký) lấy `order + 1` để luôn vẽ ngay trên chủ của nó.

Vị trí dùng để sort là **pivot** của sprite. Đó là lý do pivot phải đặt ở chân (xem
`unity-import-sprite.md`): pivot ở giữa thân thì con thấp mà to vẫn có thể bị vẽ sai lớp.

**Vì sao không dùng Transparency Sort Mode = Custom Axis (0, 1, 0):** khi hai sprite cùng order đứng trùng
một hàng pixel (quái vây đánh một hero), trục sort cho kết quả hoà và Unity vẽ lúc con này lúc con kia
trên nhau → nhấp nháy. Setting đó vẫn còn trong `ProjectSettings/GraphicsSettings.asset` nhưng không còn
quyết định gì, vì mọi unit đã có order khác nhau.

**Làm bằng tay:** chọn một hero/quái trong lúc Play → Inspector `SpriteRenderer → Order in Layer` sẽ đổi
khi nó đi lên xuống. Thêm loại object mới lên sân thì chọn dải trong bảng trên, đừng gõ số tự do.

## Bước 4 — Ô đặt hero là object 2D bấm được

9 ô (lưới 3×3, x 696/816/936 px — cột đầu cách vách phải pháo đài (px 604) 92 px, y 360/525/690 px — ba hàng rải đều dải 250–800) là **SpriteRenderer + BoxCollider2D** nằm trong
thế giới, không phải UI. Vì sao: ô phải đứng đúng chỗ trên sân và vẽ trước/sau cùng hệ với hero. Nếu làm
bằng UI, phải đổi toạ độ màn hình ↔ thế giới mỗi khi camera đổi.

Muốn object 2D nhận được chạm (`IPointerClickHandler`), phải có đủ **3 thứ**. Thiếu một thứ là chạm không
có tác dụng gì, và cũng không có lỗi nào báo:
1. `EventSystem` trong scene (có sẵn cho UI).
2. **`Physics2DRaycaster` gắn trên camera.** Builder thêm ngay sau `CreateCamera`.
3. `BoxCollider2D` trên object. Thêm sau SpriteRenderer thì nó tự khớp theo kích thước sprite.

Bảng chọn hero là UI, và panel bật `raycastTarget` để chạm trên panel không xuyên xuống ô bên dưới.

**Làm bằng tay:** chọn `Main Camera` → Add Component → `Physics 2D Raycaster`. Tạo sprite ô → Add
Component `Box Collider 2D` → `Hero Slot`.

## Kiểm tra

Spawn 20 con rồi đọc y: mọi con phải nằm trong dải canvas 250–800 (world 2,9 … −2,6). Chụp màn hình để
xem con thấp có đè lên con cao hay không.

