# SpriteCook vs PixelLab — kế hoạch đánh giá

## Setup đã xong
- marketplace `spritecook` + plugin `spritecook@spritecook` v0.1.8 (scope user)
- MCP `plugin:spritecook:spritecook` -> https://mcp.spritecook.ai/mcp/claude
- CÒN LẠI: OAuth (`/mcp` trong Claude Code, cần TTY)

## Baseline PixelLab (TinySword)
Pipeline THẬT đang dùng: `create_image_pixflux` (identity tĩnh) + `animate_image`.
KHÔNG dùng `create_character`/`animate_character` — view=side va low top-down deu ra
rotation 3D that -> profile canh ben, lech style. Xem `camera_angle_root_cause` trong
D:/Edu/GameDev/TinySword/config/agent-bridge/pixellab-tuned-params.json

Convention huong nhin: xin model ve quay TRAI, LAT NGANG luc dong goi (0 generation).

Ranger (commit 0a8e50a) — 7 action x 6 frame, raw 80x80, `raw/<Action>/<Action>_refN.png`
(ref0 = anh identity, KHONG dung; frame that la ref1..ref6)

| Action  | Danh gia |
|---------|----------|
| Idle    | Bien do gan bang 0 |
| Walk    | Nhac chan nhe; cung troi vi tri |
| Attack1 | Mui ten ro, nhung frame 1-2-6 trung idle |
| Attack2 | Nhu tren |
| Cast    | PROP DRIFT NANG — cung doi hinh dang/kich thuoc/huong |
| Hurt    | Hau nhu khong chuyen dong |
| Death   | Tot nhat, arc sup do ro |

Contact sheet: ./ranger_pixellab.png

## Test se chay sau khi auth
0. get_credit_balance  +  list_character_workflows   (LUON chay truoc)

### Test A — dung yeu cau user (top-down)
generate_character(prompt=<char>, perspective="topdown")
generate_character_animations(character_id, perspective="topdown",
  animation_ids=[idle, idle_back, idle_right, walk_up, walk_down, walk_right,
                 attack, hurt, death])
Ghi chu: preset topdown KHONG co walk_left/idle_left -> SpriteCook thiet ke san
theo huong "phai la mac dinh, trai thi flip". Khop yeu cau user.

### Test B — apples-to-apples voi Ranger (QUAN TRONG HON)
generate_game_art(pixel=true, 64x64, bg=transparent) tao identity giong Ranger
animate_game_art(asset_id) cho Idle/Walk/Attack/Cast/Hurt/Death
=> so thang 6 hang canh nhau voi ranger_pixellab.png
CAST LA BAI TEST QUYET DINH: PixelLab drift hinh cung; SpriteCook giu duoc khong?

## Chi so can do de so
- so mau unique (srcAssets goc 7-16; atlas PixelLab that 141-181)
- bien do chuyen dong giua cac frame
- prop/weapon consistency qua 6 frame
- foot-Y drift
- credit ton / action
