# Lane sinh asset — giao Codex / Antigravity làm ảnh game

Bài này dạy **cách giao một việc sinh ảnh cho agent khác chạy headless** rồi nghiệm thu kết quả.
Không dạy vẽ: quy ước tạo hình nằm trong `docs/huong-dan/nghiem-thu-identity-sprite.md`.

## Khi nào dùng lane, khi nào tự làm

Dùng lane khi việc là **sinh ảnh**: identity, frame action, atlas, icon UI, nền — tức thứ tốn thời
gian gõ prompt và gọi model ảnh, và **không cần đọc code của mình**.

Đừng dùng khi việc là sửa code/scene: lane con không thấy HANDOFF, không thấy doc-map, không có hook
governance — nó sẽ sửa mù. Việc code thì làm trong phiên chính.

## Gọi một lần

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File config/agent-bridge/art-lane.ps1 `
  -Lane codex `
  -Brief config/agent-bridge/briefs/brief-<slug>.md `
  -Allow "tempart/<Unit>/**" `
  -TimeoutSec 900
```

- `-Lane codex` hoặc `-Lane agy` (Antigravity). `-Model` chỉ có tác dụng với agy.
- `-Allow` là allowlist của **lần chạy đó** — mặc định `tempart/**`. Đừng mở `Assets/**` cho lane
  trừ khi bạn thật sự muốn nó ghi thẳng vào asset production.
- `-DryRun` in ra prompt + lệnh mà không chạy. Dùng khi nghi brief sai.
- `-Resume` nối phiên trước (nói tiếp "frame 3 to hơn" mà không phải gửi lại cả brief).

Mỗi lần chạy tạo `.govern/agent-bridge/<stamp>/` gồm `prompt.md` (brief + contract đã ghép),
`stdout.log`, `stderr.log`, `last-message.md`, `run.json`.

## Viết brief — 5 mục, đừng hơn

Mẫu: `config/agent-bridge/brief-template.md`. Bắt buộc có: **mục tiêu**, **tạo hình** (kèm ảnh tham
chiếu để mở ra xem), **danh sách frame**, **output path**, **ràng buộc riêng**.

Brief là **toàn bộ** context con có. Viết như giao việc cho người mới: "con dơi xám tím, cánh xoè,
hướng sang phải", không phải "làm con dơi như cũ".

## Luật art nằm ở đâu

`config/agent-bridge/asset-contract.md` — script chèn file này vào cuối prompt mỗi lần chạy. Nó gồm:
hướng nhìn 3/4 + bảng aspect theo unit, nền chroma `#FF00FF`, baseline chân, khe 2 chân ≥2px, quy
trình raw → key → despill → đo → mắt, và khuôn final message.

Sửa luật art thì sửa **file đó**, đừng sửa từng brief — brief là biến, contract là hằng.

## Nghiệm thu: đọc báo cáo, đừng tin lời khai

Script in ra 4 khối: `FINAL MESSAGE` của con, `FILES TOUCHED` (kèm kích thước PNG thật đọc từ IHDR),
`QC` (mỗi file một dòng `canvas/bbox/aspect/colors/alevels/magenta/mirror`), và `OUTSIDE ALLOWLIST`
nếu con ghi ra ngoài vùng cho phép.

Cách chấm:

1. `RESULT: done` mà không có file nào trong `FILES TOUCHED` → con bịa, bỏ qua.
2. `magenta` phải `0.0%`; còn số thì chạy lại `despill_magenta.py`.
3. `bbox`/`aspect` so bảng unit trong contract; lệch quá ~10% là phải sinh lại.
4. `mirror` 45–62% = 3/4 chuẩn. >80% = chính diện, sai pose (trừ unit đối xứng).
5. Mở ảnh ra nhìn ở 4x — số đo không thấy được giải phẫu vỡ.

Dòng `RAW-SHEET` trong QC nghĩa là file đó **chưa key** (còn nền magenta) nên bbox/mirror vô nghĩa;
đo lại trên file đã key.

## Đóng asset vào game (bước của NGƯỜI DUYỆT, không phải lane)

Lane chỉ ghi vào `tempart/<Unit>/<action>/`. Đưa vào production:

1. Chọn frame đạt → copy sang `Assets/Art/Generated/<Unit>/<action>/<action>-<i>.png`.
2. Thêm/cập nhật `pipeline-meta.json` trong thư mục action: `source` = lane nào + brief nào + ngày,
   cộng số đo QC (`bbox`, `mirror`, `magenta`).
3. Để Unity tự import (`.meta` do `GeneratedArtImporter` sinh) — đừng tạo `.meta` bằng tay.
4. Xoá/đổi tên file cũ trong thư mục đích trước khi copy, không để hai phiên bản nằm cạnh nhau.

## Trạng thái thật của lane trên máy này (2026-10-26)

| Lane | Trạng thái | Bằng chứng |
|---|---|---|
| `codex` | Chạy được, đã smoke 2 lần | slime 128×128, 53s, ~18k token; bat identity `tempart/_smoke/bat/identity-1.png` — bbox 94×80, aspect 1.175, mirror 59.69%, magenta 0%, 385s |
| `agy` | **Chưa cài** — `-Lane agy` báo lỗi rõ | không có `agy` trên PATH, không có `~/.gemini/` |

`agy` muốn dùng được thì: cài Antigravity CLI + đăng nhập, rồi đặt `AGY_EXE` hoặc thêm đường dẫn
vào `Resolve-Agy` trong `art-lane.ps1`. Cờ headless của agy (`--print`, `--add-dir`,
`--dangerously-skip-permissions`) lấy từ lần chạy thật trước đây ở project khác — **phải kiểm lại**
với bản agy mới cài trước khi tin.

## Bẫy đã dính (đừng dính lại)

- Gọi `codex` qua shim `codex.ps1` bằng `powershell -File` → `argument "name" is not valid`. Đi thẳng
  `node <...>/@openai/codex/bin/codex.js` thì chạy.
- Prompt không nói rõ "luật nằm trong message này" → con đi tìm `asset-contract.md` trên đĩa rồi
  `blocked`. Header trong `art-lane.ps1` sinh ra để chặn đúng ca đó.
- Model ảnh mặc định lưu ảnh **ngoài repo** (`$CODEX_HOME/…`), mà allowlist thì chỉ có trong repo →
  con `blocked`. Contract đã ghi rõ: được phép copy từ ngoài vào, một chiều.
- Máy này **không có python trên PATH**; script tự rơi về python của DSH runtime (có Pillow) và
  chèn đường dẫn đó vào prompt dưới dạng `{{PYTHON}}` để con chạy được `key_chroma.py` / `qc_png.py`.
