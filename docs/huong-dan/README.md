# Hướng dẫn tự làm — đọc từ đâu

Thư mục này dạy bạn **tự làm lại dự án bằng tay, không cần AI**: mở gì, bấm ô nào, gõ lệnh nào, và code
bên trong chạy ra sao. Mỗi bài đứng riêng được, nhưng đọc theo thứ tự dưới đây thì bài sau không phải
giải thích lại khái niệm của bài trước.

## Chặng 1 — Dựng game trong Unity

Đọc theo đúng thứ tự:

1. [unity-import-sprite.md](unity-import-sprite.md): import PNG pixel-art (7 ô Inspector), pivot chân
   cho từng action, `GeneratedArtImporter` tự làm những việc đó thế nào.
2. [unity-2d-lane-chieu-sau.md](unity-2d-lane-chieu-sau.md): dải đất có chiều sâu, sort theo trục y, ô đặt
   hero bấm được.
3. [dung-scene-gameplay.md](dung-scene-gameplay.md): toạ độ pixel ↔ world, nền, lâu đài, quái/wave, HUD
   9-slice, gacha/pity, save JSON, màn Đội quân (nâng level/sao) — và vì sao scene dựng bằng `SceneBuilder`.
4. [ky-nang-hero-va-hieu-ung-trang-thai.md](ky-nang-hero-va-hieu-ung-trang-thai.md): thêm một kỹ năng
   hero hoặc hiệu ứng trạng thái (đóng băng, choáng) từng bước.
5. [am-thanh.md](am-thanh.md): thêm/đổi file tiếng, gán tiếng riêng cho hero/pháo, `Sfx` và tiếng đòn đánh chạy ra sao.

## Chặng 2 — Vẽ và chấm art

Đọc khi bạn tự vẽ sprite/animation. Bài 6 trước, các bài sau chọn theo loại clip đang làm:

6. [nghiem-thu-identity-sprite.md](nghiem-thu-identity-sprite.md): chấm frame nhận dạng của nhân vật trước
   khi vẽ các action (kích thước, hướng nhìn, khe chân, sạch mép).
7. Walk:
   - nhân vật hai chân tách rời → [nghiem-thu-walk-cycle.md](nghiem-thu-walk-cycle.md);
   - golem / áo choàng / không tách được chân → [nghiem-thu-locomotion-khong-co-chan.md](nghiem-thu-locomotion-khong-co-chan.md);
   - unit 4 chân (ngựa) → [rig-chan-unit-4-chan-bang-code.md](rig-chan-unit-4-chan-bang-code.md), dựng walk
     từ idle bằng script, không cần vẽ từng frame.
8. [nghiem-thu-clip-giu-the.md](nghiem-thu-clip-giu-the.md): clip chiêu đứng yên (hoá đá, khiên).
9. [sua-scale-va-troi-frame.md](sua-scale-va-troi-frame.md): nhân vật phồng xẹp hoặc trôi ngang giữa các frame.
10. [asset-cong-trinh-vao-khung-toa-do.md](asset-cong-trinh-vao-khung-toa-do.md): lâu đài/nền — crop vào
   khung, đo điểm neo, pitch mặt đất.

## Không nằm ở đây

- Đang làm tới đâu → `docs/project-state/HANDOFF.md`.
- Luật chơi, chỉ số → `GAME_DESIGN.md`.
- Bài học/bẫy agent đã dính, quy trình sinh art bằng AI → `docs/project-state/conventions.md`.
