# Build Build — Game Design Document

> **Nguồn sự thật về thiết kế game.** Thay thế `project_overview_grow_castle_clone.md` (đã xoá).
> Mọi con số ở đây là giá trị khởi điểm để cân bằng, đặt trong ScriptableObject.
> Art direction: `docs/project-state/art-style.md` · Pipeline: `docs/project-state/art-pipeline.md`

Thể loại: Tower Defense + Gacha · Unity 2022 LTS / C# · PC (mở rộng mobile) · 8 tuần · 3 người

## 1. Vòng lặp

Chống wave → nhận vàng → gacha ra hero/pháo → nâng cấp level → wave khó hơn.

- **Đánh từng wave một:** người chơi bấm để đánh wave kế tiếp. Hết wave thì trận **dừng lại**, người
  chơi đi nâng cấp rồi mới bấm wave sau. Lâu đài hồi đầy máu trước mỗi wave.
- **Thua:** máu lâu đài về 0. Trận **dừng ngay tại wave đó**, không quay về wave 1. Người chơi phát
  triển thêm (xem tài nguyên ngoài trận ở mục 8) rồi đánh lại chính wave đó; **giữ toàn bộ** vàng +
  vật phẩm + level + bộ đếm pity.
- **Thắng wave:** diệt hết quái của wave, nhận thưởng để nâng cấp. **Không giới hạn wave** (người dùng chốt 2026-09-29):
  máu, sát thương, số lượng quái tiếp tục tăng theo công thức mục 5; **boss mỗi 10 wave**.
- Sau wave 30: wave thường dùng thành phần của 21–29 (hỗn hợp + 2 Gatebreaker), wave chia hết cho 10 dùng thành phần
  wave 30 (boss), hệ số boss +1,5 mỗi 10 wave (30 ×3,5 → 40 ×5 → 50 ×6,5…) — `WaveTuning.bossMultiplierPer10Waves`.

## 2. Cơ chế

| Cơ chế | Mô tả |
|---|---|
| Hero đặt vào ô | Màn trận có sẵn **9 ô** trên sân (dưới đất, trước lâu đài). Chạm ô trống → chọn hero đang sở hữu; chạm ô có hero → gỡ ra. Chỉ đổi hero giữa các wave. Ô là **ĐIỂM XUẤT HIỆN đầu wave**, không phải chỗ đứng bất khả xâm phạm: hero **có máu và bị quái đánh** như mọi thứ khác trên sân. Hết máu → hero chết và biến mất tới hết wave; đầu wave sau spawn lại đầy máu tại đúng ô đó. Các tầng thành chỉ để lắp pháo |
| Hero tự đi tìm quái (chốt 2026-09-15) | Hero **không đứng chờ**: luôn nhắm con quái gần nhất trên sân, trong tầm thì đứng đánh. **Cận chiến** (tầm ≤ 1,5: Cragfist, Ironblade) đuổi khắp sân. **Tầm xa** (Frostweaver, Hawkfletch, Bonecaller) chỉ đi tới khi vào tầm, và chỉ khi chỗ đứng bắn nằm trong **dây xích `leashRange` = 3 unit tính từ ô**; quái ngoài khoảng đó hoặc hết quái thì đi về ô. **Thắng wave → mọi hero về lại ô, đầy máu** (kể cả con đã chết). Thay luật cũ `aggroRange` (hero đứng yên khi quái ngoài tầm nhìn) |
| Chặn quái (chốt 2026-09-13) | **Mọi unit phe ta đứng trên sân — hero lẫn lính triệu hồi — đều chặn quái. Mỗi unit giữ tối đa 3 quái**; quái thứ 4 trở đi không bị giữ, đi tiếp. Thay luật cũ "hero không chặn quái" (2026-09-11). Chưa có trong code: `Enemy` hiện rẽ sang đánh hero gần nhất nhưng không giới hạn số quái trên một hero |
| Mặt đất có chiều sâu | Sân **phủ gần hết màn** theo bố cục game mẫu (chốt 2026-09-15, thay dải đất side-scroller): rìa cây trên/dưới, bãi đánh ở giữa, quái xuất hiện ở độ cao bất kỳ trong **gần hết chiều cao sân** (~y 220–930 px trên 1080), vẽ trước/sau theo vị trí. Toạ độ chính xác chốt sau khi nền mới được duyệt |
| Pháo trên tháp | Căn cứ là **pháo đài đá dọc mép trái** (chốt 2026-09-15) với **3 bệ pháo** xếp dọc trên mặt, mỗi bệ lắp 1 pháo tự bắn; quái đánh vào mặt tường bên phải. 9 ô hero giữ nguyên chỗ: trước mặt tường. **Lính triệu hồi chờ ở bãi đất sau thành** (trái pháo đài), vòng hồi chiêu trên đầu, đầy mới kéo thả ra sân (chốt 2026-09-15) |
| Gacha | Dùng vàng quay, ra hero hoặc pháo theo bảng tỉ lệ có pity |
| Nâng cấp level | Mỗi hero/pháo có level riêng, dùng vàng |
| Trùng lặp → mảnh | Quay trúng đồ đã có → nhận mảnh ghép, gom đủ nâng sao |

Lâu đài MVP: 3 tầng cố định, **HP 2000**. Nâng cấp lâu đài (tăng HP, mở tầng 4–5) là phạm vi mở rộng.

**Nâng cấp thành trì — người dùng chốt hướng 2026-10-01 (hệ thống thật trong meta, CHƯA code; số là TẠM, Claude
chưa cân):** thành có **3 cấp**, mỗi cấp đổi hẳn hình (fantasy đá xám-lam → thành ấn rune) và mở thêm bệ pháo.

| Cấp | Tên | Bệ pháo | HP thành | Giá (vàng) | Điều kiện mở |
|---|---|---|---|---|---|
| 1 | Đồn Đá Biên | 3 | 2000 | — | mặc định |
| 2 | Thành Đá Lam | 4 | 3000 | 3.000 | đã thắng wave 10 |
| 3 | Thành Ấn Rune | 5 | 4500 | 8.000 | đã thắng wave 20 |

- Mỗi cấp có 3 sprite (lành / hư 1 / hư 2), cùng vách phải + đáy để quái và 9 ô hero không phải dời.
- Nâng cấp làm ở sảnh giữa các wave, có hiệu lực từ wave sau; bệ mới trống, người chơi tự lắp pháo.
- **Chinh Phạt:** thành NPC dùng chung bộ sprite, lật gương + đổi màu cờ bằng code; cấp thành NPC theo độ khó
  (khớp 3 icon bản đồ `map-castle-npc-small/mid/large`).

## 3. Hero (4 vai trò, không trùng chức năng)

Cột **Máu** là số thật đang chạy trong game (`HeroDefinition.maxHealth`) — quái nhắm hero ở gần hơn là nhắm lâu đài,
nên đặt một hero 260 máu ở hàng ngoài cùng là một quyết định có giá.

| Hero | Vai trò | Máu | Sát thương | Tốc đánh | Tầm | Mana |
|---|---|---|---|---|---|---|
| Hiệp sĩ (Ironblade) | Cận chiến thiên sát thương | 900 | 30 | 0.9/s | 0.55 | 4 |
| Người đá (Cragfist) | Tank thuần, chịu đòn thay đồng đội | 1400 | 10 | 0.5/s | 0.6 | 6 |
| Cung thủ | Sát thương đơn mục tiêu | 220 | 45 | 1.2/s | 6.0 | 4 |
| Pháp sư nguyên tố | Sát thương diện rộng | 260 | 35 | 0.6/s | 5.0 | 6 |
| Pháp sư triệu hồi (Bonecaller) | Hỗ trợ, chặn đường | 300 | 15 | 0.7/s | 4.0 | 5 |

**Tầm cận chiến đo trên MẶT ĐẤT (sửa 2026-09-15):** tầm = tầm với của frame chạm đòn + nửa thân quái, chiều sâu y
tính gấp đôi. Tầm 1,0 cũ đánh trúng khi còn cách ~40 px không khí. Quái: Goblin 0,4 · Ashfang 0,45; lính xương 0,45 ·
xương lớn 0,65 · kỵ sĩ 0,7. Tầm xa vẫn đo thẳng trên màn. Sát thương ra ở frame chạm đòn của clip, không ở frame 1/cuối.

**Hiệp sĩ vs Người đá (chốt 2026-09-12):** Ironblade mang tên vai "tank" từ bản GDD đầu nhưng chỉ
số thật đọc ra là cận chiến thiên damage (sát thương 25, cao thứ ba roster). Tank THUẦN tách thành
hero riêng — Cragfist: máu gấp 1,75× Ironblade, sát thương 10 (thấp nhất roster), tốc đánh 0.5/s.
Chiêu lớn **Hoá Đá**: bất động 5s, miễn hoàn toàn sát thương, phản 20% sát thương nhận về kẻ đánh.
Bất động là cái GIÁ, không phải phụ phẩm — miễn sát thương mà vẫn di chuyển/đánh được thì không có
quyết định gì để chơi.

**Chiêu lớn Ironblade — Chém Quạt (code 2026-09-15, art `cone-sweep-body` + `cone-sweep-fx` đã có).**
Thanh mana 4 đầy (buff 2026-09-29 từ 800/25/0,8, mana 5, 3×) → một nhát chém vòng cung trước mặt: mọi quái trong 0,8 unit phía trước (bề ngang
±0,4 theo trục y — khớp bề rộng art `cone-sweep-fx`; bản đầu 2,0/±1,0 rộng gấp 3 vết chém) ăn **4× sát thương đòn thường** (120). *Số Claude đề xuất, CHƯA cân bằng*, gồm cả
tốc chạy 1,1 của Ironblade (GDD không ghi).

**Vespera (R, hồi máu) — kit chốt với người dùng 2026-09-28, số CHƯA cân.** Đòn thường hồi máu một đồng minh thấp máu
nhất trong tầm (đã code). Vì phẩm R nên chỉ có **MỘT chiêu, chiêu lớn *Đèn Canh*** (thanh mana; không chiêu phụ, nội tại *Cứu Nguy* ở dưới): đặt một vòng sáng
trên đất với **tâm là đồng minh thấp máu nhất** (vòng nhỏ, bán kính khoảng 1,5, bẹp 0,5 như `FrostZone`). Mọi hero
trong vòng được hồi máu và nhận một **khiên ảo NHỎ** chặn một lượng sát thương có hạn trong vài giây, bị đánh hết thì
khiên vỡ. Cast dùng lại clip attack. **Số tạm (code 2026-09-28, `SkillKind.Lantern`):** mana 5 (0,35/giây + 0,25 mỗi lần
hồi máu), bán kính 1,5, hồi 2× đòn thường (40), khiên 60 sát thương trong 5 giây, cast lại thì làm mới chứ không cộng dồn.
Vòng chỉ phủ hero kế bên CÙNG hàng (hàng trên/dưới cách 1,65, vòng bẹp chỉ vươn 0,75). FX: `Vespera/dencanh-ring`,
`shield-loop`, `shield-break`; hồi máu từng người dùng lại `heal-fx`.

**Nội tại hero bậc thấp (người dùng chốt 2026-09-29 theo đề xuất của Claude; số CHƯA cân).** "Chỉ một chiêu" của hero N/R
nghĩa là không có chiêu phụ — nội tại vẫn có, mở ở 1★ như mọi hero. Code: `HeroDefinition.passiveKind` + `passive*`, `Hero.Passive.cs`.

| Hero | Nội tại | Hiệu ứng |
|---|---|---|
| Ironblade (N) | *Truy Kích* | Đòn thường vào quái dưới 30% máu +50% sát thương — dọn quái Chém Quạt để sót |
| Cragfist (R) | *Khiêu Khích* | Quái trong bán kính 1,5 bỏ unit khác, vào chỗ giữ của ông (khi ông còn chỗ); quái bắn xa đổi mục tiêu sang ông |
| Hawkfletch (N) | *Nhắm Yếu Điểm* | Cứ phát thứ 4 chí mạng ×2 sát thương (tính cả lúc Bắn Nhanh) |
| Frostweaver (N) | *Vỡ Băng* | Quái đang bị làm chậm (Vùng Băng, Pháo Băng…) chết trong tầm đánh → nổ, quái trong bán kính 1,0 quanh xác ăn 50% đòn thường; nổ được dây chuyền |
| Vespera (R) | *Cứu Nguy* | Đồng minh dưới 30% máu được hồi +50% (cả đòn thường lẫn Đèn Canh) |

**Bộ hero N fantasy mới — kit ĐÃ CHỐT (Claude đề xuất, người dùng duyệt 2026-10-02; số CHƯA cân).**
Theo khung N: 1 nội tại (1★) + 1 chiêu xịn (thanh mana), không chiêu phụ. Chỉ số neo vào roster N hiện có (Frostweaver 260/35/0,6/5,0).

| Hero | Vai | Máu | Sát thương | Tốc đánh | Tầm | Mana |
|---|---|---|---|---|---|---|
| Pháp Sư Lửa Tập Sự (`EmberApprentice`) | Tầm xa, sát thương theo thời gian | 240 | 22 + cháy | 0,8/s | 5,0 | 5 |
| Người Lùn Khiên Rune (`RuneShield`) | Chặn đường, chắn đạn | 1100 | 14 | 0,7/s | 0,55 | 6 |
| Phù Thuỷ Đầm Lầy (`BogWitch`) | Hao mòn, phá giáp | 250 | 14 + độc | 0,9/s | 4,5 | 5 |

- **Pháp Sư Lửa.** Đòn thường bắn **đốm lửa** (đạn bay), trúng thì quái **Cháy** 3 s, 5 sát thương/s (trúng lại thì làm mới, không cộng dồn).
  *Nội tại Bén Lửa:* quái đang Cháy mà chết thì lửa nhảy sang quái gần nhất trong bán kính 1,2 (Cháy 3 s mới) — dây chuyền được.
  *Chiêu Mưa Tàn Lửa:* 3 quả cầu lửa rơi lệch nhau quanh cụm quái đông nhất trong tầm, mỗi quả nổ bán kính 0,7 gây 2× đòn thường + Cháy,
  để lại **vũng lửa** 4 s (quái đứng trong bị Cháy liên tục; dùng lại cơ chế `PoisonPool`). Cast: clip **cast** riêng (giơ gậy lên trời).
  FX cần vẽ: đạn đốm lửa · lửa Cháy nhỏ trên người quái (loop) · cầu lửa rơi + nổ · vũng lửa loop.
- **Người Lùn Khiên Rune.** Đòn thường đập khiên + nện búa (cận chiến, chặn 2 quái như hero cận chiến khác).
  *Nội tại Chắn Đạn:* **đạn của quái bắn xa trúng ông thì vỡ, không gây sát thương** (cận chiến vẫn ăn đủ) — khắc chế Goblin Ná/quái bắn xa;
  quái bắn xa nhắm hero gần nhất nên ông đứng hàng đầu là hút đạn cho cả đội.
  *Chiêu Tường Rune:* cắm khiên xuống đất, dựng **tường rune 5 s** ngang trước mặt phủ hàng của ông và hai hàng kề: quái chạm tường bị
  chặn đứng như gặp hero (không đi vòng — lane không có tìm đường), đạn quái không bay qua; tường không có máu, hết giờ thì tan.
  Trong 5 s đó ông đứng yên sau khiên. Cast: clip **cast** riêng (nâng khiên, cắm xuống, giữ thế).
  FX cần vẽ: tường rune (mọc lên 3 frame + loop + tan) · tia đạn vỡ trên khiên (nhỏ, 3 frame).
- **Phù Thuỷ Đầm Lầy.** Đòn thường hất **giọt độc** (đạn bay vòng cung), mỗi trúng +1 tầng **Độc** (tối đa 5), mỗi tầng 2 sát thương/s,
  tầng làm mới 4 s. *Nội tại Ngấm Độc* chính là cơ chế cộng dồn này (không thêm gì khác — một câu là hiểu).
  *Chiêu Vạc Sương Độc:* màn sương bán kính 1,3 ở cụm quái đông nhất trong tầm, kéo dài 5 s: quái trong sương +1 tầng Độc mỗi giây và
  **−20% giáp**. Cast: clip **cast** riêng (khuấy muôi như khuấy vạc).
  FX cần vẽ: đạn giọt độc · bong bóng độc nhỏ trên người quái (loop, đậm theo tầng thì code chỉnh alpha) · màn sương loop + tan.

Ba chiêu đều KHÔNG trùng niche đã có (vùng chậm Frostweaver, hoá đá Cragfist, vùng độc của quái Alchemist là phe địch).
Asset thêm ngoài 5 clip cơ bản: **3 clip cast + 10 FX** — brief riêng sau khi bộ clip cơ bản được duyệt.

**Bộ hero N fantasy đợt 2 (N5–N8) — kit ĐỀ XUẤT (Claude, 2026-10-03; số CHƯA cân).** Người dùng (2026-10-03): **Gnome Đạo Chích BỎ HẲN**; Hiệp Sĩ Nấm ĐẠT; Tiên Dệt Tơ đổi sang Múa Rối, Ngư Nhân đổi chiêu sang Xoáy Nước (v1 bị chê).
Identity đã duyệt 2026-10-03 (`tempart/<Unit>/identity/`). Cùng khung N: 1 nội tại (1★) + 1 chiêu xịn (thanh mana), không chiêu phụ.

| Hero | Vai | Máu | Sát thương | Tốc đánh | Tầm | Mana |
|---|---|---|---|---|---|---|
| Tiên Dệt Tơ (`SilkFae`) | Tầm xa, khống chế nhóm | 230 | 16 | 0,8/s | 4,5 | 6 |
| Hiệp Sĩ Nấm (`ShroomKnight`) | Chặn đường giá rẻ | 1000 | 12 | 0,7/s | 0,55 | 6 |
| Ngư Nhân Đinh Ba (`TritonGuard`) | Cận chiến tầm nhỉnh | 800 | 22 | 0,8/s | 0,9 | 5 |

- **Tiên Dệt Tơ — skill v2 "Múa Rối" (người dùng chọn 2026-10-03).** Đòn thường bắn **sợi tơ bạc** (đạn bay thẳng).
  *Nội tại Dây Rối:* quái trúng đòn thường bị tơ vướng tay — **25% đòn của nó trượt** trong 2 s (trúng lại thì làm mới, không cộng dồn).
  *Chiêu Múa Rối:* giật tơ lên quái **nhiều máu nhất** trong tầm (trừ boss): **4 s nó quay sang đánh quái phe mình** (tinh anh 2 s), hết giờ trở lại phe địch, đứng nguyên chỗ.
  Cast: clip **cast** riêng (hai tay giật tơ như người múa rối).
  FX cần vẽ: đạn sợi tơ · dây rối bạc nối từ trên đầu quái bị điều khiển (loop) + đứt tơ (3 frame).
- **Hiệp Sĩ Nấm.** Đòn thường chém kiếm gỗ (cận chiến, chặn 2 quái).
  *Nội tại Bào Tử:* mỗi lần bị đánh cận chiến thì nhả bào tử, quái trong bán kính 0,8 ăn 4 sát thương (tối đa 2 lần/giây — chống spam khi bị vây).
  *Chiêu Nở Nấm:* 3 cây nấm mọc quanh chân (bán kính 1,0), 2 s sau nổ bào tử, mỗi cây gây 3× đòn thường trong bán kính 0,6.
  Cast: clip **cast** riêng (cắm kiếm xuống đất).
  FX cần vẽ: bụi bào tử (3 frame) · nấm mọc (3 frame) + nổ bào tử (4 frame).
- **Ngư Nhân Đinh Ba — skill v2 "Xoáy Nước" (người dùng chọn 2026-10-03).** Đòn thường thọc đinh ba, tầm 0,9 (xa nhất trong nhóm cận chiến).
  *Nội tại Thọc Hàng:* đòn thường xuyên qua, quái đứng ngay sau mục tiêu (trong 0,6) ăn 50%.
  *Chiêu Xoáy Nước:* cắm đinh ba, **xoáy nước bán kính 1,0 quanh chân trong 4 s**: quái trong xoáy **không đánh được** (bị cuốn) và mất 0,5× đòn thường mỗi giây; ông vẫn đánh bình thường.
  Tinh anh đánh được nhưng −50% sát thương; boss không bị khoá đòn, chỉ ăn sát thương. Cast: clip **cast** riêng (cắm đinh ba xuống đất).
  FX cần vẽ: xoáy nước quanh chân (loop 4 s) + tan (3 frame).

Asset thêm ngoài 5 clip cơ bản: **3 clip cast + 8 FX** (Tiên 3 · Nấm 3 · Ngư Nhân 2).

**Bộ hero N fantasy đợt 3 (N9–N11) — kit ĐÃ CHỐT (Claude đề xuất, người dùng duyệt cả 3 ngày 2026-10-03; số CHƯA cân).** Hoa Yêu chọn Hạt Bám; Kobold chiêu đổi sang Kích Nổ.
Cùng khung N: 1 nội tại (1★) + 1 chiêu xịn (thanh mana), không chiêu phụ. Identity đã duyệt 2026-10-03 (`tempart/<Unit>/identity/`).

| Hero | Vai | Máu | Sát thương | Tốc đánh | Tầm | Mana |
|---|---|---|---|---|---|---|
| Hoa Yêu Bắn Hạt (`PetalSprite`) | Tầm xa, dọn quái lẻ | 220 | 24 | 1,0/s | 4,5 | 5 |
| Kobold Gài Bẫy (`KoboldTrapper`) | Khống chế rẻ | 300 | 15 | 0,8/s | 3,5 | 6 |
| Thầy Bùa Ấn Sao (`SigilMage`) | Hỗ trợ sát thương | 240 | 18 | 0,8/s | 4,5 | 6 |

- **Hoa Yêu Bắn Hạt.** Đòn thường nhổ **hạt** từ tâm hoa (đạn bay thẳng).
  *Nội tại Hạt Bám (người dùng chọn 2026-10-03):* mỗi đòn cắm 1 hạt lên quái, **đủ 3 hạt thì nảy mầm nổ** gây 2× đòn thường lên
  chính nó (không lan), hạt bám 4 s kể từ hạt cuối — dồn sát thương đơn mục tiêu.
  *Chiêu Mưa Hạt:* bắn liên tiếp 8 hạt trong 1,6 s vào cụm quái đông nhất trong tầm, mỗi hạt = đòn thường (đi kèm nội tại đã chọn).
  Cast: clip **cast** riêng (ngửa bông hoa lên, rung cánh).
  FX cần vẽ: đạn hạt · hạt dính trên quái (1 frame, code xếp 1–3 cái) + mầm nổ 4 frame.
- **Kobold Gài Bẫy.** Đòn thường ném **dao nhỏ** (đạn bay thẳng, tầm ngắn 3,5).
  *Nội tại Đặt Bẫy:* mỗi 8 s rải 1 **bẫy kẹp** lên đường quái phía trước trong tầm (tối đa 3 trên sân); quái đầu tiên giẫm phải
  **đứng im 1,5 s** + ăn 1× đòn thường, bẫy mất. Tinh anh đứng im 0,75 s; boss không bị giữ (dùng `Unit.ApplyRoot`, boss `Unmovable`).
  Hết wave dọn sạch bẫy.
  *Chiêu Kích Nổ (người dùng chọn 2026-10-03, thay Rải Bẫy):* giật dây cho **mọi bẫy đang nằm trên sân nổ cùng lúc** — mỗi bẫy nổ
  bán kính 0,8, gây 3× đòn thường + đứng im 1 s (tinh anh 0,5 s, boss chỉ ăn sát thương), rồi lập tức đặt lại 3 bẫy mới. Không có bẫy nào
  thì vẫn đặt 3 bẫy. Cast: clip **cast** riêng (giật mạnh sợi dây nối các bẫy).
  FX cần vẽ: đạn dao · bẫy nằm đất (mở, 1 frame) · bẫy sập (3 frame, frame cuối giữ trên chân quái lúc đứng im) · bẫy nổ (4 frame).
- **Thầy Bùa Ấn Sao.** Đòn thường bắn **ấn sao nhỏ** (đạn bay thẳng).
  *Nội tại Ấn Yếu:* quái trong bán kính 2,0 quanh ông nhận **+15% sát thương từ mọi nguồn** (dùng lại `Unit.ApplyArmorBreak` làm mới liên
  tục — hệ quả: KHÔNG cộng với −20% giáp của Vạc Sương Độc, lấy mức cao hơn).
  *Chiêu Bùa Nổ:* gắn ấn sao lên **3 quái nhiều máu nhất** trong tầm, 2 s sau mỗi ấn nổ 3× đòn thường bán kính 0,6.
  Cast: clip **cast** riêng (mở sách bùa, giơ tay vẽ sao).
  FX cần vẽ: đạn ấn sao · vòng ấn mờ dưới chân (loop, cỡ bán kính 2) · ấn sao trên đầu quái (loop) · nổ sao (4 frame).

Asset thêm ngoài 5 clip cơ bản: **3 clip cast + 10 FX** (Hoa 3 · Kobold 4 · Thầy Bùa 4).

**Pháp sư triệu hồi — bộ kỹ năng (chốt với người dùng 2026-09-13).** Chủ đề: quái chết → dựng thành
xương đánh cho phe ta. Có hai NGUỒN lính xương. Mỗi nguồn có bộ đếm riêng, giới hạn riêng và con
xương lớn riêng; lính của nguồn này không tính vào giới hạn của nguồn kia, nên tối đa 6 + 6 = 12 con cùng lúc.

| | Nội tại *(luôn chạy)* | Chiêu thường *(cooldown ẩn)* | Chiêu xịn *(thanh mana 5)* |
|---|---|---|---|
| Kích hoạt | Quái chết trong bán kính quanh pháp sư → **có tỉ lệ** dựng lính xương | Tạo vòng trên đất: quái dính vòng bị **ấn ký 10s**; chết khi còn ấn ký → **chắc chắn** dựng lính xương | Triệu hồi **Kỵ sĩ tử thần cưỡi ngựa xương** (xem bên dưới) |
| Thời gian sống của lính | **10s** | Tới khi bị giết hoặc hết wave | — |
| Giới hạn còn sống | 6 | 6 | — |
| Con xương lớn | Cứ **xác đủ điều kiện thứ 4** (đếm cả khi nguồn đã đầy, không phụ thuộc số xương đang sống) thì dựng quái xương lớn (chỉ số to), luôn dựng được kể cả vượt giới hạn, rồi bộ đếm về 0 (sửa 2026-09-15; hạ 6 → 4 cùng ngày vì ít xuất hiện) | Như nội tại, bộ đếm riêng | — |

Luật chung: **hết wave thì dọn sạch mọi lính xương**. Lính xương chặn quái như mọi unit (giữ tối đa 3).
Tinh anh/boss chết cũng chỉ dựng lính xương thường. Nguồn đã đủ 6 con thì không dựng thêm xương thường,
nhưng bộ đếm VẪN tăng (sửa 2026-09-15). Quái vừa có ấn ký vừa chết trong bán kính nội tại thì tính cho **chiêu thường**; nếu
chiêu thường đã đủ 6 thì thử tiếp tỉ lệ của nội tại.
Vòng của chiêu thường gây **sát thương thấp**; tác dụng chính là ấn ký. Hình phải khác hẳn đĩa băng
của Frostweaver.

**Kỵ sĩ tử thần (chiêu xịn, chốt hướng 2026-09-13).** Chọn thay rồng xương vì người dùng cần một
unit **CHẶN quái** (rồng bay thì không chặn). Là một unit đầy đủ như hero: **có thanh máu và thanh mana**,
chặn quái như mọi unit (giữ tối đa 3), không tính vào giới hạn 6 của hai nguồn lính xương. Hai kỹ năng:
- **Nội tại — Hào quang:** lính xương (cả hai nguồn, gồm xương lớn) đứng trong bán kính quanh kỵ sĩ được buff.
- **Chiêu (thanh mana) — Phi xuyên lane:** phi thẳng qua lane, gây sát thương và **choáng** mọi quái trên đường.

Chốt thêm (2026-09-13): kỵ sĩ **tồn tại tới khi chết hoặc hết wave**, mỗi lúc chỉ một con · khi kỵ sĩ còn
sống, thanh mana của pháp sư **vẫn tích, đầy thì hồi đầy máu cho kỵ sĩ** (không gọi con thứ hai) ·
**boss và tinh anh chỉ bị choáng một nửa** thời gian.
*Số khởi điểm Claude đề xuất, CHƯA cân bằng:* kỵ sĩ 900 máu / 30 dmg / 0,8 đòn/s / tầm 0,7 / aggro 3,5 /
mana 6 · hào quang bán kính 3,0, lính xương +25% sát thương và +20% tốc đánh · phi dài 6,0 unit, bề
ngang ±0,6 theo trục y, 60 dmg, choáng 2s, phi xong đi bộ về điểm triệu hồi.
**Tăng lực 2026-09-15 (người dùng chấm "quá yếu, thiếu AoE, thiếu buff"):** 1800 máu / 55 dmg / 1,0 đòn/s /
mana 5 · **đòn thường sát thương diện rộng** bán kính 1,2 quanh mục tiêu · hào quang bán kính 4,0, +50% sát
thương, +30% tốc đánh, lính xương trong hào quang **tô xanh nhạt** để thấy buff · phi 150 dmg · **phi xong đánh
tiếp tại chỗ, không đi bộ về** (đi về giữa lúc bị vây thì quay lưng ăn đòn) · **phi về phía cụm quái đông nhất**
(phi chéo được, không chỉ thẳng trước mặt) · **đòn thường đẩy lùi** mọi quái trúng đòn, kể cả quái đang vung đòn ·
vòng ấn ký của pháp sư hiện **1,5s** (trước ~0,44s, không kịp thấy).

**Đã code 2026-09-15** (`Necromancer` + `Summoner_Bonecaller.asset`). Hai điểm lệch có chủ ý: kỵ sĩ
và lính xương dùng luật di chuyển hero mới (đuổi quái khắp sân) nên con số aggro 3,5 không còn dùng; tốc
chạy kỵ sĩ 1,2 do Claude chọn (GDD không ghi). Luật "giữ tối đa 3 quái" vẫn chưa code — lính chặn
theo kiểu quái rẽ sang đánh unit gần nhất.

*Số khởi điểm Claude đề xuất, CHƯA cân bằng:* bán kính nội tại 4,0 (= tầm đánh) · tỉ lệ dựng 35% ·
vòng chiêu thường bầu dục 2:1 bán kính 1,6 (cùng khung với FrostZone), 10 sát thương, cooldown 12s ·
lính xương 150 máu / 12 dmg / 1,0 đòn/s / tốc 1,0 · xương lớn 700 máu / 40 dmg / 0,7 đòn/s / tốc 0,7.

**Đạn đánh thường (chốt 2026-09-13):** một **đầu lâu hồn ma xanh bạc hà nhỏ** bay thẳng, 4 frame. Trúng
đích thì **vỡ thành mảnh xương + khói bạc hà** (impact 4 frame). Màu bạc hà lấy từ hốc mắt đầu lâu trên
gậy, nên đạn đọc ra là "đầu lâu trên gậy bắn ra". Đạn đơn mục tiêu, không xuyên, không gây hiệu ứng phụ.

**Cai ngục móc xích (Chainwarden, SR) — bộ kỹ năng (chốt với người dùng 2026-09-24).** Vai: chốt chặn
khống chế + khuếch đại sát thương — kéo quái về, giữ chặt tại chỗ để đồng đội và pháo dồn đòn. Ba ô móc vào
nhau qua trạng thái **Xích** (quái bị Xích nhận thêm sát thương từ mọi nguồn).

| | Nội tại *Cai Ngục* *(luôn chạy)* | Chiêu thường *Quật Xích* *(cooldown ẩn)* | Chiêu xịn *Ném Móc → Cuồng Nộ* *(thanh mana 5)* |
|---|---|---|---|
| Hiệu ứng | Giữ tối đa **4 quái** (unit khác 3). Quái đang bị ông chặn bị **Xích**: **+20% sát thương nhận từ mọi nguồn** | Quay xích quanh mình: sát thương thấp + **trói** (đứng im, vẫn đánh được) mọi quái trong bán kính | Ném móc kéo quái tầm xa (ưu tiên Goblin Ná) về sát mình + choáng ngắn + **Xích 4s**, rồi **biến hình Cuồng Nộ VĨNH VIỄN** (tới khi gục — người dùng chốt 2026-09-29, bỏ giới hạn 8s): xích thành **hai xích dài hai bên** — tầm đánh ~2,0, mỗi đòn trúng thêm một con **bên kia**, **+50% công, +30% tốc đánh, −20% sát thương nhận vào, hút máu 25% sát thương gây ra**; Quật Xích đổi thành **Lốc Xích** (quay 360°, bán kính 2,0, 2× đòn đánh, trói 1s, hồi 4s). Không tích mana trong lúc Cuồng Nộ |
| Tinh anh/boss | Như quái thường | Trói **một nửa** thời gian | Choáng + trói Lốc Xích **một nửa** |

*Số khởi điểm Claude đề xuất, CHƯA cân bằng:* 1200 máu / 28 dmg / 0,8 đòn/s / tầm 0,6 / mana 5 (buff 2026-09-29 từ 1000/18/0,7 — quá yếu cho SR) · Quật Xích
bán kính ~1,0, 25 sát thương (cũ 10), trói 1,5s, cooldown ~10s. Bỏ ý cũ "chiêu nhỏ móc lại quái lọt hàng" vì trùng chủ đề chiêu lớn.
Chiêu xịn đổi 2026-09-26 (người dùng: Ném Móc một mình "quá yếu" cho SR) — Ném Móc giữ làm mở đầu, thêm biến hình
Cuồng Nộ; số trong `Warden_Chainwarden` (`rage*`, `storm*`).
**Chiêu phụ 2 *Giật Xích* (người dùng chốt 2026-09-29, mở ở `secondMinorStar` 3):** cooldown ẩn ~8s, chỉ tung khi đang
giữ ≥ 2 quái — giật mạnh hai xích, mỗi quái đang bị giữ ăn đòn thường × (1 + 0,5 × số quái bị giật) + choáng 1s (boss/tinh
anh nửa). Cuồng Nộ: kéo thêm mọi quái chưa bị giữ trong tầm 2,0 về sát mình vào cú giật. Ăn thẳng vào nội tại giữ 4 quái.
Art chỉ vẽ dạng Cuồng Nộ (`rage-yank` + FX `yank-clash-fx`); dạng thường chỉ FX. Số trong `Warden_Chainwarden` (`yank*`).

**Long Kỵ Binh (Wyrmspear, SSR) — bộ kỹ năng (chốt với người dùng 2026-09-27).** Vai: diệt boss/tinh anh.
Cơ chế riêng: **biến hình VĨNH VIỄN** (giống Chainwarden Cuồng Nộ) — lần tung chiêu xịn đầu tiên hoá **Long Nhân** (mọc sừng,
đuôi, áo choàng thành cánh thật) và gọi **Bạch Địa Long** trồi lên đánh cùng; từ đó cả bộ kỹ năng đổi hẳn. Chủ đề
*Thiên – Địa song long*: người là rồng TRỜI (mũi giáo, đánh từ trên xuống), địa long là rồng ĐẤT (tường, giữ trận).

Giai đoạn 1 — Kỵ Sĩ (1 chiêu thường, để biến hình thành một bước nhảy lớn):

| | Nội tại *Nghịch Lân* | Chiêu thường *Xuyên Long* *(cooldown ẩn)* | Chiêu xịn *Long Hoá* *(thanh mana 5)* |
|---|---|---|---|
| Hiệu ứng | Đòn đánh vào boss/tinh anh cộng thêm % máu tối đa mục tiêu | Đâm xuyên một đường, bỏ qua giáp | Biến hình vĩnh viễn: đất nứt, Bạch Địa Long trồi lên cạnh ông |

Giai đoạn 2 — Long Nhân + Bạch Địa Long (2 chiêu thường):

| | Nội tại *Long Uy · Long Giáp · Long Khế* | Chiêu thường 1 *Thiên Long Liên Thương* (người) | Chiêu thường 2 *Địa Long Chấn* (rồng) | Chiêu xịn *Thiên Địa Long Giáng* *(mana 5)* |
|---|---|---|---|---|
| Hiệu ứng | **Long Giáp:** +60% máu tối đa, −35% mọi sát thương nhận, miễn choáng/hất văng. **Long Uy:** +60% công; quái trong tầm −20% công; boss/tinh anh trong tầm nhận +15% sát thương từ mọi nguồn. **Long Nộ:** mỗi lần ông **hoặc địa long** chịu sát thương → +1 tầng, **cộng dồn tối đa 7**, mỗi tầng +5% giảm sát thương nhận và +8% công (7 tầng: +35% / +56%), ông và rồng cùng hưởng; **mỗi tầng còn +3% hút máu** (người dùng thêm 2026-09-28, 7 tầng 21%). **Long Khế:** địa long hưởng **một nửa** Long Giáp + Long Uy | Đâm liên hoàn 5 nhát cực nhanh một mục tiêu, nhát cuối **xuyên cả đường** phía sau; mỗi nhát vào boss đều ăn Nghịch Lân | Rồng dựng người đập hai chân trước: gai đá trắng quanh nó — sát thương vùng, **choáng 1,5s**, **phá giáp −30% trong 5s** | Rồng cắm đầu xuống đất, **cột đá trắng** (chiêu của rồng — rồng bị hạ thì không có cột) trồi ở **cụm quái đông nhất trong vùng hoạt động của rồng** (tầm đánh + dây xích, người dùng chốt 2026-09-29) (không theo mục tiêu của ông), hất tung cả cụm; ông dang cánh bay lên lao xuống cắm giáo → **đất nát** (chỉ vết nứt, người dùng chốt 2026-09-28). Sát thương rất lớn + % máu tối đa; **kết liễu quái thường/tinh anh dưới 20% máu**, boss thì ×2 sát thương; hạ được mục tiêu hoàn 2 mana |
| Tinh anh/boss | — | Như trên | Choáng **một nửa** | Không kết liễu, ×2 sát thương |

**Bạch Địa Long:** đứng **ô phía trước** Wyrmspear trên lane, **chặn tối đa 4 quái** (như Chainwarden); ông đứng
sau **đâm qua đầu rồng** (tầm ~2). Đòn thường táp + quật đuôi, sát thương vừa, nhiều máu. Bị hạ thì chui xuống đất,
**trồi lại sau ~15s** (trong lúc vắng: mất Long Khế, đòn đánh vào rồng không cộng Long Nộ). Tạo hình: rồng **không
cánh**, thân thấp nặng như thằn lằn khổng lồ, vảy **trắng ngà**, gai lưng **xám đá**, chỉ sừng + móng **vàng đồng
nhạt** để nối với giáp Wyrmspear.

*Số khởi điểm Claude đề xuất, CHƯA cân bằng.* Long Nộ (code 2026-09-28, số chưa cân): tối thiểu 0,5s giữa hai lần cộng tầng
(không thì 1 bầy quái đánh là đầy 7 ngay), không chịu đòn 4s thì rơi 1 tầng/giây. Art giai đoạn 2 nặng: Long Nhân đủ
bộ clip + clip liên thương + clip bay-lao-cắm; địa long identity/idle/walk 4 chân/attack/đập đất/cắm đầu/hurt/death
(chui xuống đất); clip biến hình 8 ô + clip rồng trồi lên; FX gai đá, cột đá, sóng chấn, đường xuyên, icon tầng Long Nộ.

**Tôn Ngộ Không (Wukong, SSR) — bộ kỹ năng (chốt với người dùng 2026-09-30).** Vai: đấu sĩ cận chiến dọn đám đông,
dùng **phân thân** làm thêm người chặn quái (ăn luật mỗi unit giữ 3 quái) và **hồi máu liên tục** để trụ lâu ở tuyến đầu.
Di chuyển = cưỡi Cân Đẩu Vân (clip walk).

| | Nội tại *Thế Thân · Lông Về Thân* | Chiêu thường *Nhổ Lông Phân Thân* *(cooldown ẩn)* | Chiêu phụ 2 *Toàn Phong Côn* *(cooldown ẩn, `secondMinorStar` 3)* | Chiêu xịn *Như Ý Thiên Trụ* *(thanh mana 5)* |
|---|---|---|---|---|
| Hiệu ứng | **Thế Thân:** lần đầu hết máu trong mỗi wave, bản thể hoá nhúm lông bay đi; còn phân thân thì một con **trở thành bản thể** với 40% máu tối đa, không còn thì chết như thường. **Lông Về Thân:** mỗi phân thân tan (hết giờ hoặc bị hạ) → bản thể **hồi 8% máu tối đa**; đòn đánh thường **hút máu 10%** | Nhổ nắm lông thổi ra: **2 phân thân** sống 8s, mỗi con 40% công/máu bản thể, tự đánh cận chiến, **mỗi con chặn 3 quái** | Xoay gậy quanh người 3s: **chặn và bật ngược đạn** bay tới (đạn ná, bình độc…) về phía quái; sát thương quanh mình mỗi 0,5s; **mỗi quái trúng mỗi nhát hồi 3% máu tối đa** (trần 25%/lần xoay), đạn bật trúng quái cũng hồi 3% | Ném gậy lên trời, gậy **biến lớn** dập nằm ngang xuống **cụm quái đông nhất trong tầm**: sát thương lớn theo vệt dài dọc sân, choáng 1,5s, để lại **vết nứt làm chậm 3s**. Mọi phân thân đang sống dập gậy nhỏ cùng lúc (mỗi cú 30%) |

*Số khởi điểm Claude đề xuất, CHƯA cân bằng:* Nhổ Lông ~10s · Toàn Phong Côn ~7s (người dùng: "hồi máu nhiều, cooldown
không quá lâu"), bán kính xoay 1,2 · Như Ý Thiên Trụ vệt dài ~4 × rộng ~1. Chọn *Lông Về Thân* thay *Bàn Đào Tiên Đan* vì
ăn khớp với phân thân (người dùng chốt 2026-09-30). Art: phân thân dùng lại sprite Wukong tô nhạt bằng
code; hồi máu dùng lại `Vespera/heal-fx`. Clip thân `clone-cast`, `smash-cast`, `spin`; FX khói lông (phân thân xuất hiện/tan,
Thế Thân), gậy khổng lồ + vết nứt, vòng gió xoay gậy, tia chặn đạn.

**Pháp sư sấm sét (Stormcaller, SSR, tên mã tạm) — hướng chốt với người dùng 2026-10-01, kit là đề xuất của Claude.**
Vai: **SSR đánh xa** (hai SSR trước đều cận chiến), sét **nảy theo chuỗi quái** — khác Frostweaver (nổ vùng quanh một
điểm). Khắc Khiên Phép của OrcShaman và đám đông chạy nhanh (RatRunner).

| | Nội tại *Quá Tải* *(ghép A+D, người dùng chốt 2026-10-01)* | Đòn thường *Xích Lôi* | Chiêu xịn *Lôi Bạo* *(thanh mana)* |
|---|---|---|---|
| Hiệu ứng | **Tầng điện:** mỗi tia sét trúng quái (kể cả tia Cột Thu Lôi, cột Lôi Bạo) +1 tầng, tối đa 3; đủ 3 → **nổ**: đòn đó +60% sát thương + **choáng 0,3s**, xoá tầng (boss/tinh anh không choáng, vẫn ăn nổ). **Tê Liệt:** quái đang choáng ăn **+35%** sát thương sét, sét nảy **ưu tiên** quái đang choáng. Sét vào **khiên** (Khiên Phép OrcShaman, khiên hút) **×2** | Tia sét trúng 1 quái rồi **nảy sang tối đa 3 quái** gần nhất (bán kính 2), mỗi lần nảy −30% sát thương | **5 cột sét** giáng xuống chỗ quái đứng trong tầm, mỗi cột sát thương lớn + **choáng 0,5s** |

*Số khởi điểm Claude đề xuất, CHƯA cân bằng:* 520 máu / 40 dmg / 0,8 đòn/s / tầm 5,5 / mana 6 · Lôi Bạo mỗi cột 120.
**Chiêu phụ 2 *Lôi Bộ* (người dùng duyệt 2026-10-01, mở ở `secondMinorStar` 3):** cooldown ẩn, tự tung khi có quái áp sát —
hoá tia sét dịch lùi 2 ô, để lại cú nổ ở chỗ cũ **choáng 0,5s**. Art: chớp biến mất/hiện ra + nổ đất, không cần clip thân mới.
**Chiêu thường *Cột Thu Lôi* (người dùng duyệt 2026-10-01):** cooldown ẩn, cắm 1 cột đồng xuống giữa đám quái, sống 6s —
mỗi 1s giật quái kề cột; *Xích Lôi* nảy được qua cột, nảy qua cột thì **+2 lần nảy, không bị giảm sát thương**.
Art: clip cắm trượng (hoặc dùng lại attack), sprite cột + tia lặp.
Nội tại: icon tầng điện nhỏ (1–3) trên đầu quái + chớp nổ. Cân bằng: để ý khoá choáng cả cụm (nổ tầng + Lôi Bộ + Lôi Bạo). Art: identity → idle → attack (vung
trượng), hurt, death, cast Lôi Bạo, walk; FX tia sét nảy (đoạn lặp), cột sét + chớp trên đất.

**Võ Tăng Gió (`Galemonk`, SR, tên mã tạm) — người dùng chọn 2026-10-01 trong 5 ý; kit + đủ bộ art người dùng DUYỆT 2026-10-01, đã nối vào game.**
Vai: **khống chế vị trí** — roster chưa ai đẩy lùi cả hàng hay gom quái. Gom xong là combo cho Pháo Nổ (AOE), Stormcaller
(sét nảy), Frostweaver (nổ vùng). Tạo hình: nhà sư giang hồ đội **nón tre rộng vành**, áo vạt tăng xanh ngọc nhạt, tay không.

| | Nội tại *Thuận Phong* | Đòn thường *Chưởng Phong* | Chiêu phụ *Phong Tảo* (3★) | Chiêu xịn *Long Quyển* *(thanh mana)* |
|---|---|---|---|---|
| Hiệu ứng | Quái bị gió đẩy/hút **chậm 20% trong 2s** | Đạn gió tầm 2,5 trúng quái đầu tiên, **đẩy lùi 0,3 ô** | Cooldown ẩn ~10s: chưởng thẳng dọc lane, **xuyên cả hàng**, đẩy lùi 1,5 ô | Lốc xoáy ở cụm quái đông nhất, **hút quái bán kính 2,5 về tâm, giữ 3s**, sát thương mỗi giây |

**Tinh anh: bị hút (Long Quyển) nhưng KHÔNG bị đẩy lùi** (người dùng chốt 2026-10-01). Boss: không bị đẩy/hút, chỉ ăn chậm. Đẩy lùi không bỏ qua luật chặn — quái đang bị giữ vẫn bị đẩy ra rồi đi lại.
*Số khởi điểm Claude đề xuất, CHƯA cân:* 700 máu / 22 dmg / 0,9 đòn/s / tầm 2,5 / mana 6 · Long Quyển 30/giây.
**Identity g2 (tụ khí) — Claude chọn theo ủy quyền người dùng 2026-10-01.** **Walk = LƠ LỬNG trên một luồng gió xoáy dưới chân** (người dùng chốt 2026-10-01): thân giữ thế, trôi nhẹ lên xuống, gió xoáy dưới chân lặp — không chu kỳ hai chân, chấm như clip hover (Gloomwing/Ma Hỏa), không dùng `gait_sort`.
Art: identity → idle → walk (lơ lửng trên gió)/attack (đẩy chưởng)/hurt/death → cast Long Quyển + FX đạn gió, sóng Phong Tảo, lốc xoáy lặp.

**Bộ Đào Viên — Lưu Bị / Quan Vũ / Trương Phi (SSR) — kit người dùng DUYỆT 2026-10-02 (đề xuất Claude, sửa 2 vòng).**
Vai: Lưu Bị hỗ trợ (1 kiếm) · Quan Vũ sát thương cận chiến (Thanh Long Đao) · Trương Phi chống chịu + khống chế (xà mâu).
**Nội tại chung *Kết Nghĩa*:** 2/3 anh em trên sân → cả ba +10% công và máu; đủ 3/3 → thêm +25% tốc hồi mana. Gọi chi viện đúng anh em còn thiếu cũng kích hoạt.

| | Nội tại riêng | Chiêu thường *(cooldown ẩn)* | Chiêu phụ 2 *(3★)* | Chiêu xịn *(thanh mana)* |
|---|---|---|---|---|
| **Lưu Bị** | *Nhân Đức*: hero trong bán kính 2,5 hồi mana +15% (anh em kết nghĩa ×2) | *Ân Đức*: đồng minh mất máu nhiều nhất hồi 15% máu tối đa + giải độc/choáng | *Đồng Sinh Cộng Tử*: anh em kết nghĩa ngã xuống lần đầu mỗi wave → dải lụa đào kéo dậy sau 1s với 30% máu + 2s bất tử (mỗi người 1 lần/wave). Người dùng loại bản gánh sát thương ("Lưu Bị chết sớm hơn") | *Đào Viên Kết Nghĩa*: hoa đào rơi khắp sân, mọi đồng minh hồi 25% máu + 30% công 6s; anh em kết nghĩa +2 mana |
| **Quan Vũ** | *Võ Thánh*: đòn thường chém quạt trúng tối đa 3 quái; mỗi quái bị hạ +1 tầng Uy (+5% công **và +2% hút máu**, tối đa 6, mất khi hết wave) | *Thanh Long Yển Nguyệt*: sóng đao trăng khuyết lục bay dọc lane, xuyên quái, ×2 | *Thác Đao Kế*: máu dưới 40% → lùi 1 ô giả thua rồi quay lại chém ×3 vào quái đuổi theo | *Thanh Long Hiện* (người dùng chọn rồng): hồn rồng lục trong suốt từ lưỡi đao lao dọc lane xuyên mọi quái, ×5 + thêm theo % máu đã mất; mỗi quái bị hạ hoàn 10% mana. Chỉ là FX, không phải vật cưỡi (khác Bạch Địa Long của Wyrmspear) |
| **Trương Phi** | *Vạn Nhân Địch*: mỗi quái trong bán kính 1,5 −6% sát thương nhận (tối đa 30%) | *Bát Xà Mâu*: đâm xuyên 2 ô, quái trúng phá giáp (+15% sát thương nhận 4s) | *Túy Tửu*: tu bầu rượu, hồi 20% máu, miễn choáng, −30% sát thương nhận 5s (người dùng: giáp thay tốc đánh cho đúng vai tank) | *Đương Dương Kiều Hống*: dậm chân gầm, nứt đất; quái bán kính 4 ăn ×3, hất lùi 2 ô + choáng 3s + phá giáp 30% 6s; Trương Phi khiêu khích cả vùng + −50% sát thương nhận 6s. Tinh anh bị hất lùi; boss chỉ chậm 50% + phá giáp |

*Số khởi điểm Claude đề xuất, CHƯA cân.* Art kỹ năng: Lưu Bị clip `cast` (giơ kiếm ban phúc, dùng cho Ân Đức + chiêu xịn) + FX hoa đào rơi, vòng hồi máu hồng đào, lụa kéo hồi sinh · Quan Vũ clip `slash-cast` (sóng đao + chiêu xịn) + FX sóng đao trăng khuyết, rồng linh hồn lục, vệt chém Thác Đao · Trương Phi clip `roar` (dậm + gầm), `drink` (tu bầu rượu) + FX sóng âm, đất nứt, dấu sợ trên đầu quái, vệt đâm xuyên. Bát Xà Mâu: clip `pierce-cast` riêng (người dùng 2026-10-02: xoay xà mâu nạp lực hai tay → lao bước dài đâm thẳng; trước đó dùng lại attack). 9 icon kỹ năng ở lane UI, làm sau.


**Bộ hero bậc thấp tiếp theo — Bọ Hung (N), Song Đao (R), Chuông Bạc (R).** Người dùng chọn bộ này ngày 2026-10-03; người dùng **DUYỆT kit ngày 2026-10-03**, số CHƯA cân.
Cùng khung N/R: đòn thường + 1 nội tại mở ở 1★ + 1 chiêu mana dùng ngay khi sở hữu; không chiêu phụ.

| Hero | Vai | Máu | Sát thương | Tốc đánh | Tầm | Mana |
|---|---|---|---|---|---|---|
| Hiệp Sĩ Bọ Hung (`BeetleKnight`, N) | Chặn, khống chế quanh thân | 1050 | 20 | 0,8/s | 0,65 | 6 |
| Hiệp Khách Song Đao (`Twinblade`, R) | Bám một mục tiêu, dồn sát thương | 800 | 32 | 1,2/s | 0,65 | 6 |
| Nữ Tu Chuông Bạc (`BellNun`, R) | Giải hiệu ứng cho đội | 360 | 14 | 0,7/s | 4,0 | 7 |

- **Hiệp Sĩ Bọ Hung — “đập đủ ba nhát thì đẩy, húc đất thì choáng”.** Đòn thường nện chùy cận chiến, chặn 3 đối thủ theo khung hero hiện tại.
  *Nội tại Chấn Động:* mỗi đòn thường thứ 3 trúng mục tiêu hất nó lùi 0,5 unit; tinh anh lùi 0,25, boss không bị đẩy. Đếm đòn trúng, không đếm đánh hụt; không thêm sát thương.
  *Chiêu Húc Sừng:* cúi sừng húc xuống đất tại chỗ; một xung quanh chân bán kính 1,0 gây 3× đòn thường và choáng 1 s; tinh anh/boss choáng 0,5 s. Không lao sang vị trí khác, không tạo vùng tồn tại.
  Cast riêng: hạ trọng tâm → sừng chạm đất → hồi thế. Body chỉ chứa chuyển động thân; bụi/xung đất ở sheet riêng.
  Art identity: giáp vỏ cứng tím-đồng trầm, một sừng đầu, chùy ngắn nặng; hai chân nhìn rõ, khác khiên Rune và mũ Nấm.
  FX cần: vệt chùy 3 frame dùng cho nhát thứ 3; xung đất có hướng lan ra quanh điểm chạm 4 frame; dấu choáng nhỏ loop 4 frame (dùng chung).
- **Hiệp Khách Song Đao — “càng chém cùng một con càng mạnh; chiêu dồn hết vào con đó”.** Đòn thường luân phiên hai đoản đao; chặn 3 đối thủ theo khung hero hiện tại.
  *Nội tại Liên Hoàn:* mỗi đòn thường trúng cùng mục tiêu tăng 12% sát thương đòn thường kế tiếp, tối đa 5 tầng (+60%). Đổi mục tiêu hoặc 2 s không đánh trúng thì mất tầng. Nhát đầu chưa hưởng tầng mới; bonus không tăng sát thương của đồng minh.
  *Chiêu Bão Đao:* khoá mục tiêu đang đánh, chém 6 nhát trong 3 s; mỗi nhát gây 65% đòn thường và hưởng số tầng Liên Hoàn lúc bắt đầu. Giữ nguyên bonus đó trong cả chiêu, không tạo thêm tầng; kết thúc xoá tầng. Mục tiêu chết/mất tầm thì ngừng, không nhảy sang con khác; không bất tử khi chém.
  Cast riêng: thu hai đao → chuỗi chém trái/phải nhanh → hồi thế. Không quay thành lốc diện rộng, không phân thân.
  Art identity: áo chẽn đỏ gạch/xám than, hai đoản đao thép, tóc buộc cao; tránh xanh ngọc của Ngư Nhân và silhouette đại kiếm Ironblade.
  FX cần: dấu Liên Hoàn trên mục tiêu, 5 frame ứng với 1–5 tầng (không phải loop); cặp vệt đao ngắn hướng về mục tiêu 4 frame; chạm đao 3 frame. Không có projectile.
- **Nữ Tu Chuông Bạc — “chuông nhỏ gỡ một hiệu ứng, hồi chuông lớn cứu cả đội khỏi khống chế”.** Đòn thường rung chuông, bắn một sóng âm nhỏ gây sát thương; không hồi máu bằng đòn thường.
  *Nội tại Thanh Tẩy:* mỗi 10 s gỡ 1 hiệu ứng Độc/Cháy/Chậm khỏi hero đồng minh bị ảnh hưởng gần nhất trong tầm 4,0 (có thể là bản thân). Nếu có nhiều loại, ưu tiên Độc → Cháy → Chậm; gỡ Độc là xoá toàn bộ tầng độc. Không có ai cần thì giữ sẵn lượt thanh tẩy đến khi có mục tiêu.
  *Chiêu Hồi Chuông:* rung một tiếng lan toàn sân; gỡ Độc, Cháy, Chậm, Trói, Khoá Đòn và Choáng hiện có khỏi mọi hero cùng phe, gồm bản thân; đồng thời đối thủ chậm 30% trong 3 s (tinh anh 15%, boss không chậm). Không hồi máu, không tạo khiên, không cho miễn hiệu ứng sau tiếng chuông; không xoá dấu kỹ năng riêng hay đổi phe.
  Cast riêng: đưa chuông lên cao → rung mạnh → hạ tay. Sóng âm là FX riêng trên đất, quét một lượt rồi tan; vùng nhìn thấy phải thể hiện nhịp lan, không là biểu tượng đứng yên.
  Art identity: áo tu trắng ngà/bạc xám, khăn trùm, chuông cầm tay; viền xanh xám nhỏ, khác đèn/áo tím của Vespera.
  FX cần: đạn sóng âm nhỏ 4 frame; thanh tẩy trên người đồng minh 4 frame; sóng hồi chuông lan + tan 6 frame. Không vẽ vòng hồi máu.

Lộ trình asset theo kit đã duyệt: mỗi hero identity trước; sau đó idle/walk/attack/hurt/death + cast riêng, FX tách body. Ba bộ có 9 sheet FX (Bọ Hung 3, Song Đao 3, Chuông Bạc 3). Neo art/camera/scale vào Frostweaver theo `art-style.md`. Người dùng duyệt cả ba identity và yêu cầu full animation ngày 2026-10-04. Đã xuất đủ 18 clip thân + 9 sheet FX qua image_gen/sprite-forge, PNG/GIF/meta ở `tempart/{BeetleKnight,Twinblade,BellNun}/<action>/final/`; người dùng duyệt các clip/FX trừ walk cả ba ngày 2026-10-04, đồng thời báo một số animation sai trục; guide walk v2 8 pose ĐÃ DUYỆT ngày 2026-10-04; đã sinh walk mới v3 8 frame/hero theo từng pose, ở `tempart/<Unit>/walk-v3/final/`, ĐÃ DUYỆT ở mục `low-trio-walk-v3-20261004` đầu `tempart/REVIEW.html`; đã sửa neo trục đứng cho Bọ Hung attack/cast và Song Đao attack, bản `<action>/axis-v2/` chờ xem lại; chưa giao Unity/code. Người dùng CHỐT DUYỆT walk v3 cho cả ba ngày 2026-10-04 sau khi xem bản sửa; v4 bị loại. Bộ được chọn: `tempart/{BeetleKnight,Twinblade,BellNun}/walk-v3/final/`; chưa import Unity.

## 4. Pháo (3 loại MVP)

| Pháo | Sát thương | Tốc bắn | Tầm | Đặc điểm |
|---|---|---|---|---|
| Liên Thanh | 12 | 2.0/s | 8 | Đơn mục tiêu, ổn định |
| Nổ | 30 | 0.5/s | 7 | AOE bán kính 2 ô |
| Băng | 8 | 1.0/s | 7 | Làm chậm 40% trong 3s |
| *Nỏ Xuyên* | *22* | *0.6/s* | *9* | *Lao bay thẳng xuyên tối đa 3 quái (số Claude đề xuất 2026-10-01, chưa cân)* |

**Bộ pháo 2026-10-05 (asset của 2 bạn, đã vào game) — số TẠM Claude đề xuất, CHƯA cân:**

| Pháo | Hiếm | Sát thương | Tốc bắn | Tầm | Đặc điểm |
|---|---|---|---|---|---|
| Lưới | R | 6 | 0.5/s | 7 | Lưới chụp vùng 0,8, trói chân 1,5s |
| Cối | N | 26 | 0.4/s | 11 | Bắn vòng cầu tới chỗ quái đứng lúc bắn, vòng đỏ báo, nổ vùng 1,6 |
| Tháp Sét | R | 14 | 0.8/s | 6.5 | Sét nảy tức thì qua 3 quái (cách nhau ≤ 2,5) |
| Đại Bác Công Thành | SR | 90 | 0.2/s | 9 | Nổ vùng nhỏ 0,6, ×2 với boss/elite |
| Gatling Tự Động | R | 5 | 2→6/s | 7 | Bắn liên tục 3s thì đạt tốc tối đa; nghỉ 0,35s thì giảm đà |
| Hoả Mai Thành | SR | 100 | 0.25/s | 10 | Nhắm quái còn nhiều máu nhất, tâm đỏ trên đầu lúc ngắm |
| Pháo Âm Thanh | SR | 20 | 1.0/s | 4.5 | Sóng hình nón 60°; phát thứ 4 xung kích ×1,75 + đẩy lùi |
| Pháo Hắc Tinh | SSR | 100 | 0.1/s | 8 | Lõi bay tới chỗ đông quái, hố đen hút 1,5s rồi nổ vùng 1,6; mỗi quái chết trong hố +15% (tối đa 5) |

## 5. Quái

| Loại | Tên | Máu | Sát thương | Tốc độ | Vàng |
|---|---|---|---|---|---|
| Thường | Goblin | 100 | 15 | 1.5 | 5 |
| Nhanh | Ashfang (chó sói) | 60 | 10 | 3.0 | 4 |
| Tank | Orc | 400 | 40 | 0.8 | 12 |
| Nhanh | Chuột Cống (`RatRunner`) | 45 | 8 | 3.5 | 3 |
| **Bắn xa** | **Goblin Ná** | — | — | — | — |
| **Bắn xa** | **Goblin Ném Bình Độc** (`GoblinAlchemist`, tên mã tạm) | *90* | *10 + vũng 12/s × 3s* | *1.2* | — |
| **Tinh anh** | **Gatebreaker** | **1200** | **60** | **0.7** | **40** |
| Boss | *Vua minotaur (chưa đặt tên)* | 5000 | 120 | 0.6 | 200 |
| Boss wave 20 | *Pháp sư Orc lơ lửng (`OrcShaman`, chưa đặt tên)* | *2500* | *60* | *0.7* | — |
| Boss wave 30 | *Rồng Ma Hỏa (`Dreadwyrm`, tên mã tạm)* | *5000* | *100* | *0.55* | — |
| Triệu hồi của rồng | *Ma Hỏa (`SoulWisp`, tên mã tạm)* | *60* | *12* | *2.4* | — |
| **Bay cao** | **Dơi Quỷ** (`Gloomwing`, tên mã tạm) | *45* | *12* | *1.8* | — |

**Bay CAO vs bay THẤP (người dùng 2026-10-01):** Dơi Quỷ là quái **bay cao** đầu tiên. Ma Hỏa, Rồng Ma Hỏa, Pháp sư Orc
chỉ bay/lơ lửng **thấp** — tính như quái mặt đất: bị luật chặn giữ, cận chiến đánh đủ.

**Bay cao `Dơi Quỷ` (`Gloomwing`, cơ chế A người dùng chốt 2026-10-01; số Claude đề xuất, CHƯA cân):** dơi khổng lồ cánh
màng tím bầm, máu giấy. **Không bị chặn**; chỉ **hero bắn xa, lính bắn xa và trụ/pháo** đánh trúng (đòn + chiêu + khống
chế của đơn vị cận chiến đều không với tới). Bay thẳng về thành, **bỏ qua** mọi unit không đánh nó; bị hero/lính bắn xa
đánh thì **quay sang phản đòn kẻ đó** tới khi kẻ đó chết; bị trụ bắn thì **không phản ứng**, vẫn lao vào đập thành.
Xuất hiện từ wave 11 trong các wave hỗn hợp đã có Goblin Ná.

**Nhanh `Chuột Cống` (thêm 2026-09-29):** chuột cống cầm dao găm, con nhanh thứ hai bên cạnh Ashfang — máu giấy, chạy nhanh hơn Ashfang. Bốn con số là **đề xuất của Claude, chưa cân bằng**.

**Bắn xa `Goblin Ná` (thêm 2026-09-21, người dùng chốt):** goblin cầm ná cao su chữ Y, bắn viên đá. **Ưu tiên bắn hero và lính phe ta** trong tầm; **hết mục tiêu mới bắn tường thành**. Chỉ số, tầm và wave xuất hiện chưa chốt. Art: identity đang sinh (`tempart/GoblinSlinger/`).

**Bắn xa `Goblin Ném Bình Độc` (`GoblinAlchemist`, người dùng chọn 2026-09-30):** goblin bịt mặt khăn xám + kính đồng trên trán, ném bình thuỷ tinh lục-vàng theo đường cong. Bình vỡ thành **vũng độc ~3 s**, hero/lính đứng trong vũng mất máu dần → ép dời hero giữa wave (khác Goblin Ná: sát thương vùng theo thời gian, không đơn mục tiêu). Chỉ số, tầm, wave xuất hiện chưa chốt. Art: identity đang sinh (`tempart/GoblinAlchemist/`); sau đó idle/walk/attack/hurt/death + bình bay + vỡ + vũng độc.

**Tinh anh `Gatebreaker` (thêm 2026-09-12):** hạng quái giữa Orc và boss — cùng dòng giống với boss,
đọc ra như "lính của nó". Vác một cánh cổng thành gỗ đã vỡ: **giảm 15% sát thương** (nửa của boss).
Xuất hiện **từ wave 12, 1 con mỗi wave; wave 21+ thì 2 con**; **không** xuất hiện ở wave boss
(10/20/30) để không loãng khoảnh khắc boss. Bốn con số máu/sát thương/tốc/vàng ở trên là **giá trị
khởi điểm tôi đề xuất, chưa qua cân bằng** — chỉnh trong ScriptableObject khi chơi thử.
Art identity ĐÃ CÓ (`tempart/Gatebreaker/identity/`, 94×81 px), còn 5 action.
**Ruinjaw** (2026-09-13): bản vẽ từng dự định làm boss, được chấm là "Gatebreaker nâng cấp một bậc" →
dùng làm **dạng nâng cấp của Gatebreaker**, không phải boss. **Số tạm (code 2026-09-30, chưa cân):** thay 2 Gatebreaker ở
wave 21–29 (và wave thường sau 30); 1800 máu / 90 sát thương / tốc 0,65 / 0,55 đòn/s / giáp 20%.

```
Máu quái   = Máu gốc × (1 + 0.12 × wave)
Sát thương = Gốc × (1 + 0.08 × wave)
Số lượng   = 5 + floor(wave / 2)
```

| Wave | Thành phần |
|---|---|
| 1–3 | Goblin |
| 4–6 | Goblin + Ashfang + Chuột Cống |
| 7–9 | Goblin + Ashfang + Goblin Ná + Orc |
| 10 | BOSS + 6 Goblin hộ tống |
| 11–19 | Hỗn hợp (+ Dơi Quỷ bay cao), tỉ lệ Orc tăng dần · **từ wave 12: +1 Gatebreaker** (trừ wave 20) |
| 20 | BOSS Minotaur (chỉ số ×2) + **BOSS Pháp sư Orc** + 4 Orc |
| 21–29 | Hỗn hợp mật độ cao · **+2 Gatebreaker** |
| 30 | BOSS (chỉ số ×3.5) + 6 quái hỗn hợp hộ tống (wave boss luôn có quái nhỏ để boss buff — người dùng 2026-09-29) |
| 31+ | Lặp: 31–39 như 21–29, 40/50/… như wave 30 (boss +1,5 hệ số mỗi 10 wave) |

*Số in nghiêng ở hai dòng Goblin Ném Bình / Pháp sư Orc là số Claude đề xuất lúc code (2026-09-30), CHƯA cân bằng.*
*Rồng Ma Hỏa / Ma Hỏa: số Claude đề xuất lúc code (2026-10-01), CHƯA cân. Rồng THAY Minotaur ở wave 30 (giả định của Claude, chờ người dùng chốt).*

**Boss (cơ chế, buộc đổi cách chơi):** giáp dày giảm cố định 30% sát thương · *Chấn Địa* mỗi 15s
gây 80 dmg + choáng hero 2s · *Tiếng Thét* mỗi 20s tăng 30% tốc chạy mọi quái trong 5s · *Nổi Giận*
dưới 30% máu tăng 50% tốc đánh và miễn slow.

**Boss wave 20 — Pháp sư Orc lơ lửng (`OrcShaman`, chốt với người dùng 2026-09-29, tên và số CHƯA chốt).** Đi
cùng Vua Minotaur ở wave 20: Minotaur đánh gần, pháp sư đánh xa. Ngoại hình: orc già gù, áo choàng rách đỏ-tím bầm,
**trôi lơ lửng** (không thấy chân), gậy gỗ cháy treo sọ ngà với ngọn lửa lục-vàng độc; cao ~1,5× Gatebreaker.
Bốn chiêu: *Đánh thường tầm xa* — dừng ở tầm xa bắn cầu lửa độc, ưu tiên hero/lính, hết mục tiêu mới bắn tường
thành · *Miệng Hố Lửa* — mỗi ~12s đánh dấu vùng quanh cụm hero đông nhất, 1,5s sau nổ · *Khiên Phép* — mỗi ~18s
bọc khiên hấp thụ sát thương lên Minotaur/quái gần nhất · *Dịch Chuyển* — hero cận chiến áp sát thì biến mất, hiện
lại xa hơn về phía phải (hồi ~10s).

**Boss wave 30 — Rồng Ma Hỏa (`Dreadwyrm`, người dùng chốt kit 2026-09-30, tên và số CHƯA chốt; thay hay đi cùng
Minotaur CHƯA chốt).** Rồng xác sống: xương than đen, thịt/cánh da đỏ-tím bầm rách thủng, lửa ma lục-vàng (cùng lửa
Pháp sư Orc — pháp sư gọi nó dậy) cháy trong lồng ngực, hốc mắt, khoé mõm; một sừng gãy. Identity đã duyệt
(`tempart/Dreadwyrm/identity/`). **Bay là là sát đất nhưng tính như QUÁI MẶT ĐẤT:** bị luật chặn 3 quái giữ, cận chiến
đánh đủ (người dùng: boss bay thẳng vào thành thì không thắng được). Di chuyển = trôi ngang + vỗ cánh, không có walk chân.
*Xác Không Đau* (nội tại) — giảm 25% mọi sát thương; đòn thường cắn/vuốt quét trúng **cả 3 unit đang chặn** ·
*Hơi Thở Ma Hỏa* ~12s — ngóc đầu 0,5s báo trước, phun lửa dải dọc lane phía trước, để lại đất cháy 4s + *Thiêu Hồn*
(hero đứng trong lửa không hồi máu/mana) · *Vỗ Cánh Cuồng Phong* ~16s — dang cánh 0,5s báo trước, đập cánh hất lùi mọi
unit đang chặn ~1 ô · *Gọi Hồn Lửa* ~20s — gầm, 6 quái hộ tống bốc lửa ma +25% sát thương, chết nổ lửa nhỏ, **và sinh thêm 2–3 Ma Hỏa** (`SoulWisp`, tên mã tạm; người dùng chốt 2026-09-30: hồn lửa lơ lửng — sọ đen chạy lửa lục-vàng, đuôi lửa tan khói tím, không chân; quái nhỏ lao vào đánh lính/hero; số lượng, máu, sát thương CHƯA chốt) ·
*Nổi Giận* <30% máu — lửa chuyển cam-rực (đổi màu bằng code), +50% tốc đánh, miễn slow, Hơi Thở hồi nhanh gấp đôi.

## 6. Gacha

| Độ hiếm | Tỉ lệ | Ghi chú |
|---|---|---|
| N | 60% | Cơ bản |
| R | 30% | Chỉ số cao hơn ~40% |
| SR | 9% | Có kỹ năng mạnh |
| SSR | 1% | Cơ chế riêng |

Sau khi ra độ hiếm → chọn loại: 60% hero / 40% pháo.

**4 banner (2026-09-26):** *Tổng hợp* (chia loại như trên, `heroShare`/`soldierShare` trong `GachaConfig`) · *Hero* · *Lính* · *Trụ* (pháo) — banner riêng chỉ ra đúng loại đó, bậc trống thì hạ bậc trong cùng loại. Giá, tỉ lệ độ hiếm và pity **dùng chung** cả 4 banner.

- Quay 1: **500 vàng** · Quay 10: **4.500 vàng**
- **Pity mềm:** 30 lần liên tiếp không SR+ → lần 31 chắc chắn SR
- **Pity cứng:** 80 lần liên tiếp không SSR → lần 81 chắc chắn SSR
- Trùng: +20 mảnh; đủ 100 mảnh thì **người chơi bấm Nâng sao** (không tự lên khi quay — chốt 2026-09-28): −100 mảnh, +1 sao, **+15% máu và sát thương gốc** (cộng tuyến tính với +8%/level). Tối đa 5 sao (Claude đặt tạm)
- **Mở kỹ năng theo sao (2026-09-28):** có unit là dùng được **đánh thường + chiêu cuối**; **nội tại** mở ở 1★, **chiêu phụ** ở 3★ (người dùng chốt 2026-09-28; Wyrmspear cả hai chiêu phụ đều 3★). **Nâng level chỉ tăng chỉ số**, không mở kỹ năng — ngưỡng từng hero ở `HeroDefinition`. Hero bậc thấp (N/R) không có chiêu phụ, chỉ có chiêu cuối, nhưng **vẫn có nội tại** (người dùng sửa 2026-09-29) — xem mục 3 *Nội tại hero bậc thấp*. Wyrmspear: Long Giáp/Uy/Nộ/Khế là hiệu ứng của biến hình (chiêu cuối) nên không khoá
- Bộ đếm pity lưu vào save, **không reset khi thua**

## 7. Kinh tế

| Nguồn vàng | Công thức |
|---|---|
| Thắng wave | 80 + 18 × wave |
| Thắng wave boss (10/20/30) | +200 |
| Đăng nhập hằng ngày | 300 *(mở rộng)* |

**Chỉ THẮNG wave mới có vàng — diệt quái không ra vàng** (người dùng chốt 2026-09-29: thua mà vẫn có tiền từ quái
đã giết thì dễ quá). Thưởng thắng nâng lên để bù phần vàng quái cũ. Chơi hết 10 wave đầu ≈ 2.000 vàng ≈ 4 lượt quay.

Nâng level n → n+1: `50 × n^1.5` (làm tròn xuống). Mỗi level **+8% máu và +8% sát thương** so với
chỉ số gốc (cộng tuyến tính). **Trần level theo sao** (hero, lính, pháo — người dùng chốt 2026-09-29): 0–1★ lv 20,
mỗi sao sau +20 (2★ 40, 3★ 60, 4★ 80), **5★ không giới hạn** — chỉ bị chặn bởi vàng. `Progression.MaxLevel(stars)`.

## 7.1 Cấp người chơi + XP (người dùng chốt 2026-10-15 — ĐÃ CODE + ĐÃ BAKE, CHƯA Play)

HUD góc trái trên (GDD 10.0) có **chân dung + `Lv N` + thanh XP có số**. Toàn bộ số dưới đây là **TẠM** (Claude), nằm
trong `Meta/PlayerLevel.cs`:

- **XP chỉ đến từ việc ĐÁNH:** thắng wave = `20 + 4 × wave`, wave boss (chia hết cho 10) **+50**; thắng một trận
  Chinh Phạt = **60**. Rương AFK **không** cho XP (đúng chốt 2026-10-01: nó là "chờ", không phải "đánh").
- **Nguỡng lên cấp:** `100 + 50 × (cấp − 1)` — cấp 2 cần 100, cấp 3 cần 150, cấp 5 cần 250…
- **Thưởng mỗi cấp:** vàng `50 + 20 × cấp`; **mỗi 5 cấp tặng 1 lượt quay miễn phí** (dùng cho Quay 1, không dùng
  được cho Quay 10 ⇒ một vé không thành mười unit).
- **Không trừ gì khi thua**, không có trần cấp. Lên nhiều cấp trong một wave thì cộng dồn, ghi 1 dòng vào bảng kết quả.
- Vàng do `BattleHud` trả (nghe `PlayerLevel.LeveledUp`), **không** do lớp dữ liệu: `Meta` không được biết `Economy`.
- **Chưa làm:** avatar người chơi chọn từ hero đã sở hữu (đang dùng tạm `avatar-frostweaver`), XP từ nhiệm vụ/event.

## 7.2 Town · Cửa hàng · Bảng kỹ năng · chế độ phụ (2026-10-15 — ĐÃ CODE + BAKE, CHƯA Play)

Toàn bộ số dưới đây là **TẠM** (Claude), chưa cân. Mục đích chung: cuối game vàng chỉ có MỘT đường tiêu (nâng level
unit + nâng cấp thành) nên nó cứ chạy mãi không có chỗ đổ — ba thứ dưới đây là chỗ đổ thứ hai, cộng thêm mục tiêu
cho người đã qua wave 30.

**Town** (nút trên HUD sảnh, mở ra hub) — hiện có 2 công trình con:
- **Cửa hàng** (`Meta/Shop` + `UI/ShopScene.cs`) — **SCENE RIÊNG** từ 2026-10-20 (`SceneNames.Shop`), vào từ
  công trình Shop trong Town (không còn là bảng phụ trong HUD trận). Bốn loại hàng, mỗi loại một chu kỳ làm mới:
  **Daily** (4 ô: 3 mảnh + 1 trang bị, mỗi ngày) · **Weekly** (3 ô: 2 gói 15 mảnh + 1 trang bị, thứ Hai) ·
  **Monthly** (2 ô: 30 mảnh + 1 trang bị SSR, ngày 1) · **Offers** (3 gói 25 mảnh, mua MỘT lần, không xoay).
  Mảnh **chỉ bán cho unit người chơi ĐÃ sở hữu** (mảnh của unit chưa có thì chưa dùng được vào việc gì); trang bị
  chỉ bán từ **R** trở lên. Giá (TẠM): 5 mảnh = **150 / 300 / 700 / 1.400** vàng theo N/R/SR/SSR (gói nhiều mảnh
  tính tuyến tính); trang bị R/SR/SSR = **800 / 1.600 / 3.200**. Mỗi ô mua **một lần trong chu kỳ**; ô đã mua hiện
  "Sold" tới lần làm mới sau. Luật nằm MỘT chỗ ở `Meta/Shop`; màn hình chỉ vẽ + gọi `TryBuy`.
- **Bảng kỹ năng** (`Meta/SkillBoard`): 3 nhánh **vĩnh viễn**, mỗi nhánh 10 cấp — Sát thương **+2%/cấp**, Máu
  **+2%/cấp**, Tốc đánh **+1,5%/cấp**; giá cấp kế tiếp `300 + 500 × cấp`. Cộng **% toàn tài khoản**, áp ở đúng một
  chỗ (`Progression.Multipliers`) nên mọi hero, lính và pháo đều ăn; cộng CÙNG chỗ với level/sao/trang bị, không nhân chồng.

**Chế độ phụ trong sân trận** (dùng chung một thanh máu, **không đổi `NextWave`** — không phải tiến trình chính):
- **Boss Rush**: 3 hiệp boss liên tiếp (dùng lại wave 10/20/30), **không sửa thành giữa các hiệp**. Thưởng = vàng 3
  wave + **500**, XP 3 wave, và một lượt rơi trang bị theo luật wave 30 (chắc ≥ R).
- **Thử thách ngày** (`Meta/DailyChallenge`): wave `31 + ngày%11`; **ngày chẵn** quái +35% sát thương, **ngày lẻ**
  thêm 3 quân thường. Thưởng vàng + **2 mảnh**; **thắng mới khoá nút tới hôm sau**, thua thì được thử lại. Seed lấy
  từ NGÀY (không dùng `string.GetHashCode()` — hash chuỗi đổi theo tiến trình nên cùng ngày mở hai lần sẽ ra hai đề khác nhau).
- **Mốc sau wave 30** (`Meta/WaveMilestones`): mỗi 5 wave thưởng vàng `200 + 20×wave` + 1–2 mảnh (mốc tròn chục gấp
  đôi); ghi `highestWave` và báo khi lập kỷ lục mới.
- **Ưu tiên mục tiêu** (nút thứ 4 trên HUD trận, GDD 10.1 "auto/ưu tiên"): chọn cách hero nhắm đòn **đánh thường** —
  Gần nhất (mặc định, đúng hành vi cũ) · Xa nhất · Máu thấp nhất · Máu cao nhất. Chỉ áp cho đòn thường; các chiêu
  vẫn tự chọn mục tiêu theo kịch bản riêng.

**Gợi ý ngữ cảnh** (`Meta/ContextTips`): 5 gợi ý cho 5 phút đầu, mỗi gợi ý hiện **một lần trong đời acc**, điều kiện
đọc thẳng từ save nên acc cũ chỉ nhận đúng phần còn thiếu.

## 8. Phạm vi

**MVP — bắt buộc:** 30 wave theo công thức · lâu đài 3 tầng + 3 loại pháo · 4 hero với 4 kỹ năng ·
9 ô đặt hero · gacha 4 bậc + pity · nâng level hero/pháo · 3 loại
quái + boss có cơ chế · UI: menu chính, màn chơi, gacha, nâng cấp, kết quả · lưu vàng/sở hữu/
level/pity.

**Mở rộng (ưu tiên giảm dần):** trang bị · hero 5–8 và pháo mới · nâng cấp lâu đài · endless ·
âm thanh · particle nâng cao · leaderboard (Firebase/PlayFab, không tự viết backend).

**Tài nguyên ngoài trận (mở rộng, người dùng thêm 2026-09-11, chưa có số):** đây là đường để người chơi
mạnh lên sau khi thua một wave.
- **Phó bản công thành:** đi đánh lâu đài của NPC khác để lấy tài nguyên.
- **AFK:** thoát game rồi vào lại thì nhận tài nguyên theo thời gian vắng mặt.
- **Lính triệu hồi** (phe ta, theo tốp, để chặn quái): xem `docs/project-state/IDEAS.md`.

**Phó bản công thành "Chinh Phạt" + rương AFK — người dùng chốt hướng 2026-09-30, số là TẠM (Claude, chưa cân):**
- **Trận:** hai thành đối xứng. Thành mình bên trái như trận thường; **thành NPC bên phải** có hero đứng ô 3×3 trước
  thành, 3 pháo trên thành, lính NPC tràn sang trái thay cho quái. Lính mình tràn sang phải đánh thành NPC. Thành NPC
  về 0 máu thì thắng, thành mình về 0 thì thua.
- **Phe NPC = lãnh chúa người đối thủ:** dùng lại hero/lính/pháo của mình, lật quay trái, đánh dấu phe đỏ (thân
  phủ đỏ + thanh máu đỏ lúc chạy game; sprite chỉ đổi viền bằng script). Thành NPC = pháo đài mình lật + đổi màu cờ.
- **Chọn ải = bản đồ do thám:** vào từ **Tháp Do Thám** trên sân sảnh. Bản đồ vùng phủ sương, thành mình ở giữa. Bấm
  **Do Thám** → vòng quét lan ra, lộ **3 thành NPC**: Dễ (lực chiến ≈ 0,85× mình), Ngang (≈ 1,0×), Khó (≈ 1,2×), cả ba
  nhân thêm **(1 + 0,06 × chuỗi thắng)** → càng thắng liên tiếp đối thủ càng mạnh, thưởng càng to.
- **Xem trước thành:** bấm thành trên bản đồ → bảng: chân dung + tên lãnh chúa, **lực chiến** so với mình (xanh/vàng/đỏ),
  đội hình 3×3 hero (avatar), loại lính, 3 pháo, máu thành, thưởng (vàng, cơ hội rơi trang bị, mảnh), nút **Tấn công**.
- **Lực chiến** (TẠM): tổng mọi unit mình đang dùng, mỗi unit = chỉ số gốc × (1 + 0,08 × (lv − 1)) × hệ số sao.
- **Luật lượt** (TẠM): 5 lượt tấn công/ngày; mỗi thành đánh 1 lần; thắng → chuỗi +1, thua → chuỗi về 0; quét lại miễn
  phí mỗi 30 phút, quét ngay tốn vàng.
- **Rương AFK trên sân sảnh** (thay Hive): 3 hình theo thời gian tích luỹ — đóng (< 1 giờ), hé sáng (1–8 giờ), đầy tràn
  (≥ 8 giờ), **trần 12 giờ**; đồng hồ tích luỹ trên rương. Bấm → popup nhận: vàng + XP người chơi + cơ hội mảnh/trang
  bị, mức thưởng/giờ theo wave cao nhất đã thắng.
  **Code 2026-10-01 (số TẠM, Claude):** vàng/giờ = 0,5 × vàng thắng wave cao nhất (`nextWave − 1`, thua không lùi wave);
  mỗi tròn 2 giờ 25% được 1 mảnh cho một unit đang sở hữu (ngẫu nhiên); mỗi tròn 4 giờ một lượt rơi trang bị như thắng
  wave (tối đa 1 món/lần nhận). **Rương AFK KHÔNG cho XP** (giữ nguyên chốt 2026-10-01), nhưng **XP người chơi nay
  ĐÃ LÀM THẬT** (người dùng chốt 2026-10-15) — xem mục 7.1. Rương nằm dưới nút
  Đội quân, ẩn khi đang đánh. Số ở hằng trong `AfkChest.cs`.
- **Code bản đồ 2026-10-01 (số TẠM, Claude):** vào bằng nút "Phó bản" trên HUD sảnh (chưa có Tháp Do Thám trên sân). Lực chiến unit (sửa 2026-10-02) = √(máu × sát thương × tốc đánh), cả ba đã nhân hệ số level/sao/trang bị (lính × số con) — trung bình nhân để hệ số NPC ×k cho lực chiến đúng ×k (công thức cộng cũ: ×1,2 thực tế mạnh ×1,44); pháo = sát thương × tốc bắn × 3; mọi unit × hệ số phẩm N 1 / R 1,15 / SR 1,3 / SSR 1,5 (chỉ số gốc không đo hết bộ chiêu). NPC dùng đúng số pháo / số loại lính của mình lúc Do Thám, mỗi unit NPC **cùng phẩm** với unit tương ứng của mình. **Nhịp trận (2026-10-02):** trong Chinh Phạt mọi unit hai phe + cả hai thành máu ×2,5; thành NPC giữ **quân tiếp viện** 2 / 3 / 4 hero (Dễ / Ngang / Khó, đã tính vào lực chiến), ra từ cổng thành theo đợt 2 hero khi tuyến đầu NPC còn một nửa hoặc mỗi 25 giây. Chi viện (sửa 2026-10-02): **mỗi phe chỉ 1 hero chi viện mỗi trận**. Một đồng hồ chung hai phe điểm mỗi 10 giây: NPC ra hero tiếp viện từ cổng thành; phe ta trận tạm dừng, chọn 1 hero đã sở hữu chưa có trong trận, hero ra từ cổng thành mình (Huỷ = bỏ lượt, lượt sau hỏi lại). Bảng Đóng góp sau trận Chinh Phạt có tab Phe ta / Phe địch. NPC: một hệ số nhân máu + sát thương cho mọi hero/lính NPC, giải cho đúng lực chiến mục tiêu; sao hero NPC = sao trung bình đội mình. Quét ngay = vàng thắng wave hiện tại. Thưởng vàng × 0,8 / 1 / 1,4 theo độ khó × chuỗi thắng; mảnh 0 / 1 / 2. Số ở `Meta/ConquestMap.cs`.
- **Hero NPC (người dùng chốt 2026-10-01):** hero HAI PHE thật, có chiêu — nội tại / chiêu phụ mở theo **sao của chính
  hero NPC** như luật hero mình (GDD 6–7). Sao + level + sức mạnh NPC suy ra từ **lực chiến tổng của phe ta** (× hệ số
  Dễ/Ngang/Khó × chuỗi thắng ở trên). Không làm bản "NPC = quái chỉ đánh thường".

Trang bị bị đẩy khỏi MVP vì kéo theo stat cộng dồn + UI riêng + mở rộng cấu trúc save.
**Trang bị — đưa lại vào làm (người dùng, 2026-09-28):** chỉ hero, **4 ô** (Vũ khí · Giáp · Mũ · Nhẫn), **chỉ cộng chỉ số**, không đổi sprite; độ hiếm **N/R/SR/SSR** (màu như thẻ unit), chỉ số theo độ hiếm; **nâng cấp đồ** bằng vàng. Nguồn đầu tiên: **rơi khi thắng wave** (boss chắc rơi); sau: banner trang bị. **Số TẠM (Claude, chưa cân):** bậc N lv1 vũ khí +6% sát thương, giáp +8% máu, mũ +5% máu, nhẫn +4% tốc đánh; ×1 / 1,8 / 3 / 5 theo N/R/SR/SSR; +10%/level, tối đa lv 10; giá nâng 60 × hệ số độ hiếm × level; rơi 35% mỗi wave thường, wave 10/20/30 chắc rơi ≥R. Số nằm ở `EquipmentConfig`. Plan: `docs/project-state/IDEAS.md` #5.

## 9. Điểm còn TRỐNG — cần chốt trước khi sinh art tương ứng

- **Boss: ngoại hình ĐÃ CHỐT 2026-09-22, tên CHƯA.** Vua minotaur cùng giống Gatebreaker, **cao gấp đôi
  Gatebreaker**, giáp sắt đen, búa đá khổng lồ, một sừng gãy, vải chiến đỏ sẫm.
- ~~Elf làm "quái nhanh"~~ **ĐÃ CHỐT 2026-09-11: thay Elf bằng CHÓ SÓI 4 chân, tên `Ashfang`.**
  Lý do: elf mặc định không đọc ra là kẻ địch. Sói 4 chân là unit duy nhất trong roster không phải
  humanoid đứng thẳng nên silhouette tách ra tức thì — đúng thứ "quái nhanh" cần. Palette xám tro
  ngả ấm + accent đỏ máu (mắt, vòng cổ gai). Chi tiết ở AD-6.
- ~~Định dạng save~~ **ĐÃ CHỐT 2026-09-11: JSON file** `Application.persistentDataPath/save.json`
  (vàng, danh sách sở hữu + level/sao/mảnh, 2 bộ đếm pity). **Theo tài khoản (2026-09-26):** mỗi acc một file
  (`save.json` = acc chính, `save-<tên>.json`), đổi acc ở Admin (F1). Acc mới được **tặng Frostweaver**; acc `test`
  sở hữu mọi unit kể cả unit chưa vào gacha; acc `fresh` xoá làm lại mỗi lần chọn (xem lại lúc mới chơi).
- **Độ hiếm ĐÃ CHỐT 2026-09-11: gắn CỐ ĐỊNH vào từng unit.** Hero hiện tại đều N, riêng Pháp sư
  triệu hồi ở bậc cao hơn (đặt SR, sửa trong `Unit_mage_summoner.asset`); 3 pháo tạm N.
  **Định lại theo sức mạnh (người dùng giao Claude, 2026-09-26):** N = Ironblade, Hawkfletch, Frostweaver, Sói chiến,
  Kỳ nhông axit, 3 pháo · R = Cragfist, Rùa đá khiên · SR = Bonecaller, Chainwarden (vào pool 2026-09-26). Vespera (R) vào gacha
  khi xong bộ. Quay ra bậc chưa có unit (SSR hiện trống) thì hạ xuống bậc thấp hơn còn unit; pity đếm theo bậc QUAY RA.
  → Với roster hiện tại, tỉ lệ thực tế ≈ 90% N / 10% Pháp sư triệu hồi cho tới khi có unit R/SSR.
- **"Giới hạn vùng và số lượng" khi đặt hero** chưa có con số cụ thể (vùng rộng bao nhiêu, tối đa
  mấy hero trên sân).
- ~~Mana~~ **ĐÃ CHỐT 2026-09-12: thanh mana tích theo THỜI GIAN + theo ĐÒN ĐÁNH trúng địch, đầy
  thanh thì tự động cast, về 0.** Frostweaver: tối đa 6 (đúng cột Mana mục 3), hồi 0,35/giây, cộng
  0,25 cho mỗi con trúng đòn thường → đầy sau ~12 giây ở nhịp thường, ~7 giây khi splash trúng 3
  con. Chi tiết ở AD-11.
- ~~Hero bậc cao nhiều kỹ năng~~ **ĐÃ CHỐT 2026-09-12: kỹ năng chia HAI TẦNG.** Chiêu **lớn** chạy
  bằng thanh mana (có thanh trên đầu, đoán trước được). Chiêu **nhỏ** chạy bằng **cooldown ẩn** —
  không thanh, không tốn mana, bật ra như một nhịp phụ. Lý do chiêu nhỏ phải ẩn: 9 ô hero trên sân,
  mỗi hero mấy thanh thì màn hình không đọc nổi, mà người chơi cũng không thao tác được gì vì mọi
  thứ đều tự động. Chi tiết ở AD-11. Còn trống: mỗi hero bậc cao có bao nhiêu chiêu nhỏ và cooldown
  bao nhiêu giây.
- **Chiêu lớn của Cung thủ (Hawkfletch) ĐÃ CHỐT 2026-09-12: "Bắn Nhanh Liên Hoàn".** Thanh mana đầy
  → trong **5 giây** hero bắn gấp 3 tốc độ, mỗi phát vẫn là mũi tên thường (không đổi sát thương,
  không AOE). **Trong 5 giây buff thanh mana bị khoá** — không tích theo thời gian lẫn theo đòn trúng
  (sửa 2026-09-15: bắn ×3 mà vẫn tích thì chiêu gần như liên hoàn). Chọn phương án này thay vì mưa tên vì mưa tên giẫm lên vai "sát thương diện rộng" của
  Pháp sư nguyên tố. **Đã code 2026-09-15** (animation cũng chạy nhanh gấp 3). **Còn thiếu FX buff** nhỏ
  trên đầu hero (~20–28 px, 4 frame, xoáy đỏ-cam) — tạm thời tô hero màu cam trong lúc buff.

## 10. Giao diện (UI) — bố cục theo game mẫu (2026-09-15, CHƯA code)

**Tham chiếu:** *AFK Monster: Summon Legend TD* (Dino Go). Người dùng gửi 3 ảnh chụp + video gameplay
17,8 phút (`https://youtu.be/gtgGj8VzXLg`) để chỉ **bố cục nút bấm và cách chia màn hình**. Ảnh cắt từ
video (mỗi lần đổi màn một ảnh, tên file = giây) nằm NGOÀI repo ở `D:\Edu\Tool\video-tools\frames\`
và lưới tổng `sheet00–08.jpg` cùng thư mục; mốc thời gian ghi bên dưới dạng `@mm:ss`.
**Chỉ lấy bố cục, KHÔNG lấy art:** giữ nguyên pixel-art chibi hiện tại (`art-style.md`, Frostweaver
làm golden reference), giữ góc nhìn/sân dải chiều sâu ở mục 2. Khung UI trong ảnh là đá xám bo góc →
bản mình là khung pixel-art riêng (tạm dùng 9-slice `hud/` có sẵn tới khi đặt vẽ).

**Ánh xạ mục của game mẫu sang game mình:**

| Game mẫu | Game mình | Trạng thái |
|---|---|---|
| Heroes | Hero (mục 3) | Có |
| Monsters (quái phe ta) | **Lính triệu hồi phe ta** (theo tốp, chặn quái — `IDEAS.md` #1) | Người dùng chốt 2026-09-15 là cùng một cơ chế; lính chưa có số/art |
| Tower | Pháo (mục 4) | Có |
| Hive (căn cứ; trong video là công trình trên sân có đồng hồ AFK, và nâng Hive mới nâng được trần level unit) | Có thể là lâu đài + màn **AFK** (mục 8) | **Chưa chốt** |
| Nút *Summon* | Gacha (mục 6) | Có |
| Town (mở ở stage 10): Dungeon, Arena, Adventure, Forge, Mountain Shrine, Bounty Mission | Phó bản công thành / AFK (mục 8) phần nào | Mở rộng, chưa chốt |

**Luồng màn chính (video):** Sảnh chờ (chính là sân trận khi không có quái) → bấm *Battle* → Trận →
Victory → quay về sảnh. Mọi màn khác mở từ sảnh dưới dạng màn phủ/ popup có nút X đỏ góc phải trên.

### 10.0 Sảnh chờ (sân trận lúc nghỉ giữa wave) — @0:06, @3:13, @15:15
Game mình đã có trạng thái này (mục 1: hết wave thì dừng, người chơi nâng cấp rồi bấm đánh tiếp) —
sảnh chờ là **cùng một scene Battle**, chỉ đổi HUD.
- **Trái trên:** avatar + `Lv` người chơi + thanh kinh nghiệm có số (`27/16,400`).
- **Giữa trên:** băng tên *Stage N* + thanh tiến độ vàng + icon đầu lâu ở mép phải (mốc boss).
- **Phải trên:** vàng · đá quý + nút `+` xanh · nút menu (≡). Sau khi mở Town: biển *Town* dưới đó.
- **Phải giữa sân:** biển gỗ **Battle** (nút đánh wave kế tiếp) — nằm TRÊN sân, không phải thanh HUD.
- **Giữa dưới sân:** công trình **Hive** + đồng hồ AFK tích luỹ (`00:05:03`) — bấm để nhận.
- **Hàng icon trái dưới:** Shop · Quest · Bag · Event · Rewards · Starter Pack (chấm đỏ = có việc).
- **Phải dưới:** **Summon** (cổng tím) · **Army** (mở màn danh sách 10.2).
- Hội thoại hướng dẫn: khung chữ trắng dưới màn + chân dung NPC bên cạnh (@1:53); cutscene thì viền đen trên dưới (@1:46).

### 10.1 Màn trận (HUD) — ảnh người dùng + @12:57, @16:55
- **Góc trái trên:** avatar người chơi + tên + thanh máu căn cứ có số.
- **Giữa trên:** 4 nút vuông cùng cỡ: cờ (đầu hàng) · tua nhanh · auto/ưu tiên · thoát. Thoát mở popup
  giấy *Quit the battle* với **No** đỏ / **Yes** xanh (@16:55).
- **Góc phải trên:** khung tên stage + thanh tiến độ; **wave boss** thì đổi thành tên boss + thanh máu boss (@12:57 *Tupoc*).
- **Góc phải dưới:** 2 thẻ phép lớn (icon + số đếm). Ở game mình dự kiến là
  nút **triệu hồi tốp lính** (tốn tài nguyên lính) — chưa chốt.
- **Trên sân:** vòng tiến độ nhỏ ở ô hero, số sát thương bay lên, thanh máu nhỏ trên đầu quái.
- **Victory** (@4:08, @8:47): nền tối, băng rôn *VICTORY* giữa, dưới là hàng icon phần thưởng + số
  (điểm vương miện, vàng), nút nhân thưởng xem quảng cáo góc trái dưới, *Tap to continue* ở đáy.

### 10.2 Màn danh sách (Hero / Lính / Pháo — dùng chung một khuôn)
- Khung viền toàn màn, **tiêu đề giữa trên**, **nút lọc góc trái trên**, **nút X góc phải trên**.
- **Lưới thẻ cuộn dọc** (~7 cột ở màn ngang). Mỗi thẻ: khung màu theo **độ hiếm**, ảnh unit, `Lv N`,
  thanh **mảnh** (vd `0/100`, mục 6), hàng **sao**, icon **vai trò** gắn trên đỉnh khung.
- Unit **chưa sở hữu**: thẻ xám toàn bộ, vẫn hiện hình để người chơi biết có gì.
- **Thanh tab dưới đáy**, tab đang mở nổi lên. Game mẫu: Hive · Heroes · Monsters · Tower.
  Bộ tab của game mình **chưa chốt** (Claude đề xuất: Trận · Hero · Lính · Pháo · Triệu hồi).

### 10.3 Màn chi tiết một unit
- **Cột trái:** ảnh unit lớn trên nền, tên + loại, sao, icon thuộc tính; dưới: `Level N` + hàng chỉ số
  (máu, sát thương, …); đáy trái: nút **Nâng level** (giá vàng, mục 7) và **Nâng sao** (thanh mảnh).
- **Cột giữa:** bảng mô tả kỹ năng (vd lính: thời gian sống, cỡ tốp, giá triệu hồi, hồi chiêu),
  số trong mô tả tô màu nổi; dưới là ô gắn thêm (game mẫu: *Spell*) — game mình chưa có tương đương.
- **Cột phải:** lưới danh sách thu nhỏ để đổi sang unit khác mà không đóng màn.
- **Hàng nút tròn dưới cột giữa:** game mẫu có Skill · Talent · Quest — game mình mới có Kỹ năng.
- **Giữa trên:** thanh tiền tệ (vàng, đá quý) + nút `+`.
- **Khác nhau theo loại (video):**
  - *Hero* (@2:18, @5:37, @15:27): nút đáy trái là **Summon** khi chưa có, **Level Up** khi đã có;
    hàng nút tròn thêm **Equipment**. Cột giữa đổi theo nút: *Skill* = bảng **ULTI** + các ô chiêu nhỏ
    + nút *Reset*; *Equipment* = lưới 4 ô trang bị + nút **Upgrade** kèm giá nguyên liệu.
    Nâng sao thành công: ngôi sao vàng lớn loé phủ cột trái (@15:29).
  - *Monster/lính* (@0:35, @5:56): hai ô số trên đầu bảng = **số con mỗi lần triệu hồi** + **hồi chiêu**;
    dòng *Life Duration* (thời gian sống); dưới là ô *Spell*. → khớp thẳng với lính triệu hồi (IDEAS #1).
  - *Tower/pháo* (@3:07, @16:35): chỉ có mô tả + chỉ số, không có hàng nút tròn; danh sách chỉ ~7 ô.
  - Ô chưa đủ điều kiện nâng: chữ báo nổi giữa màn, vd *Not enough Gold* (@5:48), *Reach the Max
    Level of the Hive…* (@10:40).

### 10.4 Các màn phụ (mở từ sảnh chờ)
- **Summon / gacha** (@7:33): nền hang tối, chân dung NPC bên trái; **gói nổi bật** ở giữa (vòng phép
  + thẻ lớn, tên *Fortune Package*, nút **Buy** giá đá quý); cột phải xếp dọc các gói nhỏ: *Common*
  (miễn phí theo đồng hồ `03:44:45`), *Silver* (80 đá), *Gold* (120 đá). → game mình: Quay 1 / Quay 10 (mục 6).
- **Shop** (@0:10): mái bạt sọc + NPC bên trái; khu kệ bên phải chia mục *Daily Deal*, *Equipment
  Chest*; thanh tab đáy *Gem · Special Pack · Monthly Card · Item*; mức VIP góc trái trên. Mua xong:
  popup giấy *Rewards* + nút **Ok** xanh (@0:15).
- **Daily Quests** (@1:57): popup khung gỗ, mỗi dòng = icon + mô tả + thanh tiến độ + nút **Claim**
  xanh / dấu tick khi đã nhận; dòng đầu *Complete all quests* là quest tổng; tab dọc bên trái.
- **Bản đồ stage** (@2:13, @16:25): các cột *Stage 1…4* nằm ngang trên nền rừng, mỗi cột có quái
  đặc trưng + ô *Rewards* + nút **Claim**/tick; thanh tiến độ vàng chạy dọc đáy với mốc điểm.
- **Starter Pack** (@1:44): popup nhân vật lớn hai bên, hàng quà, đồng hồ *Ends in*, nút giá.
- **Town** (@14:06, @15:02): bản đồ đảo, mỗi công trình là một nút có biển tên; công trình khoá hiện
  *Unlock at Stage 10*; nút *Back To Battle* góc phải dưới. Con: *Mountain Shrine* → chọn **Skill
  Board** (sách nâng chỉ số toàn cục) / **Artifact** (3 ô gắn + lưới bộ sưu tập); *Bounty Mission* →
  thẻ nhiệm vụ AFK 2 giờ, chọn tối đa 6 hero, *Auto-fill* / *Start*.

**Đã chốt (người dùng, 2026-09-15):** làm **sảnh chờ + HUD trận (10.0 + 10.1) trước**; bộ tab đáy =
**Trận · Hero · Lính · Pháo · Triệu hồi**; đưa lên HUD: **Lv + XP người chơi**, **đá quý**, **Hive = nút
AFK**, **hàng icon Shop/Quest/Bag/Event/Rewards/Starter Pack** (lượt này chỉ đặt nút, chưa có màn).
Art UI giao lane agy: icon + minh hoạ do image model, khung/chrome dựng bằng script.

**Còn trống:** 2 nút giữa trên của HUD; thẻ góc phải dưới là triệu hồi lính hay kỹ năng người chơi;
số liệu Lv/XP, đá quý, AFK; có làm Shop, Daily Quests, bản đồ stage, Town thành màn thật hay không —
hiện GDD chưa có mục nào trong số này (mục 8 chỉ có menu chính, màn chơi, gacha, nâng cấp, kết quả). Mục 8 "UI: menu chính, màn chơi, gacha,
nâng cấp, kết quả" giờ ánh xạ vào 10.1 (màn chơi), 10.2 + 10.3 (nâng cấp).

## 11. Nhiệm vụ (2026-10-15 — track "Hành trình" ĐÃ CODE, chờ chạy builder + Play)

**Vì sao có hệ này (đo trong code, không phải cảm tính):** acc mới chỉ có **1 hero** (`SaveSystem.StarterUnitId`)
trong khi sân có **9 ô hero**, và 10 wave đầu chỉ ≈ 2.000 vàng ≈ **4 lượt quay**. Nên "dễ chơi" ở đây là
**lấp roster sớm + chỉ cho người chơi biết làm gì tiếp**, không phải thêm tiền tệ.

**Track đã làm — *Hành trình* (tân thủ):** 7 mốc × (3 nhiệm vụ + 1 ô thưởng cuối mốc). Mốc N+1 chỉ mở khi đã
**nhận hết thưởng** mốc N (kể cả ô thưởng cuối mốc). Mốc 2/3/4 tặng **hero cố định** (Archer → Knight →
Cragfist); mốc 5 tặng **vé chọn SR (1 trong 4)**; mốc 7 tặng **vé chọn SSR (1 trong 8)**.
Chưa làm: nhiệm vụ **Ngày** (điểm hoạt động + rương mốc), **Tuần**, **Thành tựu** — ý tưởng ở `IDEAS.md`.

**Luật:**
- Tiến độ đọc thẳng từ save (wave cao nhất · số hero · level hero cao nhất · 3 bộ đếm quay/Chinh Phạt/rương AFK)
  ⇒ **hồi tố**: làm trước khi nhiệm vụ mở khoá thì lúc mở nó đã xong. Không lưu tiến độ từng nhiệm vụ.
- Mốc **không hết hạn**, **không cần đăng nhập liên tiếp** (khác login-track của Arknights).
- Tặng tướng: chưa sở hữu → **nguyên con**; đã sở hữu → **quy ra mảnh** (N/R 20 · SR 60 · SSR 100) — không có
  phần thưởng vô dụng vì đã có hero.
- **Vé chọn tướng** nằm trong save tới khi người chơi bấm chọn (nút *Later* để chọn sau) — không mất.
- Vàng cộng qua `Economy` (để HUD nghe `Changed`); tướng/mảnh/vé ghi thẳng trong `QuestLog.Claim`.

**Code:** `Data/QuestCatalog.cs` (nội dung + bảng vé) · `Meta/QuestLog.cs` (+ `QuestSave` trong `SaveData.quests`:
`claimed` · `vouchers` · `gachaRolls` · `conquestWins` · `afkClaims` · `lastHero`) · `UI/QuestScreen.cs`
(panel trong scene Battle, mở từ nút **Quests** trên HUD, có chấm đỏ). Bộ đếm do `GachaSystem.Roll` /
`ConquestMap.Finish` / `AfkChest.Claim` gọi. UI dựng bằng chrome có sẵn — **không sinh art mới**.
Số đều là **TẠM** (Claude), chưa cân.
