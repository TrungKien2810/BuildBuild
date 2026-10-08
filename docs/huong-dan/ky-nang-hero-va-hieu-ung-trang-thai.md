# Hướng dẫn: kỹ năng hero & hiệu ứng trạng thái (vùng băng Frostweaver)

> Doc **học nghề**. Mục đích: bạn tự thêm được một kỹ năng mới (hoặc một hiệu ứng trạng thái mới:
> làm chậm, choáng, thiêu đốt) mà không phải đọc lại toàn bộ code, và hiểu vì sao `FrostZone.cs`
> viết như vậy. Quyết định kiến trúc rút gọn: `docs/project-state/technical-design.md` AD-10.

Áp dụng cho: `Assets/Scripts/Combat/FrostZone.cs`, `Enemy.cs`, `Hero.cs`,
`Assets/Scripts/Data/HeroDefinition.cs`, `Assets/Scripts/Core/SpriteAnimator.cs`.

---

## 0. Bài toán: ai giữ đồng hồ của một hiệu ứng có thời hạn?

Đây là câu hỏi quan trọng nhất, và chọn sai thì mọi thứ sau đó đều phải vá.

Có đúng hai cách:

**Cách A — nạn nhân giữ đồng hồ.** Vùng băng gọi `enemy.Freeze(3f)` **một lần** lúc quái chạm mép,
quái tự đếm 3 giây rồi tan. Nghe hợp lý, nhưng:

- Quái đi vào vùng ở giây thứ 2,9 của vùng → nó vẫn đóng băng 3 giây, tức **2,9 giây sau khi vùng
  đã tan**. Nhìn ra ngay: đĩa băng biến mất mà quái vẫn đứng như tượng.
- Muốn sửa thì phải có `OnExit` (quái rời vùng) — nhưng quái **đang đóng băng thì không rời được**,
  nên `OnExit` chỉ bắn khi vùng bị huỷ, tức bạn phải cho vùng nhớ danh sách quái nó đã chạm.
- Hai vùng chồng nhau → phải đếm số nguồn (refcount), và refcount rò ngay lần đầu quái **chết** khi
  đang trong vùng.

**Cách B — NGUỒN giữ đồng hồ (cách dự án này chọn).** Vùng còn sống thì **mỗi frame** nó nói với
mọi con trong bán kính: *"mày còn bị đóng băng thêm đúng `thời gian còn lại của tao` giây nữa"*.

```csharp
// FrostZone.Update()
_remaining -= Time.deltaTime;
if (_remaining <= 0f) { Destroy(gameObject); return; }
foreach (var enemy in Enemy.Alive)
    if (trong bán kính) enemy.ApplyFreeze(_remaining);

// Enemy.ApplyFreeze() — đồng hồ nằm trong kho hiệu ứng chung (mục 7), luật "lấy hạn dài hơn" = Max
Status.Apply(StatusId.Freeze, 0f, seconds);
```

Ba hệ quả **miễn phí**, không viết thêm dòng nào:

| Tình huống | Kết quả |
|---|---|
| Quái vào vùng ở giây cuối | Tan băng **đúng lúc** vùng tan (`_remaining` lúc đó gần 0) |
| Hai vùng chồng nhau | `Max` tự lấy cái dài hơn — không refcount, không danh sách |
| Quái chết trong vùng | Không có gì phải dọn; `ApplyFreeze` tự bỏ qua khi `_dying` |

**Quy tắc rút ra, dùng cho MỌI hiệu ứng vùng sau này** (vùng lửa, vùng buff tốc đánh): nguồn phát
giữ đồng hồ và nhắc lại mỗi frame bằng **thời gian còn lại của chính nó**; nạn nhân chỉ lưu một
`float` và `Max`. Đừng để nạn nhân giữ danh sách buff.

Ngoại lệ: hiệu ứng **một phát rồi thôi** (đòn choáng 1 giây của boss) thì nguồn không sống lâu, gọi
`ApplyFreeze(1f)` một lần là đúng — cùng một hàm, không cần API thứ hai.

---

## 1. Đóng băng phải NHÌN thấy được, nếu không nó là bug

Vì sao: chặn `March()` và `Attack()` là đủ về mặt luật chơi — quái đứng im, không gây
sát thương. Nhưng `SpriteAnimator` vẫn chạy, nên **chân nó vẫn bước tại chỗ**. Người chơi đọc ra
"game bị đứng hình / quái kẹt", không phải "mình vừa đóng băng nó".

Hai thứ tối thiểu để đọc ra được:

1. **Dừng animation tại khung hình hiện tại** — thêm `public bool Paused { get; set; }` vào
   `SpriteAnimator`, kiểm ở đầu `Update()`. Dừng tại chỗ (không reset `_frame`) mới ra dáng "bị
   khoá giữa động tác"; reset về frame 0 thì thành "vừa đổi tư thế".
2. **Đổi màu sprite** — `SpriteRenderer.color = (0,62 · 0,82 · 1)`. Xanh băng nhạt, vẫn đọc được
   silhouette gốc.

Chi tiết dễ bỏ sót: **chỉ chạm renderer khi trạng thái ĐỔI**, không phải mỗi frame.

```csharp
bool frozen = IsFrozen;
if (frozen != _frozenVisual) { _frozenVisual = frozen; /* đổi màu, đổi Paused */ }
```

Và **`hurt` không được phát khi đang đóng băng** (`OnDamaged` phải kiểm `IsFrozen`), nếu không con
quái bị bắn sẽ giật một cái rồi cứng lại — hỏng đúng cái cảm giác vừa dựng lên.

Cuối cùng, `OnDied` phải gọi `Thaw()` trước khi phát `death`: chết trong lúc đóng băng mà animator
vẫn `Paused` thì animation death không chạy, và `Enemy` đợi `ClipFinished` mãi mãi → **quái không
bao giờ biến mất và người chơi không nhận vàng**.

---

## 2. Đặt trước khi trừ cooldown — thứ tự trong `Update` là một quyết định thiết kế

```csharp
void Update()
{
    if (_dying || _definition == null) return;
    if (UpdateFrozen()) return;        // <-- ĐẶT Ở ĐÂY
    _attackCooldown -= Time.deltaTime;
    // ...
}
```

Đặt sau dòng trừ cooldown thì đồng hồ đánh vẫn chạy trong lúc đóng băng: con quái đóng băng 3 giây
với nhịp 1 đòn/giây sẽ **đánh ngay một đòn lúc vừa tan băng** và coi như được 3 giây hồi chiêu miễn
phí. Đặt trước thì nó tiếp tục từ đúng chỗ cũ. Chênh nhau một dòng, khác nhau cả chuyện đòn khống
chế mạnh hay vô dụng.

---

## 3. Làm bằng tay: thêm một kỹ năng mới, từng bước

### Bước 1 — thêm số vào `HeroDefinition.cs`

```csharp
public enum SkillKind { None, FrostZone, Petrify, ConeSweep, RapidFire, SummonKnight, Charge }
// SummonKnight (Bonecaller): gọi kỵ sĩ tử thần — số nằm ở asset SummonerDefinition (ô Summoner).
// Charge (kỵ sĩ): phi dài skillRadius, skillPower sát thương, choáng skillDuration giây.

[Header("Ky nang")]
public SkillKind skillKind = SkillKind.None;       // kỹ năng LÀM GÌ
public SkillTier skillTier = SkillTier.Ultimate;   // Ultimate = chiêu lớn, Minor = chiêu nhỏ
public float skillCooldown;              // mặc định 0 — CHỈ cho chiêu nhỏ
public float manaMax;                    // mặc định 0 — CHỈ cho chiêu lớn
public float manaPerSecond = 0.35f;
public float manaPerHit = 0.25f;
public float skillRadius = 1.6f;         // FrostZone: bán kính vùng · ConeSweep: chiều dài nhát chém
public float skillDuration = 3f;         // FrostZone: vùng tồn tại · Petrify: bất động · RapidFire: buff
public float skillPower = 1f;            // ConeSweep: ×damage · RapidFire: ×tốc đánh · Petrify: tỉ lệ phản đòn
public Sprite[] skillFrames;             // FrostZone: vùng băng · ConeSweep: vết chém

public bool HasSkill => skillKind != SkillKind.None
    && (skillTier == SkillTier.Ultimate ? manaMax > 0f : skillCooldown > 0f);
```

**`skillKind` quyết định ba ô Radius/Duration/Power nghĩa là gì** — một bộ ô dùng chung cho 4 loại kỹ
năng thay vì mỗi loại một bộ field riêng. Tooltip trong Inspector ghi nghĩa theo từng loại.

**Clip thân lúc cast** (tuỳ chọn): `SpriteAnimationSet` có ô **Skill**. Kéo frame vào (Cragfist:
`petrify-body/petrify-1..6`, Ironblade: `cone-sweep-body/cone-sweep-body-1..6`). Để trống thì hero cast
bằng clip Attack — Frostweaver dùng cách này.

**Hai tầng kỹ năng.** Chiêu **lớn** chạy bằng thanh mana; chiêu **nhỏ** chạy bằng **cooldown ẩn** —
không thanh, không tốn mana. Tiêu chí phân tầng là **chỗ người chơi thấy**, không phải sức mạnh:
thanh mana là thứ người chơi chờ đợi nên để dành cho đòn đổi cục diện, còn bày thanh cho chiêu nhỏ
thì 9 ô hero trên sân thành một mớ thanh nhấp nháy mà người chơi chẳng làm gì được với thông tin đó
(mọi thứ đều tự động).

`HasSkill` đọc **đúng con số của tầng đang chọn** là có chủ đích: đổi `skillTier` mà quên điền số
của tầng mới thì hero **mất hẳn kỹ năng** — lỗi ồn ào, thấy ngay. Nếu viết `manaMax > 0 || skillCooldown > 0`
thì hero đổi sang Minor vẫn chạy lén theo `manaMax` cũ, và bạn đi tìm rất lâu.

**Vì sao `manaMax` mặc định 0:** `HeroDefinition` dùng chung cho mọi hero. Nếu mặc định là 6 thì
Cung thủ, Hiệp sĩ... thêm vào sau sẽ **tự nhiên biết đóng băng** mà không ai chủ ý — và bạn sẽ đi
tìm bug đó rất lâu. Mặc định phải là "không có"; bật thì bật rõ trong asset.

**Vì sao hai nguồn mana chứ không phải cooldown:** cơ chế nhân vật của game là thanh mana đầy thì
tự cast. Mana vào từ **thời gian** (`manaPerSecond`) và từ **số quái ăn đòn thường**
(`manaPerHit`, cộng cho TỪNG con trúng splash, không phải mỗi phát bắn). Hai nguồn chứ không phải
một là có chủ đích:

- Chỉ thời gian → thành cooldown trá hình, hero đứng không cũng lên chiêu đúng nhịp đó.
- Chỉ đòn đánh → hero không có mục tiêu thì tê liệt vĩnh viễn, và wave thưa quái thành vô dụng.
- Cả hai → **đánh đông thì lên chiêu nhanh hơn**, đúng lúc đang bị vây. Đó là lúc đòn khống chế
  đáng giá nhất. Frostweaver bắn splash nên một phát trúng 3 con cộng 0,75 mana thay vì 0,25.

### Bước 2 — bật trong asset

Trong Unity: chọn `Assets/Data/Hero_Frostweaver.asset` → Inspector → **Skill Kind `FrostZone`**, điền
Mana Max `6`, Mana Per Second `0.35`, Mana Per Hit `0.25`, Skill Radius `1.6`, Skill Duration `3`.

Số của các hero khác (menu **BuildBuild → Scenes → Build Battle + Gacha** tự điền khi asset chưa có Skill Kind):

| Hero | Skill Kind | Mana Max | Radius | Duration | Power |
|---|---|---|---|---|---|
| Cragfist | Petrify | 6 | — | 5 | 0.2 |
| Ironblade | ConeSweep | 5 | 2 | — | 3 |
| Hawkfletch | RapidFire | 4 | — | 5 | 3 |

Sửa tay file YAML cũng được — chèn ngay trước
`animations`:

```yaml
  manaMax: 6
  manaPerSecond: 0.35
  manaPerHit: 0.25
  skillRadius: 1.6
  skillDuration: 3
  skillFrames: []
  animations: {fileID: 11400000, guid: ...}
```

**Chiêu mới là nội tại hay chiêu phụ thì khoá nó theo sao.** Người chơi có hero là dùng được đánh thường + chiêu
cuối ngay; nội tại và chiêu phụ chỉ chạy khi đủ sao. Ba ô ngưỡng nằm ở nhóm **Mo khoa theo sao** trong Inspector:
`Passive Star` (mặc định 1), `Minor Star` (3), `Second Minor Star` (3, chỉ Wyrmspear). Trong code chiêu mới, chặn
bằng `_hero.PassiveUnlocked` / `_hero.MinorUnlocked` ngay đầu nhánh chạy chiêu — ví dụ `Warden.Update` có
`if (!_hero.MinorUnlocked) return;` trước khi đếm cooldown Quét Xích. `Hero` đọc số sao từ save (`Progression.Stars`)
mỗi lần `Initialise`, nên nâng sao giữa wave thì từ wave sau mới có tác dụng. Test nhanh: F1 → Admin → nút **+★**
cạnh unit.

Thứ tự trong YAML không bắt buộc (Unity khớp theo tên field), nhưng lệch thứ tự khai báo thì lần
Unity ghi lại file sẽ tạo diff giả trong git — giữ đúng thứ tự cho sạch.

### Bước 3 — nhánh cast trong `Hero.Update`

```csharp
bool UsesMana => _definition.HasSkill && _definition.skillTier == SkillTier.Ultimate;

bool SkillReady => _definition.skillTier == SkillTier.Ultimate
    ? _mana >= _definition.manaMax
    : _skillCooldown <= 0f;

// trong Update:
_skillCooldown -= Time.deltaTime;
if (UsesMana)
    _mana = Mathf.Min(_definition.manaMax, _mana + _definition.manaPerSecond * Time.deltaTime);

if (_definition.HasSkill && SkillReady) BeginCast(skill: true);
else if (_cooldown <= 0f)               BeginCast(skill: false);
```

Thanh mana chỉ gắn cho chiêu lớn (`if (UsesMana) ManaBar.Attach(this)`) — "cooldown **ẩn**" nghĩa
đen là không vẽ gì lên màn hình.

**Kỹ năng ưu tiên hơn đòn thường** khi cả hai cùng sẵn sàng: thanh đã đầy mà còn nhường nhịp cho một
phát đạn (chu kỳ 1,67 giây) là phí cả chu kỳ tích mana.

**Trừ mana ở ĐẦU cú cast, không phải cuối.** Thanh vơi đi ngay lúc cây gậy vung lên — khớp với cái
người chơi nhìn thấy. Trừ ở cuối thì thanh đứng đầy suốt animation, đọc ra như game bị treo.

Vùng băng ra ở **frame chạm đòn** của clip (ô `hitFrame` của clip trong `Anim_<Hero>.asset`, tính từ 1;
`SpriteAnimator` bắn sự kiện `HitFrame` → `OnHitFrame` → `FinishCast`), giống hệt đạn thường — khớp với lúc
cây gậy vươn xa nhất. Chọn `hitFrame` bằng cách mở từng frame attack, lấy frame vũ khí/tay vươn xa nhất.
Để `hitFrame = 0` thì đòn ra ở cuối clip như cũ (`OnClipFinished`). `_casting` vẫn giữ tới hết clip nên
phần thu tay không bị cắt. Dùng cờ `_castingSkill` để `FinishCast` biết nó đang kết thúc cú nào,
rồi `switch (skillKind)` chọn việc: FrostZone → `CastFrostZone`, ConeSweep → `CastConeSweep` (trừ máu mọi
quái trong nửa hình chữ nhật trước mặt + `OneShotFx` vết chém), RapidFire → đặt mốc hết buff
`_rapidFireUntil` (trong buff, cooldown đòn thường chia cho `skillPower` và `SpriteAnimator.Speed` nhân lên
cho animation khớp nhịp).

**Hoá Đá khác ba loại kia: bắt đầu ngay ĐẦU cú cast**, không đợi hết clip. `BeginPetrify` bật
`Health.Invulnerable` và đặt mốc hết giờ; `Update` return sớm suốt thời gian đó (không đi, không đánh);
hết clip thì giữ frame cuối (đống đá). Cùng lúc nó gọi `OneShotFx.SpawnLoop` gắn vỏ đá làm con của
hero: `skillIntroFrames` (`Cragfist/petrify-cast`, phiến đá mọc lên) chạy một lần, rồi `skillFrames`
(`Cragfist/petrify-shell`) lặp đúng `skillDuration` giây và tự huỷ. FX pivot giữa canvas nên đẩy lên
`PetrifyFxOffset` 0,32 unit cho chân đá trùng chân hero; order 11 để đá nằm trước thân (order 10). Đòn đánh vào lúc này không trừ máu mà bắn sự kiện
`Health.Blocked(sát thương, Health kẻ đánh)` — hero trả `sát thương × skillPower` vào kẻ đánh. Vì thế
`Enemy.Attack` gọi `target.TakeDamage(_damage, _health)`: thiếu tham số nguồn thì không phản được.

### Bước 4 — chỉ số khởi điểm, lấy từ đâu

| Số | Giá trị | Lấy từ đâu |
|---|---|---|
| `skillDuration` | 3 giây | Bằng thời gian làm chậm của pháo Băng (GDD mục 4) — cùng họ hiệu ứng thì cùng nhịp |
| `skillRadius` | 1,6 unit | Bằng bề rộng art `frost-zone` (326 px = 3,26 unit đường kính). `splashRadius` của đạn thường giờ là 0,45 = bề rộng art `frost-impact` (83 px) |
| `manaMax` | 6 | Cột Mana của Pháp sư nguyên tố trong GDD mục 3 |
| `manaPerSecond` | 0,35 | 0,35 × 12 = 4,2 mana |
| `manaPerHit` | 0,25 | 12 giây được ~7 phát; trúng trung bình 1 con → ~1,75 mana |

Hai dòng cuối cộng lại ≈ 6, tức **đầy thanh sau ~12 giây khi đánh bình thường** — và xuống còn ~7
giây khi mỗi phát splash trúng 3 con. Uptime 3/12 = 25% ở nhịp thường.

Đây là **số khởi điểm để cân bằng**, không phải số đã cân bằng. Để trong asset đúng như mọi chỉ số
khác (GDD mục 3), không hard-code trong script.

---

## 4. Ngắm vào cụm, không ngắm con gần nhất

`FindTarget()` của đòn thường lấy con **gần nhất** — đúng cho đòn đơn mục tiêu. Đòn khống chế thì
sai: con gần nhất thường là con đi lẻ đầu đội hình, đóng băng nó xong cả đám phía sau vẫn tiến.

`FindDensestCluster()` thử **từng con quái trong tầm hero làm tâm**, đếm xem đặt vùng ở đó thì dính
mấy con, rồi lấy tâm nhiều nhất.

Đây là O(n²). **Vì sao vẫn chấp nhận:** `n` là số quái còn sống trong wave — công thức GDD là
`5 + floor(wave/2)`, tức **tối đa khoảng 20 con ở wave 30** — và vòng quét chỉ chạy **một lần mỗi
lần cast** (~12 giây/lần), không phải mỗi frame. 400 phép so sánh bình phương khoảng cách mỗi 12
giây là số không. Đổi sang lưới không gian ở đây là tối ưu cho một bài toán không tồn tại.

Ngưỡng để đổi ý: wave có trên 200 quái, **hoặc** vòng quét bị gọi mỗi frame. Đo lại rồi hãy tối ưu.

---

## 5. Chưa có art thì làm gì — placeholder phải TỒN TẠI

Vùng băng chưa có sprite trong `Assets/Art/Generated/Frostweaver-v2/`. Có hai lựa chọn tệ và một
lựa chọn đúng:

- **Sai — không vẽ gì, đợi art.** Tính năng chạy nhưng vô hình: quái tự nhiên đứng im giữa sân,
  không ai biết vì sao. Đây đúng kiểu "tính năng chết trong im lặng" — tệ hơn là chưa làm.
- **Sai — mượn tạm sprite `frost-impact`.** Sai tỉ lệ, và sáu tháng sau không ai nhớ nó là tạm.
- **Đúng — vẽ một đĩa tròn bằng `Texture2D` lúc chạy**, khoảng 20 dòng trong `FrostZone.Placeholder()`.

```csharp
float d = khoảng_cách_tới_tâm / bán_kính;
float alpha = d > 1f ? 0f : d > 0.86f ? 0.8f : 0.3f;   // viền đậm, lòng trong
```

Ba mẹo khiến placeholder này không gây hiểu nhầm về sau:

- `Sprite.Create(..., pixelsPerUnit: size)` → sprite rộng **đúng 1 unit**, nên scale thẳng theo bán
  kính: `transform.localScale = Vector3.one * (radius * 2f)`. Đổi `skillRadius` trong Inspector thì
  hình vẽ đổi theo, không lệch.
- Nhánh placeholder **chỉ chạy khi `skillFrames` rỗng**. Đổ frame thật vào là nó tự tắt, và art thật
  **không** bị scale (art phải vẽ đúng cỡ vùng) — đừng để cả hai nhánh cùng scale.
- `filterMode = FilterMode.Point`, giống mọi sprite pixel-art khác của dự án
  (`unity-import-sprite.md`).

`sortingOrder = 5` (dải `SortOrder.GroundDecal`): dưới quái và hero (order theo chân 680 … 9320, xem
`unity-2d-lane-chieu-sau.md`) và dưới đạn (`SortOrder.Air` 9500). Đĩa nằm trên **mặt đất**, quái phải đứng đè lên nó.

---

## 6. Checklist khi thêm hiệu ứng trạng thái tiếp theo

- [ ] Nguồn giữ đồng hồ, nhắc lại mỗi frame bằng **thời gian còn lại của nguồn**; nạn nhân chỉ `Max`.
- [ ] Kiểm trạng thái **trước** khi trừ mọi cooldown trong `Update`.
- [ ] Có tín hiệu **nhìn thấy được**: đổi màu và/hoặc dừng animation — chỉ chạm renderer khi đổi trạng thái.
- [ ] `hurt` và animation khác không được phá tín hiệu đó.
- [ ] Đường chết: `OnDied` gỡ hiệu ứng trước khi phát `death`, nếu không quái kẹt vĩnh viễn.
- [ ] Số để trong ScriptableObject, **mặc định = tắt**.
- [ ] Điều kiện kích hoạt đi qua đúng tầng: chiêu lớn → thanh mana, chiêu nhỏ → cooldown ẩn. Không tự chế nhịp thứ ba.
- [ ] Chiêu nhỏ KHÔNG được vẽ thanh hay chỉ báo nào lên màn hình.
- [ ] Chưa có art thì có placeholder tự vẽ, và nhánh placeholder tự tắt khi art về.
- [ ] Mở Unity: Console không có lỗi compile, vào Play xem kỹ năng chạy đúng.

---

## 7. Kho hiệu ứng chung `StatusEffects` — mọi nguồn gọi cùng một chỗ

Cháy, Độc, Chậm, Đóng băng, Choáng, Phá giáp, Cấm hồi máu không còn nằm rải trong `Unit`/`Enemy`/`Hero`
hay component riêng (`Burn`, `Venom` đã xoá). Mỗi unit có một kho `unit.Status` (component
`StatusEffects`, tự gắn lần đầu gọi tới). Pháp Sư Lửa, đất cháy của Rồng hay quái Kỳ Nhông sau này đều gọi:

```csharp
enemy.Status.Apply(StatusId.Burn, dps, giây, nguồnHealth, Side, spread: bánKínhLan);
enemy.Status.Apply(StatusId.Poison, dpsMỗiTầng, giây, nguồnHealth, Side, stacks: 1, maxStacks: 5);
hero.Status.Apply(StatusId.NoHeal, 0f, giây);
```

Code bên trong làm gì:

- **Luật cộng dồn đặt theo mã, không theo nguồn.** `Poison` cộng tầng tới trần (trần lớn nhất từng gặp),
  sát thương mỗi tầng lấy mức cao hơn. Mọi mã khác không cộng: giữ mức **mạnh hơn** và hạn **dài hơn**.
- **DoT tick mỗi 0,5 giây** trong `StatusEffects.Update` (Cháy: `dps × 0,5`; Độc: `dps × tầng × 0,5`),
  ghi sát thương loại `Periodic`.
- **Cháy lan:** unit chết lúc đang cháy với `spread > 0` thì lửa nhảy sang đối thủ gần nhất của phe người đốt.
- **Cấm hồi máu** ghi xuyên sang `Health.NoHealUntil`, vì `Health.Heal` đọc trường đó.
- **FX bám thân:** nguồn có asset riêng thì truyền `fx: frames` vào `Apply` (Pháp Sư Lửa truyền `passiveFrames`
  của nó). Không truyền thì dùng hình chung trong `Assets/Resources/StatusLibrary.asset`: chọn asset đó trong
  Project, mở `Visuals`, kéo frame vào `Fx Frames` của dòng `Burn`/`Poison`. Vẫn là MỘT trạng thái — hình hiện
  theo lần gắn mạnh nhất, lửa lan sang con khác mang theo hình của nó.
- **Giải debuff:** `unit.Status.Cleanse()` gỡ toàn bộ. Ấn Đức của Lưu Bị gọi qua `ClearDebuffs()`, hàm này
  gọi `Cleanse()` rồi gỡ thêm Trói / Xích / Long Uy (ba hiệu ứng này chưa chuyển vào kho).
- **Bảng thông tin hero** lấy dòng từ `StatusEffects.Collect`.
- **Vùng trên mặt đất** (`HazardPool`: vũng độc Goblin, đất cháy Rồng) không tự trừ máu. Cứ mỗi 0,5 giây nó
  `Apply` trạng thái cho hero đứng trong vùng, với hạn `poolPoisonSeconds` / `burnLingerSeconds` (ô trong
  `EnemyDefinition` / `DreadwyrmDefinition`). Bước ra khỏi vùng thì trạng thái vẫn dính hết hạn đó.

Thêm một hiệu ứng mới bằng tay:

1. Mở `Assets/Scripts/Combat/StatusEffects.cs`, thêm mã vào `enum StatusId`.
2. Luật mặc định là "mạnh hơn + dài hơn". Cần luật khác (cộng tầng, tick sát thương) thì thêm nhánh trong `Apply`
   và trong `IsDot`, giống `Poison`.
3. Thêm một dòng vào `Collect` để bảng thông tin hiện nó. Nếu cần icon riêng, thêm mã vào cuối `EffectKind`
   (`UnitEffect.cs`) và thêm một dòng tương ứng vào cuối `EffectIconPaths` trong `SceneBuilder.cs`.
4. Có FX bám thân: mở `StatusLibrary.asset`, thêm một phần tử vào `Visuals`, chọn `Id` rồi kéo frame vào.
5. Chỗ đọc hiệu ứng (khoá di chuyển, giảm sát thương...) gọi `Status.Has(...)` / `Status.Power(...)`.
6. Không cần sửa hàm giải debuff: `Cleanse` tự gỡ mọi mã.
