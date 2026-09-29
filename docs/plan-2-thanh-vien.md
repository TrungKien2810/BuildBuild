# Plan — 2 thành viên: commit asset mới, nối vào game, code cơ chế Lính + Trụ

> Lập 2026-09-25. Nguồn: `GAME_DESIGN.md` (mục 2, 3, 4, 5), `docs/project-state/IDEAS.md` #1, `HANDOFF.md`.
> Con số nào GDD ghi "chưa cân bằng" / "chưa chốt" thì plan này **dùng số tạm, đặt trong ScriptableObject**, không hard-code.

Hai người, gọi tắt:

- **A — Lính:** hệ lính triệu hồi + luật chặn quái (giữ tối đa 3) + nối art lính và Chainwarden.
- **B — Trụ:** hệ pháo trên pháo đài + nối art pháo, quái mới, Boss, Vespera.

---

## 0. Asset + `.meta` — ĐÃ XONG (2026-09-25)

Mọi asset mới và `.meta` của chúng (Unity import qua `GeneratedArtImporter`) đã có trên `main` (commit `5992584`).
Hai người chỉ cần **pull `main` TRƯỚC khi mở Unity**. Nếu `git status` sau khi mở Unity thấy `.meta` trong
`Assets/Art/Generated/` bị sửa, đừng commit mà hỏi lại nhau trước: sửa như vậy là GUID/pivot bị đổi.

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

> **Tiến độ nhánh `feat/linh` (2026-09-26): đã code A1 + A2 + A3 + A4 + lính vào gacha.** Chưa mở Unity trên máy
> làm (chỉ type-check C# bằng reference assembly Unity 2022.3, cả nhánh riêng lẫn bản merge thử với `feat/tru`:
> merge tự động không xung đột, compile sạch). Asset / scene KHÔNG commit — theo mục 1, mở Unity chạy
> `BuildBuild/Scenes/Build Battle + Gacha` để sinh `Assets/Data/Soldiers/`, `Anim_WarWolf/StoneTortoise/AcidLizard/Chainwarden`,
> `Hero_Chainwarden`, `Warden_Chainwarden`, `SummonConfig`, `Units/Unit_soldier_*` rồi commit kèm `.meta`.
> Script mới đã có `.meta` (GUID tự tạo) trong cùng commit.
>
> **Thử nhanh:** Admin panel (F1) có nút "Linh: day vong hoi chieu" (chỉ bấm được lúc đang đánh); muốn đủ nhân vật thì đổi sang tài khoản "Test (du nv)".
> A1: để mỗi Ironblade trên sân, đánh wave 1 (đúng 5 Goblin) — chọn Ironblade trong Hierarchy, Scene view vẽ Gizmos
> chỗ vây (xanh = trống, đỏ = có quái).
>
> Điểm chạm file chung — **B đọc trước khi merge**:
> - `Enemy.cs`: vây đánh chuyển sang `Blocker` (`Blocker.cs` mới). **Giữ nguyên `Enemy.EngagedHero`** (trả `Hero`) cho
>   `EnemyExtras`. Quái bắn xa (`attackRange` > 1,5, vd Goblin Ná 4) **không bị giữ, không chiếm chỗ**: vẫn đặt
>   `EngagedHero` = unit gần nhất trong `heroAggroRange` và đứng xa bắn. Thêm `Holder`, `CanBeHeld`, `HoldBy()`,
>   `ApplyRoot()`, `ApplyChain()`, `PullTo()`, `IsRooted`, `ChainBonus`; hai lời gọi `March()` trong `Update` đổi thành
>   `Walk()` (trói thì đứng). Không đụng `March`/`Advance`/`Attack` (chỗ B đọc `MoveSpeed` / `AttackRateMultiplier`).
> - `Hero.cs`: bỏ `EngageSlots` / `HasFreeSlot` / `ClaimSlot` (chuyển vào `Blocker`); `Initialise` gắn `Blocker` và
>   `Warden`; switch `FinishCast` thêm `case HookThrow` ở đầu.
> - `Health.cs`: thêm `Vulnerability` (Xích), nhân ở **đầu** `TakeDamage` — không đặt cạnh dòng `Armor` của B để git
>   không báo xung đột; nhân giao hoán nên kết quả như nhau.
> - `HeroDefinition`: header mới "Chan quai" sau `leashRange` (`maxHeld`, `heldDamageBonus`, `warden`); `SkillKind.HookThrow` cuối enum.
> - `UnitCategory.Soldier` cuối enum. `GachaConfig.soldierShare` + `GachaSystem` quay 3 loại.
> - `SceneBuilder`: thêm `BuildSoldiers()`, `BuildChainwarden()`, `BuildSoldierCamp()` (+ hàm con), 3 dòng gọi; bảng chọn
>   hero loại thư mục `Data/Soldiers/` (SoldierDefinition kế thừa HeroDefinition).
> - `Battlefield.OnField()`; `AdminPanel` thêm 1 nút (sau "Giet het quai").
>
> Số tạm A tự chọn (chưa có trong plan/GDD): giá + hồi chiêu lính — Sói 30 / 12s, Rùa 40 / 18s, Kỳ nhông 35 / 15s ·
> đầu wave vòng hồi chiêu đầy sẵn · gacha hero 60% / lính 20% / pháo 20% · độ hiếm 3 lính tạm N · Chainwarden tốc chạy 1,0 ·
> Quật Xích 10 dmg · Ném Móc tầm 6, kéo 0,3s, choáng 0,8s, không có quái bắn xa thì kéo con xa nhất chưa bị mình giữ ·
> quái bị Xích tô tím nhạt · lính và Chainwarden tạm dùng ảnh `identity` làm avatar/icon.

#### A — việc CÒN LẠI (cập nhật 2026-09-26)

Code A1–A4 + gacha đã có trên `feat/linh`, nhưng **chưa mục nào được chạy trong Unity** — chưa mục nào coi là "Xong khi".

**Cần Unity (làm trước):**

- [ ] Pull `feat/linh`, mở Unity 6000.4, xem Console không có lỗi compile (mới type-check bằng Unity 2022.3, có thể lệch API).
- [ ] Chạy `BuildBuild/Scenes/Build Battle + Gacha` → commit asset sinh ra **kèm `.meta`** trong cùng commit:
      `Assets/Data/Soldiers/`, `Anim_WarWolf/StoneTortoise/AcidLizard/Chainwarden`, `Hero_Chainwarden`, `Warden_Chainwarden`,
      `SummonConfig`, `Units/Unit_soldier_*`, `GachaConfig` (đã nối lính vào pool), scene `Battle.unity` / `Gacha.unity`.
- [ ] Nghiệm thu **A1**: 5 Goblin vào 1 Ironblade → đúng 3 con đứng đánh, 2 con đi tiếp; giết 1 con bị giữ → 1 con đang đi vào thế chỗ;
      Ironblade chết → quái được thả, đi tiếp.
- [ ] Nghiệm thu **A2**: 3 lính đúng hướng nhìn, đúng scale, animation attack ra đòn đúng frame (`hitFrame` Sói 3, Rùa 2, Kỳ nhông 2 —
      đo bằng alpha, cần nhìn lại); đạn axit Kỳ nhông bay đúng cỡ (`projectileScale` tạm 1).
- [ ] Nghiệm thu **A3**: bãi chờ nằm đúng sau thành (toạ độ `CampXPx` / `CampRowsPx` trong `SceneBuilder` là ước lượng, chưa nhìn trên nền thật);
      kéo thả ra sân sinh đúng tốp; kéo ra ngoài sân / thiếu tài nguyên thì không ra gì; thanh tài nguyên chạy; hết wave dọn sạch lính.
- [ ] Nghiệm thu **A4**: Chainwarden giữ được 4; quái bị Xích tô tím và ăn thêm 20% (kể cả đạn pháo sau khi merge B);
      Quật Xích trói (đứng im vẫn đánh); Ném Móc kéo Goblin Ná về, choáng, Xích 4s.
- [ ] Gacha: quay ra được lính, thẻ hiện icon lính.

**Việc chung với B (lịch mục 4):**

- [ ] Merge `feat/tru` + `feat/linh` vào `main` (đã merge thử: không xung đột, compile sạch). Merge xong **chạy lại builder** —
      scene `Battle.unity` B đã commit là bản chưa có bãi lính / thanh tài nguyên / Chainwarden.
- [ ] Chạy thử chung chặn + pháo, wave 1–12, rồi wave 1–30; chỉnh số trong ScriptableObject.
- [ ] Cập nhật `HANDOFF.md`.

**Còn dở trong code A:**

- [ ] "Tinh anh bị trói / choáng một nửa": `main` chưa có tinh anh. Sau merge B (`EnemyKind.TinhAnh`) sửa `Warden.Sweep` và
      `Warden.ThrowHook` từ `enemy.IsBoss` thành boss **hoặc** tinh anh.
- [ ] Quái vừa bị Xích vừa bị làm chậm: `EnemyExtras` của B tô màu chậm trong `LateUpdate` đè mất màu tím Xích (chỉ là hình, sát
      thương vẫn đúng). Chốt thứ tự ưu tiên màu với B.
- [ ] FX Quật Xích (chưa có art) và Ném Móc (`hook-throw-body`, `hook-fx` chờ duyệt): duyệt xong gán vào `Warden_Chainwarden`
      (`sweepFrames`, `hookFrames`) và clip `skill` của `Anim_Chainwarden`.
- [ ] Avatar / icon riêng cho 3 lính + Chainwarden (đang tạm dùng ảnh `identity`).
- [ ] Bãi chờ chưa lọc theo lính **đã sở hữu** (hiện mang cả 3 loại vào trận); bảng chọn hero cũng chưa lọc theo sở hữu.
- [ ] Chốt các số tạm ở mục 6 (câu 1, 2, 7, 8) rồi sửa trong ScriptableObject.

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
- **Không có tài nguyên (người dùng chốt 2026-09-26)** — bỏ bản tạm "+1/giây, +2/kill, trần 100". Chỉ vòng hồi chiêu: **đầu wave vòng rỗng**, đầy dần trong lúc đánh (`summonCooldown`), thả xong về rỗng. Chỉ lính **đã sở hữu** (quay gacha ra) mới hiện ở bãi.
- Vòng đầy → **kéo thả** ra sân → sinh **nguyên tốp** quanh điểm thả (lệch theo y, dùng `Battlefield.Clamp`).
- **Hết wave → dọn sạch lính** (như lính xương).
- Lính **có trong gacha** (IDEAS #1) → thêm vào `GachaConfig` sau khi B và A đã merge `UnitCategory`.

### A4. Nối Chainwarden (hero SR, kit đã chốt 2026-09-24)

Chainwarden sống chung với luật chặn nên để A làm:

- `Hero_Chainwarden.asset`: 1000 máu / 18 dmg / 0,7 đòn/s / tầm 0,6 / mana 5 (số tạm GDD).
- Nội tại *Cai Ngục*: `Blocker.maxHeld = 4`; quái đang bị giữ mang trạng thái **Xích** = nhận **+20% sát thương từ mọi nguồn** (cài ở `Health`, để pháo của B cũng hưởng).
- *Quật Xích* (cooldown ẩn ~10s): bán kính ~1,0, sát thương thấp + **trói 1,5s** (đứng im, vẫn đánh); tinh anh/boss trói một nửa.
- *Ném Móc* (mana): kéo quái tầm xa (ưu tiên `GoblinSlinger`) về sát mình + choáng ngắn + Xích 4s. Art `hook-throw-body` / `hook-fx` **đang chờ duyệt** → làm cuối, hoặc để sau.
  **Đổi 2026-09-26:** Ném Móc thành mở đầu cho biến hình **Cuồng Nộ 8s** (GDD mục 3) — code đã có trong `Warden` (`CastUltimate`, `OffhandStrike`, hút máu qua `Health.DamageDealt`), số ở `Warden_Chainwarden` (`rage*`, `storm*`).

---

## 3. Thành viên B — Trụ (pháo) + asset quái/hero còn lại

> **Tiến độ nhánh `feat/tru` (2026-09-25): đã code B1 + B2 + B3.** Asset / scene KHÔNG commit — theo mục 1, merge xong
> mới chạy `BuildBuild/Scenes/Build Battle + Gacha` để sinh `Assets/Data/Cannons/`, `Enemy_GoblinSlinger/Gatebreaker/Boss`,
> `Boss_Minotaur`, `Hero_Vespera` và dựng lại scene. Admin panel (F1) có nút "So huu ca 3 phao" để thử bệ pháo.
>
> Điểm chạm file chung — **A đọc trước khi merge**:
> - `Enemy.cs` (của A): B **chỉ thêm** hệ số tốc qua hàm public (mục B1.4): `ApplySlow()`, `ApplyHaste()`, `IsSlowed`,
>   `SlowImmune`, `AttackRateMultiplier`; hai dòng di chuyển đọc `MoveSpeed` thay `_definition.moveSpeed`, dòng cooldown đòn
>   nhân `AttackRateMultiplier`. Không đụng luật đuổi / vây / chặn.
> - Phần còn lại của B ở component riêng `EnemyExtras` (giáp qua `Health.Armor`, viên đá Goblin Ná, gắn `Boss`, tô màu
>   khi bị chậm), `WaveManager.Spawn` gắn sau `Initialise`. Goblin Ná đánh bằng luật của A: `attackRange` 4,
>   `heroAggroRange` 4,5 → đứng xa đánh, hero/lính trong tầm được chọn trước tường; sát thương ra ở frame ném, viên đá
>   chỉ để nhìn. `EnemyDefinition.IsRanged` có sẵn nếu A muốn `Blocker` không giữ quái bắn xa.
> - Boss **không** chạy clip thân `slam` / `roar` (Enemy.Update tự chọn clip mỗi frame nên clip riêng bị cắt): Chấn Địa /
>   Tiếng Thét chỉ hiện bằng FX. Muốn có clip thân thì cần A thêm một móc "đang cast" trong Enemy.Update.
> - Gatebreaker thiếu death: clip death tạm = walk (Enemy chờ hết clip death mới báo chết).
> - `Hero` thành `partial`; phần của B ở `Hero.Support.cs` (`ApplyStun`, hero hồi máu, FX buff). Móc trong `Hero.cs`:
>   `UpdateStunned()`, nhánh `healsAllies`, `ClearSupport()` trong `OnDied`.
> - `Health`: thêm `Armor` (giảm sát thương, nhân trước khi trừ máu) + `Heal()`. Hệ số Xích +20% của A đặt cạnh dòng `Armor`.
> - `SaveData.cannons`; `WaveBand.fixedCount/eliteCount`, `WaveTuning.boss/elite`; `EnemyKind` thêm `TinhAnh`, `BanXa` ở cuối enum.
> - `SceneBuilder`: chỉ thêm hàm `BuildNewEnemies()`, `BuildVespera()`, `BuildCannons()`, `BuildCannonBoard()` (+ hai hàm
>   con) và mỗi hàm một dòng gọi (3 dòng trong `BuildAll`, 1 dòng cuối `BuildBattleScene`).
>
> Số tạm B tự chọn (chưa có trong GDD): bán kính Chấn Địa 2,0 · Goblin Ná 0,8 đòn/s · Gatebreaker 0,6 đòn/s · boss 0,5 đòn/s ·
> Goblin Ná xuất hiện từ wave 7 (thế chỗ Orc chưa có art), wave 20 dùng 4 Goblin thay 4 Orc · pháo Nổ bán kính 2,0 (hiểu "2 ô" = 2 đơn vị).

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
| 1 | Pull `main` · thêm `UnitCategory.Soldier`, push | Pull `main` · đọc `Enemy`/`Projectile`/`Castle` |
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

1. ~~Tài nguyên triệu hồi lính~~ — chốt 2026-09-26: không có tài nguyên, chỉ vòng hồi chiêu.
2. Cỡ tốp + chỉ số 3 lính.
3. Pháo đổi được giữa wave hay chỉ giữa các wave.
4. Chỉ số + wave xuất hiện của Goblin Ná.
5. Kit Vespera.
6. Ai sinh art Pháo Nổ / Pháo Băng và Gatebreaker attack/hurt/death.
7. Lính trong gacha: tỉ lệ ra lính (đang tạm 20%, lấy từ phần của pháo), độ hiếm từng lính; giá + hồi chiêu triệu hồi.
8. Quái bắn xa có bị giữ không (đang: không bị giữ, không chiếm chỗ của unit).
