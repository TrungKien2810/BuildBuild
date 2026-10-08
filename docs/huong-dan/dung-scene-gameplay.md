# Hướng dẫn: dựng scene gameplay 2D (nền, lâu đài, HUD, màn gacha, save)

> Doc **học nghề**. Mục đích: bạn tự dựng lại được scene `Battle` và `Gacha` bằng tay, và hiểu vì
> sao `Assets/Editor/SceneBuilder.cs` viết như vậy. Quyết định kiến trúc rút gọn nằm ở
> `docs/project-state/technical-design.md` AD-9.

---

## 0. Vì sao dựng scene bằng code thay vì kéo tay

Kéo tay thì làm được, nhưng mỗi lần art giao lại (nền đổi kích thước, lâu đài vẽ lại) bạn phải căn
lại từng vị trí. Toàn bộ vị trí trong game này **tính ra được từ số liệu art đã giao**:
kích thước `Background/bg-field.png`, `Castle/mounts.json` và bbox đo của sprite. Nên ta viết các phép tính đó
thành code một lần, còn việc giao lại art thì chỉ là bấm lại một menu.

Menu: **`BuildBuild → Scenes → Build Battle + Gacha`**. Scene/prefab dựng lại từ đầu mỗi lần bấm;
ScriptableObject trong `Assets/Data/` chỉ được **tạo** nếu chưa có, nên số bạn chỉnh trong
Inspector (máu Goblin, tỉ lệ gacha...) không bị ghi đè.

Hệ quả cần nhớ: **đừng sửa tay trong `Battle.unity`/`Gacha.unity`**, vì lần bấm menu sau sẽ xoá
mất. Muốn đổi bố cục thì sửa `SceneBuilder.cs`.

---

## 1. Hệ toạ độ: từ pixel art sang đơn vị thế giới

Art nền vẽ trên canvas **1920×1080**. Import với **PPU 100** (xem `unity-import-sprite.md`), tức
**1 px = 0,01 unit**.

**Camera** (làm tay: GameObject → Camera):

| Ô | Giá trị | Vì sao |
|---|---|---|
| Projection | Orthographic | Game 2D, không phối cảnh |
| Size | **5,4** | Size là NỬA chiều cao nhìn thấy: 1080 px ÷ 100 ÷ 2 = 5,4. Sai số này thì nền bị cắt hoặc hở viền |
| Position | (0, 0, −10) | Camera phải đứng trước các sprite (z = 0) |

Game view phải đặt **16:9** (menu tỉ lệ ở góc trên Game view). Tỉ lệ khác thì hai mép trái/phải
bị cắt hoặc lộ màu nền.

**Công thức đổi toạ độ.** Pixel tính từ góc **trên-trái**, còn world tính từ **tâm màn hình, trục
y hướng lên**:

```
x_world = (px − 960) / 100
y_world = (540 − py) / 100
```

Ví dụ thật: mép dưới làn quái `py = 800` → y_world = (540 − 800)/100 = **−2,6**.

---

## 2. Nền sân (một tấm)

Sân là **một** ảnh `Background/bg-field.png` 1920×1080 phủ kín camera (rìa cây trên, bãi cỏ giữa,
rìa thông dưới). Sprite nền không có `pipeline-meta.json` nên pivot mặc định ở **tâm**, vậy tâm ảnh
nằm ở px (960, 540) → world **(0, 0)**.

Làm tay: kéo `bg-field.png` vào Hierarchy (tạo SpriteRenderer), Position (0, 0, 0), **Sorting Order
−100**. Order âm để mọi thứ gameplay (order ≥ 0) luôn nằm trên nền.

**Làn quái** là dải y mà quái được spawn ngẫu nhiên, đọc từ ảnh sân: tán cây rìa trên xuống tới
py ~244, ngọn thông rìa dưới cao nhất py 805 → làn **py 250..800**. Trong `SceneBuilder.cs` là
`LaneTopPx`/`LaneBottomPx`; object `Battlefield` giữ hai số đó dạng world (2,90 và −2,60).

> **Kiểm làn:** mở `bg-field.png`, tìm hàng thấp nhất còn ngọn cây rìa trên và hàng cao nhất có ngọn
> thông rìa dưới. Làn nằm ngoài khoảng đó thì quái đi đè lên tán cây — sửa hai hằng số, đừng bù trong
> code quái (cách đo: `unity-2d-lane-chieu-sau.md` bước 1).

---

## 3. Lâu đài: đặt gốc ở TƯỜNG TRƯỚC, không ở tâm sprite

Pháo đài đá `Castle/fortress.png` (và `fortress-damaged1/2.png`, cùng canvas **735×900**) hiện ở
**×0,5** (`FortressScale`, gán `localScale` cho object `Body`). Đo alpha bản lành trong canvas: mép trái
x **246**, vách phải x **614** (`FortressWallInSprite`), đáy y **861** (`FortressBaseInSprite`). Builder tính:

- mép trái canvas trên màn = −246 × 0,5 = **−123** (`FortressLeftPx`) → mép alpha trái chạm px 0;
- đỉnh canvas = đáy mong muốn px **820** (`FortressBasePx`) − 861 × 0,5 = **389,5**;
- vách phải trên màn = −123 + 614 × 0,5 = **184**.

Muốn đổi cỡ thành: sửa `FortressScale`, giữ số nguyên hoặc ½ để pixel đều, rồi chạy lại
**BuildBuild → Scenes → Build Battle + Gacha**; các toạ độ trên tự tính lại.

`Enemy` dừng lại đánh khi `|x_mục_tiêu − x_quái| ≤ attackRange + BodyRadius` (tầm với + nửa thân quái, xem
mục hitbox trong `Enemy`), và nó đo tới **transform** của lâu đài. Nếu transform nằm ở tâm sprite thì quái
đi xuyên nửa bức thành rồi mới dừng. Nên ta tách thành hai object:

```
Castle   (Health 2000 + Castle)   ← đặt ở tường trước: px 184, py 525 (giữa làn) → world (−7,76, 0,15)
└─ Body  (SpriteRenderer fortress.png, pivot tâm)
         tâm ảnh: px −10 + 735/2 = 357,5, py 90 + 900/2 = 540 → world (−6,025, 0)
```

`Enemy` chỉ so **x** với tường, nên quái ở làn cao hay thấp đều dừng đúng mép vách phải; y của `Castle`
chỉ để nhìn trong Scene view. Builder lấy kích thước ảnh từ `sprite.rect`, không ghim số 735×900.

Làm tay: tạo empty `Castle`, gắn `Health` (Max Health 2000) và `Castle`; tạo con `Body` với
sprite, rồi kéo `Body` vào ô **Body** của `Castle`, kéo 3 sprite vào 3 ô Intact/Damaged1/Damaged2.

### 3b. Thành trì 3 cấp (thay fortress khi chạy)

Fortress ở trên giờ chỉ là hình dự phòng trong Scene view. Lúc chạy, `Castle.Awake` đọc cấp trong save
(`SaveData.castleTier`, mặc định 1) và gọi `ApplyTier`: thay 3 sprite, dời + scale `Body`, đặt lại máu tối đa.
Gốc `Castle` giờ nằm ở **px 560** (`CastleWallPx`), không còn là vách fortress px 536.

Bảng cấp nằm trong ô **Tiers** của component `Castle`, do builder điền (`BuildCastleTiers`), mỗi cấp:

1. Đọc `Assets/Art/Generated/Castle/mounts-tiers.json` → tên sprite + toạ độ tâm mặt bệ (px trong sprite, gốc trên-trái).
2. Scale = **720 / chiều cao sprite** (cùng phép QC đã duyệt), đáy sprite đặt ở **px 830** (`CastleBasePx`).
3. Đo **vách phải**: mỗi hàng 20–97% chiều cao lấy pixel alpha ngoài cùng bên phải, lấy trung vị → đặt đúng px 560,
   nên quái dừng cùng một chỗ dù thành cấp mấy.
4. Đổi toạ độ bệ trong sprite ra world (`trái + x × scale`, `đỉnh + y × scale`) → `mounts` (local theo `Castle`).
5. Scale từng bệ = bề rộng mặt bệ trên màn (`platform_w × scale`) / 100 px, kẹp 0,6–1 → `mountScales`; `CannonBoard`
   gán vào `localScale` của bệ nên pháo (con của bệ) nhỏ theo, điểm ra đạn tính theo `lossyScale`.
6. HP / giá / điều kiện lấy từ `CastleTierStats` trong `SceneBuilder` (2000/3000/4500 · 0/3000/8000 vàng · thắng wave 0/10/20).

`CannonBoard` có sẵn 5 bệ (đủ cho cấp 3); khi đổi cấp nó bật đúng 3/4/5 bệ đầu và dời chúng về `mounts` của cấp đó
(`LayoutMounts`). Nút **Nâng thành** (góc trên-trái, dưới thanh máu) do `BattleHud` bật/tắt; bấm nút mở bảng
**`CastleUpgradePanel`** (builder dựng ở `BuildCastleUpgradePanel`): ảnh sprite lành của cấp này → cấp sau, dòng Máu
thành và Bệ pháo trước → sau, dòng điều kiện (xanh khi đạt, đỏ khi chưa), nút **Nâng · giá** chỉ bấm được khi đạt điều
kiện `Castle.UpgradeBlocker()` (wave cao nhất đã thắng = `nextWave − 1`) và đủ vàng. Bấm Nâng: `BattleHud` trừ vàng,
đóng bảng, gọi `Castle.Upgrade()` — save được ghi ngay, rồi `UpgradeRoutine` chạy hiệu ứng ~0,9 s (thành cũ rung và
ngả vàng → đổi sang cấp mới → thành mới nảy từ 1,06 về 1 và nhạt dần về màu gốc). Lúc đổi cấp, `CannonBoard` cho
khung các bệ vừa mở nháy 8 lần để người chơi thấy chỗ lắp pháo mới.

Đổi số: sửa `CastleTierStats` rồi chạy lại builder (bảng cấp được ghi đè mỗi lần build). Đổi vị trí thành: sửa
`CastleWallPx` / `CastleBasePx` / `CastleHeightPx`.

> **Vì sao `Castle` đọc máu trong `Start`, không trong `Awake`:** Unity **không đảm bảo** thứ tự `Awake`
> giữa hai component trên cùng một object. Nếu `Castle.Awake` chạy trước `Health.Awake` thì máu đang là 0
> và lâu đài hiện sprite **sắp sụp** ngay khi mở scene. Mọi `Awake` luôn chạy xong trước mọi `Start`, nên
> cần đọc giá trị của component khác thì đọc trong `Start`.

---

## 4. Quái và wave

- `EnemySpawn` ở px **1980**, ngoài mép phải 1920, để quái đi vào màn hình chứ không hiện ra giữa đường.
- Prefab `Enemy`: SpriteRenderer (Order 10) + `SpriteAnimator` + `Health` + `Enemy` (kéo
  SpriteAnimator vào ô Animator).
- `Assets/Data/`: `Anim_Goblin` (5 clip, frame lấy theo tên `idle-1..4`, `walk-1..6`...),
  `Enemy_Goblin` (100 máu, 15 sát thương, tốc 1,5, 5 vàng, theo GDD mục 5), `Anim_Ashfang` +
  `Enemy_Ashfang` (60 máu, 10 sát thương, tốc 3,0, 4 vàng, Kind = Nhanh), `WaveTuning` với 2 band:
  wave 1–3 chỉ Goblin, wave 4–30 Goblin + Ashfang (Orc/boss chưa có art). Làm tay: trong
  `WaveTuning` mở mảng **Bands**, thêm phần tử, gõ From/To Wave, kéo asset quái vào **Enemies**.

Tốc 1,5 unit/s, đường từ x 9,8 tới tường −6,22 dài khoảng 16 unit, nên con Goblin đầu tới lâu đài sau
khoảng **10,7 s**.

---

## 5. UI uGUI: Canvas, EventSystem, 9-slice ở 2x

**Canvas** (GameObject → UI → Canvas):

| Ô | Giá trị | Vì sao |
|---|---|---|
| Render Mode | Screen Space – Overlay | HUD luôn đè lên game, không phụ thuộc camera |
| Pixel Perfect | bật | Làm tròn vị trí về pixel màn hình, cạnh pixel art không bị nhoè |
| UI Scale Mode | Scale With Screen Size, 1920×1080, Match 0,5 | Bố cục tính theo màn 1080p, màn khác thì co giãn đều |

**EventSystem.** Project này bật **Input System mới** (`ProjectSettings → Player → Active Input
Handling = Input System Package`). Khi bạn tạo EventSystem, Unity có thể gắn sẵn
`StandaloneInputModule` (của hệ input cũ): nút bấm không phản hồi và console báo lỗi mỗi frame.
Bấm **"Replace with InputSystemUIInputModule"** trong Inspector, hoặc gắn thẳng component đó.

**Mọi UI hiện ở 2x.** Asset UI vẽ nhỏ (nút 96×48, thẻ 96×128, icon 48). Hiện chúng ở 1,5x thì
có hàng pixel dày 1, có hàng dày 2, nhìn méo. Nên ta chỉ dùng **bội số nguyên** và chọn 2x:
thẻ 192×256, badge wave 192×192.

**9-slice** (panel, nút). Ảnh 96×48 kéo thành nút 320×96 mà không cắt thì góc bị kéo giãn. Cách
làm tay:
1. Chọn PNG → **Sprite Editor** → kéo 4 đường xanh ở cạnh, hoặc gõ **Border** L/R/T/B = 12 → Apply.
   Số border lấy từ `UI/9slice/slice.json` đi kèm bộ asset UI.
2. Trên `Image`: **Image Type = Sliced**.
3. **Pixels Per Unit Multiplier = 0,5**, để border 12 px hiện thành 24 px, cùng mật độ 2x với
   phần UI còn lại. Để 1 thì viền nút mảnh bằng một nửa viền thẻ ngay cạnh.

`GeneratedArtImporter` tự đọc `slice.json` nên bước 1 chỉ cần khi làm không có script.

> **Lưu ý:** sprite **không có border** mà đặt Image Type = Sliced thì Unity vẽ như Simple (kéo giãn
> cả góc) và **không báo lỗi gì**. Nếu thấy góc nút bị kéo dài, kiểm tra Border trong Sprite Editor trước.

**Nút 4 trạng thái:** Button → Transition = **Sprite Swap**, kéo `button-hover` / `-pressed` /
`-disabled` vào 3 ô tương ứng. Bốn ảnh này được giao **trùng bbox ≤ 1 px** chính là để nút không
bị nhảy khi hover.

**Chữ:** project chưa import TextMesh Pro, nên dùng `Text` legacy với font `LegacyRuntime.ttf`
(có đủ dấu tiếng Việt), thêm component `Outline` để đọc được trên nền sáng.

---

## 6. Save JSON

File nằm ở `Application.persistentDataPath/save.json`. Trên Windows đó là
`%USERPROFILE%\AppData\LocalLow\<Company>\BuildBuild\save.json`, xem chính xác ở
`Edit → Project Settings → Player → Company Name`.

```json
{ "version": 1, "gold": 5000, "rollsSinceSR": 12, "rollsSinceSSR": 12,
  "owned": [ { "id": "knight", "level": 1, "stars": 0, "shards": 20 } ] }
```

- **Vì sao không PlayerPrefs:** PlayerPrefs chỉ lưu từng số/chuỗi đơn lẻ. Danh sách sở hữu, mỗi
  món có level, sao, mảnh, phải tự mã hoá thành chuỗi, còn JSON thì `JsonUtility` làm sẵn.
- **Ghi file tạm rồi mới thay** (`save.json.tmp` → `save.json`): tắt game đúng lúc đang ghi thì
  vẫn còn bản cũ nguyên vẹn, không bị file cụt nửa chừng.
- **Test gacha mà không cần cày vàng:** vào Play Mode, chuột phải component `Economy` →
  **Dev: +5000 vang**. Hoặc tắt game, sửa trường `gold` trong file. Xoá file là làm lại từ đầu.
- **`id` của unit là khoá trong save.** Đổi `id` trong `Unit_*.asset` là người chơi mất món đó.

---

## 7. Gacha: tỉ lệ và pity

Một lượt quay chạy theo thứ tự sau (`GachaSystem`):

1. Quay **độ hiếm** theo bảng 60 / 30 / 9 / 1.
2. **Pity:** đã 80 lượt liên tiếp không SSR thì ép SSR; đã 30 lượt không SR+ thì nâng lên ít nhất SR.
3. Chọn **loại** theo banner đang mở (4 tab trên màn Gacha). Banner *Tổng hợp* quay loại theo `heroShare` /
   `soldierShare` trong `GachaConfig` (phần còn lại là pháo); banner *Hero* / *Lính* / *Trụ* dùng luôn loại đó
   (`GachaSystem.Roll(count, only)`, `only = null` là banner tổng hợp).
4. Chọn unit có **đúng độ hiếm + loại**. Banner tổng hợp: không có unit nào thì thử các loại khác; vẫn không có thì
   hạ một bậc. Banner riêng: chỉ hạ bậc **trong cùng loại**, không bao giờ ra loại khác.
5. Đã sở hữu thì cộng **+20 mảnh**; đủ 100 mảnh thì **+1 sao**.

Roster hiện tại chỉ có N (3 hero + 3 pháo) và một SR (Pháp sư triệu hồi). Vì vậy R (30%) rơi về
N, SSR (1%) rơi về SR. Tỉ lệ thực tế khoảng **90% N / 10% Triệu hồi sư**, tới khi có unit R/SSR.

> **Vì sao pity đếm theo bậc QUAY RA, không theo unit NHẬN ĐƯỢC:** nếu đếm theo unit nhận được thì lượt 81
> bị ép SSR, nhưng roster không có unit SSR nên hạ thành SR, và bộ đếm SSR **không bao giờ về 0** — từ đó
> mọi lượt đều bị ép SSR. Đếm theo bậc quay ra thì bộ đếm reset đúng dù roster thiếu bậc.

Dòng "Chắc chắn SR trong N lượt" có **+1**: bộ đếm = 30 nghĩa là lượt **kế tiếp** chắc chắn SR,
nên lúc bộ đếm = 0 thì phải hiện "trong 31 lượt", không phải 30.

## 8. Màn Đội quân: xem và nâng Hero / Lính / Trụ

Màn này là scene riêng `Assets/Scenes/Units.unity`, dựng bằng code như Battle và Gacha.

1. **Dựng lại:** menu **BuildBuild → Scenes → Build Battle + Gacha**. Lệnh này dựng cả ba scene và thêm `Units` vào
   Build Settings. Bạn cần dựng lại mỗi khi thêm unit mới (`Assets/Data/Units/Unit_*.asset`), vì danh sách unit được
   nạp lúc dựng scene chứ không phải lúc chạy.
2. **Mở trong game:** trên màn trận, lúc nghỉ giữa hai wave, bấm nút **Đội quân** dưới nút Triệu hồi. Trong lúc đang
   đánh wave thì nút này bị xám.
3. **Dùng:** chọn tab Hero / Lính / Trụ, bấm vào một thẻ. Bảng bên phải hiện chỉ số hiện tại. Số màu xanh sau `→` là
   chỉ số sau khi nâng thêm 1 level.
   - **Nâng level** tốn vàng: hero và lính theo `floor(50 × n^1,5)`, pháo theo giá riêng trong `CannonDefinition`. Level
     chỉ tăng chỉ số.
   - **Nâng sao** tốn `GachaConfig.shardsPerStar` mảnh (100). Nút sáng lên khi đủ mảnh. Sao vừa tăng chỉ số vừa mở kỹ
     năng.

**Code làm gì bên trong:**
- `SceneBuilder.BuildUnitsScene` nạp mọi `UnitDefinition` trong `Assets/Data/Units`, mọi `HeroDefinition` có `unitId`
  (gồm cả lính, vì `SoldierDefinition` kế thừa `HeroDefinition`) và mọi `CannonDefinition`, rồi gán các danh sách này
  vào `UnitsScreen`.
- `UnitsScreen` ghép thẻ với chỉ số qua id: thẻ `UnitDefinition.id` khớp với `HeroDefinition.unitId` hoặc
  `CannonDefinition.unitId`. Đọc level, sao và mảnh từ save qua `SaveSystem.Data.Find(id)`. Unit chưa sở hữu thì thẻ
  bị tô xám.
- Hệ số chỉ số lấy từ `Progression`, cũng là hàm mà `Hero.Initialise` dùng trên sân. Công thức:
  `1 + 8% × (level − 1) + 15% × sao`, nhân vào máu và sát thương gốc.
- Tab "Kỹ năng" là tối đa 4 dòng, mỗi dòng một icon 48 px bên trái + chữ bên phải. Icon lấy từ ô `Ultimate Icon` /
  `Minor Icon` / `Second Minor Icon` của hero (nội tại không có icon); `FillSkillTexts` gán lại icon `UI/icons/icon-skill-*`
  theo `unitId` MỖI lần dựng, nên muốn đổi icon thì sửa bảng `icons` trong hàm đó chứ không sửa Inspector.
- Chữ trong tab "Kỹ năng" lấy từ 4 ô `Ultimate Text` / `Passive Text` / `Minor Text` / `Second Minor Text` trong
  Inspector của hero. `SceneBuilder.FillSkillTexts` chỉ điền vào ô còn trống, nên bạn sửa chữ trong Inspector thì lần
  dựng lại sau cũng không bị ghi đè. Ô nào để trống thì dòng kỹ năng đó không hiện.
- Dòng kỹ năng chưa đủ sao hiện màu xám, kèm `Mở ở N★`. Ngưỡng sao lấy từ `Passive Star` / `Minor Star` /
  `Second Minor Star` của hero.

### Trang bị (tab Trang bị trong bảng chi tiết hero)

1. Chọn một hero, bấm **Trang bị**. Hàng trên có 4 ô Vũ khí / Giáp / Mũ / Nhẫn. Ô viền màu theo độ hiếm; ô xám là
   ô trống.
2. Bấm vào một ô để chọn. Hàng dưới hiện các món cùng loại đang nằm trong túi (tối đa 8 món tốt nhất). Bấm vào một món
   để mặc. Món đang mặc ở ô đó (nếu có) tự về túi.
3. **Nâng cấp** tốn vàng; **Tháo** trả món về túi.
4. Muốn có đồ để thử mà chưa thắng wave nào: F1 → Admin → **+4 trang bị** (mỗi ô 1 món, độ hiếm ngẫu nhiên).

**Code làm gì bên trong:**
- Không có asset riêng cho từng món. Một món là một dòng `OwnedEquipment` trong save, gồm `slot`, `rarity`,
  `level` và `heroId` (unitId của hero đang mặc; để trống nghĩa là món đó nằm trong túi). `Armory` lo việc mặc, tháo,
  cộng chỉ số và rơi đồ.
- Mọi con số nằm ở `Assets/Data/EquipmentConfig.asset`: % của bậc N ở level 1, hệ số theo độ hiếm, % tăng mỗi level,
  giá nâng, tỉ lệ rơi, và 4 icon. Sửa số trong Inspector là game dùng số mới ngay.
- Công thức một món: `% gốc của ô × hệ số độ hiếm × (1 + 10% × (level − 1))`. Vũ khí cộng sát thương, giáp và mũ cộng
  máu, nhẫn cộng tốc đánh.
- `Progression.Multipliers` cộng dồn level + sao + đồ thành một hệ số rồi nhân vào chỉ số gốc. `Hero.Initialise` nhận
  config đồ từ `HeroBoard`, nên đổi đồ trong màn Đội quân thì từ wave sau hero trên sân mới dùng số mới.
- Khi thắng wave, `BattleHud` gọi `Armory.RollDrop`: wave thường rơi đồ theo `dropChance`; wave chia hết cho
  `guaranteedEvery` (wave boss) chắc chắn rơi, ít nhất bậc R. Món nhận được hiện thành một dòng trong popup thắng.
