# Nghiên cứu cải thiện combat — 2026-10-21

Trạng thái: đã triển khai bản thử feedback và kiểm tra Unity Play (2026-10-21). Người dùng chốt bỏ vệt cyan, giữ burst trắng + thân unit nhận đòn lóe trắng. Mục tiêu là cảm giác đòn trúng rõ, giữ được khả năng đọc trận đông unit. Không đổi damage, tốc đánh hay luật chặn. Phần nghiên cứu bên dưới ghi nhận code tại checkpoint trước sửa; kết quả triển khai ở mục tiếp theo.

## Bản thử đã triển khai

### Mở rộng rung camera và âm thanh (cùng ngày)

- Đã thêm rung nhỏ 180ms thực khi **Xạ Nhật Hậu Nghệ**, **Họa Long Điểm Tinh Ngự Bút Sư**, **đại liên nổ Hồng Liên** thật sự làm mất HP. Trấn Sơn Ấn và chiêu phụ kích nổ sen có nhịp nhẹ hơn. Đánh thường không rung; miễn sát thương/shield-only không kích hoạt rung.
- Một lần gọi sau vòng damage cho cả skill; các impact đồng thời chỉ lấy lực mạnh nhất, không cộng lực hoặc kéo dài pulse. Khoảng nghỉ toàn camera350ms. Dao động tắt dần, không dùng Random chung của gameplay.
- `CombatCameraImpulse` chỉ dịch projection trong begin/endCameraRendering của URP rồi ResetProjectionMatrix; camera transform không thay đổi, projection lấy lại theo zoom/aspect hiện tại. HUD Battle đang ScreenSpaceOverlay nên giữ vị trí. Không thêm timeScale/hit-stop; pause không rung và không chạy đồng hồ pulse.
- SFX có sẵn: Ngự Bút Sư Hoành Bút `spell`, Trấn Sơn Ấn `skill-stone`, rồng trúng `explode`; Hậu Nghệ Cửu Tiễn và Xạ Nhật có `shot-bow` lúc nhả, Xạ Nhật trúng có `hit-arrow`; nụ sen Hồng Liên nổ có `explode`. Dùng limiter16 voices/khoảng cách cùng ID60ms của Sfx, không thêm asset audio. Chưa phủ tiếng toàn roster.
- Menu Play `BuildBuild/Combat/Preview heavy camera impact` thử nhanh nhịp rung; toggle Enhanced A-B tắt cả rung và tiếng mới. Menu preview không cần gây damage, còn các call site skill thật đều kiểm mất HP trước khi rung.
- Kiểm tra: build0 lỗi/22 warning có sẵn; Unity Console0 error. 20 request cùng thời điểm không stack; callback URP thực sự dịch hình, trả projection/transform đúng sau render; pause, x3, cooldown, A-B, zoom và disable qua smoke checks. Test trên component/asset thật của cả3hero: hit làm mấtHP rung, invulnerable không rung; Hậu Nghệ không rung ở release (enumerator kiểm bằng tiến bước thủ công, chưa đo thời gian bay bằng video). 11pha render đo có dịch hình ở pha1–8, về hình gốc pha9–10.
- GIF kiểm tra camera tại `tempart/combat-impulse-20261021/camera-impact-preview.gif` dùng các pha render điều khiển, biên độ thật, không phải video trận đấu. Save đã backup/phục hồi theo hash. Chưa profile GPU/thiết bị hoặc đánh giá âm lượng chủ quan/toàn trận đông.

- `Health.Impacted(Vector2)` phát khi đòn direct thật sự làm mất HP; không phát với fully blocked, periodic, reflected, linked hoặc source chính mình. `TakeDamage` nhận hướng va chạm tùy chọn; projectile giữ vector bay trước impact/pierce/arc. Không đổi phép tính damage và event cũ.
- `CombatFeedback` thêm silhouette mesh bám sprite/UV/pivot/flip/sorting của thân, shader lấy alpha và xuất trắng, opacity 75%, 65ms thực; không thay material/tint gốc. Mesh sprite được cache để không dựng lại mỗi frame. Lớp trắng nằm ngay trên thân; status/tint gốc tiếp tục chạy.
- `HitSpark`: burst sao trắng tại giữa thân, khoảng 160ms thực, cap 48 burst đồng thời, giới hạn trên mỗi nạn nhân 90ms. Pool tái sử dụng object. **Không có vệt cyan hoặc vệt màu sau chỉnh theo người dùng**; không xóa FX nguyên tố riêng có sẵn của hero.
- Số damage địch trắng ngà, viền tối bốn góc; giữ pop hiện có, tuổi thọ 0,8 giây thực ở x1/x3 khi Enhanced bật; pool giữ tối đa 96 số đã hết hạn. Pause timeScale=0 đóng băng presentation. Không thêm hit-stop hoặc ngắt animation cast; rung camera giới hạn đã bổ sung ở mục trên.
- `Hero.HitDelay` tính `clip.frameSeconds` khi có override, khớp thời lượng mà SpriteAnimator sử dụng. Âm thanh chiêu phụ đã bổ sung có chọn lọc như mục trên, chưa phủ toàn roster.
- Khi Play: menu `BuildBuild → Combat → Enhanced feedback (A-B)` bật/tắt lớp mới để so cảm giác. Đây là toggle presentation, không rollback pooling hay sửa HitDelay. Checkpoint rollback toàn code vẫn là `25342b9`.
- Đã build `BuildBuild.sln`: 0 lỗi, 22 warning hiện có; Unity Console không có error và shader không có message lỗi tại kiểm tra. Smoke checks trực tiếp trong Unity: damage totals/hướng, fully blocked/invulnerable/non-direct/self, per-clip HitDelay; cap 48, pause khi editor frame vẫn chạy, expire/recycle/reuse pool. Chưa đo CPU/GPU trên thiết bị mục tiêu hoặc thử toàn roster/biome.
- Ảnh A/B lấy trong scene Battle cùng camera và target, chủ động gây một hit 671 rồi giữ ở khoảnh khắc impact; đây là hình kiểm chứng hiển thị, không phải benchmark hay ảnh trận tự nhiên. Ảnh cuối tại `tempart/combat-feedback-checkpoint-20261021/comparison.png`; `impact-after.png` và `comparison.png` đã cập nhật bản không vệt cyan (đếm vùng impact: 0 pixel cyan).
- Save `save.json`, `save-test.json`, `save-fresh.json` được backup trước Play trong `tempart/combat-feedback-checkpoint-20261021/` và phục hồi sau kiểm tra; không đổi tài khoản. Giữ bản backup để đối chiếu hash.

- **Sửa sau review (2026-10-21, DSH):** bốn lỗi thật đã sửa — (1) hai đồng hồ khác nhau cho tint đỏ (scaled) và silhouette trắng (unscaled) làm đỏ mất hút ở x3; (2) cache `Sprite→Mesh` tĩnh không bao giờ thả nên giữ atlas của trận trước sau khi đổi scene; (3) `Camera.main` + `GetComponent` tra lại mỗi lần Kick và `AddComponent` ở lần rung đầu; (4) rung/tiếng đòn nặng lặp tay ở từng hero (5 chỗ `Kick` + 7 chỗ `if (Enhanced) Sfx.Play`). Nay: `CombatAccent` giữ material + cache và thả khi scene unload, `CombatFeedback.PresentationOn` là cửa duy nhất, `CombatFeedback.HeavyHit/Cue` là cửa duy nhất, tint đỏ cùng đồng hồ thực với silhouette. `dotnet build` runtime 0 lỗi/4 warning, editor 0 lỗi; CHƯA Play lại — cảm giác và hiệu năng vẫn cần nghiệm thu như mục 6–7.

## Checkpoint và phạm vi

- Git checkpoint trước nghiên cứu: `25342b982af8dd9bb46bf070dabf6926d8f46abc`, nhánh `dev`, message `checkpoint: before combat feel research 2026-10-21`.
- Commit lưu 229 file thay đổi có sẵn, gồm code/scene/art; không phải các thay đổi do nghiên cứu này tạo.
- Git bỏ qua `tempart/`, `docs/project-state/` và runtime governance theo `.gitignore`; checkpoint không bảo vệ các thư mục bị ignore. Không tự chạy reset khi chưa có yêu cầu rollback.
- Đã đọc code tĩnh; chưa quay video, đo GPU/GC, nghe âm thanh hay chạy A/B trong Unity. Không suy ra hiệu năng hoặc mức cải thiện cảm nhận từ code.

## Ảnh mẫu nói được gì

Thấy rõ: burst trắng tương phản cao tại nơi trúng, mảng cyan kéo theo một hướng, số damage trắng có viền tối tách khỏi FX. Điểm chạm trở thành tâm chú ý, số ở phía trên vẫn đọc được.

Ảnh tĩnh không chứng minh duration, hit-stop, screen shake, âm thanh, thứ tự frame hoặc cơ chế crit. Mọi thông số dưới đây là giả thuyết làm thử, không phải đo từ game mẫu. Có thể tái tạo ngôn ngữ impact, chưa thể hứa giống chuyển động khi thiếu clip.

## Nguồn và cách áp dụng

1. [Lin et al., What Features Influence Impact Feel? (2022)](https://arxiv.org/abs/2208.06155): đề xuất khung 19 đặc tính, nhận thấy hit-stop, độ khớp âm thanh và camera có thể ảnh hưởng mạnh đến impact feel. Đây là phân tích đặc tính/game và nhận xét người chơi, không đưa ra bộ milliseconds tối ưu cho tower defense. Áp dụng: thử từng lớp feedback, không coi tăng brightness là toàn bộ giải pháp.
2. [Riot, VFX Style Guide — bản PDF chính thức](https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf): yếu tố chính phải truyền đạt gameplay, yếu tố phụ hỗ trợ chủ đề và điểm nhấn. Áp dụng: core chỉ điểm trúng; vệt chỉ hướng; vòng đất chỉ vùng tác động thật; giảm chi tiết phụ khi trận đông.
3. [GDC, Artistic Principles of VFX](https://gdcvault.com/play/1024439/Visual-Effects-Bootcamp-Artistic-Principles): trình bày nguyên tắc mỹ thuật và gameplay qua League/Hearthstone. Dùng làm nguồn tham khảo thiết kế, không coi là bằng chứng đã xem toàn bộ talk.
4. [Unity, Time.timeScale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeScale.html): thay timeScale ảnh hưởng thời gian mô phỏng; timeScale bằng 0 dừng FixedUpdate và coroutine đang chờ WaitForSeconds. Áp dụng: không thêm coroutine đặt timeScale=0 rồi chờ thời gian scaled để tự khôi phục.
5. [Unity, ObjectPool](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html): tái sử dụng object để giảm chi phí tạo/hủy thường xuyên. Áp dụng: đo allocation khi tăng spark/số; pool các đối tượng phát lặp nhiều nếu profile cho thấy cần. Tài liệu tham khảo Unity 6.0; project hiện là 6000.4.0f1, phải đối chiếu API khi triển khai.

## Audit hiện tại

Các đường dẫn dưới tính từ gốc repository; số dòng tại checkpoint trên.

| Khu vực | Bằng chứng | Hệ quả với mục tiêu |
|---|---|---|
| Flash | `Assets/Scripts/Combat/CombatFeedback.cs:20,68,89`: flash đỏ 0,12 giây game qua `_Color` nhân màu | Không thể biến pixel tối thành silhouette trắng chỉ bằng nhân màu. Cần thử shader blend sang trắng trong alpha sprite, giữ được tint trạng thái sau flash. Chưa kiểm shader thực tế trong Play. |
| Impact chung | `CombatFeedback.cs:68`: xếp damage + flash, không spawn impact | Một số đòn có FX riêng nhưng chưa có lớp spark chung bảo đảm đòn được đọc rõ. Tránh spawn trùng impact riêng đã có. |
| Hướng projectile | `Assets/Scripts/Combat/Projectile.cs:157–173`: chuyển impact, reset rotation và flipX | Burst đối xứng dùng được; vệt định hướng cần giữ hướng bay ngay trước va chạm. Không chỉ lấy hướng nhìn hero nếu đạn vòng/quay lại. |
| Số damage | `Assets/Scripts/Combat/FloatingNumber.cs:15–18,37,50,71`: sống 0,8s, rise 0,45 world, pop 1,35; root + hai label mỗi số | Đã có pop, không cần viết lại từ đầu. Bóng lệch hiện tại khác viền kín ảnh mẫu. `CombatFeedback.cs:113` chỉ gom cùng frame, chưa giải quyết nhiều số chồng giữa các frame. |
| Nguồn damage | `Assets/Scripts/Core/Health.cs:22,56–59,171–183` | Local `Damaged(float)` thiếu source/kind/hướng; global events có thêm source và HP thực mất nhưng vẫn thiếu điểm chạm/hướng đạn/crit/skill. Shield-only có đường riêng. Không gắn spark nặng vào mọi lần HP giảm: độc/phản đòn/liên kết cần phản hồi khác. |
| Nhịp đánh thường | `Assets/Scripts/Core/SpriteAnimator.cs:97,131`; `Assets/Scripts/Combat/Hero.cs:1274–1277` | Đã có HitFrame, nên bám thời điểm release/trúng thực tế. Đạn bắn ra và đạn chạm là hai thời điểm khác nhau. |
| Nguy cơ nhịp chiêu phụ | `Hero.cs:953–957` so với `SpriteAnimator.cs:103–106`; `HongLien.cs:143,164`, `InkScribe.cs:116,176` | HitDelay dùng FPS chung nhưng animator ưu tiên clip.frameSeconds. Có thể lệch nếu clip có override; chưa chứng minh các clip này đang có override hoặc đang lệch. Kiểm trước khi sửa. |
| Âm thanh | `Assets/Scripts/Core/Sfx.cs:25–26,83–98`; `Hero.cs:1006,1065,968–979` | Đã có 16 voice và khoảng cách cùng ID 60ms theo unscaled time. Ultimate có skillSfx, đòn thường có fireSfx; PlayAction không phát tiếng chung cho chiêu phụ. Giữ limiter, phân biệt tiếng cast với tiếng chạm. |
| FX và allocation | `Assets/Scripts/Combat/OneShotFx.cs:13,79,94,98,107`; `Projectile.cs:214` | FX mặc định 9fps, tạo/hủy object; số cũng tạo/hủy. 4 frame khoảng 0,44 giây game, khoảng 0,15 giây thực ở x3. Đây là phép tính, chưa phải đo frame hiển thị. |
| Pause/tua | `Assets/Scripts/Core/GameSpeed.cs:18,84`; `Assets/Scripts/UI/HeroBoard.cs:316–317,413` | Hai đường đang điều khiển/lưu phục hồi timeScale. Hit-stop toàn trận có thể xung đột pause/x3. Chưa tìm thấy combat hit-stop hay camera shake trong `Assets/Scripts` (có shake UI gacha). |
| Hurt | `Hero.cs:1213–1218`; `Enemy.cs:755–768` | Đang chủ động tránh ngắt cast bởi hurt, boss còn miễn hurt/knockback. Feedback mới không được restart animation hoặc đẩy transform gốc làm sai ô/chặn/target. |

## Phương án làm thử đề xuất

Chỉ thử một hero bắn xa trước (Frostweaver phù hợp để so cast → projectile → impact), rồi một đòn cận chiến và một AoE. Chọn combat event thực tế làm điểm phát; không thay toàn roster ngay.

| Lớp | Giả thuyết thông số ban đầu | Tiêu chí |
|---|---|---|
| Impact | Core trắng 1–2 frame sáng nhất; tổng burst khoảng 100–180ms thực để thử x1/x3 | Nhìn được điểm trúng ở camera game; FX tắt gọn, không che cả cụm lâu. Con số là dải thử, cần chỉnh bằng video. |
| Vệt hướng | 3–5 frame hình kéo dài theo vector va chạm, accent của nguyên tố/chủ nhân | Hai phía đánh có hướng đúng; giữ được hướng đạn pierce/return; không dùng cyan cho toàn roster vì cyan/blue đã mang nghĩa slow/frost. |
| Flash thân | Thử blend trắng 40–70ms thực ở direct hit nổi bật; reset về tint trạng thái | Không tạo một khối trắng liên tục dưới tốc đánh cao; không mất màu đóng băng/độc sau flash. |
| Số | Giữ pop hiện có; thử trắng ngà + viền tối cho damage địch, heal xanh; đòn mạnh khác cỡ vừa phải | Đọc được trên map tuyết và nền tối ở kích thước game; không che HP. Chỉ phân biệt crit nếu gameplay thực sự có crit metadata. |
| Âm thanh | Tiếng release ở lúc nhả đạn, impact ở lúc chạm; chiêu phụ có accent riêng | Khớp frame, nghe rõ giữa trận đông mà không tăng volume hàng loạt; không phát tiếng chạm nếu chỉ cast hụt. |
| Đòn rất mạnh | Sau khi thử core: thử rung ngắn 60–100ms hoặc local visual hold 25–45ms | Chỉ dùng sự kiện hiếm như ultimate/boss, có giới hạn kích hoạt; không dừng cả trận theo từng hit. Không thêm stun/knockback gameplay chỉ để tạo cảm giác. |

Thứ tự ưu tiên: **P0** điểm chạm/hướng/timing + impact và flash cho mẫu thử; **P1** âm thanh chiêu phụ + số đọc tốt + chống chồng; **P2** profile/pool và thử phản hồi đòn lớn. Đây là thứ tự đề xuất, chưa phải quyết định kiến trúc hoặc xác nhận triển khai.

Với thời gian: mô phỏng damage/cooldown tiếp tục theo tốc trận. Presentation ngắn cần thử một thời lượng thực tối thiểu ở x3, nhưng pause thật phải đóng băng và không tiếp tục sinh FX. Nếu chọn unscaled time cho phần nhìn thì phải gate theo trạng thái pause, không chỉ đổi deltaTime. Không khôi phục timeScale về 1 tùy tiện; ưu tiên thử phản hồi cục bộ trước khi cân nhắc global hit-stop.

Với art: lõi sáng nhỏ, shape rõ và cạnh pixel cứng; không cần bật bloom toàn màn. Body, projectile/impact và ground-zone là sheet riêng. Nếu tạo raster, bắt buộc dùng `C:/tmp/agent-sprite-forge` / `generate2dsprite` + processor, image_gen chỉ sinh raw; kiểm alpha, frame/pivot/feet và scale theo workflow dự án. Chưa sinh asset trong phiên này. AGENTS người dùng vừa cung cấp yêu cầu Frostweaver bắt buộc, còn `art-style.md:5,26` ghi Stormcaller thay Frostweaver ngày 2026-10-10; khi tạo art phải ưu tiên chỉ dẫn hiện tại của người dùng và nêu rõ mâu thuẫn này, không tự đổi reference.

## Nghiệm thu A/B khi triển khai

1. Quay cùng đội hình, wave và tốc trận, trước/sau ở camera gameplay; không chỉ xem ảnh zoom. Dùng seed cố định nếu đã có cơ chế seed, không mặc định project có.
2. Thử đòn trúng/hụt, hai hướng, projectile pierce/return, shield-only, invulnerable, periodic/reflected/linked, chết bởi hit và mục tiêu mất giữa đường. Spark phải theo sự kiện đúng, không phát thêm damage hoặc đúp impact.
3. Đối chiếu frame: damage/HP, flash, spark và tiếng chạm cùng thời điểm trúng. Với đánh xa, release đi trước impact theo thời gian bay.
4. Test x1/x2/x3; pause trong impact, mở/đóng HeroBoard, đổi speed khi đang pause, kết thúc wave và đổi scene. Không tự unpause, không làm tốc độ rò sang sảnh.
5. Test một unit và trận đông trên map tuyết/map tối: đọc được điểm chạm, hướng, số và vùng nguy hiểm. Giới hạn burst theo cụm khi nhiều tick trùng nhau; không làm mất thông tin damage thật.
6. Đo CPU/GPU frame time, GC allocation, object count và overdraw ở cùng điều kiện trên thiết bị mục tiêu. Chốt ngân sách sau baseline; chưa có số để tuyên bố đạt 60fps hay không tăng GC.
7. Build/compile và kiểm Unity Console; xem video ở tốc độ thật để quyết định. Bản thử đã có kết quả smoke test ở mục đầu; cảm giác chuyển động, toàn roster và hiệu năng thiết bị vẫn cần nghiệm thu tiếp.

Kết luận nghiên cứu: nền code đủ để thử theo phạm vi nhỏ. Phần tạo khác biệt lớn nhất so ảnh là impact sáng có hướng tại đúng thời điểm chạm; phải phối hợp timing, âm thanh và mật độ FX để combat rõ hơn khi đông unit.
