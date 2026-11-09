# Build Build — Tài liệu tổng dự án

> Tài liệu tổng quan để người mới (dev, art, tester) nắm dự án trong một lượt đọc.
> Chi tiết số liệu và quyết định thiết kế nằm ở [`GAME_DESIGN.md`](../GAME_DESIGN.md) — đó là **nguồn sự thật** về thiết kế.
> Số liệu cân bằng trong tài liệu này là giá trị khởi điểm, đa số đang là số TẠM và nằm trong ScriptableObject.

---

## 1. Giới thiệu

**Build Build** là game **Tower Defense + Gacha** (phong cách pixel art fantasy), làm bằng Unity 6 / C#.
Người chơi bảo vệ một pháo đài đá trước các wave quái, dùng vàng quay gacha ra hero / lính / pháo, nâng cấp rồi đánh wave khó hơn.

- Nền tảng: PC (Windows), thiết kế sẵn để mở rộng sang mobile (có hỗ trợ Back key / cảm ứng).
- Quy mô: đồ án nhóm, ~3 người, giai đoạn MVP mở rộng.
- Ngôn ngữ giao diện trong game: tiếng Anh. Tài liệu / comment code: tiếng Việt.

## 2. Vòng lặp chơi

1. Xếp hero vào **9 ô** trước pháo đài, lắp **pháo** lên các bệ, chọn **lính** ở bãi sau thành.
2. Bấm **Fight Wave N** → quái ra theo wave, hero tự đi tìm quái và đánh, pháo tự bắn, lính triệu hồi hỗ trợ.
3. **Thắng wave** → nhận vàng + XP + (có thể) rơi trang bị. Trận dừng, người chơi nâng cấp rồi mới đánh tiếp.
4. **Thua** (máu thành về 0) → dừng đúng wave đó, **không mất gì** (vàng, level, pity đều giữ), đánh lại sau khi mạnh hơn.
5. Không giới hạn wave; **boss mỗi 10 wave**.

## 3. Hệ thống chính

| Hệ thống | Tóm tắt |
|---|---|
| **Hero** | 4 vai trò (Tank / Cận chiến / Xạ thủ / Pháp sư, cộng Hỗ trợ & Hồi máu). Mỗi hero có đánh thường, chiêu cuối, nội tại (mở ở 1★), chiêu phụ (3★). Hero tự nhắm quái gần nhất, có dây xích `leashRange` quanh ô. |
| **Pháo** | Gắn trên bệ thành, tự bắn: Liên Thanh, Nổ, Băng, Nỏ Xuyên, Lưới, Cối, Tháp Sét, Đại Bác, Gatling, Long Viêm (SSR)… |
| **Lính** | Triệu hồi từ bãi sau thành, có vòng hồi chiêu; chặn quái như hero. |
| **Gacha** | Quay 1 = 500 vàng, quay 10 = 4.500. Tỉ lệ N60 / R30 / SR9 / SSR1. Pity mềm 30 (chắc SR), pity cứng 80 (chắc SSR). 4 banner: Tổng hợp / Hero / Lính / Trụ. |
| **Tiến triển** | Level (`50×n^1.5` vàng/cấp, +8% máu & sát thương), sao (mảnh trùng, +15%), trần level theo sao, trang bị rơi từ wave, Bảng kỹ năng vĩnh viễn (3 nhánh × 10 cấp). |
| **Thành** | 3 cấp (Đồn Đá Biên → Thành Đá Lam → Thành Ấn Rune), mở thêm bệ pháo, tăng HP; mở khoá khi thắng wave 10 / 20. |
| **Cấp người chơi** | XP từ việc đánh (wave, Chinh Phạt); lên cấp thưởng vàng, mỗi 5 cấp tặng 1 lượt quay. Có màn **Hồ sơ người chơi**. |
| **Town** | Hub: **Cửa hàng** (scene riêng; Daily / Weekly / Monthly / Offers) và **Bảng kỹ năng**. |
| **Nhiệm vụ** | Track "Hành trình" + nhiệm vụ ngày; nhận thưởng qua `ResultPopup`. |
| **Rương AFK** | Tích vàng khi offline, tối đa 12 giờ. |
| **Chế độ phụ** | **Boss Rush** (3 boss liên tiếp), **Thử thách ngày**, mốc sau wave 30. |
| **Chinh Phạt** | PvE đối xứng: đánh thành NPC trên bản đồ Do Thám (xem mục 4). |
| **Tài khoản** | Đăng nhập cục bộ, save theo từng tài khoản (ghi nguyên tử). |

## 4. Chinh Phạt (chế độ đối kháng với thành NPC)

- Mở từ nút **Conquest** trên HUD sảnh → **bản đồ Do Thám**: quét để lộ 3 thành NPC (Dễ / Ngang / Khó), mỗi thành có đội hình, lực chiến, thưởng riêng. Mỗi ngày có số lượt tấn công giới hạn và chuỗi thắng.
- Vào trận: camera lùi ra nền "Chiến Địa Biên Giới", dựng **thành NPC đối xứng** qua tâm màn hình. Máu mọi unit và hai thành nhân ×2,5.
- **Quân chi viện**: một đồng hồ chung hai phe, mỗi 10 giây. NPC thả hero dự bị từ cổng thành; phe ta **tạm dừng trận và chọn 1 hero** (đã sở hữu, chưa có trong đội hình) ra từ cổng thành mình. Mỗi phe chỉ 1 hero chi viện mỗi trận. Không còn hero nào để chi viện thì bỏ qua lượt, không có bảng chọn, trận chạy bình thường.
- Hạ thành NPC = thắng (vàng theo độ khó × chuỗi thắng, mảnh, XP 60). Thành mình sụp, hoặc Surrender = thua.
- **Sau trận**: bảng Victory/Defeat và bảng **Stats (Đóng góp, có tab Phe ta / Phe địch)** hiện **trên nền Chiến Địa**; đóng bảng kết quả mới dỡ nền + thành NPC và quay lại bản đồ Do Thám.
- Cần ít nhất 1 hero trong đội hình mới bấm tấn công được (nút Start chặn khi sân trống).

## 5. Kiến trúc mã nguồn

Unity project, assembly `BuildBuild` (`Assets/Scripts/BuildBuild.asmdef`), 4 tầng:

| Thư mục | Vai trò | Ví dụ |
|---|---|---|
| `Core/` | Trận đấu, vòng đời wave, sân, thành | `WaveManager`, `ConquestMatch`, `ConquestArena`, `Castle`, `Health`, `Battlefield`, `WaveStats`, `Economy`, `Sfx`, `GameSpeed` |
| `Combat/` | Unit và hành vi chiến đấu | `Hero` (+ các file partial theo nhóm hero), `Enemy`, `Boss`, `Cannon`, `DragonBreath`, `SoldierCamp` |
| `Data/` | ScriptableObject định nghĩa | `HeroDefinition`, `CannonDefinition`, `SoldierDefinition`, `WaveTuning`, `GachaConfig`, các `*Kit` |
| `Meta/` | Luật ngoài trận + lưu trữ (**không biết UI**) | `SaveSystem`, `Accounts`, `GachaSystem`, `Progression`, `PlayerLevel`, `Shop`, `SkillBoard`, `ConquestMap`, `QuestLog`, `AfkChest`, `Tutorial` |
| `UI/` | Màn hình, HUD, popup | `BattleHud`, `HeroBoard`, `CannonBoard`, `ResultPopup`, `ContributionPanel`, `ConquestMapScreen`, `PlayerProfileScreen`, `ShopScene`, `BackKey` |

Nguyên tắc: `Meta` không phụ thuộc `Economy`/UI (UI nghe sự kiện của Meta rồi tự cộng vàng); luật mỗi hệ thống nằm **một chỗ**, màn hình chỉ vẽ và gọi.

**Scene** (`Assets/Scenes`, thứ tự build): `Login` → `Battle` (sảnh + trận) → `Gacha`, `Units`, `Shop`. Tên scene dùng hằng trong `Core/SceneNames.cs`.

**Dữ liệu**: ~170 asset ScriptableObject ở `Assets/Data` (hero, quái, pháo, lính, animation, config).

**Dựng scene bằng code**: `Assets/Editor/SceneBuilder*.cs` sinh toàn bộ UI/scene (menu Unity). Sửa bố cục → sửa builder rồi **rebuild scene**, không chỉnh tay trong scene. Lưu ý: rebuild scene Battle cần lưu scene trước (hộp thoại save sẽ chặn).

## 6. Luồng kết thúc wave (quan trọng khi sửa)

1. `WaveManager` phát `WaveCleared` / `Defeated`.
2. `HeroBoard`, `CannonBoard`, `SoldierCamp`, `WaveStats` nghe sự kiện để dọn trận, chụp thống kê.
3. `BattleHud` hiện `ResultPopup`. Nút **Stats** mở `ContributionPanel`, đóng thì quay lại popup.
4. Đóng popup (`Closed`): với Chinh Phạt thì `LeaveConquest()` dỡ nền/thành NPC rồi mở lại bản đồ Do Thám; với wave thường thì chuyển biome nếu cần.

> Từ 2026-11-09, Chinh Phạt **không** tự dỡ nền khi kết thúc trận; `BattleHud` dỡ khi đóng popup (nếu không có popup thì dỡ ngay). Thoát scene giữa trận vẫn dọn qua `WaveManager.OnDisable`.

## 7. Cách chạy và build

**Chạy trong Editor**: mở project bằng Unity 6, mở scene `Assets/Scenes/Login.unity`, bấm Play. Có thể dùng tài khoản seed hoặc đăng ký mới. Panel **ADMIN** (góc trái dưới) dùng để debug: nhảy wave, vào Chinh Phạt thẳng, giết hết quái…

**Build bản chạy thật (Windows)**: `File → Build Profiles → Windows`, scene theo thứ tự `Login, Battle, Gacha, Units, Shop`, xuất ra `Build/BuildBuild.exe` (thư mục `Build/` đã nằm trong `.gitignore`). Bản build hiện **vẫn chứa panel ADMIN** — cân nhắc ẩn trước khi phát hành.

**Kiểm tra biên dịch ngoài Editor**: `dotnet build BuildBuild.csproj` và `BuildBuild.Editor.csproj`.

## 8. Quy ước làm việc

- Nhánh làm việc: `dev`; `main` là nhánh ổn định. Commit theo kiểu `type(scope): mô tả` (tiếng Việt không dấu).
- Art đã duyệt nằm ở `Assets/Art/Generated`; ảnh thô của pipeline ở `tempart/` (không commit).
- Số liệu cân bằng đặt trong ScriptableObject, không hard-code.
- Doc nội bộ của governance ở `docs/project-state/` (đã gitignore): `HANDOFF.md` (trạng thái hiện tại), `decisions-log.md`, `technical-design.md`, `conventions.md`.

## 9. Trạng thái và việc còn lại

- Đã có: vòng lặp wave/boss, gacha 4 banner, hero/pháo/lính, thành 3 cấp, Town/Shop/Bảng kỹ năng, nhiệm vụ, Boss Rush, Daily, Chinh Phạt, hồ sơ người chơi, tài khoản cục bộ.
- Số cân bằng phần lớn là **TẠM**, cần playtest để chốt.
- Chưa làm / ý tưởng: avatar người chơi chọn từ hero sở hữu, XP từ nhiệm vụ/event, bản mobile, ẩn ADMIN ở bản phát hành.
- Cảnh báo kỹ thuật còn lại: vài API Unity đã obsolete (`GetInstanceID`, `FindObjectsSortMode`) — chỉ là warning.

## 10. Tài liệu liên quan

- [`GAME_DESIGN.md`](../GAME_DESIGN.md) — thiết kế chi tiết: hero, pháo, quái, gacha, kinh tế, UI, nhiệm vụ.
- `docs/project-state/art-style.md`, `art-pipeline.md` — định hướng và quy trình art (cục bộ).
