"""Compose `Assets/Art/Generated/Fence/ground-skirt.png` — the band that buries the terrace's bottom edge in ground.

The background is a full painted field; the stone terrace is a separate sprite drawn on top of it, which is why its
bottom reads as a cut-out: the wall stops on a perfectly straight navy line with the field's grass starting one
pixel below. This asset reveals the GROUND THAT IS ALREADY PAINTED UNDER THE TERRACE along an irregular silhouette,
so the wall looks planted in the earth instead of pasted over it.

Because every pixel is copied from the same canvas position of the same background, the colours cannot drift and
the apron's bottom edge is invisible by construction — there is nothing "added" to the image except an irregular
top line. The art lane (`agy`) was asked for this strip first and returned thin pale tufts whose alpha line did
not match the terrace (deleted — see HANDOFF 2026-10-30).

Layout contract with `SceneBuilder.BuildFenceSkirt`:
  * canvas 1024 x 96, 1 art px = 1 screen px, centred on the terrace;
  * row `SEAM_Y` lands on the terrace's bottom edge (world y = FenceFarPx + 624 px = screen y 874);
  * above that row the apron climbs 3..20 px (rare 24) so the straight outline is covered by grass/pebbles;
  * below it the apron is a straight copy of the background, so nothing below the seam changes.

Run:  <python> tools/art/make_fence_skirt.py
"""
import os
from PIL import Image

BG = "Assets/Art/Generated/Background/bg-field.png"
OUT = "Assets/Art/Generated/Fence/ground-skirt.png"

SEAM_Y = 48            # phai khop SceneBuilder.SkirtSeamPx
WIDTH = 1024
HEIGHT = 96
LEFT_PX = -254         # cot i = man x (i + LEFT_PX)
SEAM_SCREEN_Y = 874    # man y cua duong cat = FenceFarPx(250) + 624


def hash_noise(x, salt=0):
    """0..1, tat dinh — chay lai ra dung file cu."""
    n = (x * 374761393 + salt * 668265263) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0


def smooth(x, salt, step=44):
    a = hash_noise(x // step, salt)
    b = hash_noise(x // step + 1, salt)
    t = (x % step) / step
    return a + (b - a) * t


def build():
    bg = Image.open(BG).convert("RGBA")
    bg_w, bg_h = bg.size
    apron = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    px = apron.load()

    tops = []
    for i in range(WIDTH):
        climb = 6 + smooth(i, 3) * 14
        if hash_noise(i // 11, 5) > 0.82:
            climb += 3 + smooth(i, 9) * 4
        tops.append(SEAM_Y - int(climb))

    # Man y cua hang 0 cua dai = SEAM_SCREEN_Y - SEAM_Y; moi px la px NEN tai dung cho cua no.
    top_screen_y = SEAM_SCREEN_Y - SEAM_Y

    for i in range(WIDTH):
        sx = min(bg_w - 1, max(0, i + LEFT_PX))
        for y in range(tops[i], HEIGHT):
            sy = min(bg_h - 1, max(0, top_screen_y + y))
            px[i, y] = bg.getpixel((sx, sy))

    # 1 px vien toi o mep tren: khoi dat an bong len chan tuong (sang tu tren-trai).
    shade = (43, 28, 23)
    for i in range(WIDTH):
        y = tops[i]
        if 0 <= y < HEIGHT:
            r, g, b, a = px[i, y]
            t = 0.35
            px[i, y] = (int(r * (1 - t) + shade[0] * t), int(g * (1 - t) + shade[1] * t),
                        int(b * (1 - t) + shade[2] * t), a)

    # Vai co moi vươn len khoi mep 1..4 px: chi o nhung cot mep la dat tron (khong co co san).
    for k in range(70):
        i = int(hash_noise(k, 13) * (WIDTH - 1))
        top = tops[i]
        height = 1 + int(hash_noise(k, 17) * 4)
        base = px[i, min(HEIGHT - 1, top + 2)]
        blade = (max(0, base[0] - 4), min(255, base[1] + 6), max(0, base[2] - 6), 255)
        for step in range(1, height + 1):
            y = top - step
            if y < 0:
                break
            if px[i, y][3] == 0:
                px[i, y] = blade

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    apron.save(OUT)
    print("wrote", OUT, apron.size, "climb", SEAM_Y - max(tops), "..", SEAM_Y - min(tops), "px")


if __name__ == "__main__":
    build()
