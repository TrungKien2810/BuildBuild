# Hướng dẫn: đưa sprite pixel-art vào Unity (và tự làm bằng tay)

> Doc **học nghề**, không phải doc trạng thái. Mục đích: bạn hiểu được từng bước mình đang làm gì,
> tự làm bằng tay được, và biết vì sao script trong `Assets/Editor/GeneratedArtImporter.cs` lại
> viết như vậy. Quy ước rút gọn đã chốt nằm ở `docs/project-state/art-pipeline.md` mục "Unity import".

---

## 0. Bức tranh tổng thể

Pipeline art của dự án chạy như sau:

```
tự vẽ frame (Aseprite/Photoshop), nền trong suốt, mỗi frame một PNG 128×128
              ↓  (tuỳ chọn) script căn chân, ghi đáy từng frame vào pipeline-meta.json
           idle-1.png … idle-4.png  +  pipeline-meta.json
              ↓  ← CHÚNG TA ĐANG Ở ĐÂY
           Unity: import setting + pivot  →  Sprite  →  Animation  →  Prefab
```

Việc "import" nghe như chỉ là kéo file vào, nhưng thực chất là **khai báo cho Unity biết đọc file
PNG đó như thế nào**. Đọc sai thì không có lỗi nào hiện ra — sprite vẫn hiện, chỉ là mờ, sai kích
thước, hoặc chân lún xuống đất. Đó là lý do phần này cần cẩn thận.

---

## 1. Import setting: 7 ô cần chỉnh và vì sao

Chọn một file PNG trong `Assets/`, nhìn cửa sổ **Inspector** bên phải. Đó là `TextureImporter`.

| Ô trong Inspector | Đặt thành | Vì sao |
|---|---|---|
| **Texture Type** | `Sprite (2D and UI)` | Mặc định là `Default` (dùng cho texture 3D). Không đổi thì không tạo ra Sprite để vẽ 2D. |
| **Sprite Mode** | `Single` | Mỗi PNG của ta là **một** frame rời. `Multiple` là khi một file chứa nhiều frame và phải cắt (xem mục 5). |
| **Filter Mode** | `Point (no filter)` | **Quan trọng nhất với pixel art.** Mặc định `Bilinear` sẽ *nội suy* màu giữa các pixel → ảnh nhoè như bị bôi. Point = lấy đúng pixel gần nhất, giữ cạnh sắc. |
| **Compression** | `None` | Nén DXT/ETC làm nhiễu màu quanh cạnh. Sprite của ta nhỏ (128×128) nên không cần tiết kiệm. |
| **Generate Mip Maps** | tắt | Mip map là các bản thu nhỏ dùng cho vật thể ở xa trong 3D. Game 2D camera cố định không dùng, bật lên chỉ làm sprite mờ khi scale. |
| **Alpha Is Transparency** | bật | Nói cho Unity biết kênh alpha là độ trong suốt → nó xử lý viền đúng, không để viền tối. |
| **Pixels Per Unit (PPU)** | `100` cho toàn game | Bao nhiêu pixel ảnh = 1 đơn vị thế giới. **Phải giống nhau cho mọi asset**, nếu không thì castle và quái sẽ sai tỉ lệ với nhau. 100 px = 1 unit nghĩa là con sói 47 px rộng ≈ 0,47 unit. |

Chỉnh xong bấm **Apply** (Unity không tự lưu).

> **Mẹo làm hàng loạt bằng tay:** chọn nhiều file cùng lúc (Ctrl/Shift + click) rồi chỉnh một lần —
> Inspector sửa cho cả nhóm. Đây là cách nhanh nhất khi chưa có script.

---

## 2. Pivot — chỗ dễ sai nhất, và là lý do có script

### Pivot là gì

Pivot là **điểm neo** của sprite: khi bạn đặt object ở toạ độ `(x, y)`, Unity đặt *pivot* của sprite
vào đúng điểm đó, phần còn lại của ảnh vẽ quanh nó.

Mặc định pivot = `Center` (giữa ảnh). Với nhân vật đứng trên mặt đất, điều đó **sai**: bạn muốn đặt
"chân nó ở đường đất y=820", chứ không phải "giữa bụng nó ở y=820".

→ Với nhân vật, pivot phải nằm ở **giữa hai chân, ngay đường đất**. Gọi là *foot pivot*.

### Hệ toạ độ — nguồn của mọi nhầm lẫn

Có **hai** hệ toạ độ ngược nhau và bạn phải đổi qua lại:

| | Gốc | Trục y |
|---|---|---|
| Toạ độ **ảnh** (PNG, pipeline-meta) | góc **trên-trái** | y tăng khi đi **xuống** |
| Pivot của **Unity** (chuẩn hoá 0→1) | góc **dưới-trái** | y tăng khi đi **lên** |

Công thức đổi:

```
pivotX = x_ảnh / chiều_rộng_ảnh
pivotY = (chiều_cao_ảnh − y_ảnh) / chiều_cao_ảnh
```

**Ví dụ thật — Ashfang idle.** File 128×128, `pipeline-meta.json` cho biết đáy nhân vật
(`aligned_bbox[3]`) = 90 ở cả 4 frame. Vậy:

```
pivotX = 64 / 128          = 0.5
pivotY = (128 − 90) / 128  = 0.296875
```

Nhập vào Inspector: **Pivot → Custom**, gõ `X = 0.5`, `Y = 0.296875`.

### Mỗi action một pivot riêng — không dùng chung cho cả nhân vật

Mỗi unit có file `character-scale-profile.json` với `output_origin`, nhìn rất giống "pivot chung
của nhân vật này". **Dùng nó cho mọi action là sai.**

Ví dụ bộ Ironblade, so đáy alpha thật của từng action với `output_origin.y = 92`:

| Action | Đáy alpha | Lệch |
|---|---|---|
| idle, walk, identity | 92 | 0 |
| attack | 107–109 | **+15 đến +17** |
| hurt | 106–107 | **+14 đến +15** |
| death | 105–106 | **+13 đến +14** |

Nếu lấy pivot từ profile cho cả bộ, thì khi Ironblade đánh/trúng đòn/chết, chân nó **thụt xuống dưới
mặt đất 15 px** — nhân vật như đang lún xuống bùn. Không có lỗi nào báo, `dotnet build` vẫn xanh.

Vì sao lệch: các action đổi tư thế lớn được căn bằng **anchor riêng** (attack/hurt giữ
anchor chân, death căn theo đáy của chính action), nên baseline của chúng khác baseline của idle.
Có unit mọi action trùng đáy (Goblin: đều 85) nên nhìn một unit thì không thấy — luôn đọc từng action.

→ **Quy tắc: đọc đáy thật của CHÍNH action đó** (`aligned_bbox[3]` trong `pipeline-meta.json` cùng
thư mục), lấy trung vị của các frame. Đừng copy hằng số từ asset khác dù cùng kích thước ô.
Tự vẽ, không có `pipeline-meta.json`: Magic Wand vùng trong suốt → đảo vùng chọn → đọc hàng đáy của từng frame.

→ **Ngoại lệ FX:** file nào có `"align": "center"` trong meta (frost-bolt, impact, cone-sweep-fx) thì
**không có chân** — giữ pivot mặc định Center. Đặt foot pivot cho một vụ nổ là vô nghĩa.

### Tự kiểm bằng mắt

Cách nhanh nhất để biết pivot đúng: kéo sprite vào Scene, đặt `Position Y` đúng bằng đường đất,
rồi bật **Gizmos**. Chân nhân vật phải chạm đúng đường đó — không lơ lửng, không lún.

---

## 3. `maxTextureSize` — ảnh lớn bị thu nhỏ im lặng

`Max Size` trong Inspector mặc định là **2048**. Ảnh gốc rộng hơn con số đó sẽ bị Unity **thu nhỏ
im lặng** khi import: file trên đĩa vẫn 4096 px, nhưng texture trong game chỉ còn 2048.

Hậu quả: mọi phép cắt frame tính sai, frame cuối biến mất. Không có warning nào.

Với raw sheet 1254×1254 của ta thì 2048 vẫn đủ, nhưng `bg-sky` 1920×1080 và các sheet lớn hơn thì
phải để ý. Quy tắc: **`Max Size` ≥ cạnh dài nhất của ảnh gốc**.

Và khi viết script, phải đọc kích thước gốc bằng:

```csharp
importer.GetSourceTextureWidthAndHeight(out int w, out int h);   // kích thước FILE
```

**không phải** `tex.width` — cái đó là kích thước **sau khi** đã bị thu nhỏ, nên nó sẽ luôn "khớp"
và che mất chính lỗi bạn đang tìm.

---

## 4. Từ setting bằng tay sang script tự động

Làm bằng tay cho 4 file thì được. Bộ art hiện tại có hơn 100 file và sẽ còn tăng — nên có
`Assets/Editor/GeneratedArtImporter.cs`.

### `AssetPostprocessor` hoạt động thế nào

```csharp
public class GeneratedArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture() { ... }
}
```

Unity tự gọi `OnPreprocessTexture()` **mỗi lần** import một texture, **trước khi** nó đọc file.
Trong đó, `assetPath` là đường dẫn file đang import, và `assetImporter` là cái `TextureImporter`
tương ứng với các ô Inspector ở mục 1.

Nghĩa là: thay vì bạn bấm 7 ô rồi Apply, script đặt 7 thuộc tính đó bằng code — và nó chạy lại
**mỗi lần** file được import, kể cả file mới thêm vào sau này.

**Điểm cần nhớ:** nếu đã có postprocessor chạy `OnPreprocessTexture`, thì `maxTextureSize` **phải**
được đặt ngay trong đó. Đặt ở menu item hay chỗ khác sẽ bị postprocessor ghi đè ở lần import kế tiếp.

### Vì sao script này chỉ đụng `Assets/Art/Generated/`

```csharp
if (!assetPath.StartsWith("Assets/Art/Generated/")) return;
```

Postprocessor áp lên **mọi** texture của project nếu không chặn. Ta không muốn ép Point filter lên
icon UI của Unity hay texture của package khác.

### Đặt file ở đâu

Bất cứ thư mục nào tên `Editor` đều là code **chỉ chạy trong Editor**, không được build vào game.
Bắt buộc, vì `UnityEditor` namespace không tồn tại lúc runtime — để sai chỗ là build fail.

Dự án có thêm **assembly definition** (`.asmdef`): `Assets/Scripts/BuildBuild.asmdef` cho code game và
`Assets/Editor/BuildBuild.Editor.asmdef` (Platforms chỉ tick `Editor`, References có `BuildBuild`).
Làm tay: chuột phải thư mục → *Create → Scripting → Assembly Definition*, đặt tên, tick platform.
Vì sao: sửa một script chỉ biên dịch lại assembly chứa nó, và code game `using UnityEditor` sẽ báo lỗi
ngay trong Editor thay vì chỉ fail lúc build. Lưu ý: script nằm ngoài mọi thư mục có asmdef thì rơi về
`Assembly-CSharp`, và asmdef KHÔNG thấy được code đó, nên đừng để script mới ra ngoài `Assets/Scripts/`.

### Trong thư mục art phải còn những file nào

Importer đọc `pipeline-meta.json` **cùng thư mục** với frame để lấy pivot (mục pivot ở trên), và
`character-scale-profile.json` ở thư mục unit làm dự phòng. Chuyển hai file này ra khỏi `Assets/` thì
không có lỗi nào hiện ra; importer lặng lẽ quay về pivot mặc định và Ironblade lún 13–17 px khi đánh/trúng đòn/chết.
Ngược lại, `raw-sheet*.png`, `animation.gif`, `prompt-used.txt` thì Unity không dùng mà lại rất nặng,
nên để ở `tempart/raw/<Unit>/<action>/`, không để trong `Assets/`.
Khi chuyển file bằng Explorer, luôn chuyển **kèm file `.meta`**, vì `.meta` giữ GUID mà scene/prefab dùng để tham chiếu.

### Hai menu item kèm theo

- `BuildBuild/Art/Reimport Generated Art` — ép import lại toàn bộ. Cần chạy **một lần** sau khi thêm
  script, vì các file đã import từ trước sẽ không tự chạy lại postprocessor.
- `BuildBuild/Art/Verify Generated Art Import` — in ra bảng: kích thước gốc vs kích thước sau import,
  maxSize, filter mode, PPU, pivot thật.

Menu thứ hai quan trọng hơn vẻ ngoài của nó: **nó đọc lại thứ Unity THẬT SỰ hiểu, không đọc file
`.meta`**. File `.meta` sửa tay (hoặc bằng script) có thể ghi một đằng mà Unity đọc ra một nẻo, nên
**nghiệm thu bằng cái Unity đọc ra, không bằng cái vừa ghi vào file.**

---

## 5. Khi nào cần Sprite Mode = Multiple (slice)

Ta đang dùng `Single` vì pipeline đã xuất **từng frame thành file riêng**. Nhưng nếu gặp một atlas
(nhiều frame trong một ảnh), quy trình là:

1. `Sprite Mode` → `Multiple`
2. Mở **Sprite Editor** (nút trong Inspector)
3. `Slice` → `Grid By Cell Size`, nhập đúng kích thước ô (ví dụ 128×128)
4. Đặt pivot cho từng ô — vẫn theo công thức mục 2
5. Apply

Lưu ý: **xoá sạch atlas cũ trong thư mục đích trước khi copy atlas mới vào** nếu đổi tên file hoặc
số frame. Để cả file cũ lẫn mới song song sẽ khiến Unity vẫn load nhầm bản cũ.

---

## 6. Bước tiếp theo sau khi import xong

1. **Animation Clip** — chọn nhiều sprite frame, kéo thẳng vào Scene: Unity tự hỏi lưu `.anim` và
   tạo luôn `Animator Controller`. Nhớ chỉnh **Samples** (FPS): pipeline xuất GIF ở 110ms/frame
   ≈ **9 FPS**, đặt Samples = 9 cho khớp tốc độ đã duyệt bằng mắt.
2. **Animator Controller** — nối các state (idle / walk / attack / hurt / death) bằng tham số.
3. **Prefab** — gộp Sprite Renderer + Animator + script thành một prefab để spawn hàng loạt.


---

## 7. Ráp vào scene

Đặt nền, lâu đài, quái và dựng prefab: xem [dung-scene-gameplay.md](dung-scene-gameplay.md).

---

## 8. Danh sách kiểm tra rút gọn

Mỗi lần đưa một bộ art mới vào Unity:

- [ ] Texture Type = Sprite, Sprite Mode = Single
- [ ] Filter Mode = **Point**, Compression = None, Mip Maps tắt, Alpha Is Transparency bật
- [ ] PPU = 100 (giống mọi asset khác)
- [ ] Max Size ≥ cạnh dài nhất của **ảnh gốc**
- [ ] Pivot lấy từ `aligned_bbox[3]` của **chính action đó**, không lấy từ profile chung
- [ ] FX (`align: center`) giữ pivot mặc định
- [ ] Chạy `BuildBuild/Art/Verify Generated Art Import` và đọc bảng, **không tin file `.meta`**
- [ ] Kéo thử vào Scene, đặt đúng đường đất, nhìn xem chân có chạm đất không
