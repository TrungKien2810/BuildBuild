# Âm thanh: thêm, đổi và gắn tiếng vào game

Bài này dạy bạn tự thêm một tiếng mới, đổi tiếng của một hero/pháo, và hiểu code âm thanh chạy ra sao.

## 1. File tiếng nằm ở đâu

Mọi tiếng nằm trong `Assets/Resources/Audio/`:

| Thư mục | Dùng cho | Ví dụ |
|---|---|---|
| `SFX/` | tiếng ngắn: đòn đánh, nút bấm, gacha | `hit-melee.ogg`, `ui-click.ogg` |
| `Jingles/` | nhạc ngắn thắng/thua | `jingle-win.ogg` |
| `Music/` | nhạc nền lặp | `music-battle.mp3`, `music-conquest.mp3`, `music-lobby.mp3` |

**Tên file (bỏ đuôi) chính là tên code gọi.** Vì sao để trong `Resources`: code nạp file theo tên lúc chạy
(`Resources.Load`), nên không phải kéo thả tham chiếu vào scene — scene dựng lại bằng `SceneBuilder` cũng không mất.

`SOURCES.md` trong cùng thư mục ghi file gốc và nguồn của từng tiếng (đều CC0 — dùng thương mại, không phải ghi công).

## 2. Đổi một tiếng có sẵn

1. Chuẩn bị file mới (`.ogg`, `.wav` hoặc `.mp3`), đặt **đúng tên** file cũ, ví dụ `enemy-die.ogg`.
2. Chép đè vào `Assets/Resources/Audio/SFX/` (Unity giữ nguyên `.meta`).
3. Mở Unity, bấm Play. Không phải sửa code.
4. Thêm dòng nguồn + license vào `SOURCES.md`.

Nguồn tiếng miễn phí CC0: kenney.nl (Audio), opengameart.org (lọc License = CC0), freesound.org (bộ lọc License: Creative Commons 0).

## 3. Gán tiếng riêng cho một hero, lính hoặc pháo

1. Mở `Assets/Data/Hero_<Tên>.asset` (lính: `Data/Soldiers/`, pháo: `Data/Cannons/`).
2. Trong Inspector, mục **Am thanh**, gõ tên tiếng (không đuôi):
   - `Fire Sfx` — lúc ra đòn thường (nhả dây cung, tung phép). Hero cận chiến để trống: đòn trúng đã có tiếng chung.
   - `Impact Sfx` — lúc đạn trúng đích (chỉ hero có đạn bay, và pháo).
   - `Skill Sfx` — lúc tung chiêu lớn (chỉ hero).
3. Để trống = im. Gõ sai tên cũng im, không báo lỗi — nếu không nghe thấy, kiểm lại chính tả với tên file.

Ví dụ đang dùng: Hawkfletch `shot-bow` / `hit-arrow`; Frostweaver trúng `frost`, chiêu `spell`; Pháo Nổ trúng `explode`.

## 4. Gọi tiếng từ code

```csharp
using BuildBuild.Core;

Sfx.Play("coin");              // tiếng ngắn trong SFX/ (hoặc Jingles/)
Sfx.Music("music-battle");     // đổi nhạc nền; gọi lại cùng bài thì không phát lại từ đầu
```

## 5. Code chạy ra sao bên trong

**`Assets/Scripts/Core/Sfx.cs`**

- Lần đầu vào game tạo một object `Audio` sống xuyên scene, gồm 16 `AudioSource` cho tiếng ngắn + 1 cho nhạc (âm lượng 0,4).
- `Sfx.Play(tên)`:
  1. Cùng tên vừa phát chưa đủ 60 ms → bỏ (wave đông có hàng chục đòn trúng cùng lúc, không gom thì thành một tiếng rè).
  2. Nạp clip `Audio/SFX/<tên>`, không có thì thử `Audio/Jingles/<tên>`; nạp xong giữ trong bộ nhớ đệm.
  3. Tìm kênh đang rảnh; hết kênh thì bỏ tiếng mới (không cắt tiếng đang kêu).
  4. Đặt âm lượng theo bảng `Gain` (tiếng nghe nhiều như chém/quái chết để 0,5–0,6), lệch pitch ngẫu nhiên ±6% để một file lặp lại không nghe máy móc (jingle thì không lệch).
- Mỗi frame, `SfxRunner`:
  - chọn nhạc: scene đang mở không phải `Battle` (tức Gacha / Units) → `music-lobby`; trong `Battle` mà đang có trận Chinh Phạt (`ConquestMatch.Current`) → `music-conquest`, còn lại `music-battle`;
  - khi bạn thả chuột/ngón tay: bắn tia UI tại điểm đó, gặp `Button` → nút bấm được thì `ui-click` (tên nút có Close/Cancel/Back thì `ui-back`), nút xám thì `ui-error`. Bắt chung ở đây vì thẻ gacha / thẻ đội quân là bản clone lúc chạy — gắn tiếng vào từng nút sẽ sót.
  - khi rê **chuột** (không áp cho cảm ứng): bắn tia UI tại con trỏ, nút dưới con trỏ đổi sang một `Button` bấm được → `ui-hover`. Đứng yên trên cùng nút không kêu lại.

**`Assets/Scripts/Combat/CombatAudio.cs`** nghe sự kiện `Health.AnyDamaged` (mọi đòn trúng trong game):

- mục tiêu là thành → `castle-hit`;
- kẻ đánh là hero/lính **cận chiến** → `hit-melee`; quái cận chiến → `hit-blunt`;
- đòn bắn xa và pháo bị bỏ qua ở đây — `Projectile` tự phát `Impact Sfx` lúc đạn chạm, để một mũi tên không kêu hai lần.

Các điểm khác gọi `Sfx.Play` trực tiếp: `Hero` (ra đòn / tung chiêu, gục → `hero-die`), `Cannon` (khai hoả), `Enemy` (chết → `enemy-die`),
`EnemyShot` (quái bắn xa nhả đạn → `enemy-shot`), `Boss` (gầm → `boss-roar`, đập đất → `boss-slam`),
`HeroBoard` / `SoldierBoard` / `CannonBoard` (chạm ô trống mở bảng chọn → `ui-open`, chọn quân vào ô → `unit-place` — hero kêu thêm `hero-pick`, chạm ô đang có quân để gỡ → `unit-remove`),
`ConquestMapScreen` (mở bản đồ → `ui-open`), `UnitsScreen` (lên cấp / nâng sao / nâng trang bị thành công → `upgrade`, mặc đồ → `equip`, tháo → `unequip`),
`WaveManager` (bắt đầu wave → `wave-start`), `ResultPopup` (thắng/thua; có ô trang bị rơi → `item-drop`), `BattleHud` (nhận rương AFK → `coin`),
`GachaScreen` (bấm quay → `gacha-roll`) và `GachaCardView` (mỗi thẻ úp bật ra → `gacha-deal`; SR/SSR lúc rung + hào quang trước khi lật → `gacha-charge`;
lật ra SSR → `gacha-ssr`, SR → `gacha-rare`, còn lại `gacha-card`).
