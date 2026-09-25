# Plan — 2 thành viên: commit asset mới, nối vào game, code cơ chế Lính + Trụ

> Lập 2026-09-25. Nguồn: `GAME_DESIGN.md` (mục 2, 3, 4, 5), `docs/project-state/IDEAS.md` #1, `HANDOFF.md`.
> Con số nào GDD ghi "chưa cân bằng" / "chưa chốt" thì plan này **dùng số tạm, đặt trong ScriptableObject**, không hard-code.

Hai người, gọi tắt:

- **A — Lính:** hệ lính triệu hồi + luật chặn quái (giữ tối đa 3) + nối art lính và Chainwarden.
- **B — Trụ:** hệ pháo trên pháo đài + nối art pháo, quái mới, Boss, Vespera.

---

## 0. Việc chung đầu tiên — sinh `.meta` cho asset mới (CHỈ 1 người làm, người kia chờ)

Asset mới **đã được commit** (2026-09-25, các commit `art: ...`) nhưng **chưa có `.meta`** vì lúc commit Unity không mở.
Các thư mục thiếu `.meta`: `AcidLizard/`, `Boss/`, `CannonRapid/`, `Chainwarden/`, `Gatebreaker/walk/`,
`GoblinSlinger/` (toàn bộ), `Hawkfletch/rapid-fire-buff/`, `StoneTortoise/`, `UI/style-b/`, `Vespera/`, `WarWolf/`.

Nếu **cả hai** cùng mở Unity trước khi có `.meta` trên git, mỗi máy sinh một GUID khác cho cùng một ảnh → prefab/asset
người này nối sẽ mất tham chiếu trên máy người kia. Vì vậy:

1. **A** pull `main`, mở Unity, chờ import xong.
2. Menu **BuildBuild → Art → Reimport Generated Art**, rồi **BuildBuild → Art → Verify Generated Art Import** (không được báo lỗi).
3. `git status` — chỉ thấy file `.meta` mới trong `Assets/Art/Generated/` (không có PNG nào bị sửa).
4. Commit `art: thêm .meta cho asset mới` + push.
5. **B chỉ mở Unity SAU KHI đã pull commit này.** Lỡ mở trước thì xoá các `.meta` Unity vừa sinh trong những thư mục trên rồi pull lại.

> Chưa có art `CannonBlast` / `CannonFrost` trong `Assets/Art/Generated/` → B dùng tạm sprite `CannonRapid` tô màu (xem mục B).

---

## 1. Hiện trạng code (đọc trước khi chia việc)

| Có rồi | Chưa có |
|---|---|
| `Hero`, `Enemy`, `Health`, `Knockback`, `Projectile`, `FrostZone` | **Không có script pháo nào** — pháo chỉ tồn tại như `UnitDefinition` gacha (`Unit_cannon_*.asset`) |
| `Necromancer` + lính xương/kỵ sĩ dùng chung prefab Hero | **Không có hệ lính triệu hồi** (bãi chờ, tài nguyên, kéo thả) |
| `Castle` (máu + đổi sprite theo máu) | **Luật chặn "giữ tối đa 3 quái"** — hiện quái chỉ rẽ sang đánh unit gần nhất, không giới hạn |
| `SceneBuilder` (sinh data asset + scene), `GeneratedArtImporter` | `UnitCategory` chỉ có `Hero, Cannon` — thiếu `Soldier` |

**File dễ đụng nhau** → chia chủ sở hữu:

| File | Chủ | Người kia muốn sửa thì |
|---|---|---|
| `Assets/Scripts/Combat/Enemy.cs` | **A** (luật chặn) | B chỉ thêm hàm nhận slow/AOE qua `Health`/component riêng, không sửa logic di chuyển |
| `Assets/Editor/SceneBuilder.cs` | chia theo **hàm**: A viết `BuildSoldiers()`, B viết `BuildCannons()` / `BuildNewEnemies()` | Mỗi người chỉ thêm hàm của mình + 1 dòng gọi; merge xong mới chạy lại |
| `Assets/Scripts/Data/UnitDefinition.cs` | **A** thêm `UnitCategory.Soldier` ngay ngày 1, push sớm | B pull trước khi đụng gacha |
| Scene `Battle.unity` | **Không sửa tay** — sinh lại bằng `SceneBuilder` | Ai cần thay đổi scene thì sửa code builder |

---

## 2. Thành viên A — Lính triệu hồi + luật chặn

### A1. Luật chặn quái (làm TRƯỚC — lính vô dụng nếu không chặn)

Luật đã chốt (GDD mục 2, IDEAS #1):

- Mọi unit phe ta trên sân (hero, lính xương, kỵ sĩ, lính triệu hồi) chặn quái. **Mỗi unit giữ tối đa 3 quái**; Chainwarden giữ **4**.
- Mỗi quái bị giữ đứng **một ô cạnh unit chặn**, lệch nhau theo trục y (không chồng lên nhau).
- Quái thứ 4 trở đi **không bị giữ**, đi tiếp về lâu đài.
- Quái bị giữ chết → con **gần nhất chưa bị giữ, trong tầm** vào thế chỗ. Báo **ngay lúc chết** (`Health.Died`), không chờ hết animation death.
- Unit chặn chết → thả hết quái nó đang giữ.

Gợi ý cài: component `Blocker` gắn trên unit phe ta (`maxHeld`, danh sách quái đang giữ, cấp slot theo offset y);
`Enemy` hỏi `Blocker` còn slot không trước khi dừng lại đánh. Số `maxHeld` đọc từ `HeroDefinition` (mặc định 3).

**Xong khi:** thả 5 Goblin vào 1 Ironblade → đúng 3 con đứng đánh, 2 con đi tiếp; giết 1 con bị giữ → 1 con đang đi vào thế chỗ.

### A2. Data + nối art lính

1. Thêm `UnitCategory.Soldier` (push sớm, báo B).
2. `SoldierDefinition` (ScriptableObject): chỉ số (máu, dmg, tốc đánh, tầm, tốc chạy), `SpriteAnimationSet`, cỡ tốp, hồi chiêu, chi phí tài nguyên.
3. `SceneBuilder.BuildSoldiers()` sinh `Anim_*.asset` + `Soldier_*.asset` cho 3 lính đã có art:

| Lính | Vai | Art | Số tạm (Claude đề xuất, CHƯA cân bằng) |
|---|---|---|---|
| Sói chiến `WarWolf` | cận chiến nhanh | đủ 6 action | 200 máu · 14 dmg · 1,2 đòn/s · tầm 0,5 · tốc 2,0 · tốp 3 |
| Rùa đá khiên `StoneTortoise` | tank chậm | đủ 6 action | 700 máu · 8 dmg · 0,5 đòn/s · tầm 0,6 · tốc 0,6 · tốp 2 |
| Kỳ nhông axit `AcidLizard` | bắn xa | action + `acid`, `acid-impact` | 150 máu · 18 dmg · 0,8 đòn/s · tầm 4,0 · tốc 1,2 · tốp 3 |

Lính dùng lại luật di chuyển của hero (đuổi quái gần nhất; tầm xa có dây xích), và gắn `Blocker` (giữ 3).

### A3. Cơ chế triệu hồi trong trận

Theo GDD mục 2 + IDEAS #1:

- **Bãi chờ sau thành** (trái pháo đài): mỗi loại lính đang mang vào trận hiện 1 icon/sprite, **vòng hồi chiêu trên đầu**.
- Tài nguyên triệu hồi **tích theo thời gian + theo số quái bị giết**. Tên tài nguyên + tốc độ tích **chưa chốt** → tạm: +1/giây, +2/mỗi kill, trần 100 (số trong ScriptableObject).
- Vòng đầy + đủ tài nguyên → **kéo thả** ra sân → sinh **nguyên tốp** quanh điểm thả (lệch theo y, dùng `Battlefield.Clamp`).
- **Hết wave → dọn sạch lính** (như lính xương). Tài nguyên có giữ qua wave không: **chưa chốt**, tạm reset về 0.
- Lính **có trong gacha** (IDEAS #1) → thêm vào `GachaConfig` sau khi B và A đã merge `UnitCategory`.

### A4. Nối Chainwarden (hero SR, kit đã chốt 2026-09-24)

Chainwarden sống chung với luật chặn nên để A làm:

- `Hero_Chainwarden.asset`: 1000 máu / 18 dmg / 0,7 đòn/s / tầm 0,6 / mana 5 (số tạm GDD).
- Nội tại *Cai Ngục*: `Blocker.maxHeld = 4`; quái đang bị giữ mang trạng thái **Xích** = nhận **+20% sát thương từ mọi nguồn** (cài ở `Health`, để pháo của B cũng hưởng).
- *Quật Xích* (cooldown ẩn ~10s): bán kính ~1,0, sát thương thấp + **trói 1,5s** (đứng im, vẫn đánh); tinh anh/boss trói một nửa.
- *Ném Móc* (mana): kéo quái tầm xa (ưu tiên `GoblinSlinger`) về sát mình + choáng ngắn + Xích 4s. Art `hook-throw-body` / `hook-fx` **đang chờ duyệt** → làm cuối, hoặc để sau.

---

## 3. Thành viên B — Trụ (pháo) + asset quái/hero còn lại

### B1. Hệ pháo trên pháo đài

Luật (GDD mục 2 + 4):

- Pháo đài đá dọc mép trái có **3 bệ pháo xếp dọc**; mỗi bệ lắp **1 pháo tự bắn**.
- Lắp/gỡ pháo **giữa các wave** (giả định, giống hero — GDD không nói rõ; hỏi lại nếu cần đổi giữa wave).
- Pháo nhắm quái trong tầm (tạm: con **gần lâu đài nhất**), không có máu, không bị đánh.
- Level: `+8% sát thương` mỗi level, giá `50 × n^1.5` (GDD mục 7).

| Pháo | Sát thương | Tốc bắn | Tầm | Đặc điểm | Art |
|---|---|---|---|---|---|
| Liên Thanh | 12 | 2,0/s | 8 | đơn mục tiêu | ✅ `CannonRapid/` (identity, idle, attack, bullet, bullet-impact) |
| Nổ | 30 | 0,5/s | 7 | **AOE** bán kính 2 ô — mọi quái trong vùng nổ đều ăn đòn (người dùng chốt 2026-09-24) | ❌ chưa có — tạm dùng sprite Liên Thanh tô cam |
| Băng | 8 | 1,0/s | 7 | làm chậm 40% trong 3s | ❌ chưa có — tạm tô xanh |

Gợi ý cài:

1. `CannonDefinition` (ScriptableObject): 4 cột bảng trên + `aoeRadius`, `slowPercent`, `slowDuration`, `SpriteAnimationSet`, frame đạn/impact.
2. `Cannon.cs` (MonoBehaviour): tìm mục tiêu, bắn theo nhịp, chơi `attack`, sinh `Projectile` (dùng lại class có sẵn).
3. `CannonMount.cs` ×3 trên `Castle`: vị trí bệ + pháo đang lắp.
4. Slow: thêm hệ số tốc chạy ở `Enemy` **qua một hàm public** thống nhất với A (A giữ file). Boss dưới 30% máu **miễn slow**.
5. UI chọn pháo cho bệ: giống `HeroSlot` (chạm bệ → chọn pháo đang sở hữu).

**Xong khi:** lắp đủ 3 loại lên 3 bệ, đánh wave 4: Liên Thanh bắn liên tục, Nổ trúng cụm thì nhiều con cùng mất máu, quái trúng Băng đi chậm rõ.

### B2. Nối quái mới

| Asset | Việc | Số |
|---|---|---|
| `GoblinSlinger` (+ `stone`, `stone-impact`) | `Enemy_GoblinSlinger.asset`; **ưu tiên bắn hero/lính phe ta trong tầm, hết mục tiêu mới bắn tường** | chưa chốt → tạm 80 máu · 12 dmg · tốc 1,3 · tầm 4 · 6 vàng |
| `Gatebreaker` (identity + walk mới) | tinh anh: giảm 15% sát thương nhận; từ wave 12 +1 con, wave 21+ +2, không ra ở wave 10/20/30 | 1200 / 60 / 0,7 / 40 vàng (GDD) — **thiếu attack/hurt/death**, tạm dùng walk |
| `Boss` (idle, walk, attack, hurt, death, roar, slam + FX) | giáp −30% dmg · *Chấn Địa* 15s: 80 dmg + choáng hero 2s · *Tiếng Thét* 20s: +30% tốc chạy mọi quái 5s · *Nổi Giận* <30% máu: +50% tốc đánh, miễn slow | 5000 / 120 / 0,6 / 200; wave 20 ×2, wave 30 ×3,5 |

Cập nhật `WaveTuning` theo bảng thành phần wave GDD mục 5.

### B3. Hero R `Vespera` + FX buff Hawkfletch

- `Hero_Vespera.asset` + đòn đánh = **hồi máu đồng minh** thấp máu nhất trong tầm (dùng `heal-fx`). Kit + chỉ số **chưa chốt** → tạm 350 máu, hồi 20/đòn, 0,8 đòn/s, tầm 4. Chiêu lớn *Đèn Canh* chưa có FX → để sau.
- `Hawkfletch/rapid-fire-buff`: thay việc tô cam hero bằng FX 4 frame trên đầu trong 5s buff.

---

## 4. Lịch 2 tuần (gợi ý)

| Ngày | A | B |
|---|---|---|
| 1 | Mục 0 (sinh + commit `.meta`) · thêm `UnitCategory.Soldier`, push | Pull commit `.meta` rồi mới mở Unity · đọc `Enemy`/`Projectile`/`Castle` |
| 2–4 | A1 luật chặn | B1 `CannonDefinition` + `Cannon` + 3 bệ, Liên Thanh bắn được |
| 5 | **Merge + chạy thử chung** (chặn + pháo cùng lúc) | ← |
| 6–8 | A2 data lính + A3 bãi chờ / tài nguyên / kéo thả | B1 Nổ (AOE) + Băng (slow), UI chọn pháo · B2 GoblinSlinger, Gatebreaker |
| 9 | **Merge + chạy thử wave 1–12** | ← |
| 10–12 | A4 Chainwarden · lính vào gacha | B2 Boss (3 cơ chế) · B3 Vespera + FX Hawkfletch |
| 13–14 | Chạy thử wave 1–30, sửa số trong ScriptableObject, cập nhật `HANDOFF.md` | ← |

## 5. Quy tắc làm chung

- Mỗi người một nhánh (`feat/linh`, `feat/tru`), merge vào `main` ở các mốc ngày 5 / 9 / 14; rebase trước khi merge.
- **Không commit** `Library/`, `Temp/`, file scene sinh ra do bấm thử; scene chỉ sinh lại qua `SceneBuilder`.
- Asset mới luôn commit **kèm `.meta`** trong cùng commit.
- Số chỉ số để trong ScriptableObject, không hard-code trong script.
- Commit message không ghi tên Claude / Co-Authored-By.

## 6. Câu hỏi cần chủ dự án chốt (đang dùng số tạm)

1. Tài nguyên triệu hồi lính: tên, tốc độ tích, có giữ qua wave không.
2. Cỡ tốp + chỉ số 3 lính.
3. Pháo đổi được giữa wave hay chỉ giữa các wave.
4. Chỉ số + wave xuất hiện của Goblin Ná.
5. Kit Vespera.
6. Ai sinh art Pháo Nổ / Pháo Băng và Gatebreaker attack/hurt/death.
