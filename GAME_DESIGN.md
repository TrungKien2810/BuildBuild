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

## 4. Pháo (3 loại MVP)

| Pháo | Sát thương | Tốc bắn | Tầm | Đặc điểm |
|---|---|---|---|---|
| Liên Thanh | 12 | 2.0/s | 8 | Đơn mục tiêu, ổn định |
| Nổ | 30 | 0.5/s | 7 | AOE bán kính 2 ô |
| Băng | 8 | 1.0/s | 7 | Làm chậm 40% trong 3s |

## 5. Quái

| Loại | Tên | Máu | Sát thương | Tốc độ | Vàng |
|---|---|---|---|---|---|
| Thường | Goblin | 100 | 15 | 1.5 | 5 |
| Nhanh | Ashfang (chó sói) | 60 | 10 | 3.0 | 4 |
| Tank | Orc | 400 | 40 | 0.8 | 12 |
| Nhanh | Chuột Cống (`RatRunner`) | 45 | 8 | 3.5 | 3 |
| **Bắn xa** | **Goblin Ná** | — | — | — | — |
| **Tinh anh** | **Gatebreaker** | **1200** | **60** | **0.7** | **40** |
| Boss | *Vua minotaur (chưa đặt tên)* | 5000 | 120 | 0.6 | 200 |
| Boss wave 20 | *Pháp sư Orc lơ lửng (`OrcShaman`, chưa đặt tên)* | — | — | — | — |

**Nhanh `Chuột Cống` (thêm 2026-09-29):** chuột cống cầm dao găm, con nhanh thứ hai bên cạnh Ashfang — máu giấy, chạy nhanh hơn Ashfang. Bốn con số là **đề xuất của Claude, chưa cân bằng**.

**Bắn xa `Goblin Ná` (thêm 2026-09-21, người dùng chốt):** goblin cầm ná cao su chữ Y, bắn viên đá. **Ưu tiên bắn hero và lính phe ta** trong tầm; **hết mục tiêu mới bắn tường thành**. Chỉ số, tầm và wave xuất hiện chưa chốt. Art: identity đang sinh (`tempart/GoblinSlinger/`).

**Tinh anh `Gatebreaker` (thêm 2026-09-12):** hạng quái giữa Orc và boss — cùng dòng giống với boss,
đọc ra như "lính của nó". Vác một cánh cổng thành gỗ đã vỡ: **giảm 15% sát thương** (nửa của boss).
Xuất hiện **từ wave 12, 1 con mỗi wave; wave 21+ thì 2 con**; **không** xuất hiện ở wave boss
(10/20/30) để không loãng khoảnh khắc boss. Bốn con số máu/sát thương/tốc/vàng ở trên là **giá trị
khởi điểm tôi đề xuất, chưa qua cân bằng** — chỉnh trong ScriptableObject khi chơi thử.
Art identity ĐÃ CÓ (`tempart/Gatebreaker/identity/`, 94×81 px), còn 5 action.
**Ruinjaw** (2026-09-13): bản vẽ từng dự định làm boss, được chấm là "Gatebreaker nâng cấp một bậc" →
dùng làm **dạng nâng cấp của Gatebreaker**, không phải boss. Cách xuất hiện và chỉ số chưa chốt.

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
| 11–19 | Hỗn hợp, tỉ lệ Orc tăng dần · **từ wave 12: +1 Gatebreaker** (trừ wave 20) |
| 20 | BOSS Minotaur (chỉ số ×2) + **BOSS Pháp sư Orc** + 4 Orc |
| 21–29 | Hỗn hợp mật độ cao · **+2 Gatebreaker** |
| 30 | BOSS (chỉ số ×3.5) + 6 quái hỗn hợp hộ tống (wave boss luôn có quái nhỏ để boss buff — người dùng 2026-09-29) |
| 31+ | Lặp: 31–39 như 21–29, 40/50/… như wave 30 (boss +1,5 hệ số mỗi 10 wave) |

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
