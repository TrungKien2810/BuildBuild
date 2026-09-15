"""Tach 4 chan ngua tu idle-1 (da duyet) thanh mask + khop, xuat anh debug de kiem bang mat."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
IDLE = HERE.parent.parent / "idle" / "idle-1.png"

# vung bao tung chan (toa do idle-1) va khop: goc -> goi/khoeo -> co chan -> day mong
LEGS = {
    "far_fore":  dict(poly=[(43, 89), (55, 89), (55, 117), (43, 117)],             joints=[(52, 90), (47, 99), (50, 109), (50, 114)]),
    "near_fore": dict(poly=[(56, 90), (68, 90), (68, 117), (56, 117)],             joints=[(62, 90), (62, 101), (63, 110), (62, 116)]),
    "far_hind":  dict(poly=[(70, 94), (85, 94), (85, 104), (82, 117), (70, 117)],  joints=[(83, 95), (80, 103), (76, 110), (76, 114)]),
    "near_hind": dict(poly=[(84, 86), (96, 86), (102, 117), (87, 117), (84, 101)], joints=[(87, 88), (93, 98), (94, 110), (94, 114)]),
}

def bone_mask(a):
    r, g, b, al = [a[..., i].astype(int) for i in range(4)]
    bright = (al > 0) & ((r + g + b) / 3 > 95) & (r >= b)
    dark_warm = (al > 0) & ((r + g + b) / 3 <= 95) & (b <= r + 6)   # vien toi cua xuong (khong tim)
    dil = bright.copy()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
        dil |= np.roll(np.roll(bright, dy, 0), dx, 1)
    return bright | (dil & (al > 0) & ((b <= r + 14) | ((r + g + b) / 3 < 40)))

def leg_masks(a):
    bm = bone_mask(a)
    h, w = a.shape[:2]
    out = {}
    for name, leg in LEGS.items():
        pm = Image.new("L", (w, h), 0)
        ImageDraw.Draw(pm).polygon(leg["poly"], fill=255)
        out[name] = bm & (np.array(pm) > 0)
    return out

if __name__ == "__main__":
    a = np.array(Image.open(IDLE).convert("RGBA"))
    masks = leg_masks(a)
    cols = {"far_fore": (60, 120, 255), "near_fore": (255, 255, 255), "far_hind": (0, 200, 255), "near_hind": (255, 200, 0)}
    k, x0, y0, x1, y1 = 10, 38, 80, 106, 120
    base = Image.new("RGBA", a.shape[1::-1], (60, 60, 70, 255)); base.alpha_composite(Image.fromarray(a))
    ov = np.array(base).copy()
    for n, m in masks.items():
        ov[m, :3] = (0.45 * ov[m, :3] + 0.55 * np.array(cols[n])).astype(np.uint8)
    pair = Image.new("RGBA", ((x1 - x0) * k * 2 + 10, (y1 - y0) * k), (0, 0, 0, 255))
    pair.paste(base.crop((x0, y0, x1, y1)).resize(((x1 - x0) * k, (y1 - y0) * k), Image.NEAREST), (0, 0))
    right = Image.fromarray(ov).crop((x0, y0, x1, y1)).resize(((x1 - x0) * k, (y1 - y0) * k), Image.NEAREST)
    d = ImageDraw.Draw(right)
    for n, leg in LEGS.items():
        pts = [((x - x0 + 0.5) * k, (y - y0 + 0.5) * k) for x, y in leg["joints"]]
        d.line(pts, fill=(255, 0, 0), width=2)
        for p in pts: d.ellipse([p[0] - 4, p[1] - 4, p[0] + 4, p[1] + 4], outline=(255, 0, 0), width=2)
    pair.paste(right, ((x1 - x0) * k + 10, 0))
    pair.save(HERE / "debug-masks-10x.png")
    for n, m in masks.items(): print(n, int(m.sum()), "px")
